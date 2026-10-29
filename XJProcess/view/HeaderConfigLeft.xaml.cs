using System;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using XJProcess.services;
using XJProcess.ultis;

namespace XJProcess.view
{
    public partial class HeaderConfigLeft : UserControl
    {
        public MainService Service;
        private DispatcherTimer _clockTimer;

        public HeaderConfigLeft()
        {
            InitializeComponent();
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += StartRealTimeClock;
            _clockTimer.Tick += Timer_Tick;

            if (TxtCurrentTime != null)
                TxtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            _clockTimer.Start();

            this.Unloaded += (s, e) =>
            {
                if (_clockTimer != null) _clockTimer.Stop();
            };
        }

        private void StartRealTimeClock(object sender, EventArgs e)
        {
            if (TxtCurrentTime != null)
                TxtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Đọc nhiệt độ và pin từ PLC
                ushort tempValue = PlcUtils.ReadUInt16("VW750");
                float displayTemp = tempValue / 10.0f;
                byte batteryValue = PlcUtils.ReadByte("VB760");

                if (TxtTankTemp != null) TxtTankTemp.Text = $"{displayTemp:F1} °C";
                if (TxtTankBattery != null) TxtTankBattery.Text = $"{batteryValue} %";

                // --- LOGIC XÁC ĐỊNH TRẠNG THÁI THEO MÃ ĐƠN HÀNG ---
                // Lấy mã đơn hàng từ DataContext hiện tại (hoặc từ Service tùy cấu trúc view model của bro)
                string orderCode = string.Empty;

                // Nếu DataContext của UserControl này đang bind với object đơn hàng
                if (this.DataContext != null)
                {
                    // Thử lấy thuộc tính OrderCode (hoặc tên property chứa mã đơn hàng bên bro, ví dụ: Code, OrderNo,...)
                    var prop = this.DataContext.GetType().GetProperty("OrderCode") ?? this.DataContext.GetType().GetProperty("Code");
                    if (prop != null)
                    {
                        orderCode = prop.GetValue(this.DataContext)?.ToString() ?? string.Empty;
                    }
                }

                // Hoặc kiểm tra phòng hờ qua Service nếu có lưu trữ đơn hiện tại
                bool hasOrder = !string.IsNullOrEmpty(orderCode) && orderCode != "0";

                if (Service != null && Service.CurrentRunningItem != null)
                {
                    hasOrder = true; // Nếu service đang có item chạy thì chắc chắn có đơn
                }

                if (TxtDrumStatus != null)
                {
                    if (!hasOrder)
                    {
                        // Mã đơn bằng 0, null hoặc empty -> Sẵn sàng (Xanh lá)
                        TxtDrumStatus.Text = "Sẵn sàng";
                        TxtDrumStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
                    }
                    else
                    {
                        // Có mã đơn hàng -> Đang vận hành (Màu cam)
                        TxtDrumStatus.Text = "Đang vận hành";
                        TxtDrumStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FB923C"));
                    }
                }
            }
            catch (Exception)
            {
                if (TxtTankTemp != null) TxtTankTemp.Text = "Lỗi đọc";
                if (TxtTankBattery != null) TxtTankBattery.Text = "-- %";
            }
        }

        public void StopClockTimer()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
            }
        }

        public void ClearOrderHeaderInfo()
        {
            if (DataContext != null) DataContext = null;
        }
    }
}