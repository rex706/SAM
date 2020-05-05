using IWshRuntimeLibrary;
using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace SAM
{
    /// <summary>
    /// Interaction logic for settings window. 
    /// </summary>
    public partial class SettingsWindow : MetroWindow
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

        private string SAMshortcut = @"\SAM.lnk";
        private string SAMexe = @"\SAM.exe";
        public SettingsWindow()
        {
            settings = new SAMSettings();

            InitializeComponent();

            this.Loaded += new RoutedEventHandler(SettingsWindow_Loaded);
            this.Decrypt = false;

            InputMethodSelectBox.ItemsSource = Enum.GetValues(typeof(VirtualInputMethod)).Cast<VirtualInputMethod>();
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(SAMSettings.FILE_NAME))
            {
                // General
                accountsPerRowSpinBox.Text = settings.File.Read(SAMSettings.ACCOUNTS_PER_ROW, SAMSettings.SECTION_GENERAL);
                sleepTimeSpinBox.Text = settings.File.Read(SAMSettings.SLEEP_TIME, SAMSettings.SECTION_GENERAL);
                startupCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.START_WITH_WINDOWS, SAMSettings.SECTION_GENERAL));
                startupMinCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.START_MINIMIZED, SAMSettings.SECTION_GENERAL));
                minimizeToTrayCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.MINIMIZE_TO_TRAY, SAMSettings.SECTION_GENERAL));
                passwordProtectCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.PASSWORD_PROTECT, SAMSettings.SECTION_GENERAL));
                rememberLoginPasswordCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.REMEMBER_PASSWORD, SAMSettings.SECTION_GENERAL));
                clearUserDataCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.CLEAR_USER_DATA, SAMSettings.SECTION_GENERAL));
                HideAddButtonCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.HIDE_ADD_BUTTON, SAMSettings.SECTION_GENERAL));
                CheckForUpdatesCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.CHECK_FOR_UPDATES, SAMSettings.SECTION_GENERAL));
                CloseOnLoginCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.CLOSE_ON_LOGIN, SAMSettings.SECTION_GENERAL));
                ListViewCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.LIST_VIEW, SAMSettings.SECTION_GENERAL));
                SandboxModeCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.SANDBOX_MODE, SAMSettings.SECTION_GENERAL));

                // AutoLog
                if (Convert.ToBoolean(settings.File.Read(SAMSettings.LOGIN_RECENT_ACCOUNT, SAMSettings.SECTION_AUTOLOG)) == true)
                {
                    mostRecentCheckBox.IsChecked = true;
                    recentAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settings.File.Read(SAMSettings.RECENT_ACCOUNT_INDEX, SAMSettings.SECTION_AUTOLOG))].Name;
                }
                else if (Convert.ToBoolean(settings.File.Read(SAMSettings.LOGIN_SELECTED_ACCOUNT, SAMSettings.SECTION_AUTOLOG)) == true)
                {
                    selectedAccountCheckBox.IsChecked = true;
                    selectedAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settings.File.Read(SAMSettings.SELECTED_ACCOUNT_INDEX, SAMSettings.SECTION_AUTOLOG))].Name;
                }
                InputMethodSelectBox.SelectedItem = (VirtualInputMethod)Enum.Parse(typeof(VirtualInputMethod), settings.File.Read(SAMSettings.INPUT_METHOD, SAMSettings.SECTION_AUTOLOG));
                HandleImeCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.HANDLE_IME, SAMSettings.SECTION_AUTOLOG));
                SteamGuardOnlyCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.IME_2FA_ONLY, SAMSettings.SECTION_AUTOLOG));

                // Customize
                ThemeSelectBox.Text = settings.File.Read(SAMSettings.THEME, SAMSettings.SECTION_CUSTOMIZE);
                AccentSelectBox.Text = settings.File.Read(SAMSettings.ACCENT, SAMSettings.SECTION_CUSTOMIZE);
                buttonSizeSpinBox.Text = settings.File.Read(SAMSettings.BUTTON_SIZE, SAMSettings.SECTION_CUSTOMIZE);
                ButtonColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read(SAMSettings.BUTTON_COLOR, SAMSettings.SECTION_CUSTOMIZE));
                ButtonFontSizeSpinBox.Text = settings.File.Read(SAMSettings.BUTTON_FONT_SIZE, SAMSettings.SECTION_CUSTOMIZE);
                ButtonFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read(SAMSettings.BUTTON_FONT_COLOR, SAMSettings.SECTION_CUSTOMIZE));
                BannerColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read(SAMSettings.BUTTON_BANNER_COLOR, SAMSettings.SECTION_CUSTOMIZE));
                BannerFontSizeSpinBox.Text = settings.File.Read(SAMSettings.BUTTON_BANNER_FONT_SIZE, SAMSettings.SECTION_CUSTOMIZE);
                BannerFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.File.Read(SAMSettings.BUTTON_BANNER_FONT_COLOR, SAMSettings.SECTION_CUSTOMIZE));
                HideBanIconsCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.HIDE_BAN_ICONS, SAMSettings.SECTION_CUSTOMIZE));

                // Steam
                SteamPathTextBox.Text = settings.File.Read(SAMSettings.STEAM_PATH, SAMSettings.SECTION_STEAM);
                ApiKeyTextBox.Text = settings.File.Read(SAMSettings.STEAM_API_KEY, SAMSettings.SECTION_STEAM);
                AutoReloadCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.AUTO_RELOAD_ENABLED, SAMSettings.SECTION_STEAM));
                AutoReloadIntervalSpinBox.Text = settings.File.Read(SAMSettings.AUTO_RELOAD_INTERVAL, SAMSettings.SECTION_STEAM);

                // Parameters
                CafeAppLaunchCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.CAFE_APP_LAUNCH_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                ClearBetaCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.CLEAR_BETA_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.CONSOLE_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                LoginCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.LOGIN_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                DeveloperCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.DEVELOPER_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                ForceServiceCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.FORCE_SERVICE_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.FORCE_SERVICE_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                NoCacheCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.FORCE_SERVICE_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                NoVerifyFilesCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.NO_VERIFY_FILES_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                SilentCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.SILENT_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                SingleCoreCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.SINGLE_CORE_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                TcpCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.TCP_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                TenFootCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.TEN_FOOT_PARAMETER, SAMSettings.SECTION_PARAMETERS));
                CustomParametersCheckBox.IsChecked = Convert.ToBoolean(settings.File.Read(SAMSettings.CUSTOM_PARAMETERS, SAMSettings.SECTION_PARAMETERS));
                CustomParametersTextBox.Text = settings.File.Read(SAMSettings.CUSTOM_PARAMETERS_VALUE, SAMSettings.SECTION_PARAMETERS);
            }
        }

        private void SaveSettings(string apr)
        {
            settings.File = new IniFile(SAMSettings.FILE_NAME);

            if (passwordProtectCheckBox.IsChecked == true && !Convert.ToBoolean(settings.File.Read(SAMSettings.PASSWORD_PROTECT, SAMSettings.SECTION_GENERAL)))
            {
                var passwordDialog = new PasswordWindow();

                if (passwordDialog.ShowDialog() == true && passwordDialog.PasswordText != "")
                {
                    Password = passwordDialog.PasswordText;
                    settings.File.Write(SAMSettings.PASSWORD_PROTECT, true.ToString(), SAMSettings.SECTION_GENERAL);
                }
                else
                {
                    Password = "";
                }
            }
            else if (passwordProtectCheckBox.IsChecked == false && Convert.ToBoolean(settings.File.Read(SAMSettings.PASSWORD_PROTECT, SAMSettings.SECTION_GENERAL)))
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

                settings.File.Write(SAMSettings.PASSWORD_PROTECT, false.ToString(), SAMSettings.SECTION_GENERAL);
                Password = "";
                Decrypt = true;
            }
            else if (passwordProtectCheckBox.IsChecked == false)
            {
                settings.File.Write(SAMSettings.PASSWORD_PROTECT, false.ToString(), SAMSettings.SECTION_GENERAL);
            }

            // General
            settings.File.Write(SAMSettings.REMEMBER_PASSWORD, rememberLoginPasswordCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.CLEAR_USER_DATA, clearUserDataCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.ACCOUNTS_PER_ROW, apr, SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.SLEEP_TIME, sleepTimeSpinBox.Text, SAMSettings.SECTION_GENERAL);

            if (startupCheckBox.IsChecked == true)
            {
                settings.File.Write(SAMSettings.START_WITH_WINDOWS, true.ToString(), SAMSettings.SECTION_GENERAL);

                WshShell shell = new WshShell();
                string shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + SAMshortcut;
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Start with windows shortcut for SAM.";
                shortcut.TargetPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + SAMexe;
                shortcut.WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                shortcut.Save();
            }   
            else
            {
                string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + SAMshortcut;

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                settings.File.Write(SAMSettings.START_WITH_WINDOWS, false.ToString(), SAMSettings.SECTION_GENERAL);
            }

            settings.File.Write(SAMSettings.START_MINIMIZED, startupMinCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.MINIMIZE_TO_TRAY, minimizeToTrayCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.HIDE_ADD_BUTTON, HideAddButtonCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.CHECK_FOR_UPDATES, CheckForUpdatesCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.CLOSE_ON_LOGIN, CloseOnLoginCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.LIST_VIEW, ListViewCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);
            settings.File.Write(SAMSettings.SANDBOX_MODE, SandboxModeCheckBox.IsChecked.ToString(), SAMSettings.SECTION_GENERAL);

            // Customize
            settings.File.Write(SAMSettings.THEME, ThemeSelectBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.ACCENT, AccentSelectBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.BUTTON_SIZE, buttonSizeSpinBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.BUTTON_COLOR, new ColorConverter().ConvertToString(ButtonColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.BUTTON_FONT_SIZE, ButtonFontSizeSpinBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.BUTTON_FONT_COLOR, new ColorConverter().ConvertToString(ButtonFontColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.BUTTON_BANNER_COLOR, new ColorConverter().ConvertToString(BannerColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.BUTTON_BANNER_FONT_SIZE, BannerFontSizeSpinBox.Text, SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.BUTTON_BANNER_FONT_COLOR, new ColorConverter().ConvertToString(BannerFontColorPicker.SelectedColor), SAMSettings.SECTION_CUSTOMIZE);
            settings.File.Write(SAMSettings.HIDE_BAN_ICONS, HideBanIconsCheckBox.IsChecked.ToString(), SAMSettings.SECTION_CUSTOMIZE);

            // AutoLog
            settings.File.Write(SAMSettings.LOGIN_RECENT_ACCOUNT, mostRecentCheckBox.IsChecked.ToString(), SAMSettings.SECTION_AUTOLOG);
            settings.File.Write(SAMSettings.LOGIN_SELECTED_ACCOUNT, selectedAccountCheckBox.IsChecked.ToString(), SAMSettings.SECTION_AUTOLOG);
            settings.File.Write(SAMSettings.INPUT_METHOD, InputMethodSelectBox.SelectedItem.ToString(), SAMSettings.SECTION_AUTOLOG);
            settings.File.Write(SAMSettings.HANDLE_IME, HandleImeCheckBox.IsChecked.ToString(), SAMSettings.SECTION_AUTOLOG);
            settings.File.Write(SAMSettings.IME_2FA_ONLY, SteamGuardOnlyCheckBox.IsChecked.ToString(), SAMSettings.SECTION_AUTOLOG);

            // Steam
            settings.File.Write(SAMSettings.STEAM_PATH, SteamPathTextBox.Text, SAMSettings.SECTION_STEAM);
            settings.File.Write(SAMSettings.STEAM_API_KEY, Regex.Replace(ApiKeyTextBox.Text, @"\s+", string.Empty), SAMSettings.SECTION_STEAM);
            settings.File.Write(SAMSettings.AUTO_RELOAD_ENABLED, AutoReloadCheckBox.IsChecked.ToString(), SAMSettings.SECTION_STEAM);
            settings.File.Write(SAMSettings.AUTO_RELOAD_INTERVAL, AutoReloadIntervalSpinBox.Text, SAMSettings.SECTION_STEAM);

            // Parameters
            settings.File.Write(SAMSettings.CAFE_APP_LAUNCH_PARAMETER, CafeAppLaunchCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.CLEAR_BETA_PARAMETER, ClearBetaCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.CONSOLE_PARAMETER, ConsoleCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.LOGIN_PARAMETER, LoginCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.DEVELOPER_PARAMETER, DeveloperCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.FORCE_SERVICE_PARAMETER, ForceServiceCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.NO_CACHE_PARAMETER, NoCacheCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.NO_VERIFY_FILES_PARAMETER, NoVerifyFilesCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.SILENT_PARAMETER, SilentCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.SINGLE_CORE_PARAMETER, SingleCoreCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.TCP_PARAMETER, TcpCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.TEN_FOOT_PARAMETER, TenFootCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.CUSTOM_PARAMETERS, CustomParametersCheckBox.IsChecked.ToString(), SAMSettings.SECTION_PARAMETERS);
            settings.File.Write(SAMSettings.CUSTOM_PARAMETERS_VALUE, CustomParametersTextBox.Text, SAMSettings.SECTION_PARAMETERS);
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
                int idx = Int32.Parse(settings.File.Read(SAMSettings.RECENT_ACCOUNT_INDEX, SAMSettings.SECTION_AUTOLOG));

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
                bool selectedEnabled = Convert.ToBoolean(settings.File.Read(SAMSettings.LOGIN_SELECTED_ACCOUNT, SAMSettings.SECTION_AUTOLOG));
                int idx = Int32.Parse(settings.File.Read(SAMSettings.SELECTED_ACCOUNT_INDEX, SAMSettings.SECTION_AUTOLOG));

                if (selectedEnabled == false || idx < 0)
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
            CheckForUpdatesCheckBox.IsChecked = settings.Default.CheckForUpdates;
            CloseOnLoginCheckBox.IsChecked = settings.Default.CloseOnLogin;
            ListViewCheckBox.IsChecked = settings.Default.ListView;
            SandboxModeCheckBox.IsChecked = settings.Default.SandboxMode;

            // Ignore password protect checkbox.
            //passwordProtectCheckBox.IsChecked = settings.Default.PasswordProtect;

            mostRecentCheckBox.IsChecked = settings.Default.LoginRecentAccount;
            selectedAccountCheckBox.IsChecked = settings.Default.LoginSelectedAccount;
            InputMethodSelectBox.SelectedItem = settings.Default.VirtualInputMethod;
            HandleImeCheckBox.IsChecked = settings.Default.HandleMicrosoftIME;
            SteamGuardOnlyCheckBox.IsChecked = settings.Default.IME2FAOnly;
            
            SteamPathTextBox.Text = Utils.CheckSteamPath();
            ApiKeyTextBox.Text = settings.Default.ApiKey;
            AutoReloadCheckBox.IsChecked = settings.Default.AutoReloadEnabled;
            AutoReloadIntervalSpinBox.Text = settings.Default.AutoReloadInterval.ToString();

            ThemeSelectBox.Text = settings.Default.Theme;
            AccentSelectBox.Text = settings.Default.Accent;
            buttonSizeSpinBox.Text = settings.Default.ButtonSize.ToString();
            ButtonColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.ButtonColor);
            ButtonFontSizeSpinBox.Text = settings.Default.ButtonFontSize.ToString();
            ButtonFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.ButtonFontColor);
            BannerColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.ButtonBannerColor);
            BannerFontSizeSpinBox.Text = settings.Default.BannerFontSize.ToString();
            BannerFontColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(settings.Default.BannerFontColor);
            HideBanIconsCheckBox.IsChecked = settings.Default.HideBanIcons;

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

        private void ApiKeyTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (ApiKeyTextBox.Text.Length == 32)
            {
                AutoReloadCheckBox.IsEnabled = true;
            }
            else
            {
                AutoReloadCheckBox.IsEnabled = false;
                AutoReloadCheckBox.IsChecked = false;
            }
        }

        private void AutoReloadCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AutoReloadIntervalSpinBox.IsEnabled = true;
        }

        private void AutoReloadCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AutoReloadIntervalSpinBox.IsEnabled = false;
        }

        private void CustomParamsHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://developer.valvesoftware.com/wiki/Command_Line_Options#Steam_.28Windows.29");
        }

        private void HandleImeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SteamGuardOnlyCheckBox.IsEnabled = true;
        }

        private void HandleImeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SteamGuardOnlyCheckBox.IsEnabled = false;
            SteamGuardOnlyCheckBox.IsChecked = false;
        }
    }
}