# B139 诊断 · OnSabotageCreated 4 红

## §0 一句话结论

**是 `81ea4fbb` 引入的。** 类 = **γ 本批回归**，回归点本身是**正确的原版行为**，它暴露出的真缺陷是 **β 实现缺陷**：
破坏（Sabotage）广播的 `subject` 取了**施放方**的督军，而原版取的是**造出来那张破坏卡所在手牌那一方（= 敌方）**的督军。

- 引入点：`Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs:7823`
  `case WhenEventKind.GetsStun: case WhenEventKind.CreatesSabotage:`（B128 · 2026-11-03 把 `CreatesSabotage` 加进
  ability 级 `targetCriteria` 闸）。`git log -L` 显示这一行**最近一次改动就是 `81ea4fbb`**，此前从 `f84cfff2`（第七十五会话）起没动过；
  工作树该文件 `git status` 干净 ⇒ B139 没碰它。
- 时间窗佐证：同一条断言在 `_tmp_view/re111_RuleEngineTest.log`（11-01 03:03）、`fin_…`（10-31 20:02）、`host_…`（10-31 18:44）**都是 ✓**；
  本次 `unity_b139_RuleEngineTest.log`（11-04 12:42）✗。
- 「4 红」= **2 条唯一断言 × 2**（失败点 979604 / 979619 ＋ 结束汇总 1049005 / 1049006 各印一次）。
- 断言本身**不是**错的（α 否），夹具的合成 subject 也只是**忠实反映了我们错误的形状**（δ 不是主因，但要跟着一起改，见 §2）。

## §1 三问逐条回答

### 1. `OnSabotageCreated` 的映射从哪来？

**手写 switch，不是生成器产物。** `RuleEngine/Core/EffectResolver.cs:7703`：

```text
case WhenEventKind.CreatesSabotage:  return "OnSabotageCreated";    // 701
```

（同族 `:7701` GainQuest → `OnCollectQuestPoints`、`:7702` CreatesSecret → `OnSecretCreated`，批次 6 `G14` 补的。）
断言文案里那句「🧨 删掉映射 / 生成器没重跑」对**本条失败不适用** —— 它描述的是另一条路径
（`工具/gen_card_vfx_by_card.py` → `card_vfx_by_card.json`，喂 `VfxMap.SelfCheckRoutedTriggers`），
那次件级自检与三张白名单表本轮**全绿**（日志 980345 / 980361 / 980377 等）。

### 2. 现读产物里有没有 `OnSabotageCreated`？

**有，而且映射断言本身是绿的**（日志 979571：`TriggerNameOfWhenKind("createsabotage") = OnSabotageCreated` ✓）。
真正拦住它的是 **ability 级 `targetCriteria` 闸**，日志自己写明了：

- 日志 979586：`[ActionQueue] **不触发**「B108A1L701·listen:createsabotage」—— ability 级触发条件 **不满足**
  （卡「B108A1L701」· trigger=OnSabotageCreated）。 候选 = targetCard「B108A1Sub701」`

链条（逐处现读）：

- 闸的族白名单：`EffectResolver.cs:7823`（`CreatesSabotage` 在列）。
- 闸的被筛对象：`EffectResolver.cs:7838-7841` `AbilityLevelGateTarget` —— CreatesSabotage 不是 Attack/Swarm ⇒ 取 `subject`。
- 闸的执行点：`RuleEngine/Core/ActionPump.cs:508-527` —— `verdict == false` ⇒ 出声并 `return false`；
  返回 false ⇒ `FireBoardWhenListener` 的 `queued == false` ⇒ **连 `EvtKind.Trigger` 一起跳过**
  （`EffectResolver.cs:10545-10551`）。这正是断言数到 0 条的原因。
- 判据实现：`RuleEngine/Core/TargetCriteriaEval.cs:222-237` —— 候选与 ability 归属同侧就查 `friendlyUnits`，
  异侧查 `enemyUnits`，该格为 0 ⇒ `return false`。
- 夹具：`CardPresentation/Editor/B108_A1_TriggerEmit_Assert.cs:170-197`
  · criteria = `E6Criteria(1, 0)` = `enemyUnits=1 / friendlyUnits=0`（`RuleEngine/Editor/RuleEngineTest_EngineE6.cs:50-69`）；
  · 监听者放在 0 号位（`:185`），subject 放在 `eventWho == 0 ? 0 : 1`（`:186-187`），本族 `eventWho = 0`（`:61-63`）
    ⇒ **subject 与监听者同侧** ⇒ 命中 `friendlyUnits == 0` ⇒ 判死。
- 反证（同一函数、同一个闸）：`UnitStunned(291)` 的 `eventWho = 1` ⇒ subject 在敌方侧 ⇒ `enemyUnits = 1` ⇒ 过闸（日志 979946 ✓）。
  ⇒ 差别**只在 subject 的侧**，不在映射、不在生成器。

### 3. 现读事件里 `CardId` 装的是什么？

`EvtKind.Trigger` 这条路上 `CardId` 装的就是**监听者卡名**、`Keyword` 装 trigger 名：

- `EffectResolver.cs:8510-8511`（`EmitCardTrigger`）：`CardId = card.Name` · `Keyword = BattleEvent.KeywordWithAbility(triggerName, abilityIndex)`。
- 调用方传的 `card` = 监听者自己的卡：`EffectResolver.cs:8105`（`EmitListenerTrigger`）与 `:10551`（`u.Card`）。

⇒ 断言 `ev.CardId == "B108A1L701"` 的期望**是对的**；它红是**级联**（0 条事件 ⇒ `B108A1FirstTrigger` 回 null）。
CLAUDE.md 里那条「引擎事件把关键词放进 `CardId`」是 `BattleContext.Emit(kind, p, slot, cardId, …)`
**另一个重载**的坑，不走这条链。

## §2 怎么修（方向，不实施）

### 2·1 生产侧（真缺陷）：破坏广播的 subject 侧

原版链（逐句现读）：

- `d:/2/tools/decomp_full/BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:95-98`：
  `BattleManager__GetHero(mgr, *(char *)(createdCardScript + 0x40 /*isPlayer*/))` → `BattleManager__BroadcastSabotageCreated`。
- `BattleManagerSupport__BroadcastSabotageCreated.c:2-15`：该督军 = `param_2`；先 `ReactToSabotageCreated(param_2, param_2)`
  做预调，再遍历棋盘卡 `ReactToSabotageCreated(观察者, param_2)`。
- `CardScript__ReactToSabotageCreated.c:5-8`：`OnTrigger(0x2bd, …, thisCard=观察者, actingCard=param_2, targetCard=param_2)`
  ⇒ **acting = target = 那个督军**（签名单 `dump.cs:23487`）。
- 那个督军是**哪一方**：破坏卡落在**敌方手牌**（卡面 `Create a random Sabotage in the enemy hand`；
  数据 `cards_truth.json` GSC7 `summon.isForEnemy = 1`）；手牌侧由创牌目的侧决定 ——
  `BattleManager__AddNewCardToHand` 用 `param_4` 选手牌（`:52-57`）并透传给 `BattleCardManager__CreateCard` →
  `CardScript__CardSetup(..., param_4)` 写 `isPlayer = param_4`（`BattleCardManager__CreateCard.c:90`，`CardScript__CardSetup.c:42`）；
  `param_4` 就是**收牌方**（同形证据：`CardScript__ProcessCompanion.c:85` 把源卡的 `isPlayer` 传进去，副本进**同侧**手牌）。
  ⇒ **原版 `targetCard` = 收牌方（敌方）督军**。

我们：

- `EffectResolver.cs:4301`：`BroadcastCreatedCardWithWarlordPreCall(ctx, WhenEventKind.CreatesSabotage, owner, c, by);`
  传的是 `owner`（施放方）；`:18474-18475` 用它取 `ctx.Players[owner].Warlord` ⇒ **施放方督军**。**差一侧。**
- 后果：真卡 `GSC68 Atalan Jackal`（`When you create a Sabotage, deal 2 damage to a random enemy`）的
  `targetCriteria` = `{targetsAffected:10, enemyUnits:1, includeHero:1, includeMinions:1}`（`cards_engine.json` 现读，池内 701 **仅此 1 行 / 1 张**）
  ⇒ 同侧督军 ⇒ `friendlyUnits == 0` ⇒ **这张卡在我们引擎里静默失效**。
  原版是活的：它 701 那一拍有**真动画行**（`Unity/数据/游戏数据/ability_anim_durations.json` · GSC68 ·
  `otherAbilityAnims[].trigger = OnSabotageCreated` · `Atk_BulletImpact_autogun_2shot` 0.75s）。
- ⚠️ 旁证（不是必须）：原版 `actingCardCriteria` 的 `targetsAffected` 缺省 = 0 = `any`
  （`FilterMethods__CheckIfMeetsCriteria.c:56-60` 直接 `return 1`），所以 `friendlyUnits:1` 被短路，
  它与 `targetCriteria` 的 `enemyUnits:1` **并不矛盾** —— 真正生效的只有 target 那一闸。

改法建议：

1. 给 `BroadcastCreatedCardWithWarlordPreCall`（`EffectResolver.cs:18470`）加一个「subject 取哪一侧」的形参，
   缺省 = `owner`（`CreatesSecret` 走牌库那一路**不变**：隐秘进的是施放方自己的牌库，
   `d:/2/.../_ResolveCreateDeckCard_d__513:113-119` 只认 `spellType == 0x96`，主体仍是本侧督军）。
2. 破坏那一支传 `who`（`EffectResolver.cs:4269` `int who = op.Dest == "hand" ? owner : 1 - owner;`）当 subject 侧；
   `who`（事件归属 / `When you …` 措辞匹配）仍传 `owner`，⛔ 不要把两者合成一个参数。
3. 顺手把 `:7823` 那句判据注释就地订正（现在写的是「`param_2` = 造牌那一方的督军」，错；铁律 5）。

### 2·2 夹具侧（要同批改）

`B108A1ListenerBeat` 现在用**一个** `eventWho` 同时决定「who」和「subject 放哪一侧」（`:186-190`），
表达不出「who = 施放方、subject = 收牌方」。修生产之后，即使 subject 侧改对，这条合成夹具仍会红。
最小改法：给该函数加一个 subject 侧参数（或对 701 特例把 subject 放到 `1 - who` 侧），让它与生产形状同形。

### 2·3 两条**不要**做

- ⛔ 不要把 `CreatesSabotage` 从 `AbilityLevelGateApplies` 删掉 —— 原版对所有 trigger 都跑
  `acting → target → board` 这条 `&&` 链（`CardAbility__CanTriggerAbility.c:126-141`），删掉 = 比原版宽。
- ⛔ 不要为了变绿去改夹具的 criteria（例如换成 `E6Criteria(0, 1)`）——那会把真缺陷（subject 侧）藏起来。

## §3 顺手发现

1. **「4 红」其实是 2 条唯一断言**：失败点（979604 / 979619）与结束汇总（1049005 / 1049006）各印一次。
   本轮汇总行是 `RE === 结束：14988/14990 通过，**2 条失败**`，与 `RE ✓ 12,416`（打印行数）不是同一口径，⛔ 别拿两者互校。
2. 这条失败的 🧨 提示（「生成器没重跑」）会把人引向数据侧重跑，**本轮别去重跑** `gen_card_vfx_by_card.py` ——
   三张白名单 + 件级自检都是绿的，问题在闸的被筛对象。
3. 池内 `trigger 701` 的可达卡 **只有 GSC68 一张**（`cards_engine.json`：`abilityTriggers` 含 701 = 1 张，
   `abilityLevelCriteria.trigger == 701` = 1 行）⇒ 生产影响面 = 1 张卡静默失效；测试面 = 这条合成夹具。
4. 原版 `actingCardCriteria` 的 `targetsAffected` 缺省 0 会被 `CheckIfMeetsCriteria.c:56-60` 短路成 `any` ——
   这条对以后读卡表的人很关键：**「acting 与 target 两道闸看起来互相矛盾」时，先看 `targetsAffected` 是不是 0**。
5. B128 同一批还改了数值路（`EffectResolver.cs:21093` 给 `FireTriggerByNumberWithSink` 补 `abilityTrigger/abilityTarget`）——
   本轮 410 `FollowUpResolution` 的判定**没有红**，那条不需要动。
