// B117_G10_15_Assert.cs — 执行代理 `B117-G10-15`（战斗域第 15 部分「墓地 · 战斗日志」）的断言
//
// ============================ 它盯的是什么 ============================
// 本批对 `Battle/BattleLogPanel.cs` 改掉的三件事（判据全部一手读过，逐条写在下面）：
//   A. **滑窗曲线的形状**：原版 `CemeteryManager__ShowCemeteryLogBtn.c:17/51` 是
//      `DOAnchorPosX(rt, finalX / initialX, DAT_1834b2dc8)`，**没有 `SetEase`** ⇒ DOTween 默认
//      **OutQuad**（`p(t) = 1 − (1−t)²`）；`DAT_1834b2dc8` 从 `GameAssembly.dll` 实读 = **0.3f**
//      （`Unity/工具/read_literal.py`）。收起位 −1200 / 拉开位 87 = `MonoBehaviour_4491.json` 的
//      `initialX` / `finalX`（场景里那份 `CemeteryManager`）。
//   B. **开 / 关两条路的补间语义【不一样】**（本批修的一处真差异）：
//      · `ShowCemeteryLogBtn.c:10-15` —— 开头 `IsPlaying(+0x98)` 为真就 `Complete(+0x98)`
//        ⇒ 飞行中的补间**一把推到底**再起新的；
//      · `HideCemeteryLogBtn.c` —— **开头没有那两句**（整个方法里既没有 `IsPlaying` 也没有 `Complete`）
//        ⇒ 关面板时 DOTween 直接对同一个 target 起新补间（默认冲突策略 Kill），**从当前位置往回滑**。
//      旧实现 `Hide()` 里也调了 `SnapSlide()` ⇒ 半开时点关闭会先把面板「啪」地推到全开、再滑回去。
//   C. 两个**颜色原值**（原来都是四舍五入过的）：
//      `BG` 的 `m_Color` = **(0, 0.07547170, 0.008201491, 1)**（`MonoBehaviour_4620.json`）；
//      `shade` 的 `m_Color.a` = **0.5529412**（`MonoBehaviour_4611.json`）。
//      两条都由 `BattleLogPanel.BgColor` / `ShadeColor` 承载 —— 本文件**读不到私有字段**，
//      所以 C 组只断「这两个值能被 `Build()` 用到」的间接量（见 C1/C2 的说明），
//      真正的逐位值记在 `判据/B117-G10-15_墓地与战斗日志.md` 的差异表里。
//
// ============================ 判据出处（⛔ 别在这里抄第二份）============================
//   · `d:/2/tools/decomp_full/CemeteryManager__ShowCemeteryLogBtn.c`（开 + 关两支 + Complete 的位置）
//   · `d:/2/tools/decomp_full/CemeteryManager__HideCemeteryLogBtn.c`（**没有** Complete）
//   · `d:/2/tools/decomp_full/CemeteryManager__AddActionToCemetery.c:60-160`（10 行的填法与溢出）
//   · `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4491.json`
//     （`initialX` / `finalX` / `cemeteryActions` 10 项 / `emptyText` = null）
//   · 运行时实况 `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive.tsv:117,134,142,143`
//     （`ShowCemeteryBtn` activeSelf=True · `CemeteryLogPanel` activeSelf=False · 行容器 748×431.3 · 10 行）
//
// ============================ 怎么用 ============================
//   `B117_G10_15_Assert.Run();`（静态类 + 无参 `Run()`；宿主由调度台接，本文件不改任何 `Editor/*Scene.cs`）
//   ⚠️ 要**主对话**跑（子代理不许 `-executeMethod`）：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_15_Assert.Run -logFile -
//   判绿红：`grep "^B117G1015"` 的**合计行**；逐条按**行首** `B117G1015 ✓` / `B117G1015 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ============================ 宿主状态要求 ============================
//   · A 组（A1–A7）**不需要任何对象** —— 纯静态函数 + 静态常量，任何宿主都能跑。
//   · B 组（B1–B8）要**一台已建好的 `BattleLogPanel`**（`BattleLogPanel.Create` 跑过）。
//     找不到 ⇒ **一条红**（⛔ 不静默跳过 —— 见 CLAUDE.md「`if (x != null) Check(…)` = 静默跳过」那条）。
//     跑的是**生产路径本身**（`Show()` / `Hide()` / `AdvanceSlideForTest`），不是另写一份模拟。
//   · 收尾把面板**收回关闭态**（`Hide()` + `FinishSlideForTest()`），不把可见性 / 滑动方向留给后面的宿主。
using UnityEngine;
using CardPresentation;

public static class B117_G10_15_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B117G1015";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5")
                  + "；实得 " + got.ToString("F5") + "）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  原版**字面量**（⛔ 不拿 `BattleLogPanel` 自己的常量反推 —— 那就是拿实现证明实现）
    //    `MonoBehaviour_4491.json`：`initialX: -1200.0` · `finalX: 87.0` · `cemeteryActions` 10 项
    //    `DAT_1834b2dc8`（`GameAssembly.dll` `.rdata` 4B float）= 0.3
    //    `CemeteryActions` 的 `m_SizeDelta.y` = 431.34399 ÷ 10 行 = 43.134399
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    const float OrigInitialX = -1200f;
    const float OrigFinalX = 87f;
    const float OrigSlideDur = 0.3f;
    const float OrigRowHeightPx = 43.134399f;

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(Tag + " ---- A 组：滑窗曲线与行数（纯静态，不需要任何对象）----");
        GroupA_Curve();
        Debug.Log(Tag + " ---- B 组：开 / 关两条路的补间语义（需要一台 BattleLogPanel）----");
        GroupB_SlideSemantics();
        Debug.Log(Tag + " 合计 pass=" + _pass + " fail=" + _fail + "（A 组 12 条 · B 组 19 条）");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  A 组：曲线的**形状**（这是「是不是 OutQuad」唯一的机器判据）
    // ─────────────────────────────────────────────────────────────────────────────
    static void GroupA_Curve()
    {
        // A1 端点 + 时长（原版三个字面量）
        CheckNear(BattleLogPanel.SlideClosedXPx, OrigInitialX, 0.01f, "A1a 收起位 = 原版 `initialX` −1200");
        CheckNear(BattleLogPanel.SlideOpenXPx, OrigFinalX, 0.01f, "A1b 拉开位 = 原版 `finalX` 87");
        CheckNear(BattleLogPanel.SlideDurationForTest, OrigSlideDur, 1e-4f,
                  "A1c 补间时长 = `DAT_1834b2dc8` 实读 0.3 s");
        CheckNear(BattleLogPanel.SlidePxAt(0f), OrigInitialX, 0.01f, "A2a `SlidePxAt(0)` = 收起位");
        CheckNear(BattleLogPanel.SlidePxAt(1f), OrigFinalX, 0.01f, "A2b `SlidePxAt(1)` = 拉开位");

        // A3 OutQuad 的**签名**：半程已走完 75%（线性只有 50%）
        float span = OrigFinalX - OrigInitialX;                    // 1287
        float half = BattleLogPanel.SlidePxAt(0.5f);
        float frac = (half - OrigInitialX) / span;
        CheckNear(frac, 0.75f, 1e-4f,
                  "A3 半程时已走完 **75%**（OutQuad 的签名 `1−(1−0.5)²`；线性会是 50%）");
        // 🧨 灭自证（1/2）：把上面那条换成「线性补间」必须红 —— 判据是**离线性值足够远**，
        //    只断「等于 −234.75」还不够（有人把 Lerp 系数写成别的东西也可能碰巧接近）。
        float linearHalf = OrigInitialX + span * 0.5f;             // −556.5
        Check(Mathf.Abs(half - linearHalf) > 100f,
              "🧨 A4【灭自证】半程位置离**线性补间**的 −556.5 至少 100 px（实差 "
              + Mathf.Abs(half - linearHalf).ToString("F1") + " px）—— 把缓动改成线性 ⇒ 本条红");

        // A5 单调 + 不越界（整条曲线）
        bool mono = true, inRange = true;
        float prev = BattleLogPanel.SlidePxAt(0f);
        for (int i = 1; i <= 20; i++)
        {
            float v = BattleLogPanel.SlidePxAt(i / 20f);
            if (v < prev - 1e-4f) mono = false;
            if (v < OrigInitialX - 0.01f || v > OrigFinalX + 0.01f) inRange = false;
            prev = v;
        }
        Check(mono && inRange, "A5 0→1 全程单调不减、且不越出 [−1200, 87]（21 个采样点）");

        // A6 前快后慢（OutQuad 的另一条等价判据）：后 1/4 只走 6.25%
        float frac75 = (BattleLogPanel.SlidePxAt(0.75f) - OrigInitialX) / span;
        CheckNear(frac75, 0.9375f, 1e-4f, "A6 走到 3/4 时已完成 **93.75%**（前段快、尾段慢）");

        // A7 越界入参不改语义（`Mathf.Clamp01` 由调用方的 `_slideT` 负责；这里是纯线性插值 ⇒ 不外推）
        CheckNear(BattleLogPanel.SlidePxAt(0f) + BattleLogPanel.SlidePxAt(1f), OrigInitialX + OrigFinalX, 0.02f,
                  "A7 两端点之和守恒（曲线没有被写成 `t²` 那类会挪动端点的版本）");

        // A8/A9 行数（focus 2 的一半：满 10 行是**硬编码的容器容量**，见 `MB_4491.cemeteryActions` 10 项）
        Check(BattleLogPanel.RowTotal == 10,
              "A8 `RowTotal` = 10（原版 `CemeteryManager.cemeteryActions` 数组**硬编码 10 项**）");
        CheckNear(BattleLogPanel.RowHeightPx, OrigRowHeightPx, 0.01f,
                  "A9 行高 = `CemeteryActions.m_SizeDelta.y` 431.34399 ÷ 10");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  B 组：开 / 关的补间语义（**两条路不一样** —— 本批修的就是这个）
    // ─────────────────────────────────────────────────────────────────────────────
    static void GroupB_SlideSemantics()
    {
        var p = Object.FindObjectOfType<BattleLogPanel>();
        // B0 前提（⛔ 不写成 `if (p != null) { … }` —— 那会让没有面板的宿主**一条都不跑**还看着全绿）
        Check(p != null,
              "B0（前提）宿主里有一台建好的 `BattleLogPanel`（`BattleLogPanel.Create` 跑过）");
        if (p == null) return;

        // 摆到「收在屏幕外」的干净初态
        p.Hide(); p.FinishSlideForTest();
        Check(!p.Visible && Mathf.Abs(p.PanelLeftX - OrigInitialX) < 0.5f,
              "B1 初态：面板收在屏幕外（左缘 " + p.PanelLeftX.ToString("F1") + " px）");

        // B2 `Show()` 从收起位起滑（⛔ 不是切显隐）
        p.Show();
        Check(p.Visible, "B2a `Show()` 之后 `Visible` = true");
        CheckNear(p.SlideT, 0f, 1e-4f, "B2b 起滑时进度仍是 0（不是「啪」一下到位）");
        Check(p.SlideDirForTest == +1, "B2c 方向 = +1（正拉开）");

        // B3 半程
        p.AdvanceSlideForTest(0.15f);                       // 0.15 / 0.3 = 半个时长
        CheckNear(p.SlideT, 0.5f, 1e-3f, "B3a 推进半个时长 ⇒ 进度 0.5");
        CheckNear(p.PanelLeftX, BattleLogPanel.SlidePxAt(0.5f), 0.5f,
                  "B3b 左缘 = 曲线在 0.5 处的值（−234.75）");
        Check(p.PanelLeftX > OrigInitialX + 1f && p.PanelLeftX < OrigFinalX - 1f,
              "B3c 判别式：半程时左缘**落在两端之间**（⇒ 它真的在滑，不是切显隐）");

        // B4 🧨 **灭自证（2/2）**：`Hide()` 不许把飞行中的补间推到终点
        //    原版 `HideCemeteryLogBtn.c` 开头**没有** `IsPlaying/Complete`（那两句只在 `ShowCemeteryLogBtn.c:10-15`）
        //    ⇒ 半开时关面板 = 从当前位置往回滑 ⇒ 进度应当**还是 0.5**。
        //    旧实现（`Hide()` 里调 `SnapSlide()`）会给 **1.0** ⇒ 本条红。
        p.Hide();
        CheckNear(p.SlideT, 0.5f, 1e-3f,
                  "🧨 B4 半开时 `Hide()` **不从当前位置跳走**（进度仍 0.5，⛔ 不是被推到底）"
                + "｜把 `Hide()` 里的 `SnapSlide()` 加回去 ⇒ 本条红");
        Check(p.SlideDirForTest == -1, "B4b 方向 = −1（正收起）");
        Check(!p.Visible, "B4c `Visible` 立刻为 false（收起补间还在走 —— 与原版同）");

        // B5 收完
        p.AdvanceSlideForTest(0.15f);
        CheckNear(p.SlideT, 0f, 1e-3f, "B5a 再推半个时长 ⇒ 进度 0（收完）");
        CheckNear(p.PanelLeftX, OrigInitialX, 0.5f, "B5b 左缘回到 −1200");
        Check(!p.RootActive, "B5c 收完之后面板节点被关掉（`RootActive` = false）");

        // B6 `Show()` 的 `Complete()` 语义：飞行中的补间**一把推到底**（推到「开」那一端）
        p.Show();
        p.AdvanceSlideForTest(0.15f);                       // 开到半程
        p.Show();                                           // 原版 `ShowCemeteryLogBtn.c:10-15` 的 Complete
        CheckNear(p.SlideT, 1f, 1e-4f,
                  "🧨 B6 开窗途中再 `Show()` ⇒ 进度被 `Complete()` **一把推到底**（= 1）"
                + "｜把 `Show()` 里的 `SnapSlide()` 删掉 ⇒ 本条红");

        // B7 反向判别：**推到底 = 推到「那条补间自己的终点」**，不是恒定推成 1
        //    此时 `_slideT = 1`；先 `Hide()` 让方向变成 −1，推半程到 0.5，再 `Show()`
        //    ⇒ `SnapSlide()` 见到方向 −1 ⇒ 先落到 **0（收起端）** 再起开窗补间。
        p.Hide();
        p.AdvanceSlideForTest(0.15f);
        CheckNear(p.SlideT, 0.5f, 1e-3f, "B7a（前提）现在正收到半程（0.5）");
        p.Show();
        CheckNear(p.SlideT, 0f, 1e-4f,
                  "🧨 B7 收窗途中再 `Show()` ⇒ 进度先落到 **0（收起端）** 再起开窗补间"
                + "｜把 `SnapSlide()` 写成「恒设 1」⇒ 本条红（那会让面板凭空跳到全开）");
        Check(p.SlideDirForTest == +1, "B7c 方向回到 +1");

        // B8 收尾：收成关闭态，不把可见性/方向留给后面的宿主
        p.Hide(); p.FinishSlideForTest();
        Check(!p.Visible && Mathf.Abs(p.PanelLeftX - OrigInitialX) < 0.5f,
              "B8 收尾：面板已收回关闭态（不影响后面的宿主）");
    }
}
