# W5 · 特效 VfxMap（AN1 `R5` / `R8` + C6 `Q5`）执行报告

> 执行代理 **W5** · 2026-10-24 · 只动三处：`Core/VfxMap.cs` · `Editor/AnimFXCheck.cs` · `工具/gen_trait_effect_bindings.py`（+ 它自己产出的 `Resources/trait_effect_bindings.json`，只经生成器写）。
> ⛔ 没跑 Unity（简报红线）。**跑过的只有**：`TMPDIR=/tmp/wf_vfx bash d:/4/Unity/工具/typecheck.sh`（**两套程序集各 0 error**）· `python -I -X utf8 工具/gen_trait_effect_bindings.py`（+ `--check` 幂等）。
> ⚠️ 因此下文的「现读数字」来自**按同一套判据重写的 Python 复算**（数据 = 仓库里那几份 JSON），**不是** C# 跑出来的 —— 主对话在同步点跑 `AnimFXCheck.Run` / `BattleScene.Run` 才算数。

---

## 一 · 总览

| # | 件 | 结论 | 主落点（现读行号） |
|---|---|---|---|
| 1 | AN1 **`R5`** `trigger` 桶名字没放行 | ✅ **36 行 / 32 卡全部 = `Strike`(580) + `Slay`(120)**，两个都有真发射口 ⇒ 已放行 + 正/反两把锁 | `VfxMap.cs:2569`（`case "strike"/"slay"`）· `:3177`（`MustRouteTriggerNames`）· `:3214`（`MustRouteTriggerKeywords`）· `:2217`（过期的「一律不动」那句，铁律 5 订正） |
| 2 | AN1 **`R8`** trait 行双播 | ✅ **证实**（两条链各自的触发点见 §3·1），且**现读有 36 个 (卡, 关键词) 对真的双播** ⇒ 已按原版口径收敛到一条链（整桶那一侧让开），并加了**结构性互斥断言** | `VfxMap.cs:2683`（`IsTraitFrameDuplicate`）· `:2720`（`TriggerBeatRows(bucket, keyword)`）· `:2992`（`chainName`）· `AnimFXCheck.cs:1467`（`CheckR5R8`） |
| 3 | C6 **`Q5`** `conventions.overrides` 文档过期 | ✅ 改**生成器**那句 → 重生成（**没手改产物**），并补了 `army` 口径（拥有者阵营 vs 原版卡自己的 `cardArmy`） | `gen_trait_effect_bindings.py:223`（conventions 文本）· `:30`（新增「`army` 口径」一节）→ 产物 `trait_effect_bindings.json`（`rows` 逐字未变） |

改动量：`VfxMap.cs` **+195/−15** · `AnimFXCheck.cs` **+234/−12** · 生成器 **+23/−3** · JSON **+2/−2**（只有 `generatedAt` 与 `conventions.overrides` 两行）。
行尾：`VfxMap.cs` LF（未变）· `AnimFXCheck.cs` **CRLF 1939/1939**（未翻）· 生成器 LF · JSON LF（未变）。

---

## 二 · 件 1：AN1 `R5` —— `trigger` 桶 36 行 / 32 卡

### 2·1 判据：这 36 行到底是哪两个 trigger 名

做法（可复算）：把 `资料/普查产出_1024c/data/an1_*_emit.tsv` 里 `emit == TRIGGER_BUCKET(归口待定)`
那 **36 行**逐行取 `(card, cardanim, prefab)`，钉到 `RuleEngine/Resources/cards_engine.json` 的
`animTimes[]`（先按 `a`、再按 `n`）上读 `tn`。结果**只有两个值**：

| trigger | 行 / 卡 | 逐卡 |
|---|---|---|
| **`Strike`(580)** | **21 行 / 19 张** | `BL15`(2) · `BL31` · `BL71` · `DA9` · `GSC16` · `GSC27` · `GSC33`(2) · `GSC71` · `GOF62` · `TL3` · `TL16` · `TL17` · `TL30` · `ASH13` · `ASH16` · `ASH69` · `SOR16` · `SW22` · `TAU18` |
| **`Slay`(120)** | **15 行 / 13 张** | `AM71`(2) · `AM72` · `DA6` · `DA10` · `DA24` · `DA26`(2) · `DA36` · `EC11` · `EC12` · `EC34` · `SW27` · `SW34` · `UM90` |

`21+15 = 36` 行、`19+13 = 32` 张 —— **与 AN1 `R5` 的影响面逐个对上**。
（`BL31 Maulerfiend` 在 `cards_engine.json` 里按 `id` 查不到、按卡名又是 3 组重名之一 ⇒ 第一次复算时掉出；按「卡名 + 阵营 BlackLegion」钉住后确认它那条 `animTimes[].tn == "Strike"`。）

### 2·2 判据：两个名字**都真的有发射口**（这是放行的前提，⛔ 不是「原版有这个名字」）

| trigger | 发射口（现读逐句） | keyword 字面量 |
|---|---|---|
| `Slay`(120) | `RuleCore.cs:5603` `FireTriggerAt(ctx, killer, KeywordTable.Slay, kOwner, kSlot, requireActorInPlayOrDying: true)` —— `KillUnit` 那条链的**唯一发射口**（原版 `BattleManager__AddTriggerSlay.c:27` → `…__ResolveTriggerSlay.c:116-226`） | `KeywordTable.Slay` = **`"slay"`** |
| `Strike`(580) | `RuleCore.cs:6096` `FireTriggerAt(ctx, attacker, KeywordTable.Strike, p, atkSlot)` —— 攻击结算尾部、**排在 `UnitAttack(50)` 之后**（原版 `CardScript__ResolveUnitAttacked` 的 `OnTrigger(580)` 在 `:99-101`），近战/远程都发（与 `Mob` 的差别就在那道 `!ranged` 闸） | `KeywordTable.Strike` = **`"strike"`** |

两者都走 `FireTriggerAt` 的 `emitSignal` → `ctx.Emit(EvtKind.Trigger, … keyword: keyword …)`
（`RuleCore.cs:10948-10951` 那一句），且 `FireTriggerAt` 的「没正文就不发」闸照样生效。

### 2·3 改动

1. `VfxMap.IsRoutedTriggerName`：加 `case "strike":` / `case "slay":`（**靠方法头那句 `ToLowerInvariant()` 归一**，没有另起一套大小写口径 —— 与 `Codex`/`Mob` 同一处，⛔ 别在别处再写）。
2. `MustRouteTriggerNames` 加 `"Strike", "Slay"`（**正向锁**）；`MustRouteTriggerKeywords` 加 `KeywordTable.Strike, KeywordTable.Slay`（两表**逐个对应**，件级自检拿这一格当 `keyword:` 实参）。
3. **反向闸不动**：`NoEmitTriggerNames` 一个没加 —— 加进去就是「它没有发射口」的假话。
4. 方法头那段「`Strike` / `Slay` / `Regiment` 这些**走旧路的一律不动**」**就地订正**（铁律 5，`VfxMap.cs:2217`）：三个都已放行（`Regiment` 是 `J1`/`C5 D11`，`Strike`/`Slay` 是本批），并写明「引擎已会发」**不是**放行的充分条件（还要候选侧有行）。
5. `SelfCheckRoutedTriggers` 的 doc 补了这两个名字的**现读口径**（`Strike` 19 张/21 行 · `Slay` 13 张/15 行 · 0 多播 / 0 漏播）。

### 2·4 断言（`AnimFXCheck.CheckR5R8()`，`Editor/AnimFXCheck.cs:1467`，调用点 `:1739`）

| 断言 | 证明什么 | 🧨 改回去会不会红 |
|---|---|---|
| `R5①` `VfxMap.SelfCheckRoutedTriggerNames().Length == 0` | 正向锁 + 反向闸 + **大小写三态**（`Strike` / `strike` / `STRIKE` 都得命中） | 删 `case "strike"`/`case "slay"` ⇒ `MustRouteTriggerNames` 锁报「不在白名单」⇒ **红**；把 `switch(triggerName.ToLowerInvariant())` 改回 Ordinal ⇒ PascalCase / 大写两态报出来 ⇒ **红**（这条是 `C3 D6` 的复发闸） |
| `R5②` `VfxMap.SelfCheckRoutedTriggers().Length == 0` | **件级**：对每个「`animTimes[].tn == Strike/Slay`」的卡，按生产路径逐参解析后**不多播、不漏播**（32 张逐卡比名字多重集） | 删掉两个 `case` ⇒ 回第 ④ 档整桶 ⇒ **现读 4 张报多播**（`AM72` / `DA36` / `SOR16` / `EC12`，它们桶里还有**别的 trait** 的框）。⚠️ 其余 28 张因 `R8` 让开 + `W6` 名字闸，取件集合恰好不变 ⇒ 那一半只有 `R5①` 的正向锁挡得住（这句已写进断言文案，⛔ 别把它当成「删了必红」的保证） |
| `R5③` 点名 `BL15`：`keyword="strike"` ⇒ 取到 `MasterOfExecutions` + `BulletImpactBurst_Blood`、**不含** `StrikeEffect` | 引擎发的**小写**形态真的把卡级行挑出来（`C3 D6` 那类坑的复发闸） | 把 `D4` 的分件闸写成「一律回空」⇒ 两条都没了 ⇒ **红**。（⚠️ 它不是放行的判别式 —— 放行与否它都绿，文案里已如实写） |
| `R5④` 点名 `AM72`：`keyword="slay"` ⇒ `GuardSummon` 有、`StrikeEffect` **无** | **真正的判别式**：`StrikeEffect` 是 `strike` 那一拍的 trait 框，只由「放行后走第 ③ 档」才会被排除 | 删 `case "slay"` ⇒ 回第 ④ 档 ⇒ `StrikeEffect` 回来 ⇒ **红** |
| `R5④b` 点名 `DA36`：`keyword="slay"` ⇒ `CreateCard Gold` 有、`TalentEffect` **无** | 同上（换一张、换一个 trait） | 删 `case "slay"` ⇒ `TalentEffect` 回来 ⇒ **红** |

---

## 三 · 件 2：AN1 `R8` —— trait 行被两条链各播一次

### 3·1 先证实（两条链各自的触发点，现读）

| | 链① | 链② |
|---|---|---|
| 入口 | `BattleDriver.PlaySignal` 的 `EvtKind.Trigger` 支（`triggerKeyword = e.Keyword`；`GenericVfxFor(Trigger)` 恒 null ⇒ `perCardOnly = true`） | `BattleDriver.PlayTraitPulse(e)`（`BattleDriver.cs` 的 `case EvtKind.Trigger: PlayTraitPulse(e)`，现读 `:21330`） |
| 取数 | `VfxMap.ResolveAllTimed(Trigger, …)` → `TryResolveTriggerPicks` → 第 ②/④ 档时 **`trigger` 整桶**（`TriggerBeatRows` 只挡 0/1/2/5 档，**`Trigger(3)`/`TriggerOnPlay(6)` 原样保留**） | `TraitFrames.DisplayTrigger(keyword)`（`Core/TraitFrames.cs:869`）→ `_triggerBook`（= `trait_effect_bindings.json` 里 `TriggerType ∈ {3,6}` 的行） |
| 落地 | `FireEventAll` → `CardEffects.Fire(name, 目标点)` | `TraitFrames.Play(prefab)` → 卡的 `effectsAnchor` |

⇒ **同一件实例化两次**，锚点都不同。原版**只播一次**：`CardScript__ActivateTriggerTraitAnim.c`
**只**调 `BattleCardUI__DisplayTriggerAnim`（= 链②）；`AbilityLogic.PlayAnim` 播的是**卡自己的**
`cardAnimations[]`，不含 trait frame。

现读**真的双播**的 (卡, 关键词) 对 = **36 个**（按「生产会发」的关键词算）：
`codex` 9 · `strike` 9 · `artifice` 7 · `backlash` 6 · `mob` 3 · `cruelty` 1 · `ecstasy` 1
（全部落在**第 ② 档**：这些卡没有 `animTimes`、桶里却有该 trait 的框）。
第 ④ 档同理存在（现读每个 trait 名扫全池共 **150** 件），只是生产的已发关键词里只有 `backlash` 还走第 ④ 档。

### 3·2 收敛（本轮做了什么）

`VfxMap.IsTraitFrameDuplicate(p, keyword)`（`:2683`）：把候选的 `A`(`CardAnim`) / `N`(prefab) 与
`TraitFrames.TriggerBindingsFor(keyword)` 那几条真值行**逐字比**（**连 `overrides` 一起比** ——
`gen_card_vfx_by_card.py` 生成期就把拥有者阵营的 override 套上了，只比默认那格会漏掉 `shield`/`invulnerable`
那 10 条），命中 ⇒ **整桶那一侧不播这一件**（`TriggerBeatRows` 里让开并计数 `TraitFrameDuplicates`）。

- **插在哪个口**：`TriggerBeatRows(bucket, keyword)`（第 ②/④ 两档的唯一入口）· `TriggerBeatRowsMatching` 多一个
  `chainName` 形参；`TryResolveTriggerPicks` 里 `chainName = keyword 为空 ? null : CanonicalTriggerName(keyword)`。
- 🔴 **为什么 `chainName` 只取「事件自己的关键词」、不取保守推断出来的名字**：链② 只认事件里的 `keyword`；
  推断出来的名字若用来挡，链② 又拿不到它 ⇒ **两件都不播**。（`keyword` 为空的自检直调 / 旧事件 ⇒ 一律不挡。）
- **不动第 ③ 档**：放行后走第 ③ 档的拍，`CandidateHasTrigger` 本来就不会收 trait 行 —— 这一侧无需再挡。

### 3·3 断言（结构性互斥，`AnimFXCheck.CheckR5R8()` 的 `R8⑤`）

1. **逐 (卡, trait) 扫描**：把绑定表里**全部**带 `Trigger(3)/TriggerOnPlay(6)` 档的 trait 名当关键词，
   对全池每张卡跑一次生产路径；返回的件里**不许**出现「那条 trait 的那几件」。
   现读：扫到 **4459** 个「有取件」的对、**撞车 0**（不装这道闸时是 **153**）。
   ⚠️ **一处明确的假阳性排除**：`UM84` / `UM85` / `UM86` 的 `CodexTraitTrigger_warlord` → `CodexEffect`
   是**卡自己 `cardAnimations[]` 里就有**的同名动画（`animTimes` 标着 `Codex`）—— 原版本来就播两次
   （卡动画 + trait 框）⇒ 断言里按「该件在 `animTimes` 里标着同一个 trigger 名」显式放过（代码里有注释）。
2. **灭自证的另一半**：`TraitFrameDuplicates` 增量必须 **> 0**（现读这次扫描 = **150**；生产只发得到
   7 个关键词、对应 36 件）。🧨 闸被删 ⇒ 增量 0 ⇒ **红**；反过来把链② 那本清空（两件都不播）⇒ 同样 **红**
   （这一对断言就是「两条都开必须红、两条都关也必须红」）。
3. 期望值**全部从数据独立算**（`trait_effect_bindings.json` + `cards_engine.json` 的 `animTimes`），
   ⛔ 不从 `TraitFrames.TriggerBindingsFor` 读回（那是拿实现验实现）。

### 3·4 与既有断言的关系（**已逐条核过，一条都不会因此变红**）

| 既有断言 | 为什么不受影响 |
|---|---|
| `SelfCheckTriggerBeatSplit`（`VfxMap.cs`，`D4` 的全池自检） | 它用编出来的关键词 `__E3_not_a_trigger__` 算期望 ⇒ 不是任何 trait 的触发名 ⇒ **一件都不会被让开**。我把两处 `TriggerBeatRows(trig)` 显式改成 `TriggerBeatRows(trig, NotATriggerKeyword)`，与 `ResolveAllTimed` **逐参一致** |
| `BattleVfxAssertE3.D4` 的 `AM27`（第 ② 档）/ `GOF26`（`keyword="unstable"`）/ `AM15`（第 ② 档）/ `DA10` | 关键词是 `null` / `unstable`；`unstable` 在绑定表里**没有** 3/6 档 ⇒ `TriggerBindingsFor` 回空 ⇒ 不挡 |
| `BattleScene` 的 `T21·G` `Concealed Explosives` / `Dark Pact of Excess` / `Acolyte Heavy` | 全部 `keyword = null` ⇒ 不挡 |
| `BattleScene` 的 `UM37` / `UM84` / `SW18` / `AM25` / `AM32` / `AM37` / `TL18` / `EC25` | 关键词都是**已放行**的（`codex` / `WhileInPlay` / `regiment` / `TriggeredSynapse` / `Ecstasy`）⇒ 走第 ③ 档，本闸不参与 |
| `AnimFXCheck` ⑤ 的 `BL14` 正例 | ⚠️ **这条前提过期了，已就地改判**（铁律 5）：`Strike` 已放行 + `StrikeEffect` 是 trait 框 ⇒ 改为 `BL15`（卡级行）正例，`BL14` 反过来成了 `R8` 的判别式（让开 ⇒ 空） |

---

## 四 · 件 3：C6 `Q5` —— `conventions.overrides` 文档过期

- 判据（C6 §五 / §十·3）：运行期**已经按阵营取** —— `TraitFrames.PrefabFor(trait, trigger, army)`
  / `PrefabOf(row, army)`（`Core/TraitFrames.cs:533-560`，`DisplayStatus` / `DisplayTrigger` 两个口都在传），
  10 条覆盖（`shield` 4 · `invulnerable` 6）；而 `army` 取的是**拥有者阵营**
  （`CardView.Data.faction` ← `ToCardData(UnitState, faction)`），**原版取的是卡自己的**
  `rawCardData.cardArmy`（`ObtainableItem.cardArmy`，偏移 `+0x2C`）。
- 改动：**只改生成器**（`gen_trait_effect_bindings.py:223` 的 `conventions.overrides`）→ 重跑生成器写产物
  （⛔ 没手改 JSON）。顺手订正那句里引用的**不存在的键** `unresolvedArmyVariants`（本文件没有这个键，
  缺的那几条在 `unresolvedRows`）。另在生成器文件头新增 **`## army 口径`** 一节（`:30`），
  写明「拥有者阵营 vs 原版卡自己的 `cardArmy`；同阵营卡组逐字一致、跨阵营盟军可能不同；根治要 `BattleDriver` 传 `CardDef.Faction`」。
- 重生成结果：`rows=75`（逐字未变，`--check` = True）· `withPrefab=68` · `unresolved=7` ·
  `overrides=10` · `guidChecked=68 / guidMismatch=0` —— **只有 `generatedAt` 与那一段文案变了**。

---

## 五 · 没做到 / 判据不足（⛔ 都写清了，没拿猜测填）

1. **`R8` 没有「整桶一律不播 trait 行」**（更强的收敛）。原因不是「不想做」：`TriggerBeatRows` 把 3/6 档
   一律挡掉会推平 `BattleVfxAssertE3.cs` 的 `AM27`/`GOF26`/`AM15` 三条既有断言与 `BattleScene`
   的同款夹具，而那**两个文件不在本代理的白名单**。⇒ 本轮只做**精确去重**（只让开「本拍 `TraitFrames`
   真的会播的那一件」），并把这条残留写进 `IsTraitFrameDuplicate` 的 doc。
   **另开账（供主对话记 `§29·b`）**：`R8-b` ——「`trigger` 桶第 ④ 档仍会播**别的 trait** 的框」
   （现读 `GOF26` + `keyword="unstable"` ⇒ `ConcussiveEffect`、`AM72`/`DA36` 的 `StrikeEffect`/`TalentEffect`），
   要收口得先改那 3 条断言的期望值（属断言线 + `BattleVfxAssertE3.cs`）。
2. **`rally` / `landing` / `sacrifice` / `spentallmana` / `survivor` / `swarm` / `abouttoattack` 没放行**：
   它们**有发射口**（`an1_trigger_matrix.tsv` 现读），但 `cards_engine.animTimes` 里 **0 行**候选 ⇒
   放进去就是空放（`SelfCheckRoutedTriggers` 会报「全表一张卡都没有」），而且会改变第 ④ 档的取件范围、
   没有判据支撑。已写进 `IsRoutedTriggerName` 上面那段注释。
3. **没跑 Unity** ⇒ `AnimFXCheck.Run` / `BattleScene.Run` / `BattleVfxAssertE3` **一条都没执行**。
   本文所有数字来自**同一套判据的 Python 复算**；C# 侧只过了 `typecheck.sh`（两套程序集 0 error）。
4. **`Bl14` 那条老正例的替代品**：我换成 `BL15`（卡级行），没有另找一个「trait 行在整桶里该播」的正例 ——
   因为那一档正是 `R8` 判为缺陷的东西（保留它做正例会把缺陷钉成期望）。`DA10`（`Trait_GainQuest` 卡级行）
   仍然是「别切过头」的正例。

---

## 六 · 顺手发现（⛔ 一个都没改，只报）

1. 🔴 **`AN1 R5` 的「36 行 / 32 卡」精确成立**（`Strike` 21/19 + `Slay` 15/13）—— 顺带发现 `BL31 Maulerfiend`
   在 `cards_engine.json` 里**按 id 查不到**（卡池 id 与 emit TSV 的 `card` 列对不上），是 3 组重名之一
   （`BlackLegion` / `EmperorsChildren` 各有一张 Maulerfiend）⇒ 任何「按名字查卡」的工具在这张卡上都会歧义。
2. 🟡 **`trait_effect_bindings.json` 的 `conventions` 里还有一个不存在的键名**（`unresolvedArmyVariants`）——
   本批已顺手订正（在生成器里）；另注意 `stats` 里 `overrides` 是**条数**（10），不是「有几个 trait」。
3. 🟡 **`UM84` / `UM85` / `UM86` 的 `CodexTraitTrigger_warlord` → `CodexEffect` 与 `codex` trait 的框同 prefab**
   ⇒ 原版这两条链会各播一次（两个实例、同一件）。我们的第 ③ 档同样播两次（= 正确）。
   ⚠️ 将来若 `codex` 变成未归口，`R8` 会让开一份 ⇒ **少播一次**（这一条已写进 `IsTraitFrameDuplicate` 的 doc）。
4. 🟡 **`CardDef.cs` / `BattleDriver.cs` 的行号这两个小时一直在漂**（其他代理正在改）。
   我引用的 `BattleDriver.cs:20803` / `CardDef.cs:5736·5742` 一开始就已过期
   （现读 `:21330` / `:5889·5895`）⇒ 本轮把**我这批新增注释里的**行号引用改成「符号 + 现读行号」写法，
   ⛔ 别人那批旧引用没动（不越界）。
5. 🟡 **`BattleVfxAssertE3.cs` 的文案有两处已过期**（不改，只报）：`D4` 的两条 🧨 文案里写着
   「删掉第 ② 支那个 `TriggerBeatRows(trig)`」—— 这个签名本轮已变成 `TriggerBeatRows(trig, chainName)`；
   另外 `BattleScene.cs:36725` 附近引用 `TriggerBeatRowsMatching` 的说明仍准确（只是签名多了个参数）。
6. 🟢 **`AnimFXCheck` 的 `UnpinnedPicks` 计数器**会被我这轮的全池扫描加数（多播桶里钉不上的候选，告警上限 16 条、
   其余只计数）。已核：**只有** `BattleScene` / `BattleVfxAssertE3` 读这个计数，且两处**读前都 `ResetUnpinnedForTest()`**
   ⇒ 不污染别人。

---

## 七 · 怎么复核（主对话同步点用）

```bash
# 1) 幂等 / 产物与生成器一致（纯 python，秒级）
python -I -X utf8 d:/4/Unity/工具/gen_trait_effect_bindings.py --check      # 期望 rows 一致：True

# 2) 类型检查（两套程序集都应 0 error）
TMPDIR=/tmp/wf_vfx bash d:/4/Unity/工具/typecheck.sh

# 3) 断言（**要 Unity，只有主对话能跑**；⛔ 本代理没跑）
#    -AnimFXCheck.Run       ← 本节 R5/R8 的 6 条断言都在这里
#    -BattleScene.Run       ← 回归：D4 / T21·G / W6 那批（R8 不该让它变红）
#    -BattleVfxAssertE3 宿主（BattleScene.Run 内）← D4 / UM37·UM84 / 白名单回归闸
```

## 八 · 改动的文件（绝对路径）

- `d:/4/Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs`
- `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/AnimFXCheck.cs`
- `d:/4/Unity/工具/gen_trait_effect_bindings.py`
- `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/trait_effect_bindings.json`（**只经生成器写**）
