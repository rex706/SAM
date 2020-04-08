using SteamAuth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using MahApps.Metro.Controls;
using System.Windows.Controls.Primitives;
using MahApps.Metro;

namespace SAM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    [Serializable]
    public partial class MainWindow : MetroWindow
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

        // Keys are changed before releases/updates
        private static readonly string eKey = "PRIVATE_KEY";
        private static string ePassword = "";

        private static string account;

        private static List<string> launchParameters;

        private static double originalHeight;
        private static double originalWidth;
        private static Thickness initialAddButtonGridMargin;

        private static string AssemblyVer;

        private static bool exporting = false;
        private static bool deleting = false;

        private static Button holdingButton = null;
        private static bool dragging = false;
        private static System.Timers.Timer mouseHoldTimer;

        private static System.Timers.Timer autoReloadApiTimer;

        private static int maxRetry = 2;

        // Resize animation variables
        private static System.Windows.Forms.Timer _Timer = new System.Windows.Forms.Timer();
        private int _Stop = 0;
        private double _RatioHeight;
        private double _RatioWidth;
        private double _Height;
        private double _Width;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // If no settings file exists, create one and initialize values.
            if (!File.Exists(SAMSettings.FILE_NAME))
            {
                GenerateSettings();
            }
            
            LoadSettings();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.BackgroundBorder.PreviewMouseLeftButtonDown += (s, e) => { DragMove(); };

            _Timer.Tick += new EventHandler(Timer_Tick);
            _Timer.Interval = (10);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Version number from assembly
            AssemblyVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            MenuItem ver = new MenuItem();
            MenuItem newExistMenuItem = (MenuItem)this.FileMenu.Items[2];
            ver.Header = "v" + AssemblyVer;
            ver.IsEnabled = false;
            newExistMenuItem.Items.Add(ver);

            // Delete updater if exists.
            if (File.Exists("Updater.exe"))
            {
                File.Delete("Updater.exe");
            }

            // Check for a new version if enabled.
            if (settings.User.CheckForUpdates && await UpdateCheck.CheckForUpdate(updateCheckUrl, releasesUrl) == 1)
            {
                // An update is available, but user has chosen not to update.
                ver.Header = "Update Available!";
                ver.Click += Ver_Click;
                ver.IsEnabled = true;
            }
            
            loginThreads = new List<Thread>();

            // Save New Button inital margin.
            initialAddButtonGridMargin = AddButtonGrid.Margin;

            // Save initial window height and width;
            originalHeight = Height;
            originalWidth = Width;

            // Load window with account buttons.
            RefreshWindow();

            // Login to auto log account if enabled and Steam is not already open.
            Process[] SteamProc = Process.GetProcessesByName("Steam");

            if (SteamProc.Length == 0)
            {
                if (settings.User.LoginRecentAccount == true)
                    Login(settings.User.RecentAccountIndex, 0);
                else if (settings.User.LoginSelectedAccount == true)
                    Login(settings.User.SelectedAccountIndex, 0);
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
                        encryptedAccounts = Utils.PasswordDeserialize(dataFile, passwordDialog.PasswordText);
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
            launchParameters = new List<string>();

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
                            int sleepTime = 0;

                            if (!Regex.IsMatch(sleepTimeString, @"^\d+$") || !Int32.TryParse(sleepTimeString, out sleepTime) || sleepTime < 0 || sleepTime > 100)
                            {
                                settings.File.Write(SAMSettings.SLEEP_TIME, settings.Default.SleepTime.ToString(), SAMSettings.SECTION_GENERAL);
                                settings.User.SleepTime = settings.Default.SleepTime * 1000;
                            }
                            else
                            {
                                settings.User.SleepTime = sleepTime * 1000;
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
                                        launchParameters.Add("-" + entry.Key);
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

            Utils.CheckSteamPath();
            settings.File.Write("Version", AssemblyVer, "System");
            isLoadingSettings = false;
        }

        private void AutoReloadApiTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                ReloadAccountsAsync();
            });
        }

        public void RefreshWindow()
        {
            decryptedAccounts = new List<Account>();

            buttonGrid.Children.Clear();

            TaskBarIconLoginContextMenu.Items.Clear();
            TaskBarIconLoginContextMenu.IsEnabled = false;

            AddButtonGrid.Height = settings.User.ButtonSize;
            AddButtonGrid.Width = settings.User.ButtonSize;

            // Check if info.dat exists
            if (File.Exists(dataFile))
            {
                // Deserialize file
                if (ePassword.Length > 0)
                {
                    MessageBoxResult messageBoxResult = MessageBoxResult.OK;

                    while (messageBoxResult == MessageBoxResult.OK)
                    {
                        try
                        {
                            encryptedAccounts = Utils.PasswordDeserialize(dataFile, ePassword);
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
                    encryptedAccounts = Utils.Deserialize(dataFile);
                }
                
                PostDeserializedRefresh(true);
            }
            else
            {
                encryptedAccounts = new List<Account>();
                SerializeAccounts();
            }

            AccountsDataGrid.ItemsSource = encryptedAccounts;

            if (firstLoad == true && settings.User.AutoReloadEnabled == true && Utils.ShouldAutoReload(settings.User.LastAutoReload, settings.User.AutoReloadInterval))
            {
                firstLoad = false;
                ReloadAccountsAsync();
            }
        }

        private async Task ReloadAccount(Account account)
        {
            dynamic userJson = null;

            if (account.SteamId != null && account.SteamId.Length > 0)
            {
                userJson = await Utils.GetUserInfoFromWebApiBySteamId(account.SteamId);
            }
            else
            {
                userJson = await Utils.GetUserInfoFromConfigAndWebApi(account.Name);
            }

            if (userJson != null)
            {
                account.ProfUrl = userJson.response.players[0].profileurl;
                account.AviUrl = userJson.response.players[0].avatarfull;
                account.SteamId = userJson.response.players[0].steamid;
            }
            else
            {
                account.AviUrl = await Utils.HtmlAviScrapeAsync(account.ProfUrl);
            }

            if (account.SteamId != null && account.SteamId.Length > 0 && Utils.ApiKeyExists())
            {
                dynamic userBanJson = await Utils.GetPlayerBansFromWebApi(account.SteamId);

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
            Title = "SAM Loading...";

            List<string> steamIds = new List<string>();

            foreach (Account account in encryptedAccounts)
            {
                if (account.SteamId != null && account.SteamId.Length > 0)
                {
                    steamIds.Add(account.SteamId);
                }
                else
                {
                    string steamId = Utils.GetSteamIdFromConfig(account.Name);
                    if (steamId != null && steamId.Length > 0)
                    {
                        steamIds.Add(steamId);
                    }
                    else if (account.ProfUrl != null && account.ProfUrl.Length > 0) 
                    {
                        // Try to get steamId from profile URL via web API.

                        dynamic steamIdFromProfileUrl = await Utils.GetSteamIdFromProfileUrl(account.ProfUrl);

                        if (steamIdFromProfileUrl != null)
                        {
                            account.SteamId = steamIdFromProfileUrl;
                            steamIds.Add(steamIdFromProfileUrl);
                        }

                        Thread.Sleep(new Random().Next(10,16));
                    }
                }
            }

            List<dynamic> userInfos = await Utils.GetUserInfosFromWepApi(new List<string>(steamIds));

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

            if (Utils.ApiKeyExists())
            {
                List<dynamic> userBans = await Utils.GetPlayerBansFromWebApi(new List<string>(steamIds));

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

                        decryptedAccounts.Add(new Account() { Name = account.Name, Alias = account.Alias, Password = tempPass, SharedSecret = temp2fa, ProfUrl = account.ProfUrl, AviUrl = account.AviUrl, SteamId = steamId, Timeout = account.Timeout, Description = account.Description });
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

                        if (Utils.AccountHasActiveTimeout(account))
                        {
                            // Set up timer event to update timeout label
                            var timeLeft = account.Timeout - DateTime.Now;

                            System.Timers.Timer timeoutTimer = new System.Timers.Timer();
                            timeoutTimers.Add(timeoutTimer);

                            timeoutTimer.Elapsed += delegate
                            {
                                this.Dispatcher.Invoke(() =>
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
                        accountButton.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseDown);
                        accountButton.PreviewMouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseUp);
                        //accountButton.PreviewMouseMove += new System.Windows.Input.MouseEventHandler(AccountButton_MouseMove);
                        accountButton.MouseLeave += new System.Windows.Input.MouseEventHandler(AccountButton_MouseLeave);
                        accountButton.MouseEnter += delegate { AccountButton_MouseEnter(accountButton, accountText); };
                        accountButton.MouseLeave += delegate { AccountButton_MouseLeave(accountButton, accountText); };

                        accountButtonGrid.Children.Add(accountImage);

                        int buttonIndex = Int32.Parse(accountButton.Tag.ToString());

                        if (Utils.AccountHasActiveTimeout(account))
                        {
                            // Set up timer event to update timeout label
                            var timeLeft = account.Timeout - DateTime.Now;

                            System.Timers.Timer timeoutTimer = new System.Timers.Timer();
                            timeoutTimers.Add(timeoutTimer);

                            timeoutTimer.Elapsed += delegate
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    TimeoutTimer_Tick(buttonIndex, timeoutTextBlock, timeoutTimer);
                                });
                            };
                            timeoutTimer.Interval = 1000;
                            timeoutTimer.Enabled = true;
                            timeoutTextBlock.Text = Utils.FormatTimespanString(timeLeft.Value);
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
                            banInfoImage.Source = new BitmapImage(new Uri(@"error_red_18dp.png", UriKind.RelativeOrAbsolute));

                            banInfoImage.ToolTip = "VAC Bans: " + account.NumberOfVACBans +
                                "\nGame Bans: " + account.NumberOfGameBans +
                                "\nCommunity Banned: " + account.CommunityBanned +
                                "\nEconomy Ban: " + account.EconomyBan +
                                "\nDays Since Last Ban:" + account.DaysSinceLastBan;

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
            MenuItem taskBarIconLoginItem = new MenuItem();
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

            MenuItem deleteItem = new MenuItem();
            MenuItem editItem = new MenuItem();
            MenuItem exportItem = new MenuItem();
            MenuItem reloadItem = new MenuItem();

            MenuItem setTimeoutItem = new MenuItem();

            MenuItem thirtyMinuteTimeoutItem = new MenuItem();
            MenuItem twoHourTimeoutItem = new MenuItem();
            MenuItem twentyOneHourTimeoutItem = new MenuItem();
            MenuItem twentyFourHourTimeoutItem = new MenuItem();
            MenuItem sevenDayTimeoutItem = new MenuItem();
            MenuItem customTimeoutItem = new MenuItem();

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

            MenuItem clearTimeoutItem = new MenuItem();
            MenuItem copyUsernameItem = new MenuItem();
            MenuItem copyPasswordItem = new MenuItem();
            MenuItem copyProfileUrlItem = new MenuItem();

            if (!Utils.AccountHasActiveTimeout(account))
            {
                clearTimeoutItem.IsEnabled = false;
            }

            deleteItem.Header = "Delete";
            editItem.Header = "Edit";
            exportItem.Header = "Export";
            reloadItem.Header = "Reload";
            setTimeoutItem.Header = "Set Timeout";
            clearTimeoutItem.Header = "Clear Timeout";
            copyUsernameItem.Header = "Copy Username";
            copyPasswordItem.Header = "Copy Password";
            copyProfileUrlItem.Header = "Copy Profile URL";

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
            accountContext.Items.Add(copyUsernameItem);
            accountContext.Items.Add(copyPasswordItem);
            accountContext.Items.Add(copyProfileUrlItem);

            return accountContext;
        }

        private ContextMenu GenerateAltActionContextMenu(string altActionType)
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem actionMenuItem = new MenuItem();

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

            MenuItem cancelMenuItem = new MenuItem();
            cancelMenuItem.Header = "Cancel";
            cancelMenuItem.Click += delegate { ResetFromExportOrDelete(); };

            contextMenu.Items.Add(actionMenuItem);
            contextMenu.Items.Add(cancelMenuItem);

            return contextMenu;
        }

        private async void AddAccount()
        {
            // User entered info
            var dialog = new TextDialog();

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
                    aviUrl = await Utils.HtmlAviScrapeAsync(dialog.UrlText);
                }

                string steamId = dialog.SteamId;

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settings.File.Write(SAMSettings.SELECTED_ACCOUNT_INDEX, (encryptedAccounts.Count + 1).ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.File.Write(SAMSettings.LOGIN_SELECTED_ACCOUNT, true.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.File.Write(SAMSettings.LOGIN_RECENT_ACCOUNT, false.ToString(), SAMSettings.SECTION_AUTOLOG);
                    settings.User.LoginSelectedAccount = true;
                    settings.User.LoginRecentAccount = false;
                    settings.User.SelectedAccountIndex = encryptedAccounts.Count + 1;
                }

                try
                {
                    Account newAccount = new Account() { Name = dialog.AccountText, Alias = dialog.AliasText, Password = StringCipher.Encrypt(password, eKey), SharedSecret = StringCipher.Encrypt(sharedSecret, eKey), ProfUrl = dialog.UrlText, AviUrl = aviUrl, SteamId = steamId, Description = dialog.DescriptionText };

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
            var dialog = new TextDialog
            {
                AccountText = decryptedAccounts[index].Name,
                AliasText = decryptedAccounts[index].Alias,
                PasswordText = decryptedAccounts[index].Password,
                SharedSecretText = decryptedAccounts[index].SharedSecret,
                UrlText = decryptedAccounts[index].ProfUrl,
                DescriptionText = decryptedAccounts[index].Description
            };

            // Reload slected boolean
            settings.User.LoginSelectedAccount = settings.File.Read(SAMSettings.LOGIN_SELECTED_ACCOUNT, SAMSettings.SECTION_AUTOLOG) == true.ToString() ? true : false;

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
                    aviUrl = await Utils.HtmlAviScrapeAsync(dialog.UrlText);
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
                    encryptedAccounts[index].Description = dialog.DescriptionText;

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

        private void Login(int index, int tryCount)
        {
            if (tryCount == maxRetry)
            {
                MessageBox.Show("Login Failed! Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Utils.AccountHasActiveTimeout(encryptedAccounts[index]))
            { 
                MessageBoxResult result = MessageBox.Show("Account timeout is active!\nLogin anyway?", "Timeout", MessageBoxButton.YesNo, MessageBoxImage.Warning, 0, MessageBoxOptions.DefaultDesktopOnly);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            foreach (Thread loginThread in loginThreads)
            {
                loginThread.Abort();
            }

            // Update the most recently used account index.
            settings.User.RecentAccountIndex = index;
            settings.File.Write(SAMSettings.RECENT_ACCOUNT_INDEX, index.ToString(), SAMSettings.SECTION_AUTOLOG);

            // Verify Steam file path.
            settings.User.SteamPath = Utils.CheckSteamPath();

            if (!settings.User.SandboxMode)
            {
                // Shutdown Steam process via command if it is already open.
                ProcessStartInfo stopInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = settings.User.SteamPath + "steam.exe",
                    WorkingDirectory = settings.User.SteamPath,
                    Arguments = "-shutdown"
                };

                try
                {
                    Process SteamProc = Process.GetProcessesByName("Steam")[0];
                    Process.Start(stopInfo);
                    SteamProc.WaitForExit();
                }
                catch
                {
                    Console.WriteLine("No steam process found or steam failed to shutdown.");
                }
            }

            // Make sure Username field is empty and Remember Password checkbox is unchecked.
            if (!settings.User.Login)
            {
                Utils.ClearAutoLoginUserKeyValues();
            }

            StringBuilder parametersBuilder = new StringBuilder();

            if (settings.User.CustomParameters)
            {
                parametersBuilder.Append(settings.User.CustomParametersValue).Append(" ");
            }
            
            foreach (string parameter in launchParameters)
            {
                parametersBuilder.Append(parameter).Append(" ");

                if (parameter.Equals("-login"))
                {
                    StringBuilder passwordBuilder = new StringBuilder();

                    foreach (char c in decryptedAccounts[index].Password)
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

                    parametersBuilder.Append(decryptedAccounts[index].Name).Append(" \"").Append(passwordBuilder.ToString()).Append("\" ");
                }
            }

            // Start Steam process with the selected path.
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = settings.User.SteamPath + "steam.exe",
                WorkingDirectory = settings.User.SteamPath,
                Arguments = parametersBuilder.ToString()
            }; 

            try
            {
                Process steamProcess = Process.Start(startInfo);
            }
            catch (Exception m)
            {
                MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (settings.User.Login == true)
            {
                if (settings.User.RememberPassword == true)
                {
                    Utils.SetRememeberPasswordKeyValue(1);
                }

                if (decryptedAccounts[index].SharedSecret != null && decryptedAccounts[index].SharedSecret.Length > 0)
                {
                    Task.Run(() => Type2FA(index, 0));
                }
                else 
                {
                    PostLogin();
                }
            }
            else
            {
                Task.Run(() => TypeCredentials(index, 0));
            }
        }

        private void TypeCredentials(int index, int tryCount)
        {
            loginThreads.Add(Thread.CurrentThread);

            WindowHandle steamLoginWindow = Utils.GetSteamLoginWindow();

            while (!steamLoginWindow.IsValid)
            {
                Thread.Sleep(10);
                steamLoginWindow = Utils.GetSteamLoginWindow();
            }

            // Debug
            //StringBuilder windowTitleBuilder = new StringBuilder(Utils.GetWindowTextLength(steamLoginWindow.RawPtr) + 1);
            //Utils.GetWindowText(steamLoginWindow.RawPtr, windowTitleBuilder, windowTitleBuilder.Capacity);

            Process steamLoginProcess = Utils.WaitForSteamProcess(steamLoginWindow);
            steamLoginProcess.WaitForInputIdle();

            Thread.Sleep(settings.User.SleepTime);
            Utils.SetForegroundWindow(steamLoginWindow.RawPtr);
            Thread.Sleep(100);

            // Enable Caps-Lock, to prevent IME problems.
            bool capsLockEnabled = System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock);
            if (settings.User.HandleMicrosoftIME && !settings.User.IME2FAOnly && !capsLockEnabled)
            {
                Utils.SendCapsLockGlobally();
            }

            foreach (char c in decryptedAccounts[index].Name.ToCharArray())
            {
                Utils.SetForegroundWindow(steamLoginWindow.RawPtr);
                Thread.Sleep(10);
                Utils.SendCharacter(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod, c);
            }

            Thread.Sleep(100);
            Utils.SendTab(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);
            Thread.Sleep(100);

            foreach (char c in decryptedAccounts[index].Password.ToCharArray())
            {
                Utils.SetForegroundWindow(steamLoginWindow.RawPtr);
                Thread.Sleep(10);
                Utils.SendCharacter(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod, c);
            }

            if (settings.User.RememberPassword)
            {
                Utils.SetForegroundWindow(steamLoginWindow.RawPtr);

                Thread.Sleep(100);
                Utils.SendTab(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);
                Thread.Sleep(100);
                Utils.SendSpace(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);
            }

            Utils.SetForegroundWindow(steamLoginWindow.RawPtr);

            Thread.Sleep(100);
            Utils.SendEnter(steamLoginWindow.RawPtr, settings.User.VirtualInputMethod);

            // Restore CapsLock back if CapsLock is off before we start typing.
            if (settings.User.HandleMicrosoftIME && !settings.User.IME2FAOnly && !capsLockEnabled)
            {
                Utils.SendCapsLockGlobally();
            }

            int waitCount = 0;

            // Only handle 2FA if shared secret was entered.
            if (decryptedAccounts[index].SharedSecret != null && decryptedAccounts[index].SharedSecret.Length > 0)
            {
                WindowHandle steamGuardWindow = Utils.GetSteamGuardWindow();

                while (!steamGuardWindow.IsValid && waitCount < maxRetry)
                {
                    Thread.Sleep(settings.User.SleepTime);

                    steamGuardWindow = Utils.GetSteamGuardWindow();

                    // Check for Steam warning window.
                    var steamWarningWindow = Utils.GetSteamWarningWindow();
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

                Type2FA(index, 0);
            }
            else
            {
                PostLogin();
            }
        }

        private void Type2FA(int index, int tryCount)
        {
            // Need both the Steam Login and Steam Guard windows.
            // Can't focus the Steam Guard window directly.
            var steamLoginWindow = Utils.GetSteamLoginWindow();
            var steamGuardWindow = Utils.GetSteamGuardWindow();

            while (!steamLoginWindow.IsValid || !steamGuardWindow.IsValid)
            {
                Thread.Sleep(10);
                steamLoginWindow = Utils.GetSteamLoginWindow();
                steamGuardWindow = Utils.GetSteamGuardWindow();

                // Check for Steam warning window.
                var steamWarningWindow = Utils.GetSteamWarningWindow();
                if (steamWarningWindow.IsValid)
                {
                    //Cancel the 2FA process since Steam connection is likely unavailable. 
                    return;
                }
            }

            Console.WriteLine("Found windows.");

            Process steamGuardProcess = Utils.WaitForSteamProcess(steamGuardWindow);
            steamGuardProcess.WaitForInputIdle();

            // Wait a bit for the window to fully initialize just in case.
            Thread.Sleep(settings.User.SleepTime);

            // Generate 2FA code, then send it to the client.
            Console.WriteLine("It is idle now, typing code...");

            Utils.SetForegroundWindow(steamGuardWindow.RawPtr);

            // Enable Caps-Lock, to prevent IME problems.
            bool capsLockEnabled = System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock);
            if (settings.User.HandleMicrosoftIME && !capsLockEnabled)
            {
                Utils.SendCapsLockGlobally();
            }

            Thread.Sleep(10);

            foreach (char c in Generate2FACode(decryptedAccounts[index].SharedSecret).ToCharArray())
            {
                Utils.SetForegroundWindow(steamGuardWindow.RawPtr);
                Thread.Sleep(10);

                // Can also send keys to login window handle, but nothing works unless it is the foreground window.
                Utils.SendCharacter(steamGuardWindow.RawPtr, settings.User.VirtualInputMethod, c);
            }

            Utils.SetForegroundWindow(steamGuardWindow.RawPtr);

            Thread.Sleep(10);

            Utils.SendEnter(steamGuardWindow.RawPtr, settings.User.VirtualInputMethod);

            // Restore CapsLock back if CapsLock is off before we start typing.
            if (settings.User.HandleMicrosoftIME && !capsLockEnabled)
            {
                Utils.SendCapsLockGlobally();
            }

            // Need a little pause here to more reliably check for popup later.
            Thread.Sleep(settings.User.SleepTime);

            // Check if we still have a 2FA popup, which means the previous one failed.
            steamGuardWindow = Utils.GetSteamGuardWindow();

            if (tryCount < maxRetry && steamGuardWindow.IsValid)
            {
                Console.WriteLine("2FA code failed, retrying...");
                Type2FA(index, tryCount + 1);
                return;
            }
            else if (tryCount == maxRetry && steamGuardWindow.IsValid)
            {
                MessageBoxResult result = MessageBox.Show("2FA Failed\nPlease wait or bring the Steam Guard\nwindow to the front before clicking OK", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);

                if (result == MessageBoxResult.OK)
                {
                    Type2FA(index, tryCount + 1);
                }
            }
            else if (tryCount == maxRetry + 1 && steamGuardWindow.IsValid)
            {
                MessageBox.Show("2FA Failed\nPlease verify your shared secret is correct!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PostLogin();
        }

        private void PostLogin()
        {
            if (settings.User.ClearUserData == true)
            {
                Utils.ClearSteamUserDataFolder(settings.User.SteamPath, settings.User.SleepTime, maxRetry);
            }
            if (settings.User.CloseOnLogin == true)
            {
                Dispatcher.Invoke(delegate () { Close(); });
            }
        }

        private string Generate2FACode(string shared_secret)
        {
            SteamGuardAccount authAccount = new SteamGuardAccount { SharedSecret = shared_secret };
            string code = authAccount.GenerateSteamGuardCode();
            return code;
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
            if (IsPasswordProtected() == true && ePassword.Length > 0)
            {
                Utils.PasswordSerialize(encryptedAccounts, ePassword);
            }
            else
            {
                Utils.Serialize(encryptedAccounts);
            }

            RefreshWindow();
        }

        private void ExportAccount(int index)
        {
            Utils.ExportSelectedAccounts(new List<Account>() { encryptedAccounts[index] });
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
                _RatioHeight = ((this.Height - _Height) / 5) * -1;
                _RatioWidth = ((this.Width - _Width) / 5) * -1;
            }
            _Stop++;

            this.Height += _RatioHeight;
            this.Width += _RatioWidth;

            if (_Stop == 5)
            {
                _Timer.Stop();
                _Timer.Enabled = false;
                _Timer.Dispose();

                _Stop = 0;

                this.Height = _Height;
                this.Width = _Width;

                SetMainScrollViewerBarsVisibility(ScrollBarVisibility.Auto);
            }
        }

        #endregion

        #region Click Events

        private void AccountButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        private void AccountButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                holdingButton = null;
                btn.Opacity = 1;
                dragging = false;
            }
        }

        private void AccountButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
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

        private void AccountButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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
                Login(index, 0);
            }
        }

        private void TaskbarIconLoginItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                Login(Int32.Parse(item.Tag.ToString()), 0);
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
                Utils.Serialize(encryptedAccounts);
                ePassword = "";
            }
            else if (settingsDialog.Password != null)
            {
                ePassword = settingsDialog.Password;

                if (previousPass != ePassword)
                {
                    Utils.PasswordSerialize(encryptedAccounts, ePassword);
                }
            }

            LoadSettings();
            RefreshWindow();
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
            if (await UpdateCheck.CheckForUpdate(updateCheckUrl, repositoryUrl) < 1)
            {
                MessageBox.Show(Process.GetCurrentProcess().ProcessName + " is up to date!");
            }
        }

        private void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RefreshWindow();
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
            Utils.ImportAccountFile();
            RefreshWindow();
        }

        private void ExportAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Utils.ExportAccountFile();
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
            switch (this.WindowState)
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

            RefreshWindow();
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
                    Utils.Deserialize(dataFile);
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
                timeoutLabel.Text = Utils.FormatTimespanString(timeLeft.Value);
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
                encryptedAccounts[index].TimeoutTimeLeft = Utils.FormatTimespanString(timeLeft.Value);
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
                    accountButton.PreviewMouseLeftButtonDown -= new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseDown);
                    accountButton.PreviewMouseLeftButtonUp -= new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseUp);
                    accountButton.MouseLeave -= new System.Windows.Input.MouseEventHandler(AccountButton_MouseLeave);
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
                Utils.ExportSelectedAccounts(exportAccounts.Values.ToList());
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

                        RefreshWindow();
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
                    accountButton.PreviewMouseLeftButtonDown -= new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseDown);
                    accountButton.PreviewMouseLeftButtonUp -= new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseUp);
                    accountButton.MouseLeave -= new System.Windows.Input.MouseEventHandler(AccountButton_MouseLeave);
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
                    accountButton.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseDown);
                    accountButton.PreviewMouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseUp);
                    accountButton.MouseLeave += new System.Windows.Input.MouseEventHandler(AccountButton_MouseLeave);

                    accountButton.Opacity = 1;
                }
            }

            deleting = false;
            exporting = false;
        }

        private void AccountsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AccountsDataGrid.SelectedItem != null && deleting == false)
            {
                Account account = AccountsDataGrid.SelectedItem as Account;
                int index = encryptedAccounts.FindIndex(a => a.Name == account.Name);
                Login(index, 0);
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

        private void AccountsDataGrid_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        private void MainScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
