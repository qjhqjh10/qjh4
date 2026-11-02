// B117_G10_06_Assert.cs — 执行代理 `B117-G10-06`（手牌域）的断言宿主（2026-11-02）
//
// 覆盖本代理改的 / 核过的四件事（判据逐条写在各组头里，全部来自第一权威反编译与解包原件）：
//   A. **撤掉悬停的那一张也瞬切** —— `StopShowingCardInHand` 三句直写（`ShowCardInHand` 的对称那一半）。
//      `B117-32` 只补了「进入」；本代理补「退出」（`HandLayout.Refresh` 的 `_hoverPrev`）。
//   B. **拖卡倾斜 `ry` 用 −Δx**（原版 `.c` 是 `^ DAT_1834b2c80` 翻符号位，**不是** `|Δx|`）。
//   C. **敌方手牌那一份 = 原版 `CardsHorizontalLayout` MB 4053** 的 8 个标量 + 4 条曲线（逐个对拍解包原件）。
//   D. **溢出压缩的口径**：只压**间距**（`cap ÷ n`，⛔ 不是 `n−1`）、**卡不缩** —— 原版 `GetPosition` 的压缩支。
//   E. **扇形方向**：我方两端下垂（`m_maxHeight` 正）、敌方两端上翘中间凹（`m_maxHeight` 负）。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_06_Assert.Run -logFile -
//
// 判绿红：看 `B117G1006 合计` 那一行；逐条按**行首** `B117G1006 ✓` / `B117G1006 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主要求：`Run()` **零前提** —— 不读 `BattleDriver` / 场景 / 存档；自带临时根 + 收尾 `DestroyImmediate`；
//   自己把 `CardTween.Mode` 拨成 `Manual`、`LinkEnabled` 拨成 `false`，退出原样还原（批处理没有帧循环）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B117_G10_06_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1006 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1006 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5")
                  + "；实得 " + got.ToString("F5") + "）");
    }

    static void Step(float dt) { CardTween.Advance(dt); }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;

            Group("A·撤悬停也瞬切", A_HoverExitSnap);
            Group("B·拖卡倾斜符号", B_DragTiltSign);
            Group("C·敌方手牌原版值", C_EnemyLayoutValues);
            Group("D·溢出压缩口径", D_OverflowCompression);
            Group("E·扇形方向", E_FanDirection);
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
        Debug.Log("B117G1006 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
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

    // ==================================================================
    //  A · 撤掉悬停那一张：**同一帧就落回槽位**（和「进入」对称）
    // ==================================================================
    //  判据（`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/CardScript.cs` 真方法体）：
    //   · `OnTouchExit`（`:4346-4356`）：`cardState == inHandShowing` ⇒ `StopShowingCardInHand(true)`；
    //   · `StopShowingCardInHand`（`:5124-5152`）**三句直写**（无补间无时长）：
    //       `transform.position = originalPosInHand` / `transform.rotation = originalRotInHand` /
    //       `transform.localScale = GetCardInHandScale()`；
    //   · `UpdateMinionInHandPosition`（`:3675`）开头 `if (cardState == CardStateOptions.inHand)`
    //     ⇒ 抬起态（state 8）**整条布局补间被跳过** ⇒ 原版对抬起那张从不补间、放回也是硬写。
    //  🔑 灭自证：A2（撤悬停那张当场落回）与 A3（其余牌**没**被一起瞬切）**不可能同时靠「全瞬切」满足**；
    //     删掉 `HandLayout.Place` 那一句的 `|| c == _hoverPrev` ⇒ A2 红（它会用 0.2 s 滑回去）。
    static void A_HoverExitSnap()
    {
        var root = new GameObject("B117G1006_A");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "G1006_A" + i, CardFace.Hand));

            // ① 先落到「没人悬停」的静止态（关补间 ⇒ 位姿当场精确）
            layout.animateRelayout = false;
            layout.Refresh(made, -1, -1);
            Vector3 rest1 = made[1].transform.localPosition;

            // ② 悬停第 0 张 + 0 号格留一个洞（洞让**其余牌**的目标位真的变了 —— 否则 A3 是同义反复）
            layout.animateRelayout = true;
            layout.Refresh(made, 0, 0);

            Vector3 wantHover = layout.SlotPosition(1, 4);        // hole ⇒ slots = 4，index 0 排到 slot 1
            wantHover.y = (layout.areaOffsetY - 0.5f) * LayoutSpace.VisibleHeight + layout.hoverLift;   // 🔴 2026-11-02（`B118-W2` · `B117-R64`）：悬停是**绝对 y** = 手牌区底边 + `hoverLift`
            wantHover.z += -layout.frontZ;
            Check(Vector3.Distance(made[0].transform.localPosition, wantHover) < 1e-3f,
                  $"A1 悬停那张**当场**抬到抬起位（回归 `B117-32`；期望 {wantHover}，实得 {made[0].transform.localPosition}）");

            // 悬停那张**转正** —— 原版 `ShowCardInHand` 第二句
            // `transform.rotation = Quaternion.Internal_FromEulerRad(Vector3.zeroVector × Deg2Rad)` ⇒ 单位四元数。
            // ⚠️ 读**渲染位姿**（`localEulerAngles`），⛔ 不读 `_lastPose`（那是目标值 ⇒ 同义反复）。
            float angHover = Mathf.DeltaAngle(0f, made[0].transform.localEulerAngles.z);
            CheckClose(angHover, 0f, 1e-3f,
                       "A1b ★ 悬停那张被**转正**（原版 `ShowCardInHand` 第二句写 `Vector3.zero` 的欧拉角 ⇒ 单位四元数）"
                     + "｜🧨 保留扇形角 ⇒ 红");

            Step(CardTween.RelayoutDuration + 0.05f);              // 其余牌走到位

            // 🔴 **2026-11-02（`B117-FIXD`）就地订正（铁律 5）**：撤悬停**之前**先把「其余牌此刻在哪」记下来 ——
            //   下面 A3 要断的是「这一趟 Refresh **没有**把它们一起瞬切」，所以参照物必须是
            //   **紧邻这一刻的旧位**（带洞那一趟 `SlotPosition(slot+1, n+1)` + `i × zOrderStep` 的 z），
            //   ⛔ 不是 A0 那一趟的 `rest1`（那是**撤完悬停之后**才该到的位置 —— 拿它当参照 = 拿目标当起点）。
            Vector3 hoverPos1 = made[1].transform.localPosition;

            // ③ 撤悬停（无洞）—— 这一张必须**同一帧**落回槽位
            layout.Refresh(made, -1, -1);
            Vector3 want0 = layout.SlotPosition(0, 3);            // 无洞 ⇒ 3 槽，index 0 → slot 0
            Vector3 got0 = made[0].transform.localPosition;
            Check(Vector3.Distance(got0, want0) < 1e-3f,
                  $"A2 ★ 撤悬停那张**当帧就落回槽位**（原版 `StopShowingCardInHand` 三句直写；"
                + $"期望 {want0}，实得 {got0}）"
                + "｜🧨 只让「被悬停那张」瞬切、放回走 0.2 s 补间 ⇒ 这一刻它还停在抬起位 ⇒ 红");

            Vector3 now1 = made[1].transform.localPosition;
            Check(Vector3.Distance(now1, hoverPos1) < 1e-4f,
                  $"A3 其余牌**没有**被一起瞬切（这一刻仍停在旧位 {hoverPos1}，实得 {now1}）"
                + "｜🧨 把整趟 `Refresh` 都改成瞬切 ⇒ 红");

            Step(CardTween.RelayoutDuration * 0.5f);
            float moved = Vector3.Distance(made[1].transform.localPosition, rest1);
            Check(moved > 1e-3f,
                  $"A4 推 0.1 s 后其余牌**真的在补间里**（离开旧位 {moved:F4} 世界单位）—— 与 A3 合起来 = "
                + "「撤悬停瞬切、其余补间」两半都在");
            Step(CardTween.RelayoutDuration + 0.05f);
            // 🔴 **2026-11-02（`B117-FIXD`）就地订正（铁律 5）**：参照物用 `rest1`（实测的**完整** `localPosition`），
            //   ⛔ 不用 `layout.SlotPosition(1, 3)` —— 那个只给 x/y，`Refresh` 还会叠 `i × zOrderStep`（第 1 张 = +0.08 z）
            //   ⇒ 拿它比**恒差 0.08**（与「补间有没有跑完」无关，量了没判别力）。
            Check(Vector3.Distance(made[1].transform.localPosition, rest1) < 1e-3f,
                  $"A5 推完 0.2 s 其余牌落到新槽位（`timeToPositionCard` 那一档；期望 {rest1}，实得 {made[1].transform.localPosition}）");

            // 撤悬停那张**扇形角也复原**（原版 `StopShowingCardInHand` 把 `originalRotInHand` 写回去）
            // ⚠️ 同样读**渲染位姿** —— A2 已经证明这一帧它是 snap 到位的，所以两者必须一致。
            float angBack = Mathf.DeltaAngle(0f, made[0].transform.localEulerAngles.z);
            Check(Mathf.Abs(Mathf.DeltaAngle(angBack, layout.RotationAt(0, 3))) < 1e-3f,
                  $"A6 撤悬停那张的**扇形角复原**（原版写回 `originalRotInHand`；期望 {layout.RotationAt(0, 3):F3}°，"
                + $"实得 {angBack:F3}°）｜🧨 只把位置瞬切、角度留在 0 ⇒ 红");
        }
        finally
        {
            foreach (var c in made) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  B · 拖卡倾斜：`ry` 用 **−Δx**（不是 |Δx|）
    // ==================================================================
    //  判据 = `d:/2/tools/decomp_full/CardScript__SetCardMovement.c:76-88`（亲读）：
    //    `fVar9 = (float)local_88 - (float)local_98;`            // Δx = target.x − prev.x
    //    `fVar9 = (float)((uint)fVar9 ^ DAT_1834b2c80);`         // ← **翻转符号位** ⇒ −Δx
    //    `rotationTarget.y = (max.y − min.y) × clamp01(−Δx / movingRotationReduction + 0.5) + min.y`
    //  数值：`maxCardRotation = (20,20)` · `minCardRotation = (−20,−20)` · `movingRotationReduction = 10`
    //  （ctor `CardScript__.ctor.c`：`0x2ec/0x2f0 = 0x41a00000` = 20 · `0x2f4/0x2f8 = 0xc1a00000` = −20 · `0x308 = 0x41200000` = 10）。
    //  🔑 灭自证：B1（右拖 ⇒ 负）与 B2（左拖 ⇒ 正）**符号必须相反**；旧写法 `|Δx|` 两次都落在 0..+20 ⇒ 两条一起红。
    static void B_DragTiltSign()
    {
        var root = new GameObject("B117G1006_B");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            for (int i = 0; i < 2; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "G1006_B" + i, CardFace.Hand));

            layout.animateRelayout = false;
            it.SetCards(made, false);
            layout.animateRelayout = true;

            Vector3 home = made[1].transform.position;
            it.SimulateHover(home);
            Check(it.HoveredIndex == 1, $"（前提）hover 到第 1 张（实得 {it.HoveredIndex} —— 不是 1 ⇒ 下面按不下去）");
            it.SimulatePress(home);
            Check(it.IsDragging, "（前提）抓起来了");
            Step(CardTween.PickUpDuration + 0.05f);        // 推完起拖那条 0.1 s 缩放补间 ⇒ 之后只有 UpdateDrag 写 localRotation

            // 右拖一步：Δx > 0 ⇒ ry 应为**负**
            it.SimulateDrag(home + new Vector3(2.5f, 0f, 0f), 1f / 60f);
            float ryRight = Mathf.DeltaAngle(0f, made[1].transform.localRotation.eulerAngles.y);

            // 再往左拖一步（从当前位跳到更左）：Δx < 0 ⇒ ry 应为**正**
            it.SimulateDrag(home + new Vector3(-1.5f, 0f, 0f), 1f / 60f);
            float ryLeft = Mathf.DeltaAngle(0f, made[1].transform.localRotation.eulerAngles.y);

            Check(ryRight < -0.5f,
                  $"B1 ★ 右拖 ⇒ 倾斜 `ry` **负**（原版 `^ DAT_1834b2c80` 翻符号；实得 {ryRight:F2}°）"
                + "｜🧨 用 `|Δx|` ⇒ 恒 ≥ 0 ⇒ 红");
            Check(ryLeft > 0.5f,
                  $"B2 ★ 左拖 ⇒ 倾斜 `ry` **正**（实得 {ryLeft:F2}°）｜🧨 用 `|Δx|` ⇒ 恒 ≥ 0 ⇒ 红");
            Check(ryRight * ryLeft < 0f,
                  $"B3 灭自证：两次倾斜**符号相反**（{ryRight:F2}° / {ryLeft:F2}°）"
                + " —— 只有「−Δx」这一档能同时满足 B1/B2/B3");

            it.SimulateRelease(home + new Vector3(-1.5f, 0f, 0f));
        }
        finally
        {
            foreach (var c in made) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  C · 敌方手牌 = 原版 `CardsHorizontalLayout` **MB 4053**
    // ==================================================================
    //  判据 = 解包原件 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/
    //         MonoBehaviour_4053.json`（宿主 `PlayerHand` MB 4350，`isPlayer: 0`，
    //         父链 `UpperAnchor/EnemyArea/HandArea`）—— 8 个标量 + 4 条曲线的关键帧**逐个手抄进断言**。
    //  🔑 灭自证：期望值是**原版序列化值**（不是 `HandLayout` 的字段）⇒ 把 `ConfigureForEnemy()`
    //     里任何一个数改成「我方那档」⇒ 本组逐条红。
    static void C_EnemyLayoutValues()
    {
        var root = new GameObject("B117G1006_C");
        try
        {
            var L = root.AddComponent<HandLayout>();
            L.ConfigureForEnemy();

            // ---- 8 个标量 ----
            CheckClose(L.maxLayoutSize, 0.51f, 1e-5f, "C1 `m_maxLayoutSize` = 0.51");
            CheckClose(L.maxLayoutSizeSmallScreen, 0.51f, 1e-5f, "C2 `m_maxLayoutSizeSmallScreen` = 0.51");
            CheckClose(L.betweenElementsSpacing, 0.6f, 1e-5f, "C3 `m_betweenElementsSpacing` = 0.6");
            Check(L.numberOfCardsForMaxHeight == 10, $"C4 `m_numberOfCardsForMaxHeight` = 10（实得 {L.numberOfCardsForMaxHeight}）");
            CheckClose(L.maxHeight, -0.78f, 1e-5f, "C5 `m_maxHeight` = **−0.78**（负的 ⇒ 中间往下凹）");
            Check(L.invertRotation, "C6 `m_invertRotation` = 1");
            Check(!L.useExtraSpaceOnSelectedCard, "C7 `useExtraSpaceOnSelectedCard` = 0（我方是 1）");
            CheckClose(L.cardScale, 0.54f, 1e-5f, "C8 `m_scale` = 0.54（⇒ 卡宽 122 px，我方 165）");

            // ---- 节点值（`m_verticalOffsetLookTo = +74.5` 要过 `OurUnitsPerWorldUnit` 那个桥）----
            CheckClose(L.lookToDistance, 74.5f * HandLayout.OurUnitsPerWorldUnit, 1e-4f,
                       "C9 `m_verticalOffsetLookTo` = +74.5 世界单位（我方 −330）");
            CheckClose(L.baselineY, HandLayout.EnemyBaselineY, 1e-6f, "C10 基准 = `EnemyBaselineY`（敌方 HandAnchor 距顶 0.32 px）");
            CheckClose(L.fanCenterX, 0.5f, 1e-6f, "C11 扇心 = 0.5（敌方 HandAnchor.x = 0；我方偏右 13.5 px）");

            // ---- 4 条曲线（取关键帧上的点，逐位对拍原件）----
            CheckClose(L.yAxisCurve.Evaluate(0.485413f), 0.010992f, 1e-5f, "C12 `m_yAxisCurve` 中点 0.485413 → 0.010992");
            CheckClose(L.yAxisCurve.Evaluate(-0.01f), -3.391766f, 1e-4f, "C13 `m_yAxisCurve` 左端 −0.010000 → −3.391766");
            CheckClose(L.heightModifierByCount.Evaluate(0.398679f), 0.181347f, 1e-4f,
                       "C14 `m_heightModifierBasedOnTotalCards` 0.398679 → 0.181347");
            CheckClose(L.heightModifierByCount.Evaluate(1f), 1.002358f, 1e-4f, "C15 `m_heightModifierBasedOnTotalCards` 尾 1 → 1.002358");
            CheckClose(L.rotationModifierByCount.Evaluate(0.25f), 0.25f, 1e-4f,
                       "C16 `m_rotationModifierBasedOnTotalCards` = 平线 y=x（0→0、1→1、切线都是 1）");
            CheckClose(L.aspectRatioModifier.Evaluate(2f), 0f, 1e-6f,
                       "C17 `maxLayoutSizeAspectRatioModifier` = **平线 0**（我方那条 (1.70,0)→(2.33,0.1013) 是 5271 的）");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  D · 溢出压缩 = **只压间距**（`cap ÷ n`）、**卡不缩**
    // ==================================================================
    //  判据 = `d:/2/tools/decomp_full/CardsHorizontalLayout__GetPosition.c:201-214`：
    //    `fVar17 = param_4(=张数) * fVar18(=lossyScale.x × m_betweenElementsSpacing);`
    //    `if (m_maxLayoutSize_at_0x80 < fVar17) { … fVar18 = fVar19 / (float)param_4; }`   // ← **除 n**
    //    `else { fVar10 = 0.0; }`                                                         // 装得下 ⇒ extra 归零
    //    x = (基准 − (n−1)/2 × 间距) + 让位 + index × 间距                                  // :215-236
    //  ⇒ **没有任何「按张数缩放」的项** —— 卡宽恒 `m_scale`（我方 0.73 / 敌方 0.54）。
    //  🔑 灭自证：D2 期望值写成 `cap / 8`（**独立复述原版式子**）；改成 `cap / 7` ⇒ 红；
    //     D5 断「12 张与 1 张的卡缩放相同」⇒ 谁给溢出加一档缩卡 ⇒ 红。
    static void D_OverflowCompression()
    {
        var root = new GameObject("B117G1006_D");
        var made = new List<CardView>();
        try
        {
            var L = root.AddComponent<HandLayout>();

            // 上限（独立复述原版：`(m_maxLayoutSize + aspectMod) × 该深度的可见宽`；16:9 ⇒ aspect = DesignAspect）
            float aspect = LayoutSpace.Cam != null ? LayoutSpace.Cam.aspect : LayoutSpace.DesignAspect;
            float cap = (0.59f + L.aspectRatioModifier.Evaluate(aspect)) * LayoutSpace.VisibleWidth;   // 0.59 = 5271 的 m_maxLayoutSize

            Check(!L.IsCompressed(7), "D1a 7 张：装得下 ⇒ **不压缩**（原版 16:9 的界在 8 张）");
            Check(L.IsCompressed(8), "D1b 8 张：装不下 ⇒ **压缩间距**");
            CheckClose(L.SpacingFor(8) * LayoutSpace.VisibleWidth, cap / 8f, 1e-4f,
                       "D2 ★ 压缩档间距 = `cap ÷ n`（**除 n**，原版照抄；🧨 写成 `cap ÷ (n−1)` ⇒ 红）");
            CheckClose(L.SpanWorld(8), 7f * (cap / 8f), 1e-4f,
                       "D3 跨度 = `(n−1) × 间距`（首卡中心↔末卡中心）");
            CheckClose(L.SpacingFor(4) * LayoutSpace.VisibleWidth, L.betweenElementsSpacing * LayoutSpace.Scale, 1e-4f,
                       "D4 装得下 ⇒ 用自然间距 `m_betweenElementsSpacing × Scale`（不压）");

            // 卡不缩：12 张与 1 张的卡缩放必须相同
            L.animateRelayout = false;
            for (int i = 0; i < 12; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "G1006_D" + i, CardFace.Hand));
            L.Refresh(made, -1, -1);
            float s12 = made[0].BaseScale;
            var one = new List<CardView> { made[0] };
            L.Refresh(one, -1, -1);
            float s1 = made[0].BaseScale;
            CheckClose(s12, s1, 1e-6f,
                       "D5 ★ 溢出档**卡不缩**（12 张与 1 张的手牌缩放相同）—— 原版压缩只改间距，`m_scale` 与张数无关"
                     + "｜🧨 谁给溢出加一档缩卡 ⇒ 红");
            CheckClose(s1, L.cardScale * LayoutSpace.Scale, 1e-6f, "D6 手牌缩放 = `m_scale 0.73 × Scale`（我方）");
        }
        finally
        {
            foreach (var c in made) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  E · 扇形方向：我方两端下垂 / 敌方两端上翘中间凹
    // ==================================================================
    //  判据 = 两份实例的 `m_maxHeight` 符号（5271 = +0.70 · 4053 = **−0.78**）乘在同一条「两端低中间高」的
    //        `m_yAxisCurve` 上（`GetPosition.c:235-236`：`y = 基准 + curve(t) × maxHeight × hMod`）。
    static void E_FanDirection()
    {
        var root = new GameObject("B117G1006_E");
        try
        {
            var P = root.AddComponent<HandLayout>();      // 我方默认（5271）
            Check(P.SlotPosition(0, 5).y < P.SlotPosition(2, 5).y - 1e-4f,
                  $"E1 我方：两端**下垂**（端 {P.SlotPosition(0, 5).y:F3} < 中 {P.SlotPosition(2, 5).y:F3}）"
                + "｜判据 = `m_maxHeight +0.70` × 两端为负的 `m_yAxisCurve`");

            var E = root.AddComponent<HandLayout>();
            E.ConfigureForEnemy();
            Check(E.SlotPosition(0, 5).y > E.SlotPosition(2, 5).y + 1e-4f,
                  $"E2 敌方：两端**上翘、中间往下凹**（端 {E.SlotPosition(0, 5).y:F3} > 中 {E.SlotPosition(2, 5).y:F3}）"
                + "｜判据 = `m_maxHeight −0.78`（负的）× 负曲线 ⇒ 弧高变正");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
