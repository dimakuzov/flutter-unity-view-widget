//using System;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Serialization;
//using UnityEngine;

//namespace SocialBeeAR
//{
//    public interface ICompletedActivityInfo : IActivityInfo
//    {
//        /// <summary>
//        /// The Id of the activity as a completed activity.
//        /// </summary>
//        /// <remarks>
//        /// When an activity is completed, a new object in the BE is created
//        /// that represents the "completed activity". That object has its own Id.
//        /// </remarks>
//        string CompletedId { get; set; }
//        /// <summary>
//        /// The data this activity was completed.
//        /// </summary>
//        DateTime DateCompleted { get; set; }
//        /// <summary>
//        /// The points earned from completing the activity.
//        /// </summary>
//        int PointsEarned { get; set; }
//        /// <summary>
//        /// Returns a serialized version of this instance.
//        /// </summary>
//        /// <returns></returns>
//        string ToJson();
//    }

//    public class CompletedActivityInfo : ActivityInfo, ICompletedActivityInfo
//    {
//        /// <summary>
//        /// The Id of the activity as a completed activity.
//        /// </summary>
//        /// <remarks>
//        /// When an activity is completed, a new object in the BE is created
//        /// that represents the "completed activity". That object has its own Id.
//        /// </remarks>
//        public string CompletedId { get; set; }
//        /// <summary>
//        /// The data this activity was completed.
//        /// </summary>
//        public DateTime DateCompleted { get; set; }
//        /// <summary>
//        /// The points earned from completing the activity.
//        /// </summary>
//        public int PointsEarned { get; set; }

//        public override IActivityInfo Clone()
//        {
//            throw new NotImplementedException();
//        }

//        public static CompletedActivityInfo CreateFrom(PhotoVideoActivityInfo activity, string id, int pointsEarned, DateTime dateCompleted)
//        {
//            // Let's not do the MemoryStream technique + serialization and deserialization
//            // as it's very expensive and unnecessary.
//            var info = new CompletedActivityInfo
//            {
//                CompletedId = activity.Id,
//                PointsEarned = pointsEarned,
//                DateCompleted = dateCompleted,
//                Status = activity.Status,

//                Id = id,                
//                ExperienceId = activity.ExperienceId,
//                Title = activity.Title,
//                ParentId = activity.ParentId,
//                MapId = activity.MapId,
//                Points = activity.Points,
//                Pose = activity.Pose,
//                IsEditing = false,                                            
//            };
            
//            return info;
//        }


//        /// <summary>
//        /// Returns a serialized version of this instance.
//        /// </summary>
//        /// <returns></returns>
//        public string ToJson()
//        {
//            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
//        }
//    }
//}
