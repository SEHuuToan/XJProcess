using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using XJProcess.modal;
using XJProcess.service;
using XJProcess.view;

namespace XJProcess.services
{
    public class MainService
    {
        private HeaderConfig? _headerConfig;
        private DrumConfig? _drumConfig;
        private DataGridView? _dataGridView;

        private readonly DispatcherTimer _stepTimer;

        // Trạng thái vận hành tập trung
        public ObservableCollection<ChemicalDetail> ChemicalList { get; private set; } = new ObservableCollection<ChemicalDetail>();
        public ChemicalDetail? CurrentRunningItem { get; set; }
        public ChemicalHeader? CurrentOrder { get; set; }

        public bool IsManualRunning { get; set; }
        public int ManualRemainingSeconds { get; set; }
        public int ManualTotalSeconds { get; set; }

        public MainService()
        {
            _stepTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _stepTimer.Tick += StepTimer_Tick;
        }

        public void RegisterViews(HeaderConfig headerConfig, DrumConfig drumConfig, DataGridView dataGridView)
        {
            _headerConfig = headerConfig;
            _drumConfig = drumConfig;
            _dataGridView = dataGridView;

            if (_drumConfig != null)
            {
                _drumConfig.ManualStartRequested += (s, minutes) => StartManualRun(minutes);
                _drumConfig.StartRequested += (s, e) => ResumeOrStartCurrentStep();
                _drumConfig.StopRequested += (s, e) => StopCurrentStep();
            }
        }

        #region Order & Data Logic

        public void LoadOrderData(ChemicalHeader currentOrder)
        {
            CurrentOrder = currentOrder;
            if (_headerConfig != null)
            {
                _headerConfig.CurrentOrder = currentOrder;
            }

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

                if (_dataGridView != null)
                {
                    _dataGridView.ChemicalList = ChemicalList;
                    _dataGridView.MainDataGrid.ItemsSource = ChemicalList;
                    _dataGridView.MainDataGrid.Visibility = System.Windows.Visibility.Visible;
                    if (_dataGridView.TxtNoOrderMessage != null)
                        _dataGridView.TxtNoOrderMessage.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        public void ProcessGroupHeaders(ObservableCollection<ChemicalDetail> list)
        {
            if (list == null) return;
            string? lastStep = null;
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

        #endregion

        #region Step & Drum Operations

        public void ToggleStepAction(ChemicalDetail clickedItem)
        {
            if (clickedItem == null) return;

            if (clickedItem.IsRunning)
            {
                clickedItem.IsRunning = false;
                clickedItem.IsPaused = true;
                StopStepTimer();
                _drumConfig?.StopDrumSpin();
            }
            else
            {
                if (CurrentRunningItem != null && CurrentRunningItem != clickedItem)
                {
                    CurrentRunningItem.IsRunning = false;
                    CurrentRunningItem.IsPaused = true;
                }

                CurrentRunningItem = clickedItem;
                CurrentRunningItem.IsRunning = true;
                CurrentRunningItem.IsPaused = false;

                if (CurrentRunningItem.RemainingSeconds <= 0)
                {
                    CurrentRunningItem.RemainingSeconds = CurrentRunningItem.DurationMinutes * 60;
                }

                if (ChemicalList != null)
                {
                    foreach (var item in ChemicalList)
                    {
                        if (item.HasAction) item.IsEnabled = (item == clickedItem);
                    }
                }

                _drumConfig?.StartDrumSpin();
                StartStepTimer();
            }
        }

        public void StartManualRun(int inputMinutes)
        {
            IsManualRunning = true;
            ManualTotalSeconds = inputMinutes * 60;
            ManualRemainingSeconds = ManualTotalSeconds;

            if (CurrentRunningItem != null)
            {
                CurrentRunningItem.IsRunning = false;
                CurrentRunningItem = null;
            }

            _drumConfig?.StartDrumSpin();
            StartStepTimer();
        }

        public void ResumeOrStartCurrentStep()
        {
            if (CurrentRunningItem != null)
            {
                ToggleStepAction(CurrentRunningItem);
            }
        }

        public void StopCurrentStep()
        {
            if (CurrentRunningItem != null)
            {
                CurrentRunningItem.IsRunning = false;
                CurrentRunningItem.IsPaused = true;
            }
            StopStepTimer();
            _drumConfig?.StopDrumSpin();
        }

        public void StopOrder()
        {
            _drumConfig?.StopDrumSpin();
            StopStepTimer();

            IsManualRunning = false;
            ManualRemainingSeconds = 0;
            ManualTotalSeconds = 0;

            if (CurrentRunningItem != null)
            {
                CurrentRunningItem.IsRunning = false;
                CurrentRunningItem = null;
            }

            _drumConfig?.ResetCountdownDisplay();

            if (ChemicalList != null)
            {
                foreach (var item in ChemicalList)
                {
                    if (item.HasAction) item.IsEnabled = true;
                }
            }

            ClearOrderHeaderInfo();

            if (_dataGridView != null)
            {
                if (_dataGridView.MainDataGrid != null)
                {
                    _dataGridView.MainDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                    _dataGridView.MainDataGrid.ItemsSource = null;
                }
                if (_dataGridView.TxtNoOrderMessage != null)
                {
                    _dataGridView.TxtNoOrderMessage.Visibility = System.Windows.Visibility.Visible;
                }
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

        #endregion

        #region Timer & Step Transition Logic

        public void StartStepTimer()
        {
            if (!_stepTimer.IsEnabled) _stepTimer.Start();
        }

        public void StopStepTimer()
        {
            if (_stepTimer.IsEnabled) _stepTimer.Stop();
        }

        private void StepTimer_Tick(object? sender, EventArgs e)
        {
            if (IsManualRunning)
            {
                if (ManualRemainingSeconds > 0)
                {
                    ManualRemainingSeconds--;
                    _drumConfig?.UpdateCountdownDisplay(ManualRemainingSeconds, ManualTotalSeconds);
                }
                else
                {
                    IsManualRunning = false;
                    _stepTimer.Stop();
                    _drumConfig?.StopDrumSpin();
                }
                return;
            }

            if (CurrentRunningItem != null && CurrentRunningItem.IsRunning)
            {
                if (CurrentRunningItem.RemainingSeconds > 0)
                {
                    CurrentRunningItem.RemainingSeconds--;
                    _drumConfig?.UpdateCountdownDisplay(CurrentRunningItem.RemainingSeconds);
                }
                else
                {
                    CurrentRunningItem.IsRunning = false;
                    _stepTimer.Stop();
                    _drumConfig?.StopDrumSpin();
                    UnlockAndEnableNextStep(CurrentRunningItem);
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

        #endregion
    }
}