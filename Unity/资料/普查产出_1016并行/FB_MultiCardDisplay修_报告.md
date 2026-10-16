# FB · MultiCardDisplay 修（F1/F11/F10）（修波）

> 执行：执行子代理 `FB` · 2026-10-16 · 白名单 = `Battle/MultiCardDisplay.cs` + 本报告。
> ⛔ 未跑 Unity（红线）· ⛔ 未动 git · ⛔ 未改白名单外的任何文件（含 `Editor/BattleScene.cs`）。
> ✅ 改完跑了 **秒级类型检查**：`TMPDIR=/tmp/wf_fb bash d:/4/Unity/工具/typecheck.sh` ⇒
> **运行时错误数 0 / 编辑器错误数 0**（共跑 **9** 次：5 次常规 + 4 次 `WF_DOC=1`，每次 0/0）。
> `WF_DOC=1` 档：本文件 XML doc 格式警告 **2 → 0**（运行时总 707 → 705），`<para>`/`<summary>` 计数 7/7 · 41/41 全平。

---

## ① 结论 —— 逐条

| 账 | 结论 | 一句话理由 |
|---|---|---|
| **F1**（层序与原版相反） | ✅ **做了** | 三件从「同 `ZContent`、tie 由建序定」改成**显式 z 阶梯**（`ZBtnBar > ZBtnText > ZBtnCircle`，判据 = 原版 `m_Children` 序）⇒ **圆钮画在最上**，与原版同 |
| **F11**（`BattleContinueButton` 组改走锚点） | ✅ **做了** | 三件一律 `aMin = aMax = (1,0)`（右-下**点锚**）+ 角偏移（由原版五元组现算）；**同批**把 `HitContinue` 改成「从条自己的实绘矩形现算」⇒ 16:9 下与改前**逐点同结论**、画布变宽时**判到哪 = 画到哪**。⚠️ `BatchScene` 那半（`Editor/BattleScene.cs` 的 `E2④`）**不在我白名单** ⇒ 见 ③ |
| **F10**（删过期幂等包装 + 订正注释） | ✅ **做了** | `MultiCardDisplay.cs` 这一处包装删掉、直接调 `HudCanvas.EnsureSubCanvas`（它已按名字幂等）+ 把那句**已过期**的理由就地订正（铁律 5 留痕）。同族另外 3 处（`BattleDoors`/`CardChoicePanel`/`SettingsPanel`）**不归我** |
| **顺批：`_barText` 的字位**（`R1` ②-1 / `A11` §⑥·b） | ✅ **做了（判为同一条链）** | 三件都必须搬上角锚；字若留在「面板中心 + 固定 px」会在画布变宽时**与条分家**（21:9 差 92.2 px）。⇒ 基准从「**条的右沿** − 60」改成「**原版 `Text` 框的右沿** − 60」⇒ 字**挪离圆钮**（16:9 左移 227.79 px）。⚠️ **残留误差 = 真实半字宽 − 60 px**（见 ⑤） |
| 顺带（不在账上） | ✅ **做了** | 补了 `HitContinue` 那个 doc 块**本来就缺**的一个 `</para>`（HEAD 就缺）⇒ 本文件 XML doc 警告 2 → 0 |

**没做的**（都不在我白名单 / 不在本件范围）：`Editor/BattleScene.cs` 的 `E2④` 两条 + 新增结构式断言（→ ③ 接线清单）；
`Show()` 的 `availPx`（简报明令别碰，它本身是上一轮量纲缺陷的**修法**）；同族另外 3 处 `EnsureSubCanvas` 包装；卡片迁画布（第 6 步）。

---

## ② 改动清单（`文件:行号`，行号 = **改后现核**）

**落盘核对**：`git diff --numstat -- …/Battle/MultiCardDisplay.cs` ⇒ **`177  32`**（HEAD 时该文件是**干净的** ⇒ 这 177/32 全是本件的）；
行尾：`CRLF 0 · LF 708`（改前 `CRLF 0 · LF 563`）⇒ **纯 LF 未翻**（文件本来就是 LF，未用 `sed -i`、未整篇重写）。

| # | 位置 | 改了什么 |
|---|---|---|
| 2-1 | `:37-41`（文件头 `②画布本体` 那条） | 补一段：**「继续」那三件的锚法换了**（原版那一级是右下点锚），旧写法 21:9 偏 320 px；标题/提示行不在那一组里 |
| 2-2 | `:67-73`（文件头 `④`） | 订正「右对齐近似」的**基准**：旧 = 条的右沿 − 60（偏右 227.77 px、压在圆钮上）→ 新 = **原版 `Text` 框右沿** − 60；写明残留误差 |
| 2-3 | `:113-119`（`BarCx…` 那条注释） | 「现在只剩兜底支在用」+ 16:9 自证值 `1611.439`（差 −0.011 px） |
| 2-4 | `:129-140`（`CircleCx…` 那条注释） | 同上；并**订正**「`CircleD` 也是 `HitContinue` 认的那一份」= 过期（命中区从来只按条算） |
| 2-5 | `:142-186`（**新增常量块**） | 原版组五元组 `BtnApX/BtnApY/BtnBoxW/BtnBoxH` · 组内偏移 `BtnBarDx/BtnCircleDx` · 组心 `BtnGroupCx/Cy` · **框右沿 `BtnTextFrameRx`（由五元组现算）** · 三个角偏移 `BtnBarCx/BtnCircleCx/BtnTextCx` · 两个角偏移 y `BtnChromeCy/BtnTextCy` |
| 2-6 | `:218-230`（**新增 `ZBtn*`**） | 显式 z 阶梯（`ZContent−0.01/−0.02/−0.03`）+ 判据（原版子件序）+ 「⛔ 别只改建序」那条更正留痕 |
| 2-7 | `:285-296`（`Create` 里的子画布） | **F10**：删掉「`SubCanvas` 查 + `EnsureSubCanvas` 建」那层幂等包装，直接 `HudCanvas.EnsureSubCanvas(…)` + 就地订正过期理由 + 写明唯一的行为差（已有画布会被对齐到本次的 (层,order)） |
| 2-8 | `:390-396`（`Build` 那段说明） | 补：「三件的**位置不在这里给**」（`0f,0f` 是占位，随后由 `AnchorBottomRight` 改锚）+ 层序显式给了 |
| 2-9 | `:398-408` / `:421`（建件三处） | `mcd_bar`/`mcd_circle` 的位置参数 `BarCx−960`/`CircleCx−540` → `0f,0f`（占位）；z `ZContent` → `ZBtnBar`/`ZBtnCircle`；`mcd_continue` 去掉了 `var bp = HudCanvas.PosPx(BarCx + BarW*0.5f − 60f, …)` 那两句 → `0f,0f` + `ZBtnText`，并订正基准那条注释 |
| 2-10 | `:430-433`（**新增三句调用**） | `AnchorBottomRight(条/圆钮/字, 角偏移)` —— **三件同批**改锚（⛔ 只改一两个 = 半迁移态） |
| 2-11 | `:458-481`（**新增助手**） | `static void AnchorBottomRight(RectTransform, dx, dy)` = `aMin=aMax=(1,0)` + `pivot=(0.5,0.5)` + `anchoredPosition=(dx,dy)`；doc 写明「为什么必须锚点」与「为什么是先建后改锚」 |
| 2-12 | `:592-593`（doc 块） | 顺手补上**本来就缺**的 `</para>`（只补标签，正文一字未改） |
| 2-13 | `:603-612`（`HitContinue` doc） | 新增一段：命中框改从实绘矩形现算（含「`BarCx` 在旧写法里为什么恰好抵消」） |
| 2-14 | `:616-637`（`HitContinue` 体） | 画布支：`size = HudCanvas.SizePx(_bar.Rect)` → `MenuDraw.PaddedRect(±size/2, ContinuePad)` → 局部点 `(_bar.Rect.InverseTransformPoint)` 折成 y 向下比对（**y 仍向上 = 世界 y 向上**，无坐标系翻转）；兜底支**一字未改** |
| 2-15 | `:640-650`（实绘口那段注释） | 订正「补它的理由」（原文写「硬写矩形 + 圆钮探出的那半圈」= 已过期）+ 追加 F11 的「再进一步」 |

---

## ③ 接线清单（白名单外，**我一行都没改**）

| # | 文件:行号（现核） | 该改什么 / 为什么 |
|---|---|---|
| **F11-①** | `Editor/BattleScene.cs` **`A964/E2④`** 那一段（现读 `:21435-21534`，四条 `Check` 在 `:21447-21468`） | 🔴 **本件必含的那条**。**四条现有断言的取值不用改、今天照旧绿**（16:9 / 批处理画布宽恒 1920 ⇒ 新旧写法同值 —— 也正是它们**分不出**本件修了什么的原因）。**要补**（三选一／可多补）：① **结构式**（不依赖画布宽、能挡回退）：三件的 `anchorMin == anchorMax == (1,0)`（得先把 `RectTransform` 开出来，或经现有 `ContinueBar*` 口补一个只读口）；② **角偏移式**（同样与画布宽无关）：`1920 − HudCanvas.CenterPx(条).x ≈ 308.56` / 圆钮 `≈ 156.54`（⚠️ `HudCanvas.CenterPx` 写死 1920/1080，只在本扇父件 = 整屏宽时成立，用前先看它的 doc）；③ 若想断 21:9：批处理画布宽拿不到 2560 ⇒ **只能断常量/锚点**，别去断实拍位置 |
| F11-② | `资料/普查产出_1016并行/_收口台账_第四十二会话.md` F1/F10/F11 三行 | 收口时标 ✅ 并把「F11 必须同批改 `HitContinue` + `E2④`」那句改成「`HitContinue` 已改（出处）/ `E2④` 待补结构式断言」 |
| F10 | `Battle/BattleDoors.cs:195-211` · `Battle/CardChoicePanel.cs:216-221` · `Battle/SettingsPanel.cs:657-705` | 同族剩下的 3 处包装（**不归我**）；注意 `SettingsPanel` 那处还挂着「不会去重」的过期注释（铁律 5）。⚠️ `FD` 的报告另外指认了**第 5 处** = `Battle/BattleLogPanel.cs:750-755`（同一族，也不归我） |
| 跨件（`FA` 提的） | `MultiCardDisplay.SubCanvasName`（现读 = `const string`，**非 `public`**） | `FA` 的 ②-7 新断言引用不了它、只能写字面量（`Editor` 程序集看不见它）。⇒ 若主对话要把公开面开出来（`const string` → `public const string`，**一个词的事**）**请另派**：简报明令「既有接口/公开面一个字不许改」⇒ **我没改**（与 F10 无关，别与本件混在一起） |
| 跨件（`G5` 提的） | `G5` §⑥ 那条「`cxPx − 960f` 这一族（… **`MultiCardDisplay.cs:305,312`**）」 | ⚠️ **本件之后那两处已不存在**：F11 把条/圆钮改成角锚 ⇒ `BarCx − 960f` / `CircleCx − 540f` 那两句已删 ⇒ `G5` 那一条**对本文件失效**（`CardChoicePanel.cs:308/320/412` 那几处仍在，不归我） |
| 顺带 | `Editor/BattleScene.cs:8149-8160`（`availPx = 1920f − 120f` 那对） | 简报明令别碰 ⇒ **本件没碰**。仅提示：它与 `MultiCardDisplay.cs` 的 `LayoutSpace.DesignPxW − SideMarginPx*2f` **成对**（那一行同样不许动） |
| 文档 | `资料/任务线/战斗线/任务/T09_整层换uGUI.md`（§五·补⑨/⑩ 一带） | 若那份要记「本扇的件现在走角锚」⇒ 由主对话决定措辞（我不改正本） |

---

## ④ 判据与出处 ＋ 算式

### 4·1 判据（2026-10-16 **我自己现读**，不是转抄）

复现命令（脚本我放在临时目录，逻辑 = 「GO 名 → `m_Component` 里的 RT → 递归 `m_Children`」，因为本包的 `GameObject_<pid>.json` **有缺**、但 `<GO名>.json` 有）：

```text
python -I <脚本> bundle_scenes_scenes_battlearena2 3251 2      # 组 + 三个子件
python -I <脚本> bundle_scenes_scenes_battlearena1 3058 2      # arena1 同一个组
python -I <脚本> bundle_scenes_scenes_battlearena1 3175 1      # 面板 + 它的子件序
```

| 件 | RT（arena2 / arena1） | 实读值 |
|---|---|---|
| 组 `BattleContinueButton` | **3251 / 3058** | `aMin = aMax = (1,0)` · `piv (1,0)` · `ap (−24.2258, −53)`（a1: −24.2256） · `sd (832.89, 49.84)` · `scale (1,1,1)` · 父 = `3370`（面板） |
| 组 `m_Children` | — | **`[3543 Button] · [3020 Text] · [3579 CircleButton]`**（a1 逐位同） |
| 组那个 GO 的组件 | — | **只有 `RectTransform` 一条**（`GameObject/BattleContinueButton.json`）⇒ **无图形** |
| `Button` | 3543 / 3348 | `a(0.5,0.5)` · `sd (577.5002, 63.84)` · `ap (132.11, 0)` |
| `Text` | 3020 / 2827 | `aMin(0,0)` · `aMax(0.7702433466911316, 0.9)` · `piv(0.5,0.5)` · `ap (145.099945, 0)` · `sd (−354.200012, 0)` ⇒ 框 **287.33 × 44.856** |
| `CircleButton` | 3579 / 3384 | `a(0.5,0.5)` · `sd (80.4687, 79.6356)` · `ap (284.1295, 0)` |
| 面板 `Generic Multi Card Display Combat` | 3370 / 3175 | `aMin(0,0.5) aMax(1,0.5) piv(0.5,0.5) ap(0,0) sd(0, 818.04)` · `m_Children = [Menu Dark Background] · [Header Text] · [Viewport] · [BattleContinueButton]` |

> ⚠️ **两场（arena1 / arena2）逐值同** ⇒ 不是「两场两种做法」；`ap.x` 的 `−24.2256 / −24.2258` 是导出的取整（本件常量取 arena2 那一份）。

### 4·2 **F11 的 16:9 / 4:3 / 21:9 算式**（自证）

- uGUI 通式（件心，画布左起 px）：**新（点锚）= 画布宽 + 角偏移**；**旧（相对面板中心）= 画布宽 / 2 + 中心偏移**
  （面板本体是 `aMin(0,0.5) aMax(1,0.5)` 的横向 stretch ⇒ 它的中心恒在画布中心）。
- 三个角偏移（C# `const float` 的实际值，与 double 逐位推导**差 0.0000 px**）：
  `BtnBarCx = −308.5608` · `BtnCircleCx = −156.5413` · `BtnTextCx = −307.5878`（= 框右沿 `−223.362` − 60）
- **16:9（画布 1920）**：条 `1920 − 308.5608 = **1611.439**`（旧 1611.45 ⇒ 差 **−0.011**）·
  圆钮 `= **1763.459**`（旧 1763.45 ⇒ **+0.009**）· 字 `= **1612.412**`（旧 1840.20 ⇒ **−227.788**）
  ⇒ **条/圆钮与「我们原来那套值」相等（0.01 px 级）** ✅ 这就是简报要的那条自证。
- **4:3（屏 1440×1080，`Expand` ⇒ 画布仍 1920×1440）**：与 16:9 **同值** ⇒ **这一档分不出两套写法**（同 `A11` §④）。
- **21:9（画布 2560）**：条 `2560 − 308.5608 = **2251.439**` vs 旧写法 `1280 + 651.45 = 1931.45` ⇒ **+319.989** ·
  圆钮 `2403.459` vs `2083.45` ⇒ **+320.009** · 字 `2252.412` vs `2160.20` ⇒ +92.212
  ⇒ 闭式 **差 = (画布宽 − 1920)/2 = 320 px** ✅（= 本件修掉的那一格，与 `A11` §⑥-a / `R1` ②-11 的表逐值吻合）。
- 另一条自证（y）：条心 y（相对面板**底边**）= `−28.08` ⇒ 条顶 = 底边上方 3.84 px ⇒ 左上原点 y = `949 − 3.84 = 945.16`、
  条底 = `949 + 60 = 1009` ⇒ 与文件头那两个原版值 `y[945.2, 1009]` 逐位吻合 ✅。

### 4·3 **F1 的「改后谁压谁」对照**（`HudCanvas.Add`：z **大 ⇒ 画在底下**；`Group.Add` 按 z 降序排、同 z 插末尾）

| 件 | 原版子件序（判据） | 改前（同 `ZContent`、按建序） | 改后（显式 z） |
|---|---|---|---|
| `Button`（条 `mcd_bar`） | `[0]` 最下 | sibling 0 ✓ | `ZContent−0.01` ⇒ sibling **0** ✓ |
| `Text`（字 `mcd_continue`） | `[1]` 中 | sibling **2** ⇒ **画在最上 ✗** | `ZContent−0.02` ⇒ sibling **1** ✓ |
| `CircleButton`（圆钮 `mcd_circle`） | `[2]` 最上 | sibling 1 ⇒ 中 ✗ | `ZContent−0.03` ⇒ sibling **2** ✓ |
| 标题 `mcd_header` | 在组**之前**（面板子件序 1/4） | 同 z、建在组前 | `ZContent`（**严格大于**三件 ⇒ 在整组之下）✓ |
| 提示行 `mcd_hint`（我们自加） | 原版无此件 | 同 z、最后建 ⇒ 最上 | `ZContent`（在组之下）—— 无重叠、无画面后果 |

> 插入序已按 `Group.Add` 的算法逐条演算过：算出的最终 `siblingIndex` = 上表「改后」那一列。

### 4·4 其它判据

- **`ContinuePad`**：条那颗 `Image` 自己的 `m_RaycastPadding = (0,−40,0,−40)`（L,B,R,T，**负 = 外扩**）⇒ 上下各 +40、横向不动（`A1061` 已核，本件沿用那一份算式 `MenuDraw.PaddedRect`）。
- **不乘缩放**：`RT_3251` 与父链（`3370` → `3055`）`m_LocalScale` 全是 `(1,1,1)` ⇒ 角偏移不除任何缩放 ✅。
- **`HitContinue` 的等价性**：旧式 `px = BarCx + l.x` 与 `BarCy − l.y` 里那个 `BarCx/BarCy` 在比较时**互相抵消**
  （矩形也以 `BarCx/BarCy` 为中心）⇒ 旧式**等价于**「在条自己的局部系里比 ±(577.5/2, 63.84/2+40)」——
  这正是新式写的东西 ⇒ **16:9 下逐点同结论**（E2④ 那四条：内侧中 / 外侧不中 / 左沿外 20 不中 / 上沿外 20 中，**全部照旧成立**）。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. **「继续」两字的真实字宽没量**（`Label.WorldW` / TMP `textBounds` 的真值要**真机跑到激活态**才准；本件红线不跑 Unity）。
   ⇒ `BtnTextCx` 里那个 `− 60`（半字宽近似）**仍在**，残留误差 = **真实半字宽 − 60 px**。
   ⇒ 精确修法有两条、**都没试**：① 真机量宽后把 `BtnTextCx` 的 `−60` 换成真实半宽（**只改这一处常量**）；
   ② 走 `Label.AlignRightOn`（`Battle/Label.cs:1331`）—— ⚠️ 它收的是**世界 x**、且内部走 `ParentXInDesignSpace()`/`WorldW`，
   **这两者在 uGUI 档的口径我没查证**（`AlignRightOn` 今天全仓的调用点都在 Shell 菜单那侧）⇒ 要用得先查。
2. **未实拍任何一档画面**（红线不跑 Unity）⇒ 21:9 的「看起来对不对」是**算式结论**，不是实拍结论。
3. **`CircleCx / CircleCy` 现在是「无引用的常量」**（如实记）：本件把摆件改成角锚之后，代码里一处都不引用它们；
   保留理由 = 留一条「原版在 16:9 下的绝对位置」记录（`BarCx/BarCy` 不同：它们仍在 `HitContinue` 的兜底支里用）。
   typecheck **0 错误**（这一档不报未用常量）。若主对话想清掉，删这两颗即可、无引用点。
4. **`E2④` 那四条断言分不出本件修的东西**（16:9 同值）⇒ 见 ③，需主对话补**结构式**断言（我不在 `BattleScene.cs` 里动手）。
5. **卡片那一支仍是世界空间**（`CardView.SetPose` 收世界坐标）⇒ 非 16:9 下卡片与画布那组图/字会分家 ——
   这是**既有**边界（随第 6 步「手牌/场上卡迁 uGUI」消失），本件**没碰**，也不是本件的缺口。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **`HudCanvas` 已经有「点锚 + 该角偏移」的共用式（`AnchorApPx` / `SetAnchorPx`），但本扇用不上** ——
   它们的式子把**父矩形当成整块 1920×1080**（doc 逐字：「点锚指的是**父矩形（= 画布）**的那个角」、
   `xPx/yPxFromTop` 是**屏幕绝对** px）⇒ 只在**父件铺满画布**时成立；本件的父件是**1920×818 的横带**（`_panel`）
   ⇒ 套上去会**静默摆错**。⇒ 我在 `AnchorBottomRight` 的 doc 里**就地写明了这段判别**（免得被当成「同一条规矩的第二份写法」）。
   ⚠️ 仍然成立的一半：`MakeImageInParent` / `MakeText` **只出中心锚** —— 以后遇到「**父件不是整屏 + 要角锚**」的件，
   本函数这一份就会开始涨 ⇒ **建议收进 `HudCanvas`**，但收的必须是「**任意父矩形的角锚**」那个口
   （`ap` = 相对**父件那个角**的 px，父矩形由 Unity 算）—— 它与 `SetAnchorPx`（**假定父件 = 整屏画布**）是两件事，**别合并**。
2. ⚠️ `Label.CreateCore`（uGUI 档）用 `go.transform.localPosition = pos`（**不是** `anchoredPosition`）——
   `Label.cs` 自己已就地订正过「那只在**父件 pivot 居中**时才等于相对父中心」。本扇 `_panel` 的 pivot 是 `(0.5,0.5)` ⇒ **今天无影响**；
   但**本件之后这三件的 `anchor` 已不是中心** ⇒ 将来谁要给它们做「按父中心摆位」的活，得用 `anchoredPosition`，别用 `localPosition`。
3. ⚠️ **条那颗 Image 的 `m_RaycastTarget`**：原版是 `1`（唯一可射线件），而 `HudCanvas.MakeImageInParent` **恒 `raycastTarget = false`**
   （那一族是装饰件的口径）。今天的命中走 `MultiCardDisplay.HitContinue` **手算**（生产路径不调它，只有自检用）
   ⇒ 无影响；但**子画布上挂着 `GraphicRaycaster`** ⇒ 若将来真接 uGUI 射线，这三件**一颗都不吃**（要另给一条 `raycastTarget = true` 的口）。
4. ⚠️ 同族包装的三处（`BattleDoors` / `CardChoicePanel` / `SettingsPanel`）**注释都还写着「不会去重」**——
   `CardChoicePanel` / `SettingsPanel` 那两处的注释我**顺手核过**、确实过期（与 `HudCanvas.EnsureSubCanvas` 的 doc 冲突）；
   `BattleDoors` 那处写的是「每调一次就新建」（同样过期）。⇒ 归 ③ 那张表。

---

### 可直接粘进总表的一行（≤3 行）

> **修波 `FB`（F1/F11/F10，2026-10-16）**：`MultiCardDisplay.cs` **+177/−32 · 纯 LF 未翻 · typecheck 0/0（跑 9 次：5 次常规 + 4 次 `WF_DOC=1`）**。
> ✅ F1 = 三件改**显式 z**（`ZBtnBar>ZBtnText>ZBtnCircle`，判据 = 原版 `RT_3251.m_Children = [Button]·[Text]·[CircleButton]`）⇒ **圆钮在最上**；
> ✅ F11 = 三件改走**右-下点锚**（`aMin=aMax=(1,0)`，角偏移由五元组现算：条 −308.5608 / 圆钮 −156.5413 / 字 −307.588）+
> **同批**把 `HitContinue` 改成从**条自己的实绘矩形**现算 ⇒ 16:9 与旧值差 **0.01 px**、21:9 修掉 **320 px**；✅ F10 = 删掉那层过期幂等包装 + 订正注释；
> ✅ 顺批订正 `_barText` 的右对齐基准（旧 = 条的右沿 ⇒ 字压在圆钮上，现 = 原版 `Text` 框右沿，16:9 左移 **227.79 px**，残留 = 真实半字宽 − 60）。
> ⏭ **接线（白名单外，未改）**：`Editor/BattleScene.cs` 的 `A964/E2④` **要补结构式断言**（现有四条 16:9 下与改前同结论 ⇒ 分不出本件）。
