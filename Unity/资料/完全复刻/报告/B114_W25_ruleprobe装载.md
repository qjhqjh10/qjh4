# B-257 · `ruleprobe` 的 `LoadPool` 不挂 `TriggerOpsByNumber`（写手 `W-25` · 2026-11-01）

> 活状态表 `B-257`（同族 `A1911` / `A1959` / `B8_AV`：**探针与引擎口径不同 ⇒ 读数静默错**）
> ⛔ 本文件只写判据、改法、读数与归因；**状态见 `活状态表.md B-257`**（本文件不写「已完成」）。

---

## 一、改法（做了什么）

**一句话**：`Program.LoadPool` 里每张卡构造完之后，**再走一遍引擎那两个 mapper** ——
`AttachTriggerAbilityIndices(MapTriggerAbilityIndices(c, version))` ＋ `AttachTriggerOpsByNumber(MapTriggerOpsByNumber(def))`
（与 `RuleEngine/Data/CardDatabase.cs:1762` **同一套代码、同一顺序**）。

| # | 文件 | 改动 | 行数 |
|---|---|---|---|
| ① | `Unity/工具/ruleprobe/CardDatabaseProbeHook.cs` **（新）** | `public static partial class CardDatabase` 的第二个 part：两个 `public` 转发 → 引擎**私有** `MapTriggerAbilityIndices(CardDto,int)`（`Data/CardDatabase.cs:4813`）／`MapTriggerOpsByNumber(CardDef)`（`Data/CardDatabasePartial_B98H7.cs:134`） | 45 |
| ② | `Unity/工具/ruleprobe/ruleprobe.csproj` | 显式 `<Compile Include="CardDatabaseProbeHook.cs" />`（本 csproj 关了 `EnableDefaultCompileItems`） | +11 |
| ③ | `Unity/工具/ruleprobe/Program.cs` | `LoadPool` 每卡末尾调 `AttachTriggerAbilityAndOpsByNumber(__def, c, poolVersion)`（`:1317`）；实现见 `:1586`；`WFPROBE_ATTACH_LOG=1` 才打的两条读数见 `:1518` / `:1526` | +121（**纯追加，0 删除**） |

⛔ `Assets/**` **一个字节没动**（`git diff --stat` = 上面三份；`typecheck` 0/0）。
⛔ 没有新增任何子命令、没有改任何既有行的语义。

### 为什么选这条路（四条备选逐条否掉，理由）

| 备选 | 否掉的判据 |
|---|---|
| **A. 整池改用 `CardDatabase.Parse` 的真实装载链** | **做不到「不改现有输出」**：`LoadPool` 必须留一份 `keywords` **原文**（`Program.PoolRawKeywords`；`CardDef` 没有公开口，见 `Program.cs:22-31`），`nonkw` / `faith` / `willrun` 三个探针全靠它；`Parse` 还会顺带写 `TalentCardData` 全局表、打它自己那串版本守卫日志 ⇒ 输出必变。⇒ 与本账验收第 2 条直接冲突 |
| **B. 反射调那两个私有 mapper** | 手法可行，但**失败是静默的**：引擎一改名 ⇒ `GetMethod` 返 null ⇒ 又退回退回档 = **本账的缺陷原样复发**。本工程刚刚因为同类「静默」吃过账（铁律：不许静默失败） |
| **C. 在探针里抄一份映射** | 两处写同一条规则（行级坏值丢弃 / `(ab,lg)` 撞键去重 / 按 `ab` 归位 / 同一 ability 多条 `lg` 行只算一次 / 三态「不挂·挂空表·挂非空」）⇒ 迟早不一致，**正是本账要治的病** |
| **D. 另跑一次 `CardDatabase.Parse`，按 id 把它的表「偷」给探针池** | op 对象会与探针自己 `_triggerOps` **不是同一批**（引擎不变量 = 表里的 op 就是 `def.TriggerOps(kw)` 的同一批对象，见 `CardDatabasePartial_B98H7.cs:153` / `:209`）；外加一整份池的内存与约 0.4 s |
| **✅ 选它：partial 转发** | `partial class` 的所有 part 是**同一个类** ⇒ `private` 跨 part 可见；本文件与 `Assets/RuleEngine/Data/CardDatabase*.cs` 被编进**同一个程序集** ⇒ 直接调真代码。<br>① **零规则重复**；② op 对象与 `_triggerOps` **同一批**（与引擎不变量一致）；③ **编译期绑定** —— 引擎改名 ⇒ `ruleprobe.sh` 当场编不过（⛔ 不是运行时静默落空）；④ `Assets/**` 不用动（那两个 mapper 仍是 private） |

> `CardDto` 由**这一张卡自己的原始 JSON** 反序列化（`UnityEngine.JsonUtility.FromJson<CardDatabase.CardDto>`，即 `Stub.cs` 那份）——
> 两个 mapper 只读 `triggerAbilityIndices`（＋出错信息里的 `id`/`name`），所以喂单卡是安全的。
> 任何异常**一律 stderr 出声**（⛔ 不让它静默退回退回档）。

---

## 二、五个探针改前 / 改后（逐字节）

口径：同一份卡池（`Assets/RuleEngine/Resources/cards_engine.json`，1129 张）、同一条命令，改前改后各跑一次，
`diff` 时**只剔除** `ruleprobe.sh` 自己打的时间戳横幅（`=== HH:MM:SS ruleprobe … ===` / `=== … 结束（rc=N）===`）——
那两行是**墙钟**，不是探针输出。

| 命令 | 内容 diff | rc（改前 → 改后） | 备注 |
|---|---|---|---|
| `check` | **0 行** | 1 → 1 | 🔴 **改前就是红的**（见下「⚠️ 前提」） |
| `play check` | **0 行** | 0 → 0 | 结算指纹 `cdf608b5da9c079e96d59e70fe549c19` **前后一致** ⇒ ⛔ 不需要重钉基线 |
| `ai GOF12` | **0 行** | 0 → 0 | |
| `traits` | **0 行** | 0 → 0 | 1134 行全池表，逐字节一致 |
| `triggerops` | **0 行** | 0 → 0 | 该命令走 `CardDatabase.Parse`，本来就已挂表（`W-22` 的读数口） |

⚠️ **前提（不是本笔引入）**：`check` 在**我开工之前**就已经 `rc=1` —— 摘要行是
`=== 全池 1121 张 · 解析差异 4 行 · 改名 0 行 · 新增 0 · 消失 0 ===` ＋ `哨兵合计：失败 1 条`（⑦ 输出指纹），
差异 4 张 = `AM41|Move! Move! Move!` · `b3735ef3…|Grisly Feast` · `TAU73|Hidden Hunters` · `UM13|Primaris Chaplain`。
⛔ 我没碰它（不是本账范围），也**没有**重生成基线。判「本笔没弄坏解析」看的是「差异仍是这 4 行、内容 0 变化」。

---

## 三、若有差异 —— 逐条归因

**五个探针：0 处差异。** 但「没变」必须解释清楚，否则下一个人会以为「没生效」。用
`WFPROBE_ATTACH_LOG=1` 量到的挂载读数（`bash Unity/工具/ruleprobe.sh seg "Deal 1 damage"` 即可复现）：

```
[triggerOpsByNumber] 本条 LoadPool：卡 1129｜已挂表 163｜未挂表(退回档) 966
[triggerOpsByNumber] 卡 1129｜列未知 0｜列空 119｜已挂表 44｜归位失败(不挂) 966｜行 1622
                    （已归位 44；跳过 lg=-1 1 · ab 坏 0 · trigger 坏 0 · 无正文 op 1129 · 该 ab 无 op 448）
[triggerOpsByNumber] 挂表 vs 退回档：(卡,trigger) 对 48｜读数不同 21｜涉及的卡 18 / 有表的卡 163
```

- **163 张挂上了表**（44 张非空 + 119 张挂空表 = 引擎「明确没有」那一态），**966 张仍走退回档** ——
  后者不是我的遗漏：引擎那一步 `MapTriggerOpsByNumber` 在这 966 张上同样返回 `null`（一条都归不了位），
  引擎自己也会退回（跳过计数里「该 ab 无 op 448」「无正文 op 1129 行」就是它）。
  ⇒ **探针现在的行为 = 引擎的行为**，一条不差（同一份 mapper 代码、同一份数据）。
- **读数确实变了 21 对（18 张卡）**，全部落在 `TriggerOpsByNumber` 这个口上（这就是本账的修复本体）：

| 卡/t | 挂表 | 退回档 | | 卡/t | 挂表 | 退回档 |
|---|---|---|---|---|---|---|
| AM19/t0 | 1 | 0 | | GOF15/t0 | 1 | 0 |
| DA21/t0 | 1 | 0 | | GOF5/t0 | 1 | 0 |
| GSC35/t0 | 1 | 0 | | GOF24/t81 | 1 | 0 |
| SAU15/t0 | 1 | 0 | | **GOF24/t300** | **0** | **1** |
| SAU17/t0 | 1 | 0 | | GOF17/t81 | 1 | 0 |
| SAU16/t0 | 1 | 0 | | **GOF17/t300** | **0** | **1** |
| GOF96/t81 | 2 | 0 | | GOF7/t0 | 1 | 0 |
| **GOF96/t300** | **0** | **2** | | SOR21/t130 | 1 | 0 |
| GOF16/t0 | 1 | 0 | | TAU66/t0 · TAU72/t0 | 1 | 0 |
| TL62/t0 | 2 | 0 | | UM7/t0 | 1 | 0 |

  两类：① **t0（`ThisCardPlayed`）**——退回档认不出 0 号 trigger 对应哪个卡面词 ⇒ 恒 0 条，挂表后能按 `ab` 取到；
  ② **t300（`Mob`）/ t81 / t130** —— 少数卡两种情况各有各的条数（`GOF96/GOF24/GOF17` 是「挂表 0 vs 退回 1~2」）。
- **为什么结算面（`play`）一点没动**：`play check` 是**全池结算对拍**（主档 1129 + 事件电池 1073 行，逐字节），
  它前后一致 ⇒ 在本池数据与现有夹具下，**没有任何一条结算路径**因为这次口径对齐而改变结果。
  差异的那 21 对集中在 `t0 / t81 / t130 / t300`，而 `EffectResolver` 里**按触发数值派发**的那几条口子走的是
  `200`（`:18749`）· `240`（`:19725` 附近）· `410`（`:21078`）· `60/185/187`（棋盘侧）· `130`（Penitence）；
  `t0`（打出那一拍）走的是**卡面文本**那条路（`WhenTriggers`），不经过这个数值口。
  ⚠️ 这是「今天零可观测」，⛔ **不是**「这个口永远不影响结算」——以后谁把它接到 `t0`/`t81` 上，读数就会变。

---

## 四、挂上之后 **仍拿不到**什么（per-logic 划分 —— 别以为挂上就万事大吉）

`MapTriggerOpsByNumber`（`CardDatabasePartial_B98H7.cs:134`）的归位条件是
**`op.AbilityIndex == 行.Ab`**（`:208`），`lg` **只**用来判「这一行是不是 `lg == -1`」（`:184`，= 该 ability 没有 logic 行 ⇒ 不贡献 op）；
同一个 `ab` 的多条 `lg` 行落进**同一个桶**并按**对象引用去重**（`:209`）。

⇒ **`TriggerOpsByNumber(trigger)` 是 ability 粒度，不是 logic 粒度**。现成例子 = `GOF12 Spanner`：
`triggerAbilityIndices` 有 5 行（`ab=0, lg=0..4, trigger=300`），而 `TriggerOpsByNumber(300)` **只有 1 条** op。

⇒ `B-251`（`W-19`）那类「`RandomAbilityChoiceOps` 按 `LogicIndex` 分组」的分析：
- 用它**仍然**只能看到 ability 桶；`op.LogicIndex` 来自 `EffectText.AssignAbilityIndices` 的**推断**，
  而 `_triggerOps` 是**按关键词**收集的、不是按 logic ⇒ 「这条 op 属于哪条 logic」**今天没有真值**。
- 想拿到 per-logic 划分，前提是先有「op ↔ `AbilityLogic` 行」的真值（数据侧），⛔ 不是探针这边补得上的。

---

## 五、顺手发现

1. 🔴 **引擎这个口的覆盖极低，而且这是数据缺口不是探针缺口**：`TriggerOpsByNumberSkipNoBody` 在本池 = **1129 行**
   （1622 行里 70%）落在「无正文 op」，真正**挂上非空表**的只有 **44 张卡**（`TriggerOpsByNumberSummary()` 现读）。
   ⇒ 谁要拿这个口做覆盖面结论，先看这条：**大部分卡在这个口上是空的**（引擎与探针现在一致，两边都空）。

2. **`GOF96` / `GOF24` / `GOF17` 的 `t300(Mob)`：挂表 0 条，而卡面关键词退回档有 1~2 条。**
   这不是我改出来的 —— 是**引擎那套 mapper 的口径**（按 `op.AbilityIndex == 行.Ab` 归位，这几张的 op 归不到那些 `ab` 上）。
   ⇒ 挂表之后探针**照出**了这个口径。要不要当成缺陷，是**引擎侧（数据/`EffectText.AssignAbilityIndices`）**的事，
   本笔⛔ 不动（`Assets/**` 不在白名单）。已在此记录，供调度台分流。

3. **`Program.cs:1515` 那句「`CardDatabase.IsLegalAbilityContext`（private，探针够不着）」现在技术上不成立了** ——
   有了 `CardDatabaseProbeHook.cs` 这条路，private **够得着**。本次**没动它**（不在本账范围），
   但以后遇到同族「探针与引擎口径不同」，优先考虑 partial 转发，⛔ 不要再抄一份规则。

4. **成本**：每卡多一次 `CardDto` 反序列化 ⇒ `LoadPool` 单次从 1.48 s 涨到 1.78 s（**+0.30 s，约 +20%**；
   放到 `play check` 那 33 s 里约 +1%）。若以后嫌慢，可以只读 `id` / `name` / `triggerAbilityIndices` 三格手工组 DTO
   —— 那会引入一点字段名重复，本次**没做**（值不值另说）。

5. **`check` 的 `rc=1` 是先前就有的**（解析差异 4 行 + 哨兵 1 条），我**没有**重钉任何基线/指纹。
   下一波如果谁要重钉，判据在 `check` 输出的那几行里，⛔ 别连这次的口径对齐一起算进去（本笔输出 0 变化）。

---

## 六、复现命令

```bash
# 挂载读数（默认不出声；这两条只在开关打开时打）
WFPROBE_ATTACH_LOG=1 bash d:/4/Unity/工具/ruleprobe.sh seg "Deal 1 damage" | grep triggerOpsByNumber

# 五个探针（改前/改后逐字节比；剔掉脚本的墙钟横幅那两行）
for c in check "play check" "ai GOF12" traits triggerops; do bash d:/4/Unity/工具/ruleprobe.sh $c; done

# 秒级类型检查
TMPDIR=/tmp/wf_w25 bash d:/4/Unity/工具/typecheck.sh    # 运行时 0 / 编辑器 0
```
