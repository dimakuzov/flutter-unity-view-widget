using System;

namespace GifPlayer
{
    /// <summary>
    /// 绘图控制扩展（包含绘图信息,必须处理）
    /// </summary>
    public struct FrameGraphicController
    {
        /// <summary>
        /// 扩展引导标识 0x21
        /// </summary>
        public byte Introducer;

        /// <summary>
        /// 绘图控制扩展标识 0xF9
        /// </summary>
        public byte Label;

        /// <summary>
        /// 扩展字节长度 不算结尾字节 绘图控制拓展的字节长度基本是0x04
        /// </summary>
        public byte Length;

        #region Packet Fields (1byte)

        /// <summary>
        /// 保留字段（3bit）
        /// </summary>
        public int Reserved;

        /// <summary>
        /// 下一帧图像预处理方法（3bit）
        /// </summary>
        public NextFrameDisposalMethod NextFrameDisposalMethod;

        /// <summary>
        /// 用户输入标志,为1 时表示处理完该图像域后等待用户的输入后才开始下一图像域的处理。(1bit)
        /// </summary>
        public bool FlagUserInput;

        /// <summary>
        /// 透明颜色索引有效标志,该标志置位表示透明颜色索引有效,使用方法参照【透明颜色索引】注释。补充：同时表示背景透明(1bit)
        /// </summary>
        public bool FlagTransparentColor;

        #endregion

        /// <summary>
        /// 帧延时 1/100 秒 （2byte）
        /// </summary>
        public ushort DelayTime;

        /// <summary>
        /// 帧延时(秒)
        /// </summary>
        public float DelaySecond
        {
            get
            {
                float delaySecond = DelayTime / 100f;

                if (delaySecond <= 0f)
                    delaySecond = 0.1f;

                return delaySecond;
            }
        }

        /// <summary>
        /// 透明颜色索引,在透明颜色索引有效标识为真的情况下,解码所得颜色索引与该索引值相等时,数据将不作处理（不更新对应像素）。(1byte)
        /// </summary>
        public byte TransparentColorIndex;

        /// <summary>
        /// 尾部标识 0x00 (1byte)
        /// </summary>
        public byte Terminator;

        public FrameGraphicController(byte[] bytes, ref int byteIndex)
        {
            //扩展标引导记 0x21 (1 byte)
            Introducer = bytes[byteIndex];
            byteIndex++;

            //绘图控制扩展标记 0xF9 (1 byte)
            Label = bytes[byteIndex];
            byteIndex++;

            //扩展字节长度 0x04 (1 byte)
            Length = bytes[byteIndex];
            byteIndex++;

            #region Packet Fields (1byte)

            //保留字段（3bit）
            Reserved = bytes[byteIndex] >> 5;

            //图像显示方法 (3 bit)
            NextFrameDisposalMethod = (NextFrameDisposalMethod)((bytes[byteIndex] >> 2) % 8);

            //用户输入标志 (1 bit)
            FlagUserInput = (bytes[byteIndex] >> 1) % 2 == 1;

            //透明颜色索引有效标志 (1 bit)
            FlagTransparentColor = bytes[byteIndex] % 2 == 1;

            #endregion

            //指针下移
            byteIndex++;

            //帧延时 (2 byte)
            DelayTime = BitConverter.ToUInt16(bytes, byteIndex);
            byteIndex += 2;

            //透明颜色索引 (1 byte)
            TransparentColorIndex = bytes[byteIndex];
            byteIndex++;

            //结尾标识 (1 byte)
            Terminator = bytes[byteIndex];
            byteIndex++;
        }
    }
}
