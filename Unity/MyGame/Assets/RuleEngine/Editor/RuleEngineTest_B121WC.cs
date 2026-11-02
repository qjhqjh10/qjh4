// RuleEngineTest_B121WC.cs — 执行代理 `B121_WC`（第一百二十一/二十二会话 · 2026-11-03）
//   账 `B120-E03b`（督军变身两列的数据管线）· `B120-E03g`（「看得见、进不了卡组」的数据标记 · 引擎侧判据）
//
// ============================ 它盯的是什么 ============================
//   ① **管线接通**（不是「有数据」）：`CardDef.AttachTransformData` / `CardDef.TransformedWarlordId` /
//      `CardDef.DeckCriteria` 这条载体真的把两格带上去；`RuleCore.CampaignHeroTransformTargetId` /
//      `CampaignHeroTransformDeckMeets` 两个取值口**读的是列、不是常量 `null`**
//      （🧨 退回「`return null;`」那一版 ⇒ B/C 组红）。
//   ② **原版跳序**（`BoardAnalysis__DeckMeetsCriteria.c`，101 行逐句）：`deckCriteria` 缺 ⇒ **查不到(null)** ·
//      `transformedWarlordId` 空 ⇒ **false** · `countCriteria` 缺 ⇒ **false** ·
//      `targetsAffected == 0 (any)` ⇒ **true** · 某一格判不了 ⇒ **null（⛔ 不猜 false/true）**。
//   ③ **今天休眠 = 数据事实**（D 组金丝雀）：现读卡表 1,129 张**两列全空、`notDeckable` 全 0**
//      —— 数据落地那天这组会红，届时同批改断言（与 `B120_W30_Assert` 的 F 组同一条纪律）。
//
// ============================ 判据（逐条实读）============================
//  · `d:/2/tools/decomp_full/BattleManager__CheckForHeroDeckTransform.c`（101 行全文）
//  · `d:/2/tools/decomp_full/BoardAnalysis__DeckMeetsCriteria.c`（101 行全文）
//  · 字段 `d:/2/tools/il2cpp_out/dump.cs:23244`（`RawCardScript.transformedWarlordId // 0x200` /
//    `deckCriteria // 0x1F8`）· `:21785`（`DeckCriteria { countCriteria +0x10; triggerIf +0x18; referenceValue +0x1C }`）
//  · 语义 / 等价物 / 为什么休眠 → `RuleEngine/Core/RuleCorePartial_B96CB.cs` 文件末「§变身（督军换将）· 集中注释」
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 B2「取值口读得到 `Attach` 上去的那个 id」 vs D1「真实卡表全 null」：把取值口写回常量 `null` ⇒ B2 红、
//     D1 仍绿；把卡表造假 ⇒ D1 红。两边不可能一起绿。
//  🧨 C2「id 空 ⇒ false」 vs C4「`targetsAffected=0` 且 id 非空 ⇒ true」：把这两个分支写反 ⇒ 必有一条红。
//  🧨 C5「判不了 ⇒ null」在本方法里**是独立第三态** —— 把它写成 `false` ⇒ C5 红（而 C1「缺列 ⇒ null」仍绿，
//     所以不能靠「全部返回 null」蒙过去：C4 会红）。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不建场景、不读存档、不写 PlayerPrefs）。挂法（调度台决定）：
//  宿主聚合行**同一物理行**追加
//      RuleEngineTest_B121WC.Run(); _sink.Pass += RuleEngineTest_B121WC.Pass; _sink.Fail += RuleEngineTest_B121WC.Fail;
//  判绿红：`grep "^B121WC"` 的**合计行**；逐条按行首 `B121WC ✓` / `B121WC ✗` 数（⛔ 别 `grep -c ✗`）。

using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B121_WC`：督军变身两列的**数据管线**（`B120-E03b`）＋ `notDeckable` 的引擎侧判据（`B120-E03g`）。</summary>
public static class RuleEngineTest_B121WC
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B121WC ✓ " + msg); }
        else { _fail++; Debug.LogError("B121WC ✗ " + msg); }
    }

    // ---- 夹具（纯 `CardDef`，不碰任何 Unity 资产）--------------------------------
    static CardDef Hero(string id) { return new CardDef(id, "WC " + id, "hero", "", "Common", "Test", 0, 1, 30, 0); }
    static CardDef Unit(string id) { return new CardDef(id, "WC " + id, "unit", "", "Common", "Test", 1, 1, 1, 0); }

    /// <summary>造一条 `deckCriteria`：`triggerIf` + `referenceValue` + 一份 `countCriteria`。</summary>
    static DeckCriteriaSpec Criteria(int ta, int triggerIf, int referenceValue)
    {
        var s = new DeckCriteriaSpec();
        s.TriggerIf = triggerIf;
        s.ReferenceValue = referenceValue;
        s.CountCriteria = new CardDef.CountSetSpec { TargetsAffected = ta };
        return s;
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        A_Carrier();
        B_TargetId();
        C_DeckMeets();
        D_DormantCanary();
        E_NotDeckable();
    }

    // ==================================================================
    //  A：载体（`CardDef` 那一侧）—— 两列**成对**、`notDeckable` 独立
    // ==================================================================
    static void A_Carrier()
    {
        var hero = Hero("WC1");
        Check(hero.TransformedWarlordId == null && hero.DeckCriteria == null,
              "A1（前提）新造的 `CardDef` 两格都是 null —— 与老调用点（不走 `Attach`）逐字节同行为");

        var spec = Criteria(0, 1, 0);
        hero.AttachTransformData("WC_TARGET", spec);
        Check(hero.TransformedWarlordId == "WC_TARGET", "A2 ★ `AttachTransformData` 把目标 id 带上去了");
        Check(ReferenceEquals(hero.DeckCriteria, spec), "A3 ★ `DeckCriteria` 就是传进去那一份（不复制、不改写）");

        var hero2 = Hero("WC2");
        hero2.AttachTransformData("", spec);
        Check(hero2.TransformedWarlordId == null && hero2.DeckCriteria == null,
              "A4 ★ 空 id ⇒ **两格一起落空**（原版这两列只对「会变身的督军」有意义；⛔ 不许留半个 `deckCriteria`）");

        hero2.AttachTransformData("WC_T2", null);
        Check(hero2.TransformedWarlordId == "WC_T2" && hero2.DeckCriteria == null,
              "A5 有 id、没有 criteria ⇒ 只有 id 一格（`DeckMeets` 随后会回 **null = 查不到**，见 C1）");

        Check(hero.NotDeckable == false, "A6（前提）`NotDeckable` 默认 false");
        hero.AttachNotDeckable(true);
        Check(hero.NotDeckable, "A7 ★ `AttachNotDeckable(true)` 置位（`B120-E03g` 的数据标记）");
    }

    // ==================================================================
    //  B：取值口 `CampaignHeroTransformTargetId`
    // ==================================================================
    static void B_TargetId()
    {
        var plain = Hero("WC3");
        Check(RuleCore.CampaignHeroTransformTargetId(plain) == null,
              "B1 没挂数据 ⇒ `null`（= 原版 `transformedWarlordId` 那一格为空）");

        var t = Hero("WC4");
        t.AttachTransformData("WC_TARGET", Criteria(0, 1, 0));
        Check(RuleCore.CampaignHeroTransformTargetId(t) == "WC_TARGET",
              "B2 ★★ 挂了数据 ⇒ 取值口读得到 —— 🧨 退回「`return null;`」那一版必红");
        Check(RuleCore.CampaignHeroTransformTargetId((CardDef)null) == null, "B3 `null` 卡 ⇒ `null`（不抛）");
    }

    // ==================================================================
    //  C：判据 `CampaignHeroTransformDeckMeets` —— 原版五跳 + 三态
    // ==================================================================
    static void C_DeckMeets()
    {
        var deck = new List<CardDef> { Unit("WC_D1"), Unit("WC_D2") };

        var noData = Hero("WC5");
        Check(RuleCore.CampaignHeroTransformDeckMeets(deck, noData) == null,
              "C1 ★ `deckCriteria` 缺 ⇒ **null = 查不到**（⛔ 不是 false：这一格是「没数据」，不是「原版说不满足」）");

        var noId = Hero("WC6");
        noId.AttachTransformData("", Criteria(0, 1, 0));   // 空 id ⇒ 载体把两格都丢了 ⇒ 仍是「没数据」
        Check(RuleCore.CampaignHeroTransformDeckMeets(deck, noId) == null,
              "C2 空 id ⇒ 载体落空 ⇒ 仍回 null（与 C1 同一档；真正的「id 空 ⇒ false」在 C3）");

        // C3：**有 criteria、id 非空**才进到原版 `DeckMeetsCriteria` 的 `.c:29-33` 那一格。
        //     用一份「本版判不了」的 criteria（`CountSetSpec` 只填 `TargetsAffected`）走到 `MatchesRawCard`：
        //     ⚠️ 它会在 `type` / `spellType` / stat 那几格返回 null ⇒ 我们要的正是这个「判不了 ⇒ null」。
        var specUnknown = new DeckCriteriaSpec { TriggerIf = 1, ReferenceValue = 0, CountCriteria = new CardDef.CountSetSpec { TargetsAffected = 10 } };
        var warlordUnknown = Hero("WC7");
        warlordUnknown.AttachTransformData("WC_T", specUnknown);
        string gap;
        var probe = TargetCriteriaEval.MatchesRawCard(deck[0], specUnknown.CountCriteria, out gap);
        Check(probe == null,
              "C3（前提）夹具那份 criteria 在本版**判不了**（`MatchesRawCard` 回 null；gap = " + gap + "）—— 下一句才有意义");

        Check(RuleCore.CampaignHeroTransformDeckMeets(deck, warlordUnknown) == null,
              "C4 ★★ 只要有一张判不了 ⇒ 整条结论 **null（查不到）** —— 🧨 写成 `false` 必红（那会把「没数据」当成「不满足」）");

        var warlordAny = Hero("WC8");
        warlordAny.AttachTransformData("WC_T", Criteria(0, 2, 3));
        Check(RuleCore.CampaignHeroTransformDeckMeets(deck, warlordAny) == true,
              "C5 ★★ `countCriteria.targetsAffected == 0 (any)` + id 非空 ⇒ **true（无条件即满足）**"
            + " —— 原版 `BoardAnalysis__DeckMeetsCriteria.c:44-46` 直接 `return 1`；🧨 把这一支写成 false 必红");

        var warlordNoCC = Hero("WC9");
        var specNoCC = new DeckCriteriaSpec { TriggerIf = 1, ReferenceValue = 0, CountCriteria = null };
        warlordNoCC.AttachTransformData("WC_T", specNoCC);
        Check(RuleCore.CampaignHeroTransformDeckMeets(deck, warlordNoCC) == false,
              "C6 ★ `countCriteria == null` ⇒ **false**（`.c:37-39` 的 `return 0`，⛔ 不是「无条件满足」）");

        var warlordUnknownTa = Hero("WC10");
        var specTaUnknown = new DeckCriteriaSpec { TriggerIf = 1, ReferenceValue = 0, CountCriteria = new CardDef.CountSetSpec() };
        warlordUnknownTa.AttachTransformData("WC_T", specTaUnknown);
        Check(RuleCore.CampaignHeroTransformDeckMeets(deck, warlordUnknownTa) == null,
              "C7 `targetsAffected` 未知（没落）⇒ **null**（判不了 ⇒ ⛔ 不猜）");

        Check(RuleCore.CampaignHeroTransformDeckMeets(deck, null) == null, "C8 督军为空 ⇒ null（不抛、不猜）");
        Check(RuleCore.CampaignHeroTransformDeckMeets(null, warlordAny) == true,
              "C9 牌表为空但 `any` 无条件 ⇒ **仍是 true**（原版 `.c:44-46` 在碰牌表**之前**就 `return 1`）");
        var warlordFiltered = Hero("WC11");
        warlordFiltered.AttachTransformData("WC_T", Criteria(10, 2, 3));
        Check(RuleCore.CampaignHeroTransformDeckMeets(null, warlordFiltered) == false,
              "C10 牌表为空 + 带真条件 ⇒ **false**（无牌可筛 ⇒ 张数 0 达不到 `referenceValue = 3`；不抛）");
    }

    // ==================================================================
    //  D：**今天休眠 = 数据事实**（金丝雀：数据落地那天这组会红）
    // ==================================================================
    static void D_DormantCanary()
    {
        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0, "D0（前提）卡表加载得到（" + (pool != null ? pool.Count : 0) + " 张）");
        if (pool == null) return;
        int withId = 0, withCriteria = 0, notDeckable = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (!string.IsNullOrEmpty(c.TransformedWarlordId)) withId++;
            if (c.DeckCriteria != null) withCriteria++;
            if (c.NotDeckable) notDeckable++;
        }
        Check(withId == 0 && withCriteria == 0,
              "D1 ★★ 现读卡表 **两列全空**（" + pool.Count + " 张：`TransformedWarlordId` 非空 " + withId
            + " · `DeckCriteria` 非空 " + withCriteria + "）⇒ 变身这条链**休眠**（数据事实，⛔ 不是缺口）"
            + " ｜🧨 数据落地那天这里会红 ⇒ 同批把断言改成真值");
        Check(notDeckable == 0,
              "D2 ★ `notDeckable` 现读 **0 张**（= `B120-E03f` 还没造那 13 张卡；⛔ 别把它当「标记没接上」）");
    }

    // ==================================================================
    //  E：`DeckRules.IsNotDeckable` —— 「看得见、进不了卡组」的**唯一判据口**
    // ==================================================================
    static void E_NotDeckable()
    {
        Check(RuleRules_IsNotDeckable(null) == false, "E1 `null` ⇒ false（与 `CannotAddToDeck` 同一条 null 口径）");
        var u = Unit("WC_N1");
        Check(RuleRules_IsNotDeckable(u) == false, "E2 普通卡 ⇒ false（= 全池今天那一档）");
        u.AttachNotDeckable(true);
        Check(RuleRules_IsNotDeckable(u), "E3 ★★ 标了 `notDeckable` ⇒ true（`CanAdd` / `Validate` 据此回 `InvalidCard`）");
    }

    /// <summary>薄封装（只为把断言文案与实现分开写；判据本体仍只此一处 = `DeckRules.IsNotDeckable`）。</summary>
    static bool RuleRules_IsNotDeckable(CardDef c) { return DeckRules.IsNotDeckable(c); }
}
