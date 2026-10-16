# T09 · UnitChatPanel + ChatPopupPanel 迁 uGUI（执行代理 A1）

> 日期 2026-10-16 · 白名单 = `CardPresentation/Battle/UnitChatPanel.cs` + `CardPresentation/Battle/ChatPopupPanel.cs` + 本报告。
> ⛔ 未跑任何 `*.Run` / 未起 player / 未动 git / 白名单外一个字节未改（`git status` 里另几个 `.cs` 是别的代理在飞的）。
> ✅ `TMPDIR=/tmp/wf_a1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**（跑过 3 次，最后一次含我这批的全部改动）。
> ⚠️ 中途那一次报过 `AttackSelector.cs(810,25) CS0103 AnchorLocal` —— 那是**另一个代理写到一半的文件**（本批 `SkillPanel`/`AttackSelector` 有别的代理在做），复跑即消失，**不是本件引入的**。

---

## ① 结论

1. **两扇都改成【双模式】**（主对话硬要求；先例 = `ErrorMessageBanner.Create(…, uguiHost)` / `Core/Tooltip.UseHost(host)`）：
   · **宿主传 `null` ⇒ 世界档**，与改之前**逐位相同**（位置走 `LayoutSpace.ToWorld`、尺寸 ÷108、字走 `Label.Create`，`MenuDraw.SetPxSize` 那句原样留着）；
   · **宿主非 null（= 调用方给的那块子画布的 `RectTransform`）⇒ 画布档**：所有图/字建在
     `HudCanvas.EnsureSubCanvas(null, "Unit Chat Canvas" / "ChatPopup Canvas", OverHand, 0)` 那块子画布上，
     坐标 = **设计 px**、缩放交给根画布的 `CanvasScaler(1920×1080/Expand)`。
   · ⛔ **两边都建**是禁止的（半迁移态在 16:9 下看不出来）；**宿主不在任何 `Canvas` 下 ⇒ 出声 + 这一件不建**（⛔ 不退回世界档）。
2. **两扇现在【未接线】**（`BattleDriver.cs` 在白名单外）⇒ 跑起来仍是世界档、行为与今天**逐位相同**。
   接线只要**两行**（各一条 `EnsureSubCanvas` + `Create(…, cv)`，见 §③·1）。
3. **子画布不自己建**：由调用方建/取后当宿主传进来（口径 = 批 2 的 `ErrorMessageBanner`）。
   名字/层/order 都是**两扇自己给出的公开常量**（`SubCanvasName` / `SortingLayerId` / `SortingOrder`），⛔ 调用方别另写字面量。
4. **输入路径一个字不用改**：两扇的对外面仍是 `Contains/ButtonAt/PointerDownAt/PointerUpAt(Vector3 **world**)`
   —— 画布档的换算收在 `Hit` 里（世界点 → 本面板设计 px，`InverseTransformPoint`），
   而 `BattleDriver:4114/4122/4126/4171/4172` 喂进来的正是 `WorldPointer()`（世界点）⇒ **接线时那条路不动**（与“看得见的地方点得到”同源）。
5. 一条 🔴 **要主对话裁的既有缺陷**（**不是**本次迁移引入的）：`ChatPopupPanel` 那套**世界 z 与原版层序是反的**（判据 4 条，见 §⑤·2 / §⑥·1）。
   **画布档我按【原版子件序】排**（⛔ 没沿用那套世界 z —— 沿用会让「色块压住六颗钮」在画布上变成**确定性**结果）；
   **世界档那一份一个字节没改**（照「逐位相同」）。⇒ 两档的先后**在这一扇上不同**，请裁定是否把世界档那 6 个常量一并修。

---

### 🔴 更正（2026-10-16 · 收口波执行代理 `G4` · 铁律 5 —— **原文一字未删**）

> ⚠️ **按本报告写断言的人先读这一段** —— 下面这个错数**会把期望值带偏 `123.5 px`**。

**§⑤·1 那张表里 `EnemyChatDisplay` 那一行的数值是错的。**
- **原文（本文 §⑤·1，`:191`，仍保留在上面）**：
  原文行（原样）：「| `EnemyChatDisplay`（**左上角**） | `aMin=aMax (0,1)` · `piv (0,0)` · `ap (12.61328125, −410.0)` · `sd (648.77404785, 236.50463867)` | 410 / 646.5 | 353.5 / 590 | **我们偏上 56.5 画布 px = 42.4 屏 px** |」。
- **错在哪**：`EnemyChatDisplay` 的 `m_Pivot = (0, 0)`（= 矩形**左下角**），而 `m_AnchoredPosition.y = −410` 是
  **自画布上沿量到那个左下角**的量 ⇒ 矩形**自画布顶**算 = `410 − 236.505 = 173.495`。
  `A1` 那个 `410 / 646.5` 等价于**把 `pivot (0,0)` 当成 `(0,1)` 用**（`410` 当上沿、`+236.5` 当下沿）。
- **正确值（4:3 · 画布 `1920×1440` · 画布 px 自画布顶）**：原版 = **`173.5 / 410`**；我们（改前）= `353.5 / 590`；
  差值 = **我们偏下 `180` 画布 px = `135` 屏 px**（**与另两颗气泡同值** ⇒ 主对话那句
  「三颗气泡上下沿各差 **180** 画布 px」**是对的**）。`A1` 写的「偏上 `56.5`」既**方向错**、**数也错**
  （错在拿 `410` 当原版上沿：`180 − 56.5 = 123.5` ⇒ 照它写断言会偏 `123.5 px`）。
- **另三行**（`PlayerChatDisplay` / `RadioChat` / `ChatPopup`）`A12` **逐值复核过 ✓**，不用改。
- **出处** → `资料/普查产出_1016并行/T09_UnitChat_锚点与层序补正_报告.md` §⑥·1（`A12` 现核；
  该报告 §④·一 的表**已按正确值实现**）。
- ⛔ **别照 `A1` 那一行去写断言**；画布档要读锚点真值请用 `A12` 新加的**灭自证读回口**
  `chat.BubbleAnchorOf(side)`（期望值取**原版 JSON**：`(0,0)` / `(0,1)` / `(1,1)`）。

---

## ② 改动清单（文件:行号 + 一句话）

### `Battle/UnitChatPanel.cs`（+209 / −24）

| 行 | 一句话 |
|---|---|
| `:34-62` | 文件头新增 `T09` 批 3 那一整块（原版宿主那一行 / 双模式 / 单位口径 / **两条如实记的差异** / **本扇零射线件**） |
| `:104-122` | 新增 `SubCanvasName = "Unit Chat Canvas"` · `SortingLayerId = OverHand` · `SortingOrder = 0` · `bool _ugui` · `IsUgui` · **`U(px)`**（世界档 ÷108 / 画布档 ×1） |
| `:128` | `class Bubble` 的三颗图 `ImageQuad` → **`HudPiece`** |
| `:243-282` | 新增三个自检口：**`BubbleSizePxOf(side)`**（本档单位；画布档读 `sizeDelta`）· **`BubbleCenterPx(side)`**（设计 px；画布档 `HudCanvas.CenterPx` / 世界档 `ToDesignPixel`）· **`OnCanvas`**（灭自证：三颗的图与字都在画布父链下） |
| `:284-292` | `Create(parent)` → 转发 `Create(parent, null)`（世界档） |
| `:295-325` | 新增 **`Create(parent, uguiHost)`**：宿主校验（不在 `Canvas` 下 ⇒ 出声 + 不建）· 世界档 `MenuDraw.SetPxSize(1920,1080)` / 画布档 **`HudCanvas.FillParent`**（原版 `Unit Chat` = `anchor (0,0)-(1,1)` + `sd (0,0)`）· `_ugui` 在 `Build()` **之前**赋值 |
| `:355-381` | `MakeBubble` 里的字两档分支：世界档 `Label.Create` / 画布档 **`HudCanvas.MakeText`**（设计 px）；字号 **`U(OrigFontPx)`** |
| `:401-412` | `Quad(...)` 改双档（返回 `HudPiece`）：画布档 **`HudCanvas.MakeImageInParent`**（中心锚 + 设计 px，宽仍由贴图比例来）；世界档**逐字不变** |
| `:441-466` | 新增 **`Move(HudPiece…)/(Label…)`**（摆位分支**只此一处**：画布档 `PosPx` + **不写 z**；世界档 `Spot(…, z)`） |
| `:468-489` | `Place` 改走 `Move`（拓扑/顺序/Aspect 一个没变） |
| `:513` / `:529` | `Speak` 里两处：`SetGlyphHeight(U(33))` · `SetAutoFitBox(U(TextW), U(TextH), 10, 33, 36)`（**框按档取量纲**；`minPx/maxPx/basePx` 三个**本来就**是 px、不换） |

### `Battle/ChatPopupPanel.cs`（+273 / −36）

| 行 | 一句话 |
|---|---|
| `:36-64` | 文件头新增 `T09` 批 3 那一整块（原版宿主那一行 · **层级差异** · **✅ 几何自核** · 两条第 4 步欠账） |
| `:146-161` | 新增 `SubCanvasName = "ChatPopup Canvas"` · `SortingLayerId` / `SortingOrder` · `bool _ugui` · `IsUgui` · `_frame`（= 本节点自己的 `RectTransform`，`PxOf` 用） |
| `:170-197` | 🔴 **两套层序**：`ZClose…ZBtnText`（**世界档那一份，值一个字没改**）＋ **`UZClose…UZBtnIcon`（uGUI 档 = 原版子件序那一份）**，两条 doc 都把判据与理由写死 |
| `:203-210` | `Btn.bg/icon` 与 `_bg/_frameX/_close` 全改 **`HudPiece`** |
| `:250-253` | `Create(parent)` → 转发 `Create(parent, null)` |
| `:261-285` | 新增 **`Create(parent, uguiHost)`**：宿主校验 · 世界档 `MenuDraw.SetPxSize(815.044,475.470)` / 画布档 **`HudCanvas.SetPxSize`**（同一对数、真 px） |
| `:294-338` | `Build`：`_close` 双档 · `Make(…)` 收 **uGUI 档的兄弟序键** · 6 颗钮的字两档分支（世界档 `Label.Create` / 画布档 `HudCanvas.MakeText`） |
| `:347-355` | `Make(name, art, uguiZ)` 双档（缺图两档都返 `null` = 旧行为） |
| `:375-415` | `Put(q, r, z)` 双档：画布档 `PosPx` + **`sizeDelta` 直接吃 px**（= 旧 `SetWorldHeight + SetAspect` 的同一结果，少一次乘除）· 世界档**逐字不变** |
| `:430-452` | `RefreshLayout`：字那颗两档分支（画布档 **30 设计 px**、世界档 `30 × VisibleHeight/RefH`）· z 字面量收成常量 |
| `:511-519` | `SetAlphaOn(ImageQuad)` → **`SetAlphaOn(HudPiece)`**（两档同一个口） |
| `:566-584` | `Hit(Rect, world)` **static → 实例**、加画布档那一支（世界点 → 设计 px 再比绝对矩形）；世界档**逐字不变** |
| `:587-598` | 新增 **`PxOf(world)`**（世界 → 本面板设计 px；⛔ 不是 `LayoutSpace.ToPixel` 那一族） |
| `:603-621` | 新增 **`ButtonWorldPos(i)`**（自检用；**由 `rect` 算、不读可能缺图的 `bg`**；世界档那句与旧的 `LayoutSpace.ToWorld` **逐位同式**）。断言⛔ 别再用 `LayoutSpace.ToWorld(ButtonRect…)` 自己换算 |
| `:624-642` | 新增 **`OnCanvas`**（灭自证：7 张图 + 6 颗钮的字都在画布父链下） |

---

## ③ 接线清单（白名单外，⛔ 我一个字节没改）

### ③·1 `Battle/BattleDriver.cs` —— **登记宿主（两扇跑在画布上的前提）**

```csharp
// ── 现核 :14485（`_unitChat = UnitChatPanel.Create(root);`）─────────────────────
            HudCanvas.Ensure(LayoutSpace.Cam);          // 幂等；见下面那条 ⚠️（本处【必须】自己兜底）
            _unitChat = UnitChatPanel.Create(root,
                HudCanvas.EnsureSubCanvas(null, UnitChatPanel.SubCanvasName,
                                          UnitChatPanel.SortingLayerId, UnitChatPanel.SortingOrder));

// ── 现核 :15088（`_chatPopup = ChatPopupPanel.Create(root);`）───────────────────
            HudCanvas.Ensure(LayoutSpace.Cam);          // 同上
            _chatPopup = ChatPopupPanel.Create(root,
                HudCanvas.EnsureSubCanvas(null, ChatPopupPanel.SubCanvasName,
                                          ChatPopupPanel.SortingLayerId, ChatPopupPanel.SortingOrder));
```

- ⚠️ **那句 `HudCanvas.Ensure` 不是多余的**：`:14430`（`AboveShader`）· `:14624`（`LoadingMenu`）· `:15193`（`TooltipManager`）·
  `:15240` · `:15487`（错误条）五处 `EnsureSubCanvas` 之前**都没有** `Ensure`，全靠 `BuildEnergyLights()`（`:14073`）
  → `AddEnergyLight` 内部的 `Ensure` 兜，而**它在本轮会提前 return**（Glow 贴图缺失那档，`:16155` 那条警告就是它）⇒ 不补这一句，
  两扇会在那一档**退回世界档**（`EnsureSubCanvas` 会打一条 error，**不是静默**，但仍然是半迁移态）。
  （同一条发现 ⇒ §⑥·3；`SkillPanel` 代理独立报了同一条。）
- ⚠️ **`BattleDriver.cs:16223` 的 `if (_unitChat != null) _unitChat.RefreshLayout();` 不用摘** ——
  ① 世界档仍然要它重锚；② 画布档下它**幂等**（`Put` 把同样的 px 再写一遍）。ChatPopup 本来就不在 `ReanchorHud` 里。
- ✅ **四个注册表无事可做**：`_hudLabels/_hudSpots/_hudImages/_hudImageSpots` 里**本来就没有**这两扇的件
  （那两个注册表只由 `HudImage/HudAbsTex` 那一族填，`grep` 现核）。
- ✅ **输入那条路一个字不改**（`HandleChatPopup` / `PollInputEdges` 喂的是 `WorldPointer()`，见 ①·4）。

### ③·2 `Battle/HudCanvas.cs` —— `SubCanvasRank` 插两条（同层同 order 的先后就靠它）

在 `:470-483` 那个 `OverHand` 段的**最前面**（**在 `"Mulligan Canvas"` 之前**、`SkillPanel` 那条之后）插：

```csharp
            "Unit Chat Canvas",                 // 2/14
            "ChatPopup Canvas",                 // 4/7（在 `Unit Chat` 内部 ⇒ 画在它之上）
```

- 判据 = **原版 `Safe area FrontCanvas` 的 14 个子件序**（`T09c` §一 那两行 / `T09` §八 开头）：`[2] Unit Chat` 在 `[3] Mulligan` **之前**；
  `ChatPopup` 是 `Unit Chat` 的**子件**（`RT_3280.m_Children` 第 **4/7** 个）⇒ 按「子件画在父件之上」⇒ 紧跟 `Unit Chat` 之后。
- ⚠️ `:464` 那句注释**已过期**，要一起改：现在写「`SkillPanel`(1) / `Unit Chat`(2) **还没迁**，迁了要插到前面」
  ⇒ 本批 `SkillPanel` 与 `Unit Chat` **都已迁**，只剩 `AttackSelector`(0) 没迁。
- ⛔ 本件**没有别的 `HudCanvas` 缺口**：两扇用的都是现成口（`MakeImageInParent` / `MakeText` / `PosPx` / `FillParent` / `SetPxSize`），
  **没有**自造 `ImageInParent` / `MakeSlicedPiece+border=0` 那种权宜件（`T09` §七·10 那条本件**不欠**）。

### ③·3 `Editor/BattleScene.cs` —— 该改/该加的断言（**6 处要改 + 1 组新增**）

| 位置 | 处置（逐条） |
|---|---|
| `:11951-11956` | 气泡中心那条：`var want = LayoutSpace.ToWorld(…)` + `BubbleCenterWorld(0)` ⇒ 改成 **`chat.BubbleCenterPx(0)`** 比 **原版设计 px `(337.0003, 761.7524)`**（= `RT_3231` 的 `ap (12.61328125, 200)` + `sd (648.774, 236.505)` 换算出来；容差 **0.05 px**）。🧨 理由：`LayoutSpace.ToWorld` 是**世界帧**的期望值，画布档活在**画布帧**（批处理下两轴系数 x 1/108 · y 1/144 ⇒ y 差 1.333 倍） |
| `:11957-11959` | `sz = chat.BubbleWorldSize` + `sz.x*108f` ⇒ 改成 **`chat.BubbleSizePxOf(0)`**（期望仍 `648.77 × 236.50`、容差 0.6）。🧨 同上，`WorldW/H` 交的是世界单位 |
| `:12042` | `float wPx = lb.WorldW * 108f, hPx = lb.WorldH * 108f;` ⇒ 乘子改成 **按档取**：`float k = chat.IsUgui ? 1f : 108f;`（`Label.WorldW/H` 在 uGUI 档**本来就是画布 px**；同 `ErrorMessageBanner` 那条 `WorldW * 1f` 先例） |
| `:12296-12301` | `var w = LayoutSpace.ToWorld((r.x+…)/1920f, …); pdrv.ChatClickAt(w)` ⇒ 换成 **`pdrv.ChatClickAt(pop.ButtonWorldPos(i))`** |
| `:16551-16552` | A218 探针：`UnitChatPanel.Create(a218root.transform)` ⇒ 传宿主 `HudCanvas.EnsureSubCanvas(null, UnitChatPanel.SubCanvasName, …)`；期望值 `1920f, 1080f` ⇒ **`a218CvW, a218CvH`**（`box()` 认得出画布子件、会自动走「直接比 px」那一支；文案补一句「stretch ⇒ 铺满父画布」）。同 `MulliganPanel`/`EndPanel` 那几行 |
| `:16589-16590` | A218 探针：`ChatPopupPanel.Create(a218root.transform)` ⇒ 同样传宿主；**期望值 `815.04f, 475.47f` 不用改**（画布档 `sizeDelta` 吃的就是这对 px） |
| `:20898-20905` | E2 那 6 颗钮：`var wp964 = LayoutSpace.ToWorld((rc.x+…)/1920f, …)` ⇒ **`pop964.ButtonWorldPos(i964)`**（`rc964` 仍可用来数「几颗」） |
| **新增一组**（建议挂在 `:12100` 前后 / 与 `ChatPopup` 那个块里） | ① 两扇子画布的 `overrideSorting == true` · `sortingLayerID == HudCanvas.SortingLayer.OverHand` · `sortingOrder == 0`；② 子画布 **`GetComponent<CanvasScaler>() == null`**（原版全场景只有一个、在根上）；③ 🔑 **灭自证**：`chat.OnCanvas` / `pop.OnCanvas` 为真 **且** 「世界档时 `OnCanvas` 必须为假」（写成两条：`chat.IsUgui 或 !chat.OnCanvas` / `pop.IsUgui 或 !pop.OnCanvas`，反面那条防「把 `OnCanvas` 写成恒真」）。⚠️ 期望值取**判据侧**（`HudCanvas.SortingLayer.OverHand` / 原版 order 0），⛔ 别读 `HudCanvas.SubCanvas(name).sortingOrder`（那是拿实现证明实现）。⚠️ 判画布档的这几条**必须**建完宿主之后跑（A218 那两处就是现成的落点） |
| `:12311` | 「点面板外」那句 `LayoutSpace.ToWorld(0.9f, 0.5f)` **不用改** —— 逐值核过：世界 (7.11, 0) 在画布帧里折成设计 px x≈1728 > 面板右沿 815 ⇒ 两档都落在面板外 ✓ |

> ⚠️ **A218 那两条探针会把面板建在画布上而不销毁** ⇒ 顺手泄漏两个子画布节点（既有账 `T09` §七·12 的老账，本次**没加**新的泄漏源）。

### ③·4 `Battle/HudCanvas.cs`（第 4 步的欠账）——**`raycastTarget` 在本扇有真实不一致**

- 🔴 **`HudCanvas.MakeImageInParent` 出厂写死 `img.raycastTarget = false`**，而原版 `ChatPopup` 这 7 处全是 **1**：
  `CloseChatPopup` 的 `Image`（`MB 4409`）· 每颗 `ChatButton` 行的**透明命中条**（`MB 4947`，`m_Color.a = 0`）·
  每颗行的可见底图 `bg`（`MB 5202`）（三处都是 `battlearena2` 现读，`m_RaycastTarget = 1`）。
  ⇒ 第 4 步（迁按钮族 / 退两条手搓沿）时要么给那个口加一个 `raycast` 形参、要么在调用点补一句 `img.raycastTarget = true`。
  **本件没做**（本件的命中仍是手搓，`raycastTarget` 今天不影响任何行为）。
- ✅ **`UnitChatPanel` 无此问题**：全仓它**没有任何** `Contains` / `Hit` / `*Hit` ⇒ **零射线件**。

### ③·5 不需要做的（已逐条核过）

- `ReanchorHud` 与 4 个注册表：见 ③·1（**保留**，不进则退）。
- `BattleDriver.AdvanceTimeline`：两扇的时间推进（`Advance`）**本来就在** `:11769` / `:11813` 那个泵上，本次没加新的随时间推进物。
- 两扇的层级（谁会挂在谁下面）：**没动** —— `ChatPopup` 我们这边仍是 `UnitChatPanel` 的**兄弟**（原版是它的**子件**），差异如实记在文件头，⛔ 没顺手倒过来（`T09` §五·补⑩ 二）。

---

## ④ 判据与出处

1. **原版宿主那两行** = `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一：
   · `UnitChatPanel` → `Unit Chat` @ `…/Safe area FrontCanvas`，`(rm 继承 FrontCanvas(1), OverHand, 0, 兄弟序 2/14)`，出处 `RT_3280` · `MB_5422` · `MB_4450`；
   · `ChatPopupPanel` → `ChatPopup` @ `…/Safe area FrontCanvas/Unit Chat`，`(继承 FrontCanvas(1), OverHand, 0, 兄弟序 4/7 —— 7 是在 Unit Chat 内部数的)`，出处 `RT_2881` · `MB_4429`。
   §三 的两条裁定（`WaitBanner` 归 `WaitText` · `AboveShader` 归 `TipText` 那一族）**不涉及本两扇**。
2. **我逐节点现核了一遍**（`bundle_scenes_scenes_battlearena2`，读 `RectTransform_*.json` + `GameObject/*.json` 的 `m_Children`）：
   · `Unit Chat`（RT 3280）= `aMin(0,0) aMax(1,1) piv(.5,.5) ap(0,0) sd(0,0)` ⇒ **stretch 铺满** ⇒ 我按 `FillParent` 复刻 ✓；
   · 三颗气泡（RT 3231 / 3375 / 3439）= `sd 648.774 × 236.505`，与本源文件头那三个 `Slot` **逐位对得上** ✓；
   · `ChatPopup`（RT 2881）= `aMin=aMax(0,0) piv(0,0.5) ap(0, 479.81005859375) sd(815.0440063476562, 475.4700012207031)`；
   · ✅ **几何自核**：`ChatPopup → CloseChatPopup(3500, ap 2.45/41.66, sd 4055.34×2114.44)` + `ChatButtons(3363, anchor(0,0), ap 371/206, sd 722.66×403.13)` →
     里面 `BG(2860, 中心锚, ap 28.816/5.615, sd 557.314×301.346)` 与本件 `Bg` 那条（557.31×301.35）**逐位吻合**（我按 `ChatButtons` 的 `ap` 倒推得 28.815/5.61）。
3. **坐标与层级模型** = `T09` §五·补③ 一（设计 px 原样搬进 `RectTransform`；16:9 逐位相同；4:3 ⇒ `scaleFactor 0.75`）·
   §五·补⑨ 一（面板 = 嵌在根画布下的**子 Canvas** + `overrideSorting`，⛔ 子画布不带 `CanvasScaler`）·
   §五·补⑩ 一（`MenuDraw.SetPxSize` = ÷108，uGUI 档走 `HudCanvas` 那四个助手）与 六（配方 8 步）·
   §五·补⑨ 三/四（**字与图同批**迁，故本件两批一起做）。
4. **为什么图走 `MakeImageInParent` 而不是配方第 3 步写的 `QuadFromTopPx`**（**这一处我没照配方**，理由记在此）：
   `QuadFromTopPx`/`MakeImage` 是**归一化锚**（`x/1920`、`1−y/1080`）—— 只有**父 rect 恰好 1920×1080** 时才等于「相对父中心」；
   而本件两扇的父节点在 `Expand` 档下**不是** 1920×1080（4:3 ⇒ 1920×1440、21:9 ⇒ 2560×1080）⇒ 套那两个会把件**静默摆到别处**。
   判据 = `T09` §五·补⑩ §七·10（**同批两个写手各自撞了一次**才补的这个口）＋ 同批先例 `MulliganPanel.Img` / `SkillPanel.MakeCv` 用的都是它。
   ⇒ **`QuadFromTopPx` 在本件一次都没用**（它是给「父件就是整屏」那一档的）。
5. **双模式先例**：`Battle/ErrorMessageBanner.cs:430 Create(parent, preset, uguiHost)`（批 2）· `Core/Tooltip.cs` 的 `UseHost(host)`。
6. **`Hit` 的世界点 → 设计 px** 走 `_frame.InverseTransformPoint`：判据 = `MulliganPanel.PxOf`（同一套）；
   ⛔ 不读 `LayoutSpace.ToPixel/PxX` —— 那是**世界帧**的逆，非 16:9 下与画布帧差 1.333 倍（`T09` §七·8 实测）。
7. **`Label.WorldW/H` 在 uGUI 档就是画布 px**（它读 `TMP.textBounds`）⇒ ③·3 那条乘子必须按档取（`ErrorMessageBanner` 里 `WorldW * 1f` 是同一件事）。
8. **`SetAutoFitBox(worldW, worldH, minPx, maxPx, basePx)` 的量纲**：前两个**按档**（写进 `sizeDelta`），后三个**恒为 px**
   （`Label.cs:1008` 拿它们与 `NominalPx()` 比）⇒ 本件只在 `worldW/H` 上包了 `U()`。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

### ⑤·1 🔴 **坐标口径差异（属全批，主对话已受理；本件按现行口径落地、一个字没改）**

**现象**：画布档我们按**固定设计 px + 中心锚**给件，原版那一级是**角锚 / 锚点拉伸**。
🔑 **核心结论（一条规则就够）**：画布比参考画布**多出来的那一块**，我们**均分到中轴两侧**（中心锚），
原版**全部落在中间**（各件锚在自己的角/边上）⇒ 差值 = **多出来的一半**，方向由「原版把那一件锚在哪个角」决定。
**4:3（1440×1080 屏 ⇒ canvas 1920×1440 单位 · `scaleFactor 0.75` · 竖直多出 360 画布 px ⇒ 我们整体下移 180）**：

| 件（原版锚在哪） | 原版 `aMin/aMax` · `pivot` · `ap` · `sd` | 原版 4:3（画布 px **自画布顶**） | 我们 4:3 | 差值 |
|---|---|---|---|---|
| `PlayerChatDisplay`（**左下角**） | `aMin=aMax (0,0)` · `piv (0,0)` · `ap (12.61328125, 200.0)` · `sd (648.77404785, 236.50468445)` | 1003.5 / 1240 | 823.5 / 1060 | **我们偏上 180 画布 px = 135 屏 px** |
| `EnemyChatDisplay`（**左上角**） | `aMin=aMax (0,1)` · `piv (0,0)` · `ap (12.61328125, −410.0)` · `sd (648.77404785, 236.50463867)` | 410 / 646.5 | 353.5 / 590 | **我们偏上 56.5 画布 px = 42.4 屏 px** 🔴 **本行数错：原版应写 `173.5 / 410`、差值应写 `180 画布 px`（方向 = 我们偏下），见文首「更正」** |
| `RadioChat`（**右上角**） | `aMin=aMax (1,1)` · `piv (1,1)` · `ap (−25.0, −126.0)` · `sd (648.77392578, 236.50463867)` | 126 / 362.5 | 306 / 542.5 | **我们偏下 180 画布 px = 135 屏 px** |
| `ChatPopup`（**左下角**，`piv (0,0.5)`） | `aMin=aMax (0,0)` · `piv (0,0.5)` · `ap (0, 479.81005859375)` · `sd (815.0440063476562, 475.4700012207031)` | 722.455 / 1197.925 | 542.455 / 1017.925 | **我们偏上 180 画布 px = 135 屏 px** |

- **x 那一半 4:3 下不动**（画布宽仍 1920 = 参考）⇒ `PlayerChatDisplay` 左沿两边都是 **12.61**、`RadioChat` 右沿两边都是 **1895** ✓。
- **21:9（2560×1080 ⇒ `scaleFactor 1` · 水平多出 640 画布 px ⇒ 我们整体向中轴缩 320）**：
  `PlayerChatDisplay` 左沿 原版 **12.61** / 我们 **332.6** · `RadioChat` 右沿 原版 **2535** / 我们 **2215** · `ChatPopup` 左沿 原版 **0** / 我们 **320**
  ⇒ **我们一律偏中 320 画布 px**；**竖直方向逐位相同**（画布高 = 参考高）。
- **16:9 下逐位相同**（画布 = 1920×1080、`scaleFactor 1`）。
- ⚠️ **`Unit Chat` 那一级本身不欠**：它两边都是 stretch 铺满 ✓（只有它**下面**的件欠）。
- **正解（本件没做，等主对话整批换口径）**：把三颗气泡按各自 `aMin/aMax + pivot + ap` **原样锚上**、`ChatPopup` 也锚回左下
  —— 代价是子件坐标要从「相对屏心」改成「相对父矩形」（`PosPx` 那一套要换），且**已在飞的 5 扇同病**（`SkillPanel` 代理独立报了同一条）⇒ **要么整批一起换、要么整批都记着**。
- 📌 **另一条同族、但不同源的差异**（本件没改、只记）：`ChatPopup` 在我们这边是 `UnitChatPanel` 的**兄弟**，原版是它的**子件** ——
  按 `T09` §五·补⑩ 二「⛔ 别顺手改嵌套、差异如实记」处置；`SubCanvasRank` 里按「子件画在父件之上」把两块画布的先后排对（③·2）。

### ⑤·2 🔴 **`ChatPopupPanel` 那套世界 z 与原版层序【是反的】—— 本件只在画布档纠正，世界档一个字节没动**

- **事实**：本文件的世界 z 是 `close 0.02 < 色块 0.04 < 框 0.05 < 钮 0.06/0.07`；而本工程的口径是 **z 越小越靠前**
  （判据 4 条：`SkillPanel.cs:89` 的 doc · `CardDisplayWindow.cs:778` · `ErrorMessageBanner` 的「底条 0、文字 −0.01」· `HudCanvas.Add` 的映射 doc）
  ⇒ 世界档下**色块（不透明深绿，`BgColor.a = 1`）压在六颗钮之上**。
- **逐值**：`Bg` 矩形 x[121.16, 678.47] y[475.64, 776.99] 与六颗钮 x[77.2, 680.8] y[481.9, 777.3] **几乎完全重叠**；
  按「到相机（0,0,−20）的欧氏距离」逐颗算（`d²`；括号里是 `z`）：色块 **429.14**(0.04) · 钮 0 底图 **429.13**(0.06) ·
  钮 0 的字 **429.24**(0.07) · 钮 3 底图 **430.15**(0.06) · 钮 5 底图 **432.93**(0.06)
  ⇒ **只有钮 0 的底图以 0.01 的余量侥幸在前，钮 0 的字和下面几行全在色块之后**（余量都在 0.01~3.8 这一档，
  而 Unity 取的是**包围盒中心**、不是 `transform.position` ⇒ 具体哪几颗被盖**以真机为准**）。
  ⇒ **世界档今天大概率就是「六颗钮的字（多半还有底图）被那一整块不透明深绿盖住」** —— 这是**既有**缺陷，与本批无关；
  ⇒ 这也正是「世界档那一份**不能**照搬进画布档」的理由：画布上**没有**距离排序这条兜底，兄弟序就是最终结果，
  照搬等于把这个缺陷**钉死**。
- **我做的处置**：画布档**不复用**这套 z，改用 **原版子件序**（`UZ*`，判据 = 上面那些 `m_Children` 逐级现读）；
  **世界档原样保留**（照「逐位相同」）。⇒ 翻档那一刻这一扇的层序**会一起被修正**。
- **要裁的**：世界档那 6 个常量要不要顺手改成同向（一行改 6 个数）？**我没改**，理由：① 改了就不满足「宿主为 null ⇒ 逐位相同」；
  ② 它属「另一笔」、且要连着「z 与距离排序谁说了算」一起验（真机）。
  代账建议：`ChatPopup 世界 z 层序与原版相反（色块压钮）` —— 判据与逐值就在本节。

### ⑤·3 其余

1. **没跑果**：只跑过**秒级类型检查**（现行「先复刻后检查」口径）。画布档在**真机**上的落点 / 手感 / 三档宽高比并排 —— **未验**（属收尾那次统一验收）。
2. **原版三颗气泡【内部】子件的 `anchorMin/Max` 没逐颗读**：本件用的是我们既有的设计 px 常量（§五·补⑩ 配方第 5 步那条口径）。
   文件头记着「原版三颗的子件是**中心锚 + `anchoredPosition`**、三颗逐字相同」——**那一句是从本文件既有的注释继承的，我本轮只核了 `Mask/Background/wave/ChatText` 四件的 `ap` 与气泡中心的差值自洽（`324.4/118.3` = 气泡中心 ✓），没核另外三件的绝对矩形**。
3. ✅ **`ChatPopup` 的 6 颗钮我顺手把原版链**回代**了一遍（结论 = 我们的常量对）**：
   六行的真正父件是 **`Buttons`（RT 3644）**、不是 `ChatButtons`（RT 3363）—— 逐级是
   `ChatPopup(2881) → ChatButtons(3363, aMin=aMax(0,0), piv(.5,.5), ap(371,206), sd 722.66×403.13)`
   `→ Buttons(3644, aMax=aMin(0.5,0.5), ap(8.0, 2.0), sd 557.31×296.06) → ChatButton 行 ×6
   (anchor (0,1), piv(.5,.5), ap.x 278.655, ap.y −24 − 49.48k, sd 603.6×48)`。
   ⇒ `Buttons` 左沿 = `(371−722.66/2) + 8.0 = 100.345`、上沿自面板左上 = `1080−(242.075+206+2+296.06/2) = 481.895`
   ⇒ 第 1 行中心 = **(379.0, 505.895)**，而本件 `Button0 = (77.20, 481.89, 603.60, 48)` 的中心正是 **(379.0, 505.89)** ✓ **逐位吻合**。
   （我第一遍误把 `ChatButtons` 当父件 ⇒ 差 90.7 px 的假警报，**已就地订正**；`ButtonPitch = 49.48` 与 `ap.y` 的步长也逐位一致 ✓。）
4. **本件没有别的没读项**：`CardArt.Ui("40k_voicelines_radio")` / `White_Square` / `40k_voicelines_bt_R|L` 四张图**都在**
   `Assets/CardPresentation/Resources/Art/ui/` 里（`ls` 现核）⇒ 画布档不会因为缺图静默少件。

---

## ⑥ 顺手发现（不属于本件的，只报不改）

1. 🔴 **上面 §⑤·2 那条**（`ChatPopupPanel` 世界 z 层序与原版相反 ⇒ 色块压住六颗钮）——**这是本批最该先裁的一条**：它既影响世界档（今天的画面），也决定我「画布档用另一套层序」这个做法能不能被接受。
2. 🆕 **原版 `ChatPopup` 的六颗钮每行是【三层】：透明命中条 + `bg` 图 + `Text (TMP)` + `button`(图标)**，而**我们这边只建了两层**（`bg` + `icon` + 字）——
   原版那颗 `MB 4947`（`m_Color.a = 0`、`m_RaycastTarget = 1`、`sd 603.6×48`）是**专门给命中用的**、与我们用 `Btn.rect` 手搓等价 ✓（**不算差异**，记下来免得下次当缺口）。
3. ⚠️ **`HudCanvas.Ensure` 的调用点缺口**（同 `SkillPanel` 代理）：`:14430` / `:14624` / `:15193` / `:15240` / `:15487` 五处 `EnsureSubCanvas`
   之前都没有 `Ensure`，全靠 `BuildEnergyLights()`（`:14073`）兜；而它**会提前 return**（Glow 贴图缺失那档）。
   ⇒ 建议这些位置**都**补一句 `HudCanvas.Ensure(LayoutSpace.Cam);`（幂等、零副作用）。
4. ⚠️ **A218 探针会把面板建在画布上而不销毁**（每跑一次泄漏 2 个子画布节点，既有账 `T09` §七·12 的延伸）——本件**没新增**泄漏源，但 ③·3 那两条改法会让这两扇也进那一族。
5. ✅ **好消息**：本件**没有**引入「同一条规矩的第二份写法」——
   ① 图/字/位置全用 `HudCanvas` 现成的口；② 摆位分支收在**一个** `Move`/`Put` 里；③ 单位换算收在**一个** `U()` 里；
   ④ 层序常量两档各**只有一份**（并在 doc 里写明哪一份对哪一档、为什么）。

---

## 可直接粘进总表的一行

| `T09` 批 3 | `UnitChatPanel`（原版 `Unit Chat`，`OverHand`/**0**/兄弟序 **2/14**）+ `ChatPopupPanel`（`ChatPopup`，`OverHand`/**0**/兄弟序 **4/7**）**代码已改完**（两文件均改为双模式：不传宿主 = 世界档逐位不变），typecheck 0 错。⏭ **差三步接线（白名单外）**：① `BattleDriver.cs:14485/15088` 各 `Ensure(HudCanvas)` + `Create(root, EnsureSubCanvas(null, <两扇的 SubCanvasName>, OverHand, 0))` ② `HudCanvas.SubCanvasRank` 的 `OverHand` 段**最前**插 `"Unit Chat Canvas"` + `"ChatPopup Canvas"`（判据 = 原版子件序 2 < 3；`ChatPopup` 是 `Unit Chat` 的子件 ⇒ 紧跟其后）③ `BattleScene.cs` **6 处断言要改**（`:11951`/`:11957`/`:12042`/`:12299`/`:16551`/`:20903`）+ 新增一组子画布/`OnCanvas` 灭自证。🔴 **两条待裁**：(a) 画布档按固定设计 px、原版是**角锚/锚点拉伸** ⇒ 4:3 下三颗气泡的上下沿差 **180 / 56.5 / 180 画布 px**（135 / 42.4 / 135 屏 px）、`ChatPopup` 差 **180 画布 px（135 屏 px）**，21:9 下 x 差 **320 画布 px**（属全批，逐值表见报告 §⑤·1）；(b) `ChatPopupPanel` 的**世界 z 层序与原版相反**（色块压住六颗钮，4 条判据 + 逐值在 §⑤·2），画布档我已按原版子件序排、**世界档一个字节没动**，请裁是否一并修。 |
