using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            get { return textBox.Text; }
            set { textBox.Text = value; }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var settingsFile = new IniFile("SAMSettings.ini");

            settingsFile.Write("AccountsPerRow", textBox.Text, "Settings");
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
