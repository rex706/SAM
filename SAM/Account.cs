using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SAM
{
    public class Account : INotifyPropertyChanged
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Password { get; set; }

        public string SharedSecret { get; set; }

        public string ProfUrl { get; set; }

        public string AviUrl { get; set; }

        public string SteamId { get; set; }

        public DateTime? Timeout { get; set; }

        private string timeoutTimeLeft;
        public string TimeoutTimeLeft { get { return timeoutTimeLeft; } set { timeoutTimeLeft = value; OnPropertyChanged(); } }

        public string Description { get; set; }

        public bool CommunityBanned { get; set; }

        public bool VACBanned { get; set; }

        public int NumberOfVACBans { get; set; }
        public int DaysSinceLastBan { get; set; }

        public int NumberOfGameBans { get; set; }

        public string EconomyBan { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
