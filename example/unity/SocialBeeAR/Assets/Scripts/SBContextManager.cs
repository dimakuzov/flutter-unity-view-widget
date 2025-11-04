using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocialBeeAR
{
    public enum ARSessionMode
    {
        /// <summary>
        /// This state indicates that the user is either creating or editing activities.
        /// </summary>
        Create,
        /// <summary>
        /// This state indicates that the user is consuming previously created activities.
        /// </summary>
        Consume,
        /// <summary>
        /// This state indicates that the user is in Consume but is creating his/her own content.
        /// </summary>
        CreateInConsume,
        OnBoarding
    }
 
    /// <summary>
    /// This class is for storing the state info synchronized from the native socialbee app.
    /// All other class in AR get/set state info through this class
    /// </summary>
    public class SBContextManager : BaseSingletonClass<SBContextManager>
    {
        public const string VonsDeviceID = "89F461D1-5527-4DD5-8F86-70D5925D0912";
        public bool isARCancelled { get; set; }
        public SBContext context;
        /// <summary>
        /// The last known location of the user.
        /// </summary>
        public Location lastKnownLocation;

        public override void Awake()
        {
            base.Awake();
            // LocationProxy.StartUpdatingLocation(name);
        }

        public void SetSBContext(SBContext contextInfo)
        {
            if (contextInfo == null) return;
            
            context = contextInfo;
            lastKnownLocation = contextInfo.UserLocation;
            print($"lastKnownLocation is null? {lastKnownLocation == null}");
        }

        public void UpdatePoints(int points)
        {
            print($"UpdatePoints");
            if (context == null) return;

            if (context.stats == null)
            {
                print($"context.stats is null");
                context.stats = new ExperienceStatistics();
            }                

            context.stats.Points += points;
            
            PointsBarManager.Instance.SetPoints(context.stats.Points);            
        }

        public void UpdateSteps(int steps, double distance)
        {
            if (context == null) return;

            if (context.stats == null)
                context.stats = new ExperienceStatistics();

            context.stats.Steps += steps;
            context.stats.Distance += distance;

            PointsBarManager.Instance.SetStepsTaken(context.stats.Steps);
            PointsBarManager.Instance.SetDistanceTravelled(context.stats.Distance);
        }

        public void UpdateElevation(double elevation)
        {
            if (context == null) return;

            if (context.stats == null)
                context.stats = new ExperienceStatistics();

            context.stats.Elevation += elevation;
            PointsBarManager.Instance.SetElevatedGained(context.stats.Elevation);
        }

        public void UpdateUserLocation(Location location)
        {
            if (context != null)
            {
                context.UserLocation = location;
                lastKnownLocation = location;
            }
        }

        public void CreateVPSAnchors()
        {
            UpdateIsCreatingGPSOnlyAnchors(false);
        }
        
        public void UpdateIsCreatingGPSOnlyAnchors(bool isCreatingGPSOnlyAnchors)
        {
            context.isCreatingGPSOnlyAnchors = isCreatingGPSOnlyAnchors;
            // if (context.isOffline && !isCreatingGPSOnlyAnchors) {
            //     ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AskDoConnectToInternet);
            // }
            if (!isCreatingGPSOnlyAnchors) {
                // hide offline status from top menu
                UIManager.Instance.ShowOfflineStatus(context.isOffline, true);
            }

            ActivityUIFacade.Instance.isSelectedTypeOfExperience = true;
        }

        public void UpdateConnectionToInternet(bool isConnectionToInternet) {
            if (isConnectionToInternet) {
                //Do something if answer yes
            }
            else {
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AskIndoorOrOutdoor);
            }
        }

        /// <summary>
        /// Updates the user's current location.
        /// </summary>
        /// <param name="info">Formatted location info (format: %f\t%f\t%f\t%f\t%@\t%@\t%@\t%@\t%@\t%@\t%@).</param>
        /// <remarks>
        /// DEV NOTES:
        /// We should put this in SocialBeeARMain or something similar
        /// where the location can be tracked throughout the entire AR session.
        /// The information gathered here should then be passed to the specific facade
        /// that will handle the gathering of information for an activity.
        /// The activity that will be submitted to the API.                
        /// </remarks>
        public void OnUpdateLocation(string info)
        {
            //Commented off by cliff: this is flooding the debug console, enable only when needed
            //print($"OnUpdateLocation()");
            var parts = info.Split('\t');
            if (parts.Length != 11)
            {
                print($"OnUpdateLocation failed, expected parts=11 but got {parts.Length}.");
                // We are expecting exactly 11 parts from the address information.
                // Even if a part is missing, it should be an empty string in the "info".
                // ToDo: log this error on the server and notify ourselves.
                return;
            }
            var lat = double.Parse(parts[0]);
            var lon = double.Parse(parts[1]);
            var speed = double.Parse(parts[2]);
            var accuracy = double.Parse(parts[3]);
            var name = parts[4];
            var street = parts[5];
            var neighborhood = parts[6];
            var city = parts[7];
            var state = parts[8];
            var postalCode = parts[9];
            var country = parts[10];

            //print($"lat={lat} | lon={lon} | speed={speed} | accuracy={accuracy} | name={name} | street={street} | neighborhood={neighborhood} | city={city} | state={state} | postal={postalCode} | country={country}");
            if (context != null)
            {
                context.UserLocation = new Location
                {
                    Latitude = lat,
                    Longitude = lon,
                    Name = name ?? $"{city}, {state}",
                    Address = street,
                    Neighborhood = neighborhood,
                    City = city,
                    State = state,
                    Country = country,
                    PostalCode = postalCode
                };
            }
        }

        #region Helper Methods

        public bool CanShowCursor()
        {
            return !Instance.IsEditCreating() && !Instance.IsConsuming() ||
                   (Instance.IsEditCreating() && context.isCreatingGPSOnlyAnchors) || 
                   Instance.IsCreatingInConsume();
        }
        public bool IsConsuming()
        {
            return context != null && context.IsConsuming();
        }

        public bool IsCreating()
        {
            return context != null && (context.IsCreating() || context.IsCreatingInConsume());
        }
        
        public bool IsEditCreating()
        {
            return context is { isEditing: true };
        }

        public bool IsCreatingInConsume()
        {
            return context != null && context.IsCreatingInConsume();
        }

        public bool IsOnBoarding()
        {
            return context != null && context.IsOnBoarding();
        }

        #endregion
    }

    public class ExperienceStatistics
    {
        public ExperienceStatistics()
        {
            LastOpened = DateTime.UtcNow;
        }
        /// <summary>
        /// The date and time, in UTC, that the experience was last opened.
        /// </summary>
        public DateTime LastOpened { get; private set; }         
        /// <summary>
        /// The total time in minutes that the experience has been running.
        /// </summary>
        public int TotalTime { get; set; }
        /// <summary>
        /// The total number of steps that the user took while creating activities.
        /// </summary>
        public int Steps { get; set; }
        /// <summary>
        /// The total accumulated elevation while creating the activities.
        /// </summary>
        public double Elevation { get; set; }
        /// <summary>
        /// The total distance, in meters, that the user covered while creating activities.
        /// </summary>
        public double Distance { get; set; }
        /// <summary>
        /// The total points the user earned in the experience.
        /// </summary>
        public int Points { get; set; }

        public int GetFinalTotalTime()
        {
            var minutes = (DateTime.UtcNow - LastOpened).TotalMinutes;
            return TotalTime + (int)minutes;
        }

        public string GetFormattedFinalTotalTime()
        {
            var time = GetFinalTotalTime();
            return ExperienceStatistics.LiteralTime(time);
        }

        /// <summary>
        /// Returns the time into a formatted string 00:00.
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public static string LiteralTime(int minutes)
        {
            // 99 hours + 99 minutes = 6039 is the max minutes we can display in the "00:00" format
            if (minutes > 6039)
            {
                return $"{((double)minutes / 60).Shortened()} h";
            }

            var hrs = (int)(minutes / 60);
            var mins = minutes % 60;
            if (hrs > 99)
            {
                hrs = 99;
                mins += 60;
            }

            return $"{hrs:00}:{mins:00}";
        }

    }

}


