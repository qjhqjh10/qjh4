// HandAssertE8.cs — 执行代理 `E8` 的断言宿主（2026-10-24）
//
// 覆盖三组（判据全部来自原版，逐条写在每组的方法头里）：
//   A. **手牌重排的那一跳：局部角度 + 缩放的所有权**
//      （原版 `CardScript.UpdateMinionInHandPosition` 的 `DOLocalRotate` / `DOScale`；
//        以及 `CardView.TweenPose` 每帧把 `_baseScale` 一起带 —— `HandLayout.Place` 2026-10-24 已换过去）
//   B. **手牌重排的「动画闸」**（原版 `PlayerHand.MoveCardsInHandToPosition` 里 `Toggle(false)` 那 0.2 s）
//   C. **松手回弹 = 普通手牌重排**（原版**没有**回弹专用补间；0.2 s / 位置+角度+缩放 / OutQuad）
//   D. 初始手牌缩放
// 🆕 2026-10-25（`X9`）追加三组（同一宿主、**同一族只此一处**）：
//   E. **战术卡落点只认卡面要的那一侧**（跨侧拒 + 不回落；本组是唯一读卡表的一组）
//   F. **输入闸**（模态窗 / 选目标 / 打法选择器 / 终局 ⇒ 抓不起来 + 打断已拖那张）
//   G. **回放档**（只禁拖；轻点看牌**必须仍然可以** —— 防一刀切）
//   ⚠️ 真实引擎链路的落点复核不在这里，在 `Editor/BattleScene.cs` §10（`X9-A/B/C`）。
// 🆕 2026-10-25（`X19/P1`）追加：
//   H. **手牌文字的量测通道是活的**（`BattleDriver.ProbeHandText` 的灭自证）——
//      真机 `b14_B_hand_zoom.png` 上「卡名 + 正文都越框」而自检复现不出来 ⇒ 这一组先钉住
//      「尺子本身量得到、而且注入越框时它真的会报」（否则「没越框」= 假绿）。
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
            // 🆕 2026-10-25（`X9` · 授权改动）：战术卡落点（跨侧）+ 输入闸（硬闸 / 回放档）
            E_DropSide();
            F_InputGate();
            G_ReplayDrag();
            // 🆕 2026-10-25（`X19/P1`）：手牌文字**量测通道**（探针的灭自证；读卡表）
            H_HandTextMeasure();
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
    //   · 🔴 **2026-10-26（`G21`）就地订正（铁律 5）—— 上面那条「最终判据 = 用户实拍」的结论【已被推翻】**：
    //     `W11` 那版（`CardsCardRect` = 格高 − `CardsCounterH`、卡只占条顶以上）**不是原版长相**。
    //     2026-10-25 裁定（判据 = prefab 现读 `Collection Card/CardUI/2DCard`：350×512 原型里
    //     y **6.2 → 505.8**，上下各 6.2 ⇒ **在整格里居中**、高 374.8）：
    //       · `CardsCardRect(cell)` == **整格**（`CollectionWindow.cs:278-293` 现在就是 `return cell;`）；
    //       · 卡体按原版公式缩放后在整格里**居中** ⇒ 上下各 ≈4.6 px；
    //       · ⇒ 卡底**压住张数条 ≈33.6 px**，**那是原版长相**（张数条画在卡下缘之上）。
    //     ⚠️ `W11` 当时量到的「重叠」根因是**缩放口径**（按整格反解、没有公式给的居中），
    //       不是「卡不该压条」；用户 2026-10-18 报的那一版**重叠得更多**（卡按整格撑满）。
    //
    // 这一组钉的是**口径本身**（`Shell/CollectionWindow.cs` 那条 public 几何 + 原版缩放公式），不是在断实现：
    //   🧨 把 `CardsCardRect` 改回「格高 − 张数条」（`W11` 那版）⇒ ① ② ③ 一起变红。
    static void D_InitialCardScale()
    {
        // 一格（相对坐标即可，两条几何都只吃相对量）
        var cell = new PxRect(0f, 0f, CollectionWindow.CardsCellW, CollectionWindow.CardsCellH);
        var cardRect = CollectionWindow.CardsCardRect(cell);
        var barRect = CollectionWindow.CardsCounterRect(cell);
        float barH = cell.y2 - barRect.y1;             // 张数条占掉的高度（原版锚点 `CardsCntMaxY`）= 38.272

        Check(Mathf.Abs(cardRect.x1 - cell.x1) < 1e-3f && Mathf.Abs(cardRect.y1 - cell.y1) < 1e-3f
              && Mathf.Abs(cardRect.x2 - cell.x2) < 1e-3f && Mathf.Abs(cardRect.y2 - cell.y2) < 1e-3f,
              $"★① 卡体**位置框 = 整格**（`CardsCardRect(cell)` 逐字段等于 `cell`：{cardRect}）"
            + "｜判据 = prefab `Collection Card/CardUI/2DCard` 在 350×512 原型里 y 6.2→505.8（上下各 6.2 ⇒ **居中**）"
            + "｜🧨 改回 `W11` 那版「格高 − 张数条」⇒ 红");

        // ---- ② 卡体**渲染高** = 原版公式（`CollectionCard__OnRectTransformDimensionsChange.c:16-22`）----
        //   `sizeDelta.y × initialCardScale(150) ÷ initialHeight(512)` = 每「卡单位」多少 px
        //   ⇒ 渲染高 = `CardView.Height(3.3313) × (CardsCellH × 150/512 ÷ 108) × 108` = **374.77 px**。
        //   ⚠️ `÷108 ×108` 那两步是「px/卡单位 → 世界缩放 → px」的换算，⛔ 别把它当成我们另外挑的数。
        float cardPx = CardView.Height * (CollectionWindow.CardsCellH * (150f / 512f) / 108f) * 108f;
        CheckClose(cardPx, 374.77f, 0.05f,
                   "★② 卡体渲染高 = 原版公式给的 **374.77 px**（`3.3313 × 384 × 150/512`）"
                 + "｜🧨 把 `150/512` 换成「按整格反解」（`W11` 那版）⇒ 卡高变 384 ⇒ 红");

        CheckClose(cell.H - cardPx, 9.23f, 0.05f,
                   "★②b 卡高与格高的差 = **9.23 px**（上下各 ≈4.61，**居中**）"
                 + "｜判据 = prefab `2DCard` 上下各 6.2/512 ⇒ 折成 384 格 = 4.65（差 0.04 来自 `CardView.Height` 的取整）"
                 + "｜🧨 卡撑满整格 ⇒ 差 0 ⇒ 红");

        // ---- ③ 卡底压进张数条 **≈33.6 px** —— 🔴 **这是原版长相，不是缺陷** ----
        //   卡在整格里居中 ⇒ 卡底离格底 ≈4.61；而张数条的顶在格底往上 38.272
        //   ⇒ 卡底越过条顶 38.272 − 4.61 = **33.66 px**（原版张数条就画在卡下缘之上）。
        float margin = (cell.H - cardPx) * 0.5f;       // 居中 ⇒ 上下各这么多
        float overlap = barH - margin;                 // 卡底越过「条顶」多少
        CheckClose(overlap, 33.66f, 0.2f,
                   $"★③ 卡底压进张数条 **{overlap:F2} px**（条高 {barH:F2} − 居中下边距 {margin:F2}）"
                 + " —— 🔴 **这是原版长相**（判据 = prefab `CardUI/2DCard` 的 y 区间 vs `/Counter` 的上锚点）"
                 + "｜🧨 为了「不压条」把卡缩到条顶以上（`W11` 那版）⇒ 重叠变 0 ⇒ 红");
    }

    // ==================================================================
    //  E · 战术卡落点：**只认卡面要的那一侧**（跨侧一律拒，且**不回落**）
    // ==================================================================
    //
    // 判据（一手）：引擎的目标侧由**卡面正文**定 —— `Core/EffectResolver.cs:1864`
    //   `spec.Side == "enemy" ? 1 - p : p`，而 `targetSlot` 只是**格号**
    //   ⇒ 「指针压在哪块棋盘」与「效果落到哪一侧」本来是两件无关的事。
    // 病根（改前，`X9` / `AUD8` #4）：`CardInteraction.DropReject` 先试敌方半场、再**回落**我方半场
    //   ⇒ 友方目标卡丢在**敌人**身上，只要我方同号格有个合法友军就判「合法」⇒ 效果加到自己人身上、
    //   指针下的敌人毫无反应（反向亦然），而且**零提示**。
    // 现在：`want = EffectText.PickSide(Parse(desc))`；只试 `want` 那一侧，错侧 `EngineRefused` 且不回落。
    //
    // ⚠️ **本组是 E8 里唯一读卡表的一组**（`CardDatabase.Load()`）—— `TacticTargetSide` 的判据就是卡面正文。
    //    读不到卡表 ⇒ `Check(false)` 收场，⛔ **不跳过**（跳过 = 假绿）。
    // ⚠️ 引擎那一半用**替身**（`CanDropAtSlot`）按「目标侧那一格有没有单位」模拟 —— `DropReject` 里
    //    被测的是「该试哪块棋盘 / 要不要回落」，那一段与引擎无关。**真实引擎链路**的复核在
    //    `Editor/BattleScene.cs` §10（`X9-A/B/C`：真实卡 + 真实 `CanPlayTactic`）。
    // ⚠️ 两条行心刻意分开（0.30 / 0.70），`TryResolveSlot` 的 y 容差（0.09）才分得开两块棋盘；
    //    两侧都放一个单位在 `k=3` —— **那正是旧口径会错打的条件**。
    //
    // 灭自证：把 `DropReject` 里 `if (want.Length > 0) { … }` 整段删掉（= 回到旧口径）⇒
    //   ★A / ★B 变成 `Ok` ⇒ **红**；★C / ★D 仍绿（旧口径同侧也收）—— A/B 与 C/D **结构互斥**。
    static void E_DropSide()
    {
        var root = new GameObject("E8_E_dropside");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var pBoard = root.AddComponent<BoardLayout>();
            var eBoard = root.AddComponent<BoardLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            it.board = pBoard;
            it.foeBoard = eBoard;
            it.SetRightButtonHeldForTest(false);
            it.SetBattleStateBlockedForTest(false);     // 本组只验落点判据 ⇒ 闸全放行
            pBoard.lineY = 0.30f;
            eBoard.lineY = 0.70f;
            const int k = 3;
            // 替身引擎：按**卡面那一侧**看「这一格有没有单位」。两侧 k 都有人 ⇒ 与旧口径的错打条件一致。
            it.CanDropAtSlot = (slot, card) =>
            {
                string side = CardInteraction.TacticTargetSideForTest(card);
                if (side.Length > 0) return slot == k;   // enemy / own / any 都只看同一格
                return true;                             // 不需要目标的战术卡：落哪都算数
            };

            // ---- 样卡（全部走真实卡表：`artId` → `CardDef` → `PickSide`）----
            var pool = RuleEngine.CardDatabase.Load();
            Check(pool != null && pool.Count > 0, "前提：卡表读得到（`CardDatabase.Load()` 非空）");
            RuleEngine.CardDef friendDef = null, enemyDef = null, noneDef = null;
            if (pool != null && pool.Count > 0)
            {
                friendDef = RuleEngine.CardDatabase.Find(pool, "Dance of Death");     // `Give Shuriken 2 to a friendly unit`
                // 「不需要目标」那一张**按判据现挑**（不硬写卡名：卡面正文一改，硬写的那张会悄悄变成「要目标」
                //   ⇒ ★D 会红得莫名其妙）。条件 = 战术卡 · 能进卡组 · **正文解析得出来** · `PickSide` 为空。
                foreach (var c in pool)
                {
                    if (c == null || c.Type != "tactic" || c.IsUnit) continue;
                    if (RuleEngine.DeckRules.CannotAddToDeck(c)) continue;
                    if (!RuleEngine.EffectText.IsFullyParsed(c.Desc)) continue;
                    var opsN = RuleEngine.EffectText.Parse(c.Desc, out _, out _);
                    if (RuleEngine.EffectText.PickSide(opsN).Length > 0) continue;
                    noneDef = c; break;
                }
                foreach (var c in pool)
                {
                    if (c == null || c.Type != "tactic" || c.IsUnit) continue;
                    if (RuleEngine.DeckRules.CannotAddToDeck(c)) continue;
                    var opsE = RuleEngine.EffectText.Parse(c.Desc, out _, out _);
                    var t = RuleEngine.EffectText.PickTarget(opsE);
                    if (t == null || t.Side != "enemy" || t.Kind != "unit") continue;
                    bool allDeal = opsE.Count > 0;
                    foreach (var o in opsE) if (o.Verb != "deal") allDeal = false;
                    if (!allDeal) continue;
                    enemyDef = c; break;
                }
                // 友方那一张也留一条兜底（同名/改名时别让整组静默跳过）
                if (friendDef == null)
                    foreach (var c in pool)
                    {
                        if (c == null || c.Type != "tactic" || c.IsUnit) continue;
                        if (RuleEngine.DeckRules.CannotAddToDeck(c)) continue;
                        var opsF = RuleEngine.EffectText.Parse(c.Desc, out _, out _);
                        if (RuleEngine.EffectText.PickSide(opsF) != "own") continue;
                        friendDef = c; break;
                    }
            }
            Check(friendDef != null && enemyDef != null && noneDef != null,
                  "前提：三张样卡都查得到（友方目标 / 敌方目标 / 无目标）"
                + $"（friend={(friendDef != null)} · enemy={(enemyDef != null)} · none={(noneDef != null)}）");
            if (friendDef == null || enemyDef == null || noneDef == null) return;

            var vFriend = CardView.Create(root.transform, BattleDriver.ToCardData(friendDef, friendDef.Faction),
                                          "E8_E_friend", CardFace.Hand);
            var vEnemy = CardView.Create(root.transform, BattleDriver.ToCardData(enemyDef, enemyDef.Faction),
                                         "E8_E_enemy", CardFace.Hand);
            var vNone = CardView.Create(root.transform, BattleDriver.ToCardData(noneDef, noneDef.Faction),
                                        "E8_E_none", CardFace.Hand);
            made.Add(vFriend); made.Add(vEnemy); made.Add(vNone);

            // ---- 前提：两个探针点各自**只**命中自己那块棋盘（否则下面的判别力全是假的）----
            Vector3 ownPt = pBoard.SlotPosition(k), foePt = eBoard.SlotPosition(k);
            int sOwn, sFoe;
            Check(pBoard.TryResolveSlot(ownPt, out sOwn) && sOwn == k, "前提：我方探针命中我方槽 " + k);
            Check(!eBoard.TryResolveSlot(ownPt, out sFoe), "前提：我方探针**不**命中敌方棋盘（两条行心分开）");
            Check(eBoard.TryResolveSlot(foePt, out sFoe) && sFoe == k, "前提：敌方探针命中敌方槽 " + k);
            Check(!pBoard.TryResolveSlot(foePt, out sOwn), "前提：敌方探针**不**命中我方棋盘");

            int slot; BoardLayout which; string why;
            string sideF = CardInteraction.TacticTargetSideForTest(vFriend);
            Check(sideF == "own", $"★ X9-E0：友方目标战术卡的侧判 = **own**（实得「{sideF}」）"
                + "｜🧨 按 `CardData.id`（= 英文卡名）去查 id 索引 ⇒ 恒查不到 ⇒ 这条红");

            int kA = it.DropRejectKindForTest(foePt, vFriend, out slot, out which, out why);
            Check(kA == (int)CardInteraction.DropRejectKind.EngineRefused,
                  $"★ X9-A：友方目标卡拖到**敌方**那个单位上 ⇒ 必须**拒**（实得 {kA}；{why}）"
                + "｜旧口径在这一格会判**合法**并把效果加到自己同号格（真机症状 = 敌人毫无反应）"
                + "｜🧨 删掉侧比对（回落那条老路）⇒ 变 `Ok` ⇒ 红");

            int kB = it.DropRejectKindForTest(ownPt, vEnemy, out slot, out which, out why);
            Check(kB == (int)CardInteraction.DropRejectKind.EngineRefused,
                  $"★ X9-B（双向之二）：敌方目标卡拖到**我方**那个单位上 ⇒ 必须**拒**（实得 {kB}；{why}）"
                + "｜🧨 同上 ⇒ 旧口径会去打敌方同号格 ⇒ 红");

            int kC1 = it.DropRejectKindForTest(ownPt, vFriend, out slot, out which, out why);
            Check(kC1 == (int)CardInteraction.DropRejectKind.Ok && which == pBoard,
                  $"★ X9-C1：友方目标卡拖我方目标 ⇒ **收**，且 `which` = 我方棋盘（实得 kind={kC1} · "
                + $"which={(which == pBoard ? "pBoard" : "别的")} · 槽 {slot}；{why}）");
            int kC2 = it.DropRejectKindForTest(foePt, vEnemy, out slot, out which, out why);
            Check(kC2 == (int)CardInteraction.DropRejectKind.Ok && which == eBoard,
                  $"★ X9-C2：敌方目标卡拖敌方目标 ⇒ **收**，且 `which` = 敌方棋盘（实得 kind={kC2} · "
                + $"which={(which == eBoard ? "eBoard" : "别的")} · 槽 {slot}；{why}）"
                + "｜🧨 把 `wantEnemy` 取反 ⇒ C1/C2 同时红、A/B 仍红 ⇒ 一次抓住「写反」这一档");

            string sideN = CardInteraction.TacticTargetSideForTest(vNone);
            int kD = it.DropRejectKindForTest(foePt, vNone, out slot, out which, out why);
            Check(sideN.Length == 0 && kD == (int)CardInteraction.DropRejectKind.Ok,
                  $"★ X9-D：**不需要目标**的战术卡侧判 = 空串、且拖到敌方半场也**收**（side=「{sideN}」· kind={kD}）"
                + "｜🧨 把「不需要目标」也当成「只认我方」⇒ 红（那就是一刀切）");

            // 边界：侧判据只服务战术卡 —— **单位卡**必须回空串（否则 `DropReject` 会把单位卡也按侧限制）
            var vUnit = CardView.Create(root.transform, CardData.Placeholder(1), "E8_E_unit", CardFace.Hand);
            made.Add(vUnit);
            Check(CardInteraction.TacticTargetSideForTest(vUnit).Length == 0,
                  "★ X9-E4（边界）：**单位卡**的侧判 = 空串（侧判据只服务战术卡）"
                + "｜🧨 把 `TacticTargetSide` 开头的 `c.Data.isUnit` 早退删掉 ⇒ 单位卡也会被判侧 ⇒ 红");
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  F · 输入闸：硬闸（模态窗 / 选目标 / 打法选择器 / 终局）⇒ 抓不起来 + **打断已经拖起来的那张**
    // ==================================================================
    //
    // 判据（一手）：原版起拖闸 = `CardScript__OnTouchDrag.c:17-23` 的 `UIstate == 1`（＋全屏压暗层）；
    //   我们这一层读裸指针、不经过 EventSystem ⇒ 那两道理所当然的闸原本**一条都不成立**
    //   （真机症状：选牌面板 / 设置 / 日志 / 展示窗 开着还能把牌打出去）。
    // ⚠️ 与 `Frozen`（掉线 / 演出未收尾）**不是一回事**：那一档原版也是整帧 `return`、**拖拽态留着**；
    //   本档必须**收回**（`AbortDrag`），否则「窗关掉后松手」照样结算 —— 那正是改前的真机症状。
    //
    // 灭自证：★F 与 ★E-2 结构互斥 —— `AbortDrag` 删掉只留 `return` ⇒ ★F 红（`Dragging` 还在）、
    //   E-1..E-2 仍绿；把整个闸去掉 ⇒ E-1 红、E-2/F 绿。
    static void F_InputGate()
    {
        var root = new GameObject("E8_F_gate");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var bd = root.AddComponent<BoardLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            it.board = bd;
            it.SetRightButtonHeldForTest(false);
            layout.animateRelayout = false;
            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 3), "E8_F_" + i, CardFace.Hand));
            const int n = 3;
            it.SetCards(made, false);
            Vector3 p0 = layout.SlotPosition(0, n);
            Step(CardTween.RelayoutDuration * 2f);      // 🔴 夹具前提：把上一把重排的闸推完（否则 `HoveredCard` 恒 null）

            // ① 硬闸开着 ⇒ 按下不抓、这一帧被吞
            it.SetBattleStateBlockedForTest(true);
            bool swallowed1 = it.SimulateTickForTest(p0, true, false);
            Check(swallowed1 && !it.IsDragging,
                  "★ X9-E-1：硬闸（= 模态窗 / 选目标 / 打法选择器 / 终局）开着时按下去 ⇒ 这一帧被吞且**抓不起来**"
                + "｜🧨 把 `BlockedThisFrame` 里那道 `BattleStateBlocked()` 删掉 ⇒ 红");

            // ② 对照：闸一放开，同一按**必须**抓得起来（与 ① 结构互斥 ⇒ 防「一刀切关死」）
            it.SetBattleStateBlockedForTest(false);
            bool swallowed2 = it.SimulateTickForTest(p0, true, false);
            Check(!swallowed2 && it.IsDragging,
                  "★ X9-E-2（对照）：闸一放开同一按就抓得起来 —— 🧨 ①若写成「永远禁」⇒ 红");

            // ③ 打断「已经拖起来」的那张
            Check(it.Dragging != null, "前提：此刻手里攥着一张");
            it.SetBattleStateBlockedForTest(true);
            bool swallowed3 = it.SimulateTickForTest(p0, false, false);
            Check(swallowed3 && !it.IsDragging,
                  "★ X9-F：闸落下的那一帧，**正在拖**的那张被**收回手牌**（`Dragging == null`）"
                + "｜🧨 把 `BlockedThisFrame` 里那句 `AbortDrag(...)` 删掉、只留 `return` ⇒ 红"
                + "（牌会留在手里 ⇒ 窗关掉后松手照样出牌 = 改前的真机症状）");
            Check(it.HandCardCountForTest == n, "★ X9-F2：收回之后手牌张数不变（" + n + " ⇒ " + it.HandCardCountForTest + "）");
            it.SetBattleStateBlockedForTest(null);
            Step(CardTween.RelayoutDuration * 2f);
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  G · 回放档：**只禁拖**，轻点看牌照旧允许（⛔ 不许一刀切）
    // ==================================================================
    //
    // 判据（一手）：`CardScript__OnTouchDrag.c:17-23` 的 `matchType != 0xA0` ⇒ **禁的是「拖出去打」**；
    //   而 `OnTouchDown` / `OnTouchUpAsButton` 全文**没有** 0xA0 闸，`CanDisplayHandCard`
    //   （`dc/BattleManager__CanDisplayHandCard.c:7-11`）也只要求 `cardState==1 && UIstate==1`
    //   ⇒ **回放里轻点手牌看牌是允许的**。整段关掉 = 把这一条一起关死。
    //
    // 灭自证：把 `ReplayDragBlocked()` 改成恒 `false`（= 取消回放档）⇒ ★G-1 红（按一下就抓起来了）；
    //   而 ★G-2 仍绿 —— 这正说明 G-2 抓的是「**一刀切关死**」那一档（在 `BlockedThisFrame` 里加回放
    //   ⇒ ★G-2 红），两条各管一头，互不替代。
    static void G_ReplayDrag()
    {
        var root = new GameObject("E8_G_replay");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var bd = root.AddComponent<BoardLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            it.board = bd;
            it.SetRightButtonHeldForTest(false);
            layout.animateRelayout = false;
            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 6), "E8_G_" + i, CardFace.Hand));
            const int n = 3;
            it.SetCards(made, false);
            Vector3 p0 = layout.SlotPosition(0, n);
            Step(CardTween.RelayoutDuration * 2f);

            int taps = 0;
            System.Action<CardView> onTap = c => taps++;
            it.OnTapped += onTap;
            it.SetReplayDragBlockedForTest(true);

            // ① 拖：过阈值也**不许抓**（原版 0xA0 闸）
            bool swallowed = it.SimulateTickForTest(p0, true, false);
            Check(!swallowed && !it.IsDragging,
                  "前提：回放档是**软闸**（不吞整帧）—— 按下只记候选、不抓（" + (!swallowed) + " / "
                + (!it.IsDragging) + "）");
            // 朝**上**拖（`HandDragPastThreshold` 还有一道 ≤65° 的方向闸，横向挪会被它先拦掉）
            Vector3 up = p0 + new Vector3(0f, LayoutSpace.VisibleHeight * 0.2f, 0f);
            it.SimulateTickForTest(up, false, false);
            Check(!it.IsDragging,
                  "★ X9-G-1：回放里**拖**被禁（过了阈值也不抓 —— 原版 `matchType != 0xA0`）"
                + "｜🧨 把 `ReplayDragBlocked()` 改成恒 `false`（= 取消回放档）⇒ 按一下就抓起来 ⇒ 红");
            it.SimulateTickForTest(up, false, true);
            Check(taps == 0, "★ X9-G-1b：拖过之后松手**不算轻点**（不开卡面展示窗；实得 " + taps + " 次）");

            // ② 正例：轻点**必须仍然可以**（原版 `OnTouchDown` / `OnTouchUpAsButton` 没有 0xA0 闸）
            it.SimulateTickForTest(p0, true, false);
            it.SimulateTickForTest(p0, false, true);
            Check(taps == 1,
                  $"★ X9-G-2（正例）：回放里**轻点**手牌照旧开卡面展示窗（`OnTapped` 实得 {taps} 次）"
                + "｜🧨 把回放档做成「整段关死」（例如在 `BlockedThisFrame` 里 `return true`）⇒ 这条红");

            it.OnTapped -= onTap;
            it.SetReplayDragBlockedForTest(null);
            Step(CardTween.RelayoutDuration * 2f);
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  H · 手牌文字的**量测通道是活的**（`X19/P1` 探针的灭自证）
    // ==================================================================
    //
    // 🔴 为什么要它（证据 → `_tmp_view/shots/2026-10-25_第七十五会话/findings.md` F3++）：
    //   真机手牌**卡名和正文都越框**（`b14_B_hand_zoom.png`：「圣典：恢复 3 点生命值」整行冲出右边框，
    //   压到隔壁卡；「军团议会」三行超出卡体下沿），而**自检里复现不出来**。
    //   两种可能：① 自检量的与真机画的**不是同一个量**；② 真机那条路**根本没走到拟合**。
    //   这一组先把「尺子」钉住 —— 否则「越框=0」这个结论本身不可信：
    //     ★H-1 **通道活**：`CardView.InkBoxLocal` 在这张真卡上真的量得到字墨（非 0）；
    //     ★H-2 **注入一次越框，同一个量必须报出来**（检测器有牙）；★H-2b 还原后墨宽也还原
    //          （防「量的是缓存 ⇒ 两个方向都不动」那种假绿）；
    //     ★H-3 生产口径下这张真卡四边都装得下（宽/高/字号只缩不放/没有待补重排）。
    //   ⛔ 本组只在**自己建的临时卡**上写 `fontSize`，收尾 `DestroyImmediate` ⇒ 不碰生产状态。
    //   ⚠️ 本组与 E 组同属「读卡表」的两组（`CardDatabase.Load()`）；读不到 ⇒ `Check(false)`，⛔ 不跳过。
    //
    // 灭自证：把 ★H-2 的注入两行删掉（或把判据改成「恒不越框」）⇒ ★H-2 红；
    //   把 `InkBoxLocal` 换成恒 `false` ⇒ ★H-1 红（★H-3 也会红：墨宽恒 n/a）。
    static void H_HandTextMeasure()
    {
        var root = new GameObject("E8_H_handtext");
        var made = new List<CardView>();
        try
        {
            var pool = RuleEngine.CardDatabase.Load();
            Check(pool != null && pool.Count > 0, "前提：卡表读得到（`CardDatabase.Load()` 非空）");
            if (pool == null || pool.Count == 0) return;

            // 挑「手牌上最容易被撑爆」的那张**单位卡**（卡名 + 效果文字，取**真正画在卡面上**的那一份）。
            // ⚠️ 只挑单位卡：单位档走态 1/2，效果文字的高度预算可以直接用 `FaceDescBand01` 现算
            //    （战术态 4 的 `DescTacticSmall` 预算是私有常量，这里不去猜它）。
            RuleEngine.CardDef def = null;
            CardData best = default(CardData);
            int bestLen = -1;
            foreach (var c in pool)
            {
                if (c == null || !c.IsUnit) continue;
                var dd = BattleDriver.ToCardData(c, c.Faction);
                int len = (dd.title != null ? dd.title.Length : 0) + (dd.keywords != null ? dd.keywords.Length : 0);
                if (len <= bestLen) continue;
                bestLen = len; def = c; best = dd;
            }
            Check(def != null, "前提：卡表里挑得出一张单位卡");
            if (def == null) return;

            var v = CardView.Create(root.transform, best, "E8_H_best", CardFace.Hand);
            made.Add(v);
            Check(v.TitleText != null && v.KeywordsText != null,
                  "前提：卡名/效果文字两层都建出来了（`TmpFont` 可用）");
            if (v.TitleText == null || v.KeywordsText == null) return;

            // 框宽：**与生产同一口径、且与探针同一个口**（`BattleDriver.HandLayerBoxWidth`）。
            // 🔴 **2026-10-25 修复（H2 假绿）**：原来这里读 TMP 的 `rectTransform.sizeDelta.x` ——
            //    TMP **出厂就是 `(20, 5)`**，而**不折行**的卡名层从没被写过折行宽 ⇒ 量到 **20**（哨兵），
            //    `墨 2.46 ≤ 20` 必然「没越框」⇒ H2 这条灭自证**当时没有任何判别力**（H3b 同样是空绿）。
            //    ⇒ 现在一律走那个共用口，**并且先断「框量到了真值」**（取不到就红，⛔ 不拿兜底值继续判）。
            float nameBox = BattleDriver.HandLayerBoxWidth(v, desc: false);
            float descBox = BattleDriver.HandLayerBoxWidth(v, desc: true);
            Check(nameBox >= 1.0f && nameBox <= 3.0f,
                  $"★ X19-H0a（前提）卡名框宽量到的是**真值**：{nameBox:F4}（期望 1.0–3.0）"
                + "｜🧨 读 TMP 出厂 `sizeDelta` ⇒ 得到 **20**（哨兵）⇒ 所有「越框」判断恒真 ⇒ 红");
            Check(descBox >= 1.0f && descBox <= 3.0f,
                  $"★ X19-H0b（前提）效果文字框宽量到的是真值：{descBox:F4}（期望 1.0–3.0）"
                + "｜🧨 同上");
            float fc, fb;
            CardView.FaceDescBand01(best.isUnit, CardView.FaceShowsRace(best), out fc, out fb);
            float descH = Mathf.Max(0.2f, fb * CardView.Height);

            Vector2 inkT, cT, inkD, cD;
            bool okT = CardView.InkBoxLocal(v.TitleText, out inkT, out cT);
            bool okD = CardView.InkBoxLocal(v.KeywordsText, out inkD, out cD);

            Check(okT && inkT.x > 1e-3f && okD && inkD.x > 1e-3f,
                  $"★ X19-H1 量测通道是活的：卡名墨 {inkT.x:F4}×{inkT.y:F4}（ok={okT}）· "
                + $"效果墨 {inkD.x:F4}×{inkD.y:F4}（ok={okD}）"
                + "｜🧨 `InkBoxLocal` 恒 false / 读错层 ⇒ 红（那样所有「越框=0」都是瞎的）");

            // ★H-2 注入越框 ⇒ 同一把尺子必须报出来
            float nominalTitleFs = v.TitleText.fontSize;
            v.TitleText.fontSize = nominalTitleFs * 2f;
            v.TitleText.ForceMeshUpdate();
            Vector2 injInk, injC;
            bool injOk = CardView.InkBoxLocal(v.TitleText, out injInk, out injC);
            Check(injOk && injInk.x > nameBox * 1.05f,
                  $"★ X19-H2（灭自证）卡名字号 ×2（{nominalTitleFs:F4}→{v.TitleText.fontSize:F4}）后，"
                + $"同一个量必须报越框：墨 {injInk.x:F4} vs 框 {nameBox:F4}（ok={injOk}）"
                + "｜🧨 检测器写成恒「没越框」⇒ 红");

            // ★H-2b 还原：同一把尺子**两个方向都要跟得动**
            v.TitleText.fontSize = nominalTitleFs;
            v.TitleText.ForceMeshUpdate();
            Vector2 backInk, backC;
            bool backOk = CardView.InkBoxLocal(v.TitleText, out backInk, out backC);
            Check(backOk && Mathf.Abs(backInk.x - inkT.x) < 0.02f,
                  $"★ X19-H2b 字号还原后墨宽也还原（{inkT.x:F4} → {backInk.x:F4}）"
                + "｜🧨 上一格若因为「量的是缓存」而绿，这一格会红");

            // ★H-3 生产口径（真卡 · 真 `CardFace.Hand` · 激活档）
            Check(v.TitleText.fontSize <= v.NameFontSize * 1.001f,
                  $"★ X19-H3a 卡名字号只缩不放：fs {v.TitleText.fontSize:F4} ≤ 名义 {v.NameFontSize:F4}"
                + "｜🧨 `FitToWidth` 改成会放大 ⇒ 红");
            Check(backInk.x <= nameBox * 1.001f,
                  $"★ X19-H3b 卡名不越框：墨 {backInk.x:F4} ≤ 框 {nameBox:F4}"
                + "｜🔴 这一格**红 = 真机那条「卡名越框」在批处理里也复现了**（是本组要抓的缺陷，不是本组写错）");
            Check(inkD.x <= descBox * 1.001f,
                  $"★ X19-H3c 效果文字不越**宽**：墨 {inkD.x:F4} ≤ 折行宽 {descBox:F4}"
                + "｜🔴 红了就是真机 `b14_B_hand_zoom.png` 那条在批处理里复现（同上，是真发现）");
            Check(inkD.y <= descH * 1.02f,
                  $"★ X19-H3d 效果文字不越**高**：墨高 {inkD.y:F4} ≤ 预算 {descH:F4}"
                + "｜判据 = 生产同一个 `FaceDescBand01` 预算（⛔ 断言里不另算一份）");
            Check(!v.TextNeedsRefit,
                  "★ X19-H3e 这张卡没有留下「待补重排」（`TextNeedsRefit=0`）—— 拟合这条路真的跑到了"
                + "｜🧨 建卡时父链不活跃 / 拟合早退 ⇒ 红（`X17` 那条 `OnEnable` 兜底也兜不住时就现形）");
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
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
