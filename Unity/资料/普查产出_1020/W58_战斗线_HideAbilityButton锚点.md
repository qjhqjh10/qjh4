# W58 · 战斗线 · `HideAbilityButton` 锚点补正（相对面板）

> 2026-10-20 · 执行代理 **W58-HIDEBTN** · 白名单：`Battle/SkillPanel.cs`、`Editor/BattleScene.cs`
> 对应 C3C4 报告「没查清的部分」第 **5** 条（`PlaceHideButton` 用 `CanvasPx` 换算到画布底右 ⇒ 与原版锚定帧不同）。
> ⛔ 未跑 `*.Run`（铁律 12）；未动 `BattleDriver.cs`。

---

## 结论

1. **原版 `HideAbilityButton` 的父 = 面板节点 `ActiveSkillDesc`（`RT_3104`），不是画布** ——
   `RT_3053.m_Father = 3104`；运行时 dump 的 path 也把它放在
   `…/Safe area FrontCanvas/ActiveSkillDesc` 之下。
2. 原版五元组（13 个 `battlearena*` 包**逐值相同**）：
   `aMin = aMax = (1,0)` · `pivot = (1,0)` · `ap = (220.3900146484375, −146.0101776123047)` ·
   `sd = (3707.830078125, 1985.3199462890625)`。
3. 我们的实现已改成**挂在 `_panelRt` 下**、五元组**逐字照抄**原版；`PlaceHideButton` 不再用
   `CanvasPx` 做「画布底右锚 → 中心锚」换算（旧实现与原版相差 **606.11 px** = 面板 `ap.x`）。
4. **未做 `ap' = ap + (0.5 − pivot) × sd` 换算**：那条换算只在「把 pivot 改成 `(0.5,0.5)`」时需要；
   本实现保留原版 `pivot = (1,0)` ⇒ 直接写字面量。核过两种写法等价。
5. **连带发现并修掉一个结构坑**：`_panelRt` 是在 `BuildCanvas` 里手工 `new` 的节点，**不进 `HudCanvas` 的 z 账**时，
   后续平级件的 `SetSiblingIndex` 会把它挤到**最上层** ⇒ 透明捕获区盖住面板，**点面板本体先命中捕获区（面板当场关掉）**。
   现已按 z 账登记（`ZPanelNode = ZHideBtn + 0.02`）+ 补一条兄弟序断言 `W58-HB⑨`。
6. 断言新增 9 条（`W58-HB①…⑨`），含**三档独立期望值**、**灭自证**（面板 `ap.x` +50 ⇒ 捕获区中心 +50）、
   **兄弟序回归哨兵**。
7. typecheck **0 / 0**。

---

## 原版五元组（出处）

| 字段 | 值 | 出处 |
|---|---|---|
| `m_AnchorMin` | `(1.0, 0.0)` | `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3053.json` |
| `m_AnchorMax` | `(1.0, 0.0)` | 同上 |
| `m_Pivot` | `(1.0, 0.0)` | 同上 |
| `m_AnchoredPosition` | `(220.3900146484375, −146.0101776123047)` | 同上 |
| `m_SizeDelta` | `(3707.830078125, 1985.3199462890625)` | 同上 |
| 父节点 | `m_Father = 3104`（= `ActiveSkillDesc` 的 `RectTransform`） | 同上 + `bundle_scenes_scenes_battlearena2/GameObject/ActiveSkillDesc.json`（`m_Component[0] = 3104`） |
| GO 名 | `HideAbilityButton`（`m_Component[0] = 3053`） | `bundle_scenes_scenes_battlearena2/GameObject/HideAbilityButton.json` |
| 运行时父链 | `…/Safe area FrontCanvas/ActiveSkillDesc` → `HideAbilityButton` | `d:/4/Unity/资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv:369` |

- **13 个战场包逐值扫描**（`battlearena1/2/3/aeldari/astramilitarum/blacklegion/darkangels/emperorschildren/genestealers/leviathan/sororitas/spacewolves/tauriorla`）：
  五元组全部相同（`ap` 只有 1e−4 量级的浮点抖动：`220.38995361328125` / `-146.01011657714844`，
  本实现采用 arena2 的 `220.3900146484375 / -146.0101776123047`，断言容差 1e−4）。
- **锚定帧 = 面板**：`RT_3104` 是双轴拉伸（`aMin (0.665576696395874,0)` … `aMax (0.965788722038269,0.3040129840373993)`），
  `RT_3053` 的 `(1,0)` 落在它的右下角。

### 如实记（原版布局本身）

按面板右下角锚定后，16:9（画布 1920×1080）下捕获区（`_cv` 局部、中心原点）为
`x ∈ [−3199.24, 508.59]`、`y ∈ [−629.25, 1356.07]`（= 屏幕坐标 `x ∈ [−2239.24, 1468.59]`）。
⇒ 左/上/下覆盖整屏且有余，**x 轴右边缘只到 1468.6（屏幕右界 1920）⇒ 右侧约 451 px 不在捕获区内**。
这是**原版自己的布局**（`RT_3053` + `RT_3104` 字面量算出来的），⛔ 没有为它擅自改 `ap/sd`；
原先文件里「覆盖整屏且有余（x 也一样）」的注释已就地订正。

---

## 改法

### `Battle/SkillPanel.cs`

1. **`BuildInput`（`:787`）**：`_hideBtn` 的父从 `_cv` 改为 **`_panelRt`**
   （`_panelRt == null` 时出声并退回 `_cv`，⛔ 不静默）。
2. **`PlaceHideButton`（`:956`）**：删掉 `CanvasPx` 换算，直接写原版五元组
   （`aMin = aMax = (1,0)` · `pivot = (1,0)` · `ap` · `sd` 全为字面量）。
3. **`MakePanelNode`（`:747`）**：把 `_panelRt` 登记进 `HudCanvas` 的 z 账（`ZPanelNode = ZHideBtn + 0.02`，`:222`）——
   否则它的子件（捕获区）会漂到子画布最上层，点面板 = 关面板。见 `ZPanelNode` 的 doc。
4. **不改 `RefreshLayout`**：它仍调用 `PlaceHideButton()`（幂等），且 4 颗自适应字/方形图标仍靠它
   写显式 `sizeDelta` —— 按任务要求原样保留。
5. 就地订正文件里「锚在**画布**右下角」的旧记录（铁律 5）：常量注释、字段 doc、`PlaceHideButton` doc、
   `HideButtonAnchoredPosForTest` doc、`_panelRt` doc（「不装子件」→「唯一子件 = `HideAbilityButton`」）。
6. 新增 **Editor-only 探针**（`#if UNITY_EDITOR`）：
   - `PanelAnchorRectForTest`（`:1753`）
   - `HideButtonParentForTest`（`:1757`）
   - `HideButtonAnchorsForTest(out …)`（五元组读回，`:1773`）
   - `HideButtonBehindAbilityContainerForTest`（兄弟序，`:1793`）
   - `HideButtonCenterSizeForTest(out centerPx, out sizePx)`（`:1812`）：
     实测支 = `panel.localPosition + hide.localPosition + hide.rect.center`（**只读 Transform 布局结果**）；
     画布 `rect` 退化（批处理）时退档 = 捕获区锚点字段 + `PanelCvRect`，并**出声一次**。

### `Editor/BattleScene.cs`

在 `if (sp.UguiMode)` 内、原 `T09-C4⑥` 之后新增一块（`:8481-8625`）。

---

## 断言（三档期望值怎么独立算）

全部在 `if (sp.UguiMode)` 里，**期望值只取原版 JSON 字面量 + 写死的三档画布尺寸**
（⛔ 不读 `PanelCvRect` / `CanvasPx` 反推）：

| 编号 | 断言 | 灭自证 / 改坏法 |
|---|---|---|
| `W58-HB①` | `_hideBtn.parent == _panelRt`（面板节点，⛔ 不是子画布） | 挂回子画布 ⇒ 红 |
| `W58-HB②` | 前提：捕获区建出来了（读得到五元组） | 没建 ⇒ 红（不假绿） |
| `W58-HB③` | 五元组 **10 个数** = 原版 `RT_3053` 字面量（容差：锚/pivot 1e−6、ap 1e−4、sd 1e−3） | 写成中心锚 / 换算 ap / 改 sd ⇒ 红 |
| `W58-HB④[3 行]` | 用**原版 `RT_3104` + `RT_3053` 字面量 + 该帧画布尺寸**还原三档 `CenterPx/SizePx`，与期望表逐行比（< 0.01 px） | 把面板当画布（旧实现）⇒ 每行 `CenterPx.x` 差 606.11 ⇒ 红 |
| `W58-HB⑤` | 前提：读得到实测 `CenterPx/SizePx` | —— |
| `W58-HB⑥` | 前提：当前画布尺寸对到 4:3 / 16:9 / 21:9 某一行（优先取**根画布** `HudCanvas.RootRect` 独立读数；量不到才退实现那一档） | 第四种宽高比 ⇒ 红（要求补行，⛔ 不静默跳过） |
| `W58-HB⑦` | 当前帧**实测** `CenterPx/SizePx` = 该行期望值（< 0.5 px） | 旧实现（画布底右锚换算）⇒ x 差 606.11 ⇒ 红 |
| `W58-HB⑧` | **灭自证**：面板节点 `ap.x` +50 ⇒ 捕获区中心同步 +50、尺寸不变（测完平回去） | 把捕获区挂回**画布** ⇒ 面板动、捕获区不动 ⇒ 红 |
| `W58-HB⑨` | **兄弟序回归哨兵**：`_panelRt` 的 sibling index < `_bg`（捕获区画在面板本体之下、射线后收） | 不登记 `_panelRt` 进 `HudCanvas` z 账 / 挂到最上层 ⇒ 点面板 = 关面板 ⇒ 红 |

三档期望表（`_cv` 局部、设计 px、中心原点、y 向上）：

| 帧 | 面板 W×H | 捕获区 SizePx | 捕获区 CenterPx |
|---|---|---|---|
| 4:3 1920×1440 | 576.4067 × 434.2687 | 3707.8301 × 1985.3199 | (−1345.3210, 183.4057) |
| 16:9 1920×1080 | 576.4067 × 324.8240 | 3707.8301 × 1985.3199 | (−1345.3210, 363.4057) |
| 21:9 2560×1080 | 768.5424 × 324.8240 | 3707.8301 × 1985.3199 | (−1047.2162, 363.4057) |

算式（断言里逐字照写，`C` = 画布尺寸）：

- 面板：`size = (aMax−aMin)·C + sd`；
  `center = −C/2 + (aMin + (aMax−aMin)·pivot)·C + ap`。
- 捕获区（点锚 `aMin=aMax=(1,0)` ⇒ `size = sd`）：
  相对面板左下 = `(1,0)·面板size + ap + (0.5 − pivot)·sd`；
  `CenterPx = 面板rect.min + 该偏移`。

---

## 没查清的部分

1. **没有跑 `*.Run`**（铁律 12）⇒ 批处理里 `HideButtonCenterSizeForTest` 走「实测支」还是
   「退档支」**未实测**。两支在 16:9 下逐位相同、三档算式一致；实测支只在 `rect` 有效时启用，
   与 `PanelCvRect` 的实测/退档口径同一条。
2. **`BattleDriver.cs` 未动**（另一代理 T12 正在改）。本件不需要改调用点：`PlaceHideButton` 仍是
   `RefreshLayout` 的末段，`SetInputEnabled` / `OnHidePointerDown` 不依赖父节点。
3. **右侧约 451 px 不覆盖**（见「如实记」）是原版数据本身的结果，**未改**；
   它是否影响原版「点右侧空白关面板」的行为，未查（不在本任务范围）。
4. 原版 `HideAbilityButton` 的 `Image` 是 `m_Type = 1 (Sliced)`、sprite PathID
   `1660267235368898380`；我们仍用 1×1 纯色 + `Image`（原版 `m_Sprite` 那条链的另一件事，
   与锚点无关，未动）。

---

## 改动清单

| 文件 | 位置（现行号） | 改动 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Battle/SkillPanel.cs` | `:209-240` | `HideAbilityButton` 常量段：就地订正「锚在画布」→「锚在面板」+ 右边缘不覆盖的如实记录 + 新增 `ZPanelNode` |
| 同上 | `:787-806` | `BuildInput`：`_hideBtn` 父 = `_panelRt`（null 时出声退回 `_cv`） |
| 同上 | `:747-761` | `MakePanelNode`：`HudCanvas.Add(rt, ZPanelNode)` 登记 z 账（否则捕获区漂到最上层） |
| 同上 | `:956-969` | `PlaceHideButton`：五元组逐字照抄原版（删 `CanvasPx` 换算）；doc 重写 |
| 同上 | `:1139` | `RefreshLayout` 里调用处注释更新 |
| 同上 | `:428-432` | `_hideBtn` 字段 doc 补父节点 |
| 同上 | `:420-424` | `_panelRt` doc：「唯一子件 = `HideAbilityButton`」 |
| 同上 | `:137-140` | 文件头「结构性差异」补注（节点现在装了原版那一颗捕获区） |
| 同上 | `:1753-1849` | 新增 Editor-only 探针 5 个 + 退档出声闩 |
| `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | `:8481-8635` | 新增 `W58-HB①…⑨`（父链 / 五元组 / 三档期望值 / 实测 / 灭自证 / 兄弟序） |

- `git diff --numstat`（收尾）：`SkillPanel.cs` 326/58、`BattleScene.cs` 565/6（后者含**别的代理**在同一文件里的既有改动）。
- 行尾：`SkillPanel.cs` 纯 LF（与 HEAD 一致）、`BattleScene.cs` 纯 CRLF（与 HEAD 一致）—— 未翻行尾。

---

## typecheck 结果

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（`TMPDIR=/tmp/wf_w58 bash d:/4/Unity/工具/typecheck.sh`；中途一次 `CS0034 Vector2+Vector3` 已修。
未跑任何 `*.Run`。）
