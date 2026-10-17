# W · T07 Toast 批（执行写手报告）

> 会话：2026-10-17 · 角色：**执行写手**（只改白名单文件）
> 施工输入：`资料/普查产出_1017/UI/R_界面线剩余条目现核.md`（§二·4 / §二·5 / §二·10 / §N21 / §二·N49①）
> 本会话**未跑任何 `*.Run` / 未起 player / 未碰 git / 未改 `d:/2`**；只跑 `bash d:/4/Unity/工具/typecheck.sh`。

---

## 〇、结论（先看这个）

| 件 | 结果 | 一句话 |
|---|---|---|
| `A1235` 中/英字号静默差 | ✅ 已改 | `Label.SetScriptHeight` 不再写死 `0.72f`，改用**实测比** `Wcap/Wglyph = 0.8217`（单一来源 `Label.CapOverGlyph`）；两个互相打架的旧值**核清是 0.0779**（0.0683 是 em 口径推算、不是实测） |
| `N49①` 教学标注细黑描边 | ✅ 已改 | `TutorialOverlay` 四颗标注上实例材质：`_OutlineWidth 0.10` / `_FaceDilate 0.10` / `_OutlineSoftness 0` / 黑 |
| `N23` Toast 根高 | ✅ 已改 | `Show` 时按本次文案**现量**文本首选高、算根高并重摆（底图/图标/文字/命中区）；`108.8638 / 86.7019` 降级成**兜底**并出声 |
| `N21①` 层级 | 🔴 **推翻了 R 报告的结论，并按正确判据改了代码** | 原版排序层 `PopUps(7)` **低于** `Blocking(8)` ⇒ **加载黑场在 toast 之上**（R 报告只比了 `m_SortingOrder`，漏了 `m_SortingLayerID`）。我们把队列从 **5000–5004 → 4072–4076**（低于加载黑场最底层 `LoadingController.QDark 4080`、高于降级遮罩 4071） |
| `N21②③` 两颗死模板 | ✅ 已删 | `Collector Level Tooltip` / `Resource Counter Item` 的 builder + 常量 + 调用（`grep` 0 消费者；删后留了「为什么删」的注释） |
| `N20③` 进度条 `Fill` | ✅ 已改 | 不再 `localScale.x`；按目标宽度**重建九宫格**（`m_Type 1` + `ppuMul 2` ⇒ 角块恒 2px） |
| `N20②` Mission toast 装配 | 🟡 静态能补的已补 | 补：`displayRule = 1` ⇒ 完成态三件 info display 全关；完成态计数文案 = 词条 `Missions/Completed`。**补不了**：`AvailableRewards()` / 里程碑逐条 / `ApplyExtraRewards` / `ToggleAmount(amount<3)`（数据在运行时/服务端） |

---

## 一、逐件改了什么

### 1. `A1235` —— `Battle/Label.cs`（+ `Battle/SettingsPanel.cs` 注释）

**判据（现核，两条路必须落到同一个 px）**
- `Label.SetScriptHeight(text, px, pxPerUnit)`：汉字 → `SetGlyphHeight(px/pxPerUnit)`；拉丁 → `SetCapHeight(px × 比值 / pxPerUnit)`。
- 改前那个比值**写死 0.72**（注释称「0.72 em」）。
- 🔴 **两个旧实测值打架（0.0683 vs 0.0779）——本次核清是 0.0779**，推导（全部本地可读）：
  - `TmpFont.MeasureGlyph` 量的是 `characterInfo.topLeft.y − bottomLeft.y`；TMP 源码
    `Runtime/TMP/TextMeshPro.cs:2841/2846` = `(glyph.metrics.height + padding*2) * fontScale`；
  - `padding = ShaderUtilities.GetPadding(...)`：Noto 内联材质（`Resources/Fonts/NotoSerifCJK-Regular SDF.asset`）
    `_FaceDilate/_OutlineWidth/_OutlineSoftness = 0`、`_GradientScale 6` ⇒ `uniformPadding 0` ⇒
    **`return uniformPadding + 1.25f` = 1.25**（`TMP_ShaderUtilities.cs:526`）；
  - TTF 实读（`Assets/CardPresentation/Fonts/NotoSerifCJK-Regular.ttf`，upem 1000 · fontTools）：`'M'` 高 **729** · `'国'` 高 **898**；
  - `Wcap = (729×50/1000 + 2×1.25) × 0.1 ÷ 50 = 0.0779` ·
    `Wglyph = (898×50/1000 + 2×1.25) × 0.1 ÷ 50 = 0.0948` ⇒ **比值 = 0.8217**（与运行期实测的记录值逐位吻合）。
  - **0.0683 的来源**：`0.72 × 0.0948`（`capLine 36 / pointSize 50` 是 **em 口径、不含 SDF padding**）——**推算值，不是 `MeasureGlyph` 的返回值**。
- 影响：旧写法英文档比中文档小 `0.72 / 0.8217 = 12.4%`（正是 `SettingsPanel` 那个「英文 36.8 vs 42」症状）。

**改了什么**
- `Battle/Label.cs`
  - 新增 **`static float CapOverGlyph`**（本文件唯一一份比值：`WorldCapPerFontSize / WorldGlyphPerFontSize`，量不出时兜底 0.72）——`GlyphHeightWorld` 与 `SetScriptHeight` 共用它。
  - `SetScriptHeight`：`px * 0.72f / pxPerUnit` → `px * CapOverGlyph / pxPerUnit`。
  - 订正 doc：`SetScriptHeight`（含 2026-10-17 就地订正段）、`SetGlyphHeight` 的「大 39%」→「大 ~22%」、`UguiFontScale` 的 doc 里 `WorldCapPerFontSize 0.0683` → **0.0779**（带更正痕迹）。
- `Battle/SettingsPanel.cs`：`LabelFontPx` 的那段「1.14 倍量纲隐患」补一段 **✅ 2026-10-17 已在上游修掉**（历史保留）；滑块标签那段「28%/39%」→「22%/18%（`CapOverGlyph ≈ 0.8217`）」。
- ⚠️ **`Core/TmpFont.cs` 不在白名单**，它自己的 `:132` / `:232` / `:260` 仍写「Noto ≈0.0683」——**过期**（不是实测值）。本批**没动**，见 §四「没查清 / 留给下一批」。

### 2. `N49①` —— `Battle/TutorialOverlay.cs`

**判据（原版材质逐字段现读）**
- `d:/2/新解包资源/assets_full/bundle_fonts_assets_all/Material/Material_751232751962157263.json`
  「`Asar-Regular Light Grey w outline thin`」：
  `_OutlineWidth = 0.10` · `_OutlineColor = (0,0,0,1)` · `_FaceDilate = 0.10` · `_OutlineSoftness = 0` ·
  `_FaceColor = (0.8018868, 0.8018868, 0.8018868, 1)`（= 我们已经在用的 `annoInk`，逐位同值）。

**改了什么**
- 新增 `ApplyThinOutline(Label)`（照 `Core/CardView.ApplyCreatedByOutline` 的写法）：取 `TMP_Text.fontMaterial`
  （**实例材质**，getter 会 `CreateMaterialInstance`）→ `OUTLINE_ON` + 三个字段 + `UpdateMeshPadding()`。
- `Build()` 里四颗 `MeleeText/RangedText/HealthText/EnergyText` 建完立刻调它。
- 新增只读自检口 `public Label AnnoLabel(int i)`（用于断言「描边真上到实例材质上 / 共享材质没被写脏」）。
- ⚠️ 没做：**字体面**仍是 `Face.Cjk`（原版是 Asar）—— 属 `A1024③` 的逐件换面，不在本批。

### 3. `N23` —— `Shell/ToastNotifications.cs` 根高

**判据（静态，R 报告 §二·5 的链 + 本次复核）**
- `LayoutRebuilder` 一轮 = 计算→控制×2（`Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs:76-91`）；
- 原版 `ShowNotificationInternal` = `FixLayout()`（Toast 根 fixer 的 `layouts = [pid 204]`）+ `ForceRebuildLayoutImmediate` ⇒ **三轮**；
- Text 上的 `ContentSizeFitterMinMax`（`MB 538`）第一轮就把 `sizeDelta.y` 写成首选文本高；
- VLG `m_ChildControlHeight = 0` ⇒ 根高 = **`padT + iconH + 文本高 + padB`**（随文案变），`108.8638 / 86.7019` **只对第一轮成立**。

**改了什么**
- `ShowInternal`：`Fill` 之后（仅 `Achievement` / `CollectionGainXP` 两档）→ `ToastRootHeight(...)` 现量 → `ApplyMeasuredHeight(...)`。
- 新增 `ToastRootHeight`（唯一算式：`padT + iconH + textH + padB`；量不出 ⇒ **出声** + 返回模板常量）。
- 新增 `ApplyMeasuredHeight`：改根 `sizeDelta.y`；底图九宫格按新矩形**局部重建**（中心上移 ΔH/2）；
  图标/文字上移 ΔH；命中区 quad 高度换成新根高（中心上移 ΔH/2）。全部**局部平移**（实例刚 `Instantiate` 还没 `Pose`，
  ⛔ 不能用 `MenuDraw.Local` 重算绝对位置——已在注释里写明）。
- `BuildToast` / `BuildGetXP` 的常量注释改成「**兜底值**」，并写清为什么。
- 新增 `BackgroundSpec(bool fancy, ...)`：两档底图参数**一处定义**（`BuildToast` / `FancyBackground` / `RebuildBackground` 共用）。
- 新增 `ClearChildrenNow`：`MenuDraw.ClearChildren` 在 Play 下是**帧末销毁**，重建前先 `SetActive(false)`，避免旧块多画一帧。

### 4. `N21` —— 层级 + 死模板（含一条**推翻 R 报告**的现核）

#### 4·1 `N21①` 层级（🔴 与施工输入给的结论相反）

**原版实读（三份文件）**
- `globalgamemanagers/TagManager/TagManager_3.json` 的 `m_SortingLayers` 顺序：
  `Default(0) → Background → Elements → OverElements → Hand → HandMoving → OverHand → **PopUps(1054366423)** → **Blocking(2286135553)**`。
- `bundle_scenes_scenes_mainmenuwarpforge/Canvas/Canvas_1074.json`（GO `Toast Notification Controller`）：
  `m_SortingLayerID = 1054366423 (PopUps)` · `m_SortingOrder = 900` · `m_OverrideSorting = true`。
- 同包 `Canvas/Canvas_1081.json`（GO `LoadingMenu`）：`m_SortingLayerID = -2008831743`（**按 int32 还原 = 2286135553 = `Blocking`**）·
  `m_SortingOrder = 0` · `m_OverrideSorting = true`。
- ⇒ **排序层先于 order 比较** ⇒ **原版是「加载黑场盖住 toast」**。
- 旧账「黑场在 toast 之上」**是对的**；R 报告 §N21① 只比了 `m_SortingOrder`（900 > 0）、**漏了 `m_SortingLayerID`**
  ⇒ 它那句「Toast 在黑场之上 / 我们已经同序」**不成立**（错因 = 拿两个**不同排序层**的 order 直接比）。

**我们的改动（⚠️ 这是一处**行为**改动，请调度台复核）**
- `ToastNotifications.QShade/QBg/QArt/QText/QHit`：**5000–5004 → 4072–4076**。
- 取值依据（本仓队列梯子）：shell 最高 `4030` · `BlockingOverlay` 降级遮罩 `4070/4071` ·
  加载黑场那一族 `LoadingController.QDark/QSkull/QCog = 4080/4081/4082` + `LoadingMenu.QPanel = 4090`
  ⇒ toast **高过全部 shell/压暗层、低于加载黑场最底层**。
- 若调度台不接受这处行为改动：把那一行改回 5000–5004 即可（其余改动与它无耦合），但那样与原版相反。

#### 4·2 `N21②③` 死模板
- 删：`BuildCollectorTooltip`（含 `TooltipText` 辅助）、`BuildResourceCounter`、`BuildInternal` 里两行调用、
  常量 `CollectorTooltipPrefabName` / `ResourceCounterPrefabName` / `TooltipW/H` / `CounterW/H`。
- 删前 `grep`（全 `Assets/`）：`CollectorTooltipPrefabName|ResourceCounterPrefabName|BuildCollectorTooltip|BuildResourceCounter`
  **0 个外部消费者**；`"Resource Counter Item"` 的其它命中全在 `Shell/TopBar.cs` 与
  `Editor/{MainMenuScene,DeckScene}.cs` —— **那是顶栏自己那一族节点**，与本文件无关。
- 原处留注释写清「为什么删」（原版 `notificationTypes` 四条只指向四个 toast、这两份没有生产路径）。
- 原版那两份 prefab 的**参数仍然保留在文件头 ② 当知识**（并标注「我们故意不建」）。

### 5. `N20` —— 进度条 & Mission toast

- **`N20③` `ApplyFill`**：改前 `fill.localScale.x = p`（九宫格角块跟着缩）；改后：
  满格宽取 `Progress Bar` 的 `RectTransform.rect.width`（`RT 362` = 211.189px）→ `MenuDraw.UnionQuadRectPx`（`QuadGate.Self`，
  完成态父链可能关着）取左/上/下沿 → `ClearChildrenNow` → 按 `[x1, x1+w]` **重建九宫格**（border 4 / borderOut 2 ⇒ 角块恒 2px）。
  判据 = 原版 `Fill` 的 Image 字段（`MonoBehaviour_410.json`：`m_Type = 1 (Sliced)` · `m_PixelsPerUnitMultiplier = 2.0`）。
- **`N20②` Mission toast**（读 `decomp_full/ToastNotificationMission__Initialize.c` 等）：
  - ✅ 补①：三个 `MissionInfoDisplay` 的 prefab 字段 `displayRule = 1`（`MonoBehaviour/{499,500,501}.json` 逐份现读），
    代入 `MissionInfoDisplay__Initialize.c` 的位运算 = **「只在未完成时显示」** ⇒ `Fill` 里
    `complete = ShowProgress && Progress01 >= 1` 时关 `Rewards` / `Progress Bar` / `progress` 三件。
  - ✅ 补②：`MissionCounterDisplay__Setup.c` —— 完成 且 `displayCompletedMessage = 1`（`MB 499` 实读）
    ⇒ 计数文案 = 词条 **`Missions/Completed`**（`Core/Loc.cs:1723` 有这条：已完成 / Completed）。
  - ⛔ 补不了（如实标注在代码注释里）：
    · `MissionRewardsDisplay__Setup.c` 的 `AvailableRewards()` 逐条实例化 + `LiveOpsManager.ApplyExtraRewards`；
    · `MissionMilestonesDisplay__Setup.c` 的逐里程碑 `MissionMilestoneStep`（阈值来自同一份运行时奖励表）；
    · `MissionRewardItem.ToggleAmount(amount < 3)` —— 我们的 `Context` 只有 `RewardText`（字符串），
      **拿不到原版那个数值**（`+0x18` 的字段归属也没核），⇒ 仍按「有文案就显示」，并已在注释里写明。

---

## 二、判据对照表（原版值 / 改前 / 改后）

| 项 | 原版判据（出处） | 改前 | 改后 |
|---|---|---|---|
| 中/英字号比 | `Wcap/Wglyph = 0.0779/0.0948 = 0.8217`（TTF + `TMP_ShaderUtilities.cs:526` 推导，见 §一·1） | 写死 `0.72f`（英文档小 12.4%） | `Label.CapOverGlyph`（运行时实测） |
| 标注描边 | `_OutlineWidth 0.10` · `_FaceDilate 0.10` · `_OutlineSoftness 0` · 黑（`Material_751232751962157263.json`） | 无描边 | 实例材质，同上 |
| Toast 根高 | `padT + iconH + 文本高 + padB`（`LayoutRebuilder` + `MB 538` + `MB 509.layouts=[204]`） | 常量 108.8638 / 86.7019 | 按本次文案现量（常量降级为兜底） |
| Toast 队列 | 排序层 `PopUps(7)` + order 900 **低于** `Blocking(8)` ⇒ 黑场在上（TagManager + 两个 Canvas JSON） | 5000–5004（压过加载） | 4072–4076（低于 `QDark 4080`、高于 `BlockingOverlay 4071`） |
| 死模板 | `notificationTypes` 只指向 4 个 toast（`MB 1939`） | 每次 Build 白建两棵死树 | 已删 |
| 进度条 Fill | `m_Type = 1 (Sliced)` · `ppuMul 2.0` ⇒ 角块恒 2px（`MB 410`） | `localScale.x`（角块跟着缩） | 按宽度重建九宫格 |
| Mission 完成态 | `displayRule = 1` ⇒ 未完成才显示（`MB 499/500/501` + `MissionInfoDisplay__Initialize.c`） | 只看有没有奖励文案 | 完成 ⇒ 三件全关；计数文案改 `Missions/Completed` |

---

## 三、断言（本批新增）

1. **`Editor/BattleScene.cs` §4.6c（`A1235`）**
   - 前提：两条探针都走 TMP 后端（前提断言 + `if`，⛔ 无 `else` 的 `if`）。
   - ★ 中文「中文」与英文「ABC」同一 `px = 42` ⇒ 两条路 `FontPxNow` **都 ≈ 42 且相等**（期望值是原版 `m_fontSize` 字面量）。
   - ★ 实测比 `= 0.8217 ± 0.005`（期望值是从 TTF/材质算出来的字面量，不取自被测实现）。
   - ★ **灭自证**：实测比与 `0.72` 必须相差 > 0.02（防止有人把 `WorldCapPerFontSize` 也改成 0.0683 去「配平」）。
   - 🧨 把 `SetScriptHeight` 改回 `0.72f` ⇒ 英文档量成 ≈36.8px ⇒ 第一条红。
2. **`Editor/BattleScene.cs` 教程节 ①·b（`N49①`）**
   - 四颗标注逐颗读 `fontMaterial`：`_OutlineWidth 0.10` / `_FaceDilate 0.10` / `_OutlineSoftness 0` / 描边色黑 ⇒ **四颗全中**。
   - **灭自证**：`fontSharedMaterial` 的 `_OutlineWidth` **不得** = 0.10（我们是实例材质、不写脏 `Resources/` 里那份资产）。
3. **`Editor/ShellScene.cs` 新增「N20/N21/N23」一节**
   - `N21①`：`4071 < QShade < LoadingController.QDark(4080)` 且 `< LoadingMenu.QPanel`（🧨 改回 5000 ⇒ 红）。
   - `N21②③`：`Templates` 下 0 颗 `Collector Level Tooltip` / `Resource Counter Item`（子件共 4）。
   - `N23`：短文案 vs 长文案 ⇒ **根高不同**（🧨 改回常量 ⇒ 红）；单行根高 = `12 + 89.8638 + 文本首选高 + 7`（±0.05px）。
   - `N20②`：未完成档 `Progress Bar`/`progress`/`Rewards` 都在画；完成档三件全关 + 计数文案 = `Loc.T("Missions/Completed")`。

⚠️ 本会话按纪律**没有跑**这些断言（`*.Run` 一条都没跑）—— 它们只经过 **typecheck**。

---

## 四、没查清 / 留给下一批

1. **`Core/TmpFont.cs` 的过期注释**（不在白名单）：
   - `:132` / `:260`「Pragati 0.0656 · Asar 0.0685 · **Noto ≈0.0683**」→ Noto 的实测是 **0.0779**（含 SDF padding）；0.0683 是 `0.72 × 0.0948` 的推算。
   - `:232`「`0.0683 ÷ 0.72 ≈ 0.0948` ⇒ 这条式子是通的」——**式子的两个量都是推算，不是 `MeasureGlyph` 的返回值**（数值上巧合自洽）。
   - 另：`资料/卡面图标_现状与缺口.md:244` 写的是 `WorldCapPerFontSize = 0.0779`（**这个对**）；`资料/普查产出_1016并行/EW4/EW5` 写 0.0683（过期）。
2. **`N20②` 三条运行时装配**（`AvailableRewards()` / 里程碑逐条 / `ApplyExtraRewards` / `ToggleAmount(amount<3)`）——
   数据在服务端/运行时，**本地没有**；`ToggleAmount` 的字段归属（`reward + 0x18 → + 0x18`）也**没核**。
3. **`N23` 的原版真机读数**：本仓**没有**原版 `rect.height` 的实拍；我们用的是 uGUI 字段语义（padding + 子件高）推导。
4. **`N49①` 的字体面**：四颗标注仍是 `Face.Cjk`（原版 `Asar`）——属 `A1024③` 的逐件换面。
5. **`N21①` 的队列改动是一处行为改动**（5000 → 4072-4076）：若调度台不采纳，改回那一行即可；
   本条与其它四条无耦合（断言会跟着红，按需一起改）。

---

## 五、typecheck 原文

`bash d:/4/Unity/工具/typecheck.sh`（改完全部 `.cs` 之后，最后一次）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（过程中出现过两次中间错误，均已修掉：`ToastNotificationType` 未限定命名空间 · `LoadingMenu.QDark` →
实际那个常量在 `LoadingController`。）

---

## 六、改动文件清单（全部在白名单内）

- `Unity/MyGame/Assets/CardPresentation/Battle/Label.cs`（`A1235` 主体 + 注释订正）
- `Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs`（注释订正两处）
- `Unity/MyGame/Assets/CardPresentation/Battle/TutorialOverlay.cs`（`N49①`：`ApplyThinOutline` + `AnnoLabel`）
- `Unity/MyGame/Assets/CardPresentation/Shell/ToastNotifications.cs`（`N20/N21/N23` 全部）
- `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（只加 `A1235` / `N49①` 断言）
- `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs`（只加 `N20/N21/N23` 断言）
- `Unity/资料/普查产出_1017/UI/W_T07_Toast批.md`（本文件）
