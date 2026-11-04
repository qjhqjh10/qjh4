# B141_W129_Mult2Clamp — `m_ColorMultiplier = 2` 的「夹取次序」统一

执行写手：`B141-W57`（第一百二十八会话 · 2026-11-04）· 批次 `B141` · 条目 `B141-MULT2-CLAMP`

**一句话**：原版 UGUI 那条链 **`tintColor * colorMultiplier` 全程不夹**，夹在**最后一次字节顶点色写入** ⇒ 唯一算式
`Clamp01(底座 × 色键 × ColorMultiplier)`；两套按钮（`Shell/WindowButton` 与 `Battle/BattleButtonFx`）从本笔起
**调用同一个静态口** `WindowButton.ComposeVertexColor`。

---

## 一、UGUI 源码结论（逐跳，全部本机现读）

| # | 步骤 | 本机出处 | **夹不夹** |
|---:|---|---|---|
| ① | `Selectable.DoStateTransition` → `StartColorTween(tintColor * m_Colors.colorMultiplier, instant)` | `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Selectable.cs:702` | **不夹**（原文就是一次 `Color * float`） |
| ② | `Graphic.CrossFadeColor(Color targetColor, …)`：`startColor = canvasRenderer.GetColor()`，`targetColor` 原样作终点，`colorTween.AddOnChangedCallback(canvasRenderer.SetColor)` | 同包 `…/UI/Core/Graphic.cs:1009-1030` | **不夹**（补间途中与终点都不夹） |
| ③ | `CanvasRenderer.SetColor(Color)` = **「Renderer multiply color」**：doc 原话「Set the color of the renderer. **Will be multiplied with the UIVertex color and the Canvas color**」 | `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.UIModule.xml:442` | **不夹**（它乘在图形自己的颜色上，而不是「先把自己夹好再乘」） |
| ④ | 顶点色的容器 = `UIVertex.color` = **`Color32`（8 位/通道）**：`Image.OnPopulateMesh` → `VertexHelper.AddVert(…, Color32, …)` | 同包 `…/UI/Core/Utility/VertexHelper.cs:273`（另有 `:239` / `:262` 两个重载） | **就在这里夹** —— `Color → Color32` 的隐式转换逐通道 `Mathf.Clamp01` |

⇒ **结论**：原版**没有**「先把色键乘出来再夹一次」这一跳 —— 夹发生在**乘积落进顶点色**那一刻。
所以 W34 原来的写法（`Clamp01(色键 × m)` 再乘底座）是**我们自己的口径**，不是原版；W35 的（只乘、留给存储层夹）方向对，
但它把 >1 留在 `Image.color` 里，**读回来**（自检/材质）仍是 1.5989 这种越界值。

**旁证（非本机）**：Unity 官方脚本参考对 `CanvasRenderer.SetColor` 只说「相乘」；社区/文档一致的说法是
`Color` 是**无界浮点**（>1 = HDR，`SetColor` 不夹），`Color32` 才是天然 0–1 的 8 位容器。
⇒ 「乘完再夹」这一条与「Renderer color 不夹」是**同一件事的两面**。

> ⚠️ **残差（如实写）**：`CanvasRenderer` 是 **native**，本机拿不到它的实现体。本结论的硬证据是 ①–④ 的
> **managed 层全程不夹** + **顶点色是 Color32**。若要 100% 钉死「renderer 颜色本身也不按字节存」，
> 唯一的判定实验是**跑 Unity**：`canvasRenderer.SetColor(new Color(2,2,2,2))` 之后 `GetColor()` 读回来，
> 看是否仍是 2（是 ⇒ 浮点存储，本结论成立）。本笔**不跑 Unity**，这一格留给宿主自检。

---

## 二、统一后的公式与落点

```csharp
// 唯一实现（Shell/WindowButton.cs 类尾 · :1307）
public static Color ComposeVertexColor(Color baseColor, Color key, float multiplier)
    => new Color(Clamp01(baseColor.r * key.r * multiplier), … ×4);
```

| 文件 | 改前 | 改后 |
|---|---|---|
| `Shell/WindowButton.ApplyTint`（`:821`） | `k = Clamp01(色键 × m)` → `底座 × k` | `ComposeVertexColor(底座, 色键, m)`（**乘完再夹**）；α 仍沿用本类历史口径 `= 底座 α` |
| `Battle/BattleButtonFx.Apply`（`:244`） | `new Color(_base.r*_key.r, …)`（只乘不夹，>1 外泄到 `Image.color`） | `WindowButton.ComposeVertexColor(_base, _key, 1f)`（乘数已在 ctor 乘进四态，`KeyForTest` 语义不变） |

**统一后两套在绿族 Continue 钮上的取值**（底座 = `m_NormalColor` = (0.3686275, 0.8941177, 0.5874373)，倍率 2）：

| 态 | 改前 WindowButton | 改前 BattleButtonFx | **统一后（两侧相同）** |
|---|---|---|---|
| Normal | (0.2718, 0.8941, 0.5874) | (0.2718, 1.0, 0.6901) | **(0.27177245, 1.0, 0.69016510)** |
| Highlighted | (0.2718, 0.8941, 0.5874) | — | (0.3440…, 1.0, 0.4700…) |
| Pressed | (0.2718, 0.8941, 0.5874) | — | (0.3527…, 0.8836…, 0.5666…) |
| 倍率 = 1 时 | (0.1359, 0.7994, 0.3451) | (0.1359, 0.7994, 0.3451) | (0.13588623, 0.79944638, 0.34508255)（**不变**） |

---

## 三、对既有按钮的影响面

`m_ColorMultiplier = 2` 全库只有 **15 颗**（`B131` 色彩 A14 / W34 §5-06 逐颗表），我们树里**真正接上的只有 5 处**：

| 站点 | 走哪套 | 改后是否有可见变化 |
|---|---|---|
| `Shell/PracticeModePopup.cs` ▸ `BattleHit`（`WireContinueButton`） | **WindowButton** | **有**：四个态的 G（及 H 态的 G/B）从「被色键提前压掉」变成**饱和**——正是本笔要修的那一档 |
| `Shell/ParentEventWindow.cs` ▸ `Two Side Event Select Team A/B`（2 颗） | **WindowButton** | **有**（同上） |
| `Shell/GiveFeedbackPopup.cs` ▸ `Feedback Scoring Button` | **WindowButton** | **有**（同上；该颗原版 `trans=0`，色偏本来就不该有观感，却把我们的悬停色压平 —— W34 B6b 已记） |
| `Battle/CardChoicePanel.cs` ▸ `Continue Button` | BattleButtonFx | **无可见变化**：`Image.color` 里从 1.5989 变成 1.0，而 UGUI 顶点色本来就在 1.0 饱和 |
| `Battle/MultiCardDisplay.cs` ▸ `Continue Button` | BattleButtonFx | **无可见变化**（同上） |

⚠️ **其余 10 颗**（`RankedEventWindow` / `RankedEventWindowV2` / `SkirmishModeEventWindow` / 3 颗 `Draft Mode … Debug Battle Button` 等）
在我们的树里**没有落点**（W52 §三 逐条反证）⇒ 本笔对它们零影响。

**倍率 = 1 的 1,252 颗按钮：逐位零变化**（`ComposeVertexColor` 与旧算式在 `m = 1` 且底座 ≤ 1 时完全相同）。

---

## 四、行数变化

| 文件 | 开工 | 现在 | Δ | 说明 |
|---|---:|---:|---:|---|
| `Shell/WindowButton.cs` | 1282 | 1315 | **+33** | `ApplyTint` 那一块**逐行等长**（`Interactable:945` / `Click:1153` / `PlayClickSound:1231` 行号**一个没动**，实测现读）；+33 全在**类尾**（`RecordMissingArt` 之后），类尾之后没有账落点 |
| `Battle/BattleButtonFx.cs` | 340 | 342 | **+2** | `Apply` 里 3 行 → 5 行（含判据注释）；本文件**零账落点**（`grep 账/*.tsv` 现读） |
| `Editor/B141_W34_Assert.cs` | 262 | 276 | **+14** | B5 一拆三 + 口径订正注释 |
| `Editor/B141_W35_Assert.cs` | 311 | 320 | **+9** | `Mul(...)` 改「乘完再夹」+ 判据注释 |
| `Editor/B141_W57_Assert.cs` | —— | 303 | 新建 | **26 条断言** |

> ⚠️ **账侧**：`Shell/WindowButton.cs` 在 `资料/完全复刻/账/*.tsv` 里有 **十余处 `文件:行号` 落点**
> （`菜单_外壳.tsv` 的 `:138/:141/:166/:468/:506/:527/:542/:568/:691/:708/:946/:1048/:1126`、`收藏.tsv:158`、
> `商店.tsv:344/:349`、`日常_奖励.tsv:776-778`）。**它们本来就已整体漂移**（现读 `Click` 在 `:1153`，账里写 `:1048`）。
> 本笔**没有让漂移变大**（`ApplyTint` 那块等长、新增全在类尾），但仍建议调度台在「没有 `.cs` 写手」时跑一次
> `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`。
> **新增符号**（便于按符号名重定位）：`WindowButton.ComposeVertexColor`。

---

## 五、断言（`Editor/B141_W57_Assert.cs` · 26 条）

`Run()` **零前提**（自带临时根 + 收尾 `DestroyImmediate`；⛔ 不建窗、不写盘、不读资源）。

| 组 | 条数 | 内容 |
|---|---:|---|
| **A（纯算式，期望值自算）** | 8 | 白×白×2 = 白 · 绿×绿×2 = (0.2718, **1.0**, **0.6902**) · 🧨 与旧口径（0.2718, 0.8941, 0.5874）**必须不同**且 R 相同 · 倍率 1 那一档 · α 也乘完再夹（1.0039 ⇒ 1.0）· 底座 >1 不外泄 |
| **B（行为：两套同一组顶点色）** | 12 | Normal / Highlighted / Pressed 三轮 **WindowButton ↔ BattleButtonFx 逐通道相等**；Selected 对公式；默认灰键 + 白底 + 倍率 1 两侧相等且不饱和；倍率 1 两侧相等 |
| **C（源码守卫 · 灭自证）** | 6 | `ApplyTint` 走 `ComposeVertexColor(b, _k, ColorMultiplier)` 且**旧写法不存在** · `BattleButtonFx.Apply` 走**同一个**静态口且旧裸乘不存在 · 唯一算式只在 `WindowButton`（没有第二份） |

**灭自证表（改坏法 → 哪条红）**

| 改坏法 | 变红的断言 |
|---|---|
| `WindowButton` 改回「先 `Clamp01(色键 × m)` 再乘底座」 | `C1🅑` + `B1🅐/B2🅐/B3🅐`（B 组拿公式算期望） |
| `BattleButtonFx` 改回「只乘不夹」 | `C2🅐/C2🅑` + `B1🅑/B2🅑/B3🅑`（它那侧会给出 >1） |
| 任一侧把乘数吃掉 | `A4/A4b/B6` |
| 把 `ComposeVertexColor` 的 `Clamp01` 删掉 | `A2/A3/A5/A6` 全红 |
| 在任一侧**另写一份**乘法 | `C2🅐/C2🅒` |

> ⚠️ **单边改回旧口径时，只有一侧会变** ⇒ B 组「两侧相等」必然红 —— 这就是本条的结构位（⛔ 不是两边一起改回去就能绿）。
> ⚠️ 本文件**不跑 Unity**；「断言真的被执行」要等调度台把它接到宿主（挂法写在文件头），**⛔ 不写「跑过了」**。

**同步改动的既有断言**：
- `B141_W34_Assert`：`B5` 由「先乘再 Clamp01」改为钉新机制，并新增 `B5c`（旧写法不许回来）/ `B5d`（唯一算式在 `ComposeVertexColor`）；
  `A2` 的口径注释就地订正（**白底座上两种口径结果相同**，所以那条值不变），并注明「夹在哪一步」由 `B141_W57` 钉。
- `B141_W35_Assert`：`Mul(...)` 改为 `Clamp01(乘积)`（`B1/B3/B4/B8/E1/E3/E5` 的期望随之变；`B2a` 的 R = 0.2717724 **不变**）。

---

## 六、❓ 未闭合 / 顺手发现（只记，不改）

1. 🔴 **`Editor/B141_W52_Assert.cs` 会因本笔变红，但它在白名单外 ⇒ 本笔没动**（⛔ 请调度台另派）：
   - `A7/A8`（`:153-170`）仍按旧口径算期望 `base × Clamp01(Normal × 2) = (0.2718, 0.8941, 0.5874)`；
     新值应为 **(0.2718, 1.0, 0.6901)**（把那段 `k2/prod2` 换成 `Clamp01(face × face × 2)` 即可）。
   - `B9`（`:228`）断言源码含 `Mathf.Clamp01(k.r * ColorMultiplier)` —— 该写法已删 ⇒ **必红**，
     应改为 `ComposeVertexColor(b, _k, ColorMultiplier)`。
   - 现读该文件**未接任何宿主**（全仓 `B141_W52_Assert.Run(` 0 命中）⇒ 今天不会把宿主跑红，但留着就是定时炸弹。
2. 🟠 **两套的 α 语义仍然不同**（**不属本笔**）：`WindowButton.ApplyTint` 一直**不叠状态色 α**（用底座 α），
   而 `BattleButtonFx` 叠 `底座.α × 色键.α` 并（现在）夹到 1.0。
   - 在 `m = 2` 那族上**恰好等价**（`0.5019608 × 2 = 1.0039 ⇒ 1.0`，两侧都是不透明）；
   - 但在 `m = 1` 的禁用档上会不同（原版 `m_DisabledColor.a = 0.502` ⇒ **半透明**，我们 `WindowButton` 这一路是不透明）。
   - 判据 = UGUI 那条链本来就把 α 一起乘（`Selectable.cs:702` 的 `tintColor * colorMultiplier` 含 α），
     ⇒ **我们这一侧是偏离**。要不要改是**另一条**（会动到 504 颗 `a = 0.502` 的钮的禁用观感），本笔只记不动。
3. 📌 **`CanvasRenderer` 是 native** —— 见 §一 那条残差与判定实验。

---

## 七、改了哪几个文件

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowButton.cs` | `ApplyTint` 改为「乘完再夹」并走统一口；类尾追加 `ComposeVertexColor`（+33，零账漂） |
| `Unity/MyGame/Assets/CardPresentation/Battle/BattleButtonFx.cs` | `Apply` 改调 `WindowButton.ComposeVertexColor`（+2） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B141_W34_Assert.cs` | B5 改钉新机制 + B5c/B5d；A2 口径注释订正（+14） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B141_W35_Assert.cs` | `Mul(...)` 改「乘完再夹」+ 判据注释（+9） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B141_W57_Assert.cs` | 新建（303 行 / 26 条断言） |
| `Unity/资料/完全复刻/报告/B141_W129_Mult2Clamp.md` | 本文件 |

**验证**：`TMPDIR=/tmp/wf_b141_w57 bash 工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**（改完全部文件后跑，一次通过）。
