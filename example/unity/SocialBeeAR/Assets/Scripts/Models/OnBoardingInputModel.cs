using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SocialBeeAR
{
    [Serializable]
    public class OnBoardingInputModel
    {
        /// <summary>
        /// The name of the user.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Auto-generated information about the user's onboarding experience.
        /// </summary>
        public PostOutput Post { get; set; }
        /// <summary>
        /// The photo the user took during the onboarding process.
        /// </summary>
        public PhotoOutput Photo { get; set; }
        /// <summary>
        /// The selected categories.
        /// </summary>
        public IEnumerable<string> PreferredCategories { get; set; }
        /// <summary>
        /// Total points earned from completing the onboarding process.
        /// </summary>
        public int Points { get; set; }

        public void CreatePost(Location location)
        {
            var experienceId = SBContextManager.Instance.context.experienceId;
            Post = new PostOutput
            {
                UniqueId = Guid.NewGuid().ToString(),
                Text = "Social Bee Signup Experience",
                ExperienceId = experienceId,
                Location = location,
                ParentId = experienceId,
                PointsCreation = 5,
                SortOrder = 0,
            };
            Points = Post.PointsCreation;
        }

        /// <summary>
        /// Add the completed photo to the model.
        /// </summary>
        /// <param name="caption">The caption of the photo.</param>
        /// <param name="localPath">The ResourceLocalIdentifier of the photo.</param>
        /// <param name="url">The URL of the photo.</param>
        public void AddPhoto(string caption, string localPath, string url)
        {
            Photo = new PhotoOutput
            {
                UniqueId = Guid.NewGuid().ToString(),
                Text = caption,
                ResourceLocalIdentifiers = new List<string> { localPath },
                SamplePhotos = new List<string> { url },
                PointsCreation = 10,
                SortOrder = 1,                
            };
            Points += Photo.PointsCreation;
            if (Post!=null)
            {
                Photo.ParentId = Post.UniqueId;
                Photo.ExperienceId = Post.ExperienceId;
                Photo.Location = Post.Location;
            }
        }

        /// <summary>
        /// Returns a serialized version of this instance.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }
    }
}