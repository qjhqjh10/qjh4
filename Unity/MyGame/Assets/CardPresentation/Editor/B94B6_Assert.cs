// B94B6_Assert.cs — 执行代理 B6（第九十四会话 · 波 2 · 未分类域）的断言
//
// ============================ 它盯的是什么 ============================
//   M 账 `未分类/其他:204` 原版 `Countdown` 的**乘倍速公式**（本轮由 VA 反汇编结案：**先截断 speedFactor、再乘 ticks**）
//   N 账 `未分类/其他:312` 注册表 `MenuUniqueIds` 的**消费端**（= 原版 `TutorialPointerData.Target`，全二进制唯一消费者）
//   P 账 `未分类/其他:99/348` 深拷家族的**不可变审计**执行版（卡定义跨 `Load()` 共享同一实例、且没人写）
//
// 逐条判据、反汇编原文、宿主/实例普查 → `资料/完全复刻/判据/B94_B6_判据补全.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B94B6Assert.Run();`
//   + 宿主里两行（照 `B93S3Assert` 的先例）：
//     `_sink.Pass += B94B6Assert.Pass; _sink.Fail += B94B6Assert.Fail;`
//   · 推荐宿主 = **`Editor/ShellScene.cs`**（`Shell/TimerDisplay.cs` 与 `Shell/TutorialPointerUi.cs` 都在壳上）。
//   · 本文件**不建持久资产**、不跑 Unity 批处理；建的 `GameObject` 在 `finally` 里 `DestroyImmediate`。
//   · 静态表（`MenuUniqueIds`）每条都在 `finally` 里 `ClearForTest()` —— 否则会污染别的宿主。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · M1/M3：把 `Scale` 改回「`FromTicks((long)(ticks * (double)factor))`」⇒ M1 变 `1:26`、M3 变 `1:36` ⇒ 红。
//   · M5：把 `+ RoundUp` 删掉 ⇒ 全组少 1 秒 ⇒ 红。
//   · N1：把 `Target` 改成「未命中抛异常」⇒ N2 抛出去 ⇒ 红；把 `Find` 改成 `FindComponent` ⇒ N1 红（返回的是组件不是 GO）。
//   · P2：让 `CardDatabase.Load()` 每次**深拷** CardDef（照原版 `DeepCopy` 那种做法）⇒ P2 红。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B6：3 条（Countdown 乘式 / 唯一 id 的消费端 / 卡定义不可变审计）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量**（`"1:31"` / `90.0` / `true`），不从被测实现里读回来。</para></summary>
public static class B94B6Assert
{
    static readonly CheckSink _sink = new CheckSink("[B6] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B6 未分类域：Countdown 乘式（先截断再乘）/ 唯一 id 的消费端 / 卡定义不可变审计");

        SectionM_Countdown乘式();
        SectionN_TutorialPointerData();
        SectionP_卡定义不可变();

        Debug.Log(_sink.P + "B6 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ M `Countdown` 的那一下乘倍速
    static void SectionM_Countdown乘式()
    {
        MenuCheck.Section(_sink, "M 原版 `Countdown`：`FromTicks((long)speedFactor * ticks)`（**先把 speedFactor 截断**）");

        var t0 = new System.DateTime(2026, 10, 29, 12, 0, 0);
        var now = t0;
        string last = null;
        var cd = new Countdown(s => last = s, null, () => now);

        // ---- M1：1.5 倍 ⇒ `(long)1.5 = 1` ⇒ 10 秒只走 10 秒 ⇒ 100 − 10 + 1 = 91 ⇒ `1:31`
        //      🧨 乘完再截断 ⇒ 走 15 秒 ⇒ 86 ⇒ `1:26` ⇒ 红。
        cd.Activate(1.5f, 100);
        C(last, "1:40", "M1a `Activate(1.5, 100)` 首帧 = `timeAvailable` 本身 = `1:40`（不带那 1 秒）");
        now = t0.AddSeconds(10);
        cd.Tick();
        C(last, "1:31", "M1 ★ 1.5 倍速按 **1 倍**走 ⇒ 10 秒后 = 100−10+1 = 91 ⇒ `1:31`（乘完再截断会变 `1:26`）");
        C(cd.Elapsed, System.TimeSpan.FromSeconds(10), "M1b `Elapsed` 正好 10 秒（`(long)1.5 = 1`，不是 15 秒）");
        C(cd.GetSecondsLeft(), 90.0, "M1c `GetSecondsLeft()` = 100−10 = 90.0（**不含**那 1 秒）");

        // ---- M2：0 < 倍速 < 1 ⇒ `(long)0.5 = 0` ⇒ 计时**冻住**（原版就这行为，⛔ 别"修"）
        //      🧨 乘完再截断 ⇒ 走 5 秒 ⇒ 96 ⇒ `1:36` ⇒ 红。
        var cd2 = new Countdown(s => last = s, null, () => now);
        now = t0;
        cd2.Activate(0.5f, 100);
        now = t0.AddSeconds(10);
        cd2.Tick();
        C(last, "1:41", "M2 ★ 0.5 倍速 ⇒ `(long)0.5 = 0` ⇒ 10 秒后**一点没走** ⇒ 100+1 = 101 ⇒ `1:41`（冻住是原版行为）");
        C(cd2.Elapsed, System.TimeSpan.Zero, "M2b `Elapsed` 仍是 `TimeSpan.Zero`（`0 × ticks = 0`）");
        C(cd2.GetSecondsLeft(), 100.0, "M2c `GetSecondsLeft()` 仍是 100.0");

        // ---- M3：整数倍速照旧（2 倍）+ `ChangeSpeed` 把已走的搬进 `timeElapsedBeforeStop`
        var cd3 = new Countdown(s => last = s, null, () => now);
        now = t0;
        cd3.Activate(1f, 100);
        now = t0.AddSeconds(10);
        cd3.Tick();                                   // elapsed = 10s
        cd3.ChangeSpeed(2f);                          // beforeStop = 10s, lastStop = now
        now = t0.AddSeconds(20);
        cd3.Tick();                                   // elapsed = 10 + 20 = 30s
        C(cd3.Elapsed, System.TimeSpan.FromSeconds(30), "M3 ★ `ChangeSpeed(2)` 后 10 秒 ⇒ 累计 10+20 = 30 秒（`timeElapsedBeforeStop` 那一跳）");
        C(last, "1:11", "M3b 100−30+1 = 71 ⇒ `1:11`");

        // ---- M4：`Stop()` 之后不再走（`running` 闸在两处乘式之前）
        cd3.Stop();
        now = t0.AddSeconds(200);
        cd3.Tick();
        C(cd3.Elapsed, System.TimeSpan.FromSeconds(30), "M4 `Stop()` 之后 `Elapsed` 不动");
        C(last, "1:11", "M4b 文本也不动");
    }

    // ============================================================ N 唯一 id 的**消费端**（原版 `TutorialPointerData.Target`）
    static void SectionN_TutorialPointerData()
    {
        MenuCheck.Section(_sink, "N 原版 `TutorialPointerData.Target` = `GameObjectUniqueIdentifier.Find(id)`，未命中 ⇒ LogError + null");

        GameObject goA = null;
        try
        {
            MenuUniqueIds.ClearForTest();
            goA = new GameObject("B6_Target");
            MenuUniqueIds.Register("PlayRankedButton", goA.transform);

            var data = new TutorialPointerData { id = "PlayRankedButton", filterRaycast = true };
            Tr(ReferenceEquals(data.Target, goA), "N1 ★ `Target` 返回**登记节点的 gameObject**（走 `MenuUniqueIds.Find`）");
            C(data.filterRaycast, true, "N1b `filterRaycast` 原样带得走（`+0x20`）");
            Tr(data.Offset == Vector2.zero, "N1c `Offset` 默认 = (0,0) —— 原版那个字段**没有 `[SerializeField]`** ⇒ 不序列化");
            C(data.Offset.x, 0f, "N1d `get_Offset` = `return this.offset` 原样（无加工）");

            var missing = new TutorialPointerData { id = "NoSuchId_B6" };
            Tr(missing.Target == null, "N2 ★ 未命中 ⇒ `Target` = **null**（不是抛；原版那一支打 `[ERROR] Tutorial. Can't find button with ID …`）");
        }
        finally
        {
            MenuUniqueIds.ClearForTest();
            if (goA != null) Object.DestroyImmediate(goA);
        }
    }

    // ============================================================ P 卡定义不可变（深拷家族审计的执行版）
    static void SectionP_卡定义不可变()
    {
        MenuCheck.Section(_sink, "P 深拷家族审计：卡定义及其嵌套判据跨 `Load()` **共享同一实例**且没人写 ⇒ 深/浅拷无差异");

        var a = CardDatabase.Load();
        var b = CardDatabase.Load();
        Tr(a != null && b != null && a.Count > 0, "P0 卡表读得到（读不到 ⇒ 下面几条无意义，先看这一条）");
        if (a == null || b == null || a.Count == 0) return;

        Tr(!ReferenceEquals(a, b), "P1 `Load()` 每次回**一份新的 List**（浅拷贝；原版也允许调用方改这个列表）");

        // 挑一张**带主动能力行**的卡（= 判据树最深的那一块），两份 List 里按 id 配
        CardDef cardA = null;
        for (int i = 0; i < a.Count; i++)
        {
            var d = a[i];
            if (d != null && d.ActiveAbilityLogic != null && d.ActiveAbilityLogic.Count > 0) { cardA = d; break; }
        }
        Tr(cardA != null, "P2a 池里至少有一张带 `ActiveAbilityLogic` 的卡（前提断言；不成立后面全是空转）");
        if (cardA == null) return;
        CardDef cardB = null;
        for (int i = 0; i < b.Count; i++) { if (b[i] != null && b[i].Id == cardA.Id) { cardB = b[i]; break; } }
        Tr(cardB != null, "P2b 第二份里找得到同一 id 的卡（前提断言）");
        if (cardB == null) return;

        Tr(ReferenceEquals(cardA, cardB), "P2 ★ 两张卡是**同一个 `CardDef` 实例**（进程级缓存 ⇒ 改它 = 改全局）");
        Tr(ReferenceEquals(cardA.ActiveAbilityLogic, cardB.ActiveAbilityLogic),
           "P2c ★ 嵌套的判据表也是**同一个实例**（我们从不克隆定义 ⇒ 原版 `DeepCopy` 在本仓没有对应行为）");
        Tr(ReferenceEquals(cardA.ActiveAbilityLogic[0], cardB.ActiveAbilityLogic[0]),
           "P2d ★ 连最里面那一层 spec 都同一实例（深拷 vs 浅拷在这里**不可观测**）");

        // 再解析一遍**不得动到既有实例**（原版 `DeepCopy` 的存在理由 = 允许「克隆后就地改」；我们没有那条路）
        int nBefore = cardA.ActiveAbilityLogic.Count;
        var e0Before = cardA.ActiveAbilityLogic[0];
        var c = CardDatabase.Load();
        Tr(c != null && c.Count > 0, "P3a 第三次 `Load()` 仍读得到");
        C(cardA.ActiveAbilityLogic.Count, nBefore,
          "P3 ★ 重复 `Load()` 没改既有卡实例的行数（判据侧唯一的写点在 `CardDatabase.Parse`，且只写新对象）");
        Tr(ReferenceEquals(cardA.ActiveAbilityLogic[0], e0Before),
           "P3b …最里面那层 spec 还是**原来那个对象**（没被重新解析出来的一批顶掉）");
    }
}
