using System.Collections.Generic;

namespace SAM
{
    class UserSettings
    {
        #region General 

        public bool ClearUserData { get { return (bool)KeyValuePairs[SAMSettings.CLEAR_USER_DATA]; } set { KeyValuePairs[SAMSettings.CLEAR_USER_DATA] = value; } }
        public bool HideAddButton { get { return (bool)KeyValuePairs[SAMSettings.HIDE_ADD_BUTTON]; } set { KeyValuePairs[SAMSettings.HIDE_ADD_BUTTON] = value; } }
        public bool PasswordProtect { get { return (bool)KeyValuePairs[SAMSettings.PASSWORD_PROTECT]; } set { KeyValuePairs[SAMSettings.PASSWORD_PROTECT] = value; } }
        public bool MinimizeToTray { get { return (bool)KeyValuePairs[SAMSettings.MINIMIZE_TO_TRAY]; } set { KeyValuePairs[SAMSettings.MINIMIZE_TO_TRAY] = value; } }
        public bool RememberPassword { get { return (bool)KeyValuePairs[SAMSettings.REMEMBER_PASSWORD]; } set { KeyValuePairs[SAMSettings.REMEMBER_PASSWORD] = value; } }
        public bool StartMinimized { get { return (bool)KeyValuePairs[SAMSettings.START_MINIMIZED]; } set{ KeyValuePairs[SAMSettings.START_MINIMIZED] = value; } }
        public bool StartWithWindows { get { return (bool)KeyValuePairs[SAMSettings.START_WITH_WINDOWS]; } set { KeyValuePairs[SAMSettings.START_WITH_WINDOWS] = value; } }
        public int AccountsPerRow { get { return (int)KeyValuePairs[SAMSettings.ACCOUNTS_PER_ROW]; } set { KeyValuePairs[SAMSettings.ACCOUNTS_PER_ROW] = value; } }
        public int SleepTime { get { return (int)KeyValuePairs[SAMSettings.SLEEP_TIME]; } set { KeyValuePairs[SAMSettings.SLEEP_TIME] = value; } }
        public bool CheckForUpdates { get { return (bool)KeyValuePairs[SAMSettings.CHECK_FOR_UPDATES]; } set { KeyValuePairs[SAMSettings.CHECK_FOR_UPDATES] = value;  } }

        #endregion

        #region AutoLog
        
        public bool LoginRecentAccount { get { return (bool)KeyValuePairs[SAMSettings.LOGIN_RECENT_ACCOUNT] ; } set { KeyValuePairs[SAMSettings.LOGIN_RECENT_ACCOUNT] = value; } }
        public int RecentAccountIndex { get { return (int)KeyValuePairs[SAMSettings.RECENT_ACCOUNT_INDEX]; } set{ KeyValuePairs[SAMSettings.RECENT_ACCOUNT_INDEX] = value; } }
        public bool LoginSelectedAccount { get { return (bool)KeyValuePairs[SAMSettings.LOGIN_SELECTED_ACCOUNT]; } set { KeyValuePairs[SAMSettings.LOGIN_SELECTED_ACCOUNT] = value; } }
        public int SelectedAccountIndex { get { return (int)KeyValuePairs[SAMSettings.SELECTED_ACCOUNT_INDEX]; } set { KeyValuePairs[SAMSettings.SELECTED_ACCOUNT_INDEX] = value; } }

        #endregion

        #region Customize

        public int ButtonSize { get { return (int)KeyValuePairs[SAMSettings.BUTTON_SIZE]; } set { KeyValuePairs[SAMSettings.BUTTON_SIZE] = value; } }
        public string ButtonColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_COLOR] = value; } }
        public int ButtonFontSize { get { return (int)KeyValuePairs[SAMSettings.BUTTON_FONT_SIZE]; } set { KeyValuePairs[SAMSettings.BUTTON_FONT_SIZE] = value; } }
        public string ButtonFontColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_FONT_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_FONT_COLOR] = value; } }
        public string ButtonBannerColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_BANNER_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_BANNER_COLOR] = value; } }
        public int BannerFontSize { get { return (int)KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_SIZE]; } set { KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_SIZE] = value; } }
        public string BannerFontColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_COLOR] = value; } }

        #endregion

        #region Steam

        public string SteamPath { get { return (string)KeyValuePairs[SAMSettings.STEAM_PATH]; } set { KeyValuePairs[SAMSettings.STEAM_PATH] = value; } }
        public string ApiKey { get { return (string)KeyValuePairs[SAMSettings.STEAM_API_KEY]; } set { KeyValuePairs[SAMSettings.STEAM_API_KEY] = value; } }

        #endregion

        #region Parameters

        public bool CafeAppLaunch { get { return (bool)KeyValuePairs[SAMSettings.CAFE_APP_LAUNCH_PARAMETER]; } set { KeyValuePairs[SAMSettings.CAFE_APP_LAUNCH_PARAMETER] = value; } }
        public bool ClearBeta { get { return (bool)KeyValuePairs[SAMSettings.CLEAR_BETA_PARAMETER]; } set { KeyValuePairs[SAMSettings.CLEAR_BETA_PARAMETER] = value; } }
        public bool Console { get { return (bool)KeyValuePairs[SAMSettings.CONSOLE_PARAMETER]; } set { KeyValuePairs[SAMSettings.CONSOLE_PARAMETER] = value; } }
        public bool Developer { get { return (bool)KeyValuePairs[SAMSettings.DEVELOPER_PARAMETER]; } set { KeyValuePairs[SAMSettings.DEVELOPER_PARAMETER] = value; } }
        public bool ForceService { get { return (bool)KeyValuePairs[SAMSettings.FORCE_SERVICE_PARAMETER]; } set { KeyValuePairs[SAMSettings.FORCE_SERVICE_PARAMETER] = value; } }
        public bool Login { get { return (bool)KeyValuePairs[SAMSettings.LOGIN_PARAMETER]; } set { KeyValuePairs[SAMSettings.LOGIN_PARAMETER] = value; } }
        public bool NoCache { get { return (bool)KeyValuePairs[SAMSettings.NO_CACHE_PARAMETER]; } set { KeyValuePairs[SAMSettings.NO_CACHE_PARAMETER] = value; } }
        public bool NoVerifyFiles { get { return (bool)KeyValuePairs[SAMSettings.NO_VERIFY_FILES_PARAMETER]; } set { KeyValuePairs[SAMSettings.NO_VERIFY_FILES_PARAMETER] = value; } }
        public bool Silent { get { return (bool)KeyValuePairs[SAMSettings.SILENT_PARAMETER]; } set { KeyValuePairs[SAMSettings.SILENT_PARAMETER] = value; } }
        public bool SingleCore { get { return (bool)KeyValuePairs[SAMSettings.SINGLE_CORE_PARAMETER]; } set { KeyValuePairs[SAMSettings.SINGLE_CORE_PARAMETER] = value; } }
        public bool TCP { get { return (bool)KeyValuePairs[SAMSettings.TCP_PARAMETER]; } set { KeyValuePairs[SAMSettings.TCP_PARAMETER] = value; } }
        public bool TenFoot { get { return (bool)KeyValuePairs[SAMSettings.TEN_FOOT_PARAMETER]; } set { KeyValuePairs[SAMSettings.TEN_FOOT_PARAMETER] = value; } }
        public bool CustomParameters { get { return (bool)KeyValuePairs[SAMSettings.CUSTOM_PARAMETERS]; } set { KeyValuePairs[SAMSettings.CUSTOM_PARAMETERS] = value; } }
        public string CustomParametersValue { get { return (string)KeyValuePairs[SAMSettings.CUSTOM_PARAMETERS_VALUE]; } set { KeyValuePairs[SAMSettings.CUSTOM_PARAMETERS_VALUE] = value; } }

        #endregion

        #region Location

        public double WindowTop { get; set; }
        public double WindowLeft { get; set; }

        #endregion

        public Dictionary<string, object> KeyValuePairs = new Dictionary<string, object>()
        {
            { SAMSettings.CLEAR_USER_DATA, false },
            { SAMSettings.HIDE_ADD_BUTTON, false },
            { SAMSettings.PASSWORD_PROTECT, false },
            { SAMSettings.MINIMIZE_TO_TRAY, false },
            { SAMSettings.REMEMBER_PASSWORD, false },
            { SAMSettings.START_MINIMIZED, false },
            { SAMSettings.START_WITH_WINDOWS, false },
            { SAMSettings.ACCOUNTS_PER_ROW, 5 },
            { SAMSettings.SLEEP_TIME, 2 },
            { SAMSettings.CHECK_FOR_UPDATES, true },

            { SAMSettings.LOGIN_RECENT_ACCOUNT, false },
            { SAMSettings.RECENT_ACCOUNT_INDEX, -1 },
            { SAMSettings.LOGIN_SELECTED_ACCOUNT, false },
            { SAMSettings.SELECTED_ACCOUNT_INDEX, -1 },

            { SAMSettings.STEAM_PATH, string.Empty },
            { SAMSettings.STEAM_API_KEY, string.Empty },

            { SAMSettings.BUTTON_SIZE, 100 },
            { SAMSettings.BUTTON_COLOR, "#FFDDDDDD" },
            { SAMSettings.BUTTON_FONT_SIZE, 0 },
            { SAMSettings.BUTTON_FONT_COLOR, "#FF000000" },
            { SAMSettings.BUTTON_BANNER_COLOR, "#7F000000" },
            { SAMSettings.BUTTON_BANNER_FONT_SIZE, 0 },
            { SAMSettings.BUTTON_BANNER_FONT_COLOR, "#FFFFFF" },

            { SAMSettings.CAFE_APP_LAUNCH_PARAMETER, false },
            { SAMSettings.CLEAR_BETA_PARAMETER, false },
            { SAMSettings.CONSOLE_PARAMETER, false },
            { SAMSettings.DEVELOPER_PARAMETER, false },
            { SAMSettings.FORCE_SERVICE_PARAMETER, false },
            { SAMSettings.LOGIN_PARAMETER, true },
            { SAMSettings.NO_CACHE_PARAMETER, false },
            { SAMSettings.NO_VERIFY_FILES_PARAMETER, false },
            { SAMSettings.SILENT_PARAMETER, false },
            { SAMSettings.SINGLE_CORE_PARAMETER, false },
            { SAMSettings.TCP_PARAMETER, false },
            { SAMSettings.TEN_FOOT_PARAMETER, false },
            { SAMSettings.CUSTOM_PARAMETERS, false },
            { SAMSettings.CUSTOM_PARAMETERS_VALUE, string.Empty },
        };
    }
}
