# B9_E3 · CardDef / UnitState / CardInstance / PlayerState / BoardSlots 切片的 B 行账升级

> 执行代理 `B9_E3` · 2026-10-26 · 第八十三会话「完全复刻」
> 切片来源（自跑，未手抄）：`工具/ledger_slice.py --domain 战斗逻辑驱动 --status B --file {CardDef,UnitState,CardInstance,PlayerState,BoardSlots} --limit 0`
> 产出补丁：`资料/完全复刻/账/_parts/B9_E3.tsv`（31 行；11 列；LF；末行有换行）

---

## ① 结论

| 项 | 数 |
|---|---|
| 切片内 **唯一** B 行（五条命令去重后） | **77** |
| **升 A** | **8** |
| **判 C** | **1** |
| **改判 D** | **13** |
| **仍留 B**（本批只修了落点行号或原样不动） | **55**（其中 9 行改了落点行号） |
| 补丁实际写入行数 | 31 |

机器闸：把本补丁当账表喂给 `工具/check_ledger_points.py` 的 `check_file(..., show_pass=True)` ⇒ **8 条 A 行全部 `OK`**（通过/漂移/失败：通过 8 · 漂移 0 · 失败 0）。⛔ 这只能证明「落点是个像样的声明」，`判据仍要人读` —— 本报告 §② 就是那份人读结论。

### 升 A 的 8 条（落点均已核到方法声明行）

| 原版类.方法 | 我们侧落点 |
|---|---|
| `CardAbility.IsRightContext(CardScript thisCard)` | `RuleEngine/Core/ActionPump.cs:1273（IsRightContext）` |
| `RawCardScript.HasTriggerOnceAbility()` | `RuleEngine/Core/CardDef.cs:2462（AbilityTriggerPresence）` |
| `MinionManager.GetIndexOfCard(CardScript card)` | `RuleEngine/Core/BoardSlots.cs:528（OriginalIndexOfCard）` |
| `MinionManager.GetInteractiveUnitsCount()` | `RuleEngine/Core/BoardSlots.cs:491（InteractiveUnitsCount）` |
| `MinionManager.ReplaceUnit(CardScript originalCard, CardScript newCard)` | `RuleEngine/Core/EffectResolver.cs:3305（DoBecome）` |
| `SupportMethods.ShouldRemnantDestroyOnTurnEnd(CardScript cardScript)` | `RuleEngine/Core/RuleCore.cs:9134（DestroyRemnants）` |
| `CardScript.ResetActions()` | `RuleEngine/Core/RuleCore.cs:14139（RefundForcedAttackAction）` |
| `CardScript.GetInitialDamage(int damage)` | `RuleEngine/Core/TutorialScript.cs:1513（SetupInitialManaAndDamage）` |

### 判 C 的 1 条
- `CardTraitCollection.GetTraitForActiveAbilityRawCard(CardArmy cardArmy)`

### 改判 D 的 13 条
- DeepCopy / ShallowCopy 家族 7 条：`ActiveAbility.DeepCopy` · `ActiveAbility.ShallowCopy` · `CardAbility.DeepCopy` · `CardAbility.ShallowCopy` · `DamageCriteria.DeepCopy` · `SummonCriteria.DeepCopy` · `TargetCriteria.DeepCopy`
- 零调用 4 条：`CardScript.ResolveReactor` · `CardScript.ResolveWeaponKilled` · `CardScript.IsInHandState` · `EntityScript.HasActiveEffect`
- 数据 0 入口 1 条：`CardScript.ReactToCardCreated`
- 死方法 1 条：`PlayerHand.GetMulliganOption`

### 本批顺手更正的三条错记录（铁律 5）
1. 🔴 **`CardScript.GetInitialDamage` 旧记「查不到（无 GetInitialDamage）」是错的** —— `TutorialScript.SetupInitialManaAndDamage`（`TutorialScript.cs:1513`）就是等价实现，数据也早就在（`TutorialStageData.startingPlayerDamage / startingEnemyDamage`），6 关现读全 0、机制照做。
2. 🔴 **`CardScript.ResetActions` 旧记「无等价落点」是错的** —— `RuleCore.RefundForcedAttackAction`（`RuleCore.cs:14139`）的 doc 就是逐条对着这个方法写的（五拍全对上）。
3. 🔴 **`CardAbility.IsRightContext` 旧记「context 20 那档 `+0x228` 掩码语义没解出来」已解** —— `+0x228` = `CardScript.cardState`（`CardStateOptions`），20 档 = `cardState ∈ {inDeck 0, inDeckDrawing 16}`、30 档 = `cardState == inCemetery 6`，与我们的 `Zone.Deck` / `Zone.Discard` 同义。

### 本批修掉的 9 条「落点指向注释/语句行」（状态仍 B，理由不变）
`BattleManager.ResolveRemoveEffect`（:1371→:1369 RevertGrantsFrom）· `BattleManager.ResolveRemoveEffectsFrom`（同上）· `BattleManagerSupport.BroadcastRemoveEffectsFrom`（:1893→:1892 RemoveStatModsFrom）· `CardScript.CheckReplacedAbility`（:233→:235 HasActiveAbility）· `CardScript.ReactToUnitJammed`（:15643→:15717 ResolveJamUnit）· `CardScript.RemoveEffect`（`Enchantment.cs:265→:269` · `UnitState :1247/:1369/:1646`）· `CardScript.ShowDropPodFigures`（`RuleCore.cs:2064-2085` 是 BeginTurn 里的**调用点** → :7670 `OpenDropPod` 声明）· `MinionManager.GetNextSlotToCard`（`EffectResolver.cs:1373` 是 doc → :1382 `ResolveTargets`）· `SupportMethods.GetCardListFromIds`（`CardDatabase.cs:3154` 是一行报错文案 → :3207 `DeckLookup`）。
> 这些行以前「看着有落点」但那个 `文件:行` 是**注释 / 语句 / 字符串行**：一旦有人把它翻成 A，机检会**直接判失败**；写在这里也是为了让下个代理知道「不是我漏了，是这行本来就该修」。

---

## ② 逐条证据（只列 C 与疑点）

### C-1 `CardTraitCollection.GetTraitForActiveAbilityRawCard(CardArmy cardArmy)` ⇒ **C**

**判据（已读全文）**：`decomp_full/CardTraitCollection__GetTraitForActiveAbilityRawCard.c:1-26`，六档按**阵营**推「主动能力对应哪条 trait」：

| 阵营 | trait | 数值 |
|---|---|---|
| 10 | `oath` (0x4fb) | 1275 |
| 80 | `pray` (0x4a1) | 1185 |
| 100 | `duty` (0x4c4) | 1220 |
| 110 | `agenda` (0x4d3) | 1235 |
| 130 | `ferocity` (0x4f1) | 1265 |
| 其余 | `activeAbility` (400) | 400 |

（数值表实核自 `DefinedTrait.cs`。）

**我们侧**：**没有这一层** —— `grep -rn GetTraitForActiveAbility Assets` = 0；我们只是读卡自己的关键词（`CardDef.Keywords`）。

**可观测差**：池内 `hasActiveAbility == 1` 的 **77 张**里 **76 张**自带阵营档那条 trait，**唯一反例 = `SOR66 Armorium Cherub`**（Sororitas 阵营，卡面 trait 只有 `rally` / `flank`）⇒ 原版运行时会给它补 `pray(1185)`，我们不会。

**可达性（这一条是判 C 的关键）**：本函数三个活调用点全在**表现层**，离线可跑：
`CardScript__ActivateIconAnim.c` · `CardScript__ActivateTriggerTraitAnim.c` · `UnitOnBoardAttackTypeSelector__Toggle.c`
（旧记「纯查表、没有运行时可观察量 ⇒ 留 B」不成立 —— 活调用点是图标/动画，不是「没人用」。）

**该改哪儿**：在 `CardDef` 加一条「按阵营推导 activeAbility trait」的查询，并在这三处（我们对应的图标/动画入口）改用它。⛔ **不要只把 SOR66 写进卡表** —— 那会把「原版有推导」降级成「我们手填」，以后每一张同族卡都要再手填一次。

### 疑点 D 族（13 条 D 是**本批的判断**，请调度台复核口径）

判 D 的依据统一是方案 §4.1「死代码 / 数据当前 0 入口 ⇒ D（必须带时间限定）」，不是「影响小」。逐条的 0 命中证据：

| 方法 | 证据 |
|---|---|
| `CVBCardAbility/ActiveAbility/DamageCriteria/SummonCriteria/TargetCriteria` 的 `DeepCopy`/`ShallowCopy`（7 条） | `out_cpp2il_dev_ilspy` 全树对 `.ShallowCopy()` **0 命中**；`.DeepCopy()` 的命中**全部落在各拷贝体内部**（`ActiveAbility.cs:116/:119/:133` · `CardAbility.cs:356/:363/:394` · `AbilityLogic.cs:199/:207/:267` · `BoardCriteria.cs:57` · `HandEffect.cs:84`）；而**两个顶层入口** `ActiveAbility.DeepCopy`(:72) / `CardAbility.DeepCopy`(:311) 全树 **0 调用** ⇒ 整簇传递性死代码。`assets_full` 按字符串 `DeepCopy` / `ShallowCopy` 也 **0 命中**（同目录 `m_MethodName` / `OnClick` 有命中 ⇒ 搜索本身有效）。 |
| `CardScript.ResolveReactor` / `ResolveWeaponKilled` / `IsInHandState` / `EntityScript.HasActiveEffect`（4 条） | `decomp_full` 与 `out_cpp2il_dev_ilspy` **各只有 1 处命中 = 它自己的声明** ⇒ 0 调用方。旁证：`ResolveReactor` 服务的 plasma 货币在本 build 也是死功能（`initialPlasma` 不建模）。 |
| `CardScript.ReactToCardCreated` | **发送者是活的**（`BattleManager__BroadcastCardCreated.c:29 / :41` 两处调它），但池内 1,351 张 `abilities[].trigger == 210` **= 0** ⇒ 今天无监听者。⚠️ 数据一变要**同时**补发送者（我们 `EvtKind` / `WhenEvent` 全树 0 命中）。 |
| `PlayerHand.GetMulliganOption` | `decomp_full`（除自身 .c）与 `out_cpp2il_dev_ilspy`（除 `PlayerHand.cs:1857` 声明）全树 **0 调用**；真档位由换牌协程 `_AddCardsToMulligan_d__41__MoveNext.c:104-117` 按列表下标写（i<pos→20 / pos≤i<pos+e→10 / 否则→0），**30 档不可达**。 |

🔴 **三点必须说清，避免被误读**：
1. **`TargetCriteria.DeepCopy` 是「有调用方但可达性为 0」**，不是「没人引用」。这条特意在补丁里写全，防止下轮只看到 `ActiveAbility.cs:116/:119` 就判回 B/C。
2. **这 13 条判 D 是「原版没有行为可对齐」**，不是「我们不做了」。若 `--fix` / 审查代理认为「传递性死代码不足以判 D」，请直接把这 13 行退回 B（证据都在补丁的「差异」列里，回退不需要重查）。
3. **`CardScript.ResetActions` 的反例同样值得记**：同族的 `chosenAbility == 220` 那条排队路（`AbilityLogic__PlayAbility.c:2632` → `AddResetActions`）**池内 0 条**，所以我们只接了强行攻击那条入口 —— 这条已写进该 A 行的「差异」，**数据若出现要补入口**。

---

## ③ 没查清的部分

1. **`CardAbility.IsRightContext` 的两条 fail-open 差异没改（只记了）**：
   - 未定义 context：原版 `LogWarning` + **return false**（fail-closed），我们 `LogError` + **return true**（fail-open）。
   - `AbilityContextUnknown = -1 ⇒ return true` 是我们自己加的回退契约（原版走「未定义」⇒ false）⇒ 属 🔶 自己设计，**代码里目前没打 🔶 标记**。
   - 今天不可达：池内 `cardContext` 不来自数据（`cards_truth.json` 里根本没有这个键，由 `RuleCore.AbilityContextOfAbilityV14` 派生）⇒ 我把「从数据侧确认有非法值」这件事**没做成**。要改 fail-open 得另立账。
2. **`SupportMethods.ShouldRemnantDestroyOnTurnEnd` 的第二份实现没并**：`ConditionEval.cs:540-542`（`case 340 onlyRemnants`）内联了同一判据。重复规则 = 迟早不一致；本批不改 `.cs`，只记在 A 行的「差异」里。
3. **`MinionManager.GetInteractiveUnitsCount` 的等价物没有接线点**：它的原版唯一消费者 `BoardAnalysis.EnemyHasMoreTroops` / `…AfterPlay` 在本 build 全树 0 调用方 ⇒ 原版那条链是死代码。我们建了 `BoardSlots.InteractiveUnitsCount`，等 AI 打分线（属 E4）接账行 750/751 时才用得上。
4. **`MinionManager.GetIndexOfCard` 的新原语没接探针**：`BoardSlots.OriginalIndexOfCard` **没有调用方**，`ruleprobe` 也读不到它 ⇒ 只能给「推导链 + 棋盘格位」这一级观测量，拿不到「我们侧实测值」。这不是等价性问题，是读数口问题。
5. **`CardScript.ReactToUnitJammed` 仍有一处真缺**：`CancelEffectsFromDisappearingUnit` 我们没有对应口（属 ActionPump / F1 范围），且 jam 由 0 张卡授予 ⇒ 整条链今天休眠。本批**没动它**（留在 B）。
6. **`CardScript.ResetActions` 的 `ActivateMinion(IsPlayerTurn() == isPlayer)` 参数**：我们的 `RecomputeActAndAttack` 是否**无条件**按本侧算（等价于原版传 `isPlayerTurn == isPlayer`）我没逐句核 —— 该行 A 的判据是「五拍结构对上」，第 ⑥ 拍的参数传递形式没复核。
7. **`PlayerHand.GetMulliganOption` 判 D 后，「起手档位」这件事仍然需要一行账**：我建议调度台**单开一行**记「起手档位 = `_AddCardsToMulligan` 协程（20/10/0，30 档不可达）」，否则「起手档位」在全表里就没有落点了。

---

## ④ 交接给下一个代理

**已经闭合、不要再查的**：
- `CardScript.GetInitialDamage` ⇒ `TutorialScript.SetupInitialManaAndDamage`（:1513）。**别再写「查不到」**。
- `CardScript.ResetActions` ⇒ `RuleCore.RefundForcedAttackAction`（:14139）。
- `CardAbility.IsRightContext` 的 `+0x228` = `cardState`。**别再写「掩码语义未解」**。
- DeepCopy / ShallowCopy 簇、`ResolveReactor`、`ResolveWeaponKilled`、`IsInHandState`、`HasActiveEffect`、`GetMulliganOption` 的「0 调用方」证据（见 §②），别重复扫。

**下一手最该做的三件（按价值排序）**：
1. **C-1 `GetTraitForActiveAbilityRawCard`**：在 `CardDef` 加「按阵营推 activeAbility trait」（六档表见 §②），接到我们的图标/动画入口；验收看 `SOR66` 的图标由 76/77 → 77/77。⚠️ 动它之前先 `grep` 表现层那三处对应物，别只改引擎侧。
2. **`CardAbility.IsRightContext` 的 fail-open 两处**：未定义 context 改成 `return false`；`-1` 那条要么删、要么在代码里打 🔶 标记（按铁律 18 必须标「自己设计 + 谁定的 + 日期」）。
3. **`ShouldRemnantDestroyOnTurnEnd` 的第二份实现收口**：把 `ConditionEval.cs:540-542` 改成调 `RuleCore.IsNecronRemnant`（判断 340 `onlyRemnants` 的口径要一起看，别只换一半）。

**仍然留 B、本轮**没**动过、但已知卡在哪的（按难度）**：
- 只差一条断言/读数口：`CardScript.SetRawCardData`（可观察量 = 卡面标题，需先核兜底卡面 `_title` 建不建）· `SupportMethods.ShouldRemnantDestroyOnTurnEnd`（跨回合读数）· `CardScript.SetInBattleId`（卡面那颗 id Text 在 prefab 里到底可见不可见，判据没取）。
- 结构差未消：`CardScript.RemoveTraitSilently` / `TransformIntoRemnant`（我们的 `_keywords` 是**合并字典**，表达不了「只摘卡面那份、保留 `buffType 3` 授予那份」）· `CardScript.IsSameTrait`（我们的 `StatMod` 从不合并）· `CardTraitCollection.GetTraitEffect` / `GetTraitEffects`（原版是全局词条表，我们是逐卡正文）。
- 入口未统一：`BattleManagerSupport.BroadcastRemoveEffectsFrom` · `BattleManager.ResolveRemoveEffectsFrom`（原版是一拍「全员扫」的广播器，我们只有单元级「按来源摘」；将来新增棋盘写入点容易漏摘、**静默**）。
- 逐句没读全：`AbilityLogic.NewBuffStat`（281 行只读到分支头）· `AbilityLogic.GetPos`（331 行只读到分支入口）· `CardScript.CardSetup`（350 行约 60 行）· `CardScript.RemoveEffect`（六档无独立落点、池内各 0）。
- 判据本身没写死：`BattleManager.UpdateManaLeftAccumulation`（「没花完的能量结转」是一次性还是永久 —— `PlayerState.cs:143-146` 已标「我们挑的」）。

**⛔ 别推翻的（本批复核过、仍然成立）**：
- 「原版不镜像敌方槽位」相关的 `BoardSlots` 语义（`Resolve` 的 `NxOf` 注释：不镜像、敌方槽 0 也在画面左边）。
- `Auras.Recompose` / `RefreshInPlayNumericAndKeywords` **不是零副作用** —— 本批没有改动它们，但 `BroadcastRemoveEffectsFrom` 那行的差异里提到「调 Recompose 整份摘再重挂」，下轮若要把广播器接上，必须先用隔离实验证明重挂不改变 `play check` 指纹（先例见 CLAUDE.md 铁律那条 F1/F3 隔离实验）。

**补丁合并注意**（给调度台）：
- 本补丁 31 行键 = `(域, 原版类, 方法)`，已对主账表做过键校验：**missing 0 · 内部重复 0**。
- 9 条「只修落点行号」的 B 行，`差异` 列**原封不动**、只在 `备注` 末尾追加一句说明 —— 合并时不会覆盖掉别轮的结论。
- 本批**没有改任何 `.cs`**、**没跑 Unity**、**没动 git**、**没碰主账表**。
