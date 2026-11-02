# B117 逐卡核验 B（Ultramarines · EmperorsChildren）

> 只读代理 `B117-B` · 2026-11-02 · 目标：`全卡核验_判定.tsv` 里本组 **73 张「引擎判定=未验到」**逐张回源，判**引擎有没有对应实现分支**。
> 只读代码 + 读**已缓存**的探针读数（`_tmp_view/w41s3b/after/all_pf.txt` / `pool_diff4.tsv`，2026-10-27 调度台跑的），⛔ 未跑 Unity / ruleprobe / 未改任何 `.cs`。

## 一、结论速览

**本组 73 张：✅ 72 · 🟠 0 · ❓ 1（UM64）**。「未验到」的成因**全部落在探针夹具/闸**，**没有一张是「引擎缺分支」**。

按闸聚类（pool_diff4 备注列，行数/卡数）：`电池只覆盖 unit`(17/15，战术卡) · `720 Cruelty 电池不触发`(11/9) · `715 Ecstasy 电池不触发`(10/9) · `电池未抬起该事件`(7/7) · `240 WhileInPlayVariable`(4/3) · `通道=电池`(5/4) · `通道=督军电池`(3/3) · `S3 无行（关键词卡）`(9 卡) · `出牌失败`(1/1，UM64) · 其余为 `criteria.*Filter` / `ta=65/150/180` / `selectFromChooseResolution` / `负向加成` / `ephemeral` 等**夹具判不了**类。

**证据代号**（下表「证据」列）：
- `T<n>` = 该 trigger 的**接收/发射点**：T0 打出即结算 `EffectResolver.cs:2724`（战术）/`RuleCore.cs:4205`（单位）· T10 `EffectResolver.cs:7642`+`RuleCore.cs:4205` · T20 `Enchantment.cs:1421-1481` · T46 `EffectResolver.cs:19199` · T50 `RuleCore.cs:7038` · T90 `RuleCore.cs:10218` · T120 `RuleCore.cs:10575` · T145 `RuleCore.cs:13465` · T180 `EffectResolver.cs:8180` · T240 `EffectResolver.cs:19739/20244` · T410 `RuleCore.cs:13036` · T650 `EffectResolver.cs:7738` · T710 `EffectResolver.cs:2765` · T715 `RuleCore.cs:9184` · T720 `RuleCore.cs:9323`。全部 97 条 trigger 号 ↔ 名在 `CardDef.cs:1914-1945`。
- `V:x@行` = 动词表 `EffectResolver.EffectDispatch` 的落点（`deal`1064 `eachunitdeal`1066 `forceattack`1068 `heal`1073 `draw`1074 `drawtype`1075 `return`1086 `destroy`1087 `setcurrenthealth`1107 `double`1109 `give`1122 `gain`1123 `lose`1130 `chooseone`1132 `choosecard`1133 `persist`1137 `create`1140 `deploy`1141 `lowercost`1142）。
- `BT<n>@行` = `ApplyAbilityBuff` 的 buffType 分支（`EffectResolverPartial_B97W2.cs`）：1→233 · 3→292 · 8→486(default) · 12→493(default) · 2→268 · 4/100→**无分支**（落 default `:494`）。
- 🔴 **架构口径（判读前提）**：`chosenAbility`(ta) 在**触发路**上只有 `21368-21374` 的 `TriggerRowKindOf` —— 除 `320/325/340/380/480` 外一律 `BodyOp`（= 正文 op 负责）；`FireTriggerRowsByChosenAbility` 只对 320 有执行器，其余无正文 op 时走 `NoteTriggerRowKindGap` 出声。buffType 表（`ApplyAbilityBuff`）**今天没有生产调用点**（`EffectResolverPartial_B97W2.cs:158-162` 自述，唯一入口 `ResolveAbilityBuffRows` 全仓 0 caller）。⇒ 本表判「✅」= **trigger 有接收点 ∧ 正文动词有落点**，与探针读数互相印证。

## 二、逐卡

| 卡id | 卡名 | 判定 | trigger | ta | buffType | 证据（文件:行号） | 备注 |
|---|---|---|---|---|---|---|---|
| EC35 | Blissbringer | ✅ | 720 | 30 | 1 | T720；V:give@1122；BT1@233 | 电池实跑 EC35 5→6→7（Cruelty 两次 +1） |
| EC34 | Fiend | ✅ | 120 | 100 createCardInHand | - | T120；V:create@1140 | 电池无 Slay 击杀 |
| EC24 | Slaanesh's Spawn | ✅ | 720 | 40 | 1,3 | T720；V:gain@1123 + V:give@1122；BT1/BT3 | Cruelty 需「敌方受伤未死」局面 |
| EC15 | Winged Daemon Prince | ✅ | 240,720 | 0,30 | 1,2 | T240；T720；V:draw@1074；降费=光环 `Aura.cs:273`/`EffectText.cs:720` | 「cost 2 less」不走 BT2，走光环层 |
| EC2 | Blastmaster Noise Marine | ✅ | 50 | 110 | - | T50；V:destroy@1087 + `AttackedBySelf` `EffectResolver.cs:1718` | 夹具无「带甲敌军」 |
| EC12 | Flawless Blade Champion | ✅ | 120,715 | 30,238 | 1 | T120；T715；238=`ForcedAttackRefund.cs:8` + `EffectResolver.cs:3298-3317` | 实跑：敌督军 30→23（Slay 2 伤×3） |
| EC11 | Flawless Blade | ✅ | 120 | 10 | - | T120；V:deal@1064 | 电池无击杀 |
| EC9 | Icon of Excess | ✅ | 715 | 30 | 8 | T715；BT8@486；`GivePayload.cs:134`（dark pact） | Ecstasy 阈值 2 未越 |
| EC8 | Infractor | ✅ | 720 | 40 | 1 | T720；V:gain@1123 | 同 EC24 |
| EC21 | Malgarash the Adamant | ✅ | 710,715 | 20,40 | 1 | T710；V:heal@1073 | 实跑 3/5→4/6 |
| EC27 | Rugged Disharmonist | ✅ | 240 | 30 | 1 | T240；减益=光环 `Aura.cs:273` | 常驻减益，单拍不可观测 |
| EC23 | Slaanesh Sorcerer | ✅ | 720 | 30 | 1 | T720；V:give@1122 | 实跑：友军 2/0→3/1 |
| EC26 | Terminator Champion | ✅ | 720 | 10,40 | 1 | T720；V:deal@1064 + V:gain@1123 | 夹具未造 Cruelty |
| EC25 | Terminator | ✅ | 715 | 40 | 1 | T715；V:gain@1123 | 实跑 5→7（Ecstasy 2 ×+2） |
| EC19 | Threnodic Noise Marine | ✅ | 715 | 20,40 | 3 | T715；V:gain@1123（bloodthirst `GivePayload.cs:133`）+V:heal | 阈值未越 |
| EC7 | Tormentor Obsessionist | ✅ | 715 | 30 | 1 | T715；V:give@1122 | 同上 |
| EC5 | Tormentor | ✅ | 715 | 10 | - | T715；V:deal@1064 | 同上 |
| EC20 | Turyan Ghauze | ✅ | 720 | 30 | 8 | T720；BT8@486 | 同上 |
| EC4 | Veteran Noise Marine | ✅ | - | - | - | 关键词 Blast `RuleCore.cs:7213-7239` | S3 表不覆盖关键词分支 |
| EC42 | Beautiful Death | ✅ | 0,410 | 10,20,320 | - | T0/T410；V:deal+V:heal（`valueOptions 210` 超额） | 实跑：10 伤打死 FIX_E1 |
| EC51 | Coterie of the Conceited | ✅ | 0 | 30 | 12 | V:persist@1137 ←`EffectText.cs:5627` ReWarlordGains；消费 `EffectResolver.cs:9003` | 常驻「回合开始造药剂」 |
| EC54 | Dark Apparitions | ✅ | 0 | 0,30 | 3 | V:give@1122 + V:draw@1074（`EffectText.cs:11042`）；boardCriteria `CountEval.cs:240` | 夹具无 Daemon ⇒ 走 draw 支 |
| 6ed51d…7956c | Duelist's Hubris | ✅ | 0 | 20,360 | 3 | V:chooseone@1132（`EffectText.cs:5957`）；360=`EffectResolver.cs:18781` | 实跑给 FIX_E2 上 vulnerable1 |
| EC40 | Embrace the Pain | ✅ | 0,410 | 10,20,320 | - | V:deal+V:heal；T410 | 实跑 2 伤 |
| EC39 | Eternal Servitude | ✅ | 0 | 75 summonBuffedUnit | 100 | V:deploy@1141 + `EffectText.cs:6577`（died since your last turn）+ V:setcurrenthealth@1107 | BT100 无分支但表无调用点（§三①） |
| b9665e…7f9d | Euphoric Strike | ✅ | 0 | 10,100 | - | V:deal@1064 + V:create@1140 | 实跑 1 伤 |
| EC47 | Exquisite Swordsmanship | ✅ | 0,410 | 10,30,320 | 1 | V:give@1122 + `EffectText.cs:8644`（equal to your Warlord's Melee） | 实跑督军 2→3；410 那半未触发 |
| EC50 | Rapid Evisceration | ✅ | 0 | 10,70 | - | V:deal@1064 + V:deploy@1141 | 实跑 2 伤；死亡支未验 |
| EC49 | Terrifying Crescendo | ✅ | 0,410 | 30,110,320 | 1 | V:lose@1130 + V:destroy@1087 | 实跑：-3 远程后摧毁 |
| EC58 | Vengeful Surge | ✅ | 0 | 30,70,75 | 3 | V:deploy@1141 ×2 | 实跑 EC8+EC5 落场；Flank 条件未验 |
| EC28 | Chaos Land Raider | ✅ | 720 | 70 | - | T720；V:deploy@1141（随机池 `CreatePool.cs`） | 夹具未造 Cruelty |
| EC31 | Chaos Rhino | ✅ | 715 | 70 | - | T715；V:deploy@1141 | 同上 |
| EC30 | Maulerfiend | ✅ | 715 | 40 | 1 | T715；V:double@1109 | 实跑 10/5→20/10 |
| UM58 | Desolation Marine | ✅ | - | - | - | Blast `RuleCore.cs:7213` | 关键词卡，S3 无行 |
| UM19 | Aggressor Sergeant | ✅ | - | - | - | Blast 同上 | 同上 |
| UM27 | Assault Centurion | ✅ | - | - | - | Vanguard `ConditionEval.cs:243` / Armour | 同上 |
| UM4 | Assault Intercessor | ✅ | - | - | - | Vanguard `ConditionEval.cs:243` | 同上 |
| UM23 | Bladeguard Lieutenant | ✅ | 145 | 20 | - | T145；V:heal@1073 | Codex 未触发 |
| UM29 | Captain Sicarius | ✅ | 46,145 | 20,40,100 | 3 | T46；T145；V:heal+V:gain | 实跑 +攻、vanguard 授予后摘除 |
| UM81 | Eliminator Sergeant | ✅ | 10 | 30 | 3 | T10；V:give@1122 | 部署他牌时触发 |
| UM8 | Eliminator | ✅ | - | - | - | 关键词 Camouflage/Sniper `ConditionEval.cs:53`/`CounterAttack.cs:64` | 同上 |
| UM106 | Ferren Areios | ✅ | 240 | 10,30 | 3 | T240；Oath 次数 `CardDef.cs:4315-4343` | 实跑 FIX_E1 6→5、5→1 |
| UM10 | Hellblaster | ✅ | - | - | - | Blast `RuleCore.cs:7213` | 关键词卡 |
| UM99 | Phobos Librarian | ✅ | 180 | 10,40 | 3 | T180；V:deal@1064 | 实跑 6→4、4→1 |
| UM79 | Primaris Eradicator | ✅ | 145 | 10 | - | T145；V:deal@1064 | Codex 未触发 |
| UM6 | Primaris Intercessor | ✅ | - | - | - | 无关键词无 ability（纯白板 2/2/3） | S3 无行且无效果可跑 |
| UM5 | Primaris Reiver | ✅ | - | - | - | Flank `RuleCore.cs:3567` | 关键词卡 |
| UM20 | Sergeant Telion | ✅ | 145 | 30 | 2 | T145；V:lowercost@1142；BT2 手牌侧 `RuleCorePartial_B115W20.cs:306` | 手牌降费 |
| UM7 | Tyrannic War Veteran | ✅ | 0 | 30 | 4 | V:lose@1130（`GivePayload.cs:133` 认 stealth/camouflage） | BT4 无分支但表无调用点（§三①） |
| UM93 | Vico Therbeus | ✅ | 20 | 30,40 | 3 | T20；Oath 跨回合 `CardDef.cs:4316` | 实跑 gain camouflage1 |
| UM64 | Angels of Death | ❓ | 0,145,410 | 30,75,320 | 1,3 | 出牌被挡 `EffectResolver.cs:2086-2088`；`deploy` 分支本身在@1141 | 见 §四 |
| UM75 | Armoured Offensive | ✅ | 0 | 30,100 | 2 | V:create@1140 ×3 实跑；V:lowercost@1142 | 3 张载具进手 |
| UM44 | Armoured Support | ✅ | 0 | 30 | 12 | V:persist@1137 ←`EffectText.cs:5610-5620`（deploy 支）；消费 `EffectResolver.cs:9710/9753` | 常驻「部署载具给护甲」 |
| UM40 | Avenging Zeal | ✅ | 0,650 | 30,40 | 1,2 | V:give@1122 ×4 实跑；T650 | Codex 降费支未触发 |
| UM37 | Death from Above | ✅ | 0,145 | 10 | - | V:deal@1064 实跑 4 伤 | Codex 加伤未触发 |
| 315d9f…511c | Exhortation of Rage | ✅ | 0 | 0,30 | 1 | V:give@1122 实跑 +1 远程 | — |
| UM110 | Fall Back | ✅ | 0 | 140,360 | - | V:return@1086 实跑（FIX_A 回手）；360=`EffectResolver.cs:18781` | Oath 2 支未验 |
| UM118 | Indomitus Crusade | ✅ | 0,410 | 10,70,360 | - | V:deploy@1141 实跑（UM101） | Oath 6 群伤未验 |
| UM39 | Inspired Retribution | ✅ | 0,410 | 110,320 | - | V:destroy@1087 实跑 | — |
| dcbd28…de1e6 | Master Tactician | ✅ | 0,145 | 0,30 | 1 | V:draw@1074 实跑（抽 filler28） | Codex 护甲支未验 |
| UM116 | No Mercy | ✅ | 0,410 | 10,110,360 | - | V:deal@1064 + V:destroy@1087 实跑 | — |
| UM34 | Point-Blank Shot | ✅ | 0,90 | 0,10 | - | V:deal@1064 实跑；T90 | 死亡抽牌未验 |
| UM74 | Rapid Deployment | ✅ | 0 | 30 | 2 | V:lowercost@1142；BT2 手牌侧 | handCostDelta -3 实测 |
| d94f91…71aa | Spear of Macragge | ✅ | 0 | 0,30 | 1 | V:drawtype@1075 + V:give@1122 | 实跑抽到 filler25 |
| UM61 | The Chapter's Due | ✅ | 0 | 0,300 | - | V:return@1086 + V:draw@1074；300→handmove `CardDef.cs:3502` | 墓地那半夹具无阵亡部队 |
| UM46 | Marneus Calgar | ✅ | 46 | 10,100 | - | T46；V:create@1140；主档实跑「手牌总价 +1」 | ⛔ pool_diff4 的 activeAbility 期望与 `hasActiveAbility=0` 冲突（§五1） |
| UM87 | Roboute Guilliman | ✅ | 46,145 | 10,20,100 | - | 同上 + V:heal@1073 | 同上 |
| UM48 | Varro Tigurius | ✅ | 46 | 10,100 | - | 同上 | 同上 |
| UM86 | Assault Doctrine | ✅ | 0,145 | 20,30 | 1 | V:give@1122 实跑 +3 攻 | Codex 护甲/治疗未验 |
| UM84 | Devastator Doctrine | ✅ | 0,145 | 30 | 1,3 | V:give@1122 实跑 blast3 | Codex +2 远程未验 |
| UM85 | Tactical Doctrine | ✅ | 0,145 | 0,30 | 1 | V:give@1122 实跑 +3 远程 ×4 单位 | Codex 抽牌未验 |
| EC22 | Veldras the Sublime | ✅ | 710,720 | 40,238 | 1 | T710/T720；238=`ForcedAttackRefund.cs:8` | 实跑 4/1→5/1 |
| EC61 | Decadent Throne | ✅ | 0 | 0,30 | 2 | V:choosecard@1133 实跑（抽 filler16）；BT2 手牌侧 | — |

## 三、真缺口清单（🟠）

**本组 0 条**。逐条核对后没有「某一面没有分支 / 静默跳过」。但有 **3 条结构性口径**必须写清（它们**今天不造成缺陷**，但会被误读成缺口）：

1. **`buffType 4`（UM7）与 `buffType 100`（EC39）在 `ApplyAbilityBuff` 里没有分支** —— 落 `default:`（`EffectResolverPartial_B97W2.cs:494`）只会出声「本表尚未实现」并 `return false`。**今天不构成缺口**：那张表**没有生产调用点**（同文件 `:158-162` 自述；`ResolveAbilityBuffRows` 全仓 0 caller），而这两张卡的效果由**正文层**承担（`lose` / `setcurrenthealth` 两支均在动词表）。原版该做什么：`CardScript__AddEffect.c:680`（case 4 = `RemoveActiveEffects(4,trait)`→`RemoveBuffedTrait`→`RemoveTrait`）· `:221`（`buffType 100` = `ShouldTriggerEcastasy` → `OnTrigger(0x2cb=715)`）。
2. **`buffType 2 changeCost` 只在【手牌侧】落地**（`RuleCorePartial_B115W20.cs:306`）；棋盘侧 `case 2`（`EffectResolverPartial_B97W2.cs:268-276`）只出声、`return false`。本组 EC15/UM40/UM75/UM74/UM20/EC61 的降费**全部由正文层 `lowercost` 或光环承担**，不经过这一支。
3. **`chosenAbility` 在触发路上不是执行分支**（`EffectResolver.cs:21368-21374` `BodyOp`）：除 `320/325/340/380/480` 外，效果**必须**由卡面正文 op 承担，正文 op 为空 ⇒ `NoteTriggerRowKindGap` 出声「本行不结算」（`EffectResolver.cs:21434-21458`）。⇒ 判「某张卡的 ta 有没有实现」时，**要看动词表 + 解析层，不能看 chosenAbility**。

## 四、查不到的（❓）

- **UM64 Angels of Death**（战术，「Deploy 3 Primaris Intercessor and give them Flank. Codex: Give +1 [Ranged] to your Primaris Intercessor」）
  · 实测：`playRc = 18 (ErrNoTargetAvailable)`、`played = 0` —— `EffectResolver.cs:2086-2088` 的「要选目标却没给格位」那两句。
  · 数据行：`trigger 0 / ca 75 summonBuffedUnit / unitsToSummon[UM6] / furtherMultiplier 3`，**该行没有 `targetCriteria`**；`trigger 145 / ca 320`、`trigger 410 / ca 30` 也都在。
  · 动词面本身齐：`deploy`@1141、`give`@1122、`320`=`EffectResolver.cs:18781`。
  · **查过**：`cards_truth.json` / `cards_engine.json` 的该卡全字段；`EffectText.PickTarget`（`EffectText.cs:1346-1372`）；`CanPlayTactic`（`EffectResolver.cs:2030-2095`）；`DoDeploy`；探针缓存 `all_dump.tsv`（`faceOps=1`）。
  · **判不了的那一步**：探针**不给战术卡传目标格**，而本卡解析出的 `deploy` op 带了 `Target`（`Side=own`）⇒ 无法区分「解析层把这句解成『必须点一格』（那就是**真差异**：原版三张是自动落位）」还是「本卡确实该点一格」。**需要**：跑一次 `ruleprobe play UM64` 带 `pick`，或在 Unity 里真打一次。

## 五、顺手发现（⛔ 只报，未改）

1. **`pool_diff4` 对 UM46/UM87/UM48 的期望本身错**：三行期望 `doDamage(activeAbility)`，但 `cards_truth.json` 这三张 `hasActiveAbility = 0`（`activeAbilityLogic` 也全是 `gate:0`）。按 `CardDefActiveAbility.cs:25-31` 的口径（原版 `CardScript__ResolveActiveAbilityPlayed.c:26-31` 闸），**原版也早退** ⇒ 这三行应改判「不适用」，不是缺陷。
2. **`EffectText.cs:5470-5476` 的注释过期**：它写「`give Armour 1 to Vehicles you put in play` 仍然判不认识」，但**同一函数下面 5610 起的 `②` 支**已经把它解成 `persist(AtTurnPhase="deploy")`，`DoPersist`（`EffectResolver.cs:7077`）+ `EffectResolver.cs:9710/9753` 都在消费（注释里还点名了 `Armoured Support`）。⇒ 按铁律 5 应就地订正那句注释。
3. **`ResolveAbilityBuffRows` 全仓 0 生产调用点**（`EffectResolverPartial_B97W2.cs:158-162` 自述） ⇒ `buffType` 整张表处于「已建、未接线」。`pool_effect_check.py` 若把 `buffType` 当判据来源之一，读到的是**死码**。
4. **`EffectText.cs:3522` 与 `8644`**：`equal to your Warlord's Melee` 的两处判据都在，`EC47` 的 410 半支今天在夹具里跑不出（因为「督军 +1 攻 → 群伤」两拍需要先触发 410）。
