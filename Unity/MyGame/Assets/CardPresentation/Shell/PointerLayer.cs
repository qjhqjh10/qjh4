// PointerLayer.cs — 阶段二「外壳」的**公共指针层**（全壳唯一一条「真鼠标 → 界面」的路径）
//
// ============================ 为什么必须有 ============================
// 🔴 2026-09-23 查出（详据见 `项目任务.md` §三 第 15 条 **第 23 条**）：**整层菜单在真鼠标下点不动** ——
//    `WindowButton` 只实现老式的 `OnMouseUpAsButton()`（`Shell/PromptPopup.cs`），而
//      ① **Unity 要求同一物体上有 `Collider` 才会派发它**，本工程却**零处 `AddComponent<...Collider>`**
//         （`ImageQuad` 里 0 处引用；只有 `BoardLayout.cs:126` / `CardBaseDemo.cs:301` 两处**删** collider）；
//      ② `ProjectSettings.asset:932 activeInputHandler: **1**` = **只用新 Input System**
//         ⇒ 老式 `OnMouseXxx` 系列**根本不派发**。
//    ⇒ 自检之所以一直全绿，是因为它们走 `ClickForTest()` **直调 action**（那条注释写得很清楚）。
// ✅ **能点的先例就在工程里**：**卡组编辑**走 `Mouse.current` 轮询 + 自己算命中
//    （`Deck/DeckRuntime.cs:732 HandlePointer()` / `:1379 Hit()`），用户验过「左键看大图 / 右键加牌 / 滚轮」。
//    ⇒ 把那条路**收口成这一份**：`WindowButton` 与滚动区都从它拿输入
//      （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
//
// ============================ 它做什么 ============================
// 每帧（**只在 Play 里跑** —— 批处理下 MonoBehaviour 的 `Update` 不执行，
// 所以它对自检完全无副作用；自检要验就直调 `ClickAt`/`WheelAt`/`HoverAt`/`DragAt`/`TickAt`/`KeyFrame`）：
//   ① 滚轮 → 命中哪个滚动区就滚哪个（`MenuScroll.Wheel`）
//   ② 左键 → **按下与抬起落在同一个 `WindowButton` 上**才算点中（UGUI 的语义），调它的 `onClick`
//   ③ 🆕 **悬停**（`HoverAt`）→ 指针下那一颗进/出悬停态（原版 UGUI 的 `IPointerEnter/Exit`）
//   ④ 🆕 **拖拽**（`DragAt`）→ 越过 **10px** 阈值就把这一套动作判成「拖滚动区」，
//      抬起时**不再点按钮**（原版 EventSystem 的 `m_DragThreshold = 10`，`EventSystem.cs:68`）
//   ⑤ 🆕 **推进滚动区的惯性/回弹**（`TickScrolls` → `MenuScroll.Tick`）
//   ⑥ 🆕 **A49 键盘导航**：ESC 关当前窗（门槛 = 那扇窗自己的 `closeOnEsc`）+ 方向键选 + 回车确认
//      （`KeyFrame`；判据与「我们挑的」三处 → 下面「键盘导航（A49）」那一节）
// 命中顺序：**渲染队列大的先**（画在上面的先吃），同队列再比 `z`（越小越靠前 —— 照 `CardInteraction.HitTest`）。
//
// ⚠️ **仍没实现的（出声，不静默）**：
//   · **摇杆/手柄**那条输入（原版 `InputManager` 里 `Horizontal`/`Vertical` 另有 type=2 的摇杆轴、
//     `Submit` 另有 `joystick button 0`）—— 见「键盘导航」那一节 ②。**已记账**。
//   · ✅ **选中态的视觉**（原版 `Selectable` 的 `Selected` 态）—— **2026-10-07（A77⑮①）做了**：
//     `WindowButton.SetSelected` + `WindowButton.BtnState`（优先级照 UGUI `currentSelectionState`），
//     本文件的 `Select()` 负责通知「原来那颗退、新那颗上」（见那个方法的注释）。
//   · **右键**：🔴 **原版就没有** —— UGUI `Button.OnPointerClick` 首行就是
//     `if (eventData.button != PointerEventData.InputButton.Left) return;`（`Button.cs`）⇒
//     原版 UI 对右键**什么都不做**。我们跟着不做（铁律 11 的「原版本身就没有」那一档）。
//   · **悬停换图**（原版 `SpriteSwap` 那 630 个按钮）—— 本版只做了统一色偏，**已记账**（同上）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CardPresentation
{
    /// <summary>指针层。**惰性取用**（`Instance` 没有就现建一台）—— 不依赖 `Awake/OnEnable`：
    /// 🔴 2026-09-23 踩过：自检跑在**编辑模式**，而编辑模式下 `Awake/OnEnable` **只对 `[ExecuteAlways]` 的脚本**才跑
    /// （`WindowHolder` 正是因此才加那个特性 + 显式 `RegisterNow()`）。指针层**不挂 `[ExecuteAlways]`**：
    /// 挂了的话编辑器里开着场景也会轮询鼠标、点一下场景就派发按钮，得不偿失。</summary>
    [DefaultExecutionOrder(-50)]
    public class PointerLayer : MonoBehaviour
    {
        static PointerLayer _inst;

        /// <summary>当前这一台；**没有就现建一台**（见类注释：不依赖生命周期回调）。</summary>
        public static PointerLayer Instance
        {
            get
            {
                if (_inst != null) return _inst;
                _inst = Object.FindFirstObjectByType<PointerLayer>();
                if (_inst == null)
                {
                    var go = new GameObject("Pointer Layer");
                    _inst = go.AddComponent<PointerLayer>();
                }
                return _inst;
            }
        }

        readonly List<MenuScroll> _scrolls = new List<MenuScroll>();
        WindowButton _down;

        // ---- 悬停 / 按下 / 拖拽 的运行时状态（照 UGUI `EventSystem` + `StandaloneInputModule`）----
        /// <summary>原版 `EventSystem.m_DragThreshold` 的默认值（`EventSystem.cs:68`）。
        /// 越过它 ⇒ 这一套「按下 → 抬起」判成拖拽，**抬起时不再点按钮**。</summary>
        public const float DragThreshold = 10f;

        WindowButton _hover;
        MenuScroll _dragScroll;
        Vector2 _pressPx, _lastPx;
        bool _pressed, _dragging, _haveLastPx;

        /// <summary>此刻指针压着的那一颗（没有就是 null）。</summary>
        public WindowButton HoveredButton { get { return _hover; } }
        /// <summary>正在拖的滚动区（没在拖就是 null）。</summary>
        public MenuScroll DraggingScroll { get { return _dragging ? _dragScroll : null; } }

        // ============================================================ 文本焦点（键盘）
        //
        // 类头原来写着「**键盘：本轮没实现**」—— 2026-09-23 做「Cards 页完整筛选面板」时补上：
        // 那一栏第 1 行是**搜索框**（原版 `CardNameFilter` + `EverguildInputField` + `TMP_InputField`），
        // 没有键盘它就是个死框（红线：不许静默失败）。**将来的 `Import Deck Popup` 也要它。**
        //
        // ⚠️ **收口不到卡组编辑那一套**：那边是 `DeckRuntime.Update` 里自己轮询 `Keyboard.current`
        //    + `onTextInput`（`Deck/DeckRuntime.cs:1313 HandleTyping`），它不是 `WindowButton` 体系。
        //    这份是给**外壳/菜单**用的，两者**语义相同、各写一份**（明账，与「滚动也是两份」同性质）。
        //
        // 用法：`BeginText(初值, 上限, 提交, 取消, 每次改动)`；自检直调 `TypeChar/Backspace/EndText`
        //（批处理里 `Update` 不跑 ⇒ 这三条是自检唯一入口）。

        /// <summary>有焦点没有。</summary>
        public bool TextEditing { get { return _editing; } }
        /// <summary>当前编辑缓冲。</summary>
        public string TextBuffer { get { return _buf; } }

        bool _editing;
        string _buf = "";
        int _maxLen = 24;
        System.Action<string> _onCommitText, _onChangedText;
        System.Action _onCancelText;
        readonly List<char> _typed = new List<char>();

        public void BeginText(string initial, int maxLen, System.Action<string> onCommit,
                              System.Action onCancel = null, System.Action<string> onChanged = null)
        {
            _editing = true;
            _buf = initial ?? "";
            _maxLen = maxLen > 0 ? maxLen : 24;
            _onCommitText = onCommit; _onCancelText = onCancel; _onChangedText = onChanged;
            _typed.Clear();
            if (_onChangedText != null) _onChangedText(_buf);
        }

        public void TypeChar(char c)
        {
            if (!_editing || char.IsControl(c) || _buf.Length >= _maxLen) return;
            _buf += c;
            if (_onChangedText != null) _onChangedText(_buf);
        }

        public void Backspace()
        {
            if (!_editing || _buf.Length == 0) return;
            _buf = _buf.Substring(0, _buf.Length - 1);
            if (_onChangedText != null) _onChangedText(_buf);
        }

        /// <summary>结束编辑。**提交与取消走同一个函数**（两条路各写一份迟早不一致 —— 照 `DeckRuntime.EndTextEdit`）。</summary>
        public void EndText(bool commit)
        {
            if (!_editing) return;
            string s = _buf;
            _editing = false; _buf = ""; _typed.Clear();
            var cb = commit ? _onCommitText : null;
            var cb2 = _onCancelText;
            _onCommitText = null; _onCancelText = null; _onChangedText = null;
            if (cb != null) cb(s);
            else if (cb2 != null) cb2();
        }

        void HandleTyping()
        {
            var kb = Keyboard.current;
            if (kb == null || !_editing) return;
            if (kb.escapeKey.wasPressedThisFrame) { EndText(false); return; }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { EndText(true); return; }
            if (kb.backspaceKey.wasPressedThisFrame) Backspace();
            for (int i = 0; i < _typed.Count; i++) TypeChar(_typed[i]);
            _typed.Clear();
        }

        // ⚠️ `OnEnable/OnDisable` 是**全类唯一一份**（下面 `_inst` 的清理也在这两个里）——
        //    2026-09-23 加键盘时一度各写了一份 ⇒ `error CS0111: already defines a member`。
        void OnEnable()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnText;
        }

        void OnDisable()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText;
            if (_inst == this) _inst = null;
        }

        void OnText(char c) { if (_editing && !char.IsControl(c)) _typed.Add(c); }

        /// <summary>没有就建一台。`root` 给了就当它的子节点。</summary>
        public static PointerLayer Ensure(Transform root = null)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Pointer Layer");
            if (root != null) go.transform.SetParent(root, false);
            return go.AddComponent<PointerLayer>();
        }

        /// <summary>登记一个滚动区（滚轮才会找到它）。**窗口重开时旧的要清掉**，见 `UnregisterOwnedBy`。</summary>
        public static void RegisterScroll(MenuScroll s)
        {
            if (Instance == null || s == null) return;
            Instance.PruneScrolls();                 // 🆕 顺手清一遍死条目 ⇒ 登记表**不会只增不减**
            if (!Instance._scrolls.Contains(s)) Instance._scrolls.Add(s);
        }

        /// <summary>🆕 **把某个宿主名下的滚动区全撤掉**（窗口/页面重建时调）。
        /// 🔴 **为什么需要它**（2026-09-27 修的一颗地雷）：窗口的 `Setup()` 是「把 Root 的子节点全删了重建」，
        ///    而重建出来的滚动区是**新对象**、旧的仍留在登记表里 —— 旧那些的 `Owner` 是**窗口根**
        ///    （重建时根不会死）⇒ 光靠 `Owner == null` 判不出它们已经没用了。
        ///    表现：登记表**每次开窗涨一批**，而且**旧条目还能被滚轮命中**（`OnChanged` 指向已经销毁的节点）。
        ///    📌 判据与「滚动区只增不减」那条 → `项目任务.md` §〇 A ②。</summary>
        public static void UnregisterOwnedBy(GameObject owner)
        {
            if (Instance == null || owner == null) return;
            for (int i = Instance._scrolls.Count - 1; i >= 0; i--)
            {
                var s = Instance._scrolls[i];
                if (s == null || s.Owner == owner) Instance._scrolls.RemoveAt(i);
            }
        }

        /// <summary>清掉全部滚动区登记（**换场景/壳重建**时调）。</summary>
        public static void ClearScrolls()
        {
            if (Instance != null) Instance._scrolls.Clear();
        }

        /// <summary>登记表里还剩几条（自检用 —— 「只增不减」那颗雷就是靠它对账）。</summary>
        public static int ScrollCountForTest { get { return Instance != null ? Instance._scrolls.Count : 0; } }

        /// <summary>清掉**已经死了**的条目（宿主销毁 / 根本没设宿主）。
        /// ⚠️ **只清「死的」，不清「关着的」** —— 页签切走走的是 `SetActive(false)`，
        ///    那些区**还会回来**，清掉就得重建时才登记得上（`HitScroll` 里那条 `continue` 就是干这个的）。</summary>
        void PruneScrolls()
        {
            for (int i = _scrolls.Count - 1; i >= 0; i--)
            {
                var s = _scrolls[i];
                // 🔴 `s.Owner == null` 有两个来源：**指向的对象被销毁**（Unity 的假 null）与**从没设过**。
                //    全工程 18 处登记**每一处都设了 `Owner`**（2026-09-27 逐处核过）⇒ 这里当成「死的」是安全的。
                if (s == null || s.Owner == null) _scrolls.RemoveAt(i);
            }
        }

        void Update()
        {
            bool wasEditing = _editing;
            HandleTyping();                 // 键盘先吃（在编辑文本时鼠标照样能点，两条不互斥）

            // ⑥ 🆕 A49 键盘导航（方向键选 + 回车确认 + ESC 关窗）—— 判据见「键盘导航」那一节
            // ⚠️ `wasEditing` = **这一帧的键盘归文本框**（`HandleTyping` 刚用 ESC/回车把编辑收掉了）
            //    ⇒ 导航不许接着把同一次 ESC 当成「关窗」（否则「取消编辑」会顺手关掉整扇窗）。
            var kb = Keyboard.current;
            if (kb != null && !wasEditing)
                KeyFrame(kb.escapeKey.wasPressedThisFrame, AxisX(kb), AxisY(kb), SubmitDown(kb),
                         Time.unscaledTime);

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 wp = LayoutSpace.ScreenToWorld(mouse.position.ReadValue(), LayoutSpace.Cam);
            Vector2 px = LayoutSpace.ToPixel(wp);

            // ① 悬停：指针动过、或这一帧有滚动区被推动过（内容会跑到指针底下）就重算一次
            bool scrollMoved = false;
            if (!_haveLastPx || (px - _lastPx).sqrMagnitude > 0.0001f)
            {
                _haveLastPx = true; _lastPx = px;
                HoverAt(px.x, px.y);
            }

            // ② 拖拽 + 点击（同一套「按下 → 移动 → 抬起」；照原版 EventSystem 的 10px 阈值）
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _pressed = true; _dragging = false;
                _pressPx = px;
                _down = HitButton(px.x, px.y);
                _dragScroll = HitScroll(px.x, px.y);
                // 🆕 A49：原版鼠标**按下**也会改「选中」（`StandaloneInputModule.ProcessMousePress` →
                //    `DeselectIfSelectionChanged`）—— 点在空白处 ⇒ 选中变 null（原版就是清掉）。
                //    键盘与鼠标**共用这一份选中**，所以它必须挂在这条唯一输入路上。
                Select(_down);
                if (_down != null) _down.Press();
            }
            else if (_pressed && mouse.leftButton.isPressed)
            {
                if (!_dragging && _dragScroll != null
                    && (px - _pressPx).magnitude > DragThreshold)
                {
                    _dragging = true;
                    _dragScroll.BeginDrag(AxisOf(_dragScroll, _pressPx));
                }
                if (_dragging) _dragScroll.DragTo(AxisOf(_dragScroll, px));
            }
            else if (_pressed && mouse.leftButton.wasReleasedThisFrame)
            {
                if (_dragging)
                {
                    if (_dragScroll != null) _dragScroll.EndDrag();   // 速度留着 ⇒ 下一帧带惯性
                }
                else
                {
                    var up = HitButton(px.x, px.y);
                    bool fired = up != null && up == _down;
                    // 🆕 **真实点击记录**（用户 2026-09-24 要的）：批处理验不到「真点一下会怎样」，
                    //    这里把「点了哪儿 / 命中了谁（含被压住的候选）/ 这一下实际触发什么」落成一行。
                    //    见 `Core/ClickLog.cs` 文件头。⚠️ 只在 `Enabled` 时写，不影响任何派发逻辑。
                    if (ClickLog.Enabled)
                    {
                        ClickLog.Begin(SceneManager.GetActiveScene().name, "PointerLayer", px);
                        LogHit(up, up == _down, fired, px.x, px.y);
                        // 收尾由 `ClickLog` 的帧末驱动做（出口多也不会漏）
                    }
                    if (fired) up.Click();
                }
                if (_down != null) _down.Release();
                _pressed = false; _down = null; _dragScroll = null;
            }

            // ③ 滚轮（拖拽中不吃 —— 同 UGUI：拖的时候不响应滚轮）
            if (!_dragging)
            {
                float dy = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(dy) >= 1f) { WheelAt(px.x, px.y, dy); scrollMoved = true; }
            }

            // ④ 推一帧惯性/回弹（照 UGUI `LateUpdate`；用 `unscaledDeltaTime` 与 UGUI 一致）
            if (TickAt(Time.unscaledDeltaTime)) scrollMoved = true;

            // 滚完之后内容可能跑到指针底下了 ⇒ 悬停重算一次
            if (scrollMoved) HoverAt(px.x, px.y);
        }

        /// <summary>滚动轴上的指针坐标（横向给 x、纵向给 y）—— `MenuScroll` 只认一个分量。
        /// <para>🔴 **2026-10-11（A167）**：`MenuScroll` 内部**一律是设计 px**（`Viewport` / `ContentX1/2` /
        /// `Offset` 全是宿主按原版矩形给的**设计**值），而这里的 `px` 是**世界 px**
        /// （`ToPixel(ScreenToWorld(mouse))` —— 相机不随窗根缩放）⇒ 喂给 `BeginDrag` / `DragTo` 之前
        /// **要除回设计 px**（M == 1 时逐位不变）。
        /// <para>📌 **判据 = 原版**：`ScrollRect.OnBeginDrag/OnDrag` 用的都是
        /// `RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Viewport, …)` ⇒ 指针坐标在
        /// **视口的局部单位**里（= 我们这份设计 px），**不是**屏幕像素 —— 所以拖拽的「走了多远」本来就该
        /// 按设计单位算（原版放大后拖同样的手感、内容走同样的设计距离）✔。</para>
        /// **改坏法**：把这一除去掉 ⇒ 开关开着的窗里拖拽位移是 `M` 倍（1.2 倍窗 ⇒ 拖一点内容跑 1.2 倍），
        /// 而 `HitScroll` 那条**不会**跟着红（两条各自独立）。</para></summary>
        static float AxisOf(MenuScroll s, Vector2 px)
        {
            var m = ScrollScale(s);
            return s != null && s.Vertical
                ? UnScaleAbout(LayoutSpace.DesignPxH * 0.5f, px.y, m.y)
                : UnScaleAbout(LayoutSpace.DesignPxW * 0.5f, px.x, m.x);
        }

        /// <summary>🆕 **2026-10-11（A167）**：一个滚动区所在那一级的缩放 = `Owner` 的 `lossyScale`
        /// （拿不到 `Owner` ⇒ `(1,1)` ⇒ 退回旧行为）。判据同 `MenuDraw.PosInDesignSpace`：
        /// 乘在**窗口根**上的那一级（`TransformScalerBySmallScreenUI`），一路含下来。</summary>
        static Vector2 ScrollScale(MenuScroll s)
        {
            var t = s != null && s.Owner != null ? s.Owner.transform : null;
            var k = t != null ? t.lossyScale : Vector3.one;
            return new Vector2(ScaleAbs(k.x), ScaleAbs(k.y));
        }

        /// <summary>设计 px → 指针那一帧（**世界 px**）：逐分量 `c + (v − c)·M`（`c` = 画布中心）。
        /// 窗根被乘 M 时，设计点 d **渲出来**在 `M·d` ⇒ 它的画布 px = `c + (ToPixel(d) − c)·M`
        /// （与 `Shell/SettingsWindow.cs` 的 `Screen()` 那条把固定 0.9 烘进矩形的仿射**同一个形状**）。
        /// ⚠️ 前提 = **窗根在世界原点**（与 `MenuDraw.PosInDesignSpace` 同一条，见它的注释）。</summary>
        static PxRect DesignToPtrPx(PxRect r, Vector2 m)
        {
            return new PxRect(ScaleAbout(LayoutSpace.DesignPxW * 0.5f, r.x1, m.x),
                              ScaleAbout(LayoutSpace.DesignPxH * 0.5f, r.y1, m.y),
                              ScaleAbout(LayoutSpace.DesignPxW * 0.5f, r.x2, m.x),
                              ScaleAbout(LayoutSpace.DesignPxH * 0.5f, r.y2, m.y));
        }
        static float ScaleAbout(float centerPx, float v, float m) { return centerPx + (v - centerPx) * m; }
        static float UnScaleAbout(float centerPx, float v, float m) { return centerPx + (v - centerPx) / m; }

        /// <summary>把「命中 / 候选 / 派发没派发」三样交给 `ClickLog`。</summary>
        static void LogHit(WindowButton up, bool sameSpot, bool fired, float px, float py)
        {
            if (up == null) return;      // 空点：`ClickLog.Begin` 已经记了点位，正文会写「这一点上没有命中」
            var q = up.GetComponentInChildren<ImageQuad>();
            ClickLog.Hit("命中 `" + up.name + "`" + PathOf(up.transform)
                         + " · 队列 " + (q != null ? q.RenderQueue.ToString() : "?")
                         + " · z " + (q != null ? q.transform.position.z.ToString("F3") : "?"),
                         up.absorbOnly
                             // 🆕 2026-10-06（A94）：吸收层**不是**「忘了绑」 —— 措辞必须与下一条分开，
                             //    否则每次点窗内空白处都会在点击记录里留下一条看着像缺陷的红字。
                             //    判据 → `MenuDraw.Absorb` 的注释（原版面板那颗 `Image` 吃掉了这一下）。
                             ? "= **窗内面板的吸收层**：同原版 —— 这一下**什么都不做**（不关窗、不派发）"
                             : (up.onClick == null ? "🔴 **这个命中区没有绑动作**（`onClick == null`）" : null));
            ClickLog.Candidates(HitLines(px, py));
            ClickLog.Hit(fired ? "→ **已派发** `onClick`"
                               : (sameSpot ? "→ **没派发**（未满足按下/抬起同一件）" : "→ **没派发**（按下与抬起不在同一件上）"));
        }

        /// <summary>短父链（往上取 3 层，够定位是哪一页的哪个件）。</summary>
        static string PathOf(Transform t)
        {
            var s = "";
            var cur = t != null ? t.parent : null;
            for (int i = 0; i < 3 && cur != null; i++) { s = "/" + cur.name + s; cur = cur.parent; }
            return s;
        }

        /// <summary>这一点上**所有**候选件（含被压住的），按「渲染队列↓ · z↑」排 —— 第一个才是真吃到的。
        /// 🔴 这一列专治本工程反复踩的「同一个队列谁盖谁不可控」那一族坑。</summary>
        static List<string> HitLines(float px, float py)
        {
            var list = new List<string>();
            var all = CollectHits(px, py);
            all.Sort((a, b) =>
            {
                if (a.q.RenderQueue != b.q.RenderQueue) return b.q.RenderQueue.CompareTo(a.q.RenderQueue);
                return a.q.transform.position.z.CompareTo(b.q.transform.position.z);
            });
            int n = Mathf.Min(all.Count, 8);
            for (int i = 0; i < n; i++)
            {
                var q = all[i].q;
                list.Add("q=" + q.RenderQueue + " z=" + q.transform.position.z.ToString("F3")
                         + "  `" + all[i].btn.name + "`" + PathOf(all[i].btn.transform));
            }
            return list;
        }

        // ============================================================ 🆕 A49 键盘导航
        //
        // 🔴 **判据（照本地 UGUI 源码复刻，不凭印象）**：
        //
        //  ① **方向键选 + 回车确认 = `StandaloneInputModule` 那一整套**
        //     （`Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/InputModules/StandaloneInputModule.cs`）：
        //     顺序固定 —— `SendUpdateEventToSelectedObject()`（:299）→ `SendMoveEventToSelectedObject()`（:311）
        //     → `SendSubmitEventToSelectedObject()`（:313）；**move 没被用掉才 submit**。
        //     **原版真的跑这一套**：它的输入模块是 `EverguildInput : StandaloneInputModule`
        //     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/EverguildInput.cs:5`），而
        //     `EverguildInput.Process()` 的第一条语句就是 `UnityEngine_EventSystems_StandaloneInputModule__Process`
        //     （`d:/2/tools/decomp_full/EverguildInput__Process.c` 亲读）。
        //  ② **键位 = 原版的 `InputManager`**（`d:/2/新解包资源/assets_full/globalgamemanagers/InputManager/InputManager_2.json`
        //     逐条实读）：`Horizontal` = `left`/`right` + alt `a`/`d` · `Vertical` = `down`/`up` + alt `s`/`w`
        //     · `Submit` = `return` / `enter` + `space`（另有 `joystick button 0`）· `Cancel` = `escape`。
        //     ⚠️ **摇杆那几条没接**（`Horizontal`/`Vertical` 另有 type=2 的摇杆轴、`Submit` 的 alt 是手柄键）
        //     —— 如实记账，**不假装接了**（我们的输入是 `Keyboard.current`，工程里没有 UGUI 事件系统）。
        //  ③ **ESC 关窗 = 原版 `WindowsManager.Update`**（`d:/2/tools/decomp_full/WindowsManager__Update.c`）：
        //     `Input.GetKeyDown(0x1b = KeyCode.Escape)` → 打给 **`currentWindow`**（字段 0x58，
        //     实证 = `WindowsManager__get_CurrentWindow.c` 读的就是 0x58）：先问一个 bool 虚方法
        //     （虚表 **0x1f8**、`GameWindow__IsOpen.c` 读的是 `*(int*)(this + 0x68) == 1`）、false 就 return，
        //     再调 **0x1e8** 那个动作。
        //     🔴 **2026-10-07（A77㉑⑦）「那两跳是谁」已从推断升级为【坐实】**：`d:/2/tools/il2cpp_out/dump.cs`
        //     的 `GameWindow` 一节带 **`Slot:` 号** —— `ESCPressed` = **Slot 11**、`IsOpen` = **Slot 12**；
        //     而 `Close` = Slot 8 = 虚表 **0x1b8**（`GameWindow__Close` 的反汇编里就是 `mov rdx,[rax+0x1c0];
        //     call [rax+0x1b8]`）⇒ 每槽 0x10 ⇒ Slot 11 = 0x1e8、Slot 12 = 0x1f8，与 `Update` 里那两个槽位
        //     逐一对上。（原来是「**中间那一跳读不出方法名**、按它接」，现已可去掉那个保留。）
        //     🔴 **`ESCPressed` 内部是【两道】门槛、按序**（反汇编 VA **0x180835ab0** 逐条读出来）：
        //       ① `EventSystemController.Instance.eventSystem.enabled` —— `call 0x181258290`
        //          （`SingletonBehaviour<EventSystemController>.get_Instance`，类指针 = `DAT_1842bdc50`）
        //          → `mov rcx,[rax + 0x20]`（= `EventSystemController.eventSystem`，`dump.cs` 实读的偏移）
        //          → `call 0x182feb6f0`（`Behaviour.get_enabled`）→ `test al,al; je <return>`
        //          ⇒ **输入系统没启用 ⇒ ESC 不关窗**；
        //       ② `if (*(char*)(this + 0x39) == 0) return;` —— 0x39 正是 `closeOnESC`
        //          （`GameWindow__ToggleESC.c` 写的就是 `param_1 + 0x39`；字段序旁证：`type` 在 0x20、
        //          `useDefaultCloseSoundIfNull`(默认 true) 在 0x38、`closeOnESC` 在 0x39、`updateNavPanel` 在 0x3a、
        //          `extraScaleSmallScreen`(默认 1.0f) 在 0x3c、`CurrentState` 在 0x68）。
        //     两道都过 ⇒ `Close()`（虚表 0x1b8）+ `updateNavPanel` 那一支。
        //     ⇒ **顺序 = `IsOpen()` ⇒ 输入系统启用 ⇒ `closeOnEsc` ⇒ 关**（我们这一侧就在 `KeyCancel`：
        //        第一跳 `IsOpen`、第二跳 `PointerLayer.InputEnabled`、第三跳在那扇窗自己的 `ESCPressed` 里）。
        //     🔴 **订正（铁律 5）**：`资料/待办判据_审查发现_1005.md` ㉑①-a 把第一道门槛写成
        //     「`ChatPreviewMessage.Instance != null && Instance.message.enabled`」—— **那一句是错的**：
        //     `ChatPreviewMessage` 是 `MonoBehaviour`、**没有 `Instance`**；而**同一个类指针槽**在两个互不相干的
        //     文件里被当 `this` 传给 `EventSystemController` 的方法（`CombatCameraZoom__HandleManualControl.c` →
        //     `EventSystemController__IsPointerOverUIObject`；`EverguildTooltipManager__Update.c` →
        //     `EventSystemController__GetPointerEventData`）⇒ 那个槽 = **`EventSystemController`**，
        //     0x20 就是它的 `eventSystem` 字段（`dump.cs` 实读））；
        //     逐条证据 → `资料/普查产出_1007/波8_A77_21_A49审查响应.md` §2·①。
        //     ⚠️ 我们这边**没有 UGUI `EventSystem`** ⇒ 第一道门槛用**唯一那条输入路**当等价物
        //     （`InputEnabled`，**我们挑的**，见那个字段的注释）。
        //
        //  ④ **全场只有一份「选中」**（键盘与鼠标共用）：原版鼠标**按下**也会改选中
        //     （`StandaloneInputModule.ProcessMousePress` → `DeselectIfSelectionChanged`；
        //     点在空白处 ⇒ 选中变成 null）⇒ 它必须住在**这一条输入路**里、与指针命中共用同一份遍历
        //     （`资料/阶段二_滚动与指针_原版规格.md` §3·2：别在第二处再写一份命中逻辑）。
        //
        // ⚠️ **我们挑的 / 查不到的（铁律 3 —— 每一处都标出来）**：
        //   · **「第一颗」是哪一颗**：原版那一刻取的是 `EventSystem.currentSelectedGameObject`（非空时）
        //     否则 `firstSelectedGameObject`（`StandaloneInputModule.cs:280-288`），而**那台 `EventSystem`
        //     的实例本地解包资源里没有**（全库 24.7 万文件 grep `m_HorizontalAxis` / `m_FirstSelected` = **零命中**）
        //     ⇒ **「开窗后默认选中谁」取不到判据**。我们定义成「**最上面那扇窗**里、层级序第一颗可用的
        //     `WindowButton`」，在两个时刻触发：
        //       (a) **开窗时**（`WindowsManager.OpenWindow` → `SelectFirstIn`）—— ⚠️ **这一个【时刻】也是我们挑的**：
        //           原版那个 `ActivateModule` **只在输入模块被激活时跑一次**（`EventSystem.cs:542`），
        //           **不是每次开窗**（判据 → ㉑⑥；`WindowsManager.OpenWindow` 那一行的注释里写着）；
        //       (b) **方向键/回车进来时若当前没有选中** —— 原版 `SendMoveEventToSelectedObject` 在
        //           `currentSelectedGameObject == null` 时**什么都不做**（`ExecuteEvents.Execute(null, …)` 空转），
        //           照抄的话「点了一下空白 = 选中被清掉」之后**键盘就死了**。
        //   · ✅ **选中态画出来了**（**2026-10-07（A77⑮①）做的**，本条原来写的是「选中是真的、但看不见」）：
        //     原版 `Selectable` 的独立 `Selected` 视觉 = `m_SelectedColor` / `m_SpriteState.m_SelectedSprite`
        //     ⇒ 现在落在 `WindowButton.SetSelected(bool)` + `BtnState`（优先级照 UGUI `currentSelectionState`：
        //     Pressed ＞ Selected ＞ Highlighted ＞ Normal），本文件的 `Select()` 换人时两颗都通知。
        //     ⚠️ **仍然「看不见」的那些是另一回事、已如实记账**：`MenuDraw.Hit` 建的命中区里那颗 quad 是
        //     **透明的**（`SetTint(0,0,0,0)`），而 `WindowButton.Collect()` 收的是**自己子树**里的 quad
        //     ⇒ 那种钮的色偏**打在透明件上**（悬停也一样，不是本批引入）。判据 = 原版 `Selectable.m_TargetGraphic`
        //     那一颗要跟着变；我们的等价物是 `Bind` 传进来的 `target`，但 `Bind` 会关掉色偏（换图那一档）。
        //   · **摇杆/手柄**没接（见 ②）。

        /// <summary>原版 `StandaloneInputModule.m_RepeatDelay`（本机 UGUI 源码
        /// `…/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/InputModules/StandaloneInputModule.cs:73`，
        /// **字段初始值 = 0.5**）。
        /// 🔴 **2026-10-07（A77㉑⑥）措辞订正（铁律 5）**：这里原来写「**实测**默认 0.5」—— **不严谨**。
        /// 我们手上的是**源码里的字段初始值**（= **默认值假设**），**不是原版那一份序列化出来的实参**：
        /// `assets_full` 里**没有 `EventSystem` GameObject**、`m_RepeatDelay` 字段名全库**零命中**
        /// ⇒ 原版运行时的值**取不到**。默认配置下两者应当一致，但**没有验过**，如实标成假设。</summary>
        public const float RepeatDelay = 0.5f;
        /// <summary>原版 `StandaloneInputModule.m_InputActionsPerSecond`（同文件 `:70`，**字段初始值 = 10**）。
        /// ⇒ 换方向 / 已过重复延迟之后，同一秒最多触发 10 次。
        /// ⚠️ 与上一条同性质：**默认值假设**，不是原版运行时的实测值（判据 → `RepeatDelay` 那段）。</summary>
        public const float InputActionsPerSecond = 10f;

        // ------------------------------------------------------------ 🆕 A77㉑①：输入层开着没有（ESC 的第一道门槛）

        static bool _inputEnabled = true;

        /// <summary>🆕 **输入层开着没有** —— 对位原版 `EventSystemController.Instance.eventSystem.enabled`
        /// （`GameWindow.ESCPressed()` 的**第一道门槛**，反汇编逐条 → `Shell/WindowsManager.cs` 的 `ESCPressed`）。
        /// <para>🔴 **在我们这边它是【我们挑的】等价物**：原版读的是 UGUI `EventSystem.enabled`，而本仓
        /// **没有 UGUI `EventSystem`**（那台 `EventSystem` 的实例在 `assets_full` 里根本不存在；
        /// 唯一那条输入路就是本类 ⇒ 用它当等价物）。**门槛的形状照原版**：为 `false` 时按 ESC **不关窗**。</para>
        /// <para>出厂值 `true`（= 原版那台 EventSystem 出厂是启用的）。原版改这一位的是
        /// `EventSystemController.Toggle(bool)`（虚表实读 = `eventSystem.enabled = option`，与
        /// `ChatPreviewMessage.Enable` 同体 ⇒ ICF），我们对应 `SetInputEnabled`。</para>
        /// <para>⚠️ 今天生产上**没有**任何一处把它置 `false`（`grep -rl "EventSystemController__Toggle"`
        /// 全量反编译 **0 命中** ⇒ 原版那边同样没人关它）⇒ 运行时恒 `true`；
        /// 它存在是为了**这道门槛本身**（自检两态都断，见 `Editor/ShellScene.cs` ⑤·j）。</para></summary>
        public static bool InputEnabled { get { return _inputEnabled; } }

        /// <summary>开关输入层（= 原版 `EventSystemController.Toggle(bool)`）。出声（红线：不许静默失败）——
        /// 「ESC 按了没反应」要么是这道门槛、要么是缺陷，必须能从日志里分辨。</summary>
        public static void SetInputEnabled(bool on)
        {
            if (_inputEnabled == on) return;
            _inputEnabled = on;
            Debug.Log($"[Key] 输入层 **{(on ? "启用" : "停用")}**（对位原版 `EventSystemController.Toggle({on.ToString().ToLowerInvariant()})`：" +
                      "它写的就是 `eventSystem.enabled` ⇒ 停用时 `GameWindow.ESCPressed` 的第一道门槛不过、ESC 不关窗）");
        }

        /// <summary>原版 `GetAxisEventData(x, y, 0.6f)` 里那个死区（`StandaloneInputModule.cs:509`）。</summary>
        public const float MoveDeadZone = 0.6f;

        /// <summary>`MoveDirection` 的值**照 UGUI**（`EventSystem/EventData/MoveDirection.cs`：None 0 · Left 1 · Up 2 · Right 3 · Down 4）。</summary>
        const int DirNone = 0, DirLeft = 1, DirUp = 2, DirRight = 3, DirDown = 4;

        // ---- 选中态 + 重复键节流状态（照 `StandaloneInputModule` 的 `m_ConsecutiveMoveCount` / `m_PrevActionTime` / `m_LastMoveVector`）----
        WindowButton _sel;
        int _moveCount;
        float _movePrevTime;
        Vector2 _moveLast;

        /// <summary>此刻的「选中」（键盘的落点；没有就是 null）。**带存活检查** —— 那颗钮被销毁 / 关掉
        /// （原版对应 `Selectable.OnDisable` 会把选中清掉）⇒ 这里返回 null 并顺手清干净。</summary>
        public WindowButton Selected
        {
            get
            {
                if (_sel == null) return null;                      // Unity 的假 null 也走这一条
                if (!_sel.isActiveAndEnabled)
                {
                    _sel.SetSelected(false);                        // 🆕 A77⑮①：把选中态的视觉一起退掉
                    _sel = null; return null;
                }
                return _sel;
            }
        }

        /// <summary>把选中挪到某一颗（`null` = 取消选中）。**= 原版 `EventSystem.SetSelectedGameObject`**：
        /// 鼠标按下与键盘移动**共用这一个入口**（原版 `ProcessMousePress` → `DeselectIfSelectionChanged`）。
        /// 🔴 **吸收层按 `null` 处理**（`WindowButton.absorbOnly`，见 `MenuDraw.Absorb`）——
        /// 原版 `DeselectIfSelectionChanged` 点在**没有 `Selectable`** 的 Graphic 上就是
        /// `SetSelectedGameObject(null)` ⇒ 点窗内面板 = **取消选中**（而不是「选中一块点不动的面板」）。
        /// <para>🆕 **A77⑮①**：换人时**两颗都要通知**（原来的 <see cref="WindowButton.SetSelected"/>(false)、
        /// 新的 `(true)`）—— 原版 `EventSystem` 就是这么发 `OnDeselect` / `OnSelect` 的，
        /// 而 `Selectable.OnSelect/OnDeselect` 各写一次 `hasSelection` 再重画一次
        /// （`Selectable.cs:1329/1356`）。少了这一跳 = 选中态**看得见地卡在旧的那颗上**。</para></summary>
        public void Select(WindowButton b)
        {
            if (b != null && b.absorbOnly) b = null;      // A94：面板不是 `Selectable`
            if (_sel == b) return;
            var prev = _sel;                              // Unity 的假 null 也走这一条
            _sel = b;
            if (prev != null) prev.SetSelected(false);    // = 原版 `OnDeselect`（`hasSelection = false`）
            if (b != null) b.SetSelected(true);           // = 原版 `OnSelect`（`hasSelection = true`）
        }

        /// <summary>选中某个窗里的**第一颗**（层级序）可用按钮 —— 见上面「我们挑的」①。
        /// 窗里一颗都没建出来时**不动**选中（不静默清空）。
        /// 🔴 **吸收层不算**（`WindowButton.absorbOnly`，见 `MenuDraw.Absorb`）：原版面板不是 `Selectable`，
        /// 方向键 / 默认选中都不该停在它上面 —— 少了这一跳，「默认选中」会落在一块**点了没反应**的面板上。</summary>
        public void SelectFirst(GameObject windowRoot)
        {
            if (windowRoot == null) return;
            var all = windowRoot.GetComponentsInChildren<WindowButton>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                // 判据只在 `Navigable` 一处：吸收层不算（A94）+ 没有命中区的钮**导航不到**（候选集与这里同一判据）
                if (!Navigable(b)) continue;
                Select(b);
                return;
            }
        }

        /// <summary>同上的静态入口（`WindowsManager.OpenWindow` 用；没有指针层就什么都不做）。</summary>
        public static void SelectFirstIn(GameObject windowRoot)
        {
            if (Instance == null) return;
            Instance.SelectFirst(windowRoot);
        }

        /// <summary>🆕 **一帧的键盘入口** —— 对位原版 `StandaloneInputModule.Process()` 里那三跳
        /// （`…__Update` → `…__Move` → `…__Submit`）。`Update` 每帧调它；**自检也调它**
        /// （批处理里没有输入事件，`Keyboard.current` 是空的 ⇒ 这是唯一入口）。
        /// <para>`esc`/`submit` = **本帧按下**；`ax`/`ay` = 轴的**原始值**（原版 `GetAxisRaw` 那条，数字键就是 ±1/0）；
        /// `time` = `Time.unscaledTime`（<c>&lt; 0</c> ⇒ 现取）。返回**这一帧键盘有没有被吃掉**。</para>
        /// <para>⚠️ **文本框有焦点时整段不接手**（原版 `InputField` 也是自己吃掉这些键）——
        /// 而且 `Update` 那边还有一道 `wasEditing` 守卫：ESC 刚把编辑**取消**掉的那一帧也不许接着关窗。</para>
        /// </summary>
        public bool KeyFrame(bool esc, float ax, float ay, bool submit, float time = -1f)
        {
            if (_editing) return false;                 // 键盘归文本框
            if (esc && KeyCancel()) return true;        // ESC 先吃（原版它在 `WindowsManager.Update` 里）
            bool used = KeyMove(ax, ay, time);          // = `SendMoveEventToSelectedObject`（恒 false，见该方法）
            if (!used && submit) used = KeySubmit();    // = `SendSubmitEventToSelectedObject`
            return used;
        }

        /// <summary>**ESC** = 原版 `WindowsManager.Update` 那一段 + `GameWindow.ESCPressed()` 的门槛。
        /// <para>🔴 **三跳、按原版顺序**（逐条 → 「键盘导航」③）：
        /// ① 取**最上面那扇**（原版 `currentWindow`，我们 = `TopWindow`）；没有 ⇒ 转给**常驻主菜单**（见下面那段）；
        /// ② **`IsOpen()`**（原版 `Update` 里虚表 `0x1f8` 那一跳：`*(int*)(this+0x68) == 1`）—— 不是 `Open` 态就 **return**；
        /// ③ 那扇窗自己的 `ESCPressed()`，里面还有两道：**输入层启用** + **`closeOnEsc`**。</para>
        /// <para>🆕 **A77㉑①**：② 是这一轮补上的（原来没有 ⇒ 「被压到 `Background` 的窗也能被 ESC 关掉」）。
        /// 它**必须**与 `WindowsManager.ShowPreviousWindow` 成对（关掉弹窗后底窗要回 `Open`，否则底窗就再也关不掉了）。</para>
        /// <para>🆕 **A77⑮② / A216（2026-10-07）**：① 的「没有窗」那一支**不再是「什么都不做」** ——
        /// 原版那一刻 `currentWindow` 是**常驻的 `baseMenu`**（`MainMenuWindow`），它把 ESC 覆写成
        /// 「开「退出游戏」弹窗」⇒ 我们这一支转给 `MainMenuRuntime.EscapePressed()`（**常驻主菜单不在场 ⇒ 仍是 false**）。</para>
        /// 返回**有没有真的关掉一扇窗**（关不掉 = 门槛没过：`IsOpen` 为假 / 输入层停用 / 那扇窗 `closeOnEsc == false`，
        /// 三种都**什么都不做**；⚠️ 主菜单那一支返回的是「**有没有真的弹了退出窗**」，同样是「这一下有没有被用掉」的口径）。</summary>
        public bool KeyCancel()
        {
            var wm = WindowsManager.Instance;
            var w = wm != null ? wm.TopWindow : null;
            if (w == null)                             // 原版：`currentWindow == null` ⇒ 直接 return
            {
                // 🆕 **2026-10-07（A77⑮② / A216）**：原版那一刻 `currentWindow` **不是 null** —— 它是**常驻的
                //   `baseMenu`（`MainMenuWindow`）**，而 `MainMenuWindow.ESCPressed()` 覆写成
                //   「`base.ESCPressed(); SettingsMenu.ExitGamePopup();`」⇒ **主菜单上按 ESC 会叫出「退出游戏」弹窗**。
                //   我们的主菜单**不是 `WindowsManager` 的窗**（`Shell/MainMenuRuntime.cs` 自己那棵树、没有 `baseMenu` 字段）
                //   ⇒ 这一支就是那条链的**等价物**：直接把 ESC 转给常驻主菜单（它不在场 ⇒ `false`，
                //   一扇窗都没有且没有主菜单时仍是「什么都不做」，与原来一致）。判据全文 → `MainMenuRuntime.EscapePressed`。
                //   ⚠️ **别在这里清 `_sel`** —— 主菜单那条链会 `OpenWindow` 那扇弹窗，
                //   而 `OpenWindow` → `SelectFirstIn` **刚把选中放进弹窗里**（清了就把它抹掉了）。
                return MainMenuRuntime.EscapePressed();
            }
            if (!w.IsOpen())                           // 原版 `WindowsManager.Update` 那道 `IsOpen()` 门槛（A77㉑①）
            {
                // 出声（红线：不许静默失败）—— 这一条不是缺陷，是原版的规矩：**不在 `Open` 态的窗不响应 ESC**
                Debug.Log($"[Key] ESC 不关 `{w.name}` —— 它现在不在 `Open` 态（`CurrentState = {w.CurrentState}`）；" +
                          "原版 `WindowsManager.Update` 先问 `IsOpen()`（`0x68 == 1`），为假直接 return");
                return false;
            }
            bool closed = w.ESCPressed();              // 门槛（输入层 / `closeOnEsc`）在那扇窗自己身上
            if (closed) Select(null);                  // 关掉的那扇窗里的选中作废（🆕 走 `Select` ⇒ 把选中态的视觉一起退掉）
            return closed;
        }

        /// <summary>**回车/空格** = 原版 `SendSubmitEventToSelectedObject`（`StandaloneInputModule.cs:470-484`）：
        /// 没有选中 ⇒ 直接 false；否则把 submit 事件打给选中那一颗。
        /// `Button.OnSubmit` → `Press()` → `onClick.Invoke()`（`UI/Core/Button.cs`）⇒ 我们调 `Click()`。
        /// ⚠️ **不调 `Press()`/`Release()`**：原版 submit 会闪一下按下态（`OnFinishSubmit` 协程收回来），
        ///    而**批处理里没有帧循环**能把那个态收回去；且 `Press/Release` 是鼠标那条路的东西。</summary>
        public bool KeySubmit()
        {
            var b = Selected;
            if (b == null) return false;               // 原版第一句就是这个 null 守卫
            b.Click();
            return true;
        }

        /// <summary>**方向键** = 原版 `SendMoveEventToSelectedObject`（`StandaloneInputModule.cs:511-556`）逐条：
        /// ① 两轴都 ≈0 ⇒ 计数清零、不动；② 同方向连按有节流（`m_ConsecutiveMoveCount == 1` 时等 `RepeatDelay`，
        /// 换方向或已过延迟则按 `1 / InputActionsPerSecond`）；③ `DetermineMoveDirection(x, y, 0.6f)` 得 `moveDir`；
        /// ④ 只有 `moveDir != None` 才走移动那一跳，且**计数与时间无条件更新**（原版就是写在那个分支里的）。
        /// <para>返回值：**恒 false** —— 原版 `Selectable.Navigate` 只写 `eventData.selectedObject`
        /// （`UI/Core/Selectable.cs:875-879`）而 `BaseEventData.selectedObject` 的 setter 转
        /// `EventSystem.SetSelectedGameObject`，**两支都不调 `Use()`** ⇒ `axisEventData.used` 恒 false
        /// ⇒ 原版**即使方向键真的移动了选中，回车那一跳照样跑**。这里照它。</para></summary>
        public bool KeyMove(float ax, float ay, float time = -1f)
        {
            if (time < 0f) time = Time.unscaledTime;

            // ① `GetRawMoveVector()` 之后那条「两轴都约等于 0」的守卫
            if (Mathf.Approximately(ax, 0f) && Mathf.Approximately(ay, 0f)) { _moveCount = 0; return false; }

            // ② 节流
            var move = new Vector2(ax, ay);
            bool similarDir = Vector2.Dot(move, _moveLast) > 0f;
            if (similarDir && _moveCount == 1)
            {
                if (time <= _movePrevTime + RepeatDelay) return false;
            }
            else if (time <= _movePrevTime + 1f / InputActionsPerSecond) return false;

            // ③ `GetAxisEventData(movement.x, movement.y, 0.6f)`
            int dir = DetermineMoveDirection(ax, ay, MoveDeadZone);
            if (dir == DirNone) { _moveCount = 0; return false; }

            // ④ 计数/时间**无条件**更新（原版在 `moveDir != None` 分支里就是无条件写的，
            //    哪怕 `ExecuteEvents.Execute` 因为目标为 null 而空转）
            if (!similarDir) _moveCount = 0;
            _moveCount++;
            _movePrevTime = time;
            _moveLast = move;

            // ⑤ 「没有选中」时照原版什么都不做（`ExecuteEvents.Execute(null, …)` 就是空转）——
            //    ⚠️ 但那样键盘就死了 ⇒ 这里走「我们挑的」①(b)：先补一颗默认选中（见本节头那条）。
            var from = Selected;
            if (from == null)
            {
                SelectFirst(TopWindowRoot());          // 只做「补默认选中」；移动留到下一次按键
                return false;
            }

            var to = FindInDirection(from, dir);
            if (to != null)
            {
                Select(to);
                // 🔴 **2026-10-07（A77⑮①）删掉了原来那条「选中态没有视觉反馈」的一次性告警** ——
                //    它不再是事实：选中态现在画在 `PointerLayer.Select` → `WindowButton.SetSelected` 上
                //    （原版 `Selectable.Selected` 的等价物，判据见那两个方法的注释）。
                //    ⚠️ **仍然要出的是另一件事**（见文件头那条）：`MenuDraw.Hit` 那种**透明命中区**上的色偏
                //    本来就看不见（悬停也一样）—— 那是既有账，不是这条告警要说的。
            }
            return false;
        }

        /// <summary>最上面那扇窗的根（= **原版 `Selectable` 全局表**在我们这边的「默认选中」来源）。
        /// 没有 WindowsManager / 没有窗 ⇒ null。</summary>
        static GameObject TopWindowRoot()
        {
            var wm = WindowsManager.Instance;
            var w = wm != null ? wm.TopWindow : null;
            return w != null ? w.gameObject : null;
        }

        // ---- 设备读数（原版 `InputManager` 的键位表 → 我们这边的 `Keyboard.current`）----
        // 出处 = `…/assets_full/globalgamemanagers/InputManager/InputManager_2.json`（逐条实读），见本节 ②。
        // ⚠️ 摇杆那几条（`Horizontal`/`Vertical` 的 type=2 轴、`Submit` 的 `joystick button 0`）**没接** —— 已记账。

        /// <summary>`Horizontal`：`right`/`left` + alt `d`/`a`。静止 = 0（原版数字键的 `GetAxisRaw` 就是 ±1/0）。
        /// 用 `isPressed`（按住）而不是 `wasPressedThisFrame` —— 原版那个轴**按住会重复**（节流见 `KeyMove`）。</summary>
        static float AxisX(Keyboard kb)
        {
            float v = 0f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) v += 1f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) v -= 1f;
            return v;
        }

        /// <summary>`Vertical`：`up`/`down` + alt `w`/`s`。**y 向上为正**（与 UGUI 的 `Vector3.up` 同向）。</summary>
        static float AxisY(Keyboard kb)
        {
            float v = 0f;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) v += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) v -= 1f;
            return v;
        }

        /// <summary>`Submit`：`return` / `enter`（小键盘）+ `space` —— 原版 `InputManager` 里同名的那三键，
        /// `GetButtonDown` 的语义 = **按下的这一帧**。</summary>
        static bool SubmitDown(Keyboard kb)
            => kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
               || kb.spaceKey.wasPressedThisFrame;

        /// <summary>UGUI `BaseInputModule.DetermineMoveDirection(x, y, deadZone)` 的原样复刻
        /// （`EventSystem/InputModules/BaseInputModule.cs:159-172`）：先按**圆**判死区（`sqrMagnitude < dz²`），
        /// 再 `|x| > |y|` 取左右、否则上下 —— **平手算上下**（原版就是这样，别「修正」成先判 y）。</summary>
        static int DetermineMoveDirection(float x, float y, float deadZone)
        {
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone) return DirNone;
            if (Mathf.Abs(x) > Mathf.Abs(y)) return x > 0 ? DirRight : DirLeft;
            return y > 0 ? DirUp : DirDown;
        }

        /// <summary>`Selectable.FindSelectable(Vector3 dir)` 的复刻（`UI/Core/Selectable.cs:778-860`）：
        /// ① **起点 = 该方向那条矩形边上的点**（`GetPointOnRectEdge`：中心 + `size ⊙ dir/2`，`dir` 先按
        ///    `max(|x|,|y|)` 归一 —— 四个轴向本来就已经是 1）；
        /// ② **候选 = 场上所有活着的 `WindowButton`**（原版是全局的 `Selectable.s_Selectables` 表 ——
        ///    **不是**「只在本窗里找」；⚠️ 我们 `ToBackground()` 不关物体 ⇒ 底下的窗也还在候选里，**与原版同**）；
        /// ③ 只收 `dot > 0` 的，打分 `dot / |v|²` 取最大（原版注释：「像吹气球那样沿 dir 涨大，先碰到的赢」）。
        /// <para>⚠️ 坐标系：**算在「画布像素、y 向上」里**（与 `RectTransform.rect` / `Vector3.up` 同向）——
        /// 指针那一侧的口径是 y **向下**，两处都翻一次的话方向判据会跟着错，所以统一在这一处翻。</para></summary>
        WindowButton FindInDirection(WindowButton from, int dir)
        {
            Vector2 d = dir == DirRight ? new Vector2(1f, 0f)
                     : dir == DirLeft ? new Vector2(-1f, 0f)
                     : dir == DirUp ? new Vector2(0f, 1f) : new Vector2(0f, -1f);

            Vector2 c0; Vector2 h0;
            if (!HitBox(from, out c0, out h0)) return null;
            Vector2 start = new Vector2(c0.x + d.x * h0.x, c0.y + d.y * h0.y);      // `GetPointOnRectEdge`

            float best = float.NegativeInfinity;
            WindowButton pick = null;
            foreach (var b in AllButtons())
            {
                if (b == from) continue;
                // 吸收层（A94，见 `MenuDraw.Absorb`）与「没有命中区」的都不进候选 —— 判据只在 `Navigable` 一处
                if (!Navigable(b)) continue;
                Vector2 c1; Vector2 h1;
                if (!HitBox(b, out c1, out h1)) continue;
                Vector2 v = c1 - start;
                float dot = Vector2.Dot(d, v);
                if (dot <= 0f) continue;                                            // 反方向 / 零距离 → 跳过
                float score = dot / v.sqrMagnitude;
                if (score > best) { best = score; pick = b; }
            }
            return pick;
        }

        /// <summary>一颗按钮的命中区（**画布像素、y 向上**）：中心 + 半宽半高。
        /// 指针命中（`CollectHits`，y 向下）与键盘导航（`FindInDirection`，y 向上）**共用这一份遍历** ——
        /// 「同一个件在两处各量一遍」正是迟早不一致的那一类（CLAUDE.md §三）。</summary>
        static bool HitBox(WindowButton b, out Vector2 centerUp, out Vector2 half)
        {
            centerUp = Vector2.zero; half = Vector2.zero;
            Vector2 cDown; Vector2 h;
            if (!HitBoxPx(b, out cDown, out h)) return false;
            centerUp = new Vector2(cDown.x, -cDown.y);                              // y 翻成 UGUI 口径
            half = h;
            return true;
        }

        /// <summary>一颗按钮的命中区（画布像素、**y 向下** = 原版 `m_AnchoredPosition` 那套口径）。
        /// 「哪一颗 = 命中区」的判据只在 `HitQuad` 一处。</summary>
        static bool HitBoxPx(WindowButton b, out Vector2 center, out Vector2 half)
        {
            ImageQuad q;
            return HitBoxPx(b, out center, out half, out q);
        }

        // ============================================================ 🆕 2026-10-11（A167）：命中/滚动区都要吃【父链缩放】
        //
        // 🔴 **病灶**：本函数（以及 `HitScroll`）原来一半在**世界**帧、一半在**设计**帧 ——
        //    `center` 取 `q.transform.position`（**已缩放**的视觉世界坐标）再 `ToPixel`，
        //    而 `half` 用的是 `q.WorldW/WorldH`（= **建它时传进去的那个数**，`ImageQuad` 那条
        //    「`WorldW` 不含父链缩放」的订正写着，见 `Shell/SettingsWindow.cs` 的 `Screen()` 注释）
        //    ⇒ 窗根一被 `TransformScalerBySmallScreenUI` 乘 M（`SmallScreenUI` 开关开 **且**
        //    `extraScaleSmallScreen ≠ 1`），**中心对、尺寸偏小 `1/M`**（1.2 倍窗 ⇒ 命中区只剩 83%、
        //    1.35 倍窗 ⇒ 74%）—— 症状是「看着在钮上、点不动」，**且只在开关打开时才现形**。
        //    📌 判据 = 原版 UGUI 射线：`Graphic.Raycast` 走
        //    `RectTransformUtility.RectangleContainsScreenPoint`（`UI/Core/Graphic.cs:868-930`）——
        //    它把屏幕点**逆变换进该 RectTransform 的局部空间** ⇒ **父链一缩放，命中区跟着缩**
        //    （= 「命中区永远等于**画出来**的那块」）。原版 `TransformScalerBySmallScreenUI` 乘的正是窗口根
        //    ⇒ 原版放大后命中区同比例变大。我们这一份必须等价。
        //
        // 🔴 **同族一起判过（A167 原文要求，⛔ 别只修这一处）—— 三处逐条结论**：
        //    · `Shell/MenuScroll.cs`：**类内部**（`Viewport` / `ContentX1/2` / `Offset` / `Shift` / `Place` /
        //      `ClampLo/Hi`）**一律是设计 px**，与宿主（`MenuDraw.Clip = Viewport`、内容按设计 px 摆）
        //      自洽 ⇒ **类本体一个字都不用改**；错的是它**跨到指针那一侧的边界**（`HitScroll` 拿世界 px 的
        //      指针去比设计 px 的 `Viewport`、`BeginDrag/DragTo` 的「轴坐标」同理）。**那两处就在本文件**
        //      ⇒ 本批在 `HitScroll` / `AxisOf` 里各换算一次（判据见那两个方法）。
        //    · `Shell/MenuDraw.cs` 的 `ClipRect` / `QuadRectPx`：**整条裁切管线活在设计 px**（`clip` =
        //      原版 prefab 上那个 `RectMask2D` 框、各调用方给的 `vis` 也一律设计 px）⇒ 返回设计 px 是对的；
        //      它的**位置项**那一次「世界 → 设计」的除法已在 **A298（本批前一件 W4）** 收口到
        //      `PosInDesignSpace` ⇒ **本批一字不动**（⛔ 别顺手改，那会推翻刚接好的那份口径）。
        //    · 各宿主的**几何断言**（`q.WorldW * 108f` 量宽高、`CheckRectPx` 一族）：它们比的是
        //      「设计 px vs 原版字面量」——**同帧自洽**，而自检跑在**出厂态**（开关关 ⇒ M == 1）⇒ **不需要改**；
        //      ⚠️ 真正带电的是「**拿它们去量一个被缩放过（M ≠ 1）的窗**」——A327 那几条两态夹具就是这种场合，
        //      量之前必须自己乘/除 M（本批 A327 的夹具逐处显式写了那一次换算）。
        //
        // 📌 两态断言（开关关 = 逐值不变 / 开关开 = 命中区 = 设计矩形 × M）在 `Editor/SettingsScene.cs`。

        /// <summary>同上，顺手把那一颗 quad 也交出来（`CollectHits` 要用它列候选）。</summary>
        static bool HitBoxPx(WindowButton b, out Vector2 center, out Vector2 half, out ImageQuad q)
        {
            center = Vector2.zero; half = Vector2.zero;
            q = HitQuad(b);
            if (q == null) return false;
            var p = q.transform.position;
            float k = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
            center = new Vector2(LayoutSpace.PxX(p.x), LayoutSpace.PxY(p.y));
            // 🔴 A167：尺寸项乘**父链缩放**（`lossyScale` 一路含自己那一级 ⇒ 连「靠 `localScale` 定宽」
            //    那种件也对：`Battle/SkillPanel.cs:248` 写的 `localScale = 目标宽 / WorldW` 照样还原成
            //    「世界宽 × K」）。`M == 1` 时逐位不变（开关出厂关 ⇒ 零回归）。
            //    **改坏法**：去掉这两个因子 ⇒ 在「开关开 + 窗根 ×1.2」下命中区比画出来的小一圈
            //    ⇒ `Editor/SettingsScene.cs` 的 A167 那两条（远边上的点：修好后点得中、改坏后点不中）立刻红。
            var ls = q.transform.lossyScale;
            half = new Vector2(q.WorldW * ScaleAbs(ls.x) * k * 0.5f, q.WorldH * ScaleAbs(ls.y) * k * 0.5f);
            return true;
        }

        /// <summary>取一个缩放分量的**模**（非有限 / ≈0 ⇒ 1）。
        /// ⚠️ 用绝对值：镜像过的父链 `lossyScale` 可以是**负**的，而命中区是一个**尺寸**
        /// （同族退化处置 = `MenuDraw.DivByScale`，那里是除、这里是乘）。</summary>
        static float ScaleAbs(float s)
            => (float.IsNaN(s) || float.IsInfinity(s) || Mathf.Abs(s) < 1e-6f) ? 1f : Mathf.Abs(s);

        /// <summary>一颗按钮的**命中用 quad**（没有 = 这颗不可命中/不可导航）。
        /// 🔴 **`GetComponentInChildren` 而不是 `GetComponent`**：`AddHit` 是 `ImageQuad.Create(hit, …)` 建的
        /// —— 那是**子物体**，quad 不在按钮自己那一层（2026-09-23 自检报「命中表里一个都没有」才看出来）。</summary>
        static ImageQuad HitQuad(WindowButton b)
        {
            if (b == null || !b.isActiveAndEnabled) return null;
            var q = b.GetComponentInChildren<ImageQuad>();
            if (q == null || !q.gameObject.activeInHierarchy) return null;
            return q;
        }

        /// <summary>场上所有**可用**的按钮（原版 `Selectable.s_Selectables` 的等价物）。
        /// **事件时才扫描**（不是每帧）：`FindObjectsByType` 只在真有输入时走一次。
        /// ⚠️ 用数组而不是 `List` 是为了不给每次导航分配（`CollectHits` 的注释同此）。</summary>
        static WindowButton[] AllButtons()
            => Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None);

        // ============================================================ 可被自检直调的两条入口
        // （批处理里没有输入事件 ⇒ 自检用它们量「这一处到底吃不吃得到这次输入」）

        /// <summary>在画布像素点 `(px,py)` 上滚一格（`dy` 是 `Mouse.current.scroll` 的原值，
        /// Windows 上一格 ±120）。返回**是不是真的滚到了某个区**。</summary>
        public bool WheelAt(float px, float py, float dy)
        {
            var s = HitScroll(px, py);
            if (s == null) return false;
            s.Wheel(dy);
            return true;
        }

        /// <summary>在画布像素点 `(px,py)` 上点一下（**按下与抬起同一处**的语义）。
        /// 返回**是不是真的点到了某个 `WindowButton`**（红线：点不到要能说出来，不许静默）。</summary>
        public bool ClickAt(float px, float py)
        {
            var b = HitButton(px, py);
            if (b == null) return false;
            b.Click();
            return true;
        }

        /// <summary>只做命中、不派发（自检量「这一点上是谁」时用）。</summary>
        public WindowButton ButtonAt(float px, float py) { return HitButton(px, py); }

        /// <summary>只做命中、不滚（同上）。</summary>
        public MenuScroll ScrollUnder(float px, float py) { return HitScroll(px, py); }

        /// <summary>一颗按钮算不算「可导航」—— **全类唯一判据**（`FindInDirection` 的候选集、
        /// `ButtonCountForTest`、`ButtonCountUnder` 三处都走它，别各写一份）。
        /// 两条：**不是吸收层**（`absorbOnly` —— 原版面板不是 `Selectable`，见 `MenuDraw.Absorb`）+
        /// **有命中区**（`HitQuad != null`，= 有 `ImageQuad` 且在激活链上）。</summary>
        static bool Navigable(WindowButton b) { return b != null && !b.absorbOnly && HitQuad(b) != null; }

        /// <summary>自检用：此刻**全场景**可导航的按钮数。
        /// 🔴 **2026-10-07（A77㉑⑤）就地订正（铁律 5）**：本行原来写「（= `FindInDirection` 的候选集大小）」——
        /// **与代码不符**：`FindInDirection` 遍历候选集时**把起点那一颗跳过去了**（`if (b == from …) continue;`）
        /// ⇒ 这个数 = 那个候选集 **+ 起点自己**（起点也可导航时）。
        /// ⚠️ 而且它是**全场景**计数（`FindObjectsByType`）—— 拿它当「场上只有 N 颗」的**前置断言**是**脆**的
        /// （别的宿主/别的窗只要漏关一颗就红，而红的原因与本段要断的事无关）。
        /// ⇒ 探针段一律用**子树**版本 `ButtonCountUnder`，这个数只当**上界 / 打印**（判据 → ㉑⑤）。</summary>
        public static int ButtonCountForTest
        {
            get
            {
                int n = 0;
                foreach (var b in AllButtons()) if (Navigable(b)) n++;
                return n;
            }
        }

        /// <summary>🆕 **某一棵子树里**可导航的按钮数（与 `ButtonCountForTest` **同一判据** = `Navigable`）。
        /// 为什么要它：全场景计数会被**别的窗**顶高 ⇒ 「本探针窗里只有 N 颗」这类前置断言必须按子树量；
        /// 收到子树之后它同时成了**鉴别器**（探针自己多一颗 / 少一颗命中区就红，而不是被别家的噪声掩盖）。</summary>
        public static int ButtonCountUnder(GameObject root)
        {
            if (root == null) return 0;
            int n = 0;
            var all = root.GetComponentsInChildren<WindowButton>(true);
            for (int i = 0; i < all.Length; i++) if (Navigable(all[i])) n++;
            return n;
        }

        // ------------------------------------------------------------ 🆕 悬停 / 拖拽 / 帧推（自检直调口）

        /// <summary>把指针挪到 `(px,py)` 上：**派发进/出悬停态**（原版 `IPointerEnter/Exit`）。
        /// 返回这一刻指针下的那一颗（没有就是 null）。批处理里 `Update` 不跑 ⇒ 这是唯一入口。</summary>
        public WindowButton HoverAt(float px, float py)
        {
            var h = HitButton(px, py);
            if (h == _hover) return h;
            if (_hover != null) _hover.Exit();
            _hover = h;
            if (_hover != null) _hover.Enter();
            return h;
        }

        /// <summary>自检用：此刻的悬停件（不重算）。</summary>
        public WindowButton HoveredForTest { get { return _hover; } }

        /// <summary>按下（自检直调；`Update` 里那一段的另一条入口）。
        /// 返回按到了哪一颗（决定抬起时算不算点中）。</summary>
        public WindowButton PressAt(float px, float py)
        {
            _pressed = true; _dragging = false;
            _pressPx = new Vector2(px, py);
            _down = HitButton(px, py);
            _dragScroll = HitScroll(px, py);
            Select(_down);          // 🆕 A49：与 `Update` 里那一路同一件事（原版鼠标按下即改选中），别少这一句
            if (_down != null) _down.Press();
            return _down;
        }

        /// <summary>按住移动（自检直调）。**越过 `DragThreshold` 才转成拖拽** —— 返回是不是在拖。
        /// ⚠️ 真的抬起由 `ReleaseAt` 收尾（它决定「这一下到底算点击还是算拖」）。</summary>
        public bool MoveTo(float px, float py)
        {
            if (!_pressed) return false;
            var p = new Vector2(px, py);
            if (!_dragging && _dragScroll != null && (p - _pressPx).magnitude > DragThreshold)
            {
                _dragging = true;
                _dragScroll.BeginDrag(AxisOf(_dragScroll, _pressPx));
            }
            if (_dragging) _dragScroll.DragTo(AxisOf(_dragScroll, p));
            return _dragging;
        }

        /// <summary>抬起（自检直调）。**在拖拽中 ⇒ 只结束拖、不点按钮**（照原版：
        /// 越过阈值那一下已经被 ScrollRect 吃了，按钮的 `onClick` 不会再派发）。
        /// 返回这一下**有没有真的点中**。</summary>
        public bool ReleaseAt(float px, float py)
        {
            bool fired = false;
            if (_dragging)
            {
                if (_dragScroll != null) _dragScroll.EndDrag();
            }
            else
            {
                var up = HitButton(px, py);
                fired = up != null && up == _down;
                if (fired) up.Click();
            }
            if (_down != null) _down.Release();
            _pressed = false; _down = null; _dragScroll = null;
            return fired;
        }

        /// <summary>推一帧惯性/回弹（自检直调；Play 里由 `Update` 每帧调）。
        /// 返回**有没有任何一个滚动区动过**。⚠️ `dt<=0` 直接返回 false（照 UGUI 那条 `deltaTime > 0` 守卫）。</summary>
        public bool TickAt(float dt)
        {
            if (dt <= 0f) return false;
            bool moved = false;
            for (int i = _scrolls.Count - 1; i >= 0; i--)
            {
                var s = _scrolls[i];
                if (s == null || s.Owner == null) { _scrolls.RemoveAt(i); continue; }
                if (s.Tick(dt)) moved = true;
            }
            return moved;
        }

        // ============================================================ 命中

        /// <summary>命中顺序：**渲染队列大的先**（画在上面的先吃），同队列再比 `z`（越小越靠前）。</summary>
        WindowButton HitButton(float px, float py)
        {
            WindowButton best = null;
            int bestQ = int.MinValue;
            float bestZ = float.MaxValue;
            foreach (var c in CollectHits(px, py))
            {
                var q = c.q; float z = q.transform.position.z;
                if (q.RenderQueue > bestQ || (q.RenderQueue == bestQ && z < bestZ))
                { best = c.btn; bestQ = q.RenderQueue; bestZ = z; }
            }
            return best;
        }

        /// <summary>🆕 **2026-10-07（A77⑮④）：这一颗现在吃得到指针吗** —— 它所属的那扇窗（有的话）必须是 `Open`。
        ///
        /// <para>🔴 **判据（原版）**：被压到背景的窗**根本不在射线的最上面**。`GameWindow.ToBackground()`
        /// 只写状态、**不关物体**（`GameWindow__ToBackground.c` 全body = `*(undefined4*)(this+0x68) = 2;`
        /// —— 我们那份逐字同），而**新窗的 `Menu Dark Background` 是一颗整屏的射线靶**
        /// （全库 **88** 个 `BackgroundCloseButton`，**88/88 的宿主都带 `Image.m_RaycastTarget = 1`**，
        /// 其中 85 个节点就叫 `Menu Dark Background`；判据 → `资料/待办判据_1006.md` 的 A84② 那一段）
        /// ⇒ 底窗那一片的点击**永远落在上层的压暗层 / 面板上**，落不到底窗的按钮。</para>
        ///
        /// <para>🔴 **我们为什么非拦这一道**：本工程挑赢家是**按渲染队列**（`HitButton`），而队列是**逐窗**定的
        /// ⇒ 底窗某个内容档可能**高过**上层压暗层那一档 —— 实测例：`PlayerProfileWindow.QHit = 3155` ＞
        /// `PromptPopup.QShade = 3140`、＞ `DailyRewardPopup.QShade = 3002`。不拦 ⇒ **弹窗盖着全屏窗时，
        /// 点到的是下面那扇的钮**（2026-10-07 查出，属「既有行为、非某批引入」）。</para>
        ///
        /// <para>⚠️ **只管【指针】这一条路**：原版 `Selectable.s_Selectables` 是**全局表**、`ToBackground()` 不碰它
        /// ⇒ 方向键的候选集**仍然含底窗的钮**（`FindInDirection` 一个字没改 —— 见它那句「与原版同」）。</para>
        ///
        /// <para>⚠️ **只拦 `Background` 这一态、不拦 `Closed`**：`Closed` 的窗本来就 `SetActive(false)`
        /// （`GameWindow.Close`），它的钮早就进不了命中表；而「刚 `Create` 出来、还没 `OpenWindow`」的窗
        /// 也停在这个态上 —— 那种窗不该被这条规矩影响（判据是「**被压到背景**」，不是「没开着」）。</para></summary>
        static bool PointerReachable(WindowButton b)
        {
            var w = b.GetComponentInParent<GameWindow>(true);
            return w == null || w.CurrentState != WindowState.Background;   // 不属于任何窗（外壳顶栏 / 主菜单那棵树）⇒ 一直可点
        }

        static bool _saidBgSkip;

        /// <summary>这一点上**所有**候选命中区（未排序）。`HitButton`（挑赢家）与
        /// `HitLines`（点击记录里列候选）**共用这一份遍历** —— 两处各写一遍迟早不一致。</summary>
        static List<(WindowButton btn, ImageQuad q)> CollectHits(float px, float py)
        {
            var list = new List<(WindowButton, ImageQuad)>();
            // **事件时才扫描**（不是每帧）：`FindObjectsByType` 只在真有滚轮/按下/抬起时走一次。
            // 🔴 遍历与「哪一颗算命中区」**收口在 `AllButtons` / `HitQuad` / `HitBoxPx`** ——
            //    键盘导航（`FindInDirection`）用的是**同一份**；两处各写一遍 = 迟早不一致（CLAUDE.md §三）。
            var all = AllButtons();
            for (int i = 0; i < all.Length; i++)
            {
                // 🆕 A77⑮④：被压到背景的窗**不参与指针命中**（判据 → `PointerReachable`）
                if (!PointerReachable(all[i]))
                {
                    if (!_saidBgSkip)     // 只说一次（红线：不许静默失败 —— 但也不能每次点击刷屏）
                    {
                        _saidBgSkip = true;
                        Debug.Log("[Key] 指针命中跳过了**被压到背景**的窗里的按钮（`" + (all[i] != null ? all[i].name : "<null>")
                                  + "` 等）—— 原版那一刻它们被上层的整屏压暗层盖住（见 `PointerReachable` 的判据）。"
                                  + "这项**只影响指针**：方向键的候选集照原版仍是全局的。");
                    }
                    continue;
                }
                Vector2 c, half; ImageQuad q;
                if (!HitBoxPx(all[i], out c, out half, out q)) continue;
                if (Mathf.Abs(px - c.x) > half.x) continue;
                if (Mathf.Abs(py - c.y) > half.y) continue;
                list.Add((all[i], q));
            }
            return list;
        }

        /// <summary>命中的滚动区 = **后登记的优先**（后开的窗盖在前面的窗上）；
        /// **已经切走 / 关掉的页里的区跳过**（页签切换是 `SetActive`，那些区还留在表里）；
        /// 🆕 **宿主已经销毁的条目直接删掉**（原来只判 `!= null` ⇒ Unity 的假 null 让它**判不出**，
        /// 死条目照样能被滚轮命中）。判据 → `项目任务.md` §〇 A ②。
        /// 🆕 **2026-10-11（A167）**：`Viewport` 是**设计 px**、`px/py` 是**世界 px** ⇒ 比之前先把视口
        /// 换算到指针那一帧（`DesignToPtrPx`，判据见它）。**改坏法**：不换算 ⇒ 开关开着的窗里视口的
        /// 命中范围比画出来的小 `1/M`（1.2 倍窗 ⇒ 边缘 17% 只是「滚不动」，**静默**）。</summary>
        MenuScroll HitScroll(float px, float py)
        {
            for (int i = _scrolls.Count - 1; i >= 0; i--)
            {
                var s = _scrolls[i];
                if (s == null || s.Owner == null) { _scrolls.RemoveAt(i); continue; }   // 死的 ⇒ 删
                if (!s.Owner.activeInHierarchy) continue;                                // 关着的 ⇒ 跳过（还会回来）
                var v = DesignToPtrPx(s.Viewport, ScrollScale(s));
                if (px >= v.x1 && px <= v.x2
                    && py >= v.y1 && py <= v.y2) return s;
            }
            return null;
        }
    }
}
