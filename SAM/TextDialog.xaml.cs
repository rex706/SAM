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
                if (InputTextBox2.Text != null)
                    return InputTextBox.Text + " " + InputTextBox1.Text + " " + InputTextBox2;
                else
                    return InputTextBox.Text + " " + InputTextBox1.Text;
            }
            set { InputTextBox.Text = value; }
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
