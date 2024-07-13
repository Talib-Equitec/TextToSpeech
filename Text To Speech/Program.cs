using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace Text_To_Speech
{
    internal class Program
    {
       
        static async Task Main(string[] args)
        {
            
            string getsourceID = await AddPDFToAi();
            string inputText = await GetSummaryFromAi(getsourceID) ;
            string outputFilePath = "C:\\Users\\Equilap41\\Downloads\\output.wav";

            // Make the API request
            string speechApiKey = string.Empty;
            string model = "tts-1";
            string voice = "alloy";
            byte[] response = CreateSpeech(speechApiKey, model, voice, inputText);

            // Save the response to a file
            if (response != null)
            {
                SaveToFile(response, outputFilePath);
                Console.WriteLine($"Audio saved to {outputFilePath}");
            }
            else
                Console.WriteLine("Failed to get a valid response from the API.");
        }

        static async Task<string> AddPDFToAi()
        {
            string apiKey = string.Empty;
            string filePath = "C:\\Users\\Equilap41\\Downloads\\Amber Enterprises - May24_RN.pdf";
            string url = "https://api.chatpdf.com/v1/sources/add-file";
            #region add pdf file to ai
            using (HttpClient client = new HttpClient())
            {
                using (MultipartFormDataContent form = new MultipartFormDataContent())
                {
                    // Add API key to headers
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                    // Add file content to the form
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        StreamContent fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                        form.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

                        try
                        {
                            HttpResponseMessage response = await client.PostAsync(url, form);

                            if (response.IsSuccessStatusCode)
                            {
                                string responseData = await response.Content.ReadAsStringAsync();
                                dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(responseData);
                                Console.WriteLine("Source ID: " + result.sourceId);
                                string resultSourceId = result.sourceId;
                                return resultSourceId;
                            }
                            else
                            {
                                string errorData = await response.Content.ReadAsStringAsync();
                                Console.WriteLine("Error: " + response.ReasonPhrase);
                                Console.WriteLine("Response: " + errorData);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception: " + ex.Message);
                        }
                    }
                }
                return string.Empty;
            }
            #endregion
        }

        static async Task<string> GetSummaryFromAi(string GetSourceId)
        {

            #region give prompt to ai for creating summary
            string url = "https://api.chatpdf.com/v1/chats/message";
            string apiKey = string.Empty;

            var data = new
            {
                sourceId = GetSourceId,
                messages = new[]
                {
                     new
                     {
                         role = "user",
                         content = "can you make 400 words summary  for this report. i want more comprehensive summary based on the information provided in the report."
                     }
                 }
            };

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string json = JsonConvert.SerializeObject(data);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    dynamic result;
                    if (response.IsSuccessStatusCode)
                    {
                        string responseData = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject(responseData);
                        string contentString = result.content;
                        Console.WriteLine("Result: " + result.content);
                        return contentString;
                    }
                    else
                    {
                        string errorData = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Error: " + response.ReasonPhrase);
                        Console.WriteLine("Response: " + errorData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
            return string.Empty;
            #endregion
        }

        static byte[] CreateSpeech(string apiKey, string model, string voice, string inputText)
        {

            ChatClient Chatclient = new ChatClient(model: "gpt-4o", apiKey);

            ChatCompletion completion = Chatclient.CompleteChat("Say 'this is a test.'");

            Console.WriteLine($"[ASSISTANT]: {completion}");

            RestClient client = new RestClient("https://api.openai.com/v1/audio/speech");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Authorization", $"Bearer {apiKey}");
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                model = model,
                voice = voice,
                input = inputText
            };
            request.AddJsonBody(body);

            RestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
                return response.RawBytes;
            else
            {
                Console.WriteLine($"Error: {response.StatusDescription}");
                return null;
            }
        }

        static void SaveToFile(byte[] data, string filePath)
        {
            File.WriteAllBytes(filePath, data);
        }

    }
}