# 查证 · 原版机制 ↔ 我们 · 免伤族与数值型 trait 对照（2026-10-17）

> 触发：用户 2026-10-17 问「**Bastion。这些我们不做的地方，我们的战斗规则及战斗引擎有相关替代的东西吗？**」
> 只读调查代理交回 · 主对话代落盘。全部现读：`d:/2/tools/decomp_full/`（25,096 个 `.c`）· `dump.cs` · `all_strings.txt` · `assets_full` · 我们自己的 `RuleEngine/`。
> ⚠️ 「能不能触发」一律是**本 build 1126 张卡池**口径 —— **卡数据本地拿不到**（13 个 `bundle_*cardassets_assets_all` 只解出 AssetBundle/AudioClip/Sprite/Texture2D，**无 MonoBehaviour**）⇒ 证不到「原版有没有」。

---

## 一、总判断（回答用户那问）

1. **原版没有「数值型 / 布尔型」的硬分类** —— `CardTraitData` 每个 trait 都带 `traitValue`，`GetValueString()` 只按 `value > 0` 决定显不显示数字。真正的分界是**哪些 trait 的 value 被代码当数字读**：`EntityScript.get_Current<X>` 那 14 个访问器 + `regeneration`/`maintenance`/`invulnerable` 走 `GetCurrentTraitValueWithModifiers` 直读。
2. 🔴 **`bastion` 不是孤例** —— 同族还有 **5 个数值型 trait 同样完全空白**：`survivor(430)` · `maintenance(370)` · `psyker(620)` · `reactor(625)` · `markOfChaos(330)`（**卡池全 0、我们全 0**）。
3. **免伤族我们有一半**：`Shield` / `Invulnerable` / `Armour` / `Regeneration` / `Vulnerable` **全在跑**，而且**顺序照原版**（**护盾 → 无敌 → 易伤 → 护甲**）；另一半没有：`bastion` · `dodge` · `dropPod` · `ward`/`wardWb` · `resistant` · `unstoppable` · `survivor`。
4. ✅ **「被别的东西顶替」的只有两条，而且都顶得对**：
   - ① 原版 **`ward`/`wardWb` → 我们的 `Stealth`/`Camouflage`**（**同一份判据位置**：原版 `get_isCurrentlyWarded` 进 `RawCardScript.IsValidTarget`/`IsValidSpellTarget`；我们进 `EffectResolver.AddSide:1137`）
   - ② 原版 **`dodge` → 我们的 `Shield`**（🔴 **原版代码里两者走同一条分支、同样一次性消耗，任何地方都分不出差异**）
   - 半顶替：`markOfChaos` → 我们自造的 **`DarkPact`**（覆盖「四增益」那一层，**层数语义不同**，算近似）
5. 🔴 **`bastion` 没有被顶替** —— 原版是**先吃伤害的吸收池**（扣掉的量**永久减少** bastion，余量才掉血）；我们的护甲是**每击固定减、最低 1** ⇒ **我们永远做不出 0 伤害**（除 Shield/Invulnerable），**语义不同、不能算等价物**。

---

## 二、对照表 · 数值型 trait

| 机制 | 原版语义 | 本地能不能触发 | 我们有没有 | **我们的替代物** | 判据 |
|---|---|---|---|---|---|
| **`bastion` 850** | 挨打先扣的**吸收池**，扣多少永久减多少，余量才掉血；UI 数字徽标 | ❌ **0/1126**；全 25,096 `.c` 里**唯一写方 = `RemoveBastionDamage` 的减法** | 无 | 🔴 **无等价物**（我们**没有吸收池概念**，`grep absorb` 全仓空） | `EntityScript__get_CurrentBastion.c:5` · `CardScript__RemoveBastionDamage.c:29-65` · `CardScript._ReceiveDamage_d__381__MoveNext.c:126-157` |
| `survivor` 430 | 死亡时按剩余次数**免死** | ❌ 0/1126 | 无（只有 `SimpleAI.cs:596` **读**，全仓无人写 = **死读**） | 无（`Remnant` 是死亡后翻面留场，机制不同） | `EntityScript__get_CurrentSurvivor.c:5` · `CardScript__CheckIfDead.c:111-171` |
| `maintenance` 370 | 每回合要付的**维护费**（能量） | ❌ 0/1126 | 无 | 无 | `PlayerManager__GetCurrentMaintenance.c:31` · `PlayerManager__TurnStart.c` |
| `markOfChaos` 330 | 四神印记的**层数** | ❌ 0/1126 | 无 | ⚠️ **部分顶替**：自造 `darkpact`（`CardDef.cs:2283`），覆盖「四增益」那一层 | `EntityScript__get_CurrentMarksOfChaos.c` · `关键词三列对账.md` §三/§五 |
| `psyker` 620 · `reactor` 625 | 资源计数的数值 | ❌ 0/1126 | 无 | 无 | `EntityScript__get_CurrentPsyker.c` / `__get_CurrentReactor.c` |
| `vulnerable` 480 | 每次受伤 **+X**（**加伤**，不是减伤） | ✅ 14 张 | ✅ | — | `RuleCore.cs:1992` |
| `regeneration` 1030 | 每回合结束治疗 X | ✅ 13 张 | ✅ | — | `CardScript__OnTurnEnd.c:84-101` · `RuleCore.cs:809-810` |
| `blast` 350 | 攻击时对相邻造成 X | ✅ 57 张 | ✅ | — | `BattleManager__ResolveTraitAdjacentDamage.c:18-22` |
| `shuriken` 1120 | 攻击伤害前额外 X 点 | ✅ 20 张 | ✅ | — | `CardDef.cs:2261` |
| `sentry` 1280 | 被攻击时先对攻击者 X | ✅ 5 张 | ✅ | — | `CardDef.cs:2289` |
| `markerlight` 1170 | 受**远程**伤害 +X，受远程伤害后清空 | ✅ 14 张 | ✅ | — | `CardDef.cs:2272` |
| `huntMark` 1260 | 可叠加；带标记者被摧毁时按层数造成伤害+治疗 | ✅ 22 张 | ✅ | — | `CardDef.cs:2280` |
| `companion` 1180 | 从手牌打出时带出 ≤X 张伴生 | ✅ 9 张 | ✅（**自动带满**，原版是玩家选 ⇒ **近似**） | — | `CardDef.cs:2203-2206` |
| `tide` 910 | 打出当回合可打 X 张复制 | ✅ 24 张 | ✅ | — | `CardDef.cs:2187-2189` |
| `talent` 845 | 标记天赋卡（回合开始生成临时战术） | ✅ 91 张 | ✅（`RuleCore.SpawnTalents`） | — | `CardScript__OnTurnStart.c:160` |
| `resolution` 840 | **语义未查到** | ❌ 0/1126；代码里只有 `AI__ScoreFromBuff.c:88` 一个 switch 按它分支，**无取值访问器** | 无 | 无 | `AI__ScoreFromBuff.c:88` |
| `mission` 860 | **语义未查到** | ❌ 0/1126；25,096 `.c` 里**没有任何一处按 trait 读它/写它** | 无 | 无 | 负证：`grep 0x35c` 无 trait 上下文命中 |

## 三、对照表 · 布尔型 trait / 免伤族

| 机制 | 原版语义 | 本地能不能触发 | 我们有没有 | **我们的替代物** | 判据 |
|---|---|---|---|---|---|
| `shield` 80 | 挡下**下一次**伤害然后碎（一次性） | ✅ 39 张 | ✅ `UnitState.HasShield` | — | `…_MoveNext.c:330-339` · `RuleCore.cs:1988/2032-2040` |
| `dodge` 240 | 🔴 **代码里与 shield 完全同一条分支**（同挡、同消耗；任何地方查不出差异） | ❌ **0/1126** | 无（仅 `SimpleAI.cs:588` 死读） | ✅ **`Shield` 顶替**（判据：**原版两者不分**） | `…_MoveNext.c:56/189/320-325` · `GetAdjustedDamage.c:41` |
| `invulnerable` 310 | 免疫伤害 + 不可摧毁 | ✅ 16 张 | ✅ | — | `RuleCore.cs:1989/2044` · `EffectResolver.cs:1890-1893` |
| `ward` 140 · `wardWb` 145 | **不能被敌方战术/能力选中**（目标合法性闸门） | ❌ **0/1126** | 无 | ✅ **`Stealth`/`Camouflage` 顶替**（**同一判据位置**） | `EntityScript__get_isCurrentlyWarded.c:8,12` · `RawCardScript__IsValidTarget.c:24-33` · `EffectResolver.cs:1137` |
| `dropPod` 230 | 单位有**独立血池**（`currentDropPodHealth`），伤害先进池；池破只报日志、**单位不死** | ❌ 0/1126 | 无 | 🔴 **无等价物**（我们没有任何第二血条） | `…_MoveNext.c:60,159-183` · `CardScript__CheckIfDead.c:76-96` · `all_strings.txt:69710936`「Destroyed drop pod for」 |
| `resistant` 600 | **取消中毒**（回合末毒伤不结算） | ❌ 0/1126 | 无（`SimpleAI.cs:640` 死读） | 无（🔴 **我们连中毒都没有**：`poison` 全仓 0） | `CardScript__OnTurnEnd.c:110-112` · `all_strings.txt:69940880` |
| `unstoppable` 180 | 攻击目标合法性上的一条豁免 | ❌ 0/1126 | 无 | 无 | `BattleManager__IsValidAttackTarget.c:149` |
| `armour` 1020 | 减 X、**最低 1**、任何来源都减。⚠️ **原版按字段读不按 trait 读**（`currentArmourValue` @`+0xb8`） | ✅ 164 张 | ✅ `UnitState.Armor` | — | `EntityScript__get_currentArmourValue.c` · `RuleCore.cs:1993` |
| `sacrifice` 470 | 与 dodge/shield/invuln/dropPod 并列进「受伤保护」名单 | ❌ 0/1126 | 无 | 无 | `CardScript__IsProtectedFromDamageOrSurvivor.c:27-97` |
| `nonCombatant` 880 | 不能行动/不能攻击 | ❌ 0/1126 | 无 | ✅ `CantAttack`(150) 在跑 | `CardScript__get_mightAct.c:25` |

### 状态 → 参数（铁律 5·c）

| 状态 | 条件 / 参数 | 效果 | 出处 |
|---|---|---|---|
| Shield 在身 | 挨任何一次伤害 > 0 | 伤害→0、**Shield 被移除**（只挡一次） | `…_MoveNext.c:330-339` |
| Invulnerable 在身 | 挨任何伤害 | 伤害→0、**不移除**（无 `RemoveTrait` 分支） | `…_MoveNext.c:58/191` 对照 `:330-339` |
| dodge 在身 | 同上 | 与 Shield **同一分支、同样移除** | `…_MoveNext.c:320-328` |
| dropPod 在身 | 挨伤害 | 先进 `+0xcc` 的池，不动 `+0x68` 的血 | `…_MoveNext.c:159-183` |
| **bastion > 0** | 挨伤害 | `RemoveBastionDamage(dmg)`：先扣 buff 层（`+0x60`）→ 再扣 `traitValue`（`+0x1c`）→ **钳 0**；**返回剩余 bastion（可为负）⇒ `health += 负值` = 溢出掉血** | `RemoveBastionDamage.c:29-65` · `…_MoveNext.c:128-156` |
| **bastion 的平台差异** | 主攻击 / 爆裂 / 践踏 / 多重伤害传 `1`；**反击那一支传 `0`** | 两个调用点：`ResolveAttackDamage(…,1,…)` vs `(…,0,…)` | `BattleManager._ResolveAttack_d__438__MoveNext.c:865`（1）与 `:1136`（0） |
| 护甲 > 0 | 挨伤害（任何来源） | `max(1, dmg − armour)` | `RuleCore.cs:1993` |
| vulnerable > 0 | 挨伤害且**没有** dodge/shield/invuln | `dmg += vulnerable`，**再**走护甲 | `…_MoveNext.c:188-197` |

---

## 四、🔴 反向差：我们**有**而原版没有的

- 这一族里**没有**「原版无、我们有」的替代机制。原版 `DefinedTrait` 里根本没有的是 `penitence` / `darkpact` / `sabotage` / `spirit stone` / `faith` / `ability` / `destroyer`，其中**只有 `DarkPact` 算顶替 `markOfChaos`**。
- 我们**多的是结构**：`RuleCore.DamageAfterReduction` 是**全仓唯一一份伤害公式**（预测与实际不可能分叉）· `GivePayload` 授予关键词时同步状态字段 · `UnimplementedKeywords()` 诊断单。
- 🔴 **我们少的是结构（这条要记牢）**：原版 `DamageInfo` 带 **`DamageType`（13 种：Normal / Ranged / Undersupplied / Ability / Attack / Random / Blast / Vulnerable / VulnerableCombat / Poison / RandomPoison / Stomp / RandomAbility）** + `damageAbsorbed` / `dealtByShuriken` / `dealtBySentry` / `dealtByMarkerlight`；**我们的 `Hurt(ctx,u,amount,source)` 只有一个 int** ⇒ **原版「按伤种分支」的那些（resistant 抗毒、absorbed 不触发「造成伤害」）我们无处可挂。**

---

## 五、判不了（逐条）

1. **`ResolveAttackDamage` 那个 bool 位的名字没查到**（反击那支传 0 是硬事实，**位义未定**）⇒ **不猜**。
2. **`CardTrait` SO 本体本地 0 命中** ⇒ 「原版 trait 表里 bastion/dodge/ward 到底有没有项」**证不出**；能证的只有「**本 build 无写方 ∧ 本地所有可查符号全空**」。
3. **卡数据本地拿不到** ⇒ §二/§三 的「能不能触发」一律是**本 build 1126 张卡池**口径。
4. **`dodge` 为什么和 `shield` 并存**没查到（本地化与字符串表都没有 dodge 词条，代码里两者无差异）。
5. **`resolution`(840) / `mission`(860) 的语义本身没查到** —— 属「**判据是空的**」，不是「我们不做」。

## 六、搜索覆盖（铁律 2）

- **卡池**：`cards_engine.json`（count=**1126**），逐词扫 `keywords` + `desc`。**判 0 的 30 个词**：bastion · drop pod · droppod · dodge · ward · resistant · resolution · survivor · maintenance · unstoppable · syphon · tainted · coward · unstunnable · berserk · frenzied · giant killer · poisonous · spikes · horns · daemonhost · summon rift · mission · non-combatant · trap · fear · meltdown · loot。**对照组非 0**：talent 56/36 · quest 26 · spirit stone 37 · invulnerable 16 · shield 14/23 · armour 82/44 · regeneration 9/4 · faith 7。
- **反编译**：`EntityScript__get_Current*`（23 个全列）· `EntityScript__{GetCurrentTraitValue,GetCurrentTraitValueWithModifiers,HasCurrentTrait,get_isCurrentlyWarded,get_currentArmourValue,get_currentDropPodHealth,get_currentDropPodAttack}` · `CardScript__{GetAdjustedDamage,ReceiveDamage,…_ReceiveDamage_d__381__MoveNext,CheckIfDead,RemoveBastionDamage,RemoveDropPod,IsProtectedFromDamageOrSurvivor,HasBastionHigherThanAttack,EnoughPendingDamageToDieWithDamageValues,ResolveDamageDealt,OnTurnEnd,OnTurnStart,AddArmour}` · `BattleManager__{ResolveAttackDamage,…_ResolveAttack_d__438__MoveNext,ResolveTraitAdjacentDamage,ResolveTraitStompDamage,…_ResolveMultiDamage_d__456,…_ResolveMultiDamageAndDestroySelf_d__457,BroadcastUnitDamaged,IsValidAttackTarget,CanAttackCard}` · `RawCardScript__IsValidTarget` · `FilterMethods__CheckIfMeetsCriteria` · `CardTraitCollection__GetDefaultTraitsToLoad` · `CardTraitData__GetValueString` · `AI__{ScoreFromBuff,ScoreFromAttack,ScoreFromBuffedAttack,ScoreFromDamagingBuffedUnit}` · `BattleCardUI__UpdateBastionr`
- **枚举/字段**：`dump.cs` 的 `DefinedTrait`（**148 个成员**）· `CardTraitData{trait,traitValue,hide}` · `CardTrait{_stackable,_hasIcon,_aIScore,_traitEffects}` · `DamageType`(13) · `DamageInfo`
- **字符串**：`all_strings.txt` —— bastion **0** · dodge 只有 `UILinearDodge`/`DodgerBlue`（非机制）· ward 只有 `Reward*` 误命中 · resistant 1 条 · survivor 2 条 · drop pod 1 条
- **数据**：`数据/游戏数据/trait_textsprites.json`（**87 个 `Atlas_trait_icon_*`，无 bastion/dodge/ward/wardWb/resistant/dropPod/survivor/maintenance/unstoppable**）· `数据/本地化/i18n/zh_CN.csv`（bastion/dodge/ward **0 词条**）
- **我们的代码**：`CardDef.cs`（`Implemented` 33 个、`Prefixes` 56 个，**都没有** bastion/dodge/ward/wardWb/resistant/survivor/maintenance/unstoppable）· `UnitState.cs` · `RuleCore.cs:1985-2069` · `EffectResolver.cs:1137,1890-1893` · `SimpleAI.cs:579-645` · `GivePayload.cs`
  ⇒ **全仓 `bastion`/`dropPod`/`ward`/`maintenance`/`unstoppable`/`syphon`/`tainted` 字符串 0 命中；`dodge`/`resistant`/`survivor` 只有 `SimpleAI.cs` 读、无人写**
- **我们自己的对账表**：`关键词三列对账.md` · `关键词图标_现状与总表.md` · `关键词频次表.md` · `规则书_实现指南对账.md`（**本族 0 命中**）· `全量反编译复核_靠推断的清单.md`（**本族 0 命中**）· `待办判据_战场与战斗视图.md:1118-1129`（bastion 前案，**结论与本文一致**）
