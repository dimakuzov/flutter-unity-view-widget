using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using GifPlayer;
using UnityEngine;
using UnityEngine.UI;


namespace SocialBeeAR
{
    
    public class TriviaActivity : SBActivity
    {
        
        [SerializeField] private GameObject panelParent;
        [SerializeField] protected GameObject correctSign;
        [SerializeField] protected GameObject wrongSign;
        [SerializeField] protected GameObject correctSignFrame;
        [SerializeField] protected GameObject wrongSignFrame;
        [SerializeField] private GameObject consumeLoader;

        //---------------------------- edit panel ----------------------------
        [SerializeField] private GameObject edit_question;
        [SerializeField] private GameObject edit_questionCover;

        [SerializeField] private GameObject edit_option1;
        [SerializeField] private GameObject edit_option2;
        [SerializeField] private GameObject edit_option3;
        [SerializeField] private GameObject edit_option4;

        [SerializeField] private GameObject edit_option1Cover;
        [SerializeField] private GameObject edit_option2Cover;
        [SerializeField] private GameObject edit_option3Cover;
        [SerializeField] private GameObject edit_option4Cover;

        [SerializeField] private Toggle edit_isRandom;

        [SerializeField] private GameObject edit_hint;
        [SerializeField] private GameObject eidt_hintCover;

        private List<GameObject> editOptionObjList;
        private List<GameObject> editOptionCoverObjList;

        //validation elements
        [SerializeField] private GameObject noneSelectedErrorSign;

        

        //---------------------------- play panel ----------------------------
        [SerializeField] private GameObject play_question;

        [SerializeField] private GameObject play_option1;
        [SerializeField] private GameObject play_option2;
        [SerializeField] private GameObject play_option3;
        [SerializeField] private GameObject play_option4;

        [SerializeField] private GameObject play_hintPanel;
        [SerializeField] private Text play_hint;


        private List<GameObject> playOptionObjList;
        private List<SimpleFlagModel> triviaOptions = new List<SimpleFlagModel>();

        private bool isHintClicked;
        private Vector3 posWhenHintClosed = new Vector3(0, 50, 0);
        private Vector3 posWhenHintOpen = new Vector3(0, 0, 0);
        private Vector2 sizeWhenHintClosed = new Vector2(518f, 660f);
        private Vector2 sizeWhenHintOpen = new Vector2(518f, 760f);
        

        public void Awake()
        {
            editOptionObjList = new List<GameObject>();
            editOptionObjList.Add(edit_option1);
            editOptionObjList.Add(edit_option2);
            editOptionObjList.Add(edit_option3);
            editOptionObjList.Add(edit_option4);

            editOptionCoverObjList = new List<GameObject>();
            editOptionCoverObjList.Add(edit_option1Cover);
            editOptionCoverObjList.Add(edit_option2Cover);
            editOptionCoverObjList.Add(edit_option3Cover);
            editOptionCoverObjList.Add(edit_option4Cover);

            playOptionObjList = new List<GameObject>();
            playOptionObjList.Add(play_option1);
            playOptionObjList.Add(play_option2);
            playOptionObjList.Add(play_option3);
            playOptionObjList.Add(play_option4);
        }
         
        public override void Reborn(IActivityInfo activityInfo)
        {
            print($"TriviaActivity.Reborn - started: activityInfo: {activityInfo}");
            if (activityInfo.ParentId.IsNullOrWhiteSpace())
                throw new ArgumentNullException("A trivia activity cannot be reborn without a parent. ParentId is required.");

            isReborn = true;
            base.Reborn(activityInfo);
            
            play_hintPanel.SetActive(!SBContextManager.Instance.context.IsConsuming());
            
            //apply data to UI
            ApplyDataToUI();
            anchorController.TriviaExist(true);
        }

        public override void Born(string id, ActivityType type, string experienceId, Pose anchorPose, string parentId = "", string mapId = "", string anchorPayload = "")
        {
            if (parentId.IsNullOrWhiteSpace())
                throw new ArgumentNullException("A trivia activity cannot be created without a parent. ParentId is required.");

            print($"TriviaActivity.Born - started: mapId={mapId}");
            base.Born(id, type, experienceId, anchorPose, parentId, mapId, anchorPayload);
            anchorController.TriviaExist(true);
            OnHintClicked();
            OnEditQuestion();
        }

        protected override void ApplyDataToUI()
        {
            //1. updating edit UI
            //the question list in edit mode is always in the ascend order, while in the preview mode could be random
            // print("ApplyDataToUI > ApplyDataToEditPanel... #debugconsume");
            ApplyDataToEditPanel();

            //2. updating preview UI
            // print("ApplyDataToUI > ApplyDataToPreviewPlayPanel... #debugconsume");
            ApplyDataToPreviewPlayPanel();

            // print($"ApplyDataToUI > IsConsuming={SBContextManager.Instance.context.IsConsuming()} | Status={completedActivity.Status.ToString()} #debugconsume");
            if (!SBContextManager.Instance.context.IsConsuming() ||
                completedActivity.Status == ActivityStatus.New) return;
            if (completedActivity.Status == ActivityStatus.Verified)
            {
                //There is no need to play completion effect(sound/animation) for reborn panels!
                //SetPanelToCorrectWithEffect();
                ShowCorrectOrWrongSign(true);
            }
            else
            {
                //There is no need to play completion effect(sound/animation) for reborn panels!
                //SetPanelToFailedWithEffect();
                ShowCorrectOrWrongSign(false);
            }
        }

        /// <summary>
        /// Set the values of the UI elements from the activityInfo.
        /// </summary>
        private void ApplyDataToEditPanel()
        {
            var activityInfo = (TriviaActivityInfo)this.activityInfo;

            edit_question.GetComponentInChildren<InputField>().text = activityInfo.Title;

            ApplyTriviaOptions(activityInfo);
             
            edit_isRandom.GetComponent<Toggle>().isOn = activityInfo.IsRandomEnabled;
            edit_hint.GetComponentInChildren<InputField>().text = activityInfo.Hints.Any()
                ? activityInfo.Hints.First()
                : "";
            
            //reset the 'save' button to 'disabled'
            print("TriviaActivity > ApplyDataToEditPanel > EnableSaveButton... #debugconsume");
            EnableSaveButton(false);
        }

        private void ApplyTriviaOptions(TriviaActivityInfo activityInfo)
        {
            edit_option1.GetComponentInChildren<InputField>().text = activityInfo.GetOptionAt(0);
            edit_option2.GetComponentInChildren<InputField>().text = activityInfo.GetOptionAt(1);
            edit_option3.GetComponentInChildren<InputField>().text = activityInfo.GetOptionAt(2);
            edit_option4.GetComponentInChildren<InputField>().text = activityInfo.GetOptionAt(3);
            print($"ApplyTriviaOptions index={activityInfo.AnswerIndex}");
            if (activityInfo.AnswerIndex != -1)
                SelectEditToggle(activityInfo.AnswerIndex);
        }

        /// <summary>
        /// Set the values of the UI elements from the activityInfo.
        /// </summary>
        protected override void ApplyDataToPreviewPlayPanel()
        {
            var triviaInfo = (TriviaActivityInfo)this.activityInfo;
            Text play_questionText = play_question.GetComponentInChildren<Text>();
            play_questionText.text = activityInfo.Title;

            print($"triviaInfo={triviaInfo} #debugconsume");
            for (var i = 0; i < playOptionObjList.Count && i<triviaInfo.OptionList.Count; i++)
            {
                print($"option {i+1} = {triviaInfo.OptionList[i]} #debugconsume");
                playOptionObjList[i].GetComponentInChildren<Text>().text = triviaInfo.GetOptionAt(i);
                editOptionObjList[i].GetComponentInChildren<InputField>().text = triviaInfo.GetOptionAt(i);
                playOptionObjList[i].SetActive(!string.IsNullOrWhiteSpace(triviaInfo.GetOptionAt(i)));
                
                editOptionObjList[i].GetComponent<Toggle>().isOn = triviaInfo.AnswerIndex == i;
            }
            // print($"playOptionObjList= OptionList={triviaInfo.OptionList.Count} #debugconsume **");
            //
            // print("playOptionObjList end loop #debugconsume");
            //select from creator's answer, only in preview mode
            if (mode is UIMode.Edit or UIMode.Preview)
            {
                //by Cliff: shuffling is needed, as this is for preview, not edit (even through we don't save the sequence)
                // If we are editing then we don't need to shuffle the answer
                //int answerIndexAfterShuffle = activityInfo.GetShuffledIndex(activityInfo.AnswerIndex);
                //SelectPlayToggle(answerIndexAfterShuffle);
                SelectPlayToggle(triviaInfo.AnswerIndex);
            }

            play_hint.text = triviaInfo.Hints.Any()
                ? triviaInfo.Hints.First()
                : "";
            
            // print($"TriviaActivity > ApplyDataToPreviewPlayPanel > correctSignFrame.GetComponentInChildren #debugconsume");
            correctSignFrame.GetComponentInChildren<Text>().text = activityInfo.Title;
            // print($"TriviaActivity > ApplyDataToPreviewPlayPanel > wrongSignFrame.GetComponentInChildren #debugconsume");
            wrongSignFrame.GetComponentInChildren<Text>().text = activityInfo.Title;

            FontSizeControl(play_questionText.gameObject, play_questionText, play_questionText.text);
            FontSizeControl(play_hint.gameObject, play_hint, play_hint.text);
            // print($"TriviaActivity > ApplyDataToPreviewPlayPanel > exiting... #debugconsume");
        }


        //-------------------------- toggle group management -----------------------------

        [SerializeField] private ToggleGroup editToggleGroup;
        [SerializeField] private ToggleGroup playToggleGroup;


        public Toggle creatorSelectedOption
        {
            get
            {
                return editToggleGroup.ActiveToggles().FirstOrDefault();
            }
        }


        public Toggle consumerSelectedOption
        {
            get
            {
                return playToggleGroup.ActiveToggles().FirstOrDefault();
            }
        }


        public int GetCreatorSelectedOptionIndex()
        {
            return GetSelectedOptionIndex(true);
        }


        public int GetConsumerSelectedOptionIndex()
        {
            return GetSelectedOptionIndex(false);
        }


        private int GetSelectedOptionIndex(bool isCreator)
        {
            ToggleGroup toggleGroup = isCreator ? editToggleGroup : playToggleGroup;

            int selectedIndex = -1;
            var toggles = toggleGroup.GetComponentsInChildren<Toggle>();
            for (int i = 0; i < toggles.Length; i++)
            {
                if (toggles[i].isOn == true)
                {
                    selectedIndex = i;
                    break;
                }
            }

            return selectedIndex;
        }


        private void SelectEditToggle(int index)
        {
            print($"SelectEditToggle index={index}");
            for (int i = 0; i < editOptionObjList.Count; i++)
            {
                editOptionObjList[i].GetComponentInChildren<Toggle>().isOn = (i == index ? true : false);
            }
        }


        private void SelectPlayToggle(int index)
        {
            for (int i = 0; i < playOptionObjList.Count; i++)
            {
                playOptionObjList[i].GetComponentInChildren<Toggle>().isOn = (i == index ? true : false);
            }
        }


        //------------------------- handle edit interaction --------------------------


        public void OnEditQuestion()
        {
            //print("Start editing Trivia question");
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOn, altText: "Enter the question for your trivia");
            OnEditTextBox(edit_question, edit_questionCover, (input) =>
            {
                uiValues.Title = input.text;
                if (input.wasCanceled)
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOff);
                else
                    OnEditOption(0);
            });
        }


        public void OnEditOption(int index)
        {
            var uiValues = (TriviaActivityInfo)this.uiValues;
            var activityInfo = (TriviaActivityInfo)this.activityInfo;
            
            if (index < 3)
            {
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOn, altText: $"Enter the answer for option {(index+1)}");
            }
            OnEditTriviaOption(index, editOptionObjList[index], editOptionCoverObjList[index], (id, input) =>
            {
                uiValues.SetOption(id, input.text);
                if (input.wasCanceled)
                {
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOff);
                    return;
                }
                if (index < 3)
                {
                    OnEditOption(index+1);
                }
                else
                {
                    OnEditHint();
                }
            });
        }


        private void OnEditTriviaOption(int index, GameObject textBoxObj, GameObject coverObj, Action<int, InputField> postAction)
        {
            //disable cover
            coverObj.SetActive(false);

            // Activate input field
            InputField input = textBoxObj.GetComponentInChildren<InputField>();
            input.interactable = true;
            input.ActivateInputField();
            var ph = textBoxObj.GetComponentInChildren<Text>();
            if (input.touchScreenKeyboard != null)
            {
                input.touchScreenKeyboard.text = ph != null ? ph.text : input.text;    
            }

            input.onEndEdit.AddListener(delegate { OnEditTriviaOptionDone(index, textBoxObj, coverObj, postAction); });
        }


        private void OnEditTriviaOptionDone(int index, GameObject textBoxObj, GameObject coverObj, Action<int, InputField> postAction)
        {
            //disable input field
            InputField input = textBoxObj.GetComponentInChildren<InputField>();
            input.DeactivateInputField();
            input.interactable = false;

            //re-enable cover, so that user can edit again
            coverObj.SetActive(true);

            //post action: e.g. using the input value to update some attributes
            postAction?.Invoke(index, input);
        }


        public void OnEditSetRandom()
        {
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOff);
            var uiValues = (TriviaActivityInfo)this.uiValues;
            var answerIndex = GetCreatorSelectedOptionIndex();
            uiValues.AnswerIndex = answerIndex;
            uiValues.IsRandomEnabled = edit_isRandom.isOn;
            if (uiValues.IsRandomEnabled)
            {
                uiValues.ConcludeSequence();
                ApplyTriviaOptions(uiValues);
            }                
            print(string.Format("Trivia: isRandom: {0}", uiValues.IsRandomEnabled));
        }


        public void OnEditHint()
        {
            var uiValues = (TriviaActivityInfo)this.uiValues;
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOn, altText: $"Enter the hint for this trivia");
            OnEditTextBox(edit_hint, eidt_hintCover, (input) =>
            {
                // ToDo: we only have one hints for now.                
                uiValues.SetAsFirstHint(input.text);
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOff);
                // This should be the code when we start supporting multiple hints.
                //uiValues.AddHint(input.text);
            });
        }


        //------------------------- bottom button interaction --------------------------

        private void ResetValidationStatus()
        {
            edit_question.transform.Find("ErrorSign").gameObject.SetActive(false);
            noneSelectedErrorSign.SetActive(false);

            for (int i = 0; i < editOptionObjList.Count; i++)
            {
                editOptionObjList[i].transform.Find("OptionInput/ErrorSign").gameObject.SetActive(false);
            }
        }


        public override void OnSave()
        {
            if (!saveEditButton.interactable) {
                return;
            }
            
            var uiValues = (TriviaActivityInfo)this.uiValues;
            uiValues.AnswerIndex = GetCreatorSelectedOptionIndex();

            // UI validation
            ResetValidationStatus();

            if (uiValues.Title.IsNullOrWhiteSpace())
            {
                edit_question.transform.Find("ErrorSign").gameObject.SetActive(true);
                BottomPanelManager.Instance.ShowMessagePanel("Please enter a question for your trivia.");
                return;
            }

            if (uiValues.AnswerIndex < 0 || uiValues.AnswerIndex > uiValues.OptionList.Count - 1)
            {
                noneSelectedErrorSign.SetActive(true);
                BottomPanelManager.Instance.ShowMessagePanel("Please select an answer.");
                return;
            }

            if (uiValues.OptionList[uiValues.AnswerIndex].IsNullOrWhiteSpace()) {
                BottomPanelManager.Instance.ShowMessagePanel("Answer is empty.");
                return;
            }

            int countOptions = 0;
            foreach (var option in uiValues.OptionList) {
                if (!String.IsNullOrWhiteSpace(option)) {
                    countOptions++;
                }
            }
            
            if (countOptions < 2)
            {
                BottomPanelManager.Instance.ShowMessagePanel("At least two options are required.");
                return;
            }

            //var groupedOptions = uiValues.OptionList
            //        .GroupBy(x => x.Trim().ToLower())
            //        .Select(g => new
            //        {
            //            option = g.Key,
            //            count = g.Count()
            //        });
            //if (groupedOptions.Any(x => x.count > 1))
            //{
            //    // Send focus on the textbox that has the duplicate value.
            //    BottomPanelManager.Instance.ShowMessagePanel("You cannot have two or more options having the same values.");
            //    return;
            //}

            List<int> dupIndex = Utilities.FindDupIndex(uiValues.OptionList);
            if(dupIndex.Count >= 2)
            {
                for (int i = 0; i < dupIndex.Count; i++)
                {
                    int index = dupIndex[i];
                    editOptionObjList[index].transform.Find("OptionInput/ErrorSign").gameObject.SetActive(true);
                }
                BottomPanelManager.Instance.ShowMessagePanel("You cannot have two or more options having the same values.");
                return;
            }

            List<String> tempOptionList = new List<string>();
            int tempAnswerIndex = -1;
            
            for (int i = 0; i < playOptionObjList.Count; i++) {
                if (!String.IsNullOrWhiteSpace(uiValues.OptionList[i])) {
                    tempOptionList.Add(uiValues.OptionList[i]);
                    if (i == uiValues.AnswerIndex) {
                        tempAnswerIndex = tempOptionList.Count - 1;
                    }
                }
            }

            uiValues.OptionList = tempOptionList;
            for (int i = 0; i < playOptionObjList.Count; i++) {
                if (uiValues.OptionList.Count - 1 < i) {
                    uiValues.OptionList.Add("");
                }
            }
            uiValues.AnswerIndex = tempAnswerIndex;

            Debug.Log("Saving trivia info...");
            BottomPanelManager.Instance.ShowMessagePanel("Saving your trivia...");

            //save
            base.OnSave();//this has to be called before the action for submitting data
            SubmitTrivia();
        }


        public override void OnEdit()
        {
            uiValues = activityInfo.Clone();
            base.OnEdit();
            OnEditQuestion();
        }


        public override void OnCancelEdit()
        {            
            // There's no need to restore from any instance
            // as we are only updating the "activityInfo"
            // when we are saving the data.
            ApplyDataToUI();

            base.OnCancelEdit();
        }

        public override void OnSuccessfulSave()
        {
            print($"TriviaActivity.OnSuccessfulSave");
            base.OnSuccessfulSave();
        }

        public override void OnFailedSave(ErrorInfo error)
        {
            print("ErrorHandler > TriviaActivity.OnFailedSave");
            print($"errorCode: {error.ErrorCode}");
            base.OnFailedSave(error);
        }

        /// <summary>
        /// The callback function when the API call for consuming an activity succeeded.
        /// </summary>
        /// <remarks>
        /// A consumed activity can return as incorrectly answered (trivia) or a photo that failed keywords validation.
        /// This is still the callback that will be called as those are treated as successful API calls.
        /// </remarks>
        public override void OnSuccessfulConsume()
        {
            //
            // ToDo: stop loader animation here
            //
            print($"TriviaActivity.OnSuccessfulConsume");
            RunCompleteActivity();
        }

        public override void OnComplete()
        {
            //
            // ToDo: run loader animation here.
            //      The animation will be hidden in the API callbacks "OnSuccessfulSave" and "OnFailedSave".            
            //
            print($"TriviaActivity.OnComplete");
            ConsumeTrivia();
            
            if(!SBContextManager.Instance.context.isOffline) {
                ShowLoader(true);
            }
        }

        void RunCompleteActivity()
        {
            var uiValues = (TriviaActivityInfo)this.uiValues;

            //conclude data
            uiValues.UserAnswerIndex = GetConsumerSelectedOptionIndex();
            //bool userCorrect = (uiValues.UserAnswerIndex == uiValues.AnswerIndex);

            isCompleted = true;

            if (completedActivity.Status == ActivityStatus.Verified)
            {
                SetPanelToCorrectWithEffect();
            }
            else
            {
                SetPanelToFailedWithEffect();
            }
            
            ShowLoader(false);
        }
        
        private void ShowLoader(bool visible) {
            if (consumeLoader == null) {
                return;
            }

            UnityGif uGif = consumeLoader.GetComponentInChildren<UnityGif>();
            if (visible) {
                consumeLoader.SetActive(true);
                Vector3 finalPos;
                Vector2 finalSize;
                
                
                if (isHintClicked) {
                    finalSize = sizeWhenHintOpen - new Vector2(32.0f, 32.0f);
                    finalPos = posWhenHintOpen;
                }
                else {
                    finalSize = sizeWhenHintClosed - new Vector2(32.0f, 32.0f);
                    finalPos = posWhenHintClosed;
                }

                consumeLoader.GetComponent<RectTransform>().sizeDelta = finalSize; //set final size for the frame
                consumeLoader.transform.localPosition = finalPos; //set final position for the frame

                uGif?.Play();
            }
            else {
                uGif?.Pause();
                consumeLoader.SetActive(false);
            }
        }

        
        void SetPanelToCorrectWithEffect()
        {
            //play audio
            AudioManager.Instance.PlayAudio(AudioManager.AudioOption.Correct);

            //rotate only when it's correct
            panelParent.transform.DOLocalRotate(new Vector3(0, 360, 0), 0.75f, RotateMode.FastBeyond360).OnComplete(() =>
            {
                //show frame
                ShowCorrectOrWrongSign(true, () =>
                {
                    if (GetComponentInParent<ActivityManager>().ActivitiesCompleted)
                    {
                        MiniMapManager.Instance.SetGreenPoint(GetComponentInParent<AnchorController>().GetAnchorInfo().id);
                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AllActivitiesComplete);
                    }
                    else {
                        OffScreenIndicatorManager.Instance.ShowArrow();
                    }
                });

                if (!isReborn)
                    base.OnComplete();
            });
        }

        void SetPanelToFailedWithEffect()
        {          
            //play audio
            AudioManager.Instance.PlayAudio(AudioManager.AudioOption.Wrong); 

            //shake panel!
            gameObject.GetComponentInChildren<ObjectShaker>().Shake(() =>
            {
                //show frame
                ShowCorrectOrWrongSign(false, () =>
                {
                    if (GetComponentInParent<ActivityManager>().ActivitiesCompleted)
                    {                        
                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AllActivitiesComplete);
                        MiniMapManager.Instance.SetGreenPoint(GetComponentInParent<AnchorController>().GetAnchorInfo().id);
                    }
                    else {
                        OffScreenIndicatorManager.Instance.ShowArrow();
                    }
                });                

                if (!isReborn)
                    base.OnComplete();
            });
        }

        private void ShowCorrectOrWrongSign(bool userCorrect, Action callback = null)
        {
            Vector3 finalPos;
            Vector2 finalSize;
            if (isHintClicked)
            {
                finalSize = sizeWhenHintOpen;
                finalPos = posWhenHintOpen;
            }
            else
            {
                finalSize = sizeWhenHintClosed;
                finalPos = posWhenHintClosed;
            }
            
            //set final size for the frame
            correctSignFrame.GetComponent<RectTransform>().sizeDelta = finalSize;
            wrongSignFrame.GetComponent<RectTransform>().sizeDelta = finalSize;
            
            //set final position for the frame
            correctSignFrame.transform.localPosition = finalPos;
            wrongSignFrame.transform.localPosition = finalPos;
            
            //show frame
            correctSign.SetActive(userCorrect);
            wrongSign.SetActive(!userCorrect);

            callback?.Invoke();
        }

        public void OnHintClicked()
        {
            play_hintPanel.SetActive(true);
            isHintClicked = true;
        }

        void SubmitTrivia()
        {
            var activityInfo = (TriviaActivityInfo)uiValues;

            print($"TriviaActivity.SubmitTrivia: submitting trivia with ID = {activityInfo.Id} | info={activityInfo}");
 
            Location anchorLocation = GetComponentInParent<AnchorController>().GetSBLocationInfo();
            var input = TriviaChallengeInput.CreateFrom(activityInfo,
                SBContextManager.Instance.context.experienceId,
                SBContextManager.Instance.context.collectionId,
                anchorLocation,
                SBContextManager.Instance.context.isPlanning);
            input.ARAnchorId = GetComponentInParent<AnchorController>().GetAnchorInfo().id;

            MessageManager.Instance.DebugMessage($"Creating TriviaActivity at Latitude={input.Location.Latitude}, Longitude={input.Location.Longitude}");
            
            if (activityInfo.IsEditing)
                SBRestClient.Instance.UpdateTrivia(activityInfo.Id, input);
            else
                SBRestClient.Instance.CreateTrivia(activityInfo.Id, input);             
        }
        
        void ConsumeTrivia()
        {
            if (isSaving)
                return;

            if (activityInfo.Status != ActivityStatus.New)
            {
                print("ConsumeTrivia: You have already completed this activity.");
                // There is no need to tell the user about this, just ignore the action.
                return;
            }
            
            isSaving = true;
            print($"ConsumeTrivia: completing the trivia activity...");
            SBRestClient.Instance.ConsumeTrivia(activityInfo.Id, new TriviaConsumeInput
            {
                ExperienceId = SBContextManager.Instance.context.experienceId,
                ActivityId = activityInfo.Id,
                Hints = 0, // ToDo: read this from the actual consumption of hints
                Answer = GetConsumerSelectedOptionIndex()
            }, completedActivity.Status);
        }

        public void StartTrackCharacters(InputField input)
        {
            FontSizeControl(input.gameObject, input.textComponent, input.text);//update the font size according to the number of characters
            UIManager.Instance.StartTrack(input);
            EnableSaveButton(true);
        }

        public void FinishTrackCharacters() {
            UIManager.Instance.FinishTrack();
        }
        
        private void FontSizeControl(GameObject go, Text text, string textValue)
        {
            if (go.name == "Question")
            {
                if (textValue.Length > 0 && textValue.Length <= 24)
                {
                    text.fontSize = 27;
                }
                else if (textValue.Length > 24 && textValue.Length <= 48)
                {
                    text.fontSize = 23;
                }
            }
            else if(go.name == "Hint")
            {
                if (textValue.Length > 0 && textValue.Length <= 84)
                {
                    text.fontSize = 25;
                }
                else if (textValue.Length > 84 && textValue.Length <= 144)
                {
                    text.fontSize = 19;
                }
            }
        }

    }
}
