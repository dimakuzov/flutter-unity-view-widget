// using System;
// using System.Collections;
// using UnityEngine;
// using UnityEngine.UI;
// using SWS;
// using UnityEngine.SceneManagement;
//
// namespace SocialBeeAR
// {
//     public class Intro : MonoBehaviour
//     {
//         public GameObject BEEBot;
//         public BezierPathManager[] Paths;
//
//         public InputField NameInputIF;
//
//         public GameObject WhatIsYourNamePanel;
//         public GameObject WelcomePanel;
//         public GameObject PlusBtnHighlightPanel;
//         public GameObject PlusBtn;
//         public GameObject LetsStartActivityPanel;
//
//         public GameObject TapToPlaceActivityTopPanel;
//         public GameObject TapToPlaceActivityPanel;
//         public GameObject CrosshairBtn;
//
//         splineMove BEEBotSplineMove;
//
//         Transform[] LookAtGOs = new Transform[2];
//         LookAt lookAt;
//
//         int TempTextNumber = 1;
//
//         private NativeCall nativeCall;
//
//         #region Texts
//
//         string str01 = "Hi, my name is Bee Bot.";
//         string str02 = "What is your name?";
//
//         string str11 = "Welcome to Social Bee!";
//         string str12 = "";
//         string str13 = "The ultimate experience platform.";
//
//         string str21 = "In Social Bee, experiences consist of activities. Let's start by placing an activity.";
//
//         string str31 = "Tap anywhere on the surface to place an activity.";
//
//         #endregion
//
//         void Start()
//         {
//             BEEBotSplineMove = BEEBot.GetComponent<splineMove>();
//             BEEBotSplineMove.speed = 4;
//
//             LookAtGOs[0] = GameObject.FindObjectOfType<Light>().transform;
//             lookAt = GameObject.FindObjectOfType<LookAt>();
//             lookAt.lookAt = LookAtGOs[0];
//
//             //StartCoroutine(ToggleLookAtGOCO());
//             nativeCall = NativeCall.Instance;
//         }
//
//         IEnumerator ToggleLookAtGOCO()
//         {
//             yield return new WaitForSeconds(UnityEngine.Random.Range(3, 6));
//
//             if (LookAtGOs[1] != null && lookAt.lookAt == LookAtGOs[0] && LookAtGOs[1].gameObject.activeInHierarchy)
//             {
//                 lookAt.lookAt = LookAtGOs[1];
//             }
//             else
//             {
//                 lookAt.lookAt = LookAtGOs[0];
//             }
//
//             StartCoroutine(ToggleLookAtGOCO());
//         }
//
//         public void UpdateName()
//         {
//             lookAt.Offset = 3;
//             lookAt.lookAt = LookAtGOs[0];
//             str12 = NameInputIF.text;
//
//             StartCoroutine(UpdateNameCO());
//         }
//         IEnumerator UpdateNameCO()
//         {
//             if (NameInputIF.text != "")
//             {
//                 //WhatIsYourNamePanel.SetActive(false);
//                 //NameInputIF.gameObject.SetActive(false);
//                 WhatIsYourNamePanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
//                 NameInputIF.GetComponent<Animator>().SetBool("PanelZoomOut", true);
//
//                 yield return new WaitForSeconds(1f);
//
//                 WelcomePanel.SetActive(true);
//                 yield return new WaitForSeconds(1f);
//
//                 //LookAtGOs[1] = WelcomePanel.transform.GetChild(1);
//
//                 yield return new WaitUntil(() => TempTextNumber == 3);
//                 StartCoroutine(TypeCharacters(WelcomePanel.transform.GetChild(0).GetComponent<Text>(), str11, ShowWelcomeText));
//             }
//         }
//
//         public void Btn_PlaceActivity()
//         {
//             StartCoroutine(Btn_PlaceActivityCO());
//         }
//         IEnumerator Btn_PlaceActivityCO()
//         {
//             LetsStartActivityPanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
//             PlusBtn.SetActive(false);
//             PlusBtnHighlightPanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
//
//             yield return new WaitForSeconds(1f);
//
//             TapToPlaceActivityTopPanel.SetActive(true);
//             TapToPlaceActivityPanel.SetActive(true);
//             CrosshairBtn.SetActive(true);
//
//             //LookAtGOs[1] = TapToPlaceActivityPanel.transform.GetChild(0);
//
//             yield return new WaitForSeconds(1.25f);
//             StartCoroutine(TypeCharacters(TapToPlaceActivityPanel.transform.GetChild(0).GetComponent<Text>(), str31, () => {
//                 // ToDo: this is a temporary code only so we can automatically close the tutorial.
//                 _ = new WaitForSeconds(3f);
//
//                 // Let's not load the AR main module yet and let's just end the AR session.
//                 nativeCall.EndAR();
//
//                 //ENABLE THIS AFTER DEBUGGING -->
//                 StartCoroutine(SwitchScene(() =>
//                 {
//                     print("FROM AR: MainScene loaded, setting as active scene...");
//                     // Set the SceneLoader as the active scene so we can unload the OnBoardingScene.
//                     SceneLoader.Instance.loadedScenes.Add(Const.MainScene);
//                     SceneManager.SetActiveScene(SceneManager.GetSceneByName(Const.MainScene));
//                     if (SceneLoader.Instance.loadedScenes.Contains(Const.OnBoardingScene))
//                     {
//                         print("FROM AR: Unloading OnBoardingScene...");
//                         SceneManager.UnloadSceneAsync(Const.OnBoardingScene);
//                         SceneLoader.Instance.loadedScenes.Remove(Const.OnBoardingScene);
//                     }
//                 }));
//                 //StartCoroutine(SwitchTest());
//             }));
//         }
//
//         IEnumerator SwitchScene(Action completeSwitchingScene)
//         {
//             // Option #1: Delay loading the MainScene.
//             //      This causes too much trouble as loading the scene takes time,
//             //      and causes a race condition. To resolve it, we need to allow the async to finish.
//             //      Then we need a callback handler in the native app to know that loading is done.
//             //      Lastly, we need to continue the loading process for the MainScene. 
//             //print("FROM AR: SceneLoader loading...");
//             //var loading = SceneManager.LoadSceneAsync(Const.SceneLoader, LoadSceneMode.Additive);
//
//             // Option #2: Load the MainScene now.
//             //      This is a (more) viable solition. We will eagerly load the MainScene.
//             //      This works, however, we might check all the methods that run initially.
//             //      We need to make sure that we are not consuming a lot of memory resource
//             //      due to the eagerly loading of the MainScene.
//             print("FROM AR: MainScene loading...");
//             var loading = SceneManager.LoadSceneAsync(Const.MainScene, LoadSceneMode.Single);
//             loading.allowSceneActivation = false;
//
//             while (!loading.isDone)
//             {
//                 print($"progress = {loading.progress}");
//                 if (loading.progress == 0.9f)
//                 {
//                     print("activating scene");
//                     loading.allowSceneActivation = true;
//                     completeSwitchingScene();
//                 }
//                 //yield return new WaitForEndOfFrame();
//                 yield return null;
//             }
//
//             print("FROM AR: SwitchScene completed.");                  
//         }
//
//         public void IF_ValueChanged()
//         {
//             if (NameInputIF.text != "")
//             {
//                 LookAtGOs[1] = NameInputIF.transform;
//                 lookAt.lookAt = LookAtGOs[1];
//             }
//             else
//             {
//                 lookAt.lookAt = LookAtGOs[0];
//             }
//         }
//
//         // SPLINE - Animations
//
//         public void FirstAnimationDone()
//         {
//             StartCoroutine(FirstAnimationDoneCO());
//         }
//         IEnumerator FirstAnimationDoneCO()
//         {
//             yield return new WaitForSeconds(0f);
//             WhatIsYourNamePanel.SetActive(true);
//             yield return new WaitForSeconds(1f);
//
//             //LookAtGOs[1] = WhatIsYourNamePanel.transform.GetChild(1);
//             lookAt.Offset = 3;
//
//             StartCoroutine(TypeCharacters(WhatIsYourNamePanel.transform.GetChild(0).GetComponent<Text>(), str01, AcceptUsername));
//         }
//
//         public void SecondAnimationDone()
//         {
//             StartCoroutine(SecondAnimationDoneCO());
//         }
//         IEnumerator SecondAnimationDoneCO()
//         {
//             if (BEEBotSplineMove.pathContainer == Paths[1])
//             {
//                 //WelcomePanel.SetActive(false);
//                 WelcomePanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
//                 yield return new WaitForSeconds(0.5f);
//                 LetsStartActivityPanel.SetActive(true);
//
//                 //LookAtGOs[1] = LetsStartActivityPanel.transform.GetChild(0);
//                 lookAt.Offset = 8.5f;
//
//                 yield return new WaitUntil(() => TempTextNumber == 6);
//                 //StartCoroutine(TypeCharacters(LetsStartActivityPanel.transform.GetChild(0).GetComponent<Text>(), str21));
//
//                 //yield return new WaitUntil(() => TempTextNumber == 7);
//                 //yield return new WaitForSeconds(1f);
//                 //PlusBtn.SetActive(true);
//                 //yield return new WaitForSeconds(1f);
//                 //PlusBtnHighlightPanel.SetActive(true);
//
//                 StartCoroutine(TypeCharacters(LetsStartActivityPanel.transform.GetChild(0).GetComponent<Text>(), str21, () =>
//                 {
//                     _ = new WaitForSeconds(1f);
//                     PlusBtn.SetActive(true);
//                     _ = new WaitForSeconds(1f);
//                     PlusBtnHighlightPanel.SetActive(true);
//                 }));
//             }
//         }
//
//         IEnumerator TypeCharacters(Text Txt, string str, Action continueAfterTyping = null)
//         {
//             LookAtGOs[1] = Txt.transform;
//             lookAt.lookAt = LookAtGOs[1];
//
//             print("X: " + TempTextNumber);
//             char[] charArr = str.ToCharArray();
//
//             foreach (char tempChar in charArr)
//             {
//                 yield return new WaitForSeconds(0.04f);
//                 Txt.text += tempChar;
//             }
//
//             yield return new WaitForSeconds(0.75f);
//             TempTextNumber++;
//
//             lookAt.lookAt = LookAtGOs[0];
//
//             continueAfterTyping?.Invoke();
//         }
//
//         #region Text Animation and Interaction
//
//         void AcceptUsername()
//         {
//             StartCoroutine(TypeCharacters(WhatIsYourNamePanel.transform.GetChild(1).GetComponent<Text>(), str02, () =>
//             {
//                 NameInputIF.gameObject.SetActive(true);
//                 lookAt.Offset = 5;
//             }));
//         }
//
//         void ShowWelcomeText()
//         {
//             StartCoroutine(TypeCharacters(WelcomePanel.transform.GetChild(1).GetComponent<Text>(), str12, () =>
//             {
//                 StartCoroutine(TypeCharacters(WelcomePanel.transform.GetChild(2).GetComponent<Text>(), str13, () =>
//                 {
//                     LookAtGOs[1] = null;
//                     _ = new WaitForSeconds(1f);
//
//                     BEEBotSplineMove.SetPath(Paths[1]);
//                     BEEBotSplineMove.ChangeSpeed(1.5f);
//                 }));
//             }));
//         }
//
//         #endregion
//
//
//         #region Test code only
//
//         IEnumerator SwitchTest()
//         {             
//             print("FROM AR: TestScene loading...");
//             var loading = SceneManager.LoadSceneAsync(Const.TestScene, LoadSceneMode.Single);
//
//             yield return loading;
//             print("FROM AR: TestScene loaded, setting as active scene...");            
//             SceneLoader.Instance.loadedScenes.Add(Const.TestScene);
//             SceneManager.SetActiveScene(SceneManager.GetSceneByName(Const.SceneLoader));
//             if (SceneLoader.Instance.loadedScenes.Contains(Const.OnBoardingScene))
//             {
//                 print("FROM AR: Unloading OnBoardingScene...");
//                 SceneManager.UnloadSceneAsync(Const.OnBoardingScene);
//                 SceneLoader.Instance.loadedScenes.Remove(Const.OnBoardingScene);
//             }
//         }
//
//         #endregion
//     }
// }
