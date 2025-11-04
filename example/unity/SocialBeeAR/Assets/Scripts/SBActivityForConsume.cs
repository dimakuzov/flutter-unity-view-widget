using System;
using SocialBeeAR;
using UnityEngine;

public class SBActivityForConsume : MonoBehaviour, ISBActivity
{
    // This is the panel that contains the "Completed" and "Wrong" buttons.
    [SerializeField] protected GameObject completionPanel;

    public Action<IActivityOutput> OnAfterActivitySubmission;

    // Flag that determines if the activity is still being processed by the server.
    protected bool isSaving = false;
    /// <summary>
    /// The data of the completed activity.
    /// </summary>
    protected IActivityInfo completedActivity;
    /// <summary>
    /// The data of the activity that will be completed.
    /// </summary>
    protected IActivityInfo activityInfo;
    public IActivityInfo GetActivityInfo()
    {
        return activityInfo;
    }

    protected NativeCall nativeCall;

    public bool IsTypeOf(Type type)
    {
        return GetActivityInfo().GetType() == type;
    }

    public bool IsPost()
    {
        return GetActivityInfo().GetType() == typeof(PostActivityInfo);
    }

    public bool IsTrivia()
    {
        return GetActivityInfo().GetType() == typeof(TriviaActivityInfo);
    }

    public bool IsPhotoVideo()
    {
        return GetActivityInfo().GetType() == typeof(PhotoVideoActivityInfo);
    }

    public bool IsAudio()
    {
        return GetActivityInfo().GetType() == typeof(AudioActivityInfo);
    }

    void UpdateExperiencePoints(int points)
    {
        activityInfo.Points = points;
        SBContextManager.Instance.UpdatePoints(points);
        PointsBarManager.Instance.SetPoints(SBContextManager.Instance.context.stats?.Points ?? 0);
    }

    public void UpdateActivityStatus(ActivityStatus status)
    {
        activityInfo.Status = status;
        completedActivity.Status = status;
    }

    public void UpdateActivityId(string id)
    {
        throw new NotSupportedException();
    }

    public void UpdateParentId(string id)
    {
        throw new NotSupportedException();
    }

    public string GetActivityId()
    {
        return activityInfo == null ? "" : activityInfo.Id;
    }
     
    public string GetPostDescription()
    {
        throw new NotImplementedException();
    }

    public string GetPostTitle()
    {
        throw new NotImplementedException();
    }

    #region size info

    //The width and height represent the size info of an panel, which will be used for layout management.
    [SerializeField] private float height;
    public float GetHeight()
    {
        return height;
    }

    [SerializeField] private float width;
    public float GetWidth()
    {
        return width;
    }

    #endregion

    public void Start()
    {        
        SBRestClient.Instance.OnActivityHasBeenCompleted += OnActivityHasBeenCompleted;
        nativeCall = NativeCall.Instance;
    }

    private void OnDestroy()
    {
        //Aggregates must be removed when GameObject is destroyed.        
        SBRestClient.Instance.OnActivityHasBeenCompleted -= OnActivityHasBeenCompleted;        
    }

    public void Born(string id, ActivityType type, string experienceId, Pose anchorPose, string parentId = "", string mapId = "", string anchorPayload = "")
    {
        // Born is not allowed for objects in Consume.
        throw new NotSupportedException();
    }

    public virtual void Reborn(IActivityInfo info)
    {        
        activityInfo = info;
        completedActivity = info;

        // The panel is hidden by default.
        completionPanel.SetActive(false);  
        
        //register to activityManager, setting anchorController
        RegisterAsActiveActivity(true); 
        // anchorController = GetComponentInParent<AnchorController>();  
        
        // SetUIMode(SBActivity.UIMode.Play);
    }

    protected void RegisterAsActiveActivity(bool isActive)
    {
        ActivityManager activityManager = transform.parent.gameObject.GetComponent<ActivityManager>();
        if (isActive)
            activityManager.RegisterAsActiveActivity(activityInfo.Id);
        else
            activityManager.ResetActiveActivity();
    }
    
    protected virtual void ApplyDataToUI()
    {
    }

    /// <summary>
    /// The method that is called when an activity, which is not a challenge
    /// is consumed by a user.
    /// </summary>
    public virtual void OnConsumed()
    {
    }

    /// <summary>
    /// The callback for when the API call failed.
    /// </summary>
    public virtual void OnFailedSave(ErrorInfo error)
    {
        isSaving = false;
        completionPanel.SetActive(false);
        //
        // ToDo: do the necessary things here like updating the UI elements, etc.
        //
        //
    }

    protected void ContinueOnError(ErrorInfo error)
    {
        BottomPanelManager.Instance.ShowMessagePanel(error?.Message ?? "Your content cannot be uploaded at this time. Please try again later.");
        OnFailedSave(error);
    }

    /// <summary>
    /// The callback for when the API call completed successfully.
    /// </summary>
    public virtual void OnSuccessfulSave()
    {
        isSaving = false;
        completionPanel.SetActive(false);
        // ToDo: do the necessary things here like updating the UI elements, etc.
        //
        //
    }

    public virtual void OnSubmit()
    {
        // This ensures we will not run the Save process twice, unnecessarily.
        if (isSaving)
        {
            print("Still saving, exiting now.");
            return;
        }

        isSaving = true;

        // 
        // Dev notes: this is where we should trigger the animation
        //      while the app is waiting for the result from the API.
        //      The animation should be handled by another component (class), trigger the delegate here.
        //

        // ToDo: Hide the submit button here
        
    }

    protected void OnActivityHasBeenCompleted(IConsumedActivityOutput output, string referenceId, ErrorInfo error)
    {
        if (error != null)
        {
            // Then the activity was not completed.
            // Use "error" to notify the user what happened.
            print($"SBActivityForConsume.OnActivityHasBeenCompleted > Error completing the activity: {error}");

            OnFailedSave(error);
            return;
        }

        // It's either there is an error or there is an output
        // but we want to be safe here to prevent run-time error.
        if (output == null)
        {
            // We really should not encounter this but let's handle this worst-case scenario.
            print($"SBActivityForConsume.OnActivityHasBeenCompleted > Both error and output are null.");
            return;
        }

        print($"SBActivityForConsume.OnActivityHasBeenCompleted: {output.ActivityId}");

        
        if (referenceId != activityInfo.Id)
        {
            // This activity is not the one that was completed.
            print($"SBActivity.OnActivityHasBeenCompleted - exiting now: This activity is not the one that was completed. referenceId={referenceId}, myId={activityInfo.Id}");
            return;
        }

        // At this point we are guaranteed that the activity was successfully consumed.
        print($"SBActivityForConsume.OnActivityHasBeenCompleted > Completed activity ID = {output.UniqueId}, points earned = {output.Points}, status = {output.Status}");
        UpdateInfo(output);
        UpdateExperiencePoints(output.Points);
        // We are calling success as long as the activity is completed either with a wrong or correct answer
        // or a accepted or failed image keywords.
        OnSuccessfulSave();
        print($"SBActivityForConsume.OnActivityHasBeenCompleted > OnActivityHasBeenCompleted: {completedActivity.ToJson()}");
        nativeCall.OnActivityCompleted(completedActivity.ToJson(), ((int)output.Type).ToString());
    }

    void UpdateInfo(IConsumedActivityOutput output)
    {
        activityInfo.Status = output.Status;
        completedActivity.Status = output.Status;
        completedActivity.DateCompleted = output.DateCompleted;
        completedActivity.PointsEarned = output.Points;
        completedActivity.CompletedId = output.UniqueId;
    }
}
