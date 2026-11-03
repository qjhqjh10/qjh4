# B124 · buffType 25/26 叠加可达性 · 2026-11-03

只读诊断代理 `B124-R1`。判的是活状态表 `B120-W20c`（「`RuleCore.cs:16193` 的 `RemoveAll("ephemeral", silent:true)` 与原版 `RemoveTraitSilently` 不同源，今天零可观测」）到底站不站得住。

## 〇 结论（一句话）

**不可达 —— 而且不是「今天碰巧不可达」，是【结构上造不出两层】。** 原版 `CardScript.AddTraitSilently` 加 trait 前先 `Enumerable.Any(已存在 ⇒ return)`（**去重**），所以 `cardTraits(+0x128)` 里同一个 trait **永远最多一条** ⇒ `RemoveTraitSilently` 的 `Find + Remove`（「摘一份」）与我们的 `RemoveAll`（整份摘）**行为等价**。**`RuleCore.cs:16192` 不需要改**。

- 旧结论「今天零可观测」——**结论对，理由要换**：原来说的是「场上单位不读 ephemeral」；真正的硬证据是下面三条（数据面 / 原版去重 / 无 UI 物）。
- ⚠️ 行号订正（铁律 5）：`RemoveAll` 那一句现读在 **`RuleEngine/Core/RuleCore.cs:16192`**（文件 0 个裸 CR、18,486 行），不是 `16193`；`16193` 是紧跟的注释行。

## 一 数据面（全池逐行，整卡扫：`abilities` + `activeAbility` + `alternativeAbility` + 其余全部子树）

口径：`cards_truth.json`（1,351 张，无 `full_raw`），并用 `资料/普查产出_1018/allcards_1350/cards_raw.json`（同样跳过 `full_raw`）交叉复核，两份**逐行一致**。

| 查什么 | 结果 | 证据 |
|---|---|---|
| `buffType 25`（ephemeralCopy） | **1 行** | `TL69 Neurotyrant` · `cards_raw: cardAbilities/0/abilityLogic/1/buffEffect` = `buffType 25`（同卡 `abilities/0/logic/1/buff`） |
| `buffType 26`（tideCopy） | **0 行** | 两份数据源都是 0 |
| `buffType 3`（addTrait）且 `trait == 5` | **0 行** | 即「用 addTrait 记录给 ephemeral 叠值」这条路不存在 |
| 引擎侧其它载荷（附魔 / 选项卡 / 进攻环境 / AI） | 25/26 全 **0** | `enchantment_templates.json` · `choice_option_cards.json` · `offensive_env_cards.json` · `ai_scoring.json` 各自扫 |
| 卡面带 `ephemeral` 的卡 | **125 张，全部 `cardType 20`（战术/法术类）** | `cardTraits` 里有 `_trait:5`；`cards_engine.json` 里 cardType 20 = 500 张，且 `type == unit` 的 586 张里 **0 张**带 ephemeral |
| 每卡 `cardTraits` 有重复 trait id 的 | **0 张** | 逐卡去重比 |

**TL69 那一行为什么不构成叠加源**：它的目标条件是 `targetsAffected 150 (previousAbilityFiltered)` + `includeSpells 1` + `traitsFilter 5 / excludeTrait 1` ⇒ 打的是**上一条 logic（`chosenAbility 100`，造复制）刚造出来的手牌复制品**，而且**源卡必须非 ephemeral** ⇒ 复制品身上最多**一层**。同行的 `meleeAttackChange: 1` 是**死值**（`AddEffect` 的 `case 0x19` 只做 `AddTraitSilently(this,5)`，不读它）。

## 二 代码面（能不能叠第二层）

### 原版（`d:/2/tools/decomp_full/`）

- `CardScript__AddTraitSilently.c`：先 `Enumerable.Any(+0x128, id == 参数)` ⇒ **命中就 `return`**；否则 `CardTrait__GetNewTrait` 造一条，同时塞进 `+0x128` 与 `+0x130`。⇒ **同一 trait 在 trait 列表里不会出现第二条**，也不会把值累加。
- `CardScript__RemoveTraitSilently.c`：`Enumerable.Find(+0x128, id)` → `List.Remove`（**一条**）→ 若 `+0x130` 含同一条也移除 → `BattleCardUI.RemoveStatusAnim(trait)`。
- 值的「叠加」在**另一本账**：`EntityScript__GetCurrentTraitValueWithModifiers.c:20-46` = `Σ(activeEffects(+0x108) 里 buffType==3 ∧ trait==T 的 traitValue)` + trait 列表那条的 `+0x1c`。⇒ 叠层 = 多条 `addTrait` 记录；而 **trait 5 的这种记录 = 0 行**（见 §一）。
- `CardScript__ResolveCardPlayed.c:42-46`：`StopReflected` → `RemoveTraitSilently(this,5)` → 紧接着 **`CleanStatusAnims(0)` + `UpdateTraitIcons(0)`**（这是**这个调用点自己**补的刷新，不是 `RemoveTraitSilently` 内建的）。

### 我们这一侧

- `UnitState.cs:835/893`：`_keywords` 是 `Dictionary<string,int>`（**合并表示**：卡面 + 效果授予 + 光环）；`AddKeyword`（`:1141` 起）对 ephemeral 没有任何去重闸（除毒×抗性），**再调一次就把值写成 2**。
- `UnitState.RemoveAll`（`:1282`）：`_keywords.Remove(keyword)` + `_remnantTraits.Remove` + 撤 `_grantedOps` / `RevertGrantsOf`；`silent:true` 时**不** `RecomputeActAndAttack`、**不** `TraitRev++`。
- ephemeral 唯一会「+1」的入口 = `EffectResolverPartial_B97W2.cs:386` 的 `case 25`。它的**生产调用链**只有 `EnchantmentExecutor_B115.cs:398 → ResolveAbilityBuffRows → ApplyAbilityBuff`（`choice_option_cards` 也走这条）；而 `enchantment_templates.json` / `choice_option_cards.json` 里 **buffType 25 = 0 行** ⇒ **`case 25` 今天没有任何生产调用点**（只有 `B97W2_Assert` 的 C1 直接调）。
- ephemeral 的其它载体是 `CardInstance.EphemeralMarked`（`BattleContext.cs:1550`，**布尔、幂等**）与卡面 `Keywords`（**0 张单位卡带**）。

⇒ **三层判据合起来**：① 数据面无叠加源；② 原版结构上禁叠；③ 我们的 `case 25` 不在生产路径上，且单位卡面不带 ephemeral。**「两条 ephemeral 同时挂在同一个 UnitState 上」这个状态，任何一条路径都到不了。**

## 三 该做什么

**`RuleCore.cs:16192` 不改**（`RemoveAll` ≡ 原版 `Find+Remove`，因为原版最多一条）。反证 = §二 那三条；另外补一条**表示层**的精确差异（今天不可观测、但要知道）：

> `RemoveAll` 会**一并**抹掉「`activeEffects` 里的 `buffType 3` 记录」给我们带来的值贡献；原版 `RemoveTraitSilently` **只摘 trait 列表那一条**，`activeEffects` 记录还在（`HasCurrentTrait` 仍会走 `+0x108` 那条支）。⇒ 差异只在「有 ephemeral 的 addTrait 记录」时成立，而**全池 0 行**。

唯一**可选**的小硬化（属实、低成本、与 `case 25` 自称「`AddEffect.c:947` 的等价物」不符）：

- 位置：`EffectResolverPartial_B97W2.cs:386`
- 改成：`if (target.Has("ephemeral")) { 出声 return false; }` 再 `AddKeyword`（= 原版 `AddTraitSilently` 的 `Any ⇒ return`）。
- 断言（加进已有 `B97W2_Assert.B97W2AddEffectDispatch()` 的 C1 块）：同一个 `u` **连调两次** `ApplyAbilityBuff(BuffType=25)` ⇒ 第二次 `ok == false`（出声），且 `u.KwValue("ephemeral") == 1`（⛔ 不是 2）。灭自证：把去重闸删掉 ⇒ 值变 2 ⇒ 红。
- 优先级：**P3（休眠硬化）**，不阻塞主线；`RuleCore.cs` 那一行一动不动。

## 四 `RemoveStatusAnim`（刷 UI）我们有没有对应物

- 有：`CardPresentation/Core/TraitFrames.cs` 的 **`TraitFrames.Remove`** = 原版 `RemoveStatusAnim`（两本字典 `PlayFrames/TriggerFrames` 各自 `Exit()`/`Destroy` + 移除；判据在文件头，逐句对过 `BattleCardUI__RemoveStatusAnim.c`）。
- 但**ephemeral 没有可摘的框**：原版 `CardTrait` SO（`Unity/数据/游戏数据/trait_collection.json`，pathID **476 / definedTrait 5**）是 `traitEffects: []`、`inPlayAnim: false`、`isInPlayIcon: false` ⇒ `RemoveStatusAnim(5)` **在原版就是空转**，`DisplayStatusAnim(5)` 也从没为它实例化过任何框。
- 所以：**不是「表现层缺」，是「原版也不播」**。手牌侧那颗临时卡角标我们另有 `CardView.ShowEphemeral`（`CardView.cs:3525`，每轮 `SyncHand` 按 `Ctx.IsEphemeral` 重刷），与状态框是两件事。
- `ResolveCardPlayed` 那一跳附带的 `UpdateTraitIcons(0)`：我们是「打出 ⇒ 新建单位视图」，图标按摘后状态生成，等价；⛔ 不要为此给 `RemoveAll` 加 `TraitRev++`（那会把 `silent` 的语义和原版 4 类「不刷图标」的口径一起弄错）。

## 五 出处索引

- `RuleEngine/Core/RuleCore.cs:16188-16197`（`ApplyPlayedCardTailEffects` ④）
- `RuleEngine/Core/UnitState.cs:835, 893, 1141, 1282`
- `RuleEngine/Core/EffectResolverPartial_B97W2.cs:160, 186-191, 380-392`
- `RuleEngine/Core/EnchantmentExecutor_B115.cs:398`
- `RuleEngine/Resources/enchantment_templates.json` / `choice_option_cards.json`（buffType 25/26 = 0）
- `CardPresentation/Core/TraitFrames.cs:55`（`RemoveStatusAnim` 等价物）
- `d:/2/tools/decomp_full/CardScript__AddTraitSilently.c`（`Any ⇒ return`）
- `d:/2/tools/decomp_full/CardScript__RemoveTraitSilently.c`（`Find + Remove` + `RemoveStatusAnim`）
- `d:/2/tools/decomp_full/CardScript__AddEffect.c:943-950`（`case 0x19`）
- `d:/2/tools/decomp_full/EntityScript__GetCurrentTraitValueWithModifiers.c:20-46`（叠值的真正来源）
- `d:/2/tools/decomp_full/CardScript__ResolveCardPlayed.c:36-46`
- `Unity/数据/游戏数据/trait_collection.json`（`Ephemeral` pathID 476，`traitEffects: []`）
