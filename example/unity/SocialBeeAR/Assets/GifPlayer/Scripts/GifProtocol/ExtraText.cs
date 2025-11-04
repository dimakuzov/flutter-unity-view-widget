using System.Collections.Generic;

namespace GifPlayer
{
    /// <summary>
    /// 文本扩展（解析文件时需处理,绘图时可忽略）
    /// </summary>
    public struct ExtraText
    {
        /// <summary>
        /// 扩展引导标识 定值0x21
        /// </summary>
        public byte Introducer;

        /// <summary>
        /// 文本扩展标识 0x01
        /// </summary>
        public byte Label;

        // Block Size
        public byte BlockSize;

        // Block Size & Plain Text Data List
        public List<BytesBlock> Blocks;

        public ExtraText(byte[] bytes, ref int byteIndex)
        {    // Extension Introducer(1 byte)
            // 0x21
            Introducer = bytes[byteIndex];
            byteIndex++;

            // Plain Text Label(1 byte)
            // 0x01
            Label = bytes[byteIndex];
            byteIndex++;

            // Block Size(1 byte)
            // 0x0c
            BlockSize = bytes[byteIndex];
            byteIndex++;

            // Text Grid Left Position(2 byte)
            // Not supported
            byteIndex += 2;

            // Text Grid Top Position(2 byte)
            // Not supported
            byteIndex += 2;

            // Text Grid Width(2 byte)
            // Not supported
            byteIndex += 2;

            // Text Grid Height(2 byte)
            // Not supported
            byteIndex += 2;

            // Character Cell Width(1 byte)
            // Not supported
            byteIndex++;

            // Character Cell Height(1 byte)
            // Not supported
            byteIndex++;

            // Text Foreground Color Index(1 byte)
            // Not supported
            byteIndex++;

            // Text Background Color Index(1 byte)
            // Not supported
            byteIndex++;

            Blocks = new List<BytesBlock>();
            // Block Size & Plain Text Data List
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

                // Plain Text Data(n byte)
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
