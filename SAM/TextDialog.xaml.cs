using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
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
            string steamPath = new IniFile("SAMSettings.ini").Read("Steam", "Settings");

            try
            {
                // Attempt to find user profile image automatically from steam config and web api.
                dynamic config = VdfConvert.Deserialize(File.ReadAllText(steamPath + "config\\config.vdf"));

                dynamic accounts = config.Value.Software.Valve.steam.Accounts;

                string userName = UsernameBox.Text;

                VObject accountsObj = accounts;

                VToken value;

                accountsObj.TryGetValue(userName, out value);

                dynamic user = value;

                VValue userId = user.SteamID;

                string userIdValue = userId.Value.ToString();

                Uri apiUri = new Uri("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + apiKey + "&steamids=" + userIdValue);

                using (WebClient client = new WebClient())
                {
                    string jsonString = await client.DownloadStringTaskAsync(apiUri);

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
