using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using XJProcess.modal;

namespace XJProcess.services
{
    public class ChemicalService
    {

        private readonly ApiService _apiService;
        public ChemicalService(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Lấy dữ liệu thô từ API và đóng gói vào ChemicalStepItem để quản lý state UI
        /// </summary>
        public async Task<List<ChemicalStepItem>> GetChemicalStepsAsync(string orderId)
        {
            var rawList = await _apiService.LoadDataChemical(orderId) ?? new List<ChemicalDetail>();
            var stepItems = new List<ChemicalStepItem>();
            bool isFirstActionEnabled = false;

            foreach (var detail in rawList)
            {
                var item = new ChemicalStepItem(detail)
                {
                    RemainingSeconds = detail.DurationMinutes * 60,
                    Status = "Chờ thực hiện",
                    IsEnabled = false
                };

                if (item.HasAction && !isFirstActionEnabled)
                {
                    item.IsEnabled = true;
                    isFirstActionEnabled = true;
                }

                stepItems.Add(item);
            }

            return stepItems;
        }

        public void CompleteStep(ChemicalStepItem item)
        {
            item.Status = "Đã hoàn thành";
            item.IsRunning = false;
            item.IsPaused = false;
            item.IsEnabled = false;
        }
    }

    public class ChemicalStepItem : INotifyPropertyChanged
    {
        public ChemicalDetail Data { get; }

        public ChemicalStepItem(ChemicalDetail data)
        {
            Data = data;
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); NotifyUI(); }
        }

        private bool _isPaused;
        public bool IsPaused
        {
            get => _isPaused;
            set { _isPaused = value; OnPropertyChanged(); NotifyUI(); }
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); NotifyUI(); }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); NotifyUI(); }
        }

        private int _remainingSeconds;
        public int RemainingSeconds
        {
            get => _remainingSeconds;
            set { _remainingSeconds = value; OnPropertyChanged(); }
        }

        public bool HasAction => Data.DurationMinutes > 0;

        public string ActionButtonText => Status switch
        {
            "Đã hoàn thành" => "Hoàn thành",
            _ when IsRunning => "Tạm dừng",
            _ when IsPaused => "Tiếp tục",
            _ => "Bắt đầu"
        };

        public string ActionButtonBackground => Status switch
        {
            "Đã hoàn thành" => "#9E9E9E",
            _ when IsRunning => "#F59E0B",
            _ when IsPaused => "#2E7D32",
            _ when IsEnabled => "#38BDF8",
            _ => "#9E9E9E"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyUI()
        {
            OnPropertyChanged(nameof(ActionButtonText));
            OnPropertyChanged(nameof(ActionButtonBackground));
        }
    }
}