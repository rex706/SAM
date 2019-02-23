using HtmlAgilityPack;
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
using System.Text;

namespace SAM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class Account
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public string SharedSecret { get; set; }

        public string ProfUrl { get; set; }

        public string AviUrl { get; set; }

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

        public static List<Account> encryptedAccounts;
        private static List<Account> decryptedAccounts;

        private static string eKey = "PRIVATE_KEY"; // This is changed before releases/updates

        private static string account;
        private static string ePassword;
        private static string eSharedSecret;

        private static string accPerRow;
        private static string steamPath;

        private static bool selected = false;
        private static int selectedAcc = -1;
        private static bool recent = false;
        private static int recentAcc = -1;

        private static string AssemblyVer;

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
            if (await UpdateCheck.CheckForUpdate("https://textuploader.com/58mva/raw") == 1)
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
                settingsFile.Write("StartWithWindows", "False", "Settings");
                settingsFile.Write("StartMinimized", "False", "Settings");
                settingsFile.Write("AccountsPerRow", "5", "Settings");
                settingsFile.Write("Recent", "False", "AutoLog");
                settingsFile.Write("RecentAcc","", "AutoLog");
                settingsFile.Write("Selected", "False", "AutoLog");
                settingsFile.Write("SelectedAcc", "", "AutoLog");
                accPerRow = "5";
            }
            // Else load settings from preexisting file.
            else
            {
                LoadSettings();
            }

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

        private void LoadSettings()
        {
            settingsFile = new IniFile("SAMSettings.ini");
            accPerRow = settingsFile.Read("AccountsPerRow", "Settings");

            if (!Regex.IsMatch(accPerRow, @"^\d+$") || Int32.Parse(accPerRow) < 1)
                accPerRow = "1";

            if (settingsFile.KeyExists("Steam", "Settings"))
                steamPath = settingsFile.Read("Steam", "Settings");

            // If the recent autolog entry exists and is set to true.
            // else create defualt settings file entry.
            if (settingsFile.KeyExists("Recent", "AutoLog") && settingsFile.Read("Recent", "AutoLog") == "True" && Int32.Parse(settingsFile.Read("RecentAcc", "AutoLog")) >= 0)
            {
                recent = true;
                recentAcc = Int32.Parse(settingsFile.Read("RecentAcc", "AutoLog"));
            }
            else if (!settingsFile.KeyExists("Recent", "AutoLog"))
            {
                settingsFile.Write("Recent", "False", "AutoLog");
                settingsFile.Write("RecentAcc", "-1", "AutoLog");
            }

            // If the selected autolog entry exists and is set to true.
            // else create defualt settings file entry.
            if (settingsFile.KeyExists("Selected", "AutoLog") && settingsFile.Read("Selected", "AutoLog") == "True")
            {
                selected = true;
                selectedAcc = Int32.Parse(settingsFile.Read("SelectedAcc", "AutoLog"));
            }
            else if (!settingsFile.KeyExists("Selected", "AutoLog"))
            {
                settingsFile.Write("Selected", "False", "AutoLog");
                settingsFile.Write("SelectedAcc", "-1", "AutoLog");
            }

            if (settingsFile.KeyExists("StartMinimized", "Settings") && settingsFile.Read("StartMinimized", "Settings") == "True")
            {
                WindowState = WindowState.Minimized;
            }
            else if (!settingsFile.KeyExists("StartMinimized", "Settings"))
            {
                settingsFile.Write("StartMinimized", "False", "Settings");
            }

            if (File.Exists("info.dat"))
            {
                StreamReader datReader = new StreamReader("info.dat");
                string temp = datReader.ReadLine();
                datReader.Close();

                // If the user is some how using an older info.dat, delete it.
                if (!temp.Contains("xml"))
                {
                    MessageBox.Show("Your info.dat is out of date and must be deleted.\nSorry for the inconvenience!", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Information);

                    try
                    {
                        File.Delete("info.dat");
                    }
                    catch (Exception m)
                    {
                        Console.WriteLine(m.Message);
                    }
                }
            }
            settingsFile.Write("Version", AssemblyVer, "System");
        }

        public void RefreshWindow()
        {
            decryptedAccounts = new List<Account>();
            buttonGrid.Children.Clear();

            // Check if info.dat exists
            if (File.Exists("info.dat"))
            {
                // Deserialize file
                encryptedAccounts = Utils.Deserialize("info.dat");
                PostDeserializedRefresh(true);
            }
            else
            {
                encryptedAccounts = new List<Account>();
            }
        }

        public void ReloadImages()
        {
            foreach (var account in encryptedAccounts)
            {
                account.AviUrl = HtmlAviScrape(account.ProfUrl);
            }

            Utils.Serialize(encryptedAccounts);

            RefreshWindow();

            MessageBox.Show("Done!");
        }

        private void PostDeserializedRefresh(bool seedAcc)
        {

            if (encryptedAccounts != null)
            {
                int bCounter = 0;
                int xcounter = 0;
                int ycounter = 0;

                int height = 100;
                int width = 100;

                double heightOffset = height + (height * 0.2);
                double widthOffset = width + (width * 0.2);

                // Create new button and textblock for each account
                foreach (var account in encryptedAccounts)
                {
                    string temppass = StringCipher.Decrypt(account.Password, eKey);

                    if (seedAcc)
                    {
                        if (account.SharedSecret != null && account.SharedSecret.Length > 0)
                        {
                            string temp2fa = StringCipher.Decrypt(account.SharedSecret, eKey);
                            decryptedAccounts.Add(new Account() { Name = account.Name, Password = temppass, SharedSecret = temp2fa, ProfUrl = account.ProfUrl, AviUrl = account.AviUrl, Description = account.Description });
                        }
                        else
                        {
                            decryptedAccounts.Add(new Account() { Name = account.Name, Password = temppass, ProfUrl = account.ProfUrl, AviUrl = account.AviUrl, Description = account.Description });
                        }
                    }

                    Button accountButton = new Button();
                    TextBlock accountText = new TextBlock();

                    accountButton.Style = (Style)Resources["MyButtonStyle"];

                    accountButton.Tag = bCounter.ToString();

                    //accountButton.Name = account.Name;
                    //accountText.Name = account.Name + "Label";
                    accountText.Text = account.Name;

                    // If there is a description, set up tooltip.
                    if (account.Description != null && account.Description.Length > 0)
                        accountButton.ToolTip = account.Description;

                    accountButton.Height = height;
                    accountButton.Width = width;
                    accountText.Height = 30;
                    accountText.Width = 100;

                    accountButton.HorizontalAlignment = HorizontalAlignment.Left;
                    accountButton.VerticalAlignment = VerticalAlignment.Top;
                    accountText.HorizontalAlignment = HorizontalAlignment.Left;
                    accountText.VerticalAlignment = VerticalAlignment.Top;

                    accountButton.Margin = new Thickness(15 + (xcounter * widthOffset), (ycounter * heightOffset) + 14, 0, 0);
                    accountText.Margin = new Thickness(15 + (xcounter * widthOffset), (ycounter * heightOffset) + 113, 0, 0);

                    accountButton.BorderBrush = null;
                    accountText.Foreground = new SolidColorBrush(Colors.White);

                    if (account.ProfUrl == "" || account.AviUrl == null || account.AviUrl == "" || account.AviUrl == " ")
                    {
                        accountButton.Content = account.Name;
                        accountButton.Background = System.Windows.Media.Brushes.LightGray;
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

                            accountButton.Content = account.Name;
                        }
                    }

                    buttonGrid.Children.Add(accountButton);

                    accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                    ContextMenu accountContext = new ContextMenu();
                    MenuItem deleteItem = new MenuItem();
                    MenuItem editItem = new MenuItem();
                    deleteItem.Header = "Delete";
                    editItem.Header = "Edit";
                    accountContext.Items.Add(editItem);
                    accountContext.Items.Add(deleteItem);
                    accountButton.ContextMenu = accountContext;
                    deleteItem.Click += delegate { DeleteEntry(accountButton); };
                    editItem.Click += delegate { EditEntry(accountButton); };

                    bCounter++;
                    xcounter++;

                    if ((xcounter % Int32.Parse(accPerRow) == 0) && xcounter != 0)
                    {
                        ycounter++;
                        xcounter = 0;
                    }
                }

                int xval = 0;
                int newHeight;

                // Adjust window size and info positions
                if (ycounter == 0)
                {
                    xval = xcounter + 1;
                    newHeight = 190;
                    buttonGrid.Height = 141;
                }
                else
                {
                    xval = Int32.Parse(accPerRow);
                    newHeight = 185 + (125 * ycounter);
                    buttonGrid.Height = 141 * (125 + ycounter);
                }

                int newWidth = (xval * 120) + 25;

                Resize(newHeight, newWidth);
                buttonGrid.Width = newWidth;

                // Adjust new account button
                NewButton.Margin = new Thickness(33 + (xcounter * widthOffset), (ycounter * heightOffset) + 52, 0, 0);
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            NewAccount();
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
                string aviUrl = HtmlAviScrape(dialog.UrlText);

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settingsFile.Write("SelectedAcc", (encryptedAccounts.Count + 1).ToString(), "AutoLog");
                    settingsFile.Write("Selected", "True", "AutoLog");
                    settingsFile.Write("Recent", "False", "AutoLog");
                    selected = true;
                    recent = false;
                    selectedAcc = encryptedAccounts.Count + 1;
                }

                try
                {
                    // Encrypt info before saving to file
                    ePassword = StringCipher.Encrypt(password, eKey);
                    eSharedSecret = StringCipher.Encrypt(sharedSecret, eKey);

                    encryptedAccounts.Add(new Account() { Name = dialog.AccountText, Password = ePassword, SharedSecret = eSharedSecret, ProfUrl = dialog.UrlText, AviUrl = aviUrl, Description = dialog.DescriptionText });

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
                PasswordText = decryptedAccounts[index].Password,
                SharedSecretText = decryptedAccounts[index].SharedSecret,
                UrlText = decryptedAccounts[index].ProfUrl,
                DescriptionText = decryptedAccounts[index].Description
            };

            // Reload slected boolean
            selected = settingsFile.Read("Selected", "AutoLog") == "True" ? true : false;

            if (selected == true && selectedAcc == index)
                dialog.autoLogCheckBox.IsChecked = true;

            if (dialog.ShowDialog() == true)
            {
                string aviUrl = HtmlAviScrape(dialog.UrlText);

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settingsFile.Write("SelectedAcc", button.Tag.ToString(), "AutoLog");
                    settingsFile.Write("Selected", "True", "AutoLog");
                    settingsFile.Write("Recent", "False", "AutoLog");
                    selected = true;
                    recent = false;
                    selectedAcc = index;
                }
                else
                {
                    settingsFile.Write("SelectedAcc", "-1", "AutoLog");
                    settingsFile.Write("Selected", "False", "AutoLog");
                    selected = false;
                    selectedAcc = -1;
                }

                try
                {
                    // Encrypt info before saving to file
                    ePassword = StringCipher.Encrypt(dialog.PasswordText, eKey);
                    eSharedSecret = StringCipher.Encrypt(dialog.SharedSecretText, eKey);

                    encryptedAccounts[index].Name = dialog.AccountText;
                    encryptedAccounts[index].Password = ePassword;
                    encryptedAccounts[index].SharedSecret = eSharedSecret;
                    encryptedAccounts[index].ProfUrl = dialog.UrlText;
                    encryptedAccounts[index].AviUrl = aviUrl;
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
            // Update the most recently used account index.
            recentAcc = index;
            settingsFile.Write("RecentAcc", index.ToString(), "AutoLog");

            // If Steam's filepath was not specified in settings. Attempt to find it and save it.
            if (steamPath == null || steamPath.Length < 3)
            {
                string defaultPath = @"C:\Program Files (x86)\Steam\";
                string secondaryPath = @"D:\Program Files (x86)\Steam\";
                string tertiaryPath = @"E:\Program Files (x86)\Steam\";

                if (Directory.Exists(defaultPath))
                {
                    steamPath = defaultPath;
                }
                else if (Directory.Exists(secondaryPath))
                {
                    steamPath = secondaryPath;
                }
                else if (Directory.Exists(tertiaryPath))
                {
                    steamPath = tertiaryPath;
                }
                else
                {
                    // Prompt user to find steam install
                    var settingsFile = new IniFile("SAMSettings.ini");

                    // Create OpenFileDialog 
                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
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

                // Save path to settings file.
                settingsFile.Write("Steam", steamPath, "Settings");
            }

            // Shutdown Steam process via command if it is already open.
            ProcessStartInfo stopInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = true,
                FileName = steamPath + "Steam.exe",
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
                FileName = steamPath + "Steam.exe",
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
                Type2FA(index, 0);
            }
        }

        private void Type2FA(int index, int failCounter)
        {
            IntPtr handle = FindWindow("vguiPopupWindow", "Steam Guard - Computer Authorization Required");
            while (handle.Equals(IntPtr.Zero))
            {
                handle = FindWindow("vguiPopupWindow", "Steam Guard - Computer Authorization Required");

                // Check for steam warning window.
                IntPtr warningHandle = FindWindow("vguiPopupWindow", "Steam - Warning");
                if (!warningHandle.Equals(IntPtr.Zero))
                {
                    //Cancel the 2FA process since Steam connection is unavailable. 
                    return;
                }
            }

            Console.WriteLine("Found it.");

            Process steamGuardProcess = null;

            // Wait for valid process to wait for input idle.
            Console.WriteLine("Waiting for it to be idle.");
            while (steamGuardProcess == null)
            {
                int procId = 0;

                // Wait for valid process id from handle.
                while (procId == 0)
                {
                    GetWindowThreadProcessId(handle, out procId);
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

            // Wait a second for the window to fully initialize just in case.
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("It is idle now, bringing it up.");

            if (SetForegroundWindow(handle))
            {
                // Generate 2FA code, then send it to the client
                Console.WriteLine("Typing code...");

                System.Windows.Forms.SendKeys.SendWait(Generate2FACode(decryptedAccounts[index].SharedSecret));
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                
                // Need a little pause here to reliably check for popup later
                System.Threading.Thread.Sleep(2000);
            }

            // Check if we still have a 2FA popup, which means, the previous one failed.
            handle = IntPtr.Zero; // just to make sure
            handle = FindWindow("vguiPopupWindow", "Steam Guard - Computer Authorization Required");

            if (failCounter < 2 && !handle.Equals(IntPtr.Zero))
            {
                Console.WriteLine("2FA code failed, retrying...");
                Type2FA(index, failCounter + 1);
            }
            else if (failCounter >= 2 && !handle.Equals(IntPtr.Zero))
            {
                MessageBox.Show("Failed to log in! Please make sure you set your shared secret correctly!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string Generate2FACode(string shared_secret)
        {
            SteamGuardAccount authaccount = new SteamGuardAccount { SharedSecret = shared_secret };

            string code = authaccount.GenerateSteamGuardCode();

            return code;
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Login with clicked button's index, which stored in Tag.
                Login(Int32.Parse(btn.Tag.ToString()));
            }
        }

        private string HtmlAviScrape(string profUrl)
        {
            // If user entered profile url, get avatar jpg url
            if (profUrl.Length > 2)
            {
                // Verify url starts with valid prefix for HtmlWeb
                if (!profUrl.StartsWith("https://") && !profUrl.StartsWith("http://"))
                {
                    profUrl = "https://" + profUrl;
                }

                try
                {
                    HtmlDocument document = new HtmlWeb().Load(profUrl);
                    return document.DocumentNode.Descendants().Where(n => n.HasClass("playerAvatarAutoSizeInner")).First().FirstChild.GetAttributeValue("src", null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            }
            return "";
        }

        private void SortAccounts(int type)
        {
            if (encryptedAccounts.Count > 0)
            {
                // Alphabetical sort based on account name.
                if (type == 0)
                {
                    encryptedAccounts = encryptedAccounts.OrderBy(x => x.Name).ToList();
                    Utils.Serialize(encryptedAccounts);
                    RefreshWindow();
                }
            }
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

        #region File Menu Click Events

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new Window1();
            settingsDialog.ShowDialog();

            accPerRow = settingsDialog.ResponseText;

            LoadSettings();
            RefreshWindow();
        }

        private void GitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/rex706/SAM");
        }

        private async void Ver_Click(object sender, RoutedEventArgs e)
        {
            await UpdateCheck.CheckForUpdate("http://textuploader.com/58mva/raw");
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

        private void ImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Utils.ImportAccountFile();
            RefreshWindow();
        }

        private void ExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Utils.ExportAccountFile();
        }

        private void ReloadImages_Click(object sender, RoutedEventArgs e)
        {
            ReloadImages();
        }

        #endregion
    }
}