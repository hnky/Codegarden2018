using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.WebApi;
using UmbracoAI.Web.Models;

namespace UmbracoAI.Web.Controllers
{
    public class CortanaController : UmbracoApiController
    {
        // GET: Cortana
        public CortanaCommentModel GetLatestComment()
        {
            var rootContent = Umbraco.TypedContentAtRoot().FirstOrDefault();
            var comments = rootContent.DescendantsOrSelf("comment").Where(a => a.GetPropertyValue<bool>("isNew")).OrderByDescending(a => a.CreateDate);


            var model = new CortanaCommentModel();

            foreach (IPublishedContent commentContent in comments)
            {
                model.Comments.Add(
                    new CommentModel()
                    {
                        Sentiment = commentContent.GetPropertyValue<double>("sentiment"),
                        PostTime = commentContent.CreateDate,
                        Text = commentContent.GetPropertyValue<string>("text_EN"),
                        Language = commentContent.GetPropertyValue<string>("language"),
                        Id = commentContent.Id
                    }
                );
            }

            return model;
        }

        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [System.Web.Http.HttpGet]
        public string ApproveComment(int contentId)
        {

            IContent content = Services.ContentService.GetById(contentId);
            content.SetValue("isNew",false);
            content.SetValue("isApproved", true);

            Services.ContentService.SaveAndPublishWithStatus(content);

            return "done";
        }

        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [System.Web.Http.HttpGet]
        public void DeleteComment(int contentId)
        {
            IContent content = Services.ContentService.GetById(contentId);
            content.SetValue("isNew", false);
            content.SetValue("isApproved", false);
            Services.ContentService.Save(content);
            Services.ContentService.UnPublish(content);
        }


    }
}