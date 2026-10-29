# B98_R6 · trigger 230/435 分派批（解 871）

> 只读代理 `B98_R6` · 2026-10-29 · 全部行号 **现读于当日**（7 个写手正在改 `RuleCore/EffectResolver/Aura/UnitState/TutorialScript/SimpleAI/BattleDriver/CardDisplayWindow` ⇒ 用之前必须现读）。
> 本代理**没改任何 `.cs` / 账 / 正本 / 工具**。

## 〇 结论：能不能现在做 / 谁先谁后 / 唯一落点在哪

**「trigger 230/435 分派」不是一个号，是一件事**：B88_R9 批次 6 的一句简写，实指 **「按 trigger 数值取该 ability 的正文并派发」那张表**。
`230`(Relentless) 与 `435`(OtherCardRequiem) 只是当时相邻的两条账行名 —— 两者机制完全不同（见 §一），**但它们共用同一个前提**：650 这种「按数值选 ability」的口没有实现。

**能做，但顺序不能倒。** 直接接 871 = 只把 435 广播发出去、而 430 的正文永远解析不出来 ⇒ **正是调度台禁的「半条路」**。

**唯一落点（三处，各有唯一主人，⛔ 别让三个写手各写一份）**：

| 层 | 落点 | 现状 |
|---|---|---|
| **数据** | `CardDef._triggerAbilityIndices`（`CardDef.cs:9231`；生成器 `工具/gen_cards_engine.py:4793` + `:5221/5966`） | ✅ **已落地**（v42/v43 · `B96_GEN` · 池内 1129/1129 张都有） |
| **桥（缺的就是这一格）** | `CardDef.AttachTriggerOpsByNumber`（`CardDefPartial_B96CA.cs:80`）→ 填 `_triggerOpsByNumber` | 🔴 **全仓 0 调用点** ⇒ `TriggerOpsByNumber` 恒走「卡面关键词」兜底 |
| **引擎** | `RuleCore.FireTriggerByNumber`（`RuleCore.cs:17514`）+ `CardDef.TriggerOpsByNumber(trigger)`（`CardDefPartial_B96CA.cs:102`） | ✅ 已建，**1 个调用点**（`:12884` 的 410） |

**482 不共用这张表**（它是 `ResolveDeadCard` ① 的 extrinsic 摘除，已落地 `SweepExtrinsicEffectsOnSourceDeath`）—— 它只是**次序约束**（① 必须在 ③ 之前），别把它拉进这批。

## 一 原版判据（逐句 + 出处）

### ① 430 `Requiem` — 发送方 = **观察者**（不是死者）

`CardScript__ResolveDeadCard.c:266-283` · ILSpy `CardScript.cs:15931-15964`（两份对照读）：
```
观察者 != 死者 且  GetAdjacentUnits(观察者).Contains(死者)      // :267
      ∧ 观察者.cardState(+0x228) != 5 (waitingToDie)            // :268
      ∧ 死者.cardType(+0x? ) != 10 (Hero)                        // :270
⇒ rawCardScript(观察者).OnTrigger(0x1ae = 430, bm, this=观察者, actingCard=凶手, targetCard=死者)   // :279
⇒ bm.BroadcastUnitRequiem(观察者)                                                                  // :283
```
- **`this` = 观察者**：`.c:279` 取的是 `*(param_1 + 0x28)`（`param_1` 的 rawCardData），`:267` 也是 `GetAdjacentUnits(bm, param_1)`。
- **发几次** = **每有一个「与死者相邻的观察者」就各发一次** ⇒ N 个相邻观察者 = **N 次 430 + N 次 435 广播**。
- **不受 `leftPlay` 约束**：翻面成残骸那一支（死者还在场）原版**照样发**。
- 参数解出法（旁证账 634）：`local_318[2]` 起 copy 32 qword，`local_308 = local_318 + 0x10 = battleAction.targetCard`（死者）、`uStack_210 = local_218 + 0x08 = actingCard`（凶手）。

**独立的第二条 430 通道**（⛔ 别与 ③ 合并，闸与语义都不同）：
`BattleManager.ResolveTriggerRequiem`（`dump.cs:32136` RVA `0x983FF0`；`ResolveTriggerRequiem.c`）由 `AddTriggerRequiem`（actionType `0x40`=64）排队，**唯一生产者** = `AbilityLogic__PlayAbility.c:2083` 的 `chosenAbility == 0x186(390)`。
闸 = `targetCard != null` ∧ **`IsInPlayOrDying(targetCard) == true`** ⇒ `OnTrigger(0x1ae)` + `BroadcastUnitRequiem(bm, targetCard, caster)`。

> 🔴 **`.c` 把这条闸渲染反了（机器码实证）**：`GameAssembly.dll` 偏移 `0x9829F0`（= VA `0x180983FF0`）：
> `call IsInPlayOrDying` → `test al,al` → **`jz +0x2CC`**，**fall-through 才是** copy 循环 → `mov edx, 1AEh`（`0x982B92`）→ OnTrigger。
> ⇒ **true 才发**。`out_il2csharp/BattleManager.cs:12594` 与 `.c` 都写成 `if (!IsInPlayOrDying())`（**错的**）；`out_cpp2il_dev_ilspy` 的 `flag2=!flag; if(!flag2)` 才是对的。
> ⚠️ `ResolveTriggerSacrifice.c:24` 是同一形状（同一嫌疑），本代理**只对 Requiem 做了机器码实证**，Sacrifice 未单独证。

### ② 435 `OtherCardRequiem` — 接收方

- 广播器 `BattleManagerSupport.BroadcastUnitRequiem(manager, unitRequiem, unitRequiemCaster)` —— **3 个形参**（ILSpy `BattleManagerSupport.cs:1085-1128`；`.c` 只显示 2 个 = Ghidra 丢了第 3 个）：
  1. 棋盘跳：`IsInPlay()` ∧ `!= unitRequiemCaster` → `TriggerOtherCardRequiem(unitRequiem)`
  2. 当前行动方手牌：**全量、不排除任何卡**
  3. 对方手牌：**全量、不排除任何卡**
- 接收 `CardScript.TriggerOtherCardRequiem(requiemUnit)`（`TriggerOtherCardRequiem.c:14-21`；ILSpy `CardScript.cs:7629-7637`）：
  `if (requiemUnit != this) OnTrigger(0x1b3 = 435, bm, this, actingCard = requiemUnit, targetCard = null)`
  ⚠️ ILSpy 把 actingCard/targetCard 打成 `default` —— `.c:20` 明写 `(raw, 0x1b3, bm, param_1, param_2, 0, 0)`。**照 ILSpy 写会丢 actingCard**（本仓已记的「一整排 default ⇒ 回 `.c`」那条）。
- **发几次** = 每个命中接收者各 1 次（棋盘在场 + 双方手牌逐卡，减去自己）。
- 死亡链那一路传 `caster = 0`（`ResolveDeadCard.c:283` 调 `BroadcastUnitRequiem(bm, this, 0, 0)`）⇒ **无广播级排除**，只有接收方的 `!= this` 自排除。

### ③ 230 `Relentless` — 与 430 **同形同闸、不同号**

`BattleManager__ResolveTriggerRelentless.c:29 / :217`：闸 = `targetCard != null ∧ IsInPlayOrDying(targetCard)`（同上极性）⇒ `OnTrigger(0xe6 = 230, …)`；唯一生产者 = `AddTriggerRelentless` ← `AbilityLogic__PlayAbility` 的 `chosenAbility == 400`。

⇒ **230 与 430 是同一张「按数值派发」表的两个样本**，这就是那句「230/435 分派」的由来。

## 二 我们现状（现读行号）

1. 🔴 **名字错**：账/施工单写的 `EffectResolver.ResolveTriggerRequiem` —— 实际是 **`RuleCore.ResolveTriggerRequiem`**（`RuleCore.cs:14298`）。`EffectResolver` 里只有 `BroadcastUnitRequiem`（`EffectResolver.cs:16023`）。
2. `RuleCore.ResolveTriggerRequiem:14298` 行为：闸 `u != null && IsUnitInPlayOrDying(u)` → `FireTriggerAlways(ctx,u,KeywordTable.Requiem,…)`（**按关键词文本**，`:14310`）→ `BroadcastUnitRequiem(ctx,owner,u)`（`:14312`）。**全仓 0 调用点**。
   - 那个闸对 ③ **近似但不精确**：原版是 `observer.cardState != 5`，我们的 `IsUnitInPlayOrDying` 还含 `pendingDeath` / `DyingThisAction`（更宽）。
3. `EffectResolver.BroadcastUnitRequiem:16023`：`BroadcastWhen(OtherCardRequiem, …, subject:unitRequiem, actor:unitRequiem)`，自排除在 `WhenEvents.SelfExcludesOnKeywordTrigger`。**缺第 3 形参 `unitRequiemCaster`**（死亡链那一路传 null ⇒ 今天净等价）。
4. **缺的三跳**：① 430 正文没走 `FireTriggerByNumber`；② `CleanupDeaths` **没有任何**按相邻观察者发 requiem 的调用；③ 数据桥 `AttachTriggerOpsByNumber` 0 调用点。

## 三 施工顺序（每步：文件:行 · 改什么 · 零行数 · 断言 · 指纹）

### Step 0（**前置，别人名下的 1288 残余**）：把「(ab,lg) → ops」的桥接落地
- 现状：数据侧 `triggerAbilityIndices` 已在（`gen_cards_engine.py:4793` / `:5221` / `:5966`，v42 `B96_GEN`）；DTO + 挂载也在（`CardDatabase.cs:4777 MapTriggerAbilityIndices` → `CardDef.AttachTriggerAbilityIndices`，`CardDef.cs:9240`）。**唯一缺的**是把 `TriggerAbilityIndexSpec[]` 变成 `Dictionary<int, List<EffectOp>>` 再调 `CardDef.AttachTriggerOpsByNumber`。
- 🔴 **唯一未定口径 = 「(ab,lg) → 哪些 op」**：我们的 op 是**文本 parse**（`CollectTriggerOps`，`CardDef.cs:1445`）按**关键词**分桶，**没有 per-ability 的 op 表**（`abilityDamage` 的 `Ability = r.ab` 只是伤害规格，不是 op 表）。⇒ 这一步**必须先由调度台定口径**，⛔ 别让写手猜。
- 零行数：`CardDatabase.cs:1749` 那一长行是 **A 行账落点**（下游 `:1799` 等），⛔ **不许加行**；桥接要么塞进已有的 `MapTriggerAbilityIndices` 同一行，要么挂类尾。
- 断言：夹具卡 `triggerAbilityIndices=[{ab,lg,trigger:430}]` ⇒ `TriggerOpsByNumber(430) != null`；反向（只给 120）⇒ `(430)` 仍 null。灭自证 = 关掉桥接两条一起回落文本兜底。
- **指纹：会变。** 现存三个消费者：`RuleCore.cs:12884`（410，池内 **30** 张）· `EffectResolver.cs:18499`（200，**0** 张）· `EffectResolver.cs:19502`（240，**33** 张）⇒ 410 / 240 两档会换源 ⇒ 必须逐档单独跑 `play check`。

### Step 1：871③ 的发送侧（`RuleCore.CleanupDeaths`）
- 落点：`RuleCore.cs:10122`（`if (leftPlay) { SweepExtrinsicEffectsOnSourceDeath…}`）**之后** —— 原版次序 = `TriggerOnMinionDeath`(:236) → `RemoveExtrinsicEffectsFrom`(:244) → `Slay` → `Requiem`(:266-283)。
- 做什么：取**死者的相邻观察者**（`BoardSpec.AdjacentSlots`，`BoardSpec.cs:61` —— 全仓唯一相邻判据），逐个调 `ResolveTriggerRequiem(观察者)`。
- 🔴🔴 **必须先在 `:9914` 之前快照邻居**：`BoardSlots.RemoveAt(ps, slot)` 在 `:9914` 就补位了，走到 `:10122` 时 `slot` 已被别人占 ⇒ 在那里算相邻会**静默算错**。这是本条的**头号坑**（同族：任何「按死者在场时的位置」的复刻都踩）。
- ⛔ **别塞进 `if (leftPlay)`**：原版 ③ 不受它约束（残骸那一支照样发）。
- 闸：观察者 `!IsDying`（原版 `cardState != 5`）+ 死者 `cardType != 10 (Hero)`。
- 断言：3 个相邻观察者 ⇒ 430 收 3 次 / 435 广播 3 次；观察者待死 ⇒ 0；死者 Hero ⇒ 0。**先写前提断言**（确实相邻、确实在场），⛔ 不写 `if (前提) Check`。
- 指纹：池内 430 = 0 张 ⇒ **预期不变**（弱证据，⛔ 不能当「改对了」）。

### Step 2：952 的接收侧补 `unitRequiemCaster`
- 落点：`EffectResolver.cs:16023` 加第 3 个**可选**形参 `caster = null`（缺省 null ⇒ 现有调用点逐字不变），棋盘跳按它排除。
- 断言：caster 非 null ⇒ 棋盘跳少收 1 次、手牌两跳照收；灭自证 = caster=null 时与今天逐字相同。
- 指纹：0 张 ⇒ 不变。

### Step 3：`ResolveTriggerRequiem` 里 430 的正文换口
- 落点：`RuleCore.cs:14310`（现 `FireTriggerAlways`）→ `FireTriggerByNumber(ctx, u, 430, …)`（Step 0 落地后才有意义）。
- 断言：有 430 ops ⇒ 结算；无 ⇒ 只发事件（与今天同）。

## 四 可达性（池内 1129 张 · `cards_engine.json` v43）

| 测什么 | 张数 |
|---|---|
| `abilityTriggers` 含 **230** / **430** / **435** | **0 / 0 / 0** |
| `abilityTriggers` 含 **210**（506①）/ **200**（1288） | **0 / 0** |
| `chosenAbility` 含 **390**（`AddTriggerRequiem` 唯一生产者）/ **400**（`AddTriggerRelentless`） | **0 / 0** |
| 卡面关键词 or trait `requiem` / `relentless` / `courtesy` | **0 / 0 / 0** |
| `triggerAbilityIndices` 列存在 | **1129 / 1129** |

- 池内 `abilityTriggers` **出现过的全部数值（56 个）里根本没有 230 / 430 / 435 / 210 / 200**：
  `[0,10,20,30,31,40,41,46,50,60,80,81,90,120,130,145,180,190,195,240,260,280,291,300,301,410,455,490,500,510,515,520,560,570,580,600,605,610,620,630,645,647,650,655,660,661,665,670,680,690,695,700,701,710,715,720,726]`
  ⚠️ 别把 `605`(`UseFaith`) / `610`(`OnCollectSpiritStone`) 当 Courtesy/Relentless —— 那是**另表**（`AbilityTriggerNumberOfName`，`CardDef.cs:1914-1949`）。
- 复算：`python -I -X utf8 -c "import json,io;d=json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json',encoding='utf-8'));cs=d['cards'];print([(n,sum(1 for c in cs if n in (c.get('abilityTriggers') or []))) for n in (200,210,230,430,435)])"`

## 五 顺带发现（⛔ 本代理没改任何东西）

1. 🔴 **新坑：`BattleManager__ResolveTrigger*.c` 的 `if (cVarX == '\0')` 是 Ghidra 反转**（§一 机器码实证）。凡「`IsInPlayOrDying` 之后紧跟 `== '\0'`」的 `.c` 段，**一律回 `.c`+机器码复核**，别按字面实现。建议进 `资料/已知的坑.md`。
2. ⚠️ **账/施工单里的方法名写错**：`EffectResolver.ResolveTriggerRequiem` → 真身 `RuleCore.ResolveTriggerRequiem`（`RuleCore.cs:14298`）。871 / 952 / B97_R10 / B97_战斗剩余_按文件施工单 里都这么写。
3. ⚠️ `BroadcastUnitRequiem` 原版 **3 形参**，我们 2 个。账 952 说的「棋盘排除」来自第 3 形参 `caster`，**不是**广播器排除 `requiemUnit` —— 两处排除机制别混（接收方排 `requiemUnit` 是 `TriggerOtherCardRequiem` 自己的 `!= this`）。
4. 🔴 **`CleanupDeaths` 的补位早于死亡触发段**（`RemoveAt` `:9914` vs 触发段 `:10019+`）—— 这条影响面**不止 requiem**：任何「按死者在场时的邻居 / 位置」的原版语义都在这里失真。（顺带：账 634 记的「原版末尾第四支 `actingCard` 再收一次」也吃同一口。）
5. ⚠️ `RuleCore.cs:17503-17506` 的注释「数据半今天还没落列」**已过期**（数据 v42 已落，缺的是桥接）—— 铁律 5，应由调度台就地订正（本代理不改源码）。
