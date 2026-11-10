using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using XJProcess.modal;
using XJProcess.service;
using XJProcess.Services;
using XJProcess.ultis;
using XJProcess.view;

namespace XJProcess.services
{
    public class MainService
    {
        private HeaderConfigLeft _headerConfigLeft;
        private DrumConfig _drumConfig;
        private DataGridView _dataGridView;
        private readonly ApiService _apiService;
        private readonly ChemicalService _chemicalService;
        private readonly DispatcherTimer _stepTimer;
        private readonly HashSet<ChemicalDetail> _completedItems = new();
        public string CurrentBillNo { get; set; } = string.Empty;
        public string CurrentBillId { get; set; } = string.Empty;
        public int IsRunning { get; set; } = 0;
        public int Timer { get; set; }
        public int TotalTimer { get; set; }
        public int ReversePlcTimer { get; set; }
        private int _reverseCounter = 0;
        public bool IsPlcConnected { get; set; } = false;
        public bool IsDrumSpinning => IsRunning == 1 || IsRunning == 4;
        public bool IsSpinUp { get; set; } = true;
        public bool IsAutoMode { get; set; }
        public bool IsAutoPaused => IsRunning == 2;
        public ObservableCollection<ChemicalDetail> ChemicalList { get; private set; } = new ObservableCollection<ChemicalDetail>();
        public ChemicalDetail? CurrentRunningItem { get; set; }
        public ChemicalData? CurrentOrder { get; set; }
        public bool IsManualRunning { get; set; } = true;
        public MainService()
        {
            _apiService = new ApiService();
            _stepTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _stepTimer.Tick += StepTimer_Tick;
            _chemicalService = new ChemicalService(_apiService);
        }
        public async Task ConnectPlcAsync(string ipAddress)
        {
            while (true)
            {
                IsPlcConnected = await Task.Run(() => PlcUtils.ConnectPlc(ipAddress));
                if (IsPlcConnected)
                {
                    CheckPlcRunningOnStartup();
                    break;
                }
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
        public void RegisterViews(HeaderConfigLeft headerConfigLeft, DrumConfig drumConfig, DataGridView dataGridView)
        {
            _headerConfigLeft = headerConfigLeft;
            _drumConfig = drumConfig;
            _dataGridView = dataGridView;

            if (_headerConfigLeft != null) _headerConfigLeft.Service = this;
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
            SettingView.OnSettingsSaved -= OnSettingsSaved_Handler;
            SettingView.OnSettingsSaved += OnSettingsSaved_Handler;
            _ = TryAutoRestoreOnStartupAsync();
        }
        private void OnSettingsSaved_Handler()
        {
            var settings = SettingService.LoadSettings();
            bool isReverseConfig = settings?.IsReverse ?? false;
            IsSpinUp = isReverseConfig;
            Application.Current.Dispatcher.Invoke(() =>
            {
                _drumConfig?.UpdateDirectionDisplay(IsSpinUp);
            });
            if (IsDrumSpinning)
            {
                bool actualSpinUp = isReverseConfig ? !IsSpinUp : IsSpinUp;

                if (actualSpinUp)
                {
                    PlcUtils.Up();
                }
                else
                {
                    PlcUtils.Down();
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _drumConfig?.StartSpinAnimation(IsSpinUp, IsAutoMode);
                });
            }
        }
        public int ConvertTime(int minutes) => minutes * 60;
        private void StartEngine()
        {
            var settings = SettingService.LoadSettings();
            bool isReverseConfig = settings?.IsReverse ?? false;
            bool actualSpinUp = isReverseConfig ? !IsSpinUp : IsSpinUp;
            if (actualSpinUp)
            {
                PlcUtils.Up();
            }
            else
            {
                PlcUtils.Down();
            }
            PlcUtils.Start();
            if (IsDrumSpinning)
            {
                _drumConfig.StartSpinAnimation(IsSpinUp, IsAutoMode);

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
            IsSpinUp = !IsSpinUp;
            var settings = SettingService.LoadSettings();
            bool isReverseConfig = settings?.IsReverse ?? false;
            bool actualSpinUp = isReverseConfig ? !IsSpinUp : IsSpinUp;
            if (actualSpinUp)
            {
                PlcUtils.Up();
            }
            else
            {
                PlcUtils.Down();
            }
            PlcUtils.Reverse(true);
            _drumConfig.StartSpinAnimation(IsSpinUp, IsAutoMode);
        }
        public void ToggleDirection(bool isForward)
        {
            if (!ConfirmPlcConnection()) return;
            IsSpinUp = isForward;
            var settings = SettingService.LoadSettings();
            bool isReverseConfig = settings?.IsReverse ?? false;
            bool actualSpinUp = isReverseConfig ? !IsSpinUp : IsSpinUp;
            if (IsDrumSpinning)
            {
                if (actualSpinUp) PlcUtils.Up();
                else PlcUtils.Down();
                PlcUtils.Reverse(true);
                _drumConfig.StartSpinAnimation(IsSpinUp, IsAutoMode);
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
        public void StartAuto(int inputMinutes, int reverseIntervalMinutes = 0)
        {
            if (!ConfirmPlcConnection()) return;
            IsManualRunning = false;
            IsAutoMode = true;
            _reverseCounter = 0;
            ReversePlcTimer = ConvertTime(reverseIntervalMinutes);
            TotalTimer = Timer = ConvertTime(inputMinutes);
            if (CurrentRunningItem != null && !IsCompleted(CurrentRunningItem) && (IsRunning == 1 || IsRunning == 4))
            {
                IsRunning = 4;
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
            if (CurrentOrder != null && !string.IsNullOrEmpty(CurrentOrder.OrderCode))
            {
                LogService.SaveRunningState(this);
            }
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
            for (int i = 0; i < ChemicalList.Count; i++)
            {
                var item = ChemicalList[i];
                if (item == CurrentRunningItem)
                {
                    item.IsEnabled = true;
                }
                else if (item.Status == "Hoàn tất" || _completedItems.Contains(item))
                {
                    item.IsEnabled = false;
                }
                else
                {
                    bool isAboveRunningOrCompleted = false;
                    if (CurrentRunningItem != null)
                    {
                        int currentIndex = ChemicalList.IndexOf(item);
                        int runningIndex = ChemicalList.IndexOf(CurrentRunningItem);
                        if (currentIndex < runningIndex)
                        {
                            isAboveRunningOrCompleted = true;
                        }
                    }
                    foreach (var completed in _completedItems)
                    {
                        if (ChemicalList.IndexOf(item) < ChemicalList.IndexOf(completed))
                        {
                            isAboveRunningOrCompleted = true;
                            break;
                        }
                    }
                    if (isAboveRunningOrCompleted)
                    {
                        item.IsEnabled = false;
                    }
                    else
                    {
                        // Các bước phía dưới -> Chỉ mở nếu có action
                        if (item.HasAction && item.Status != "Hoàn tất")
                        {
                            item.IsEnabled = true;
                            item.IsRunning = 0;
                            item.IsPaused = false;
                        }
                        else
                        {
                            item.IsEnabled = false;
                            item.IsRunning = 0;
                        }
                    }
                }
            }
        }
        public void ToggleStepAction(ChemicalDetail clickedItem)
        {
            if (clickedItem == null || !clickedItem.HasAction) return;
            if (CurrentRunningItem == clickedItem && (IsRunning == 1 || IsRunning == 4))
            {
                IsRunning = 2;
                StopEngine();
                clickedItem.IsRunning = 2;
                InitializeStepStates();
                if (CurrentOrder != null && !string.IsNullOrEmpty(CurrentOrder.OrderCode))
                {
                    LogService.SaveRunningState(this);
                }
                return;
            }
            if (CurrentRunningItem == clickedItem && IsRunning == 2)
            {
                if (!ConfirmPlcConnection()) return;
                IsRunning = 1;
                StartEngine();
                clickedItem.IsRunning = 1;
                foreach (var item in ChemicalList)
                {
                    item.IsEnabled = (item == clickedItem);
                }
                if (Timer <= 0)
                {
                    int runMinutes = clickedItem.DurationMinutes;
                    TotalTimer = Timer = ConvertTime(runMinutes);
                }
                _drumConfig?.UpdateCountdownDisplay(Timer, TotalTimer);
                if (CurrentOrder != null && !string.IsNullOrEmpty(CurrentOrder.OrderCode))
                {
                    LogService.SaveRunningState(this);
                }
                return;
            }
            if (IsDrumSpinning) return;
            if (!ConfirmPlcConnection()) return;
            if (CurrentRunningItem != null && CurrentRunningItem != clickedItem)
            {
                CurrentRunningItem.IsRunning = 0;
                CurrentRunningItem = null;
            }
            IsAutoMode = true;
            IsManualRunning = false;
            CurrentRunningItem = clickedItem;
            foreach (var item in ChemicalList)
            {
                item.IsEnabled = (item == clickedItem);
            }
            int normalRunMinutes = clickedItem.DurationMinutes;
            _drumConfig.SetAutoModeUI(true, normalRunMinutes);
            int revTime = _drumConfig.reverseTime;
            if (revTime < 1)
            {
                var settings = SettingService.LoadSettings();
                revTime = settings.ReverseTime;
                _drumConfig?.SetReverseTimeUI(revTime);
            }
            if (revTime < 1)
            {
                IsRunning = 0;
                MessageBox.Show($"Vui lòng nhập 'Thời gian đảo chiều' trong phần cài đặt hoặc trên giao diện rồi bấm Bắt đầu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                InitializeStepStates();
                return;
            }
            if (revTime >= normalRunMinutes)
            {
                IsRunning = 0;
                MessageBox.Show($"Thời gian đảo chiều phải nhỏ hơn thời gian chạy!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                InitializeStepStates();
                return;
            }
            IsRunning = 1;
            clickedItem.IsRunning = 1;
            if (Timer <= 0 || CurrentRunningItem != clickedItem)
            {
                TotalTimer = Timer = ConvertTime(normalRunMinutes);
            }
            ReversePlcTimer = ConvertTime(revTime);
            _reverseCounter = 0;
            _drumConfig.UpdateCountdownDisplay(Timer, TotalTimer);
            StartEngine();
            if (CurrentOrder != null && !string.IsNullOrEmpty(CurrentOrder.OrderCode))
            {
                LogService.SaveRunningState(this);
            }
        }
        private void StepTimer_Tick(object? sender, EventArgs e)
        {
            bool hasActiveOrder = CurrentOrder != null && !string.IsNullOrEmpty(CurrentOrder.OrderCode);
            if (Timer > 0)
            {
                Timer--;
                _drumConfig.UpdateCountdownDisplay(Timer, TotalTimer);
                if (hasActiveOrder)
                {
                    LogService.SaveRunningState(this);
                }
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
            LogService.SaveRunningState(this);
        }
        public void StopCurrentStep()
        {
            IsRunning = 2;
            if (CurrentRunningItem != null)
            {
                CurrentRunningItem.IsRunning = 2;
            }
            StopEngine();
            if (CurrentOrder != null && !string.IsNullOrEmpty(CurrentOrder.OrderCode))
            {
                LogService.SaveRunningState(this);
            }
        }
        public void StopOrder()
        {
            if (CurrentOrder != null && ChemicalList != null)
            {
                string summary = string.Join("\n", ChemicalList.Select(s => $"Step {s.Sn} - {s.Name}: {s.Status} (Thời gian: {s.DurationMinutes} phút)"));
                LogService.SaveOrderHistory(CurrentOrder, summary);
            }
            LogService.ClearRunningState();
            StopEngine();
            IsRunning = 0;
            IsManualRunning = false;
            Timer = TotalTimer = 0;
            _reverseCounter = 0;
            ReversePlcTimer = 0;
            CurrentRunningItem = null;
            CurrentOrder = null;
            _completedItems.Clear();
            _drumConfig?.ResetCountdownDisplay();
            _drumConfig?.ClearInputs();
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
            if (_headerConfigLeft != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _headerConfigLeft.txtOrderCode.Text = string.Empty;
                    _headerConfigLeft.TxtTechnician.Text = string.Empty;
                    _headerConfigLeft.TxtOperator.Text = string.Empty;
                    _headerConfigLeft.TxtWeight.Text = string.Empty;
                    _headerConfigLeft.TxtDrumStatus.Text = "Sẵn sàng";
                });
            }
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
        public void SetDataGridEnabledState(bool isEnabled)
        {
            if (_dataGridView.MainDataGrid != null)
            {
                _dataGridView.MainDataGrid.IsEnabled = isEnabled;
            }
        }
        public string SplitQRValue(string scannedResult)
        {
            if (!scannedResult.Contains("6000CFDH"))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Mã QR không hợp lệ! Vui lòng quét đúng đơn bồn quay!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return null;
            }
            var parts = scannedResult.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                CurrentBillNo = parts[0].Trim();
                CurrentBillId = parts[1].Trim();
                return CurrentBillId;
            }
            else if (parts.Length == 1)
            {
                CurrentBillNo = parts[0].Trim();
                return CurrentBillNo;
            }
            return null;
        }
        public async Task<ChemicalData?> ProcessQrCodeAsync(string billCode)
        {
            if (string.IsNullOrEmpty(billCode)) return null;
            if (CurrentOrder != null && !string.IsNullOrEmpty(CurrentOrder.OrderCode))
            {
                if (billCode != CurrentOrder.OrderCode && !billCode.Contains(CurrentOrder.OrderCode))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("Nếu muốn quét đơn khác vui lòng Hủy đơn hiện tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return null;
                }
            }
            var chemicalData = await _apiService.LoadDataChemical(billCode);
            CurrentOrder = chemicalData;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ChemicalList.Clear();
                if (chemicalData != null && chemicalData.List != null && chemicalData.List.Count > 0)
                {
                    foreach (var item in chemicalData.List)
                    {
                        ChemicalList.Add(item);
                    }
                    InitializeStepStates();
                    if (_dataGridView?.MainDataGrid != null)
                    {
                        _dataGridView.MainDataGrid.Visibility = Visibility.Visible;
                        _dataGridView.MainDataGrid.ItemsSource = ChemicalList;
                        if (_dataGridView.TxtNoOrderMessage != null)
                            _dataGridView.TxtNoOrderMessage.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    MessageBox.Show($"Không tìm thấy dữ liệu hóa chất cho mã đơn: {billCode}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
            return chemicalData;
        }
        private async Task TryAutoRestoreOnStartupAsync()
        {
            try
            {
                var savedState = LogService.LoadRunningState();
                if (savedState == null || (string.IsNullOrEmpty(savedState.BillId) && string.IsNullOrEmpty(savedState.OrderCode))) return;
                var chemicalData = await _apiService.LoadDataChemical(savedState.BillId);
                if ((chemicalData == null || chemicalData.List == null || chemicalData.List.Count == 0) && !string.IsNullOrEmpty(savedState.OrderCode))
                {
                    chemicalData = await _apiService.LoadDataChemical(savedState.OrderCode);
                }
                if (chemicalData == null || chemicalData.List == null || chemicalData.List.Count == 0)
                {
                    MessageBox.Show($"Không thể tải lại dữ liệu đơn hàng từ API khi khôi phục!", "Lỗi khôi phục", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                CurrentOrder = chemicalData;
                CurrentBillNo = savedState.BillNo;
                CurrentBillId = savedState.BillId;
                IsAutoMode = true;
                IsManualRunning = false;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (_headerConfigLeft != null)
                    {
                        _headerConfigLeft.SetData(chemicalData);
                    }
                    ChemicalList.Clear();
                    foreach (var item in chemicalData.List)
                    {
                        ChemicalList.Add(item);
                    }
                    int elapsedOfflineSeconds = (int)(DateTime.Now - savedState.SavedAt).TotalSeconds;
                    for (int i = 0; i < ChemicalList.Count && i < savedState.Steps.Count; i++)
                    {
                        var targetStep = ChemicalList[i];
                        var snapStep = savedState.Steps[i];
                        targetStep.Status = snapStep.Status;
                        if (snapStep.IsCompleted || snapStep.Status == "Hoàn tất")
                        {
                            targetStep.IsEnabled = false;
                            targetStep.IsRunning = 3;
                            _completedItems.Add(targetStep);
                        }
                    }
                    if (savedState.CurrentStepIndex >= 0 && savedState.CurrentStepIndex < ChemicalList.Count)
                    {
                        var runningStep = ChemicalList[savedState.CurrentStepIndex];
                        int adjustedRemainingSeconds = savedState.RemainingSeconds - elapsedOfflineSeconds;
                        TotalTimer = savedState.TotalSeconds;
                        bool isPlcRunning = PlcUtils.IsPlcRunning();

                        if (adjustedRemainingSeconds > 0)
                        {
                            CurrentRunningItem = runningStep;
                            Timer = adjustedRemainingSeconds;
                            TotalTimer = savedState.TotalSeconds;
                            if (isPlcRunning)
                            {
                                IsRunning = 1;
                                runningStep.IsRunning = 1;
                                IsAutoMode = true;
                                IsManualRunning = false;

                                _drumConfig?.SetAutoModeUI(true, runningStep.DurationMinutes);
                                _drumConfig?.UpdateCountdownDisplay(Timer, TotalTimer);
                                _drumConfig?.StartSpinAnimation(IsSpinUp, true);

                                if (!_stepTimer.IsEnabled)
                                {
                                    _stepTimer.Start();
                                }
                            }
                            else
                            {
                                var result = MessageBox.Show(
                                    $"Phát hiện step đang chạy dở dang: [Step {runningStep.Sn}]\n" +
                                    $"Thời gian còn lại: {Timer / 60} phút {Timer % 60} giây.\n\n" +
                                    $"Bạn có muốn tiếp tục tiến trình hiện tại hay không?\n",
                                    "Khôi phục tiến trình bồn quay",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question);
                                if (result == MessageBoxResult.Yes)
                                {
                                    runningStep.IsRunning = 2; // Chờ bấm Tiếp tục
                                    _drumConfig?.SetAutoModeUI(true, runningStep.DurationMinutes);
                                    _drumConfig?.UpdateCountdownDisplay(Timer, TotalTimer);
                                }
                                else
                                {
                                    runningStep.Status = "Hoàn tất";
                                    runningStep.IsRunning = 3;
                                    _completedItems.Add(runningStep);
                                    CurrentRunningItem = null;
                                    Timer = TotalTimer = 0;
                                    _drumConfig?.ResetCountdownDisplay();
                                    LogService.SaveRunningState(this);
                                }
                            }
                        }
                        else
                        {
                            if (isPlcRunning)
                            {
                                PlcUtils.Stop();
                                MessageBox.Show($"Cảnh báo: Step {runningStep.Sn} đã quá thời gian chạy trong lúc mất kết nối! Hệ thống đã tự động dừng.",  "An toàn vận hành", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            runningStep.Status = "Hoàn tất";
                            runningStep.IsRunning = 3;
                            _completedItems.Add(runningStep);
                            CurrentRunningItem = null;
                            Timer = TotalTimer = 0;
                            LogService.ClearRunningState();
                        }
                    }
                    InitializeStepStates();
                    if (_dataGridView?.MainDataGrid != null)
                    {
                        _dataGridView.MainDataGrid.Visibility = Visibility.Visible;
                        _dataGridView.MainDataGrid.ItemsSource = ChemicalList;
                        if (_dataGridView.TxtNoOrderMessage != null)
                            _dataGridView.TxtNoOrderMessage.Visibility = Visibility.Collapsed;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tự động khôi phục: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void RefreshSettings()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _headerConfigLeft?.LoadConfig();
            });
        }
        public void CheckPlcRunningOnStartup()
        {
            try
            {
                if (PlcUtils.IsPlcRunning())
                {
                    IsRunning = 1;
                    IsManualRunning = true;
                    IsAutoMode = false;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_drumConfig != null)
                        {
                            _drumConfig.StartSpinAnimation(IsSpinUp, false);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi check trạng thái PLC khi khởi động: " + ex.Message);
            }
        }
        public static void ChangeAppLanguage(string langCode)
        {
            try
            {
                string uriPath = langCode == "zh" ? "Languages/StringResources.zh.xaml" : "Languages/StringResources.vi.xaml";
                var newDict = new System.Windows.ResourceDictionary
                {
                    Source = new Uri(uriPath, UriKind.Relative)
                };

                var mergedDictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
                System.Windows.ResourceDictionary langDict = null;

                foreach (var dict in mergedDictionaries)
                {
                    if (dict.Source != null && dict.Source.OriginalString.Contains("StringResources"))
                    {
                        langDict = dict;
                        break;
                    }
                }

                if (langDict != null)
                {
                    int index = mergedDictionaries.IndexOf(langDict);
                    mergedDictionaries[index] = newDict;
                }
                else
                {
                    mergedDictionaries.Add(newDict);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi đổi ngôn ngữ: {ex.Message}");
            }
        }
        public static class AutoUpdater
        {
            public static bool CheckFormUpdate(string localVersion)
            {
                try
                {
                    var client = new WebClient();
                    client.Credentials = new NetworkCredential("huutoan", "(3:K,b8z");

                    string json = client.DownloadString("ftp://192.168.11.99/homes/appBonQuay/version.json");
                    JObject data = JObject.Parse(json);
                    string serverVersion = (string)data["version"];
                    string updateUrl = (string)data["updateUrl"];

                    if (serverVersion != localVersion)
                    {
                        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update_url.txt"), updateUrl);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi check update: " + ex.Message);
                }

                return false;
            }
        }
    }
}