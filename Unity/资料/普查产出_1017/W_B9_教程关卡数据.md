# W_B9 · 教程关卡数据（6 关脚本进运行时）+ S5 督军订正（2026-10-17）

> 写手代理 B9。白名单内：**改 1 个**（`Unity/工具/gen_prebuilt_decks.py`，只为 ②）+ **重跑 1 个产物**（`…/Resources/tutorial_decks.json`）
> + **新建 3 个**（`Unity/工具/gen_tutorial_stages.py` · `…/Resources/tutorial_stages.json` + 它的 `.meta`）+ 本报告。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改正本 · ⛔ **一个 `.cs` 都没碰**。

## ① 主件：`Assets/RuleEngine/Resources/tutorial_stages.json`（651968 B · md5 `e17baef895ca680d53a64ea0f98606ac` · 全 LF）

- **生成器** `Unity/工具/gen_tutorial_stages.py`（照 `gen_prebuilt_decks.py` 的骨架：可重跑、幂等、产物头写「别手改」）。
  🔴 **必须用带 UnityPy 的解释器**：`"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_tutorial_stages.py`（牌组名/音效名要开原版
  bundle 解）；换别的 python **直接报错并告诉你该用哪条命令**（不静默降级）。
- **文件名**：按调度台建议用 `tutorial_stages.json`。⚠️ 先核过：现有那份**同名**的 `Unity/数据/游戏数据/tutorial_stages.json` **全工程 0 个消费者**
  （`grep -rn tutorial_stages --include=*.cs Assets/` 只命中 `TutorialModePopup.cs` 的**注释**）⇒ 不触发「有消费者就改名」。两份是**不同东西**：
  那份 = Godot 原型的**派生快照**（152 条、无时序），本产物 = **全量 462 条**；产物 `note` 里写死了这条提醒。
- **字段 → SO 的哪个**（一对一的）：

| 产物 | SO `TutorialStage` | 备注 |
|---|---|---|
| `playerDeck`/`aiDeck` + `*Pid` | 同名两个 PPtr | pid→资产名 = 开 `prebuiltdecks_assets_all.bundle` 解的；pid 留坐标 |
| `playerStarts` | 同名 bool | **S3 = false ⇒ AI 先手** |
| `starting{Player,Enemy}{Mana,Damage}` | 同名四个 int | 6 关全 0 |
| `hide{Cemetery,CardsLeftInDeck,LargeCardDisplay,Chat}` | 同名四个 bool | 6 关全 `0,1,0,1` |
| `playerAlwaysWins`·`preventPlayerResign`·`skipNormalBattleEndOn{Victory,Defeat}` | 同名四个 bool | 6 关全 false（机制照做，见 B6 §③） |
| `{player,enemy}StartingTroops{InHand}` | 同名四个 `List<RawCardScript>` | 共 30 个引用；`name` 来自 SO 自己的 `actionType` 字符串（见 ④） |
| `preMulligan`·`onVictory`·`onDefeat`·`turns[]` | `preMulliganScriptedActions`·`onVictoryScriptedActions`·`onDefeatScriptedActions`·`turnScriptedData` | **组内原序** |
| `actionOnPlayerResign` | 同名 | 6 关**全空**（与 `preventPlayerResign` 全 0 自洽） |
| 动作 `type`/`text`/`arg` | `scriptedActionData[0].actionType` / `actionType` / 后者的括号部分 | `type` = 枚举名（**运行时读的就是它**） |
| 动作 `sound`·`waitBefore`·`waitAfter`·`textReference`·`isPCTip`·`playerAction`·`shouldHighlightElement`·`smallTipParams`(8 子字段) | 同名 SO 字段 | 一对一 |
| `data[].type`·`acting`·`target` | `scriptedActionData[]` | `unitType`→`ScriptedActionUnit` 名；`pid`/`name`/`ourId`/`matchTier` |

- 判据印在产物顶部（`fieldSource` + 四张枚举表 `ScriptedActionType`/`ScriptedActionUnit`/`PositionReference`/`PositionRelation`，**逐条抄自 `.cs` 桩** + `actionTypeNote`/`refNote`/`knownIssues`）—— **产物自带判据，不靠人记**。
- 形状核过：**只有 string/数字/bool/数组/嵌套对象，无字典、无 null、无 NaN**（`assert_jsonable()` 每次跑都走一遍）。

## ② 交叉校验（与派生快照 `数据/游戏数据/tutorial_stages.json`）✅ **79/79 回合、0 处不符**

| 关 | 回合 | 玩家动作 | tip | 不符 | | 关 | 回合 | 玩家动作 | tip | 不符 |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 15 | 21 | 15 | 0 | | 4 | 15 | 31 | 11 | 0 |
| 2 | 11 | 16 | 10 | 0 | | 5 | 13 | 36 | 11 | 0 |
| 3 | 12 | 21 | 7 | 0 | | 6 | 13 | 27 | 7 | 0 |

- **对账规则（实测反推，写在 `cross_check()` 里）**：快照 `actions[]` == SO 里 `playerAction==true` 的**枚举名**（按原序）；`tips[]` == `SmallTip` 文案，
  **只洗 `<link=…>`/`</link>`/`<nobr>`/`</nobr>`/`<sprite name=…>`、保留 `<b>`、`<br>`→换行**；`name`↔`turnName`、`is_player`↔名字以 `Player` 开头。
  三条**全等** ⇒ 两边读的确实是同一份 SO（复现了 B6 的 79/79）。
- 规模断言（每次跑都验）：回合 **79** · 回合内动作 **430** · 回合内玩家动作 **152** · tip **61** = 上一批查实的四个数。🔴 产物 `actionCount=462` 是**全量**
  （430 + preMulligan 19 + onVictory 13）—— **别与 430 混**（产物 `countNote` 已写）。⚠️ 快照的 `turn` 字段 79 条**全是 "1"**（原型那管道的口径）⇒ 拿不了它对账。

## ③ ②件：S5 督军订正（改 `gen_prebuilt_decks.py`）+ **prebuilt 逐字节未变**

- 改动**两处、只影响教程那一路**：① 新增 `TUT_HERO_BY_SO` 表（挨着 `TUT_ROLE`，裁定/理由/⚠️ 全在注释里）；② `build_tutorial()` 解出督军后按表覆盖。
  **三条当场断言**：目标必须在卡池里且 `type=="hero"`；**名字必须真的含 SO 尾段**（核不上就抛，⛔ 不会静默换一个「看着像」的）；表里只有 1 条。
- 🔴 **没把覆盖搬进 `hero_match()`** —— 池那一路（103 副）走同一个函数，搬进去 `prebuilt_decks.json` 就会变。⛔ 三张数据表（`decklists.json`/`card_ids.json`/`warlord_ids.json`）**一个都没动**（表的矛盾 = 另一件账 A876）。
- 结果：`tutorial heroMismatch = 2 → 1`（只剩 S1，不在白名单）。S5 player `heroId='ASH5'`、`heroNote='SO尾段裁定（…）'`；池里 `ASH5` = `SaimHann / hero / Medreyal Ghaelyn`（`ASH3` 仍是 `Eliac Zephyrblade`）。`tutorial_decks.json` 15843 → 15754 B（净 −89）。

```
$ md5sum Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json
5097f3e89ce7288485961d2a70a6de9d          # 与 B6 记的基线**同一个** md5（改前=改后）
$ git diff --numstat -- Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json
                                           # ← **空**（git status 里连 ` M` 都没有）
$ git diff --numstat -- Unity/工具/gen_prebuilt_decks.py
308	67	Unity/工具/gen_prebuilt_decks.py     # B6 那批是 283/67 ⇒ 本批只多 25 行（表 + 覆盖 + 注释）
```

## ④ `actionType` 那半「参数」——**查清了：它不需要解析，它是【派生物】**

- 🔴 **`ScriptedAction.actionType` 是显示名**（`ScriptedAction__UpdateName.c`）= `Enum.ToString(scriptedActionData[0].actionType)` ＋按 sub
  actionType 决定追加 `" (" + X + ")"`：`{Attack 30, AttackFreeMode 31, ChangeToRanged 35, ChangeToMelee 36, ActiveAbility 40, PlayCard 20,
  ClickCard 110, TapCard 120, ContinueSmallTip 150}` ⇒ X = 单位显示名（`actingUnitType==10`→`Player warlord`、`==20`→`Enemy warlord`、
  否则 = `actingUnit` 的卡名 `+0x60`，ref 空则**不加**）；`{PlayerChat 50/55, AiChat 60/65, SmallTip 80, RadioMessage 90}` ⇒ X = `textReference`
  查本地化表出来的**文案**；`ResolveCard 140` 也加卡名。
- 🔴 **运行时读的是枚举**：`AiScripted__ExecuteAction.c:59-67` 把那串只喂了一句 `CustomDebug.Log`，分派走 `scriptedActionData[0].actionType`
  （int `+0x10`）。**acting/target 怎么解** = `AiScripted__GetActingCard.c`：10→玩家督军(`GetHero 1`) · 20→敌方督军(`GetHero 0`) ·
  30/31/32→`GetPlayerMinionsAndRemnantInPlay` · 40→`GetEnemyMinions…` · 50→`GetFriendlyHandRef` · 60→`GetEnemyHandRef`，再 `GetChosenCard`
  按卡 id 认那张、**认不到取第 0 张**；其它值 `LogWarning` 返 0。
- ✅ **本脚本反过来用这条规则**：把 `(卡名)` 收起来 ⇒ **76 个 pid → 卡名**（同一 pid 只对应一个名字，多值就抛）—— 这是**本地唯一**能拿到
  `RawCardScript` 名字的路（卡 SO 那个包本地没有，见 ⑤）。⚠️ 只有「发起者」有名字，「只当目标」的 pid 天然没名字。
- 产物**原样保留**该字符串（`text`），括号里那半另存 `arg`；`check_arg_rule()` 每次跑把上面那张分支表**逐条核一遍**（核不上就抛）。

## ⑤ 没查清 / 需要裁定（如实列，**没有猜**）

1. 🔴 **`allcards_assets_all.bundle` 本地没有** —— 卡 SO 住在那儿（SO `externals[2]` = `CAB-fb8f01449289635703d83d52c3f18245`）；`aa/catalog.bin`
   里有它、本地 84 个 bundle 里**没有**（同缺：`alternateartstyles`/`localization`）⇒ **8 个 pid 连名字都拿不到**（`unresolvedNoName`）。
2. ✅ **（2026-10-17 当日已裁、已改完 —— 见 §⑦）3 张卡有名字但池里没有**（`unresolvedNotPool`）：`Bladeguard Lieutenant`（S6 AI 第 6 回合 PlayCard）
   —— `card_stats.json` **有**、`cards_engine.json` **没有** ⇒ **池缺卡**；`Gauss Warrior (tutorial)` / `Tesla Immortal`（S3/S5 起始手牌 + 多回合）
   —— `card_stats.json` 的 **`ocrName`** 列正叫这两个名字，对应的池内卡是 `SAU10 Gauss Reaper Warrior` / `SAU12 Tesla Carbine Immortal`
   （**名字与 SO 一致、位置唯一**）。⚠️ 原来写「我们没采纳」—— **当日调度台就裁了：采纳**（判据同上，⚠️ **不是原版明确指认**，见 §⑦）。
3. S1 player 的督军**仍解不出**（`decklists.json` 把 `Uriel` 那副的 `faction` 标成了 `EmperorsChildren`）—— 不在白名单（B6 已记）。
4. `AiScripted.GetCurrentWaitTime.c` 里 `SoundAsset.GetDuration(clip) + *(float*)(action+0x28)` 的 `+0x28` 与 `textReference`(+0x28) 位置**对不上**
   （`GetDelay` 取的是 `+0x20`/`+0x24` = `waitBefore`/`waitAfter`）⇒ **这一处没读清**；「等待时长怎么算」留给执行器那批（产物里那两个值是**原样**的）。
5. `ExecuteAction` 22 个分支各自的表现细节**没读**；教程文案的**本地化表在远端**（`textReference` 有、表没有）⇒ 沿用 SO 内嵌英文。⚠️ `sound.volume` 多数是 `0.0`：
   `PlayNextSoundInQueue` 是 `PlayOneShot(clip, AudioListener.volume * sound.volume)` ⇒ **原版自己就是静音**，⛔ 别顺手改成 1.0（产物 `knownIssues` 里也写了）。

## ⑥ 跑过的命令与输出（原样贴）

```
$ "D:/2/Warpforge_tools/py312/python.exe" -I d:/4/Unity/工具/gen_tutorial_stages.py
pid->卡名：76 个（判据 = SO 自己的 actionType 字符串） / 牌组名 12/12 · 音效名 95/95（都开原版 bundle 解的）
wrote d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_stages.json
stages=6 turns=79 actions=462(回合内430) playerActions=152(回合内152) tips=61(回合内61) 未解出pid=8/3
   S1 15回合/21动作/15tip/不符0   S2 11/16/10/0   S3 12/21/7/0   S4 15/31/11/0   S5 13/36/11/0   S6 13/27/7/0
   tier: exact=215 · no-hit=1 · no-name=14 · norm=44 · null-ref=638 · ocrName=12 · 起始单位: exact=22 no-name=3 norm=2 ocrName=3
   ! pid=-8500042654006139415 tier=no-hit 名字=「Bladeguard Lieutenant」1 处（S6 AI 6 第1 条 PlayCard）；候选（**未采纳**）：…
$ python d:/4/Unity/工具/gen_prebuilt_decks.py
wrote …/prebuilt_decks.json  practice=103 complete=66 classic=49 classic_complete=29 no_cardback=0 hero_missing=7
wrote …/tutorial_decks.json  tutorial stages=6 decks=12 complete=2 / tutorial heroMismatch = 1   # ← 改前是 2
$ md5sum …/tutorial_stages.json    # 连跑两次都是 e17baef895ca680d53a64ea0f98606ac（**幂等**）
                                   # prebuilt 两次都是 5097f3e89ce7288485961d2a70a6de9d（未变）；tutorial_decks 仍是 b7066ac58ef9385964cc2067d83e7117
$ # 行尾：4 个文件全 LF（新 .py CRLF 0/LF 764 行 · 产物 CRLF 0/LF 25261 行 · .meta 158 B = 抄现有 .meta 的字节格式）；新 .meta 的 guid=b76a062548f74755aa7079ab6dae4366，扫过全工程 10714 个 .meta，无重
$ # ⛔ 没跑类型检查 —— 本批**一个 `.cs` 都没改**（改的全是 `.py`/`.json`/`.md`）
```

## ⑦ 追加 · S5 那两张卡的**裁定映射**（调度台 2026-10-17 当日裁 · 写手代理 B9 落地）

- **谁改的**：调度台裁定 ⇒ 写手代理 B9 落地（同一个代理、同一天，追加记录）。
- **改了哪**（只两处，都在白名单内）：
  · `Unity/工具/gen_tutorial_stages.py`：新增 `OCR_NAME_FIX = {'Gauss Warrior (tutorial)': 'SAU10', 'Tesla Immortal': 'SAU12'}` +
    `build_candidate_lookup()` 里新增 `verify_ocr_fix()` + `main()` 的 `resolve()` 多一档 `ocrName`；
    **文件头 docstring / 表上方注释 / `knownIssues` / `fieldSource`·`refNote`** 四处都写了判据与「不是原版明确指认」。
  · 产物 `…/Resources/tutorial_stages.json` 重跑（651968 B · md5 `e17baef895ca680d53a64ea0f98606ac`）。
- **判据**（每次重跑都**当场重验**，判据塌了就抛）：① 教程 SO 用的那个名字（= 原版卡 SO 的 `cardName`）与 `数据/游戏数据/card_stats.json` 的
  **`ocrName`** 列（= 当年从 **PnP 成品卡图**上 OCR 出来的名字）**逐字吻合**（按 `norm`）；② 在我们卡池里**位置唯一**；
  ③ 🔴 **这是按 `ocrName` 对上的，不是原版明确指认** —— 原版卡 SO 包（`allcards_assets_all.bundle`）本地没有，**钉不死**它俩与 SAU10/SAU12 是同一张卡。
- **两条硬要求都照办**：① 产物里 **SO 原名一个字没改**（`name` 仍是 `Gauss Warrior (tutorial)` / `Tesla Immortal`），
  我们的 id 另写 `ourId`、`matchTier='ocrName'`（与 `exact`/`norm` 分开，⛔ 别当「名字对上了」用）；② 三处如实标注：
  **生成器注释 + 产物**（`note` 尾巴 + `ocrNameFixes[]` + `ocrNameFixNote` + `knownIssues`）**+ 本报告**。
- ⛔ **`Bladeguard Lieutenant` 没塞**：它是「**池里真的没有**」（`card_stats.json` 有、`cards_engine.json` 没有）⇒ `ourId` **仍留空**，
  仍挂在 `unresolvedNotPool` 里，**另开一件查**。
- **结果**：`unresolvedNotPool` 3 个 pid → **1 个**（只剩 `Bladeguard Lieutenant`）；tier 直方图 `no-hit 13→1`、新增 `ocrName`（动作引用 12 处 + 起始单位 3 个）；
  `gen_prebuilt_decks.py` 与 `tutorial_decks.json` **按调度台要求重跑**（`heroMismatch` 仍 1、`tutorial_decks.json` md5 未变 `b7066ac58ef9385964cc2067d83e7117`）。
- **复验**（收口时重跑）：`prebuilt_decks.json` md5 **仍是** `5097f3e89ce7288485961d2a70a6de9d`、`git diff --numstat` **仍空**；
  `tutorial_stages.json` 连跑两次同 md5（幂等）；行尾：新 `.py` CRLF 0/LF 764、产物 CRLF 0/LF 25261（无整篇翻转）；**没跑类型检查**（本批仍无任何 `.cs` 改动）。
