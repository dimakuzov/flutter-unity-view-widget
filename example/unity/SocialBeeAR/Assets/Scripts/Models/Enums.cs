namespace SocialBeeAR
{
    /// <summary>
    /// ActivityType represents the type of the activity.
    /// </summary>
    public enum ActivityType
    {
        Undefined = -1, //the default state of selection mode
        
        CheckIn = 10, //The activity for creating a Point-of-Interest (check-in)
        Post = 12, //The activity for creating a Post
        PhotoVideo = 3, //The activity for creating photo content. ToDo: need to change this to Photo
        Video = 7,
        Trivia = 1, //The activity for creating Trivia
        Audio = 17 //The activity for creating audio content
    }

    public enum ActivityStatus
    {
        /// <summary>
        /// The activity has just been created.
        /// </summary>
        New = 0,
        /// <summary>
        /// The activity has been completed and needs approval.
        /// </summary>
        Submitted = 1,
        /// <summary>
        /// The activity has been completed "successfully".
        /// </summary>
        Verified = 2,
        Declined = 3,
        /// <summary>
        /// The activity is a trivia and the answer is incorrect.
        /// </summary>
        IncorrectAnswer = 4,
        Disqualified = 5,
        Deleted = 6,
        Locked = 7,
        PartiallyCompleted = 8,
        Undefined = -1
    }

    public enum AssetType
    {
        Photo,
        Video,
        Audio,
        /// <summary>
        /// The thumbnail for the anchor used for relocalization
        /// </summary>
        Thumbnail
    }
}