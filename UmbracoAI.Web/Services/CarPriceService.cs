using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace UmbracoAI.Web.Services
{
    public class CarPriceService
    {
        private readonly string _apiKey;

        public CarPriceService(string apiKey) {
            _apiKey = apiKey;
        }

        public async Task<int> InvokeRequestResponseService(InputRequest request)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, List<Dictionary<string, string>>>() {
                        {
                            "input1",
                            new List<Dictionary<string, string>>(){new Dictionary<string, string>(){
                                            {
                                                "Year", request.Year.ToString()
                                            },
                                            {
                                                "Mileage", request.Mileage.ToString()
                                            },
                                            {
                                                "Make", request.Make
                                            },
                                            {
                                                "Model", request.Model
                                            },
                                }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };

               
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/def1fdd5bc8d4654a9076d9c10466748/services/2b91fc5745f74a1897d8391126df3f04/execute?api-version=2.0&format=swagger");

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();

                    ResponseOutput output = JsonConvert.DeserializeObject<ResponseOutput>(result);
                    return Convert.ToInt32(output.Results.output1.First().SuggestedPrice);
                }
      
              
            }

            return 0;
        }


        public class InputRequest
        {

            public int Mileage { get; set; }

            public int Year { get; set; }

            public string Make { get; set; }

            public string Model { get; set; }

        }


        public class Output1
        {
            public double SuggestedPrice { get; set; }
        }

        public class Results
        {
            public List<Output1> output1 { get; set; }
        }

        public class ResponseOutput
        {
            public Results Results { get; set; }
        }



    }
}