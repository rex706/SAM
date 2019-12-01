using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows;

namespace SAM
{
    /// <summary>
    /// Interaction logic for TextDialog.xaml
    /// </summary>
    public partial class TextDialog : MetroWindow
    {
        public TextDialog()
        {
            InitializeComponent();
        }

        public string AccountText
        {
            get { return UsernameBox.Text; }
            set { UsernameBox.Text = value; }
        }

        public string AliasText
        {
            get { return AliasBox.Text;  }
            set { AliasBox.Text = value; }
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
            get { return UrlTextBox.Text; }
            set { UrlTextBox.Text = value; }
        }

        private string OriginalUrlText { get; set; }

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
            if (AccountText == null || AccountText.Length == 0)
            {
                MessageBox.Show("Account login required!");
                UsernameBox.Focus();
                return;
            }
            if (PasswordText == null || PasswordText.Length == 0)
            {
                MessageBox.Show("Account password required!");
                PasswordBox.Focus();
                return;
            }

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
            if (UsernameBox.Text.Length < 3)
            {
                return;
            }

            OKButton.IsEnabled = false;

            dynamic userJson = await Utils.GetUserInfoFromConfigAndWebApi(UsernameBox.Text.ToString());

            if (userJson != null)
            {
                try
                {
                    dynamic profileUrl = userJson.response.players[0].profileurl;
                    dynamic avatarUrl = userJson.response.players[0].avatarfull;
                    dynamic steamId = userJson.response.players[0].steamid;

                    UrlTextBox.Text = profileUrl;

                    SteamId = steamId;
                    AviText = avatarUrl;
                }
                catch
                {

                }
            }
            else
            {
                SteamId = null;
                AviText = null;
            }

            OKButton.IsEnabled = true;
        }

        private void UrlBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OriginalUrlText = UrlTextBox.Text;
        }

        private async void UrlBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (UrlTextBox.Text != OriginalUrlText && SteamId == null)
            {
                OKButton.IsEnabled = false; 

                dynamic steamId = await Utils.GetSteamIdFromProfileUrl(UrlTextBox.Text);

                if (steamId != null)
                {
                    SteamId = steamId;
                }

                OKButton.IsEnabled = true;
            }
        }
    }
}
