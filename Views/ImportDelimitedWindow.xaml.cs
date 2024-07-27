using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using SAM.Core;
using Microsoft.Win32;
using System.IO;

namespace SAM.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ImportDelimited : MetroWindow
    {
        private readonly string eKey;

        public ImportDelimited(string eKey)
        {
            this.eKey = eKey;
            InitializeComponent();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (DelimitedAccountsTextBox.Text.Length == 0)
            {
                DelimitedAccountsTextBox.Focus();
                MessageBox.Show("No accounts to import!");
                return;
            }

            if (DelimiterCharacterTextBox.Text.Length == 0)
            {
                DelimiterCharacterTextBox.Focus();
                MessageBox.Show("Delimiter character required!");
                return;
            }

            ImportButton.IsEnabled = false;

            char delimiter = DelimiterCharacterTextBox.Text[0];
            string delimitedAccountsText = DelimitedAccountsTextBox.Text;

            string[] lines = delimitedAccountsText.Split('\n');

            List<Account> accounts = new List<Account>();

            int sucessful = 0;
            List<string> errors = new List<string>();

            foreach (string line in lines)
            {
                string current = RemoveWhitespace(line);

                // Skip empty lines.
                if (current.Length == 0)
                {
                    continue;
                }

                string[] info = current.Split(delimiter);

                // Log account error.
                if (info.Length < 2)
                {
                    errors.Add(line);
                    continue;
                }

                string name = info[0];
                string password = info[1];

                string steamId = AccountUtils.GetSteamIdFromConfig(name);

                // Shared secret.
                if (info.Length > 2 && info[2] != null && info[2] != string.Empty)
                {
                    string secret = info[2];
                    accounts.Add(new Account { Name = name, Password = StringCipher.Encrypt(password, eKey), SharedSecret = StringCipher.Encrypt(secret, eKey), SteamId = steamId });
                }
                else
                {
                    accounts.Add(new Account { Name = name, Password = StringCipher.Encrypt(password, eKey), SteamId = steamId });
                }

                sucessful++;
            }

            AccountUtils.ImportAccountsFromList(accounts);

            if (errors.Count > 0)
            {
                MessageBox.Show("There were " + errors.Count + " problems with import:\n" + String.Join("\n", errors.ToArray()));
            }

            Close();
        }

        private void DelimiterCharacterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (PreviewTextBlock != null && DelimiterCharacterTextBox.Text.Length > 0)
            {
                PreviewTextBlock.Content = "account" + DelimiterCharacterTextBox.Text + "password" + DelimiterCharacterTextBox.Text + "sharedSecret";
            }
        }

        private void ReadTextFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Text Files (*.txt)|*.txt",
                Multiselect = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                SetTextFromFiles(dialog.FileNames);
            }
        }

        private void DelimitedAccountsTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void DelimitedAccountsTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            SetTextFromFiles(files);
            e.Handled = true;
        }

        private void SetTextFromFiles(string[] files)
        {
            DelimitedAccountsTextBox.Text = "";

            foreach (string file in files)
            {
                try
                {
                    string ext = Path.GetExtension(file);

                    if (ext == ".txt")
                    {
                        DelimitedAccountsTextBox.Text += File.ReadAllText(file);
                    }
                    else
                    {
                        string name = Path.GetFileName(file);
                        MessageBox.Show(name + " is not a .txt file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } 
        }

        private string RemoveWhitespace(string str)
        {
            return string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
