# DIAG 防御卡 —— 进手牌还是「选」？

> 只读诊断（2026-10-12）。判据 = 反编译 `d:/2/tools/decomp_full/` + `dump.cs`（字段偏移）+ 反汇编
> `工具/disasm_va.py`（VA 实证）+ PnP 卡图。未跑 Unity、未动 git、未改任何生产文件。

## 一、结论（直答）

**(a) 防御卡「放在卡组」= 成立**：它是**独立的一格**（`CardDeck.defensiveCard // 0x48`，与督军并列），
**不占那 30 张**，构筑时由玩家自己挑，**也可以不带**（原版 `ValidateDeck` 完全不校验它）。

**(b) 用户说的「后手直接在手卡」= 本机这份参考构建里成立**：原版**没有能走到的「选防御卡」环节** ——
`SetupEnviromentalEffectPhase`（弹面板那一支）与 `ApplyOffensiveAndDefensiveEffects`（把防御卡塞进手牌那一支）
**都被 `ShouldUseEnviromentalEffects()` 挡住，而它恒返回 `false`**（反汇编实证，见 §二·2）⇒ 原版**直接发卡**：
`StartBattlePhase` → `AddGoesSecondCardToDeck` → **`AddNewCardToHand`**，后手方开局手牌里多一张防御卡。
⚠️ **但「选防御卡」的代码确实存在**（`ChooseCardMenu`、标题字面量 `defensive`），只是在这个 build 里**到不了**
—— 这就是我们多做了一步的根因（当初只读 `.c`，没看门本身是不是常量）。

## 二、原版证据

**1. 卡组那一格（(a)）**
- `dump.cs` `CardDeck`：`defensiveCard // 0x48`（**单体、不是 List**）+ `AddDefensiveCard/TryAddDefensive/CanAddDefensive/HasDefensiveCard/RemoveDefensiveCard`。
- `CardDeck__CanAddCard.c`：`cardType==10 → CanAddHero`；`cardType==0x14(20 Tactic) && spellType==0xd2(210 DefensiveCard) → CanAddDefensive` ⇒ **防御卡 = `CardTypeOptions.Tactic(20)` + `SpellType.DefensiveCard(210)`**。
- `DeckUtility__ValidateDeck.c`：只数 `cardLibrary(+0x58)` 与 `deckSize`（默认 `0x1e`=30）⇒ **防御卡不计入 30、0 张也合法**。
- 带进对局：`DeckAndWarlordData.defensiveCard // 0x20`（`MatchData.playerRawBattleData // 0x58` / `enemyRawBattleData // 0x50`）。
- `GameplayVariablesData // 0x48 shouldReshuffle · 0x49 showMulligan · 0x4A assignDefensiveCardsInEditMode`；后者**唯一读点** = `DeckEditingWindow___GetCardCollection_b__32_1.c`（编辑卡池的筛选 lambda）：`inventoryOptions != CantAddToDeck(5)` ⇒ 直接放行；`spellType != 210` ⇒ 剔除；**只剩「标了 CantAddToDeck 的防御卡」看这个开关**。

**2. 🔴「选」那一步在本 build 里是死代码（(b) 的关键，反汇编实证）**
| 调用点 | 反汇编 | 后果 |
|---|---|---|
| `0x1809C2D90`（`SetupEnviromentalEffectPhase.MoveNext`） | `call 0x1806FB110` → `test al,al; je` | 走「无环境卡」支：`bm+0x4f9=1` + `ClickChosenCardDone(bm, null)` ⇒ **面板永不弹** |
| `0x1809A8D23`（`FinishMulliganFinalPhase.MoveNext`） | 同一地址，`je 0x1809A8D85` | **不调** `ApplyOffensiveAndDefensiveEffects` ⇒ 防御卡**不走这条进手牌** |
| `0x1809C3F89`（`StartBattlePhase.MoveNext`） | `IsTutorialOrCampaignMatch()==0` 后再过它，`jne` 跳过 `call 0x180952380` | **`AddGoesSecondCardToDeck` 恒执行** |
- `0x1806FB110` 的字节 = `32 c0 c3`（`xor al,al; ret`）= **`return false`**。`dump.cs` 与 `script.json` 里 `BattleManager.ShouldUseEnviromentalEffects` 的 RVA **都是 `0x6FB110`**，与 `IsMobilePlatform` 同样折叠到这一个桩（全 dump **640 个方法**共用）；`IsMobilePlatform()==false` 在 PC 上本来就对 ⇒ **这是真 body，不是反编译假象**。
- 被它挡住的正是：`BattleManager.chooseCardManager(+0x1F8)`（`ChooseCardMenu`）+ 标题字面量 `DAT_1842a5eb0='defensive'` / `DAT_184283060='offensive'`。

**3. 发卡那条路（走到的那条）** —— `BattleManager__AddGoesSecondCardToDeck.c`
- `:28-95` 先取**后手那一方**的 `rawBattleData.defensiveCard`（本机后手⇒本机卡组那张；本机先手⇒对手那张）；`:98-142` 只有 `matchType ∈ {50 PracticeOffline, 80 EventAI, 110 ClosedDeck, 120 ClosedDeckBot}` 才从**后手方阵营的防御池**随机抽一张；`:154-166` 本机后手 ⇒ **卡组那张覆盖掉随机那张**。
- `:184` `AddNewCardToHand(card, 0, isPlayer: playerGoesFirst==0, …)` ⇒ **进后手方手牌**；调用点 `_StartBattlePhase_d__334__MoveNext.c:172`（**换牌全部做完之后**）。

**4. 旁证（第二来源、非官方）** 规则书中文 `:105-106`「防御卡：后手（防守方）可打出的特殊战术，弥补后手劣势，每个阵营有 3 张」· `:121` 战前第 6 步「后手取得防御卡（**抽牌后置入起手牌**）」· 第 2 步「**分离防御卡**」。

**5. PnP 卡面**（亲读 `d:/2/Warpforge部队卡片/Space Wolves/5防御卡/Warpforge_67_Orbital-Defences.png`）：费用 **0**、文字 `Deal 1 damage to an enemy troop and give it [Hunt Mark]`、兵种行写 **Defence**、无数值排 ⇒ 它是**手里打出的 0 费战术**；卡面**没印**「只能后手用」或「放进卡组」（那两条是规则层，不是卡面文案）。

## 三、我们现在怎么做 / 当初的判据是什么

- **卡组那一格**：`Deck/DeckEditorState.cs:246-247,282,316,321`（`Deck.DefensiveId`，进防御位**不占名额**；`:27` 类型 `defence`）；`RuleEngine/Core/DeckRules.cs:243` 只查「带了但不是 defence」。
- **发牌（= 原版走到的那条）**：`RuleEngine/Core/RuleCore.cs:608 GrantDefenceCard`（幂等）→ 现取来源 `GoesSecondCard`（`:633+` 三路：卡组那张 / 四档模式随机池 / AI 督军 `goSecondCardInHand`）；调用点 `:208-209`（`NewBattle`，换牌关）与 `:407-408`（`EndMulligan`，换牌开）。
- 🔴 **多出来的一步（原版到不了）**：`BattleDriver.cs:4610-4652` 分支③ —— 本机是后手就弹「选择防御卡」面板（`CardChoicePanel.Open(..., "选择防御卡", "defensive")` `:4650`）→ `:4729 OnDefensiveDone` → `RuleCore.ChooseDefensiveCard`(`:435`)/`SetDefensiveCard`(`:453`，**先摘掉原有那张再换进手牌**)；`:4764 PickDefensiveForAi` 是 AI 那半边。总闸 `offensivePhaseEnabled = true`(`:4546`，默认开)；另一半 `offensiveCardEnabled = false`(`:4564`，用户 2026-10-11 拍板「进攻卡不要、防御卡照旧要」)。
- **当初的判据** = `资料/普查产出_0930/进攻防御卡_面板语义.md` §四「后手方有没有『选防御卡』这一步 —— **有**」。那条是**只读 `.c` 源文本**得出的；`.c` 里这扇门被写成 `SpriteAtlasReference__ValidateAsset` 这种**串名的折叠桩**，所以当时没人发现它是常量 `false`（`grep ShouldUseEnviromentalEffects` 全仓文档 0 命中 `0x6FB110`）。

## 四、逐项差异 + 改动清单

| # | 原版（本 build 实证） | 我们 | 判定 |
|---|---|---|---|
| 1 | 防御卡在卡组里、独立一格、不占 30、可不带 | 同（`DeckEditorState` / `DeckRules:243`） | ✅ |
| 2 | 后手方开局由 `StartBattlePhase → AddGoesSecondCardToDeck` **直接进手牌** | `RuleCore.GrantDefenceCard` + `GoesSecondCard` | ✅ |
| 3 | 🔴 **没有**玩家「选防御卡」这一步（门恒 `false`） | 本机后手时**弹面板**，选完**换掉**卡组那张 | ❌ 偏离（多一次交互 + 改变最终那张牌） |
| 4 | 进攻卡那一段同样到不了 | 已由 `offensiveCardEnabled=false` 关掉 | ✅（巧合一致） |

**改动清单**（照本 build 收口；建议**只关不删**，与 `offensiveCardEnabled` 同一套口径）：
1. `BattleDriver.cs:4564` 之后加 `public bool defensiveCardPanelEnabled = false;`
2. `BattleDriver.cs:4610` 分支③ 的首句改成 `if (!defensiveCardPanelEnabled) return false;`；上面「AI 是后手 ⇒ `PickDefensiveForAi`」
   (`:4594`) 也要**关进同一个开关**（否则 AI 那侧仍走面板语义）。
3. 关掉后必须确认 `RuleCore.GrantDefenceCard` 照旧发卡（`EndMulligan` 那条 `:407-408`）⇒ 玩家手里仍有那张。
4. 要「完全照本 build」：`OnDefensiveDone` / `PickDefensiveForAi` / `SetDefensiveCard` 留着当备份即可（铁律 11 例外②）。

## 五、没查清的部分（不猜）

1. **别的 build 是否也恒 `false`** —— 本地只有一份 `GameAssembly.dll`（两份副本 md5 相同 `4d4ba80d786cc763cc520da3e776c9fe`）⇒ 无法比对。用户 2026-09-29 说过「以前选进攻卡效果时战斗场景背景那里的特效会有所改变」⇒ **现行线上版很可能是 `true`**；要坐实得再拿一份线上版 DLL。**这是本条唯一的不确定处。**
2. `ChooseCardMenu` 在**别的用途**（非环境卡）上的行为未查（本条只判「环境卡这条面板到不到得了」）。
3. `assignDefensiveCardsInEditMode` **两个模式各取什么值** —— 在服务器数据里，本地读不到（沿用既有结论）。
4. `RawCardScript.goSecondCardInHand(+0x150)` 的**值**（AI 兜底那一档）：本地 `*cardassets` 包只有 `Sprite/Texture2D/AudioClip`、无卡 SO ⇒ 读不到（沿用既有结论）。
5. **没跑到实况** —— 本条全部是静态反汇编 + 反编译 + 卡图，没有进原版游戏跑一次。
6. **搜过的词与范围**：`Defenc` / `Defens`（两种拼写）/ `SetupEnviromentalEffectPhase` / `EnviromentalEffect` / `goSecondCardInHand` / `Random Defence card` / `Mulligan` / `assignDefensive` → `d:/2/tools/decomp_full/`（25,096 个 `.c`）、`il2cpp_out/dump.cs`、`all_methods.txt`、`stringliteral.json`；反汇编 → `工具/disasm_va.py`、`工具/rva_of_method.py`。
