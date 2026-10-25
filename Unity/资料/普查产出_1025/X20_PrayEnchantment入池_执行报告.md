# `X20`：`Pray Enchantment` 按卡数据链入池 —— 执行报告（2026-10-25 · 执行代理）

> 范围：`Assets/RuleEngine/**` · `工具/gen_cards_engine.py` · `Assets/RuleEngine/Editor/RuleEngineTest*.cs`。
> ⛔ 未跑 Unity · 未碰 git · 未动 `CardPresentation/**` 与任何 `任务总表.md`。
> 全表（118 行逐张）→ **`资料/普查产出_1025/X20_附魔能力表.tsv`**（本报告 §五 只展开「有池内父卡」那 32 行）。

## 一、卡型支持（改了什么）

| 位置 | 改动 |
| --- | --- |
| `Core/CardDef.cs:65-83` | `Type` 的 doc 加第五档 **`enchantment`**（原版 `cardType 20` 的**隐藏附魔模板**；不进卡组/手牌/收藏、不可造、不可打出） |
| `Core/CardDef.cs:5870-5877` | 新增 **`IsEnchantment`**（判据只此一处） |
| `Core/CreatePool.cs:131-146 / 181 / 211 / 256-260` | 造牌候选池四处（`or` 名单 / 兵种池 / 阵营全体 / 具名）**一律排除** `IsEnchantment` |
| `Core/RuleCore.cs:3137-3142` · `Core/EffectResolver.cs:1889-1895` | `CanPlayCard` / `CanPlayTactic` 显式 `ErrNotPlayable`（`desc` 为空 ⇒ 否则会被当「可打但什么都不做」，花 2 费静默） |
| `Data/CardDatabase.cs:2960-2975` | `OfFaction` **不再返回**附魔模板 —— 下游有 `BoosterPackOpenWindow.RollPack`（`System.Random(seed)` 摇号），混进去会**静默改掉每一包内容** |

**「新增类型是否多余」的判定（协调者问的）：保留，且有必要。** 把这张卡标成 `tactic` 会在**不改任何代码**的情况下被三处当成真战术卡：
`CreatePool` 的「阵营全体」支（**没有** Type 过滤 ⇒ 会被 `create` 造出来）、`CountEval.cs:619`（`includeSpells` 计数把它算进法术）、
`CardDatabase.OfFaction`（开包摇号）。新值 `enchantment` 在**所有** `Type ==` 分支里都**落空**（fail-closed）——
这正是少踩 `switch` 岔路的方向；反过来标成 `tactic` 才是给将来埋一处静默走错。

## 二、生成器：判据驱动，不是名字清单

`工具/gen_cards_engine.py`：`ENCHANTMENT_EXECUTORS`（`:3819`）+ `build_enchantment_entries()`（`:3843`）+ 主循环之后追加（`build()` 内）。

**入池判据** = ① 被**池内卡**以 `cardAbilities[].abilityLogic[].buffEffect{buffType ∈ {12,15,16}, enchantment→它}` 引用
**∧** ② 它自己 `cardAbilities[].abilityLogic[]` 的 `(trigger, chosenAbility)` 形状**逐条命中**一个**已实现执行点**。
两条都不硬编码卡名：`ENCHANTMENT_EXECUTORS` 里的 `pairs` 是执行点假设的**形状**，数据一漂就 `sys.exit`；
`ids` 是执行点自己的适用范围（今天那两跳在 `RuleCore`/`Enchantments` 里**按 id 认卡** —— 如实写出来，不假装通用）。
另加两条硬闸：`inventoryOptions == 10 ∧ showInCollectionOptions == 0`（隐藏位一漂当场炸）；id 不得与主循环撞。

**现读数**：名字含 `enchant` **120** 张 → `cardType 20` 模板 **118** ＋ 兄弟卡 **2**（`Wurrboy`/`Sergeant Gadriel`，`cardType 0`，本体是部队卡、两张都不在池内）。
118 张里：**有池内父卡 32 张**（其中只有 `Pray` 命中执行点 ⇒ **入池 1 张**）· **无池内父卡 86 张**。
⛔ **不把另外 31 张也塞进池**：它们的父卡挂载**本身**就没有执行链（见 §五 缺口①），入池 = 有数据没人用的死卡，
且会改掉 `create`/开包按阵营摇号的确定性结果。

**卡表**：`version 29 → 30`（**加卡不加列**；`CardDatabase` 只有 `< N` 守卫、无「> N」守卫 ⇒ 不 bump 也读得进来，bump 是为了让卡池变更有版本可认）。

## 三、接线前后行为等价性（`X13` → 卡数据）

`UnitState.PrayEnchantment`（bool）**已删除**（`UnitState.cs:218` 留一段「为什么没有这个字段」）。
唯一存放处 = `CardInstance.ActiveEnchantments`；入口全在 `Enchantment.cs`：`FindPrayCard:624` · `FindPray:639` · `HasPray:648` · `AttachPray:663` · `RemovePray:689`（`BuffTypeSingleUse = 15`，沿用既有表、**未逐值核**）。

| 情形 | 旧（bool） | 新（卡数据） | 落点 |
| --- | --- | --- | --- |
| 祈祷 | `PrayEnchantment = true` | `AttachPray`（挂卡池里那张 `CardDef`，`ctx.EnchantmentsUsed += 1`） | `RuleCore.cs:11326` |
| 自己回合开始 | 清 bool + `Faith+1` | `HasPray` → `RemovePray` → `Faith+1` + 广播 + `EvtKind.GainFaith` | `RuleCore.cs:1572-1573` |
| 受伤（`!absorbed && dmg != 0`） | `= false` | `RemovePray` | `RuleCore.cs:8295` |
| 疲劳（督军） | `= false` | `RemovePray` | `RuleCore.cs:2948` |
| 敌方回合开始 / 0 伤 | 不变 | 不变（只扫当前行动方；同一道闸） | `RuleCore.cs:2171-2174` |

等价性依据：四种情形逐条一一对应，**没有任何一条判据被放宽**（`X13` 原有 4 组断言只把「读 bool」换成「读附魔层」，期望值一个字没改）。
差异（如实）：① 日志多一条（`Attach`/`Remove` 各自出声）；② `ctx.EnchantmentsUsed` 从 0 变 1（原版 `enchatmentsUsed(+0x4e4)` 本来就该 +1 ⇒ 这是**修正**，但它是 `NetProtocol` 状态哈希的一格，见 §六）；③ 卡池里没有这张卡时**不挂并出声**（生产侧恒有；`X13` 夹具改成传 `CardDatabase.Load()`）。

## 四、断言与灭自证（`RuleEngineTest_EngineX13.cs`）

新增/收紧：
1. `Enchantments.FindPray(a) != null` —— **祈祷后附魔层里真有一个实例**；
2. `ReferenceEquals(e1.Card, prayCard)` —— 挂的**就是卡池那一张** `CardDef`（不是自造假卡）；
3. `e1.Effect.BuffType == 15`；`ActiveEnchantments.Count == 1`（幂等，不叠两条）；
4. 结算后 `!HasPray` + `ActiveEnchantments.Count == 0`；受伤后同理；**0 伤后 `HasPray` 仍真**；冲突档同样一次性。
前提断言：卡池里真有 `id = 9218576fd3e26554db5b4c239a36dbc7`、`Type == "enchantment"`；`ProbeBattleX13:174` 传真卡池。

**灭自证**：退回 bool（或 `AttachPray` 改回只置 flag）⇒ 1/2 红；把 `RemovePray` 从结算或受伤里删掉 ⇒ 3/4 红；
把 0 伤闸删掉 ⇒ ③ 红。期望值全是写死字面量，没有一条从实现里反算。

## 五、附魔能力表（A）＋ B/C 结论

### 5.1 分类（118 张模板；逐张明细见 TSV）

| 分类 | 张数 | 结论 |
| --- | --- | --- |
| **A 有池内父卡**（`cardAbilities` → `buffEffect.enchantment`，bt 12/15/16） | **32** | 只有 `Pray Enchantment` 命中执行点 ⇒ **入池**；其余 31 张入池 = 死卡（缺口①） |
| **B `DarkPact`**（`markOfChaos` 链） | 5 | 不入池：引擎用 `DarkPactFx`/`PactGrantedKeywords`（`EffectResolver.cs:11362,11549`）**硬编码**四条契约的增益，附魔卡只喂 `TriggerEnchantment` 事件/动画（`:11499`）⇒ 挂进池会与硬编码表**成对** |
| **C 进攻卡/环境卡**（`Z-` 前缀） | 49 | 不入池：父卡是**环境卡**，引擎走 `OffensiveCardData` / `OffensiveCards.json`（键 = envSO，`OffensiveCardData.cs:1-20`），**不走卡池** |
| **D `Carnifex` 变体** | 3 | 不入池：真值里 **0 条** PPtr 指回来（关联方式**未查清**，见 §八） |
| **E 教程副本**（`T*`） | 26 | 不入池：引用者只有 `_Tutorial_*` 副本，不在池内 |
| **F 真值无引用** | 3 | 不入池：没有任何 `buffEffect.enchantment` 指向它 |
| **H 兄弟卡**（`cardType 0`） | 2 | 不入池：不是模板（`Wurrboy` / `Sergeant Gadriel`） |

### 5.2 A 组 32 张（`池内父卡数 | 自身 trigger | 自身 chosenAbility | 判定`）

| 模板 | 池内父卡 | 自身 trigger | 自身 chosenAbility | 判定 |
| --- | --- | --- | --- | --- | --- |
| Armoured Support | 1 | 10 OtherCardPlayed ✓ | 30 buffOthers ✓ | ✗ 缺口①+② |
| Beastboss on Squigosaur | 1 | 120 Slay ✓ | 40 buffSelf ✓ | ✗ 缺口①+② |
| Cadia Stands | 1 | 31 TurnStartPlayer ✓ | 70 summonUnit ✓ | ✗ 缺口①+② |
| Coterie of the Conceited | 1 | 31 TurnStartPlayer ✓ | 100 createCardInHand ✓ | ✗ 缺口①+② |
| Deffkopta | 1 | 0 ThisCardPlayed ✓ | 10 doDamage ✓ | ✗ 缺口①+② |
| Duty's End | 1 | 80 ThisUnitDeath ✓ | **410 triggerCodex ✗** | ✗ ①+②+缺执行器 |
| Enhanced Aggression | 1 | 580 Strike ✓ | 0 drawCard ✓ | ✗ 缺口①+② |
| Experimental Drones | 1 | 190 OtherUnitSummoned ✓ | 30 buffOthers ✓ | ✗ 缺口①+② |
| Ferocious Rage | 1 | 120 Slay ✓ | 0 drawCard ✓ | ✗ 缺口①+② |
| Graceful Avoidance | 1 | 80 ThisUnitDeath ✓ | 140 recallToOwnerHand ? | ✗ 缺口①+② |
| Hallowed Martyrs | 1 | 130 Rage ✓；410 FollowUpResolution ✓(仅 active 族) | 320 followAfterResolution ?；10 doDamage ✓ | ✗ 缺口①+② |
| Helspear Assault | 1 | 120 Slay ✓ | 70 summonUnit ✓ | ✗ 缺口①+② |
| Hive Fleet Leviathan | 3 | （无 ability） | （无 logic） | ✗ 缺口①+② |
| Hunters of Heretics | 1 | 80 ThisUnitDeath ✓ | 60 buffMana ✓ | ✗ 缺口①+② |
| Idolatrous Despoilers | 1 | 190 OtherUnitSummoned ✓ | 30 buffOthers ✓ | ✗ 缺口①+② |
| Krump da Gitz | 1 | 10 OtherCardPlayed ✓ | 30 buffOthers ✓ | ✗ 缺口①+② |
| Let Loose | 1 | 0 ThisCardPlayed ✓ | **238 scriptedSelfCardAttackDontCount ✗** | ✗ ①+②+缺执行器 |
| Litany of Despair | 1 | 80 ThisUnitDeath ×2 ✓ | 30 buffOthers ✓；340 removeThisEnchantment ✓(仅 Pray 支) | ✗ 缺口①+② |
| Living Icon | 2 | 580 Strike ✓ | 30 buffOthers ✓ | ✗ 缺口①+② |
| Master Outrider | 1 | 120 Slay ✓ | 100 createCardInHand ✓ | ✗ 缺口①+② |
| Oath of Moment | 1 | 580 Strike ✓ | **410 triggerCodex ✗** | ✗ ①+②+缺执行器 |
| Pledge to the Dark Prince | 1 | 120 Slay ×2 ✓ | 20 doHeal ✓；40 buffSelf ✓ | ✗ 缺口①+② |
| Power of the Waaagh! | 1 | 300 Mob ✓ | 40 buffSelf ✓ | ✗ 缺口①+② |
| **Pray Enchantment** | **24** | **60 UnitDamaged / 31 TurnStartPlayer ✓** | **340 remove / 60 buffMana ✓** | **✅ 入池（X20）** |
| Raid Tactics | 1 | **726 TriggeredFerocity ✗** | 10 doDamage ✓ | ✗ ①+②+缺 trigger 分发 |
| Reconnaissance Mission | 1 | 0 ThisCardPlayed ×2 ✓ | 60 buffMana ✓；340 remove ✓(仅 Pray 支) | ✗ 缺口①+② |
| Saviour Protocols | 1 | （无 ability） | （无 logic） | ✗ 缺口①+② |
| Spreading Corruption | 1 | 80 ThisUnitDeath ×3 / 40 TurnEnd ✓ | 30 buffOthers ×2；340 remove ×2 ✓(仅 Pray 支) | ✗ 缺口①+② |
| Telephatic Domination | 1 | 40 TurnEnd ×2 ✓ | **185 stealMinionSelf ✗**；340 remove ✓ | ✗ ①+②+缺执行器 |
| Uge Choppa | 1 | 120 Slay ✓ | 20 doHeal ✓ | ✗ 缺口①+② |
| Undying Legions | 1 | 90 OtherUnitDeath ✓ | 30 buffOthers ✓ | ✗ 缺口①+② |
| Wages of Retribution | 1 | 90 OtherUnitDeath ✓ | 60 buffMana ✓ | ✗ 缺口①+② |

### 5.3 C 缺口表（精确到「缺哪一跳」）

| 缺口 | 内容 | 判据 |
| --- | --- | --- |
| **① 挂载执行链（31 张共有，最根本）** | 父卡的 `cardAbilities[].abilityLogic[].buffEffect{buffType 12/15/16, enchantment}` **没有执行器** ⇒ `Enchantments.Attach` 的调用者今天**只有** `X20` 的 Pray 支。`EffectResolver.cs:6405-6427` 那段是**注释**（解释原版怎么做），不是代码 | `grep`：`Enchantments.Attach` 全仓调用点 = 1 |
| **② 附魔自身 ability 解释器（118 张共有）** | `Enchantments.Matching` 只认**卡面 `desc`** 里的关键词正文；这些隐藏卡 `desc` 为空（原版无卡面文字）⇒ 一条能力都跑不了。要有的是「结构化 `cardAbilities` → op」的桥（`chosenAbility` + `targetCriteria` 36 格 + `furtherMultiplier`），参考 active 能力那条（v23/v24） | `Enchantment.cs:384-394` |
| ③ trigger 无分发 | **726 `TriggeredFerocity`**（`Raid Tactics`）· **500 `TriggerEnchantment`**（5 张 Mark；引擎只在 `abilityContext` 里特判，**不产生事件**）· **640 `EarnedSkull`** · **240 `WhileInPlayVariable`** | 引擎 `FireTrigger*` 无对应调用点 |
| ④ chosenAbility 无执行器 | **410 `triggerCodex`**（作为效果：`Duty's End` / `Oath of Moment`）· **238 `scriptedSelfCardAttackDontCount`**（`Let Loose`）· **185 `stealMinionSelf`**（`Telephatic Domination`）· **13 `doDamageFast` / 15 `doDamageAndDestroySelf`**（本组未命中，全表有） | `AbilityEffect` 枚举 ↔ `EffectResolver` 无此分支 |
| ⑤ 参数解释 | 即使 ① ② 接上，`buffEffect` 的 `trait` / `traitValue` / `armourChange` / `until*` 仍需逐格翻译（`buffType 1/3` 的取值见 `T6 Hyper-adaptation` / `T12 Infinite Biomorphologies` 那 12 张） | `EffectResolver.cs:4736-4744` 现读 |

## 六、读数（全部现跑）

- `TMPDIR=/tmp/wf_ench2 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
- `bash 工具/ruleprobe.sh check` → **rc=0** · `=== 全池 1121 张 · 解析差异 0 行 · 新增 0 · 消失 0 ===` · 哨兵 **失败 0 条** ·
  ⑦ 输出指纹仍 `af836aa0…`。**为什么没变**：`Scan` 跳过 `desc` 为空的卡（`Scan.cs:167-180` 的已知口径，池内 7 张现在含本卡）⇒ 本卡不进 dump。
- `bash 工具/ruleprobe.sh play check` → **rc=0**（主对话已按 X20 第一版重生成 `play` 基线；结算指纹 `0da717c6…` 与基线文件**一致**）。
  X20 第一版时的读数（留痕）：`0 张卡变了 · 新增 1（9218576…）· 消失 0`，唯一差 = 池里多了这张卡的那一行
  （`enchantment`，`playRc=20 ErrNotPlayable`、`played=0`、note `打不出去：rc20` —— 因为 §一 的把关把它挡在打出之外）。
  ⚠️ 第二版（落齐列）**没有**再动 `play` 输出（本卡 `desc` 为空、主档 0 个 op ⇒ 指纹仍是 `0da717c6…`）。
- `python 工具/gen_cards_engine.py --check` → **一致 ✅**（幂等）；`python 工具/gen_diag_runner.py --check` → **逐字节相同（46863 B）**。
- 🔴 **`X13` 的四种情形已离线真跑过**（本批禁跑 Unity ⇒ 用 net8 把 `Core/*.cs` 编起来、`CardDatabase.Parse` 吃**真的** `cards_engine.json`，池 1128 张）：
  **33 ok / 0 FAIL**。逐条覆盖：① 挂的是卡池那张对象（`ReferenceEquals`）· `buffType 15` · 只一条 · 敌方回合不加不摘 · 自己回合 +1 后摘净 · 不重挂；
  ② 真伤撤；③ 盾挡 0 伤**不撤**且下回合照样 +1；④ 冲突档立刻 +1 且下回合再 +1；⑤（附加）空池 ⇒ 不挂、出声（如实记的行为差异）。
  探针源码与复现命令 → **`资料/普查产出_1025/X20_离线探针_Program.cs.txt`**（**不进工程、不参与编译**）。
- 行尾：`gen_cards_engine.py` **6710 行全 CRLF、0 裸 LF**；`EffectResolver.cs` **14499/14499 全 CRLF**；`RuleEngineTest.cs` **37282/37282 全 CRLF**；`Enchantment.cs` / `RuleCore.cs` / `CardDef.cs` / `CreatePool.cs` / `UnitState.cs` / `CardDatabase.cs` 保持原 LF。

## 七、新池 md5 ＋ 逐列 diff

| 项 | 值 |
| --- | --- |
| 卡池 md5（**最终** = 第二版落齐列之后） | **`84c0f84ef2fc4e473df09698cf546cb6`** |
| 卡池 md5（X20 第一版，未落齐列） | `e3041b072a28a07d4e363726449f6d88`（已作废） |
| 卡池 md5（HEAD / 最初钉住的） | `ef431e8054822f359162862fdf51dd5b` |
| `count` | 1127 → **1128** · `version` 29 → **30** |
| **新增卡** | **恰好 1 张**：`9218576fd3e26554db5b4c239a36dbc7` `Pray Enchantment`（`enchantment` / Sororitas / epic / cost 2 / `inventoryOptions 10` / `showInCollectionOptions 0` / `spellType 0`） |
| **既有卡改动** | **0 张**（逐卡 JSON 相等） |
| `talentNameIds` / 其它顶层 | **逐字节未变** |

> ⛔ 未重钉任何指纹。**现在只需重钉一颗**：`PinnedPoolMd5 = 84c0f84ef2fc4e473df09698cf546cb6`；
> `PinnedParseMd5`（`af836aa0…`）与 `PinnedBaselineMd5`（`71cedcd7…`，parse 基线文件未重生成）**都不用动**；`play` 基线（`0da717c6…`）已与现算一致。

## 八、没查清 / 顺手发现

**没查清**：
1. `Carnifex` 的 3 张 `_Enchantment_*`（`Crushing Claws` / `Devourer Cannon` / `Armoured Exoskeleton`）在真值里**0 条 PPtr 指回来** —— 父卡是 `Carnifex` 的主动能力变体，关联方式（`activeAbility` 的哪个字段 / 短 id / 名字）**没查到**；
2. `Hive Fleet Leviathan` / `Saviour Protocols` 两个模板**没有 ability**（纯标记），原版挂上去到底靠什么生效**没查到**；
3. `Graceful Avoidance`（140 `recallToOwnerHand`）/ `Hallowed Martyrs`（320 `followAfterResolution`）两处的参数语义只做「有名无实现」标注，**没有逐格核**；
   `buffEffect` 的参数层（`trait`/`traitValue`/`armourChange`/`until*`）只在 `T6 Hyper-adaptation` / `T12 Infinite Biomorphologies` 那 12 张上看过一眼（`EffectResolver.cs:4736-4744` 的既有注释），**没有逐格核**。

**前提更正（协调者上一条简报里两处）**：
1. **`cardType 20` 不是附魔专用型** —— 全表 **680 张**是 20（绝大多数是普通战术卡）；120 张名字带 `_enchantment` 的只是子集；
   且这 **120 张里 2 张是 `cardType 0`**，`cardType 20` 的模板实为 **118**。
2. **「1204 张卡引用了 `enchantment` 字段或 `buffType 15`」在本快照里复现不出来**（`cards_raw.json` 1,351 张）：
   - `enchantment` 这个**键**在每个 `buffEffect` 对象里都有（**13,012 处 / 1,351 张卡**）⇒ 「键存在」不能当引用；
   - 真正的非零 PPtr（任意容器）**260 处**；`cardAbilities` 里的非零引用 = **114 张父卡**；
   - `buffType == 15` 的父卡 **39 张**（**36 张在池内**）；按 `buffType ∈ {12,15,16}` + `cardAbilities` 口径 = **115 条 / 54 张池内父卡**。
   ⇒ 若你说的 1204 是别的口径（比如能力行数），给我口径我再对一次。**「附魔层已有」这一点成立**（`Enchantment.cs` 存在且 `ConditionEval` 真在读它）。

**顺手发现（该立账，未改）**：
- `资料/任务线/引擎线/任务总表.md` 里 `X13` 那条仍写「**这是引擎内等价实现**」—— `X20` 起已不成立（现在走卡数据），台账该判由主对话改。
- `Enchantment.cs` 顶部「可达性」那段还写「今天卡池 **0 张**卡会挂附魔」—— 现已过期（本卡入池），但那段是 `A1567` 的历史理由，我没有改它的措辞（不在本轮范围；语义已由 §三 覆盖）。
- `Enchantments.Attach` 的 `BuffType` 取值仍是「沿用既有表、未逐值核」；本轮**没有**新增证据。
- `EffectResolver.cs:4736-4744` 那 12 张 `Hyper-adaptation`/`Infinite Biomorphologies` 变体（C 组外的 `T*`）在真值里有 `armourChange`/`meleeAttackChange` 等**参数层**信息，本轮没消费。
- 工作区里 `资料/任务线/引擎线/任务总表.md` 与 `战斗线/任务总表.md` 的改动**不是我做的**（并行写手），我没有碰它们。

## 九、会因「池子 +1 张」而红、但**不在我白名单**的断言（请主对话处置）

这几条我**没有改**（`⛔ CardPresentation/** 一律不碰`）。它们都是「前提检查」性质，**不是缺陷**，
按下面的现值改一次即可（改完应回到绿）：

| 位置 | 现在会在哪红 | 该改成 |
| --- | --- | --- |
| `CardPresentation/Editor/BattleScene.cs:24587` | `Check(n == 1127, …)` | **1128** |
| `CardPresentation/Editor/BattleScene.cs:24588-24590` | `Check(h == 0xE85616DAu, …)`（`desc`/`descZh` FNV-1a 32） | **`0x152CA48F`**（我用**同一算法**在 HEAD 池上复算得 `0xE85616DA` 逐字符相同 ⇒ 新值可信；本卡 `desc`/`descZh` 皆空，贡献 = `id + 3 个 \u0001`） |
| `CardPresentation/Editor/RuleEngineTest_EngineE1.cs:77` | `Check(pool.Count, 1127, …)` | **1128** |

我在**自己白名单内**已经就地改掉的三处同类前提：`RuleEngineTest.cs:21985`（→1128）·
`RuleEngineTest_G12.cs:57`（→1128）· `RuleEngineTest_EngineD4.cs:336`（`cards` →1128）。
其余宿主（`CollectionScene` / `DeckScene` / `SettingsScene` / `ShopScene` / `RewardsScene` / `MainMenuScene` / `ShellScene`）
我按「硬写 11xx 的 `Check`」全扫过：**没有**受影响项（`CollectionScene.cs:5356` 那条是**现算**去重 id 数，会自动跟着变）。

## 十、第二版：**落齐所有列**（`RuleEngineTest` 6 红的修复）

### 10.1 根因与修法
第一版只手写了「这张卡用得到的列」⇒ 它在池里成了**唯一** `null` 的那张（`null` = 整列缺席 / 旧表 / 调用方没给，
与「没有行 = `[]`」是两件事）。**修的是生成器**（⛔ 没改断言）：
`build_enchantment_entries()` 现在**逐列调用主循环用的同一个 helper**（`ability_triggers_of` / `ability_targets_of` /
`ability_multipliers_of` / `active_ability_logic_of` / `active_ability_targets_of` / `ability_target_criteria_of` /
`ability_level_criteria_of` / `ability_acting_criteria_of` / `active_ability_pick_criteria_of` / `ability_gate_of` /
`anim_times_of`）＋ 同一段 `A1799_FIELDS` 循环 ＋ 同一段 `choicesDefinition` 三列。
入口改成 `build_enchantment_entries(..., truth_by_path, anim_idx, anim_cache, anim_trig_names, anim_report)`。

### 10.2 改了哪几列（只在 `Pray Enchantment` 这一张卡上）
| 列 | 现值 | 说明 |
|---|---|---|
| `abilityTriggers` | `[31, 60]` | 第一版已有 |
| `abilityTargets` | 2 条 ability / 3 个 logic（`[{60,[20]},{31,[20,20]}]`） | **真值有行** |
| `abilityMultipliers` | 同形 3 格（全 0） | 真值有行 |
| `abilityTargetCriteria` | **3 行**（`ab/lg` = 0/0 · 1/0 · 1/1，`ta` 20，36 格齐） | 真值有行 |
| `abilityLevelCriteria` | **1 行**（`ab 0` / `trigger 60`，`ca = ""` = 族表认不出 `removeThisEnchantment`） | 真值有行 |
| `animTimes` | **2 行**：`Pray_Exit`(tg 60 `UnitDamaged`) · `Pray_Success`(tg 31 `TurnStartPlayer`) | 真值有行 |
| `activeAbilityLogic` / `activeAbilityTargets` / `abilityActingCriteria` / `activeAbilityPickCriteria` / `choicesSpecificCardIds` | `[]` | 真值没有这些行 |
| `useOverDraw` / `cantBeRandomlyCreated` / `itemDescriptionKey` / `fullHeroSpriteReference` / `eventSpriteName` | 0 / 1 / "" / "" / "" | `A1799` 十列（与主循环同一循环） |
| `choicesTargetsAffected` / `choicesMaxTargets` | 0 / 0 | 与主循环同口径（本卡 `choicesDefinition` 全 0） |

### 10.3 新 md5 ＋ 逐列 diff
- **新卡池 md5 = `84c0f84ef2fc4e473df09698cf546cb6`**（第一版 `e3041b07…` 作废；HEAD/最初钉住 `ef43…`）。
- **vs HEAD**：新增**恰好 1 张**、既有卡改动 **0 张**、`talentNameIds` 与其它顶层逐字节未变。
- **vs 第一版**：**只有这一张卡**多出上表那 9 个结构化列 ＋ `animTimes` ＋ `choices*` ＋ `A1799` 五列；其余 1127 张逐字节相同。
- **重钉**：只需 `PinnedPoolMd5 = 84c0f84ef2fc4e473df09698cf546cb6`（`PinnedParseMd5`/`PinnedBaselineMd5`/`play` 基线**都不用动**）。

### 10.4 跟着更新的前提断言（数量/版本，**不是**放宽判据）
| 位置 | 改动 | 为什么 |
|---|---|---|
| `RuleEngineTest_EngineD4.cs` A1 | `version 29 → **30**`（+ v30 说明） | 卡表确实 bump |
| `RuleEngineTest_EngineP1.cs` W67T3 | `TurnStartPlayer(31)` ability `7 → **8**`；**动画行 `3 → 4`**；doc `7/3 ⇒ 缺 4` → `8/4 ⇒ 缺 4` | 本卡既有 31 这条 ability、`animTimes` 里也真有 `Pray_Success`(31) ⇒ 两边各 +1，差值仍 4 |
| `EngineD4` A4a / `EngineC5` Q4(a) | `atcRows 1616 → **1619**`、`atcCards 1008 → **1009**` | 本卡**真值有** 2 ability / 3 logic 的 `targetCriteria` 行 ⇒ 行数/卡数各涨 |
| `EngineX6` ① | `rows 85 → **86**`、`unknownOrEmpty 5 → **6**`（`known 80` 不变） | 本卡 1 行 ability 级 criteria，`ca` 判不出（340 不在族表） |
| `RuleEngineTest_G12` | `cardsEmpty 995 → **996**`；`notVersioned` 文案 `1127/1127 → 1128/1128` | 本卡 `activeAbilityLogic = []`（非 null、无行） |
| `RuleEngineTest.cs` / `EngineA3` / `RuleEngineTest_Priority.cs` | 文案里的 `1127` 清掉（`842 → 843` / 「全池」） | 纯文本，避免下一轮再被当判据 |
| 第 6 条红（`abilityTriggers` ≡ `abilityTargets`） | **两列都落真值** ⇒ 现读 0 张不一致 | 同进同出恢复 |

### 10.5 读数（第二版现跑）
`typecheck` **0/0** · `gen_cards_engine.py --check` **一致 ✅** · `gen_diag_runner.py --check` **逐字节相同** ·
`ruleprobe.sh check` **rc=0**（解析差异 0 行 · 哨兵失败 0 · ⑦ 指纹 `af836aa0…`）· `ruleprobe.sh play check` **rc=0**（`0da717c6…` 与基线一致）·
离线探针（真 `cards_engine.json`）**33 ok / 0 FAIL**。
行尾：`.py` / `EffectResolver.cs` / `RuleEngineTest.cs` 全 CRLF 未翻；`Enchantment.cs` 等保持 LF。

### 10.6 需你处置的新发现（白名单外，未改）
`CardPresentation/Core/VfxMap.cs` 的 `SelfCheckUnitSkillAnims()`（**今天没有任何宿主调用它**）会扫全池、要求「有 `UnitDamaged`
动画行的卡」必须进 `UnitDamaged` 桶 —— 本卡新增了 `Pray_Exit`(tg 60) 那一行，而这个桶由 `card_vfx_by_card.json` 提供、里面没有本卡。
⇒ 若将来把这个自检接进宿主，两条路任选：① 把本卡补进那张 VFX 表；② 该自检跳过 `Type == "enchantment"`。
（`SelfCheckRoutedTriggers()` **不受影响**：它只遍历 VFX 表里的卡。）

> ✅ 核对：主对话已把 `BattleScene.cs:24587/24588` 改成 **1128 / `0x152CA48F`**、`RuleEngineTest_EngineE1.cs:77` 改成 **1128** ——
> 与我在 §九 给的值**逐字符一致**（我这边按同一 FNV-1a 算法在现池上复算 = `0x152CA48F`）。

