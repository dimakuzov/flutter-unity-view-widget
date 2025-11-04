using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GifPlayer
{
    /// <summary>
    /// Lzw解码
    /// </summary>
    public static class LzwUtil
    {
        /// <summary>
        /// 初始化lzw字典
        /// </summary>
        private static Dictionary<int, string> InitLzwDictionary(int power)
        {
            //字典大小为2的power次方
            var dictLength = (int)Math.Pow(2, power);
            var dict = new Dictionary<int, string>();
            for (var index = 0; index < dictLength + 2; index++)
                //ASCII (0,c0),(1,c1),(2,c2),(3,c3)....(length+2,c(length+2))
                dict.Add(index, ((char)index).ToString());
            return dict;
        }

        /// <summary>
        /// 获取整型
        /// </summary>
        private static int GetInt(this BitArray array, int startIndex, int bitLength)
        {
            var newArray = new BitArray(bitLength);

            for (var index = 0; index < bitLength; index++)
                if (array.Length <= startIndex + index)
                    newArray[index] = false;
                else
                    newArray[index] = array.Get(startIndex + index);

            if (newArray.Length > 32)
                return 0;

            var intArray = new int[1];
            newArray.CopyTo(intArray, 0);
            return intArray[0];
        }

        /// <summary>
        /// Lwz解码为目标长度
        /// </summary>
        public static byte[] GetLzwDecodedBytes(List<byte> srcBytes, int dictPower, int destLength)
        {
            //初始化字典
            var dictPowerPlush = dictPower + 1;
            var dictLength = (int)Math.Pow(2, dictPower);
            var dictLengthPlush = dictLength + 1;
            var dict = InitLzwDictionary(dictPower);

            //转为比特流处理
            var srcBits = new BitArray(srcBytes.ToArray());

            var destBytes = new byte[destLength];
            var outputAddIndex = 0;

            string prevEntry = null;

            var dictInitFlag = false;

            var bitDataIndex = 0;

            // 循环处理Lzw数据
            while (bitDataIndex < srcBits.Length)
            {
                if (dictInitFlag)
                {
                    dictPowerPlush = dictPower + 1;
                    dictLength = (int)Math.Pow(2, dictPower);
                    dictLengthPlush = dictLength + 1;
                    dict = InitLzwDictionary(dictPower);
                    dictInitFlag = false;
                }

                var key = srcBits.GetInt(bitDataIndex, dictPowerPlush);

                string entry;

                if (key == dictLength)
                {
                    dictInitFlag = true;
                    bitDataIndex += dictPowerPlush;
                    prevEntry = null;
                    continue;
                }
                else if (key == dictLengthPlush)
                    break;
                else if (dict.ContainsKey(key))
                    entry = dict[key];
                else if (key >= dict.Count)
                {
                    if (prevEntry != null)
                        entry = prevEntry + prevEntry[0];
                    else
                    {
                        bitDataIndex += dictPowerPlush;
                        continue;
                    }
                }
                else
                {
                    bitDataIndex += dictPowerPlush;
                    continue;
                }

                var temp = Encoding.Unicode.GetBytes(entry);
                for (var index = 0; index < temp.Length; index++)
                {
                    if (index % 2 == 0)
                    {
                        destBytes[outputAddIndex] = temp[index];
                        outputAddIndex++;
                    }
                }

                if (outputAddIndex >= destLength)
                    break;

                if (prevEntry != null)
                    dict.Add(dict.Count, prevEntry + entry[0]);

                prevEntry = entry;

                bitDataIndex += dictPowerPlush;

                if (dictPowerPlush == 3 && dict.Count >= 8)
                    dictPowerPlush = 4;
                else if (dictPowerPlush == 4 && dict.Count >= 16)
                    dictPowerPlush = 5;
                else if (dictPowerPlush == 5 && dict.Count >= 32)
                    dictPowerPlush = 6;
                else if (dictPowerPlush == 6 && dict.Count >= 64)
                    dictPowerPlush = 7;
                else if (dictPowerPlush == 7 && dict.Count >= 128)
                    dictPowerPlush = 8;
                else if (dictPowerPlush == 8 && dict.Count >= 256)
                    dictPowerPlush = 9;
                else if (dictPowerPlush == 9 && dict.Count >= 512)
                    dictPowerPlush = 10;
                else if (dictPowerPlush == 10 && dict.Count >= 1024)
                    dictPowerPlush = 11;
                else if (dictPowerPlush == 11 && dict.Count >= 2048)
                    dictPowerPlush = 12;
                else if (dictPowerPlush == 12 && dict.Count >= 4096)
                {
                    var nextKey = srcBits.GetInt(bitDataIndex, dictPowerPlush);
                    if (nextKey != dictLength)
                        dictInitFlag = true;
                }
            }

            return destBytes;
        }

        /// <summary>
        /// 整理交错
        /// </summary>
        public static byte[] GetInterlaceDecodedIndexs(byte[] indexs, int width)
        {
            var height = 0;
            var dataIndex = 0;
            var newIndexs = new byte[indexs.Length];
            // Every 8th. row, starting with row 0.
            for (var index = 0; index < newIndexs.Length; index++)
            {
                if (height % 8 == 0)
                {
                    newIndexs[index] = indexs[dataIndex];
                    dataIndex++;
                }
                if (index != 0 && index % width == 0)
                {
                    height++;
                }
            }
            height = 0;
            // Every 8th. row, starting with row 4.
            for (var index = 0; index < newIndexs.Length; index++)
            {
                if (height % 8 == 4)
                {
                    newIndexs[index] = indexs[dataIndex];
                    dataIndex++;
                }
                if (index != 0 && index % width == 0)
                {
                    height++;
                }
            }
            height = 0;
            // Every 4th. row, starting with row 2.
            for (var index = 0; index < newIndexs.Length; index++)
            {
                if (height % 4 == 2)
                {
                    newIndexs[index] = indexs[dataIndex];
                    dataIndex++;
                }
                if (index != 0 && index % width == 0)
                {
                    height++;
                }
            }
            height = 0;
            // Every 2nd. row, starting with row 1.
            for (var index = 0; index < newIndexs.Length; index++)
            {
                if (height % 8 != 0 && height % 8 != 4 && height % 4 != 2)
                {
                    newIndexs[index] = indexs[dataIndex];
                    dataIndex++;
                }
                if (index != 0 && index % width == 0)
                {
                    height++;
                }
            }

            return newIndexs;
        }
    }
}
