# B141 · W129 §4-15 剩余 5 处命中区外扩（执行写手 `B141-W23`）

> 会话：第一百二十八 · 批次 `B141` · 2026-11-04 · 写手：`B141-W23`
> 输入判据：`判据/B141_W129_对账_UI动画.md` §4-15 行 · `报告/B138_审计汇总_问题清单.md` §4-15
> 原版判据一律**逐字段现读**（`d:/2/tools/assetripper_out/ExportedProject/` 的 prefab / 场景 YAML），
> ⛔ 未改账主表 · ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未动 `d:/2` · ⛔ 未碰 `CampaignTab.cs` / `CollectionWindow.cs`。

---

## 一、五处改后状态（一行一处）

| # | 站点（现读行号） | 控件 | 原版 `m_RaycastPadding`（UGUI 序 **L/B/R/T**） | 改前 | 改后 | 外扩/内缩后的命中矩形 |
|---|---|---|---|---|---|---|
| 1 | `Shell/MissionsTab.cs:1756`（`AddInfoTooltip`） | `info`（4 颗吃射线） | **(−15, −15, −15, −15)** | 0（裸 `R(r)`） | ✅ −15 全边（内联） | 41.1569² ⇒ **71.1569 × 71.1573** |
| 2 | `Shell/PracticeModePopup.cs:1440`（`BattleHit`） | `Continue Button` | **(0, −40, 0, −40)** | 0（裸框） | ✅ `ContinueHitPad` | 我们的框 383.84 × 69.92 ⇒ **383.84 × 149.92** |
| 3 | `Shell/LiveOpsEventWindow.cs:1488`（`HelpHit`） | `Help Button` | **(−30, −30, −30, −30)** | 0（裸 `r`） | ✅ `HelpHitPad` | 65.1264² ⇒ **125.1264²** |
| 4 | `Shell/ExpansionPassEventWindow.cs:991`（`Help Icon Player Hit`） | `Help Icon Player` | **(−20, −20, −20, −20)** | 0（裸框，注释自陈「未逐字段读」） | ✅ −20 全边（内联） | 52.2² ⇒ **92.2²** |
| 5 | `Shell/TopBar.cs:320`（头像格 `ProfileHit`） | `Profile content`（`Profile border` 的拉伸子件） | **(42, 25, 46.5, 27.5)**（**正 = 内缩**） | `Vector4.zero`（不登记） | ✅ `ProfilePad` | `AvatarBorderBox` 175.5×130.1 ⇒ **87 × 77.6** |

符号口径 = `MenuDraw.PaddedRect`（全工程唯一一份）：**负 = 外扩、正 = 内缩**。

## 二、判据出处（逐字段现读）

| # | 文件 | 读到的东西 |
|---|---|---|
| 1 | `Assets/Missions Tab.prefab` | 4 颗 `m_RaycastTarget = 1` 的 `info` Image **全部 `(−15,−15,−15,−15)`**；同 prefab 另有 4 颗 `Handle` = −25（**另一族**，不属于本条） |
| 2 | `Assets/Practice Mode Menu.prefab` | `Continue Button`（453.08 × 49.84）`m_RaycastPadding = (0, −40, 0, −40)`；全 prefab 另一颗非零 pad 是 `Toggle`（−39.36/−35.75/−47.09/−55.32） |
| 3 | `Assets/RankedEventWindowV2.prefab` · `Assets/SkirmishModeEventWindow.prefab` | 两份 `Help Button` 同值 `(−30,−30,−30,−30)` |
| 4 | `Assets/Expansion Pass Tab.prefab` · `Assets/Raid Progress Tab.prefab` · `Expansion Pass Missions Tab.prefab` | `Help Icon Player` Image `(−20,−20,−20,−20)`（三份同值） |
| 5 | `Assets/Data/Scenes/MainMenu Warpforge.unity:68443-68444` | `Profile content` 的 Image：`m_RaycastTarget = 1` · `m_RaycastPadding = (42, 25, 46.5, 27.5)`（父 = `Profile border`，其 `Border` 自己 `m_RaycastTarget = 0`） |

⚠️ **第 5 条的判据不在 prefab 里，在场景 YAML** —— `grep *.prefab` 对这组值 **0 命中**（已复核），别据此判「判据为空」。

## 三、行数变化（账面漂移控制）

| 文件 | 前 | 后 | 净变 | 说明 |
|---|---|---|---|---|
| `MissionsTab.cs` | 2840 | 2840 | **0** | 2 处就是**就地改写**（`1748` 注释段 · `1755/1756` 调用）；⛔ 未新增成员（原因见 §五-4） |
| `PracticeModePopup.cs` | 3808 | 3816 | **+8** | 类尾新增 `ContinueHitPad`（类尾之后无账落点：本文件账最大引用行 = `:3769`） |
| `LiveOpsEventWindow.cs` | 2504 | 2511 | **+7** | 类尾新增 `HelpHitPad`（账最大引用 = `:2475`） |
| `ExpansionPassEventWindow.cs` | 2925 | 2925 | **0** | pad 写成**内联字面量**（原因见 §五-5） |
| `TopBar.cs` | 1150 | 1159 | **+9** | 类尾新增 `ProfilePad`（账最大引用 = `:1026`）；另就地订正 3 段**过期注释**（`AvatarBorderBox` 的 doc · `Wire` 的 `pad` 参数 doc · 头像格接线注释） |

改动**全部落在每文件账最大引用行之后**（或纯就地改写）⇒ `check_ledger_points` 不会因本笔产生漂移。

## 四、验证

1. `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w23 bash 工具/typecheck.sh`
   - 运行时程序集：**错误 0**
   - 编辑器程序集：**错误 0**
2. 新增断言 `CardPresentation/Editor/B141_W23_Assert.cs`（**27 条**）：
   - **A 组 12 条**（纯值 + 纯几何，零前提）：A1/A2 MissionsTab · A3/A4/A4b Practice · A5/A6 LiveOps · A7/A8 ExpansionPass · A9/A10 TopBar · A11 符号口径。
   - **B 组 15 条**（源码结构）：`B0` ×5（读得到 5 份源码）+ `B1~B5`（每个站点**真的**把 pad 传进建命中区那一句）+ `B6~B10`（旧的「不扩」写法**已不在**）。
   - ⚠️ **本写手没有宿主接线权限**（`Editor/*Scene.cs` 是别人/调度台的），接线方式见文件头注释：
     同一物理行追加 `B141_W23_Assert.Run(); pass += B141_W23_Assert.Pass; fail += B141_W23_Assert.Fail;`
   - ⚠️ 断言**依赖的三个常量**：`PracticeModePopup.ContinueHitPad` · `LiveOpsEventWindow.HelpHitPad` · `TopBar.ProfilePad`（都是 `public static readonly`）。
     MissionsTab 与 ExpansionPass 那两处是**内联字面量**，故 B 组按**源码文本**验（含 pad 数值本身）。
3. **灭自证**（🧨 哪条会红）：
   - 任一处退回 `r` / `Vector4.zero` ⇒ 该站点 `B(真传了)` **与** `B(旧写法已不在)` 同时红。
   - pad 数改错（如 −15 → −11）⇒ A 组字面量 / 外扩宽高红。
   - `MenuDraw.PaddedRect` 正负号被翻 ⇒ A11 + A2/A4/A6/A8/A10 全红。
   - `ProfilePad` 正负颠倒 ⇒ A9/A10 红（内缩变外扩）。
   - 退出码 / typecheck 都**不能**证明「断言真跑了」—— 判绿红只看行首 `B141W23 ✓` / `✗`。

## 五、顺手发现（⛔ 本笔未改，交给调度台分流）

1. **【TopBar 底框与原版不是一个件】**（**✅ 2026-11-04 `B141-W23b` 已修，见 §七-1**）原版吃射线的是 `Profile content`（`m_SizeDelta = (88.532, 72.849)` 的**拉伸**子件，真矩形 = 父 109.451×122.279 外扩 ⇒ **197.983 × 195.128**），pad 收完 **109.48 × 142.63**；
   我们沿用的 `AvatarBorderBox` 是 **175.5 × 130.1**（手抄框）⇒ 同一个 pad 收完 **87 × 77.6**（面积约原版 44%）。
   ⇒ **pad 已按原版落地，但底框仍偏**。要完全对齐须改 `AvatarBorderBox`（属「改整格尺寸」= 本笔被明令禁止）⇒ 请调度台另立一条。
2. **【PracticeModePopup 底框是手拼框】**（**✅ 2026-11-04 `B141-W23b` 已修，见 §七-2**）原版 `Continue Button` = **453.08 × 49.84**（pad 收完高 **129.84**）；我们传进 `BattleHit` 的是「`Continue` ∪ `CircleButton`」的手拼框 **383.84 × 69.92** ⇒ pad 落地后高 **149.92**（比原版高 20.08）。
   同一条链上 `CircleButton`（90×90）自身的 pad 我们**没读**（`HitOn` 把两颗合成一格）⇒ 严格复刻应改成「两件各自 pad 后取并集」（口径 = `MenuDraw.Union`）；本笔按简报只补 pad。
3. **【MissionsTab 的 `Handle` ×4 = −25，不在 4-15 内】**（**✅ 2026-11-04 `B141-W23b` 已判 ⛔，见 §七-3**）同 prefab 里 4 颗 `Handle` Image 的 `m_RaycastPadding = (−25 全边)`；4-15 那条只点 `info`。若那 4 颗是真的可点件（像滑条把手 / 领取钮），它们的命中区**仍缺外扩** —— 请调度台确认是否另立一条（⛔ 本笔未动）。
4. **【MissionsTab 为什么用内联字面量】** 本文件是 `namespace` 下**三个并列 class**（`MissionsTab` / `MissionContainer` / `GridMissionCellCount`），而 `MissionContainer` 的 `:2763` / `:2765` 被账按行号钉住 ⇒ 往 `MissionsTab` 类尾插成员会让它们整体下移。
   ⇒ pad 写成内联（零行数变化）。**未来若要提成常量，必须同批改那两条账。**
5. **【ExpansionPassEventWindow 为什么用内联字面量】** 本类类尾在 `:2830` 一带，之后还有他批的 `ExpansionPassEventTabLogic` / `ExpansionPassMissionsTabLogic`，而账钉着 `ExpansionPassEventWindow.cs:2910` ⇒ 加成员会让锚点下移 ⇒ 同样走内联。
6. **【TopBar 的旧注释已按铁律 5 订正】** 原写「那颗 `Border` 的 pad 全 0（且 `m_RaycastTarget = 0`）⇒ 零 pad = 不登记」，只说了「谁不吃指针」、**没说清谁吃**。
   现已就地改成「`Border` 确实 `m_RaycastTarget = 0`，但真正吃指针的是拉伸子件 `Profile content`，pad = (42,25,46.5,27.5)」——判据 `MainMenu Warpforge.unity:68443-68444`。
7. **【同一条 4-15 还剩的站点】** `CollectionWindow.cs:3961`（`CloseHit` −10）**不在本件**（另一包）；`CampaignTab.cs` 两处（1001 = 「子树各靶 pad 后取并集 = 142×132」· 1695 = info pad **0** 原版就是 0）由 `W16` 已做完 ⇒ 4-15 的 8 处现在只剩 `CollectionWindow` 那 1 处。

## 六、改动清单（文件级）

- `CardPresentation/Shell/MissionsTab.cs` —— `AddInfoTooltip` 命中区外扩 −15 全边（就地改 2 行 + 1 段注释）。
- `CardPresentation/Shell/PracticeModePopup.cs` —— `BattleHit` 命中区按 `ContinueHitPad` 上下各 +40；类尾新增常量。
- `CardPresentation/Shell/LiveOpsEventWindow.cs` —— `HelpHit` 命中区按 `HelpHitPad` 外扩 30；类尾新增常量。
- `CardPresentation/Shell/ExpansionPassEventWindow.cs` —— `Help Icon Player Hit` 命中区外扩 20（内联）；就地订正「未逐字段读」的过期注释。
- `CardPresentation/Shell/TopBar.cs` —— 头像格按 `ProfilePad` 内缩；类尾新增常量；就地订正 3 段过期注释。
- `CardPresentation/Editor/B141_W23_Assert.cs` —— 新建，27 条断言（**未接线**，等调度台挂宿主）。

---

## 七、续做（`B141-W23b` · 2026-11-04）：三处底框 / 残余

> 触发 = `B141-W23` 在 §五 报的三条。本节的改动都在**同一批白名单文件**里（`TopBar.cs` · `PracticeModePopup.cs` · `MissionsTab.cs` · `Editor/B141_W23_Assert.cs` · 本报告）。
> typecheck（`TMPDIR=/tmp/wf_b141_w23 bash 工具/typecheck.sh`）= 本笔改完后**运行时 0 / 编辑器 0**。
> ⚠️ 中途复跑曾出现 **2 条不属于本件的错**：`Shell/BoosterPackOpenWindow.cs(148/150) CS0103 'QBase'`（该文件 `git diff` 19/13 = **另一写手在飞**，
> 属铁律 13·3「错误全落在不是你负责的文件上 ⇒ 不是你的问题」）；**他写完后最后一次复跑已回到 0/0** ⇒ 本件四个文件**全程没有一条**编译错。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改账主表。

### 七-1 · TopBar 头像格：命中区底框换成 `Profile content` 的真矩形（**已落地**）

| 项 | 改前（W23） | 改后（W23b） |
|---|---|---|
| 命中区底框 | `AvatarBorderBox`（**画**盾框用的框）175.5 × 130.1 | **`ProfileContentBox`**（原版吃射线那颗图自己的矩形）**197.983 × 195.128** |
| pad | `ProfilePad = (42, 25, 46.5, 27.5)` | 不变 |
| 收完的命中区 | 87 × 77.6（约原版 44%） | **(23.1335, −7.149) … (132.6165, 135.479)** = **109.483 × 142.628**（= 审计的 109.48 × 142.63） |
| 「画出来那块」 | `FitAspect(盾框贴图, AvatarBorderBox)` | **不变**（仍按 `AvatarBorderBox` —— `Wire` 新增的 `hitBox` 只喂命中那一半） |

**逐级算式**（设计空间绝对坐标，y 向下；判据 = `MainMenu Warpforge.unity` 的 4 个 RectTransform 逐字段现读）：
- `Player Profile` = 528.65 × 211.07 · 锚 (0,1)/枢 (0,1)/pos (0,0) ⇒ 设计矩形 **0..528.65 × 0..211.07**
- `Profile border` = 109.451 × 122.279 · 锚/枢 (0.5,0.5)/pos **(−186.6, 31.12)** ⇒ 中心 (77.725, 74.415) ⇒ 矩形 **22.9995..132.4505 × 13.2755..135.5545**
- `Profile content` = 拉伸锚 (0,0)-(1,1) · pos **(2.4, 11.5)** · sizeDelta **(88.532, 72.849)** · 枢 (0.5,0.5)
  ⇒ offsetMin = (2.4 − 44.266, 11.5 − 36.4245) · offsetMax = (2.4 + 44.266, 11.5 + 36.4245)
  ⇒ 矩形 **−18.8665..179.1165 × −34.649..160.479** = 197.983 × 195.128 ✓

**代码改动**（零行数变化）：
- `Wire` 签名加**第 6 个可选形参** `PxRect? hitBox = null`；命中那一半改成 `MenuDraw.PaddedHitRect(hitBox ?? box, pad)`，**`drawn` 仍按 `box`**（第 6 参不传时逐位等于旧行为 ⇒ 另外三颗钮零影响）。
- 头像那一次调用补 `, ProfileContentBox`。
- 类尾新增 `public static readonly PxRect ProfileContentBox`（逐级算式写进注释）。

⚠️ **行为变化（相对「pad 全 0 不登记」那一版）**：命中区由「画出来那块盾」（19.52..135.98 × 9..139.1）变成 **23.13..132.62 × −7.15..135.48**
—— 左右各**收窄约 3.4 px**、上沿**高出 16.1 px**（`Profile content` 那件本来就从 `Profile border` 上方探出）。这与原版一致，
但 `Editor/MainMenuScene.cs` 里若有「点盾框最边缘那一点」的断言，需要按新边界复核（本写手跑不了 Unity ⇒ 只登记，未验）。

### 七-2 · PracticeModePopup `BattleHit`：改成「两件各自 pad 后取并集」（**已落地**）

| 项 | 改前（W23） | 改后（W23b） |
|---|---|---|
| 底框 | 手拼「`ContL..CircR` × `ContT..CircB`」= 383.84 × 69.92 | **`MenuDraw.Union(Continue(带 pad), CircleButton(pad 0))`** |
| pad | `(0, −40, 0, −40)` 加在手拼框上 | 分别作用：`ContinueButton` **(0,−40,0,−40)**、`CircleButton` **(0,0,0,0)**（原版逐字段现读） |
| 结果 | 383.84 × **149.92** | **453.08 × 129.84** ✓（= 原版） |

- `Continue` 带 pad = (1384.90, 866.26) … (1837.98, 996.10)；
  `CircleButton` 原版矩形 = (1678.74, 886.18) … (1768.74, 976.18) **整块落在里面** ⇒ 并集外接盒不变（A4c 专门钉这一条）。
- 旧的「手拼框」同时是**两处错**：宽度少 69.24（截在 `CircR` 而不是 `ContR`）、高度多 20.08 ⇒ W23 只补 pad 的时候才显出 149.92。

### 七-3 · MissionsTab `Handle` ×4（−25）：**判「不是命中区」，⛔ 不补**（反证）

- **`Handle` 是什么**：4 颗全在 `…/Mission Milestones Progress Bar/Progress Bar/Handle Slide Area/Handle`（1 份在 `Daily Login Container`、3 份在 `Daily Mission Container(0/1/2)`），另 1 颗在 `Weekly Mission` 那棵（共 **5** 颗，`B138` 只列 4 颗）。
- **反证 ①**：父 `Progress Bar` 的 uGUI `Slider`（prefab 里 `m_Script: {fileID: -113659843}`）**`m_Interactable = 0`**（4 份全 0）⇒ `Selectable.IsInteractable()` 恒假 ⇒ `Slider.OnPointerDown / OnDrag` 第一句就早退。
- **反证 ②**：`Handle` 自己只有 `RectTransform + CanvasRenderer + Image`（组件 cls `224/222/114`）—— **没有 Button / EventTrigger / Slider / 任何脚本** ⇒ 它 `m_RaycastTarget = 1` 只是「吃射线」，**零消费者**。
- ⇒ 那 `−25` 是「不可交互滑条把手」的 pad，**不构成命中区**（我们侧也只画矩形，`MissionsTab.cs:1441` 的反证注释）；**⛔ 不补外扩**。
- ⚠️ 若将来做「进度条可拖」那是**新功能**（原版没有），要另立判据，⛔ 别拿这 −25 当它的命中区。

### 七-4 · 断言增量

`Editor/B141_W23_Assert.cs` 现在 **30 条**（A 组 15 · B 组 15）：
- 新增/改写：`A4b`（453.08 × 129.84）· `A4c`（并集不受圆钮影响）· `A9b`（`ProfileContentBox` = 197.983 × 195.128）· `A10`（收完 109.483 × 142.628）· `A10b`（**拿画用底框去收必须不等于** A10 ⇒ 证明「底框换对了」有观测效应）。
- `B2` / `B5` / `B7` 改成盯**新写法**（并集表达式 · `ProfileContentBox` 实参 · 旧手拼框已不在）。
- 灭自证：把 `Wire` 的 `hitBox` 传回 `box`（或不传）⇒ `A10` + `A10b` 红；`BattleHit` 退回手拼框 ⇒ `A4b` + `B7` 红。

### 七-5 · 行数变化（W23b 这一笔）

| 文件 | 前 | 后 | 净变 | 说明 |
|---|---|---|---|---|
| `TopBar.cs` | 1159 | **1173** | **+14** | 类尾新增 `ProfileContentBox`（11 行注释 + 1 行声明 + 空行）；其余 4 处**就地改写**（`Wire` 签名/体 · `pad` 参数 doc · 命中区那一行的 doc · 头像格 2 处注释）。账最大引用 = `:1026` ⇒ **全部落在之后，无漂移** |
| `PracticeModePopup.cs` | 3816 | **3816** | **0** | 只改 `HitOn` 那一行 + 2 段注释（就地）；类尾常量块仍 7 行 |
| `MissionsTab.cs` | 2840 | **2840** | **0** | 只改 1 行注释（`Handle` 反证） |
| `Editor/B141_W23_Assert.cs` | —— | —— | —— | 本件无账落点 |

