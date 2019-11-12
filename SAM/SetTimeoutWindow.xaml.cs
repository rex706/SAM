using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace SAM
{
    /// <summary>
    /// Interaction logic for SetTimeoutWindow.xaml
    /// </summary>
    public partial class SetTimeoutWindow : MetroWindow
    {
        public DateTime? timeout;

        public SetTimeoutWindow(DateTime? timeout)
        {
            InitializeComponent();
            this.timeout = timeout;

            if (timeout != null && timeout < new DateTime())
            {
                var timeLeft = timeout - DateTime.Now;

                int years = timeLeft.Value.Days / 365;
                int days = timeLeft.Value.Days;

                if (years > 0)
                {
                    days = (timeLeft.Value.Days / (years * 365));
                }

                YearsSpinBox.Value = years;
                DaysSpinBox.Value = days;
                HoursSpinBox.Value = timeLeft.Value.Hours;
                MinutesSpinBox.Value = timeLeft.Value.Minutes;
                SecondsSpinBox.Value = timeLeft.Value.Seconds;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (YearsSpinBox.Value == 0 && DaysSpinBox.Value == 0 && HoursSpinBox.Value == 0 && MinutesSpinBox.Value == 0 && SecondsSpinBox.Value == 0)
            {
                Close();
            }

            timeout = DateTime.Now.AddYears((int)YearsSpinBox.Value).AddDays((int)DaysSpinBox.Value).AddHours((int)HoursSpinBox.Value).AddMinutes((int)MinutesSpinBox.Value).AddSeconds((int)SecondsSpinBox.Value);

            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            YearsSpinBox.Value = 0;
            DaysSpinBox.Value = 0;
            HoursSpinBox.Value = 0;
            MinutesSpinBox.Value = 0;
            SecondsSpinBox.Value = 0;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
