# B9_L4 —— 战斗逻辑驱动 · 四类剩余 B（79 条）

**块**：`RawCardScript`(20) + `BattleManagerSupport`(20) + `PlayerHand`(15) + `MinionManager`(13) + `EntityScript`(7) + `SupportMethods`(4) = **79 条**
**补丁**：`资料/完全复刻/账/_parts/B9_L4.tsv` —— 26 行（4 升 A + 22 行就地订正/重定位）
**日期**：2026-10-26 · 代理 `L4`

---

## 一、结论（合计）

| 判 | 条数 | 说明 |
|---|---|---|
| **升 A** | **4** | 全部在 `PlayerHand`：`AddCardNotDrawnToHand` · `AddCardNotDrawnToHandAtIndex` · `CardPlayedWithEffects` · `CheckEffectsOnNewCard` |
| **留 B** | **75** | 逐条一句话理由见 §二 |
| **判 C**（我们没做） | **0** | 本块没有「原版有、我们完全没有」的条目 |
| **判 D**（不复刻） | **0** | 唯一的候选（`GetExtraAbilityTraits` 全树死代码）已由调度台 2026-10-26 裁定「死代码不足以判 D」⇒ 留 B |

> ⛔ 本块 22 行「就地订正」不等于改判：其中 **6 条旧差异被证伪/过期**（见 §三），**3 条旧落点是 const 或指错方法**。

---

## 二、留 B 的逐条一句话理由

### RawCardScript（20，全部 B）

| 方法 | 一句话 |
|---|---|
| CanPlayAbilities | 原版只判「有目标」就放行，我们还要过能量/次数/ward ⇒ **我们更严**（真差异）。 |
| GetAbilitiesForTriggerRawcard | 原版返回**列表**（同 trigger 可多条），我们按 trigger 归一成一组。 |
| GetActiveAbility | 17 行逐句对过，但落点只能到字段/属性/switch case（调度台已 A→B）；且 `MeetsCriteria` 我们多一态 `null`。 |
| GetExtraAbilityTraits | 判据（25 对表 + 六档阵营表）已齐但**尚未照搬**，0 落点；照搬后可达性仍 0（死代码）。 |
| GetOriginalId | 异画不建档 ⇒ 本地**无对象**；旧落点 `LegacyCardIds` 不是等价物（旧卡号迁移表）。★ |
| GetTargetsMeetingCriteria | 判据已读全 47 行；原版一个方法，我们拆 `ResolveTargets`+`IsLegalPick`+`TargetCriteriaEval.Matches` 三层。★ |
| GetTransformedKnightId | 原版是 `pilotId → 骑士 id` 的映射表（`+0x208`），我们按 op 目标现查卡池；那张表的内容未核。★ |
| GetValidTargets | 判据已读全 51 行；原版「枚举（unitsInPlay）+ 单格校验」一个方法，我们只留单格校验（枚举在调用方）。★ |
| IsValidActiveAbilityTarget | 原版对「没主动能力」是 LogError + 拼诊断串，我们返回错误码。★ |
| IsValidTarget | camouflage 那道**我们比原版严**（已知偏离，池内 0 张待裁）；旧「`0x46` 未知 trait」已查明 = **stealth(70)**。★ |
| OnCardPlayedWithTarget | 120 行的分叉**未逐句核**（只读前段）。★ |
| OnCardPlayedWithTargetJustThisCardPlayed | 原版是独立的自指触发入口，我们用 trigger 名区分。★ |
| OnQuestResolved | 原版 trigger 490 + `createCardInDeck`；**我们没有 490 分发**，按督军阵营代理 ⇒ 真行为差。★ |
| OnTriggerAlt | 原版是独立的「替代」触发入口，我们「选支后再分发」；被调方 `TriggerAbilities` 本身仍 B。★ |
| OnTriggerEffectInCard | 260 是 **chosenAbility** 枚举值（与 `AbilityTrigger 260` 不是一套），我们走 `ChooseEffectPools`。★ |
| ResolveAbilities | 粒度不同（原版逐 `AbilityLogic.PlayAbility`、我们逐 `EffectOp`）；`randomAbilityChoice` 那格未核。★ |
| ResolveActiveAbility | 选支逐句等价，但原版「**先播动画**再执行」与我们的「先结算、表现层再播」次序不同。 |
| TriggerAbilities | 272 行**只读了签名**（本族最大一块），逐条遍历 / 多条同 trigger 处理未核。 |
| TriggerAbility | 116 行**只读了签名**，选支 / enchantment 分支未核。 |
| UpdateTriggerOnce | trigger 200 池内 0 条，且我们**没有 200 这一档分发**（有卡也接不上）。 |

### BattleManagerSupport（20，全部 B）

| 方法 | 一句话 |
|---|---|
| BroadcastCardPlayed | 我们传 `preCalled=null`（打出那张**不被排除**）+ 棋盘跳带「待死」闸。 |
| BroadcastCollectSpiritStone | 已收窄为 `boardAndPersistentOnly`；仍**缺预调**（原版预调实参 = 那颗残骸本身，我们传 null）。 |
| BroadcastDeadUnit | 缺原版第 4 支（手牌里**不在场**那张）；死亡载荷（原因/格位）没进事件。 |
| BroadcastEarnedSkull | 广播器逐句同形，但**调用点未接**（缺 `earnedSkull` 计数 + 血量写完后的检查点）。 |
| BroadcastRemoveEffectsFrom | 我们没有「一个动作让全员按来源摘」的统一入口，只有单元级口 + 整份 `Recompose`。 |
| BroadcastSabotageCreated | 已收窄；仍缺「对造出来那张预调」（那张卡不在棋盘、没有 UnitState）。 |
| BroadcastSecretCreated | 已收窄；仍缺 `Signal.Raise(CreateSecretSignal)` 与预调主体。 |
| BroadcastTurnSetup | **真缺口（休眠）**：我们从不发 155。 |
| BroadcastTurnStart | 原版同一回调**连发 30 再 31**，我们二选一（池内无一张两者皆有 ⇒ 今天零可观测差）。 |
| BroadcastTurnStarted | **真缺口（休眠）**：我们从不发 160；与 30/31 是两个不同触发。 |
| BroadcastUnitAttacked | 自打自己时原版发**两次**预调我们一次；且我们的 Attack 广播**不携带 attackType**。 |
| BroadcastUnitDamaged | 缺「**攻击者必收一次**」那一支（根因 = 伤害广播不带 damageType）。 |
| BroadcastUnitHealed | 广播器逐句同形，**调用点未接**（原版唯一生产点 = `CardScript.UseSurvivor`，⛔ 不是 Heal）。 |
| BroadcastUnitMob | 自排除按 **subject**，原版按 **caster** ⇒ 触发者 ≠ caster 时集合不同。 |
| BroadcastUnitObtainedHuntMark | 逐句同形，唯一差 = 我们棋盘跳带「待死」闸。 |
| BroadcastUnitObtainedMark | **没有预调** ⇒ 手牌里那张「收到 Dark Pact」的监听卡收不到。 |
| BroadcastUnitPoisoned | **真缺口（休眠）**：我们从不发 290。 |
| BroadcastUnitReanimated | 差两点：原版先 `Signal.Raise`（无对应物）+ 我们棋盘跳带待死闸。 |
| BroadcastUnitRegiment | 可达 0（休眠）；自排除 subject vs caster 同 Mob。 |
| GetTargetUnit | 旧记把 `friendlyHero/abilityTarget` **读反了**；我们按 effect owner 取侧、原版按 `actingCard` 座位，且我们没有 abilityTarget 这一档。 |

### PlayerHand（15：4 A + 11 B）

| 方法 | 一句话 |
|---|---|
| **AddCardNotDrawnToHand** → **A** | 判据读全（d39 :29/:34/:53-64）；落点 `RuleCore.cs:3128-3146`。★ |
| **AddCardNotDrawnToHandAtIndex** → **A** | 判据读全（d40 :29/:32-35/:59-65）；落点 `RuleCore.cs:13988-14009`。★ |
| **CardPlayedWithEffects** → **A** | 判据读全 52 行；落点 `RuleCore.cs:10276-10311`（次数核销）。★ |
| **CheckEffectsOnNewCard** → **A** | 与原版 `SetupCardInHand` 引擎半**逐句同**；落点 `RuleCore.cs:10629-10691`（§4.1 那一档）。★ |
| CompleteMulliganPhase | 分工合成一条链、净次序同；但原版对**双方**牌库各洗一次，我们只洗换过牌那一方。 |
| GetMulliganCardsToDiscardIds | 原版传 **card id**，我们传**手牌下标**（联机线格式不同）。 |
| GetWhispersCardsCount | **判据读不到**（decomp 文件名与内容不符 = 匿名类 `GetHashCode`）。 |
| MoveCardToMulliganPos | 表现层量；序列化字段已核，父链 `lossyScale≈0.794` 的差额待实机复测。 |
| RemoveEffectsFromCard | 原版按**施放者卡**反查后整条**全局**摘；我们没有同形入口。★ |
| RemoveFromGameCard | 「移出游戏」**我们有**（`ctx.Removed`）但只有临时卡那一条路，缺通用入口。★ |
| RemoveHandEffectAt | `RemoveHandEffectFromHand` 对**不限次**记录恒空转（今天全部）；摘除走 `ExpireHandBuffs` 到期支。★ |
| ReplaceMulliganCard | 逐张换 vs 整批「洗后再抽」⇒ **牌库排列不同**（分布同）。 |
| SetMulliganCardsToDiscard | 原版有「记录 → 确认」两态，我们一次提交（无中间态）。 |
| SetMulliganCardsToDiscardStart | 整批校验**已实现**；仍差 id-vs-下标 键 + 「打勾」那半在表现层。★ |
| SetupCardInHand | 落点只覆盖**引擎半**，界面四步在 `CardPresentation/Hand/`（另一域）。★ |

### MinionManager（13，全部 B）

| 方法 | 一句话 |
|---|---|
| ActivateMinions | 原版回合开始写旗标，我们动作前现算（净效果同、时机不同）。 |
| AreMinionsWithAmbush | 这个**查询**我们没做（无消费者），我们是事件驱动翻面。 |
| Awake | 公式 18 格逐格同（Δ≤2.5e-7）；差 `slotsPerSide` 来自运行时 SO（离线取不到）+ `cardShadow` 无对应物。 |
| BroadcastActivateMinion | 三条机制差：无集中广播方法 / 督军不参与 / 召唤病无条件清（原版带回合号守卫）。 |
| FillMinionPositions | 公式同；「它与 Awake 的调用关系」查不到（0 调用者，两种解释都站得住）。 |
| GetClosestAvailableSlot | 原版在屏幕坐标里算最近格，我们只留「格号 → 插入位置」。 |
| GetMinionList | 排序键（缓存委托）本 `.c` 读不出；遍历顺序无读数口。 |
| GetNextSlotPos | 落点只覆盖「位置→格号」；多格需求选侧 / 满盘回督军锚**无等价物**。 |
| GetNextSlotToCard | 「贴着这张卡（`IndexOf+2`）」两支**未核**在 `summonPos` 里有没有对应物。 |
| GetOwnPositionInBoard | 格号半边有（`BoardSlots.OriginalIndexOfCard`），坐标半边在表现层。 |
| ReassembleMinionsWhilePlayingUnit | 纯拖拽手感（我们在预览时就位移），需真机验收。 |
| RemoveSwarm | 原版**纯关图标**；引擎属性的回滚点**未查清**（不在 RemoveSwarm 两个方法里）。 |
| SlotPosToBoardPos | 归并进 `ArenaSlots.Position` 的 y 项（`ExtraYOnBoard` 13 场全 0 ⇒ 净恒等）；0 调用者 + 缺非自证读数。 |

### EntityScript（7，全部 B）

| 方法 | 一句话 |
|---|---|
| EnemyReturnsAttack | 预测版已接 `AiScoring.cs:582`；`SimpleAI.ScoreFromAttack` 仍用四道版（原版两条入口**本来就不一致**，待裁）；无离线读数口。 |
| GetActiveAbilityCard | 净等价（`buffType==40` 池内 0 条），但落点是**属性 getter ⇒ 不能 A**；且无读数口。★ |
| GetThisCardPlayedDamage | 本机**无聚合口**（`BattleDriver` 的注释逐字记着）。★ |
| HasCurrentTraitActiveEffect | 与账 1180 **共用同一落点** ⇒ 类级命中 ⇒ 只能 B。 |
| HasEnchantment | `enum → 关键词` 映射与 `TriggerOps` 覆盖未证；原版另外三个 gate 无对应物。 |
| ReachedQuestCounter | 原版第一条判据 `cardType == Whispers` 我们**没有这个 Type**（推断不是判据）。 |
| RevealedSecret | 我们没建「已揭示」这一位（`RuleCore.cs:7915` 已如实记录）。★ |

### SupportMethods（4，全部 B）

| 方法 | 一句话 |
|---|---|
| GetCardListFromIds | 原版结果表**保留 null 洞**（下标对齐），我们返回单值 + 调用方跳过。 |
| GetRandomOrder | 三条：Fisher-Yates vs 按名次挑选（同种子排列不同）· RNG 形状（预烧 100+下标 vs 每次 `Next`）· 钉位池内不可达。 |
| ShuffleCardList | 同 1308（另有 `checkStratagem` 钉位支）。 |
| ShuffleRawCardList | 同 1308（无钉位支）。 |

---

## 三、本轮**证伪 / 过期**的旧记录（铁律 5：已就地订正进账）

| 账行 | 旧说法 | 实况（判据） |
|---|---|---|
| 1153 `CardPlayedWithEffects` | 「原版 numberOfUses 是全局一条，我们每份牌一份 ⇒ 真差异」 | **我们也是共享盒**：`MakeHandBuffUses` 注释「一个盒子发一批牌」、`AttachHandEffectCopy` 照抄同一引用 ⇒ 与原版同形。**升 A**。 |
| 1148 `AddCardNotDrawnToHandAtIndex` | 「两处「打不出去 ⇒ 留回手里」仍是 `ps.Hand.Add`，已交 F2」 | 批次 6 `G14` ② 已改走 `AddCardToHandAtWithinLimit`；现读全仓 `ps.Hand.Add` **仅 1 处**（autoPlay 暂存，原版同形）。**升 A**。 |
| 1190 `SetMulliganCardsToDiscardStart` | 「我们缺整批校验 ⇒ 部分生效」 | 批次 4 `F1` 已实现（`RuleCore.cs:459-471` + `MulliganCollectIndices:13867`）。 |
| 1186 `RemoveFromGameCard` | 「我们只有 Discard 一条出口、没有「移出游戏」这一档」 | **有**：`ctx.Removed` / `RemovedCard`（`BattleContext.cs:1513`，`SweepEphemeral:2865` 写入）。缺的是**通用入口**。 |
| 1185 `RemoveEffectsFromCard` | 「摘的是这一张，只动这一张」 | 原版反查 `enchantingCard`，命中后走 `RemoveHandEffectAt` ⇒ 摘的是**所有**还带着它的牌。 |
| 1270 `OnQuestResolved` | 「逐句等价（薄包装），触发号 490 与我们的表一致」 | **没有 490 分发**（`TriggerNameOfWhenKind` 无该分支，`CardDef.cs:1934` 只是名字表）⇒ 真实实现是**阵营代理**，属真行为差。 |
| 1262 `IsValidTarget` | 「camouflage 与 `0x46` 两道闸没核清」 | `0x46` = **stealth(70)**（已实现 `RuleCore.cs:11107-11109`）；camouflage 在 `AddSide` 的 stealthGate 支（:2843-2849），**比原版严**（已知偏离）。 |
| 1242 / 1281 / 1286 / 1264 / 1265 / 1277 / 1270 | 落点指向 **const `TN_OtherCardDrawn`** 或 `ResolveTargets` | const / 挑目标 ≠ 该行为 ⇒ 已改到真方法（`PlayTacticBodyDeferred` / `ResolveOps` / `QuestPointThreshold` / `UsesAlternativeAbility`）。 |
| 1037 / 1186 | 落点里带 `BattleDriver.cs:33184` / 缺 `RuleEngine/` 前缀 | 前者现读是**无关语句行**、后者路径不存在 ⇒ 已改。 |

---

## 四、没查清（交接，⛔ 别当成「已查」）

1. **`RawCardScript` 三个大方法体没读**：`OnCardPlayedWithTarget`(120 行)、`TriggerAbilities`(272 行)、`TriggerAbility`(116 行) —— 只读了签名/前段。这是本块**最大**的未读口。
2. `GetTransformedKnightId`：`+0x208` 那张 `pilotId → 骑士 id` 映射表的内容与匹配规则。
3. `GetWhispersCardsCount`：判据读不到（decomp 文件名与内容不符）⇒ 只能走 VA 反汇编。
4. `RemoveSwarm`：引擎属性回滚点不在 `MinionManager.RemoveSwarm` / `CardScript.RemoveSwarm` 里，**没找到**。
5. `MinionManager.GetMinionList`：排序键（缓存委托 `FUN_181a60060`）身份读不出 —— 需 ILSpy/VA 另证。
6. `GetNextSlotToCard`：「贴着这张卡」两支在 `summonPos` 里有没有对应物（`PlayAbility` 2878 行没读全）。
7. `EnemyReturnsAttack` 第三条入口（`CardHighlight__ToggleCombatPreviewHighlight` 战斗预览高亮）**仍未接**。
8. `IsValidTarget` 的 camouflage：「我们比原版严」要不要按卡种收窄 ⇒ **等调度台裁**（池内 0 张，今天零行为）。

---

## 五、交接（给下一个代理）

- **下一块建议**：`BattleManager`(181) + `CardScript`(72)（本块剩下的 `BattleManagerSupport` 三条真缺口 —— 155 / 160 / 290 —— 都在那边落地：相位钩子 + `WhenEventKind` 常量）。
- 本块 B 里「**只差一个读数口**」的六条（最可能下一轮升 A）：`GetActiveAbilityCard` · `ActivateMinions` · `Awake` · `FillMinionPositions` · `SlotPosToBoardPos` · `EnemyReturnsAttack`。
- **已证伪的旧等价（⛔ 别再照着写）**：`LegacyCardIds` ≠ `GetOriginalId`；`ResolveTargets` ≠ `ResolveAbilities`/`TriggerAbility`；`AddCardToHandWithinLimit` 不是「移出游戏」；`TriggerNameOfWhenKind` **没有** 490；`0x46` = **stealth(70)**；`RemoveEffectsFromCard` 摘的是**全局**。
- **行号**：`RuleCore.cs` / `EffectResolver.cs` 本次会话被别的写手加了行（现读比旧账整体 +45 左右漂移）⇒ 本补丁写的都是**现读**行号；此后再有插入会再漂，按**裸声明名**重定位。
- **纪律**：本代理未改任何 `.cs`、未跑 Unity、未动 git、未写主账表（只写 `_parts/B9_L4.tsv`）。
