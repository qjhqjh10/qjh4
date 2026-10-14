# `AB` · 卡牌线 P2/P3 交回的**接线批**（2026-10-15）

> 写手：接线批唯一写手。⛔ 本笔**未动 git**、**未动正本**（四份 + `资料/普查产出_第三十会话/**`）、
> **未跑任何 Unity 批处理**（按用户口径：自检先等等）。
> ✅ 跑过：**秒级类型检查**（运行时 + 编辑器两个程序集 **0 错 / 0 错**，三次）·
> `bash 工具/ruleprobe.sh check`（**读数与改前逐字相同**，见 §五）·
> 两个**仓外**演算台（`d:/4/_tmp_wire/armour` · `d:/4/_tmp_wire/kwprobe`，⛔ 不是交付物）。
> 读数时刻 ≈ **2026-10-15 01:10–01:35**。

---

## 〇、一句话结论（四件各一行）

| # | 件 | 状态 | 一句话 |
|---|---|---|---|
| **①** | `armour` 列的**读点**要切 | ✅ **做完** | 读点收成一处（`_keywords[armour]`，值 = **列优先、旧口径兜底**）；**全池 1127 张行为改动面 = 0 张**（离线演算台逐张量过）；唯一例外 `DA83` 已出声 + 立账 + 上断言 |
| **②** | `A1799` 那 **10 列**接消费者 | ✅ **判完**（**实接 0 列**） | 逐列给「消费点 / 值域 / 本笔接不接」；`attackDamageType` = **另立一笔**（**今天恒不触发**，且机制已用另一种形态落地）；`useOverDraw` = **属表现层，本笔只报**；其余 8 列各自的理由见 §二 |
| **③** | `Rage` / `Frenzied` / `Unstoppable` 进不了 `keywords` | ✅ **做完** | 三个词全部登记；`rage`→`Penitence`、`frenzied`→`Destroyer`（**同 trait 的第二种拼法**）、`unstoppable` = **独立 trait**（新常量 + 接进唯一判定口）；**现池读数改前改后逐字相同**；★ 断言 6 条 |
| **④** | `SOR31` 我们英文 `desc`「多了一句」 | 🔴 **没做 —— 简报前提被现读推翻** | **PnP 成品卡图印的就是「dies or」**（亲读）⇒ 不是「我们多了一句」，是**两代源文本的冲突**（PnP 2026-08-15 vs 官方本地化 2025-07-30）⇒ 改 `desc` 会**删掉一个真机制**且**偏离印刷卡面**。**停手交回**，两条候选修法见 §四 |

**卡池一个字没动**（`cards_engine.json` md5 = `8aa9b9bf5adfa0a7e249c14e8ef56cce`，前后相同）·
**`card_ids.json` / `cardface_fixes.json` / `工具/gen_cards_engine.py` 一个字没动**。

---

## 一、① `armour` 列的读点切换（✅ 做完）

### 1·1 先查清了什么（简报点名要先查的）

`KwValue(KeywordTable.Armour)` 的调用面（现读，全仓 `grep`）：

| 读点 | 位置 | 期望的形状 |
|---|---|---|
| **引擎**（唯一一处真跑结算的） | `Core/UnitState.cs:594` `Armor = KwValue(KeywordTable.Armour);` | **纯 int**（`UnitState.Armor` 是 `int` 字段） |
| 间接（读 `Armor`，不读关键词） | `Core/EffectResolver.cs:7469` `holds = t.Armor > 0;`（`If it has Armour` 那一族） | —— |
| 表现层 | `CardPresentation/Battle/BattleDriver.cs:12681`（手牌卡面显示护甲）· `:12783`（关键词行拼串） | int（**不在本笔白名单**，但它读的是 `CardDef.KwValue`，改法对它**零影响**） |

⇒ **历史输入是字符串**（`keywords` 里的 `"Armour 2."` / 裸 `"Armour"`），**输出是纯 int**。
⇒ 切换**不改任何调用点的签名**：全仓护甲读点收成**一处** = `_keywords[KeywordTable.Armour]` 这一格
（`CardDef` 构造里定值 → `KwValue` / `Has` / `UnitState.Armor` / 表现层全走它）。

### 1·2 🔬 两套口径的**全池逐张对账**（离线演算台，用真编译出来的 `Core/*.cs`）

| 读数 | 值 |
|---|---|
| 全池 | **1127** 张 |
| 旧口径（`keywords` 字符串）> 0 | **81** 张（1×62 · 2×18 · 3×1） |
| 新列（v9 `armour`）> 0 | **80** 张（1×61 · 2×18 · 3×1） |
| **交集**（两处都 > 0） | **80 张 · 值不等的 0 张** |
| **只有列**有值 | **0 张** |
| **只有旧口径**有值 | 🔴 **1 张**：`DA83 Ravenwing Ballistus Dreadnought`（列=0 · keywords=1） |
| **改前 vs 改后的 `KwValue(armour)` 不等** | ✅ **0 张（行为改动面 = 0）** |

⇒ 并集差异只有 `DA83` 一张，而**错的是那一列**（三条判据）：
1. **PnP 成品卡图**（`d:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_09_Ballistus-Dreadnought.png`，
   2026-10-15 **亲读**）：关键词行印 `Armour 1.`，右下角**银盾印 `1`**；同图费用 8 / 近 5 / 远 9 / 生 9
   与我们池逐一吻合（⇒ 是同一张卡）；
2. 它自己的 `desc` 就是 `Armour 1. When you create or play a Secret, …`；
3. 它的 `keywords` 是**裸 `Armour`**（没数字）⇒ 生成器那一侧的 PnP 读出器**没捡到数字**，于是列写了 0。

### 1·3 改法（**两处口径只留一份**）

`Core/CardDef.cs` 构造函数（`:262-290`）：

```csharp
ArmourFromKeywords = KwValue(KeywordTable.Armour);            // 旧口径（只为对账留）
ArmourFromPool = armour > 0 ? armour : 0;                      // v9 的 `armour` 列
if (ArmourFromPool > 0) _keywords[KeywordTable.Armour] = ArmourFromPool;
```

- **列 > 0 ⇒ 用列**（**80** 张，与旧口径值逐张相同）；
- **列 = 0 而旧口径 > 0 ⇒ 旧口径兜底**（就是 `DA83` 那一张）——
  ⛔ **不能删这一支**：删了它 `DA83` 会**静默少 1 点护甲**（与 PnP 卡面相反）；
- **两处都有且不等 / 只有一处有 ⇒ 出声**（`CardDatabase.Parse` 装载时 `LogWarning`）+ 自检断言。

新增两个**只读对账列**（⛔ 不许拿去结算）：`CardDef.ArmourFromPool` / `CardDef.ArmourFromKeywords`
（`CardDef.cs:195` / `:206`，doc 在 `:181-206`）。

### 1·4 改了哪些文件哪些行

| 文件 | 行 | 改动 |
|---|---|---|
| `Core/CardDef.cs` | **`:210-217`**（ctor 加 `int armour = 0`） | 新增一个可选参数（末尾，**不影响任何既有调用点**） |
| 同上 | **`:262-290`** | 新增「护甲读数切换」那一段（含口径、兜底、为何不能删兜底、判据） |
| 同上 | **`:181-206`** | 新增 `ArmourFromPool` / `ArmourFromKeywords` 两个只读属性 + doc |
| 同上 | **`:3360` 附近**（`KeywordTable.Armour` 的 doc） | 补「值的来源有两条腿、判定只此一处」+ 现读数 |
| `Data/CardDatabase.cs` | **`:284-288`** | `new CardDef(...)` 补 `armour: c.armour` + 调对账口 |
| 同上 | **`:327-357`** | 新增 `static void ReportArmourDisagreements(List<CardDef>)`（只出声、不改值） |
| 同上 | **`:76-111`**（`CardDto.armour` doc） | 铁律 5 就地订正：原写「消费者尚未切换」→ 现写「**已切换**」+ `DA83` 例外 + 判据 |

### 1·5 加了哪些断言（含判别式）

`Editor/RuleEngineTest.cs`：

| 位置 | 断言 | 判别式（**结构上不可能同时满足**） |
|---|---|---|
| `TestKeywordParsing` `:2050-2066` | **3 张造出来的卡**：① 只给列（3）⇒ `KwValue=3` 且 `Has=true`；② 列=2 / keywords=`Armour 1` ⇒ **列赢**；③ 列=0 / keywords 裸 `Armour` ⇒ 兜底 **1** | 「只把列接上不读它」「只留旧口径」「两处都读」都至少红一条 |
| `TestCardDatabase` `:2111-2140` | 全池对账：**交集值不等 = 0** · **只有列有值 = 0** · **只有旧口径有值只放行 `DA83`**（白名单只这一张，新出现的任何一张会红）· 交集 ≥ 70 | 「有人改了列的值」「有人改了旧口径」⇒ 前者落「不等」、后者落「只有列」；两处一起改回同一写法也**过不了**「交集 ≥ 70 + `DA83` 值锁」 |
| 同上 `:2138-2140` | **值锁**：`DA83` 的有效护甲 = **1**（列 0 · 旧口径 1 · PnP 银盾 1） | 删掉兜底那一支 ⇒ 立刻红 |

### 1·6 没查清的部分

- 生成器那一侧**为什么会漏 `DA83`**（PnP 那一层读 `keywords` 的 `Armour N`、`fix_own_armour` 从 `desc` 正则补，
  两条都没覆盖到裸 `Armour` + `desc` 有 `Armour 1.` 的这张）—— **属 `工具/gen_cards_engine.py`，不在本笔白名单**，只报。
- `armour` 列会不会还有**别的**缺陷（本笔只证明「与旧口径 + PnP 不对的只有这一张」）。

---

## 二、② `A1799` 那 10 列接消费者（✅ 判完 —— **本笔实接 0 列**）

> 逐列判据的**反编译出处**见下表；本节结论与 `X_卡牌P2.md` §二·`A1799` 表**不冲突**（它只报「落了值、无消费者」）。

| 列 | 有消费点吗（判据） | 值域 / 形状（现池） | **本笔接?** |
|---|---|---|---|
| **`attackDamageType`** | **有，但不是结算**：卡 SO 那一列全库**只有 3 处引用**，唯一真消费点 = **AI 评分**（`decomp_full/AI__ScoreFromBuffedAttack.c:34,42,52,60,67`）；`EntityScript__get_attackDamageType.c:3` 只是字段读口、`BasicCardUI__get_AttackDamageType.c:3` **全库 0 调用** | `4`×965 · `0`×44 · `-1`×118（**非零值只有 `4` 一种**） | 🔴 **另立一笔**（那一笔的结论大概率是「**无可接**」）—— 理由见 §二·1 |
| `cardShortId` | ⛔ 无（`decomp_full` 按名 0 命中） | `-1`×118，其余 1…914 | ⛔ 不接（且**不许当卡 id 换** —— 与 `A970` 那套 0/763 重合） |
| **`useOverDraw`** | ✅ **有**：`CardDisplayWindow__ChangeCardPosition.c:75-81` → `BasicCardUI__SetImageOverDraw.c:2` → `CardImageController.ChangeSize/ChangeState`；我们侧承载层**已经建好** = `CardPresentation/Core/CardView.cs:1983-1986`（层的建点）/ `:5330`（`UseFrontLayer`，那句注释还写着「值在服务器、我们拿不到」= **已过期**） | `1`×572 · `0`×437 · `-1`×118 | 📤 **属表现层 ⇒ 本笔只报**（`CardPresentation/**` 不在白名单） |
| `cantBeRandomlyCreated` | 🔴 **未证**：`decomp_full` 按名 **0 命中**，但它是 `public bool`（`RawScript.cs:11`）由代码**直接按偏移读** ⇒ **「按名搜不到」不能证伪**；我们侧「随机生成」那道闸门今天只看卡面载荷（`Core/CreatePool.cs:168-233`），**没有这道闸** | `0`×860 · `1`×149 · `-1`×118 | ⛔ **不接**（判据不足 —— 要接得先推出它的偏移并读到消费点） |
| `expansionPack` | ✅ 有（**属商店线**）：`CardShopFilter.cs:11-13` 的 `blacklistedExpansions` · `CrateItemData.cs:3-5`；枚举 `CardExpansionType.cs:1-19` | `0`×828 + 13 种 | 📤 另立（商店 / 开包池那一线） |
| `refNameId` | 有（`RawCardScript__GetCardNameTerm.c:19-26` → I2） | **现池全空**（`""`×1127） | ⛔ 不接（**无数据**） |
| `itemDescriptionKey` | 有（`ObtainableItem.cs:6-7`，沿奖励/商店 UI） | **只有 36 张**、**只有一种值** `Item_Description/DefenceCardExplanation` | 📤 另立（数据只有 36 张、落点 UI 未定） |
| `fullHeroSpriteReference` | ✅ 有：getter `RawCardScript__get_fullHeroSprite.c:22-41`，**19 处调用**（`DeckInfoPopup__Open.c:62` · `WarlordMasteryContainer__OnInitialize.c:117` · `TutorialModePopup__SetArmyButtons.c:42` …） | 50 张（资产名 `UI_Deck_Warlord_<卡名>` 形态 或 32 位 GUID） | 📤 另立（卡组 / 督军大图那一线） |
| `eventSpriteName` | ⛔ **有 getter、全库 0 调用**（`RawCardScript__get_eventHeroSprite.c:26-38`；对比 `fullHeroSprite` 19 处） | 44 张、**值全同** `Warlord_events_Loken` | ⛔ 不接（**消费点为 0**） |
| `targeted` | ✅ 有（12 处：`BattleManager__CanPlayCard.c:128` · `__NeedToChooseMinionTarget.c:15` · `CardScript__MoveIntoFieldFromHand.c:53,62` …） | `1`×212 · `0`×797 · `-1`×118 | ⛔ **只落值**（**与原版不等价**，见 §二·2） |

### 2·1 为什么 `attackDamageType` **本笔不接**（判据链）

1. **`DamageType`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DamageType.cs:1-15`，13 个成员）
   = `Normal 0 · Ranged 1 · Undersupplied 2 · Ability 3 · **Attack 4** · Random 5 · Blast 6 ·
   **Vulnerable 7 · VulnerableCombat 8** · Poison 9 · RandomPoison 10 · Stomp 11 · RandomAbility 12`。
2. **`GameStaticData__IsVulnerableDamage`** 方法体只有一句语义：`(uint)(t - 7) < 2`
   ⇒ 真值 = **`t ∈ {7, 8}`**。全库 3 处调用：`CardScript._ReceiveDamage_d__381__MoveNext.c:197` · `:291` ·
   `AI__ScoreFromDamagingBuffedUnit.c:78`。
3. 🔴 **`attackDamageType` 不参与伤害结算**：原版造 `DamageInfo` 时攻击伤害**硬编码 `4`**
   （`BattleManager__ResolveAttackDamage.c:33` · `BattleManager__RecordAttackPendingDamage.c:113,122`），
   技能伤害的类型取自**技能数据**（`AbilityLogic__PlayAbility.c:1488`）
   ⇒ **「卡面攻击伤害类型」这条路在原版真战里根本不存在**。
4. 🔴 **同一条语义我们已经用另一种形态落地了**：`RuleCore.cs:5744-5747` 的 `bool alreadyVulnerableHit`
   （注释自陈「我们引擎不携带伤害类型 ⇒ 用这个显式标志表达同一个意思」）· 产生点 `:5832` ·
   消费点 `:5887`；原版那三条守卫（跳过 / 第二发 / 高亮）在我们引擎里**齐了**。
5. 🔴 **今天恒不触发**：现池非零值**只有 `4` 一种**（快照 1195 张也只有 `4`/`0`），
   `IsVulnerableDamage(0/4)` **都是 false** ⇒ 就算现在接上，**行为差异 = 0**。
6. ⇒ **另立一笔**，并写明：那一笔该做的是「把 `alreadyVulnerableHit` 与原版三条守卫逐条对账」
   （**不是**给 `Hurt/ApplyDamage` 加一个真伤害类型参数 —— 那不是原版的做法）。

### 2·2 为什么 `targeted` **不替换现有推断**

原版那一列是**人手填的「打出这张牌要不要点目标」**（play-time，只对法术/单位两种卡种生效：
`CanPlayCard.c:128` 要求「法术且 targeted ⇒ 必须有合法目标」、`NeedToChooseMinionTarget.c:15` 要求
「单位且 targeted ⇒ 入场选目标」）；我们那套是**按效果文本在触发时推**
（`Core/EffectSpec.cs:40` `EffectTargets.NeedsPick(target) => target == "enemyunit"`；调用面
`RuleCore.cs:7283,7311,7707` · `SimpleAI.cs:379`）。**两者不在一个平面上** ⇒ 与 `CardDto.targeted`
的 doc 一致：**只落值、⛔ 不替换推断**（要替换得先跑引擎对出「差异清单 + 每条该信谁」）。

> ⚠️ **本笔对 ② 没有改任何一行代码** —— 逐列结论都是「不接 / 另立 / 转交表现层」。
> 简报里那句「卡池张数会变」的前提**没有触发**（本笔一个字没动卡池）。

---

## 三、③ `Rage` / `Frenzied` / `Unstoppable`（✅ 做完）

### 3·1 先查清：这三个词**该不该认**

| 词 | 原版是什么 | 卡面印什么（**亲开 PnP 图**） | 引擎里有没有实现 | 结论 |
|---|---|---|---|---|
| `rage`（`DefinedTrait.rage = 280`） | **trait 的枚举名** | `SOR10 Sisters Repentia` = `… 。` + **[骷髅图标] `Penitence: Gain +2 ✊`** ⇒ **卡面字是 `Penitence`** | ✅ **有** —— `RuleCore.cs:5985`（受伤触发）· 在 `Implemented` · 在 `Prefixes` | **该认** ⇒ **登记为「第二种拼法」→ 归并到已存在的 `Penitence`** |
| `frenzied`（`= 680`） | 同上 | `SAU18 Skorpekh Destroyer` = `[骷髅] Remnant.` **[爪拳图标] `Destroyer.`** `[回转] Flank` ⇒ **卡面字是 `Destroyer`** | ✅ **有** —— `RuleCore.cs:3550` 那一带（只能打「可被摧毁 / 打得死」的目标） | **该认** ⇒ **归并到已存在的 `Destroyer`** |
| `unstoppable`（`= 180`） | **独立 trait** | `SAU7 Canoptek Wraith` 关键词行**只有** `Remnant. This troop can ignore enemy units with Vanguard when attacking` —— **全图没有 `Unstoppable` 这个字** | ✅ **有机制、但入口不是它** —— `CardDef.IgnoresVanguard`（`CardDef.cs:1275`/`:1370`，由卡面那句英文认出来），读点 `RuleCore.cs:3560` | **该认** ⇒ **独立常量 + 接进同一个判定口** |

**三条独立判据**（都现核）：
1. 官方本地化表：`Card_Trait/rage` = **`Penitence`/忏悔** · `Card_Trait/frenzied` = **`Destroyer`/毁灭者**
   （另有一条**化石键** `Card_Trait/frenzied_old` = `Frenzied` ⇒ 改名史的实据）；
   `Card_Trait/unstoppable` **不存在**（全 9441 条里只有成就标题等命中 ⇒ 它没有卡面/提示框文字）。
2. 卡面（上面那张表，**亲开 3 张图**）。
3. 全池 **10 张** `cardTraits=Frenzied` 的卡，`keywords` **全部**带 `Destroyer`。
4. `unstoppable` 的原版读点 = `decomp_full/BattleManager__IsValidAttackTarget.c:149`
   （攻击者带 `0xb4` ⇒ **跳过**「敌方场上有 Vanguard 就只能打 Vanguard」那道硬闸）；
   旁证 = `FilterMethods__MeetsConditionCriteria.c:39` · `TraitDefinitionDisplay__GetAbilityList.c:124`
   （后者：带 `0xb4` 就把 `flying` 从展示列表里删掉 ⇒ **它和 `flying` 共用展示位**，
   这也解释了它为什么没有自己的卡面文字）。

> ⛔ **简报里给的两条反编译线索是【假证据】，已排除**（写进报告免得下个会话再撞）：
> `AbilityLogic__PlayAbility.c:385` 的 `iVar8 != 0xb4` 是**ability 类型 switch**；
> `AbilityLogic__GetTargets.c:89` 的 `iVar6 == 0xb4` 读的是**对象字段**。都不是 trait 查询。

### 3·2 改法（**改前**：三个词 `Normalize` 全返回 null ⇒ 写了会被静默丢弃，`Dropped` 记 3 个）

| 文件 | 行 | 改动 |
|---|---|---|
| `Core/CardDef.cs` | **`:4258-4259`** | `new[] { "rage", Penitence },` · `new[] { "frenzied", Destroyer },`（**指向已存在的常量** ⇒ 只多入口、不多规则） |
| 同上 | **`:4277`** | `new[] { "unstoppable", RuleCore.Unstoppable },` |
| 同上 | **`:4226-4300`** | 整段注释：三条判据 / 为什么 `rage`⇄`frenzied` 是「第二种拼法」/ 前缀撞车现核 / **影响面实测** |
| 同上 | **`:3849`** | `Implemented` 补 `RuleCore.Unstoppable`（机制真接上了，不进会白打 `*`） |
| `Core/RuleCore.cs` | **`:3283-3318`** | 新增 `public const string Unstoppable = "unstoppable";` + 判据 doc（放本文件照 `Noncombatant`/`Unstunnable` 先例） |
| 同上 | **`:3556-3562`** | 读点接上：`… && !(attacker.Card != null && attacker.Card.IgnoresVanguard) && !attacker.Has(Unstoppable))` —— **一条规则、两个入口**（唯一判定口还是这一句 `&&`） |

### 3·3 影响面实测（两个方向都量了，**改前 vs 改后**）

| 读数 | 改前 | 改后 |
|---|---|---|
| `Normalize("Rage")` / `("Frenzied")` / `("Unstoppable")` | `null` / `null` / `null` | **`penitence` / `destroyer` / `unstoppable`** |
| 被静默丢弃（`KeywordTable.Dropped`） | **3 个** | **0 个** |
| `CardDef(keywords=["Rage"]).Has("penitence")` | `False` | **`True`（值 1）** |
| 全池 `Has(penitence)` / `Has(destroyer)` / `Has(unstoppable)` | **1 / 10 / 0** | **1 / 10 / 0**（逐字相同） |
| **以这三个词开头的卡面串**（扫 `keywords` 每一项 / `desc` / `descZh`，按 `[.:;。！？\n]` 切句） | **0 条** | 0 条 ⇒ **不改动任何现有解析结果** |
| 前缀撞车 | `rage` 与 `rally`（`r-a-g` vs `r-a-l`）· `frenzied` 与 `fast`/`flank`/`ferocity`/`flying`（`f-r` vs `f-a`/`f-l`）· `unstoppable` 与 `unstable`（`unst-o` vs `unst-a`）**都不撞**；反向（新词抢先吃老词）也不成立 | 同 |

### 3·4 加了哪些断言（含判别式）

`Editor/RuleEngineTest.cs`：

| 位置 | 断言 | 判别式 |
|---|---|---|
| `TestKeywordParsing` `:2072-2088` | 三个词各造一张卡：`Rage`⇒`Has(penitence)` · `Frenzied`⇒`Has(destroyer)` · `Unstoppable`⇒`Has(unstoppable)`；裸写取 1；**三个词都不在 `Dropped` 名单里** | 「只加断言不改 `Prefixes`」（返回 null）与「改了表但落到别的键」都会红；`Dropped` 那一条直接断「静默丢弃已消失」 |
| `TestCardDatabase` `:2155-2173` | 全池 `Has(penitence) = 1` · `Has(destroyer) = 10` · `Has(unstoppable) = 0`（**登记前后必须一字不变**） | 只加词不改池 ⇒ 三个数都不动；**前缀撞车** ⇒ 至少一个动（这就是「只加保险」与「抢走了别人的文本」的判别式） |

---

## 四、④ `SOR31 Sister Dogmata` —— 🔴 **没做，停手交回**（简报前提被现读推翻）

### 4·1 简报的前提 vs 现读事实

简报写：「我们 `desc` = `… If the target **dies or** is a friendly unit …`，官方 EN = `… If the target **is** a friendly unit …` ⇒ **我们多了一句**」。

🔴 **亲开 PnP 成品卡图**（`d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_32_Dogmata.png`，2026-10-15 读）：

```
[闪电图标] Rally: Deal 1 damage. If the target dies or is a friendly
unit, gain 1 [太阳图标]
```

⇒ **卡面印的就是「dies or」**，与我们 `desc` **逐字一致**（`card_stats.json` 的 `ocrSrc` 指的就是这张图 ⇒ OCR 没抄错）。
⇒ **不是「我们多了一句」，是【两代源文本冲突】**：

| 源 | 文本 | 年代 / 性质 |
|---|---|---|
| **PnP 成品卡图**（v1.5-3） | `… If the target **dies or** is a friendly unit …` | **2026-08-15**；`施工设计 §七 已裁⑥` 写明它「**与我们要复刻的客户端同期**」，且**效果文字也在「PnP 优先」那一档里** |
| 官方本地化 `Card_Description/SOR31` | `[[rally]]: ((Deal_1_damage)). If the target **is** a friendly unit, gain ((Faith_cost_x,1))` | **2025-07-30 快照**（比 PnP 旧） |
| 真值快照（2022 时代） | —— | 更旧 |

**⇒ 按本仓既有口径（铁律 7「拿不准就看成品卡图」+ 已裁⑥「PnP 印了的内容 PnP 优先」+
`A1876`/`SOR29` 的同型先例「PnP 现行印刷版胜」），我们 `desc` 是**对的那一侧**。**

### 4·2 为什么我**没有**动 `desc`

1. 改它 = 让我们的卡面印一句**与原版成品卡图不同**的话（**复刻偏离**）；
2. 改它 = **删掉一条真机制**：`If the target dies` 是死亡观察那一路的载体（改 `desc` 会动 op 树），
   而引擎解析的原文**就是** `desc`；
3. **J3 红的成因不是「`desc` 错」**，是「`desc`（PnP 那代）与 `descZh`（官方那代，P3 刚换）
   **不是同一代文本**」—— 中文那半边现在**少了一句**（玩家看到的：英文说死亡也给能量、中文没说）。

### 4·3 两条候选修法（**都要主对话裁**；⛔ 我一条都没做）

| 选项 | 改法 | 依据 | 代价 |
|---|---|---|---|
| **A（我倾向）** | **补齐中文** ⇒ 在 `数据/游戏数据/cardface_fixes.json` 的 `descZh` 加一条 `Sister Dogmata`：`[[rally]]：((Deal_1_damage))。如果目标死亡或为友方单位，获得 ((Faith_cost_x,1))` | 铁律 7 + 已裁⑥（PnP 胜）⇒ 中文应当是**PnP 那代英文的翻译** | 会盖掉 P3 刚落的官方中文（那一张）；且**要重跑生成器才生效** |
| **B** | 照简报改 `desc` 为官方 EN（去掉 `dies or`） | `A1995` 那条「英文列对齐官方」的口径 | 与 PnP 卡面不符；**删一条机制**；同样要重跑生成器 |

🔴 **两条都卡在同一处**：落点文件（`cardface_fixes.json` / `CardPool`）**都只是生成器的输入**，
**不重跑 `工具/gen_cards_engine.py` 就不会进卡池** —— 而那个脚本**不在本笔白名单**（另有写手）。
⇒ 这一件**不是「改一行字符串」那么简单**，请主对话先裁口径、再决定由谁连带生成器一起做。

### 4·4 顺手核的「同族」（简报要求）

方法：把现池 1127 张的 `desc` 与官方 `Card_Description/<id>` 的 **EN 列**逐张比 ——

| 分类 | 张数 |
|---|---|
| 官方表**没有**这张卡的条目 | 118 |
| 官方 EN 的值是**坏值**（去空白后 < 3 字符：`(` ×91 / `[` ×89 / 其它 ×1） | 149（P3 报的 181 里有 149 张落在我们池里） |
| 官方 EN 带富文本标记（`[[…]]` / `((…))` / `$$…$$` / `<br>`）⇒ **无法逐字比** | 858（`SOR31` 落在这类里 ⇒ 所以简报与 P3 是**逐字段人眼比**出来的） |
| **官方 EN 是纯文本、可逐字比** | **102** |
| 其中**逐字相同** | **92** |
| 其中**不同**（10 条里 **6 条只是富文本标记**：`<link=…><sprite name=…>` / `<nobr>`） | **4 条真差异** |

**真差异 4 条（含 `SOR31`）**：
| 卡 | 我们 `desc`（多出的那半句加粗） | 官方 EN |
|---|---|---|
| `SOR31 Sister Dogmata` | `… If the target **dies or** is a friendly unit, gain 1 Energy.` | `… If the target is a friendly unit, gain ((Faith_cost_x,1))` |
| `ASH48 Cosmic Serpent` | `Trigger the abilities requiring Spirit Stones of all your troops. **2 [Spirit Stone]: Repeat this effect**` | `Trigger the abilities requiring Spirit Stones of all your troops` |
| `SAU58 Implacable Conqueror` | `Lower the cost of all troops in your deck by 2. **Draw a troop**` | `Lower the cost of all troops in your deck by 2` |
| `TAU51 Saviour Protocolst` | `Your Drones cost 1 less for the rest of this battle. **Choose a Drone and add it to your hand**` | `For the rest of this battle, your Drones cost 1 less` |

🔴 **四条的形态完全一样：我们**（= PnP 卡面那代）**比官方 EN 多一句或半句** ⇒ 这不是 4 个孤立错，
**是同一件事的 4 个实例**（官方本地化表比印刷卡面旧 / 少更新）。⇒ **⛔ 别一条一条改**，
应当**整族一起裁**（选项 A 或 B 对整族一起用）。⚠️ 这 4 条的 PnP 卡图**我只亲读了 `SOR31` 那一张**，
另外 3 张**没开图**（如实记）。

---

## 五、`ruleprobe check`（本笔**没重钉任何指纹**）

`bash 工具/ruleprobe.sh check` ⇒ **rc=1**，四个读数与**改前逐字相同**：
`全池 1121 张 · 解析差异 1 行 · 改名 0 · 新增 172 · 消失 171`，唯一那行差异仍是 **`GSC29`**（`A1989` 那笔）；
哨兵**失败 1 条** = **⑦ 输出指纹**（主对话刻意未钉）。
🔴 ⇒ 本笔改的三处（`Prefixes` 三词 · 护甲读数 · `IgnoresVanguard` 判定口）**对解析基线是隐形的**。
卡池 md5 = `8aa9b9bf5adfa0a7e249c14e8ef56cce`（**未动**）。

---

## 六、顺手发现（**只报，一条都没改**）

1. 🔴 **`工具/gen_cards_engine.py` 的排除表现在过期**：那里有一张
   `TRAIT_ENGINE_UNREGISTERED = {"frenzied","rage","unstoppable"}`（约 `:843`），把这三个词挡在 `keywords` 外
   —— 本笔之后**引擎已经认得出它们**了 ⇒ 两张口径现在并存。
   **要动生成器那一侧**（不在本笔白名单），否则「引擎认得 / 生成器仍排除」会一直是两处写法。
2. 🔴 **`Core/RuleCore.cs` 里 destroyer 那一段写着「判据查不到」—— 不成立**（铁律 5 该就地订正）：
   原文写三条论据（`IsValidAttackTarget` 里没有 destroyer 这一支 · `grep -i destroyer decomp_full` 0 命中 ·
   `Destroyer` 不是 `DefinedTrait`），**三条全不成立** —— 那一支**就在**
   `BattleManager__IsValidAttackTarget.c:207`（trait 用**枚举值 `0x2a8` = frenzied** 写）；
   **IL2CPP 的 C dump 里永远不会有枚举成员名** ⇒ 「grep 不到名字」**不能**当「原版没有」的证据。
   ⚠️ 它同时影响「破坏者那条要不要照 `CheckIfAttackKillsTarget` 改写实现」—— 那是**另一笔**，本笔没碰。
3. ⚠️ **`CardDef` 的 `cardTraits` 显示名与卡面字不同名**：`gen_cards_engine.py:807-812` 的 `TRAIT_DISPLAY`
   把 `frenzied`→`"Frenzied"`、`rage`→`"Rage"`（官方真值是 `Destroyer` / `Penitence`），
   并把 `unstoppable`→`"Unstoppable"`（官方**根本没有**这个显示名，是照惯例 Title Case 造的）。
   ⇒ 那一列现在**读起来像卡面字、其实不是**（本轮提问的混淆源就是这个）。建议改成官方显示名 / 标明「无显示名」。
4. ⚠️ **`doubleRage`（trait 300）也没登记**（全池 **0 张**卡带它；原版读点 = `CardScript__ResolveDamageDealt.c:101`，
   用**十进制 `300`** 写）⇒ 与本笔那三个词同族的保险，本笔**没加**（简报只点了三个词）。
5. ⚠️ **`EffectResolver.cs:6967` 授予护甲时只写 `u.Armor`、不同步 `_keywords["armour"]`**
   ⇒ 今天**不是活缺陷**（全仓只有 `t.Armor > 0` 在读它，处 `EffectResolver.cs:7469`，没有一处靠 `Has(armour)` 判场上单位），
   但它是「同一件事两种表示」的一处，**值得立账**。
6. ⚠️ **`DA83` 的卡名**：我们叫 `Ravenwing Ballistus Dreadnought`，PnP 卡面印 `Ballistus Dreadnought`
   （`card_stats.json` 里另有 `ocrName = "Ballistus Dreadnought"`）⇒ **卡名差异**（谁对**没查清**，与 `A970` 那条线同族）。
7. ⚠️ `资料/关键词三列对账.md:65` 那张「只剩 `concussive`/`frenzied`/`rage`/`unstoppable`/`none` 五种没做」的表
   —— 现读实际是「**5 个名字对不上**」而不是「5 条机制没做」：
   `frenzied` = `destroyer` ✅ · `rage` = `penitence` ✅ · `unstoppable` = `IgnoresVanguard` ✅（本笔起也认这个名字）·
   `concussive` → `concussion` ✅ · `none` = trait 0（生成器明写丢弃）。**该表应就地改写**（不在本笔白名单）。
8. ⚠️ **`cantBeRandomlyCreated` 的「读取点没找到」只能说「未证」**：它是 `public bool`、代码**直接按偏移读**
   ⇒ 按名 grep 0 命中**不能证伪**（`decomp_full` 现有 **25,096** 个方法文件）。

---

## 七、没查清 / 交回主对话

1. 🔴 **`SOR31` 那一族（4 条）该改哪一列** —— 口径没定（§四·3 两个选项），**本笔停手**。
2. 🔴 **`gen_cards_engine.py` 的两处**（`TRAIT_ENGINE_UNREGISTERED` 排除表 · `TRAIT_DISPLAY` 显示名）
   —— 都在生成器，本笔白名单外。
3. ⚠️ `DA83` 那个 `armour` 列缺陷的**根因**（生成器 PnP 读出器对「裸 `Armour` + `desc` 有 `Armour 1.`」这一格没覆盖）
   —— 同属生成器那一侧。
4. ⚠️ `cantBeRandomlyCreated` 的偏移 / 消费点 · `refNameId` 的偏移（P2 报的「按桩字段序推断、未证」）。
5. ⚠️ `fullHeroSpriteReference` 50 条里 GUID 形态占几条（`guid_map.tsv` 没含那一类引用）。
6. ⚠️ `attackDamageType` 那一笔要做的**不是接列**，而是「`alreadyVulnerableHit` 与原版三条守卫逐条对账」
   （另立账；本笔只给了判据链）。
7. ⚠️ 官方 EN 与我们 `desc` 的**全量**对账只做到「官方为纯文本的 102 张」那一层
   （另 858 张带富文本标记，要渲染器才能比）。

---

## 八、本笔账（给主对话合并进正本用的一段）

> **`AB` 接线批（2026-10-15）**：① `armour` 读数已切到 v9 列（**行为改动 0 张**；`DA83` 列缺陷 ⇒ 旧口径兜底 +
> 出声 + 断言 + 值锁）· ② `A1799` 十列逐列判完、**实接 0 列**（`attackDamageType` 另立 ·
> `useOverDraw` 转交表现层 · 其余不接/另立）· ③ `rage`/`frenzied`/`unstoppable` 三词登记
> （前两个归并到 `Penitence`/`Destroyer`，第三个独立常量并接进唯一判定口；**现池读数不变**；断言 6 条）·
> ④ 🔴 `SOR31` **未做** —— **PnP 卡面印着「dies or」**（亲读），是命令行前提被现读推翻；同族 4 条
> （`SOR31`/`ASH48`/`SAU58`/`TAU51`）**整族等口径**。**新账 3 条**：生成器排除表过期 ·
> `RuleCore` 的 destroyer「判据查不到」不成立 · `TRAIT_DISPLAY` 显示名≠卡面字。
> typecheck **0/0** · 卡池未动 · `ruleprobe check` 读数与改前逐字相同 · **未跑 Unity 自检**。
