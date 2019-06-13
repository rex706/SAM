using SteamAuth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using Win32Interop.WinHandles;
using System.Windows.Threading;
using System.Windows.Media.Effects;

namespace SAM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class Account
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Password { get; set; }

        public string SharedSecret { get; set; }

        public string ProfUrl { get; set; }

        public string AviUrl { get; set; }

        public string SteamId { get; set; }

        public DateTime Timeout { get; set; }

        public string Description { get; set; }
    }

    [Serializable]
    public partial class MainWindow : Window
    {
        #region Globals

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lp1, string lp2);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const int WM_SETTEXT = 0x000C;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_CHAR = 0x0102;

        public const int VK_RETURN = 0x0D;

        public static List<Account> encryptedAccounts;
        private static List<Account> decryptedAccounts;
        private static Dictionary<int, Account> exportAccounts;

        private static List<Thread> loginThreads;
        private static List<System.Timers.Timer> timeoutTimers;

        private static string updateCheckUrl = "https://raw.githubusercontent.com/rex706/SAM/master/latest.txt";

        // Keys are changed before releases/updates
        private static string eKey = "PRIVATE_KEY";
        private static string ePassword = "";

        private static string account;

        private static string accPerRow = "0";
        private static string steamPath;

        private static bool selected = false;
        private static int selectedAcc = -1;
        private static bool recent = false;
        private static int recentAcc = -1;

        private static string AssemblyVer;

        private static bool exporting = false;

        private static Button holdingButton = null;
        private static bool dragging = false;
        private static System.Timers.Timer mouseHoldTimer;

        IniFile settingsFile;

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

            // Check for a new version.
            if (await UpdateCheck.CheckForUpdate(updateCheckUrl) == 1)
            {
                // An update is available, but user has chosen not to update.
                ver.Header = "Update Available!";
                ver.Click += Ver_Click;
                ver.IsEnabled = true;
            }

            // If no settings file exists, create one and initialize values.
            if (!File.Exists("SAMSettings.ini"))
            {
                settingsFile = new IniFile("SAMSettings.ini");
                settingsFile.Write("Version", AssemblyVer, "System");
                settingsFile.Write("AccountsPerRow", "5", "Settings");
                settingsFile.Write("StartWithWindows", "false", "Settings");
                settingsFile.Write("StartMinimized", "false", "Settings");
                settingsFile.Write("MinimizeToTray", "false", "Settings");
                settingsFile.Write("Recent", "false", "AutoLog");
                settingsFile.Write("RecentAcc", "", "AutoLog");
                settingsFile.Write("Selected", "false", "AutoLog");
                settingsFile.Write("SelectedAcc", "", "AutoLog");
                accPerRow = "5";

                MessageBoxResult messageBoxResult = MessageBox.Show("Do you want to password protect SAM?", "Protect", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    settingsFile.Write("PasswordProtect", VerifyAndSetPassword(), "Settings");
                }
            }
            // Else load settings from preexisting file.
            else
            {
                LoadSettings();
            }

            loginThreads = new List<Thread>();

            // Load window with account buttons.
            RefreshWindow();

            // Login to auto log account if enabled and steam is not already open.
            Process[] SteamProc = Process.GetProcessesByName("Steam");

            if (SteamProc.Length == 0)
            {
                if (recent == true)
                    Login(recentAcc);
                else if (selected == true)
                    Login(selectedAcc);
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

                    return "true";
                }
                else if (passwordDialog.PasswordText == "")
                {
                    messageBoxResult = MessageBox.Show("No password detected, are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }
            }

            return "false";
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
                        encryptedAccounts = Utils.PasswordDeserialize("info.dat", passwordDialog.PasswordText);
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
                            VerifyPassword();
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

        private void LoadSettings()
        {
            settingsFile = new IniFile("SAMSettings.ini");

            if (!settingsFile.KeyExists("AccountsPerRow", "Settings"))
            {
                settingsFile.Write("AccountsPerRow", "5", "Settings");
                accPerRow = "5";
            }
            else
            {
                accPerRow = settingsFile.Read("AccountsPerRow", "Settings");

                if (!Regex.IsMatch(accPerRow, @"^\d+$") || Int32.Parse(accPerRow) < 1)
                {
                    settingsFile.Write("AccountsPerRow", "5", "Settings");
                    accPerRow = "5";
                }
            }

            Utils.CheckSteamPath();

            // If the recent autolog entry exists and is set to true.
            // else create defualt settings file entry.
            if (settingsFile.KeyExists("Recent", "AutoLog") && settingsFile.Read("Recent", "AutoLog").ToLower().Equals("true"))
            {
                int tryParseResult = -1;
                Int32.TryParse(settingsFile.Read("RecentAcc", "AutoLog"), out tryParseResult);

                if (tryParseResult != -1)
                {
                    recent = true;
                    recentAcc = tryParseResult;
                }
                else
                {
                    settingsFile.Write("Recent", "false", "AutoLog");
                }
            }
            else if (!settingsFile.KeyExists("Recent", "AutoLog"))
            {
                settingsFile.Write("Recent", "false", "AutoLog");
                settingsFile.Write("RecentAcc", "-1", "AutoLog");
            }

            // If the selected autolog entry exists and is set to true.
            // else create defualt settings file entry.
            if (settingsFile.KeyExists("Selected", "AutoLog") && settingsFile.KeyExists("SelectedAcc", "AutoLog") && settingsFile.Read("Selected", "AutoLog").ToLower().Equals("true"))
            {
                int tryParseResult = -1;
                Int32.TryParse(settingsFile.Read("SelectedAcc", "AutoLog"), out tryParseResult);

                if (tryParseResult != -1)
                {
                    selected = true;
                    selectedAcc = tryParseResult;
                }
                else
                {
                    settingsFile.Write("Selected", "false", "AutoLog");
                }
            }
            else if (!settingsFile.KeyExists("Selected", "AutoLog"))
            {
                settingsFile.Write("Selected", "false", "AutoLog");
                settingsFile.Write("SelectedAcc", "-1", "AutoLog");
            }

            if (settingsFile.KeyExists("MinimizeToTray", "Settings") && settingsFile.Read("MinimizeToTray", "Settings").ToLower().Equals("true"))
            {

            }
            else
            {
                settingsFile.Write("MinimizeToTray", "false", "Settings");
            }

            if (settingsFile.KeyExists("StartMinimized", "Settings") && settingsFile.Read("StartMinimized", "Settings").ToLower().Equals("true"))
            {
                WindowState = WindowState.Minimized;
            }
            else
            {
                settingsFile.Write("StartMinimized", "false", "Settings");
            }

            if (settingsFile.KeyExists("PasswordProtect", "Settings") && settingsFile.Read("PasswordProtect", "Settings").ToLower().Equals("true") && (encryptedAccounts == null || encryptedAccounts.Count == 0))
            {
                VerifyAndSetPassword();
            }
            else if (!settingsFile.KeyExists("PasswordProtect", "Settings") || encryptedAccounts == null || encryptedAccounts.Count == 0)
            {
                settingsFile.Write("PasswordProtect", "false", "Settings");
            }

            settingsFile.Write("Version", AssemblyVer, "System");
        }

        public void RefreshWindow()
        {
            decryptedAccounts = new List<Account>();
            buttonGrid.Children.Clear();
            TaskBarIconLoginContextMenu.Items.Clear();
            TaskBarIconLoginContextMenu.IsEnabled = false;

            // Check if info.dat exists
            if (File.Exists("info.dat"))
            {
                // Deserialize file

                if (ePassword.Length > 0)
                {
                    MessageBoxResult messageBoxResult = MessageBoxResult.OK;

                    while (messageBoxResult == MessageBoxResult.OK)
                    {
                        try
                        {
                            encryptedAccounts = Utils.PasswordDeserialize("info.dat", ePassword);
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
                    encryptedAccounts = Utils.Deserialize("info.dat");
                }

                PostDeserializedRefresh(true);
            }
            else
            {
                encryptedAccounts = new List<Account>();
            }
        }

        public async Task ReloadAccount(Account account)
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
                account.AviUrl = Utils.HtmlAviScrape(account.ProfUrl);
            }
        }

        public async Task ReloadAccountsAsync()
        {
            foreach (var account in encryptedAccounts)
            {
                await ReloadAccount(account);
            }

            Utils.Serialize(encryptedAccounts);

            RefreshWindow();

            MessageBox.Show("Done!");
        }

        private void PostDeserializedRefresh(bool seedAcc)
        {
            // Dispose and reinitialize timers each time grid is refreshed as to not clog up more resources than necessary. 
            if (timeoutTimers != null)
            {
                foreach (System.Timers.Timer timer in timeoutTimers)
                {
                    timer.Dispose();
                }
            }

            timeoutTimers = new List<System.Timers.Timer>();

            if (encryptedAccounts != null)
            {
                int bCounter = 0;
                int xCounter = 0;
                int yCounter = 0;

                int height = 100;
                int width = 100;

                double heightOffset = height + (height * 0.2);
                double widthOffset = width + (width * 0.2);

                // Create new button and textblock for each account
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

                    Button accountButton = new Button();
                    TextBlock accountText = new TextBlock();
                    TextBlock timeoutTextBlock = new TextBlock();

                    accountButton.Style = (Style)Resources["SAMButtonStyle"];

                    accountButton.Tag = bCounter.ToString();

                    //accountButton.Name = account.Name;
                    //accountText.Name = account.Name + "Label";

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
                        accountButton.ToolTip = account.Description;

                    accountButton.Height = height;
                    accountButton.Width = width;
                    accountText.Height = 15;
                    accountText.Width = 100;
                    timeoutTextBlock.Height = 15;
                    timeoutTextBlock.Width = 100;

                    accountButton.HorizontalAlignment = HorizontalAlignment.Left;
                    accountButton.VerticalAlignment = VerticalAlignment.Top;
                    accountText.HorizontalAlignment = HorizontalAlignment.Left;
                    accountText.VerticalAlignment = VerticalAlignment.Top;
                    timeoutTextBlock.HorizontalAlignment = HorizontalAlignment.Left;
                    timeoutTextBlock.VerticalAlignment = VerticalAlignment.Top;

                    accountButton.Margin = new Thickness(15 + (xCounter * widthOffset), (yCounter * heightOffset) + 14, 0, 0);
                    accountText.Margin = new Thickness(15 + (xCounter * widthOffset), (yCounter * heightOffset) + 113, 0, 0);
                    timeoutTextBlock.Margin = new Thickness(15 + (xCounter * widthOffset), (yCounter * heightOffset) + 95, 0, 0);
                    timeoutTextBlock.Padding = new Thickness(13, 0, 0, 0);

                    accountButton.BorderBrush = null;
                    accountText.Foreground = new SolidColorBrush(Colors.White);

                    timeoutTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    timeoutTextBlock.Background = new SolidColorBrush(new Color { A = 100, R = 0, G = 0, B = 0 });
                    timeoutTextBlock.Effect = new DropShadowEffect
                    {
                        Color = new Color { A = 255, R = 0, G = 0, B = 0 },
                        Direction = 320,
                        ShadowDepth = 0,
                        Opacity = 1
                    };

                    if (account.ProfUrl == "" || account.AviUrl == null || account.AviUrl == "" || account.AviUrl == " ")
                    {
                        accountButton.Content = account.Name;
                        accountButton.Background = Brushes.LightGray;
                    }
                    else
                    {
                        try
                        {
                            ImageBrush brush1 = new ImageBrush();
                            BitmapImage image1 = new BitmapImage(new Uri(account.AviUrl));
                            brush1.ImageSource = image1;
                            accountButton.Background = brush1;
                            buttonGrid.Children.Add(accountText);
                        }
                        catch (Exception m)
                        {
                            // Probably no internet connection or avatar url is bad
                            Console.WriteLine("Error: " + m.Message);

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

                    buttonGrid.Children.Add(accountButton);

                    accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                    accountButton.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseDown);
                    accountButton.PreviewMouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseUp);
                    //accountButton.PreviewMouseMove += new System.Windows.Input.MouseEventHandler(AccountButton_MouseMove);
                    accountButton.MouseLeave += new System.Windows.Input.MouseEventHandler(AccountButton_MouseLeave);

                    ContextMenu accountContext = new ContextMenu();

                    MenuItem deleteItem = new MenuItem();
                    MenuItem editItem = new MenuItem();
                    MenuItem exportItem = new MenuItem();
                    MenuItem reloadItem = new MenuItem();
                    MenuItem setTimeoutItem = new MenuItem();
                    MenuItem clearTimeoutItem = new MenuItem();
                    MenuItem copyPasswordItem = new MenuItem();

                    deleteItem.Header = "Delete";
                    editItem.Header = "Edit";
                    exportItem.Header = "Export";
                    reloadItem.Header = "Reload";
                    setTimeoutItem.Header = "Set Timeout";
                    clearTimeoutItem.Header = "Clear Timeout";
                    copyPasswordItem.Header = "Copy Password";

                    if (account.Timeout == null || account.Timeout == new DateTime())
                    {
                        clearTimeoutItem.IsEnabled = false;
                    }
                    else
                    {
                        // Set up timer event to update timeout label
                        var timeLeft = account.Timeout - DateTime.Now;

                        System.Timers.Timer timeoutTimer = new System.Timers.Timer();
                        timeoutTimers.Add(timeoutTimer);

                        timeoutTimer.Elapsed += delegate
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                TimeoutTimer_Tick(Int32.Parse(accountButton.Tag.ToString()), timeoutTextBlock, timeoutTimer);
                            });
                        };
                        timeoutTimer.Interval = 1000;
                        timeoutTimer.Enabled = true;
                        timeoutTextBlock.Text = Utils.FormatTimespanString(timeLeft);
                        timeoutTextBlock.Visibility = Visibility.Visible;

                        buttonGrid.Children.Add(timeoutTextBlock);
                    }

                    accountContext.Items.Add(editItem);
                    accountContext.Items.Add(deleteItem);
                    accountContext.Items.Add(exportItem);
                    accountContext.Items.Add(reloadItem);
                    accountContext.Items.Add(setTimeoutItem);
                    accountContext.Items.Add(clearTimeoutItem);
                    accountContext.Items.Add(copyPasswordItem);

                    accountButton.ContextMenu = accountContext;
                    accountButton.ContextMenuOpening += new ContextMenuEventHandler(ContextMenu_ContextMenuOpening);

                    deleteItem.Click += delegate { DeleteEntry(accountButton); };
                    editItem.Click += delegate { EditEntry(accountButton); };
                    exportItem.Click += delegate { ExportAccount(Int32.Parse(accountButton.Tag.ToString())); };
                    reloadItem.Click += async delegate { await ReloadAccount_ClickAsync(Int32.Parse(accountButton.Tag.ToString())); };
                    setTimeoutItem.Click += delegate { AccountButtonSetTimeout_Click(Int32.Parse(accountButton.Tag.ToString())); };
                    clearTimeoutItem.Click += delegate { AccountButtonClearTimeout_Click(Int32.Parse(accountButton.Tag.ToString())); };
                    copyPasswordItem.Click += delegate { copyPasswordToClipboard(Int32.Parse(accountButton.Tag.ToString())); };

                    // TaskbarIcon Context Menu Item
                    MenuItem taskBarIconLoginItem = new MenuItem();
                    taskBarIconLoginItem.Tag = bCounter.ToString();
                    taskBarIconLoginItem.Click += new RoutedEventHandler(TaskbarIconLoginItem_Click);

                    if (account.Alias != null && account.Alias.Length > 0)
                    {
                        taskBarIconLoginItem.Header = account.Alias;
                    }
                    else
                    {
                        taskBarIconLoginItem.Header = account.Name;
                    }

                    TaskBarIconLoginContextMenu.IsEnabled = true;
                    TaskBarIconLoginContextMenu.Items.Add(taskBarIconLoginItem);

                    bCounter++;
                    xCounter++;

                    if ((xCounter % Int32.Parse(accPerRow) == 0) && xCounter != 0)
                    {
                        yCounter++;
                        xCounter = 0;
                    }
                }

                int xVal = 0;
                int newHeight;

                // Adjust window size and info positions
                if (yCounter == 0)
                {
                    xVal = xCounter + 1;
                    newHeight = 190;
                    buttonGrid.Height = 141;
                }
                else
                {
                    xVal = Int32.Parse(accPerRow);
                    newHeight = 185 + (120 * yCounter);
                    buttonGrid.Height = 141 * (120 + yCounter);
                }

                int newWidth = (xVal * 120) + 25;

                Resize(newHeight, newWidth);
                buttonGrid.Width = newWidth;

                // Adjust new account and export buttons
                Thickness newThickness = new Thickness(33 + (xCounter * widthOffset), (yCounter * heightOffset) + 52, 0, 0);
                Thickness offsetThickness = new Thickness(33 + (xCounter * widthOffset), (yCounter * heightOffset) + 92, 0, 0);

                NewButton.Margin = newThickness;
                ExportButton.Margin = newThickness;
                CancelExportButton.Margin = offsetThickness;
            }
        }

        private void NewAccount()
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
                    aviUrl = Utils.HtmlAviScrape(dialog.UrlText);
                }

                string steamId = dialog.SteamId;

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settingsFile.Write("SelectedAcc", (encryptedAccounts.Count + 1).ToString(), "AutoLog");
                    settingsFile.Write("Selected", "true", "AutoLog");
                    settingsFile.Write("Recent", "false", "AutoLog");
                    selected = true;
                    recent = false;
                    selectedAcc = encryptedAccounts.Count + 1;
                }

                try
                {
                    encryptedAccounts.Add(new Account() { Name = dialog.AccountText, Alias = dialog.AliasText, Password = StringCipher.Encrypt(password, eKey), SharedSecret = StringCipher.Encrypt(sharedSecret, eKey), ProfUrl = dialog.UrlText, AviUrl = aviUrl, SteamId = steamId, Description = dialog.DescriptionText });

                    Utils.Serialize(encryptedAccounts);

                    RefreshWindow();
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    var itemToRemove = encryptedAccounts.Single(r => r.Name == dialog.AccountText);
                    encryptedAccounts.Remove(itemToRemove);

                    Utils.Serialize(encryptedAccounts);

                    NewAccount();
                }
            }
        }

        private void EditEntry(object butt)
        {
            Button button = butt as Button;
            int index = Int32.Parse(button.Tag.ToString());

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
            selected = settingsFile.Read("Selected", "AutoLog") == "true" ? true : false;

            if (selected == true && selectedAcc == index)
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
                    aviUrl = Utils.HtmlAviScrape(dialog.UrlText);
                }

                string steamId = dialog.SteamId;

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settingsFile.Write("SelectedAcc", button.Tag.ToString(), "AutoLog");
                    settingsFile.Write("Selected", "true", "AutoLog");
                    settingsFile.Write("Recent", "false", "AutoLog");
                    selected = true;
                    recent = false;
                    selectedAcc = index;
                }
                else
                {
                    settingsFile.Write("SelectedAcc", "-1", "AutoLog");
                    settingsFile.Write("Selected", "false", "AutoLog");
                    selected = false;
                    selectedAcc = -1;
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

                    Utils.Serialize(encryptedAccounts);
                    RefreshWindow();
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    EditEntry(butt);
                }
            }
        }

        private void DeleteEntry(object butt)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this entry?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (result == MessageBoxResult.Yes)
            {
                Button button = butt as Button;
                encryptedAccounts.RemoveAt(Int32.Parse(button.Tag.ToString()));
                Utils.Serialize(encryptedAccounts);
                RefreshWindow();
            }
        }

        private void Login(int index)
        {
            foreach (Thread loginThread in loginThreads)
            {
                loginThread.Abort();
            }

            // Update the most recently used account index.
            recentAcc = index;
            settingsFile.Write("RecentAcc", index.ToString(), "AutoLog");

            steamPath = Utils.CheckSteamPath();

            // Shutdown Steam process via command if it is already open.
            ProcessStartInfo stopInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = true,
                FileName = steamPath + "steam.exe",
                WorkingDirectory = steamPath,
                WindowStyle = ProcessWindowStyle.Hidden,
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

            // Start Steam process with the selected path.
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = true,
                FileName = steamPath + "steam.exe",
                WorkingDirectory = steamPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = "-login " + decryptedAccounts[index].Name + " " + decryptedAccounts[index].Password
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

            // Only handle 2FA if shared secret was entered.
            if (decryptedAccounts[index].SharedSecret != null && decryptedAccounts[index].SharedSecret.Length > 0)
            {
                Task.Run(() => Type2FA(index, 0));
            }
        }

        private void Type2FA(int index, int tryCount)
        {
            loginThreads.Add(Thread.CurrentThread);

            // Need both the Steam Login and Steam Guard windows.
            // Can't focus the Steam Guard window directly.
            var steamLoginWindow = TopLevelWindowUtils.FindWindow(wh => wh.GetWindowText().Contains("Steam") && !wh.GetWindowText().Contains("-") && !wh.GetWindowText().Contains("—"));
            var steamGuardWindow = TopLevelWindowUtils.FindWindow(wh => wh.GetWindowText().StartsWith("Steam Guard - ") || wh.GetWindowText().StartsWith("Steam Guard — "));

            while (!steamLoginWindow.IsValid || !steamGuardWindow.IsValid)
            {
                Thread.Sleep(10);
                steamLoginWindow = TopLevelWindowUtils.FindWindow(wh => wh.GetWindowText().Contains("Steam") && !wh.GetWindowText().Contains("-") && !wh.GetWindowText().Contains("—"));
                steamGuardWindow = TopLevelWindowUtils.FindWindow(wh => wh.GetWindowText().StartsWith("Steam Guard - ") || wh.GetWindowText().StartsWith("Steam Guard — "));

                // Check for Steam warning window.
                var steamWarningWindow = TopLevelWindowUtils.FindWindow(wh => wh.GetWindowText().StartsWith("Steam - ") || wh.GetWindowText().StartsWith("Steam — "));
                if (steamWarningWindow.IsValid)
                {
                    //Cancel the 2FA process since Steam connection is likely unavailable. 
                    return;
                }
            }

            Console.WriteLine("Found windows.");

            Process steamGuardProcess = null;

            // Wait for valid process to wait for input idle.
            Console.WriteLine("Waiting for it to be idle.");
            while (steamGuardProcess == null)
            {
                int procId = 0;
                GetWindowThreadProcessId(steamGuardWindow.RawPtr, out procId);

                // Wait for valid process id from handle.
                while (procId == 0)
                {
                    Thread.Sleep(10);
                    GetWindowThreadProcessId(steamGuardWindow.RawPtr, out procId);
                }

                try
                {
                    steamGuardProcess = Process.GetProcessById(procId);
                }
                catch
                {
                    steamGuardProcess = null;
                }
            }

            steamGuardProcess.WaitForInputIdle();

            // Wait a bit for the window to fully initialize just in case.
            Thread.Sleep(2000);

            // Generate 2FA code, then send it to the client
            Console.WriteLine("It is idle now, typing code...");

            SetForegroundWindow(steamLoginWindow.RawPtr);
            //SetActiveWindow(steamLoginWindow.RawPtr);

            Thread.Sleep(10);

            foreach (char c in Generate2FACode(decryptedAccounts[index].SharedSecret).ToCharArray())
            {
                SetForegroundWindow(steamLoginWindow.RawPtr);
                //SetActiveWindow(steamLoginWindow.RawPtr);

                Thread.Sleep(10);

                // Can also send keys to login window handle, but nothing works unless it is the foreground window.
                System.Windows.Forms.SendKeys.SendWait(c.ToString());
                //SendMessage(steamGuardWindow.RawPtr, WM_CHAR, c, IntPtr.Zero);
                //PostMessage(steamGuardWindow.RawPtr, WM_CHAR, (IntPtr)c, IntPtr.Zero);
            }

            SetForegroundWindow(steamLoginWindow.RawPtr);
            //SetActiveWindow(steamLoginWindow.RawPtr);

            Thread.Sleep(10);

            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            //SendMessage(steamGuardWindow.RawPtr, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
            //PostMessage(steamGuardWindow.RawPtr, WM_KEYDOWN, (IntPtr)VK_RETURN, IntPtr.Zero);

            // Need a little pause here to more reliably check for popup later
            Thread.Sleep(3000);

            // Check if we still have a 2FA popup, which means the previous one failed.
            steamGuardWindow = TopLevelWindowUtils.FindWindow(wh => wh.GetWindowText().StartsWith("Steam Guard - ") || wh.GetWindowText().StartsWith("Steam Guard — "));

            if (tryCount < 2 && steamGuardWindow.IsValid)
            {
                Console.WriteLine("2FA code failed, retrying...");
                Type2FA(index, tryCount + 1);
            }
            else if (tryCount == 2 && steamGuardWindow.IsValid)
            {
                MessageBoxResult result = MessageBox.Show("2FA Failed\nPlease wait or bring the Steam Guard\nwindow to the front before clicking OK", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);

                if (result == MessageBoxResult.OK)
                {
                    Type2FA(index, tryCount + 1);
                }
            }
            else if (tryCount == 3 && steamGuardWindow.IsValid)
            {
                MessageBox.Show("2FA Failed\nPlease verify your shared secret is correct!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                Utils.Serialize(encryptedAccounts);
                RefreshWindow();
            }
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

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            NewAccount();
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Login with clicked button's index, which stored in Tag.

                int index = Int32.Parse(btn.Tag.ToString());

                if (!Utils.AccountHasActiveTimeout(encryptedAccounts[index]))
                {
                    Login(index);
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Account timeout is active!\nLogin anyway?", "Timeout", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        Login(index);
                    }
                }
            }
        }

        private void TaskbarIconLoginItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                Login(Int32.Parse(item.Tag.ToString()));
            }
        }

        private void AccountButtonSetTimeout_Click(int index)
        {
            // TODO: if account already has timeout enabled, disable it and return.
            // Otherwise display timeout window and calculate date/time of selection and save.

            var setTimeoutWindow = new SetTimeoutWindow(encryptedAccounts[index].Timeout);
            setTimeoutWindow.ShowDialog();

            if (setTimeoutWindow.timeout != null && setTimeoutWindow.timeout != new DateTime())
            {
                encryptedAccounts[index].Timeout = setTimeoutWindow.timeout;

            }
            else
            {

            }

            if (IsPasswordProtected())
            {
                Utils.PasswordSerialize(encryptedAccounts, ePassword);
            }
            else
            {
                Utils.Serialize(encryptedAccounts);
            }

            RefreshWindow();
        }

        private void AccountButtonClearTimeout_Click(int index)
        {
            encryptedAccounts[index].Timeout = new DateTime();

            if (IsPasswordProtected())
            {
                Utils.PasswordSerialize(encryptedAccounts, ePassword);
            }
            else
            {
                Utils.Serialize(encryptedAccounts);
            }

            RefreshWindow();
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

            Utils.Serialize(encryptedAccounts);

            RefreshWindow();

            MessageBox.Show("Done!");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsWindow();
            settingsDialog.ShowDialog();

            accPerRow = settingsDialog.ResponseText;

            string previousPass = ePassword;

            if (settingsDialog.Decrypt == true)
            {
                if (encryptedAccounts.Count > 0)
                {
                    Utils.Serialize(encryptedAccounts);
                }

                ePassword = "";
            }
            else if (settingsDialog.Password != null)
            {
                ePassword = settingsDialog.Password;

                if (encryptedAccounts.Count > 0 && previousPass != ePassword)
                {
                    Utils.PasswordSerialize(encryptedAccounts, ePassword);
                }
            }

            LoadSettings();
            RefreshWindow();
        }

        private void GitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/rex706/SAM");
        }

        private async void Ver_Click(object sender, RoutedEventArgs e)
        {
            if (await UpdateCheck.CheckForUpdate(updateCheckUrl) < 1)
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

        private void ExportSelectedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            exporting = true;

            exportAccounts = new Dictionary<int, Account>();

            NewButton.Visibility = Visibility.Hidden;
            ExportButton.Visibility = Visibility.Visible;
            CancelExportButton.Visibility = Visibility.Visible;
            FileMenuItem.IsEnabled = false;
            EditMenuItem.IsEnabled = false;

            IEnumerable<Button> buttonCollection = buttonGrid.Children.OfType<Button>();

            foreach (Button accountButton in buttonCollection)
            {
                accountButton.Style = (Style)Resources["ExportButtonStyle"];
                accountButton.Click -= new RoutedEventHandler(AccountButton_Click);
                accountButton.Click += new RoutedEventHandler(AccountButtonExport_Click);
                accountButton.PreviewMouseLeftButtonDown -= new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseDown);
                accountButton.PreviewMouseLeftButtonUp -= new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseUp);
                accountButton.MouseLeave -= new System.Windows.Input.MouseEventHandler(AccountButton_MouseLeave);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (exportAccounts.Count > 0)
            {
                Utils.ExportSelectedAccounts(exportAccounts.Values.ToList());
                ResetFromExport();
            }
            else
            {
                MessageBox.Show("No accounts selected to export!");
            }
        }

        private void CancelExportButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFromExport();
        }

        private void ShowWindowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            Focus();
        }

        private void copyPasswordToClipboard(int index)
        {
            Clipboard.SetText(decryptedAccounts[index].Password);
        }

        #endregion

        private void ResetFromExport()
        {
            NewButton.Visibility = Visibility.Visible;
            ExportButton.Visibility = Visibility.Hidden;
            CancelExportButton.Visibility = Visibility.Hidden;
            FileMenuItem.IsEnabled = true;
            EditMenuItem.IsEnabled = true;

            IEnumerable<Button> buttonCollection = buttonGrid.Children.OfType<Button>();

            foreach (Button accountButton in buttonCollection)
            {
                accountButton.Style = (Style)Resources["SAMButtonStyle"];
                accountButton.Click -= new RoutedEventHandler(AccountButtonExport_Click);
                accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                accountButton.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseDown);
                accountButton.PreviewMouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(AccountButton_MouseUp);
                accountButton.MouseLeave += new System.Windows.Input.MouseEventHandler(AccountButton_MouseLeave);

                accountButton.Opacity = 1;
            }

            exporting = false;
        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (exporting == true)
            {
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
                    if (settingsFile.KeyExists("MinimizeToTray", "Settings") && settingsFile.Read("MinimizeToTray", "Settings").ToLower().Equals("true"))
                    {
                        ShowInTaskbar = false;
                    }
                    break;
                case WindowState.Normal:
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
            return settingsFile.KeyExists("PasswordProtect", "Settings") && settingsFile.Read("PasswordProtect", "Settings").ToLower().Equals("true");
        }

        void TimeoutTimer_Tick(int index, TextBlock timeoutLabel, System.Timers.Timer timeoutTimer)
        {
            var timeLeft = encryptedAccounts[index].Timeout - DateTime.Now;

            if (timeLeft.CompareTo(TimeSpan.Zero) <= 0)
            {
                timeoutTimer.Stop();
                timeoutTimer.Dispose();

                timeoutLabel.Visibility = Visibility.Hidden;
                AccountButtonClearTimeout_Click(index);
            }
            else
            {
                timeoutLabel.Text = Utils.FormatTimespanString(timeLeft);
                timeoutLabel.Visibility = Visibility.Visible;
            }
        }
    }
}