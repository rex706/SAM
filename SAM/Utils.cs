using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using HtmlAgilityPack;
using System.Text;

namespace SAM
{
    class Utils
    {
        private static string apiKey = "API_KEY";

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
            var stream = new StreamReader(file);
            var ser = new XmlSerializer(typeof(List<Account>));
            object obj = ser.Deserialize(stream);
            stream.Close();
            return (List<Account>)obj;
        }

        public static List<Account> PasswordDeserialize(string file, string password)
        {
            string contents = File.ReadAllText(file);
            contents = StringCipher.Decrypt(contents, password);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));
            var ser = new XmlSerializer(typeof(List<Account>));
            object obj = ser.Deserialize(stream);
            stream.Close();
            return (List<Account>)obj;
        }

        public static void ImportAccountsFromList(List<Account> accounts)
        {
            try
            {
                MainWindow.encryptedAccounts = MainWindow.encryptedAccounts.Concat(accounts).ToList();
                Serialize(MainWindow.encryptedAccounts);
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

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    var tempAccounts = Deserialize(dialog.FileName);
                    MainWindow.encryptedAccounts = MainWindow.encryptedAccounts.Concat(tempAccounts).ToList();
                    Serialize(MainWindow.encryptedAccounts);
                    MessageBox.Show("Accounts imported!");
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static string GetSteamPathFromRegistry()
        {
            string registryValue = string.Empty;
            RegistryKey localKey = null;
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
            catch (NullReferenceException nre)
            {

            }
            return registryValue;
        }

        public static string CheckSteamPath()
        {
            var settingsFile = new IniFile("SAMSettings.ini");

            string steamPath = settingsFile.Read("Steam", "Settings");

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
                    Nullable<bool> result = dlg.ShowDialog();

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
            settingsFile.Write("Steam", steamPath, "Settings");

            return steamPath;
        }

        public static async Task<dynamic> GetUserInfoFromConfigAndWebApi(string userName)
        {
            dynamic userJson = null;
            dynamic steamId = null;

            try
            {
                string steamPath = new IniFile("SAMSettings.ini").Read("Steam", "Settings");

                // Attempt to find Steam Id from steam config.
                dynamic config = VdfConvert.Deserialize(File.ReadAllText(steamPath + "config\\config.vdf"));
                dynamic accounts = config.Value.Software.Valve.Steam.Accounts;

                VObject accountsObj = accounts;
                VToken value;

                accountsObj.TryGetValue(userName, out value);

                dynamic user = value;
                VValue userId = user.SteamID;
                steamId = userId.Value.ToString();
            }
            catch (Exception ex)
            {
                // Attempt to find Steam Id from web api.
                Uri vanityUri = new Uri("http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key=" + apiKey + "&vanityurl=" + userName);

                using (WebClient client = new WebClient())
                {
                    string vanityJson = await client.DownloadStringTaskAsync(vanityUri);
                    dynamic vanityValue = JValue.Parse(vanityJson);

                    steamId = vanityValue.response.steamid;
                }
            }

            if (steamId != null)
            {
                userJson = await GetUserInfoFromWebApiBySteamId(Convert.ToString(steamId));
            }

            return userJson;
        }

        public static async Task<dynamic> GetUserInfoFromWebApiBySteamId(string steamId)
        {
            dynamic userJson = null;
            try
            {
                Uri userUri = new Uri("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + apiKey + "&steamids=" + steamId);

                using (WebClient client = new WebClient())
                {
                    string userJsonString = await client.DownloadStringTaskAsync(userUri);
                    userJson = JValue.Parse(userJsonString);
                }
            }
            catch(Exception m)
            {
                //MessageBox.Show(m.Message);
            }

            return userJson;
        }

        public static string HtmlAviScrape(string profUrl)
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
                    HtmlDocument document = new HtmlWeb().Load(new Uri(profUrl));
                    return document.DocumentNode.Descendants().Where(n => n.HasClass("playerAvatarAutoSizeInner")).First().FirstChild.GetAttributeValue("src", null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return "";
        }
    }
}
