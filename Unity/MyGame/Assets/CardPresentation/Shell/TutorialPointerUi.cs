// TutorialPointerUi.cs — 菜单侧 FTUE 的**手指指针 + 文字气泡**（原版 `TutorialPointer` + `TutorialText`）。
//
// ============================ 出处（第一权威 = prefab / 反编译，全部本地现读）============================
// · prefab（`bundle_generalgamewindows_assets_all`，`python 工具/menu_dump.py … "TutorialPointerMenu" --md` 现读）：
//     | 节点 | 矩形（设计 px） | 组件 | sprite | 其它 |
//     |---|---|---|---|---|
//     | `TutorialPointerMenu`（根） | 1920×1080 | `TutorialPointer` | — | 整屏锚点 |
//     | `Glow (1)` | 884.27,464.27 → 1035.73,615.73（151.451²，居中） | `Image` + `UIBorderGlow` | `Border Circles FX` 128² | `m_Color (1,1,1,1)` · `animTime 1` / `animDelay 0` / `playOnEnable 0` |
//     | `Pointer` | 921,531 → 1021,631（100²） | `Image` | `Cursor_Space Marine_01` 128² | **pivot (0.39,0.91)** · `preserveAspect` |
//     | `Pointer/light` | 同父 | `Image` | `Cursor_Space Marine_01Glow` 128² | `m_Color (1,0.946,0.505,1)` · `preserveAspect` |
//   序列化字段（4 个 prefab 全读过）：`waitBeforeClick = 0.5` · `waitAfterClick = 0.5` ·
//     `clickScaleFactor = 0.699999988079071`（**4 个 prefab 逐值相同**）；Menu/Wildcards 另有
//     `useLocalPositions = 1` · `xOffset/yOffset = (0,0)` · `moveDuration = 0`；
//     Base/Combat 是 `useLocalPositions = 0` · `(2,-2)` · `0.7`。
//   `Glow (1)` 只在 **Menu** 那一份里；`clickAnim` 四处**全 null**。
// · 文字气泡（`TutorialPointerWildcards` 的子树 + 独立 prefab `TutorialText`，实读）：
//     `Generic Popup Background` **500×200**（`40k_popup` · `m_Border (169,160,169,160)` · `m_Rect 359×336` ·
//       `m_Type = Sliced`；出厂 **active = false** —— 由 `SetTutorialText` 打开）
//       ├ `Mask` **479.73×180.75**（`40k_popup` 同图 + `Mask.showGraphic = 0`）
//       │   └ `Background fill`（`40k_popup_texture` 128² · `Tiled` · `ppuMul = 2.0`）
//       └ `TutorialText` **456.5×157.9** · TMP `fs 36` · `auto[18~40]` · `base 36` · 白 · Center/Middle · 折行
//     它在 prefab 里挂在根下、`anchor (0.5,0.5)` · `anchoredPosition (319.789, −230)`
//       ⇒ 相对**根的中心**（= 指针的出厂位置，见下）右下 319.789 / 230 px。
// · 行为（`d:/2/tools/decomp_full/TutorialPointer__*.c` + `TutorialPointer._ClickInPosition_d__23__MoveNext.c`）：
//     · `StartPointer(action, startPos, endPos, orientation = right)`：`SetActive(true)` → `animCount++` →
//       `activeAnim = true` → 按 `TutorialPointerActions` 起协程（`clickInPosition = 0` / `clickAndHold = 1` /
//       `clickAndDrag = 2`）。
//     · `ClickInPosition`：**loop**{ `ShowStartingPointer` → `WaitForSeconds(waitBeforeClick)` →
//       `ShowClickedPointer` → `WaitForSeconds(waitAfterClick)` }，每轮校验 `animCount` 未变。
//     · `ShowStartingPointer`：scale = **1**（`Vector3.one`）· 旋转 = 0（right）/ 180（left）·
//       位置 = `clickPos + (±xOffset, yOffset)`（**left 时 xOffset 取反**）·
//       `bgLight`（= `light` 子图）**alpha 置 0** · `pointer.SetActive(showPointer)`。
//     · `ShowClickedPointer`：scale = **`clickScaleFactor`**（0.7）· `bgLight` alpha **置 1** ·
//       `clickAnim` 非空则 `Instantiate(clickAnim, clickPos)`（**本 prefab 是 null ⇒ 不实例化**）。
//     · `SetTutorialText(text, showPointer)`：`bgTutorialText.SetActive(true)` → `tutorialText.text = text` →
//       `pointer.gameObject.SetActive(showPointer)` → `glowObj.gameObject.SetActive(showPointer)`。
//     · `GetAdjustedPos`：`clickPos + (xOffset, yOffset)`（left 时 x 取反）—— 坐标系 = 指针根的 local 或 world
//       （`useLocalPositions` 决定）。
// · 🔴 **根挂在哪**（`TutorialManager__AddPointerAction.c`，逐句现读）：
//     `pointerRoot.transform.SetParent(target.transform)` → `localPosition = (0,0)` → `localScale = (1,1,1)` →
//     `SetupCursor(army)` → `StartPointer(clickInPosition, clickPos, endPos, right)`。
//     ⇒ **指针根被挂到目标控件下面、中心对准目标的 pivot**；`clickPos` 是原点（静态 `Vector3.zero`），
//       `xOffset/yOffset` 对 Menu 是 (0,0) ⇒ **手指尖正好落在目标 pivot 上**。
//   我们这一套没有 uGUI 父矩形 ⇒ **不挂到目标下**（会继承目标父链的缩放），改成「按目标的设计 px 位置摆」，
//     数学等价（Root 中心 = 目标 pivot = 手指尖）。⚠️ 这是**等价实现**，⛔ 不写成「照原版挂父」。
//
// · 🔴 **图（2026-10-19 订正：已经导进来了）**：`Cursor_Space Marine_01` / `…01Glow` / `Border Circles FX`
//   三张源图在 `d:/2/新解包资源/assets_full/bundle_armycursors_assets_all/Texture2D/` 与
//   `bundle_generalgamewindows_assets_all/Texture2D/`；`工具/import_original_art.py` 的 `MENU_TEXTURE_IMAGES`
//   已收录（空格→下划线落成 `Cursor_Space_Marine_01[Glow].png` / `Border_Circles_FX.png`，128²），
//   **`.meta` 已在盘上**（GUID `bdeec8e2…` / `d8f1eb7d…` / `af52d0da…`）。
//   兜底仍然保留：取不到时**只出声、不画**（手指那一格留空），并把缺的图名记进 `MissingArt`（自检读口）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `TutorialPointer`（菜单侧那一份 = `TutorialPointerMenu`）的等价实现。</summary>
    public class TutorialPointerUi : MonoBehaviour
    {
        // ============================================================ 原版实读参数（prefab 逐值）
        public const float PointerW = 100f, PointerH = 100f;
        public const float PointerPivotX = 0.39f;      // ← 从**左上**量 = 0.39；从底量 = 0.91
        public const float PointerPivotYFromTop = 0.09f;
        public const float GlowSize = 151.451f;
        public const float WaitBeforeClick = 0.5f;
        public const float WaitAfterClick = 0.5f;
        public const float ClickScale = 0.699999988079071f;
        /// <summary>`light` 子图的 `m_Color`（实读 (1,0.946,0.505,1)）。</summary>
        public static readonly Color LightTint = new Color(1f, 0.946f, 0.505f, 1f);

        // 气泡（`TutorialText` prefab 实读）
        public static readonly Vector2 BubbleOffset = new Vector2(319.789f, 230f); // y 向下（prefab 的 apos.y = −230）
        public const float BubbleW = 500f, BubbleH = 200f;
        public static readonly Vector4 BubbleBorder = new Vector4(169f, 160f, 169f, 160f);
        public const float BubbleTexW = 359f, BubbleTexH = 336f;
        public const float TextFont = 36f, TextAutoMin = 18f, TextAutoMax = 40f, TextAutoBase = 36f;

        // 队列：指针在聚光灯之上（本链自成一档 4110–4119）。
        public const int QGlow = 4110, QPointer = 4112, QLight = 4113, QBubble = 4114, QBubbleFill = 4115, QBubbleText = 4116;

        public const string ArtPointer = "Cursor_Space_Marine_01";
        public const string ArtPointerGlow = "Cursor_Space_Marine_01Glow";
        public const string ArtBorderGlow = "Border_Circles_FX";
        public const string ArtBubble = "40k_popup";
        public const string ArtBubbleFill = "40k_popup_texture";

        /// <summary>指针 tier（= prefab 名后缀；**本链只用 `Menu`**，其余留给别的线）。</summary>
        public enum Tier { Base, Combat, Menu, Wildcards }

        /// <summary>缺图（自检读口 —— 非空就是「有件根本没画」）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        Tier _tier = Tier.Menu;
        Transform _anchor, _pointer, _bubble;
        ImageQuad _pointerImg, _lightImg, _glowImg;
        float _phase;          // 0 = 起手（scale 1 / light 0），1 = 点击态（scale 0.7 / light 1）
        float _phaseT;
        bool _visible;
        bool _showPointer = true;

        // ---- 自检 / 诊断读口
        public bool Visible { get { return _visible; } }
        public Tier CurrentTier { get { return _tier; } }
        public Transform AnchorNode { get { return _anchor; } }
        public Transform PointerNode { get { return _pointer; } }
        public Transform BubbleNode { get { return _bubble; } }
        public Vector2 LastPointPx { get; private set; }
        public bool PointerImageDrawn { get { return _pointerImg != null; } }
        public bool GlowDrawn { get { return _glowImg != null; } }
        public bool BubbleShown { get { return _bubble != null && _bubble.gameObject.activeSelf; } }

        public static TutorialPointerUi Create(Transform parent, Tier tier)
        {
            var go = new GameObject("TutorialPointer" + tier);
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<TutorialPointerUi>();
            p._tier = tier;
            return p;
        }

        /// <summary>在**设计像素坐标**（左上原点）处显示：`posPx` = 手指尖要落的那一点（= 目标控件 pivot）。
        /// <para>`text` 非空 ⇒ 同时显示文字气泡（原版 `SetTutorialText`）。`showPointer = false` ⇒ 只留文字
        /// （原版 `SetTutorialText` 的 `showPointer` 形参就是这一档）。</para></summary>
        public void Show(Vector2 posPx, string text, bool showPointer = true)
        {
            LastPointPx = posPx;
            _showPointer = showPointer;
            Rebuild(posPx, text);
            _visible = true;
            _phase = 0f; _phaseT = 0f;
            ApplyPhase();
        }

        /// <summary>只换文字（不重建几何）。</summary>
        public void SetText(string text)
        {
            if (_bubble == null)
            {
                if (!string.IsNullOrEmpty(text)) Show(LastPointPx, text, _showPointer);
                return;
            }
            var lb = _bubble.GetComponentInChildren<Label>(true);
            if (lb != null) lb.SetText(text);
            _bubble.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void Hide()
        {
            _visible = false;
            if (_anchor != null) _anchor.gameObject.SetActive(false);
        }

        /// <summary>每帧推进「起手 ↔ 点击」两拍（原版是协程 + `WaitForSeconds`，我们这版按 dt 累计）。
        /// ⚠️ **不建 DOTween / 不建协程**：本仓没有 tween 引擎，而协程在批处理下不会跑（同 `BlinkGraphic` 的口径）。</summary>
        public void Tick(float dt)
        {
            if (!_visible || _pointer == null) return;
            _phaseT += dt;
            if (_phase <= 0.5f)
            {
                if (_phaseT >= WaitBeforeClick) { _phase = 1f; _phaseT = 0f; ApplyPhase(); }
            }
            else
            {
                if (_phaseT >= WaitAfterClick) { _phase = 0f; _phaseT = 0f; ApplyPhase(); }
            }
        }

        void ApplyPhase()
        {
            if (_pointer == null) return;
            bool clicked = _phase >= 0.5f;
            _pointer.localScale = Vector3.one * (clicked ? ClickScale : 1f);
            if (_lightImg != null)
                _lightImg.SetTint(new Color(LightTint.r, LightTint.g, LightTint.b, clicked ? 1f : 0f));
        }

        // ============================================================ 建树
        void Rebuild(Vector2 pos, string text)
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _anchor = _pointer = _bubble = null;
            _pointerImg = _lightImg = _glowImg = null;

            // 根：零尺寸锚点，中心 = 手指尖（= 原版「根挂到目标、中心对准目标 pivot」的等价）
            _anchor = MenuDraw.Node(transform, "TutorialPointer" + _tier, new PxRect(pos.x, pos.y, pos.x, pos.y));
            _anchor.gameObject.SetActive(true);

            // `Glow (1)`（**只有 Menu 那一份 prefab 有**）。⚠️ 原版 `TutorialPointer` 的四个方法体
            // **一次都没读 `glowObj`** ⇒ 它恒停在根的局部原点（= 手指尖）。我们照此摆在指针处。
            if (_tier == Tier.Menu)
            {
                var gr = Centered(pos, GlowSize);
                _glowImg = MenuDraw.Rect(_anchor, Art(ArtBorderGlow, "`Glow (1)` 的光圈"), gr, "Glow (1)", QGlow,
                                         null, false, MenuDraw.NoClip);
            }

            // `Pointer`：pivot (0.39, 0.91) ⇒ 从左上量 (0.39, 0.09) ⇒ 矩形左上 = 指尖 − (39, 9)
            var pr = new PxRect(pos.x - PointerW * PointerPivotX, pos.y - PointerH * PointerPivotYFromTop,
                                pos.x + PointerW * (1f - PointerPivotX),
                                pos.y + PointerH * (1f - PointerPivotYFromTop));
            // 🔴 **节点原点必须落在指尖上**（原版 `Pointer` 的 pivot 就是指尖 ⇒ 点击时按 0.7 缩放**指尖不动**）。
            //   本仓 `MenuDraw.Node` 一律把 pivot 写成 (0.5,0.5)，照它缩放会让指尖往里缩 ~12.7px
            //   ⇒ 这里不用 `MenuDraw.Node`，自己建一个**原点在指尖**的裸节点，子图仍按绝对矩形摆。
            _pointer = new GameObject("Pointer", typeof(RectTransform)).transform;
            _pointer.SetParent(_anchor, false);
            _pointer.localPosition = Vector3.zero;        // `_anchor` 的原点就是指尖
            _pointerImg = MenuDraw.Rect(_pointer, Art(ArtPointer, "`Pointer` 的手形图"), pr, "Image", QPointer,
                                        null, true, MenuDraw.NoClip);
            _lightImg = MenuDraw.Rect(_pointer, Art(ArtPointerGlow, "`Pointer/light` 的高光图"), pr, "light", QLight,
                                      LightTint, true, MenuDraw.NoClip);
            _pointer.gameObject.SetActive(_showPointer);
            ApplyPhase();

            // `Generic Popup Background` + `TutorialText`（=`SetTutorialText` 那一支）
            if (!string.IsNullOrEmpty(text)) BuildBubble(pos, text);
        }

        void BuildBubble(Vector2 pos, string text)
        {
            float cx = pos.x + BubbleOffset.x, cy = pos.y + BubbleOffset.y;
            float x1 = cx - BubbleW * 0.5f, y1 = cy - BubbleH * 0.5f;
            // 夹进屏幕（原版气泡是**固定位**，不夹；我们这份会跟着目标跑 ⇒ 越界就翻到另一侧，避免画到屏外）
            if (x1 + BubbleW > LayoutSpace.DesignPxW - 8f) x1 = LayoutSpace.DesignPxW - 8f - BubbleW;
            if (x1 < 8f) x1 = 8f;
            if (y1 + BubbleH > LayoutSpace.DesignPxH - 8f) y1 = LayoutSpace.DesignPxH - 8f - BubbleH;
            if (y1 < 8f) y1 = 8f;
            var br = new PxRect(x1, y1, x1 + BubbleW, y1 + BubbleH);

            _bubble = MenuDraw.Node(_anchor, "Generic Popup Background", br);
            MenuDraw.Nine(_bubble, Art(ArtBubble, "`Generic Popup Background` 的九宫底"), br, BubbleBorder,
                          BubbleTexW, BubbleTexH, QBubble, null, true, "Image", null, MenuDraw.NoClip);
            // `Mask` / `Background fill`：相对气泡内缩（prefab 实读：10.4 / 9.44 / 10.4 / 9.8）
            var fillR = new PxRect(br.x1 + 10.4f, br.y1 + 9.44f, br.x2 - 10.4f, br.y2 - 9.8f);
            // 平铺尺寸：prefab 是 `40k_popup_texture` 128² + `m_PixelsPerUnitMultiplier = 2` ⇒ 实铺 64 px
            MenuDraw.Tiled(_bubble, Art(ArtBubbleFill, "`Background fill` 的平铺底"), fillR, 64f, QBubbleFill,
                           "Background fill", null, MenuDraw.NoClip);
            // `TutorialText`：相对气泡 23 / 21 → 479.5 / 179（prefab 实读 (1052.75,691.05)-(1509.25,848.95)）
            var tr = new PxRect(br.x1 + 23f, br.y1 + 21f, br.x1 + 479.5f, br.y1 + 179f);
            MenuDraw.TextBox(_bubble, tr, text, Color.white, "TutorialText", TextFont, TextAutoMin, QBubbleText,
                             TextAutoMax, TextAutoBase, MenuDraw.NoClip);
        }

        // ============================================================ 取图（不静默）
        Texture2D Art(string name, string what)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name))
            {
                MissingArt.Add(name);
                Debug.LogWarning("[MenuTutorial] 指针图取不到：`" + name + "`（" + what + "）⇒ **这一件不画**。"
                               + "原图在 `d:/2/新解包资源/assets_full/bundle_armycursors_assets_all/Texture2D/`"
                               + "（`Cursor_Space Marine_01[Glow]`）与 `bundle_generalgamewindows_assets_all`"
                               + "（`Border Circles FX`）；`工具/import_original_art.py` 的 `MENU_TEXTURE_IMAGES` 里已有它们"
                               + "⇒ 这里报错 = 落盘/导入丢了，去跑 `--only-menu` 重导。⛔ 不拿别的图顶。");
            }
            return t;
        }

        static PxRect Centered(Vector2 c, float side)
        {
            return new PxRect(c.x - side * 0.5f, c.y - side * 0.5f, c.x + side * 0.5f, c.y + side * 0.5f);
        }
    }
}
