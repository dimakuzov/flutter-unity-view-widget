using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocialBeeAR
{
    [Serializable]
    public class MapActivitiesInput : RestSerializable
    {
        public MapActivitiesInput()
        {
            Activities = new List<string>();
            //Breadcrumb = new List<ARDefinition.ARPosition>();
        }
        /// <summary>
        /// The Id of the map in Placenote.
        /// </summary>
        public string MapId { get; set; }
        /// <summary>
        /// The list of activities that will be linked to the map.
        /// </summary>
        public IEnumerable<string> Activities { get; set; }
        /// <summary>
        /// The Id of the experience that the activities belongs to.
        /// </summary>
        public string ExperienceId { get; set; }
        
        public string Breadcrumb { get; set; }
        /// <summary>
        /// True, if the user is creating activities in Consume.
        /// </summary>
        public bool CreateInConsume { get; set; }
        ///// <summary>
        ///// The navigation path.
        ///// </summary>
        //public IEnumerable<ARDefinition.ARPosition> Breadcrumb { get; private set; }

        //public void SetBreadcrumb(IEnumerable<Vector3> path)
        //{
        //    Breadcrumb = path.Select(v => new ARDefinition.ARPosition
        //    {
        //        X = v.x,
        //        Y = v.y,
        //        Z = v.z
        //    });            
        //}
        /// <summary>
        /// Validates if the input has the minimum required values: ExperienceId, MapId, and at least one activity.
        /// </summary>
        /// <remarks>
        /// Let's throw an error to inform/remind devs of the required and valid params.        
        /// </remarks>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ExperienceId))
                throw new ArgumentException("ExperienceId cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(MapId))
                throw new ArgumentException("MapId cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(Breadcrumb))
                throw new ArgumentException("BreadcrumbsJSON cannot be null or empty.");
            
            if (Activities == null || Activities.Count() < 1)
                throw new ArgumentException("There should be at least one activity.");
        }

        public override string ToString()
        {
            return $"MapId={MapId} | activities: {string.Join(", ", Activities)}";
        }
    }
}