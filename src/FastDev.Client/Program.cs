using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace FastDev.Client
{
    public class Program
    {
        private static readonly int MaxObjects = 10000;
        private static readonly int DefaultBatchSize = 64;
        private static readonly int MaxBatchSize = 1024;
        public static void Main(string[] args)
        {
            var rand = new Random();

            var uri = new Uri("http://localhost:1972");
            Console.Write($"Base Uri (Default = http://localhost:1972): ");
            while (true)
            {
                Uri value;
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line)) break;
                if (Uri.TryCreate(line, UriKind.Absolute, out value))
                {
                    uri = value;
                    break;
                }

                Console.WriteLine("Error parsing count. Please enter a valid uri address");
            }

            var count = rand.Next(MaxObjects);
            Console.Write($"Count (Default = {count}): ");
            while (true)
            {
                int value;
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line)) break;
                if (int.TryParse(line, out value))
                {
                    count = value;
                    break;
                }

                Console.WriteLine("Error parsing count. Please enter a number");
            }

            var batchSize = DefaultBatchSize;
            Console.Write($"BatchSize (Default = {batchSize}, Max = {MaxBatchSize}): ");
            while (true)
            {
                int value;
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line)) break;
                if (int.TryParse(line, out value))
                {
                    if (1 <= value && value <= MaxBatchSize)
                    {
                        batchSize = value;
                        break;
                    }
                    Console.WriteLine($"Please enter a number between 1 and {MaxBatchSize}");
                }
                Console.WriteLine($"Error parsing count. Please enter a number between 1 and {MaxBatchSize}");
            }

            var simulateErrors = false;
            Console.Write("SimulateErrors (Default = No) yn: ");
            while (true)
            {
                var line = Console.ReadLine().Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(line)) break;
                if(line == "y" || line == "yes")
                {
                    simulateErrors = true;
                    break;
                }
                if(line == "n" || line == "no")
                {
                    break;
                }
                Console.WriteLine("Please enter y = yes or n = no");
            }

            var client = new HttpClient
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromHours(1)
            };

            for (var chunk = 0; chunk * batchSize < count; chunk++)
            {
                var content = new MultipartFormDataContent();
                for (var i = 0; i < batchSize && chunk * batchSize + i < count; i++)
                {
                    if (!simulateErrors || rand.NextDouble() < 0.95) content.Add(new StringContent(Guid.NewGuid().ToString()), $"guid[{i}]");
                    if (!simulateErrors || rand.NextDouble() < 0.95) content.Add(new StreamContent(File.OpenRead($@"..\..\..\..\..\testdata\img_{rand.Next(6) + 1:D2}.jpg")), $"file1[{i}]", $"file1[{i}]");
                    if (!simulateErrors || rand.NextDouble() < 0.95) content.Add(new StreamContent(File.OpenRead($@"..\..\..\..\..\testdata\img_{rand.Next(6) + 1:D2}.jpg")), $"file2[{i}]", $"file2[{i}]");
                    if (!simulateErrors || rand.NextDouble() < 0.95) content.Add(new StreamContent(File.OpenRead($@"..\..\..\..\..\testdata\img_{rand.Next(6) + 1:D2}.jpg")), $"file3[{i}]", $"file3[{i}]");
                }
                var responseTask = client.PostAsync("api/a", content);
                responseTask.Wait();
                var response = responseTask.Result;


                if (response.IsSuccessStatusCode)
                {

                }
                else
                {
                    Console.WriteLine($"An error occured. Status = {response.StatusCode}");
                }
                response.Content.ReadAsStreamAsync();


                var responseContentTask = response.Content.ReadAsStreamAsync();
                responseContentTask.Wait();

                var serializer = new JsonSerializer();
                using (var textReader = new StreamReader(responseContentTask.Result))
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    foreach (var message in serializer.Deserialize<IList<string>>(jsonReader))
                    {
                        Console.WriteLine(message);
                    }
                }

                Console.WriteLine($"Progress: {(double)Math.Min((chunk + 1) * batchSize, count) / count:P}");
            }

            Console.WriteLine("Done!");
        }
    }
}
