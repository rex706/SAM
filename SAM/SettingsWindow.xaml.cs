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

        public string ResponseText
        {
            get
            {
                if (!Regex.IsMatch(accountsPerRowSpinBox.Text, @"^\d+$") || Int32.Parse(accountsPerRowSpinBox.Text) < 1)
                    return "1";
                else
                    return accountsPerRowSpinBox.Text;

            }
            set { accountsPerRowSpinBox.Text = value; }
        }

        public int buttonSize { get; set; }

        public string Password { get; set; }

        public bool Decrypt { get; set; }

        private IniFile settingsFile;
        
        private string start;
        private string minimized;
        private string minimizeToTray;
        private string passwordProtect;
        private string rememberPassword;
        private string recent;
        private string recentAcc;
        private string selected;
        private string selectedAcc;

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
                start = settingsFile.Read("StartWithWindows", "Settings");
                minimized = settingsFile.Read("StartMinimized", "Settings");
                minimizeToTray = settingsFile.Read("MinimizeToTray", "Settings");
                passwordProtect = settingsFile.Read("PasswordProtect", "Settings");
                rememberPassword = settingsFile.Read("RememberPassword", "Settings");
                recent = settingsFile.Read("Recent", "AutoLog");
                recentAcc = settingsFile.Read("RecentAcc", "AutoLog");
                selected = settingsFile.Read("Selected", "AutoLog");
                selectedAcc = settingsFile.Read("SelectedAcc", "AutoLog");

                if (start.ToLower().Equals("true"))
                {
                    startupCheckBox.IsChecked = true;
                }

                if (minimized.ToLower().Equals("true"))
                {
                    startupMinCheckBox.IsChecked = true;
                }

                if (minimizeToTray.ToLower().Equals("true"))
                {
                    minimizeToTrayCheckBox.IsChecked = true;
                }
                
                if (passwordProtect.ToLower().Equals("true"))
                {
                    passwordProtectCheckBox.IsChecked = true;
                }

                if (rememberPassword.ToLower().Equals("true"))
                {
                    rememberLoginPasswordCheckBox.IsChecked = true;
                }

                if (recent.ToLower().Equals("true"))
                {
                    mostRecentCheckBox.IsChecked = true;
                    recentAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(recentAcc)].Name;
                }
                else if (selected.ToLower().Equals("true"))
                {
                    selectedAccountCheckBox.IsChecked = true;
                    selectedAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(selectedAcc)].Name;
                }

                SteamPathTextBox.Text = settingsFile.Read("Steam", "Settings");
            }
        }

        private void SaveSettings(string apr)
        {
            var settingsFile = new IniFile("SAMSettings.ini");

            if (passwordProtectCheckBox.IsChecked == true && passwordProtect.ToLower().Equals("false"))
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
            else if (passwordProtectCheckBox.IsChecked == false && passwordProtect.ToLower().Equals("true"))
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

            if (rememberLoginPasswordCheckBox.IsChecked == true)
            {
                settingsFile.Write("RememberPassword", "true", "Settings");
            }
            else
            {
                settingsFile.Write("RememberPassword", "false", "Settings");
            }
            
            settingsFile.Write("AccountsPerRow", apr, "Settings");
            settingsFile.Write("ButtonSize", buttonSizeSpinBox.Text, "Settings");

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

            if (startupMinCheckBox.IsChecked == true)
                settingsFile.Write("StartMinimized", "true", "Settings");
            else
                settingsFile.Write("StartMinimized", "false", "Settings");

            if (minimizeToTrayCheckBox.IsChecked == true)
                settingsFile.Write("MinimizeToTray", "true", "Settings");
            else
                settingsFile.Write("MinimizeToTray", "false", "Settings");

            if (mostRecentCheckBox.IsChecked == true)
                settingsFile.Write("Recent", "true", "AutoLog");
            else
                settingsFile.Write("Recent", "false", "AutoLog");

            if (selectedAccountCheckBox.IsChecked == true)
                settingsFile.Write("Selected", "true", "AutoLog");
            else
                settingsFile.Write("Selected", "false", "AutoLog");

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
                int idx = Int32.Parse(recentAcc);

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
            catch (Exception m)
            {
                Console.WriteLine(m.Message);
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
                int idx = Int32.Parse(selectedAcc);

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
            catch (Exception m)
            {
                Console.WriteLine(m.Message);
            }
        }

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
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
            catch (Exception m)
            {
                MessageBox.Show(m.Message);
            }
        }
    }
}