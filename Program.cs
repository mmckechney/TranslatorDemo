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
            string route;
            if (!string.IsNullOrEmpty(customCatId) && !string.IsNullOrEmpty(customToLan))
            {
                route = $"/translate?api-version=3.0&to={customToLan}&category={customCatId}";
                Console.Write($"Using custom translator to translate to {customToLan}");
            }
            else
            {
                route = "/translate?api-version=3.0&to=de&toScript=latn&to=it&toScript=latn&to=ja&toScript=latn&to=hi&toScript=latn&to=en&toScript=latn&to=es&toScript=latn&category=619cb84d-bb3a-4cd3-9d3d-629777113e7b-ENERGY";
            }

            while (true)
            { 
               
            Console.WriteLine();
                Console.WriteLine("Please make a selection:");
                Console.WriteLine("1. Translate Text");
                Console.WriteLine("2. Translate Documents");
                var entry = Console.ReadLine();
            if (entry.StartsWith("1"))
            {
                Console.WriteLine("Text Translation selected (end your line with an @ symbol and press return to translate):");
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
                var res = await TextTranslation.TranslateTextRequest(subscriptionKey, endpoint, route, textToTranslate);
                Console.WriteLine();




            }
            else if ((entry.StartsWith("2")))
            {
                Console.WriteLine("Document Translation selected");
                Console.WriteLine();
                await DocumentTranslation.TranslateBlobDocs();

            }
            }
        }
    }
}
