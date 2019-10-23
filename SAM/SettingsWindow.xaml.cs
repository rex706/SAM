using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
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
            if (System.IO.File.Exists(SAMSettings.FILE_NAME))
            {
                // General
                accountsPerRowSpinBox.Text = settings.File.Read("AccountsPerRow", SAMSettings.SECTION_GENERAL);
                sleepTimeSpinBox.Text = settings.File.Read("SleepTime", SAMSettings.SECTION_GENERAL);
                startupCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("StartWithWindows", SAMSettings.SECTION_GENERAL));
                startupMinCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("StartMinimized", SAMSettings.SECTION_GENERAL));
                minimizeToTrayCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("MinimizeToTray", SAMSettings.SECTION_GENERAL));
                passwordProtectCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("PasswordProtect", SAMSettings.SECTION_GENERAL));
                rememberLoginPasswordCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("RememberPassword", SAMSettings.SECTION_GENERAL));
                clearUserDataCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("ClearUserData", SAMSettings.SECTION_GENERAL));
                HideAddButtonCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("HideAddButton", SAMSettings.SECTION_GENERAL));
                CheckForUpdatesCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("CheckForUpdates", SAMSettings.SECTION_GENERAL));
               
                // AutoLog
                if (Convert.ToBoolean(settings.File.Read("LoginRecentAccount", SAMSettings.SECTION_AUTOLOG)))
                {
                    mostRecentCheckBox.IsChecked = true;
                    recentAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settings.File.Read("RecentAccountIndex", SAMSettings.SECTION_AUTOLOG))].Name;
                }
                else if (Convert.ToBoolean(settings.File.Read("LoginSelectedAccount", SAMSettings.SECTION_AUTOLOG)))
                {
                    selectedAccountCheckBox.IsChecked = true;
                    selectedAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settings.File.Read("SelectedAccountIndex", SAMSettings.SECTION_AUTOLOG))].Name;
                }

                // Customize
                buttonSizeSpinBox.Text = settings.File.Read("ButtonSize", SAMSettings.SECTION_CUSTOMIZE);
                ButtonColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonColor", SAMSettings.SECTION_CUSTOMIZE));
                ButtonFontSizeSpinBox.Text = settings.File.Read("ButtonFontSize", SAMSettings.SECTION_CUSTOMIZE);
                ButtonFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonFontColor", SAMSettings.SECTION_CUSTOMIZE));
                BannerColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonBannerColor", SAMSettings.SECTION_CUSTOMIZE));
                BannerFontSizeSpinBox.Text = settings.File.Read("ButtonBannerFontSize", SAMSettings.SECTION_CUSTOMIZE);
                BannerFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read("ButtonBannerFontColor", SAMSettings.SECTION_CUSTOMIZE));

                // Steam
                SteamPathTextBox.Text = settings.File.Read("Path", SAMSettings.SECTION_STEAM);
                ApiKeyTextBox.Text = settings.File.Read("ApiKey", SAMSettings.SECTION_STEAM);

                // Parameters
                CafeAppLaunchCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("cafeapplaunch", SAMSettings.SECTION_PARAMETERS));
                ClearBetaCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("clearbeta", SAMSettings.SECTION_PARAMETERS));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("console", SAMSettings.SECTION_PARAMETERS));
                LoginCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("login", SAMSettings.SECTION_PARAMETERS));
                DeveloperCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("developer", SAMSettings.SECTION_PARAMETERS));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("forceservice", SAMSettings.SECTION_PARAMETERS));
                NoCacheCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("forceservice", SAMSettings.SECTION_PARAMETERS));
                NoVerifyFilesCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("noverifyfiles", SAMSettings.SECTION_PARAMETERS));
                SilentCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("silent", SAMSettings.SECTION_PARAMETERS));
                SingleCoreCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("single_core", SAMSettings.SECTION_PARAMETERS));
                TcpCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("tcp", SAMSettings.SECTION_PARAMETERS));
                TenFootCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("tenfoot", SAMSettings.SECTION_PARAMETERS));
                CustomParametersCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read("customParameters", SAMSettings.SECTION_PARAMETERS));
                CustomParametersTextBox.Text = settings.File.Read("customParametersValue", SAMSettings.SECTION_PARAMETERS);
            }
        }

        private void SaveSettings(string apr)
        {
            settings.File = new IniFile(SAMSettings.FILE_NAME);

            if (passwordProtectCheckBox.IsChecked == true && !Convert.ToBoolean(settings.File.Read("PasswordProtect", SAMSettings.SECTION_GENERAL)))
            {
                var passwordDialog = new PasswordWindow();

                if (passwordDialog.ShowDialog() == true && passwordDialog.PasswordText != "")
                {
                    Password = passwordDialog.PasswordText;
                    settings.File.Write("PasswordProtect", "true", SAMSettings.SECTION_GENERAL);
                }
                else
                {
                    Password = "";
                }
            }
            else if (passwordProtectCheckBox.IsChecked == false && Convert.ToBoolean(settings.File.Read("PasswordProtect", SAMSettings.SECTION_GENERAL)))
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

                settings.File.Write("PasswordProtect", "false", SAMSettings.SECTION_GENERAL);
                Password = "";
                Decrypt = true;
            }
            else if (passwordProtectCheckBox.IsChecked == false)
            {
                settings.File.Write("PasswordProtect", "false", SAMSettings.SECTION_GENERAL);
            }

            // General
            settings.File.Write("RememberPassword", rememberLoginPasswordCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write("ClearUserData", clearUserDataCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write("AccountsPerRow", apr, SAMSettings.SECTION_GENERAL);
            settings.File.Write("SleepTime", sleepTimeSpinBox.Text, SAMSettings.SECTION_GENERAL);

            if (startupCheckBox.IsChecked == true)
            {
                settings.File.Write("StartWithWindows", "true", SAMSettings.SECTION_GENERAL);

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

                settings.File.Write("StartWithWindows", "false", SAMSettings.SECTION_GENERAL);
            }

            settings.File.Write("StartMinimized", startupMinCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write("MinimizeToTray", minimizeToTrayCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write("HideAddButton", HideAddButtonCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write("CheckForUpdates", CheckForUpdatesCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);

            // Customize
            settings.File.Write("ButtonSize", buttonSizeSpinBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write("ButtonColor", new ColorConverter().ConvertToString(ButtonColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write("ButtonFontSize", ButtonFontSizeSpinBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write("ButtonFontColor", new ColorConverter().ConvertToString(ButtonFontColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write("ButtonBannerColor", new ColorConverter().ConvertToString(BannerColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write("ButtonBannerFontSize", BannerFontSizeSpinBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write("ButtonBannerFontColor", new ColorConverter().ConvertToString(BannerFontColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);

            // AutoLog
            settings.File.Write("LoginRecentAccount", mostRecentCheckBox.IsChecked.ToString(), SAMSettings.SECTION_AUTOLOG);
            settings.File.Write("LoginSelectedAccount", selectedAccountCheckBox.IsChecked.ToString(), SAMSettings.SECTION_AUTOLOG);

            // Steam
            settings.File.Write("Path", SteamPathTextBox.Text, SAMSettings.SECTION_STEAM);
            settings.File.Write("ApiKey", ApiKeyTextBox.Text, SAMSettings.SECTION_STEAM);

            // Parameters
            settings.File.Write("cafeapplaunch", CafeAppLaunchCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("clearbeta", ClearBetaCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("console", ConsoleCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("login", LoginCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("developer", DeveloperCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("forceservice", ForceServiceCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("nocache", NoCacheCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("noverifyfiles", NoVerifyFilesCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("silent", SilentCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("single_core", SingleCoreCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("tcp", TcpCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("tenfoot", TenFootCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("customParameters", CustomParametersCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write("customParametersValue", CustomParametersTextBox.Text, SAMSettings.SECTION_PARAMETERS);
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
                int idx = Int32.Parse(settings.File.Read("LoginRecentAccount", SAMSettings.SECTION_AUTOLOG));

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
                int idx = Int32.Parse(settings.File.Read("LoginSelectedAccount", SAMSettings.SECTION_AUTOLOG));

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
            ApiKeyTextBox.Text = settings.Default.ApiKey;

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
            CustomParametersCheckBox.IsChecked = settings.Default.CustomParameters;
            CustomParametersTextBox.Text = settings.Default.CustomParametersValue;
        }

        private void CustomParametersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CustomParametersTextBox.IsEnabled = true;
        }

        private void CustomParametersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CustomParametersTextBox.IsEnabled = false;
        }

        private void ApiKeyHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://steamcommunity.com/dev/apikey");
        }
    }
}