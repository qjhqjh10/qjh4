# B141_W129_DisabledAlpha — 禁用档 α 也乘状态色（两套按钮机制统一）

执行写手：`B141-W71`（第一百二十八会话 · 2026-11-04）· 批次 `B141` · 条目 `B141-DISABLED-ALPHA`

**一句话**：`Shell/WindowButton.ApplyTint` 原来算完 `Clamp01(底座 × 色键 × m)` 之后**把 α 换回底座 α**（`c.a = b.a`），
而 UGUI 原版那条链**四通道同乘、α 一起**（`Selectable.cs:702` + `:1091` 的 `useAlpha: true`）。
本笔删掉那一句 ⇒ 两套按钮（`WindowButton` / `BattleButtonFx`）在 N/H/P/S/Disabled 五档、`m = 1` 与 `m = 2` 上**逐通道相等**。

---

## 一、UGUI 判据（本机现读，逐跳）

| # | 事实 | 出处 |
|---:|---|---|
| ① | `DoStateTransition` 的 ColorTint 支：`StartColorTween(tintColor * m_Colors.colorMultiplier, instant)` —— Unity 的 `Color * float` **四通道同乘，含 α** | `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Selectable.cs:702` |
| ② | `StartColorTween` 末句 `m_TargetGraphic.CrossFadeColor(targetColor, …, true, **true**)`（第 4 个实参 = `useAlpha`）⇒ **α 确实参与补间** | 同文件 `:1086-1092`（调用点 `:1091`） |
| ③ | `Graphic.CrossFadeColor`：`targetColor` 原样当补间终点，逐帧 `canvasRenderer.SetColor`，**途中与终点都不夹** | 同包 `Graphic.cs:1009-1030` |
| ④ | `CanvasRenderer.SetColor` = 「Renderer **multiply** color」，乘在图形自己的颜色上 | `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.UIModule.xml:442` |
| ⑤ | 顶点色容器 `UIVertex.color` = **`Color32`** ⇒ 夹发生在最后一次字节写入 | 同包 `Utility/VertexHelper.cs:273` |

⇒ **唯一算式**：`Clamp01(底座 × 色键 × colorMultiplier)`，**四通道，α 一起**。

> ⚠️ 残差（沿用 W57 的记法）：`CanvasRenderer` 是 native，本机拿不到实现体；判定实验（`SetColor(2,2,2,2)` 后 `GetColor()` 读回）要跑 Unity，本笔不跑。

---

## 二、改前 / 改后

| 侧 | 改前 | 改后 |
|---|---|---|
| `Shell/WindowButton.ApplyTint` | `c = ComposeVertexColor(b, _k, m)` 之后 **`c.a = b.a`**（α 取底座）⇒ 状态色 α 永远不生效，禁用档**不透明** | 四通道全部来自 `ComposeVertexColor`（**无 α 覆写**） |
| `Battle/BattleButtonFx.Apply` | 已经是 `ComposeVertexColor(_base, _key, 1f)`（乘数在 ctor 乘进四态，**含 α**） | **未改**（本来就对；本笔只是让另一侧对齐它） |

**统一后**（底座 α = 1，禁用色 α = 0.5019608）：

| 档 | 乘数 | 改前 `WindowButton` | 改前 `BattleButtonFx` | **改后（两侧相同）** |
|---|---:|---|---|---|
| N/H/P/S | 1 / 2 | 底座 α（1.0） | 色键 α（1.0） | 1.0（**不变**） |
| **Disabled** | **1** | **1.0**（偏离） | **0.5019608** | **0.5019608** |
| **Disabled** | **2** | **1.0** | **1.0**（0.502×2 夹到 1） | **1.0**（**不变**） |

---

## 三、影响面（如实：**当前生产站点肉眼零变化**）

生效条件三条同时满足才会变：`ColorTintGreyOnDisable == false`（否则原版会把 `m_DisabledColor.a` 重写成 `GrayOnDisableAlpha = 1.0`，
见 `WindowButton.cs:207-215`）· 禁用色 α < 1 · 乘数 = 1（乘数 2 时 0.502×2 夹到 1.0）。

逐站点现读（`grep ColorTintGreyOnDisable =` 全仓 9 处 → 生产 2 处）：

| 站点 | 类型 | 乘数 | 禁用 α | 改后是否变 |
|---|---|---:|---|---|
| `Shell/PracticeModePopup.cs:3881` ▸ `BattleHit`（Continue 钮） | **WindowButton**（`grey=0`） | **2** | 0.5019608 | **不变**（×2 ⇒ 夹到 1.0） |
| `Battle/MultiCardDisplay.cs:652` ▸ `_fxBar` | **BattleButtonFx**（`grey=0`） | 2 | 0.5019608 | **不变**（本来就乘，×2 ⇒ 1.0） |
| 其余全部 `WindowButton` 站点 | — | 1 或 2 | 被重写成 **1.0**（`ColorTintGreyOnDisable` 缺省 true） | **不变** |

⇒ 本笔是**机制对齐**（把「α 取底座」这条自造口径去掉），**不是视野内的观感改动**；
它挡住的是「以后任何 `grey=0 + 乘数1 + α<1` 的站点会被画成不透明」这类静默偏离。
`GrayOnDisableAlpha` 全仓**没有任何站点覆写**（现读），所以 `grey=1` 那一族恒 1.0（D 组断言钉住）。

**副作用面**：`WindowButton` 的 α 从此跟随色键，凡是把 `NormalKey` / `HighlightKey` … 覆盖成 α<1 的站点都会**开始变半透明** ——
现读 **7 个**覆盖站点（`DraftModeWindow:3055` / `GiveFeedbackPopup:663` / `MainMenuRuntime:1397` / `MainMenuSubmenuWindow:914` /
`ParentEventWindow:802` / `PracticeModePopup:3874` / `SettingsWindow:4735`）**α 全是 1**，零影响。

---

## 四、行数变化

| 文件 | 开工 | 现在 | Δ | 说明 |
|---|---:|---:|---:|---|
| `Shell/WindowButton.cs` | 1315 | 1315 | **0** | `c.a = b.a;` 那**一行**就地换成判据注释；`ComposeVertexColor` 的 doc 两句就地改写 ⇒ **行号零漂移**（`Click:1153` / `PlayClickSound:1231` 未动） |
| `Battle/BattleButtonFx.cs` | 342 | 342 | **0** | 未改（本来就四通道同乘） |
| `Editor/B141_W71_Assert.cs` | —— | 312 | 新建 | **31 条断言** |

> ⚠️ 账侧：`Shell/WindowButton.cs` 有十余处 `文件:行号` 落点（本来已整体漂移，见 `B141_W129_Mult2Clamp.md` §四）。
> 本笔**行数零变化** ⇒ 漂移**一点没变大**。新增符号 = 无（`ComposeVertexColor` 是 W57 建的）。
> 仍然建议调度台在「没有 `.cs` 写手」时跑一次 `check_ledger_points.py --all-A --fix`。

---

## 五、断言（`Editor/B141_W71_Assert.cs` · 31 条）

`Run()` **零前提**（自带临时根 + 收尾 `DestroyImmediate`；⛔ 不建窗、不写盘、不读资源）。

| 组 | 条数 | 内容 |
|---|---:|---|
| **A（纯算式，期望自算）** | 5 | α 也乘（0.5019608）· 乘完再夹（×2 ⇒ 1.0）· 🧨 与「先夹色键」在 α 上可分辨（1.0 vs 0.9）· 🧨 与旧口径（α 取底座）差 > 0.4 · RGB 不变 |
| **B（行为 · 两套机制同一组顶点色；`m = 1` 与 `m = 2` 各 10 条）** | 20 | Normal / Highlighted / Pressed / Disabled 各两条（🅐 = 公式值 · 🅑 = 两侧相同）· Selected 对公式（战斗侧无这一档）· 禁用档 α **显式** = `底座α × 0.5019608 × m` |
| **C（源码守卫 · 灭自证）** | 5 | `c.a = b.a` **不存在** · `ComposeVertexColor(...)` 与 `SetTint(c)` 之间**无 α 覆写** · 战斗侧仍走同一口且**无自己的 α 覆写** · α 通道算式只有一份 |
| **D（回归）** | 1 | `grey = 1` 那一族禁用档 α 仍 **1.0**（原版重写 `m_DisabledColor.a`）—— 本笔不许把它画成半透明 |

**灭自证表（改坏法 → 哪条红）**

| 改坏法 | 变红的断言 |
|---|---|
| `WindowButton` 把 α 换回底座（`c.a = b.a` 回来） | `C1` + `C2` + `B1D🅐` + `B1Dα` + `A4` |
| `WindowButton` 干脆不叠 α（恒 1） | `B1D🅐` + `B1Dα`（期望 0.5019608） |
| `BattleButtonFx` 把 α 换回底座 | `B1D🅑`（两侧不等） |
| `ComposeVertexColor` 的 α 通道改成 `baseColor.a` | `A1/A2/A3` + 全部 D 组 |
| **两侧一起**改成「α 取底座」 | `A1` / `A4`（期望值自算，⛔ 不从实现读回来）—— 这就是挡「两边一起改回去」的结构位 |

> ⚠️ 本文件**不跑 Unity**；「断言真的被执行」要等调度台把它接到宿主（挂法写在文件头），**⛔ 不写「跑过了」**。

---

## 六、❓ 未闭合 / 顺手发现（只记，不改）

1. 🟠 **`WindowButton` 的 α 从此跟随色键** —— 如果将来有人把某颗的 `NormalKey` 覆盖成 α<1 想「恒定半透明」，
   现在会**再乘一次底座 α**。按原版语义这是对的（原版也是图形色 × 状态色）；但**现有 7 个覆盖站点的 α 全是 1**，
   真出现半透明键时**不要再回头加 `c.a = b.a`**，而应把半透明写进**底座色**。
2. 📌 **`B141_W52_Assert` 已被别的写手同步过**（它现在引用 `ComposeVertexColor` 与 W57 口径），本笔未动它，也没让它变红。
3. 📌 `CanvasRenderer` native 残差见 §一末尾（沿用 W57 的判定实验，本笔不跑 Unity）。

---

## 七、改了哪几个文件

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowButton.cs` | `ApplyTint`：删掉 α 覆写（那一行就地换成判据注释）；`ComposeVertexColor` 的 doc 订正「调用方不许再覆盖任何一路」（**行数零变化**） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B141_W71_Assert.cs` | 新建（312 行 / 31 条断言） |
| `Unity/资料/完全复刻/报告/B141_W129_DisabledAlpha.md` | 本文件 |

**验证**：`TMPDIR=/tmp/wf_b141_w71 bash 工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**（改完全部文件后跑，一次通过）。
