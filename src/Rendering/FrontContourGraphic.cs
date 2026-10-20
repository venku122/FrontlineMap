using System;
using FrontlineMap.Core;
using UnityEngine;
using UnityEngine.UI;
namespace FrontlineMap.Rendering
{
    // Separate masked UI geometry; raw fill stays a single transactionally published texture.
    public sealed class FrontContourGraphic : MaskableGraphic
    {
        public ContourSegment[] Segments { get; private set; } = Array.Empty<ContourSegment>();
        public double WidthPixels { get; private set; } = 3;
        Matrix4x4 previousTransform, previousView, previousProjection;
        Rect previousPixels;
        Canvas previousCanvas; Camera previousCamera; RenderMode previousMode;
        int previousWidth, previousHeight; bool projectionValid;
        protected override void OnEnable() { base.OnEnable(); projectionValid = false; }
        protected override void OnCanvasHierarchyChanged() { base.OnCanvasHierarchyChanged(); projectionValid = false; }
        void LateUpdate()
        {
            if (Segments.Length == 0 || !IsActive()) return;
            var c = canvas;
            var camera = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
            var transformMatrix = rectTransform.localToWorldMatrix;
            var view = camera == null ? Matrix4x4.identity : camera.worldToCameraMatrix;
            var projection = camera == null ? Matrix4x4.identity : camera.projectionMatrix;
            var pixels = camera == null ? new Rect(0, 0, Screen.width, Screen.height) : camera.pixelRect;
            var mode = c == null ? RenderMode.ScreenSpaceOverlay : c.renderMode;
            if (!projectionValid || transformMatrix != previousTransform || camera != previousCamera || c != previousCanvas
                || view != previousView || projection != previousProjection || pixels != previousPixels || mode != previousMode
                || Screen.width != previousWidth || Screen.height != previousHeight)
            {
                previousTransform = transformMatrix; previousView = view; previousProjection = projection;
                previousPixels = pixels; previousCamera = camera; previousCanvas = c; previousMode = mode;
                previousWidth = Screen.width; previousHeight = Screen.height; projectionValid = true;
                SetVerticesDirty();
            }
        }
        public void Set(ContourSegment[] segments, double width, Color tint)
        {
            Segments = segments ?? Array.Empty<ContourSegment>(); WidthPixels = width; color = tint;
            raycastTarget = false; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (Segments.Length == 0) return;
            var r = rectTransform.rect; var c = canvas;
            Camera camera = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
            int count = Math.Min(8192, Segments.Length);
            for (int i = 0; i < count; i++)
            {
                var s = Segments[i]; var a = new Vector2(r.xMin+(float)s.A.U*r.width,r.yMin+(float)s.A.V*r.height);
                var b = new Vector2(r.xMin+(float)s.B.U*r.width,r.yMin+(float)s.B.V*r.height);
                Vector2 sa = RectTransformUtility.WorldToScreenPoint(camera,rectTransform.TransformPoint(a));
                Vector2 sb = RectTransformUtility.WorldToScreenPoint(camera,rectTransform.TransformPoint(b));
                var d=sb-sa;if(d.sqrMagnitude<1e-8f)continue;
                var normal=new Vector2(-d.y,d.x).normalized*(float)WidthPixels*.5f;
                Vector2 ap,am,bp,bm;
                if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,sa+normal,camera,out ap)
                    ||!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,sa-normal,camera,out am)
                    ||!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,sb+normal,camera,out bp)
                    ||!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,sb-normal,camera,out bm))continue;
                Color tint=color;tint.a*=(float)s.Confidence;int offset=vh.currentVertCount;
                vh.AddVert(ap,tint,Vector2.zero);vh.AddVert(am,tint,Vector2.zero);vh.AddVert(bm,tint,Vector2.zero);vh.AddVert(bp,tint,Vector2.zero);
                vh.AddTriangle(offset,offset+1,offset+2);vh.AddTriangle(offset,offset+2,offset+3);
            }
        }
    }
}
