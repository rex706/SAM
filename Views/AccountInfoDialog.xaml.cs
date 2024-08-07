﻿using MahApps.Metro.Controls;
using System.Windows;
using SAM.Core;
using System;
using System.Linq;
using System.Diagnostics;

namespace SAM.Views
{
    /// <summary>
    /// Interaction logic for TextDialog.xaml
    /// </summary>
    public partial class AccountInfoDialog : MetroWindow
    {
        private const int STEAMID64_LENGTH = 17;

        public AccountInfoDialog()
        {
            InitializeComponent();

            FriendsOnlineStatusComboBox.ItemsSource = Enum.GetValues(typeof(FriendsLoginStatus)).Cast<FriendsLoginStatus>();
        }

        public string AccountText
        {
            get { return UsernameBox.Text; }
            set { UsernameBox.Text = value; }
        }

        public string AliasText
        {
            get { return AliasBox.Text;  }
            set { AliasBox.Text = value; }
        }

        public string PasswordText
        {
            get { return PasswordBox.Password; }
            set { PasswordBox.Password = value; }
        }

        public string SharedSecretText
        {
            get { return SharedSecretBox.Password; }
            set { SharedSecretBox.Password = value; }
        }

        public string UrlText
        {
            get { return UrlTextBox.Text; }
            set { UrlTextBox.Text = value; }
        }

        private string OriginalUrlText { get; set; }

        public string ParametersText
        {
            get { return ParametersBox.Text; }
            set { ParametersBox.Text = value; }
        }

        public string DescriptionText
        {
            get { return DescriptionBox.Text; }
            set { DescriptionBox.Text = value; }
        }

        public FriendsLoginStatus FriendsLoginStatus
        {
            get { return (FriendsLoginStatus)FriendsOnlineStatusComboBox.SelectedItem; }
            set { FriendsOnlineStatusComboBox.SelectedItem = value; }
        }

        public bool AutoLogAccountIndex { get; set; }

        public string AviText { get; set; }

        public string SteamId 
        {
            get { return SteamIdBox.Text; }
            set { SteamIdBox.Text = value; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountText == null || AccountText.Length == 0)
            {
                MessageBox.Show("Account login required!");
                UsernameBox.Focus();
                return;
            }
            if (PasswordText == null || PasswordText.Length == 0)
            {
                MessageBox.Show("Account password required!");
                PasswordBox.Focus();
                return;
            }

            if (autoLogCheckBox.IsChecked == true)
                AutoLogAccountIndex = true;
            else
                AutoLogAccountIndex = false;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void UsernameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (UsernameBox.Text.Length < 3)
            {
                return;
            }

            OKButton.IsEnabled = false;

            dynamic userJson = await AccountUtils.GetUserInfoFromConfigAndWebApi(UsernameBox.Text.ToString());

            if (userJson != null)
            {
                try
                {
                    dynamic profileUrl = userJson.response.players[0].profileurl;
                    dynamic avatarUrl = userJson.response.players[0].avatarfull;
                    dynamic steamId = userJson.response.players[0].steamid;

                    UrlTextBox.Text = profileUrl;

                    SteamId = steamId;
                    AviText = avatarUrl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                SteamId = null;
                AviText = null;
            }

            OKButton.IsEnabled = true;
        }

        private void UrlBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OriginalUrlText = UrlTextBox.Text;
        }

        private async void UrlBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (UrlTextBox.Text != OriginalUrlText && SteamId == null)
            {
                OKButton.IsEnabled = false; 

                dynamic steamId = await AccountUtils.GetSteamIdFromProfileUrl(UrlTextBox.Text);

                if (steamId != null)
                {
                    SteamId = steamId;
                }

                OKButton.IsEnabled = true;
            }
        }

        private void SteamIdBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (SteamId.Length == STEAMID64_LENGTH && long.TryParse(SteamId, out _))
            {
                FriendsOnlineStatusComboBox.IsEnabled = true;
            }
            else
            {
                FriendsOnlineStatusComboBox.IsEnabled = false;
                FriendsOnlineStatusComboBox.SelectedItem = FriendsLoginStatus.Unchanged;
            }
        }

        private void CustomParamsHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://developer.valvesoftware.com/wiki/Command_Line_Options#Steam_.28Windows.29");
        }

        private void SharedSecretHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Jessecar96/SteamDesktopAuthenticator");
        }
    }
}
