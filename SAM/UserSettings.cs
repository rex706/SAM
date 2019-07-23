using System.Collections.Generic;

namespace SAM
{
    class UserSettings
    {
        #region General 

        public bool ClearUserData { get { return (bool)KeyValuePairs["ClearUserData"]; } set { KeyValuePairs["ClearUserData"] = value; } }
        public bool HideAddButton { get { return (bool)KeyValuePairs["HideAddButton"]; } set { KeyValuePairs["HideAddButton"] = value; } }
        public bool PasswordProtect { get { return (bool)KeyValuePairs["PasswordProtect"]; } set { KeyValuePairs["PasswordProtect"] = value; } }
        public bool MinimizeToTray { get { return (bool)KeyValuePairs["MinimizeToTray"]; } set { KeyValuePairs["MinimizeToTray"] = value; } }
        public bool RememberPassword { get { return (bool)KeyValuePairs["RememberPassword"]; } set { KeyValuePairs["RememberPassword"] = value; } }
        public bool StartMinimized { get { return (bool)KeyValuePairs["StartMinimized"]; } set{ KeyValuePairs["StartMinimized"] = value; } }
        public bool StartWithWindows { get { return (bool)KeyValuePairs["StartWithWindows"]; } set { KeyValuePairs["StartWithWindows"] = value; } }
        public int AccountsPerRow { get { return (int)KeyValuePairs["AccountsPerRow"]; } set { KeyValuePairs["AccountsPerRow"] = value; } }
        public int SleepTime { get { return (int)KeyValuePairs["SleepTime"]; } set { KeyValuePairs["SleepTime"] = value; } }

        #endregion

        #region AutoLog
        
        public bool LoginRecentAccount { get { return (bool)KeyValuePairs["LoginRecentAccount"] ; } set { KeyValuePairs["LoginRecentAccount"] = value; } }
        public int RecentAccountIndex { get { return (int)KeyValuePairs["RecentAccountIndex"]; } set{ KeyValuePairs["RecentAccountIndex"] = value; } }
        public bool LoginSelectedAccount { get { return (bool)KeyValuePairs["LoginSelectedAccount"]; } set { KeyValuePairs["LoginSelectedAccount"] = value; } }
        public int SelectedAccountIndex { get { return (int)KeyValuePairs["SelectedAccountIndex"]; } set { KeyValuePairs["SelectedAccountIndex"] = value; } }

        #endregion

        #region Customize

        public int ButtonSize { get { return (int)KeyValuePairs["ButtonSize"]; } set { KeyValuePairs["ButtonSize"] = value; } }
        public string ButtonColor { get { return (string)KeyValuePairs["ButtonColor"]; } set { KeyValuePairs["ButtonColor"] = value; } }
        public int ButtonFontSize { get { return (int)KeyValuePairs["ButtonFontSize"]; } set { KeyValuePairs["ButtonFontSize"] = value; } }
        public string ButtonFontColor { get { return (string)KeyValuePairs["ButtonFontColor"]; } set { KeyValuePairs["ButtonFontColor"] = value; } }
        public string ButtonBannerColor { get { return (string)KeyValuePairs["ButtonBannerColor"]; } set { KeyValuePairs["ButtonBannerColor"] = value; } }
        public int BannerFontSize { get { return (int)KeyValuePairs["ButtonBannerFontSize"]; } set { KeyValuePairs["ButtonBannerFontSize"] = value; } }
        public string BannerFontColor { get { return (string)KeyValuePairs["ButtonBannerFontColor"]; } set { KeyValuePairs["ButtonBannerFontColor"] = value; } }

        #endregion

        #region Steam

        public string SteamPath { get { return (string)KeyValuePairs["Path"]; } set { KeyValuePairs["Path"] = value; } }

        #endregion

        #region Parameters

        public bool CafeAppLaunch { get { return (bool)KeyValuePairs["cafeapplaunch"]; } set { KeyValuePairs["cafeapplaunch"] = value; } }
        public bool ClearBeta { get { return (bool)KeyValuePairs["clearbeta"]; } set { KeyValuePairs["clearbeta"] = value; } }
        public bool Console { get { return (bool)KeyValuePairs["console"]; } set { KeyValuePairs["console"] = value; } }
        public bool Developer { get { return (bool)KeyValuePairs["developer"]; } set { KeyValuePairs["developer"] = value; } }
        public bool ForceService { get { return (bool)KeyValuePairs["forceservice"]; } set { KeyValuePairs["forceservice"] = value; } }
        public bool Login { get { return (bool)KeyValuePairs["login"]; } set { KeyValuePairs["login"] = value; } }
        public bool NoCache { get { return (bool)KeyValuePairs["nocache"]; } set { KeyValuePairs["nocache"] = value; } }
        public bool NoVerifyFiles { get { return (bool)KeyValuePairs["noverifyfiles"]; } set { KeyValuePairs["noverifyfiles"] = value; } }
        public bool Silent { get { return (bool)KeyValuePairs["silent"]; } set { KeyValuePairs["silent"] = value; } }
        public bool SingleCore { get { return (bool)KeyValuePairs["single_core"]; } set { KeyValuePairs["single_core"] = value; } }
        public bool TCP { get { return (bool)KeyValuePairs["tcp"]; } set { KeyValuePairs["tcp"] = value; } }
        public bool TenFoot { get { return (bool)KeyValuePairs["tenfoot"]; } set { KeyValuePairs["tenfoot"] = value; } }

        #endregion

        #region Location

        public double WindowTop { get; set; }
        public double WindowLeft { get; set; }

        #endregion

        public Dictionary<string, object> KeyValuePairs = new Dictionary<string, object>()
        {
            { "ClearUserData", false },
            { "HideAddButton", false },
            { "PasswordProtect", false },
            { "MinimizeToTray", false },
            { "RememberPassword", false },
            { "StartMinimized", false },
            { "StartWithWindows", false },
            { "AccountsPerRow", 5 },
            { "SleepTime", 2 },

            { "LoginRecentAccount", false },
            { "RecentAccountIndex", -1 },
            { "LoginSelectedAccount", false },
            { "SelectedAccountIndex", -1 },

            { "Path", string.Empty },

            { "ButtonSize", 100 },
            { "ButtonColor", "#FFDDDDDD" },
            { "ButtonFontSize", 0 },
            { "ButtonFontColor", "#FF000000" },
            { "ButtonBannerColor", "#7F000000" },
            { "ButtonBannerFontSize", 0 },
            { "ButtonBannerFontColor", "#FFFFFF" },

            { "cafeapplaunch", false },
            { "clearbeta", false },
            { "console", false },
            { "developer", false },
            { "forceservice", false },
            { "login", true },
            { "nocache", false },
            { "noverifyfiles", false },
            { "silent", false },
            { "single_core", false },
            { "tcp", false },
            { "tenfoot", false }
        };
    }
}
