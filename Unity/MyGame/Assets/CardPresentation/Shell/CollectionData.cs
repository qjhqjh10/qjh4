// CollectionData.cs — 收藏线（`Collection Menu Variant`）的**本地数据源**
//
// ============================ 原版是什么（A4 实证） ============================
// 原版这几页的数据**全是本地单例**、**没有 `handler.`**（这一点和锻造厂/商店不同）：
//   · Deck 页 = 卡组列表（我们已有 `DeckLibrary` / `DeckStore` ✓ 直接搬）
//   · Cards 页 = `CollectionManager.AllCardCollection`（我们 = `CardDatabase.Load()` ✓）
//   · CardBacks 页 = `CollectionManager.Instance.FullCosmeticCollection.OfType<CosmeticItemCardback>()`
//     （图在 `bundle_cosmeticscardbacksimages_assets_all/Texture2D/` **233 张 —— 还没导进工程**）
//   · Alternate Art 页 = `AllCardCollection` 按 `alternateArtStyles[currentStyleIndex]` 过滤（**待查**）
//
// ⚠️ **`PlayerDeck` 没有 id 字段**（`RuleEngine/Core/DeckRules.cs` 的 `PlayerDeck`：只有 Name/WarlordId/DefensiveId/CardIds）
//    ⇒ 本轮拿 **Name** 当稳定标识（重命名会让选中态丢，**如实记**；将来加 id 要动存档格式）。
using System.Collections.Generic;
using UnityEngine;          // 🆕 2026-10-13（A503）：四个口落盘失败时要**出声**（`Debug.LogWarning`）
using RuleEngine;

namespace CardPresentation
{
    /// <summary>🆕 **2026-10-18（A855 · 用户拍板「按来路回」）**：`DeckEditor`（卡组编辑那个**独立场景**）
    /// 是**从哪条路**进的 —— 决定「编辑完离场」时回程往哪里开。
    /// ⚠️ 全仓**仅有**两处 `LoadScene("DeckEditor")`，就在这两个入口里各写一次
    /// （见 `CollectionData.SetReturnIntent` / `CollectionData.PendingReturn`）。</summary>
    public enum DeckExitSource
    {
        /// <summary>没有意图。🔴 **「有没有回程意图」的唯一判据是它** —— ⛔ 别拿「页签是不是 `None`」当判据。</summary>
        None = 0,
        /// <summary>收藏窗的卡组页（`Shell/CollectionWindow.GoEdit`）
        /// ⇒ 回程 = 回主菜单 + 重开**收藏窗的卡组页**（`CollectionDecks`）。</summary>
        Collection = 1,
        /// <summary>遭遇 / 排位事件窗的 `Create deck`（`Shell/LiveOpsEventWindow.CreateDeckInMode`）
        /// ⇒ 回程**本该**回那扇事件窗，但**本地没有事件数据、本次复刻不了**（理由逐条 → `MainMenuRuntime.Build`
        /// 里那一段注释）⇒ 今天**只把来源记下来、不开窗**。这笔账**还开着**：将来事件数据能复刻时，
        /// 只需在 `MainMenuRuntime.Build` 里补「按来源开窗」那一跳。</summary>
        LiveOpsEvent = 2,
    }

    /// <summary>收藏线要用的那几样数据（纯静态、可 Reset，自检之间互不影响）。</summary>
    public static class CollectionData
    {
        /// <summary>一格卡组要显示的东西。</summary>
        public struct DeckInfo
        {
            public string Name;        // 卡组名（**本轮兼作 id** —— `PlayerDeck` 没有 id 字段）
            public string WarlordId;
            public string Faction;     // 从督军卡推（`CardDef.Faction`），查不到就是空串
            public int Count;          // 普通卡张数（不含督军/防御卡）
            /// <summary>这副卡组选的卡背（空 = 没选过，用该阵营默认）。
            /// 取值一律走 `CardArt.DeckCardback(CardbackId, Faction)` —— 判据只那一处。</summary>
            public string CardbackId;
            /// <summary>本副卡组的模式（`0` 经典 / `13` 遭遇）。
            /// 🆕 2026-09-26：原版玩家自己的卡组**是有 `gameMode` 的**（`CardDeck.gameMode` @0x70），
            /// 之前这里没带出来是因为我们还没有那个字段（判据 → `资料/加时与冲突模式_原版规格.md` §2.7）。</summary>
            public int GameMode;
        }

        static DeckLibrary _lib;
        static DeckEditorState _lookup;      // 只借它做 id → `CardDef`（`Find`）

        static DeckLibrary Lib { get { if (_lib == null) _lib = DeckLibrary.Load(); return _lib; } }
        static DeckEditorState Lookup
        {
            get { if (_lookup == null) _lookup = new DeckEditorState(CardDatabase.Load()); return _lookup; }
        }

        /// <summary>🆕 **2026-10-18（A855）作废进程级那份卡组库缓存**（下次读 <c>Lib</c> 会**重读存档**）。
        ///
        /// <para>🔴 **为什么必须有这个口**：本类的 `Lib` 是 **`static DeckLibrary _lib`** —— 一份**进程级缓存**；
        /// 而**卡组编辑窗写盘走的是另一个实例**（`RuleEngine/Data/DeckLibrary.cs:117-119` 的
        /// `DeckLibrary.Load()` **每次 `new`**；编辑器那条路 → `Deck/DeckRuntime.cs:726`）。
        /// 那份缓存此前**没有任何生产侧作废口**（`_lib = null` 只在 <see cref="ResetForTest"/> 里，
        /// 而它只被 `Editor/*` 调）⇒ **玩家在编辑窗存完、回壳，收藏页画的还是编辑前那一份**：
        /// 列表 = `Shell/CollectionWindow.cs:3032` 的 `MenuDraw.DeckCell(…, CollectionData.DeckAt(i), …)`
        /// → <see cref="Raw"/> → `Lib.Decks[i]`。</para>
        ///
        /// <para>**原版对位**（这条链**一处场景切换都没有**）：开编辑 = 在收藏窗之上**开一扇窗**
        /// （`DeckEditingWindow__UploadDeck` 那一族全族 grep `LoadScene` = 0 命中）、离场 = **关窗**；
        /// 而**保存 = 把那份 `CardDeck` 就地覆写回收藏页正在显示的那一个对象** ——
        /// `d:/2/tools/decomp_full/DeckEditingWindow__UploadDeck.c:63`
        /// `CardDeck__CopyDeck(库那一份, 编辑中那一份, 0)`，而 `CardDeck__CopyDeck.c:25-36` 是**就地覆写**
        /// （清空 dest `+0x58` 那个 `List` 再 `AddRange`）⇒ **原版收藏页根本不需要刷新**。
        /// 我们这份缓存是**另一份拷贝**，所以要在**消费端**补上这一跳
        /// （⚠️ **不是**把编辑器改成持有本类的实例 —— 那与 A397/A363 定过的口径相反，
        /// 见 `Deck/DeckRuntime.cs:5941-5952` 那段「维持现状、不加守卫」的裁定）。</para>
        ///
        /// <para>🔴 **调用时机 = 「刚离开卡组编辑」那一跳**，唯一调用点 = `Shell/MainMenuRuntime.cs` 的 `Build`
        /// 里消费 `TakePendingReturn` 那里（**两个来源都调**：`Collection` 与 `LiveOpsEvent` 都能改卡组、都写盘）。
        /// ⛔ **别在 `Build` 里无条件调** —— 正常进主菜单也会白读一次盘。
        /// ⛔ **也别在编辑窗那条路上调**（`DeckRuntime.CommitDeck` / `BackToMenu`）—— 见下一段。</para>
        ///
        /// <para>⚠️ **为什么不放在「保存」那一刻**（那样时序上更像原版的 `UploadDeck`）：`CommitDeck` 之后
        /// 编辑窗还在用自己那份 `Library`，**本类此时并没有人在读**；把作废点挪进 `Deck` 层等于让
        /// 「卡组编辑」去改「收藏线」的内部状态（跨层），而回程那一跳**本来就**要把玩家送回收藏页
        /// ⇒ 作废与开窗挨着写，一处看得全。⚠️ 如实记：**这是机制上的偏离**（原版是就地覆写、无缓存一说），
        /// 可观测结果对齐（回收藏页看到的就是刚存的那一版）。</para></summary>
        public static void InvalidateLibrary() { _lib = null; }

        public static int DeckCount() { return Lib.Decks.Count; }
        public static int CurrentIndex() { return Lib.CurrentIndex; }
        public static PlayerDeck Raw(int i)
        {
            return (i >= 0 && i < Lib.Decks.Count) ? Lib.Decks[i] : null;
        }

        public static DeckInfo DeckAt(int i)
        {
            var info = new DeckInfo();
            var d = Raw(i);
            if (d == null) return info;
            info.Name = d.Name ?? "";
            info.WarlordId = d.WarlordId;
            info.Count = d.CardIds != null ? d.CardIds.Count : 0;
            info.CardbackId = d.CardbackId;
            info.GameMode = d.GameMode;
            var hero = string.IsNullOrEmpty(d.WarlordId) ? null : Lookup.Find(d.WarlordId);
            info.Faction = hero != null ? hero.Faction : "";
            return info;
        }

        /// <summary>上一次「选中卡组」**失败的人话**（成功时是空串）。**两种原因分开**：
        /// ① **没选中**（下标越界 ⇒ 当前选中那一套一动没动）；② **选中了，但没落盘**
        /// （内存里的 `_current` 已经是这一套、盘上还是上一套）。
        ///
        /// <para>🔴 **2026-10-13（A600）**：原来 `Select` 是 `void` + 裸 `Lib.Save();` —— **返回值与 `LastError` 都不读**
        /// ⇒ 写盘失败时玩家以为「选中了」，而 `BattleDriver.PickSavedDeck` 是**从磁盘重读**的
        /// （那条因果见下面 `Select` 里那段注释）= **战斗会拿磁盘上那套旧的**，全程一声不响。
        /// **与 <see cref="LastDeleteError"/> 逐条同形**（A503 定的那一套）：调用点直接打它，
        /// ⛔ 别在那边自己再判一遍（两处写同一条规则 = 迟早不一致）。</para>
        /// <para>⚠️ 与 `LastDeleteError` 那处的**一处差别（如实记）**：`DeckLibrary.Select` 越界时**早退**
        /// 且**不清 `LastError`** ⇒ 这里**不能**只看 `LastError` 分两种；判据取的是**「调完
        /// `Lib.CurrentIndex` 是不是你要的那一个」**（同 A503「看 `Decks.Count` 变没变」那条思路：
        /// 观测状态，不把 `DeckLibrary.Select` 的越界判断抄第二份）。</para></summary>
        public static string LastSelectError { get; private set; } = "";

        /// <summary>选中第 <paramref name="i"/> 套卡组。返回**选成了没有**
        /// （= 当前选中项已经是这一套 **且** 落盘成功）；失败时**是哪一种**看 <see cref="LastSelectError"/>。
        /// ⚠️ 返回 `bool` 是**加出口、不是改契约**：原来 `void`，调用点全把它当语句用 ⇒
        /// 一处不改也编得过（⛔ 别为了它去动签名形状、也别加 `out`）。</summary>
        public static bool Select(int i)
        {
            LastSelectError = "";
            Lib.Select(i);                     // ⛔ 越界时它**早退**（既不改 `_current`、也不清 `LastError`）
            // 🔴 判据 = **观测状态**（「当前选中的是不是你要的那一套」）—— ⛔ 不在这里把
            //    `DeckLibrary.Select` 的越界判断抄第二份（同 A503「看 `Decks.Count` 变没变」那条思路）。
            if (Lib.CurrentIndex != i)
            {
                LastSelectError = "**没选中** —— 下标 " + i + " 越界（库里现在 " + Lib.Decks.Count + " 套）";
                Debug.LogWarning("[CollectionData] 选中卡组失败：" + LastSelectError);
                return false;
            }
            // 🔴 **2026-09-26 补上落盘**：`BattleDriver.PickSavedDeck` 走的是
            //   `DeckLibrary.Load()`（**从磁盘重读一份**），所以「在窗里选了第几套」这件事
            //   **必须落盘才过得去** —— 原来只改内存里的 `_current`，切场景后那一下选择**静默失效**
            //   （战斗会拿磁盘上那套「上次存的」）。`Select` 的几个调用点（收藏窗 / `DeckInfoPopup` /
            //   练习窗 / 模式窗）全都在「玩家刚选定」这一刻，落盘的时机正好。
            // 🔴 **2026-10-13（A600）**：上面那段因果原来**没有对应实现** —— 那次 `Lib.Save()` 的返回值
            //   与 `LastError` 都不读 ⇒ 写盘失败时那段「静默失效」**照样会发生**。现在这条出口管它。
            if (!Lib.Save())                   // `Save()` 每次重写 `LastError`（成功写 null）⇒ 这里读它才是新鲜的
            {
                LastSelectError = "选中了，但**没写进存档**：" + SaveFailReason()
                                + "（内存里已经是这一套、盘上还是上一套 ⇒ 战斗会拿磁盘上那套旧的）";
                Debug.LogWarning("[CollectionData] 选中卡组失败：" + LastSelectError);
                return false;
            }
            return true;
        }

        /// <summary>按**卡组名**反查下标（查不到给 −1）。
        /// ⚠️ 又是「名字当 id」那条老账（`PlayerDeck` 没有 id 字段，见文件头）——
        /// 同名卡组**取第一个**；重命名会让外面拿着的名字失效，**如实记**。
        /// 用途：`Deck Selection Popup` 的回调只给一份 `DeckInfo`，要拿回下标。</summary>
        public static int IndexOf(string deckName)
        {
            if (string.IsNullOrEmpty(deckName)) return -1;
            for (int i = 0; i < Lib.Decks.Count; i++)
                if (Lib.Decks[i].Name == deckName) return i;
            return -1;
        }

        /// <summary>卡 id → 卡定义（`Deck info Popup` 要把卡组里的 id 列表画出来）。查不到给 null。</summary>
        public static CardDef Card(string id) { return string.IsNullOrEmpty(id) ? null : Lookup.Find(id); }
        /// <summary>督军卡（没有 / 查不到给 null）。</summary>
        public static CardDef Warlord(int i)
        {
            var d = Raw(i);
            return d == null ? null : Card(d.WarlordId);
        }
        /// <summary>上一次「删卡组」**失败的人话**（成功时是空串）。**两种原因分开**：
        /// ① **没删**（下标越界 ⇒ 一套都没少）；② **删了，但没落盘**（内存里少了、盘上还在 ⇒ 重启它又回来）。
        ///
        /// <para>🔴 **2026-10-13（A503）**：原来这两件事在调用点被**混成同一句「删不了」**
        /// （`Shell/DeckInfoPopup.cs` 删失败那一支），而那句还把原因归给了「只剩一套时不许删」—— 那条规矩**不在本层**。
        /// **判据 = 调用前后 `Decks.Count` 变没变** —— ⛔ **别只看 `LastError`**：`DeckLibrary.Delete` 越界那条**早退**，
        /// 而且它**不清 `LastError`**（上一次的旧值会留着 ⇒ 只看它会把「越界」误判成「没落盘」）。
        /// 用途 = 调用点的失败文案直接打它（⛔ 别在那边自己再判一遍：两处写同一条规则 = 迟早不一致）。</para></summary>
        public static string LastDeleteError { get; private set; } = "";

        /// <summary>删一套卡组。返回**删成了没有**（= 内存里删掉 **且** 落盘成功）；失败时**是哪一种**看 <see cref="LastDeleteError"/>。
        ///
        /// <para>🔴 **2026-10-13（A503）就地订正（铁律 5）**：本行原来写「**只剩一套时不许删**（`DeckLibrary.Delete` 的规矩）」——
        /// **`DeckLibrary.Delete` 没有这条规矩**：它删到 0 套也照删（`_current = -1`，见 `RuleEngine/Data/DeckLibrary.cs` 的 `Delete`）。
        /// 拦这件事的是 **UI 侧**：`Shell/DeckInfoPopup.DeleteInteractable`（照原版 `DeckInfoControls__Initialize`
        /// 的 `1 &lt; 卡组数`）⇒ 这里**改注释、不补实现**（本层再拦一次 = 与 UI 侧两处写同一条规则）。</para>
        ///
        /// <para>⛔ 原来那行末尾还有一次 `Lib.Save()`：`DeckLibrary.Delete` 内部已经是 `return Save();`（A398）
        /// ⇒ 那是**多余的第二趟落盘**，顺带把它自己的返回值也一起吞了（写盘失败照样报「删成功」）。</para></summary>
        public static bool DeleteDeck(int i)
        {
            LastDeleteError = "";
            int before = Lib.Decks.Count;
            bool ok = Lib.Delete(i);          // ⛔ 别在后面再加一次 `Lib.Save()`（A398 起它自己 `return Save();`）
            if (ok) return true;
            LastDeleteError = Lib.Decks.Count < before
                ? "删了，但**没写进存档**：" + SaveFailReason() + "（内存里已经少了这一套、盘上还在 ⇒ 重启它又回来）"
                : "**没删** —— 下标 " + i + " 越界（库里现在 " + before + " 套）";
            Debug.LogWarning("[CollectionData] 删卡组失败：" + LastDeleteError);
            return false;
        }

        /// <summary>上一次「复制卡组」**失败的人话**（成功时是空串）。**两种原因分开**：
        /// ① **没复制**（下标越界 ⇒ 库里一套没多）；② **复制出来了，但没落盘**
        /// （内存里已经多了一套、盘上还在 ⇒ 重启它就没了）。
        ///
        /// <para>🔴 **2026-10-13（A611）**：这条出口原来**不存在** —— `DuplicateDeck` 只有「新卡组名 / 空串」
        /// 这一个返回值，而空串有**两种**成因 ⇒ 调用点（`Shell/DeckInfoPopup.cs` 的 `OnOption`）只能拿
        /// **「调用前后 `DeckCount()` 变没变」**这个**间接判据**去猜是哪一种 —— 那等于把同一条规则**写了第二份**
        /// （CLAUDE.md §三）。**与 <see cref="LastDeleteError"/> 逐条同形**（A503 定的那一套）：
        /// 调用点直接打它的**原话**，⛔ 别在那边自己再判一遍。</para>
        /// <para>⚠️ 与 `LastDeleteError` 那处的**一处差别（如实记）**：`DeckLibrary.Duplicate` 越界时**早退**、
        /// 而且**不清 `LastError`**（与 `Delete` 同形）⇒ 判「越界」**必须先看返回值是不是 `null`**
        /// （那一刻 `Lib.LastError` 里留着的是**上一次**的旧值）；走到 `LastError` 那一支时 `SaveOrWarn`
        /// 刚跑过、它每次都重写 `LastError` ⇒ 那时读到的才是新鲜的。</para></summary>
        public static string LastDuplicateError { get; private set; } = "";

        /// <summary>复制一套卡组，返回新卡组名。**失败给空串**（两种：① 下标越界；② **写不进存档**，
        /// 见 <see cref="SaveFailReason"/>）；失败时**是哪一种**看 <see cref="LastDuplicateError"/>。
        /// 🔴 **2026-10-13（A503）**：原来这里**既不读返回值、也不读 `LastError`**，后面还多调一次 `Lib.Save()`
        /// （`DeckLibrary.Duplicate` 内部早已是 `SaveOrWarn`，A398）⇒ 写盘失败时玩家看到的是「复制成功」，盘上却没变。
        /// 🔴 **2026-10-13（A611）**：补一条与 `LastDeleteError` **同形**的出口 <see cref="LastDuplicateError"/>
        /// —— 调用点不必再拿「`DeckCount()` 变没变」猜是哪一支。</summary>
        public static string DuplicateDeck(int i)
        {
            LastDuplicateError = "";
            int before = Lib.Decks.Count;      // 观测状态（同 A503 的 `DeleteDeck`）：越界那一支库不会变大
            var d = Lib.Duplicate(i);          // ⛔ 别在后面再加一次 `Lib.Save()`（A398 起它自己 `SaveOrWarn`）
            if (d == null)                     // 下标越界：**没复制**（⚠️ 必须先判它 —— 那一刻 `Lib.LastError` 是旧值）
            {
                LastDuplicateError = "**没复制** —— 下标 " + i + " 越界（库里现在 " + before + " 套）";
                Debug.LogWarning("[CollectionData] 复制卡组失败：" + LastDuplicateError);
                return "";
            }
            if (Lib.LastError != null)         // 落盘失败：**内存里已经复制好了**，但没写进存档
            {
                LastDuplicateError = "复制出来了，但**没写进存档**：" + SaveFailReason()
                                   + "（内存里已经多了一套、盘上还在 ⇒ 重启它就没了）";
                Debug.LogWarning("[CollectionData] 复制卡组失败：" + LastDuplicateError);
                return "";
            }
            return d.Name;
        }

        /// <summary>导入一条卡组串（原版 `MenuDeck/Share/*` 那套）。成功返回新卡组名；失败返回空串并给**人话**原因
        /// （三种：空串 / 不是合法卡组串 / **写不进存档** —— 第三种是 🆕 2026-10-13（A503）补的）。
        /// 🔴 **判据与错误文案与卡组编辑那边逐字一致**（`DeckRuntime.TryImport` 的那两句一字不动）——
        /// 两处各写一套迟早不一致（CLAUDE.md §三）。</summary>
        public static string ImportDeck(string s, out string why)
        {
            why = "";
            // 🔴 **2026-10-18（波 1b）**：三句全部走词条 —— 键名由调度台在波 0b 一次定死
            //   （`MenuDeck/Error/Import{Empty,BadString,NotPersisted}`，表里有），
            //   **与 P1 的 `Deck/DeckRuntime.cs:3950/3951/3982` 共用同一批键**（施工单 §③ 要求逐字一致）。
            //   ⚠️ 前两句 ZH 与改前写死串**逐字相同** ⇒ 中文档零变化。
            if (string.IsNullOrWhiteSpace(s)) { why = Loc.T("MenuDeck/Error/ImportEmpty"); return ""; }
            var deck = DeckLibrary.ImportString(s, Card);
            if (deck == null) { why = Loc.T("MenuDeck/Error/ImportBadString"); return ""; }
            Lib.Add(deck);                     // ⛔ 别在后面再加一次 `Lib.Save()`（A398 起它自己 `SaveOrWarn`）
            if (Lib.LastError != null)
            {
                // 🔴 卡组串**读出来了**，但没落盘 —— 原来这里照样回名字 ⇒ 玩家看到「导入成功」而盘上没变（下次开游戏就没了）。
                // 🔴 **第三句有意对齐到 `DeckRuntime` 那一版**（波 0b 判据 = 取 `Deck/DeckRuntime` 的措辞，
                //   见那三条键）：加了「导入失败：」前缀、分隔符 `：`→`——`、尾加「（重启就没了）」
                //   ⇒ 中文档下这句**比改前长**（这是裁定的目的：两处**逐字一致**，不是保持原样）。
                // ⚠️ 用 `Replace` 不用 `string.Format`（文案里有 `**`；先例 `Battle/HUD/CreatedBy`）。
                why = Loc.T("MenuDeck/Error/ImportNotPersisted").Replace("{0}", SaveFailReason());
                // ⚠️ 这句是开发者日志（②，不上屏）：原来它自己在尾巴上拼「（重启就没了）」，
                //    现在 `why` 里已经有了 ⇒ **去掉重复的尾巴**，免得一条日志念两遍。
                Debug.LogWarning("[CollectionData] 导入卡组「" + deck.Name + "」：" + why);
                return "";
            }
            return deck.Name;
        }

        /// <summary>新建一套卡组（原版走 `Deck Editing Menu` 的「Create」，本轮只建卡组、不进编辑）。
        /// <paramref name="gameMode"/> = 本副卡组的模式（`0` 经典 / `13` 遭遇）——
        /// 照原版 `SelectDecksTab.CreateDeck`：**建组那一刻把当前模式打进卡组**，之后没有改的路径
        /// （判据 → `资料/加时与冲突模式_原版规格.md` §2.7）。
        ///
        /// <para>🆕 **2026-10-13（A503）**：返回新卡组名；**写不进存档时返回空串** —— 内存里那套还在，但它迟早会丢，
        /// 说「没成」比说「成功」诚实。调用方拿到空串时**别**再拿它往下走（尤其别拿去 `IndexOf` 再进编辑）；
        /// 原来这里连 `LastError` 都不读，写盘失败时玩家看到的是「建好了」。</para></summary>
        public static string CreateDeck(int gameMode = 0)
        {
            // 默认卡组名走词条（键 **`MenuDeck/NewDeckName`** —— ⛔ **不是** `MenuDeck/HUD/NewDeckName`，
            // 调度台 2026-10-18 已统一成前者，见 `Core/Loc.cs` 的 ⑪「默认 / 演示卡组名」那一节；
            // 中文列「新卡组」与改前的写死串**逐字相同** ⇒ 中文档零变化）。
            // ⚠️ **本期行号订正（铁律 5）**：上面原来写的是 `Core/Loc.cs:1050-1065` / `:1057-1062` ——
            //    那两处现读落在 **③「通用弹窗的三颗钮」**（`MainMenu/General/OK`）那一段，**与默认卡组名无关**
            //    （写这条注释之后 `Loc.cs` 又插了几节 ⇒ 行号漂了，而指针指的是别的小节）⇒
            //    **一律按小节名认**（⑪ 那一节），⛔ 别再按行号找。
            // ⚠️ **如实记**：这个名字**会写进存档** ⇒ 英文档下新建的卡组字面就叫 `New deck`（数据被翻了）。
            //    施工单 §⑦ P1 那一行把同一件事标成「需裁决」，但调度台在 `Loc.cs` ⑪ 那一节已裁决「三处一律用本键」。
            //
            // 🔴 **2026-10-18（`A1037`）裁定：保留现状，⛔ 别再翻案。** 口径 —— **它【不是】「显示时才翻」**，
            //    而是「**创建那一刻按当前语档生成一个名字、当场物化进存档**」：
            //      · 英文档下新建 ⇒ 盘上就是 `New deck`；中文档下新建 ⇒ 盘上就是 `新卡组`（**预期行为、不是缺陷**）；
            //      · **已有卡组不会因为玩家改语言而改名**（那串字已经是**玩家数据**，不是文案了）——
            //        这正是它与「显示时翻」的分水岭：后者会在切语言时把玩家的卡组名当场改掉，那才是真缺陷。
            //    📌 **同一口径还管着** `Shell/ProfileData.cs` 的 `DefaultPlayerName`（`A1047`，同类裁定「翻」）——
            //       两者是**同一类问题**（铁律 6：同一条规则只留一份），别只改一处。
            //    ⚠️ 反面才是错的：**玩家自己命名的卡组名【绝不进 `Loc` 表】**（`Loc.cs` ⑪ 那一节自己的原话）。
            //
            // 🔴 **`MenuDeck/NewDeckName` 不是原版键 —— 三路都搜过（2026-10-18 现核）**：
            //    ① `d:/2/新解包资源/assets_full/` 的 `mTerm`：`MenuDeck*` 共 **42** 条，**无 `NewDeckName`**
            //       （同族近邻 = `MenuDeck/HUD/New` · `MenuDeck/HUD/EditDeckName` · `MenuDeck/MenuButtons/CreateDeck`）；
            //    ② prefab 组件字段的 `text`：全 `assets_full` 搜 `New deck` = **0 个文件**、`My deck` = **0 个文件**
            //       （正对照：已知原版串 `Auto zoom` = **14** 个文件 · `bundle_menus_assets_all/` 里带 `"m_text"` 的文件 = **4894**
            //        ⇒ **扫描本身有效**，这 0 不是「没搜到」）；
            //    ③ `d:/2/tools/il2cpp_out/stringliteral.json`：`NewDeckName` = **0 命中**
            //       （该文件里 `MenuDeck*` 只有 **33** 条，`DeckName` 一族只有一条前缀串 `MenuDeck/DeckName/`）。
            //    ⇒ 键名与两列**均自拟**（与 `Core/Loc.cs` ⑪ 那一节的记录一致）。
            string name = Lib.UniqueName(Loc.T("MenuDeck/NewDeckName"));
            var d = Lib.Create(name, gameMode);   // ⛔ 别在后面再加一次 `Lib.Save()`（A398 起它自己 `SaveOrWarn`）
            string got = d != null ? d.Name : name;
            if (Lib.LastError != null)
            {
                Debug.LogWarning("[CollectionData] 新建卡组「" + got + "」：内存里**已经有了**，但**没写进存档**："
                                 + SaveFailReason() + "（重启就没了）");
                return "";
            }
            return got;
        }

        /// <summary>落盘失败的原因（人话）—— **上面四个口共用这一句**（两处写同一条规则 = 迟早不一致，CLAUDE.md §三）。
        /// 🔴 **2026-10-18（`G9`）改成与 `DeckRuntime.SaveFailReason()` 【逐字同一形状】**：
        ///   · **人话**那一半走词条 —— `Loc.T(DeckRuntime.TermSaveFailed)`（键 `MenuDeck/Error/SaveFailed`，
        ///     在 `Core/Loc.cs` 的表里；中文列就是原来写死的那句「写不进存档文件」⇒ **中文档零变化**）；
        ///   · `Lib.LastError` 只用**兜底诊断**缀在括号里（⛔ 不再是显示主路 —— `G8` 起它装的是诊断串）。
        /// ⚠️ **改之前**这里是写死的中文「写不进存档文件」⇒ 与 `DeckRuntime` 那一半在**英文档**下分叉
        ///   （`DeckRuntime` 那一半 `G8` 已改走词条，本半没跟）。现在两半**同一形状**。
        /// 🔴 键名**只从 `DeckRuntime.TermSaveFailed` 取一处**，⛔ 别在这儿再抄一遍字面量。</summary>
        static string SaveFailReason()
        {
            string why = Loc.T(DeckRuntime.TermSaveFailed);
            string diag = Lib.LastError;
            return string.IsNullOrEmpty(diag) ? why : why + "（" + diag + "）";
        }

        /// <summary>自检用：把缓存丢掉，下次重新从存档读。</summary>
        /// <summary>「从收藏进编辑」的**交接**：收藏窗点「编辑」时把这一套的下标写在这里，
        /// `DeckRuntime.Build` 开局读它、读完清成 −1。
        /// ⚠️ **静态字段跨场景存活**（`DeckEditor` 是**另一个场景**）—— 这是最小代价的做法
        /// （原版在同一扇窗里换页、不需要交接；我们的编辑器是独立场景，见正本 §七 的那处偏离）。
        /// ⚠️ 批处理下收藏窗**只交接、不切场景** ⇒ 自检验的就是这个下标。</summary>
        public static int PendingEditDeck = -1;

        /// <summary>🆕 **2026-10-18（A855）「编辑完卡组离场」的回程意图** —— 与上面 `PendingEditDeck` **对称**：
        /// 去程写「进哪一套」· 回程写「**从哪条路来**」。同样**静态字段跨场景存活**（`MainMenu` 也是另一个场景）、
        /// 同样**批处理下只交接、不切场景** ⇒ 自检验的就是这个意图。
        ///
        /// <para>🔴 **用户 2026-10-18 拍板：按来路回**（不是「一律回收藏窗」）⇒ 所以记的是**来源**，
        /// 不是一个「回收藏窗」的布尔。写作方 = **入口**（见 <see cref="DeckExitSource"/> 那两条路）；
        /// 读方 = `MainMenuRuntime.Build`（读走即清，再照来源开窗落页）。</para>
        ///
        /// <para>⚠️ **`Take*` 是本仓既有的交接口径**（**读走即清**，先例：`PrebuiltDecks.TakePendingBattleDeck` ·
        /// `BattleDriver.TakePendingPlayMode` · `NetBattle.Take`）—— ⛔ 别另设一套 `Reset*` 入口。</para></summary>
        public struct ReturnIntent
        {
            /// <summary>来路。**「有没有意图」的唯一判据**（`None` ⇒ 没有）。</summary>
            public DeckExitSource Source;
            /// <summary>来路那条路指定的**落点页**（收藏那条路 = `CollectionDecks`；事件窗那条路今天不用）。</summary>
            public WindowTabType Tab;
        }

        /// <summary>待消费的回程意图。`default` ⇒ `Source = None`（= 没有意图）。</summary>
        public static ReturnIntent PendingReturn;

        /// <summary>写下回程意图。**入口调**（「来源由入口决定、不由离场方式决定」—— 见 `DeckRuntime.BackToMenu`
        /// 那段注释：离场那三处调用点都只是「关闭这条路上的编辑器」）。</summary>
        public static void SetReturnIntent(DeckExitSource src, WindowTabType tab)
        {
            PendingReturn = new ReturnIntent { Source = src, Tab = tab };
        }

        /// <summary>取回程意图并**清掉**（读一次就没了 —— 同上面那三个 `Take*` 先例的口径；
        /// 不清的话下次进主菜单还会再落一次）。</summary>
        public static ReturnIntent TakePendingReturn()
        {
            var r = PendingReturn;
            PendingReturn = default(ReturnIntent);
            return r;
        }

        public static void ResetForTest()
        {
            _lib = null; _lookup = null;
            PendingEditDeck = -1;
            // 🆕 2026-10-18（A855）：回程意图与 `PendingEditDeck` **同进同出** —— 自检之间互不影响
            PendingReturn = default(ReturnIntent);
            // 🆕 2026-10-18 之后 · 第五会话（`A1068`，W2 顺手查出）：**三个 sticky 错误串也必须清**。
            //   它们是「上一次失败的文案」：各自的操作开头会清（`Select`/`Delete`/`Duplicate` 那句 `= ""`）、
            //   失败时赋新值 —— 但**跨夹具**时，前一个夹具失败留下的串会漏进后一个夹具：
            //     · `Editor/CollectionScene.cs:6780` / `:6891` 断「成功 ⇒ `Length == 0`」⇒ **假红**
            //     · `:6965` 那类断「有话说」⇒ **假绿**
            //   ⇒ 一处清三格，自检之间才**真隔离**。（`A1068`）
            LastSelectError = ""; LastDeleteError = ""; LastDuplicateError = "";
        }
    }
}
