using System;
using FrontlineMap.Game;
using FrontlineMap.Core;
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
            public ContourSegment[] Segments; public double Width; public bool Layered;
            public void Dispose()
            {
                if (Texture != null)
                    UnityEngine.Object.Destroy(Texture);
                Texture = null;
            }
        }
        private GameObject overlay;
        private RawImage image;
        private FrontContourGraphic contours;
        public int ContourCount => contours == null ? 0 : contours.Segments.Length;
        private Color32[] uploadColors;
        public OverlayAppearance Appearance { get; set; } = OverlayAppearance.Default;
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
        public int OwnedObjects => overlay == null ? 0 : (contours == null ? 1 : 2);
        public IDisposable Prepare(FieldResult result)
        {
            Texture2D texture = null;
            try
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var g = result.Field.Geometry;
                var colors = uploadColors;
                if (colors == null || colors.Length != result.Pixels.Length)
                    uploadColors = colors = new Color32[result.Pixels.Length];
                var palette = Appearance;
                bool recolor = !ReferenceEquals(palette, OverlayAppearance.Default);
                for (int i = 0; i < colors.Length; i++)
                {
                    var p = result.Pixels[i];
                    if (recolor) p = palette.Apply(p);
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
                return new Handle { Texture = texture, Segments = result.Contours, Width = result.LineWidthPixels, Layered = result.Field.Confidence != null };
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
                GameObject pending = null, pendingChild = null;
                FrontContourGraphic pendingContours = null;
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
                    if (h.Layered)
                    {
                        var child = pendingChild = new GameObject("FrontlineMap contours", typeof(RectTransform), typeof(FrontContourGraphic));
                        child.transform.SetParent(pending.transform,false); pendingChild = null; child.layer=pending.layer;
                        var cr=(RectTransform)child.transform;cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=Vector2.zero;cr.offsetMax=Vector2.zero;
                        pendingContours=child.GetComponent<FrontContourGraphic>();pendingContours.enabled=false;pendingContours.raycastTarget=false;
                    }
                    overlay = pending;
                    image = pendingImage;
                    contours = pendingContours;
                    pending = null;
                }
                finally
                {
                    if (pendingChild != null) { pendingChild.SetActive(false); UnityEngine.Object.Destroy(pendingChild); }
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
            var previousSegments=contours == null ? null : contours.Segments;
            double previousWidth=contours == null ? 3 : contours.WidthPixels;
            Color previousColor=contours == null ? Color.white : contours.color;
            bool previousContoursEnabled=contours != null && contours.enabled;
            try
            {
                if (contours != null)
                {
                    Color front; ColorUtility.TryParseHtmlString("#"+Appearance.Front,out front);
                    contours.Set(h.Segments,h.Width,front); contours.enabled=h.Segments.Length>0;
                }
                image.texture = h.Texture;
                image.enabled = true;
            }
            catch
            {
                // UI dirty callbacks can throw after RawImage has already stored the new texture.
                // The caller releases that staged texture on failure, so restore the shown handle.
                try
                {
                    if(contours!=null){contours.Set(previousSegments,previousWidth,previousColor);contours.enabled=previousContoursEnabled;}
                    image.texture = previousTexture;
                    image.enabled = previousEnabled && previousTexture != null;
                }
                catch
                {
                    if (image != null) image.enabled = false;
                    if (contours != null) contours.enabled = false;
                }
                throw;
            }
        }
        public void Clear()
        {
            if(contours!=null){contours.enabled=false;contours.Set(Array.Empty<ContourSegment>(),3,Color.white);}
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
            contours = null;
            Binding = null;
            uploadColors = null;
        }
    }
}
