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
        private static readonly string newUpdaterFileName = "Updater_new.exe";

        private static readonly string gitHubUrlPrefix = "https://raw.githubusercontent.com/rex706/Updater/master/";

        /// <summary>
        /// Check program for updates with the given text url.
        /// Returns 1 if the user chose not to update or 0 if there is no update available.
        /// </summary>
        public static async Task<int> CheckForUpdate(string updateUrl, string repoUrl)
        {
            // Nkosi Note: Always use asynchronous versions of network and IO methods.

            // Check for version updates
            var client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 1, 0);

            await UpdaterUpdateCheck(client);

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
                            if (File.Exists(updaterFileName))
                            {
                                // Setup update process information.
                                ProcessStartInfo startInfo = new ProcessStartInfo();
                                startInfo.CreateNoWindow = false;
                                startInfo.UseShellExecute = true;
                                startInfo.FileName = updaterFileName;
                                startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                                startInfo.Arguments = updateUrl;

                                // Launch updater and exit.
                                Process.Start(startInfo);
                            }
                            else
                            {
                                Process.Start(repoUrl);
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

        private static async Task UpdaterUpdateCheck(HttpClient client)
        {
            Version latest;

            try
            {
                using (Stream stream = await client.GetStreamAsync(gitHubUrlPrefix + "version.txt"))
                {
                    StreamReader reader = new StreamReader(stream);
                    string latestVersionString = await reader.ReadLineAsync();
                    latest = new Version(latestVersionString);
                }

                using (Stream stream = await client.GetStreamAsync(gitHubUrlPrefix + "latest.txt"))
                {
                    StreamReader reader = new StreamReader(stream);

                    string latestUpdaterUrl = await reader.ReadLineAsync();

                    if (!File.Exists(updaterFileName))
                    {
                        await UpdateUpdater(latestUpdaterUrl);
                    }
                    else
                    {
                        Version current = new Version(FileVersionInfo.GetVersionInfo(updaterFileName).FileVersion);

                        if (latest > current)
                        {
                            await UpdateUpdater(latestUpdaterUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        private static async Task UpdateUpdater(string url)
        {
            // Start downloading the file.
            await new WebClient().DownloadFileTaskAsync(url, AppDomain.CurrentDomain.BaseDirectory + newUpdaterFileName);

            // If a new version of the updater was downloaded, replace the old one.
            if (File.Exists(newUpdaterFileName))
            {
                File.Delete(updaterFileName);
                File.Move(newUpdaterFileName, updaterFileName);
            }
        }
    }
}
