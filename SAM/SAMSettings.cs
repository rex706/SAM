using System.Collections.Generic;

namespace SAM
{
    class SAMSettings
    {
        public const string FILE_NAME = "SAMSettings.ini";

        public const string SECTION_GENERAL = "Settings";
        public const string SECTION_AUTOLOG = "AutoLog";
        public const string SECTION_CUSTOMIZE = "Customize";
        public const string SECTION_STEAM = "Steam";
        public const string SECTION_PARAMETERS = "Parameters";
        public const string SECTION_LOCATION = "Location";
        public const string SECTION_COLUMNS = "Columns";

        public IniFile File = new IniFile(FILE_NAME);
        public UserSettings User = new UserSettings();
        public readonly UserSettings Default = new UserSettings();

        public const string CLEAR_USER_DATA = "ClearUserData";
        public const string HIDE_ADD_BUTTON = "HideAddButton";
        public const string PASSWORD_PROTECT = "PasswordProtect";
        public const string MINIMIZE_TO_TRAY = "MinimizeToTray";
        public const string REMEMBER_PASSWORD = "RememberPassword";
        public const string START_MINIMIZED = "StartMinimized";
        public const string START_WITH_WINDOWS = "StartWithWindows";
        public const string ACCOUNTS_PER_ROW = "AccountsPerRow";
        public const string SLEEP_TIME = "SleepTime";
        public const string CHECK_FOR_UPDATES = "CheckForUpdates";
        public const string CLOSE_ON_LOGIN = "CloseOnLogin";
        public const string LIST_VIEW = "ListView";
        public const string SANDBOX_MODE = "SandboxMode";

        public const string LOGIN_RECENT_ACCOUNT = "LoginRecentAccount";
        public const string RECENT_ACCOUNT_INDEX = "RecentAccountIndex";
        public const string LOGIN_SELECTED_ACCOUNT = "LoginSelectedAccount";
        public const string SELECTED_ACCOUNT_INDEX = "SelectedAccountIndex";
        public const string INPUT_METHOD = "InputMethod";
        public const string HANDLE_IME = "HandleIME";
        public const string IME_2FA_ONLY = "IME_2FA_ONLY";

        public const string THEME = "Theme";
        public const string ACCENT = "Accent";
        public const string BUTTON_SIZE = "ButtonSize";
        public const string BUTTON_COLOR = "ButtonColor";
        public const string BUTTON_FONT_SIZE = "ButtonFontSize";
        public const string BUTTON_FONT_COLOR = "ButtonFontColor";
        public const string BUTTON_BANNER_COLOR = "ButtonBannerColor";
        public const string BUTTON_BANNER_FONT_SIZE = "ButtonBannerFontSize";
        public const string BUTTON_BANNER_FONT_COLOR = "ButtonBannerFontColor";
        public const string HIDE_BAN_ICONS = "HideBanIcons";

        public const string STEAM_PATH = "Path";
        public const string STEAM_API_KEY = "ApiKey";
        public const string AUTO_RELOAD_ENABLED = "AutoReloadEnabled";
        public const string AUTO_RELOAD_INTERVAL = "AutoReloadInterval";
        public const string LAST_AUTO_RELOAD = "LastAutoReload";

        public const string CAFE_APP_LAUNCH_PARAMETER = "cafeapplaunch";
        public const string CLEAR_BETA_PARAMETER = "clearbeta";
        public const string CONSOLE_PARAMETER = "console";
        public const string DEVELOPER_PARAMETER = "developer";
        public const string FORCE_SERVICE_PARAMETER = "forceservice";
        public const string LOGIN_PARAMETER = "login";
        public const string NO_CACHE_PARAMETER = "nocache";
        public const string NO_VERIFY_FILES_PARAMETER = "noverifyfiles";
        public const string SILENT_PARAMETER = "silent";
        public const string SINGLE_CORE_PARAMETER = "single_core";
        public const string TCP_PARAMETER = "tcp";
        public const string TEN_FOOT_PARAMETER = "tenfoot";
        public const string CUSTOM_PARAMETERS = "customParameters";
        public const string CUSTOM_PARAMETERS_VALUE = "customParametersValue";

        public const string WINDOW_TOP = "WindowTop";
        public const string WINDOW_LEFT = "WindowLeft";
        public const string LIST_VIEW_HEIGHT = "ListViewHeight";
        public const string LIST_VIEW_WIDTH = "ListViewWidth";

        public const string LIGHT_THEME = "BaseLight";
        public const string DARK_THEME = "BaseDark";

        public const string NAME_COLUMN_INDEX = "NameColumnIndex";
        public const string DESCRIPTION_COLUMN_INDEX = "DescriptionColumnIndex";
        public const string TIMEOUT_COLUMN_INDEX = "TimeoutColumnIndex";
        public const string VAC_BANS_COLUMN_INDEX = "VacBansColumnIndex";
        public const string GAME_BANS_COLUMN_INDEX = "GameBansColumnIndex";
        public const string ECO_BAN_COLUMN_INDEX = "EcoBanColumnIndex";
        public const string LAST_BAN_COLUMN_INDEX = "LastBanColumnIndex";

        public Dictionary<string, string> KeyValuePairs = new Dictionary<string, string>()
        {
            { CLEAR_USER_DATA, SECTION_GENERAL },
            { HIDE_ADD_BUTTON,  SECTION_GENERAL },
            { PASSWORD_PROTECT, SECTION_GENERAL },
            { MINIMIZE_TO_TRAY, SECTION_GENERAL },
            { REMEMBER_PASSWORD, SECTION_GENERAL },
            { START_MINIMIZED, SECTION_GENERAL },
            { START_WITH_WINDOWS, SECTION_GENERAL },
            { ACCOUNTS_PER_ROW, SECTION_GENERAL },
            { SLEEP_TIME, SECTION_GENERAL },
            { CHECK_FOR_UPDATES, SECTION_GENERAL },
            { CLOSE_ON_LOGIN, SECTION_GENERAL },
            { LIST_VIEW, SECTION_GENERAL },
            { SANDBOX_MODE, SECTION_GENERAL },

            { LOGIN_RECENT_ACCOUNT, SECTION_AUTOLOG },
            { RECENT_ACCOUNT_INDEX, SECTION_AUTOLOG },
            { LOGIN_SELECTED_ACCOUNT, SECTION_AUTOLOG },
            { SELECTED_ACCOUNT_INDEX, SECTION_AUTOLOG },
            { INPUT_METHOD, SECTION_AUTOLOG },
            { HANDLE_IME, SECTION_AUTOLOG },
            { IME_2FA_ONLY, SECTION_AUTOLOG },

            { THEME, SECTION_CUSTOMIZE },
            { ACCENT, SECTION_CUSTOMIZE },
            { BUTTON_SIZE, SECTION_CUSTOMIZE },
            { BUTTON_COLOR, SECTION_CUSTOMIZE },
            { BUTTON_FONT_SIZE, SECTION_CUSTOMIZE },
            { BUTTON_FONT_COLOR, SECTION_CUSTOMIZE },
            { BUTTON_BANNER_COLOR, SECTION_CUSTOMIZE },
            { BUTTON_BANNER_FONT_SIZE, SECTION_CUSTOMIZE },
            { BUTTON_BANNER_FONT_COLOR, SECTION_CUSTOMIZE },
            { HIDE_BAN_ICONS, SECTION_CUSTOMIZE },

            { STEAM_PATH, SECTION_STEAM },
            { STEAM_API_KEY, SECTION_STEAM },
            { AUTO_RELOAD_ENABLED, SECTION_STEAM},
            { AUTO_RELOAD_INTERVAL, SECTION_STEAM },
            { LAST_AUTO_RELOAD, SECTION_STEAM },

            { CAFE_APP_LAUNCH_PARAMETER, SECTION_PARAMETERS },
            { CLEAR_BETA_PARAMETER, SECTION_PARAMETERS },
            { CONSOLE_PARAMETER, SECTION_PARAMETERS },
            { DEVELOPER_PARAMETER, SECTION_PARAMETERS },
            { FORCE_SERVICE_PARAMETER, SECTION_PARAMETERS },
            { LOGIN_PARAMETER, SECTION_PARAMETERS },
            { NO_CACHE_PARAMETER, SECTION_PARAMETERS },
            { NO_VERIFY_FILES_PARAMETER, SECTION_PARAMETERS },
            { SILENT_PARAMETER, SECTION_PARAMETERS },
            { SINGLE_CORE_PARAMETER, SECTION_PARAMETERS },
            { TCP_PARAMETER, SECTION_PARAMETERS },
            { TEN_FOOT_PARAMETER, SECTION_PARAMETERS },
            { CUSTOM_PARAMETERS, SECTION_PARAMETERS },
            { CUSTOM_PARAMETERS_VALUE, SECTION_PARAMETERS },

            { LIST_VIEW_HEIGHT, SECTION_LOCATION },
            { LIST_VIEW_WIDTH, SECTION_LOCATION },

            { NAME_COLUMN_INDEX, SECTION_COLUMNS },
            { DESCRIPTION_COLUMN_INDEX, SECTION_COLUMNS },
            { TIMEOUT_COLUMN_INDEX, SECTION_COLUMNS },
            { VAC_BANS_COLUMN_INDEX, SECTION_COLUMNS },
            { GAME_BANS_COLUMN_INDEX, SECTION_COLUMNS },
            { ECO_BAN_COLUMN_INDEX, SECTION_COLUMNS },
            { LAST_BAN_COLUMN_INDEX, SECTION_COLUMNS }
        };

        public Dictionary<string, string> ListViewColumns = new Dictionary<string, string>
        {
            { "Name", NAME_COLUMN_INDEX },
            { "Description", DESCRIPTION_COLUMN_INDEX },
            { "Timeout", TIMEOUT_COLUMN_INDEX },
            { "VAC Bans", VAC_BANS_COLUMN_INDEX },
            { "Game Bans", GAME_BANS_COLUMN_INDEX},
            { "Economy Ban", ECO_BAN_COLUMN_INDEX },
            { "Last Ban (Days)", LAST_BAN_COLUMN_INDEX }
        };

        public void HandleDeprecatedSettings()
        {
            // Update Recent and Selected login setting names.
            if (File.KeyExists("Recent", SECTION_AUTOLOG))
            {
                File.Write(LOGIN_RECENT_ACCOUNT, File.Read("Recent", SECTION_AUTOLOG), SECTION_AUTOLOG);
                File.DeleteKey("Recent", SECTION_AUTOLOG);
            }
            if (File.KeyExists("RecentAcc", SECTION_AUTOLOG))
            {
                File.Write(RECENT_ACCOUNT_INDEX, File.Read("RecentAcc", SECTION_AUTOLOG), SECTION_AUTOLOG);
                File.DeleteKey("RecentAcc", SECTION_AUTOLOG);
            }
            if (File.KeyExists("Selected", SECTION_AUTOLOG))
            {
                File.Write(LOGIN_SELECTED_ACCOUNT, File.Read("Selected", SECTION_AUTOLOG), SECTION_AUTOLOG);
                File.DeleteKey("Selected", SECTION_AUTOLOG);
            }
            if (File.KeyExists("SelectedAcc", SECTION_AUTOLOG))
            {
                File.Write(SELECTED_ACCOUNT_INDEX, File.Read("SelectedAcc", SECTION_AUTOLOG), SECTION_AUTOLOG);
                File.DeleteKey("SelectedAcc", SECTION_AUTOLOG);
            }

            // Move Steam file path to it's own section.
            if (File.KeyExists(SECTION_STEAM, SECTION_GENERAL))
            {
                File.Write(STEAM_PATH, File.Read(SECTION_STEAM, SECTION_GENERAL), SECTION_STEAM);
                File.DeleteKey(SECTION_STEAM, SECTION_GENERAL);
            }

            // Move button size to 'Customize' section.
            if (File.KeyExists(BUTTON_SIZE, SECTION_GENERAL))
            {
                File.Write(BUTTON_SIZE, File.Read(BUTTON_SIZE, SECTION_GENERAL), SECTION_CUSTOMIZE);
                File.DeleteKey(BUTTON_SIZE, SECTION_GENERAL);
            }
        }
    }
}
