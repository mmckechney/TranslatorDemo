using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace translator_demo
{
    using System.Text;
    using System.Text.Json;
    using TranslateTextSample;

    class DocumentTranslation
    {
        private static readonly string endpoint = $"{Program.documentEndpoint}translator/text/batch/v1.1";
        private static readonly string key = Program.subscriptionKey;


        static readonly string route = "/batches";
        static readonly string sourceURL = $"\"{Program.sourceBlobSas}\"";
        static readonly string targetURL = $" \"{Program.targetBlobSas}\"";
        private static HttpClient client = new HttpClient();


        static readonly string json = ("{\"inputs\": [{\"source\": {\"sourceUrl\":" + sourceURL + " ,\"storageSource\": \"AzureBlob\",\"language\": \"en\"}, \"targets\": [{\"targetUrl\":" + targetURL + ",\"storageSource\": \"AzureBlob\",\"category\": \"general\",\"language\": \"es\"}]}]}");

        public static async Task TranslateBlobDocs()
        {

            var createTime = DateTime.UtcNow;
            using HttpRequestMessage request = new HttpRequestMessage();
            {

                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Content = content;

                HttpResponseMessage response = await client.SendAsync(request);
                string result = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Status code: {response.StatusCode}");
                    Console.WriteLine("Document translation accepted and started.");

                    await CheckTranslationStatus(createTime);
                }
                else
                {
                    Console.WriteLine($"Status code: {response.StatusCode}");
                    Console.Write("Error");
                }

            }

        }

        public static async Task CheckTranslationStatus(DateTime createdTime) 
        {
            Console.WriteLine("Checking status of document translation...");
            while(true)
            {
                using HttpRequestMessage request = new HttpRequestMessage();
                {

                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri(endpoint + route + $"?createdDateTimeUtcStart={createdTime.ToString()}");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                    request.Content = content;

                    HttpResponseMessage response = await client.SendAsync(request);
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var resObj = JsonSerializer.Deserialize<dynamic>(result);
                        var resPretty = JsonSerializer.Serialize(resObj, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine($"Status code: {response.StatusCode}");
                        Console.WriteLine($"Return Message:");
                        Console.WriteLine(resPretty);

                        if(result.ToLower().Contains("succeeded") && !result.ToLower().Contains("running"))
                        {
                            Console.WriteLine("Translation complete");
                            break;
                        }

                    }
                    else
                    {
                        Console.WriteLine($"Status code: {response.StatusCode}");
                        Console.Write("Error");
                    }

                }

                Thread.Sleep(10000);
            }
        }
    }
}
