using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XJProcess.modal;

namespace XJProcess.services
{
    public class ApiService
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        public string AccessToken { get; private set; } = string.Empty;
        public ApiService(string baseUrl = "https://api.xiangjiang.io.vn")
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.xiangjiang.io.vn" : baseUrl.Trim();
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
        /// <summary>
        /// Đăng nhập và lưu AccessToken.
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password)
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest("api/v1/auth/generateToken", Method.Post);
            request.AddJsonBody(new
            {
                username = username,
                password = password
            });
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                return false;
            using JsonDocument doc = JsonDocument.Parse(response.Content);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("accessToken", out var tokenProp))
            {
                return false;
            }
            AccessToken = tokenProp.GetString() ?? string.Empty;
            return true;
        }

        public static async Task<string> LoadDataChemical(string orderCode)
        {
            try
            {
                // TODO: Viết code gọi API thực tế của bạn ở đây (HttpClient, RestSharp, v.v.)
                // Ví dụ:
                // using (var client = new HttpClient())
                // {
                //     var response = await client.GetAsync($"https://api.yourdomain.com/chemical?code={orderCode}");
                //     if (response.IsSuccessStatusCode)
                //     {
                //         return await response.Content.ReadAsStringAsync();
                //     }
                // }

                Console.WriteLine($"Đang gọi LoadDataChemical với mã đơn: {orderCode}");
                await Task.Delay(500);

                return "Kết quả dữ liệu hóa chất cho mã: " + orderCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gọi LoadDataChemical: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// GET
        /// </summary>
        //public async Task<List<PrintTask>> GetAsync(string url)
        //{
        //    var client = new RestClient(_baseUrl);
        //    var request = new RestRequest(url, Method.Get);
        //    request.AddHeader("Authorization", $"Bearer {AccessToken}");
        //    var response = await client.ExecuteAsync(request);
        //    if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        //        return new List<PrintTask>();
        //    using var doc = JsonDocument.Parse(response.Content);
        //    if (!doc.RootElement.TryGetProperty("data", out var data))
        //        return new List<PrintTask>();
        //    return JsonSerializer.Deserialize<List<PrintTask>>(
        //        data.GetRawText(),
        //        new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true
        //        }) ?? new List<PrintTask>();
        //}

        /// <summary>
        /// POST
        /// </summary>
        //public async Task PostAsync(PrintTask print, int newStatus)
        //{
        //    var client = new RestClient(_baseUrl);
        //    print.Status = newStatus;
        //    var request = new RestRequest("api/v1/system/systemPrinterList/update", Method.Post);
        //    request.AddHeader("Authorization", $"Bearer {AccessToken}");
        //    request.AddJsonBody(new
        //    {
        //        id = print.Id,
        //        status = newStatus,
        //        printerIp = print.PrinterIp,
        //        printerName = print.PrinterName,
        //        printTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        //    });
        //    var response = await client.ExecuteAsync(request);
        //    Console.WriteLine(response.StatusCode);
        //    Console.WriteLine(response.Content);
        //    if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        //        return;
        //    using var doc = JsonDocument.Parse(response.Content);
        //}

        /// <summary>
        /// GET Danh sách Function
        /// </summary>
        //public async Task<List<Function>> GetFunctionsAsync(string url)
        //{
        //    var client = new RestClient(_baseUrl);
        //    var request = new RestRequest(url, Method.Get);
        //    request.AddHeader("Authorization", $"Bearer {AccessToken}");
        //    var response = await client.ExecuteAsync(request);
        //    if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        //        return new List<Function>();
        //    using var doc = JsonDocument.Parse(response.Content);
        //    if (!doc.RootElement.TryGetProperty("data", out var data))
        //        return new List<Function>();
        //    return JsonSerializer.Deserialize<List<Function>>(
        //        data.GetRawText(),
        //        new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true
        //        }) ?? new List<Function>();
        //}
    }
}
