# W58 · 界面线：`ToastNotifications.cs` 里 `BlockingOverlay` 旧队列号订正

> 执行代理 **W58-TOASTCOMMENT**。只改白名单一份：`Shell/ToastNotifications.cs`。
> ⛔ 未改正本、未动 git、未跑 Unity / `*.Run` / 截图。只跑了秒级类型检查。

---

## 结论

`Shell/ToastNotifications.cs` 里三处对 `BlockingOverlay` 旧队列号 **4070/4071** 的引用已就地订正为
**3338/3339**（`QDim=3338` / `QSpinner=3339`），判据 → `资料/普查产出_1020/W58_界面线_BlockingOverlay收尾.md`。
**只动了注释**：无任何常量 / 代码逻辑 / 字符串输出改动；`QShade..QHit` 仍是 4072–4076，未动。
订正痕迹按原有「旧值 → 新值 + 日期 + 判据」句式追加，历史引文保留未抹。

---

## 逐处（旧 → 新）

| 位置（改后行号） | 旧 | 新 |
|---|---|---|
| `:77-79`（文件头 ④，现网清单） | `BlockingOverlay 4070/4071`（降级件，无运行时调用点） | `BlockingOverlay 3338/3339`（降级件，无运行时调用点；🔴 2026-10-20（W58）：由 `4070/4071` 降下来，判据 → `W58_界面线_BlockingOverlay收尾.md`） |
| `:95-100`（文件头 ④ 的 W58·D4 订正段） | 引文「在 BlockingOverlay 4071 与加载黑场 4080 之间」后面直接接「⛔ 别再按『5000 起』去推」 | **引文原样保留**（历史原话），其后追加一段 **W58·TOASTCOMMENT** 订正痕迹：`BlockingOverlay 4071` 这个数也已过期，已由 `4070/4071` 降到 `3338/3339`，判据同上，「别据此再推转圈件的档」 |
| `:238-239`（渲染队列常量段） | `BlockingOverlay` 降级遮罩 `4070/4071`（降级件，无运行时调用点） | `BlockingOverlay` 降级遮罩 `3338/3339`（降级件，无运行时调用点；🔴 2026-10-20（W58）：由 `4070/4071` 降下来，判据 → `W58_界面线_BlockingOverlay收尾.md`） |

⚠️ 行号会随其他批次的注释改动漂移，按内容（`BlockingOverlay` + `4070/4071`）找。

---

## typecheck

```text
TMPDIR=/tmp/wf_tc2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat`（口径：工作区 vs HEAD）：

```text
111  11  Unity/MyGame/Assets/CardPresentation/Shell/ToastNotifications.cs
```

⚠️ 该文件在本会话开始前**已是 `M` 状态**（含 D4/D5/D6/D7 等未提交批次），所以 111/11 不是本代理的改动量。
**本代理的改动 = 3 处注释、相对改前约 +6/−3 行，全部在 `//` 注释里**，无代码行变动
（diff 中 `QShade..QHit`、`wrap:`、`TintOn`、`DisposeForSceneChange` 等均为**本批之前**的改动）。

行尾：改后 **LF 1431 / CRLF 0**（纯 LF，无翻转；全程用 Edit，未用 `sed -i`）。

---

## 未做 / 留给同步点

1. ⛔ 按红线**未跑 `ShellScene.Run`**。本代理只动注释，理论零影响；同步点若按覆盖面收口，
   可随「动了 `Shell/*`」那一批一起跑 `ShellScene.Run`。
2. `资料/普查产出_1020/W58_界面线_BlockingOverlay收尾.md` 的「没查清的部分」第 1 条
   （本文件仍写着旧值）**现已收掉** —— 是否需要在该报告里追加一句关联说明，由主对话决定（本代理不改他人报告）。
