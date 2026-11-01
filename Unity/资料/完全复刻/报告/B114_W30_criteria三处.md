# B114 · 写手 `W-30` · `B-245` —— `B-177` 刻意没接的三处：回源判定 + 落地

> 状态：**两处已在白名单内落地 + 一处（整道闸）交出补丁**；`typecheck 0/0`、账闸 `A 5,841 / 漂移 0 / 失败 0`。
> ⚠️ **宿主未跑**（本波禁 Unity）⇒ 断言的真值读数由调度台在同步点跑出来。
> 报告只写「凭什么 / 做了什么 / 还差什么」，⛔ 不写「已完成」（那是活状态表的事）。

---

## 一、结论（逐处：原版到底复不复核 criteria）

| # | 那一处 | 原版**也**复核 criteria？ | 本轮动作 |
|---|---|---|---|
| ① | **`Synapse`**（`RepeatTacticOnAdjacent` 的 `IsLegalPick` 不传 `casterCard`） | ✅ **是**（`CardScript__TargetedSpellPlayed.c:51-69` 对每个相邻重跑 `IsValidTarget`） | ✅ **已接**（`EffectResolver.cs:2965` 加 `casterCard: card`） |
| ② | **`PlayTargetPool` 的 `asIfOnBoard` 预览单位不过闸** | ✅ **是**（落位后它**自己也在候选池里**，原版扫的就是落位后的棋盘） | 🟠 **只补得动一格**（`includeSelf`，不依赖棋盘）；其余 35 格要 `TargetCriteriaEval` 开 side 钩子 ⇒ **补丁见 §四**（`TargetCriteriaEval.cs` 不在我的白名单） |
| ③ | **`includeSelf` / `excludeNotSelf` 两格在点选路跳过** | ⚠️ **拆成两半，结论相反**：<br>· `includeSelf` —— 原版**也判**，但点选路 `候选 == thisCard` **恒假** ⇒ 那一格**永不命中** ⇒ 跳过 **= 等价**（⛔ 别补）<br>· `excludeNotSelf` —— 原版 `候选 != thisCard` **恒真** ⇒ **整池淘汰** ⇒ 跳过是**真缺口** | ✅ **`excludeNotSelf` 已补**（`EffectResolver.cs:21278` 同一行追加 + helper）；`includeSelf` **刻意不改** |

**一句话**：三处里 ①③(后半) 判据支持、已接；③(前半) 判据支持「跳过本来就对」、反证写在下面 §二；
②判据支持但**落点不在白名单**，只做了一格、其余交补丁。

---

## 二、证据（文件:行号，全部现读）

### ① `Synapse` 那条 —— 原版**同一个谓词**，且 `thisCard` = 这张战术自己

| 事实 | 判据 |
|---|---|
| 相邻循环里逐条调 `IsValidTarget` | `d:/2/tools/decomp_full/CardScript__TargetedSpellPlayed.c:49-69`：`GetAdjacentUnits` → 每个 `nb` 走 vtable `+0x318`（`:57-58`）= `RawCardScript.IsValidTarget` |
| 实参 = `(rawOf这张战术, 这张战术, 相邻)` | `.c:53` `plVar1 = *(*(param_3+0x288)+0x28)`，而 `:33` 把 `param_3+0x288` 写成 `param_1`（= 被打出的这张战术）⇒ `raw` 是**这张战术自己的**；`.c:57-58` 的第 2 实参 = `param_2` |
| `param_2` 就是这张战术**自己** | 唯一调用点 `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1263`：`TargetedSpellPlayed(lVar23, lVar23, chosen, 0)` ⇒ 第 1、2 实参同一个对象 |
| 与「玩家点目标」是同一条闸 | `BattleManager__IsValidSpellTarget.c:59-62` 也是 `(*(param_2+0x28), param_2, param_3)` 调 vtable `+0x318` |
| `IsValidTarget` 末句 = criteria | `RawCardScript__IsValidTarget.c:43-44` → `FilterMethods__CheckIfMeetsCriteria(thisCard=param_2, 目标, 目标, *(raw+0x280))` |

**顺带修对的一条**：`IsValidTarget.c:30-36` 的 camouflage 只在 `卡种 == 0x14(tactic) ∧ 异侧` 时挡 ——
我们把 `casterCard` 补上后，`AddSide` 的 `tacticCaster` 也跟着一起对（原来这条路上伪装**完全不挡**）。

**为什么 `targeted == 0 ⇒ 不筛` 的闸在这里**不是**多一道**：`TargetedSpellPlayed` 是**被点过目标的法术**才走到的
（状态机 `:1263` 传的就是 `chosen != 0`），而 `chosen` 非空必须先过 `IsValidSpellTarget` 的 `get_targeted`（`.c:46`）
⇒ `targeted == 0` 结构上进不了这一支，两个口径**等价**。

### ③ `includeSelf` 为什么「跳过 = 等价」（反证，⛔ 别照着补）

`FilterMethods__CheckIfMeetsCriteria.c:236-243` 逐句：

```c
cVar2 = op_Equality(param_1 /*thisCard*/, lVar14 /*候选*/, 0);
if (cVar2 && *(char*)(param_4 + 0x1d) == '\0') return 0;      // includeSelf == 0 ∧ 候选 == thisCard ⇒ 淘汰
cVar2 = op_Inequality(param_1, 候选, 0);
if (cVar2 && *(char*)(param_4 + 0x1e) != '\0') return 0;      // excludeNotSelf != 0 ∧ 候选 != thisCard ⇒ 淘汰
```

- 点选路的 `thisCard` = **打出的那张牌**（`IsValidSpellTarget.c:61-62` 传 `param_2`；`GetValidTargets.c:31-40`
  传的是那张单位卡）—— 它**永远不是**棋盘上的候选对象 ⇒ `候选 == thisCard` **恒假**：
  - `includeSelf == 0` ⇒ 第一句**永不命中** ⇒ 我们跳过它**结果一模一样** ⇒ **不改**（补了会把一批卡凭空打死）；
  - `excludeNotSelf != 0` ⇒ 第二句**恒真** ⇒ **整池淘汰** ⇒ 跳过是**真缺口** ⇒ 已补。
- ⚠️ 这两句只在 `targetsAffected == 10` 时可达：别的档在原版里走**更早的 switch 短路**（`.c:56-160`）
  ⇒ 补齐的那条带 `ta == 10` 的闸（现读 `targeted == 1` 的 246 张**全是 10**，见断言 `前提 0a`）。

### ② 预览单位 —— 原版**复核**，但复核的是「落位之后」的那张

| 事实 | 判据 |
|---|---|
| 落位 → 再问有没有合法目标 | `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:849-860`：`IsThereValidUnitTarget(mgr, 卡)` |
| 那个函数扫的就是棋盘、逐候选过 `IsValidTarget` | `BattleManager__IsThereValidUnitTarget.c` → `RawCardScript__GetValidTargets.c:31-45`（遍历 `manager+0x470`，逐个 `vtable+0x318`） |
| ⇒ 待落地那张**自己也在池里**，`thisCard` 就是它 | 同上第 2 实参 = 那张卡；`includeSelf == 1` 的 `SOR66 Armorium Cherub` 正是为此存在 |

⇒ **结论：原版确实复核**。我们这一侧执行不了整道，原因是 `TargetCriteriaEval.SideOf` 只认棋盘（`TargetCriteriaEval.cs:637-647`），
预览不在棋盘 ⇒ 恒 `-1` ⇒ `Matches` 在**阵营闸（`:230-231`）**就返回 `null`（判不了），`includeSelf`（`:245-255`）
排在那之后 ⇒ **根本走不到**。所以：
- ✅ **能判的一格** = `includeSelf`（预览对象就是 thisCard ⇒ `候选 == thisCard` **恒真**，不需要棋盘）⇒ 已补；
- 🟠 **其余 35 格**（阵营 / 兵种 / 关键词 / stat / trait / army …）⇒ 要 side 钩子 ⇒ 补丁见 §四。

---

## 三、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Assets/RuleEngine/Core/EffectResolver.cs` | ① `:2965` 同一行加 `casterCard: card`；② `:2203` 同一行加 `!PreviewSelfExcludedByCardCriteria(card)`；③ `:21278` 同一行追加 `excludeNotSelf` 整池淘汰支；④ `:21206-21230` 那段头注释**就地改写**（原「突触不传 / 预览不过闸，不算分叉」已过期，按铁律 5 改掉） | **0**（21,484 行不变） |
| `Assets/RuleEngine/Core/EffectResolverPartial_B114W30.cs` | **新建**：两个 helper（`CardCriteriaExcludeNotSelfEmptiesPool` / `PreviewSelfExcludedByCardCriteria`）+ 三处的判据头注释 | **+80** |
| `Assets/RuleEngine/Core/EffectResolverPartial_B114W30.cs.meta` | **新建** | +2 |
| `Assets/CardPresentation/Editor/B114_W30_Assert.cs` | **新建**：`CheckB114W30CriteriaThreePlaces`（A/B/C 三段 + 数据面前提） | **+252** |
| `Assets/CardPresentation/Editor/B114_W30_Assert.cs.meta` | **新建** | +2 |
| `Assets/RuleEngine/Editor/RuleEngineTest.cs` | 聚合行 `:1367` **同一物理行**追加 `Guard("CheckB114W30CriteriaThreePlaces", CheckB114W30CriteriaThreePlaces);` | **0**（39,737 行不变） |

**验证（我跑的，不是转述）**：
- `TMPDIR=/tmp/wf_w30 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
- `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` → **A 5,841 条全通过 · 漂移 0 · 失败 0**
  （两处改动都是「同一物理行」，新文件落在文件尾/新文件 ⇒ 零漂移确证）。
- 行尾：`EffectResolver.cs` / `RuleEngineTest.cs` 现读 **纯 CRLF、裸 CR 0、行数与改前一致**；
  两个新文件纯 LF（与同目录 `EffectResolverPartial_B109E6.cs` / `B114_A_Assert.cs` 一致）。

### 断言三条（怎么满足「① 前提 ② 结论 ③ 灭自证」）

| 段 | 前提 | 结论 | 灭自证 |
|---|---|---|---|
| **0** 数据面 | `targeted==1` 的 246 张 `ta` 恒 10（`0a`）· 全池 `excludeNotSelf` 全 0（`0b`）· 有 `includeSelf==1` 的卡（`0c`） | —— | 这三条一破，下面两条补齐的前提就不成立 ⇒ 红 |
| **A** 突触 | 夹具挂上卡级 criteria（`A0`）· 锚点带 Synapse（`A1`）· 两邻居 Vehicle/Infantry（`A2`） | `A5/A5b`：锚点 + Vehicle 邻居都被治（4→13 / 4→7） | **`A6`：Infantry 邻居**必须**还是 4** —— 摘掉 `casterCard: card` ⇒ 变 7、红；`A7b/A7c` 是**同一块棋盘只把 `keywordFilter` 换成 0** 的对照（两个都变 7）⇒ 差异只可能来自 criteria |
| **B** `excludeNotSelf` | 友军在场上（`B0`） | `B1`：池子 **0** | **`B2`：同一张牌只把 `excludeNotSelf` 翻成 0 ⇒ 同一个候选必须留在池里** —— 恒剔 / 不接 各红一头 |
| **C** 预览 self | `PlayTargetPoolAsIfDeployed` 有返回（`C0`）· **`C1`：预览不在棋盘 ⇒ 逐候选谓词给 `null`**（这就是「其余 35 格接不上」的机制证据） | `C2`：`includeSelf=0` ⇒ 预览**不进池** | **`C3`：同一张牌只翻 `includeSelf` 一位 ⇒ 预览进池** ⇒ 说明 `C2` 不是「池恒空」 |

---

## 四、补丁（②的全量接法 —— 需要 `TargetCriteriaEval.cs` 的写手）

> ⛔ `TargetCriteriaEval.cs` **不在我这份简报的白名单**，`Matches` 的 side 只在内部算，从外部绕不过去
> ⇒ 按铁律 13·3③「撞上了就别硬改，写成可粘贴补丁交回」。三步，改动都很小：

**(1) `TargetCriteriaEval.cs` · `Matches` 加一个可选形参（追加在形参表尾，老调用点零影响）**

```csharp
public static bool? Matches(BattleContext ctx, int owner, CardDef.CountSetSpec set,
                            UnitState candidate, CardDef.ConditionSpec cond,
                            UnitState thisCard, UnitState targetCard,
                            out string gap,
                            int? sideOverride = null)      // 🆕 B-245：预览/离场对象的侧由调用方给
{
    ...
    int side = sideOverride ?? SideOf(ctx, candidate);     // 改这一行（原 = `SideOf(ctx, candidate)`）
```

**(2) `EffectResolver.cs` · `CardTargetCriteriaAccepts` 透传 `thisCard` / `sideOverride`**

```csharp
public static bool? CardTargetCriteriaAccepts(BattleContext ctx, int owner, CardDef card,
                                              UnitState candidate, out string gap,
                                              UnitState thisCard = null, int? sideOverride = null)
{
    ...
    return TargetCriteriaEval.Matches(ctx, owner, card.CardTargetCriteria, candidate,
                                      null, thisCard, candidate, out gap, sideOverride);
}
```
⚠️ `thisCard` **必须**传预览自己（不是 `null`）：原版这一档的 `thisCard` 就是待落地那张单位
⇒ `includeSelf` / `excludeSimilarCards` / 四组 stat 的「载体值」都靠它；传 `null` 会退回「跳过」。

**(3) `EffectResolver.cs` · `PlayTargetPool` 的 probe 换成整道闸**（把 `:2203` 那个窄口替掉）

```csharp
var probe = new List<UnitState> { asIfOnBoard };
try
{
    ApplyTargetFilters(ctx, probe, spec, quiet: true, structuredCondition: pickCond,
                       targetsAffected: TargetsAffectedForOp(ops, spec));
    // 🆕 B-245：原版是「落位之后」问（IsThereValidUnitTarget → GetValidTargets → 逐候选 IsValidTarget）
    //   ⇒ 待落地那张自己也要过整道 criteria；它不在棋盘 ⇒ 侧由调用方给（owner = 出牌者那一方，
    //   单位落地必在出牌者棋盘），thisCard = 它自己。
    ApplyCardTargetCriteriaFilter(ctx, probe, owner, card, quiet: true,
                                  thisCard: asIfOnBoard, sideOverride: owner);
}
catch (...) { ... }
if (probe.Count > 0) pool.Add(asIfOnBoard);
```
⚠️ **必须仍然「单独跑 probe」**（现状已是如此）：`ApplyCardTargetCriteriaFilter` 遇到「判不了」是**整条不筛**，
塞进 `pool` 会让棋盘那批的筛也一起废掉。
⚠️ 接完这条以后，`PreviewSelfExcludedByCardCriteria` 可以退役（它只是 (1)(2) 落地前的窄口）；
**今天两条并存是安全的**（窄口只剔 `includeSelf == 0`，全量闸会给出同一结论，额外多剔别的格）。
⚠️ 接完要重跑 `BattleScene.cs:29496` 那条 `n == 0` 的断言：`SelfTestOnlySelfTarget`（`subtype = Vehicle`，
卡级 criteria **未挂**）不受影响；但如果谁给它挂了 `includeSelf = 0` 的 criteria，那条夹具的「池非空」前提会翻。

---

## 五、预计影响面

| 改动 | 影响面（静态推算，非实测） |
|---|---|
| ① 突触传 `casterCard` | 只在「**被点过目标的战术** × 命中 **带 `Synapse` 的单位**（现读 **13 张**：`TL19 Broodlord` / `TL30`… 全是单位）+ 邻居存在」时可能变。再叠一层：那张战术的**卡级 criteria 必须真能收窄**（除侧/卡种外还开闸的战术现读 **11 张**：`Flickerjump` / `Execution` / `Master of Manoeuvre` / `Methodical Destruction` / `More Dakka` / `Ramshackle` / `Wreckin' Ball` / `Canis Helix` / `Shield Generator` / `Zephyr's Grace` / `Inspired Retribution` / `Light Cover`…）⇒ 交集很小，`play check` 指纹**预计 0 或个位数变化**，需调度台跑一次确认。 |
| ① 的副作用（伪装闸） | 同一条路上 `tacticCaster` 现在为真 ⇒ 相邻的**敌人**若带 `Camouflage` 会被挡（原来不挡）。只在 `spec.Side` 含 `enemy` / `any` 时可达。 |
| ② 预览 `includeSelf` | `targeted==1 · ta==10 · includeSelf==0 · friendlyUnits==1` 的**单位卡现读 14 张**（`AM70 Scion Medic` / `DA5 Apothecary` / `GOF80 Grot Orderly` / `GOF18 Painboy Sniklaw` / `SW14 Wolf Priest` / `SOR22` / `SOR31` / `SW26` / `UM89` / `ASH70` / `SAU19` / `GSC4` / `TAU68` / `TAU33`）。<br>其中 **13 张**除侧/卡种外没有别的闸 ⇒ 这一格接完就**与原版一致**；<br>`ASH70 Swooping Hawk Exarch` 还带 `traitsFilter = 980(vanguard)` ⇒ 它自己算不算 vanguard 仍需 §四 那条整闸（这一格仍属未覆盖）。<br>另外 **35 张**敌方目标（`friendlyUnits=0`）的单位卡本来就不该把「友军的自己」算进池子 ⇒ 这次被 `includeSelf==0` **顺带**挡住（见 §六·1）。 |
| ③ `excludeNotSelf` | 全池 **0 张** ⇒ **零行为变化**，补的是「将来写得出」这一层。 |

---

## 六、没查清的部分（⛔ 不猜）

1. **没跑宿主 ⇒ 断言的真值读数未知**。本波禁止 Unity，`RuleEngineTest.Run` 必须由调度台在同步点跑。
   我核的是「编译过 + 账闸零漂移」，**不能**核「断言绿」。若红，最可能的两处（按判据推的，仍属推断）：
   - `A6`：`IsLegalPick` 的 `AddSide` 池里 `mid`（锚点）也是 Vehicle、也会进池 —— 不影响 A6（只比左右邻居）。
   - `C2/C3`：夹具 warlord 靠 `includeHero = 0` 从池里剔掉；若 `AddSide` 之后还有别的单位混进来（`Deck` 的
     filler 会不会随手抽到场上？`ProbeBattle` 只放督军），`C2` 的 `Count == 0` 会红。**旁证**：`Battle` 起手只发手牌、不上场。
2. **②的其余 35 格仍未接**（要 §四 的补丁）。今天预览这一档的最终结果 = 「`includeSelf` 判过、别的格没判」。
3. **`ASH70` 的 `traitsFilter = 980`** 对「它自己」是否成立，我没核（需要它的 `CardTraits` 与 vanguard 判据）。
4. **`ta == 10` 那条闸的边界**：`targeted == -1`（手写夹具）且 `ta == 0` 的卡走不到补齐支（我特意加的闸）
   —— 这是**保守方向**（宁可少补），⛔ 不是「原版如此」；原版 `ta=0(any)` 本来就在更早的地方 `return 1`。
5. **突触那一支的 `pickCond` / `targetsAffected` 仍未传**（`IsLegalPick` 还有这两个形参默认为
   `null` / `Unknown`）。它们不是原版 `IsValidTarget` 的东西（是我们 `B-16` 的结构化条件补充）⇒ 本次**不动**；
   若将来判定「结构化条件也要对相邻判」，落点就是同一行。

---

## 七、顺手发现（⛔ 没改，只报）

1. 🔴 **`PlayTargetPool` 的 `asIfOnBoard` 预览不看 `spec.Side`** —— 侧筛只发生在 `AddSide`（`EffectResolver.cs:3061-3085`），
   而预览是直接塞进 `probe` 走 `ApplyTargetFilters`（**不做侧筛**）⇒ **敌方目标**（`Rally: Deal N damage to an enemy unit`）
   的单位卡会把**自己（友军）**算进「要不要问」的判定里。现读 **35 张**单位卡是 `friendlyUnits=0` ⇒ 这类池子会多出一个
   永远不会被点到的候选（症状 = `池非空、可点格数 0`，`BattleDriver.cs:7935` 已有兜底，**今天不炸**）。
   本次那条 `includeSelf == 0` 的窄口**顺带**把它们挡住了，但那是**巧合不是判据**（`includeSelf` 与 `friendlyUnits` 是两格）。
   正解 = 在 probe 那一趟也把 `spec.Side` 过一遍，或走 §四 的整闸（`friendlyUnits/enemyUnits` 会自然剔掉）。
2. ⚠️ **`EffectResolver.cs:21206-21230` 那段头注释原来有两句已经过期**（「突触那条本轮不筛」/「预览不过闸，所以不算分叉」），
   本轮按铁律 5 **就地改写**（行数不变）。
3. ⚠️ **`B114_WA` 报告 §六·6 指出的所有权表笔误仍没改**（`CardPresentation/Editor/RuleEngineTest.cs` 不存在，
   真身是 `RuleEngine/Editor/RuleEngineTest.cs`）—— 本轮我按真身接线，**表归调度台维护，⛔ 我没动**。
