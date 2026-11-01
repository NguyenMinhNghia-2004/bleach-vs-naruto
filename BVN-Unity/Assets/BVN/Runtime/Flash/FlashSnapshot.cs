using UnityEngine;

namespace BVN.Flash
{
    /// <summary>Offscreen render of a Stage into a Texture2D (edit mode / batchmode with a GPU).</summary>
    public static class FlashSnapshot
    {
        public static int LastQuads, LastBatches;

        public static Texture2D Render(Stage stage, int width, int height, Color background)
        {
            var shader = Resources.Load<Shader>("BVN/FlashSprite") ?? Shader.Find("BVN/FlashSprite");
            var renderer = new FlashRenderer(shader);
            var mats = renderer.Build(stage);
            LastQuads = renderer.LastQuadCount;
            LastBatches = renderer.LastBatchCount;
            float ppu = renderer.PixelsPerUnit;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.Clear(true, true, background);
            // stage px → world: (x/ppu, -y/ppu); map world rect [0,w]x[-h,0] onto the render target
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, stage.stageWidth / ppu, -stage.stageHeight / ppu, 0, -10, 10));
            GL.modelview = Matrix4x4.identity;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i].SetPass(0);
                UnityEngine.Graphics.DrawMeshNow(renderer.Mesh, Matrix4x4.identity, i);
            }
            GL.PopMatrix();
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(renderer.Mesh);
            return tex;
        }
    }
}
