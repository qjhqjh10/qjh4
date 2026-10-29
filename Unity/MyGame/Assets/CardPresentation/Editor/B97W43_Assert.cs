// B97W43_Assert.cs — 第九十七会话 · 执行代理 `B97_W43`（2026-10-29 · 波 4 · 表现层收尾）
//
// ============================ 它盯的是什么 ============================
// 账 `战斗逻辑驱动:804`（`CardScript.MinionPlayedIntoField`）的**最后一块**：
// 把卡 SO 的 `summonAnimDelayed`（原版 `rawCardData + 0xB0` 那个 AssetReference）从通用 `deploy` 桶里**分出来**，
// 补上原版 **state 3** 的两道闸，并在**落地那一拍**播它。
//
//   · 判据 = `decomp_full/CardScript._MinionPlayedIntoField_d__314__MoveNext.c:186-250`（整段一起读全）
//     + 工厂 `CardScript__MinionPlayedIntoField.c:20-25`（`+0x3c ← param_6` = `shouldTriggerSummon`）
//     + `CardScript.__ctor`（`+0x154 = 0.05` · `+0x150 = 0.2` · `+0x1a0 = AttackStepTime = 0.1`）
//   · 数据 = `Resources/card_vfx_by_card.json` 的 `es[].so`（2026-10-29 起写入；消费端 = `Core/VfxMap.cs` 的 `CardPick.so`）
//   · 播放 = `BattleDriver.PlaySummonDelayedVfx`；两道闸 = `BattleDriver.SummonAnimGate`
//
// 本件断三段：
//   ① 闸（`SummonAnimGate`）：`HandPlayed` ∧ 非 `ephemeral` ∧ 非 `ambush` ⇒ **真**；四个反例各一条。
//   ② 数据：`VfxMap.ResolveSummonDelayed` 取得到那件；**通用 `deploy` 解析已经不再返回它**（= 那张卡终于会播登场淡入），
//      而**原始逐卡桶里它还在**（证明这是「分桶」而不是「把数据删了」）。
//   ③ 接线：`BattleDriver.cs` 里 `PlaySummonDelayedVfx(e)` **恰好 2 处**（玩家 `PlaySignal` 的 Deploy 支 + AI 落地拍）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   见每条断言的「🧨」后缀（就在断言文案里，这里不抄第二份）。
//   骨架那一对：g-1 真 / g-2 假 —— 恒 `true` ⇒ g-2 红；恒 `false` ⇒ g-1 红。
//   数据那一对：d-2（通用桶剔干净）/ d-4（原始桶还在）—— 删数据 ⇒ d-4 红；不过滤 ⇒ d-2 红。
//   接线那一条：`PlaySummonDelayedVfx(e)` 的数量 —— 删一处 ⇒ 1 ⇒ 红；多挂一处（普通召唤/离场）⇒ 3 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B97W43_Assert.CheckB97W43(driver);`
//   · `drv` 只在 ① **要读同一局里有没有「已经带 ephemeral/ambush 的单位」时**用得上 —— 本件**不用**：
//     夹具是**离盘的** `new UnitState(CardDef, false)`（不碰场上的真单位，⛔ 不改任何状态）。
//     ⇒ 传 `null` 也完全成立（`BattleDriver` 那一侧一个字段都不读）。
//   · ② 要 `Resources/card_vfx_by_card.json` 已载入（`VfxMap.CardTableCards > 0`，任何宿主都会懒载）。
//   · ③ 要读源码文件（`Application.dataPath` 下的 `.cs`）⇒ 只在编辑器里成立（本文件本来就在 `Editor/`）。

using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_W43`：账 `战斗逻辑驱动:804` 分桶 + 两道闸的**不变量**读数口。
/// <para>⛔ 期望值一律**字面量**（prefab 名 / 枚举档 / 2 处），不从被测实现里反算。</para></summary>
public static class B97W43_Assert
{
    static readonly CheckSink _sink = new CheckSink("[W43] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>全池唯一一张带 `summonAnimDelayed` 的卡（`SOR33 Saint Celestine`）解出来的 prefab 名
    /// （判据 → `B96_GEN` 的卡 SO type tree 普查 + 本件现读运行时表）。</summary>
    const string SummonPrefab = "Sororitas Saint Custom summon";
    const string Faction = "Sororitas";
    const string CardId = "SOR33";
    /// <summary>原版 `DefinedTrait.ephemeral = 5` 的规范名（`KeywordTable` 没有这个常量 ⇒ 与生产同一处口径）。</summary>
    const string Ephemeral = "ephemeral";

    /// <summary>本件唯一入口（宿主 `Run()` 里用一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，同 `B97W3_Assert` 的理由）。</summary>
    public static void CheckB97W43(BattleDriver drv = null)
    {
        MenuCheck.Section(_sink, "W43：账 804 —— `summonAnimDelayed` 分桶 + state 3 两道闸");
        CheckGate(drv);
        CheckDataLine();
        CheckWiring();
        Debug.Log(_sink.P + "W43 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  ① 两道闸（`BattleDriver.SummonAnimGate`）
    //    判据 = `_MinionPlayedIntoField_d__314__MoveNext.c:186-197`：
    //      `if (shouldTriggerSummon == 0 || HasCurrentTrait(5 ephemeral) || HasCurrentTrait(0x4b0 ambush)) ⇒ 跳过`
    //    逐调用点实读（`shouldTriggerSummon` = 第 6 个实参，含 this）：
    //      `_ResolvePlayCardFromHand:659/892` = 1 · `ResolveTransformUnit.c:1207` = 1
    //      `_ResolveCardAutoSummoned_d__511:245` = `& 0xffffffffffffff00` ⇒ **0**
    //      `_ResolveSummonUnit_d__510:295` 只给 4 个实参 ⇒ 默认 false
    // ==================================================================
    static void CheckGate(BattleDriver drv)
    {
        // 夹具 = **离盘的** `UnitState`（`new UnitState(CardDef, false)` ⇒ `CardInstance.Detached`）
        //   ⇒ `AddKeyword` 只会动这个临时对象，⛔ 不碰场上任何一个真单位。
        var pool = CardDatabase.Load();
        CardDef proto = null;
        if (pool != null)
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null || !c.IsUnit) continue;
                if (c.Has(Ephemeral) || c.Has(KeywordTable.Ambush)) continue;
                proto = c; break;
            }
        Tr(proto != null, "W43-804g-0（前提）卡池里能取到一张**既非 ephemeral 也非 ambush** 的单位卡当夹具"
                        + "（取不到 ⇒ 红，⛔ 不静默跳过——本仓踩过 3 次「前提不成立 ⇒ 结论空转绿」）");
        if (proto == null) return;

        var u = new UnitState(proto, false);
        Tr(!u.Has(Ephemeral) && !u.Has(KeywordTable.Ambush),
            "W43-804g-0b（前提）夹具单位身上确实没有那两个词（`" + proto.Name + "`）"
          + "｜🧨 换一张带 ambush 的卡当夹具 ⇒ 红（那时下面的 g-1 就没有判别力）");

        Tr(BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u),
            "W43-804g-1 ★ `shouldTriggerSummon = true`（= `DeploySource.HandPlayed`）且非 ephemeral/ambush ⇒ **播**"
          + "（闸真 ∧ 数据 `d-1` 有件 ∧ 接线 `w-1` 两处 = 三段都成立即「播」）"
          + "｜🧨 把闸 1 改成 `!= HandPlayed`（或恒 false）⇒ 红");

        u.AddKeyword(Ephemeral, 1);
        Tr(!BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u),
            "W43-804g-2（灭自证）★ 同一个单位挂上 `ephemeral` ⇒ **不播**"
          + "（原版 `HasCurrentTrait(5)` 那一句）｜🧨 删掉闸 2 的 `u.Has(Ephemeral)` 那一行 ⇒ 红；"
          + "闸恒 true ⇒ 也红");

        var u2 = new UnitState(proto, false);
        u2.AddKeyword(KeywordTable.Ambush, 1);
        Tr(!BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u2),
            "W43-804g-3（灭自证）★ 挂上 `ambush` ⇒ **不播**（原版 `HasCurrentTrait(0x4b0)` 那一句）"
          + "｜🧨 删掉闸 2 的 `u.Has(KeywordTable.Ambush)` 那一行 ⇒ 红");

        var u3 = new UnitState(proto, false);
        Tr(!BattleDriver.SummonAnimGate(DeploySource.AutoPlayFromHand, u3),
            "W43-804g-4 ★ `AutoPlayFromHand`（原版 `_ResolveCardAutoSummoned`）⇒ **不播** —— 那一处第 6 个实参"
          + "写的是 `in_stack_… & 0xffffffffffffff00`（低位清零 ⇒ false）"
          + "｜🧨 把闸 1 放宽成「`HandPlayed || AutoPlayFromHand`」（`B97_W37` 报告正文的写法）⇒ 红");

        Tr(!BattleDriver.SummonAnimGate(DeploySource.Summon, u3),
            "W43-804g-5 ★ `Summon`（原版 `_ResolveSummonUnit`，只给 4 个实参 ⇒ 后两格吃默认 false）⇒ **不播**"
          + "｜🧨 把闸 1 改成「所有 `Deploy` 都触发」⇒ 红（那会让每个效果召唤的单位都多播一件）");

        Tr(!BattleDriver.SummonAnimGate(DeploySource.Reanimate, u3),
            "W43-804g-6 ★ `Reanimate`（残骸翻回，走的是另一条链）⇒ **不播**｜🧨 只判 `!= Summon` ⇒ 红");

        Tr(!BattleDriver.SummonAnimGate(DeploySource.HandPlayed, null),
            "W43-804g-7 ★ 那一格**没有单位**（已离场）⇒ 不播（原版摆不到「卡的位置」上）"
          + "｜🧨 删掉 `SummonAnimGate` 里的 `u == null` 那一行 ⇒ 红");
    }

    // ==================================================================
    //  ② 数据线：分桶 + 淡化兜底
    //    判据 = `VfxMap.DropSummonDelayedPicks` / `ResolveSummonDelayed`（`Core/VfxMap.cs` 末段）
    // ==================================================================
    static void CheckDataLine()
    {
        Tr(VfxMap.CardTableCards > 0,
            "W43-804d-0（前提）运行时逐卡特效表已载入（`VfxMap.CardTableCards > 0`）｜🧨 表没载 / 文件没了 ⇒ 红");
        if (VfxMap.CardTableCards <= 0) return;

        C(VfxMap.ResolveSummonDelayed(Faction, CardId), SummonPrefab,
            "W43-804d-1 ★ 取数口 `ResolveSummonDelayed(Sororitas, SOR33)` = 那件 prefab"
          + "｜🧨 生成器漏写 `so` 键 / DTO 又把它丢了（`JsonUtility` 静默忽略）⇒ null ⇒ 红");

        var all = VfxMap.ResolveAllTimed(VfxMap.Deploy, Faction, CardId,
                                         (IReadOnlyList<CardDef.CardAnimTime>)null);
        int n = 0;
        for (int i = 0; i < all.Length; i++) if (all[i].Name == SummonPrefab) n++;
        C(n, 0,
            "W43-804d-2 ★ 通用 `deploy` 解析**不再返回**那件 summon 动画（原版它是 state 3 的第二拍，不是登场那一拍）"
          + "｜🧨 去掉 `DropSummonDelayedPicks` 那一跳 ⇒ 1 ⇒ 红；"
          + "把剔除做成「删数据」⇒ 与 d-4 一起红");

        Tr(all.Length >= 1 && !string.IsNullOrEmpty(all[0].Name),
            "W43-804d-3 ★ 剔完之后**必须落回登场淡入那一档**（`Invoke Minion Card Fade Default/Legendary`），不许留白"
          + "｜🧨 把 `DropSummonDelayedPicks` 改成「剔完仍算命中」⇒ 返回空数组 ⇒ 红（那张卡会连淡入都没有）"
          + "；实得 " + all.Length + " 件");

        C(VfxMap.ResolvePerCard(VfxMap.Deploy, Faction, CardId), SummonPrefab,
            "W43-804d-4（灭自证）★ **原始逐卡桶里它还在**（`ResolvePerCard` = 不经过滤的口）"
          + " ⇒ 证明这是「消费端分桶」而不是「把数据删掉」"
          + "｜🧨 去改生成器把这条从 `deploy` 桶删掉 ⇒ 红（那时 d-2 的绿就没有意义了）");
    }

    // ==================================================================
    //  ③ 接线：两处调用点都还在
    //    宿主 `PlaySignal` 的 `Deploy` 支（玩家/召唤）· `PlayAiDeployLandingVfx`（AI 飞行落地拍）
    // ==================================================================
    static void CheckWiring()
    {
        string p = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        Tr(File.Exists(p), "W43-804w-0（前提）能读到 `BattleDriver.cs`（" + p + "）");
        if (!File.Exists(p)) return;
        C(CountOf(File.ReadAllText(p), "PlaySummonDelayedVfx(e)"), 2,
            "W43-804w-1 ★ `PlaySummonDelayedVfx(e)` 在 `BattleDriver.cs` 里**恰好 2 处**"
          + "（① `PlaySignal` 的 `Deploy` 支 · ② `PlayAiDeployLandingVfx`）"
          + "｜🧨 删掉任一处 ⇒ 1 ⇒ 红（那一路就永远不播）；再挂第三处（普通召唤 / 离场）⇒ 3 ⇒ 红");
    }

    static int CountOf(string hay, string needle)
    {
        int n = 0, at = 0;
        while (true)
        {
            at = hay.IndexOf(needle, at, System.StringComparison.Ordinal);
            if (at < 0) return n;
            n++; at += needle.Length;
        }
    }
}
