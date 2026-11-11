using System.Windows;
using System.Windows.Media;

namespace XJProcess.view
{
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public CustomMessageBox(string message, string title, MessageBoxButton button, MessageBoxImage image)
        {
            InitializeComponent();
            TxtMessage.Text = message;
            string defaultNotice = Application.Current.TryFindResource("Msg_Notice") as string ?? "Thông báo";
            TxtTitle.Text = (string.IsNullOrEmpty(title) || title == "Thông báo") ? defaultNotice : title;
            ConfigureImage(image);
            ConfigureButtons(button);
        }

        private void ConfigureImage(MessageBoxImage image)
        {
            switch (image)
            {
                case MessageBoxImage.Error:
                    TxtIconSymbol.Text = "❌";
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    break;
                case MessageBoxImage.Warning:
                    TxtIconSymbol.Text = "⚠️";
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    break;
                case MessageBoxImage.Question:
                    TxtIconSymbol.Text = "❓";
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
                    break;
                default:
                    TxtIconSymbol.Text = "ℹ️";
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
                    break;
            }
        }

        private void ConfigureButtons(MessageBoxButton button)
        {
            string cancelText = Application.Current.TryFindResource("Msg_Cancel") as string ?? "Hủy";
            string confirmText = Application.Current.TryFindResource("St_Confirm") as string ?? "Xác nhận";
            string yesText = Application.Current.TryFindResource("Msg_Yes") as string ?? "Có";
            string noText = Application.Current.TryFindResource("Msg_No") as string ?? "Không";
            if (button == MessageBoxButton.OK)
            {
                BtnCancel.Visibility = Visibility.Collapsed;
                BtnOk.Content = confirmText;
            }
            else if (button == MessageBoxButton.YesNo)
            {
                BtnCancel.Content = noText;
                BtnOk.Content = yesText;
            }
            else
            {
                BtnCancel.Content = cancelText;
                BtnOk.Content = confirmText;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }

        public static MessageBoxResult Show(string message, string title = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Warning)
        {
            var msgBox = new CustomMessageBox(message, title, button, image);
            var activeWindow = Application.Current.MainWindow;
            if (activeWindow != null && activeWindow.IsVisible)
            {
                msgBox.Owner = activeWindow;
            }
            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}