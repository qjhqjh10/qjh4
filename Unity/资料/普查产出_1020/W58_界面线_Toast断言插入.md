# W58-TOASTASSERT：Toast 规格/生命周期断言块插入报告

## 结论

已按 `W58_界面线_Toast规格与生命周期.md` 的「断言块」整块落进
`Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs`，**只改这一份文件**。
插入的块 = `Section("🆕 W58：toast 生命周期 / hover 色偏 / Player Level 折行 / 错误色转发")`，
覆盖 D5①/②/③、D6①/②/③（含灭自证）、D7、D9①/②。

## 插入位置与行号

- 位置：`Section("🆕 N20/N21/N23：Toast 控制器 …")` 收尾 `}`（现 `:7415`）**之后**、
  `Debug.Log(P + shell.Dump());`（现 `:7529`）**之前**；符合判据文档要求。
- 新块范围：`:7417` – `:7527`（共 111 行代码 + 3 行头注释）。
- 文件行数：8433 → 8545（+112 行）；无行尾翻转（`crlf 0 / lf 8545`）。
- 依赖口逐句核对（全部**已存在**，未新增/未改白名单外文件）：
  - `Shell/ToastNotifications.cs`：`ClearLive()` `:1226` · `DisposeForSceneChange()` `:1250` ·
    `LiveCount` `:295` · `ScheduledCount` `:296` · `FreeSpot` `:297` · `Holder` `:293` ·
    `Templates` `:294` · `LevelUpPrefabName` `:217` · `DisposeForTest()` `:1202`。
  - `Shell/ShellRuntime.cs`：`SceneCleanupHookInstalledForTest` `:411`；`Build()` 里
    `EnsureSceneCleanupHook()` `:419` ⇒ 本宿主（`ShellScene.Run`）运行时为真。
  - `Shell/WindowButton.cs`：`HighlightK` `:54` · `PressedK` `:56` · `Enter` `:485` ·
    `Exit` `:495` · `Press` `:505` · `Release` `:530`。编辑模式（`!Application.isPlaying`）
    `SetTarget` 直接落目标色，不走 0.1s 补间 ⇒ 断言可同步读回。
  - `Battle/ImageQuad.cs`：`Tint` `:318`（读 `sharedMaterial.color`）。
  - `Battle/Label.cs`：`WrappingMode` `:731`（读 TMP `m_TextWrappingMode` 原文）。
  - `Shell/MessageToast.cs`：`Ensure()` `:143` · `Show()` `:187` · `ShowError()` `:212` ·
    `Banner` `:107` · `Built` `:124` · `DisposeForTest()` `:253`。
  - `Battle/ErrorMessageBanner.cs`：`TextColorOf(int)` `:911`。
- 与本节前置上下文无冲突：本文件本会话已有的 A1350-B5 / D11 层序断言（`N21①`/`D1`/`D2`/`D3`）
  均独立于新块；`w58` 等局部变量全部包在新块 `{}` 内，无重名；`Section` 名称唯一。
- 断言语义逐条按当前实现核对过：
  - D5① `ClearLiveInternal` 清 `_live`/`_scheduled` 且 `_freeSpot = 0`、`Instance` 保留；
  - D5② `DisposeForSceneChange` → `DisposeForTest` 编辑模式走 `DestroyImmediate` ⇒ `w58Ctrl == null`；
  - D6①/② `ShowInternal` 对非 Mission 调 `hitBtn.TintOn(bgQuads)`（`ToastNotifications.cs:875`），
    `ApplyTint` 以基准色乘状态键；
  - D6③ Mission 不接 `TintOn` ⇒ 只给透明命中 quad 上色、`Background` 不变；
  - D7 `Player Level Text` 调用点已是 `wrap: true`（`ToastNotifications.cs:585`）；
  - D9 `ShowError` → `ErrorMessageBanner.ShowError` → `ErrorColor` (0.8,0,0,1)，白档走
    `MessageColor`；横幅 5 条轮转 ⇒ 第 0 条红、第 1 条白。

## grep 证据

```
7417:        // ============================================================ 🆕 2026-10-20（W58）Toast 规格与生命周期
7421:        Section("🆕 W58：toast 生命周期 / hover 色偏 / Player Level 折行 / 错误色转发");
7436:                      $"★★ W58-D5①：`ClearLive()` 清空在飞/排队/格位、**控制器还在**"
7443:                      "★★ W58-D5②：`DisposeForSceneChange()` ⇒ 控制器节点**整棵销毁**、`Instance` 归 null"
7447:                      "★ W58-D5③：`ShellRuntime.Build` 挂上了『任何 sceneLoaded ⇒ 清 toast』的兜底订阅"
7466:                          $"★★ W58-D6①：悬停后**可见底图**乘 0.9608（现读 k={k:F4}，期望 {WindowButton.HighlightK:F4}）"
7472:                          $"★ W58-D6②：按下后乘 0.7843（现读 k={kp:F4}，期望 {WindowButton.PressedK:F4}）"
7490:                          $"★★ W58-D6③（灭自证）：**Mission 悬停不变色**（现读 {mBase.g:F4}→{mQuad.Tint.g:F4}）"
7502:                  "★ W58-D7：`Player Level Text` 的 `m_TextWrappingMode` = 1 (Normal)（原版 `MB_505`）"
7516:                          $"★★ W58-D9①：`ShowError` 文字色 = (0.8,0,0,1)（现读 {red.r:F4},{red.g:F4},{red.b:F4},{red.a:F4}）"
7524:                          $"★ W58-D9②（反证）：白档仍是 (1,1,1,1)（现读 {white.r:F4},{white.g:F4},{white.b:F4}）");
```

`git diff --numstat`（该文件，含本会话此前未提交改动）：
```
244	73	Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs
```
（本次纯插入 +112 行；行尾未翻，`crlf 0 / lf 8545`。）

## 缺的口（若有）

**无。** 断言块依赖的公开口全部已在生产文件里存在，未新增/未修改白名单外文件。

## typecheck 结果

`TMPDIR=/tmp/wf_toasta bash d:/4/Unity/工具/typecheck.sh`：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

未跑任何 `*.Run` / 未起 player / 未截图（铁律 12）。
