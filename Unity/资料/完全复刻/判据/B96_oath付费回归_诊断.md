# B96_OATH · `play check` 28 张 oath 卡「少付」归因诊断（只读诊断，未改任何源码）

> 只读诊断代理 `B96_OATH` · 2026-10-29
> ⛔ 没改源码 / 账 / 正本 / git；⛔ 没跑 Unity（跑的是 `工具/ruleprobe.sh play check` / `play all dump`，dotnet 探针）。

---

## 〇、一句话结论

**归因 = (β) 修复，不是回归。** 28 张卡少付的那一笔，是 `B94_FX4` 在 `RuleCore.PayCardPlayedCostOath` 里加的那道
「卡面有 `Oath N:` 付费组（`CardDef.OathOps` 非空）⇒ 跳过 `SpendNormalMana`」的闸造成的；
而它去掉的，正是 `B94_F1`（单位卡）/ `B94_F2`（战术卡）加进去的「**手牌打出时再付一次誓约**」。
**原版对手牌出牌只收卡费**（`PayCardCost.c:23/27` 只读 `EntityScript.get_CurrentCost`），
誓约值只在两处收：① **单位**用主动能力（`UsedActiveAbility.c:41-44` → `PayActiveAbilityCostOath.c`）·
② **战术卡**的出牌誓约行（`abilityContext 50 ＋ chosenAbility 360` → `PayCardPlayedCostOath.c`）。
⇒ **基线 `228f4304…` 是在那道闸落地【之前】钉的，它本身是错的；现在这一版才对。**

⚠️ 但**闸的位置和注释都写歪了**（详见 §四），且它顺带压掉了原版 `.c:24` 的那一下 Codex 检查；
建议按 §四 的「结构性修法」收口，**⛔ 不要为了「让指纹回到基线」把闸撤掉**（那会把一笔原版没有的钱加回去）。

---

## 一、时序（为什么基线是错的 —— 这是本次最关键的一条证据）

| 时刻（2026-10-29） | 事件 | 出处 |
|---|---|---|
| **03:11:48** | 生成 `工具/ruleprobe/基线/out_play_baseline.txt`（= 基线 `228f4304…` 的来源） | 文件 mtime |
| 03:12:26 | `PlayProbe.PinnedPlayMd5` 改成 `228f4304…`（`PlayProbe.cs:4344`） | 文件 mtime |
| **03:56:11** | `报告/B94_FX4.md` —— **那道 `OathOps` 闸就是这一批加的**（报告 §2.1 明写「改前 3 行 / 改后 3 行」） | 文件 mtime + 报告原文 |
| **04:07:02** | commit `cef436cd`（第九十四会话）—— **把闸和旧 pin 一起提交了** | `git log -1` |

⇒ **pin 早于闸 44 分钟**。`FX4` 的修复落在「先测后钉」的 **钉之后**，而它的报告没有要求重钉 ⇒ 基线停在了
「有 `B94_F1` 那笔多付」的中间态上。`PlayProbe.cs:4344` 的常量注释把 28 张的差异写成
「**补上的原版行为，不是回归**」—— 那句话引用的是 `AbilityLogic__PlayAbility.c:2033`（`chosenAbility 360` 支），
但 **`B94_F1` 的调用点在 `PlayCardCore` 的手牌打出路径**，根本走不到 360 支（见 §三·3）。

---

## 二、改动对象：谁改了哪几行（工作树 vs HEAD 现读）

`PayCardPlayedCostOath` **在 HEAD 里就已经是「带闸」的版本**，工作树没有再动它
（`git diff -- RuleEngine/Core/RuleCore.cs` 里 `OathOps` / `PayCardPlayedCostOath` 零命中）⇒
**这 28 张的差异全部来自 HEAD 那次提交**，不是当前在飞的写手。

`RuleCore.cs:16970-16981`（现读原文）：

```csharp
        static void PayCardPlayedCostOath(BattleContext ctx, int p, CardInstance inst)
        {
            if (ctx == null || inst == null || inst.Card == null) return;
            if (p < 0 || p >= ctx.Players.Length) return;
            if (!inst.Card.Has(KeywordTable.Oath)) return;                     // `.c:10` `HasCurrentTrait(oath)`
            int amount = inst.Card.KwValue(KeywordTable.Oath);                 // `.c:21` `GetCurrentTraitValue(oath)`
            // 🔴 2026-10-29（B94_FX4 · DG1 γ1）：…（略）
            if (inst.Card.OathOps == null || inst.Card.OathOps.Count == 0)   // ← ★ 这道闸
                SpendNormalMana(ctx, p, amount, "誓约（" + (inst.Card.Name ?? "?") + "）", energySpentSignal: false, costType: 0);
            // `.c:24` `CheckIfCodexTriggered(…, shouldCheckCost: 0)`
            CheckCodex(ctx, p, inst, shouldCheckCost: false);
        }
```

原版 `d:/2/tools/decomp_full/BattleManager__PayCardPlayedCostOath.c` **全文没有这个条件** ——
`if (HasCurrentTrait(0x4fb)) { … UseMana(GetCurrentTraitValue(0x4fb)) … CheckIfCodexTriggered(…,0) }`，无条件。

**三个调用点**（`grep -rn PayCardPlayedCostOath Assets/`）：

| # | 位置 | 谁加的 · 是否原版 |
|---|---|---|
| 1 | `RuleCore.cs:3955`（`PlayCardCore`，**单位卡**打出） | `B94_F1` 新加 · **原版没有** |
| 2 | `EffectResolver.cs:2587`（`PlayTacticCore`，**战术卡**打出） | `B94_F2` 新加 · **对应原版 `chosenAbility 360` 那一支**（见 §三·3/4） |
| 3 | `EffectResolverPartial_B88E2.cs:993`（`RunPlayChosenCardAbility`＝360 支本体） | 旧有 · **原版就是这一条**（唯一真调用者） |

---

## 三、判据（原版到底该不该付）

### 3.1 原版全树只有 4 处 `PlayerManager.UseMana`

```
BattleManager__PayCardCost.c:27                 ← 手牌打出：只收 CurrentCost
BattleManager__PayActiveAbilityCostOath.c:27    ← 单位用主动能力（誓约）：收 GetCurrentTraitValue(0x4fb) ＋ 发 EnergySpentSignal(Type=2)
BattleManager__PayCardPlayedCostOath.c:22       ← 收 GetCurrentTraitValue(0x4fb) ＋ CheckIfCodexTriggered(shouldCheckCost:0)
RawCardScript__TriggerAbility.c:57              ← trigger 600 = UseSpiritStone，manaType = 5（SpiritStone），不是能量
```
（`grep -rn "UseMana" d:/2/tools/decomp_full` 共 6 个文件命中，去掉定义 `PlayerManager__UseMana.c` 与
`BattleManager__ProcessManaBuff.c` 就是上表 4 处；`ManaType` 只有 `Normal=0 / SpiritStone=5`。）

⇒ **手牌出牌这条路（`PayCardCost`）一分誓约钱都不收**，也没有第 5 处收费口。

### 3.2 `PayCardPlayedCostOath` 的调用者全树只有一个

- `decomp_full`：`grep -rl PayCardPlayedCostOath` = **2 个文件** —— 它自己的定义 + `AbilityLogic__PlayAbility.c:2033`。
- 交叉验证（`out_il2csharp/Assembly-CSharp/AbilityLogic.cs:2647`，比 C 伪码干净）：
  ```csharp
  else if (abilityLogic2.chosenAbility == AbilityEffect.playChosenCardAbility)
  {
      object obj627 = battleManager2.cardChosenInSelection;      // ← 必须是「已选中的那张」，null 就什么都不做
      ...
      bool flag25 = obj627.HasDefaultTrait(DefinedTrait.oath);
      if (flag25) { battleManager2.PayCardPlayedCostOath(abilityData2.actingCard); ... obj627.OnTriggerAlt(ThisCardPlayed, …); }
  }
  ```
  ⇒ 它是「**360 = playChosenCardAbility**」那一支里的**可选加价**，且**以 `cardChosenInSelection` 非空为前提**。

### 3.3 手牌打出时 `cardChosenInSelection` 是空的

出牌链（`PlayCardCore` → `ChargePaidGroups` / 单位路）**不会**给 `ctx.CardChosenInSelection` 赋值
（唯一两个写点是 `EffectResolver.cs:4845` 的 `DoChooseCard` / `CheckClickChosenCardDone`，都是「选一张卡」类效果）。
⇒ 即便把 `B94_F1` 的调用摆进 360 支的形状里，它也会在 `cardChosenInSelection == null` 处早退。

### 3.4 谁真的在出牌时带「誓约行」—— **只有战术卡**

扫全池 29 张 `oath` 卡（`cards_truth.json` 的 `abilities[].abilityContext == 50` 且 `logic[].chosenAbility == 360`）：

| 卡 | 类型 | abilityContext 50 行数 |
|---|---|---|
| `UM109` `UM110` `UM111` `UM112` `UM115` `UM117` | tactic | 1 |
| `UM116` `UM118` | tactic | 3 / 2 |
| `Angel's Wrath`(`4de4fcfb…`) / `Catechism of Death`(`8482077f…`) | tactic | 1 / 2 |
| **其余 19 张（全是 unit）** | unit | **0** |

⇒ **单位卡的誓约不是「出牌时付」**，它的誓约在 `activeAbilityLogic` 里（例：`UM101` 的 `activeAbilityLogic[0].ca=[10]`、
`dmg 3` ＝ 卡面 `Oath 2: Deal 3 damage`），走的是主动能力那条路。

### 3.5 单位卡 = 主动能力（现读三份）

- `CardScript__CanUseOathAbility.c:8` 首闸就是 `IsTheSameTurnPlayed` ⇒ **「部署当回合就能激活动」**，不是自动结算。
- `CardScript__UsedActiveAbility.c:41-44`：`if (HasCurrentTrait(0x4fb)) PayActiveAbilityCostOath(manager, card)` —— 用主动能力时才收钱。
- `CardScript__CanPlayWithOathNowInHand.c` 的**唯一调用者**是 `BattleCardUI__ChangeToHighlightColor.c:48` —— **纯高亮提示**
  （「你这点能量够打完这张还能再开誓约」），**不是一条独立的出牌动作**。
  （`CanPlayWithOathNowInPlay` 在 `decomp_full` 里**0 调用点**。）

⚠️ **一份唱反调的第二源（已核，按第二源处理）**：粉丝实体规则书
`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md:194` 写「**誓言 X（Oath X）：部署时支付 X 能量以触发效果**」。
它是**把线上游戏改成实体卡**的 fan 版（该文件头部自述 `Not official. Fan project.`），且它的说法与二进制冲突：
- 二进制里**单位卡没有「出牌誓约行」**（`abilityContext 50 ＋ chosenAbility 360` 的 29 张里 **19 张单位卡 0 行**），
  单位的誓约在 `activeAbilityLogic` 里、由 `CanUseOathAbility`（`IsTheSameTurnPlayed`）闸住的**主动能力**；
- 「部署时支付」只在**战术卡**上成立（那 10 张有出牌誓约行）。
⇒ 与本仓一贯口径一致：**二进制 > 粉丝规则书**（CLAUDE.md §二「权威 = ① 反编译 → ② 解包资源 → ③ 成品卡面」）。

### 3.6 实测：少付的金额逐张等于 `KwValue(Oath)`

`play check` 现跑（08:58）①-a 差异面 **恰好 28 张**（26 张 `UM89…UM118` ＋ 2 张 hash id 战术卡），
逐张 `energyDelta` 差值与探针实际用的 `KwValue(Oath)` **26/26 完全一致**（脚本逐张核过，0 例外）：

| 卡 | Oath 值（`cardTraits`） | `keywords` 写法 | `KwValue` | energyDelta 差 |
|---|---|---|---|---|
| UM92 | 3 | `Oath 3` | 3 | 3 |
| UM105 | 2 | `oath 2` | 2 | 2 |
| UM112 | 4 | `Oath 4` | 4 | 4 |
| UM89 / UM101 / UM111 / UM118 … | 2 / 2 / 8 / 6 | **裸 `Oath`** | **1** | **1** |
| 其余同族 | 1~4 | 裸 `Oath` | 1 | 1 |
| Angel's Wrath | 2 | `oath 2` | 2 | 2 |
| Catechism of Death | 3 | `oath 3` | 3 | 3 |

⇒ 少付的那笔钱**就是** `PayCardPlayedCostOath` 里的 `SpendNormalMana(amount)`，**证据闭合**。

---

## 四、建议（**不是**恢复补丁 —— 因为不该恢复到基线）

### 4.1 为什么不能「把闸撤掉」

撤闸 ⇒ 28 张回到基线的「多付」。但 §三 已证：**单位卡原版出牌时不收誓约**，战术卡的原版收费由
`ChargePaidGroups` 的 `Oath N:` 前缀（= 同一个 N）已经收过了（实测：`Angel's Wrath` 现付 1+2=3、
基线 5；`Catechism` 现付 0+3=3、基线 6）。撤闸 = 单位卡凭空多一笔 + 战术卡双计。

### 4.2 推荐的结构性修法（行为与今天**完全一致**，只是把三处摆正）

**① 删掉 `RuleCore.cs:3955` 里那一句**（单位卡出牌口，原版没有）：

```csharp
// 改前
            ConsumeOnceCostMods(ctx, p, inst);  if (card.Has(KeywordTable.Oath)) PayCardPlayedCostOath(ctx, p, inst);   // 🆕 2026-10-29（B94_F1 …）
// 改后
            ConsumeOnceCostMods(ctx, p, inst);   // 🔴 2026-10-29（B96_OATH）：原版手牌出牌只收卡费（`PayCardCost.c:23/27`）；誓约由主动能力收（`PayActiveAbilityCostOath.c`）⇒ ⛔ 别在这里加誓约费
```

**② 删掉 `EffectResolver.cs:2587` 里那一句**（战术卡出牌口，誓约已由 `ChargePaidGroups` 的 `Oath N:` 前缀收过）：

```csharp
// 改前
            RuleCore.ConsumeOnceCostMods(ctx, p, inst); if (card.Has(KeywordTable.Oath)) PayCardPlayedCostOath(ctx, p, inst);   // …
// 改后
            RuleCore.ConsumeOnceCostMods(ctx, p, inst);   // 🔴 2026-10-29（B96_OATH）：战术卡的誓约钱由 `ChargePaidGroups` 按 `Oath N:` 前缀收（实测 Angel's Wrath 1+2 / Catechism 0+3）；原版那一笔就是 `PayCardPlayedCostOath`，⛔ 别两处都收
```

**③ `RuleCore.cs:16976-16978` 恢复成 `.c` 逐句 1:1**（去掉闸；这样 `RunPlayChosenCardAbility` 那条**原版真的会收钱**的路才不被静默改掉）：

```csharp
// 改前
            // 🔴 2026-10-29（B94_FX4 · DG1 γ1）：…（略）
            if (inst.Card.OathOps == null || inst.Card.OathOps.Count == 0)   // …
                SpendNormalMana(ctx, p, amount, "誓约（" + (inst.Card.Name ?? "?") + "）", energySpentSignal: false, costType: 0);
// 改后
            SpendNormalMana(ctx, p, amount, "誓约（" + (inst.Card.Name ?? "?") + "）",
                            energySpentSignal: false, costType: 0);   // `.c:22` `PlayerManager.UseMana(amount, Normal, …)` —— ⛔ 不发 `EnergySpentSignal`；⛔ 别加条件（原版 `.c` 无条件）
```

> ⚠️ ①②③ 必须**同批**做：只做 ③ 会把战术卡变回双计，只做 ①② 会留着一条与 `.c` 不符的函数体。

### 4.3 如果调度台只想先止血

保持现状（不改代码），**在写手全停的静止点重钉 `PinnedPlayMd5`**，并把 `PlayProbe.cs:4344` 那句
「补上的原版行为，不是回归」**就地订正**成「这是 **B94_F1/F2 多收的那一笔被 FX4 的闸去掉**；原版的收费点见 §三」。

---

## 五、顺手发现（二次缺陷，都要记账）

1. 🔴 **`KwValue(oath)` 有 20/28 张读成 1**（`cards_engine.json` 的 `keywords` 是**裸 `"Oath"`**、真实 N 在 `cardTraits` 的 `"oath 2"`）：
   `UM101` 卡面 `Oath 2` → `KwValue` = **1**；`UM111` 卡面 `Oath 8` → **1**。
   受影响 = `PayCardPlayedCostOath` 的 `amount`（现在只在 `RunPlayChosenCardAbility` 那条休眠路里可见）
   —— **但它是真的错的**，原版读 `GetCurrentTraitValue(0x4fb)` = 2/8。
   （`CardDef.OathCost`（`CardDef.cs:4609/4624`，取 `OathOps` 里最大的 `op.Cost`）是对的，誓约主动能力那条路**没受影响**。）
2. ⚠️ **闸把 `.c:24` 的 Codex 检查也一起压掉了**（闸在 `SpendNormalMana` 那一行，但 Codex 那一句在闸外、照跑 ——
   实际压掉的是**「先扣钱再查 Codex」里的扣钱**；对 10 张带出牌誓约行的战术卡，`manaLeft` 会多 N ⇒ Codex 的
   `shouldCheckCost:0` 判据读到的余能与原版不同）。池内这 10 张目前**都不带 `codex`**，所以今天零可观测，
   但 `B94_FX4` 报告的「`.c:24` 那一跳照跑（与收不收钱无关）」这句**不成立**（Codex 读的正是收完钱之后的余能）。
3. 🔴 **指纹现状已经不再是 `431d7b0d…`**：我 08:58 现跑 = **`5580b29e1c6c805797f20251616acd36`**；
   ①-a 仍是 **28 张**（= 本问题），但 **①-b 事件电池有 48 张变了**，其中 **17 张与 oath 无关**
   （`ASH6/12/62/63/69/77/79/80 · GOF91 · SAU6/9/12/15/50/69 · SOR69 · SW34`）——
   那是**别的写手在飞的改动**（工作树 `EffectResolver.cs +495` 那批：`BroadcastUnitDamaged` 外加支 /
   `Summon`/`Transform` / `RemoveExtrinsicEffectsFrom`）。
   ⇒ **现在重钉会把别人半成品的行为一起钉进去**（`调度台日志` §23·31 的「并发写手期闸读数不可信」同一条）。

---

## 六、没查清 / 需要下一手

1. **原版「`Oath N:` 前缀」到底在哪一句被收**：原版只有 4 处 `UseMana`，我推断战术卡那一笔**就是**
   `PayCardPlayedCostOath`（由 `chosenAbility 360` 支触发），**没有**第二条独立的「前缀收费」。
   我们 `ChargePaidGroups` 的前缀模型是否是它的等价物，金额上已实测等价（N 对 N），**机制上未逐句对上**。
2. **`abilityContext 50 = InPlayOrCemetery` 的出牌语义**：`UM118` 有两行（`Oath 6` 与 `10` 那行 `trigger 410`），
   我只核到「它挂在 ThisCardPlayed（trigger 0）上、闸是 `triggerIf 2 (EqualOrHigher) + referenceValue N`」，
   `countCriteria.targetsAffected 160` 到底数的是「剩余能量」还是别的量 —— **没查到 160 的枚举名**。
3. **池内 29 张 `oath` 卡，①-a 只动了 28 张** —— 差 1 张（29 − 28），**没逐张定位是哪张、为什么不动**。
4. `RunPlayChosenCardAbility` 的可达性：`cards_engine.json` 的 `activeAbilityLogic[].ca` 里 **360 出现 0 次**
   （`chosenAbility` 字段里才有 360，那是**出牌时**的能力表，不是主动能力表）⇒ 那条路今天**池内不可达**，
   所以「闸把 360 支的合法收费也压掉了」目前**零可观测**。B94_F2 报告里「`chosenAbility 360` 池内 18 张」的
   计数口径与 `activeAbilityLogic` 不同，需调度台确认。

---

## 七、复现命令（可照抄）

```bash
cd /d/4/Unity
bash 工具/ruleprobe.sh play check            # rc=1，①-a「28 张卡变了」
bash 工具/ruleprobe.sh play all dump <tmp.tsv>
```
现读：`基线 out_play_baseline.txt（主档 1129 ＋ 电池 1073）· 指纹当次 = 5580b29e1c6c805797f20251616acd36 ≠ 228f4304…`
