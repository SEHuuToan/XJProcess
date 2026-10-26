using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XJProcess.services;
using XJProcess.ultis;

namespace XJProcess.view
{
    public partial class HeaderConfig : UserControl
    {
        public MainService Service;
        private DispatcherTimer _clockTimer;
        public HeaderConfig()
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
        private void BtnStopOrder_Click(object sender, RoutedEventArgs e)
        {
            Service?.StopOrder();
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