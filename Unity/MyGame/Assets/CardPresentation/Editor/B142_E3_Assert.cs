// B142_E3_Assert.cs — 批次 **B142** · 执行写手 `E3`（4 条：CEMROW / TITLEFACE / L986 派生 trait / CHK-05 L1393 目标表）
//
// ============================ 它盯的是什么 ============================
//   A 组（`B141-CEMROW-DOC` · 账 `HUD:696`）：`duringBattleFlag(+0x25c)` 的等价物
//       —— 判据 = `BattleManager._StartBattlePhase_d__334__MoveNext.c:161-165`（与 `UIstate = 1` **同一句**置真）
//       ⇒ **换牌期为假**。改前 `CemeteryRowClickAllowed` 不看 `MulliganOpen`（比原版多放行一档）。
//
//   B 组（`B141-TITLEFACE`）：称号那两颗标签的**字体面 = Asar**（口径一致性；Midline 档今天数值零差）。
//
//   C 组（`B141-L986-DERIVED-TRAIT` · 账 `战斗逻辑驱动:986`）：原版
//       `CardTraitCollection.GetTraitInPlayListForIcons.c:112-192` 的**第二支** —— 由 ability trigger
//       派生的 trait **也进场上 7 格**；⛔ 但不许写进引擎关键词表。
//
//   D 组（`B141-CHK05-L1393`）：`play` / `trigger` 两桶的**多目标逐目标播放**
//       （判据 `AbilityLogic__PlayAnim.c:171-176`：三个位置选项任一 == `abilityTarget(2)` ⇒ 按 `abilityTargets` 逐目标各播一份）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2：把 `CemeteryDuringBattle` 里的 `&& !mulliganOpen` 删掉 ⇒ A2 红（证明「换牌期为假」这一维真的在生效）。
//   · A3：把 `!isOver` 删掉 ⇒ A3 红（证明不是只看换牌期）。
//   · B1/B2：把 `BattleDriver.TitleFace` 改成 `Pragati` ⇒ B1/B2 红。
//   · C3/C4：把 `MergeDerivedTraits` 的去重从「按图名」改成「按关键词名」⇒ C3/C4 红
//       （那 14 张「已经画出来」的卡会被重复加一枚同样的图）。
//   · C5：同上 ⇒ 池级读数从 4 变 18 ⇒ C5 红。
//   · C6：把 `For` 里 `zeroCounterKeys` 那一档删掉 ⇒ C6 红（`ecstasy` 会多印一个「1」）。
//   · D2：把 `MultiTargetPlayCount` 的 `usesTgt` 判据删掉（改成「有表就多播」）⇒ D2 红。
//   · D3/D4：把「目标数 > 1」换成「>= 1」⇒ D3 红；不判 null ⇒ D4 抛异常。
//
// ============================ 怎么接（宿主）============================
//   `Assets/CardPresentation/Editor/BattleScene.cs` 聚合行**同一物理行**追加（接线由调度台统一做）：
//     `B142_E3_Assert.Run(); pass += B142_E3_Assert.Pass; fail += B142_E3_Assert.Fail;`（⚠️ BattleScene 宿主里**没有 `Guard`**，照抄 Guard 会 CS0103）
//   （⚠️ 追加文本必须落在该行第一个 `//` **之前**。）
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>B142-E3 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B142_E3_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B142-E3：换牌期闸 / 称号字体面 / 派生 trait 图标 / 多目标逐目标播放");
        GroupA_CemeteryDuringBattle();
        GroupB_TitleFace();
        GroupC_DerivedTraits();
        GroupD_MultiTargetPlays();
        Summary();
    }

    // ==================================================================
    //  A 组 · `duringBattleFlag` 的等价物（换牌期为假）
    // ==================================================================
    static void GroupA_CemeteryDuringBattle()
    {
        Section("A · `B141-CEMROW-DOC`：`CemeteryDuringBattle(isOver, mulliganOpen)`");

        CheckTrue(BattleDriver.CemeteryDuringBattle(false, false),
                  "A1 开打中（未终局、不在换牌期）⇒ **真**（原版 `duringBattleFlag` 已置真）");

        CheckTrue(!BattleDriver.CemeteryDuringBattle(false, true),
                  "A2 ★ **换牌期 ⇒ 假**（判据 `_StartBattlePhase_d__334__MoveNext.c:161-165`："
                + "`duringBattleFlag` 与 `UIstate = 1` 同一句置真，换牌期 `_StartBattlePhase` 还没跑）"
                + " —— 改前这里返 true（比原版多放行一档）");

        CheckTrue(!BattleDriver.CemeteryDuringBattle(true, false),
                  "A3 终局 ⇒ 假（原版 `DeadHero` 会把 `duringBattleFlag` 置假）");

        CheckTrue(!BattleDriver.CemeteryDuringBattle(true, true),
                  "A4 终局 + 换牌期 ⇒ 假（两维任一为真都关）");
    }

    // ==================================================================
    //  B 组 · 称号字体面
    // ==================================================================
    static void GroupB_TitleFace()
    {
        Section("B · `B141-TITLEFACE`：`_titleMe/_titleFoe` 的 `SetVAlign` 必须传 Asar");

        CheckTrue(BattleDriver.TitleFace == Label.OrigFace.Asar,
                  "B1 ★ 称号两颗的字体面 = **Asar**（建标签时传的是 `TmpFont.Face.LatinUi`，见 `BattleDriver.cs:30067/:30070`）");

        CheckTrue(BattleDriver.TitleFace != Label.OrigFace.Pragati,
                  "B2 ★ 且**不是** `SetVAlign` 的缺省 `Pragati`（改前就是漏传 ⇒ 走缺省）");
    }

    // ==================================================================
    //  C 组 · 派生 trait 进 7 格图标（L986）
    // ==================================================================
    static void GroupC_DerivedTraits()
    {
        Section("C · `B141-L986-DERIVED-TRAIT`：派生 trait 进徽标（⛔ 不进引擎关键词）");
        Badges.ResetUnknownForTest();

        // C1 合成卡：只有 trigger 145（Codex）⇒ 派生 `codex`
        var codexCard = MakeCard("T_CODEX", "Test Codex", null, new[] { 145 });
        int withDerived = Badges.For(codexCard.Keywords, null, codexCard.NumericKeywords, null, true, false, codexCard).Count;
        int withoutDerived = Badges.For(codexCard.Keywords, null, codexCard.NumericKeywords, null, true, false).Count;
        CheckTrue(withDerived == 1 && withoutDerived == 0,
                  $"C1 触发 145（codex）的合成卡：改前 0 枚、并派生后 1 枚（实际 = {withoutDerived} → {withDerived}）");

        // C2 灭自证：派生 trait **不进** `_keywords`（`Has(...)` 必须看不见）
        CheckTrue(!codexCard.Has("codex"),
                  "C2 ★ 派生 trait **不进引擎关键词** —— `card.Has(\"codex\") == false`"
                + "（原版 `HasCurrentTrait` 读 `CurrentTraits`，派生 trait 只由 `CheckCurrentTraits` 追加，而它 0 调用点）");

        // C3 去重按【图】不按【词名】：`Penitence` 在 `Alias` 里就是 `rage` 那枚图 ⇒ 已有就不再加
        var penCard = MakeCard("T_PEN", "Test Penitence", new[] { "Penitence" }, new[] { 130 });
        int penBefore = Badges.For(penCard.Keywords, null, penCard.NumericKeywords, null, true, false).Count;
        int penAfter = Badges.For(penCard.Keywords, null, penCard.NumericKeywords, null, true, false, penCard).Count;
        CheckTrue(penAfter == penBefore,
                  $"C3 ★ `Penitence` 已画在 `rage` 那枚图上（`Badges.Alias`）⇒ 触发 130 派生的 `penitence` **不再加一枚**"
                + $"（实际 {penBefore} → {penAfter}；按关键词名去重会变 {penBefore + 1}）");

        // C4 带数值后缀的词同理：`Ecstasy 1`（`Strip` 后 = ecstasy）
        var ecCard = MakeCard("T_EC", "Test Ecstasy", new[] { "Ecstasy 1: Deal 3 damage to a random enemy" }, new[] { 715 });
        int ecBefore = Badges.For(ecCard.Keywords, null, ecCard.NumericKeywords, null, true, false).Count;
        int ecAfter = Badges.For(ecCard.Keywords, null, ecCard.NumericKeywords, null, true, false, ecCard).Count;
        CheckTrue(ecAfter == ecBefore,
                  $"C4 ★ `Ecstasy 1` 与派生的 `ecstasy` 是同一枚图 ⇒ 不重复（实际 {ecBefore} → {ecAfter}）");

        // C5 池级分母（现读 = 4：3 张 codex + 1 张 slay；⛔ 不是 18 —— 那 14 张已由 alias/数值后缀画出来了）
        var pool = RuleEngine.CardDatabase.Load();
        if (pool == null || pool.Count == 0)
        {
            CheckTrue(false, "C5 **前提不成立**：卡池加载为空（`CardDatabase.Load()`）—— 这条**没有执行**，不是通过");
        }
        else
        {
            int gainedCards = 0, maxPerCard = 0;
            var gainedIds = new List<string>();
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                int b0 = Badges.For(c.Keywords, null, c.NumericKeywords, null, true, false).Count;
                int b1 = Badges.For(c.Keywords, null, c.NumericKeywords, null, true, false, c).Count;
                int d = b1 - b0;
                if (d > 0) { gainedCards++; if (d > maxPerCard) maxPerCard = d; gainedIds.Add(c.Id); }
            }
            CheckTrue(gainedCards == 4,
                      $"C5 ★ 全池「并进派生 trait 后**多出一枚徽标**」的卡 = **4** 张（实际 {gainedCards}：{string.Join(",", gainedIds)}）"
                    + " —— 分母不是 18：`Penitence`×5 与 `Ecstasy 1`×10 经 `Alias`/`Strip` **本来就已经画出来了**");
            CheckTrue(maxPerCard <= 1,
                      $"C6 每张卡最多只多 1 枚（实际最大 {maxPerCard}）");
            CheckTrue(!gainedIds.Contains("EC5") && !gainedIds.Contains("SOR8"),
                      "C7 ★ 反证：`EC5`（`Ecstasy 1`）/ `SOR8`（`Penitence`）**不该**在增益名单里 —— "
                    + "按关键词名去重（而不是按图名）就会把它们算成缺口");
        }

        // C8 角标：**派生那一枚**不许印数字（`ecstasy` 在 `CarriesValue` 的 ③ 表里 ⇒ 不压制就印「1」）
        //    ⚠️ 必须用「只有派生、没有卡面关键词」的卡：`EC5` 那种自带 `Ecstasy 1` 的卡，那一枚走的是
        //    **卡自己**那一支（角标本来就该是 1），断它会误红。
        var ecOnly = MakeCard("T_EC_ONLY", "Test Derived Ecstasy", null, new[] { 715 });
        var ecBadges = Badges.For(ecOnly.Keywords, null, ecOnly.NumericKeywords, null, true, false, ecOnly);
        int ecCounter = -1;
        for (int i = 0; i < ecBadges.Count; i++)
            if (Badges.Norm(ecBadges[i].sprite) == "ecstasy") ecCounter = ecBadges[i].counter;
        CheckTrue(ecBadges.Count == 1 && ecCounter == 0,
                  $"C8 ★ **派生**的 `ecstasy` 那一枚角标 = 0、且只有 1 枚（实际枚数 {ecBadges.Count}，角标 {ecCounter}）"
                + " —— 不压制就会印一个「1」（它命中 `CarriesValue` 的 ③ 表）");
    }

    // ==================================================================
    //  D 组 · 多目标逐目标播放（L1393）
    // ==================================================================
    static void GroupD_MultiTargetPlays()
    {
        Section("D · `B141-CHK05-L1393`：`MultiTargetPlayCount`（判据 `AbilityLogic__PlayAnim.c:171-176`）");

        var three = Ev(3);
        var one = Ev(1);
        var none = Ev(0);

        CheckTrue(BattleDriver.MultiTargetPlayCount(three, Place(2, 0, 0)) == 3,
                  "D1 ★ 位置选项含 `abilityTarget(2)` + 3 个目标 ⇒ 播 **3** 遍");

        CheckTrue(BattleDriver.MultiTargetPlayCount(three, Place(0, 0, 4)) == 1,
                  "D2 ★ 位置选项**不含** `abilityTarget` ⇒ 只播 1 遍（哪怕事件带着 3 个目标）"
                + " —— 改成「有表就多播」会让这条红");

        CheckTrue(BattleDriver.MultiTargetPlayCount(one, Place(2, 0, 0)) == 1,
                  "D3 只有 1 个目标 ⇒ 1 遍（不重复播）");

        CheckTrue(BattleDriver.MultiTargetPlayCount(none, Place(2, 0, 0)) == 1,
                  "D4 没有目标表（`AbilityTargetList == null`）⇒ 1 遍（改前口径，零回归）");

        CheckTrue(BattleDriver.MultiTargetPlayCount(null, Place(2, 0, 0)) == 1,
                  "D5 事件为 `null` ⇒ 1 遍（不抛）");

        CheckTrue(BattleDriver.MultiTargetPlayCount(three, null) == 1,
                  "D6 没有 `CardAnimPlacement`（判据为空）⇒ 1 遍，退回单点路");
    }

    // ---------------------------------------------------------------- 夹具
    static BattleEvent Ev(int targets)
    {
        var e = new BattleEvent { Kind = EvtKind.Trigger, Player = 0, Slot = 1, CardId = "T_EV" };
        if (targets <= 0) return e;
        var l = new List<BattleEventTarget>();
        for (int i = 0; i < targets; i++)
            l.Add(new BattleEventTarget { Player = 1, Slot = i, CardId = "T_TGT" + i, InstanceId = 100 + i });
        e.AbilityTargetList = l;
        return e;
    }

    static CardAnimPlacement Place(int sp, int ep, int pt)
    {
        return new CardAnimPlacement { StartPos = sp, EndPos = ep, PointTowards = pt };
    }

    /// <summary>合成卡（同 `B141_W83_Assert.MakeCard` 的形状）：只填本件要用的那几列。</summary>
    static CardDef MakeCard(string id, string name, IEnumerable<string> keywords, IEnumerable<int> triggers)
    {
        var c = new CardDef(id, name, "unit", "", null, "Ultramarines", 2, 1, 5, 0, keywords,
                            subtype: "Infantry", hasActiveAbility: 0);
        c.AttachAbilityTriggers(triggers);
        return c;
    }

    // ---------------------------------------------------------------- 输出
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B142_E3 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B142_E3 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
