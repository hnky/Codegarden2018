using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using UmbracoAI.Web.Helpers;
using UmbracoAI.Web.Services;

namespace UmbracoAI.Web.App_Start
{
    public class TextAnalyticsEventsHandler : ApplicationEventHandler
    {
        private readonly string _textAnalyticsApiKey = ConfigurationManager.AppSettings["TextAnalyticsApiKey"];

        //   private readonly MediaFileSystem _fs = FileSystemProviderManager.Current.GetFileSystemProvider<MediaFileSystem>();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentService.Saving += ContentService_Saving;
        }

        private void ContentService_Saving(IContentService sender, SaveEventArgs<IContent> e)
        {
            var textAnalyticsService = new TextAnalyticsService(_textAnalyticsApiKey);


            foreach (IContent content in e.SavedEntities.Where(a => a.ContentType.Alias == "comment"))
            {
                var request = new TextAnalyticsRequestDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = content.GetValue<string>("text")
                };

                var analyticsResponse = AsyncHelpers.RunSync(() => textAnalyticsService.TextAnalyticsRequestAsync(request,TextAnalyticsFeature.Languages));
                string isoLanguageCode = analyticsResponse.DetectedLanguages.First().Iso6391Name;

                var analyticsSentimentResponse = AsyncHelpers.RunSync(() => textAnalyticsService.TextAnalyticsRequestAsync(request, TextAnalyticsFeature.Sentiment));
                double sentimentScore = analyticsSentimentResponse.Score;

                var analyticsKeyPhrasesResponse = AsyncHelpers.RunSync(() => textAnalyticsService.TextAnalyticsRequestAsync(request, TextAnalyticsFeature.KeyPhrases));
                string keyPhrases = string.Join("\n",analyticsKeyPhrasesResponse.KeyPhrases);




                content.SetValue("language", isoLanguageCode);
                content.SetValue("sentiment", Math.Round(sentimentScore,2));
                content.SetValue("keyPhrases", keyPhrases);

            }
        }
    }
}