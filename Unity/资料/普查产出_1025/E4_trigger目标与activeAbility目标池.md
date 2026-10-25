# E4 · trigger 事件的目标 · `ActiveAbility.targetCriteria` 目标闸 · 易伤两条 `Hit`（执行报告）

> **执行代理 `E4`** · 2026-10-25 · 第七十五会话
> 白名单使用：`MyGame/Assets/RuleEngine/**`（`Core/` + `Data/` + `Editor/RuleEngineTest*`）· `工具/gen_cards_engine.py`
> ⛔ 没碰：`CardPresentation/Battle/**` · `CardPresentation/Core/**` · `Shell/**` · `Editor/BattleScene.cs` ·
> `工具/ruleprobe/**`（**只运行，一个字节没改**）· 三份正本 · 任何 `任务总表.md`。`git` 一个字没动。
> 行尾：见 §六（全部「原样未翻」）。

---

## 〇 · 一句话结论

| 件 | 结果 |
|---|---|
| **E4-1** `EvtKind.Trigger` 带目标 | ✅ **做完**（机制 + 四组真卡断言）。`TL37 Spore Mine` 的 `TurnStartPlayer` 事件从 `p-1s-1` 变成 **`p1s6(Mucolid Spore)`**（= 那条 `deal 3 damage to a random enemy` 真的打到的敌人）。 |
| **E4-2** `ActiveAbility.targetCriteria` | ⚠️ **数据 + 唯一入口 + 最小消费点都落了**（v28 新列 `activeAbilityPickCriteria` · 107 行/107 张 · `RuleCore.CanUseAbility` 的 36 格闸，三态 + 出声）；🔴 **但今天一张真卡都走不到**（如实：全池 **0 张**卡有解析成功的 `Ability:`，`CanStartAbility` 先回 `ErrNoAbility`）—— 闸已就位，**等「结构化 `ActiveAbility` 执行」那一批**。 |
| **E4-3** 易伤两条 `Hit` | ✅ **做完**（引擎只发事件那一层 + 新位 `BattleEvent.IsVulnerableSecondHit`）。四条闸（无易伤 / 致死 / 残骸 / 盾挡下）+ 堡垒被打穿档全部按原版拆数。 |
| 验收 | `typecheck` **0/0** · `ruleprobe check` **rc=0**（解析差异 0 行 · 哨兵失败 0 条；⑤ **已重钉后连跑两次均通过**）· `play check` **rc=0**（主档 1127 + 电池 586 逐字节零差异，指纹 `87209677…` 不变）· 离线探针（`_tmp_view/e4probe`，临时）**E4-1/E4-2/E4-3 共 45 条全过**。**Unity `RuleEngineTest.Run` 未跑**（本代理不许跑）⇒ 新写的 `RuleEngineTest_EngineE4.cs` 是它的第一次运行。 |

---

## 一 · E4-1：`EvtKind.Trigger` 带目标

### 1 · 判据（第一权威 = 全量反编译，逐句）

| 步 | 原文 | 说明 |
|---|---|---|
| ① 目标从哪来 | `AbilityLogic__PlayAbility.c:257-264` | `uVar12 = *(abilityLogic + 0x28)`（= `AbilityLogic.targetCriteria`）→ `AbilityLogic__GetTargets(manager, 它, abilityData, checkMaxTargets:1)` → **存进 `abilityLogic + 0x78`（`possibleTargets`）** → `AbilityLogic__PlayAnim(logic, manager, abilityData, possibleTargets)` |
| ② 动画怎么用目标 | `AbilityLogic__PlayAnim.c:171-248` | `param_4`（`possibleTargets`）**非空时逐目标**：`AbilityLogic__GetPos(manager, animData, 目标, 位置选项)` 算三个落点 → `BattleManager__AddCardAnim(...)`。**空 ⇒ `CustomDebug.LogError` + 整条 `return`**（`:171-176`） |
| ③ 所以 | 原版**必须**有目标列表才播得出卡本体位移 | 我们的事件一条只带 0/1 个目标（`BattleEvent.Target*` 是标量；表现层 `BattleDriver.ExpandAbilityBeats` 也按 0/1 处理）⇒ **取第一个可用的** |

### 2 · 机制（为什么是「事后填同一个对象」）

我们这边「广播（发 `EvtKind.Trigger`）」和「结算（`ResolveOps`）」是**两拍**
（`EmitAtTurnTriggers` 整趟发完 → 再 `EnqueueAbility` 入队），**发的那一刻目标还没选出来**。
原版是 `PlayAbility` **自己**先算 `GetTargets` 再 `PlayAnim`（同一拍）。

做法（`BattleContext.TriggerTargetSink` 的 doc 有全文）：
1. 发射体把**它刚发的那个 `BattleEvent` 对象**放进 `ctx.TriggerTargetSink`（arm）；
2. `EffectResolver.ResolveTargets` 的**唯一收口**（新包的一层，13 个 `return` 只判一次）在**第一次真的落到目标**时
   **当场写进那个事件**（`TargetPlayer/TargetSlot/TargetCardId`）；
3. `BattleContext.Signals` 存的是**同一个对象引用**、表现层要等整段引擎调用返回才 `DrainSignals`
   ⇒ **事后填的字段一定能被表现层看到**（不重排、不多发一条事件）。

三条判据（写在 `NoteTriggerTargetSink` 的 doc 里）：
- **只填第一个**（原版按 `possibleTargets` 逐目标各播一份，我们的事件只能带 1 个）；
- **跳过「就是施放者自己」的目标** —— 原版落点 `boardPos = GetBoardPosOfCard(targetCard)`，自指时
  `boardPos == start` ⇒ `normalize(0)`、**不位移**（与「没有目标」同一结果）。实据：`TL37` 的两条 op 是
  `[0] destroy this unit`（**自指**）与 `[1] deal 3 damage to a random enemy`，**先解析出来的是自指那条**；
  不跳过就永远填成自己（= 等于没修）；
- **必须当场落格号**（`FindSlot`）—— 目标常常被这条效果**打死**，等结算完再查就查不到了。

### 3 · 改动清单

| 文件 | 改动 |
|---|---|
| `Core/BattleContext.cs` | 新 `TriggerTargetSink`（`BattleEvent`）+ `TriggerTargetSinkDone`（bool），带完整判据 doc |
| `Core/EffectResolver.cs` | `ResolveTargets` 拆成**收口**（public，记 sink）+ `ResolveTargetsCore`（原体，13 个 `return` 不动）；`NoteTriggerTargetSink`；`EmitTurnTrigger` / `EmitTurnTriggerNamed` / `EmitTurnTriggerRaw` 返回值 `void`/`bool` → **`BattleEvent`**（`null` = 没发）；`EmitAtTurnTriggerPass` 把事件存进 `AtTurnUnitJob.TriggerEvent`（结构体按下标写回）；`AtTurnUnitJobDeferred` arm → `ResolveOps` → 还原（嵌套安全） |
| `Core/BattleEvent.cs` | `Target*` 那段 doc 补 E4-1 的判据（谁填、填哪个） |

### 4 · 离线读数（探针真跑，`_tmp_view/e4probe`）

| 卡 | 触发 | 事件目标（改后） | 期望 |
|---|---|---|---|
| **`TL37 Spore Mine`** | `TurnStartPlayer` | **`p1s6 (Mucolid Spore)`**（改前 `p-1s-1`） | ✅ 敌方、**不是自己** |
| `TAU13 DS8 Support Turret` | `TurnStart`（all enemies，多目标） | `p1s4 (敌督军)` | ✅ 取第一个 |
| `TL8 Genestealer` | `TurnStartPlayer`（create in hand，**没有棋盘目标**） | `-1` | ✅ 保持 `-1` |
| `GOF80 Grot Orderly` | `TurnStartPlayer`（return to your hand，**自指**） | `-1` | ✅ 跳过自指 |

---

## 二 · E4-2：`ActiveAbility.targetCriteria`（`+0x38`）

### 1 · 判据

| 问题 | 结论 | 出处 |
|---|---|---|
| **① 原版在哪一步消费** | `RawCardScript.IsValidActiveAbilityTarget(thisCard, 候选)`：先过 `BattleManager__IsValidActiveAbilityTarget` 那几条前置闸（候选 `IsInPlay` · 教程许可 · **`thisCard.isPlayer == 当前回合方`** · 异侧且 `stealth(0x46)` ⇒ 假 · 异侧且 `isCurrentlyWarded` ⇒ 假 · `summonRift(0x2da=730)` ⇒ 假），再进 `RawCardScript__IsValidActiveAbilityTarget.c:103-107` 的 `CheckIfMeetsCriteria(thisCard, 候选, 候选, *(activeAbility+0x38), 0)` | 调用者 = `BattleManager__IsValidActiveAbilityTarget.c` → `GetAvailableActions` / `ShowPotentialTargets`（高亮）/ `StartChoosingAbilityTargetByClick`（点选）/ `IsThereValidAbilityTarget` / `_ResolvePlayActiveAbility`（出手前复核）/ `Update` |
| **② 我们缺哪一段** | **数据列 + `CardDef` 入口 + 消费点，三件都缺**（现读 `CardDef.cs:2470` 就写着「`activeAbility.targetCriteria` 不在这里 …… 另账」） | 现读真值 `cards_raw.json` 有这一格（1351 张） |
| **③ 影响多少张卡** | 池内 **107 张 / 107 行**（`ta=10` 73 · `ta=20 self` 34；`ta=0` 零信息**不落** 2147 行）· 其中 **`enemyUnits == 1` 的 48 张**才是「我们的候选（恒为敌方）是原版候选集的子集」那一档 | `python -I -X utf8` 直算 + 生成器读数（§二·2） |
| 🔴 **可达性（如实）** | **今天 0 张真卡走到这道闸**：`CanUseAbility` 要求 `u.Ability != null`（正文里有一条 `Ability:`），而**全池 0 张**卡有解析成功的 `Ability:` ⇒ `CanStartAbility` 先回 `ErrNoAbility(14)`。**闸已就位、路还没通**（要等「结构化 `ActiveAbility` 执行」那一批） | 探针实测 + 卡池直扫 |

**与 `activeAbilityTargets`（v25）不是同一格**：

| 列 | 原版字段 | 层 | 键 | 回答什么 |
|---|---|---|---|---|
| `activeAbilityTargets`（v25） | `activeAbilityLogic[j].targetCriteria`（`AbilityLogic +0x28`） | logic 级 | `ab`, `lg` | 「这条 logic 从哪儿取目标」（结算建池） |
| **`activeAbilityPickCriteria`（**v28 新**）** | **`ActiveAbility.targetCriteria`（`+0x38`）** | **对象级** | **只有 `ab`（**没有 `lg`**）** | **「这个格位能不能被点」** |

⛔ 名字**不能**叫 `activeAbilityTargetCriteria` —— 那个名字**已经被 v25 的 logic 级列占着**
（`CardDef` ctor 的同名参数 = `activeAbilityTargets`）。所以本列叫 **`activeAbilityPickCriteria`**。

### 2 · 数据侧（卡表 v27 → **v28**）

```
形状：activeAbilityPickCriteria: [{"ab": -1|-2, "criteria": {36 格 + playerTurn=-1}}]
· 每张卡都落（没有行 = []；真值里没有这张卡才不落列）
· ta == 0 (any) 不落（原版 CheckIfMeetsCriteria 直接 return 1 = 零信息）
```

```
改前 cards_engine.json  md5 = 5a17f6af49ead1829b067bd9e9082118   3,119,176 B   (v27)
改后 cards_engine.json  md5 = 0397005df1c7de826f56e651c4ca7b01   3,235,327 B   (v28)  +116,151 B
连跑两次 --write：md5 逐字节相同  ✅
python -I -X utf8 工具/gen_cards_engine.py --check  → 一致 ✅
行尾：CRLF 0 / LF 0（紧凑单行，原样）
生成器读数：目标判据⑤ activeAbilityPickCriteria 落值 1127/1127 张 · 有行 107 张 · 107 行
            按 targetsAffected：10:73 · 20:34
v25 自检：criteria 键集 2084 行 × 37 键全部一致 ✅（1894 + 本笔 107 + 其他…）
```

### 3 · 消费点（`RuleCore.CanUseAbility`）

三态（照 E2/E3 的形状）：

| 档 | 处置 |
|---|---|
| 有数据 ∧ `enemyUnits == 1` | 过 36 格（`TargetCriteriaEval.Matches`，候选 = 那个敌方单位、`thisCard`/`targetCard` 都传施放者与候选）—— `false` ⇒ `ErrTarget` |
| `set == null` 且 `why == null`（明确没有这一格 / `ta=any`） | **放行**、不出声 |
| 判不了（列没落 / `ab` 非法 / 判不出走普通支还是替代支 / **`enemyUnits != 1`（候选侧不是敌方）** / 36 格判不了） | **放行 + 出声一次**（`Debug.LogWarning`，同一 `(卡,原因)` 只喊一次；计数 `RuleCore.ActiveAbilityPickCriteriaWarningsForTest`） |

🔴 **为什么 `enemyUnits != 1` 也要放行**：`CanUseAbility` 的候选**恒为敌方棋盘上的单位**
（`ctx.Players[1 - p].Board[targetSlot]`；表现层 `BattleDriver.CommandTargetSide` 对 `Ability` 也恒回 `1 - _me`）⇒
只有 `enemyUnits == 1` 时我们的候选才是原版候选集的**子集**、可以忠实过闸；
别的档（34 张 `ta=20 self` + 十几张「只打友方」）**根本不在原版这一档的候选集里** ——
拿「不在候选集里」当「不满足」会把那批主动能力**整条变成不可用**（⛔ 那就是自造规则）。

### 4 · 顺手发现（⛔ 只报，没改）

1. **`CanUseAbility` 恒拒敌方督军**（`t.IsWarlord → ErrTarget`）—— 原版只要 criteria 的 `includeHero == 1` 就能点督军
   （`AM69 Tempestor Sergeant` 的 criteria 正是 `includeHero=1 / includeMinions=0` = **只能打督军**）。
   ⇒ 今天那张卡的主动能力**一个合法目标都没有**。这是**先于本笔**的偏差（改它要动「主动能力候选侧」那一层）。
   > ⚠️ **2026-10-25 订正（执行代理 `E5` 现核 · 铁律 5）**：前半句成立（原版确实由 `includeHero` 决定），
   > **括号里的 `AM69` 例子不成立** —— 现读 `cards_raw.json` / `cards_engine.json`，`AM69` 的
   > `activeAbility.targetCriteria` = `friendlyUnits=1 / enemyUnits=0 / includeMinions=1 / includeHero=0 /
   > keywordFilter=5`（=「一个**友方** Regiment 部队」，卡面也是 `Regiment: Give +1 [Ranged] to a random
   > friendly troop`）。而**全池 `includeHero=1 ∧ includeMinions=0` 的行 = 0**（107 行里 `includeHero=1` 有 62 行）。
   > 督军那条偏差**仍然成立、也已修**（`RuleCore.CanUseAbilityTarget`，`E5-2`）；`AM69` 卡住的真实原因是
   > **「友方目标」这一整条路没建模**（本段第 3 条）＋它的正文没有 `Ability:` 段（`CanStartAbility` 先回
   > `ErrNoAbility`）。证据与断言 → `资料/普查产出_1025/E5_引擎剩余小缺口.md` §E5-3。
2. **`stealth` 恒拒**（不看「是不是异侧」）—— 原版只在**异侧**时拒。同样是先于本笔的偏差。
   > ✅ **2026-10-25（`E5` · `E5-2`）已修**：`RuleCore.CanUseAbilityTarget` 按 `targetOwner != p` 判（并带出
   > `CanUseAbilityTarget` 这个「候选侧显式」入口，让「同侧潜行可点」这条判据可断言）。
3. `RuleCore.CanUseAbility` 的**友方 / 自己目标**这一整条路**没建模**（`CommandTargetSide` 恒敌方）——
   这是 E4-2 真正的前置件。要落地它得改 `CardPresentation/Battle/BattleDriver.cs`（本代理**黑名单**）。
4. **`conditionsFilter` / `excludeCondition` 两格没进本列**：现读非零的只有 **1 行**（`SW9`，`cond=50 ex=1`），
   而 `unitConditions` 里 `ab=-1/-2` 的那些行是 **logic 级**（带 `lg`）= 不是同一格
   ⇒ 本列按「没有结构化条件」判（**比原版宽一格**）。要补得给 `unitConditions` 加一档 `role`。
5. `hasAlternativeAbility` 在 JSON 里**只落非 0**（1 张），其余 1126 张**缺席**；`CardDatabase` 缺席 → `0`（v21+）
   ⇒ `UsesAlternativeAbility` 返回 `false`（不是 `null`）。**合成夹具必须显式传 `hasAlternativeAbility: 0`**，
   否则闸会落进「判不了」档（本代理第一版探针就栽在这里，已就地记在夹具注释里）。
6. `TargetCriteriaEval.Matches` 对**任何一格 `Unknown(-1)`** 都回 `null`（判不了）⇒ 夹具必须给满 37 格
   （真数据是满的：生成器有键集自检）。同样栽过一次，已记在夹具注释。

---

## 三 · E4-3：易伤发两条 `Hit`

### 1 · 判据（J3 §3，本代理复核了引用的两个方法体）

- 第二发是**另一条 `DamageInfo` / 另一个动作**：`CardScript._ReceiveDamage_d__381__MoveNext.c:226` 造、
  `:252-254 BattleManager.DealMultiDamage(second, [target], priority:2)` ⇒ 它**再走一遍**
  dodge/shield/invuln → 堡垒 → 护甲 → 血 ⇒ 落到血上的是 `max(1, 易伤 − 护甲)`（我们的 `VulnerableDamagePart`）。
- 量 = `CurrentVulnerable` **原值**（不减护甲）；类型 = `(本次 type==Attack(4)) + 7`。
- **串行**（`DealMultiDamage` 是 `void`、只入队 priority=2）⇒ 主拍在前。
- 触发闸（全部满足）：`CurrentVulnerable>0` ∧ 无 dodge/shield/invulnerable ∧ 未被堡垒吸收 ∧ 本次类型不是 7/8 ∧
  `damageValue>0` ∧ **主伤害落完血仍 >0** ∧ `!isRemnant`。
- 时长：主 **1.0 s** + 易伤 **1.4 s** = **2.4 s**。

### 2 · 改动清单

| 文件 | 改动 |
|---|---|
| `Core/RuleCore.cs` | `DamageAfterReduction(u, dmg)` 加一个 `out int vulnPart` 的重载（**闸只算一次**，2 参版转发）；`ApplyDamage` 里 `vulnSecondPart` 由三处之一给出（护甲普通路 / 空投舱「池已碎」那一支 / 堡垒被打穿那一支），函数尾 **拆成两条 `EmitHit`**（主 = `actual − vulnSecondPart`，易伤 = `vulnSecondPart` + `vulnerableSecond: true`）；`EmitHit` / `EmitUnit` 各加一个缺省 `bool` 尾参 |
| `Core/BattleEvent.cs` | 新 `IsVulnerableSecondHit`（`Hit` 专用，带完整判据 doc） |

`u.Health -= actual` **没动**（还是一个合计）· `DamageToEnemy` 记账也**没动** ⇒ **算术零变化**，只把事件拆开。

### 3 · 离线读数（探针真跑）

| 场景 | Hit 条数 | 第 1 条 | 第 2 条 | 返回值 |
|---|---|---|---|---|
| 易伤 2 / 甲 1 / 打 5（非致死） | **2** | **4**（不含易伤） | **1**（`max(1,2−1)`）+ `IsVulnerableSecondHit` | 5 |
| 无易伤 | 1 | 4 | —— | 4 |
| 主伤害致死（9 打 9 血） | 1 | 9 | ——（`:210-211` 那道闸） | 9 |
| 残骸 | 1 | 5 | ——（`+0x65`） | 5 |
| 盾挡下 | 1 | **0** | —— | 0 |
| 堡垒 3 被打穿（打 5 / 甲 1 / 易伤 2） | **2** | **2**（溢出） | **1** | 3 |

### 4 · ⚠️ 与 `AU1` 的配合（**必须一起改，否则两拍都排 1.4 s**）

`Core/EventTiming.cs` 的 `HitHoldOf` 走钩子 `HitIsVulnerable`，而它现在由
**`CardPresentation/Battle/BattleDriver.cs:3642`** 接成「受击方**现在**带不带 `vulnerable`」——
拆成两拍之后，**两条** `Hit` 都会命中它 ⇒ **主拍被排成 1.4 s**（比 2.4 短、也比 1.0 长）。
✅ 正确改法（一行）：`EventTiming.HitIsVulnerable = e => e != null && e.IsVulnerableSecondHit;`
（本代理**没碰**那两个文件 —— 它们归 `AU1`。）

---

## 四 · 验收读数（如实）

| 命令 | rc | 读数 |
|---|---|---|
| `TMPDIR=/tmp/wf_e4 bash 工具/typecheck.sh` | **0** | 运行时 0 · 编辑器 0（E4-1 / E4-2 / E4-3 各跑一次，最后一次在夹具修正之后） |
| `bash 工具/ruleprobe.sh check`（**重钉后第 1 次**） | **0** | **解析差异 0 行** · 改名/新增/消失 0 · **哨兵合计失败 0 条** · ⑤ `[基线指纹]` **✓ 卡池与基线的指纹都与钉住的一致** · ⑦ 输出指纹 `af836aa0…` ✅ |
| `bash 工具/ruleprobe.sh check`（**第 2 次**） | **0** | 与第 1 次逐条相同（⑤ ✓ · 哨兵失败 0 · 解析差异 0） |
| `bash 工具/ruleprobe.sh play check` | **0** | 主档 **1127 张** + 事件电池 **586 张逐字节零差异**（两段差异列表都是空的）· 结算指纹 `87209677070c8421b8479e3bcc0effab`（与基线一致） |
| 离线探针（`_tmp_view/e4probe`，临时） | 0 | **45 条断言全过**（E4-1 5 + E4-2 12 + E4-3 28） |
| `python -I -X utf8 工具/gen_cards_engine.py --check` | 0 | 一致 ✅（v28 幂等，见 §二·2） |
| **`RuleEngineTest.Run`** | **未跑** | 本代理不许跑 Unity ⇒ `RuleEngineTest_EngineE4.cs`（438 行）**是第一次运行**；另见 §五·6 |

> 🔴 `play check` 零差异**证明不了**这三件的行为：E4-1 改的是**事件的字段**、E4-2 的闸在探针夹具里连
> `Ability:` 都没有（真卡更是 0 张）、E4-3 改的是事件条数 —— 三者都**不进** `play` 的棋盘快照指纹。
> 真实行为证据 = **离线探针那 45 条** + **待跑的 `RuleEngineTest_EngineE4`**。

---

## 五 · 没查清 / 残余（如实标着）

1. **E4-2 的闸今天不可达**（§一·②·c 的读数：全池 0 张有 `Ability:`）⇒ 它是**为「结构化 `ActiveAbility` 执行」预置的**。
   那一批落地时必须**逐张复核**这 48 张 `enemyUnits==1` 的卡（⛔ 别以为「闸已经验过真卡」）。
2. **友方 / 自己目标**这一整条路没建模（`CommandTargetSide` 恒敌方）—— E4-2 的**先决件**，且落在**黑名单文件**。
3. **`SW9` 那 1 行的 `conditionsFilter`/`excludeCondition` 没进本列**（§二·4·4）⇒ 那道闸对它是「宽一格」。
   > ✅ **2026-10-25（`E5` · `E5-1`）订正并已修**：**数据早就在** —— `SW9` 的 `unitConditions` =
   > `[{ab:-1, lg:-1, role:"target", cond:50, name:"flying", ex:1}]`，`lg = -1` 就是「直接挂在能力对象上」
   > （= 对象级 `activeAbility.targetCriteria`），**不是** logic 级。§二·4·4 说「那些是 logic 级、不是同一格」
   > **不成立**（错因：只看了 `lg >= 0` 那一批）。缺的只是**消费**：`ActiveAbilityPickGate` 里
   > `TargetCriteriaEval.Matches` 的 `cond` 形参当时恒传 `null`；现已改传
   > `card.TryGetCondition(ab, -1, "target", …)`（旧表未落该列 ⇒ 出声放行）。断言 →
   > `RuleEngineTest_EngineE5.cs` ①。
4. **E4-1 只接了「回合起止棋盘单位」那一趟**（有延后结算体的那条）。其余发射点（监听者 `EmitListenerTrigger` /
   天赋 / 605 / 410 / `WhileInPlay`）**仍不带目标**：
   · 监听者族的「目标 = 哪一格」判据**未定**（`CardAbility.abilityTargets` 与 `targetCard` 是两回事；
     例 `UM70` 的 `ta=30 target` 在 `Attack` 事件上筛的是**被打者**，而卡面正文的 `it` 是**攻击者**）——
     ⛔ 不猜；本代理**没接**。
   · 天赋 / 605 / 410 三族的 ability 目标列表根本没解析出来（`animTimes` 不携带 `lg`）。
5. **多目标只带 1 个**（原版按 `possibleTargets` 逐目标各播一份；`TAU13` 实测取第一个）。要真做得多目标，
   `BattleEvent` 要加一个列表位 + 表现层改成「一事件多落点」——**另一笔**。
6. ⚠️ **`RuleEngineTest` 里可能被「多一条 `Hit`」顶红的既有断言**（本代理**跑不了**，只能点名）：
   · `RuleEngineTest.cs:25026` / `:25045` 按**下标**取 `ctx.Signals[3]` / `ActionLog[...]`；
   · `:15617` / `:15640` 数 `EvtKind.Hit` 的**条数**；
   · `:13290` / `:24726` / `:25748` 取「第一条 Hit 的下标」。
   只有**夹具里带 `vulnerable`** 的那几格会多一条；`A1563` 那一族读的是 `ApplyDamage` 返回值与血量（**不受影响**）。
   若真红，那是**断言该跟着改**（原版就是两条），⛔ 不是回退。
7. `BattleEvent.IsVulnerableSecondHit` **不进录像 / 联机**（`BattleEvent` 本来就不进 `.json`）—— 与既有口径一致。

---

## 六 · 改动清单汇总 + 行尾

| 文件 | 改动 | 行尾（改完现数） |
|---|---|---|
| `Unity/工具/gen_cards_engine.py` | 新增 `active_ability_pick_criteria_of()` + `ACTIVE_ABILITY_PICK_CRITERIA_REPORT`（含判据长注释）· build 处写 `activeAbilityPickCriteria` · `version: 27 → 28` + v28 注释块 · 摘要行「目标判据⑤」· criteria 键集自检并进本列 · `set_bad` 出声列表 | CRLF 6292 / LF 6292（未翻） |
| `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | **重生成**（v28 · +116,151 B · md5 `0397005d…`） | CRLF 0 / LF 0（原样） |
| `.../Data/CardDatabase.cs` | `CardDto.activeAbilityPickCriteria` · `MapActiveAbilityPickCriteria`（三道行级闸；**不校验 `lg`**）· `Parse` 传参 + `>= 28` 版本位 · `< 28` 守卫出声 · `ReportTargetCriteriaColumns` 多一段 | CRLF 0 / LF 3088 |
| `.../Core/CardDef.cs` | ctor 尾参 ×2 · `_activeAbilityPickCriteria` + 撞键出声 · `ActiveAbilityPickCriteriaVersioned` / `RowCount` / `ActiveAbilityPickCriteriaFor(ab, out why)` | CRLF 0 / LF 7660 |
| `.../Core/RuleCore.cs` | `CanUseAbility` 尾段接 `ActiveAbilityPickGate` · 闸本体 + `NoteActivePickGap` + 计数 · `DamageAfterReduction(..., out vulnPart)` · `ApplyDamage` 拆两条 `EmitHit` · `EmitHit`/`EmitUnit` 尾参 | CRLF 0 / LF 12476 |
| `.../Core/EffectResolver.cs` | `ResolveTargets` 收口 + `ResolveTargetsCore` · `NoteTriggerTargetSink` · `EmitTurnTrigger*` 返回 `BattleEvent` · `AtTurnUnitJob.TriggerEvent` + arm/还原 | CRLF 14138 / LF 14138（未翻） |
| `.../Core/BattleEvent.cs` | `IsVulnerableSecondHit` + `Target*` doc 补判据 | CRLF 0 / LF 698 |
| `.../Core/BattleContext.cs` | `TriggerTargetSink` / `TriggerTargetSinkDone` | CRLF 0 / LF 2085 |
| `.../Editor/RuleEngineTest_EngineE4.cs` | **新增**（438 行 · 4 组：E4-1 真卡 / E4-2 数据+闸 / E4-3 六档） | 新文件，LF |
| `.../Editor/RuleEngineTest_EngineE4.cs.meta` | **新增**（guid `a227222319b94d5a906aa75220956ddc`） | Unity meta |
| `.../Editor/RuleEngineTest.cs` | `Run()` 加一行 `Step(TestE4TriggerTargetAndActivePick)` + 注释 | CRLF 37047 / LF 37047（未翻） |
| `.../Editor/RuleEngineTest_EngineD4.cs` | 版本断言 `27 → 28` + 更正痕迹 | CRLF 0 / LF 522 |

**没改**：`工具/ruleprobe/**`（**一个字节都没动**，含 `PinnedPoolMd5` —— 简报要求不重钉）· 基线文件 ·
`CardPresentation/**`（除白名单外的全部）· 三份正本 · 任何 `任务总表.md` · git。

### ✅ 已重钉（主对话 2026-10-25 裁定后执行 · E4-A）

| 项 | 值 |
|---|---|
| 旧 | `5a17f6af49ead1829b067bd9e9082118`（卡表 v27 · 3,119,176 B） |
| 新 | **`0397005df1c7de826f56e651c4ca7b01`**（卡表 v28 · 3,235,327 B · +116,151 B） |
| 为什么 | v27→v28 **只加了一列** `activeAbilityPickCriteria`（107 行 / 107 张卡；**既有列的字段一个都没改**）。+116,151 B 全部来自这一列 |
| 位置 | `工具/ruleprobe/Program.cs:251` + 上方归因注释块（E9/E2/E3 的旧段**全部保留**，只追加新段） |
| 未动 | `PinnedBaselineMd5`（`71cedcd7…`）· `PinnedParseMd5`（`af836aa0…`）· 基线文件 · `PlayProbe.cs` 逻辑 —— **一个字没动**（`git diff` 的删除行只有 HEAD 那一行旧 pin） |

重钉后读数：`check` **连跑两次均 rc=0**（⑤ `[基线指纹]` ✓ · 哨兵失败 **0** · 解析差异 **0 行** · ⑦ `af836aa0…` ✓）·
`play check` **rc=0**（主档 1127 + 电池 586 逐字节零差异 · 结算指纹 `87209677…` 不变）。

### 仍开（⛔ 本批不做，已记账）

1. ~~**`SW9 Fenrisian Wolf` 那 1 行**：`activeAbility.targetCriteria` 的 `conditionsFilter = 50` / `excludeCondition = 1`
   **没进** `activeAbilityPickCriteria` 列（`unitConditions` 里 `ab=-1/-2` 的那些行是 **logic 级**（带 `lg`）= 不是同一格）
   ⇒ 那道闸对它是「**宽一格**」。要补得给 `unitConditions` 加一档 `role`（**另一笔**）。~~
   > ✅ **2026-10-25 已做完（`E5` · `E5-1`）· 且上面那条理由被证伪**：那一行**就是**对象级的
   > `{ab:-1, lg:-1, role:"target"}`（`lg = -1` = 直接挂在能力对象上），不需要新 `role`；
   > 缺的只是消费端把 `cond` 传进 `TargetCriteriaEval.Matches`。详见 §五·3 的订正框。
2. **`BattleDriver.cs:3642`**（`EventTiming.HitIsVulnerable`）要改成读 `e.IsVulnerableSecondHit`（§三·4）—— 归 `AU1`。
