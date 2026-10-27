# B9 / C2T7c —— `菜单_外壳.tsv` B 行逐条核（全部 119 条）

> 查判据型子代理（只读 + 落账补丁）。范围 = `账/菜单_外壳.tsv` 里 **状态=B 的全部行（119 条，一次核完）**。
> 产出：`判据/B行逐条核_菜单外壳.tsv`（8 列 · 119 行）· `账/_parts/B9_C2T7c.tsv`（11 列 · 117 行，状态保持原值）· 本报告。

---

## 一、结论速览

- **119 条：建议 A（等价）= 17 · 建议 B（保持）= 102。**
- 102 条 B 细分为：**无落点(—) 16** · **落点非方法级（类级/散写/裸文件/描述）80** · **方法级但真偏离/未逐句核 6**。
- **无「落点失效（文件不存在）」**；但多条落点**行号漂移**（合并请 `--fix` 按符号名重定位）。
- 🔴 **2 条（`MenuActions.QueueAction` / `QueueActionWithCallback`）主表 `方法` 列本身就是坏的**（见 §四），**无法进补丁**（校验器拒），已从 patch 剔除、仍留在判据 TSV（= 119 完整），需你修主表。

### 建议升 A 的 17 条（双侧方法级等价，供你裁定）

| # | 方法 | 落点（真身） | 等价依据 |
|---|---|---|---|
| 5 | `ChooseNamePopup.Setup(Action<string>,Action)` | ProfileTab.CommitNameWindow(:1491)/CancelNameWindow(:1499) | 改名窗完成/关闭回调 = 这两个口 |
| 43 | `MainMenuWindow.ClickChooseName(string)` | ProfileTab.CommitNameWindow/ApplyName(:1491) | 全工程唯一写名点 |
| 48 | `MenuActions.CanTrigger(Action)` | LiveOpsHandler.CanTriggerNow(:1312) | 组去重/currentWindow/MenuFilter/onboarding 合并 |
| 53 | `MenuActions.Routine()` | LiveOpsHandler.TryDrain(:1284) | Priority 降序稳定 drain |
| 55 | `MenuActions.TryTriggerAction(Action,GameWindow,Action<bool>)` | LiveOpsHandler.Trigger(:1328)+Complete | 摘除+ActionRule 完成回调 |
| 75 | `ScreenUtilities.GetDiagonalSizeReferenceValue()` | Core/ScreenUtilities.cs:60 | clamp01((phys−4.7)/0.9000001) 逐字 |
| 76 | `ScreenUtilities.GetScreenDPI()` | Core/ScreenUtilities.cs:44 | max(100,Screen.dpi) |
| 77 | `ScreenUtilities.GetScreenPhysicalSize()` | Core/ScreenUtilities.cs:51 | sqrt(w²+h²)/DPI |
| 95 | `ToastNotification.ForceUpdateLayout()` | ToastNotifications ForceRelayout/Label.ForceRelayout | 机制不同意图等价（铁律18 我们接受） |
| 96 | `ToastNotificationController.ShowNotificationInternal(ToastNotificationContext)` | ToastNotifications.ShowInternal(:842) | ①②④同形、③换 Label.ForceRelayout 量高 |
| 97 | `ToggleDisplay.Setup(bool,string,Sprite)` | WindowButton SetSelected+SetNormalTex(:465)+Label | 拆三跳，语义同 |
| 100 | `UIErrorMessageItem.LayoutRefresh()` | ErrorMessageBanner.RelayoutActive(:806) | 错误条重排 |
| 101 | `UIErrorMessageItem.Show(string,Color,bool)` | ErrorMessageBanner.ShowError(:642)/ShowMessage(:648) | 显示错误/消息 |
| 114 | `WindowTabBase.Awake()` | （原版空实现；我们未写 Awake） | 空对空 |
| 117 | `WindowsManager.CloseWindow<T>()` | WindowsManager.cs:374 | GetOpenWindow<T>() 非空才 Close |
| 118 | `WindowsManager.GetOpenWindow<T>()` | WindowsManager.cs:360 | openWindows 首个 is T |
| 119 | `WindowsManager.RefreshWindow<T>()` | WindowsManager.cs:381 | 只认 currentWindow 是 T 才 TryOpen |

### 方法级但保持 B 的 6 条（真偏离/未接线/判据缺）

- **14 `EverguildInput.GetKeyDown(...)`** —— 落点 `PointerLayer.cs:630(HitLines)` **与 GetKeyDown（键盘）不对应**（落点可疑，需重找）→ B。
- **22 `EverguildTooltipTrigger.OnDeselect(...)` / 23 `OnSelect(...)`** —— **能力已落、接线数 0**：`tipText` 无站点赋值 ⇒ 今天零行为变化（SetSelected→HideTip/ShowTip 已实现但没接 tooltip 数据）→ B（要做：给站点赋 tipText）。
- **44 `MainMenuWindow.Initialize()`** —— 落点 `MainMenuRuntime.Build(:401)`，差异「一致」但未逐句核 → 保守 B。
- **68 `ResourcesBarController.OnCurrencyChange(...)`** —— 真缺口：数值滚动已对齐（0.5s），但**缺 `+N` 飘字**（原版 amountChanged 用于弹变化量提示）→ B。
- **82 `Shade.finish()`** —— 判据缺：原文 `Shade__finish.c` 是 `throw NotImplementedException()`（体缺失），语义只按 CIL 推断未核 → B。

---

## 二、80 条「落点非方法级」说明（最大的 B 桶）

这 80 条的 `落点` 是**类级 / 散写 / 裸文件名 / 描述性括注**，不是方法级，按方案 §4.1「类级命中 ⇒ 只能 B」一律保持 B。典型形如：
- `Shell/ProfileTab.cs（恒走首次免费那条）`（`ToggleChangeFree`，且「恒免费」本身是偏离）
- `Shell/SettingsWindow.cs 图像页 FPS 滑块`（`FPSLimitValueChanged`）
- `各窗按固定列手算（卡池/收藏）`（`FlexibleGridLayout.DoesItemFit`）
- `Shell/WindowsManager.TopWindow + T…` / `各窗标题 + ShowPreviousWindow 回调`（`MenuActions.CheckActiveMenus` / `WindowHeaderWithBackButton.Initialize`）
- `Shell/ShellRuntime 的单一淡入淡出`（`SceneTransitionList.GetTransition`）
- `Core/LayoutSpace / MenuDraw PxRect`（`UICulling.GetWorldRect`）

⇒ 要升 A，需先把这些**定位到具体方法并逐句核**（属下一轮「补精确落点」的活，不是本轮）。

其中若干**差异原文就写着「等价/一致」但落点是文件级**（如 G 审计 2026-10-25 把它们「退 B、待重新找本方法实现」的那批：`ChooseNamePopup.DelaySizeFix/ResetCanClick`、`ExpansionPassPointDrawer.DrawForArmy`、`MainMenuWindow.OpenChooseNameMenu`、`TitleDrawer/TitleIconDrawer` 等）—— 它们的**行为可能已等价，只差一个方法级落点**；本轮我确认了 `Setup`(#5)/`ClickChooseName`(#43) 两个能定位的（升 A），其余暂留 B。

---

## 三、方法学 & 证据

1. **落点机械核**：119 条逐条解析 `File.cs[:line]（Method）`，`os.path.exists` 全 OK（无文件不存在）；`name_ok`（=（）里是真方法名且在文件内）仅 17 条 → 这 17 条里另核等价，其余 80 条落点是类级/描述（`NAMEABSENT`/`NOMETHOD`/`NOPARSE`）。
2. **读过的落点实体**：`LiveOpsHandler.TryDrain/CanTriggerNow/Trigger`（:1284/:1312/:1328）· `WindowsManager.GetOpenWindow/CloseWindow/RefreshWindow`（:360/:374/:381）· `ScreenUtilities` 三式（:44/:51/:60）· `ProfileTab.CommitNameWindow/CancelNameWindow`（:1491/:1499）· `ErrorMessageBanner.ShowError/ShowMessage/RelayoutActive/HideAll`（:642/:648/:806/:1027）· `WindowButton.SetSelected/SetNormalTex`（:141/:465）· `WindowTabBase.OnSetup/Toggle`（:52/:76）· `ToastNotifications` 高度重排段。
3. **A 的判据**：只对「落点方法体读过、且与要点行为等价 / 结构性等价（空方法=空方法、机制不同意图等价）」的升 A；其余保守留 B。

---

## 四、顺手发现

1. 🔴 **主表 `菜单_外壳.tsv` 有 2 条 `方法` 列本身是坏的（需你修主表）**：
   - L459 `QueueAction(System.Action action, [Optional][DefaultParameterValue(0)` —— **缺 `)]` 尾巴**（方法签名被截断）；
   - L460 `QueueActionWithCallback(Action<System.Action> action, [Optional][DefaultParameterValue(0)` —— 同上。
   这两条的 `方法` 被截断后 `merge_ledger_parts.py` 的 `looks_like_one_method` 判「一格塞多个方法」（`[DefaultParameterValue(0` 无闭合 `]` ⇒ 不被剥离 ⇒ 多个 `(`）⇒ **无法进补丁**。我已从 `B9_C2T7c.tsv` 剔除这 2 条（仍留在判据 TSV，= 119 完整）。**修法**：把主表这两行 `方法` 补全成完整签名（原版真身如 `QueueAction(System.Action action, [Optional][DefaultParameterValue(0)] ... )`），补全后我可以再补这两行进 patch。
2. **行号漂移普遍**：本批多条 `落点` 的 `:行号` 已落后文件真身，合并前务必 `--fix`。
3. **落点非方法级是本域通病**（80/119）：`菜单_外壳.tsv` 相当多 B 行的 `落点` 停留在「文件 + 描述」层，正是 §4.1「类级命中 ⇒ 只能 B」的典型。建议单列一轮「补精确方法级落点」，可一次性把其中不少「行为已等价」的行升 A（本轮 `Setup`/`ClickChooseName` 就是这种）。

---

## 五、产物（git status 仅这 3 个新文件，未动 .cs/主表/正本/git，未跑 Unity）

- `判据/B行逐条核_菜单外壳.tsv`：8 列 × 119 行（`域 方法 原等级 建议等级 判据出处 我们的落点(文件:行) 一句结论 还差什么`）。建议等级 A=17 / B=102。
- `账/_parts/B9_C2T7c.tsv`：11 列 × **117 行**（表头逐字抄主表；状态列全保持原值；只改 差异（A 行改写「等价（C2T7c 核）：…」/ B 行原差异后追加 `｜[C2T7c 2026-10-27] …`）/ 轮次=`B9_C2T7c` / 备注前缀 `[C2T7c 2026-10-27]`，原备注全文保留）。**剔除的 2 行 = L459/L460 坏 `方法`**（见 §四·1）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7c` → **更新 117 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
