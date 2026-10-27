# B9_R1DUTY —— `cond=341 canUseDuty` 的 `ex` 语义（报告 · 2026-10-27）

> 判据正本 = `资料/完全复刻/判据/cond341_canUseDuty_ex语义.md`（本报告是摘要，两份内容一致，详细证据在正本）。
> 只读代理：**没改任何 `.cs` / `.tsv` / 资产 / 正本**，没跑 Unity，没动 git。

## 结论

1. **`ex` = 原版 `TargetCriteria.excludeCondition`（`+0x3D`），是 0/1 的「取反位」** ——
   **不是阈值、不是索引、不是「≥N / 恰好 N」**。结构上就没地方放阈值：`UnitConditions` 是裸枚举
   （`dump.cs:20735-20774`），`TargetCriteria` 38 格里只有 `conditionsFilter // 0x38` 与 `excludeCondition // 0x3D`（`dump.cs:23850/23853`）。
2. **怎么参与判定**：出口处 **XOR 取反一次**（`FilterMethods__MeetsConditionCriteria.c:384-389`；ILSpy `FilterMethods.cs:1190-1196`）。
   - `ex=0` ⇒ 保留 `match` 为真 = **主动能力还能用**的单位；
   - `ex=1` ⇒ 保留 `match` 为假 = **主动能力已经用过**的单位；
   - `ex=0` **不等于**「这一格没条件」（`cond=0` 才是 none / 缺行）。
3. **341 的 match 本体 = `!usedActiveAbility(+0x4C)`**（`.c:170-174`）。
   **我们这一层没错**：`ConditionEval.cs:548-550` 返回未取反的 `!u.DutyUsed`，取反统一在 `Meets`（`:169-175`），
   四个消费口也都把 `Exclude` 传进来了（`CardCriteria`/`EffectResolver`/`TargetCriteriaEval`/`AiTargets`）。
   ⇒ **⛔ 不要改 `ConditionEval` 的 ex/取反**（维持第四批 `E5ENG` §E-(a) 的结论）。
4. **但查出两条真偏离（都不是 ex 本身）**：
   - **A（决定性）**：原版 `CheckIfMeetsCriteria` **先按 `targetsAffected` 短路**（`0/20/30/40/70/80/200/201/235` 走不到第 ⑧ 步，
     `.c:57-157`）；`BL64` 那条 cond=341/ex=1 的 criteria 是 **`ta=30`** ⇒ 在原版是**死字段**。
     我们的 `ApplyStructuredConditionFilter`（`EffectResolver.cs:2348-2377`，经 `PlayTargetPool:2091` / `ResolveTargets:1703`）
     **不看 ta**，无条件套用 ⇒ **`BL64` 的目标池被死字段筛空**（`ErrNoTargetAvailable` 的真因）。
     原版 `BL64` 的合法目标来自卡级 `cardTargetCriteria = {ta:10, friendly, damaged(40), ex=0}`（`RawCardScript__IsValidTarget.c:44` + SO 真值）——
     卡面「使一支受伤的友方单位自行攻击」一致。
   - **B**：`+0x4c usedActiveAbility` 在**任何主动能力发动**（duty/oath/ferocity/agenda 同一链）时置 1，且**不随回合重置**
     （`CardScript__OnTurnEnd.c:209-210` 只清 `+0x48/+0x50`；清零只有初始化/变形/装填 duty 三处）。
     我们只在 `keyword == duty` 时置 `DutyUsed`（`RuleCore.cs:11960`），另有一个**每回合重置**的
     `UsedActiveAbilityThisTurn`（`UnitState.cs:760/1809`）被注释误标成 `+0x4c` ⇒ **池内 40 张 oath/agenda/ferocity 单位漏记**。

## 证据（文件:行号）

正本 §二 有 24 条完整表；关键几条：
- `d:/2/tools/decomp_full/FilterMethods__MeetsConditionCriteria.c:170-174`（341 = `+0x4c == 0`）· `:384-389`（ex 取反）
- `d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c:298-303`（传 `+0x38`/`+0x3d`）· `:76-91`（**ta=30 短路**）
- `d:/2/tools/il2cpp_out/dump.cs:20735-20774`（枚举，`canUseDuty=341` 在 `:20772`）· `:23823-23881`（TargetCriteria 字段，`:23850`/`:23853`）· `:21979`（`+0x4C usedActiveAbility`）
- `d:/2/tools/decomp_full/CardScript__ResolveActiveAbilityPlayed.c:23-30`（闸 `hasActiveAbility(+0x220)` + `+0x4c = 1`）· `CardScript__OnTurnEnd.c:209-210`（**不清** `+0x4c`）· `BattleManager__ResolveResetDuty.c:6`（`+0x4c = 0`）
- `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:1086-1095`（建池后逐候选 `CheckIfMeetsCriteria`）
- `d:/2/tools/decomp_full/RawCardScript__IsValidTarget.c:44`（战术卡合法目标 = 卡级 `cardTargetCriteria`，`+0x280`）
- 我们：`Assets/RuleEngine/Core/ConditionEval.cs:169-175 / 545-550` · `Core/TargetCriteriaEval.cs:117-121`（ta=30 同形）· `Core/EffectResolver.cs:2348-2377 / 2091 / 1703` · `Core/EffectText.cs:1698-1835`（贴 cond 无 ta 闸）· `Core/RuleCore.cs:11533/11737/11960-11963` · `Core/UnitState.cs:327/748-760/1807-1809/2696` · `Data/AiTargets.cs:634`
- 数据：`Assets/RuleEngine/Resources/{cards_engine,ai_scoring}.json`；原始 SO 真值 `资料/普查产出_1018/allcards_1350/cards_raw.json`（bundle = `D:/2/关服前存档/.../ea8bca4f…/__data`，1,351 张）

## 没查清的部分

1. ferocity / agenda 是否 100% 走 `_ResolvePlayActiveAbility_d__479`（正面证据 = `UsedActiveAbility.c:37/60` 有这两族分支、`CardScript.cs:6678` 把 ferocity/oath 设 active 档；但我没逐行核它们的 action 准入点，`:46730` 那道闸只写 duty/oath）。
2. 卡级 `cardTargetCriteria`（`role=cardtarget`）在我们引擎里**没有消费者**（只有角色白名单 + 钉表自检命中）—— 原版那一层我们用什么顶、覆盖多少张，**没查清**（属另一条账）。
3. 装填 duty 的作用域：原版 `ResolveResetDuty` 对传入卡无条件清 `+0x4c`；「它是否也会清 oath/ferocity 单位的 `+0x4c`」取决于建参点，我没追。

## 该怎么改

- **A（必修）**：在 `EffectResolver.ApplyTargetFilters(... structuredCondition)` 调 `ApplyStructuredConditionFilter` 之前加 **ta 闸**：
  `ta ∈ {0,20,30,40,70,80,200,201,235}` ⇒ 不套 cond/ex（并出声一次）。短路集合与 `TargetCriteriaEval` 那几档共用一份常量（⛔ 别写第二份）。作用面 **18 行 / 16 张卡**（含 `BL64`、`BL50`）。
- **B（必修）**：`UnitState` **类尾**追加 `ActiveAbilityUsed`（= 原版 `+0x4c`，本局一次、不随回合重置）；
  置位 = `RuleCore.UseAbilityCore:11533` / `UseOathAbilityCore:11737` / `UseAlternativeCore:11960`（duty/ferocity/agenda，**pray 不置**）；
  清零 = 装填 duty（`EffectResolver.cs:15039-15040` 与 `DutyUsed=false` 同行附近）+ `ResetToValuesInHand`（`UnitState.cs:2696`）；
  **不要**在 `RefreshForNewTurn:1807` 清；`ConditionEval.cs:548-550` 改读这一位；`UnitState.cs:748-760` 的错注释就地订正（铁律 5）。
- **⛔ 不该做**：把 `BL64` 的 `ex` 读成 0；把 `ex` 当阈值；在 `ConditionEval` 里再取反一次。

## 影响面（含可复现脚本）

| 面 | 量 |
|---|---|
| 池内 `cond=341` | **4 张卡 / 4 行**（`ex≠0` 的 **3** 张：`Cadian Honour`(nontarget,1) `AM47`(nontarget,1) `BL64`(target,1)；`UM42`(targetscore,0)） |
| 其中真影响出牌目标池的 | **只有 `BL64`**（另 3 张是 AI 打分；`nontarget`/`targetscore` 这两行在 `cards_engine.json` 里**无引擎消费者**，AI 读 `ai_scoring.json`） |
| 会短路掉 cond 的 ta（改动 A 面） | **18 行 / 16 张卡** |
| 替代行动单位（改动 B 面） | **86 张**（duty 23 · oath 18 · agenda 13 · ferocity 9 · pray 23）；会置 `+0x4c` 的 **63 张**，我们只覆盖 23 ⇒ **40 张漏记** |
| 原始 SO 上游疑点 | **已闭合**：`BL64` 的 `excludeCondition=1` 是原版真值，**不是抽取错误** |

脚本（正本 §五 有完整四条，口径 = `cards_engine.json` 单份，无 `full_raw`）：

```bash
# ① 池内 cond=341
python -I -X utf8 -c "
import json,io
d=json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json','r',encoding='utf-8'))
rows=[(c['id'],c['name'],r['role'],r['cond'],r.get('ex')) for c in d['cards'] for r in (c.get('unitConditions') or []) if r.get('cond')==341]
print('cards',len(set(x[0] for x in rows)),'rows',len(rows),'ex!=0',sum(1 for x in rows if x[4]))
for r in rows: print(r)
"

# ② 落在短路 ta 上的 cond 行（改动 A 的面）
python -I -X utf8 -c "
import json,io
d=json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json','r',encoding='utf-8'))
DEAD={0,20,30,40,70,80,200,201,235}; out=[]
for c in d['cards']:
    ta={(e.get('ab'),e.get('lg')):((e.get('criteria') or {}).get('targetsAffected')) for e in (c.get('abilityTargetCriteria') or [])}
    for r in (c.get('unitConditions') or []):
        if r.get('role')!='target' or not r.get('cond'): continue
        t=ta.get((r.get('ab'),r.get('lg')))
        if t in DEAD: out.append((c['id'],c['name'],r.get('cond'),r.get('ex'),t))
print('rows',len(out),'cards',len(set(x[0] for x in out)))
"

# ③ 替代行动单位
python -I -X utf8 -c "
import json,io,collections
d=json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json','r',encoding='utf-8'))
cnt=collections.Counter()
for c in d['cards']:
    if c.get('type') not in ('unit','hero'): continue
    ks=[k.lower() for k in (c.get('keywords') or [])]
    for w in ('duty','oath','ferocity','agenda','pray'):
        if any(x==w or x.startswith(w+':') or x.startswith(w+' ') for x in ks): cnt[w]+=1
print(dict(cnt),'sum',sum(cnt.values()))
"
```

## 顺手发现（⛔ 未改，交调度台分流）

- `UnitState.cs:748-760` 的 doc 有两句错（`+0x4c` 被写成「回合末清零」、且被等同于 `UsedActiveAbilityThisTurn`）—— 铁律 5 素材。
- `判据/主档夹具缺口_评估.md:131-132` 那句「(a) 修 `cond=341` 的 `ex` 语义」应按本件作废（方向 = ta 闸 + 状态位）。
- `BL50 Execution` 与 `BL64` 同病（`ta=30` + `cond=40 damaged`），说明这不是「341 特有的怪数据」，而是 **ta 闸缺失**这一类。
