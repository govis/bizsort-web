using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        var client = new HttpClient(handler);
        var response = await client.GetAsync("https://localhost:5001/api/company/profile/view?company=8981");
        Console.WriteLine($"Status: {response.StatusCode}");
        if (response.IsSuccessStatusCode) {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
