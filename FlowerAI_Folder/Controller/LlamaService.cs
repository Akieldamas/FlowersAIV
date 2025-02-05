using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowersAIV.Model;
using Newtonsoft.Json;

namespace FlowersAIV.Controller
{
    public class LlamaService
    {
        private readonly string baseUlr = "http://127.0.0.1:11434/api/chat";
        private readonly HttpClient _httpClient;
        PlantInfo plantInfo;

        public LlamaService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        public async Task<PlantInfo> GetPlantInfo(string plantName, string selectedLanguage)
        {
            var payload = new
            {
                model = "llama3.2:latest",
                messages = new[]
                { //"Please provide the name and scientific, give me certain characteristics such as the origin, toxicity, edibility, fun facts if it exists, etc.), and also give me a description of the {plantName}."
                    new { role = "user",
                                      content = $@"
                    Give me information about the flower {plantName} in {selectedLanguage} in this format:

                    Name: {{CommonName}} ({{ScientificName}}) // Keep the word ""Name"" in English so the code doesn't break.
                    Characteristics: // Keep the word ""Characteristics"" in English so the code doesn't break.
                    - {{Any other relevant characteristic}}: {{Value or ""{selectedLanguage} translation of 'I don’t have information, sorry'""}}
                    - {selectedLanguage} will be used for all sections that aren't 'Name', 'Description' or 'Characteristics'. If no information is available, write ""{selectedLanguage} translation of 'I don’t have information, sorry'"" for the section.

                    Description: // Keep the word ""Description"" in English so the code doesn't break.
                    {{Description or ""{selectedLanguage} translation of 'I don’t have information, sorry'""}}

                    Everything you write that is not 'Name', 'Characteristics', or 'Description' must be in {selectedLanguage}. If no information is available for any section, write ""{selectedLanguage} translation of 'I don’t have information, sorry'""."

                    }
                }
            };
            var jsonContent = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(baseUlr, content);
                response.EnsureSuccessStatusCode();

                var responseString = await CombineContentFromResponse(response);
                return ParseResponse(responseString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        private async Task<string> CombineContentFromResponse(HttpResponseMessage response)
        {
            var combinedContent = new StringBuilder();
            var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                try
                {
                    var responsePart = JsonConvert.DeserializeObject<Dictionary<string, object>>(line);
                    if (responsePart != null && responsePart.ContainsKey("message"))
                    {
                        var message = responsePart["message"] as Newtonsoft.Json.Linq.JObject;
                        var content = message?["content"]?.ToString();
                        if (!string.IsNullOrEmpty(content))
                        {
                            combinedContent.Append(content);
                        }
                    }

                    if (responsePart != null && responsePart.TryGetValue("done", out var isDone) && (bool)isDone)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            return combinedContent.ToString();
        }

        private PlantInfo ParseResponse(string responseString)
        {
            if (string.IsNullOrEmpty(responseString))
            {
                return new PlantInfo
                {
                    NameAndScientificName = "Information not found",
                    Characteristics = "Information not found",
                    Description = "Information not found"
                };
            }

            var plantInfo = new PlantInfo();
            var sections = responseString.Split(new[] { "Characteristics:", "Description:" }, StringSplitOptions.None);
            if (sections.Length > 0)
            {
                var nameSection = sections[0].Trim();
                var nameMatch = System.Text.RegularExpressions.Regex.Match(nameSection, @"Name:\s*(.+?)\s*\((.+?)\)");
                if (nameMatch.Success)
                {
                    var commonName = nameMatch.Groups[1].Value.Trim();
                    var scientificName = nameMatch.Groups[2].Value.Trim();
                    plantInfo.NameAndScientificName = $"{commonName} ({scientificName})";
                }
                else
                {
                    plantInfo.NameAndScientificName = "Information not found";
                }
            }

            if (sections.Length > 1)
            {
                plantInfo.Characteristics = sections[1].Trim();
            }
            else
            {
                plantInfo.Characteristics = "Information not found";
            }

            if (sections.Length > 2)
            {
                plantInfo.Description = sections[2].Trim();
            }
            else
            {
                plantInfo.Description = "Information not found";
            }

            return plantInfo;
        }
    }
}
