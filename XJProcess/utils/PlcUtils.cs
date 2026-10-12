using S7.Net;
using System;

namespace XJProcess.ultis
{
    internal class PlcUtils
    {
        private static Plc plc = null;

        // Vẫn giữ lại biến này theo ý bro
        public static bool IsSimulationMode { get; set; } = false; // Mặc định để false để chạy thật

        // Kiểm tra trạng thái đã kết nối thực sự chưa
        public static bool IsConnected => plc != null && plc.IsConnected;

        public static bool ConnectPlc(string ip)
        {
            try
            {
                // Nếu đã kết nối rồi thì không tạo lại tránh rò rỉ
                if (plc != null && plc.IsConnected) return true;

                plc = new Plc(CpuType.S7200Smart, ip, 0, 1);
                plc.Open();
                return plc.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        public static void Stop()
        {
            if (plc == null || !plc.IsConnected) return;
            plc.Write("M0.0", false);
        }

        public static void Start()
        {
            if (plc == null || !plc.IsConnected) return;
            plc.Write("M0.0", true);
        }

        public static void Reverse(bool value)
        {
            if (plc == null || !plc.IsConnected) return;
            plc.Write("M0.2", value);
        }

        public static void Up()
        {
            if (plc == null || !plc.IsConnected) return;
            plc.Write("M0.3", true);
        }

        public static void Down()
        {
            if (plc == null || !plc.IsConnected) return;
            plc.Write("M0.3", false);
        }

        public static bool Running()
        {
            if (plc == null || !plc.IsConnected) return false;
            var bytes = plc.ReadBytes(DataType.Memory, 0, 0, 0);
            return BitConverter.ToBoolean(bytes, 0);
        }

        public static bool ReadBool(string address)
        {
            if (plc == null || !plc.IsConnected) return false;
            return (bool)plc.Read(address);
        }

        public static void Write(short value)
        {
            if (plc == null || !plc.IsConnected) return;
            byte[] bytes =
            {
                (byte)(value >> 8),
                (byte)(value & 0xFF)
            };
            plc.WriteBytes(DataType.DataBlock, 1, 1000, bytes);
        }

        // --- CÁC HÀM ĐỌC THỰC TẾ TỪ PLC ---
        public static byte ReadByte(string address)
        {
            if (plc == null || !plc.IsConnected) return 0;
            try { return (byte)plc.Read(address); }
            catch { return 0; }
        }

        public static ushort ReadUInt16(string address)
        {
            if (plc == null || !plc.IsConnected) return 0;
            try { return (ushort)plc.Read(address); }
            catch { return 0; }
        }

        public static float ReadFloat(string address)
        {
            if (plc == null || !plc.IsConnected) return 0f;
            try { return (float)plc.Read(address); }
            catch { return 0f; }
        }

        public static void WriteByte(string address, byte value)
        {
            if (plc == null || !plc.IsConnected) return;
            try { plc.Write(address, value); } catch { }
        }

        public static void WriteUInt16(string address, ushort value)
        {
            if (plc == null || !plc.IsConnected) return;
            try { plc.Write(address, value); } catch { }
        }

        public static void WriteInt(string address, int value)
        {
            if (plc == null || !plc.IsConnected) return;
            try { plc.Write(address, value); } catch { }
        }

        public static void SetTimeStop(short value)
        {
            if (plc == null || !plc.IsConnected) return;
            byte[] bytes =
            {
                (byte)(value >> 8),
                (byte)(value & 0xFF)
            };
            plc.WriteBytes(DataType.DataBlock, 1, 850, bytes);
        }
    }
}