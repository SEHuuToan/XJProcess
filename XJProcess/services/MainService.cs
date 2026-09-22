using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using XJProcess.modal;
using XJProcess.service;
using XJProcess.ultis; // Namespace chứa PlcUtils
using XJProcess.view;

namespace XJProcess.services
{
    public class MainService
    {
        private HeaderConfig? _headerConfig;
        private DrumConfig? _drumConfig;
        private DataGridView? _dataGridView;

        private readonly DispatcherTimer _stepTimer;

        // Quản lý các step đã hoàn thành bằng HashSet ngay tại MainService
        private readonly HashSet<ChemicalDetail> _completedItems = new();

        // Biến toàn cục thời gian
        public int Timer { get; set; }
        public int TotalTimer { get; set; }
        public int TimerPLC { get; set; }

        // Biến quản lý chu kỳ đảo chiều
        public int ReversePlcTimer { get; set; } // Chu kỳ thời gian đảo chiều (Giây)
        private int _reverseCounter = 0;               // Bộ đếm đếm ngược/tiến để kích hoạt đảo chiều

        public bool IsPlcConnected { get; set; } = false;

        // Trạng thái Bồn
        public bool IsDrumSpinning { get; private set; }
        public bool IsForwardDirection { get; set; } = true;
        public bool IsAutoMode { get; set; }
        public bool IsAutoPaused { get; set; }

        public ObservableCollection<ChemicalDetail> ChemicalList { get; private set; } = new ObservableCollection<ChemicalDetail>();
        public ChemicalDetail? CurrentRunningItem { get; set; }
        public ChemicalHeader CurrentOrder { get; set; }
        public bool IsManualRunning { get; set; }

        public MainService()
        {
            _stepTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _stepTimer.Tick += StepTimer_Tick;
        }

        public async Task ConnectPlcAsync(string ipAddress)
        {
            while (true)
            {
                IsPlcConnected = await Task.Run(() => PlcUtils.ConnectPlc(ipAddress));
                if (IsPlcConnected)
                {
                    break;
                }
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Lỗi kết nối đến PLC, vui lòng kiểm tra lại thiết bị","Lỗi kết nối PLC",MessageBoxButton.OK,MessageBoxImage.Error);
                });
            }
        }

        private bool ConfirmPlcConnection()
        {
            if (!IsPlcConnected)
            {
                MessageBox.Show("Chưa thể kết nối PLC, vui lòng thử lại thao tác này sau",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public bool HasAction(ChemicalDetail item)
        {
            return item != null && (item.DurationMinutes > 0 || !string.IsNullOrEmpty(item.CheckInfo));
        }

        public bool IsCompleted(ChemicalDetail item) => _completedItems.Contains(item);

        public void RegisterViews(HeaderConfig headerConfig, DrumConfig drumConfig, DataGridView dataGridView)
        {
            _headerConfig = headerConfig;
            _drumConfig = drumConfig;
            _dataGridView = dataGridView;

            if (_headerConfig != null) _headerConfig.Service = this;
            if (_drumConfig != null) _drumConfig.Service = this;
            if (_dataGridView != null)
            {
                _dataGridView.Service = this;

                if (ChemicalList != null && ChemicalList.Count > 0 && _dataGridView.MainDataGrid != null)
                {
                    _dataGridView.MainDataGrid.Visibility = System.Windows.Visibility.Visible;
                    _dataGridView.MainDataGrid.ItemsSource = ChemicalList;

                    if (_dataGridView.TxtNoOrderMessage != null)
                        _dataGridView.TxtNoOrderMessage.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        public int ConvertTime(int minutes) => minutes * 60;

        private void StartEngine()
        {
            // 1. Cài đặt chiều quay sang PLC
            if (IsForwardDirection)
            {
                PlcUtils.Up();
            }
            else
            {
                PlcUtils.Down();
            }

            // 2. Gửi thời gian cài đặt tổng sang PLC
            SendTimeToPLC(Timer);

            // 3. Kích hoạt lệnh chạy PLC
            PlcUtils.Start();

            if (!IsDrumSpinning)
            {
                IsDrumSpinning = true;
                _drumConfig?.StartSpinAnimation(IsForwardDirection, IsAutoMode);
            }

            if (!_stepTimer.IsEnabled) _stepTimer.Start();
        }

        private void StopEngine()
        {
            if (!ConfirmPlcConnection()) return;

            // Ngắt tín hiệu quay bồn ở PLC
            PlcUtils.Stop();

            IsDrumSpinning = false;
            _drumConfig?.StopSpinAnimation(IsAutoMode);

            if (_stepTimer.IsEnabled) _stepTimer.Stop();
        }

        /// <summary>
        /// Thực hiện quy trình dừng -> Đổi chiều UI & PLC -> Chạy lại
        /// </summary>
        private void ReverseDrum()
        {
            StopEngine();
            IsForwardDirection = !IsForwardDirection;
            PlcUtils.Reverse(true);
            StartEngine();
        }

        public void ToggleDirection(bool isForward)
        {
            if (!ConfirmPlcConnection()) return;
            IsForwardDirection = isForward;

            // Nếu bồn đang quay thì cập nhật PLC & UI ngay lập tức
            if (IsDrumSpinning)
            {
                if (IsForwardDirection)
                {
                    PlcUtils.Up();
                }
                else
                {
                    PlcUtils.Down();
                }
                PlcUtils.Reverse(true);
                _drumConfig.StartSpinAnimation(IsForwardDirection, IsAutoMode);
            }
        }

        public void StartManual()
        {
            if (!ConfirmPlcConnection()) return;
            IsManualRunning = true;
            CurrentRunningItem = null;
            Timer = 0;
            TotalTimer = 0;
            _reverseCounter = 0;
            ReversePlcTimer = 0;
            _drumConfig?.ResetCountdownDisplay();
            StartEngine();
        }

        public void StartAuto(int inputMinutes, int reverseIntervalMinutes = 0)
        {
            if (!ConfirmPlcConnection()) return;
            IsManualRunning = false;
            IsAutoPaused = false;
            CurrentRunningItem = null;

            _reverseCounter = 0;
            ReversePlcTimer = ConvertTime(reverseIntervalMinutes); // Quy đổi thời gian đảo chiều sang giây
            TotalTimer = Timer = ConvertTime(inputMinutes);

            _drumConfig?.UpdateCountdownDisplay(Timer, TotalTimer);
            StartEngine();
        }

        public void ResumeAuto()
        {
            if (!IsAutoPaused) return;
            if (!ConfirmPlcConnection()) return;

            IsAutoPaused = false;
            StartEngine();
        }

        public void StartCurrentStep()
        {
            if (CurrentRunningItem != null)
            {
                ToggleStepAction(CurrentRunningItem);
            }
            else if (ChemicalList != null)
            {
                foreach (var item in ChemicalList)
                {
                    if (HasAction(item) && !IsCompleted(item))
                    {
                        ToggleStepAction(item);
                        break;
                    }
                }
            }
        }

        public void InitializeStepStates()
        {
            if (ChemicalList == null || ChemicalList.Count == 0) return;

            bool isFirstActionFound = false;

            foreach (var item in ChemicalList)
            {
                if (!isFirstActionFound && item.HasAction && !IsCompleted(item) && item.Status != "Đã hoàn thành")
                {
                    item.IsEnabled = true;
                    isFirstActionFound = true;
                }
                else
                {
                    item.IsEnabled = false;
                }
            }
        }

        public void ToggleStepAction(ChemicalDetail clickedItem)
        {
            if (clickedItem == null || !clickedItem.HasAction) return;

            if (CurrentRunningItem == clickedItem && IsDrumSpinning)
            {
                clickedItem.IsRunning = false;
                clickedItem.IsPaused = true;
                StopEngine();
            }
            else
            {
                if (!ConfirmPlcConnection()) return;
                if (IsDrumSpinning)
                {
                    StopEngine();
                }
                IsManualRunning = false;
                bool isResuming = (CurrentRunningItem == clickedItem && clickedItem.IsPaused);
                CurrentRunningItem = clickedItem;
                CurrentRunningItem.IsRunning = true;
                CurrentRunningItem.IsPaused = false;

                LockAllOtherSteps(clickedItem);

                if (!isResuming || Timer <= 0)
                {
                    TotalTimer = ConvertTime(clickedItem.DurationMinutes);
                    Timer = TotalTimer;

                    _reverseCounter = 0;
                    // Lấy thời gian đảo chiều cấu hình riêng của step trong DataGridView (nếu có)
                    //ReversePlcTimer = ConvertTime(clickedItem.ReverseIntervalMinutes); tạm thời chưa cấu hình thời gian đảo chiều ở setting nên comment 
                }

                _drumConfig?.UpdateCountdownDisplay(Timer, TotalTimer);
                StartEngine();
            }
        }

        private void LockAllOtherSteps(ChemicalDetail activeItem)
        {
            if (ChemicalList == null) return;

            foreach (var item in ChemicalList)
            {
                item.IsEnabled = (item == activeItem);
            }
        }

        private void StepTimer_Tick(object? sender, EventArgs e)
        {
            if (IsManualRunning)
            {
                return;
            }

            if (Timer > 0)
            {
                Timer--;
                _drumConfig?.UpdateCountdownDisplay(Timer, TotalTimer);
                if (ReversePlcTimer > 0)
                {
                    _reverseCounter++;
                    if (_reverseCounter >= ReversePlcTimer)
                    {
                        _reverseCounter = 0; // Reset bộ đếm chu kỳ
                        ReverseDrum(); // Thực hiện dừng ➔ Đổi chiều ➔ Chạy lại
                    }
                }
            }
            else
            {
                _reverseCounter = 0;
                StopEngine();

                if (IsManualRunning)
                {
                    IsManualRunning = false;
                }
                else if (CurrentRunningItem != null)
                {
                    UnlockAndEnableNextStep(CurrentRunningItem);
                }
            }
        }
        //private void StepTimer_Tick(object? sender, EventArgs e)
        //{
        //    if (IsManualRunning)
        //    {
        //        return;
        //    }

        //    if (Timer > 0)
        //    {
        //        Timer--;
        //        _drumConfig?.UpdateCountdownDisplay(Timer, TotalTimer);

        //        // TH1: Thời gian vừa đếm về 0 -> Dừng hẳn bồn NGAY LẬP TỨC và chuyển Step
        //        if (Timer == 0)
        //        {
        //            _reverseCounter = 0;
        //            StopEngine();

        //            if (CurrentRunningItem != null)
        //            {
        //                UnlockAndEnableNextStep(CurrentRunningItem);
        //            }
        //        }
        //        // TH2: Vẫn còn thời gian chạy -> Mới xét đến việc đảo chiều
        //        else if (ReversePlcTimer > 0)
        //        {
        //            _reverseCounter++;
        //            if (_reverseCounter >= ReversePlcTimer)
        //            {
        //                _reverseCounter = 0; // Reset bộ đếm chu kỳ
        //                ReverseDrum(); // Thực hiện dừng ➔ Đổi chiều ➔ Chạy lại
        //            }
        //        }
        //    }
        //}

        public void StopCurrentStep()
        {
            if (CurrentRunningItem != null)
            {
                CurrentRunningItem.IsRunning = false;
                CurrentRunningItem.IsPaused = true;
            }
            else if (IsAutoMode && IsDrumSpinning)
            {
                IsAutoPaused = true;
            }
            StopEngine();
        }

        public void StopOrder()
        {
            StopEngine();
            IsManualRunning = false;
            IsAutoPaused = false;
            Timer = TotalTimer = TimerPLC = 0;
            _reverseCounter = 0;
            ReversePlcTimer = 0;
            CurrentRunningItem = null;
            _completedItems.Clear();
            _drumConfig?.ResetCountdownDisplay();
            ClearOrderHeaderInfo();
            if (_dataGridView?.MainDataGrid != null)
            {
                _dataGridView.MainDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                _dataGridView.MainDataGrid.ItemsSource = null;
                if (_dataGridView.TxtNoOrderMessage != null)
                    _dataGridView.TxtNoOrderMessage.Visibility = System.Windows.Visibility.Visible;
            }
        }

        public void SendTimeToPLC(int seconds)
        {
            if (seconds >= 0 && seconds <= short.MaxValue)
            {
                PlcUtils.Write((short)seconds);
            }
        }

        public void ClearOrderHeaderInfo()
        {
            if (CurrentOrder != null)
            {
                CurrentOrder.Line1Col0 = CurrentOrder.Line2Col0 = CurrentOrder.Line3Col0 = CurrentOrder.Line4Col0 = string.Empty;
                CurrentOrder.Line1Col1 = CurrentOrder.Line2Col1 = CurrentOrder.Line3Col1 = CurrentOrder.Line4Col1 = string.Empty;
                CurrentOrder.TechnicianName = CurrentOrder.DrumNo = CurrentOrder.StartTime = CurrentOrder.EndTime = string.Empty;
            }
            _headerConfig?.ClearOrderHeaderInfo();
        }

        private void UnlockAndEnableNextStep(ChemicalDetail currentItem)
        {
            if (ChemicalList == null) return;
            currentItem.Status = "Đã hoàn thành";
            currentItem.IsRunning = false;
            currentItem.IsPaused = false;
            currentItem.IsEnabled = false;

            _completedItems.Add(currentItem);
            CurrentRunningItem = null;

            int currentIndex = ChemicalList.IndexOf(currentItem);
            for (int i = currentIndex + 1; i < ChemicalList.Count; i++)
            {
                var nextItem = ChemicalList[i];
                if (nextItem.HasAction && nextItem.Status != "Đã hoàn thành")
                {
                    nextItem.IsEnabled = true;
                    break;
                }
            }
        }
    }
}