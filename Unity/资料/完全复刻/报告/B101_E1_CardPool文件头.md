# B101_E1 — `CardPoolCriteriaFilter.cs` 文件头订正（零行数变化）

> 日期：2026-10-30 · 执行代理 `B101_E1` · 唯一改动文件 = `Unity/MyGame/Assets/RuleEngine/Core/CardPoolCriteriaFilter.cs`（**只改注释**）
> 触发：`交接_第一百会话.md:86`（`CardPoolCriteriaFilter.cs` 文件头仍列 `180`/`190`，实现已删）· `报告/B100_F14.md:201-202`（同一处自相矛盾）
> 红线遵守：⛔ 未跑 Unity · ⛔ 未动 git（只 `git diff` 看）· ⛔ 未改本文件以外的 `.cs` · ⛔ 未改账/正本。

## 一、改了什么（5 处，13 行 / 13 行纯替换）

### ① `:23` — `ta=180` 那一条

改前：
```
//     · 180 `choiceResolution`         （`:123`，全卡池回落 —— 那一支原版直接调 `CheckIfRawCardMeetsCriteria`）
```

改后：
```
//     · ~~180 `choiceResolution`~~      **已删除（2026-10-30）**：判据 = ILSpy 可读版 `BattleManager.cs:25622 GetChoiceFullPool` —— 选牌候选池归 `rawCard.choicesDefinition`，⛔ 不是 `abilityLogic.targetCriteria`（详见本文件 `:73-76`）
```

### ② `:24` — `ta=190` 那一条

改前：
```
//     · 190 `lastPlayedIfMeetsCriteria`（名字本身写着「若合条件」）
```

改后：
```
//     · ~~190 `lastPlayedIfMeetsCriteria`~~ **已删除（2026-10-30）**，判据 `AbilityLogic__GetTargets.c:1378-1380` 是 `return`，不过末尾 `CheckIfMeetsCriteria` 循环（池内 0 行）
```

### ③ `:29` — 「17 行 / 1 行」的口径歧义（数字是对的，但「当前卡池」会被读成全池）

改前：
```
//   而那两档在当前卡池里分别是 17 行 / 1 行 —— 更窄只会少筛、不会多筛（安全方向）。
```

改后：
```
//   而那两档在那 23 行里分别是 17 行 / 1 行（2026-10-30 现读；⚠️ 全池 `ta=10` 共 399 行 —— 这里只数带 stat 过滤的）—— 更窄只会少筛、不会多筛（安全方向）。
```
> 理由：`ta=10` 全池 **399 行**、`ta=150` 全池 **54 行**；那 17/1 指的是「带四组 stat 过滤的 23 行」里的分布。原句不设限定，读者会按全池理解。

### ④ `:42-50` — 现池影响面（整块 9 行就地改写，行数不变）

改前（要点）：版本写 `v43`（已过期）· 「落到本类射程的只有 **4 行**」· 把 `UM114`（`ta=180`）当成射程内且「会真的改变结果」· 末尾「本类接上后……**可能动 `play check` 指纹**（须调度台核/重钉）」。

改后（要点）：
- 版本改为**不写死的措辞**：`1,129 张（2026-10-30 现读，当日 \`version\` = 45；⚠️ 该字段每次重生成都会变）`；
- 射程 **4 行 → 3 行**（删 180/190 之后只剩 `ta=50` 三行）；
- `UM114` 明确写「**已不在射程**……它**曾**会真的改变结果」，并保留 `rarity<=3` 剔 `Special(5)` 的来龙去脉 + 池内 `UM52/53/54` 三个 id；
- 末尾改为「`B99_D1` 已实测删 180 后 `play check` 指纹回到基线；这一格留作备忘，⛔ 别照它把 `case 180` 加回去」。

### ⑤ 顺带修的错别字（`:73`，同一个实现侧删除说明）

`原 \`choiceResolution）\`` → `原 \`choiceResolution\``（反引号与全角右括号错配）。

## 二、账补丁 B101_E1（2026-10-30 转岗追加）

产出 = `d:/4/Unity/资料/完全复刻/账/_parts/B101_E1.tsv`（**只写补丁**：未动主表、未跑 `merge_ledger_parts.py`）。
对象 = `账/收藏.tsv:189` —— `收藏 · FilterMethods · CheckIfRawCardMeetsCriteria(RawCardScript evaluatedCard, TargetCriteria targetCriteria, BattleManager manager, bool isPlayer)`；状态列 `A` **未动**。

### 改了哪两列（旧值 → 新值）

| 列 | 列名 | 旧值 → 新值 |
|---|---|---|
| 7 | **我们的落点** | ① `RuleEngine/Core/CardPoolCriteriaFilter.cs:85`（`Apply`） → **`:88`（`Apply`）**（现读 `:85` 是 `/// <para>` 注释行、`Apply` 定义在 `:88`，逐行确认；`CardCriteria.cs:299` / `TargetCriteriaEval.cs:1062` / `EffectResolver.cs:4791` / `:4707` 四个老锚点**现读全部仍命中**，未动）<br>② 「射程 = `ta ∈ {50,51,52,60,65,180,190}`（候选池本来就是一批卡那几档；`GetTargets.c:895/907/917/926/963` + `:123`）。」 → 「实现射程 = `ta ∈ {50,51,52,60,65}`（同上出处）；**现池真正落到射程的 = `ta=50` 三行（`AM31`/`GOF24`/`UM20`）**。🔴 `180`/`190` **已删（2026-10-30，`B99_D1`）**：判据见 `CardPoolCriteriaFilter.cs:73-76`，`B99_D1` 已实测删 180 后 `play check` 指纹回到基线，⛔ 别把 `case 180` 加回去。」 |
| 8 | **差异** | 「⚠️ **现池会真的变一条**：`UM114 Lead From the Front`（`ta=180`…）…**可能动 `play check` 指纹**（须调度台核/重钉）。另 `ta=50` 三行…恒真、零变化。」 → 「🔴 2026-10-30 `B101_E1` 更正（原写「现池会真的变一条 `UM114`…可能动指纹（须重钉）」—— **已过期**）：`180`/`190` 已从 `IsCardPoolTargetsAffected` 删除（判据见 `CardPoolCriteriaFilter.cs:73-76`）⇒ `UM114`（`ta=180`，`rarityFilter=3 + option=3(EqualOrLower)` + `armyFilter=1`）**已不在射程**，它那格 `rarity<=3` 剔 `Special(5)`（池内 `UM52/53/54` 三张）今天不会发生；`B99_D1` 已实测删 180 后 `play check` 指纹回到基线（主档 1,129 张 / 电池 1,073 张逐字节一致），⛔ 别把 `case 180` 加回去。射程只剩 `ta=50` 三行（…`manaCostFilter=1 + EqualOrHigher`）⇒ 池内 0 费单位 = 0 张、恒真、零变化。原版 `CardRarity` = `None0/Common1/Rare2/Epic3/Legendary4/Special5`（判据留档）。」 |

- **保留更正痕迹**（铁律 5）：旧句没有整段抹掉，而是以「原写……—— **已过期**」的形式留在括号里，下个会话能看出这格被核过。
- ⚠️ 说明：两处过期句其实落在「**我们的落点**」与「**差异**」两列，**不在「判据要点」列** —— 那一列讲的是原版 `CheckIfRawCardMeetsCriteria` 的 ①~⑧ 逐格语义（ta==0 / 卡种四格 / spellType / traits / stat / 白名单 / army / keyword + 「原版一次都不读」的 12 个字段），逐条看过**仍然成立**，故未动。
- **其余 9 列逐字节未动**：域 / 原版类 / 方法 / 判据出处 / 判据要点 / 状态 / 等级 / 轮次 / 备注。

### 往返比对（裸 `split("\t")` 读写，⛔ 未用 `csv` 模块）

| 列 | 列名 | 往返结果 | 长度（旧 → 新） |
|---|---|---|---|
| 1 | 域 | EQUAL | 2 → 2 |
| 2 | 原版类 | EQUAL | 13 → 13 |
| 3 | 方法 | EQUAL（完整签名带 `(...)`，未动） | 125 → 125 |
| 4 | 判据出处 | EQUAL | 293 → 293 |
| 5 | 判据要点 | EQUAL | 644 → 644 |
| 6 | 状态 | EQUAL（`A`，按要求不动） | 1 → 1 |
| 7 | 我们的落点 | EQUAL（= 新值） | 461 → 1001 |
| 8 | 差异 | EQUAL（= 新值） | 1215 → 1335 |
| 9 | 等级 | EQUAL | 2 → 2 |
| 10 | 轮次 | EQUAL | 8 → 8 |
| 11 | 备注 | EQUAL | 620 → 620 |

- **11/11 列往返全等**（主表读 → 写补丁 → 再读补丁 → 逐列比；未改的 9 列与主表**逐字节相同**，覆盖调度台要求的「8 列」口径）。
- 补丁文件形态：**首行 = 主表 11 列表头逐字节照抄** · 共 2 行（表头 + 1 行数据）· **5,747 字节** · **纯 LF、0 个 `\r`、以单个 `\n` 结尾** · 列内无 TAB。
- 生成方式：内容**先算进变量、再一次性 `wb` 写**（没用 `io.open(p,'wb').write(<表达式>)` 那种先截断的写法）；脚本 `_tmp_view/b101_e1/mkpatch.py` **纯 ASCII**，两个长中文列值走独立 UTF-8 侧文件（避开「bash 双引号 + 中文 + 反引号」那个坑）。

### 🔴 合并顺序（重要，别按字典序合）

`python -I -X utf8 Unity/工具/check_patch_conflicts.py` 报：`FilterMethods . CheckIfRawCardMeetsCriteria(...)` 是**跨补丁重复键** —— `B101_E1.tsv:2` 与 `B99_C1.tsv:4` 同一行。

- 字典序 `"B101_E1" < "B99_C1"` ⇒ **若按 `--only B9…` 之类的宽前缀批量合并，`B99_C1` 后写、会把本补丁整行盖回旧内容，零报错**。
- ✅ 正确顺序：**先 `--only B99_C1`，再 `--only B101_E1`**（已验证 `B99_C1.tsv` 第 4 行与主表 `收藏.tsv:189` **逐字节相同** ⇒ 先合它是 no-op，安全）。
- 📌 `B99_C1.tsv` 目前**尚未归档**（仍在 `_parts/`、无同名 `_merged_*` 目录）⇒ 这条顺序问题是真的，不是理论问题。

### 本行其他过期句的扫描结果（**只报不改**，除上面已修的两列）

| 列 | 断言 | 现读核验 | 结论 |
|---|---|---|---|
| 4 | `decomp_full/FilterMethods__CheckIfRawCardMeetsCriteria.c:1-104` | 文件在，共 **105 行** | ✅ 成立 |
| 4 | `cpp2il/out_cpp2il_dev_ilspy/FilterMethods.cs:612-715` | `:612` = 该函数签名 · `:715` = `}` | ✅ 成立 |
| 4 | 分项 `MeetsCardTypeCriteria.c` 等 5 个短名 | **5/5 全在**，真名带前缀：`FilterMethods__MeetsCardTypeCriteria.c` / `__MeetsSpellTypeCriteria.c` / `__MeetsArmyCriteria.c` / `__MeetsSpecificCardCriteria.c` / `__MeetsStatCriteria.c` | ✅ 成立（**短名是缩写、不是缺文件** —— 按原名 `ls` 会 0 命中，走前缀模糊匹配才对上，属铁律 5 那条坑） |
| 4 | 「`manager`/`isPlayer` 两个形参原版**体内一次没读**」 | ILSpy `:612-715` 内两词只出现在签名 `:612` 与 `:628`，而 `:628` 是**转发**给 `RawCardMeetsTraitCriteria(evaluatedCard, targetCriteria, manager, isPlayer)`；再核被调方 `:1282-1373` 体内**同样 0 次读** | ✅ 成立（**只转发、不读**，连下一层也不读 ⇒ 确系形状参数） |
| 5 | ①~⑧ 逐格语义 | **未逐格重核**（属别批的活，本件只做文件头/账同步） | ? 未核 |
| 11 | 「池内 `cardType 40` 现读 **0 张**」 | 现读 `cardType==40` **0 张** · `type=="whispers"` **0 张** | ✅ 成立 |
| 11 | `收藏:201` 的 `TargetCriteriaEval.cs:1030` | `:1030` = `RawCardMeetsTraitCriteria(...)` | ✅ 未漂 |
| 11 | `收藏:188` 的 `:93/:533/:710/:880/:950/:1007` | 逐个现读 = `Matches` / `StatGroupsPass` / `TraitPasses` / `CandidateAdjacentTo` / `KeywordHas` / `TypeGatePasses` | ✅ 未漂 |
| 11 | 「`play check` rc=0、结算指纹未动」 | 与第 8 列现已改成「删 180 后回基线」同义 | ✅ 与本次更正一致 |

**结论：除已修的两列以外，本行没有别的过期句。** 剩下没核的只有第 5 列 ①~⑧ 的逐格语义（不在本次范围内）。

## 三、零行数变化验证

```
$ cd d:/4 && git diff --numstat -- Unity/MyGame/Assets/RuleEngine/Core/CardPoolCriteriaFilter.cs
13	13	Unity/MyGame/Assets/RuleEngine/Core/CardPoolCriteriaFilter.cs
```

- **added = deleted = 13**（纯替换，无净增删）。
- 行尾：`crlf 0 · lf 125 · bare_cr 0` —— **改前改后都是 125 个 `\n`、0 个 `\r`**（文件本来就是 LF，⛔ 未用 `sed -i`）。
- `git diff -U0` 的 hunk 头 = `@@ -23,2 +23,2 @@` / `@@ -29 +29 @@` / `@@ -42,9 +42,9 @@` ⇒ **`23/24` 与 `42-50` 的行号未变**，`:73-76` 的删除说明仍在原位（现读确认），`case 180`/`case 190` 已无任何可执行行（`grep "case 180"` 只剩注释）。

## 四、`ta` 现读统计（⛔ 未信旧数字，全部重算）

数据源 = `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`，统计口径 = `abilityTargetCriteria[].criteria`，一行一条（`ab`/`lg` 不同各算一行）。

| 项 | 现读 | 文件头旧值 | 结论 |
|---|---|---|---|
| 张数 | **1,129**（`count` = 1129） | 1,129 | ✅ 一致 |
| `version` | 读时 **45**（任务开始读到时是 44，中途被别的代理重生成翻了一版） | `v43` | ❌ **旧值过期** → 改成不写死版本的措辞 |
| `abilityTargetCriteria` 总行数 | **1,621** | （未写） | — |
| 带四组数值 stat 过滤（`attackFilter`/`currentHealthFilter`/`manaCostFilter`/`rarityFilter` 任一 ≠ 0） | **23 行 / 14 张** | 23 行 / 14 张 | ✅ 一致（在 v44 与 v45 两份上都复算过，两次都是 23/14） |
| 这 23 行的 `ta` 分布 | **`10×17 · 50×3 · 0×1 · 180×1 · 150×1`** | 同 | ✅ 一致（17+3+1+1+1 = 23） |
| 删 180/190 后的射程行数 | **3 行（全是 `ta=50`）** | 「4 行」 | ❌ **旧值过期**（那是删 180 之前的数） |
| `ta=50` 三行 | `AM31`/`GOF24`/`UM20`，**全是 `manaCostFilter=1 + option=2(EqualOrHigher)`** | 同 | ✅ 一致 |
| `ta=180` 那一行 | `UM114 Lead From the Front`，`rarityFilter=3 + option=3(EqualOrLower)` + `armyFilter=1` | 同 | ✅ 一致 |
| `ta=180` 全池行数（`:75` 那条注释） | **43 行**，且**全部** `filterSpecificCards` 空 + `selectFromChooseResolution=1` | 43 行 | ✅ 一致（逐行复算） |
| `ta=190` 全池行数 | **0 行**（分布里根本没有 190） | 未写 | ✅ 与「已删除」不冲突 |
| 池内 Ultramarines `special` 卡 | **3 张 = `UM52`/`UM53`/`UM54`** | 「3 张 `Special`」 | ✅ 一致（补了 id） |

`option` 标签交叉核对：`CardDef.cs:271-272` / `CountEval.cs:32-33` —— `StatFilterOptions` = `1 EqualTo / 2 EqualOrHigher / 3 EqualOrLower / 8 RandomChance` ⇒ 文件头的 `option=2(EqualOrHigher)`、`option=3(EqualOrLower)` 标注**正确**。

## 五、typecheck

```
$ TMPDIR=/tmp/wf_b101_e1 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**0/0，一次通过，没有「别人的文件」的错**（两次跑都干净：第一次在改完 ①②③④ 之后，第二次在 ⑤ 之后）。本波其他代理正在写的 `BattleDriver.cs` / `ItemDrawer.cs` / `RuleEngine/*.cs` 当时都没有半成品错误暴露出来。

## 六、顺手发现（**只报不改**）

1. ✅ **账主表过期 —— 已修成补丁**（转岗后补的，见 §二）：`账/收藏.tsv:189`（同一行也存在于 `账/_parts/B99_C1.tsv:4`）里「射程 = `ta ∈ {50,51,52,60,65,180,190}`」「现池会真的变一条 `UM114`…**可能动 `play check` 指纹**（须重钉）」两句已按现读改写；补丁 = `账/_parts/B101_E1.tsv`。**主表未动（红线）。**
2. ✅ **落点已漂 —— 已修成补丁**（见 §二）：原写 `CardPoolCriteriaFilter.cs:85`（声称 = `Apply`），现读 `Apply` 在 **`:88`**、`:85` 是 `/// <para>` 注释行 ⇒ 补丁改成 `:88`。剩余建议：调度台合并后跑一次 `check_ledger_points` 确认落点判定通过（**改落点必须走账，我没有动主表**）。
3. 🔴 **合并顺序有雷**：`B101_E1.tsv` 与 `B99_C1.tsv` 撞同一行，字典序 `B101_E1 < B99_C1` ⇒ **必须先 `--only B99_C1`、后 `--only B101_E1`**，否则本补丁被整行盖回旧内容且零报错。详见 §二末节。
4. ⚠️ **文件头 `:8-9` 仍写**「卡模板级那一支（`choiceResolution(180)` 的全卡池回落）另有函数 `:123` `FilterMethods.CheckIfRawCardMeetsCriteria`」 —— 这句**描述的是原版结构、仍然成立**，我没动；但它紧挨着上面那张「射程表」，容易被读成「180 还在我们射程里」。已在 `:23` 用 `~~180~~ 已删除` + 指向 `:73-76` 消歧。
5. ⚠️ **`cards_engine.json` 正在被并发重生成** — 我核对期间它从 `version 44` 变成 `45`（工作树有未提交改动，`gen_cards_engine.py` 也有 9/1 行改动）。所以我把文件头里的版本号改成「当日 `version` = 45；⚠️ 该字段每次重生成都会变」而不是写死 `v45`。**全部统计量在 v44 与 v45 两份上各算一次，结果完全相同**。
6. 📌 **可核销** — `交接_第一百会话.md:86` 与 `报告/B100_F14.md:201-202` 记的就是这一条，本件做完即可核销（交接件由调度台改，我没动）。
7. 📌 `B101_E1` 已在 `文件所有权_当前.md:23` 登记（只改注释），未与任何写手撞车：本件全程只碰这一个 `.cs`。

## 七、没查清的部分

- **判据本身我没有回源重核**：`:23`/`:24`/`:73-76` 引用的 `BattleManager.cs:25622 GetChoiceFullPool`、`AbilityLogic__GetTargets.c:1378-1380`（`iVar6==0xbe` ⇒ `return`），我是**照简报给的判据 + 文件里 `:73-76` 与 `B99_D1`/`B100_X3` 已有结论**同步过来的，**没有打开 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs` 与 `decomp_full/AbilityLogic__GetTargets.c` 逐行复核**。若调度台要独立验证这两条，需现读那两处（本件是「注释同步」，不是判据重判）。
- **`:45`/`:46` 那句「池内 3 张 `Special` 的 Ultramarines 卡会漏进来」是历史叙述**（180 删了之后不再发生）。我按「保留来龙去脉」保留它，并加了「已实测回基线」的结局句；若调度台认为备忘里不该留这种「会漏进来」的现在时表述，可再压一句。
