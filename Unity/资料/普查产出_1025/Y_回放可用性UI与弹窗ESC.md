# 执行代理 Y · 回放可用性最后一跳（Shell 侧）+ 弹窗 ESC 口径（2026-10-25）

> 任务 = 主对话派单的 `Y1`（回放钮按 `replaysVersion` 点亮/灰掉）· `Y2`（`OnReplay` 读盘失败弹窗）·
> `Y3`（`PopUpGameWindow.closeOnEsc` 口径）· `Y4`（断言）。
> 判据由代理 X 查实（`资料/普查产出_1025/X_回放Q5_执行报告.md` §三 / §4.2 / 五-1/3），本件**只做复核 + 落地**，没重复考据。
> ⛔ 没跑 Unity · 没动 git · 没碰正本 / 任务总表。

---

## 〇、结论速览

| 件 | 结论 |
|---|---|
| **Y1** | ✅ 已接：`MatchLogRow.Build` 现在按 `ReplayStore.ReplayAvailable(m.ReplayFile)` 决定**建不建 `Hit`**；没有回放 ⇒ 不建（= 原版 `interactable = false` 的效果）+ `NoteNoReplay` **出声**。灰态**没做**（原版禁用图 pid 反查不到名字 ⇒ 只能「我们挑的」，按铁律 11 例外①不做）。 |
| **Y2** | ✅ 已改：`OnReplay` 读盘失败不再只 `LogWarning` —— 现在 `LogError` + 弹 `CustomErrors/ErrorLoadReplay`（钮 `MainMenu/General/OK`）+ **不进回放**，键常量**复用** `BattleDriver.ReplayErrorKey / ReplayErrorOkKey`（零字符串重复）。 |
| **Y3** | ✅ 已核 + 已改：原版 `ShowPopUp(text, **localizeTexts**, **closeOnEsc**, …)` —— **第 3 个形参 = `localizeTexts`、第 4 个 = `closeOnEsc`**（证据见 §四）。`PopUpGameWindow.Create` / `WindowsManager.ShowMessagePopUp` 现在**可由调用方指定** `closeOnEsc`，**默认 `false` = 现状不变**（⛔ 没把所有弹窗一刀切成 1）。 |
| **Y4** | ✅ 已写：真值表 3+1 条 · 行级有/无回放各 1 条 + 2 条前提 · 失败弹窗 8 条 · 负例 4 条（共 18 条，全部带「🧨 改坏法」）。落点 = `Editor/MainMenuScene.cs`（对局历史弹窗那一节末尾）。 |

---

## 一、判据复核（Y3 的形参顺序 —— 本件唯一重新查证的一处）

| 结论 | 证据 |
|---|---|
| `WindowsManager.ShowPopUp` **三个重载的前三个形参完全同位**：`(string text, bool localizeTexts, bool closeOnEsc, …)` | 签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:327`（单词版）/`:331`（双串版）/`:335`（`GameWindowButton` 版）**逐个亲读** |
| 反编译体 `WindowsManager__ShowPopUp.c` 把 `param_3` 传给 `LoadPopUpAndShow` 的**第 3 位**（`localizeTexts`）、`param_4` 传给**第 5 位**（`closeOnESC`） | `d:/2/tools/decomp_full/WindowsManager__ShowPopUp.c:17`：`LoadPopUpAndShow(param_1,param_2,param_3,0,param_4,0)`；被调方签名 = `LoadPopUpAndShow(string text, bool localizeTexts, GameWindowButton[] gameWindowButton, bool closeOnESC)`（签名桩 `WindowsManager.cs:340`） |
| 落点确认：`closeOnEsc` 是**第 5 个形参**（`this` 之后第 4 个），在方法末尾**写进窗字段**（偏移 `0x39`） | `PopUpGameWindow__ConfigurePopUp.c:44`：`*(undefined1 *)(param_1 + 0x39) = param_5;`；签名桩 `PopUpGameWindow.cs:25` = `ConfigurePopUp(string text, bool localizeTexts, GameWindowButton[] buttons, bool closeOnEsc)` |
| `localizeTexts` 是**第 3 个形参**（另一个 bool）：它只决定正文要不要过 `GetTranslation` | `PopUpGameWindow__ConfigurePopUp.c:32`（`SetText(this, text, param_3)`）与 `:129-142`（`if (param_3 == 0) 原样；else GetTranslation`） |
| 原版这条失败弹窗传的是 **`localizeTexts: 1` / `closeOnEsc: 1`** | `BattleLogItem.__c___ClickReplayButton_b__15_0.c:24`：`WindowsManager__ShowPopUp(this, <CustomErrors/ErrorLoadReplay>, 1, 1, <MainMenu/General/OK>, 0, 0)` |
| ⚠️ 如实标：我们读到的那个 `.c` **体**是三个重载里的 **`GameWindowButton` 版**（它 `FUN_1803f37c0(…,1/2)` 建 `GameWindowButton[]`，见 `:17-64`），不是调用点那一版；**但前三个形参的位次由签名桩统一**，且 `ConfigurePopUp` 那一跳（`param_5 → 0x39`）是**独立**补证 ⇒ 结论不受「哪一版体」影响 | 见上四行 |

> 所以：X 的「`1, 1` = `localizeTexts: true` / `closeOnEsc: true`」**成立**，现在有**形参名**级的证据（X 当时是从调用位次反推的）。

---

## 二、Y1 · `Shell/MatchLogRow.cs` 回放钮的可用性门槛

**结论**：判据接线完成。**不可回放 ⇒ 不建 `Hit`** ⇒ 真鼠标也点不到（`PointerLayer` 的命中表只收 `ImageQuad` / `WindowButton`，
`MenuDraw.Hit` 是唯一入口）—— 这比「点了弹一句日志」更贴原版 `interactable = false`。

**改动清单**（`Unity/MyGame/Assets/CardPresentation/Shell/MatchLogRow.cs`）：
1. `:296-330`（`Build()` 里两个圆钮那一段）：`bool canReplay = ReplayStore.ReplayAvailable(m.ReplayFile);`
   - `!canReplay` ⇒ `ReplayStore.NoteNoReplay(m.ReplayFile, "对局历史行 \`ReplayButton\`")`（**去重出声**，红线「不许静默」）；
   - `if (canReplay)` ⇒ 照旧建 `Hit`（SpriteSwap 的 `target = repBg` 不变）；
   - `else` ⇒ **不建 `Hit`**，并写明为什么「不建就够」+ **为什么不画灰**。
2. **灰态没做（如实标）**：原版那颗钮 `m_Transition = 2(SpriteSwap)`、`m_SpriteState.m_DisabledSprite` 有值
   （pid `5693181797853584851`），但**那个 pid 反查不到名字**（`资料/普查产出_0927/回放_界面真值.md:221`）
   ⇒ 原版禁用图**本地拿不到**，灰只能是我们挑的 ⇒ 按**铁律 11 例外①** 不做。⛔ 代码里没有任何自造的灰度值。
3. 判据映射（照 X）：`Match.ReplayFile` **空 / `"-1"`** = 没有；**⛔ 与 `ReplayRecord.version` 无关**
   （那是**录像文件格式**的版本，对不上时 `ReplayStore.Load` 会拒绝播放，是另一道闸）。

---

## 三、Y2 · `OnReplay` 的读盘失败分支（Q5.1 落到玩家眼前的最后一跳）

**结论**：`Shell/MatchLogRow.cs:458-477` 的 `rec == null` 分支从「只 `LogWarning`」改成：
`Debug.LogError`（写明三种失败 + 出处）→ `WindowsManager.Get().ShowMessagePopUp(BattleDriver.ReplayErrorKey,
BattleDriver.ReplayErrorOkKey, null, closeOnEsc: true)` → `return`（**不进回放**、不切场景）。

- **键常量复用 X 加的那两颗**（⛔ 没有一个字面量被抄第二份）。
- **`closeOnEsc: true` 是照原版**（那一处原版传 `1`，见 §一）；这也是这一扇窗**唯一的出口**（见 §七·3）。
- 空 `ReplayFile` 的兜底分支（`:452-457`）**保留原样**（只出声、不弹窗）：那是「门槛生效后本来就点不到」的点击侧兜底，
  原版那一档根本没有这个入口。

---

## 四、Y3 · `closeOnEsc` 由调用方指定（口径订正）

**改动清单**：
1. `Shell/PopUpGameWindow.cs:282-303`：`Create(..., string secondaryKey = null, System.Action onSecondary = null,
   **bool closeOnEsc = false**)`；`win.closeOnEsc = closeOnEsc;`
   - 🔴 **就地订正一句错记录**（铁律 5）：原注释「实证 `closeOnESC = 0`」说的是**两扇 prefab 自己的序列化值**，
     而**原版运行时那个值由调用方经 `ConfigurePopUp` 覆盖**（`:44` 写 `0x39`）⇒ 那句不能当运行期真值。
2. `Shell/WindowsManager.cs:1360-1368`（`ShowMessagePopUp` 加同名可选形参 + `:1399` 透传）。
3. **默认 `false` = 现状逐位不变**（11 个生产调用点 + 4 个自检站点零改动）—— 任务明确要求「⛔ 别把所有弹窗一刀切成 1」，
   而**每个原版调用点传什么要逐条核**（已核到的两处：`DeckEditingWindow.TrySaveDeck` = **0**；
   `BattleLogItem.ClickReplayButton` 的失败弹窗 = **1**）。
4. `Shell/WindowsManager.ShowPopUp`（旧外壳，`:1296`）**没加**这个形参：它是「先关窗再回调」的兼容壳，
   今天 11 个调用点全走它、且它们的原版实参还没逐条核 ⇒ ⛔ 不在这里猜（记账 → §七·4）。

---

## 五、Y4 · 断言清单（全部落在 `Editor/MainMenuScene.cs` · 对局历史弹窗那一节的末尾）

> 为什么放这一边：这一节**已经**开着 `BattleLogPopup` + `WindowsManager`（`menu.OpenBattleLogPopup()` 里 `EnsureHost`），
> 而且行模板就是 `Shell/MatchLogRow.cs` 那一份 —— 点的是**生产那条路**（行上 `ReplayButton/Hit` → `OnReplay`）。
> `Editor/ShellScene.cs` 不加（它那一节只验视口/裁切几何，没有对局数据夹具）。

| # | 断言 | 灭自证（写进文案） |
|---|---|---|
| 1 | 真值表：`""` / `null` / `"-1"` ⇒ **没有回放** | — |
| 2 | 真值表：`"abc.json"` / `"4"` ⇒ **有回放**（并注明与 `ReplayRecord.version` 无关） | — |
| 3 | 有录像的行 ⇒ `ReplayButton` 下**有** `Hit` + `WindowButton` | — |
| 4 | （前提）没录像的行 ⇒ `ReplayButton` **图还在** | — |
| 5 | **★** 没录像的行 ⇒ `ReplayButton` 下**没有** `Hit` ⇒ 点不到 | 🧨 拆掉 `if (canReplay)` ⇒ 红；🧨 `ReplayAvailable` 恒 `true` ⇒ 也红 |
| 6 | （前提）失败档那行回放钮点得到 | — |
| 7 | **★** 读盘失败 ⇒ **弹出一扇消息窗** | 🧨 退回「只 `LogWarning`」⇒ 红 |
| 8 | 正文键 = `CustomErrors/ErrorLoadReplay` | 🧨 换键 ⇒ 红 |
| 9 | 那颗钮 = `MainMenu/General/OK` | 🧨 换键 ⇒ 红 |
| 10 | 正文 = **本地化后**的「加载比赛回放出错。」（= `Loc.T(键)`） | — |
| 11 | **★ 灭自证**：印的**不是键名本身** | 🧨 `Term()` 末句改回 `return key ?? ""` ⇒ 红 |
| 12 | **★** 这一扇 **`closeOnEsc == true`**（原版那一处传 `1`） | 🧨 改回写死 `false`（= 改前）⇒ 红 |
| 13 | 失败 ⇒ **不进回放**（`PendingPlay` 空 + 一个场景都没切） | — |
| 14 | （前提·负例）写出一份合法录像（落进自检临时目录） | — |
| 15 | （前提·负例）正常录像那一行点得到 | — |
| 16 | 负例·前提：合法录像 ⇒ **真的进回放了**（`PendingPlay` 已挂） | — |
| 17 | 负例：切场景那一跳选的是 `Battle`（夹具无开局头 ⇒ 兜底） | — |
| 18 | **★★ 负例：正常录像一扇窗都不弹** | 🧨 把弹窗挪到无条件支 / 拆掉 `if (rec == null)` ⇒ 红 |

**配套的两处夹具改动**（否则门槛一接上，老断言会红）：
- `MainMenuScene.cs:7258` 的行模板夹具补 `ReplayFile = "__row_template_probe__.json"` —— 它下面那两条
  `repHit` / `repBtn`（`:7353-7356`）验的是「有录像的行点得到」，**必须**有录像名；
- `MainMenuScene.cs:14813` 那 6 行 `Feed N` 夹具补 `ReplayFile = "__feed_probe_N__.json"` —— 下面那条
  「滚到最下之后**列表末行的 `ReplayButton` 命中区也在**」（`:14921-14925`）同理。
- ⚠️ 另一处 `Feed N`（`MainMenuScene.cs:7371`，档案窗那一页）**没补**：那一块不断 `Hit`，那些行本来就该是「没有录像」的样本。

**新增的一个自检口**（`Shell/MatchLogRow.cs:435-442`）：`SuppressSceneLoadForTest` / `LastReplaySceneForTest` ——
批量自检里 `SceneManager.LoadScene` 会把整个夹具场景换掉（后面几百条断言随之消失）⇒ 成功那一支只记场景名、不切。
同族先例 = `ReplayStore.OverrideDir`。生产路径恒 `false`。

---

## 六、验证

- `TMPDIR=/tmp/wf_y1 bash d:/4/Unity/工具/typecheck.sh`：**运行时 0 / 编辑器 0**（三次，最后一次在全部改动之后）。
- 行尾（`python -I` 按字节数）：`MatchLogRow.cs` LF 496 / CRLF 0 · `PopUpGameWindow.cs` LF / CRLF 0 ·
  `WindowsManager.cs` LF / CRLF 0 · `Editor/MainMenuScene.cs` LF / CRLF 0 —— **四个文件一个字节都没翻**。
- `git diff --numstat`：`MatchLogRow.cs` **+63/−3** · `PopUpGameWindow.cs` **+14/−3** · `WindowsManager.cs` **+10/−2** ·
  `MainMenuScene.cs` **+167/−6**（其中约 9 行是**本会话之前**就在工作区的别人改动）。
- ⛔ 没跑 Unity 自检、没进真机（铁律 12：那是主对话/同步点的活）。**断言从没在运行时跑过** —— 红了先量实得值。

---

## 七、顺手发现 / 没查清 / 请主对话决定

1. 🔴🔴 **`BattleDriver.ReportReplayLoadFailed` 那扇窗现在是「关不掉」的**（`Battle/BattleDriver.cs:1439`，**我的黑名单**）。
   - 三件事叠起来：① 它 `ShowMessagePopUp(键, 键, **null**)` ⇒ 那颗 OK 的 `onClick` 是 null（**原版同形**：
     `ShowPopUp(…, OK, **0**)` ⇒ `PopUpWindowButtonPressed` 只调 `OnWindowSubmitted`、不关窗）；
     ② `closeOnEsc` **默认 `false`**（两扇 prefab 的序列化值）；③ 压暗层那颗 `BackgroundCloseButton` 的 `window` 是**空引用**
     ⇒ 点背景也不关。⇒ **战斗里弹出来就出不去了**（只能靠别处再弹一扇把它顶掉）。
   - **修法一行**：`Battle/BattleDriver.cs:1439` → `ShowMessagePopUp(ReplayErrorKey, ReplayErrorOkKey, null, closeOnEsc: true)`
     （原版那一处传的是 `1`，判据见 §一）。⛔ 我**没动**那个文件。
2. **`MatchLogRow` 空 `ReplayFile` 那一支仍只出声不弹窗** —— 有意：门槛之后它点不到，原版也没有这个入口。
   （若将来有人把 `Hit` 又建成恒有，这一支就成了玩家唯一能碰到的路径。）
3. **「OK 点了什么都不做」是原版形状，不是我们的 bug**：原版那一条 `onSubmit = 0`，`PopUpWindowButtonPressed` 只调
   `OnWindowSubmitted`。⇒ 这条链上 **ESC 是唯一出口**，所以 §四 那个 `closeOnEsc` 口径**不是锦上添花**。
4. **`WindowsManager.ShowPopUp`（旧外壳）的 11 个生产调用点，原版各自传的 `closeOnEsc` 还没逐条核**
   （已核 2 处：`TrySaveDeck` = 0、`BattleLogItem` 失败窗 = 1）。⛔ 没猜、没改默认值 —— 这是**新账**，
   判据来源 = `d:/2/tools/decomp_full/<各调用点>.c` 里 `WindowsManager__ShowPopUp` 那一跳的两个 bool 实参。
5. **`ReplayStore` 没有「出过几次声」的公开计数**（`_noReplayWarned` 是私有的）⇒ 自检只能断「有没有 `Hit`」，
   断不了「有没有出声」。要断就得加 `NoReplayWarnCountForTest`（`Battle/ReplayStore.cs` 是**我的黑名单**）。
6. `Editor/MainMenuScene.cs` 在本会话开始时**已经是 `M`**（别的代理也在动这个文件）。我的两处夹具改动 +
   一个新块都按「整行锚点」插入、`typecheck` 现绿；**若同步点发现该文件被别人重写过，请以 `git diff` 里
   本件 §五 那些断言文案为锚重新核一遍**（⛔ 别只看行号）。
