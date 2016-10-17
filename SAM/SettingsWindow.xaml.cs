using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SAM
{
    /// <summary>
    /// Interaction logic for Window1.xaml
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
            if (File.Exists("SAMSettings.ini"))
            {
                var settingsFile = new IniFile("SAMSettings.ini");
                textBox.Text = settingsFile.Read("AccountsPerRow", "Settings");
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

        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
