using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// UUID-like id: SHA-1 of (time + host string + random + counter), formatted 8-4-4-4-12 upper-case hex.
    /// Uses a private System.Random (not the seeded game RNG) so creating ids never shifts gameplay randomness.
    /// </summary>
    public class UUID
    {
        private static double _counter = 0;
        private static readonly Random _rnd = new Random();

        public static string create()
        {
            double id1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            double id2 = _rnd.NextDouble() * double.MaxValue;
            string id3 = serverString;
            string rawID = calculate(NumStr(id1) + id3 + NumStr(id2) + NumStr(_counter++)).ToUpperInvariant();
            return substring(rawID, 0, 8) + "-" + substring(rawID, 8, 12) + "-" + substring(rawID, 12, 16) +
                   "-" + substring(rawID, 16, 20) + "-" + substring(rawID, 20, 32);
        }

        /// <summary>flash.system.Capabilities.serverString equivalent.</summary>
        static string serverString => "OS=" + UnityEngine.SystemInfo.operatingSystem + "&M=" + UnityEngine.SystemInfo.deviceModel;

        private static string calculate(string src)
        {
            return hex_sha1(src);
        }

        /// <summary>SHA-1 over the low byte of each UTF-16 char (same as the AS3 str2binb implementation).</summary>
        private static string hex_sha1(string src)
        {
            var bytes = new byte[src.Length];
            for (int i = 0; i < src.Length; i++) bytes[i] = (byte)(src[i] & 0xFF);
            using (var sha = System.Security.Cryptography.SHA1.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var sb = new System.Text.StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public UUID()
        {
        }
    }
}
