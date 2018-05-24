using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using UmbracoAI.Web.Helpers;
using File = Umbraco.Core.Models.File;

namespace UmbracoAI.Web.App_Start
{
    public class FaceEventsMediaHandler : ApplicationEventHandler
    {

        private readonly string _faceApiKey = ConfigurationManager.AppSettings["FaceApiKey"];
        private readonly string _faceApiUrl = ConfigurationManager.AppSettings["FaceApiUrl"];
        private readonly string _faceApiGroup = ConfigurationManager.AppSettings["FaceApiGroup"];

        private readonly MediaFileSystem _fs = FileSystemProviderManager.Current.GetFileSystemProvider<MediaFileSystem>();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            MediaService.Saving += MediaService_Saving;
        }

        /* ==> Stap 7 -> Detect faces and match them to Umbraco Members */
        private void MediaService_Saving(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            FaceServiceClient faceServiceClient = new FaceServiceClient(_faceApiKey, _faceApiUrl);
            IMemberService memberService = ApplicationContext.Current.Services.MemberService;

            foreach (IMedia media in e.SavedEntities.Where(a => a.ContentType.Alias.Equals(Constants.Conventions.MediaTypes.Image)))
            {
                string relativeImagePath = ImagePathHelper.GetImageFilePath(media);
               // string fullPath = _fs.GetFullPath(relativeImagePath);

                using (Stream imageFileStream = _fs.OpenFile(relativeImagePath))
                {
                    var faces = AsyncHelpers.RunSync( () => faceServiceClient.DetectAsync(imageFileStream));

                    if (faces.Any())
                    {
                        Guid[] faceIds = faces.Select(a => a.FaceId).ToArray();
                        IdentifyResult[] results = AsyncHelpers.RunSync(() => faceServiceClient.IdentifyAsync(_faceApiGroup, faceIds, 5));

                        var matchedPersons = new List<IMember>();

                        foreach (IdentifyResult identifyResult in results)
                        {
                            foreach (var candidate in identifyResult.Candidates)
                            {
                                IEnumerable<IMember> searchResult = memberService.GetMembersByPropertyValue("personId", candidate.PersonId.ToString());
                                matchedPersons.AddRange(searchResult);
                            }
                        }

                        if (matchedPersons.Any())
                        {
                            media.SetValue("persons", string.Join(",", matchedPersons.Select(a => a.GetUdi().ToString())));
                        }
                    }
                }
            }
        }


    }
}