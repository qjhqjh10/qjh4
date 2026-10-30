// B97W11_Assert.cs — 第九十七会话 · 波 1 补位 · 执行代理 `B97_W11`
//
// ============================ 它盯的是什么 ============================
//  `RuleEngine/Core/BoardSlots.cs` 的 `NextSlotPos`（原版 `MinionManager.GetNextSlotPos` 的引擎半）**两处实现缺陷**：
//   ① **循环上界写错了对象**：原来 `if (Math.Max(右人数, 左人数) <= i) break;` —— 原版 `.c:64-69` 的上界是
//      `max(len(rightSlotPos…), len(leftSlotPos…))` = **每侧格数**（`BoardSpec.SlotsPerSide` = 4），**不是人数**。
//      把人数当格数 ⇒ **提前 break** ⇒ 掉进「一个都找不到」那一支（`LogError` + 右最外），**每次都回错值**。
//   ② **缺 `required == 1` 的 `pos.x == 0` 中性支**（`.c:178-276` 的坐标半）。原版两个调用点
//      **都传 `Vector2.zero`**（`AbilityLogic.cs:2038` ILSpy 现读：`GetNextSlotPos(Vector2.zeroVector, slot)`）
//      ⇒ 中性支才是实走的那条；我们原来一律「出声 + `int.MinValue`」。
//
// 判据（⛔ 别在这里抄第二份）：
//   · `d:/2/tools/decomp_full/MinionManager__GetNextSlotPos.c:44-95`（`required != 1` 的交替扫描）
//     ＋ `:165-276`（`required == 1` 的坐标半：右满 ⇒ `GetLeftSlotPos(leftCount)`、左满 ⇒ `GetRightSlotPos(rightCount)`、
//        `pos.x == 0` 中性支 = 人少那一侧、平手走右、两侧都满 ⇒ 督军 `GetEffectAnchor()`）。
//   · 交叉源（C 伪码丢了实参时用它）：`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:1017-1272`
//     —— 它把 `token 正 = 右(index-1) / 负 = 左(~token)` 那条解码写得很直白：
//     `vector24 = num8 < 0 ? GetLeftSlotPos(~num8) : GetRightSlotPos(num8 - 1);`
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一行块语句**（建议位置 = 现读 `:1309` 的
// `{ CheckB97Gen2(); }` **之后**、`:1311` 那段「静默少跑闸」注释**之前**）：
//       // 🆕 2026-10-29（调度台代接线）：`B97_W11` —— `BoardSlots.NextSlotPos` 上界 = 每侧格数 ＋ `required==1` 中性支。
//       { CheckB97W11Gaps(); }
//   🔴 **故意不用 `Step(...)`、方法名不带 `Test`/`Report` 前缀** —— 与 `B96_CR` / `B97_US` / `B97_W1` / `B97_ER2` / `B97_W2` 同一条理由：
//     `Step(...)` 会让 `_ranCases` 记账、`VerifyRunCaseCoverage()` 拿**反射清单**对账，而 `工具/gen_diag_runner.py`
//     只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用**两条都不碰**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **B 段（缺陷①）**：把 `if (i >= BoardSpec.SlotsPerSide)` 改回 `if (Math.Max(r, l) <= i)` ⇒ **B1 红**
//     （本夹具右 2 / 左 2、`required = 2`：旧实现 i=0/1 都被判「没空格」⇒ 一个都没找到 ⇒ 回 `+4`，不是 `-3`）。
//     E 段再补一条**结构上不可能同时满足**的：断言「生产实现」与「旧缺陷的逐字转录」在本夹具上**必须给出不同答案**
//     ⇒ 光把期望值改成 `+4` 也不行（E2 会红）。
//   · **C 段（缺陷②）**：把 `required == 1` 那 block 改回「出声 + `return int.MinValue`」⇒ **C1..C5 全红**
//     （C6 那一档两种写法都回 `int.MinValue`，所以它单独盖不住 —— 它就是原来那条路唯一覆盖的一档）。
//   · **D 段（交替次序）**：把「先右后左」改成「先左后右」⇒ D2..D8 全红（空场 `required=2` 会变 `+1` 而不是 `-1`）。
//   · **P 段（映射）**：把 `WarlordSlot` 改成 3（或把 `SlotOf` 的 `side*(index+1)` 改成 `side*index`）⇒ P 段红
//     —— 这一段钉的是 `BattleDriver.EnsureMinionSpotAnchor` 用的那条 `ourSlot = WarlordSlot + 有符号格号`。
//
// ============================ 已知偏差（⛔ 不是「已验收」）============================
//   · **两侧都满 / 一个空格都没有** 那一支：原版 `.c:151` 调 `GetRightSlotPos(-1)`，而 `GetRightSlotPos` 的
//     `if (slotNum >= len) { LogError; slotNum = max(0, len-1); }` 对 `-1` **不成立**（`-1 >= 4` 为假）⇒
//     接着越界访问 ⇒ **原版自己会抛 `IndexOutOfRangeException`**。我们回「右最外」`+SlotsPerSide`（我方等价物，已出声）。
//     ⇒ 这一格**没有原版期望值**，B3 断的是**我们挑的**那个等价物（如实标，⛔ 别当成原版行为）。
//   · **动画锚点的最终位置只能真机看** —— 本文件只钉**格号**（引擎半），不钉屏幕坐标。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97W11Gaps()
    {
        Section("🆕 B97_W11：`BoardSlots.NextSlotPos` 上界 = 每侧格数（不是人数）＋ `required==1` 的 `pos.x==0` 中性支");
        B97W11Preconditions();
        B97W11Bound();
        B97W11Neutral();
        B97W11Alternation();
        B97W11AntiSelfProve();
    }

    // ==================================================================
    //  夹具：每侧摆 n 个单位（**连续无洞** —— 引擎的增删路径恒保持这个形状）
    // ==================================================================
    static PlayerState B97W11Board(int left, int right)
    {
        var ps = new PlayerState { Name = "B97W11" };
        for (int i = 0; i < left; i++)
            ps.Board[BoardSlots.SlotOf(BoardSlots.Left, i)] = new UnitState(Unit("B97W11L" + i, 1, 1, 1), false);
        for (int i = 0; i < right; i++)
            ps.Board[BoardSlots.SlotOf(BoardSlots.Right, i)] = new UnitState(Unit("B97W11R" + i, 1, 1, 1), false);
        return ps;
    }

    // ==================================================================
    //  P · 前提：格号编码与接线用的那条映射（`ourSlot = WarlordSlot + 有符号格号`）
    // ==================================================================
    static void B97W11Preconditions()
    {
        Section("P 前提：`SlotsPerSide = 4` · `WarlordSlot = 4` · `WarlordSlot + 有符号格号 == SlotOf(侧, |格号| - 1)`");

        Check(BoardSpec.SlotsPerSide, 4, "P1 **前提断言**：`BoardSpec.SlotsPerSide == 4`（原版 `left/rightSlotPos.Length`，13 场逐场 4）");
        Check(BoardSpec.WarlordSlot, 4, "P2 **前提断言**：`BoardSpec.WarlordSlot == 4`（本文件的期望值全按这一条写）");
        Check(BoardSpec.Size, 9, "P3 **前提断言**：棋盘 9 格（4 + 督军 + 4）");

        // P4/P5：接线用的那条映射必须逐格成立（`BattleDriver.EnsureMinionSpotAnchor` 就是 `WarlordSlot + signed`）
        bool rightOk = true, leftOk = true;
        for (int k = 1; k <= BoardSpec.SlotsPerSide; k++)
        {
            rightOk &= (BoardSpec.WarlordSlot + k) == BoardSlots.SlotOf(BoardSlots.Right, k - 1);
            leftOk &= (BoardSpec.WarlordSlot - k) == BoardSlots.SlotOf(BoardSlots.Left, k - 1);
        }
        Check(rightOk, true, "P4 ★ `WarlordSlot + k` == 右侧第 k-1 格（k = 1..4）—— 接线 `ourSlot = WarlordSlot + signed` 的判据");
        Check(leftOk, true, "P5 ★ `WarlordSlot - k` == 左侧第 k-1 格（k = 1..4）—— 负号 = 左，`~i` = `-(i+1)` 的解码前提");
    }

    // ==================================================================
    //  B · 缺陷①：循环上界 = 每侧格数（`BoardSpec.SlotsPerSide`），⛔ 不是人数
    // ==================================================================
    static void B97W11Bound()
    {
        Section("B 缺陷① 上界：每侧格数（`.c:64-69`）—— 旧实现用「人数」⇒ 提前 break ⇒ 回错值");

        // B1 ★★ 简报点名的那一条：右 2 / 左 2、required = 2 ⇒ 左第 3 格（有符号 -3）
        //   走查（原版交替：每轮**先右后左**）：i=0 两侧都占着；i=1 两侧都占着；i=2 右空(count 1) → 左空(count 2 == required)
        //   ⇒ token = `~2` = **-3** ⇒ `GetLeftSlotPos(2)`。
        //   旧实现：`max(2,2) <= i` 在 i=2 时就 break（早一步），一个都没找到 ⇒ `LogError` + 右最外 `+4`。
        var ps = B97W11Board(2, 2);
        Check(BoardSlots.CountOnSide(ps, BoardSlots.Left), 2, "B0 **前提断言**：夹具左侧 2 个单位");
        Check(BoardSlots.CountOnSide(ps, BoardSlots.Right), 2, "B0b **前提断言**：夹具右侧 2 个单位");
        Check(BoardSlots.NextSlotPos(ps, 2), -3,
              "B1 ★★ 右 2 / 左 2、`required = 2` ⇒ **左第 3 格（-3）**（🧨 上界换回 `CountOnSide` ⇒ 这里变 +4，红）");
        Check(ps.Board[BoardSpec.WarlordSlot - 3], (UnitState)null,
              "B1b 那一格（左侧第 3 格 = 1 号格）**确实是空的** —— 挡「返回一个已经有人的格」那类实现");

        // B2：同一局 required = 1 ⇒ 中性支：两侧人数平手 ⇒ **走右**，`GetRightSlotPos(rightCount = 2)` ⇒ 右第 3 格（+3）
        Check(BoardSlots.NextSlotPos(ps, 1), 3,
              "B2 右 2 / 左 2、`required = 1` ⇒ 中性支平手走右 ⇒ 右第 3 格（+3）（`.c:239-241`）");

        // B3：8 个格全占满、required = 2 ⇒ 一个都没找到 ⇒ 原版 `LogError` + `GetRightSlotPos(-1)`
        //   ⚠️ **这一格没有原版期望值**：`GetRightSlotPos` 的夹取条件 `len <= slotNum` 对 -1 不成立 ⇒ 原版自己越界抛异常。
        //   `+SlotsPerSide` = 我们挑的等价物（已出声）⇒ 见文件头「已知偏差」。
        var full = B97W11Board(4, 4);
        Check(BoardSlots.NextSlotPos(full, 2), BoardSpec.SlotsPerSide,
              "B3 满场（8 格全占）、`required = 2` ⇒ 我方等价物 = 右最外（+4）；原版此处 `GetRightSlotPos(-1)` 越界抛 IndexOutOfRange");

        // B4：空场 required = 2 ⇒ i=0 先右(count 1) 再左(count 2) ⇒ `~0` = **-1**
        var empty = B97W11Board(0, 0);
        Check(BoardSlots.NextSlotPos(empty, 2), -1,
              "B4 空场、`required = 2` ⇒ 左内格（-1）（旧实现只枚举 i=0 也会给 -1 —— 所以单看这一条盖不住缺陷①，B1 才盖得住）");

        // B5：空场 required = 3 ⇒ i=1 的右侧(count 3) ⇒ `GetRightSlotPos(1)` = 右第 2 格（+2）
        //   🔴 **期望值是 +2 不是 +4**（协调单里写「回右最外」是口语；`B97_W3.md:58` 那张表自己写的就是「右第 2 格（+2）」）。
        Check(BoardSlots.NextSlotPos(empty, 3), 2,
              "B5 空场、`required = 3` ⇒ **右第 2 格（+2）**（旧实现只枚举 i=0 ⇒ 回 -1）");
    }

    // ==================================================================
    //  C · 缺陷②：`required == 1` 的 `pos.x == 0` 中性支（两个原版调用点都传 `Vector2.zero`）
    // ==================================================================
    static void B97W11Neutral()
    {
        Section("C 缺陷② `required == 1` 中性支：右满 ⇒ 左第 N 格 · 左满 ⇒ 右第 N 格 · 否则**人少那一侧**（平手走右）");

        // C1：空场（平手）⇒ 走右、`GetRightSlotPos(0)` ⇒ +1
        Check(BoardSlots.NextSlotPos(B97W11Board(0, 0), 1), 1,
              "C1 空场、`required = 1` ⇒ 右内格（+1）（平手走右，`.c:239-241`）");

        // C2：右少（左 2 / 右 0）⇒ 放右 `GetRightSlotPos(0)` ⇒ +1
        Check(BoardSlots.NextSlotPos(B97W11Board(2, 0), 1), 1,
              "C2 左 2 / 右 0、`required = 1` ⇒ 右内格（+1）（人少那一侧）");

        // C3：左少（左 0 / 右 2）⇒ 放左 `GetLeftSlotPos(0)` ⇒ -1
        Check(BoardSlots.NextSlotPos(B97W11Board(0, 2), 1), -1,
              "C3 左 0 / 右 2、`required = 1` ⇒ 左内格（-1）（人少那一侧）");

        // C4：右满（4）⇒ **不看 pos.x**，直接 `GetLeftSlotPos(leftCount = 1)` ⇒ 左第 2 格（-2）
        Check(BoardSlots.NextSlotPos(B97W11Board(1, 4), 1), -2,
              "C4 右满 / 左 1、`required = 1` ⇒ 左第 2 格（-2）（`.c:210-226`：右满 ⇒ `GetLeftSlotPos(leftCount)`）");

        // C5：左满（4）⇒ 直接 `GetRightSlotPos(rightCount = 1)` ⇒ 右第 2 格（+2）
        Check(BoardSlots.NextSlotPos(B97W11Board(4, 1), 1), 2,
              "C5 左满 / 右 1、`required = 1` ⇒ 右第 2 格（+2）（`.c:263-265`）");

        // C6：两侧都满 ⇒ `int.MinValue`（原版回督军 `GetEffectAnchor()`，由表现层落到督军锚点）
        Check(BoardSlots.NextSlotPos(B97W11Board(4, 4), 1), int.MinValue,
              "C6 两侧都满、`required = 1` ⇒ `int.MinValue`（= 原版那一支回督军 `GetEffectAnchor()`；"
            + "⚠️ 这一档**旧实现也给 int.MinValue** ⇒ 它单独盖不住缺陷②，C1..C5 才盖得住）");
    }

    // ==================================================================
    //  D · 交替次序（空场 required = 1..8 的完整序列）
    // ==================================================================
    static void B97W11Alternation()
    {
        Section("D 交替次序：空场 `required = 1..8` ⇒ +1 -1 +2 -2 +3 -3 +4 -4（每轮**先右后左**，`.c:48-87`）");

        var empty = B97W11Board(0, 0);
        var want = new[] { 1, -1, 2, -2, 3, -3, 4, -4 };
        for (int k = 1; k <= want.Length; k++)
        {
            int got = BoardSlots.NextSlotPos(empty, k);
            Check(got, want[k - 1],
                  "D" + k + " 空场 `required = " + k + "` ⇒ 有符号格号 " + want[k - 1]
                + "（🧨 把「先右后左」改成「先左后右」⇒ 从 D2 起全红）");
        }
    }

    // ==================================================================
    //  E · 灭自证：旧缺陷的**逐字转录**必须与生产实现给出**不同**答案
    // ==================================================================
    /// <summary>🔴 **这是【旧缺陷】的逐字转录，只给 E 段灭自证用，⛔ 生产代码里一个字都不许照它写。**
    /// 与 `BoardSlots.NextSlotPos` 唯一的差别 = 循环上界用**人数**（`Math.Max(右人数, 左人数)`）而不是每侧格数。</summary>
    static int B97W11OldCountBounded(PlayerState ps, int required)
    {
        int found = 0, taken = 0;
        for (int i = 0; ; i++)
        {
            int r = BoardSlots.CountOnSide(ps, BoardSlots.Right);
            int l = BoardSlots.CountOnSide(ps, BoardSlots.Left);
            if (System.Math.Max(r, l) <= i) break;               // ← 就是那处缺陷（原版是 `max(len(slotPos[]))`）
            if (r <= i) { taken++; found = i + 1; if (taken == required) break; }
            if (l <= i) { taken++; found = ~i;      if (taken == required) break; }
        }
        return found != 0 ? found : BoardSpec.SlotsPerSide;
    }

    static void B97W11AntiSelfProve()
    {
        Section("E 灭自证：在本夹具上，「旧上界（人数）」与「新上界（每侧格数）」**必须给出不同答案**");

        var ps = B97W11Board(2, 2);
        Check(B97W11OldCountBounded(ps, 2), BoardSpec.SlotsPerSide,
              "E1 前提：旧上界的转录在本夹具上回 +4（= 那个错值）—— 它若回 -3，说明本夹具盖不住缺陷①");
        Check(BoardSlots.NextSlotPos(ps, 2) != B97W11OldCountBounded(ps, 2), true,
              "E2 ★★ 生产实现与旧上界的转录**必须不同**：谁把上界改回去、或把 B1 的期望值跟着改成 +4，这里都红");

        // E3：🔴 2026-10-30 `B100_F34` 修正前提 —— 原写「空场 required = 2 两者**恰好相同**（-1）」，与事实**相反**。
        //     实况：旧上界的转录在空场 i=0 就 `max(0,0) <= 0` break ⇒ `found = 0` ⇒ 回落 `BoardSpec.SlotsPerSide`(**+4**)；
        //     生产实现给 **-1**（同文件 B4 就断言了 -1）⇒ 两者恰好**不同**，原来的 `==` 是恒红（正确的实现也被判失败）。
        //     ⇒ 这正好是「缺陷① 在空场也暴露」的正面证据。
        var empty = B97W11Board(0, 0);
        Check(B97W11OldCountBounded(empty, 2), BoardSpec.SlotsPerSide,
              "E3a 旧上界的转录在**空场**也回 +4（i=0 就 break ⇒ found=0 ⇒ 落 SlotsPerSide）—— 缺陷①在空场同样暴露");
        Check(B97W11OldCountBounded(empty, 2) != BoardSlots.NextSlotPos(empty, 2), true,
              "E3 空场 `required = 2`：两种上界**不同**（旧 +4 / 新 -1）⇒ E1/E2 那道灭自证在空场照样成立");
    }
}
