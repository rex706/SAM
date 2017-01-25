
using IWshRuntimeLibrary;
using System;
using System.IO;
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
        public Window1()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(SettingsWindow_Loaded);
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists("SAMSettings.ini"))
            {
                var settingsFile = new IniFile("SAMSettings.ini");
                textBox.Text = settingsFile.Read("AccountsPerRow", "Settings");
                string start = settingsFile.Read("StartWithWindows", "Settings");

                if (start == "True")
                    startupCheckBox.IsChecked = true;
            }
        }

        public string ResponseText
        {
            get
            {
                if (!Regex.IsMatch(textBox.Text, @"^\d+$") || Int32.Parse(textBox.Text) < 1)
                {
                    saveSettings("1");
                    return "1";
                }
                else
                {
                    saveSettings(textBox.Text);
                    return textBox.Text;
                }
                    
            }
            set { textBox.Text = value; }
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
                shortcut.Description = "New shortcut for SAM";
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
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void startupCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
