// DeckEditingWindow.cs — 卡组编辑那一扇窗（原版 **`DeckEditingWindow : GameWindow`**）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_第二十四会话/D10_卡组编辑改成窗_施工件.md` §一（原版真身，2026-10-13 现读）+ §二（最小改动）。
// 本文件 = `A1132` 六步方案的**第 1 步**：**纯新增窗、⛔ 零生产调用**（见文件末「本步不做什么」）。
//
// ============================ 原版真身（照抄，逐条有出处） ============================
//   · **类** = `DeckEditingWindow : GameWindow`（**直接子类**，不是 `GameWindowWithTabs`）。
//   · **开窗参数**：`windowsPlacement = 10 (World)` · `type = 0 (Fullscreen)`
//     —— 🔴 **这两个值原版只有 prefab 序列化字段有、代码里没赋**（ctor 没赋）⇒ 我们**必须显式赋**
//     （`GameWindow` 契约：哨兵会 `Debug.LogError` 并兜底，见 `UnsetType` / `UnsetPlacement`）。
//   · **不是模态**（`type = Fullscreen`，不是 `PopupGameWindow`）；`GameWindowOptions` 不传
//     （`SelectDecksTab__CreateDeck.c:28` 只给 `closeAll = false`）。
//   · **根 prefab 名** = `Deck Editing Menu`，唯一子件 `Content Area` 四周内缩 `167.175 / 70.94`。
//     ⚠️ **那棵 prefab 树我们【不重建】** —— `Root = transform` 就是本窗窗根，内容由 `DeckRuntime`
//     自己建在它下面（见 `Open()`）。D10 §二那句「`DeckRuntime` 结构一行都不用改」说的就是这个。
//   · **ESC = 保存**：原版**覆写了整条** `ESCPressed` ——
//     `d:/2/tools/decomp_full/DeckEditingWindow__ESCPressed.c` 全文只有一句
//     `DeckEditingWindow__TrySaveDeck(param_1, 0);`（无 `base` 调用）
//     ⇒ 基类那条「ESC 关窗」的**两道门槛（输入层 / `closeOnEsc`）与末尾的 `Close()` 都不跑**。
//   · **开**（原版）：`SelectDecksTab__CreateDeck.c:28`
//     `WindowsManager.OpenWindow<DeckEditingWindow>(wm, deck, closeAll:false)`（收藏窗 Decks 页的
//     Create/Edit 走同一个类）；`TryOpen` 开头 `NavigationPanelController.Toggle(0,1,0)` = 收起左竖导航。
//   · **关**（原版）：`DeckEditingWindow__Close.c:19-20`：`Toggle(1,1,0)`（放回导航栏）→ `GameWindow.Close()`；
//     随后若 `wm.currentWindow` 是 `CollectionScreen` ⇒ 回到底下那扇一直开着的收藏窗（页签从未被切过）。
//
// ============================ 进度（口径见 D10 §三那张表） ============================
//   ✅ **第 1 步**（纯新增窗）：本类建成。
//   ✅ **第 2 步**（本窗自我豁免）：`DeckRuntime.HostWindow` + `OtherWindowUp()` 里那一句豁免；
//      本文件 `Open()` 里 `_runtime.HostWindow = this;` 就是登记处。
//      ⚠️ 这两步**必须同批**：只做第 3 步不做第 2 步 ⇒ 本窗自己就是 `wm.TopWindow`
//      ⇒ 那道闸**恒真** ⇒ **窗一开，整个窗的点击 / 拖拽 / ESC 全死**。
//   ✅ **第 3 步**（入口改开窗）：三处入口改调本文件的 `OpenFrom(mgr)`（`LoadScene` 降级成兜底）。
//   ⛔ **第 4 步**（回程）：`DeckRuntime.BackToMenu()` **一个字都没动** ——
//      ⇒ **今天关掉本窗【不会】回主菜单 / 不会回收藏窗**（那一跳还是「在壳底下换场景」）。
//      第 4 步必须补：`BackToMenu` → 关本窗（`HostWindow.Close()`），并收拾
//      `MainMenuRuntime` 的 `PendingReturn` / `Editor/DeckScene.cs` 那 5 条 A855 断言。
//      🔴 **2026-10-17 就地订正（铁律 5 · `U4` 现核）：上面这条【已过期】** —— 第 4 步
//      （2026-10-14）已落地：`Deck/DeckRuntime.cs:4997` 走 `LeaveByHostWindow()`（关窗不切场景），
//      `:5027-5062` 收拾了回程。第 3 步（三处入口）也已接（`CollectionWindow.cs:3622/3698`·
//      `LiveOpsEventWindow.cs:1163`）。⛔ 别再照这三行去「补第 4 步」。
//   ✅ **第 5 步**（遮让）：**已被 `D10 §三` 证伪取消**（`OpenWindow` 全屏支已经 `HideAllWindows`）。
//   ✅ **第 6 步**：`DeckEditor.unity` 的定位已按 D10 §三 定（它只剩自检/兜底用途）。
//
// ---- ⚠️ 如实标注、⛔ 别当成已复刻的一处 ----
//   ① **原版那一堆 `[SerializeField]`（约 20 个）与它们带的行为一个都没接**
//      （拖拽落点、上传、导航面板…）—— 它们今天全在 `DeckRuntime` 里，由**第 4 步**决定怎么并进来。
//   （原来这里还有 ② 「关窗只做了 `GameWindow` 那一套」—— 已并进上面「第 4 步」那一格，⛔ 不留两份。）
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡组编辑那一扇窗（原版 `DeckEditingWindow`）。**内容全部由 `DeckRuntime` 建在本窗根下** ——
    /// 本类只负责三件事：① 照原版 `Create()` 两个必填参数建窗 · ② `Open()` 里挂 `DeckRuntime` 并 `Build`
    /// · ③ 照原版把 ESC 覆写成「保存」。出处 → 本文件头。</summary>
    public class DeckEditingWindow : GameWindow
    {
        /// <summary>原版 prefab 根名（`bundle_menus_assets_all` 的 `Deck Editing Menu`，
        /// 248 节点 / 最深 12 层 —— 留痕 `资料/普查产出_第二十四会话/D10_prefab树.md`）。
        /// 🔴 **我们这棵树只借它这个名字**（自检按名字认这一扇），⛔ 不代表我们重建了那 248 个节点。</summary>
        public const string NodeName = "Deck Editing Menu";

        /// <summary>本窗那台 `DeckRuntime`（`Open()` 建；自检读它断「内容真建起来了」）。</summary>
        public DeckRuntime Runtime { get { return _runtime; } }
        DeckRuntime _runtime;

        /// <summary>建一扇（**⛔ 不接线**：本步生产路径零调用）。
        /// 形状照同族先例 `Shell/DeckInfoPopup.cs` 的 `Create`（`new GameObject` → `AddComponent`
        /// → 显式赋 `type`/`placement` → `AttachToAnchor`）。</summary>
        public static DeckEditingWindow Create(WindowsManager mgr)
        {
            var go = new GameObject(NodeName);
            var win = go.AddComponent<DeckEditingWindow>();

            // 🔴 **这两个值必须显式赋**（原版只有 prefab 序列化字段有、代码里没赋）——
            //    `AttachToAnchor` 对哨兵会 `Debug.LogError` 并照**旧默认值**兜底
            //    （`type` → `Fullscreen`(0) / `placement` → `Popup`(15)），
            //    而 `placement` 兜错档 = 窗挂到**弹窗锚点**上（画面/命中全不对）⇒ 不许靠兜底。
            win.type = WindowType.Fullscreen;          // 原版 prefab 实证 `type = 0`
            win.placement = WindowsPlacement.World;    // 原版 prefab 实证 `windowsPlacement = 10`

            // ⚠️ `closeOnEsc` / `extraScaleSmallScreen` 的 **prefab 真值没读到**（D10 §六·2 只查到
            //    「覆写了整条 `ESCPressed` ⇒ 对行为无影响」）⇒ 保持基类默认值，**如实标注**：
            //    · `closeOnEsc`（基类默认 `true`）：本窗覆写了整条 `ESCPressed` ⇒ 这一格**不会被读**
            //      （值本身原版没读到 —— ⛔ 别按「原版大概是 1」写一个数字上去）。
            //    · `extraScaleSmallScreen`（基类默认 `1f`）：`1f` 的原版语义 = 「**不覆盖**」prefab 烤的
            //      `menuScale`（见那个字段的 doc）⇒ 我们这棵树没有烤值 ⇒ 等价于「不放大」。
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>🆕 **2026-10-13（`A1132` 第 3 步）**：**建 + 开** —— 「进编辑」那三处入口改的就是调它。
        ///
        /// <para>它之所以单独成一格（而⛔不在三个入口各抄一遍）＝ CLAUDE.md §三那条
        /// 「**两处写同一条规则 = 迟早不一致**」：`Create`（两个必填参数 · 锚点 · 宿主登记）
        /// 与 `OpenWindow`（栈/前任/默认选中）的顺序**只有一份**。</para>
        ///
        /// <para>🔴 **三个调用点各自的判据**（现读 · `文件:行号` 按写这一笔时）：
        /// `Shell/CollectionWindow.GoEdit` · 同文件 `GoNewDeck` · `Shell/LiveOpsEventWindow.CreateDeckInMode`
        /// —— 三处都是「`Application.isBatchMode` 那道闸之后、`LoadScene` 兜底之前」，
        /// **只在前者为假、且 `WindowsManager.Instance != null` 时**才走到这里。</para>
        ///
        /// <para>⚠️ **本方法不做兜底决定**：传 `null` 是调用方的 bug ⇒ **出声 + 返回 `null`**
        /// （红线「不许静默失败」；⛔ 不在这里偷偷 `EnsureHost()` —— 那会把「直起场景」这条路
        /// 也变成开窗，正是第 3 步要保住的兜底分支）。</para></summary>
        public static DeckEditingWindow OpenFrom(WindowsManager mgr)
        {
            if (mgr == null)
            {
                Debug.LogWarning("[DeckWin] `OpenFrom(null)` —— 没有壳就不该走开窗这条路"
                                 + "（调用点应当先判 `WindowsManager.Instance`，为 null 时走 `LoadScene(\"DeckEditor\")` 兜底）。"
                                 + "⚠️ 这一下**什么都没开**。");
                return null;
            }
            var win = Create(mgr);
            mgr.OpenWindow(win);
            return win;
        }

        /// <summary>铺内容。**原版这里那一整棵 prefab 树我们不重建** —— 把 `DeckRuntime` 挂在本窗窗根上，
        /// 由它按 `Root = transform` 把整套界面建出来（D10 §二）。
        /// 🔴 `DeckRuntime.Build` 开头就是「清空 `Root` 的所有子件再重建」⇒ **同窗再开会重建、不叠**
        /// （`GameWindow.TryOpen` 只在 `Closed ⇒ Open` 那一拍调 `Open()`，见它那边的注释）。</summary>
        public override void Open()
        {
            // 🔴 **2026-10-17（界面线 `T12`）：收起左竖导航** —— 原版 `DeckEditingWindow__TryOpen.c:139`
            //    = `NavigationPanelController.Toggle(0, 1, 0)`（收起、带动画）。放回在 `Close()`。
            //    ⚠️ 必须在 `Build` **之前**：那一跳会把整棵界面建出来，晚一步会看到左栏压在编辑器上闪一下。
            MainMenuRuntime.SetNavPanelVisible(false, true);
            if (_runtime == null) _runtime = gameObject.AddComponent<DeckRuntime>();
            // 🔴🔴 **2026-10-13（`A1132` 第 2 步）—— 本句【不能省】**（它和第 3 步必须同批交件）：
            //   第 3 步起本窗就是「进编辑」的落点 ⇒ 开出来那一刻，**本窗自己就是 `wm.TopWindow`**；
            //   而 `DeckRuntime.OtherWindowUp()` 判的是「`TopWindow` 是不是一扇**别的**窗」——
            //   不登记宿主 ⇒ 那道闸**恒真** ⇒ 本窗的点击 / 拖拽 / ESC **全部早退**（= 窗一开就死）。
            //   ⛔ 判据 **不是**「名字等于 `Deck Editing Menu`」：名字在 Unity 里可以重，
            //      这里要答的是「顶窗**是不是**我头上这一扇」⇒ 只有对象身份算数（见 `HostWindow` 的 doc）。
            //   ⚠️ 写在 `Build` **之前**：`Build` 之后本窗已经能收输入，那一小段窗口期里闸必须已经是对的。
            _runtime.HostWindow = this;
            // ⚠️ `Build` 的实参现读签名 = `Build(DeckLibrary lib)`（`Deck/DeckRuntime.cs`），
            //    与 `Editor/DeckScene.cs` 那条 Play 入口冒烟测试**同一种喂法**（传当前库）。
            _runtime.Build(DeckLibrary.Load());
        }

        /// <summary>ESC —— **原版 `DeckEditingWindow__ESCPressed.c` 全文只有一句
        /// `DeckEditingWindow__TrySaveDeck(this, 0);`**（逐句亲读，无 `base` 调用）。
        ///
        /// <para>⇒ 两条都要照做：① **覆写整条**（因此基类那两道门槛 —— `PointerLayer.InputEnabled` 与
        /// `closeOnEsc` —— 以及末尾那句 `Close()` 在本窗**都不跑**；那不是漏写，是原版的形状）；
        /// ② 落到**我们的保存口** `DeckRuntime.EscPressed()`（它自己的 doc 写着
        /// 「🆕 2026-10-11（A223）：ESC = 保存（原版 `DeckEditingWindow__ESCPressed`）」，
        /// 内部四级：输入框 → 导入弹窗 → 模态窗 → `SaveAndSay()` = 我们的 `TrySaveDeck`）。
        /// ⛔ **别绕开它直调 `SaveAndSay`**（那是 private，而且会跳过前三级的让位口径）。</para>
        ///
        /// <para>🔴 **返回值是我们加的**（原版 `void`；见基类 `ESCPressed` 的 doc：语义 = 「有没有真的
        /// **关掉**一扇窗」）—— 本窗按 ESC **只存不关** ⇒ **`false`**。
        /// ⛔ **别改成 `true`**：`Shell/PointerLayer.cs` 的 `KeyCancel` 是
        /// `bool closed = w.ESCPressed(); if (closed) Select(null);` ⇒ 返 `true` 会多跑一句
        /// `Select(null)`、把编辑窗里的选中态抹掉，而**原版没有那一步**（原版这一跳是 `void`）。</para></summary>
        public override bool ESCPressed()
        {
            if (_runtime == null)
            {
                // 出声（红线：不许静默失败）—— 走到这里说明「窗从没 `Open()` 过」或运行时挂了别的路，
                // 这时按 ESC 应当什么都不做，而不是假装存过了。
                Debug.LogWarning($"[DeckWin] `{name}` 的 `ESCPressed`：本窗还没有 `DeckRuntime`"
                                 + "（= 从没 `Open()` 过？）⇒ 这一下**什么都不做**（不许静默假装存过）");
                return false;
            }
            _runtime.EscPressed();
            return false;   // 只存不关 ⇒ 没有关掉任何一扇窗（判据见上面的 doc）
        }

        /// <summary>🔴 **2026-10-17（界面线 `T12`）：关窗时把左竖导航放回来。**
        /// 判据 = `DeckEditingWindow__Close.c:19-20`（**先** `NavigationPanelController.Toggle(true, true)`，
        /// **再** `GameWindow.Close()`）⇒ 顺序照抄：放回在 `base.Close()` 之前。
        /// ⚠️ 原版 `Toggle` 的「放」会先写 `hiddenPosition` 再 `SetActive(true)` 再补间回展示位 ——
        /// 那三条在 `MainMenuRuntime.SetNavPanelVisible` 里逐句照做了，⛔ 别在这儿重写一份。</summary>
        public override void Close()
        {
            MainMenuRuntime.SetNavPanelVisible(true, true);
            base.Close();
        }
    }
}
