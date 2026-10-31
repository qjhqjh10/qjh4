// EffectsProbe.cs — `effects <卡id|卡名>` / `effects all [dump <tsv>]`：**效果投影**（原版
// `EntityScript.GetCardEffectsToShow` → 我们 `EffectsToShow.Of`）的读数口。
//
// 🔴 **它修的是哪一类【结构性失明】**（2026-11-01 · 第 111 会话 · 执行代理 `B111_E3`）：
//   账 `特效动画:521` 的残差 S7 写着「今天没有任何机器读数口能看见这条链」——
//   `play all dump` 是结算层（打了之后数字对不对），`scan` / `layers` 是**解析层**（正文认成什么 op），
//   而「**这张卡的记录最终投影成几行、有没有超过 5 槽被截断、哪一行是洞**」这三件事
//   **两个口都照不到**。本模式补的就是这个口。
//
// 判据（逐条）：
//   · `资料/完全复刻/判据/B110_G5A_§2复核.md` 的 `N19` 行（S7 = 唯一剩下的那一条）
//   · `资料/完全复刻/报告/B101_R7_效果行消费端.md` §五 Step 5（输出列的原始定义）
//   · 投影本身的口径 = `MyGame/Assets/RuleEngine/Core/EffectsToShow.cs` 文件头（原版 `.c` 第一权威）
//
// ⚠️ **它不判对错**：读数摆出来，判决归人 / 调度台（与 `play` / `ai` 同一条纪律）。
// ⛔ **它不改引擎**：`MyGame/Assets/**` 一个字节都不动；只读卡池、建夹具、打牌、读投影。
// ⛔ **不影响既有输出**：不碰基线、不碰三个指纹、不改 `check` / `play` / `ai` 的任何一行。
//
// 用法：
//   bash d:/4/Unity/工具/ruleprobe.sh effects SOR49          # 单卡：解析结果 + 投影逐行
//   bash d:/4/Unity/工具/ruleprobe.sh effects "Fire Dragon"  # 按卡名
//   bash d:/4/Unity/工具/ruleprobe.sh effects all            # 全池扫描（只打分布）
//   bash d:/4/Unity/工具/ruleprobe.sh effects all dump D:/4/_tmp_view/effects_all.tsv
//
// 退出码：单卡 = 参数错 / 打不出去 ⇒ rc≠0（1 = 夹具或引擎抛异常，2 = 用法/找不到卡）；
// `all` = 池里**每一张都抛异常**这种灾难才 rc=1，正常 rc=0（读数而已）。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RuleEngine;

class EffectsProbe
{
    // ── 夹具常数（与 `PlayProbe` 的**默认夹具**同源；⛔ 别在这里发明新数值）────────────────────
    //   ⚠️ 这几个数**刻意抄自** `PlayProbe`（`:45/50/109/110/111`），目的是让「同一张卡在
    //      `play` 与 `effects` 两个口上看到的是同一个局面」。改一边不改另一边会让两份读数对不上，
    //      而且**不会报错**（同 `A1911` / `A1943` 那一族「探针与引擎口径不同」）。
    const int FixtureEnergy = 20;
    const int FixtureMaxEnergy = 30;
    const int FixtureFaith = 12;
    const int FixtureSpirit = 5;
    const int FixtureQuestpoints = 0;
    const int FillerCount = 20;
    /// <summary>把我方单位各扣 2 血 —— 满血治疗/「若受伤则…」那一族才有可见效果（同 `PlayProbe.FixtureChip`）。</summary>
    const int ChipDamage = 2;
    /// <summary>原版「受到以下影响」表的槽数上限 —— **超过它的行在真机上被截断**（判据 `B101_R7` §七·4）。</summary>
    const int SlotLimit = 5;

    /// <summary>摆场格（同 `PlayProbe.DeploySlots`：贴着督军格 4、且有相邻对 6-7）。</summary>
    static readonly int[] DeploySlots = { 3, 6, 7 };

    /// <summary>垫牌兵种（轮着来）—— `Draw a Beast` 那一族要有对应兵种才可能有效果。</summary>
    static readonly string[] FillerSubtypes = { "Infantry", "Vehicle", "Beast", "Drone" };

    // ==================================================================
    //  入口
    // ==================================================================

    /// <summary>`effects &lt;卡id|卡名&gt;` · `effects all [dump &lt;out.tsv&gt;]`。
    /// 返回退出码（见文件头）。</summary>
    public static int Run(string spec, string a2 = null, string a3 = null)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            Usage();
            return 2;
        }
        if (string.Equals(spec, "all", StringComparison.OrdinalIgnoreCase))
        {
            string dump = null;
            if (a2 != null)
            {
                if (!string.Equals(a2, "dump", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(a3))
                {
                    Console.Error.WriteLine("!! `effects all` 的第二参只认 `dump <out.tsv>`（拿到的是「" + a2 + "」"
                                            + (a3 == null ? "" : " + 「" + a3 + "」") + "）");
                    Usage();
                    return 2;
                }
                dump = a3;
            }
            return Sweep(dump);
        }
        return One(spec);
    }

    static void Usage()
    {
        Console.Error.WriteLine("用法：effects <卡id|卡名>");
        Console.Error.WriteLine("      effects all [dump <out.tsv>]");
        Console.Error.WriteLine("说明：逐条打印「这张卡的效果树被解析成什么」＋「打出去之后，各单位的效果投影成哪几行」；");
        Console.Error.WriteLine("      前 5 行标 [槽 i]、附魔那族标 [洞]、第 6 行起标 [截断]（原版 5 槽上限）。");
        Console.Error.WriteLine("      ⚠️ 它不判对错，只摆读数（同 `play` / `ai` 的口径）。");
    }

    // ==================================================================
    //  单卡
    // ==================================================================

    static int One(string spec)
    {
        var pool = Program.PoolList;
        var card = Find(pool, spec);
        if (card == null)
        {
            Console.Error.WriteLine("!! 卡池里找不到：" + spec);
            return 2;
        }

        Console.WriteLine("=== effects " + card.Id + " \"" + card.Name + "\" ===");
        // ⚠️ **按名字找会取「池里排在前面的那一张」** —— 池内现读有同名组（`CardDatabase` 的
        //   `BuildSourceCardIndex` 也把它当「判不了」）。这里**出声**，⛔ 不静默挑一张。
        if (!string.Equals(card.Id, spec, StringComparison.OrdinalIgnoreCase))
        {
            var same = new List<string>();
            foreach (var c in pool)
                if (c != null && string.Equals(c.Name, card.Name, StringComparison.OrdinalIgnoreCase)) same.Add(c.Id);
            if (same.Count > 1)
                Console.WriteLine("[⚠️ 同名] 池内有 **" + same.Count + "** 张都叫「" + card.Name + "」：" + string.Join(",", same)
                                  + " ⇒ 本次取 **" + card.Id + "**（按池序第一个）。要另一张请直接给 id。");
        }
        Console.WriteLine("[卡] type=" + card.Type + " cost=" + card.Cost + " atk=" + card.Attack
                          + " ranged=" + card.RangedAttack + " hp=" + card.Health
                          + " faction=" + (string.IsNullOrEmpty(card.Faction) ? "-" : card.Faction));
        Console.WriteLine("[正文] " + (string.IsNullOrEmpty(card.Desc) ? "（空）" : card.Desc));
        DumpParsed(card);

        if (string.Equals(card.Type, "hero", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[跳过] `hero` 不从手牌打出 ⇒ 本命令不做投影（督军档不在这里量）。");
            return 0;
        }

        BattleContext ctx;
        int handIdx;
        bool injected;
        try
        {
            ctx = BuildFixture(pool, card, out handIdx, out injected);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[夹具] !! 建夹具时引擎抛异常：" + ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
        if (handIdx < 0)
        {
            Console.WriteLine("[夹具] !! **测试卡没接住**：它既不在手牌里、注入也没成功 ⇒ 下面的投影**不代表这张卡**。");
            Console.WriteLine("        手牌 = [" + string.Join(",", ctx.Players[0].Hand.Select(x => x.Card.Id)) + "]");
            return 1;
        }
        Console.WriteLine("[夹具] 我方棋盘 " + BoardLine(ctx.Players[0])
                          + " · 敌方棋盘 " + BoardLine(ctx.Players[1])
                          + " · 能量 " + ctx.Players[0].Energy + "/" + ctx.Players[0].MaxEnergy
                          + " · 被测卡手牌下标 " + handIdx + (injected ? "（**注入的**：卡组分流里没有它，照引擎 `SetupCardInHand` 补进来）" : ""));

        var before = Snap(ctx);
        string how;
        int rc, landed = -1;
        try { rc = PlayIt(ctx, card, handIdx, out how, out landed); }
        catch (Exception ex)
        {
            Console.WriteLine("[打牌] !! 引擎抛异常：" + ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
        var after = Snap(ctx);
        Console.WriteLine("[打牌] " + how + " ⇒ rc=" + rc + (rc == 0 ? "（OK）" : "（**打不出去**，原因见 `RuleCodes`）"));
        if (rc != 0)
            Console.WriteLine("        ⚠️ 打不出去 ⇒ 下面的投影只能说明「夹具里这一张没生效」，⛔ 不能当「这张卡没有效果」。");

        // ── 投影 ────────────────────────────────────────────────────────────────
        int changed = 0, maxRows = 0;
        var order = new List<string>(after.Keys);
        order.Sort(StringComparer.Ordinal);
        var sb = new StringBuilder();
        foreach (var k in order)
        {
            List<ShownEffect> a, b;
            after.TryGetValue(k, out a);
            if (!before.TryGetValue(k, out b)) b = new List<ShownEffect>();
            if (a == null) continue;
            if (a.Count > maxRows) maxRows = a.Count;
            bool diff = a.Count != b.Count || !SameRows(a, b);
            if (!diff) continue;
            changed++;
            sb.AppendLine("  ── " + KeyLabel(k, ctx) + "：" + b.Count + " → " + a.Count + " 行"
                          + (a.Count > SlotLimit ? "  🔴 **超过 " + SlotLimit + " 槽 ⇒ 真机会截断**" : ""));
            DumpRows(sb, a);
        }
        Console.WriteLine("[投影] 有变化的单位 = " + changed + " 个 · 全场最大行数 = " + maxRows
                          + (maxRows > SlotLimit ? "  🔴 超过 5 槽" : ""));
        if (changed == 0)
            Console.WriteLine("  （没有任何单位的投影变了 —— 要么这张卡不产生记录，要么它的效果落在本夹具够不到的地方）");
        Console.Write(sb.ToString());
        return rc == 0 ? 0 : 1;
    }

    /// <summary>把「这张卡的效果树被解析成什么」逐条打出来（= `EffectText.Parse` 的产物，⛔ 不重新解析一遍）。</summary>
    static void DumpParsed(CardDef card)
    {
        List<EffectOp> ops;
        List<string> unparsed, partial;
        try
        {
            ops = EffectText.Parse(card.Desc ?? "", out unparsed, out partial, card: card);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[解析] !! `EffectText.Parse` 抛异常：" + ex.GetType().Name + ": " + ex.Message);
            return;
        }
        Console.WriteLine("[解析] `EffectText.Parse(desc, card: this)` ⇒ op **" + ops.Count + "** 条 · unparsed "
                          + unparsed.Count + " 段 · partial " + partial.Count + " 段"
                          + (card.HasAbilityEffectTexts ? " · 这张卡有 `abilityEffectTexts`" : ""));
        for (int i = 0; i < ops.Count; i++)
        {
            var op = ops[i];
            Console.WriteLine("   op[" + i + "] Verb=" + (string.IsNullOrEmpty(op.Verb) ? "-" : op.Verb)
                              + " · Cost=" + op.Cost + (string.IsNullOrEmpty(op.CostKind) ? "" : "(" + op.CostKind + ")")
                              + " · Target=" + TargetText(op.Target)
                              + " · Target2=" + TargetText(op.Target2));
        }
        if (unparsed.Count > 0) Console.WriteLine("   ⚠️ unparsed（整段没认出来）：" + string.Join(" ‖ ", unparsed));
        if (partial.Count > 0) Console.WriteLine("   ⚠️ partial（部分没认出来）：" + string.Join(" ‖ ", partial));
    }

    static string TargetText(EffectTargetSpec t)
    {
        if (t == null) return "-";
        return (string.IsNullOrEmpty(t.Side) ? "?" : t.Side) + "/"
             + (string.IsNullOrEmpty(t.Kind) ? "?" : t.Kind)
             + " ×" + t.Count
             + (t.Random ? " random" : "")
             + (t.Adjacent ? " adjacent" : "");
    }

    // ==================================================================
    //  全池
    // ==================================================================

    /// <summary>全池扫一遍：每张卡建夹具 → 打出去 → 量「任何一个单位上最多投影出几行」。
    /// 用途 = 回答 `B101_R7` §七·4 那个未测项「池内有没有 >5 条（会触发截断）」。</summary>
    static int Sweep(string dumpPath)
    {
        var pool = Program.PoolList;
        if (pool == null || pool.Count == 0) { Console.Error.WriteLine("!! 卡池是空的"); return 1; }

        var rows = new List<string>();
        rows.Add("id\ttype\trc\tmaxRows\tover5\tmaxWhere\tinjected\tnote");

        var hist = new SortedDictionary<int, int>();
        int nPlayable = 0, nFail = 0, nOver5 = 0, nCrash = 0;
        var over5Ids = new List<string>();
        var failReasons = new Dictionary<int, int>();

        foreach (var card in pool)
        {
            if (card == null) continue;
            if (string.Equals(card.Type, "hero", StringComparison.OrdinalIgnoreCase))
            {
                rows.Add(card.Id + "\thero\t-\t-\t0\t-\t0\t(hero 不从手牌打出 ⇒ 不计)");
                continue;
            }
            int rc = -99, maxRows = 0;
            string where = "-", note = "";
            bool injected = false;
            try
            {
                int handIdx;
                var ctx = BuildFixture(pool, card, out handIdx, out injected);
                if (handIdx < 0) { note = "夹具没接住"; rc = -98; }
                else
                {
                    string how; int landed;
                    rc = PlayIt(ctx, card, handIdx, out how, out landed);
                    var after = Snap(ctx);
                    foreach (var kv in after)
                    {
                        if (kv.Value != null && kv.Value.Count > maxRows) { maxRows = kv.Value.Count; where = KeyLabel(kv.Key, ctx); }
                    }
                }
            }
            catch (Exception ex)
            {
                nCrash++;
                rc = -97;
                note = ex.GetType().Name + ": " + ex.Message;
            }
            if (rc == 0) nPlayable++;
            else if (rc != -99)
            {
                nFail++;
                int k = rc;
                int c; failReasons.TryGetValue(k, out c); failReasons[k] = c + 1;
            }
            if (maxRows > SlotLimit) { nOver5++; over5Ids.Add(card.Id); }
            int h; hist.TryGetValue(maxRows, out h); hist[maxRows] = h + 1;
            rows.Add(card.Id + "\t" + card.Type + "\t" + rc + "\t" + maxRows + "\t" + (maxRows > SlotLimit ? "1" : "0")
                     + "\t" + where + "\t" + (injected ? "1" : "0") + "\t" + note.Replace('\t', ' '));
        }

        Console.WriteLine("=== effects all · 全池 " + pool.Count + " 张 ===");
        Console.WriteLine("[能打出] " + nPlayable + " 张 · [打不出去/夹具没接住] " + nFail + " 张 · [抛异常] " + nCrash + " 张");
        if (failReasons.Count > 0)
        {
            Console.WriteLine("[打不出去的 rc 分布]");
            foreach (var kv in failReasons.OrderByDescending(x => x.Value))
                Console.WriteLine("   rc=" + kv.Key + " × " + kv.Value);
        }
        Console.WriteLine("[投影行数分布]（任何单位上的**最大**行数）");
        foreach (var kv in hist)
            Console.WriteLine("   " + kv.Key + " 行 → " + kv.Value + " 张" + (kv.Key > SlotLimit ? "  🔴 **超过 5 槽 ⇒ 真机会截断**" : ""));
        Console.WriteLine("[超过 " + SlotLimit + " 槽的卡] " + nOver5 + " 张"
                          + (nOver5 > 0 ? "：" + string.Join(",", over5Ids) : ""));

        if (!string.IsNullOrEmpty(dumpPath))
        {
            try
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(dumpPath));
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllLines(dumpPath, rows, new UTF8Encoding(false));
                Console.WriteLine("[dump] 写了 " + dumpPath + "（" + rows.Count + " 行，含表头）");
            }
            catch (Exception ex)
            {
                // ⛔ 不静默：写不出去要说出来（本仓红线），但读数本身已经打完了 ⇒ 不改 rc。
                Console.Error.WriteLine("!! dump 写失败：" + ex.GetType().Name + ": " + ex.Message);
            }
        }
        return nCrash > 0 && nPlayable == 0 ? 1 : 0;
    }

    // ==================================================================
    //  投影快照 / 打印
    // ==================================================================

    /// <summary>键 = <c>s{侧}:W</c>（督军）或 <c>s{侧}:b{格}</c>。</summary>
    static Dictionary<string, List<ShownEffect>> Snap(BattleContext ctx)
    {
        var d = new Dictionary<string, List<ShownEffect>>(StringComparer.Ordinal);
        for (int s = 0; s < 2; s++)
        {
            var w = ctx.Players[s].Warlord;
            if (w != null) d["s" + s + ":W"] = EffectsToShow.Of(w, s == 0, ctx);
            for (int b = 0; b < BoardSpec.Size; b++)
            {
                var u = ctx.Players[s].Board[b];
                // ⚠️ **督军在棋盘上也有一个格**（`s0:b4`）—— 上面已经用 `s{侧}:W` 打了它，
                //   这里必须跳过，否则同一个单位会被算两次（读数里「有变化的单位 = N」会虚高，
                //   而且是**静默**的：两份行看起来都合法）。
                if (u != null && !u.IsWarlord) d["s" + s + ":b" + b] = EffectsToShow.Of(u, s == 0, ctx);
            }
        }
        return d;
    }

    static string KeyLabel(string key, BattleContext ctx)
    {
        // key 形状固定：s{0|1}:{W|b{0..8}}
        int side = key[1] - '0';
        string tail = key.Substring(3);
        if (tail == "W")
        {
            var w = ctx.Players[side].Warlord;
            return key + " 「" + (w != null ? w.Name : "?") + "」" + (side == 0 ? "（我方督军）" : "（敌方督军）");
        }
        int slot;
        if (!int.TryParse(tail.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out slot))
            return key;
        var u = slot >= 0 && slot < BoardSpec.Size ? ctx.Players[side].Board[slot] : null;
        return key + " 「" + (u != null ? u.Name : "（空）") + "」" + (side == 0 ? "（我方）" : "（敌方）");
    }

    static bool SameRows(List<ShownEffect> a, List<ShownEffect> b)
    {
        for (int i = 0; i < a.Count; i++)
        {
            if (i >= b.Count) return false;
            var x = a[i]; var y = b[i];
            if (x.Family != y.Family || x.BuffType != y.BuffType || x.Trait != y.Trait || x.TraitValue != y.TraitValue
                || x.Attr != y.Attr || x.Value != y.Value || x.MarkOfChaosType != y.MarkOfChaosType
                || x.Timed != y.Timed || x.SecretBuff != y.SecretBuff || x.QuestBuff != y.QuestBuff
                || x.Source != y.Source || x.Body != y.Body || x.Seq != y.Seq || x.RowGroup != y.RowGroup
                || x.IsSet != y.IsSet || x.OwnerCard != y.OwnerCard) return false;
        }
        return a.Count == b.Count;
    }

    /// <summary>逐行打印投影（列序照 `B101_R7` §五 Step 5，⚠️ 两个改动见下面那行注释）。</summary>
    static void DumpRows(StringBuilder sb, List<ShownEffect> rows)
    {
        // ⚠️ **与 Step 5 原始列序的两处差异（如实标）**：
        //   ① 加了 `Seq` / `RowGroup` —— S2/S3-c 落地后这两格就是「行序为什么是这样」的唯一解释，
        //      不打出来就没法回答「谁排谁前面」（Step 5 写那份时它们还没接）。
        //   ② `左列` 在模型里就是 `Source`（`ShownEffect.Source` 的 doc 写着「左列（谁给的）」），
        //      所以原定义的 `Source | 左列` 两列在本模型里是**同一格** ⇒ 只打一次，⛔ 不复制一遍冒充两列。
        sb.AppendLine("     i | Family | BuffType | Trait | TraitValue | Attr/Value | Timed | MarkGod | Seq | RowGroup | 槽 | 左列(Source) | 右列");
        for (int i = 0; i < rows.Count; i++)
        {
            var e = rows[i];
            string slotMark = i < SlotLimit ? "[槽 " + i + "]" : "[截断]";
            if (e.Family == ShownFamily.Enchantment) slotMark += "·[洞]";
            string right;
            if (!string.IsNullOrEmpty(e.Body)) right = e.Body;
            else if (e.Family == ShownFamily.Enchantment) right = "（空 —— 原版这一族右列就是洞）";
            else right = "（表现侧合成）";
            sb.AppendLine("     " + i + " | " + e.Family + " | " + e.BuffType
                          + " | " + (string.IsNullOrEmpty(e.Trait) ? "-" : e.Trait)
                          + " | " + e.TraitValue
                          + " | " + (string.IsNullOrEmpty(e.Attr) ? "-" : e.Attr) + " " + e.Value + (e.IsSet ? " (set)" : "")
                          + " | " + (e.Timed ? "1" : "0")
                          + " | " + e.MarkOfChaosType
                          + " | " + e.Seq
                          + " | " + e.RowGroup
                          + " | " + slotMark
                          + " | " + (string.IsNullOrEmpty(e.Source) ? "-" : e.Source)
                          + " | " + right);
        }
    }

    static string BoardLine(PlayerState p)
    {
        var sb = new StringBuilder();
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = p.Board[s];
            if (u == null) continue;
            if (sb.Length > 0) sb.Append(" · ");
            sb.Append("格" + s + "=" + u.Name);
        }
        return sb.Length == 0 ? "（空）" : sb.ToString();
    }

    // ==================================================================
    //  夹具（形状照 `PlayProbe.BuildFixture`，但**只摆一手** —— 见下面那段注释）
    // ==================================================================

    /// <summary>
    /// 建夹具：**P0 的第一手**（不推进回合）。
    ///
    /// <para>🔴 **为什么不照 `PlayProbe` 打满两回合**：`PlayProbe` 要打满一个来回，是为了让
    /// 「回合末才结算」那一族也照得到；代价是**临时卡（`ephemeral`）会被 `SweepEphemeral` 弃掉**、
    /// 必须再补一段注入。本命令只关心「**这张卡打出去之后投影出几行**」，**不需要跨回合** ⇒
    /// 停在第一手，结构上就不会撞上那个问题（`defence` 仍要注入：`BuildPlayer` 会把
    /// `type == "defence"` 从牌库分流出去，见 `PlayProbe` 同段注释）。</para>
    ///
    /// <para>⚠️ 敌方三张是**直接摆上棋盘**的（不走 `PlayCard`）—— 那需要先切到 P1 的回合、
    /// 也就是要推进回合，与本函数「只摆一手」的口径冲突。形状照 `AiProbe.BuildFixture` 的做法。</para>
    /// </summary>
    static BattleContext BuildFixture(List<CardDef> pool, CardDef card, out int handIdx, out bool injected)
    {
        injected = false;
        var vars = GameplayVariables.Classic.Clone();
        vars.startingHand = 4;      // ⚠️ 只改「发几张」，别的一律照 `Classic`

        // 兵种**故意分散**（`a friendly Vehicle` / `your Beasts` 那一族才找得到目标）。
        var a = Fix("E3FIX_A", 2, 4, 0, "Infantry");
        var b = Fix("E3FIX_B", 3, 5, 0, "Vehicle");
        var c = Fix("E3FIX_C", 1, 4, 0, "Beast");
        var e1 = Fix("E3FIX_E1", 2, 6, 0, "Infantry");
        var e2 = Fix("E3FIX_E2", 3, 6, 0, "Vehicle");
        var e3 = Fix("E3FIX_E3", 1, 6, 0, "Beast");

        // 督军阵营 = 被测卡的真实阵营（原版「造牌没写阵营 ⇒ 按施放者阵营」，见 `PlayProbe` `p0Fac` 那一段）。
        string fac = string.IsNullOrEmpty(card.Faction) ? "P0" : card.Faction;

        var d0 = Deck(fac, new[] { a, b, c, card });
        var d1 = Deck(fac, new[] { e1, e2, e3 });
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);

        SetEnergy(ctx, 0);
        SetFaction(ctx, 0);
        for (int i = 0; i < 3; i++)
        {
            int hi = HandIdx(ctx, 0, i == 0 ? a : (i == 1 ? b : c));
            if (hi >= 0) RuleCore.PlayCard(ctx, 0, hi, DeploySlots[i]);
        }
        // 敌方三张：直接摆盘（见函数头）。
        ctx.Players[1].Board[DeploySlots[0]] = new UnitState(ctx.NewInstance(e1), false);
        ctx.Players[1].Board[DeploySlots[1]] = new UnitState(ctx.NewInstance(e2), false);
        ctx.Players[1].Board[DeploySlots[2]] = new UnitState(ctx.NewInstance(e3), false);

        // 各扣 2 血（满血治疗 / 「若受伤」那一族才有可见效果）。
        //   ⚠️ 走引擎自己的 `ApplyDamage`（⛔ 不直接改 `u.Health`）：它会走盾/闪避/触发那一套。
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[0].Board[s];
            if (u != null && !u.IsWarlord) RuleCore.ApplyDamage(ctx, u, ChipDamage, "夹具扣血");
        }
        RuleCore.ApplyDamage(ctx, ctx.Players[0].Warlord, ChipDamage, "夹具扣血");

        handIdx = HandIdx(ctx, 0, card);
        if (handIdx < 0)
        {
            // 形状照引擎自己的 `RuleCore.SetDefensiveCard`：`NewInstance` → `Hand.Add` → `SetupCardInHand`
            //   （⛔ 不自己拼一个「半份实例」，那会与引擎口径不同）。
            var late = ctx.NewInstance(card);
            ctx.Players[0].Hand.Add(late);
            RuleCore.SetupCardInHand(ctx, 0, late);
            handIdx = HandIdx(ctx, 0, card);
            injected = true;
        }
        return ctx;
    }

    /// <summary>把被测卡打出去。返回 `RuleCodes`（0 = OK）；`out landed` = 落地格（非单位卡恒 -1）。</summary>
    static int PlayIt(BattleContext ctx, CardDef card, int handIdx, out string how, out int landed)
    {
        landed = -1;
        if (string.Equals(card.Type, "enchantment", StringComparison.OrdinalIgnoreCase))
        {
            // 附魔卡**打不出去**（`IsEnchantment ⇒ ErrNotPlayable`）—— 它在真局里唯一的出现方式是
            //   「挂到一张己方部队身上」（口径同 `PlayProbe.BatteryTypes` 那段注释）。
            UnitState carrier = null;
            for (int s = 0; s < BoardSpec.Size && carrier == null; s++)
            {
                var u = ctx.Players[0].Board[s];
                if (u != null && !u.IsWarlord) carrier = u;
            }
            if (carrier == null)
            {
                how = "`Enchantments.Attach` —— **我方棋盘上没有载体** ⇒ 没挂";
                return RuleCodes.ErrNoTargetAvailable;
            }
            ctx.Players[0].Hand.RemoveAt(handIdx);
            var eff = Enchantments.Attach(ctx, carrier.Instance, card, source: card.Name, acting: carrier);
            how = "`Enchantments.Attach`（载体 = 格 " + SlotOf(ctx.Players[0], carrier) + "「" + carrier.Name + "」）";
            return eff == null ? RuleCodes.ErrUnimplemented : 0;
        }
        if (card.IsUnit)
        {
            int slot = FreeSlot(ctx, 0);
            how = "`RuleCore.PlayCard`（落第 " + slot + " 格）";
            return RuleCore.PlayCard(ctx, 0, handIdx, slot, out landed);
        }
        // 战术 / 防御：目标格的口径**照 `PlayProbe.MeasurePlay` 的默认档**（不点定目标，只给格）。
        int tslot = -1;
        List<EffectOp> ops;
        try { ops = EffectText.Parse(card.Desc ?? "", out _, out _, card: card); }
        catch { ops = new List<EffectOp>(); }
        var spec = EffectText.PickTarget(ops);
        if (spec != null) tslot = PickSlot(ctx, spec.Side, spec.Kind);
        how = "`RuleCore.PlayTactic`（目标侧=`" + (spec == null ? "-" : spec.Side) + "` 格=" + tslot + "）";
        return RuleCore.PlayTactic(ctx, 0, handIdx, tslot);
    }

    static int SlotOf(PlayerState p, UnitState u)
    {
        for (int s = 0; s < BoardSpec.Size; s++) if (ReferenceEquals(p.Board[s], u)) return s;
        return -1;
    }

    /// <summary>目标格：`enemy` ⇒ 敌方；其余 ⇒ 我方。**优先取兵种与 `spec.Kind` 相同的那一张**，
    /// 没有同类再退回「第一个非督军单位」；一个都没有 ⇒ -1。
    ///
    /// <para>⚠️ **为什么要看 `Kind`**：`PlayProbe.MeasurePlay` 的默认档（不带 `preset`）**完全不看**目标短语，
    /// 只给「第一个非督军格」—— 那一档对 `play` 是对的（它量的就是「默认夹具下打不打得出去」），
    /// 但本命令量的是「**这张卡的效果树投影成什么**」，给错兵种会让 `Give … to your Vehicles`
    /// 那一族（实测 11 张）直接 `ErrNoTargetAvailable`、读数变成「0 行」—— 那是**量具的锅**，
    /// 不是这张卡没有效果。所以这里优先按兵种挑。退回档与 `play` 逐字一致（⛔ 不发明新行为）。</para>
    /// </summary>
    static int PickSlot(BattleContext ctx, string side, string kind)
    {
        int s = string.Equals(side, "enemy", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        int fallback = -1;
        bool wantKind = !string.IsNullOrEmpty(kind)
                        && kind != "unit" && kind != "any" && kind != "troop" && kind != "prev";
        for (int i = 0; i < BoardSpec.Size; i++)
        {
            var u = ctx.Players[s].Board[i];
            if (u == null || u.IsWarlord) continue;
            if (fallback < 0) fallback = i;
            if (wantKind && u.Card != null
                && string.Equals(u.Card.Subtype, kind, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return fallback;
    }

    static int FreeSlot(BattleContext ctx, int p)
    {
        foreach (int s in new[] { 3, 5, 2, 6, 1, 7, 0, 8 })
            if (ctx.Players[p].Board[s] == null) return s;
        return 3;
    }

    static CardDef Fix(string id, int atk, int hp, int rng, string subtype)
    {
        return new CardDef(id, id, "unit", "", null, "Fixture", 1, atk, hp, rng, null, subtype: subtype);
    }

    static CardDef HeroOf(string id, string fac)
    {
        return new CardDef(id, id, "hero", "", null, fac, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    /// <summary>牌库 = 督军 + 垫牌 + `hand`（**倒着加**：抽牌从尾部 pop ⇒ `hand[0]` 最先上手）。形状照 `PlayProbe.Deck`。</summary>
    static List<CardDef> Deck(string fac, CardDef[] hand)
    {
        var d = new List<CardDef> { HeroOf("E3Warlord_" + fac, fac) };
        for (int i = 0; i < FillerCount; i++)
        {
            if (i % 5 == 4)
                d.Add(new CardDef("e3filler" + i, "e3filler" + i, "tactic", "", null, "Fixture", 1, 0, 0, 0, null));
            else
                d.Add(Fix("e3filler" + i, 0, 1, 0, FillerSubtypes[i % FillerSubtypes.Length]));
        }
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static int HandIdx(BattleContext ctx, int p, CardDef c)
    {
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++) if (h[i].Card == c) return i;
        return -1;
    }

    static void SetEnergy(BattleContext ctx, int p)
    {
        ctx.Players[p].Energy = FixtureEnergy;
        ctx.Players[p].MaxEnergy = FixtureMaxEnergy;
    }

    /// <summary>起始派系资源三件 —— 与 `SetEnergy` 同一个调用点、同一个形状（同 `PlayProbe.SetFaction`）。</summary>
    static void SetFaction(BattleContext ctx, int p)
    {
        ctx.Players[p].Faith = FixtureFaith;
        ctx.Players[p].SpiritStones = FixtureSpirit;
        ctx.Players[p].QuestPoints = FixtureQuestpoints;
    }

    static CardDef Find(List<CardDef> pool, string spec)
    {
        foreach (var c in pool) if (string.Equals(c.Id, spec, StringComparison.OrdinalIgnoreCase)) return c;
        foreach (var c in pool) if (string.Equals(c.Name, spec, StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }
}
