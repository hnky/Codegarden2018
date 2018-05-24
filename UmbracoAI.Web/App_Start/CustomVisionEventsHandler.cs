using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Cognitive.CustomVision.Prediction;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using UmbracoAI.Web.Helpers;

namespace UmbracoAI.Web.App_Start
{
    public class CustomVisionEventsHandler : ApplicationEventHandler
    {
        private string _customVisionPredictionKey; 
        private string _customVisionApiProjectId; 

        private readonly MediaFileSystem _fs = FileSystemProviderManager.Current.GetFileSystemProvider<MediaFileSystem>();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            MediaService.Saving += MediaService_Saving;
        }

        private void MediaService_Saving(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            var umbracoHelper = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);

            _customVisionPredictionKey = umbracoHelper.TypedContent(1127).GetPropertyValue<string>("customVisionPredictionKey");
            _customVisionApiProjectId = umbracoHelper.TypedContent(1127).GetPropertyValue<string>("customVisionProjectId");


            if (!string.IsNullOrWhiteSpace(_customVisionPredictionKey) && !string.IsNullOrWhiteSpace(_customVisionApiProjectId))
            {
                PredictionEndpoint endpoint = new PredictionEndpoint()
                {
                    ApiKey = _customVisionPredictionKey
                };

                foreach (IMedia media in e.SavedEntities.Where(a => a.ContentType.Name.Equals(Constants.Conventions.MediaTypes.Image)))
                {
                    string relativeImagePath = ImagePathHelper.GetImageFilePath(media);

                    using (Stream imageFileStream = _fs.OpenFile(relativeImagePath))
                    {
                        var result = endpoint.PredictImage(Guid.Parse(_customVisionApiProjectId), imageFileStream);
                        IEnumerable<string> tags = result.Predictions.Where(a => a.Probability > 0.75).Select(a => a.Tag);
                        media.SetTags("customVisionTags", tags, true);
                    }
                }
            }

        }
    }
}