# J1 · VfxMap 分桶 + 白名单（`C5` D11 / D12 / D13 / D14）

> 执行代理 **J1** · 2026-10-24 · **有写权限**（未跑 Unity、未动 git、未改正本 / 任务总表）。
> 判据来源：`资料/普查产出_1023e/C5_动画特效审查_索引.md` §四 D11–D14 · `A21_VfxMap白名单.md` §二/§四
> · `A27_VfxMap第三批.md` §六（三处同批的做法）· 全量反编译 `d:/2/tools/decomp_full/` · 解包 `d:/2/新解包资源/`。
> 候选规模一律**回 `cards_engine.json` / 运行期 `card_vfx_by_card.json` 现读**（⛔ 没有一条是从报告里抄的数）。

## 〇 · 一句话

四件**全部落地**：`Regiment`(301) 进白名单（14 行 / 10 张）· `TriggeredSynapse`(630) 补映射 + 进白名单（1 行 / 1 张）·
`death` 桶里「死于能力」的 6 行不再当阵亡爆散体（5 张卡从**播错件**回到默认件）·
`ability` 桶里的 `OnPlay(2)` 天赋框整族挪到新桶 `trait_onplay`（137 张卡 / 142 行；**用主动技能不再顺带播登场框**）。
**没跑 Unity**（红线），但 14 条断言全部按「改坏就红」写好，且期望值都用 Python 在**运行期表**上预演过（见 §六）。

---

## 一 · D11 `Regiment`(301)：白名单一条

| 项 | 现读 |
|---|---|
| 候选 | `cards_engine.json` 里 `animTimes[].tn == "Regiment"`：**14 行 / 10 张**，全 AstraMilitarum（`AM6/9/14/25/26/29/31/32/37/69`；`AM6`/`AM37`/`AM69` 各 2 行） |
| 发射口 | `RuleCore.cs:6030` `FireTriggerAt(ctx, attacker, KeywordTable.Regiment, p, atkSlot)`（远程攻击结算尾部，与 `Mob` `:6008` 成对）；keyword = `KeywordTable.Regiment` = **`"regiment"`**（`CardDef.cs:5197`） |
| 白名单 | `VfxMap.IsRoutedTriggerName` 原来**没有** `case "regiment"` |
| 修 | `case "regiment": return true;` + 进 `MustRouteTriggerNames` / `MustRouteTriggerKeywords`（`KeywordTable.Regiment`） |

**件级预演（现读运行期表，按 `SelfCheckRoutedTriggers` 的同一套合并逻辑）**：10 张卡全部**不多不少**——

`AM31`→`Card_Buff_Hand` · `AM9`→`Lasrifle_basic` · `AM6`→`Sniper Target`+`BulletImpact_1shotSniper_trail` ·
`AM69`→`Basic_army_Guard`+`Buff_Purple` · `AM25`→`Basic_army_Guard_quick`+`Buff_Purple` · `AM14`→`Buff_Purple` ·
`AM29`→`BulletImpact_autocannon_Hydra` · `AM32`→`BulletImpact_tank_big` ·
`AM37`→`Basic_army_Guard_quick`+`Buff_Purple` · `AM26`→`Basic_army_Guard_quick`（0 行无件、0 行歧义）。

---

## 二 · D12 `TriggeredSynapse`(630)：映射 + 白名单（三处同批）

| 环节 | 现读 / 改动 |
|---|---|
| 候选 | `TL18 Zoanthrope` **1 行**：`Psychic_Lightning_Tyranid`（`CardAnim = Atk_SmiteLightning_Tyranid`） |
| 事件源 | `EffectResolver.cs:2270` `BroadcastKeywordEvent(ctx, WhenEventKind.Triggers(KeywordTable.Synapse), anchor)` |
| 断点（改前） | `TriggerNameOfWhenKind` 的 `TriggersPrefix` 分支只有 `Ferocity/Pray/Duty/Swarm` ⇒ 回 `null` ⇒ `EmitListenerTrigger` 直接 return |
| 改①（引擎） | `EffectResolver.cs`：常量 `TN_TriggeredSynapse = "TriggeredSynapse"` + `case KeywordTable.Synapse: return TN_TriggeredSynapse;` |
| 改②（白名单） | `VfxMap.IsRoutedTriggerName` 加 `case "triggeredsynapse":` + 进两张正向锁（`"TriggeredSynapse"` 两格**都写 PascalCase** —— 发射体是 `EmitCardTrigger`，`BattleEvent.Keyword` 写的是原版枚举名） |

**安全边界（现读全池）**：`abilityTriggers` 含 630 的只有 `TL18` / `TL19` / `TL70`；其中**只有 `TL18` 有动画行**。
另两张会走 `HasTriggerSource` 的「有 ability、没动画行」那一支（照发事件 + 出声，表现层无件）—— 与既有 A/E/F 族同口径。

**副作用（已核，⛔ 不是回归）**：`TL70 Norn Emissary` 的 630 那条 ability 在 v14 表里 `abilityContext = 10 (Hand)`。
映射补上后 `AbilityContextOfAbilityV14` 从 `-1（未知，回退旧口径）` 变成 `10`。但它的降费走
`BroadcastCostWhen → EmitListenerTrigger`（`EffectResolver.cs:8596` 那一带），那条路**不查 abilityContext**
（降费在 `CostMods` 里独立结算）⇒ **玩法不受影响**；`ListenerLane` 也仍回默认 `1 Instant`（全池无 `abilityTriggerPriority` 的 630 行）。

---

## 三 · D13 `death` 桶混进「死于能力」的动画（5 张播错件）

### 3·1 原版怎么挑（先查再改）

`CardScript._UnitDeath_d__446__MoveNext.c` 的阵亡粒子是**二选一**、整个函数只有两处 `Instantiate`：
`:233` 逐卡 `customDeathParticles` · `:240` 判空 ⇒ `:262` 默认 `cardDestroyFX`（= `Card 3D Death Explosion`）。
`ThisUnitDeath(80)` / `UnitDeathByAbility(180)` 那两族是 **`TriggerAbilities(80/180)` 那一拍的能力动画**，
`UnitDeath` 全程没碰它们 ⇒ **不是**阵亡爆散体。

### 3·2 我们这边的形状（现读）

运行期 `death` 桶 **72 行 = 66 行 `a` 空（`customDeathParticles`）+ 6 行 `a` 非空（`cardAbilities[]`）**，没有第三种形状。
判据 = **`CardPick.A`（`CardAnim` 名）空不空**：`customDeathParticles` 是 `AssetReferenceGameObject`
（生成器 `resolve_so_ref` 只填 `prefab`），而 `cardAbilities[..].cardAnimations[]` 必带 `CardAnim`。

| 卡 | `death` 桶 | 改前播（`picks[0]`） | 改后 |
|---|---|---|---|
| `AM34 Valkyrie` | `GuardSummon`（180） | **错件** | 默认件 `Card 3D Death Explosion` |
| `BL62 Dark Disciple` | `Buff_MarkOfTzeentch`（80） | **错件** | 默认件 |
| `BL66 Ghallaron's Disciple` | `Buff_Chaos_Star`（80） | **错件** | 默认件 |
| `GOF83 Grot on Bombsquig` | `BeastSnagga_Summon`（80） | **错件** | 默认件 |
| `GOF3 Bomb Squig` | `Squig_Explosion`（80） | **错件** | 默认件 |
| `SAU72 Lokhust Lord` | `Necrons death explosion` + `CreateCard Necron` | 恰好对 | **不变**（仍取 `Necrons death explosion`） |

### 3·3 改法（为什么没动数据侧）

按原版「彻底分桶」应让生成器把这两族另立一桶（`C5` §四 D13 修法 ①），但本批白名单**不允许改数据产物**
⇒ 在**唯一消费口**把「不是爆散体」的那一族挡掉：

- `VfxMap` 新增 `ResolveDeathParticle(faction, cardIdOrName)`：只回**第一条 `A` 为空的候选**（= `customDeathParticles`）；
  没有 ⇒ `null`（调用方用默认件，正是原版那个二选一）。
- `VfxMap` 新增 `DeathBucketAbilityRows(...)`（只给断言用）：把那几行**列出来**，证明我们**没删数据**。
- `CardFeel.PickDeathFx` 的 ③ 从 `ResolvePerCard(VfxMap.Death, …)` 改为 `ResolveDeathParticle(…)`。
- ⚠️ `VfxMap.ResolvePerCard(Death, …)` **保持原语义**（`death` 桶第一条）—— 它是「原始查询」，留给需要看整桶的地方
  和自检对照；⛔ 别拿它当阵亡件。

---

## 四 · D14 `ability` 桶混进关键词 `OnPlay(2)` 天赋框

### 4·1 原版行为（先查再改 —— 结论：`OnPlay(2)` **不在技能那一拍**）

- `CardScript__ActivateTraitsOnSummonOrEnchantment.c`：在**召唤 / 附魔那一刻**逐条 trait 调
  `BattleCardUI__DisplayStatusAnim`；后者只处理 `EffectTriggerType == Start(0)`（播放）与 `== OnPlay(2)`
  （挂上持久状态对象）。
- `BattleCardUI__DisplayTriggerAnim.c:71`（触发那一拍）**只认 `Trigger(3)` / `TriggerOnPlay(6)`**。
⇒ `OnPlay(2)` 的播放口是**登场**，我们这一侧 = `Core/TraitFrames.cs`（`_playBook` ← `Resources/trait_effect_bindings.json`）。

### 4·2 改法（生成器分桶，落到第三桶 `trait_onplay`）

`工具/gen_card_vfx_by_card.py`：

| 位置 | 改了什么 |
|---|---|
| `EV_*` 常量区 | 新增 `EV_TRAIT_ONPLAY = "trait_onplay"`（+ 判据与「⛔ 它不是事件、没有 `EvtKind`」的整段注释） |
| `TRAIT_SUFFIX_EVENT["traitplay"]` | `"ability"` → `EV_TRAIT_ONPLAY` |
| `TRAIT_TRIGGER_EVENT[2]` | `EV_ABILITY` → `EV_TRAIT_ONPLAY`（**必须同批改**：不改 ⇒ 升权威时 `want_ev != ev` ⇒ OnPlay 行**不升权威** ⇒ 再被 `strip_unbacked_inference` 当无依据删掉） |
| `_conventions.events` | 补 `"trait_onplay"` + 一行说明 |

`VfxMap`：新增常量 `public const string TraitOnPlay = "trait_onplay";`（**⛔ 不进 `Events`** —— 它不是引擎事件）。

**重生成后现读**：`ability` 条目 **440 → 364** · `trait_onplay` **137 张卡 / 142 行**；
`byCard` 总条目 **4582 → 4582（一条没丢）**；`notPlayed` **0 → 0**。
逐步核对：`cardtrait-so` 的行现在只在两个桶 —— `trigger` **491**（Start/Trigger/TriggerOnPlay）+ `trait_onplay` **142**（OnPlay）。

**`TraitFrames` 覆盖核对**（证明「换桶 ≠ 丢画面」）：被挪走的 6 个 `CardAnim`
（`FlyingTraitPlay` 77 · `StealthTraitPlay` 31 · `CamouflageTraitPlay` 21 · `ShieldTraitPlay` 7 ·
`ShieldTraitTAUPlay` 5 · `ShieldTraitECPlay` 1）在 `trait_effect_bindings.json` 里**全部有绑定**
（后两个是 `shield` 那条的 `overrides` 阵营变体），6 个 prefab 也全都在绑定表里。

**曝露面（按 `C5` 同一判据现算）= 21 张卡**：
`AM16 AM22 AM34 AM59 AM68` · `DA13 DA14 DA18 DA27 DA29 DA71` · `SOR29 SOR66 SOR69 SOR71` · `SW8 SW23` ·
`UM93 UM94 UM97 UM99`。其余 116 张只有天赋行、永远不发 `EvtKind.Ability`。

---

## 五 · 改动清单（只碰白名单）

| 文件 | 改动 |
|---|---|
| `CardPresentation/Core/VfxMap.cs` | `case "regiment"` + `case "triggeredsynapse"`（各带整段判据注释）· 两张正向锁各加 2 项 · `ResolveDeathParticle` / `DeathBucketAbilityRows` · `TraitOnPlay` 常量 · `SelfCheckRoutedTriggers` 口径注释 |
| `CardPresentation/Core/CardFeel.cs` | `PickDeathFx` 的 ③ 改用 `ResolveDeathParticle`（+ doc 就地改：也**不许**问 `ResolvePerCard`） |
| `RuleEngine/Core/EffectResolver.cs` | **只有 D12 的那两处**：常量 `TN_TriggeredSynapse` + `case KeywordTable.Synapse` |
| `CardPresentation/Editor/BattleScene.cs` | 新断言块（D11 / D12 / D13 / D14 + 白名单两条闸）；**两条旧断言跟着数据改桶**（见 §六·5） |
| `工具/gen_card_vfx_by_card.py` | D14 分桶（4 处，见 §四·2） |
| 生成产物（同一次运行） | `数据/游戏数据/card_vfx_by_card.json` · `CardPresentation/Resources/card_vfx_by_card.json` |

**没有碰**：`RuleEngine/**`（`EffectResolver.cs` 那两处之外）· `Shell/**` · `Net/**` · `Hand/**` · `Effects/**` ·
`WarpforgeVFX/**` · `Editor/` 下除 `BattleScene.cs` 外的一切 · `工具/` 下除生成器外的一切 · 正本 / 任务总表 / git。

---

## 六 · 断言（`Editor/BattleScene.cs`，全部「改坏就红」）

新增一块（`T21·B/G` 那一段之后），位置最近 `driver` / `VfxMap` / 卡池都可用：

| # | 断言 | 🧨 怎么才会红 |
|---|---|---|
| 1 | `AM25` 的团那一拍 = `Basic_army_Guard_quick` + `Buff_Purple`、**没有** `ArmourEffect` | 删 `case "regiment"` ⇒ `target` 恒 null ⇒ 回 `trigger` 粗桶 ⇒ 拿到 `ArmourEffect` |
| 2 | `AM32` = `BulletImpact_tank_big`（长度 1） | 同上 |
| 3 | `AM37` = 它自己那 2 条、无 `ArmourEffect` | 同上 |
| 4 | `TL18` 的 `TriggeredSynapse` 那一拍 = `Psychic_Lightning_Tyranid` | 删 `case "triggeredsynapse"` ⇒ 0 条（它没有 `trigger` 桶） |
| 5 | **引擎口**：`EmitListenerTrigger(…, Triggers(synapse))` 真发出 `EvtKind.Trigger` 且 `Keyword == "TriggeredSynapse"` | 删 `EffectResolver` 的 `case KeywordTable.Synapse` ⇒ `tn == null` ⇒ 0 条信号 |
| 6 | **反向**：`Triggers("no_such_keyword_j1")` **不发**任何事件（新增 0 条） | 把兜底改成 `return "Triggered" + kw` ⇒ 红 |
| 7 | `AM34 / BL62 / BL66 / GOF83 / GOF3` 的 `PickDeathFx` = 默认件 `Card 3D Death Explosion`、`PerCard == null` | 改回 `ResolvePerCard(Death, …)` ⇒ 各自拿到那条能力动画 |
| 8 | **正例·别切过头**：`SAU72` 仍取 `Necrons death explosion` | 过滤写成「一律默认件」⇒ 红 |
| 9 | **灭自证**：`AM34` 的 `death` 桶里 `GuardSummon` **还在**（`ResolvePerCard` 拿得到）· `ResolveDeathParticle` 回 null · `DeathBucketAbilityRows` 长度 1 | 从数据里删这条 / 过滤写成「取最后一条」⇒ 红 |
| 10 | `AM59` 的 `ability` 桶 = 只有 `Basic_army_Guard Digital`、无 `ShieldTraitEffect_Idle` / `CamouflageEffect` | 生成器把 OnPlay 塞回 `ability` ⇒ 红 |
| 11 | `AM59` 的 `trait_onplay` = 那 2 条（**没丢**，只是换桶） | 生成器干脆丢弃这一族 ⇒ 红 |
| 12 | `SelfCheckRoutedTriggerNames()` 长度 0（14 个正向名字三态都命中、反向闸 8 个都不在） | 改回大小写敏感 switch / 删任一正向 case / 把反向闸里的名字加进去 |
| 13 | `SelfCheckRoutedTriggers()` 长度 0（**全池件级**，自动覆盖 14 个名字，含 `Regiment` 10 张/14 行、`TriggeredSynapse` 1 张/1 行） | `TryResolveTriggerPicks` 的 ③ 改回「只查 trigger 桶」⇒ 每张卡都报「漏播」 |

> ⚠️ 第 5 条走**反射**取 `RuleCore.EmitListenerTrigger`（`internal`；本文件在编辑器程序集，直接调编不过）——
> 本文件既有做法就是反射（`StartPracticeMatch` / `OnSearchFinished` 那几处）。⛔ 没为了自检把生产 API 改成 public。
> ⚠️ `RuleCore.NewBattle` 建的是**一次性 ctx**，不碰主对局夹具。

### 6·5 两条旧断言跟着数据改桶（不改就会假红）

| 位置（现读） | 原来 | 现在 |
|---|---|---|
| `BattleScene` 的「权威全播」样例 `BlackLegion / Warp Talon` | `ResolveAll(VfxMap.Ability, …)` 期望 2 条（`FlyingEffect` + `StealthEffect`） | 改查 `VfxMap.TraitOnPlay`（**断言意图不变**：同档两条都播、按序） |
| 「新账-75②」`TAU17 Crisis Bodyguard` / `EC20 Turyan Ghauze` 的 Shield 变体 | `ResolveAllTimed(VfxMap.Ability, …)` | 改查 `VfxMap.TraitOnPlay`（变体改写那件事**仍然验得到**） |

### 6·6 期望值预演（Python，同一份运行期表）

- 件级白名单：14 个 `MustRouteTriggerNames` 逐个复算 ⇒ **0 违规**（`Codex` 22 张/30 行里 2 行歧义按既有口径跳过；`WhileInPlay` 1 行歧义跳过）。
- 多播/未钉计数会随分桶变化：`多播桶 500 → 472`（仍 > 300）· `钉不上 AnimTimes 的候选 237 → 169`（仍在 20–400）
  ⇒ `新账-75③` 那条区间断言**不会**因此变红。

---

## 七 · 自检状态

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_j1 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（最后一次）；中途两次红都落在**别人的文件**（`CardView.cs` 的 `ClearOutline`、`RuleEngineTest_EntryPump.cs` 的 `ShowCardsWindowOf`、`CollectionScene.cs` 的 `Check<T>`）—— 那是并行写手的半成品，重跑即 0（铁律 13·3 的已知形状） |
| `bash 工具/ruleprobe.sh check` | `=== 哨兵合计：失败 0 条 ===` · `解析差异 0 行` · rc=0 |
| `play all dump`（A/B 对拍，验「本批零行为变化」） | 改前/关掉 D12 那一条 `case` 重跑 ⇒ 两份 dump **逐字节相同** ⇒ **本批不改任何结算** |
| `bash 工具/ruleprobe.sh play check` | **rc=1，但红点不是本批**：基线 `out_play_baseline.txt` 相对**当前工作树**（含其他代理未提交的引擎改动）已过期 —— 主档 7 张、电池 463 张有差异（`BL64` playRc 0→18、`AM43` 抽牌序列变化…）。A/B 对拍已证明本批贡献 = 0 ⇒ **不要**为本批去重生成基线，应由引擎线核完那 7/463 后再决定 |
| Unity 批处理 | ⛔ 未跑（本批红线） |
| 行尾 | 五个手改文件逐个二进制计数：`VfxMap`/`CardFeel`/生成器 = 纯 LF，`BattleScene`/`EffectResolver` = 纯 CRLF ⇒ **没有一个被翻** |
| `git diff --numstat` | `VfxMap 709/67` · `BattleScene 1200/47` · `EffectResolver 540/62` · `CardFeel 17/4` · `gen_card_vfx_by_card.py 86/18`（前四个含其他代理的既有改动）· `Resources/card_vfx_by_card.json 1/1`（单行 JSON）· `数据/…/card_vfx_by_card.json 2523/2373`（**其中 2466/2342 = 桶键改名导致的行块移动**，语义差异只有 137 张卡的桶名 + 16 行 stats） |

---

## 八 · 没查清 / 没做的（如实记，⛔ 没有猜着写）

1. **没跑 Unity** ⇒ 上面 13 条断言**没有被真正执行过**，期望值是我在**运行期表**上用同一套合并/歧义逻辑离线预演的。
   要坐实：`BattleScene.Run` 里搜 `★ C5 D11` / `★ C5 D12` / `★ C5 D13` / `★ C5 D14` / `★ J1（白名单`。
2. **D11 的「有没有正文可触发」那一道闸没实跑**（`FireTriggerAt` 开头：`ops == null && spec == null && skipDraw == null && 附魔空 ⇒ return false`）。
   静态判据与 `A27` 同一口径（`regiment` 在 `CardDef.RoutableTriggers` / `BodyKeywords` 里），但解析器没跑过。
   建议真机放一次 AM 的远程攻击，日志里看有没有「触发 REGIMENT」那一行。
3. **D13 的另一半没做**（`C5` 修法 ②）：那 6 行「死于能力」的动画**今天仍然没有任何播放拍** ——
   `80(backlash)` 的候选在 `death` 桶、`TryResolveTriggerPicks` 只查 `trigger`+`ability`，且 `backlash` 不在白名单；
   `180(UnitDeathByAbility)` **全仓没有发射口**。要收口需要：生成器把它们另立桶 **+** `backlash` 进白名单 **+** 给 180 新造发射口（`RuleCore`，本批黑名单）。
4. **`ResolveAllTimed(Death, …)` 仍会返回那 6 行**（只有 `CardFeel` 那条生产口被过滤）。今天没有别的生产调用点（`EvtKind.Death` 的通用件是 null），
   但将来若有人拿它当阵亡件就会重蹈覆辙 —— 已在 `ResolveDeathParticle` 的 doc 里写明「判据只有这一处」。
5. **D14 的「登场真的播了变体」只做了数据侧核对**（`trait_effect_bindings.json` 6/6 有绑定 + 变体在 `overrides` 里），
   `TraitFrames` 的真实播放没跑 Unity 验；建议真机放一张 `TAU17` / `AM59` 看登场框 + 用一次技能**不该**再出框。
6. **`A` 空 = `customDeathParticles` 这条判据是数据形状推出来的**（不是字段名）：全池现读只有这一种形状（66 空 / 6 非空）。
   若生成器将来给 SO 字段行补 `cardanim`（`customDeathParticles` 类型是 GameObject，结构上不会），这条判据会静默变成「全丢」——
   §六 第 8 条正例（`SAU72`）就是给它上的闸。

---

## 九 · 顺手发现（按铁律 13·4⑧ 报上来，⛔ 我没有越界改）

1. 🔴 **`play check` 的基线已经过期**：`工具/ruleprobe/基线/out_play_baseline.txt` 的指纹 `5d81171a…` vs 现算 `af14b59d…`，
   主档 **7 张**、事件电池 **463 张**有差异（`BL64 playRc 0→18` / `AM43` 抽牌序列 / `EC58` 生成单位…）。
   它们**不是本批**（A/B 对拍已证）。**⛔ 别为了让 rc=0 去重生成基线** —— 那会把其他代理的真回归一起抹绿。
2. 🟠 **`BattleScene` 里原来根本没有 `SelfCheckRoutedTriggers()` 的接线**（`A27` §B 写「下一批贴进来」，一直没贴）。
   本批补上了（名字级 + 件级各一条）⇒ **A21/A27/A30/G2 放行的那 12 个名字从今天起第一次有全池件级断言**。
3. ℹ️ **`ability` 桶变空的卡有 76 张**（`trait_onplay` 是它唯一内容）。副作用 = `BattleDriver.PlaySignal` 的
   「没有专用技能动画 ⇒ 播默认技能音」那一支（`BattleDriver.cs:17213-17218`，`ResolvePerCard(Ability)`）对它们**现在会响**了。
   按原版语义这是**修对了**（原版 `_ResolveActiveAbilityAnim` 就是没有专用演出才响），但它是可听出来的变化 ⇒ 真机顺手听一下。
4. ℹ️ `数据/游戏数据/card_vfx_by_card.json` 的 `git diff` 看起来很大（2466/2342），**绝大部分是 `byCard` 里桶键改名造成的行块移动**，
   语义差异只有 137 张卡的桶名 + `stats` 几行。复核方法：按 JSON 字典比（我用的就是这一手），⛔ 别看 diff 行数。
5. ℹ️ `VfxMap` 里 `IsRoutedTriggerName` 的 doc 有一段旧文「`Strike` / `Slay` / `Regiment` 这些走旧路的一律不动」——
   `Regiment` 已经放行了，但那段是历史叙述（下面紧跟 A21 的就地订正），我**没有删**（保持更正痕迹，与 A21 的做法一致）。
6. ℹ️ `TL19 Broodlord` / `TL70 Norn Emissary` 从本批起会**每次突触都发一条 `EvtKind.Trigger`**（`HasTriggerSource` 的 ② 支：有 ability、没动画行）。
   表现层无件、只在日志里多一行「有 ability 没动画行」。这是 A/E/F 族既定的如实记账口径，⛔ 不是新缺陷。
