using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Input;

namespace SAM
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class PasswordWindow : MetroWindow
    {
        public string PasswordText
        {
            get { return PasswordTextBox.Password; }
            set { PasswordTextBox.Password = value; }
        }

        public PasswordWindow()
        {
            InitializeComponent();
            PasswordTextBox.Focus();
        }

        private void PasswordTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (validateInput())
                {
                    DialogResult = true;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (validateInput())
            {
                DialogResult = true;
            }
        }

        private bool validateInput()
        {
            if (PasswordTextBox.Password.Length > 0)
            {
                PasswordText = PasswordTextBox.Password;
                return true;
            }
            else if (PasswordTextBox.Password.Length == 0)
            {
                //MessageBoxResult messageBoxResult = MessageBox.Show("No password detected, are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                //if (messageBoxResult == MessageBoxResult.OK)
                //{
                //    return true;
                //}

                PasswordText = "";

                return true;
            }

            return false;
        }
    }
}
