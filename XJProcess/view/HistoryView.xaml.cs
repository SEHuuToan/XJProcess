using System.Windows;

namespace XJProcess.view
{
    public partial class HistoryView : Window
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); 
        }
    }
}