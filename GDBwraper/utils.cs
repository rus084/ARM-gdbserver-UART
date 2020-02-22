
using System;
using System.IO;
using System.Text;

namespace common
{
    public static class utils
    {
        #region HexToByte
        /// <summary>
        /// method to convert hex string into a byte array
        /// </summary>
        /// <param name="msg">string to convert</param>
        /// <returns>a byte array</returns>
        public static byte[] HexToByte(string msg)
        {
            //remove any spaces from the string
            msg = msg.Replace(" ", "").Replace(":", "");
            //create a byte array the length of the
            //divided by 2 (Hex is 2 characters in length)
            byte[] comBuffer = new byte[msg.Length / 2];
            //loop through the length of the provided string
            for (int i = 0; i < msg.Length; i += 2)
                //convert each set of 2 characters to a byte
                //and add to the array
                try
                {
                    comBuffer[i / 2] = (byte)Convert.ToByte(msg.Substring(i, 2), 16);
                }
                catch
                {
                    return comBuffer;
                }
            //return the array
            return comBuffer;
        }
        #endregion


        #region ByteToHex
        /// <summary>
        /// method to convert a byte array into a hex string
        /// </summary>
        /// <param name="comByte">byte array to convert</param>
        /// <returns>a hex string</returns>
        public static string ByteToHex(byte[] comByte)
        {
            //create a new StringBuilder object
            StringBuilder builder = new StringBuilder(comByte.Length * 3);
            //loop through each byte in the array
            foreach (byte data in comByte)
                //convert the byte to a string and add to the stringbuilder
                builder.Append(Convert.ToString(data, 16).PadLeft(2, '0'));
            //return the converted value
            return builder.ToString().ToUpper();
        }


        public static string ByteToHex(byte[] comByte, int length)
        {
            //create a new StringBuilder object
            StringBuilder builder = new StringBuilder(length * 2);
            //loop through each byte in the array
            for (int i = 0; i < length; i++)
            {
                builder.Append(Convert.ToString(comByte[i], 16).PadLeft(2, '0'));
            }
            //return the converted value
            return builder.ToString().ToUpper();
        }
        #endregion

        public static byte[] StringToByte(string s)
        {
            var mems = new MemoryStream();
            var binWriter = new BinaryWriter(mems);
            //binWriter.Write((byte)deserialized.message.Length);
            binWriter.Write(s);
            binWriter.Seek(0, SeekOrigin.Begin);

            byte[] result = mems.ToArray();
            return result;
        }

    }
}
