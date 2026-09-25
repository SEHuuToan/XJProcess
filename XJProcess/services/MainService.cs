using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using XJProcess.modal;
using XJProcess.service;
using XJProcess.ultis;
using XJProcess.view;

namespace XJProcess.services
{
    public class MainService
    {
        private HeaderConfig _headerConfig;
        private DrumConfig _drumConfig;
        private DataGridView _dataGridView;

        private readonly DispatcherTimer _stepTimer;
        private readonly HashSet<ChemicalDetail> _completedItems = new();

        /// <summary>
        /// Trạng thái hoạt động của bồn:
        /// 0 = Stop (Bồn dừng)
        /// 1 = Start (Bồn đang quay tự động / chạy bước từ DataGridView)
        /// 2 = Pause (Tạm dừng)
        /// 3 = Finish (Hoàn thành chu trình quay)
        /// 4 = User Manual Override (Can thiệp thời gian -> Bước DataGridView chuyển "Hoàn tất" & Unlock bước tiếp)
        /// </summary>
        public int IsRunning { get; set; } = 0;

        public int Timer { get; set; }
        public int TotalTimer { get; set; }

        public int ReversePlcTimer { get; set; }
        private int _reverseCounter = 0;

        public bool IsPlcConnected { get; set; } = false;

        // Bồn đang quay khi trạng thái ở 1 (Start) hoặc 4 (User Override)
        public bool IsDrumSpinning => IsRunning == 1 || IsRunning == 4;
        public bool IsForwardDirection { get; set; } = true;
        public bool IsAutoMode { get; set; }
        public bool IsAutoPaused => IsRunning == 2;

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
                if (IsPlcConnected) break;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Lỗi kết nối đến PLC, vui lòng kiểm tra lại thiết bị", "Lỗi kết nối PLC", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    _dataGridView.MainDataGrid.Visibility = Visibility.Visible;
                    _dataGridView.MainDataGrid.ItemsSource = ChemicalList;

                    if (_dataGridView.TxtNoOrderMessage != null)
                        _dataGridView.TxtNoOrderMessage.Visibility = Visibility.Collapsed;
                }
            }
        }

        public int ConvertTime(int minutes) => minutes * 60;

        private void StartEngine()
        {
            if (IsManualRunning)
            {
                if (IsForwardDirection) PlcUtils.Up();
                else PlcUtils.Down();
            }

            PlcUtils.Start();

            if (IsDrumSpinning)
            {
                _drumConfig.StartSpinAnimation(IsForwardDirection, IsAutoMode);

                if (!IsManualRunning && !_stepTimer.IsEnabled)
                {
                    _stepTimer.Start();
                }
            }
        }

        private void StopEngine()
        {
            if (!ConfirmPlcConnection()) return;

            PlcUtils.Stop();
            _drumConfig.StopSpinAnimation(IsAutoMode);

            if (_stepTimer.IsEnabled) _stepTimer.Stop();
        }

        private void ReverseDrum()
        {
            IsForwardDirection = !IsForwardDirection;
            PlcUtils.Reverse(true);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _drumConfig.StartSpinAnimation(IsForwardDirection, IsAutoMode);
            });
        }

        public void ToggleDirection(bool isForward)
        {
            if (!ConfirmPlcConnection()) return;
            IsForwardDirection = isForward;

            if (IsDrumSpinning)
            {
                if (IsForwardDirection) PlcUtils.Up();
                else PlcUtils.Down();

                PlcUtils.Reverse(true);
                _drumConfig.StartSpinAnimation(IsForwardDirection, IsAutoMode);
            }
        }

        public void StartManual()
        {
            if (!ConfirmPlcConnection()) return;

            IsManualRunning = true;
            IsAutoMode = false;
            IsRunning = 1;

            CurrentRunningItem = null;
            Timer = TotalTimer = ReversePlcTimer = _reverseCounter = 0;

            _drumConfig.ResetCountdownDisplay();
            StartEngine();
        }

        //public void StartAuto(int inputMinutes, int reverseIntervalMinutes = 0)
        //{
        //    if (!ConfirmPlcConnection()) return;

        //    IsManualRunning = false;
        //    IsAutoMode = true;

        //    _reverseCounter = 0;
        //    ReversePlcTimer = ConvertTime(reverseIntervalMinutes);
        //    TotalTimer = Timer = ConvertTime(inputMinutes);

        //    if (CurrentRunningItem != null && !IsCompleted(CurrentRunningItem))
        //    {
        //        IsRunning = 4;
        //        UnlockAndEnableNextStep(CurrentRunningItem);
        //    }
        //    else
        //    {
        //        IsRunning = 1;
        //    }

        //    _drumConfig.UpdateCountdownDisplay(Timer, TotalTimer);
        //    StartEngine();
        //}
        public void StartAuto(int inputMinutes, int reverseIntervalMinutes = 0)
        {
            if (!ConfirmPlcConnection()) return;

            IsManualRunning = false;
            IsAutoMode = true;

            _reverseCounter = 0;
            ReversePlcTimer = ConvertTime(reverseIntervalMinutes);
            TotalTimer = Timer = ConvertTime(inputMinutes);

            // CHỈ ép "Hoàn tất" bước cũ NẾU bước đó ĐANG THỰC SỰ CHẠY HOẶC PAUSE (IsRunning = 1, 2, 4)
            if (CurrentRunningItem != null && !IsCompleted(CurrentRunningItem) && (IsRunning == 1 || IsRunning == 4))
            {
                IsRunning = 4; // User Override thời gian bước đang chạy
                UnlockAndEnableNextStep(CurrentRunningItem);
            }
            else
            {
                IsRunning = 1;
                if (CurrentRunningItem != null)
                {
                    CurrentRunningItem.IsRunning = 1;
                }
            }

            _drumConfig.UpdateCountdownDisplay(Timer, TotalTimer);
            StartEngine();
        }

        public void ResumeAuto()
        {
            if (IsRunning != 2) return;
            if (!ConfirmPlcConnection()) return;

            IsRunning = 1;
            if (CurrentRunningItem != null)
            {
                CurrentRunningItem.IsRunning = 1;
            }
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
                if (!isFirstActionFound && item.HasAction && !IsCompleted(item) && item.Status != "Hoàn tất")
                {
                    item.IsEnabled = true;
                    item.IsRunning = 0;
                    item.IsPaused = false;
                    isFirstActionFound = true;
                }
                else
                {
                    item.IsEnabled = false;
                    item.IsRunning = 0;
                }
            }
        }

        public void ToggleStepAction(ChemicalDetail clickedItem)
        {
            if (clickedItem == null || !clickedItem.HasAction) return;
            // 1. Đang CHẠY -> Bấm để TẠM DỪNG
            if (CurrentRunningItem == clickedItem && (IsRunning == 1 || IsRunning == 4))
            {
                IsRunning = 2;
                StopEngine();
                clickedItem.IsRunning = 2;
                return;
            }
            // 2. Đang TẠM DỪNG -> Bấm để TIẾP TỤC
            if (CurrentRunningItem == clickedItem && IsRunning == 2)
            {
                if (!ConfirmPlcConnection()) return;
                IsRunning = 1;
                StartEngine();
                clickedItem.IsRunning = 1;
                return;
            }
            if (!ConfirmPlcConnection()) return;
            if (IsDrumSpinning) StopEngine();
            IsAutoMode = true;
            IsManualRunning = false;
            CurrentRunningItem = clickedItem;
            foreach (var item in ChemicalList)
            {
                if (item == clickedItem)
                {
                    item.IsEnabled = true;
                    item.IsRunning = 0;
                }
                else
                {
                    item.IsEnabled = false;
                }
            }

            int runMinutes = clickedItem.DurationMinutes;
            _drumConfig.SetAutoModeUI(true, runMinutes);
            // --- KIỂM TRA ĐIỀU KIỆN ĐẢO CHIỀU ---
            if (_drumConfig.reverseTime < 1)
            {
                IsRunning = 0;
                MessageBox.Show($"Vui lòng nhập 'Thời gian đảo chiều' rồi bấm Bắt đầu!",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_drumConfig.reverseTime >= runMinutes)
            {
                IsRunning = 0;
                MessageBox.Show($"Thời gian đảo chiều phải nhỏ hơn thời gian chạy!",
                                "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            IsRunning = 1;
            clickedItem.IsRunning = 1;
            TotalTimer = Timer = ConvertTime(runMinutes);
            ReversePlcTimer = ConvertTime(_drumConfig.reverseTime);
            _reverseCounter = 0;

            _drumConfig.UpdateCountdownDisplay(Timer, TotalTimer);
            StartEngine();
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
            if (Timer > 0)
            {
                Timer--;
                _drumConfig.UpdateCountdownDisplay(Timer, TotalTimer);

                if (Timer == 0)
                {
                    OnTimerFinished();
                    return;
                }

                if (ReversePlcTimer > 0)
                {
                    _reverseCounter++;
                    if (_reverseCounter >= ReversePlcTimer)
                    {
                        _reverseCounter = 0;
                        ReverseDrum();
                    }
                }
            }
            else
            {
                OnTimerFinished();
            }
        }

        private void OnTimerFinished()
        {
            _reverseCounter = 0;
            StopEngine();

            if (IsRunning == 1 && CurrentRunningItem != null)
            {
                UnlockAndEnableNextStep(CurrentRunningItem);
            }

            IsRunning = 3;
        }

        public void StopCurrentStep()
        {
            IsRunning = 2;
            if (CurrentRunningItem != null)
            {
                CurrentRunningItem.IsRunning = 2;
            }
            StopEngine();
        }

        public void StopOrder()
        {
            StopEngine();
            IsRunning = 0;
            IsManualRunning = false;
            Timer = TotalTimer = 0;
            _reverseCounter = 0;
            ReversePlcTimer = 0;
            CurrentRunningItem = null;
            _completedItems.Clear();

            _drumConfig.ResetCountdownDisplay();
            ClearOrderHeaderInfo();

            if (_dataGridView?.MainDataGrid != null)
            {
                _dataGridView.MainDataGrid.Visibility = Visibility.Collapsed;
                _dataGridView.MainDataGrid.ItemsSource = null;
                if (_dataGridView.TxtNoOrderMessage != null)
                    _dataGridView.TxtNoOrderMessage.Visibility = Visibility.Visible;
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
            if (ChemicalList == null || currentItem == null) return;

            currentItem.Status = "Hoàn tất";
            currentItem.IsRunning = 3;
            _completedItems.Add(currentItem);

            int currentIndex = ChemicalList.IndexOf(currentItem);
            if (currentIndex < 0) return;

            for (int i = 0; i <= currentIndex; i++)
            {
                ChemicalList[i].IsEnabled = false;
            }

            for (int i = currentIndex + 1; i < ChemicalList.Count; i++)
            {
                var nextItem = ChemicalList[i];
                if (nextItem.HasAction && nextItem.Status != "Hoàn tất")
                {
                    nextItem.IsEnabled = true;
                    nextItem.IsRunning = 0;
                }
            }

            CurrentRunningItem = null;
        }
    }
}