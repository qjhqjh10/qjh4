using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RuleEngine;

/// <summary>
/// `willrun` —— **「这张卡的正文到底收到没有、收到的是不是空的」**。
///
/// 🔴 **它修的是什么**（台账 `A1432` · 出处 `资料/普查产出_第十四会话/逐卡效果检查_方法与边界.md` §2）：
///   全池尺子原来只有一种问法 —— **「拿 `EffectText.WillRunOps(c)` 返回的那批 op，逐条问
///   `OpHasMechanism`」**（`RuleEngineTest.cs` 的 `willrun_mechanism.md` 生成器 · `EffectText.Coverage` 第二层）。
///   那是一种 **【空转 ⇒ 全绿】** 的形状：**op 表为空 ⇒ 一条都不判 ⇒ 这张卡算「全通」**。
///   实测代价（`A1386`）：`CardDef.RoutableTriggers` 缺 `survivor` / `sacrifice` 时，
///   卡面印着的正文**根本收不下来**、`FireTriggerAt` 恒空转，而报表照样 **586/586 全绿**。
///   ⇒ 这不是「漏报一张」，是**整整一类「正文没收下来」永远报不出来**。
///
/// ⇒ **本子命令把「空表」显式拆开**（`A1432` 要的就是「两者必须能分开」）：
///   · **① 本来就没正文** —— 卡面上没有任何「可执行正文段」（`desc` 空 / 整条都是裸关键词）。
///     **不是缺陷**，但**必须报出来**，⛔ 不许混进「全通」冒充成绩。
///   · **② 收漏了** —— 卡面上**写着**一段主解析器认得的正文，而执行层没收到。**这才是缺陷**。
///   · **③ 未归因** —— 收 0 条、卡面**没有** `Head:` 形状的段，但主解析器对整条 `desc` 解得出 op。
///     **如实标出来**（本仓红线：不许静默），⛔ 不判它是缺陷还是正常。
///
/// 🔴 **但 ①②③ 四档【全部以 `Head:` 为锚】**（`A1459` 实测）⇒ 「**卡面没有头、又没被消费**」的句子
///   只会掉进 ①/③、**永远不会被点名**。最要紧的后果：`SW23` / `GSC71` / `GSC36` 三张**真缺陷**
///   正因为这道缝躲开了清单 —— 它们各**还有一条别的 op**（`Rally` / `Strike` / `Uprising`）
///   ⇒ **腿 A ≠ 0** ⇒ 连「③ 未归因」都进不去（③ 的判据是「腿 A 收到 0 条」）。
///   ⇒ **第五档 `⑤「无头正文」逐句报账`**（见 <see cref="AddHeadless"/> / <see cref="ClassifyHeadless"/>）：
///     卡面**没有 `Head:`**、而**主解析器**解得出 op、**腿 A 一条都没收到**、**且没有任何别的层认领它**
///     ⇒ 那句**永不发生**。三道闸（`HandledByOtherLayer` / `AtTurnClauses` / `CostWhens`）
///     **全部转调引擎的公开面**，⛔ 探针里一行新文法都不写 —— 那正是它不误报的原因。
///
/// 🔴 **判据是两条【独立的腿】**（这是它能看见 `A1386` 的原因，也是它**不是自证**的原因）：
///   · **腿 A** = 执行层的输出（`WillRunOps`）；
///   · **腿 B** = **从卡面文本独立抽的**「可执行正文段」（`desc` 分句 ∪ `keywords` 原文条目），
///     只借引擎的**归一函数**（`EffectText.Split` / `StripLeadingIcons`），**不借任何注册结果**。
///   ⛔ 原来那条口径的两条腿**共用一个口**（都看 `WillRunOps`）⇒ 空表时两边一起空转。
///
/// ⚠️ **只对 `unit` / `hero` 做「段级对账」** —— 因为只有这两种卡的 `WillRunOps` 是**注册层**产出的。
///   `tactic` / `defence` 的 `WillRunOps` **就是** `Parse(c.Desc)`（整条 desc 一次解析，
///   子效果藏在 `chooseone` 的 `ChooseWhat` 或 `give "…"` 的引号里）⇒ **按「段」对账会产生假警报**：
///   实测 `ASH83`（`Choose one: …`）· `BL4`（`Your Warlord gains "Blast 2 and Slay: …"`）
///   两族被逐条误报过。⇒ 那两类卡只报「整条 desc 解不出 op」那一档。
///
/// ⛔ **本文件一行都不改引擎**（`MyGame/Assets/**` 不在本工具的白名单里）——
///   它把「空表 / 收漏」**报出来**，判决（改哪张表、补哪个消费点）由调度台另派。
/// ⚠️ **它是【快速内环 / 旁证】**：正式验收仍是 `RuleEngineTest.Run`；但它不跑 Unity（整趟 &lt; 2 秒）。
/// </summary>
class WillRun
{
    // ==================================================================
    //  数据结构
    // ==================================================================

    /// <summary>卡面上一段**可执行的正文**：`Head: body` 形状，且 `body` 主解析器解得出 op。</summary>
    class FaceBody
    {
        public string Head;       // 小写、去首尾空白（口径照 `CardDef.AddTriggerOp` 里那句）
        public string Text;       // 正文原文（`:` 之后那半句）
        public string Raw;        // 整段原文（报告里显示）
        public string Where;      // `desc 第N句` / `keywords[i]`
        public List<EffectOp> Ops;
        public string Kind = "";  // 没收下来时：`收漏` / `付费段` / `同头两写`
        public string Why = "";   // 上面那一档的人话
    }

    /// <summary>收漏的三档（判据见 <see cref="Classify"/>）。</summary>
    const string KMiss = "收漏";         // 🔴 触发头没收下来 —— `A1386` 是这一族。**计入 rc**
    const string KPaid = "付费段";       // 🔴 `N ☀:` / `N [Energy]:` 形状的付费能力段没收下来。**计入 rc**
    const string KTwice = "同头两写";    // ⚠️ 头注册了、但正文与这一段不同（`desc` / `keywords` 两份数据不一致）。**不计入 rc**

    /// <summary>🆕 **无头档**（`A1459`）的三档 —— 判据见 <see cref="ClassifyHeadless"/>。
    ///   这一档治的是：**卡面没有 `Head:` 的句子，腿 B 结构上抽不到**（`AddFaceBody` 里 `col &lt;= 0` 直接 return）
    ///   ⇒ 凡是「没头的句子又没被消费」，它只会掉进 ①「本来没正文」/ ③「未归因」，
    ///   **永远不会被点名**（`SW23` / `GSC71` / `GSC36` 就是这么躲开清单的）。</summary>
    const string KHNone = "无消费点";    // 🔴 腿 A 没收到、**也没有任何别的层认领** ⇒ 这句永不发生。**计入 rc**
    const string KHAtTurn = "回合起止";  // ⚠️ `EffectText.AtTurnClauses` 认领（`ResolveAtTurn:4792` 扫 `u.Card.Desc` 吃它）。**不计入 rc**
    const string KHCostWhen = "降费触发器"; // ⚠️ `CardDef.CostWhens` 认领（`FireCostWhen` 吃它）。**不计入 rc**

    class Row
    {
        public CardDef C;
        public List<EffectOp> RunOps = new List<EffectOp>();   // 腿 A：执行层收到的 op
        public List<string> NoMech = new List<string>();       // 老口径里「没机制」的原因 + 出处
        public bool OldPass;                                   // 老口径（空表算通）
        public List<FaceBody> Bodies = new List<FaceBody>();   // 腿 B：卡面上带正文的段（只 unit/hero）
        public List<FaceBody> Orphans = new List<FaceBody>();  // 其中没被收到的
        /// <summary>🆕 `A1459` **无头档**：卡面**没有 `Head:`**、而主解析器解得出 op、**且腿 A 一条都没收到**的段。
        ///   ⚠️ 含「已被别的层认领」的那些（<see cref="FaceBody.Kind"/> 记着是哪个层）——
        ///   它们**只是计数**，不进账；只有 <see cref="KHNone"/> 那一批才是这一档要点的名。</summary>
        public List<FaceBody> Headless = new List<FaceBody>();
        public string EmptyKind = "";                          // 空表分类："" / 本来没正文 / 收漏了 / 未归因
    }

    /// <summary>⚠️ **op 的身份【不含 `Source`】** —— 这一点是实测踩出来的，别加回去：
    ///   同一个正文，**不同采集点解析出来 `Source` 不同**：
    ///   · `CardDef.AddTriggerOp:1340` 是 `Parse(body)` ⇒ `Source` 是**正文那半句**；
    ///   · `CardDef.CollectSpiritOps:1000` 是 `Parse(Desc)` 再挑 `CostKind == spirit` 的 op
    ///     ⇒ `Source` 是**整句**（`1 [Spirit Stone]: Give +1 …`）。
    ///   ⇒ 拿 `Source` 逐字比对，**会把 50 条已经收到的段误报成「收漏」**（实测：ASH 那一族
    ///     灵魂石卡全中招）。⇒ 只比 `verb / amount / payload / target` 四样（语义那一半）。</summary>
    static string Sig(EffectOp o)
    {
        return (o.Verb ?? "") + "\u0001" + o.Amount + "\u0001" + (o.Payload ?? "")
             + "\u0001" + (o.Target == null ? "" : o.Target.Raw ?? "");
    }

    // ==================================================================
    //  入口
    // ==================================================================

    /// <summary>返回**退出码**：`0` = 没有「收漏」/「付费段」；`1` = 有（⛔ 不许静默绿）。</summary>
    public static int Run(List<CardDef> pool, List<List<string>> poolRawKw, string dumpPath)
    {
        if (pool == null || pool.Count == 0) { Console.Error.WriteLine("!! 卡池是空的"); return 2; }

        var rows = new List<Row>();
        for (int i = 0; i < pool.Count; i++)
            rows.Add(Measure(pool[i], (poolRawKw != null && i < poolRawKw.Count) ? poolRawKw[i] : null, pool));

        var types = new[] { "tactic", "unit", "hero", "defence" };
        Console.WriteLine("=== willrun · 卡面正文「收下来没有」（按卡类型问对的层）===");
        Console.WriteLine("  卡池 " + Program.PoolPath);
        Console.WriteLine("  池 " + rows.Count + " 张（`scan` 的 " + rows.Count(r => !string.IsNullOrEmpty(r.C.Desc))
                          + " 张 = 池 − `desc` 为空的 " + rows.Count(r => string.IsNullOrEmpty(r.C.Desc)) + " 张）");
        Console.WriteLine("  腿 A（执行层） = `EffectText.WillRunOps`（unit/hero 走事件·触发·灵魂石·誓约·光环；"
                          + "tactic/defence 走主解析器）");
        Console.WriteLine("  腿 B（卡面）  = `desc` 分句 ∪ `keywords` 原文条目里 `Head: body` 且 `body` 解得出 op 的那些"
                          + "（**只对 unit/hero 做段级对账**，见文件头）");
        Console.WriteLine();

        // ---------------------------------------------------------------- ① 按卡类型
        Console.WriteLine("  --- ① 按卡类型 ---");
        Console.WriteLine("  " + Pad("类型", 11) + Pad("卡数", 7) + Pad("收到正文", 9) + Pad("空表", 7)
                          + Pad("本来没正文", 12) + Pad("收漏了", 8) + "未归因");
        foreach (var t in types)
        {
            var g = rows.Where(r => r.C.Type == t).ToList();
            Console.WriteLine("  " + Pad("[" + t + "]", 11) + Pad(g.Count.ToString(), 7)
                + Pad(g.Count(r => r.RunOps.Count > 0).ToString(), 9)
                + Pad(g.Count(r => r.RunOps.Count == 0).ToString(), 7)
                + Pad(g.Count(r => r.EmptyKind == "本来没正文").ToString(), 12)
                + Pad(g.Count(r => r.EmptyKind == "收漏了").ToString(), 8)
                + g.Count(r => r.EmptyKind == "未归因"));
        }
        var others = rows.Where(r => !types.Contains(r.C.Type)).ToList();
        if (others.Count > 0)
            Console.WriteLine("  " + Pad("[其它]", 11) + Pad(others.Count.ToString(), 7)
                + Pad(others.Count(r => r.RunOps.Count > 0).ToString(), 9)
                + Pad(others.Count(r => r.RunOps.Count == 0).ToString(), 7));
        Console.WriteLine();

        // ---------------------------------------------------------------- ② 老口径 vs 新口径
        Console.WriteLine("  --- ② 🔴 老口径 vs 新口径（同一批卡、同一趟算出来）---");
        Console.WriteLine("  老口径 = `RuleEngineTest` 报表的形状：只对 `WillRunOps` **返回的** op 逐条问 `OpHasMechanism`");
        var oldParts = types.Select(t => { var g = rows.Where(r => r.C.Type == t).ToList();
                                           return t + " " + g.Count(r => r.OldPass) + "/" + g.Count; });
        Console.WriteLine("    " + string.Join(" · ", oldParts) + " —— 全通");
        Console.WriteLine("    ⚠️ 这条口径**把「一条 op 都没收到」也算作【全通】**（`A1432`）");
        Console.WriteLine("       ⇒ 它绿**不代表**「卡面正文收下来了」，只代表「**收到的那部分**都有机制」");
        Console.WriteLine("       ⇒ 拿它当「逐卡效果可实现」的尺子 = **空转 ⇒ 全绿**（`A1386` 就是这么漏掉的）");
        Console.WriteLine("  新口径 = 本子命令：**空表要出声**，且「本来没正文」与「收漏了」分开算");
        foreach (var t in types)
        {
            var g = rows.Where(r => r.C.Type == t).ToList();
            Console.WriteLine("    " + Pad(t, 8) + "收到 " + Pad(g.Count(r => r.RunOps.Count > 0).ToString(), 5)
                              + " 空表 " + Pad(g.Count(r => r.RunOps.Count == 0).ToString(), 5)
                              + "（本来没正文 " + g.Count(r => r.EmptyKind == "本来没正文")
                              + " / 收漏 " + g.Count(r => r.EmptyKind == "收漏了")
                              + " / 未归因 " + g.Count(r => r.EmptyKind == "未归因") + "）");
        }
        Console.WriteLine();

        // ---------------------------------------------------------------- ③ 空表逐张
        var missed = rows.Where(r => r.EmptyKind == "收漏了").ToList();
        var unattr = rows.Where(r => r.EmptyKind == "未归因").ToList();
        int noprose = rows.Count(r => r.EmptyKind == "本来没正文");
        Console.WriteLine("  --- ③ 空表的卡逐张（🔴 这一栏老口径【看不见】）---");
        if (missed.Count == 0) Console.WriteLine("    ② 收漏了：**0 张**");
        else
        {
            Console.WriteLine("    ② 收漏了：" + missed.Count + " 张 ← ⛔ **这是缺陷**（卡面写着正文、执行层一条没收到）");
            foreach (var r in missed)
                Console.WriteLine("       " + r.C.Id + "  " + r.C.Name + "  [" + r.C.Type + "]  —— "
                    + string.Join(" ／ ", r.Orphans.Select(b => b.Kind))
                    + "；卡面段：" + string.Join(" ／ ", r.Orphans.Select(b => "`" + Clip(b.Raw, 60) + "`(" + b.Where + ")")));
        }
        if (unattr.Count == 0) Console.WriteLine("    ③ 未归因：**0 张**");
        else
        {
            Console.WriteLine("    ③ 未归因：" + unattr.Count + " 张 ← ⚠️ **如实标出**（空表、卡面没有 `Head:` 形状的段，"
                              + "但主解析器对整条 `desc` 解得出 op）");
            Console.WriteLine("       ⇒ 这些卡的效果**多半由 `WillRunOps` 之外的层消费**"
                              + "（例 `EffectResolver.ResolveAtTurn:4792` 直接扫 `u.Card.Desc` 的回合起止从句）");
            Console.WriteLine("       ⇒ ⛔ 本工具**不判**它们是缺陷还是正常 —— 但**也绝不把它们算进「全通」**");
            foreach (var r in unattr)
                Console.WriteLine("       " + r.C.Id + "  " + r.C.Name + "  [" + r.C.Type
                                  + "]  desc=「" + Clip(r.C.Desc, 70) + "」");
        }
        Console.WriteLine("    ① 本来没正文：" + noprose + " 张（不逐张列 —— 卡面确实没有可执行的正文）");
        Console.WriteLine();

        // ---------------------------------------------------------------- ④ 段级总账
        var orphAll = rows.SelectMany(r => r.Orphans).ToList();
        int nMiss = orphAll.Count(b => b.Kind == KMiss);
        int nPaid = orphAll.Count(b => b.Kind == KPaid);
        int nTwice = orphAll.Count(b => b.Kind == KTwice);
        int bodiesAll = rows.Sum(r => r.Bodies.Count);
        int orphCards = rows.Count(r => r.Orphans.Count > 0);
        Console.WriteLine("  --- ④ 段级总账（**只 unit/hero** —— 一张卡可以「收了一半」，那一半老口径照样报「全通」）---");
        Console.WriteLine("    卡面带正文的段 " + bodiesAll + " 条 · 已收到 " + (bodiesAll - orphAll.Count)
                          + " 条 · **没收下来 " + orphAll.Count + " 条**（落在 " + orphCards + " 张卡上）");
        Console.WriteLine("      · **" + KMiss + "**（触发头没进 `TriggerTexts` —— `AddTriggerOp` 要求头在 "
                          + "`CardDef.RoutableTriggers` 里）  " + nMiss + " 条 ← `A1386` 是这一族。**计入 rc**");
        Console.WriteLine("      · **" + KPaid + "**（`N ☀:` / `N [Energy]:` 形状的付费能力段 —— "
                          + "`WillRunOps` 的 unit/hero 支**压根不看这类段**）  " + nPaid + " 条 ← **计入 rc**");
        Console.WriteLine("      · **" + KTwice + "**（同一个头注册了、但 `desc` 与 `keywords` 两份正文**不一样**，"
                          + "后者被「先到先得」丢掉）  " + nTwice + " 条 ← ⚠️ **数据不一致**，⛔ 不计入 rc");
        if (missed.Count != orphCards)
            Console.WriteLine("    （「整张卡一条都没收到」= 上面 ② 那 " + missed.Count + " 张；其余是**半漏**）");
        if (orphAll.Count > 0)
        {
            Console.WriteLine("    🔴 没收下来的段逐条（前 60 条；⛔ **候选**，判决归调度台/引擎侧）：");
            int printed = 0;
            foreach (var r in rows)
            {
                if (printed >= 60) break;
                foreach (var b in r.Orphans)
                {
                    if (printed >= 60) break;
                    printed++;
                    Console.WriteLine("       " + Pad(b.Kind, 7) + r.C.Id + "  " + r.C.Name + "  [" + r.C.Type
                                      + "]  `" + Clip(b.Raw, 64) + "`  (" + b.Where + ")  ← " + b.Why);
                }
            }
        }
        Console.WriteLine();

        // ---------------------------------------------------------------- ⑤ 🆕 `A1459` 无头档
        var headAll = rows.SelectMany(r => r.Headless).ToList();
        var headNone = headAll.Where(b => b.Kind == KHNone).ToList();
        int headNoneCards = rows.Count(r => r.Headless.Any(b => b.Kind == KHNone));
        int headAtTurn = headAll.Count(b => b.Kind == KHAtTurn);
        int headCostWhen = headAll.Count(b => b.Kind == KHCostWhen);
        var headOther = headAll.Where(b => b.Kind != KHNone && b.Kind != KHAtTurn && b.Kind != KHCostWhen)
                               .GroupBy(b => b.Kind).OrderByDescending(g => g.Count()).ToList();
        Console.WriteLine("  --- ⑤ 🔴 「无头正文」逐句报账（`A1459`）—— 腿 B **结构上抽不到**的那一半 ---");
        Console.WriteLine("    🔴 **这一档为什么必须单开**：腿 B 只抽 `Head: body`（`AddFaceBody` 里 `col <= 0` 直接 return）");
        Console.WriteLine("       ⇒ 「**卡面没有头、又没被消费**」的句子只会掉进 ①「本来没正文」/ ③「未归因」，");
        Console.WriteLine("         **永远不会被点名**。实测后果（`A1459` 出处 §④㈡）：`SW23` / `GSC71` / `GSC36`");
        Console.WriteLine("         三张真缺陷**正是因为这道缝**躲开清单 —— 它们各有一条别的 op（`Rally`/`Strike`/`Uprising`）");
        Console.WriteLine("         ⇒ **腿 A ≠ 0** ⇒ 连「③ 未归因」也进不去；而**那句无头的**在腿 B 里结构上不成段"
                          + "（`faceBodies` 是 1，是它那条有头的 `Rally:`）⇒ 段级那一趟（④）也照不到。");
        Console.WriteLine("    无头段（卡面没有 `Head:`、而**主解析器**解得出 op、且**腿 A 一条都没收到**）"
                          + "共 **" + headAll.Count + " 条**，落在 **" + rows.Count(r => r.Headless.Count > 0) + " 张卡**上：");
        Console.WriteLine("      · 🔴 **" + KHNone + "**  " + headNone.Count + " 条（" + headNoneCards + " 张卡）"
                          + " ← **计入 rc**：这三道闸全空 ⇒ **没有任何一层会执行它**");
        Console.WriteLine("      · ⚠️ **" + KHAtTurn + "**  " + headAtTurn + " 条 ← `EffectText.AtTurnClauses` 认领"
                          + "（`ResolveAtTurn:4792` 直接扫 `u.Card.Desc`，**不经过 `WillRunOps`**）⛔ 不计入 rc");
        Console.WriteLine("      · ⚠️ **" + KHCostWhen + "**  " + headCostWhen + " 条 ← `CardDef.CostWhens` 认领"
                          + "（`FireCostWhen` 消费；`WillRunOps` 的六个来源里没有 `_costWhens`）⛔ 不计入 rc");
        foreach (var g in headOther)
            Console.WriteLine("      · ⚠️ **" + g.Key + "**  " + g.Count() + " 条 ← `CardDef.HandledByOtherLayer` 认领"
                              + "（判据转调引擎那一处，⛔ 探针不另写一套）");
        Console.WriteLine("    ⛔ 三道闸（`HandledByOtherLayer` / `AtTurnClauses` / `CostWhens`）**全部转调引擎的公开面**，");
        Console.WriteLine("       探针里**一行新文法都没写** —— 这正是它不会误报的原因。");
        Console.WriteLine("    ⚠️ 与 ③「未归因」**不是两笔账** —— 同一件事的两种粒度（③ 按卡、⑤ 按句）："
                          + "「整张卡空表」的卡会同时出现在两边，⛔ **别把两个数相加**；");
        Console.WriteLine("       而**卡上还有别的 op** 的那一批（`SW23` / `GSC71` / `GSC36`）**只有 ⑤ 看得见**。");
        if (headNone.Count > 0)
        {
            Console.WriteLine("    🔴 「" + KHNone + "」逐条（⛔ **候选**，判决归调度台/引擎侧）：");
            int printed2 = 0;
            foreach (var r in rows)
            {
                if (printed2 >= 80) break;
                foreach (var b in r.Headless)
                {
                    if (b.Kind != KHNone) continue;
                    if (printed2 >= 80) break;
                    printed2++;
                    var verbs = string.Join(" · ", b.Ops.Select(o => o.Verb
                        + (string.IsNullOrEmpty(o.Payload) ? "" : " " + o.Payload)
                        + (o.Amount != 0 ? " " + o.Amount : "")).Distinct());
                    Console.WriteLine("       " + Pad(r.C.Id, 24) + Pad(r.C.Name, 30) + "[" + r.C.Type + "]  `"
                                      + Clip(b.Raw, 64) + "`  (" + b.Where + ")  → op " + verbs);
                }
            }
        }
        Console.WriteLine();

        // ---------------------------------------------------------------- ⑥ 三个口径的换算
        Console.WriteLine("  --- ⑥ 三个口径在同一池上的读数与换算（`A1433` ① / ②）---");
        int scanRows = 0, scanUn = 0, scanPa = 0, scanWhole = 0;
        foreach (var r in rows)
        {
            if (string.IsNullOrEmpty(r.C.Desc)) continue;      // `Scan.cs:85` 跳过 desc 为空的卡
            scanRows++;
            EffectText.Parse(r.C.Desc, out var un, out var pa, false);
            if (un != null && un.Count > 0) scanUn++;
            if (pa != null && pa.Count > 0) scanPa++;
            if (un != null && un.Count == 1 && string.Equals(un[0].Trim(), r.C.Desc.Trim(),
                                                             StringComparison.OrdinalIgnoreCase)) scanWhole++;
        }
        Console.WriteLine("    (a) `scan` 的列（**原始口径 · 不过滤**）  unparsed 非空 " + scanUn
                          + " 行 · partial 非空 " + scanPa + " 行（合计 " + (scanUn + scanPa) + " / " + scanRows
                          + " 行；其中整条 desc 都是残渣的 " + scanWhole + " 行）"
                          + " ← 与 `check` 的「绝对读数」段同源（同一次 `Parse`，同一对 out 参数）");
        foreach (var t in types)
        {
            var cov = EffectText.Coverage(pool, t, pool);
            Console.WriteLine("    (b) `EffectText.Coverage` [" + Pad(t, 8) + "]（**过滤掉「别的层接手」之后**）  卡 "
                              + cov.Cards + " · 完全解析 " + cov.Full + " · 部分 " + cov.Partial
                              + " · 完全不懂 " + cov.None);
        }
        Console.WriteLine("    (c) `willrun`（**执行层**收没收到）  空表 " + rows.Count(r => r.RunOps.Count == 0)
                          + " · ① 本来没正文 " + noprose + " · ② 收漏 " + missed.Count
                          + " · ③ 未归因 " + unattr.Count + " · 段级没收下来 " + orphAll.Count + " 条"
                          + " · ⑤ 无头/" + KHNone + " " + headNone.Count + " 条");
        Console.WriteLine("    换算关系（**这是 `A1433` ① 要的**）：");
        Console.WriteLine("      · (a) 与 (b) 是**同一个解析器的两种口径**：(b) 把「已由别的层接手」的句子"
                          + "**先筛掉**再看");
        Console.WriteLine("        （`EffectText.cs:1755` 转调 `CardDef.HandledByOtherLayer`：`When <事件>,` / `Talent:` /");
        Console.WriteLine("         `Companion N:` / 光环 / 开局上手**五族不算不认识**）"
                          + "⇒ (a) 那 " + scanUn + " 行 `unparsed` 里绝大多数是**正常**类。");
        Console.WriteLine("        ⛔ **别把 (a) 当缺陷计数** —— 两个口径并排会看着像「" + scanUn + " 张卡坏了」"
                          + "（`A1433` ① 的病灶）。");
        Console.WriteLine("        ⚠️ 反过来，(b) 那四栏全 0 **也**不等于没问题：它**看不见** `A1386`"
                          + "（正文在 `keywords` 里、整条 `desc` 可能就是空的）。");
        Console.WriteLine("      · (b) 与 (c) 是**两条独立的腿**（(b) 走主解析器、(c) 走注册结果 + 卡面段）"
                          + "⇒ **互相看不见对方的洞**，**两条都要看**。");
        Console.WriteLine("      · (a) 与 (c) 也独立：(c) 的腿 B **不依赖主解析器的 unparsed 列**。");
        try
        {
            var unimpl = RuleCore.UnimplementedKeywords(pool);
            Console.WriteLine("    (d) 未实现关键词 = " + (unimpl == null ? 0 : unimpl.Count) + " 个"
                              + (unimpl != null && unimpl.Count > 0 ? "：" + string.Join(" · ", unimpl) : ""));
        }
        catch (Exception e) { Console.WriteLine("    (d) 未实现关键词：读不到（" + e.GetType().Name + "）"); }
        Console.WriteLine();

        // ---------------------------------------------------------------- dump
        if (!string.IsNullOrEmpty(dumpPath))
        {
            var sb = new StringBuilder();
            sb.Append("id\tname\ttype\trunOps\toldPass\tfaceBodies\torphans\temptyKind\torphanDetail\tnoMech"
                      + "\theadless\theadlessNone\theadlessDetail\n");
            foreach (var r in rows)
                sb.Append(r.C.Id).Append('\t').Append(r.C.Name).Append('\t').Append(r.C.Type)
                  .Append('\t').Append(r.RunOps.Count).Append('\t').Append(r.OldPass ? "1" : "0")
                  .Append('\t').Append(r.Bodies.Count).Append('\t').Append(r.Orphans.Count)
                  .Append('\t').Append(r.EmptyKind)
                  .Append('\t').Append(string.Join(" ‖ ", r.Orphans.Select(b => b.Kind + ":" + b.Raw)))
                  .Append('\t').Append(string.Join(" ‖ ", r.NoMech))
                  .Append('\t').Append(r.Headless.Count)
                  .Append('\t').Append(r.Headless.Count(b => b.Kind == KHNone))
                  .Append('\t').Append(string.Join(" ‖ ", r.Headless.Select(b => b.Kind + ":" + b.Raw)))
                  .Append('\n');
            var dir = Path.GetDirectoryName(Path.GetFullPath(dumpPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(dumpPath, sb.ToString());
            Console.WriteLine("  wrote " + dumpPath + " lines=" + (rows.Count + 1));
            Console.WriteLine();
        }

        Console.WriteLine("  判据：**②「收漏了」= 0 张**、段级 **" + KMiss + " / " + KPaid
                          + " 都为 0**、且 **⑤「" + KHNone + "」= 0 条** ⇒ rc=0；否则 rc=1（⛔ 不许静默绿）。");
        Console.WriteLine("  ⚠️「本来没正文」「未归因」「" + KTwice + "」「" + KHAtTurn + "」「" + KHCostWhen
                          + "」**不算失败**，但它们**也绝不混进「全通」**。");
        return (missed.Count > 0 || nMiss > 0 || nPaid > 0 || headNone.Count > 0) ? 1 : 0;
    }

    // ==================================================================
    //  一张卡的账
    // ==================================================================

    static Row Measure(CardDef c, List<string> rawKw, List<CardDef> pool)
    {
        var row = new Row { C = c };

        // ---- 腿 A：执行层收到了什么 ----
        row.RunOps = EffectText.WillRunOps(c) ?? new List<EffectOp>();
        row.OldPass = true;
        foreach (var op in row.RunOps)
        {
            string why; bool imprecise;
            if (EffectText.OpHasMechanism(op, c.Faction, pool, out why, out imprecise)) continue;
            row.OldPass = false;
            row.NoMech.Add((imprecise ? "打得宽" : "没机制") + "：" + why);
        }

        // ---- 腿 B：卡面上写着哪些「可执行正文」（**独立于腿 A**；只 unit/hero，见文件头）----
        if (c.Type == "unit" || c.Type == "hero")
        {
            var segs = EffectText.Split(c.Desc);
            for (int i = 0; i < segs.Count; i++)
                AddFaceBody(row.Bodies, c, segs[i], "desc 第" + (i + 1) + "句");
            var kw = rawKw ?? new List<string>();
            for (int i = 0; i < kw.Count; i++)
                AddFaceBody(row.Bodies, c, kw[i], "keywords[" + i + "]");

            // ---- 对账：卡面段里哪些**没被**执行层收到 ----
            var got = new HashSet<string>(row.RunOps.Select(Sig));
            foreach (var b in row.Bodies)
            {
                bool hit = false;
                foreach (var o in b.Ops) if (got.Contains(Sig(o))) { hit = true; break; }
                if (hit) continue;
                Classify(c, b);
                row.Orphans.Add(b);
            }

            // ---- 🆕 `A1459` 无头档：**卡面没有 `Head:`** 的段（腿 B 结构上抽不到的那一半）----
            //   判据链（缺一条都会误报，逐条见 `AddHeadless` / `ClassifyHeadless` 的注释）：
            //   ① 无头（`col <= 0`，与 `AddFaceBody` 拒收用的是**同一个表达式**）
            //   ② 主解析器对它解得出一批 op
            //   ③ 那批 op **一条都不在腿 A 里**（按 <see cref="Sig"/> 比，与腿 B 同一把尺子）
            //   ④ **没有任何别的层认领它**（`HandledByOtherLayer` / `AtTurnClauses` / `CostWhens`）
            var segsAll = EffectText.Split(c.Desc);
            for (int i = 0; i < segsAll.Count; i++)
                AddHeadless(row.Headless, c, segsAll[i], "desc 第" + (i + 1) + "句");
            var kwAll = rawKw ?? new List<string>();
            for (int i = 0; i < kwAll.Count; i++)
                AddHeadless(row.Headless, c, kwAll[i], "keywords[" + i + "]");
            var kept = new List<FaceBody>();
            foreach (var b in row.Headless)
            {
                bool hit = false;
                foreach (var o in b.Ops) if (got.Contains(Sig(o))) { hit = true; break; }
                if (hit) continue;                 // 腿 A 已经收到这一句 ⇒ 不归本档（那是「收到了」）
                ClassifyHeadless(c, b);
                kept.Add(b);
            }
            row.Headless = kept;
        }

        // ---- 空表分类（`A1432` 的正面判据）----
        if (row.RunOps.Count == 0)
        {
            if (row.Orphans.Count > 0) row.EmptyKind = "收漏了";
            else
            {
                var descOps = EffectText.Parse(c.Desc, out _, out _);
                row.EmptyKind = (descOps != null && descOps.Count > 0) ? "未归因" : "本来没正文";
            }
        }
        return row;
    }

    /// <summary>一条卡面段 → 若它是 `Head: body` 形状、且 `body` 主解析器解得出 op，收进 <paramref name="into"/>。
    /// ⚠️ **归一照 `CardDef.AddTriggerOp` 的口径**（`StripLeadingIcons` 之后再找第一个 `:`）——
    ///    卡面把触发关键词印成图标（`⚡ Rally: …`）时，不剥字形 `head` 会变成 `⚡ rally`。
    ///
    /// 🔴 **必须先问一句「这一句是不是已经由别的层接手了」**（转调 `CardDef.HandledByOtherLayer`）——
    ///    否则有**假警报**：`Talent: Deploy Anchors`（`TAU45 Stormsurge Battlesuit`）里
    ///    `Deploy Anchors` **恰好解得出一条 `deploy` op**，而它是**天赋名**、归
    ///    `TalentName` + `RuleCore.SpawnTalents` 那一层，**根本不是正文**（实测：不过这道闸时被误报）。
    ///    同族还有 `Companion N: <卡名>` / 光环 / 开局上手 —— 引擎自己有「谁接手」的判据，
    ///    ⛔ 别在探针里另写一套。</summary>
    static void AddFaceBody(List<FaceBody> into, CardDef c, string seg, string where)
    {
        if (string.IsNullOrEmpty(seg)) return;
        string s = EffectText.StripLeadingIcons(seg.Trim());
        if (string.IsNullOrEmpty(s)) return;
        int col = s.IndexOf(':');
        if (col <= 0) return;
        string head = s.Substring(0, col).Trim().ToLowerInvariant();
        string body = s.Substring(col + 1).Trim();
        if (head.Length == 0 || body.Length == 0) return;
        var ops = EffectText.Parse(body, out _, out _);
        if (ops == null || ops.Count == 0) return;                 // 解不出来的段不是「可执行正文」
        if (CardDef.HandledByOtherLayer(c, seg) != null) return;   // 已有别的层接手 ⇒ 不是「漏」
        // ⚠️ **同一段正文在 `desc` 和 `keywords` 里各印一遍是常态**（实测 `ASH32 Dire Avenger Exarch`
        //    两处都是 `Rally: All enemies lose Stealth and Camouflage`）⇒ 按「头 + 正文」去重，
        //    否则**同一个洞会被数两次**、把「收漏了几条」这个数灌水。
        //    ⛔ 去重键**不含 `where`**：段落在哪儿印的不改变它是一条正文这件事。
        string key = head + "\u0001" + body;
        foreach (var x in into) if (x.Head + "\u0001" + x.Text == key) return;
        into.Add(new FaceBody { Head = head, Text = body, Raw = s, Where = where, Ops = ops });
    }

    /// <summary>🆕 **`A1459`：收一条「无头段」** —— 卡面**没有 `Head:`**、而**主解析器**解得出一批 op 的段。
    ///
    /// 🔴 **它补的是哪道缝**：<see cref="AddFaceBody"/> 在 `col &lt;= 0`（= 这一句没有 `Head:`）时**直接 return**
    ///    ⇒ 「卡面没有头、又没被消费」的句子**永远不可能是腿 B 的段**、也就**永远不会进任何一档**。
    ///    实测后果（出处 `资料/普查产出_第十四会话/诊断_未归因24张.md` §④㈡）：`SW23 Hrolf the Ironhowl`
    ///    （`Friendly Beasts cost 1 less.`）/ `GSC71 Atalan Leader`（`Friendly Vehicles cost 1 less.`）/
    ///    `GSC36 Metamorph Leader`（`Your troops cost 1 less.`）**三张真缺陷**正是**因为这道缝**躲开了清单
    ///    —— 它们**有一条别的 op**（`Rally` / `Strike` / `Uprising`）⇒ 腿 A ≠ 0 ⇒ 连「③ 未归因」也进不去。
    ///
    /// ⚠️ **「无头」的判据 = `AddFaceBody` 拒收那一条的【同一个表达式】**（`s.IndexOf(':') &gt; 0` 才算有头）——
    ///    在这里另写一套「什么叫头」迟早会和腿 B 不一致（本仓红线：两处写同一条规则 = 迟早不一致）。
    /// ⚠️ **归一也照腿 B**（`StripLeadingIcons` 之后再找 `:`）—— 卡面把触发关键词印成图标时，
    ///    不剥字形 `Head` 会变成 `⚡ rally`（`AddFaceBody` 的注释里记着同一个坑）。
    /// ⚠️ **只对 `unit` / `hero` 走这一档**（与腿 B 同一个范围，理由同 README「三条别推翻的取舍」1）：
    ///    `tactic` / `defence` 的腿 A **就是** `Parse(c.Desc)` ⇒ 这一类卡的「无头句」早就被腿 A 收到了，
    ///    走这一档只会重复记账。
    /// ⚠️ **按段原文去重**（`desc` 与 `keywords` 里同一段各印一遍是常态，同 `AddFaceBody` 的去重理由）。</summary>
    static void AddHeadless(List<FaceBody> into, CardDef c, string seg, string where)
    {
        if (string.IsNullOrEmpty(seg)) return;
        string s = EffectText.StripLeadingIcons(seg.Trim());
        if (string.IsNullOrEmpty(s)) return;
        if (s.IndexOf(':') > 0) return;                    // 有 `Head:` ⇒ 归腿 B（同一表达式，见 summary）
        var ops = EffectText.Parse(s, out _, out _);
        if (ops == null || ops.Count == 0) return;         // 解不出来的段不是「可执行正文」
        foreach (var x in into) if (x.Raw == s) return;    // 按段原文去重
        into.Add(new FaceBody { Head = "", Text = s, Raw = s, Where = where, Ops = ops });
    }

    /// <summary>给一条「无头段」定性（**先问「有没有别人认领」，再问「解不解得出 op」**）。
    ///
    /// 🔴 **三道闸全部【转调引擎自己的判据】，一行新文法都不写** —— 理由与 `AddFaceBody` 转调
    ///    `HandledByOtherLayer` 完全一样（本仓红线：⛔ 别在探针里另写一套「谁接手」）：
    ///    1. `CardDef.HandledByOtherLayer` —— 事件层 / 天赋 / 伴生 / 开局上手 / 光环 / 静态改战斗规则；
    ///    2. `EffectText.AtTurnClauses` —— **回合起止从句**：它**不在** `WillRunOps` 里，
    ///       由 `EffectResolver.ResolveAtTurn:4792` 直接扫 `u.Card.Desc` 消费 ⇒ 无头、解得开、腿 A = 0，
    ///       **但它确实在跑**（`AM15 Master of Ordnance` 那一批就是）。
    ///       ⚠️ **不加这道闸会把它整整一族误报成「无消费点」**（实测：这是本档最容易误报的一处）；
    ///    3. `CardDef.CostWhens` 的 `Body` —— **降费触发器**那一层（`Lower cost by N every time …`，
    ///       `TL83 Norn Emissary`）：`_costWhens` **不经过 `WillRunOps`**（那个方法只收
    ///       `WhenTriggers`/`TriggerTexts`/`SpiritOps`/`OathOps`/`AttackedOps`/`AuraSpecs` 六个来源）
    ///       ⇒ 同样会「无头 + 解得开 + 腿 A = 0」，**而 `FireCostWhen` 在吃它**。
    ///       ⚠️ **不加这道闸会把 `TL83` 误报**。
    ///
    /// 三道闸都没认领 ⇒ <see cref="KHNone"/>：**这句卡面正文【没有任何一层会执行】**（不是「查不到」，
    ///   是「全仓唯一读 `u.Card.Desc` 的地方只有 `ResolveAtTurn`，而它只收回合起止从句」）。</summary>
    static void ClassifyHeadless(CardDef c, FaceBody b)
    {
        string layer = CardDef.HandledByOtherLayer(c, b.Raw);
        if (layer != null)
        {
            b.Kind = layer;
            b.Why = "由「" + layer + "」接手（判据转调 `CardDef.HandledByOtherLayer`）";
            return;
        }
        if (EffectText.AtTurnClauses(b.Raw).Count > 0)
        {
            b.Kind = KHAtTurn;
            b.Why = "回合起止从句 —— `ResolveAtTurn:4792` 直接扫 `u.Card.Desc`，"
                  + "**不经过 `WillRunOps`**（所以腿 A 里没有它，但它确实在跑）";
            return;
        }
        var cws = c.CostWhens;
        if (cws != null)
            foreach (var cw in cws)
                if (cw != null && string.Equals((cw.Body ?? "").Trim(), b.Raw,
                                                StringComparison.OrdinalIgnoreCase))
                {
                    b.Kind = KHCostWhen;
                    b.Why = "降费触发器（`CardDef.CostWhens`，由 `FireCostWhen` 消费）"
                          + " —— `WillRunOps` 的六个来源里**没有** `_costWhens`";
                    return;
                }
        b.Kind = KHNone;
        b.Why = "卡面写着、主解析器解得出 op，而腿 A 一条都没收到、"
              + "**也没有任何别的层认领它**（`HandledByOtherLayer` / 回合起止 / 降费触发器三道闸全空）";
    }

    /// <summary>给一条「没收下来」的段定性（三档，判据全在**引擎的公开面**上，⛔ 不新写文法）。</summary>
    static void Classify(CardDef c, FaceBody b)
    {
        // ① 同一个头**本卡注册过**（`TriggerTexts` 是公开面）⇒ 两份正文不一样，后者被「先到先得」丢了。
        //    实测（同一族三例）：`ASH79 Howling Banshee Exarch` 的 `Strike` 在 `desc` 里写
        //    `Give +1 [Attack] to your units`、在 `keywords` 里写 `Give +1 to your units`；
        //    `DA9 Ravenwing Bikes` 的 `Agenda` 在两处写 `Gain 1 Quest Point` / `Gain 1`。
        //    ⚠️ 这不是「空转」（效果照样由另一份跑）⇒ **单独一档、不计入 rc**。
        var tt = c.TriggerTexts;
        if (tt != null && tt.ContainsKey(b.Head))
        {
            b.Kind = KTwice;
            b.Why = "这个头本卡注册过（`TriggerTexts[\"" + b.Head + "\"]` = `" + Clip(tt[b.Head], 50)
                  + "`）—— `desc` / `keywords` 两份正文**不一样**，后者被先到先得丢掉";
            return;
        }

        // ② `N ☀:` / `N [Energy]:` / `N [Spirit Stone]:` 形状 —— 「付 N 点换这个效果」的**付费能力段**。
        //    🔴 它**不是触发头**，`WillRunOps` 的 unit/hero 那一支也不看它
        //    （六个来源：`WhenTriggers` / `TriggerTexts` / `SpiritOps` / `OathOps` / `AttackedOps` / `AuraSpecs`）
        //    ⇒ 这是**覆盖面**的洞，不是「表里缺词」。
        if (System.Text.RegularExpressions.Regex.IsMatch(b.Head, @"^\d+\s*[\[\(]?\s*[a-z☀]"))
        {
            b.Kind = KPaid;
            b.Why = "付费能力段（`N ☀:` / `N [Energy]:` 形状）—— `WillRunOps` 的 unit/hero 支不收这类段";
            return;
        }

        // ③ 其余 = **触发头没进 `TriggerTexts`** ⇒ `A1386` 那一族。
        b.Kind = KMiss;
        b.Why = "触发头 `" + b.Head + ":` 没进本卡的 `TriggerTexts`"
              + "（`AddTriggerOp` 要求它在 `CardDef.RoutableTriggers` 里）";
    }

    static string Pad(string s, int w)
    {
        // 中文按 2 列宽算（终端里才对得齐）
        int vis = 0;
        foreach (char ch in s) vis += ch > 0x2000 ? 2 : 1;
        var b = new StringBuilder(s);
        while (vis < w) { b.Append(' '); vis++; }
        return b.ToString();
    }

    static string Clip(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}
