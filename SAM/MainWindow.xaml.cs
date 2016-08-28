using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SAM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class Account
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public string Url { get; set; }
    }

    [Serializable]
    public partial class MainWindow : Window
    {

        #region Globals

        private static List<Account> encryptedAccounts;
        private static List<Account> decryptedAccounts;

        private static string eKey = "PRIVATE_KEY"; // Change this before use

        private static string account;
        private static string ePassword;

        private static string accPerRow;

        private static Version latest;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //verion number from assembly
            string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            try
            {
                int idx = AssemblyVersion.LastIndexOf('0') - 1;
                AssemblyVersion = AssemblyVersion.Substring(0, idx);
            }
            catch
            {

            }

            //Check for a new version.
            int updateResult = await CheckForUpdate();
            if (updateResult == -1)
            {
                //Some Error occurred.
                //TODO: Handle this Error.
            }
            else if (updateResult == 1)
            {
                //An update is available, but user has chosen not to update.

            }
            else if (updateResult == 2)
            {
                //An update is available, and the user has chosen to update.
                //TODO: Initiate a process that downloads new updated binaries.
                Close();
            }

            if (!File.Exists("SAMSettings.ini"))
            {
                var settingsFile = new IniFile("SAMSettings.ini");
                settingsFile.Write("Version", AssemblyVersion, "System");
                settingsFile.Write("AccountsPerRow", "5", "Settings");
                accPerRow = "5";
            }
            else
            {
                var settingsFile = new IniFile("SAMSettings.ini");
                accPerRow = settingsFile.Read("AccountsPerRow", "Settings");

                if (File.Exists("info.dat"))
                {
                    StreamReader datReader = new StreamReader("info.dat");
                    string temp = datReader.ReadLine();
                    datReader.Close();

                    if (!temp.Contains("xml"))
                    {
                        MessageBox.Show("Your info.dat is out of date and must be deleted.\nSorry for the inconvenience!");

                        try
                        {
                            File.Delete("info.dat");
                        }
                        catch (Exception m)
                        {
                            Console.WriteLine(m.Message);
                        }
                    }
                }
                settingsFile.Write("Version", AssemblyVersion, "System");
            }

            RefreshWindow();
        }

        private void RefreshWindow()
        {
            decryptedAccounts = new List<Account>();

            //check if info.dat exists
            if (File.Exists("info.dat"))
            {
                //deserialize file and count the number of entries
                encryptedAccounts = Deserialize();
                postDeserializedRefresh(true);
                
            }
            else
            {
                encryptedAccounts = new List<Account>();
            }
        }

        private void postDeserializedRefresh(bool seedAcc)
        {
           
            if (encryptedAccounts != null)
            {
                int bCounter = 0;
                int xcounter = 0;
                int ycounter = 0;

                foreach (var account in encryptedAccounts)
                {
                    string temppass = StringCipher.Decrypt(account.Password, eKey);

                    if (seedAcc)
                    {
                        decryptedAccounts.Add(new Account() { Name = account.Name, Password = temppass, Url = account.Url });
                    }

                    //Console.WriteLine("Name = {0}, Pass = {1}, Url = {2}", account.Name, account.Password, account.Url);
                    Console.WriteLine("Name = {0}, Pass = {1}, Url = {2}", account.Name, temppass, account.Url);

                    Button accountButton = new Button();
                    Label accountLabel = new Label();

                    accountButton.Style = (Style)Resources["MyButtonStyle"];

                    accountButton.Tag = bCounter.ToString();

                    accountButton.Name = account.Name;
                    accountLabel.Name = account.Name + "Label";
                    accountLabel.Content = account.Name;

                    accountButton.Height = 100;
                    accountButton.Width = 100;
                    accountLabel.Height = 30;
                    accountLabel.Width = 100;

                    accountButton.HorizontalAlignment = HorizontalAlignment.Left;
                    accountButton.VerticalAlignment = VerticalAlignment.Top;
                    accountLabel.HorizontalAlignment = HorizontalAlignment.Left;
                    accountLabel.VerticalAlignment = VerticalAlignment.Top;

                    accountButton.Margin = new Thickness(15 + (xcounter * 120), (ycounter * 120) + 34, 0, 0);
                    accountLabel.Margin = new Thickness(15 + (xcounter * 120), (ycounter * 120) + 128, 0, 0);


                    accountButton.BorderBrush = null;
                    accountLabel.Foreground = new SolidColorBrush(Colors.White);

                    if (account.Url == null || account.Url == "" || account.Url == " ")
                    {
                        accountButton.Content = account.Name;
                        accountButton.Background = Brushes.LightGray;
                    }
                    else
                    {
                        try
                        {
                        ImageBrush brush1 = new ImageBrush();
                        BitmapImage image = new BitmapImage(new Uri(account.Url));
                        brush1.ImageSource = image;
                        accountButton.Background = brush1;
                        MainGrid.Children.Add(accountLabel);
                        }
                        catch (Exception m)
                        {
                            //probably no internet connection or avatar url is bad
                            Console.WriteLine("Error: " + m.Message);

                            accountButton.Content = account.Name;
                        }
                    }

                    MainGrid.Children.Add(accountButton);
                    
                    accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                    ContextMenu accountContext = new ContextMenu();
                    MenuItem menuItem1 = new MenuItem();
                    menuItem1.Header = "Delete Account";
                    accountContext.Items.Add(menuItem1);
                    accountButton.ContextMenu = accountContext;
                    menuItem1.Click += delegate { deleteAccount(accountButton); };

                    bCounter++;
                    xcounter++;

                    if ((xcounter % Int32.Parse(accPerRow) == 0) && xcounter != 0)
                    {
                        ycounter++;
                        xcounter = 0;
                    }
                }

                int xval = 0;

                //adjust window size and info positions
                if (ycounter == 0)
                {
                    xval = xcounter + 1;
                    Application.Current.MainWindow.Height = (190);
                }
                else
                {
                    xval = Int32.Parse(accPerRow);
                    Application.Current.MainWindow.Height = (185 + (125 * ycounter));
                }

                Application.Current.MainWindow.Width = (xval * 120) + 25;

                //adjust new account button
                NewButton.Margin = new Thickness(33 + (xcounter * 120), (ycounter * 120) + 52, 0, 0);
            }
        }

        private void deleteAccount(object butt)
        {
            Button button = butt as Button;
            encryptedAccounts.RemoveAt(Int32.Parse(button.Tag.ToString()));
            Serialize(encryptedAccounts);
            hardRefresh();
        }

        private void hardRefresh()
        {
            MainGrid.Children.RemoveRange(2, MainGrid.Children.Count - 2);
            postDeserializedRefresh(false);
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            NewAccount();
        }

        private void NewAccount()
        {
            // User entered info
            var dialog = new TextDialog();
            if (dialog.ShowDialog() == true)
            {
                string[] input = dialog.ResponseText.Split(' ');
                account = input[0];
                string password = input[1];
                string profUrl = null;
                HtmlDocument document = null;

                //if user entered profile url, get avatar jpg url
                if (input.Length > 2)
                {
                    if (input[2] != "")
                    {
                        document = new HtmlWeb().Load(input[2]);
                        var urls = document.DocumentNode.Descendants("img").Select(t => t.GetAttributeValue("src", null)).Where(s => !String.IsNullOrEmpty(s));

                        foreach (string url in urls)
                        {
                            if (url.Contains("http://cdn.akamai.steamstatic.com/steamcommunity/public/images/avatars/") && url.Contains("full.jpg"))
                            {
                                profUrl = url;
                            }
                        }
                    }
                }
                try
                {
                    // Encrypt info before saving to file
                    ePassword = StringCipher.Encrypt(password, eKey);

                    encryptedAccounts.Add(new Account() { Name = account, Password = ePassword, Url = profUrl });

                    Serialize(encryptedAccounts);

                    RefreshWindow();
                }
                catch (Exception m)
                {
                    MessageBox.Show("Error: " + m.Message);

                    var itemToRemove = encryptedAccounts.Single(r => r.Name == account);
                    encryptedAccounts.Remove(itemToRemove);

                    Serialize(encryptedAccounts);

                    NewAccount();
                }
            }
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                //kill steam process if it is already open
                try
                {
                    Process[] SteamProc = Process.GetProcessesByName("Steam");
                    SteamProc[0].Kill();
                    Thread.Sleep(1314);
                }
                catch
                {
                    Console.WriteLine("No steam process found.");
                }

                string defaultPath = @"C:\Program Files (x86)\Steam\";
                string secondaryPath = @"D:\Program Files (x86)\Steam\";
                string tertiaryPath = @"E:\Program Files (x86)\Steam\";

                string path = defaultPath;

                if (Directory.Exists(defaultPath))
                {
                    path = defaultPath;  
                }
                else if (Directory.Exists(secondaryPath))
                {
                    path = secondaryPath;
                }
                else if (Directory.Exists(tertiaryPath))
                {
                    path = tertiaryPath;
                }
                else
                {
                    //error
                    //prompt user to find steam install
                    var settingsFile = new IniFile("SAMSettings.ini");

                    if (settingsFile.KeyExists("Steam", "Settings"))
                    {
                        path = settingsFile.Read("Steam", "Settings");
                    }
                    else
                    {
                        // Create OpenFileDialog 
                        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                        // Set filter for file extension and default file extension 
                        dlg.DefaultExt = ".exe";
                        dlg.Filter = "Steam (*.exe)|*.exe";

                        // Display OpenFileDialog by calling ShowDialog method 
                        Nullable<bool> result = dlg.ShowDialog();

                        // Get the selected file path
                        if (result == true)
                        {
                            path = Path.GetDirectoryName(dlg.FileName) + "\\";
                            settingsFile.Write("Steam", path, "Settings");
                        }
                    }
                }

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = true;
                startInfo.FileName = path + "Steam.exe";
                startInfo.WorkingDirectory = path;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "-login " + btn.Name.ToString() + " " + decryptedAccounts[Int32.Parse(btn.Tag.ToString())].Password.ToString();

                try
                {
                    //sart the process with the info specified
                    Process exeProcess = Process.Start(startInfo);
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK);
                    return;
                }
            }
        }

        //Checks if an update is available. 
        //-1 for check Error, 0 for no update, 1 for update is available, 2 for perform update.
        private static async Task<int> CheckForUpdate()
        {
            //Nkosi Note: Always use asynchronous versions of network and IO methods.

            //Check for version updates
            var client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 0, 10);
            try
            {
                // Open the text file using a stream reader
                using (Stream stream = await client.GetStreamAsync("http://textuploader.com/58mva/raw"))
                {
                    System.Version current = Assembly.GetExecutingAssembly().GetName().Version;
                    StreamReader reader = new StreamReader(stream);
                    latest = System.Version.Parse(reader.ReadToEnd());

                    if (latest != current)
                    {
                        MessageBoxResult answer = MessageBox.Show("A new version of SAM is available!\n\nCurrent Version     " + current + "\nLatest Version     " + latest + "\n\nUpdate now?", "SAM Update", MessageBoxButton.YesNo);
                        if (answer == MessageBoxResult.Yes)
                        {
                            //TODO: Later on, remove this and replace with automated process of downloading new binaries.
                            Process.Start("https://github.com/rex706/SAM");

                            //Update is available, and user wants to update. Requires app to close.
                            return 2;
                        }
                        //Update is available, but user chose not to update just yet.
                        return 1;
                    }
                }
                //No update available.
                return 0;
            }
            catch (Exception m)
            {
                //MessageBox.Show("Failed to check for update.\n" + m.Message,"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        #region Serialize/Deserialize

        public static void Serialize(List<Account> input)
        {
            var serializer = new XmlSerializer(input.GetType());
            var sw = new StreamWriter("info.dat");
            serializer.Serialize(sw, input);
            sw.Close();
        }

        public static List<Account> Deserialize()
        {
            var stream = new StreamReader("info.dat");
            var ser = new XmlSerializer(typeof(List<Account>));
            object obj = ser.Deserialize(stream);
            stream.Close();
            return (List<Account>)obj;
        }

        #endregion

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new Window1();
            settingsDialog.ShowDialog();

            accPerRow = settingsDialog.ResponseText;
            hardRefresh();
        }

        private void GitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/rex706/SAM");
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}