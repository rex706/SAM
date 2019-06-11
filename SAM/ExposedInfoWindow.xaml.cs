using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SAM
{
    /// <summary>
    /// Interaction logic for ExposedInfoWindow.xaml
    /// </summary>
    public partial class ExposedInfoWindow : Window
    {
        private List<Account> decryptedAccounts;

        public ExposedInfoWindow(List<Account> decryptedAccounts)
        {
            InitializeComponent();
            this.decryptedAccounts = decryptedAccounts;
            RefreshAccountsList();
        }

        private void RefreshAccountsList()
        {
            StringBuilder accountListBuilder = new StringBuilder();

            foreach (Account account in decryptedAccounts)
            {
                accountListBuilder.AppendLine(account.Name + DelimiterCharacterTextBox.Text + account.Password);
            }

            DelimitedAccountsTextBox.Text = accountListBuilder.ToString();
        }

        private void DelimiterCharacterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PreviewTextBlock != null && DelimiterCharacterTextBox.Text.Length > 0)
            {
                PreviewTextBlock.Text = "account" + DelimiterCharacterTextBox.Text + "password";
                RefreshAccountsList();
            }
        }
    }
}
