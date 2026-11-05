// B145_E26_Assert.cs — 批次 **B145** · 执行代理 `E26`
//   （活状态表 `B145-TRAITSCORE-FIRSTWORD` · `B145-TRAITTABLE-SYNC-ASSERT`）
//
// ============================ 它盯的是什么 ============================
//   A 组 · `B145-TRAITSCORE-FIRSTWORD`：`SimpleAI.PayloadScore` **逐段计分**。
//         原版 `AI__ScoreFromPlayingNow.c` 对每一条 scoring criterion 各算一次再
//         `fVar7 = fVar7 + fVar6` **累加**；而一条 criterion 只带一个 buff、一个 buff 只有
//         **一个** trait（`AI__ScoreFromBuff.c` 只读 `+0x3c`）。⇒ 载荷里写几项就要逐项算再相加
//         （`+1 attack and unstoppable` = 1.0 + 1.0 = 2.0，而不是「只按第一个词 = 1.0」）。
//   B 组 · `B145-TRAITTABLE-SYNC-ASSERT`：**同一张 trait→分表的两个键入口必须一致** ——
//         `SimpleAI.TraitScore(string)`（字符串键）vs `AiScoring.TraitAiScore(UnitState,int,int)`
//         （原版 `DefinedTrait` **枚举数值**键）。归一拼法 = `CardDef.DefinedTraitName(id)` + 小写化。
//
// ============================ 原版判据 ============================
//   · 逐条累加：`d:/2/tools/decomp_full/AI__ScoreFromPlayingNow.c`（`fVar6 = ScoreFromCriteria(...)`；
//     `fVar7 = fVar7 + fVar6;`）。
//   · 每条 criterion 一个 buff 一个 trait：`d:/2/tools/decomp_full/AI__ScoreFromBuff.c`
//     （`*(int *)(param_3 + 0x38)` = buffType、`*(int *)(param_3 + 0x3c)` = **单个** trait id）。
//   · 分值表：`判据/AI打分与动作选择_第2轮.md` §6·6（`AI.cs:1807-1965` 实读）。
//   · 数值 ↔ 名字：`CardDef.DefinedTraitName(int)`（表源 `Core/CardDefPartial_B94B16.cs:50-61`，
//     逐条转写自 `DefinedTrait.cs`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2/A3/A4：把 `PayloadScore` 改回「只取第一个非数字词」⇒ 全部变 1.0 / 1.2 / 0 ⇒ 红。
//   · A5：把属性系数改掉（1.0 / 0.7 / 0.6）⇒ 红。
//   · B3：把 `TraitScore` 的 `pindown` 改成 −1.0（不改 `TraitAiScore`）⇒ 跨表不一致 ⇒ 红；
//         反向同理。
//   · B4：从 `DefinedTraitNumberOfName` 删掉 `pindown/970` ⇒ `DefinedTraitName(970) == null` ⇒ 红。
//   · B7：把比较函数写成恒真（例如两边都直接 return 0）⇒ B2/B3/B5 同时红。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加：
//     `B145_E26_Assert.Run(); _pass += B145_E26_Assert.Pass; _fail += B145_E26_Assert.Fail;`
//   （本类在**全局命名空间**、类名 == 文件名 ⇒ 也可直接 `-executeMethod B145_E26_Assert.Run`。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B145-E26：`PayloadScore` 逐段计分 · 两张 trait→分表一致性。</summary>
public static class B145_E26_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int RunCount { get { return _pass + _fail; } }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log("--- 🆕 B145-E26：PayloadScore 逐段计分 / trait→分表两键一致 ---");
        GroupA_PayloadScorePerSegment();
        GroupB_TraitTableTwoKeysAgree();
        Debug.Log("--- B145-E26 合计：通过 " + _pass + " / 失败 " + _fail + " ---");
    }

    // ============================================================ 夹具
    static CardDef Hero(string id)
        => new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, (IEnumerable<string>)null);

    static CardDef Unit(string id, int cost, int atk, int hp)
        => new CardDef(id, id, "unit", "", "common", "Test", cost, atk, hp, 0, (IEnumerable<string>)null);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B145E26Hero0") };
        var d1 = new List<CardDef> { Hero("B145E26Hero1") };
        for (int i = 0; i < 10; i++)
        {
            d0.Add(Unit("B145E26Filler0_" + i, 1, 0, 1));
            d1.Add(Unit("B145E26Filler1_" + i, 1, 0, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // `UnitState` ctor 恒 `Exhausted = true` ⇒ 显式写 false（同 `B145_E4_Assert.Place`）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    // ============================================================ A：PayloadScore 逐段计分
    static void GroupA_PayloadScorePerSegment()
    {
        Section("A `PayloadScore`：一条载荷里的每一项各计一次再相加（B145-TRAITSCORE-FIRSTWORD）");

        // ---- 前提：每一项单独算出来是多少（这些是「逐词分之和」右边的那些数）----
        CheckFloat(SimpleAI.PayloadScore("+1 attack"), 1.0f,
                   "A0a 前提：`+1 attack` = 1.0（属性系数 = 我们配的 1.0）");
        CheckFloat(SimpleAI.PayloadScore("unstoppable"), 1.0f,
                   "A0b 前提：`unstoppable` = 1.0（原版表 §6·6）");
        CheckFloat(SimpleAI.PayloadScore("+1 health"), 0.7f, "A0c 前提：`+1 health` = 0.7");
        CheckFloat(SimpleAI.PayloadScore("armour 2"), 1.2f, "A0d 前提：`armour 2` = 1.2（0.6 × 2）");
        CheckFloat(SimpleAI.PayloadScore("dodge"), 2.0f, "A0e 前提：`dodge` = 2.0");
        CheckFloat(SimpleAI.PayloadScore("sniper"), 0f, "A0f 前提：`sniper` = 0（原版表里没有这一格）");

        // ---- 结论 ----
        CheckFloat(SimpleAI.PayloadScore("+1 attack and unstoppable"), 2.0f,
                   "A1 ★★ 两词载荷 = 逐词分之和（1.0 + 1.0 = 2.0）"
                 + "｜🧨 改回「只取第一个非数字词」⇒ 1.0 ⇒ 红");
        CheckFloat(SimpleAI.PayloadScore("+1 attack, +1 health and unstoppable"), 2.7f,
                   "A2 ★★ 三词载荷 = 逐词分之和（1.0 + 0.7 + 1.0 = 2.7）"
                 + "｜🧨 改回只取第一个词 ⇒ 1.0 ⇒ 红");
        CheckFloat(SimpleAI.PayloadScore("armour 2 and dodge"), 3.2f,
                   "A3 ★ 属性段 + 关键词段（1.2 + 2.0 = 3.2）"
                 + "｜🧨 把 ` and ` 两段当一段（只认第一个词）⇒ 1.2 ⇒ 红");
        CheckFloat(SimpleAI.PayloadScore("sniper and +1 ranged"), 1.0f,
                   "A4 ★ 第一个词是**0 分关键词**、第二个才是属性（0 + 1.0 = 1.0）"
                 + "｜🧨 旧实现只取第一个词 `sniper` ⇒ 0 ⇒ 红（这是改前真正在漏分的形状）");

        // ---- 分段器本身 ----
        var segs = new List<string>(SimpleAI.SplitPayloadSegments("+1 attack, +1 health and unstoppable"));
        CheckTrue(segs.Count == 3, "A5a `SplitPayloadSegments` 把三词载荷拆成 3 段（实际 = " + segs.Count + "）");
        CheckTrue(segs.Count == 3 && segs[0] == "+1 attack" && segs[1] == "+1 health" && segs[2] == "unstoppable",
                  "A5b 三段内容/顺序 = `+1 attack` / `+1 health` / `unstoppable`");
        CheckFloat(SimpleAI.PayloadSegmentScore("armour 2"), 1.2f, "A5c 单段 `armour 2` = 1.2");

        // ---- 反例对照：单段口径不变（⛔ 别把整张表改宽） ----
        CheckFloat(SimpleAI.PayloadScore("shield"), 2.0f, "A6a 对照：单段 `shield` = 2.0（改前改后一致）");
        CheckFloat(SimpleAI.PayloadScore("fast"), 0f, "A6b 对照：单段 `fast` = 0（原版表里没有这一格）");
        CheckFloat(SimpleAI.PayloadScore(""), 0f, "A6c 对照：空载荷 = 0");
        CheckFloat(SimpleAI.PayloadScore("__B145E26_NotAPayload__"), 0f,
                   "A6d 对照：认不出的段 = 0（⛔ 不猜，与 `TraitScore` 的 `default:` 同口径）");

        // ---- 顺手修好的一格（改前「第一个词 = an」⇒ 0） ----
        CheckFloat(SimpleAI.PayloadScore("an additional +1 health"), 0.7f,
                   "A7 顺手：属性词不在段首（`an additional +1 health`）也能认出来（改前 = 0，静默漏分）"
                 + "｜⚠️ 这一格不是本任务点名的形状，是「按段找属性词」带来的附带修正");
    }

    // ============================================================ B：两张表的键一致
    static void GroupB_TraitTableTwoKeysAgree()
    {
        Section("B `TraitScore(string)` ↔ `TraitAiScore(int)`：同一张表、两种键（B145-TRAITTABLE-SYNC-ASSERT）");

        // ---- B1：拼法归一的样板（原版枚举名 = 驼峰，`TraitScore` 的键 = 全小写） ----
        Check(CardDef.DefinedTraitName(970), "pindown", "B1a 归一：`970` → 枚举名 `pindown`");
        Check(CardDef.DefinedTraitName(630), "firstStrike",
              "B1b 归一：`630` → 枚举名 `firstStrike` ⇒ 小写后 `firststrike`（= `TraitScore` 的键）");
        Check(CardDef.DefinedTraitName(150), "cantAttack",
              "B1c 归一：`150` → 枚举名 `cantAttack` ⇒ 小写后 `cantattack`（= `KeywordTable.CantAttack`）");

        // ---- B2：数值键入口可达 + 非平凡值（证明这个断言真的在读两张表）----
        CheckFloat(AiScoring.TraitAiScore(null, 970, 0), -1.5f,
                   "B2a `TraitAiScore(null, pindown=970, 0)` = −1.5（原版 §6·6）");
        CheckFloat(SimpleAI.TraitScore("pindown"), -1.5f, "B2b `TraitScore(\"pindown\")` = −1.5");
        CheckTrue(AiScoring.TraitAiScore(null, 970, 0) != SimpleAI.TraitScore("__B145E26_NotAKeyword__"),
                  "B2c 【灭自证】比较器能区分 −1.5 与 0（不是「怎么比都相等」）");

        // ---- B3：全枚举逐 id 对拍（0..1400）----
        int cmp = 0, mismatch = 0;
        string detail = "";
        for (int id = 0; id <= 1400; id++)
        {
            string name = CardDef.DefinedTraitName(id);
            if (name == null) continue;
            float a = SimpleAI.TraitScore(name.ToLowerInvariant());   // 字符串键入口
            float b = AiScoring.TraitAiScore(null, id, 0);            // 枚举值键入口（无单位上下文）
            cmp++;
            if (Math.Abs(a - b) > 1e-4f)
            {
                mismatch++;
                if (detail.Length < 600)
                    detail += " [" + id + " " + name + " 字符串=" + a + " 数值=" + b + "]";
            }
        }
        CheckTrue(cmp == CardDef.DefinedTraitCount,
                  "B3a 覆盖：枚举到 " + cmp + " 个 trait id（应 = `DefinedTraitCount` " + CardDef.DefinedTraitCount + "）");
        CheckTrue(mismatch == 0,
                  "B3b ★★ 两张表对全部 " + cmp + " 个 trait id 给出**同一分值**（不一致 " + mismatch + " 条）" + detail
                + "｜🧨 只改其中一张表的一格 ⇒ 这里必红");

        // ---- B4：被计分的 43 个 id 每一个都必须能折回名字（防「映射缺了 ⇒ 0==0 静默通过」）----
        int[] scoredIds = new int[]
        {
            980, 240, 80, 430, 630, 640,          // 2.0
            70, 20, 470, 610,                     // 1.5
            140, 985, 1160, 180, 390, 110, 160, 190, 120, 450, 455, 840, 520, 620, 625, 760, 770, // 1.0
            230, 360,                             // 0.5
            100, 970, 975,                        // −1.5
            480, 1170,                            // −1.0
            150,                                  // −2.0
            200,                                  // −4.0
            380,                                  // 3.0
            220, 370, 850, 780,                   // 动态取值
            350, 1120,                            // e.traitValue
        };
        int missing = 0;
        string missingDetail = "";
        for (int i = 0; i < scoredIds.Length; i++)
            if (CardDef.DefinedTraitName(scoredIds[i]) == null)
            { missing++; if (missingDetail.Length < 300) missingDetail += " " + scoredIds[i]; }
        CheckTrue(missing == 0,
                  "B4 表内 " + scoredIds.Length + " 个 trait id 都能折回枚举名（缺 " + missing + " 个）" + missingDetail
                + "｜🧨 从 `DefinedTraitNumberOfName` 删掉任何一格 ⇒ 红");

        // ---- B5：带单位上下文的动态档（4 个）—— 字符串键的带单位重载 vs 数值键入口 ----
        var ctx = NewCtx();
        var u = Place(ctx, 0, 5, Unit("B145E26_Dyn", 2, 3, 10));
        // 前提（⛔ 不写 `if (前提) Check(结论)`）—— 放在加关键词**之前**，免得关键词副作用混进来
        CheckTrue(u.Attack == 3, "B5a 前提：探针单位近战 = " + u.Attack + "（= 3）");
        u.AddKeyword(RuleCore.Maintenance, 2);
        u.AddKeyword(KeywordTable.Bastion, 4);
        u.AddKeyword("mutation", 5);
        CheckTrue(u.KwValue(RuleCore.Maintenance) == 2, "B5b 前提：maintenance 关键词值 = " + u.KwValue(RuleCore.Maintenance) + "（= 2）");
        CheckTrue(u.Bastion == 4, "B5c 前提：bastion 关键词值 = " + u.Bastion + "（= 4）");
        CheckTrue(u.KwValue("mutation") == 5, "B5d 前提：mutation 关键词值 = " + u.KwValue("mutation") + "（= 5）");

        // ① 先钉**具体数值**（独立于「两个入口互相比」⇒ 防两处同时错成一样）
        CheckFloat(SimpleAI.TraitScoreExtra(KeywordTable.BloodThirst, u), 3f,
                   "B5e bloodThirst(220) = 单位当前近战攻击力（= 3）");
        CheckFloat(SimpleAI.TraitScoreExtra(RuleCore.Maintenance, u), 2f, "B5f maintenance(370) = 2");
        CheckFloat(SimpleAI.TraitScoreExtra(KeywordTable.Bastion, u), 4f, "B5g bastion(850) = 4");
        CheckFloat(SimpleAI.TraitScoreExtra("mutation", u), 5f, "B5h mutation(780) = 5");
        // ② 再对拍两个入口
        CheckFloat(SimpleAI.TraitScoreExtra(KeywordTable.BloodThirst, u), AiScoring.TraitAiScore(u, 220, 0),
                   "B5i bloodThirst(220)：带单位两入口同值");
        CheckFloat(SimpleAI.TraitScoreExtra(RuleCore.Maintenance, u), AiScoring.TraitAiScore(u, 370, 0),
                   "B5j maintenance(370)：带单位两入口同值");
        CheckFloat(SimpleAI.TraitScoreExtra(KeywordTable.Bastion, u), AiScoring.TraitAiScore(u, 850, 0),
                   "B5k bastion(850)：带单位两入口同值");
        CheckFloat(SimpleAI.TraitScoreExtra("mutation", u), AiScoring.TraitAiScore(u, 780, 0),
                   "B5l mutation(780)：带单位两入口同值");

        // ---- B6：已知边界 —— `blast(350)` / `shuriken(1120)` 原版读 `e.traitValue`，字符串入口拿不到 buff ----
        CheckFloat(AiScoring.TraitAiScore(null, 350, 6), 6f,
                   "B6a 边界：数值键入口 `blast` = `e.traitValue`（这里传 6 ⇒ 6）");
        CheckFloat(SimpleAI.TraitScoreExtra("blast", u), 0f,
                   "B6b 边界：字符串入口没有 buff 对象 ⇒ 退回 `t.KwValue(\"blast\")`（这里 = 0）"
                 + "｜📌 这两格**本来就不相等**（判据能拿到的输入不同），⛔ 别把它算成不一致");
        CheckFloat(AiScoring.TraitAiScore(null, 1120, 0), SimpleAI.TraitScore("shuriken"),
                   "B6c 边界对照：`shuriken` 在 traitValue = 0 时两入口同值（0 == 0）");
    }

    // ============================================================ 断言小工具（同 `B144_R2_AI_Assert` / `B145_E4_Assert`）
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void CheckFloat(float actual, float expected, string msg)
    {
        bool ok = Math.Abs(actual - expected) <= 1e-4f * Math.Max(1f, Math.Abs(expected));
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B145E26] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B145E26] ✗ " + msg); }
    }
}
