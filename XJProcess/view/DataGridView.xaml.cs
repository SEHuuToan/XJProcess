using System.Windows;
using System.Windows.Controls;
using XJProcess.modal;
using XJProcess.services;

namespace XJProcess.view
{
    public partial class DataGridView : UserControl
    {
        public MainService Service { get; set; }

        public DataGridView()
        {
            InitializeComponent();
        }

        public void LoadChemicalData(ChemicalHeader currentOrder)
        {
            Service.CurrentOrder = currentOrder;
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ChemicalDetail clickedItem)
            {
                Service.ToggleStepAction(clickedItem);
            }
        }
    }
}