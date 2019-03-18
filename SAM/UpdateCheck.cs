using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SAM
{
    class UpdateCheck
    {
        /// <summary>
        /// String containing the latest version acquired from online text file.
        /// </summary>
        public static string latestVersion { get; set; }

        /// <summary>
        /// Check program for updates with the given text url.
        /// Returns 1 if the user chose not to update or 0 if there is no update available.
        /// </summary>
        public static async Task<int> CheckForUpdate(string url)
        {
            // Nkosi Note: Always use asynchronous versions of network and IO methods.

            // Check for version updates
            var client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 0, 10);

            // Check for new Updater past version 1.0.0.0.
            // Old updater was hard coded to serve only one specific url and cannot be aquired automatically through itself like the new one.
            if (!File.Exists("Updater.exe") || FileVersionInfo.GetVersionInfo("Updater.exe").FileVersion == "1.0.0.0")
            {
                // Show message box that an update is available.
                MessageBoxResult answer = MessageBox.Show("A new version of the Updater is available!\n\n" +
                    "This will be required for future SAM auto-updates\n" +
                    "as the old updater used hard coded URLs,\n" + 
                    "which obviously isn't ideal.\n" +
                    "\n\nDownload now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                // Update is available, and user wants to update.
                if (answer == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Open the text file using a stream reader.
                        using (Stream stream = await client.GetStreamAsync("https://raw.githubusercontent.com/rex706/Updater/master/latest.txt"))
                        {
                            StreamReader reader = new StreamReader(stream);

                            string latestUpdaterUrl = await reader.ReadLineAsync();

                            // Start downloading the file.
                            await new WebClient().DownloadFileTaskAsync(latestUpdaterUrl, AppDomain.CurrentDomain.BaseDirectory + "Updater_new.exe");
                            MessageBox.Show("Done!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }

            // If a new version of the updater was downloaded, replace the old one.
            if (File.Exists("Updater_new.exe"))
            {
                File.Delete("Updater.exe");
                File.Move("Updater_new.exe", "Updater.exe");
            }

            try
            {
                // Open the text file using a stream reader.
                using (Stream stream = await client.GetStreamAsync(url))
                {
                    StreamReader reader = new StreamReader(stream);

                    // Get current and latest versions of program.
                    Version current = Assembly.GetExecutingAssembly().GetName().Version;
                    Version latest = Version.Parse(await reader.ReadLineAsync());

                    // Update latest version string class member.
                    latestVersion = latest.ToString();

                    // If the version from the online text is newer than the current version,
                    // ask user if they would like to download and install update now.
                    if (latest > current)
                    {
                        // Show message box that an update is available.
                        MessageBoxResult answer = MessageBox.Show("A new version of " +
                            AppDomain.CurrentDomain.FriendlyName.Substring(0, AppDomain.CurrentDomain.FriendlyName.IndexOf('.')) +
                            " is available!\n\nCurrent Version     " + current + "\nLatest Version        " + latest +
                            "\n\nUpdate now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        // Update is available, and user wants to update. Requires app to close.
                        if (answer == MessageBoxResult.Yes)
                        {
                            // Setup update process information.
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.CreateNoWindow = false;
                            startInfo.UseShellExecute = true;
                            startInfo.FileName = "Updater.exe";
                            startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            startInfo.WindowStyle = ProcessWindowStyle.Normal;
                            startInfo.Arguments = url;

                            // Launch updater and exit.
                            Process.Start(startInfo);
                            Environment.Exit(0);

                            return 2;
                        }

                        // Update is available, but user chose not to update just yet.
                        return 1;
                    }
                }

                // No update available.
                return 0;
            }
            catch
            {
                // Some error occured or there is no internet connection.
                return -1;
            }
        }
    }
}
