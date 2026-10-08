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
        ///    只有 `Cost` 一个数表达不了 `8+`。卡组编辑那边只填 `Cost`（⇒ 退化成精确匹配，行为不变）。
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
            Deck.Name = CardText.Name(w.Name, w.NameZh);
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

            // 🆕 2026-09-27（用户拍板）：**效果生成的卡**（药剂/破坏/秘仪）不能放进卡组。
            //    判据只此一处 = `DeckRules.IsEffectOnly`（`Validate` 那边也调它）。
            if (DeckRules.IsEffectOnly(c.Subtype)) return DeckError.EffectOnlyCard;

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

        /// <summary>按当前筛选条件过一遍卡池。顺序保持卡池原序 —— **不排序**，
        /// 原版有没有排序规则没查到（见方案文档；`DeckCollectionDisplay.SaveDeckOrder` 只管卡组列表的拖拽序）。</summary>
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

            foreach (var c in _pool)
            {
                if (WarlordGatedPool)
                {
                    if (wl == null) { if (c.Type != "hero") continue; }                 // 还没督军 ⇒ 只列督军
                    else if (!DeckRules.SameFaction(c.Faction, wl.Faction)) continue;   // 定了 ⇒ 只列该阵营
                    // 🆕 2026-09-27（用户拍板）：**效果生成的卡**（药剂/破坏/秘仪）**不进卡池** ——
                    //    它们是「打牌时被效果生成出来」的，不是拿来构筑的。判据 = `DeckRules.IsEffectOnly`。
                    if (DeckRules.IsEffectOnly(c.Subtype)) continue;
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
                    if (c.Cost < f.Cost || c.Cost > hi) continue;
                }
                outList.Add(c);
            }

            // 🆕 **2026-09-28：按阵营【分组】**（用户 2026-09-27：「总不能全部阵营的督军不按照一定的顺序全部混乱地排列吧」）。
            //   🔴 **卡池原序确实会交错** —— 实测 56 位督军里有一个掉队的：
            //     `EmperorsChildren/Lord Kaphrael` 排在第 **56** 位，而它同阵营的两位在第 17/18 位（隔了 37 位）。
            //   做法 = **稳定分组**：**阵营的先后顺序取「首次出现」的顺序**（= 卡池自己的阵营次序，
            //     不另发明一套排序），**组内保持卡池原序**。⇒ 只把掉队那位挪回它自己的阵营段，其余一个不动。
            //   ⚠️ 只在「还没督军」时做（那是唯一会列出跨阵营督军的状态）；定了督军之后只剩一个阵营，无所谓。
            if (WarlordGatedPool && wl == null) outList = StableGroupByFaction(outList);
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

        /// <summary>按阵营**稳定分组**：阵营次序 = 首次出现的次序，组内保持原序。见 `VisibleCards` 里那段说明。</summary>
        static List<CardDef> StableGroupByFaction(List<CardDef> src)
        {
            var order = new List<string>();
            var seen = new HashSet<string>();
            foreach (var c in src) { var f = c.Faction ?? ""; if (seen.Add(f)) order.Add(f); }
            var res = new List<CardDef>(src.Count);
            foreach (var f in order)
                foreach (var c in src) if ((c.Faction ?? "") == f) res.Add(c);
            return res;
        }

        /// <summary>卡池里出现过的阵营（给筛选下拉用），按出现顺序。</summary>
        public List<string> Factions()
        {
            var seen = new List<string>();
            foreach (var c in _pool)
                if (!string.IsNullOrEmpty(c.Faction) && !seen.Contains(c.Faction)) seen.Add(c.Faction);
            seen.Sort(StringComparer.Ordinal);
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
