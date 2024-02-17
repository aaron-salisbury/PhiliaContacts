using System.Diagnostics;

namespace PhiliaContacts.Data
{
    public static class WebRequests
    {
        public static async Task<byte[]?> DownloadImageFromURLAsync(string url, IHttpClientFactory httpClientFactory)
        {
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();
                return await client.GetByteArrayAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HTTP request failed: {ex.Message}", "ERROR");
                return null;
            }
        }
    }
}
