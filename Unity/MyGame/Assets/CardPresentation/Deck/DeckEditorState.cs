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

        /// <summary>「5 个筛选字段都没设」。⚠️ **不含 `Owned` / `Upgradable`** —— 那两个是独立开关
        /// （原版 `Clear filters` 清的是 `filters[]` 那几件，**不动** `showOnlyOwnedCards`），
        /// 而且 `Owned` 出厂就是开、在单机全解锁下也不改变结果 ⇒ 它不是「有没有筛选」的判据。</summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Faction)
                    && string.IsNullOrEmpty(Rarity) && string.IsNullOrEmpty(Type) && Cost < 0;
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
            //   （键名 = 调度台 2026-10-18 裁定、自拟；中文列 = 改之前这里写死的 `新卡组` ⇒ 中文档零变化）。
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
        /// 连「清掉无效卡」也只动副本（`:46 DeckUtility__RemoveInvalidCards(*plVar1, 0)`）。
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
        }

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
        /// </summary>
        public DeckError CanAdd(CardDef c)
        {
            if (c == null) return DeckError.UnknownCard;

            // 督军和防御卡走各自的位子，不占普通卡位，也各只能有一个。
            // 换督军不走这里 —— 走 SetWarlord（它会把不合阵营的卡一并清掉）。
            if (c.Type == "hero")
                return Deck.WarlordId == null ? DeckError.None : DeckError.WarlordAlreadySet;
            if (c.Type == "defence")
                return Deck.DefensiveId == null ? DeckError.None : DeckError.DefensiveAlreadySet;

            // 🔴 **2026-10-14（`A1798`）改口径**：原来是「用户 2026-09-27 拍板的、按兵种猜的三类
            //    （药剂/破坏/秘仪）」。现在改用**原版真值**：判据只此一处 = `DeckRules.CannotAddToDeck`
            //    （原版 `DeckUtility__ValidateDeckCards.c:68-77`，`inventoryOptions` / `showInCollectionOptions`）。
            //    ⚠️ 那个函数**内部**已经照原版把 hero / 防御卡路由走（`CanAddCard.c:32-39`），
            //    所以这里不必再自己挡一遍（上面那两行早返回的仍在，是**槽位**口径不是准入口径）。
            if (DeckRules.CannotAddToDeck(c)) return DeckError.EffectOnlyCard;

            if (Deck.CardIds.Count >= MaxDeckCount) return DeckError.TooManyCards;

            // 阵营：以督军为准；还没选督军时先按卡自己的阵营（选督军时会清掉不合的）
            if (!string.IsNullOrEmpty(Deck.WarlordId))
            {
                var w = Find(Deck.WarlordId);
                if (w != null && !DeckRules.SameFaction(w.Faction, c.Faction)) return DeckError.WrongFaction;
            }

            if (Deck.CountOf(c.Id) >= DeckRules.CopyLimit(c.Rarity)) return DeckError.CopyLimitExceeded;
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
        /// （`Loc.T` 拿到一个**不是键**的字符串会返回它自己 + 记一次 `MissingCount` + 出一条 warning）。</para></summary>
        public bool TryAdd(CardDef c, out string why)
        {
            var e = CanAdd(c);
            why = Loc.T(DeckRules.Describe(e));
            if (e != DeckError.None) return false;

            if (c.Type == "hero") Deck.WarlordId = c.Id;
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

            foreach (var c in _pool)
            {
                if (showGate && !PassesShowInCollection(c)) continue;       // 原版 `showInCollectionOptions != Hide(0)`
                if (WarlordGatedPool)
                {
                    if (wl == null) { if (c.Type != "hero") continue; }                 // 还没督军 ⇒ 只列督军
                    else if (!DeckRules.SameFaction(c.Faction, wl.Faction)) continue;   // 定了 ⇒ 只列该阵营
                    // 🔴 **2026-10-18**：这一行原来是 `DeckRules.CannotAddToDeck(c)`（准入判据），
                    //   现换成**原版卡池的 `inventoryOptions` 层**（防御卡例外 —— 用户 2026-09-27 口径）。
                    //   ⛔ 别再退回准入判据：那会把「可见但加不进」的卡藏起来（原版是显示+变灰）。
                    if (!PassesEditModeInventory(c)) continue;
                }
                if (needle != null && !NameSearchMatch(c, needle))
                    continue;
                if (!string.IsNullOrEmpty(f.Faction) && !DeckRules.SameFaction(c.Faction, f.Faction)) continue;
                if (!string.IsNullOrEmpty(f.Rarity) && !string.Equals(c.Rarity ?? "", f.Rarity, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(f.Type) && c.Type != f.Type) continue;
                // 🆕 2026-09-28（原版两个开关，卡组编辑与收藏窗同一套）：
                //   · `Owned`（原版**出厂就是开** —— 三路实读见 `DeckFilter.None`）：单机**全解锁**
                //     ⇒ 打开它**筛不掉任何一张** ——
                //     不是我们没做，是「已拥有」在这个前提下恒真。**如实注释，不静默**。
                //   · `Upgradable`：我们**没有升级系统** ⇒ 打开它**必然一张不剩** ——
                //     `项目任务.md` §〇 已拍板「恒空是预期的」（界面那边会出声说明）。
                if (f.Upgradable) continue;
                if (f.Cost >= 0)
                {
                    // `CostMax <= 0` ⇒ 只看 `Cost` 这一个值（卡组编辑那条老路）
                    int hi = f.CostMax > 0 ? f.CostMax : f.Cost;
                    // 🔴🆕 **2026-10-14（账 `N10`）：比的是**夹过**的卡费（`[1,8]`），不是裸 `c.Cost`。**
                    //   判据 = 原版 `d:/2/tools/decomp_full/CardCostFilter__FilterItem.c:15-20`（现读）：
                    //     `iVar2 = *(int *)(card + 0x78);`（= 这张卡的 `manaCost`）
                    //     `if (iVar2 < 2) iVar2 = 1; else if (7 < iVar2) iVar2 = 8;`
                    //   —— 夹完**才**交给那条筛选谓词（`:22` `FUN_1817002f0(filter + 0x38, iVar2, …)`）。
                    //   🔑 **谓词是什么，现读清了**：`CardCostFilter` 的桩是
                    //     `class CardCostFilter : ToggleCollectionFilter&lt;RawCardScript, int&gt;`
                    //     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardCostFilter.cs`）
                    //     ⇒ `+0x38` = 那一族**已选中的 int 档位集**，那句就是
                    //     **「夹过的费用 ∈ 已选档位」**。八档的 `alternativeText` 是
                    //     `1-` / `2`…`7` / `8+`（本工程 `FilterPanelModel.CostBuckets` 实读）
                    //     ⇒ **夹取正是为了把「≤1」折进第 1 档、「≥8」折进第 8 档** ——
                    //     不夹的话那两档的边界卡永远匹配不上。**夹的是【卡】那一侧**，
                    //     档位（`Cost` / `CostMax`）原版不动 ⇒ 我们这边夹的也只有 `c.Cost`。
                    //   📐 **与我们的区间写法等价**：我们 `CostBucketHi` 给的是
                    //     `1-` ⇒ `[1,1]` · `2`…`7` ⇒ `[n,n]` · `8+` ⇒ `[8, int.MaxValue]`
                    //     ⇒ 夹过之后 `[1,1]` ⇔ 原值 ≤ 1、`[8,∞)` ⇔ 原值 ≥ 8（逐档都对得上）。
                    //   📌 **可达性（我们池子实测，`RuleEngine/Resources/cards_engine.json` 1126 张）**：
                    //     · `cost == 0` **111 张**（56 个督军 + 55 张其它：`ASH_Flickerjump` /
                    //       `ASH56 Inspiring Legacy` 这类）⇒ 夹成 1 ⇒ 在 `1-` 档里
                    //       **改前被筛掉（`0 < 1`）、改后列得出来**；
                    //     · `cost >= 8` **83 张**（8…14）⇒ 夹成 8；我们 `8+` 档的上界是
                    //       `int.MaxValue` ⇒ **这一半今天看不出差别**，公式照原版补全（铁律 11）。
                    //   ⚠️ **本笔只改筛选口径、不改卡表**：督军 `cost = 0` 是**真值偏差**
                    //     （真值 `2`，另一笔 `A1773` 去改）—— ⛔ 别在这里顺手改数据。
                    int ccost = ClampPoolCost(c.Cost);
                    if (ccost < f.Cost || ccost > hi) continue;
                }
                outList.Add(c);
            }

            // 🆕 **2026-10-18：显示序**（原版那 4 个稳定 `OrderBy` ⇒ 阵营 → 战将 → 费用 → 名；收藏窗多夹 inventory）。
            //   🔴 本笔**替换**了原来那一步 `StableGroupByFaction`（「阵营按首次出现、组内保持原序」）——
            //   那一步只是「把掉队的战将挪回自己阵营段」的权宜之计（用户 2026-09-27：「总不能混乱地排吧」），
            //   而原版真值是**按 `cardArmy` 枚举值整表排**（10→130），比「首次出现序」更确定，也顺带解决了掉队那位。
            //   判据见 `SortForDisplay` 的 doc。
            SortForDisplay(outList);
            return outList;
        }

        /// <summary>卡名搜索**一格**的匹配。判据 = 原版 `CardNameFilter.CheckInputTextSearch`（实读，
        /// `d:/2/tools/decomp_full/CardNameFilter__CheckInputTextSearch.c`）：
        /// · **子串包含**（`String.Contains`），**不是前缀**；**大小写不敏感**（两边都 `ToLower()`）；
        /// · 🔴 **不只看卡名** —— 原版依次比 ① 本地化卡名 ② 本地化效果文字（先剥富文本标签）
        ///   ③ 阵营名 ④ 兵种名 ⑤ 卡类型名 ⑥ 费用（`cost.ToString()`）；**任一中就算命中**。
        /// ⚠️ 我们是双语工程 ⇒ 中英两套（`Name`/`NameZh`、`Desc`/`DescZh`）**都查**（比原版宽一点点，
        ///    原版一次只查当前语言那一套）—— 如实标。
        /// ⚠️ 原版空串 / 全空白 ⇒ **直接返回全集**（`Filter.c` 的 `IsNullOrWhiteSpace` 分支）。</summary>
        static bool NameSearchMatch(CardDef c, string needle)
        {
            if (Hit(c.Name, needle) || Hit(c.NameZh, needle) || Hit(c.Desc, needle) || Hit(c.DescZh, needle))
                return true;
            // 🔴 2026-09-28（审核抓出来的真缺陷）：原来拿**内部 id** 比（`unit` / `Ultramarines`）
            //    ⇒ **照着抽屉里印的字搜不到**（打 `Troops` / 中文阵营名全落空）。
            //    改成 id 与**显示名**都比（原版比的就是本地化后的阵营名/类型名）。
            if (Hit(c.Faction, needle) || Hit(CardText.Faction(c.Faction), needle)) return true;
            if (Hit(c.Subtype, needle) || Hit(c.Type, needle)) return true;
            // 🔴 **2026-10-18（双语线 · 波 1 · P1）**：类型名那两串**都查** —— ① 抽屉 Type 那一行
            //   **现在印的那一份**（随语档，`FilterPanelModel.TypeLabelAt`）② 原版的英文串
            //   （`TypeLabels`）。
            //   ⚠️ **2026-10-18 之后·第六会话订正（铁律 5）**：这里原写「（`TypeLabels`，**闸门未开时两者相同**）」
            //     —— 那个**取词闸门已删除**（`WCoreDeck` / `A1025` 最终形态：键全在表 ⇒ 一律裸 `Loc.T`，
            //     闸门是静默兜底）⇒ 措辞不成立。**事实仍成立**：`TypeLabels` 现在**只作「原版英文串」用**
            //     （不再参与显示，`FilterPanelModel.RarityNames` / `TypeLabels` 的 doc 已写明）。
            //   少 ① = **照着屏上印的字搜不到** ——
            //   那正是本节上面那条真缺陷（拿内部 id 当显示名比）的同一种病。
            //   ⚠️ 未认识的 `c.Type` 不在这里（`Hit(c.Type, needle)` 上面已经比过）。
            for (int i = 0; i < FilterPanelModel.TypeKeys.Length; i++)
            {
                if (FilterPanelModel.TypeKeys[i] != c.Type) continue;
                if (Hit(FilterPanelModel.TypeLabelAt(i), needle)) return true;
                if (Hit(FilterPanelModel.TypeLabels[i], needle)) return true;
            }
            return Hit(c.Cost.ToString(), needle);
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
        ///   我们单机**全解锁** ⇒ `owned` 恒真 ⇒ 这一条恒过。⚠️ **如实标**：这是**边界**（用户「资源 9999 / 全解锁」），
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

        /// <summary>卡组编辑卡池**额外来的一层**：`inventoryOptions != CantAddToDeck(5)`
        /// （判据 = `DeckEditingWindow___GetCardCollection_b__32_1.c:28-35` 现读：
        /// `if (*(int*)(param_2 + 0x4c) != 5) return 1;`，`inv==5` 的防御卡再看
        /// `assignDefensiveCardsInEditMode`）。
        /// <para>🔴 **防御卡例外**：用户 **2026-09-27** 明确要求「经典模式编辑卡池要列防御卡、玩家从这儿挑」
        /// ⇒ 防御卡**整类放行**。⚠️ 实测池内 `inv==5` 的 **39 张全是 `type == "defence"`**
        /// ⇒ 这一条例外等于把那 39 张全留下，没有别的卡被顺带放进来。</para>
        /// <para>⚠️ 收藏卡牌页**不走这一条**（原版 `CardCollectionTab` 只看 `showInCollectionOptions`）——
        /// 那 39 张防御卡在收藏里照旧显示（现读卡表它们 `show=5`）。</para></summary>
        static bool PassesEditModeInventory(CardDef c)
        {
            if (c == null) return false;
            if (c.Type == "defence") return true;                  // 用户 2026-09-27 口径
            if (c.InventoryOptions < 0) return true;               // 真值缺失 ⇒ 不猜、不隐藏
            return c.InventoryOptions != DeckRules.OptionCantAddToDeck;
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
    }
}
