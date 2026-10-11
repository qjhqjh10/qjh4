using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RuleEngine;

/// <summary>
/// `faith` —— **信仰（Faith）付费段的【两把尺子】对账 ＋ 收集层覆盖**（台账 `A1452` / `A1453` / `A1565`）。
///
/// 🔴 **为什么单开一条子命令**：同一个池上，「信仰付费段有多少条」有**两个数**，
///    而两边都是**同一份源码跑出来的**、都「看着有依据」：
///
///   · **尺子 P（解析层）** = 逐段跑 <see cref="EffectText.ParseSegment"/>，取
///     `PaidCost &gt; 0 &amp;&amp; CostKindOf(PaidKind) == "faith"` 的段。**全池口径**（1126 张、四种卡型都算）。
///   · **尺子 Q（探针 ④「付费段」档）** = <see cref="WillRun"/> 腿 B ∪ `Classify` ②：
///     **只对 `unit`/`hero`**、段必须**有 `Head:` 冒号**、**冒号后正文解得出 op**、
///     且 <see cref="CardDef.HandledByOtherLayer"/> **没认领**、且头形状过 `WillRun.IsPaidHead`。
///
///   ⇒ 两把尺子的**分母根本不同**（卡型范围 / 冒号要求 / 解不解得开 / 谁接手）。
///     `A1452` 的射程原来写「11 条 / 9 张」用的是**尺子 Q**，而 `E5` 复核 `SOR72` 用的是**尺子 P**
///     ⇒ **19 条 / 17 张 vs 11 条 / 9 张**，看着像打架，其实是**两个问题**。
///
/// 🔴 **本子命令把它拆开**：逐条列出两边各收到谁、差在哪、**差因**是什么；
///    最后给出 **`CardDef.FaithOps`（收集层）对尺子 P 的覆盖率** —— **那才是「射程」该用的数**
///     （射程 = 「引擎真的会执行到几条」，尺子 P 是它的**上界候选集**）。
///
/// ⚠️ **尺子 Q 这一档在本文件里是【照抄】`WillRun.AddFaceBody` + `Classify` ②的判据重写的**
///    （`FaceBody` 是 `WillRun` 的私有嵌套类型，外面拿不到）。为防「照抄写歪」，
///    本命令 §⑤ **和 `WillRun.PaidSegments`（④ 实测，**同一份判据、不是第二份**）对账** ——
///    ⚠️ **是【子集检查】，不是相等**：④ 比 Q 多一道「腿 A（**它自己那条通道**）收没收到」的闸，
///    所以 `PaidSegments ⊆ Q` 成立、反向不成立。**不通过就出声并计入 rc**（⛔ 不许静默）。
///    🔴 **2026-10-11（`A1593`）**：那道闸原来是「全卡 `op` 签名并集比」⇒ 同一张卡**别的**
///    ability 里出现过的同签名 op 会把这一段顶成「收到了」（`SOR28` / `SOR36` 两个实例）。
///    现在**按通道比**，并且 ④ 的名单**不再只有信仰**（付费货币没有通道的那些也会进来）
///    ⇒ §⑤ 只比**货币 = `faith`** 的那些（`PaidSegments` 的第 4 项就是为这一维加的）。
///
/// ⛔ **本文件不判「哪把尺子对」** —— 判据归台账；它只把两个口径**摆到一起**并**指名差因**。
/// </summary>
class FaithProbe
{
    /// <summary>尺子 P 的一条读数：**一段**（卡 × 段原文）在解析层被认成信仰付费前缀。</summary>
    class Row
    {
        public CardDef C;
        public string Raw;         // 段原文（`Split(desc)` 出来的那一句 / `keywords` 原文条目）
        public string Where;       // `desc 第N句` / `keywords[i]`
        public int Cost;
        public int OpCount;        // `ParseSegment` 解出几条 op
        public string Ack;         // `HandledByOtherLayer` 认领它的层（null = 没别人认领）
        public string WhyP = "";   // 差因：**尺子 Q 为什么收不到它**（"" = Q 也收得到）
        public string Key { get { return C.Id + "\u0001" + Raw; } }
    }

    public static int Run(List<CardDef> pool, List<List<string>> rawKw, string dumpPath)
    {
        if (pool == null || pool.Count == 0) { Console.WriteLine("!! 池为空"); return 1; }
        var kwAll = rawKw ?? new List<List<string>>();

        // ---------- 尺子 P：解析层（全池、四种卡型） ----------
        var P = new List<Row>();
        for (int ci = 0; ci < pool.Count; ci++)
        {
            var c = pool[ci];
            if (c == null) continue;
            var kw = ci < kwAll.Count ? kwAll[ci] : null;

            var segs = EffectText.Split(c.Desc);
            for (int i = 0; i < segs.Count; i++) Consider(P, c, segs[i], "desc 第" + (i + 1) + "句");
            if (kw != null)
                for (int i = 0; i < kw.Count; i++) Consider(P, c, kw[i], "keywords[" + i + "]");
        }

        // ---------- 尺子 Q：探针 ④ 档（**逐条照抄 `WillRun.AddFaceBody` + `Classify` ②**） ----------
        var Q = new List<Row>();
        foreach (var r in P)
        {
            if (LegB(r.C, r.Raw) == null) continue;             // 卡型 / 冒号 / 正文解得开 / 无别层认领 四闸
            if (!WillRun.IsPaidHead(HeadOf(r.Raw))) continue;   // `Classify` ② 的形状闸
            Q.Add(r);
        }

        var onlyP = P.Where(r => !Q.Contains(r)).ToList();
        var onlyQ = Q.Where(r => !P.Contains(r)).ToList();

        Console.WriteLine("=== ruleprobe faith · 信仰付费段的【两把尺子】（`A1452` / `A1453` / `A1565`）===");
        Console.WriteLine("  卡池 " + Program.PoolPath);
        Console.WriteLine("  池 " + pool.Count(p => p != null) + " 张");
        Console.WriteLine();
        Console.WriteLine("  尺子 P（解析层）= 逐段 `EffectText.ParseSegment`，取 `PaidCost > 0 && CostKindOf(PaidKind) == \"faith\"`");
        Console.WriteLine("                    · **全池、四种卡型都算**（`desc` 分句 ∪ `keywords` 原文条目）");
        Console.WriteLine("  尺子 Q（探针④）= `willrun` 的「付费段」档（`WillRun.AddFaceBody` + `Classify` ②）");
        Console.WriteLine("                    · **只 unit/hero** · 段必须**有 `Head:`** · 冒号后**解得出 op** · 无别层认领");
        Console.WriteLine("  ⚠️ **两把都不是「射程」** —— 射程 = **§四 收集层（`CardDef.FaithOps`）真的收到几条**。");
        Console.WriteLine();

        Console.WriteLine("--- ① 尺子 P（解析层）逐条：" + P.Count + " 条 / " + P.Select(r => r.C.Id).Distinct().Count() + " 张 ---");
        foreach (var g in P.GroupBy(r => r.C.Id).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var c = g.First().C;
            Console.WriteLine("   " + Pad(c.Id, 24) + " " + Pad(c.Name, 26) + " [" + c.Type + "]");
            foreach (var r in g.OrderBy(r => r.Where, StringComparer.Ordinal))
                Console.WriteLine("      " + Pad(r.Where, 18) + " cost=" + Pad(r.Cost.ToString(), 4)
                                  + " ops=" + Pad(r.OpCount.ToString(), 4) + " `" + r.Raw + "`"
                                  + (r.Ack != null ? "   ←「" + r.Ack + "」接手" : ""));
        }
        Console.WriteLine();

        Console.WriteLine("--- ② 尺子 Q（探针 ④「付费段」档）逐条：" + Q.Count + " 条 / " + Q.Select(r => r.C.Id).Distinct().Count() + " 张 ---");
        foreach (var r in Q) Console.WriteLine("   " + Pad(r.C.Id, 24) + " " + Pad(r.C.Name, 26) + " `" + r.Raw + "`");
        Console.WriteLine();

        Console.WriteLine("--- ③ 差集与【差因】（P − Q = " + onlyP.Count + " 条 · Q − P = " + onlyQ.Count + " 条）---");
        if (onlyP.Count == 0 && onlyQ.Count == 0)
            Console.WriteLine("   ✅ 两把尺子逐条相等（同一批卡、同一批段）");
        foreach (var r in onlyP)
            Console.WriteLine("   P−Q  " + Pad(r.C.Id, 24) + " [" + Pad(r.C.Type, 8) + "] `" + r.Raw + "`\n"
                              + "          差因：" + (r.WhyP == "" ? "🔴 **未解释**（不该发生）" : r.WhyP));
        foreach (var r in onlyQ)
            Console.WriteLine("   Q−P  " + Pad(r.C.Id, 24) + " [" + Pad(r.C.Type, 8) + "] `" + r.Raw + "`\n"
                              + "          差因：🔴 **未解释** —— Q ⊆ P 按构造成立，这一条不该出现");
        Console.WriteLine();

        // ---------- ④ 收集层覆盖（`CardDef.FaithOps`）—— **这一栏才是射程** ----------
        //
        //   🔴 判据 = **cost 的多重集**逐卡比：解析层（尺子 P，只取 `desc` 那些段）vs 收集层。
        //      为什么比多重集而不是比条数：一张卡可以有**两段门槛不同**的信仰段（`SOR27` = 2 与 4、
        //      `SOR68` = 2 与 4）⇒ 只比「有几条」会把「2↔4 张冠李戴」放过。
        //   ⚠️ 为什么**只拿 `desc` 的段**比：`FaithOps` 是从 `EffectText.Parse(Desc)` 收的，
        //      `keywords` 原文条目**不在它的输入里** ⇒ 拿 keywords 去比会假报「漏」。
        //   ⚠️ **`Parse(Desc)` 有跨句 `carry`**（本段没前缀时接着付上一段的前缀）⇒ 收集层**可能合法地
        //      多于**逐段口径。本栏把这种情形**单独标成一档**（`F ⊃ P`），⛔ 不混进「漏」。
        var byCard = P.Where(r => r.Where.StartsWith("desc ", StringComparison.Ordinal))
                      .GroupBy(r => r.C.Id)
                      .ToDictionary(g => g.Key, g => g.ToList());
        int nSame = 0, nSuper = 0, nMiss = 0;
        var badLines = new List<string>();
        foreach (var kv in byCard.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var c = kv.Value[0].C;
            var pc = kv.Value.Select(r => r.Cost).OrderBy(x => x).ToList();
            var fc = (c.FaithOps ?? (IReadOnlyList<EffectOp>)new List<EffectOp>())
                     .Select(o => o.Cost).OrderBy(x => x).ToList();
            string ps = "[" + string.Join(",", pc) + "]", fs = "[" + string.Join(",", fc) + "]";
            bool same = pc.SequenceEqual(fc);
            bool super = !same && pc.All(x => fc.Contains(x))
                         && pc.Count <= fc.Count;    // 收集层多出来的 = carry / 同前缀贴到更多 op 上
            if (same) nSame++;
            else if (super) nSuper++;
            else nMiss++;
            if (same) continue;
            badLines.Add("   " + (super ? "F⊃P " : "⛔ 漏 ") + Pad(c.Id, 24) + " [" + Pad(c.Type, 8) + "]"
                         + " 尺子P(desc)=" + Pad(ps, 10) + " 收集层=" + Pad(fs, 10)
                         + (super ? "← 收集层**多**（`Parse` 的跨句 `carry`，或同一前缀盖到更多 op 上）"
                                  : "← 🔴 **尺子 P 有、收集层没有**（射程缺口）"));
        }
        int nCards = byCard.Count;
        Console.WriteLine("--- ④ 收集层（`CardDef.FaithOps`）对尺子 P 的覆盖 —— **这一栏才是射程** ---");
        Console.WriteLine("   逐卡比【cost 多重集】：✅ 完全一致 " + nSame + " 张 · ⚠️ F⊃P " + nSuper
                          + " 张 · 🔴 漏 " + nMiss + " 张（共 " + nCards + " 张有信仰段）");
        foreach (var s in badLines) Console.WriteLine(s);
        Console.WriteLine("   ⚠️ `F⊃P` **不是缺陷**：`Parse(Desc)` 的付费前缀会**跨句承接到下一句**"
                          + "（`SegResult.PaidCost` 的 carry），而尺子 P 是**逐段**跑的 ⇒ 收集层合法地多。");
        Console.WriteLine("   ⚠️ **本栏只比 `desc`**：`keywords` 原文条目不在 `Parse(Desc)` 的输入里。");
        Console.WriteLine();

        // ---------- ⑤ 与 `willrun` ④ 档（`WillRun.PaidSegments`）的关系 ----------
        //
        //   🔴 **两者不是「应当相等」，而是 `PaidSegments ⊆ Q`** —— 关系要写对，否则会把
        //      「④ 与 Q 覆盖的不是同一批货币」误判成本命令写歪了（这正是本节设成子集检查的原因）。
        //   · `Q` = **形状判据**（只问「这段长不长成 `N ☀:` 且没别的层认领」）；
        //   · `PaidSegments` = 形状 + **再一道「腿 A 一条都没收到」**的闸。
        //   ✅ **2026-10-11（`A1593`）：那第二道闸已经改成【按通道比】**（`WillRun.ClaimChannel`）
        //      —— 原来它按「**全卡** `op` 签名并集」比，`Sig()` 不带「属于哪条 ability」那一维
        //      ⇒ 同一张卡**别的** ability 里出现过的同签名 op 会把这一段顶成「收到了」。
        //      两个实例（`SOR28` / `SOR36`）就**从没进过 ④ 的名单**。
        //      ⚠️ **那两条今天仍然不在 ④ 里，但理由变了**：`CardDef.FaithOps` 已接进
        //      `WillRunOps` ⇒ 它们的**信仰通道**确实收到了它们（自检见 `willrun` ⑦ 的断言 B）。
        //      🔴 **`A1452` 台账里「11 条 / 9 张」是那个**低报**的数**；形状口径 = **13 条 / 11 张**。
        //   🆕 **`A1593` 起 ④ 的名单不再只有信仰**：按通道比之后，**付费货币没有对应通道**的
        //      那些（`N [Energy]:` / `N [Might]:` …）也会进来 ⇒ 第 4 项（货币）就是为这一节加的，
        //      ⛔ 不比这一维会把**别的货币**误判成「本命令写歪了」。
        var own = WillRun.PaidSegments(pool, kwAll);
        var ownFaith = own.Where(t => t.Item4 == "faith").ToList();   // ⚠️ 本命令的 Q 只覆盖信仰
        var myQ = Q.Select(r => r.Key).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
        var ownSet = new HashSet<string>(ownFaith.Select(t => t.Item1 + "\u0001" + t.Item2));
        var notCovered = ownSet.Where(k => !myQ.Contains(k)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        bool qOk = notCovered.Count == 0;
        Console.WriteLine("--- ⑤ 与 `willrun` ④ 档（`WillRun.PaidSegments`）的关系 —— **子集检查，不是相等** ---");
        Console.WriteLine("   本命令 Q（形状判据） = " + myQ.Count + " 条 / "
                          + Q.Select(r => r.C.Id).Distinct().Count() + " 张");
        Console.WriteLine("   `WillRun.PaidSegments`（④ 实测）= " + ownSet.Count + " 条 / "
                          + ownFaith.Select(t => t.Item1).Distinct().Count() + " 张"
                          + "（**只取货币 = `faith` 的那些**；④ 全表 " + own.Count + " 条 / "
                          + own.Select(t => t.Item1).Distinct().Count() + " 张 —— "
                          + "`A1593` 起 ④ 也会收别的货币，见上面那段）");
        if (!qOk)
        {
            Console.WriteLine("   🔴 **`PaidSegments ⊄ Q`** —— 本命令重写的判据与 `WillRun.AddFaceBody` 已经写歪了，"
                              + "§③ 的差因**不可用**（⛔ 不许静默）：");
            foreach (var x in notCovered) Console.WriteLine("       Q 没覆盖到：" + x.Replace('\u0001', '|'));
        }
        else Console.WriteLine("   ✅ `PaidSegments ⊆ Q`（本命令的判据至少和 `WillRun.AddFaceBody` 一样宽）");
        // 🔴 **`A1452` 的正面断言：这一档必须被【关掉】** —— `WillRunOps` 收了 `FaithOps` 之后，
        //    ④ 里不该再有任何**信仰**付费段。> 0 ⇒ 收集层/执行层没接上（本条修复回归）。
        if (ownSet.Count > 0)
        {
            Console.WriteLine("   🔴 **④「付费段」档还没关** —— 实测还剩 " + ownSet.Count + " 条：");
            foreach (var x in ownSet.OrderBy(x => x, StringComparer.Ordinal))
                Console.WriteLine("       " + x.Replace('\u0001', '|'));
            Console.WriteLine("     （期望 **0** —— `CardDef.FaithOps` 已经接进 `WillRunOps`，"
                              + "这些段不该再算「没收下来」）");
        }
        else Console.WriteLine("   ✅ ④「付费段」档 = **0 条**（`CardDef.FaithOps` 已接进 `WillRunOps`）");
        Console.WriteLine("   ⚠️ 两个数**不相等是正常的**：`PaidSegments` 多一道「腿 A 一条都没收到」的闸。"
                          + "🔴 **那道闸 2026-10-11（`A1593`）已经改成【按通道（= 按 ability）比】** ——"
                          + "原来它按全卡 `op` 签名并集比，`SOR28` / `SOR36` 那两条会被同一张卡的"
                          + "`Rally:` / `Pray:` 顶成「收到了」⇒ **④ 结构上少报那两条**（现已修，"
                          + "自检 = `willrun` ⑦ 的断言 A/B）。");
        Console.WriteLine();

        if (dumpPath != null)
        {
            var sb = new StringBuilder();
            sb.Append("id\ttype\twhere\tcost\tops\tinQ\tacked_other_layer\twhy_Q_misses\traw\n");
            foreach (var r in P)
                sb.Append(r.C.Id).Append('\t').Append(r.C.Type).Append('\t').Append(r.Where).Append('\t')
                  .Append(r.Cost).Append('\t').Append(r.OpCount).Append('\t')
                  .Append(Q.Contains(r) ? "1" : "0").Append('\t').Append(r.Ack ?? "").Append('\t')
                  .Append(r.WhyP).Append('\t').Append(r.Raw).Append('\n');
            var dir = Path.GetDirectoryName(Path.GetFullPath(dumpPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(dumpPath, sb.ToString());
            Console.WriteLine("  wrote " + dumpPath + " lines=" + (P.Count + 1));
        }

        // ---------- ⑥ 真打：门槛过不过 + 信仰扣不扣（`A1453` 的正面判据） ----------
        //
        //   🔴 **为什么非要有这一栏**：上面 §①②③④⑤ 量的全是**文本/收集**，
        //      没有一条回答「打出去之后按不按阈值算、信仰有没有被扣掉」——
        //      而 `A1453` 要的恰恰是**后者**（我们原来在扣 `ps.Faith`）。
        //   🔴 **断言（结构上不可能同时满足的那一条）**：`信仰前后【必须相等】`。
        //      只断「效果落地了」是不够的 —— 把扣款那一行加回去、效果照样落地，
        //      「效果对」那一半仍会绿（那正是我们原来那套「自洽」的样子）。
        int nScFail = 0;
        Console.WriteLine("--- ⑥ 真打（夹具直调 `RuleCore.NewBattle` → `PlayCard`）：门槛 + 信仰【不许被扣】 ---");
        //   fixture 里 P1 场上没有单位 ⇒ 只挑**纯自身增益**的段，避免「自动选最弱敌人」那一档干扰。
        nScFail += Scenario(pool, "SOR27", 0);
        nScFail += Scenario(pool, "SOR27", 2);
        nScFail += Scenario(pool, "SOR27", 6);
        nScFail += Scenario(pool, "SOR11", 2);
        nScFail += Scenario(pool, "SOR11", 3);
        nScFail += Scenario(pool, "SOR68", 4);
        nScFail += Scenario(pool, "SOR12", 4);
        nScFail += Scenario(pool, "SOR12", 5);
        nScFail += Scenario(pool, "SOR72", 6);
        nScFail += Scenario(pool, "SOR40", 6);
        Console.WriteLine();

        int rc = 0;
        if (!qOk) rc = 1;                                       // 重写写歪了 ⇒ 后面几个数都不可信
        if (onlyP.Any(r => r.WhyP == "") || onlyQ.Count > 0) rc = 1;   // 差因有解释不了的
        if (nMiss > 0) rc = 1;                                  // 收集层漏掉尺子 P 的段
        if (ownSet.Count > 0) rc = 1;                           // ④「付费段」档没关（`A1452` 的正面判据）
        if (nScFail > 0) rc = 1;                                // ⑥ 真打对不上
        Console.WriteLine("=== 结束（rc=" + rc + "）===");
        Console.WriteLine("  rc 判据：③ 每条差集都有差因 · ④ 无 `漏` · ⑤ `PaidSegments ⊆ Q` 且 ④ 里"
                          + "**货币 = `faith`** 的那一档 = 0 条"
                          + " · ⑥ 真打全对（含**信仰前后相等**）⇒ rc=0。");
        Console.WriteLine("  ⚠️ §④ 的 `F⊃P` 与 §③ 的 `P−Q` **都不算失败**（前者是 carry、后者是分母不同），"
                          + "但它们**也绝不混进「全通」**。");
        return rc;
    }

    // ==================================================================
    //  ⑥ 真打夹具
    // ==================================================================

    /// <summary>一个最小的真打场景：`faith` 点信仰 → 打出一张带信仰段的卡 → 读落场后的本队单位。</summary>
    static int Scenario(List<CardDef> pool, string id, int faith)
    {
        var card = pool.FirstOrDefault(c => c != null && c.Id == id);
        if (card == null) { Console.WriteLine("   ⛔ 池里没有 `" + id + "`"); return 1; }

        var vars = GameplayVariables.Classic.Clone();
        vars.startingHand = 5;
        var fillers = new List<CardDef>();
        for (int i = 0; i < 30; i++)
            fillers.Add(new CardDef("f" + i, "f" + i, "unit", "", null, "Fixture", 1, 0, 1, 0, null,
                                    subtype: "Infantry"));
        var d0 = new List<CardDef> { Hero("FIXW0") };
        d0.AddRange(fillers); d0.Add(card);
        var d1 = new List<CardDef> { Hero("FIXW1") };
        d1.AddRange(fillers);

        BattleContext ctx;
        try { ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars); }
        catch (Exception e) { Console.WriteLine("   ⛔ `" + id + "` faith=" + faith + " 建局抛异常：" + e.Message); return 1; }

        var ps = ctx.Players[0];
        ps.Energy = 30; ps.MaxEnergy = 30;
        ps.Faith = faith;

        int hi = -1;
        for (int i = 0; i < ps.Hand.Count; i++) if (ps.Hand[i].Card == card) { hi = i; break; }
        if (hi < 0) { Console.WriteLine("   ⛔ `" + id + "` 没上手（夹具问题）"); return 1; }

        int faithBefore = ps.Faith;
        int energyBefore = ps.Energy;
        int slot = 3;
        int code = RuleCore.PlayCard(ctx, 0, hi, slot);
        var u = ps.Board[slot];
        if (code != RuleCodes.OK || u == null)
        {
            Console.WriteLine("   ⛔ `" + id + "` 打不出去（code=" + code + "）—— 换一格夹具");
            return 1;
        }

        // 期望：卡上 **每一条** `N ☀:` 段，`faith >= N` ⇒ 那一条生效；否则不生效。
        //   判据 = `CardDef.FaithOps`（**逐条**），不是「一个 FaithCost」（见那个属性的 doc）。
        var unmet = card.FaithOps.Where(o => o.Cost > faith).Select(o => o.Cost).ToList();
        var met = card.FaithOps.Where(o => o.Cost <= faith).Select(o => o.Cost).ToList();

        bool faithOk = ps.Faith == faithBefore;      // 🔴 `A1453` 的正面判据：**一分都不能扣**
        string mark = faithOk ? "✅" : "🔴";
        Console.WriteLine("   " + mark + " " + Pad(id, 8) + " 信仰=" + Pad(faith.ToString(), 3)
                          + " 打后信仰=" + Pad(ps.Faith.ToString(), 4)
                          + " 门槛够=" + Pad(met.Count > 0 ? string.Join(",", met) : "-", 6)
                          + " 门槛不够=" + Pad(unmet.Count > 0 ? string.Join(",", unmet) : "-", 6)
                          + " 落场后 攻=" + Pad(u.Attack.ToString(), 3) + " 远=" + Pad(u.RangedAttack.ToString(), 3)
                          + " 血=" + Pad(u.Health.ToString(), 3) + " 甲=" + Pad(u.Armor.ToString(), 3)
                          + " 盾=" + (u.HasShield ? "1" : "0")
                          + " 能量 " + energyBefore + "→" + ps.Energy);
        foreach (var line in ctx.Events.Skip(Math.Max(0, ctx.Events.Count - 6)))
            Console.WriteLine("         · " + line);
        return faithOk ? 0 : 1;
    }

    static CardDef Hero(string id) =>
        new CardDef(id, id, "hero", "", null, "Fixture", 0, 2, 30, 0, null, subtype: "Warlord");

    /// <summary>把一段丢给解析层，是信仰付费前缀就收进 <paramref name="into"/>，并顺手算差因。</summary>
    static void Consider(List<Row> into, CardDef c, string seg, string where)
    {
        if (string.IsNullOrEmpty(seg)) return;
        var r = EffectText.ParseSegment(seg);
        if (r == null || r.PaidCost <= 0) return;
        if (EffectText.CostKindOf(r.PaidKind) != "faith") return;
        var row = new Row
        {
            C = c, Raw = seg.Trim(), Where = where, Cost = r.PaidCost,
            OpCount = r.Ops == null ? 0 : r.Ops.Count,
            Ack = CardDef.HandledByOtherLayer(c, seg),
        };
        row.WhyP = WhyMissesQ(c, seg);
        into.Add(row);
    }

    /// <summary>这一段的小写头（口径照 `WillRun.AddFaceBody`：`StripLeadingIcons` 之后找 `:`）。</summary>
    static string HeadOf(string seg)
    {
        string s = EffectText.StripLeadingIcons((seg ?? "").Trim());
        int col = s.IndexOf(':');
        return col <= 0 ? "" : s.Substring(0, col).Trim().ToLowerInvariant();
    }

    /// <summary>把一段按 **`WillRun.AddFaceBody` 的四道闸**过一遍，过得去就返回小写头（否则 null）。
    /// ⚠️ 这是**照抄**，不是调用 —— `FaceBody` 是 `WillRun` 的私有嵌套类型。§⑤ 的自检就是为它设的。</summary>
    static string LegB(CardDef c, string seg)
    {
        if (c.Type != "unit" && c.Type != "hero") return null;     // 腿 B 只对 unit/hero
        if (string.IsNullOrEmpty(seg)) return null;
        string s = EffectText.StripLeadingIcons(seg.Trim());
        if (string.IsNullOrEmpty(s)) return null;
        int col = s.IndexOf(':');
        if (col <= 0) return null;
        string head = s.Substring(0, col).Trim().ToLowerInvariant();
        string body = s.Substring(col + 1).Trim();
        if (head.Length == 0 || body.Length == 0) return null;
        var ops = EffectText.Parse(body, out _, out _);
        if (ops == null || ops.Count == 0) return null;
        if (CardDef.HandledByOtherLayer(c, seg) != null) return null;
        return head;
    }

    /// <summary>尺子 Q 收不到这一段时，**是哪一道闸挡的**（口径逐条照抄 `WillRun.AddFaceBody` + `Classify` ②）。</summary>
    static string WhyMissesQ(CardDef c, string seg)
    {
        if (c.Type != "unit" && c.Type != "hero")
            return "卡型 = `" + c.Type + "` —— 探针腿 B **只对 unit/hero 做段级对账**"
                 + "（`WillRun` 文件头：tactic/defence 的腿 A 就是 `Parse(desc)`，按段对账会产生假警报）";
        if (LegB(c, seg) == null)
        {
            if (CardDef.HandledByOtherLayer(c, seg) != null)
                return "已被「" + CardDef.HandledByOtherLayer(c, seg) + "」接手（`HandledByOtherLayer`）";
            string s0 = EffectText.StripLeadingIcons((seg ?? "").Trim());
            int col0 = s0.IndexOf(':');
            if (col0 <= 0)
                return "**段里没有 `Head:` 冒号**（`AddFaceBody` 里 `col <= 0` 直接 return）"
                     + " ⇒ ④ 档看不见它（它会掉进 `willrun` 的 ⑤「无头正文」档）";
            string body0 = s0.Substring(col0 + 1).Trim();
            var o0 = EffectText.Parse(body0, out _, out _);
            if (o0 == null || o0.Count == 0)
                return "冒号后正文 `" + body0 + "` **主解析器解不出 op** ⇒ 不算「可执行正文段」";
            return "头或正文为空";
        }
        if (!WillRun.IsPaidHead(HeadOf(seg)))
            return "头 `" + HeadOf(seg) + "` **不过 `WillRun.IsPaidHead`**"
                 + "（正则 `^\\d+\\s*[\\[\\(]?\\s*[a-z☀]`）";
        return "";   // 尺子 Q 也收得到
    }

    static string Pad(string s, int w)
    {
        if (s == null) s = "";
        int n = 0;
        foreach (var ch in s) n += ch > 0x2e80 ? 2 : 1;
        var sb = new StringBuilder(s);
        while (n < w) { sb.Append(' '); n++; }
        return sb.ToString();
    }
}
