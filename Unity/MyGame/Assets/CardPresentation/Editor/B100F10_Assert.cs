// B100F10_Assert.cs — 第一百会话 · 执行代理 `B100_F10`
//
// ============================ 它盯的是什么 ============================
//  报告 `资料/完全复刻/报告/B100_X3.md` #17（P0）：
//    `ReplayRecord.vars`（`Battle/ReplayStore.cs` 的 `ReplayVars`）原来靠 **`vars == null`** 判「这份录像
//    **没有记**规则参数」。🔴 **那条分支恒不成立** —— `JsonUtility.FromJson` 会把**缺键的可序列化类字段
//    ＊物化成一份全 0 对象＊**（本项目已知坑，记忆 `jsonutility-null-subtree`）。实测老档 `{"version":4}` 读出来
//    `vars != null` ⇒ 生产 `BattleDriver.ReplayVarsForReplay` 会拿 `deckSize=0` / `startingMana=0` 去 `ToVars()`，
//    老录像放出来就是**全 0 参数开一局**。
//
//  本件落地 = 照同文件 `overtimeTurnSet` 的**同族哨兵**写法加 `ReplayVars.varsRecorded`：
//    · `Capture` 置 `true`（= 记过）；老档 JSON 没有这个键 ⇒ 读到 `false`（= 没记）；
//    · **唯一读法** = `ReplayVars.IsRecorded(v)`（`v != null && v.varsRecorded`）；
//    · 生产消费点 = `BattleDriver.ReplayVarsForReplay`：`!IsRecorded(...)` ⇒ `Debug.LogWarning` **出声** + 退回默认。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `资料/完全复刻/报告/B100_X3.md` #17 / #18（#18 = 同一个类的字段数分母）
//   · 同族先例：`ReplayVars.overtimeTurnSet`（`int?` 编码成「值 + 有没有值」两个字段）
//   · 哨兵取 `false` 的纪律：`ReplayRecord.tutorialStage` / `botSeatPlus2` 的 doc（`0`/`false` **既是 CLR 默认值、
//     也是初始化器值** ⇒ `JsonUtility` 跑不跑无参构造都给同一个值）
//   · 本件报告 → `资料/完全复刻/报告/B100_F10.md`
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()`（现读 `:1294` 那条超长行，**行内追加** ⇒ 零行数变化）：
//      B100F10_Assert.Run(); _pass += B100F10_Assert.Pass; _fail += B100F10_Assert.Fail;
//  （本件只碰纯 C# / `JsonUtility`，**不建 GameObject、不要 driver** ⇒ 放哪个宿主都行，推荐 `RuleEngineTest.Run`。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **C1**：把 `IsRecorded` 退回成 `v != null`（= 回到 bug 前那条恒真判据）⇒ 一份没置位的 `ReplayVars` 会被
//     当成「记过」⇒ C1 立刻红。这正是「哨兵被删掉」的唯一结构性防线。
//   · **C2**：把 `Capture` 里那句 `varsRecorded = true` 删掉 ⇒ 新档也被当成「没记」⇒ C2 红。
//   · **C3**：把 `varsRecorded` 改成属性 / `private` / `[NonSerialized]` ⇒ `JsonUtility` 读不出哨兵 ⇒ B2 / C3 红。
//   · **C4**：加字段忘了同步 `TotalFieldCount` ⇒ 本条与 `B97W18` B6a 一起红（同一个分母）。
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B100_F10`：`ReplayVars` 的「没记」哨兵（老档 ⛔ 不能判 `vars == null`）。</summary>
public static class B100F10_Assert
{
    static readonly CheckSink _sink = new CheckSink("[F10] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B100_F10：`ReplayVars.varsRecorded` 哨兵 —— 老档「没记」≠「记了但全 0」");

        SectionA_老档没有这一步();
        SectionB_新档往返();
        SectionC_灭自证_结构与分母();

        Debug.Log(_sink.P + "F10 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A：老档
    static void SectionA_老档没有这一步()
    {
        // 真走 `JsonUtility.FromJson` —— 这就是 `ReplayStore.Load`（`ReplayStore.cs` 的 `FromJson<ReplayRecord>`）那条存档路。
        var oldRec = JsonUtility.FromJson<ReplayRecord>("{\"version\":4}");
        Tr(oldRec != null, "A0（前提）老档 `{\"version\":4}` 读得出来");
        if (oldRec == null) return;

        // 🔴 事实（不是设计）：`JsonUtility` 把缺键的**可序列化类字段物化成了对象**，不是 `null`。
        Tr(oldRec.vars != null,
           "A1（事实）老档读出来 `vars` **不是** `null` —— `JsonUtility` 把它物化成了一份对象（⇒ 用 `== null` 判「没记」恒不成立，这就是 #17 的根因；若哪天 Unity 改成留 `null`，本条会当场红）");
        if (oldRec.vars == null) return;

        C(oldRec.vars.deckSize, 0, "A2 物化出来的那份是**全 0**（`deckSize=0`）—— 拿它开一局 = `deckSize=0` / `startingMana=0`（这正是要挡的东西）");
        Tr(!ReplayVars.IsRecorded(oldRec.vars),
           "A3 ★ 老档 ⇒ `ReplayVars.IsRecorded` = **false**（= **没记**），⛔ 不是「记了、但每格都是 0」—— 这两件事从此可分");

        // A4：`vars` 真的是 `null` 的那条路（`Capture(null)` / 录制端没接上）也要算「没记」。
        Tr(!ReplayVars.IsRecorded(null), "A4 `IsRecorded(null)` = false（`vars` 真是 null 时同样走「没记」那条路）");
    }

    // ============================================================ B：新档往返
    static void SectionB_新档往返()
    {
        var src = GameplayVariables.For(GameMode.Classic).Clone();
        src.deckSize = 27;          // 故意 ≠ 默认（默认见 `GameplayVariables`，⛔ 别在这一行写死它）
        src.startingMana = 2;
        src.startingHand = 5;

        var cap = ReplayVars.Capture(src);
        Tr(ReplayVars.IsRecorded(cap), "B0（前提）`Capture` 抓出来的快照 `IsRecorded` = true（哨兵在 `Capture` 里置真）");
        if (cap == null) return;

        // 真过一遍 JSON —— 写 → 读。
        var rec = new ReplayRecord { vars = cap };
        var back = JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(rec));
        Tr(back != null && back.vars != null, "B1（前提）新档过一遍 `JsonUtility` 读得回来");
        if (back == null || back.vars == null) return;

        Tr(ReplayVars.IsRecorded(back.vars),
           "B2 ★ 哨兵 `varsRecorded` **过一遍 JSON 还在**（= true）—— 证明它是 public 实例字段、没被 `[NonSerialized]` / 属性吃掉");
        var v = back.vars.ToVars();
        C(v.deckSize, 27, "B3 `deckSize` 往返（这一格在 `RuleCore.NewBattle` 那一趟就被读掉 ⇒ 装不回 = 另一局）");
        C(v.startingMana, 2, "B4 `startingMana` 往返");
        C(v.startingHand, 5, "B5 `startingHand` 往返（**我们独有**那 4 格之一）");
    }

    // ============================================================ C：灭自证 + 分母
    static void SectionC_灭自证_结构与分母()
    {
        // ①「没走过 `Capture` 的 `ReplayVars`」= 老档物化出来的那种 ⇒ 必须判成「没记」。
        //   把 `IsRecorded` 写成 `v != null`（= bug 前那条判据）⇒ 本条立刻红。
        var blank = new ReplayVars();
        Tr(!ReplayVars.IsRecorded(blank),
           "C1 ★ 一份**没走过 `Capture`** 的 `ReplayVars`（= 老档物化出来的那种，`vars != null`）⇒ `IsRecorded` = **false**。"
         + "把 `IsRecorded` 退回 `v != null` ⇒ 本条立刻红（这就是「哨兵被删掉」的结构性防线）");

        // ② 新档必须置真 —— 删掉 `Capture` 里那句 `varsRecorded = true` ⇒ 本条红。
        Tr(ReplayVars.IsRecorded(ReplayVars.Capture(GameplayVariables.For(GameMode.Classic))),
           "C2 ★ `Capture` 必须置哨兵 —— 删掉 `varsRecorded = true` ⇒ 新档也被当成「没记」⇒ 本条红");

        // ③ 结构：`JsonUtility` 只序列化**字段** ⇒ 哨兵必须还是 public 实例 `bool` 字段。
        var f = typeof(ReplayVars).GetField("varsRecorded", BindingFlags.Public | BindingFlags.Instance);
        Tr(f != null && f.FieldType == typeof(bool),
           "C3 ★ `varsRecorded` 是 **public 实例 `bool` 字段**（改成属性 / `private` / `[NonSerialized]` ⇒ 老档新档都读不出哨兵 ⇒ 本条红）");

        // ④ 分母：加字段必须同步 `TotalFieldCount`（同一个常量也被 `B97W18` B6a 用）。
        int total = typeof(ReplayVars).GetFields(BindingFlags.Public | BindingFlags.Instance).Length;
        C(total, ReplayVars.TotalFieldCount,
          "C4 `ReplayVars` 公有实例字段数 = " + ReplayVars.TotalFieldCount
        + "（= `B97W18` B6a 那条的分母；加哨兵忘了同步常量 ⇒ 两条一起红）");
    }
}
