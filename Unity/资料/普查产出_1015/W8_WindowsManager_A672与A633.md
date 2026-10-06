# W8 · `Shell/WindowsManager.cs` —— A672（`type` 哨兵）+ A633（注释订正）

> **执行写手 W8**（2026-10-15「清空 A 表」第六轮）。**独占文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs`
> **只改了这一个文件**（`git diff --numstat` = `78 / 8`，全在该文件内）· 行尾 **LF 保持不变**（改前 `CRLF 0 / LF 1538` → 改后 `CRLF 0 / LF 1608`）· **没跑 Unity**（本轮规矩）。
> **秒级类型检查**：`TMPDIR=/tmp/wf_w8 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**；
> 加 `WF_DOC=1` 再跑一次 ⇒ **本文件零 doc 警告**（全仓那 40 条在别的文件里，逐条 grep 过 `WindowsManager` 零命中）。

---

## 一、摘要（6 行）

1. **A672 已做**：`GameWindow.type` 的默认值从 `Fullscreen` 改成**哨兵 `UnsetType = (WindowType)(-1)`**（与 `placement` 的 `UnsetPlacement` 同形，⛔ 不是枚举成员），并补了 `HasType`。
2. **配了【两层】出声**（照 A166 已有的两层形状，⛔ 没发明第三种）：① `AttachToAnchor`（建窗那一刻）② `OpenWindow`（有人绕过 `AttachToAnchor` 时 —— 与 `GetWindowAnchor` 那条「走到这里 = 有人绕过了它」同形）。两层都照**旧默认值 `Fullscreen`** 兜底。
3. 🔴 **现核后：不是 31 处，是 52 处**（**35 个生产建窗点 + 17 处 `Editor/ShellScene.cs` 夹具**）—— 逐条核过，**全部在 `AttachToAnchor`/`OpenWindow` 之前就赋了值** ⇒ 两层守卫一处都不命中、行为零变化。
4. 🔴 **另有一类今天没人提过的**：**11 处裸 `AddComponent<GameWindow>()` 夹具探针**（`SettingsScene` 8 + `ShopScene` 3）**从不赋 `type`** —— 它们**不调** `AttachToAnchor`/`OpenWindow`（直调 `TryOpen(null)`），而 `type` 的读者只有 `OpenWindow`/`ShowPreviousWindow` ⇒ **两层守卫都不命中、今天行为不变**（但它们的字段值现在确实是哨兵，见 §四-③）。
5. **A633 已做**：那段「`MainMenuRuntime._openByRef` 第二份要等调度台合并」**已按实情订正** —— 前提（A177 尾巴，2026-10-11）**早就收口了**：`MainMenuRuntime` 里**没有** `_openByRef` 字段，只剩一行转调；顺手把同段里另一句同样过期的「`Ref*` 常量**合并时以这里为准**」也订正了（同一前提、同一文件，铁律 5 的「同一句话被复制到别处一起改」）。
6. ⚠️ **一条没落地**：A672 要的「配套**断言**」的**夹具那一半**不在我的白名单里（宿主是 `Editor/{Settings,Shell}Scene.cs`）⇒ 我**把可直接粘贴的候选断言写在 §四-①**（标了「未跑」），**没有替它落地**。

---

## 二、改动清单（全部在 `Shell/WindowsManager.cs`，按锚点）

| # | 锚点（内容，行号按改后） | 改前 | 改后 | 依据 |
|---|---|---|---|---|
| 1 | `GameWindow` 的 `type` 字段声明（`:87` 一带） | `public WindowType type = WindowType.Fullscreen;` | 前面插入 `UnsetType` 哨兵（`public static readonly WindowType UnsetType = (WindowType)(-1);` + 一整段 doc）+ 字段改成 `public WindowType type = UnsetType;` | A672（`批次计划_1013.md:359`）· 形状照 A166 的 `UnsetPlacement`（`WindowsManager.cs:96`/`:98` 那一对，**同形不同值**） |
| 2 | `HasPlacement` 属性之后 | （无） | 新增 `public bool HasType { get { return (int)type >= 0; } }` + doc | 照 `HasPlacement`（`:394`）逐字同构；A166 那条★断言就是拿 `HasPlacement` 判「出厂是哨兵」的 |
| 3 | `AttachToAnchor` 的 doc 与体内（`placement` 那段**之后**、`GetWindowAnchor(p)` **之前**） | 只有 `placement` 一条哨兵出声 | 追加第二条：`if (!win.HasType) { Debug.LogError("…的 `type` **没有显式赋值**（还是哨兵 -1）…"); win.type = WindowType.Fullscreen; }` | A672 要的「出声」；位置照 A166（那里就在 `AttachToAnchor` 拦） |
| 4 | `OpenWindow` 体内（`OpenWindow(null)` 那句之后、`if (closeAll)…` 之前） | （无） | 新增第二层哨兵出声 + `win.type = WindowType.Fullscreen;` 兜底 | 见 §三-①（不补这一层的话，哨兵会在 `== Fullscreen` 这个**肯定式**判断下**静默走弹窗支** —— 另一种坏法） |
| 5 | `OpenByRef` 上方那段「缓存搬到这儿」的注释（`:1167` 一带） | 「⚠️ **今天仓里仍是两份**（`MainMenuRuntime._openByRef` 那 8 条…）……合并成**一份**要等调度台把那边 8 条改成转调这里……**已写进报告**」 | 改成「✅ **已合并成一份**」+ 保留更正痕迹（原文引用）+ 现读实据（`MainMenuRuntime.cs:562-564` 一行转调 · 全仓 `_openByRef` 字段声明**只有本文件这一处**）+ 指向守它的断言（`MainMenuScene.cs` ⑥） | **A633**（`批次计划_1013.md:320`）；实据见 §三-④ |
| 6 | `PrefabRef*` 那一族常量的 doc（`:1194` 一带） | 「⚠️ `MainMenuRuntime` 那边**另有一份同名的 `Ref*` 常量（合并时以这里为准）**」 | 改成「→ 那个合并**已经发生**；现读：重名的只有 `RefChat`/`RefProfile` 两个，且它们**直接取这一份**（`= WindowsManager.PrefabRefChat;`）；其余 6 个是那边独有的入口键」 | 与 #5 **同一前提**（都指向那场已完成的合并）⇒ 铁律 5「同一句话被复制到别处一起改」 |

⚠️ **改动 #6 是我的判断，不是派单点名**：A633 只点名了「要等调度台合并」那句（#5）。我把它一并改的依据是：它写的是「**合并时**以这里为准」—— 说的**是同一场**（A177 的缓存/常量合并）、而且同样**已经发生**；留着它下个会话还会以为「合并还没做」。如果你认为这超出了 A633 的范围，回退 #6 一行即可（#5 独立成立）。

---

## 三、「31 处为什么不受影响」—— 现核后是 **52 处**，逐条说明

🔴 **先订正数字**：派单 / `批次计划_1013.md:359` 写的是「今天 **31** 个 `Create()` 逐个核过全都有值」。
**我复算不出 31**，实读是 **35 个生产建窗点**（`Shell/` 各窗的建窗方法，一个窗一处 `win.type = …`）+ **17 处夹具**（`Editor/ShellScene.cs`）= **52 处**。
（可能的解释、**但没核**：A251 那 6 扇 + A103 那 1 扇是 WA569 之后建的 ⇒ 快照口径会偏小。**别把我的推测当结论**。）

### ① 35 个生产建窗点（`Shell/`）—— 零影响

逐个核过，**形状完全一样**：`var go = new GameObject(...)` → `AddComponent<XxxWindow>()` → **紧接着 `win.type = WindowType.…;`** → …… → `AttachToAnchor(win)`（有的还 `mgr.OpenWindow(win)`）。
⇒ 走到两层守卫时 `HasType` **恒为 true** ⇒ **不报、不改值、行为逐位不变**。

**35 个文件:行（`win.type = …` 那一行）**：
`AllianceMemberOptionsPopup:210` · `BaseOfferPopup:629` · `BattleLogPopup:113` · `BoosterInfoPopup:235` · `BoosterPackOpenWindow:280` · `CampaignRewardWindow:307` · `CardDetailPopup:270` · `ChatPanel:154` · `CollectionWindow:119` · `DailyRewardPopup:141` · `DailyStreakPopup:239` · `DeckInfoPopup:713` · `DeckSelectionPopup:211` · `DuelPopupWindow:111` · `EnergySinglePlayerOnlyEventWindow:236` · `GenericOptionsPanel:212` · `ImportDeckPopup:76` · `InboxWindow:227` · `LeaderboardWindow:217` · `LiveOpsEventWindow:333` · `MissionRerollPopup:283` · `PlayerProfileWindow:211` · `PopUpGameWindow:236` · `PracticeModePopup:722` · `PromptPopup:92` · `PurchasePremiumWindow:273` · `RankedRewardEventWindow:240` · `ReferralPopupWindow:292` · `RewardsWindow:222` · `RewardWindow:745` · `SearchingOpponentWindow:163` · `SettingsWindow:396` · `ShopWindow:104` · `SocialWindow:101` · `TrophyInfoPopup:205`
（全部经脚本 census，不是抽样；**35 个文件 = 35 个 `AttachToAnchor` 调用点**，两个数对得上。）

⚠️ **两处「差点漏掉」的**（`grep` 首轮被 `head` 截断过，第二轮补上）：
- `TrophyInfoPopup:205` —— 它的建窗方法叫 **`Open(...)`** 不叫 `Create()`，按名字搜会漏；`type = Popup`（原版 MB 实读 `type = 1`）。
- `PracticeModePopup:722`（首轮读到的是 `:709` 那条注释里的行，真赋值在 `:722`）。

### ② 17 处夹具（`Editor/ShellScene.cs`）—— 零影响

同样形状：**先赋 `type`、再 `AttachToAnchor`**。逐处（`type=` 行 → `AttachToAnchor` 行）：
`:875→877` · `:1740→1743` · `:1857→1860` · `:1891→1894` · `:1924→1927` · `:1953→1955` · `:2130→2132` · `:2184→2187` · `:2294→2297` · `:2353→2354` · `:2357→2358` · `:2361→2362` · `:2826→2829` · `:2885→2886` · `:2899→2900` · `:3134→3136` · `:3161→3163`。⇒ **17/17 都在守卫之前赋了值**。

### ③ 11 处裸 `AddComponent<GameWindow>()` 夹具探针（**没人提过、我自己查出来的**）—— 零影响，但有边界

| 文件 | 行 | 它们干什么 |
|---|---|---|
| `Editor/SettingsScene.cs` | `:518`（A166 探针）· `:1333` · `:1343` · `:1362` · `:1378` · `:1469` · `:1746` · `:1779` | **只赋 `extraScaleSmallScreen` + 直调 `TryOpen(null)`**（A165/A228/A167 那几族小屏缩放/命中探针） |
| `Editor/ShopScene.cs` | `:3331` · `:3494` · `:3605` | 同上（A294/A297/A298） |

**它们全都不赋 `type`**，但**今天行为不变**，判据两条（都是现读）：
- **(a) 它们不调 `AttachToAnchor`、也不调 `OpenWindow`** —— 直调 `TryOpen(null)` → `OpenByState()`，而 `OpenByState` **一个 `type` 都不读**（读它的是 `OpenWindow` 的分支与 `ShowPreviousWindow` 的弹窗判定）。
- **(b) 全仓 `GameWindow.type` 的读者只有 3 处**：`OpenWindow:915`（=`if (win.type == Fullscreen)`）· `ShowPreviousWindow:1080/1082/1094` · `Editor/MainMenuScene.cs:6806`（那条断的是 `TrophyInfoPopup`，**赋过值**）。而这 11 个探针**一处都不经过**。

⇒ 结论：**两层守卫对这 11 处一声不响**（`AttachToAnchor`/`OpenWindow` 都没被调）—— **它们今天安全，靠的是「没人读它们的 `type`」**，**不是**靠「它们赋过值」。
⚠️ **这是今天最脆的一格**（如实标）：它们的字段值**现在确实是哨兵 `-1`**。将来谁把这类探针**改成走 `AttachToAnchor` 或 `OpenWindow`**，就会**立刻收到两条 `LogError`**（那是设计意图），但如果谁只是**新增一个 `type` 的读者**（比如「`!= Popup` 就当全屏」这种写法），哨兵会**静默**落进另一支。**建议**（不在本件范围）：这套探针建窗时顺手写一句 `w.type = WindowType.Fullscreen;`（它们本来就在测「窗根」这一层，赋个明确值最省事）。

### ④ 为什么第 2 层（`OpenWindow`）**必须**有 —— 不补它，哨兵会换一种坏法（静默）

原判据说的是「忘赋 ⇒ 静默走**全屏**支（开它就把别的窗全藏起来）」。那个描述**只在旧默认值 `Fullscreen` 下成立**。
一旦默认值变成哨兵：`if (win.type == WindowType.Fullscreen)` 是**肯定式**判断 ⇒ 哨兵 `-1` **判假** ⇒ 会落进 **else（弹窗支）**：`currentWindow.ToBackground()` + `popUpWindow = win`。
⇒ 那就从「静默藏掉所有窗」变成「静默把别的窗压到背景、还把自己记成弹窗」—— **同样是静默、同样没有断言会红，而且症状更难认**。
⇒ 所以两层都**先落定成旧默认值 `Fullscreen`**，再往下走：**兜底只有一种**，且与改前逐位一致。

### ⑤ A166 那两条既有★断言**保持绿**的推理（请复核这一格，它是本件唯一有耦合的地方）

`Editor/SettingsScene.cs:517-533` 那只探针窗**只赋 `placement`、从不赋 `type`**，而它**调两次 `AttachToAnchor`**，第二次外面套着「**不报警**」的断言。逐拍推：

| 拍 | 调用 | 进 `AttachToAnchor` 时的字段 | 结果 |
|---|---|---|---|
| 1 | `CaptureErrors(() => AttachToAnchor(probeWin))` | `placement = -1`，`type = -1` | 先报 `placement` 那条、**再报 `type` 那条**（顺序 = 代码顺序）⇒ `errs = [placement, type]` |
| — | `CheckTrue(errs.Count > 0, …)` | — | **✓**（2 > 0） |
| — | `CheckTrue(errs[0].Contains("没有显式赋值"), …)` | — | **✓** —— `errs[0]` 仍是 `placement` 那条（它在前）；两条文案**都**含这四个字，但字段名不同（`` `placement` `` / `` `type` ``），所以**不会互相冒充** |
| 2 | `probeWin.placement = Popup;` 然后 `CaptureErrors(() => AttachToAnchor(probeWin))` | `placement = Popup`（显式），`type = Fullscreen`（**第 1 拍已写回**） | **两条都不报** ⇒ `errs2.Count == 0` ⇒ **✓** |

🔴 **这一格的全部重量压在第 1 拍那句【写回】**（`win.type = WindowType.Fullscreen;`）—— 见 #3。**如果复核时把写回删掉，`:531` 那条立刻红**（第二次会报 `type`）。
⚠️ **另注**：`placement` 那条**不写回**（照 A166 原样，只用一个局部量 `p`）—— 两者**故意不对称**，理由见下条。

### ⑥ 为什么 `type` 要写回、`placement` 不写回（如实标：这是**我挑的**，不是原版判据）

- `placement` 的**消费者只有 `GetWindowAnchor` 一处**，而它就在同一个方法体里、用的就是那个局部量 `p` ⇒ **哨兵不会漏出去** ⇒ 不写回也安全。
- `type` 的消费者在**另一个方法**（`OpenWindow`）里 ⇒ 不写回就会漏 ⇒ 必须写回。
- 代价（有意接受）：写回之后**同一个对象**再挂一次不再重复出声。今天没有第二处会这么干（只有 A166 那只探针会调两次），而**新窗忘赋**是「一次建窗一次出声」⇒ 不影响「必须出声」这条红线。

---

## 四、没做完 / 做不了的

### ① 🔴 A672 的「配套**断言**」—— 夹具那一半**没落地**（不在白名单）

本件能落的只有**运行时守卫（两层出声）**；**断言**必须落在自检宿主里，而那两个宿主（`Editor/SettingsScene.cs` / `Editor/ShellScene.cs`）**都在我的白名单之外** ⇒ 我**一行都没碰**（照「卡住就停手写进报告、⛔ 别去别的文件补」）。

**照 A166 那条的形状**（`:508-533`，同一个宿主、同一套助手 `CaptureErrors` / `CheckTrue` / `Section`），**建议插在 A166 那一节之后**（同一片区域内，`CaptureErrors` 就近可用）：

> ⚠️ **下面是【候选】，我一次都没跑过**（本轮不跑 Unity）。落地前请自己核两件事：
> **(a)** 第二层那段会让 `wm.OpenWindow` 走**全屏支** ⇒ `if (openWindows.Count > 0) HideAllWindows();` **会把别的窗藏起来** ⇒ **要么在 `wm.openWindows` 为空时跑它，要么前后各 `wm.CloseAllWindows()`**（否则会污染后面的段）。
> **(b)** `errs[0]` 的取法照 A166 —— 本件的两条文案**字段名不同**（`` `type` `` / `` `placement` ``），可以按字段名精确咬。

```csharp
            // ---------------- 🆕 A672：`type` 忘了显式赋值 ⇒ **出声**（两层，与上面 A166 同形）----------------
            // 判据：原版 `type`（字段 0x20）在 prefab 里**必填** —— `WindowsManager__OpenWindowCO.c` 按它决定
            //   「把其余全部藏起来」（==0）还是「把上一个压到背景」（==1）。
            Section("A672：`type` 忘了显式赋值 ⇒ 出声（① 建窗那一刻 ② 绕过 `AttachToAnchor` 直接开）");
            {
                Debug.Log(P + "  ⚠️ 下面这几条会**故意**打几行 `[Win] …` 的 LogError（就是「出声」本身）—— 那不是失败");
                // 第一层：`AttachToAnchor`
                var tGo = new GameObject("probe window (type 未赋)");
                var tWin = tGo.AddComponent<GameWindow>();
                tWin.placement = WindowsPlacement.Popup;      // 先跟 A166 那条隔开（两条都报时也能各认各的）
                CheckTrue(!tWin.HasType, "裸 `GameWindow` 的 `type` 出厂是**哨兵**（= 还没显式赋过值）");
                var tErrs = CaptureErrors(() => WindowsManager.AttachToAnchor(tWin));
                CheckTrue(tErrs.Count > 0 && tErrs[0].Contains("`type` **没有显式赋值**"),
                          "★ **忘了赋 `type` ⇒ 出声**（改坏法：默认值改回 `WindowType.Fullscreen` ⇒ 一声不吭 ⇒ 红）"
                        + "；实得 " + tErrs.Count + " 条：" + (tErrs.Count > 0 ? tErrs[0] : "**一条都没有**"));
                CheckTrue(tWin.type == WindowType.Fullscreen && tWin.HasType,
                          "…而且照**旧默认值 `Fullscreen`(0)** 兜底 + 写回（⛔ 别让它落成「当弹窗」那一支）");
                // 反面（互为对照）：显式赋过值 ⇒ 一声不吭
                var t2Go = new GameObject("probe window (type 已赋)");
                var t2Win = t2Go.AddComponent<GameWindow>();
                t2Win.placement = WindowsPlacement.Popup;
                t2Win.type = WindowType.Popup;
                var tErrs2 = CaptureErrors(() => WindowsManager.AttachToAnchor(t2Win));
                CheckTrue(tErrs2.Count == 0, "显式赋过 `type` 的窗**不报警**（实得 " + tErrs2.Count + " 条）");
                Object.DestroyImmediate(tGo); Object.DestroyImmediate(t2Go);

                // 第二层：绕过 `AttachToAnchor` 直接开（⚠️ 这一段会让 `OpenWindow` 走全屏支 ⇒ 先清空窗表）
                var wm = WindowsManager.EnsureHost();
                wm.CloseAllWindows();
                var t3Go = new GameObject("probe window (type 未赋 + 绕过 AttachToAnchor)");
                var t3Win = t3Go.AddComponent<GameWindow>();
                var tErrs3 = CaptureErrors(() => wm.OpenWindow(t3Win));
                CheckTrue(tErrs3.Count > 0 && tErrs3[0].Contains("还是哨兵"),
                          "★ **绕过 `AttachToAnchor` 直接开** ⇒ 第二层也出声（实得 " + tErrs3.Count + " 条："
                        + (tErrs3.Count > 0 ? tErrs3[0] : "**一条都没有**") + "）");
                CheckTrue(t3Win.type == WindowType.Fullscreen, "…第二层也照旧默认值 `Fullscreen` 兜底");
                wm.CloseAllWindows();
                Object.DestroyImmediate(t3Go);
            }
```

### ② 两条「做不到 / 今天验不了」

- **没有跑 Unity** ⇒ 上面那个候选断言、以及「A166 两条★断言仍绿」**都是静态推理**（§三-⑤ 那张表），**不是跑出来的结论**。同步点那次 `SettingsScene.Run` 才是判据。
- **`Shell/` 里没有可放断言的宿主**（本文件只有 `ClearAnchorsForTest` / `ClearReuseCacheForTest` / `Dump()` 三个测试向口，没有 `CheckTrue` 那一套）⇒ **没有第二条路**能让我在本文件里落一条「会红」的断言。

### ③ 边界（今天没影响、但要说清）

`HasType` **只判「赋没赋过」，不判「值合不合法」** —— 有人写 `win.type = (WindowType)2`，`HasType` 照样 true、`OpenWindow` 会静默走弹窗支。
⚠️ 这是**既有**的洞（改前 `type` 默认 `Fullscreen`，写 `2` 同样静默走弹窗支），**本件没引入、也没修**（修它要另立形状，超出 A672 的哨兵口径）。今天全仓 `.type =` **零处**非 `WindowType` 字面量的赋值（脚本扫过，唯一命中是一条讲 `Image.type = Sliced` 的注释）。

---

## 五、顺手发现（⛔ 我自己没动别的文件）

1. **🔴 `批次计划_1013.md:359` 的「31 处」** —— 现读是 **35（生产）+ 17（夹具）= 52**。同一条注释里写的 `:87` 也已漂到 `:87`→（现在 `type` 字段在 `:102`，`UnsetType` 在 `:99`）。**建议把 A672 那一行的计数与行号一并订正**（这条正本归调度台）。
2. **⚠️ `WindowsManager.cs:393-394` 的 `HasPlacement` 也有同一族边界**（把 `None`(0) 当「赋过」）—— 今天靠 `GetWindowAnchor` 那句「找不到 None 的锚点」兜底出声，**不是静默**，所以不算缺陷；但「哨兵只判赋没赋过」这条口径建议写进 A166 的 doc（**我没改它，避免越 A166 的界**）。
3. **🔴 `Shell/MainMenuRuntime.cs:532-539` 与 `WindowsManager.cs` 那段是「同一件事的两份叙述」** —— 那边**已经**写对了（「现在这里只剩**一条转调**」），只有 `WindowsManager` 这一侧还停在「仍是两份」。⇒ 这正是 A633。**顺带提示**：那两处以后若要合并叙述，**以 `WindowsManager` 那一份为准**（判据与「为什么」按铁律只留一处）。
4. **`Editor/MainMenuScene.cs:9120` 有一条既有的 `CS1570`（XML doc）**，以及 `BattleScene.cs:40/46/217`、`RuleEngineTest.cs` 多条 —— **都不是本件**（`WF_DOC=1` 跑出来的全仓 40 条，本文件零命中）。如实记，⛔ 没去改。
5. **🔴 那 11 处裸 `AddComponent<GameWindow>` 探针（§三-③）建议补一句 `type`** —— 今天靠「没人读它」保平安，属于「安全但脆」。**不在本件范围**，留个账。
