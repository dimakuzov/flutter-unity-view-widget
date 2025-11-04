using Newtonsoft.Json;
using System;
using UnityEngine;
using SocialBeeAR;


namespace SocialBeeARDK
{
    /// <summary>
    /// The interface to the native module, handling the communication with the native module 
    /// </summary>
    public class IntegrationProxy : BaseSingletonClass<IntegrationProxy>
    {
        /// <summary>
        /// The Id of the VPS location.
        /// </summary>
        private string _lastSavedMapId;
        
        // Start is called before the first frame update
        void Start()
        {
            // print($"IntegrationProxy.Start > HasARSessionInitialized={ARHelper.Instance.HasARSessionInitialized}  #arsession-init");
            // DisableLogs();
        }

        // Update is called once per frame
        void Update()
        {

        }

        #region Start Methods
        /// <summary>
        /// This method to be called by native to start AR creation
        /// </summary>
        /// <param name="json"></param>
        public void StartARCreation(string json)
        {
            StartARHelper(json, ARSessionMode.Create);
        }     
        
        public void StartARCreation(SBContext context)
        {
            StartARHelper(context, ARSessionMode.Create);
        }
        
        
        /// <summary>
        /// This method to be called by native to start AR editing
        /// </summary>
        /// <param name="json"></param>
        public void StartAREditing(string json)
        {
            StartAREditingHelper(json, ARSessionMode.Create);
        }

        public void StartAREditing(SBContext context)
        {
            StartAREditingHelper(context, ARSessionMode.Create);
        }

        public void StartOnBoarding(string json) {            
            StartARHelper(json, ARSessionMode.OnBoarding);
        }

        
        /// <summary>
        /// This method to be called by native to start AR consuming
        /// </summary>
        /// <param name="json"></param>
        public void StartARConsuming(string json)
        {
            print($"IntegrationProxy.StartARConsuming - json={json}");

            InteractionManager.Instance.isInitialized = false;
            try
            {
                var model = JsonConvert.DeserializeObject<ARSessionInitModel>(json);
                if (model == null) return;
                if (model.Config != null)
                {
                    model.Context.SetConfigurations(model.Config);
                }
                StartARConsuming(model.Context);
                
                if (model.Config != null)
                {
                    SBRestClient.Instance.SetApiUrl(model.Config.apiURL);
                }                
                SBRestClient.Instance.UpdateAuthToken(model.AuthToken);
            }
            catch
            {
                // print($"IntegrationProxy.StartARConsuming: error deserializing json.");
                // ToDo: log this error and notify ourselves.
                // Create an invalid instance and let the helper handle it.
                StartARConsuming(new SBContext { experienceId = "" });
            }
        }
        
        
        public void StartARConsuming(SBContext context)
        {
            // print($"IntegrationProxy.StartARConsuming: context is null? ={context==null}");
            // print($"IntegrationProxy.StartARConsuming: mapId={context?.mapId?.ToString()}");
            if (InteractionManager.Instance.IsIntegrated)
            {
                if (context == null)
                {
                    print("Cannot start Consuming, either the context is null or there's no active map.");
                    return;
                }
                else if (context.mapId.IsNullOrWhiteSpace())
                {
                    context.isCreatingGPSOnlyAnchors = true;
                }

                _lastSavedMapId = context.mapId;
            }
            else
            {
                print("Stand alone mode");
            }

            //update context info
            if (context != null)
            {
                print($"IntegrationProxy.StartARConsuming: setting mode to consume [isCreatingGPSOnlyAnchors={context.isCreatingGPSOnlyAnchors}]");
                context.SetMode(ARSessionMode.Consume);
                context.stats ??= new ExperienceStatistics
                {
                    Points = 0,
                    Steps = 0,
                    Distance = 0,
                    Elevation = 0,
                    TotalTime = 0
                };
            }            
            SBContextManager.Instance.SetSBContext(context);
            MiniMapManager.Instance.AddContextAnchors(context);
            
            // //set plane detection to default (enabled but hide visualization)
            // PlaneManager.Instance.SetARPlanesVisible(true); 
            // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.Hexagon); 
            // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.Horizontal); 
            ARHelper.Instance.StopPlaneDetection();
            
            //start locating
            InteractionManager.Instance.StartAsIntegratedModule(() =>
            {
                print("LoadMap 4");
                InteractionManager.Instance.LoadMap(_lastSavedMapId);
                
                //set minimap
                // MiniMapManager.Instance.AddContextAnchors(context);
                MiniMapManager.Instance.ShowMiniMap();

                //PointsBarManager.Instance.ShowPointsBar();
                // print($"-=- context.stats.Points = {context.stats.Points}, context.stats.Distance = {context.stats.Distance}");
                PointsBarManager.Instance.SetPointsBar(context.stats, true);
            });
        }


        /// <summary>
        /// This method to be called by native to start AR creation process in Consume.
        /// </summary>
        /// <param name="json"></param>
        public void StartARConsumeCreation(string json)
        {
            StartARHelper(json, ARSessionMode.CreateInConsume);
        }

        public void StartARConsumeCreation(SBContext context)
        {
            StartARHelper(context, ARSessionMode.CreateInConsume);
        }

        public void StartARConsumeEditing(SBContext context)
        {
            StartAREditingHelper(context, ARSessionMode.CreateInConsume);
        }

        public void StartARConsumeEditing(string json)
        {
            StartAREditingHelper(json, ARSessionMode.CreateInConsume);
        }

        void StartARHelper(string json, ARSessionMode mode)
        {
            //print($"1 - IntegrationProxy.StartARHelper: mode={mode} | json={json}");
            print($"1 - IntegrationProxy.StartARHelper: mode={mode} **");
            // print($"1 - IntegrationProxy.StartARHelper: json={json}");
 
            try
            {
                var model = JsonConvert.DeserializeObject<ARSessionInitModel>(json);
                if (model == null) return;
                if (model.Config != null)
                {
                    model.Context.SetConfigurations(model.Config);
                }
                SBContextManager.Instance.context = model.Context;
                if (model.Config != null)
                {
                    SBRestClient.Instance.SetApiUrl(model.Config.apiURL);
                }
                SBRestClient.Instance.UpdateAuthToken(model.AuthToken);
                print($"StartARHelper > StartARHelper > userId={model.Context.userId} | experienceId={model.Context.experienceId}");
                StartARHelper(model.Context, mode);
            }
            catch
            {
                print($"IntegrationProxy.StartARHelper:json cannot be deserialized.");
                // ToDo: log this error and notify ourselves.
                // Create an invalid instance and let the helper handle it.
                StartARHelper(new SBContext { experienceId = "" }, mode);
            }
        }

        void StartARHelper(SBContext context, ARSessionMode mode)
        {
            print($"2 - IntegrationProxy.StartARHelper: HasARSessionInitialized={ARHelper.Instance.HasARSessionInitialized}");
            if (!ARHelper.Instance.HasARSessionInitialized)
            {
                // print("ARHelper.Instance.InitializeARSession...");
                ARHelper.Instance.InitializeARSession();
            }
            
            InteractionManager.Instance.isInitialized = false;
            //update context info
            if (context != null)
            {
                // print("StartARHelper: Setting session mode...");
                context.SetMode(mode);
                print($"StartARHelper: mode={mode} | context = {context}");
                if (context.stats != null)
                {
                    PointsBarManager.Instance.SetPointsBar(context.stats);
                    print("StartARHelper: points bar set.");
                }
                ARHelper.Instance.SetARUser(context.userId);
            }
            // print("StartARHelper: Setting SB context into the SBContextManager...");
            SBContextManager.Instance.SetSBContext(context);
            // print("StartARHelper: Setting SB context DONE.");
            if (context is { startWithPhotoVideo: true })
            {
                UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
                RecordManager.Instance.ClearPaths();
                RecordManager.Instance.startWithPhotoVideo = true;
                RecordManager.Instance.HideARContent();
                UIManager.Instance.HideLoader();
                MiniMapManager.Instance.HideMiniMap();
                PointsBarManager.Instance.HidePointsBar();
                
                InteractionManager.Instance.EnableARSession();
                UIManager.Instance.FadeOutInitCover();
            }
            else
            {
                if (InteractionManager.Instance.IsIntegrated) {
                    //start creator process
                    InteractionManager.Instance.StartAsIntegratedModule(() => {
                        if (mode == ARSessionMode.OnBoarding)
                        {
                            //UIManager.Instance.HideLoader();
                            OnBoardManager.Instance.StartOnBoard();
                            return;
                        }

                        print($"isPlanning={context.isPlanning}, markerPlacementType={context.markerPlacementType}, SBAnchorPlacementType.GPS={ (int)SBAnchorPlacementType.GPS}");
                        if (!context.isPlanning && context.markerPlacementType != (int)SBAnchorPlacementType.GPS)
                        {
                            if (context.markerPlacementType == (int)SBAnchorPlacementType.VPS)
                            {
                                print("StartAsIntegratedModule > ShowNearbyVPS");
                                InteractionManager.Instance.ShowNearbyVPS();
                            }
                            else
                            {
                                print("StartAsIntegratedModule > AskIndoorOrOutdoor");
                                InteractionManager.Instance.AskIndoorOrOutdoor();      
                            }
                        }
                        else //planning
                        {
                            if (context.markerPlacementType == (int)SBAnchorPlacementType.GPS)
                            {
                                SBContextManager.Instance.UpdateIsCreatingGPSOnlyAnchors(true);    
                            }
                            print("StartAsIntegratedModule > OnNewActivityClicked");
                            InteractionManager.Instance.OnNewActivityClicked();
                        }
                    });
                }
                else {
                    InteractionManager.Instance.StartAsStandaloneApp(() => {
                        if (mode == ARSessionMode.OnBoarding)
                        {
                            //UIManager.Instance.HideLoader();
                            OnBoardManager.Instance.StartOnBoard();
                            return;
                        }

                        print($"isPlanning={context.isPlanning}, markerPlacementType={context.markerPlacementType}, SBAnchorPlacementType.GPS={ (int)SBAnchorPlacementType.GPS}");
                        if (!context.isPlanning && context.markerPlacementType != (int)SBAnchorPlacementType.GPS)
                        {
                            if (context.markerPlacementType == (int)SBAnchorPlacementType.VPS)
                            {
                                print("StartAsIntegratedModule > ShowNearbyVPS");
                                InteractionManager.Instance.ShowNearbyVPS();
                            }
                            else
                            {
                                print("StartAsIntegratedModule > AskIndoorOrOutdoor");
                                InteractionManager.Instance.AskIndoorOrOutdoor();      
                            }
                        }
                        else //planning
                        {
                            if (context.markerPlacementType == (int)SBAnchorPlacementType.GPS)
                            {
                                SBContextManager.Instance.UpdateIsCreatingGPSOnlyAnchors(true);    
                            }
                            print("StartAsIntegratedModule > OnNewActivityClicked");
                            InteractionManager.Instance.OnNewActivityClicked();
                        }
                    });
                }
            }
        }

        void StartAREditingHelper(SBContext context, ARSessionMode mode)
        {
            print($"1 - StartAREditingHelper: mode={mode} | context: {context} *");
            
            InteractionManager.Instance.isInitialized = false;
            if (InteractionManager.Instance.IsIntegrated)
            {
                if (context == null)
                {
                    // print($"Cannot start editing because the context is null");
                    return;
                }
                if (context.mapId.IsNullOrWhiteSpace())
                {
                    //// print($"Cannot start editing there's no active map.");
                    //return;
                    print("isCreatingGPSOnlyAnchors=true");
                    context.isCreatingGPSOnlyAnchors = true;                    
                }

                context.isEditing = true;
                _lastSavedMapId = context.mapId;
            }
            else
            {
                print("Stand alone mode");
            }

            //update context info
            if (context != null)
            {
                // print($"IntegrationProxy.StartAREditing: setting mode to {mode}");
                context.SetMode(mode);
                if (context.stats != null)
                    PointsBarManager.Instance.SetPointsBar(context.stats);
            }
            SBContextManager.Instance.SetSBContext(context);

            // // TODO: Debug only
            // foreach (var contextAnchor in SBContextManager.Instance.context.anchors)
            // {
            //     print($"id={contextAnchor.id} | anchorPayload={contextAnchor.anchorPayload}");
            // }
            
            // #placenote2lightship - we don't need this in ARDK?
            // //set plane detection to default (enabled but hide visualization)
            // PlaneManager.Instance.SetARPlanesVisible(true);
            // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.Hexagon); 
            // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.Horizontal); 
            
            //start locating
            if (!ARHelper.Instance.HasARSessionInitialized)
            {
                print("ARHelper.Instance.InitializeARSession...");
                ARHelper.Instance.InitializeARSession();
                ARHelper.Instance.OnTrackingStarted -= state => { };
                ARHelper.Instance.OnTrackingStarted += state =>
                {
                    print($"StartAREditingHelper > Session.currentTrackingMode={ARHelper.Instance.arSession.currentTrackingMode}");
                    if (SBContextManager.Instance.context.IsVPSPlacementType)
                    {
                        WayspotAnchorManager.Instance.RefreshAnchorService();    
                        InteractionManager.Instance.RefreshAnchorServiceState((WayspotLocalizationState)WayspotAnchorManager.Instance.LocalizationState);
                    }
                    StartLocatingInEditMode(context);
                };
            }
            else
            {
                StartLocatingInEditMode(context);
            }
        }

        /// <summary>
        /// This is a helper method to cleanup all handlers with the current AR Session.
        /// </summary>
        public static void ReleaseARSessionDelegates()
        {
            ARHelper.Instance.OnTrackingStarted -= state => { };
        }

        void StartLocatingInEditMode(SBContext context)
        {
            InteractionManager.Instance.StartAsIntegratedModule(() =>
            {
                print("LoadMap 3");
                InteractionManager.Instance.LoadMap(_lastSavedMapId);
                        
                //prepare minimap
                MiniMapManager.Instance.AddContextAnchors(context);
                MiniMapManager.Instance.ShowMiniMap();
                
                //prepare point-bar
                PointsBarManager.Instance.ShowPointsBar();
                    
                //Breadcrumb is only visible when editing in Create or editing own content in Consume.
                NavigationManager.Instance.ConfigureMapData(context.mapId, context.mapNavPoints);
            });
        }

        void StartAREditingHelper(string json, ARSessionMode mode)
        {            
            // print($"StartAREditingHelper: mode={mode} | json = {json}"); 
            try
            {
                var model = JsonConvert.DeserializeObject<ARSessionInitModel>(json);
                if (model == null) return;
                if (model.Config != null)
                {
                    model.Context.SetConfigurations(model.Config);
                }
                
                StartAREditingHelper(model.Context, mode);

                if (model.Config != null)
                {
                    SBRestClient.Instance.SetApiUrl(model.Config.apiURL);
                }
                SBRestClient.Instance.UpdateAuthToken(model.AuthToken);
            }
            catch(Exception ex)
            {
                // print($"StartAREditingHelper: mode={mode} | json cannot be deserialized. ERROR:{ex}");
                // ToDo: log this error and notify ourselves.
                // Create an invalid instance and let the helper handle it.
                StartAREditingHelper(new SBContext { experienceId = "" }, mode);
            }
        }

        /// <summary>
        /// This method is for testing Unity app(with fake context info) only.
        /// </summary>
        /// <returns></returns>
        public void StartStandalone()
        {
            //prepare execution mode info
            InteractionManager.Instance.executionMode = ExecutionMode.Standalone;

            //start
            // InteractionManager.Instance.StartAsStandaloneApp();
        }

        public void StartForTestingARSessionManamagement()
        {
            InteractionManager.Instance.executionMode = ExecutionMode.TestingARSession;
            UIManager.Instance.HideLoader();
            UIManager.Instance.FadeOutInitCover();
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.TestARSession);
        }
        
        #endregion
        private void DisableLogs()
        {
            Debug.unityLogger.logEnabled = SBContextManager.Instance.context.isNativeDebugOn;
        }
        
        /// <summary>
        /// The method called by the native app to update the app configurations.
        /// </summary>
        /// <param name="json">A json instance of Configurations.</param>
        public void UpdateConfigurations(string json)
        {
            try
            {
                // print($"IntegrationProxy.UpdateConfigurations - json={json}");

                var config = JsonConvert.DeserializeObject<Configurations>(json);
                SBRestClient.Instance.SetApiUrl(config.apiURL);
            }
            catch
            {
                // ToDo: log this error and notify ourselves.                
                // print("IntegrationProxy.UpdateConfigurations: Cannot deserialize the token value.");
            }         
        }
        
        /// <summary>
        /// The method called by the native app to update the authorization token.
        /// </summary>
        /// <param name="json">A json instance of the auth token.</param>
        public void UpdateAuthToken(string json)
        {
            // TBD
        }
    }
}