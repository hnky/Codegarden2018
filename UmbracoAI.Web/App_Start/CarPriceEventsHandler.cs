using Newtonsoft.Json;
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
    public class CarPriceEventsHandler : ApplicationEventHandler
    {
        private readonly string _apiKey = ConfigurationManager.AppSettings["CarPricePredictorKey"];

        //   private readonly MediaFileSystem _fs = FileSystemProviderManager.Current.GetFileSystemProvider<MediaFileSystem>();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentService.Saving += ContentService_Saving;
        }

        private void ContentService_Saving(IContentService sender, SaveEventArgs<IContent> e)
        {
            // var textAnalyticsService = new TextAnalyticsService(_textAnalyticsApiKey);

            CarPriceService service = new CarPriceService(_apiKey);
            var umbracoHelper = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);


            foreach (IContent content in e.SavedEntities.Where(a => a.ContentType.Alias == "carAdvert"))
            {

                var model = umbracoHelper.TypedContent(content.GetValue("car"));

                var inputModel = new CarPriceService.InputRequest();
                inputModel.Year = content.GetValue<int>("year");
                inputModel.Mileage = content.GetValue<int>("mileage");
                inputModel.Model = model.Name;
                inputModel.Make = model.Parent.Name;

                var result = AsyncHelpers.RunSync(() => service.InvokeRequestResponseService(inputModel));

                content.SetValue("suggestedPrice", result);
            }
        }
    }







}