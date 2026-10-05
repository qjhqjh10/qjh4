# FX4 · Rewards 十一条红修复（写手 · 2026-10-11）

> 只改了两个文件（白名单内）：`Editor/RewardsScene.cs` · `Battle/Label.cs`。
> ⛔ 没跑 Unity、没跑自检、没动 git、没改 `资料/**` 里任何既有文件（本文件是新建的）。
> 秒级类型检查：**运行时错误数: 0 / 编辑器错误数: 0**（第一次跑撞上别人正在改的 `Shell/CampaignData.cs`，3 条 `CS0171` 全在那个文件里，等 2 分钟后复跑已 0/0 —— 见 §七）。

## 一、结论（三件各一句）

- **A（根因 #1，8 条级联）**：`RewardsScene.Build` 手抄的「三 Holder + `AddComponent<WindowsManager>()`」**已删**，改成 `var wm = WindowsManager.EnsureHost();` ⇒ `Instance` 就是 `wm2` 那一台（A309 新接的开窗路径不再另建第二台）。
- **B（#3）**：`punchFlips >= 1` **已删**（它与 DOTween 的算法矛盾，不是与本实现矛盾），换成 **「不向下穿零」+ 两条「段界端点」**（幅度 0.1 @ `delay+0.4/3`、归 0 @ `delay+0.4`）。
- **C（#11）**：`LineCount` 那条**改成激活后先重排再读**，并新增 **`Label.PendingWrapAppliedCount` 计数器**断「那一刀真的被兑现」；⚠️ **DIAG-B 要的「跨 `SetActive` 增长」我没照写**（判据冲突，理由见 §五·1）。

## 二、判据（逐条带 文件:行号 / 原版值 vs 我们值 / 出处）

| # | 判据 | 原版值 vs 我们值 | 出处 |
|---|---|---|---|
| A | 夹具必须走公共件建管理器 | 原版只有一个静态锚点表 + 一台管理器；我们手抄了第三份 ⇒ `Instance` 恒 null | `Shell/WindowsManager.cs:565,577-578,592-593,601-620` · `Shell/RewardWindow.cs:446,469` · `Shell/DailyData.cs:868` |
| A | 批处理下 `Awake/OnEnable` 只对 `[ExecuteAlways]` 跑 | `WindowsManager` 无该特性（`:64` 的 `WindowHolder` 有） | `资料/已知的坑.md:704` · `Shell/PromptPopup.cs:868` · `Shell/MainMenuRuntime.cs:533` · `Shell/PointerLayer.cs:47-48`（**四条独立记录**） |
| B | `DOTween.Punch` 段数 `= (int)(vibrato × duration)`、`<2` 钳 2 | 我们实参 = 原版字面量 0.4/5/0.2/0.1（`Shell/RewardWindow.cs:273-276`）⇒ `count = 2` ⇒ 曲线 `1.0→1.1→1.0`，**构造性不穿零** | `DG.Tweening.DOTween::Punch` 方法体 IL + 原版 `GameAssembly.dll` 同函数反汇编（DIAG-B §二·#3） |
| B | 段界 = `[0.4/3, 0.8/3]`（`dur[i] ∝ (i+1)/count` 归一化） | 第一段走完那一拍 = `delay + 0.4/3` 恰为 **0.1**；末点 `delay + 0.4` 恰为 **0** | 同上（段时长那两段 IL/反汇编） |
| C | `LineCount` 读的是 `m_textInfo.lineCount`，而 `TextMeshPro.Awake()` 会 `m_textInfo = new TMP_TextInfo(this)` | 两种世界读数**都是 0** ⇒ 旧断言无判别力 | `TextMeshPro.cs:582-593`（`[ExecuteAlways]` 在 `:19`）· `TMP_Text.cs:1233` |
| C | TMP 的 `ForceMeshUpdate` 有 `!m_isAwake` 闸；`GetTextInfo` **没有** | 激活后 `m_isAwake == true`（`Awake` 里 `:626` 置位）⇒ `ForceRelayout` 可用；`WorldW` 那条路更稳 | `TextMeshPro.cs:347-352,2119` · `TmpFont.cs:244-260` |

## 三、改动清单

`Editor/RewardsScene.cs`
1. `Build()`：删 `:704-710` 三句 `MakeHolder` + `AddComponent<WindowsManager>()` ⇒ **`var wm = WindowsManager.EnsureHost();`**（含 20 行订正注释 + 改坏法）。
   ⚠️ **不动** `MakeHolder` 本体（现在是死代码，加了一行注释说明「留着当形状存档、新代码别调」）。
2. `:5141-5148` 采样循环：`punchFlips`/`prevSign` 换成 `punchMin`（曲线形状）。
3. `:5158-5175`：`punchFlips >= 1` 整条**删**，换成 `CheckTrue(punchMin >= -1e-3f, …)`（含 10 行订正注释：为什么旧判据错、原版也是 0 次）。
4. `:5176-5201`：**新增**两条段界断言（新建一扇 `rwE`，两次 `Advance` **精确落在 t 上**）。
5. `:6278-6316`：A266 段 —— 新增计数器快照/断言、`ForceRelayout()` 挪到 `SetActive` 之后、`WorldW` 提前、`LineCount`/`WorldW` 两条**称号改写**（不再号称能证 `OnEnable`）。

`Battle/Label.cs`（**CRLF 文件，用 `wb` 二进制改的，改完 CRLF 仍 921/921**）
6. `TryApplyPendingWrap()` 里加 `PendingWrapAppliedCount++;`（**只记账，不改任何行为**）。
7. 紧跟 `LineCount` 之后加 `public int PendingWrapAppliedCount { get; private set; }`（只读口 + 8 行理由）。

## 四、改后预计（哪几条会绿 / 通过数怎么变 / 还剩什么）

- **8 条级联一次全绿**：`#1 :5032` · `#2 :5061` · `#4-#8 :5479/5485/5541/5543/5558` · `#9 :295-302`（由 `:5620` 调）· `#10 :303-304`。⛔ 队列常量一个没动。
- **#3 / #11 各被替换成 3 条 / 2 条** ⇒ 断言总数 **1282 → 1285**（+2 +1）。预计 **1285 通过 / 0 失败**。
- **还剩（本件范围外）**：`Shell/RewardWindow.cs:446` 那个「`EnsureHost` = 没有就另建一台」的语义仍在；4 个夹具的手抄（A351）；A352（`05_收件箱_空态.png` 拍的不是收件箱）。
- 🔴 **理论上有「绿的翻红」的可能**（DIAG-B §五·3 点名）：修好之后 **§六 那几条真鼠标/窗口态/截图探针会第一次打在被它们该打的东西上**，若那处的行为本身没验过 ⇒ 会现形。**那不是回归，是原来被遗留窗遮住的缺陷** —— 要跑一次才知道。（`05_收件箱_空态.png` 现在拍到的会是**真的收件箱**。）

## 五、没查清 / 判不了的（⛔ 不猜）

1. 🔴 **判据冲突（我改了 DIAG-B 给的做法，如实报）**：DIAG-B §三·#11(b) 要求「断 `PendingWrapAppliedCount` **跨 `SetActive` 增长**」。按本仓**四条独立记录**，批处理编辑模式下 `Awake/OnEnable` **只对 `[ExecuteAlways]` 的脚本**跑，而 **`Label` 没有那个特性** ⇒ 那一跳**很可能根本不跑** ⇒ 照写**大概率恒红**（等于拿红换红）。⇒ 我改成断**「跨【激活 → 第一次真重排】」增长**（`ForceRelayout()` 走 `RefreshBounds` 尾句那一跳）：**两种世界都绿**，而删掉待办机制/删掉 `RefreshBounds` 尾句**会红**；⛔ 单删 `OnEnable` **报不出来**（Play 模式那一跳**本夹具判不到**）。**补偿**：`appliedAtActivate`（激活那一刻的计数）**印进断言消息** ⇒ 下一次跑 `RewardsScene.Run` 就能用日志**坐实**「`OnEnable` 到底跑没跑」——那正是 `V4b_三件口径.md` §Q1 「⚖️ 判不了」那一格。
2. **`Label.OnEnable` 与 `TextMeshPro.Awake` 谁先跑**（V4b 同一条）：仍判不了，但**不影响**上面那条（两种世界都绿）。
3. **DIAG-B §五·2 那三个字面量的第四次复算**（`read_literal.py` 跑 `0x1834b158 / 0x1834b2bb0 / 0x1834b2dc4`）**我没做** —— 采信 W1/DIAG-B 的读数。**不影响**本件：段数只用 0.4 与 5，而这两个数在我们自己的代码里也写着同一份（`Shell/RewardWindow.cs:273-274`）。
4. **两条新段界断言判不到缓动**（`SetEase` 只改路上、不改端点）—— 已在注释里如实写。
5. **新段界断言的采样精度**：靠「一次 `Advance` 精确推到 t」保证（段界那一拍斜率 0.75/s，0.02 步长会偏 0.015 > 容差 0.01 ⇒ 走过去必然假红）。**没有实跑验证**（⛔ 不许跑 Unity）⇒ 若下次跑这两条红，先看是不是浮点/`ManualUpdate` 语义与预期不同。

## 六、顺手发现（⛔ 只报不改）

1. **`EnsureHost()` 的语义仍是「没有就另建一台」**，不查「场景里是不是已经有一台在管这些窗」—— 生产侧不触发（`ShellRuntime.cs:216` 先建），但**任何新的 `EnsureHost()` 开窗路径都会在同形的 4 个夹具里再犯一次**（A351 已立）。
2. **`RewardsScene.MakeHolder` 现在是死代码**（只有 `Build` 调过）。我**没删**，加了一行注释说明「留着当形状存档」；要删请调度台裁决。
3. **DOTween 段数公式是跨件知识**（DIAG-B §六·4 已报，我复算过公式本身）：`CardFeel.PushBackVibrato = 8 × 0.4 ⇒ count = 3 ⇒ 会穿零`；`TargetReticle vibrato = 0 ⇒ count = 0 → 钳 2 ⇒ 单峰`。⚠️ 那两处的**断言**我**没查**（不在本件白名单）—— 建议派一条去核。
4. **「`[ExecuteAlways]` 的回调在批处理下确实跑」有了本仓实证**（比引文档更硬）：本夹具的日志里就有 `WindowHolder`（`:64` 带 `[ExecuteAlways]`）在 `AddComponent` 那一刻打出「锚点 placement = None」= 它的 `OnEnable` 真跑了；而**同一段日志**里 `WindowsManager`（无该特性）的 `Awake` 没跑（`Instance` 恒 null）。⇒ 这一对**反例/正例同框**，建议进 `资料/已知的坑.md`。
5. `TMP_Text.textInfo` 的 **getter 会自己 new 一个**（`TMP_Text.cs:1233`）⇒ 「没 `Awake` 就一定 NRE」不成立（`GetTextInfo` 那条路能跑通就是这个原因）—— DIAG-B §二·#11 第 3 步说「`m_textInfo` 在这一次之前一定是 null」对，但**不能反推「Awake 跑过」**。

6. **`PointerLayer` 的创建时机提前了（已核：无可观测差异）** —— `EnsureHost()` 第一句就 `PointerLayer.Ensure(root)`，所以指针层现在在 **`Build()` 那一刻**建（改前是「第一次读 `PointerLayer.Instance`」时建）。两边都是 `new GameObject("Pointer Layer")` **无父**；而 `RegisterScroll` 读的 `Instance` 是**惰性 getter**（没有就现建）⇒ **注册表内容一字不变**（`Shell/PointerLayer.cs:55-68,188-193`）。所以 §三 那两条滚轮断言（`:2367/:2369`）不受本件影响。

## 七、跑过的检查（原文贴）

```
$ TMPDIR=/tmp/wf_fx4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（**中间那一次**：3 条 `CS0171` 全在 `Shell/CampaignData.cs:310`（`RewardSpec.IsEphemeral/EphemeralMs/ConvertedInto` 未赋值）—— 那是**别的写手正在改的文件**（本件黑名单点名），不是我改出来的；等 2 分钟复跑即 0/0。判据照 `CLAUDE.md` §13·3：错误**全部集中在不是我负责的文件**上 ⇒ 不是我的问题。）

**改完的行尾核对**（铁律 12）：`RewardsScene.cs` CRLF=0 / LF=6558（本来就纯 LF，仍是）· `Label.cs` CRLF=920 / LF=920（本来就 CRLF，**仍是** —— 用 `wb` 二进制改的，没用 `sed -i`）。
