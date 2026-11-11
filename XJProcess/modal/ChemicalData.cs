using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace XJProcess.modal
{
    //header data xài cho headerconfigleft
    public class ChemicalData : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("fid")]
        public string Fid { get; set; } = string.Empty;

        [JsonPropertyName("printerTime")]
        public string PrinterTime { get; set; } = string.Empty;

        private bool _isAdd;
        [JsonPropertyName("isAdd")]
        public bool IsAdd
        {
            get => _isAdd;
            set { _isAdd = value; OnPropertyChanged(); }
        }

        private string _tech = string.Empty;
        [JsonPropertyName("tech")]
        public string Tech
        {
            get => _tech;
            set { _tech = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechnicianName)); }
        }

        private string _weight = string.Empty;
        [JsonPropertyName("weight")]
        public string Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); }
        }

        private string _user = string.Empty;
        [JsonPropertyName("user")]
        public string User
        {
            get => _user;
            set { _user = value; OnPropertyChanged(); }
        }

        private string _mobill = string.Empty;
        [JsonPropertyName("mobill")]
        public string Mobill
        {
            get => _mobill;
            set { _mobill = value; OnPropertyChanged(); OnPropertyChanged(nameof(OrderCode)); }
        }

        [JsonPropertyName("list")]
        public List<ChemicalDetail> List { get; set; } = new List<ChemicalDetail>();

        public string TechnicianName
        {
            get => _tech;
            set { _tech = value; OnPropertyChanged(); OnPropertyChanged(nameof(Tech)); }
        }

        public string OrderCode => _mobill;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    //Chemical detail xài cho datagridview
    public class ChemicalDetail : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("billFk")]
        public string BillFk { get; set; } = string.Empty;

        [JsonPropertyName("func")]
        public string Func { get; set; } = string.Empty;

        [JsonPropertyName("sn")]
        public int Sn { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("percent")]
        public decimal Percent { get; set; }

        [JsonPropertyName("weight")]
        public double? Weight { get; set; }

        [JsonPropertyName("temp")]
        public double? Temp { get; set; }

        [JsonPropertyName("time")]
        public int? TimeMinutes { get; set; }

        [JsonPropertyName("check")]
        public string Check { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("startTime")]
        public string? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public string? EndTime { get; set; }

        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("isComplete")]
        public bool? IsComplete { get; set; }
        [JsonPropertyName("shortCode")]
        public string? ShortCode { get; set; }

        // --- CÁC PROPERTY TRUNG GIAN ĐỂ BINDING VÀO CỘT XAML ---

        public string StepNo => Func;
        public int Stt => Sn;
        public string ChemName => Name;
        public string ChemicalName => Name;

        public string Kg
        {
            get => (Weight ?? 0).ToString();
            set { double.TryParse(value, out var w); Weight = w; OnPropertyChanged(); }
        }

        public string Temperature => (Temp ?? 0).ToString();

        public int DurationMinutes
        {
            get => TimeMinutes ?? 0;
            set { TimeMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(h)); }
        }

        public int h => TimeMinutes ?? 0;
        //public int DurationMinutes
        //{
        //    get
        //    {
        //        int originalMinutes = TimeMinutes ?? 0;
        //        // FAKE GIẢM THỜI GIAN: Chia 3 (chia cho 3). 
        //        // Đảm bảo tối thiểu là 1 phút nếu step đó có thời gian (> 0) để vẫn chạy được, hoặc trả về 0 nếu không có thời gian.
        //        if (originalMinutes <= 0) return 0;
        //        int reduced = originalMinutes / 3;
        //        return reduced > 0 ? reduced : 1;
        //    }
        //    set
        //    {
        //        TimeMinutes = value;
        //        OnPropertyChanged();
        //        OnPropertyChanged(nameof(h));
        //    }
        //}
        //public int h => DurationMinutes; // Gọi lại DurationMinutes để đồng bộ giá trị đã chia 3

        public string CheckInfo => Check;

        // --- UI STATES (Trạng thái nút bấm, màu sắc) ---
        private bool _isEnabled = true;
        private int _isRunning;
        private bool _isPaused;
        private string _status = string.Empty;

        public bool HasAction => (TimeMinutes.GetValueOrDefault() > 0) || !string.IsNullOrEmpty(CheckInfo);

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsButtonEnabled)); }
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
                string startText = Application.Current.FindResource("dgv_buttonStart") as string ?? "Bắt đầu";
                string pauseText = Application.Current.FindResource("dgv_buttonStop") as string ?? "Tạm dừng";
                string resumeText = Application.Current.FindResource("dgv_buttonContinue") as string ?? "Tiếp tục";
                string completeText = Application.Current.FindResource("dgv_buttonFinish") as string ?? "Hoàn thành";
                if (IsRunning == 1 || IsRunning == 4) return pauseText;
                if (IsRunning == 2) return resumeText;
                if (IsRunning == 3 || Status == "Đã hoàn thành") return completeText;
                return startText;
            }
        }

        public string ActionButtonText => ButtonText;

        public Brush ActionButtonBackground
        {
            get
            {
                if (IsRunning == 1 || IsRunning == 4) return (Brush)new BrushConverter().ConvertFrom("#EAB308")!; // Vàng
                if (IsRunning == 2) return (Brush)new BrushConverter().ConvertFrom("#0284C7")!;  // Xanh dương (Đã fix lỗi dính chữ)
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