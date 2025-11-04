using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialBeeAR
{
    [Serializable]
    public class PhotoActivityInput : ActivityInput
    {
        /// <summary>
        /// ctor
        /// </summary>
        public PhotoActivityInput()
        {
            SamplePhotos = new List<string>();
            ResourceLocalIdentifiers = new List<string>();
            TaggedFriends = new List<string>();
            Keywords = new List<string>();
            AlternateKeywords = new List<string>();
            SortOrder = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// The URLs of the sample photos.
        /// </summary>
        /// <remarks>
        /// Multiple photos in a single API call are already supported by the API.
        /// In AR, for now, we will allow a single photo upload but we will already use a list.
        /// </remarks>
        public IEnumerable<string> SamplePhotos { get; set; }
        /// <summary>
        /// An identifier which persistently identifier the object on a given device.
        /// </summary>
        /// <remarks>
        /// We will use this to try and load resources from the user's device, if present.
        /// If it fails, then the resource will be loaded via the <see cref="ResourceURL"/>.
        /// </remarks>
        public IEnumerable<string> ResourceLocalIdentifiers { get; set; }
        /// <summary>
        /// The list of user IDs of the tagged SB users.
        /// </summary>
        /// <remarks>
        /// This list will be populated every time the user tag a user by typing with the "@" character.
        /// </remarks>
        public IEnumerable<string> TaggedFriends { get; set; }
        /// <summary>
        /// The list of words that will be used to auto-validate the submitted photos
        /// when this activity is consumed.
        /// </summary>
        public IEnumerable<string> Keywords { get; set; }
        /// <summary>
        /// The list of additional words that can be used to further validate the submitted photos
        /// when this activity is consumed.
        /// </summary>
        public IEnumerable<string> AlternateKeywords { get; set; }
        /// <summary>
        /// If true, then this activity will be marked as a challenge
        /// that other users need to complete.
        /// Otherwise, this will just be a POI.
        /// </summary>
        public bool IsChallenge { get; set; }
        /// <summary>
        /// Validates the model.
        /// </summary>
        public new void Validate()
        {
            base.Validate();

            if (!SamplePhotos.Any())
            {
                throw new ArgumentException("SamplePhotos cannot be null or empty.");
            }
            if (IsChallenge && !Keywords.Any())
            {
                throw new ArgumentException("Keywords cannot be null or empty if this instance a challenge.");
            }
        }

        public override string ToString()
        {
            var val = base.ToString();

            val += $" | IsChallenge={IsChallenge} | SamplePhotos={string.Join(",", SamplePhotos)} | Selected Keywords={string.Join(",", Keywords)} | All Keywords={string.Join(",", AlternateKeywords)}";

            return val;
        }
        /// <summary>
        /// Creates an instance of this class from an instance of <see cref="PhotoVideoActivityInfo"/>.
        /// </summary>
        /// <param name="info">The information about the photo/videp.</param>
        /// <param name="experienceId">The Id of the experience.</param>
        /// <param name="location">The user's current location.</param>
        /// <returns></returns>
        /// <remarks>
        /// It's not a responsibility of this class to validate the PhotoVideoActivityInfo instance.
        /// Also, we want to lessen the dependency of this class
        /// so we will not call any singleton classes from this method.
        /// </remarks>        
        public static PhotoActivityInput CreateFrom(PhotoVideoActivityInfo info, string experienceId, string collectionId, Location location, bool isPlanning = false)
        {            
            return new PhotoActivityInput
            {
                Text = info.Title,
                ExperienceId = experienceId,
                BucketId = collectionId,
                Location = location,
                ARInfo = ARDefinition.CreateFrom(info.Pose),

                ParentId = info.ParentId,
                PlacenoteMapID = info.MapId,
                AnchorPayload = info.AnchorPayload,
                
                IsChallenge = info.IsChallenge,
                IsPlanning = isPlanning,
                Keywords = info.Keywords,
                AlternateKeywords = info.AlternateKeywords,
                SamplePhotos = new List<string> { info.ContentURL },
                ResourceLocalIdentifiers = new List<string> { info.ContentPath },
            };
        }
    }

    public class SelectedMedia
    {
        /// <summary>
        /// Base64 encoded string
        /// </summary>
        public string Data { get; set; }
        //public string Type { get; set; }
        /// <summary>
        /// Returns the<see cref= "Data" /> as byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] GetMedia()
        {
            try
            {
                return Convert.FromBase64String(Data);
            }
            catch
            {
                return null;
            }
        }
    }
}

