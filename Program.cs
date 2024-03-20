using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Spectre.Console;
using System.Data;
using System.Text;


namespace translator_demo
{

   public class Program
   {
      private static ILoggerFactory logFactory;
      private static LogLevel logLevel;
      private static ILogger log;

      private const string region_var = "TRANSLATOR_SERVICE_REGION";
      internal static string region;

      private const string key_var = "TRANSLATOR_TEXT_SUBSCRIPTION_KEY";
      internal static string subscriptionKey;

      private const string endpoint_var = "TRANSLATOR_TEXT_ENDPOINT";
      internal static string endpoint;

      private const string document_endpoint_var = "TRANSLATOR_DOCUMENT_ENDPOINT";
      internal static string documentEndpoint;

      private const string source_blob_sas = "SOURCE_BLOB_SAS";
      internal static string sourceBlobSas;

      private const string target_blob_sas = "TARGET_BLOB_SAS";
      internal static string targetBlobSas;

      private const string custom_translator_cat_id = "CUSTOM_TRANSLATOR_CATEGORY_ID";
      internal static string customCatId;

      private const string custom_translator_to_lan = "CUSTOM_TRANSLATOR_TO_LANGUAGE";
      internal static string customToLan;

      private const string text_translation_to_lan = "TEXT_TRANSLATION_LANGUAGES";
      internal static string[] textToLans;

      static Program()
      {
         var config = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)  // common settings go here.
           .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")}.json", optional: true, reloadOnChange: false)  // environment specific settings go here
           .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)  // secrets go here. This file is excluded from source control.
           .AddEnvironmentVariables()
           .Build();


       



         region = config[region_var];
         subscriptionKey = config[key_var];
         endpoint = config[endpoint_var];
         documentEndpoint = config[document_endpoint_var];
         sourceBlobSas = config[source_blob_sas];
         targetBlobSas = config[target_blob_sas];
         customCatId = config[custom_translator_cat_id];
         customToLan = config[custom_translator_to_lan];
         textToLans = config.GetSection(text_translation_to_lan).AsEnumerable().Select(a => a.Value).ToArray();


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

      static async Task Main(string[] args)
      {
         var loglevel = SetLogLevel(args);
         logFactory = LoggerFactory.Create(builder =>
         {
            builder.SetMinimumLevel(loglevel);
            builder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddConsole(options =>
            {
               options.FormatterName = "custom";

            });
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
         });
         log = logFactory.CreateLogger("Program");


         Console.ForegroundColor = ConsoleColor.Blue;

         AnsiConsole.Write(new FigletText("Azure AI Translation Demo"));
         Console.WriteLine("This demo tool will allow you to translate text or documents using Azure AI Translator.");
         Console.WriteLine("You can use the standard translator or a custom translator configuration.");
         Console.WriteLine("The custom translator configuration (if any) and the selection of text translation languages are defined in the local.settings.json file.");
         Console.ForegroundColor = ConsoleColor.White;
         string route;
         string customRoute = "";


         if (!string.IsNullOrEmpty(customCatId) && !string.IsNullOrEmpty(customToLan))
         {
            customRoute = $"/translate?api-version=3.0&to={customToLan}&category={customCatId}";
         }

         route = "/translate?api-version=3.0";
         foreach (var lan in textToLans)
         {
            if (!string.IsNullOrEmpty(lan)) route += $"&to={lan}&toScript=latn";
         }


         while (true)
         {

            Console.WriteLine();
            log.LogInformation("Please make a selection:");
            log.LogInformation("1. Translate Text");
            log.LogInformation("2. Translate Documents");
            if (!string.IsNullOrWhiteSpace(customRoute))
            {
               log.LogInformation("3. Translate Text with Custom Translator");
            }
            var entry = Console.ReadLine();
            if (entry.StartsWith("1") || entry.StartsWith("3"))
            {
               if (entry.StartsWith("3") && !string.IsNullOrWhiteSpace(customRoute))
               { 
                  Console.Write("Custom ");
               }
               log.LogInformation(new() { { "Text Translation selected ", ConsoleColor.White }, { "(Multi-line enabled. End your last line with an @ symbol and press return to translate):", ConsoleColor.Cyan } });
               Console.WriteLine();

               log.LogInformation("Type the phrase you'd like to translate? ");
               var sb = new StringBuilder();
               while (true)
               {
                  var txt = Console.ReadLine();
                  if (!txt.EndsWith("@"))
                  {
                     sb.AppendLine(txt);
                  }
                  else
                  {
                     sb.Append(txt.Substring(0, txt.Length - 1));
                     break;
                  }
               }
               string textToTranslate = sb.ToString().Trim();
               Console.WriteLine();

               var routeToUse = route;
               if (entry.StartsWith("3") && !string.IsNullOrWhiteSpace(customRoute))
               {
                  routeToUse = customRoute;
               }

               var res = await TextTranslation.TranslateTextRequest(subscriptionKey, endpoint, routeToUse, textToTranslate, region,log);
               Console.WriteLine();
            }
            else if (entry.StartsWith("2"))
            {
               log.LogInformation("Document Translation selected");
               log.LogInformation("");
               log.LogInformation(new() { { "Please select a target language for translation", ConsoleColor.White }, { "(use the two character language code):", ConsoleColor.DarkYellow } });
               log.LogInformation("If you need help, the codes can be found here: https://learn.microsoft.com/en-us/azure/ai-services/translator/language-support");
               var code = Console.ReadLine();
               Console.WriteLine();
               string path = "";
               while (true)
               {
                  log.LogInformation("Provide the full path to a document to upload and translate:");
                  path = Console.ReadLine().Replace("\"", "");
                  if (!File.Exists(path))
                  {
                     log.LogInformation("File not found. Please try again.", ConsoleColor.Red);
                     continue;
                  }
                  else
                  {
                     break;
                  }
               }
               var fileInfo = new FileInfo(path);
               Console.WriteLine();
               await DocumentTranslation.TranslateBlobDocs(code.ToLower().Trim(), fileInfo, log);

            }
         }
      }

      private static LogLevel SetLogLevel(string[] args)
      {
         var levelIndex = Array.IndexOf(args, "--loglevel");
         if (levelIndex >= 0 && args.Length > levelIndex)
         {
            var logLevel = args[levelIndex + 1];
            if (Enum.TryParse<LogLevel>(logLevel, true, out LogLevel logLevelParsed))
            {
               Program.logLevel = logLevelParsed;
               return logLevelParsed;
            }
         }
         return LogLevel.Information;
      }
   }
}
