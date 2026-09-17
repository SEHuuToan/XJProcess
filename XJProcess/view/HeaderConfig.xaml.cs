using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XJProcess.modal;
using XJProcess.services;
using XJProcess.view;

namespace XJProcess.view
{
    public partial class HeaderConfig : UserControl
    {
        public ChemicalHeader CurrentOrder { get; set; }
        public MainService? Service { get; set; }
        private DispatcherTimer _clockTimer;

        public HeaderConfig()
        {
            InitializeComponent();
            StartRealTimeClock();
        }

        private void StartRealTimeClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) =>
            {
                if (TxtCurrentTime != null) TxtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            };
            _clockTimer.Start();
            if (TxtCurrentTime != null) TxtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void StopClockTimer()
        {
            _clockTimer?.Stop();
        }

        public void ClearOrderHeaderInfo()
        {
            if (CurrentOrder != null)
            {
                CurrentOrder.Line1Col0 = CurrentOrder.Line2Col0 = CurrentOrder.Line3Col0 = CurrentOrder.Line4Col0 = string.Empty;
                CurrentOrder.Line1Col1 = CurrentOrder.Line2Col1 = CurrentOrder.Line3Col1 = CurrentOrder.Line4Col1 = string.Empty;
                CurrentOrder.TechnicianName = CurrentOrder.DrumNo = CurrentOrder.StartTime = CurrentOrder.EndTime = string.Empty;
            }
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