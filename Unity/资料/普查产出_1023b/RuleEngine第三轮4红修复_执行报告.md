# RuleEngine 第三轮同步 4 红 —— 执行报告

- 日期：**2026-10-23** · 执行代理（第三轮修复批）
- 判据：`普查产出_1023b/诊断_RuleEngine第三轮4红.md`（只读诊断；4 红 = 3 条 α 测试侧 + 1 条 β 实现侧）
- 红线遵守：**未跑 Unity**（只跑秒级 `typecheck` + `ruleprobe check`）· **未动 git** · 未改正本与其他线台账。
- 改动文件（3 个，全在简报白名单内）：
  1. `Unity/MyGame/Assets/RuleEngine/Core/ConditionEval.cs`（110 实现）
  2. `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`（3 条测试）
  3. `Unity/资料/任务线/引擎线/任务总表.md`（新开一行 + 顺手发现记账）

---

## 一、逐条改动

### #3 `B-16 · 110`（**β · 实现缺陷**）—— 主语 `u` → `thisUnit`

**改法**（`ConditionEval.cs` 的 `case 110`）：把 `currentCombatAttack` 这半段的**主语**改成施法者：

```csharp
if (thisUnit.Has(KeywordTable.DropPod)) return null;
int combat = thisUnit.IsRemnant ? 0
           : (thisUnit.CurrentAttackType == UnitState.AttackTypeRanged
              ? thisUnit.RangedAttack : thisUnit.Attack);
return combat <= u.Bastion;          // 原版：`CurrentBastion(card = 候选)`
```

- **判据（现读全量反编译）**：
  - `FilterMethods__MeetsConditionCriteria.c`（`case 0x6e` = 110）：`p100` 为假 ⇒ `CardScript__HasBastionHigherThanAttack(param_1 = card/*候选*/, param_4 = thisCard/*施法者*/)`。
  - `CardScript__HasBastionHigherThanAttack.c:18-21`：`iVar2 = CurrentBastion(param_1)` · `iVar3 = currentCombatAttack(param_2)` · `return iVar3 <= iVar2`。
  - `EntityScript__get_currentCombatAttack.c:15-22`（`0xe6 = dropPod`）与 `:30-38`（残骸 `+0x65`，且 `+0x66 == 0`）**都是 `param_1` 自己的位** —— 所以 dropPod / 残骸两道守卫也必须读施法者。
- **只改了 3 处 `u` → `thisUnit`**（dropPod 守卫 / `IsRemnant` / 攻击值三选一）；**`u.Bastion` 保持不变**（它本来就是候选的 `CurrentBastion`）。
- 同步订正了该分支头部的判据注释（原来也写着 `thisCard.currentCombatAttack <= card.CurrentBastion`，只是代码没照做），并就地记下**已知偏离**（见 §三·2）。

**断言改动（把「假绿」转成真红）**：`RuleEngineTest.cs` 110 那一块

| 项 | 改前 | 改后 |
|---|---|---|
| 目标夹具 | `Unit("B16_110t", 1, 1, 5, "Bastion 3")`（攻击 1） | `Unit("B16_110t", 1, **9**, 5, "Bastion 3")`（攻击 9） |
| 第一条文案 | `施法者攻击 2 ≤ 目标 Bastion 3 ⇒ 真` | 加 **`｜🧨 把主语误读成候选（= 目标自己，9 > 3）⇒ 本行红`** |
| 第二条 | 单行 `攻击 5 > Bastion 3 ⇒ 假` | 拆两行，写明「改的是**施法者**的攻击 —— 目标那 9 点从头到尾不参与」 |
| 第三条 | `MeetsUnit(tgt, 110) == null` | **一字不改**，只补注释（原版这里其实是 `false`，见 §三·2） |

- **为什么这样才叫「真红」**：旧实现读候选 ⇒ 第一条算 `目标攻击 9 <= 目标 Bastion 3` ⇒ **假** ⇒ 期望 `true` ⇒ 红。
  改前目标攻击 = 1 时旧实现「以错误的理由」通过（`1 <= 3`），所以是假绿。
- 三条断言在新实现下的手算：`2 ≤ 3 ⇒ true` · `5 > 3 ⇒ false` · 施法者为空 ⇒ `null`（本版口径未变，见 §三·2）。

### #1 / #2 `A992OathB` / `A992FeroB` 的远程（**α · 只改测试**）

- 夹具第 10 个 int（`rangedAttack`）`0 → 3`，两个夹具各一处（`1, 3, 9, 0` → `1, 3, 9, 3`）。**期望保持 `ErrNoAttack(7)`、文案不改。**
- 判据链：`RuleCore.CanAttackNow` 的攻击值闸（`RuleCore.cs:5019-5025`，B-31 把近战/远程拆成两码）排在 `CannotAttackThisTurn`（`:5035`）**之前**；远程 3 ⇒ `FieldAttack(..., ranged: true) = u.RangedAttack = 3 > 0` ⇒ 越过闸 ⇒ 落到 `:5035` ⇒ `ErrNoAttack(7)`。
- ⛔ **没有**按「备选 B」把期望改成 22 —— 远程 0 本身就会返回 22，改成 22 后把 `:5035` 整句删掉仍然全绿（**丢判别力**）。已在夹具上方写了 5 行注释说明这条。
- 影响面核对（照诊断）：甲段（`plain`/`fast`，走 `Unit()` 助手，远程仍 0）只断近战 ⇒ 不受影响；乙段回合刷新后只断 `CanAttackNow(..., false) == OK` ⇒ 不受影响；`CanUseAlternative` 与 `Exhausted` 两位无关攻击值。

### #4 `cond=277`（**α · 只改测试**）

- ①-f「判不了 ⇒ 整条不筛」的样本：**277 → 240（`hasSecretOrder`）**，`Check(count, 4)` 与「不筛」断言原样保留。
- 为什么换：277 在 `ConditionEval` 接上之后**已判得了**（`case 277` = `subtype == Infantry || Beast`，单位 subtype 数据齐），而 ①-a 起那四个夹具是 `Unit(...)` 建的（subtype 空串）⇒ 277 判**假** ⇒ 池子被筛成 0（= 失败原因）。240 仍是 `Kind.ModelGap`（`case 240` 返回 `null`，缺「附魔实例 → spellType」那一层）⇒ 才测得到那条不变量。
- **机制侧一行未动**（`EffectResolver.ApplyStructuredConditionFilter` 拿到 `null` 就整条 `return`，并且出声）。
- **277 的真覆盖没有丢**：`RuleEngineTest.cs` 已有 `MeetsUnitCondition(hurt, 277) == false`（空 subtype，`:34501` 附近）+ `MeetsCard` 的 Infantry / Beast / Vehicle 三条正反例（`TestB16ConditionEval` 270/275/276/277 那一块）。

---

## 二、读数

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_re4 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（0/0） |
| `bash 工具/ruleprobe.sh check` | **rc=0 · 哨兵失败 0 条 · 解析差异 0 行 · 输出指纹 ✓** |
| `RuleEngineTest.cs` 行尾 | **CRLF 35494 / LF 35494**（改前 35476；本轮 +18 行，全部 CRLF，**没翻**） |
| `ConditionEval.cs` 行尾 | LF（改前 LF，未变） |
| `任务总表.md` 行尾 | LF 91 → 92 行，CRLF 仍 0（**没翻**）；新行**竖线 6 条 = 5 列** |
| `git diff --numstat` | `RuleEngineTest.cs 724/45` —— ⚠️ 这是**与 HEAD** 的差（含前几批未提交改动）；**本轮自身** = `2522341 → 2524549` 字节、`35476 → 35494` 行 |

⚠️ `ConditionEval.cs` 在 `git status` 里是 **`??`（未跟踪）**（上一批新建的文件）⇒ `git diff` 里看不到它，这是预期的。

---

## 三、台账（引擎线 `任务总表.md`）

1. **新开一行 `诊断-RuleEngine第三轮4红`**（状态 `✓`，5 列 / 6 竖线）：写了 4 条修法、读数、以及 ⏭「待主对话跑 `RuleEngineTest.Run` 验收（期望 **6736/6736**）」。
2. **顺手发现（按铁律 5·b / 11 计入同一行，**未做**）**：
   - `thisUnit == null` 时**原版明确 `return false`**（`CardScript__HasBastionHigherThanAttack.c:16-17,26`）⇒ 110 退化成「只看前半段 `100`」，**不是「判不了」**；我们仍返回 `null`（`ConditionEval.cs` 那句 `if (u == null || thisUnit == null) return null;`），并且 `RuleEngineTest.cs` 已把这条 `null` 钉成断言。**本批不改**（改了要连断言 + 生产入口一起动）。
   - **生产入口拿不到施法者**：`RuleCore.MeetsUnitCondition`（`EffectResolver.cs:10106-10109`）只有 4 个形参，调用点 `:1776` / `:10216` 都传不进 `thisUnit` ⇒ 生产上 110 只能是 `p100` 或 `null`。
   - **影响潜伏**：现读 `cards_engine.json`（1127 张）的 `unitConditions`，**`cond=110` 0 行**（对照：277 = 1 行 `Speshul Dose` · 240 = 0 行）。⇒ 做 110 时必须连那个形参一起补。
   - 同一处代码注释（`ConditionEval.cs` 的 110 分支头）也写了这几句「已知偏离」，避免下一个会话把它当原版口径。

---

## 四、没查清 / 如实记

1. **110 的 `null` vs 原版 `false`**：判据是明确的（原版 `:16-17,26`），但**本批 scope 是「4 红」**，改它会动生产入口签名 + 一条已钉死的断言 ⇒ 只记账、未改。**这不是「查不到」，是「排期」**。
2. **110 的 dropPod 分支仍返回 `null`**：原版读的是「空投舱在飞时的攻击值」（`+0xe0..+0xf0`），我们**没有**「空投舱在飞」这一层 ⇒ 拿不到真值，按「⛔ 不猜」返回 null。**未变**。
3. **未跑 Unity**（红线）。`RuleEngineTest.Run` 的实际结果（是否 6736/6736）**没有验证**，由主对话在同步点跑。
4. **`RuleEngineTest_Diag.cs` 不用重生成**：本轮只改测试方法体，**没有增删测试方法**（生成器的输入没变）。
5. **240 当样本的副作用**：它会让 `ApplyStructuredConditionFilter` 走「判不了 ⇒ 出声」那条路（`StructuredConditionNotice` 去重），**测试里不数日志** ⇒ 不影响断言。已核 `EffectResolver.cs:1767-1794`。
6. **没跑「反向验证」**（把实现改回错的、确认第一条真的红）—— 那需要跑 Unity 批处理，本批不允许。真红是按代码路径手算的（旧实现读 `u.Attack = 9` vs `u.Bastion = 3`）。

---

## 五、建议跑的宿主

- **`RuleEngineTest.Run`** 一条（唯一受影响宿主；`ConditionEval` 是 `RuleEngine/` 内共用件 ⇒ 按本工程规矩必跑这一条）。
- 判红绿看**断言合计**（期望 `6736 / 6736`）；⚠️ 别用 `grep -c ✗` 计数（会命中文案里的 `✗`）。
