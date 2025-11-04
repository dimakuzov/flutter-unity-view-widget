using System;
using UnityEngine;

namespace SocialBeeAR
{
    [Serializable]
    public class CheckInInput : ActivityInput
    {
        /// <summary>
        /// Creates an instance of this class from the user's current location.
        /// </summary>        
        /// <param name="experienceId">The Id of the experience.</param>
        /// <param name="location">The user's current location.</param>
        /// <returns></returns>
        /// <remarks>
        /// It's not a responsibility of this class to validate the PostActivityInfo instance.
        /// Also, we want to lessen the dependency of this class
        /// so we will not call any singleton classes from this method.
        /// </remarks>        
        public static CheckInInput CreateFrom(string experienceId, Location location, Pose pose, bool isPlanning = false)
        {            
            return new CheckInInput
            {                
                ExperienceId = experienceId,
                Location = location,
                ARInfo = ARDefinition.CreateFrom(pose),
                IsPlanning = isPlanning
            };
        }
    }     
}

