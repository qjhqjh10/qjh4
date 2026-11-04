# B141 · W129 · P7 / `5-04` — 选卡面板 `Continue Button` 绿底 · 四态 · `m_ColorMultiplier 2`

> 执行代理 `B141-W35` · 2026-11-04（第一百二十八会话）。
> 判据来源 = `判据/B141_W129_对账_卡组色彩.md` `5-04` 行 · `报告/B138_审计汇总_问题清单.md:225` · `报告/B131_审计_色彩材质与渲染.md` C18/A14/A15 ·
> **最终判据 = 原版资产现读**（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1`）。
> ⛔ 本代理未跑 Unity、未动 git、未改 `d:/2`、未改账主表 / 活状态表 / 所有权表 / `CLAUDE.md`。
> 写入面 = `Battle/BattleButtonFx.cs` · `Battle/CardChoicePanel.cs` · 新建 `Editor/B141_W35_Assert.cs` · 本文件。

---

## 一、原版判据（逐字段现读，非转述）

| 来源 | 字段 | 值 |
|---|---|---|
| `bundle_scenes_scenes_battlearena1/MonoBehaviour_4262.json` | `m_GameObject` | `779`（GO **Continue Button** 那一族） |
| 同上 | 脚本类名（`bundle_Waprforge_monoscripts/MonoScript_-4448497653027179337.json`） | **`Button`（`UnityEngine.UI`）** |
| 同上 | `m_Transition` / `m_Interactable` | `1`（ColorTint）/ `1` |
| 同上 | `m_Colors.m_NormalColor` | `(0.3686274886, 0.8941176534, 0.5874372721, 1)` |
| 同上 | `m_Colors.m_HighlightedColor` | `(0.4666666985, 0.9137255549, 0.4000000358, 1)` |
| 同上 | `m_Colors.m_PressedColor` | `(0.4784314036, 0.4941176772, 0.4823529720, 1)` |
| 同上 | `m_Colors.m_SelectedColor` | `(0.3686274588, 0.8941177130, 0.5882353187, 1)` |
| 同上 | `m_Colors.m_DisabledColor` | `(0.7843137383, 0.7843137383, 0.7843137383, **0.5019608140**)` |
| 同上 | **`m_Colors.m_ColorMultiplier`** | **`2.0`** |
| 同上 | `m_Colors.m_FadeDuration` | `0.1`（= 本类 `FadeSeconds`） |
| 同上 | `m_TargetGraphic` → `MonoBehaviour_4210.json` = 子件 `BG`(GO `1153`) 的 `Image` | `m_Color = (0.3686274886, 0.8941176534, 0.5874372721, 1)`（**与 N 同一个绿**） |

**复合式（唯一判据链，三跳都已本机现读）**

```
屏上 = 贴图 × Image.m_Color × (stateColor × m_ColorMultiplier)
```

* ① `Selectable.DoStateTransition` = `StartColorTween(tintColor * m_Colors.colorMultiplier, instant)`
  —— 本机 `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Selectable.cs:702`（**α 一起乘**）。
* ② `Graphic.CrossFadeColor` → `canvasRenderer.SetColor(...)` —— 同包 `…/Graphic.cs:1009`。
* ③ `CanvasRenderer.SetColor` 的 doc 原话「Will be multiplied with the UIVertex color and the Canvas color」
  —— 本机 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.UIModule.xml:442`。
  ⇒ **Image 自己的 `m_Color` 不是被顶掉，而是再乘一次**。

⇒ 静止时屏上 = 贴图 × `(0.3686275, 0.8941177, 0.5874373)` × `(0.3686275, 0.8941177, 0.5874373)` × 2
= 贴图 × `(0.2717724, 1.5988922, 0.6901660)`。**只留一处绿**会变成 `(0.7372549, 1.7882353, 1.1748746)`
—— 这是本条目最容易被做错的地方（R 通道差 2.7 倍）。

---

## 二、我们改了什么（原版值 → 改前 → 改后）

| 位置 | 改前 | 改后 | 原版 |
|---|---|---|---|
| `Battle/BattleButtonFx.cs`（新增常量块） | 无「选卡 Continue」族常量 | `ContinueNormal / ContinueHighlighted / ContinuePressed / ContinueSelected` + `ContinueColorMultiplier = 2f` | 上表 |
| `BattleButtonFx.Tint(...)` | 四个可选状态色，**无倍率** | 多一个 `float colorMultiplier = 1f`；四个状态色**在工厂里乘好**（`(normal ?? Normal) * m`，四通道含 α） | `tintColor * colorMultiplier` |
| `Battle/CardChoicePanel.cs:500-501`（建底条） | `MakeImg(..., BarW, BarH, Z)` ⇒ 基准色 **白** | `MakeImg(..., BarW, BarH, Z, BattleButtonFx.ContinueNormal)` ⇒ 基准色 = 原版 `Image.m_Color` 绿 | 绿 |
| `CardChoicePanel.MakeImg(...)` | 恒 `Color.white` | 多一个 `Color? color = null`（缺省白 ⇒ 另一处调用点「卡钮 :871」零变化） | —— |
| `CardChoicePanel.BuildFeedback()`（`:902`） | `Tint(_bar, "Continue Button")` ⇒ 白/0.9608/0.7843、**无 ×2、不立刻落色** | ① 先 `_bar.SetTint(ContinueNormal)` 还原基准色 ② `Tint(_bar, "Continue Button", N, H, P, Disabled, 2f)` ③ `ApplyInstant()` | 绿四态 × 2 + 进面板立刻是 Normal |

**行数变化**

| 文件 | 行数（HEAD → 现读） | git diff |
|---|---|---|
| `Battle/CardChoicePanel.cs` | **1501 → 1501（零变化，含 W35b 续做）** | `16 / 16`（全部就地改写：账按 `文件:行号` 引用该文件 12 处，零漂移） |
| `Battle/BattleButtonFx.cs` | **308 → 339（+31）**（全部是常量块与 doc；该文件**无账落点**） | `35 / 4` |
| `Editor/B141_W35_Assert.cs` | 新建 | —— |

> ⚠️ `CardChoicePanel.cs` 的零行数变化是刻意的：账 `战斗界面_HUD.tsv` 有 12 条 A 行按行号指它（`:405`/`:602`/`:645`/`:710`/`:719`/`:732`/`:743`/`:756`/`:797`/`:826`/`:1003`/`:1187`/`:1200`）。

---

## 三、两处必须解释的实现细节

1. **为什么基准色要在这里传（而不是「Normal 已经是绿了，Image 给白就行」）** —— 见 §一复合式：原版 `Image.m_Color` 与 `m_NormalColor` **同为那个绿**。只留 Normal ⇒ R 从 0.2717 抬到 0.7373，屏上是明显更亮的绿。
2. **为什么 `BuildFeedback` 每次都要先 `_bar.SetTint(ContinueNormal)`** —— `BattleButtonFx` 的 `_base` 是在**构造时**从 `target.Tint` 抓的（`_base = target.Tint`）。`LayOutCards` 每次 `Open` 都会重建 fx，若不还原，第二次抓到的就是**上一次的合成色**，`base × state × 2` 会逐次翻倍（越开越偏、零报错）。同族先例 = `SettingsPanel.BuildButtonFeedback` 里那句 `_azBox.SetTint(Color.white)`。
3. **`ApplyInstant()` 必须在建完就调** —— `Tint()` 工厂不像 `Swap()` 那样自己 `Apply()`；不调的话静止态停在「只有基准绿、没有 ×2、也没有 Normal 那一乘」，第一次悬停才跳一下。原版是 `OnEnable → DoStateTransition(Normal, instant)`。

---

## 四、断言（`Editor/B141_W35_Assert.cs`，**35 条**，零前提）

| 组 | 条数 | 盯什么 |
|---|---:|---|
| A · 常量 = 原版字面值 | 8 | 四态五格逐通道（tol 1e-6）、倍率 2、禁用 α 0.5019608、`FadeDuration 0.1`、S−N 只差 B 0.0007979 |
| B · 复合语义 | 11 | 静止/悬停/按下/**禁用**四档各自的 `base × state × 2`；重建不越乘（B9）；读的是 `Image.color`（B10） |
| C · 既有调用点零变化 | 4 | 缺省（白底 + 白 N + 倍率 1）静止/悬停/按下 = 改前值；显式传色但不传倍率 = 原样 |
| D · 面板接线（源码守卫） | 6 | `_bar.SetTint(ContinueNormal)` · 四态 + 倍率实参 · `ApplyInstant()` · 建底条时传基准绿 · `SetDisabled(!on)` · 重建后重放禁用态 |
| E · 确认钮禁用态（`B141-W35b`） | 6 | 禁用色 `base × D × 2` · 「真灰了 / 明确记了没灰成」恰好一条为真 · 恢复后不灰 · 闸关掉时不灰也不报错（灭自证） |

**灭自证（🧨，结构上不可能同时满足）**

* 🧨1 —— B1 钉 `base × N × 2 = 0.2717724`，B2a 单独钉 R；**漏基准色**（→0.7372）或**漏 ×2**（→0.1359）都会红。
* 🧨2 —— B5 用**同一组常量、不传 multiplier** 再建一颗 ⇒ 期望 `base × N = 0.1358862`。把 2 烘进 `ContinueNormal` 常量 ⇒ B1 与 B5 **同时**红。
* 🧨3 —— B6/B7 用**原版字面量**算出的复合值断言 N≠H、N≠P（差 > 0.05）⇒ 退回「一套默认 0.9608/0.7843」两处一起红。
* 🧨4 —— D 组按源码串钉接线 ⇒ 只改注释 / 摘接线 ⇒ D 红（**接线守卫**，不替代 B 组的行为断言）。
* 🧨5（`B141-W35b`）—— E2 要求「`GrayedForTest` / `GrayShaderMissingForTest` **恰好一条为真**」，E6 在闸关掉时要求**两条都假**；
  把灰化写死成常量、或失败时静默 return ⇒ 两句不可能同时绿。

**接线（`B141-W35b` 已完成）**：`Editor/BattleScene.cs:47035` 那条方法尾聚合行、`Debug.Log(P + $"=== 合计…")` 之前，同一物理行已追加：

```csharp
B141_W35_Assert.Run(); pass += B141_W35_Assert.Pass; fail += B141_W35_Assert.Fail;
```

判绿红：`grep "^\[B141W35\] 合计"`；逐条按**行首** `[B141W35] ✓` / `[B141W35] ✗` 数（⛔ 别 `grep -c ✗`）。

---

## 五、❓ / 顺手发现（只记，未做）

1. **判据文档里有一处名实不符，已就地订正**：`MonoBehaviour_4262.json` 的脚本类是 **`UnityEngine.UI.Button`**，**不是** `EverguildButton`
   （`bundle_Waprforge_monoscripts/MonoScript_-4448497653027179337.json` 的 `m_ClassName` = `Button`；B131 C18 与 B141 `5-04` 行都用「EverguildButton」泛指了这一族）。
   ⇒ 两条直接后果：① 没有 `colorTintGreyOnDisable` 那一跳 ⇒ 禁用 α 保持序列化的 **0.5019608**（所以本件显式传 `BattleButtonFx.Disabled`，⛔ 不是 `DisabledOpaque`）；
   ② `CardChoicePanel.cs` 旧 doc 说的「四颗件各挂一颗 `EverguildButtonStateFollower`」与这一条**不矛盾** —— 灰化来自**旁挂的 follower 组件**，不是按钮自己的 flag。该 doc 已在原位补一句区分。
2. **禁用观感已接（原「遗留」已消）**：`SetConfirmInteractable(false)` → `_fxBar.SetDisabled(true)`；见 §七。原前提「当时没有灰化 shader」自 `B117-G10-03` 起已不成立。
3. **`ContinueSelected` 只承载原值**：本类状态机没有 Selected 态（同已有的 `Selected` 常量）。
   本面板点完就关、没有 EventSystem 选中过程 ⇒ 今天**不可观测**；⛔ 别为它造一个没有调用者的 `SetSelected`。
4. **底条基准色被 `BuildFeedback` 接管**：目前 `_bar` 的 tint 只有这条链在写，所以「每次重建还原基准色」是安全的；
   将来若有别的代码给底条染色（例如压暗/高亮），必须先还原再建 fx，否则会重演 §三·2 的翻倍。
5. **未验证项（本代理不跑 Unity）**：屏上观感（绿底亮不亮、羽化边有没有被 ×2 冲白）**只做了数值推导，没有截图**。
   建议下一次 `BattleScene.Run` 顺带跑 `Editor/BattleScene.cs §20 选牌面板` 那一段，看 `BarPieceForTest.Tint` 是否为 `(0.2717, 1.5989, 0.6902)`。

---

## 六、typecheck

```
TMPDIR=/tmp/wf_b141_w35 bash 工具/typecheck.sh
```

* 首跑（19:23）：**运行时 32 错 + 编辑器 1 错，全部落在别人正在写的文件**
  （`Shell/WindowButton.cs` `_selectedTex` / `ColorMultiplier`、`Deck/DeckRuntime.cs` `DrawerTw` / `_armPrevPx` / `HandleScrollDragTick`…、`Core/FilterPanelModel.cs` `ArmyRowVisible`）
  —— mtime 19:23:18 / 19:23:20 = 同批其它写手**当刻**在飞（`CLAUDE.md` 铁律 13·3 ② 的已知现象：编辑器那半边的 `CS0006` 是运行时 DLL 没编出来导致的级联）。
* **复跑（19:26:35 起，同批写手落盘后）：`运行时错误数: 0` / `编辑器错误数: 0`（exit 0）** ✅
  ⇒ 本代理三个文件（`Battle/BattleButtonFx.cs` · `Battle/CardChoicePanel.cs` · `Editor/B141_W35_Assert.cs`）**0 错**。
* 账闸旁证（只读跑 `工具/check_ledger_points.py`）：输出里 **`CardChoicePanel` 零命中**（= 该文件那 12 条 A 行**零漂移**，与 §二「1501 → 1501」一致）。

---

## 七、`B141-W35b` 续做 —— 确认钮**禁用态**接线

### 7.1 原版判据
* `Continue Button` 子树四颗件（父 `Continue Button` / `BG` / `ContinueText` / `ContinueButton`）各挂一颗
  `EverguildButtonStateFollower`（`m_Script -9096086197743689236`，无序列化字段）；
  `interactable = false` ⇒ `…StateFollower__Disabled.c` → `DoMaterialRefresh` ⇒ 换
  `EverguildButtonHelper.get_DisabledMaterial` 造出来的**灰度材质**（`Everguild/UI/Greyscale`）。
* **同时**按钮自己的 `m_Colors.m_DisabledColor`（`0.7843137`×3、α **0.5019608**）× `m_ColorMultiplier 2` 照常生效
  ⇒ 两者**同时**，⛔ 不是二选一。
* 流程闸：`ChooseCardMenu__Setup.c:58` = `set_interactable(chooseButton, 0)`；`ChooseCardMenu__SelectCard.c:29` = `…1`（全类只有这两处）。

### 7.2 改法（`CardChoicePanel.cs`，**行数 1501 → 1501，零变化**）
| 位置 | 改前 | 改后 |
|---|---|---|
| `SetConfirmInteractable(bool on)`（`:786-789`） | 只写 `_confirmInteractable = on;`（视觉什么都不做） | 同一行追加 `if (_fxBar != null) _fxBar.SetDisabled(!on);` |
| `BuildFeedback()`（`:902`） | 建完 `ApplyInstant()` 就结束 | 追加 `_fxBar.SetDisabled(!_confirmInteractable)` —— **重建后重放**禁用态（否则懒重建会把钮刷回可交互） |
| `SetConfirmInteractable` 的 doc（`:774-785`） | 写着「视觉上的灰掉这一波没做 / 没有 shader」 | 就地改写为「已接线 + 判据链 + 只接了 `BG` 一颗」 |

* 颜色那一半走 `BattleButtonFx.Disabled`（α 0.5019608）× 2 ⇒ 运行时白到过曝、和原版一致；灰化那一半走 `ApplyGray`
  （取不到 shader ⇒ **不变灰但出声**，`GrayShaderMissingForTest` 可断，⛔ 不静默）。
* ⚠️ **只接了 `BG`（`_bar`）一颗**：父命中面 α=0（看不见）、`ContinueButton`、`ContinueText` 三颗**还没挂** ——
  它们不是 `HudPiece`，要接得先各建一份反馈对象；记在这里，⛔ 别拿一件的灰冒充四颗都对。

### 7.3 断言（新增 8 条：E 组 6 + D5/D6；总计 **35**）
| 编号 | 盯什么 |
|---|---|
| E1 | 禁用 = `base × D × 2`（色那一半；与 B8 的「闸关掉」互为对照） |
| 🧨 E2 | `GrayedForTest` / `GrayShaderMissingForTest` **恰好一条为真** —— 要么真灰了、要么明确记了「想灰没灰成」 |
| E3 | 恢复可交互 ⇒ `base × N × 2` |
| E4 | 恢复后 `GrayedForTest == false`（材质还回去了） |
| E5 | `ColorTintGreyOnDisable = false` 时仍走 Disabled 色（只变色、不灰） |
| 🧨 E6 | 闸关掉 ⇒ **既不灰、也不报「想灰没灰成」**（证明 E2 的闸是活的，不是写死的常量） |
| D5 / D6 | 源码守卫：`SetConfirmInteractable` 里真有 `_fxBar.SetDisabled(!on)`；`BuildFeedback` 里真有重放 |

### 7.4 断言接线（本次已做）
`Editor/BattleScene.cs:47035`（方法尾聚合行、`Debug.Log(P + $"=== 合计…")` 之前）同一物理行已追加
`B141_W35_Assert.Run(); pass += B141_W35_Assert.Pass; fail += B141_W35_Assert.Fail;`
—— **`BattleScene.cs` 行数 51,505 → 51,505（零变化）**，CRLF 未翻（裸 CR 0）。

### 7.5 typecheck
`TMPDIR=/tmp/wf_b141_w35 bash 工具/typecheck.sh` 最终复跑（19:36:28）：
**`运行时错误数: 0` / `编辑器错误数: 0`（exit 0）** ✅
中间两次跑的错全在别人当刻在写的文件（`Editor/B141_W32_Assert.cs` 缺 `using System.Collections.Generic` ·
`RuleEngine/Editor/B141_W39_Assert.cs` 缺 `using RuleEngine`），**本代理三个文件 0 错**。
