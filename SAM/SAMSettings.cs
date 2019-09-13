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

        public IniFile File = new IniFile(FILE_NAME);
        public UserSettings User = new UserSettings();
        public readonly UserSettings Default = new UserSettings();

        public Dictionary<string, string> KeyValuePairs = new Dictionary<string, string>()
        {
            { "ClearUserData", SECTION_GENERAL },
            { "HideAddButton",  SECTION_GENERAL },
            { "PasswordProtect", SECTION_GENERAL },
            { "MinimizeToTray", SECTION_GENERAL },
            { "RememberPassword", SECTION_GENERAL },
            { "StartMinimized", SECTION_GENERAL },
            { "StartWithWindows", SECTION_GENERAL },
            { "AccountsPerRow", SECTION_GENERAL },
            { "SleepTime", SECTION_GENERAL },
            { "CheckForUpdates", SECTION_GENERAL },

            { "LoginRecentAccount", SECTION_AUTOLOG },
            { "RecentAccountIndex", SECTION_AUTOLOG },
            { "LoginSelectedAccount", SECTION_AUTOLOG },
            { "SelectedAccountIndex", SECTION_AUTOLOG },

            { "ButtonSize", SECTION_CUSTOMIZE },
            { "ButtonColor", SECTION_CUSTOMIZE },
            { "ButtonFontSize", SECTION_CUSTOMIZE },
            { "ButtonFontColor", SECTION_CUSTOMIZE },
            { "ButtonBannerColor", SECTION_CUSTOMIZE },
            { "ButtonBannerFontSize", SECTION_CUSTOMIZE },
            { "ButtonBannerFontColor", SECTION_CUSTOMIZE },

            { "Path", SECTION_STEAM },

            { "cafeapplaunch", SECTION_PARAMETERS },
            { "clearbeta", SECTION_PARAMETERS },
            { "console", SECTION_PARAMETERS },
            { "developer", SECTION_PARAMETERS },
            { "forceservice", SECTION_PARAMETERS },
            { "login", SECTION_PARAMETERS },
            { "nocache", SECTION_PARAMETERS },
            { "noverifyfiles", SECTION_PARAMETERS },
            { "silent", SECTION_PARAMETERS },
            { "single_core", SECTION_PARAMETERS },
            { "tcp", SECTION_PARAMETERS },
            { "tenfoot", SECTION_PARAMETERS },
            { "customParameters", SECTION_PARAMETERS },
            { "customParametersValue", SECTION_PARAMETERS }
        };

        public void HandleDeprecatedSettings()
        {
            // Update Recent and Selected login setting names.
            if (File.KeyExists("Recent", "AutoLog"))
            {
                File.Write("LoginRecentAccount", File.Read("Recent", "AutoLog"), "AutoLog");
                File.DeleteKey("Recent", "AutoLog");
            }
            if (File.KeyExists("RecentAcc", "AutoLog"))
            {
                File.Write("RecentAccountIndex", File.Read("RecentAcc", "AutoLog"), "AutoLog");
                File.DeleteKey("RecentAcc", "AutoLog");
            }
            if (File.KeyExists("Selected", "AutoLog"))
            {
                File.Write("LoginSelectedAccount", File.Read("Selected", "AutoLog"), "AutoLog");
                File.DeleteKey("Selected", "AutoLog");
            }
            if (File.KeyExists("SelectedAcc", "AutoLog"))
            {
                File.Write("SelectedAccountIndex", File.Read("SelectedAcc", "AutoLog"), "AutoLog");
                File.DeleteKey("SelectedAcc", "AutoLog");
            }

            // Move Steam file path to it's own section.
            if (File.KeyExists("Steam", "Settings"))
            {
                File.Write("Path", File.Read("Steam", "Settings"), "Steam");
                File.DeleteKey("Steam", "Settings");
            }

            // Move button size to 'Customize' section.
            if (File.KeyExists("ButtonSize", "Settings"))
            {
                File.Write("ButtonSize", File.Read("ButtonSize", "Settings"), "Customize");
                File.DeleteKey("ButtonSize", "Settings");
            }
        }
    }
}
