//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.UI;

//namespace SocialBeeAR
//{

//    public class TriviaQuestion
//    {
//        public string question;
//        public List<string> optionList;
//        public int selectedIndex;
//        public string hints;
//    }


//    /// <summary>
//    /// Facade class for Trivia content for an anchor object.
//    /// (Facade class is for managing interaction for a set of UI components)
//    /// </summary>
//    public class TriviaContentFacade : ContentFacade
//    {
//        //board
//        [SerializeField] private GameObject triviaSetting;
//        [SerializeField] private GameObject triviaViewing;

//        //question details
//        [SerializeField] private Text questionText;
//        [SerializeField] private Text option1Text;
//        [SerializeField] private Text option2Text;
//        [SerializeField] private Text option3Text;
//        [SerializeField] private Text option4Text;
//        [SerializeField] private Text hintsText;

//        //for testing
//        [SerializeField] private Text test1ResultText;
//        [SerializeField] private Text test2ResultText;

//        // shorthand
//        SBRestClient api = SBRestClient.Instance;
//        Location currentLocation = null;
//        PhotoActivityInput photoInput = null;
//        /// <summary>
//        /// This validates the call from the native code.
//        /// If the identifier given back by the native code
//        /// is not the same as the one generated here when the native is called
//        /// then the callback method will fail.
//        /// </summary>
//        string identifier;

//        // For debugging only.
//        NativeCall nativeCall = null;

//        public void InitUIMode(UIMode uiMode)
//        {
//            //init basic components
//            base.Init(uiMode);

//            // For debugging only.
//            nativeCall = FindObjectOfType<NativeCall>();

//            // 
//            // NOTE: This should be present anywhere where we want to extract
//            //       the user's location information such as the city name.
//            //       It is best if we put this and the callback in SocialBeeARMain
//            //       and then pass the information to the appropriate facade object.
//            // Start getting location information.
//            //
//            LocationProxy.StartUpdatingLocation(name);

//            //init activity specific components
//            if (uiMode != UIMode.Consumer) //for creator
//            {
//                this.triviaSetting.SetActive(true);
//                this.triviaViewing.SetActive(false);
//            }
//            else //for consumer
//            {
//                this.triviaSetting.SetActive(false);
//                this.triviaViewing.SetActive(true);
//            }

//            editButton.SetActive(false); //for testing!

//            // Set up callback
//            SBRestClient.Instance.OnWillUpdateAuthToken += OnWillUpdateAuthToken;
//            //SBRestClient.Instance.OnTriviaSubmission += OnTriviaSubmission;

//            #region Should be moved to its proper class.
//            // ToDo: The codes in this region are just for testing
//            //      and should be moved in the class that needs to implement these codes.
//            //SBRestClient.Instance.OnPostSubmission += OnPostSubmission;
//            //SBRestClient.Instance.OnCheckInSubmission += OnCheckInSubmission;
//            //SBRestClient.Instance.OnPhotoSubmission += OnPhotoSubmission;
//            SBRestClient.Instance.OnMapActivitySubmission += OnMapActivitySubmission;
//            #endregion
//        }

//        ///// <summary>
//        ///// 
//        ///// </summary>
//        ///// <param name="info">Formatted location info (format: %f\t%f\t%f\t%f\t%@\t%@\t%@\t%@\t%@\t%@\t%@).</param>
//        ///// <remarks>
//        ///// DEV NOTES:
//        ///// We should put this in SocialBeeARMain or something similar
//        ///// where the location can be tracked throughout the entire AR session.
//        ///// The information gathered here should then be passed to the specific facade
//        ///// that will handle the gathering of information for an activity.
//        ///// The activity that will be submitted to the API.                
//        ///// </remarks>
//        //public void OnUpdateLocation(string info)
//        //{
//        //    var parts = info.Split('\t');
//        //    if (parts.Length != 11)
//        //    {
//        //        print($"OnUpdateLocation failed, expected parts=11 but got {parts.Length}.");
//        //        // We are expecting exactly 11 parts from the address information.
//        //        // Even if a part is missing, it should be an empty string in the "info".
//        //        // ToDo: log this error on the server and notify ourselves.
//        //        return;
//        //    }
//        //    var lat = double.Parse(parts[0]);
//        //    var lon = double.Parse(parts[1]);
//        //    var speed = double.Parse(parts[2]);
//        //    var accuracy = double.Parse(parts[3]);
//        //    var name = parts[4];
//        //    var street = parts[5];
//        //    var neighborhood = parts[6];
//        //    var city = parts[7];
//        //    var state = parts[8];
//        //    var postalCode = parts[9];
//        //    var country = parts[10];

//        //    //print($"lat={lat} | lon={lon} | speed={speed} | accuracy={accuracy} | name={name} | street={street} | neighborhood={neighborhood} | city={city} | state={state} | postal={postalCode} | country={country}");

//        //    currentLocation = new Location
//        //    {
//        //        Latitude = lat,
//        //        Longitude = lon,
//        //        Name = name == null ? $"{city}, {state}" : name,
//        //        Address = street,
//        //        Neighborhood = neighborhood,
//        //        City = city,
//        //        State = state,
//        //        Country = country,
//        //        PostalCode = postalCode
//        //    };
//        //}

//        private void OnPostSubmission(PostOutput output, string referenceId, ErrorInfo error)
//        {
//            // do any completion stuff here
//            if (error != null)
//            {
//                // Then the activity was not created.
//                // Use "error" to notify the user what happened. 
//                print($"TriviaContentFacade > Error creating the post activity: {error.ToString()}");
//            }
//            // It's either there is an error or there is an output
//            // but we want to be safe here to prevent run-time error.
//            else if (output == null)
//            {
//                // It's either there is an error or there is an output.
//                print($"TriviaContentFacade > Both error and output are null.");
//            }
//            else
//            {
//                // We really should not encounter this but let's handle this worst-case scenario.
//                print($"TriviaContentFacade > Created post ID = {output.UniqueId}, points earned = {output.Points}");
//            }
//        }

//        private void OnCheckInSubmission(CheckInOutput output, ErrorInfo error)
//        {
//            // do any completion stuff here
//            if (error != null)
//            {
//                // Then the activity was not created.
//                // Use "error" to notify the user what happened. 
//                print($"TriviaContentFacade > Error creating the check-in activity: {error.ToString()}");
//            }
//            // It's either there is an error or there is an output
//            // but we want to be safe here to prevent run-time error.
//            else if (output == null)
//            {
//                // It's either there is an error or there is an output.
//                print($"TriviaContentFacade > Both error and output are null.");
//            }
//            else
//            {
//                // We really should not encounter this but let's handle this worst-case scenario.
//                print($"TriviaContentFacade > Created activity ID = {output.UniqueId}, points earned = {output.Points}");
//            }
//        }

//        private void OnPhotoSubmission(PhotoOutput output, ErrorInfo error)
//        {
//            // do any completion stuff here
//            if (error != null)
//            {
//                // Then the activity was not created.
//                // Use "error" to notify the user what happened. 
//                print($"TriviaContentFacade > Error creating the photo activity: {error.ToString()}");
//            }
//            // It's either there is an error or there is an output
//            // but we want to be safe here to prevent run-time error.
//            else if (output == null)
//            {
//                // It's either there is an error or there is an output.
//                print($"TriviaContentFacade > Both error and output are null.");
//            }
//            else
//            {
//                // We really should not encounter this but let's handle this worst-case scenario.
//                print($"TriviaContentFacade > Created activity ID = {output.UniqueId}, points earned = {output.Points}");
//            }
//        }

//        private void OnWillUpdateAuthToken(string obj)
//        {
//            // do something here if needed
//            print("TriviaContentFacade > OnWillUpdateAuthToken");
//        }

//        private void OnMapActivitySubmission(string mapId, IEnumerable<string> activityIds, ErrorInfo error)
//        {
//            // do any completion stuff here
//            if (error != null)
//            {
//                // Then the activity was not created.
//                // Use "error" to notify the user what happened. 
//                print($"TriviaContentFacade > Error attaching the activities to the map: {error.ToString()}");
//            }
//            else
//            {
//                print($"TriviaContentFacade > the mapId was successlly linked to the activities.");
//            }
//        }

//        private void OnTriviaSubmission(TriviaOutput output, ErrorInfo error)
//        {
//            // do any completion stuff here
//            if (error != null)
//            {
//                // Then the activity was not created.
//                // Use "error" to notify the user what happened. 
//                print($"TriviaContentFacade > Error creating the trivia: {error.ToString()}");
//            }
//            // It's either there is an error or there is an output
//            // but we want to be safe here to prevent run-time error.
//            else if (output == null)
//            {
//                // It's either there is an error or there is an output.
//                print($"TriviaContentFacade > Both error and output are null.");
//            }
//            else
//            {
//                // We really should not encounter this but let's handle this worst-case scenario.
//                print($"TriviaContentFacade > Created trivia ID = {output.UniqueId}, points earned = {output.Points}");
//            }
//        }

//        public void EditTrivia()
//        {
//            print("Start editing trivia question.");

//            // call native UI here...

//        }


//        private void OnEditTriviaDone(TriviaQuestion triviaQuestion)
//        {
//            print("Editing trivia done");

//            //code to update content...

//        }


//        public void Test1()
//        {
//            print("On Test Button 1 Clicked");

//            // call native UI here...            
//            //nativeCall.triviaContentFacade = transform.GetComponent<TriviaContentFacade>();
//            //string activityID = Utilities.GenerateAnchorUniqueId(nativeCall.expData.experienceId, nativeCall.expData.activityGroupId);            
//            //nativeCall.ShowNative("Trivia");
//            //nativeCall.GetCurrentUserAuthToken("Trivia123");

//            //
//            // Test trivia submission.
//            api.CreateTrivia(Guid.NewGuid().ToString(), new TriviaChallengeInput
//            {
//                Answer = 1,
//                Text = "2 - Trivia from AR",
//                ExperienceId = "CEPgRF1gTE2VTi9fAEwD8A==",
//                //Location = new Location
//                //{
//                //    Name = "Seattle, WA",
//                //    State = "WA",
//                //    City = "Seattle",
//                //    Neighborhood = "Seattle",
//                //    Latitude = 1.01,
//                //    Longitude = 1.01
//                //},
//                Location = currentLocation,
//                Options = new[] { "x", "o", "o", "o" },
//            });


//        }


//        public void OnTest1Done(string resultStr)
//        {
//            print("On Test Button 1 Done, called from native");
//            this.test1ResultText.text = resultStr;


//        }


//        public void Test2()
//        {
//            print("On Test Button 2 Clicked");

//            // call native UI here...            
//            //nativeCall.triviaContentFacade = transform.GetComponent<TriviaContentFacade>();                        

//            //
//            // Test post submission.
//            //
//            TestPostSubmission();

//            //
//            // Test check-in submission.
//            //
//            //TestCheckInSubmission();

//            //
//            // Test photo submission.
//            //
//            //TestPhotoSubmission();

//            //nativeCall.OpenGallery("test1");
//        }


//        public void OnTest2Done(string resultStr)
//        {
//            print("On Test Button 2 Done, called from native");
//            this.test2ResultText.text = resultStr;
//        }


//        #region REST methods

//        void TestPostSubmission()
//        {
//            //try
//            //{
//            //    // This fails
//            //    api.CreatePost(new PostActivityInput
//            //    {
//            //        Text = $"Post from AR - {DateTime.Now.ToShortDateString()}",
//            //        ExperienceId = "",
//            //        //Location = new Location
//            //        //{
//            //        //    Name = "Seattle, WA",
//            //        //    State = "WA",
//            //        //    City = "Seattle",
//            //        //    Neighborhood = "Seattle",
//            //        //    Latitude = 1.01,
//            //        //    Longitude = 1.01
//            //        //},
//            //        Location = currentLocation,
//            //        SourceType = PostType.Text
//            //    });
//            //}
//            //catch
//            //{
//            //    print("TestPostSubmission: creating post failed.");
//            //}

//            print("TestPostSubmission: creating a PostType.Text.");
//            api.CreatePost(Guid.NewGuid().ToString(), new PostActivityInput
//            {
//                Text = $"Post from AR - {DateTime.Now.ToShortDateString()}",
//                ExperienceId = "CEPgRF1gTE2VTi9fAEwD8A==",
//                //Location = new Location
//                //{
//                //    Name = "Seattle, WA",
//                //    State = "WA",
//                //    City = "Seattle",
//                //    Neighborhood = "Seattle",
//                //    Latitude = 1.01,
//                //    Longitude = 1.01
//                //},
//                Location = currentLocation,
//                ARInfo = new ARDefinition
//                {
//                    Position = new ARDefinition.ARPosition { X = 10, Y = 10, Z = 10 },
//                    Rotation = new ARDefinition.ARRotation { X = 1, Y = 2, Z = 3 },
//                },
//                SourceType = PostType.Text
//            });

//            //print("TestPostSubmission: creating a PostType.Image.");
//            //api.CreatePost(new PostActivityInput
//            //{
//            //    Text = $"Post from AR - {DateTime.Now.ToShortDateString()}",
//            //    ExperienceId =   "CEPgRF1gTE2VTi9fAEwD8A==",
//            //    //Location = new Location
//            //    //{
//            //    //    Name = "Seattle, WA",
//            //    //    State = "WA",
//            //    //    City = "Seattle",
//            //    //    Neighborhood = "Seattle",
//            //    //    Latitude = 1.01,
//            //    //    Longitude = 1.01
//            //    //},
//            //    Location = currentLocation,
//            //    ResourceURL = "https://socialbeedev.blob.core.windows.net/exp-o-xi-xu03xt6bu-xs-xn-xm-xdn-xa-xfmy-xfzw-eq-eq/q6ZNBSZ9FcGcmp7.jpeg",
//            //    SourceType = PostType.Image
//            //});

//            //print("TestPostSubmission: creating a PostType.Video.");
//            //api.CreatePost(new PostActivityInput
//            //{
//            //    Text = $"Post from AR - {DateTime.Now.ToShortDateString()}",
//            //    Description = "something about the post activity goes here...",
//            //    ExperienceId = "CEPgRF1gTE2VTi9fAEwD8A==",
//            //    Location = new Location
//            //    {
//            //        Name = "Seattle, WA",
//            //        State = "WA",
//            //        City = "Seattle",
//            //        Neighborhood = "Seattle",
//            //        Latitude = 1.01,
//            //        Longitude = 1.01
//            //    },
//            //    ResourceURL = "https://socialbeedev.blob.core.windows.net/exp-w-xj9ha-xu-xv-xoc0c-xu-xt-xe-xsh-xy-xbbrwa-x-eq-eq/9JAxjmBI4EuJIizJmenhKg==RK5GIe6L3GVqaqxqge1z3yr2gOy2.mp4",
//            //    SourceType = PostType.Video
//            //});
//        }

//        void TestCheckInSubmission()
//        {
//            try
//            {
//                // This fails
//                api.CreateCheckIn(Guid.NewGuid().ToString(), new CheckInInput
//                {
//                    Text = $"Check-in from AR - {DateTime.Now.ToShortDateString()}",
//                    ExperienceId = "",
//                    Location = currentLocation,
//                });
//            }
//            catch
//            {
//                print("TestCheckInSubmission: creating check-in failed.");
//            }

//            print("TestCheckInSubmission: creating a check-in.");
//            api.CreateCheckIn(Guid.NewGuid().ToString(), new CheckInInput
//            {
//                Text = $"Check-in from AR - {DateTime.Now.ToShortDateString()}",
//                ExperienceId = "CEPgRF1gTE2VTi9fAEwD8A==",
//                Location = currentLocation,
//            });
//        }

//        void TestPhotoSubmission()
//        {
//            print("TestPhotoSubmission: creating a photo activity.");

//            // Set the experience Id to use.
//            var experienceId = "CEPgRF1gTE2VTi9fAEwD8A==";
//            //
//            // Step 1: Set up the input model.
//            photoInput = new PhotoActivityInput
//            {
//                Text = $"Photo from AR - {DateTime.Now.ToShortDateString()}",
//                ExperienceId = experienceId,
//                Location = currentLocation,
//                SortOrder = DateTime.Now.Ticks,
//            };
//            //
//            // Step 2: Check that we have a valid token. The callback here is the actuall callback
//            //      and should not be changed to something else.
//            if (!api.ValidateTokenFor("OnWillResumePhotoSubmission"))
//                return;

//            //
//            // Step 3: If the token is valid then let's call the helper method
//            //      that will complete the photo submission.
//            ContinuePhotoSubmission();

//            //
//            // Test another submission.
//            //print("TestPhotoSubmission: creating another photo challenge.");
//            //// Create a photo challenge
//            //api.CreatePhoto(new PhotoActivityInput
//            //{
//            //    Text = $"Photo from AR - {DateTime.Now.ToShortDateString()}",
//            //    ExperienceId = experienceId,
//            //    Location = currentLocation,
//            //    SamplePhotos = new List<string> { blobURL },
//            //    IsChallenge = true,
//            //    Keywords = new List<string> { "ice cream", "soft", "sweet" },
//            //    SortOrder = DateTime.Now.Ticks,
//            //});
//        }

//        /// <summary>
//        /// This helper method can also be called from the native callback assigned
//        /// when the photo activity was initially submitted.
//        /// </summary>
//        public void ContinuePhotoSubmission()
//        {
//            // At this point the "photoInput" should not be null.
//            if (photoInput == null)
//            {
//                print("Cannot continue the photo submission because photoInput is null.");
//                return;
//            }

//            var blobURL = api.UploadBlob(photoInput.ExperienceId, true);
//            //print($"TriviaContentFacade: blobURL={blobURL}.");

//            photoInput.SamplePhotos = new List<string> { blobURL };
//            // Create a Photo POI
//            api.CreatePhoto(Guid.NewGuid().ToString(), photoInput);

//            print("Resetting photoInput.");
//            photoInput = null;
//        }

//        public void ShowSelectedMedia(string localPath)
//        {
//            test2ResultText.text = localPath;

//            //var www = UnityWebRequestTexture.GetTexture(localPath);


//            try
//            {
//                //var obj = JsonUtility.FromJson<SelectedMedia>(data);

//                //var media = obj.GetMedia();
//                //if (media == null)
//                //{
//                //    print($"1- TriviaContentFacade: media cannot be inferred from data.");
//                //    return;
//                //}
//                //var str = Encoding.Default.GetString(media);
//                //print($"TriviaContentFacade: media size = {media.Length}.");                


//            }
//            catch
//            {
//                //print($"TriviaContentFacade: cannot deserialize data = {data}");

//                // For debugging only.                
//                //nativeCall.DebugMessage($"1- TriviaContentFacade: cannot deserialize data.");

//                test2ResultText.text = "";
//            }

//        }

//        #endregion



//    }

//}
