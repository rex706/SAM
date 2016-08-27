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
                if (UrlBox.Text != null)
                    return UsernameBox.Text + " " + PasswordBox.Password + " " + UrlBox.Text;
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
