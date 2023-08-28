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

    public class Program
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
                        var sb = new StringBuilder();   
                        while(true)
                        {
                            var txt = Console.ReadLine();
                            if(!txt.EndsWith("@"))
                            {
                                sb.AppendLine(txt);
                            }
                            else
                            {
                                sb.Append(txt.Substring(0,txt.Length -1));
                                break;
                            }
                        }
                        string textToTranslate = sb.ToString();
                        Console.WriteLine();
                        var res = await TextTranslation.TranslateTextRequest(subscriptionKey, endpoint, route, textToTranslate);
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
        }
    }
}
