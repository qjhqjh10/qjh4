// B121_WB_Assert.cs — 执行写手 `B121-WB`（2026-11-03）· 拖牌 / 帧率 两条（落点容差 / 拖太高放弃 / -1 帧率档）
//
// ============================ 它盯的是什么 ============================
//  A 段（`B120-Q04c3`）：`BoardLayout` 的 **2D 兜底落点档**判据 = 候选集 + 2D 欧氏 + **无容差**
//        —— 两个自定容差字段 `snapTolerance` / `snapToleranceY` **已删**，且删掉之后行为按原版走。
//        判据：`cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:321-390`（= `GetClosestAvailableSlot`：
//        候选 = 每侧「最内空格 + 内圈已占格」、2D 欧氏、`bestIdx < 0` 只在「一个候选都没有」时出现）·
//        互验 `decomp_full/MinionManager__GetClosestAvailableSlot.c:35-145`。
//  B 段（`B120-Q04c6`）：`CardInteraction.MaxPlayHeightWorld` = `VarsGlobal.maxHeightOfUnplayableCard`
//        = **25.0 原版世界单位** × 桥 0.13736 = **3.4340 我们的单位 ≈ 370.9 px**（`工具/read_varsglobal.py`
//        2026-11-03 现读 25.0；`VarsGlobal__.cctor.c` 的 3.0 是 ctor 默认值、运行时被序列化覆盖），
//        以及「**拖太高 + 这一手打不出去 ⇒ 收回手牌**」这条闸（原版 `case 2`）。
//  C 段（`B120-Q07`）：`Card3DAnimationController` 的默认帧率档 = 原版 **−1**（调用点钳制已删）——
//        剩不到 1 s 的 clip **第一次 `Update` 就当帧收尾**。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//  🧨 A0：把 `snapTolerance` / `snapToleranceY` 任何一个字段加回来 ⇒ A0 红（反射按名字找得到）。
//  🧨 A2/A3：把容差（任何等价阈值：y 带、`|dx| <= 半格`）写回 2D 档 ⇒ 那两个「远处点」解不出格 ⇒ 红。
//  🧨 A4：候选集写回「九格全算」⇒ `x = NxOf(4)`（督军位正中）会解回 **4** ⇒ A4 红。
//  🧨 A5：把 `_sideLeft/_sideRight` 传错侧、或把「数不出来(-1)」当「满」⇒ A5 红。
//  🧨 B1：把常量改成 ctor 默认值 `3.0` 世界单位（= 0.4120 我们的单位）⇒ B1 红。
//  🧨 B2：把「拖太高就放弃」整条删掉 ⇒ 打不出去的那张牌会**一直留在手里** ⇒ B2 红。
//  🧨 B3：把闸写成「拖太高就一律收回」（不看能不能打）⇒ 能打的那一张也会被收回 ⇒ B3 红。
//  🧨 B4：把高度门槛去掉（或写成 `>=` 反了）⇒ 抬高不够也收回 ⇒ B4 红。
//  🧨 C1/C2：把 `if (fps <= 0) fps = 60;` 加回调用点 ⇒ 0.6 s 的 clip 在剩 0.59 s 时**不会**收尾 ⇒ C1 红。
//
// ============================ 覆盖边界（如实记）============================
//  · A 段只跑 **2D 兜底档**（没有 `boardCam` 的 `BoardLayout`）—— 3D 产品档的判据在 `ArenaSlots.TryResolveSlot`
//    / `PickNearest`，由 `B120_W21_Assert` 盯着，本件不重复。
//  · B 段验的是**时机 / 结局**（是否收回手牌），不验「卡停不停跟手」那一支 —— 原版那一支**不可达**
//    （出牌闸与放弃闸问的是同一个 `CanPlayCard` 谓词、只差提示开关 ⇒ 能打的话早就提交了）。
//  · C 段依赖运行时的 `Application.targetFrameRate`：若它 > 0（不是原版默认档），C1 会退化成自洽读数
//    （C0 会把这件事**明确报出来**，⛔ 不静默跳过）。
//  · **本件还没接线**：入口 `Run()`（无参、零前提）。建议接在 `Editor/BattleScene.cs` 的聚合行上，
//    `B120_W16_Assert.Run();` 之后（同一族：拖牌 / 落点）。
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CardPresentation;

public static class B121_WB_Assert
{
    static int _pass, _fail;
    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B121_WB ✓ " + msg); }
        else { _fail++; Debug.LogError("B121_WB ✗ " + msg); }
    }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;                       // 批处理没有帧循环 ⇒ 补间拨 Manual，退出时**原样还原**
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            Group("A·落点容差已删 / 2D 档按原版", A_ResolveSlotNoTolerance);
            Group("B·拖太高就放弃", B_GiveUpTooHigh);
            Group("C·帧率默认档 -1", C_FrameRateMinusOne);
        }
        catch (System.Exception e)
        {
            Check(false, "整份抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
        finally { CardTween.Mode = keepMode; }
        Debug.Log("B121_WB 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }

    // ==================================================================
    //  A · 落点：两个自定容差已删，2D 档 = 候选集 + 2D 欧氏 + 无容差
    // ==================================================================
    static void A_ResolveSlotNoTolerance()
    {
        var root = new GameObject("B121WB_board");
        try
        {
            var board = root.AddComponent<BoardLayout>();      // `use3D = false` ⇒ 走 2D 兜底档

            // ---- A0：两个字段**不存在**（删干净了；⛔ 不是「值为 0」）----
            var t = typeof(BoardLayout);
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Check(t.GetField("snapTolerance", F) == null && t.GetField("snapToleranceY", F) == null,
                  "A0 ★ `BoardLayout` 上**不再有** `snapTolerance` / `snapToleranceY` 两个字段"
                + "｜🧨 把任何一个加回来 ⇒ 红（判据：原版 `GetClosestAvailableSlot` 没有任何容差）");

            float nx0 = board.NxOf(0), nx3 = board.NxOf(3), nx4 = board.NxOf(4), nx5 = board.NxOf(5);
            Check(Mathf.Abs(nx5 - nx3 - 2f * board.spacing) < 1e-5f,
                  "（前提）槽 3 / 5 关于督军位对称（" + nx3.ToString("F4") + " / " + nx4.ToString("F4") + " / " + nx5.ToString("F4") + "）");

            // ---- A1：两侧皆空（−1 = 数不出来 ⇒ 该侧按皆空）⇒ 候选只有 {3, 5} ----
            int s;
            bool ok3 = board.TryResolveSlot(At(nx3, 0.42f), out s, -1, -1);
            Check(ok3 && s == 3, "A1a 两侧皆空 ⇒ 指针在**槽 3** 上解回 3（实得 ok=" + ok3 + "、槽 " + s + "）");
            bool ok5 = board.TryResolveSlot(At(nx5, 0.42f), out s, -1, -1);
            Check(ok5 && s == 5, "A1b 指针在**槽 5** 上解回 5（实得 ok=" + ok5 + "、槽 " + s + "）");

            // ---- A2：横向**无容差**（旧 `snapTolerance` = 0.036 会把这一点判成「没落在格上」）----
            float farX = nx5 + 5f * board.spacing;                      // 离最近候选 0.389 归一化 ≫ 旧容差 0.036
            bool farOk = board.TryResolveSlot(At(farX, 0.42f), out s, -1, -1);
            Check(farOk && s == 5,
                  "A2 ★★ **横向无容差**：指针甩到 x=" + farX.ToString("F3") + "（离槽 5 有 " + (farX - nx5).ToString("F3")
                + " 归一化，旧 `snapTolerance`=0.036 会判「没落上」）⇒ 仍解回**最近候选**槽 5"
                + "｜🧨 把容差写回来 ⇒ 红｜判据 `MinionManager.cs:359-388`（`bestIdx < 0` 只在无候选时出现）");

            // ---- A3：纵向**无容差**（旧 `snapToleranceY` = 0.09 会把这一点判成「不在这一行」）----
            bool yOk = board.TryResolveSlot(At(nx3, 0.42f + 0.45f), out s, -1, -1);
            Check(yOk && s == 3,
                  "A3 ★★ **纵向无容差**：指针在行心上方 0.45 归一化（旧 `snapToleranceY`=0.09 会判「不在这一行」）"
                + " ⇒ 仍解回槽 3｜🧨 把 y 带写回来 ⇒ 红");
            // A3′ 反向：出牌区线以下也照样解得出（原版那条线由**上游**判、不由格位解析判）
            bool lowOk = board.TryResolveSlot(At(nx5, 0.05f), out s, -1, -1);
            Check(lowOk && s == 5,
                  "A3′ ★ 指针在手牌带里（归一化 y=0.05）也**解得出格** —— 原版「出不出牌」由 `HandLimitArea` 那条 y 线在"
                + "**上游**决定（`BattleManager__Update.c:249`），⛔ 不由格位解析决定");

            // ---- A4：候选集 = 每侧「最内空格 + 内圈已占格」；督军位**永不是候选** ----
            bool c0 = board.TryResolveSlot(At(nx0, 0.42f), out s, 3, 3);
            Check(c0 && s == 0,
                  "A4a 每侧已占 3 ⇒ 该侧 4 格全在候选 ⇒ 指针在**最外格 0** 上解回 0（实得 ok=" + c0 + "、槽 " + s + "）");
            bool c4 = board.TryResolveSlot(At(nx4, 0.42f), out s, 3, 3);
            Check(c4 && s != 4 && (s == 3 || s == 5),
                  "A4b ★ 指针正对**督军位 4** ⇒ 解回最近的**非督军**格（实得槽 " + s + "）"
                + "｜判据 `MinionManager.cs:326-342`：候选里根本没有督军格｜🧨 把督军位放回候选 ⇒ 解回 4 ⇒ 红");
            bool full = board.TryResolveSlot(At(nx4, 0.42f), out s, 4, 4);
            Check(!full && s == -1,
                  "A4c ★ 两侧都满 ⇒ **解不出格**（原版 `start >= end` ⇒ 返回 0 = 无；⛔ 不是「随便给一格」）");

            // ---- A5：`allowNearest` 两取同解（容差删掉之后它不再分支）----
            int sa, sb;
            bool oa = board.TryResolveSlot(At(farX, 0.42f), out sa, -1, -1, false);
            bool ob = board.TryResolveSlot(At(farX, 0.42f), out sb, -1, -1, true);
            Check(oa == ob && sa == sb,
                  "A5 ★★ `allowNearest` 的**两种取值现在给同一个答案**（" + oa + "/" + sa + " vs " + ob + "/" + sb + "）"
                + " —— 容差删掉之后它只是签名兼容的遗留形参｜🧨 让它再分支（例如把 y 带挂回 false 那一档）⇒ 红");

            // ---- A6：平手留**更早下标**（原版 `if (real1 > real5)` 严格不等）----
            bool tie = board.TryResolveSlot(At(nx4, 0.42f), out s, -1, -1);
            Check(tie && s == 3,
                  "A6 正好落在两候选正中（x=" + nx4.ToString("F4") + "）⇒ 留**更早下标**的槽 3（实得 " + s + "）"
                + "｜判据 `MinionManager.cs:365-369`｜🧨 把 `<` 改成 `<=` ⇒ 红");
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>归一化 (x, y) → 拖拽平面的世界点（`TryResolveSlot` 收的就是它；3D 档才另外换屏幕点）。</summary>
    static Vector3 At(float nx, float ny) { return LayoutSpace.ToWorld(nx, ny); }

    // ==================================================================
    //  B · 「拖太高 + 打不出去 ⇒ 收回手牌」（原版 `case 2` 的 `maxHeightOfUnplayableCard`）
    // ==================================================================
    sealed class Fixture : System.IDisposable
    {
        public GameObject root;
        public HandLayout hand;
        public BoardLayout board;
        public CardInteraction it;
        public CardView card;
        public int flights, returns;
        public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
    }

    static Fixture Make(float lineY, float cardNormY, bool playable, string name)
    {
        var f = new Fixture();
        f.root = new GameObject("B121WB_root");
        f.hand = f.root.AddComponent<HandLayout>();
        f.board = f.root.AddComponent<BoardLayout>();
        f.it = f.root.AddComponent<CardInteraction>();
        f.it.hand = f.hand;
        f.it.board = f.board;
        f.it.SetRightButtonHeldForTest(false);
        f.it.SetBattleStateBlockedForTest(false);
        f.it.CanDropAtSlot = (slot, c) => playable;     // 引擎那一道换成替身：本件验的是「太高就放弃」这一闸
        f.hand.animateRelayout = false;
        f.board.lineY = lineY;
        f.it.OnDeployFlightStart += (c, b, s, d) => f.flights++;
        f.it.OnReturned += (c, k) => f.returns++;

        var d = CardData.Placeholder(1);
        d.id = name; d.artId = name;                     // ⛔ 别撞真实卡（`TacticTargetSide` 会拿 id 回查卡表）
        d.isUnit = true; d.type = "unit"; d.rarity = "common";
        f.card = CardView.Create(f.root.transform, d, name, CardFace.Hand);
        f.card.transform.position = LayoutSpace.ToWorld(0.5f, cardNormY);
        f.it.SetCards(new List<CardView> { f.card }, false);
        return f;
    }

    static void B_GiveUpTooHigh()
    {
        // ---- B1：常量 = 25 原版世界单位（⛔ 不是 ctor 默认的 3.0）----
        float bridge = HandLayout.OurUnitsPerWorldUnit;
        Check(Mathf.Abs(CardInteraction.MaxPlayHeightWorld - 25f * bridge) < 1e-6f
              && Mathf.Abs(CardInteraction.MaxPlayHeightWorld - 3.4340f) < 1e-3f,
              "B1 ★★ `MaxPlayHeightWorld` = 25 × 桥 = " + CardInteraction.MaxPlayHeightWorld.ToString("F4")
            + " 我们的单位（≈ 370.9 px）—— 判据 = `VarsGlobal` **序列化资产** `maxHeightOfUnplayableCard` = **25.0**"
            + "（`工具/read_varsglobal.py` 现读；ctor 里的 3.0 会被覆盖）"
            + "｜🧨 改成 ctor 的 3.0 ⇒ 0.4120 ⇒ 红");

        float cardY = 0.12f;                                  // 手牌带里（< HandLimitY 0.2778）
        float maxWorld = CardInteraction.MaxPlayHeightWorld;

        // ---- B2：打不出去 + 拖太高 ⇒ **收回手牌**（不提交、不播 cantdo）----
        {
            var f = Make(lineY: 0.42f, cardNormY: cardY, playable: false, name: "B121WB_B2");
            try
            {
                Vector3 home = f.card.transform.position;
                Vector3 tooHigh = new Vector3(home.x, home.y + maxWorld * 1.05f, home.z);
                Check(CardInteraction.IsInPlayArea(tooHigh),
                      "（前提·B2）那一点在出牌区里（归一化 y " + LayoutSpace.ToNormalized(tooHigh).y.ToString("F4")
                    + " ≥ " + BoardLayout.HandLimitY.ToString("F4") + "）");
                Check(tooHigh.y - home.y > maxWorld, "（前提·B2）抬高超过 `MaxPlayHeightWorld`");
                f.it.SimulateHover(home);
                f.it.SimulatePress(home);
                Check(f.it.Dragging == f.card, "（前提·B2）抓起来了");
                f.it.SimulateDrag(tooHigh, 1f / 60f);
                Check(f.it.Dragging == null && f.flights == 0 && f.it.HandCardCountForTest == 1,
                      "B2 ★★ **打不出去的牌拖得太高 ⇒ 收回手牌**（`Dragging` 空、起飞 0 次、手牌仍 1 张）"
                    + "｜判据 `BattleManager.cs:25794-25822`（`!CanPlayCard` ⇒ `MoveBackToHand` + `HandCardReleased`）"
                    + "｜🧨 删掉这条闸 ⇒ 牌一直留在手里 ⇒ 红");
                Check(f.returns == 0,
                      "B2b 这一支**不发 `OnReturned`**（原版这条支只有 `MoveBackToHand`，⛔ 没有「拖到目标上再松手」那句提示）");
            }
            finally { f.Dispose(); }
        }

        // ---- B3：**能打**的牌拖到同样的高度 ⇒ 原版不放弃（迟早由出牌闸提交）----
        {
            var f = Make(lineY: 0.42f, cardNormY: cardY, playable: true, name: "B121WB_B3");
            try
            {
                Vector3 home = f.card.transform.position;
                Vector3 tooHigh = new Vector3(home.x, home.y + maxWorld * 1.05f, home.z);
                f.it.SimulateHover(home);
                f.it.SimulatePress(home);
                f.it.SimulateDrag(tooHigh, 1f / 60f);
                Check(f.it.Dragging == null && f.flights == 1,
                      "B3 ★★ **能打出去的牌拖到同样高度 ⇒ 照样提交**（起飞 " + f.flights + " 次）"
                    + " —— 放弃闸只看「打不打得出去」，⛔ 不看高度本身"
                    + "｜🧨 写成「太高就一律收回」⇒ 起飞 0 次 ⇒ 红");
            }
            finally { f.Dispose(); }
        }

        // ---- B4：灭自证 —— 打不出去但**抬高不够** ⇒ 不收回（仍在拖）----
        {
            var f = Make(lineY: 0.42f, cardNormY: cardY, playable: false, name: "B121WB_B4");
            try
            {
                Vector3 home = f.card.transform.position;
                Vector3 notHigh = new Vector3(home.x, home.y + maxWorld * 0.6f, home.z);
                Check(CardInteraction.IsInPlayArea(notHigh),
                      "（前提·B4）这一点仍在出牌区里（归一化 y " + LayoutSpace.ToNormalized(notHigh).y.ToString("F4") + "）");
                f.it.SimulateHover(home);
                f.it.SimulatePress(home);
                f.it.SimulateDrag(notHigh, 1f / 60f);
                Check(f.it.Dragging == f.card,
                      "B4 ★ 打不出去、但抬高**没超过** 25 原版世界单位 ⇒ **还在拖**（闸只看高度）"
                    + "｜🧨 把高度门槛去掉 / 写成反的 ⇒ 红");
            }
            finally { f.Dispose(); }
        }
    }

    // ==================================================================
    //  C · 帧率默认档 = −1（调用点钳制已删）
    // ==================================================================
    static void C_FrameRateMinusOne()
    {
        int tfr = Application.targetFrameRate;
        Check(tfr <= 0,
              "C0 前提：运行时 `Application.targetFrameRate = " + tfr + "` ≤ 0（原版默认档 −1）。"
            + "⚠️ 若这里 > 0，C1 只是自洽读数、判别力下降 —— 不静默跳过，⛔ 也别读成「已验证 −1 档」");

        // ---- C1：剩 0.59 s 时（< 1 s）⇒ −1 档**当帧收尾** ----
        var go = new GameObject("B121WB_ctl");
        try
        {
            var ctl = new Card3DAnimationController(go.transform);
            int done = 0;
            bool started = ctl.DoAnim(new CardAnimPlayback { Prefab = "Probe", Duration = 0.6f, Label = "b121wb" }, false, () => done++);
            Check(started && ctl.PlayedCount == 1, "（前提·C1）0.6 s 的拍起来播了");
            ctl.Update(0.01f);                               // 剩 0.59 s —— 故意不推过时长
            int expect = Card3DAnimationController.FrameSlackExpired(tfr, 0.59f) ? 1 : 0;
            Check(done == expect && ctl.FinishedCount == expect,
                  "C1 ★ 真控制器与 `FrameSlackExpired(" + tfr + ", 0.59)` **同一个答案**（收尾 " + done + " / 期望 " + expect + "）"
                + "｜🧨 把 `if (fps <= 0) fps = 60;` 加回调用点 ⇒ 60 档下剩 0.59 s 不收尾 ⇒ 红");
            ctl.Detach();
        }
        finally { Object.DestroyImmediate(go); }

        // ---- C2：−1 档的语义边界 = 「砍掉最后 1 秒」，⛔ 不是「永远收尾」----
        Check(Card3DAnimationController.FrameSlackExpired(-1, 0.999f)
              && !Card3DAnimationController.FrameSlackExpired(-1, 1.001f),
              "C2 ★★ **−1 档的语义边界**：剩 0.999 s ⇒ 收尾；剩 1.001 s ⇒ 继续演"
            + "（`1/(-1) + t < 0` ⇔ `t < 1`；原版 `Card3DAnimationController__Update.c:22-23`）"
            + "｜🧨 给式子加 `fps <= 0 ⇒ 60` ⇒ 0.999 s 那一档变「继续演」⇒ 红");
    }
}
