using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace SAM
{
    class Utils
    {
        public static void Serialize(List<Account> input)
        {
            var serializer = new XmlSerializer(input.GetType());
            var sw = new StreamWriter("info.dat");
            serializer.Serialize(sw, input);
            sw.Close();
        }

        public static List<Account> Deserialize(string file)
        {
            var stream = new StreamReader(file);
            var ser = new XmlSerializer(typeof(List<Account>));
            object obj = ser.Deserialize(stream);
            stream.Close();
            return (List<Account>)obj;
        }

        public static void ImportAccountFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".dat",
                Filter = "SAM DAT Files (*.dat)|*.dat"
            };

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    var tempAccounts = Deserialize(dialog.FileName);
                    MainWindow.encryptedAccounts = MainWindow.encryptedAccounts.Concat(tempAccounts).ToList();
                    Serialize(MainWindow.encryptedAccounts);
                    MessageBox.Show("Accounts imported!");
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void ImportMassAccountFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "SAM JSON Files (*.json)|*.json"
            };

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    List<Account> array = JsonConvert.DeserializeObject<List<Account>>(File.ReadAllText(dialog.FileName));
                    foreach (var account in array)
                    {
                        account.Password = StringCipher.Encrypt(account.Password, MainWindow.RequestEKey());
                        account.SharedSecret = StringCipher.Encrypt(account.SharedSecret, MainWindow.RequestEKey());
                    }
                    MainWindow.encryptedAccounts = MainWindow.encryptedAccounts.Concat(array).ToList();
                    Serialize(MainWindow.encryptedAccounts);
                    MessageBox.Show("Accounts mass imported!");
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void ExportAccountFile()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.Copy("info.dat", dialog.SelectedPath + "\\info.dat");
                    MessageBox.Show("File exported to:\n" + dialog.SelectedPath);
                }
                catch (Exception m)
                {
                    MessageBox.Show(m.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
