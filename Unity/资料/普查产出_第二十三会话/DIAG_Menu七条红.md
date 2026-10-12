# D-M1 · MainMenuScene 6 条红（只读诊断，⛔ 未改任何代码）

判据日志 = `d:/4/_tmp_view/s24_MainMenuScene.log`（2026-10-12 23:07）。栈帧已逐条取出。

## 一、一句话结论

| # | 红项 | 类 | 是不是本会话引进 |
|---|---|---|---|
| 1 | `MainMenuRuntime.Instance == menu` | **(δ) 夹具前提不成立**（自检这台编辑期 `Instance` 恒为 null） | **不是**（改动前就红） |
| 2–5 | `SetNavToggle(Collection/Shop/Rewards/Social)` | **同上一条的根，同属 (δ)**；断言本身没写错 | **不是** |
| 6 | 关窗后主菜单回不来 | **(β) 实现真错**（`WindowsManager.Close()` 收尾顺序） | **不是**（本会话没碰 WindowsManager / MainMenuWindow / MainMenuRuntime） |

🔴 **6 条全部不是本会话改动引进的**：本会话对 `Editor/MainMenuScene.cs` 的改动只有 **`@@ -6005,41` / `@@ -6070,10`** 两块（`git diff -U0` 现读 = 34/9 行，全在 `A1697` 那一节），而 6 条红的行是 **753 / 766 / 10431**，逐行与 `git show HEAD:` 相同；6 条的**实现判据文件**（`Shell/{MainMenuRuntime,WindowsManager,MainMenuWindow}.cs`）本会话 `git status` 显示**未修改**。
🔴 夹具（`DeckLibrary.Insert` + `CollectionData.ResetForTest`）**不可能**是元凶，四条独立证据见 §二·末。

## 二、第 1 条判到底：`Instance` 是 **null**，不是「另一台」

**栈帧**：`MainMenuScene.cs:753`（`CheckTrue` 于 `:40`）。

**它不是别的树，是 null —— 由日志反证**：`MainMenuScene.cs:775` 调了 `SetNavToggle(Event)`；若 `rt != null`，`LightNavToggle` **必**打一行 `[Menu] SetNavToggle(Event)`（`Shell/MainMenuRuntime.cs:654-664`，无任何前置分支）⇒ 全日志 grep `本仓左竖导航只有` = **0 命中** ⇒ `SetNavToggle` 在 `Shell/MainMenuRuntime.cs:649` 的 `if (rt == null) return;` 就退了。

**根因**：`Instance` 全仓唯一赋值点 = `Awake`（`Shell/MainMenuRuntime.cs:55`；另一处 `:63` 是清空）。`MainMenuRuntime` **没有 `[ExecuteAlways]`**（类声明 `:48`），而自检跑在编辑模式 ⇒ `Awake` 不跑。本仓两处早已写明：`Shell/MainMenuRuntime.cs:1045`「编辑模式下 `Awake` 不跑（只对 `[ExecuteAlways]` 的脚本跑，本类不是）⇒ 自检里 `Instance` 可能是 null」· `Shell/PointerLayer.cs:47` 同款。
⇒ 这条**前提断言干得对**（它就是要报这件事），红是必然：这个量在编辑期恒为 null，加断言那天起就不可能绿。
**同批证据**：`MainMenuRuntime.cs:1048` 的 `InstanceOrFind()` 就是为这件事加的，但 `SetNavToggle`（`:646`）**没走它**，直读 `Instance` —— 与同文件 `EscapePressed`（`:1041` 用 `InstanceOrFind`）写法不一致。

**是不是本会话引进的 = 不是**。证明：① 该值 6 条红全程只依赖 `MainMenuRuntime.cs`（未改）+ `MainMenuScene.cs:753/766`（与 HEAD 逐字相同）；② 本会话改的 `A1697` 夹具在 `:6014-6100`，**排在 753/766 之后**，因果上不可能；③ `git log -S 'MainMenuRuntime.Instance == menu'` = 唯一提交 `ac5a952`（第二十一会话，今天 14:55），而全天 `_tmp_view` 里**含该断言串的日志只有 `s24_MainMenuScene.log` 一份**（grep -rls 全目录）⇒ 它是**第一次真跑**，无「曾经绿过」的记录。

**夹具为什么不是元凶（四条）**：① `CollectionData.ResetForTest()` = 纯静态数据，无窗/无场景操作（`Shell/CollectionData.cs:444-457`）· ② `DeckLibrary.Load()/Insert` = 纯数据 · ③ `sk.CreateDeckInMode()` 在批处理里 `LoadScene` 之前就 return（`Shell/LiveOpsEventWindow.cs:1150`），日志有 `（批处理：不切场景，只交接）` · ④ 顺序：夹具 `:6014` **晚于** 753/766。

## 三、第 6 条判到底：`Close()` 的收尾顺序把它自己刚开回来的主菜单摁灰

**栈帧（`s24_MainMenuScene.log:45574` 那条 warning，位置 = 断言 `:10433` 前 17 行）**：
`GameWindow.OpenByState (:545)` ← `TryOpen(object) (:488)` ← `WindowsManager.OpenWindow (:995)` ← **`ShowPreviousWindow (:1221)`** ← `NotifyClosed (:1170)` ← `GameWindow.Close (:657)` ← `MainMenuScene.Run (:10425)`（= `:10427` 的 `firstSW.Close()`）。

**根因（β，逐行）**：`Shell/WindowsManager.cs:655-658`
```
CurrentState = WindowState.Closed;                    // 655
Data = null;                                          // 656
if (Manager != null) Manager.NotifyClosed(this);      // 657 ← 这一跳里 ShowPreviousWindow→OpenWindow(BaseMenu)→TryOpen 把 baseMenu 重新 SetActive(true)+state=Open
gameObject.SetActive(false);                          // 658 ← 紧接着又把它摁灰 ⇒ (state=Open, activeSelf=false)
```
⇒ 造出的正是 `:543-546` 那条守卫**自己点名**的非法组合（「只可能来自绕过 Close()/Hide() 直接改激活态」）—— 但这次**不是有人绕过，是本方法自己造的**。
**加重的一环**：`CloseAllWindows()`（`:1271-1278`）在逐扇 `Close()` 之后无条件 `openWindows.Clear()`（`:1275`）⇒ 被 early-return 过的窗（:`653`，不活就退）**一个态都没写就被摘了表** = **孤儿**。于是 `firstSW.Close()` 时 `ShowPreviousWindow` 看到 `Count == 0` 走 `:1220` 那一支 `OpenWindow(BaseMenu)`，`TryOpen` 撞上 `(Open, inactive)` ⇒ 照原版那一支**一个字段都不写**（`:543-546`）⇒ baseMenu 停在 inactive ⇒ `:10433` 红。
**方向与判据一致**：本仓自己写着原版那两步的次序 —— `Shell/WindowsManager.cs:614`「原版 `Close` → `manager.CloseWindow` → `CloseWindowCO` → **先调 Slot 9 `Hide()`** 再摘表/销毁」，而 `Hide()`（`:623-627`）是「state=Closed + `SetActive(false)`」；**原版把「物体置灰」排在摘表/`ShowPreviousWindow` 之前**，我们排在**之后**。
**真实影响（⚠️ 建议优先，不只是红）**：导航钮 SOCIAL/REWARDS 的 `closeAll: true`（`MainMenuRuntime.OpenSocial/OpenRewards`）每次都会跑 `CloseAllWindows()`，而 baseMenu 必在表里、且是**最旧那一扇**（下标 0、被循环最后关到）⇒ 关它那一刻列表正好清空 ⇒ **那一拍几乎必然造出脏态**。后果 = 关掉上面那扇窗之后**主菜单回不来**（比「一直可见」更糟）—— 与用户 2026-10-12 抱怨的「界面相互重叠」是同一族。⚠️ 我**没进游戏**，这条是代码 + 日志推的，未真机复现。

**是不是本会话引进的 = 不是**：`WindowsManager.cs` / `MainMenuWindow.cs` / `MainMenuRuntime.cs` 本会话 `git status` 均**未修改**，`:10427-10436` 也与 HEAD 逐字相同；`A1697` 夹具不碰窗口栈（§二末四条）。

## 四、2–5 条是否纯级联：**是**（与 1 同根，但不是 1 的「语义级联」）

因果链：`Instance == null` ⇒ `SetNavToggle`（`:648-649`）第一句就 return ⇒ `LightNavToggle → SelectNav`（`:654/:687`）一次都没跑 ⇒ 高亮**永远停在建树时那颗 `Home`**（`Editor/MainMenuScene.cs:689-690` 建的就是它）。
⇒ 每轮 `litI` 恒 = 1（全树只有 1 颗 `Selected highlight`），而 `wantHl`（Collection/Shop/Rewards/Social 底下那颗）**不存在** ⇒ `:766` 的 `wantHl != null` 为假 ⇒ 红，且文案「实测亮 1 颗」与日志逐字吻合。`Main`（i=0）与 `Event`（`:777`）两条**是假绿**：前者 `wantHl` = Home 那颗、后者只断 `hlHome.activeSelf`。
§ 所以 2–5 **不是**「1 红了所以它们跟着假」，而是**同一根因的四处显形**；把 1 改绿（例如只改 753 的期望值）**并不会**让 2–5 变绿。

## 五、每条该改什么（⛔ 我不改，只给落点）

- **1（改断言，落 `Editor/MainMenuScene.cs:753`）**：这条前提钉的「`Instance` 就是本台」在编辑宿主里**恒不成立**。要么改成断**事实**（编辑期 `Instance` 为 null / `Awake` 不跑），要么断「解析得到的实例是本台」。
- **2–5（优先改实现，落 `Shell/MainMenuRuntime.cs:648`）**：把 `var rt = Instance;` 改成 `InstanceOrFind()` —— 与同文件 `EscapePressed`（`:1041-1047`）既定写法一致；无主菜单的场景里 `FindFirstObjectByType` 返回 null ⇒ 仍「无事可做」，零可观测差异（该口 doc `:640-645` 的语义不变）。**备选**（只改断言、不动实现）：`:756-771` 那一圈改调**实例方法** `menu.SelectNav(i)`（`MainMenuRuntime.cs:687` 是 public），代价 = 测的不再是「共用口」，与 `:746-750` 的立意相抵。
  ⚠️ 无论走哪条，**753 都得改**（`Instance` 字段仍是 null）。
- **6（改实现，落 `Shell/WindowsManager.cs:657-658`）**：把 `gameObject.SetActive(false);` **挪到 `NotifyClosed` 之前**（照 `:614` 记的原版次序：先 `Hide()` 再摘表）；`:1275` 的 `openWindows.Clear()` 建议只清「真的已 `Closed`」的那些（否则继续造孤儿）。⛔ 这是**共用件**（8 个自检宿主都吃它 + `Editor/ShellScene.cs` 的 A217 三档断言钉着 `Close()` 的守卫与文案）⇒ 改前先 `grep` 受影响宿主，改后按覆盖面跑。

## 六、没查清的部分 + 还差什么

1. **脏态最早产生的那一拍没定位到**：`(Open, inactive)` 在本次日志里最早表现为 `:45342` 那条 warning，但**产生它的那次 `CloseAllWindows()` 无日志**（`Close()`/`Hide()` 的正常路径全静默）⇒ §三的链条是**用栈帧 + 源码推的**，不是读出来的。要钉死需给 `Close()`/`CloseAllWindows()` 加临时日志（或断点）再跑一次 —— **我按红线没跑 Unity**。
2. **真机未验**：§三末那条「关窗后主菜单回不来」的用户可见性，我没进游戏复现（只按代码推）。
3. `MainMenuScene.Run` 在 `ac5a952`（今天 14:55）与本次之间**有没有别的跑过**：我按 `_tmp_view` 全目录 grep 断言串（仅 s24 命中）+ `git log -S` 判的；若有被删/别处的日志，我这条「第一次真跑」的判据会弱，但 §二的 null 结论**不依赖它**。

## 七、顺手发现（只报不改）

1. 🔴 **`CloseAllWindows()` 有两处独立的记账洞**（`Shell/WindowsManager.cs:1271-1278`）：① 下降循环里 `Close()` 会经 `NotifyClosed`→`openWindows.Remove` **改列表**；② 逐扇 `Close()` 对「已经不活」的窗**首句就 return**（`:645-654`）⇒ 被 `HideAllWindows()` 藏过的窗全走这一支，最后一个都不摘，紧接着 `:1275` 无条件 `Clear()` ⇒ **凡是「藏起来的窗 + closeAll」这一组合，都会留下 `state != Closed` 的孤儿**。建议单独立账。
2. 🟡 **`Editor/MainMenuScene.cs:777-780` 是弱断言**：文案要求「日志里必须有一行 `[Menu] SetNavToggle(Event)…` 说明原因」，但条件只断 `hlHome.activeSelf` ⇒ **今天它什么都没做的（真静默）仍然绿**。与 `资料/已知的坑.md` 里「弱断言分不出两种状态」同族。
3. 🟡 2–5 里的 `litI == 1` **结构性恒真**（全树只有 1 颗 `Selected highlight`）⇒ 真正的判别力只在 `wantHl != null && wantHl.activeSelf`；文案「恰好 X 那一颗亮（实测亮 1 颗）」并不由 `litI` 支撑。
4. ✅ **夹具本身没问题**：`CollectionData.ResetForTest()`（`Shell/CollectionData.cs:444-457`）只清 6 个静态字段、不碰窗口/场景；`DeckLibrary.Insert` 是数据侧。`A1697` 那一节的读数（`s24:28741-28794`）= 4 ✓ + **1 ✗ = `:28781`「没有顺手改当前选中」期望 [3] 实得 [0]**，即调度台**已自行认定并修掉**的那一条（夹具期望值算错，(α)），与这 6 条红无关。
