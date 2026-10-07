# W_B2 · 教程模式窗 `Tutorial Mode Menu`（A853）

> 2026-10-17 · 写手代理 B2 · 只碰三个文件（`Shell/MainMenuRuntime.cs` · **新建** `Shell/TutorialModePopup.cs`
> · `Editor/MainMenuScene.cs`）。⛔ 没跑 Unity（断言由主对话收口时统一跑）· ⛔ 没动 git · ⛔ 没改正本。

## ① 结论

1. **A853 做完了**：主菜单 Tutorial 模式卡接上 `"tutorial"` ⇒ `OpenMode` 开出了那扇窗（`TutorialModePopup.LastOpened`）。
2. **`Tutorial Mode Menu` 建了**（`Shell/TutorialModePopup.cs`）：整棵树逐项照原版 prefab 实读 —— 标题栏 /
   6 关名单（可滚）/ 右列说明 / `Play Tutorial` 钮 / 压暗层；能点关（压暗层 + 返回钮）、能换关（右列整块重建）。
3. 🔴 **`Play` 钮【打不进教程战斗】—— 如实出声，不静默、不假装**：缺的三件全在**战斗侧**（§④）。
   ⛔ 没发明教程流程，也⛔ 没拿普通 bot 局冒充教程局（那样玩家会拿到一场「没有教程的教程」）。
4. **两条错断言收口**（调度台中途追加）：`Editor/MainMenuScene.cs` 里读 `ds2.ScopeText` 那两条改成
   断原版事实 / 删掉（§⑤）；另把「Tutorial 不该有 `Hit`」那几条反面断言**翻成正面**。

## ② 窗的逐项判据（原版值 + 出处）

取数（本轮现读）：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Tutorial Mode Menu" --depth 4 --md`
+ 同命令换 `"Tutorial Army Select Button"` · 窗参 → `资料/普查产出_1006/A154_A155_窗口档位与缩放.md:361`
（该行即本窗：`93 | Tutorial Mode Menu | TutorialModePopup | 1 | 15 | 1 | 0 | 1 | 1`）· 方法体 →
`d:/2/tools/decomp_full/` 的 `TutorialModePopup__*.c` · `TutorialSpecificInfo__Setup.c` ·
`TutorialArmySelectionButton__Initialize.c` · `WindowHeaderWithBackButton__Initialize.c`。

| 项 | 原版值（实读） | 我们 |
|---|---|---|
| 窗参 | `type=1` · `placement=15` · `closeOnESC=1` · **`extraScaleSmallScreen=1.0`** | 逐条照抄（⚠️ 练习窗是 **1.07**，别互推） |
| `Menu Dark Background` | `-1327.30,-746.18→3247.30,1826.18` · 色 `a=0.773` · 挂 `BackgroundCloseButton`（点 = 关窗） | 照抄 + `MenuDraw.ShadeHit`（档 = 压暗层自己那档） |
| `Generic Window Red Background Big` | `-601.28,105.84→2521.28,1028.32` · `UI_Deck_Information_Back` 1100×701 · 九宫 `(42,363,655,81)` · `Sliced` · `ppuMul 0.62` | 照抄（同图同九宫另有两个同族调用点都不传 `borderOutPx` ⇒ 本处照办） |
| 同上 · 射线 | `m_RaycastTarget = 1` —— **本轮实测**：该 sprite 全包 **52 个实例逐条读，全是 1**（`ppuMul=0.62` 那份 = `MonoBehaviour_-4773956429851211214.json`） | 建 `AbsorbHit` 吸收层（⛔ 不建 ⇒ 点窗底会穿到压暗层**把窗关掉**） |
| `Header With Back Button` | `0,40.87→550,150.42`（**比底板矮** · 原版就这样） | 照抄（⛔ 别「顺手对齐」） |
| `Header Background` | `0,40.87→550,156.23` · `WF_Campaign_Info_Background` · 九宫 `(335,0,395,0)` · 宽 **550** = `ContentSizeFitter` **下限**（内容 491.12） | **直接复用 `LiveOpsEventWindow` 的公开常量**（同一个 prefab 件；⛔ 不抄第二份数字） |
| `Header Background (1)` / `Header Back Button` | `-462.10,40.87→87.90,156.23` / `-24.40,42.88→143.48,154.21` | 同上（复用 `LiveOpsEventWindow.Hdr*`） |
| `Window Title` | `155,57.22→430.12,139.87` · fs **67.55** · base 36 · auto `[18,67.55]` · `charSpacing 5` · `Left` · 文案 = prefab 出厂原文 `Game mode` | 照抄；🔴 原版运行时由 `WindowHeaderWithBackButton.Initialize` 按**教程事件数据**换掉（本地没有）⇒ 印出厂值 + 出声 |
| `Warlod Image` | `410.93,-9.07→1509.07,1089.07`（1098.14²）· 出厂 `sprite=0`；`SetArmyButtons` 喂督军立绘且 `set_enabled(图, sprite!=null)` | 喂 `CardArt.Portrait(督军卡)`；取不到 ⇒ **那层不画 + 出声**（照 `set_enabled` 那一支） |
| ↳ `Warlord Darkening` | `489.97,627.87→1430.03,947.80` · `Smooth background square` 32² 九宫 `(12,2,12,12)` · `a=0.816` | 照抄（⚠️ 与遭遇战窗的 `529.97,418.18→1470.03,975.82` **不是同一格**） |
| `PlayTutorialButton` / 字 | `1387.83,902.70→1828.17,1023.30` · `UI_Button_Mulligan` **Simple + preserveAspect** · `Button Text` `1403.28,909.70→1812.05,1016.18` fs 74.25 · `Play Tutorial` | 照抄 |
| `TutorialInfo` 四行 | 逐条：`265.04→370.73` fs74.25 · `326.52→457.85` fs54.30 · `475.89→552.94` fs54.30 · `576.53→831.10` fs40 **折行 + `Left/Top`** | 照抄（描述那条给折行宽 + `SetVAlign(Top)`） |
| 四行文案来源 | `TutorialSpecificInfo.Setup`：标题 = `ToUpper(词条)+关号` · 副标题 = `Loc(loreKey+后缀)` · 督军 = `Format(词条, GetWarlordName())` · 描述 = `Loc(loreKey)` | 后两条走**远端词条表** ⇒ 只有第 1 关有 prefab 出厂原文，其余印占位 |
| `Army Selector`/`Viewport`/`Filters` | `60,244.89→649.68,902.69`（父件与 `Viewport` **同矩形**）· `Filters` = VLG **spacing 7.31** · `align 0` · 出厂 **0 高 0 子**（6 格运行时 `Instantiate`） | 照抄；`Filters` 按内容长开 = 6×200+5×7.31 = **1236.55**（原版那个 0 是模板位） |
| 名单格 `Tutorial Army Select Button` | 根 **589.75×200** · 兄弟序 `Background (1)`→`Background`→`Highlight`(出厂 **act F**)→`BackgroundComplete`→`Icon` · 三行字 fs36 auto`[18,36]` `Left` | 逐层照抄（格内偏移 = 绝对矩形 − 模板根左上 `(-294.88,980)`） |
| 格 · 缺图 | `Background (1)` 的 `Tutorial Background` 512×144 | 🔴 **没进 `Resources/`**（只在 `Assets/CardPresentation/Art/原版/0_mainmenu/Tutorial_Background.png`）⇒ 节点照建、**这层不画 + 记账**（同 `40k_OfferBadge` / `40k_UI_Banner BW` 两条老账） |
| `Completed Text` | `-0.01,129.52→658.33,301.02` fs54 · 文案 = `Format(词条, 已完成, 总关数)` | 印 `Completed: 0/6`（进度在 PlayFab 云脚本、本地无源 ⇒ 0） |
| 6 关表 | 6 个 `Demo DeckInfo * Tutorial`（`tutorialIndex` **0..5**）+ 12 副 `*_Deck0_Tutorial{1..6}_*` 预组牌 | `Rows[]`：UM3 / GOF3 / SAU3 / BL5 / ASH5 / TL5 |

**督军那一列（`Rows.Hero`）的推导**：原版资产名 `*_Deck0_Tutorial{n}_{督军}` 的短名 → 我们卡表里同阵营
**唯一**含该短名的 `hero` 卡（判据链 = `DemoDeckInfoSO__GetWarlordName.c` → `classicDeck.DeckHero.GetLocalizedCardName()`）。
🟢 **独立交叉验证**：第 1 关推出来是 `UM3 Uriel Ventris`，与 prefab 出厂原文 `Warlord: <color=orange>Uriel Ventris</color>` **逐字吻合**。

## ③ 改动清单

1. **`Shell/MainMenuRuntime.cs`**（+20/−1）：`BuildModeCard(… "TUTORIAL", null)` → **`"tutorial"`**；
   `OpenMode` 加 `case "tutorial"` → `TutorialModePopup.Create(wm)` + `wm.OpenWindow(win)` + 出声。
2. **`Shell/TutorialModePopup.cs`（新建 + `.cs.meta`）**：`Create/Open(Build 打头)/Close` + `BuildBackdrop` ·
   `BuildPane` · `BuildHeader` · `BuildWarlordImage` · `BuildInfo` · `BuildArmySelector` · `RebuildStageCells` ·
   `BuildStageCell` · `BuildCompleted` · `BuildPlayButton` · `SelectStage` · `PlayTutorial`；队列 **3420–3430**
   （本轮现扫：全工程 `const int Q*` 不占这一段 —— 3400–3405 决斗 · 3450–3458 战报 · 3460–3478 排位奖励）。
3. **`Editor/MainMenuScene.cs`**（+198/−16）：
   · **翻面**：`modeCardsWithAction` 加入 `Tutorial`（原来它是反面「不该有 `Hit`」）；`:1239` 那条
     `…"Hit") == null` 改成断 `WindowButton.onClick != null`。
   · **新节** `Section("★ A853：教程模式窗 …")`（`:2860` 起）：点卡开窗 · 窗参 4 条 · 压暗层档规则 ·
     14 条 `CheckAt/CheckAtWorld` 矩形 · 6 条文案（`Game mode` / `Play Tutorial` / `TUTORIAL 01` / `The Basics` /
     `Warlord: <color=orange>Uriel Ventris</color>` / 描述）· 6 关表长度 · **开门只实例化 4 格**（视口 657.80 / 每格 207.31）·
     换第 3 关 ⇒ 右列跟着换（`TUTORIAL 03` + `Nemesor Zahndrekh`）· 第 3 关副标题印**占位**（钉「没编文案」）·
     🔴 **`Play` 点了必须出声**（`AnyModalPopupLeft` = true；⚠️ **故意不验文案** —— 文案会随缺口变，
     钉死它就是造一个迟早变假的绿）· 压暗层点击 = 关窗。
   · 🧨 判别式：`modeKind` 改回 `null` ⇒ 「接上了动作」+ 本节第 1 条红；摘掉 `case "tutorial"` ⇒ 第 1 条红
     （断的是 `TutorialModePopup.LastOpened`，不是 `MainMenuRuntime` 自己的常量 ⇒ 无自证）。
   · 另加一条**成对判据**：格的「完成」两件（`BackgroundComplete` / `TutorialComplete`）原版是**同一句
     `SetActive`** ⇒ 一关没打时**两件全关**（只关一件 = 画面上每格都写着 `Complete!`，没做却看着像做了）。

## ④ ⛔ 没查清 / 做不了的部分

**A. 「进教程战斗」—— 打不了**（`Play` 钮点了**如实出声**，见 `PlayTutorial()`）。缺的三件全在战斗侧：
① **教程关卡执行器**：`TutorialStage1..6`（79 回合 / 430 动作 / 61 条小提示）**数据在**我们工程的
`数据/游戏数据/tutorial_stages.json`，但 **全工程 0 个消费者**（`grep -rn tutorial_stages Assets/` 只命中它自己）
⇒ 跑它的那段没有。原版 = `TutorialManager` + `TutorialTipScript` + `TutorialPointer`（反编译 353 个方法体）。
② **教程对局规则**：`TutorialScenario.json`（`startingMana 1` / `startingHand 3` / `clockTimeLimit 1000`…）+
关卡 SO 的 `playerAlwaysWins` / `preventPlayerResign` / `hideCemetery` —— 引擎没这一档。
③ **12 副教程预组牌不在我们的数据里**：`Resources/prebuilt_decks.json` 是 103 副 `isPractice==1` 那一池
（生成器自注「教程那副 isPractice==0 不在这个池子里」）⇒ **连「用哪副牌开这一局」都取不到**。
⇒ 要接这条链得动 `Battle/**` + `RuleEngine/**` + `工具/gen_prebuilt_decks.py`，**都不在本批白名单** ⇒ 停手上报。

**B. 查不到的原版值**（逐条如实标，**没编**）：
① `Window Title` 的真标题（原版从教程事件数据取标签，本地任何形态都没有 ⇒ 印出厂原文）。
② 第 2..6 关的副标题 / 描述（原版走远端词条表 `Demo/GoffDeckTutorial` 等 6 个 key，客户端连 TextAsset 目录都没有）
⇒ 印占位 `—` + 出声。⚠️ Godot 原型 `tutorial.gd` 里那 6 关的 name/sub/warlord/desc **是我们自己写的**
（`资料/教程线_原版规格与资源存量.md` §五 明写「不许当权威」）⇒ **一个字都没用**。
③ `Viewport` 那颗 `RectMask2D` 的 `m_Padding` / `m_Softness`：`menu_dump` 只打字段名不打值，本轮两次尝试
（按组件 pid 反查宿主 GO）**都没拿到** ⇒ 按 **0 / 0（硬边）** 处理。⛔ **这不是「读过、确认是 0」，是「未读」**。
④ 教程进度（`Completed` 那个 n）：原版走 PlayFab 云脚本 ⇒ 本地无源，恒 0。

**C. 有意的偏离（标清，不是复刻）**：督军立绘那层 `keepAspect=true`（原版 `m_PreserveAspect=0` 拉满，我们
手上只有竖构图卡面插图）—— 与 `LiveOpsEventWindow.BuildDeckSelection` 逐字同一条处置 · `Filters` 按内容长开 ·
⚠️ **重复了一份 `WindowHeaderWithBackButton` 的建法**（本窗与 `LiveOpsEventWindow.BuildHeader` 同一个 prefab 件；
矩形已复用常量，但**建法**是两份）⇒ ⛔ 改共用件不在白名单，**停手上报**：建议另开一件收口成共用件 ·
⚠️ `Tutorial Background` 要进 `Resources/`（`工具/import_original_art.py` 的 `MENU_IMAGES` 加一条，白名单外**没动**）⇒ 名单格那层现在是空的，已记账 + 出声。

## ⑤ 调度台中途追加的「顺手收口」三件

| 位置 | 原来 | 现在 |
|---|---|---|
| `:2651`（`ds2.ScopeText`） | `ds2.ScopeText.Contains("本页列 " + tab.Count)` | **改断原版事实**：`FindChild(ds2.transform, "Scope Note") == null`（我们自己加的那行小字**一个节点都没有**）+ 把空态那件从「断字符串」升成**断 `activeSelf`**（`Empty Collection Warning` 关着 —— 原版同一件两用，**字符串空 ≠ 节点关着**） |
| `:2670`（`ds2.ScopeText`） | `ds2.ScopeText.Length == 0` | **删掉**（留 tombstones 注释） |
| `:613` 附近（设置窗页签） | 文案写「三个页签：图像 / 音频 / 联机」（**已过期**，现为 4 页），且**没断页数** | 文案改成 **General / Graphics / Audio / Online 4 页**，并**顺手把页数断上**（`Tab Buttons` 的子件 == 4 且四个名字都在）—— 原来只断「有这个节点」⇒ **少一页/多一页都不红**，那正是它能「写着三个、实际四个」还一直绿的原因。⚠️ 期望值 4 是**我们的**页数（原版那栏是 5 页 `General/Media/Account/Graphics/Support`，见 `Shell/SettingsWindow.cs` 的 `BuildTabs` 注释），⛔ 别读成「原版就是 4 页」 |

**为什么这两条要动**：那行 `Scope Note` 是**我们自己加的**、原版空态只有 `Empty Collection Warning` 一件（D11 已把节点
从屏上删掉），而 `ScopeText` 属性**还留着**（`Shell/DeckSelectionPopup.cs` 不在那批白名单）⇒ 旧两条**今天仍绿、断的却是
一个不上屏的字符串** = 假绿。第二条**删**而不改：**「有没有这一行」在两个页签上是同一个事实**（节点压根不在），上面
那条已覆盖两个页签，再来一遍是**同义反复**（工程明令不许）。⚠️ 两个**别的写手的**文件（`DeckSelectionPopup.cs` /
`SettingsWindow.cs`）**一个字没动**。

## ⑥ 类型检查（原样贴）

`TMPDIR=/tmp/wf_b2t bash d:/4/Unity/工具/typecheck.sh` → `运行时错误数: 0` / `编辑器错误数: 0`。
（中途那次 27 错**全在 `Deck/DeckRuntime.cs`** = 别的写手正在写的半成品，与本批无关；等它写完后复跑已归零，
⛔ 没去改那个文件。）行尾：三个文件**全 LF**，`git diff --numstat` = `20/1` 与 `198/16`（**没整篇翻行尾**）。
