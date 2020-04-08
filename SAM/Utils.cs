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
using Win32Interop.WinHandles;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace SAM
{
    class Utils
    {
        #region dll imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        #endregion

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int VK_RETURN = 0x0D;
        public const int VK_TAB = 0x09;
        public const int VK_SPACE = 0x20;

        public static int API_KEY_LENGTH = 32;
        readonly static char[] specialChars = { '{', '}', '(', ')', '[', ']', '+', '^', '%', '~' };

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
                Console.WriteLine(nre.Message);
            }

            return registryValue;
        }

        public static void ClearAutoLoginUserKeyValues()
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
                localKey = localKey.OpenSubKey(@"Software\\Valve\\Steam", true);
                localKey.SetValue("AutoLoginUser", "", RegistryValueKind.String);
                localKey.SetValue("RememberPassword", 0, RegistryValueKind.DWord);
            }
            catch (NullReferenceException nre)
            {
                Console.WriteLine(nre.Message);
            }
        }

        public static void SetRememeberPasswordKeyValue(int value)
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
                localKey = localKey.OpenSubKey(@"Software\\Valve\\Steam", true);
                localKey.SetValue("RememberPassword", value, RegistryValueKind.DWord);
            }
            catch (NullReferenceException nre)
            {
                Console.WriteLine(nre.Message);
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
                        dynamic userJson = await Utils.GetSteamIdFromVanityUrl(url);

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

                    dynamic userJson = await Utils.GetUserInfoFromWebApiBySteamId(url);

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

        public static string GetSteamIdFromConfig(string userName)
        {
            dynamic steamId = null;

            try
            {
                string steamPath = new IniFile(SAMSettings.FILE_NAME).Read(SAMSettings.STEAM_PATH, SAMSettings.SECTION_STEAM);

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
                Console.WriteLine(ex.Message);
            }

            return Convert.ToString(steamId);
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
                catch (Exception m)
                {
                    MessageBox.Show(m.Message);
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
                    catch (Exception m)
                    {
                        MessageBox.Show(m.Message);
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
                catch (Exception m)
                {
                    MessageBox.Show(m.Message);
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
                    catch (Exception m)
                    {
                        MessageBox.Show(m.Message);
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

        public static WindowHandle GetSteamLoginWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
            wh.GetClassName().Equals("vguiPopupWindow") && 
            (wh.GetWindowText().Contains("Steam") && 
            !wh.GetWindowText().Contains("-") && 
            !wh.GetWindowText().Contains("—") && 
             wh.GetWindowText().Length > 5));
        }

        public static WindowHandle GetSteamGuardWindow()
        {
            // Also checking for vguiPopupWindow class name to avoid catching things like browser tabs.
            WindowHandle windowHandle = TopLevelWindowUtils.FindWindow(wh =>
            wh.GetClassName().Equals("vguiPopupWindow") &&
            (wh.GetWindowText().StartsWith("Steam Guard") ||
             wh.GetWindowText().StartsWith("Steam 令牌") ||
             wh.GetWindowText().StartsWith("Steam ガード")));
            return windowHandle;
        }

        public static WindowHandle GetSteamWarningWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
            wh.GetClassName().Equals("vguiPopupWindow") && 
            (wh.GetWindowText().StartsWith("Steam - ") || 
             wh.GetWindowText().StartsWith("Steam — ")));
        }

        public static Process WaitForSteamProcess(WindowHandle windowHandle)
        {
            Process process = null;

            // Wait for valid process to wait for input idle.
            Console.WriteLine("Waiting for it to be idle.");
            while (process == null)
            {
                int procId = 0;
                GetWindowThreadProcessId(windowHandle.RawPtr, out procId);

                // Wait for valid process id from handle.
                while (procId == 0)
                {
                    Thread.Sleep(10);
                    GetWindowThreadProcessId(windowHandle.RawPtr, out procId);
                }

                try
                {
                    process = Process.GetProcessById(procId);
                }
                catch
                {
                    process = null;
                }
            }

            return process;
        }

        /**
         * Because CapsLock is handled by system directly, thus sending
         * it to one particular window is invalid - a window could not
         * respond to CapsLock, only the system can.
         * 
         * For this reason, I break into a low-level API, which may cause
         * an inconsistency to the original `SendWait` method.
         * 
         * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-keybd_event
         */
        public static void SendCapsLockGlobally()
        {
            // Press key down
            keybd_event((byte)System.Windows.Forms.Keys.CapsLock, 0, 0, 0);
            // Press key up
            keybd_event((byte)System.Windows.Forms.Keys.CapsLock, 0, 0x2, 0);
        }

        public static void SendCharacter(IntPtr hwnd, VirtualInputMethod inputMethod, char c)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_CHAR, c, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_CHAR, (IntPtr)c, IntPtr.Zero);
                    break;

                default:
                    if (Utils.IsSpecialCharacter(c))
                    {
                        if (inputMethod == VirtualInputMethod.SendWait)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{" + c.ToString() + "}");
                        }
                        else
                        {
                            System.Windows.Forms.SendKeys.Send("{" + c.ToString() + "}");
                        }
                    }
                    else
                    {
                        if (inputMethod == VirtualInputMethod.SendWait)
                        {
                            System.Windows.Forms.SendKeys.SendWait(c.ToString());
                        }
                        else
                        {
                            System.Windows.Forms.SendKeys.Send(c.ToString());
                        }
                    }
                    break;
            }
        }

        public static void SendEnter(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_RETURN, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_RETURN, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_RETURN, IntPtr.Zero);
                    break;

                case VirtualInputMethod.SendWait:
                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                    break;
            }
        }

        public static void SendTab(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_TAB, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_TAB, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_TAB, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_TAB, IntPtr.Zero);
                    break;

                case VirtualInputMethod.SendWait:
                    System.Windows.Forms.SendKeys.SendWait("{TAB}");
                    break;
            }
        }

        public static void SendSpace(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_SPACE, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_SPACE, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_SPACE, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_SPACE, IntPtr.Zero);
                    break;

                case VirtualInputMethod.SendWait:
                    System.Windows.Forms.SendKeys.SendWait(" ");
                    break;
            }
        }

        public static void ClearSteamUserDataFolder(string steamPath, int sleepTime, int maxRetry)
        {
            WindowHandle steamLoginWindow = GetSteamLoginWindow();
            int waitCount = 0;

            while (steamLoginWindow.IsValid && waitCount < maxRetry)
            {
                Thread.Sleep(sleepTime);
                waitCount++;
            }

            string path = steamPath + "\\userdata";

            if (Directory.Exists(path))
            {
                Console.WriteLine("Deleting userdata files...");
                Directory.Delete(path, true);
                Console.WriteLine("userdata files deleted!");
            }
            else
            {
                Console.WriteLine("userdata directory not found.");
            }
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

        public static bool IsSpecialCharacter(char c)
        {
            foreach (char special in specialChars)
            {
                if (c.Equals(special))
                {
                    return true;
                }
            }

            return false;
        }

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
