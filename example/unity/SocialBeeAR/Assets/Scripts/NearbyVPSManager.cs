using System;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.VpsCoverage;

namespace SocialBeeAR
{
    public class NearbyVPSManager : BaseSingletonClass<NearbyVPSManager>
    {
        /// <summary>
        /// The callback function for when the GPS button is tapped.
        /// </summary>
        public Action OnGpsOptionSelected;
        /// <summary>
        /// The callback function when a nearby wayspot is selected on the map.
        /// </summary>
        public Action<LocalizationTarget> OnWayspotSelected;
        /// <summary>
        /// The callback function when a wayspot is selected on the map and it is beyond the threshold.
        /// </summary>
        public Action<LocalizationTarget> OnNavigateToSelectedWayspot;
        
        public Action<IReadOnlyDictionary<string, LocalizationTarget>, ErrorInfo> OnWayspotsFound;

        private List<LocalizationTarget> wayspots = new List<LocalizationTarget>();

        // Threshold Distance in Metres.
        public float distanceThreshold = 50f;

        public LocalizationTarget? selectedWayspot;



        // Start is called before the first frame update
        void Start()
        {
            print("#NearbyVPSManager started");
        }

        private void OnLocalizationTargetsFound(IReadOnlyDictionary<string, LocalizationTarget> targets, ErrorInfo error)
        {
            if (error != null)
            {
                OnWayspotsFound(null, error);
                return;
            }
            
            // Do not handle anything here from the VpsCoverageManager script.
            // Let the InteractionManager handle it.
            OnWayspotsFound(targets, null);
            
            if (!targets.Any())
            {
                // Inform the user that no wayspots are found within the searched area. 
                // The area covered is defined in the VpsCoverageManager script.

                return;
            }
            
            // At this point we determined that there is at least one wayspot within the area.
            // Handle any map-related things here such as rendering the map, rendering the markers, etc.
            print($"Locations found = {targets.Count} | GO={this.gameObject.name}");

            // When generating map markers, use the LocalizationTarget.Identifier as the unique ID of the marker on the map.
            // That ID will be useful when handling the "selected marker" event - responding to the tapping of the marker on the map. 
            wayspots = targets.Values.ToList();

            // Populate Map Here....
            MapBoxManager.Instance.isMapLoading = true;
            MapBoxManager.Instance.loadingPanel.SetActive(true);
            MapBoxManager.Instance.GenerateMap(wayspots);

        }

     
        /// <summary>
        /// Triggers a query from the VPS API searching for VPS-activated locations within the user's current location.
        /// </summary>
        public void SearchWayspots()
        {
            VpsCoverageManager.Instance.OnLocalizationTargetsFound -= OnLocalizationTargetsFound;
            VpsCoverageManager.Instance.OnLocalizationTargetsFound += OnLocalizationTargetsFound;
            VpsCoverageManager.Instance.ShowLocationMessage = false;
            VpsCoverageManager.Instance.StartTrackingUserLocation();
        }
        
        /// <summary>
        /// Triggers a query from the VPS API searching for VPS-activated locations within the specified coordinates.
        /// </summary>
        public void SearchWayspots(LatLng coordinates)
        {
            
        }

        /// <summary>
        /// The method used when the GPS button is tapped.
        /// </summary>
        public void OnWillCreateGPSActivities()
        {
            print($"#NearbyVPSManager 4 OnGpsOptionSelected null? {OnGpsOptionSelected==null}");
            OnGpsOptionSelected();
        }

        /// <summary>
        /// The method that is called when a wayspot marker is selected on the map.
        /// </summary>
        public void OnMapMarkerSelected(LocalizationTarget currentSelctedMarker)
        {
            // ToDo: handle the selected marker here.
            // Build an instance of "LocalizationTarget" containing the information from the selected map marker,
            // and then pass it to the delegate.
            //var selectedMapMarker = new LocalizationTarget(); // this is a temporary init only.
            //var selectedMapMarker = currentSelctedMarker;
            selectedWayspot = currentSelctedMarker;

            // ToDo: for debugging only
            //print($"has selectedMapMarker? = {(VpsCoverageManager.Instance.ClosestWayspot != null)}");
            //selectedMapMarker = VpsCoverageManager.Instance.ClosestWayspot.GetValueOrDefault();
            print($"Name={selectedWayspot.Value.Name} | OnWayspotSelected null? {OnWayspotSelected == null}");

            // Note: The WayspotMarker class is now responsible for
            //      determining the type of action to be taken
            //      when a user taps on a map marker (localize or navigate).
            
            // if the marker is within the distance threshold then call this.
            //OnWayspotSelected(currentSelctedMarker);

            // otherwise, the marker is too far away and is beyond the threshold.
            // call this then --> OnNavigateToSelectedWayspot(selectedMapMarker);
        }
        
        public void OnLocalizeWayspot(LocalizationTarget currentSelctedMarker)
        {
            if (selectedWayspot == null)
            {
                print("There is no selected wayspot!");
                return;
            }

            OnWayspotSelected(selectedWayspot.Value);
        }

        public void OnNavigateToWayspot(LocalizationTarget currentSelctedMarker)
        {
            if (selectedWayspot == null)
            {
                print("There is no selected wayspot!");
                return;
            }
            
            OnNavigateToSelectedWayspot(selectedWayspot.Value);
        }
    }
}
