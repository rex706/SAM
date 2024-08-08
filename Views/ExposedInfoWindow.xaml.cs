using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using SAM.Core;

namespace SAM.Views
{
    /// <summary>
    /// Interaction logic for ExposedInfoWindow.xaml
    /// </summary>
    public partial class ExposedInfoWindow : MetroWindow
    {
        private readonly List<Account> decryptedAccounts;
        private readonly string eKey;

        public ExposedInfoWindow(List<Account> decryptedAccounts, string eKey)
        {
            InitializeComponent();
            this.decryptedAccounts = decryptedAccounts;
            this.eKey = eKey;
            RefreshAccountsList();
        }

        private void RefreshAccountsList()
        {
            StringBuilder accountListBuilder = new StringBuilder();

            foreach (Account account in decryptedAccounts)
            {
                string password = StringCipher.Decrypt(account.Password, eKey);
                string sharedSecret = StringCipher.Decrypt(account.SharedSecret, eKey);

                string line = account.Name + DelimiterCharacterTextBox.Text + password;

                if (!string.IsNullOrEmpty(sharedSecret))
                {
                    line += DelimiterCharacterTextBox.Text +  sharedSecret;
                }

                accountListBuilder.AppendLine(line);
            }

            DelimitedAccountsTextBox.Text = accountListBuilder.ToString();
        }

        private void DelimiterCharacterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PreviewTextBlock != null && DelimiterCharacterTextBox.Text.Length > 0)
            {
                PreviewTextBlock.Content = "account" + DelimiterCharacterTextBox.Text + "password" + DelimiterCharacterTextBox.Text + "sharedSecret";
                RefreshAccountsList();
            }
        }
    }
}
