using S7.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;

namespace XJProcess.ultis
{
    internal class PlcUtils
    {
        private static Plc plc = null;
        public static bool IsSimulationMode { get; set; } = true;
        public static bool ConnectPlc(string ip)
        {
            if (IsSimulationMode) return true;
            try { 
                plc = new Plc(CpuType.S7200Smart, ip, 0, 1);
                plc.Open();
                return plc.IsConnected; // Return true if connected successfully, false otherwise
            }
            catch
            {
                return false;
            }
        }

        public static void Stop()
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected) return;
            plc.Write("M0.0", false);
        }

        public static void Start()
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected) return;
            plc.Write("M0.0", true);
        }

        public static void Reverse(bool value)
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected) return;
            plc.Write("M0.2", value);
        }

        public static void Up()
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected) return;
            plc.Write("M0.3", true);
        }

        public static void Down()
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected) return;
            plc.Write("M0.3", false);
        }

        public static bool Running()
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected) return false;
            var bytes = plc.ReadBytes(DataType.Memory, 0, 0, 0);
            return BitConverter.ToBoolean(bytes, 0);
        }

        public static bool ReadBool(string address)
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected)
                return false;
            //if (plc == null || !plc.IsConnected)
            //    return false;
            return (bool)plc.Read(address);
        }

        public static void Write(short value)
        {
            if (IsSimulationMode || plc == null || !plc.IsConnected)
                return;
            //if (plc == null || !plc.IsConnected)
            //    return;
            byte[] bytes =
            {
                (byte)(value >> 8),
                (byte)(value & 0xFF)
            };

            plc.WriteBytes(DataType.DataBlock, 1, 500, bytes);
        }

        //public static int GetTimeStop()
        //{
        //    var bytes = plc.ReadBytes(DataType.DataBlock, 1, 500, 2);
        //    short value = (short)((bytes[0] << 8) | bytes[1]);
        //    return value;
        //}

        //public static void SetTimeStop(short value)
        //{
        //    if (plc == null || !plc.IsConnected)
        //        return;
        //    byte[] bytes =
        //    {
        //        (byte)(value >> 8),
        //        (byte)(value & 0xFF)
        //    };

        //    plc.WriteBytes(DataType.DataBlock, 1, 500, bytes);
        //}
    }
}