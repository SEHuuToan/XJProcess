using System.Windows;
using System.Windows.Controls;
using XJProcess.services;

namespace XJProcess.view
{
    public partial class HeaderConfigRight : UserControl
    {
        public MainService Service;

        public HeaderConfigRight()
        {
            InitializeComponent();
        }

        private void BtnStopOrder_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Bạn có muốn kết thúc đơn này không?",
                "Xác nhận kết thúc đơn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

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
    }
}