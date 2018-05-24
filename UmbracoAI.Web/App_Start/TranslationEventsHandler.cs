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
    public class TranslationEventsHandler : ApplicationEventHandler
    {
        private readonly string _translationApiKey = ConfigurationManager.AppSettings["TranslationApiKey"];

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentService.Saving += ContentService_Saving;
        }

        private void ContentService_Saving(IContentService sender, SaveEventArgs<IContent> e)
        {
            var translationService = new TranslationService(_translationApiKey);

            foreach (IContent content in e.SavedEntities.Where(a => a.ContentType.Alias == "comment"))
            {
                var textToBeTranslated = content.GetValue<string>("text");

                var translatedTextNl = AsyncHelpers.RunSync(() => translationService.TranslateAsync(textToBeTranslated, "nl"));
                content.SetValue("text_NL", translatedTextNl);

                var translatedTextDk = AsyncHelpers.RunSync(() => translationService.TranslateAsync(textToBeTranslated, "da"));
                content.SetValue("text_DK", translatedTextDk);

                var translatedTextDe = AsyncHelpers.RunSync(() => translationService.TranslateAsync(textToBeTranslated, "de"));
                content.SetValue("text_DE", translatedTextDe);

                var translatedTextEn = AsyncHelpers.RunSync(() => translationService.TranslateAsync(textToBeTranslated, "en"));
                content.SetValue("text_EN", translatedTextEn);

                var translatedTextTlh = AsyncHelpers.RunSync(() => translationService.TranslateAsync(textToBeTranslated, "tlh"));
                content.SetValue("text_Tlh", translatedTextTlh);


                
            }
        }
    }
}