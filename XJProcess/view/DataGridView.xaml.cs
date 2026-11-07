using System.IO;
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
            LoadAppVersion();
        }
        private void LoadAppVersion()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string versionFile = Path.Combine(baseDir, "version_local.txt");

                if (File.Exists(versionFile))
                {
                    string ver = File.ReadAllText(versionFile).Trim();
                    TxtAppVersion.Text = ver.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? ver : "v" + ver;
                }
                else
                {
                    TxtAppVersion.Text = "v1.0.0.0";
                }
            }
            catch
            {
                TxtAppVersion.Text = "v1.0.0.0";
            }
        }

        public void LoadChemicalData(ChemicalData? currentOrder)
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