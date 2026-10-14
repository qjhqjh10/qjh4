# W · 模式（经典/冲突）× 战将血量 —— 只读深查（2026-10-15）

> 只读代理。**没跑 Unity、没动 git、没改任何既有文件**；本件是唯一新建文件。
> 我方数据一律**现读**（`cards_engine.json` / `card_stats.json` / `GameplayVariables.cs` 读于 2026-10-15）。
> 目标：查「冲突/经典两个模式会不会让**战将血量**与**卡牌效果**不一样」，以及真值快照与我们卡表各属哪个模式。

## 1 原版有哪些模式（枚举/标识）

两张**正交**的表，别混：

| 表 | 档 | 出处 |
|---|---|---|
| **`PlayModes`**（15 档） | `Classic=0 · Duel=1 · PracticeLodge=2 · Dungeon=3 · Tutorial=4 · CutScene=5 · OfflinePractice=6 · ClosedDeck=7 · Campaign=8 · TutorialReplay=9 · Replay=10 · RankedFriendly=11 · OwnDeckTraining=12 · **Skirmish=13** · Battle4Warpforge=14` | `d:/2/tools/il2cpp_out/dump.cs:46188-46205`；桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayModes.cs` |
| **`MatchType`**（20 档） | `Ranked=10 / RankedBot=20 / PracticeOffline=50 / EventAI=80 / Practice=90 / Tutorial=100 / ClosedDeck=110 / ClosedDeckBot=120 / Campaign=130 / TutorialReplay=140 / Duel=150 / Replay=160 / Unranked=170 / UnrankedBot=180 / Scene=190 / **FastMode=200 / FastModeBot=210 / FastModeRanked=211**` | `dump.cs:27369-27392` |

- 🔴 **玩家看得见的两颗脸只有两个**：官方本地化 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 里
  `MainMenu/GameMode/Classic = ["Classic","经典"]` · `MainMenu/GameMode/Practice = ["Practice","练习"]`
  —— 全表搜 `冲突`/`快速` **没有一个命中在模式上**（都是卡名/台词）。
- ⇒ **「冲突」= 内部 `Skirmish` / `FastMode`**：判据三条 —— ① `PlayModes.Skirmish=13` + `MatchType.FastMode{200,210,211}`；
  ② 原版 Practice Mode Menu 里那颗经典/冲突 Toggle（图标 `40k_gamemode_icon_classic` / `…_skirmish`）与模式文案
  （`资料/索引与盘点/解包资源使用地图.md:533`）；③ 用户 2026-09-16 口述口径（`资料/加时与冲突模式_原版规格.md` §〇）。
- **模式参数住在哪**：`PlayEventData.gameplayVariables`（`Everguild/LiveOps/PlayEventData.cs:40`，
  接口 `IPlayEventData.GameplayVariables`，`IPlayEventData.cs:5`）—— **服务器按 event 下发**，本地 84 只 aa 包里没有这只包。

## 2 战将血量按模式不同吗 —— **会，但只有一条全局增量**

🔴 **唯一赋值点** = `d:/2/tools/decomp_full/BattleManager__SetupWarlordsLifeAndEnchantment.c`：

| 行 | 内容 |
|---|---|
| `:29` | 守卫 `if (*(int *)(GameplayData + 0x20) != 0)` —— `GameplayData` = `MatchData+0xA8`（`dump.cs:78158`），`+0x20` = **`warlordLifeChange`**（`dump.cs:130176`） |
| `:53` / `:72` | 我方 / 敌方：`CardScript__ForceChangeMaxHealth(督军, *(int *)(GameplayData+0x20) + 督军现值, 0)` |
| `:83` / `:94` | 仅当 `MatchType ∈ {EventAI(80), PracticeOffline(50)}` 才继续 |
| `:120` / `:146` | `MatchData.playerMaxLife(0xC0)` / `enemyMaxLife(0xC4)`（`dump.cs:78171-78172`）**绝对覆盖** |

- `CardScript__ForceChangeMaxHealth` **全库只有一个调用者**（就是这一处）⇒ 战将血量**没有第二处**会被模式改。
- 「状态 → 参数」表：

| 模式 / 条件 | 战将最大生命 |
|---|---|
| 经典（`warlordLifeChange = 0`） | = **卡 SO 的 `maxHealth` 原样**（整段跳过） |
| 冲突 / Skirmish（文案「少 10」） | = 卡 SO `maxHealth` **+ `warlordLifeChange`**（−10），**敌我同一条** |
| 练习(50) / EventAI(80) 且 `playerMaxLife ≠ 0` | 我方 = **绝对值** `playerMaxLife`（覆盖上式） |
| 同上且 `enemyMaxLife ≠ 0` | 敌方 = **绝对值** `enemyMaxLife` |

- ⚠️ **只覆盖这两种情况**：`warlordLifeChange` 的**具体数值**本地拿不到（远端下发）。
  我们落的 0 / −10 来自原版游戏内文案 + 粉丝规则书（`RuleEngine/Core/GameplayVariables.cs:99-107, 285-290`），
  并已按原版机制实现（`RuleEngine/Core/RuleCore.cs:520-526`：`warlordLifeChange != 0` 才走，敌我各一次）。

## 3 卡牌效果按模式不同吗 —— **不按**（只有一条「整张卡禁用」，不是改效果）

- 卡面效果全在卡 SO 上（`cardAbilities` / `CardTraits` / 三围）—— 而模式那份 `GameplayVariablesData` 的
  **18 个字段**（`dump.cs:130170-130190`）里**没有一个**动卡面：只有 `maxLegendaries / copies / slotsPerSide /
  warlordLifeChange / deckSize / overtimeTurn / 能量 6 项 / handLimit / 3 个 bool`。
- 🔴 **唯一一条「模式能挡卡」**：`PlayEventData.IsCardBanned(card)` = `bannedCardsCache.Contains(card.uniqueId)`
  （`d:/2/tools/decomp_full/Everguild.LiveOps.PlayEventData__IsCardBanned.c:13-20`；缓存来自
  `BannedCardsSO.bannedCards`，桩 `BannedCardsSO.cs`）。**它 ban 整张卡，不改效果**；SO 实例本地没有
  （`assets_full` 只在 `globalgamemanagers/MonoScript/MonoScript_3683.json` 有那个脚本）。
- **`deckCriteria` / `knightCriterias` 都不是模式字段**：
  · `deckCriteria` = `DeckCriteria{countCriteria, triggerIf, referenceValue}`（`DeckCriteria.cs`）——
    **1192/1195 张是同一份空默认值**（`triggerIf=1`），属 AI 评分/目标那一族（同族还有
    `cardTargetScoringCriteria` / `nonTargetScoringCriteria` / `attackScoringCriteria` …）。
  · `knightCriterias` = `KnightCriteria{pilotId, convertedKnightId}`（`KnightCriteria.cs`）——
    **1192/1195 是空数组 `[]`**，且 `grep -rl knightCriteria d:/2/tools/decomp_full` = **0 命中** ⇒ 未实现的机制。
  · 两者都**不按模式变**，也不影响效果结算。

## 4 真值快照属于哪个模式 —— **它不属于任何模式**

- 快照 = `RawCardScript` **1,195 条卡 SO**（每张 `m_Script` 指向 RT1 形脚本；字段与
  `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:41 maxHealth` 逐一对上）。
  **118 个字段里没有任何「模式 / 事件」字段**（`expansionPack` 是资料片、`cardType` 是卡类）。
- §2 的机制说明：模式那份 −10 是**运行时**在 `ForceChangeMaxHealth` 里加的，**不回写卡 SO**
  ⇒ `maxHealth` 是**两模式共用的基准值**。
- 但**「哪一版」反推得出来**（这才是它真正的身份）：快照 `cardArmy` 只有 **12 个阵营**，
  **整个 Space Wolves 0 张**，`Logan / Ragnar / Njal / Zacharial / Paxor / Guilliman` 在 1,195 条里**全 0 命中**；
  而 PnP 里有这些卡 ⇒ **快照是较旧的一版**（与 `d:/2/远端缓存_旧profile_1013/README.md` §四自陈一致）。

## 5 我们卡表的督军血量来自哪

**链条**：PnP 卡面 OCR → `数据/游戏数据/card_stats.json`（`merge_card_stats.py`，2026-08-16）
→ `工具/gen_cards_engine.py:30 SRC` → `:1074 health = norm_int(c.get("health"))`
→（`STAT_FIXES` / `cardface_fixes.json` 手工列）→ `MyGame/Assets/RuleEngine/Resources/cards_engine.json`。

- 每个督军的血 = **我们自己 OCR 卡面最右绿框**（`card_stats.json` 的 `ocrSrc` 指着 PnP PNG）。
- 2026-10-15 **亲开四张核过**：`Kaphrael` 印 **25**（`Emperor_s Children/1督军/Warpforge_01_Lord-Kaphrael.png`）·
  `Swarmlord` 印 **40**（`Tyranid/1督军/Warpforge_3_Swarmlord.png`）·
  `Commissar Denkler` 印 **30**（`Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png`）·
  `Lucius The Eternal` 印 **25**（`Emperor_s Children/1督军/Warpforge_03_Lucius-The-Eternal.png`）
  ⇒ **不是 OCR 错读，PnP 印刷面就是这几个数**。
- 手工列**没动过**这 7 张的血：`STAT_FIXES`（`gen_cards_engine.py:220`）**无 health 项**；
  `cardface_fixes.json` **无 `"health"` 键**（只有 `desc` / `_manual_subtype` / `_manual_stat` 那几族）。
- ⇒ 结论：**我们那 25/30/40 是「PnP 印刷面那一版」的数，同样不属于任何一个模式。**

## 6 那 7 张数能否归模式（逐张）

| id | 卡 | 我们 | 快照 | 快照−10 | 像「冲突那一档」吗 |
|---|---|---|---|---|---|
| `ASH76` | Lhykhis | **25** | 35 | 25 | 数值巧合（见下） |
| `EC55` | Lucius The Eternal | **25** | 35 | 25 | 数值巧合 |
| `EC56` | Lord Kaphrael | **25** | 35 | 25 | 数值巧合 |
| `EC57` | Xarahan Daemon Prince | **30** | 40 | 30 | 数值巧合 |
| `AM3` | Commissar Denkler | **30** | 35 | 25 | ✗ 两个都不是 |
| `GOF76` | Mozrog Skragbad | **40** | 35 | 25 | ✗ **方向相反**（我们比快照**高** 5） |
| `TL35` | SwarmLord | **40** | 35 | 25 | ✗ **方向相反** |

**为什么不能归模式（三条硬判据）**：

1. 模式的算法是**一条全局偏移**（§2，`CardScript__ForceChangeMaxHealth` 只有一个调用者）。
   若 PnP 印的是「冲突」那一档，**全部 51 张能对上的督军都该比快照低 10** ——
   实测**32 张正好 35、11 张正好 40，一张都没低**（我方现读分布：35×32 / 40×13 / 25×7 / 30×4 / 0×1；
   快照 60 个督军：35×40 / 40×11 / 20×6 / 30×3）。⇒ **不成立**。
2. **反向两例**（Mozrog 40、Swarmlord 40，都快照 35）**不可能**由「−10」产生，任何模式组合都解释不了。
3. `AM3` 我们 30，快照 35、快照−10 = 25 —— **两头都不是**。
   ⇒ 那 4 张「数值上正好等于快照−10」是**巧合**（25/30 都是整数档位），不是同一条规则。

## 没查清（搜过哪些词 / 哪些文件）

- **「冲突」在官方串里没有名字**：`官方本地化_en+zh-CN.json` 9,441 条里搜 `冲突` / `快速` / `skirmish` / `fast mode`
  —— 模式名只找到 `MainMenu/GameMode/{Classic,Practice}`。冲突/经典那对在游戏里怎么叫，
  **只靠本仓记录**（`资料/索引与盘点/解包资源使用地图.md:533` + 用户口述），**没有第二份独立判据**。
- **本地没有任何一版「现行」卡数据**：`d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`
  84 只包里**没有** `cards*` / `gameplayvariables*` / liveops event 包；`C:/Users/qjh36/AppData/LocalLow/Unity/Everguild_Warpforge/`
  只有旧 profile 那三只（54bdb7b1… / c9187397… / 7416b641…）。搜过词：`cards` `gameplayVariables` `warlordLifeChange`
  `BannedCards` `allcards`。
- **远端下发的具体数值**（`warlordLifeChange` 等 18 项的两套真值、`playerMaxLife` 的写入者）**拿不到**。
  `WarlordPathData.PlayerLifeStages` / `CampaignPathData.EnemyLifeStages` 提示**战役**那条路会改战将生命，
  但**谁写 `MatchData.playerMaxLife`** 没查到（反编译无字段名，`grep MaxLife` = 0 命中）。
- 我为找「PnP 那一版是不是现行版」的**第二条独立判据**搜了：`d:/2` 全盘的 `*card*.json` / `*allcard*`、
  `d:/2/Warpforge部队卡片/*.md` 四份 PnP 派生表、`d:/4/Unity/资料/卡表核对_卡图提取/` —— **都只是 PnP 的下游**，
  **不独立**。⇒ 「该信哪一版」**仍然只有一条腿**。

## 倾向（裁定归主对话）

- 🟢 **确定度【很高】：这 7 张既不是「冲突模式」的数、也不是任何模式差异** ——
  模式只给一条敌我同用的 `warlordLifeChange` 增量，机制上产不出「3 张反向 + 1 张两头都不是」这种分布。
  ⇒ **「模式不同 ⇒ 不能照真值改」这条理由不成立**（那条风险可以从 `A2006` 的顾虑里划掉）。
- 🟡 **确定度【低】：该不该照真值改**。两条现成先例**方向相反** ——
  `A1876`（2026-10-14，同形状的 22 条**效果文字**）裁 **PnP 现行印刷版胜、快照是旧版**；
  而 `A2006`（督军**费用** 0→2）裁的是 **快照胜**。而本条查实**快照整体比 PnP 旧**（无 SW 整族）
  ⇒ 「照真值改」= **把新版的数换成旧版的数**，方向存疑。
  ⇒ 我倾向：**先别动这 7 张的血量**，等一条能证明「哪一版是现行」的独立判据（同一笔里那 47 张**费用**
  不受本结论影响 —— 那个数在卡 SO 上、两个模式同值，冲突/经典都不构成理由）。
