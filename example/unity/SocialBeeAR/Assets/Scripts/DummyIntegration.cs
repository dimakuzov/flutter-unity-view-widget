using System;
using System.Collections.Generic;
using SocialBeeARDK;

namespace SocialBeeAR
{

    /// <summary>
    /// This class handles the button click on the temporary entrance UI, which is for test running in different execution mode.
    /// </summary>
    public class DummyIntegration : BaseSingletonClass<DummyIntegration>
    {


        /// <summary>
        /// Launch AR main scene and start continuous activity creation process
        /// </summary>
        public void LaunchARCreation()
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(MakeupDummyInitModel());
            print($"standalone > LaunchARCreation > json={json}");
            IntegrationProxy.Instance.StartARCreation(json);
        }

        /// <summary>
        /// Launch AR main scene and start consuming process
        /// </summary>
        public void LaunchARConsuming()
        {
            print("standalone > LaunchARConsuming");
            SBContext context = MakeupDummyContext();
            IntegrationProxy.Instance.StartARConsuming(context);
        }


        /// <summary>
        /// Launch AR main scene and start consuming process, but as creator, which is actually a 'preview' mode.
        /// </summary>
        public void LaunchARConsumingAsCreator()
        {
            print("standalone > LaunchARConsumingAsCreator");
            SBContext context = MakeupDummyContext();
            context.SetMode(ARSessionMode.Create);
            IntegrationProxy.Instance.StartAREditing(context);
        }


        /// <summary>
        /// (For internal testing only) Launch AR main scene in standalone mode.
        /// </summary>
        public void LaunchARStandalone()
        {
            IntegrationProxy.Instance.StartStandalone();
        }


        private ARSessionInitModel MakeupDummyInitModel()
        {
            return new ARSessionInitModel
            {
                Context = MakeupDummyContext(),
                Config = new Configurations
                {   
                    apiURL = "https://www.bonsaimediagroup.com/",
                },
                AuthToken = new AuthorizationToken
                {
                    Identifier = "SBAPI",
                    Token = "INVALID_TOKEN",
                    ExpiresIn = DateTime.Now.AddDays(30).TimeOfDay.Ticks
                }
            };
        }
        private SBContext MakeupDummyContext()
        {
            SBContext fakeContext = new SBContext()
            {
                enablePrivateVPSAnchors = true,
                
                markerPlacementType = (int)SBAnchorPlacementType.VPS,
                
                experienceId = "EXP_001",
                experienceName = "Seattle City Tour",
                collectionId = "ACTG_001",
                collectionName = "Seattle Art Museum",
                userId = Guid.NewGuid().ToString(),
                stats = new ExperienceStatistics
                {
                  Distance  = 0,
                  Elevation = 0,
                  Points = 0,
                  Steps = 0,
                  TotalTime = 0
                },
                startWithPhotoVideo = false
                
            };

            return fakeContext;
        }


        private ActivityInfo MakeupDummyActivityInfo()
        {
            Dictionary<string, string> dummyActivitySpecific = new Dictionary<string, string>();
            dummyActivitySpecific.Add("PoIDescription", "This is a dummy PoI");

            var dummyActInfo = new PostActivityInfo()
            {
                //activityId = Utilities.GenerateActivityId(),
                //type = ActivityType.Post,
                //activitySpecific = dummyActivitySpecific
                Id = Utilities.GenerateActivityId(),
                Title = "",
                Description = "This is a dummy PoI"
            };

            return dummyActInfo;
        }
    }

}
