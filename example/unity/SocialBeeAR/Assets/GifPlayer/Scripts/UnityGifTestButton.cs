using UnityEngine;
using UnityEngine.UI;

namespace GifPlayer
{
    public class UnityGifTestButton : MonoBehaviour
    {
        public UnityGif Gif;

        void Start()
        {
            this.GetComponent<Button>().onClick.AddListener(delegate
            {
                if (Gif.IsPlaying)
                    Gif.Pause();
                else
                    Gif.Play();
            });
        }
    }
}
