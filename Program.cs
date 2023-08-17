// This sample requires C# 7.1 or later for async/await.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
// Install Newtonsoft.Json with NuGet
using Newtonsoft.Json;
using translator_demo;
using translator_demo.Models;

namespace TranslateTextSample
{

    class Program
    {

        private const string region_var = "TRANSLATOR_SERVICE_REGION";
        internal static readonly string region = Environment.GetEnvironmentVariable(region_var);

        private const string key_var = "TRANSLATOR_TEXT_SUBSCRIPTION_KEY";
        internal static readonly string subscriptionKey = Environment.GetEnvironmentVariable(key_var);

        private const string endpoint_var = "TRANSLATOR_TEXT_ENDPOINT";
        internal static readonly string endpoint = Environment.GetEnvironmentVariable(endpoint_var);

        private const string document_endpoint_var = "TRANSLATOR_DOCUMENT_ENDPOINT";
        internal static readonly string documentEndpoint = Environment.GetEnvironmentVariable(document_endpoint_var);

        private const string source_blob_sas = "SOURCE_BLOB_SAS";
        internal static readonly string sourceBlobSas = Environment.GetEnvironmentVariable(source_blob_sas);

        private const string target_blob_sas = "TARGET_BLOB_SAS";
        internal static readonly string targetBlobSas = Environment.GetEnvironmentVariable(target_blob_sas);

        static Program()
        {
            if (null == region)
            {
                throw new Exception("Please set/export the environment variable: " + region_var);
            }
            if (null == subscriptionKey)
            {
                throw new Exception("Please set/export the environment variable: " + key_var);
            }
            if (null == endpoint)
            {
                throw new Exception("Please set/export the environment variable: " + endpoint_var);
            }
        }

        // Async call to the Translator Text API
        static public async Task<List<TranslationResult>> TranslateTextRequest(string subscriptionKey, string endpoint, string route, string inputText)
        {
            object[] body = new object[] { new { Text = inputText } };
            var requestBody = JsonConvert.SerializeObject(body);
            List<TranslationResult> translationRes = new();
            using (var client = new HttpClient())
            using(var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", region);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();
                translationRes = JsonConvert.DeserializeObject<List<TranslationResult>>(result);
                translationRes[0].MeteredUsage = response.Headers.Contains("X-Metered-Usage") ? response.Headers.GetValues("X-Metered-Usage").First() : "Usage not found";
                // Iterate over the deserialized results.
                foreach (TranslationResult o in translationRes)
                {
                    // Print the detected input languge and confidence score.
                    Console.WriteLine("Detected input language: {0}\nConfidence score: {1}\n", o.DetectedLanguage.Language, o.DetectedLanguage.Score);
                    // Iterate over the results and print each translation.
                    foreach (Translation t in o.Translations)
                    {
                        if (t.Transliteration == null)
                        {

                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write($"Translated to {t.To}: ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{t.Text} ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write($"Translated to {t.To}: ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"{t.Text} ");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($" {t.Transliteration.Text} ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine();
                        }
                    }
                }
            }

            Console.WriteLine($"Metered Usage: {translationRes[0].MeteredUsage}");

            return translationRes;
        }





        static async Task Main(string[] args)
        {

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Please make a selection:");
                Console.WriteLine("1. Translate Text");
                Console.WriteLine("2. Translate Documents");
                var entry = Console.ReadLine();
                if (entry.StartsWith("1"))
                {
                    Console.WriteLine("Text Translation selected");
                    Console.WriteLine();

                    {
                        string route = "/translate?api-version=3.0&to=de&toScript=latn&to=it&toScript=latn&to=ja&toScript=latn&to=hi&toScript=latn&to=en&toScript=latn";
                        Console.Write("Type the phrase you'd like to translate? ");
                        string textToTranslate = Console.ReadLine();
                        Console.WriteLine();
                        var res = await TranslateTextRequest(subscriptionKey, endpoint, route, textToTranslate);
                        Console.WriteLine();


                    }

                }
                else if ((entry.StartsWith("2")))
                {
                    Console.WriteLine("Document Translation selected");
                    Console.WriteLine();
                    await DocumentTranslation.TranslateBlobDocs();

                }
            }
            // This is our main function.
            // Output languages are defined in the route.
            // For a complete list of options, see API reference.
            // https://docs.microsoft.com/azure/cognitive-services/translator/reference/v3-0-translate
            
            // Prompts you for text to translate. If you'd prefer, you can
            // provide a string as textToTranslate.

            

        }
    }
}
