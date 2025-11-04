using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SocialBeeAR;

[Serializable]
public class VisionApiRequest : RestSerializable
{
    public IList<AnnotateImageRequest> Requests { get; set; }

    public VisionApiRequest()
    {
        Requests = new List<AnnotateImageRequest>();
    }

    public override string ToString()
    {
        return base.ToString();
    }
      
    [Serializable]
    public class ImageInput : RestSerializable
    {
        /// <summary>
        /// The Base64 representation of the image to be annotated.
        /// </summary>
        public string Content { get; set; }
    }

    [Serializable]
    public class FeatureWeight : RestSerializable
    {
        /// <summary>
        /// The type of the feature to query.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The maximum number of results for this type.
        /// </summary>
        public int MaxResults { get; set; }
    }

    [Serializable]
    public class AnnotateImageRequest : RestSerializable
    {
        public AnnotateImageRequest()
        {
            Features = new List<FeatureWeight>();
        }

        public ImageInput Image { get; set; }
        public IList<FeatureWeight> Features { get; set; }
    }
}

public class VisionApiResponse
{
    public IEnumerable<AnnotateImageResponse> Responses { get; set; }

    public class AnnotateImageResponse
    {
        public IEnumerable<EntityAnnotation> LandmarkAnnotations { get; set; }
        public IEnumerable<EntityAnnotation> LabelAnnotations { get; set; }

        public AnnotateImageResponse()
        {
            LandmarkAnnotations = new List<EntityAnnotation>();
            LabelAnnotations = new List<EntityAnnotation>();
        }
    }

    public VisionApiResponse()
    {
        Responses = new List<AnnotateImageResponse>();
    }

    public static VisionApiResponse Create(string json)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<VisionApiResponse>(json);

            return response;
        }
        catch
        {
            // ToDo: let's log this error and notify ourselves.
            //print($"Cannot deserialize json to create an instance of VisionApiResponse.");
            return null;
        }
    }
}
 
public class EntityAnnotation
{
    /// <summary>
    /// Entity textual description, expressed in its local language.
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Overall score of the result. Range [0,1].
    /// </summary>
    public double Score { get; set; }
    /// <summary>
    /// The relevancy of the ICA (Image Content Annotation) label to the image. For example, the relevancy of "tower" is likely higher to an image containing the detected "Eiffel Tower" than to an image containing a detected distant towering building, even though the confidence that there is a tower in each image may be the same. Range [0, 1].
    /// </summary>
    public double Topicality { get; set; }
}

public class PhotoChallengeKeywords
{
    public IDictionary<string, double> Keywords { get; set; }
    /// <summary>
    /// Returns the <see cref="Keywords"/> in descending order.
    /// </summary>
    public IEnumerable<string> SortedKeywords
    {
        get
        {
            if (Keywords.Count < 1) return new List<string>();

            return Keywords.OrderByDescending(o => o.Value).Select(x => x.Key).Take(10);
        }
    }

    public PhotoChallengeKeywords()
    {
        Keywords = new Dictionary<string, double>();
    }

    public static PhotoChallengeKeywords CreateFrom(VisionApiResponse data)
    {
        //InteractionManager.Instance.OnKeywordsGenerated(keywords);
        if (data == null || data.Responses == null || data.Responses.Count() < 1)
            return new PhotoChallengeKeywords();


        // First is guaranteed not null at this point.
        var labels = data.Responses.First().LabelAnnotations.ToList();
        labels.AddRange(data.Responses.First().LandmarkAnnotations.ToList());

        // Make sure there will be no duplicate labels.
        var keywords = labels.GroupBy(g => g.Description).Select(g => g.First());

        return new PhotoChallengeKeywords
        {
            Keywords = keywords.OrderByDescending(o => o.Score)
                .ThenByDescending(o => o.Topicality)
                .Take(10)
                .ToDictionary(x => x.Description, x => x.Score)
        };
    }
}
