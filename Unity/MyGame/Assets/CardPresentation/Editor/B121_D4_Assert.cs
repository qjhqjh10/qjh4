// B121_D4_Assert.cs — 执行写手 `B121-D4`（第一百二十一~二十二会话 · 卡表数据列补齐）
//
// ============================ 它盯的是什么（账 `B121-04` / `B121-05` / `B121-DATA-09` / `B121-06`）====
//   ① `B121-04`：卡表**没有** `healCriteria` 列 ⇒ `EffectResolver.DoHeal` 只能吃正文正则；现加
//      `abilityHeal`（原版 `AbilityLogic.healCriteria +0x48` 三格）＋消费口 `ApplyHealCriteria`。
//   ② `B121-05`：`manaBuff` 十一格无载体 ⇒ 现加 `abilityMana`（**只落 `ca == 60` 的行**）＋
//      消费口 `ApplyManaBuff`（定「哪一方收」与 `create`/`refill` 的种类）。
//   ③ `B121-DATA-09`：`AbilityBuffSpec.PlayersAffected` / `Criteria` 在**卡表**那一列**恒缺**
//      （生成器原来只给附魔模板表补那四格）⇒ 现补；消费端 `AttachHandAbilityBuff` 早就在读它们。
//   ④ `B121-06`：报告的断语「`spellType` 没进 `CardCriteria`、`conditionsFilter` 整列不存在」——
//      **判据本身错**。本文件用两条断言把真值钉住（数据在**另外两列**、消费端也早在读），
//      ⛔ 别再照那句去加第二份数据。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · H1/M1/P1 是「数据落对」—— 把生成器对应的 helper 删掉/改键名 ⇒ `AbilityHealAt` 等返回 null ⇒ 红。
//   · H2/M2 是「反证」—— 生成器**没按 `ca` 闸过滤**（把非 `ca 20` / 非 `ca 60` 的行也落下来）⇒
//     `AbilityHealAt(0,1)` / `AbilityManaBuffAt(0,0)` 变成有行 ⇒ 红。
//   · H3/M4 是「三态」—— 旧表（`version < 48`）必须 `Has* == false`（= 消费端退回正文）；
//     谁把 `null` 折成 `new[0]` ⇒ `Has*` 变真 ⇒ 红（那会把「未知」静默变成「明确没有」）。
//   · P3 是哨兵反证 —— 非 `handBuff` 行的 `PlayersAffected` 必须仍 `-1`（不适用），
//     谁把 `-1` 折成 `0`（Both）⇒ 红（那会让「没这一格」静默变成「给双方」）。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里加一行聚合：`Step(B121D4Case);`
//   （本文件是 `RuleEngineTest` 的**分部** ⇒ 复用夹具 `Check` / `CheckTrue` / `Section`，
//     ⛔ 另起一个类会抄第二份。⚠️ 那一行在 `RuleEngine/Editor/RuleEngineTest.cs`，**不属本代理白名单**
//     ⇒ 接线补丁写在报告 `报告/B121_D4_数据列.md` §接线 里，由调度台合并。）
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>B121-D4 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(B121D4Case);`）。</summary>
    public static void B121D4Case()
    {
        Section("🆕 B121-D4 —— 卡表 v48 三处数据列（`abilityHeal` / `abilityMana` / `handBuff` 四格）"
              + " ＋ `B121-06` 的真值（`spellType` / `conditionsFilter` 在另外两列）");
        B121D4Heal();
        B121D4Mana();
        B121D4HandBuffPlayersAffected();
        B121D4CriteriaColumnsReallyExist();
    }

    // ==============================================================================================
    //  ① `abilityHeal`（账 `B121-04`）
    // ==============================================================================================
    static void B121D4Heal()
    {
        var pool = CardDatabase.Load();
        CardDef saw = null, node = null, diviner = null, bd = null;
        foreach (var c in pool)
        {
            if (c.Name == "Sawbonez") saw = c;
            else if (c.Name == "Infestation Node") node = c;
            else if (c.Name == "Orikan the Diviner") diviner = c;
            else if (c.Name == "Beautiful Death") bd = c;
        }

        // ---- H0（硬前提）：这四张卡都在池里 ----
        CheckTrue(saw != null && node != null && diviner != null && bd != null,
                  "H0 （前提）样本卡都在池里：Sawbonez / Infestation Node / Orikan the Diviner / Beautiful Death");

        // ---- H1（数据落对）：池内 ≥ 40 张有 `abilityHeal` 行，且区间那一条读得对 ----
        int healed = 0;
        foreach (var c in pool) if (c.HasAbilityHeal) healed++;
        Check(healed >= 40, true,
              $"H1 池内**有 `abilityHeal` 列**的卡 {healed} 张 ≥ 40（现读 57 张 / 60 行；"
              + "判据 = `AbilityLogic.healCriteria +0x48`，生成器 `ability_heal_of`）");
        Check(saw != null ? saw.AbilityHealAt(1, 0)?.Min ?? -99 : -99, 1,
              "H1 `Sawbonez` ab1/lg0 的 `healCriteria.minValue` = **1**（卡面 `heal 1-5 to it`）");
        Check(saw != null ? saw.AbilityHealAt(1, 0)?.Max ?? -99 : -99, 5,
              "H1 同一条 `maxValue` = **5** ⇒ 区间（`DoHeal` 走 `ctx.Rng` 掷，与原版一致）");
        Check(node != null ? node.AbilityHealAt(0, 0)?.Min ?? -99 : -99, 2,
              "H1 `Infestation Node` ab0/lg0 = **2/2**（定值档）");
        Check(diviner != null ? diviner.AbilityHealAt(1, 0)?.Min ?? -99 : -99, 1,
              "H1 `Orikan the Diviner` ab1/lg0 = **1/1**");

        // ---- H2（反证 / `ca` 闸）：非 `ca 20` 的行**不许**落进这一列 ----
        //   `Infestation Node` 的 ab0/lg1 在真值里是一条 `ca 100` 的 `healCriteria 2/2`
        //   （SO 序列化残留）；原版 `PlayAbility.c:1178` 只在 `ca 20` 那一支读它 ⇒ 生成器必须滤掉。
        Check(node != null && node.AbilityHealAt(0, 1) == null, true,
              "H2 （反证）`Infestation Node` ab0/lg1（真值里是 `ca 100` 的残留行）**没有** `abilityHeal` 行"
              + " —— 生成器按 `ca == 20` 滤过（⛔ 滤掉这道闸 ⇒ 本格变有行 ⇒ 红）");
        //   `Beautiful Death` 的两条 heal 行（`ca 320` / `ca 20` 的 `valueOptions = 210`）：
        //   `ca 320` 那条不进列；`ca 20` 那条进列但 `opt = 210`（本仓没有那个求值档）⇒ 消费端沿用正文。
        Check(bd != null ? bd.AbilityHealAt(1, 0)?.ValueOptions ?? -99 : -99, 210,
              "H2 `Beautiful Death` ab1/lg0（真值里 `ca 320` 那条不进列；进列的是 `ab1/lg0` 的 `ca 20` 且 `valueOptions = 210`）—— 2026-11-03（B123 首跑）就地订正：原写 `ab0/lg1` 是把两个下标写反了（现读 `cards_engine.json`：`{ab:1, lg:0, ca:20, opt:210}`）"
              + "（`TargetExcessDamage` —— 本仓没有这个档 ⇒ `ApplyHealCriteria` 出声并**沿用正文**）");

        // ---- H3（三态 / 反证）：旧表（version < 48）⇒ 整列未知，消费端必须退回正文 ----
        var oldPool = CardDatabase.Parse(OldTableJson());
        CardDef oldSaw = null;
        foreach (var c in oldPool) if (c.Name == "Sawbonez") oldSaw = c;
        CheckTrue(oldSaw != null && !oldSaw.HasAbilityHeal && oldSaw.AbilityHealAt(1, 0) == null,
                  "H3 （反证）`version < 48` 的卡表：`HasAbilityHeal == false` 且 `AbilityHealAt` 返回 **null**"
                  + " ⇒ 消费端退回正文口径（谁把 `null` 折成空数组 ⇒ 本格红）");
    }

    // ==============================================================================================
    //  ② `abilityMana`（账 `B121-05`）
    // ==============================================================================================
    static void B121D4Mana()
    {
        var pool = CardDatabase.Load();
        CardDef plasma = null, recon = null, pit = null;
        foreach (var c in pool)
        {
            if (c.Name == "Plasma Generator") plasma = c;
            else if (c.Id == "e8cf1eb55ca41534abe38efc90b302e9") recon = c;   // 自设计卡：`Reconnaissance Mission`
            else if (c.Name == "Hellfire Pit") pit = c;
        }

        CheckTrue(plasma != null && recon != null && pit != null,
                  "M0 （前提）样本卡都在池里：Plasma Generator / Reconnaissance Mission / Hellfire Pit");

        // ---- M1（数据落对）：`create` / `refill` / 面别 三格 ----
        var p = plasma != null ? plasma.AbilityManaBuffAt(0, 0) : null;
        CheckTrue(p != null, "M1 `Plasma Generator` ab0/lg0 有 `abilityMana` 行（原版 `ca 60` 的 `manaBuff`）");
        if (p != null)
        {
            Check(p.CreateMana, 1, "M1 `Plasma Generator` 的 `createMana = 1`（抬上限，`ProcessManaBuff.c:38-49`）");
            Check(p.RefillMana, 0, "M1 同一条 `refillMana = 0`（⇒ 当前能量不补，与卡面 `Gain 1 Energy` 一致）");
            Check(p.FriendlyPlayer, 1, "M1 同一条 `friendlyPlayer = 1`（施放方收）");
            Check(p.ManaAmount, 1, "M1 同一条 `manaAmount = 1`");
        }
        var r = recon != null ? recon.AbilityManaBuffAt(0, 0) : null;
        CheckTrue(r != null, "M1 `Reconnaissance Mission`（自设计卡，`desc` 为空）有 `abilityMana` 行"
                           + " —— 正文解析不出来时这条**唯一**的数据来源");
        if (r != null)
        {
            Check(r.EnemyPlayer, 1, "M1 `Reconnaissance Mission` 的 `enemyPlayer = 1`（**对面**收）");
            Check(r.FriendlyPlayer, 0, "M1 同一条 `friendlyPlayer = 0` ⇒ `ApplyManaBuff` 会把 `op.Target.Side` 置 `enemy`");
            Check(r.QuestPoints, 1, "M1 同一条 `questPoints = 1`（任务点）");
            Check(r.ManaAmount, 3, "M1 同一条 `manaAmount = 3`（这一行的量）");
        }

        // ---- M2（反证 / `ca` 闸）：非 `ca 60` 的 `manaBuff` 行**不许**落 ----
        //   `Hellfire Pit` 的 ab0/lg0 是 `ca 10`（真值里带 `refillMana = 1` 的残留）——原版只在
        //   `ca 60` 那一支读 `manaBuff` ⇒ 生成器过滤后这一列里没有它。
        Check(pit != null && pit.AbilityManaBuffAt(0, 0) == null, true,
              "M2 （反证）`Hellfire Pit` ab0/lg0（真值里 `ca 10` 带 `refillMana`）**没有** `abilityMana` 行"
              + " —— 生成器按 `ca == 60` 滤过（⛔ 滤掉 ⇒ 本格红）");

        // ---- M3（池内规模）：≥ 40 张有这一列（现读 44 张 / 51 行） ----
        int n = 0;
        foreach (var c in pool) if (c.HasAbilityManaBuff) n++;
        Check(n >= 40, true, $"M3 池内**有 `abilityMana` 列**的卡 {n} 张 ≥ 40（现读 44 张 / 51 行）");

        // ---- M4（三态）：旧表 ⇒ 整列未知 ----
        var oldPool = CardDatabase.Parse(OldTableJson());
        CardDef oldP = null;
        foreach (var c in oldPool) if (c.Name == "Plasma Generator") oldP = c;
        CheckTrue(oldP != null && !oldP.HasAbilityManaBuff && oldP.AbilityManaBuffAt(0, 0) == null,
                  "M4 （反证）`version < 48` 的卡表：`HasAbilityManaBuff == false` 且 `AbilityManaBuffAt` 返回 **null**");
    }

    // ==============================================================================================
    //  ③ 卡表 `abilityBuffs` 的 `handBuff` 四格（账 `B121-DATA-09`）
    // ==============================================================================================
    static void B121D4HandBuffPlayersAffected()
    {
        var pool = CardDatabase.Load();
        CardDef venom = null, network = null;
        foreach (var c in pool)
        {
            if (c.Name == "Venomcrawler") venom = c;
            else if (c.Name == "Underground Network") network = c;
        }

        CheckTrue(venom != null && network != null,
                  "P0 （前提）样本卡都在池里：Venomcrawler / Underground Network");

        // ---- P1（数据落对）：Self(10) 与 Enemy(20) 两档 + criteria 挂上了 ----
        CardDatabase.AbilityBuffSpec vRow = null, nRow = null;
        foreach (var b in (CardDatabase.AbilityBuffsOf(venom) ?? new List<CardDatabase.AbilityBuffSpec>()))
            if (b != null && b.IsHandBuff) vRow = b;
        foreach (var b in (CardDatabase.AbilityBuffsOf(network) ?? new List<CardDatabase.AbilityBuffSpec>()))
            if (b != null && b.IsHandBuff) nRow = b;
        CheckTrue(vRow != null, "P1 `Venomcrawler` 有 `handBuff` 载荷行");
        Check(vRow != null ? vRow.PlayersAffected : -99, 10,
              "P1 该行 `playersAffected = 10 (Self)` —— 原版 `HandEffect.playersAffected`，"
              + "消费端 `RuleCore.AttachHandAbilityBuff` 按它选「哪一方的手」");
        CheckTrue(vRow != null && vRow.Criteria != null,
                  "P1 该行 `criteria`（`HandEffect.targetCriteria` 36 格）**已挂上** —— 改前卡表这一列恒 `null`");
        CheckTrue(nRow != null && nRow.PlayersAffected == 20,
                  "P1 `Underground Network` 的 `handBuff` 行 `playersAffected = 20 (Enemy)`（给对面那张手加费）");

        // ---- P2（规模）：池内 `handBuff` 行 = 53，且 `pa != -1` 的正是这 53 行 ----
        int rows = 0, withPa = 0;
        foreach (var c in pool)
        {
            var list = CardDatabase.AbilityBuffsOf(c);
            if (list == null) continue;
            foreach (var b in list)
            {
                if (b == null || !b.IsHandBuff) continue;
                rows++;
                if (b.PlayersAffected != -1) withPa++;
            }
        }
        Check(rows, 53, "P2 池内 `handBuff` 载荷行 = **53**");
        Check(withPa, rows, "P2 这 53 行的 `playersAffected` **全部有值**（≠ -1）—— 卡表这一列不漏行");

        // ---- P3（反证 / 哨兵）：**非** `handBuff` 行的 `PlayersAffected` 必须仍是 -1（不适用）----
        bool sawNonHand = false, nonHandLeaked = false;
        foreach (var c in pool)
        {
            var list = CardDatabase.AbilityBuffsOf(c);
            if (list == null) continue;
            foreach (var b in list)
            {
                if (b == null || b.IsHandBuff) continue;
                sawNonHand = true;
                if (b.PlayersAffected != -1) nonHandLeaked = true;
            }
        }
        CheckTrue(sawNonHand, "P3 （前提）池内存在非 `handBuff` 的 `abilityBuffs` 行");
        CheckTrue(!nonHandLeaked,
                  "P3 （反证）非 `handBuff` 行的 `playersAffected` 恒 `-1`（**不适用**）—— "
                  + "谁把 `-1` 折成 `0`（Both）⇒ 本格红（那会把「没这一格」静默变成「给双方」）");
    }

    // ==============================================================================================
    //  ④ `B121-06`：`spellType` / `conditionsFilter` 的**真值**（报告那句断语是错的）
    // ==============================================================================================
    static void B121D4CriteriaColumnsReallyExist()
    {
        var pool = CardDatabase.Load();
        CardDef ec17 = null, gsc6 = null;
        foreach (var c in pool)
        {
            if (c.Name == "Konstrictus Tormentor") ec17 = c;
            else if (c.Name == "Neophyte Initiate") gsc6 = c;
        }
        CheckTrue(ec17 != null && gsc6 != null,
                  "S0 （前提）样本卡都在池里：Konstrictus Tormentor / Neophyte Initiate");

        // ---- S1：`spellType` **已经在** `abilityTargetCriteria` 列里，而且消费端读得到 ----
        var set = ec17 != null ? ec17.AbilityTargetCriteriaAt(0, 0) : null;
        CheckTrue(set != null, "S1 `Konstrictus Tormentor` ab0/lg0 有 36 格 criteria（`abilityTargetCriteria`）");
        Check(set != null ? set.SpellType : -99, 260,
              "S1 该行 `spellType = 260 (Elixir)` —— **不是「没落列」**（`B121-06` 报告只看了 `CardCriteria` 那条路）");
        //   消费端：`TargetCriteriaEval.Matches` 的第 ⑦ 步按它淘汰（判据 `CheckIfMeetsCriteria.c:301-308`）
        //   ⇒ 这里只钉「数据 + 载体都在」，筛人逻辑由 `B116_W26` 那批断言覆盖。

        // ---- S2：`conditionsFilter` **在 `unitConditions` 的 `role="target"` 行里**，不在 36 格里 ----
        //   判据：`CardDef.cs` 的 v25 段头注释写死「原版只有一个 criteria 对象，cond/ex 两格由
        //   `unitConditions` 的 `role="target"` 行表达」（我们拆成两处是**数据形状**，消费端按 `(ab,lg)` 拼回）。
        Check(gsc6 != null ? gsc6.ConditionOf(0, 0, "target") : -99, 220,
              "S2 `Neophyte Initiate` ab0/lg0 的 `role=\"target\"` 行 `cond = 220`（= `targetCriteria.conditionsFilter`）"
              + " —— **这一列不是不存在，是在 `unitConditions`**");
        CheckTrue(gsc6 != null && gsc6.ConditionOf(0, 0, "target") != CardDef.ConditionNone,
                  "S2 同上：`cond` 不是 `none(0)`（取值域与 `UnitConditions` 枚举一致）");

        // ---- S3（反证）：`CardCriteria.FromTarget` 那一路**没有** `spellType` 生产者 ⇒ 必须留 -1 ----
        var spec = new EffectTargetSpec { Raw = "any unit", Side = "any", Kind = "unit", Count = 1 };
        var crit = CardCriteria.FromTarget(spec);
        CheckTrue(crit == null || crit.SpellType == -1,
                  "S3 （反证）正文短语翻出来的 `CardCriteria.SpellType` 恒 **-1**（「不筛」）—— "
                  + "⛔ 别给它编一个默认 0（`0 = Normal` 是**合法筛选值**，会把非普通卡全筛掉）");
    }

    /// <summary>一张**旧表**（`version = 47`，没有 v48 那两列）的最小卡表 JSON —— 只放样本那几张卡，
    /// 用来断「缺列 = 未知」（三态里的第一态）。字段形状与 `cards_engine.json` 同源。</summary>
    static string OldTableJson()
    {
        return "{\"version\":47,\"count\":2,\"cards\":["
             + "{\"id\":\"b4d6d186eb95a3f468013c5be649ee3b\",\"name\":\"Sawbonez\",\"type\":\"tactic\","
             + "\"cost\":1,\"attack\":0,\"health\":0,\"ranged\":0,\"desc\":\"Deal 1-2 damage. If the target survives, heal 1-5 to it\","
             + "\"faction\":\"Orks\",\"rarity\":\"common\",\"subtype\":\"\"},"
             + "{\"id\":\"DA61\",\"name\":\"Plasma Generator\",\"type\":\"tactic\","
             + "\"cost\":1,\"attack\":0,\"health\":0,\"ranged\":0,\"desc\":\"Gain 1 Energy\","
             + "\"faction\":\"Adeptus Mechanicus\",\"rarity\":\"common\",\"subtype\":\"\"}"
             + "]}";
    }
}
