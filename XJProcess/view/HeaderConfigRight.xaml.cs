using System.Windows;
using System.Windows.Controls;
using XJProcess.services;
using XJProcess.modal;

namespace XJProcess.view
{
    public partial class HeaderConfigRight : UserControl
    {
        public MainService Service;
        private Language language = new Language();

        public HeaderConfigRight()
        {
            InitializeComponent();
            UpdateLanguageSelection();
        }

        private void BtnStopOrder_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Bạn có muốn kết thúc đơn này không?", "Xác nhận kết thúc đơn", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Service?.StopOrder();
            }
        }

        private void btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow loginDialog = new SettingWindow { Owner = Window.GetWindow(this) };
            loginDialog.ShowDialog();
        }

        private void btn_History_Click(object sender, RoutedEventArgs e)
        {
            HistoryView historyView = new HistoryView { Owner = Window.GetWindow(this) };
            historyView.ShowDialog();
        }

        private void btn_Language_Click(object sender, RoutedEventArgs e)
        {
            LanguagePopup.IsOpen = !LanguagePopup.IsOpen;
        }
        private void btn_Chinese_Click(object sender, RoutedEventArgs e)
        {
            language.SetLanguage("zh");
            MainService.ChangeAppLanguage("zh");
            UpdateLanguageSelection();
            LanguagePopup.IsOpen = false;
        }

        private void btn_Vietnamese_Click(object sender, RoutedEventArgs e)
        {
            language.SetLanguage("vi");
            MainService.ChangeAppLanguage("vi");
            UpdateLanguageSelection();
            LanguagePopup.IsOpen = false;
        }

        private void UpdateLanguageSelection()
        {
            bool isChinese = language.CurrentLanguage == "zh";
            ChineseCheck.Visibility = isChinese ? Visibility.Visible : Visibility.Collapsed;
            VietnameseCheck.Visibility = isChinese ? Visibility.Collapsed : Visibility.Visible;
            if (isChinese)
            {
                CurrentLangText.Text = "Zh";
                CurrentFlagImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/images/china.png", UriKind.Relative));
            }
            else
            {
                CurrentLangText.Text = "Vi";
                CurrentFlagImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/images/vietnam.png", UriKind.Relative));
            }
        }
    }
}