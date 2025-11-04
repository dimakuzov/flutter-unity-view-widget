using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocialBeeAR
{
    public enum SBAnchorPlacementType
    {
        GPS = 0,
        VPS = 1,
        Marker = 2
    }
    /// <summary>
    /// This represents the context from SocialBee native app
    /// </summary>
    [Serializable]
    public class SBContext
    {
        Configurations configurations { get; set; }

        public void SetConfigurations(Configurations config)
        {
            configurations = config;
        }
        
        /// <summary>
        /// Flag to check if its the first time the anchors are check for distance in the current AR session.
        /// </summary>
        public bool hasAnchorDistanceChecked { get; set; }
        /// <summary>
        /// The maximum distance that determines when the SB anchor shows up.
        /// Beyond this distance, the Google marker should show up.
        /// </summary>
        public int distanceForMarker { get; set; }
        /// <summary>
        /// The maximum distance that determines when the Google marker shows up.
        /// Beyond this distance, the SB indicator should show up.
        /// </summary>
        public int distanceForIndicator { get; set; }        
        /// <summary>
        /// True, if the user is planning for the activity.
        /// </summary>
        public bool isPlanning { get; set; }

        /// <summary>
        /// The name of the location for the planned activity.
        /// </summary>
        public MapLocationInfo plannedLocation { get; set; }        
        /// <summary>
        /// Flag used to determine if the app is manually flagged as offline.
        /// </summary>
        public bool isOffline { get; set; }

        public bool isNativeDebugOn;

        /// <summary>
        /// Social Bee's Bundler Identifier for iOS.
        /// </summary>
        public string bundleIdentifier { get; set; }

        public bool isEditing;
        public bool isCreatingGPSOnlyAnchors;
        /// <summary>
        /// Flag to use if Private VPS is enabled during create.
        /// </summary>
        public bool enablePrivateVPSAnchors;

        /// <summary>
        /// The placement type is now controlled in native.
        /// 0 = GPS
        /// 1 = VPS
        /// 2 = Marker-based
        /// </summary>
        /// <returns></returns>
        public int markerPlacementType;

        public bool IsGPSPlacementType => markerPlacementType == (int)SBAnchorPlacementType.GPS;
        public bool IsVPSPlacementType => markerPlacementType == (int)SBAnchorPlacementType.VPS;
        public bool IsMarkerPlacementType => markerPlacementType == (int)SBAnchorPlacementType.Marker;

        /// <summary>
        /// The Id of the currently logged-in user.
        /// </summary>
        public string userId;

        public int activityCompletionDistanceThreshold;
        public bool startWithPhotoVideo;
        public string experienceId;
        public string experienceName;
        /// <summary>
        /// The name of the blob container.
        /// </summary>
        public string experienceContainerName;

        public string collectionId;
        public string collectionName;
        
        /// <summary>
        /// The path for video on AR video panel in OnBoard.
        /// </summary>
        [Obsolete(message: "Moved to Configurations.")]
        public string onboardingVideoURL;
        public string GetOnboardingVideoURL()
        {
            if (configurations == null || configurations.onboardingVideoURL.IsNullOrWhiteSpace())
                return onboardingVideoURL;
            
            return configurations.onboardingVideoURL;
        }
        
        public string TitleToDisplay
        {
            get
            {
                return !string.IsNullOrWhiteSpace(collectionName) ? collectionName : experienceName;
            }
        }

        /// <summary>
        /// The user's current GPS location.
        /// </summary>
        public Location UserLocation;
        /// <summary>
        /// The Id of the cloud map (or the VPS location) that is currently loaded.
        /// </summary>
        public string mapId { get; set; }
        /// <summary>
        /// The navigation points gathered while the creator walks around creating activities for an experience.       
        /// This will be translated to Vector3.
        /// </summary>
        //public IList<ARDefinition.ARPosition> mapNavPoints { get; set; }
        public string mapNavPoints { get; set; }
        /// <summary>
        /// The Id of the post that was selected in native.
        /// The opening of AR is intiated on per activity specific.
        /// That is true for Editing and Consuming.
        /// When creating, this is nil.
        /// </summary>
        public string initialAnchorId { get; set; }
        /// <summary>
        /// The thumbnail for the anchor of the selected activity.
        /// </summary>
        public string initialThumbnail { get; set; }

        /// <summary>
        /// The Cloud Vision API key.
        /// </summary>
        [Obsolete(message: "Moved to Configurations.")]
        public string visionKey  { get; set; }
        public string GetVisionKey()
        {
            if (configurations == null || configurations.visionKey.IsNullOrWhiteSpace())
                return visionKey;
            
            return configurations.visionKey;
        }
        /// <summary>
        /// The connection string to the Azure blob container.
        /// </summary>
        [Obsolete(message: "Moved to Configurations.")]
        public string blobContainerConnectionString { get; set; }

        public ExperienceStatistics stats { get; set; }
        /// <summary>
        /// These are the anchors from all the maps that in the experience.
        /// </summary>
        public IEnumerable<AnchorDto> anchors { get; set; }
        /// <summary>
        /// These are the anchors that are created in the map represented by <see cref="mapId"/>.
        /// </summary>
        public IEnumerable<AnchorDto> MapAnchors { get; set; }
        /// <summary>
        /// These are the anchors that are created in other maps, other than the the map represented by <see cref="mapId"/>.
        /// </summary>
        public IEnumerable<AnchorDto> OtherAnchors { get; set; }
        /// <summary>
        /// The Vector3 representation of <see cref="mapNavPoints"/>. 
        /// </summary>
        /// <remarks>
        /// This is read-only as we want this class to manage this list.
        /// </remarks>
        public string Breadcrumb
        {
            get
            {
                return mapNavPoints; //.Select(x => x.ToVector3()).ToList();
            }
        }
        //public IDictionary<string, object>[] Activities { get; set; }

        public ARSessionMode Mode { get; private set; }

        public void SetMode(ARSessionMode mode)
        {
            Mode = mode;
        }

        public bool IsConsuming()
        {
            return Mode == ARSessionMode.Consume;
        }

        public bool IsCreating()
        {
            return Mode == ARSessionMode.Create;
        }

        public bool IsCreatingInConsume()
        {
            return Mode == ARSessionMode.CreateInConsume;
        }

        public bool IsOnBoarding()
        {
            return Mode == ARSessionMode.OnBoarding;
        }

        /// <summary>
        /// Holds the Ids of the activities that were created per AR session.
        /// </summary>        
        public IList<string> NewActivities
        {
            get; private set;
        }

        /// <summary>
        /// Keeps track of the number of the uploaded photo, video and audio activities within an AR session.
        /// </summary>
        public int UploadedMedia { get; set; }

        public SBContext()
        {            
            anchors = new List<AnchorDto>();
            OtherAnchors = new List<AnchorDto>();
            MapAnchors = new List<AnchorDto>();
            NewActivities = new List<string>();
        }

        public void StoreNewActivity(string id)
        {
            NewActivities.Add(id);
        }

        public IEnumerable<AnchorDto> SetMapAnchors()
        {
            MapAnchors = anchors.Where(x => x.mapID == mapId);
            return MapAnchors;
        }

        public IEnumerable<AnchorDto> SetOtherAnchors()
        {
            OtherAnchors = anchors.Where(x => x.mapID != mapId);
            return OtherAnchors;
        }

        /// <summary>
        /// Creates an instance of <see cref="AnchorInfoList"/> from this instance.
        /// </summary>
        /// <returns></returns>
        public AnchorInfoList ToAnchorInfoList(Vector3 reticlePosition)
        {
            var anchorInfos = new List<AnchorInfo>();
            
            foreach (var anchor in anchors.Where(x => x.mapID == mapId))
            {
                Debug.Log($"mapId={mapId} | anchorId={anchor.id} | payload={anchor.anchorPayload} #radebug");
                var anchorInfo = new AnchorInfo
                {
                    id = anchor.id,
                    pose = new Pose
                    {
                        position = (reticlePosition.Equals(Vector3.negativeInfinity) ? anchor.position.ToVector3() : reticlePosition),
                        rotation = anchor.rotation.ToQuaternion(),
                    },
                    locationInfo = new ARLocation.Location
                    {
                        Latitude = anchor.latitude,
                        Longitude = anchor.longitude,                        
                    },
                    anchorPayload = anchor.anchorPayload,
                };                
                var post = anchor.activities.FirstOrDefault();
                if (post != null)
                {
                    //print($"caption={post.text} | id={post.id} #spawnanchor");
                    anchorInfo.postInfo = (PostActivityInfo)post.ToActivityInfo(anchorInfo.pose, experienceId, mapId);
                    var location = SBContextManager.Instance.context.plannedLocation;
                    
                    if (location != null)
                    {
                        anchorInfo.postInfo.MapLocation = new MapLocationInfo
                        {
                            Thumbnail = location.Thumbnail,
                            Name = location.Name,
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            Altitude = location.Altitude,
                            Map3DUrl = location.Map3DUrl,
                            Map3DLocalIdentifier = location.Map3DLocalIdentifier,
                            City = location.City,
                            State = location.State,
                            Neighborhood = location.Neighborhood,
                            Country = location.Country,
                            PostalCode = location.PostalCode,
                            PlaceId = location.PlaceId,
                            Rating = location.Rating,
                            Address = location.Address                            
                        };
                    }
                }
                else
                {
                    // This should not happen.
                    anchorInfo.postInfo = new PostActivityInfo
                    {
                        Id = "",
                        Title = "*Untitled",
                        Description = "*"
                    };
                }

                anchorInfo.activityInfoList = anchor.activities
                    .Select(x => x.ToActivityInfo(anchorInfo.pose, experienceId, mapId))
                    .Where(x => x.GetType() != typeof(PostActivityInfo))
                    .ToList();


                anchorInfos.Add(anchorInfo);
            }

            return new AnchorInfoList { mapContent = anchorInfos.ToArray() };
        }

        /// <summary>
        /// Returns the Id of all activities in this context.
        /// </summary>
        public IEnumerable<string> ActivityIds
        {
            get
            {
                return anchors.SelectMany(x => x.activities.Select(a => a.id));
            }
        }

        /// <summary>
        /// Update the location info.
        /// </summary>
        /// <param name="city"></param>
        /// <param name="state"></param>
        /// <param name="neighborhood"></param>
        /// <param name="country"></param>
        /// <returns></returns>
        public Location UpdateLocationInfo(string city, string state, string neighborhood, string country)
        {
            if (UserLocation == null && !isCreatingGPSOnlyAnchors)
            {
                MessageManager.Instance.DebugMessage($"UserLocation is null, getting from lastKnownLocation...");
                UserLocation = SBContextManager.Instance.lastKnownLocation;
            }

            UserLocation.Name = $"{city}, {state}";
            UserLocation.City = city;
            UserLocation.State = state;
            UserLocation.Country = country;
            UserLocation.Neighborhood = neighborhood;

            return UserLocation;
        }

        /// <summary>
        /// Helper method that returns the user's last known location.
        /// </summary>
        /// <returns></returns>
        public Location GetProperLocation()
        {
            if (isPlanning && SBContextManager.Instance.context.plannedLocation != null)
            {
                return SBContextManager.Instance.context.plannedLocation; 
            }
            if (UserLocation == null && !isCreatingGPSOnlyAnchors)
            {
                MessageManager.Instance.DebugMessage($"UserLocation is null, getting from lastKnownLocation...");
                UserLocation = SBContextManager.Instance.lastKnownLocation;
            }

            if (UserLocation == null)
            {
                MessageManager.Instance.DebugMessage($"UserLocation and lastKnownLocation are both null, getting from Unity-GPS...");
                // If the location is still null, let's build from Unity-GPS
                //var location = new LibPlacenote.MapLocation();
                var location = (new LocationService()).lastData;

                UserLocation = new Location
                {
                    Name = $"{location.latitude},{location.longitude}",
                    Latitude = location.latitude,
                    Longitude = location.longitude,
                    Altitude = location.altitude,
                    City = "Unknown",
                    State = "Unknown",
                    Country = "Unknown",
                    Neighborhood = "Unknown",
                };
            }

            return UserLocation;
        }
        /// <summary>
        /// Adds a navigation point to the entire AR session.
        /// </summary>
        /// <param name="crumb"></param>
        public void AddCrumb(Vector3 crumb)
        {
            //mapNavPoints.Add(new ARDefinition.ARPosition
            //{
            //    X = crumb.x,
            //    Y = crumb.y,
            //    Z = crumb.z
            //});
        }

        public override string ToString()
        {
            return $"userId={userId} | experienceId={experienceId} | mapId={mapId} | startWithPhotoVideo={startWithPhotoVideo}";
        }
    }
}