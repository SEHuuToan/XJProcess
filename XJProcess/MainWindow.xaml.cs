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
            HeaderConfigControl.Service = _mainService;
            DataGridViewControl.Service = _mainService;

            var initialOrder = DataGridViewService.GetFakeOrderHeaderInfo();
            var initFakeDataDetail = DataGridViewService.GetFakeTechnicalSheetData();

            _mainService.ChemicalList.Clear();
            foreach (var detail in initFakeDataDetail)
            {
                _mainService.ChemicalList.Add(detail);
            }

            _mainService.RegisterViews(HeaderConfigControl, DrumConfig, DataGridViewControl);

            _mainService.CurrentOrder = initialOrder;
            HeaderConfigControl.OrderHeaderGrid.DataContext = initialOrder;
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
            HeaderConfigControl?.StopClockTimer();
            _mainService?.StopOrder();
        }

        private void ScanService_ScanerEvent(ScanService.ScanerCodes codes)
        {
            string orderCode = codes.Result;
            if (string.IsNullOrEmpty(orderCode)) return;

            Task.Run(async () =>
            {
                try
                {
                    var apiService = new ApiService();
                    var chemicalData = await apiService.LoadDataChemical(orderCode);

                    Dispatcher.Invoke(() =>
                    {
                        DrumConfig.UpdateScannedCode(orderCode);
                        if (chemicalData == null)
                        {
                            MessageBox.Show($"Không tìm thấy dữ liệu, vui lòng kiểm tra lại: {orderCode}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Lỗi hệ thống không thể tải dữ liệu lên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        #endregion
    }
}