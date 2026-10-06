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
        /// <summary>设计基准：可见高度固定 10 个**世界单位**。
        /// 🔴 **2026-10-11 警告（量纲陷阱 —— A332 踩过一次，4 条自检红）**：
        /// 本常量与 <see cref="DesignWidth"/> 是**世界单位**（10 / 16:9 下 17.7778）；
        /// 而 <see cref="DesignPxW"/> / <see cref="DesignPxH"/> 是**设计像素**（1920 / 1080）。
        /// 两组名字只差 `Px` 两个字母、**量纲差 108 倍**（108 = `DesignPxH / DesignHeight`）。
        /// ⛔ **别把这一对传进任何 `*Px*` 形参**（`MenuDraw.SetPxSize` / `MenuDraw.Node` / `MenuDraw.ApplyPxRect`、
        /// `MainMenuRuntime.New` 的 `(wPx, hPx)` …）—— 那些口内部会**再 ÷108** ⇒ 1/108 做两遍，
        /// 尺寸静默变成 1/108、`rect` 元数据全错（**画面常常看不出**：那些节点的子件走绝对坐标、不读父 `rect`）。
        /// ✅ 要「1920×1080」就用 <see cref="DesignPxW"/> / <see cref="DesignPxH"/>，或直接写 px 字面量。
        /// ⚠️ **别给这两对改名** —— `Apply` 里的 `Camera.orthographicSize` / `VisibleWidth` / `Scale` /
        /// `ImageQuad.Create` / `SetAspect` 与 `DeckScene` / `ShellScene` 一大片在用。</summary>
        public const float DesignHeight = 10f;

        /// <summary>设计基准宽高比（16:9）—— 布局按这个宽度设计的</summary>
        public const float DesignAspect = 16f / 9f;

        /// <summary>设计宽度（**世界单位** = `DesignHeight × DesignAspect`，16:9 下 17.7778）。
        /// ⚠️ 量纲警告见 <see cref="DesignHeight"/>：它不是 px（px 那一对是 <see cref="DesignPxW"/> / <see cref="DesignPxH"/>）。</summary>
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

        /// <summary>把屏幕坐标（Input System 给的像素点）转成世界坐标 —— 拖拽要用
        /// <para>⚠️ **这是纯换算器，屏幕外的点照直外推**（调用点十来个：`Shell/PointerLayer` ·
        /// `Deck/DeckRuntime` · `Hand/CardInteraction` · `Board/BoardLayout` …）。
        /// ⛔ **别在这儿加「越界就归零」** —— 原版那道守卫只属于 `BattleManager.GetMousePerspectivePos`
        /// 一个函数，它的等价物是 `BattleDriver.WorldPointer()`（守卫落在那儿，见
        /// <see cref="IsInsideScreen"/>）。在这里拦会把船坞/卡组/外壳的拖拽一起改掉。</para></summary>
        public static Vector3 ScreenToWorld(Vector2 screenPos, Camera cam = null)
        {
            cam = cam != null ? cam : Cam;
            if (cam == null) return Vector3.zero;
            var w = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            w.z = 0f;
            return w;
        }

        /// <summary>🆕 **2026-10-14（A659）**：这个屏幕点**在屏幕内**吗 —— 就是原版
        /// `BattleManager.GetMousePerspectivePos` 那道守卫的条件。
        /// <para>判据（逐句，`d:/2/tools/decomp_full/BattleManager__GetMousePerspectivePos.c:26-27`）：
        /// `0.0 &lt;= f1 &amp;&amp; f1 &lt; Screen.width &amp;&amp; 0.0 &lt;= f2 &amp;&amp; f2 &lt; Screen.height`；
        /// **不满足时原版 `return Vector2.zero`**（`:40-44` 那一支）——
        /// ⚠️ 那个 `zero` 是**世界坐标**的零、**不是**「屏幕 (0,0) 换算过去的点」：
        /// 该函数返回的 8 字节就是 `Camera.ScreenToWorldPoint(...)` 的前两格（`:34-35`），
        /// 即「世界 (x,y)」；进不去那道 `if` 时就给世界零。
        /// ⇒ 我们的等价物 = `BattleDriver.WorldPointer()` 在屏幕外**返回 `Vector3.zero`**
        /// （我们的世界原点 = 屏幕中心，与「原版世界零」同义：都是「这个坐标系里的零」）。</para>
        /// <para>⚠️ **批处理下它恒真**：实测 `Screen.width/height = 640×480`（`_tmp_view` 的
        /// `BattleScene` 日志里自检自己打的「前提：`Screen.width(640) != Screen.height(480)`」），
        /// 而 `Mouse.current == null` ⇒ 屏幕点恒 `(0,0)` ⇒ 在屏幕内 ⇒ **自检一条都不受影响**。</para>
        /// <para>⚠️ **不要拿它替换 `Hand/CardInteraction.PointerWorldSafe` 的阈值启发式**
        /// （那条要单独一笔账）：两者治的是同一族症状，但一个是在屏幕空间判、一个是在世界空间判。</para></summary>
        public static bool IsInsideScreen(Vector2 screenPos)
        {
            return screenPos.x >= 0f && screenPos.x < Screen.width
                && screenPos.y >= 0f && screenPos.y < Screen.height;
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

        // ============================================================ 原版「像素矩形」↔ 世界坐标
        //
        // 原版是 UGUI，所有界面参数都是**1920×1080 设计像素、左上为原点、y 向下**（`m_AnchoredPosition` 那一套）。
        // 我们全线是世界空间 mesh ⇒ 到处都要做这一次换算。**判据只留这一份**（CLAUDE.md §三：
        // 「两处写同一条规则 = 迟早不一致」，`MainMenuRuntime.Center` 已经转发到这里）。
        //
        // 换算关系：可见高度固定 10 个世界单位（`DesignHeight`）= 1080 px ⇒ **1 px = 1/108 世界单位**。

        /// <summary>设计像素（1920×1080）</summary>
        public const float DesignPxW = 1920f, DesignPxH = 1080f;

        /// <summary>像素长度 → 世界长度。**只有这一个换算**，别在别处再乘 108。</summary>
        public static float Px(float px) { return px / (DesignPxH / DesignHeight); }

        /// <summary>原版像素点（左上原点、y 向下）→ 世界坐标（屏幕中心为原点）。</summary>
        public static Vector3 FromPixel(float xPx, float yPx)
            => ToWorld(xPx / DesignPxW, 1f - yPx / DesignPxH);

        /// <summary>原版像素矩形 (x1,y1)-(x2,y2)（左上原点）的**中心** → 世界坐标。</summary>
        public static Vector3 RectCenter(float x1, float y1, float x2, float y2)
            => FromPixel((x1 + x2) * 0.5f, (y1 + y2) * 0.5f);

        /// <summary>世界坐标 → 原版像素点（**`FromPixel` 的逆**）。命中判定要用它。
        /// 🔴 2026-09-23 收口：`RewardsScene.PxOf/PxYOf` 与 `PointerLayer` 都转发到这里 ——
        ///    以前那条换算（`x*108+960` / `540−y*108`）在自检里另外写了一份，正是
        ///    「两处写同一条规则 = 迟早不一致」那一类。**别在别处再乘 108**。</summary>
        public static Vector2 ToPixel(Vector3 world)
        {
            float k = DesignPxH / DesignHeight;
            return new Vector2(world.x * k + DesignPxW * 0.5f, DesignPxH * 0.5f - world.y * k);
        }

        /// <summary>世界 x → 画布像素 x（见 `ToPixel`）。</summary>
        public static float PxX(float worldX) { return worldX * (DesignPxH / DesignHeight) + DesignPxW * 0.5f; }

        /// <summary>世界 y → 画布像素 y（**y 是反的**）。
        /// 🔴 2026-09-23 踩过：拿 `PxX` 的式子去量 y，得出了「节点整体偏下 163px」的**假警报**。</summary>
        public static float PxY(float worldY) { return DesignPxH * 0.5f - worldY * (DesignPxH / DesignHeight); }
    }
}
