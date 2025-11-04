using System;

namespace SocialBeeAR
{
    /// <summary>
    /// Holds application-wide information that are needed during the AR session.    
    /// </summary>
    [Serializable]
    public class Configurations
    {
        /// <summary>
        /// The base address of SocialBee's API.
        /// </summary>
        public string apiURL { get; set; }
        /// <summary>
        /// The Cloud Vision API key.
        /// </summary>
        public string visionKey { get; set; }
        /// <summary>
        /// The connection string to the Azure blob container.
        /// </summary>
        public string blobContainerConnectionString { get; set; }
        /// <summary>
        /// The path for video on AR video panel in OnBoard.
        /// </summary>
        public string onboardingVideoURL;
    }

    /// <summary>
    /// A DTO for accepting multiple information from the native app that initializes the AR session.
    /// </summary>
    [Serializable]
    public class ARSessionInitModel
    {
        public Configurations Config { get; set; }
        public AuthorizationToken AuthToken { get; set; }
        public SBContext Context { get; set; }

        public override string ToString()
        {
            var val = "";

            if (Context != null)
            {
                val = $"userId={Context.userId} | experienceId={Context.experienceId}";
            }
            if (AuthToken != null)
            {
                val += $"| authID={AuthToken.Identifier} | token={AuthToken.Token}";
            }
            if (Config != null)
            {
                val += $"| apiURL={Config.apiURL}";
            }
            
            return val;
        }
    }
}
