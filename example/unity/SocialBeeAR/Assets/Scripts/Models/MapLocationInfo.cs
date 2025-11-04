namespace SocialBeeAR
{
    /// <summary>
    /// The model that contains the information of the activity's location as reprenseted in Google Map.
    /// </summary>
    public class MapLocationInfo : Location
    {
        /// <summary>
        /// The preview image of the location.
        /// </summary>
        public string Thumbnail { get; set; }
        /// <summary>
        /// The "location on the device" of the snapshot of the map where the location is located in Google Map.
        /// </summary>
        public string Map3DLocalIdentifier { get; set; }
        /// <summary>
        /// The URL of the snapshot of the map where the location is located in Google Map.
        /// </summary>
        public string Map3DUrl { get; set; }         
        /// <summary>
        /// The ID of the location in Google Map.
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// The rating of the location in Google Map.
        /// </summary>
        public double Rating { get; set; }

    }
}