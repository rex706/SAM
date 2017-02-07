using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

        public string Description { get; set; }
    }

    [Serializable]
    public partial class MainWindow : Window
    {
        #region Globals

        public static List<Account> encryptedAccounts;
        private static List<Account> decryptedAccounts;

        private static string eKey = "PRIVATE_KEY"; // Change this before release

        private static string account;
        private static string ePassword;

        private static string accPerRow;
        private static string steamPath;

        private static bool selected = false;
        private static int selectedAcc = -1;
        private static bool recent = false;
        private static int recentAcc = -1;

        private static string AssemblyVer;

        IniFile settingsFile;

        // Resize animation vars
        private static System.Windows.Forms.Timer _Timer = new System.Windows.Forms.Timer();
        private int _Stop = 0;
        private double _RatioHeight;
        private double _RatioWidth;
        private double _Height;
        private double _Width;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);

            _Timer.Tick += new EventHandler(timer_Tick);
            _Timer.Interval = (10);
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
            if (await UpdateCheck.CheckForUpdate("http://textuploader.com/58mva/raw") == 1)
            {
                // An update is available, but user has chosen not to update.
                ver.Header = "Update Available!";
                ver.Click += Ver_Click;
                ver.IsEnabled = true;
            }

            // If no settings file exists, create one and initialize values.
            if (!File.Exists("SAMSettings.ini"))
            {
                settingsFile = new IniFile("SAMSettings.ini");
                settingsFile.Write("Version", AssemblyVer, "System");
                settingsFile.Write("AccountsPerRow", "5", "Settings");
                settingsFile.Write("StartWithWindows", "Settings");
                settingsFile.Write("Recent", "False", "AutoLog");
                settingsFile.Write("RecentAcc","", "AutoLog");
                settingsFile.Write("Selected", "False", "AutoLog");
                settingsFile.Write("SelectedAcc", "", "AutoLog");
                accPerRow = "5";
            }
            // Else load settings from preexisting file.
            else
            {
                settingsFile = new IniFile("SAMSettings.ini");
                accPerRow = settingsFile.Read("AccountsPerRow", "Settings");

                if (!Regex.IsMatch(accPerRow, @"^\d+$") || Int32.Parse(accPerRow) < 1)
                    accPerRow = "1";

                if (settingsFile.KeyExists("Steam", "Settings"))
                    steamPath = settingsFile.Read("Steam", "Settings");

                // If the recent autolog entry exists and is set to true.
                // else create defualt settings file entry.
                if (settingsFile.KeyExists("Recent", "AutoLog") && settingsFile.Read("Recent", "AutoLog") == "True" && Int32.Parse(settingsFile.Read("RecentAcc", "AutoLog")) >=0)
                {
                    recent = true;
                    recentAcc = Int32.Parse(settingsFile.Read("RecentAcc", "AutoLog"));
                }
                else if (!settingsFile.KeyExists("Recent", "AutoLog"))
                {
                    settingsFile.Write("Recent", "False", "AutoLog");
                    settingsFile.Write("RecentAcc", "-1", "AutoLog");
                }

                // If the selected autolog entry exists and is set to true.
                // else create defualt settings file entry.
                if (settingsFile.KeyExists("Selected", "AutoLog") && settingsFile.Read("Selected", "AutoLog") == "True")
                {
                    selected = true;
                    selectedAcc = Int32.Parse(settingsFile.Read("SelectedAcc", "AutoLog"));
                }
                else if (!settingsFile.KeyExists("Selected", "AutoLog"))
                {
                    settingsFile.Write("Selected", "False", "AutoLog");
                    settingsFile.Write("SelectedAcc", "-1", "AutoLog");
                }

                if (File.Exists("info.dat"))
                {
                    StreamReader datReader = new StreamReader("info.dat");
                    string temp = datReader.ReadLine();
                    datReader.Close();

                    // If the user is some how using an older info.dat, delete it.
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

            // Load window with account buttons.
            RefreshWindow();

            // Login to auto log account if enabled.
            if (recent == true)
                Login(recentAcc);
            else if (selected == true)
                Login(selectedAcc);
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

                // Create new button and textblock for each account
                foreach (var account in encryptedAccounts)
                {
                    string temppass = StringCipher.Decrypt(account.Password, eKey);

                    if (seedAcc)
                    {
                        decryptedAccounts.Add(new Account() { Name = account.Name, Password = temppass, ProfUrl = account.ProfUrl, AviUrl = account.AviUrl, Description = account.Description });
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

                    // If there is a description, set up tooltip.
                    if (account.Description.Length > 0)
                        accountButton.ToolTip = account.Description;

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

                    if (account.ProfUrl == "" || account.AviUrl == null || account.AviUrl == "" || account.AviUrl == " ")
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
                int newHeight;

                // Adjust window size and info positions
                if (ycounter == 0)
                {
                    xval = xcounter + 1;
                    newHeight = 190;
                    buttonGrid.Height = 141;
                }
                else
                {
                    xval = Int32.Parse(accPerRow);
                    newHeight = 185 + (125 * ycounter);
                    buttonGrid.Height = 141 * (125 + ycounter);
                }

                int newWidth = (xval * 120) + 25;

                resize(newHeight, newWidth);
                buttonGrid.Width = newWidth;

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

            if (dialog.ShowDialog() == true && dialog.AccountText != "" && dialog.PasswordText != "")
            {
                account = dialog.AccountText;
                string password = dialog.PasswordText;
                string aviUrl = htmlAviScrape(dialog.UrlText);

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settingsFile.Write("SelectedAcc", (encryptedAccounts.Count + 1).ToString(), "AutoLog");
                    settingsFile.Write("Selected", "True", "AutoLog");
                    settingsFile.Write("Recent", "False", "AutoLog");
                    selected = true;
                    recent = false;
                    selectedAcc = encryptedAccounts.Count + 1;
                }

                try
                {
                    // Encrypt info before saving to file
                    ePassword = StringCipher.Encrypt(password, eKey);

                    encryptedAccounts.Add(new Account() { Name = dialog.AccountText, Password = ePassword, ProfUrl = dialog.UrlText, AviUrl = aviUrl, Description = dialog.DescriptionText });

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
            int index = Int32.Parse(button.Tag.ToString());

            var dialog = new TextDialog();
            dialog.AccountText = decryptedAccounts[index].Name;
            dialog.PasswordText = decryptedAccounts[index].Password;
            dialog.UrlText = decryptedAccounts[index].ProfUrl;
            dialog.DescriptionText = decryptedAccounts[index].Description;

            if (selectedAcc == index)
                dialog.autoLogCheckBox.IsChecked = true;

            if (dialog.ShowDialog() == true)
            {
                string aviUrl = htmlAviScrape(dialog.UrlText);

                // If the auto login checkbox was checked, update settings file and global variables. 
                if (dialog.AutoLogAccountIndex == true)
                {
                    settingsFile.Write("SelectedAcc", button.Tag.ToString(), "AutoLog");
                    settingsFile.Write("Selected", "True", "AutoLog");
                    settingsFile.Write("Recent", "False", "AutoLog");
                    selected = true;
                    recent = false;
                    selectedAcc = index;
                }
                else
                {
                    settingsFile.Write("SelectedAcc", "-1", "AutoLog");
                    settingsFile.Write("Selected", "False", "AutoLog");
                    selected = false;
                    selectedAcc = -1;
                }

                try
                {
                    // Encrypt info before saving to file
                    ePassword = StringCipher.Encrypt(dialog.PasswordText, eKey);

                    encryptedAccounts[index].Name = dialog.AccountText;
                    encryptedAccounts[index].Password = ePassword;
                    encryptedAccounts[index].ProfUrl = dialog.UrlText;
                    encryptedAccounts[index].AviUrl = aviUrl;
                    encryptedAccounts[index].Description = dialog.DescriptionText;

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
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this entry?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (result == MessageBoxResult.Yes)
            {
                Button button = butt as Button;
                encryptedAccounts.RemoveAt(Int32.Parse(button.Tag.ToString()));
                Serialize(encryptedAccounts);
                RefreshWindow();
            }
        }

        private void Login(int index)
        {
            // Update the most recently used account index.
            recentAcc = index;
            settingsFile.Write("RecentAcc", index.ToString(), "AutoLog");

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

            // If Steam's filepath was not specified in settings. Attempt to find it and save it.
            if (steamPath == null || steamPath.Length < 3)
            {
                string defaultPath = @"C:\Program Files (x86)\Steam\";
                string secondaryPath = @"D:\Program Files (x86)\Steam\";
                string tertiaryPath = @"E:\Program Files (x86)\Steam\";

                if (Directory.Exists(defaultPath))
                {
                    steamPath = defaultPath;
                }
                else if (Directory.Exists(secondaryPath))
                {
                    steamPath = secondaryPath;
                }
                else if (Directory.Exists(tertiaryPath))
                {
                    steamPath = tertiaryPath;
                }
                else
                {
                    // Prompt user to find steam install
                    var settingsFile = new IniFile("SAMSettings.ini");

                    // Create OpenFileDialog 
                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                    // Set filter for file extension and default file extension 
                    dlg.DefaultExt = ".exe";
                    dlg.Filter = "Steam (*.exe)|*.exe";

                    // Display OpenFileDialog by calling ShowDialog method 
                    Nullable<bool> result = dlg.ShowDialog();

                    // Get the selected file path
                    if (result == true)
                        steamPath = Path.GetDirectoryName(dlg.FileName) + "\\";
                }

                // Save path to settings file.
                settingsFile.Write("Steam", steamPath, "Settings");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;
            startInfo.FileName = steamPath + "Steam.exe";
            startInfo.WorkingDirectory = steamPath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-login " + decryptedAccounts[index].Name + " " + decryptedAccounts[index].Password;

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

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                // Login with clicked button's index, which stored in Tag.
                Login(Int32.Parse(btn.Tag.ToString()));
            }
        }

        private string htmlAviScrape(string htmlString)
        {
            HtmlDocument document = null;

            // If user entered profile url, get avatar jpg url
            if (htmlString.Length > 2)
            {
                if (htmlString.Contains("https://"))
                {
                    htmlString = htmlString.Remove(4,1);
                }
                if (htmlString.Contains("http://steamcommunity.com/"))
                {
                    document = new HtmlWeb().Load(htmlString);
                    var urls = document.DocumentNode.Descendants("img").Select(t => t.GetAttributeValue("src", null)).Where(s => !String.IsNullOrEmpty(s));

                    foreach (string url in urls)
                    {
                        if ((url.Contains("http://cdn.akamai.steamstatic.com/steamcommunity/public/images/avatars/")
                            || url.Contains("http://cdn.edgecast.steamstatic.com/steamcommunity/public/images/avatars/")) && url.Contains("full.jpg"))
                        {
                            return url;
                        }
                    }
                }
            }
            return "";
        }

        #region Resize and Resize Timer

        public void resize(double _PassedHeight, double _PassedWidth)
        {
            _Height = _PassedHeight;
            _Width = _PassedWidth;

            _Timer.Enabled = true;
            _Timer.Start();
        }

        private void timer_Tick(Object myObject, EventArgs myEventArgs)
        {
            if (_Stop == 0)
            {
                _RatioHeight = ((this.Height - _Height) / 5) * -1;
                _RatioWidth = ((this.Width - _Width) / 5) * -1;
            }
            _Stop++;

            this.Height += _RatioHeight;
            this.Width += _RatioWidth;

            if (_Stop == 5)
            {
                _Timer.Stop();
                _Timer.Enabled = false;
                _Timer.Dispose();

                _Stop = 0;

                this.Height = _Height;
                this.Width = _Width;
            }
        }

        #endregion

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

        private async void Ver_Click(object sender, RoutedEventArgs e)
        {
            await UpdateCheck.CheckForUpdate("http://textuploader.com/58mva/raw");
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