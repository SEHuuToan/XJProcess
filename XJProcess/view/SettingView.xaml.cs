using System.Windows;
using XJProcess.modal;
using XJProcess.Services;

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
                TxtApiUrl.Text = config.ApiUrl;
                TxtPingInterval.Text = config.PingInterval;
                TxtTimeout.Text = config.Timeout;

                // Nhóm 2: Vận hành bồn quay
                TxtDefaultSpeed.Text = config.DefaultSpeed;
                TxtReverseDelay.Text = config.ReverseDelay;
                TxtMotorAccel.Text = config.MotorAccel;
                TxtStepAngle.Text = config.StepAngle;
                TxtStopDelay.Text = config.StopDelay;
                TxtDefaultMode.Text = config.DefaultMode;

                // Nhóm 3: Giới hạn an toàn
                TxtMaxTemp.Text = config.MaxTemp;
                TxtMaxPressure.Text = config.MaxPressure;
                TxtMaxWeight.Text = config.MaxWeight;
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
                ApiUrl = TxtApiUrl.Text,
                PingInterval = TxtPingInterval.Text,
                Timeout = TxtTimeout.Text,

                // Nhóm 2
                DefaultSpeed = TxtDefaultSpeed.Text,
                ReverseDelay = TxtReverseDelay.Text,
                MotorAccel = TxtMotorAccel.Text,
                StepAngle = TxtStepAngle.Text,
                StopDelay = TxtStopDelay.Text,
                DefaultMode = TxtDefaultMode.Text,

                // Nhóm 3
                MaxTemp = TxtMaxTemp.Text,
                MaxPressure = TxtMaxPressure.Text,
                MaxWeight = TxtMaxWeight.Text
            };

            // Gọi service để lưu xuống thư mục Setting/setting.xml
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