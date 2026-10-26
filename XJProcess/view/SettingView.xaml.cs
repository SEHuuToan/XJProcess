using System.Windows;
using System.Windows.Controls;
using XJProcess.modal;
using XJProcess.Services;
using XJProcess.ultis;

namespace XJProcess.view
{
    public partial class SettingView : Window
    {
        public SettingView()
        {
            InitializeComponent();

            // Tự động nạp dữ liệu từ file XML lên các TextBox khi mở form
            LoadDataToUI();
        }

        private void LoadDataToUI()
        {
            SettingModel config = SettingService.LoadSettings();

            if (config != null)
            {
                // Nhóm 1: Thiết bị & Mạng
                TxtDrumId.Text = config.DrumId;
                TxtIpAddress.Text = config.IpAddress;
                TxtWebSocketPort.Text = config.WebSocketPort;
                TxtUid.Text = config.uid.ToString("X");
                TxtSlave.Text = config.slaveId;

                // Nhóm 2: Vận hành bồn quay
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Gom dữ liệu từ các TextBox vào Model
            SettingModel config = new SettingModel()
            {
                // Nhóm 1
                DrumId = TxtDrumId.Text,
                IpAddress = TxtIpAddress.Text,
                WebSocketPort = TxtWebSocketPort.Text,
                uid = string.IsNullOrEmpty(TxtUid.Text) ? (short)0 : Convert.ToInt16(TxtUid.Text.Trim(), 16),
                slaveId = TxtSlave.Text

                // Nhóm 2

            };
            SettingService.SaveSettings(config);
            MessageBox.Show("Đã lưu tất cả thông số cấu hình hệ thống thành công!", "Thông báo HMI",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}