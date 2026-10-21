// RuleEngineTest_Priority.cs — 引擎线 `T03·c` 批 B（第 1 步）：**四道动作队列（`ActionQueue`）** 的断言
// （2026-10-21 第六十一会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_Order` / `_BoardModel` / `_S14` / `_S23_*` / `_Diag` 都在），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(...)`**。
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1021/W61_引擎线_T03c批B判据.md`：
//   · §2.1 枚举 `ActionPriority { Player = 0, Instant = 1, Interrupt = 2, DelayedInstant = 3 }`
//     （`ActionPriority.cs:1-7`；默认 1 = Instant，`CardAbility__ctor.c:5`）。
//   · §2.2 分道（`AddAutoActionToQueue.c`）：0/1/2/3 → 四条独立队列；非法值 `LogError` **丢弃**。
//   · §2.3 消费序（`Update.c:1284-1299` / `NextActionInQueue.c:11-28`）：
//     **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**；同道 FIFO（`List.Add` +
//     `get_Item(0)` + `RemoveAt(0)`）。
//   · §5 本文件覆盖的断言编号：**T1 / T2 / T3 / T8 / T9 / T13**
//     （T4 不抢占、T5–T7 与 PlayOrder/默认值、T10–T12 死亡链与回合末 —— 要等队列接线，另批）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 **期望值一律写死字面量**（`0/1/2/3`、`"2,1,3,0"`）—— ⛔ 不从 `ActionQueue` 的实现常量反算
//    （那会自证：实现改错、期望跟着错，永远绿）。
//
// ⚠️ 本批**不接线**（`RuleCore` / `EffectResolver` 仍同步递归）⇒ 这里只测**原语**，不碰对局。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>
    /// 本批的**唯一入口**（`Run()` 里一行 `Step(TestW61ActionQueue)`）。
    /// 六个子用例是 `W61…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。
    /// </summary>
    public static void TestW61ActionQueue()
    {
        Section("🆕 `W61` · `T03·c` 批 B：四道动作队列（`ActionQueue` 原语）");
        W61T1EnumValues();
        W61T2LaneOrder();
        W61T3SameLaneFifo();
        W61T8InterruptJumpsQueue();
        W61T9DelayedInstantLast();
        W61T13InvalidPriority();
        // 🆕 2026-10-21（接线第一步）：数据层那一跳（`cards_engine.json` v13 `triggerPriority`
        //   → `CardDatabase.Parse` → `CardDef._triggerPriority`）—— 上面六条测的是原语，这条测**接线**。
        W61T14DataTriggerPriority();
    }

    // ==================================================================
    //  T1 · 枚举数值（字面量 0/1/2/3）
    // ==================================================================
    /// <summary>
    /// **枚举数值必须逐字是 0/1/2/3** —— 它是**数据里的字面量**
    /// （`cards_engine.json` 的 `triggerPriority[].priority` 与 `CardAbility.priority` 同一套）。
    /// 🧨 把 `Interrupt` 与 `DelayedInstant` 换个位置、或整体 +1，本条必红。
    /// ⛔ 期望值写死（`(int)Enum.X` 去比 `Enum.X` 那种写法**恒真**，等于没测）。
    /// </summary>
    static void W61T1EnumValues()
    {
        Check((int)ActionPriority.Player, 0, "T1 `ActionPriority.Player` 的字面量 = 0");
        Check((int)ActionPriority.Instant, 1, "T1 `ActionPriority.Instant` 的字面量 = 1（= 原版默认值）");
        Check((int)ActionPriority.Interrupt, 2, "T1 `ActionPriority.Interrupt` 的字面量 = 2");
        Check((int)ActionPriority.DelayedInstant, 3, "T1 `ActionPriority.DelayedInstant` 的字面量 = 3");
    }

    // ==================================================================
    //  T2 · 道序 = 字面量 [2,1,3,0]
    // ==================================================================
    /// <summary>
    /// 四条道**各放一条**（入队顺序故意按道号 0→1→2→3），出队必须是
    /// **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**。
    /// 🧨 若实现「按枚举数值从小到大排」，出队序会变成 0,1,2,3 ⇒ 本条必红
    ///    （`[2,1,3,0]` 与 `[0,1,2,3]` 只有首尾两个数巧合相同，中间两位直接不同）。
    /// </summary>
    static void W61T2LaneOrder()
    {
        var q = new ActionQueue();
        CheckTrue(q.Enqueue(new QueuedAction { Label = "p" }, ActionPriority.Player), "T2 夹具：Player 道入队成功");
        CheckTrue(q.Enqueue(new QueuedAction { Label = "i" }, ActionPriority.Instant), "T2 夹具：Instant 道入队成功");
        CheckTrue(q.Enqueue(new QueuedAction { Label = "x" }, ActionPriority.Interrupt), "T2 夹具：Interrupt 道入队成功");
        CheckTrue(q.Enqueue(new QueuedAction { Label = "d" }, ActionPriority.DelayedInstant),
                  "T2 夹具：DelayedInstant 道入队成功");
        Check(q.Count, 4, "T2 夹具：四条道各一条 ⇒ Count = 4");

        var got = new List<string>();
        for (int k = 0; k < 4; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T2 第 {k + 1} 次 `TryDequeue` 应成功（道序断言的前置）");
            if (ok) got.Add(((int)lane).ToString());
        }
        Check(string.Join(",", got.ToArray()), "2,1,3,0",
              "T2 道序 = 字面量 [2,1,3,0]（Interrupt→Instant→DelayedInstant→Player）");
        Check(q.Count, 0, "T2 取完四条 ⇒ Count = 0");
    }

    // ==================================================================
    //  T3 · 同道 FIFO ＋ 追加入**队尾**
    // ==================================================================
    /// <summary>
    /// Player 道依次放 A、B、C（中间往别的道插一条，验「插队不影响本道」），
    /// 取出来必须还是 **A → B → C**（原版 `List.Add` 入队尾 + `get_Item(0)`/`RemoveAt(0)` 出队首）。
    /// 🧨 若实现是 LIFO（`RemoveAt(Count-1)`）、或往队首插（`Insert(0, …)`）⇒ 取到 C,B,A ⇒ 必红。
    /// 🧪 夹具自检：三条各自的 `Seq` 必须是**递增**的（1,2,4 —— 中间那条 Instant 拿了 3），
    ///    否则「队尾追加」这半句没被测到（例如实现把 Seq 也重排了）。
    /// </summary>
    static void W61T3SameLaneFifo()
    {
        var q = new ActionQueue();
        var a = new QueuedAction { Label = "A" };
        var b = new QueuedAction { Label = "B" };
        var x = new QueuedAction { Label = "X" };
        var c = new QueuedAction { Label = "C" };
        q.Enqueue(a, ActionPriority.Player);
        q.Enqueue(b, ActionPriority.Player);
        q.Enqueue(x, ActionPriority.Instant);    // 插到**另一条道**，不该打乱 Player 道的相对次序
        q.Enqueue(c, ActionPriority.Player);

        Check(q.CountOf(ActionPriority.Player), 3, "T3 夹具：Player 道里 3 条（A/B/C）");
        Check(q.CountOf(ActionPriority.Instant), 1, "T3 夹具：Instant 道里 1 条（X）");

        // 先取走 Instant 的 X（道序在前），再取 Player 道的三条
        ActionPriority lane0;
        QueuedAction first;
        CheckTrue(q.TryDequeue(out lane0, out first), "T3 第一次出队成功");
        Check((int)lane0, 1, "T3 第一条来自 Instant(1) 道（X）");
        Check(first != null ? first.Label : "(null)", "X", "T3 第一条是 X");

        var got = new List<string>();
        for (int k = 0; k < 3; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T3 Player 道第 {k + 1} 次出队应成功");
            if (ok) got.Add(act.Label);
        }
        Check(string.Join(",", got.ToArray()), "A,B,C", "T3 同一条道 = FIFO（A→B→C），⛔ 不是 LIFO");

        // 🧪 队尾追加：Seq 单调递增（A=1 B=2 X=3 C=4）
        Check(a.Seq, 1, "🧪 T3 A 的入队序号 = 1");
        Check(b.Seq, 2, "🧪 T3 B 的入队序号 = 2");
        Check(x.Seq, 3, "🧪 T3 X 的入队序号 = 3（另一条道也参与全局递增）");
        Check(c.Seq, 4, "🧪 T3 C 的入队序号 = 4 ⇒ C 是**队尾**追加（⛔ 不是插队首）");
    }

    // ==================================================================
    //  T8 · Interrupt 跳队（跨道优先，不需要抢占）
    // ==================================================================
    /// <summary>
    /// 先入 Instant 的 A、再入 Interrupt 的 B ⇒ 出队必须 **B → A**。
    /// 🧨 若实现「按入队顺序出队」（单队列）⇒ 取到 A,B ⇒ 必红。
    /// ⚠️ 这里只测「跨道优先」；「**不抢占**正在结算的那一条」是 `Resolving` 旗标的语义
    ///    （T4，等泵点接线后再测 —— 本批没有泵点，测不了）。
    /// </summary>
    static void W61T8InterruptJumpsQueue()
    {
        var q = new ActionQueue();
        q.Enqueue(new QueuedAction { Label = "A" }, ActionPriority.Instant);
        q.Enqueue(new QueuedAction { Label = "B" }, ActionPriority.Interrupt);

        var got = new List<string>();
        for (int k = 0; k < 2; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T8 第 {k + 1} 次出队成功");
            if (ok) got.Add(act.Label);
        }
        Check(string.Join(",", got.ToArray()), "B,A",
              "T8 Interrupt 道**跳队**：后入队的 B(Interrupt) 先出，A(Instant) 后出");
    }

    // ==================================================================
    //  T9 · DelayedInstant 最后（连 Player 都排在它前面）
    // ==================================================================
    /// <summary>
    /// 入队顺序**故意反着来**（3 → 0 → 1 → 2，即 DelayedInstant, Player, Instant, Interrupt），
    /// 出队必须仍是 **Interrupt → Instant → DelayedInstant → Player**（= `2,1,3,0`）。
    /// 🧨 若实现按**入队顺序**、或按**枚举数值**（0,1,2,3）排 ⇒ 出队序都不是 `2,1,3,0` ⇒ 必红。
    /// 🔴 这一条是本批唯一能证明「**DelayedInstant 比 Player 还晚**」的断言（T2 的入队顺序里
    ///    DelayedInstant 与 Player 的相对位置没被单独拉开）。
    /// </summary>
    static void W61T9DelayedInstantLast()
    {
        var q = new ActionQueue();
        q.Enqueue(new QueuedAction { Label = "d" }, ActionPriority.DelayedInstant);   // 先入、最后出
        q.Enqueue(new QueuedAction { Label = "p" }, ActionPriority.Player);
        q.Enqueue(new QueuedAction { Label = "i" }, ActionPriority.Instant);
        q.Enqueue(new QueuedAction { Label = "x" }, ActionPriority.Interrupt);

        var got = new List<string>();
        for (int k = 0; k < 4; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T9 第 {k + 1} 次出队成功");
            if (ok) got.Add(act.Label);
        }
        Check(string.Join(",", got.ToArray()), "x,i,d,p",
              "T9 DelayedInstant 最后（出队 = Interrupt→Instant→DelayedInstant→Player，入队顺序故意反）");
    }

    // ==================================================================
    //  T13 · 非法 priority ⇒ LogError ＋ 丢弃（⛔ 不 clamp、不默认）
    // ==================================================================
    /// <summary>
    /// `Enqueue(action, (ActionPriority)5)` 与 `(ActionPriority)(-1)` 都必须：
    /// **返回 false** ＋ **不入队** ＋ `Dropped` 计数 +1。
    /// 🧨 若实现「clamp 到最近的道」或「默认 Instant」⇒ `Count` 会变成 1/2 ⇒ 必红
    ///    （只断「不抛异常」是自证 —— 那样 clamp/默认 也照样绿）。
    /// ⚠️ 本用例会**故意打两条 `Debug.LogError`**（这就是「出声」本身）——
    ///    批处理日志里看到这两条**不是**失败，它们由下面三条断言兜着。
    /// </summary>
    static void W61T13InvalidPriority()
    {
        var q = new ActionQueue();

        bool ok5 = q.Enqueue(new QueuedAction { Label = "bad5" }, (ActionPriority)5);
        Check(ok5, false, "T13 非法 priority=5 ⇒ Enqueue 必须返回 false");
        Check(q.Count, 0, "T13 非法 priority=5 的动作**不入队**（Count=0 —— 若 clamp/默认，这里 ≥1 ⇒ 红）");
        Check(q.Dropped, 1, "T13 非法 priority=5 ⇒ 丢弃计数 = 1（出声口在 `Enqueue` 的 LogError）");

        bool okNeg = q.Enqueue(new QueuedAction { Label = "badNeg" }, (ActionPriority)(-1));
        Check(okNeg, false, "T13 非法 priority=-1 ⇒ Enqueue 必须返回 false");
        Check(q.Count, 0, "T13 两条非法动作都不入队 ⇒ Count 仍为 0");
        Check(q.Dropped, 2, "T13 丢弃计数累计 = 2");

        // 🧪 夹具对照：合法的 0 必须能进（否则「全部丢弃」也能让上面几条绿 ⇒ 假绿）
        bool ok0 = q.Enqueue(new QueuedAction { Label = "ok0" }, ActionPriority.Player);
        CheckTrue(ok0, "🧪 T13 对照：合法 priority=0 必须入队成功（防「Enqueue 恒 false」的假绿）");
        Check(q.Count, 1, "🧪 T13 对照：合法那条真的进了队列（Count=1）");
    }

    // ==================================================================
    //  T14 · 数据级：`cards_engine.json` v13 的 `triggerPriority` → `CardDef`
    // ==================================================================
    /// <summary>
    /// **这一条测的是「接线那一跳」** —— `CardDatabase.Parse` 把 v13 的 `triggerPriority` 列
    /// 逐条传给 `CardDef` 的构造参数（落进 `CardDef._triggerPriority` / `TriggerPriority()`），
    /// ⛔ 不是测 `TriggerPriority()` 自己的「缺键 ⇒ 1」那半句。
    ///
    /// 🔴 **期望值全部写死**，来源 = `cards_engine.json` v13 现读（2026-10-21）：
    ///   非默认的**只有 4 张** —— `ASH53` spiritstone 3 · `SAU13`/`SAU70` artifice 3 · `UM37` codex 3。
    ///
    /// 🧨 什么会让它红：
    ///   · `CardDatabase` **不传**这一列（= 本批之前的状态）⇒ 四张卡的 3 全变 1 ⇒ 前四条红；
    ///   · 生成器把 `GOF97` 那条**空 logic 的 `priority=0`** 落进来 ⇒ 全池扫描多一条 ⇒ 红；
    ///   · 生成器 / 卡表把某张卡的**默认值 1 也写进列**、或给别的卡落了非默认值 ⇒ 全池扫描的集合变大 ⇒ 红。
    ///   · `CardDef.TriggerPriority` 的「缺键 ⇒ 1」被改成返回 0 / 抛异常 ⇒ `SAU1` 那两条红。
    ///
    /// ⚠️ 全池扫描用的关键词表 = `CardDef.RoutableTriggers`（25 词）—— 生成器收的 21 个键
    ///   **逐个都在里面**（`gen_cards_engine.py` 的 `ABILITY_TRIGGER_KEYWORD` 收词时就核过这一条）
    ///   ⇒ 不会漏扫；多扫到的几个词（`rally`/`backlash`/…）在数据里没有对应列，返回默认 1，无副作用。
    /// </summary>
    static void W61T14DataTriggerPriority()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool.Count >= 1000, $"T14 夹具：卡表加载 {pool.Count} 张（应 ≥ 1000）");

        var sau13 = CardDatabase.FindById(pool, "SAU13");
        var sau70 = CardDatabase.FindById(pool, "SAU70");
        var um37 = CardDatabase.FindById(pool, "UM37");
        var ash53 = CardDatabase.FindById(pool, "ASH53");
        var sau1 = CardDatabase.FindById(pool, "SAU1");
        var gof97 = CardDatabase.FindById(pool, "GOF97");
        CheckTrue(sau13 != null && sau70 != null && um37 != null && ash53 != null && sau1 != null && gof97 != null,
                  "T14 夹具：六张样本卡都在池里（SAU13 / SAU70 / UM37 / ASH53 / SAU1 / GOF97）");
        // 夹具不成立就**不再往下比**（否则空引用会把整条用例抛掉、后面的断言一条都记不上）
        if (sau13 == null || sau70 == null || um37 == null || ash53 == null || sau1 == null || gof97 == null)
            return;

        // ---- ① 四张真卡：非默认 3（DelayedInstant）----
        Check(sau13.TriggerPriority("artifice"), 3, "T14 `SAU13`（Canoptek Spyder）`artifice` = 3（DelayedInstant · 原版真值）");
        Check(sau70.TriggerPriority("artifice"), 3, "T14 `SAU70`（Damaged Hexmark）`artifice` = 3（DelayedInstant · 原版真值）");
        Check(um37.TriggerPriority("codex"), 3, "T14 `UM37`（Death from Above）`codex` = 3（DelayedInstant · 原版真值）");
        Check(ash53.TriggerPriority("spiritstone"), 3, "T14 `ASH53`（Reclaim the Stars）`spiritstone` = 3（DelayedInstant · 原版真值）");

        // ---- ② 普通卡：默认 1 ----
        // `SAU1` 的 `keywords` 里**就写着** `Artifice`（卡面 `Artifice: Deploy a Canoptek Scarab`），
        //   但它的原版 priority = 1 ⇒ 列里没有它 ⇒ 读出来必须是 1。
        //   🧨 若接线把「有 Artifice 关键词」当成「有 priority」、或 `Parse` 给全池都塞了 1（改为落默认值），
        //      这条与下面的全池扫描会一起露。
        Check(sau1.TriggerPriority("artifice"), 1, "T14 `SAU1`（普通卡 · `keywords` 有 `Artifice`）`artifice` = 1（默认 Instant）");
        // ---- ③ 没登记的关键词 ⇒ 默认 1（⛔ 不是 0）----
        Check(sau1.TriggerPriority("no_such_trigger_keyword"), 1, "T14 未登记的关键词 ⇒ 默认 1（⛔ 不是 0）");

        // ---- ④ GOF97 那条数据陷阱：空 logic 的 priority=0 必须**不落** ----
        //   真值 `GOF97 Gargantuan Squiggoth`：第 0 条 `trigger=0 / priority=0 / logic=0`（空 logic）、
        //   第 1 条 `trigger=0 / priority=1 / logic=2`、第 2 条 `trigger=300 Mob / priority=1`。
        //   生成器「只认有 logic 的那条」⇒ 本卡**一条非默认都不该有**（尤其不能凭空出现 = 0 / = 2）。
        //   🧨 生成器改回「第一条为准」那版 ⇒ 若 trigger=0 哪天被映射成关键词，这里会先红；
        //      今天 trigger=0 没有关键词 ⇒ 本条的**主要兜底**是下面那条全池扫描（GOF97 不在清单里）。
        Check(gof97.TriggerPriority("mob"), 1, "T14 `GOF97`（空 logic 那条 priority=0 必须不落）`mob` = 1（默认 Instant）");

        // ---- ⑤ 全池扫描：非默认项必须**恰好**是上面那 4 条 ----
        //   这一条是**结构性**的：它同时覆盖「漏接线」（0 条）、「多接线 / 生成器回归」（>4 条），
        //   而且期望清单里 **没有 GOF97** ⇒ 那条陷阱的两种可能错法都会被它抓住。
        var got = new List<string>();
        foreach (var c in pool)
        {
            if (c == null) continue;
            foreach (string kw in CardDef.RoutableTriggers)
            {
                int pr = c.TriggerPriority(kw);
                if (pr != CardDef.DefaultTriggerPriority) got.Add(c.Id + "/" + kw + "=" + pr);
            }
        }
        got.Sort(System.StringComparer.Ordinal);
        Check(got.Count, 4, "★ T14 全池非默认 priority 的**条目数** = 4（2026-10-21 现读；漏接线 ⇒ 0、多落一条 ⇒ 5+）");
        Check(string.Join(",", got.ToArray()), "ASH53/spiritstone=3,SAU13/artifice=3,SAU70/artifice=3,UM37/codex=3",
              "★ T14 全池非默认 priority 的**逐条清单**（`id/关键词=值`，按 Ordinal 排序）");
    }
}
