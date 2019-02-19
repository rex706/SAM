using System.Windows;

namespace SAM
{
    /// <summary>
    /// Interaction logic for TextDialog.xaml
    /// </summary>
    public partial class TextDialog : Window
    {
        private IniFile settingsFile;

        public TextDialog()
        {
            InitializeComponent();
            settingsFile = new IniFile("SAMSettings.ini");
        }

        public string AccountText
        {
            get { return UsernameBox.Text; }
            set { UsernameBox.Text = value; }
        }

        public string PasswordText
        {
            get { return PasswordBox.Password; }
            set { PasswordBox.Password = value; }
        }

        public string SharedSecretText
        {
            get { return SharedSecretBox.Password; }
            set { SharedSecretBox.Password = value; }
        }

        public string UrlText
        {
            get { return UrlBox.Text; }
            set { UrlBox.Text = value; }
        }

        public string DescriptionText
        {
            get { return DescriptionBox.Text; }
            set { DescriptionBox.Text = value; }
        }

        public bool AutoLogAccountIndex { get; set; }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (autoLogCheckBox.IsChecked == true)
                AutoLogAccountIndex = true;
            else
                AutoLogAccountIndex = false;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
