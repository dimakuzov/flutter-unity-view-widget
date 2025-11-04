using UnityEngine;

public class AudioManager : BaseSingletonClass<AudioManager>
{

    [SerializeField] private AudioSource sourceTap;
    [SerializeField] private AudioSource sourceCompleteSetup;
    [SerializeField] private AudioSource sourceCorrect;
    [SerializeField] private AudioSource sourceWrong;
    [SerializeField] private AudioSource sourceScore;
    [SerializeField] private AudioSource sourceOnCall;
    [SerializeField] private AudioSource sourceOnReturn;


    public enum AudioOption
    {
        Undefined,
        Tap,
        CompleteSetup,
        Correct,
        Wrong,
        Score,
        OnCall,
        OnReturn
    }
    

    public void PlayAudio(AudioOption option)
    {
        switch (option)
        {
            case AudioOption.Tap:
                sourceTap.Play();
                break;
            case AudioOption.CompleteSetup:
                sourceCompleteSetup.Play();
                break;
            case AudioOption.Correct:
                sourceCorrect.Play();
                break;
            case AudioOption.Wrong:
                sourceWrong.Play();
                break;
            case AudioOption.Score:
                sourceScore.Play();
                break;
            case AudioOption.OnCall:
                sourceOnCall.Play();
                break;
            case AudioOption.OnReturn:
                sourceOnReturn.Play();
                break;
        }
    }
    
}
