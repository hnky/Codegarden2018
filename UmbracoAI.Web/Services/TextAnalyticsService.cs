using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UmbracoAI.Web.Services
{
    public enum TextAnalyticsFeature { Languages, Sentiment, KeyPhrases }

    public class TextAnalyticsService
    {
        private readonly string _subscriptionKey;

        private string url = "https://westeurope.api.cognitive.microsoft.com/text/analytics/v2.0";

        public TextAnalyticsService(string subscriptionKey)
        {
            _subscriptionKey = subscriptionKey;
        }
    
        public async Task<TextAnalyticsResponseDocument> TextAnalyticsRequestAsync(TextAnalyticsRequestDocument document, TextAnalyticsFeature apiFeature)
        {
            var analyticsRequest = new TextAnalyticsRequest
            {
                Documents = new List<TextAnalyticsRequestDocument>
                {
                    document
                }
            };

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

            var uri = $"{url}/{apiFeature}";
            string requestContent = JsonConvert.SerializeObject(analyticsRequest);

            byte[] byteData = Encoding.UTF8.GetBytes(requestContent);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(uri, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                var documents =  JsonConvert.DeserializeObject<TextAnalyticsResponse>(responseContent);
                if (documents.Documents.Any())
                {
                    return documents.Documents.First();
                }

                return null;
            }
        }
    }


    // Request
    public class TextAnalyticsRequest
    {
        public List<TextAnalyticsRequestDocument> Documents { get; set; }
    }

    public class TextAnalyticsRequestDocument
    {

        public string Id { get; set; }

        public string Text { get; set; }

        public string Language { get; set; }
    }


    // Response
    public class TextAnalyticsResponse
    {
        public List<TextAnalyticsResponseDocument> Documents { get; set; }
    }

    public class TextAnalyticsResponseDocument
    {
        public string Id { get; set; }

        public double Score { get; set; }

        public List<string> KeyPhrases { get; set; }

        public List<TextAnalyticsDetectedLanguage> DetectedLanguages { get; set; }
    }

    public class TextAnalyticsDetectedLanguage
    {
        public string Name { get; set; }

        public string Iso6391Name { get; set; }

        public double Score { get; set; }
    }
}