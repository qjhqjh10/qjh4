# D2 · 轮抽（免费/常规）窗 —— 执行报告

> 执行代理 **D2**，2026-10-24。工程根 `d:/4/Unity`。
> 判据正本 = `资料/普查产出_1024b/S3_轮抽窗原版规格.md`（下称 S3）＋ 本轮亲自跑的
> `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "<节点>" --depth N --relative`
> （八棵树现读：`Draft Mode Menu Demo` / `Pay State` / `Select Packs State` / `Select Warlord State` /
> `Ongoing State` / `Deck Info Panel` / `Prebuilt Pack UI` / `Generic Select Card Container`）
> ＋ 根 MB `MonoBehaviour_-7910505541983962427.json`（窗参逐字段）。

---

## 一、做了什么（改动清单）

| 文件 | 动作 | 说明 |
|---|---|---|
| `CardPresentation/Shell/DraftModeWindow.cs` | **新建**（+ `.meta`） | 原版 `Draft Mode Menu Demo` 的复刻：背景四层 / 顶部返回栏 / 四页（Pay·SelectWarlord·SelectPack·Ongoing+Finished）/ 右栏 Deck Info Panel / Reward Info Panel |
| `CardPresentation/Shell/MainMenuRuntime.cs` | **只加 `case "draft"` 分支**（diff 15/17 行） | 点了模式卡 ⇒ `WindowsManager.OpenByRef(DraftModeWindow.RootName, …)`（旧的「暂无服务器」弹窗**不再走**，那条词条键没删） |
| `CardPresentation/Editor/DraftWindowAssertD2.cs` | **新建**（+ `.meta`） | `Run/Pass/Fail`，5 条派单验收 ＋ 3 条自加（空态 / 缺图出声 / 右栏显隐） |

**窗参**（根 MB 实读，逐字段）：`type=Popup(1)` · `windowsPlacement=Popup(15)` · `closeOnESC=1` ·
`updateNavPanel=0` · `extraScaleSmallScreen=1.0` · 两个 sound 空 PPtr。

**状态机**：`DraftModeState{Intro0,PayScreen2,SelectWarlord5,SelectPack10,OngoingRun15,Finished20,Collected22}`。
换页按**状态类型**（原版 `GetTab<T>()` + `ChangeTabCO` 的等价物 = 本文件 `PageFor(state)` 那张表），
⛔ 没有页签按钮。`Intro` / `PayScreen` **都落 Pay 页**（`GetTabFromState(0)` 的 else 分支）；
`OngoingRun`/`Finished` 同页、只切显隐；`Collected` = 关窗。

**流程**：`Free` → 选将（`WarlordsPerDraft=5` 张、按 seed 确定性抽）→ `Continue` → 选包
（`Stage Counter` = `{round+1}/{RoundsPerDraft}`；一屏包数 = `DraftPacks.RollRound(...)` 的**返回条数**）
→ 每选 1 包 +3 张、轮次 +1、到末轮自动切 Ongoing → `To Battle`：
`DraftPacks.BuildClosedDeck` → `PrebuiltDecks.SetPendingBattleDeck` + `BattleDriver.SetPendingPlayMode(ClosedDeck)`
→ `ShellRuntime.LoadSceneWithFade(ArenaByArmy.BattleSceneNameFor(阵营))`（批处理只记账）。**战斗内一行没动。**

---

## 二、验了什么

* `TMPDIR=/tmp/wf_d2 bash 工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（本轮最后一次实跑）。
* 🔴 **没跑 Unity 批处理、没跑断言**（派单红线）。`DraftWindowAssertD2.Run()` 由主对话在同步点执行；
  它可以单独跑（`-executeMethod DraftWindowAssertD2.Run`）或并进任一宿主（两行，见文件头）。
* 中途出现过两条编译错误，都不是本批代码：① 我自己两处 `System.Action` 传了 `bool` 方法（已改 lambda）；
  ② `Core/CardView.cs:636` 的 `d != null`（`CardData` 是 struct）—— **E16 当时正在写那个文件**，
  隔一会儿重跑即 0 错（那一条按纪律**没去改别人的文件**）。

### 断言逐条（写进 `DraftWindowAssertD2`，每条都带「怎么改会红」）

1. **开窗**：名字 = `Draft Mode Menu Demo`；`type/placement` 逐值；父 = `3 - PopUp Holder`；
   Pay 页节点名/active；`BackdropHit` 在；右栏在；选包页此刻关着；`LastOpened` 记到。
2. **`Stage Counter` = `1/4`**（选 1 包后 `2/4`），并**再断一次**「= 现算的 `(round+1)+"/"+RoundsPerDraft`」。
3. **一屏包数 = `RollRound` 条数**：③a 真池两处独立算法对账；③b **合成 2 包池**（`SetPacksForTest`）
   ⇒ 页面必须只给 2 个、`Packs Container` 只建 2 张、并显式断言「**不是** 3」——**把包数写死成 3 这条会红**。
4. **选 1 包 ⇒ `selectedCards` +3、`CurrentDraftRound` +1**，右栏 `Cards: 3/12`。
5. **四轮 ⇒ 12 张**（现算 + 字面量 12 两条）· `LaunchBattle()` 成功 · `PendingPlayMode == ClosedDeck(7)` ·
   交出去的牌 12 张 / `heroId` / `faction` 对得上。
6（自加）**空态**：选将页 `NoWarlordHint`、包页 `NoPackHint`（**这两句是我们自拟的** —— S3 §四 明写
   「原版这两页没有空态节点」）；候选 0 时只亮提示、一张卡都不建。
7（自加）**缺图出声**：本机缺的两张 toggle 图标必须进 `MissingArt`（见 §四·1）。
8（自加）右栏显隐：Pay/选将关、选包/进行中开、Finished 关。

---

## 三、我们挑的 / 原版在服务端的（已写在代码注释里，⛔ 别当原版真值）

| 项 | 取值 | 依据 |
|---|---|---|
| `draftRounds` | `DraftPacks.RoundsPerDraft = 4` | 服务端 `DraftEventData+0x98`，本地 0 命中（S3 §五） |
| `packsPerDraft` | `DraftPacks.PacksPerRoundOffered = 3` | 同上（`+0x9C`） |
| `warlordsPerDraft` | 本文件 `WarlordsPerDraft = 5` | 服务端（`+0xA0`）；S3 §五 也建议 5 |
| `seed` | 本窗现抽（`SeedOverride` 可钉；**打进日志**） | 服务端事件实例字段，本地不存在 |
| 选将页可滚 | `MenuScroll`（120 px/格） | 🔴 **我们挑的**：原版 `ScrollRect` 组件序列化 `m_Enabled = 0`（现读），5 张卡宽 2475px > 屏宽 ⇒ 不加滚动后 2 张永远点不到 |
| 单包卡内的 3 张卡 | **3 张缩略卡面（`CardView`）＋卡名** | 🔴 我们挑的：原版按 `cardInfoItem`（`UICardInfoItem`）逐张实例化，那颗模板的 PathID `-5880222813801091413` 在 `GameObject/` **0 命中** ⇒ 尺寸解不出 |
| 右栏默认选中 | `Deck Info`（0） | 🔴 我们挑的：原版 prefab 序列化 `Card List isOn=1`，但 `Initialize` 又把三页全关、只留 Energy 页（S3 §2.9）——原版那对值是矛盾的 |
| `ClosedDeck` 的参数表 | 🔴 **2026-10-24 已改**：显式 **`GameplayVariables.ClosedDeck`**（= `Skirmish.Clone()`，**值仍是我们挑的** —— 原版 `DraftEventData.gameplayVariables` 服务端下发、本地 0 命中）；⛔ 不再靠 `For` 的 default 落进 Classic，也不再靠调用方临时传 Skirmish | 🔴 **我们挑的**（原版参数拿不到）；`deckSize 12` / `maxLegendaries 4` / `warlordLifeChange −10` / `manaPerTurn 2` / `showMulligan=false` / `shouldReshuffle=false` 有游戏内文案旁证，`handLimit 8` / `startingHand 4` / `manaAccumulation 1` 来自非官方规则书 |
| `ClosedDeck.warlordId` | 传**阵营 id** 当标记 | `BuildClosedDeck` 的 `int warlordId` 只做标记（D1 已核）；我们的战将 id 是字符串 |
| 不做 | `Premium`/`10x` 两档 · `Reroll` 整支 · `Timer` · `Intro` 页节点 · `DEBUG_REROLL_BUTTON` · `Debug Win`/`Debug Battle` · `Generic Multi Card Display`（预览弹窗） | 价格/成本/活动时刻全在服务端；`Intro` 的激活者**全反编译查不到**（S3 §七·1）；两个 Debug 钮原版 `OnEnable` 强制关；预览是独立弹窗，不在本批范围。**每一处都在 `Open()`/`Build()` 里出声一次** |
| 动画 | 全不做（包卡入场 0.8→1、选包飞出、红底闪一下、`DOPunchScale`） | 时长/缓动只有 `DAT_*` 符号（S3 §五·3）⇒ 选包时出声说明「原版这里会闪」 |

---

## 四、顺手发现（都不在本批白名单里，**未改**）

1. **两张 toggle 图标本地没有**：`40K_Info_button_draft menu` / `40K_Cards_button_draft menu`
   在 `Resources/Art/{ui_menu,ui_deck,ui}` **0 命中**（第三张 `40K_Chat_icon_Alliance v2` 有）。
   ⇒ 现在这两格只有底、没有图标，且**记进 `MissingArt` + 出声**。要补得走美术导入（不属本批）。
2. **S3 §七·6 需要订正一处**：S3 写 Demo `Pay State > Event image` 的 `m_Sprite = 0`（运行时换图）。
   本轮现读（`menu_dump bundle_menus_assets_all "Draft Mode Menu Demo" --depth 2`）它序列化的是
   **`UI_Deck_Warlord_Abaddon` 2048×2048**（tint 0.802）⇒ 是「prefab 里残留了一张图」，不是空。
   我**没有**画它（原版运行时按活动 label 换 → 那张图本地查不到），⛔ 也没拿 Abaddon 顶替。
3. **S3 §2.9 的 `Buttons` 条宽度对不上**：S3 写 `383.52×63.15 @ (-3.31,56.70)`；现读（布局跑完）
   是 **`38.0,-56.7 → 352.1,6.4`（相对面板）= 314.05×63.15**，HLG `spacing 12.98 · align 3(MiddleLeft)`。
   我按**现读**排（三颗 82.09² 从 bar 左沿起、间距 13）。请下一位复核这两个数谁是运行期真值。
4. **`Draft Mode Select Warlord State > Generic Card Selector Menu` 的 `ScrollRect` 组件是禁用的**
   （现读 `⛔MULTI-COMP(2组件·1禁用)`，`m_Enabled = 0`）—— 而 S3 §2.4 把它当可滚控件给了
   `h=1/v=0/inertia/elasticity/decel`。⇒ 「原版那一刻拖不拖得动」按现读是**拖不动**，S3 那段描述要加注。
5. **`Generic Select Card Container` 有 2 份**（`1817043715861867242` / `7446820828767025861`），
   `menu_dump` 取第一份。**哪一份是选将页用的没分辨**（`UICardSelector.cardContainer` 的 PPtr 可再追）。
   我用的几何（视觉 503.97×754.20 · `Select Button` 210.02×81.66）来自第一份。
6. `Generic Select Card Container` 里 `Select Button` 的**视觉**矩形（101.4,585.5→311.4,667.1）S3 没给，
   本轮现读补上，已进常量注释。
7. `Draft Mode Ongoing State` 的胜场格是 `GridLayoutGroup`（cell 80² · spacing 23.7/30.04 · **2 列** ·
   align MiddleCenter），S3 §2.7 只写了「GridLayoutGroup」⇒ 参数本轮补齐（`StageCell/StageGapX/StageGapY`）。
8. `Prebuilt Pack UI > Pack title` 现读 y=25.5..58.7、`Separator` 70.4..73.3、`Select Button` x=41.9..264.0
   —— 与 S3 §2.6 的「三张卡竖直排列」不矛盾，但**原版那三张到底是 UICardInfoItem 行、还是卡面缩略图**
   本地判不出（见 §三 那条）。这是本窗**最需要下一位拿原版实拍复核**的一处。

---

## 五、还欠 / 下一位接手要看什么

* **跑一次断言**（主对话）：`DraftWindowAssertD2.Run()`。⚠️ 它会 `SetPacksForTest` 再 `RestoreRealPool()`
  （先 `InvalidateCache`、读不到再直接读 `数据/游戏数据/draft_packs.json`），跑完把
  `BattleDriver.PendingPlayMode` / `PrebuiltDecks.PendingSource` 原样还回去。
* **真机看一眼**（主对话）：`-wfscene Shell` → 点模式卡「选秀」→ 免费 → 选将 → 4 轮 → Battle!。
  最可能出问题的三处：① 包卡内 3 张缩略卡的长宽比（`PackCardsArea` 是我们挑的）；
  ② 选将页 5 张卡横向滚动的观感；③ 右栏 toggle 缺图标那两格。
* **未接**：`Generic Multi Card Display` 预览弹窗（另一扇窗）· 战斗结果回灌（胜场/败场恒 0）·
  `Finished`/`Reward Info Panel` 的触发者（需要战斗结算回写，战斗内不动）· 联盟信息页（服务端）·
  付费档 / 重掷（判据缺失）。
