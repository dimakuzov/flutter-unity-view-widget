using UnityEngine;

namespace SocialBeeAR
{
    public class ARDefinition  
    {
        public ARPosition Position { get; set; }
        public ARRotation Rotation { get; set; }

        public class ARPosition
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public Vector3 ToVector3()
            {
                return new Vector3
                {
                    x = (float)X,
                    y = (float)Y,
                    z = (float)Z
                };
            }
        }

        public class ARRotation : ARPosition
        {
            public double W { get; set; }

            public Quaternion ToQuaternion()
            {
                return new Quaternion
                {
                    x = (float)X,
                    y = (float)Y,
                    z = (float)Z,
                    w = (float)W
                };
            }
        }

        public string MapId { get; set; }

        /// <summary>
        /// Create an instance of this class from a <see cref="Pose"/> object.
        /// </summary>
        /// <param name="pose"></param>
        /// <returns></returns>
        public static ARDefinition CreateFrom(Pose pose)
        {
            if (pose == null) return null;

            return new ARDefinition
            {
                Position = new ARPosition
                {
                    X = pose.position.x,
                    Y = pose.position.y,
                    Z = pose.position.z,
                },
                Rotation = new ARRotation
                {
                    X = pose.rotation.x,
                    Y = pose.rotation.y,
                    Z = pose.rotation.z,
                    W = pose.rotation.w,
                }
            };
        }
    }

    // Consumer process we will load all activities for a single experience.
    // 1. We will use the  ExperienceId =   "CEPgRF1gTE2VTi9fAEwD8A==".
    // 2. Every activities have the ARDefinition. At this point can we respawn the anchors?
    //      - Activities in different anchors in a single map.
    //      - A. Post + Trivia (on a table) > Complete > New Activity
    //      - B. Post + Trivia (on a longer table) > Complete > New Activity
    //      - C. Post + Trivia (on a wall) > Complete > Then Save map

    // Save map call SB API pass: mapID + experienceID
    // 
}