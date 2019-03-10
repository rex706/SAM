using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using HtmlAgilityPack;

namespace SAM
{
    class Utils
    {
        private static string apiKey = "API_KEY";

        public static void Serialize(List<Account> input)
        {
            var serializer = new XmlSerializer(input.GetType());
            var sw = new StreamWriter("info.dat");
            serializer.Serialize(sw, input);
            sw.Close();
        }

        public static List<Account> Deserialize(string file)
        {
            var stream = new StreamReader(file);
            var ser = new XmlSerializer(typeof(List<Account>));
            object obj = ser.Deserialize(stream);
            stream.Close();
            return (List<Account>)obj;
        }

        public static void ImportAccountFile()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                DefaultExt = ".dat",
                Filter = "SAM DAT Files (*.dat)|*.dat"
            };

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    var tempAccounts = Deserialize(dialog.FileName);
                    MainWindow.encryptedAccounts = MainWindow.encryptedAccounts.Concat(tempAccounts).ToList();
                    Serialize(MainWindow.encryptedAccounts);
                    MessageBox.Show("Accounts imported!");
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void ExportAccountFile()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.Copy("info.dat", dialog.SelectedPath + "\\info.dat");
                    MessageBox.Show("File exported to:\n" + dialog.SelectedPath);
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void ExportSelectedAccounts(List<Account> accounts)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var serializer = new XmlSerializer(accounts.GetType());
                    var sw = new StreamWriter(dialog.SelectedPath + "\\info.dat");
                    serializer.Serialize(sw, accounts);
                    sw.Close();

                    MessageBox.Show("File exported to:\n" + dialog.SelectedPath);
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static string GetSteamPathFromRegistry()
        {
            string registryValue = string.Empty;
            RegistryKey localKey = null;
            if (Environment.Is64BitOperatingSystem)
            {
                localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, RegistryView.Registry64);
            }
            else
            {
                localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, RegistryView.Registry32);
            }

            try
            {
                localKey = localKey.OpenSubKey(@"Software\\Valve\\Steam");
                registryValue = localKey.GetValue("SteamPath").ToString() + "/";
            }
            catch (NullReferenceException nre)
            {

            }
            return registryValue;
        }

        public static async Task<dynamic> GetUrlsFromWebApiByName(string userName)
        {
            dynamic userJson = null;

            try
            {
                // Attempt to find user profile image automatically from web api.

                Uri vanityUri = new Uri("http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key=" + apiKey + "&vanityurl=" + userName);

                using (WebClient client = new WebClient())
                {
                    string vanityJson = await client.DownloadStringTaskAsync(vanityUri);
                    dynamic vanityValue = JValue.Parse(vanityJson);

                    dynamic steamId = vanityValue.response.steamid;

                    // Not found.
                    if (steamId == null)
                    {
                        string steamPath = new IniFile("SAMSettings.ini").Read("Steam", "Settings");

                        // Attempt to find userId from steam config.
                        dynamic config = VdfConvert.Deserialize(File.ReadAllText(steamPath + "config\\config.vdf"));
                        dynamic accounts = config.Value.Software.Valve.steam.Accounts;

                        VObject accountsObj = accounts;
                        VToken value;

                        accountsObj.TryGetValue(userName, out value);

                        dynamic user = value;
                        VValue userId = user.SteamID;
                        steamId = userId.Value.ToString();
                    }

                    userJson = await GetUrlsFromWebApiBySteamId(steamId);
                }
            }
            catch (Exception m)
            {
                //MessageBox.Show(m.Message);
            }

            return userJson;
        }

        public static async Task<dynamic> GetUrlsFromWebApiBySteamId(string steamId)
        {
            dynamic userJson = null;
            try
            {
                Uri userUri = new Uri("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + apiKey + "&steamids=" + steamId);

                using (WebClient client = new WebClient())
                {
                    string userJsonString = await client.DownloadStringTaskAsync(userUri);
                    userJson = JValue.Parse(userJsonString);
                }
            }
            catch(Exception m)
            {
                //MessageBox.Show(m.Message);
            }

            return userJson;
        }

        public static string HtmlAviScrape(string profUrl)
        {
            // If user entered profile url, get avatar jpg url
            if (profUrl != null && profUrl.Length > 2)
            {
                // Verify url starts with valid prefix for HtmlWeb
                Uri profileUri = new Uri(profUrl);

                try
                {
                    HtmlDocument document = new HtmlWeb().Load(profileUri);
                    return document.DocumentNode.Descendants().Where(n => n.HasClass("playerAvatarAutoSizeInner")).First().FirstChild.GetAttributeValue("src", null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return "";
        }
    }
}
