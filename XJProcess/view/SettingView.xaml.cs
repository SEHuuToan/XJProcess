using System.Windows;
using System.Windows.Controls;
using XJProcess.modal;
using XJProcess.services;
using XJProcess.Services;
using XJProcess.ultis;

namespace XJProcess.view
{
    public partial class SettingView : Window
    {
        public static event Action? OnSettingsSaved;
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
                TxtDrumNumber.Text = config.DrumNumber;
                TxtIpAddress.Text = config.IpAddress;
                TxtWebSocketPort.Text = config.WebSocketPort;
                TxtUid.Text = config.uid.ToString("X");
                TxtSlave.Text = config.slaveId;
                // Nhóm 2: Vận hành bồn quay
                TxtReverseDelay.Text = config.ReverseTime.ToString();
                // Mặc định là false nếu config chưa có
                if (TglIsReverse != null)
                {
                    TglIsReverse.IsChecked = config.IsReverse;
                }
            }
            else
            {
                if (TglIsReverse != null)
                {
                    TglIsReverse.IsChecked = false;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int revTime = 0;
            if (int.TryParse(TxtReverseDelay.Text.Trim(), out int parsedVal))
            {
                revTime = parsedVal;
            }
            SettingModel config = new SettingModel()
            {
                DrumNumber = TxtDrumNumber.Text,
                IpAddress = TxtIpAddress.Text,
                WebSocketPort = TxtWebSocketPort.Text,
                uid = string.IsNullOrEmpty(TxtUid.Text) ? (short)0 : Convert.ToInt16(TxtUid.Text.Trim(), 16),
                slaveId = TxtSlave.Text,
                ReverseTime = revTime,
                IsReverse = TglIsReverse?.IsChecked ?? false
            };
            SettingService.SaveSettings(config);
            if (config != null)
            {
                PlcUtils.TempSlave(config.slaveId);
                PlcUtils.TempUID(config.uid);
            }
            OnSettingsSaved?.Invoke();
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