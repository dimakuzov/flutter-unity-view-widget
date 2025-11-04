using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SocialBeeAR
{
    public class SceneLoader : BaseSingletonClass<SceneLoader>
    {
        public IList<string> loadedScenes = new List<string>();

        NativeCall nativeCall;
        public NativeCall NativeCall
        {
            get
            {
                if (nativeCall == null)
                    nativeCall = NativeCall.Instance;

                return nativeCall;
            }
        }


        void Start()
        {           
        }
        
        public void LoadMainScene()
        {
            print("FROM AR: LoadMainScene, entered");
            //var activeScene = SceneManager.GetActiveScene();
            if (!IsSceneLoaded(Const.MainScene))
            {
                print("FROM AR: loading MainScene...");               
                StartCoroutine(DoLoadMainScene(() => {
                    // Check again to make sure we aren't loading the MainScene more than once.
                    // We need to do this because of the nature of how this callback is called.
                    if (IsSceneLoaded(Const.MainScene))
                    {
                        print("FROM AR: LoadMainScene > MainScene is already loaded.");
                        return;
                    }
                    print("FROM AR: LoadMainScene > MainScene loaded, setting as active scene...");
                    // Set the SceneLoader as the active scene so we can unload the OnBoardingScene.
                    Instance.loadedScenes.Add(Const.MainScene);
                    NativeCall.OnSceneLoaded("");
                    try
                    {
                        SceneManager.SetActiveScene(SceneManager.GetSceneByName(Const.MainScene));
                    }
                    catch
                    {

                    }
                    if (Instance.loadedScenes.Contains(Const.OnBoardingScene))
                    {
                        print("FROM AR: LoadMainScene > Unloading OnBoardingScene...");
                        SceneManager.UnloadSceneAsync(Const.OnBoardingScene);
                        Instance.loadedScenes.Remove(Const.OnBoardingScene);
                    }
                }));
            }            
            print("FROM AR: LoadMainScene, exiting...");
        }

        bool IsSceneLoaded(string name)
        {
            var activeScene = SceneManager.GetActiveScene();
            var isLoaded = Instance.loadedScenes.Contains(name) || activeScene != null && activeScene.name == name;
            print($"IsSceneLoaded '{name}' = {isLoaded}");
            return isLoaded;
        }

        IEnumerator DoLoadMainScene(Action completeLoadingMainScene)
        {                                    
            print("FROM AR: DoLoadMainScene > MainScene loading...");
            var loading = SceneManager.LoadSceneAsync(Const.MainScene, LoadSceneMode.Single);
            loading.allowSceneActivation = false;

            while (!loading.isDone)
            {
                print($"progress = {loading.progress}");
                if (loading.progress == 0.9f)
                {
                    print("activating scene");
                    loading.allowSceneActivation = true;
                    completeLoadingMainScene();
                }
                //yield return new WaitForEndOfFrame();
                yield return null;
            }

            print("FROM AR: DoLoadMainScene completed.");                
        }

        public void LoadOnBoardingScene()
        {
            print("FROM AR: LoadOnBoardingScene, entered");
            if (!Instance.loadedScenes.Contains(Const.OnBoardingScene))                
            {
                print("FROM AR: loading OnBoardingScene...");
                Instance.loadedScenes.Add(Const.OnBoardingScene);
                SceneManager.LoadSceneAsync(Const.OnBoardingScene);
            }
            print("FROM AR: LoadOnBoardingScene, exiting...");
        }


        public void LoadTestScene()
        {
            print("FROM AR: LoadTestScene, entered");
            var activeScene = SceneManager.GetActiveScene();
            if (!IsSceneLoaded(Const.TestScene))
            {
                print("FROM AR: loading LoadTestScene...");              
                StartCoroutine(DoLoadTestScene());
            }
            print("FROM AR: LoadTestScene, exiting...");
        }


        IEnumerator DoLoadTestScene()
        {
            print("FROM AR: DoLoadTestScene > TestScene loading...");
            var loading = SceneManager.LoadSceneAsync(Const.TestScene, LoadSceneMode.Single);

            yield return loading;
            print("FROM AR: DoLoadTestScene > TestScene loaded, setting as active scene...");
            
            Instance.loadedScenes.Add(Const.TestScene);
            NativeCall.OnSceneLoaded("");
            try
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(Const.SceneLoader));
            }
            catch
            {

            }
            if (Instance.loadedScenes.Contains(Const.OnBoardingScene))
            {
                print("FROM AR: DoLoadTestScene > Unloading OnBoardingScene...");
                SceneManager.UnloadSceneAsync(Const.OnBoardingScene);
                Instance.loadedScenes.Remove(Const.OnBoardingScene);
            }
        }
    }
}
