# ID · 卡牌「类型」与「数值字段」的关系

> 只读判据代理 · 2026-10-17 · 主判据 = 原版 2022 卡 SO 全字段快照 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1195 条）
> ＋ 反编译 `d:/2/tools/decomp_full/` ＋ `d:/2/tools/il2cpp_out/dump.cs`。本仓数据 `cards_engine.json`（现读 1127 张）。

## 一、结论摘要（直接回答用户）

**数据层：还在。** 把一张部队卡的卡型字段改成战术卡（`cardType = 20`），它 SO 里的
`manaCost / meleeAttack / rangeAttack / maxHealth / armour` **一个都不会消失** —— 原版 SO 是一个**扁平类**，
所有卡共用同一套字段，没有"按类型裁剪字段"这回事。**原版自己就带着现成例证**：快照里有 **67 对**「本体 + `_enchantment` 副本」，
副本的 `cardType` 全是 **20（Tactic）**，而四项数值与本体**逐项相同（67/67）**。

**行为层：数值在，但没人会用它。** 卡能不能落棋盘由 **`cardType == 0` 这一处硬门**决定
（`BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:393-394`）；改成 20 ⇒ 走战术卡分支（结算 `cardAbilities` → 消散进弃牌堆），
**既不上场、也不进战斗**。数值确实会被无门地抄进实体（`CardSetup.c:44-101`），但**刷新与显示**又都只对 `IsUnit` 生效
（`ResetToValuesInHand.c:47`、`BasicCardUI__SetRawCardData.c:89`）。⇒ **结论：字段留着，语义死了。**

**只改 id、不改类型 ⇒ 什么都不会变** —— id 不携带类型（见 §二）。

## 二、类型存在哪（字段名 + 出处；id 有没有编码类型）

原版是**两个独立字段**，不是从 id 推出来的：

| 字段 | 偏移 | 枚举 | 出处 |
|---|---|---|---|
| `cardType` | `0x94` | `CardTypeOptions`：`Minion=0 / Hero=10 / Tactic=20 / Whispers=40` | `dump.cs:23261`（字段）· `dump.cs:45710-45717`（枚举） |
| `spellType` | `0x90` | `SpellType`：`Normal=0 … OffensiveCard=200 / DefensiveCard=210 / MarkOfChaos=190 …` | `dump.cs:23260` · `dump.cs:45948-45982` |

- 快照实数：`cardType` = 0:540 · 10:60 · 20:592 ·（3 条无类型的是 `AddressablesBuildNumber` + 2 张异画 `#HB_*`）。
  `spellType` 在 Tactic 上还分出 200(84) / 210(36) / 90(6) / 240(4) 等子类。
- **我们的单列 `type` 是把这两个字段合出来的**：`unit/tactic/hero/defence`。
  `defence` = 原版 `cardType=20 + spellType=210` —— 逐张核过 6 张（`ASH55 Aspect Shrine` · `AM61 Defence Batteries` ·
  `ASH56 Inspiring Legacy` · `ASH57 Webway Entrance` · `AM60 Timely Reinforcements` · `AM62 Munitorum Supplies`，
  全部 `ct=20 / st=210 / inv=5`）。生成器口径也写着这条：`工具/gen_cards_engine.py:1124-1129`。
- 🔴 **id 不编码类型**（三种口径都试过）：
  · 阵营前缀同一套：`GOF/ASH/UM/BL/...` 在 Minion / Hero / Tactic 里**都出现**（`GOF56`=unit、`GOF73`=tactic、`GOF27`=hero）；
  · 数字段不分类型（`DA42`/`DA3`/`DA46` 分属 tactic/hero/tactic）；
  · 32 位 hex id **不是战术卡专属**：tactic 232/592、minion 17、hero 10；
  · `_enchantment` 后缀是复制件的命名习惯，不是类型判据（77 张里 75 张 ct=20，另 2 张不是）。

## 三、战术卡的 SO 里到底有没有数值字段（真实例子举证）

**字段一定在**：`RawCardScript` 是一个 118 字段的扁平类（`dump.cs:23244-23357`），
`manaCost 0x78 · maxHealth 0x9C · armour 0xA0 · meleeAttack 0xA4 · rangeAttack 0xA8` 与 `cardType 0x94` **同级并列**。

**值按类型分布**（快照实数）：

| 字段 | Minion(540) | Hero(60) | Tactic(592) |
|---|---|---|---|
| `meleeAttack` | 1–8 有值 | 2:58 / 3:2 | **0:587** · 4:3 · 3:1 · 7:1 |
| `rangeAttack` | 0–12 | 2:58 / 1:2 | **0:587** · 4:4 · 5:1 |
| `maxHealth` | 2–9+ | 35:40 / 40:11 / 20:6 / 30:3 | **0:587** · 6:3 · 5:1 · 7:1 |
| `armour` | 0:478 / 1:46 / 2:15 / 3:1 | 0:60 | **0:592（全零）** |
| `manaCost` | 1–8 | 2:57 / 0:3 | 2:155 · **-1:99** · 1:74 · 3:71 · 4:70 · 0:51 |

- **5 张非零的 Tactic 全是 `_enchantment` 副本**（不是"战术卡有数值"的普通情况）：
  `Deffkopta_enchantment` 3/4/5、`Beastboss on Squigosaur_enchantment` 7/5/7（与本体 `GOF56`/`GOF91` 一字不差），
  以及 3 张 Carnifex 附魔 `Crushing Claws / Devourer Cannon / Armoured Exoskeleton` 4/4/6。
  ⇒ **这 5 张就是"同一张部队卡、类型=20、数值完整保留"的现成样本。**
- `manaCost = -1` 是**"不可直接打出"的哨兵**（99 张 tactic 都是它；进攻卡/环境卡 `spellType=200` 也在其中），
  ⚠️ 与 `inventoryOptions/initiative` 的 `-1` 同一个坑：**别把 -1 当"缺值"读成费用 0**。

## 四、谁读数值、有没有类型门（反编译出处）

| 读点 | 有门吗 | 判据 |
|---|---|---|
| `CardScript__CardSetup.c:44-101`（raw → 实体，抄 `0x9C/0xA0/0xA4/0xA8`） | **无门**（无条件抄） | 三个 `if (…+0x28) == 0` 只防空引用 |
| `BattleCardManager__CreateCard.c:90`（调用 CardSetup） | **无门** | 调用点不看 `cardType`；手牌/牌库/召唤/附魔同一入口 |
| `CardScript__ResetToValuesInHand.c:47-50` | **有门**：先 `RawCardScript__IsUnit` | 非单位回手**不刷新**数值 |
| `BattleManager__CanPlayCard.c:123` → `SupportMethods__IsSpellType.c` | **有门** | `cardType ∈ {20,40}` 才算"法术"（查目标）；单位走棋盘位（`:100` `CardScript__IsUnit`） |
| `BattleManager._ResolvePlayCardFromHand…MoveNext.c:393-394 / :470` | **有门（最关键）** | `cardType==0` → `MinionManager__PlayMinionFromHand`；`==0x14/0x28` → 法术分支 |
| `BasicCardUI__SetRawCardData.c:89-93` | **有门** | 数值/关键词计数器 `Toggle(..., IsUnit, ...)` |
| `AbilityLogic__GetValueFromCriteria.c:45-110` | **无门** | 按 criteria（10=近战/20=远程/30=生命…）读**实体**的 `CurrentMeleeAttack` 等 |
| `EntityScript__get_CurrentMeleeAttack.c` | **无门** | 读实体 `0x7C`（由 CardSetup 从 raw `0xA4` 抄来）＋ buff 列表 |

- 类型的两个判据本体：`RawCardScript__IsUnit.c` = `cardType==0 || cardType==10`；
  `SupportMethods__IsSpellType.c` = `cardType==0x14 || cardType==0x28`。
- ⇒ 精确说法：**"数值还在不在"由数据决定；"数值有没有意义"由 `cardType` 决定，且只决定两件事 ——
  ① 落不落棋盘（`==0` 才落）② 数值刷不刷新/显不显示（`IsUnit` 才做）。结算读值本身不按类型门，
  因为它读的是"已经在场的实体"，而实体是落场才建的。**

## 五、我们这边的对应情况

- **数据**：`cards_engine.json` 只有一列 `type`（`unit/tactic/hero/defence`）；数值列 `cost/attack/health/ranged/armour` 全类型都有。
  现读 **446 张 tactic 的四项数值全为 0**（`attack/ranged/health/armour` 非零 = **0 张**）——**与原版口径一致**。
- **`CardDef`**：构造**无条件**收 `Cost/Attack/Health/RangedAttack`（`Core/CardDef.cs:258`）；
  `IsUnit = Type=="unit" || Type=="hero"`（`CardDef.cs:2779`）；`CanListenForEvents => IsUnit`（`CardDef.cs:2805`）。
- **分路**：`RuleCore.PlayCard` 非单位卡开头就分流 → `EffectResolver.PlayTactic`（`RuleCore.cs:2389` → `EffectResolver.cs:1543`）；
  战术卡那条路不落格位（`landedSlot` 恒 -1）。
- **`UnitState`**：构造**无条件**从 `CardDef` 读四项数值（`Core/UnitState.cs:582-585`），
  但只有真正上场的单位才 `new UnitState(...)` ⇒ 与我们"战术卡不进棋盘"同构。
- ⚠️ **字段兼职的真实先例（题面点名的那条）**：`KeywordTable.Implemented` 那一栏**同时承担**
  「卡面词已登记」与「那个时机真的有代码在读」两件事 ⇒ `DoubleRage` **只能登记、不能进 `Implemented`**
  （`CardDef.cs:3399-3412`：「⛔ 别为了看起来做完了把它塞进 `Implemented`」）。
  ⇒ 本仓确有"一个字段两种语义"的地方，**加列/加表项前先问一句"谁在消费它"**，别按名字推断。

## 六、边界（subtype / trait / keywords / ability 与类型的关系）

- **`subtype`（兵种）**：⛔ **原版 SO 里没有 `subtype` 字段**（118 个键逐个扫过：只有 `spellType/cardType/keyword/attackDamageType/CardTraits/ShowTraits/cardRaceTextReferenceId`）。
  兵种实际存在 **`keyword`（偏移 `0x98`）** 里，枚举是 `Infantry=5/Vehicle=10/Monster=15/Building=20/Battlesuit=25/Drone=30/Beast=35/Daemon=40`
  （`dump.cs:45899-45912`；读取点 `GameStaticData__CardRaceToString.c:29-35` → `MinionRaceToString`）。
  我们那列 `subtype` 来自 `card_stats.json`/PnP 卡面（OCR + 手工修正，见 `gen_cards_engine.py:607-611`），**是第二来源**，别当原版字段名。
  **战术卡**：587/592 `keyword=none`，5 张副本继承本体兵种 ⇒ 对战术卡**基本无意义**；它只用于"a friendly Vehicle"这类**场上目标筛选**。
- **`trait`**：`CardTraits` 全类型都有（tactic **110/592** 非空），条目形如 `{name:"rally", _trait:10, _traitValue:0}`。
  它是**事件触发定义**，消费侧挂在场上实体上（对应我们 `CanListenForEvents => IsUnit`）⇒ 对未落场的战术卡**不触发**。
- **`keywords`**：⚠️ **名字撞车**：原版字段 `keyword` 是**兵种**，卡面关键词在 `CardTraits[]._trait`。⛔ 别按字段名猜语义。
- **`activeAbility` / `cardAbilities` 与类型**：字段全类型都有，但**实际用法按类型分叉**（快照实数）：
  `activeAbility.activeAbilityLogic` 非空 = Minion **53** / Hero **56** / Tactic **0**；
  `cardAbilities` 非空 = Minion 430 / Hero 58 / **Tactic 578**。
  ⇒ **战术卡的效果全走 `cardAbilities`**；"有些效果在 `activeAbility`"那条已知事实只发生在**单位卡**（AM35 是单位卡，不矛盾）。

## 七、还没查清的（⛔ 不猜）

1. 2022 快照**缺新卡**（UM 等 37 张查不到）⇒ 新卡的 `cardType/spellType` 分布未覆盖；`Whispers(40)` 快照 **0 张**，完全未覆盖。
2. `_enchantment` 副本的数值**是否真被某条代码路径读取**：只证到「字段存在 + 布局同形 + CardSetup 无门地抄」，
   **没有逐条追**附魔卡的数值消费点（`CardScript__AddEnchantment.c` 本身不合并数值，它把附魔当独立 CardScript 挂到列表上）。
3. 我们 `cards_engine.json` 的 `type` 列**最初由哪张源表的哪一版**生成（`card_stats.json` / allcards 链），只看了生成器读的是现成列，没往回追到最上游。
4. 原版 `cardType` 在**运行时**有没有被改写的赋值点（铁律 5·c：一个值 ≠ 全部情况）—— 本次只核了序列化初值与读点，**未查赋值点**。
