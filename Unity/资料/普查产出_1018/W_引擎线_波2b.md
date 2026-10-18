# W · 引擎线 波 2b（选择面板数据管线 ＋ 波 5 交来的数据补丁）

> 范围 = `A1959` 数据管线半边 · `A1960` 候选域 · 波 5 §3·3 的 3 条 `desc` 补丁 · 波 5 §3·4 的 2 张解析缺口 · 波 5 §3·2 的 6 行待裁 · `SW4`。
> 日期 2026-10-19 · 写手 = 引擎线波 2b · **独占** ④ 列文件。
> 红线遵守：**未跑 Unity / `*.Run` / player**；`工具/ruleprobe/**` 与基线**一个字节未改**；未动 git；未改正本；`BattleDriver.cs` / `CardPresentation/**` **零改动**。
> 验证口：`TMPDIR=/tmp/wf_eng2b bash 工具/typecheck.sh` → **运行时 0 · 编辑器 0**；另用**临时**离线台（`_tmp_eng2b/probe2b/`，不进仓库）跑了真卡池的解析/候选域/端到端结算（见 §三·二、§六·5）。

---

## §一 逐条表

| A号 | 做了什么 | 文件:行（2026-10-19 现读） | 判据 | typecheck |
|---|---|---|---|---|
| **`A1959`（数据管线半边）** | `cards_engine.json` v9→**v10**：每张卡新增 `choicesTargetsAffected` / `choicesMaxTargets` / `choicesSpecificCardIds` 三列；`CardDef` 新增同名 `Choices*` 属性；`CardDatabase` 读入并 bump 版本守卫。**池内 1127/1127 落值**（无 `-1`）。 | `工具/gen_cards_engine.py:1873`（`path_id`→`uniqueId` 解引用表）· `:2298-2322`（逐卡三列）· `:2751-2764`（跑完读数）· `:2776`（v10 注释）· `Core/CardDef.cs:227/231/235/245`（属性）· `:302-304`（构造入参）· `:310-318`（赋值）· `Data/CardDatabase.cs:191-212`（DTO）· `:278-288`（v10 守卫）· `:354-356`（传参） | `dump.cs:23341` `RawCardScript.choicesDefinition (TargetCriteria) @0x2A0` · `dump.cs:23855` `TargetCriteria.maxTargets @0x40` · `dump.cs:20777` `TargetsAffected.choiceResolution = 180` · `BattleManager__GetChoiceFullPool.c:36-49` · `BattleManager__GetChoiceOptions.c:40-45` | 0/0 |
| **`A1960`（候选域）** | ① `EffectOp.ChooseSpecificIds`；② `EffectText.Parse(…, card:)` 注入：**正文没有 choose 子句**且**会授予一份未指定种类的 Dark Pact**且**带 `choicesDefinition`(180+mt>0+候选非空)** ⇒ 在最前面插一条 `choosecard`（`ChooseSrc="specific"`）；③ `ChooseCardCandidates` 新增**第 7 个候选域 `specific`**（按 id 点名、池外如实跳过并写进 `detail`）；④ `GrantDarkPact` **消费 `ctx.LastChosenCard`**（选中那张 `Dark Pact of X` ⇒ 种类 X；显式种类仍然赢）。 | `Core/EffectText.cs:386`（字段）· `:1197-1199`（`Parse` 加 `card`）· `:1392`（调用点）· `:1423-1468`（注入函数 + 固定 `Source` + 契约授予探测）· `Core/EffectResolver.cs:3138-3178`（`specific` 域）· `:7284-7296`（消费选中那张）· `:7412-7423`（`PactKindOfChosenCard`）· 传 `card` 的四个解析点：`CardDef.cs:1039/2127`、`EffectResolver.cs:1430/1592/3612/3708` | `BattleManager__GetChoiceFullPool.c:36-49`（180 ⇒ `filterSpecificCards`）· `BattleManager__ChoiceOfCardPlayer.c:27`（玩家那一路必排序）· `GivePayload.ExtractVariant:637-649`（未写 `of X` ⇒ `"random"`）· `BuffType` 枚举（`dump.cs:45430`，addTrait=3） | 0/0 |
| **波 5 §3·3 · 3 条 `desc` 补丁** | `cardface_fixes.json` 的 `desc` 补 `Talent:` 行：`GOF87 Zodgrod Wortsnagga` +`Talent: Speshul Dose` · `SW1 Logan Grimnar` +`Talent: High King of Fenris` · `GOF77 Beastboss Morgrim` +`Talent: Ferocious Rage`。重生成 `cards_engine.json`。 | `数据/游戏数据/cardface_fixes.json` 的 `desc` 段（三条新键 + 一条 `_2026-10-19_A1959_天赋行补回` 说明键）；CRLF 保持（2088→**2092** 对 CRLF/LF 相等）、重复键 **0** | **PnP 成品卡图亲读**（铁律 7）：`d:/2/Warpforge部队卡片/Orks/3部队/Warpforge_12_Zodgrod-Wortsnagga.png` · `…/Space Wolves/1督军/Warpforge_01_Logan-Grimnar.png` · `…/Orks/1督军/Warpforge_01_Beastboss-Morgrim.png`（本次三张开图复核，文字逐字一致） | 0/0（纯数据） |
| **波 5 §3·4 · DA1 / AM15 解析缺口** | **核查结论：今天两张都已解析正确，无需改代码。** `DA1` = `choosecard(decktop)` + `create(Talent: Supreme Grand Master)`；`AM15` = `lowercost` + `create(Talent: Cadian Honour)`；两张 `unparsed`/`partial` 均为空。波 5 的读数来自 **2026-10-18 07:04 那份旧 `scan` 基线**（见 §三·三）。 | 无（未改 `RuleEngine/**` 的解析逻辑） | 现跑 `ruleprobe card "Azrael"` / `card "Minka Lesk"`；且 `cardface_fixes.json` 里 `Azrael`/`Minka Lesk` 两条 `desc` 已在 2026-10-18 `W` 批补过（`_2026-10-18_A2005_A1995_A2002_desc与descZh`） | — |
| **波 5 §3·2 · 6 行待裁** | 逐条查证（见 §四）：**4 条仪器误判**（`TAU55` · `SW40` · `SAU44` · `SW33`）· **1 条替代分支已解释**（`SW21`）· **1 条无机器判据**（`Mekaniak`，留「没查到」）。 | 只读 `cards_raw.json` 的 `abilityLogic` / `BoardCriteria` / 解包反编译；**未改任何代码或数据** | 见 §四逐行 | — |
| **`SW4`** | 主对话裁定落位：**基础三围按当前 build SO、效果正文数值按 PnP/`desc`**。`SW4` 的 cost/attack/health/ranged = **1/2/2/1**，与 SO **逐项相同**；冲突只在**效果正文**（SO `doDamage 2` vs PnP/`desc` `Deal 3 damage`）⇒ 属「效果正文」那一类 ⇒ **取 3、保持现状、不改**。 | 无改动（`cards_engine.json` 的 SW4 三围/desc 均已就位） | `cards_raw.json` SW4 `manaCost=1 / meleeAttack=2 / maxHealth=2 / rangeAttack=1`；`cards_engine.json` 同值；W5 TSV row `SW4 … 原版 2 点伤害，我们 3` | — |

---

## §二 数据管线：字段与生成前后逐卡 diff

### 2·1 三列的口径（写进生成器与 DTO 的注释里，判据别处不再抄）

| JSON 列 | 来源（原版 SO） | 缺值口径 | 消费者 |
|---|---|---|---|
| `choicesTargetsAffected` | `RawCardScript.choicesDefinition.targetsAffected`（`TargetCriteria` @0x2A0） | `-1` = 真值里没这张卡（⛔ 别当 `0`，`0` 是合法 `any`） | `CardDef.ChoicesTargetsAffected` / `HasSpecificCardChoice` |
| `choicesMaxTargets` | `…maxTargets`（@0x40） | `-1` = 缺值；`0` = 合法「不摆」 | `CardDef.ChoicesMaxTargets` → **交战斗线**（见 §五） |
| `choicesSpecificCardIds` | `…filterSpecificCards`（@0x70，PPtr 列表） | 空表 = 原版那一格空（回落 `BattleManager+0x308`，我们**没有**那一格） | `CardDef.ChoicesSpecificCardIds` → `specific` 候选域 |

- **PPtr 解引用**：`m_PathID` → `path_id` → 真值卡的 `uniqueId`（= 我们池的 id）。全快照 **178 条 filter 引用全部解出（0 未解）**；池内 **169 条**，其中 **57 条指向池外卡**（变体卡 SO / 附魔模板，例如 `Adaptive Strategy` 指向 `_normal`/`_oath` 两张变体）⇒ **数据照实落值**，消费侧自己判（⛔ 不在数据层替它丢掉）。
- ⚠️ **`path_id` 原来被丢掉**：`load_truth_cards()` 展平 `{cards:[…full_raw]}` 时只取 `full_raw`，而 `path_id` **只在元素外层** ⇒ 本次在展平时补进 row（`gen_cards_engine.py:1080-1085`）。

### 2·2 池内分布（现读）

| `(targetsAffected, maxTargets)` | 张数 | 说明 |
|---|---|---|
| `(0, 0)` | 979 | 无选牌定义 |
| `(0, 3)` / `(0, 2)` / `(0, 4)` | 62 / 1 / 1 | 普通目标规格，不是 `choiceResolution` |
| `(180, 3)` / `(180, 2)` / `(180, 4)` | 37 / 11 / 8 | **`choiceResolution`**，共 **56** |
| `(65, 3)` / `(50, 3)` / `(60, 3)` | 14 / 8 / 6 | 其它 `TargetsAffected` |
| 有非空 `filterSpecificCards` | 50 张 / 169 条引用 | `180+mt>0+filter 非空` 的 = **40 张** |

- **`maxTargets ∈ {2,4}` 的池内卡 = 21 张**，与波 2 在 `BattleDriver` 里那张 21 条镜像表**逐张吻合**（12 张 2 · 9 张 4）——这是在数据侧独立复核了那张表（见 §五）。
- 40 张里 **17 张正文没有 choose 子句**：6 张注入（§三·二）· `BL29` 死数据（当前 build 能力列表为空）· `Drone Companion`（下游造牌不消费选中那张）· 9 张 filter 指向池外变体卡（`UM109/110/111/112/115/116/117/118` + `Angel's Wrath`）。其余 23 张正文自己就有 choose。

### 2·3 生成前后逐卡 diff（`cards_engine.json`）

对比 `HEAD` 的 v9 与本次 v10（`python` 逐卡 join，全 1127 张 id 集合不变）：

| 列 | 张数 | 明细 |
|---|---|---|
| `choicesTargetsAffected` / `choicesMaxTargets` / `choicesSpecificCardIds` | **1127 / 1127 / 1127**（新增列） | 每张都有值；`-1` **0 张** |
| `desc` | **6** | 本次 3 张：`GOF87` / `SW1` / `GOF77`（+`Talent:` 行）；**另 3 张是工作树里先前已改、只是没重生成** 的：`ASH83 Autarch`（补 `Waystone. `）· `AM15`（补 `Talent: Cadian Honour`）· `DA1`（补 `Talent: ` 前缀） |
| `descZh` | 15 | 全部是**先前工作树里的官方本地化改名**（`Abaddon→四者之选` · `Painboy→锯骨者` · `Necron 墓刃→墓穴之刃` …），本次**未改**这些内容，只是随重生成落进卡表 |
| `keywords` | 12 | 同上（`Njal Stormcaller`/`Ragnar`/`Titan…` 的 `Talent: …` 与 `Slay:` 整句、`Oath` 等），**非本次改动** |
| `cost` | 4 | 四张 `Dark Pact of *`：`1 → -1`（2026-10-19 另一笔已改数据源的「不可直接打出」哨兵），**非本次改动** |

> 🔴 **为什么会有「非本次」的 diff**：`cards_engine.json` 是生成物，工作树里 `cardface_fixes.json` 等源文件**已被先前的批次改过、但没人重生成**。本波按任务要求重生成 ⇒ 这些既有的数据修正**一并落进了卡表**（不落反而是「表与源不一致」）。本波自己的数据改动只有 **3 条 `desc`**。
>
> `--check` 现读：**一致 ✅**（重跑同一版生成器不会再有 diff）。文件 823 KB（v9 → v10 的增量来自 1127 张 × 三列）。

---

## §三 3 条补丁与解析修复

### 3·1 三条 `desc` 补丁（波 5 §3·3）

PnP 卡图逐字亲读（本次再次开图复核）：

| 卡 | 原 `desc` | 新 `desc` |
|---|---|---|
| `GOF87 Zodgrod Wortsnagga` | `Deploy a Grot or a Snakebite Grot at random.` | `… at random. **Talent: Speshul Dose**` |
| `SW1 Logan Grimnar` | `Start the game with Tyrnak and Fenrir in hand.` | `… in hand. **Talent: High King of Fenris**` |
| `GOF77 Beastboss Morgrim` | `When you deploy a Beast, give +1 [Attack] to your Warlord this turn.` | `… this turn. **Talent: Ferocious Rage**` |

补完后的解析（离线台现读）：

- 三张的 `CardDef.TalentName` = `Speshul Dose` / `High King of Fenris` / `Ferocious Rage`（补之前**三张都是 null**）。
- 三条 `desc` 各多一条 `create`（`payload = <天赋名>`，`dest = hand`）—— 与 `EffectText.TryTalent`（`A1873` 口径）及 `RuleCore.SpawnTalents` 的机制层对齐。
- `GOF87` 的 **Mob 裸写正文**由 `BareBodyText`/`HandledByOtherLayer`（`CardDef.cs:1073` / `:2759`）在 `Talent:` 段**截断** ⇒ `TriggerOps("mob")` 仍只有 `deploy` 一条（离线台已核），**不会被 Mob 吞掉**。
- `SW1` 的第一段 `Start the game with Tyrnak and Fenrir in hand` 仍 `unparsed`（既有缺口，不是本波引入；它由 `StartWithInHand` 另一层承载）。

### 3·2 `A1960` 注入的**全池面**（离线台实测）

对全池 1127 张逐张比较 `EffectText.Parse(desc)` 与 `Parse(desc, card: card)` 的 op 序列：

```
changed 6
  BL4  Chaos Sergeant        : gain/pool            => choosecard/specific;gain/pool
  BL13 Chosen                : gain/pool            => choosecard/specific;gain/pool
  BL37 Abaddon's Chosen      : give/pool            => choosecard/specific;give/pool
  BL38 Dark Oratory          : give/pool            => choosecard/specific;give/pool
  BL57 Hellfire Torch        : give/pool            => choosecard/specific;give/pool
  141c5ed6… Unhallowed Gifts : give/pool            => choosecard/specific;give/pool
```

⇒ **全池口径只动了这 6 张**（其余 1121 张逐字不变）。反例也已核：`GSC65 Pilfered Supplies` 的 ask 仍 = **1**（正文自己有 choose，不重复问）；`BL29 Terminator Champion` = **0**（数据有、能力为空 ⇒ 死数据）；`Drone Companion` / `UM109` 等 = **0**（见 §六·2）。

候选域 `specific`（离线台）：`BL4/BL37` 各 **4 张**、顺序 = SO 的 filter 顺序 `Blood · Resilience · Excess · Fate`；`detail = 原版指定 4 张 · 池内命中 4`。端到端：打出 `Abaddon's Chosen`、面板选 `Dark Pact of Excess` ⇒ 目标部队 `PactOf = excess`（`PlayTactic code=0`）；显式种类（`kind="blood"`）仍压过选中那张。

> 这份行为也写成了 `RuleEngineTest.TestPlayerChoice` 的 **⑦**（`Editor/RuleEngineTest.cs:16573-16632`）：三条反例 + 端到端一条。⚠️ **本波没跑 `RuleEngineTest.Run`**（红线），但同一段逻辑已在离线台上**逐句跑过并通过**（临时台不进仓库）。

### 3·3 `DA1` / `AM15`：**波 5 的读数已过期，解析本身没有问题**

波 5 §3·4 说 `DA1` 的 `unparsed = "Supreme Grand Master"`、`AM15` 的 Talent 被 `lowercost` 吞掉。**今天两张都解析干净**：

```
CARD Azrael id=DA1 …
  unparsed= partial=
  op: choosecard(deck→decktop) ["Choose a card from your deck and put it at the top of your deck"]
  op: create payload=[Supreme Grand Master] dest=hand ["Talent: Supreme Grand Master"]

CARD Minka Lesk id=AM15 …
  unparsed= partial=
  op: lowercost/1|friendly infantry
  op: create payload=[Cadian Honour] dest=hand ["Talent: Cadian Honour"]
```

错因：波 5 §四·10 自己写明「本报告里的 `scan` 读数仍是 **2026-10-18 07:04** 那份」——那份基线**早于** `cardface_fixes.json` 的 `_2026-10-18_A2005_A1995_A2002_desc与descZh`（它给 `AM15` 补了 `Talent:`、给 `DA1` 补了前缀）。本波用**当天的解析器 + 当天的卡表**复跑 `card` 得到上面的结果。⇒ **不需要改代码**；`W_引擎线_波5.md` §3·4 那两行建议由主对话作废（本波未改他人报告文件）。

---

## §四 波 5 待裁 6 行 + `SW4` 判定

判据 = `cards_raw.json` 的 `abilityLogic`（含 `targetCriteria.excludeTrait` / `buffEffect.buffType` / `normalAbilityBoardCriteria`）＋ 反编译 `RawCardScript__GetActiveAbility.c` ＋ PnP 亲读。

| 卡 | 波 5 判定 | 本波结论 | 证据 |
|---|---|---|---|
| `TAU55 Relentless Fusillade` | 取值不同（原版 4 vs 我们 2） | **仪器误判（语义同）** | 原版 L0 = `2` 点给 **`excludeTrait=1` 的敌人**（非 Markerlight）；L1 = `4` 点给 **Markerlight 敌人**。卡面「2 点 + 对 Markerlight 额外 2 点」= 2 / 4 ▶ 同。仪器只比了 `traitsFilter`、**没读 `excludeTrait`**。 |
| `SAU44 Disruption Blades` | 条数不同（原版 +1 / +2） | **仪器误判（语义同）** | L0 = 全体 +1 近战；L1 = **Destroyer（trait 680）** 再 +2 ⇒ Destroyer 合计 +3。卡面「+1，若为 Destroyer 则 +3」▶ 同（原版是**加法编码**，我们是条件换值）。 |
| `SW33 Vindicator` | 条数不同（原版 3 / 6） | **仪器误判（语义同）** | L0 = `3` 点给 **`excludeTrait=1`（无猎杀标记）**；L1 = `6` 点给有猎杀标记 ⇒ 与卡面「3，或带猎杀标记 6」**同**。同 `TAU55` 的 `excludeTrait` 漏读。 |
| `SW40 Bjorn's Shrine` | 条数不同（第二条 +1 血 + trait） | **仪器误判（语义同）** | 第二条 `buffEffect.buffType = 3 = addTrait`（`dump.cs:45430`）⇒ 只有 `trait=1270 (dontReturnFerocity)` 生效；`healthChange: 1` 是**残留字段**（与波 5 §五·7 记的 `buffEffect` 残留同一类）。PnP 亲读 `Space Wolves/4计策/Warpforge_46_Bjorns-Shrine.png` = `Give +2 Health … stays in play`，与我们逐字一致。 |
| `SW21 Wulfen Pack Leader` | ❓查不清（替代分支） | **已解释：不是缺陷** | `hasAlternativeAbility=1`；`RawCardScript__GetActiveAbility.c`：`MeetsCriteria(normalAbilityBoardCriteria)` 不成立 ⇒ 用 `alternativeAbility`。SW21 的 criteria = `friendly minions` **EqualTo 0**（`triggerIf=1(EqualTo)`、`referenceValue=0`）⇒ **控制 0 个其他部队时**走 normal = **5 点**，否则 alternative = **3 点**。与我们的 desc「3，或若你没有其他部队则 5」**一致**。仪器只读 `activeAbility`（5）故报红。 |
| `Mekaniak` | 选区动作不符 | **没查到（无机器判据）** | 原版一步 `chosenAbility=100 (createCardInHand)` + `selectFromChooseResolution=1`、target = `friendlyUnits + includeSpells + keywordFilter=10`；我们 `chooseeffect(空) + give`（三张 Kustom Job 的候选表在 `ChooseEffectPools`）。两边都「挑一个改装件给载具」，但原版把结果**编码成造到手牌**、我们**直接应用** ⇒ 没有可核的机器判据。**不改**（波 5 也留红）。 |

**`SW4 Blood Claw`**（主对话裁定后的落位）：

| 维度 | 当前 build SO | PnP / 我们的 `desc` | 结论 |
|---|---|---|---|
| cost / attack / health / ranged | `1 / 2 / 2 / 1` | `1 / 2 / 2 / 1` | **一致**（这一层按 SO，早已落好） |
| 效果正文 | `doDamage 2`（`activeAbilityLogic`） | `Deal 3 damage to an enemy` | **属「效果正文」** ⇒ 按主对话裁定取 **PnP/`desc` = 3** ⇒ **保持现状、不改** |

---

## §五 交战斗线的接口需求（`BattleDriver` 侧零改动）

`BattleDriver` 里那张**21 条的镜像表**现在**可以删了**——数据与读取口都已就位，而且**数据侧独立复核过它与真值逐张吻合**（§二·2）。

现状（`CardPresentation/Battle/BattleDriver.cs`）：
- `:6102` `static readonly Dictionary<string, int> ChooseShowByCardId`（21 条，`maxTargets ∈ {2,4}`）
- `:6131-6136` `static int ChooseShowLimit(CardDef c)`：查表，查不到 ⇒ `ChooseShowDefault`(3)

建议改法（**行为等价**，可逐条核）：

```csharp
// 删掉 ChooseShowByCardId 整张表；ChooseShowDefault 保留（= 原版众数 / 缺数据兜底）
static int ChooseShowLimit(CardDef c)
{
    // A1959：逐卡 `maxTargets` 从卡表来（原版 `choicesDefinition.maxTargets` @0x40；
    //   数据源 = 工具/gen_cards_engine.py 的 choicesMaxTargets，v10）。
    // ⚠️ `-1` = 真值里没这张卡、`0` = 原版「不摆」⇒ 两者都回落到众数 3（与改前一致）。
    if (c != null && c.ChoicesMaxTargets > 0) return c.ChoicesMaxTargets;
    return ChooseShowDefault;
}
```

- **等价性证据**：池内 `choicesMaxTargets ∈ {2,4}` 正好 **21 张**，与镜像表**同一批**；其余卡 `maxTargets` 为 `3`（62+14+8+37=121 张）或 `0`（980 张）⇒ 前者回 3（= 旧默认）、后者也回 3（旧表也没有它们）⇒ **没有任何一张卡的摆牌数会变**。
- `ChooseShowByCardId` 表里那 21 条的注释/数字也已被波 2 就地订正过；删表时把 `:6035-6101` 那段 doc 一并收掉即可。
- 本波**没有**碰 `BattleDriver.cs`（归战斗线）。

---

## §六 没查到 / 风险 / 给收口的事

1. **`工具/ruleprobe` 需要补三列（禁区，交收口）** —— `工具/ruleprobe/Program.cs:453-458` 的 `LoadPool` **自己构造 `CardDef`**，只传到 `initiative` 为止，**没传** `choicesTargetsAffected/ChoicesMaxTargets/ChoicesSpecificCardIds`。⇒ 探针的 `scan`/`play` 读数**看不到 `choicesDefinition`**，对 `A1960` 的注入面**恒为「没发生」**（这正是 `A1339` 那族「探针与引擎口径不同」）。
   本波**按红线没有改那个目录**；**收口重钉基线时必须先补这三行**（否则基线会把「引擎有、探针看不见」钉死）。建议改法：
   ```csharp
   int ta = c.TryGetProperty("choicesTargetsAffected", out var taEl) && taEl.ValueKind == JsonValueKind.Number ? taEl.GetInt32() : -1;
   int mt = c.TryGetProperty("choicesMaxTargets", out var mtEl) && mtEl.ValueKind == JsonValueKind.Number ? mtEl.GetInt32() : -1;
   var chIds = new List<string>();
   if (c.TryGetProperty("choicesSpecificCardIds", out var ce) && ce.ValueKind == JsonValueKind.Array)
       foreach (var e in ce.EnumerateArray()) if (e.ValueKind == JsonValueKind.String) chIds.Add(e.GetString());
   // … new CardDef(…, choicesTargetsAffected: ta, choicesMaxTargets: mt, choicesSpecificCardIds: chIds)
   ```
   同时给缺列补一条出声（同 `initiative` 那条）。
2. **`A1960` 只注入了 6 张，不是 56 张** —— 刻意收窄，理由（⛔ 不是「影响小不做」）：
   - `40 张` 里 **23 张正文自己就有 choose**（再插一条 = **问两遍**）；
   - `BL29 Terminator Champion`：SO 有 4 张 filter，但**当前 build 里它没有能力列表**（`cardAbilities` 空）⇒ 这是**死数据**，开了面板也没人消费；
   - `Drone Companion`：filter 指向池内 3 张无人机，但我们的 `create` 是「三选一随机造」，**下游不消费选中那张** ⇒ 开面板 = 选了不生效（假装有作用，红线）；
   - `UM109/110/111/112/115/116/117/118` + `Angel's Wrath`：filter 指向 **`_normal`/`_oath` 变体卡 SO**，**不在我们卡池**（`CardDef` 里没有这些卡）⇒ 没有可摆的候选；
   - 其余 `targetsAffected=180` 但 `filterSpecificCards` **为空**的 16 张：原版回落 `BattleManager+0x308`（`pooledAbilityData`），我们**没有**那一格 ⇒ **不猜**。
   这 34 张的**数据都已在卡表里**（`choicesSpecificCardIds` 等），接线时不需要再动数据管线。
3. **`Mekaniak` 无机器判据**（§四）—— 留红/留「没查到」。
4. **`PinnedPlayMd5` 与 `play` 基线未重钉**（按任务要求，收口统一做）。本波改了 3 条 `desc` ⇒ 解析产物**必然变**（那三张各多一个 `create` op）⇒ **收口要重钉基线**。`cards_engine.json` v10 也会让旧基线对不上。
5. **验证手段与局限**：
   - ✅ 秒级 typecheck **0/0**（含新加的 `RuleEngineTest` ⑦，编得过）。
   - ✅ 全池解析对拍（临时离线台，编译 `Core/*.cs` + 真卡池）：**只 6 张变化**；候选域/端到端结算/天赋补丁均已跑通。
   - ⛔ **没有跑** `RuleEngineTest.Run` / `BattleScene.Run` / player（红线）；新增断言**未在 Unity 里执行过**。
   - ⚠️ `ruleprobe` **未参与**本波验证（见第 1 条：它现在还看不到这三列）。
6. **回滚边界（给合并的人）**：本波在 `RuleEngine/Core` 的三个文件里**与波 1 的未提交改动共存**（`//` 段落可分辨：本波的块都带 `2026-10-19` + `A1959/A1960`）。⛔ 别把 `git checkout` 到 HEAD（会连波 1 的 `DoGive`/护甲/`DeployFree` 一起回滚）。
7. **顺手发现（交调度台分流）**：
   - `SW1 Logan Grimnar` 的第一段 `Start the game with Tyrnak and Fenrir in hand` **至今 `unparsed`**（机制由 `StartWithInHand` 另一层承载，所以不报错）—— 这是既有缺口，不是本波引入。
   - `cards_engine.json` 在本次之前**已经落后于源文件**（§二·3 的那 15 条 `descZh` / 12 条 `keywords` / 4 条 `cost` 早就在工作树里、只是没重生成）。下一次任何人改 `cardface_fixes.json` 之后都应顺手跑一次生成器 + `--check`。
   - 波 5 §3·4 的两条读数已过期（§三·3）—— 建议在 `W_引擎线_波5.md` 就地标「已由波 2b 复核推翻」。
