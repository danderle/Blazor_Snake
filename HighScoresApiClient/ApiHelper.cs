using System.Net.Http.Headers;

namespace HighScoresApiClient;

public static class ApiHelper
{
    public static HttpClient Client { get; private set; }

    static ApiHelper()
    {
        InitializeClient();
    }

    private static void InitializeClient()
    {
        Client = new HttpClient();
        Client.BaseAddress = new Uri("https://localhost:7089");
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
