using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

using translator_demo.Models;

namespace translator_demo
{
   internal class TextTranslation
   {
      // Async call to the Translator Text API
      static public async Task<List<TranslationResult>> TranslateTextRequest(string subscriptionKey, string endpoint, string route, string inputText, string region, ILogger log)
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
            request.Headers.Add("Ocp-Apim-Subscription-Region", region);

            // Send the request and get response.
            HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
            // Read response as a string.
            string result = await response.Content.ReadAsStringAsync();
            try
            {
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

                        log.LogInformation(new() { { $"Translated to {t.To}: ", ConsoleColor.Cyan }, { t.Text, ConsoleColor.White } });

                     }
                     else
                     {
                        log.LogInformation(new() { { $"Translated to {t.To}: ", ConsoleColor.Cyan }, { t.Text, ConsoleColor.White }, { $"[{t.Transliteration.Text}]", ConsoleColor.Blue } });
                     }
                  }
               }
               log.LogInformation($"Metered Usage: {translationRes[0].MeteredUsage}");

            }
            catch (Exception exe)
            {
               log.LogInformation($"{exe.Message}{Environment.NewLine}{result}", ConsoleColor.Red);
            }
         }
#pragma warning disable CS8603 // Possible null reference return.
         return translationRes;
#pragma warning restore CS8603 // Possible null reference return.
      }

   }
}
