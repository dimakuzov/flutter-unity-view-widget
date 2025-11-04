namespace SocialBeeAR
{
    /// <summary>
    /// The location of a resource (i.e. experience, collection or activity).
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Required.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Required.
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// Required.
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// Required.
        /// </summary>
        public double Altitude { get; set; }
        /// <summary>
        /// The street, apartment or building number of the location.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Required.
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// Required.
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Country { get; set; }
        public string PostalCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// If we are using the Geocoding API: https://developers.google.com/maps/documentation/geocoding/intro#GeocodingResponses.
        /// If not then, while this is optional, we need to find a way to get this information. 
        /// </remarks>
        public string Neighborhood { get; set; }
        /// <summary>
        /// Returns true if the location has all the required fields.
        /// </summary>
        public bool IsValid()
        {            
            return !Name.IsNullOrWhiteSpace() && !City.IsNullOrWhiteSpace() && !State.IsNullOrWhiteSpace()
                // It is very unlikely that people will be in the Null Island (in the Atlantic Ocean)
                // when they create an activity. So a coordinate of 0,0 indicates we forgot to set it
                // and thus is invalid.
                && Latitude != 0 && Longitude != 0;
        }

        public string FormattedAddress
        {
            get
            {
                var address = $"{City}, {State}";
                if (!string.IsNullOrWhiteSpace(Neighborhood))
                {
                    address = $"{Neighborhood}, {address}";
                }
                return address;
            }
        }

        public override string ToString()
        {
            return $"Name={Name} | City={City} | State={State}";
        }
    }
}
