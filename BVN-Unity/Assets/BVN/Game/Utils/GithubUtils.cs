using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// GitHub helpers. AS3 embedded .git/refs/heads/master|develop at compile time; this port reads the same
    /// hash from text resources Resources/bvn/raw/embed/git/&lt;branch&gt;.txt (missing file → empty hash).
    /// </summary>
    public class GithubUtils
    {
        /// <summary>Full git commit hash format (40 hex chars).</summary>
        private static readonly Regex HASH_PATTERN = new Regex("^[0-9a-f]{40}$", RegexOptions.IgnoreCase);

        /// <summary>Short hash display length.</summary>
        private const uint HASH_SHORT_LENGTH = 7;

        /// <summary>Master branch name</summary>
        public const string BRANCH_MASTER = "master";
        /// <summary>Develop branch name</summary>
        public const string BRANCH_DEVELOP = "develop";

        /// <summary>Cached master tip hash; null = not read yet.</summary>
        private static string _cachedMasterHash;

        /// <summary>Cached develop tip hash; null = not read yet.</summary>
        private static string _cachedDevelopHash;

        /// <summary>Master branch commit hash resource (AS3 [Embed] of .git/refs/heads/master).</summary>
        private static readonly object _masterCommitHash = "embed/git/master.txt";

        /// <summary>Develop branch commit hash resource (AS3 [Embed] of .git/refs/heads/develop).</summary>
        private static readonly object _developCommitHash = "embed/git/develop.txt";

        /// <summary>Embedded branch tip hash: develop when IsDebugger(), else master.</summary>
        public static string getCommitsHash()
        {
            return IsDebugger() ? getDevelopCommitHash() : getMasterCommitHash();
        }

        /// <summary>Short (7 chars) hash, or empty when invalid.</summary>
        public static string getCommitsHashShort()
        {
            string hash = getCommitsHash();
            if (!isValidHash(hash))
            {
                return "";
            }

            return substr(hash, 0, (int)HASH_SHORT_LENGTH);
        }

        /// <summary>Branch name for the current runtime (develop when IsDebugger()).</summary>
        public static string getCommitBranch()
        {
            return IsDebugger() ? BRANCH_DEVELOP : BRANCH_MASTER;
        }

        /// <summary>"branch@shortHash", e.g. develop@a8dbce1; empty when the hash is invalid.</summary>
        public static string getCommitsDisplayLabel()
        {
            string hashShort = getCommitsHashShort();
            if (string.IsNullOrEmpty(hashShort))
            {
                return "";
            }

            return getCommitBranch() + "@" + hashShort;
        }

        /// <summary>Opens the GitHub commit page of a hash.</summary>
        public static void goCommitsByHash(string hash)
        {
            URL.go(getCommitsUrlByHash(hash), false);
        }

        /// <summary>Branch commit list url.</summary>
        public static string getBranchUrl()
        {
            return URL.GITHUB + "/commits/" + getCommitBranch() + "/";
        }

        /// <summary>Commit page url; repository url when the hash is invalid.</summary>
        public static string getCommitsUrlByHash(string hash)
        {
            if (!isValidHash(hash))
            {
                return URL.GITHUB;
            }

            return URL.GITHUB + "/commit/" + hash;
        }

        private static string getMasterCommitHash()
        {
            if (_cachedMasterHash == null)
            {
                _cachedMasterHash = readEmbeddedHash(_masterCommitHash);
            }

            return _cachedMasterHash;
        }

        private static string getDevelopCommitHash()
        {
            if (_cachedDevelopHash == null)
            {
                _cachedDevelopHash = readEmbeddedHash(_developCommitHash);
            }

            return _cachedDevelopHash;
        }

        /// <summary>Reads the embedded ref file text and validates it as a full commit hash.</summary>
        private static string readEmbeddedHash(object embedClass)
        {
            string text = null;
            try
            {
                text = AssetBridge.LoadText(Str(embedClass));
            }
            catch (Exception e)
            {
                trace("GithubUtils.readEmbeddedHash failed:", e.Message);
            }

            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            string hash = text.Trim();
            return isValidHash(hash) ? hash : "";
        }

        private static bool isValidHash(string hash)
        {
            return !string.IsNullOrEmpty(hash) && HASH_PATTERN.IsMatch(hash);
        }
    }
}
