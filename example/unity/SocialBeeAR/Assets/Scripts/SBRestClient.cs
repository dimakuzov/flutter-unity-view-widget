using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SocialBeeAR
{     
    /// <summary>
    /// Represents the error from the API.
    /// </summary>
    public class ServerResponse
    {
        /// <summary>
        /// If the request is to create a new resource then this is the ID of the created resource.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// If the API call fails, this returns the error code and the message containing the details of the error.
        /// </summary>
        public ErrorInfo Error { get; set; }

        public class ErrorInfo
        {
            public int ErrorCode { get; set; }
            public string Message { get; set; }
        }
    }

    /// <summary>
    /// Handles API calls. 
    /// </summary>
    /// <remarks>
    /// This should encapsulate the Web API calls
    /// so that when change plugins, we do not have to change everywhere,
    /// and only change the implementation in this class.
    /// Contains the definition of the SB Web API endpoints.
    /// </remarks>    
    public class SBRestClient : BaseSingletonClass<SBRestClient>
    {     
        public override void Awake()
        {
            base.Awake();
            // HttpClient needs to be singleton.
            var handler = new TimeoutHandler
            {
                InnerHandler = new HttpClientHandler()
            };
            httpClient = new HttpClient(handler);
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        private void Start()
        {
            nativeCall = NativeCall.Instance;
            Timer.Instance.OnTimerTriggered += OnTimerTriggered;
        }

        private void OnDestroy()
        {
            Timer.Instance.OnTimerTriggered -= OnTimerTriggered;
        }

        void OnTimerTriggered()
        {            
            //if (webRequest == null) return;

            //var timeElapsed = DateTime.Now.Subtract(webRequestExecutionTime.GetValueOrDefault(DateTime.Now));         
            //if (timeElapsed.TotalSeconds > 120)// && webRequest.uploadProgress < 0.5)
            //{
            //    print("UploadBlobIEnumerator timed out.");
            //    webRequest.Dispose();
            //    webRequest = null;
            //    willCancelUploadBlobIEnumerator = true;
            //    uploadBlobTimeout?.Invoke();
            //}
        }

        /// <summary>
        /// Sets the base API URL.
        /// </summary>
        /// <param name="url"></param>
        public void SetApiUrl(string url)
        {
            apiURL = $"{url}/api/";
            httpClient.BaseAddress = new Uri(apiURL);
        }

        void RestoreApiUrl()
        {
            httpClient.BaseAddress = new Uri(apiURL);
        }         

        void TryAddHeader(string name, string value)
        {
            TryRemoveHeader(name);            
            try
            {
                httpClient.DefaultRequestHeaders.Add(name, value);
            }
            catch (Exception ex)
            {
                // ToDo: log this error and notify ourselves.                
                print($"SBRestClient.TryAddHeader for {name} with value {value}: Exception = {ex.Message}.");
            }
        }

        void TryRemoveHeader(string name)
        {
            try
            {
                // ToDo: We're encounter an error here so we wrapped this in a try-catch:
                //      MissingMethodException: SocialBeeAR.SBRestClient.UpdateAuthToken Due to: Attempted to access a missing member.
                if (httpClient.DefaultRequestHeaders.Contains(name))
                {
                    httpClient.DefaultRequestHeaders.Remove(name);
                }
            }
            catch (Exception ex)
            {
                // ToDo: log this error and notify ourselves.                
                print($"SBRestClient.TryRemoveHeader for {name}: Exception = {ex.Message}.");
            }
        }

        void RemoveAuthToken()
        {
            TryRemoveHeader("Authorization");             
        }

        void RestoreAuthToken()
        {
            UpdateAuthToken(token); 
        }

        /// <summary>
        /// Updates the authorization token of the currently logged in user.
        /// </summary>
        /// <param name="token"></param>
        public void UpdateAuthToken(AuthorizationToken token)
        {
            print($"SBRestClient.UpdateAuthToken started: counter={tokenRefreshCounter}.");
            this.token = token;
            tokenRefreshCounter = 0;
            if (httpClient == null)
            {
                print($"SBRestClient.UpdateAuthToken: reinitializing httpClient.");
                // httpClient is null when the native app calls this Unity app the second time.                
                httpClient = new HttpClient(new TimeoutHandler { InnerHandler = new HttpClientHandler() });
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
            }

            print($"SBRestClient.UpdateAuthToken: setting the token in the header.");
            TryRemoveHeader("Authorization");
            TryAddHeader("Authorization", $"Bearer {token.Token}");            
        }

        /// <summary>
        /// Returns true if the token has not yet expired.
        /// </summary>
        /// <returns></returns>
        bool IsValidToken()
        {
            var expiration = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            print($"IsValidToken > ExpiresIn={token.ExpiresIn} #debugconsume");
            print($"Compare to date={DateTime.Now.CompareTo(expiration.AddSeconds(token.ExpiresIn).ToLocalTime()) } #debugconsume");
            return token != null && DateTime.Now.CompareTo(expiration.AddSeconds(token.ExpiresIn).ToLocalTime()) < 0;
        }

        /// <summary>
        /// Checks if the auth token has not yet expired.
        /// If the token is expired, the method will trigger a native call.
        /// Provide the callback that the native code will trigger to resume processing.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public bool ValidateTokenFor(string callback)
        {
            if (!IsValidToken()) // This is for debugging only, forcing a token refresh ->> || tokenRefreshCounter == 0
            {
                print("Auth token is invalid! #debugconsume");
                identifier = Guid.NewGuid().ToString();
                // Try and refresh the token twice.
                if (tokenRefreshCounter > 2) return false;

                //print("Getting a fresh auth token...");
                nativeCall.GetCurrentUserAuthToken(identifier, callback);
                tokenRefreshCounter += 1;
                OnWillUpdateAuthToken(identifier);
                return false;
            }
            else
            {
                UpdateAuthToken(token);
            }

            return true;
        }

        IEnumerator CheckInToggleEnumerator(string id, bool isCheckIn, string callback, Action<HttpResponseMessage> onAfterSubmit)
        {
            Func<string, bool, string, HttpResponseMessage> api = CheckInToggle;
            var apiResult = api.BeginInvoke(id, isCheckIn, callback, null, null);
            while (!apiResult.IsCompleted)
            {
                print($"Waiting for SBRestClient.CheckInToggleEnumerator to finish...");
                yield return new WaitForEndOfFrame();
            }
            onAfterSubmit(api.EndInvoke(apiResult));
        }

        HttpResponseMessage CheckInToggle(string id, bool isCheckIn, string callback)
        {
            print("---------------------- SubmitToApi -----------------------");
            print($"isCheckIn={isCheckIn}");
            activityId = id;

            #region Validation             
            //
            // Let's throw an error from here to inform/remind devs of the required and valid params.             
            if (!ValidateTokenFor(callback))
                return null;

            #endregion

            var endpoint = string.Format(Endpoints.PostCheckIn, id);
            var response = isCheckIn
                ? httpClient.PutAsync(endpoint, null).Result
                : httpClient.DeleteAsync(endpoint).Result;

            return response;
        }

        IEnumerator SubmitToApiEnumerator(string id, IActivityInput input, string callback, string endpoint, Action<HttpResponseMessage> onAfterSubmit)
        {
            // ToDo: test http error handler
            //if (input.Text == "error*")
            //{
            //    throw new ApplicationException();
            //}

            Func<string, IActivityInput, string, string, HttpResponseMessage> api = SubmitToApi;
            print($"Waiting for SBRestClient.SubmitToApiEnumerator to finish...");
            var apiResult = api.BeginInvoke(id, input, callback, endpoint, null, null);
            while (!apiResult.IsCompleted)
            {                
                yield return new WaitForEndOfFrame();
            }
            print($"Waiting for SBRestClient.SubmitToApiEnumerator DONE.");
            onAfterSubmit(api.EndInvoke(apiResult));
        }        

        HttpResponseMessage SubmitToApi(string id, IActivityInput input, string callback, string endpoint)
        {
            var isEditing = !id.IsNullOrWhiteSpace();
            //MessageManager.Instance.ClearDebug();
            print("---------------------- SubmitToApi -----------------------");
            print($"{(isEditing ? "Editing" : "Creation")} started.");
            activityId = id;

            #region Validation             
            //
            // Let's throw an error from here to inform/remind devs of the required and valid params.

            input.Validate();

            if (!ValidateTokenFor(callback))
                return null;

            #endregion

            print(isEditing ? $"Updating activity @ {apiURL}{endpoint}/{id}" : $"Posting activity @ {apiURL}{endpoint}");

            HttpMethod method = isEditing ? HttpMethod.Put : HttpMethod.Post;
            var uri = isEditing ? $"{endpoint}/{id}" : $"{endpoint}";
            var request = new HttpRequestMessage(method, uri);
            request.SetTimeout(TimeSpan.FromSeconds(60));
            request.Content = input.ToStringContent();            

            //var response = isEditing
            //    ? httpClient.PutAsync($"{endpoint}/{id}", input.ToStringContent()).Result
            //    : httpClient.PostAsync($"{endpoint}", input.ToStringContent()).Result;
         
            var response = httpClient.SendAsync(request).Result;

            return response;
        }

        IEnumerator CompleteActivityApiEnumerator(string id, IConsumeActivityInput input, string callback, string endpoint, Action<HttpResponseMessage> onAfterSubmit)
        {
            Func<string, IConsumeActivityInput, string, string, HttpResponseMessage> api = CompleteActivityApi;
            var apiResult = api.BeginInvoke(id, input, callback, endpoint, null, null);
            while (!apiResult.IsCompleted)
            {
                // print($"Waiting for SBRestClient.CompleteActivityApiEnumerator to finish...");
                yield return new WaitForEndOfFrame();
            }
            onAfterSubmit(api.EndInvoke(apiResult));
        }

        HttpResponseMessage CompleteActivityApi(string id, IConsumeActivityInput input, string callback, string endpoint)
        {            
            if (id.IsNullOrWhiteSpace())
                throw new ArgumentNullException("id cannot be null or empty.");
 
            activityId = id;

            #region Validation             
            //
            // Let's throw an error from here to inform/remind devs of the required and valid params.

            print($"Validating the input #debugconsume");
            input.Validate();
            print("The input is valid... #debugconsume");

            if (!ValidateTokenFor(callback))
            {
                MessageManager.Instance.DebugMessage("Auth token is not valid!");
                return null;
            }
            MessageManager.Instance.DebugMessage("Auth token is valid");

            #endregion

            print($"Completing activity @ {apiURL}{endpoint}");
            var sc = input.ToStringContent().ReadAsStringAsync().Result;
            print($"content: {sc}");
            
            var response = httpClient.PostAsync($"{endpoint}", input.ToStringContent()).Result;

            return response;
        }

        void FinalizeSubmission<T>(bool isEditing, IActivityInput input, HttpResponseMessage response, OnAfterSubmission callback)
            where T : ActivityOutput<T>
        {
            var responseResult = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {                                
                print($"FinalizeSubmission: isEditing={isEditing} | mapID={input.PlacenoteMapID}");                
                print($"FinalizeSubmission response: {responseResult}");
                var output = ActivityOutput<T>.Create(responseResult);
                if (output==null)
                {
                    print($"FinalizeSubmission: output is null? {output==null}");
                }
                output.IsPlanning = input.IsPlanning;
                if (!isEditing && input.PlacenoteMapID.IsNullOrWhiteSpace() && output != null)
                {                    
                    SBContextManager.Instance.context.StoreNewActivity(output.UniqueId);
                }
                // reset the input               
                activityId = "";
                input = null;
                callback(null, output, null);
            }
            else
            {
                print($"FinalizeSubmission Error: {responseResult}");

                var errorInfo = ErrorInfo.Create(responseResult);
                if (errorInfo == null)
                {
                    // Create a generic error.
                    print($"FinalizeSubmission Error: Creating a generic error.");
                    errorInfo = new ErrorInfo { ErrorCode = 100, Title = "The activity cannot be created at this time. Please try again later." };
                }
                callback(input, null, errorInfo);
            }
        }
         
        /// <summary>
        /// The callback method after the submission of a consumed activity.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="response"></param>
        /// <param name="callback"></param>
        void FinalizeActivityCompletion<T>(IConsumeActivityInput input, HttpResponseMessage response, OnAfterConsumeSubmission callback)
            where T : ConsumedActivityOutput<T>
        {
            var responseResult = response.Content.ReadAsStringAsync().Result;
            print($"FinalizeActivityCompletion StatusCode: {response.StatusCode}");
            if (response.IsSuccessStatusCode)
            {                
                print($"FinalizeActivityCompletion response: {responseResult}");
                //callback(consumedActivity, ConsumedActivityOutput<T>.Create<T>(responseResult), null);
                callback(consumedActivity, ConsumedActivityOutput<T>.Create(responseResult), null);
                consumedActivity = null;
            }
            else
            {
                print($"FinalizeActivityCompletion Error: {responseResult}");

                var errorInfo = ErrorInfo.Create(responseResult);
                if (errorInfo == null)
                {
                    // Create a generic error.
                    print("create a generic error");
                    errorInfo = new ErrorInfo { ErrorCode = 100, Title = "The activity cannot be completed at this time. Please try again later." };
                }
                else if (errorInfo.ErrorCode == ErrorCodes.ActivityAlreadyCompleted)
                {
                    callback(input, ConsumedActivityOutput<T>.CreateFromData(responseResult), null);
                }
                else
                {
                    print("known error");
                    callback(input, null, errorInfo);
                }                
            }
        }

        #region Delete Activities

        /// <summary>
        /// Deletes an activity in the database. If the activity is a post, all attached activities will also be deleted.
        /// </summary>
        /// <param name="experienceId"></param>
        /// <param name="id"></param>
        /// <param name="afterDelete"></param>
        public void DeleteActivity(string experienceId, string id, Action<ErrorInfo> afterDelete = null)
        {
            print($"DeleteActivity: experienceId={experienceId} | activityId={id}");
            var ownContent = SBContextManager.Instance.context.IsCreatingInConsume();
            this.StartThrowingCoroutine(DeleteActivityEnumerator(ownContent, experienceId, id, (response) =>
            {
                 
                ErrorInfo error = null;
                var responseResult = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    // reset the selected Id
                    activityId = "";
                    afterDelete?.Invoke(null);
                }
                else
                {
                    var errorInfo = ErrorInfo.Create(responseResult);
                    if (errorInfo == null)
                    {
                        // Create a generic error.
                        errorInfo = new ErrorInfo { ErrorCode = 100, Title = "The activity cannot be deleted at this time. Please try again later." };
                    }
                    afterDelete?.Invoke(errorInfo);
                }
                if (afterDelete==null)
                    OnActivityHasBeenDeleted(id, error);            
            }), e =>
            {
                print("ErrorHandler > callback - DeleteActivity");
                var errorInfo = new ErrorInfo
                {
                    ErrorCode = ErrorCodes.CannotBeDeleted,
                    Title = "The activity cannot be deleted at this time. Please try again later."
                };                
                afterDelete?.Invoke(errorInfo);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="experienceId"></param>
        /// <param name="id">The Id of the activity to be deleted.</param>
        /// <param name="onAfterSubmit"></param>
        /// <returns></returns>
        IEnumerator DeleteActivityEnumerator(bool ownContent, string experienceId, string id, Action<HttpResponseMessage> onAfterSubmit)
        {
            Func<bool, string, string, HttpResponseMessage> api = DoDeleteActivity;
            var apiResult = api.BeginInvoke(ownContent, experienceId, id, null, null);
            while (!apiResult.IsCompleted)
            {
                //print($"Waiting for SBRestClient.DeleteActivityEnumerator to finish...");
                yield return new WaitForEndOfFrame();
            }
            onAfterSubmit(api.EndInvoke(apiResult));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="experienceId">The Id of the experience where the activity belongs to.</param>
        /// <param name="id">The Id of the activity.</param>
        /// <returns></returns>
        HttpResponseMessage DoDeleteActivity(bool ownContent, string experienceId, string id)
        {            
            print("---------------------- DeleteActivity -----------------------");            
            activityId = id;

            #region Validation             
             
            if (!ValidateTokenFor("OnWillResumeActivityDelete"))
                return null;

            #endregion

            var url = ownContent
                ? string.Format(Endpoints.DeleteOwn, id, experienceId)
                : $"{Endpoints.Experiences}/{experienceId}/{Endpoints.Activities}/{id}";

            print($"URL: {url}");
            var response = httpClient.DeleteAsync(url).Result;
            
            return response;
        }

        /// <summary>
        /// This is a callback method that retries the deletion of an activity with an updated auth token.
        /// </summary>
        /// <param name="token"></param>
        public void ContinueDeleteActivity(AuthorizationToken token)
        {
            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                return;
            }
             
            // Update the token in this instance.
            UpdateAuthToken(token);

            print("Will try and submit the post again...");

            DeleteActivity(SBContextManager.Instance.context.experienceId, activityId);
        }

        #endregion 

        #region Map to Activities Linking

        public void DoLinkMapToActivities(MapActivitiesInput input)
        {
            if (input == null)
                print($"input is null");
            if (input.Activities == null)
                print($"input.Activities is null");
            if (input.MapId.IsNullOrWhiteSpace())
                print($"input.MapId is null or empty");

            this.StartThrowingCoroutine(LinkMapToActivitiesEnumerator(input, (response) =>
            {
                print($"LinkMapToActivitiesEnumerator response: {response}");
                 
                if (OnMapActivitySubmission == null)
                    print($"LinkMapToActivities OnMapActivitySubmission is null");
                else
                    OnMapActivitySubmission(input.MapId, input.Activities, null);

                // reset the input               
                activityId = "";
                mapActivitiesInput = null;
            }), ex =>
            {
                print("ErrorHandler > callback - DoLinkMapToActivities");
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue linking the map to the activities.", true, false, () =>
                {                                        
                    OnMapActivitySubmission("", null, ErrorInfo.CreateNetworkError());
                });
            });
        }

        IEnumerator LinkMapToActivitiesEnumerator(MapActivitiesInput input, Action<HttpResponseMessage> onAfterLinking)
        {             
            Func<MapActivitiesInput, HttpResponseMessage> api = LinkMapToActivities;
            print($"Waiting for SBRestClient.LinkMapToActivitiesEnumerator to finish...");
            var apiResult = api.BeginInvoke(input, null, null);
            while (!apiResult.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }
            print($"Waiting for SBRestClient.LinkMapToActivitiesEnumerator DONE.");
            onAfterLinking(api.EndInvoke(apiResult));
        }

        HttpResponseMessage LinkMapToActivities(MapActivitiesInput input)
        {
            print("LinkMapToActivities started.");

            if (input == null)
            {
                // This is the responsibility of the devs.
                // We will trigger a run-time exception here.
                print("SBRestClient.LinkMapToActivities: input cannot be null.");
                throw new ArgumentNullException("input cannot be null.");
            }

            mapActivitiesInput = input;

            #region Validation             
            //
            // Let's throw an error from here to inform/remind devs of the required and valid params.

            input.Validate();

            if (!ValidateTokenFor("OnWillAttachMapToActivities"))
                return null;

            #endregion

            input.CreateInConsume = SBContextManager.Instance.context.IsCreatingInConsume();
            print($"Linking map to the activities @ {apiURL}{Endpoints.AttachCloudMap.Replace("{{id}}", input.ExperienceId)}");
            print($"input data: {input}");

            HttpMethod method = HttpMethod.Post;
            var uri = $"{Endpoints.AttachCloudMap.Replace("{{id}}", input.ExperienceId)}";
            var request = new HttpRequestMessage(method, uri);
            request.SetTimeout(TimeSpan.FromSeconds(60));
            request.Content = input.ToStringContent();

            var response = httpClient.SendAsync(request).Result;             
       
            return response;
        }

        /// <summary>
        /// This is a callback method that resumes the <see cref="LinkMapToActivities"/>.
        /// </summary>
        /// <param name="token"></param>
        public void ResumeLinkMapToActivities(AuthorizationToken token)
        {
            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                return;
            }

            if (mapActivitiesInput == null)
            {
                // ToDo: log this scenario, there should be a valid input object here.
                return;
            }

            // Update the token in this instance.
            UpdateAuthToken(token);

            print("Will try and link the map to the activities again...");

            //linkMapRetries = 0;
            LinkMapToActivities(mapActivitiesInput);
        }

        #endregion

        #region Publishing Own Activities

        public IEnumerator PublishAddedActivitiesInConsumeEnumerator(string id, Action<HttpResponseMessage> onAfterPublishing)
        {
            Func<string, HttpResponseMessage> api = PublishAddedActivitiesInConsume;
            print($"Waiting for SBRestClient.PublishAddedActivitiesInConsume to finish...");
            var apiResult = api.BeginInvoke(id, null, null);
            while (!apiResult.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }
            print($"Waiting for SBRestClient.PublishAddedActivitiesInConsume DONE.");
            onAfterPublishing(api.EndInvoke(apiResult));
        }

        /// <summary>
        /// Publish the added own activities in Consume.
        /// </summary>
        /// <param name="id">Thd Id of the experience.</param>
        /// <returns></returns>
        HttpResponseMessage PublishAddedActivitiesInConsume(string id)
        {
            print($"PublishAddedActivitiesInConsume started: id={id}");
 
            if (!ValidateTokenFor("OnWillPublishAddedActivitiesInConsume"))
                return null;

            
 
            HttpMethod method = HttpMethod.Post;
            var uri = string.Format(Endpoints.PublishOwn, id);
            var request = new HttpRequestMessage(method, uri);
            request.SetTimeout(TimeSpan.FromSeconds(60));            

            var response = httpClient.SendAsync(request).Result;

            return response;
        }

        #endregion

        #region Trivia challenge

        /// <summary>
        /// Creates a new trivia challenge.
        /// </summary>
        /// <param name="id">The temporary ID assigned to the activity that was already added to "activityObjList".</param>
        /// <param name="input">The information about the trivia.</param>
        public void CreateTrivia(string id, TriviaChallengeInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;

            print("CreateTrivia");
            SubmitTriviaHelper("", input);
        }

        /// <summary>
        /// This is a callback method that re-submits a previously submitted trivia challenge with an a auth token.
        /// </summary>
        /// <param name="token"></param>
        public void SubmitTrivia(AuthorizationToken token)
        {
            //print($"SubmitTrivia(identifier) started. identifier={token.Identifier}");

            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                //print($"SubmitTrivia(identifier): received identifier is invalid.");
                return;
            }

            if (triviaInput == null)
            {
                //print($"SubmitTrivia(identifier): triviaInput is null.");
                // ToDo: log this scenario, there should be a valid input object here.
                return;
            }

            // Update the token in this instance.
            UpdateAuthToken(token);

            print("SubmitTrivia");
            //print("Will try and submit the trivia again...");            
            // The identifier is valid so let's try and submit the input again.
            // The expectation here is that no method is able to call any method in this class.
            // So the activityId never changed. It's either empty, which means we are adding a new activity,
            // or it has a value, which means we are updating an existing activity.
            // Remember that we are running this class as a singleton
            // and each process is expected to be completely finished before proceeding to another.
            // That is, for example, a submit trivia is completely done (either it failed or it succeeded)
            // before another activity is submitted.
            SubmitTriviaHelper(activityId, triviaInput);
        }

        /// <summary>
        /// Submits the trivia again with updated information.
        /// </summary>
        /// <param name="id">The Id of the trivia challenge.</param>
        /// <param name="input">The information about the trivia.</param>
        /// <remarks>
        /// We created this method with a separate member for the "id" instead of putting it in the input model.
        /// That is because we want to be explicit in what we want the RestClient to do.
        /// We do not want to put the logic of determining what to do based on the presence or absence of the id.
        /// </remarks>
        public void UpdateTrivia(string id, TriviaChallengeInput input)
        {
            if (id.IsNullOrWhiteSpace())
                throw new ArgumentNullException("id cannot be null or empty.");

            print("UpdateTrivia");
            // Flag "id" as the current activity being processed.
            referenceId = id;
            SubmitTriviaHelper(id, input);
        }

        /// <summary>
        /// The helper method that takes care of the logic if the input will be created or updated.
        /// </summary>
        /// <param name="id">The Id of the trivia challenge.</param>
        /// <param name="input">The information about the trivia.</param>
        void SubmitTriviaHelper(string id, TriviaChallengeInput input)
        {
            var ownContent = SBContextManager.Instance.context.IsCreatingInConsume();
            triviaInput = input;

            print($"SBRestClient.SubmitTriviaHelper: {input.ToString()}");
            
            if (SBContextManager.Instance.context.isOffline)
            {                
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                TriviaOutput output = TriviaOutput.CreateFrom(id, input);
                if(SBContextManager.Instance.context.mapId != null) {
                    output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                }
                output.ARInfo = input.ARInfo;
                output.ReferenceId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                output.IsOwnContent = ownContent;
                output.Location = input.Location;
                output.SortOrder = input.SortOrder;
                output.PointsCreation = 10;
                output.BucketId = input.BucketId;
                output.ParentId = input.ParentId;
                output.Text = input.Text;
                print($"calling OnActivityHasBeenSubmitted 1 > SBRestClient SubmitTriviaHelper isOffline = true, output = {output.ToJson()}");
                // reset the referenceId                
                referenceId = "";                
                OnActivityHasBeenSubmitted(output, output.ReferenceId, null);
                return;
            }

            this.StartThrowingCoroutine(SubmitToApiEnumerator(id, input, "OnWillSubmitTrivia", Endpoints.Trivias, (response) =>
            {
                FinalizeSubmission<TriviaOutput>(!id.IsNullOrWhiteSpace(), triviaInput, response,
                (i, o, e) => {
                    print($"1T SBRestClient.SubmitTriviaHelper: referenceId={referenceId}, id={id}");
                    var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                    triviaInput = (TriviaChallengeInput)i;
                    TriviaOutput output = null;
                    if (o != null)
                    {
                        output = (TriviaOutput)o;
                        output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                        output.ReferenceId = refId;
                        // reset the referenceId
                        referenceId = "";
                    }

                    print($"calling OnActivityHasBeenSubmitted 2 > 2T SBRestClient.SubmitTriviaHelper > OnActivityHasBeenSubmitted");
                    OnActivityHasBeenSubmitted(output, refId, e);
                });
            }), ex =>
            {                
                print($"ErrorHandler > callback - SubmitTriviaHelper: error={ex}");
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue saving your trivia. You may resubmit it again.", true, false, () =>
                {
                    var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                    print($"calling OnActivityHasBeenSubmitted 3");
                    OnActivityHasBeenSubmitted(null, refId, ErrorInfo.CreateNetworkError());
                });
            });
        }

        #endregion

        #region Post activity

        /// <summary>
        /// Creates a new post activity.
        /// </summary>
        /// <param name="id">The temporary ID assigned to the activity that was already added to "activityObjList".</param>
        /// <param name="input">The information about the post activity.</param>
        public void CreatePost(string id, PostActivityInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;            

            print($"SBRestClient.CreatePost: input = {input}");
            //StartCoroutine(SubmitPostHelperIEnumerator("", input));
            SubmitPostHelper("", input);
        }

        /// <summary>
        /// This is a callback method that re-submits a previously submitted post activity with an updated auth token.
        /// </summary>
        /// <param name="token"></param>
        public void ContinuePostSubmission(AuthorizationToken token)
        {
            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                return;
            }

            if (postInput == null)
            {
                // ToDo: log this scenario, there should be a valid input object here.
                return;
            }

            // Update the token in this instance.
            UpdateAuthToken(token);

            print("Will try and submit the post again...");
            // The identifier is valid so let's try and submit the input again.
            // The expectation here is that no method is able to call any method in this class.
            // So the activityId never changes. It's either empty, which means we are adding a new activity,
            // or it has a value, which means we are updating an existing activity.
            // Remember that we are running this class as a singleton
            // and each process is expected to be completely finished before proceeding to another.
            // That is, for example, a submit trivia is completely done (either it failed or it succeeded)
            // before another activity is submitted.
            SubmitPostHelper(activityId, postInput);
        }

        /// <summary>
        /// Edit the post activity with updated information.
        /// </summary>
        /// <param name="id">The Id of the post activity as assigned by the API.</param>
        /// <param name="input">The information about the post activity.</param>
        /// <remarks>
        /// We created this method with a separate member for the "id" instead of putting it in the input model.
        /// That is because we want to be explicit in what we want the RestClient to do.
        /// We do not want to put the logic of determining what to do based on the presence or absence of the id.
        /// Also, we are using the same input model, serialized, in submitting to the API.
        /// That means we do not want to create another object (i.e. a DTO) just so we can map the input values to the dto
        /// and use the dto in submitting to the API.
        /// 
        /// Lastly, the implementation in <see cref="CreatePost"/> is different.
        /// So we chose to have two methods instead of a single method
        /// having a logical "if" in determining what code to execute.
        /// </remarks>
        public void UpdatePost(string id, PostActivityInput input)
        {
            if (id.IsNullOrWhiteSpace())
                throw new ArgumentNullException("id cannot be null or empty.");

            // Flag "id" as the current activity being processed.
            referenceId = id;            
            print($"SBRestClient.UpdatePost: input = {input}");
            //StartCoroutine(SubmitPostHelperIEnumerator(id, input));
            SubmitPostHelper(id, input);
        }

        void SubmitPostHelper(string id, PostActivityInput input)
        {
            var ownContent = SBContextManager.Instance.context.IsCreatingInConsume();
            print($"SBRestClient.SubmitPostHelperIEnumerator: ownContent={ownContent} | {input.ToString()} | isOffline={SBContextManager.Instance.context.isOffline}");

            postInput = input;

            var endpoint = ownContent ? Endpoints.OwnPosts : Endpoints.Posts;
            
            if (SBContextManager.Instance.context.isOffline)
            {                
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                PostOutput output = PostOutput.CreateFrom(id, input);                             
                output.ARInfo = input.ARInfo;
                if (SBContextManager.Instance.context.mapId != null && output.ARInfo != null)
                {
                    output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                }                
                output.ReferenceId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;                
                output.IsOwnContent = ownContent;
                output.Location = input.Location;                
                output.SortOrder = input.SortOrder;
                output.PointsCreation = 10;
                output.BucketId = input.BucketId;
                output.ParentId = input.ParentId;
                output.Text = input.Text;
                output.MapLocation = input.MapLocation;
                output.AnchorPayload = input.AnchorPayload;
                
                print($"calling OnActivityHasBeenSubmitted 4 > SBRestClient SubmitPostHelper isOffline = true, output = {output.ToJson()} #payload");
                OnActivityHasBeenSubmitted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            this.StartThrowingCoroutine(SubmitToApiEnumerator(id, input, "OnWillResumeSubmitPost", endpoint, (response) =>
            {
                print($"SubmitPostHelper: Calling FinalizeSubmission...");
                FinalizeSubmission<PostOutput>(!id.IsNullOrWhiteSpace(), postInput, response,
                   (i, o, e) => {
                       print($"SBRestClient.SubmitPostHelperIEnumerator: referenceId={referenceId}, id={id}");
                       var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                       postInput = (PostActivityInput)i;
                       PostOutput output = null;
                       if (o != null)
                       {
                           o.IsOwnContent = ownContent;
                           output = (PostOutput)o;                           
                           output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                           output.ReferenceId = refId;
                           // reset the referenceId
                           referenceId = "";
                           // reset the flag                           
                           output.PhotoVideoId = input.PhotoVideoId;                           
                       }
                       print($"calling OnActivityHasBeenSubmitted 5 > SBRestClient SubmitPostHelper 2");
                       OnActivityHasBeenSubmitted(output, refId, e);
                   });
                print("Exiting SubmitPostHelperIEnumerator...");
            }), ex =>
            {                
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue saving your post. You may resubmit it again.", true, false, () =>
                {
                    var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                    print($"calling OnActivityHasBeenSubmitted 6");
                    OnActivityHasBeenSubmitted(null, refId, ErrorInfo.CreateNetworkError());
                });
            });
        }

        /// <summary>
        /// Updates the post activity as having a check-in only, or not.
        /// </summary>
        /// <param name="id">The Id of the post activity.</param>
        /// <param name="isCheckIn">The flag.</param>
        public void MarkPostAsCheckIn(string id, bool isCheckIn)
        {            
            this.StartThrowingCoroutine(CheckInToggleEnumerator(id, isCheckIn, "OnWillResumeMarkPostCheckIn", (response) =>
            {
               
                ErrorInfo error = null;
                var responseResult = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {                                        
                    print($"PostCheckInHelper response: {responseResult}");
                    BottomPanelManager.Instance.ShowMessagePanel("This is now a point of interest.", autoClose: false);
                }
                else
                {
                    print($"PostCheckInHelper Error: {responseResult}");

                    var errorInfo = ErrorInfo.Create(responseResult);
                    if (errorInfo == null)
                    {
                        // Create a generic error.
                        errorInfo = new ErrorInfo { ErrorCode = 100, Title = "The post activity cannot be created at this time. Please try again later." };
                    }                    
                }
                
                OnPostHasBeenMarkedAsCheckIn(id, isCheckIn, error);

                print("Exiting PostCheckInHelper...");
            }), ex =>
            {
                print("ErrorHandler > callback - MarkPostAsCheckIn");
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue saving your check-in. You may resubmit it again.", true, false, () =>
                {
                    OnPostHasBeenMarkedAsCheckIn(id, isCheckIn, ErrorInfo.CreateNetworkError());
                });
            });
        }

        /// <summary>
        /// This method calls the API that marks the specified activity as consumed by the logged-in user.
        /// </summary>
        /// <param name="id">The Id of the post activity being consumed.</param>
        /// <param name="input"></param>
        public void ConsumePost(string id, IConsumeActivityInput input)
        {
            print($"SBRestClient.ConsumePost: {id}");
            MessageManager.Instance.DebugMessage($"SBRestClient.ConsumePost: {id}");
            isBusy = true;
            var endpoint = string.Format(Endpoints.ConsumePosts, id);
            
            if (SBContextManager.Instance.context.isOffline) {
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                ConsumedPostOutput output = new ConsumedPostOutput {
                    UniqueId = id,
                    ExperienceId = input.ExperienceId,
                    ActivityId = input.ActivityId,
                    DateCompleted = DateTime.Now,
                    Status = ActivityStatus.Verified
                };
                
                print($"SBRestClient ConsumePost isOffline = true, output = {output.ToJson()}");
                isBusy = false;
                OnActivityHasBeenCompleted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            print($"Data to be saved: {input.ToString()}");
            this.StartThrowingCoroutine(CompleteActivityApiEnumerator(id, input, "OnWillResumeConsumePost", endpoint, (response) =>
            {                
                print($"Calling FinalizeActivityCompletion...");
                FinalizeActivityCompletion<ConsumedPostOutput>(input, response,
                   (i, o, e) =>
                   {
                       isBusy = false;
                       ConsumedPostOutput output = null;
                       if (o != null)
                       {
                           output = new ConsumedPostOutput
                           {
                               ActivityId = input.ActivityId,
                               ExperienceId = input.ExperienceId,
                               UniqueId = o.completedActivityId,
                               Points = o.points,
                               DateCompleted = o.dateCompleted,
                               Status = (ActivityStatus)o.status,
                           };
                       }
                       
                       OnActivityHasBeenCompleted(output, id, e);
                   });                 
            }), ex =>
            {
                print($"ErrorHandler > callback - ConsumePost, \nmessage:{ex.Message}, \nstacktrace: {ex.StackTrace}");
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue completing your activity. You may resubmit it again.", true, false, () =>
                {                    
                    OnActivityHasBeenCompleted(null, id, ErrorInfo.CreateNetworkError());
                });
            });
        }

        #endregion        

        #region CheckIn activity

        /// <summary>
        /// Creates a new check-in activity.
        /// </summary>
        /// <param name="id">The temporary ID assigned to the activity that was already added to "activityObjList".</param>
        /// <param name="input">The information about the check-in activity.</param>
        public void CreateCheckIn(string id, CheckInInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;

            SubmitCheckInHelper("", input);
        }

        /// <summary>
        /// This is a callback method that re-submits a previously submitted check-in activity with an updated auth token.
        /// </summary>
        /// <param name="token"></param>
        public void SubmitCheckIn(AuthorizationToken token)
        {
            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                return;
            }

            if (checkinInput == null)
            {
                // ToDo: log this scenario, there should be a valid input object here.
                return;
            }

            // Update the token in this instance.
            UpdateAuthToken(token);

            print("Will try and submit the post again...");
            // The identifier is valid so let's try and submit the input again.
            // The expectation here is that no method is able to call any method in this class.
            // So the activityId never changed. It's either empty, which means we are adding a new activity,
            // or it has a value, which means we are updating an existing activity.
            // Remember that we are running this class as a singleton
            // and each process is expected to be completely finished before proceeding to another.
            // That is, for example, a submit trivia is completely done (either it failed or it succeeded)
            // before another activity is submitted.
            SubmitCheckInHelper(activityId, checkinInput);
        }

        /// <summary>
        /// Submits the check-in activity again with updated information.
        /// </summary>
        /// <param name="id">The Id of the check-in activity.</param>
        /// <param name="input">The information about the check-in activity.</param>
        /// <remarks>
        /// We created this method with a separate member for the "id" instead of putting it in the input model.
        /// That is because we want to be explicit in what we want the RestClient to do.
        /// We do not want to put the logic of determining what to do based on the presence or absence of the id.
        /// </remarks>
        public void UpdateCheckIn(string id, CheckInInput input)
        {
            if (id.IsNullOrWhiteSpace())
                throw new ArgumentNullException("id cannot be null or empty.");

            // Flag "id" as the current activity being processed.
            referenceId = id;
            SubmitCheckInHelper(id, input);
        }

        /// <summary>
        /// The helper method that takes care of the logic if the input will be created or updated.
        /// </summary>
        /// <param name="id">The Id of the check-in activity.</param>
        /// <param name="input">The information about the check-in activity.</param>
        void SubmitCheckInHelper(string id, CheckInInput input)
        {
            checkinInput = input;

            var response = SubmitToApi(id, input, "OnWillResumeSubmitCheckIn", Endpoints.CheckIns);
            var ownContent = SBContextManager.Instance.context.IsCreatingInConsume();
            
            if (SBContextManager.Instance.context.isOffline)
            {                
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                CheckInOutput output = CheckInOutput.CreateFrom(id, input);
                if(SBContextManager.Instance.context.mapId != null) {
                    output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                }
                output.ARInfo = input.ARInfo;
                output.ReferenceId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                output.IsOwnContent = ownContent;
                output.Location = input.Location;
                output.SortOrder = input.SortOrder;
                output.PointsCreation = 10;
                output.BucketId = input.BucketId;
                output.ParentId = input.ParentId;
                output.Text = input.Text;
                print($"calling OnActivityHasBeenSubmitted 8 > SBRestClient SubmitCheckInHelper isOffline = true, output = {output.ToJson()}");
                OnActivityHasBeenSubmitted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            FinalizeSubmission<CheckInOutput>(!id.IsNullOrWhiteSpace(), checkinInput, response,
               (i, o, e) => {
                   var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                   checkinInput = (CheckInInput)i;
                   CheckInOutput output = null;
                   if (o != null)
                   {
                       output = (CheckInOutput)o;
                       output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                       output.ReferenceId = refId;
                       // reset the referenceId
                       referenceId = "";
                   }
                   print($"calling OnActivityHasBeenSubmitted 7 > SBRestClient SubmitCheckInHelper: FinalizeSubmission");
                   OnActivityHasBeenSubmitted(output, refId, e);
               });
        }

        #endregion

        #region Photo activity

        /// <summary>
        /// Creates a new photo activity.
        /// </summary>
        /// <param name="id">The temporary ID assigned to the activity that was already added to "activityObjList".</param>
        /// <param name="input">The information about the photo activity.</param>
        public void CreatePhoto(string id, PhotoActivityInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;
            apiAttempts = 0;
            SubmitPhotoHelper("", input);
        }

        /// <summary>
        /// This is a callback method that re-submits a previously submitted photo activity with an updated auth token.
        /// </summary>
        /// <param name="token"></param>
        public void ResumeSubmitPhoto(AuthorizationToken token)
        {
            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                return;
            }

            if (photoInput == null)
            {
                // ToDo: log this scenario, there should be a valid input object here.
                return;
            }

            // Update the token in this instance.
            UpdateAuthToken(token);

            print("Will try and submit the activity again...");
            apiAttempts = 0;
            // The identifier is valid so let's try and submit the input again.
            // The expectation here is that no method is able to call any method in this class.
            // So the activityId never changed. It's either empty, which means we are adding a new activity,
            // or it has a value, which means we are updating an existing activity.
            // Remember that we are running this class as a singleton
            // and each process is expected to be completely finished before proceeding to another.
            // That is, for example, a submit trivia is completely done (either it failed or it succeeded)
            // before another activity is submitted.
            SubmitPhotoHelper(activityId, photoInput);
        }

        /// <summary>
        /// Submits the photo activity again with updated information.
        /// </summary>
        /// <param name="id">The Id of the photo activity.</param>
        /// <param name="input">The information about the photo activity.</param>
        /// <remarks>
        /// We created this method with a separate member for the "id" instead of putting it in the input model.
        /// That is because we want to be explicit in what we want the RestClient to do.
        /// We do not want to put the logic of determining what to do based on the presence or absence of the id.
        /// </remarks>
        public void UpdatePhoto(string id, PhotoActivityInput input)
        {
            if (id.IsNullOrWhiteSpace())
                throw new ArgumentNullException("id cannot be null or empty.");

            // Flag "id" as the current activity being processed.
            referenceId = id;
            apiAttempts = 0;
            SubmitPhotoHelper(id, input);
        }

        /// <summary>
        /// The helper method that takes care of the logic if the input will be created or updated.
        /// </summary>
        /// <param name="id">The Id of the photo activity.</param>
        /// <param name="input">The information about the photo activity.</param>
        void SubmitPhotoHelper(string id, PhotoActivityInput input)
        {            
            var ownContent = SBContextManager.Instance.context.IsCreatingInConsume();
            photoInput = input;            
            apiAttempts++;
            var endpoint = ownContent ? Endpoints.OwnPhotos : Endpoints.Photos;

            print($"SubmitPhotoHelper params: {input.ToString()}");

            if (SBContextManager.Instance.context.isOffline)
            {                
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                PhotoOutput output = PhotoOutput.CreateFrom(id, input);
                if(SBContextManager.Instance.context.mapId != null) {
                    output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                }
                output.ARInfo = input.ARInfo;
                output.ReferenceId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                output.IsOwnContent = ownContent;
                output.Location = input.Location;
                output.SortOrder = input.SortOrder;
                output.PointsCreation = 10;
                output.BucketId = input.BucketId;
                output.ParentId = input.ParentId;
                output.Text = input.Text;
                print($"calling OnActivityHasBeenSubmitted 9 > SBRestClient SubmitPhotoHelper isOffline = true, output = {output.ToJson()}");
                OnActivityHasBeenSubmitted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            this.StartThrowingCoroutine(SubmitToApiEnumerator(id, input, "OnWillResumeSubmitPhoto", endpoint, (response) =>
            {
                FinalizeSubmission<PhotoOutput>(!id.IsNullOrWhiteSpace(), photoInput, response,
                (i, o, e) =>
                   {
                       var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                       photoInput = (PhotoActivityInput)i;
                       PhotoOutput output = null;
                       if (o != null)
                       {
                           o.IsOwnContent = ownContent;
                           output = (PhotoOutput)o;
                           output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                           output.ReferenceId = refId;
                           if (!output.SamplePhotos.Any())
                           {
                               output.SamplePhotos = input.SamplePhotos;
                           }
                           // reset the referenceId
                           referenceId = "";                           
                           print($"SBRestClient.SubmitPhotoHelper: {o.ToJson()}");
                       }

                       if (e != null)
                       {
                           if (apiAttempts >= 3)
                           {
                               print($"calling OnActivityHasBeenSubmitted 10 > SBRestClient.SubmitPhotoHelper: apiAttempts>3");
                               BottomPanelManager.Instance.ShowMessagePanel($"There's an issue uploading your photo. You may resubmit it again.", true);
                               OnActivityHasBeenSubmitted(output, refId, e);
                           }
                           else
                           {
                               print($"There's an issue uploading your photo. Retrying [{apiAttempts}]...");
                               BottomPanelManager.Instance.ShowMessagePanel($"There's an issue uploading your photo. Retrying [{apiAttempts}]...", false);
                               SubmitPhotoHelper(id, input);
                           }
                       }
                       else
                       {
                           print($"calling OnActivityHasBeenSubmitted 11 > SBRestClient.SubmitPhotoHelper: no error");
                           BottomPanelManager.Instance.ShowMessagePanel($"Your content has been successfully submitted.", true);
                           OnActivityHasBeenSubmitted(output, refId, e);
                       }
                   });
            }), ex =>
            {
                print("ErrorHandler > callback - SubmitPhotoHelper");
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue uploading your photo. You may resubmit it again.", true, false, () =>
                {
                    print($"calling OnActivityHasBeenSubmitted 12");
                    var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                    OnActivityHasBeenSubmitted(null, refId, ErrorInfo.CreateNetworkError());
                });                
            });
        }

        /// <summary>
        /// This method calls the API that marks the specified activity as consumed by the logged-in user.
        /// </summary>
        /// <param name="id">The Id of the photo activity being consumed.</param>
        /// <param name="input"></param>
        public void ConsumePhoto(string id, IConsumeActivityInput input)
        {
            print($"SBRestClient.ConsumePhoto: {id}");

            isBusy = true;
            var endpoint = string.Format(Endpoints.ConsumePhotos, id);

            if (SBContextManager.Instance.context.isOffline) {
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                ConsumedPhotoOutput output = new ConsumedPhotoOutput {
                    UniqueId = id,
                    ExperienceId = input.ExperienceId,
                    ActivityId = input.ActivityId,
                    DateCompleted = DateTime.Now,
                    Status = ActivityStatus.Verified
                };
                
                print($"SBRestClient ConsumePhoto isOffline = true, output = {output.ToJson()}");
                isBusy = false;
                OnActivityHasBeenCompleted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }

            StartCoroutine(CompleteActivityApiEnumerator(id, input, "OnWillResumeConsumePhoto", endpoint, (response) =>
            {
                print($"Calling FinalizeActivityCompletion...");
                FinalizeActivityCompletion<ConsumedPhotoOutput>(input, response,
                   (i, o, e) =>
                   {
                       isBusy = false;                       
                       ConsumedPhotoOutput output = null;
                       if (o != null)
                       {
                           output = new ConsumedPhotoOutput
                           {
                               ActivityId = input.ActivityId,
                               ExperienceId = input.ExperienceId,
                               UniqueId = o.completedActivityId,
                               Points = o.points,
                               DateCompleted = o.dateCompleted,
                               Status = (ActivityStatus)o.status
                           };                           
                       }

                       OnActivityHasBeenCompleted(output, id, e);
                   });
            }));
        }

        /// <summary>
        /// Completes the photo challenge.
        /// </summary>
        /// <param name="id">The Id of the photo activity being consumed.</param>
        /// <param name="input"></param>
        public void CompletePhotoChallenge(string id, IConsumeActivityInput input, ActivityStatus status)
        {
            print($"SBRestClient.CompletePhotoChallenge: {id} | {input.ToString()}");

            isBusy = true;
            var endpoint = string.Format(Endpoints.ConsumePhotoChallenges, id);
            
            if (SBContextManager.Instance.context.isOffline) {
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                ConsumedPhotoOutput output = new ConsumedPhotoOutput {
                    UniqueId = id,
                    ExperienceId = input.ExperienceId,
                    ActivityId = input.ActivityId,
                    DateCompleted = DateTime.Now,
                    Status = status
                };
                if (!String.IsNullOrWhiteSpace(RecordManager.Instance.filteredFilePath)) {
                    output.ResourceLocalIdentifiers = new List<string> { RecordManager.Instance.filteredFilePath };
                }
                
                print($"SBRestClient CompletePhotoChallenge isOffline = true, output = {output.ToJson()}");
                OnActivityHasBeenCompleted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            StartCoroutine(CompleteActivityApiEnumerator(id, input, "OnWillResumeCompletePhoto", endpoint, (response) =>
            {
                print($"CompletePhotoChallenge: Calling FinalizeSubmission...");                
                FinalizeActivityCompletion<ConsumedPhotoOutput>(input, response,
                   (i, o, e) =>
                   {
                       isBusy = false;
                       print($"SBRestClient.CompletePhotoChallenge: id={id}");
                       print($"SBRestClient.CompletePhotoChallenge: input is null? {input==null}, output is null? {o==null}");
                       print($"SBRestClient.CompletePhotoChallenge: ActivityId={input.ActivityId}");
                       print($"SBRestClient.CompletePhotoChallenge: ExperienceId={input.ExperienceId}");
                       
                       ConsumedPhotoOutput output = null;
                       if (o != null)
                       {
                           print($"SBRestClient.CompletePhotoChallenge: uniqueId={o.completedActivityId}, points={o.points}");
                           print($"SBRestClient.CompletePhotoChallenge: points={o.points}");
                           print($"SBRestClient.CompletePhotoChallenge: dateCompleted={o.dateCompleted}");
                           print($"SBRestClient.CompletePhotoChallenge: status={o.status}");
                           output = new ConsumedPhotoOutput
                           {
                               ActivityId = input.ActivityId,
                               ExperienceId = input.ExperienceId,
                               UniqueId = o.completedActivityId,
                               Points = o.points,
                               DateCompleted = o.dateCompleted,
                               Status = (ActivityStatus)o.status
                           };
                       }
                       if (e!=null)
                       {
                           print($"SBRestClient.CompletePhotoChallenge: error={e.Message}");
                       }

                       OnActivityHasBeenCompleted(output, id, e);
                   });
            }));
        }

        /// <summary>
        /// This method calls the API that marks the specified activity as consumed by the logged-in user.
        /// </summary>
        /// <param name="id">The Id of the video activity being consumed.</param>
        /// <param name="input"></param>
        public void ConsumeVideo(string id, IConsumeActivityInput input)
        {
            print($"SBRestClient.ConsumeVideo: {id}");
            isBusy = true;
            var endpoint = string.Format(Endpoints.ConsumeVideos, id);
            
            if (SBContextManager.Instance.context.isOffline) {
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                ConsumedVideoOutput output = new ConsumedVideoOutput {
                    UniqueId = id,
                    ExperienceId = input.ExperienceId,
                    ActivityId = input.ActivityId,
                    DateCompleted = DateTime.Now,
                    Status = ActivityStatus.Verified
                };
                
                print($"SBRestClient ConsumeVideo isOffline = true, output = {output.ToJson()}");
                isBusy = false;
                OnActivityHasBeenCompleted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            StartCoroutine(CompleteActivityApiEnumerator(id, input, "OnWillResumeConsumeVideo", endpoint, (response) =>
            {
                print($"Calling FinalizeActivityCompletion...");
                FinalizeActivityCompletion<ConsumedVideoOutput>(input, response,
                   (i, o, e) =>
                   {
                       isBusy = false;
                       print($"SBRestClient.ConsumeVideo: id={id}");
                       ConsumedVideoOutput output = null;
                       if (o != null)
                       {
                           output = new ConsumedVideoOutput
                           {
                               ActivityId = input.ActivityId,
                               ExperienceId = input.ExperienceId,
                               UniqueId = o.completedActivityId,
                               Points = o.points,
                               DateCompleted = o.dateCompleted,
                               Status = (ActivityStatus)o.status
                           };
                       }

                       OnActivityHasBeenCompleted(output, id, e);
                   });
            }));
        }

        /// <summary>
        /// This method calls the API that marks the specified activity as consumed by the logged-in user.
        /// </summary>
        /// <param name="id">The Id of the audio activity being consumed.</param>
        /// <param name="input"></param>
        public void ConsumeAudio(string id, IConsumeActivityInput input)
        {
            print($"SBRestClient.ConsumeAudio: {id}");
            isBusy = true;
            var endpoint = string.Format(Endpoints.ConsumeAudios, id);
            
            if (SBContextManager.Instance.context.isOffline) {
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                ConsumedAudioOutput output = new ConsumedAudioOutput {
                    UniqueId = id,
                    ExperienceId = input.ExperienceId,
                    ActivityId = input.ActivityId,
                    DateCompleted = DateTime.Now,
                    Status = ActivityStatus.Verified
                };
                
                print($"SBRestClient ConsumeAudio isOffline = true, output = {output.ToJson()}");
                isBusy = false;
                OnActivityHasBeenCompleted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            StartCoroutine(CompleteActivityApiEnumerator(id, input, "OnWillResumeConsumeAudio", endpoint, (response) =>
            {
                print($"Calling FinalizeActivityCompletion: input is nil? {input==null}");
                FinalizeActivityCompletion<ConsumedAudioOutput>(input, response,
                   (i, o, e) =>
                   {
                       isBusy = false;
                       print($"SBRestClient.ConsumeAudio: id={id}");
                       ConsumedAudioOutput output = null;
                       if (o != null)
                       {                           
                           output = new ConsumedAudioOutput
                           {
                               ActivityId = input.ActivityId,
                               ExperienceId = input.ExperienceId,
                               UniqueId = o.completedActivityId,
                               Points = o.points,
                               DateCompleted = o.dateCompleted,
                               Status = (ActivityStatus)o.status
                           };
                       }
                        
                       OnActivityHasBeenCompleted(output, id, e);
                   });
            }));
        }

        /// <summary>
        /// This method calls the API that marks the specified activity as consumed by the logged-in user.
        /// </summary>
        /// <param name="id">The Id of the trivia challenge being consumed.</param>
        /// <param name="input"></param>
        public void ConsumeTrivia(string id, IConsumeActivityInput input, ActivityStatus status)
        {
            print($"SBRestClient.ConsumeTrivia: {id}");
            isBusy = true;
            var endpoint = string.Format(Endpoints.ConsumeTriviaChallenges, id);
            
            if (SBContextManager.Instance.context.isOffline) {
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                ConsumedTriviaOutput output = new ConsumedTriviaOutput {
                    UniqueId = id,
                    ExperienceId = input.ExperienceId,
                    ActivityId = input.ActivityId,
                    DateCompleted = DateTime.Now,
                    Status = status
                };
                
                print($"SBRestClient ConsumeTrivia isOffline = true, output = {output.ToJson()}");
                isBusy = false;
                OnActivityHasBeenCompleted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            StartCoroutine(CompleteActivityApiEnumerator(id, input, "OnWillResumeConsumeTrivia", endpoint, (response) =>
            {
                print($"Calling FinalizeActivityCompletion...");
                FinalizeActivityCompletion<ConsumedTriviaOutput>(input, response,
                   (i, o, e) =>
                   {
                       isBusy = false;
                       print($"SBRestClient.ConsumedTrivia: {o}");
                       ConsumedTriviaOutput output = null;
                       if (o != null)
                       {
                           output = new ConsumedTriviaOutput
                           {
                               ActivityId = input.ActivityId,
                               ExperienceId = input.ExperienceId,
                               UniqueId = o.completedActivityId,
                               Points = o.points,
                               DateCompleted = o.dateCompleted,
                               Status = (ActivityStatus)o.status,                               
                           };
                       }

                       OnActivityHasBeenCompleted(output, id, e);
                   });
            }));
        }

        #endregion

        #region Video Activity

        public void CreateVideo(string id, VideoActivityInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;

            SubmitVideoHelper("", input);
        }

        public void UpdateVideo(string id, VideoActivityInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;

            SubmitVideoHelper(id, input);
        }

        /// <summary>
        /// This is a callback method that re-submits a previously submitted video activity with an updated auth token.
        /// </summary>
        /// <param name="token"></param>
        public void SubmitVideo(AuthorizationToken token)
        {
            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                return;
            }

            if (videoInput == null)
            {
                // ToDo: log this scenario, there should be a valid input object here.
                return;
            }

            // Update the token in this instance.
            UpdateAuthToken(token);

            print("Will try and submit the activity again...");
            // The identifier is valid so let's try and submit the input again.
            // The expectation here is that no method is able to call any method in this class.
            // So the activityId never changed. It's either empty, which means we are adding a new activity,
            // or it has a value, which means we are updating an existing activity.
            // Remember that we are running this class as a singleton
            // and each process is expected to be completely finished before proceeding to another.
            // That is, for example, a submit trivia is completely done (either it failed or it succeeded)
            // before another activity is submitted.
            SubmitVideoHelper(activityId, videoInput);
        }

        /// <summary>
        /// The helper method that takes care of the logic if the input will be created or updated.
        /// </summary>
        /// <param name="id">The Id of the video activity.</param>
        /// <param name="input">The information about the video activity.</param>
        void SubmitVideoHelper(string id, VideoActivityInput input)
        {
            videoInput = input;
            var ownContent = SBContextManager.Instance.context.IsCreatingInConsume();
            var endpoint = ownContent ? Endpoints.OwnVideos : Endpoints.Videos;
            
            if (SBContextManager.Instance.context.isOffline)
            {                
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                VideoOutput output = VideoOutput.CreateFrom(id, input);
                if(SBContextManager.Instance.context.mapId != null) {
                    output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                }
                output.ARInfo = input.ARInfo;
                output.ReferenceId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                output.IsOwnContent = ownContent;
                output.Location = input.Location;
                output.SortOrder = input.SortOrder;
                output.PointsCreation = 10;
                output.BucketId = input.BucketId;
                output.ParentId = input.ParentId;
                output.Text = input.Text;
                print($"calling OnActivityHasBeenSubmitted 13 > SBRestClient SubmitVideoHelper isOffline = true, output = {output.ToJson()}");
                OnActivityHasBeenSubmitted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            this.StartThrowingCoroutine(SubmitToApiEnumerator(id, input, "OnWillResumeSubmitVideo", endpoint, (response) =>
            {                
                FinalizeSubmission<VideoOutput>(!id.IsNullOrWhiteSpace(), videoInput, response,
                   (i, o, e) => {                       
                       var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;                       
                       videoInput = (VideoActivityInput)i;                       
                       VideoOutput output = null;                       
                       if (o != null)
                       {                           
                           o.IsOwnContent = ownContent;                           
                           output = (VideoOutput)o;                           
                           if (output.ARInfo == null)
                           {                               
                               output.ARInfo = input.ARInfo;
                           }                           
                           output.ARInfo.MapId = SBContextManager.Instance.context.mapId;                           
                           output.ReferenceId = refId;                           
                           // reset the referenceId
                           referenceId = "";
                       }
                       
                       OnActivityHasBeenSubmitted(output, refId, e);
                   });
            }), ex =>
            {
                print("ErrorHandler > callback - SubmitVideoHelper");
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue saving your video. You may resubmit it again.", true, false, () =>
                {
                    var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                    OnActivityHasBeenSubmitted(null, refId, ErrorInfo.CreateNetworkError());                    
                });
            });
        }

        #endregion

        #region Audio Activity

        public void CreateAudio(string id, AudioActivityInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;

            SubmitAudioHelper("", input);
        }

        public void UpdateAudio(string id, AudioActivityInput input)
        {
            // Flag "id" as the current activity being processed.
            referenceId = id;

            SubmitAudioHelper(id, input);
        }

        /// <summary>
        /// This is a callback method that re-submits a previously submitted audio activity with an updated auth token.
        /// </summary>
        /// <param name="token"></param>
        public void ResumeSubmitAudio(AuthorizationToken token)
        {
            // Validate that the source has a valid identifier.
            if (token.Identifier != this.identifier)
            {
                return;
            }

            if (audioInput == null)
            {
                // ToDo: log this scenario, there should be a valid input object here.
                return;
            }

            // Update the token in this instance.
            UpdateAuthToken(token);

            print("Will try and submit the activity again...");
            // The identifier is valid so let's try and submit the input again.
            // The expectation here is that no method is able to call any method in this class.
            // So the activityId never changed. It's either empty, which means we are adding a new activity,
            // or it has a value, which means we are updating an existing activity.
            // Remember that we are running this class as a singleton
            // and each process is expected to be completely finished before proceeding to another.
            // That is, for example, a submit trivia is completely done (either it failed or it succeeded)
            // before another activity is submitted.
            SubmitAudioHelper(activityId, audioInput);
        }

        /// <summary>
        /// The helper method that takes care of the logic if the input will be created or updated.
        /// </summary>
        /// <param name="id">The Id of the audio activity.</param>
        /// <param name="input">The information about the audio activity.</param>
        void SubmitAudioHelper(string id, AudioActivityInput input)
        {
            audioInput = input;
            var ownContent = SBContextManager.Instance.context.IsCreatingInConsume();
            var endpoint = ownContent ? Endpoints.OwnAudios : Endpoints.Audios;
            
            if (SBContextManager.Instance.context.isOffline)
            {                
                id = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id;
                AudioOutput output = AudioOutput.CreateFrom(id, input);
                if(SBContextManager.Instance.context.mapId != null) {
                    output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                }
                output.ARInfo = input.ARInfo;
                output.ReferenceId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                output.IsOwnContent = ownContent;
                output.Location = input.Location;
                output.SortOrder = input.SortOrder;
                output.PointsCreation = 10;
                output.BucketId = input.BucketId;
                output.ParentId = input.ParentId;
                output.Text = input.Text;
                print($"SBRestClient SubmitAudioHelper isOffline = true, output = {output.ToJson()}");
                OnActivityHasBeenSubmitted(output, referenceId.IsNullOrWhiteSpace() ? id : referenceId, null);
                return;
            }
            
            this.StartThrowingCoroutine(SubmitToApiEnumerator(id, input, "OnWillResumeSubmitAudio", endpoint, (response) =>
            {
                FinalizeSubmission<AudioOutput>(!id.IsNullOrWhiteSpace(), audioInput, response,
                   (i, o, e) => {
                       var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                       audioInput = (AudioActivityInput)i;
                       AudioOutput output = null;
                       if (o != null)
                       {
                           o.IsOwnContent = ownContent;                           
                           output = (AudioOutput)o;
                           if (output.ARInfo == null)
                           {
                               output.ARInfo = input.ARInfo;
                           }
                           output.ARInfo.MapId = SBContextManager.Instance.context.mapId;
                           output.ReferenceId = refId;
                           // reset the referenceId
                           referenceId = "";
                       }

                       OnActivityHasBeenSubmitted(output, refId, e);
                   });
            }), ex =>
            {
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue saving your audio. You may resubmit it again.", true, false, () =>
                {
                    var refId = referenceId.IsNullOrWhiteSpace() ? id : referenceId;
                    OnActivityHasBeenSubmitted(null, refId, ErrorInfo.CreateNetworkError());
                });
            });
        }

        #endregion

        #region Google Cloud Vision API Integration

        public void GetImageKeywords(string imagePath)
        {
            this.StartThrowingCoroutine(RequestImageAnnotationsEnumerator(imagePath, (response) =>
            {
                var responseResult = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    print($"Response Success: {responseResult}");
                    var model = VisionApiResponse.Create(responseResult);
                    if (model == null)
                    {
                        print($"Cannot deserialize json to create an instance of VisionApiResponse.");
                        OnKeywordsReceived(null, new ErrorInfo
                        {
                            ErrorCode = -1,
                            Title = "Invalid data",
                            Message = "Cannot deserialize json to create an instance of VisionApiResponse."
                        });
                    }
                    else
                    {
                        OnKeywordsReceived(PhotoChallengeKeywords.CreateFrom(model), null);
                    }
                }
                else
                {
                    print($"Response Error: {responseResult}");
                    OnKeywordsReceived(null, new ErrorInfo
                    {
                        ErrorCode = -1,
                        Title = "Request Error",
                        Message = responseResult
                    });
                }
            }), ex =>
            {                
                OnKeywordsReceived(null, new ErrorInfo
                {
                    ErrorCode = ErrorCodes.NetworkError,
                    Title = "Request Error",
                    Message = ex.Message
                });
            });            
        }

        IEnumerator RequestImageAnnotationsEnumerator(string imagePath, Action<HttpResponseMessage> onKeywordsReceived)
        {
            Func<string, HttpResponseMessage> api = RequestImageAnnotations;
            print($"Waiting for SBRestClient.GetImageAnnotations to finish...");
            var apiResult = api.BeginInvoke(imagePath, null, null);
            while (!apiResult.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }
            print($"Waiting for SBRestClient.GetImageAnnotationsEnumerator DONE.");
            onKeywordsReceived(api.EndInvoke(apiResult));             
        }

        HttpResponseMessage RequestImageAnnotations(string imagePath)
        {
            print("GetImageAnnotations started.");

            var bytes = File.ReadAllBytes(imagePath);
            var content = Convert.ToBase64String(bytes);

            var request = new VisionApiRequest
            {
                Requests = new List<VisionApiRequest.AnnotateImageRequest>
                {
                    new VisionApiRequest.AnnotateImageRequest
                    {
                        Image = new VisionApiRequest.ImageInput
                        {
                            Content = content
                        },
                        Features = new List<VisionApiRequest.FeatureWeight>
                        {
                            new VisionApiRequest.FeatureWeight { Type = "LABEL_DETECTION", MaxResults = 10 },
                            new VisionApiRequest.FeatureWeight { Type = "LANDMARK_DETECTION", MaxResults = 10 },
                        }
                    }
                }
            };
              
            print($"request body >>> {request.ToStringContent()}");
            var requestUri = $"{Endpoints.CloudVisionAPIBaseURL}{Endpoints.AnnotateImageURL}?key={SBContextManager.Instance.context.GetVisionKey()}";
            print(requestUri);
           
            TryRemoveHeader("Authorization");
            TryAddHeader("X-Ios-Bundle-Identifier", SBContextManager.Instance.context.bundleIdentifier);
            var response = httpClient.PostAsync(requestUri, request.ToStringContent()).Result;
            RestoreAuthToken();
            TryRemoveHeader("X-Ios-Bundle-Identifier");                        

            return response;
        }

        #endregion

        public HttpResponseMessage SaveBreadcrumb(string mapId, IEnumerable<Vector3> breadcrumb)
        {
            print("SaveBreadcrumb started.");

            if (mapId.IsNullOrWhiteSpace())
            {
                // This is the responsibility of the devs.
                // We will trigger a run-time exception here.
                print("SBRestClient.SaveBreadcrumb: mapId cannot be null.");
                throw new ArgumentNullException("mapId cannot be null or empty.");
            }

            if (!breadcrumb.Any())
            {
                // This is the responsibility of the devs.
                // We will trigger a run-time exception here.
                print("SBRestClient.SaveBreadcrumb: breadcrumb cannot be null.");
                throw new ArgumentNullException("breadcrumb cannot be null or empty.");
            }
             
            #region Validation             
            //
            // Let's throw an error from here to inform/remind devs of the required and valid params.

            #endregion

            print($"Saving breadcrumbs with mapId = {mapId}");

            var experienceId = SBContextManager.Instance.context.experienceId;
            var crumbs = breadcrumb.Select(v => new ARDefinition.ARPosition
            {
                X = v.x,
                Y = v.y,
                Z = v.z
            });
            var httpContent = new StringContent(JsonConvert.SerializeObject(crumbs), Encoding.UTF8, "application/json");

            var response = httpClient.PostAsync($"{Endpoints.UpdateBreadcrumb.Replace("{{id}}", experienceId).Replace("{{mapId}}",mapId)}", httpContent).Result;
            var responseResult = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                print($"SBRestClient.SaveBreadcrumb response: {responseResult}");

                if (OnMapActivitySubmission == null)
                    print($"LinkMapToActivities OnMapActivitySubmission is null");
                else
                    OnBreadcrumbSubmitted(null);
            }
            else
            {
                print($"SBRestClient.SaveBreadcrumb Error: {responseResult}");

                var errorInfo = ErrorInfo.Create(responseResult);
                if (errorInfo == null)
                {
                    // Create a generic error.
                    errorInfo = new ErrorInfo { ErrorCode = 100, Title = "The breadcrumbs cannot be created. Please try again later." };
                }
                OnBreadcrumbSubmitted(errorInfo);
            }

            return response;
        }

        async Task<string> GetExperienceContainerUrlAsync(string id, bool refreshPolicy)
        {
            var url = refreshPolicy
                ? Endpoints.ExperienceContainerSASWithRefresh.Replace("{{id}}", id)
                : Endpoints.ExperienceContainerSAS.Replace("{{id}}", id);
            print($"GetExperienceContainerURL: id={id} | url={url}");
            var response = await httpClient.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                print($"GetExperienceContainerURL: result={result}");
                var sasurl = JsonConvert.DeserializeObject<SasURL>(result);

                return sasurl.sas;
            }
            catch
            {
                print($"GetExperienceContainerURL: error deserializing result.");
            }

            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">The Id of the experience.</param>
        /// <returns></returns>        
        string GetExperienceContainerURL(string id, bool refreshPolicy)
        {            
            var url = refreshPolicy
                ? Endpoints.ExperienceContainerSASWithRefresh.Replace("{{id}}", id)
                : Endpoints.ExperienceContainerSAS.Replace("{{id}}", id);
            print($"GetExperienceContainerURL: id={id} | url={httpClient.BaseAddress}{url}");            
            var response = httpClient.GetAsync($"{url}").Result;
            var result = response.Content.ReadAsStringAsync().Result;

            try
            {
                print($"GetExperienceContainerURL: result={result}");
                var sasurl = JsonConvert.DeserializeObject<SasURL>(result);

                return sasurl.sas;
            }
            catch
            {
                print($"GetExperienceContainerURL: error deserializing result.");
            }

            return string.Empty;
        }

        public IEnumerator GetExperienceContainerUrlIEnumerator(string id, bool refreshSASPolicy,
            Action<string> onUrlReceived, Action<ErrorInfo> onError)
        {             
            Func<string, bool, string> getSAS = GetExperienceContainerURL;
            var getSasResult = getSAS.BeginInvoke(id, refreshSASPolicy, null, null);
            print($"Waiting for GetExperienceContainerURL to finish...");
            while (!getSasResult.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }
            print($"GetExperienceContainerURL DONE.");
            string sasURL = getSAS.EndInvoke(getSasResult);
            //var sasURL = GetExperienceContainerURL(id);
            print($"sasURL = {sasURL}");

            if (sasURL.IsNullOrWhiteSpace())
            {
                BottomPanelManager.Instance.UpdateMessage($"Your content cannot be uploaded at this time. You may try and save your content again.");
                RestoreAuthToken();
                onError(new ErrorInfo { ErrorCode = ErrorCodes.GetSASFailed, Message = "Your content cannot be uploaded at this time. You may try and save your content again." });
                yield break;
            }

            onUrlReceived(sasURL);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="uploadURL"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        HttpResponseMessage UploadMedia(string assetPath, string uploadURL, string caption)
        {
            TryRemoveHeader("Authorization");
            using (var form = new MultipartFormDataContent())
            {
                using (var fs = File.OpenRead(assetPath))
                {
                    print($"Length={fs.Length}");
                    using (var sc = new StreamContent(fs))
                    {
                        print($"Adding content only: GetFileName={Path.GetFileName(assetPath)}");

                        TryAddHeader("x-ms-blob-type", "BlockBlob");
                        //print($"headers={string.Join(",", httpClient.DefaultRequestHeaders.Select(x => x.Key))}");
                        //print($"BaseAddress={httpClient.BaseAddress}");
                        //print($"uploadURL={uploadURL}");                        
                        var response = httpClient.PutAsync(uploadURL, sc).Result;
                        RestoreApiUrl();
                        TryRemoveHeader("x-ms-blob-type");
                        RestoreAuthToken();

                        return response;
                        //if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Created)
                        //{
                        //    print("SBRestClient.UploadBlobIEnumerator Upload complete!");
                        //    if (!SBContextManager.Instance.IsOnBoarding())
                        //        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.CancelStartScan);
                        //    return true;
                        //}
                        //else
                        //{
                        //    print($"SBRestClient.UploadBlobIEnumerator Upload error: {response.ReasonPhrase}");
                        //    if (!SBContextManager.Instance.IsOnBoarding())
                        //        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.CancelStartScan);
                        //    onError();
                        //}                                             
                    }
                }
            }             
        }

        /// <summary>
        /// An asynchronous method for uploading a photo or video to blob storage.
        /// </summary>
        /// <param name="id">The Id of the experience.</param>
        /// <param name="blobId">The Unique Id of the blob.</param>
        /// <param name="caption">The optional caption for the blob.</param>
        /// <param name="thumbnailPath">Flag used to determine if the blob is a photo or a video.</param>
        /// <param name="continueSubmitPhotoVideo">The callback after the blob has been uploaded.</param>
        /// <returns></returns>
        public IEnumerator UploadBlobIEnumerator(string sasURL, string blobId, string caption, AssetType type, 
            Action<string, string> continueSubmitPhotoVideo, Action<ErrorInfo> continueOnError, string thumbnailPath = "")
        {
            //BottomPanelManager.Instance.UpdateMessage($"Preparing to upload your content...");

            if (type == AssetType.Thumbnail && thumbnailPath.IsNullOrWhiteSpace())
                throw new ArgumentNullException("thumbnailPath cannot be null or empty.");

            uploadBlobTimeout = continueOnError;
            
            BottomPanelManager.Instance.UpdateMessage($"Uploading your content...");
            var parts = sasURL.Split('?');// sasResult.BlobURL.Split('?');
            var baseUrl = parts[0];
            var queryString = "";
            if (parts.Length > 1)
                queryString = parts[1];

            //var mimeType = "image/jpeg";
            var ext = ".jpeg"; // We are forcing the type of the image to jpg.
            byte[] bytes;

            string assetPath;
            if (type == AssetType.Video)
            {
                print($"videoPath = {RecordManager.Instance.filteredFilePath}");
                bytes = File.ReadAllBytes(RecordManager.Instance.filteredFilePath);
                assetPath = RecordManager.Instance.filteredFilePath;
                ext = ".mov"; // we are forcing the video type to .mov
                //mimeType = "video/x-msvideo";
            }
            else if (type == AssetType.Audio)
            {
                print($"audioPath = {RecordManager.Instance.audioPath}");
                bytes = File.ReadAllBytes(RecordManager.Instance.audioPath);
                assetPath = RecordManager.Instance.audioPath;
                ext = ".mp4"; // we are forcing the video type to .mov
                //mimeType = "video/mp4";
            }
            else if (type == AssetType.Photo)
            {
                print($"photoPath = {RecordManager.Instance.filteredFilePath}");
                bytes = File.ReadAllBytes(RecordManager.Instance.filteredFilePath);
                assetPath = RecordManager.Instance.filteredFilePath;
            }
            else // type is AssetType.Thumbnail
            {
                bytes = File.ReadAllBytes(thumbnailPath);
                assetPath = thumbnailPath;
            }

            print($"Resource size = {bytes.Length}");
             
            var uploadURL = $"{baseUrl}/{blobId}{ext}?{queryString}";
            print($"-=- uploadURL = {uploadURL}");

            // --- Showing progress bar
            //ActivityUIFacade.Instance.ResetMappingProgress();
            //ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.StartBlobUpload);
 
            webRequestExecutionTime = DateTime.Now;


            // ToDo: test http error handler
            //assetPath = assetPath + "x";

            print($"UploadBlobIEnumerator: upload init > assetPath={assetPath}.");
            //// Uploading the asset should not exceed a minute.
            //// Otherwise either the asset is too big, the server is responding too slow,
            //// or the client connection is poor.

            yield return new WaitForEndOfFrame();
             
            Func<string, string, string, HttpResponseMessage> uploadMedia = UploadMedia;
            var uploadMediaResult = uploadMedia.BeginInvoke(assetPath, uploadURL, caption, null, null);
            print($"Waiting for UploadMedia to finish...");
            while (!uploadMediaResult.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }
            print($"UploadMedia DONE.");
            HttpResponseMessage response = uploadMedia.EndInvoke(uploadMediaResult);
            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                print("SBRestClient.UploadBlobIEnumerator Upload complete!");
                if (!SBContextManager.Instance.IsOnBoarding())
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.CancelStartScan);
                continueSubmitPhotoVideo(caption, uploadURL);
            }
            else
            {
                print($"SBRestClient.UploadBlobIEnumerator Upload error: {response.ReasonPhrase}");
                if (!SBContextManager.Instance.IsOnBoarding())
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.CancelStartScan);
                continueOnError(new ErrorInfo { ErrorCode = ErrorCodes.UploadFailed, Message = response.ReasonPhrase });
            }

            print($"UploadBlobIEnumerator: request DONE 2.");
        }
         
        #region Callbacks

        /// <summary>
        /// Delegate used after the activity has been submitted to the API.
        /// </summary>        
        /// <param name="input">The information about the activity to be submitted.</param>
        /// <param name="output">The information about the created/updated activity from the server, which includes the input information.</param>
        /// <param name="error">The error from the server, if there's any.</param>        
        delegate void OnAfterSubmission(IActivityInput input, IActivityOutput output, ErrorInfo error);
        delegate void OnReceivedSasUrl(string blobURL, ErrorInfo error);
        /// <summary>
        /// Delegate used after the consumed activity has been submitted to the API.
        /// </summary>        
        /// <param name="input">The information about the activity to be submitted.</param>
        /// <param name="output">The information about the created/updated activity from the server, which includes the input information.</param>
        /// <param name="error">The error from the server, if there's any.</param>        
        //delegate void OnAfterConsumeSubmission(IConsumeActivityInput input, IConsumedActivityOutput output, ErrorInfo error);
        delegate void OnAfterConsumeSubmission(IConsumeActivityInput input, GenericConsumedOutput output, ErrorInfo error);

        public Action<string, bool, ErrorInfo> OnPostHasBeenMarkedAsCheckIn;
        public Action<string, ErrorInfo> OnActivityHasBeenDeleted;
        public Action<IActivityOutput, string, ErrorInfo> OnActivityHasBeenSubmitted;
        public Action<IConsumedActivityOutput, string, ErrorInfo> OnActivityHasBeenCompleted;
        public Action<string> OnWillUpdateAuthToken;
        public Action<string, IEnumerable<string>, ErrorInfo> OnMapActivitySubmission;
        public Action<ErrorInfo> OnBreadcrumbSubmitted;
        public Action<PhotoChallengeKeywords, ErrorInfo> OnKeywordsReceived;

        #endregion

        /// <summary>
        /// The flag that indicates if the API call is still running.
        /// </summary>
        bool isApiRunning = false;
        /// <summary>
        /// The number of attempts the API is called.
        /// </summary>
        int apiAttempts = 0;
        /// <summary>
        /// The member that controls
        /// 
        /// </summary>
        /// <remarks>
        /// We are also using this for testing. We will force a token refresh for the first API call.
        /// </remarks>
        int tokenRefreshCounter = 0;        
        /// <summary>
        /// The base URL of the API.
        /// </summary>
        string apiURL;
        /// <summary>
        /// The authorization token of the currently logged in user.
        /// </summary>
        AuthorizationToken token;
        /// <summary>
        /// This validates the call from the native code.
        /// If the identifier given back by the native code
        /// is not the same as the one generated here when the native is called
        /// then the callback method will fail.
        /// </summary>
        string identifier;
        /// <summary>
        /// The instance of the class that manages the communication with the native code.
        /// </summary>
        NativeCall nativeCall;
        ///// <summary>
        ///// Flag that indicates if the content is being created in Consume.
        ///// </summary>
        //bool isCreatingOwnContent = false;
        /// <summary>
        /// The information of the trivia challenge being submitted.
        /// </summary>
        TriviaChallengeInput triviaInput;
        /// <summary>
        /// The information of the post activity being submitted.
        /// </summary>
        PostActivityInput postInput;
        /// <summary>
        /// The information of the check-in activity being submitted.
        /// </summary>
        CheckInInput checkinInput;
        /// <summary>
        /// The information of the audio activity being submitted.
        /// </summary>
        AudioActivityInput audioInput;
        /// <summary>
        /// The information of the photo activity being submitted.
        /// </summary>
        PhotoActivityInput photoInput;
        /// <summary>
        /// The information of the video activity being submitted.
        /// </summary>
        VideoActivityInput videoInput;
        /// <summary>
        /// Contains a list of activities Ids in a single map.
        /// </summary>
        MapActivitiesInput mapActivitiesInput;
        /// <summary>
        /// This class will only handle a single API processing at any given time.
        /// So this id represents the current activity being processed.
        /// </summary>
        static string activityId;
        /// <summary>
        /// See the definition of <see cref="ActivityOutput{T}.ReferenceId"/>.
        /// </summary>
        static string referenceId;
        /// <summary>
        /// The activity being consumed.
        /// </summary>
        /// <remarks>
        /// There should only be one activity that is consumed at any give time.
        /// We do not support completing multiple activities at the same time. 
        /// </remarks>
        public IConsumeActivityInput consumedActivity;
        /// <summary>
        /// A singleton HttpClient.
        /// </summary>
        static HttpClient httpClient;
        /// <summary>
        /// Used only for uploading assets. Don't use for API calls.
        /// </summary>
        //static UnityWebRequest webRequest;
        /// <summary>
        /// The last time the webRequest was used.
        /// </summary>
        DateTime? webRequestExecutionTime;
        /// <summary>
        /// The flag that controls if the UploadBlobIEnumerator needs to cancel it's done operation
        /// after the manual time-out checking occured.
        /// </summary>
        bool willCancelUploadBlobIEnumerator;
        /// <summary>
        /// The reference to the "continueOnError" in UploadBlobIEnumerator.
        /// </summary>
        Action<ErrorInfo> uploadBlobTimeout;
        /// <summary>
        /// Flag used to check if the app is busy saving an activity. There can only be one activity that can be processed at any given time.
        /// </summary>
        public bool isBusy = false;

        /// <summary>
        /// A helper class that holds the endpoints.
        /// </summary>
        class Endpoints
        {
            /// <summary>
            /// The endpoint for creating or updating trivia challenges. 
            /// </summary>
            public const string Trivias = "activities/trivias";
            /// <summary>
            /// The endpoint for creating or updating post activities.
            /// </summary>
            public const string Posts = "activities/posts";
            /// <summary>
            /// The endpoint for marking a post activity as "with checkin only" or not.
            /// </summary>
            public const string PostCheckIn = "activities/posts/{0}/checkin";
            /// <summary>
            /// The endpoint for creating or updating audio activities.
            /// </summary>
            public const string Audios = "activities/audios";
            /// <summary>
            /// The endpoint for creating or updating photo activities.
            /// </summary>
            public const string Photos = "activities/photos";
            /// <summary>
            /// The endpoint for creating or updating video activities.
            /// </summary>
            public const string Videos = "activities/videos";
            /// <summary>
            /// The endpoint for creating or updating checkins (POIs).
            /// </summary>
            public const string CheckIns = "activities/checkins";
            /// <summary>
            /// The endpoint for creating or updating post activities while in Consume.
            /// </summary>
            public const string OwnPosts = "activities/myown/posts";
            /// <summary>
            /// The endpoint for creating or updating photo activities while in Consume.
            /// </summary>
            public const string OwnPhotos = "activities/myown/photos";
            /// <summary>
            /// The endpoint for creating or updating video activities while in Consume.
            /// </summary>
            public const string OwnVideos = "activities/myown/videos";
            /// <summary>
            /// The endpoint for creating or updating audio activities while in Consume.
            /// </summary>
            public const string OwnAudios = "activities/myown/audios";
            /// <summary>
            /// The endpoint for deleting own content.
            /// </summary>
            public const string DeleteOwn = "activities/myown/{0}/experiences/{1}";
            /// <summary>
            /// The endpoint for publishing the "added (own)" activities in Consume.
            /// </summary>
            public const string PublishOwn = "activities/myown/publish/experiences/{0}";
            /// <summary>
            /// The endpoint for deleting activities.
            /// </summary>
            public const string Activities = "activities";
            /// <summary>
            /// The endpoint for experiences.
            /// </summary>
            public const string Experiences = "experiences";

            #region Endpoints for consuming activities
            // <summary>
            /// The endpoint for completing a post POI activity.
            /// </summary>
            public const string ConsumePosts = "activities/{0}/posts";
            /// <summary>
            /// The endpoint for completing a photo POI activity.
            /// </summary>
            public const string ConsumePhotos = "activities/{0}/photos";   
            /// <summary>
            /// The endpoint for completing photo challenges.
            /// </summary>
            public const string ConsumePhotoChallenges = "activities/{0}/photos";
            /// <summary>
            /// The endpoint for completing a video POI activity.
            /// </summary>
            public const string ConsumeVideos = "activities/{0}/videos";
            /// <summary>
            /// The endpoint for completing an audio activity.
            /// </summary>
            public const string ConsumeAudios = "activities/{0}/audios";
            /// <summary>
            /// The endpoint for completing trivia challenges.
            /// </summary>
            public const string ConsumeTriviaChallenges = "activities/{0}/trivias";
            #endregion

            public const string AttachCloudMap = "experiences/{{id}}/cloudmaps";

            public const string UpdateBreadcrumb = "experiences/{{id}}/cloudmaps/{{mapId}}/breadcrumbs";

            public const string ExperienceContainerSAS = "experiences/{{id}}/containersas?key=A1A40F68A71B430CB96FA0A9C8474FD8";
            public const string ExperienceContainerSASWithRefresh = "experiences/{{id}}/containersas?forceRefresh=true&key=A1A40F68A71B430CB96FA0A9C8474FD8";

            #region Google Cloud Vision API
            public const string CloudVisionAPIBaseURL = "https://vision.googleapis.com/v1";
            public const string AnnotateImageURL = "/images:annotate";
            #endregion

            // Test / simulations
            public const string TestHttp500 = "admin/playground/error500delay3";
            public const string TestTimeout = "admin/playground/delay120";
        }
    }
}