using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

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

        public string AviText { get; set; }

        public string SteamId { get; set; }

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

        private async void UsernameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            OKButton.IsEnabled = false;
            dynamic userJson = await Utils.GetUrlsFromWebApiByName(UsernameBox.Text);

            if (userJson != null)
            {
                dynamic profileUrl = userJson.response.players[0].profileurl;
                dynamic avatarUrl = userJson.response.players[0].avatarfull;
                dynamic steamId = userJson.response.players[0].steamid;

                UrlBox.Text = profileUrl;

                SteamId = steamId;
                AviText = avatarUrl;
            }
            else
            {
                SteamId = null;
                AviText = null;
            }

            OKButton.IsEnabled = true;
        }
    }
}
