/* code by 372792797@qq.com https://assetstore.unity.com/packages/2d/environments/gif-play-plugin-116943 */
using UnityEngine;

namespace GifPlayer
{
    /// <summary>
    /// Unity序列帧
    /// </summary>
    public class UnityGifFrame
    {
        /// <summary>
        /// 帧画面
        /// </summary>
        public Sprite Sprite;

        /// <summary>
        /// 帧画面
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                return Sprite.texture;
            }
        }

        /// <summary>
        /// 帧延时(秒Seconds)
        /// </summary>
        public float Delay;

        /// <summary>
        /// 初始化
        /// </summary>
        public UnityGifFrame(Sprite sprite, float delaySecond)
        {
            Sprite = sprite;
            Delay = delaySecond;
        }

        public static UnityGifFrame[] GetArray(Sprite[] sprites, float[] delays)
        {
            var array = new UnityGifFrame[sprites.Length];
            for (var index = 0; index < sprites.Length; index++)
                array[index] = new UnityGifFrame(sprites[index], delays[index]);
            return array;
        }
    }
}