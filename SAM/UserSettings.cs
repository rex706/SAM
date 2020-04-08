using System;
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
        public bool CloseOnLogin { get { return (bool)KeyValuePairs[SAMSettings.CLOSE_ON_LOGIN]; } set { KeyValuePairs[SAMSettings.CHECK_FOR_UPDATES] = value; } }
        public bool ListView { get { return (bool)KeyValuePairs[SAMSettings.LIST_VIEW]; } set { KeyValuePairs[SAMSettings.LIST_VIEW] = value; } }
        public bool SandboxMode { get { return (bool)KeyValuePairs[SAMSettings.SANDBOX_MODE]; } set { KeyValuePairs[SAMSettings.SANDBOX_MODE] = value; } }

        #endregion

        #region AutoLog
        
        public bool LoginRecentAccount { get { return (bool)KeyValuePairs[SAMSettings.LOGIN_RECENT_ACCOUNT] ; } set { KeyValuePairs[SAMSettings.LOGIN_RECENT_ACCOUNT] = value; } }
        public int RecentAccountIndex { get { return (int)KeyValuePairs[SAMSettings.RECENT_ACCOUNT_INDEX]; } set{ KeyValuePairs[SAMSettings.RECENT_ACCOUNT_INDEX] = value; } }
        public bool LoginSelectedAccount { get { return (bool)KeyValuePairs[SAMSettings.LOGIN_SELECTED_ACCOUNT]; } set { KeyValuePairs[SAMSettings.LOGIN_SELECTED_ACCOUNT] = value; } }
        public int SelectedAccountIndex { get { return (int)KeyValuePairs[SAMSettings.SELECTED_ACCOUNT_INDEX]; } set { KeyValuePairs[SAMSettings.SELECTED_ACCOUNT_INDEX] = value; } }
        public VirtualInputMethod VirtualInputMethod { get { return (VirtualInputMethod)KeyValuePairs[SAMSettings.INPUT_METHOD]; } set { KeyValuePairs[SAMSettings.INPUT_METHOD] = value; } }
        public bool HandleMicrosoftIME { get { return (bool)KeyValuePairs[SAMSettings.HANDLE_IME]; } set { KeyValuePairs[SAMSettings.HANDLE_IME] = value; } }
        public bool IME2FAOnly { get { return (bool)KeyValuePairs[SAMSettings.IME_2FA_ONLY]; } set { KeyValuePairs[SAMSettings.IME_2FA_ONLY] = value; } }

        #endregion

        #region Customize

        public string Theme { get { return (string)KeyValuePairs[SAMSettings.THEME]; } set { KeyValuePairs[SAMSettings.THEME] = value; } }
        public string Accent { get { return (string)KeyValuePairs[SAMSettings.ACCENT]; } set { KeyValuePairs[SAMSettings.ACCENT] = value; } }
        public int ButtonSize { get { return (int)KeyValuePairs[SAMSettings.BUTTON_SIZE]; } set { KeyValuePairs[SAMSettings.BUTTON_SIZE] = value; } }
        public string ButtonColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_COLOR] = value; } }
        public int ButtonFontSize { get { return (int)KeyValuePairs[SAMSettings.BUTTON_FONT_SIZE]; } set { KeyValuePairs[SAMSettings.BUTTON_FONT_SIZE] = value; } }
        public string ButtonFontColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_FONT_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_FONT_COLOR] = value; } }
        public string ButtonBannerColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_BANNER_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_BANNER_COLOR] = value; } }
        public int BannerFontSize { get { return (int)KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_SIZE]; } set { KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_SIZE] = value; } }
        public string BannerFontColor { get { return (string)KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_COLOR]; } set { KeyValuePairs[SAMSettings.BUTTON_BANNER_FONT_COLOR] = value; } }
        public bool HideBanIcons { get { return (bool)KeyValuePairs[SAMSettings.HIDE_BAN_ICONS]; } set { KeyValuePairs[SAMSettings.HIDE_BAN_ICONS] = value; } }

        #endregion

        #region Steam

        public string SteamPath { get { return (string)KeyValuePairs[SAMSettings.STEAM_PATH]; } set { KeyValuePairs[SAMSettings.STEAM_PATH] = value; } }
        public string ApiKey { get { return (string)KeyValuePairs[SAMSettings.STEAM_API_KEY]; } set { KeyValuePairs[SAMSettings.STEAM_API_KEY] = value; } }
        public bool AutoReloadEnabled { get { return (bool)KeyValuePairs[SAMSettings.AUTO_RELOAD_ENABLED]; } set { KeyValuePairs[SAMSettings.AUTO_RELOAD_ENABLED] = value; } }
        public int AutoReloadInterval { get { return (int)KeyValuePairs[SAMSettings.AUTO_RELOAD_INTERVAL]; } set { KeyValuePairs[SAMSettings.AUTO_RELOAD_INTERVAL] = value; } }
        public DateTime? LastAutoReload { 

            get {
                try
                {
                    return Convert.ToDateTime(KeyValuePairs[SAMSettings.LAST_AUTO_RELOAD]);
                }
                catch
                {
                    return null;
                }
            } 
            set { KeyValuePairs[SAMSettings.LAST_AUTO_RELOAD] = value; } 
        }
         
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
        public double ListViewHeight { get { return Convert.ToDouble(KeyValuePairs[SAMSettings.LIST_VIEW_HEIGHT]); } set { KeyValuePairs[SAMSettings.LIST_VIEW_HEIGHT] = value; } }
        public double ListViewWidth { get { return Convert.ToDouble(KeyValuePairs[SAMSettings.LIST_VIEW_WIDTH]); } set { KeyValuePairs[SAMSettings.LIST_VIEW_WIDTH] = value; } }

        #endregion

        #region Columns

        public int NameColumnIndex { get { return (int)KeyValuePairs[SAMSettings.NAME_COLUMN_INDEX]; } set { KeyValuePairs[SAMSettings.NAME_COLUMN_INDEX] = value; } }
        public int DescriptionColumnIndex { get { return (int)KeyValuePairs[SAMSettings.DESCRIPTION_COLUMN_INDEX]; } set { KeyValuePairs[SAMSettings.DESCRIPTION_COLUMN_INDEX] = value; } }
        public int TimeoutColumnIndex { get { return (int)KeyValuePairs[SAMSettings.TIMEOUT_COLUMN_INDEX]; } set { KeyValuePairs[SAMSettings.TIMEOUT_COLUMN_INDEX] = value; } }
        public int VacBansColumnIndex { get { return (int)KeyValuePairs[SAMSettings.VAC_BANS_COLUMN_INDEX]; } set { KeyValuePairs[SAMSettings.VAC_BANS_COLUMN_INDEX] = value; } }
        public int GameBanColumnIndex { get { return (int)KeyValuePairs[SAMSettings.GAME_BANS_COLUMN_INDEX]; } set { KeyValuePairs[SAMSettings.GAME_BANS_COLUMN_INDEX] = value; } }
        public int EconomyBanColumnIndex { get { return (int)KeyValuePairs[SAMSettings.ECO_BAN_COLUMN_INDEX]; } set { KeyValuePairs[SAMSettings.ECO_BAN_COLUMN_INDEX] = value; } }
        public int LastBanColumnIndex { get { return (int)KeyValuePairs[SAMSettings.LAST_BAN_COLUMN_INDEX]; } set { KeyValuePairs[SAMSettings.LAST_BAN_COLUMN_INDEX] = value; } }

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
            { SAMSettings.CLOSE_ON_LOGIN, false },
            { SAMSettings.LIST_VIEW, false },
            { SAMSettings.SANDBOX_MODE, false },

            { SAMSettings.LOGIN_RECENT_ACCOUNT, false },
            { SAMSettings.RECENT_ACCOUNT_INDEX, -1 },
            { SAMSettings.LOGIN_SELECTED_ACCOUNT, false },
            { SAMSettings.SELECTED_ACCOUNT_INDEX, -1 },
            { SAMSettings.INPUT_METHOD, VirtualInputMethod.SendMessage },
            { SAMSettings.HANDLE_IME, false },
            { SAMSettings.IME_2FA_ONLY, false },

            { SAMSettings.STEAM_PATH, string.Empty },
            { SAMSettings.STEAM_API_KEY, string.Empty },
            { SAMSettings.AUTO_RELOAD_ENABLED, false },
            { SAMSettings.AUTO_RELOAD_INTERVAL, 30 },
            { SAMSettings.LAST_AUTO_RELOAD, string.Empty },

            { SAMSettings.THEME, "BaseDark" },
            { SAMSettings.ACCENT, "Blue" },
            { SAMSettings.BUTTON_SIZE, 100 },
            { SAMSettings.BUTTON_COLOR, "#FFDDDDDD" },
            { SAMSettings.BUTTON_FONT_SIZE, 0 },
            { SAMSettings.BUTTON_FONT_COLOR, "#FF000000" },
            { SAMSettings.BUTTON_BANNER_COLOR, "#7F000000" },
            { SAMSettings.BUTTON_BANNER_FONT_SIZE, 0 },
            { SAMSettings.BUTTON_BANNER_FONT_COLOR, "#FFFFFF" },
            { SAMSettings.HIDE_BAN_ICONS, false },

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

            { SAMSettings.LIST_VIEW_HEIGHT, 300 },
            { SAMSettings.LIST_VIEW_WIDTH, 750 },

            { SAMSettings.NAME_COLUMN_INDEX, 0},
            { SAMSettings.DESCRIPTION_COLUMN_INDEX, 1 },
            { SAMSettings.TIMEOUT_COLUMN_INDEX, 2 },
            { SAMSettings.VAC_BANS_COLUMN_INDEX, 3 },
            { SAMSettings.GAME_BANS_COLUMN_INDEX, 4 },
            { SAMSettings.ECO_BAN_COLUMN_INDEX, 5 },
            { SAMSettings.LAST_BAN_COLUMN_INDEX, 6 }
        };
    }
}
