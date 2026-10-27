# B9_W50ENGT —— 给「`ta` 短路闸」＋「`+0x4C` 主动能力已用」补仓库内断言 · 执行报告（2026-10-27）

> 任务：这两条引擎改动（`B9_W27ENG` 落地）**只有仓库外夹具证明过，仓库内 0 断言**。
> 本件把断言写进 `Assets/RuleEngine/Editor/RuleEngineTest.cs`（**只读被测实现**）。
> 判据不在这里抄第二份 → `资料/完全复刻/报告/B9_W27ENG_ta短路与主动能力已用.md` §A/§B/§「正面证明」
> ＋ 判据正本 `资料/完全复刻/判据/cond341_canUseDuty_ex语义.md`。

---

## 一、结论

**做完了**：新增 **36 条断言**（`CheckTrue` 26 · `Check` 7 · `CheckCode` 3，其中 **8 条是「（前提）」硬检查**）
—— `RuleEngineTest.cs` 的 **E4 组**，行 **38540–38818**，覆盖任务单 7 项：

| # | 任务单要求 | 落点 | 条数 |
|---|---|---|---|
| 1 | `cond=341` 只跟 `ActiveAbilityUsed` 走（含「只清它而 `DutyUsed` 仍真」/「反之亦然」） | (D1) `:38561` | **6** |
| 2 | 不随回合重置 | (D2) `:38606` | **5**（单测 3 + **真推进一整个来回** 2） |
| 3 | 清零两处（装填 duty / `ResetToValuesInHand`） | (D3) `:38637` | **6** |
| 4 | `pray` **不置** | (D4) `:38669` | **8**（含「正文确实结算了」前提 + `agenda` 对偶） |
| 5 | `ta ∈ 短路集合` ⇒ 结构化条件不参与筛（正例池不空 / 反例池被筛空）+ `ta=30` 语义说清 | (D5) `:38707` | **8** |
| 6 | 「判不了就不闸 + 出声」 | (D6) `:38766` | **3**（⚠️ 见 §三·1：任务单括注与实现口径相反，按**实现/报告**断） |
| 7 | 两条灭自证 | (D1)/(D2)/(D4)/(D5) | ✅ **三对**（对偶 ①②③④ —— 要求的是 2 对，给了 4 个读数点 3 对） |

**读数**：
- `TMPDIR=/tmp/wf_w50 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完第一次 0/0，补空行后复跑仍 0/0）。
- `工具/check_patch_conflicts.py` ⇒ ✅ **没有跨补丁重复键**（2 份补丁）。
- `工具/merge_ledger_parts.py --dry` ⇒ 更新 9 · 新增 0 · 无变化 0，**0 条被拒**（我的 2 行 + `B9_W49BDR3` 的 7 行）。
- ⛔ **没跑任何 Unity**（`RuleEngineTest.Run` 由调度台在同步点跑）⇒ **红绿待实跑**。

**行号漂移 = 0**：新代码**全部挂在文件最后一个方法 `TestB9EngForcedAttackAndShuffleGate` 的尾部**
（`:38392` 起，方法体最后一句在 `:38538`）⇒ 账里对 `RuleEngineTest.cs` 的**所有既有行号引用一个都没动**，
只有「方法右花括号 / 类右花括号」从 `:38539-38540` 挪到 `:38818-38819`（账不可能引用这两行）。

**为什么没新增 `Test` 开头的方法**：`RuleEngineTest_Diag.cs` 的 `VerifyDiagCoverage()` ② 比的是
「反射看到的用例方法」↔「**生成物里的 `RunCaseNames`**」，而那一份要跑 `工具/gen_diag_runner.py` 重生成
（不在本代理白名单）。挂在**已注册**的用例尾部 ⇒ `Step(...)`/`VerifyRunCaseCoverage` 全都不用动，
也不需要重生成产物。⚠️ 若调度台更希望它是一条独立用例，那要**同时**重生成 diag 产物。

---

## 二、证据（文件:行号）

### 1. 断言落点（全部在 `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`）

| 组 | 行 | 断的是什么 |
|---|---|---|
| 头注 + `Section` | `:38540-38559` | 判据指针（⛔ 不抄第二份）· 明写「挂在方法尾是为了零行号漂移」 |
| **(D1)** `cond=341` 状态位 | `:38561-38604` | 三个「用过」位互相可分 + 灭自证对偶 ① |
| **(D2)** 不随回合重置 | `:38606-38635` | `RefreshForNewTurn` 清按回合位 / 不清 `+0x4c`（对偶 ②）+ 真推一整个来回 |
| **(D3)** 清零两处 | `:38637-38667` | 装填 duty / `ResetToValuesInHand` |
| **(D4)** `pray` 不置 | `:38669-38705` | `pray` 不置（正文结算作前提）+ `agenda` 置（对偶 ③） |
| **(D5)** `ta` 短路闸 | `:38707-38764` | `ta=30` 不筛 / `ta=10` 照筛（对偶 ④）· `ta=30` 语义 = 「候选 == targetCard」 |
| **(D6)** 判不了两档 | `:38766-38818` | `ta` 读不到 ⇒ 闸不生效、照旧筛 + 出声 · `cond` 判不了 ⇒ 整条不筛 |

### 2. 被测实现（⛔ 一个字节没动，只读）

| # | 位置 | 读它证明什么 |
|---|---|---|
| 1 | `Core/TargetCriteriaEval.cs:1190/1194/1201-1209` | `EarlyReturnTargetsAffected` / `NeverFilteredTargetsAffected` / `ConditionIsDeadByTargetsAffected` —— 短路集合**唯一判据** |
| 2 | `Core/EffectResolver.cs:16704-16727` | `CondDeadByTa`：`Unknown` ⇒ **返 false（不跳过）+ 出声**；命中集合 ⇒ 返 true（跳过） |
| 3 | `Core/EffectResolver.cs:2354-2360` | `ApplyStructuredConditionFilter` 第一句之后的那道闸 |
| 4 | `Core/EffectResolver.cs:1703` / `:2030` / `:2094` / `:21156`（`AdjacentNeighborCounts`） | 四个消费点把 `ta` 传下去（高亮与结算读同一条 op） |
| 5 | `Core/ConditionEval.cs:554-556` | `case 341: return !u.ActiveAbilityUsed;`（未取反，取反仍只在 `Meets` 做一次） |
| 6 | `Core/UnitState.cs:1782-1825`（`RefreshForNewTurn`） | **只清** `UsedActiveAbilityThisTurn`（`:1822`），**不碰** `ActiveAbilityUsed` |
| 7 | `Core/UnitState.cs:2712` | `ResetToValuesInHand`：`UsedActiveAbilityThisTurn = ActiveAbilityUsed = false;` |
| 8 | `Core/EffectResolver.cs:15103` | `DoReloadDuty`：**无条件**清 `ActiveAbilityUsed`（在 `DutyUsed` 的 `continue` **之前**） |
| 9 | `Core/RuleCore.cs:11972` | `u.ActiveAbilityUsed \|= keyword != KeywordTable.Pray;`（`pray` 不置） |
| 10 | `Core/EffectResolver.cs:13412-13421` | `_structuredConditionNotices` 去重表 + `StructuredConditionNotice`（(D6) 读它当「出声」的口） |
| 11 | `Core/EffectResolver.cs:2930-2944`（`AddSide`） | `troopOnly && u.IsWarlord ⇒ skip` —— (D5) 的池子只有我那三个友方（督军不算） |
| 12 | `Core/EffectResolver.cs:1616-1619` | 「无主语 + 点了目标」那一支**只在 `SubjectTargetsAffected ∈ {30,220}` 才收 `chosen`** ⇒ 我的合成 op（该字段 0）**不走那一支**，池子不受它影响 |

### 3. 逐条断言（断什么 / 改坏哪处会红 / 是灭自证吗）

> `★` = 关键判别式；`🧨` = 改坏哪处会红（写进了断言文案里，日志里能直接看到）。

**(D1) `cond=341`（6 条）**
1. 前提：三个位全假（硬检查，⛔ 不是 `if`）
2. 全假 ⇒ 341 真
3. ★ 只置 `DutyUsed` ⇒ 341 **仍真**｜🧨 341 改回读 `DutyUsed`（改前那版）⇒ 红
4. ★ 只置 `ActiveAbilityUsed` ⇒ 341 **假**｜🧨 341 不读这一位 ⇒ 红
5. ★ 只置 `UsedActiveAbilityThisTurn` ⇒ 341 **仍真**｜🧨 读成按回合位 ⇒ 红（`canUseDuty` 会每回合复活）
6. **灭自证对偶 ①**：交错两态「Active 真/Duty 假 ⇒ 假」∧「Active 假/Duty 真 ⇒ 真」——
   只读其中一个字段的实现**不可能两条都过**

**(D2) 不随回合重置（3 条）**
7. 回合开始：`UsedActiveAbilityThisTurn` **被清**
8. ★ `+0x4c` **没被清**｜🧨 在 `RefreshForNewTurn` 里补一句 `ActiveAbilityUsed = false` ⇒ 红；
   🧨 反过来删掉第 7 条的清 ⇒ 第 7 条红 —— **灭自证对偶 ②**（同一个改动不可能把两条一起弄绿）
9. 配套：`DutyUsed` 也不随回合清（与既有 `TestAlternativeActions` ① 的 `ErrDutyUsed` 一致）
   + **端到端**：`PassTurn`×2 真的走完一整个来回后 `+0x4c` 仍真

**(D3) 清零两处（4 条）**
10. 前提：装填前两位都真
11. `CheckCode(PlayTactic, OK)` —— 真打出 `Reload the Duty abilities of all your units`
12. 既有口径：`DutyUsed` 被复位
13. ★ `+0x4c` 也被清｜🧨 把 `DoReloadDuty` 那一句挪到 `DutyUsed` 的 `continue` **之后** ⇒ 红
14. ★ `ResetToValuesInHand` 之后 `+0x4c` 假｜🧨 只清 `+0x50` 那一族 ⇒ 红

**(D4) `pray` 不置（4 条）**
15. 前提：祈祷前 `+0x4c` 假
16. 前提·硬检查：祈祷正文**真的结算了**（`Health 9 → 10`）—— ⛔ 否则第 18 条是「压根没发动」的假绿
17. ★ `pray` 之后 `+0x4c` **仍假**｜🧨 `|= keyword != Pray` 改成无条件 `= true` ⇒ 红
18. 配套：按回合那一位**照样置真**（两位口径不同，才说明 `|=` 那句不是白写）
19. ★ **灭自证对偶 ③**：`agenda` 走同一入口 ⇒ `+0x4c` **置真**｜🧨 「一律置真」让第 17 条红、「一律不置」让本条红

**(D5) `ta` 短路闸（7 条）**
20. 前提：A/B 已受伤、C 满血（`cond=40` 的判别式）
21. ★ `ta=30` + `cond=40` ⇒ **池 3**（不筛）｜🧨 删掉 `CondDeadByTa` 那句（= 改前）⇒ 2
22. ★ 反例 `ta=10` ⇒ **池 2**（照筛）｜🧨 闸写成「无条件跳过」⇒ 3 —— **灭自证对偶 ④**（与 21 同一次改动不可能双绿）
23. 而且筛掉的**正是满血那个**
24. ★ `ta=30` + `cond=244`（都不带甲）⇒ **池 3**（没被筛空）｜🧨 漏闸 ⇒ 0
25. ★ 同一条 `cond=244` + `ta=10` ⇒ **池 0**｜🧨 把 `ConditionIsDeadByTargetsAffected` 改成恒真 ⇒ 3
26. ★ `ta=30` 在 `TargetCriteriaEval` 里 = **「候选 == targetCard」**（点着 A ⇒ A 真 / B 假）
27. ★ 而**没点选目标时它判不了（`null`）**⇒ 明写「`gated==3` 里还叠着 `ApplyTargetCriteriaFilter` 遇 null 不筛这一层」
    （⛔ 别把 21 单独读成「36 格也过了」）

**(D6) 判不了两档（3 条）**
28. ★ `ta` **读不到**（`op == null` ⇒ `Unknown`）⇒ 闸**不生效、照旧筛** ⇒ `cond=244` 把池筛成 **0**｜🧨 把 `Unknown` 当死字段 ⇒ 2
29. ★ 而且**出声**：去重表里出现 `cond-ta-unknown|` 前缀的键｜🧨 删掉那句 `StructuredConditionNotice` ⇒ 红；
    该字段改名 ⇒ `NoticeCount` 返 -1 ⇒ **也红**（⛔ 不会静默变绿）
30. ★ `cond` **本身**判不了（110 缺施法者）⇒ **整条不筛** ⇒ 池 2｜🧨 判不了时「全排除」⇒ 0；
    ⚠️ 断言文案明写「必须与第 28 条 `noTa == 0` **对读**」

> 合计 **36** 条（`CheckTrue` 26 · `Check` 7 · `CheckCode` 3；上表按**语义**归成 30 个读数点，
> 其中若干条同时含「前提硬检查」+「结论」两句，所以条数比读数点多）。
> 复跑计数：`sed -n '38540,38818p' … | grep -o "CheckTrue(\|CheckCode(\|Check(" | wc -l` ⇒ **36**。

---

## 三、没查清的部分（⛔ 不猜、没有拿猜测填空）

1. 🔴 **任务单第 6 项的括注与实现口径相反 —— 我按【实现 + `B9_W27ENG` 报告】断的，请调度台裁**：
   - 任务单写「`ta` 读不到时**不闸**（**池不空**）」；
   - 实现（`EffectResolver.cs:16707-16716`）与 `B9_W27ENG` 报告 §A/§三·2 都写 **「判不了 ⇒ 不闸 + 出声」= 「不加这道闸 ⇒ 照旧筛」**，
     即 `CondDeadByTa` 对 `Unknown` **返 false**，`ApplyStructuredConditionFilter` **继续筛** ⇒ 池**会被筛空**；
     代码自己的告警文案也写着「**这一条照旧筛**目标池（= 维持旧行为）」。
   - ⇒ 我断的是 **「照旧筛 ⇒ 池 0」**（第 28 条），并在文案里写清「⛔ 不是放行整个池子」。
     **「池不空」那种读数是另一档**（`cond` 本身判不了），我另外补了第 30 条来覆盖它。
     **两条对读**才是这件事的全貌。
2. **「出声」只证明到「去重表被写」**：`StructuredConditionNotice` 是 `UnityEngine.Debug.LogWarning`，
   自检里没有捕获控制台的机制（本文件无 `Application.logMessageReceived` 先例）。
   ⇒ 第 29 条断的是「**那条语句被执行了**」，⛔ 不等于「控制台真有一行」。要更强只能新引一个日志捕获工具（会动全局）。
3. **红绿待实跑**：本件**不能跑 Unity**（红线 1）。所有期望值都是「按判据推出来的」，没有一条是实跑观察到的。
   最可能出问题的三处（**如实列出**，不是预言）：
   - 第 21/22/24/25 条依赖「`ProbeBattle` 的己方棋盘上除督军外只有我摆的那三个单位」
     （依据：`AddSide` 的 `troopOnly` 跳过督军 `EffectResolver.cs:2937`；`ProbeBattle` 只放督军）；
   - 第 11 条依赖 `PlayTactic(..., targetSlot:-1)` 对「all your units」那张卡返回 `OK`
     （依据：同文件既有用例 `:1541-1553` 走的就是这条，且它现在绿）；
   - 第 9 条端到端依赖 `RuleCore.BeginTurn` 真会调 `RefreshForNewTurn`（依据：`UnitState.cs:1804` 的注释写明「本方法只被 BeginTurn 对 p.Board 调」）。
4. **`Section(...)` 在用例体内**只影响日志分组，不影响 `VerifyRunCaseCoverage`
   （它按**反射 + `Step` 记实口**对账）—— 但**我没有实跑验证过**这一点，只是按代码推的。

---

## 四、改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | 在**最后一个方法** `TestB9EngForcedAttackAndShuffleGate` 尾部追加 E4 组（头注 + `Section` + (D1)–(D6)） | **+280**（`38540-38819`，**零漂移**：所有既有行号不动） |
| `资料/完全复刻/账/_parts/B9_W50ENGT.tsv` | 账补丁 **2 行**（整行复制主表 → 只在末列 `备注` 追加「断言已补」） | 新建 7,991 B |
| `资料/完全复刻/账/_parts/_gen_B9_W50ENGT.py` | 生成上面那份补丁的脚本（保留，便于复跑/取证） | 新建 |
| `资料/完全复刻/报告/B9_W50ENGT_引擎断言.md` | 本报告 | 新建 |

**⛔ 没有动**：`EffectResolver.cs` / `ConditionEval.cs` / `UnitState.cs` / `RuleCore.cs` / `TargetCriteriaEval.cs`
（被测实现一律只读）· `CLAUDE.md` / `项目任务.md` / 任务总表 / 账主表 / 基线文件 / 别人的报告 / 任何 `*.unity` / git。

**账补丁为什么不含第三行（`EntityScript.set_usedActiveAbility(bool value)`）**：
`check_patch_conflicts.py` 实测它与并行的 **`B9_W49BDR3.tsv`（行 2，同键）** 冲突，而按文件名排序**我的会赢** ⇒
我那份「整行副本」会把 W49 尚未合并的徽标接线内容（`!u.ActiveAbilityUsed`）**冲掉**。
⇒ 取证后**让给 W49**。这一条注记请调度台**在合完 `B9_W49BDR3` 之后**人工追加到该行末尾（原文如下，一字不改即可）：

```
｜✅ 2026-10-27（`B9_W50ENGT`）：**清零两处的断言已补** —— E4 组 (D3)：装填 duty（`Reload the Duty abilities of all your units`）之后 `ActiveAbilityUsed` 为假；`ResetToValuesInHand` 之后为假。判别式：把 `DoReloadDuty` 里那一句挪到 `DutyUsed` 的 `continue` **之后** ⇒ 第一条红；`ResetToValuesInHand` 只清 `+0x50` 那一族 ⇒ 第二条红。
```

**复跑命令**：
```bash
TMPDIR=/tmp/wf_w50 bash d:/4/Unity/工具/typecheck.sh          # 0 / 0
grep -c "Check(\|CheckTrue(\|CheckCode(" MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs   # 全文件计数（含既有）
sed -n '38540,38818p' MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs | grep -o "CheckTrue(\|CheckCode(\|Check(" | wc -l   # 本组 36 条
python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry
```
同步点上要跑的宿主：**`RuleEngineTest.Run`**（只动了 `Editor/RuleEngineTest.cs` + 账补丁 + 报告，
按铁律 12 的「只动一个宿主 ⇒ 只跑那一条」）。

---

## 五、顺手发现（⛔ 均未改，交调度台分流）

1. **`B9_W27ENG` 报告 §六·1 那条「假红」已经不存在了**：报告写 `RuleEngineTest.cs:37253` 钉在 `u.DutyUsed = true` 上、
   同步点前要先改。**现读 `:37253` 已是 `u.ActiveAbilityUsed = true;`**（旁边还留着 2026-10-27 的订正注释）
   ⇒ 那一条**不用再改**，也不会假红。建议报告 owner 把 §六·1 标成已消（铁律 5：就地订正）。
2. **`B9_W27ENG` §六·2 的「`Duty` 徽标读错位」已由 `B9_W49BDR3` 接掉**（它的补丁行 2 写着
   `BattleDriver.cs` 的 `KeywordActive` 改成 `!u.ActiveAbilityUsed`）⇒ 那一条也不用再分派。
   ⚠️ 但**它的断言在哪**我没核（不在本件白名单）—— 建议调度台问一句「徽标那格有没有配断言」。
3. **`RuleEngineTest.cs` 的「用例方法」口径很脆**：新增一条 `Test*` 方法就必须**同时**重生成
   `RuleEngineTest_Diag.cs` 的 `RunCaseNames`（否则 `VerifyDiagCoverage` ② 报 `missing 1`，而 `Run()` 那边是绿的）
   —— 这一次我用「挂到已注册用例尾部」绕开了，但**下一个写手仍会撞**。
   建议把这条写进 `资料/工作准则_操作经验.md`（或 `gen_diag_runner.py` 的用法注释里已有？我没核）。
4. **`RuleEngineTest.cs` 现在 38,819 行 / 2.78 MB**，单文件已接近「改一次就要全量重编」的体感上限。
   若后续还要挂断言，建议**按 `RuleEngineTest_<主题>.cs` 分册**（同 `partial class`，已有 30+ 分册先例），
   但**分册要连带重生成 diag 产物**（同上第 3 条）。
