// B118W18_Assert.cs — 执行写手 `B118-W18` 的断言宿主（2026-11-02）
//
// 覆盖「手牌可打出 = 绿光 / 拖拽中不该多出状态环 / 松手打出那一刻 = 淡蓝卡框光」这三刀。
// 判据正本 = `资料/完全复刻/判据/B118_J8_拖拽与可打出卡框光.md`（色值全部来自原版预制体直读）。
//
//   ① 手牌可打出：SDF `_Outline` = `cardPlayableColor` **#1C7101**（`OutlinePlayable`）。
//   ② **拖拽中**：`_Outline` **仍是 #1C7101** **且** 状态环 α = 0。
//      原版 `CardScript__StartMovingFromHand.c` 全文五句**没有** `ChangeState`；`CardScript__OnTouchDown.c`
//      里唯一那句 `ChangeState(2)` 在 `cardState == 2 (inPlay)` 那一支，`inHand` 那支是 `if (isPlayer) return;`
//      ⇒ 被拖的手牌**从头没有状态环**（绿环只点棋盘上的候选单位 `ShowPotentialTargets`）。
//   ③ 松手打出（合法区）：`_Outline` → `cardSelectedColor` **#003765**；`rarity == legendary` → HDR 橙 #FF6203。
//      原版 `CardScript__MoveIntoFieldFromHand.c:42` → `BattleCardUI__ChangeHighlightToSelectColor.c:6-24`。
//   ④ 战术卡走同一条（`MoveIntoFieldFromHand` 不分卡类型）；`AbortDrag` 之后回到 ① 的绿。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`；本文件**未接线**，调度台自行挂进宿主）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B118W18_Assert.Run -logFile -
//
// 判绿红：`grep "^B118W18"` 的**合计行**；逐条按**行首** `B118W18 ✓` / `B118W18 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主前提：**零前提** —— 不读 `BattleDriver` / 场景 / 存档；自带临时根 + `DestroyImmediate` 收尾；
//   自己把 `CardTween.Mode` 拨成 `Manual`、退出**原样还原**（批处理没有帧循环）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B118W18_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    // 🔴 **期望值一律写原版预制体字面量**（`CardPrefab` 的 `BattleCardUI` /
    //    `CardHighlight` 两具 MB 直读；出处 = `判据/B118_J8` §1）—— ⛔ 不拿
    //    `CardHighlight.Outline*` 自己当期望（那是「实现证明实现」）。
    //    `CardHighlight.Outline*` 只在下面 `CheckConstantMirrorsPrefab()` 里被**钉一次**。
    static readonly Color PrefabPlayable  = new Color(0.10980392f, 0.44313726f, 0.00392157f, 1f);  // #1C7101 cardPlayableColor
    static readonly Color PrefabSelected  = new Color(0.00000000f, 0.21568628f, 0.39607844f, 1f);  // #003765 cardSelectedColor
    static readonly Color PrefabLegendary = new Color(1.30411887f, 0.38584328f, 0.01365572f, 1f);  // #FF6203 cardSelectedColor（legendary，HDR r>1）

    /// <summary>`CardHighlight` 的三条常量与**原版预制体字面量**逐个对上（给人改了就红）。</summary>
    static void CheckConstantMirrorsPrefab()
    {
        Check(Near(CardHighlight.OutlinePlayable, PrefabPlayable),
              "C0a `CardHighlight.OutlinePlayable` == 原版 #1C7101（实得 " + S(CardHighlight.OutlinePlayable) + "）");
        Check(Near(CardHighlight.OutlineSelected, PrefabSelected),
              "C0b `CardHighlight.OutlineSelected` == 原版 #003765（实得 " + S(CardHighlight.OutlineSelected) + "）");
        Check(Near(CardHighlight.OutlineLegendary, PrefabLegendary),
              "C0c `CardHighlight.OutlineLegendary` == 原版 #FF6203（实得 " + S(CardHighlight.OutlineLegendary) + "）");
    }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B118W18 ✓ " + msg); }
        else { _fail++; Debug.LogError("B118W18 ✗ " + msg); }
    }

    static bool Near(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) <= 1e-4f && Mathf.Abs(a.g - b.g) <= 1e-4f
            && Mathf.Abs(a.b - b.b) <= 1e-4f && Mathf.Abs(a.a - b.a) <= 1e-4f;
    }

    static string S(Color c)
    {
        return "(" + c.r.ToString("F4") + "," + c.g.ToString("F4") + "," + c.b.ToString("F4")
             + ",a=" + c.a.ToString("F4") + ")";
    }

    /// <summary>跑一组并把异常收成一条失败（⛔ 不让一组炸掉后面各组）。</summary>
    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    /// <summary>建一具最小夹具：手牌布局 + 一块棋盘 + 交互器。
    /// ⚠️ `CanDropAtSlot` 是**替身**（落点合法性的真判据在引擎 `RuleCore.CanPlayCard`）⇒ 本夹具一律放行，
    /// 这样验的才是「松手之后那一句描边」而不是引擎判据。</summary>
    static CardInteraction MakeIt(out GameObject root, out BoardLayout board, out HandLayout layout)
    {
        root = new GameObject("B118W18_root");
        layout = root.AddComponent<HandLayout>();
        board = root.AddComponent<BoardLayout>();
        var it = root.AddComponent<CardInteraction>();
        it.hand = layout;
        it.board = board;
        it.SetRightButtonHeldForTest(false);      // 批处理里没有鼠标设备 ⇒ 把闸钉成「没按右键」
        it.SetBattleStateBlockedForTest(false);   // 本组只验表现 ⇒ 状态闸全放行
        it.CanDropAtSlot = (slot, card) => true;
        layout.animateRelayout = false;           // 夹具：位姿当场精确（不推补间也能读坐标）
        board.lineY = 0.30f;                      // 与手牌限位线（0.2778）分开，免得 UpdateDrag 走让位那一段
        return it;
    }

    static CardView Make(CardInteraction it, GameObject root, string name, bool unit, string rarity)
    {
        var d = CardData.Placeholder(1);
        // 🔴 id 用夹具自己的名字：`CardInteraction.TacticTargetSide` 会拿 `artId`/`id` 回查卡表，
        //    若凑巧撞上一张真实卡，落点就会被那张卡的「目标侧」带着走（夹具前提跟着变）⇒ 这里钉死「查不到」。
        d.id = name; d.artId = name;
        d.isUnit = unit;
        d.type = unit ? "unit" : "tactic";
        d.rarity = rarity;
        return CardView.Create(root.transform, d, name, CardFace.Hand);
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;

            CheckConstantMirrorsPrefab();
            Group("A/B·拖拽中不挂环 + 打出写 select 色", AB_DragThenRelease);
            Group("C·传说档走 HDR 橙", C_LegendaryHdr);
            Group("D·战术卡同样走 select 色", D_Tactic);
            Group("E·AbortDrag 复原成可打出绿", E_AbortRestores);
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
        Debug.Log("B118W18 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  A/B · 拖到合法格 ⇒ 描边仍绿 + 环 α=0；松手 ⇒ 描边换 select 蓝
    // ==================================================================
    //  🔑 **灭自证（两条，缺一不可）**：
    //    ② 「描边仍绿」与「环 α=0」**写成一条**（`&&`）—— 只断环的话，把环改回绿仍绿；
    //       只断描边的话，两处一起改回蓝仍绿。
    //    ③ 再加「② 的描边值 **≠** ③（打出后）的描边值」—— 结构上不可能同时满足 ⇒ 挡「两边一起改」。
    static void AB_DragThenRelease()
    {
        GameObject root; BoardLayout board; HandLayout layout;
        var it = MakeIt(out root, out board, out layout);
        CardView card = null;
        try
        {
            card = Make(it, root, "B118W18_A", unit: true, rarity: "common");
            it.SetCards(new List<CardView> { card }, false);

            // ① 手牌可打出那一档（生产里由 `BattleDriver.RefreshHandPlayable` 写，夹具手工摆）
            card.SetOutline(CardHighlight.OutlineOf(null, canBePlayed: true));
            Check(Near(card.OutlineTargetForTest, PrefabPlayable),
                  "① 手牌·可打出 ⇒ 描边 = `cardPlayableColor` **#1C7101**（实得 " + S(card.OutlineTargetForTest) + "）"
                + "｜判据 `BattleCardUI__ChangeToHighlightColor.c:16` 末支兜底");

            Vector3 home = card.transform.position;
            it.SimulateHover(home);
            it.SimulatePress(home);
            Check(it.IsDragging && it.Dragging == card,
                  "（前提）抓起来了（否则下面整组是空验；实得 Dragging = "
                + (it.Dragging != null ? it.Dragging.name : "<null>") + "）");
            if (!it.IsDragging) return;

            Vector3 drop = board.SlotPosition(4);      // 与 `TryResolveSlot` 同一把尺子 ⇒ 必中一格
            // 🔴 2026-11-03（`B124-W22` / `B120-W16`）：越过两道门的那一帧**就已经提交**（「还按着就 commit」）
            //   ⇒ 「拖拽中」这个窗口只能在**手牌带里**取（指针还在 `HandLimitY` 以下 ⇒ 闸②不成立 ⇒ 不提交）。
            Vector3 hover = home + new Vector3(0.60f, 0.05f, 0f);
            it.SimulateDrag(hover, 1f / 60f);
            Check(it.Dragging == card, "（前提）拖拽态还在（这一帧指针仍在手牌带 ⇒ 出牌闸不成立、没提交）");

            // ---- ② 拖拽中（还没越过出牌闸）----
            var o2 = card.OutlineTargetForTest;
            var r2 = card.RimTargetForTest;
            bool outlineStillGreen = Near(o2, PrefabPlayable);
            bool rimOff = r2.a <= 1e-4f;
            Check(outlineStillGreen && rimOff,
                  "★② 拖拽中（指针在手牌带、落点已解析到合法格）：描边**仍是 #1C7101**（实得 " + S(o2) + "）**且**状态环 α = 0（实得 "
                + S(r2) + "）—— 原版被拖的手牌从头没有状态环（`StartMovingFromHand` 无 `ChangeState`）"
                + "｜🧨 把 `UpdateDrag` 里那句 `SetHighlight(ValidTarget)` 写回来 ⇒ 环 α=1 ⇒ 红；"
                + "🧨 只断「环不绿」时把描边改蓝也会绿 ⇒ 所以两个条件写成一条");

            // ---- ③ 越过两道门 ⇒ **还按着就提交**（原版 `UpdateDrag` 里就 Release，⛔ 不等松手）----
            it.SimulateDrag(drop, 1f / 60f);
            it.SimulateRelease(drop);
            Check(it.Dragging == null, "（前提）越过出牌闸那一帧就提交、拖拽态已空（`SimulateRelease` 只是空操作）");
            var o3 = card.OutlineTargetForTest;
            Check(Near(o3, PrefabSelected),
                  "★③ 松手打出 ⇒ 描边 = `cardSelectedColor` **#003765**（实得 " + S(o3) + "）"
                + "｜判据 `CardScript__MoveIntoFieldFromHand.c:42` → `ChangeHighlightToSelectColor.c:6-24`"
                + "｜🧨 删掉 `Release` 那句 `SetOutline(OutlineSelectOf(...))` ⇒ 停在绿 ⇒ 红");
            Check(!Near(o2, o3),
                  "★③b 灭自证：**② 的描边值 ≠ ③ 的**（" + S(o2) + " vs " + S(o3) + "）"
                + " —— 结构上不可能同时满足 ⇒ 挡「②③ 两边一起改回同一个颜色」");
            Check(it.DeployingViewForTest == card,
                  "★③c 打出链闸已上膛：`DeployingViewForTest` == 这一张（`RefreshHandPlayable` 靠它跳过，"
                + "否则飞行那 0.65 s 里一次 `SyncHand` 就把蓝光刷回绿）");
        }
        finally
        {
            if (card != null) Object.DestroyImmediate(card.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  C · 传说档：select 色是 **HDR 橙 #FF6203**（r = 1.3041 > 1）
    // ==================================================================
    //  ⚠️ 必须**逐分量比 `Color`**、⛔ 不用 `Color32`（会被截断到 255 ⇒ 这条差异看不出）
    //      —— 原版那个值在预制体里就是大于 1 的 HDR。
    static void C_LegendaryHdr()
    {
        GameObject root; BoardLayout board; HandLayout layout;
        var it = MakeIt(out root, out board, out layout);
        CardView card = null;
        try
        {
            card = Make(it, root, "B118W18_C", unit: true, rarity: "legendary");
            it.SetCards(new List<CardView> { card }, false);

            Vector3 home = card.transform.position;
            it.SimulateHover(home);
            it.SimulatePress(home);
            Check(it.IsDragging, "（前提）传说法卡抓得起来");
            if (!it.IsDragging) return;

            Vector3 drop = board.SlotPosition(4);
            it.SimulateDrag(drop, 1f / 60f);
            it.SimulateRelease(drop);

            var o = card.OutlineTargetForTest;
            Check(Near(o, PrefabLegendary),
                  "★C1 传说档 ⇒ 描边 = **#FF6203**（实得 " + S(o) + "；期望 " + S(CardHighlight.OutlineLegendary) + "）");
            Check(o.r > 1.0f,
                  "★C2 传说的红分量 **> 1**（HDR 原值 1.3041 **没有被夹到 1**；实得 " + o.r.ToString("F4") + "）"
                + "｜🧨 期望值改用 `Color32` / `Color.HSVToRGB` 一类会截断的取法 ⇒ 这条红");
            Check(!Near(o, PrefabSelected),
                  "★C3 传说档 **≠** 非传说档（否则「按稀有度分色」这一跳压根没生效）");
        }
        finally
        {
            if (card != null) Object.DestroyImmediate(card.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  D · 战术卡打出也走同一条（原版 `MoveIntoFieldFromHand` 不分卡类型）
    // ==================================================================
    static void D_Tactic()
    {
        GameObject root; BoardLayout board; HandLayout layout;
        var it = MakeIt(out root, out board, out layout);
        CardView card = null;
        try
        {
            card = Make(it, root, "B118W18_D", unit: false, rarity: "common");
            it.SetCards(new List<CardView> { card }, false);
            // ⚠️ 这张是合成卡（卡表里查不到）⇒ `TacticTargetSide` 会打一行 warning、并退回旧口径
            //    （两块棋盘都试）。本夹具只有一块棋盘 ⇒ 落点判据照样必中，不影响下面这一条。

            Vector3 home = card.transform.position;
            it.SimulateHover(home);
            it.SimulatePress(home);
            Check(it.IsDragging, "（前提）战术卡抓得起来");
            if (!it.IsDragging) return;

            Vector3 drop = board.SlotPosition(4);
            it.SimulateDrag(drop, 1f / 60f);
            // 战术卡「落格」这一档在本夹具里由替身 `CanDropAtSlot` 放行（真判据在引擎）
            it.SimulateRelease(drop);
            var o = card.OutlineTargetForTest;
            Check(Near(o, PrefabSelected),
                  "★D 战术卡松手打出 ⇒ 同样写 select 蓝（实得 " + S(o) + "）"
                + "｜判据 `MoveIntoFieldFromHand.c` 通篇不按卡类型分流（那一段的 `cardType` 判断在它之后）");
        }
        finally
        {
            if (card != null) Object.DestroyImmediate(card.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  E · `AbortDrag`（拖到一半被战斗状态闸收回）⇒ 描边回到「可打出绿」
    // ==================================================================
    //  为什么要有这一条：`Release` 的合法支会把描边写成 select 蓝，而「打出 → 被拒 / 取消」
    //  之后牌**回到手牌** ⇒ 若不复原，玩家手里会留一张蓝框牌（原版手牌里恒为绿）。
    //  夹具先把描边**人为摆成蓝**（= 模拟「已经进过打出链」），再让闸把它收回来。
    static void E_AbortRestores()
    {
        GameObject root; BoardLayout board; HandLayout layout;
        var it = MakeIt(out root, out board, out layout);
        CardView card = null;
        try
        {
            card = Make(it, root, "B118W18_E", unit: true, rarity: "common");
            it.SetCards(new List<CardView> { card }, false);

            Vector3 home = card.transform.position;
            it.SimulateHover(home);
            it.SimulatePress(home);
            Check(it.IsDragging, "（前提）抓起来了");
            if (!it.IsDragging) return;
            // 🔴 2026-11-03（`B124-W22`）：留在**手牌带**里拖 —— 拖到棋盘上会在那一帧「还按着就提交」，
            //   牌直接出去、`AbortDrag` 那条路根本走不到（实测 `battle_b124b.log:1717411`）。
            it.SimulateDrag(home + new Vector3(0.60f, 0.05f, 0f), 1f / 60f);

            // 前提：先把描边摆成「写出去了」那一档（否则「回到绿」是同义反复）
            card.SetOutline(CardHighlight.OutlineSelectOf(card.Data.rarity));
            Check(Near(card.OutlineTargetForTest, PrefabSelected),
                  "（灭自证锚）先把描边摆成 select 蓝（实得 " + S(card.OutlineTargetForTest) + "）");

            // 闸落下 ⇒ `BlockedThisFrame` 里那句 `AbortDrag(...)`
            it.SetBattleStateBlockedForTest(true);
            it.SimulateTickForTest(home, false, false);
            Check(it.Dragging == null, "（前提）闸落下 ⇒ 这一拖被收回（`Dragging == null`）");

            var o = card.OutlineTargetForTest;
            Check(Near(o, PrefabPlayable),
                  "★E 被收回之后 ⇒ 描边回到 **#1C7101**（实得 " + S(o) + "）"
                + "｜🧨 删掉 `AbortDrag` 那句 `SetOutline(...)` ⇒ 停在 #003765 ⇒ 红");
            it.SetBattleStateBlockedForTest(null);
        }
        finally
        {
            if (card != null) Object.DestroyImmediate(card.gameObject);
            Object.DestroyImmediate(root);
        }
    }
}
