using System.Windows;

namespace SAM
{
    /// <summary>
    /// Interaction logic for TextDialog.xaml
    /// </summary>
    public partial class TextDialog : Window
    {
        public TextDialog()
        {
            InitializeComponent();
        }

        public string ResponseText
        {
            get
            {
                if (UrlBox.Text != "")
                {
                    if(!UrlBox.Text.Contains("http://steamcommunity.com/"))
                    {
                        MessageBox.Show("Invalid Url!\nMake sure your steam url starts with:\nhttp://steamcommunity.com/", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Close();
                    }
                    return UsernameBox.Text + " " + PasswordBox.Password + " " + UrlBox.Text;
                } 
                else
                    return UsernameBox.Text + " " + PasswordBox.Password;
            }
            set { UsernameBox.Text = value; }
        } 

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
