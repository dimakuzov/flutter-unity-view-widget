using System.Collections;
using System.Collections.Generic;
using NatCorder.Examples;
using SocialBeeAR;
using UnityEngine;


public class AudioVisualization : MonoBehaviour {
    [HideInInspector] public AudioSource audioSource;
    [HideInInspector] public bool isWorking;
    
    public  float[] samples = new float[2048];
    private float[] freqBand = new float[10];
    private float[] bandBuffer = new float[10];
    private float[] bufferDecrease = new float[10];

    private float[] _freqBandHighest = new float[10];
    public static float[] _audioBand = new float[10];
    public static float[] _audioBandBuffer = new float[10];

    public static float _Amplitude, _AmplitudeBuffer;
    private float _AmplitudeHighest;
    
    private static AudioVisualization _instance;

    public static AudioVisualization Instance {
        get { return _instance; }
    }
    
    private void Awake() {
        _instance = this;
    }
    
    private void Update () 
    {
        if(audioSource != null) {
            GetSpectrumAduioSource();
            MakeFrequencyBands();
            BandBuffer();
            CreateAudioBands();
            GetAmplitude();
            int v = 0;
            foreach (var band in _audioBandBuffer) {
                if (band >= 1) {
                    v++;
                }
                else {
                    break;
                }
            }
            if (v == 10) {
                _audioBandBuffer = new[] {0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            }
            isWorking = true;
        }
        else if(isWorking) {
            isWorking = false;
        }
    }
    
    
    private void GetSpectrumAduioSource()
    {
        audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
    }
    
    
    private void MakeFrequencyBands()
    {
        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            float average = 0;
            int sampleCount = (int)Mathf.Pow(2,i+1);
            if (i == 9)
            {
                sampleCount += 2;
            }
            for (int j = 0; j < sampleCount; j++)
            {
                average += samples[count]*(count+1);
                count++;
            }
            average /= count;
            freqBand[i] = average * 12;
        }
    }
    
    
    private void BandBuffer()
    {
        for (int i = 0; i < 10; i++)
        {
            if (freqBand[i] > bandBuffer[i])
            {
                bandBuffer[i] = freqBand[i];
                bufferDecrease[i] = 0.005f;
            }
            if (freqBand[i] < bandBuffer[i])
            {
                bandBuffer[i] -= bufferDecrease[i];
                bufferDecrease[i] *= 1.2f;
            }
        }
    }
    
    
    private void CreateAudioBands()
    {
        for (int i = 0; i < 10; i++)
        {
            if (freqBand[i] > _freqBandHighest[i])
            {
                _freqBandHighest[i] = freqBand[i];
            }
            _audioBand[i] = (freqBand[i]/_freqBandHighest[i]);
            _audioBandBuffer[i] = (bandBuffer[i]/_freqBandHighest[i]);
        }
    }
    
    
    private void GetAmplitude()
    {
        float _CurrentAmplitude = 0;
        float _CurrentAmplitudeBuffer = 0;
        for (int i = 0; i < 10; i++)
        {
            _CurrentAmplitude += _audioBand[i];
            _CurrentAmplitudeBuffer += _audioBandBuffer[i];
        }
        if (_CurrentAmplitude > _AmplitudeHighest)
            _AmplitudeHighest = _CurrentAmplitude;
        _Amplitude = _CurrentAmplitude / _AmplitudeHighest;
        _AmplitudeBuffer = _CurrentAmplitudeBuffer / _AmplitudeHighest;
    }


}