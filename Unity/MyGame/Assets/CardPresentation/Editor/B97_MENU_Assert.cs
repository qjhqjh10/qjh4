// B97_MENU_Assert.cs — 执行代理 `B97_MENU`（第九十七会话 · 2026-10-29）
//
// ============================ 它盯的是什么 ============================
// 本批 11 条（`菜单_外壳.tsv` 的 B4 + C7）里**我自己动过代码的 8 条**：
//   · **7 条** `DynamicList` 家族（`:142` `Add` · `:143` `Clear` · `:149` `GetList` · `:150` `IndexOf` ·
//     `:151` `Inser` · `:154` `Remove` · `:155` `RemoveAt`）—— 落点 = **新建** `Shell/DynamicList.cs`，
//     并把 `LiveOpsEventFilter.Triggers` / `.Conditions` 的**静态类型**改过去（原版 `+0x30` / `+0x38` 就是它）。
//   · **1 条** `菜单/外壳:58` `CardDrawer.ToggleBanned` 的**③ ban 徽标**那一半 —— 落点 =
//     `Deck/DeckRuntime.cs` 卡池卡格（原版 `Collection Card/CardUI/Ban Icon` + 子件 `Banned Text`）。
//   其余 3 条（`:427` / `:434` / `:438`）的落点 `Shell/MainMenuRuntime.cs` **当时正被别的代理写** ⇒
//   本文件**不覆盖**它们（改了会撞车），补丁见 `报告/B97_MENU.md` §三。
//
// 判据（逐条现读）：
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DynamicList.cs`（647 行，**有体**）
//     `get_Item:171` · `Count:183` · `GetList:198` · `ListType:203` · `GetIndexType:211` ·
//     `Get<T1>(T):228` · `Get<T1>(int):269` · `GetAll<T1>:321` · `Add:555` · `Inser:577` ·
//     `IndexOf:582` · `Clear:593` · `Remove:605` · `RemoveAt:610` · 基类 `:642-647`
//   · `d:/2/tools/il2cpp_out/dump.cs:115653-115848`（成员 / 泛型约束 / 字段偏移）
//   · ban 徽标：`bundle_menus_assets_all` 的 `Collection Card.json` 350×512 ·
//     `CardUI`（`m_LocalScale 150`）· `Ban Icon`（中心锚、`m_SizeDelta 1.75×1.75`、`m_PreserveAspect 1`、
//     sprite = `40k_Cross_icon_cross_big Banned card`）· 子件 `Banned Text`（anchor `0.1..0.9 / 0.4..0.6`、
//     TMP `m_fontSizeBase 36` / autosize `0.25…72` / Center+Middle / Bold+UpperCase、本地化键 `MenuDeck/Banned`）
//
// ============================ 🔴 灭自证（什么改动会让哪一条红）============================
//   · `DynamicList` 退回裸 `List<T>`（或 `Triggers`/`Conditions` 的类型被改回去）⇒ DL-1/2 红。
//   · `Inser` 的形参次序被「顺手改对」成 `(index, element)` ⇒ DL-5 红（原版**就是** `(element, index)`）。
//   · `GetAll<T1>` 从 `Where(is) → Select(as)` 改成「按 `GetType() ==` 精确比」⇒ DL-8 红（子类会被剔除）。
//   · `Clear()` 改用「换一个新表」⇒ DL-9 红（原版是**原地清**，`GetList()` 拿到的引用不许失效）。
//   · `SetBannedCards` 不重建缓存（只写 `bannedCardIds` 不 `OnAfterRead`）⇒ BB-2 红。
//   · ban 徽标的图改名/挪目录 ⇒ BB-4 红；`MenuDeck/Banned` 词条丢了 ⇒ BB-5 红。
//   · 卡池装配不接 `SetPoolBanIcon`（或禁了却不显示）⇒ PB-3 / PB-4 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   · **纯逻辑那两段（`CheckB97MenuDynamicList` / `CheckB97MenuBanBadge`）不依赖任何场景** ⇒ 插在哪都行；
//     建议插在 `Editor/ShellScene.cs` 的 `Run()` 里任一 `Section(...)` 之后（它最早跑、失败最快暴露）。
//   · **卡池那一段（`CheckB97MenuDeckPoolBanIcon`）需要 `DeckRuntime`** ⇒ 宿主 = `Editor/DeckScene.cs`：
//     `B97_MENU_Assert.CheckB97MenuDeckPoolBanIcon(drv);`（`drv` = 那个宿主里已经建好的 `DeckRuntime`），
//     建议插在**卡池装配断言那一段之后**（那时候池里已经有卡）。⚠️ 它会 `RefreshAll()` 两次再还原，
//     插在「卡池几何 / 张数条」那批断言**之前**会把它们的读数冲掉 ⇒ 插在后面。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_MENU`：`DynamicList` 家族（7 条）+ 卡格 ban 徽标（`菜单/外壳:58` ③）。
/// <para>⛔ 期望值一律写死字面量 / 独立算出来，**不从被测实现反算**。</para></summary>
public static class B97_MENU_Assert
{
    static readonly CheckSink _sink = new CheckSink("[MENU] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    /// <summary>不需要场景的那两段一起跑（`ShellScene` 那类宿主直接调它）。</summary>
    public static void RunAll()
    {
        CheckB97MenuDynamicList();
        CheckB97MenuBanBadge();
    }

    // ==================================================================
    //  ① `DynamicList`（账 菜单/外壳:142/143/149/150/151/154/155 —— 七个成员）
    // ==================================================================

    /// <summary>`DynamicList&lt;T&gt;` 的七个账上成员 + 三个非账成员（`Count` / 索引器 / `ListType`）一起钉。
    /// <para>选 `T = object` 是因为 `Get&lt;T1&gt;` / `GetAll&lt;T1&gt;` 的约束是 `where T1 : class, T`——
    /// 只有 `object` 能让「任意引用类型」都满足它（原版的真实用法就是这样：`DynamicList&lt;object&gt;`，
    /// 见 `.ctor` 的 `DynamicList&lt;object&gt;___ctor` 调用点 `EffectListing__.ctor.c:13`）。</para></summary>
    public static void CheckB97MenuDynamicList()
    {
        S("🗂 B97_MENU：`DynamicList<T>`（账 菜单/外壳:142/143/149/150/151/154/155）");

        // ---- ① 空表的初值（原版 `.ctor:615` 里 `list = new List<T>()`）----
        var l = new DynamicList<object>();
        C(l.Count, 0, "DL-0 新建 ⇒ `Count == 0`（原版 ctor 就 `new List<T>()`，永不为 null）");
        T(l.GetList() != null, "DL-0b `GetList()` 非 null（`.cs:198` = `return list;`）");
        C(l.ListType(), typeof(object), "DL-0c `ListType()` = `typeof(T)`（`.cs:203`）");

        // ---- ② `Add`（`.cs:555`）----
        var s1 = "one"; var s2 = "two"; var n1 = 42;
        l.Add(s1); l.Add(s2); l.Add(n1);
        C(l.Count, 3, "DL-1 `Add(T)` × 3 ⇒ `Count == 3`（`.cs:555` = `list.Add`）");
        C(l[0], (object)s1, "DL-1b 索引器 `[0]` 拿回第一个元素（`.cs:171` = `list[index]`）");
        C(l[2], (object)n1, "DL-1c 索引器 `[2]` = 最后加进去的那个（顺序是**追加**）");

        // ---- ③ `GetList`（`.cs:198`：返回**那一张表本身**，不是副本）----
        var raw = l.GetList();
        C(raw.Count, 3, "DL-2 `GetList().Count` = 3");
        T(ReferenceEquals(raw, l.GetList()), "DL-2b `GetList()` 两次拿到**同一个对象**（`.cs:198` = `return list`，不是 `ToList()`）");

        // ---- ④ `IndexOf`（`.cs:582`）----
        C(l.IndexOf(s2), 1, "DL-3 `IndexOf(第二个)` = 1");
        C(l.IndexOf("不在表里"), -1, "DL-3b `IndexOf(不在表里)` = −1（`List.IndexOf` 的语义）");

        // ---- ⑤ `Inser`（`.cs:577` —— 🔴 原版形参次序是 **(element, index)**，方法名**就是拼错的 `Inser`**）----
        var s0 = "zero";
        l.Inser(s0, 0);
        C(l.Count, 4, "DL-4 `Inser(e, 0)` ⇒ 长度 3→4");
        C(l[0], (object)s0, "DL-4b 插在**下标 0** ⇒ 第 0 项变成它（若形参次序被写成 `(index, element)` 这条会红）");
        C(l.IndexOf(s1), 1, "DL-4c 原来的第一项被顶到下标 1（`List.Insert` 的位移）");
        l.Inser("tail", l.Count);          // `index == Count` ⇒ 追加（`List.Insert` 允许）
        C(l[l.Count - 1], (object)"tail", "DL-4d `Inser(e, Count)` ⇒ 追加到末尾（`List.Insert` 的边界口径）");

        // ---- ⑥ `Remove` / `RemoveAt`（`.cs:605` / `.cs:610`）----
        l.Remove("tail");
        C(l.Count, 4, "DL-5 `Remove(存在项)` ⇒ 长度 5→4");
        l.Remove("根本不在");
        C(l.Count, 4, "DL-5b `Remove(不存在项)` ⇒ 长度不变（`List.Remove` 返回 false，原版丢掉那个 bool）");
        l.RemoveAt(0);
        C(l.Count, 3, "DL-6 `RemoveAt(0)` ⇒ 长度 4→3");
        C(l[0], (object)s1, "DL-6b 删掉下标 0 之后，`[0]` 变成原来的第 1 项");

        // ---- ⑦ `Clear`（`.cs:593`：**原地清**，引用不失效）----
        var beforeClear = l.GetList();
        l.Clear();
        C(l.Count, 0, "DL-7 `Clear()` ⇒ `Count == 0`");
        T(ReferenceEquals(beforeClear, l.GetList()), "DL-7b `Clear()` 之后 `GetList()` 仍是**同一个对象**（原地清，不是换新表）");

        // ---- ⑧ `Get<T1>` / `GetAll<T1>` / `GetIndexType`（`.cs:228 / 269 / 321 / 211`）----
        l.Clear();
        l.Add("a"); l.Add(7); l.Add("b");
        C(l.Get<string>(l[0]), "a", "DL-8 `Get<string>(第 0 项)` 得到它本身（`.cs:228` = `element as T1`）");
        C(l.Get<string>(l[1]), (string)null, "DL-8b 第 1 项是 int ⇒ `Get<string>` 得 **null**（不是抛）");
        C(l.Get<string>(2), "b", "DL-8c `Get<string>(2)` 按下标取（`.cs:269` = `list[index] as T1`）");
        var allStr = l.GetAll<string>();
        C(allStr.Count, 2, "DL-8d `GetAll<string>()` 只挑出 2 个 string（`.cs:321` = `Where(is T1)`）");
        C(allStr[0], "a", "DL-8e `GetAll` **保持原序**（第 0 个是 `a`）");
        C(allStr[1], "b", "DL-8f …第 1 个是 `b`");
        C(l.GetIndexType(1), typeof(int), "DL-8g `GetIndexType(1)` = 元素的**运行时**类型 `int`（`.cs:211`）");

        // ---- ⑨ 基类多态（`DynamicList` 那两条抽象：`.cs:642-647`）----
        DynamicList asBase = l;
        C(asBase.ListType(), typeof(object), "DL-9 按**基类**引用调 `ListType()` 仍得 `typeof(T)`（虚分派）");

        // ---- ⑩ 真消费者：`LiveOpsEventFilter` 那两格（原版 `+0x30` / `+0x38` 就是 `DynamicList<T>`）----
        var f = new LiveOpsEventFilter();
        T(f.Triggers != null, "DL-10 `EventFilter.Triggers` 非 null（原版 ctor 里就 `new`，`EventFilter__.ctor.c` 的 `+0x30`）");
        T(f.Conditions != null, "DL-10b `EventFilter.Conditions` 非 null（同上，`+0x38`）");
        C(f.Triggers.GetType(), typeof(DynamicList<LiveOpsEventTrigger>),
          "DL-10c `Triggers` 的**静态类型**已是 `DynamicList<LiveOpsEventTrigger>`（退回裸 `List<T>` 这条会红）");
        C(f.Conditions.GetType(), typeof(DynamicList<EventCondition>),
          "DL-10d `Conditions` 的**静态类型**已是 `DynamicList<EventCondition>`");
        C(f.Triggers.Count, 0, "DL-10e 新过滤器 ⇒ `Triggers.Count == 0`（`HasTriggers` 的判据就是这一格）");
    }

    // ==================================================================
    //  ② 禁卡表的数据面（账 菜单/外壳:58 ①）
    // ==================================================================

    /// <summary>= 原版 `PlayEventData.OnAfterRead` / `IsCardBanned` 的语义（`Shell/PlayEvent.cs` 那一族）。
    /// <para>🔴 **反直觉但照抄**：缓存为 `null` ⇒ 恒 `false` ——「没配禁卡表」与「禁卡表是空的」**行为相同**。</para></summary>
    public static void CheckB97MenuBanBadge()
    {
        S("🚫 B97_MENU：禁卡表（账 菜单/外壳:58 ①）· ban 徽标的素材与文案（③）");

        var evt = new B97MenuFakePlayEventData();
        T(!evt.IsCardBanned("A"), "BB-1 没灌表 ⇒ `IsCardBanned` 假（原版「缓存 null ⇒ 恒 false」）");

        evt.SetBannedCards(new[] { "A", "B" });
        T(evt.IsCardBanned("A"), "BB-2 `SetBannedCards([A,B])` ⇒ 立刻生效（等价「读到带 bannedCards 的事件后跑 `OnAfterRead`」）");
        T(evt.IsCardBanned("B"), "BB-2b 表里第二个也在");
        T(!evt.IsCardBanned("C"), "BB-2c 表外的卡不受影响");

        evt.SetBannedCards(null);
        T(!evt.IsCardBanned("A"), "BB-3 `SetBannedCards(null)` ⇒ 清表、回到恒假（原版 `bannedCardIds == null` ⇒ 缓存留 null）");

        // ③ 素材与文案：徽标那两张「东西」必须在本地拿得到，否则建出来是空白（红线：不许静默失败）
        T(CardArt.MenuUi("40k_Cross_icon_cross_big_Banned_card") != null,
          "BB-4 ban 图标 sprite 在本地（`Resources/Art/ui_menu/40k_Cross_icon_cross_big_Banned_card.png`）");
        string banned = Loc.T("MenuDeck/Banned");
        T(!string.IsNullOrEmpty(banned) && banned != "MenuDeck/Banned",
          "BB-5 本地化键 `MenuDeck/Banned` 能取到词条（官方表 = `Banned` / `禁止`；getter 回键名 = 缺词）");
    }

    /// <summary>假事件数据：`PlayEventData` 没有抽象成员 ⇒ 空子类即可（同 `Editor/GameModesCheck.cs` 的 `FakePlayEventData`）。
    /// ⚠️ 本类只为本文件服务，⛔ 别拿去当生产类型。</summary>
    sealed class B97MenuFakePlayEventData : PlayEventData { }

    // ==================================================================
    //  ④ 主菜单活动容器（账 菜单/外壳:427 / :434 / :438）—— 宿主 = MainMenuScene
    // ==================================================================

    /// <summary>在**已经建好主菜单**的实例上验容器表的两条不变量。
    /// <para>⚠️ **覆盖边界（如实记）**：本机 13 份活动 SO 的日期全在 2025 年 ⇒ `ModeCardsFromLiveOpEvents`
    /// 今天恒返回 `null` ⇒ 走手挑 5 张兜底 ⇒ **增量回收复用那一支今天跑不到**
    /// （同 `判据/B96_菜单未分类44_分诊.md` §二末尾那条「休眠」注）。所以这里钉的是**它今天必须不误伤**：
    /// 容器表必须为空（手挑卡不属容器族），且扔进一次真实的增量刷新后仍然为空、滚动确实动了。</para>
    /// <para>⛔ 要验「复用同一颗、数据变了才重建」，得先有在期活动数据 —— 本工程没有那个数据源
    /// （服务端），所以那几条**没有断言**，如实记在 `报告/B97_MENU.md` §四。</para></summary>
    public static void CheckB97MenuLiveOpContainers(MainMenuRuntime menu)
    {
        S("📦 B97_MENU：主菜单活动容器表（账 菜单/外壳:427 / :434 / :438）");
        T(menu != null, "MC-0 前提：拿到 `MainMenuRuntime`（否则本条等于没查）");
        if (menu == null) return;
        T(menu.ModeScroll != null, "MC-0b 前提：模式卡滚动区已建（`BuildGameModes` 跑过）");
        if (menu.ModeScroll == null) return;

        C(menu.LiveOpContainerCountForTest, 0,
          "MC-1 无在期活动 ⇒ 容器表为空（手挑那 5 张**不进容器表**：原版主菜单根本没有静态卡）");

        float off0 = menu.ModeScroll.Offset;
        menu.ModeScroll.ScrollBy(120f);                       // 触发 `OnChanged` ⇒ `RebuildModeCards(false)` = **增量支**
        T(menu.ModeScroll.Offset > off0, "MC-2 前提：滚动确实动了（`OnChanged` ⇒ 增量刷新真的跑了一趟）");
        C(menu.LiveOpContainerCountForTest, 0, "MC-3 增量刷新一趟之后容器表仍为空（没把非容器卡误登记进去）");

        menu.ModeScroll.ScrollBy(-120f);                      // 滚回去（不给后面的断言留状态）
        C(menu.LiveOpContainerCountForTest, 0, "MC-4 再滚回来仍为空");
        C(menu.LiveOpContainerNameForTest(0), (string)null, "MC-5 空表时 `NameForTest(0)` = null（读口本身不抛）");
    }

    // ==================================================================
    //  ③ 卡池卡格的 ban 徽标（账 菜单/外壳:58 ③）—— 需要 `DeckRuntime`（宿主 = DeckScene）
    // ==================================================================

    /// <summary>在**已建好卡池**的 `DeckRuntime` 上验：无禁卡表 ⇒ 徽标隐藏；禁掉第 0 格那张卡 ⇒ 徽标出现且字有内容。
    /// <para>⚠️ 前提断言写在前面（⛔ 不用 `if (前提) Check(结论)` 那种静默跳过的写法）：拿不到 `drv` 或池里没卡时，
    /// 先红一条「前提」再返回 —— 让「本条等于没查」这件事在日志里**看得见**。</para></summary>
    public static void CheckB97MenuDeckPoolBanIcon(DeckRuntime drv)
    {
        S("🚫 B97_MENU：卡池卡格的 ban 徽标（账 菜单/外壳:58 ③）");
        T(drv != null, "PB-0 前提：拿到 `DeckRuntime`（否则本条等于没查）");
        if (drv == null) return;

        var prev = DeckRuntime.CurrentEventData;
        try
        {
            DeckRuntime.CurrentEventData = null;
            drv.RefreshAll();

            string id0 = drv.PoolCellCardId(0);
            T(!string.IsNullOrEmpty(id0), "PB-1 前提：卡池第 0 格装着一张卡（空池时本条等于没查）");
            if (string.IsNullOrEmpty(id0)) return;
            T(drv.PoolBanIconBuilt(0), "PB-2 第 0 格建出了 ban 图标对象（装配路径跑了 `SetPoolBanIcon`）");
            T(!drv.PoolBanIconVisible(0), "PB-3 没有禁卡表 ⇒ 徽标**隐藏**（原版 `ToggleBanned(false)`）");

            var evt = new B97MenuFakePlayEventData();
            evt.SetBannedCards(new[] { id0 });
            DeckRuntime.CurrentEventData = evt;
            drv.RefreshAll();
            T(drv.PoolBanIconVisible(0), "PB-4 禁掉第 0 格那张卡 ⇒ 徽标**显示**（原版 `ToggleBanned(true)`）");
            T(!string.IsNullOrEmpty(drv.PoolBanText(0)), "PB-5 …那行字有内容（`MenuDeck/Banned`；空串 = 缺词/没建 Label）");

            // 灭自证：禁的是**别的卡** ⇒ 第 0 格必须还是不显示
            var other = new B97MenuFakePlayEventData();
            other.SetBannedCards(new[] { id0 + "__not_a_real_id__" });
            DeckRuntime.CurrentEventData = other;
            drv.RefreshAll();
            T(!drv.PoolBanIconVisible(0), "PB-6 灭自证：禁的是**另一张**卡 ⇒ 第 0 格仍隐藏（不是「有禁卡表就全显示」）");
        }
        finally
        {
            DeckRuntime.CurrentEventData = prev;
            drv.RefreshAll();
            Debug.Log(_sink.P + "MENU 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
        }
    }
}
