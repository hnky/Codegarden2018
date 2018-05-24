using System;
using System.Collections.Generic;

namespace UmbracoAI.Web.Models
{
    public class CortanaCommentModel
    {
        public CortanaCommentModel()
        {
            Comments = new List<CommentModel>();
        }

        public List<CommentModel> Comments { get; set; }
    }

    public class CommentModel
    {
        public string Text { get; set; }

        public DateTime PostTime { get; set; }

        public string Language { get; set; }

        public double Sentiment { get; set; }

        public int Id { get; set; }
    }
}