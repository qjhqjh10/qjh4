# W_B6 · 教程牌数据（12 副进运行时）+ 教程关卡执行器 / 教程局规则 查证

> 2026-10-17 · 写手代理 B6。白名单内**改 1 个**（`Unity/工具/gen_prebuilt_decks.py`）+ **重跑产物 1 个**（`…/Resources/prebuilt_decks.json`，**逐字节未变**）
> + **新建 2 个**（`…/Resources/tutorial_decks.json` 与它的 `.meta`）+ 本报告。⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改正本。

## ① 12 副教程预组牌已进运行时 ✅（做法 = **另开一个产物文件**，`prebuilt_decks.json` 一个字节都不动）

- 新产物 `Assets/RuleEngine/Resources/tutorial_decks.json`（**15843 B** / LF）+ `.meta`（guid `018e64e3316981d26018e8a2ef9f7c7e`，核过全工程 10713 条 guid 无重）。
  结构 `{format, note, pool, roleSource, incomplete, heroMismatch, knownIssues, stageCount:6, stages:[{stage, player:{…deck…}, ai:{…deck…}}×6]}`；
  **deck 对象与 `prebuilt_decks.json` 逐字段同形** ⇒ `Shell/PrebuiltDecks.cs` 的 `Deck` 类可原样复用（只多一个 `so` = 原版 SO 资产名，当坐标）。已机检：**只含 string/int/bool/数组、无字典无 null**（JsonUtility 读得动）。
- 生成器：原 `main()` 里逐张解析那段抽成**共用** `make_entry(d, ctx, kind)`（**产物形状只此一处**）—— 预组池 `kind="practice"`、教程 `kind="tutorial"`；新增 `build_tutorial()`。
  教程那一路**不卡张数、不卡「同名 ≤2」**（正本 `资料/预组卡组_原版规格.md:286`「破格的 5 副全是教程副」；原版自己 20/25/30 张不等），`complete` = 卡全解析出 + 督军解析出；**不带防御卡**
  （原版 `CardDeck(PrebuiltDeck,…)` 的 `defensiveCard` 恒 0，同上 `:350` —— 池那一路的防御卡是**我们加的**、用户 09-26 拍板，教程这路不加、三格留空串）。
- **为什么另开文件、不加标记**：① **原版自己就是两条路** —— `PrebuiltDeckCollection__AddDeck.c:21-25`（名字含 `Tutorial` → `return`）教程 12 副**从不进任何集合**，只由 `TutorialStage.playerDeck/aiDeck` 直引（`MatchData__GetBotDeck.c` 的 `MatchType 100/0x8c` 分支 → `currentTutorialStage + 0x40`）；② 硬约束要的**最硬证据形式是「逐字节为 0」**，同文件加标记就说不清（连 `note`/`pool` 那两句都不能改）。
- **哪副是玩家/敌方**（**不是按名字猜**）：6 个关卡 SO 的 `playerDeck`/`aiDeck` 两个 PPtr（`{m_FileID:2, m_PathID:…}`）—— 用 UnityPy 开 `…/aa/StandaloneWindows64/prebuiltdecks_assets_all.bundle`（单 CAB `CAB-dbb6f14c119af496b058533ae98dc790`）**12/12 全解出**，写进生成器 `TUT_ROLE`（pid 一并抄在注释里）。
  🟢 **独立交叉验证**：`bundle_menus_assets_all` 的 6 个 `Demo DeckInfo * Tutorial` 的 `classicDeck` pid 与 `TutorialStage{n}.playerDeck` **逐关相同**、`tutorialIndex` = 0..5（`DemoDeckInfoSO + 0x50`）。
  侧证：`MatchData__ShouldShuffleDeck.c` 对 100/140 返 0（不洗牌），12 副 `m_Script` 实测全是 `PrebuiltSortedDeck`（`146420486734122931`）；且**实测 236/236 副**的 `decklists.json` 卡序 **== 原版 SO 的 `cardLibraryIds` 序**（被重排过的是 `prebuilt_decks_full.json`）⇒ 我们只过滤、不重排。

| 关 | 玩家副（原版 SO / deckId / 阵营 / 督军 / 张数·命中 / 齐） | 敌方副（同列） |
|---|---|---|
| 1 | `Ultramarines_Deck0_Tutorial1_Uriel` / UMTutorial / **EmperorsChildren ⚠️** / **无 ⚠️** / 30·7 / ✗ | `…Tutorial1_Marneus` / UMTutorial / Ultramarines / UM1 Marneus Calgar / 30·0 / ✗ |
| 2 | `Orks_Deck0_Tutorial2_Ghazghkull` / OrksTutorial / Goff / GOF3 Ghazghkull Thraka / 25·24 / ✗ | `Ultramarines_Deck0_Tutorial2_Uriel` / UMTutorial / Ultramarines / UM3 Uriel Ventris / 20·8 / ✗ |
| 3 | `Sautekh_Deck0_Tutorial3_Zahndrekh` / NecronsTutorial / Sautekh / SAU3 Nemesor Zahndrekh / 30·28 / ✗ | `Sautekh_Deck0_Tutorial3_Orikan` / NecronsTutorial / Sautekh / SAU5 Orikan the Diviner / 30·28 / ✗ |
| 4 | `BlackLegion_Deck0_Tutorial4_Sylar` / BlackLegionDeckTutorial / BlackLegion / BL5 Sylar Hexcorn / 30·26 / ✗ | `Ultramarines_Deck0_Tutorial4_Varro` / BlackLegionTutorial / Ultramarines / UM5 Varro Tigurius / 30·14 / ✗ |
| 5 | `Aeldari_Deck0_Tutorial5_Ghaelyn` / AeldariDeckTutorial / SaimHann / **ASH3 ⚠️** / 30·27 / ✗ | `Sautekh_Deck0_Tutorial5_Imotekh` / NecronsTutorial / Sautekh / SAU1 Imotekh the Stormlord / 30·30 / ✅ |
| 6 | `Leviathan_Deck0_Tutorial6_Tervigon` / LeviathanDeckTutorial / Leviathan / TL5 Tervigon / 30·30 / ✅ | `Ultramarines_Deck0_Tutorial6_Uriel` / UMTutorial / Ultramarines / UM3 Uriel Ventris / 30·15 / ✗ |

- 「只有 2 副拼齐」**不是本脚本的锅**：缺的是**原版 id 表里根本没有的卡号**（`UM6/8/15/19/20/21/25/26/33/36/38…` 这类「连续编号里的洞」+ 32 位 GUID 卡 `eb815e1e…`/`d0ac7c25…`/`847acecf…`）—— 逐条查过 `card_ids.json`/`card_stats.json`/`cards.json`/`gems_rarity.json`/`deck_btn_map.json`/`prebuilt_decks_full.json`，**一个都没有**（与 `check_prebuilt_decks.py` 文件头同结论）⇒ ⛔ 没补牌、没猜名，如实写 `missing`。
- ⚠️ **两个数据缺陷**（生成器每跑一次自动报一次，写进产物 `knownIssues`，不靠人记）：
  1. **S1 player（Uriel）督军解不出**：`deckHero.targetId` = GUID `ec0d25faeb266ea4eaec41ae72b8d8ad`，`warlord_ids.json` 明写它是 `UM_Warlord_Uriel Ventris`；但 `decklists.json` 这条的 **`faction` 标成了 `EmperorsChildren`**（`heroPortrait` 还写着 Lucius）⇒ `hero_match` 去 EC 表里找 → `对不上` → `heroId=""`（**改按 Ultramarines 就解出 UM3**，实测）。`decklists.json` **不在白名单**，没动。
  2. **S5 player 督军对不上**：SO 尾段 `Ghaelyn` · `warlord_ids.json[ASH3]='ASH_Warlord_Medreyal Ghaelyn'` · 但 `card_ids.json[ASH3]='Eliac Zephyrblade'`（我们池 `ASH3` 也是 Eliac；Ghaelyn 在池里是 **`ASH5`**）⇒ 解出 Eliac。12 副里**只此一副**对不上、其余 11 副卡名都含 SO 尾段 ⇒ 极可能 `card_ids.json`/池的 `ASH3` 标错，判据不足 ⛔ 没替它挑。（W_B2 建窗按 SO 名推的是 **ASH5**，与本产物不一致 —— **以谁为准请调度台裁**。）

## ② `tutorial_stages.json` 结构 + 原版谁消费它

**结构**：`{Stage1..Stage6}`，每关 `{steps:[…]}`；step = `{turn:"1"(串), name:"Player 1"/"AI 1", is_player:bool, tips:[富文本], actions:[动作名]}`。规模 6 关 / 79 回合 / 61 tip。**全工程 0 个消费者**（B2 已核）。
🔴 **它是【派生快照】不是原版规格**（出处 `资料/说明书/01_战斗_对战/教程流程.md:8-9`：Godot 原型 `build_tutorial_data.py` 洗掉 `<link>`/`<sprite>`/`<br>` 后写进 `D:/warpforge/data/`）。**实测它与 SO 的差**：
`actions[]` **恰好等于 SO `scriptedActions` 里 `playerAction==true` 的那些**（按原序，**79/79 回合逐条相等**）；SO 共 **430** 动作、派生只留 **152**（丢 `SmallTip 61`·`PlayerChat 31`·`AiChat 27`·`DrawCard 9`·`ChangeToMelee 8`·`ActivateHandCards 6`·`RadioMessage 5`·`PlayerChoice 2` 以及全部 AI 动作）；`tips[]` 把 `SmallTip` 文本单独抽出来 ⇒ **「第几条 tip 在第几个动作之前」这层时序没了**；`smallTipParams`（位置/箭头/时长/waitForTip…）· `waitBefore/waitAfter` · `sound` · `shouldHighlightElement` · `scriptedActionData`（谁打谁）**全没了**。
⇒ **下一批的数据源必须是原版 SO**（`assets_full/bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1..6}.json`）；这份只能当「玩家该做什么」的速查表。

**原版逐跳（判据 = `d:/2/tools/decomp_full/`）**：
1. **点 `Play`** → `TutorialModePopup__BattleButtonOnClick`：`selectedDeck`(`DemoDeckInfoSO`) 的 **`+0x50` = `tutorialIndex`** 写进 `PlayerDataManager + 0x3a0`；`GetDeck()` 取 `classicDeck` 建 `CardDeck` → `CreatePlayerBattleData` → **`MatchMakerManager.StartMatch(PlayModes.Tutorial=4, …)`**（`((idx>>31)&2)+4`，idx≥0 ⇒ 恒 4）。
2. **开局**：`matchType = MatchType.Tutorial(100)` ⇒ `IsTutorialMatch`（也认 `TutorialReplay 140`）；AI 牌 = **`currentTutorialStage + 0x40`**（`MatchData__GetBotDeck.c`）；`currentTutorialStage` 是**运行期 Addressables 加载**：`PlayerDataManager__get_currentTutorialStage.c` = `tutorialStages[+0x3a0]`（`PlayerDataManager.cs:765` `AssetReferenceTyped<TutorialStage>[]`）。
3. **建牌** `BattleManager__CreatePlayerDeck.c`：`ShouldShuffleDeck(100)`=false ⇒ **不洗** → 把 `stage+0xa0`(敌)/`+0x98`(玩家) 的 `*StartingTroopsInHand` 插到**牌库顶**；**督军落场** `BattleManager._TutorialStartSequence_d__592__MoveNext.c`：等 → 玩家督军 `SetActive(true)`+`HeroLandIntoField` → 等 → 敌方同 → 相机 `Initialize` → **逐条跑 `stage+0x50` = `preMulliganScriptedActions`**（S1 是 4 句开打前对白）。
4. **站位/换牌**：`SetupStartingTroops`(`+0x88/0x90` 场上单位) · `SetupInitialMana`(`+0xa8..0xb4`，**6 关全 0**) · `GetPlayerGoesFirst` → `+0x28` = **`playerStarts`**（S3=0 ⇒ **AI 先手**，与派生文件 Stage3 第一步是 `AI 1` 吻合）；**每回合** `BattleFinished` → 回合计数 `+1` → **`AiScripted.UpdateTurn`**（回合变则 `actionCounter=0`）→ `GetCurrentTurnScriptedData()` = `stage+0x48`(`turnScriptedData`)[turn-1] → `ExecuteScriptedTurn` 协程 → `AiScripted.PlayScriptedTurn` **逐条** `ExecuteAction(scriptedActions[actionCounter])` 再 `actionCounter++`；**胜负后**取 `stage+0x58`(胜)/`+0x68`(负) 继续跑。

## ③ 教程局 vs 普通局（**「有这条机制」≠「用上了」**）

| 项 | 值 / 判据（`d:/2/tools/decomp_full/` 与 `Scripts/Assembly-CSharp/`） |
|---|---|
| 模式 | `MatchType.Tutorial 100` / `TutorialReplay 140`；`PlayModes.Tutorial 4` —— `MatchType.cs`/`PlayModes.cs` · `BattleManager__IsTutorialMatch.c` |
| 对局参数 SO | **`TutorialScenario.json` 的类 = `ScenarioVariables`**：`startingMana 1 · startingHand 3 · maxMana 10 · maxCardsInHand 10 · questPointsForTrigger 3/4 · clock 1000/1000/10` —— `BattleManager__get_scenarioVariables.c`（教程返 `+0x68`）+ `MonoScript_-5845469671781466849.json` |
| **不洗牌** | 100/140 ⇒ `ShouldShuffleDeck`=0（`MatchData__ShouldShuffleDeck.c`）；12 副全是 `PrebuiltSortedDeck` |
| **玩家只能做「当前脚本动作」** | `CheckIfPlayerActionPermittedInTutorial`：`scriptedTurn` 真时看 `scriptedActions[actionCounter].playerAction`；为假 ⇒ 只有 `actionType ∈ {PlayerChoice 70, ActivateHandCards 170}` 放行，否则逐条 `ScriptedActionData.CheckIfMatchesActionData(...)`（谁打谁）比；不匹配 ⇒ **拒绝** |
| UI 屏蔽 | `hideCardsLeftInDeck=1 · hideCemetery=0 · hideLargeCardDisplay=0 · hideChat=1`（**6 关全同**）⇒ 牌库剩余数**不显示**、墓地/大图显示、聊天关 —— `…CanDisplay{DeckSize,Cemetery,LargeCard,Chat}InTutorial.c` 读 `stage+0x2a/0x29/0x2b/0x2c`（0 才显示）+ 逐关实读 |
| `playerAlwaysWins` | 机制在（`GetWinnerAfterBattleEnd.c:63-66`：真 ⇒ `LogWarning` + 直接判 `BattleWinner.Player = 10`）—— **但 6 关实测全是 0（关着）** |
| `preventPlayerResign` | 真 ⇒ 点退出**不判死**、改跑 `actionOnPlayerResign`（`ClickExitBattle.c:57-71`，读 `+0xb9`/`+0xc0`）；**6 关实测全是 0** |
| 起手 + 先手 | 场上 `*StartingTroops`（**只 S5 敌方有 2 个**）+ 手牌 `*StartingTroopsInHand`（S3 4/3·S4 3/4·S5 3/4·S6 3/4；S1/S2 空）；`playerStarts` = **S3 为 0（AI 先手）**、其余 1 —— 逐关实读 + `SetupStartingTroops.c`/`CreatePlayerDeck.c`/`GetPlayerGoesFirst.c` |
| 胜负后 + 关卡内前后 | `onVictory` 每关 2 句（S5 是 3 句）· `onDefeat` **6 关全空** · `skipNormalBattleEnd*` 全 0 · `preMulliganScriptedActions` = 3~4 句对白 · **无** `playerTurnEndEnchantment` —— `BattleFinished.c:95-111` + 逐关实读 |
| 动作全集 | `ScriptedActionType` 22 项（DrawCard 10·PlayCard 20·Attack 30/31·ChangeToRanged/Melee 35/36·ActiveAbility 40·PlayerChat 50/55·AiChat 60/65·PlayerChoice 70·SmallTip 80·RadioMessage 90·EndTurn 100·ClickCard 110·TapCard 120·ShowManaAura 130·ResolveCard 140·ContinueSmallTip 150·LandWarlords 160·ActivateHandCards 170）；**6 关实测只用到 14 种**（430 条） |

🔴 **没查到（如实标，没编）**：① SO 里 `actionType` 是**带参数字符串**（`"SmallTip (文本)"`/`"AttackFreeMode (Player warlord)"`），**参数那半怎么解析成 `actingUnit/targetUnit` 没读完**；② `AiScripted.ExecuteAction`（1281 行）22 个分支各自的表现细节（播什么动画/等多久）**没读**；③ 教程文案的**本地化表在远端**（`textReference` = `Tutorial1/Turn1/Tip1`）⇒ 只能沿用 SO 内嵌的英文。

## ④ 下一批（教程战斗）施工草图（一件一个写手）

1. **【数据】新产物 `Resources/tutorial_stages_engine.json`**：从 6 个 SO 抽 `playerStarts` / `hide*` / `playerAlwaysWins` / `preventPlayerResign` / `startingMana…`（6 关全 0）/ 起始单位与手牌（`RawCardScript` 要解成我们的卡 id）/ `preMulligan`+`onVictory`+`onDefeat` 动作 / `turnScriptedData`。**与 `gen_prebuilt_decks.py` 同源**（它已有 `TUT_ROLE` + 读 SO 的路子）。
2. **【引擎】`MatchType.Tutorial`**：不洗牌 · 先手读关卡 · 开局放起始单位/手牌 · `playerAlwaysWins`/`preventPlayerResign` 两条闸门（6 关虽都关着**也要照做** —— 铁律 11）。
3. **【执行器】`TutorialScript` 单指针状态机**：`turn → scriptedActions[actionCounter]`（`PlayScriptedTurn`/`PlayNextScriptedAction`/`UpdateTurn`/`PlayerChoiceAction`/`FinishedScriptedActionsInTurn` 五个方法就是全部骨架）+ **玩家动作白名单闸门**（③ 那套）。先做能跑通 S1~S6 的 14 种，缺的**出声**。
4. **【表现】提示 UI**：`TutorialTipScript`+`TutorialPointer`（`PositionReference`/`PositionRelation` 两张表在 `资料/教程线_原版规格与资源存量.md` §一，**照抄别写死坐标**）+ `Tutorial highlight`。
5. **【接线】教程窗 `Play`** → 用 `tutorial_decks.json` 的 `stages[stage-1].player/ai` 建局，删掉现在那句「如实出声」。⚠️ **S1/S5 两个督军缺陷先由调度台裁**，别硬顶着开。

## ⑤ 跑过的自检（原样贴命令与输出；**唯一标了「中略」的那 10 行**是逐副明细，已压成 1 行）

```
$ python d:/4/Unity/工具/gen_prebuilt_decks.py
wrote d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json
practice=103 complete=66 classic=49 classic_complete=29 no_cardback=0 hero_missing=7
wrote d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_decks.json
tutorial stages=6 decks=12 complete=2
   （中略 10 行逐副明细 = 上表「张数·命中」列：7/0 · 24/8 · 28/28 · 26/14 · 27/30 · 15）
tutorial heroMismatch = 2
   stage1 player：SO `Ultramarines_Deck0_Tutorial1_Uriel` 尾段「Uriel」，但解出的督军是「（没解出）」（decklists heroId=（空）· matcher 注=对不上）
   stage5 player：SO `Aeldari_Deck0_Tutorial5_Ghaelyn` 尾段「Ghaelyn」，但解出的督军是「Eliac Zephyrblade」（decklists heroId=ASH3 · matcher 注=id命中）
$ md5sum Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json
5097f3e89ce7288485961d2a70a6de9d          # 改前 = 改后，同一个 md5
$ git diff --numstat -- Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json
                                           # ← **空**。预组页那 66 副（103 副里 complete 的）逐字节未变
$ git diff --numstat -- Unity/工具/gen_prebuilt_decks.py
283  67
$ "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/check_prebuilt_decks.py
WARN: … 里有 4 个 2026-09-24 手工追加的节 … 这次覆盖写会把它们抹掉！…
decks=236 full=152 size_ok=234 limit_bad=98
hero_ids=57/63 missing_unknown=85 missing_known=3 suspect=40
wrote d:/4/_tmp_view/prebuilt_decks_check.tsv
wrote d:/4/Unity/资料/原版预组牌_核对.md (117 lines)
$ # ⚠️ 它把报告覆盖成 117 行（原 35622 B，含 4 个手工追加节）⇒ **已按原字节还原**：md5 = 2717c572c4266de73b1a15babf578183（= HEAD，git status 干净）
```

行尾：4 个文件**全 LF**（`gen_prebuilt_decks.py` CRLF 0 / LF 595）；`git diff --numstat` 无「数字≈文件行数」的整篇翻转。**没跑类型检查** —— 本批**一个 `.cs` 都没改**（改的全是 `.py`/`.json`/`.md`）。

## ⑥ 还欠什么 / 没查清

1. **① 的两个督军缺陷**要调度台裁（`decklists.json` 的 `faction` 标签 · `ASH3` 归属）—— 两个文件**都不在白名单**。
2. **12 副里 10 副拼不齐**（缺的是原版 id 表里没有的卡号）；既有缺口（`资料/原版预组牌_核对.md` 已记），本次只是把它落到**教程这一路**上。
3. ③ 的「没查到」三条（`actionType` 参数解析 · `ExecuteAction` 22 分支 · 教程文案本地化）。
4. ⚠️ `check_prebuilt_decks.py` **一跑就覆盖** `资料/原版预组牌_核对.md`（4 个手工追加节已被抹过一次，靠会话内备份还原）⇒ 建议改成写 `_tmp_view/`，不在本批白名单。
