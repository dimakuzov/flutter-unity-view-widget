/* 
*   NatCorder
*   Copyright (c) 2020 Yusuf Olokoba
*/
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

namespace NatCorder.Examples {

    using UnityEngine;
    using System.Collections;
    using Clocks;
    using Inputs;
    
    public class ReplayCam : MonoBehaviour {

        public AudioVisualization audioVisualization;
        public AudioSource audioSourcesForMergeAudio;
        public Camera audioCamera;
        // public AudioSource audioListener;

        
        [Header("Recording")]
        public int videoWidth = 1280;
        public int videoHeight = 720;
        public bool recordMicrophone;

        [HideInInspector] public Texture2D photoTexture2D;
        [HideInInspector] public Texture2D videoTexture2D;
        public GameObject showingPlane;
        
        private IMediaRecorder recorder;
        private IMediaRecorder audioRecorder;
        private CameraInput cameraInput;
        private AudioInput audioInput;
        [HideInInspector] public AudioSource microphoneSource;
        private VideoPlayer vp;

        [Header("Filter's values")] public VideoPlayer[] recordVideoPlayers;
        public VideoPlayer recordVideoPlayer;
        public GameObject filterPlane;
        public Camera filterCamera;
        public List<Material> filterMats = new List<Material>();
        [HideInInspector] public string filteredFilePath;
        private int numberOfSelectedIcon;
        private bool iconsDraggedSelectedIcon;
        private float daleyTimeForIconButton;


        public VideoClip[] videoClips;
        private int clipNumber;
        
        private string videoPath;
        private int filerMatInList;
        private bool isPhotoTaking;
        
        RealtimeClock clock;
        
        
        public GameObject outputPlane;//---Temp

        public GameObject arPanel;
        private IEnumerator Start () {
            // vp = GetComponent<VideoPlayer>();
            // vp.time = 0;
            // vp.Play();
            // Texture tex = vp.texture;
            // Texture2D tex2d = Texture2D.CreateExternalTexture(tex.width, tex.height, TextureFormat.RGB24,
            //     false, false, tex.GetNativeTexturePtr());
            // Debug.Log($"-=- Texture2D tex2d");
            // showingPlane.GetComponent<Renderer>().material.mainTexture = tex2d;
            // vp.Stop();
            
            // vp.time = 0;
            // vp.Play();
            // int width = vp.texture.width;
            // int height = vp.texture.height;
            // Texture2D preview = new Texture2D(width, height, TextureFormat.RGB24, false);
            // RenderTexture.active = vp.targetTexture;
            // preview.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            // preview.Apply();
            // // Sprite sprite = Sprite.Create(preview, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            // vp.Pause();
            // RenderTexture.active = null;
            
            
            
            // // Start microphone
            videoWidth = Screen.width;
            videoHeight = Screen.height;
            SetCroppingPositionOnARPanel(arPanel);
            microphoneSource = gameObject.AddComponent<AudioSource>();
            microphoneSource.mute = false;
            microphoneSource.loop = true;
            microphoneSource.bypassEffects =
            microphoneSource.bypassListenerEffects = false;
            microphoneSource.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
            yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
            microphoneSource.Play();
            microphoneSource.volume = 0.2f;
            vp = GetComponent<VideoPlayer>();
            audioVisualization.audioSource = microphoneSource;
            // CreateIconsLine();
            // StartPlayMergeredClip();
            // TestScalePanelByWidth(filterPlane);
        }

        public GameObject photoPlaneForRecord;
        public VideoPlayer vp1;
        private VideoPlayer fullscreenPlayer;
        public void Fullscreen() {
            photoPlaneForRecord.SetActive(true);
                // fullscreenPlayer = vp;
                // чего-то не работет замена видеоплеера
                vp1.targetMaterialRenderer = photoPlaneForRecord.GetComponent<MeshRenderer>();
                vp1.targetMaterialProperty = "_MainTex";
                // vp.targetMaterialRenderer.GetComponent<Renderer>().material = filterMats[0];
                // fullscreenPlayer = photoPlaneForRecord.GetComponent<VideoPlayer>();
                // fullscreenPlayer.clip = vp.clip;
                // fullscreenPlayer.Play();
                vp1.Play();
                fullscreenPlayer = vp1;
        }

        void ExitFullScreen() {
            vp1.targetMaterialRenderer = null;
            vp1.targetMaterialProperty = "_Albedo";
            vp1.Play();
            
        }
        
        void TestScalePanelByWidth(GameObject panel) {
            float h = videoHeight;
            float w = videoWidth;
            float rootW = panel.transform.localScale.x;
            float rootZ = panel.transform.localScale.z;
            float k = h / w;

            panel.transform.localScale = new Vector3(rootW, rootW * k, rootZ);
        }

        public GameObject selectedSquare;
        void TestChangeSizeOfSelectedArea() {
            RectTransform focalTr = selectedSquare.GetComponent<RectTransform>();
            int width = Screen.width;
            int height = Screen.height;
            Vector2 size = new Vector2(0, (1125 - 2436));
            Debug.Log($"size = {size}");
            focalTr.sizeDelta = size;
            // focalTr = width;
        }
        
        private void Update() {
            // vp.Pause();
            if (Input.GetKeyDown(KeyCode.R)) {
                TestChangeSizeOfSelectedArea();
                // Fullscreen();
            }
            if (Input.GetKeyDown(KeyCode.Q)) {
                // ExitFullScreen();
            }
            // // if(Input.touchCount > 0) { // touchpad
            // if(Input.GetMouseButton(0)) {   
            //     // SelectingIcon();
            //     daleyTimeForIconButton += Time.deltaTime;
            //     MoveFilters(Input.mousePosition.x);
            // }
            //
            // if (Input.GetMouseButtonUp(0)) {
            //     previousXPos = 0;
            //     daleyTimeForIconButton = 0;
            //     StartCoroutine(MoveToSelectedPos());
            // }
        }

        private void OnEnable() {
            audioPlayer = audioCamera.GetComponent<VideoPlayer>();
            audioPlayer.SetTargetAudioSource(0, audioSourcesForMergeAudio);
            audioPlayer.loopPointReached += ChangeClip;
            audioPlayer.prepareCompleted += StartFirstAudioClipForMerge;
            
            ChangeClip(audioPlayer);            


            // recordVideoPlayers = filterPlane.GetComponents<VideoPlayer>();
            // recordVideoPlayers[0].prepareCompleted += PauseClip;
            // recordVideoPlayers[1].prepareCompleted += PauseClip;
            // recordVideoPlayers[0].loopPointReached += PlayNewClip;
            // recordVideoPlayers[1].loopPointReached += PlayNewClip;

            // recordVideoPlayer.loopPointReached += PlayNewClip;
            // recordVideoPlayer.prepareCompleted += StartMergeClip;

            // recordVideoPlayer.loopPointReached += StopFilterRecord;
            // recordVideoPlayer.prepareCompleted += StartVideoWithFilter;
        }

        private void OnDisable() {
            // recordVideoPlayers[0].prepareCompleted -= PauseClip;
            // recordVideoPlayers[1].prepareCompleted -= PauseClip;
            // recordVideoPlayers[0].loopPointReached -= PlayNewClip;
            // recordVideoPlayers[1].loopPointReached -= PlayNewClip;
            
            // recordVideoPlayer.loopPointReached -= PlayNewClip;
            // recordVideoPlayer.prepareCompleted -= StartMergeClip;

            // recordVideoPlayer.loopPointReached -= StopFilterRecord;
            // recordVideoPlayer.prepareCompleted -= StartVideoWithFilter;
        }

        //-=--=-=-=-=-=-=--=-=---=-=-=-=-=-=-=-=-
        
        
        
        
        [SerializeField] private VideoClip[] audioClips;
        private int currClip;
        private VideoPlayer audioPlayer;
        
        void StartMergeAudioClips() {
            Debug.Log($"StartMergeAudioClips()");
            var sampleRate = AudioSettings.outputSampleRate;
            var channelCount = (int)AudioSettings.speakerMode;
            var clock = new RealtimeClock();
            // clock = new RealtimeClock();
            var frameRate = 30;

            // audioSourcesForMergeAudio = audioPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource
            // audioPlayer.SetTargetAudioSource(0, audioSourcesForMergeAudio);
            
            audioRecorder = new MP4Recorder(2, 2, frameRate, sampleRate, channelCount);
            // Create recording inputs
            cameraInput = new CameraInput(audioRecorder, clock, Camera.main);
            audioInput = new AudioInput(audioRecorder, clock, audioSourcesForMergeAudio, true);
            // audioInput = new AudioInput(audioRecorder, clock, microphoneSource, true);
            microphoneSource.mute = audioInput == null;
        }
        
        async void StopMergeAudioClips() {
            microphoneSource.mute = true;
            audioInput?.Dispose();
            cameraInput.Dispose();
            var path = await audioRecorder.FinishWriting();
            Debug.Log($"Saved audio recording to: {path}");
        }
        
        void ChangeClip(VideoPlayer calledVP) {
            if (currClip == audioClips.Length) {
                StopMergeAudioClips();
                calledVP.Stop();
            }
            else {
                // if(currClip != 0) {
                //     PauseRecording();
                // }
                calledVP.clip = audioClips[currClip];
                calledVP.Play();
                currClip++;
            }
        }

        void StartFirstAudioClipForMerge(VideoPlayer calledVP) {
            if (currClip == 1) {
                StartMergeAudioClips();
            }
            // else {
            //     ResumeRecording();
            // }
        }
        
        
        
        // void PauseRecording() {
        //     Debug.Log($"-=- PauseRecording() Time.time = {Time.time}");
        //     // First, we pause the recording clock
        //     // This will essentially 'freeze' time, reflected in the clock's timestamps
        //     clock.Paused = true;
        //     // Then we dispose the camera input
        //     // We do this because we don't want to record anything while recording is paused
        //     cameraInput.Dispose();
        //     // audioInput?.Dispose();
        // }
        //
        // public void ResumeRecording() {
        //     Debug.Log($"-=- ResumeRecording() Time.time = {Time.time}");
        //     // Now we resume the recording clock
        //     // The clock's timestamps will now continue from when we paused
        //     // It won't matter how long the clock was paused, which is what we want!
        //     clock.Paused = false;
        //     // And of course, we continue recording video frames
        //     // NOTE: We must use the same recording clock when creating the new recorder
        //     cameraInput = new CameraInput(audioRecorder, clock, Camera.main);
        //     // audioInput = new AudioInput(audioRecorder, clock, audioSourcesForMergeAudio, true);
        // }
        
        
        
        
        //-=--=-=-=-=-=-=--=-=---=-=-=-=-=-=-=-=-
        
        
        private void OnDestroy () {
            // Stop microphone
            // microphoneSource.Stop();
            // Microphone.End(null);
        }

        #region AudioRecord

        public void StartAudioRecord() {
            
            Debug.Log($"StartAudioRecord()");
            var sampleRate = AudioSettings.outputSampleRate;
            var channelCount = (int)AudioSettings.speakerMode;
            var clock = new RealtimeClock();
            var frameRate = 30;
            // audioRecorder = new WAVRecorder (sampleRate, channelCount);
            // audioInput = new AudioInput(audioRecorder, clock, microphoneSource, true);
            audioRecorder = new MP4Recorder(2, 2, frameRate, sampleRate, channelCount);
            // Create recording inputs
            cameraInput = new CameraInput(audioRecorder, clock, Camera.main);
            audioInput = new AudioInput(audioRecorder, clock, microphoneSource, true);
            microphoneSource.mute = audioInput == null;
        }

        public async void StopAudioRecording() {
            // Mute microphone
            microphoneSource.mute = true;
            // Stop recording
            audioInput?.Dispose();
            cameraInput.Dispose();
            var path = await audioRecorder.FinishWriting();
            Debug.Log($"Saved audio recording to: {path}");
            
            vp.url = path;
            vp.Play();
            // StartCoroutine(GetAudioClip(path));

            // AudioClip track = Resources.Load<AudioClip>(path);
            // AudioSource audio = GetComponent<AudioSource>();
            // audio.clip = track;
            // audio.PlayOneShot(track);
        }

        // IEnumerator GetAudioClip(string path) {
        //     using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
        //         "file://" + "/Users/Kuz/Upwork/Bee/SocialBeeAR/recording_2020_09_19_13_34_42_768.wav", AudioType.WAV)) {
        //         yield return www.SendWebRequest();
        //
        //         if (!String.IsNullOrWhiteSpace(www.error)) {
        //             Debug.Log(www.error);
        //         }
        //         else {
        //             Play(DownloadHandlerAudioClip.GetContent(www));
        //         }
        //     }
        // }
        //
        // void Play(AudioClip clip) {
        //     Debug.Log($"Play(AudioClip clip)");
        //     audioSourseForPlay.clip = clip;
        //     audioSourseForPlay.Play();
        //     
        //     // AudioSource audio = GetComponent<AudioSource>();
        //     // audio.clip = track;
        //     // audio.PlayOneShot(track);
        // }
        
        
        // --------------
        
        /*
        // convert two bytes to one float in the range -1 to 1
         static float bytesToFloat(byte firstByte, byte secondByte) {
             // convert two bytes to one short (little endian)
             short s = (short)((secondByte << 8) | firstByte);
             // convert to range from -1 to (just below) 1
             return s / 32768.0F;
         }
 
         static int bytesToInt(byte[] bytes,int offset=0){
             int value=0;
             for(int i=0;i<4;i++){
                 value |= ((int)bytes[offset+i])<<(i*8);
             }
             return value;
         }
 
         private static byte[] GetBytes(string filename){
             return File.ReadAllBytes(filename);
         }
         // properties
         public float[] LeftChannel{get; internal set;}
         public float[] RightChannel{get; internal set;}
         public int ChannelCount {get;internal set;}
         public int SampleCount {get;internal set;}
         public int Frequency {get;internal set;}
         
         // Returns left and right double arrays. 'right' will be null if sound is mono.
         public WAV(string filename):
             this(GetBytes(filename)) {}
 
         public WAV(byte[] wav){
             
             // Determine if mono or stereo
             ChannelCount = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels
 
             // Get the frequency
             Frequency = bytesToInt(wav,24);
             
             // Get past all the other sub chunks to get to the data subchunk:
             int pos = 12;   // First Subchunk ID from 12 to 16
             
             // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
             while(!(wav[pos]==100 && wav[pos+1]==97 && wav[pos+2]==116 && wav[pos+3]==97)) {
                 pos += 4;
                 int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                 pos += 4 + chunkSize;
             }
             pos += 8;
             
             // Pos is now positioned to start of actual sound data.
             SampleCount = (wav.Length - pos)/2;     // 2 bytes per sample (16 bit sound mono)
             if (ChannelCount == 2) SampleCount /= 2;        // 4 bytes per sample (16 bit stereo)
             
             // Allocate memory (right will be null if only mono sound)
             LeftChannel = new float[SampleCount];
             if (ChannelCount == 2) RightChannel = new float[SampleCount];
             else RightChannel = null;
             
             // Write to double array/s:
             int i=0;
             while (pos < wav.Length) {
                 LeftChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
                 pos += 2;
                 if (ChannelCount == 2) {
                     RightChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
                     pos += 2;
                 }
                 i++;
             }
         }
 
         public override string ToString ()
         {
             return string.Format ("[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, Frequency={4}]", LeftChannel, RightChannel, ChannelCount, SampleCount, Frequency);
         }
        */
        
        

        #endregion
        
        
        #region Record

        
        //----------------- Make Video -----------------
        public void StartRecording () {
            isPhotoTaking = false;
            videoWidth = Screen.width;
            videoHeight = Screen.height;
            // Start recording
            var frameRate = 30;
            var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
            var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
            var clock = new RealtimeClock();
            recorder = new MP4Recorder(videoWidth, videoHeight, frameRate, sampleRate, channelCount);
            // Create recording inputs
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            audioInput = recordMicrophone ? new AudioInput(recorder, clock, microphoneSource, true) : null;
            // Unmute microphone
            microphoneSource.mute = audioInput == null;
        }

        public async void StopRecording () {
            // Mute microphone
            microphoneSource.mute = true;
            // Stop recording
            audioInput?.Dispose();
            cameraInput.Dispose();
            var path = await recorder.FinishWriting();
            Debug.Log($"Saved recording to: {path}");
            
            //----------------- Show Video -----------------
            vp.url = path;
            vp.Play();
            videoPath = path;
            
            showingPlane.SetActive(true);
            ScalePanelByWidth(showingPlane);
            // ShowImageWithFilter();
        }
        
        
        //----------------- Make Photo -----------------
        public void GetPhoto() {
            isPhotoTaking = true;
            Debug.Log("--- GetPhoto()");
            videoWidth = Screen.width;
            videoHeight = Screen.height;
            recorder = new JPGRecorder(videoWidth, videoHeight);
            var clock = new RealtimeClock();
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            StartCoroutine(StopPhotoSeq());
        }

        IEnumerator StopPhotoSeq() {
            yield return  new WaitForSeconds(0.15f);
            StopPhotoAsynx();
            yield return null;
        }

        async void StopPhotoAsynx() {
            cameraInput.Dispose();
            var path = await recorder.FinishWriting();
            
            //----Show Photo
            var bytes = System.IO.File.ReadAllBytes(path + "/1.jpg");
            var tex = new Texture2D(1, 1);
            tex.LoadImage(bytes);
            photoTexture2D = tex;
            
            showingPlane.SetActive(true);
            ScalePanelByWidth(showingPlane);
            // ShowImageWithFilter();
            
            // --- remove other .JPGs
            string[] files = Directory.GetFiles(path);
            foreach (var file in files) {
                if(file != path + "/1.jpg") {
                    // Debug.Log("file: " + file);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
        }
        
        
        //----------------- Scaling -----------------
        void ScalePanelByWidth(GameObject panel) {
            float h = videoHeight;
            float w = videoWidth;
            float rootW = panel.transform.localScale.x;
            float rootZ = panel.transform.localScale.z;
            float k = h / w;
            panel.transform.localScale = new Vector3(rootW, rootW * k, rootZ);
        }
        
        #endregion
        
        
        /*
        #region Filter Applying

        
        //----------------- Public Functions -----------------

        // --- call by 'Next' button or press filter button
        public void ChangeFilter(bool next) {
            if (next) {
                if(filerMatInList != filterMats.Count - 1) {
                    filerMatInList++;
                }
            }
            else {
                if(filerMatInList != 0) {
                    filerMatInList--;
                }
            }
            ShowImageWithFilter();
        }

        // --- Show Photo or Video With Filter on panel or something else
        void ShowImageWithFilter() {
            Renderer rend = showingPlane.GetComponent<Renderer>();
            rend.material = filterMats[filerMatInList];
            if (isPhotoTaking) {
                rend.material.mainTexture = photoTexture2D;
            }
        }

        // --- call after press 'Next' or 'Filter' button
        public void SaveImageWithFilter() {
            if (isPhotoTaking) {
                SavePhotoFilter();
            }
            else {
                SaveVidoeFilter();
            }
        }
        
        void SavePhotoFilter() {
            ScalePanelByWidth(filterPlane);
            Renderer rend = filterPlane.GetComponent<Renderer>();
            rend.material = filterMats[filerMatInList];
            rend.material.mainTexture = photoTexture2D;
            SavePhotoWithFilter();
        }
        
        void SaveVidoeFilter() {
            ScalePanelByWidth(filterPlane);
            Renderer rend = filterPlane.GetComponent<Renderer>();
            rend.material = filterMats[filerMatInList];
            recordVideoPlayer.url = videoPath;
            recordVideoPlayer.Play();
        }
        
        
        //----------------- Apply Video Filter -----------------
        void StartVideoWithFilter (VideoPlayer vp) {
            Debug.Log("-=- SaveVideoWithFilter()");
            videoWidth = Screen.width;
            videoHeight = Screen.height;
            // Start recording
            var frameRate = 30;
            var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
            var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
            var clock = new RealtimeClock();
            recorder = new MP4Recorder(videoWidth, videoHeight, frameRate, sampleRate, channelCount);
            // Create recording inputs
            cameraInput = new CameraInput(recorder, clock, filterCamera);
            audioInput = recordMicrophone ? new AudioInput(recorder, clock, microphoneSource, true) : null;
            // Unmute microphone
            microphoneSource.mute = audioInput == null;
        }

        void StopFilterRecord(UnityEngine.Video.VideoPlayer vp) {
            StopVideoWithFilter();
        }

        async void StopVideoWithFilter () {
            // Mute microphone
            microphoneSource.mute = true;
            // Stop recording
            audioInput?.Dispose();
            cameraInput.Dispose();
            var path = await recorder.FinishWriting();
            filteredFilePath = path;
            Debug.Log($"Saved recording to: {path}");
            
            // --- Show Video ---
            ScalePanelByWidth(showingPlane);
            vp.url = path;
            // Debug.Log($"-=- mergered clip frame count = {vp.clip.length}");

            vp.Play();
        }

        
        
        
        //----------------- Apply Photo Filter -----------------
        void SavePhotoWithFilter() {
            Debug.Log("-=- SavePhotoWithFilter()");
            videoWidth = Screen.width;
            videoHeight = Screen.height;
            recorder = new JPGRecorder(videoWidth, videoHeight);
            var clock = new RealtimeClock();
            cameraInput = new CameraInput(recorder, clock, filterCamera);
            StartCoroutine(StopPhotoSeqWithFilter());
        }

        IEnumerator StopPhotoSeqWithFilter() {
            yield return  new WaitForSeconds(0.15f);
            StopPhotoAsynxWithFilter();
            yield return null;
        }

        async void StopPhotoAsynxWithFilter() {
            cameraInput.Dispose();
            var path = await recorder.FinishWriting();
            filteredFilePath = path;
            
            // --- remove other .JPGs
            string[] files = Directory.GetFiles(path);
            foreach (var file in files) {
                if(file != path + "/1.jpg") {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
            
            // --- Show Photo
            var bytes = System.IO.File.ReadAllBytes(path + "/1.jpg");
            var tex = new Texture2D(1, 1);
            tex.LoadImage(bytes);
            
            ScalePanelByWidth(outputPlane);
            outputPlane.GetComponent<Renderer>().material.mainTexture = tex;
        }
        

        #endregion
        
        #region FilterUI
        

        List<GameObject> icons = new List<GameObject>();
        [SerializeField] GameObject filtersBar;
        [SerializeField] GameObject filterIconPrefab;

        private float iconDis;
        GameObject selectedIcon;
        
        // --- Temporary values
        [SerializeField] List<string> filterNames = new List<string>();

        
        // -- first action, create all icons
        void CreateIconsLine() {
            iconDis = filtersBar.GetComponent<RectTransform>().rect.width / 4;

            for (int i = 0; i < filterNames.Count; i++) {
                Vector3 pos = new Vector3(iconDis * i,0,0);
                GameObject icon = Instantiate(filterIconPrefab, Vector3.zero, Quaternion.identity, filtersBar.transform);
                icon.transform.localPosition = pos;
                icon.GetComponentInChildren<Text>().text = filterNames[i];

                int num = i;
                icon.GetComponent<Button>().onClick.AddListener(delegate { MoveToSelectedPos(icon);});
                icon.GetComponent<Button>().onClick.AddListener(delegate { SettingNumberOfSelectedIcon(num);});
                icons.Add(icon);
            }
            IncreasingAndSelectingIcon();
        }

        void MoveToSelectedPos(GameObject icon) {
            if (!iconsDraggedSelectedIcon && daleyTimeForIconButton < 0.2f) {
                selectedIcon = icon;
                StartCoroutine(MoveToSelectedPos());
            }
        }
        
        

        void SettingNumberOfSelectedIcon(int i) {
            Debug.Log($"-=- SettingNumberOfSelectedIcon numberOfSelectedIcon =  {numberOfSelectedIcon}");
            numberOfSelectedIcon = i;
        }
        private void IncreasingAndSelectingIcon() {
            if (icons.Count == 0) {
                return;
            }
            // --- scale icons
            float startShowIncreaseDis = 0.95f;
            for (int i = 0; i < icons.Count; i++) {
                float xPosition = icons[i].transform.localPosition.x;
                if (xPosition > -iconDis * startShowIncreaseDis && xPosition < iconDis * startShowIncreaseDis) {
                    GameObject iconImageObject = icons[i].GetComponentInChildren<Image>().gameObject;
                    float addedScale = (1 - Mathf.Abs(xPosition / iconDis) / startShowIncreaseDis) * 0.5f;
                    float scale = 1 + addedScale;
                    iconImageObject.transform.localScale = new Vector3(scale, scale, scale);

                    // --- apply filter
                    if (!selectedIcon && xPosition > -iconDis * 0.5f && xPosition < iconDis * 0.5f ||
                        selectedIcon.transform != icons[i].transform &&
                        xPosition > -iconDis * 0.5f && xPosition < iconDis * 0.5f){

                        SelectingIcon(selectedIcon, i);
                        selectedIcon = icons[i];
                    }
                }
            }
        }

        void SelectingIcon(GameObject selectedIcon, int numberOfSelectedIcon) {
            Debug.Log($"-=- numberOfSelectedIcon =  {numberOfSelectedIcon}");
            Debug.Log($"-=- Show New Filter: {filterNames[numberOfSelectedIcon]}");
        }

        private float previousXPos = 0;
        void MoveFilters(float xPos) {
            if (previousXPos == 0) {
                previousXPos = xPos;
                return;
            }
            if (previousXPos != xPos) {
                float moveDis = xPos - previousXPos;

                foreach (var icon in icons) {
                    Vector3 newPos = icon.GetComponent<RectTransform>().localPosition;
                    newPos += new Vector3(moveDis * 1.3f, 0, 0);
                    icon.GetComponent<RectTransform>().localPosition = newPos;
                }
                previousXPos = xPos;
                IncreasingAndSelectingIcon();
            }
        }

        // --- when user touch up, we will move selected icon to center.
        IEnumerator MoveToSelectedPos() {
            iconsDraggedSelectedIcon = true;

            RectTransform currRectTr = selectedIcon.GetComponent<RectTransform>();
            float reverseTime = 0.17f;
            float moveDis = -currRectTr.localPosition.x;
            int times = (int)(reverseTime / Time.deltaTime);
            moveDis = moveDis / times;

            for (int i = 0; i < times; i++) {
                foreach (var icon in icons) {
                    Vector3 newPos = icon.GetComponent<RectTransform>().localPosition;
                    newPos += new Vector3(moveDis, 0, 0);
                    icon.GetComponent<RectTransform>().localPosition = newPos;
                }
                IncreasingAndSelectingIcon();
                yield return null;
            }
            
            SelectingIcon(selectedIcon, numberOfSelectedIcon);
            
            moveDis = -selectedIcon.GetComponent<RectTransform>().localPosition.x;
            foreach (var icon in icons) {
                Vector3 newPos = icon.GetComponent<RectTransform>().localPosition;
                newPos += new Vector3(moveDis, 0, 0);
                icon.GetComponent<RectTransform>().localPosition = newPos;
            }
            IncreasingAndSelectingIcon();
            iconsDraggedSelectedIcon = false;
            yield return null;
        }
        

        #endregion

        #region MergeClips

        

        private int mergeringClipStart;
        public GameObject showingPlane1; 
        public GameObject showingPlane2;
        private float previousClipLength;
        private float nextClipDelay;

        void Play() {
            // showingPlane2.SetActive(true);
            recordVideoPlayers[1].Play();
        }

        void Pause() {
            recordVideoPlayers[1].Pause();
            // recordVideoPlayers[1].Stop();
            // showingPlane2.SetActive(false);
        }
        
        // ------ Start Action -------
        void StartPlayMergeredClip() {
            mergeringClipStart = 0;
            if (videoClips.Length > 1) {
                recordVideoPlayers[0].clip = videoClips[0];
                recordVideoPlayers[0].Play();
                previousClipLength = (float)videoClips[0].length;
                clipNumber++;
            }   
        }

        private bool lastSection;
        // ------- Called when clip finish  (Finish call) -------
        void PlayNewClip(UnityEngine.Video.VideoPlayer calledVP) {
            Debug.Log($"-=- PlayNewClip Time.time = {Time.time}");
            // showingPlane1.transform.localPosition = new Vector3(0,0,1.874f);
            // showingPlane2.transform.localPosition = new Vector3(0,0,1.874f);

            // --- Stop Showing, this is the end
            if (lastSection) {
                Debug.Log($"-=- Finish");
                return;
            }
            
            if (clipNumber == videoClips.Length) {
                clipNumber = 0;
                lastSection = true;
            }
            
            foreach (var videoPlayer in recordVideoPlayers) {
                // --- Next clip
                if (videoPlayer != calledVP) {
                    // videoPlayer.Stop();
                    previousClipLength = (float) videoPlayer.clip.length;
                    videoPlayer.targetMaterialRenderer.transform.localPosition = new Vector3(0,0,1.874f);
                }
            }

            foreach (var videoPlayer in recordVideoPlayers) {
                // --- After Next clip
                if (videoPlayer == calledVP) {
                    videoPlayer.clip = videoClips[clipNumber];
                    videoPlayer.Play();
                    videoPlayer.targetMaterialRenderer.transform.localPosition = new Vector3(0,0,10);
                    videoPlayer.targetMaterialRenderer.gameObject.SetActive(false);
                }
            }
            
            // --- Change quad to another clip
            // if (showingPlane1.activeSelf) {
            //     showingPlane1.transform.localPosition = new Vector3(0,0,10);
            //     showingPlane1.SetActive(false);
            //     showingPlane2.SetActive(true);
            // }
            // else {
            //     showingPlane2.transform.localPosition = new Vector3(0,0,10);
            //     showingPlane2.SetActive(false);
            //     showingPlane1.SetActive(true);
            // }

            clipNumber++;
        }

        
        // ------- Called when next clip ready to start (First call) -------
        void PauseClip(UnityEngine.Video.VideoPlayer calledVP) {
            Debug.Log($"-=- PauseClip Time.time = {Time.time}");

            // --- first video
            if (mergeringClipStart == 0) {
                mergeringClipStart++;
                // --- start second video
                recordVideoPlayers[1].clip = videoClips[1];
                recordVideoPlayers[1].Play();
                realStartPlayTime = Time.time;
                StartCoroutine(StartChangingNewClip(recordVideoPlayers[1], (float) videoClips[0].length));
                
                // todo here we can start record mergered video
                
                clipNumber++;       
            }
            // --- second video
            else if (mergeringClipStart == 1) {
                calledVP.Pause();
                mergeringClipStart++;
            }
            else {
                calledVP.Pause();
                // foreach (var videoPlayer in recordVideoPlayers) {
                //     if (videoPlayer != calledVP) {
                //         previousClipLength = (float)videoPlayer.clip.length;
                //     }
                // }
                
                foreach (var videoPlayer in recordVideoPlayers) {
                    if (videoPlayer == calledVP) {
                        StartCoroutine(StartChangingNewClip(videoPlayer, previousClipLength));
                    }
                }
            }
        }

        private float realStartPlayTime;
        // ------- It start play next video on hidden videoplayer -------
        IEnumerator StartChangingNewClip(VideoPlayer videoPlayer, float previousClipL) {
            // --- it is time when current clip has started to playing
            
            float time = previousClipL - 0.4f + realStartPlayTime;
            Debug.Log($"-=- IEnumerator time ==== {time} : {Time.time} ====, previousClipLength = {previousClipL.ToString("R")}");
            while (time > Time.time) {
                yield return null;
            }
            
            // --- it is time minus next clip's delay from when current clip will have to finish
            
            videoPlayer.targetMaterialRenderer.gameObject.SetActive(true);
            // videoPlayer.targetMaterialRenderer.transform.localPosition = new Vector3(0,0,10);
            Debug.Log($"-=- After IEnumerator Time.time = {Time.time}");
            realStartPlayTime = Time.time;
            videoPlayer.Play();

            yield return null;
        }


        #endregion
        */
        public Material photoPlaneMat; 
        public Texture2D texture; 
        
        void SetCroppingPositionOnARPanel(GameObject panel) {

            Material newMat = new Material(Shader.Find("Mask/MaskedForARPlane"));
            newMat.CopyPropertiesFromMaterial(photoPlaneMat);
            newMat.SetTexture("_Albedo", texture);
            // newMat.mainTexture = texture;
            if (texture != null)
                panel.GetComponent<Renderer>().material = newMat;
            
            
            
            
            
            Vector3 croppingData = new Vector3(0.25f,0.25f,2f);
            if (croppingData == Vector3.zero) {
                croppingData = new Vector3(0.5f,0.5f, 1);
            }
            // --- at first set the scale of panel
            RectTransform panelTr = panel.GetComponent<RectTransform>();
            Debug.Log($"-=- panelTr.localPosition = {panelTr.localPosition}");
            
            // --- init scale (like without cropping)
            float h = (float)videoHeight / (float)videoWidth;
            panelTr.localScale = new Vector3(486, h * 486, 1);
            Debug.Log($"-=- panelTr.localScale = {panelTr.localScale}");
            
            // --- apply scale
            panelTr.localScale = new Vector3(panelTr.localScale.x * croppingData.z, panelTr.localScale.y * croppingData.z, 1);
            
            // --- calculate and apply position of future center
            float x = panelTr.localScale.x * croppingData.x;
            float y = panelTr.localScale.y * croppingData.y;
            Vector3 translateVector =  new Vector3(panelTr.localScale.x/2 - x, panelTr.localScale.y/2 - y, 0.0f);
            Debug.Log($"-=- calculate and apply position translateVector = {translateVector}");
            // panelTr.Translate(translateVector);
            panelTr.localPosition = new Vector3(panelTr.localPosition.x - translateVector.x,
                panelTr.localPosition.y + translateVector.y, 0);
            
            
            Debug.Log($"-=- panelTr.localPosition = {panelTr.localPosition}");
        }
    }
}