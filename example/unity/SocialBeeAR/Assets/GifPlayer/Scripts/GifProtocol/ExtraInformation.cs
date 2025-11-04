using System.Collections.Generic;
using System.Text;

namespace GifPlayer
{
    /// <summary>
    /// 应用扩展（解析文件时需处理,绘图时可忽略）
    /// </summary>
    public struct ExtraInformation
    {
        /// <summary>
        /// 扩展引导标识 0x21
        /// </summary>
        public byte Introducer;

        /// <summary>
        /// 应用扩展标识 0xFF
        /// </summary>
        public byte Label;

        // Block Size
        public byte BlockSize;

        // Block Size & Application Data List
        public List<BytesBlock> Blocks;

        public string ApplicationIdentifier;

        public string ApplicationAuthenticationCode;

        public ExtraInformation(byte[] bytes, ref int byteIndex)
        {             // Extension Introducer(1 byte)
                      // 0x21
            Introducer = bytes[byteIndex];
            byteIndex++;

            // Extension Label(1 byte)
            // 0xFF
            Label = bytes[byteIndex];
            byteIndex++;

            // Block Size(1 byte)
            // 0x0B
            BlockSize = bytes[byteIndex];
            byteIndex++;

            // Application Identifier(8 byte)
            ApplicationIdentifier = Encoding.ASCII.GetString(bytes, byteIndex, 8);
            byteIndex += 8;

            // Application Authentication Code(3 byte)
            ApplicationAuthenticationCode = Encoding.ASCII.GetString(bytes, byteIndex, 3);
            byteIndex += 3;

            Blocks = new List<BytesBlock>();

            // Block Size & Application Data List
            while (true)
            {
                // Block Size (1 byte)
                var blockSize = bytes[byteIndex];
                byteIndex++;

                if (blockSize == 0x00)
                {
                    // Block Terminator(1 byte)
                    break;
                }

                var block = new BytesBlock();
                block.Size = blockSize;

                // Application Data(n byte)
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
