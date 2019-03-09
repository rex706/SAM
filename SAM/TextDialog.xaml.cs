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

        private static string apiKey = "API_KEY";

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

        private async void UsernameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await GetAviUrlFromConfig();
        }

        private async Task GetAviUrlFromConfig()
        {
            string steamPath = settingsFile.Read("Steam", "Settings");

            try
            {
                // Attempt to find user profile image automatically from web api.
                string userName = UsernameBox.Text;

                Uri vanityUri = new Uri("http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key=" + apiKey + "&vanityurl=" + userName);

                using (WebClient client = new WebClient())
                {
                    string vanityJson = await client.DownloadStringTaskAsync(vanityUri);
                    dynamic vanityValue = JValue.Parse(vanityJson);

                    dynamic steamId = vanityValue.response.steamid;

                    Uri userUri = new Uri("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + apiKey + "&steamids=" + steamId);

                    string jsonString = await client.DownloadStringTaskAsync(userUri);
                    dynamic jsonValue = JValue.Parse(jsonString);

                    dynamic profileUrl = jsonValue.response.players[0].profileurl;
                    dynamic avatarUrl = jsonValue.response.players[0].avatarfull;

                    UrlBox.Text = profileUrl;
                }
            }
            catch (Exception m)
            {
                //MessageBox.Show(m.Message);
            }
        }
    }
}
