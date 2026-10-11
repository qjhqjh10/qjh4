# E4 —— `CardDef` 三笔（`A1439` · `A1469` · `A1471`）＋ 同根因一笔（`EC28`）

> 会话：第十九会话（2026-10-11）· 写手 `E4` · 独占文件 = `RuleEngine/Core/CardDef.cs`
> ⛔ 没跑 Unity · ⛔ 没跑 `工具/typecheck.sh` · 没动 git · 没改正本 · 没碰白名单以外的任何文件。
> ✅ **改前/改后是【真代码跑出来的】**（离线 net8 探针，见 §六），⛔ 不是推断。

---

## 一、改动清单

| # | 位置 | 改了什么（≤40 字） | 服务 |
|---|---|---|---|
| 1 | `CardDef.cs` `IsBodyKeyword` 之后（新增 `StripParamSuffix` / `IsBodyKeywordHead`） | 新增两条「触发头剥数值后缀」的判据（**只此一份**） | `A1469` |
| 2 | `CollectBareKeywordBody` 门 ① | `IsBodyKeyword` → `IsBodyKeywordHead`（看得见 `Ecstasy N:`） | `A1469` |
| 3 | 同方法门 ①-bis（引号里那种） | 同上 | `A1469` |
| 4 | `StartsAnotherThing` ①（断句） | 同上 —— **`EC28` 当场中招的那一处** | `A1469` 同根因 |
| 5 | `AddTriggerOp` 头部剥数字那一段 | 抽成转调 `StripParamSuffix`（**行为一字不变**） | `A1469` |
| 6 | 新增 `WhenBodyAt(segs, i)` | `When …` 那条路的**跨句并接**（停句判据转调 `StartsAnotherThing`） | `A1471` |
| 7 | `CollectWhenTriggers` | `Desc` 那一路先过 `WhenBodyAt` 再送 `AddWhenTrigger` | `A1471` |
| 8 | `AddTriggerOp` / `CollectSpiritOps` 的 doc | 写明 `EffectOp.Source` 是**两个口径、不是身份键** | `A1439` |
| 9 | `RoutableTriggers` 入表判据 doc（`:181`） | 「− 本表 **23** 词」→ **两表共有 21 词**（47 是对的） | 顺手订正 |
| 10 | `EcstasyX` / `CollectCompanionKeyword` / `AddTriggerOp` 的 `Maulerfiend` 注释 | 「`keywords` 里没有 `Ecstasy`」**已不成立**，就地订正 | 顺手订正 |

---

## 二、每笔的判据（现读账目那一行）与结论

### `A1439` —— `EffectOp.Source` 在不同采集点不是同一个东西
账目原文（`资料/任务_卡组卡牌战斗.md:52`）：`AddTriggerOp:1340` 的 `Source` = **正文半句**；
`CollectSpiritOps:1000` 的 `Source` = **整句**；⇒ 要做：**要么统一语义、要么写明是两个口径**。

**裁决 = 写明两个口径**（第 8 条）。理由：本笔只能碰 `CardDef.cs`，而 `Source` 真正被写下的地方在
`EffectText.ParseSegment`（**别人的文件**）—— 想「统一语义」就得动它 ⇒ 按简报红线**停手**，
改走账目允许的另一条路（写明口径）。

**⚠️ 订正账目的措辞（铁律 5，落在我自己文件的注释里）**：逐行走了一遍 `ParseSegment`，
**「那一路是整句」按代码读不成立** —— `1 [Spirit Stone]: …` 命中的是**付费前缀**那一支
（`EffectText.cs` 的 `RePaid`），那一条里写着 `s = low`，**前缀在进 `Dispatch` 之前就被剥掉了**
⇒ 那一路的 `Source` 同样是「冒号后那半句」。
两条路真正**逐字不同**的是**大小写/归一化**：
· 触发那一路（`Parse(body)`）`Source` **保留原文大小写**、只剥方括号；
· 灵魂石那一路（付费前缀）整串**被转成小写**。
🔴 **这一条我没跑仪器复核**（`ruleprobe` 打不出 `Source`，见 §四）⇒ 记成「按代码读」，
⛔ 别当成「跑出来的」。**结论不变**：`Source` 不能当身份键；先例 = `工具/ruleprobe/WillRun.cs:97-104`
的 `Sig()`（只比 `verb / amount / payload / target`）。

### `A1469` —— `CollectBareKeywordBody` 门 ① 看不见 `Ecstasy N:`
账目原文（`:64`）：门 ① = `IsBodyKeyword`（**整词精确等值**）⇒ `Ecstasy N:` 过不了门 ①；
实测 5 张（`EC12` / `EC14` / `EC22` / `EC23` / `EC39`）走到裸写路的门口，靠「先到先得」兜住。
⇒ 要做：**查清这 5 张今天走的是哪条路、`先到先得` 兜住它们是不是恰好**。

**查清了（静态扫全池 1126 张 + 读代码）**：
1. **全池带 `Ecstasy N:` 头的卡 = 9 张**（不止 5）：`EC28` / `EC14` / `EC29` / `EC22` / `EC18` /
   `EC23` / `EC12` / `EC39` / `EC5`。另 4 张（`EC28` / `EC29` / `EC18` / `EC5`）在**② 那道闸**
   （`only == null`，`keywords` 里一个带正文关键词都没有）就被挡下 ⇒ **账上那 5 张是对的**。
2. **那 5 张走的路**：`CollectTriggerOps` → `TriggerBodyAt` → `AddTriggerOp`（头 `ecstasy 2`
   → `StripParamSuffix` → `ecstasy` → 可路由 ⇒ 收下正文、登记 `_triggerOps["ecstasy"]`）；
   随后 `CollectBareKeywordBody` 门 ① 漏 → ② 认出 `only = ecstasy` → **在
   `_triggerOps.ContainsKey(only)` 那一句 `return`**。
3. **「先到先得」兜住它们是不是恰好 —— 是结构性的、不是巧合**：只要
   `AddTriggerOp` 收得到（头可路由 + 正文解得出来），它**一定**先登记；
   但**守卫本身是坏的**：正文**解析不出来**时 `_triggerOps` 里没有它 ⇒ 裸写路会把**整条 `desc`**
   当成 `keywords` 里那个唯一关键词的正文（静默、错时机）。合成卡实证见 §三·①。
4. **同一根因还有一处更重的**：`StartsAnotherThing` 的 ① 也走 `IsBodyKeyword` ⇒ **断句失效**，
   `EC28 Flawless Blade Champion`（卡面两条独立能力，成品卡图逐字核过）的 `Ecstasy 3:` 那半
   被**并进 `Slay` 的正文** ⇒ `Slay` 一杀人就白打一次随机攻击。**已一并修**（§三·②）。

### `A1471` —— `DA25` 的 `If it survives, heal 3 to it` 永不发生
账目原文（`:66`）：它是 `When …` 那条的**尾句**，而 `AddWhenTrigger` 拿的是**单段** ⇒
`TriggerBodyAt` 那条并句路**只服务「带正文关键词」那条路**，`When …` **没有通道**。

**按账目确认并修**：`CollectWhenTriggers` 原来 `foreach (seg in Split(Desc)) AddWhenTrigger(seg)`
—— 一段一段单独送，尾句落进 ② 的降费句式就 `return`。新增 `WhenBodyAt` 走**与 `TriggerBodyAt`
同一份停句判据**（`StartsAnotherThing`），⛔ 不另写一套。
⚠️ **实测面**：会因此多并到一句的**全池只有 `DA25` 一张** ⇒ 改不动别的卡（§三·③ 全池 diff 证实）。

---

## 三、实测对照（**真代码**，改前 → 改后）

探针 = 离线 net8 控制台（`UnityEngine` 30 行桩 + **只读**引用 `Core/*.cs` + `Data/CardDatabase.cs`），
跑全池 1126 张、逐卡 dump 出 `TriggerTexts` / `WhenTriggers` / `SpiritOps` / `OathOps` /
`AttackedOps` / `Auras` 的正文与每条 op（含 `verb/amount/payload/target/condition/cost`）。
`before` = `git show HEAD:…CardDef.cs`（**同一个探针**），`after` = 工作区。

### ① `A1469` 门 ①：合成卡 `keywords=[Rally]` · `desc=[Ecstasy 2: Gain +2 Attack]`

| | `TriggerOps("rally")` | `TriggerOps("ecstasy")` |
|---|---|---|
| **改前** | `[gain\|pay=+2 attack]` ← **错：Rally 把狂喜的正文吞了** | `[gain\|pay=+2 attack]` |
| **改后** | `null` ✅ | `[gain\|pay=+2 attack]` |

合成卡 `desc=[Give +1 Attack to all friendly troops. Friendly Beasts have "Ecstasy 2: Heal 2"]`：

| | `TriggerOps("rally")` |
|---|---|
| **改前** | `[give\|pay=+1 attack]`（门 ①-bis 也漏） |
| **改后** | `null` ✅（①-bis 认得出引号里那半是他们家的） |

⚠️ **真池 1126 张上，这两处门是「零行为差异」的**（改前也是 `null` 或先到先得兜住）——
**判别式只能靠合成卡**，见下。

### ② 同根因（`EC28`）：**真卡、真读数**

```text
改前  TRIG slay => [Give +1 [Might] to all friendly troops. Ecstasy 3: Attack a random enemy]
          op: give|pay=+1 might|tgt=all friendly troops
          op: forceattack|amt=0                 ← 多出来的一下（Slay 一杀人就白打）
改后  TRIG slay => [Give +1 [Might] to all friendly troops]
          op: give|pay=+1 might|tgt=all friendly troops
      TRIG ecstasy => [Attack a random enemy]   ← 归位（前后都有，本来就对）
```

### ③ `A1471`：`DA25 Techmarine`

```text
改前  WHEN 友方 attack vehicle => [gain 1 Quest Point]
          op: gainquest|amt=1|tgt=(玩家任务点)          ← 只有 1 条，heal 不见
改后  WHEN 友方 attack vehicle => [gain 1 Quest Point. If it survives, heal 3 to it]
          op: gainquest|amt=1|tgt=(玩家任务点)
          op: heal|amt=3|tgt=it|cond=it survives/targetsurvives   ← 多这一条
```

### ④ 全池回归（`before.txt` vs `after.txt`，各 10,088 行）

**逐行 diff 只有 6 行变化，全部落在上面 ①② 两张卡上** ⇒ 其余 **1124 张一字未变**。

### ⑤ 编译

两次 `dotnet build`（§六 那两个探针工程，含整个 `Core/*.cs` + `Data/CardDatabase.cs`）
**都是 0 error / 0 warning**。
⚠️ 另做了一次**单文件**语法核对（`csc` 只编 `CardDef.cs`）：该文件在「同一份 refs 下编 HEAD 版」
与「编工作区版」**错误画像完全相同**（各 33 条，全是 `CS0246` —— `EffectOp` / `UnitState` 等
**在别的文件里**的类型取不到，属预期）⇒ **我的改动没引入任何新的编译期问题**。
⚠️ 全量类型检查**没跑**（简报要求由调度台统一跑）。

---

## 四、没查清 / 没做的

1. 🔴 **`A1439` 的「两条路 `Source` 到底差在哪」没跑仪器复核** —— `ruleprobe` 的 dump
   （`Scan.cs` / `Program.Dump`）**一共不打 `Source` 这一列**（我把三个入口都翻了一遍），
   基线 `基线/out_baseline.txt` 也没有 ⇒ 只能「按代码读」。**要复核**：写一句
   `Console.WriteLine(op.Source)` 或跑一次带 `Source` 的 dump。
2. ⚠️ **`A1439` 的「统一语义」这一支我没做** —— 得改 `EffectText.ParseSegment`（**别人的文件**）。
   按简报红线**停手**，改走「写明两个口径」。
3. ⚠️ `A1489` 那一类「门 ①-bis」**没有真卡可证**（全池 diff 0 变化）⇒ 只报了合成卡读数（§三·①）。
4. ⚠️ **`RuleEngineTest.Run` 没过**（简报要求由调度台跑）。**建议钉的断言**见 §五。
5. ⚠️ `WhenBodyAt` **只在 `Desc` 那一侧并句** —— `keywords` 数组里的多句条目（如 `Jain Zar`）
   照旧一段一段送。今天没有卡因此受损（全池 diff 0），但**同族将来可能出现**，这一笔没做。

---

## 五、判别式（🧨 什么改动会把它弄红）

| 笔 | 🧨 改坏法 | 会红的东西 |
|---|---|---|
| `A1469` 门 ① | `IsBodyKeywordHead` 换回 `IsBodyKeyword`（或删掉 `StripParamSuffix` 的剥数字那步） | **合成卡**：`keywords=[Rally]` + `desc=[Ecstasy 2: Gain +2 Attack]` ⇒ `TriggerOps("rally")` 从 `null` 变成 `[gain]`。⚠️ **真池抓不到**（改前也是 null）⇒ 断言必须用合成夹具 |
| `A1469` 门 ①-bis | 同上 | 合成卡 2：`TriggerOps("rally")` 从 `null` 变成 `[give]` |
| **`EC28`**（同根因） | `StartsAnotherThing` ① 换回 `IsBodyKeyword` | **真卡**：`EC28.TriggerText("slay")` 从 `Give +1 [Might] to all friendly troops` 变回拖着 `… Ecstasy 3: Attack a random enemy`；`TriggerOps("slay").Count` 从 1 变 2 |
| `A1471` | `CollectWhenTriggers` 改回 `AddWhenTrigger(segs[k])`（不并句） | **真卡**：`DA25.WhenTriggers[0].Ops` 少掉 `heal`（`ConditionKind == targetsurvives`）；`Body` 从带尾句变回 `gain 1 Quest Point` |
| `A1439` | 纯注释 ⇒ **没有行为判别式** | 唯一检法 = 读那两处注释，核它说的与 `EffectText.ParseSegment` 一致（⛔ 不是「应该没问题」，是「这一笔本就没有可断言的产物」） |
| 顺手订正 | 数错就可能再写错 | 现读数：`Implemented` **68** · `RoutableTriggers` **22** · 两表共有 **21** · 差集 **47**（`spiritstone` 在表内不在 `Implemented` 里） |

**建议给 `RuleEngineTest.Run` 补的断言**（**落点是 `Editor/RuleEngineTest.cs` —— 别人的白名单，
本笔没写，只交规格**）：
1. `EC28`：`TriggerText("slay") == "Give +1 [Might] to all friendly troops"` 且 `TriggerOps("slay").Count == 1`。
2. `DA25`：`WhenTriggers[0].Ops` 里恰有一条 `Verb == "heal" && ConditionKind == "targetsurvives" && Amount == 3`。
3. `DA25`：`WhenTriggers[0].Body` 以 `"gain 1 Quest Point"` 开头**且**含 `"If it survives"`。
4. 合成夹具（门 ① / ①-bis）：照 §三·① 那两张卡造，断 `TriggerOps("rally") == null`。
5. 「不该动的」：`EC31 Chaos Rhino` 的 `TriggerOps("ecstasy")` 仍是那条 `deploy`（裸写路没被误伤）。

---

## 六、怎么复跑的（⛔ 没进工程）

```
D:/tmp/wf_e4_probe/    # 工作区版（after）：Stub.cs + Program.cs + wf_e4.csproj
D:/tmp/wf_e4_before/   # HEAD 版（before）：CardDef.before.cs（= git show HEAD:…）+ 同一个 Program.cs
```

- `<Compile Include="D:/4/Unity/MyGame/Assets/RuleEngine/Core/*.cs" />`（before 那份 `Exclude` 掉
  `CardDef.cs`、换成 `CardDef.before.cs`）+ `Data/CardDatabase.cs`；
  `Stub.cs` 只实现 `Debug` / `Random` / `Resources` / `JsonUtility` / `TextAsset`（照抄 E2 那份）。
- 跑法：`cd D:/tmp/wf_e4_probe && dotnet build -c Release && dotnet run -c Release -- D:/tmp/wf_e4_probe/after.txt`。
- 卡池 = `Assets/RuleEngine/Resources/cards_engine.json`（**现读 1126 张**）。
- ⚠️ **它编的是别人正在写的 `Core/*.cs`** —— 万一被半成品打断，症状是「错误全集中在别人的文件上」，
  那时**不要**去改别人（`MEMORY.md` 第十会话那条）。本次两次 build 都是 0 error。

---

## 七、顺手发现的（⛔ 一条都没顺手改白名单外的）

1. ⚠️ **`EC28` 的真缺陷**（= `A1469` 的同根因，**真卡可达**）：`Slay` 吞掉 `Ecstasy 3:` 那半
   ⇒ 一杀人就白打一次随机攻击。**已修**（在 `CardDef.cs` 内），改前/改后读数见 §三·②。
2. ⚠️ **账目 `A1439` 的措辞与代码不符**（「整句」那一半）—— 见 §二 `A1439`，**成稿只落在我文件的注释里**。
3. ⚠️ **`CardDef.cs:181` 的「− 本表 23 词 = 47 词」**：**23 是错的**（现核 22 —— 且是「两表共有 21」，
   因为 `spiritstone` 不在 `Implemented` 里）；**47 是验过的**。已就地订正。
4. ⚠️ **`Maulerfiend` 的三处注释都说「它的 `keywords` 里没有 `Ecstasy`」——现核 `['Ecstasy 5']`，
   已不成立**（`EcstasyX` doc / `CollectCompanionKeyword` doc / `AddTriggerOp` 注释）。已就地订正。
   今天真正靠**裸写路**收的是 **`EC31 Chaos Rhino`**（`desc` 里没有 `Ecstasy:` 头）。
5. ⚠️ **`ruleprobe` 的三个 dump 入口都不打 `Source`** ⇒ 「两条路的 `Source` 差在哪」这类问题
   **今天没仪器可证**（也是 §四·1 做不完的原因）。
6. ⚠️ 我留了两个**工作区外**的临时目录没删（`D:/tmp/wf_e4_probe` · `D:/tmp/wf_e4_before`；
   另有 `D:/_tmp_e4syn`（只编译检查用）与 `C:/Users/qjh36/AppData/Local/Temp/e4syn`）——
   删除操作被沙箱挡了，请调度台或用户自行清理。**不影响工程**（都在 `d:/4` 之外）。
