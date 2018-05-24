using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using UmbracoAI.Web.Helpers;
using UmbracoAI.Web.Models;
using File = System.IO.File;

namespace UmbracoAI.Web.App_Start
{
    public class ComputerVisionEventsHandler : ApplicationEventHandler
    {
        private readonly string _visionApiKey = ConfigurationManager.AppSettings["VisionApiKey"];
        private readonly string _visionApiUrl = ConfigurationManager.AppSettings["VisionApiUrl"];

        private readonly MediaFileSystem _fs = FileSystemProviderManager.Current.GetFileSystemProvider<MediaFileSystem>();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            MediaService.Saving += MediaService_Saving;
        }

        private void MediaService_Saving(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            VisionServiceClient visionServiceClient = new VisionServiceClient(_visionApiKey, _visionApiUrl);

            foreach (IMedia media in e.SavedEntities.Where(a => a.ContentType.Alias.Equals(Constants.Conventions.MediaTypes.Image)))
            {
                string relativeImagePath = ImagePathHelper.GetImageFilePath(media);
                AnalysisResult computervisionResult;

                // Computer Vision API
                using (Stream imageFileStream = _fs.OpenFile(relativeImagePath))
                {
                    // Call the Computer Vision API
                    computervisionResult = visionServiceClient
                        .AnalyzeImageAsync(
                            imageFileStream,
                            new[]
                            {
                                VisualFeature.Description,
                                VisualFeature.Adult,
                                VisualFeature.Tags,
                                VisualFeature.Categories
                            },
                            new[]
                            {
                                "celebrities", "landmarks"
                            }
                        ).Result;

                    // Get the result and set the values of the ContentItem
                    var celebrityTags = new List<string>();
                    var landmarksTags = new List<string>();

                    foreach (Category category in computervisionResult.Categories.Where(a => a.Detail != null))
                    {
                        var detailResult = JsonConvert.DeserializeObject<DetailsModel>(category.Detail.ToString());
                        celebrityTags.AddRange(detailResult.Celebrities.Select(a => a.Name));
                        landmarksTags.AddRange(detailResult.Landmarks.Select(a => a.Name));
                    }

                    IEnumerable<string> tags = computervisionResult.Tags.Select(a => a.Name);
                    string caption = computervisionResult.Description.Captions.First().Text;
                    bool isAdult = computervisionResult.Adult.IsAdultContent;
                    bool isRacy = computervisionResult.Adult.IsRacyContent;

                    media.SetTags("tags", tags, true);
                    media.SetTags("celebrities", celebrityTags, true);
                    media.SetTags("landmarks", landmarksTags, true);
                    media.SetValue("description", caption);
                    media.SetValue("isAdult", isAdult);
                    media.SetValue("isRacy", isRacy);

                    
                }

                // Computer Vision => OCR
                using (Stream imageFileStream = _fs.OpenFile(relativeImagePath))
                {
                    var boundingBoxes = new List<Rectangle>();
                    var textLines = new List<string>();


                    OcrResults result = visionServiceClient.RecognizeTextAsync(imageFileStream).Result;

                    if (result.Regions.Any())
                    {
                        boundingBoxes = result.Regions.SelectMany(a => a.Lines.Select(b => b.Rectangle)).ToList();
                        textLines.AddRange(result.Regions.SelectMany(a => a.Lines).Select(line => string.Join(" ", line.Words.Select(a => a.Text))));


                        var totalarea = (computervisionResult.Metadata.Height * computervisionResult.Metadata.Width);
                        var coveredArea = boundingBoxes.Sum(a => (a.Height * a.Width));
                        double percentageConvered = 100.0/totalarea*coveredArea;

                        media.SetValue("hasText",true);
                        media.SetValue("textOnTheImage", string.Join("\n",textLines));
                        media.SetValue("percentageCovered", percentageConvered);
                    }

                    
                }
            }
        }
    }
}