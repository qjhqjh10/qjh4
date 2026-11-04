# B141-W8b · 语言下拉按内容收缩（战斗内设置面板）+ 三颗标签 `Midline`

> 执行写手 `B141-W8b` · 第一百二十八会话 · 2026-11-04
> 白名单内只改：`Battle/SettingsPanel.cs` · `Battle/BattleDriver.cs` · 新建 `Editor/B141_W8b_Assert.cs` · `Editor/BattleScene.cs`（一行接线）。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账主表 / 活状态表 / 所有权表 · 未碰 `RuleCore` / `EffectResolver`。
> 类型检查（`TMPDIR=/tmp/wf_b141_w8 bash 工具/typecheck.sh`）：**运行时 0 / 编辑器 0**（末次读数；中途几次报错全在
> 别人在飞的 `RuleEngine/Core/RuleCore.cs`、`Editor/B141_W19_Assert.cs`，本包两个文件零条）。

---

## 一 · 结论

| # | 项 | 原版值 | 改前 | 改后 | 落点（现读） |
|---|---|---|---|---|---|
| `4-05` 同族 | 战斗内设置面板 · 语言下拉面板高 | **按内容收缩** `n × 40.8707 + 0.8519`<br>12 项 = **491.30** · 13 项 = **532.171** | 恒 **573.96**（画满高） | **按内容收缩**（13 项 = 532.171，底 = **−524.471**，顶仍 **7.7**） | `Battle/SettingsPanel.cs:1739`（`BuildLangListSubtree`）· `:1724`（原型调用点）· `:1870`（克隆体调用点） |
| `4-10` | END TURN 那颗 `TurnText` | `m_VerticalAlignment = **4096 (Midline)**` | `Middle`（出厂档） | **Midline** | `Battle/BattleDriver.cs:29180`（`_endTurnLabel.SetVAlign`） |
| `4-10` | 我方 `ManaText` | 同上 **4096** | `Middle` | **Midline** | `Battle/BattleDriver.cs:29079` |
| `4-10` | 敌方 `ManaText` | 同上 **4096** | `Middle` | **Midline** | `Battle/BattleDriver.cs:29088` |

⚠️ `BattleDriver.cs:30084-30085`（`_titleMe/_titleFoe` = Midline）**按派单要求没有动**（"已接对，⛔ 不用再动"）。

---

## 二 · `4-05` 同族（`Battle/SettingsPanel.cs`）

### 2·1 判据

- `TMP_Dropdown.Show()`（未改动的官方 ugui `Runtime/TMP/TMP_Dropdown.cs:926-932`）：
  `sizeDelta.y = itemSize.y * m_Items.Count + offsetMin.y − offsetMax.y`；随后
  `extraSpace = dropdownRect.height − contentRect.height; if (extraSpace > 0) dropdownRect.sizeDelta.y -= extraSpace;`
  ⇒ **面板高 = 内容高**（内容比模板矮时才缩）。
- 常数：`Item` 高 **40.8707**（本扇 `LstItemH`，原版 `Item.sd.y`）· `Content` 模板高 41.7226 ⇒ 常量项 **0.8519**。
- 同族第一处（`Shell/SettingsWindow.cs`）已由 `B141-W13` 改完，本件**照它的做法**做（同一份算式、同一个 `withRows` 语义）。

### 2·2 改法（与 W13 同构）

1. 新增 `const float LstItemGap = 0.8519f;`（`:693`）与 **唯一**算式 `static float DropdownContentH(int n) { return n * LstItemH + LstItemGap; }`（`:697`）。
2. `BuildLangListSubtree(..., int rows, **bool withRows**)`：
   `botFull = top − LstHPx`；`bot = withRows ? Mathf.Max(botFull, top − DropdownContentH(rows)) : botFull`；
   `panelH = top − bot`。
   底板 / `scroll_bg` / `Handle` 的滑区高三处**全部改吃 `panelH`**（原来写死 `LstHPx`）；
   `vpCy = (top + bot) / 2` 自动跟着走 ⇒ **顶不动、底上收**。
3. 调用点：`Template` 原型传 `withRows = false`（**保持序列化满高 573.96** —— 原版 prefab 里它就是这么高）；
   `Dropdown List` 克隆体传 `true`。
4. 顺带就地订正三处过期数字注释（`13 行 531.32 < 视口 573.96` → `视口 = 面板高 532.171`），铁律 5。

### 2·3 数值（现读）

- 13 项：内容高 `13 × 40.8707 + 0.8519 = **532.171**`；面板顶 `29.7 − 22 = **7.7**`、底 `7.7 − 532.171 = **−524.471**`；
  比满高矮 **41.79 px**。
- 12 项：`12 × 40.8707 + 0.8519 = **491.30**`（B138 §4 `4-05` 给的原版参考值，同一条公式的另一档）。
- 行位置**不受影响**：`Content` 框心仍是 `top − rows × 40.8707 / 2`，`Content` 的绝对位置 = `vpCy + (ctCy − vpCy) = ctCy`
  ⇒ `Editor/BattleScene.cs:20141-20150` 那两条「第 0 项 / 第 N 项行心 y」**逐值不变**（已逐项核过，不需要改）。

### 2·4 灭自证

`B141_W8b_Assert` A4/A7：面板高必须 = 内容高 **且严格小于满高 573.96** ⇒ 把 `bot` 改回恒 `top − LstHPx` 立刻红；
A2：`Template` 原型必须仍 573.96 ⇒ 把原型也缩（`withRows = true`）立刻红。

---

## 三 · `4-10` 其余两处（`Battle/BattleDriver.cs`）

### 3·1 判据（原版 prefab 逐颗现读，`bundle_scenes_scenes_battlearena1/MonoBehaviour/`）

| 原版 TMP | pathID | `m_text` | `m_VerticalAlignment` |
|---|---|---|---|
| `Clock/TurnBtn/TurnText` | `MonoBehaviour_3971.json`（`go 77`） | `"END TURN"` | **4096 = Midline** |
| `EnemyMana/ManaHolder/ManaText` | `MonoBehaviour_4919.json`（`go 100`） | `"10/10"` | **4096 = Midline** |
| `PlayerMana/ManaHolder/ManaText` | `MonoBehaviour_5242.json`（`go 382`） | `"10/10"` | **4096 = Midline** |

⚠️ **易踩的坑（写进代码注释了）**：同目录还有一颗**同名** `TurnText_293`（`m_text = "You go second"`、`go 293`，
`m_VerticalAlignment = **512 (Middle)**`）—— 按名字搜会搜到它。**本件要的是 `go 77` 那颗 `"END TURN"`**，
先按 `m_text` 认、再读档位。

工程侧映射：`Label.VAlign.Midline = 0x1000 = 4096`（`Battle/Label.cs:2008`）⇒ 与原版**逐位同值**。

### 3·2 改法

- `if (_energyLabel != null) _energyLabel.SetVAlign(Label.VAlign.Midline);`
- `if (_foeEnergyLabel != null) _foeEnergyLabel.SetVAlign(Label.VAlign.Midline);`
- `if (_endTurnLabel != null) _endTurnLabel.SetVAlign(Label.VAlign.Midline, 0f, Label.OrigFace.Asar);`

`OrigFace` 按各颗自己的字体档给：两颗 `ManaText` 是 `Face.LatinCardBody`(Pragati) ⇒ 默认 `Pragati`；
`TurnText` 是 `Face.LatinUi`(Asar) ⇒ 显式 `Asar`（`Label` 的两份度量 `a/c/d` 不同：Pragati 70/60/−20 vs Asar 80/61/−35）。
`Midline` 不吃框高 ⇒ 第二个实参传 0（同 `_titleMe/_titleFoe` 的先例）。

### 3·3 ⚠️ 顺手发现（⛔ 未改，按派单"不用再动"）

`_titleMe/_titleFoe`（`BattleDriver.cs:30052-30057`）用 `TmpFont.Face.LatinUi`(Asar)，但 `SetVAlign(Label.VAlign.Midline)`
**没给 `OrigFace.Asar`**（走的是默认 Pragati）⇒ 同一颗标签渲染用 Asar、掩码度用 Pragati。
**不是本件的活**，记在这里由调度台分流（改它=2 行，风险低）。

---

## 四 · 断言（`Editor/B141_W8b_Assert.cs`，154 行）

接线：`Editor/BattleScene.cs:45405` 活 driver 聚合行，紧跟 `B141_W8_Assert` 之后、`driver.DetachStaticHooks();` 之前
（同一物理行追加 ⇒ **BattleScene 行数零变化**）。

| # | 断什么 | 🧨 改坏法 |
|---|---|---|
| A1 | `DropdownContentHForTest(12)` = **491.30** | 删 `LstItemGap` / 改行高 ⇒ 红 |
| A1b | `DropdownContentHForTest(13)` = **532.171** | 同上（两档都断 ⇒ 写死单值必有一条红） |
| A2 | `Template` 原型底板仍 **573.96** | 给原型也传 `withRows = true` ⇒ 红 |
| A3/A3b | 真开一次列表；行数 = `Loc.Languages.Length` | —— |
| **A4** | 克隆体底板高 = **532.171** | `bot` 改回恒 `top − LstHPx` ⇒ 红 |
| A5 | 列表**顶** = **7.7**（框心往下 22） | 只改高不改 `vpCy` ⇒ 红 |
| A6 | 列表**底** = **−524.471** | 底边写死 ⇒ 红 |
| A7 | 灭自证：高**严格小于** 573.96（与 A4 不同源的不等式） | 改回满高 ⇒ 红 |
| B1/B2 | 我 / 敌 `ManaText` 的 `VAlignTier == Midline` | 漏敌那颗 ⇒ B2 红 |
| B3 | END TURN `TurnText` = `Midline` | 照抄 `TurnText_293`（Middle）⇒ 红 |
| B4 | 灭自证：两颗都不是出厂档 `Middle` | 删 `SetVAlign` ⇒ 红 |

**覆盖边界**：批处理无帧循环 ⇒ 只读「建好那一刻」的矩形/档位，不验真机观感；`A` 组真的 `ShowLangList()`
并在 `finally` 里 `HideLangList()` 复原（不给后面留现场）。

---

## 五 · 行数变化（供同步点 `--fix` 重钉）

| 文件 | HEAD | 现读 | 变化 | 本包贡献 |
|---|---|---|---|---|
| `Battle/SettingsPanel.cs` | 2,959 | **3,000** | **+41** | **+41**（全归本包） |
| `Battle/BattleDriver.cs` | 42,205 | **42,356** | **+151** | 本包 **+20**（前序 `B141-W8` 已 +131） |
| `Editor/BattleScene.cs` | 51,505 | 51,505 | **0** | 0（同一物理行追加） |
| `Editor/B141_W8b_Assert.cs` | —— | **154** | 新建 | —— |

- 行尾：`SettingsPanel.cs` HEAD 与现读**都是纯 LF**（`b.count(b'\r\n')` = 0）；`BattleDriver.cs` 仍纯 CRLF、无裸 CR。**没有翻行尾**。
- `BattleDriver.cs` 的本包 +20 分布在 `:29079`（+1）/`:29085`（+1）/`:29172-29179`（+7）/`:29072` 附近注释 4 行/类尾读口 8 行 —
  ⇒ **`:29072` 之后整段行号 +20**（含 `B141-W8` 那 +131 的叠加位移）。`SettingsPanel.cs` 的 `:686` 之后整段 +41。
- 两文件都在 `check_ledger_points` 的引用面里 ⇒ 同步点一次 `--all-A --fix` 后**复跑看真实数字**（`--fix` 第一遍印的是修前快照）。

---

## 六 · 还差什么

1. **真机**：语言下拉缩高后的观感（底板端帽 / 与下方静音行的 22.7 px 重叠关系是否改变）—— 重叠量只与**列表底边**有关，
   现在底边上收 41.79 ⇒ 重叠**消失**（原来是列表底伸到面板 y = −249.3、静音行在 −281.5…−226.6）。
   这是**变好**，但要真机看一眼确认不是原版另有一条我们需要补的间距。
2. **`ManaText` 换档后的字位**：`Midline` 会把数字整体下移一点（相对 `Middle`）；原版就是 Midline ⇒ 方向正确，
   但「下移多少」要真机对着原版截图比（`资料/原版实拍/`）。
3. **`_titleMe/_titleFoe` 的 `OrigFace` 不一致**（见 §3·3）—— 未改，交调度台。

---

## 七 · 出处索引

- `4-05`：`TMP_Dropdown.Show():926-932` · `判据/B141_W129_对账_UI动画.md` §4 · `报告/B138_审计汇总_问题清单.md:186` ·
  同族第一处 = `报告/B141_W129_SettingsWindow.md` §二（`4-05`）
- `4-10`：`报告/B138_审计汇总_问题清单.md:191` · `判据/B141_W129_对账_UI动画.md:32` · 原版 prefab 现读见 §3·1 那张表
- `Label.VAlign` / `OrigFace`：`Battle/Label.cs:2006-2015`（枚举值 = TMP 的原值）· `:2027-2029`（两份度量）
