using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using XJProcess.modal;
using XJProcess.service;
using XJProcess.view;

namespace XJProcess
{
    public partial class MainWindow : Window
    {
        private readonly View3DService _view3DService = new View3DService();
        private readonly AxisAngleRotation3D _spinnerRotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
        private bool _isSpinning = false;
        private bool _isForwardDirection = true;
        public ChemicalHeader CurrentOrder { get; set; }
        public ObservableCollection<ChemicalDetail> ChemicalList { get; set; }
        private DispatcherTimer _stepTimer;
        private DispatcherTimer _timer;
        private ChemicalDetail _currentRunningItem = null;
        private bool _isAutoMode = false;
        private int _manualRemainingSeconds = 0;
        private int _manualTotalSeconds = 0;
        private bool _isManualRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            Init3DScene();
            LoadChemicalData();
            BtnStart.Click += BtnStart_Click;
            if (BtnStop != null)
            {
                BtnStop.Click += BtnStop_Click;
            }
            BtnRotateUp.Click += BtnRotateUp_Click;
            BtnRotateDown.Click += BtnRotateDown_Click;
            StartRealTimeClock();
            if (TxtButtonStatus != null)
            {
                TxtButtonStatus.Text = "Xoay Xuống";
            }
        }
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
        private void ScanService_ScanerEvent(ScanService.ScanerCodes codes)
        {
            string orderCode = codes.Result;
            if (string.IsNullOrEmpty(orderCode)) return;
            Dispatcher.Invoke(() =>
            {

            });
            Task.Run(async () =>
            {
                try
                {
                    var chemicalData = await ApiService.LoadDataChemical(orderCode);
                    Dispatcher.Invoke(() =>
                    {
                        if (chemicalData != null)
                        {
                            // TODO: Thay các control (txtKhuVuc3, txtKhuVuc4) bằng tên control thực tế ở XAML của bạn

                            // Ví dụ hiển thị cho khu vực 3 (thông tin chung/mã đơn)
                            // txtKhuVuc3.Text = orderCode; 

                            // Ví dụ hiển thị cho khu vực 4 (dữ liệu chi tiết hóa chất trả về từ API)
                            // txtKhuVuc4.Text = chemicalData.ToString(); 
                        }
                        else
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

        private void LoadChemicalData()
        {
            CurrentOrder = DataGridViewService.GetFakeOrderHeaderInfo();
            OrderHeaderGrid.DataContext = CurrentOrder;
            ChemicalList = DataGridViewService.GetFakeTechnicalSheetData();
            ProcessGroupHeaders(ChemicalList);
            if (ChemicalList != null && ChemicalList.Count > 0)
            {
                var firstStepNo = ChemicalList[0].StepNo;
                foreach (var item in ChemicalList)
                {
                    item.RemainingSeconds = item.DurationMinutes * 60;
                    item.IsCurrentStep = (item.StepNo == firstStepNo);
                }
                InitializeProcessFlow(ChemicalList);
                MainDataGrid.ItemsSource = ChemicalList;
                MainDataGrid.Visibility = Visibility.Visible;
                if (TxtNoOrderMessage != null)
                {
                    TxtNoOrderMessage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void InitializeProcessFlow(ObservableCollection<ChemicalDetail> dataList)
        {
            if (dataList == null) return;
            foreach (var item in dataList)
            {
                if (item.HasAction)
                {
                    item.IsEnabled = true;
                    item.RemainingSeconds = item.DurationMinutes * 60;
                }
            }
        }

        private void ProcessGroupHeaders(ObservableCollection<ChemicalDetail> list)
        {
            if (list == null) return;

            string lastStep = null;
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item.StepNo) && item.StepNo != lastStep)
                {
                    item.IsGroupHeader = true;
                    lastStep = item.StepNo;
                }
                else if (string.IsNullOrEmpty(item.StepNo))
                {
                    item.IsGroupHeader = true;
                    lastStep = null;
                }
                else
                {
                    item.IsGroupHeader = false;
                }
            }
        }

        private void InitStepTimer()
        {
            _stepTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _stepTimer.Tick += StepTimer_Tick;
        }

        private void StepTimer_Tick(object sender, EventArgs e)
        {
            if (_isManualRunning)
            {
                if (_manualRemainingSeconds > 0)
                {
                    _manualRemainingSeconds--;
                    UpdateCountdownDisplay(_manualRemainingSeconds, _manualTotalSeconds);
                }
                else
                {
                    _isManualRunning = false;
                    _stepTimer.Stop();
                    StopDrumSpin();
                }
                return;
            }
            if (_currentRunningItem != null && _currentRunningItem.IsRunning)
            {
                if (_currentRunningItem.RemainingSeconds > 0)
                {
                    _currentRunningItem.RemainingSeconds--;
                    UpdateCountdownDisplay(_currentRunningItem.RemainingSeconds);
                }
                else
                {
                    _currentRunningItem.IsRunning = false;
                    _stepTimer.Stop();
                    StopDrumSpin();
                    UnlockAndEnableNextStep(_currentRunningItem);
                }
            }
        }

        private void UnlockAndEnableNextStep(ChemicalDetail currentItem)
        {
            if (ChemicalList == null) return;
            int currentIndex = ChemicalList.IndexOf(currentItem);
            currentItem.Status = "Đã hoàn thành";
            currentItem.IsRunning = false;
            currentItem.IsPaused = false;
            currentItem.IsEnabled = false;
            foreach (var item in ChemicalList)
            {
                if (item.HasAction && item.Status != "Đã hoàn thành")
                {
                    item.IsEnabled = false;
                }
            }
            for (int i = currentIndex + 1; i < ChemicalList.Count; i++)
            {
                if (ChemicalList[i].HasAction)
                {
                    ChemicalList[i].IsEnabled = true;
                    ChemicalList[i].IsPaused = false;
                    if (ChemicalList[i].RemainingSeconds <= 0)
                    {
                        ChemicalList[i].RemainingSeconds = ChemicalList[i].DurationMinutes * 60;
                    }
                    break;
                }
            }
        }

        private void UpdateCountdownDisplay(int remainingSeconds, int? customTotalSeconds = null)
        {
            int hours = remainingSeconds / 3600;
            int minutes = (remainingSeconds % 3600) / 60;
            int seconds = remainingSeconds % 60;

            if (hours > 0)
            {
                TxtCountdownTime.Text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            else
            {
                TxtCountdownTime.Text = $"{minutes:D2}:{seconds:D2}";
            }
            int totalSeconds = 1;
            if (customTotalSeconds.HasValue)
            {
                totalSeconds = customTotalSeconds.Value;
            }
            else if (_currentRunningItem != null)
            {
                totalSeconds = _currentRunningItem.DurationMinutes * 60;
            }

            if (ProgressArc != null && totalSeconds > 0)
            {
                XJProcess.utils.SystemUtils.UpdateProgressArc(ProgressArc, remainingSeconds, totalSeconds);
            }
            else
            {
                XJProcess.utils.SystemUtils.UpdateProgressArc(ProgressArc, 0, 1);
            }
        }

        private void Init3DScene()
        {
            try
            {
                string objPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "3dmodel.obj");
                RotateTransform3D rotateTransform = new RotateTransform3D(_spinnerRotation);
                Model3DGroup loadedModel = _view3DService.Load3DModel(objPath, rotateTransform);
                ModelVisual3D modelVisual = new ModelVisual3D
                {
                    Content = loadedModel
                };
                TankViewport.Children.Clear();
                TankViewport.Children.Add(new DefaultLights());
                TankViewport.Children.Add(modelVisual);
                TankViewport.Camera = _view3DService.GetFrontFlatCamera();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load mô hình: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartDrumSpin()
        {
            if (!_isSpinning)
            {
                double currentAngle = _spinnerRotation.Angle;
                double targetAngle = _isForwardDirection ? currentAngle + 360 : currentAngle - 360;
                var spinAnimation = new DoubleAnimation
                {
                    To = targetAngle,
                    Duration = TimeSpan.FromSeconds(3),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                _spinnerRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, spinAnimation);
                _isSpinning = true;
                if (btn_Setting != null) btn_Setting.IsEnabled = false;
                if (BtnRotateUp != null) BtnRotateUp.IsEnabled = false;
                if (BtnRotateDown != null) BtnRotateDown.IsEnabled = false;
                if (TglMode != null) TglMode.IsEnabled = false;
            }
            if (TxtButtonStatus != null)
            {
                TxtButtonStatus.Text = _isForwardDirection ? "Xoay Xuống" : "Xoay Lên";
            }
        }

        private void StopDrumSpin()
        {
            if (_isSpinning)
            {
                double currentAngle = _spinnerRotation.Angle;
                _spinnerRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
                _spinnerRotation.Angle = currentAngle;

                _isSpinning = false;
                if (btn_Setting != null) btn_Setting.IsEnabled = true;
                if (BtnRotateUp != null) BtnRotateUp.IsEnabled = !_isAutoMode;
                if (BtnRotateDown != null) BtnRotateDown.IsEnabled = !_isAutoMode;
                if (TglMode != null) TglMode.IsEnabled = true;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            int inputMinutes = 0;
            bool hasManualInput = TxtRunTimeInput != null && int.TryParse(TxtRunTimeInput.Text, out inputMinutes) && inputMinutes > 0;
            if (hasManualInput)
            {
                StartDrumSpin();

                _manualTotalSeconds = inputMinutes * 60;
                _manualRemainingSeconds = _manualTotalSeconds;
                _isManualRunning = true;
                UpdateCountdownDisplay(_manualRemainingSeconds, _manualTotalSeconds);
                if (!_stepTimer.IsEnabled)
                {
                    _stepTimer.Start();
                }
            }
            else if (_currentRunningItem != null)
            {
                StartDrumSpin();
                _isManualRunning = false;
                if (!_currentRunningItem.IsRunning)
                {
                    _currentRunningItem.IsRunning = true;
                    _currentRunningItem.IsPaused = false;
                    UpdateCountdownDisplay(_currentRunningItem.RemainingSeconds);
                    if (!_stepTimer.IsEnabled)
                    {
                        _stepTimer.Start();
                    }
                }
            }
            else
            {
                return;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopDrumSpin();
            _stepTimer.Stop();
            if (_currentRunningItem != null)
            {
                _currentRunningItem.IsRunning = false;
                _currentRunningItem.IsPaused = true;
            }
        }

        private void btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow loginDialog = new SettingWindow();
            loginDialog.Owner = this;
            loginDialog.ShowDialog();
        }

        private void btn_History_Click(object sender, RoutedEventArgs e)
        {
            HistoryView historyView = new HistoryView();
            historyView.Owner = this;
            historyView.ShowDialog();
        }

        private void StartRealTimeClock()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) =>
            {
                TxtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            };
            _timer.Start();

            TxtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer?.Stop();
            _stepTimer?.Stop();
        }

        private void BtnRotateUp_Click(object sender, RoutedEventArgs e)
        {
            if (_isAutoMode) return;
            _isForwardDirection = false;
            if (_isSpinning)
            {
                StopDrumSpin();
                StartDrumSpin();
            }
            TxtButtonStatus.Text = "Xoay Lên";
        }

        private void BtnRotateDown_Click(object sender, RoutedEventArgs e)
        {
            if (_isAutoMode) return;
            _isForwardDirection = true;
            if (_isSpinning)
            {
                StopDrumSpin();
                StartDrumSpin();
            }
            TxtButtonStatus.Text = "Xoay Xuống";
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ChemicalDetail clickedItem)
            {
                if (clickedItem.IsRunning)
                {
                    clickedItem.IsRunning = false;
                    clickedItem.IsPaused = true;
                    _stepTimer.Stop();
                    StopDrumSpin();
                }
                else
                {
                    if (_currentRunningItem != null && _currentRunningItem != clickedItem)
                    {
                        _currentRunningItem.IsRunning = false;
                        _currentRunningItem.IsPaused = true;
                    }
                    _currentRunningItem = clickedItem;
                    _currentRunningItem.IsRunning = true;
                    _currentRunningItem.IsPaused = false;
                    if (_currentRunningItem.RemainingSeconds <= 0)
                    {
                        _currentRunningItem.RemainingSeconds = _currentRunningItem.DurationMinutes * 60;
                    }
                    if (ChemicalList != null)
                    {
                        foreach (var item in ChemicalList)
                        {
                            if (item.HasAction)
                            {
                                item.IsEnabled = (item == clickedItem);
                            }
                        }
                    }
                    StartDrumSpin();
                    _stepTimer.Start();
                }
            }
        }

        private void BtnStopOrder_Click(object sender, RoutedEventArgs e)
        {
            StopDrumSpin();
            if (_stepTimer != null)
            {
                _stepTimer.Stop();
            }
            _isManualRunning = false;
            _manualRemainingSeconds = 0;
            _manualTotalSeconds = 0;
            if (_currentRunningItem != null)
            {
                _currentRunningItem.IsRunning = false;
                _currentRunningItem = null;
            }
            if (TxtCountdownTime != null)
            {
                TxtCountdownTime.Text = "00:00";
            }

            if (ProgressArc != null)
            {
                XJProcess.utils.SystemUtils.UpdateProgressArc(ProgressArc, 0, 1);
            }
            if (ChemicalList != null)
            {
                foreach (var item in ChemicalList)
                {
                    if (item.HasAction)
                    {
                        item.IsEnabled = true;
                    }
                }
            }
            ClearOrderHeaderInfo();
            if (MainDataGrid != null)
            {
                MainDataGrid.Visibility = Visibility.Collapsed;
                MainDataGrid.ItemsSource = null;
            }
            if (TxtNoOrderMessage != null)
            {
                TxtNoOrderMessage.Visibility = Visibility.Visible;
            }
        }

        private void ClearOrderHeaderInfo()
        {
            if (CurrentOrder != null)
            {
                CurrentOrder.Line1Col0 = string.Empty;
                CurrentOrder.Line2Col0 = string.Empty;
                CurrentOrder.Line3Col0 = string.Empty;
                CurrentOrder.Line4Col0 = string.Empty;

                CurrentOrder.Line1Col1 = string.Empty;
                CurrentOrder.Line2Col1 = string.Empty;
                CurrentOrder.Line3Col1 = string.Empty;
                CurrentOrder.Line4Col1 = string.Empty;

                CurrentOrder.TechnicianName = string.Empty;
                CurrentOrder.DrumNo = string.Empty;
                CurrentOrder.StartTime = string.Empty;
                CurrentOrder.EndTime = string.Empty;
            }
        }

        private void TglMode_Checked(object sender, RoutedEventArgs e)
        {
            _isAutoMode = true;
            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = false;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = false;
            if (TxtReverseTimeInput != null)
            {
                TxtReverseTimeInput.IsEnabled = false;
                TxtReverseTimeInput.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
            }
        }

        private void TglMode_Unchecked(object sender, RoutedEventArgs e)
        {
            _isAutoMode = false;
            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = true;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = true;
            if (TxtReverseTimeInput != null)
            {
                TxtReverseTimeInput.IsEnabled = true;
                TxtReverseTimeInput.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private bool IsTextAllowed(string text)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}