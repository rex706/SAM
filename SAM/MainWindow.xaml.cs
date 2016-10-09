using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace SAM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class Account
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public string ProfUrl { get; set; }

        public string AviUrl { get; set; }
    }

    [Serializable]
    public partial class MainWindow : Window
    {
        #region Globals

        private static List<Account> encryptedAccounts;
        private static List<Account> decryptedAccounts;
        
        private static string eKey = "PRIVATE_KEY"; // Change this before release

        private static string account;
        private static string ePassword;

        private static string accPerRow;

        private static string AssemblyVer;
        private static Version latest;

        private static string fileParams;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Verion number from assembly
            AssemblyVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            MenuItem ver = new MenuItem();
            MenuItem newExistMenuItem = (MenuItem)this.FileMenu.Items[2];
            ver.Header = "v" + AssemblyVer;
            ver.IsEnabled = false;
            newExistMenuItem.Items.Add(ver);

            // Check for a new version.
            int updateResult = await CheckForUpdate();
            if (updateResult == -1)
            {
                // Some Error occurred.
                // TODO: Handle this Error.
            }
            else if (updateResult == 1)
            {
                // An update is available, but user has chosen not to update.
                ver.Header = "Update Available!";
                ver.Click += Ver_Click;
                ver.IsEnabled = true;
            }
            else if (updateResult == 2)
            {
                // An update is available, and the user has chosen to update.
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = true;
                startInfo.FileName = "Updater.exe";
                startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = fileParams;

                Process.Start(startInfo);
                Environment.Exit(0);
            }

            // If no settings file exists, create one and initialize values
            if (!File.Exists("SAMSettings.ini"))
            {
                var settingsFile = new IniFile("SAMSettings.ini");
                settingsFile.Write("Version", AssemblyVer, "System");
                settingsFile.Write("AccountsPerRow", "5", "Settings");
                accPerRow = "5";
            }
            // Else load settings from preexisting file
            else
            {
                var settingsFile = new IniFile("SAMSettings.ini");
                accPerRow = settingsFile.Read("AccountsPerRow", "Settings");

                if (!Regex.IsMatch(accPerRow, @"^\d+$") || Int32.Parse(accPerRow) < 1)
                    accPerRow = "1";

                if (File.Exists("info.dat"))
                {
                    StreamReader datReader = new StreamReader("info.dat");
                    string temp = datReader.ReadLine();
                    datReader.Close();

                    // If the user is some how using an older info.dat, delete it
                    if (!temp.Contains("xml"))
                    {
                        MessageBox.Show("Your info.dat is out of date and must be deleted.\nSorry for the inconvenience!", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Information);

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
                settingsFile.Write("Version", AssemblyVer, "System");
            }
            RefreshWindow();
        }

        private void RefreshWindow()
        {
            decryptedAccounts = new List<Account>();
            buttonGrid.Children.Clear();

            // Check if info.dat exists
            if (File.Exists("info.dat"))
            {
                // Deserialize file
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

                if (seedAcc)
                {
                    decryptedAccounts.Clear();
                }

                // Create new button and textblock for each account
                foreach (var account in encryptedAccounts)
                {
                    string temppass = StringCipher.Decrypt(account.Password, eKey);

                    if (seedAcc)
                    {
                        decryptedAccounts.Add(new Account() { Name = account.Name, Password = temppass, ProfUrl = account.ProfUrl, AviUrl = account.AviUrl });
                    }

                    //Console.WriteLine("Name = {0}, Pass = {1}, Url = {2}", account.Name, account.Password, account.Url);
                    //Console.WriteLine("Name = {0}, Pass = {1}, Url = {2}", account.Name, temppass, account.Url);

                    Button accountButton = new Button();
                    TextBlock accountText = new TextBlock();

                    accountButton.Style = (Style)Resources["MyButtonStyle"];

                    accountButton.Tag = bCounter.ToString();

                    accountButton.Name = account.Name;
                    accountText.Name = account.Name + "Label";
                    accountText.Text = account.Name;

                    accountButton.Height = 100;
                    accountButton.Width = 100;
                    accountText.Height = 30;
                    accountText.Width = 100;

                    accountButton.HorizontalAlignment = HorizontalAlignment.Left;
                    accountButton.VerticalAlignment = VerticalAlignment.Top;
                    accountText.HorizontalAlignment = HorizontalAlignment.Left;
                    accountText.VerticalAlignment = VerticalAlignment.Top;

                    accountButton.Margin = new Thickness(15 + (xcounter * 120), (ycounter * 120) + 14, 0, 0);
                    accountText.Margin = new Thickness(15 + (xcounter * 120), (ycounter * 120) + 113, 0, 0);

                    accountButton.BorderBrush = null;
                    accountText.Foreground = new SolidColorBrush(Colors.White);

                    if (account.AviUrl == null || account.AviUrl == "" || account.AviUrl == " ")
                    {
                        accountButton.Content = account.Name;
                        accountButton.Background = Brushes.LightGray;
                    }
                    else
                    {
                        try
                        {
                            ImageBrush brush1 = new ImageBrush();
                            BitmapImage image = new BitmapImage(new Uri(account.AviUrl));
                            brush1.ImageSource = image;
                            accountButton.Background = brush1;
                            buttonGrid.Children.Add(accountText);
                        }
                        catch (Exception m)
                        {
                            // Probably no internet connection or avatar url is bad
                            Console.WriteLine("Error: " + m.Message);

                            accountButton.Content = account.Name;
                        }
                    }

                    buttonGrid.Children.Add(accountButton);
                    
                    accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                    ContextMenu accountContext = new ContextMenu();
                    MenuItem deleteItem = new MenuItem();
                    MenuItem editItem = new MenuItem();
                    deleteItem.Header = "Delete";
                    editItem.Header = "Edit";
                    accountContext.Items.Add(editItem);
                    accountContext.Items.Add(deleteItem);
                    accountButton.ContextMenu = accountContext;
                    deleteItem.Click += delegate { deleteEntry(accountButton); };
                    editItem.Click += delegate { editEntry(accountButton); };

                    bCounter++;
                    xcounter++;

                    if ((xcounter % Int32.Parse(accPerRow) == 0) && xcounter != 0)
                    {
                        ycounter++;
                        xcounter = 0;
                    }
                }

                int xval = 0;

                // Adjust window size and info positions
                if (ycounter == 0)
                {
                    xval = xcounter + 1;
                    Application.Current.MainWindow.Height = (190);
                    buttonGrid.Height = 141;
                }
                else
                {
                    xval = Int32.Parse(accPerRow);
                    Application.Current.MainWindow.Height = (185 + (125 * ycounter));
                    buttonGrid.Height = 141 * (125 + ycounter);
                }

                Application.Current.MainWindow.Width = (xval * 120) + 25;
                buttonGrid.Width = (xval * 120) + 25;

                // Adjust new account button
                NewButton.Margin = new Thickness(33 + (xcounter * 120), (ycounter * 120) + 52, 0, 0);
            }
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
                account = dialog.AccountText;
                string password = dialog.PasswordText;
                string aviUrl = "";
                HtmlDocument document = null;

                // If user entered profile url, get avatar jpg url
                if (dialog.UrlText.Length > 2)
                {
                    if (dialog.UrlText.Contains("http://steamcommunity.com/"))
                    {
                        document = new HtmlWeb().Load(dialog.UrlText);
                        var urls = document.DocumentNode.Descendants("img").Select(t => t.GetAttributeValue("src", null)).Where(s => !String.IsNullOrEmpty(s));

                        foreach (string url in urls)
                        {
                            if (url.Contains("http://cdn.akamai.steamstatic.com/steamcommunity/public/images/avatars/") && url.Contains("full.jpg"))
                            {
                                aviUrl = url;
                            }
                        }
                    }
                }
                try
                {
                    // Encrypt info before saving to file
                    ePassword = StringCipher.Encrypt(password, eKey);

                    encryptedAccounts.Add(new Account() { Name = dialog.AccountText, Password = ePassword, ProfUrl = dialog.UrlText, AviUrl = aviUrl });

                    Serialize(encryptedAccounts);

                    RefreshWindow();
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    var itemToRemove = encryptedAccounts.Single(r => r.Name == dialog.AccountText);
                    encryptedAccounts.Remove(itemToRemove);

                    Serialize(encryptedAccounts);

                    NewAccount();
                }
            }
        }

        private void editEntry(object butt)
        {
            Button button = butt as Button;

            var dialog = new TextDialog();
            dialog.AccountText = decryptedAccounts[Int32.Parse(button.Tag.ToString())].Name;
            dialog.PasswordText = decryptedAccounts[Int32.Parse(button.Tag.ToString())].Password;
            dialog.UrlText = decryptedAccounts[Int32.Parse(button.Tag.ToString())].ProfUrl;

            if (dialog.ShowDialog() == true)
            {
                string aviUrl = "";
                HtmlDocument document = null;

                // If user entered profile url, get avatar jpg url
                if (dialog.UrlText.Length > 2)
                {
                    if (dialog.UrlText.Contains("http://steamcommunity.com/"))
                    {
                        document = new HtmlWeb().Load(dialog.UrlText);
                        var urls = document.DocumentNode.Descendants("img").Select(t => t.GetAttributeValue("src", null)).Where(s => !String.IsNullOrEmpty(s));

                        foreach (string url in urls)
                        {
                            if (url.Contains("http://cdn.akamai.steamstatic.com/steamcommunity/public/images/avatars/") && url.Contains("full.jpg"))
                            {
                                aviUrl = url;
                            }
                        }
                    }
                }
                try
                {
                    // Encrypt info before saving to file
                    ePassword = StringCipher.Encrypt(dialog.PasswordText, eKey);

                    encryptedAccounts[Int32.Parse(button.Tag.ToString())].Name = dialog.AccountText;
                    encryptedAccounts[Int32.Parse(button.Tag.ToString())].Password = ePassword;
                    encryptedAccounts[Int32.Parse(button.Tag.ToString())].ProfUrl = dialog.UrlText;
                    encryptedAccounts[Int32.Parse(button.Tag.ToString())].AviUrl = aviUrl;

                    Serialize(encryptedAccounts);
                    RefreshWindow();
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    editEntry(butt);
                }
            }
        }



        private void deleteEntry(object butt)
        {
            Button button = butt as Button;
            encryptedAccounts.RemoveAt(Int32.Parse(button.Tag.ToString()));
            Serialize(encryptedAccounts);
            RefreshWindow();
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                // Kill steam process if it is already open
                try
                {
                    Process[] SteamProc = Process.GetProcessesByName("Steam");
                    SteamProc[0].Kill();
                    SteamProc[0].WaitForExit();
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
                    // Error
                    // Prompt user to find steam install
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
                    // Sart the process with the info specified
                    Process exeProcess = Process.Start(startInfo);
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }

        // Checks if an update is available. 
        // -1 for check Error, 0 for no update, 1 for update is available, 2 for perform update.
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
                    latest = System.Version.Parse(await reader.ReadLineAsync());

                    List<string> newFiles = new List<string>();

                    while (!reader.EndOfStream)
                    {
                        newFiles.Add(await reader.ReadLineAsync());
                    }

                    for(int i = 0; i < newFiles.Count; i++)
                    {
                        if (i == 0)
                            fileParams += newFiles[i];
                        else
                            fileParams += " " + newFiles[i];
                    }

                    if (latest > current)
                    {
                        MessageBoxResult answer = MessageBox.Show("A new version of SAM is available!\n\nCurrent Version     " + current + "\nLatest Version        " + latest + "\n\nUpdate now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (answer == MessageBoxResult.Yes)
                        {
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
                //MessageBox.Show("Failed to check for update.\n" + m.Message,"Error", MessageBoxButtons.OK, MessageBoxImage.Error);
                //return -1;
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

        #region File Menu Click Events

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new Window1();
            settingsDialog.ShowDialog();

            accPerRow = settingsDialog.ResponseText;
            RefreshWindow();
        }

        private void GitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/rex706/SAM");
        }

        private void Ver_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show("A new version of SAM is available!\n\nCurrent Version     " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\nLatest Version        " + latest + "\n\nUpdate now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
            {
                // An update is available, and the user has chosen to update.
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = true;
                startInfo.FileName = "Updater.exe";
                startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = fileParams;

                Process.Start(startInfo);
                Environment.Exit(0);
            }
        }

        private void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RefreshWindow();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}