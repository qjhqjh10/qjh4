// B98N1_Assert.cs — 执行代理 `B98_N1`（第九十八会话 · 波 12 · 2026-10-29）
//
// ============================ 它盯的是什么 ============================
//   用户 2026-10-29 裁定：「『点我方牌堆摊开牌库』删不删 —— 这个删除，因为脱离了原版，
//   我自己就可以真机检查。」⇒ 本件钉两件事：
//     · **A 段（纯反射，任何宿主都能跑）**：那条交互的入口**真的没了**
//       （`BattleDriver.ShowMyDeck` 不存在）；而且**没有连坐删掉**原版真有的两件
//       （`HitMyDeckPile` = 按住牌库里的卡 ⇒ 显示牌库数；`OpenShowCardsWindow` = 那扇窗唯一的原版入口）。
//     · **B 段（需要活驱动）**：按住我方牌堆 ⇒ 牌库数照亮（原版 `ShowDeckSize`）；
//       点我方牌堆 ⇒ **不再摊开牌库**。
//   判据出处（⛔ 别在这抄第二份）→ `Battle/BattleDriver.cs` 里 `HitMyDeckPile` 上方那段
//   `B98_N1` 删除记号（含 ①②③ 三处原版判据）＋ 报告 `资料/完全复刻/报告/B98_N1.md`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B98N1Assert.Run();` ＋ `B98N1Assert.CheckB98N1(driver);`
//   ＋ 两句计数转发：`pass += B98N1Assert.Pass; fail += B98N1Assert.Fail;`
//   · `Run()` = **A 段**（纯反射，零夹具，任何宿主都跑得动）。
//   · `CheckB98N1(driver)` = **B 段**，必须有**活的 `BattleDriver`**（推荐宿主 `Editor/BattleScene.cs`）。
//   ⚠️ **接在哪**：B 段会跑三帧 `TickFrameForTest()`（跑的是生产 `Update`）⇒ 请接在
//      「**我方回合 · 不在换牌 · 没有模态窗**」的那一段。本件自己会再判一次前提，
//      不满足时**红**（⛔ 不是静默跳过 —— 那是 `if (前提) Check(...)` 的老坑）。
//   ⚠️ B 段会把 `_command` / `_selectedSlot` 置成中性（`None` / `-1`），并显式把
//      `MultiCards` 关掉；接在**本段之后不再依赖上一次选中状态**的位置。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A1 把 `ShowMyDeck` 加回 `BattleDriver`（哪怕只是个空方法 / 只写注释不算）⇒ **A1 红**。
//   A2 顺手把 `HitMyDeckPile` 一起删掉（「反正没人点了」）⇒ **A2 红** —— 它是**原版真有**的那条
//      「按住牌库里的卡 ⇒ 显示牌库数」的命中口（`CardScript__OnTouchDown.c` `case 0`）。
//   A3 把 `OpenShowCardsWindow`（那扇窗**唯一**的原版入口）删掉 ⇒ **A3 红**。
//   A4 把 `MultiCards` 读口删掉 ⇒ **A4 红**（B 段也就没有观察点了）。
//   B1 把 `CardHoldPeek` 里 `HitMyDeckPile(WorldPointer())` 改成恒 `false`（顺手「清理」）⇒ **B1 红**。
//   B2 把「点牌堆 ⇒ `ShowMyDeck(true)`」那一支加回 `Update` 的松手沿（换个名字也一样）⇒ **B2 红**。
//   B2′ 反向：把「摊开牌库」改成别的窗 / 别的名字 ⇒ B2 也红（它断的是「这一下**什么都不该摊开**」）。
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B98_N1`：「点我方牌堆 ⇒ 摊开牌库」那条**原版没有**的交互已删除的断言。
/// <para>⛔ 期望值全是**字面量/反射事实**，不从被测实现里反算。</para></summary>
public static class B98N1Assert
{
    static readonly CheckSink _sink = new CheckSink("[B98N1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const BindingFlags AnyMember =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // =============================================================================================
    //  A 段 —— 纯反射，零夹具
    // =============================================================================================

    /// <summary>A 段：入口删干净了 + 原版那两件还在。任何宿主都能跑。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "A · 结构（`ShowMyDeck` 已删 / 原版那两件还在）");

        // ---- A1：「点我方牌堆 ⇒ 摊开牌库」的入口不存在 ----
        var gone = typeof(BattleDriver).GetMethods(AnyMember);
        int hit = 0;
        for (int i = 0; i < gone.Length; i++)
            if (gone[i].Name == "ShowMyDeck") hit++;
        C(hit, 0,
          "A1 ★ `BattleDriver.ShowMyDeck` **不存在了**（点我方牌堆摊开牌库那条入口已按用户 2026-10-29 裁定删除）"
        + $"｜🧨 把它加回来（哪怕只是个空方法）⇒ 本条红（实得 {hit} 个同名方法）");

        // ---- A2：`HitMyDeckPile` 必须还在（原版真有的那条命中口）----
        var m = typeof(BattleDriver).GetMethod("HitMyDeckPile", AnyMember);
        Tr(m != null,
           "A2 ★ `HitMyDeckPile` **仍然在**（原版真有：「按住牌库里的卡 ⇒ 显示牌库数」，"
         + "`CardScript__OnTouchDown.c:34-59` `case 0` → `BattleManager.ShowDeckSize`；"
         + "我们的消费者 = `CardHoldPeek` / `TutorialClickHitsSomething`）"
         + "｜🧨 删「点牌堆摊开」时把它一起删掉 ⇒ 本条红（原版那条也跟着没了）");
        Tr(m != null && m.IsStatic && m.ReturnType == typeof(bool) && m.GetParameters().Length == 1
           && m.GetParameters()[0].ParameterType == typeof(Vector3),
           "A2′ `HitMyDeckPile` 的形状没变（`static bool (Vector3 world)`）"
         + "｜🧨 改成实例方法 / 换签名 ⇒ 所有调用点的语义都要重核 ⇒ 本条红");

        // ---- A3：原版那唯一入口必须还在 ----
        var open = typeof(BattleDriver).GetMethod("OpenShowCardsWindow",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Tr(open != null,
           "A3 ★ `OpenShowCardsWindow` **仍然在** —— 那扇窗**唯一**的原版入口"
         + "（效果 `showCardsWindow(470)` → 动作 `86` → `ResolveAction.c:8115` → `BattleHud.DisplayMultiCardView`）"
         + "｜🧨 一并删掉 ⇒ 那张卡（全池只有 `GSC55`）的效果就静默失效了 ⇒ 本条红");

        // ---- A4：观察点（B 段也靠它）----
        var mc = typeof(BattleDriver).GetProperty("MultiCards",
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Tr(mc != null && mc.PropertyType.Name == "MultiCardDisplay",
           "A4 `MultiCards` 读口还在（`MultiCardDisplay`）—— 那扇窗本身**没被删**，删的只是「点牌堆」这条入口"
         + "｜🧨 把窗一起删掉 ⇒ 本条红");
    }

    // =============================================================================================
    //  B 段 —— 行为：需要活的 `BattleDriver`
    // =============================================================================================

    /// <summary>B 段：按住我方牌堆 ⇒ 牌库数亮（原版行为仍在）；点我方牌堆 ⇒ 不再摊开牌库。</summary>
    public static void CheckB98N1(BattleDriver driver)
    {
        MenuCheck.Section(_sink, "B · 行为（点我方牌堆不再摊开牌库）");

        if (driver == null)
        {
            Tr(false, "B0（前提）拿到活驱动 `BattleDriver driver` —— ⛔ 拿不到就不是「跳过」，是本条红"
                    + "（`if (前提) Check(...)` 那种静默跳过正是本工程踩过的坑）");
            return;
        }
        var mcd = driver.MultiCards;
        Tr(mcd != null, "B0′（前提）多卡摊开窗建出来了（原版 `UIMultiCardDisplay`）");
        if (mcd == null) return;

        var ctx = driver.Ctx;
        bool ctxOk = ctx != null && !ctx.IsOver && ctx.Turn > 0;
        Tr(ctxOk, "B0″（前提）这一局还活着且已开过第 1 回合"
                + $"（Ctx {(ctx == null ? "null" : "ok")} · IsOver {(ctx != null && ctx.IsOver)}"
                + $" · Turn {(ctx != null ? ctx.Turn : -1)}）");
        if (!ctxOk) return;

        bool frameOk = !driver.InMulligan && ctx.Active == driver.MySeat && !driver.HudButtonsBlockedForTest;
        Tr(frameOk, "B0‴（前提）这一帧到得了 `Update` 的松手沿那一支"
                  + $"（InMulligan {driver.InMulligan} · Active {ctx.Active} / 我 {driver.MySeat}"
                  + $" · 模态窗 {driver.HudButtonsBlockedForTest}）—— 接错位置就是红，⛔ 不是跳过");
        if (!frameOk) return;

        // 牌堆命中区的前提：底板坐标必须落在 `HitMyDeckPile` 的方框里（不然下面两帧等于点空处）
        Vector3 deckW = driver.MyDeckPlateWorldPos;
        Tr(deckW != Vector3.zero && BattleDriver.HitMyDeckPile(deckW),
           "B0⁗（前提）我方牌堆底板的世界坐标落在 `HitMyDeckPile` 的命中区里"
         + $"（{deckW}）—— 守 `MyDeckX01/MyDeckY01` 与底板实际位置没跑偏");

        // 观察起点：窗必须是关着的（否则 `TickMultiCards` 会把整帧吞掉、松手沿根本到不了）
        if (mcd.Visible) mcd.Hide();
        Tr(!driver.MultiCardsVisibleForTest, "B0⁵（前提）多卡摊开窗起点是关着的");

        bool? puWas = BattleDriver.PointerOverUiForTest;
        Vector3? pwWas = BattleDriver.PointerWorldForTest;
        bool? phWas = BattleDriver.PointerHeldForTest;
        // 中性化选中状态：让「松手那一帧」不会落进「④ 选目标」那支（它会走 `CancelTargetChoosingByInvalidClick`）。
        // ⚠️ 跑完之后不还原（没有读口）；接在「本段之后不依赖上一次选中」的位置。
        driver.SetCommandForTest(AttackKind.None, -1);
        driver.SetCardHoldForTest(null);      // 上一节若把它钉死过，这里还原成看真指针
        try
        {
            BattleDriver.PointerOverUiForTest = false;
            BattleDriver.PointerWorldForTest = deckW;
            BattleDriver.PointerHeldForTest = false;
            driver.TickFrameForTest();                                  // 第 1 帧：把 `_held` 清到「没按」

            BattleDriver.PointerHeldForTest = true;
            driver.TickFrameForTest();                                  // 第 2 帧：出**按下**沿
            Tr(driver.CardHoldPeek,
               "B1 ★ **按住我方牌堆 ⇒ 牌库数照亮**（原版 `CardScript__OnTouchDown.c:34-59` `case 0` → "
             + "`BattleManager.ShowDeckSize(isPlayer)`；我们的等价物 = `CardHoldPeek` 读 `HitMyDeckPile`）"
             + "｜🧨 把 `CardHoldPeek` 里那句 `HitMyDeckPile(WorldPointer())` 改成恒 false ⇒ 本条红"
             + "（原版那条「按牌库看张数」就跟着没了）");

            BattleDriver.PointerHeldForTest = false;
            driver.TickFrameForTest();                                  // 第 3 帧：出**松手**沿 ← 旧代码在这里 `ShowMyDeck(true)`
            Tr(!driver.MultiCardsVisibleForTest,
               "B2 ★★ **点我方牌堆 ⇒ 不再摊开牌库**（原版没有这条交互；用户 2026-10-29 裁定删除）"
             + $"｜🧨 把 `if (HitMyDeckPile(world)) {{ ShowMyDeck(true); return; }}` 加回 `Update` 的松手沿"
             + "（换个方法名 / 改成别的窗也一样）⇒ 本条红"
             + $"（实得窗可见 = {driver.MultiCardsVisibleForTest}）");
        }
        finally
        {
            BattleDriver.PointerOverUiForTest = puWas;
            BattleDriver.PointerWorldForTest = pwWas;
            BattleDriver.PointerHeldForTest = phWas;
            // 🔴 还原静态之后必须再用还原后的口径走一帧：`_held` 是**共享实例状态**，
            //   它只在 `PollInputEdges` 里被写 ⇒ 只还原静态不补一帧，下一节的第一帧会继承这里的边沿
            //   （坑的判据 → `BattleScene.cs` 里 `W24` / `D2_BattleScene11红诊断.md` §4）。
            driver.PollInputEdgesForTest();
            driver.ReleasedThisFrameForTest();     // 顺手耗掉可能补出来的松手沿，别漏给下一节
        }
    }
}
