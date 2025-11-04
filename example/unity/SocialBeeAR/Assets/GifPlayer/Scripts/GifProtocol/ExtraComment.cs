using System.Collections.Generic;

namespace GifPlayer
{
    /// <summary>
    /// 注释扩展（解析文件时需处理,绘图时可忽略）
    /// </summary>
    public struct ExtraComment
    {
        /// <summary>
        /// 扩展引导标识 0x21
        /// </summary>
        public byte Introducer;

        /// <summary>
        /// 注释扩展标识 0xFE
        /// </summary>
        public byte Label;

        // Block Size & Comment Data List
        public List<BytesBlock> Blocks;

        public ExtraComment(byte[] bytes, ref int byteIndex)
        {
            // Extension Introducer(1 byte)
            // 0x21
            Introducer = bytes[byteIndex];
            byteIndex++;

            // Comment Label(1 byte)
            // 0xFE
            Label = bytes[byteIndex];
            byteIndex++;

            Blocks = new List<BytesBlock>();
            // Block Size & Comment Data List
            while (true)
            {
                // Block Size(1 byte)
                var blockSize = bytes[byteIndex];
                byteIndex++;

                if (blockSize == 0x00)
                {
                    // Block Terminator(1 byte)
                    break;
                }

                var block = new BytesBlock();
                block.Size = blockSize;

                // Comment Data(n byte)
                block.Bytes = new byte[block.Size];
                for (var index = 0; index < block.Bytes.Length; index++)
                {
                    block.Bytes[index] = bytes[byteIndex];
                    byteIndex++;
                }

                Blocks.Add(block);
            }
        }
    }
}
