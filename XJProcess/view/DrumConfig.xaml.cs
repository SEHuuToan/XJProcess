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

        public MainService? Service { get; set; }

        public DrumConfig()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            Init3DScene();

            if (TxtButtonStatus != null) TxtButtonStatus.Text = "Xoay Xuống";

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

        #region Render & Animation UI

        public void StartSpinAnimation(bool isForward, bool isAutoMode)
        {
            ApplySpinAnimation(isForward);
            // KHI BỒN ĐANG QUAY: Luôn khóa 2 nút xoay (cả Auto lẫn Thủ công)
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

            // KHI BỒN DỪNG: Chỉ mở lại 2 nút xoay nếu KHÔNG ở AutoMode VÀ KHÔNG có bước DataGrid nào đang chạy
            bool isDataGridStepActive = Service?.CurrentRunningItem != null;
            bool disableRotate = isAutoMode || isDataGridStepActive;

            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = !disableRotate;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = !disableRotate;
            if (TglMode != null) TglMode.IsEnabled = true;

            if (isAutoMode && (TglMode?.IsChecked ?? false))
            {
                if (TxtRunTimeInput != null) { TxtRunTimeInput.IsEnabled = true; TxtRunTimeInput.Background = Brushes.White; }
                if (TxtReverseTimeInput != null) { TxtReverseTimeInput.IsEnabled = true; TxtReverseTimeInput.Background = Brushes.White; }
            }
        }

        private void ApplySpinAnimation(bool isForward)
        {
            double currentAngle = _spinnerRotation.Angle;
            double targetAngle = isForward ? currentAngle + 360 : currentAngle - 360;

            var spinAnimation = new DoubleAnimation
            {
                To = targetAngle,
                Duration = TimeSpan.FromSeconds(3),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _spinnerRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, spinAnimation);

            if (TxtButtonStatus != null)
            {
                TxtButtonStatus.Text = isForward ? "Xoay Lên" : "Xoay Xuống";
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

            // 1. Ưu tiên chạy tự động datagridview
            if (Service.CurrentRunningItem != null)
            {
                if (Service.IsAutoMode)
                {
                    MessageBox.Show("Bạn đang trong một tiến trình, không thể chạy tự động lúc này",
                                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!Service.IsDrumSpinning)
                {
                    Service.ToggleStepAction(Service.CurrentRunningItem);
                }
                return;
            }

            // 2. Chạy tự động (Auto Mode)
            if (Service.IsAutoMode)
            {
                if (TxtRunTimeInput == null || !int.TryParse(TxtRunTimeInput.Text, out int runMinutes) || runMinutes <= 0)
                {
                    MessageBox.Show("Vui lòng nhập thời gian vận hành hợp lệ (số phút > 0)!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int reverseTime = 0;
                if (TxtReverseTimeInput != null && int.TryParse(TxtReverseTimeInput.Text, out reverseTime) && reverseTime > 0)
                {
                    if (reverseTime >= runMinutes)
                    {
                        MessageBox.Show("Thời gian đảo chiều phải nhỏ hơn thời gian vận hành!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (Service.IsAutoPaused)
                {
                    if (Service.ConvertTime(runMinutes) == Service.TotalTimer)
                    {
                        Service.ResumeAuto();
                    }
                    else
                    {
                        Service.StartAuto(runMinutes, reverseTime);
                    }
                    return;
                }
                Service.StartAuto(runMinutes, reverseTime);
            }
            // 3. Chạy thủ công (Manual Mode)
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
            if (TxtButtonStatus != null) TxtButtonStatus.Text = "Xoay Lên";
        }

        private void BtnRotateDown_Click(object sender, RoutedEventArgs e)
        {
            if (Service == null || Service.IsAutoMode || Service.CurrentRunningItem != null || Service.IsDrumSpinning) return;
            Service.ToggleDirection(false);
            if (TxtButtonStatus != null) TxtButtonStatus.Text = "Xoay Xuống";
        }

        private void TglMode_Checked(object sender, RoutedEventArgs e)
        {
            if (Service != null) Service.IsAutoMode = true;
            SetInputControlsState(true);
            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = false;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = false;
        }

        private void TglMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Service != null)
            {
                Service.IsAutoMode = false;
                Service.IsAutoPaused = false;
            }
            SetInputControlsState(false);
            bool isStepActive = Service?.CurrentRunningItem != null;
            bool isSpinning = Service?.IsDrumSpinning ?? false;
            // Chỉ mở lại nút xoay khi không chạy bước DataGrid và bồn đang dừng
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