# S_D_词条线没扫范围.md — 只读普查（2026-10-18）· 代号 `S_D_词条线没扫的范围`

补 W1 词条线「最终总账 §D 类」：`Battle/` + `RuleEngine/` 全族、其它窗、除 `menus` 之外的 bundle。
本轮把 **`Battle/` + `RuleEngine/` 全族** 与 **全部 91 个顶层解包条目** 扫了一遍。
🔴 本轮**只读** —— 一个字都没改（唯一产出 = 本文件）。

---

## 一、一句话结论 + 搜索面声明（先打这个）

**结论**：`Battle/` + `RuleEngine/` 两族里**面向玩家**的写死文案 = **43 条清单行**（`Battle/` 35 行 · `RuleEngine/` 8 行；
⚠️「行」不是「处」—— 一行里可能覆盖多处，例如 `SettingsPanel.cs:128` 一行 = 3 根滑块的标签、`BattleDriver.cs:7521-7522` 一行 = 4 块注解）；
其中 **17 条原版侧有确凿 `mTerm`**（拿得到键名 + 英文原文 + 父链，可直接接 `Loc.T`），
**3 条上一轮的判定被本轮推翻**（见 §四），**1 处真缺陷**（中文档下 5 个键印英文，见 §二·C）,
**1 处结构隐患**（`EndPanel` 拿中文串当状态哨兵，见 §七·2）。
`RuleEngine/` 另有约 **940 条中文串**，逐条追过调用点后确认**全部是日志/诊断**（不上屏），只有 **3 处例外**。

### 搜索面声明（可复跑）

**A. 我们这一侧（清单①的来源）**
- 目录：`Unity/MyGame/Assets/CardPresentation/Battle/**`（37 个 `.cs`）· `Unity/MyGame/Assets/RuleEngine/**`（33 个 `.cs`）
- 工具：自写词法扫描器（逐字符走，**正确剥掉 `//` · `/* */` · `@"…"` · 字符字面量**，不信「按行 grep」）
  ⇒ 两族共 **15,282 条字符串字面量**；先按「含 CJK 或像人话」筛到 **7,980 条**，再按调用点分类。
- 判「面向玩家」的判据**只有一条**：它**最终落到玩家看得见的 UI 上**。逐条追了这几条落地路：
  `Battle/Label.Create/SetText` · `Shell/MenuDraw.Text/TextBox` · `Core/TmpFont.NewText` ·
  字段/常量初始化（`const string` / `static readonly string[]`）· `BattleDriver.SetHint`（HUD 提示行）·
  `BattleLogPanel.SetEntries` · `BattleDriver` 的 `notice`/`_deckNotice` · `Deck/DeckRuntime.Say` · `Shell/DeckInfoPopup`。
  ⇒ **排除**：`Debug.Log*` / `LogWarning` / `LogError` / 自检断言文案 / `Describe()`·`ToString()` 诊断串 / 资源路径 / shader 名 / 节点名。

**B. 原版那一侧（清单②的来源）—— 🔴 本轮**没有**只扫 `bundle_menus_assets_all`**
- 扫过：**`d:/2/新解包资源/assets_full/` 全部 91 个顶层条目**。
- 其中 **50 个含 `MonoBehaviour/` 目录**，全部逐文件读过（不是 grep 字符串，是 `json.loads` 后按
  `m_Script.m_PathID == "8610481073976370760"` 判 `I2.Loc.Localize`）
  ⇒ 命中 **1,518 个 `Localize` 实例**，去重后 **346 个唯一 `mTerm`**。
- **另外 41 个条目没有 `MonoBehaviour/` 目录**（因此天然不含 `Localize`，逐个列出来免得被读成"没扫"）：
  `bundle_Waprforge_monoscripts` · `bundle_Warpforge_unitybuiltinassets` · `bundle_shaders_assets_all` ·
  `bundle_videos_assets_all` · `bundle_audiocontrol_assets_all` · `bundle_menumusic_assets_all` ·
  `bundle_menusharedresources_assets_all` · `bundle_atlasgroup_assets_all` ·
  `bundle_atlasindividual_assets_0_mainmenu` · `bundle_atlasindividual_assets_40ktraiticonatlas` ·
  `bundle_atlasindividual_assets_battleatlasui` · `bundle_deckselectionbuttons_assets_all` ·
  `bundle_flavourframes_assets_all` · `bundle_alliancesbadgesimages_assets_all` · `bundle_armycursors_assets_all` ·
  `bundle_armyicons_assets_all` · `bundle_cosmeticavatarsimages_assets_all` · `bundle_cosmeticscardbacksimages_assets_all` ·
  `bundle_campaignrewardbackgrounds_assets_all` · `bundle_inboxmessagesheaderslocal_assets_all` ·
  `bundle_liveopsicons_assets_all` · `bundle_liveopsmenuimages_assets_all` · `bundle_rankeddivisionicons_assets_all` ·
  `bundle_specialofferscontentlocal_assets_all` · `bundle_uiwarlords_assets_all` · `bundle_tutorialwarlordchats_assets_all` ·
  **13 个 `bundle_*cardassets_assets_all`**（aeldarisaimhann / astramilitarum / chaosspacemarinesblacklegion /
  chaosspacemarinesemperorschildren / genestealercults / necronssautekh / orksgoff / sororitas /
  spacemarinesdarkangels / spacemarinespacewolves / spacemarinesultramarines / tauempire / tyranidsleviathan）·
  `sharedassets1` · `_stats.json`。
- **含词条的包（19 个，其余 31 个有 `MonoBehaviour/` 但 0 条）**：
  `menus`(**889**) · 13 个 `scenes_scenes_battlearena*`(**各 45**) · `scenes_scenes_mainmenuwarpforge`(30) ·
  `generalgamewindows`(9) · `battlesharedresources`(2) · `staticgeneralassets`(2) · `battleprefabs_vfxandmisc`(1)。
- **战斗侧的额外一道核（比"搜 mTerm"更硬）**：用 `工具/menu_rect.py` 的 `Bundle/find_rt/children/parent/chain_up`
  把 `bundle_scenes_scenes_battlearena1` 的**整棵树**走了一遍（`RectTransform.m_Father/m_Children` + `GameObject.m_Component`
  + `MonoBehaviour.m_text/mTerm`）⇒ **137 个有字或有词条的文本节点**，每一条都拿到了**父链**。
  ⇒ 「这一格原版印的是什么字、有没有 `Localize`」是**逐节点**核过的，不是靠词表反推。
- **关键词**（报"没有"之前逐个打过）：`Battle/`（全 19 条列出）· `SocialMenu/Alliances`（全 22 条列出）·
  `END TURN` `OVERTIME` `VICTORY` `DEFEAT` `Draw` `AI Difficulty` `Settings` `Cards left` `available`
  `Greetings` `TapToContinue` `Waiting` `Replace` `Select` `Continue` `AffectedBy` `Invalid` `Error` `Fail`
  `TransferLeadership` `ConfirmTransferLeadership`。

---

## 二、清单① — `Battle/` 的写死玩家可见文案（**35 行**：A 组 17 行有词条 · B 组 18 行无词条）

格式：`文件:行 | 字面量 | 面向玩家?（判据） | 原版有无词条 | 建议`

（`Battle/` 共 37 个 `.cs`；下面是**全部**落得到 UI 的写死串，其余字面量全是日志/资源路径/shader 名/节点名）

### A. 有确凿原版词条 —— 直接接（17 行）

| 文件:行 | 字面量 | 面向玩家?（怎么落地的） | 原版词条 + 父链 + TMP 原文 | 建议 |
|---|---|---|---|---|
| `BattleDriver.cs:10569` | `"OVERTIME!"` | ✅ `Label.Create` → `OvertimeSplashText` | **`Battle/Overtime/Title`** · `AboveShader < OvertimeSplashText < Text Container < Text` · TMP 逐字 `OVERTIME!` | **接** |
| `CardDisplayWindow.cs:338` | `TitleText = "受到以下影响："` | ✅ 效果清单标题 | **`Battle/HUD/AffectedBy`** · 根邻 `AffectedBy` · TMP 逐字 `Affected by:` | **接** |
| `ChoosePanel.cs:52` | `DefaultTitle = "选择一张牌"` | ✅ 选牌面板标题 | **`Battle/ChooseCard/Instructions`** · `ChooseCardMenu < ChooseText < Text` · TMP `Choose one card`（⚠️ 原版是 `键 + "-" + actingCardId`，空则回落无后缀那条，见 `ChoosePanel.cs:58` 注释） | **接** |
| `ChoosePanel.cs:54` | `ConfirmLabel = "继续"` | ✅ 确认钮 | **`Battle/Mulligan/ButtonDone`** · `ChooseCardMenu < ButtonsGroup < Continue Button < ContinueText` · TMP `Continue` | **接** |
| `ChoosePanel.cs:70` | `CardBtnWord = "Select"` | ✅ 每张牌下那颗钮 | **`Battle/Prebattle/SelectButton`** · `CardChooseCardButtonFrame < Generic Simplified UI Button_updated < Button Text` · TMP `Select`（包 = `bundle_battlesharedresources_assets_all`） | **接**（🔴 推翻上一轮结论，见 §四·1） |
| `MulliganPanel.cs:61` | `DoneLabel = "完成换牌"` | ✅ 完成钮 | **`Battle/Mulligan/ButtonDone`** · 同族两处（`ChooseCardMenu` 与 `battleprefabs` 的 `Change Card Button/Button Text`，后者 TMP 是西语占位 `Continuar`） | **接** |
| `MulliganPanel.cs:90` | `TurnSecond = "你后手"` | ✅ 换牌面板回合行 | **`Battle/Mulligan/secondTurn`** · `Mulligan < MulliganText < TurnText` · TMP `You go second` | **接** |
| `MulliganPanel.cs:164` | `"选择要换掉的牌（回车 = 完成）"` | ✅ `Label.Create` 提示 | **`Battle/Mulligan/Instructions`** · `Mulligan < MulliganText < Text` · TMP `Choose cards to replace in first hand` | **接** |
| `MulliganPanel.cs:285` | `"换"` | ✅ 每张牌那颗钮 | **`Battle/Mulligan/Replace`** · `bundle_battleprefabs_vfxandmisc_assets_all` 的 `ReplaceText` · TMP `Replace` | **接** |
| `MultiCardDisplay.cs:141` | `"继续"` | ✅ 底条钮 | **`Battle/Mulligan/ButtonDone`** · `Generic Multi Card Display Combat < BattleContinueButton < Text` · TMP `Continue` | **接** |
| `WaitBanner.cs:271` | `CardText.Phrase("WAITING FOR OPPONENT")` = `等待对手…` | ✅ 等待横幅 | **`Battle/Mulligan/WaitEnemy`** · `BackCanvas < WaitText < Generic Popup Background < Text` · TMP **`Waiting for enemy`** | **接**（🔴 上一轮记的"原版 `WaitText` 文案没查到"**已过期**，见 §四·2） |
| `SettingsPanel.cs:128` | `SliderNames = {Music, Sound Effects, Voice-overs}` | ✅ 三根滑块标签 | **`MainMenu/Settings/SettingLabel/Music`** · **`…/SettingLabel/SoundFx`** · **`Settings/Media/VoiceOvers`** · 父链 `BattleSettingsPanel < Volume Sliders < {Music,FX,Voiceover} Container < Text` · TMP 逐字同值 | **接（三条）** |
| `SettingsPanel.cs:474` | `"Resign"` | ✅ 投降钮 | **`Battle/Settings/ResignButton`** · `BattleSettingsPanel < Bottom buttons < Resign Button < Button Text` · TMP `Resign` | **接** |
| `TutorialOverlay.cs:258` | `"Skip tutorial"` | ✅ 跳过钮 | **`Battle/Settings/SkipTutorial`** · `BattleSettingsPanel < Bottom buttons < SkipTutorial Button < Button Text` | **接** |
| `TutorialOverlay.cs:310` | `"Continue"` | ✅ 提示条继续 | **`Battle/Tips/Continue`** · `Tutorial < TutorialTip < ContinueText` · TMP `Continue` | **接** |
| `BattleDriver.cs:7521-7522` | `CardText.Phrase("MELEE"/"RANGED"/"HEALTH"/"ENERGY")` | ✅ 教程四枚注解 | **`Battle/Tips/{MeleeAttack,RangeAttack,HealthPoints,EnergyCost}`** · `Card Display Window < Card Display < TutorialObjs < …` · TMP `Melee Attack / Ranged Attack / Health Points / Energy Cost` | **接**（⚠️ `Battle/Tips/EnergyCost` **已在 `Loc.cs:365`**，其余 3 条**没有**） |
| `TutorialOverlay.cs:336-337` | `_initText1/2`（初值空，运行时喂） | ✅ 教程开场两行 | **`TutorialData/Lesson/AttackLabel`** · **`TutorialData/Lesson/HealthLabel`** · `Tutorial < InitTutorialTip < TipText / TipText (1)`（TMP 原文本地是**空的**） | **接**（两行文字来自 `tutorial_stages.json`，词条只给键；文案仍取数据） |

### B. 原版**没有**词条 —— ⛔ 别编（按铁律 11 例外①保持现状）

| 文件:行 | 字面量 | 面向玩家? | 原版一侧怎么核的（关键词 + 位置） |
|---|---|---|---|
| `SettingsPanel.cs:425` | `"Settings"` | ✅ 面板标题 | `BattleSettingsPanel` 子树**没有标题文本节点**（全树 137 个文本节点里 0 命中）；`terms` 全表 346 条里**没有** Settings 标题键（关键词 `Settings` 命中 9 条，全是设置项不是标题） |
| `SettingsPanel.cs:490` | `"AI Difficulty"` | ✅（**我们自己加的**） | `BattleSettingsPanel` 子树里**没有这一行**（0 命中）；⚠️ **顺手发现**：原版战斗设置面板没有"AI 难度"这一项 |
| `ChatPopupPanel.cs:75` | `ButtonLabels = {Greet, Threat, Well Played, Taunt, Sorry, Oops}` | ✅ 6 颗聊天钮 | `Unit Chat < ChatPopup < ChatButtons < Buttons < ChatButton{,(1..5)} < Text (TMP)` —— **6 颗 TMP 全是 `Greetings`、6 颗全无 `Localize`**（运行时赋）⇒ 本地**零词条**；我们表里那 6 个词是**我们挑的**（文件头已如实标注） |
| `CardDisplayWindow.cs:195` | `"再点一下关闭"` | ✅ 提示行 | `Card Display Window` 子树**没有**"点一下关闭"这样的节点（0 命中）⇒ **我们自加** |
| `CardDisplayWindow.cs:369-378` | `AttrZh`: `近战/远程/生命/护甲` | ✅ 效果清单 | 原版那几句模板在**服务端** `GameStaticData.GetEffectDesc`（同文件 :344 注释已记，本地只有一条样例 `Get {0} Melee Attack, …`，见 `EffectText` 节点） |
| `MultiCardDisplay.cs:144` | `"点「继续」或再点一下牌堆关闭"` | ✅ 提示 | `Generic Multi Card Display Combat` 下只有 `Header Text`(TMP=`Header Text`，无词条) / `BattleContinueButton` / `CardUI Reference` / `Viewport` ⇒ 0 命中 |
| `EndPanel.cs:274-277` | `"{rounds} 回合   我方投降/对方投降"` · `"…敌方战将最低生命 {n}"` | ✅ 结算副标题 | `EndBattlePanel` 子树只有 `Quantity` / `RatingText` 两个文本节点（**没有任何标题/副标题节点**）⇒ 0 命中 |
| `BattleDriver.cs:11076/11118/11120` | `CardText.Phrase("HAND"/"DECK"/"DISC")` | ✅ HUD 计数 | 原版对应节点是 `CardsInHandText`(TMP **`Cards left: XX`**) 与 `Player Deck Size Tex`(TMP **`Cards left: 2/5`**)，**两颗都无 `Localize`** ⇒ 0 词条；🔴 **而且文案与我们不同**（见 §五·1） |
| `BattleDriver.cs:10035/11066` | `CardText.Phrase("END TURN"/"YOUR TURN"/"ENEMY TURN")` | ✅ 回合钮 / 回合行 | `TurnText`（`BackCanvas < RightArea < Right Anchor < Energy And turn holder < Clock < TurnBtn`）TMP = **`END TURN`**、**无 `Localize`** ⇒ 0 词条（我们的英文与之逐字一致 ✅） |
| `SkillPanel.cs:301/315` | `CardText.TargetsAvailable(n)` → `N available` | ✅ 技能面板 | `ActiveSkillDesc < TargetsAvailableText` TMP = **`0 available`**、**无 `Localize`** ⇒ 0 词条（英文与我们逐字一致 ✅） |
| `BattleDriver.cs:5237-5272` | 战斗日志模板 12 条（`我方/敌方` · `打出「」` · `进入格位 {n}` · `远程/近战攻击「」` · `回复 N 点生命` · `受到 N 点伤害` · `阵亡` · `离开格位（回手牌或牌库）` · `发动技能` · `触发「」` · `$"回合 {n}　…"`） | ✅ `BattleLogPanel`（`CemeteryLogPanel`） | `CemeteryLogPanel` 下 10 颗 `CemeterySliderUI < ActionText` —— **`m_text` 全是空的、0 颗挂 `Localize`**（运行时赋）⇒ 本地**零词条**（与 `W1` 那份「原版日志文案在远端」的判断一致） |
| `BattleDriver.cs:5752 / 5764 / 5770` | `$"本局用你编的「…」"` · `$"你的卡组「…」展开后只剩 N 张能上场"` · `$"…不合法（{DeckRules.Describe(err)}）"` | ✅ `notice` → `SetHint` 提示行 | 我们自造（走引擎校验结果）；原版对应文案在服务器 ⇒ 0 命中 |
| `BattleDriver.cs:257 / 672 / 3653 / 3664 / 3682 / 4890 / 5584` | `换牌完成，开打` · `已重连并追平` · `换牌中：点牌上的「换」…` · `换牌已提交，等主机定序…` · `$"换掉了 {n} 张"` · `选一张牌，然后点「继续」` / `选一项，然后点「继续」` · `联机局不能自己重开 —— 对面还在这一局里` | ✅ `SetHint`（`BattleDriver.cs:7078` → `_hintLabel`） | 我们自造（**联机/流程提示**，原版这类提示走服务器或根本没有）⇒ 0 命中 |
| `BattleDriver.cs:3662` | `"等待对手…"`（`SetDoneText`） | ✅ 换牌等待 | ⚠️ **这里不是原版那一格**：原版 `Battle/Mulligan/WaitEnemy` 挂在 `BackCanvas/WaitText`（= 我们的 `WaitBanner`），**不挂在换牌面板的完成钮上** ⇒ 这一处**没有对应词条** |
| `BattleDriver.cs:2600` | `$"卡组存档读不出来（…）—— 本局自动凑了一副"` | ✅ `_deckNotice` → `SetHint` | 我们自造 ⇒ 0 命中 |
| `BattleDriver.cs:3769 / 3790` | `"选择进攻卡"` / `"选择防御卡"` | ✅ `ChoosePanel.Open(...)` 标题 | 原版 `ChooseCardMenu` **只有一个标题对象** `ChooseText`（词条 `Battle/ChooseCard/Instructions`），**没有"进攻卡/防御卡"这两档**（`ChoosePanel.cs:58` 注释已记）⇒ 0 命中 |
| `BattleDriver.cs:5963` | `_multiCards.Show(cards, "你的牌库")` | ✅ `MultiCardDisplay` 头行 | `Generic Multi Card Display Combat < Header Text` TMP = `Header Text`、**无 Localize** ⇒ 0 命中 |
| `EndPanel.cs:265` + `:272/:293-294` | `"平局"/"胜利"/"失败"`（**又是状态哨兵**：`:272` 比 `r == "胜利"`、`:293-294` 比 `ResultText == "胜利"`） | ✅ 结算标题（走 `CardText.Phrase`） | `EndBattlePanel` 无标题节点（见上）⇒ 0 命中；🔴 **另有两个结构问题**，见 §七·2 |

### C. 走 `CardText.Phrase`（第二套本地化路径）—— 表里缺键的

`Battle/` 的 HUD 与提示**不走 `Loc.cs`**，走 `Core/CardText.cs` 的 `Phrases` 表（键 = 英文原文）。
`Battle/` 用到的键共 **20 个**，**表里齐 15 个**，**缺 5 个**（→ 中文档下原样印英文）：

| 键 | 用处（`Battle/` 侧调用点） | `Phrases` 表 | 结论 |
|---|---|---|---|
| `VICTORY` | `EndPanel.cs:272` 结算大标题 | ❌ **没有这个键** | 🔴 **中文档下印 `VICTORY`**（见 §七·2） |
| `DEFEAT` | `EndPanel.cs:272` | ❌ **没有这个键** | 🔴 同上 |
| `HEALTH` | `BattleDriver.cs:7522` 教程四块注解之一 | ❌ **没有这个键** | 🔴 中文档下印 `HEALTH`；⚠️ 且原版那一格是 `Health Points`（`Battle/Tips/HealthPoints`）⇒ **文案本身也不同** |
| `ENERGY` | `BattleDriver.cs:7522` | ❌ **没有这个键** | 🔴 同上；原版那一格是 `Energy Cost`（`Battle/Tips/EnergyCost`） |
| `ABILITY` | `BattleDriver.cs:6758`（`what = "ABILITY"`）→ `:6759 SetHint(...)` | ❌ **没有这个键** | 🔴 中文档下印 `ABILITY` |

（齐的 15 个：`END TURN` `YOUR TURN` `ENEMY TURN` `WAITING FOR OPPONENT` `GAME OVER` `YOU WIN` `YOU LOSE` `DRAW`
`HAND` `DECK` `DISC` `HP` `CHOOSE ACTION` `PICK A TARGET` `NO LEGAL TARGET` `MELEE` `RANGED`
`THIS UNIT ALREADY ACTED` `STUNNED` `THIS UNIT CANNOT ACT` —— ⚠️ 这里 20 条里有 5 条与上表重复计数，
**「齐 15 / 缺 5」是「用途去重后 20 个键」的口径**，逐键对照见上面那张表与这一段。）

> 🔴 **另一处需要订正的话**：`BattleDriver.cs:7518-7519` 的注写「教学标注四块的字（原版那四块是 TMP + 远端 I2 词条表，
> 本地没有 ⇒ 这四句是我们按卡面语义写的）」—— **「本地没有」这句不成立**：
> 四块**逐块找得到**（`Battle/Tips/MeleeAttack` / `RangeAttack` / `HealthPoints` / `EnergyCost`，
> 挂在 `Card Display Window < Card Display < TutorialObjs < UnitObjs` 的 `MeleeText` / `RangedText` / `HealthText` / `EnergyText`，
> TMP 原文 = `Melee Attack` / `Ranged Attack` / `Health Points` / `Energy Cost`）。
> ⇒ 同 `资料/已知的坑.md` #20 那一族（**只扫一个包 ⇒ 说成"本地没有"**）。

---

## 三、清单① — `RuleEngine/` 的写死玩家可见文案（**8 行**）

`RuleEngine/` 两族共 **1,005 条含 CJK 的字面量**（非 Editor）。逐条追调用点后：

🔴 **判据 = 引擎里只有一个上屏口** —— `BattleContext.Log(string)`（`:1248`）写的是 `Events`
（**诊断环形缓冲**，没有任何 UI 消费者）；真正喂给 `BattleLogPanel` 的是 `ActionLog`
（`List<BattleEvent>`，**结构化**，由 `BattleDriver.RefreshBattleLog:5228` 自己拼人话）。
⇒ **`RuleEngine/` 里那些 `Explain`/`Why`/`Describe` 中文串一条都上不了屏**（约 940 条，含
`EffectResolver.cs` 444 · `RuleCore.cs` 167 · `EffectText.cs` 118 · `TutorialScript.cs` 79 · `CreatePool.cs` 57 …）。

**只有 3 处例外（共 8 个字面量点）**：

| 文件:行 | 字面量 | 面向玩家?（落地路） | 原版有无词条 | 建议 |
|---|---|---|---|---|
| `Core/RuleCodes.cs:50-67` | `Names` 表 **16 条**中文（`手牌索引非法` `能量不足` `格位非法或被占` `不是你的回合` `该格没有单位` `该单位本回合已行动` `该单位没有攻击力或不能攻击` `不能攻击自己` `该单位处于眩晕` `目标不合法（Vanguard / Stealth / Flying 限制）` `该单位被压制，无法进行近战攻击` `该功能本版未实现` `该单位没有主动技能` `该单位的职责本局已经用过了` `该单位没有这条替代行动` `这一格不是可收集的路标石残骸`） | ✅ **上屏**：`BattleDriver.cs:6535` `SetHint(RuleCodes.Describe(rc))`（HUD 提示行） | ❌ 零命中（关键词 `Invalid` `Error` `Fail` 全表只命中 `MenuDeck/Error/InvalidDeckBannedCards` 1 条）—— 原版拒绝理由在服务器 | 接 `Loc`（键自拟，原版无） |
| `Core/RuleCodes.cs:72` | `"未知错误码 " + code` | ✅ 同上 | ❌ 零命中 | 同上 |
| `Core/DeckRules.cs:311-324` | `Describe(DeckError)` **13 条**中文（`卡组里有卡不在卡池中` … `这张是**效果生成的卡**（药剂/破坏/秘仪），不能放进卡组`） | ✅ **四处上屏**：`BattleDriver.cs:5770`(`notice`) · `Deck/DeckRuntime.cs:2441/3443`(`Say`) · `Shell/DeckInfoPopup.cs:575` · `Deck/DeckEditorState.cs:265` | ❌ 零命中（原版只有 `MenuDeck/Error/InvalidDeckBannedCards` = 禁卡那一条） | 接 `Loc`（键自拟） |
| `Core/DeckRules.cs:337` · `:386` | `"新卡组"`（默认卡组名） | ✅ 卡组列表里那一行 | ❌ 零命中 | 接 `Loc`（键自拟） |
| `Data/DeckLibrary.cs:77` | `"新卡组"`（`Create` 兜底名） | ✅ 同上 | ❌ 零命中 | 同上 |
| `Data/DeckLibrary.cs:110` | `" 副本"`（`Duplicate` 后缀） | ✅ 同上 | ❌ 零命中 | 同上（⚠️ 后置空格是**名字拼接**的一部分，接词条时别丢） |
| `Data/DeckLibrary.cs:296` · `:303` | `"导入的卡组"` | ✅ 同上 | ❌ 零命中 | 同上 |
| `Data/DeckStore.cs:55/64/71/90/116` | `"还没有存档"` `"存档解析失败（内容为空）"` `"未命名"` `"存档读取失败："` `"存档写入失败："` | ✅ **部分上屏**：`DeckLibrary.cs:50` 只把**含"失败"**的 note 留给 `LastError`，而 `LastError` 被 `Shell/Deck/DeckRuntime` 念出来（`DeckRuntime.cs:2617` 注释 + `:2974` 的底部报错横幅）⇒ `:64/:90/:116` 上屏；`:55` 不上屏；`:71` 是**名字兜底**（上屏） | ❌ 零命中 | 接 `Loc`（键自拟）；⚠️ `DeckLibrary.cs:50` 那个 `note.Contains("失败")` **是按中文子串判的**，接词条时会一起坏掉（见 §五·3） |

---

## 四、🔴 上一轮（W1）被本轮推翻 / 需要订正的三条

1. **`ChoosePanel.CardBtnWord = "Select"` —— 原来记「中文词条查不到（I2 表本地没有）⇒ 留英文」**。
   **实际**：键**就在本地** —— `Battle/Prebattle/SelectButton`，挂在
   `bundle_battlesharedresources_assets_all` 的 `CardChooseCardButtonFrame < Generic Simplified UI Button_updated < Button Text`，
   TMP 原文逐字 `Select`。**上一轮只是没扫 `menus` 之外的包**（正是 D 类）。
   ⇒ 归「**有键、可接**」，不再是「原版没有」。

2. **`WaitBanner` 那条注（`Battle/WaitBanner.cs:16` 与 `Core/CardText.cs:328-331` 附近）写「原版 `WaitText` 的文案没查到
   （dump 里那个 `Text` 节点是空的、本地化 key 也没解出来）⇒ 这条是我们加的，不是复刻」**。
   **实际**：节点**有字也**有键 —— `BackCanvas < WaitText < Generic Popup Background < Text`，
   TMP = **`Waiting for enemy`**，`mTerm = Battle/Mulligan/WaitEnemy`。
   ⇒ 那句话要按铁律 5 就地订正成「**原版文案 = `Waiting for enemy`，键 = `Battle/Mulligan/WaitEnemy`**」。
   （我们表里那条键值 `等待对手…` 的中文仍是我们译的 —— **中文那一列的来源口径不变**。）

3. **`MulliganPanel.DoneLabel` / `ChoosePanel.ConfirmLabel` 的注写「词条内容本地没有」**。
   **实际**：键在本地、**TMP 原文也在本地**（`ChooseCardMenu/ButtonsGroup/Continue Button/ContinueText` = `Continue`；
   `battleprefabs` 的 `Change Card Button/Button Text` = 西语占位 `Continuar`）。
   ⇒ 「**取不到译文**」≠「**连键名/英文原文都取不到**」（同 `资料/已知的坑.md` #20 那一族）。

---

## 五、③ 点名待查：`SocialMenu/Alliances/TransferLeadership`

**结论：本地【没有】任何资产挂这条键；键名本身本地【读得到】；文案（英/中）只可能在远端 I2 表。**

**搜过的面（逐条打出）：**
1. `d:/2/新解包资源/assets_full/` **全部 91 个顶层条目 / 50 个有 `MonoBehaviour/` 的包**，
   按 `Localize`（`m_Script.m_PathID == 8610481073976370760`）逐文件 `json.loads` 判过（1,518 个实例 / 346 个唯一 term）
   ⇒ **`TransferLeadership` 零命中**（同时 `ConfirmTransferLeadership` 也零命中）。
2. 全库按**字节**搜 `TransferLeadership`（`assets_full` 全部文件，不只 `MonoBehaviour`）⇒ **零命中**。
3. `d:/2/解包整理/`（旧解包）⇒ **零命中**。
4. `d:/2/tools/decomp_full/`（26,282 个方法体）⇒ **零命中**（字符串常量在 `.rdata`，不在方法体里，属预期）。
5. ✅ **`d:/2/tools/il2cpp_out/stringliteral.json` 命中 2 处**（这就是"读得到键名"的判据）：
   - `0x4253CB0` = **`SocialMenu/Alliances/TransferLeadership`**（RVA = 0x184253CB0）
   - `0x42530B0` = **`SocialMenu/Alliances/ConfirmTransferLeadership`**（同一条链上的**第二个键**，上一轮没记）
   - 相邻的 `0x4253AB0` = `SocialMenu/Alliances/Promote`（**地址逐条并排，确认不是串位**）
   同一批还读出 `SocialMenu/Alliances/{Badges/, CancelInvitation, InvitePlayer, Join, Language/, Members,
   Messages/…, Privacy/…, Role/, Trophies/…}` —— 这些**全是代码拼出来的键前缀**。
6. `d:/2/tools/il2cpp_out/script.json` · `global-metadata.dat`（两份运行时目录）· `d:/2/tools/all_strings.txt`
   ⇒ 只有这三处 + `stringliteral.json` 命中（都是**二进制/元数据**，**不是** prefab 资产）。

**为什么 prefab 上找不到（机制，不是"没扫到"）**：
`SocialMenu/Alliances/TransferLeadership` 是一个**运行期赋值的键**，不是出厂值。
`AllianceMemberOptionsPopup.cs:25-40` 已经记下了原版那条链：`Open()` 时按 `role` 决定
`role == 2 (Admin) ⇒ TransferLeadership`、否则 `⇒ Promote`，把结果写进同一颗 `Localize.Term`。
⇒ **出厂 prefab 的 `mTerm` 永远是 `Promote`**（我们实读：那颗 `Localize` 在 prefab 里还是 `m_Enabled = 0`），
**`TransferLeadership` 在磁盘上不可能出现在任何 prefab**。这就是「按键名扫全库也扫不到」的**根本原因**。

**要接的话落点（判据齐）：**
- 键名常量已有：`Shell/AllianceMemberOptionsPopup.cs:157-158`（`TermPromote` / `TermTransferLeadership`）。
- **要改的那一处**：`Shell/AllianceMemberOptionsPopup.cs:437-444` —— 现在它 `role == 2` 时**只 `Debug.Log` 不换字**；
  接的话把 `_btnTexts[IPromote].SetText(...)` 换到 `Loc.T(TermTransferLeadership)`。
- 🔴 **但"接"要先把 `Loc.cs` 里那两条键的**内容**补上，而内容本地取不到**：
  `SocialMenu/Alliances/TransferLeadership` 的英文原文与中文**两边都没有**（与上一轮那三颗西语占位同族，
  但更糟 —— 那三颗至少有 `Degradar`/`Expulsar Jugador`/`Abandonar Alianza` 这种出厂占位串可留档，
  这一条连出厂串都没有）。⇒ 落到铁律 11 的**第①种情形（判据是空的）**：**键名照抄、文案必须标成"我们自拟"**，
  并写明「原版这一条的英文取自远端 I2 表、本地零命中（搜过 91 个顶层条目 / 50 个含 MonoBehaviour 的包 / 2 份元数据）」。
- ⚠️ 同时**新发现**：`SocialMenu/Alliances/ConfirmTransferLeadership`（`0x42530B0`）也是**同类**——
  **本地零资产**。它多半是那条链上"确认弹窗"的标题。**建议一并记成待办**（判据同上一段）。

**顺带核对的上一轮数字**：`SocialMenu/Alliances*` 前缀的唯一 term 实测 **22 条**（上一轮记 21，
差的那条是 **`SocialMenu/Alliances` 本身**—— 无尾斜杠，被 `…/*` 那个写法漏掉了）。

---

## 六、没查清的部分（判据不足，如实记）

1. **上一轮把「西语占位」当"原版没给词条"的那三颗**（`Demote` / `Kick` / `Quit`）—— 本轮**复核了键**
   （三条 term 都在、都挂在 `Button Text < {Demote,Kick,Quit} < Buttons < Member Options Panel`），
   也复核了 TMP（`Degradar` / `Expulsar Jugador` / `Abandonar Alianza`，**确实是西语**）。
   ⇒ 「英文原文取不到」**成立**。但**为什么偏偏这三颗是西语**、原版是不是有别的英文源（比如远端表里另有条目）
   **没查清** —— 要拿到远端 I2 表才能钉死。
2. **`Battle/Tips/*` 那四条与 `Battle/Settings/*` 两条的"同一键挂几颗"** —— 本轮只在 `battlearena1` 逐节点核过，
   **其余 12 个战场场景没逐节点核**（只做了 `mTerm` 计数：**13 个 arena 每个 45 条**，与本场一致）。
   要"13 场逐一"再核一遍才敢说"逐场相同"。
3. **`battlearena1` 之外那 12 个场景的 `EndBattlePanel` 子树**没逐节点核（只核了 arena1；
   13 场的 `EndBattlePanel` 名字都在，但子树未逐个比对）。
4. **中文那一列的来源**：本轮**没有**碰"中文从哪来"这个问题 —— `Loc.cs` 现有的中文列是自译的，
   本轮只补了"原版有没有键/键在哪/英文原文是什么"，**没有**给出任何中文译文（避免与 `W1` 的口径打架）。

---

## 七、顺手发现（本轮新查实，⛔ 未做 —— 由主对话分流）

1. 🔴 **HUD 的牌库/手牌计数文案与原版【对不上】**（不是词条问题，是复刻偏差）：
   我们 `_handLabel` = `HAND {n}`、`_pileLabel` = `DECK {n}  DISC {m}`
   （`BattleDriver.cs:11076/11118/11120`），而原版那两颗节点的 TMP 原文是
   **`Cards left: XX`**（`CardsInHandText`）与 **`Cards left: 2/5`**（`Player Deck Size Tex`，
   `BackCanvas < RightArea < PlayerDeck < Player Deck Size Container`）。两颗都**无 `Localize`**。
   ⇒ 这是**文案层面的偏差**（我们自造了三段式），**与新开一条待办**。

2. 🔴 **`EndPanel` 把中文串当状态哨兵**（`EndPanel.cs:265` 产 `"胜利"/"失败"/"平局"`，
   `:272` 与 `:293-294` 拿 `==` 去比）。两个后果：
   ① **一旦这些串走 `Loc.T`（中英切换）那些 `==` 比较会全部失效**（英文档下标题与门开视频都判错）；
   ② 顺带确认 **`CardText.Phrase("VICTORY"/"DEFEAT")` 的键在 `Phrases` 表里根本不存在**
   ⇒ 现在**中文档下结算大标题印的是英文 `VICTORY` / `DEFEAT`**（这是个**看得见的缺陷**）。
   ⇒ 要做：状态用**枚举**（不是字符串），标题另找文案来源（原版 `EndBattlePanel` **没有标题节点** —— 胜负靠 `Video Image` 播视频）。

3. 🔴 **`DeckLibrary.cs:50` 的 `note.Contains("失败")` 是"按中文子串判类型"** ——
   一旦 `DeckStore` 那几条 note 走 `Loc`（中英切换），**英文档下这个 `Contains` 恒 false**
   ⇒ `LastError` 永远是 null ⇒ 底部报错横幅**静默消失**。接词条时**必须同批改**（改判据 = 让 `DeckStore`
   直接返回一个错误码/枚举，别回字符串）。

4. 🔴 **`Net/NetBattle.cs` 有 8 处中文硬串也落在同一个 HUD 提示行上**（`NetSay` → `BattleDriver.cs:218` → `SetHint`）：
   `:223` `"联机对局中止：" + why` · `:567` `$"对手掉线了 —— 正在等他回来（{sec} 秒后判他弃权）"` ·
   `:571-…` `ReconnectPopup` 那句 · `:681` `$"重连没成功（超过 {sec} 秒）—— 这一局判你负"` ·
   `:693` `$"对手掉线超过 {sec} 秒还没回来 —— 判他弃权，这一局你赢了"`。
   ⇒ **不在 `Battle/` + `RuleEngine/` 这个搜索面里**，但**判据是同一块屏**（`_hintLabel`）⇒ 记一笔。
   （`Net/` 的其余 `NetRuntime.Notice` 通道走的是 Shell 侧，本轮**没扫**。）

5. ⚠️ **`Core/Tooltip.cs` 的 `TipText`（战斗里悬停卡面四个数值/关键词时弹的那张表）也是"我们写的"**，
   而且它**在 `Core/`、不在 `Battle/`** —— 文件头已如实标注（原版走 `Tips/MeleeAttackTip` 一族 I2 key，本地零 value）。
   本轮把它并进搜索面看一眼：**结论与它自己的标注一致**（`TipText` 那股键在原版 prefab 的触发器 `text` 字段里，
   本地无表、无 value）。⚠️ 但 `Tips/MeleeAttackTip` 这类 key **在本轮 346 条 term 里 0 命中** ——
   因为它们是 `EverguildTooltipItem` 触发器**字段里的普通字符串**，不是 `Localize.mTerm`
   ⇒ **「按 mTerm 扫」这一类表对它们是无效否定**（同 `已知的坑.md` #20 那一族的第四种载体）。

6. ⚠️ **`Battle/` 里唯一的"第二套本地化路径"**：HUD/提示走 `Core/CardText.cs` 的 `Phrases`（键 = 英文原文），
   窗口类走 `Core/Loc.cs` 的 `Loc.T`（键 = I2 `mTerm`）。**两条路互不相通**：
   `Loc.cs` 里只有 **1 条 `Battle/` 键**（`Battle/Tips/EnergyCost`，`Loc.cs:365`），而它在 `Battle/` 里的
   消费点**是 `CardText.Phrase("ENERGY")`、根本不走那条键**。
   ⇒ 这是「**两处写同一条规则 = 迟早不一致**」的一个现存实例，接词条前必须先定"战斗侧走哪条路"。

---
（本文件由只读普查代理 `S_D` 产出；所有判据均给了 `文件:行` / 资产路径 / 父链。⛔ 本轮一个字都没改。）
