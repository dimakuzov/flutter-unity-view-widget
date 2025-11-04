using System.Collections;
using System.Collections.Generic;
using NatCorder;
using NatCorder.Clocks;
using NatCorder.Inputs;
using UnityEngine;

public class LandscapeTest : MonoBehaviour
{
    
    int videoWidth = 720;
    int videoHeight = 1280;
    
    private IMediaRecorder recorder;
    private CameraInput cameraInput;
    private AudioInput audioInput;
    [HideInInspector] public AudioSource microphoneSource;
    private RealtimeClock clock;
    

    
    public void StartVideoRecording() {
        videoWidth = Screen.width;
        videoHeight = Screen.height;
        print($"1videoWidth = {videoWidth}, videoHeight = {videoHeight}");
        
        var sampleRate = false ? AudioSettings.outputSampleRate : 0;
        var channelCount = false ? (int)AudioSettings.speakerMode : 0;
        clock = new RealtimeClock();
        recorder = new MP4Recorder(videoWidth, videoHeight, 60, sampleRate, channelCount);
        // recorder = new MP4Recorder(videoWidth, videoHeight, 60, sampleRate, channelCount);
        // Create recording inputs
        cameraInput = new CameraInput(recorder, clock, Camera.main);
        audioInput = false ? new AudioInput(recorder, clock, microphoneSource, true) : null;
        // Unmute microphone
        // microphoneSource.mute = audioInput == null;
    }
    
    
    
    public async void StopRecording()
    {
        // Stop recording
        audioInput?.Dispose();
        cameraInput.Dispose();
        string path = await recorder.FinishWriting();
        
    }

    public void LandscapeOrientation() {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }
    
    
}
