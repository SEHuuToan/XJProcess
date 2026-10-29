using System;
using System.Threading.Tasks;
using System.Windows;
using XJProcess.service;
using XJProcess.services;
using XJProcess.ultis;

namespace XJProcess
{
    public partial class MainWindow : Window
    {
        private readonly MainService _mainService = new MainService();

        public MainWindow()
        {
            InitializeComponent();
            InitServicesAndViews();
        }

        private void InitServicesAndViews()
        {
            HeaderConfigLeftControl.Service = _mainService;
            HeaderConfigRightControl.Service = _mainService;
            DataGridViewControl.Service = _mainService;

            var initialOrder = DataGridViewService.GetFakeOrderHeaderInfo();
            var initFakeDataDetail = DataGridViewService.GetFakeTechnicalSheetData();

            _mainService.ChemicalList.Clear();
            foreach (var detail in initFakeDataDetail)
            {
                _mainService.ChemicalList.Add(detail);
            }

            _mainService.RegisterViews(HeaderConfigLeftControl, DrumConfig, DataGridViewControl);

            _mainService.CurrentOrder = initialOrder;
            HeaderConfigRightControl.DataContext = initialOrder;
        }

        #region Window LifeCycle & Scanner

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = _mainService.ConnectPlcAsync("192.168.2.1");
            ScanService.ScanerEvent += ScanService_ScanerEvent;
            bool isStarted = ScanService.Start();
            if (!isStarted)
            {
                MessageBox.Show("Không thể khởi động kết nối máy quét!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ScanService.ScanerEvent -= ScanService_ScanerEvent;
            ScanService.Stop();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            HeaderConfigLeftControl?.StopClockTimer();
            _mainService?.StopOrder();
        }

        private async void ScanService_ScanerEvent(ScanService.ScanerCodes codes)
        {
            string rawQr = codes.Result;
            if (string.IsNullOrEmpty(rawQr)) return;
            await _mainService.ProcessQrCodeAsync(rawQr);
        }

        #endregion
    }
}