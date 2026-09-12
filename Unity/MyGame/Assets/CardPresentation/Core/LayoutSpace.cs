// LayoutSpace.cs — 分辨率无关的坐标基准
//
// 用户要求：**1080p / 2K 这些要能自由切换**。所以布局里不能出现「像素」和「固定世界坐标」，
// 只能出现**归一化坐标**（0–1），由这里换算成世界坐标。
//
// 为什么不用 uGUI 的 CanvasScaler：
//   卡牌要跟特效（世界空间的粒子）严丝合缝地对齐 —— 走 UI 的话每次都要在
//   RectTransform 和世界坐标之间来回换算，粒子还会被 UI 的排序规则缠住。
//   世界空间 + 自适应相机只要一处换算，而且能顺便处理超宽屏。
//
// 坐标约定：
//   · 相机是**正交**的，看 -Z 方向，屏幕中心 = 世界原点
//   · **可见高度恒等于 DesignHeight**（10 世界单位），与分辨率无关 —— 这是基准
//   · 可见宽度 = DesignHeight × 宽高比（16:9 → 17.78，21:9 → 23.3，4:3 → 13.3）
//   · 归一化坐标 (0,0) = 左下角，(1,1) = 右上角，都相对**可见区域**
//
// 窄屏（4:3）可见宽度会小于设计宽度，手牌会挤出画面 —— 所以有一个 `Scale`：
// 可见宽度不够时整体缩，够的时候保持 1（多出来的宽度就是留白）。
using UnityEngine;

namespace CardPresentation
{
    public static class LayoutSpace
    {
        /// <summary>设计基准：可见高度固定 10 个世界单位</summary>
        public const float DesignHeight = 10f;

        /// <summary>设计基准宽高比（16:9）—— 布局按这个宽度设计的</summary>
        public const float DesignAspect = 16f / 9f;

        public static float DesignWidth { get { return DesignHeight * DesignAspect; } }

        /// <summary>当前相机</summary>
        public static Camera Cam;

        /// <summary>让相机进入「正交 + 高度固定」的状态。切分辨率时调用。</summary>
        public static void Apply(Camera cam)
        {
            Cam = cam;
            cam.orthographic = true;
            cam.orthographicSize = DesignHeight * 0.5f;
            cam.transform.position = new Vector3(0f, 0f, -20f);
            cam.transform.rotation = Quaternion.identity;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 200f;
        }

        public static float VisibleHeight { get { return DesignHeight; } }

        public static float VisibleWidth
        {
            get
            {
                float aspect = Cam != null ? Cam.aspect : DesignAspect;
                return DesignHeight * Mathf.Max(0.1f, aspect);
            }
        }

        /// <summary>窄屏时整体缩放系数。可见宽度 ≥ 设计宽度时为 1。</summary>
        public static float Scale
        {
            get { return Mathf.Min(1f, VisibleWidth / DesignWidth); }
        }

        /// <summary>归一化坐标 → 世界坐标（z = 0）。x/y 都是 0–1，相对可见区域。</summary>
        public static Vector3 ToWorld(float x01, float y01)
        {
            return new Vector3((x01 - 0.5f) * VisibleWidth,
                               (y01 - 0.5f) * VisibleHeight,
                               0f);
        }

        /// <summary>世界坐标 → 归一化坐标（放牌、判落点时用）</summary>
        public static Vector2 ToNormalized(Vector3 world)
        {
            return new Vector2(world.x / VisibleWidth + 0.5f,
                               world.y / VisibleHeight + 0.5f);
        }

        /// <summary>把屏幕坐标（Input System 给的像素点）转成世界坐标 —— 拖拽要用</summary>
        public static Vector3 ScreenToWorld(Vector2 screenPos, Camera cam = null)
        {
            cam = cam != null ? cam : Cam;
            if (cam == null) return Vector3.zero;
            var w = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            w.z = 0f;
            return w;
        }

        /// <summary>一个尺寸（设计单位）在当前分辨率下的实际尺寸</summary>
        public static Vector2 Size(float w, float h)
        {
            float s = Scale;
            return new Vector2(w * s, h * s);
        }

        /// <summary>诊断用：当前分辨率下的可见范围</summary>
        public static string Describe()
        {
            return $"可见 {VisibleWidth:F2} × {VisibleHeight:F2}（设计 {DesignWidth:F2} × {DesignHeight:F2}）"
                 + $"  缩放 {Scale:F2}  宽高比 {(Cam != null ? Cam.aspect : 0f):F3}";
        }
    }
}
