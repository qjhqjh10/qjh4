// B97_HUDROT_Assert.cs — 第九十七会话 · 执行代理 `B97_HUDROT` 的断言
//                      （账 `未分类_其他.tsv:584` `SimpleRotation.Update()`）
//
// ============================ 它盯的是什么 ============================
//   原版 `SimpleRotation`（`m_Script = -6857224376968460714`）在**战斗 HUD** 上有 39 个实例
//   = **13 场 × 3**，逐个现读**同值**（取证表 → `资料/完全复刻/报告/B97_HUDROT.md` §一）：
//     · 宿主 = `Drag Attack Selector` 的那三颗钮各自的子件 `Highlight`（GO 112 / 396 / 180）
//       —— 与 `AttackSelector.Order = { Melee, Ability, Ranged }` 一一对应；
//     · `RectTransform.m_SizeDelta = (176.22999572753906, 176.875)`、anchor/pivot 全居中、`localScale 1`、
//       `m_LocalRotation = identity`（起始角 0）；
//     · `Image.m_Sprite` = `Attack type button highlight`（227×227 · `m_PixelsToUnits 100`）；
//     · `speed = (0, 0, 100)` ⇒ `decomp_full/SimpleRotation__Update.c` 的
//       `eulerAngles += speed * Time.deltaTime` ⇒ **100°/s（3.6 s 一圈）**；
//     · 显隐 = `CardDisplayAttackTypeButton__HighlightIfNeeded.c` 第一句
//       `SetActive(highlightImage, param_2 == attackType)` ⇒ **只有「已选打法」那一格亮**。
//
//   本批落地的**唯一新行为** = `AttackSelector.AdvanceRingRotation`（在 `Tick` / `TickForTest` 里每帧推一格）。
//   ⇒ 本文件钉三件事：**几何（照原版写死的值）· 转速 100°/s · 显隐与「关着的那颗不转」**。
//
// 逐条结论 / 没查清的部分 → `资料/完全复刻/报告/B97_HUDROT.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么接（⚠️ 本代理不改宿主）============================
// 宿主建议 = **`CardPresentation/Editor/BattleScene.cs`**（战斗 HUD 的断言都在那儿；`A28` 就在 `:10052-10073`
// 逐值钉过 `Highlight` 的节点 rect）。接法：在 `BattleScene.Run()` 的 `:10073`（那句
// `"★ 这个单位**还没打过** ⇒ 各格都不放大…");`）**同一行末尾追加**（⇒ **行数零变化**，不会让账落点漂移）：
//     B97HUDROTAssert.Run(); pass += B97HUDROTAssert.Pass; fail += B97HUDROTAssert.Fail;
//   ⚠️ `BattleScene.Run` 的计数是**局部变量 `pass`/`fail`**（`:633`）＋ 局部 `Check`（`:637`）——
//      **没有 `_sink`**（那只在 `ShellScene` 那一族）。⛔ 别照抄 `_sink.Pass += …`，那在这里编译不过。
// ⚠️ **本文件自带夹具**（自己 `new GameObject` + `AddComponent<AttackSelector>()`），**不依赖宿主手里那只**
//    ⇒ 接在任何「TMP 字体已装好、批处理还在跑」的位置都行；临时对象在 `finally` 里 `DestroyImmediate`
//    （`BattleScene.Run` 收尾会 `SaveScene()`，⛔ 不许把自己的临时件留进 `Battle.unity`）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   A1..A3：把 `RingW/RingH` 改回按贴图比例算（176.2²）⇒ A2 的高差 0.675 px 超容差 ⇒ 红。
//   A4：    `_rings` 一颗都没建（贴图丢了）⇒ 前提条先红（⛔ 不会因为「循环 0 次」而空转绿）。
//   B1/B2： 把 `SimpleRotation.Step` 的乘改成加（`angle + speed`）或去掉 `dt` ⇒ B1 红。
//   B3/B4： 把 `RingSpinDegPerSec` 从 100 改成 90 / 180 ⇒ B3 红（25° 变 22.5 / 45）。
//   B5：    只在 `_ringSpinDeg` 上加、**不写** `transform.localRotation` ⇒ B5 红（两者不再一致）。
//   B6：    改成 `Quaternion.Euler(0,0,-angle)`（反向转）⇒ B6 红（`DeltaAngle(0, a)` 变负）。
//   B7：    删掉 `AdvanceRingRotation` 里那句 `!activeSelf ⇒ continue`（= 关着那颗也跟着转）⇒ B7 红。
//   C1..C4： 退回「三格全亮、非选的压暗」⇒ C2 红；`IsPickedSlot` 判错 ⇒ C1/C3 红。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_HUDROT` 的断言：战斗 HUD 三颗 `Highlight` 的自转（原版 `SimpleRotation`）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量**（`176.23 / 176.875 / 100° 每秒 / 25° / 75°`），不从被测实现里读。</para></summary>
public static class B97HUDROTAssert
{
    static readonly CheckSink _sink = new CheckSink("[HUDROT] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败的条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>唯一入口（宿主 `Run()` 里调一句）。</summary>
    public static void Run()
    {
        SectionA_RingGeometry();
        SectionB_Spin();
        SectionC_Visibility();
    }

    // ============================================================ 小工具
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Eq<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Nr(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>把角度差折算到 −180..180（跨 0/360 时直接相减会得出 ±360 的假差异）。</summary>
    static float Delta(float a, float b) { return Mathf.DeltaAngle(a, b); }

    /// <summary>建一只**自带**的攻击选择器（三格齐全，`selectedType` 按 `attackType` 位标志）。
    /// <para>⚠️ 编辑模式下 `AddComponent` **不会**跑 `Awake`，所以要靠 `Show()` 里的 `Build()` 建件
    /// （本仓的 `AttackSelector` 没有 `Create` 工厂，见它的文件头「组件是场景挂的」那段）。</para></summary>
    /// <summary>`finally` 里 `DestroyImmediate` 用（`BattleScene.Run` 收尾会 `SaveScene`，⛔ 不能留临时件）。
    /// <para>⚠️ `Go` 在 `AddComponent` **之前**就登记 —— `Show()` 中途抛异常也不会漏掉那个 GameObject。</para></summary>
    sealed class GameObjectsHolder
    {
        public GameObject Go;
        public void Destroy() { if (Go != null) { UnityEngine.Object.DestroyImmediate(Go); Go = null; } }
    }

    static AttackSelector NewSelector(GameObjectsHolder holder, int selectedType)
    {
        var go = new GameObject("B97HUDROT_Selector");
        holder.Go = go;
        var sel = go.AddComponent<AttackSelector>();
        sel.Show(new List<AttackSelector.Option>
        {
            new AttackSelector.Option { Kind = AttackKind.Melee, Enabled = true },
            new AttackSelector.Option { Kind = AttackKind.Ability, Enabled = true, Army = 10 },
            new AttackSelector.Option { Kind = AttackKind.Ranged, Enabled = true },
        }, null, null, null, selectedType: selectedType);
        return sel;
    }

    // ============================================================ A · 几何（照原版写死）
    static void SectionA_RingGeometry()
    {
        MenuCheck.Section(_sink, "A 三颗 `Highlight` 圈的节点 rect = 原版 **176.23 × 176.875**（13 场 × 3 逐个现读，同值）");

        var holder = new GameObjectsHolder();
        try
        {
            var sel = NewSelector(holder, 1);

            // ---- 前提（硬检查，⛔ 不用 `if (前提) Check(结论)` —— 那会在前提不成立时静默跳过）
            int built = 0;
            for (int i = 0; i < 3; i++) if (sel.HighlightPieceForTest(i) != null) built++;
            Eq(built, 3, "A4 ★（前提）三颗 `Highlight` **全部建出来了**（贴图缺一颗这里就红；"
                       + "⛔ 后面那几条几何断言靠它成立，不许空转）");
            Tr(sel.Visible, "A5（前提）选择器处于展开态（`Show` 之后 `Visible` 应为真）");
            if (built != 3) return;

            // ---- A1..A3：三种打法各一条（原版三份 RT 的 `m_SizeDelta` 完全相同）
            Nr(sel.HighlightNodeSizePx(AttackKind.Melee).x, 176.23f, 0.05f,
               "A1 ★ 近战那颗圈的宽 = **176.23**（原版 `RT_3368` 的子件 `RT_3028`）");
            Nr(sel.HighlightNodeSizePx(AttackKind.Melee).y, 176.875f, 0.05f,
               "A1b ★ 近战那颗圈的高 = **176.875**（⛔ 不是 176.23 —— 高差 0.675 px 是原版的真值）");
            Nr(sel.HighlightNodeSizePx(AttackKind.Ability).x, 176.23f, 0.05f,
               "A2 ★ 主动技能那颗圈的宽 = **176.23**（原版 `RT_2626` 的子件 `RT_2776`）");
            Nr(sel.HighlightNodeSizePx(AttackKind.Ranged).x, 176.23f, 0.05f,
               "A3 ★ 远程那颗圈的宽 = **176.23**（原版 `RT_2804` 的子件 `RT_3512`）");
            Nr(sel.HighlightNodeSizePx(AttackKind.Ranged).y, 176.875f, 0.05f,
               "A3b ★ 远程那颗圈的高 = **176.875**");
        }
        finally { holder.Destroy(); }
    }

    // ============================================================ B · 转速（100°/s）
    static void SectionB_Spin()
    {
        MenuCheck.Section(_sink, "B 自转 = **100°/s**（3.6 s 一圈）· 写进节点 `localRotation` · 关着那颗不转");

        // ---- B1/B2：先把**数学那一半**钉死（`CardPresentation.SimpleRotation`，`Shell/LoadingMenu.cs`）
        //      原版 `SimpleRotation__Update.c`：`eulerAngles += speed * Time.deltaTime`，逐轴。
        Nr(SimpleRotation.Step(0f, 100f, 0.5f), 50f, 1e-3f,
           "B1 ★ `Step(0, 100°/s, 0.5s)` = **50°**（⛔ 写成 `angle + speed` 或漏 `dt` 这一条就红）");
        Nr(SimpleRotation.Step(350f, 100f, 1f), 90f, 1e-3f,
           "B2 ★ 跨 360 时回到 0..360（`350 + 100 = 450 ⇒ 90`；= 原版下一次读 `eulerAngles` 的值域）");

        var holder = new GameObjectsHolder();
        try
        {
            var sel = NewSelector(holder, 1);      // selectedType = 1 ⇒ 亮的是「近战」那颗（Order 下标 0）

            Tr(sel.Visible, "B0（前提）选择器展开着（`TickForTest` 在收起态会直接返回、什么都不推）");
            if (!sel.Visible) return;
            Tr(sel.HighlightActiveForTest(0), "B0b（前提）近战那颗圈亮着（转速只对亮着的那颗有定义）");
            if (!sel.HighlightActiveForTest(0)) return;

            // 起始角：原版序列化 `m_LocalRotation = identity` ⇒ 0°
            Nr(sel.RingNodeRotationZForTest(AttackKind.Melee), 0f, 1e-3f,
               "B3b 起始角 = **0°**（原版 `Highlight` 的 `m_LocalRotation` 是 identity）");

            // ---- B3：0.25 s ⇒ 25°
            sel.TickForTest(0.25f, Vector2.zero);
            Nr(sel.RingNodeRotationZForTest(AttackKind.Melee), 25f, 0.05f,
               "B3 ★ 过 0.25 s ⇒ **25°**（100°/s；改成 90°/s 得 22.5、180°/s 得 45 ⇒ 都红）");

            // ---- B4：再过 0.5 s ⇒ 累计 75°
            sel.TickForTest(0.5f, Vector2.zero);
            Nr(sel.RingNodeRotationZForTest(AttackKind.Melee), 75f, 0.05f,
               "B4 ★ 再过 0.5 s ⇒ 累计 **75°**（累加、⛔ 不是「每帧重算成 25°」）");

            // ---- B5：累加器与节点 rotation 必须一致（防「只动私有 float、没写 transform」）
            float acc = sel.RingSpinDegForTest(0);
            float node = sel.RingNodeRotationZForTest(AttackKind.Melee);
            Nr(Mathf.Abs(Delta(acc, node)), 0f, 0.01f,
               $"B5 ★ 累加器（{acc:F2}°）与节点 `localRotation.z`（{node:F2}°）一致 —— "
             + "🧨 只加 float 不写 transform ⇒ 这条红（画面完全不动，而 B3/B4 若也读 float 就会一起骗过）");

            // ---- B6：方向是 **+Z**（speed.z = 100 > 0 ⇒ 逆时针增角），⛔ 不是反向
            Tr(Delta(0f, node) > 1f,
               "B6 ★ 累计角是**正的**（`DeltaAngle(0, 当前)` = " + Delta(0f, node).ToString("F2")
             + "° > 0）—— 🧨 写成 `Quaternion.Euler(0,0,-angle)` 这条红");

            // ---- B7：**关着的那两颗不转**（原版：GameObject 关着 ⇒ `Update` 不跑 ⇒ 角度冻住）
            Nr(sel.RingNodeRotationZForTest(AttackKind.Ability), 0f, 1e-3f,
               "B7 ★ 主动技能那颗（没选中、`activeSelf = false`）**角度冻在 0** —— "
             + "🧨 删掉 `AdvanceRingRotation` 里的 `!activeSelf ⇒ continue`（三颗无条件跟着转）⇒ 这条红");
            Nr(sel.RingSpinDegForTest(1), 0f, 1e-3f,
               "B7b ★ 同一条的累加器那一侧：`RingSpinDegForTest(1)` 仍是 0（= 它一次都没被推进过）");
        }
        finally { holder.Destroy(); }
    }

    // ============================================================ C · 显隐（「该藏的时候藏住了吗」）
    static void SectionC_Visibility()
    {
        MenuCheck.Section(_sink, "C 圈的显隐 = 只有「已选打法」那一格亮（原版 `HighlightIfNeeded` 第一句 `SetActive`）");

        var holder = new GameObjectsHolder();
        try
        {
            var sel = NewSelector(holder, 1);      // 1 = Melee

            // ---- C1/C2：selectedType = 1 ⇒ 只有近战亮；另外两颗**真的关着**（不是压暗）
            Tr(sel.HighlightActiveForTest(0),
               "C1 ★ `selectedType = 1`（近战）⇒ 近战那颗圈**亮**");
            Tr(!sel.HighlightActiveForTest(1) && !sel.HighlightActiveForTest(2),
               "C2 ★ 「该藏的时候藏住了吗」：技能 / 远程那两颗的 **`gameObject.activeSelf` 为假**"
             + " —— 🧨 退回「三格全亮、非选的压暗」⇒ 这条红（压暗的圈还会吃射线，原版关着的圈收不到）");
            var p1 = sel.HighlightPieceForTest(1);
            Tr(p1 != null && p1.gameObject != null && !p1.gameObject.activeSelf,
               "C2b ★ 同一条的**独立**读法：直接读那颗件的 `gameObject.activeSelf` = 假"
             + "（⛔ 不复用 `HighlightActiveForTest`，免得两处同一个口一起错）");

            // ---- C3：换一个已选打法（2 = Ranged）⇒ 亮的换人，近战灭
            sel.Show(new List<AttackSelector.Option>
            {
                new AttackSelector.Option { Kind = AttackKind.Melee, Enabled = true },
                new AttackSelector.Option { Kind = AttackKind.Ability, Enabled = true, Army = 10 },
                new AttackSelector.Option { Kind = AttackKind.Ranged, Enabled = true },
            }, null, null, null, selectedType: 2);
            Tr(sel.HighlightActiveForTest(2) && !sel.HighlightActiveForTest(0) && !sel.HighlightActiveForTest(1),
               "C3 ★ `selectedType = 2`（远程）⇒ **只有远程**那颗亮（近战/技能都灭）"
             + $"（实得 近战 {sel.HighlightActiveForTest(0)} / 技能 {sel.HighlightActiveForTest(1)}"
             + $" / 远程 {sel.HighlightActiveForTest(2)}）");

            // ---- C3b：刚亮起的那颗从「冻住的角度」续上（B7 的另一面：关着的期间没走，所以还是 0）
            Nr(sel.RingNodeRotationZForTest(AttackKind.Ranged), 0f, 1e-3f,
               "C3b ★ 刚亮起的远程那颗起始角是 **0°**（它此前一直关着 ⇒ 没被 `Update` 推过）");

            // ---- C4：整条收起 ⇒ 三颗全灭
            sel.Hide();
            Tr(!sel.HighlightActiveForTest(0) && !sel.HighlightActiveForTest(1) && !sel.HighlightActiveForTest(2),
               "C4 ★ `Hide()` 之后**三颗全灭**（原版选择器收起 = `Toggle(false)` 把三颗 GameObject 关掉）");
            Tr(!sel.Visible, "C4b ★ ……而且 `Visible` 为假（`Tick` / `TickForTest` 从此早退 ⇒ 自转也停）");
        }
        finally { holder.Destroy(); }
    }
}
