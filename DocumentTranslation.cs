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
        static readonly string sourceURL = $"\"{Program.sourceBlobSas}\"";
        static readonly string targetURL = $" \"{Program.targetBlobSas}\"";
        private static HttpClient client = new HttpClient();


        static readonly string json = ("{\"inputs\": [{\"source\": {\"sourceUrl\":" + sourceURL + " ,\"storageSource\": \"AzureBlob\",\"language\": \"en\"}, \"targets\": [{\"targetUrl\":\"<<targetUrl>>\",\"storageSource\": \"AzureBlob\",\"category\": \"general\",\"language\": \"<<targetLanguage>>\"}]}]}");

        public static async Task TranslateBlobDocs(string targetLanguageCode, FileInfo docToTranslate)
        {
            var uploaded = await UploadBlobForTranslation(docToTranslate, targetLanguageCode);
            if(!uploaded)
            {
                Console.WriteLine("File upload failed. Exiting");
                return;
            }

            var jsonWithCode = json.Replace("<<targetLanguage>>", targetLanguageCode);
            (Uri targetUri, string targetSas) = GetContainerUriAndSas(Program.targetBlobSas);
            jsonWithCode = jsonWithCode.Replace("<<targetUrl>>", $"{targetUri.AbsoluteUri}/{targetLanguageCode}?{targetSas}");
            var createTime = DateTime.UtcNow;
            using HttpRequestMessage request = new HttpRequestMessage();
            {

                StringContent content = new StringContent(jsonWithCode, Encoding.UTF8, "application/json");

                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Content = content;

                HttpResponseMessage response = await client.SendAsync(request);
                string result = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Status code: {response.StatusCode}");
                    Console.WriteLine("Document translation accepted");

                   var success =  await CheckTranslationStatus(createTime);
                    if(success)
                    {
                        await DownloadTranslatedDocument(docToTranslate, targetLanguageCode);
                    }
                }
                else
                {
                    Console.WriteLine($"Status code: {response.StatusCode}");
                    Console.Write("Error");
                }

            }

        }

        public static async Task<bool> CheckTranslationStatus(DateTime createdTime) 
        {
            Console.WriteLine("Checking status of document translation:");
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
                        var resObj = JsonSerializer.Deserialize<DocumentResult>(result).Value.First();
                        var status = resObj.Status.ToLower();
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
                                Console.WriteLine($"\tStatus: {resObj.Status}");
                                if (resObj.Error != null)
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

                Thread.Sleep(10000);
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
                var blobClient = containerClient.GetBlobClient($"{targetLanguageCode}/{file.Name}");
                string translatedFileName = Path.Combine(file.Directory.FullName, file.Name.Replace(file.Extension, $"_{targetLanguageCode}{file.Extension}"));
                await blobClient.DownloadToAsync(translatedFileName);
                Console.WriteLine($"Translated document saved to:\t {translatedFileName}");
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
                var blobClient = containerClient.GetBlobClient($"{targetLanguageCode}/{file.Name}");
                blobClient.DeleteIfExists();
            }
            catch(Exception exe)
            {
                Console.WriteLine($"Error: {exe.Message}");
            }
        }
    }
}
