using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SocialBeeAR
{   
    public class ErrorInfo
    {
        /// <summary>
        /// ctor
        /// </summary>
        public ErrorInfo()
        {
            //Data = new Dictionary<string, string>();
        }
        /// <summary>
        /// The identifier of the error.
        /// </summary>
        public int ErrorCode { get; set; }
        /// <summary>
        /// The title of the error.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The description of the error.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// The additional information about the error
        /// </summary>
        //[JsonIgnore]
        //public IDictionary<string, string> Data { get; set; }
        /// <summary>
        /// Create an instance of <see cref="ErrorInfo"/> from a dictionary value.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ErrorInfo Create(IDictionary values)
        {
            if (values == null || !values.Contains("errorCode"))
                return null;

            int.TryParse(values["errorCode"].ToString(), out int errorCode);
            var error = new ErrorInfo { ErrorCode = errorCode };
            if (values.Contains("title"))
                error.Title = values["title"].ToString();

            if (values.Contains("message"))
                error.Title = values["message"].ToString();

            //if (values.Contains("data"))
            //{
            //    foreach (var item in (Dictionary<string, object>)values["data"])
            //    {
            //        error.Data.Add(item.Key, item.Value.ToString());  
            //    }                
            //}                

            return error;
        }
        /// <summary>
        /// Create an instance of <see cref="ErrorInfo"/> from a json value.
        /// </summary>
        /// <param name="json">The json value.</param>
        /// <returns></returns>
        public static ErrorInfo Create(string json)
        {            
            try
            {
                var error = JsonConvert.DeserializeObject<ErrorInfo>(json);

                return error;
            }
            catch
            {
                // ToDo: let's log this error and notify ourselves.
                MessageManager.Instance.DebugMessage("Cannot deserialize json to create an ErrorInfo.");
                return null;
            }
        }

        /// <summary>
        /// Create an instance of <see cref="ErrorInfo"/> with a code of <see cref="ErrorCodes.NetworkError"/>.
        /// </summary>
        /// <returns></returns>
        public static ErrorInfo CreateNetworkError()
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCodes.NetworkError,
                Title = "Network Error",
                Message = "The app is having a connection issue."
            };
        }

        public static ErrorInfo CreateFrom(Exception ex)
        {
            return new ErrorInfo
            {
                ErrorCode = -1,
                Title = ex.Message,
                Message = ex.StackTrace
            };
        }

        public override string ToString()
        {
            var error = $"code={ErrorCode} | title={Title} | message={Message} | data=";
            //foreach(var item in Data)
            //{
            //    error += $" key:{item.Key}, val:{item.Value} <->";
            //}

            return error;
        }
    }

    public class ErrorCodes
    {
        public const int UploadFailed = 10000;
        public const int GetSASFailed = 10100;
        public const int ActivityAlreadyCompleted = 30000;
        public const int NetworkError = -100;
        public const int CannotBeDeleted = 100;
        public const int WayspotInitializationFailed = 200;
        public const int CoverageAreasNotFound = 201;
        public const int LocalizationFailed = 202;

        public static string GetMessage(int errorCode)
        {
            switch (errorCode)
            {
                case NetworkError: return "Internet connection problem.";
                default: return "An unexpected error has occured.";
            }
        }
    }

    [Serializable]
    public class ResultData 
    {
        public GenericConsumedOutput data { get; set; }
        //public int errorCode { get; set; }
        //public string title { get; set; }
        //public string message { get; set; }
        //public bool hasNoError { get; set; }        
    }

    [Serializable]
    public class GenericConsumedOutput
    {
        /// <summary>
        /// The Id of the "submitted/completed activity" generated from the server.
        /// This is not the Id of the consumed "created activity".
        /// </summary>               
        public string completedActivityId { get; set; }
        /// <summary>
        /// The points earned from consuming or completing the activity.
        /// </summary>
        public int points { get; set; }
        /// <summary>
        /// The date the activity was completed.
        /// </summary>
        public DateTime dateCompleted { get; set; }
        /// <summary>
        /// The status of the completed activity.
        /// </summary>
        public int status { get; set; }

        public override string ToString()
        {
            return $"uniqueId={completedActivityId} | points={points} | dateCompleted={dateCompleted} | status={status} | ";
        }
    }
}
