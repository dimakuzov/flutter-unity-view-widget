using System.Collections.Generic;

namespace SocialBeeAR
{
    public class AnchorDto
    {
        public string id { get; set; }
        public ARDefinition.ARPosition position { get; set; }
        public ARDefinition.ARRotation rotation { get; set; }
        public string mapID { get; set; }
        /// <summary>
        /// The payload of this wayspot anchor.
        /// </summary>
        public string anchorPayload { get; set; }
        public string thumbnail { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public IEnumerable<ActivityDto> activities { get; set; }
    }
}