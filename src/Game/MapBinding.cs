using System;
using FrontlineMap.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontlineMap.Game
{
    public sealed class MapBinding
    {
        public readonly DynamicMap Map;
        public readonly RectTransform Rect;
        public readonly MapGeometry Geometry;
        public readonly double ScreenWidth, ScreenHeight;
        public readonly string GeometryKey;
        private MapBinding(DynamicMap map, RectTransform rect, MapGeometry geometry, double width, double height)
        {
            Map = map;
            Rect = rect;
            Geometry = geometry;
            ScreenWidth = width;
            ScreenHeight = height;
            GeometryKey = map.GetInstanceID() + ":" + rect.GetInstanceID() + ":" + geometry.MinX.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + geometry.MinZ.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + geometry.Width.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + geometry.Height.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
        public static bool TryCreate(int resolution, out MapBinding binding, out string failure)
        {
            binding = null;
            failure = "MapUnavailable";
            var map = SceneSingleton<DynamicMap>.i;
            if (map == null || map.mapImage == null || !ModelSettings.Positive(map.mapDisplayFactor))
                return false;
            failure = "MapHidden";
            if (!map.gameObject.activeInHierarchy || !map.mapImage.activeInHierarchy)
                return false;
            var rect = map.mapImage.GetComponent<RectTransform>();
            var image = map.mapImage.GetComponent<Image>();
            failure = "UnsupportedImageGeometry";
            if (rect == null || image == null || image.sprite == null || image.type != Image.Type.Simple || image.preserveAspect || image.sprite.packed)
                return false;
            failure = "UnsupportedMaterial";
            var material = image.material;
            if (material == null || material.shader == null || !material.HasProperty("_MainTex"))
                return false;
            var scale = material.GetTextureScale("_MainTex");
            var offset = material.GetTextureOffset("_MainTex");
            if (!MapSurfaceContract.SupportsMaterial(material == image.defaultMaterial, material.shader.name, scale.x, scale.y, offset.x, offset.y))
                return false;
            failure = "UnsupportedSpriteUv";
            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite);
            if (Math.Abs(uv.x) > 1e-5 || Math.Abs(uv.y) > 1e-5 || Math.Abs(uv.z - 1) > 1e-5 || Math.Abs(uv.w - 1) > 1e-5)
                return false;
            if (!ModelSettings.Positive(rect.rect.width) || !ModelSettings.Positive(rect.rect.height))
                return false;
            failure = "InvalidScreenGeometry";
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            Vector2 c = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
            double width = Vector2.Distance(a, c), height = Vector2.Distance(a, b);
            if (!ModelSettings.Positive(width) || !ModelSettings.Positive(height))
                return false;
            double factor = map.mapDisplayFactor;
            var g = new MapGeometry(rect.rect.xMin / factor, rect.rect.yMin / factor, rect.rect.width / factor, rect.rect.height / factor, resolution, resolution);
            binding = new MapBinding(map, rect, g, width, height);
            failure = null;
            return true;
        }
    }
}
