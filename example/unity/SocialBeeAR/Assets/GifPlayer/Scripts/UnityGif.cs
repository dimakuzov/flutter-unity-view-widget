/* code by 372792797@qq.com https://assetstore.unity.com/packages/2d/environments/gif-play-plugin-116943 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GifPlayer
{
    public class UnityGif : MonoBehaviour
    {
        [Header("GIF的文件流，将 .gif 变为 .gif.bytes 后便可拖拽")]
        [Header("For Serialize Field,you can rename .gif to .gif.bytes")]
        public TextAsset GifBytes;

        [SerializeField]
        bool _autoPlay = true;

        [SerializeField]
        bool _loop = true;

        [Header("预加载状态，请先点击Preload按钮，确认资源已经生成后再勾选")]
        [Header("resources had been gernarated ,then select")]
        [Header("Preload state,click Preload botton,make sure")]
        [SerializeField]
        bool _preloaded = false;

        int _frameIndex = 0;
        UnityGifFrame[] _frames;
        SpriteRenderer _rendererCanvas;
        Image _imageCanvas;
        RawImage _rawCanvas;

        public bool IsPlaying { get; private set; }

        IEnumerator PlayNextFrame()
        {
            //判断状态
            if (!IsPlaying)
                yield break;

            //帧序号
            _frameIndex %= _frames.Length;

            //绘图
            if (_rendererCanvas)
                _rendererCanvas.sprite = _frames[_frameIndex].Sprite;
            if (_imageCanvas)
                _imageCanvas.sprite = _frames[_frameIndex].Sprite;
            if (_rawCanvas)
                _rawCanvas.texture = _frames[_frameIndex].Texture;

            //帧延时
            yield return new WaitForSeconds(_frames[_frameIndex].Delay);

            //序号++
            _frameIndex++;

            //播放一次
            if (!_loop && _frameIndex == _frames.Length)
            {
                IsPlaying = false;
                yield break;
            }

            //递归播放下一帧
            StartCoroutine(PlayNextFrame());
        }

        public bool IsInited { get; private set; }

        public void Init()
        {
            if (IsInited)
                return;

            _rendererCanvas = GetComponent<SpriteRenderer>();
            _imageCanvas = GetComponent<Image>();
            _rawCanvas = GetComponent<RawImage>();

            if (GifBytes == null)
            {
                Debug.LogError("UnityGif@" + name + ": GifBytes is null, Check GifBytes 请检查文件流");
                return;
            }

            var time = DateTime.Now;

            if (_preloaded)
                _frames = GifUtil.GetFramesFromResources(GifBytes);
            else
                _frames = GifUtil.GetFrames(GifBytes);

            Debug.Log(string.Format("UnityGif initing costs {0}s, loads {1} frames", (DateTime.Now - time).TotalSeconds.ToString("0.000"), _frames.Length));
        }

        public void Play()
        {
            if (IsPlaying)
                return;

            if (!IsInited)
                Init();

            if (_frames != null && _frames.Length > 0)
            {
                IsPlaying = true;
                StartCoroutine(PlayNextFrame());
            }
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        void Awake()
        {
            if (_autoPlay)
                Play();
        }
    }
}