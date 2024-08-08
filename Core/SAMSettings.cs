using System;
using System.Collections.Generic;
using System.Reflection;

namespace SAM.Core
{
    class SAMSettings
    {
        public const string FILE_NAME = "SAMSettings.ini";

        public const string SECTION_SYSTEM = "System";
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
        public List<string> globalParameters;

        public const string VERSION = "Version";

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
        public const string DEVELOPER_PARAMETER = "dev";
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

        public const string LIGHT_THEME = "Light";
        public const string DARK_THEME = "Dark";

        public const string NAME_COLUMN_INDEX = "NameColumnIndex";
        public const string DESCRIPTION_COLUMN_INDEX = "DescriptionColumnIndex";
        public const string TIMEOUT_COLUMN_INDEX = "TimeoutColumnIndex";
        public const string VAC_BANS_COLUMN_INDEX = "VacBansColumnIndex";
        public const string GAME_BANS_COLUMN_INDEX = "GameBansColumnIndex";
        public const string ECO_BAN_COLUMN_INDEX = "EcoBanColumnIndex";
        public const string LAST_BAN_COLUMN_INDEX = "LastBanColumnIndex";

        public Dictionary<string, string> KeyValuePairs = new Dictionary<string, string>()
        {
            { VERSION, SECTION_SYSTEM },

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
        public SAMSettings()
        {
            HandleDeprecatedSettings();
            UpdateVersion();
            ReadSettingsFile();
        }

        public void GenerateSettings()
        {
            foreach (KeyValuePair<string, string> entry in KeyValuePairs)
            {
                File.Write(entry.Key, Default.KeyValuePairs[entry.Key].ToString(), entry.Value);
            }
        }

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

            // Update developer launch parameter.
            if (File.KeyExists("developer", SECTION_PARAMETERS))
            {
                File.Write(DEVELOPER_PARAMETER, File.Read("developer", SECTION_PARAMETERS), SECTION_PARAMETERS);
                File.DeleteKey("developer", SECTION_PARAMETERS);
            }

            // Remove 'Base' prefix from theme value.
            if (File.KeyExists(THEME, SECTION_CUSTOMIZE))
            {
                string value = File.Read(THEME, SECTION_CUSTOMIZE);
                if (value.StartsWith("Base"))
                {
                    File.Write(THEME, value.Substring(4), SECTION_CUSTOMIZE);
                }
            }
        }

        public void ReadSettingsFile()
        {
            globalParameters = new List<string>();

            foreach (KeyValuePair<string, string> entry in KeyValuePairs)
            {
                if (!File.KeyExists(entry.Key, entry.Value))
                {
                    File.Write(entry.Key, Default.KeyValuePairs[entry.Key].ToString(), entry.Value);
                }
                else
                {
                    switch (entry.Key)
                    {
                        case ACCOUNTS_PER_ROW:
                            string accountsPerRowString = File.Read(ACCOUNTS_PER_ROW, SECTION_GENERAL);

                            if (!Int32.TryParse(accountsPerRowString, out int accountsPerRow) || accountsPerRow < 1)
                            {
                                File.Write(ACCOUNTS_PER_ROW, Default.AccountsPerRow.ToString(), SECTION_GENERAL);
                                User.AccountsPerRow = Default.AccountsPerRow;
                            }
                            else
                            {
                                User.AccountsPerRow = accountsPerRow;
                            }
                            break;

                        case SLEEP_TIME:
                            string sleepTimeString = File.Read(SLEEP_TIME, SECTION_GENERAL);

                            if (!Single.TryParse(sleepTimeString, out float sleepTime) || sleepTime < 0 || sleepTime > 100)
                            {
                                File.Write(SLEEP_TIME, Default.SleepTime.ToString(), SECTION_GENERAL);
                                User.SleepTime = Default.SleepTime * 1000;
                            }
                            else
                            {
                                User.SleepTime = (int)(sleepTime * 1000);
                            }
                            break;

                        case START_MINIMIZED:
                            User.StartMinimized = Convert.ToBoolean(File.Read(START_MINIMIZED, SECTION_GENERAL));
                            break;

                        case BUTTON_SIZE:
                            string buttonSizeString = File.Read(BUTTON_SIZE, SECTION_CUSTOMIZE);

                            if (!Int32.TryParse(buttonSizeString, out int buttonSize) || buttonSize < 50 || buttonSize > 200)
                            {
                                File.Write(BUTTON_SIZE, "100", SECTION_CUSTOMIZE);
                                User.ButtonSize = 100;
                            }
                            else
                            {
                                User.ButtonSize = buttonSize;
                            }
                            break;

                        case INPUT_METHOD:
                            User.VirtualInputMethod = (VirtualInputMethod)Enum.Parse(typeof(VirtualInputMethod), File.Read(INPUT_METHOD, SECTION_AUTOLOG));
                            break;

                        default:
                            switch (Type.GetTypeCode(User.KeyValuePairs[entry.Key].GetType()))
                            {
                                case TypeCode.Boolean:
                                    User.KeyValuePairs[entry.Key] = Convert.ToBoolean(File.Read(entry.Key, entry.Value));
                                    if (entry.Value.Equals(SECTION_PARAMETERS) && (bool)User.KeyValuePairs[entry.Key] == true && !entry.Key.StartsWith("custom"))
                                    {
                                        globalParameters.Add("-" + entry.Key);
                                    }
                                    break;
                                case TypeCode.Int32:
                                    User.KeyValuePairs[entry.Key] = Convert.ToInt32(File.Read(entry.Key, entry.Value));
                                    break;
                                case TypeCode.Double:
                                    User.KeyValuePairs[entry.Key] = Convert.ToDouble(File.Read(entry.Key, entry.Value));
                                    break;

                                default:
                                    User.KeyValuePairs[entry.Key] = File.Read(entry.Key, entry.Value);
                                    break;
                            }
                            break;
                    }
                }
            }
        }

        public void UpdateVersion()
        {
            File.Write(VERSION, Assembly.GetExecutingAssembly().GetName().Version.ToString(), SECTION_SYSTEM);
        }

        public void EnableSelectedAccountIndex(int index)
        {
            File.Write(SELECTED_ACCOUNT_INDEX, index.ToString(), SECTION_AUTOLOG);
            File.Write(LOGIN_SELECTED_ACCOUNT, true.ToString(), SECTION_AUTOLOG);
            File.Write(LOGIN_RECENT_ACCOUNT, false.ToString(), SECTION_AUTOLOG);
            User.LoginSelectedAccount = true;
            User.LoginRecentAccount = false;
            User.SelectedAccountIndex = index;
        }

        public void ResetSelectedAccountIndex()
        {
            File.Write(SELECTED_ACCOUNT_INDEX, "-1", SECTION_AUTOLOG);
            File.Write(LOGIN_SELECTED_ACCOUNT, false.ToString(), SECTION_AUTOLOG);
            User.LoginSelectedAccount = false;
            User.SelectedAccountIndex = -1;
        }

        public void EnableRecentAccountIndex(int index) 
        {
            File.Write(SELECTED_ACCOUNT_INDEX, index.ToString(), SECTION_AUTOLOG);
            File.Write(LOGIN_SELECTED_ACCOUNT, true.ToString(), SECTION_AUTOLOG);
            File.Write(LOGIN_RECENT_ACCOUNT, false.ToString(), SECTION_AUTOLOG);
            User.LoginSelectedAccount = true;
            User.LoginRecentAccount = false;
            User.SelectedAccountIndex = index;
        }

        public void UpdateRecentAccountIndex(int index)
        {
            File.Write(RECENT_ACCOUNT_INDEX, index.ToString(), SECTION_AUTOLOG);
            User.RecentAccountIndex = index;
        }

        public bool IsLoginSelectedEnabled() 
        {
            return File.Read(LOGIN_SELECTED_ACCOUNT, SECTION_AUTOLOG) == true.ToString();
        }

        public void SetPasswordProtect(bool passwordProtect)
        {
            File.Write(PASSWORD_PROTECT, passwordProtect.ToString(), SECTION_GENERAL);
            User.PasswordProtect = passwordProtect;
        }

        public void SetLastAutoReload(DateTime dateTime)
        {
            File.Write(LAST_AUTO_RELOAD, dateTime.ToString(), SECTION_STEAM);
            User.LastAutoReload = dateTime;
        }
    }
}
