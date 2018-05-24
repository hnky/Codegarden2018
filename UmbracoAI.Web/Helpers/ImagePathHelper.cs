using System;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web.Models;

namespace UmbracoAI.Web.Helpers
{
    public class ImagePathHelper
    {
        public static string GetImageFilePath(IMedia media)
        {
            try
            {
                string umbracoFile = media.GetValue<string>(Constants.Conventions.Media.File);
                return JsonConvert.DeserializeObject<ImageCropDataSet>(umbracoFile).Src;
            }
            catch (Exception e)
            {
                try
                {
                    return media.GetValue<string>(Constants.Conventions.Media.File);
                }
                catch (Exception exception)
                {
                    return string.Empty;
                }

            }
        }
    }
}