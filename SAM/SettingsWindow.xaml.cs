using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace SAM
{
    /// <summary>
    /// Interaction logic for settings window. 
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public int AutoAccIdx { get; set; }

        public int AccountsPerRow
        {
            get
            {
                if (!Regex.IsMatch(accountsPerRowSpinBox.Text, @"^\d+$") || Int32.Parse(accountsPerRowSpinBox.Text) < 1)
                    return 1;
                else
                    return Int32.Parse(accountsPerRowSpinBox.Text);
            }
            set
            {
                accountsPerRowSpinBox.Text = value.ToString();
            }
        }

        public int buttonSize { get; set; }

        public string Password { get; set; }

        public bool Decrypt { get; set; }

        private SAMSettings settings;

        public SettingsWindow()
        {
            InitializeComponent();

            settings = new SAMSettings();

            this.Loaded += new RoutedEventHandler(SettingsWindow_Loaded);
            this.Decrypt = false;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists("SAMSettings.ini"))
            {
                // General
                accountsPerRowSpinBox.Text = settings.File.Read("AccountsPerRow", "Settings");
                sleepTimeSpinBox.Text = settings.File.Read("SleepTime", "Settings");
                startupCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("StartWithWindows", "Settings"));
                startupMinCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("StartMinimized", "Settings"));
                minimizeToTrayCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("MinimizeToTray", "Settings"));
                passwordProtectCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("PasswordProtect", "Settings"));
                rememberLoginPasswordCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("RememberPassword", "Settings"));
                clearUserDataCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("ClearUserData", "Settings"));
                HideAddButtonCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("HideAddButton", "Settings"));
                CheckForUpdatesCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("CheckForUpdates", "Settings"));
               
                // AutoLog
                if (Convert.ToBoolean(settings.File.Read("LoginRecentAccount", "AutoLog")))
                {
                    mostRecentCheckBox.IsChecked = true;
                    recentAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settings.File.Read("RecentAccountIndex", "AutoLog"))].Name;
                }
                else if (Convert.ToBoolean(settings.File.Read("LoginSelectedAccount", "AutoLog")))
                {
                    selectedAccountCheckBox.IsChecked = true;
                    selectedAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settings.File.Read("SelectedAccountIndex", "AutoLog"))].Name;
                }

                // Customize
                buttonSizeSpinBox.Text = settings.File.Read("ButtonSize", "Customize");
                ButtonColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonColor", "Customize"));
                ButtonFontSizeSpinBox.Text = settings.File.Read("ButtonFontSize", "Customize");
                ButtonFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonFontColor", "Customize"));
                BannerColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonBannerColor", "Customize"));
                BannerFontSizeSpinBox.Text = settings.File.Read("ButtonBannerFontSize", "Customize");
                BannerFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonBannerFontColor", "Customize"));

                // Steam
                SteamPathTextBox.Text = settings.File.Read("Path", "Steam");

                // Parameters
                CafeAppLaunchCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("cafeapplaunch", "Parameters"));
                ClearBetaCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("clearbeta", "Parameters"));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("console", "Parameters"));
                LoginCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("login", "Parameters"));
                DeveloperCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("developer", "Parameters"));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("forceservice", "Parameters"));
                NoCacheCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("forceservice", "Parameters"));
                NoVerifyFilesCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("noverifyfiles", "Parameters"));
                SilentCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("silent", "Parameters"));
                SingleCoreCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("single_core", "Parameters"));
                TcpCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("tcp", "Parameters"));
                TenFootCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("tenfoot", "Parameters"));
            }
        }

        private void SaveSettings(string apr)
        {
            settings.File = new IniFile("SAMSettings.ini");

            if (passwordProtectCheckBox.IsChecked == true && !Convert.ToBoolean(settings.File.Read("PasswordProtect", "Settings")))
            {
                var passwordDialog = new PasswordWindow();

                if (passwordDialog.ShowDialog() == true && passwordDialog.PasswordText != "")
                {
                    Password = passwordDialog.PasswordText;
                    settings.File.Write("PasswordProtect", "true", "Settings");
                }
                else
                {
                    Password = "";
                }
            }
            else if (passwordProtectCheckBox.IsChecked == false && Convert.ToBoolean(settings.File.Read("PasswordProtect", "Settings")))
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure you want to decrypt your data file?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    var passwordDialog = new PasswordWindow();

                    if (passwordDialog.ShowDialog() == true)
                    {
                        messageBoxResult = MessageBoxResult.OK;

                        while (messageBoxResult == MessageBoxResult.OK)
                        {
                            try
                            {
                                Utils.PasswordDeserialize("info.dat", passwordDialog.PasswordText);
                                messageBoxResult = MessageBoxResult.None;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                messageBoxResult = MessageBox.Show("Invalid Password", "Invalid", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                                if (messageBoxResult == MessageBoxResult.Cancel)
                                {
                                    passwordProtectCheckBox.IsChecked = true;
                                    return;
                                }

                                passwordDialog = new PasswordWindow();
                                passwordDialog.ShowDialog();
                            }
                        }
                    }
                }
                else
                {
                    passwordProtectCheckBox.IsChecked = true;
                    return;
                }

                settings.File.Write("PasswordProtect", "false", "Settings");
                Password = "";
                Decrypt = true;
            }
            else if (passwordProtectCheckBox.IsChecked == false)
            {
                settings.File.Write("PasswordProtect", "false", "Settings");
            }

            // General
            settings.File.Write("RememberPassword", rememberLoginPasswordCheckBox.IsChecked.ToString(), "Settings");
            settings.File.Write("ClearUserData", clearUserDataCheckBox.IsChecked.ToString(), "Settings");
            settings.File.Write("AccountsPerRow", apr, "Settings");
            settings.File.Write("SleepTime", sleepTimeSpinBox.Text, "Settings");

            if (startupCheckBox.IsChecked == true)
            {
                settings.File.Write("StartWithWindows", "true", "Settings");

                WshShell shell = new WshShell();
                string shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\SAM.lnk";
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Start with windows shortcut for SAM.";
                shortcut.TargetPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\SAM.exe";
                shortcut.WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                shortcut.Save();
            }   
            else
            {
                string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\SAM.lnk";

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                settings.File.Write("StartWithWindows", "false", "Settings");
            }

            settings.File.Write("StartMinimized", startupMinCheckBox.IsChecked.ToString(), "Settings");
            settings.File.Write("MinimizeToTray", minimizeToTrayCheckBox.IsChecked.ToString(), "Settings");
            settings.File.Write("HideAddButton", HideAddButtonCheckBox.IsChecked.ToString(), "Settings");
            settings.File.Write("CheckForUpdates", CheckForUpdatesCheckBox.IsChecked.ToString(), "Settings");

            // Customize
            settings.File.Write("ButtonSize", buttonSizeSpinBox.Text, "Customize");
            settings.File.Write("ButtonColor", new ColorConverter().ConvertToString(ButtonColorPicker.SelectedColor), "Customize");
            settings.File.Write("ButtonFontSize", ButtonFontSizeSpinBox.Text, "Customize");
            settings.File.Write("ButtonFontColor", new ColorConverter().ConvertToString(ButtonFontColorPicker.SelectedColor), "Customize");
            settings.File.Write("ButtonBannerColor", new ColorConverter().ConvertToString(BannerColorPicker.SelectedColor), "Customize");
            settings.File.Write("ButtonBannerFontSize", BannerFontSizeSpinBox.Text, "Customize");
            settings.File.Write("ButtonBannerFontColor", new ColorConverter().ConvertToString(BannerFontColorPicker.SelectedColor), "Customize");

            // AutoLog
            settings.File.Write("LoginRecentAccount", mostRecentCheckBox.IsChecked.ToString(), "AutoLog");
            settings.File.Write("LoginSelectedAccount", selectedAccountCheckBox.IsChecked.ToString(), "AutoLog");

            // Steam
            settings.File.Write("Path", SteamPathTextBox.Text, "Steam");

            // Parameters
            settings.File.Write("cafeapplaunch", CafeAppLaunchCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("clearbeta", ClearBetaCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("console", ConsoleCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("login", LoginCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("developer", DeveloperCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("forceservice", ForceServiceCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("nocache", NoCacheCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("noverifyfiles", NoVerifyFilesCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("silent", SilentCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("single_core", SingleCoreCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("tcp", TcpCheckBox.IsChecked.ToString(), "Parameters");
            settings.File.Write("tenfoot", TenFootCheckBox.IsChecked.ToString(), "Parameters");
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Regex.IsMatch(accountsPerRowSpinBox.Text, @"^\d+$") || Int32.Parse(accountsPerRowSpinBox.Text) < 1)
                SaveSettings("1");
            else
                SaveSettings(accountsPerRowSpinBox.Text);

            Close();
        }

        private void AutologRecentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                int idx = Int32.Parse(settings.File.Read("LoginRecentAccount", "AutoLog"));

                // If index is invalid, uncheck box.
                if (idx < 0)
                {
                    mostRecentCheckBox.IsChecked = false;
                }
                else
                {
                    AutoAccIdx = idx;
                    recentAccountLabel.Text = MainWindow.encryptedAccounts[idx].Name;
                    selectedAccountCheckBox.IsChecked = false;
                    selectedAccountLabel.Text = "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                mostRecentCheckBox.IsChecked = false;
            }
        }

        private void AutologRecentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            recentAccountLabel.Text = "";
        }

        private void SelectedAccountLabel_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                int idx = Int32.Parse(settings.File.Read("LoginSelectedAccount", "AutoLog"));

                if (idx < 0)
                {
                    selectedAccountCheckBox.IsChecked = false;
                }
                else
                {
                    mostRecentCheckBox.IsChecked = false;
                    recentAccountLabel.Text = "";
                    AutoAccIdx = idx;
                    selectedAccountLabel.Text = MainWindow.encryptedAccounts[idx].Name;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                selectedAccountCheckBox.IsChecked = false;
            }
        }

        private void SelectedAccountLabel_Unchecked(object sender, RoutedEventArgs e)
        {
            selectedAccountLabel.Text = "";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            //keyTextBox.Text = RandomString(10);
        }

        private void ChangePathButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt user to find steam install
            string path = "";

            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".exe",
                Filter = "Steam (*.exe)|*.exe",
                InitialDirectory = Environment.SpecialFolder.MyComputer.ToString()
            };

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file path
            if (result == true)
            {
                // Save path to settings file.
                path = Path.GetDirectoryName(dlg.FileName) + "\\";
                SteamPathTextBox.Text = path;
            }
        }

        private void AutoPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SteamPathTextBox.Text = Utils.CheckSteamPath();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            clearUserDataCheckBox.IsChecked = settings.Default.ClearUserData;
            rememberLoginPasswordCheckBox.IsChecked = settings.Default.RememberPassword;
            startupCheckBox.IsChecked = settings.Default.StartWithWindows;
            startupMinCheckBox.IsChecked = settings.Default.StartMinimized;
            minimizeToTrayCheckBox.IsChecked = settings.Default.MinimizeToTray;
            accountsPerRowSpinBox.Text = settings.Default.AccountsPerRow.ToString();
            sleepTimeSpinBox.Text = settings.Default.SleepTime.ToString();

            // Ignore password protect checkbox.
            //passwordProtectCheckBox.IsChecked = settings.Default.PasswordProtect;

            mostRecentCheckBox.IsChecked = settings.Default.LoginRecentAccount;
            selectedAccountCheckBox.IsChecked = settings.Default.LoginSelectedAccount;

            SteamPathTextBox.Text = Utils.CheckSteamPath();

            buttonSizeSpinBox.Text = settings.Default.ButtonSize.ToString();
            ButtonColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.ButtonColor);
            ButtonFontSizeSpinBox.Text = settings.Default.ButtonFontSize.ToString();
            ButtonFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.ButtonFontColor);
            BannerColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.ButtonBannerColor);
            BannerFontSizeSpinBox.Text = settings.Default.BannerFontSize.ToString();
            BannerFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.BannerFontColor);

            CafeAppLaunchCheckBox.IsChecked = settings.Default.CafeAppLaunch;
            ClearBetaCheckBox.IsChecked = settings.Default.ClearBeta;
            ConsoleCheckBox.IsChecked = settings.Default.Console;
            DeveloperCheckBox.IsChecked = settings.Default.Developer;
            ForceServiceCheckBox.IsChecked = settings.Default.ForceService;
            LoginCheckBox.IsChecked = settings.Default.Login;
            NoCacheCheckBox.IsChecked = settings.Default.NoCache;
            NoVerifyFilesCheckBox.IsChecked = settings.Default.NoVerifyFiles;
            SilentCheckBox.IsChecked = settings.Default.Silent;
            SingleCoreCheckBox.IsChecked = settings.Default.SingleCore;
            TcpCheckBox.IsChecked = settings.Default.TCP;
            TenFootCheckBox.IsChecked = settings.Default.TenFoot;
        }
    }
}