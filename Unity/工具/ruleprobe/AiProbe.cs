// AiProbe.cs — `ai <卡id|卡名>`：把 **AI 打分** 拆开摆出来（只读 · 不判对错）
//
// 🔴 **它修的是哪一类【结构性失明】**（2026-10-25 · 第八十会话 · 代理 `R11`）：
//   `play all dump` 是**结算层**读数（列 = playRc / played / dmg / changes…），
//   **整份 dump 里没有一列是 AI 决策或 AI 分数** —— 实测：本仓 `play all dump` 在
//   「只改 AI 打分」的那一版前后**逐字节相同**（`R11` 报告 §四）。
//   ⇒ 在那之前，每一笔 AI 打分改动都只能给「解析式读数」（手算），**没有机器读数口**。
//   本模式补的就是这个口：**同一个 `ScoreFromCriteria` / `ScoreFromAttack` / `ScoreDamaging`
//   实现**（⛔ 不另写一份算式），只是把每一段的贡献打出来。
//
// 判据（打分公式本身）→ `资料/完全复刻/判据/AI打分与动作选择_第2轮.md`；
// 落地报告 → `资料/完全复刻/报告/R11_AI打分落地_第2轮.md`。
//
// ⚠️ **它不判对错**：读数摆出来，判决归人 / 调度台（与 `play` 同一条纪律）。
// ⛔ **本文件不改引擎**（`MyGame/Assets/**` 不在本工具的白名单里；只**读**卡池与 `ai_scoring.json`）。
// ⛔ **不影响既有输出**：不碰基线、不碰三个指纹、不改 `check`/`play` 的任何一行。
//
// 用法：
//   bash d:/4/Unity/工具/ruleprobe.sh ai SOR49
//   bash d:/4/Unity/工具/ruleprobe.sh ai "Fire Dragon"
//
// 怎么用它验 `R11` 那几条（报告 §四 有同样的例子）：
//   · `R6-02`（option 5 的 `mightAct` 闸）：找一条 option 5 的卡（例 `Path of the Warrior`），
//     `[出牌分·逐条]` 里那一行会打 `mightAct=真 M / 假 K` —— K>0 说明这个目标被闸掉了。
//   · `R6-04`（爆裂/标记光/毒）：`[攻击分·分解]` 的 ③⑤⑥ 三段直接给数（`⑤ 爆裂 = +blast×相邻`）。
//   · `R6-05`（小兵比值 + 「打没打死」闸）：`[挨打分]` 的曲线在 `d = 目标血` 处**跳变**，
//     且**与护甲无关** —— 拿带甲的夹具对比同伤害两行即可。
//   · `R6-07`（挨反击存活 +0.1）：`[攻击分·分解]` 的 ② 行会写出 `存活奖励=0.1` 还是 `0`。
//   · `R6-10`（option 35 的 `base` 进乘法）：`[出牌分·逐条]` 里 option 35 那一行的数
//     = `base + M·mult·(delta+base)`（`delta` 会把 `HandMax/HandCount` 一起打出来）。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RuleEngine;

class AiProbe
{
    // ------------------------------------------------------------------
    //  夹具（固定、种子化）—— 与 `play` 的夹具**刻意不同**：本模式不真打，只摆局面
    // ------------------------------------------------------------------

    const int ProbeSlot = 3;      // 我方探针单位
    const int FoePlainSlot = 3;   // 敌方「无甲」目标
    const int FoeArmSlot = 5;     // 敌方「2 甲」目标（R6-05/07 的对照）

    public static int Run(string spec, string spec2 = null)
    {
        // 🆕 `B8_AV`（2026-10-26）：`ai actions <卡id>` —— 动作表那一档（见 `RunActions`）。
        //   只有第一参**字面量**等于 `actions` 才切档 ⇒ 老用法（`ai <卡id|卡名>`）一字不动。
        if (string.Equals(spec, "actions", StringComparison.OrdinalIgnoreCase)) return RunActions(spec2);
        // 🆕 `B88_E3`（2026-10-27）：`ai board <卡id>` —— **被测卡摆上棋盘**那一档（见 `RunBoard`）。
        //   🔴 为什么需要：`ai actions` 把被测卡放**手牌** ⇒ 任何「**只有上场的单位才产**」的候选
        //   （第 ④ 档主动能力 = 原版通用 `activeAbilityLogic`；以及任何「盘上单位才枚举」的支）
        //   在那个口上**结构上不可观测**。本档直接把卡摆上盘，并把「AI 挑不挑它 / 结算成不成」打出来。
        if (string.Equals(spec, "board", StringComparison.OrdinalIgnoreCase)) return RunBoard(spec2);
        if (string.IsNullOrWhiteSpace(spec))
        {
            Console.Error.WriteLine("!! 用法：ai <卡id|卡名>    |    ai actions <卡id|卡名>    |    ai board <卡id|卡名>");
            return 1;
        }

        var pool = Program.PoolList;
        var card = Find(pool, spec);
        if (card == null)
        {
            Console.Error.WriteLine("!! 卡池里找不到：" + spec);
            return 1;
        }

        var dto = LoadScoring(card.Id);
        if (dto == null)
            Console.WriteLine("!! `ai_scoring.json` 里没有这张卡的条目（= 原版「缺列」⇒ 出牌分只剩 ValueInPlay）");

        UnitState probe, foePlain, foeArm, foeWarlord;
        var ctx = BuildFixture(pool, card, out probe, out foePlain, out foeArm, out foeWarlord);

        Console.WriteLine("=== ai " + card.Id + " \"" + card.Name + "\" ===");
        Console.WriteLine("[卡] type=" + card.Type + " cost=" + card.Cost + " atk=" + card.Attack
                        + " ranged=" + card.RangedAttack + " hp=" + card.Health
                        + " armour=" + (probe != null ? probe.Armor.ToString() : "-")
                        + "  ·  ctx.Active=" + ctx.Active
                        + "  ·  条目 nonTarget=" + Count(dto != null ? dto.nonTargetScoringCriteria : null)
                        + " cardTarget=" + Count(dto != null ? dto.cardTargetScoringCriteria : null));

        // ---- ① 卡在场上的价值 ----
        float vip = probe != null ? SimpleAI.ValueInPlay(probe) : 0f;
        Console.WriteLine("[价值] GetValueInPlay = " + F(vip) + "（= 2×费+1 + 附魔 − 伤势/眩晕衰减）");

        // ---- ② 出牌分：逐条 criteria 的贡献（**这就是「每一项各多少」**）----
        float sum = 0f;
        if (dto != null)
        {
            Console.WriteLine("[出牌分·逐条]（`ScoreFromCriteria` 的返回值 = 这一条的贡献）");
            sum += DumpColumn(ctx, dto, "nonTarget", dto.nonTargetScoringCriteria, card, probe, foePlain, true);
            sum += DumpColumn(ctx, dto, "cardTarget", dto.cardTargetScoringCriteria, card, probe, foePlain, false);
        }
        Console.WriteLine("  ⇒ ScoreFromPlayingNow = " + F(sum)
                        + "   ·  ScoreFromPlayingCard ≈ " + F((card.IsUnit ? vip : 0f) + sum)
                        + "（单位才加 GetValueInPlay；战术/低语 = 0，原版 `IsSpellType` 口径）");

        // ---- ③ 攻击分：逐段分解（R6-03 / R6-04 / R6-07）----
        if (probe != null && foePlain != null)
        {
            foreach (bool ranged in new[] { false, true })
            {
                if (ranged && probe.RangedAttack <= 0) continue;
                Console.WriteLine("[攻击分·分解] " + (ranged ? "远程" : "近战")
                                + " → P1[" + FoePlainSlot + "] " + foePlain.Name
                                + "（我方 atk=" + RuleCore.FieldAttack(ctx, 0, probe, ranged)
                                + " · 守方 atk=" + foePlain.Attack + " · 守方血=" + foePlain.Health + "）");
                var lines = new List<string>();
                SimpleAI.TraceAttack = lines.Add;
                float s;
                try { s = SimpleAI.ScoreFromAttack(ctx, 0, probe, foePlain, ranged); }
                finally { SimpleAI.TraceAttack = null; }
                foreach (var l in lines) Console.WriteLine("  " + l);
                Console.WriteLine("  ⇒ ScoreFromAttack = " + F(s));
            }
        }
        else
            Console.WriteLine("[攻击分] 跳过（这张卡不是能出手的单位，或夹具没有敌方目标）");

        // ---- ④ 挨打分：`ScoreDamaging` 的伤害曲线（R6-05 的「打死闸 + 比值」，**护甲不参与**）----
        foreach (var t in new[] { foePlain, foeArm })
        {
            if (t == null) continue;
            var parts = new List<string>();
            int top = t.Health + 2;
            for (int d = 1; d <= top; d++) parts.Add("d=" + d + "→" + F(SimpleAI.ScoreDamaging(t, d)));
            Console.WriteLine("[挨打分] " + t.Name + "（血 " + t.Health + " · 甲 " + t.Armor
                            + " · 堡垒 " + t.Bastion + " · 卡价值 " + F(SimpleAI.ValueInPlay(t)) + "）："
                            + string.Join(" · ", parts));
        }
        if (foeWarlord != null)
        {
            var parts = new List<string>();
            for (int d = 1; d <= 6; d++) parts.Add("d=" + d + "→" + F(SimpleAI.ScoreWarlordDamage(foeWarlord, d)));
            Console.WriteLine("[督军分] " + foeWarlord.Name + "（血 " + foeWarlord.Health + " · 甲 " + foeWarlord.Armor + "）："
                            + string.Join(" · ", parts));
        }

        Console.WriteLine("=== 结束（本模式只读、不返回红绿；判据在卡面与 `判据/AI打分与动作选择_第2轮.md`）===");
        return 0;
    }

    // ------------------------------------------------------------------
    //  一条 criteria 列
    // ------------------------------------------------------------------

    static float DumpColumn(BattleContext ctx, AiCardScoringDto dto, string label,
                            AiScoringEntryDto[] entries, CardDef card, UnitState probe,
                            UnitState target, bool nonTarget)
    {
        if (entries == null || entries.Length == 0) return 0f;
        float sum = 0f;
        int side = ctx.Active;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null) continue;
            // 原版：cardTarget 那列先过 `CheckIfMeetsCriteria`，不过就整条跳过
            if (!nonTarget && probe != null && target != null
                && !AiTargets.MatchesTarget(ctx, e.targetCriteria, card, null, side, target))
            {
                Console.WriteLine("  " + label + "[" + i + "] option=" + e.scoringOption + " ⇒ 跳过（targetCriteria 不成立）");
                continue;
            }
            float v = AiScoring.ScoreFromCriteria(ctx, card, null, side, target, e);
            // 🔴 `nonTarget` 那两列**原版是所有卡都跑的、而且「这一条值多少」取决于有没有目标**
            //    （`baseValue` 按 `target` 是不是异阵营选 friend/enemy；option 9/10/35 都吃这个）。
            //    ⇒ 两个都打出来，合计按【无目标】算（单位那条路原版就不带目标，见
            //      `SimpleAI.ScoreFromPlayingCard`：`card.IsUnit ? -1 : a.TargetP`）。
            float vNoTarget = nonTarget ? AiScoring.ScoreFromCriteria(ctx, card, null, side, null, e) : v;
            sum += vNoTarget;
            var extra = new System.Text.StringBuilder();
            if (e.targetCriteria != null)
                extra.Append(" targets=").Append(AiTargets.GetTargets(ctx, e.targetCriteria, card, null, side, target).Count);
            if (e.multiplierCriteria != null && e.multiplierCriteria.targetsAffected != 0)
                extra.Append(" M=").Append(AiTargets.GetTargets(ctx, e.multiplierCriteria, card, null, side, target).Count);
            if (e.scoringOption == 35)
            {
                // `R6-10` 的读数：把 `delta` 的来源一起打出来（`base + M·mult·(delta+base)`；
                // 牌库空那一支 = `−2 − 疲劳`，**不带**里层那个 `base`）
                var p = ctx.Players[side];
                extra.Append(" HandMax=").Append(RuleCore.HandMaxFor(ctx))
                     .Append(" hand=").Append(p.Hand.Count)
                     .Append(" deck=").Append(p.Deck.Count)
                     .Append(" fatigue=").Append(p.Fatigue)
                     .Append(p.Deck.Count == 0 ? "（牌库空支）" : "（delta 支）");
            }
            if (e.scoringOption == 5 && e.targetCriteria != null)
            {
                // `R6-02` 的读数：这批目标里有多少**能行动**（`mightAct`）—— 假的那几个会被闸掉
                var ts = AiTargets.GetTargets(ctx, e.targetCriteria, card, null, side, target);
                int yes = 0, no = 0;
                for (int k = 0; k < ts.Count; k++) { if (AiTargets.MightAct(ctx, ts[k])) yes++; else no++; }
                extra.Append(" mightAct=真").Append(yes).Append("/假").Append(no);
            }
            if (e.scoringOption == 5)
            {
                // 🆕 2026-10-26（批次 9 · `E4P08` · 账行 12）：**`defensive=` 读数**。
                //   原版 `AI.GetDefensiveAttackValue` 的**调用点 = 0**（`decomp_full` 全树只命中它自己那份 `.c`）
                //   ⇒ ⛔ 这个数**不参与打分**（`v` / `sum` 一个字节都没动），只把「若接线会扣成多少」摆出来。
                //   三档一起摆 = 三条支各一个读数：双督军 ⇒ 0 · 单位打督军 2.1 ⇒ 原样 · 1.9 ⇒ 0（`2.0f` 那道闸）。
                var myHero = ctx.Players[side].Warlord;
                var foeHero = ctx.Players[1 - side].Warlord;
                if (myHero != null && foeHero != null)
                    extra.Append(" defensive=督军互砍→").Append(F(AiScoring.GetDefensiveAttackValue(myHero, foeHero, 2.1f)))
                         .Append("（应 0）");
                // 「非督军打督军」那一档：本方法**不看阵营**（只读 `cardType` 与 `currentScore`）
                // ⇒ 用夹具里那个「无甲小兵」当非督军样本即可；option 5 的卡都是战术/低语（`probe == null`），
                //   所以 probe 那道口在这里恒取不到。
                var anyUnit = probe != null ? probe : ctx.Players[1 - side].Board[FoePlainSlot];
                if (anyUnit != null && foeHero != null)
                    extra.Append(" 非督军打督军 2.1→").Append(F(AiScoring.GetDefensiveAttackValue(anyUnit, foeHero, 2.1f)))
                         .Append(" 1.9→").Append(F(AiScoring.GetDefensiveAttackValue(anyUnit, foeHero, 1.9f)))
                         .Append("（应 2.1 / 0）");
                if (myHero == null || foeHero == null)
                    extra.Append(" defensive=（本夹具缺督军 ⇒ 不可读）");
                extra.Append("（未接线：原版 0 调用点）");
            }
            if (e.scoringOption == 13)
            {
                // 🆕 2026-10-26（批次 9 · `E4P08` · 账行 29）：**⑥⑦ 两项**（督军 / 同侧场上的
                //   `activeAbility.manaCost` 落区间各 +1）的分解。借产品自己的 `ScoreFromManaBuff`
                //   ＋ `TraceCriteria` 钩子（⛔ 不在这里另写一份算式）—— 跑完**立刻复位**成 null。
                var mbLines = new List<string>();
                AiScoring.TraceCriteria = mbLines.Add;
                try { AiScoring.ScoreFromManaBuff(ctx, side, e); }
                finally { AiScoring.TraceCriteria = null; }
                for (int k = 0; k < mbLines.Count; k++)
                    Console.WriteLine("  " + label + "[" + i + "]" + mbLines[k]);
            }
            Console.WriteLine("  " + label + "[" + i + "] option=" + e.scoringOption
                            + " base=" + F(e.baseValueFriend) + "/" + F(e.baseValueEnemy)
                            + " mult=" + F(e.multiplyByFriend) + "/" + F(e.multiplyByEnemy)
                            + extra
                            + (nonTarget
                               ? " ⇒ 无目标=" + F(vNoTarget) + " · 有敌方目标=" + F(v)
                               : " ⇒ " + F(v)));
        }
        return sum;
    }

    // ------------------------------------------------------------------
    //  夹具
    // ------------------------------------------------------------------

    static BattleContext BuildFixture(List<CardDef> pool, CardDef card,
                                      out UnitState probe, out UnitState foePlain,
                                      out UnitState foeArm, out UnitState foeWarlord)
    {
        // 夹具卡：全部 `Test` 阵营、名字带 `R11` 前缀（⛔ 别用池内真卡 —— 那会让读数依赖卡池版本）
        var heroP0 = new CardDef("R11HERO0", "R11HERO0", "hero", "", "common", "Test", 0, 2, 30, 0, null);
        var heroP1 = new CardDef("R11HERO1", "R11HERO1", "hero", "", "common", "Test", 0, 2, 30, 0, null);
        var filler = new CardDef("R11FILL", "R11FILL", "unit", "", "common", "Test", 1, 0, 1, 0, null);
        var foeCard = new CardDef("R11FOE", "R11FOE", "unit", "", "common", "Test", 3, 2, 8, 0, null);
        var armCard = new CardDef("R11FOE_ARM", "R11FOE_ARM", "unit", "", "common", "Test", 3, 2, 8, 0,
                                  new[] { "Armour 2" });

        var d0 = new List<CardDef> { heroP0, card };
        var d1 = new List<CardDef> { heroP1, foeCard };
        for (int i = 0; i < 12; i++) { d0.Add(filler); d1.Add(filler); }

        var vars = GameplayVariables.Classic.Clone();
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);

        // ⚠️ 夹具**直接摆棋盘**（不走 `PlayCard`）：本模式只量打分，不量部署链。
        probe = null;
        if (card.IsUnit)
        {
            probe = new UnitState(ctx.NewInstance(card), false);
            ctx.Players[0].Board[ProbeSlot] = probe;
        }
        foePlain = new UnitState(ctx.NewInstance(foeCard), false);
        foeArm = new UnitState(ctx.NewInstance(armCard), false);
        ctx.Players[1].Board[FoePlainSlot] = foePlain;
        ctx.Players[1].Board[FoeArmSlot] = foeArm;
        foeWarlord = ctx.Players[1].Warlord;
        return ctx;
    }

    static CardDef Find(List<CardDef> pool, string spec)
    {
        foreach (var c in pool) if (string.Equals(c.Id, spec, StringComparison.OrdinalIgnoreCase)) return c;
        foreach (var c in pool) if (string.Equals(c.Name, spec, StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }

    static int Count(AiScoringEntryDto[] a) { return a == null ? 0 : a.Length; }

    /// <summary>把 `"untilEndOfTurn": 0/1` 归一成 `false/true`（**只在内存里改，⛔ 不写回文件**）。
    ///
    /// 为什么需要：`AiBuffDto.untilEndOfTurn` 在我们 DTO 里声明成 **`bool`**，而 `ai_scoring.json`
    /// 里它写的是 **整数**（现数 **118 处** —— SO 里那个字段本来就是 `int`）。
    /// Unity 的 `JsonUtility` 对「数字给 bool」是宽容的，但本探针的桩走 `System.Text.Json`
    /// （`IncludeFields`）—— 那是严格的，会抛 `The JSON value could not be converted to System.Boolean`。
    ///
    /// 🔴 **这是探针的兼容垫片，不是「数据没问题」**：`顺手发现` 已把「DTO=bool / 数据=int」这条
    ///   类型不一致记进 `R11` 报告（建议生成器改写成 `true/false`，或 DTO 改成 `int`）。
    /// </summary>
    static string NormalizeBools(string json)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            json, "\"untilEndOfTurn\"\\s*:\\s*(-?[0-9]+)",
            m => "\"untilEndOfTurn\":" + (m.Groups[1].Value == "0" ? "false" : "true"));
    }

    static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

    /// <summary>从 `MyGame/Assets/RuleEngine/Resources/ai_scoring.json` 按 id 取一条。
    /// ⚠️ **不走 `AiScoringData`**（它的装载口要 `Resources.Load`，探针里那是桩 ⇒ 恒 null）——
    /// 这里直接读文件 + `JsonUtility` 桩（`System.Text.Json`，字段名逐字对齐）。
    /// ⛔ 只读：不改那个文件、也不写回。</summary>
    static AiCardScoringDto LoadScoring(string cardId)
    {
        try
        {
            string path = Path.Combine(Program.OutDir, "RuleEngine", "Resources", "ai_scoring.json");
            if (!File.Exists(path)) { Console.Error.WriteLine("!! 找不到 " + path); return null; }
            var file = UnityEngine.JsonUtility.FromJson<AiScoringFileDto>(NormalizeBools(File.ReadAllText(path)));
            if (file == null || file.cards == null) return null;
            foreach (var c in file.cards)
                if (c != null && string.Equals(c.id, cardId, StringComparison.OrdinalIgnoreCase)) return c;
        }
        catch (Exception ex) { Console.Error.WriteLine("!! 读 ai_scoring.json 失败：" + ex.Message); }
        return null;
    }
    // ==================================================================
    //  🆕 `B8_AV`（2026-10-26）：`ai actions <卡id|卡名>` —— **动作表**读数口
    // ==================================================================
    //
    // 🔴 它治的是哪一条（`B8_E` 报告 §五 原话）：
    //   「`BattleManager.GetAvailableActions` … `play all dump` **结构上看不见 AI**；
    //    `ai` 模式只打**打分**、不打**动作表**。⇒ 需要 `ruleprobe ai actions <卡id>`」。
    //   本模式把 `SimpleAI.EnumerateActions(ctx)`（= 原版 `BattleManager.GetAvailableActions` 的等价物）
    //   的每一条动作摆出来，并逐条给 `ScoreAction`，最后给 `SelectBestAction` 挑中的那一条。
    //
    // ⚠️ **它不判对错**：读数摆出来，判决归人 / 调度台（与 `play` 同一条纪律）。
    // ⛔ **不影响既有输出**：不碰基线、不碰三个指纹、不改 `check`/`play`/`ai`（打分那一档）的任何一行。
    // ⚠️ **两条已知边界**（照 `B8_E` §五 抄，⛔ 别当成本模式的缺陷）：
    //   ① 夹具**直接摆棋盘**（不走部署链）⇒ 手牌/牌库卡只看 criteria 贡献；
    //   ② `ScoreAction` 里 option 5 在「攻击方 ≠ `ctx.Active`」时 `BestAttackScore` 会因
    //      `IsValidTarget` 返 0 ⇒ **读数 0 不一定是闸**（本模式把 `Kind` + `slot` 一起打出来，方便分辨）。
    public static int RunActions(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            Console.Error.WriteLine("!! 用法：ai actions <卡id|卡名>");
            return 1;
        }
        var pool = Program.PoolList;
        var card = Find(pool, spec);
        if (card == null)
        {
            Console.Error.WriteLine("!! 卡池里找不到：" + spec);
            return 1;
        }
        UnitState probe;
        BattleContext ctx;
        try { ctx = BuildActionsFixture(pool, card, out probe); }
        catch (Exception ex)
        {
            Console.Error.WriteLine("!! 建夹具时引擎抛异常：" + ex.GetType().Name + ": " + ex.Message);
            return 1;
        }

        var list = SimpleAI.EnumerateActions(ctx);
        Console.WriteLine("=== ai actions " + card.Id + " \"" + card.Name + "\" · 动作表（原版 `BattleManager.GetAvailableActions` 的等价物）===");
        Console.WriteLine("[夹具] ctx.Active=" + ctx.Active + "（本模式量的是**行动方**的动作表）· 被测卡**在手牌里**"
                          + (card.Type == "hero" ? "（⚠️ 它是 `hero`，hero 不从手牌打出 ⇒ 表里不会有它自己的 `PlayCard` 行）" : "")
                          + " · 我方棋盘上有 1 张探针单位（`Exhausted=false`，为了量到攻击行）· 敌方 2 张（无甲 / 2 甲）"
                          + " · 能量 " + ctx.Players[ctx.Active].Energy + "/" + ctx.Players[ctx.Active].MaxEnergy);
        Console.WriteLine("[动作表] 共 **" + list.Count + "** 条（`EndTurn` 恒为最后一条、分数恒 0）");
        Console.WriteLine("  列：序号 · Kind(=原版 `PlayerActions` 取值) · 手牌下标 · 落点格 · 目标P@格 · Ranged · 花多少能 · ScoreAction · 备注");

        var byKind = new Dictionary<string, int>();
        float bestScore = float.NegativeInfinity;
        int bestIdx = -1;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            float sc = SimpleAI.ScoreAction(ctx, a);
            // 🔴 必须把分数**写回** `a.Score` —— 产品自己的那条路就是这么做的
            //   （`SimpleAI.NextAction`：`foreach (var a in list) a.Score = ScoreAction(ctx, a);`
            //    → `TweakAvailableActions` → `SelectBestAction`）。
            //   ⚠️ `EnumerateActions` **不填** `Score` ⇒ 不写回的话 `SelectBestAction` 看到的是
            //   一堆 0（实测：最高分明明是 3.1 的攻击，它却按「全 0 + 先出现的赢」挑了回去）。
            a.Score = sc;
            string kn = KindName(a.Kind);
            byKind[kn] = (byKind.TryGetValue(kn, out int c0) ? c0 : 0) + 1;
            if (sc > bestScore) { bestScore = sc; bestIdx = i; }
            var note = new System.Text.StringBuilder();
            if (a.Kind == AiActionKind.PlayCard)
            {
                var hc = a.HandInst != null ? a.HandInst.Card : null;
                note.Append(hc == null ? "card=?" : "card=" + hc.Id);
                if (hc != null && string.Equals(hc.Id, card.Id, StringComparison.OrdinalIgnoreCase))
                    note.Append(" ← **被测卡**");
            }
            if (a.AltKeyword != null) note.Append(" alt=").Append(a.AltKeyword);
            Console.WriteLine("  [" + i.ToString().PadLeft(3) + "] " + Pad(kn, 15)
                            + " hand#=" + (a.HandIdx >= 0 ? a.HandIdx.ToString() : "-").PadLeft(2)
                            + " slot=" + (a.Slot >= 0 ? a.Slot.ToString() : "-").PadLeft(2)
                            + " →P" + (a.TargetP >= 0 ? a.TargetP.ToString() : "-")
                            + "@" + (a.TargetSlot >= 0 ? a.TargetSlot.ToString() : "-").PadLeft(2)
                            + " ranged=" + (a.Ranged ? "1" : "0")
                            + " mana=" + a.ManaCost
                            + " score=" + F(sc)
                            + (note.Length > 0 ? "  " + note : ""));
        }
        Console.WriteLine("[按 Kind 计数] " + string.Join(" · ",
            byKind.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)));
        Console.WriteLine("[最高分] [" + bestIdx + "] "
                          + (bestIdx >= 0 ? KindName(list[bestIdx].Kind) + " = " + F(bestScore) : "（空表）"));
        // 产品自己的挑法（`SelectBestAction` 就是 AI 决策那一处，⛔ 不另写一份打分）
        try
        {
            var best = SimpleAI.SelectBestAction(ctx, new List<AiAction>(list));
            Console.WriteLine("[SelectBestAction] "
                              + (best == null ? "（null —— 没有可做的动作）"
                                              : KindName(best.Kind)
                                                + (best.HandIdx >= 0 ? " hand#" + best.HandIdx : "")
                                                + (best.Slot >= 0 ? " @" + best.Slot : "")
                                                + (best.TargetSlot >= 0 ? " →P" + best.TargetP + "@" + best.TargetSlot : "")
                                                + " = " + F(SimpleAI.ScoreAction(ctx, best))));
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SelectBestAction] 🔴 抛异常：" + ex.GetType().Name + ": " + ex.Message);
        }
        // 🆕 产品那条**完整**的路：`NextAction` = 重新枚举 → 逐条写回 Score → `TweakAvailableActions`（难度旋钮）
        //   → `SelectBestAction`。它才是 `PlayTurn` 真正的一步（⛔ `SelectBestAction` 只是一半：
        //   它看不见难度那套「把低分动作随机砍掉」的旋钮）。
        try
        {
            AiAction nbest;
            bool can = SimpleAI.NextAction(ctx, AiDifficulty.Normal, out nbest);
            Console.WriteLine("[NextAction(Normal)] " + (can ? "有一条" : "**没有**（最佳是 endTurn ⇒ 这一回合收手）")
                              + (nbest == null ? ""
                                 : "  ⇒ " + KindName(nbest.Kind)
                                   + (nbest.HandIdx >= 0 ? " hand#" + nbest.HandIdx : "")
                                   + (nbest.Slot >= 0 ? " @" + nbest.Slot : "")
                                   + (nbest.TargetSlot >= 0 ? " →P" + nbest.TargetP + "@" + nbest.TargetSlot : "")
                                   + " = " + F(nbest.Score))
                              + "\n   ⚠️ 它带 `TweakAvailableActions` 的难度旋钮（`Normal`：`minSkips/maxSkips/skipChance`）"
                              + " ⇒ 与上面 `SelectBestAction`（**不带旋钮**）不同是**预期的**，⛔ 不是缺陷。");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[NextAction] 🔴 抛异常：" + ex.GetType().Name + ": " + ex.Message);
        }
        Console.WriteLine("=== 结束（本模式只读、不返回红绿；判据在卡面与 `判据/AI打分与动作选择_第2轮.md`）===");
        return 0;
    }

    // ==================================================================
    //  🆕 `ai board <卡id>`（`B88_E3` · 2026-10-27）：**被测卡摆上棋盘**
    //
    //  🔴 它修的是哪一类【结构性失明】：`ai actions` 的夹具把被测卡放**手牌** ⇒ 只有上场单位才产的候选
    //  （第 ④ 档主动能力 = 原版通用 `activeAbilityLogic`）在那个口上**一条都看不见** ——
    //  实测 `ai actions SOR66` 改前改后都是 17 条、`[按 Kind 计数]` 里没有 `activeAbility`，
    //  而那一档恰恰是**盘上单位**的入口。
    //
    //  本档做三件事（⛔ 只调引擎接口，不改引擎、不写基线）：
    //    ① 把被测卡**直接摆上棋盘**，打动作表 + 逐条 `ScoreAction`（同 `actions` 口径）；
    //    ② 单独把**第 ④ 档候选**挑出来，用 `AiScoring.TraceCriteria` 打它**打分逐条贡献**
    //       （`ability.nonTarget[i] option=… ⇒ v`，判据 = 原版 `AI.GetActionScore` case 5）；
    //    ③ **实打一次**：在**同 seed 的第二份夹具**上强制 `SimpleAI.ExecuteAction` 那条 ④ 档候选，
    //       打 `True/False` ＋ 前后局面（能量 / 攻击 / `Exhausted` / 事件数）⇒ 「结算真的成功」有读数。
    //       ⚠️ 必须用第二份夹具：「被执行的那一下」会改局面，而 ①② 要在**未执行**的局面上量。
    //
    //  ⚠️ `card.Type != "unit"`（战术 / 督军 / 防御）时**不摆盘**（那不是这个档的用法），照旧放手里并出声。
    //  ⛔ 判读仍归人 / 调度台：本档只摆数，不返回红绿（与 `ai` / `actions` 同口径）。
    // ==================================================================
    public static int RunBoard(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            Console.Error.WriteLine("!! 用法：ai board <卡id|卡名>");
            return 1;
        }
        var pool = Program.PoolList;
        var card = Find(pool, spec);
        if (card == null)
        {
            Console.Error.WriteLine("!! 卡池里找不到：" + spec);
            return 1;
        }

        BattleContext ctx;
        UnitState placed;
        try { ctx = BuildBoardFixture(pool, card, out placed); }
        catch (Exception ex)
        {
            Console.Error.WriteLine("!! 建夹具时引擎抛异常：" + ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
        int me = ctx.Active;
        int slot = placed != null ? BoardSlotOf(ctx, me, placed) : -1;

        Console.WriteLine("=== ai board " + card.Id + " \"" + card.Name + "\" · 卡在**盘上**（`B88_E3`）===");
        Console.WriteLine("[夹具] ctx.Active=" + me
                          + " · 被测卡 " + (placed != null ? "P" + me + "[" + slot + "]" : "**没摆盘**（不是 `unit`）")
                          + " · 友方小兵 1 张（5 号格）· 敌方 2 张（3 / 5 号格）· 能量 "
                          + ctx.Players[me].Energy + "/" + ctx.Players[me].MaxEnergy);
        if (placed != null)
        {
            Console.WriteLine("[④ 档五道读数] `HasActiveAbility`=" + card.HasActiveAbility
                              + " · `HasAbility`=" + (placed.HasAbility ? 1 : 0)
                              + " · 替代行动=" + (RuleCore.AlternativeActionKeyword(placed) ?? "-")
                              + " · 誓约 ops=" + card.OathOps.Count
                              + " · `CanStartActiveAbility`=" + RuleCore.CanStartActiveAbility(ctx, me, slot)
                              + " · `CanUseActiveAbility(-1)`=" + RuleCore.CanUseActiveAbility(ctx, me, slot, -1));
        }

        // ---- ① 动作表 + 逐条分数 ----
        var list = SimpleAI.EnumerateActions(ctx);
        Console.WriteLine("[① 动作表] 共 **" + list.Count + "** 条");
        var byKind = new Dictionary<string, int>();
        var entry = new List<AiAction>();
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            a.Score = SimpleAI.ScoreAction(ctx, a);       // 产品自己也这么写回（见 `RunActions` 的注释）
            string kn = KindName(a.Kind);
            byKind[kn] = (byKind.TryGetValue(kn, out int c0) ? c0 : 0) + 1;
            if (a.Kind == AiActionKind.ActiveAbility && a.AltKeyword == SimpleAI.AltActiveAbility) entry.Add(a);
            Console.WriteLine("  [" + i.ToString().PadLeft(3) + "] " + Pad(kn, 15)
                            + " slot=" + (a.Slot >= 0 ? a.Slot.ToString() : "-").PadLeft(2)
                            + " →P" + (a.TargetP >= 0 ? a.TargetP.ToString() : "-")
                            + "@" + (a.TargetSlot >= 0 ? a.TargetSlot.ToString() : "-").PadLeft(2)
                            + " score=" + F(a.Score)
                            + (a.AltKeyword != null ? "  alt=" + a.AltKeyword : ""));
        }
        Console.WriteLine("[按 Kind 计数] " + string.Join(" · ",
            byKind.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)));

        // ---- ② 第 ④ 档候选 + 打分的逐条贡献 ----
        Console.WriteLine("[② 第 ④ 档候选] 共 **" + entry.Count + "** 条"
                          + (entry.Count == 0 ? "（🔴 0 条 = 这一档**没进动作表**）" : ""));
        for (int i = 0; i < entry.Count; i++)
        {
            var lines = new List<string>();
            AiScoring.TraceCriteria = lines.Add;
            float sc;
            try { sc = SimpleAI.ScoreAbility(ctx, entry[i]); }
            finally { AiScoring.TraceCriteria = null; }
            Console.WriteLine("  [④-" + i + "] slot=" + entry[i].Slot
                              + " →P" + entry[i].TargetP + "@" + entry[i].TargetSlot
                              + " score=" + F(sc));
            foreach (var l in lines) Console.WriteLine("        " + l);
            if (lines.Count == 0)
                Console.WriteLine("        （`TraceCriteria` 没有输出 ⇒ 这张卡**没有 `ai_scoring` 的 `activeAbility` 条目**："
                                  + "原版那一支也只打 warning + 0 分，⛔ 别当成「分算错了」）");
            // 🔴 **离屏桩的口径差（如实标）**：`SimpleAI.ScoreAbility` 走 `AiScoringData.Get(card)`，而
            //   `AiScoringData.EnsureLoaded` 用的是 `Resources.Load<TextAsset>` —— 离屏是**桩、恒 null**
            //   ⇒ 产品那条路在这里退化成 `ScoreOps(TriggerOps("active"))` = **恒 0**（不是打分错了）。
            //   下面这一行**显式把同一份 dto 喂给同一个函数** `AiScoring.ScoreActiveAbility`（产品在 Unity 里
            //   走的就是这一支，只是它自己从 `Resources/ai_scoring.json` 里取 dto）⇒ 这才是**真实分数**的读数。
            var pdto = LoadScoring(card.Id);
            if (pdto != null && pdto.activeAbility != null && placed != null)
            {
                UnitState tgt = null;
                if (entry[i].TargetSlot >= 0 && entry[i].TargetP >= 0 && entry[i].TargetP <= 1)
                    tgt = ctx.Players[entry[i].TargetP].Board[entry[i].TargetSlot];
                var lines2 = new List<string>();
                AiScoring.TraceCriteria = lines2.Add;
                float sc2;
                try { sc2 = AiScoring.ScoreActiveAbility(ctx, card, placed.Instance, me, tgt, pdto.activeAbility); }
                finally { AiScoring.TraceCriteria = null; }
                Console.WriteLine("        [显式喂 dto] `AiScoring.ScoreActiveAbility` = " + F(sc2)
                                  + "  ← **Unity 里 AI 真正看见的那个分**（同一个函数、同一份 `ai_scoring.json`）");
                foreach (var l in lines2) Console.WriteLine("          " + l);
                Console.WriteLine("        ⚠️ 上面那行 `SimpleAI.ScoreAbility` 的值是 **0** 属**离屏桩口径**："
                                  + "`AiScoringData` 靠 `Resources.Load` 装载，离屏恒 null ⇒ 退化成 `ScoreOps` 兜底（恒 0）");
            }
        }

        // ---- ③ AI 自己挑哪条 ----
        try
        {
            var best = SimpleAI.SelectBestAction(ctx, new List<AiAction>(list));
            Console.WriteLine("[③ SelectBestAction] " + (best == null ? "（null）"
                : KindName(best.Kind) + " slot=" + best.Slot
                  + (best.TargetSlot >= 0 ? " →P" + best.TargetP + "@" + best.TargetSlot : "")
                  + (best.AltKeyword != null ? " alt=" + best.AltKeyword : "")
                  + " = " + F(SimpleAI.ScoreAction(ctx, best))));
            Console.WriteLine("     " + (best != null && best.AltKeyword == SimpleAI.AltActiveAbility
                ? "✅ **AI 挑的就是第 ④ 档**（这条就是「AI 会不会用它」的直接读数）"
                : "（AI 挑了别的一条 —— 那不是缺陷：④ 档只是**在表里可选**；它的结算由 ③b 单独验）"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("[③ SelectBestAction] 🔴 抛异常：" + ex.GetType().Name + ": " + ex.Message);
        }

        // ---- ③c 若 `AiScoringData` 在位（= Unity 里），④ 档会不会赢过其余动作 ----
        //   ⚠️ 这一行是**由读数推出来**的比较，⛔ 不是离屏跑了一遍 AI 决策：`SelectBestAction` 在离屏用的是
        //   `SimpleAI.ScoreAction`，而那条路因为 `AiScoringData` 恒 null 会把 ④ 档算成 0。
        {
            float bestOther = float.NegativeInfinity;
            string bestOtherName = "（无）";
            foreach (var a in list)
            {
                if (a.Kind == AiActionKind.EndTurn) continue;
                if (a.Kind == AiActionKind.ActiveAbility && a.AltKeyword == SimpleAI.AltActiveAbility) continue;
                if (a.Score > bestOther) { bestOther = a.Score; bestOtherName = KindName(a.Kind); }
            }
            var pd = LoadScoring(card.Id);
            if (pd != null && pd.activeAbility != null && placed != null && entry.Count > 0)
            {
                float bestE = float.NegativeInfinity;
                int bestEt = -1;
                for (int i = 0; i < entry.Count; i++)
                {
                    UnitState t = null;
                    if (entry[i].TargetSlot >= 0 && entry[i].TargetP >= 0 && entry[i].TargetP <= 1)
                        t = ctx.Players[entry[i].TargetP].Board[entry[i].TargetSlot];
                    float v = AiScoring.ScoreActiveAbility(ctx, card, placed.Instance, me, t, pd.activeAbility);
                    if (v > bestE) { bestE = v; bestEt = entry[i].TargetSlot; }
                }
                Console.WriteLine("[③c 若 dto 在位（= Unity 里）] ④ 档最高 = " + F(bestE) + "（目标格 " + bestEt
                                  + "） vs 其余动作最高 = " + F(bestOther) + "（" + bestOtherName + "）"
                                  + " ⇒ " + (bestE > bestOther
                                      ? "**AI 会挑 ④ 档**（在同样这份局面上）"
                                      : "其余动作更高 ⇒ AI 不挑它（**不是缺陷**：它只是可选）"));
            }
        }

        // ---- ③b 强制结算最高分的 ④ 档候选（**另一份同 seed 夹具**，局面逐位相同）----
        if (entry.Count == 0)
        {
            Console.WriteLine("[③b 实打] 跳过（动作表里没有第 ④ 档候选）");
        }
        else
        {
            BattleContext c2;
            UnitState u2;
            try { c2 = BuildBoardFixture(pool, card, out u2); }
            catch (Exception ex)
            {
                Console.WriteLine("[③b 实打] 🔴 建第二份夹具抛异常：" + ex.GetType().Name + ": " + ex.Message);
                return 0;
            }
            int slot2 = u2 != null ? BoardSlotOf(c2, c2.Active, u2) : -1;
            AiAction pick = null;
            float bestSc = float.NegativeInfinity;
            foreach (var a in SimpleAI.EnumerateActions(c2))
            {
                if (a.Kind != AiActionKind.ActiveAbility || a.AltKeyword != SimpleAI.AltActiveAbility) continue;
                float sc = SimpleAI.ScoreAction(c2, a);
                if (sc > bestSc) { bestSc = sc; pick = a; }
            }
            if (pick == null)
            {
                Console.WriteLine("[③b 实打] 🔴 第二份夹具里没有 ④ 档候选（**不稳定**，如实报出来）");
                return 0;
            }
            Console.WriteLine("[③b 实打] 强制 `SimpleAI.ExecuteAction` 最高分那条 ④ 档：slot=" + pick.Slot
                              + " →P" + pick.TargetP + "@" + pick.TargetSlot + " score=" + F(bestSc));
            Console.WriteLine("   前：" + BoardLine(c2, pick.Slot));
            bool ok;
            try { ok = SimpleAI.ExecuteAction(c2, pick); }
            catch (Exception ex)
            {
                Console.WriteLine("   🔴 抛异常：" + ex.GetType().Name + ": " + ex.Message);
                return 0;
            }
            Console.WriteLine("   ⇒ `ExecuteAction` = **" + (ok ? "True" : "False") + "**");
            Console.WriteLine("   后：" + BoardLine(c2, pick.Slot));
            Console.WriteLine("   事件数 ctx.Events = " + c2.Events.Count);
            Console.WriteLine("   " + (ok
                ? "✅ 读数：结算**成功**（`RuleCore.UseActiveAbility` 回 `OK`）"
                : "🔴 读数：结算**失败** —— 看上面 `CanUseActiveAbility` 的码与引擎那侧出声（⛔ 不猜原因）"));
        }
        Console.WriteLine("=== 结束（本模式只读、不返回红绿；判据 `判据/AI打分与动作选择_第2轮.md` ＋ `报告/A9_主动能力按钮.md`）===");
        return 0;
    }

    static int BoardSlotOf(BattleContext ctx, int p, UnitState u)
    {
        for (int i = 0; i < BoardSpec.Size; i++)
            if (ReferenceEquals(ctx.Players[p].Board[i], u)) return i;
        return -1;
    }

    /// <summary>一行局面读数（能量 / 手牌数 / 双方场上单位）—— 用来对「执行前 vs 执行后」。</summary>
    static string BoardLine(BattleContext ctx, int slot)
    {
        int me = ctx.Active;
        var sb = new System.Text.StringBuilder();
        sb.Append("能量 P").Append(me).Append('=').Append(ctx.Players[me].Energy)
          .Append(" · 手牌 P").Append(me).Append('=').Append(ctx.Players[me].Hand.Count);
        for (int p = 0; p < 2; p++)
        {
            sb.Append(" · P").Append(p).Append(": ");
            bool first = true;
            for (int i = 0; i < BoardSpec.Size; i++)
            {
                var u = ctx.Players[p].Board[i];
                if (u == null) continue;
                if (!first) sb.Append(" | ");
                first = false;
                sb.Append('[').Append(i).Append(']').Append(u.Name)
                  .Append(' ').Append(u.Attack).Append('/').Append(u.RangedAttack)
                  .Append('/').Append(u.Health).Append(u.Exhausted ? " 已行动" : "");
                if (ReferenceEquals(u, ctx.Players[me].Board[slot])) sb.Append(" ←被测");
            }
        }
        return sb.ToString();
    }

    /// <summary>`ai board` 的夹具：**被测卡直接摆上棋盘**（不走部署链，与 `BuildActionsFixture` 同一口径）。
    /// <para>槽位：被测卡 = `ProbeSlot`(3)；友方小兵 = 5；敌方 = 3(无甲) / 5(2 甲)；督军在 4。</para>
    /// <para>⚠️ `Exhausted = false` 是**故意**的：直接摆上去的单位默认「本回合已行动」⇒ 第 ④ 档与攻击行都会消失
    /// （那是量具的错，不是引擎）。</para></summary>
    static BattleContext BuildBoardFixture(List<CardDef> pool, CardDef card, out UnitState placed)
    {
        var heroP0 = new CardDef("AVBHERO0", "AVBHERO0", "hero", "", "common", "Test", 0, 2, 30, 0, null);
        var heroP1 = new CardDef("AVBHERO1", "AVBHERO1", "hero", "", "common", "Test", 0, 2, 30, 0, null);
        var filler = new CardDef("AVBFILL", "AVBFILL", "unit", "", "common", "Test", 1, 0, 1, 0, null);
        var mate = new CardDef("AVBMATE", "AVBMATE", "unit", "", "common", "Test", 2, 2, 5, 0, null);
        var foeCard = new CardDef("AVBFOE", "AVBFOE", "unit", "", "common", "Test", 3, 2, 8, 0, null);
        var armCard = new CardDef("AVBFOE_ARM", "AVBFOE_ARM", "unit", "", "common", "Test", 3, 2, 8, 0,
                                  new[] { "Armour 2" });

        var d0 = new List<CardDef> { heroP0 };
        var d1 = new List<CardDef> { heroP1 };
        for (int i = 0; i < 12; i++) { d0.Add(filler); d1.Add(filler); }

        var vars = GameplayVariables.Classic.Clone();
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);
        int me = ctx.Active;
        ctx.Players[me].Energy = 20;
        ctx.Players[me].MaxEnergy = 20;

        placed = null;
        if (card.Type == "unit")
        {
            placed = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
            placed.DeployedTurn = ctx.Turn;
            placed.PlayOrder = ctx.TakePlayOrder();
            ctx.Players[me].Board[ProbeSlot] = placed;
        }
        else
        {
            // 非 unit（战术 / 督军 / 防御）：照旧放手里，让动作表至少能看见它自己的出牌行。
            var inst = ctx.NewInstance(card);
            ctx.Players[me].Hand.Add(inst);
            RuleCore.SetupCardInHand(ctx, me, inst);
        }

        var m1 = new UnitState(ctx.NewInstance(mate), false) { Exhausted = false };
        m1.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[me].Board[FoeArmSlot] = m1;            // 5 号格（友方小兵，给「友方目标」用）

        int foe = 1 - me;
        var f1 = new UnitState(ctx.NewInstance(foeCard), false);
        var f2 = new UnitState(ctx.NewInstance(armCard), false);
        f1.PlayOrder = ctx.TakePlayOrder();
        f2.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[foe].Board[FoePlainSlot] = f1;
        ctx.Players[foe].Board[FoeArmSlot] = f2;
        return ctx;
    }

    /// <summary>`AiActionKind` 的**原版名字**（取值照 `PlayerActions`，见 `AiActionKind` 的 doc）。
    /// ⚠️ 打的是**原版枚举名**而不是 C# 名 —— 读数要能直接对反编译/判据里的词。</summary>
    static string KindName(AiActionKind k)
    {
        switch (k)
        {
            case AiActionKind.PlayCard: return "playCard(0)";
            case AiActionKind.AttackMelee: return "attackMelee(1)";
            case AiActionKind.AttackRanged: return "attackRanged(2)";
            case AiActionKind.ActiveAbility: return "activeAbility(5)";
            case AiActionKind.CollectWaystone: return "clickWaystone(6)";
            case AiActionKind.EndTurn: return "endTurn(4)";
            default: return k.ToString();
        }
    }

    static string Pad(string s, int n) { return (s == null ? "" : s).PadRight(n); }

    /// <summary>`ai actions` 的夹具 —— 与打分档（<see cref="BuildFixture"/>）**刻意不同**：
    /// 这里要的是「**被测卡在手牌里**、棋盘上有能出手的单位」⇒ 动作表里既该有 `PlayCard` 行、
    /// 也该有 `attackMelee/Ranged` 行。
    /// ⚠️ 与打分档一样**直接摆棋盘**（不走 `PlayCard` 部署链）—— 这是既有口径，⛔ 本次不动它。
    /// ⚠️ 探针单位必须 `Exhausted = false`：`UnitState` 构造时是 `Exhausted = !HasDeployExemption(...)`，
    ///    直接摆上去的单位默认「本回合已行动」⇒ **攻击行一条都不会出现**（那是量具的错，不是引擎）。
    /// </summary>
    static BattleContext BuildActionsFixture(List<CardDef> pool, CardDef card, out UnitState probe)
    {
        var heroP0 = new CardDef("AVHERO0", "AVHERO0", "hero", "", "common", "Test", 0, 2, 30, 0, null);
        var heroP1 = new CardDef("AVHERO1", "AVHERO1", "hero", "", "common", "Test", 0, 2, 30, 0, null);
        var filler = new CardDef("AVFILL", "AVFILL", "unit", "", "common", "Test", 1, 0, 1, 0, null);
        var plain = new CardDef("AVPROBE", "AVPROBE", "unit", "", "common", "Test", 1, 3, 6, 0, null);
        var foeCard = new CardDef("AVFOE", "AVFOE", "unit", "", "common", "Test", 3, 2, 8, 0, null);
        var armCard = new CardDef("AVFOE_ARM", "AVFOE_ARM", "unit", "", "common", "Test", 3, 2, 8, 0,
                                  new[] { "Armour 2" });

        var d0 = new List<CardDef> { heroP0 };
        var d1 = new List<CardDef> { heroP1 };
        for (int i = 0; i < 12; i++) { d0.Add(filler); d1.Add(filler); }

        var vars = GameplayVariables.Classic.Clone();
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);
        int me = ctx.Active;
        // 能量给足（否则被测卡会因为 `ErrCost` 从动作表里消失 —— 那是夹具没给钱，不是闸）
        ctx.Players[me].Energy = 20;
        ctx.Players[me].MaxEnergy = 20;

        // 被测卡**放进手牌**（形状同 `play` 的补牌那一跳：`NewInstance` → `Hand.Add` → `SetupCardInHand`）
        if (card.Type != "hero")
        {
            var inst = ctx.NewInstance(card);
            ctx.Players[me].Hand.Add(inst);
            RuleCore.SetupCardInHand(ctx, me, inst);
        }

        probe = new UnitState(ctx.NewInstance(plain), false);
        probe.PlayOrder = ctx.TakePlayOrder();
        probe.Exhausted = false;                      // ← 不设的话攻击行一条都没有（见函数头）
        ctx.Players[me].Board[ProbeSlot] = probe;

        int foe = 1 - me;
        var f1 = new UnitState(ctx.NewInstance(foeCard), false);
        var f2 = new UnitState(ctx.NewInstance(armCard), false);
        f1.PlayOrder = ctx.TakePlayOrder();
        f2.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[foe].Board[FoePlainSlot] = f1;
        ctx.Players[foe].Board[FoeArmSlot] = f2;
        return ctx;
    }
}
