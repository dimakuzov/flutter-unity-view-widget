/* code by 372792797@qq.com https://assetstore.unity.com/packages/2d/environments/gif-play-plugin-116943 */
using System;
using System.Text;
using UnityEngine;

namespace GifPlayer
{
    public static class GifUtil
    {
        private static Vector2 _pivotCenter = new Vector2(0.5f, 0.5f);

        public static Sprite GetSprite(this Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), _pivotCenter);
            sprite.name = texture.name;
            return sprite;
        }

        /// <summary>
        /// 获取序列帧集
        /// </summary>
        public static UnityGifFrame[] GetFrames(TextAsset gifAsset)
        {
            //解析 analyze
            try
            {
                var bytes = gifAsset.bytes;
                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogError("GIF解析发生错误/Gif analysed error\r\n输入了空的文件流/the bytes is empty");
                    return null;
                }

                //初始化GIF
                var gif = new GraphicsInterchangeFormat(bytes);

                //序列帧集初始化
                var frames = new UnityGifFrame[gif.FrameImageDescriptors.Length];
                //初始化Texture
                var frameTexture = new Texture2D(gif.Width, gif.Height);

                //透明背景
                var transparentPixels = frameTexture.GetPixels32();
                for (var index = 0; index < transparentPixels.Length; index++)
                    transparentPixels[index] = Color.clear;

                //背景色
                var backgroundColor = gif.GetColor(gif.BgColorIndex);
                var backgroundPixels = frameTexture.GetPixels32();
                for (var index = 0; index < backgroundPixels.Length; index++)
                    backgroundPixels[index] = backgroundColor;

                //记录下一帧的处理方法
                NextFrameDisposalMethod frameDisposalMethod = NextFrameDisposalMethod.Normal;
                bool previousReserved = false;

                //处理每个图块
                for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
                {
                    //命名
                    frameTexture.name = "FrameOfIndex" + frameIndex;
                    //图像描述器
                    var frameImageDescriptor = gif.FrameImageDescriptors[frameIndex];
                    //绘图控制扩展
                    var frameGraphicController = gif.FrameGraphicControllers[frameIndex];

                    //上一帧控制器如果记录本帧的处理方法为bg，并且本帧的透明标识为true，那么背景替换为透明
                    if (frameDisposalMethod == NextFrameDisposalMethod.Bg && frameGraphicController.FlagTransparentColor)
                        frameTexture.SetPixels32(transparentPixels);

                    //着色范围
                    var blockWidth = frameImageDescriptor.Width;
                    var blockHeight = frameImageDescriptor.Height;

                    var leftIndex = frameImageDescriptor.MarginLeft;//含
                    var rightBorder = leftIndex + blockWidth;//不含

                    var topBorder = gif.Height - frameImageDescriptor.MarginTop;//不含
                    var bottomIndex = topBorder - blockHeight;//含

                    //色表
                    var descriptorColors = frameImageDescriptor.GetColors(frameGraphicController, gif);
                    //色表指针
                    var colorIndex = -1;
                    //gif的y是从上往下，texture的y是从下往上
                    for (var y = topBorder - 1; y >= bottomIndex; y--)
                    {
                        for (var x = leftIndex; x < rightBorder; x++)
                        {
                            colorIndex++;
                            //判断是否保留像素
                            if (previousReserved && descriptorColors[colorIndex].a == 0)
                                continue;
                            frameTexture.SetPixel(x, y, descriptorColors[colorIndex]);
                        }
                    }

                    //保存
                    frameTexture.wrapMode = TextureWrapMode.Clamp;
                    frameTexture.Apply();

                    //添加序列帧,并兵初始化Texture
                    var spriteFrame = frameTexture.GetSprite();
                    frames[frameIndex] = new UnityGifFrame(spriteFrame, frameGraphicController.DelaySecond);
                    frameTexture = new Texture2D(gif.Width, gif.Height);

                    //预处理下一帧图像
                    previousReserved = false;
                    switch (frameGraphicController.NextFrameDisposalMethod)
                    {
                        //1 - Do not dispose. The graphic is to be left in place. 
                        //保留此帧
                        case NextFrameDisposalMethod.Last:
                            frameTexture.SetPixels(frames[frameIndex].Texture.GetPixels());
                            previousReserved = true;
                            break;

                        //2 - Restore to background color. The area used by the graphic must be restored to the background color. 
                        //还原成背景色
                        case NextFrameDisposalMethod.Bg:
                            frameTexture.SetPixels32(backgroundPixels);
                            break;

                        //3 - Restore to previous. The decoder is required to restore the area overwritten by the graphic with what was there prior to rendering the graphic.
                        //还原成上一帧
                        case NextFrameDisposalMethod.Previous:
                            frameTexture.SetPixels(frames[frameIndex - 1].Texture.GetPixels());
                            previousReserved = true;
                            break;
                    }
                    frameDisposalMethod = frameGraphicController.NextFrameDisposalMethod;
                }
                return frames;
            }
            catch (Exception ex)
            {
                var logBuilder = new StringBuilder();
                logBuilder.AppendLine("Exception: Gif analysed error");
                logBuilder.AppendLine("Maybe the version invalid , try to convert it by photoshop web format gif");
                logBuilder.Append(ex.Message);
                Debug.LogError(logBuilder.ToString());
                return null;
            }
        }

        public const string ResourcePath = "GifPreload/{0}/{1}";

        public static UnityGifFrame[] GetFramesFromResources(TextAsset gifAsset)
        {
            var delaysSeconds = Resources.Load<TextAsset>(string.Format(ResourcePath, gifAsset.name, "delays")).text.Split(',');
            var frames = new UnityGifFrame[delaysSeconds.Length];
            for (var index = 0; index < frames.Length; index++)
            {
                frames[index] = new UnityGifFrame(Resources.Load<Texture2D>(
                     string.Format(ResourcePath, gifAsset.name, index)).GetSprite(),
                     float.Parse(delaysSeconds[index]));
            }
            return frames;
        }
    }
}