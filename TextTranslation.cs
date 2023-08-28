using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranslateTextSample;
using translator_demo.Models;

namespace translator_demo
{
    internal class TextTranslation
    {
        // Async call to the Translator Text API
        static public async Task<List<TranslationResult>> TranslateTextRequest(string subscriptionKey, string endpoint, string route, string inputText)
        {
            object[] body = new object[] { new { Text = inputText } };
            var requestBody = JsonConvert.SerializeObject(body);
            List<TranslationResult> translationRes = new();
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", Program.region);

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

    }
}
