using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

namespace SAM
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ImportDelimited : Window
    {
        private string eKey;

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

            char delimiter = DelimiterCharacterTextBox.Text[0];
            string delimitedAccountsText = DelimitedAccountsTextBox.Text;
            string[] lines = delimitedAccountsText.Split('\n');

            List<Account> accounts = new List<Account>();

            foreach (string line in lines)
            {
                string[] info = line.Split(delimiter);

                if (info.Length < 2)
                {
                    MessageBox.Show("Invalid account format!");
                    return;
                }

                // Remove new lines and white space from info.
                string username = Regex.Replace(info[0], @"\s+", string.Empty);
                string password = Regex.Replace(info[1], @"\s+", string.Empty);

                // Shared secret.
                if (info.Length > 2 && info[2] != null && info[2] != string.Empty)
                {
                    string secret = Regex.Replace(info[2], @"\s+", string.Empty);
                    accounts.Add(new Account { Name = username, Password = StringCipher.Encrypt(password, eKey), SharedSecret = StringCipher.Encrypt(secret, eKey) });
                }
                else
                {
                    accounts.Add(new Account { Name = username, Password = StringCipher.Encrypt(password, eKey) });
                }
            }

            Utils.ImportAccountsFromList(accounts);

            Close();
        }

        private void DelimiterCharacterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (PreviewTextBlock != null && DelimiterCharacterTextBox.Text.Length > 0)
            {
                PreviewTextBlock.Text = "account" + DelimiterCharacterTextBox.Text + "password" + DelimiterCharacterTextBox.Text + "sharedSecret";
            }
        }
    }
}
