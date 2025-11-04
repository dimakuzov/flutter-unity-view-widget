using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GifPlayer
{
    /// <summary>
    /// GIF文件结构
    /// </summary>
    public class GraphicsInterchangeFormat
    {
        //结构顺序是有意义的 根据 重要程度、处理先后 排序
        //todo 结构命名标出是global属性还是frame属性 有助于优化

        #region 全局属性
        /// <summary>
        /// GIF声明
        /// </summary>
        public string Signature;

        /// <summary>
        /// 版本声明
        /// </summary>
        public string Version;

        /// <summary>
        /// 画布宽
        /// </summary>
        public ushort Width;

        /// <summary>
        /// 画布高
        /// </summary>
        public ushort Height;

        #region Packet Fields(1 byte)
        /// <summary>
        /// 是否包含全局色表
        /// </summary>
        public bool FlagGlobalColorTable;

        /// <summary>
        /// 颜色分辨率 （不处理）
        /// </summary>
        public int ColorResolution;

        /// <summary>
        /// 重要颜色前排标识 一般为false （不处理）
        /// </summary>
        public bool Flagsort;

        /// <summary>
        /// 全局色表的数量 范围 0-256 所占字节再*3
        /// </summary>
        public int GlobalColorTableSize;
        #endregion

        /// <summary>
        /// 背景色色号
        /// </summary>
        public byte BgColorIndex;

        /// <summary>
        /// 像素宽高比
        /// </summary>
        public byte PixelAspectRatio;

        /// <summary>
        /// 全局色表 3个byte一组
        /// </summary>
        public byte[][] GlobalColorTable;
        #endregion

        #region 附附属性（序列帧）集
        /// <summary>
        /// 帧描述器集
        /// </summary>
        public FrameImageDescriptor[] FrameImageDescriptors;

        /// <summary>
        /// 绘图控制器集
        /// </summary>
        public FrameGraphicController[] FrameGraphicControllers;

        /// <summary>
        /// 应用扩展集
        /// </summary>
        public ExtraInformation[] ExtraInformations;

        /// <summary>
        /// 注释扩展集
        /// </summary>
        public ExtraComment[] ExtraComments;

        /// <summary>
        /// 文本扩展集
        /// </summary>
        public ExtraText[] ExtraTexts;
        #endregion

        /// <summary>
        /// 结束符 0x3B
        /// </summary>
        public byte Trailer;

        #region 解析方法 //方法顺序是有意义的 根据 重要程度、处理先后 排序
        /// <summary>
        /// 设置全局属性
        /// </summary>
        private void SetGlobalProperties(byte[] bytes, ref int byteIndex)
        {
            // GIF声明 (GIF) (3 byte)
            Signature = Encoding.ASCII.GetString(bytes, 0, 3);

            // 版本声明 (87a) (3 byte)
            Version = Encoding.ASCII.GetString(bytes, 3, 3);

            // 画布宽度(2 byte)
            Width = BitConverter.ToUInt16(bytes, 6);

            // 画布高度(2 byte)
            Height = BitConverter.ToUInt16(bytes, 8);

            #region Packet Fields (1 byte)

            // 是否包含全局色表(1 bit)
            // 为1 时表明Logical Screen Descriptor 后面跟的是全局颜色表。
            FlagGlobalColorTable = bytes[10] >> 7 == 1;

            // 颜色分辨率(3 bit)
            //值加1 代表颜色表中每种基色用多少位表示,如为“111”时表示每种基色用8 位表示,则颜色表中每项为3Byte。由于该值有时可为0,一般在解码程序中,该3 位不作处理,而直接由Global Color Table Size 算出颜色表大小。
            ColorResolution = (bytes[10] >> 4) % 8 + 1;

            // 重要颜色前排标识(1 bit)
            //表示重要颜色排序标志,标志为1 时,表示颜色表中重要的颜色排在前面,有利于颜色数较少的解码器选择最好的颜色。一般该标志为0,不作处理。
            Flagsort = (bytes[10] >> 3) % 2 == 1;

            //色表长度（一个色表有3字节）(3 bit)
            //值加1 作为2 的幂,算得的数即为颜色表的项数,实际上颜色表每项由RGB 三基色构成,每种基色占一个字节,则颜色表占字节数为项数的3 倍。由于最大值为“111”,故颜色表的项数最多为256项,即256 种颜色,8 位每基色则颜色表大小为768 Bytes。
            var power = bytes[10] % 8 + 1;
            GlobalColorTableSize = (int)Math.Pow(2, power);

            #endregion

            // 背景色色号(1 byte)
            //表示背景颜色索引值*。可以这样理解：在指定大小显示区,GIF 图像的大小可能小于显示区域大小,显示区中剩余的区域则一律用背景颜色索引值在全局颜色表中对应的颜色填充。在实际解码过程中,在显示图像之前可将显示区域全部用该颜色填充。
            BgColorIndex = bytes[11];

            // 像素宽高比(1 byte)
            //表示像素宽高比,一般为0,不作处理,直接以Logical Screen 宽和高作处理。如该项不为0,则参照GIF89a 标准【1】计算。
            PixelAspectRatio = bytes[12];

            byteIndex = 13;

            //判断是否包含全局色表
            if (FlagGlobalColorTable)
            {
                //获取全局色表
                GlobalColorTable = new byte[GlobalColorTableSize][];
                var colorIndex = 0;
                var endIndex = byteIndex + (GlobalColorTableSize * 3);
                //色表长度*3（字节数）
                for (; byteIndex < endIndex; byteIndex += 3)
                    //3个字节一组
                    GlobalColorTable[colorIndex++] = new byte[] { bytes[byteIndex], bytes[byteIndex + 1], bytes[byteIndex + 2] };
            }
        }

        /// <summary>
        /// 设置扩展属性和帧描述
        /// </summary>
        private void SetExtraProperties(byte[] bytes, ref int byteIndex)
        {
            var frameGraphicControllers = new List<FrameGraphicController>();
            var frameImageDescriptors = new List<FrameImageDescriptor>();
            var extraComments = new List<ExtraComment>();
            var extraTexts = new List<ExtraText>();
            var extraInformations = new List<ExtraInformation>();

            //记录上次处理的序号
            var lastIndex = 0;

            //处理字节 序号相同就是没有字节能识别 循环结束
            while (lastIndex != byteIndex)
            {
                //同步序号
                lastIndex = byteIndex;

                //处理下一字节
                switch (bytes[byteIndex])
                {
                    //帧描述器 0x2C
                    case 0x2C:
                        frameImageDescriptors.Add(new FrameImageDescriptor(bytes, ref byteIndex));
                        break;

                    //扩展 0x21
                    case 0x21:
                        switch (bytes[byteIndex + 1])
                        {
                            //绘图控制扩展 0xF9
                            case 0xF9:
                                frameGraphicControllers.Add(new FrameGraphicController(bytes, ref byteIndex));
                                break;

                            //应用扩展 0xFF
                            case 0xFF:
                                extraInformations.Add(new ExtraInformation(bytes, ref byteIndex));
                                break;

                            //注释扩展 0xFE
                            case 0xFE:
                                extraComments.Add(new ExtraComment(bytes, ref byteIndex));
                                break;

                            //文本扩展 0x01
                            case 0x01:
                                extraTexts.Add(new ExtraText(bytes, ref byteIndex));
                                break;
                        }
                        break;

                    //结尾 0x3B
                    case 0x3B:
                        Trailer = bytes[byteIndex];
                        byteIndex++;
                        FrameGraphicControllers = frameGraphicControllers.ToArray();
                        FrameImageDescriptors = frameImageDescriptors.ToArray();
                        ExtraComments = extraComments.ToArray();
                        ExtraTexts = extraTexts.ToArray();
                        ExtraInformations = extraInformations.ToArray();
                        return;
                }
            }
        }
        #endregion

        public GraphicsInterchangeFormat(byte[] bytes)
        {
            int byteIndex = 0;
            SetGlobalProperties(bytes, ref byteIndex);
            SetExtraProperties(bytes, ref byteIndex);
        }

        /// <summary>
        /// 全局色表中获取颜色
        /// </summary>
        public Color32 GetColor(int colorIndex)
        {
            //异常
            if (!FlagGlobalColorTable || colorIndex >= GlobalColorTableSize)
            {
                Debug.LogError("全局色表不存在或者超出全局色表长度");
                return Color.clear;
            }

            //全局色
            var rgb = GlobalColorTable[colorIndex];
            return new Color32(rgb[0], rgb[1], rgb[2], 255);
        }
    }
}
