using System.Windows;
using System.Windows.Input;

namespace XJProcess.view
{
    public partial class SettingWindow : Window
    {
        private const string ADMIN_PASSWORD = "888888";

        public SettingWindow()
        {
            InitializeComponent();
            TxtPassword.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            CheckPassword();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckPassword();
            }
        }

        private void CheckPassword()
        {
            if (TxtPassword.Password == ADMIN_PASSWORD)
            {
                SettingView settingDetailWindow = new SettingView();
                settingDetailWindow.Show();
                this.Close();
            }
            else
            {
                TxtErrorMessage.Visibility = Visibility.Visible;
                TxtPassword.Clear();
                TxtPassword.Focus();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}