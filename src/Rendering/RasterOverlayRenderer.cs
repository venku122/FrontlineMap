using System;
using FrontlineMap.Game;
using FrontlineMap.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace FrontlineMap.Rendering
{
    public sealed class RasterOverlayRenderer : IPresentation, IDisposable
    {
        private sealed class Handle : IDisposable
        {
            public Texture2D Texture;
            public void Dispose()
            {
                if (Texture != null)
                    UnityEngine.Object.Destroy(Texture);
                Texture = null;
            }
        }
        private GameObject overlay;
        private RawImage image;
        public MapBinding Binding
        {
            get; set;
        }
        public double LastCopyMilliseconds
        {
            get; private set;
        }
        public double LastUploadMilliseconds
        {
            get; private set;
        }
        public int OwnedTextures
        {
            get; private set;
        }
        public int OwnedObjects => overlay == null ? 0 : 1;
        public IDisposable Prepare(FieldResult result)
        {
            Texture2D texture = null;
            try
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var g = result.Field.Geometry;
                var colors = new Color32[result.Pixels.Length];
                for (int i = 0; i < colors.Length; i++)
                {
                    var p = result.Pixels[i];
                    colors[i] = new Color32(p.R, p.G, p.B, p.A);
                }
                LastCopyMilliseconds = timer.Elapsed.TotalMilliseconds;
                timer.Restart();
                texture = new Texture2D(g.Columns, g.Rows, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(colors);
                texture.Apply(false, true);
                LastUploadMilliseconds = timer.Elapsed.TotalMilliseconds;
                OwnedTextures++;
                return new Handle { Texture = texture };
            }
            catch { if (texture != null) UnityEngine.Object.Destroy(texture); throw; }
        }
        public void Commit(IDisposable staged)
        {
            var h = (Handle)staged;
            if (Binding == null || Binding.Rect == null)
                throw new InvalidOperationException("Map binding lost");
            if (overlay == null)
            {
                GameObject pending = null;
                try
                {
                    pending = new GameObject("FrontlineMap influence", typeof(RectTransform), typeof(RawImage));
                    var pendingImage = pending.GetComponent<RawImage>();
                    // Never attach an enabled, texture-less RawImage to the native canvas.
                    pendingImage.enabled = false;
                    pendingImage.raycastTarget = false;
                    pending.layer = Binding.Rect.gameObject.layer;
                    pending.transform.SetParent(Binding.Rect, false);
                    pending.transform.SetAsFirstSibling();
                    var rect = (RectTransform)pending.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    overlay = pending;
                    image = pendingImage;
                    pending = null;
                }
                finally
                {
                    if (pending != null)
                    {
                        pending.SetActive(false);
                        UnityEngine.Object.Destroy(pending);
                    }
                }
            }
            if (overlay.transform.parent != Binding.Rect)
                throw new InvalidOperationException("Map changed before commit");
            var previousTexture = image.texture;
            bool previousEnabled = image.enabled;
            try
            {
                image.texture = h.Texture;
                image.enabled = true;
            }
            catch
            {
                // UI dirty callbacks can throw after RawImage has already stored the new texture.
                // The caller releases that staged texture on failure, so restore the shown handle.
                try
                {
                    image.texture = previousTexture;
                    image.enabled = previousEnabled && previousTexture != null;
                }
                catch
                {
                    if (image != null)
                        image.enabled = false;
                }
                throw;
            }
        }
        public void Clear()
        {
            if (image != null)
            {
                image.enabled = false;
                image.texture = null;
            }
        }
        public void Release(IDisposable handle)
        {
            handle.Dispose();
            OwnedTextures--;
        }
        public void Dispose()
        {
            Clear();
            if (overlay != null)
                UnityEngine.Object.Destroy(overlay);
            overlay = null;
            image = null;
            Binding = null;
        }
    }
}
