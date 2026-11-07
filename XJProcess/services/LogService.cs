using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using XJProcess.modal;

namespace XJProcess.services
{
    public class RunningStateModel
    {
        public string BillId { get; set; } = string.Empty;
        public string BillNo { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public int CurrentStepIndex { get; set; } = -1;
        public int RemainingSeconds { get; set; } = 0;
        public int TotalSeconds { get; set; } = 0;
        public DateTime SavedAt { get; set; }
        public List<StepStateSnapshot> Steps { get; set; } = new();
    }

    public class StepStateSnapshot
    {
        public int Sn { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }

    public static class LogService
    {
        // 1. Lịch sử đơn hàng: Vẫn lưu trong thư mục gốc của app như cũ (Logs/History)
        private static readonly string HistoryDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "History");

        // 2. Trạng thái dở dang: Lưu ngay tại C:\Users\XIANG JIANG\XJProcess\running_state.json
        // (Dùng UserProfile để tự động bắt đúng tên máy tính hiện tại của bro)
        private static readonly string UserProfileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "XJProcess");
        private static readonly string RecoveryFile = Path.Combine(UserProfileFolder, "running_state.json");

        static LogService()
        {
            EnsureDirectoriesExist();
        }

        private static void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(HistoryDir))
                    Directory.CreateDirectory(HistoryDir);

                if (!Directory.Exists(UserProfileFolder))
                    Directory.CreateDirectory(UserProfileFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo thư mục log: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveOrderHistory(ChemicalData orderData, string logContent)
        {
            try
            {
                EnsureDirectoriesExist();
                if (orderData == null || string.IsNullOrEmpty(orderData.OrderCode)) return;

                string dateStr = DateTime.Now.ToString("yyyyMMdd");
                string fileName = $"{dateStr}_{orderData.OrderCode}.txt";
                string filePath = Path.Combine(HistoryDir, fileName);

                string fullLog = $"=== LỊCH SỬ VẬN HÀNH ĐƠN HÀNG ===\n" +
                                 $"Thời gian ghi log: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"Mã đơn (Mobill): {orderData.OrderCode}\n" +
                                 $"Kỹ thuật viên: {orderData.TechnicianName}\n" +
                                 $"Người vận hành: {orderData.User}\n" +
                                 $"Tổng trọng lượng: {orderData.Weight} KG\n" +
                                 $"------------------------------------\n" +
                                 logContent;

                File.WriteAllText(filePath, fullLog);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi ghi log lịch sử: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveRunningState(MainService service)
        {
            try
            {
                EnsureDirectoriesExist();

                if (service == null)
                {
                    MessageBox.Show("Lỗi SaveRunningState: MainService đang null!", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (service.CurrentOrder == null)
                {
                    MessageBox.Show("Lỗi SaveRunningState: CurrentOrder đang null! (API tải đơn hàng có thể bị lỗi khi chạy file .exe độc lập)", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(service.CurrentOrder.OrderCode))
                {
                    MessageBox.Show("Lỗi SaveRunningState: OrderCode đang trống!", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var state = new RunningStateModel
                {
                    BillId = service.CurrentBillId,
                    BillNo = service.CurrentBillNo,
                    OrderCode = service.CurrentOrder.OrderCode,
                    CurrentStepIndex = service.CurrentRunningItem != null ? service.ChemicalList.IndexOf(service.CurrentRunningItem) : -1,
                    RemainingSeconds = service.Timer,
                    TotalSeconds = service.TotalTimer,
                    SavedAt = DateTime.Now,
                    Steps = service.ChemicalList.Select(s => new StepStateSnapshot
                    {
                        Sn = s.Sn,
                        Code = s.Code,
                        Name = s.Name,
                        Status = s.Status,
                        IsCompleted = service.IsCompleted(s)
                    }).ToList()
                };

                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RecoveryFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi ngoại lệ khi lưu trạng thái phục hồi: " + ex.Message, "Lỗi Log", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static RunningStateModel? LoadRunningState()
        {
            try
            {
                if (File.Exists(RecoveryFile))
                {
                    string json = File.ReadAllText(RecoveryFile);
                    return JsonSerializer.Deserialize<RunningStateModel>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đọc trạng thái phục hồi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public static void ClearRunningState()
        {
            try
            {
                if (File.Exists(RecoveryFile))
                {
                    File.Delete(RecoveryFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xóa file phục hồi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}