# B141 · W59 · 账 L29 `AI.ScoreFromManaBuff` ⑥⑦ —— `activeAbility.manaCost` 判据列

> 执行代理 `B141-W59`（第一百二十八会话 · W128 波次）· 2026-11-04
> 施工单来源：`报告/B141_CHK05_分域与首批定性.md` L29
> 涉及文件：`RuleEngine/Data/AiScoring.cs`（唯一改动）· `工具/gen_cards_engine.py`（**未改**，理由见 §3）·
> `RuleEngine/Resources/cards_engine.json`（**未改**，`--check` 逐字节一致）

---

## 1. 一句话结论

**施工单的前提（「`activeAbility.manaCost` 未进 `cards_engine.json`」）已过期** —— 该列自卡表 **v37** 起就在，
`CardDef.ActiveAbilityManaCost` 与产品钩子 `AiScoring.ActiveAbilityManaCost` 也早已接线（2026-10-29 `B98_K6`）。
逐张核对：池内 **1,129 张有真值的卡 = 0 处差异**（含那 143 张里落在池内的 132 张，0 处差异）。

**但 ⑥⑦ 当时确实不对 —— 病根不是「缺列」，是「三态闸判反了」**：
`AbilityManaCostInRange` 写的是 `HasActiveAbility == 0 ⇒ 跳过`，而**卡表为 0 时根本不落这个键**
（生成器 `if v != 0` 才写）⇒ 产品数据里 `HasActiveAbility` 只可能是 `1` 或 `-1`，**那道闸一次都没触发过**。
后果：**34 张 `manaCost=2` 的督军**（原版 `hasActiveAbility=0`、本该整段跳过）在 ⑥ 上**各多 +1**。
已改成 `!= 1 ⇒ false`（= 原版对字节真值的 `!= 0`，未知档不猜），并补 7 条断言。

---

## 2. 原版判据（逐行读过）

`d:/2/tools/decomp_full/AI__ScoreFromManaBuff.c:15-97` · 交叉源 `out_cpp2il_dev_ilspy/AI.cs:2052-2110`（两份一致）

```
iVar2 = BoardAnalysis.GetTotalMana(manager, thisCard.isPlayer, 0)        // :22  总能量
iVar3 = BoardAnalysis.GetManaLeft(manager, thisCard.isPlayer, 0)         // :23  剩余能量
iVar8 = criteria.buffEffect.costChange(+0x68) + iVar3                    // :25  limit
if (iVar2 < iVar8) return 0.0                                            // :26-28  早退
⑤ 手牌：CurrentCost > manaLeft && <= limit          ⇒ +1 每张           // :35-47（**无** any 闸）
⑥ 督军：hasActiveAbility(+0x220) != 0
         && activeAbility.manaCost(+0x14) > manaLeft && <= limit ⇒ +1   // :48-59
⑦ 同侧 minions+remnants：同一条判据（逐张 +1）                          // :61-91
```

三条关键事实（这次现核）：

| 事实 | 判据 |
|---|---|
| `activeAbility` 取的是**卡自己**那一份 —— `GetActiveAbility` 的 `activeAbilityRef(+0x118)` 分支**全库不命中** | 真值 1,351/1,351 张 `full_raw.activeAbilityRef` **键都不存在** ⇒ 恒走「用卡自己」⇒ 我们读 `trow["activeAbility"]` 就是原版读的那一份 |
| 闸是**字节真值**（`!= 0`），不是 `== 0 ⇒ 跳过` | `.c:48` / `.c:70` 两处都是 `if (*(char *)(x + 0x220) != '\0')` |
| ⑦ 含**残骸**（`GetPlayerMinionsAndRemnantInPlay`） | 我们的 ⑦ 遍历 `ctx.Players[p].Board` 且**不过滤存活** ⇒ 残骸（`UnitState.IsRemnant`）仍在盘上 ⇒ 覆盖到 |

---

## 3. 逐张核对（施工单第 4 条）

命令（可复跑，纯只读）：

```bash
cd d:/4/Unity && python -I -X utf8 - <<'PY'
import json, io, collections
eng = json.load(io.open('MyGame/Assets/RuleEngine/Resources/cards_engine.json', encoding='utf-8'))
tr  = json.load(io.open('资料/普查产出_1018/allcards_1350/cards_raw.json', encoding='utf-8'))
truth = {c['uniqueId']: c for c in tr['cards']}
diff, miss, ok = [], [], 0
for c in eng['cards']:
    t = truth.get(c['id'])
    if t is None: miss.append(c['id']); continue
    want = t['full_raw']['activeAbility']['manaCost']
    if c.get('activeAbilityManaCost') != want: diff.append((c['id'], c.get('activeAbilityManaCost'), want))
    else: ok += 1
print('matched', ok, 'no-truth', len(miss), 'mismatch', len(diff))
PY
```

结果：

| 口径 | 数 |
|---|---|
| 池内卡 | **1,142** |
| 按 `id == uniqueId` 命中真值行 | **1,129** |
| **逐张值不一致** | **0** |
| 无真值行（`TRANSFORM_*` 13 张「Warlord Ascension」，`activeAbilityManaCost` 落 0） | 13 |
| 池内 `activeAbilityManaCost` 分布 | `0` = 1,108 · `2` = 34 |

施工单说的「143 张」：口径 = **真值里 `activeAbility.activeAbilityLogic` 非空**的卡（不是「有 manaCost 的卡」）。
现核：真值全量 **143** 张，其中**落在池内 132 张**（另 11 张不在池）⇒ **132/132 逐张 = 0 差异**，
其 `manaCost` 分布 = `0` : 98 · `2` : 34。
（真值全量 `manaCost` 分布 = `0` : 1,307 · `2` : 44 —— 池外那 10 张 `2` 就是 44−34。）

---

## 4. 真正的缺口 = 三态闸（本次唯一代码改动）

### 4.1 现读事实

| 集合 | 池内张数 | `activeAbilityManaCost` | 真值 `hasActiveAbility` | 卡表落键 |
|---|---|---|---|---|
| A | **87**（`hasActiveAbility=1`） | **全 0** | 1 | `hasActiveAbility: 1` |
| B | **34**（`manaCost=2`，**全部 `cardType=10` = 督军**） | 2 | **0** | **不落**（生成器 `v != 0` 才写）⇒ `CardDef.HasActiveAbility = -1`（`GateUnknown`） |

⇒ 产品数据里 `HasActiveAbility` **永远取不到 `0`**（1,142 张里没有一个显式 0）⇒ 修前那道
`if (HasActiveAbility == 0) return false;` **一次也没触发过**，等于没有闸：
B 那 34 张（Orikan the Diviner · Boss Zagstruk · Jain Zar · Lieutenant Titus …）只要
`2 ∈ (manaLeft, limit]` 就给 ⑥ **+1**，而原版 `hasActiveAbility=0` ⇒ 整段跳过 ⇒ **0**。

### 4.2 修法（一行，`RuleEngine/Data/AiScoring.cs`）

```csharp
- if (u.Card.HasActiveAbility == 0) return false;
+ if (u.Card.HasActiveAbility != 1) return false;
```

为什么是 `!= 1` 而不是补一条 `-1` 分支：

- 原版是**字节真值**判断（`!= 0`），`0` 就是「没有」；
- 我们的三态里 `1` = 有 / `0` = 明确没有 / `-1` = 未知（旧表、夹具、"真值说 0 但键被省略"）；
- `!= 1` 对 **0 与原版逐字等价**，对 `-1` 取「不猜 = false」（本仓红线）；
- ⛔ 另一条「让生成器把 0 也落键」的路**没走**：那会改卡表字节（`--check` 不再一致、下游 DTO 全表变动），
  而这一格的原版语义在消费端就能表达完整。

### 4.3 同步修正的两处夹具（否则既有的「行29 ④」会变红）

修前 ④ 之所以能拿到 3 分，**正是靠这条 bug**（夹具卡 `HasActiveAbility` 默认 `-1`，假钩子绕过了闸）。
⇒ 给 `Hero()` 与 ④ 里那个假单位显式 `hasActiveAbility: 1`（**同一物理行内改**，零行数变化）。

---

## 5. 验证读数（全部本机现跑）

| 项 | 命令 | 结果 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_b141_w59 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 生成器显式动作 | `python -I -X utf8 工具/gen_cards_engine.py --check` | **rc=0** · `cards_engine.json` 一致 ✅ · `enchantment_templates.json` 一致 ✅ · **未写盘**（mtime 仍是 2026-11-03 02:30） |
| 解析层对拍 | `bash 工具/ruleprobe.sh check` | **rc=0** · 解析差异 **0 行** · 哨兵失败 **0** · 输出指纹 **`77a3c2bc…`**（未变） |
| 结算层对拍 | `bash 工具/ruleprobe.sh play check` | **rc=0** · 基线指纹 **`a0e9c63d…`** 一致 |
| 账落点闸 | `python -I -X utf8 工具/check_ledger_points.py` | 本次改动**零漂移**：`AiScoring.cs` 的 27 处账落点全部 ≤ `:1274`，新增行全在 `:1274` **之后**（见 §6） |

**指纹为什么不该动**：`play all` 是**结算层** sweep，结构上看不见 AI（已知事实）；本次只改 AI 打分的一条闸
⇒ `play check` 逐字节不变是**预期的**，⛔ 不能拿它当「改动生效」的证据。AI 侧的可观测口是
`ruleprobe ai <卡>` 的 option 13 分解（现读夹具 `totalMana=0` 恒走早退支，因此那行仍印「limit=10 > totalMana=0」——
trace 通路本身是活的，但夹具给不出非 0 读数；见 §7 ❓）。

---

## 6. 行数变化与账落点

`RuleEngine/Data/AiScoring.cs`：**1,279 → 1,334 行（+55）**。

| 位置 | 改动 | 行数 |
|---|---|---|
| `:1051-1058` | 改写「⑥⑦ 今天恒 0 —— 缺数据列」那段**已过期**的 doc（8 行 → 8 行） | **0** |
| `:1060-1061` | 同上（2 → 2） | **0** |
| `:1116-1117` | 更新「行为零变化」那句（2 → 2） | **0** |
| `:1132` | **三态闸修正**（同一行内改 + ✅ 标记） | **0** |
| `:1155-1156` / `:1271` | 夹具补 `hasActiveAbility: 1`（同一行内改） | **0** |
| `:1275-1329` | 新增断言块（⑤～⑪，含数据不变量） | **+55** |

⇒ 账里 `AiScoring.cs` 的全部 27 处落点（最大 `:1274`）**一个都没挪**（已逐条现读确认：`:1063` 仍是
`ScoreFromManaBuff` 声明行、`:1120` 仍是钩子字段、`:1146` 仍是自检入口 doc、`:1274` 仍是复位行）。

---

## 7. 断言（7 条，接在已接线的 `AiScoringSelfTest` 里）

`AiScoringSelfTest.Run` 由 `RuleEngineTest.cs:1330` 调用（**不是孤儿**），本次新增的 ⑤～⑪ 随之自动跑到：

| # | 断言 | 灭自证（怎么才会红） |
|---|---|---|
| ⑤ | 真 `CardDef`（`hasActiveAbility=1` ∧ `manaCost=5` ∈ (3,5]）⇒ 督军 **+1** | 不靠假钩子 ⇒ 只要产品列没接上就红 |
| ⑥ | 同一张卡 `manaCost=6` ⇒ **0** | 把区间闸写宽 / 恒 +1 ⇒ 红 |
| ⑦ | `HasActiveAbility == -1`（缺键）⇒ **0** | **把闸改回 `== 0` ⇒ 红**（= 本次修的 bug） |
| ⑧ | `HasActiveAbility == 0` ⇒ **0** | 与 ⑦ 分成两条断 ⇒ 只断一档过不了 |
| ⑨ | 池内 `ActiveAbilityManaCost == 2` 的卡 = **34** | 列消失 ⇒ 变 0 ⇒ 红；池变动 ⇒ 提醒重新核对 |
| ⑩ | `hasActiveAbility=1` 的 **87** 张且其 `manaCost` 全 0 | 若有人「修数据」把两者搅在一起 ⇒ 红（那会改变原版行为） |
| ⑪ | 真卡抽查：`ASH1` = 2 ∧ gate `-1` · `AM2` gate 1 ∧ = 0 | 值错 / 张冠李戴 ⇒ 红 |

⚠️ ⑦ 与 ⑧ 是**结构上不可能同时满足**的一对（`-1` 与 `0` 分档），防「两个分支一起改回去」。

---

## 8. ❓ 没做完 / 没查清的

1. **❓ 断言没在本会话跑过** —— 按红线不跑 Unity，`RuleEngineTest.Run` 未执行。`typecheck 0/0` 只证明**编得过**，
   `AiScoringSelfTest` 的 ⑤～⑪ 要等下次同步点跑宿主才算绿（§五「主对话在同步点统一跑」）。
2. **❓ ⑥⑦ 的侧别口径未复核（本次没动）** —— 原版 ⑦ 走 `GetPlayerMinionsAndRemnantInPlay()` /
   `GetEnemyMinionsAndRemnantInPlay(manager, 0)` 两份不同形状的表，我们统一用「效应所属方的 `Board`」。
   施工单只点了 manaCost 这一条，这一维**没有回源核到底哪一份表**（也未见账里有人钉过）⇒ 记在这里，
   ⛔ 别当成已核。同理 ⑤ 的 `GetCardsInHandRef(manager, isPlayer, 0)`。
3. **❓ 生成器未改** —— 施工单第 2 条要求「把 `activeAbility.manaCost` 输出进 `cards_engine.json`」，实测**已经在**：
   `gen_cards_engine.py:4412 active_ability_cost_of` → `:5894/:6700 entry.update(...)`，卡表 `version` ≥ 37，
   `--check` 一致。⇒ **没有可加的代码**；再写一遍只会造第二个语义源（铁律：两处写同一条规则迟早不一致）。
4. **⚠️ 生成器里的 `hasActiveAbility` 只在 `!= 0` 时落键**，导致「明确 0」与「缺键」在 `CardDef` 里**不可区分**。
   本次在消费端用 `!= 1` 兜住（语义无损）；若将来有别的消费者需要区分这两档，得单独开口——**别在本行上再叠。**
5. **❓ 没有新建 `RuleEngine/Editor/B141_W59_Assert.cs`**（白名单允许，但**没法接线**）：
   该目录属于**编辑器程序集**，而 `AiScoring.cs` 的 `#if UNITY_EDITOR` 自检块编在**运行时程序集**里
   （`gen_csc_rsp.py` 第 1 份 rsp 明确排除 `Editor/`）⇒ 从 `AiScoring.cs` 引用它**编不过**；
   唯一的接线点是 `RuleEngineTest.cs`，**不在本代理白名单**。
   ⇒ 7 条断言全部落在**已接线**的 `AiScoringSelfTest` 里（⛔ 不造第二份清单/孤儿文件）。

---

## 9. 给下一个会话/调度台的交接

- 这条账（L29）**已可核销**，判据落点 = `RuleEngine/Data/AiScoring.cs` 的 `AbilityManaCostInRange`（`:1127-1136`）
  ＋ `ScoreFromManaBuff`（`:1063`）；断言宿主 = `AiScoringSelfTest`（`:1276-1329`）。
- 同步点要跑的那一条：**`RuleEngineTest.Run`**（`AiScoringSelfTest` 在其中），确认 ⑤～⑪ 全绿。
- 若下次发现 ⑥⑦ 又变回「多 +1」，先看 `:1132` 那行是不是被人改回 `== 0`（断言 ⑦ 会先红）。
