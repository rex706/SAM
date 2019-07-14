using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM
{
    public class Account
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Password { get; set; }

        public string SharedSecret { get; set; }

        public string ProfUrl { get; set; }

        public string AviUrl { get; set; }

        public string SteamId { get; set; }

        public DateTime Timeout { get; set; }

        public string Description { get; set; }
    }
}
