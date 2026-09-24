using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace XJProcess.modal
{
    public class ChemicalDetail : INotifyPropertyChanged
    {
        public string StepNo { get; set; } = string.Empty;
        public int Stt { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ChemName { get; set; } = string.Empty;
        public string Kg { get; set; } = string.Empty;
        public string Temp { get; set; } = string.Empty;
        public string CheckInfo { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public bool IsGroupHeader { get; set; }
        public string ChemicalName => ChemName;
        public string Weight => Kg;
        public string Temperature => Temp;
        public int h => DurationMinutes;

        private bool _isEnabled = true;
        private int _isRunning; // 0=Stop, 1=Run, 2=Pause, 3=Finish, 4=Override
        private bool _isPaused;
        private string _status = string.Empty;

        public bool HasAction => DurationMinutes > 0 || !string.IsNullOrEmpty(CheckInfo);

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsButtonEnabled));
            }
        }
        public bool IsButtonEnabled => IsEnabled;

        public int IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ActionButtonBackground));
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ActionButtonBackground));
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ActionButtonBackground));
            }
        }

        public string ButtonText
        {
            get
            {
                if (IsRunning == 1 || IsRunning == 4) return "Tạm dừng";
                if (IsRunning == 2) return "Tiếp tục";
                if (IsRunning == 3 || Status == "Đã hoàn thành") return "Hoàn thành";
                return "Bắt đầu";
            }
        }

        // Binding ở XAML ăn vào thuộc tính này
        public string ActionButtonText => ButtonText;

        public Brush ActionButtonBackground
        {
            get
            {
                if (IsRunning == 1 || IsRunning == 4) return (Brush)new BrushConverter().ConvertFrom("#EAB308")!; // Vàng
                if (IsRunning == 2) return (Brush)new BrushConverter().ConvertFrom("#0284C7")!;  // Xanh dương
                if (IsRunning == 3 || Status == "Đã hoàn thành") return (Brush)new BrushConverter().ConvertFrom("#22C55E")!; // Xanh lá
                return (Brush)new BrushConverter().ConvertFrom("#0284C7")!;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}