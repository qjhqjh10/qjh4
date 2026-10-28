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
//       `ShowClickedPointer` → `WaitForSeconds(waitAfterClick)` }，每轮校验 `animCount` 未变。（**2 拍 / 1.0s**）
//     · `ClickAndHold`：**4 拍 / 2.0s** —— 起手 0.5 → 点击 0.5 → **再等 0.5 并 `Instantiate(clickAnim)`** →
//       **再等 0.5 并 `Instantiate(clickAnim)`** → 回首（`_ClickAndHold_d__24__MoveNext.c` state0→4 逐拍现读）。
//       `clickAnim` 为 null 只抹掉那两下动画，**等待仍在** ⇒ 点击态占 1.5s（ClickInPosition 只占 0.5s）。
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

        /// <summary>原版 `TutorialPointerActions`（值照 dump：`clickInPosition = 0` / `clickAndHold = 1` / `clickAndDrag = 2`）。
        /// <para>判据（伪码可读 + 常量实读）：
        /// · **Hold = 4 拍**（`TutorialPointer._ClickAndHold_d__24__MoveNext.c` state0→4：
        ///   起手 `waitBeforeClick`(0.5) → 点击 `waitAfterClick`(0.5) → 第 3 拍(0.5) → 第 4 拍(0.5) → 回首），
        ///   一轮 **2.0s**、其中点击态 **1.5s**。第 3/4 拍原版各 `Instantiate(clickAnim)` 一次，而
        ///   **本 prefab 的 `clickAnim = null` ⇒ 那两拍只多等、不实例化**。
        ///   🔴 **2026-10-27（B88-E7 · 铁律 5）就地订正**：本文原来写「Hold 与 ClickInPosition 可见行为相同，
        ///   差别只在 clickAnim」——**不成立**：`clickAnim` 为 null 只抹掉了「多余的那两下动画」，
        ///   **那两拍等待还在**（一轮 2.0s vs 1.0s、点击态占比 1.5s vs 0.5s）。
        /// · ClickInPosition = **2 拍**（`d__23` state0→2）：起手 0.5 → 点击 0.5 → 循环。
        /// · Drag：起始指针 → 0.5s → **点击态停在起点** → `waitAfterClick × 0.5`（=0.25s，常量 `DAT_1834b2bb4 = 0.5`）
        ///   → `DOMove(endPos, moveDuration)`（Menu/Wildcards 的 `moveDuration = 0` ⇒ **瞬移**）
        ///   → 等 `moveDuration + 0.1`（`DAT_1834b2dc4 = 0.1`）→ 回到起点、循环。</para></summary>
        public enum PointerAction { ClickInPosition = 0, ClickAndHold = 1, ClickAndDrag = 2 }

        /// <summary>一轮几拍（`ClickInPosition = 2` / `ClickAndHold = 4`；Drag 走自己的 `TickDrag`）。
        /// 判据 = 两个协程状态机的状态数（见 <see cref="PointerAction"/> 的 doc）。</summary>
        public static int BeatsOf(PointerAction a)
        {
            return a == PointerAction.ClickAndHold ? 4 : (a == PointerAction.ClickAndDrag ? 4 : 2);
        }

        /// <summary>一轮总时长（秒）—— 自检判据用：`ClickInPosition = 1.0` / `ClickAndHold = 2.0`。
        /// ⚠️ Drag 的那三拍时序不同（第 2 拍是 `waitAfterClick × 0.5`），本属性只覆盖前两档。</summary>
        public static float LoopSecondsOf(PointerAction a)
        {
            return WaitBeforeClick + (BeatsOf(a) - 1) * WaitAfterClick;
        }

        // ---- 自检读口（相位推进）
        public float Phase { get { return _phase; } }
        public bool InClickedPhase { get { return _phase >= 0.5f; } }

        /// <summary>缺图（自检读口 —— 非空就是「有件根本没画」）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        Tier _tier = Tier.Menu;
        Transform _anchor, _pointer, _bubble;
        ImageQuad _pointerImg, _lightImg, _glowImg;
        ImageQuad _bubbleBg, _bubbleFill;      // 🆕 淡出用（见 SetFade）
        float _fade = 1f;                      // 🆕 整组不透明度（战役页滚动淡出，= 原版 TutorialHidePointerInCampaign）
        PointerAction _action = PointerAction.ClickInPosition;
        Vector2 _startPx, _endPx;
        string _bubbleText;
        float _dragWait;       // Drag 的第三段等待（0.1s）
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
        public void Show(Vector2 posPx, string text, bool showPointer = true,
                         PointerAction action = PointerAction.ClickInPosition, Vector2? endPx = null)
        {
            LastPointPx = posPx;
            _showPointer = showPointer;
            _action = action;
            _startPx = posPx;
            _endPx = endPx ?? posPx;
            _bubbleText = text;
            _dragWait = 0f;
            Rebuild(posPx, text);
            _visible = true;
            _phase = 0f; _phaseT = 0f;
            _fade = 1f;
            ApplyFade();
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
            if (_action == PointerAction.ClickAndDrag)
            {
                TickDrag();
                return;
            }
            // ClickInPosition = 2 拍 / ClickAndHold = 4 拍（判据 = 两个协程的状态数，见 PointerAction 的 doc）。
            // 第 0 拍等 `waitBeforeClick`，其后每一拍都等 `waitAfterClick`；`_phase` 为 0 时是起手、>0 是点击态。
            int beats = BeatsOf(_action);
            if (_phase <= 0.5f)
            {
                if (_phaseT >= WaitBeforeClick) { _phase = 1f; _phaseT = 0f; ApplyPhase(); }
            }
            else if (_phaseT >= WaitAfterClick)
            {
                _phaseT = 0f;
                _phase = (_phase < beats - 1) ? _phase + 1f : 0f;
                ApplyPhase();
            }
        }

        /// <summary>Drag 的四段：起手(0.5) → 点击态停起点(0.25) → 瞬移到终点(`moveDuration`=0) → 等 0.1 → 回起点。</summary>
        void TickDrag()
        {
            switch ((int)_phase)
            {
                case 0:   // 起手（起点）
                    if (_phaseT >= WaitBeforeClick) { _phase = 1f; _phaseT = 0f; ApplyPhase(); }
                    break;
                case 1:   // 点击态（仍停起点）
                    if (_phaseT >= WaitAfterClick * 0.5f)
                    {
                        _phase = 2f; _phaseT = 0f;
                        MoveTo(_endPx);          // moveDuration = 0 ⇒ 瞬移（prefab 实读）
                        _phase = 3f; _dragWait = 0.1f;   // DAT_1834b2dc4 = 0.1
                    }
                    break;
                case 3:   // 终点停 0.1s → 回起点、循环
                    if (_phaseT >= _dragWait)
                    {
                        _phase = 0f; _phaseT = 0f;
                        MoveTo(_startPx);
                        ApplyPhase();
                    }
                    break;
                default:
                    _phase = 0f; _phaseT = 0f;
                    break;
            }
        }

        /// <summary>把整组指针搬到新位置（重建子件；气泡文案保留）。</summary>
        void MoveTo(Vector2 px)
        {
            LastPointPx = px;
            Rebuild(px, _bubbleText);
            ApplyFade();
            ApplyPhase();
        }

        /// <summary>自检读口：当前动作 / 当前点（拖拽两段各一个点）。</summary>
        public PointerAction Action { get { return _action; } }
        public Vector2 StartPointPx { get { return _startPx; } }
        public Vector2 EndPointPx { get { return _endPx; } }

        void ApplyPhase()
        {
            if (_pointer == null) return;
            bool clicked = _phase >= 0.5f;
            _pointer.localScale = Vector3.one * (clicked ? ClickScale : 1f);
            if (_lightImg != null)
                _lightImg.SetTint(new Color(LightTint.r, LightTint.g, LightTint.b, (clicked ? 1f : 0f) * _fade));
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
            _pointerImg = MenuDraw.Rect(_pointer, Art(_cursorArt, "`Pointer` 的手形图"), pr, "Image", QPointer,
                                        null, true, MenuDraw.NoClip);
            _lightImg = MenuDraw.Rect(_pointer, Art(_glowArt, "`Pointer/light` 的高光图"), pr, "light", QLight,
                                      LightTint, true, MenuDraw.NoClip);
            _pointer.gameObject.SetActive(_showPointer);
            ApplyPhase();

            // `Generic Popup Background` + `TutorialText`（=`SetTutorialText` 那一支）
            if (!string.IsNullOrEmpty(text)) BuildBubble(pos, text);
        }

        /// <summary>🆕 **2026-10-25（代理 G 执行）**：整组不透明度（0..1）。
        /// **判据 = 原版 `TutorialHidePointerInCampaign.Update`**（伪码可读 + 常量实读）：
        /// <code>
        /// float t = Clamp01(scrollRect.normalizedPosition * alphaMultiplier);   // alphaMultiplier = 12.5f（.ctor 0x41480000）
        /// canvasGroup.alpha = Clamp01(t * -1f + 1f);                            // DAT_1834b2bc8 = -1, DAT_1834b2bb8 = 1
        /// </code>
        /// 即 **alpha = 1 − clamp01(normalized × 12.5)**（滚过 8% 就全隐）。
        /// ⚠️ 我们只有 `ImageQuad`、没有 `CanvasGroup` ⇒ 逐图设 tint alpha（文字气泡的 Label 不在其中，见 `ApplyFade`）。</summary>
        public void SetFade(float alpha01)
        {
            float a = Mathf.Clamp01(alpha01);
            if (Mathf.Abs(a - _fade) < 1e-4f) return;
            _fade = a;
            ApplyFade();
            ApplyPhase();          // light 的 alpha 是「相位 × fade」，要一起重算
        }

        public float Fade { get { return _fade; } }

        void ApplyFade()
        {
            if (_pointerImg != null) _pointerImg.SetTint(new Color(1f, 1f, 1f, _fade));
            if (_bubbleBg != null) _bubbleBg.SetTint(new Color(1f, 1f, 1f, _fade));
            if (_bubbleFill != null) _bubbleFill.SetTint(new Color(1f, 1f, 1f, _fade));
            // `Glow (1)` 原图本来就半亮，这里只乘 fade
            if (_glowImg != null) _glowImg.SetTint(new Color(1f, 1f, 1f, _fade));
            // ⚠️ 气泡里的 `TutorialText` 是 TMP（`Label`）—— 本工程 `Label` 只有 `SetColor`，没有独立 alpha 口；
            //    指针淡出时**气泡文字不淡**（如实记：原版 CanvasGroup 是整组一起淡）。
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
            var bgGo = MenuDraw.Nine(_bubble, Art(ArtBubble, "`Generic Popup Background` 的九宫底"), br, BubbleBorder,
                                     BubbleTexW, BubbleTexH, QBubble, null, true, "Image", null, MenuDraw.NoClip);
            _bubbleBg = bgGo != null ? bgGo.GetComponent<ImageQuad>() : null;
            // `Mask` / `Background fill`：相对气泡内缩（prefab 实读：10.4 / 9.44 / 10.4 / 9.8）
            var fillR = new PxRect(br.x1 + 10.4f, br.y1 + 9.44f, br.x2 - 10.4f, br.y2 - 9.8f);
            // 平铺尺寸：prefab 是 `40k_popup_texture` 128² + `m_PixelsPerUnitMultiplier = 2` ⇒ 实铺 64 px
            var fillGo = MenuDraw.Tiled(_bubble, Art(ArtBubbleFill, "`Background fill` 的平铺底"), fillR, 64f, QBubbleFill,
                                        "Background fill", null, MenuDraw.NoClip);
            _bubbleFill = fillGo != null ? fillGo.GetComponent<ImageQuad>() : null;
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

        // ============================================================ 按阵营换指针图（B94_S3 · 2026-10-29）
        // 🔴 **追加在类尾**（本文件在账里被按 `文件:行号` 引用 198/231/206/274/167/284 ⇒ 插中间会整体漂移）。
        // 判据 = 原版 `TutorialPointer.SetupCursor(cardArmy)` → `CursorByArmySO.GetCursor(army, out cursor, out highlight)`
        // （`decomp_full/CursorByArmySO__GetCursor.c` 全文，逐句见下面 `CursorByArmy` 类）。

        /// <summary>当前用的指针图名（= 原版 `CursorByArmySO` 给的那一张；出厂 = `Cursor_Space_Marine_01`）。
        /// ⚠️ 带下划线是**我们 Resources 的落盘名**（导入器把原版名里的空格换成了 `_`）。</summary>
        string _cursorArt = ArtPointer;
        /// <summary>当前用的高光图名（同上；`null` = 这个阵营原版就没有高光（Dark Angels））。</summary>
        string _glowArt = ArtPointerGlow;

        public string CursorArtName { get { return _cursorArt; } }
        public string GlowArtName { get { return _glowArt; } }

        /// <summary>= 原版 `TutorialPointer.SetupCursor(CardArmy)`：按阵营取「指针图 + 高光图」两张。
        /// <para>**找到** ⇒ 两张都换掉（下一帧 `Rebuild` 生效）；**找不到**（原版 `GetCursor` 返回 false =
        /// 这一档阵营没有配指针）⇒ **保持不变**并出声（⛔ 不静默换回默认，也不拿别的阵营顶）。</para>
        /// <returns>有没有换（= 原版 `GetCursor` 的返回值）。</returns></summary>
        public bool SetArmy(int army)
        {
            string cursor, glow;
            if (!CursorByArmy.GetCursor(army, out cursor, out glow))
            {
                Debug.LogWarning("[MenuTutorial] 阵营 " + army + " 在 `CursorByArmySO` 那张表里**没有指针**"
                               + "（原版 `GetCursor` 扫完表会两个 out 都置 null 并返回 false）⇒ 沿用当前图 `"
                               + _cursorArt + "`。");
                return false;
            }
            _cursorArt = cursor;
            _glowArt = glow;
            if (_visible) Rebuild(LastPointPx, _bubbleText);     // 已经显示着 ⇒ 立刻换
            return true;
        }
    }

    /// <summary>原版 `CursorByArmySO.GetCursor(CardArmy cardArmy, out Sprite cursor, out Sprite highlight)` 的等价实现。
    /// <para>判据（`decomp_full/CursorByArmySO__GetCursor.c` **逐句**）：</para>
    /// <code>
    /// entries = this(+0x18)                          // 一个 List&lt;Entry&gt;
    /// for (i = 0; i &lt; entries.Count; i++) {
    ///     entry = entries[i];
    ///     if (entry.army(+0x10) == cardArmy) break;  // **命中就 break**
    ///     cursor = null; highlight = null;           // 没命中那一拍把两个 out 都清一次
    /// }
    /// if (i == entries.Count) { cursor = null; highlight = null; return 0; }   // 扫完没有 ⇒ 0
    /// if (entry.cursor(+0x18) != null) { cursor = Load(cursor);               // 只 Load 有值的那个
    ///     if (entry.highlight(+0x20) != null) { highlight = Load(highlight); return 1; } }
    /// // 其余 = throw（原版 NRE）
    /// </code>
    /// <para>⚠️ **数据来源**：`CursorByArmySO` 那个 SO **不在 `bundle_armycursors_assets_all` 里**
    /// （那个包只有 21 张 `Sprite/`、没有 `MonoBehaviour/`）⇒ **entry 的字段值读不到**。
    /// 但 21 张 Sprite 的**名字**把 11 个阵营一一列全了（`Cursor_&lt;阵营&gt;_01[Glow]`，见下表）——
    /// 表就是照这份名字排的（**有判据**），而 `EmperorsChildren(120)` / `SpaceWolves(130)` / `Neutral(0)`
    /// **一张都没有** ⇒ 原版对这三个阵营也是「扫完没有 ⇒ 返回 false」。</para>
    /// <para>⚠️ **Dark Angels(110)** 只有一张 `Cursor_Dark Angels`、**没有 Glow** ⇒ 原版那一支会走到 throw；
    /// 我们按「高光缺失 = 不画高光」返回 true 并出声一次（⛔ 不拿别的阵营的高光顶）。</para></summary>
    public static class CursorByArmy
    {
        /// <summary>一条 = 原版 `CursorByArmySO` 的 `Entry`（`army` / `cursor` / `highlight` 三个字段）。
        /// ⚠️ 图名用的是**我们 Resources 的落盘名**（原版名里的空格 → `_`）。</summary>
        public struct Entry
        {
            public int Army;
            public string Cursor;      // 原版 `entry.cursor`（`Sprite` 引用；我们存资源名）
            public string Glow;        // 原版 `entry.highlight`（同上；null = 那一档没有）
            public Entry(int army, string cursor, string glow) { Army = army; Cursor = cursor; Glow = glow; }
        }

        /// <summary>11 个阵营（**顺序无所谓** —— 原版是线性扫、命中即 break；这里按 `CardArmy` 值升序排）。
        /// 判据 = `bundle_armycursors_assets_all/Sprite/` 的 21 个 json 文件名（现读）。</summary>
        public static readonly Entry[] Table =
        {
            new Entry( 10, "Cursor_Space_Marine_01",   "Cursor_Space_Marine_01Glow"),   // Ultramarines
            new Entry( 20, "Cursor_Ork_01",            "Cursor_Ork_01Glow"),            // Goff
            new Entry( 30, "Cursor_Eldar_01",          "Cursor_Eldar_01Glow"),          // SaimHann
            new Entry( 40, "Cursor_Necron_01",         "Cursor_Necron_01Glow"),         // Sautekh
            new Entry( 50, "Cursor_Chaos_01",          "Cursor_Chaos_01Glow"),          // BlackLegion
            new Entry( 60, "Cursor_Space_Tyranid_01",  "Cursor_Space_Tyranid_01Glow"),  // Leviathan
            new Entry( 70, "Cursor_Tau_01",            "Cursor_Tau_01Glow"),            // TauEmpire
            new Entry( 80, "Cursor_Sororitas",         "Cursor_Sororitas_Glow"),        // Sororitas
            new Entry( 90, "Cursor_Genestealer_01",    "Cursor_Genestealer_01Glow"),    // Genestealers
            new Entry(100, "Cursor_AstraMillitarum_01","Cursor_AstraMillitarum_01Glow"),// AstraMilitarum
            new Entry(110, "Cursor_Dark_Angels",       null),                           // DarkAngels：原版只有 cursor
        };

        static bool _darkSaid;

        /// <summary>= 原版 `CursorByArmySO.GetCursor(...)`。命中 ⇒ 两个 out 写资源名、返回 true；
        /// 扫完没有 ⇒ 两个 out 都置 null、返回 false（**不是抛**）。</summary>
        public static bool GetCursor(int army, out string cursor, out string glow)
        {
            cursor = null; glow = null;
            for (int i = 0; i < Table.Length; i++)
            {
                if (Table[i].Army != army) continue;
                cursor = Table[i].Cursor;
                glow = Table[i].Glow;
                if (glow == null && !_darkSaid)
                {
                    _darkSaid = true;
                    Debug.LogWarning("[MenuTutorial] 阵营 " + army + " 的指针**没有高光图**"
                                   + "（`bundle_armycursors_assets_all` 里只有 `Cursor_Dark Angels` 一张）"
                                   + " ⇒ 只画指针、不画高光（原版这一支会 NRE；我们出声 + 继续）。");
                }
                return true;
            }
            return false;     // 原版：扫完没有 ⇒ 两个 out 都置 null 并 return 0
        }
    }

    // ============================================================ 原版 `TutorialPointerData`（B94_B6 · 2026-10-29）
    //
    // 判据（三源同读，全部现读）：
    //   · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/TutorialPointerData.cs`（**体最干净**的一份）：
    //     `public string id;` · `private Vector2 offset;` · `public bool filterRaycast;`
    //     · `Target` getter 全文 = `GameObjectUniqueIdentifier.Find(id)` → 非 null 就 `return`；
    //       否则 `CustomDebug.LogError(string.Concat("[ERROR] Tutorial. Can't find button with ID ", id))` → `return null`
    //   · `decomp_full/TutorialPointerData__get_Offset.c` = `return *(undefined8 *)(param_1 + 0x18)` ——
    //     就是那个字段本身（`+0x18` = `offset`），**没有任何加工**
    //   · 错误串字面量 = `out_il2csharp/stringliteral.json` `"[ERROR] Tutorial. Can't find button with ID "`（逐字）
    //
    // 🔴 **它为什么重要**：这是 `GameObjectUniqueIdentifier`（见 `MenuDraw.cs:3662` 的 `MenuUniqueIds`）在**全二进制里唯一的消费者**
    //   —— 三份产物（`decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp`）里 `Find`/`FindComponent` 的
    //   外部调用点**只有 `TutorialPointerData.Target` 这一处**。⇒ 注册表那一半（B94_S3 已落地）缺的就是这一跳。
    //
    // ⚠️ **两处如实标**：
    //   ① 原版 `offset` 是**私有且没有 `[SerializeField]`** ⇒ Unity 不序列化它 ⇒ 从 prefab/SO 里读到的**永远是 (0,0)**；
    //      真正非零的偏移都来自调用点（`AddPointer(node, offset, filterRaycast: 1)` 的**实参**），⛔ 别指望 SO 里带偏移。
    //   ② 原版 `Find` 走的是 `GameObjectUniqueIdentifier` 那个组件的 `Awake` 自注册表；本仓没有那个能挂的
    //      `MonoBehaviour`（见 `MenuDraw.cs:3640` 的说明）⇒ 这里走 `MenuUniqueIds`（同一份语义）。
    /// <summary>原版 `TutorialPointerData`（`[Serializable]`）：一条「指针该指哪」的数据
    /// = `id` + `offset` + `filterRaycast`，以及唯一的行为 <see cref="Target"/>。</summary>
    public sealed class TutorialPointerData
    {
        /// <summary>原版 `public string id`（`+0x10`）—— 唯一 id，交给注册表查节点。</summary>
        public string id;
        /// <summary>原版 `private Vector2 offset`（`+0x18`）—— ⚠️ 不序列化（见本节 ⚠️①）。</summary>
        Vector2 offset;
        /// <summary>原版 `public bool filterRaycast`（`+0x20`）—— 挂指针时要不要把射线限制到目标子树
        /// （消费点 = `MenuTutorialFlow` 的 `AddPointerAction`，判据在那里）。</summary>
        public bool filterRaycast;

        /// <summary>原版 `public Vector2 Offset { get { return offset; } }`（`+0x18` 原样）。</summary>
        public Vector2 Offset { get { return offset; } }

        /// <summary>原版 `public GameObject Target { get {...} }`：
        /// `Find(id)` → 命中即返回；未命中 ⇒ 打那条**逐字**错误串（`"[ERROR] Tutorial. Can't find button with ID " + id`）并返回 null。
        /// <para>🧨 灭自证：把日志那一句删掉、或把「未命中回 null」改成「抛异常」⇒ 断言 N 区翻面（见 `Editor/B94B6_Assert.cs`）。</para></summary>
        public GameObject Target
        {
            get
            {
                var go = MenuUniqueIds.Find(id);
                if (go != null) return go;                       // 原版 `if ((bool)gameObject) return gameObject;`
                Debug.LogError("[ERROR] Tutorial. Can't find button with ID " + id);   // 原版 `CustomDebug.LogError(string.Concat(...))`
                return null;
            }
        }
    }
}
