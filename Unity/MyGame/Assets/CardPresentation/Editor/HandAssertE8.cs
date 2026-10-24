// HandAssertE8.cs — 执行代理 `E8` 的断言宿主（2026-10-24）
//
// 覆盖三组（判据全部来自原版，逐条写在每组的方法头里）：
//   A. **手牌重排的那一跳：局部角度 + 缩放的所有权**
//      （原版 `CardScript.UpdateMinionInHandPosition` 的 `DOLocalRotate` / `DOScale`；
//        以及 `CardView.TweenPose` 每帧把 `_baseScale` 一起带 —— `HandLayout.Place` 2026-10-24 已换过去）
//   B. **手牌重排的「动画闸」**（原版 `PlayerHand.MoveCardsInHandToPosition` 里 `Toggle(false)` 那 0.2 s）
//   C. **松手回弹 = 普通手牌重排**（原版**没有**回弹专用补间；0.2 s / 位置+角度+缩放 / OutQuad）
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod HandAssertE8.Run -logFile -
//
// 判绿红：`grep "^E8"` 的**合计行**；逐条按**行首** `E8 ✓` / `E8 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求（给调度台挂进 `Editor/BattleScene.cs` 时看）：
//   · **零前提**：不读 `BattleDriver`、不读场景、不读游戏数据；自带临时根 + 收尾 `DestroyImmediate`。
//   · 自己在 `Run()` 里把 `CardTween.Mode` 拨成 `Manual`、`CardTween.LinkEnabled` 拨成 `false`，
//     退出时**原样还原**（所以它可以插在别的段中间，不会污染别人的补间口径）。
//   · `LayoutSpace.Cam` 有没有都行（`VisibleWidth` 在 `Cam == null` 时回落 `DesignAspect`）；
//     但**期望值全部从被测同一份公开 API 现算**（`SlotPosition` / `RotationAt` 的**原版公式**在
//     `HandLayout` 里，那两条本批没改），⛔ 没有一个「我们自己的常量」被当成期望值。
//   · 唯一会出声的地方：`CardView.Create` 在批处理里没有美术资源时照旧建占位面（与 `CardBaseDemo` 一致）。
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using CardPresentation;

public static class HandAssertE8
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            // 批处理没有帧循环 ⇒ 只有 `CardTween.Advance(dt)` 推得动补间（同 `CardBaseDemo` / `BattleScene`）。
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            // 探针对象收尾要 `DestroyImmediate`；补间不绑生命周期，免得在已销毁的 GameObject 上 `SetLink` 空转报警。
            CardTween.LinkEnabled = false;

            A_LocalRotationAndScaleOwnership();
            B_AnimGate();
            C_Bounce();
            D_InitialCardScale();
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
        Debug.Log("E8 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  A · 手牌重排那一跳：**局部**角度 + `_baseScale` 的所有权
    // ==================================================================
    //
    // 🔴 判据（一手，`d:/2/tools/decomp_full/`，亲读）：
    //   · `CardScript__UpdateMinionInHandPosition.c`：
    //       角度 = 先 `Quaternion.Internal_ToEulerRad` × `Rad2Deg` 再 `Internal_MakePositive`
    //              ⇒ `DOLocalRotate(transform, euler, param_4, 0, 0)`（`:33`）—— **局部**角；
    //       缩放 = `DOScale(transform, GetCardInHandScale(), param_4, 0)`（`:43`）—— 写 `localScale`；
    //       位置 = `DOMove(transform, pos, param_4, 0, 0)`（`:52`）—— 世界；
    //       时长 `param_4` = `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:76-80` 读
    //       `globalVars + 0xA4`（`timeToPositionCard` = 0.2 s）；**全文没有 `SetEase`** ⇒ 吃
    //       `DOTween.defaultEaseType`（原版 `DOTweenSettings` 实读 = 6 `OutQuad`）。
    //   · `CardView.TweenPose`（`Core/CardView.cs:8039`）每帧走 `SetPose` ⇒ 局部位置 + 局部角度 +
    //     `_baseScale` **一起带**；`HandLayout.Place`（`Hand/HandLayout.cs:838-883`）2026-10-24 已整段换过去
    //     （原来走 `CardTween.ToPose` 的 Sequence 版，`DOScale` 直写 `localScale`）。
    //
    // 为什这两半都要在这里断（A3 在 `CardBaseDemo.AssertHandPoseTween` 断的是**终值**）：
    //   ① 世界欧拉与局部欧拉在**终值**上差 30°、在**半程**上差 `0.75 × 30° = 22.5°`
    //      —— 半程那一条把「补间末帧被谁拉回来」这种巧合也堵掉了；
    //   ② `DOScale` 直写 `localScale` 时 `_baseScale` **整段都停在起点值**（`T04` 那条已知缺陷），
    //      所以「半程 `BaseScale` 已经走到 0.75 处」是**结构上不可能靠巧合成立**的那一格。
    static void A_LocalRotationAndScaleOwnership()
    {
        var root = new GameObject("E8_A_rot");
        var made = new List<CardView>();
        try
        {
            // 父件带 **30° 旋转 + 2× 缩放**：局部角与被变换的局部位都会分叉（正是要测的那一档）。
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 0f, 30f));
            root.transform.localScale = Vector3.one * 2f;
            var layout = root.AddComponent<HandLayout>();
            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "E8_A_" + i, CardFace.Hand));

            const int n = 3;
            var c0 = made[0];
            float wantAng = layout.RotationAt(0, n);
            float startAng = layout.RotationAt(1, n);
            Vector3 wantWorld = layout.SlotPosition(0, n);
            Vector3 startWorld = layout.SlotPosition(1, n);
            float wantScale = layout.cardScale * LayoutSpace.Scale;
            float startScale = wantScale * 0.5f;      // 故意不同 ⇒ 缩放那一格真的在补

            Check(Mathf.Abs(Mathf.DeltaAngle(startAng, wantAng)) > 1f,
                  "前提：起点角与终点角**不同**（差 "
                  + Mathf.Abs(Mathf.DeltaAngle(startAng, wantAng)).ToString("F2") + "°）"
                  + "｜不足 1° ⇒ 下面角度那两条**空验**（扇形的张角太小）");

            // 起点：槽位 1、半档缩放。走 `SetPose` 落地（⛔ 不直接写 transform —— 那样不认 `_baseScale`）。
            c0.SetPose(root.transform.InverseTransformPoint(startWorld), startAng, startScale);
            CheckClose(c0.BaseScale, startScale, 1e-4f, "前提：起点 `BaseScale` 已经落实到 " + startScale.ToString("F4"));

            layout.animateRelayout = true;             // 出厂 false（批处理/演示档）⇒ 手工打开，补间才会建
            layout.Refresh(made, -1, -1);              // → HandLayout.Place → CardView.TweenPose

            // ---- 半程（0.1 s）：OutQuad 在 0.5 处的值 = 0.75 ----
            Step(CardTween.RelayoutDuration * 0.5f);
            float wantMidAng = startAng + Mathf.DeltaAngle(startAng, wantAng) * 0.75f;
            float midAng = c0.transform.localEulerAngles.z;
            Check(Mathf.Abs(Mathf.DeltaAngle(midAng, wantMidAng)) < 1.0f,
                  $"★ 重排补间**半程**写的是【局部】角：要 {wantMidAng:F2}°，实得**局部** {midAng:F2}°"
                + $"（父件带 30° ⇒ 世界欧拉那一版在这一刻会落在 {wantMidAng - 22.5f:F2}° 附近）"
                + "｜判据 `CardScript__UpdateMinionInHandPosition.c:33` 的 `DOLocalRotate`"
                + "｜🧨 改回 `DORotate`（世界欧拉）⇒ 半程差 0.75×30°=22.5°、终值差 30° ⇒ 红");

            float wantMidScale = startScale + (wantScale - startScale) * 0.75f;
            CheckClose(c0.BaseScale, wantMidScale, 5e-3f,
                       "★ 补间**进行中** `CardView.BaseScale` 就跟着走（OutQuad 半程 "
                     + wantMidScale.ToString("F4") + "）"
                     + "｜判据 `Core/CardView.cs:8039` 的 `TweenPose`（每帧 `SetPose` ⇒ 连 `_baseScale` 一起带）"
                     + "｜🧨 退回 `CardTween.ToPose` / `transform.DOScale(...)`（直写 `localScale`）⇒ "
                     + "`BaseScale` 整段停在起点 " + startScale.ToString("F4") + " ⇒ 红");

            // ---- 终点 ----
            Step(CardTween.RelayoutDuration);
            float endAng = c0.transform.localEulerAngles.z;
            Check(Mathf.Abs(Mathf.DeltaAngle(endAng, wantAng)) < 0.5f,
                  $"★ ……推完之后**局部**角落到扇形角 {wantAng:F2}°（实得 {endAng:F2}°）");
            Check(Vector3.Distance(c0.transform.position, wantWorld) < 1e-3f,
                  $"★ ……而且**世界位置**仍落在槽位 0：要 ({wantWorld.x:F3},{wantWorld.y:F3})，"
                + $"实得 ({c0.transform.position.x:F3},{c0.transform.position.y:F3})"
                + "（父件带旋转+2× 缩放 ⇒ 喂世界坐标前必须 `InverseTransformPoint`）"
                + "｜🧨 `Place` 里不换算（把世界位当局部位喂 `SetPose`）⇒ 红");
            CheckClose(c0.BaseScale, wantScale, 1e-3f,
                       "★ ……终点 `BaseScale` = 手牌档 " + wantScale.ToString("F4")
                     + "（`T04` 那条潜伏缺陷的症状就是「这里还停在起点值」）");
            CheckClose(c0.transform.localScale.x, wantScale * c0.HighlightMul, 1e-3f,
                       "★ ……`localScale` = `BaseScale × 高亮系数`（"
                     + (wantScale * c0.HighlightMul).ToString("F4") + "）"
                     + "｜🧨 直写 `localScale` ⇒ 高亮那一档被抹掉 / 补间一结束被弹回旧缩放 ⇒ 红");
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  B · 手牌重排的「动画闸」
    // ==================================================================
    //
    // 🔴 判据（`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c`，亲读）：
    //   · 状态 0（`:44-47`）对名单里**每一张**牌调 `CardCollider2D.Toggle(false)`
    //     —— 体 = `Behaviour.set_enabled(boxCollider2D, false)`；
    //   · 状态 1（`:126-127`，即 `WaitForSeconds(timeToPositionCard)` 之后）再调
    //     `SetColliderInHand()` —— 它末尾就是 `Toggle(true)`。
    //   ⇒ 原版语义 = **重排补间这 0.2 s 里，手牌一张都点不到**；时长 = `timeToPositionCard` = 0.2 s。
    //   · `+0x61`（`isDoingAnim`）只在 `selectedCard == -1` 时置位（`:33`），读口 `PlayerHand__IsDoingAnim.c`。
    //   ⚠️ 我们**没有卡碰撞体开关**这一层（命中走 `CardView.HitByCollider` 的常开 collider），
    //      所以闸落在交互层（`Hand/CardInteraction.cs:237-267` 的 `_animGate`）——
    //      开闸只在「整把重排」那几条路（`SetCards` / `Release` / `EndPressCandidate`）。
    //
    // 灭自证：③④（闸内什么都进不来）与 ⑦⑧（闸一放开同样的两步必须成立）是**结构上互斥的一对** ——
    //   闸写成「永远禁止」⇒ ③④ 绿、⑦⑧ 红；闸写成「不存在」⇒ ③④ 红、⑦⑧ 绿。
    static void B_AnimGate()
    {
        var root = new GameObject("E8_B_gate");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            // 自检口：把「右键按着 ⇒ 不许起拖」那道闸钉成 false —— 批处理里 `Mouse.current` 有可能是真的
            // （编辑器带输入设备时），不钉死的话「抓不起来」那条可能因为**右键**而绿 = 假绿。
            it.SetRightButtonHeldForTest(false);
            // board / foeBoard 故意留 null ⇒ `Release` 一定走「回弹」那一支（`DropReject` 返回 NoBoard），
            // 而且 `CardInteraction.Update` 第一句 `board == null ⇒ return`（批处理里它不会自己跑）。

            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "E8_B_" + i, CardFace.Hand));

            const int n = 3;
            layout.animateRelayout = false;
            it.SetCards(made, false);                  // 逐位 snap（不补间 ⇒ 也不开闸）
            Vector3 p0 = layout.SlotPosition(0, n);
            Check(it.HandCardCountForTest == n, "前提：三张牌都进了交互层（实得 " + it.HandCardCountForTest + "）");
            Check(it.HoveredIndex == -1, "前提：起手没有 hover");

            layout.animateRelayout = true;             // 出厂 false ⇒ 手工打开（没有动画就没有「重排期间」）
            it.Relayout();                             // = 原版 `MoveCardsInHandToPosition(-1, …)`
            Check(it.IsDoingAnimForTest,
                  "★① 整把重排一开始闸就是开的（= 原版 `isDoingAnim = 1` + 每张 `Toggle(false)`）"
                + "｜🧨 删掉 `Relayout(true)` 里那句 `ArmAnimGate()` ⇒ 红");
            Check(it.AnimGateRemainingForTest > 0.85f * CardTween.RelayoutDuration
                  && it.AnimGateRemainingForTest <= CardTween.RelayoutDuration + 1e-3f,
                  $"★② 闸长 = `timeToPositionCard` 0.2 s（实得 {it.AnimGateRemainingForTest:F3}）"
                + "｜判据 `_MoveCardsInHandToPosition_d__76__MoveNext.c:104-106` 的 `WaitForSeconds`"
                + "｜🧨 把 `ArmAnimGate` 的时长写成别的数（比如 0.1）⇒ 红");

            it.SimulateHover(p0);
            Check(it.HoveredIndex == -1,
                  "★③ 闸内**不产生新的 hover**（原版这 0.2 s 里手牌碰撞体全是关的）"
                + "｜🧨 删掉 `UpdateHover` 开头那句 `if (IsDoingAnim) return;` ⇒ 红");

            it.SimulatePress(p0);
            Check(!it.IsDragging,
                  "★④ 闸内按在牌上**抓不起来**"
                + "｜判据 `_MoveCardsInHandToPosition_d__76__MoveNext.c:44-47` 的 `Toggle(false)`"
                + "｜🧨 删掉 `PickUp` 开头那句 `if (IsDoingAnim) return;` ⇒ 红");

            Step(0.1f);
            Check(it.IsDoingAnimForTest, "⑤ 半程（0.1 s）闸还开着 —— 🧨 把时长写成固定小值 / 立刻关 ⇒ 红");
            Step(0.11f);
            Check(!it.IsDoingAnimForTest,
                  "★⑥ 过 0.2 s 闸自己松开（= 原版 `SetColliderInHand()` 末尾那句 `Toggle(true)`）"
                + "｜🧨 把闸写成「永远开着」⇒ 红");

            // 对照：闸放开之后，同样的两步**必须**成立（与 ③④ 结构互斥）。
            it.SimulateHover(p0);
            Check(it.HoveredIndex == 0,
                  "★⑦（对照）闸一放开 hover 照样进得来（实得 " + it.HoveredIndex + "）"
                + "｜🧨 上面 ③④ 若是「卡压根就不响应」而绿，这条会红");
            it.SimulatePress(p0);
            Check(it.IsDragging, "★⑧（对照）闸一放开照样抓得起来 —— 🧨 同上");

            // 收尾：松手回手牌（board == null ⇒ 回弹），把这一趟的补间/闸都推完，别留给下一个宿主。
            it.SimulateRelease(p0);
            Step(CardTween.RelayoutDuration * 2f);
            Check(!it.IsDragging && it.HandCardCountForTest == n,
                  "（收尾）牌回到手牌（仍 " + it.HandCardCountForTest + " 张、没有正在拖的）");
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  C · 松手回弹 = **普通手牌重排**（原版没有回弹专用补间）
    // ==================================================================
    //
    // 🔴 判据（`d:/2/tools/decomp_full/BattleManager__Update.c` case 2 的松手那一支，亲读）：
    //   `:210` `CardScript.MoveBackToHand`（只改状态：sortingLayer + `StopAnimsInPlay` + `SetStateInHand`
    //          → `SetParent(handAnchorLayout, worldPositionStays: true)`，**全文一个 `DOMove` 都没有**）；
    //   `:223` `PlayerHand.HandCardMovedToNewIndex` / `:246` `PlayerHand.HandCardReleased`
    //          ⇒ 两条 `MoveCardsInHandToPosition(-1, null)` ⇒ 对**每一张**（含刚回来那张）开
    //          `DOMove` / `DOLocalRotate` / `DOScale`，**0.2 s**、**不写 `SetEase`**（默认 `OutQuad`）。
    //   ⇒ 「回弹」= 普通重排：**从松手那一刻的位置起补**、0.2 s、OutQuad。
    //   （原来那条自造的 `SnapBackDuration = 0.22` 序列已删；`Core/CardTween.cs` 里也不再定义它。）
    //
    // 灭自证：②（半程 0.75 = OutQuad 单补间）与 ③（**正好** 0.2 s 到位）钉两个互相独立的量：
    //   形状对而时长错（0.22 那条）⇒ ③ 红；时长对而形状错（Sequence 层再挂一次 OutQuad ⇒ 半程 0.9375）⇒ ② 红。
    static void C_Bounce()
    {
        var root = new GameObject("E8_C_bounce");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            // 自检口：把「右键按着 ⇒ 不许起拖」那道闸钉成 false —— 批处理里 `Mouse.current` 有可能是真的
            // （编辑器带输入设备时），不钉死的话「抓不起来」那条可能因为**右键**而绿 = 假绿。
            it.SetRightButtonHeldForTest(false);
            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "E8_C_" + i, CardFace.Hand));

            const int n = 3;
            layout.animateRelayout = false;
            it.SetCards(made, false);
            layout.animateRelayout = true;
            // 拿**最右**那张：它的扇形角 ≈ −3.7°，而下面把它拖到最左 ⇒ 重排后落到槽 0（≈ +3.7°）。
            // 这样「回弹带角度」那一格才不是同义反复（回原位的话角度不变 = 缺角度补间也绿）。
            const int pick = 2;
            var card = made[pick];
            Vector3 home = layout.SlotPosition(pick, n);

            it.SimulateHover(home);
            Check(it.HoveredIndex == pick, "前提：hover 到第 " + pick + " 张（实得 " + it.HoveredIndex
                                         + "；不是 " + pick + " ⇒ 下面按不下去）");
            it.SimulatePress(home);
            Check(it.IsDragging, "前提：抓起来了");

            // 拖到**最左**（归一化 x 0.12 ⇒ `GetClosestInHandSlot` 给槽 0；board == null ⇒ 松手一定被拒）
            Vector3 far = LayoutSpace.ToWorld(0.12f, 0.25f);
            for (int i = 0; i < 10; i++) it.SimulateDrag(far, 1f / 30f);
            float off = Vector3.Distance(card.transform.position, home);
            Check(off > 1.0f, "前提：松手前卡已经离原位 " + off.ToString("F2")
                            + " 世界单位（>1 ⇒ 下面「从当前位置起补」那条才有分辨率）");
            float angBefore = card.transform.localEulerAngles.z;   // 拖拽期角度不变（PickUp 的目标就是当前角）

            it.SimulateRelease(far);
            Check(!it.IsDragging && it.HandCardCountForTest == n,
                  "前提：松手后这张牌回到手牌（没有被当成打出；仍 "
                  + it.HandCardCountForTest + " 张）");

            Vector3 wantWorld;
            float wantAng, wantScale;
            Check(layout.TryLastPose(card, out wantWorld, out wantAng, out wantScale),
                  "前提：`HandLayout._lastPose` 里有这张牌的落点（回弹补间的终点就是它）");
            Check(wantScale > 0f, "前提：落点缩放有效（" + wantScale.ToString("F4") + "）");
            Check(Mathf.Abs(Mathf.DeltaAngle(angBefore, wantAng)) > 1.5f,
                  "前提：回弹前后**角度真的变了**（拖拽期 " + angBefore.ToString("F2") + "° ⇒ 落位 "
                  + wantAng.ToString("F2") + "°）"
                + "｜不足 1.5° ⇒ 下面「带角度」那条**空验**（牌回的是原位）");

            Vector3 a = card.transform.position;       // ⚠️ 松手这一帧补间已经建好 ⇒ 读到的就是**起点**
            Check(Vector3.Distance(a, wantWorld) > 1.0f,
                  "★① 回弹**从当前位置起补**（不是先瞬移回槽位再补）：起点离终点 "
                  + Vector3.Distance(a, wantWorld).ToString("F2") + " 世界单位"
                + "｜🧨 用「先 snap 回槽位、再播一条 0 位移的补间」顶替 ⇒ 红");

            Step(CardTween.RelayoutDuration * 0.5f);
            Vector3 mid = card.transform.position;
            Step(CardTween.RelayoutDuration * 0.5f);
            Vector3 b = card.transform.position;

            float travelled = Vector3.Distance(a, b);
            Check(travelled > 1e-3f, "前提：这 0.2 s 里卡真的走了 " + travelled.ToString("F3") + " 世界单位");
            float p = Vector3.Distance(a, mid) / Mathf.Max(1e-6f, travelled);
            Check(Mathf.Abs(p - 0.75f) < 0.05f,
                  $"★② 回弹**半程走了 0.75 的位移**（OutQuad 在 0.5 处的值；实得 {p:F3}）"
                + "｜判据 `UpdateMinionInHandPosition` 那三条**单**补间吃默认 `defaultEaseType`=6"
                + "｜🧨 换成「Sequence 层再挂一次 OutQuad」的旧写法 ⇒ 半程 0.9375 ⇒ 红"
                + "｜🧨 换成 `Ease.Linear` ⇒ 半程 0.5 ⇒ 红");

            Check(Vector3.Distance(b, wantWorld) < 1e-3f,
                  $"★③ **正好 0.2 s 就到位**（`timeToPositionCard`；实得离终点 "
                + $"{Vector3.Distance(b, wantWorld):F4} 世界单位）"
                + "｜🧨 换回旧的 0.22 s 自造序列 ⇒ 0.2 s 时只走到 91% ⇒ 红");
            Check(Mathf.Abs(Mathf.DeltaAngle(card.transform.localEulerAngles.z, wantAng)) < 0.5f,
                  $"★④ 回弹**带角度**（终值 = 落位槽的扇形角 {wantAng:F2}°，实得 "
                + $"{card.transform.localEulerAngles.z:F2}°）"
                + "｜原版三条单补间里有一条 `DOLocalRotate`，旧的 0.22 序列**没有角度**"
                + "｜🧨 把角度那一跳去掉 ⇒ 卡停在拖拽前的 " + angBefore.ToString("F2") + "° ⇒ 红");
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  D · `initialCardScale = 150 / initialHeight = 512` 的**口径复核**（收藏 Cards 页）
    // ==================================================================
    //
    // 🔴 判据（一手）：
    //   · `CollectionCard__OnRectTransformDimensionsChange.c:16-22`（本批亲读）：
    //       `sizeDelta.y × [this+0x70](initialCardScale 150) ÷ [this+0x74](initialHeight 512)`
    //       → `cardUI.transform.localScale = (s,s,s)`；
    //     序列化实读 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_40971030049256792.json`：
    //       `initialCardScale 150.0` · `initialHeight 512.0`。**不在 `AnimateIn` 那条路上**（A14 的更正成立）。
    //   · ⚠️ **这个 s 是「每卡单位多少 px」的绝对缩放，⛔ 不能直译成我们的 `SetPose(scale)`**
    //     （我们的 scale 是「相对卡原生大小的世界缩放」）。直译（`scale = 格高×150/512÷108`）⇒
    //     卡渲成 `3.3313 × 384 × 150/512` = **374.8 px** 高。
    //   · 🔴 而且**原版公式的输入反推不出来**：`CardUI` 的 `RectTransform` 在 stretch 锚下
    //     `m_SizeDelta = (−350, −512)`、父 `Content` 已 stretch 到 `Collection Card` 的 350×512
    //     ⇒ 算出 **0×0**；`localScale` 出厂就是 150。⇒ 卡的真尺寸/位置是**运行时**设的
    //     （同族结论见 `资料/普查产出_0923/A4_装饰页与驱动链.md:149,223` 的「查不到」）。
    //   · 🔑 **最终判据 = 用户的原版实拍** `C:/Users/qjh36/Desktop/warpforge参考图/
    //     收藏—张牌的界面参考图（打开过滤器按钮后）.png`：卡框底边（含银色下边框）在**张数条之上**，
    //     两者几乎相接、**没有重叠** ⇒ 卡只占「条顶以上那一段」= 格高 − 张数条高。
    //     ⇒ **现行口径（`CardsCardRect` = 格高 − `CardsCounterH`）与原版实拍一致，本批【不改】**；
    //       把 150/512 直译进来反而会压条 **29.1 px**（与实拍矛盾）。
    //
    // 这一组钉的是**口径本身**（`Shell/CollectionWindow.cs` 那两条 public 几何），不是在断实现：
    //   🧨 谁把 `CardsCardRect` 改成「整格 384」/ 把卡按 `150/512` 直译 ⇒ ② 与 ③ 变红。
    static void D_InitialCardScale()
    {
        // 一格（相对坐标即可，两条几何都只吃相对量）
        var cell = new PxRect(0f, 0f, CollectionWindow.CardsCellW, CollectionWindow.CardsCellH);
        var cardRect = CollectionWindow.CardsCardRect(cell);
        var barRect = CollectionWindow.CardsCounterRect(cell);
        float barH = cell.y2 - barRect.y1;             // 张数条占掉的高度（原版锚点 `CardsCntMaxY`）

        Check(Mathf.Abs(cardRect.y2 - barRect.y1) < 1e-3f,
              $"★① 卡区**底边**正好停在张数条**框顶**（卡区底 {cardRect.y2:F2} vs 条顶 {barRect.y1:F2}）"
            + "｜判据 = 用户原版实拍「卡框底边在张数条之上、无重叠」（见本段方法头）"
            + "｜🧨 卡区改成整格（`CardsCellRect`）⇒ 卡底 = 格底 ⇒ 压条 38.27 px ⇒ 红");

        CheckClose(cell.y2 - cardRect.H, barH, 0.01f,
                   "★② 卡区高 = 格高 − 张数条高（两段拼满一格，一格不少一格不多）"
                 + "｜期望值 = 原版 prefab 的张数条锚点（`CardsCntMaxY × CardsCellH`）"
                 + "｜🧨 `CardsCardRect` 少减/多减一格 ⇒ 红");

        // 原版公式**直译**到我们口径会长成多高（纯原版常数算术，与我们的常量无关）：
        //   `initialCardScale / initialHeight` = 150/512 = 0.29296875（每 px 格高给多少「px/卡单位」）
        //   卡单位高 3.3313（`CardView.Height`）⇒ 渲染高 = 3.3313 × 384 × 0.29296875 = 374.76 px
        float naivePx = CardView.Height * CollectionWindow.CardsCellH * (150f / 512f);
        Check(cardRect.H < naivePx - 20f,
              $"★③ ⛔ **别把 `150/512` 直译成我们的 `SetPose` 口径**：直译渲成 {naivePx:F2} px 高，"
            + $"卡区只有 {cardRect.H:F2} px ⇒ 会压进张数条 {naivePx - cardRect.H:F2} px"
            + "｜判据 = 原版实拍「无重叠」＋ `CardUI` 的 rect 在 prefab 里是 0×0（`sizeDelta.y` 是运行时值，"
            + "静态推不出来）｜🧨 把卡区改成整格 / 把 `CardsCellH` 抬大到让差值 <20 px ⇒ 红");
    }

    // ==================================================================
    //  工具
    // ==================================================================

    /// <summary>推进补间（批处理没有帧循环；本宿主不建粒子，所以不用 `Simulate`）。</summary>
    static void Step(float dt) { CardTween.Advance(dt); }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("E8 ✓ " + msg); }
        else { _fail++; Debug.LogError("E8 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }
}
