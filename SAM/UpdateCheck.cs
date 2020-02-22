using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
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

        private static readonly string updaterFileName = "Updater.exe";
        private static readonly string latestUpdaterVersionUrl = "https://raw.githubusercontent.com/rex706/Updater/master/latest.txt";

        /// <summary>
        /// Check program for updates with the given text url.
        /// Returns 1 if the user chose not to update or 0 if there is no update available.
        /// </summary>
        public static async Task<int> CheckForUpdate(string updateUrl, string releasesUrl)
        {
            // Allows downloading files directly from GitHub repositories. 
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Nkosi Note: Always use asynchronous versions of network and IO methods.

            // Check for version updates
            var client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 1, 0);

            try
            {
                // Open the text file using a stream reader.
                using (Stream stream = await client.GetStreamAsync(updateUrl))
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
                            // Delete updater if exists.
                            if (File.Exists(updaterFileName))
                            {
                                File.Delete(updaterFileName);
                            }

                            // Download latest updater.
                            using (Stream updaterStream = await client.GetStreamAsync(latestUpdaterVersionUrl))
                            {
                                reader = new StreamReader(updaterStream);
                                string latestUpdaterUrl = await reader.ReadLineAsync();
                                await DownloadUpdater(latestUpdaterUrl);
                            }

                            // Setup update process information.
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.UseShellExecute = true;
                            startInfo.FileName = updaterFileName;
                            startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            startInfo.Arguments = updateUrl;
                            startInfo.Verb = "runas";

                            // Launch updater and exit.
                            try
                            {
                                Process.Start(startInfo);
                            }
                            catch
                            {
                                // Open browser to releases page.
                                Process.Start(releasesUrl);
                            }
                            
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

        private static async Task DownloadUpdater(string url)
        {
            await new WebClient().DownloadFileTaskAsync(url, AppDomain.CurrentDomain.BaseDirectory + updaterFileName);
        }
    }
}
