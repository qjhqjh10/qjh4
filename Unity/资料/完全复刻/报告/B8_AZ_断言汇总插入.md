# B8_AZ —— 多路代理的断言草稿「汇总插入」（执行代理报告）

> 代理 = **B8_AZ** · 2026-10-26 · 批次 8
> 白名单 = `Editor/BattleScene.cs` · `Editor/SettingsScene.cs` · `Editor/MainMenuScene.cs` · 本报告
> ⛔ 未跑 Unity / 未动 git / 未写账补丁 / 未改正本 / 未越白名单。
> 机检：`TMPDIR=/tmp/wf_b8az bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## 一 结论（一句话）

把 **4 份草稿 → 3 份宿主** 全部插完：**6 处插入点、+262 行、35 条 `Check` 调用**
（其中 **4 条前提**、**2 条 else 兜底** ⇒ **29 条真断言**）。
**改了 3 处草稿的写法**（2 处自证、1 处事实错），**1 组（AQ ④）插不了**（宿主不在白名单 + 读口不存在），
**B8_AF 报告里根本没有断言草稿**（已核，见 §六）。

| 来源草稿 | 宿主 | 插入位置（新行号） | +行 | Check |
|---|---|---|---|---|
| `B8_AR` §四（棋盘几何 4 组） | `Editor/BattleScene.cs` | `:20810-20858`（`Position(5,false).z` 那条之后） | **+49** | **5** |
| `B8_AL` §四（表现层读数口 7 组） | `Editor/BattleScene.cs` | `:3589-3715`（`CheckCardViewState(...)` 调用之后） | **+127** | **13** |
| `B8_AQ` ①（第 2/3 根滑块） | `Editor/SettingsScene.cs` | `:3751-3770`（第 1 根那条之后） | **+20** | **3** |
| `B8_AQ` ②+⑥（窗口模式下拉 / 切页重算帽字） | `Editor/SettingsScene.cs` | `:4208-4272`（音频页末、`// 联机页` 之前） | **+65** | **7** |
| `B8_AQ` ⑤（阵营列点击两态） | `Editor/MainMenuScene.cs` | `:3942-3983`（`⑤ 显隐` 那段还原之后） | **+42** | **5** |
| `B8_AQ` ③（压暗层 = ShadeHit 不是 Absorb） | `Editor/MainMenuScene.cs` | `:17487-17510`（`换回出厂内容` 那条之后、⑪ 关窗音之前） | **+24** | **2** |
| `B8_AQ` ④（飘窗 handler 路由） | — | **没插**（见 §五） | 0 | 0 |
| `B8_AF` | — | **没有断言草稿**（见 §六） | 0 | 0 |

文件规模：`BattleScene.cs` **47,793 → 47,969** · `SettingsScene.cs` **5,604 → 5,689** · `MainMenuScene.cs` **19,842 → 19,908**。
行尾逐个核过（二进制）：`BattleScene.cs` **纯 CRLF（47,968/47,968）** · 另两份 **纯 LF** · **裸 CR 全 0**。

---

## 二 改了草稿的哪几条（3 处，全部写进代码注释里）

### 2·1 🔴 `B8_AL` AL-1049 是**自证**，已改成「同手牌两张卡的比值」

- **草稿原文**：`Mathf.Abs(ls - hand.cardScale * hand.hoverScale * LayoutSpace.Scale) < 1e-3f`
  —— 三个乘数**全是被测实现自己的字段**。把 `HandLayout.hoverScale` 从 `1.3` 改成 `1.0`（或 `1.5`），
  **期望值跟着一起变** ⇒ 这条永远绿。草稿自己写的「🧨 把 `hoverScale` 改成 1.0 ⇒ 红」**不成立**。
- **改成**：同一次 `Refresh` 里取**悬停那张**与**未悬停那张**的 `TryLastPose(...).scale`，断**比值 == 字面量 1.3**
  （`VarsDevice.cardInHandShownScale = 1.3`）⇒ 期望值里**一个我们自己的常量都不出现**，
  `hoverScale` 改成任何别的数都红。
- 顺带：受体不用 `it.HoveredCard`（**它是 private，Editor 程序集读不到**，草稿也读不到 —— 那会直接编不过），
  改用**悬停前**取到的 `it.HandCardForTest(0)`（`_cards` 那个列表，`HandCardForTest` 的 doc 明写别拿
  `BattleViewAt/HandViewAt` 顶替）；「离开后 HoveredCard == null」那半并入「那张卡的两个位都关」。

### 2·2 🔴 `B8_AR` ③ 里的 `PlayerExtraYOnBoard == 0f` 是**同义反复**，已删

- 那两个常量**就定义成 `0f`**（`Board/ArenaSlots.cs:195/198`）⇒ 编译期折成恒真，**把值改坏也绿**
  （改坏时它自己那个比较也跟着变）。
- **改成**：只断**行为** —— 9 槽 × 2 侧逐格 `Position(...).y == 0`（谁把 `ExtraYOnBoard` 填成非 0，计数立刻 > 0）。
  这一改**不是放宽**：`y` 那一路本来就覆盖了它，且强度更高。

### 2·3 🔴 `B8_AQ` ③ 的第二句是**事实错**，照抄会红

- 草稿：`CheckTrue(MenuDraw.WasShadeHit(wc.CloseHitNode), "…关窗钮那颗也是 `ShadeHit`")`。
  实读 `Shell/WhereToGetCardsPopup.cs:357`：关窗钮那颗是 **`MenuDraw.Hit(_close, "Hit", …)`** 建的，
  **不是** `ShadeHit` ⇒ `WasShadeHit` **恒假**，照抄就是一条永久红。
- **改成**：断「它是**真动作**」= `CloseHitNode.GetComponent<WindowButton>() != null && !absorbOnly`
  （原版同一颗 `closeButton` 挂 `Close`；判据 = 同一份 `BackgroundCloseButton__…OnPointerClick.c:24-27`）。
  压暗层那条（`WasShadeHit && !WasAbsorb`）**保持草稿原样** —— 那条才是 `B8_AQ` 修掉的真缺陷的判别式。

### 2·4 补强（不是改判据，是把草稿里「钉不住常量」的那一条换成判别式）

- `B8_AL` AL-1100d 草稿是「90° 下 altAngle 也拦」—— **65 和 70 两个值下都成立**，钉不住常量。
  **改成 67.5°**：65° 档拦 / 70° 档放 ⇒ 一条就证明 `altAngle` 用的是 **70**
  （`C3` 那条判据本来就这么写的，`CardInteraction.HandDragMaxAngleDegAlt = 70f`）。
- `B8_AQ` ② 草稿只断了「选 Full screen ⇒ setter 收到 true」，它自己的「改坏法」里写的那半（选 Windowed ⇒ false）
  **没写**。补上（从全屏态选第 1 行 ⇒ 必须收到 `false`），并把草稿的 `Check(got, true, …)`（`bool?` 泛型推断）
  改成 `CheckTrue(got == true, …)`（`got` 为 `null` = setter 没被调 ⇒ 红，语义不变）。
- `B8_AQ` ⑥ 草稿用 `win.WinCapText()` —— **那个口不存在**（`Shell/SettingsWindow.cs` 里 `_winCap` 是 private，
  没给自检口）。**改成**沿节点路径取那颗 caption：
  `Menu Area/Media Tab/Visual Settings/WindowMode Selector/Dropdown/Label` 的 `Label.Text`
  （`Text(winFld, "Label", …)` 建的就是它，`SettingsWindow.cs:5382`）。判据不变：
  先在「全屏」档切页 ⇒ 要印 `Full screen`；再把注入点翻成窗口化、**不点任何按钮**、只切页 ⇒ 必须重算成 `Windowed`
  （🧨 删掉 `RefreshTexts()` 里 `_onLines/_onLabels` 那一圈 ⇒ 停在 Full screen ⇒ 红）。

---

## 三 行号位移表（**同步点 `--fix` 要用的那张**）

### 3·1 映射（**只对本批之前读到的行号成立**）

| 文件 | 插入点（原行号） | +行 | 映射：原行号 → 新行号 |
|---|---|---|---|
| `Editor/BattleScene.cs` | 第 1 处：`orig:3588` 之后（`CheckCardViewState(driver, Check);`） | **+127** | `L ≤ 3588` ⇒ `L`；`3589 ≤ L ≤ 20682` ⇒ **`L+127`**；`L ≥ 20683` ⇒ **`L+176`** |
| `Editor/BattleScene.cs` | 第 2 处：`orig:20682` 之后（`那个 z 偏移只管督军位`） | **+49** | （已并入上一行） |
| `Editor/SettingsScene.cs` | 第 1 处：`orig:3750` 之后（`sl[0].SetValue(before, true);`） | **+20** | `L ≤ 3750` ⇒ `L`；`3751 ≤ L ≤ 4187` ⇒ **`L+20`**；`L ≥ 4188` ⇒ **`L+85`** |
| `Editor/SettingsScene.cs` | 第 2 处：`orig:4188` 之前（`// ---------------- 联机页 ----------------`） | **+65** | （已并入上一行） |
| `Editor/MainMenuScene.cs` | 第 1 处：`orig:3941` 之后（`pw.ArmyScroll.OnChanged();  // 还原…`） | **+42** | `L ≤ 3941` ⇒ `L`；`3942 ≤ L ≤ 17444` ⇒ **`L+42`**；`L ≥ 17445` ⇒ **`L+66`** |
| `Editor/MainMenuScene.cs` | 第 2 处：`orig:17444` 之后（`换回出厂内容 ⇒ 又回到 0 段`） | **+24** | （已并入上一行） |

### 3·2 账里这三个宿主的锚点分布（现读账本主表，`_parts/` 待合并补丁里 **0 条**引用这三份）

| 宿主 | 锚点数 | 最高行 | ≤ 第一插入点 | 两插入点之间 | ≥ 第二插入点 |
|---|---|---|---|---|---|
| `BattleScene.cs` | 27（正文出现次数；按行算 25 行） | **47325**（`战斗逻辑驱动.tsv` 第 1013 行） | **1** | **18**（各 +127） | **8**（各 +176） |
| `SettingsScene.cs` | 8 | 3747 | **8** | 0 | 0 |
| `MainMenuScene.cs` | 7 | 17410 | **4** | **3**（各 +42） | 0 |

⇒ **`BattleScene.cs` 有 26 处锚点必须 +127 / +176**（含最上面那条 `:47325 → :47501`）；
`SettingsScene.cs` **一处都不用动**（最高锚点是 3747，正好在插入点之前）；
`MainMenuScene.cs` 有 **3 处需 +42**（最高 `:17410 → :17452`）。

🔴 **订正一处简报里的数字**（铁律 5）：简报写「`BattleScene.cs` 账锚点最高 `:36520`」——
`36520` 那条确实存在（`战斗逻辑驱动.tsv:692`），但**不是最高的**：最高是 **`:47325`**（同文件 tsv 第 1013 行）。
⚠️ 而且这个 47325 **落在第二个插入点之后** ⇒ 它要 `+176`，不是 `+0`。

⚠️ **`check_ledger_points.py` 现在的漂移数不能当判据**：本批跑它时另有多路代理正在改
`Core/CardView.cs` / `RuleEngine/**` 等（同一份输出两次跑，`卡组编辑` 漂移 4→1、`战斗逻辑驱动` 122→117、
失败 4→0）。**同步点那次 `--all-A --fix` 必须在「没有 `.cs` 写手」时跑**（CLAUDE.md 16·8 那条）。

---

## 四 预期可能红 / 需要跑一次宿主才能定论的断言

⛔ 本代理**没跑 Unity**（宿主会 `NewScene → SaveScene` 覆盖存盘，简报明确禁止）。下面几条是**静态能看出风险的**，
请在下一次 `BattleScene.Run` / `SettingsScene.Run` / `MainMenuScene.Run` 时先看它们：

| # | 断言 | 先判什么 | 为什么会可能红 |
|---|---|---|---|
| 1 | `AL-857`（锚点比 = 1.34） | 前提 = `CardView.EffectAnchorBoard` 对一张 `CardFace.Board` 探针卡**建得出来**（`EffectsAnchorHeight` 取不到就返 0 并出声） | 若 batch 下 3D 体/锚点节点没建 ⇒ 比值算出 0（不是 1.34）⇒ 红。这是**真缺口**（该出声的），不是假红 |
| 2 | `AL-921/922`（抬牌成对开关） | 前提 = `it.HandCardForTest(0) != null` **且** `IsDoingAnim == false` | 我在前面推了 `CardTween.Advance(0.30f)` 清重排闸；若开局发牌还挂了别的闸（`FrozenNow` / `DragOnlyBlocked`）⇒ `UpdateHover` 早退 ⇒ 悬停不生效 ⇒ 红 |
| 3 | `AL-1049`（悬停比值 1.3） | 同上 + 手牌 ≥ 2 张 | 同 ② |
| 4 | `AL-1012/1013`（督军视图形态 / 同一实例） | 前提 = `driver.BoardViewAt(BoardLayout.WarlordSlot, true) != null` | 已有断言（`BattleScene.cs:36590`）在同一条读法上做过**兜底**（拿不到就退到任意激活单位）⇒ 说明督军视图**可能为 null**；若为 null 这条直接红（这是**如实报**，不是放宽） |
| 5 | `AQ-①/Jingles`（`Dump()` 里的 dB） | 前提 = batch 下 `AudioMixer.GetFloat("VolumeFX"/"VolumeJingles")` 返回 true | 现网只在 `Core/PlayerBoot.cs:292` 打过这条；Editor batch **没有先例**。若 batch 读不到暴露参数，`Dump()` 会印 `(**)` ⇒ 红（红得对：说明 mixer 没接上） |
| 6 | `AQ-②`（窗口模式下拉） | 前提 = `Menu Area/Media Tab/Visual Settings/WindowMode Selector` **这一行建出来了** | 该行是「PC 档才显示」（`PlatformBasedComponents`）；若某档没建，`vmRow == null` ⇒ 前提那条红 + ②/⑥ 跳过（会是**红**不是静默） |
| 7 | `AQ-⑥`（切页重算帽字） | 同上 + caption `Label` 取得到 | 路径写死到 `…/Dropdown/Label`；改名就红（红得对，节点名不进本地化的口径不变） |
| 8 | `AR-④`（越界出声） | 前提 = `Application.logMessageReceived` 在 batch 下**同步**回调 | 这条会**真打一条 `Debug.LogError`**（`[ArenaSlots] 槽号越界 slot=-1…`）。`BattleScene.Run` 的退出码只看 `fail` 计数（`:45397`），不吃 LogError ⇒ 预期无副作用；但日志会多条 ERROR |

---

## 五 没插进去的：`B8_AQ` ④（飘窗 handler 路由，原本落 `Editor/ShellScene.cs`）

**两个独立的阻断，任一都够**：

1. **`Editor/ShellScene.cs` 不在本代理白名单**（简报白名单只有 BattleScene / SettingsScene / MainMenuScene）。
2. **它要的读口不存在**：草稿用 `ToastNotifications.InternalShowCount`，而 `Shell/ToastNotifications.cs` 里
   **没有任何计数器**（现读：只有 `_handlers` 表 + `ShowNotification` 的分派；`ShowNotificationInternal` 是私有实作）。
   加一个只读计数口是 **3 行**改动，但那是 `Shell/ToastNotifications.cs`（别人的白名单）——
   按 `ItemDrawer.HandleCount` 的先例，**要不要加这个口请调度台在同步点拍板**。
   ⚠️ 顺带：草稿还要一个 5 行的 `IHandler` 夹具（`TestHandler`），也得进 `ShellScene.cs`。

⇒ 建议：把 ④ 转派给 `ShellScene.cs` 的写手，并**先**决定「加不加 `InternalShowCount`」。
（断言意图本身是对的：`RegisterNotificationHandler` 命中 ⇒ handler 收 1 次、内部展示 +0；未注册 ⇒ 内部 +1。
灭自证点 = 把 `ShowNotification` 里 handler 那一支挪到内部展示之后 ⇒ 第二句红。）

---

## 六 `B8_AF` 报告里**没有**断言草稿（已逐节核）

`B8_AF_战斗窗口剩余缺口.md` 的 §①–§⑥ 逐节看过：它的「可观察量（G3）」是**读数口清单**
（`BattlePostFx.BloomIntensityNow` / `BloomThresholdNow` / `ResetBloomRamp01` / `ResetLutRamp01` / `BlendNow`），
**没有一条 `Check(...)` 草稿**；它自己也写明「我们侧实测值：本批未跑宿主 ⇒ 读数口已备」。
⇒ 本批**不为它新增断言**。若调度台要补，最小的一对是：
`ResetBloom(instant:true)` ⇒ 当拍 `BloomIntensityNow` 等于 `Start` 缓存的原值；`instant:false` ⇒ 1.0 s 斜坡单调
（判据 `PostFXController__ResetBloom.c:3-61` + `__.cctor.c` 的 `TIME_TO_RESET = 1.0f`）——
但**期望值只能取「原版缓存的那两个值」**（`bloomOriginalIntensity/Threshold`），拿 `BattlePostFx` 自己的常量比就是自证。

---

## 七 顺手发现（⛔ 只报；都没改）

1. 🔴 **`check_ledger_points.py` 不解析 `--help`**：我为了看用法跑了 `--help`，它**直接跑了整轮检查**
   （好在没带 `--fix`、只读）。这正是 CLAUDE.md 里「会写盘的脚本不带显式动作参数必须什么都不做」那条的现行违例 ——
   脚本本身值得按那条改（默认打印用法 + 退出）。
2. 🔴 **`B8_AQ` ⑤ 草稿假设的「点一格之后旧 `ArmyCells[i]` 还能用」不成立**：
   `PickArmy` 末句走 `ArmyScroll.OnChanged()` → `RebuildArmyCells` ⇒ **整列格被销毁重建**
   （与我已落地的 `:3968` 那句注释同源）。**同阵营再点之前必须重找命中节点**，否则拿到的是 Unity 假 null
   （同族先例：`MainMenuScene.cs:3544-3549` 那条注释）。
3. ⚠️ **`B8_AR` §六·1 报的「`ArenaSlots.cs` 行号过期」仍然成立**：`报告/B8_M`、`报告/B8_F` 里把
   `ArenaSlots.cs:109/110` 当「敌方取负那一行」写 —— 现读 `:109` 是 `return`、`:110` 是 `}`，
   内容已变成 `new Vector3(x, ExtraYOnBoard(enemy), z)`。建议收口时统一改符号名。
4. ⚠️ **`B8_AQ` §五·5 的节点名**：`WhereToGetCardsPopup` 的压暗层命中节点名仍是 **`"BackgroundHit"`**
   （全仓 `ShadeHit` 默认名是 `"CloseHit"`）。我按**现名**写的断言（`:17495`）；若有人把它改名，
   这条断言要同步改（一行）。
5. ℹ️ **`BattleScene.cs` 那 26 处账锚点漂移是本批**必然产物（加断言就得加行）。这不是缺陷，
   但**必须在同步点统一 `--fix`**，否则下一个人会按旧行号去找、找到的是别的语句（CLAUDE.md 里
   「`--fix` 把 15 行账改成错行、而且闸变绿」那条风险正是这种情形的下游）。

---

## 八 本次落地的判据出处（可复现）

```
B8_AR：d:/2/tools/decomp_full/MinionManager__Awake.c:59-62 / :101-103
       d:/2/tools/decomp_full/MinionManager__GetLeftSlotPos.c:25-42 · __GetRightSlotPos.c:25-42
       d:/2/tools/decomp_full/MinionManager__SlotPosToBoardPos.c:15-21
       d:/2/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json · _4373.json
       （MinionSeparation 0.82/1.53 · minionExtraDistanceFromHero 0.09/0.13 · ExtraYOnBoard 0 · slotsPerSide 4）
B8_AL：decomp_full/CardScript._AttackMeleeAnim_d__357__MoveNext.c:139-267 · CardScript__GetEffectAnchor.c:8-24
       cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:10593-10601（StartShowingCardInHand）/ :11013-11036（Stop…）
       VarsDevice.cardInHandShownScale = 1.3 · .rdata DAT_1834b3150 = 0.00015f · DAT_1834b316c = 65.0f · DAT_1834b3170 = 70.0f
B8_AQ：decomp_full/PlayerDataManager__SetMusicAndFxVolumes.c:49-50 / :64-65,79-80
       decomp_full/MediaTab__OnWindowModeChanged.c:13 · MediaTab__OnOpen.c:3-7 · MediaTab__SetupVideoSettings.c:82
       decomp_full/BackgroundCloseButton__…OnPointerClick.c:24-27 · WhereToGetCardsWindow__Awake.c:12-24
       decomp_full/PracticeModePopup__ChangeSelectedArmy.c:30-33 / :68-72
```

**插入器**（可复跑、带 `--dry`）：`d:/4/Unity/_tmp_view/b8az/insert.py`（片段在同目录 `snip_*.cs.txt`；
三份宿主改前副本在 `_tmp_view/b8az/bak/`）。
