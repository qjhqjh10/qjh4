// RuleEngineTest_S23_Parse.cs — 第二十三会话（写手 `W1`）· **解析层两笔账**：
//   · `A1686` —— 前缀形 `When <事件>, <效果>` 走主解析器会解出**垃圾 op**
//   · `A1446` —— `Lower cost by 2 when <事件> instead` 解析层认了、注册层没收（`CostWhens.Count` = 0）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（已有兄弟
// `RuleEngineTest_S14.cs` / `_Diag.cs` / `_BoardModel.cs`），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批一律写在这里，`RuleEngineTest.cs` **一个字都没动**（`Run()` 里的 `Section` / `Step` 由调度台挂）。
//
// 🔴 **期望值的取法**（铁律 2 / 铁律 10）：先查原版反编译 / 解包字段，**再**用离线探针
//    （`工具/ruleprobe`：只编 `RuleEngine/Core/`，不跑 Unity）把每一格**实测**一遍，然后才写进断言。
//    本文件里凡带「改前实测」「改后实测」的读数，命令都是
//    `bash d:/4/Unity/工具/ruleprobe.sh seg "<那句话>"` / `… card "<卡名>"` / `… scan <out>`，
//    逐条抄在 `资料/普查产出_第二十三会话/W1_解析层A1686A1446.md` 里 —— ⛔ 期望值**不是**从实现读回来的。
//
// 🔴 **两个记账口**：本文件**只读**（解析层是纯函数、不碰棋盘），没有任何对局动作 ⇒ 不涉及录像。
//
// ⚠️ 本文件**只写断言**、不改引擎；`Run()` 的挂载由调度台做 —— 要挂的两个方法名：
//      `TestS23ParseWhenPrefix` · `TestS23ParseCostWhenInstead`（都是 `static void`、不收形参）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  本批专用的小工具（名字一律带 `S23` 前缀，⛔ 不与既有私有辅助重名）
    // ==================================================================

    /// <summary>`ops` 里有没有 `whenclause`（**标记 op**：机制在事件层，见 `EffectText.TryWhenPrefixStub`）。</summary>
    static bool S23HasWhenClause(List<EffectOp> ops)
    {
        if (ops == null) return false;
        foreach (var o in ops) if (o != null && o.Verb == "whenclause") return true;
        return false;
    }

    /// <summary>第一处 `whenclause` 的下标（`-1` = 没有）。⛔ 不用 `[]` 直接取 —— 缺了就抛异常，
    /// 会把「一条断言失败」变成「整轮断在这儿」（`RuleEngineTest.Run` 没有逐条 try/catch）。</summary>
    static int S23WhenClauseAt(List<EffectOp> ops)
    {
        if (ops == null) return -1;
        for (int i = 0; i < ops.Count; i++)
            if (ops[i] != null && ops[i].Verb == "whenclause") return i;
        return -1;
    }

    /// <summary>目标段（`Target` / `Target2`）的 `Raw` —— 没有目标时返回 `""`。
    /// 用途：`A1686` 的病灶**正是**「事件从句被当成了目标段」，所以要能逐条读出那一栏。</summary>
    static string S23TargetRaw(EffectOp o, bool second = false)
    {
        if (o == null) return "";
        var t = second ? o.Target2 : o.Target;
        return t == null || t.Raw == null ? "" : t.Raw;
    }

    /// <summary>`Raw` 是不是「事件从句被吃进来了」（`when …` 开头）。</summary>
    static bool S23RawIsWhenClause(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;
        string s = raw.TrimStart().ToLowerInvariant();
        return s.StartsWith("when ") || s.StartsWith("whenever ");
    }

    // ==================================================================
    //  `A1686` —— 前缀形 `When <事件>, <效果>` 不许解出垃圾 op
    //
    //  病灶（改前实测，`ruleprobe seg`）：
    //    `When an enemy gains Markerlight, gain +1 Attack`
    //    ⇒ `verb=gain payload=[markerlight, gain +1 attack] target=[when an enemy]`
    //      —— `ReGain` 的非贪婪主语把**事件从句整个当成了目标段**。
    //  全池同类实测 **13 张**的 `Target.Raw` 就是 `when …`（`scan` dump 的 `T:Raw=when` 那一栏）。
    //
    //  做法：与**后缀形**（`A1461` 的 `TryWhenSuffix`）对称 —— `Dispatch` 的 **8c-ter**
    //    落一条 `whenclause` **标记 op**，切句判据**转调** `CardDef.TryParseWhenSentence`
    //    （注册层 `AddWhenTrigger` ① 与报表层 `HandledByOtherLayer` 用的是**同一个函数**，
    //     ⛔ 没在 `EffectText` 里另写一套切句）。
    // ==================================================================
    static void TestS23ParseWhenPrefix()
    {
        // ---- ① 正例（**量载荷，不量「有没有 op」**）----
        //   ⚠️ 改前它也「解得出 op」（1 条 `gain`）⇒ 只断 `Count > 0` 的话**改前改后都绿**。
        //   ⇒ 断的是**三个会一起变的东西**：动词 / 载荷 / 目标段。改前那条 op 三个全不一样。
        {
            const string seg = "When an enemy gains Markerlight, gain +1 Attack";
            var r = EffectText.ParseSegment(seg);
            Check(r.Kind, EffectText.SegKind.Ok, $"`{seg}` 认得出（`A1686` 正例）");
            int at = S23WhenClauseAt(r.Ops);
            CheckTrue(at >= 0, $"★ 产出的是 `whenclause` **标记 op**（动词改前是 `gain`）");
            if (at >= 0)
            {
                Check(r.Ops[at].Payload, "gain +1 Attack",
                      "★ 载荷 = **效果那半句原文**（改前是 `markerlight, gain +1 attack`，从句被一起吃进去了）");
                Check(S23TargetRaw(r.Ops[at]), "",
                      "★ **灭自证**：目标段必须是空的 —— 改前那条 op 的 `Target.Raw` 就是 "
                      + "`when an enemy`（事件从句被当成了目标段），而这就是「给敌人发标记光」的来源");
            }
        }

        // ---- ② 正例 · 真卡（`BL20 Child of Torment`，卡面亲读的那一句）----
        //   改前实测：`verb=gain payload=[+1 ranged] target={T:Raw=«when a unit receives damage,» …}`
        //   —— 载荷其实是对的，**错的只有目标段**（Kind=unit / Side=any / Count=1）
        //   ⇒ 这一格的判别式**只有 `Target.Raw`**，所以单独钉一条。
        {
            const string seg = "When a unit receives damage, gain +1 [Ranged] this turn";
            var r = EffectText.ParseSegment(seg);
            int at = S23WhenClauseAt(r.Ops);
            CheckTrue(at >= 0, $"★ `{seg}` 也走同一条路（真卡 `BL38`）");
            if (at >= 0)
                Check(S23TargetRaw(r.Ops[at]), "",
                      "★ 目标段空的 —— 改前它是 `when a unit receives damage,`（**从句整个被吃成目标**）");
        }

        // ---- ③ 反例 · 多段互不吃掉（`DA15 Ravenwing Champion` 的真实形状）----
        //   `Rally: Gain 1 Quest Point. When you create a Secret, deal 3 damage to a random enemy`
        //   实测：2 条 op = `gainquest` ＋ `whenclause` ⇒ 前缀形**只领走它自己那一段**。
        {
            const string seg =
                "Rally: Gain 1 Quest Point. When you create a Secret, deal 3 damage to a random enemy";
            var r = EffectText.Parse(seg, out _, out _);
            Check(r == null ? 0 : r.Count, 2, $"`{seg}` 解出 2 条 op（`Rally` 那条不许被前缀形吃掉）");
            CheckTrue(r != null && r.Count == 2 && r[0].Verb == "gainquest" && r[1].Verb == "whenclause",
                      "★ 第 1 条仍是 `gainquest`、第 2 条才是 `whenclause`（两条各归各）");
        }

        // ---- ④ 反例（**最重要**）· `When played, …` 必须继续走主解析器 ----
        //   判据 = 原版语义（`Core/WhenEvent.cs` 的 `SelfOnly` 那段，2026-10-17 查实）：
        //   原版「打出一张牌」这条广播对**打出那张牌自己**的意义就是「**结算我自己的正文**」
        //   （`CardScript__ResolveCardPlayed.c` / `__NonTargetSpellPlayed.c` 传的就是自己），
        //   ⇒ `CardDef.CanListenForEvents` 那句注释也点明「给 `played` 单开一条 = 造一条永远不响的监听器」。
        //   实据：战术卡 `DA42 Reconnaissance Mission` 的 desc 第 2 段。
        //   ⚠️ 本条的**闸**是第 ② 道：`played` 省主语 ⇒ `WhenEvents.ParseAll` 认不出 ⇒ `TryParseWhenSentence` 为假
        //      ⇒ 我的新分支**天然收不到它**，它照旧落到 `TryGain` 那条老路（`gainquest 3`）。
        {
            const string seg = "When played, gain 3 Quest Points";
            var r = EffectText.ParseSegment(seg);
            CheckTrue(r.Ops != null && r.Ops.Count > 0, $"`{seg}` 认得出（`When played` 不走事件层）");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "gainquest", "★ 仍是 `gainquest`（⛔ 不许变成 `whenclause`）");
                Check(r.Ops[0].Amount, 3, "★ 数值 3 保住");
            }

            // 端到端口径：真卡整条 desc 也不许出现 `whenclause`
            var pool = CardDatabase.Load();
            var rm = CardDatabase.Find(pool, "Reconnaissance Mission");
            CheckTrue(rm != null, "卡池里找得到 `Reconnaissance Mission`（`DA42`）");
            var all = rm == null ? null : EffectText.Parse(rm.Desc, out _, out _);
            CheckTrue(!S23HasWhenClause(all),
                      "★ **灭自证**：`DA47` 的整条 desc 里**一条 `whenclause` 都不许有** —— "
                      + "否则「当其被打出时获得 3 点任务」这句**不再结算**（卡面印着、机制静默不发生）");
            CheckTrue(all != null && all.Count == 2 && all[0].Verb == "choosecard" && all[1].Verb == "gainquest",
                      "★ 而且它仍是 `choosecard` ＋ `gainquest` 两条（与改前逐字相同）");
        }

        // ---- ⑤ 反例 · 第 ③ 道闸：正文解析不出 ⇒ 认不出（**宁可判 `unparsed`，不静默收成标记**）----
        //   实测改后读数：`unparsed=When an enemy dies, wibble the wobble`、无 op。
        {
            const string seg = "When an enemy dies, wibble the wobble";
            var r = EffectText.ParseSegment(seg);
            CheckTrue(!S23HasWhenClause(r.Ops),
                      $"★ `{seg}` **不许**被收成 `whenclause`（正文「wibble the wobble」解不出 op）");
            Check(r.Kind, EffectText.SegKind.Unknown, "★ 照实判「认不出」（静默收下 = 骗玩家）");
        }

        // ---- ⑥ 反例 · 第 ② 道闸：事件短语认不出 ⇒ 不收（判「半懂」，也是响的）----
        //   实测改后读数：`partial=When the moon rises, gain 1 Attack`，op 是 `gain`（**不是** `whenclause`）。
        {
            const string seg = "When the moon rises, gain 1 Attack";
            var r = EffectText.ParseSegment(seg);
            CheckTrue(!S23HasWhenClause(r.Ops),
                      $"★ `{seg}` 不许被收成 `whenclause` —— 事件短语「the moon rises」"
                      + "`WhenEvents` 认不出，收下就是**注册一条永远不响的监听器**");
        }

        // ---- ⑦ 全池扫描（**结构性**，不指名单张卡）：任何 op 都**不许**把事件从句当目标段 ----
        //   改前：**13 张**卡的 `Target.Raw` 以 `when ` 开头（`scan` dump 的 `T:Raw=when`）。
        //   改后：**0 张**。⇒ 这条断言盯的正是病灶本身，改回去立刻红。
        {
            var pool = CardDatabase.Load();
            int bad = 0; string firstBad = null;
            foreach (var c in pool)
            {
                var ops = EffectText.Parse(c.Desc, out _, out _);
                if (ops == null) continue;
                foreach (var o in ops)
                {
                    if (S23RawIsWhenClause(S23TargetRaw(o)) || S23RawIsWhenClause(S23TargetRaw(o, true)))
                    {
                        bad++;
                        if (firstBad == null) firstBad = c.Name + "｜" + S23TargetRaw(o) + "｜" + S23TargetRaw(o, true);
                        break;
                    }
                }
            }
            Check(bad, 0, "★ **全池 1126 张**：没有任何 op 的目标段以 `when ` 开头"
                          + "（改前 13 张；这条红了就把 `Dispatch` 的 8c-ter 撤了）"
                          + (firstBad == null ? "" : $" —— 第一张 = {firstBad}"));
        }

        // ---- ⑧ 全池扫描 · **爆炸半径**：非单位卡里只许有 1 张被这段领走，且那张是手牌陷阱 ----
        //   为什么单钉这一条：前缀形在**单位卡**上「解出的 op 没人执行」（`WillRunOps` 只读注册层），
        //   但在**战术卡**上 `PlayTactic` → `Parse(Desc)` **会真执行** ⇒ 若被误领，效果会静默消失。
        //   实测：全池战术/防御卡里**只有 `68909cf02f472734ea391665e0d28e15`** 一张（它是**手牌陷阱**，
        //   效果走 `HandTrapWhens` + `BroadcastHandTrapWhen`，与主解析器无关）。
        {
            var pool = CardDatabase.Load();
            var hits = new List<string>();
            foreach (var c in pool)
            {
                if (c.Type != "tactic" && c.Type != "defence") continue;
                var ops = EffectText.Parse(c.Desc, out _, out _);
                if (S23HasWhenClause(ops)) hits.Add(c.Id);
            }
            Check(hits.Count, 1, "★ 非单位卡里「解析出 `whenclause`」的**只许 1 张** —— "
                                 + "多了说明有战术卡的效果被静默吃掉（实得：" + string.Join(",", hits) + "）");
            if (hits.Count == 1)
            {
                Check(hits[0], "68909cf02f472734ea391665e0d28e15", "★ 而且必须是那张手牌陷阱");
                var jc = CardDatabase.Find(pool, "Jammed Communications");
                CheckTrue(jc != null && EffectText.IsHandTrap(jc),
                          "★ 它确实是手牌陷阱（`IsHandTrap`）⇒ 效果由 `HandTrapWhens` 那条路结算，没丢");
            }
        }

        // ---- ⑨ 全池扫描 · **不许出现「收下但没人消费」**（本工程红线）----
        //   判据**转调** `CardDef.HandledByOtherLayer`（解析层/报表层/注册层共用同一处判据）。
        //   ⚠️ 这条**不是我推的**，是**量出来的**：把卡池换成「只有 9 张这样的卡」的最小池
        //      （`WFPROBE_POOL=… ruleprobe willrun`）⇒ `⑤ 无消费点 0 条` · 且 `事件层（WhenTriggers）9 条`；
        //      全池读数同样 `⑤ 无消费点 0 条`（改前 13 张的 `T:Raw=when` 那批本来就在这一栏）。
        {
            var pool = CardDatabase.Load();
            int n = 0; string firstUnclaimed = null;
            foreach (var c in pool)
            {
                if (!c.IsUnit) continue;
                foreach (var seg in EffectText.Split(c.Desc))
                {
                    if (string.IsNullOrEmpty(seg)) continue;
                    var r = EffectText.ParseSegment(seg);
                    if (!S23HasWhenClause(r.Ops)) continue;
                    n++;
                    if (CardDef.HandledByOtherLayer(c, seg) == null)
                    {
                        n = -1;                      // 只要有一处没被认领，这条就该红
                        if (firstUnclaimed == null) firstUnclaimed = c.Name + "｜" + seg;
                        break;
                    }
                }
                if (n < 0) break;
            }
            CheckTrue(n >= 0,
                      "★ 每一个「前缀形」句子都**必须**由事件层认领（`HandledByOtherLayer` 非空）—— "
                      + "否则那条 `whenclause` 就是「注册了一条不响的监听器」的同义词"
                      + (firstUnclaimed == null ? "" : $" —— 没被认领的第一处 = {firstUnclaimed}"));
            CheckTrue(n > 0, "★ 而且确实扫到了这类句子（`n = " + n + "` —— 若为 0 说明扫描口径失效，"
                             + "这条断言就变成了**恒真的空断言**）");
        }
    }

    // ==================================================================
    //  `A1446` —— `Lower cost by 2 when <事件> instead`：解析层认了、注册层没收
    //
    //  改前实测（`资料/普查产出_第十四会话/W_A1435十一处贴点.md` §三·1 的表格）：
    //    | 句子 | 解析层 | `CardDef.CostWhens.Count` | `WhenEvents.Parse(事件短语)` |
    //    | `… when a friendly unit triggers Synapse`        | `costwhen` | **1** | `triggers:synapse` |
    //    | `… when a friendly unit triggers Synapse instead` | `costwhen` | **0** | **null** |
    //  ⇒ 正是 `CardDef.AddWhenTrigger` 那条注释点名的老坑「解析器认了、卡面不生效」。
    //
    //  做法：两半走**同一个剥离口 + 同一份抽取判据** —— `EffectText.TryCostWhen`
    //    （内部先 `PeelInstead` 摘句尾的 ` instead`，再切 `when` / `every time`）；
    //    解析层 `TryCostWhenStub` 与注册层 `CardDef.AddWhenTrigger` ② 都转调它。
    //
    //  ⚠️ **`instead` 的【结算语义】判据空缺，如实标着**（铁律 2 / 3）：全池 1126 张 0 张是这个形状 ·
    //     `card_stats.json`（1213 张）0 命中 · 原版 `CardEffect`/`HandEffect`/`ChooseValueOptions` 无该字段 ·
    //     全量反编译无 `instead` 字面 ⇒ 只把那个词**摘下来如实记着**（`EffectOp.Instead` /
    //     `CardDef.CostWhen.Instead`），⛔ **不据此改结算**（降价仍按 `Delta` 生效）。
    // ==================================================================
    static void TestS23ParseCostWhenInstead()
    {
        // ---- ① 解析层：两条都认，而 `Instead` 旗标分得开 ----
        //   `ruleprobe seg` 读数（改前/改后同）⇒ `verb=costwhen amount=2`。
        {
            var r1 = EffectText.ParseSegment("Lower cost by 2 when a friendly unit triggers Synapse instead");
            Check(r1.Kind, EffectText.SegKind.Ok, "`… instead` 那一句解析层认得出");
            CheckTrue(r1.Ops != null && r1.Ops.Count == 1 && r1.Ops[0].Verb == "costwhen",
                      "★ 动词 = `costwhen`（标记 op）");
            if (r1.Ops != null && r1.Ops.Count == 1)
            {
                Check(r1.Ops[0].Amount, 2, "★ 降 2 费");
                CheckTrue(r1.Ops[0].Instead, "★ 旗标 `Instead` = true（**那个词不许被静默丢掉**）");
            }

            var r0 = EffectText.ParseSegment("Lower cost by 2 when a friendly unit triggers Synapse");
            CheckTrue(r0.Ops != null && r0.Ops.Count == 1 && r0.Ops[0].Verb == "costwhen",
                      "★ 反例 · **不带 `instead`** 的那一支逐字不变（动词仍是 `costwhen`）");
            if (r0.Ops != null && r0.Ops.Count == 1)
            {
                Check(r0.Ops[0].Amount, 2, "★ 降 2 费（对照）");
                CheckTrue(!r0.Ops[0].Instead, "★ 对照那条的 `Instead` = false（旗标不许恒真）");
            }
        }

        // ---- ② 注册层（**本条的验收本体**）：`CostWhens.Count` 由 0 变 1 ----
        //   ⚠️ 期望值**不是**从实现读回来的：先用**另一条尺子**量过 ——
        //      往卡池副本里塞 3 张夹具（本条这种形状 ×2 ＋ 对照 ×1），
        //      跑 `WFPROBE_POOL=… ruleprobe willrun` ⇒ 「降费触发器」那一栏 **1 条 → 3 条**
        //      （= `CardDef.CostWhens` 认领），且 `⑤ 无消费点` 仍 **0 条**、`rc=0`
        //      ⇒ **`instead` 那一条**真的进了 `CostWhens`（若没进，它会掉进「无消费点」并让 rc=1）。
        {
            var t1 = Tactic("T_S23CostWhenInstead", 5,
                            "Deal 1 damage to an enemy. Lower cost by 2 when a friendly unit triggers Synapse instead");
            Check(t1.CostWhens.Count, 1, "★ `… instead` 那一句**注册上了**（改前 = 0 —— 判据空缺那条老坑）");
            if (t1.CostWhens.Count == 1)
            {
                Check(t1.CostWhens[0].Delta, 2, "★ 降 2 费（数值走的是同一份抽取判据）");
                CheckTrue(t1.CostWhens[0].Instead, "★ 旗标也传到了监听器上（`CostWhen.Instead`）");
            }

            var t0 = Tactic("T_S23CostWhenPlain", 5,
                            "Deal 1 damage to an enemy. Lower cost by 2 when a friendly unit triggers Synapse");
            Check(t0.CostWhens.Count, 1, "★ 反例 · **不带 `instead`** 的那一支没被破坏（一直是 1）");
            if (t0.CostWhens.Count == 1)
                CheckTrue(!t0.CostWhens[0].Instead, "★ 对照那条的旗标 = false");
        }

        // ---- ③ 真卡对照：真池里那几张降费卡**逐张不受影响** ----
        //   全池 `desc` 含 `Lower cost by` 的 5 张（现读，`cards_engine.json`）：
        //     `AM60` / `AM61` / `TAU66` / `TL70` / `UM40`。
        //   ✅ **2026-10-13（第二十五会话 · `A1689`）`UM40` 已经注册上了** —— 它原来
        //      **不在**下面这张名单里，它那句
        //      `Lower cost by 1 when a friendly Troop or Stratagem trigger [Codex icon] Codex`
        //      认不出，三个卡点：**动词不带 `s`**（`… stratagem trigger codex …`）· **图标记号
        //      `[Codex icon]` 被剥成裸字 `codex icon`** · **并列主语 `troop or stratagem` 不是兵种词**
        //      （退化成一个永不匹配的卡名）。修补点 = `Core/WhenEvent.cs` 三处（`Clean` 整块丢图标记号 ·
        //      `TryParseKeywordTrigger` 收中缀不带 `s` · `Subjects.Parse` 拆并列兵种词）。
        //      🔴 **原来这里写「它落在 ⑤ 无消费点栏里」【不成立】**（2026-10-13 现核）：⑤ 那道闸
        //      **只对 `unit`/`hero` 做**，而它是 `tactic` ⇒ 它**从来没进过**那一栏（铁律 5 就地订正）。
        //   ⚠️ 下面 4 张**量过**（同一把尺子）：把它们的**原句**塞进卡池副本当夹具跑 `willrun`
        //      ⇒ 「降费触发器」那一栏 **1 条 → 4 条**（1 = `TL70`，＋ 这 3 句）。
        //      `UM40` 现在**一并列进来**（改前它会让 `CostWhens.Count >= 1` 直接红）。
        {
            var pool = CardDatabase.Load();
            var expect = new[]
            {
                "Mechanised Infantry",      // AM60
                "Vengeance for Cadia",      // AM61
                "Burgeoning Empire",        // TAU66
                "Norn Emissary",            // TL70（全池唯一落「降费触发器」栏的那张）
                "Avenging Zeal",            // UM40（`A1689` 修好之后才进得来 —— 见上面那段）
            };
            foreach (var name in expect)
            {
                var c = CardDatabase.Find(pool, name);
                CheckTrue(c != null, $"卡池里找得到「{name}」");
                if (c == null) continue;
                CheckTrue(c.CostWhens.Count >= 1, $"★ 真卡「{name}」的降费监听器仍在（改这笔之前就在）");
                foreach (var cw in c.CostWhens)
                    CheckTrue(!cw.Instead, $"★ 而且它的 `Instead` = false（这几张卡面都没有 `instead`）");
            }
        }

        // ---- ④ 全池扫描：`Instead` 旗标**不许恒真** ----
        //   真池里 `desc` 含 `instead` 的 14 张**全是替换值那一族**（`Deal 3 … deal 6 instead`），
        //   **0 张**是降费族 ⇒ 全池所有 `CostWhen.Instead` 必须**全是 false**。
        //   这条同时挡住「把 `PeelInstead` 接到降费上时顺手把旗标写成恒真」。
        {
            var pool = CardDatabase.Load();
            int n = 0, bad = 0;
            foreach (var c in pool)
            {
                foreach (var cw in c.CostWhens)
                {
                    n++;
                    if (cw.Instead) { bad++; if (bad == 1) CheckTrue(false, "★ 出现 `Instead=true` 的监听器：「" + c.Name + "」"); }
                }
            }
            Check(bad, 0, $"★ 全池 {n} 条降费监听器里，带 `instead` 的必须 **0** 条（真池里没有这种卡）");
            CheckTrue(n >= 4, $"★ 而且确实扫到了若干条（`n = {n}`）—— 若为 0 就是**恒真的空断言**");
        }
    }

    /// <summary>
    /// **降费句里的【图标记号】与【并列主语】**（2026-10-13 · 账 `A1689`）。
    ///
    /// 验收本体 = 真卡 `UM40` 那句
    /// `Lower cost by 1 when a friendly Troop or Stratagem trigger [Codex icon] Codex`
    /// **注册进 `CardDef.CostWhens`**（改前 = 0 —— 卡面印着降费、机制静默不发生）。
    /// 三个卡点：① 动词**不带 `s`**（`… stratagem trigger codex …`）② 图标记号 `[Codex icon]`
    /// 被上游剥成裸字 `codex icon` ⇒ 关键词归一成 `codexiconcodex` ③ 并列主语
    /// `troop or stratagem` 不是兵种词 ⇒ 退化成永不匹配的卡名。修法 = `Core/WhenEvent.cs` 三处。
    ///
    /// 🔴 **灭自证**：只断「注册上了」不够 —— 若把闸整个删掉 / 放宽，下面 ②③ 会一起变绿。
    ///   所以同时钉住：② 假关键词仍然不收 · ③ `[Shield]` 那种「**括号里就是词**」的记号不许被整块吃掉 ·
    ///   ④ 主语那一维的**三向判别式**（`troop` 命中 / `stratagem` 命中 / **督军不命中**）。
    /// </summary>
    static void TestS23ParseCostWhenIconMarker()
    {
        // ---- ① 注册层（验收本体）----
        {
            var t = Tactic("T_A1689Zeal", 12,
                "Give +2 [Attack] and +2 [Ranged] to all your units in play and in hand. "
                + "Lower cost by 1 when a friendly Troop or Stratagem trigger [Codex icon] Codex");
            Check(t.CostWhens.Count, 1, "★ `[Codex icon]` 那一句**注册上了**（改前 = 0）");
            if (t.CostWhens.Count == 1)
            {
                var cw = t.CostWhens[0];
                Check(cw.Delta, 1, "★ 降 1 费");
                Check(cw.Ev != null ? cw.Ev.Kind : null, "triggers:codex", "★ 事件种类 = 典籍被触发");
                if (cw.Ev != null)
                {
                    Check(cw.Ev.OwnerIs, WhenEvent.RelFriendly, "★ 方向 = 本方（卡面写的是 `friendly`）");
                    var crit = cw.Ev.Criteria;
                    CheckTrue(crit != null, "★ 主语那一维**不是空的**（退化版会落 `Name`，不是 null）");
                    if (crit != null)
                    {
                        CheckTrue(crit.Matches(Unit("FxTroop", 1, 1, 1)), "★ 命中部队");
                        CheckTrue(crit.Matches(Tactic("FxStrat", 1, "")),
                                  "★ 命中战术大类（`or` 那半必须**真进筛选**）");
                        CheckTrue(!crit.Matches(Hero("FxHero", 1, 1)),
                                  "★ **督军不算** —— `troop` 是真筛选，⛔ 不许把它当「不限」"
                                  + "（三种改法里只有「并集」这一版是 [T, T, F]）");
                    }
                }
            }
        }

        // ---- ② 两道闸不许被放宽：关键词不在 `Implemented` ⇒ 仍然不收 ----
        {
            var bad = Tactic("T_A1689Nonsense", 5,
                "Lower cost by 1 when a friendly Troop or Stratagem trigger [Codex icon] Nonsenseword");
            Check(bad.CostWhens.Count, 0, "★ 改后仍然**挡得住**假关键词（⛔ 不许为了让它亮而放宽那道闸）");
        }

        // ---- ③ 两种图标记号形态都要吃；`[Shield]` 不许被整块吃掉 ----
        {
            CheckTrue(WhenEvents.Parse("a friendly Troop or Stratagem trigger [Codex icon] Codex") != null,
                      "★ 带方括号那一路认得出");
            CheckTrue(WhenEvents.Parse("a friendly troop or stratagem trigger codex icon codex") != null,
                      "★ 已脱方括号那一路（降费那一支交出来的形态）也认得出");
            var sh = WhenEvents.Parse("a friendly unit with [Shield] dies");
            CheckTrue(sh != null && sh.Criteria != null && sh.Criteria.Keyword == "shield",
                      "★ 反例 · `[Shield]` 这种「**括号里就是词**」的记号**不许**被整块吃掉"
                      + "（只有以 ` icon` 结尾的图标记号才丢）");
        }

        // ---- ④ 真卡对照（挡「夹具能过、真卡不行」）----
        {
            var pool = CardDatabase.Load();
            var c = CardDatabase.Find(pool, "Avenging Zeal");
            CheckTrue(c != null, "卡池里找得到「Avenging Zeal」");
            if (c != null)
            {
                Check(c.CostWhens.Count, 1, "★ 真卡上那条监听器在（改前 = 0）");
                if (c.CostWhens.Count == 1)
                    Check(c.CostWhens[0].Delta, 1, "★ 降 1 费");
            }
        }
    }
}
