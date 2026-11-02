// B117F11_Assert.cs — 执行代理 `F11` 的断言宿主（2026-11-03）
//
// 覆盖「战斗手感」这一批的五条（判据全部来自第一权威反编译，逐条写在每组头里）：
//   A. `B117-32` 手牌**悬停那一张瞬切**（原版 `CardScript__ShowCardInHand.c:108/135/152` 三句直写），
//      其余牌仍走 0.2 s 重排（`CardScript__UpdateMinionInHandPosition.c`）。
//   B. `B117-33` **拾起那一刻**就响 `moveCardIntoBattle`（`CardScript__TryDraggingFromHand.c:89`
//      → `CardScript__StartMovingFromHand.c:43`）。
//   C. `B117-33` 起飞那一拍**不再重复响** `moveCardIntoBattle`（原版那一刻只有 `playMinionFromHand`）。
//   D. `B117-39` Trigger 的**前置 = 0**（原版那条协程只有 `WaitForSeconds(0.2)`，
//      `CardScript._ResolveTriggerTraitAnim_d__476.c:11-14`）。
//   E. `B117-37` 同一次**多目标伤害**的 N 条 `Hit` **并排**（`_ResolveMultiDamage_d__456__MoveNext.c:150-171`）。
//      E1 = 纯函数（`BattleDriver.HitBurstParallel`）· E2 = 真时间线（`RunWithDriver`，量 `_timelineTailAt`）。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117F11_Assert.Run -logFile -
//
// 判绿红：`grep "^B117F11"` 的**合计行**；逐条按**行首** `B117F11 ✓` / `B117F11 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求（给调度台挂进 `Editor/BattleScene.cs` 时看）：
//   · `Run()`：**零前提** —— 不读 `BattleDriver`、不读场景、不读存档；自带临时根 + 收尾 `DestroyImmediate`；
//     自己把 `CardTween.Mode` 拨成 `Manual`、`LinkEnabled` 拨成 `false`，退出**原样还原**。
//   · `RunWithDriver(driver)`：要一台**开局完的** `BattleDriver`（它往 `Ctx` 里塞 3 条合成 `Hit` 再读时间线尾）
//     ⇒ 必须接在「卸 driver 之前」那一行、且**排在所有依赖棋盘状态的断言之后**（它会改动受击方的血量显示账）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B117F11_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117F11 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117F11 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }

    static void Step(float dt) { CardTween.Advance(dt); }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            // 批处理没有帧循环 ⇒ 只有 `CardTween.Advance(dt)` 推得动补间（同 `HandAssertE8` / `BattleScene`）。
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;

            // ⚠️ 每组各包一层：一组炸掉**不许**把后面几组一起拖没（否则报告里只剩一条异常、别的全是「没验」）。
            Group("A·悬停瞬切", A_HoverSnap);
            Group("B·拾牌音效", B_PickupCue);
            Group("C·起飞不双响", C_TakeoffCue);
            Group("D·Trigger 前置", D_TriggerLead);
            Group("E1·多目标判定", E1_HitBurstRules);
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            CardTween.Mode = keepMode;
            CardTween.LinkEnabled = keepLink;
        }
        Debug.Log("B117F11 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    /// <summary>跑一组并把异常收成一条失败（⛔ 不让一组的崩溃吞掉后面各组）。</summary>
    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ==================================================================
    //  A · 悬停那一张**瞬切**、其余牌仍走 0.2 s 重排
    // ==================================================================
    //  判据（亲读）：
    //   · `d:/2/tools/decomp_full/CardScript__ShowCardInHand.c:108/135/152` —— 被悬停（抬起）的那一张是
    //     `Transform.set_position` / `set_rotation` / `set_localScale` **三句直写**：没有补间、没有时长；
    //   · `CardScript__UpdateMinionInHandPosition.c` —— **其余牌**是 `DOMove`/`DOLocalRotate`/`DOScale`
    //     三条单补间，时长 `timeToPositionCard` = 0.2 s（`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c`）。
    //  🔑 灭自证：把 `HandLayout.Place` 的 `snap` 去掉（悬停那张也塞进补间）⇒ 第 ① 条**当场红**
    //     （补间起点 = 它原来的静止位，抬不起来）。
    static void A_HoverSnap()
    {
        var root = new GameObject("B117F11_A_hover");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            for (int i = 0; i < 4; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "B117F11_H" + i, CardFace.Hand));

            // ① 先摆到「没人悬停」的静止态（`animateRelayout = false` ⇒ 全 snap，位姿当场精确）
            layout.animateRelayout = false;
            layout.Refresh(made, -1, -1);
            Vector3 rest1 = made[1].transform.localPosition;

            // ② 生产档（带重排补间）+ **悬停第 0 张** + 0 号格留一个洞
            //    （洞让**其余牌**的目标位真的变了 —— 否则「它们没动」是同义反复，验不出补间还在）
            layout.animateRelayout = true;
            layout.Refresh(made, 0, 0);

            // ③ 悬停那张：**同一次调用内**就该到位（洞在 0 ⇒ 它排到 slot 1）
            Vector3 want = layout.SlotPosition(1, 5);
            want.y += layout.hoverLift;
            want.z += -layout.frontZ;
            Vector3 got = made[0].transform.localPosition;
            Check(Vector3.Distance(got, want) < 1e-3f,
                  $"★ B117-32：**悬停那张当场落到抬起位**（要 {want}，实得 {got}）"
                + "｜判据 `CardScript__ShowCardInHand.c:108/135/152` 的三句直写"
                + "｜🧨 把它也塞进 0.2 s 补间 ⇒ 这一刻它还在它**原来的静止位**上 ⇒ 红");

            float wantScale = layout.cardScale * LayoutSpace.Scale * layout.hoverScale;
            CheckClose(made[0].BaseScale, wantScale, 5e-3f,
                       "★ B117-32：悬停那张的**缩放**也当场到位（原版第三句 `set_localScale`）");

            // ④ 其余牌**这一帧还没动**（它们的目标位变了，但补间还没跑）
            Vector3 now1 = made[1].transform.localPosition;
            Check(Vector3.Distance(now1, rest1) < 1e-4f,
                  $"★ B117-32：**其余牌仍按 0.2 s 走补间**（这一刻停在旧位 {rest1}，实得 {now1}）"
                + "｜🧨 把整趟都改成瞬切 ⇒ 它当场跳到新位 ⇒ 红");

            // ⑤ 推半步 ⇒ 它真的动了（证明第 ④ 条不是「什么都没变」的空验）
            Step(CardTween.RelayoutDuration * 0.5f);
            float moved = Vector3.Distance(made[1].transform.localPosition, rest1);
            Check(moved > 1e-3f,
                  $"★ B117-32：推 0.1 s 后其余牌**真的在补间里**（离开旧位 {moved:F4} 世界单位）"
                + " —— 与第 ④ 条合起来 = 「悬停瞬切、其余补间」两半都在");
            Step(CardTween.RelayoutDuration);
        }
        finally
        {
            foreach (var c in made) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  B · 拾起那一刻就响 `moveCardIntoBattle`
    // ==================================================================
    //  判据：`CardScript__TryDraggingFromHand.c:89`（**起拖**那一刻调 `StartMovingFromHand`）
    //        → `CardScript__StartMovingFromHand.c:43`（进方法第一句就 `SoundManager.Play2D(MoveCardIntoBattle)`）。
    //  🧨 灭自证：删掉 `CardInteraction.PickUp` 里那句 `BattleSfx.Play(MoveCardIntoBattle)` ⇒ 本组红。
    static void B_PickupCue()
    {
        var root = new GameObject("B117F11_B_pickup");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            for (int i = 0; i < 2; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "B117F11_P" + i, CardFace.Hand));
            it.SetCards(made, false);
            Check(made[0] != null && it.hand != null, "（前提）拾牌夹具建起来了（手牌 2 张 + `HandLayout` + `CardInteraction`）");

            BattleSfx.ResetCounters();
            it.PickUpForTest(made[0], made[0].transform.position);
            Check(it.IsDragging, "（前提）`PickUp` 真的进了拖拽态");
            Check(BattleSfx.Played == 1 && BattleSfx.LastField == BattleSfx.MoveCardIntoBattle,
                  $"★ B117-33：**拾起那一刻**响 `moveCardIntoBattle`（实得 {BattleSfx.Played} 次，最后一条 = {BattleSfx.LastField ?? "（无）"}）"
                + "｜判据 `CardScript__TryDraggingFromHand.c:89` → `CardScript__StartMovingFromHand.c:43`"
                + "｜🧨 把这一句删掉 / 挪回起飞那一拍 ⇒ 0 次 ⇒ 红");
        }
        finally
        {
            foreach (var c in made) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  C · 起飞那一拍**不再重复** `moveCardIntoBattle`
    // ==================================================================
    //  判据：玩家手动拖拽那条路，`moveCardIntoBattle` 已经在**拾起**时响过（B 组）；
    //        起飞那一刻原版只有 `playMinionFromHand`（`_ResolvePlayCardFromHand…:397`）。
    //  🧨 灭自证：把 `OnPlayerDeployFlightStart` 的 `moveCue:false` 改回默认 ⇒ 本组第 ① 条红（2 次）。
    static void C_TakeoffCue()
    {
        var root = new GameObject("B117F11_C_takeoff");
        var drv = root.AddComponent<BattleDriver>();
        try
        {
            // ① 玩家手动拖拽那条路（`moveCue:false`）
            BattleSfx.ResetCounters();
            BattleSfx.ResetQueueForTest();     // ⚠️ `ResetCounters` **不清 `Log`** ⇒ 要断「队列里 0 条」必须两个都调
            drv.PlayDeployTakeoffCuesForTest(false);
            Check(BattleSfx.Played == 1 && BattleSfx.LastField == BattleSfx.PlayMinionFromHand,
                  $"★ B117-33：**起飞只响 `playMinionFromHand` 一条**（实得 {BattleSfx.Played} 次，最后一条 = {BattleSfx.LastField ?? "（无）"}）"
                + "｜🧨 改回两条 ⇒ 2 次 ⇒ 红（与 B 组一起 = 「拾起响一次、起飞不再响」）");
            // ⚠️ 「这一拍没有 `moveCardIntoBattle`」由上面那一条的 `Played == 1` 直接钉住
            //    （若它还在这里响，`Played` 会是 2、`LastField` 会是 `moveCardIntoBattle`）——
            //    ⛔ 别用 `BattleSfx.Log` 判：那张表只在 `DrainOne` 时填，批处理里一次都不会被填（空表 ≠ 没响）。
            Check(BattleSfx.Played == 1, "★ B117-33：这一拍**只响了一条** cue（`moveCardIntoBattle` 没有重复出现）");

            // ② 没有「拾起」的那几路（AI / 程序化 / 效果召唤）保持原样：两条都响
            BattleSfx.ResetCounters();
            drv.PlayDeployTakeoffCuesForTest(true);
            Check(BattleSfx.Played == 2 && BattleSfx.LastField == BattleSfx.PlayMinionFromHand,
                  $"★ B117-33（反向·防一刀切）：`moveCue:true` 那条路仍是两条（实得 {BattleSfx.Played} 次）");
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ==================================================================
    //  D · Trigger 的前置 = 0（只有 0.2 的动画拍）
    // ==================================================================
    //  判据：`CardScript._ResolveTriggerTraitAnim_d__476.c:11-14` —— 整条协程只有
    //        `yield WaitForSeconds(twoFractDelay = 0.2)`，**进来不等任何东西**。
    //        原来 `DelayBetween(next: Trigger)` 返回 `TriggerHold` 0.3（`sec3FractionDelay` 那个**通用节拍**）
    //        ⇒ N 次触发白等 0.3N s。
    //  🧨 灭自证：把它改回 `TriggerHold` ⇒ 第 ② 条 1.5 s ⇒ 红。
    static void D_TriggerLead()
    {
        var hit = new BattleEvent { Kind = EvtKind.Hit, Player = 0, Slot = 4, CardId = "B117F11", Amount = 1 };
        var trig = new BattleEvent { Kind = EvtKind.Trigger, Player = 0, Slot = 4, CardId = "B117F11" };

        CheckClose(EventTiming.DelayBetween(hit, trig), 0f, 1e-6f,
                   "★ B117-39：`DelayBetween(next: Trigger)` = **0**（原版那条协程没有前置）");
        CheckClose(EventTiming.DurationOf(trig), EventTiming.TriggerLen, 1e-6f,
                   "★ B117-39（前提）：Trigger 自己仍占 `TriggerLen` 0.2 s（`twoFractDelay`）");

        float total = 0f;
        BattleEvent prev = hit;
        for (int k = 0; k < 3; k++) { total += EventTiming.DelayBetween(prev, trig) + EventTiming.DurationOf(trig); prev = trig; }
        CheckClose(total, 3f * EventTiming.TriggerLen, 1e-3f,
                   "★ B117-39：连续 3 次触发总时长 = 0.2×3 = 0.6 s（⛔ 不是 0.5×3 = 1.5）"
                 + "｜🧨 把前置改回 `TriggerHold` 0.3 ⇒ 1.5 s ⇒ 红");
    }

    // ==================================================================
    //  E1 · 「同一次多目标伤害」的判定规则（纯函数）
    // ==================================================================
    //  判据：`BattleManager._ResolveMultiDamage_d__456__MoveNext.c:150-171` —— 非末目标
    //        `StartCoroutine(ReceiveDamage)` **不 yield**，只等最后一个 ⇒ 同帧并行。
    static void E1_HitBurstRules()
    {
        BattleEvent H(int slot, bool vuln = false)
        {
            return new BattleEvent { Kind = EvtKind.Hit, Player = 1, Slot = slot, CardId = "B117F11", Amount = 1, IsVulnerableSecondHit = vuln };
        }
        BattleEvent A(int slot)
        {
            return new BattleEvent { Kind = EvtKind.Attack, Player = 1, Slot = slot, CardId = "B117F11" };
        }
        BattleEvent T(int slot)
        {
            return new BattleEvent { Kind = EvtKind.Trigger, Player = 1, Slot = slot, CardId = "B117F11" };
        }

        var aoe = new List<BattleEvent> { H(4), H(5), H(6) };
        Check(!BattleDriver.HitBurstParallel(aoe, 0), "（前提）多目标伤害的**第 1 条**自己起一组（不并排到谁身上）");
        Check(BattleDriver.HitBurstParallel(aoe, 1) && BattleDriver.HitBurstParallel(aoe, 2),
              "★ B117-37：第 2、3 个目标**并排**（判据 `_ResolveMultiDamage_d__456__MoveNext.c:150-171`）");

        var withTrig = new List<BattleEvent> { H(4), T(4), H(5) };
        Check(BattleDriver.HitBurstParallel(withTrig, 2),
              "★ B117-37：中间夹一条 `Trigger`（被打出来的反应）**仍算同一组**（原版那些是排进动作队列的）");

        var vuln = new List<BattleEvent> { H(4), H(4, true) };
        Check(!BattleDriver.HitBurstParallel(vuln, 1),
              "★ B117-37（防串拍）：**易伤的第二拍必须串行**（`EventTiming.HitHoldOf` 那 1.4 s 排在后面）"
            + "｜🧨 把它也并排 ⇒ 两拍同时开 ⇒ 红");

        var same = new List<BattleEvent> { H(4), H(4) };
        Check(!BattleDriver.HitBurstParallel(same, 1),
              "★ B117-37（防串拍）：**同一格**的连续两条 `Hit` 不并排（那不是多目标伤害）");

        var attack = new List<BattleEvent> { A(4), H(4), H(5) };
        Check(!BattleDriver.HitBurstParallel(attack, 1),
              "★ B117-37：**攻击命中**（紧跟 `Attack` 的那条 `Hit`）不并入多目标组");
    }

    // ==================================================================
    //  E2 · 真时间线：3 目标 AoE 的**总时长 ≈ 1.0 s**（不是 3.0）
    // ==================================================================
    //  🧨 灭自证：把 `PlaySignals` 里那句 `if (HitBurstParallel(...)) ... t = hitBurstAt;` 删掉
    //     ⇒ 3 条 `Hit` 各自 `+= DurationOf` ⇒ 尾巴落在 +3.0 ⇒ 红。
    //  ⚠️ 会往受击方塞 3 条**合成** `Hit`（只是表现层事件，引擎状态不动）⇒ 只许接在**最后**。
    public static void RunWithDriver(BattleDriver driver)
    {
        try { Group("E2·AoE 时长", () => E2_Body(driver)); }
        catch (System.Exception e) { Check(false, "E2 组**抛异常**：" + e.GetType().Name + ": " + e.Message); }
    }

    static void E2_Body(BattleDriver driver)
    {
        if (driver == null || driver.Ctx == null)
        {
            Check(false, "（前提）`RunWithDriver` 拿到一台活着、已开局过的 `BattleDriver` —— 没拿到 ⇒ E2 这一条**没验**");
            return;
        }

        int foe = 1 - driver.Ctx.Active;
        var slots = new List<int>();
        for (int s = 0; s < BoardSpec.Size && slots.Count < 3; s++)
            if (BoardSpec.IsDeployable(s) && driver.Ctx.Players[foe].Board[s] != null) slots.Add(s);
        if (slots.Count < 3)
        {
            Check(false, "（前提）敌方场上至少 3 个单位（实测 " + slots.Count + "）—— 不够 ⇒ E2 这一条**没验**（⛔ 别读成通过）");
            return;
        }

        driver.RefreshAll();       // 先把前面几组断言留下的残留信号倒干净（否则它们会混进下面**第一趟**那一批）

        // 🔑 用「**对照**」而不是绝对秒数：先量**1 个目标**那一批占多久，再量**3 个目标**那一批。
        //   原版两者应当**一样长**（非末目标不 yield）；改回逐条累加 ⇒ 3 个目标 ≈ 3 倍 ⇒ 红。
        //   ⚠️ 这样写免疫「这一批里混进别的事件（抬头 / 残留）」那类噪声 —— 两趟吃同一套底噪。
        float start1 = Mathf.Max(driver.PresentationClock, driver.TimelineTailAtForTest);
        EmitHit(driver, foe, slots[0]);
        driver.RefreshAll();
        float span1 = driver.TimelineTailAtForTest - start1;

        float start2 = Mathf.Max(driver.PresentationClock, driver.TimelineTailAtForTest);
        for (int k = 0; k < 3; k++) EmitHit(driver, foe, slots[k]);
        driver.RefreshAll();
        float span3 = driver.TimelineTailAtForTest - start2;

        bool baseOk = Mathf.Abs(span1 - EventTiming.HitWaitSeconds) <= 0.15f;
        Check(baseOk,
              $"（前提·对照）**单目标**那一批占 {span1:F2} s ≈ `HitWaitSeconds` {EventTiming.HitWaitSeconds:F2}"
            + " —— 不成立时下面那条比值**不用看**（先查这一段是不是被别的事件污染了）");
        if (!baseOk) { Check(driver.WaitingForBlockingAction, "（前提·闸）这一段仍锁着输入（`Frozen`）"); return; }
        Check(Mathf.Abs(span3 - span1) <= 0.15f,
              $"★ B117-37：**3 目标 AoE 的总时长 = 单目标的时长**（{span3:F2} s vs 对照 {span1:F2} s）"
            + "｜判据 `BattleManager._ResolveMultiDamage_d__456__MoveNext.c:150-171`（非末目标 `StartCoroutine` **不 yield**，只等最后一个）"
            + $"｜🧨 改回逐条累加 ⇒ {3f * EventTiming.HitWaitSeconds:F2} s ⇒ 红");
        Check(driver.WaitingForBlockingAction,
              "★ B117-37（闸）：这一段仍**锁着输入**（`Frozen` 跟着同一套 `t` 走，只是变短）"
            + " —— ⛔ 别为了让它放行把闸拆掉（那不叫修好）");
        return;
    }

    static void EmitHit(BattleDriver d, int player, int slot)
    {
        var u = d.Ctx.Players[player].Board[slot];
        d.Ctx.Emit(EvtKind.Hit, player, slot, u != null ? u.Name : "B117F11", null, null, 1);
    }

}
