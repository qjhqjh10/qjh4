# A22 · DOTween `Sequence.SetEase` 全仓审计（`Shell/**` · `Core/**` · `Editor/**`）

> 执行代理 **A22** · 2026-10-24
> 白名单：`CardPresentation/Shell/**` · `CardPresentation/Core/**`（⛔ 未碰 `VfxMap.cs`）·
> `CardPresentation/Editor/**`（⛔ 未碰 `BattleScene.cs` / `CollectionScene.cs`）。
> ⛔ 未跑 Unity。改完 `.cs` 跑过 **`TMPDIR=/tmp/wf_a22 bash d:/4/Unity/工具/typecheck.sh`** ⇒ **运行时 0 / 编辑器 0**。
> ⛔ 没动 git。`git diff --numstat`：`Core/CardTween.cs +12/−1` · `Shell/CardDetailPopup.cs +19/−2`（两份都是 **LF**，行尾没翻、无 BOM）。
> 背景与一处已修样例（收藏入场动画）→ `A18_收藏入场动画.md` §一 / §五·4，本批**没有**重复查。

---

## 〇、结论速览

| 项 | 数 |
| --- | --- |
| 本范围内 `DOTween.Sequence()` **创建点** | **17**（CardFeel 12 · CardView 1 · UnitTweenTable 2 · CardTween.ToPose 1 · CollectionWindow 1） |
| ↳ 其中序列层**固定 `Linear` / 未设（= 默认 Linear）** | **16** |
| ↳ 其中序列层 ease **由调用方传**（`CardTween.ToPose`） | **1**（代码逻辑未动，只加 doc 钉死口径） |
| 把**非 `Linear`** 传进 Sequence 的**调用点**（= 二次弯曲） | **2**（`CardDetailPopup.cs:1049/1070`，均经 `ToPose`） |
| 已改 | **2 处调用点 + 1 处 helper 文档** |
| 判为不改 | **16 个序列 + 全部单 tween `SetEase`**（见 §2.1） |
| 待查 | **0**（范围内） |
| 范围外、同一缺陷（需转交） | **2 处**（`Hand/HandLayout.cs:844` · `Hand/CardInteraction.cs:493`） |

---

## 一、判据（本轮新查实的「硬知识」—— 以后别再靠印象）

### 1.1 `DOTween.Sequence()` 的默认 `easeType` 是 **`Linear`**，**不是** `defaultEaseType`

文件头那种「原版没写 `SetEase` ⇒ 吃 DOTween 默认 `OutQuad`」的说法，**只对单个 tween 成立**，
**对 Sequence 不成立**。反编译 `Assets/Plugins/Demigiant/DOTween/DOTween.dll` 实读：

| 方法 | IL | 含义 |
| --- | --- | --- |
| `DG.Tweening.Sequence::Setup` | `IL_003a: ldc.i4.1` → `IL_003c: stfld DG.Tweening.Tween::easeType` | **序列自己的 ease 硬编码 `Ease.Linear`(1)** |
| `DG.Tweening.Tweener::Setup` | `IL_006a: ldsfld DG.Tweening.DOTween::defaultEaseType` → `IL_006f: stfld …::easeType` | 单个 tween 才取 `defaultEaseType`（本工程 `DOTweenSettings.asset` = **6 OutQuad**） |
| `DG.Tweening.Sequence::DoApplyTween` | `IL_0018-1f: ldfld …::easeType; ldc.i4.1; beq.s → 跳过` | `easeType == Linear` 时**整段跳过**缓动；否则 `EaseManager.Evaluate(...)` 去乘 `from`/`to` |

⇒ **两步推理，缺一不可：**

1. 原版「`DOTween.Sequence()` + 若干 `Join/Append`、**不写 `SetEase`**」⇒ 整条时间轴**恒等（不弯曲）**，
   每条子补间各吃自己的 `defaultEaseType`（OutQuad）。
2. 我们若把同一串包进 Sequence 再 **`SetEase(OutQuad)`** ⇒ 子补间的 OutQuad **之上再弯一次**
   （`DOTween.XML` `SetEase` 原文：`If applied to Sequences eases the whole sequence animation`）⇒ **静默变样**。

### 1.2 原版**全部 31 个**建过 `DOTween.Sequence()` 的方法 —— 机械普查

对 `d:/2/tools/decomp_full/*.c` 里所有含 `DG_Tweening_DOTween__Sequence` 的文件，取序列变量名、
再抓所有 `SetEase<object>(<第一个实参>, …)` 与它比对：

- **游戏代码：0 个**在序列上设缓动。所有 `SetEase` 的第一个实参都是**子 tween**。
- **只有 3 个**是 `SEQ_EASE=YES`，全部是 **DOTween 自带的模块实现**
  （`DOTweenModulePhysics__DOJump` / `DOTweenModulePhysics2D__DOJump` / `DOTweenModuleUI__DOJumpAnchorPos`）——
  那是库内部的写法，**本工程 `Shell/Core/Editor` 一处都没用 `DOJump`**（grep 0 命中）。

⇒ **本项目的判据线：凡是「照着原版复刻的 Sequence」，序列这一层只能是 `Linear`。**
（原版若真的想让整条序列走某个 ease，才照抄那个 ease；本轮 31 个文件里**一个都没有**。）

### 1.3 这些 IL 是怎么读出来的（免得下个会话重复摸索）

`d:/2/Warpforge_code/Scripts/DOTween/DG/Tweening/Sequence.cs` 是**签名桩**（96 行、方法体空），读不出行为；
Unity 自带的 `ikdasm` / `monodis` 是 **macOS Mach-O**（Windows 上跑不了）。
本轮用 **Mono.Cecil**（`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/il2cpp/build/deploy/Mono.Cecil.dll`）
写了个临时 IL dumper（`/tmp/ildump`，**没进仓库**，`dotnet build` 即可，源码在报告末尾附录）。

---

## 二、逐点表（范围内全部 `Sequence` / `ToPose` 调用点）

> 「我们对 Sequence 设的 ease」= 最终落到 `Tween.easeType` 的值。
> 「原版有无 `SetEase`」= **序列这一层**有没有；子 tween 的 ease 另列。

| # | 文件:行号 | 用途 | 我们对 Sequence 设的 ease | 原版有无 SetEase（出处） | 结论 | 改动 |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `Core/CardTween.cs:78` | `ToPose`：位置+倾角+缩放（3 条 `Join`） | **调用方传什么就是什么** | 原版 `CardDisplayWindow__ChangeCardPosition.c`：一个 Sequence + 6 条 Join，**无 SetEase** | **改**（helper 文档钉死「必须传 Linear」） | 加 XML doc（+12 行） |
| 2 | `Shell/CardDetailPopup.cs:1049` | 相关卡换位（被点那张 → 前台位） | **`OutQuad`（错）** → `Linear` | 同上（`ChangeCardPosition.c` 全文 `grep SetEase` = **0**） | **改** | `OutQuad`→`Linear` + 判据注释（+17 行） |
| 3 | `Shell/CardDetailPopup.cs:1070` | 相关卡换位（前台那张 → 被点槽位） | **`OutQuad`（错）** → `Linear` | 同上 | **改** | `OutQuad`→`Linear` |
| 4 | `Core/CardFeel.cs:1045` | `MeleeAttack`：前冲 → 转身 → 回位 | `Linear` | 原版是 Sequence；**两处 `SetEase` 的第一个实参都是子 tween**（`CardScript._AttackMeleeAnim_d__357__MoveNext.c:200` `SetEase(1)` / `:266` `SetEase(0xc)`），序列层无 | 不改（已正确） | — |
| 5 | `Core/CardFeel.cs:1346` | `ShakeCamera`：镜头震（把 `apply(t)` 包一层） | `Linear` | 原版不是 DOTween Sequence（相机震走 preset / 逐帧），**无序列级 ease** | 不改 | — |
| 6 | `Core/CardFeel.cs:1710` | `Charge`：出手前后仰 | `Linear` | 原版是**逐帧 `lerp`**（`ChargeTime`/`timeToChargeAttack`），不是 Sequence | 不改 | — |
| 7 | `Core/CardFeel.cs:1754` | `ReturnToHand`：回手（反向时间轴） | `Linear` | 原版是 **`Card Hand To Board` clip 曲线**，不是 Sequence | 不改 | — |
| 8 | `Core/CardFeel.cs:1935` | `HandToBoard`：登场（正向时间轴） | `Linear` | 同上（clip 曲线） | 不改 | — |
| 9 | `Core/CardFeel.cs:2134` | `AttackRecover`：攻击后回正 | `Linear` | 原版是 Sequence、`SetEase` 在子 tween；序列层无 | 不改 | — |
| 10 | `Core/CardFeel.cs:2338` | `DeathExplosion`：死亡爆散时间轴 | `Linear` | 原版是 Sequence（`DOShakePosition` 等）+ 子 tween ease；序列层无 | 不改 | — |
| 11 | `Core/CardFeel.cs:2919` | `Dissolve`（**我们自己的降级路**） | `Linear` | 原版是**材质 `_DissolveAmount`**，不是 DOTween Sequence | 不改 | — |
| 12 | `Core/CardFeel.cs:3031` | `TurnCardAround`：抽牌翻面两段 | `Linear` | 原版两段都是**子 tween `SetEase(1)`**（`CardScript__TurnCardAround.c`）；序列层无 | 不改 | — |
| 13 | `Core/CardFeel.cs:3105` | `DealInPose`：我方抽牌入场 | `Linear` | 原版是 Sequence + 子 tween `OutCubic`；序列层无 | 不改 | — |
| 14 | `Core/CardFeel.cs:3147` | `DealIn`：抽牌（无姿态版） | `Linear` | 同上 | 不改 | — |
| 15 | `Core/CardFeel.cs:3182` | `DealInQuad`：敌方抽牌（quad） | `Linear` | 同上 | 不改 | — |
| 16 | `Core/CardView.cs:7214` | `ShowHeal`：治疗飘字缩放 | `Linear`（子 tween `OutQuad` + `SetLoops(2,Yoyo)`） | 原版 `CardScript._Heal_d__468__MoveNext.c:195-199`：**单条** `DOScale` + `SetEase(6=OutQuad)` + `SetLoops(2,1)`；**没有 Sequence** | 不改（子 tween 值与顺序都对得上） | — |
| 17 | `Core/UnitTweenTable.cs:175` | `Build`：把 `UnitTweenSO` 的补间串成序列 | **不设**（⇒ 保持默认 `Linear`） | 原版 `UnitTweenSO__BuildSequence.c`：一个 Sequence + Join/Append，**无 SetEase** | 不改 | — |
| 18 | `Core/UnitTweenTable.cs:453` | `MakeResetBody`：回正（三条 Join） | **不设**（⇒ `Linear`） | 原版 `ResetBodyTween__GetTween.c:40,55,71` 三处 `SetEase` **全是子 tween** | 不改 | — |
| 19 | `Shell/CollectionWindow.cs:1312` | 收藏卡池入场（延迟 + 淡入 + 落位） | `Linear` | 原版 `CollectionCard__AnimateIn.c` 全文无 `SetEase`（机械普查同结论） | 不改（A18 已按此落地） | — |
| — | `Editor/**` | **没有任何 `DOTween.Sequence()` 创建点** | — | — | 不涉及 | — |
| — | `Editor/DOTweenSmokeTest.cs:32,51` | 冒烟测试（`DOMove` 单条 + `OutQuad`） | 单条 tween，**不是 Sequence** | 无原版对应（纯自检） | 不改 | — |

### 2.1 范围内**非 Sequence** 的 `SetEase`（列出以便确认「没漏、也没误伤」）

单个 tween 上设 ease **不受本条规则影响**（它没有外层序列去二次弯曲），本轮**一律没动**：

| 文件:行号 | 用途 | 我们的 ease | 判据 |
| --- | --- | --- | --- |
| `Core/CardFan.cs:235` | `TweenTint`：换位换色 | `OutQuad` | 原版 `UIGroupTint__SetColor.c`：`DOTween.To(...)` **单条、无 SetEase** ⇒ 默认 OutQuad ⇒ **一致** |
| `Core/CardFeel.cs:1078` / `:1197` | 出手 punch / 挨打 punch | `OutQuad` | 原版 `DOPunchPosition` 无 SetEase（`CardScript__ReceiveAttackAnim.c:27-28`）⇒ 默认 OutQuad ⇒ 一致 |
| `Core/CardFeel.cs:2402` | 墓地生长缩放 | `OutCubic` | 原版 `SetEase(9)` |
| `Core/CardFeel.cs:3040` | 翻面单条 `DORotate` | `Linear` | 原版 `SetEase(1)` |
| `Core/CardFeel.cs:3076` | 重排落位 | `OutQuad` | 原版 `DOLocalMove` 无 SetEase |
| `Core/CardView.cs:3358` | 徽标脉冲 | `OutCubic` | 原版 `BoardTraitIcon__HighlightIcon.c` `SetEase(9)` |
| `Core/CardView.cs:6655` / `:6798` / `:8347` / `:8391` | 描边 / 将死 / 高亮 / outline | `Linear` | 逐帧着色通道（不走 ease） |
| `Core/CardView.cs:10454` | 数值 bump | 随 `CardFeel.StatBump*` | 单条 punch |
| `Shell/CollectionWindow.cs:4950` | 卡格选中 punch | `Linear` | `DOPunchScale` 本身不吃 ease |
| `Shell/CollectionWindow.cs:4979` | 卡背悬停 `_Outline` α | `Linear` | 原版 `DoColorChange(..., 0.2f)`；线性是**原版口径待查项**，但**不是 Sequence 问题**（见 §四·3） |

---

## 三、改动清单（提交时请认这一组）

```text
 Unity/MyGame/Assets/CardPresentation/Core/CardTween.cs          +12 / −1
 Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs   +19 / −2
```

1. **`Shell/CardDetailPopup.cs:1049 / :1070`** —— `CardTween.ToPose(..., Ease.OutQuad)` → **`Ease.Linear`**。
   两处都带判据注释（原版方法体 + DOTween IL + 为什么战斗侧那份传 OutQuad 反而是对的）。
   **行为变化**：换位的三条子补间仍是 OutQuad，但**不再被外层序列二次弯曲**
   （半程进度 `0.75`，改前是 `OutQuad(0.75) = 0.9375`）。
2. **`Core/CardTween.cs:ToPose`** —— 只加 XML doc，把他钉死：**调用方传 `Linear`**，别传 `OutQuad`；代码逻辑未动。

**没动任何人未提交的内容**（这两个文件在本轮开始时 `git diff --numstat` = 0 行；
`Shell/CollectionWindow.cs` 里 A18/A14 的 +354/−41 未提交段落**一个字节都没碰**）。

---

## 四、待查 / 未验 / 顺手发现

### 4.1 ⛔ 本轮没跑 Unity，也没补断言 —— 覆盖情况如实说明

- 受影响的可视行为只有一处：**菜单版卡片详情窗的「相关卡换位」**（`CardDetailPopup.SwapToFront`）。
- **没有任何现成断言覆盖它**（全仓 `grep SwapToFront` 在 `Editor/` 只命中 `BattleScene.cs` 的**战斗侧**
  `CardDisplayWindow.SwapToFront`，那是另一个类、走单条 `TweenPose`）。
- 唯一能覆盖它的宿主是 **`CollectionScene.Run`**，而 `Editor/CollectionScene.cs` **在本代理黑名单里**
  （A18 所有）⇒ 本轮**没有**加断言。**建议由主对话/A18 补一条（判据已定，直接抄）：**

  ```text
  CardTween.Mode = Manual;
  var a = 详情窗 SlotViews[0].transform.position; var b = 详情窗 SlotViews[1].transform.position（改前记）；
  SwapToFront(1);
  CardTween.Advance(CardFan.SwapTime * 0.5f);
  // 半程：Linear 序列 + 子 tween OutQuad ⇒ 进度 = 0.75（不是 0.9375）
  Check(Vector3.Distance(位置, a) / Vector3.Distance(b, a) ≈ 0.75, 0.02)
  ```

  ⚠️ 这条**不是自证**：期望值 `0.75` 来自「OutQuad 在半程的值」，而改前会读成 `0.9375` ⇒ 结构上不可能两边一起绿。

### 4.2 🔴 范围外、**同一缺陷**（需转交）：`Hand/**` 里两处 `CardTween.ToPose(..., Ease.OutQuad)`

它们也把 `OutQuad` 挂到 **Sequence** 上 ⇒ 同样二次弯曲。**不在本代理白名单，一个字节没动**：

| 文件:行号 | 用途 | 现状 |
| --- | --- | --- |
| `Hand/HandLayout.cs:844` | 手牌重排（让位/补位） | `CardTween.ToPose(..., CardTween.RelayoutDuration, Ease.OutQuad)` |
| `Hand/CardInteraction.cs:493` | 拿起手牌（放大 + 抬起） | `CardTween.ToPose(..., CardTween.PickUpDuration, Ease.OutQuad)` |

⚠️ 这两处的「原版是不是 per-tween OutQuad」**本轮没查**（不在范围）——
`RelayoutDuration` 的原版 `PlayerHand.PositionCardInHand` 是逐帧移动还是补间、`PickUpDuration` 走的是
`timeToScaleIntoMoving`，都要各自回原版方法体核一遍**再决定改成 Linear 还是另有 ease**。⛔ 别照抄本批结论硬改。

### 4.3 顺手发现（不影响本轮结论，但下个会话需要知道）

1. 🆕 **「Sequence 默认 = `Linear`」这条硬知识值得进 `资料/已知的坑.md`**
   （或等价正本）：它和「tween 默认 = `defaultEaseType`(OutQuad)」是**两套默认值**，
   凭直觉写会 100% 写错。判据 = §一·1.1 那三条 IL。
2. **原版 31 个 `DOTween.Sequence()` 创建点里，游戏代码 0 个给序列设 ease**；
   3 个 `SEQ_EASE=YES` 全是 DOTween 自带 `DOJump*`（本工程没用到）。⇒ 以后**照原版写的序列，一律 `Linear`**，
   除非在具体方法体里真读到 `SetEase(<序列变量>, …)`。
3. `Shell/CollectionWindow.cs:4979`（卡背悬停 `_Outline` alpha）用 `Linear`，注释里只说「原版 `DoColorChange(..., 0.2f)`」，
   **没写原版那条的 ease**。本轮**没有**去查（单条 tween，不属于 Sequence 命题）—— 记一笔待查：
   判据 = 原版那颗材质色补间的 `DOTween.To` / `DOColor` 有没有 `SetEase`。
4. `CardTween.ToPose` 用的是 `DOMove`（**世界坐标**），原版 `ChangeCardPosition` 用的是 `DOLocalMove`（局部坐标）。
   这是 `T04` 已经记过的口径差（父链 `lossyScale ≠ 1` 时会不同），**本批没碰**，只提醒别把两件事混成一件。
5. `Core/CardView.cs:7215` 的注释写「`SetEase(6 = OutQuad)`」—— **原文核对过是对的**
   （`CardScript._Heal_d__468__MoveNext.c:197` `SetEase(...,6,...)`）。
   顺手提醒：同一族的 `BoardTraitIcon__HighlightIcon.c:33` 是 **`SetEase(9 = OutCubic)`**，
   两个方法别互相抄错（`CardView.cs:3358` 那处用的就是 OutCubic，正确）。

---

## 五、计数（与本报告开头一致，收口用）

- **总数**：范围内 `DOTween.Sequence()` 创建点 **17** 个；另有 **2** 处调用点把 ease 传进 `Sequence` 型 helper。
- **已改**：**2** 处调用点（`Shell/CardDetailPopup.cs:1049` / `:1070`：`OutQuad` → `Linear`）
  - **1** 处 helper 文档（`Core/CardTween.cs` 的 `ToPose`，代码逻辑未动）。
- **判为不改**：**16** 个序列创建点（其中 15 个固定 `Linear`、1 个（`UnitTweenTable.Build`）完全不设 ⇒ 默认 `Linear`）
  - §2.1 表里全部**单 tween** 的 `SetEase`（不适用「序列二次弯曲」）。
- **待查**：范围内 **0**；范围外 **3 条**见 §4.2 / §4.3·3（`Hand/` 两处 + 卡背悬停那条 ease 出处）。

---

```text
/tmp/ildump/ildump.csproj   → net8.0 控制台，<Reference Include="Mono.Cecil"> HintPath 指 Unity 自带那份
/tmp/ildump/Program.cs      → AssemblyDefinition.ReadAssembly(argv[0])
                              遍历 Types（含 NestedTypes）/ Methods，按 argv[1](类型名包含) / argv[2](方法名包含) 过滤，
                              逐条打印 IL 指令
用法：
  dotnet /tmp/ildump/bin/Release/net8.0/ildump.dll \
      "D:/4/Unity/MyGame/Assets/Plugins/Demigiant/DOTween/DOTween.dll" "DG.Tweening.Sequence" "Setup"
```

关键三条读数（复现用）：

```text
=== DG.Tweening.Sequence::Setup
  IL_003a: ldc.i4.1
  IL_003c: stfld DG.Tweening.Ease DG.Tweening.Tween::easeType        ← 序列默认 Linear(1)

=== DG.Tweening.Tweener::Setup
  IL_006a: ldsfld DG.Tweening.Ease DG.Tweening.DOTween::defaultEaseType
  IL_006f: stfld DG.Tweening.Ease DG.Tweening.Tween::easeType        ← 单 tween 默认 = defaultEaseType

=== DG.Tweening.Sequence::DoApplyTween
  IL_0019: ldfld DG.Tweening.Ease DG.Tweening.Tween::easeType
  IL_001e: ldc.i4.1
  IL_001f: beq.s IL_0079: ldc.r4 0                                    ← Linear ⇒ 跳过整段缓动
  IL_0046: call System.Single DG.Tweening.Core.Easing.EaseManager::Evaluate(...)
  IL_004b: mul                                                       ← 否则把 from/to 乘上缓动结果
```
