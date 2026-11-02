# B121-CMT 子报告 · 引擎块（`Assets/RuleEngine/Core/*.cs` + `Data/*.cs`）

本块扫描了 **118 个 .cs / 119,828 行**（不含 `Editor/`）。全块注释行 **64,554**（密度 **53.9%**）。
**结论一句话：关键词那一层（`KeywordTable` 64 个常量）注释是「教科书级」—— 每个常量都有 `///`、原版 trait id、规则书出处、阵营归属、落点；缺的是反向那一半：实现方法这一头很少写回「我这段是哪个关键词 / 哪个阵营的什么功能」，只能靠人去 `CardDef.cs` 反查。**

## 一、现状量化

| 指标 | 现读 |
|---|---|
| 文件数 / 总行数 / 注释行 | 118 / 119,828 / 64,554（53.9%） |
| 首行「`文件名.cs — 一句话功能」/ 文件头带「判据·第一权威·出处·反编译」 | **117/118**（例外 `EffectResolverPartial_B114W30.cs` 首行是 banner）/ **104/118** |
| `KeywordTable` 常量 | **64 个，64/64 有 `///`**；其中 doc 点了实现位置 **44/64**，写了**阵营名** **18/64** |
| `🔶 自己设计（非原版）` 严格格式标记 | 52 处（另有 56 处变体写法，合计 108） |

| 文件 | 行数 | 注释行 | 密度 | 文件 | 行数 | 注释行 | 密度 |
|---|---|---|---|---|---|---|---|
| EffectResolver.cs | 21,705 | 11,083 | 51% | RuleCore.cs | 18,486 | 11,570 | 62% |
| EffectText.cs | 11,226 | 5,896 | 52% | CardDef.cs | 9,616 | 5,919 | 61% |
| CardDatabase.cs | 5,088 | 2,141 | 42% | UnitState.cs | 4,430 | 2,931 | 66% |
| BattleContext.cs | 2,727 | 1,809 | 66% | ActionPump.cs | 2,657 | 1,574 | 59% |
| SimpleAI.cs | 2,260 | 1,049 | 46% | Enchantment.cs | 2,118 | 1,036 | 48% |
| WhenEvent.cs | 2,105 | 1,259 | 59% | CreatePool.cs | 1,757 | 781 | 44% |
| CountEval.cs | 1,446 | 678 | 46% | Aura.cs | 1,078 | 533 | 49% |

**方法级空档（方法前一行无任何注释的成员，实测）**：EffectResolver **35** · RuleCore **11** · EffectText 8 · CardDatabase 7 · BattleContext 7 · CardDef 6 · UnitState 4 · SimpleAI 3。口径：**不等于「无注释内容」**（多数方法体内仍有逐段 `//`），缺的是**方法头那一句功能标题**。

## 二、关键词 → 实现位置（任务点名的 18 个）

| 关键词 | 定义（`CardDef.cs`） | 实现位置（类.方法:行） | 实现处有无「这段是什么」注释 |
|---|---|---|---|
| Vanguard | `:6915` | `RuleCore.IsValidTarget:5572/:5581` · `HasVanguardTrait:11605` | ⚠️ 只在 `:5570-5572` 作为 Flying 段旁注，**没有独立一句「Vanguard = 前线单位」** |
| Flying | `:6979` | `RuleCore:5710` | ✅ `:5695-5713` 七行，四析取项逐字对原版 |
| Stealth | `:6953` | `RuleCore:5530`（攻击）· `:2163-2165`（回合开始到期）· `:6611`（攻击揭示）· `:11563`（能力目标） | ✅ 定义处三条机制齐；四个落点各一句 |
| LongRange | `:7308` | `CounterAttack.NoCounter:60` | ✅ 实现处（文件头 `:17` + `:59`）；❌ **定义处无落点** |
| Armour | `:6995` | `RuleCore.DamageAfterArmourOne:7786` · `DamageAfterReduction:7607` | ✅ 大段（含「预览侧不加这道闸」的对称说明） |
| Shield | `:7005` | `RuleCore.ApplyDamage:8670`（`RemoveAll(Shield)`） | ✅ |
| Ambush | `:7703` | `RuleCore.PlaysFaceDown:3919` · `DeclareAttackCore:6497-6520` · `ApplyDamage:8992/9022` | ✅ 非常详细（含「stealth 优先」「翻开即摘词」） |
| Remnant | `:7862` | `RuleCore:10019`（翻面支）· `UnitState.IsRemnant:410` · `ActionPump:1223` | ✅ |
| Jam | `:7773` | `EffectResolver.DoJam:16300` · `ResolveJamUnit:16370` · `UnitState.ApplyJam:2228` | ✅（`ApplyJam` ①～⑤ 有编号注释） |
| Swarm | `:7817` | `RuleCore.IsSwarmHost:4663` · `TrySwarmMerge:4685` · `SwarmMergeNow:4712` | ✅ |
| BloodThirst | `:7657` | `RuleCore.ActionQuota:3590` · `EmitBloodThirst:12647` | ✅（四道闸逐条对原版） |
| Duty | `:7678` | `RuleCore.AlternativeActions:12095` · 结算支 `:12145` | ✅（四词对照表在 `:12073-12096`） |
| Pray | `:7660` | `RuleCore:12275`（`Prayed=true` + 挂附魔）· `Enchantment.AttachPray:755` | ✅ |
| Ferocity | `:7644` | `RuleCore:3552`（召唤病豁免）· `:12306`（洗回牌库） | ✅ |
| Agenda | `:7641` | `RuleCore.AlternativeActions:12096`（同一支） | ✅ |
| GiantKiller | `:6940` | `RuleCore.IsValidTarget:5730` | ✅ 定义处最详细（含「池内 0 张」可达性） |
| SummonRift | `:6971` | `RuleCore:5777`（攻击侧裸闸）· `:11572`（能力侧裸闸） | ✅ |
| Waystone | `:7616` | `RuleCore.CanCollectWaystone:12536` · `CollectWaystone:12558` | ✅ 全块最好的一处（原版 5 道闸逐条） |

**补充索引**：`ActionPump.TriggerNameOfKeyword:1081`（case 表 `:1110-1127`）是一张「关键词 → 原版 trigger id」映射表（`Pray→655` `Duty→662` `Ambush→670` `Ferocity→725` …），块内最像「功能索引」的一处；`判据/B115_S2_trigger0派发现状.md:69` 已指向它。

## 三、阵营机制

| 阵营 | 机制 | 注释位置 | 实现位置 |
|---|---|---|---|
| 修女会 Sororitas | 信仰 Faith（`N ☀:` 阈值前缀，**不扣费**） | `CardDef.cs:3783` · `EffectText.cs:8496` · `:8606` | `RuleCore.CanUseFaithAbility:12028` · `UseFaithAbilityCore:12060` · `EffectResolver:791-922` |
| 修女会 | 祈祷 Pray（替代行动 + `Pray Enchantment`） | `CardDef.cs:7660` · `Enchantment.cs:682-706`（⑥ 整段） | `RuleCore:12275` · `Enchantment.AttachPray:755` |
| 灵族 SaimHann | 灵魂石 SpiritStone / 路标石 Waystone | `CardDef.cs:7695`（写明「灵族 SaimHann」）· `:7616` | `RuleCore.CanCollectWaystone:12536` · `CollectWaystone:12558` · `UnitState.IsAeldariRemnant:2499` |
| 基因窃取者 / 太空野狼 / 暗黑天使 / 星界军 / 极限战士 / 帝皇之子 / 泰伦 | 伏击·起义 / 狂暴 / 议程·传送 / 职责 / 典籍 / 激励 / 虫群合并 | `CardDef.cs:7703/7697` · `7644` · `7641` · `7699` · `7678` · `7685` · `7701` · `7817` | 同 §二 |

**缺口**：`修女会` 全块只有 **5 处**注释提到（`修女` 共 6 行；`Sororitas` 93 处多是卡 id/卡名），`Acts of Faith` **0 处**。
**「这是哪个阵营的什么机制」这句话只活在 `CardDef.cs` 的关键词常量上** —— `RuleCore.UseFaithAbilityCore` / `:12275` 的 Pray 支 / `Enchantment.AttachPray` 里一个阵营名都没有，读代码的人看不出它属于修女会。

## 四、反例（最该补「一句话功能标题」的 10 处）

| # | 文件:行 | 这段其实是什么功能 |
|---|---|---|
| 1 | `Core/EffectResolver.cs:3091` `DoDeal` | 卡面 `Deal N damage` 的**伤害动词**总入口（无 `///`，上方只有 banner「各个动词的结算」） |
| 2 | `Core/EffectResolver.cs:3722` `DoHeal` | 卡面 `Heal N` —— **治疗**（含「治己方督军」默认支） |
| 3 | `Core/EffectResolver.cs:3748` `DoDraw` | 卡面 `Draw N` —— **抽牌**（含「抽到的这几份」代词槽回填） |
| 4 | `Core/EffectResolver.cs:4332` `DoDestroy` | 卡面 `Destroy …` —— **直接置死**（「不能走 `Hurt`，否则 5 血 2 甲只掉 3」这条差别只藏在行内） |
| 5 | `Core/EffectResolver.cs:4373` `DoStun` | 卡面 `Stun a unit` —— **眩晕**（含「未写目标 ⇒ 打槽号最小那个」） |
| 6 | `Core/RuleCore.cs:9187` `Hurt` | **全引擎的伤害入口**（含死亡/广播/触发三拍），无 `///`、无首句 |
| 7 | `Core/RuleCore.cs:13635` `UnstableBlast` | 关键词 `Unstable`（死时对相邻自爆）的**结算体** |
| 8 | `Core/RuleCore.cs:13721` `SpawnTalents` | 开局/回合**造天赋卡**的入口（与 `CreatePool` 天赋池联动） |
| 9 | `Core/UnitState.cs:1104` · `Core/CardDef.cs:6146` `KwValue` | **关键词数值的唯一读口**（`Armour 2` 的 2 从哪来），两处都零注释 |
| 10 | `Data/CardDatabase.cs:1126` `Parse` · `Data/SimpleAI.cs:1665` `ExecuteActionCore` | 卡表 JSON 装载入口 / AI 动作执行核心，均无方法头 |

## 五、正例（好注释长什么样，原文摘录）

**① 关键词常量 = 语义 + 判据 + 落点 + 可达性**（`Core/CardDef.cs:6955-6971`，SummonRift）：
> **召唤裂隙**（原版 `DefinedTrait.summonRift = 730` …）／ · **主动能力**：`BattleManager__IsValidActiveAbilityTarget.c:55-56` 是**裸闸**…
> ⇒ 我们的落点 = `RuleCore.CanUseAbilityTarget` 的 ②（⛔ 别「对偶地」写成「只在异侧挡」）
> ⚠️ **全池今天 0 张卡带它** ⇒ **零行为变化**；补的是「原版有、我们缺」这一层（铁律 11）。

**② 文件头当「功能地图」**（`Core/CounterAttack.cs:1-19`）：
> `CounterAttack.cs — 「这一记攻击会不会吃反击」的**唯一判据**`
> · **terror(520)**：攻方带 trait ∧ 守方 `cardType == 0`（**Minion；不含 Hero/督军**）……
> ⚠️ **这个文件是「同一条规则的唯一写法」**：`RuleCore` 的两处……都转调这里

**③ 阵营机制段**（`Core/Enchantment.cs:682-690`）：
> ⑥ 祈祷附魔（`X20` · 2026-10-25）—— 原版隐藏卡 `Pray Enchantment`；23 张带 `pray` 特性的卡在
> `cardAbilities[trigger = 655 (AbilityTrigger.Pray)]`

**④ 「它和别的东西不是一档」**（`Core/RuleCore.cs:11995-12003`，信仰能力）：
> 🔴 **它和誓约【不是】同一档东西，别照 `CanUseOathAbility` 抄全**：
> · 誓约 = **玩家/AI 主动激活的一次行动**……　· 信仰 = **入场/打出那一刻自动结算**……

**⑤ 游戏功能全链**（`Core/RuleCore.cs:12520`，`CanCollectWaystone` 开头）：
`/// **这一格是不是一颗「可以收集的路标石」**（灵族的灵魂石）。**只判不执行** —— 表现层拿它决定「点这一下走收集、还是走攻击选择器」。` —— 这正是用户要的形状。

## 六、补注释的**零行数变化**做法（账按行号引用，⛔ 不能加行）

| 场景 | 做法 |
|---|---|
| 方法头已有一条 `//` | 就地改写，句首加「**功能：<一句话>**（<关键词/阵营>）」—— 行数不变 |
| 方法头是 `///` 多行块 | 改写 `/// <summary>` **同一行**，不改行数 |
| 方法前一行空行、无任何注释 | ⛔ 不插行；改写**方法体第一行**的行尾注释，或并进已有的首个 `//` |
| 完全裸（如 `KwValue`） | **追加到类尾**（类尾之后无账落点）建一张「功能名 → 落点」索引表；或写进**文件头**（不在账落点区） |
| 阵营归属 | 优先写进 `CardDef.cs` 对应关键词常量的 `///`（功能名的主场，已有 18 个先例）；实现处再 `见 KeywordTable.Pray` 一句 |

## §顺手发现

1. **`TODO(谁定的)` 7 处未填**：`AchievementTally.cs:134` · `CardDef.cs:910/7365/7468/7597` · `ConditionEval.cs:302` · `StarterCards.cs:26` —— `🔶 自己设计` 要求写「谁定的 + 日期」，这 7 条仍是占位符。
2. **`CardDef.cs:910`**（`spellType = -1`）同时缺「为什么必须自己定」，只写了「那 26 张不在原版快照里」。
3. **`RuleCore.cs:16` 的「v1 只实现 5 个关键词」仍在文件头**（紧接三行才有「已过期」订正）—— 新读者扫前 20 行会先读到错误口径；建议把订正句前置到同一行（零行数变化）。
4. **`CardDef.cs:8336` 的别名表**有 `new[] { "remnant", "remnant" }`（规范名映射到自己）—— 与相邻的 `"long range"→LongRange` 不同形，**未判缺陷**，仅记录。
5. **`ActionPump.TriggerNameOfKeyword:1081` 的关键词↔trigger 映射表**已有 `判据/B115_S2_trigger0派发现状.md:69` 指向，但**功能点索引（`判据/B8_功能点索引_复核.tsv` 一族）里没有反向指针** —— 建议补一行，否则「哪个关键词走哪条 trigger」还得现 grep。
6. **`修女会` 5 处 / `Acts of Faith` 0 处**：若要做「阵营 × 机制」功能注释，这是最该补的一块（灵族 30 处、SaimHann 115 处可参照）。
