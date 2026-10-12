using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XJProcess.ultis;
using static XJProcess.ultis.PlcUtils;

namespace XJProcess.view
{
    public partial class TestScanView : UserControl
    {
        private DispatcherTimer _timer;

        public TestScanView()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Định kỳ mỗi 1 giây quét 1 lần
            _timer.Tick += Timer_Tick;

            this.Loaded += (s, e) =>
            {
                // --- BƯỚC 1: KẾT NỐI VÀ GỬI CẤU HÌNH KHI MỞ UI ---
                PlcUtils.IsSimulationMode = false; // Đảm bảo không chạy giả lập

                // Kết nối vào IP thực tế của PLC S7-200 Smart (Nhớ đổi lại IP đúng với xưởng của bro)
                bool isConnected = PlcUtils.ConnectPlc("192.168.0.1");

                if (isConnected)
                {
                    // Gửi cấu hình chỉ điểm hộp thu sóng xuống PLC
                    InitializeDeviceConfig();
                }
                else
                {
                    TxtTemperature.Text = "Chưa kết nối PLC";
                }

                // Bắt đầu bật timer chạy định kỳ
                _timer.Start();
            };

            this.Unloaded += (s, e) =>
            {
                _timer.Stop();
            };
        }

        /// <summary>
        /// Gửi thông số cấu hình xuống PLC để bộ thu biết đọc trạm nào, UID nào
        /// </summary>
        private void InitializeDeviceConfig()
        {
            try
            {
                if (!PlcUtils.IsConnected) return;

                // 1. Cấu hình Slave ID (VB800)
                byte slaveId = 1;
                PlcUtils.WriteByte("VB800", slaveId);

                // 2. Cấu hình Input UID (VW850)
                ushort inputUid = 12345; // Thay bằng UID thực tế của bro
                PlcUtils.WriteUInt16("VW850", inputUid);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi ghi cấu hình nếu có
            }
        }

        // --- BƯỚC 2: ĐỌC DỮ LIỆU ĐỊNH KỲ MỖI GIÂY TỪ PLC ---
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Nếu chưa kết nối hoặc mất kết nối thì dừng, tuyệt đối không tự sinh data giả
                if (!PlcUtils.IsConnected)
                {
                    TxtTemperature.Text = "Chưa kết nối PLC";
                    return;
                }

                // Đọc thực tế từ ô nhớ PLC do hộp thu sóng trả về
                ushort vw750Temp = PlcUtils.ReadUInt16("VW750");  // Nhiệt độ
                byte vb760Battery = PlcUtils.ReadByte("VB760");   // % Pin

                // Tính toán hiển thị (Nếu PLC nhân 10 thì chia 10, nếu lưu trực tiếp thì bỏ / 10.0f đi)
                float displayTemp = vw750Temp / 10.0f;

                // Hiển thị kết quả thật lên giao diện
                TxtTemperature.Text = $"{displayTemp:F1} °C (Pin: {vb760Battery}%)";
            }
            catch (Exception ex)
            {
                TxtTemperature.Text = "Lỗi đọc dữ liệu PLC";
            }
        }
    }
}