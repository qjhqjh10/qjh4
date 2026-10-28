// WindowHeaderWithBackButton.cs — 事件窗表头（标题 + 返回钮 + 逐个事件 UI 抽屉）
//
// ============================ 判据（全部现读） ============================
// 类：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/WindowHeaderWithBackButton.cs`
//     字段 4 个：`backButton`(`EverguildButton`) · `titleText`(`EverguildTextMeshPro`) ·
//     `eventUIDrawers`(`LiveopEventUIDrawers[]`) · `OnBackButtonPressed`(`Action`)
//     `OnEnable` = `backButton.m_OnClick.AddListener(BackButtonPressed)`；
//     `OnDisable` = 同形 `RemoveListener`；`BackButtonPressed` = 有回调就调。
//     `Initialize(string title, Action onBack)` = 存回调 + **`titleText.text = title`**（⚠️ 直接赋 `.text`，
//        不是 `SetText`/本地化）。
//     `Initialize(ILiveOps, Action)` 体 = `d:/2/tools/decomp_full/WindowHeaderWithBackButton__Initialize.c` **49 行**逐句：
//       ① `if (liveopEvent == null) { CustomDebug.LogError("[ERROR] Trying to initialize WindowHeaderWithBackButton
//          with null event"); return; }`
//       ② 逐个 `eventUIDrawers[i].Initialize(liveopEvent)`（`+0x30` 数组 · 虚槽 `+0x178`）
//       ③ `label = LiveOpsAssetUtility.GetLabel(baseData, Title(0), Array.Empty<object>())`
//          （`.c` 里是 `FUN_1800021f0(0xf, …)` → `LiveOpsAssetUtility__GetLabel`；`0xf` 是装箱/取 baseData 那一步的常量，
//           枚举值本身按 ILSpy + 账 822 记的 **Title = 0**）
//       ④ 存 `onBack`（`+0x38`）
//       ⑤ `titleText.text = label`（虚槽 `+0x558` = `set_text`）
// 实例（**原版 8 个 prefab**，本轮用「`.cs.meta` guid → grep 导出工程」现读，guid = `6d74726afb8bab28201edc2a0ace1fa8`）：
//   `RankedEventWindowV2` · `EnergySinglePlayerOnlyEventWindow` · `Two Sides Event Window` · `SkirmishModeEventWindow` · `Tutorial Mode Menu` ·
//   `Draft Mode Menu Demo` · `Draft Mode Timed Mode Window` · `Daily Streak Popup`（⚠️ 原文写「7 处」，2026-10-28 现读 8 处并补 `Tutorial Mode Menu`）
//
// ============================ 我们的对应 / 🔶 自己设计（非原版）============================
//   ① `EverguildButton` → 本仓 **`WindowButton`**（`onClick` 是 `System.Action` 字段 ⇒ 用 `+=` / `-=` 实现
//      原版 `AddListener` / `RemoveListener` 的**多播**语义）。
//   ② `EverguildTextMeshPro` → 本仓 **`Label`**（它没有 `.text` setter ⇒ 走 `SetText`，机械等价）。
//   ③ `LiveopEventUIDrawers[]` → 我们**没有这个类**（账 416 未建）⇒ 这里用一个最小的
//      <see cref="IWindowHeaderUIDrawer"/> 占位接口，`null` 时**出声**（原版空数组就不进循环；
//      原版**数组字段为 null 会 NRE**，我们不照抄那个崩）。
//   ④ `GetLabel(baseData, Title, [])` → 行为**照原版**（取不到标签 ⇒ 写**空串**，判据 `Shell/BaseEventPopup.cs:42`）；
//      🔶 自己设计（非原版）的只有**接线方式**：`LiveOpsLocalCatalog` 里那套取器正在被重构
//      （`GetEventLabel`/`GetAssetReference` 已搬到别处）⇒ 本件不直接依赖它，改留静态接线口 <see cref="TitleTermResolver"/>，
//      `null`（默认）= **待接** ⇒ 标题写空串 + 出声（与原版空串档同一语义）。
//   ⑤ 原版 `Initialize(string, Action)` 在 `titleText` 为 null 时**会 NRE**；我们改成 `LogError` + 继续
//      （崩溃不是「静默失败」，但把整扇壳带下去不值当 —— 如实标成我们的差别）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `LiveopEventUIDrawers` 的**最小占位**：表头只用到它的 `Initialize(liveopEvent)` 那一跳。
    /// <para>⚠️ 真正的那个类还没建（账 416 `LiveopUIDrawer_GameModeIcon.Initialize` 判据有缺口）。
    /// 建出来之后让它实现本接口即可，⛔ 别另立一套。</para></summary>
    public interface IWindowHeaderUIDrawer
    {
        void Initialize(ILiveOps liveopEvent);
    }

    /// <summary>原版 `WindowHeaderWithBackButton`（判据见文件头）。
    /// <para>用法（接线方）：建/取到返回钮与标题后 `Bind(back, title)`（会挂上点击），再 `Initialize(...)`。</para>
    /// <para>⚠️ 编辑模式下 `AddComponent` 不触发 `OnEnable` ⇒ <see cref="Bind"/> 里**显式**挂钩（不依赖生命周期）。</para></summary>
    public class WindowHeaderWithBackButton : MonoBehaviour
    {
        /// <summary>原版 `backButton`（`EverguildButton`）→ 我们的 `WindowButton`。</summary>
        public WindowButton backButton;
        /// <summary>原版 `titleText`（`EverguildTextMeshPro`）→ 我们的 `Label`。</summary>
        public Label titleText;
        /// <summary>原版 `eventUIDrawers`（`LiveopEventUIDrawers[]`）→ 占位接口数组（可为 null）。</summary>
        public IWindowHeaderUIDrawer[] eventUIDrawers;

        /// <summary>原版 `OnBackButtonPressed`。</summary>
        public Action OnBackButtonPressed;

        bool _hooked;

        /// <summary>原版那一跳 `GetLabel(baseData, Title, [])` 的**等价接线口**（给事件 → 标题词条键）。
        /// <para>🔶 自己设计（非原版）：接线口本身（行为照原版：取不到 ⇒ 空串 + 出声）。默认 `null` = **待接**，接线方设一次即可。</para></summary>
        public static Func<ILiveOps, string> TitleTermResolver;

        /// <summary>自检读口：现在标题那格显示的字（没绑 `Label` ⇒ `null`）。</summary>
        public string TitleForTest { get { return titleText != null ? titleText.Text : null; } }
        /// <summary>自检读口：返回钮点击有没有挂上（= 原版 `OnEnable` 那一跳）。</summary>
        public bool HookedForTest { get { return _hooked; } }

        /// <summary>把构件接上并挂点击（= 原版 `OnEnable` 的 `AddListener`，但不依赖生命周期）。幂等。</summary>
        public void Bind(WindowButton back, Label title, IWindowHeaderUIDrawer[] drawers = null)
        {
            Unhook();
            backButton = back;
            titleText = title;
            eventUIDrawers = drawers;
            Hook();
        }

        /// <summary>原版 `OnEnable()`：`backButton.m_OnClick.AddListener(BackButtonPressed)`。</summary>
        public void Hook()
        {
            if (_hooked || backButton == null) return;
            backButton.onClick += BackButtonPressed;      // `+=` = 原版 `AddListener` 的多播语义
            _hooked = true;
        }

        /// <summary>原版 `OnDisable()`：同形 `RemoveListener`。</summary>
        public void Unhook()
        {
            if (!_hooked || backButton == null) return;
            backButton.onClick -= BackButtonPressed;
            _hooked = false;
        }

        void OnEnable() { Hook(); }
        void OnDisable() { Unhook(); }

        /// <summary>原版 `BackButtonPressed()`：有回调就调，没有就什么都不做（`.c` 里就是 `if (OnBackButtonPressed != null)`）。</summary>
        public void BackButtonPressed()
        {
            var cb = OnBackButtonPressed;
            if (cb != null) cb();
        }

        /// <summary>原版 `Initialize(string title, Action OnBackButtonPressed)`（ILSpy 体 4 句）：
        /// 存回调 + **直接赋 `.text`**（不是本地化 `SetText`）。
        /// ⚠️ 原版 `titleText` 为 null 会 NRE；我们 `LogError` 后继续（文件头 ⑤）。</summary>
        public void Initialize(string title, Action onBackButtonPressed)
        {
            OnBackButtonPressed = onBackButtonPressed;
            if (titleText == null)
            {
                Debug.LogError("[WindowHeader] `Initialize(string, Action)`：`titleText` 没绑 ⇒ 标题写不进去"
                               + "（原版此处会 NRE；我们出声后继续）");
                return;
            }
            titleText.SetText(title ?? "");       // = 原版 `titleText.text = title`（我们 `Label` 只有 `SetText`）
        }

        /// <summary>原版 `Initialize(ILiveOps liveopEvent, Action OnBackButtonPressed)`
        /// （判据 = `decomp_full/WindowHeaderWithBackButton__Initialize.c` 全 49 行，五步见文件头）。</summary>
        public void Initialize(ILiveOps liveopEvent, Action onBackButtonPressed)
        {
            if (liveopEvent == null)
            {
                // 原版逐字：`[ERROR] Trying to initialize WindowHeaderWithBackButton with null event`
                Debug.LogError("[ERROR] Trying to initialize WindowHeaderWithBackButton with null event");
                return;
            }

            // ② 逐个抽屉喂事件（原版空数组就跳过；数组为 null 时原版 NRE ⇒ 我们出声后跳过）
            if (eventUIDrawers != null)
            {
                for (int i = 0; i < eventUIDrawers.Length; i++)
                {
                    var d = eventUIDrawers[i];
                    if (d != null) d.Initialize(liveopEvent);
                    else Debug.LogWarning("[WindowHeader] `eventUIDrawers[" + i + "]` 是 null ⇒ 那一格没初始化（原版此处 NRE）");
                }
            }

            // ③ 标题 = 事件自己的 `Title` label（`GetLabel(baseData, Title(0), [])`）
            string label = null;
            var resolver = TitleTermResolver;
            if (resolver != null)
            {
                string term = resolver(liveopEvent);
                if (!string.IsNullOrEmpty(term)) label = Loc.T(term);      // 词条键 → 文本（`Loc.T` 缺键时原样返回）
            }
            else
            {
                Debug.LogWarning("[WindowHeader] `Initialize(ILiveOps, …)`：**标题词条解析器没接**"
                                 + "（原版走 `LiveOpsAssetUtility.GetLabel(baseData, Title, [])`）⇒ 标题写空串，如实说");
            }

            // ④ 存回调
            OnBackButtonPressed = onBackButtonPressed;

            // ⑤ 写标题（原版直接赋 `.text`；我们 `SetText`）
            if (titleText == null)
            {
                Debug.LogError("[WindowHeader] `Initialize(ILiveOps, …)`：`titleText` 没绑 ⇒ 标题写不进去（原版此处 NRE）");
                return;
            }
            titleText.SetText(label ?? "");        // 原版 `GetLabel` 取不到 ⇒ 写空串（⛔ 不编文案）
        }

        // ============================================================ 接线（B89-E29 · 原版 8 个 prefab，本批接 6 处）
        //
        // 原版那 7 扇窗（guid `6d74726a…`）都是在自己的 `Open` 里拿这颗组件调
        // `header.Initialize(liveopEvent, onBack)`。我们这边窗头是**运行时建**的
        // （`WindowHeader.WithBackButton` 或各窗内联建）⇒ 建完把组件挂上去、`Bind` + `Initialize`。
        //
        // 🔴 **为什么必须清掉返回钮原来的 `onClick`**：`WindowHeader.WithBackButton` 建窗头时已经把
        // `Spec.OnBack` 直接赋给了 `WindowButton.onClick`（`MenuWindowBase.cs` 里那两句），而原版的点击链
        // **只有组件这一条**（`OnEnable` 的 `AddListener(BackButtonPressed)` → `OnBackButtonPressed`）。
        // 不清掉就会 **点一下走两条路**（同一次 `Close()` 调两遍 —— 在 `Daily Streak` 那种
        // 「先 `StreakAutoCollect` 再 `Close`」的链上会**收两次**）。
        // ⇒ 清掉 `onClick` 之后，唯一驱动就是本组件的 <see cref="OnBackButtonPressed"/>。

        /// <summary>把一颗**已经建好的**窗头接到本组件上（= 原版 prefab 上常驻的这颗组件 + `Open` 里那句
        /// `Initialize(liveopEvent, onBack)`）。返回挂上去的组件实例。
        ///
        /// <para><paramref name="liveopEvent"/> 非空 ⇒ 走原版 `Initialize(ILiveOps, Action)`（标题经
        /// <see cref="TitleTermResolver"/> 解析）；为空 ⇒ 走 `Initialize(string, Action)`，标题用
        /// <paramref name="titleText"/>（本窗已按**原版同一条词条键**解析好的字）。
        /// ⚠️ 我们这 6 扇窗**都没有 `ILiveOps` 实例**（本地等价物是 `LiveOpsLocalEvent` DTO，不是 `ILiveOps`）
        /// ⇒ 实际都走后者，标题**不会**是空的；<see cref="TitleTermResolver"/> 仍然装上（给将来真有事件的窗用）。</para>
        ///
        /// <para>幂等：同一颗 <paramref name="headerRoot"/> 重复调用只会 `Bind` 一次（`Bind` 内含 `Unhook`）。</para></summary>
        public static WindowHeaderWithBackButton WireHeader(Transform headerRoot, WindowButton back, Label title,
                                                           Action onBack, string titleText, ILiveOps liveopEvent = null)
        {
            if (headerRoot == null)
            {
                Debug.LogError("[WindowHeader] 接线失败：`headerRoot` 是 null ⇒ 本窗表头组件没挂上（原版这一处必有）");
                return null;
            }
            var hdr = headerRoot.GetComponent<WindowHeaderWithBackButton>();
            if (hdr == null) hdr = headerRoot.gameObject.AddComponent<WindowHeaderWithBackButton>();
            if (back != null) back.onClick = null;      // ⛔ 别让 `Spec.OnBack` 与组件各走一遍（见上）
            hdr.Bind(back, title);
            if (back == null)
                Debug.LogError("[WindowHeader] `WireHeader`：返回钮上没找到 `WindowButton` ⇒ 点不动（原版这一颗必有）");
            if (liveopEvent != null) hdr.Initialize(liveopEvent, onBack);
            else hdr.Initialize(titleText ?? "", onBack);
            return hdr;
        }

        /// <summary>按原版节点路径接线 —— 给**拿不到 <see cref="WindowHeader.Parts"/>**（父类私有建法 ⇒ 只能在
        /// 子类 `BuildExtras` 里补线）的宿主用。路径 = `<paramref name="headerRootName"/>` →
        /// `<c>Header Background/Window Title</c>`（标题 `Label`）与 `<c>BackHit</c>`（返回钮命中区，`WindowButton` 在它身上）。</summary>
        public static WindowHeaderWithBackButton WireByPath(Transform windowRoot, string headerRootName,
                                                            Action onBack, string titleText, ILiveOps liveopEvent = null)
        {
            var hdr = (windowRoot != null && !string.IsNullOrEmpty(headerRootName)) ? windowRoot.Find(headerRootName) : null;
            if (hdr == null)
            {
                Debug.LogError("[WindowHeader] `WireByPath` 找不到窗头节点 `" + headerRootName
                               + "` ⇒ 本窗表头没接线（原版这一处必有）");
                return null;
            }
            var titleNode = hdr.Find(WindowHeader.PlateName + "/" + WindowHeader.TitleName);
            var title = titleNode != null ? titleNode.GetComponent<Label>() : null;
            var hitNode = hdr.Find(WindowHeader.BackHitName);
            var wb = hitNode != null ? hitNode.GetComponent<WindowButton>() : null;
            if (title == null)
                Debug.LogError("[WindowHeader] `WireByPath` 找不到标题 `" + WindowHeader.PlateName + "/" + WindowHeader.TitleName + "`");
            if (wb == null)
                Debug.LogError("[WindowHeader] `WireByPath` 找不到返回钮命中区 `" + WindowHeader.BackHitName + "`（`WindowButton`）");
            return WireHeader(hdr, wb, title, onBack, titleText, liveopEvent);
        }

        /// <summary>装上「事件 → 标题词条键」的解析器（= 原版 `LiveOpsAssetUtility.GetLabel(baseData, Title, [])`）。
        /// <para>🔶 自己设计（非原版）：**装法**——本组件不直依赖 `LiveOpsLocalCatalog`（那个文件正被搬，上一版因此断过编译）
        /// ⇒ 由调用方把一份通用解析器塞进来（今天 = `ParentEventWindow.ResolveHeaderTitleTerm`，
        /// 那扇窗本来就依赖本地活动目录）。<paramref name="resolver"/> 为 null ⇒ 什么都不做（保留现状）。</para></summary>
        public static void InstallTitleTermResolver(Func<ILiveOps, string> resolver)
        {
            if (resolver != null) TitleTermResolver = resolver;
        }
    }
}
