using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace UmbracoAI.Web.Services
{
    public class TranslationService
    {
        private readonly string _subscriptionKey;

        private const string TranslateUrlTemplate = "http://api.microsofttranslator.com/v2/http.svc/translate?text={0}&to={1}&category={2}";

        public TranslationService(string subscriptionKey)
        {
            _subscriptionKey = subscriptionKey;
        }

        public async Task<string> TranslateAsync(string translantionText, string targetLanguage)
        {

            using (HttpClient client = new HttpClient())
            {
                string url = string.Format(TranslateUrlTemplate, System.Net.WebUtility.UrlEncode(translantionText), targetLanguage, "general");

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
                var translateResponse = await client.GetAsync(url);

                using (Stream stream = await translateResponse.Content.ReadAsStreamAsync())
                {
                    DataContractSerializer dcs = new DataContractSerializer(typeof(string));
                    string languagesForTranslate = (string)dcs.ReadObject(stream);

                    return languagesForTranslate;
                }
            }
        }

        public List<string> GetLanguages()
        {
            string uri = "https://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForTranslate";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            using (WebResponse response = httpWebRequest.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                DataContractSerializer dcs = new DataContractSerializer(typeof(List<string>));
                List<string> languagesForTranslate = (List<string>)dcs.ReadObject(stream);

                return languagesForTranslate;
            }
        }
    }
}