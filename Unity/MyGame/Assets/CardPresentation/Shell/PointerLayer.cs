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
// 所以它对自检完全无副作用；自检要验就直调 `ClickAt`/`WheelAt`）：
//   ① 滚轮 → 命中哪个滚动区就滚哪个（`MenuScroll.Wheel`）
//   ② 左键 → **按下与抬起落在同一个 `WindowButton` 上**才算点中（UGUI 的语义），调它的 `onClick`
// 命中顺序：**渲染队列大的先**（画在上面的先吃），同队列再比 `z`（越小越靠前 —— 照 `CardInteraction.HitTest`）。
//
// ⚠️ **本轮没实现的（出声，不静默）**：拖拽滚动 · 惯性/回弹（要有帧循环，且这一版没做）· 右键 · 键盘 ·
//    按住移开后松手不触发（已按 UGUI 语义做了）之外的其他交互。**这几条写在 `项目任务.md` §三 第 15 条。**
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

        /// <summary>登记一个滚动区（滚轮才会找到它）。**窗口重开时旧的要 `Clear`**，见 `ResetForWindow`。</summary>
        public static void RegisterScroll(MenuScroll s)
        {
            if (Instance == null || s == null) return;
            if (!Instance._scrolls.Contains(s)) Instance._scrolls.Add(s);
        }

        /// <summary>清掉全部滚动区登记（换窗/关窗时调 —— 不然后面开的窗会滚到已经没了的区）。</summary>
        public static void ClearScrolls()
        {
            if (Instance != null) Instance._scrolls.Clear();
        }

        void Update()
        {
            HandleTyping();                 // 键盘先吃（在编辑文本时鼠标照样能点，两条不互斥）

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 wp = LayoutSpace.ScreenToWorld(mouse.position.ReadValue(), LayoutSpace.Cam);
            Vector2 px = LayoutSpace.ToPixel(wp);

            float dy = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(dy) >= 1f) WheelAt(px.x, px.y, dy);

            if (mouse.leftButton.wasPressedThisFrame) _down = HitButton(px.x, px.y);
            else if (mouse.leftButton.wasReleasedThisFrame)
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
                _down = null;
            }
        }

        /// <summary>把「命中 / 候选 / 派发没派发」三样交给 `ClickLog`。</summary>
        static void LogHit(WindowButton up, bool sameSpot, bool fired, float px, float py)
        {
            if (up == null) return;      // 空点：`ClickLog.Begin` 已经记了点位，正文会写「这一点上没有命中」
            var q = up.GetComponentInChildren<ImageQuad>();
            ClickLog.Hit("命中 `" + up.name + "`" + PathOf(up.transform)
                         + " · 队列 " + (q != null ? q.RenderQueue.ToString() : "?")
                         + " · z " + (q != null ? q.transform.position.z.ToString("F3") : "?"),
                         up.onClick == null ? "🔴 **这个命中区没有绑动作**（`onClick == null`）" : null);
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

        /// <summary>这一点上**所有**候选命中区（未排序）。`HitButton`（挑赢家）与
        /// `HitLines`（点击记录里列候选）**共用这一份遍历** —— 两处各写一遍迟早不一致。</summary>
        static List<(WindowButton btn, ImageQuad q)> CollectHits(float px, float py)
        {
            var list = new List<(WindowButton, ImageQuad)>();
            // **事件时才扫描**（不是每帧）：`FindObjectsByType` 只在真有滚轮/按下/抬起时走一次。
            var all = Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b == null || !b.isActiveAndEnabled) continue;
                // 🔴 **`GetComponentInChildren` 而不是 `GetComponent`**：`AddHit` 是
                //    `ImageQuad.Create(hit, …)` 建的 —— 那是**子物体**，quad 不在按钮自己那一层
                //    （2026-09-23 自检报「命中表里一个都没有」，诊断打出 `[ClaimHit 无quad]` 才看出来）。
                var q = b.GetComponentInChildren<ImageQuad>();
                if (q == null || !q.gameObject.activeInHierarchy) continue;
                var p = q.transform.position;
                float hw = q.WorldW * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight) * 0.5f;
                float hh = q.WorldH * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight) * 0.5f;
                if (Mathf.Abs(px - LayoutSpace.PxX(p.x)) > hw) continue;
                if (Mathf.Abs(py - LayoutSpace.PxY(p.y)) > hh) continue;
                list.Add((b, q));
            }
            return list;
        }

        /// <summary>命中的滚动区 = **后登记的优先**（后开的窗盖在前面的窗上）；
        /// **已经切走 / 关掉的页里的区跳过**（页签切换是 `SetActive`，那些区还留在表里）。</summary>
        MenuScroll HitScroll(float px, float py)
        {
            for (int i = _scrolls.Count - 1; i >= 0; i--)
            {
                var s = _scrolls[i];
                if (s == null) { _scrolls.RemoveAt(i); continue; }
                if (s.Owner != null && !s.Owner.activeInHierarchy) continue;
                if (px >= s.Viewport.x1 && px <= s.Viewport.x2
                    && py >= s.Viewport.y1 && py <= s.Viewport.y2) return s;
            }
            return null;
        }
    }
}
