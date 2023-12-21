using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace translator_demo
{
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Transactions;
    using TranslateTextSample;
    using translator_demo.Models;

    class DocumentTranslation
    {
        private static readonly string endpoint = $"{Program.documentEndpoint}translator/text/batch/v1.1";
        private static readonly string key = Program.subscriptionKey;


        static readonly string route = "/batches";
        private static HttpClient client = new HttpClient();
        private static string RequestJson { get; set; }
        private static string TargetFileName { get; set; }
        public static async Task TranslateBlobDocs(string targetLanguageCode, FileInfo docToTranslate)
        {
            (Uri targetUri, string targetSas) = GetContainerUriAndSas(Program.targetBlobSas);
            (Uri sourceUri, string sourceSas) = GetContainerUriAndSas(Program.sourceBlobSas);
            TargetFileName = $"{targetLanguageCode}/{Path.GetFileNameWithoutExtension(docToTranslate.Name)}_{targetLanguageCode}{Path.GetExtension(docToTranslate.Name)}";
            var reqDoc = new DocumentRequest()
            {
                Inputs = new List<Input>
                {
                    new Input
                    {
                        Source = new Source
                        {
                            SourceUrl = $"{sourceUri.AbsoluteUri}/{docToTranslate.Name}?{sourceSas}"//Program.sourceBlobSas
                        },
                        Targets = new List<Target>
                        {
                            new Target
                            {
                                TargetUrl = $"{targetUri.AbsoluteUri}/{DocumentTranslation.TargetFileName}?{targetSas}",
                                Language = targetLanguageCode
                            }
                        }
                    }
                }
            };
            DocumentTranslation.RequestJson = JsonSerializer.Serialize<DocumentRequest>(reqDoc, new JsonSerializerOptions() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            //Console.WriteLine(DocumentTranslation.RequestJson);
            var uploaded = await UploadBlobForTranslation(docToTranslate, targetLanguageCode);
            if(!uploaded)
            {
                Console.WriteLine("File upload failed. Exiting");
                return;
            }
          

            var createTime = DateTime.UtcNow;
            using HttpRequestMessage request = new HttpRequestMessage();
            {

                StringContent content = new StringContent(DocumentTranslation.RequestJson, Encoding.UTF8, "application/json");

                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Content = content;

                HttpResponseMessage response = await client.SendAsync(request);
                string result = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    var operationUrl = response.Headers.GetValues("Operation-Location").FirstOrDefault();

                    Console.WriteLine($"Status code: {response.StatusCode}");
                    Console.WriteLine("Document translation accepted");

                   var success =  await CheckTranslationStatus(operationUrl);
                    if(success)
                    {
                        await DownloadTranslatedDocument(docToTranslate, targetLanguageCode);
                    }
                }
                else
                {
                    Console.WriteLine($"Status code: {response.StatusCode}");
                    if (result.ToLower().Contains("invalidtolanguage"))
                    {
                        Console.WriteLine($"Invalid target language code. Please try again with a valid code.{Environment.NewLine}See https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support for a list of supported language codes");
                    }
                    else
                    {
                        Console.WriteLine(result);
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                }

            }

        }

        public static async Task<bool> CheckTranslationStatus(string operationUrl) 
        {
            int sleepTime = 2000;
            Console.WriteLine("Checking status of document translation:");
            while(true)
            {
                using HttpRequestMessage request = new HttpRequestMessage();
                {

                    StringContent content = new StringContent(DocumentTranslation.RequestJson, Encoding.UTF8, "application/json");

                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri(operationUrl);
                    request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                    request.Content = content;

                    HttpResponseMessage response = await client.SendAsync(request);
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        DocumentResult resObj = null;
                        try
                        {
                            resObj = JsonSerializer.Deserialize<DocumentResult>(result);
                        }
                        catch(Exception)
                        {
                            Console.WriteLine("Waiting for response");
                            Thread.Sleep(sleepTime);
                        }
                        var status = resObj?.Status?.ToLower();
                        switch(status)
                        {
                            case "notstarted":
                                Console.WriteLine("\tWaiting for translation to start");
                                break;
                            case "running":
                                Console.WriteLine("\tTranslation in progress");
                                break;
                            case "succeeded":
                                Console.WriteLine("\tTranslation complete");
                                Console.WriteLine($"Total Characters Charged: {resObj.Summary.TotalCharacterCharged}");
                                return true;
                              case "validationfailed":
                                Console.WriteLine($"\tTranslation failed: {resObj.Error?.Message}");
                                return false;
                            default:
                                Console.WriteLine($"\tStatus: {resObj?.Status}");
                                if (resObj?.Error != null)
                                {
                                    Console.WriteLine($"\tError message: {resObj.Error.Message}");
                                    return false;
                                }
                                break;

                        }
                    }
                    else
                    {
                        Console.WriteLine($"Status code: {response.StatusCode}");
                        Console.Write("Error");
                        return false;
                    }

                }

                Thread.Sleep(sleepTime);
            }
        }

        public static async Task<bool> UploadBlobForTranslation(FileInfo file, string targetLanguageCode)
        {
            try
            {
                (Uri containerUrl, string signature) = GetContainerUriAndSas(Program.sourceBlobSas);
                var containerClient = new BlobContainerClient(containerUrl, new AzureSasCredential(signature));

                //upload the file to blob storage using the sourceURL SAS token
                var blobClient = containerClient.GetBlobClient(file.Name);
                var result = await blobClient.UploadAsync(file.FullName, true);
                //validate that the upload was successful
                if (result.GetRawResponse().Status == 201)
                {
                    Console.WriteLine("File uploaded successfully");
                    DeletePreExistingFile(file, Program.targetBlobSas, targetLanguageCode);
                    return true;
                }
                else
                {
                    Console.WriteLine("File upload failed");
                    return false;
                }

                //Delete any pre-translated document
                
            }
            catch(Exception exe)
            {
                Console.Write($"Error: {exe.Message}");
                return false;
            }
        }

        public static async Task<bool> DownloadTranslatedDocument(FileInfo file, string targetLanguageCode)
        {
            try
            {
                (Uri containerUrl, string signature) = GetContainerUriAndSas(Program.targetBlobSas);
                var containerClient = new BlobContainerClient(containerUrl, new AzureSasCredential(signature));
                var blobClient = containerClient.GetBlobClient($"{DocumentTranslation.TargetFileName}");
                string localFile = Path.Combine(file.Directory.FullName, Path.GetFileName(DocumentTranslation.TargetFileName));
                await blobClient.DownloadToAsync(localFile);
                Console.WriteLine($"Translated document saved to:\t {localFile}");
                return true;
            }
            catch(Exception exe)
            {
                Console.WriteLine($"Error: {exe.Message}");
                return false;
            }

        }

        private static (Uri containerUri, string containerSas) GetContainerUriAndSas(string fullSasUrl)
        {
            var containerUri = new Uri(fullSasUrl);
            var containerUrl = new Uri($"{containerUri.Scheme}://{containerUri.Host}{containerUri.AbsolutePath}");
            var signature = $"{containerUri.Query.Substring(1)}";

            return (containerUrl, signature);
        }
        private static void DeletePreExistingFile(FileInfo file, string containerSasUri, string targetLanguageCode)
        {
            try
            {
                (Uri containerUrl, string signature) = GetContainerUriAndSas(containerSasUri);
                var containerClient = new BlobContainerClient(containerUrl, new AzureSasCredential(signature));
               var blobClient = containerClient.GetBlobClient($"{DocumentTranslation.TargetFileName}");
                blobClient.DeleteIfExists();
            }
            catch(Exception exe)
            {
                Console.WriteLine($"Error: {exe.Message}");
            }
        }
    }
}
