using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XJProcess.modal
{
    public class ChemicalDetail : INotifyPropertyChanged
    {
        public string StepNo { get; set; }
        public int Stt { get; set; }
        public string Code { get; set; }
        public string ChemName { get; set; }
        public string Kg { get; set; }
        public string Temp { get; set; }

        private int _durationMinutes;
        public int DurationMinutes
        {
            get => _durationMinutes;
            set
            {
                _durationMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationDisplay));
                OnPropertyChanged(nameof(HasAction));
            }
        }

        public string CheckInfo { get; set; }

        private bool _isCurrentStep;
        public bool IsCurrentStep
        {
            get => _isCurrentStep;
            set { _isCurrentStep = value; OnPropertyChanged(); }
        }

        // Trạng thái Đang chạy
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ActionButtonBackground));
            }
        }

        // Trạng thái Tạm dừng (để hiện chữ "Tiếp tục")
        private bool _isPaused;
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ActionButtonBackground));
            }
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsButtonEnabled));
                OnPropertyChanged(nameof(ActionButtonBackground));
            }
        }

        public bool IsButtonEnabled => IsEnabled;

        private int _remainingSeconds;
        public int RemainingSeconds
        {
            get => _remainingSeconds;
            set { _remainingSeconds = value; OnPropertyChanged(); }
        }

        public bool IsGroupHeader { get; set; }
        public bool HasAction => DurationMinutes != 0;

        public string ActionButtonText
        {
            get
            {
                if (Status == "Đã hoàn thành") return "Bắt đầu";
                if (IsRunning) return "Tạm dừng";
                if (IsPaused) return "Tiếp tục";
                return "Bắt đầu";
            }
        }
        public string ActionButtonBackground
        {
            get
            {
                if (Status == "Đã hoàn thành") return "#9E9E9E";
                if (IsRunning) return "#F59E0B";
                if (IsPaused) return "#2E7D32";  
                if (IsEnabled) return "#38BDF8";  
                return "#9E9E9E"; 
            }
        }

        public string DurationDisplay => DurationMinutes > 0 ? $"{DurationMinutes}" : string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}