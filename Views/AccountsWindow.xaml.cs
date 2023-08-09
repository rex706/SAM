using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Win32Interop.WinHandles;
using System.Windows.Controls.Primitives;
using SAM.Core;
using MahApps.Metro.Controls;
using MahApps.Metro;
using System.Media;
using System.Windows.Input;

namespace SAM.Views
{
    /// <summary>
    /// Interaktionslogik für AccountsWindow.xaml
    /// </summary>
    public partial class AccountsWindow : MetroWindow
    {
        #region Globals

        public static List<Account> encryptedAccounts;
        private static List<Account> decryptedAccounts;
        private static Dictionary<int, Account> exportAccounts;
        private static Dictionary<int, Account> deleteAccounts;

        private static SAMSettings settings;

        private static List<Thread> loginThreads;
        private static List<System.Timers.Timer> timeoutTimers;

        private static readonly string updateCheckUrl = "https://raw.githubusercontent.com/rex706/SAM/master/latest.txt";
        private static readonly string repositoryUrl = "https://github.com/rex706/SAM";
        private static readonly string releasesUrl = repositoryUrl + "/releases";

        private static bool isLoadingSettings = true;
        private static bool firstLoad = true;

        private static readonly string dataFile = "info.dat";
        private static readonly string backupFile = dataFile + ".bak";
        private static string loadSource;

        // Keys are changed before releases/updates
        private static readonly string eKey = "PRIVATE_KEY";
        private static string ePassword = "";
        private static string account;

        private static List<string> globalParameters;

        private static double originalHeight;
        private static double originalWidth;
        private static Thickness initialAddButtonGridMargin;

        private static string AssemblyVer;

        private static bool exporting = false;
        private static bool deleting = false;
        private static bool loginAllSequence = false;
        private static bool loginAllCancelled = false;
        private static bool noReactLogin = false;

        private static Button holdingButton = null;
        private static bool dragging = false;
        private static System.Timers.Timer mouseHoldTimer;

        private static System.Timers.Timer autoReloadApiTimer;

        private static readonly int maxRetry = 2;

        // Resize animation variables
        private static System.Windows.Forms.Timer _Timer = new System.Windows.Forms.Timer();
        private int _Stop = 0;
        private double _RatioHeight;
        private double _RatioWidth;
        private double _Height;
        private double _Width;

        #endregion

        public AccountsWindow()
        {
            InitializeComponent();
            // If no settings file exists, create one and initialize values.
            if (!File.Exists(SAMSettings.FILE_NAME))
            {
                GenerateSettings();
            }

            LoadSettings();

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            BackgroundBorder.PreviewMouseLeftButtonDown += (s, e) => { DragMove(); };

            _Timer.Tick += new EventHandler(Timer_Tick);
            _Timer.Interval = 10;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Version number from assembly
            AssemblyVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var ver = new MenuItem();
            var newExistMenuItem = (MenuItem)FileMenu.Items[2];
            ver.Header = "v" + AssemblyVer;
            ver.IsEnabled = false;
            newExistMenuItem.Items.Add(ver);

            if (settings.User.CheckForUpdates)
            {
                UpdateResponse response = await UpdateHelper.CheckForUpdate(updateCheckUrl);

                switch (response)
                {
                    case UpdateResponse.Later:
                        ver.Header = "Update Available!";
                        ver.Click += Ver_Click;
                        ver.IsEnabled = true;
                        break;

                    case UpdateResponse.Update:

                        if (eKey == "PRIVATE_KEY")
                        {
                            MessageBoxResult result = MessageBox.Show(
                                "An update for SAM is available!\n\n" +
                                "Please pull the latest changes and rebuild.\n\n" +
                                "Do you understand?", 
                                "Update Available", MessageBoxButton.YesNo);

                            if (result == MessageBoxResult.No)
                            {
                                // TODO: wiki for #176
                                Process.Start("https://github.com/rex706/SAM/issues/176");
                            }

                            Close();
                            return;
                        }

                        await UpdateHelper.StartUpdate(updateCheckUrl, releasesUrl);

                        Close();
                        return;
                }
            }

            loginThreads = new List<Thread>();

            // Save New Button inital margin.
            initialAddButtonGridMargin = AddButtonGrid.Margin;

            // Save initial window height and width;
            originalHeight = Height;
            originalWidth = Width;

            // Load window with account buttons.
            RefreshWindow(dataFile);

            // Login to auto log account if enabled and Steam is not already open.
            Process[] SteamProc = Process.GetProcessesByName("Steam");

            if (SteamProc.Length == 0)
            {
                if (settings.User.LoginRecentAccount == true)
                    Login(settings.User.RecentAccountIndex);
                else if (settings.User.LoginSelectedAccount == true)
                    Login(settings.User.SelectedAccountIndex);
            }
        }

        private string VerifyAndSetPassword()
        {
            MessageBoxResult messageBoxResult = MessageBoxResult.No;

            while (messageBoxResult == MessageBoxResult.No)
            {
                var passwordDialog = new PasswordWindow();

                if (passwordDialog.ShowDialog() == true && passwordDialog.PasswordText != "")
                {
                    ePassword = passwordDialog.PasswordText;

                    return true.ToString();
                }
                else if (passwordDialog.PasswordText == "")
                {
                    messageBoxResult = MessageBox.Show("No password detected, are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }
            }

            return false.ToString();
        }

        private bool VerifyPassword()
        {
            MessageBoxResult messageBoxResult = MessageBoxResult.No;

            while (messageBoxResult == MessageBoxResult.No)
            {
                var passwordDialog = new PasswordWindow();

                if (passwordDialog.ShowDialog() == true && passwordDialog.PasswordText != "")
                {
                    try
                    {
                        encryptedAccounts = AccountUtils.PasswordDeserialize(dataFile, passwordDialog.PasswordText);
                        messageBoxResult = MessageBoxResult.None;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        messageBoxResult = MessageBox.Show("Invalid Password", "Invalid", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                        if (messageBoxResult == MessageBoxResult.Cancel)
                        {
                            return false;
                        }
                        else
                        {
                            return VerifyPassword();
                        }
                    }

                    return true;
                }
                else if (passwordDialog.PasswordText == "")
                {
                    messageBoxResult = MessageBox.Show("No password detected, are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }
            }

            return false;
        }

        private void GenerateSettings()
        {
            settings = new SAMSettings();

            settings.File.Write("Version", AssemblyVer, "System");

            foreach (KeyValuePair<string, string> entry in settings.KeyValuePairs)
            {
                settings.File.Write(entry.Key, settings.Default.KeyValuePairs[entry.Key].ToString(), entry.Value);
            }

            MessageBoxResult messageBoxResult = MessageBox.Show("Do you want to password protect SAM?", "Protect", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                settings.File.Write(SAMSettings.PASSWORD_PROTECT, VerifyAndSetPassword(), SAMSettings.SECTION_GENERAL);
            }
            else
            {
                settings.File.Write(SAMSettings.PASSWORD_PROTECT, false.ToString(), SAMSettings.SECTION_GENERAL);
            }
        }

        private void LoadSettings()
        {
            settings = new SAMSettings();

            isLoadingSettings = true;
            globalParameters = new List<string>();

            settings.HandleDeprecatedSettings();

            foreach (KeyValuePair<string, string> entry in settings.KeyValuePairs)
            {
                if (!settings.File.KeyExists(entry.Key, entry.Value))
                {
                    settings.File.Write(entry.Key, settings.Default.KeyValuePairs[entry.Key].ToString(), entry.Value);
                }
                else
                {
                    switch (entry.Key)
                    {
                        case SAMSettings.ACCOUNTS_PER_ROW:
                            string accountsPerRowString = settings.File.Read(SAMSettings.ACCOUNTS_PER_ROW, SAMSettings.SECTION_GENERAL);

                            if (!Regex.IsMatch(accountsPerRowString, @"^\d+$") || Int32.Parse(accountsPerRowString) < 1)
                            {
                                settings.File.Write(SAMSettings.ACCOUNTS_PER_ROW, settings.Default.AccountsPerRow.ToString(), SAMSettings.SECTION_GENERAL);
                                settings.User.AccountsPerRow = settings.Default.AccountsPerRow;
                            }

                            settings.User.AccountsPerRow = Int32.Parse(accountsPerRowString);
                            break;

                        case SAMSettings.SLEEP_TIME:
                            string sleepTimeString = settings.File.Read(SAMSettings.SLEEP_TIME, SAMSettings.SECTION_GENERAL);
                            float sleepTime = 0;

                            if (!Single.TryParse(sleepTimeString, out sleepTime) || sleepTime < 0 || sleepTime > 100)
                            {
                                settings.File.Write(SAMSettings.SLEEP_TIME, settings.Default.SleepTime.ToString(), SAMSettings.SECTION_GENERAL);
                                settings.User.SleepTime = settings.Default.SleepTime * 1000;
                            }
                            else
                            {
                                settings.User.SleepTime = (int)(sleepTime * 1000);
                            }
                            break;

                        case SAMSettings.START_MINIMIZED:
                            settings.User.StartMinimized = Convert.ToBoolean(settings.File.Read(SAMSettings.START_MINIMIZED, SAMSettings.SECTION_GENERAL));
                            if (settings.User.StartMinimized)
                            {
                                WindowState = WindowState.Minimized;
                            }
                            break;

                        case SAMSettings.BUTTON_SIZE:
                            string buttonSizeString = settings.File.Read(SAMSettings.BUTTON_SIZE, SAMSettings.SECTION_CUSTOMIZE);
                            int buttonSize = 0;

                            if (!Regex.IsMatch(buttonSizeString, @"^\d+$") || !Int32.TryParse(buttonSizeString, out buttonSize) || buttonSize < 50 || buttonSize > 200)
                            {
                                settings.File.Write(SAMSettings.BUTTON_SIZE, "100", SAMSettings.SECTION_CUSTOMIZE);
                                settings.User.ButtonSize = 100;
                            }
                            else
                            {
                                settings.User.ButtonSize = buttonSize;
                            }
                            break;

                        case SAMSettings.INPUT_METHOD:
                            settings.User.VirtualInputMethod = (VirtualInputMethod)Enum.Parse(typeof(VirtualInputMethod), settings.File.Read(SAMSettings.INPUT_METHOD, SAMSettings.SECTION_AUTOLOG));
                            break;

                        default:
                            switch (Type.GetTypeCode(settings.User.KeyValuePairs[entry.Key].GetType()))
                            {
                                case TypeCode.Boolean:
                                    settings.User.KeyValuePairs[entry.Key] = Convert.ToBoolean(settings.File.Read(entry.Key, entry.Value));
                                    if (entry.Value.Equals(SAMSettings.SECTION_PARAMETERS) && (bool)settings.User.KeyValuePairs[entry.Key] == true && !entry.Key.StartsWith("custom"))
                                    {
                                        globalParameters.Add("-" + entry.Key);
                                    }
                                    break;

                                case TypeCode.Int32:
                                    settings.User.KeyValuePairs[entry.Key] = Convert.ToInt32(settings.File.Read(entry.Key, entry.Value));
                                    break;

                                case TypeCode.Double:
                                    settings.User.KeyValuePairs[entry.Key] = Convert.ToDouble(settings.File.Read(entry.Key, entry.Value));
                                    break;

                                default:
                                    settings.User.KeyValuePairs[entry.Key] = settings.File.Read(entry.Key, entry.Value);
                                    break;
                            }
                            break;
                    }
                }
            }

            //Load and validate saved window location.
            if (settings.File.KeyExists(SAMSettings.WINDOW_LEFT, SAMSettings.SECTION_LOCATION) && settings.File.KeyExists(SAMSettings.WINDOW_TOP, SAMSettings.SECTION_LOCATION))
            {
                Left = Double.Parse(settings.File.Read(SAMSettings.WINDOW_LEFT, SAMSettings.SECTION_LOCATION));
                Top = Double.Parse(settings.File.Read(SAMSettings.WINDOW_TOP, SAMSettings.SECTION_LOCATION));
                SetWindowSettingsIntoScreenArea();
            }
            else
            {
                SetWindowToCenter();
            }

            if (settings.User.ListView == true)
            {
                AddButtonGrid.Visibility = Visibility.Collapsed;

                Height = settings.User.ListViewHeight;
                Width = settings.User.ListViewWidth;

                ResizeMode = ResizeMode.CanResize;

                foreach (DataGridColumn column in AccountsDataGrid.Columns)
                {
                    column.DisplayIndex = (int)settings.User.KeyValuePairs[settings.ListViewColumns[column.Header.ToString()]];
                }

                AccountsDataGrid.ItemsSource = encryptedAccounts;
                AccountsDataGrid.Visibility = Visibility.Visible;
            }

            if (settings.User.AutoReloadEnabled)
            {
                int interval = settings.User.AutoReloadInterval;

                if (settings.User.LastAutoReload.HasValue == true)
                {
                    double minutesSince = (DateTime.Now - settings.User.LastAutoReload.Value).TotalMinutes;

                    if (minutesSince < interval)
                    {
                        interval -= Convert.ToInt32(minutesSince);
                    }
                }

                if (interval <= 0)
                {
                    interval = settings.User.AutoReloadInterval;
                }

                autoReloadApiTimer = new System.Timers.Timer();
                autoReloadApiTimer.Elapsed += AutoReloadApiTimer_Elapsed;
                autoReloadApiTimer.Interval = 60000 * interval;
                autoReloadApiTimer.Start();
            }
            else
            {
                if (autoReloadApiTimer != null)
                {
                    autoReloadApiTimer.Stop();
                    autoReloadApiTimer.Dispose();
                }
            }

            // Set user's theme settings.
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(settings.User.Accent), ThemeManager.GetAppTheme(settings.User.Theme));

            // Apply theme settings for extended toolkit and tabItem brushes.
            if (settings.User.Theme == SAMSettings.DARK_THEME)
            {
                Application.Current.Resources["xctkForegoundBrush"] = Brushes.White;
                Application.Current.Resources["xctkColorPickerBackground"] = new BrushConverter().ConvertFromString("#303030");
                Application.Current.Resources["GrayNormalBrush"] = Brushes.White;
            }
            else
            {
                Application.Current.Resources["xctkForegoundBrush"] = Brushes.Black;
                Application.Current.Resources["xctkColorPickerBackground"] = Brushes.White;
                Application.Current.Resources["GrayNormalBrush"] = Brushes.Black;
            }

            if (settings.User.PasswordProtect && ePassword.Length == 0)
            {
                VerifyAndSetPassword();
            }

            AccountUtils.CheckSteamPath();
            settings.File.Write("Version", AssemblyVer, "System");
            isLoadingSettings = false;
        }

        private void AutoReloadApiTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ReloadAccountsAsync();
            });
        }

        public void RefreshWindow(string file)
        {
            loadSource = file;

            decryptedAccounts = new List<Account>();

            buttonGrid.Children.Clear();

            TaskBarIconLoginContextMenu.Items.Clear();
            TaskBarIconLoginContextMenu.IsEnabled = false;

            AddButtonGrid.Height = settings.User.ButtonSize;
            AddButtonGrid.Width = settings.User.ButtonSize;

            // Check if info.dat exists
            if (File.Exists(file))
            {
                MessageBoxResult messageBoxResult = MessageBoxResult.OK;

                // Deserialize file
                if (ePassword.Length > 0)
                {
                    while (messageBoxResult == MessageBoxResult.OK)
                    {
                        try
                        {
                            encryptedAccounts = AccountUtils.PasswordDeserialize(file, ePassword);
                            messageBoxResult = MessageBoxResult.None;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            messageBoxResult = MessageBox.Show("Invalid Password", "Invalid", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                            if (messageBoxResult == MessageBoxResult.Cancel)
                            {
                                Close();
                            }
                            else
                            {
                                VerifyAndSetPassword();
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        encryptedAccounts = AccountUtils.Deserialize(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);

                        if (file == backupFile)
                        {
                            MessageBox.Show(e.Message, "Deserialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Close();
                        }

                        if (File.Exists(backupFile))
                        {
                            messageBoxResult = MessageBox.Show("An error has occured attempting to deserialize your .dat file.\n\n" +
                                "Would you like to try a detected backup?", "Deserialization Error", MessageBoxButton.YesNo, MessageBoxImage.Error);

                            if (messageBoxResult == MessageBoxResult.No)
                            {
                                Close();
                            }
                            else
                            {
                                RefreshWindow(backupFile);
                            }
                        }

                        return;
                    }
                }

                PostDeserializedRefresh(true);
            }
            else
            {
                encryptedAccounts = new List<Account>();
                SerializeAccounts();
            }

            AccountsDataGrid.ItemsSource = encryptedAccounts;

            if (firstLoad == true && settings.User.AutoReloadEnabled == true && AccountUtils.ShouldAutoReload(settings.User.LastAutoReload, settings.User.AutoReloadInterval))
            {
                firstLoad = false;
                ReloadAccountsAsync();
            }
        }

        private async Task ReloadAccount(Account account)
        {
            dynamic userJson;
            if (account.SteamId != null && account.SteamId.Length > 0)
            {
                userJson = await AccountUtils.GetUserInfoFromWebApiBySteamId(account.SteamId);
            }
            else
            {
                userJson = await AccountUtils.GetUserInfoFromConfigAndWebApi(account.Name);
            }

            if (userJson != null)
            {
                account.ProfUrl = userJson.response.players[0].profileurl;
                account.AviUrl = userJson.response.players[0].avatarfull;
                account.SteamId = userJson.response.players[0].steamid;
            }
            else
            {
                account.AviUrl = await AccountUtils.HtmlAviScrapeAsync(account.ProfUrl);
            }

            if (account.SteamId != null && account.SteamId.Length > 0 && AccountUtils.ApiKeyExists())
            {
                dynamic userBanJson = await AccountUtils.GetPlayerBansFromWebApi(account.SteamId);

                if (userBanJson != null)
                {
                    account.CommunityBanned = Convert.ToBoolean(userBanJson.CommunityBanned);
                    account.VACBanned = Convert.ToBoolean(userBanJson.VACBanned);
                    account.NumberOfVACBans = Convert.ToInt32(userBanJson.NumberOfVACBans);
                    account.NumberOfGameBans = Convert.ToInt32(userBanJson.NumberOfGameBans);
                    account.DaysSinceLastBan = Convert.ToInt32(userBanJson.DaysSinceLastBan);
                    account.EconomyBan = userBanJson.EconomyBan;
                }
            }
        }

        public async Task ReloadAccountsAsync()
        {
            Title = "SAM | Loading";

            List<string> steamIds = new List<string>();

            foreach (Account account in encryptedAccounts)
            {
                if (account.SteamId != null && account.SteamId.Length > 0)
                {
                    steamIds.Add(account.SteamId);
                }
                else
                {
                    string steamId = AccountUtils.GetSteamIdFromConfig(account.Name);
                    if (steamId != null && steamId.Length > 0)
                    {
                        account.SteamId = steamId;
                        steamIds.Add(steamId);
                    }
                    else if (account.ProfUrl != null && account.ProfUrl.Length > 0)
                    {
                        // Try to get steamId from profile URL via web API.

                        dynamic steamIdFromProfileUrl = await AccountUtils.GetSteamIdFromProfileUrl(account.ProfUrl);

                        if (steamIdFromProfileUrl != null)
                        {
                            account.SteamId = steamIdFromProfileUrl;
                            steamIds.Add(steamIdFromProfileUrl);
                        }

                        Thread.Sleep(new Random().Next(10, 16));
                    }
                }
            }

            List<dynamic> userInfos = await AccountUtils.GetUserInfosFromWepApi(new List<string>(steamIds));

            foreach (dynamic userInfosJson in userInfos)
            {
                foreach (dynamic userInfoJson in userInfosJson.response.players)
                {
                    Account account = encryptedAccounts.Find(a => a.SteamId == Convert.ToString(userInfoJson.steamid));

                    if (account != null)
                    {
                        account.ProfUrl = userInfoJson.profileurl;
                        account.AviUrl = userInfoJson.avatarfull;
                    }
                }
            }

            if (AccountUtils.ApiKeyExists())
            {
                List<dynamic> userBans = await AccountUtils.GetPlayerBansFromWebApi(new List<string>(steamIds));

                foreach (dynamic userBansJson in userBans)
                {
                    foreach (dynamic userBanJson in userBansJson.players)
                    {
                        Account account = encryptedAccounts.Find(a => a.SteamId == Convert.ToString(userBanJson.SteamId));

                        if (account != null)
                        {
                            account.CommunityBanned = Convert.ToBoolean(userBanJson.CommunityBanned);
                            account.VACBanned = Convert.ToBoolean(userBanJson.VACBanned);
                            account.NumberOfVACBans = Convert.ToInt32(userBanJson.NumberOfVACBans);
                            account.NumberOfGameBans = Convert.ToInt32(userBanJson.NumberOfGameBans);
                            account.DaysSinceLastBan = Convert.ToInt32(userBanJson.DaysSinceLastBan);
                            account.EconomyBan = userBanJson.EconomyBan;
                        }
                    }
                }
            }

            settings.File.Write(SAMSettings.LAST_AUTO_RELOAD, DateTime.Now.ToString(), SAMSettings.SECTION_STEAM);
            settings.User.LastAutoReload = DateTime.Now;

            SerializeAccounts();

            Title = "SAM";
        }

        private void PostDeserializedRefresh(bool seedAcc)
        {
            SetMainScrollViewerBarsVisibility(ScrollBarVisibility.Hidden);

            // Dispose and reinitialize timers each time grid is refreshed as to not clog up more resources than necessary. 
            if (timeoutTimers != null)
            {
                foreach (System.Timers.Timer timer in timeoutTimers)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            }

            timeoutTimers = new List<System.Timers.Timer>();

            if (encryptedAccounts != null)
            {
                foreach (var account in encryptedAccounts)
                {
                    string tempPass = StringCipher.Decrypt(account.Password, eKey);

                    if (seedAcc)
                    {
                        string temp2fa = null;
                        string steamId = null;

                        if (account.SharedSecret != null && account.SharedSecret.Length > 0)
                        {
                            temp2fa = StringCipher.Decrypt(account.SharedSecret, eKey);
                        }
                        if (account.SteamId != null && account.SteamId.Length > 0)
                        {
                            steamId = account.SteamId;
                        }

                        decryptedAccounts.Add(new Account() {
                            Name = account.Name,
                            Alias = account.Alias,
                            Password = tempPass,
                            SharedSecret = temp2fa,
                            ProfUrl = account.ProfUrl,
                            AviUrl = account.AviUrl,
                            SteamId = steamId,
                            Timeout = account.Timeout,
                            Parameters = account.Parameters,
                            Description = account.Description,
                            FriendsLoginStatus = account.FriendsLoginStatus
                        });
                    }
                }

                if (settings.User.ListView == true)
                {
                    SetMainScrollViewerBarsVisibility(ScrollBarVisibility.Auto);

                    for (int i = 0; i < encryptedAccounts.Count; i++)
                    {
                        Account account = encryptedAccounts[i];

                        int index = i;

                        TaskBarIconLoginContextMenu.IsEnabled = true;
                        TaskBarIconLoginContextMenu.Items.Add(GenerateTaskBarMenuItem(index, account));

                        if (AccountUtils.AccountHasActiveTimeout(account))
                        {
                            // Set up timer event to update timeout label
                            var timeLeft = account.Timeout - DateTime.Now;

                            System.Timers.Timer timeoutTimer = new System.Timers.Timer();
                            timeoutTimers.Add(timeoutTimer);

                            timeoutTimer.Elapsed += delegate
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    TimeoutTimer_Tick(index, timeoutTimer);
                                });
                            };
                            timeoutTimer.Interval = 1000;
                            timeoutTimer.Enabled = true;
                        }
                    }
                }
                else
                {
                    AccountsDataGrid.Visibility = Visibility.Collapsed;
                    AddButtonGrid.Visibility = Visibility.Visible;

                    int bCounter = 0;
                    int xCounter = 0;
                    int yCounter = 0;

                    int buttonOffset = settings.User.ButtonSize + 5;

                    // Create new button and textblock for each account
                    foreach (var account in encryptedAccounts)
                    {
                        Grid accountButtonGrid = new Grid();

                        Button accountButton = new Button();
                        TextBlock accountText = new TextBlock();
                        TextBlock timeoutTextBlock = new TextBlock();

                        Border accountImage = new Border();

                        accountButton.Style = (Style)Resources["SAMButtonStyle"];
                        accountButton.Tag = bCounter.ToString();

                        if (account.Alias != null && account.Alias.Length > 0)
                        {
                            accountText.Text = account.Alias;
                        }
                        else
                        {
                            accountText.Text = account.Name;
                        }

                        // If there is a description, set up tooltip.
                        if (account.Description != null && account.Description.Length > 0)
                        {
                            accountButton.ToolTip = account.Description;
                        }

                        accountButtonGrid.HorizontalAlignment = HorizontalAlignment.Left;
                        accountButtonGrid.VerticalAlignment = VerticalAlignment.Top;
                        accountButtonGrid.Margin = new Thickness(xCounter * buttonOffset, yCounter * buttonOffset, 0, 0);

                        accountButton.Height = settings.User.ButtonSize;
                        accountButton.Width = settings.User.ButtonSize;
                        accountButton.BorderBrush = null;
                        accountButton.HorizontalAlignment = HorizontalAlignment.Center;
                        accountButton.VerticalAlignment = VerticalAlignment.Center;
                        accountButton.Background = Brushes.Transparent;

                        accountText.Width = settings.User.ButtonSize;
                        if (settings.User.ButtonFontSize > 0)
                        {
                            accountText.FontSize = settings.User.ButtonFontSize;
                        }
                        else
                        {
                            accountText.FontSize = settings.User.ButtonSize / 8;
                        }

                        accountText.HorizontalAlignment = HorizontalAlignment.Center;
                        accountText.VerticalAlignment = VerticalAlignment.Bottom;
                        accountText.Margin = new Thickness(0, 0, 0, 7);
                        accountText.Padding = new Thickness(0, 0, 0, 1);
                        accountText.TextAlignment = TextAlignment.Center;
                        accountText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.User.BannerFontColor));
                        accountText.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.User.ButtonBannerColor));
                        accountText.Visibility = Visibility.Collapsed;

                        timeoutTextBlock.Width = settings.User.ButtonSize;
                        timeoutTextBlock.FontSize = settings.User.ButtonSize / 8;
                        timeoutTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
                        timeoutTextBlock.VerticalAlignment = VerticalAlignment.Center;
                        timeoutTextBlock.Padding = new Thickness(0, 0, 0, 1);
                        timeoutTextBlock.TextAlignment = TextAlignment.Center;
                        timeoutTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        timeoutTextBlock.Background = new SolidColorBrush(new Color { A = 128, R = 255, G = 0, B = 0 });

                        accountImage.Height = settings.User.ButtonSize;
                        accountImage.Width = settings.User.ButtonSize;
                        accountImage.HorizontalAlignment = HorizontalAlignment.Center;
                        accountImage.VerticalAlignment = VerticalAlignment.Center;
                        accountImage.CornerRadius = new CornerRadius(3);

                        if (account.ProfUrl == "" || account.AviUrl == null || account.AviUrl == "" || account.AviUrl == " ")
                        {
                            accountImage.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.User.ButtonColor));
                            accountButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.User.ButtonFontColor));
                            timeoutTextBlock.Margin = new Thickness(0, 0, 0, 50);

                            if (account.Alias != null && account.Alias.Length > 0)
                            {
                                accountButton.Content = account.Alias;
                            }
                            else
                            {
                                accountButton.Content = account.Name;
                            }
                        }
                        else
                        {
                            try
                            {
                                ImageBrush imageBrush = new ImageBrush();
                                BitmapImage image1 = new BitmapImage(new Uri(account.AviUrl));
                                imageBrush.ImageSource = image1;
                                accountImage.Background = imageBrush;
                            }
                            catch (Exception m)
                            {
                                // Probably no internet connection or avatar url is bad.
                                Console.WriteLine("Error: " + m.Message);

                                accountImage.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.User.ButtonColor));
                                accountButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.User.ButtonFontColor));
                                timeoutTextBlock.Margin = new Thickness(0, 0, 0, 50);

                                if (account.Alias != null && account.Alias.Length > 0)
                                {
                                    accountButton.Content = account.Alias;
                                }
                                else
                                {
                                    accountButton.Content = account.Name;
                                }
                            }
                        }

                        accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                        accountButton.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(AccountButton_MouseDown);
                        accountButton.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(AccountButton_MouseUp);
                        //accountButton.PreviewMouseMove += new MouseEventHandler(AccountButton_MouseMove);
                        accountButton.MouseLeave += new MouseEventHandler(AccountButton_MouseLeave);
                        accountButton.MouseEnter += delegate { AccountButton_MouseEnter(accountButton, accountText); };
                        accountButton.MouseLeave += delegate { AccountButton_MouseLeave(accountButton, accountText); };

                        accountButtonGrid.Children.Add(accountImage);

                        int buttonIndex = Int32.Parse(accountButton.Tag.ToString());

                        if (AccountUtils.AccountHasActiveTimeout(account))
                        {
                            // Set up timer event to update timeout label
                            var timeLeft = account.Timeout - DateTime.Now;

                            System.Timers.Timer timeoutTimer = new System.Timers.Timer();
                            timeoutTimers.Add(timeoutTimer);

                            timeoutTimer.Elapsed += delegate
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    TimeoutTimer_Tick(buttonIndex, timeoutTextBlock, timeoutTimer);
                                });
                            };
                            timeoutTimer.Interval = 1000;
                            timeoutTimer.Enabled = true;
                            timeoutTextBlock.Text = AccountUtils.FormatTimespanString(timeLeft.Value);
                            timeoutTextBlock.Visibility = Visibility.Visible;

                            accountButtonGrid.Children.Add(timeoutTextBlock);
                        }

                        accountButtonGrid.Children.Add(accountText);
                        accountButtonGrid.Children.Add(accountButton);

                        if (settings.User.HideBanIcons == false && (account.NumberOfVACBans > 0 || account.NumberOfGameBans > 0))
                        {
                            Image banInfoImage = new Image();

                            banInfoImage.HorizontalAlignment = HorizontalAlignment.Left;
                            banInfoImage.VerticalAlignment = VerticalAlignment.Top;
                            banInfoImage.Height = 14;
                            banInfoImage.Width = 14;
                            banInfoImage.Margin = new Thickness(10, 10, 10, 10);
                            banInfoImage.Source = new BitmapImage(new Uri(@"\Resources\error.png", UriKind.RelativeOrAbsolute));

                            banInfoImage.ToolTip = "VAC Bans: " + account.NumberOfVACBans +
                                "\nGame Bans: " + account.NumberOfGameBans +
                                "\nCommunity Banned: " + account.CommunityBanned +
                                "\nEconomy Ban: " + account.EconomyBan +
                                "\nDays Since Last Ban: " + account.DaysSinceLastBan;

                            accountButtonGrid.Children.Add(banInfoImage);
                        }

                        accountButton.ContextMenu = GenerateAccountContextMenu(account, buttonIndex);
                        accountButton.ContextMenuOpening += new ContextMenuEventHandler(ContextMenu_ContextMenuOpening);

                        buttonGrid.Children.Add(accountButtonGrid);

                        TaskBarIconLoginContextMenu.IsEnabled = true;
                        TaskBarIconLoginContextMenu.Items.Add(GenerateTaskBarMenuItem(bCounter, account));

                        bCounter++;
                        xCounter++;

                        if (bCounter % settings.User.AccountsPerRow == 0 && (!settings.User.HideAddButton || (settings.User.HideAddButton && bCounter != encryptedAccounts.Count)))
                        {
                            yCounter++;
                            xCounter = 0;
                        }
                    }

                    if (bCounter > 0)
                    {
                        // Adjust window size and info positions
                        int xVal = settings.User.AccountsPerRow;

                        if (settings.User.HideAddButton)
                        {
                            AddButtonGrid.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            AddButtonGrid.Visibility = Visibility.Visible;
                        }

                        if (yCounter == 0 && !settings.User.HideAddButton)
                        {
                            xVal = xCounter + 1;
                        }
                        else if (yCounter == 0)
                        {
                            xVal = xCounter;
                        }

                        int newHeight = (buttonOffset * (yCounter + 1)) + 57;
                        int newWidth = (buttonOffset * xVal) + 7;

                        Resize(newHeight, newWidth);

                        // Adjust new account and export/delete buttons
                        AddButtonGrid.HorizontalAlignment = HorizontalAlignment.Left;
                        AddButtonGrid.VerticalAlignment = VerticalAlignment.Top;
                        AddButtonGrid.Margin = new Thickness((xCounter * buttonOffset) + 5, (yCounter * buttonOffset) + 25, 0, 0);
                    }
                    else
                    {
                        // Reset New Button position.
                        Resize(180, 138);

                        AddButtonGrid.HorizontalAlignment = HorizontalAlignment.Center;
                        AddButtonGrid.VerticalAlignment = VerticalAlignment.Center;
                        AddButtonGrid.Margin = initialAddButtonGridMargin;
                        ResizeMode = ResizeMode.CanMinimize;
                    }
                }
            }
        }

        private MenuItem GenerateTaskBarMenuItem(int index, Account account)
        {
            var taskBarIconLoginItem = new MenuItem();
            taskBarIconLoginItem.Tag = index;
            taskBarIconLoginItem.Click += new RoutedEventHandler(TaskbarIconLoginItem_Click);

            if (account.Alias != null && account.Alias.Length > 0)
            {
                taskBarIconLoginItem.Header = account.Alias;
            }
            else
            {
                taskBarIconLoginItem.Header = account.Name;
            }

            return taskBarIconLoginItem;
        }

        private ContextMenu GenerateAccountContextMenu(Account account, int index)
        {
            ContextMenu accountContext = new ContextMenu();

            var deleteItem = new MenuItem();
            var editItem = new MenuItem();
            var exportItem = new MenuItem();
            var reloadItem = new MenuItem();

            var setTimeoutItem = new MenuItem();

            var thirtyMinuteTimeoutItem = new MenuItem();
            var twoHourTimeoutItem = new MenuItem();
            var twentyOneHourTimeoutItem = new MenuItem();
            var twentyFourHourTimeoutItem = new MenuItem();
            var sevenDayTimeoutItem = new MenuItem();
            var customTimeoutItem = new MenuItem();

            thirtyMinuteTimeoutItem.Header = "30 Minutes";
            twoHourTimeoutItem.Header = "2 Hours";
            twentyOneHourTimeoutItem.Header = "21 Hours";
            twentyFourHourTimeoutItem.Header = "24 Hours";
            sevenDayTimeoutItem.Header = "7 Days";
            customTimeoutItem.Header = "Custom";

            setTimeoutItem.Items.Add(thirtyMinuteTimeoutItem);
            setTimeoutItem.Items.Add(twoHourTimeoutItem);
            setTimeoutItem.Items.Add(twentyOneHourTimeoutItem);
            setTimeoutItem.Items.Add(twentyFourHourTimeoutItem);
            setTimeoutItem.Items.Add(sevenDayTimeoutItem);
            setTimeoutItem.Items.Add(customTimeoutItem);

            var clearTimeoutItem = new MenuItem();
            var copyMenuItem = new MenuItem();
            var copyUsernameItem = new MenuItem();
            var copyPasswordItem = new MenuItem();
            var copyProfileUrlItem = new MenuItem();
            var copyMFATokenItem = new MenuItem();

            if (!AccountUtils.AccountHasActiveTimeout(account))
            {
                clearTimeoutItem.IsEnabled = false;
            }

            deleteItem.Header = "Delete";
            editItem.Header = "Edit";
            exportItem.Header = "Export";
            reloadItem.Header = "Reload";
            setTimeoutItem.Header = "Set Timeout";
            clearTimeoutItem.Header = "Clear Timeout";
            copyMenuItem.Header = "Copy";
            copyUsernameItem.Header = "Username";
            copyPasswordItem.Header = "Password";
            copyProfileUrlItem.Header = "Profile URL";
            copyMFATokenItem.Header = "2FA/MFA Token";

            deleteItem.Click += delegate { DeleteEntry(index); };
            editItem.Click += delegate { EditEntryAsync(index); };
            exportItem.Click += delegate { ExportAccount(index); };
            reloadItem.Click += async delegate { await ReloadAccount_ClickAsync(index); };
            thirtyMinuteTimeoutItem.Click += delegate { AccountButtonSetTimeout_Click(index, DateTime.Now.AddMinutes(30)); };
            twoHourTimeoutItem.Click += delegate { AccountButtonSetTimeout_Click(index, DateTime.Now.AddHours(2)); };
            twentyOneHourTimeoutItem.Click += delegate { AccountButtonSetTimeout_Click(index, DateTime.Now.AddHours(21)); };
            twentyFourHourTimeoutItem.Click += delegate { AccountButtonSetTimeout_Click(index, DateTime.Now.AddDays(1)); };
            sevenDayTimeoutItem.Click += delegate { AccountButtonSetTimeout_Click(index, DateTime.Now.AddDays(7)); };
            customTimeoutItem.Click += delegate { AccountButtonSetCustomTimeout_Click(index); };
            clearTimeoutItem.Click += delegate { AccountButtonClearTimeout_Click(index); };
            copyUsernameItem.Click += delegate { CopyUsernameToClipboard(index); };
            copyPasswordItem.Click += delegate { CopyPasswordToClipboard(index); };
            copyProfileUrlItem.Click += delegate { CopyProfileUrlToClipboard(index); };

            accountContext.Items.Add(editItem);
            accountContext.Items.Add(deleteItem);
            accountContext.Items.Add(exportItem);
            accountContext.Items.Add(reloadItem);
            accountContext.Items.Add(setTimeoutItem);
            accountContext.Items.Add(clearTimeoutItem);

            copyMenuItem.Items.Add(copyUsernameItem);
            copyMenuItem.Items.Add(copyPasswordItem);
            copyMenuItem.Items.Add(copyProfileUrlItem);
            if (decryptedAccounts[index]?.SharedSecret != null && decryptedAccounts[index].SharedSecret.Length > 0)
                copyMFATokenItem.Click += delegate { Copy2FA(index); };
            else
                copyMFATokenItem.IsEnabled = false;

            copyMenuItem.Items.Add(copyMFATokenItem);

            accountContext.Items.Add(copyMenuItem);

            return accountContext;
        }

        private ContextMenu GenerateAltActionContextMenu(string altActionType)
        {
            ContextMenu contextMenu = new ContextMenu();
            var actionMenuItem = new MenuItem();

            if (altActionType == AltActionType.DELETING)
            {
                actionMenuItem.Header = "Delete Selected";
                actionMenuItem.Click += delegate { DeleteSelectedAccounts(); };
            }
            else if (altActionType == AltActionType.EXPORTING)
            {
                actionMenuItem.Header = "Export Selected";
                actionMenuItem.Click += delegate { ExportSelectedAccounts(); };
            }

            var cancelMenuItem = new MenuItem();
            cancelMenuItem.Header = "Cancel";
            cancelMenuItem.Click += delegate { ResetFromExportOrDelete(); };

            contextMenu.Items.Add(actionMenuItem);
            contextMenu.Items.Add(cancelMenuItem);

            return contextMenu;
        }

        private async void AddAccount()
        {
            // User entered info
            var dialog = new AccountInfoDialog();

            if (dialog.ShowDialog() == true && dialog.AccountText != "" && dialog.PasswordText != "")
            {
                account = dialog.AccountText;
                string password = dialog.PasswordText;
                string sharedSecret = dialog.SharedSecretText;

                string aviUrl;
                if (dialog.AviText != null && dialog.AviText.Length > 1)
                {
                    aviUrl = dialog.AviText;
                }
                else
                {
                    aviUrl = await AccountUtils.HtmlAviScrapeAsync(dialog.UrlText);
                }

                string steamId = dialog.SteamId;

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settings.File.Write(SAMSettings.SELECTED_ACCOUNT_INDEX, (encryptedAccounts.Count).ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.File.Write(SAMSettings.LOGIN_SELECTED_ACCOUNT, true.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.File.Write(SAMSettings.LOGIN_RECENT_ACCOUNT, false.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.User.LoginSelectedAccount = true;
                    settings.User.LoginRecentAccount = false;
                    settings.User.SelectedAccountIndex = encryptedAccounts.Count;
                }

                try
                {
                    Account newAccount = new Account() {
                        Name = dialog.AccountText,
                        Alias = dialog.AliasText,
                        Password = StringCipher.Encrypt(password, eKey),
                        SharedSecret = StringCipher.Encrypt(sharedSecret, eKey),
                        ProfUrl = dialog.UrlText,
                        AviUrl = aviUrl,
                        SteamId = steamId,
                        Parameters = dialog.ParametersText,
                        Description = dialog.DescriptionText,
                        FriendsLoginStatus = dialog.FriendsLoginStatus
                    };

                    await ReloadAccount(newAccount);

                    encryptedAccounts.Add(newAccount);
                    SerializeAccounts();
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    var itemToRemove = encryptedAccounts.Single(r => r.Name == dialog.AccountText);
                    encryptedAccounts.Remove(itemToRemove);

                    SerializeAccounts();
                    AddAccount();
                }
            }
        }

        private async Task EditEntryAsync(int index)
        {
            Account account = decryptedAccounts[index];

            var dialog = new AccountInfoDialog
            {
                AccountText = account.Name,
                AliasText = account.Alias,
                PasswordText = account.Password,
                SharedSecretText = account.SharedSecret,
                UrlText = account.ProfUrl,
                SteamId = account.SteamId,
                ParametersText = account.Parameters,
                DescriptionText = account.Description,
                FriendsLoginStatus = account.FriendsLoginStatus
            };

            // Reload slected boolean
            settings.User.LoginSelectedAccount = settings.File.Read(SAMSettings.LOGIN_SELECTED_ACCOUNT, SAMSettings.SECTION_AUTOLOG) == true.ToString();

            if (settings.User.LoginSelectedAccount == true && settings.User.SelectedAccountIndex == index)
                dialog.autoLogCheckBox.IsChecked = true;

            if (dialog.ShowDialog() == true)
            {
                string aviUrl;
                if (dialog.AviText != null && dialog.AviText.Length > 1)
                {
                    aviUrl = dialog.AviText;
                }
                else
                {
                    aviUrl = await AccountUtils.HtmlAviScrapeAsync(dialog.UrlText);
                }

                string steamId = dialog.SteamId;

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settings.File.Write(SAMSettings.SELECTED_ACCOUNT_INDEX, index.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.File.Write(SAMSettings.LOGIN_SELECTED_ACCOUNT, true.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.File.Write(SAMSettings.LOGIN_RECENT_ACCOUNT, false.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.User.LoginSelectedAccount = true;
                    settings.User.LoginRecentAccount = false;
                    settings.User.SelectedAccountIndex = index;
                }
                else
                {
                    settings.File.Write(SAMSettings.SELECTED_ACCOUNT_INDEX, "-1", SAMSettings.SECTION_AUTOLOG);
                    settings.File.Write(SAMSettings.LOGIN_SELECTED_ACCOUNT, false.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.User.LoginSelectedAccount = false;
                    settings.User.SelectedAccountIndex = -1;
                }

                try
                {
                    encryptedAccounts[index].Name = dialog.AccountText;
                    encryptedAccounts[index].Alias = dialog.AliasText;
                    encryptedAccounts[index].Password = StringCipher.Encrypt(dialog.PasswordText, eKey);
                    encryptedAccounts[index].SharedSecret = StringCipher.Encrypt(dialog.SharedSecretText, eKey);
                    encryptedAccounts[index].ProfUrl = dialog.UrlText;
                    encryptedAccounts[index].AviUrl = aviUrl;
                    encryptedAccounts[index].SteamId = dialog.SteamId;
                    encryptedAccounts[index].Parameters = dialog.ParametersText;
                    encryptedAccounts[index].Description = dialog.DescriptionText;
                    encryptedAccounts[index].FriendsLoginStatus = dialog.FriendsLoginStatus;

                    SerializeAccounts();
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    EditEntryAsync(index);
                }
            }
        }

        private void DeleteEntry(int index)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this entry?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (result == MessageBoxResult.Yes)
            {
                encryptedAccounts.RemoveAt(index);
                SerializeAccounts();
            }
        }

        private void Login(int index)
        {
            if (!settings.User.SandboxMode)
            {
                foreach (Thread loginThread in loginThreads)
                {
                    loginThread.Abort();
                }
            }

            MainGrid.IsEnabled = settings.User.SandboxMode;
            Title = "SAM | Working";

            new Thread(() => {
                try
                {
                    Login(index, 0);
                }
                finally
                {
                    Dispatcher.Invoke(delegate () {
                        MainGrid.IsEnabled = true;
                        Title = "SAM";
                    });
                }
            }).Start();
        }

        private void Login(int index, int tryCount)
        {
            if (tryCount == 0)
            {
                loginThreads.Add(Thread.CurrentThread);
            }

            if (tryCount == maxRetry)
            {
                MessageBox.Show("Login Failed! Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AccountUtils.AccountHasActiveTimeout(encryptedAccounts[index]))
            {
                MessageBoxResult result = MessageBox.Show("Account timeout is active!\nLogin anyway?", "Timeout", MessageBoxButton.YesNo, MessageBoxImage.Warning, 0, MessageBoxOptions.DefaultDesktopOnly);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            // Update the most recently used account index.
            settings.User.RecentAccountIndex = index;
            settings.File.Write(SAMSettings.RECENT_ACCOUNT_INDEX, index.ToString(), SAMSettings.SECTION_AUTOLOG);

            // Verify Steam file path.
            settings.User.SteamPath = AccountUtils.CheckSteamPath();

            if (!settings.User.SandboxMode)
            {
                ShutdownSteam();
            }

            // Make sure Username field is empty and Remember Password checkbox is unchecked.
            AccountUtils.ClearAutoLoginUserKeyValues();

            StringBuilder parametersBuilder = new StringBuilder();
            Account account = decryptedAccounts[index];
            List<string> parameters = globalParameters;

            if (account.FriendsLoginStatus != FriendsLoginStatus.Unchanged && account.SteamId != null && account.SteamId.Length > 0)
            {
                AccountUtils.SetFriendsOnlineMode(account.FriendsLoginStatus, account.SteamId, settings.User.SteamPath);
            }

            if (account.HasParameters)
            {
                parameters = account.Parameters.Split(' ').ToList();
                noReactLogin = account.Parameters.Contains("-noreactlogin");
            }
            else if (settings.User.CustomParameters)
            {
                parametersBuilder.Append(settings.User.CustomParametersValue).Append(" ");
                noReactLogin = settings.User.CustomParametersValue.Contains("-noreactlogin");
            }

            foreach (string parameter in parameters)
            {
                if (parameter.Equals("-login"))
                {
                    // Not working as of August 2023
                    //parametersBuilder.Append(" -vgui ").Append(parameter).Append(" ");

                    StringBuilder passwordBuilder = new StringBuilder();

                    foreach (char c in account.Password)
                    {
                        if (c.Equals('"'))
                        {
                            passwordBuilder.Append('\\').Append(c);
                        }
                        else
                        {
                            passwordBuilder.Append(c);
                        }
                    }

                    parametersBuilder.Append(account.Name).Append(" \"").Append(passwordBuilder.ToString()).Append("\" ");
                }
                else
                {
                    parametersBuilder.Append(parameter).Append(" ");
                }
            }

            string startParams = parametersBuilder.ToString();

            // Start Steam process with the selected path.
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = settings.User.SteamPath + "steam.exe",
                WorkingDirectory = settings.User.SteamPath,
                UseShellExecute = true,
                Arguments = startParams
            };

            Process steamProcess;

            try
            {
                steamProcess = Process.Start(startInfo);
            }
            catch (Exception m)
            {
                MessageBox.Show("There was an error starting Steam\n\n" + m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (settings.User.Login == true)
            {
                // TODO needs more investigation.
                //if (settings.User.RememberPassword == true)
                //{
                //    AccountUtils.SetRememeberPasswordKeyValue(1, account);
                //}

                if (account.SharedSecret != null && account.SharedSecret.Length > 0)
                {
                    Handle2FA(steamProcess, index);
                }
                else
                {
                    PostLogin();
                }
            }
            else
            {
                // -noreactlogin parameter has been depecrated as of January 2023
                if (noReactLogin)
                {
                    TypeCredentials(steamProcess, index, tryCount);
                }
                else
                {
                    EnterCredentials(steamProcess, account, 0);
                }
            }
        }

        private void TypeCredentials(Process steamProcess, int index, int tryCount)
        {
            WindowHandle steamLoginWindow = WindowUtils.GetLegacySteamLoginWindow();

            while (!steamLoginWindow.IsValid)
            {
                Thread.Sleep(100);
                steamLoginWindow = WindowUtils.GetLegacySteamLoginWindow();
            }

            Process steamLoginProcess = WindowUtils.WaitForSteamProcess(steamLoginWindow);
            steamLoginProcess.WaitForInputIdle();

            Thread.Sleep(settings.User.SleepTime);
            WindowUtils.SetForegroundWindow(steamLoginWindow.RawPtr);
            Thread.Sleep(100);

            // Enable Caps-Lock, to prevent IME problems.
            bool capsLockEnabled = System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock);
            if (settings.User.HandleMicrosoftIME && !settings.User.IME2FAOnly && !capsLockEnabled)
            {
                WindowUtils.SendCapsLockGlobally();
            }

            foreach (char c in decryptedAccounts[index].Name.ToCharArray())
            {
                WindowUtils.SetForegroundWindow(steamLoginWindow.RawPtr);
                Thread.Sleep(10);
                WindowUtils.SendCharacter(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod, c);
            }

            Thread.Sleep(100);
            WindowUtils.SendTab(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);
            Thread.Sleep(100);

            foreach (char c in decryptedAccounts[index].Password.ToCharArray())
            {
                WindowUtils.SetForegroundWindow(steamLoginWindow.RawPtr);
                Thread.Sleep(10);
                WindowUtils.SendCharacter(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod, c);
            }

            if (settings.User.RememberPassword)
            {
                WindowUtils.SetForegroundWindow(steamLoginWindow.RawPtr);

                Thread.Sleep(100);
                WindowUtils.SendTab(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);
                Thread.Sleep(100);
                WindowUtils.SendSpace(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);
            }

            WindowUtils.SetForegroundWindow(steamLoginWindow.RawPtr);

            Thread.Sleep(100);
            WindowUtils.SendEnter(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);

            // Restore CapsLock back if CapsLock is off before we start typing.
            if (settings.User.HandleMicrosoftIME && !settings.User.IME2FAOnly && !capsLockEnabled)
            {
                WindowUtils.SendCapsLockGlobally();
            }

            int waitCount = 0;

            // Only handle 2FA if shared secret was entered.
            if (decryptedAccounts[index].SharedSecret != null && decryptedAccounts[index].SharedSecret.Length > 0)
            {
                WindowHandle steamGuardWindow = WindowUtils.GetLegacySteamGuardWindow();

                while (!steamGuardWindow.IsValid && waitCount < maxRetry)
                {
                    Thread.Sleep(settings.User.SleepTime);

                    steamGuardWindow = WindowUtils.GetLegacySteamGuardWindow();

                    // Check for Steam warning window.
                    WindowHandle steamWarningWindow = WindowUtils.GetLegacySteamWarningWindow();
                    if (steamWarningWindow.IsValid)
                    {
                        //Cancel the 2FA process since Steam connection is likely unavailable. 
                        return;
                    }

                    waitCount++;
                }

                // 2FA window not found, login probably failed. Try again.
                if (waitCount == maxRetry)
                {
                    Dispatcher.Invoke(delegate () { Login(index, tryCount + 1); });
                    return;
                }

                Handle2FA(steamProcess, index);
            }
            else
            {
                PostLogin();
            }
        }

        private void EnterCredentials(Process steamProcess, Account account, int tryCount)
        {
            if (steamProcess.HasExited)
            {
                return;
            }

            if (tryCount > 0 && WindowUtils.GetMainSteamClientWindow().IsValid)
            {
                PostLogin();
                return;
            }

            WindowHandle steamLoginWindow = WindowUtils.GetSteamLoginWindow();

            while (!steamLoginWindow.IsValid)
            {
                Thread.Sleep(100);
                steamLoginWindow = WindowUtils.GetSteamLoginWindow();
            }

            LoginWindowState state = LoginWindowState.None;

            while (state != LoginWindowState.Success && state != LoginWindowState.Code)
            {
                if (steamProcess.HasExited || state == LoginWindowState.Error)
                {
                    return;
                }

                Thread.Sleep(100);

                state = WindowUtils.TryCredentialsEntry(steamLoginWindow, account.Name, account.Password, settings.User.RememberPassword);
            }

            if (account.SharedSecret != null && account.SharedSecret.Length > 0)
            {
                EnterReact2FA(steamProcess, account, tryCount);
            }
            else
            {
                Thread.Sleep(settings.User.SleepTime);
                state = LoginWindowState.Loading;

                while (state == LoginWindowState.Loading)
                {
                    Thread.Sleep(100);
                    state = WindowUtils.GetLoginWindowState(steamLoginWindow);
                }

                PostLogin();
            }
        }

        private void Copy2FA(int index)
        {
            var key = WindowUtils.Generate2FACode(decryptedAccounts[index].SharedSecret);
            Clipboard.SetText(key);
            Task.Run(() => {
                SystemSounds.Beep.Play();
                return Task.CompletedTask;
            });
        }

        private void Handle2FA(Process steamProcess, int index)
        {
            if (noReactLogin)
            {
                Type2FA(steamProcess, index, 0);
            }
            else
            {
                EnterReact2FA(steamProcess, decryptedAccounts[index], 0);
            }
        }

        private void EnterReact2FA(Process steamProcess, Account account, int tryCount)
        {
            int retry = tryCount + 1;

            if (steamProcess.HasExited)
            {
                return;
            }

            if (tryCount > 0 && WindowUtils.GetMainSteamClientWindow().IsValid)
            {
                PostLogin();
                return;
            }

            WindowHandle steamLoginWindow = WindowUtils.GetSteamLoginWindow();

            while (!steamLoginWindow.IsValid)
            {
                Thread.Sleep(100);
                steamLoginWindow = WindowUtils.GetSteamLoginWindow();
            }

            LoginWindowState state = LoginWindowState.None;
            string secret = account.SharedSecret;

            while (state != LoginWindowState.Success)
            {
                if (steamProcess.HasExited || state == LoginWindowState.Error)
                {
                    return;
                }
                else if (state == LoginWindowState.Login || state == LoginWindowState.Selection)
                {
                    EnterCredentials(steamProcess, account, retry);
                    return;
                }
                else if (WindowUtils.GetMainSteamClientWindow().IsValid)
                {
                    PostLogin();
                    return;
                }

                Thread.Sleep(100);

                state = WindowUtils.TryCodeEntry(steamLoginWindow, secret);
            }

            Thread.Sleep(settings.User.SleepTime);
            state = LoginWindowState.Loading;

            while (state == LoginWindowState.Loading)
            {
                Thread.Sleep(100);
                state = WindowUtils.GetLoginWindowState(steamLoginWindow);
            }

            steamLoginWindow = WindowUtils.GetSteamLoginWindow();

            if (tryCount < maxRetry && steamLoginWindow.IsValid)
            {
                Console.WriteLine("2FA code might have failed, attempting retry " + retry + "...");
                EnterReact2FA(steamProcess, account, retry);
                return;
            }
            else if (tryCount == maxRetry && steamLoginWindow.IsValid)
            {
                MessageBox.Show("2FA Failed\nPlease verify your shared secret is correct!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PostLogin();
        }

        private void Type2FA(Process steamProcess, int index, int tryCount)
        {
            if (tryCount > 0 && WindowUtils.GetLegacyMainSteamClientWindow().IsValid)
            {
                PostLogin();
                return;
            }

            // Need both the Steam Login and Steam Guard windows.
            // Can't focus the Steam Guard window directly.
            var steamLoginWindow = WindowUtils.GetLegacySteamLoginWindow();
            var steamGuardWindow = WindowUtils.GetLegacySteamGuardWindow();

            while (!steamLoginWindow.IsValid || !steamGuardWindow.IsValid)
            {
                Thread.Sleep(100);
                steamLoginWindow = WindowUtils.GetLegacySteamLoginWindow();
                steamGuardWindow = WindowUtils.GetLegacySteamGuardWindow();

                // Check for Steam warning window.
                var steamWarningWindow = WindowUtils.GetLegacySteamWarningWindow();
                if (steamWarningWindow.IsValid)
                {
                    //Cancel the 2FA process since Steam connection is likely unavailable. 
                    return;
                }
            }

            Console.WriteLine("Found windows.");

            // Generate 2FA code, then send it to the client.
            Console.WriteLine("It is idle now, typing code...");

            WindowUtils.SetForegroundWindow(steamGuardWindow.RawPtr);

            // Enable Caps-Lock, to prevent IME problems.
            bool capsLockEnabled = System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock);
            if (settings.User.HandleMicrosoftIME && !capsLockEnabled)
            {
                WindowUtils.SendCapsLockGlobally();
            }

            Thread.Sleep(100);

            foreach (char c in WindowUtils.Generate2FACode(decryptedAccounts[index].SharedSecret))
            {
                WindowUtils.SetForegroundWindow(steamGuardWindow.RawPtr);
                Thread.Sleep(10);

                // Can also send keys to login window handle, but nothing works unless it is the foreground window.
                WindowUtils.SendCharacter(steamGuardWindow.RawPtr, settings.User.VirtualInputMethod, c);
            }

            WindowUtils.SetForegroundWindow(steamGuardWindow.RawPtr);
            Thread.Sleep(100);
            WindowUtils.SendEnter(steamGuardWindow.RawPtr, settings.User.VirtualInputMethod);
            
            // Restore CapsLock back if CapsLock is off before we start typing.
            if (settings.User.HandleMicrosoftIME && !capsLockEnabled)
            {
                WindowUtils.SendCapsLockGlobally();
            }

            // Need a little pause here to more reliably check for popup later.
            Thread.Sleep(settings.User.SleepTime);

            // Check if we still have a 2FA popup, which means the previous one failed.
            steamGuardWindow = WindowUtils.GetLegacySteamGuardWindow();

            int retry = tryCount + 1;

            if (tryCount < maxRetry && steamGuardWindow.IsValid)
            {
                Console.WriteLine("2FA code might have failed, attempting retry " + retry + "...");
                Type2FA(steamProcess, index, retry);
                return;
            }
            else if (tryCount == maxRetry && steamGuardWindow.IsValid)
            {
                MessageBoxResult result = MessageBox.Show("2FA Failed\nPlease wait or bring the Steam Guard\nwindow to the front before clicking OK", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);

                if (result == MessageBoxResult.OK)
                {
                    Type2FA(steamProcess, index, retry);
                }
            }
            else if (tryCount == maxRetry + 1 && steamGuardWindow.IsValid)
            {
                MessageBox.Show("2FA Failed\nPlease verify your shared secret is correct!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PostLogin();
        }

        private void PostLogin()
        {
            if (loginAllSequence == false)
            {
                if (settings.User.ClearUserData == true)
                {
                    WindowUtils.ClearSteamUserDataFolder(settings.User.SteamPath, settings.User.SleepTime, maxRetry);
                }
                if (settings.User.CloseOnLogin == true)
                {
                    Dispatcher.Invoke(delegate () { Close(); });
                }
            }
        }

        private void SortAccounts(int type)
        {
            if (encryptedAccounts.Count > 0)
            {
                // Alphabetical sort based on account name.
                if (type == 0)
                {
                    encryptedAccounts = encryptedAccounts.OrderBy(x => x.Name).ToList();
                }
                else if (type == 1)
                {
                    encryptedAccounts = encryptedAccounts.OrderBy(x => Guid.NewGuid()).ToList();
                }

                SerializeAccounts();
            }
        }

        private void SerializeAccounts()
        {
            if (loadSource != backupFile && File.Exists(dataFile))
            {
                File.Delete(backupFile);
                File.Copy(dataFile, backupFile);
            }

            if (IsPasswordProtected() == true && ePassword.Length > 0)
            {
                AccountUtils.PasswordSerialize(encryptedAccounts, ePassword);
            }
            else
            {
                AccountUtils.Serialize(encryptedAccounts);
            }

            RefreshWindow(dataFile);
        }

        private void ExportAccount(int index)
        {
            AccountUtils.ExportSelectedAccounts(new List<Account>() { encryptedAccounts[index] });
        }

        #region Resize and Resize Timer

        public void Resize(double _PassedHeight, double _PassedWidth)
        {
            _Height = _PassedHeight;
            _Width = _PassedWidth;

            _Timer.Enabled = true;
            _Timer.Start();
        }

        private void Timer_Tick(Object myObject, EventArgs myEventArgs)
        {
            if (_Stop == 0)
            {
                _RatioHeight = ((Height - _Height) / 5) * -1;
                _RatioWidth = ((Width - _Width) / 5) * -1;
            }
            _Stop++;

            Height += _RatioHeight;
            Width += _RatioWidth;

            if (_Stop == 5)
            {
                _Timer.Stop();
                _Timer.Enabled = false;
                _Timer.Dispose();

                _Stop = 0;

                Height = _Height;
                Width = _Width;

                SetMainScrollViewerBarsVisibility(ScrollBarVisibility.Auto);
            }
        }

        #endregion

        #region Click Events

        private void AccountButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Opacity = 0.5;

                holdingButton = btn;

                mouseHoldTimer = new System.Timers.Timer(1000);
                mouseHoldTimer.Elapsed += MouseHoldTimer_Elapsed;
                mouseHoldTimer.Enabled = true;
                mouseHoldTimer.Start();
            }
        }

        private void MouseHoldTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            mouseHoldTimer.Stop();

            if (holdingButton != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => holdingButton.Opacity = 1));
                dragging = true;
            }
        }

        private void AccountButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                holdingButton = null;
                btn.Opacity = 1;
                dragging = false;
            }
        }

        private void AccountButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                holdingButton = null;
                btn.Opacity = 1;
                dragging = false;
            }
        }

        private void AccountButton_MouseLeave(Button accountButton, TextBlock accountText)
        {
            accountText.Visibility = Visibility.Collapsed;
        }

        private void AccountButton_MouseEnter(Button accountButton, TextBlock accountText)
        {
            accountText.Visibility = Visibility.Visible;
        }

        private void AccountButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                if (dragging == false)
                {
                    return;
                }

                btn.Opacity = 1;

                Point mousePoint = e.GetPosition(this);

                int marginLeft = (int)mousePoint.X - ((int)btn.Width / 2);
                int marginTop = (int)mousePoint.Y - ((int)btn.Height / 2);

                btn.Margin = new Thickness(marginLeft, marginTop, 0, 0);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddAccount();
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Login with clicked button's index, which stored in Tag.
                int index = Int32.Parse(btn.Tag.ToString());
                Login(index);
            }
        }

        private void TaskbarIconLoginItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int index = Int32.Parse(item.Tag.ToString());
                Login(index);
            }
        }

        private void AccountButtonSetTimeout_Click(int index, DateTime timeout)
        {
            if (timeout != null && timeout != new DateTime())
            {
                encryptedAccounts[index].Timeout = timeout;
            }
            else
            {
                //MessageBox.Show("Error setting account timeout.");
                return;
            }

            SerializeAccounts();
        }

        private void AccountButtonSetCustomTimeout_Click(int index)
        {
            var setTimeoutWindow = new SetTimeoutWindow(encryptedAccounts[index].Timeout);
            setTimeoutWindow.ShowDialog();

            if (setTimeoutWindow.timeout != null && setTimeoutWindow.timeout != new DateTime())
            {
                encryptedAccounts[index].Timeout = setTimeoutWindow.timeout;
            }
            else
            {
                MessageBox.Show("Error setting account timeout.");
                return;
            }

            SerializeAccounts();
        }

        private void AccountButtonClearTimeout_Click(int index)
        {
            encryptedAccounts[index].Timeout = null;
            SerializeAccounts();
        }

        private void AccountButtonExport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int index = Int32.Parse(btn.Tag.ToString());

                // Check if this index has already been added.
                // Remove if it is, add if it isn't.
                if (exportAccounts.ContainsKey(index))
                {
                    exportAccounts.Remove(index);
                    btn.Opacity = 1;
                }
                else
                {
                    exportAccounts.Add(index, encryptedAccounts[index]);
                    btn.Opacity = 0.5;
                }
            }
        }

        public async Task ReloadAccount_ClickAsync(int index)
        {
            await ReloadAccount(encryptedAccounts[index]);
            SerializeAccounts();
            MessageBox.Show("Done!");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsWindow();
            settingsDialog.ShowDialog();

            settings.User.AccountsPerRow = settingsDialog.AccountsPerRow;

            string previousPass = ePassword;

            if (settingsDialog.Decrypt == true)
            {
                AccountUtils.Serialize(encryptedAccounts);
                ePassword = "";
            }
            else if (settingsDialog.Password != null)
            {
                ePassword = settingsDialog.Password;

                if (previousPass != ePassword)
                {
                    AccountUtils.PasswordSerialize(encryptedAccounts, ePassword);
                }
            }

            LoadSettings();
            RefreshWindow(dataFile);
        }

        private void DeleteBannedAccounts_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete all banned accounts?" +
                "\nThis action is perminant and cannot be undone!", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                List<Account> accountsToDelete = new List<Account>();

                foreach (Account account in encryptedAccounts)
                {
                    if (account.NumberOfVACBans > 0 || account.NumberOfGameBans > 0)
                    {
                        accountsToDelete.Add(account);
                    }
                }

                foreach (Account account in accountsToDelete)
                {
                    encryptedAccounts.Remove(account);
                }

                SerializeAccounts();
            }
        }

        private void GitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(repositoryUrl);
        }

        private async void Ver_Click(object sender, RoutedEventArgs e)
        {
            UpdateResponse response = await UpdateHelper.CheckForUpdate(updateCheckUrl);

            switch (response)
            {
                case UpdateResponse.NoUpdate:
                    MessageBox.Show(Process.GetCurrentProcess().ProcessName + " is up to date!");
                    break;

                case UpdateResponse.Update:
                    await UpdateHelper.StartUpdate(updateCheckUrl, releasesUrl);
                    Close();
                    return;
            }
        }

        private void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RefreshWindow(dataFile);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SortAlphabetical_Click(object sender, RoutedEventArgs e)
        {
            SortAccounts(0);
        }

        private void ShuffleAccounts_Click(object sender, RoutedEventArgs e)
        {
            SortAccounts(1);
        }

        private void ImportFromFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AccountUtils.ImportAccountFile();
            RefreshWindow(dataFile);
        }

        private void ExportAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AccountUtils.ExportAccountFile();
        }

        private void ExportSelectedAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ExportAccount(Int32.Parse(btn.Tag.ToString()));
            }
        }

        private async void ReloadAccounts_Click(object sender, RoutedEventArgs e)
        {
            await ReloadAccountsAsync();
        }

        private void ShowWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }

        private void TaskbarIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = WindowState.Normal;
        }

        private void CopyUsernameToClipboard(int index)
        {
            try
            {
                Clipboard.SetText(decryptedAccounts[index].Name);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void CopyPasswordToClipboard(int index)
        {
            try
            {
                Clipboard.SetText(decryptedAccounts[index].Password);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void CopyProfileUrlToClipboard(int index)
        {
            try
            {
                Clipboard.SetText(decryptedAccounts[index].ProfUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void LoginAllMissingItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show(
                "You are about to start the automatic login sequence for all " +
                "accounts that do not currently have an associated Steam Id. " +
                "This will generate a Steam Id in the local vdf files for these " +
                "accounts to be read by SAM.\n\n" +
                "You can cancel this process at any time with ESC.\n\n" +
                "This may take some time depending on the number of accounts. " +
                "Are you sure you want to login all accounts missing a Steam Id?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                FileMenu.IsEnabled = false;
                AccountsDataGrid.IsEnabled = false;
                buttonGrid.IsEnabled = false;
                AddButtonGrid.IsEnabled = false;
                TaskBarIconLoginContextMenu.IsEnabled = false;

                loginAllSequence = true;

                InterceptKeys.OnKeyDown += new System.Windows.Forms.KeyEventHandler(EscKeyDown);
                InterceptKeys.Start();

                Task.Run(() => LoginAllMissing());
            }
        }
        #endregion

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (exporting == true)
            {
                GenerateAltActionContextMenu(AltActionType.EXPORTING).IsOpen = true;
                e.Handled = true;
            }
            else if (deleting == true)
            {
                GenerateAltActionContextMenu(AltActionType.DELETING).IsOpen = true;
                e.Handled = true;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    break;
                case WindowState.Minimized:
                    if (settings.User.MinimizeToTray == true)
                    {
                        Visibility = Visibility.Hidden;
                        ShowInTaskbar = false;
                    }
                    break;
                case WindowState.Normal:
                    Visibility = Visibility.Visible;
                    ShowInTaskbar = true;
                    break;
            }
        }

        private void ImportDelimitedTextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var importDelimitedWindow = new ImportDelimited(eKey);
            importDelimitedWindow.ShowDialog();

            RefreshWindow(dataFile);
        }

        private void ExposeCredentialsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure you want to expose all account credentials in plain text?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (messageBoxResult == MessageBoxResult.No || (IsPasswordProtected() && !VerifyPassword()))
            {
                return;
            }

            var exposedCredentialsWindow = new ExposedInfoWindow(decryptedAccounts);
            exposedCredentialsWindow.ShowDialog();
        }

        private bool IsPasswordProtected()
        {
            if (settings.User.PasswordProtect == true)
            {
                return true;
            }
            else
            {
                try
                {
                    if (!File.Exists(dataFile))
                    {
                        return false;
                    }
                    else
                    {
                        string[] lines = File.ReadAllLines(dataFile);
                        if (lines.Length == 0 || lines.Length > 1)
                        {
                            return false;
                        }
                    }
                    AccountUtils.Deserialize(dataFile);
                }
                catch
                {
                    return true;
                }
            }

            return false;
        }

        void TimeoutTimer_Tick(int index, TextBlock timeoutLabel, System.Timers.Timer timeoutTimer)
        {
            var timeLeft = encryptedAccounts[index].Timeout - DateTime.Now;

            if (timeLeft.Value.CompareTo(TimeSpan.Zero) <= 0)
            {
                timeoutTimer.Stop();
                timeoutTimer.Dispose();

                timeoutLabel.Visibility = Visibility.Hidden;
                AccountButtonClearTimeout_Click(index);
            }
            else
            {
                timeoutLabel.Text = AccountUtils.FormatTimespanString(timeLeft.Value);
                timeoutLabel.Visibility = Visibility.Visible;
            }
        }

        void TimeoutTimer_Tick(int index, System.Timers.Timer timeoutTimer)
        {
            var timeLeft = encryptedAccounts[index].Timeout - DateTime.Now;

            if (timeLeft.Value.CompareTo(TimeSpan.Zero) <= 0)
            {
                timeoutTimer.Stop();
                timeoutTimer.Dispose();

                encryptedAccounts[index].TimeoutTimeLeft = null;
                AccountButtonClearTimeout_Click(index);
            }
            else
            {
                encryptedAccounts[index].TimeoutTimeLeft = AccountUtils.FormatTimespanString(timeLeft.Value);
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (!isLoadingSettings && settings.File != null && IsInBounds() == true)
            {
                settings.File.Write(SAMSettings.WINDOW_LEFT, Left.ToString(), SAMSettings.SECTION_LOCATION);
                settings.File.Write(SAMSettings.WINDOW_TOP, Top.ToString(), SAMSettings.SECTION_LOCATION);
            }
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!isLoadingSettings && settings.File != null && settings.User.ListView == true)
            {
                settings.User.ListViewHeight = Height;
                settings.User.ListViewWidth = Width;

                settings.File.Write(SAMSettings.LIST_VIEW_HEIGHT, Height.ToString(), SAMSettings.SECTION_LOCATION);
                settings.File.Write(SAMSettings.LIST_VIEW_WIDTH, Width.ToString(), SAMSettings.SECTION_LOCATION);
            }
        }

        private void SetMainScrollViewerBarsVisibility(ScrollBarVisibility visibility)
        {
            MainScrollViewer.VerticalScrollBarVisibility = visibility;
            MainScrollViewer.HorizontalScrollBarVisibility = visibility;
        }

        private void SetWindowSettingsIntoScreenArea()
        {
            if (IsInBounds() == false)
            {
                SetWindowToCenter();
            }
        }

        private bool IsInBounds()
        {
            foreach (System.Windows.Forms.Screen scrn in System.Windows.Forms.Screen.AllScreens)
            {
                if (scrn.Bounds.Contains((int)Left, (int)Top))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetWindowToCenter()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            Left = (screenWidth / 2) - (Width / 2);
            Top = (screenHeight / 2) - (Height / 2);
        }

        #region Account Button State Handling

        private void ExportSelectedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            exporting = true;
            FileMenuItem.IsEnabled = false;
            EditMenuItem.IsEnabled = false;

            exportAccounts = new Dictionary<int, Account>();

            if (settings.User.ListView == true)
            {
                AccountsDataGrid.SelectionMode = DataGridSelectionMode.Extended;
                Application.Current.Resources["AccountGridActionHighlightColor"] = Brushes.Green;
            }
            else
            {
                AddButton.Visibility = Visibility.Hidden;
                ExportButton.Visibility = Visibility.Visible;
                CancelExportButton.Visibility = Visibility.Visible;

                IEnumerable<Grid> buttonGridCollection = buttonGrid.Children.OfType<Grid>();

                foreach (Grid accountButtonGrid in buttonGridCollection)
                {
                    Button accountButton = accountButtonGrid.Children.OfType<Button>().FirstOrDefault();

                    accountButton.Style = (Style)Resources["ExportButtonStyle"];
                    accountButton.Click -= new RoutedEventHandler(AccountButton_Click);
                    accountButton.Click += new RoutedEventHandler(AccountButtonExport_Click);
                    accountButton.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(AccountButton_MouseDown);
                    accountButton.PreviewMouseLeftButtonUp -= new MouseButtonEventHandler(AccountButton_MouseUp);
                    accountButton.MouseLeave -= new MouseEventHandler(AccountButton_MouseLeave);
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportSelectedAccounts();
        }

        private void ExportSelectedAccounts()
        {
            if (settings.User.ListView == true)
            {
                for (int i = 0; i < AccountsDataGrid.SelectedItems.Count; i++)
                {
                    exportAccounts.Add(i, AccountsDataGrid.SelectedItems[i] as Account);
                }
            }

            if (exportAccounts.Count > 0)
            {
                AccountUtils.ExportSelectedAccounts(exportAccounts.Values.ToList());
            }
            else
            {
                MessageBox.Show("No accounts selected to export!");
            }

            ResetFromExportOrDelete();
        }

        private void CancelExportButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFromExportOrDelete();
        }

        private void DeleteAllAccountsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (encryptedAccounts.Count > 0)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure you want to delete all accounts?\nThis action will perminantly delete the account data file.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    if ((IsPasswordProtected() && VerifyPassword()) || !IsPasswordProtected())
                    {
                        try
                        {
                            File.Delete(dataFile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        RefreshWindow(dataFile);
                    }
                }
            }
        }

        private void DeleteSelectedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            deleting = true;
            deleteAccounts = new Dictionary<int, Account>();

            FileMenuItem.IsEnabled = false;
            EditMenuItem.IsEnabled = false;

            if (settings.User.ListView == true)
            {
                AccountsDataGrid.SelectionMode = DataGridSelectionMode.Extended;
                Application.Current.Resources["AccountGridActionHighlightColor"] = Brushes.Red;
            }
            else
            {
                AddButton.Visibility = Visibility.Hidden;
                DeleteButton.Visibility = Visibility.Visible;
                CancelExportButton.Visibility = Visibility.Visible;

                IEnumerable<Grid> buttonGridCollection = buttonGrid.Children.OfType<Grid>();

                foreach (Grid accountButtonGrid in buttonGridCollection)
                {
                    Button accountButton = accountButtonGrid.Children.OfType<Button>().FirstOrDefault();

                    accountButton.Style = (Style)Resources["DeleteButtonStyle"];
                    accountButton.Click -= new RoutedEventHandler(AccountButton_Click);
                    accountButton.Click += new RoutedEventHandler(AccountButtonDelete_Click);
                    accountButton.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(AccountButton_MouseDown);
                    accountButton.PreviewMouseLeftButtonUp -= new MouseButtonEventHandler(AccountButton_MouseUp);
                    accountButton.MouseLeave -= new MouseEventHandler(AccountButton_MouseLeave);
                }
            }
        }

        private void AccountButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int index = Int32.Parse(btn.Tag.ToString());

                // Check if this index has already been added.
                // Remove if it is, add if it isn't.
                if (deleteAccounts.ContainsKey(index))
                {
                    deleteAccounts.Remove(index);
                    btn.Opacity = 1;
                }
                else
                {
                    deleteAccounts.Add(index, encryptedAccounts[index]);
                    btn.Opacity = 0.5;
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedAccounts();
        }

        private void DeleteSelectedAccounts()
        {
            if (settings.User.ListView == true)
            {
                for (int i = 0; i < AccountsDataGrid.SelectedItems.Count; i++)
                {
                    deleteAccounts.Add(i, AccountsDataGrid.SelectedItems[i] as Account);
                }
            }

            if (deleteAccounts.Count > 0)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure you want to delete the selected accounts?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    foreach (Account account in deleteAccounts.Values.ToList())
                    {
                        encryptedAccounts.Remove(account);
                    }

                    SerializeAccounts();
                }
            }
            else
            {
                MessageBox.Show("No accounts selected!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            ResetFromExportOrDelete();
        }

        private void ResetFromExportOrDelete()
        {
            FileMenuItem.IsEnabled = true;
            EditMenuItem.IsEnabled = true;

            if (settings.User.ListView == true)
            {
                AccountsDataGrid.SelectionMode = DataGridSelectionMode.Single;
            }
            else
            {
                if (settings.User.HideAddButton == true)
                {
                    AddButton.Visibility = Visibility.Visible;
                }

                DeleteButton.Visibility = Visibility.Hidden;
                ExportButton.Visibility = Visibility.Hidden;
                CancelExportButton.Visibility = Visibility.Hidden;

                IEnumerable<Grid> buttonGridCollection = buttonGrid.Children.OfType<Grid>();

                foreach (Grid accountButtonGrid in buttonGridCollection)
                {
                    Button accountButton = accountButtonGrid.Children.OfType<Button>().FirstOrDefault();

                    accountButton.Style = (Style)Resources["SAMButtonStyle"];
                    accountButton.Click -= new RoutedEventHandler(AccountButtonExport_Click);
                    accountButton.Click -= new RoutedEventHandler(AccountButtonDelete_Click);
                    accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                    accountButton.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(AccountButton_MouseDown);
                    accountButton.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(AccountButton_MouseUp);
                    accountButton.MouseLeave += new MouseEventHandler(AccountButton_MouseLeave);

                    accountButton.Opacity = 1;
                }
            }

            deleting = false;
            exporting = false;
        }

        private void AccountsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AccountsDataGrid.SelectedItem != null && deleting == false)
            {
                Account account = AccountsDataGrid.SelectedItem as Account;
                int index = encryptedAccounts.FindIndex(a => a.Name == account.Name);
                Login(index);
            }
        }

        private void AccountsDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (exporting == true)
            {
                AccountsDataGrid.ContextMenu = GenerateAltActionContextMenu(AltActionType.EXPORTING);
            }
            else if (deleting == true)
            {
                AccountsDataGrid.ContextMenu = GenerateAltActionContextMenu(AltActionType.DELETING);
            }
            else if (AccountsDataGrid.SelectedItem != null)
            {
                Account account = AccountsDataGrid.SelectedItem as Account;
                int index = encryptedAccounts.FindIndex(a => a.Name == account.Name);
                AccountsDataGrid.ContextMenu = GenerateAccountContextMenu(account, index);
            }
        }

        private void AccountsDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject DepObject = (DependencyObject)e.OriginalSource;

            while ((DepObject != null) && !(DepObject is DataGridColumnHeader) && !(DepObject is DataGridRow))
            {
                DepObject = VisualTreeHelper.GetParent(DepObject);
            }

            if (DepObject == null || DepObject is DataGridColumnHeader)
            {
                AccountsDataGrid.ContextMenu = null;
            }
        }

        #endregion

        private void AccountsDataGrid_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            foreach (DataGridColumn column in AccountsDataGrid.Columns)
            {
                settings.File.Write(settings.ListViewColumns[column.Header.ToString()], column.DisplayIndex.ToString(), SAMSettings.SECTION_COLUMNS);
            }
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void LoginAllMissing()
        {
            // Auto login all accounts missing steamId.
            for (int i = 0; i < encryptedAccounts.Count; i++)
            {
                if (loginAllCancelled == true)
                {
                    StopLoginAllMissing();
                    return;
                }

                if (encryptedAccounts[i].SteamId == null || encryptedAccounts[i].SteamId.Length == 0)
                {
                    Login(i);

                    // Wait and check if full steam client window is open.
                    WindowUtils.WaitForSteamClientWindow();
                }
            }

            StopLoginAllMissing();

            MessageBox.Show("Done!");
        }

        private void StopLoginAllMissing()
        {
            InterceptKeys.Stop();
            InterceptKeys.OnKeyDown -= EscKeyDown;

            ShutdownSteam();

            Dispatcher.Invoke(() =>
            {
                ReloadAccountsAsync();

                FileMenu.IsEnabled = true;
                AccountsDataGrid.IsEnabled = true;
                buttonGrid.IsEnabled = true;
                AddButtonGrid.IsEnabled = true;
                TaskBarIconLoginContextMenu.IsEnabled = true;
                loginAllSequence = false;
                loginAllCancelled = false;
            });
        }

        private void EscKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                // Prompt to cancel auto login process.
                MessageBoxResult messageBoxResult = MessageBox.Show(
                "Cancel Login All Sequence?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    Dispatcher.Invoke(() =>
                    {
                        loginAllCancelled = true;
                    });

                    WindowUtils.CancelLoginAll();
                }
            }
        }

        private void ShutdownSteam()
        {
            // Shutdown Steam process via command if it is already open.
            ProcessStartInfo stopInfo = new ProcessStartInfo
            {
                FileName = settings.User.SteamPath + "steam.exe",
                WorkingDirectory = settings.User.SteamPath,
                Arguments = "-shutdown"
            };

            try
            {
                Process SteamProc = Process.GetProcessesByName("Steam").FirstOrDefault();
                Process[] WebClientProcs = Process.GetProcessesByName("steamwebhelper");
                if (SteamProc != null)
                {
                    Process.Start(stopInfo);
                    SteamProc.WaitForExit();

                    foreach (Process proc in WebClientProcs)
                    {
                        proc.WaitForExit();
                    }
                }
            }
            catch
            {
                Console.WriteLine("No steam process found or steam failed to shutdown.");
            }
        }

        private void AccountsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (loginThreads != null)
            {
                foreach (Thread thread in loginThreads)
                {
                    Console.WriteLine("Aborting thread...");
                    thread.Abort();
                }
            }
        }

        private void AccountsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                object selectedItem = AccountsDataGrid.SelectedItem;
                if (selectedItem != null)
                {
                    Login(AccountsDataGrid.SelectedIndex);
                }

                e.Handled = true;
            }
        }
    }
}
