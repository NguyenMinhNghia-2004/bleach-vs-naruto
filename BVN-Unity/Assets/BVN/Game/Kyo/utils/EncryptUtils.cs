using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>MD5 file fingerprint and AES-128-CBC (no padding, as3crypto "null" pad) helpers on ByteArray.</summary>
    public class EncryptUtils
    {
        public static string md5(ByteArray bytes)
        {
            return hashBytes(bytes);
        }

        public static ByteArray encryptAES(object source, string key, string iv)
        {
            ByteArray byteArr = null;
            if (source is string s)
            {
                byteArr = new ByteArray();
                byteArr.writeUTFBytes(s);
            }
            if (source is ByteArray ba) byteArr = ba;
            if (byteArr == null) return null;

            var result = aes(byteArr.Data.ToArray(), hexToArray(key), hexToArray(iv), true);
            // as3crypto encrypted in place
            byteArr.Data.Clear();
            byteArr.Data.AddRange(result);
            byteArr.position = 0;
            return byteArr;
        }

        public static string decryptAES(ByteArray code, string key, string iv)
        {
            ByteArray b = decryptAESBytes(code, key, iv);
            b.position = 0;
            // Flash readUTFBytes stops at the first NUL (zero padding of the last block)
            return b.readUTFBytes(b.length).TrimEnd('\0');
        }

        public static ByteArray decryptAESBytes(ByteArray code, string key, string iv)
        {
            var result = aes(code.Data.ToArray(), hexToArray(key), hexToArray(iv), false);
            code.Data.Clear();
            code.Data.AddRange(result);
            code.position = 0;
            return code;
        }

        /// <summary>com.hurlant.util.Hex.toArray.</summary>
        static byte[] hexToArray(string hex)
        {
            hex = (hex ?? "").Replace(" ", "").Replace(":", "");
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
            if (hex.Length % 2 == 1) hex = "0" + hex;
            var a = new byte[hex.Length / 2];
            for (int i = 0; i < a.Length; i++) a[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return a;
        }

        static byte[] aes(byte[] data, byte[] key, byte[] iv, bool encrypt)
        {
            // no padding: the last partial block is zero-filled like as3crypto's CBC loop did
            int len = (data.Length + 15) / 16 * 16;
            if (len != data.Length) Array.Resize(ref data, len);
            if (len == 0) return data;
            using (var a = System.Security.Cryptography.Aes.Create())
            {
                a.Mode = System.Security.Cryptography.CipherMode.CBC;
                a.Padding = System.Security.Cryptography.PaddingMode.None;
                a.KeySize = key.Length * 8;
                a.Key = key;
                var ivb = new byte[16];
                Array.Copy(iv, ivb, Math.Min(16, iv.Length));
                a.IV = ivb;
                using (var t = encrypt ? a.CreateEncryptor() : a.CreateDecryptor())
                    return t.TransformFinalBlock(data, 0, data.Length);
            }
        }

        private static string hashBytes(ByteArray fileBytes)
        {
            var src = fileBytes.Data;
            int length = src.Count;
            byte[] bytes;
            if (length < 1024 * 2)
            {
                bytes = src.ToArray();
            }
            else
            {
                bytes = new byte[2048];
                src.CopyTo(0, bytes, 0, 1024);
                src.CopyTo(length - 1024, bytes, 1024, 1024);
            }

            using (var md = System.Security.Cryptography.MD5.Create())
            {
                var hash = md.ComputeHash(bytes);
                var sb = new System.Text.StringBuilder(32);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
