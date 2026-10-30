// B103W6_Assert.cs — 执行代理 `B103_W6`（2026-10-30）：`Shell/TabButtons.cs` 死 handler 谓词的**立证**
//
// ============================ 它盯的是什么 ============================
//   `B102AUD03R` 的四条红（`B103_D1` §二）是**同一个根因**：`TabButtons` 原先两处都写
//       `target != null && target == null`          // target 的静态类型是 UnityEngine.Object
//   ⇒ `!=` 与 `==` **两边都绑到 Unity 的重载**（`op_Inequality` = `!(op_Equality)`，而对**已销毁**对象
//   `op_Equality(x, null) == true`）⇒ 整个式子 = `!X && X` = **恒 false**
//   ⇒ 「摘死 handler」与「数死 handler」两条路**从落地起就一次都没生效过**（生产侧死 handler 静默累积）。
//   修法（已落地）= 抽一个唯一谓词 `TabButtons.IsDeadHandler`：
//       `!ReferenceEquals(uo, null) && uo == null`   // 先用 C# 引用判「非字面 null」，再用 Unity 重载判「已销毁」
//   **逻辑等价于原意图**（注释 / 原版语义都是「只摘已经销毁的那些」），⛔ 不是新设计。
//
// ============================ 期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）============================
//   · ①②③④ 的期望值不是我们定的常量，是 **`UnityEngine.Object` 重载 `==` 的语义**：
//     `Destroy` 之后对象引用仍在、`== null` 为真（Unity 假 null）；`ReferenceEquals(x, null)` 仍为假。
//     ⇒ 「已销毁 ⇒ 死」与「存活 / 纯托管 ⇒ 活」这两格由 Unity 自己的行为定义，实现只能对上或对不上。
//   · B 组的期望值 = **`B102AUD03R_Assert` 的 A0-前提⑥ / A1 / A2 那三条**（诊断正本 `报告/B103_D1.md` §2.1 已判
//     「断言侧期望值是对的一个字都不用改」）。本文件只是把「谓词真的被两个调用点用上了」也一并钉住 ——
//     ⛔ 那三条红的断言本体没动、也不该动。
//
// ============================ 灭自证（哪一处改动会让哪一条红）============================
//   A5 / A5b  🧨 把 `IsDeadHandler` 换回 `target != null && target == null` ⇒ **A2 必红**（返回 false），
//             且 A5b「新旧在已销毁这一格上结论必须相反」直接不成立 ⇒ 红。
//   A2        🧨 把谓词写成 `if (d == null) return true;`（把 null 也当死的）⇒ **A4 红**。
//   A3/A3b    🧨 写成 `return uo == null;`（省掉 `ReferenceEquals` 分档）⇒ 纯托管 Target 的 `as` 结果是
//             **字面 null** ⇒ `uo == null` 为真 ⇒ 返回 true ⇒ **A3b 红**（那会静默摘掉闭包 / 静态方法）。
//   B1/B2     🧨 谓词只建不调（调用点仍写旧式）⇒ **B1 实得 0 / B2 实得 2** ⇒ 红（这是「助手建了没人用」那类）。
//   B3        🧨 把 `DropDeadHandlers` 改成「全清再挂自己」⇒ 实得 1 ⇒ 红。
//   C2/C2b    🧨 把 `SceneInitializer.IsSettled` 换回 `obj != null && obj == null` ⇒ 对已销毁件返回 false ⇒ 红
//             （那一支的语义 = `:310` 注释「被销毁的件也算 settled —— 否则 `:280` 那条等待永远等」）。
//   C3/C3b    🧨 同上：新旧谓词在「已销毁」格上必须结论相反，任一边改成另一边 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   接在 `CardPresentation/Editor/ShellScene.cs` 的**方法尾聚合行**之前，**同一物理行追加**（⇒ 行数零变化）：
//     `B103W6_Assert.Run(); _sink.Pass += B103W6_Assert.Pass; _sink.Fail += B103W6_Assert.Fail;`
//   （⚠️ 追加前先看该行第一个 `//`，文本必须落在它**之前**。）
//   也可单独跑：`-executeMethod B103W6_Assert.Run`。
//   ⚠️ 本件**不需要宿主已开着任何窗**：自建的探针对象用 `DestroyImmediate` 收干净（编辑模式没有帧循环，
//      ⛔ 不用 `Destroy`）。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B103_W6`：`TabButtons` 死 handler 谓词（Unity 假 null 必须先用 `ReferenceEquals` 分档）。</summary>
public static class B103W6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103W6] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B103W6：`TabButtons.IsDeadHandler`（死 handler 谓词）—— 活 / 已销毁 / 纯托管 / null / 灭自证 + 两个调用点");
        try { CaseAPredicate(); }
        catch (Exception e) { Tr(false, "A「谓词本身」这一组抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        try { CaseBCallSites(); }
        catch (Exception e) { Tr(false, "B「两个调用点」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { CaseCSettledBranch(); }
        catch (Exception e) { Tr(false, "C「`SceneInitializer.IsSettled` 的已销毁分支」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B103W6 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>⛔ **被修掉的那个写法**（逐字照 `B103_D1` §2.2 抄）。只在「灭自证」那条里用，**产品代码里不许再出现**。</summary>
    static bool OldPredicate(System.Delegate d)
    {
        var target = d != null ? d.Target as UnityEngine.Object : null;
        return target != null && target == null;   // ← 两边都绑 Unity 重载 ⇒ 恒 false
    }

    // ============================================================ A · 谓词本身

    static void CaseAPredicate()
    {
        Debug.Log(_sink.P + "--- A：谓词本身（① 活 ② 已销毁 ③ 纯托管 ④ null ⑤ 灭自证）---");

        var go = new GameObject("B103W6 live", typeof(RectTransform));
        var tb = go.AddComponent<TabButtons>();
        Tr(tb != null, "A0-前提①：`TabButtons` 组件建出来了（编辑器模式下 `AddComponent` 成功 —— 它有同名脚本资产）");
        if (tb == null) { UnityEngine.Object.DestroyImmediate(go); return; }

        System.Action<int> live = tb.Click;            // 实例方法 ⇒ Target 是**活的** UnityEngine.Object
        var liveTarget = live != null ? live.Target as UnityEngine.Object : null;
        Tr(!ReferenceEquals(liveTarget, null),
           "A0-前提③：这条 delegate 的 `Target` 真是一个 `UnityEngine.Object`（⇒ 下面 ①/② 分得开，不是两条都断在空上）");

        Tr(!TabButtons.IsDeadHandlerForTest(live),
           "A1 ① **活对象** ⇒ `IsDeadHandler == false`（活的容器绝不能被当死 handler 摘掉）");

        var dead = live;                                // ⚠️ 必须在销毁**之前**抓（销毁之后 `tb.Click` 取不到 Method）
        UnityEngine.Object.DestroyImmediate(go);
        Tr(!ReferenceEquals(tb, null) && tb == null,
           "A0-前提④：`DestroyImmediate` 之后 `tb` 是 **Unity 假 null**（引用非 null、`== null` 为真）—— 这是 ② 的前提");

        Tr(TabButtons.IsDeadHandlerForTest(dead),
           "A2 ★ ② **已销毁对象** ⇒ `IsDeadHandler == true`"
         + "｜🧨 换回 `target != null && target == null` ⇒ 本条红（恒假）—— 这正是 `B102AUD03R` 场景里那四条红");

        System.Action<int> staticD = StaticNoop;       // 静态方法 ⇒ Target 为 null
        Tr(ReferenceEquals(staticD.Target, null), "A0-前提⑤：静态方法 delegate 的 `Target` 是**字面 null**");
        Tr(!TabButtons.IsDeadHandlerForTest(staticD),
           "A3 ③ **静态方法 delegate**（Target == null）⇒ false（⛔ 它不是「死的」）");

        int counter = 0;
        System.Action<int> closureD = x => counter += x;   // 闭包 ⇒ Target 是编译器生成的显示类（非 UnityEngine.Object）
        Tr(!ReferenceEquals(closureD.Target, null) && !(closureD.Target is UnityEngine.Object),
           "A0-前提⑥：闭包 delegate 的 `Target` **非 null** 且**不是** `UnityEngine.Object`");
        Tr(!TabButtons.IsDeadHandlerForTest(closureD),
           "A3b ★ ③ **纯托管 Target（闭包）** ⇒ false"
         + "｜🧨 写成 `return uo == null;`（省掉 `ReferenceEquals` 分档）⇒ `as` 出来的字面 null 让本条变 true ⇒ 红"
         + "（那会静默摘掉闭包 / 静态方法的 handler）");

        Tr(!TabButtons.IsDeadHandlerForTest(null),
           "A4 ④ **`null` delegate** ⇒ false"
         + "｜🧨 写成 `if (d == null) return true;` ⇒ 本条红（null 不是「死了」）");

        Tr(!OldPredicate(dead),
           "A5 ★ 灭自证：把谓词换回 `target != null && target == null`，它对**同一条**已销毁 handler 返回 false"
         + " ⇒ 那时候 ②（A2）必红 —— 这说明 A2 确实在测「假 null」，不是同义反复");
        Tr(TabButtons.IsDeadHandlerForTest(dead) != OldPredicate(dead),
           "A5b ★ 灭自证（结构锁）：新旧两条实现在「已销毁」这一格上**结论相反** —— 把任何一边改成另一边，本条必红");
    }

    static void StaticNoop(int ignored) { }

    // ============================================================ B · 两个调用点真的都走这条谓词

    static void CaseBCallSites()
    {
        Debug.Log(_sink.P + "--- B：`DropDeadHandlers` 与 `DeadHandlerCountForTest` 真的都走这条谓词 ---");

        var wgo = new GameObject("B103W6 TabWin", typeof(RectTransform));
        var win = wgo.AddComponent<GameWindowWithTabs>();
        Tr(win != null, "B0-前提①：探针 `GameWindowWithTabs` 建出来了");
        if (win == null) { UnityEngine.Object.DestroyImmediate(wgo); return; }
        C(TabButtons.HandlerCountForTest(win), 0, "B0-前提②：新窗的 `OnTabChanged` 一开始 0 条");

        var tb1 = NewTabButtons(win);
        C(TabButtons.HandlerCountForTest(win), 1, "B0-前提③：第一次 `Initialize` 之后正好 1 条");
        C(TabButtons.DeadHandlerCountForTest(win), 0, "B0-前提④：而且没有死的");

        UnityEngine.Object.DestroyImmediate(tb1.gameObject);   // = 整窗重建时旧容器被销毁（死 handler 现场）
        C(TabButtons.DeadHandlerCountForTest(win), 1,
           "B1 ★ `DeadHandlerCountForTest` **数得出**那一条死的"
         + "｜🧨 旧写法恒假 ⇒ 实得 0 ⇒ 红（= `B102AUD03R` A0-前提⑥ 那条）");

        var tb2 = NewTabButtons(win);
        C(TabButtons.HandlerCountForTest(win), 1,
           "B2 ★ `DropDeadHandlers` **真的摘掉了**那条死的"
         + "｜🧨 旧写法一条都摘不掉 ⇒ 实得 2 ⇒ 红（= `B102AUD03R` A1 那条）");
        C(TabButtons.DeadHandlerCountForTest(win), 0, "B2b 摘完之后一条死的都没有了");

        var tb3 = NewTabButtons(win);
        C(TabButtons.HandlerCountForTest(win), 2,
           "B3 ★ 灭自证：**两个活容器都在** —— `DropDeadHandlers` 只摘死的，不是「全清再挂」"
         + "｜🧨 改成「只留自己」⇒ 实得 1 ⇒ 红（= `B102AUD03R` A2 那条）");

        UnityEngine.Object.DestroyImmediate(win.gameObject);
        // 收尾：tb2 / tb3 是 win 的子节点，已随上面那句一起销毁
    }

    // ============================================================ C · `SceneInitializer.IsSettled` 的「已销毁」分支

    /// <summary>`SceneInitializer.IsSettled`（`Shell/SceneInitializer.cs:307-314`）里「被 Unity 销毁的件也算 settled」那一支。
    /// <para>⚠️ **不驱动协程**（编辑模式没有帧循环，`:280` 的 `while (!AllSettled(group)) yield return null;` 起不来、也不能驱动）——
    /// 改用两个纯函数读口 `SceneInitializer.IsSettledForTest` / `AllSettledForTest`，它们就是那个 `while` 判的东西本身
    /// （所以这是**行为级**断言，不是源码形状断言）。</para></summary>
    static void CaseCSettledBranch()
    {
        Debug.Log(_sink.P + "--- C：`SceneInitializer.IsSettled` 的「已销毁」分支（`报告/B103_D1.md` §5.1 那处同形缺陷）---");

        var go = new GameObject("B103W6 settled probe", typeof(RectTransform));
        var comp = go.AddComponent<MaterialChangerByQuality>();   // 运行时程序集里的具体 `InitializableBehaviour`
        Tr(comp != null, "C0-前提①：探针件（`InitializableBehaviour` 的具体子类）建出来了");
        if (comp == null) { UnityEngine.Object.DestroyImmediate(go); return; }

        comp.Status = InitializationStatus.Pending;               // = 「还在初始化、销毁前没定下来」那一档
        C(comp.Status, InitializationStatus.Pending, "C0-前提②：它的状态是 `Pending`（⇒ 光看 Status 判不出 settled）");

        Tr(!SceneInitializer.IsSettledForTest(comp),
           "C1 ★ **活着的** `Pending` 件 ⇒ `IsSettled == false`（协程应当继续等它）");
        Tr(!SceneInitializer.AllSettledForTest(new List<IBaseInitializable> { comp }),
           "C1b ★ 同上走 `AllSettled`（`:280` 那一跳的等价物）⇒ false");

        UnityEngine.Object.DestroyImmediate(go);
        Tr(!ReferenceEquals(comp, null) && comp == null,
           "C0-前提③：销毁之后 `comp` 是 **Unity 假 null**（引用非 null、`== null` 为真）");

        Tr(SceneInitializer.IsSettledForTest(comp),
           "C2 ★ **已销毁的件也算 settled** ⇒ true（`:310` 那条注释的语义）"
         + "｜🧨 换回 `obj != null && obj == null` ⇒ 本条红 —— 那样一个 `Initializing` 时被销毁的件会让 `:280` **永久空转且静默**");
        Tr(SceneInitializer.AllSettledForTest(new List<IBaseInitializable> { comp }),
           "C2b ★ 走 `AllSettled` ⇒ true（= 那条 `while` 会退出、组不会卡死）");

        bool oldPredicateOnDead = OldSettledPredicate(comp);
        Tr(!oldPredicateOnDead,
           "C3 ★ 灭自证：旧的 `obj != null && obj == null` 对**同一个**已销毁件返回 false ⇒ 那时 C2 必红");
        Tr(SceneInitializer.IsSettledForTest(comp) != oldPredicateOnDead,
           "C3b ★ 灭自证（结构锁）：新旧两条实现在「已销毁」这一格上**结论相反**");
    }

    /// <summary>⛔ `SceneInitializer.IsSettled` 被修掉的那个写法（逐字照旧代码）。只在灭自证那条里用。</summary>
    static bool OldSettledPredicate(IBaseInitializable i)
    {
        var obj = i as UnityEngine.Object;
        return obj != null && obj == null;   // ← 两边都绑 Unity 重载 ⇒ 恒 false
    }

    /// <summary>建一个 `Tab Buttons` 节点 + `TabButtons` 容器并 `Initialize`（与 `B102AUD03R_Assert` 的夹具同形）。</summary>
    static TabButtons NewTabButtons(GameWindowWithTabs win)
    {
        var go = new GameObject("Tab Buttons", typeof(RectTransform));
        go.transform.SetParent(win.transform, false);
        var tb = go.AddComponent<TabButtons>();
        tb.Initialize(win);
        return tb;
    }
}
