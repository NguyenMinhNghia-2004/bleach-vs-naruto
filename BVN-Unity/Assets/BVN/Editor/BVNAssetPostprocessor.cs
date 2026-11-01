using UnityEditor;
using UnityEngine;

namespace BVN.EditorTools
{
    /// <summary>Import settings for converted BVN data: lossless atlases without mipmaps, decompressed short SFX.</summary>
    public sealed class BVNAssetPostprocessor : AssetPostprocessor
    {
        const string DataRoot = "Assets/BVNData/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(DataRoot)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = false; // atlases are extruded; keep raw colour under alpha
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.isReadable = false;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(DataRoot)) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            bool music = assetPath.Contains("/bgm/");
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            ai.defaultSampleSettings = s;
            ai.forceToMono = false;
            ai.loadInBackground = false;
        }
    }
}
