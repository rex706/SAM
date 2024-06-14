using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SAM.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using SteamIDs_Engine;
using Newtonsoft.Json;
using SAM.Properties;

namespace SAM.Core
{
    class AccountUtils
    {
        private const int API_KEY_LENGTH = 32;

        public static void Serialize(List<Account> accounts)
        {
            var serializer = new XmlSerializer(accounts.GetType());
            var sw = new StreamWriter("info.dat");
            serializer.Serialize(sw, accounts);
            sw.Close();
        }

        public static void PasswordSerialize(List<Account> accounts, string password)
        {
            var serializer = new XmlSerializer(accounts.GetType());
            MemoryStream memStream = new MemoryStream();
            serializer.Serialize(memStream, accounts);

            string serializedAccounts = Encoding.UTF8.GetString(memStream.ToArray());
            string encryptedAccounts = StringCipher.Encrypt(serializedAccounts, password);

            File.WriteAllText("info.dat", encryptedAccounts);

            memStream.Close();
        }

        public static List<Account> Deserialize(string file)
        {
            object obj = null;

            try
            {
                var stream = new StreamReader(file);
                var ser = new XmlSerializer(typeof(List<Account>));
                obj = ser.Deserialize(stream);
                stream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return (List<Account>)obj;
        }

        public static List<Account> PasswordDeserialize(string file, string password)
        {
            object obj = null;

            try
            {
                string contents = File.ReadAllText(file);
                contents = StringCipher.Decrypt(contents, password);

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));

                var ser = new XmlSerializer(typeof(List<Account>));
                obj = ser.Deserialize(stream);

                stream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return (List<Account>)obj;
        }

        public static void ImportAccountsFromList(List<Account> accounts)
        {
            try
            {
                AccountsWindow.encryptedAccounts = AccountsWindow.encryptedAccounts.Concat(accounts).ToList();
                Serialize(AccountsWindow.encryptedAccounts);
                MessageBox.Show("Account(s) imported!");
            }
            catch (Exception m)
            {
                MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ImportAccountFile()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                DefaultExt = ".dat",
                Filter = "SAM DAT Files (*.dat)|*.dat"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    var tempAccounts = Deserialize(dialog.FileName);
                    AccountsWindow.encryptedAccounts = AccountsWindow.encryptedAccounts.Concat(tempAccounts).ToList();
                    Serialize(AccountsWindow.encryptedAccounts);
                    MessageBox.Show("Accounts imported!");
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void ExportAccountFile()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.Copy("info.dat", dialog.SelectedPath + "\\info.dat");
                    MessageBox.Show("File exported to:\n" + dialog.SelectedPath);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void ExportSelectedAccounts(List<Account> accounts)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var serializer = new XmlSerializer(accounts.GetType());
                    var sw = new StreamWriter(dialog.SelectedPath + "\\info.dat");
                    serializer.Serialize(sw, accounts);
                    sw.Close();

                    MessageBox.Show("File exported to:\n" + dialog.SelectedPath);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static string GetSteamPathFromRegistry()
        {
            string registryValue = string.Empty;
            RegistryKey localKey;

            if (Environment.Is64BitOperatingSystem)
            {
                localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            }
            else
            {
                localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            }

            try
            {
                localKey = localKey.OpenSubKey(@"Software\\Valve\\Steam");
                registryValue = localKey.GetValue("SteamPath").ToString() + "/";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return registryValue;
        }

        public static void ClearAutoLoginUserKeyValues()
        {
            RegistryKey localKey;

            if (Environment.Is64BitOperatingSystem)
            {
                localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            }
            else
            {
                localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            }

            try
            {
                localKey = localKey.OpenSubKey(@"Software\\Valve\\Steam", true);
                localKey.SetValue("AutoLoginUser", "", RegistryValueKind.String);
                localKey.SetValue("RememberPassword", 0, RegistryValueKind.DWord);
                localKey.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void SetRememeberPasswordKeyValue(int value, Account account)
        {
            RegistryKey localKey;

            if (Environment.Is64BitOperatingSystem)
            {
                localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            }
            else
            {
                localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            }

            try
            {
                localKey = localKey.OpenSubKey(@"Software\\Valve\\Steam", true);
                localKey.SetValue("RememberPassword", value, RegistryValueKind.DWord);
                localKey.SetValue("AutoLoginUser", value == 1 ? account.Name.ToLower() : "", RegistryValueKind.String);
                localKey.Close();

                string steamPath = new IniFile(SAMSettings.FILE_NAME).Read(SAMSettings.STEAM_PATH, SAMSettings.SECTION_STEAM);
                string loginusersPath = steamPath + "config/loginusers.vdf";

                dynamic loginusers = VdfConvert.Deserialize(File.ReadAllText(loginusersPath));

                dynamic usersObject = loginusers.Value;
                dynamic userObject = usersObject[account.SteamId];

                userObject.RememberPassword = value;
                userObject.AllowAutoLogin = value;
                //userObject.MostRecent = value;

                usersObject[account.SteamId] = userObject;
                loginusers.Value = usersObject;

                string serialized = VdfConvert.Serialize(loginusers);

                File.WriteAllText(loginusersPath, serialized);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error setting Remember Password values\n\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine(e.StackTrace);
            }
        }

        public static string CheckSteamPath()
        {
            var settingsFile = new IniFile(SAMSettings.FILE_NAME);

            string steamPath = settingsFile.Read(SAMSettings.STEAM_PATH, SAMSettings.SECTION_STEAM);

            int tryCount = 0;

            // If Steam's filepath was not specified in settings or is invalid, attempt to find it and save it.
            while (steamPath == null || !File.Exists(steamPath + "\\steam.exe"))
            {
                // Check registry keys first.
                string regPath = GetSteamPathFromRegistry();
                string localPath = System.Windows.Forms.Application.StartupPath;

                if (Directory.Exists(regPath))
                {
                    steamPath = regPath;
                }

                // Check if SAM is isntalled in Steam directory.
                // Useful for users in portable mode.
                else if (File.Exists(localPath + "\\steam.exe"))
                {
                    steamPath = localPath;
                }

                // Prompt user for manual selection.
                else
                {
                    if (tryCount == 0)
                    {
                        MessageBox.Show("Could not find Steam path automatically.\n\nPlease select Steam manually.");
                    }

                    // Create OpenFileDialog 
                    OpenFileDialog dlg = new OpenFileDialog
                    {
                        DefaultExt = ".exe",
                        Filter = "Steam (*.exe)|*.exe"
                    };

                    // Display OpenFileDialog by calling ShowDialog method 
                    bool? result = dlg.ShowDialog();

                    // Get the selected file path
                    if (result == true)
                    {
                        steamPath = Path.GetDirectoryName(dlg.FileName) + "\\";
                    }
                }

                if (steamPath == null || steamPath == string.Empty)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show("Steam path required!\n\nTry again?", "Confirm", MessageBoxButton.YesNo);

                    if (messageBoxResult.Equals(MessageBoxResult.No))
                    {
                        Environment.Exit(0);
                    }
                }

                tryCount++;
            }

            // Save path to settings file.
            settingsFile.Write(SAMSettings.STEAM_PATH, steamPath, SAMSettings.SECTION_STEAM);

            return steamPath;
        }

        public static List<string> GetSteamIdsFromConfig(List<Account> accounts)
        {
            List<string> steamIds = new List<string>();

            foreach (Account account in accounts)
            {
                string steamId = GetSteamIdFromConfig(account.Name);

                if (steamId != null && steamId.Length > 0)
                {
                    account.SteamId = steamId;
                    steamIds.Add(steamId);
                }
            }

            return steamIds;
        }

        public static async Task<string> GetSteamIdFromProfileUrl(string url)
        {
            dynamic steamId = null;

            if (ApiKeyExists() == true)
            {
                // Get SteamId for either getUserSummaries (if in ID64 format) or vanity URL if (/id/) format.

                if (url.Contains("/id/"))
                {
                    // Vanity URL API call.

                    url = url.TrimEnd('/');
                    url = url.Split('/').Last();

                    if (url.Length > 0)
                    {
                        dynamic userJson = await GetSteamIdFromVanityUrl(url);

                        if (userJson != null)
                        {
                            try
                            {
                                steamId = userJson.response.steamid;
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                else if (url.Contains("/profiles/"))
                {
                    // Standard user summaries API call.

                    dynamic userJson = await GetUserInfoFromWebApiBySteamId(url);

                    if (userJson != null)
                    {
                        try
                        {
                            steamId = userJson.response.players[0].steamid;
                        }
                        catch
                        {

                        }
                    }
                }
            }

            return steamId;
        }

        public static string GetSteamId3FromSteamId64(string steamId)
        {
            try
            {
                string value = SteamIDConvert.Steam64ToSteam32(long.Parse(steamId));
                string[] parts = value.Split(':');
                return parts[2];
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return null;
        }

        public static string GetSteamIdFromConfig(string userName)
        {
            try
            {
                string steamPath = new IniFile(SAMSettings.FILE_NAME).Read(SAMSettings.STEAM_PATH, SAMSettings.SECTION_STEAM);
                string content = File.ReadAllText(steamPath + "config\\loginusers.vdf");
                dynamic config = VdfConvert.Deserialize(content);

                dynamic users = config.Value;

                foreach (dynamic user in users)
                {
                    string steamId = Convert.ToString(user.Key);
                    string accountName = Convert.ToString(user.Value.AccountName);

                    if (accountName.Equals(userName.ToLower()))
                    {
                        return steamId;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return null;
        }

        public static async Task<dynamic> GetUserInfoFromConfigAndWebApi(string userName)
        {
            dynamic userJson = null;
            string steamId = GetSteamIdFromConfig(userName);

            if (steamId != null)
            {
                userJson = await GetUserInfoFromWebApiBySteamId(steamId);
            }

            return userJson;
        }

        public static async Task<dynamic> GetUserInfoFromWebApiBySteamId(string steamId)
        {
            var settingsFile = new IniFile(SAMSettings.FILE_NAME);
            string apiKey = settingsFile.Read(SAMSettings.STEAM_API_KEY, SAMSettings.SECTION_STEAM);

            dynamic userJson = null;

            if (ApiKeyExists() == true)
            {
                try
                {
                    Uri userUri = new Uri("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + apiKey + "&steamids=" + steamId);

                    using (WebClient client = new WebClient())
                    {
                        string userJsonString = await client.DownloadStringTaskAsync(userUri);
                        userJson = JValue.Parse(userJsonString);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            return userJson;
        }

        public static async Task<List<dynamic>> GetUserInfosFromWepApi(List<string> steamIds)
        {
            var settingsFile = new IniFile(SAMSettings.FILE_NAME);
            string apiKey = settingsFile.Read(SAMSettings.STEAM_API_KEY, SAMSettings.SECTION_STEAM);

            List<dynamic> userInfos = new List<dynamic>();

            if (ApiKeyExists() == true)
            {
                while (steamIds.Count > 0)
                {
                    IEnumerable<string> currentChunk;

                    // Api can only process 100 accounts at a time.
                    if (steamIds.Count > 100)
                    {
                        currentChunk = steamIds.Take(100);
                        steamIds = steamIds.Skip(100).ToList();
                    }
                    else
                    {
                        currentChunk = new List<string>(steamIds);
                        steamIds.Clear();
                    }

                    string currentIds = String.Join(",", currentChunk);

                    try
                    {
                        Uri userUri = new Uri("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + apiKey + "&steamids=" + currentIds);

                        using (WebClient client = new WebClient())
                        {
                            string userJsonString = await client.DownloadStringTaskAsync(userUri);
                            dynamic userInfoJson = JValue.Parse(userJsonString);
                            userInfos.Add(userInfoJson);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }
            }

            return userInfos;
        }

        public static async Task<dynamic> GetSteamIdFromVanityUrl(string vanity)
        {
            var settingsFile = new IniFile(SAMSettings.FILE_NAME);
            string apiKey = settingsFile.Read(SAMSettings.STEAM_API_KEY, SAMSettings.SECTION_STEAM);

            dynamic userInfoJson = null;

            if (apiKey != null && apiKey.Length == 32)
            {
                try
                {
                    Uri userUri = new Uri("http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key=" + apiKey + "&vanityurl=" + vanity);

                    using (WebClient client = new WebClient())
                    {
                        string userJsonString = await client.DownloadStringTaskAsync(userUri);
                        userInfoJson = JValue.Parse(userJsonString);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            return userInfoJson;
        }

        public static async Task<dynamic> GetPlayerBansFromWebApi(string steamId)
        {
            List<dynamic> userBansJson = await GetPlayerBansFromWebApi(new List<string>() { steamId });

            if (userBansJson.Count > 0)
            {
                return userBansJson[0].players[0];
            }
            else
            {
                return null;
            }
        }

        public static async Task<List<dynamic>> GetPlayerBansFromWebApi(List<string> steamIds)
        {
            var settingsFile = new IniFile(SAMSettings.FILE_NAME);
            string apiKey = settingsFile.Read(SAMSettings.STEAM_API_KEY, SAMSettings.SECTION_STEAM);

            List<dynamic> userBans = new List<dynamic>();

            if (ApiKeyExists() == true)
            {
                while (steamIds.Count > 0)
                {
                    IEnumerable<string> currentChunk;

                    // Api can only process 100 accounts at a time.
                    if (steamIds.Count > 100)
                    {
                        currentChunk = steamIds.Take(100);
                        steamIds = steamIds.Skip(100).ToList();
                    }
                    else
                    {
                        currentChunk = new List<string>(steamIds);
                        steamIds.Clear();
                    }

                    string currentIds = String.Join(",", currentChunk);

                    try
                    {
                        Uri userUri = new Uri("http://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key=" + apiKey + "&steamids=" + currentIds);

                        using (WebClient client = new WebClient())
                        {
                            string userJsonString = await client.DownloadStringTaskAsync(userUri);
                            dynamic userInfoJson = JValue.Parse(userJsonString);
                            userBans.Add(userInfoJson);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }
            }

            return userBans;
        }

        public static async Task<string> HtmlAviScrapeAsync(string profUrl)
        {
            // If user entered profile url, get avatar jpg url
            if (profUrl != null && profUrl.Length > 2)
            {
                // Verify url starts with valid prefix for HtmlWeb
                if (!profUrl.StartsWith("https://") && !profUrl.StartsWith("http://"))
                {
                    profUrl = "https://" + profUrl;
                }

                try
                {
                    HtmlWeb htmlWeb = new HtmlWeb();
                    HtmlDocument document = await htmlWeb.LoadFromWebAsync(profUrl);
                    IEnumerable<HtmlNode> enumerable = document.DocumentNode.Descendants().Where(n => n.HasClass("playerAvatarAutoSizeInner"));
                    HtmlNode htmlNode = enumerable.First().SelectSingleNode("img");
                    string url = htmlNode.GetAttributeValue("src", null);
                    return url;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return "";
        }

        public static void SetFriendsOnlineMode(FriendsLoginStatus loginMode, string steamId, string steamPath)
        {
            if (loginMode == FriendsLoginStatus.Unchanged || steamId == null || steamId.Length == 0)
            {
                Console.WriteLine("Login mode is unchanged or steamId is invalid!");
                return;
            }

            string steamId3 = GetSteamId3FromSteamId64(steamId);
            string localPrefsKey = "FriendStoreLocalPrefs_" + steamId3;

            string fileName = "localconfig.vdf";
            string configPath = steamPath + "userdata/" + steamId3 + "/config/";
            string configFile = configPath + fileName;

            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }

            dynamic localconfig;

            if (!File.Exists(configFile))
            {
                localconfig = VdfConvert.Deserialize(Resources.localconfig);
            }
            else
            {
                long fileSize = new FileInfo(configFile).Length;
                int maxTokenSize = (int)fileSize / 7;

                VdfSerializerSettings vdfSerializerSettings = new VdfSerializerSettings();
                vdfSerializerSettings.MaximumTokenSize = maxTokenSize;
                vdfSerializerSettings.UsesEscapeSequences = true;
                localconfig = VdfConvert.Deserialize(File.ReadAllText(configFile),vdfSerializerSettings);
            }
            
            dynamic configStore = localconfig.Value;

            string loginValue = "1";
            if (loginMode == FriendsLoginStatus.Offline)
            {
                loginValue = "0";
            }
            else
            {
                dynamic webStorageObject = configStore["WebStorage"];
                dynamic friendStorePrefs = webStorageObject[localPrefsKey];

                if (friendStorePrefs == null)
                {
                    friendStorePrefs = new VValue("{\"ePersonaState\":1,\"strNonFriendsAllowedToMsg\":\"\"}");
                }

                dynamic prefsJson = JsonConvert.DeserializeObject(friendStorePrefs.Value);

                if (loginMode == FriendsLoginStatus.Online)
                {
                    prefsJson.ePersonaState.Value = 1;
                }
                else if (loginMode == FriendsLoginStatus.Invisible)
                {
                    prefsJson.ePersonaState.Value = 7;
                }

                friendStorePrefs.Value = JsonConvert.SerializeObject(prefsJson);
                webStorageObject[localPrefsKey] = friendStorePrefs;
                configStore["WebStorage"] = webStorageObject;
            }

            dynamic friendsObject = configStore["friends"];
            friendsObject.SignIntoFriends = loginValue;
            configStore["friends"] = friendsObject;

            localconfig.Value = configStore;

            string serialized = VdfConvert.Serialize(localconfig);

            File.WriteAllText(configFile, serialized);
        }

        public static string FormatTimespanString(TimeSpan time)
        {
            int years = time.Days / 365;
            int days = time.Days;

            if (years > 0)
            {
                days = (time.Days / (years * 365));
            }

            return years.ToString("D2") + ":" + days.ToString("D2") + ":" + time.ToString(@"hh\:mm\:ss");
        }

        public static bool AccountHasActiveTimeout(Account account)
        {
            if (account.Timeout == null || account.Timeout == new DateTime() || account.Timeout.Value.CompareTo(DateTime.Now) <= 0)
            {
                account.Timeout = null;
                account.TimeoutTimeLeft = null;
                return false;
            }

            return true;
        }

        public static bool ApiKeyExists()
        {
            var settingsFile = new IniFile(SAMSettings.FILE_NAME);
            string apiKey = settingsFile.Read(SAMSettings.STEAM_API_KEY, SAMSettings.SECTION_STEAM);
            return apiKey != null && apiKey.Length == API_KEY_LENGTH;
        }

        public static bool ShouldAutoReload(DateTime? lastReload, int interval)
        {
            if (lastReload.HasValue == false)
            {
                return true;
            }

            DateTime offset = lastReload.Value.AddMinutes(interval);

            if (offset.CompareTo(DateTime.Now) <= 0)
            {
                return true;
            }

            return false;
        }
    }
}
