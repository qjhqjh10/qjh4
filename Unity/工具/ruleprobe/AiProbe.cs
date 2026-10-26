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
        if (string.IsNullOrWhiteSpace(spec))
        {
            Console.Error.WriteLine("!! 用法：ai <卡id|卡名>    |    ai actions <卡id|卡名>");
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
