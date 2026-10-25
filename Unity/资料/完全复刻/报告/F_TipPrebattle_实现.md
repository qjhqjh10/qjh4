# F · `TipPrebattle`（战前提示条）实现

> 产出：**新文件** `Unity/MyGame/Assets/CardPresentation/Battle/TipPrebattle.cs`（**392 行**，含判据注释；CRLF 与 `Battle/*.cs` 一致；`.meta` 已建）。
> ⛔ 没改 `Battle/**` 里任何既有文件（E 占着）；接线点见 §3，交给 E。
> ⛔ 没改账、没动 git、没跑 Unity（用户 2026-10-25 暂停自检）。
> ✅ **秒级类型检查已过**：`bash Unity/工具/typecheck.sh` → **运行时 0 错 · 编辑器 0 错**。

---

## ① 数字 / 做了什么

| 项 | 内容 |
|---|---|
| 新建文件 | `CardPresentation/Battle/TipPrebattle.cs`（命名空间 `CardPresentation`，与工程一致） |
| 实现的方法 | 原版 **10 个**全实现：`SetupClosed` · `DisplayTip` · `DisplayTips` · `GetNextTip` · `OnTouchUpAsButton` · `HideTips` · `OpenTip` · `CloseTip` · `RenewTip` · `DeactivateTip` |
| 原版字段 | `openBgWidth = 1.8f` · `timeToOpenBg = 0.3f`（**ILSpy 的字段初始化器**，:408/:410） |
| 断言 | **2 条**（建出来了 / 按判据的时机显示·隐藏），标「待自检恢复时跑」 |
| 类型检查 | **0 错**（运行时 + 编辑器两个程序集） |

**对应的账**：`战斗界面_HUD.tsv` 的 `TipPrebattle` 那 **10 行 C**（E 已把等级复核为 **P0**，备注写着「同一件（战前提示）」）。
本文件一次覆盖那 10 行。

---

## ② 判据（每一处都能回到原版）

| 本类的东西 | 判据 | 出处 |
|---|---|---|
| 方法体全部行为 | 原版 ILSpy 反编译（**方法体完整**） | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TipPrebattle.cs`，逐方法行号写在文件头 |
| `openBgWidth = 1.8f` / `timeToOpenBg = 0.3f` | **字段初始化器直读** | 同上 `:408` / `:410`。⚠️ `AssetRipper` 那份桩把它丢了（`public float openBgWidth;` 无初值）⇒ **只能取 ILSpy** |
| 展开/淡入时序：`scaleX 0→1.8`（0.3s 线性）→ 等 0.15s → 换字 + 淡入 0.3s → `animFlag=false` | `OpenTip` 状态机 | 同上 `_003COpenTip_003Ed__17.MoveNext`（:541 起） |
| 收起：淡出 0.15s + `scaleX→0`（0.15s）→ 等 0.15s → `showingTip=false` | `CloseTip` | 同上 `_003CCloseTip_003Ed__18`（:552 起） |
| 换条：`CloseTip` 走完 → 若还 active 再 `OpenTip` | `RenewTip` | 同上（:563 起） |
| 隐藏：停 `OpenTip` → 杀两条补间 → `CloseTip` **且** `DeactivateTip` | `HideTips` | 同上（:511 起） |
| 最多试 **6 次**抽文案（避免与上一条重复） | `object obj = 5;` + 循环 `obj--` | 同上 `GetNextTip`（:467 起） |
| 点条 = 换下一条 | `OnTouchUpAsButton` | 同上（:502） |

### 2.1 🔴 两处**反编译没解出来**的地方（文件头也标了，⛔ 别当已定案）
- **`SetupClosed` 的 `localScale`**：ILSpy `:430-434` 是 `transform.localScale = (Vector3)(&obj)`（by-ref 未解）。
  结合 `CloseTip` 里的 `DOScaleX(…, 0f, …)`，本类取「**只把 x 压到 0、y/z 保持**」—— **这是推断**。
- **`DeactivateTip` 的等待时长**：ILSpy `:583` 前有一条
  `NoteDecompilerIssue("Unmanaged memory load: [v28 @ rcx_v2 (UnityEngine.Component)+34]")`
  ⇒ 真实 `WaitForSeconds(...)` 实参**没解出来**（反编译里是占位 `0f`）。
  本类取 `timeToOpenBg * 0.5f` 并**暴露成字段 `deactivateDelay`**，有人从实机/prefab 核到真值只改这一个字段。

---

## ③ 【转 E】接线说明 —— **可直接照此接线**（含两条硬缺口；⛔ 我没碰 `Battle/**`）

> 本节自包含：几何参数、调用时机、文案源三件事都在这儿，E 看完不用再翻别的节。

### 3.1 建（一次性）
```csharp
// 战场 HUD 建完之后（`BattleHud` / `BattleDriver` 的建场收尾处）：
var tip = CardPresentation.TipPrebattle.Create(
    hudRoot,                      // 宿主 Transform（战场 HUD 的锚点）
    barHeightPx:  <待定>,          // ⚠️ 原版真值缺（见 §4 ①），建议先给 40
    anchoredPosPx: <待定>,        // ⚠️ 同上，建议先给 (0, -260)
    anchor: new Vector2(0.5f, 0.5f),
    fontPx: 22f);
```
`Create` 内部已经调了 `SetupClosed()`（收起态：底板 x=0、文字 α0），**不需要再调一次**。

### 3.2 显示 / 隐藏
```csharp
tip.DisplayTips(pool);   // pool: IReadOnlyList<string> —— 文案池，见 §4 ②
tip.HideTips();          // 该收的时候
// 玩家点提示条：
tip.OnTouchUpAsButton();
```

### 3.3 🔴 **调用时机判不了**（如实说）
全 26k 个反编译方法里，`TipPrebattle` 这个类型名**只出现在它自己的文件里** ⇒
原版是靠 **prefab 上的引用 / UnityEvent** 驱动的，**本地没有任何一处 C# 调用点**。
所以「什么时候该 `DisplayTips`」= **原版判据缺**，只能由 E 按界面语义定（例如「进战斗场景、倒计时开始前」）。
⛔ 别把本类接到一个我猜的时机上然后当成「原版如此」。

---


### 3.4 🔴 硬缺口①：**几何参数本地拿不到**（位置 / 尺寸 / 贴图）
三种搜法全空，所以**必须由宿主任传**：
- 全量 `assets_full` grep **`openBgWidth` / `timeToOpenBg` → 0 命中**；
- 按 `m_Script` 找：`TipPrebattle` 只出现在 `globalgamemanagers/MonoScript/MonoScript_1941.json`，
  **没有任何 MonoBehaviour 指向它**（按 `m_FileID≠0` + pathID=1941 扫过，0 条）；
- `AssetRipper` 导出工程里只有 `Assets/Scripts/Assembly-CSharp/TipPrebattle.cs`（脚本源码），**没有 prefab**。
⇒ `Create(parent, barHeightPx, anchoredPosPx, anchor, fontPx)` 的五参里，后四个**都要接线方给**
（报告里给的 40 / (0,-260) / 22 只是**占位建议**，⛔ 不是原版值）。

### 3.5 🔴 硬缺口②：**文案源缺**（`PrebattleTips/*` 本地 0 条）
原版：`playerData.GetRandomTip()` → 词条 **`PrebattleTips/<key>`**（`TipPrebattle.cs:474-490`）。
本地：`官方本地化_en+zh-CN.json` 与 `13lang` **各 0 条**；只有 **`Battle/Tips/*`（20 条）** ——
**那是另一族键，⛔ 不能拿来顶替**（顶了就是「我们自己编的提示」）。
⇒ `DisplayTips(pool)` 的 `pool` 由宿主任传；池空/查不到时组件**出声**、不显示假文案。

## ④ 没查清（明细见 §3.4 / §3.5）

两条硬缺口（几何参数缺判据 · 文案源缺词条）已经写进 **§3.4 / §3.5**，本节不再重复。

## ⑤ 两条断言（按调度台要求；**待自检恢复时跑**）

| # | 断言 | 怎么接 |
|---|---|---|
| 1 | **建出来了** —— 底板 + 文字都在，且处在收起态（`scaleX≈0`、文字 α≈0） | `tip.SelfCheckBuilt()`（同步，返回 bool） |
| 2 | **按判据的时机显示 / 隐藏** | `StartCoroutine(tip.SelfCheckShowTiming())`：调 `DisplayTips` 后 **0.75×T 时必须还在播**（挡「一帧到位」）、**1.5×T 后必须完全展开**（x=1.8、α=1、`animFlag=false`）；`StartCoroutine(tip.SelfCheckHideTiming())`：`HideTips()` 后 **0.5×T 内回收起态**、`deactivateDelay` 后 `activeSelf==false` |

⚠️ 两条都用 `Debug.LogError` 报失败（不是静默 return false）——与本工程「不许静默失败」一致。

---

## ⑥ 顺手发现

1. **`TipPrebattle` 的 10 行账已经由 E 复核成 P0**（备注：「同一件（战前提示）⇒ 改 P0」）——
   也就是说我上一轮 P0 候选清单里「`战斗场景加载与开场` 10 条 C 全来自 TipPrebattle」那件事，E 已经处理了等级。
2. **`PrebattleTips/*` 这条词条族在客户端**不存在 —— 与之前那次「官方中文表不在客户端安装目录、而在远端
   Addressables 包」是同一类现象。**断言「本地没有」之前要先想到这一层**（铁律 2 的教训）。
3. 本类的 `Create` 用了 `ImageQuad`（`Battle/ImageQuad.cs`）+ `Label`（`Battle/Label.cs`）两件——
   都是**既有共用件**，没有新建任何渲染路径。⚠️ 两个都是世界档 HUD 的口径（设计 px ÷ 108），
   若 E 把它挂到 uGUI 画布下，要用 `Label.CreateUgui` 那条（尺寸口径不同，见 `Label.cs:171` 的注释）。
