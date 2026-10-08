# W_Targeted填充 — `A985⑥①`（能做的那些）+ `A179⑤` · 2026-10-18 第三会话

> 执行写手代理。**只动白名单三个文件**：`RuleEngine/Core/{RuleCore,EffectResolver,EffectText}.cs`。
> ⛔ 没跑 Unity · 没动 git · 没改正本。✅ 秒级类型检查（`TMPDIR=/tmp/wf_tg`、`/tmp/wf_tg2` 各一次）末次 **运行时 0 / 编辑器 0**。

## ① 判据（本笔自己逐行读的方法体，不是转述）

`decomp_full/CemeteryManager__GetActionText.c`（372 行）：
- **case `0xF`（`:194`）** = `Ability` 二选一：`op_Equality(uStack_50, 0, 0)`，`uStack_50 = *(undefined8 *)(param_2 + 10)`（`:197`）—— ⚠️ **`param_2` 是 `int*`** ⇒ 下标 10 = **字节 0x28**（目标引用）。
- **case `10`（`:85`）** = `Play` 六选一，**两个 bool**：「靶向」= 同一条 `op_Equality(… uStack_50 …)`（字节 0x28）·「伏击」= **字节 0x1C**（`local_18._4_1_`，`local_18` 取自 `param_2 + 0x18`）·「哪一方」= `cVar3 = (char)*(param_2 + 6)` = 字节 0x18 首字节（与 `0x14` 支同一个 `isPlayer` 位）。
  ⇒ **2(靶向) + 2(普通) + 2(伏击) = 6**，与 `G5` 那张表逐条对上；**伏击有自己的位**，不是从 `isPlayer` 推的。

## ① 两处消费者读到什么 · 怎么变

| 消费者 | 表达式 | 填之前 | 填之后 |
|---|---|---|---|
| `BattleDriver.cs:1824` → `WFEffectCardContext.targetIsPlayer` | `e.TargetPlayer == _me` | **恒 `false`**（-1） | **目标侧**：`_me` 施放仍 `false`；**AI 施放带目标技能 ⇒ `false→true`** |
| `BattleDriver.cs:1450` → `WFModuleCollisions.targetIsWarlord` | `_animfxLastEvent.TargetSlot == WarlordSlot` | 恒 `false`（-1） | 目标正好是督军格 ⇒ **`false→true`** |

（第三处 `BattleDriver.cs:425 EvtText` 只是**录像诊断串**（`:512/:532` 存进 `traceLogTail`）；两端同一算法 ⇒ 不参与成败。）
下游：`WFModuleCollisions.ResolveColliderId`（`:626`）只有 `DynamicTarget(25)` 读 `targetIsPlayer`；`Opponent/MySelf/DynamicTarget` 三支读 `targetIsWarlord`；`WFModuleTransformModifier.ResolveObjective`（`:383-384`）`objective != Caster ⇒ return ctx.targetIsPlayer`。⇒ **只有「带目标的技能 / 靶向战术卡」那几条特效会受影响。**

## ① 填了 / 没填（按符号）

| 落点 | 处置 |
|---|---|
| `RuleCore.cs:4201-4205`（`UseAbility` 的 `Emit(EvtKind.Ability,…)`） | ✅ **填**：`chosen != null` 时给 `targetPlayer: 1-p` / `targetSlot` / `targetCardId: chosen.Name`；否则三个留默认 |
| `EffectResolver.cs:1103-1107`（`PlayTactic` 的 `Emit(EvtKind.Play,…)`） | ✅ **填**：判别式 = **`EffectText.PickTarget(ops) != null` 且 `chosen != null`**（见下） |
| `RuleCore.cs:4324`（`UseOathAbility` 的 `Emit(EvtKind.Ability,…)`） | ➖ **不填** —— 誓约**没有**目标引用（原版那条链 `PayActiveAbilityCostOath` → `ResolveActiveAbilityPlayed`，不带 +0x28） |
| `RuleCore.cs:1537`（`PlayCard` **单位卡**的 `Emit(EvtKind.Play,…)`） | ➖ **不填** —— 单位卡是 `ActionYouPlay`/`ActionOpponentPlays`，**不靶向**；`e.Slot` 是**落点**不是目标 |

### 🔴 关键坑（现读推翻了「`chosen != null` 就是判别式」这条直觉）
`CanPlayTactic` **只在卡面要求选目标时**才校验 `targetSlot`（`spec != null` 那一支），而**不需要目标的战术卡也能被拖到某一格上打出** ⇒ `PlayTactic` 里的 `chosen` 会**非空**。⇒ `Play` 那条**必须**写成两个条件都满足；只写 `chosen != null` 会把「随便放的那一格」静默记成「靶向」。

### ⛔ 今天做不了的：`Play` 的「伏击」位
原版它有**自己的位（字节 0x1C）**；我们这边伏击事实**算得出**（`RuleCore.cs:1611` 的
`card.Has(KeywordTable.Ambush) && card.TriggerOps(KeywordTable.Ambush) != null`，是**卡的纯函数**、在 `:1537` 就可算），
但**没有字段可放**：`BattleEvent.cs` 不在白名单，⛔ 我也没拿 `Effect`/`Keyword` 自造编码（那是口径，要你裁）。
请裁：**(a)** `BattleEvent.cs` 加 `bool PlayAmbush`（最干净）；**(b)** 复用 `Effect: "ambush"`
（有先例 —— 上一批给 `AmbushExit` 用过 `effect:"damage"/"window"`，但那同样是自造编码）。

### 🔴 同行一条：`BattleEvent.cs:204` 的字段 doc 现在是错的
写着「**只有 `Attack` 用**」—— 填了之后 `Play`/`Ability` 也用 ⇒ 请由属主改成
「`Attack` 必填；`Play`/`Ability` **有目标时**填（判别式见 `CemeteryManager__GetActionText.c` case `0xF`/`10`）」。

## ① 该配的断言清单（宿主 = `RuleEngine/Editor/RuleEngineTest.cs`；本笔白名单外 ⇒ 只给形状，请转派）

| # | 断什么（**两态**） | 🧨 改坏法 | 灭自证 |
|---|---|---|---|
| ① | 带目标技能（照 `TestAbility:18169-18179` 那组夹具，`UseAbility(ctx,0,3,3)`）：`FindSignal(Ability)` ⇒ `TargetPlayer==1`、`TargetSlot==3`、`TargetCardId==` 那一格的卡名 | 删 `Emit` 那三个具名实参 ⇒ 红 | **格位不写字面量**：用 `ev.TargetSlot` 回查 `ctx.Players[1].Board[…]`，断它**就是**那次 `chosen`（比 `Instance.Id`），再与 `TargetCardId` 交叉 ⇒「随便填个合法格位」过不了 |
| ② | 不带目标的技能（`NeedsPick == false`，不传 `targetSlot`）⇒ 三个字段**一个都没填**（`-1/-1/null`） | 条件写成无条件填 ⇒ 红 | 与 ① 成对 ⇒ 「永远填」「永远不填」都过不了 |
| ③ | **誓约** `UseOathAbility` 成功那条 ⇒ 仍是 `-1/-1/null` | 顺手把那三个实参也加到 `:4324` ⇒ 红 | 「别顺手填宽」——誓约无目标可填（判据见上表） |
| ④ | **靶向战术卡**（`:17873` 一带的现成用例）⇒ 三个字段填上，且 `TargetPlayer` **跟着 `PickSide` 那一边走** | 删 `EffectResolver.cs:1105-1107` 三个实参 ⇒ 红 | **换侧**：再打一张 `Side == "own"` 的靶向卡 ⇒ 写死 `1-p` 的实现过不了 |
| ⑤ | **非靶向**战术卡（`Refill`/`Draw` 那类）**拖到己方有单位的格上** ⇒ 仍是 `-1/-1/null` | 去掉 `PickTarget(ops) != null &&` 那半句 ⇒ 红 | 🔴 **必须用「拖到有人的格」这一态**：拖到空格时 `chosen` 本就是 null ⇒ 去掉那半句也照样绿（**假绿**） |
| ⑥ | **单位卡**（`PlayCard` 单位分支）那条 `Play` ⇒ 仍是 `-1/-1/null` | 在 `RuleCore.cs:1537` 上也填 ⇒ 红 | 与 ④ 成对 ⇒「凡 `Play` 都把 `e.Slot` 抄进 `TargetSlot`」过不了 |
| ⑦ | **台账**：`EvtKind.Ability` 发出点 **2** 处 · `EvtKind.Play` 发出点 **2** 处（防将来新增第 3 处漏填） | 新增一处 emit ⇒ 红 | 表式断言（数发出点），与行为无关 |

**表现层侧（宿主 = `CardPresentation/Editor/BattleScene.cs`，另有属主）**：`BuildCardContext` 对**带目标技能**给出的
`targetIsPlayer` 应是**目标那一侧**（AI 施放 ⇒ `true`）· `targetIsWarlord` 只在目标格 = 督军格时为 `true`；
两态 = 我方施放 / AI 施放；🧨 改坏法 = 把 `Emit` 的目标实参删回 `-1` ⇒ 红。

## ② `Summary()` 措辞

- **改前**：`return $"战术卡文本解析：完全解析 {Full}/{Cards}" + …` —— 该方法**不知道卡种**
  （`Coverage(cards, type, …)` 的 `type` 只决定装哪类卡），却被 `RuleEngineTest.ReportUnitDescCoverage`
  印在 `[unit]`/`[hero]`/`[defence]` 三张表的表头上（实测日志原文：`RE    [unit] 战术卡文本解析：完全解析 586/586…`）⇒ **话说的不是它记的事**。
- **改后**（`:1295-1297`）：`return $"文本解析：完全解析 {Full}/{Cards}" + …`，并在 `Summary()` 上补 XML doc，
  写明「**通用的**文本摘要，被 `unit`/`hero`/`defence`/战术卡**共用**；卡种由调用方那行前缀（如 `## [unit]`）给」。
  **一行行为都没动**（只从字面量里删掉「战术卡」四个字 + 加注释）。
- **那两份 `.txt` 查清了**：`_tmp_view/tactic_unparsed.txt` ← `RuleEngineTest.DumpUnparsed`（`RuleEngineTest.cs:17082-17083`）·
  `_tmp_view/unit_desc_unparsed.txt` ← `ReportUnitDescCoverage`（`:14113-14114`）。**都是自动生成的**
  （跑自检时 `File.WriteAllText` 重写；`.gitignore:154` 忽略 `_tmp_view/` ⇒ **不进 diff**）。**重跑命令**（必须串行；
  本笔不许跑 Unity，故**今日没重生成** —— 两份 `.txt` 首行仍是旧文案，下次自检自动改）：
  `unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run -logFile -`
- ⚠️ **不在白名单、未动**：`RuleEngineTest.cs:17047` 自己有独立字面量 `"战术卡文本解析 —— 未覆盖清单…"`
  （**战术卡报表的标题**，不算错、不用改）；`资料/战术卡效果_移植方案.md:182` 引用旧文案（纯文档）。

## 类型检查 · 行尾 · 没查清

**类型检查**：`运行时错误数: 0` / `编辑器错误数: 0`（改动后各一次；**没有**别人半成品的假错）。

**行尾**（`b.count(b'\r\n')` vs `b.count(b'\n')`，**不是** `file`；全程 Edit、⛔ 无 `sed -i`）：

| 文件 | 改前 | 改后 | 判定 | 净增行 |
|---|---|---|---|---|
| `RuleCore.cs` | CRLF 0 / loneLF 5236 | CRLF **0** / loneLF 5263 | **纯 LF（保持）** ✅ | +27 |
| `EffectResolver.cs` | CRLF 7597 / loneLF 0 | CRLF **7624** / loneLF **0** | **纯 CRLF（保持）** ✅ | +27 |
| `EffectText.cs` | CRLF 0 / loneLF 7656 | CRLF **0** / loneLF 7672 | **纯 LF（保持）** ✅ | +16 |

**哈希/联机影响：零**（判了，不是猜）：`NetProtocol.Fingerprint`/`StateHash` 只读
`Turn/Active/Winner/ctx.Events.Count/Players[*]`（`Fingerprint` 实读）；本笔**没加一句 `ctx.Log`** ⇒ `Events.Count` 不变；
`ActionLog`/`Signals` **不参与任何哈希、也不上线**（`CardPresentation/Net/*.cs` 里 0 处引用）⇒ **旧录像 / 旧联机局不受影响**。

**没查清 / 请你处置**
1. **`Play` 的「伏击」位放哪**（见上）—— 要 `BattleEvent.cs`（白名单外）或你裁一个编码。
2. **`BattleEvent.cs:204` 的 doc「只有 `Attack` 用」现已过时** —— 白名单外，请转派一行。
3. **表现层没跑**：`targetIsPlayer`/`targetIsWarlord` 的画面差异只做了**静态推导** ⇒
   **收口必跑 `RuleEngineTest.Run`（宿主 = 引擎事件）+ `BattleScene.Run`（AnimFX 上下文 + 战斗日志面板）**。
4. **19 条文案值仍在远端**（与本笔无关，照旧挂着）。
