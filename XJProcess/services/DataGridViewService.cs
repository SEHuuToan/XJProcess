using System.Collections.ObjectModel;
using XJProcess.modal;

namespace XJProcess.service
{
    public class DataGridViewService
    {
        public static ObservableCollection<ChemicalDetail> GetFakeTechnicalSheetData()
        {
            return new ObservableCollection<ChemicalDetail>
            {
                // NO 1: 回湿 (Hồi thấp) - Thời gian 240 phút nằm ở dòng STT 5
                new ChemicalDetail { StepNo = "1. 回湿", Stt = 1, Code = "06.99.0116N", ChemName = "水 H2O", Kg = "", Temp = "30", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "1. 回湿", Stt = 2, Code = "16", ChemName = "盐", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "1. 回湿", Stt = 3, Code = "31", ChemName = "硫铵", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "1. 回湿", Stt = 4, Code = "", ChemName = "脱脂剂", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "1. 回湿", Stt = 5, Code = "33", ChemName = "草酸", Kg = "", Temp = "", DurationMinutes = 1, CheckInfo = "" },

                // 洗水 RUA NUOC
                new ChemicalDetail { StepNo = "", Stt = 7, Code = "", ChemName = "洗水RUA NUOC", Kg = "", Temp = "25", DurationMinutes = 1, CheckInfo = "倒水" },

                // NO 2: 复鞣 (Phục nhuộm)
                new ChemicalDetail { StepNo = "2. 复鞣", Stt = 8, Code = "", ChemName = "水 H2O", Kg = "", Temp = "25", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "2. 复鞣", Stt = 9, Code = "13", ChemName = "甲酸", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "2. 复鞣", Stt = 11, Code = "", ChemName = "铬粉", Kg = "", Temp = "", DurationMinutes = 120, CheckInfo = "查PH" },
                new ChemicalDetail { StepNo = "2. 复鞣", Stt = 12, Code = "", ChemName = "脂肪醛", Kg = "", Temp = "", DurationMinutes = 60, CheckInfo = "" },

                // NO 3: 提碱 (Đề kiềm)
                new ChemicalDetail { StepNo = "3. 提碱", Stt = 13, Code = "", ChemName = "水 H2O", Kg = "", Temp = "25", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "3. 提碱", Stt = 14, Code = "39", ChemName = "甲酸钠", Kg = "", Temp = "", DurationMinutes = 20, CheckInfo = "" },
                new ChemicalDetail { StepNo = "3. 提碱", Stt = 15, Code = "37", ChemName = "小苏打", Kg = "", Temp = "", DurationMinutes = 30, CheckInfo = "" },
                new ChemicalDetail { StepNo = "3. 提碱", Stt = 16, Code = "37", ChemName = "小苏打", Kg = "", Temp = "", DurationMinutes = 40, CheckInfo = "查PH" },

                // 洗水 RUA NUOC
                new ChemicalDetail { StepNo = "", Stt = 17, Code = "", ChemName = "洗水RUA NUOC", Kg = "", Temp = "25", DurationMinutes = 10, CheckInfo = "倒水" },

                // NO 4: 中和 (Trung hòa)
                new ChemicalDetail { StepNo = "4. 中和", Stt = 18, Code = "", ChemName = "水 H2O", Kg = "", Temp = "25", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "4. 中和", Stt = 19, Code = "39", ChemName = "甲酸钠", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "4. 中和", Stt = 20, Code = "", ChemName = "中和单宁", Kg = "", Temp = "", DurationMinutes = 30, CheckInfo = "" },
                new ChemicalDetail { StepNo = "4. 中和", Stt = 21, Code = "35", ChemName = "碳酸氢", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "4. 中和", Stt = 22, Code = "37", ChemName = "小苏打", Kg = "", Temp = "", DurationMinutes = 120, CheckInfo = "查PH" },
                new ChemicalDetail { StepNo = "4. 中和", Stt = 23, Code = "", ChemName = "大苏打", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "4. 中和", Stt = 24, Code = "", ChemName = "丙烯酸", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "查BCG" },

                // 洗水 RUA NUOC
                new ChemicalDetail { StepNo = "", Stt = 25, Code = "", ChemName = "洗水RUA NUOC", Kg = "", Temp = "25", DurationMinutes = 10, CheckInfo = "倒水" },

                // NO 5: 染色 (Nhuộm)
                new ChemicalDetail { StepNo = "5. 染色", Stt = 26, Code = "", ChemName = "水 H2O", Kg = "", Temp = "25", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "5. 染色", Stt = 27, Code = "11", ChemName = "氨水", Kg = "", Temp = "", DurationMinutes = 5, CheckInfo = "" },
                new ChemicalDetail { StepNo = "5. 染色", Stt = 28, Code = "", ChemName = "置换单宁", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "5. 染色", Stt = 29, Code = "", ChemName = "填充单宁", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "5. 染色", Stt = 30, Code = "", ChemName = "分散单宁", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "5. 染色", Stt = 31, Code = "", ChemName = "染料", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "5. 染色", Stt = 32, Code = "", ChemName = "染料", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "5. 染色", Stt = 33, Code = "", ChemName = "染料", Kg = "", Temp = "", DurationMinutes = 60, CheckInfo = "Ø" },

                // NO 6: 加脂 (Bôi mỡ) - Ảnh không ghi thời gian cho bước 6
                new ChemicalDetail { StepNo = "6. 加脂", Stt = 34, Code = "", ChemName = "水 H2O", Kg = "", Temp = "50", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "6. 加脂", Stt = 35, Code = "", ChemName = "亚硫酸化油", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "对色" },
                new ChemicalDetail { StepNo = "6. 加脂", Stt = 36, Code = "", ChemName = "磷脂加脂", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "6. 加脂", Stt = 37, Code = "", ChemName = "鱼油加脂剂", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },
                new ChemicalDetail { StepNo = "6. 加脂", Stt = 38, Code = "", ChemName = "合成加脂剂", Kg = "", Temp = "", DurationMinutes = 0, CheckInfo = "" },

                // NO 7: 固定 (Cố định)
                new ChemicalDetail { StepNo = "7. 固定", Stt = 39, Code = "", ChemName = "水 H2O", Kg = "", Temp = "", DurationMinutes = 60, CheckInfo = "对色" },
                new ChemicalDetail { StepNo = "7. 固定", Stt = 40, Code = "13", ChemName = "甲酸", Kg = "", Temp = "", DurationMinutes = 40, CheckInfo = "" },
                new ChemicalDetail { StepNo = "7. 固定", Stt = 41, Code = "13", ChemName = "甲酸", Kg = "", Temp = "", DurationMinutes = 15, CheckInfo = "" },

                // Giữ nguyên STT 42, 43, 44 tự thêm của bác
                new ChemicalDetail { StepNo = "7. 固定", Stt = 42, Code = "13", ChemName = "甲酸", Kg = "", Temp = "", DurationMinutes = 15, CheckInfo = "" },
                new ChemicalDetail { StepNo = "7. 固定", Stt = 43, Code = "13", ChemName = "甲酸", Kg = "", Temp = "", DurationMinutes = 15, CheckInfo = "查PH" },
                new ChemicalDetail { StepNo = "", Stt = 44, Code = "", ChemName = "洗水RUA NUOC", Kg = "", Temp = "25", DurationMinutes = 10, CheckInfo = "倒水" },
            };
        }

        public static ChemicalHeader GetFakeOrderHeaderInfo()
        {
            return new ChemicalHeader
            {
                Line1Col0 = "美国原张(Prime Asia) 7/10.5",
                Line2Col0 = "1.40-1.50 B",
                Line3Col0 = "1050 kg",
                Line4Col0 = "2026-07-08 11:51:33",

                Line1Col1 = "100026070402085-ADDS.1159E",
                Line2Col1 = "K1.40-1.50",
                Line3Col1 = "普通反绒",
                Line4Col1 = "SHADOW BROWN AEX5",

                TechnicianName = "Nguyễn Văn A",
                DrumNo = "#32",
                StartTime = "2026-08-14 08:00",
                EndTime = "2026-08-14 10:30"
            };
        }
    }
}