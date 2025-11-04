
using System.Collections.Generic;
using System.Linq;

namespace SocialBeeAR
{
    public class TriviaActivityInfo : ActivityInfo
    {         
        public List<string> OptionList { get; set; }
        public List<string> Hints { get; set; }
        public bool IsRandomEnabled { get; set; }

        public int AnswerIndex { get; set; }
        public int UserAnswerIndex { get; set; }
        //public List<int> DisplaySequence { get; set; }
        /// <summary>
        /// Helper method that returns the OptionList in a comma-delimited format.
        /// </summary>
        public string AllOptions
        {
            get
            {
                return OptionList.Count < 1 ? "" : string.Join(",", OptionList);
            }
        }

        public TriviaActivityInfo()
        {
            Type = ActivityType.Trivia;
            //DisplaySequence = new List<int>();
            // Set option count
            OptionList = new List<string>() {"","","",""};
            Hints = new List<string>();
            AnswerIndex = -1;
            UserAnswerIndex = -1;
            SetIsChallenge(true);
        }

        public List<string> AddHint(string hint)
        {
            if (Hints == null) Hints = new List<string>();
            Hints.Add(hint);

            return Hints;
        }

        public List<string> SetAsFirstHint(string hint)
        {
            if (Hints == null) Hints = new List<string>();
            if (Hints.Count < 1)
                Hints.Add(hint);
            else
                Hints[0] = hint;

            return Hints;
        }

        /// <summary>
        /// Helper method for getting the option at an index.
        /// If the index doesn't exists, this returns an empty string.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <remarks>This is to prevent the index out of bound run-time error.</remarks>
        public string GetOptionAt(int index)
        {
            return index > OptionList.Count - 1 ? "" : OptionList[index];
        }

        public void SetOption(int index, string optionStr)
        {
            OptionList[index] = optionStr;
        }
        //public int GetShuffledIndex(int originalIndex)
        //{
        //    int currentIndex = -1;
        //    for (int i = 0; i < DisplaySequence.Count; i++)
        //    {
        //        if (DisplaySequence[i] == originalIndex)
        //        {
        //            currentIndex = i;
        //            break;
        //        }
        //    }

        //    return currentIndex;
        //}
        public string ConcludeSequence()
        {            
            //shuffle if it's set to random
            if (IsRandomEnabled)
            {
                //print($"AnswerIndex current={AnswerIndex}");
                var triviaOptions = new List<SimpleFlagModel>();
                for (int i = 0; i < OptionList.Count; i++)
                    triviaOptions.Add(new SimpleFlagModel { Text = OptionList[i], IsActive = i == AnswerIndex });

                var newOptions = Utilities.RandomSortList(triviaOptions);
                AnswerIndex = newOptions.IndexOf(triviaOptions.Find(x => x.IsActive));                
                //print($"AnswerIndex new={AnswerIndex} | original sequence: {string.Join(",",OptionList)}");
                OptionList.Clear();
                OptionList.AddRange(newOptions.Select(x => x.Text));
                //print($"new sequence: {string.Join(",", OptionList)}");
            }                

            string sequenceStr = "";
            //for (int i = 0; i < DisplaySequence.Count; i++) //Todo: currently trivia only have 4 options, 'count' cannot be more than 4
            //    sequenceStr += DisplaySequence[i];

            return sequenceStr;
        }

        public override string ToString()
        {
            var value = base.ToString();
            value += $" | options={string.Join(",", OptionList)} | hints={string.Join(",", Hints)} | answer={AnswerIndex} ";

            return value;
        }

        public override IActivityInfo Clone()
        {
            // Let's not do the MemoryStream technique + serialization and deserialization
            // as it's very expensive and unnecessary.
            return new TriviaActivityInfo
            {
                Id = Id,
                ExperienceId = ExperienceId,
                Title = Title,
                ParentId = ParentId,
                MapId = MapId,
                AnchorPayload = AnchorPayload,
                Points = Points,
                Pose = Pose,
                IsEditing = IsEditing,
                IsRandomEnabled = IsRandomEnabled,
                AnswerIndex = AnswerIndex,
                UserAnswerIndex = UserAnswerIndex,
                OptionList = OptionList == null ? new List<string>() : new List<string>(OptionList),
                Hints = Hints == null ? new List<string>() : new List<string>(Hints),
                //DisplaySequence = DisplaySequence == null ? new List<int>() : new List<int>(DisplaySequence),
                Status = Status
            };
        }
    }

}