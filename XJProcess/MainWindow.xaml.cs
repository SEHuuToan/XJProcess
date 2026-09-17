using System;
using System.Threading.Tasks;
using System.Windows;
using XJProcess.service;
using XJProcess.services;

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
            // 1. Gán MainService cho các UserControl
            HeaderConfigControl.Service = _mainService;
            DataGridViewControl.Service = _mainService;

            // 2. Đăng ký các View vào MainService để lắng nghe & điều khiển tập trung
            _mainService.RegisterViews(HeaderConfigControl, DrumConfig, DataGridViewControl);

            // 3. Nạp dữ liệu mẫu ban đầu qua MainService
            var initialOrder = DataGridViewService.GetFakeOrderHeaderInfo();
            HeaderConfigControl.CurrentOrder = initialOrder;
            HeaderConfigControl.OrderHeaderGrid.DataContext = initialOrder;

            _mainService.LoadOrderData(initialOrder);
        }

        #region Window LifeCycle & Scanner

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            _mainService?.StopStepTimer();
        }

        private void ScanService_ScanerEvent(ScanService.ScanerCodes codes)
        {
            string orderCode = codes.Result;
            if (string.IsNullOrEmpty(orderCode)) return;

            Task.Run(async () =>
            {
                try
                {
                    var chemicalData = await ApiService.LoadDataChemical(orderCode);
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