using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XJProcess.modal
{
    public class ChemicalHeader : INotifyPropertyChanged
    {
        private string _line1Col0;
        public string Line1Col0
        {
            get => _line1Col0;
            set { _line1Col0 = value; OnPropertyChanged(); }
        }

        private string _line2Col0;
        public string Line2Col0
        {
            get => _line2Col0;
            set { _line2Col0 = value; OnPropertyChanged(); }
        }

        private string _line3Col0;
        public string Line3Col0
        {
            get => _line3Col0;
            set { _line3Col0 = value; OnPropertyChanged(); }
        }

        private string _line4Col0;
        public string Line4Col0
        {
            get => _line4Col0;
            set { _line4Col0 = value; OnPropertyChanged(); }
        }

        private string _line1Col1;
        public string Line1Col1
        {
            get => _line1Col1;
            set { _line1Col1 = value; OnPropertyChanged(); }
        }

        private string _line2Col1;
        public string Line2Col1
        {
            get => _line2Col1;
            set { _line2Col1 = value; OnPropertyChanged(); }
        }

        private string _line3Col1;
        public string Line3Col1
        {
            get => _line3Col1;
            set { _line3Col1 = value; OnPropertyChanged(); }
        }

        private string _line4Col1;
        public string Line4Col1
        {
            get => _line4Col1;
            set { _line4Col1 = value; OnPropertyChanged(); }
        }

        private string _technicianName;
        public string TechnicianName
        {
            get => _technicianName;
            set { _technicianName = value; OnPropertyChanged(); }
        }

        private string _drumNo;
        public string DrumNo
        {
            get => _drumNo;
            set { _drumNo = value; OnPropertyChanged(); }
        }

        private string _startTime;
        public string StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        private string _endTime;
        public string EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}