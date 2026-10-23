# J3 · `Hand/**` 三处口径差（旋转跳 / 重排动画闸 / 松手回弹）

> 执行代理 **J3** · 2026-10-24
> 白名单：`CardPresentation/Hand/**` ＋ 本目录。⛔ 未碰 `Core/**`（另一路在写）、未碰 `Editor/**`、未动 git、未跑 Unity。
> 改完 `.cs` 跑过 **`TMPDIR=/tmp/wf_j3 bash d:/4/Unity/工具/typecheck.sh`** ⇒ **运行时 0 / 编辑器 0**。

---

## 〇、结论速览

| # | 任务 | 原版判据 | 结论 | 处置 |
| --- | --- | --- | --- | --- |
| 1 | 旋转用 `DOLocalRotate` | `CardScript__UpdateMinionInHandPosition.c`（`DOLocalRotate` 在 `:33`） | 原版**位置用世界（`DOMove` `:52`）、角度用局部（`DOLocalRotate` `:33`）、缩放用局部（`DOScale` `:43`）**；我们是 `DORotate`（世界） | ✅ **改**（新增 `HandPoseTween`，`Place` / `PickUp` 两处改用它） |
| 2 | 手牌重排的动画闸 | `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c`（`isDoingAnim` + `Toggle(false)` / `SetColliderInHand()`） | 原版是**两件事**：`+0x61` 是**状态位**（只在 selectedCard == -1 时置）、**真正挡输入的是卡碰撞体开关**（整把牌 0.2 s 内点不到） | ✅ **实现**（交互层闸：`SetCards` / `Release` / `EndPressCandidate` 三条路开闸） |
| 3 | 松手回弹 | `BattleManager__Update.c:210/220-223/246` → `MoveBackToHand` → `MoveCardsInHandToNewIndex` + `HandCardReleased` → `MoveCardsInHandToPosition(-1, null)` | **找到了**：原版**没有回弹专用补间**，回弹就是**普通的手牌重排**（0.2 s、位置+角度+缩放、默认 `OutQuad`） | ✅ **按原版改**（删掉自造的 0.22 s 序列，交给重排） |

三条里 **3 是「判据找到了并改掉」**（不是 A26 当时判的「查不到」）—— 判据在 `BattleManager.Update` 的 **case 2 松手那一支**，A26 §4.1 建议去查的那两处**方向是对的**。

---

## 一、判 1：旋转那一跳 = `DOLocalRotate`（⛔ 不是 `DORotate`）

### 1.1 原版形态（逐句，`d:/2/tools/decomp_full/CardScript__UpdateMinionInHandPosition.c`）

```
if (cardState(+0x228) == 1) {                                   // 只在「手牌常态」
  Kill(+0x340);  uVar1 = DOLocalRotate(transform, euler, param_4, 0, 0);  save → +0x340
  Kill(+0x348);  uVar1 = DOScale(transform, GetCardInHandScale(), param_4, 0); save → +0x348
  Kill(+0x2d0);  uVar1 = DOMove(transform, pos, param_4, 0, 0);            save → +0x2d0
}
```
- 角度**先** `Quaternion.Internal_ToEulerRad` × `Rad2Deg`（`DAT_1834b2e98`）**再** `Internal_MakePositive`
  ⇒ 传给 `DOLocalRotate` 的是**局部欧拉角的度**；
- 位置那一跳是 `DOMove`（**世界**），⛔ 不是 `DOLocalMove`；
- 缩放 `DOScale` 写 `localScale`；
- `param_4` = 时长，由 `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:76-80` 读
  `globalVars + 0xA4`（`timeToPositionCard` = 0.2 s）喂进来；**全文没有 `SetEase`** ⇒ 三条各自吃
  `DOTween.defaultEaseType`（原版资源实测 = 6 `OutQuad`，见 `A26` §3.1）。

### 1.2 我们原来错在哪

`Core/CardTween.ToPose` 三条 `Join` 是 `DOMove` + **`DORotate`（世界）** + `DOScale`。
父链带旋转时：世界欧拉 `(0,0,θ)` ⇒ 卡的**局部**角变成 `θ − 父件角` —— 原版要的是局部 `θ`。
`CardInteraction.PickUp` 原来更明显：它把 `card.transform.eulerAngles.z`（**世界**角）当目标，
父链一有旋转卡就会被「反着拧」。

### 1.3 改动

- `Hand/HandLayout.cs`：新增 `internal static class HandPoseTween`（文件末尾，与 `HandLayout` 同文件同命名空间
  —— **非 MonoBehaviour，不触发「类名==文件名」那条存根坑**），逐句同形 `CardTween.ToPose`、
  **只把中间那一跳换成 `DOLocalRotate`**；`Place` 改用它（`Ease.Linear` 不变，A26 的「别二次弯曲」结论继续成立）。
- `Hand/CardInteraction.cs`：`PickUp` 改用它，角度实参由 `eulerAngles.z` → **`localEulerAngles.z`**。
- ⚠️ **为什么是第二份实现**：`Core/CardTween.cs` 在本次黑名单里。**收口办法**（交给调度台）：
  把 `CardTween.ToPose` 的中间那一跳也换成 `DOLocalRotate` → 删掉 `HandPoseTween` → `Place`/`PickUp` 改回调用。
  在两处都改完之前 **⛔ 别再加第三份**。
- **行为影响**：今天**零像素变化** —— 手牌卡的父件 = `BattleDriver` 根
  （`BattleScene.cs:40243-40249` 的 `sceneRoot.AddComponent<…>()`；`new GameObject("Battle")` 在原点、
  `identity`、缩放 1；`BattleDriver.SyncHand` 的 `CardView.Create(transform, …)` 挂的就是它）
  ⇒ 局部位姿 ≡ 世界位姿。这一改是**把语义摆正**＋把「父链一旦带旋转就静默走样」堵死。

### 1.4 断言（怎么才会红）—— 建议主对话加在 `Editor/BattleScene.cs`

```csharp
// ① 造一个带 30° 旋转 + 2 倍缩放的临时父件（不动别的状态）
var probe = new GameObject("J3_LocalRotateProbe");
probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 0f, 30f));
probe.transform.localScale = Vector3.one * 2f;

var cv = driver.HandViewAt(0);
var oldParent = cv.transform.parent;
bool oldAnim = it.hand.animateRelayout;
var oldMode = CardTween.Mode;
try
{
    cv.transform.SetParent(probe.transform, worldPositionStays: true);
    it.hand.animateRelayout = true;
    CardTween.Mode = DG.Tweening.UpdateType.Manual;

    it.Relayout();                                  // 全手牌重排（走 HandPoseTween）
    CardTween.Advance(CardTween.RelayoutDuration * 1.2f);

    int n = it.HandCardCountForTest;
    float want = Mathf.DeltaAngle(0f, it.hand.RotationAt(0, n));
    float got  = Mathf.DeltaAngle(0f, cv.transform.localEulerAngles.z);
    Check(Mathf.Abs(Mathf.DeltaAngle(want, got)) < 0.5f,
          $"★ 重排补间写的是【局部】角（要 {want:F2}°，实得局部 {got:F2}°）"
        + " —— 判据 `CardScript__UpdateMinionInHandPosition.c` 的 `DOLocalRotate`；"
        + " 🧨 改回 `DORotate`（世界欧拉）⇒ 局部角 = 目标 − 父件 30° ⇒ 差 30° ⇒ 红");
}
finally
{
    cv.transform.SetParent(oldParent, worldPositionStays: true);
    it.hand.animateRelayout = oldAnim;
    CardTween.Mode = oldMode;
    Object.DestroyImmediate(probe);
}
```

**为什么它「结构上不可能两边一起绿」**：位置/缩放两条在 `DORotate` 与 `DOLocalRotate` 下**完全一样**，
只有**局部角**这一格分叉；父件带 30° ⇒ 两种写法的终值差 30°，而断言比的是**卡自己的 `localEulerAngles`**。
⚠️ 临时改父件会动 `HitByCollider` 的世界位置，所以断言只读**局部角**、并且 `finally` 里必须还原父件。

---

## 二、判 2：手牌重排的「动画闸」

### 2.1 原版的两个闸（**是两件事**，别合并）

判据 = `d:/2/tools/decomp_full/PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c`（亲读，行号逐条核过）：

| 闸 | 位置 | 语义 |
| --- | --- | --- |
| **① `isDoingAnim`（`PlayerHand +0x61`）** | 置 1 `:33-35`；`WaitForSeconds(timeToPositionCard)` `:104-106`；置 0 `:134` | `:33` 是 `if (*(int*)(state + 0x30) == -1) *(bool*)(hand + 0x61) = 1;` —— **`+0x30` = `MoveCardsInHandToPosition(int selectedCard, …)` 的 selectedCard 实参** ⇒ **只有「整把重排」才置位**；`CardScript.ShowCardInHand` 传的是**卡序号** ⇒ 不置。读口 `PlayerHand__IsDoingAnim.c` = `return *(bool*)(this + 0x61);` |
| **② 卡碰撞体开关**（**真正挡输入**） | `Toggle(false)` `:44-47`；`SetColliderInHand()` `:126-127` | `*(card + 0x140)` = `CardCollider2D`；`:45` 是虚表 **+0x1F8** 的调用、实参 **0** ⇒ `Toggle(false)`（体 = `Behaviour.set_enabled(boxCollider2D, false)` ＋ 顺手关 `colliderShowInBig`）。等完 0.2 s 后 `SetColliderInHand()` —— **它的末尾就是 `Toggle(true)`**（`CardCollider2D__SetColliderInHand.c` 最后一句 `(**(code**)(*param_1+0x1f8))(param_1,1,…)`）。 |

⇒ **原版语义 = 重排补间这 0.2 s 里，手牌一张都点不到**（时长 = `timeToPositionCard` = 补间时长本身）。

调用方（谁开了这个闸）：
- `PlayerHand.HandCardReleased()` → `MoveCardsInHandToPosition(-1, null)`；
- `PlayerHand.HandCardMovedToNewIndex()` → 改完列表序后再 `(-1, null)`；
- `CardScript.StopShowingCardInHand(restart: true)` → `(-1, null)`；
- 抽牌那三条协程（`_AddDrawnCardToHand_d__37:119` / `_AddCardNotDrawn…d__39:35` / `…AtIndex d__40:36`）→ `(-1, null)`；
- `CardScript.ShowCardInHand` → `(cardIndex, null)` —— **不置 isDoingAnim**，但**仍然会 `Toggle(false)`**。

### 2.2 我们怎么落地（`Hand/**` 内，不改碰撞体）

闸落在**交互层**（我们的命中是 `CardView.HitByCollider` 的常开 collider，没有原版那层开关）：

```
Tween _animGate;                                        // = 原版 WaitForSeconds(timeToPositionCard)
public bool IsDoingAnim => _animGate != null && _animGate.IsActive();
void ArmAnimGate();                                     // DOVirtual.DelayedCall(RelayoutDuration).SetUpdate(CardTween.Mode)
```
- **开闸**：`Relayout(true)`（公开口）—— 由 `SetCards`（手牌列表变了）/ `Release`（松手，两支）/ `EndPressCandidate`（轻点结束）调用；
- **不开闸**：`UpdateHover` / `UpdateDrag`（边拖边让位）/ `PickUp` → `Relayout(false)`
  （原版对应 `ShowCardInHand(selectedCard≠-1)` 与 `DisplayHandWithCardSpace`，**后者根本不碰碰撞体**；
  而且我们的悬停是**连续**鼠标模型，开闸会让指针扫过手牌时每 0.2 s 冻一次）；
- **闸内挡住**：`UpdateHover`（不产生新的 hover）、`PickUp` / `BeginPressCandidate`（起不了拖/按不下）；
  ⚠️ **不挡**已经在进行中的拖拽与松手（原版那道闸也不会在拖拽中途出现）；
- **`hand.animateRelayout == false` 就不开闸**（批处理/演示档：位置当场落 ⇒ 没有「重排期间」；
  ⛔ 否则整套自检会凭空红）。

⚠️ **计时器为什么用 `DOVirtual.DelayedCall` 而不是 `Time.time`**：批处理自检把 `CardTween.Mode` 拨成
`UpdateType.Manual` 后，只有走 DOTween 时间轴的才被 `CardTween.Advance(dt)` 推进；`Time.time` 是墙钟，
喂 dt 推不动它 ⇒ 闸在自检里**验不了**。

⚠️ **顺手补的一处防御**：闸把 `UpdateHover` 冻住的那 0.2 s 里，`_cards` 可能已被 `SetCards` 换短，
而 `_hovered` 还是旧下标 ⇒ 新加 `CardView HoveredCard`（带 `_hovered < _cards.Count` 夹取），
`Update` / `SimulatePress` / `SimulatePressPendingForTest` 三处改用它。

### 2.3 断言（怎么才会红）—— 建议主对话加在 `Editor/BattleScene.cs`

```csharp
bool oldAnim = it.hand.animateRelayout;
var oldMode = CardTween.Mode;
try
{
    it.hand.animateRelayout = true;            // 批处理出厂 false ⇒ 手工打开（闸只在有动画时存在）
    CardTween.Mode = DG.Tweening.UpdateType.Manual;

    it.Relayout();                              // = 原版 MoveCardsInHandToPosition(-1, …)
    Check(it.IsDoingAnimForTest,
          "① 重排开始时闸是开的（原版 isDoingAnim=1 + 每张 Toggle(false)）"
        + " 🧨 删掉 ArmAnimGate ⇒ 红");
    Check(it.AnimGateRemainingForTest > 0.15f && it.AnimGateRemainingForTest <= 0.2f + 1e-3f,
          $"② 闸长 = timeToPositionCard 0.2 s（实得 {it.AnimGateRemainingForTest:F3}）");

    it.SimulateHover(it.hand.SlotPosition(0, it.HandCardCountForTest));
    Check(it.HoveredIndex == -1, "③ 闸内**不产生新的 hover**（原版这 0.2 s 碰撞体是关的）"
        + " 🧨 把 UpdateHover 的闸删掉 ⇒ 红");

    it.SimulatePress(it.hand.SlotPosition(0, it.HandCardCountForTest));
    Check(!it.IsDragging, "★ ④ 闸内按在牌上**抓不起来** —— 判据 MoveNext.c:44-47 的 Toggle(false)"
        + " 🧨 把 PickUp 的闸删掉 ⇒ 红");

    CardTween.Advance(0.1f);
    Check(it.IsDoingAnimForTest, "⑤ 半程（0.1 s）闸还开着 —— 🧨 把时长写成固定小值/立刻关 ⇒ 红");
    CardTween.Advance(0.11f);
    Check(!it.IsDoingAnimForTest, "⑥ 过 0.2 s 闸自己松开 —— 🧨 把闸写成「永远开着」⇒ 红");

    it.SimulateHover(it.hand.SlotPosition(0, it.HandCardCountForTest));
    it.SimulatePress(it.hand.SlotPosition(0, it.HandCardCountForTest));
    Check(it.IsDragging, "（对照）闸一放开照样能抓 —— 🧨 上面 ③④ 若因为「卡压根就不响应」而绿，这条会红");
    it.SimulateRelease(/* 拖回手牌带 */ LayoutSpace.ToWorld(0.20f, HandLayout.DefaultBaselineY));
}
finally { it.hand.animateRelayout = oldAnim; CardTween.Mode = oldMode; }
```

**灭自证的第二半**：④ 与最后那条「对照」是**结构上互斥**的一对 —— 闸写成「永远禁止」④ 绿、对照红；
闸写成「不存在」④ 红、对照绿。

---

## 三、判 3：松手回弹 —— **判据找到了**，按原版改

### 3.1 原版链条（亲读，行号逐条核过 `d:/2/tools/decomp_full/BattleManager__Update.c`）

```
case 2（拖拽中）：
  :208  MinionManager.RemoveSwarm(...)
  :210  CardScript.MoveBackToHand(card)            // ← 只改状态，没有补间
  :220  slot = CardsHorizontalLayout.GetClosestInHandSlot(handLayout, 指针, count)
  :223  PlayerHand.HandCardMovedToNewIndex(card, slot)   // 改列表序 + 起 MoveCardsInHandToPosition(-1, null)
  :243  MinionManager.ReassembleMinions(...)
  :246  PlayerHand.HandCardReleased()                    // **再**起一条 MoveCardsInHandToPosition(-1, null)
```
- `CardScript__MoveBackToHand.c` 全文：`SetSortingLayer(手牌层)` → `StopAnimsInPlay` →
  单位卡 `DissolveMinionCard` → `SetStateInHand()`。**一个 `DOMove` / 一个时长常量都没有**。
- `CardScript__SetStateInHand.c`：卡状态 = 1，`PlayerHand.SetParentOfCard(card)` →
  `Transform.SetParent(handAnchorLayout, worldPositionStays: true)`（`PlayerHand__SetParentOfCard.c`）。**也没有补间**。
- 两条协程最后都落到 `CardScript.UpdateMinionInHandPosition` ⇒ 对**手牌里每一张**（含刚回来的那张）
  开 `DOMove` / `DOLocalRotate` / `DOScale`，**0.2 s**、**不写 `SetEase`**（默认 `OutQuad`）。

⇒ **原版的「回弹」= 普通的手牌重排**（0.2 s、位置+角度+缩放、OutQuad）。**没有专门的回弹补间、也没有 0.22 这个数。**

### 3.2 我们原来的实现（已删）

```csharp
var seq = DOTween.Sequence();
seq.Join(card.transform.DOMove(home, CardTween.SnapBackDuration));         // 0.22（我们挑的）
seq.Join(card.transform.DOScale(handsScale, CardTween.SnapBackDuration));  // 没有角度
CardTween.Use(seq, Ease.OutQuad, card.transform);                          // Sequence 层再弯一次
```
而且这条序列**在同一帧末尾就被 `Relayout()` 的 `DOKill()` 杀掉、换成正确的重排补间**
（`HandLayout.Place` 第一句就是 `c.transform.DOKill()`）⇒ 它**从来没真正跑完过**，纯属白开白杀。

### 3.3 改动

`Hand/CardInteraction.Release` 的 else 支：**删掉那条自造序列**（连同 `home`），回弹完全交给末尾那句
`Relayout(true)` —— 时长 / 形状 / 角度三样一次对齐原版。日志文案同步改成「走手牌重排那条补间」。

`CardTween.SnapBackDuration`（0.22，`Core/CardTween.cs:32`）从此**没有调用方**
（全仓 `grep SnapBackDuration` 只剩 Core 的定义 + 本次两处注释）。
⚠️ `Core/**` 不在本笔白名单 ⇒ **没删**。收口建议（调度台）：删掉那个常量，或至少把它的 doc 从
「原版判据没定位到」改成「**判据已定位**：回弹 = `PlayerHand.MoveCardsInHandToPosition` 的普通重排，
0.2 s / OutQuad；本常量已无调用方」。

### 3.4 断言（怎么才会红）

现有宿主已经覆盖**终态**（`CardBaseDemo.cs:234-248` 的「弹回手牌第 0 位」、`BattleScene.cs:5026` 的
「回弹补间走完」）—— 但**终态对两版都成立**（旧版也会被末尾 `Relayout` 拉到同一个位置）⇒ **它测不出这次改动**。
建议补一条**时长/形状**判别式：

```csharp
// 松手到非法落点之后，只推进【半程】
CardTween.Mode = DG.Tweening.UpdateType.Manual;
it.hand.animateRelayout = true;
it.SimulatePress(...); it.SimulateDrag(nowhere, 1/30f); it.SimulateRelease(nowhere);

Vector3 a = backCard.transform.position;
CardTween.Advance(0.10f);                       // 0.2 s 的半程
Vector3 mid = backCard.transform.position;
CardTween.Advance(0.10f);
Vector3 b = backCard.transform.position;
float p = Vector3.Distance(a, mid) / Mathf.Max(1e-6f, Vector3.Distance(a, b));
Check(Mathf.Abs(p - 0.75f) < 0.05f,
      $"★ 回弹半程走了 **0.75** 的位移（OutQuad 在半程的值；实得 {p:F3}）"
    + " —— 判据 UpdateMinionInHandPosition 三条单补间吃默认 ease=6；"
    + " 🧨 换回 `CardTween.ToPose(…, Ease.OutQuad)`（二次弯曲）⇒ 半程 0.9375 ⇒ 红；"
    + " 🧨 换回旧的 0.22 s 自造序列 ⇒ 半程位点不同 ⇒ 红");
```
（`a` 取的是「松手那一刻卡的当前位置」，所以这条同时钉住了「回弹从当前位置起补、不是瞬移」。）

---

## 四、验证与改动清单

```text
 Unity/MyGame/Assets/CardPresentation/Hand/HandLayout.cs        +69 / −1   (LF，CRLF=0 ⇒ 没翻行尾)
 Unity/MyGame/Assets/CardPresentation/Hand/CardInteraction.cs  +162 / −27   (CRLF，CRLF=LF=1307 ⇒ 没翻行尾)
 资料/普查产出_1023e/J3_Hand口径差.md                              新增
```
（`git diff --numstat` 是 **vs HEAD**，含 `A26` 本轮之前那两处未提交改动。）

- **`TMPDIR=/tmp/wf_j3 bash d:/4/Unity/工具/typecheck.sh`** ⇒ **运行时错误数 0 · 编辑器错误数 0**（最后一次改动之后复跑过一遍）。
- ⛔ 没跑 Unity、没看画面、没动 git。`CardInteraction.cs` 全程 CRLF（1307/1307）、`HandLayout.cs` 全程 LF。

---

## 五、没查清 / 没做的部分（如实）

1. 🔴 **`PlayerHand.IsDoingAnim()` 的消费者查不到**：`decomp_full`（**25,096** 个方法体）里 `grep IsDoingAnim`
   **只命中它自己的定义**（`PlayerHand__IsDoingAnim.c`）⇒ 这份 build 里要么被内联进了没解出来的方法、
   要么是死码。⇒ 我**没有**把它当「原版挡输入」的唯一证据，落地依据是**卡碰撞体开关**（判据更强、可逐句核）。
2. ⚠️ **hover/show 那条路的碰撞体开关没复刻**：`CardScript.ShowCardInHand` 也会触发
   `MoveCardsInHandToPosition(selectedCard=卡序号)` ⇒ 同样 `Toggle(false)` 0.2 s；
   它靠 `StopShowingCardInHand` 里的 `SetCollidersShowBig(false, 2)` → `Toggle(true)` **立刻**补回。
   我们的悬停是连续鼠标模型，机械照搬会「指针扫过手牌时每 0.2 s 冻一次」⇒ **本轮没做**，判据留在代码注释里，
   做法等真机验收 + 用户拍板（这条**不是「不做」**，是**判据在、做法未定**）。
3. ⚠️ **闸的时长有 ≤1 帧的系统差**：原版是「**先**开补间，**再** `WaitForSeconds(0.2)`」——
   补间从下一帧才开始被 DOTween 推，`WaitForSeconds` 从当前帧起算 ⇒ 原版闸可能比补间**早一帧**松开。
   我们 `DelayedCall` 与补间同帧创建（同一个时间轴）⇒ 差 ≤1 帧。**如实记，没为这 1 帧加偏置**。
4. ⚠️ **`HandPoseTween` 是第二份实现**（`Core/CardTween.cs` 黑名单）—— 收口办法见 §1.3，**没做**。
5. ⚠️ **`CardTween.SnapBackDuration` 现在是死常量**（`Core/**` 黑名单，**没删**）。
6. ⚠️ **没跑真机**：闸会让「每次出牌/抽牌后的 0.2 s」手牌不可点（原版如此），
   实际手感（尤其**悬停没开闸**这一档）必须真 Play 再判 —— 建议进 `资料/真Play待验清单.md` 加一条。
7. ⚠️ 原版 `PlayerHand.MoveCardsInHandToPosition` 还有两件我们没做的：`CardScript.SetSortingOrder(名单长度 − 序号 − 1)`
   与 `card + 0x148 = handAnchorLayout`（我们已用渲染队列等价物承担前者，见 `HandLayout.queueTop`）；
   另一件 `ActivateCard(CanPlayCard(...))`（闸松开后**重新点亮可打牌**）我们由 `RefreshHandPlayable` 那条独立链负责 ——
   **两处没在本轮核**「闸松开那一刻**是否同帧**刷新」，可能差一帧。

---

## 六、顺手发现（都在本件范围外，供调度台分流）

1. 🔴 **`HandLayout.Place` 走的还是 Sequence 版 `ToPose`（`DOScale` 直写 `localScale`）⇒ `CardView._baseScale` 在补间期间
   停在起点值**。这正是 `Core/CardView.cs:7840-7860`（`T04`）写明的那个潜伏缺陷（补间一结束，任何一次
   `SetPose` / `SetHighlightScale` 都会把卡弹回旧缩放）。`CardView.TweenPose` 已经是修好的那一版
   （**局部位置 + 局部角度 + `_baseScale` 一起带**），只是手牌这条链还没换过去。
   ⇒ 建议立账：**把 `HandLayout.Place` 整体换成 `CardView.TweenPose`**（本笔只做了「旋转那一跳」这一半，
   没动缩放的所有权，理由是那会顺带改 `_baseScale` 的写入时机 = 另一件事）。
2. 📌 `CardTween.ToPose` 全仓还有别的调用方（`Shell/CardDetailPopup` 等）—— 收口 `DOLocalRotate` 时**一起看**，
   别只改 `Hand` 这两处。
3. 📌 `PlayerHand.PositionCardInHand` 末尾那句「`isPlayer(+0x28)` 为假时把 selectedCardIndex 传 -1」
   （`PlayerHand__PositionCardInHand.c:28-30`）与我们 `HandLayout.useExtraSpaceOnSelectedCard` 的**两道门**分工
   已经写进 `HandLayout` 的 doc（`:265-272`）—— 本轮复核**仍然准确**，未改。
