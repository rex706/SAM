using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

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

        private IniFile settingsFile;

        public SettingsWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(SettingsWindow_Loaded);
            this.Decrypt = false;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists("SAMSettings.ini"))
            {
                settingsFile = new IniFile("SAMSettings.ini");

                accountsPerRowSpinBox.Text = settingsFile.Read("AccountsPerRow", "Settings");
                buttonSizeSpinBox.Text = settingsFile.Read("ButtonSize", "Settings");
                sleepTimeSpinBox.Text = settingsFile.Read("SleepTime", "Settings");

                startupCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("StartWithWindows", "Settings"));
                startupMinCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("StartMinimized", "Settings"));
                minimizeToTrayCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("MinimizeToTray", "Settings"));
                passwordProtectCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("PasswordProtect", "Settings"));
                rememberLoginPasswordCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("RememberPassword", "Settings"));
                clearUserDataCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("ClearUserData", "Settings"));
               
                if (Convert.ToBoolean(settingsFile.Read("Recent", "AutoLog")))
                {
                    mostRecentCheckBox.IsChecked = true;
                    recentAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settingsFile.Read("RecentAcc", "AutoLog"))].Name;
                }
                else if (Convert.ToBoolean(settingsFile.Read("Selected", "AutoLog")))
                {
                    selectedAccountCheckBox.IsChecked = true;
                    selectedAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(settingsFile.Read("SelectedAcc", "AutoLog"))].Name;
                }

                CafeAppLaunchCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("cafeapplaunch", "Parameters"));
                ClearBetaCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("clearbeta", "Parameters"));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("console", "Parameters"));
                LoginCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("login", "Parameters"));
                DeveloperCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("developer", "Parameters"));
                ConsoleCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("forceservice", "Parameters"));
                NoCacheCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("forceservice", "Parameters"));
                NoVerifyFilesCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("noverifyfiles", "Parameters"));
                SilentCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("silent", "Parameters"));
                SingleCoreCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("single_core", "Parameters"));
                TcpCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("tcp", "Parameters"));
                TenFootCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("tenfoot", "Parameters"));

                SteamPathTextBox.Text = settingsFile.Read("Steam", "Settings");
            }
        }

        private void SaveSettings(string apr)
        {
            settingsFile = new IniFile("SAMSettings.ini");

            if (passwordProtectCheckBox.IsChecked == true && !Convert.ToBoolean(settingsFile.Read("PasswordProtect", "Settings")))
            {
                var passwordDialog = new PasswordWindow();

                if (passwordDialog.ShowDialog() == true && passwordDialog.PasswordText != "")
                {
                    Password = passwordDialog.PasswordText;
                    settingsFile.Write("PasswordProtect", "true", "Settings");
                }
                else
                {
                    Password = "";
                }
            }
            else if (passwordProtectCheckBox.IsChecked == false && Convert.ToBoolean(settingsFile.Read("PasswordProtect", "Settings")))
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

                settingsFile.Write("PasswordProtect", "false", "Settings");
                Password = "";
                Decrypt = true;
            }
            else if (passwordProtectCheckBox.IsChecked == false)
            {
                settingsFile.Write("PasswordProtect", "false", "Settings");
            }

            settingsFile.Write("RememberPassword", rememberLoginPasswordCheckBox.IsChecked.ToString(), "Settings");
            settingsFile.Write("ClearUserData", clearUserDataCheckBox.IsChecked.ToString(), "Settings");
            settingsFile.Write("AccountsPerRow", apr, "Settings");
            settingsFile.Write("ButtonSize", buttonSizeSpinBox.Text, "Settings");
            settingsFile.Write("SleepTime", sleepTimeSpinBox.Text, "Settings");

            if (startupCheckBox.IsChecked == true)
            {
                settingsFile.Write("StartWithWindows", "true", "Settings");

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

                settingsFile.Write("StartWithWindows", "false", "Settings");
            }

            settingsFile.Write("StartMinimized", startupMinCheckBox.IsChecked.ToString(), "Settings");
            settingsFile.Write("MinimizeToTray", minimizeToTrayCheckBox.IsChecked.ToString(), "Settings");

            settingsFile.Write("Recent", mostRecentCheckBox.IsChecked.ToString(), "AutoLog");
            settingsFile.Write("Selected", selectedAccountCheckBox.IsChecked.ToString(), "AutoLog");

            settingsFile.Write("cafeapplaunch", CafeAppLaunchCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("clearbeta", ClearBetaCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("console", ConsoleCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("login", LoginCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("developer", DeveloperCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("forceservice", ForceServiceCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("nocache", NoCacheCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("noverifyfiles", NoVerifyFilesCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("silent", SilentCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("single_core", SingleCoreCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("tcp", TcpCheckBox.IsChecked.ToString(), "Parameters");
            settingsFile.Write("tenfoot", TenFootCheckBox.IsChecked.ToString(), "Parameters");

            settingsFile.Write("Steam", SteamPathTextBox.Text, "Settings");
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
                int idx = Int32.Parse(settingsFile.Read("RecentAcc", "AutoLog"));

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
                int idx = Int32.Parse(settingsFile.Read("SelectedAcc", "AutoLog"));

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
            accountsPerRowSpinBox.Text = "5";
            buttonSizeSpinBox.Text = "100";
            sleepTimeSpinBox.Text = "2";

            startupCheckBox.IsChecked = false;
            startupMinCheckBox.IsChecked = false;
            minimizeToTrayCheckBox.IsChecked = false;
            rememberLoginPasswordCheckBox.IsChecked = false;
            clearUserDataCheckBox.IsChecked = false;

            // Ignore password protect checkbox.
            //passwordProtectCheckBox.IsChecked = false;

            mostRecentCheckBox.IsChecked = false;
            selectedAccountCheckBox.IsChecked = false;

            SteamPathTextBox.Text = Utils.CheckSteamPath();

            CafeAppLaunchCheckBox.IsChecked = false;
            ClearBetaCheckBox.IsChecked = false;
            ConsoleCheckBox.IsChecked = false;
            DeveloperCheckBox.IsChecked = false;
            ForceServiceCheckBox.IsChecked = false;
            LoginCheckBox.IsChecked = true;
            NoCacheCheckBox.IsChecked = false;
            NoVerifyFilesCheckBox.IsChecked = false;
            SilentCheckBox.IsChecked = false;
            SingleCoreCheckBox.IsChecked = false;
            TcpCheckBox.IsChecked = false;
            TenFootCheckBox.IsChecked = false;
        }
    }
}