# B123 · 执行代理 E1（附魔 / 引擎主路 · `EffectResolver`）报告

> 批次 **B123** · 2026-11-03 · 白名单：`RuleEngine/Core/EffectResolver.cs` · `RuleEngine/Core/EffectResolverPartial_*.cs` · 新建 `RuleEngine/Core/EffectResolverPartial_B123E1.cs` · 新建 `RuleEngine/Editor/B123_E1_Assert.cs`
> **未跑 Unity / 未跑 `-executeMethod` / 未动 git / 未改账主表 / 未改任何正本**
> 已跑：`TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**
> `EffectResolver.cs`：改前 **21,881 行** / 改后 **21,881 行**（**零行数变化**，CRLF 21881 / 裸 CR 0，两侧都现读过）

---

## 〇 总表

| 简报条目 | 本批结果 | 落点 |
|---|---|---|
| ③-1 `B121-D4-03`（`ca 50` 主路无消费者） | **✅ 已落地**（代码 + 断言） | `EffectResolver.cs:21404`（同一物理行内追加）+ 断言 `B123_E1_Assert` A 组 / E 组 |
| ③-2 `B121-D1B-03`（`valueOptions 203` / `ta 230`） | **🟠 实现已交、开关未接** | 实现 → `EffectResolverPartial_B123E1.cs:148 / :194`；开关在 **E2 独占**的 `EnchantmentExecutor_B115.cs` ⇒ 补丁见 §3 |
| ③-3 `B121-D5-04` A 半（`DoStun` 抽 `ApplyStunTo`） | **✅ 抽取已落地**；附魔路那一行转调在 E2 独占文件里 ⇒ 补丁见 §3 | `EffectResolverPartial_B123E1.cs:52` + `EffectResolver.cs:4438`（58 行原地替换、行数守恒） |
| ③-4 `B121-D5R-03`（`abilitySummon.ids` 主路消费者） | **⛔ 零改动 + 反证**（施工点 `DoDeploy` 不成立） | 反证表见 §4；账 `状态 = C` |

🔴 **一处流程冲突（请调度台裁定）**：简报 ③-2 的施工点（`TryResolveCriteriaValue` 的 `case 203` · `ResolveEnchantmentRowTargets` 的 `case 230` · `TaGoesThroughCriteriaFilter` 加 230）与 ③-3 的消费端（`EnchantmentExecutor_B115.cs:1317` 的 `DoStun` → `ApplyStunTo`）**全部落在简报 ⑤ 明令 ⛔ 的 `EnchantmentExecutor_B115.cs`（E2 独占）**。
按 `CLAUDE.md` 13·3 的处置口径：**没有硬改**，改成「**实现放我这边的 partial 文件（可编译、可断言）+ 一行/三行可粘贴补丁**」交回（§3）。E2 落地后这 4 条即可全绿。

---

## 1 逐条：改了什么

### 1·1 `B121-D4-03` — 卡表主路 `ca 50 buffHand` 的消费端

**改**：`RuleEngine/Core/EffectResolver.cs:21404`（`FireTriggerRowsByChosenAbility` 的 `default:` **同一物理行**内追加，零行数变化）：

```csharp
default: if (r.Ca == 50) { fired += RunHandBuffRowOfEnchantment(ctx, owner, u, u.Card, r, "trigger:" + triggerNumber); continue; }
```

**判据**：`decomp_full/PlayerHand__AddHandEffect.c:88-139`
—— `ca 50 buffHand` = ① 先**无条件**把这条 `HandEffect` 收进 `activeEffects(+0x48)`；② 再遍历**当前手牌**逐张过 `targetCriteria(+0x20)`，命中即 `AddEffect`。`playersAffected` 决定打哪一方的手（10 Self / 20 Enemy / 0 Both）。

**落地为什么是「转调」而不是「另写一份」**：`RunHandBuffRowOfEnchantment`（`RuleCorePartial_B115W20.cs:62`）就是那两段，`AttachHandAbilityBuff` 逐档实现了 `pa` 三档 + `-1`/其它值「出声、不挂」。⛔ 没有再写第二份。

**「不双发」的前提已现读确认**（简报要求）：`FireTriggerRowsByChosenAbility` 的**唯一生产调用点**在 `EffectResolver.cs:21078`，形态是

```csharp
var ops = u.Card.TriggerOpsByNumber(triggerNumber); ... if (ops == null || ops.Count == 0) { int _caFired = FireTriggerRowsByChosenAbility(...); return ...; }
```

⇒ **正文 op 非空时这个函数根本不被调** ⇒ 与文本路天然互斥。这条已写进注释，并配了断言 **E1**（构造「同一 trigger 上 `ca 50` 行 + 正文 op」的夹具，断言主路跑完手牌一条 `HandEffect` 都不多）。

**观测面（现读 `cards_engine.json` v49）**：`ca 50` 行 **18 行**（`abilityBuffs` 里 `handBuff` 53 行）。

**⚠️ 顺带查出来的一条残差**（已进账，见 §7·1）：18 张里有 **6 张**在同一 trigger 上还挂着别的 ca 行；只要那个 trigger 的正文 op 非空，它们的 `ca 50` 行就进不到主路。**本批没跑实况，只记疑点。**

### 1·2 `B121-D5-04` A 半 — `DoStun` 的施加段抽成 `ApplyStunTo`

**改**：
- 新实现 → `RuleEngine/Core/EffectResolverPartial_B123E1.cs:52` `internal static void ApplyStunTo(BattleContext ctx, UnitState t, string by, EffectOp op)`
- `RuleEngine/Core/EffectResolver.cs:4438`：`DoStun` 的循环体（原 `:4438-4495`，**58 行**）**原地换成 1 行转调 + 57 行注释**，**总行数不变**（21,881 → 21,881；下面 297 条账落点一处不漂）。

**判据**：`decomp_full/CardScript__Stun.c:44-48 / :98 / :125`。五步逐条搬（`!IsAlive` → 闸① `AddEffectCardStateAllows` → 闸③ `AddEffectRemnantAllowsOp` → `unstunnable(0xfa)` 跳过 → 已晕跳过 → `AddKeyword(Stun,1)` → `EmitStatusApplied` → `StunnedAtStartOfTurn=false` → `BroadcastKeywordEvent(GetsStun)`）。

**🔴 简报点名的那个坑（「保住 `EffectOp` 依赖，否则残骸门会静默放宽」）已按最保守处理**：共用件的第 4 形参**就是 `EffectOp`**，闸③ 仍原样收 `op`（它只用 `op.Source` 记日志）。
附魔路（`RunStunRowOfEnchantment`）**自己已经造了一个最小 `EffectOp`**（`EnchantmentExecutor_B115.cs:1307-1316`：`Verb="stun"` / `Source=附魔卡名` / `SubjectTargetsAffected=30`）⇒ 转调时把它原样传进来，两路的残骸门**逐字同义**。⛔ 没有把闸③ 换成 `string source`。
断言 **B3** 就是「`op == null` 也照常施加、不抛」，**B5** 是「`!IsAlive` 不施加」。

**闸②/闸④ 留在 `DoStun`**（`:4434` 的 `AddEffectCasterGates`）：原版 `PlayAbility.c:642-657` 的 `ca 130` 逐目标 `AddStun` **没有** jam 闸，所以附魔路**不该**经过它 —— 这正是本次抽取的目的。

### 1·3 `B121-D1B-03` — `valueOptions 203` + `ta 230`（**实现已交、开关未接**）

| 子项 | 实现（本批已落） | 判据 |
|---|---|---|
| `valueOptions 203` | `EffectResolverPartial_B123E1.cs:148` `TryResolveCriteriaValue203(ctx, thisCard, min, max, out value, out gap)` | `AbilityLogic__GetValueFromCriteria.c:320-325`（`case 0xcb`）= **`minValue + thisCard.CurrentShuriken × maxValue`** |
| `ta 230` | `EffectResolverPartial_B123E1.cs:194` `ResolveTargetsTa230(ctx, out gap)` | `AbilityLogic__GetTargets.c:809-833` + `BattleManager__GetHighestHealthWarlord.c` + `BattleManager__GetHero.c` |

**🔴 判据原表 §5 标「未取到」的『平局回落 `GetHero(manager,1)` 的 1 是哪一侧』——本批已取到直接判据，不再需要「只出声、不硬猜」**：
- `BattleManager__GetHero.c` 全文两句：`param_2 == '\0' ⇒ +(200 = 0xC8)`，否则 `⇒ +0xC0`；
- `GetTargets.c:721 / :738`（`0x78 friendlyHero` / `0x82 enemyHero`）把 **`actingCard.isPlayer` 原样当实参**传进 `GetHero` ⇒ 实参就是**座位号**（0/1）。
- `GetHighestHealthWarlord.c`：A 侧 = `+0xC0`、B 侧 = `+0xC8`；`iVar4 < iVar3 ⇒ 返 A`，相等 ⇒ `return 0`（null）。
⇒ 合起来 = **「当前生命高者胜；平局 ⇒ 座位 1」**。（A 侧 = `+0xC0` = `GetHero(…,1)` 那一侧，两句话自洽。）

**⚠️ 一个仍未直接读到的**（如实标着）：`.c` 比的是督军 `+0x68` 的 `ObscuredInt`，判「当前生命」而**不是**「生命上限」是**由同址推出来的**（与 `GetValueFromCriteria` 的 `case 0x1e = targetCard 当前生命` 同址），不是直接读到字段名。我们这边取 `UnitState.Health`（当前生命）。

**开关为什么没接**：`case 203` 要加在 `EnchantmentExecutor_B115.cs:1044 TryResolveCriteriaValue` 的 `switch` 里；`case 230` 要加在 `:601 ResolveEnchantmentRowTargets` 的 `switch` 里；还要给 `:1089 TaGoesThroughCriteriaFilter` 的「不筛」组加 `230`。三处全在 E2 独占文件 ⇒ 见 §3。

---

## 2 断言覆盖（`RuleEngine/Editor/B123_E1_Assert.cs`）

| 组 | 断的是什么 | 灭自证（怎么才能红） |
|---|---|---|
| **A1~A4**（pa=10） | 池里找一张 `pa=10` 的 `ca 50` 卡 ⇒ 主路返回 ≥1；**自己**手牌挂上 `HandEffect`；**对手**手牌一张没动；空手返回 0 | 删掉 `default:` 里 `r.Ca==50` ⇒ 返回 0 ⇒ 红；把 `pa` 判据改成「两边都挂」⇒ A3 红 |
| **A5~A8**（pa=20） | 同上，但目标换成**对手**手牌 | 同上（反向） |
| **A\*0** | 前提：该 trigger 上**没有正文 op**（生产链路会进 ca 分支） | 前提断言独立成条，不成立就红（⛔ 不用 `if (前提) Check(...)` 静默跳过） |
| **B1~B5** | `ApplyStunTo` 真挂上 · `unstunnable` 挡得住 · `op == null` 不抛 · 重复施加不抛 · 已死不施加 | 删掉 `StunBlockedByTraits` 那一段 ⇒ B2 红；清空方法体 ⇒ B1 红 |
| **C3/C4/C5** | `1 + 3×2 = 7`；`0 + 3×5 = 15`（**反证不是 40/41/50 那族的 `min×value`**）；载体为空 ⇒ 不结算 + 出声 | 写成 `min==0 ? value : min×value` ⇒ 得 3 ⇒ 红 |
| **D1~D5** | 座位 0 高 ⇒ 座位 0；座位 1 高 ⇒ 座位 1；**平局 ⇒ 座位 1**；恰好一个目标 | 平局写成「座位 0」或「返回空」⇒ D4 红（这一条只能由判据决定） |
| **E1** | 同一 trigger 上正文 op 非空 ⇒ 主路不进 ca 分支，手牌一条都不多 | 把 `:21078` 的闸去掉 ⇒ 红 |

**接线**（调度台做）：`RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加
`Guard("B123E1_Assert", B123_E1_Assert.Run); _pass += B123_E1_Assert.Pass; _fail += B123_E1_Assert.Fail;`（追加文本必须落在该行第一个 `//` **之前**）。

**两处「已知会不一样」的读数，先说明**：
- `A1~A8` 依赖 `CardDatabase.Load()` 与真实卡的 `abilityBuffs` 旁表（`RecordAbilityBuffs` 是 private，手写卡挂不上）⇒ 用的是**真卡**，不造假。
- `A` 组的**手牌备料是按 criteria 现算的**（`FillHand`）：池内 `ca 50` 的 criteria 有要求具体 `spellType` 的（`GSC45 Underground Network` = **230**），而手写卡的 `SpellType` 恒 `-1`（未知）⇒ `MatchesRawCard` 判不了、一律不命中（**静默 0**）。所以缺哪一档就从**真实卡池**补一张真卡（含 `spellType` 匹配的战术卡）。
- `A` 组的**空手对照显式清空手牌**：`RuleCore.NewBattle` 会发起手牌（`Vars.startingHand` = 3）⇒ 不清空就不叫「空手」。
- `D5` 里对 `TaGoesThroughCriteriaFilter(230)` 只打 `Debug.LogWarning`、**不写成会红的断言** —— 那是「E2 的接线没做」，不是「本批实现错」。补丁落地后它会自动变 `Debug.Log("✓ …")`。

---

## 3 可粘贴补丁（**`EnchantmentExecutor_B115.cs` 是 E2 独占 ⇒ 我没动，请 E2 或调度台落地**）

> 三处**只加行**，且都加在 `switch` 的 `default:` 之前；⚠️ 落盘时请确认该文件**当前没有别的写手**。
> 🔴 **现读（2026-11-03 本批收工前）`EnchantmentExecutor_B115.cs` 与 `RuleCorePartial_B115W20.cs` 在 `git status` 里都还是 `M`** ⇒ **E2 仍在飞**，这两份补丁请**交给 E2**（或等它交回后再落），⛔ 别在它写的时候并发改。

### 3·1 `valueOptions 203`（`EnchantmentExecutor_B115.cs:1044 TryResolveCriteriaValue`）

在 `case 90:` 那一段之后、`default:` 之前插入（约 4 行）：

```csharp
                case 203: // `GetValueFromCriteria.c:320-325`（case 0xcb）：min + thisCard.CurrentShuriken × max
                    return TryResolveCriteriaValue203(ctx, thisCard, min, max, out value, out gap);
```

（`TryResolveCriteriaValue203` 是**同一个 `RuleCore` 类**的 `internal static` 成员 ⇒ 直接调得到，⛔ 不用改可见性。）

### 3·2 `ta 230`（`EnchantmentExecutor_B115.cs:601 ResolveEnchantmentRowTargets`）

在 `default:` 之前插入：

```csharp
                case 230: // `GetTargets.c:809-833`（case 0xe6）：两位督军里当前生命更高者；平局 ⇒ 座位 1
                    return ResolveTargetsTa230(ctx, out gap);
```
⚠️ 那个方法的 `out` 变量名按它现读的形参表写（它现在没有 `out gap` 形参 —— 若没有，就写成
`case 230: { string _g230; var _r230 = ResolveTargetsTa230(ctx, out _g230); if (_r230 == null) NoteEnchantRowTargetGap(card, row, caName, ta); return _r230; }`）。

### 3·3 `TaGoesThroughCriteriaFilter`（`EnchantmentExecutor_B115.cs:1089`）

```csharp
                case 20: case 30: case 40: case 80: case 120: case 130: case 140: case 150: case 220: case 230:
```

### 3·4 附魔路 `ca 130 stun` 绕开 jam 闸（`EnchantmentExecutor_B115.cs:1317`）

把那一行 `DoStun(ctx, owner, by, op, t, unresolved);` **原地**换成（**零行数变化**）：

```csharp
                ApplyStunTo(ctx, t, by, op);        // 原版 AddStun 没有 jam 闸（PlayAbility.c:642-657）⇒ 走共用件
```
（它上面已经造好了 `op`、下面也已经自己判了 `!t.IsAlive`；`DoStun` 的那两段「解析目标 / 代词映射 / 闸②④」这一支本来就用不上。）

---

## 4 `B121-D5R-03`：**零改动 + 反证**（施工点 `DoDeploy` 不成立）

### 4·1 数据现读（`cards_engine.json` v49）

| 项 | 数 |
|---|---|
| `abilitySummon` 总行数 | **1,621** |
| `ids` 非空 | **290 行 / 214 张卡** |
| 其中**部署族**（ca 70 / 75 / 77 / 3 / 320） | **73 行** |
| 其余（非部署族） | **217 行** |

**217 行按 `ca` 分布**：`100`(createCardInHand / Talent) **133** · `40` **36** · `30` **24** · `20` **8** · `10` **5** · `270` **4** · `50` **2** · `60` **2** · `0`/`3`/`130`/`90` 各 1。

### 4·2 三条反证

1. **口径不对**：`unitsToSummon` 是**逐 ability 行的载荷**，原版由**各个 ca 执行器分别读**（`GetCardToSummon` 的调用方在 `addUnit(ca 70)` / `createCardInHand(ca 100)` / 选牌(ca 270) … 各自那里）。把它当成「统一 Deploy 列表」接到 `DoDeploy` **前置**，对 217 行是**空转**（那些行根本没有 deploy op 走到 `DoDeploy`），对部署族则是**语义替换**。
   例：`ASH79 Orian Laratharjos` 的 `ids=["Flickerjump"]` 出现在 `ca 100`（Talent）行上；`AM56 Vengeance for Cadia` 的 `ids=["Shock Trooper Sgt.","…","…"]` 出现在 `ca 40`/`ca 30` 行上（卡面写的是「Give +2 [Ranged]」）。

2. **正文已覆盖**：73 行部署族里，**72 行**的卡面同时含 `deploy/summon/put in play/create a copy of` 字样**且**全部 `ids` 卡名（逐行比过）；剩 1 行 `EC50 Rapid Evisceration`（`Deal 2 damage. If target dies, put in play one "Infractor".`）也是正文路覆盖的写法。
   ⇒ 接上去不是「补缺口」，是**拿结构化候选替换正文**。

3. **`ids` 不编码次数** ⇒ 替换会**少量部署**（这是最硬的一条）：
   - `Skyborne Deployment`：「Deploy **two** Storm Guardian」→ `ids` 只有 **1** 个（`all=1`）
   - `AM55 Mechanised Infantry`：「Deploy **three** Shock Trooper」→ `ids` **1** 个
   - `GSC33 Goliath Truck`：「Deploy **two** Neophyte Hybrid」→ `ids` **1** 个
   - `SAU76 Inescapable Retribution`：「Deploy **two** Tomb Blade」→ `ids` **1** 个
   - `BL45 Spawndom`：「Deploy one Chaos Spawn **for each enemy unit**」→ `ids` **1** 个
   - 同类合计 **18 行**（`ids.Count == 1` 而卡面写 two/three）
   ⇒ 次数只在**正文**里（two / three / for each X）。用 `ids` 当候选表、正文让路 ⇒ 一律只部署 1 个。

### 4·3 结论与真残余

- **本批零代码改动**（`DoDeploy` / `DoDeployOnce` 一行没动）。
- 真残余（新口径，供调度台重划范围）：**`ids` 应当按 `ca` 交给各自的执行器**（`ca 70/75/77` 部署 · `ca 100` 造进手牌 · `ca 270` 选牌 …），而**次数由正文/`summonAllTargets` 决定**。这是一件**比原条目大得多**的活，且与 §7·1 那条闸的语义改动耦合。
- 附带一条**已核**（不是缺陷）：`MapSummonIds` 拿 `CardDef.Id` 与 `ids` 比对是**能对上的**（382 个 id 里 **378** 命中卡池）—— 32 位 GUID 与短 id 混存是生成器的既有形态，不是 bug。

---

## 5 没做 / 判不了的（如实）

| 项 | 状态 | 原因 |
|---|---|---|
| `case 203` / `case 230` / `TaGoesThroughCriteriaFilter` 加 230 | **未落地** | 三处全在 `EnchantmentExecutor_B115.cs`（简报 ⑤ 明令 ⛔，E2 独占）⇒ §3 补丁 |
| 附魔路 `ca 130` 转调 `ApplyStunTo` | **未落地** | 同上（`:1317`） |
| `B121-D5R-03` 的结构化接线 | **本批不做**（零改动） | §4 三条反证；要做也得先重划范围 |
| 6 张「`ca 50` 与别的 ca 同 trigger」的卡是否真被跳过 | **未跑实况** | 需要跑引擎（`ruleprobe` / 断言实跑）；本批只记疑点，不写结论 |
| `ta 230` 比的是「当前生命」而非「上限」 | **同址推出** | `.c` 读督军 `+0x68`，与 `case 0x1e`（目标当前生命）同址；不是直接读到字段名 |

---

## 6 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh` | **运行时 0 错 / 编辑器 0 错**（两次：加 partial 文件后 / 加断言文件后） |
| `EffectResolver.cs` 行数 | 21,881 → 21,881（**零漂移**）· CRLF 21,881 / 裸 CR **0** |
| 账落点是否漂移 | `EffectResolver.cs` 现读有 **368 处** `文件:行号` 引用（其中 > 4437 的 **297 处**、> 21,390 的 **1 处**）⇒ 本次两处改动都做成**零行数变化**，一处不漂 |
| 账补丁 | `账/_parts/B123_E1.tsv`：11 列 × 7 行（含表头），落点全是**声明行** |
| 未跑 | Unity / `-executeMethod` / `ruleprobe` / 账主表合并 |

---

## 7 顺手发现（⛔ 只报不改）

1. 🔴 **`ca 50` 的 6 张卡可能整行进不到主路**（新账，已进 `账/_parts/B123_E1.tsv` 最后一行）：
   18 张 `ca 50` 里有 6 张在**同一个 trigger** 上还挂着别的 ca 行 —— `GSC45`{100,50} · `SAU55`{30,3,50} · `GOF103`{10,50} · `TAU51`{50,30,100} · `Brood Progenitor`{100,50} · `TL47`{50,30,0}。
   只要那个 trigger 的正文 op 非空，`:21078` 的闸就让 `FireTriggerRowsByChosenAbility` 整趟不进 ⇒ 它们的 `ca 50` 行**不生效**。
   ⚠️ **本批没跑实况验证**（要 `ruleprobe` 或断言实跑）⇒ **只记疑点**，⛔ 别把 D4-03 的核销当成「`ca 50` 全通了」。
2. **`abilitySummon.ids` 有 4 个 id 查不到卡**（全是同一张 `AM76 Armoured Lightning` 的 4 行 → `66486b3f61682ab42ba2ce0fa921002f`），而池内**有同名卡** `AM68 Tempestus Scion`（卡面写「Deploy three Tempestus Scion」）。
   ⇒ 疑似生成器的 id 映射漏了一条（或那 4 行指向**另一份同名 SO**）。**未证实**，属数据侧（`工具/gen_cards_engine.py`），不在我的白名单里。
3. **`EnchantmentExecutor_B115.cs:316-320` 的注释过期**（写「11 组 buffType 不含 1/2/3」，而 `EffectResolverPartial_B97W2.cs` 现读已含 1/3）—— 判据原表 §12·1 已报，**我没动**（E2 独占）。
4. **`RuleCorePartial_B115W20.cs:30-40` 的「仍未覆盖 ①」过期**（「后进手牌」那半已接：`RuleCore.cs:11158 / :11092`）—— 同上，**我没动**（E2 独占），但 §1·1 的转调**依赖**这条已过期的描述，下一个写手别被它带偏。
5. **`B121-D4-01` 的口径缺口对本批同样成立**：`ruleprobe` 的 `LoadPool` 没挂 `abilityBuffs` ⇒ **`play check` 指纹不变不能当「本批这几列验过」的证据**。本批的验收靠断言（跑宿主），不靠 `play check`。
6. **`DoStun` 循环体的抽取用了「注释占位保行数」这一手法**（1 行代码 + 57 行注释 = 原 58 行）。这是**为了不搬 297 条账落点**的取舍：注释逐字保留、开头写清「实现已搬到 `ApplyStunTo`」。⚠️ 若以后有人把那段注释当「代码还在本地」读，会被误导 —— 我在第 2~14 行专门写了三条「⛔ 别当代码还在这里」。
