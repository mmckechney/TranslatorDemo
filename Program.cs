// This sample requires C# 7.1 or later for async/await.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
// Install Newtonsoft.Json with NuGet
using Newtonsoft.Json;
using translator_demo;
using translator_demo.Models;
using Spectre.Console;
using Spectre.Console.Rendering;


namespace TranslateTextSample
{

    public class Program
    {

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
            foreach(var lan in textToLans)
            {
                if(!string.IsNullOrEmpty(lan)) route += $"&to={lan}&toScript=latn";
            }


            while (true)
            {

                Console.WriteLine();
                Console.WriteLine("Please make a selection:");
                Console.WriteLine("1. Translate Text");
                Console.WriteLine("2. Translate Documents");
                if (!string.IsNullOrWhiteSpace(customRoute))
                { 
                    Console.WriteLine("3. Translate Text with Custom Translator");
                }
                var entry = Console.ReadLine();
                if (entry.StartsWith("1") || entry.StartsWith("3"))
                {
                    if (entry.StartsWith("3") && !string.IsNullOrWhiteSpace(customRoute))
                    {
                        Console.Write("Custom ");
                    }
                    Console.WriteLine("Text Translation selected (multi-line enabled. End your last line with an @ symbol and press return to translate):");
                    Console.WriteLine();

                    Console.Write("Type the phrase you'd like to translate? ");
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
                    string textToTranslate = sb.ToString();
                    Console.WriteLine();

                    var routeToUse = route;
                    if(entry.StartsWith("3") && !string.IsNullOrWhiteSpace(customRoute))
                    {
                        routeToUse = customRoute;
                    }

                    var res = await TextTranslation.TranslateTextRequest(subscriptionKey, endpoint, routeToUse, textToTranslate, region);
                    Console.WriteLine();
                }
                else if ((entry.StartsWith("2")))
                {
                    Console.WriteLine("Document Translation selected");
                    Console.WriteLine();
                    Console.WriteLine("Please select a target language for translation (use the two character language code):");
                    Console.WriteLine("If you need help, the codes can be found here: https://learn.microsoft.com/en-us/azure/ai-services/translator/language-support");
                    var code = Console.ReadLine();
                    Console.WriteLine();
                    string path = "";
                    while (true)
                    {
                        Console.WriteLine("Provide the full path to a document to upload and translate:");
                        path = Console.ReadLine();
                        if (!File.Exists(path))
                        {
                            Console.WriteLine("File not found. Please try again.");
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    var fileInfo = new FileInfo(path);
                    Console.WriteLine();
                    await DocumentTranslation.TranslateBlobDocs(code.ToLower().Trim(), fileInfo);

                }
            }
        }
    }
}
