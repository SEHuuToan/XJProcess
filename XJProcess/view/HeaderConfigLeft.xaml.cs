using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using XJProcess.modal;
using XJProcess.services;
using XJProcess.Services;
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
            LoadConfig();
            SettingView.OnSettingsSaved += LoadConfig;
            var settings = XJProcess.Services.SettingService.LoadSettings();
            string drumNumber = settings.DrumNumber?.ToString() ?? "--"; 

            if (TxtTankCode != null)
            {
                TxtTankCode.Text = drumNumber.EndsWith("#") ? drumNumber : drumNumber + "#";
            }

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
                SettingView.OnSettingsSaved -= LoadConfig;
            };
        }
        public void LoadConfig()
        {
            var settings = SettingService.LoadSettings();
            string drumNumber = settings.DrumNumber?.ToString() ?? "--";

            if (TxtTankCode != null)
            {
                TxtTankCode.Text = drumNumber.EndsWith("#") ? drumNumber : drumNumber + "#";
            }
        }
        private void StartRealTimeClock(object sender, EventArgs e)
        {
            if (TxtCurrentTime != null)
                TxtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void SetData(ChemicalData data)
        {
            if (data == null) return;

            Dispatcher.Invoke(() =>
            {
                if (txtOrderCode != null)
                    txtOrderCode.Text = data.Mobill ?? string.Empty;

                if (TxtTechnician != null)
                    TxtTechnician.Text = data.Tech ?? string.Empty;
                //người vận hành ( thêm sau )
                //if (TxtOperator != null)
                //    TxtOperator.Text = data.User ?? string.Empty;

                if (TxtWeight != null)
                    TxtWeight.Text = data.Weight ?? string.Empty;
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                ushort tempValue = PlcUtils.ReadUInt16("VW750");
                float displayTemp = tempValue / 10.0f;
                byte batteryValue = PlcUtils.ReadByte("VB760");
                if (TxtTankTemp != null) TxtTankTemp.Text = $"{displayTemp:F1} °C";
                if (TxtTankBattery != null) TxtTankBattery.Text = $"{batteryValue} %";
                string orderCode = string.Empty;
                if (this.DataContext != null)
                {
                    var prop = this.DataContext.GetType().GetProperty("OrderCode") ?? this.DataContext.GetType().GetProperty("Code");
                    if (prop != null)
                    {
                        orderCode = prop.GetValue(this.DataContext)?.ToString() ?? string.Empty;
                    }
                }
                bool hasDatabaseOrder = !string.IsNullOrEmpty(orderCode) && orderCode != "0";
                bool isEngineActive = false;
                if (Service != null)
                {
                    if (Service.IsDrumSpinning || (Service.CurrentRunningItem != null && Service.IsRunning == 1))
                    {
                        isEngineActive = true;
                    }
                }
                if (TxtDrumStatus != null)
                {
                    if (!hasDatabaseOrder && !isEngineActive)
                    {
                        TxtDrumStatus.Text = Application.Current.FindResource("Str_Ready") as string ?? "Sẵn sàng";
                        TxtDrumStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399")); // Xanh lá
                    }
                    else
                    {
                        TxtDrumStatus.Text = Application.Current.FindResource("Str_Running") as string ?? "Đang vận hành";
                        TxtDrumStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FB923C")); // Màu cam
                    }
                }
            }
            catch (Exception)
            {
                if (TxtTankTemp != null) TxtTankTemp.Text = Application.Current.FindResource("Str_ErrRead") as string ?? "Sẵn sàng";
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
     
            if (txtOrderCode != null) txtOrderCode.Text = string.Empty;
            if (TxtTechnician != null) TxtTechnician.Text = string.Empty;
            if (TxtOperator != null) TxtOperator.Text = string.Empty;
        }
    }
}