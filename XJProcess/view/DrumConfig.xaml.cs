using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using XJProcess.service;

namespace XJProcess.view
{
    public partial class DrumConfig : UserControl
    {
        private readonly View3DService _view3DService = new View3DService();
        private readonly AxisAngleRotation3D _spinnerRotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);

        private DispatcherTimer _reverseTimer;
        private bool _isSpinning = false;
        private bool _isForwardDirection = true;
        private bool _isAutoMode = false;

        public event EventHandler<int>? ManualStartRequested;
        public event EventHandler? StartRequested;
        public event EventHandler? StopRequested;

        public bool IsSpinning => _isSpinning;

        public DrumConfig()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            Init3DScene();
            InitReverseTimer();

            if (TxtButtonStatus != null) TxtButtonStatus.Text = "Xoay Xuống";
            SetReverseInputState(false);
        }

        private void InitReverseTimer()
        {
            _reverseTimer = new DispatcherTimer();
            _reverseTimer.Tick += ReverseTimer_Tick;
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

        #region Drum Animation & Display API

        public void StartDrumSpin()
        {
            ApplySpinAnimation();
            _isSpinning = true;

            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = false;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = false;
            if (TglMode != null) TglMode.IsEnabled = false;

            if (_isAutoMode && int.TryParse(TxtReverseTimeInput?.Text, out int reverseSec) && reverseSec > 0)
            {
                _reverseTimer.Interval = TimeSpan.FromSeconds(reverseSec);
                _reverseTimer.Start();
            }
        }

        public void StopDrumSpin()
        {
            _reverseTimer?.Stop();

            if (_isSpinning)
            {
                double currentAngle = _spinnerRotation.Angle;
                _spinnerRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
                _spinnerRotation.Angle = currentAngle;

                _isSpinning = false;
                if (BtnRotateUp != null) BtnRotateUp.IsEnabled = !_isAutoMode;
                if (BtnRotateDown != null) BtnRotateDown.IsEnabled = !_isAutoMode;
                if (TglMode != null) TglMode.IsEnabled = true;
            }
        }

        private void ApplySpinAnimation()
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

            if (TxtButtonStatus != null)
            {
                TxtButtonStatus.Text = _isForwardDirection ? "Xoay Xuống" : "Xoay Lên";
            }
        }

        private void ReverseTimer_Tick(object? sender, EventArgs e)
        {
            _isForwardDirection = !_isForwardDirection;
            ApplySpinAnimation();
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
            if (txtScannedResult != null)
            {
                txtScannedResult.Text = string.IsNullOrEmpty(code) ? "Chưa có dữ liệu..." : code;
            }
        }

        #endregion

        #region Event Handlers UI

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (TxtRunTimeInput != null && int.TryParse(TxtRunTimeInput.Text, out int inputMinutes) && inputMinutes > 0)
            {
                ManualStartRequested?.Invoke(this, inputMinutes);
            }
            else
            {
                StartRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnRotateUp_Click(object sender, RoutedEventArgs e)
        {
            if (_isAutoMode) return;
            _isForwardDirection = false;
            if (_isSpinning) ApplySpinAnimation();
            if (TxtButtonStatus != null) TxtButtonStatus.Text = "Xoay Lên";
        }

        private void BtnRotateDown_Click(object sender, RoutedEventArgs e)
        {
            if (_isAutoMode) return;
            _isForwardDirection = true;
            if (_isSpinning) ApplySpinAnimation();
            if (TxtButtonStatus != null) TxtButtonStatus.Text = "Xoay Xuống";
        }

        private void TglMode_Checked(object sender, RoutedEventArgs e)
        {
            _isAutoMode = true;
            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = false;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = false;
            SetReverseInputState(true);
        }

        private void TglMode_Unchecked(object sender, RoutedEventArgs e)
        {
            _isAutoMode = false;
            if (BtnRotateUp != null) BtnRotateUp.IsEnabled = true;
            if (BtnRotateDown != null) BtnRotateDown.IsEnabled = true;
            SetReverseInputState(false);
        }

        private void SetReverseInputState(bool isEnabled)
        {
            if (TxtReverseTimeInput != null)
            {
                TxtReverseTimeInput.IsEnabled = isEnabled;
                TxtReverseTimeInput.Background = isEnabled
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(220, 220, 220));
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