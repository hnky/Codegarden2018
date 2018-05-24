using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using UmbracoAI.Web.Helpers;
using File = System.IO.File;


namespace UmbracoAI.Web
{

    public class FaceEventsHandler : ApplicationEventHandler
    {

        private readonly string _faceApiKey = ConfigurationManager.AppSettings["FaceApiKey"];
        private readonly string _faceApiUrl = ConfigurationManager.AppSettings["FaceApiUrl"];
        private readonly string _faceApiGroup = ConfigurationManager.AppSettings["FaceApiGroup"];

        private readonly MediaFileSystem _fs = FileSystemProviderManager.Current.GetFileSystemProvider<MediaFileSystem>();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            MemberService.Saving += MemberService_Saving;
            MemberService.Deleting += MemberService_Deleting;
            CreateFaceGroup();
        }

        /* Stap 1 -> Face API: Try to create the face group */
        private void CreateFaceGroup()
        {
            try
            {
                var faceServiceClient = new FaceServiceClient(_faceApiKey, _faceApiUrl);
                AsyncHelpers.RunSync(() => faceServiceClient.CreatePersonGroupAsync(_faceApiGroup, _faceApiGroup));
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void MemberService_Saving(IMemberService sender, SaveEventArgs<IMember> e)
        {

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            var faceServiceClient = new FaceServiceClient(_faceApiKey, _faceApiUrl);

            foreach (IMember member in e.SavedEntities)
            {

                var profileImage = member.GetValue<string>("profilePicture");

                if (!string.IsNullOrWhiteSpace(profileImage))
                {
                    var profileImageUdi = Udi.Parse(profileImage);
                    var profileImageMedia = umbracoHelper.TypedMedia(profileImageUdi);

                    string fullPath = _fs.GetFullPath(profileImageMedia.Url);

                    /* Stap 2  -> Face API: Delete the person if exists */
                    if (!string.IsNullOrWhiteSpace(member.GetValue<string>("personId")))
                    {
                        try
                        {
                            var personId = Guid.Parse(member.GetValue<string>("personId"));
                            AsyncHelpers.RunSync(() => faceServiceClient.DeletePersonAsync(_faceApiGroup, personId));
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    /* Stap 3 -> Face API: Detect face and attributes */
                    using (Stream imageFileStream = _fs.OpenFile(fullPath))
                    {
                        Face[] detectface = AsyncHelpers.RunSync(
                            () => faceServiceClient.DetectAsync(imageFileStream,
                                false, false, new[]
                                {
                                    FaceAttributeType.Age,
                                    FaceAttributeType.Gender,
                                    FaceAttributeType.Glasses,
                                    FaceAttributeType.Makeup,
                                    FaceAttributeType.Hair,
                                }));

                        // Getting values and setting the properties on the member
                        string age = detectface.First().FaceAttributes.Age.ToString();
                        string gender = detectface.First().FaceAttributes.Gender;
                        string glasses = detectface.First().FaceAttributes.Glasses.ToString();
                        bool eyeMakeup = detectface.First().FaceAttributes.Makeup.EyeMakeup;
                        bool lipMakeup = detectface.First().FaceAttributes.Makeup.LipMakeup;

                        member.SetValue("Age", age);
                        member.SetValue("Gender", gender);
                        member.SetValue("glasses", glasses);
                        member.SetValue("eyeMakeup", eyeMakeup);
                        member.SetValue("lipMakeup", lipMakeup);
                    }

                    // ==> Stap 4 -> Create a person in the persongroup 
                    CreatePersonResult person = AsyncHelpers.RunSync(() => faceServiceClient.CreatePersonAsync(_faceApiGroup, member.Name));

                    member.SetValue("personId", person.PersonId.ToString());

                    // ==> Stap 5 -> Add face to person and make persistent 
                    using (Stream imageFileStream = _fs.OpenFile(fullPath))
                    {
                        AddPersistedFaceResult result = AsyncHelpers.RunSync( () => faceServiceClient.AddPersonFaceAsync(_faceApiGroup, person.PersonId, imageFileStream));
                        member.SetValue("faceId", result.PersistedFaceId.ToString());
                    }
                }
            }

            // ==> Stap 6 -> Train the facegroup
            AsyncHelpers.RunSync(() => faceServiceClient.TrainPersonGroupAsync(_faceApiGroup));
        }

        private void MemberService_Deleting(IMemberService sender, DeleteEventArgs<IMember> deleteEventArgs)
        {
            var faceServiceClient = new FaceServiceClient(_faceApiKey, _faceApiUrl);

            foreach (IMember member in deleteEventArgs.DeletedEntities)
            {
                if (!string.IsNullOrWhiteSpace(member.GetValue<string>("personId")))
                {
                    try
                    {
                        var personId = Guid.Parse(member.GetValue<string>("personId"));
                        AsyncHelpers.RunSync(() => faceServiceClient.DeletePersonAsync(_faceApiGroup, personId));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }
}