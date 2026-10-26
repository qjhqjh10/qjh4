// DeckEditorState.cs — 卡组编辑界面的**状态核心**（不碰绘制，能单测）
//
// 为什么把状态和绘制分开：绘制要靠 ImageQuad/Label 摆一堆世界空间 quad，在批处理里
// 只能靠截图验收；而「筛选对不对、加牌删牌守不守规则、卡组合不合法」这些是**纯逻辑**，
// 可以断言。分开之后自检能覆盖绝大部分行为，截图只用来验收观感。
//
// 界面结构照原版的 prefab 节点树（见 `资料/卡组编辑界面_查证_0920.md` —— 🔴 **2026-10-10 改指（A262）**：原写 `资料/卡组编辑_原版数值与实现方案.md`，那份已被 0920 那份取代、且已加更正横幅）：
//   Deck Editing Menu > Card Display > Scroll View（卡列表）
//   Deck Editing Menu > Card Filters > Name/Army/Rarity/Cost/Type（筛选）
//   Deck Editing Menu > Sidebar > Deck Details > Deck List drawer（卡组内容）
//
// ✅ 原版筛选项里的 `Owned Toggle` / `Upgradable Toggle`：**2026-09-28 已建成真开关**（见 `DeckFilter.Owned/Upgradable`）——
//    出厂态是「Owned **开** / Upgradable **关**」（三路实读，见 `DeckFilter.None` 的注释）。
using System;
using System.Collections.Generic;
using System.Text;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡列表的筛选条件。空/负值表示「不限」。</summary>
    [Serializable]
    public struct DeckFilter
    {
        public string Name;      // 卡名子串，大小写不敏感
        public string Faction;   // "" = 不限
        public string Rarity;    // "" = 不限
        public string Type;      // "" = 不限；unit / tactic / hero / defence
        public int Cost;         // -1 = 不限
        /// <summary>🆕 **2026-09-28**（原版 `Owned Toggle`，`CardNameFilter` 同排那个 `EverguildToggle`）：
        /// 只看**已拥有**的卡。**原版出厂就是【开】**（prefab `m_IsOn=1` + `showOnlyOwnedCards` 初值 true
        /// + `TryOpen` 同步 —— 三路实读，见 `DeckFilter.None`）。
        /// ⚠️ 我们单机**全解锁** ⇒ 打开它**不改变结果**（不筛选也在池里）—— 如实标注，不是静默失效。</summary>
        public bool Owned;
        /// <summary>🆕 **2026-09-28**（原版 `Upgradable Toggle`）：只看**可升级**的卡。
        /// ⚠️ 我们**没有升级系统** ⇒ 打开它**必然筛成空**（`项目任务.md` §〇 已拍板「恒空是预期的」）。</summary>
        public bool Upgradable;
        /// <summary>费用的**上界**（含）。`&lt;= 0` ⇒ 只看 `Cost` 这一个值（旧行为）。
        /// 🔴 为什么要它：原版**收藏窗的 Cost 筛选是 8 个区间档**（`1-` / `2`…`7` / `8+`，
        ///    `CardCostFilter.options` 的 `alternativeText` 实读），不是「每个费用一格」——
        ///    只有 `Cost` 一个数表达不了 `8+`。卡组编辑那边只填 `Cost`（⇒ 退化成精确匹配
        ///    —— ⚠️ **2026-10-14（账 `N10`）就地订正**：原写「行为不变」，夹取补上之后**变了** ——
        ///    今天匹配的是**夹过 `[1,8]` 的费用**（见 <see cref="ClampPoolCost"/>），
        ///    所以 `Cost = 1` 会把**费用 ≤ 1（含 0）**的卡都列出来，`Cost = 8` 会把 **≥ 8** 的都列出来）。
        ///    出处：`资料/普查产出_0923/A3_Cards页.md` §五·1 + `bundle_menus_assets_all` 的 `CardCostFilter` MB。</summary>
        public int CostMax;

        /// <summary>「没有筛选」= **原版出厂态**。
        /// 🔴 **2026-09-28 核（子代理逐条实读，推翻了我们文档里原来的说法）**：原版 `Owned Toggle`
        ///   **出厂就是【开】** —— ① prefab `EverguildToggle.m_IsOn = 1`；② `DeckEditingWindow.showOnlyOwnedCards`
        ///   字段初值 = `true`（`.ctor` 里写 `0x101`）；③ `TryOpen` 再把它同步回开关
        ///   （`DeckEditingWindow__TryOpen.c:49-52`）。⚠️ 我们文档原来写「原版默认是关」，**是错的**。
        /// `Upgradable` 恰好相反：prefab 也是 1，但 `UpgradableCardFilter__SetupFilter.c:16` 一跑就
        /// `toggle.isOn = false`（且订监听在后）⇒ **运行期默认关**。
        /// ⚠️ 单机**全解锁** ⇒ `Owned` 开着**不筛掉任何一张**（如实注释，不是静默失效）。</summary>
        public static DeckFilter None
        {
            get { return new DeckFilter { Name = "", Faction = "", Rarity = "", Type = "", Cost = -1, CostMax = 0, Owned = true, Upgradable = false }; }
        }

        /// <summary>多选：阵营 / 稀有度 / 类型的**已选集合**（**并集**语义）。
        ///
        /// <para>🔴 **2026-10-25（`C` 域 · 多选筛选）**：原版 `ToggleCollectionFilter.typeFilters` /
        /// `CardArmyFilter` 那一族存的是**集合**（判据 → `资料/完全复刻/判据/共用件_过滤模型_B升A.md`），
        /// 我们原来是**单值** ⇒ 这条补「同一类可以多选、结果取**并集**」。</para>
        /// <para>⚠️ **兼容路径（本轮的关键）**：集合为 `null` / 空 ⇒ **仍然看上面的单值字段**
        /// （`Faction` / `Rarity` / `Type`）⇒ 今天没有调用点填集合 ⇒ **行为零变化、既有断言全绿**；
        /// UI 接上集合之后单值字段自然让位（下一步，见 `报告/C_多选筛选_第1轮.md`）。</para></summary>
        public HashSet<string> Factions, Rarities, Types;

        /// <summary>🆕 **2026-10-25（多选筛选 · 费用）**：已选**费用档位**集合（原版 `CardCostFilter` 也是集合，
        /// `ToggleCollectionFilter<RawCardScript,int>` 的 `typeFilters`）。
        /// <para>⚠️ **兼容路径**：集合 `null`/空 ⇒ 仍走 `Cost`/`CostMax` 那对**区间**字段（老口径）；
        /// 集合非空 ⇒ 用 <see cref="CostOk"/> 的集合支（**夹过的费用 ∈ 集合**）。</para></summary>
        public HashSet<int> Costs;

        /// <summary>费用是否通过：集合非空 ⇒ **`ClampPoolCost(cost)` ∈ 集合**（与原版一样**夹完再比**）；
        /// 否则走 `Cost`/`CostMax` 区间（`CostMax <= 0` ⇒ 只看 `Cost` 一个值）。</summary>
        public bool CostOk(int cost)
        {
            if (Costs != null && Costs.Count > 0) return Costs.Contains(DeckEditorState.ClampPoolCost(cost));
            if (Cost < 0) return true;
            int hi = CostMax > 0 ? CostMax : Cost;
            int c = DeckEditorState.ClampPoolCost(cost);
            return c >= Cost && c <= hi;
        }

        /// <summary>UI 用：费用那一档亮不亮（集合优先、区间兜底）。</summary>
        public bool CostSelected(int lo)
        {
            if (Costs != null && Costs.Count > 0) return Costs.Contains(lo);
            return Cost == lo;
        }

        /// <summary>阵营是否通过：**集合非空 ⇒ 属于集合**（逐项 `DeckRules.SameFaction`，与单值同一把尺）；
        /// 否则走单值（空串 = 不限）。</summary>
        public static bool FactionOk(DeckFilter f, string faction)
        {
            if (f.Factions != null && f.Factions.Count > 0)
            {
                foreach (var x in f.Factions) if (DeckRules.SameFaction(faction, x)) return true;
                return false;
            }
            return string.IsNullOrEmpty(f.Faction) || DeckRules.SameFaction(faction, f.Faction);
        }

        /// <summary>稀有度是否通过（集合非空 ⇒ 属于集合；比较一律**不敏感**，与单值那一支同口径）。</summary>
        public static bool RarityOk(DeckFilter f, string rarity)
        {
            if (f.Rarities != null && f.Rarities.Count > 0)
            {
                foreach (var x in f.Rarities)
                    if (string.Equals(rarity ?? "", x ?? "", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            return string.IsNullOrEmpty(f.Rarity) || string.Equals(rarity ?? "", f.Rarity, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>卡类是否通过（集合非空 ⇒ 属于集合；否则单值）。</summary>
        public static bool TypeOk(DeckFilter f, string type)
        {
            if (f.Types != null && f.Types.Count > 0)
            {
                foreach (var x in f.Types) if (string.Equals(type ?? "", x ?? "", StringComparison.Ordinal)) return true;
                return false;
            }
            return string.IsNullOrEmpty(f.Type) || type == f.Type;
        }

        /// <summary>UI 用：**这一格现在是不是「选中」状态**（与 <see cref="FactionOk"/> 的区别：
        /// `*Ok` 回答「这张卡过不过」，空筛选时返回 **true**；`*Selected` 回答「这一格亮不亮」，空筛选时返回 **false**）。
        /// 🔴 两者**语义不同、别互相替代**（2026-10-25 多选筛选）。</summary>
        public static bool FactionSelected(DeckFilter f, string v)
        {
            if (f.Factions != null && f.Factions.Count > 0)
            {
                foreach (var x in f.Factions) if (DeckRules.SameFaction(v, x)) return true;
                return false;
            }
            return DeckRules.SameFaction(v, f.Faction ?? "");
        }

        /// <summary>UI 用：稀有度那一格亮不亮（集合优先、单值兜底）。</summary>
        public static bool RaritySelected(DeckFilter f, string v)
        {
            if (f.Rarities != null && f.Rarities.Count > 0)
            {
                foreach (var x in f.Rarities)
                    if (string.Equals(x ?? "", v ?? "", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            return string.Equals(f.Rarity ?? "", v ?? "", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>UI 用：卡类那一格亮不亮（集合优先、单值兜底）。</summary>
        public static bool TypeSelected(DeckFilter f, string v)
        {
            if (f.Types != null && f.Types.Count > 0)
            {
                foreach (var x in f.Types) if (string.Equals(x ?? "", v ?? "", StringComparison.Ordinal)) return true;
                return false;
            }
            return string.Equals(f.Type ?? "", v ?? "", StringComparison.Ordinal);
        }

        /// <summary>「筛选一个字都没设」：单值五项全空 **＋** 四个多选集合（`Factions`/`Rarities`/`Types`/**`Costs`**）全空
        /// （集合是 2026-10-25 多选改造的第 2 条路 ⇒ **同一个谓词的两半都得算**；漏 `Costs` ⇒ 只勾费用档时 `Clear filters` 不亮，2026-10-26 补）
        /// ⚠️ **不含 `Owned` / `Upgradable`** —— 那两个是独立开关（原版 `Clear filters` 清的是 `filters[]`，**不动** `showOnlyOwnedCards`），且 `Owned` 出厂就开、单机全解锁下不改变结果 ⇒ 不是「有没有筛选」的判据。</summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Faction)
                    && string.IsNullOrEmpty(Rarity) && string.IsNullOrEmpty(Type) && Cost < 0
                    && (Factions == null || Factions.Count == 0)
                    && (Rarities == null || Rarities.Count == 0)
                    && (Types == null || Types.Count == 0) && (Costs == null || Costs.Count == 0);
            }
        }
    }

    /// <summary>
    /// 卡组编辑器的状态。**改这个类就要加断言**（`DeckScene.Run` 里那一段）。
    /// </summary>
    public class DeckEditorState
    {
        public const int AnyCost = -1;

        readonly List<CardDef> _pool = new List<CardDef>();

        /// <summary>🆕 **2026-09-27（用户给的规格）**：卡池要不要**按督军分流**。
        /// `true`（**只有卡组编辑那条路会开**）：
        ///   · 卡组**还没有督军** ⇒ 卡池**只列督军**（各阵营的都在）—— 玩家从这里挑一个；
        ///   · 督军已定 ⇒ 卡池**只列该阵营的卡**（含该阵营其它督军，可换；防御卡也在里面）。
        /// `false`（**收藏窗 / 纯逻辑自检**）：照旧「按筛选条件过一遍全池」—— 收藏没有督军这回事。
        /// 🔴 **默认 false 是故意的**：`DeckScene` 有一条断言「不设筛选时命中全部」，
        ///   而它用的 state 不是运行时那个 ⇒ 默认一开就会把那条打红。</summary>
        public bool WarlordGatedPool;

        /// <summary>🆕 **2026-10-18（卡池排序与显隐 · 原版 `showInCollectionOptions`）**：
        /// 卡池 / 收藏共用的**显示判据开关**（<see cref="PassesShowInCollection"/>）。
        /// `true` ⇒ 按原版那条 `Where(showInCollectionOptions != Hide(0))` 把**122 张 `show=0`**
        /// （92 张天赋卡 + 30 张 `Z-NC` 效果卡）剔掉 —— 收藏卡牌页由 `CollectionWindow.CardsState` 打开。
        /// 卡组编辑那条路（<see cref="WarlordGatedPool"/>）**自带同一条**（原版两条链都有）。
        /// 🔴 **默认 false 是故意的**：纯逻辑用例（`DeckScene.TestFilters` 等）要「不设筛选时命中全部」；
        /// 而且 `Core` 层的「能不能进卡组」（`DeckRules.CannotAddToDeck`）是**另一条判据**，别混死
        /// （原版：显示 = `showInCollectionOptions`，准入 = `inventoryOptions` + `showInCollectionOptions`）。</summary>
        public bool ShowInCollectionOnly;

        /// <summary>🆕 **2026-10-18**：排序要不要**多夹一层 `inventoryOptions`**。
        /// 原版**只有收藏卡牌页**那条链夹（`CardCollectionTab__GetCollection.c:99` 的
        /// `OrderBy&lt;CardInventoryOptions&gt;`，夹在「战将优先」与「费用」之间）；卡组编辑卡池没有这一层
        /// （`DeckEditingWindow__GetCardCollection.c` 只有 4 个 `OrderBy`）⇒ 收藏窗（`CollectionWindow.CardsState`）
        /// 打开它。次序见 <see cref="SortForDisplay"/>。</summary>
        public bool SortByInventoryOptions;

        readonly Dictionary<string, CardDef> _byId = new Dictionary<string, CardDef>();

        public DeckEditorState(IEnumerable<CardDef> pool)
        {
            if (pool != null)
                foreach (var c in pool)
                {
                    if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                    if (_byId.ContainsKey(c.Id)) continue;      // 卡池里真有重名就取第一张
                    _byId[c.Id] = c;
                    _pool.Add(c);
                }
            Deck = new PlayerDeck();
            // ⚠️ 必须显式初始化 —— `default(DeckFilter)` 的 `Cost` 是 0，而 0 是个**合法费用**，
            //    不初始化的话筛选默认变成「只看 0 费卡」（自检抓到过：1127 张池子筛成 103 张）
            Filter = DeckFilter.None;
        }

        public IReadOnlyList<CardDef> Pool { get { return _pool; } }
        public int PoolCount { get { return _pool.Count; } }

        /// <summary>正在编辑的卡组。</summary>
        public PlayerDeck Deck { get; private set; }

        public DeckFilter Filter { get; set; }

        /// <summary>是否处于「遭遇模式」规则下（12 张）。
        /// 🔴 **2026-09-26 改成派生只读** —— 原来是裸 `bool` 字段，而**全仓没有任何地方给它赋值**
        /// （实测 grep 0 命中）⇒「玩家自建遭遇卡组」那条路**静默走不通**。
        /// 现在从**正在编辑的那副卡组**派生，照原版：编辑器不认识「当前模式」，
        /// 它读 `EditingDeck.GameMode`（`DeckEditingWindow._GetCardCollection` 那条链）。
        /// 判据全文 → `资料/加时与冲突模式_原版规格.md` §2.7。</summary>
        public bool Skirmish { get { return Deck != null && Deck.IsSkirmish; } }

        public CardDef Find(string id)
        {
            CardDef c;
            return (id != null && _byId.TryGetValue(id, out c)) ? c : null;
        }

        // ------------------------------------------------------------ 卡组

        /// <param name="gameMode">本副卡组的模式（`0` 经典 · `13` 遭遇）。照原版
        /// `SelectDecksTab.CreateDeck`（`GetEmptyDeck()` 之后立刻打上当前模式）—— **建组时定死**。</param>
        public void NewDeck(string name, int gameMode = 0)
        {
            // 🔴 **2026-10-18（双语线 · 波 1 · P1）**：默认名走词条 `MenuDeck/NewDeckName`
            // 🔶 自己设计（非原版）：新建卡组默认名的词条键 `MenuDeck/NewDeckName` · 原版两张表都查不到这条键，键名由调度台裁定自拟 · 调度台 2026-10-18 裁定｜原注：（键名 = 调度台 2026-10-18 裁定、自拟；中文列 = 改之前这里写死的 `新卡组` ⇒ 中文档零变化）。
            //   ⚠️ **同一个串的第二处**在 `Deck/DeckRuntime.cs` 的清名那一拍（`SetDeckName`）；
            //      第三处（`Shell/CollectionData.cs:260` 的 `Lib.UniqueName("新卡组")`）**不在本笔白名单里** ⇒
            //      已写进交件报告（铁律 6：同一件事只留一条键，三处一律用 `MenuDeck/NewDeckName`）。
            Deck = new PlayerDeck(string.IsNullOrEmpty(name) ? Loc.T("MenuDeck/NewDeckName") : name, null, null, null, gameMode);
        }

        /// <summary>把一副卡组装进编辑器。🔴 **装的是【副本】** —— 这是原版那条链的语义。
        ///
        /// <para>判据 = 原版 `DeckEditingWindow__TryOpen`
        /// （`d:/2/tools/decomp_full/DeckEditingWindow__TryOpen.c:41-45`）：
        /// `lVar5 = thunk_FUN_18042f4a0(DAT_1842d42f8)`（**`new` 一个 `CardDeck`**）→
        /// `CardDeck___ctor(lVar5, plVar8, 0)`（**拷贝构造**，`plVar8` = 开窗时传进来那副，
        /// 它在窗的 `+0x40`、类型检查对着 `CardDeck`）→ `*(longlong *)(param_1 + 0x118) = lVar5`
        /// （窗上那个 `EditingDeck`（`get_EditingDeck` 读的就是 `+0x118`）存的是**副本**）；
        /// 连「清掉无效卡」也只动副本（`:47 DeckUtility__RemoveInvalidCards(*plVar1, 0)` ——
        /// ⚠️ 2026-10-19（`A1896`）实读订正：原文写 `:46`，实际那一句在第 **47** 行）。
        /// 真写回**库里那份**只在一处 = `DeckEditingWindow__UploadDeck.c` 的
        /// `CardDeck__CopyDeck(库那份 ← 编辑中那份)`（= 我们 `Done`/`ESC` 那一拍的
        /// `DeckRuntime.CommitDeck()` → `DeckLibrary.CommitCurrent`）。</para>
        ///
        /// <para>🔴 **2026-10-13（A397）改向**：原来这里是 `Deck = deck ?? new PlayerDeck();`
        /// （**直接赋引用，不拷贝**）⇒ `State.Deck` 就是 `DeckLibrary.Current` **那个对象本身**，
        /// 于是**任何库级 `Save()`**（`Create` / `Delete` / `Add` / `Duplicate` / `Rename`
        /// —— 它们序列化的是**整份库**）会把编辑器里**还没 Done 的改动顺手写盘**。原版不会：
        /// 那些改动只活在副本里。</para>
        ///
        /// <para>⚠️ **调用方要知道的两件事**（都是这次改向带出来的，别当成缺陷）：
        /// ① 编辑器里的改动**不再**自动出现在 `Library.Current` 上（内存里也看不到）——
        ///    要让库看见只有一条路：`DeckRuntime.CommitDeck()`（`Done`/`ESC` 那一拍）；
        /// ② `DeckLibrary.CommitCurrent` 写的是**当前选中那一套**（`_current`），所以「编辑副本」
        ///    这件事还依赖「编辑期间 `_current` 不变」。编辑器自己从不改选中项（原版是按
        ///    「开窗时传进来那一副」写回，见 `__UploadDeck.c` 里那个 `window+0x40`）⇒ 两条等价。</para>
        /// </summary>
        public void LoadDeck(PlayerDeck deck)
        {
            // `PlayerDeck.Clone()`（`RuleEngine/Core/DeckRules.cs:342`）逐字段拷：
            // 名字 / 督军 / 防御卡 / 卡表（**新 List**，不与传入那副共享）/ 模式 / 卡背 —— 六格全在。
            Deck = deck != null ? deck.Clone() : new PlayerDeck();
            // ⚠️ 名字**照原样**再写一次：`PlayerDeck` 的构造器（`Clone` 走的就是它）会把**空/null 名字**
            //    替成「新卡组」（`Core/DeckRules.cs:335`），而拷贝构造要**逐格照抄**
            //    （原版 `CardDeck___ctor(副本, 原副, 0)` 就是逐格抄）——
            //    少这一行 = 一副「名字被清空过」的卡组一进编辑器就**静默改名**（铁律：不许静默）。
            if (deck != null) Deck.Name = deck.Name;
            // `Clone()` 与 `PlayerDeck()` 都给非 null 的 `CardIds`；这一行是**兜底不变式**（保持原样无害）。
            if (Deck.CardIds == null) Deck.CardIds = new List<string>();
            // 🔴🔴 **2026-10-19（`A1896` · 用户拍板「方案 B」）：打开编辑器时**逐张摘除**不合法卡。**
            //   判据 = 原版 `DeckEditingWindow__TryOpen`（`d:/2/tools/decomp_full/`，逐句实读）：
            //     `:41-45` 先 `new CardDeck(deckToCopy)` 拷一份（= 上面那行 `Clone()`）→
            //     `:47` `DeckUtility__RemoveInvalidCards(*plVar1, 0)` **对副本**逐张摘掉不合法卡 →
            //     `:141` `*(bool*)(*plVar1 + 0x60) = (removed == 0)`（**摘了就标脏**；原版 `+0x60` 是
            //     「干净」标志：`DeckEditingWindow__TryClose.c:9` 里 `== 0` 才走 `ConfirmDiscard`）。
            //   ⚠️ 逐条判据**只有一处实现** = `RuleEngine/Core/DeckRules.RemoveInvalidCards`（别在这儿重写）。
            //   ⚠️ 全库调用点**只有这一处** —— 原版开局**不跑**它（判据 `BattleManager__CreatePlayerDeck.c`：
            //     拿 `cardLibrary` 直接逐张建卡）；我们 `BattleDriver.ResolveDeck` 里原来那一处已按 `A1896` 移走。
            //   ⚠️ 摘除**只动这份副本**：传进来那副（库/存档/调用方的对象）一个字节都不动 ——
            //     原版也是先 `new CardDeck(deckToCopy)` 再摘。
            //   ⚠️ `+0x60`（脏标记）那半**本文件不拥有**（出口是 `DeckRuntime.DeckDirty`）——
            //     `LastLoadRemoved` 留给调用方接，缺口记在 `W_引擎线_A1896_1019b.md`。
            LastLoadRemoved = DeckRules.RemoveInvalidCards(Deck, Find);
            foreach (var rc in LastLoadRemoved)
                UnityEngine.Debug.LogWarning($"[Deck] 打开编辑器时按原版摘掉一张 `{rc.Id}`"
                                           + $"（原版判据 `{DeckRules.Describe(rc.Why)}`）");
        }

        /// <summary>最近一次 <see cref="LoadDeck"/> 按原版 `DeckUtility.RemoveInvalidCards` 摘掉的卡
        /// （= **打开编辑器**那一趟的结果；空表 = 一张都没摘）。
        /// ⚠️ 原版同一步还会把「摘过」写进 `CardDeck+0x60`（脏标记）—— 我们那半还没接
        /// （`DeckRuntime.DeckDirty` 的出口不在本文件）⇒ 这里先把结果摊开给调用方/断言用。</summary>
        public List<DeckRules.RemovedCard> LastLoadRemoved { get; private set; }
            = new List<DeckRules.RemovedCard>();

        /// <summary>
        /// 校验这副卡组。**它有一个【副作用】，是照原版搬过来的**：
        /// **合法 + 卡组名是空的时候，把名字补成督军卡名。**
        ///
        /// <para>判据 = 原版 `DeckUtility.ValidateDeck`（`d:/2/tools/decomp_full/DeckUtility__ValidateDeck.c`）——
        /// 它在**所有校验都过了**之后那一段里做这件事：
        /// · `:69` 张数必须**正好**等于该模式的 `deckSize`（`iVar5` 默认 `0x1e`=30，自定义事件另有值）；
        /// · `:70` `System_String__IsNullOrWhiteSpace(deck.deckName)`（`deckName` 在 `deck+0x10`）——
        ///   **空 / 全空白**才算空；
        /// · `:73-75` `deck.deckName = RawCardScript_GetLocalizedCardName(deck.deckHero)`（督军在 `deck+0x40`）
        ///   —— 补的是**督军那张卡的本地化卡名**；
        /// · `:77-78` 补完 `err = 0`、`return 1`（合法）。
        /// 不合法时**不补**（`:80` 起直接给错误码 / 走别的错误支）。</para>
        ///
        /// <para>🔴 **为什么补名这件事不能留在「改名」那一处做**（A365，2026-10-12）：
        /// 原版的改名（`DeckEditingPanel__ChangeName.c:6`）是**无条件**写进 `deck.deckName` 的
        /// —— **空名照样接受**，补名是**校验**那一拍的产物。原版 `ValidateDeck` 有**两个**调用点
        /// （`DeckEditingWindow__UpdateDoneButton.c:24` 点亮 Done、`__TrySaveDeck.c:80` 保存），
        /// **参数逐字相同** ⇒ 「灯亮」与「放行」看到的是同一个结果，补名也就跟着 Done 灯一起发生。
        /// 我们这里 `RefreshHeader()` 也是拿它点亮/熄灭 Done（`foot_hl`）⇒ 同一个位置。</para>
        ///
        /// <para>⚠️ **如实标两处差别**：
        /// ① 原版补的是「本地化卡名」，我们给的是 `CardText.Name(Name, NameZh)` —— 就是本工程卡面上
        ///    印的那个名字（中文优先），与 `DeckRuntime` 里别处写卡名走同一条；
        /// ② 原版补名**不**顺手标脏（方法体里没有写 `deck+0x60` 那一句）⇒ 我们也不标
        ///    （`DeckRuntime.MarkDeckDirty` 不由这里调）。</para>
        /// </summary>
        public DeckError Validate()
        {
            var e = DeckRules.Validate(Deck, Find, Skirmish);
            if (e == DeckError.None) FillNameFromWarlord();
            return e;
        }

        /// <summary>合法卡组 + 空名字 ⇒ 补成督军卡名。判据逐条见 <see cref="Validate"/>。</summary>
        void FillNameFromWarlord()
        {
            if (!string.IsNullOrWhiteSpace(Deck.Name)) return;
            var w = Find(Deck.WarlordId);
            // 走到这里督军一定在（`DeckRules.Validate` 的 ② 那一步否则会给 `NoWarlord`）——
            // 这一行只是「取不到就什么都不做」，不静默编一个名字出来。
            if (w == null) return;
            // 🆕 2026-10-17（`N42`）：按 `CardDef` 取 —— 非中英语档用官方 13 语言卡名（`Card_Name/<id>`）
            Deck.Name = CardText.Name(w);
        }

        /// <summary>卡组还差几张（负数=超了）。UI 上「12/30」那个计数用它。</summary>
        public int SlotsLeft { get { return DeckRules.CardCount(Skirmish) - Deck.CardIds.Count; } }

        public int DeckCount { get { return Deck.CardIds.Count; } }
        public int MaxDeckCount { get { return DeckRules.CardCount(Skirmish); } }

        // ------------------------------------------------------------ 加/删

        /// <summary>
        /// 能不能把这张加进卡组。**判据全部转发给 `DeckRules`**，这里只做「单张牌」这一层的检查 ——
        /// 整副牌的合法性另有 <see cref="Validate"/>，两件事别混。
        /// <para>🆕 **2026-10-25（完全复刻 · R3）**：补上原版 `CanAddCard` 的「**传奇总数**」那一档
        /// （判据/出处/为什么返回 `CopyLimitExceeded` → <see cref="LegendaryTotalReached"/>）；
        /// 同时把「督军/防御卡槽里已经有人 ⇒ 一律挡」改成**原版口径：只挡「就是这张」与「异阵营」，同阵营 = 换**
        /// （判据 = `CardDeck.CanAddHero` / `CanAddDefensive`，见下面那两支的注释）。</para>
        /// </summary>
        public DeckError CanAdd(CardDef c)
        {
            return CanAddCore(c);
        }

        /// <summary><see cref="CanAdd"/> 的真身。判据/出处 → <see cref="LegendaryTotalReached"/>。
        /// <para>🟢 **2026-10-25（`E1` · 第五段）：不再需要 `out bool legendaryTotal` 那个载体了** ——
        /// `DeckError` 已经补上 `LegendaryTotalExceeded`（住 `RuleEngine/Core/DeckRules.cs`），
        /// 「同名上限」与「传奇总数」现在**返回值就能分开** ⇒ 任何按返回值出文案的地方都不会再说反。</para></summary>
        DeckError CanAddCore(CardDef c)
        {
            if (c == null) return DeckError.UnknownCard;

            // 督军和防御卡走各自的位子，不占普通卡位，也各只能有一个。
            // ⚠️ 「换」有两个入口：① **这里**（`TryAdd` 直接替换槽位）② `SetWarlord`（先清不合阵营的卡再换，自检在用）。
            // 🔴 **2026-10-25（R3）改口径：槽里已经有人 ≠ 一律挡。** 原来这两支见到 `!= null` 就回
            //    `WarlordAlreadySet` / `DefensiveAlreadySet`；**原版不是那样** ——
            //    `CardDeck.CanAddHero` / `CanAddDefensive`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardDeck.cs:960-1136`，
            //    Ghidra 同形）的判定只有三层：
            //      ① 槽里**就是这一张** ⇒ 挡（给 `MenuDeck/Error/MaxCopiesReached`）；
            //      ② 槽里是**别的**同型卡 ⇒ **只比阵营**：`rawScript.cardArmy != card.cardArmy` ⇒ `MenuDeck/Error/InvalidArmy`；
            //      ③ 其余 ⇒ **放行**（`TryAddCard` 随后整张替换：`deckHero = card; deckArmy = card.cardArmy` /
            //         `deck[+0x48] = card`）。
            //    ⇒ 玩家在池里点**同阵营**的另一位战将 / 另一张防御卡 = **换**，不是被挡；
            //      只允许同阵营换 ⇒ 原有卡仍然合法，**换完不需要清卡**（原版 `TryAddCard` 里也没有清）。
            //    🆕 **2026-10-26（B9 · 卡组:8）已按原版对齐**：「槽里就是这一张」改回 `CopyLimitExceeded`
            //      （= 原版 `MenuDeck/Error/MaxCopiesReached` 那一档；`*AlreadySet` 两档从此不由本函数产生）。
            if (c.Type == "hero")
            {
                if (Deck.WarlordId == null) return DeckError.None;
                if (Deck.WarlordId == c.Id) return DeckError.CopyLimitExceeded;
                var curW = Find(Deck.WarlordId);
                return (curW == null || DeckRules.SameFaction(curW.Faction, c.Faction)) ? DeckError.None : DeckError.WrongFaction;
            }
            if (c.Type == "defence")
            {
                if (Deck.DefensiveId == null) return DeckError.None;
                if (Deck.DefensiveId == c.Id) return DeckError.CopyLimitExceeded;
                var curD = Find(Deck.DefensiveId);
                return (curD == null || DeckRules.SameFaction(curD.Faction, c.Faction)) ? DeckError.None : DeckError.WrongFaction;
            }

            // 🔴 **2026-10-14（`A1798`）改口径**：原来是「用户 2026-09-27 拍板的、按兵种猜的三类
            //    （药剂/破坏/秘仪）」。现在改用**原版真值**：判据只此一处 = `DeckRules.CannotAddToDeck`
            //    （原版 `DeckUtility__ValidateDeckCards.c:68-77`，`inventoryOptions` / `showInCollectionOptions`）。
            //    ⚠️ 那个函数**内部**已经照原版把 hero / 防御卡路由走（`CanAddCard.c:32-39`），
            //    所以这里不必再自己挡一遍（上面那两行早返回的仍在，是**槽位**口径不是准入口径）。
            if (DeckRules.CannotAddToDeck(c)) return DeckError.EffectOnlyCard;

            // 阵营：以督军为准；还没选督军时先按卡自己的阵营（选督军时会清掉不合的）
            if (!string.IsNullOrEmpty(Deck.WarlordId))
            {
                var w = Find(Deck.WarlordId);
                if (w != null && !DeckRules.SameFaction(w.Faction, c.Faction)) return DeckError.WrongFaction;
            }

            if (Deck.CountOf(c.Id) >= DeckRules.CopyLimit(c.Rarity)) return DeckError.CopyLimitExceeded;
            if (LegendaryTotalReached(c)) return DeckError.LegendaryTotalExceeded;   // 原版 `:100-147`（2026-10-25：不再借 CopyLimitExceeded）
            // 🔴 2026-10-26（B9 · 卡组:8）：「整副满」是**最后**一道闸 —— 原版 `CanAddCard.c:166`（`EmptySlotsCount` 那一句）
            if (Deck.CardIds.Count >= MaxDeckCount) return DeckError.TooManyCards;
            return DeckError.None;
        }

        /// <summary>加一张牌。<paramref name="why"/> 拿不合法的原因（**给人看的那句话**；合法时为空串）。
        ///
        /// <para>🔴 **2026-10-18（A985①）**：`DeckRules.Describe` 已改成**只出词条键**
        /// （`"MenuDeck/Error/" + 枚举名`，见它的 doc），所以这个 `out` 口必须在**这里**过一遍
        /// `Loc.T(...)` —— ⛔ 不然调用点拿到的是**键名**（`MenuDeck/Error/CopyLimitExceeded`），
        /// 而它同时被当诊断串与「人话原因」用（`Editor/DeckScene.cs` 那条
        /// `CheckTrue(!string.IsNullOrEmpty(why), "被挡时给出了人话原因")`）。
        /// 判据与唯一先例 = `DeckRuntime.DeckErrorText`（`Loc.T(Describe(e))` 的那一处），⛔ 别在调用点再包第二遍
        /// （`Loc.T` 拿到一个**不是键**的字符串会返回它自己 + 记一次 `MissingCount` + 出一条 warning）。
        /// 🆕 **2026-10-25（R3）**：这一跳原来要问 `CanAddCore` 的 `out legendaryTotal` 才分得清两档文案；
        /// **第五段起不用了** —— `DeckError.LegendaryTotalExceeded` 让 `Describe` 自己就能出对键
        /// （`MenuDeck/Error/MaxLegendayCopiesReached`）⇒ 这里统一 `Loc.T(Describe(e))`。</para></summary>
        public bool TryAdd(CardDef c, out string why)
        {
            var e = CanAddCore(c);
            why = e == DeckError.None ? "" : DeckRuntime.DeckErrorText(e);
            if (e != DeckError.None) return false;

            if (c.Type == "hero") Deck.WarlordId = c.Id;          // 换督军 = 直接替换（`CanAdd` 已保证只允许**同阵营**换 ⇒ 不需要清卡）
            else if (c.Type == "defence") Deck.DefensiveId = c.Id;
            else Deck.CardIds.Add(c.Id);
            return true;
        }

        public bool TryAdd(string id, out string why) { return TryAdd(Find(id), out why); }

        /// <summary>
        /// 选督军。**会把不合阵营的卡清掉** —— 规则书 :53「所有卡必须与督军同阵营」，
        /// 留着它们的话卡组一直是非法的，不如直接清并让 UI 报一声。
        /// 返回被清掉的张数。
        /// </summary>
        public int SetWarlord(string warlordId)
        {
            var w = Find(warlordId);
            if (w == null || w.Type != "hero") return 0;
            Deck.WarlordId = w.Id;

            int removed = 0;
            for (int i = Deck.CardIds.Count - 1; i >= 0; i--)
            {
                var c = Find(Deck.CardIds[i]);
                if (c == null || !DeckRules.SameFaction(c.Faction, w.Faction)) { Deck.CardIds.RemoveAt(i); removed++; }
            }
            var d = Find(Deck.DefensiveId);
            if (d != null && !DeckRules.SameFaction(d.Faction, w.Faction)) { Deck.DefensiveId = null; removed++; }
            return removed;
        }

        /// <summary>从卡组拿走一张（普通卡位；督军/防御卡要单独清）。</summary>
        public bool TryRemove(CardDef c)
        {
            if (c == null) return false;
            if (c.Type == "hero") return Deck.WarlordId == c.Id && ClearWarlord();
            if (c.Type == "defence") return Deck.DefensiveId == c.Id && ClearDefensive();
            return Deck.RemoveCard(c.Id);
        }

        public bool ClearWarlord() { if (Deck.WarlordId == null) return false; Deck.WarlordId = null; return true; }
        public bool ClearDefensive() { if (Deck.DefensiveId == null) return false; Deck.DefensiveId = null; return true; }

        /// <summary>清空整副（保留名字，换督军时用得上）。</summary>
        public void ClearCards()
        {
            Deck.CardIds.Clear();
            Deck.WarlordId = null;
            Deck.DefensiveId = null;
        }

        // ------------------------------------------------------------ 筛选

        /// <summary>费用档夹取的下界 / 上界（原版 `CardCostFilter` 的 `1-` 与 `8+` 那一头）。</summary>
        public const int PoolCostMin = 1, PoolCostMax = 8;

        /// <summary>把**卡的费用**夹到 <c>[1,8]</c> —— 判据 = 原版 `CardCostFilter__FilterItem.c:15-20`
        /// （`d:/2/tools/decomp_full/`，现读，**逐字**）：
        /// <code>
        /// iVar2 = *(int *)(param_2 + 0x78);   // 这张卡的 manaCost
        /// if (iVar2 &lt; 2) iVar2 = 1;
        /// else if (7 &lt; iVar2) iVar2 = 8;
        /// </code>
        /// 那两个字面量 <c>2</c> / <c>7</c> **照抄原版**（所以下面写的是 `<c>cost &lt; 2</c>` / `<c>cost &gt; 7</c>`，
        /// ⛔ 别改写成 `&lt; PoolCostMin + 1` 之类 —— 那会让人以为边界可调，而原版是**写死的**）。
        /// 调用链（原版）：`DeckEditingWindow.FilterToDeck()` → `CardCollectionFilterController.SetFiltersToDeck()`
        /// → 本族各 filter 的 `FilterItem`。**收藏窗与卡组编辑共用同一份**（我们两个宿主也共用
        /// <see cref="VisibleCards"/>）⇒ 判据只此一处，⛔ 别在宿主里再写第二份。
        /// 🔴 **夹的是卡那一侧**：「≤1 折进第 1 档 / ≥8 折进第 8 档」正是那两档能匹配上边界卡的原因。
        /// </summary>
        public static int ClampPoolCost(int cost)
        {
            if (cost < 2) return PoolCostMin;    // 原版 `iVar2 < 2 ⇒ 1`（含 0 / 负 —— 就这一条）
            if (cost > 7) return PoolCostMax;    // 原版 `7 < iVar2 ⇒ 8`
            return cost;
        }

        /// <summary>按当前筛选条件过一遍卡池 —— 出来的就是**界面上的显示序**（原版那串 `OrderBy`，见 <see cref="SortForDisplay"/>）。
        ///
        /// <para>🔴 **2026-10-18（卡池排序与天赋卡显隐）就地订正（铁律 5）**：本方法原来写
        /// 「顺序保持卡池原序 —— **不排序**，原版有没有排序规则没查到」。**现在查到了**（判据 = `d:/2/tools/decomp_full/`，现读）：
        /// `DeckEditingWindow__GetCardCollection.c:106,121,136,151` 是 **4 个稳定 `OrderBy`**
        /// （实际主序 = 最后一个键）⇒ **`cardArmy`(10→130) → `cardType != Hero`（同阵营战将整段先）→ `manaCost` → 本地化卡名**；
        /// 收藏卡牌页 `CardCollectionTab__GetCollection.c:69,84,99,114,129` 多夹一层 `inventoryOptions`
        /// ⇒ **阵营 → 战将 → inventory → 费用 → 名**。实现见 <see cref="SortForDisplay"/>。</para>
        ///
        /// <para>🔴 同时把**显示判据**从 `DeckRules.CannotAddToDeck` 换回原版卡池那条链
        /// （`PassesShowInCollection` + 卡组编辑额外来的一层 `PassesEditModeInventory`）——
        /// 「显不显示」与「能不能进卡组」在原版是**两条判据**；混用会让「可见但加不进」的卡静默消失
        /// （5 张暗黑天使秘仪就是被混死的那一类；原版对它们的表现是**显示 + 变灰**）。</para></summary>
        public List<CardDef> VisibleCards()
        {
            var outList = new List<CardDef>();
            var f = Filter;
            string needle = string.IsNullOrWhiteSpace(f.Name) ? null : f.Name.Trim().ToLowerInvariant();

            // 🆕 **2026-09-27（用户给的规格）**：卡组编辑那条路的卡池**按督军分流** ——
            //   · **卡组还没有督军** ⇒ 卡池**只列督军**（**各阵营的都在**）—— 玩家从这里挑一个；
            //   · **督军已定** ⇒ 卡池**只列该阵营的卡**（含该阵营**其它督军**，可以换；防御卡也在里面）。
            //   🔴 用户原话：「卡组没有督军的时候，右边卡库显示的是各个阵营的督军，放入督军后，右边卡库显示该阵营的卡牌。」
            //   ⚠️ **只在 `WarlordGatedPool` 为真时生效**（只有卡组编辑开它）——
            //     收藏窗复用同一个类、`DeckScene` 的纯逻辑自检也复用 ⇒ 一改全局就会把「不设筛选时命中全部」那条断言打红。
            CardDef wl = (WarlordGatedPool && !string.IsNullOrEmpty(Deck.WarlordId)) ? Find(Deck.WarlordId) : null;

            // 🆕 **2026-10-18**：显示闸在两处宿主上都生效 ——
            //   · 卡组编辑（`WarlordGatedPool`）：原版 `DeckEditingWindow.GetCardCollection` 那条链；
            //   · 收藏卡牌页（`ShowInCollectionOnly`）：原版 `CardCollectionTab.GetCollection` 同一条 `Where`。
            bool showGate = WarlordGatedPool || ShowInCollectionOnly;
            // 🆕 2026-10-23（`W64·B-12`）：原版分支③那个字段**一次取好**（属性里要过 `GameplayVariables.For`）
            bool assignDefensiveInEditMode = AssignDefensiveCardsInEditMode;

            foreach (var c in _pool)
            {
                if (showGate && !PassesShowInCollection(c)) continue;       // 原版 `showInCollectionOptions != Hide(0)`
                if (WarlordGatedPool)
                {
                    if (wl == null) { if (c.Type != "hero") continue; }                 // 还没督军 ⇒ 只列督军
                    else if (!DeckRules.SameFaction(c.Faction, wl.Faction)) continue;   // 定了 ⇒ 只列该阵营
                    // 🔴 **2026-10-18**：这一行原来是 `DeckRules.CannotAddToDeck(c)`（准入判据），
                    //   现换成**原版卡池的 `inventoryOptions` 层**。
                    //   ⛔ 别再退回准入判据：那会把「可见但加不进」的卡藏起来（原版是显示+变灰）。
                    // 🆕 **2026-10-23（`W64·B-12`）**：防御卡那一格**不再整类放行**，改由原版分支③的字段决定
                    //   （`AssignDefensiveCardsInEditMode`）—— 只有「经典 + 字段 false」这一格因用户 2026-09-27
                    //   的拍板仍保留「列」（见那个属性的 doc，⛔ 别删）。
                    if (!PassesEditModeInventory(c, assignDefensiveInEditMode)) continue;
                }
                if (needle != null && !NameSearchMatch(c, needle))
                    continue;
                // 🆕 2026-10-25（多选筛选）：三条判定收进 `DeckFilter.*Ok` —— **集合优先、单值兜底**
                //   （集合为空时逐字等价于原来那三行 ⇒ 今天行为零变化）
                if (!DeckFilter.FactionOk(f, c.Faction)) continue;
                if (!DeckFilter.RarityOk(f, c.Rarity)) continue;
                if (!DeckFilter.TypeOk(f, c.Type)) continue;
                // 🆕 2026-09-28（原版两个开关，卡组编辑与收藏窗同一套）：
                //   · `Owned`（原版**出厂就是开** —— 三路实读见 `DeckFilter.None`）：单机**全解锁**
                //     ⇒ 打开它**筛不掉任何一张** ——
                //     不是我们没做，是「已拥有」在这个前提下恒真。**如实注释，不静默**。
                //   · `Upgradable`：我们**没有升级系统** ⇒ 打开它**必然一张不剩** ——
                //     `项目任务.md` §〇 已拍板「恒空是预期的」（界面那边会出声说明）。
                if (f.Upgradable) continue;
                // 🆕 2026-10-25（多选筛选 · 费用）：整段收进 `DeckFilter.CostOk`（**集合优先、区间兜底**）
                if (!f.CostOk(c.Cost)) continue;
                outList.Add(c);
            }

            // 🆕 **2026-10-18：显示序**（原版那 4 个稳定 `OrderBy` ⇒ 阵营 → 战将 → 费用 → 名；收藏窗多夹 inventory）。
            //   🔴 本笔**替换**了原来那一步 `StableGroupByFaction`（「阵营按首次出现、组内保持原序」）——
            //   那一步只是「把掉队的战将挪回自己阵营段」的权宜之计（用户 2026-09-27：「总不能混乱地排吧」），
            //   而原版真值是**按 `cardArmy` 枚举值整表排**（10→130），比「首次出现序」更确定，也顺带解决了掉队那位。
            //   判据见 `SortForDisplay` 的 doc。
            SortForDisplay(outList);
            // 🔴🔴 **2026-10-25（调度台裁定 · 口径 1）：搜索生效时整表【反序】—— 照抄原版。**
            //   判据（双证）：`CardNameFilter.cs:54-170` 的 `CheckInputTextSearch` 是
            //   `int num = collection._size - 1;` 起、每轮 `num--`、命中就 `list.Add(...)`；
            //   `decomp_full/CardNameFilter__CheckInputTextSearch.c:44-46` 同一句（`iVar1 += -1` → `get_Item(param_2,iVar1)`）
            //   ⇒ **结果表 = 传进来那一列的池序反序**（不是排序、不是 bug 修正的对象 —— 原版就这样）。
            //   ⚠️ **前提**：我们的池序 == 原版 `collection` 序。原版进 `CardNameFilter.Filter` 之前，
            //   `collection` 就是 `CardCollectionTab.GetCollection()` 那 5 个 `OrderBy` 排完的那一列
            //   ⇒ 与我们的 `SortForDisplay(outList)` **同一条**（`SortForDisplay` 的 doc 已逐条对上）。
            //   ⛔ **只有卡名搜索这一支翻序**：原版其余筛选器（`ToggleCollectionFilter` 一族 / `CardCostFilter` …）
            //   都是原地 `Where`，**不翻**。所以这里判的是 `needle != null`（= `Filter.Name` 非空）。
            if (needle != null) outList.Reverse();
            return outList;
        }

        /// <summary>卡名搜索**一格**的匹配。判据 = 原版 `CardNameFilter.CheckInputTextSearch`
        /// （🥇 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardNameFilter.cs:54-170`，`decomp_full/` 那份逐句一致）：
        /// · **子串包含**（`String.Contains`），**不是前缀**；**大小写不敏感**（两边都 `ToLower()`）；
        /// · 原版依次比 **六项**，任一中就算命中：
        ///   ① 本地化**卡名**（`GetLocalizedCardName`）
        ///   ② 本地化**效果文字**（`GetLocalizedCardDescWithLineBreak`，**先 `RemoveBetween(desc,"&lt;","&gt;")`**）
        ///   ③ **阵营名**（`GameStaticData.CardArmyToString` = `Loc("Armies/" + 枚举名)`）
        ///   ④ **兵种/种族名**（`GameStaticData.CardRaceToString` = `Loc("Card_Race/" + 种族串)`）
        ///   ⑤ **资料片枚举名**（`PlayerItem.expansionPack` 的 `Enum.ToString()`，`!= Default` 才比）
        ///   ⑥ **稀有度**（`Loc("MainMenu/General/Rarity" + (int)cardRarity)`）
        /// 🔴 **2026-10-25（`C` 收藏域 · K3 P2-⑤）就地订正（铁律 5）** —— 本 doc 原来那张表写着
        ///   「③ 阵营名 ④ 兵种名 ⑤ **卡类型名** ⑥ **费用**」，**⑤⑥ 记错了**：原版**不比卡类型名、也不比费用**，
        ///   那两项是我们的自加。本轮：**删掉「按费用数字搜」**、**补上稀有度**（⑥）；
        ///   ⑤（资料片）见下面「未落地」两行，④ 的本地化名同理。
        /// ⚠️ 我们是双语工程 ⇒ 中英两套（`Name`/`NameZh`、`Desc`/`DescZh`）**都查**（比原版宽一点点，
        ///    原版一次只查当前语言那一套）—— 如实标。
        /// ⚠️ 原版空串 / 全空白 ⇒ **直接返回全集**（`Filter.c` 的 `IsNullOrWhiteSpace` 分支）。</summary>
        static bool NameSearchMatch(CardDef c, string needle)
        {
            // ① 本地化卡名（中英两套）
            if (Hit(c.Name, needle) || Hit(c.NameZh, needle)) return true;
            // ② 本地化效果文字 —— 原版先剥 `<…>` 之间的富文本/图标标记再比
            //    （`CardNameFilter.RemoveBetween(source, "<", ">")`：正则 `<(.*?)>`、**右到左**、替换成 `<>`）
            if (Hit(StripTags(c.Desc), needle) || Hit(StripTags(c.DescZh), needle)) return true;
            // ③ 阵营名：原版 `Loc("Armies/" + 枚举名)`；`CardText.Faction` 就是那一份（唯一映射）
            //    ⚠️ **如实标：比原版多查一条裸 id**（`Hit(c.Faction)`）—— 留作「本地化缺键」时的兜底，
            //    是**我们自加**的（原版只比本地化名）。它只会多命中、不会少命中。
            if (Hit(c.Faction, needle) || Hit(CardText.Faction(c.Faction), needle)) return true;
            // ④ 兵种/种族名 —— 原版 `CardRaceToString`（空 refId 时：兵种走 `MinionRaceToString`、
            //    战将走 `Card_Race/Warlord`、战术走 spellType 枚举名），最终都是 `Loc("Card_Race/" + 串)`。
            //    ✅ **2026-10-25 已接通**：代理 E 按请求把卡面那一条**同款**显示名暴露成
            //    `CardView.RaceDisplayName(subtype)`（bulk → 手写表 → 老键，**3 步口径只此一处**）⇒ 这里直接转它，
            //    不再自己抄第二份。（同时保留裸 subtype：本地化缺键时 `RaceDisplayName` 原样返回英文，两路同值。）
            if (Hit(c.Subtype, needle) || Hit(CardView.RaceDisplayName(c.Subtype), needle)) return true;
            // ⑤ 资料片枚举名（原版 `expansionPack.ToString()`；`Default` 不参与）——
            //    ✅ **2026-10-25 已接通**：代理 A 给 `CardDef` 加了 `Expansion` + `ExpansionSearchName`
            //    （后者就是原版枚举成员名的小写；`<= 0` 返回 null ⇒ 正好是原版「`!= Default` 才比」那一档）。
            if (Hit(c.ExpansionSearchName, needle)) return true;
            //    ⚠️ 顺带：**卡类型名我们不查** —— 原版就没有这一项（`Troops` / `Spells` 在原版搜不到）。
            // ⑥ 稀有度：原版 `Loc(GetTermTranslation("MainMenu/General/Rarity" + (int)cardRarity))`
            //    （`CardNameFilter.cs:140-150`；`CardRarity` 枚举 = None0/Common1/Rare2/Epic3/Legendary4/Special5）。
            //    🔴🔴 **2026-10-25（调度台裁定 · 口径 2）：照原版的调用路径走 —— 无条件取词条翻译再比。**
            //    原版**没有**「键不在表里就跳过比较」这条分支，所以我们也不加。
            //    现况（如实记）：官方词条表 10,854 条 + `d:/2` 全盘 json/txt/csv 里 `MainMenu/General/Rarity*`
            //    **0 命中** ⇒ 我们 `Loc.T` 走缺键档 = **返回键名本身**（`Core/Loc.cs` 的缺键档，
            //    且**按 key 去重**只出声一次 ⇒ 最多 5 条告警，不会按卡刷屏）
            //    ⇒ `Hit("MainMenu/General/Rarity3", …)` ⇒ **搜 `rarity3` 会把所有卡都匹配上**。
            //    ✅ **这就是原版行为**（I2 缺键回键名）。⛔ 别改成比 `Card_Rarity/*`（那是让功能「变好用」，
            //    偏离原版；用户口径 = 一切按原版、不做多余的自我设计）。
            {
                int ri = RarityEnumIndex(c.Rarity);
                if (ri >= 0 && Hit(Loc.T("MainMenu/General/Rarity" + ri), needle)) return true;
            }
            return false;
        }

        /// <summary>稀有度串 → 原版 `CardRarity` 枚举值（`None0/Common1/Rare2/Epic3/Legendary4/Special5`）。
        /// 认不出给 −1。表只有一份（`FilterPanelModel.RarityKeys` 的顺序 = Common…Special）⇒ 用它推 +1。</summary>
        static int RarityEnumIndex(string rarity)
        {
            if (string.IsNullOrEmpty(rarity)) return -1;
            for (int i = 0; i < FilterPanelModel.RarityKeys.Length; i++)
                if (string.Equals(FilterPanelModel.RarityKeys[i], rarity, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            return -1;
        }

        /// <summary>把 `<…>` 之间的内容**整段吃掉**（保留尖括号）—— 原版 `CardNameFilter.RemoveBetween(desc,"&lt;","&gt;")`
        /// 的等价物（正则 `&lt;(.*?)&gt;` + `RightToLeft` + 替换成 `&lt;&gt;`）。不贪心 ⇒ 逐个配对。
        /// ⚠️ 只用于**搜索比较**，不改任何显示串（卡面印的还是原文）。</summary>
        static string StripTags(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('<') < 0) return s;
            var sb = new StringBuilder(s.Length);
            bool inside = false;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '<' && !inside) { inside = true; sb.Append('<'); continue; }
                if (ch == '>' && inside) { inside = false; sb.Append('>'); continue; }
                if (!inside) sb.Append(ch);
            }
            return sb.ToString();
        }

        static bool Hit(string hay, string needle)
        {
            return !string.IsNullOrEmpty(hay) &&
                   hay.ToLowerInvariant().IndexOf(needle, StringComparison.Ordinal) >= 0;
        }

        /// <summary>原版「这张卡该不该出现在**卡池 / 收藏**里」的显示闸 —— **两条链共用这一份**
        /// （卡组编辑 `DeckEditingWindow__GetCardCollection` 与收藏卡牌页 `CardCollectionTab__GetCollection`）。
        /// 逐条落地状态（判据全部现读 `d:/2/tools/decomp_full/`）：
        /// <list type="bullet">
        /// <item>✅ **`showInCollectionOptions != Hide(0)`** —— 判据 = `DeckEditingWindow.__c___GetCardCollection_b__32_2.c:8`
        ///   （`return *(int*)(param_2 + 0x48) != 0;`）与 `CardCollectionTab.__c___GetCollection_b__8_0.c:19-30`。
        ///   取值表见 `CardDef.ShowInCollectionOptions`（`Hide=0 / Show=5 / OnlyIfOwned=10`）。
        ///   现读卡表：**122 张 `show=0`**（92 天赋 + 30 `Z-NC`）⇒ 这一条把它们全挡在外面。</item>
        /// <item>✅ **（恒过）`show != OnlyIfOwned(10) || owned`** —— 判据 = 同族 `b__32_0.c`：
        ///   `if (*(int*)(param_2+0x48) != 10) return 1; return HasItem(card);`。
        ///   我们单机**全解锁** ⇒ `owned` 恒真 ⇒ 这一条恒过。⚠️ **如实标**：这是**边界**（用户「资源 2000（2026-10-24 从 9999 改） / 全解锁」），
        ///   ⛔ 不是判据算出来的；现读卡表 `show=10` 的卡 = **0 张**，今天它连边界都用不上。</item>
        /// <item>⛔ **`!IsHidden()`（不做）** —— 判据 = `b__32_3.c` 的 vtable `0x2A8`，落到
        ///   `PlayerItem.IsHidden()` → `FeatureConfig.IsItemHidden(item)`（`FeatureConfig__IsItemHidden.c:20-40`，
        ///   读服务端下发的两张隐藏表 `+0x20` / `+0x28`）。**这份配置本地没有** ⇒ 这一条**不做**
        ///   （铁律 11 例外①：判据本身取不到；⛔ 别静默当成 `false`，也别按卡名猜一个隐藏集）。</item>
        /// </list>
        /// ⚠️ 真值缺失（`-1`）**不隐藏** —— 不猜（与 `DeckRules.CannotAddToDeck` 对 `-1` 的口径一致）。</summary>
        static bool PassesShowInCollection(CardDef c)
        {
            return c != null && c.ShowInCollectionOptions != 0;       // `Hide(0)`
        }

        /// <summary>🆕 **2026-10-23（`W64·B-12`）**：原版分支③的那一跳 —— **卡组编辑卡池列不列
        /// 「被标了不能进卡组」的防御卡**。判据现读
        /// `d:/2/tools/decomp_full/DeckEditingWindow___GetCardCollection_b__32_1.c:24-40`
        /// （整条链 = `EditingDeck(+0x118).CustomGameModeEvent` → `GameModes.GetActiveEvent(deck.gameMode @0x74)`）
        /// → `PlayEventData.gameplayVariables(+0x80)` → `.assignDefensiveCardsInEditMode(+0x4A)`
        /// ⇒ **`return *(undefined1 *)(*(longlong *)(lVar2 + 0x80) + 0x4a);`**，谓词**真 = 保留**。
        ///
        /// <para>⇒ **`true` = 卡池里列出来 · `false` = 剔掉**（唯一读点；它**不**管「自动分配」——
        /// 原版全客户端只有这一处读它，别扩到别处）。</para>
        ///
        /// <para>⚠️ **判据缺的那一半（如实标）**：**各模式的真值在服务器** ——
        /// `GameplayVariablesData..ctor` 是空桩、全量反编译里没有一处带字面量赋值
        /// ⇒ 本地**不可能**解出每个模式吃到的值（`资料/普查产出_1017/查证_assignDefensiveCardsInEditMode.md` §三 / §六）。
        /// 这里取**我们那份建模值**，走全仓唯一的「模式号 → 参数」入口 `GameplayVariables.For`：
        /// `Skirmish = true`【文案；且 1017 §三 已判那句文案**不是**本字段的证据】/
        /// `Classic = false`（字段无初始化器 ⇒ 本地缺省）。⛔ **别把这当成原版真值**。</para>
        ///
        /// <para>🔴 **用户拍板保留的偏离（不会被原版取代）**：**经典模式编辑卡池【列】防御卡**
        /// （用户 2026-09-27 明确要求「玩家从这儿挑」；2026-10-18 归档为「两条保留现状的偏离」①
        /// → `资料/历史/任务总表_第五十六会话_核销前/界面线.md`）——
        /// 原版 `Classic = false` 会剔掉它们，**⛔ 别按原版改回**。
        /// ⇒ 本属性**只对「字段 false 且非遭遇」这一格**保留「列」，其余照字段走。</para>
        ///
        /// <para>⚠️ **原版无事件时这一支是抛空引用**（`b__32_1.c:40` 的 `FUN_1803f47a0()`，
        /// 注释写着 `Subroutine does not return`，没有兜底值）⇒ 那个异常态**不复刻**
        /// （我们用 `For(...)` 的经典档顶上，判据缺 → 按上一条用户口径处理）。</para></summary>
        bool AssignDefensiveCardsInEditMode
        {
            get
            {
                var vars = GameplayVariables.For((GameMode)(Deck != null ? Deck.GameMode : 0));
                bool v = vars.assignDefensiveCardsInEditMode;
                if (!v && !Skirmish) return true;     // 🔴 用户 2026-09-27 拍板保留的偏离（见上，⛔ 别删）
                return v;
            }
        }

        /// <summary>卡组编辑卡池**额外来的一层**（`inventoryOptions` + 防御卡那一跳）——
        /// 逐支照 `DeckEditingWindow___GetCardCollection_b__32_1.c:24-40` 现读：
        /// <list type="number">
        /// <item>`*（card + 0x4c) != 5` ⇒ **`return 1`（保留）** —— 连卡型都不看；</item>
        /// <item>`inv == 5` 且 `cardType(+0x90) != 0xD2`（不是防御卡）⇒ **`return 0`（剔除）**；</item>
        /// <item>`inv == 5` 且**是**防御卡 ⇒ **`return assignDefensiveCardsInEditMode`**
        ///   （= <see cref="AssignDefensiveCardsInEditMode"/>，**唯一读点**）。</item>
        /// </list>
        /// <para>⚠️ **`InventoryOptions &lt; 0`（真值缺失）这一支是我们自己的**（原版没有这个状态：
        /// 卡资产上恒有值）—— 不猜、不隐藏，与 `DeckRules.CannotAddToDeck` 对 `-1` 的口径一致。
        /// 现读卡表：池内 `inv == 5` 的 **39 张全是 `type == "defence"`**、`defence` 39 张**全为 5**
        /// ⇒ 分支②今天在数据上命中 0 张（口径仍要留着，那是原版的形状）。</para>
        /// <para>⚠️ 收藏卡牌页**不走这一条**（原版 `CardCollectionTab` 只看 `showInCollectionOptions`）——
        /// 那 39 张防御卡在收藏里照旧显示（现读卡表它们 `show=5`）。</para></summary>
        static bool PassesEditModeInventory(CardDef c, bool assignDefensiveInEditMode)
        {
            if (c == null) return false;
            if (c.InventoryOptions < 0) return true;                              // 我们的一条：真值缺失 ⇒ 不猜、不隐藏
            if (c.InventoryOptions != DeckRules.OptionCantAddToDeck) return true;   // 原版 ①：`!= 5 ⇒ return 1`
            if (c.Type != "defence") return false;                                 // 原版 ②：`!= 0xD2 ⇒ return 0`
            return assignDefensiveInEditMode;                                      // 原版 ③：`return 字段`
        }

        /// <summary>原版卡池 / 收藏卡牌页的**显示序**（判据见 <see cref="VisibleCards"/> 的 doc）。
        /// <para>键按**目标次序直接比**（⛔ 不靠「链式 `OrderBy` 最后一个键才是主序」那种隐式行为）：</para>
        /// <list type="number">
        /// <item>**`cardArmy`**：枚举值 **10→130**（本工程那一份 = <see cref="ArmyOrder"/> 的下标；
        ///   不在表里的排最后 —— 与 <see cref="Factions"/> 的兜底同一条口径）；</item>
        /// <item>**`cardType != Hero`**：bool 升序 ⇒ `false` 先 ⇒ **同阵营的战将整段排最前**（不是「单独插一张」）；</item>
        /// <item>🆕 **仅收藏窗**（<see cref="SortByInventoryOptions"/>）：`inventoryOptions` 升序
        ///   （`InInventory(0)` → `CantAddToDeck(5)` → `NotInInventory(10)`）；</item>
        /// <item>**`manaCost` 升序** → **本地化卡名**（复用现成那一把尺 <see cref="DeckRuntime.CompareDisplayName"/>，
        ///   与左侧卡组列表 `DeckEntries` **同一个比较器**，⛔ 别新写第二套）→ **`Id` 序数兜底**
        ///   （`List.Sort` 不稳定，原版 `OrderBy` 稳定；现读池内「同阵营同名」= 0 张 ⇒ 这一档实际不参与）。</item>
        /// </list>
        /// <para>⚠️ 名字**先算一次**再排：`CardText.Name` 要吃词条表，放在比较器里就是 O(n log n) 次查表。</para></summary>
        void SortForDisplay(List<CardDef> list)
        {
            if (list == null || list.Count < 2) return;
            var nameOf = new Dictionary<CardDef, string>(list.Count);
            foreach (var c in list) nameOf[c] = CardText.Name(c) ?? "";
            bool byInv = SortByInventoryOptions;
            list.Sort((a, b) =>
            {
                int k = ArmyRank(a.Faction).CompareTo(ArmyRank(b.Faction));
                if (k != 0) return k;
                k = (a.Type == "hero" ? 0 : 1).CompareTo(b.Type == "hero" ? 0 : 1);
                if (k != 0) return k;
                if (byInv)
                {
                    k = a.InventoryOptions.CompareTo(b.InventoryOptions);
                    if (k != 0) return k;
                }
                k = a.Cost.CompareTo(b.Cost);
                if (k != 0) return k;
                k = DeckRuntime.CompareDisplayName(nameOf[a], nameOf[b]);
                if (k != 0) return k;
                return string.CompareOrdinal(a.Id, b.Id);
            });
        }

        /// <summary>`cardArmy` 的排序秩 = <see cref="ArmyOrder"/> 的下标。
        /// 那张表**就是原版 13 个 option 的 10→130 序号**（判据见它自己的 doc）⇒ 直接当下标用，⛔ 别另建一份枚举表。</summary>
        static int ArmyRank(string faction)
        {
            int i = Array.IndexOf(ArmyOrder, faction ?? "");
            return i < 0 ? int.MaxValue : i;
        }

        /// <summary>卡池里出现过的阵营（给筛选下拉用），按出现顺序。</summary>
        /// <summary>🔴 **2026-10-11（A1503 ⑤）：军种筛选格的**排列顺序** = 原版那 13 个 option 的【序号】。**
        /// <para>判据（只读代理 2026-10-11 现读 `bundle_menus_assets_all` 的 `CardArmyFilter`：
        /// 13 个 option 的 `value` 从 **10 → 130** 逐个解 `background` PPtr，得到的就是这个序）：
        /// `Ultramarines · Goff · SaimHann · Sautekh · BlackLegion · Leviathan · TauEmpire · Sororitas ·
        /// Genestealers · AstraMilitarum · DarkAngels · EmperorsChildren · SpaceWolves`。
        /// ⚠️ **与 `ForgeData.Armies` 的序【不是同一个序】** —— 那个是锻造厂/战役页用的（我们自定），
        /// 这一条只管**筛选栏那一行**，⛔ 别去改 `ForgeData.Armies`（会牵动另外两页）。</para>
        /// <para>🔴 **我们原来是 `seen.Sort(StringComparer.Ordinal)` = 字母序**
        /// （`AstraMilitarum, BlackLegion, DarkAngels, EmperorsChildren, …`）——
        /// 与参考图（`收藏—张牌的界面参考图（打开过滤器按钮后）.png` 左栏那一列）逐格对不上。</para></summary>
        public static readonly string[] ArmyOrder =
        {
            "Ultramarines", "Goff", "SaimHann", "Sautekh", "BlackLegion", "Leviathan", "TauEmpire",
            "Sororitas", "Genestealers", "AstraMilitarum", "DarkAngels", "EmperorsChildren", "SpaceWolves",
        };

        public List<string> Factions()
        {
            var seen = new List<string>();
            foreach (var c in _pool)
                if (!string.IsNullOrEmpty(c.Faction) && !seen.Contains(c.Faction)) seen.Add(c.Faction);
            // 🔴 按**原版 option 序号**排（见 `ArmyOrder`）；**不在表里的**（将来新增的阵营）排到最后、
            //    内部再按字母 ⇒ **稳定**（同一批牌两次跑出同一个序，自检才不会闪）。
            seen.Sort((a, b) =>
            {
                int ia = System.Array.IndexOf(ArmyOrder, a), ib = System.Array.IndexOf(ArmyOrder, b);
                if (ia < 0) ia = int.MaxValue;
                if (ib < 0) ib = int.MaxValue;
                return ia != ib ? ia.CompareTo(ib) : string.CompareOrdinal(a, b);
            });
            return seen;
        }

        /// <summary>卡池里出现过的费用值（给筛选下拉用），升序。</summary>
        public List<int> Costs()
        {
            var seen = new List<int>();
            foreach (var c in _pool) if (!seen.Contains(c.Cost)) seen.Add(c.Cost);
            seen.Sort();
            return seen;
        }

        // ------------------------------------------------------------ 改名

        /// <summary>
        /// 给当前卡组改名。**空名字照原版【接受】**，不再挡。
        ///
        /// <para>判据 = 原版 `DeckEditingPanel__ChangeName`（`d:/2/tools/decomp_full/DeckEditingPanel__ChangeName.c`）：
        /// `:6` 一句 `deck.deckName = 传进来的串`（`deck+0x10`）、`:9` 一句 `deck.syncedToServer = 0`
        /// （就是那个字节 `+0x60`）—— **方法体里没有任何空值判断**。
        /// 「空名怎么办」是**校验**那一拍的事（见 <see cref="Validate"/>：合法就补成督军卡名）。</para>
        ///
        /// <para>🔴 **改向记录（A365，2026-10-12）**：我们原来对空名 `return false` **拒绝**
        /// （注释写「空白名会让卡组列表里那一行没有字」）—— **方向与原版相反**：原版不但接受，
        /// 而且合法时下一拍就会把它补成一个有字的名字。那条注释担心的画面在原版里不会出现。</para>
        ///
        /// <para>返回值 = **名字真的变了没**（变了 ⇒ 调用方要标脏）。⚠️ 我们多做了一步 `Trim()`
        /// （原版存原样）—— 留着它是为了让「全空白」也落进 `IsNullOrWhiteSpace` 那一支、
        /// 并让页头那句「没有名字」的提示（判据 `string.IsNullOrEmpty`）对得齐。</para>
        /// </summary>
        public bool SetDeckName(string name)
        {
            var n = (name ?? "").Trim();
            if (string.Equals(Deck.Name, n, StringComparison.Ordinal)) return false;
            Deck.Name = n;
            return true;
        }

        // ------------------------------------------------------------ 卡池的滚动窗口
        //
        // 🔴 2026-09-20：**原来这里是「翻页」**（Page / NextPage / PrevPage / PageSize=12），
        //    已删 —— 查证结论：**原版没有分页**，卡池是 `PolyAndCode.UI.RecyclableScrollRect`
        //    无限滚动（`CollectionDisplay.cs:11`，全库无任何 Page/Next/Prev 方法）。
        //    现在只提供「按序号取一张」，窗口算多大由 UI 那边决定（布局是 UI 的事）。
        //    出处：`资料/卡组编辑界面_查证_0920.md` §二 + §七。

        /// <summary>筛选后第 <paramref name="i"/> 张（越界给 null）。滚动窗口按它取。</summary>
        public CardDef VisibleAt(int i)
        {
            var all = VisibleCards();
            return (i >= 0 && i < all.Count) ? all[i] : null;
        }

        /// <summary>改筛选条件。</summary>
        public void SetFilter(DeckFilter f) { Filter = f; }

        /// <summary>当前卡组的费用曲线：费用 → 张数（UI 上那排柱子用它）。
        /// 对照原版 `DeckEnergyCostDrawer`（`NUMBER_OF_CARDS_FOR_NORMALIZATION = 10`，柱子按 10 张归一化）。</summary>
        public int[] CostCurve()
        {
            const int maxCost = 20;                  // 卡池里费用没超过 20 的
            var curve = new int[maxCost + 1];
            foreach (var id in Deck.CardIds)
            {
                var c = Find(id);
                if (c == null) continue;
                int k = c.Cost < 0 ? 0 : (c.Cost > maxCost ? maxCost : c.Cost);
                curve[k]++;
            }
            return curve;
        }

        // ============================================================ 传奇总数（原版 `CanAddCard` 的那一档）

        /// <summary>卡组里现存的**传奇张数**（不含督军、防御卡）。
        ///
        /// <para>**判据 = 原版 `CardDeck.LegendaryCardsInDeck`**（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardDeck.cs:1194-1218`；
        /// Ghidra 同族 `decomp_full/CardDeck__LegendaryCardsInDeck.c`）：对 **`cardLibrary`** 做
        /// `Enumerable.Count(…, c ⇒ c != null && c.cardType != Hero && c.Rarity == 4)`。
        /// `cardLibrary` = 卡组里**普通卡**那一条 List（Ghidra `CardDeck__CanAddCard.c:113` 读的是 `deck + 0x58`）；
        /// 督军住在 `+0x40`、防御卡住在 `+0x48`，**两者都不在这条 List 里** ⇒ 词条那句
        /// 「Warlord not included」是**结构保证**的，不是额外判一条。
        /// 我们的等价物 = `PlayerDeck.CardIds`（`TryAdd` 把 hero / defence 分别路由到
        /// `WarlordId` / `DefensiveId`，不进 `CardIds`）。</para>
        ///
        /// <para>⚠️ 这里**仍然显式**跳过 hero / defence：万一某份存档把某一型混进了 `CardIds`，计数也不变
        /// （原版那两格根本不进这条 List）。混进来的那两张另有 `Validate` 的 ⑥（`WarlordInCards`）管。</para>
        /// <para>⚠️ 卡池里查不到的 id（`Find` 给 null）**不计** —— 原版 List 里存的是真卡引用、没有这一档；
        /// 坏 id 由 `Validate` 的 ① 负责报，⛔ 别在这里静默编一张出来。</para></summary>
        public int LegendaryCountInDeck()
        {
            int n = 0;
            if (Deck == null) return 0;
            foreach (var id in Deck.CardIds)
            {
                var d = Find(id);
                if (d == null) continue;
                if (d.Type == "hero" || d.Type == "defence") continue;
                if (DeckRules.IsLegendary(d.Rarity)) n++;
            }
            return n;
        }

        /// <summary>「再加这一张就撞上本模式的**传奇总数**上限」—— 原版 `CardDeck.CanAddCard` 的
        /// `:100-147` 那一段。
        ///
        /// <para>**原版逐句**（`d:/2/tools/decomp_full/CardDeck__CanAddCard.c:100-147`；cpp2il 同族
        /// `CardDeck.cs:821-869`）：
        /// <code>
        /// if (PlayerItem__get_Rarity(card) == 4) {                    // :100-101  rarity == Legendary
        ///   lVar6 = CardDeck__get_CustomGameModeEvent(this);           //         本局 PlayEventData
        ///   gv    = *(longlong*)(lVar6 + 0x80);                        //         = gameplayVariables（PlayEventData+0x80）
        ///   if (0 &lt; *(int*)(gv + 0x10)) {                              // :105    maxLegendaries &gt; 0 才生效
        ///     iVar3 = Enumerable.Count(cardLibrary, 非督军 &amp;&amp; rarity==4);  // :128  已在这副牌里的传奇张数
        ///     if (*(int*)(gv + 0x10) &lt;= iVar3) {                       // :132   limit &lt;= count ⇒ 挡
        ///       error = "MenuDeck/Error/MaxLegendayCopiesReached";      // :136   （原版拼写就是 Legenday）
        ///       String.Format(error, limit);                            // :143  把上限值塞进词条
        ///     }
        ///   }
        /// }</code>
        /// · 偏移判据：`gv + 0x10` = **`GameplayVariablesData.maxLegendaries`** —— 类字段序
        ///   `maxLegendaries` / `numberOfCopiesOtherRarities` / `numberOfCopiesLegendary` / `slotsPerSide` ……
        ///   与 `Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:10-13` 读的 `+0x18` / `+0x14`
        ///   **两处互相印证**，且 `overtimeTurn = +0x28` 对得上 `il2cpp_out/dump.cs:130179`。
        /// · 三个「不是」：**不是**同名上限（那是 `.c:48` 的 `MaxCopiesReached`，上面那一行先判过了）、
        ///   **不是**持有数（`.c:76` 的 `MissingCopies` —— 我们单机全解锁，照 `DeckRules` 头那段跳过）、
        ///   **不是**整副满（`.c:166` 的 `DeckFull`，我们那一档叫 `TooManyCards`，见下面「顺序差异」）。</para>
        ///
        /// <para>🔴 **两个模式的取值 —— 判据 = 官方【游戏内文案】实读**（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json`，
        /// 2026-10-25 现读）：
        /// · `Event_Description/Classic/Help` = **`No Legendary card limit`**（中文「无传奇张牌限制」）
        ///   ⇒ 经典 `maxLegendaries` **不是正数** ⇒ 上面那道 `&gt; 0` 闸直接跳过
        ///   = **经典没有传奇总数上限**（与 `DeckRules.LegendaryTotalLimit(false)` 的 `int.MaxValue` 等价）；
        /// · `Event_Description/Skirmish/Help` = `Up to 4 Legendaries (Warlord not included)` ⇒ 遭遇 = **4**；
        ///   词条 `MenuDeck/Error/MaxLegendayCopiesReached` 的官方两列也写死 4
        ///   （EN `Decks in this game mode can contain only 4 legendary cards.` / ZH「只能包含4张传奇牌」）。</para>
        ///
        /// <para>🟢 **2026-10-25（`E1` · 第五段）：不再「借码」了。** 原版这一支**只出一个字符串**、没有错误枚举；
        /// 我们的 `DeckError` 已经补上 `LegendaryTotalExceeded`（住 `RuleEngine/Core/DeckRules.cs`，
        /// `Describe` 出原版键 `MenuDeck/Error/MaxLegendayCopiesReached`）⇒ `CanAdd` / `Validate` 给同一个码，
        /// 且**返回值本身**就能和「同名上限」区分开（⛔ 别再借 `CopyLimitExceeded`）。</para>
        ///
        /// <para>✅ **顺序差异已消掉（2026-10-26 · B9 · 账 `卡组:8`）**：原版把「整副满」(`DeckFull`) 排在**最后**
        /// （`CardDeck__CanAddCard.c:166` 的 `EmptySlotsCount`），我们原来是**第三** ⇒ 两闸同真时报的码不同。
        /// 现在 `CanAddCore` 的闸序 = 原版：槽位 → 不可入池 → 阵营 → 同名上限(`.c:48`) → 传奇总数(`.c:100-147`)
        /// → **整副满(`.c:166`)** ⇒ 满编 + 同名同时越限先报 `CopyLimitExceeded`（= 原版 `MaxCopiesReached`）。
        /// ⚠️ **更正痕迹（铁律 5）**：本节原文写「没动它的原因 = `Editor/DeckScene.cs:2706` 那条断言钉死了
        /// `TooManyCards`、而该文件不在可写范围」—— 该文件本批已在白名单里，那条夹具已同批改成
        /// 「**普通卡（非传奇）满编 ⇒ `TooManyCards`** + **满编的同名卡 ⇒ `CopyLimitExceeded`**」两闸分判。</para></summary>
        public bool LegendaryTotalReached(CardDef c)
        {
            if (c == null || Deck == null) return false;
            // 督军 / 防御卡各自只占一个位子、不进 `cardLibrary` ⇒ 不吃这一条（原版 `.c:32-39` 早就路由走了）
            if (c.Type == "hero" || c.Type == "defence") return false;
            if (!DeckRules.IsLegendary(c.Rarity)) return false;

            // 唯一出处 = `GameplayVariables.{Classic,Skirmish}.maxLegendaries`（`DeckRules.LegendaryTotalLimit` 转发）
            int limit = DeckRules.LegendaryTotalLimit(Skirmish);
            if (limit <= 0) return false;              // 原版 `.c:105` 那道闸：`<= 0` ⇒ 本模式无上限
            // ⚠️ 经典那一支在我们的表里编码成 `int.MaxValue`（「无上限」）⇒ 走这一支也恒不挡，与上面口径一致。
            return LegendaryCountInDeck() >= limit;    // 原版 `.c:132` `limit <= count` ⇒ 挡
        }

        /// <summary>加不进时**给玩家看的那句话**（原版 `CanAddCard` 的 `out error` 那一路）；合得进去 ⇒ 空串。
        ///
        /// <para>🟢 **2026-10-25（`E1` · 第五~八段）：这里现在只有一句 `DeckRuntime.DeckErrorText(e)`。**
        /// 它内部的选择规则（**只那一处** = `DeckRules.ErrorTextKey`，判据 = 「**这个键是不是原版就有的**」）：
        /// ① 原版有具名键 ⇒ 具名；② 我们自己的槽位判据（`WarlordAlreadySet` / `DefensiveAlreadySet`）⇒ 保留具名；
        /// ③ 其余 10 档（自拟键、官方表里没有）⇒ **映射到原版那 6 个枚举名**（`NoWarlord→MissingHero` ·
        /// `TooFew/TooMany→IncompleteDeck` · 其余→`InvalidDeck`）再走具名族 —— 即原版 `ToRawLocalizationString` 的真实路径。
        /// 两档文案的区分已经下沉到 `DeckError.LegendaryTotalExceeded` + `DeckRules.Describe`
        /// （原版键 `MenuDeck/Error/MaxLegendayCopiesReached`）。
        /// ⚠️ **顺序仍是原版口径**：一张传奇卡**同时**撞同名上限（1 张）与传奇总数时，先命中同名上限
        /// （`CanAddCore` 里那一行排在前，= 原版 `.c:48` 早于 `.c:100`）⇒ 文案给 `MaxCopiesReached`。
        /// 📌 **官方两条文案今天都没有 `{0}` 占位符**（数字写死 4）⇒ 原版那一步 `String.Format` 是空操作；
        /// 我们**没有**照做（删掉了旧的 `LegendaryTotalErrorText`）—— 将来词条若真带占位符，
        /// 在这一句上加一次 `string.Format(…, DeckRules.LegendaryTotalLimit(Skirmish))` 即可。</para>
        /// <para>调用点：`DeckRuntime.BeginCardDragAt`（拖拽那条路的 `Say(...)`）。</para></summary>
        public string AddErrorText(CardDef c)
        {
            var e = CanAddCore(c);
            if (e == DeckError.None) return "";
            // 🟢 **2026-10-25（`E1` · 第七段 · 裁定 (b)）**：这里原来直接 `Loc.T(DeckRules.Describe(e))` ——
            //   对手写表 / 官方表**都没有具名键**的那 9 档会印**键名**（拖拽提示条上玩家可见）。
            //   现在统一走 `DeckRuntime.DeckErrorText`（**判据只那一处** = `DeckRules.ErrorTextKey`：原版有具名键 ⇒ 具名；
            //   我们自己的槽位判据 ⇒ 保留具名；其余 ⇒ 映射到原版那 6 个枚举名再走具名族）。
            return DeckRuntime.DeckErrorText(e);
        }

    }
}
