using System.Collections.ObjectModel;

namespace XJProcess.modal
{
    public class ProcessGroup
    {
        public ObservableCollection<ChemicalDetail> Chemicals { get; set; } = new ObservableCollection<ChemicalDetail>();
        public int TotalDurationMinutes { get; set; }
    }
}