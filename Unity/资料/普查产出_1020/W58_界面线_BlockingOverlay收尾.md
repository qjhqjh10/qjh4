# W58 · 界面线：`BlockingOverlay` 队列收尾 + `MainMenuRuntime:287` 注释订正

> 执行代理 **W58-OVERLAY**。只改白名单三份：`Shell/BlockingOverlay.cs` · `Shell/MainMenuRuntime.cs` ·
> `Editor/ShellScene.cs`（仅相关断言）。⛔ 未改正本、未动 git、未跑 Unity / `*.Run` / 截图。只跑了秒级类型检查。

---

## 结论

两处尾巴都收掉了：

1. `BlockingOverlay` 判为**备用加载遮罩（降级件）** ⇒ 队列按原版 `Default`/`Blocking` 语义落回转圈档同族，
   **4070/4071 → 3338/3339**（dim / 旧圈），并公开 `QDim/QSpinner` 常量供断言读。
2. `MainMenuRuntime.cs:287` 的「加载黑场（4071）」已订正为 **`LoadingMenu.QPanel = 4090`**，并留更正痕迹。
3. `Editor/ShellScene.cs` 原先**没有** `BlockingOverlay` 的队列断言（只有行为断言）⇒ 本轮**新增两条**：
   绝对值回归闸 + 「落在 3337/3340 双槽空隙且低于 toast」的梯子断言。

---

## `BlockingOverlay` 用途与处置（判据）

**用途 = 备用（降级）加载遮罩，不是原版件。** 证据：

| 证据 | 位置 |
|---|---|
| 由 `ShellRuntime.Build` 无条件建出来，挂在壳根上 | `Shell/ShellRuntime.cs:533`（`Blocker` 属性 `:88`） |
| `IsBlocking` 的唯一运行时消费者 = 指针层的输入闸 | `Shell/PointerLayer.cs:1371`（`if (BlockingOverlay.IsBlocking) return true;`；闸门注释 `:1343`） |
| `StartSpinning/StopSpinning/AutoStopSpinner` **零运行时调用点** —— 全仓 `grep "BlockingOverlay"` 只命中本文件、`ShellRuntime.Build`（只建不调）、`PointerLayer`（只读 `IsBlocking`）、`ShellScene` 自检、注释 | `Shell/BlockingOverlay.cs` 文件头 · `Editor/ShellScene.cs:1192-1205,1245-1247` |
| 真正那两件已按判据建在 `LoadingMenu.cs`：转圈 `LoadingController`（`Default`(0) 层）+ 黑场 `LoadingMenu`（`Blocking` 层） | `Shell/LoadingMenu.cs:59-62,243-256,87-88` |
| 它是「图取不到时」的兜底：`LoadingController` 取不到 `40K_icon_searching_cog` / 降级图时报错，只剩暗底 | `Shell/LoadingMenu.cs:327-333` |

⇒ **不是「零调用点、只是历史件」**（它是壳树里真实存在、且被指针闸读的一件），所以走第一条处置路：
**按原版加载遮罩层级落档**，不删文件、不只改注释。

### 新档位 = 3338（dim）/ 3339（旧圈）

- 原版判据：加载遮罩不在 toast（`PopUps`/900）之上；toast 我们取 4072–4076 ⇒ 降级件必须 < 4072。
- 梯子空隙（现读全表逐号核对）：
  - `TrophyInfoPopup` 带 = 3310–3326 · 转圈件 = **3327–3329** · `GenericOptionsPanel` 带 = 3330–**3337**；
  - `AllianceMemberOptionsPopup` 带首 = `QShade 3340` ⇒ **3338–3339 是这一段唯一的双槽空隙**；
  - 再往上 3340→3487 是连续弹窗带（3340–3347 · 3348–3361 · 3362–3370 · 3372–3380 · 3382–3390 ·
    3392–3399 · 3400–3439 … · 3460–3487），只剩单槽空隙（3371/3381/3391），**容不下 dim+spinner 两件**。
- ⇒ 与转圈件 3327–3329 同族、不抢它的号、不落进任何窗的号段。
- ⚠️ **如实记残留**：它因此高于 `GenericOptionsPanel` 那一带弹窗。与 `W58_界面线_联盟搜索转圈落地.md`
  已记的「转圈件高于 `TrophyInfoPopup` 带」是**同一类倒置**；本件零运行时调用点、只在降级路径手动打开，
  与任何弹窗都不同屏 ⇒ 今天不可见。要严格消除只能整体重排 3310–3487 的弹窗带（不在本批）。

---

## 注释订正

| 文件:行 | 旧 | 新 |
|---|---|---|
| `Shell/BlockingOverlay.cs:22-24` | 「3060/3061 → 4070/4071 … 抬到全部 shell UI 之上、黑场之下」 | 追加 **2026-10-20 W58** 段：4070/4071 判据已过期，原版加载遮罩在 `Default`/`Blocking`、不在 toast 之上 ⇒ 降到 3338/3339；写明取空隙的理由 + 已知残留 |
| `Shell/BlockingOverlay.cs:34` | 「现读全仓没有消费者（只有自检读它）—— 挡输入没有接线」 | 「消费者 = `PointerLayer.InputBlocked`（2026-10-17 N8 接上）+ 自检；`StartSpinning` 仍零运行时调用点」 |
| `Shell/BlockingOverlay.cs:18-19` | grep 命中清单漏了 `PointerLayer` | 补上（只读 `IsBlocking`） |
| `Shell/BlockingOverlay.cs:50,57` | `SetRenderQueue(4070)` / `(4071)` | `SetRenderQueue(QDim)` / `(QSpinner)`；新增 `public const int QDim = 3338, QSpinner = 3339;` |
| `Shell/MainMenuRuntime.cs:287` | 「低于 … 弹窗/提示层与加载黑场（4071）」 | 「低于 … 弹窗/提示层（最低 3310）与加载黑场（`LoadingMenu.QPanel = 4090`）」+ 一行订正痕迹（4071 是 `BlockingOverlay` 最上面那一格，不是黑场） |

---

## 断言

`Editor/ShellScene.cs` 原状：**没有**任何 `BlockingOverlay` 的队列断言（`1192–1205` 只断 `IsBlocking` 行为，
`1245–1247` 只断 `PointerLayer.InputBlocked`）。⇒ 在 `N20/N21` 段（转圈件回归闸 `:7332-7336` 之后）**新增 `①a2` 两条**：

1. **绝对值回归闸**：`BlockingOverlay.QDim == 3338 && QSpinner == 3339`（🧨 改回 4070/4071 必红）。
2. **梯子（灭自证）**：`QDim > GenericOptionsPanel.QHit(3337)` 且 `QSpinner < AllianceMemberOptionsPopup.QShade(3340)`
   且 `QSpinner < ToastNotifications.QShade(4072)` —— 两端钉在**外部件常量**上，改回 4070/4071 时
   `4071 > 3340` ⇒ 红；想两边一起挪就得同时改那两个弹窗件（会让它们自己的断言红）。

⚠️ 按红线**未跑 `ShellScene.Run`**；同步点上建议覆盖：`ShellScene.Run`（动了 `Editor/ShellScene.cs` + `Shell/*`）。

---

## 没查清的部分 / 留给下一位

1. **`Shell/ToastNotifications.cs` 里仍写着旧值**（不在本批白名单）：
   `:77`「`BlockingOverlay 4070/4071`」· `:233`「`BlockingOverlay` 降级遮罩 `4070/4071`」·
   `:93` 的更正痕迹提到 4071。**现应为 3338/3339**，建议持有该文件的代理/主对话就地订正（铁律 5）。
2. **`BlockingOverlay` 的静态注释**（`ShellParts.cs:7,22` 等）仍称它「全屏遮罩：挡输入 + 可选转圈」——描述本身仍成立，
   只是没写档位；不构成错值。
3. **3338/3339 之后 4071 成为空号**：无任何件引用，无需处理（数值不回收，避免和旧记录混淆）。

---

## typecheck

```text
TMPDIR=/tmp/wf_overlay bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat`（口径：工作区 vs HEAD，**含本批之前未提交的改动**）：

```text
21  8   Unity/MyGame/Assets/CardPresentation/Shell/BlockingOverlay.cs      ← 本代理全部改动
 3  1   Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs      ← 本代理全部改动
299 74  Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs          ← 大头是别批未提交改动；本代理只 +16 行、0 删
```

行尾：三份都是纯 LF（改后 `CRLF 0`），无翻转。
