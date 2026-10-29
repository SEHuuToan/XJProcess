using System;
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
                ushort tempValue = PlcUtils.ReadUInt16("VW750");
                float displayTemp = tempValue / 10.0f;
                byte batteryValue = PlcUtils.ReadByte("VB760");

                if (TxtTankTemp != null) TxtTankTemp.Text = $"{displayTemp:F1} °C";
                if (TxtTankBattery != null) TxtTankBattery.Text = $"{batteryValue} %";
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