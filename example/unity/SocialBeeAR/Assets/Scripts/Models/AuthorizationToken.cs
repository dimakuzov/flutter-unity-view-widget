using System;

namespace SocialBeeAR
{
    /// <summary>
    /// The model used to hold the information about the user's authentication token.
    /// </summary>
    [Serializable]
    public class AuthorizationToken
    {
        /// <summary>
        /// The token.
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// The token's expiration relative to the time it was generated.
        /// </summary>
        public double ExpiresIn { get; set; }
        /// <summary>
        /// Used to validate the source of this data.
        /// </summary>
        public string Identifier { get; set; }
    }

    public class SasURL
    {
        public string sas { get; set; }
    }
}
