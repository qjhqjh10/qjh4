# `B-264` · `ruleprobe check` 解析差异 4 行 —— 先判后钉

- 写手：`W-29` · 2026-11-01
- 入口：`bash d:/4/Unity/工具/ruleprobe.sh check`（改前 **rc=1**）
- 结论：**4 行全部 ✅ 预期内（有判据）· 无 🔴 真回归 · 无 ❓ 归不了因** ⇒ 按工具流程重钉 `PinnedParseMd5` ＋ 重生成基线。
- 改后读数：**`解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0` · `哨兵合计：失败 0 条` · `rc=0`**。

---

## 〇、先说一处与任务书不一致的事实（已就地记下）

任务书写「哨兵 `PinnedParseMd5` 现钉的是 `ebdac77915a81673bf44114062061ee9`」——**现读不是**：

| 位置 | 改前 | 改后 |
|---|---|---|
| `工具/ruleprobe/Program.cs:350` `PinnedParseMd5` | `136cbb6731e1d6a6ed171462fdb3de00` | `ebdac77915a81673bf44114062061ee9` |
| `check` 现算（`[输出指纹]` 那一行） | `ebdac77915a81673bf44114062061ee9` | 同左 |

即 **`ebdac779…` 是「现算」值、不是「改前钉住」值**（改前钉住的是 `136cbb67…`，第八十八会话钉的）。
另：**这条常量在 `Program.cs`，不在任务书写的 `PlayProbe.cs`**（`PlayProbe.cs` 里只有三处注释提到它，没有该常量）。
本次据此改的是 `Program.cs:350` 那一行（**行数守恒、只动这一行**，见 §四）。

---

## 一、四条差异逐行「改前 → 改后」

比对方法（两把尺子，互不依赖）：
`bash 工具/ruleprobe.sh scan d:/4/_tmp_view/w29_scan.txt` 的现算 dump ↔ `git show` 出的旧基线；
再对每一条走 `ruleprobe card <名>` 看 `dur=` / `T={…}` 全字段。

### 1) `AM41|Move! Move! Move!` —— 只变一个字段：`SubtypeFilter: - → Infantry`

卡面（`cards_engine.json` 的 `desc`，改前/改后**逐字相同**）：`Give Flank to all your Infantry troops`

```
改前  give/pool///0/flank/… {T:Raw=all your infantry troops|Side=own|Kind=troop|Count=0|…|SubtypeFilter=-|…}
改后  give/pool///0/flank/… {T:Raw=all your infantry troops|Side=own|Kind=troop|Count=0|…|SubtypeFilter=Infantry|…}
```
（除该字段外**逐字节相同**；`Count=0` / `Side=own` / `Raw` 都没动。）

- **归因**：`b7075d6d`（第一百一十二会话 · `B-203`）——`EffectText.ParseTarget` 的 `t.Contains("troop")` 那两支改成
  `{ spec.Kind = "troop"; ApplyKindNounSubtype(spec, t); }`（现读 `EffectText.cs:10265` → `:10860` `SubtypeFilterOfKindNoun` → `:10900` `ApplyKindNounSubtype`）。
  病灶 = `<兵种词> troops` 里的兵种词**被通称名词 `troops` 吃掉**，`SubtypeFilter` 永远落 `-`（= 不筛兵种，比卡面**宽**）。
- **判据**（原版卡数据，第一权威 · 本代理现读复核过）：
  `资料/普查产出_1018/allcards_1350/cards_raw.json` 里 `AM41` 的 `cardAbilities[0].abilityLogic[0].targetCriteria`
  = **`keywordFilter: 5`** · `includeHero: 0` · `includeMinions: 1` · `targetsAffected: 10`。
  `5` = `Infantry`（`CardDef` 的兵种枚举；池内 `keywordFilter` 取值分布 `{5,10,25,30,35,40}` 全部落在这套兵种枚举里）。
  ⇒ 原版确实是「兵种筛 + 只限 minion」，与改后 `Kind=troop` + `SubtypeFilter=Infantry` **两层并且**一致。
- **判档：✅ 预期内（有判据）**

### 2) `TAU73|Hidden Hunters` —— 同上，只变 `SubtypeFilter`

卡面：`Give +1 [Attack] and Stealth to your Infantry troops`
- 改前 `{T:Raw=your infantry troops|…|SubtypeFilter=-|…}` → 改后 `SubtypeFilter=Infantry`，**其余逐字节相同**。
- 归因：同 1（`b7075d6d` · `B-203`）。
- 判据：`cards_raw.json` 的 `TAU73` 有 **2 条 logic 行（lg0/lg1）**，`targetCriteria.keywordFilter` **都是 5**，
  `includeHero: 0` / `includeMinions: 1` / `targetsAffected: 10` ⇒ 两条 op 都该带 `SubtypeFilter=Infantry`。
- **判档：✅ 预期内（有判据）**

### 3) `UM13|Primaris Chaplain` —— 同上，只变 `SubtypeFilter`

卡面：`Give +1 [Melee] and +1 [Ranged] to your Infantry troops`
- 改前 `SubtypeFilter=-` → 改后 `SubtypeFilter=Infantry`，其余逐字节相同。
- 归因：同 1（`b7075d6d` · `B-203`）。
- 判据：`cards_raw.json` 的 `UM13` 唯一一条 logic 行 `keywordFilter: 5` / `includeHero: 0` / `includeMinions: 1`。
- **判档：✅ 预期内（有判据）**

### 4) `b3735ef3d1408d544b0947e31f898e20|Grisly Feast` —— 唯一一条**整行重写**：2 op → 4 op

卡面（改前/改后逐字相同）：`Give +1 [Attack] this turn and +1 Health to your Warlord and Infantry troops`

改前（基线那一行，2 个 op）：
```
op1 give +1 attack  and +1 health   T={Raw=your warlord |Side=own |Kind=warlord|Count=1|SubtypeFilter=-}
                                    TAIL=give +1 attack  and +1 health to infantry troops this turn
op2 give +1 attack  and +1 health   T={Raw=infantry troops|Side=any |Kind=troop  |Count=1|SubtypeFilter=-}
```
改后（现算，4 个 op；`ruleprobe card "Grisly Feast"` 连 `dur=` 一起读）：
```
op1 give +1 attack  T={Raw=your warlord        |Side=own|Kind=warlord|Count=1|SubtypeFilter=- }  dur=nextturn
op2 give +1 attack  T={Raw=your infantry troops|Side=own|Kind=troop  |Count=0|SubtypeFilter=Infantry}  dur=nextturn
op3 give +1 health  T={Raw=your warlord        |Side=own|Kind=warlord|Count=1|SubtypeFilter=- }  dur=（空 = 永久）
op4 give +1 health  T={Raw=your infantry troops|Side=own|Kind=troop  |Count=0|SubtypeFilter=Infantry}  dur=（空）
```

四处行为变化，**每一处都有自己的归因与判据**：

| # | 变化 | 归因（提交 · 代码） | 判据 |
|---|---|---|---|
| a | **2 op → 4 op**（载荷 `+1 attack and +1 health` 拆成两条；`this turn` 只修饰前半） | `246eba25`（第一百一十一会话 · 代码注释标 `B113_G2`）· `ApplyGiveDuration` / `SplitMidPayloadDuration`（现读 `EffectText.cs:10656`/`:10668`） | ① 卡面 `+1 [Attack] **this turn** and +1 Health` —— 时长短语夹在载荷中间，只修饰它前面那半；② 原版 `cards_raw.json` **4 条 logic 行**：lg0/lg1 `meleeAttackChange:1 + untilPlayerTurnStart:1`、lg2/lg3 `healthChange:1` 且五个 `until*` 全 0（**永久**） |
| b | op2 的 **`Side: any → own`** | `b7075d6d`（一百一十二 · `B-189`）· `InheritTargetSide`（`:10755`） | 卡面 `your Warlord **and** Infantry troops` —— `your` 并列分配到整个列表；英文里 Y 前面连冠词都没有，不是「某个步兵部队」。原版 4 条 logic 行 `friendlyUnits:1 / enemyUnits:0` |
| c | op2 的 **`Count: 1 → 0`**（= 从「挑一个」变成「全体」） | 不是独立改动，是 (b) 的**连带**：`ParseTarget` 的 `RePluralOwn`（`:10381`）条件是「`your` 或 `friendly` ＋复数 `troops`/`units`」，加上 `your` 之后才命中 | 卡面是复数 `troops`（无冠词）；原版 `targetsAffected: 10`（= 全体）。旧基线的 `Count=1` 本身就是缺陷 |
| d | op2/op4 的 **`SubtypeFilter: - → Infantry`** | 同 §1（`B-203`） | 原版 lg1/lg3 的 `keywordFilter: 5`；lg0/lg2 是 `keywordFilter: 0 + includeHero: 1`（督军），与改后 op1/op3 **不带兵种筛**一致 |

- 补充：改后 4 条 op 与 `cards_raw.json` 的 lg0…lg3 **逐条 1:1 对齐**（攻击→督军 · 攻击→步兵 · 生命→督军 · 生命→步兵；前两条限时、后两条永久）。
- **判档：✅ 预期内（有判据）**

### 归因的旁证（两份写手报告 + 我自己的复核）

- `资料/完全复刻/报告/B112W_B203.md` §五 的表格**就是这 4 张卡**、逐条写「`SubtypeFilter: - → Infantry`」，并在 §七 写明
  「解析差异 4 行 = 上表这 4 张……若调度台确认本笔正确，**才**按工具里那条流程重钉 `PinnedParseMd5` 并重生成基线」。
- `资料/完全复刻/报告/B112W_B189.md` §六 记录当时「解析差异 1 行 = `Grisly Feast`」，并指出**盘上基线比解析器老**（同一张卡）。
- 我这一轮**没有照抄这两份**：`scan` 现跑逐行 diff（`diff --numstat` = **4/4**，只有这 4 行）＋ `cards_raw.json` 现读 4 张卡的 logic 行（见上）＋ `ruleprobe card` 读 `dur=`。
- 另外核过：**卡池数据没参与这次差异** —— 这 4 张卡的 `desc`/`descZh`/`abilityTargetCriteria` 在 `169cfb19`(钉基线那版) / `HEAD` / 工作树**三处逐字相同**。
- 也核过第一百一十一/一百一十三会话对 `EffectText.cs` 的另外两笔改动（`246eba25` 的另一半 = `TailIsTimedGiveClause`；`6e84c7f7` = target-criteria gap 出声）：
  `git log -S` 确认 **`SubtypeFilterOfKindNoun` / `InheritTargetSide` 只在 `b7075d6d` 引入**，且全池只这 4 行变 —— 与两笔改动的覆盖面自洽。

---

## 二、最终决定

**重钉（不是停手）。** 四条全是 ✅、各有判据、无 🔴/❓。执行了两件事（都是工具自己写的流程）：

1. **重生成基线** `工具/ruleprobe/基线/out_baseline.txt`
   （`bash 工具/ruleprobe.sh scan D:/4/Unity/工具/ruleprobe/基线/out_baseline.txt`，用**绝对路径**避开「相对路径写成嵌套目录」那个坑）——
   `git diff --numstat` = **4 行改 / 4 行删**（只有这 4 张卡），新基线与我先前导出的临时 scan **逐字节相同**；
   新基线 md5 = **`b6b412f52f5e76cde6c005b6a118b0d4`**（旧 = `ed2edeab…`）。
2. **手改 `Program.cs:350`**：`PinnedParseMd5` `136cbb67…` → **`ebdac77915a81673bf44114062061ee9`**，
   并在同一行的注释里写清「为什么这 4 行是预期内的 + 归因到哪次提交 + 报告指针」。
   ⚠️ **只改了这一行**：`git diff` 里 `Program.cs` 的那 `-1/+1` 就是它（其余 `+121` 是别的写手 `W-25` 的 `triggerops` 活，**原样未动**），**行数守恒**。

---

## 三、重钉后的 `check` 读数（原样抄）

```
⚠️ [基线指纹] 卡池的 md5 与钉住的**不同** ⇒ 本哨兵**此时不判**（⛔ 不是「过了」）。
             现值 卡池 md5=64309acdab6d0d8003c9ae26a31bc694 · 基线 md5=b6b412f52f5e76cde6c005b6a118b0d4
✓ [输出指纹] 当次现算的解析输出与被确认的那一版一致（ebdac779… · 与卡池/基线文件都无关 —— 重生成基线抹不掉）
=== 哨兵合计：失败 0 条 ===
=== 全池 1121 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
=== 17:35:14 结束（rc=0）===
```

⚠️ `[基线指纹]` 那条**仍是 ⚠️、不是 ✓**：卡池 `cards_engine.json`（现在 **v46**，工作树里被别人改着、未提交）的 md5
`64309acd…` ≠ 钉住的 `81d580f3…` ⇒ 它走「此时不判」那一支（**设计如此**，由 ⑦ 独立兜住）。
**我没有替别人钉 `PinnedPoolMd5` / `PinnedBaselineMd5`**：前者是在写的活、后者不在我的白名单（见 §四）。

---

## 四、没查清 / 没做（交调度台）

1. **`PinnedBaselineMd5` 现在是陈的**：钉住 `ed2edeab…`，盘上 `b6b412f5…`。
   眼下不会红（卡池 md5 也不同 ⇒ ⑤ 走「不判」），但**一旦有人把 `PinnedPoolMd5` 重钉了、而基线这颗没跟着钉**，
   ⑤ 会立刻红（「卡池没动、基线被改写」）。⇒ **同步点请三颗一起钉**（工具自己那条提示也是这么写的）。
   我**故意没改**这颗：任务书白名单只授权 `PinnedParseMd5` 那一行。
2. **卡池 v46 是谁在改、会不会再变**：`cards_engine.json` 工作树是修改态（`M`，未提交）。本轮只确认了
   「这 4 张卡的输入字段三处逐字相同」⇒ 它与本次 4 行差异**无关**；至于 v46 整体加了什么列，不在本活范围。
3. **`untilPlayerTurnStart → dur=nextturn` 这个映射**：本轮只核到「原版那两条确实是限时、另两条确实永久」
   （靠 `until*` 五个字段），没有另开一轮去证「`untilPlayerTurnStart` 精确等于我们 `nextturn` 的语义」（早就存在、非本次变化）。
4. 本轮**没有跑** `play check`（任务书说它已 rc=0，且本次改动只有「解析哨兵常量 + 解析基线文件」，两者都不进结算指纹）。
5. 本轮**没有跑 Unity**、**没有动 git 的写操作**、**没有碰 `Assets/**`、`账/*.tsv`、正本、`活状态表.md`**。

---

## 五、顺手发现

1. 🔴 **任务书里的文件位置写错了**：`PinnedParseMd5` 在 `工具/ruleprobe/**Program.cs**:350`，**不在 `PlayProbe.cs`**
   （`PlayProbe.cs` 只有 3 行注释提到它）。下次派这类活直接给 `Program.cs`，否则写手会先去找一圈。
2. 🔴 **任务书把「现算值」当成「钉住值」了**（见 §〇）—— 这正好说明「先读、别照抄简报」是有用的：
   若照简报去把常量改成 `ebdac779…` 并对一下「已经等于」就收工，**rc 仍然是 1**（解析差异 4 行照样让 `rc=1`，
   见 `Program.cs` 收尾那句 `return (parseDiff > 0 || sentFail > 0) ? 1 : 0;`）。
   ⇒ **这条值得记进「怎么读 `check`」**：`rc` 是 **解析差异 ∨ 哨兵失败** 的「或」，只钉哨兵**不可能**把 rc 弄绿。
3. 🟡 **旧基线里编码着一个真缺陷**：`Grisly Feast` 的 op2 是 `Side=any`（= 会给**敌方**部队 +1/+1）、`Count=1`（只给一个）、
   `SubtypeFilter=-`（不筛兵种）。把旧基线当「正确答案」去比，会把「修好了」看成「坏了」——
   这正是 `A1382` 那条「基线能被 `scan` 一条命令重写」的反面：**基线也会把缺陷固化成「参照物」**。
4. 🟡 `[Source 列]` 的形状失真（假 op 6 条 / 截断 5 条）仍在，是**已知且不计入失败**的那一类（`A1374`/`E1` 顺手发现 3），本轮没动。
5. ℹ️ 一个可复核的小读法：`Grisly Feast` 的 `Count: 1 → 0` **不是**某次独立改动，是 `B-189` 补上 `your` 之后
   顺带**命中** `RePluralOwn`（`EffectText.cs:10381`）的结果。以后遇到「加了侧别词却连带改了 Count」的报警，
   先看这条正则，别当成新 bug。
