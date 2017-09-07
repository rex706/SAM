using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SAM
{
    /// <summary>
    /// Interaction logic for settings window. 
    /// </summary>
    public partial class Window1 : Window
    {
        public int AutoAccIdx { get; set; }

        public string ResponseText
        {
            get
            {
                if (!Regex.IsMatch(textBox.Text, @"^\d+$") || Int32.Parse(textBox.Text) < 1)
                    return "1";
                else
                    return textBox.Text;

            }
            set { textBox.Text = value; }
        }

        private IniFile settingsFile;
        
        private string start;
        private string minimized;
        private string recent;
        private string recentAcc;
        private string selected;
        private string selectedAcc;

        public Window1()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(SettingsWindow_Loaded);
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists("SAMSettings.ini"))
            {
                settingsFile = new IniFile("SAMSettings.ini");
                textBox.Text = settingsFile.Read("AccountsPerRow", "Settings");
                start = settingsFile.Read("StartWithWindows", "Settings");
                minimized = settingsFile.Read("StartMinimized", "Settings");
                recent = settingsFile.Read("Recent", "AutoLog");
                recentAcc = settingsFile.Read("RecentAcc", "AutoLog");
                selected = settingsFile.Read("Selected", "AutoLog");
                selectedAcc = settingsFile.Read("SelectedAcc", "AutoLog");

                if (start == "True")
                    startupCheckBox.IsChecked = true;

                if (minimized == "True")
                    startupMinCheckBox.IsChecked = true;

                if (recent == "True")
                {
                    mostRecentCheckBox.IsChecked = true;
                    recentAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(recentAcc)].Name;
                }
                else if (selected == "True")
                {
                    selectedAccountCheckBox.IsChecked = true;
                    selectedAccountLabel.Text = MainWindow.encryptedAccounts[Int32.Parse(selectedAcc)].Name;
                }  
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void saveSettings(string apr)
        {
            var settingsFile = new IniFile("SAMSettings.ini");
            settingsFile.Write("AccountsPerRow", apr, "Settings");

            if (startupCheckBox.IsChecked == true)
            {
                settingsFile.Write("StartWithWindows", "True", "Settings");

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

                settingsFile.Write("StartWithWindows", "False", "Settings");
            }

            if (startupMinCheckBox.IsChecked == true)
                settingsFile.Write("StartMinimized", "True", "Settings");
            else
                settingsFile.Write("StartMinimized", "False", "Settings");

            if (mostRecentCheckBox.IsChecked == true)
                settingsFile.Write("Recent", "True", "AutoLog");
            else
                settingsFile.Write("Recent", "False", "AutoLog");

            if (selectedAccountCheckBox.IsChecked == true)
                settingsFile.Write("Selected", "True", "AutoLog");
            else
                settingsFile.Write("Selected", "False", "AutoLog");
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Regex.IsMatch(textBox.Text, @"^\d+$") || Int32.Parse(textBox.Text) < 1)
                saveSettings("1");
            else
                saveSettings(textBox.Text);

            Close();
        }

        private void autologRecentCheckBox_Checked(object sender, RoutedEventArgs e)
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

        private void autologRecentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            recentAccountLabel.Text = "";
        }

        private void selectedAccountLabel_Checked(object sender, RoutedEventArgs e)
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

        private void selectedAccountLabel_Unchecked(object sender, RoutedEventArgs e)
        {
            selectedAccountLabel.Text = "";
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void generateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            //keyTextBox.Text = RandomString(10);
        }

        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".dat";
            dialog.Filter = "SAM DAT Files (*.dat)|*.dat";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    var tempAccounts = Utils.Deserialize(dialog.FileName);
                    MainWindow.encryptedAccounts = MainWindow.encryptedAccounts.Concat(tempAccounts).ToList();
                    Utils.Serialize(MainWindow.encryptedAccounts);
                    MessageBox.Show("Accounts imported!");
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    System.IO.File.Copy("info.dat", dialog.SelectedPath + "info.dat");
                    MessageBox.Show("File exported to:\n" + dialog.SelectedPath);
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
