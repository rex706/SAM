using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private static Hashtable hashAddresses;
        private static Hashtable DecryptedAddresses;

        private static string EKey = "PRIVATE_KEY"; // Change this before use

        private static string account;
        private static string ePassword;

        private static Version latest;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //verion number from assembly
            string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            int idx = AssemblyVersion.LastIndexOf('0') - 1;
            AssemblyVersion = AssemblyVersion.Substring(0, idx);

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
                //TODO: Exit the application. Later, initiate a process that downloads new updated binaries.
                Close();
            }

            RefreshWindow();
        }

        private void RefreshWindow()
        {
            DecryptedAddresses = new Hashtable();

            //check if info.dat exists
            if (File.Exists("info.dat"))
            {
                //deserialize file and count the number of entries
                Deserialize();
                postDeserializedRefresh(true);
                
            }
            else
            {
                hashAddresses = new Hashtable();
            }
        }

        private void postDeserializedRefresh(bool seedDic)
        {
           
            if (hashAddresses != null)
            {
                //adjust window size and info positions
                refreshSize();

                int counter = 0;

                foreach (DictionaryEntry de in hashAddresses)
                {
                    string tempname = de.Key.ToString();
                    string temppass = StringCipher.Decrypt(de.Value.ToString(), EKey);
                    if (seedDic)
                    {
                        DecryptedAddresses.Add(tempname, temppass);
                    }
                    Console.WriteLine("Key = {0}, Value = {1}", de.Key.ToString(), de.Value.ToString());
                    Console.WriteLine("Key = {0}, Value = {1}", tempname, temppass);

                    Button accountButton = new Button();
                    accountButton.Tag = tempname;
                    accountButton.Content = tempname;
                    accountButton.Name = "Button" + counter.ToString();
                    accountButton.Height = 100;
                    accountButton.Width = 100;
                    accountButton.HorizontalAlignment = HorizontalAlignment.Left;
                    accountButton.VerticalAlignment = VerticalAlignment.Top;
                    accountButton.Margin = new Thickness(15 + (counter * 138), 30, 0, 0);
                    MainGrid.Children.Add(accountButton);
                    accountButton.Click += new RoutedEventHandler(AccountButton_Click);
                    ContextMenu accountContext = new ContextMenu();
                    MenuItem menuItem1 = new MenuItem();
                    menuItem1.Header = "Delete Account";
                    accountContext.Items.Add(menuItem1);
                    accountButton.ContextMenu = accountContext;
                    menuItem1.Click += delegate { deleteAccount(accountButton); };
                    // accountButton.MouseRightButtonDown += accountButton_mouseRightButtonDown;


                    counter++;
                }
            }
        }

        private void refreshSize() {
            Application.Current.MainWindow.Width = ((hashAddresses.Count + 1) * 138);
            NewButton.Margin = new Thickness(15 + (hashAddresses.Count * 138), 44, 0, 0);

        }



        private void deleteAccount(object butt)
        {
            Button button = butt as Button;
            MainGrid.Children.RemoveRange(1, MainGrid.Children.Count - 1);
            hashAddresses.Remove(button.Tag);
            Serialize();
            
            postDeserializedRefresh(false);
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            //get avatar?

            // User entered info
            var dialog = new TextDialog();
            if (dialog.ShowDialog() == true)
            {
                string[] input = dialog.ResponseText.Split(' ');
                account = input[0];
                string password = input[1];

                // Encrypt info before saving to file
                // eAccount = StringCipher.Encrypt(accountName, EKey);
                ePassword = StringCipher.Encrypt(password, EKey);
                hashAddresses.Add(account, ePassword);
                Serialize();

                RefreshWindow();
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
                    Thread.Sleep(2000);
                }
                catch
                {

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
                }

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = true;
                startInfo.FileName = path + "Steam.exe";
                startInfo.WorkingDirectory = path;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "-login " + btn.Tag.ToString() + " " + DecryptedAddresses[btn.Tag.ToString()].ToString();

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
                //open the text file using a stream reader
                using (Stream stream = await client.GetStreamAsync("http://textuploader.com/58mva/raw"))
                {
                    Version current = Assembly.GetExecutingAssembly().GetName().Version;
                    StreamReader reader = new StreamReader(stream);
                    latest = Version.Parse(reader.ReadToEnd());

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

        private static void Serialize()
        {
            // Update hashtable of values that will eventually be serialized.
            

            // To serialize the hashtable and its key/value pairs, you must first open a stream for writing. 
            // In this case, use a file stream.
            FileStream fs = new FileStream("info.dat", FileMode.Create);

            // Construct a BinaryFormatter and use it to serialize the data to the stream.
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, hashAddresses);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        private static void Deserialize()
        {
            // Declare the hashtable reference.
            hashAddresses = null;

            // Open the file containing the data that you want to deserialize.
            FileStream fs = new FileStream("info.dat", FileMode.Open);
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();

                // Deserialize the hashtable from the file and assign the reference to the local variable.
                hashAddresses = (Hashtable)formatter.Deserialize(fs);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }

            // To prove that the table deserialized correctly, display the key/value pairs.
            foreach (DictionaryEntry de in hashAddresses)
            {
                Console.WriteLine("{0} : {1}.", de.Key, de.Value);
            }
        }

        #endregion
    }
}
