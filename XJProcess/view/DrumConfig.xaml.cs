using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using XJProcess.service;
using XJProcess.services;

namespace XJProcess.view
{
    public partial class DrumConfig : UserControl
    {
        private readonly View3DService _view3DService = new View3DService();
        private readonly AxisAngleRotation3D _spinnerRotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);

        // Biến kiểm soát việc thay đổi Text từ phía người dùng hay từ Code
        private bool _isRunTimeInputChanged = false;
        private bool _isProgrammaticChange = false;

        public int reverseTime = 0;
        private MainService? _service;
        public MainService? Service
        {
            get => _service;
            set
            {
                _service = value;
                if (_service != null)
                {
                    // Đồng bộ mặc định ban đầu giữa Service và UI (false = Xoay Xuống)
                    _service.IsForwardDirection = false;
                    UpdateDirectionDisplay(false);
                }
            }
        }

        public DrumConfig()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            Init3DScene();

            // Đăng ký sự kiện theo dõi khi người dùng tự gõ/chỉnh sửa thời gian
            if (TxtRunTimeInput != null)
            {
                TxtRunTimeInput.TextChanged += TxtRunTimeInput_TextChanged;
            }
            if (TxtReverseTimeInput != null)
            {
                TxtReverseTimeInput.TextChanged += TxtRunTimeInput_TextChanged;
            }

            UpdateDirectionDisplay(false);
            SetInputControlsState(false);
        }

        private void Init3DScene()
        {
            try
            {
                string objPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "3dmodel.obj");
                RotateTransform3D rotateTransform = new RotateTransform3D(_spinnerRotation);
                Model3DGroup loadedModel = _view3DService.Load3DModel(objPath, rotateTransform);
                ModelVisual3D modelVisual = new ModelVisual3D { Content = loadedModel };

                TankViewport.Children.Clear();
                TankViewport.Children.Add(new DefaultLights());
                TankViewport.Children.Add(modelVisual);
                TankViewport.Camera = _view3DService.GetFrontFlatCamera();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load mô hình 3D: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtRunTimeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Chỉ đánh dấu người dùng thao tác khi không phải là cập nhật tự động từ Code UI
            if (!_isProgrammaticChange)
            {
                _isRunTimeInputChanged = true;
                reverseTime = int.TryParse(TxtReverseTimeInput?.Text, out int rev) ? rev : 0;
            }
        }

        #region Render & Animation UI

        public void UpdateDirectionDisplay(bool isForward)
        {
            if (TxtButtonStatus != null)
            {
                TxtButtonStatus.Text = isForward ? "Xoay Lên" : "Xoay Xuống";
            }
        }

        public void StartSpinAnimation(bool isForward, bool isAutoMode)
        {
            ApplySpinAnimation(isForward);

            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = false;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = false;
            if (TglMode != null) TglMode.IsEnabled = false;

            if (isAutoMode)
            {
                Brush disabledBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                if (TxtRunTimeInput != null) { TxtRunTimeInput.IsEnabled = false; TxtRunTimeInput.Background = disabledBrush; }
                if (TxtReverseTimeInput != null) { TxtReverseTimeInput.IsEnabled = false; TxtReverseTimeInput.Background = disabledBrush; }
            }
        }

        public void StopSpinAnimation(bool isAutoMode)
        {
            double currentAngle = _spinnerRotation.Angle;
            _spinnerRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
            _spinnerRotation.Angle = currentAngle;

            bool isDataGridStepActive = Service?.CurrentRunningItem != null;
            bool disableRotate = isAutoMode || isDataGridStepActive;

            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = !disableRotate;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = !disableRotate;
            if (TglMode != null) TglMode.IsEnabled = true;

            if (isAutoMode)
            {
                if (TxtRunTimeInput != null) { TxtRunTimeInput.IsEnabled = true; TxtRunTimeInput.Background = Brushes.White; }
                if (TxtReverseTimeInput != null) { TxtReverseTimeInput.IsEnabled = true; TxtReverseTimeInput.Background = Brushes.White; }
            }

            if (Service != null)
            {
                UpdateDirectionDisplay(Service.IsForwardDirection);
            }
        }

        private void ApplySpinAnimation(bool isForward)
        {
            double currentAngle = _spinnerRotation.Angle;
            double targetAngle = isForward ? currentAngle - 360 : currentAngle + 360;

            var spinAnimation = new DoubleAnimation
            {
                To = targetAngle,
                Duration = TimeSpan.FromSeconds(3),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _spinnerRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, spinAnimation);
            UpdateDirectionDisplay(isForward);
        }

        public void SetAutoModeUI(bool isAuto, int minutes)
        {
            if (TglMode != null && TglMode.IsChecked != isAuto)
            {
                TglMode.IsChecked = isAuto;
            }
            if (TxtRunTimeInput != null)
            {
                _isProgrammaticChange = true;
                TxtRunTimeInput.Text = minutes.ToString();
                _isProgrammaticChange = false;
            }
        }

        public void UpdateCountdownDisplay(int remainingSeconds, int? customTotalSeconds = null)
        {
            int hours = remainingSeconds / 3600;
            int minutes = (remainingSeconds % 3600) / 60;
            int seconds = remainingSeconds % 60;

            if (TxtCountdownTime != null)
            {
                TxtCountdownTime.Text = hours > 0
                    ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
                    : $"{minutes:D2}:{seconds:D2}";
            }

            int totalSeconds = customTotalSeconds ?? 1;
            if (ProgressArc != null && totalSeconds > 0)
            {
                XJProcess.utils.SystemUtils.UpdateProgressArc(ProgressArc, remainingSeconds, totalSeconds);
            }

            // Khi đang chạy Auto (ô Input đang bị khóa): Tính số phút tròn còn lại bằng phép chia nguyên
            if (Service != null && Service.IsAutoMode && TxtRunTimeInput != null && !TxtRunTimeInput.IsEnabled)
            {
                int remainingMinutes = remainingSeconds / 60;

                _isProgrammaticChange = true;
                TxtRunTimeInput.Text = remainingMinutes.ToString();
                _isProgrammaticChange = false;
            }
        }

        public void ResetCountdownDisplay()
        {
            if (TxtCountdownTime != null) TxtCountdownTime.Text = "00:00";
            if (ProgressArc != null) XJProcess.utils.SystemUtils.UpdateProgressArc(ProgressArc, 0, 1);
        }

        public void UpdateScannedCode(string code)
        {
            var txt = FindName("txtScannedResult") as TextBlock ?? FindName("TxtScannedResult") as TextBlock;
            if (txt != null)
            {
                txt.Text = string.IsNullOrEmpty(code) ? "Chưa có dữ liệu..." : code;
            }
        }

        #endregion

        #region UI Handlers

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (Service == null) return;
            if (Service.IsDrumSpinning) return;
            if (Service.IsAutoMode)
            {
                // 1. Kiểm tra Thời gian vận hành (Phải là số nguyên >= 2 phút)
                if (!int.TryParse(TxtRunTimeInput?.Text, out int runMinutes) || runMinutes < 2)
                {
                    string msg = (runMinutes == 1)
                        ? "Thời gian vận hành 1 phút quá ngắn cho Auto! Vui lòng chuyển sang chế độ Thủ công (Manual)."
                        : "Vui lòng nhập thời gian vận hành hợp lệ (từ 2 phút trở lên)!";

                    MessageBox.Show(msg, "Cảnh báo vận hành", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // 2. Kiểm tra Thời gian đảo chiều (Bắt buộc nhập, >= 1 phút và < Thời gian vận hành)
                if (!int.TryParse(TxtReverseTimeInput?.Text, out int revMinutes) || revMinutes < 1)
                {
                    MessageBox.Show("Thời gian đảo chiều không được để trống và phải từ 1 phút trở lên!", "Cảnh báo cài đặt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (revMinutes >= runMinutes)
                {
                    MessageBox.Show($"Thời gian đảo chiều phải nhỏ hơn thời gian vận hành của bồn!", "Cảnh báo cài đặt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                reverseTime = revMinutes;
                SetAutoModeUI(true, runMinutes);
                if (Service.IsAutoPaused && !_isRunTimeInputChanged)
                {
                    Service.ResumeAuto();
                }
                else
                {
                    _isRunTimeInputChanged = false;
                    Service.StartAuto(runMinutes, reverseTime);
                }
            }
            else
            {
                Service.StartManual();
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Service?.StopCurrentStep();
        }

        private void BtnRotateUp_Click(object sender, RoutedEventArgs e)
        {
            if (Service == null || Service.IsAutoMode || Service.CurrentRunningItem != null || Service.IsDrumSpinning) return;

            Service.ToggleDirection(true);
            UpdateDirectionDisplay(true);
        }

        private void BtnRotateDown_Click(object sender, RoutedEventArgs e)
        {
            if (Service == null || Service.IsAutoMode || Service.CurrentRunningItem != null || Service.IsDrumSpinning) return;

            Service.ToggleDirection(false);
            UpdateDirectionDisplay(false);
        }

        private void TglMode_Checked(object sender, RoutedEventArgs e)
        {
            if (TxtRunTimeInput != null) TxtRunTimeInput.Text = string.Empty;
            if (Service != null)
            {
                Service.IsAutoMode = true;
                Service.SetDataGridEnabledState(true);
            }
            SetInputControlsState(true);
            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = false;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = false;
        }

        private void TglMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Service != null)
            {
                Service.IsAutoMode = false;
                bool isProcessRunningOrPaused = Service.IsDrumSpinning || Service.IsAutoPaused;
                Service.SetDataGridEnabledState(!isProcessRunningOrPaused);
            }
            SetInputControlsState(false);
            bool isStepActive = Service?.CurrentRunningItem != null;
            bool isSpinning = Service?.IsDrumSpinning ?? false;
            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = !isStepActive && !isSpinning;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = !isStepActive && !isSpinning;
        }

        private void SetInputControlsState(bool isAuto)
        {
            Brush bgBrush = isAuto ? Brushes.White : new SolidColorBrush(Color.FromRgb(220, 220, 220));

            if (TxtRunTimeInput != null)
            {
                TxtRunTimeInput.IsEnabled = isAuto;
                TxtRunTimeInput.Background = bgBrush;
            }

            if (TxtReverseTimeInput != null)
            {
                TxtReverseTimeInput.IsEnabled = isAuto;
                TxtReverseTimeInput.Background = bgBrush;
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^[0-9]+$")) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        #endregion
    }
}