using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XJProcess.modal;
using System.Net.Http.Json;

namespace XJProcess.services
{


    public class ApiService
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public ApiService(string baseUrl = "http://192.168.10.8:8072")
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://192.168.10.8:8072" : baseUrl.Trim();
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<ChemicalData?> LoadDataChemical(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            try
            {
                string jsonString = await _httpClient.GetStringAsync($"api/v1/private/PLCprocess/billInfo?id={id}");
                if (string.IsNullOrWhiteSpace(jsonString))
                    return null;
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var chemicalData = JsonSerializer.Deserialize<ChemicalData>(dataElement.GetRawText(), options);
                    if (chemicalData?.List != null)
                    {
                        chemicalData.List = chemicalData.List.OrderBy(x => x.Sn).ToList();
                    }
                    return chemicalData;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi ApiService LoadDataChemical: {ex.Message}");
            }
            return null;
        }
    }
}
