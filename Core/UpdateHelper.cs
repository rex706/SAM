using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SAM.Core
{
    class UpdateHelper
    {
        /// <summary>
        /// String containing the latest version acquired from online text file.
        /// </summary>
        public static string latestVersion { get; set; }

        private static readonly string updaterFileName = "Updater.exe";
        private static readonly string latestUpdaterVersionUrl = "https://raw.githubusercontent.com/rex706/Updater/master/latest.txt";

        /// <summary>
        /// Check program for updates with the given text url.
        /// </summary>
        public static async Task<UpdateResponse> CheckForUpdate(string updateUrl)
        {
            // Allows downloading files directly from GitHub repositories. 
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Nkosi Note: Always use asynchronous versions of network and IO methods.

            await Task.Run(() => DeleteUpdater());

            // Check for version updates
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(0, 0, 1, 0);

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
                                return UpdateResponse.Update;
                            }

                            // Update is available, but user chose not to update just yet.
                            return UpdateResponse.Later;
                        }
                    }
                }

                // No update available.
                return UpdateResponse.NoUpdate;
            }
            catch
            {
                // Some error occured or there is no internet connection.
                return UpdateResponse.Error;
            }
        }

        public static async Task StartUpdate(string updateUrl, string releasesUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 1, 0);

                await Task.Run(() => DeleteUpdater());

                // Download latest updater.
                using (Stream updaterStream = await client.GetStreamAsync(latestUpdaterVersionUrl))
                {
                    StreamReader reader = new StreamReader(updaterStream);
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
            }
        }

        private static async Task DownloadUpdater(string url)
        {
            await new WebClient().DownloadFileTaskAsync(url, AppDomain.CurrentDomain.BaseDirectory + updaterFileName);
        }

        private static void DeleteUpdater()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + updaterFileName;

            if (File.Exists(path))
            {
                while (IsFileLocked(path))
                {
                    Console.WriteLine("Waiting for updater to close...");
                    Thread.Sleep(1000);
                }

                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
}
