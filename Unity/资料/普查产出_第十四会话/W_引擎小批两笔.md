# W · 引擎小批两笔（`A1404` / `A1427`）

> 第十四会话 · 写手报告。两笔都是「判据齐的小件」，**只动三个白名单文件**。
> ⚠️ **本笔没有跑任何 Unity 自检**（简报禁止）—— `RuleEngineTest.Run` 由调度台在同步点跑。
> 本报告里的读数全部来自 **① 秒级类型检查** ＋ **② 工程外的离线驱动**（`D:/tmp/wf_eng2/`）。

---

## 一、`A1404` —— `CardDef.IsBodyKeyword` 开成 `public`，表现层转调它

### 结论

**做完。** `CardDef.IsBodyKeyword` 由 `private` 开成 `public`；`CardText.IsBodyKeyword`
（表现层那份「同一概念」的判定）**不再自己遍历 `BodyKeywords`**，改成**转调它** ⇒ 判据只此一份。
**全池口径等价**（旧口径 OrdinalIgnoreCase vs 新口径精确等值：**差异 0 个**，见 §一·2）。

### 改动清单

| `文件:行号` | 一句话 |
|---|---|
| `RuleEngine/Core/CardDef.cs:583-590` | `static bool IsBodyKeyword` → **`public static bool IsBodyKeyword`**（加 doc：谁在转调、为什么、传入的必须是规范键） |
| `CardPresentation/Core/CardText.cs:309-323` | `IsBodyKeyword` 的函数体由「`foreach (BodyKeywords) OrdinalIgnoreCase`」改成 **`return RuleEngine.CardDef.IsBodyKeyword(canonicalKey);`**（保留 `null/空` 守卫；doc 里记明改前为什么自己遍历） |

⚠️ 只做了「开成 public + 转调」这一件事：`BodyKeywords` 表本身、`CardDef` 里那 4 个内部调用点
（`:526` / `:544` / `:552` / `:1262`）**一个字没动**（它们本来就调这个方法）。

### 证据（改前 vs 改后读数）

离线驱动（`D:/tmp/wf_eng2/{before,after}`，两个项目只差这两个文件的快照，round-trip 逐字节核过）：

| 读数 | 改前 | 改后 |
|---|---|---|
| 反射 `CardDef.IsBodyKeyword` 的 `IsPublic` | **False** | **True** |
| `CardDef.BodyKeywords` 条数 | 21 | 21（未动） |
| 全池关键词行（`texts.tsv` 的 `#k` 行） | 1021 条 ⇒ 归一后 1019 个 | 同左 |
| 归一后**不是全小写**的键 | **0** 个 | 0 个 |
| 旧口径（改前 `CardText` 那份）vs 新口径（`CardDef.IsBodyKeyword`）**差异** | —— | **0 个** |
| 命中「带正文」那一档的规范键（去重） | 19 个 | 19 个（同一批） |

- **命中表（19 个）**：`agenda ambush artifice backlash codex cruelty duty ecstasy ferocity mob
  penitence pray rally regiment slay stimulation strike teleport uprising`
- **口径差异只在「非规范输入」上现形**（`'Rally'`：旧 True / 新 False）——
  「真实输入到不了这里」的**判据**：`_keywords` 的键一律出自 `KeywordTable.Normalize`
  （`CardDef.cs:3033` 那句 `ToLowerInvariant`）或全小写常量（`:700` / `:706` / `:1342` 的 `head`）；
  实测全池 **1019/1019 全小写**（上表第 4 行）⇒ **两种口径对现有输入等价**。

**「跨类取得到」的硬证据 = 秒级类型检查**：`CardText.cs` 在**运行时程序集**里，
`TMPDIR=/tmp/wf_eng2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**
（若仍是 `private`，`CardText.cs:322` 那一行**必然编不过**）。

### 还差什么 / 全库还有几处在「遍历同一张表」

- `grep -rn "BodyKeywords" Unity/MyGame/Assets --include=*.cs`：除 `CardDef.cs` **自己**之外，
  **代码里一处都没有**（其余命中**全是注释/文档字符串**：`CardText.cs` · `Editor/BattleScene.cs:933`
  的一条待裁注释 · `RuleEngineTest*.cs` 的说明文字）。
  ⇒ **「遍历同一张表」的只有表现层那一处，已改掉；其余 0 处未动也不需要动。**
- 还差：Unity 下的 `RuleEngineTest.Run`（调度台跑）。

---

## 二、`A1427` —— `destroy` / `blind` 补 `instead` 贴点

### 结论

**做完。** `TryDestroy` / `TryBlind` **各加一行 `PeelInstead(ref tok)`**（现成的唯一剥离口，
`EffectText.cs:4867-4882`），位置与 `TryDeal` / `TryStun` / `TryHeal` **同形**：
**先 `SplitAndTail` 切掉尾句，再对「头句的目标段」摘** ⇒ 尾句里那个 ` instead` 仍归尾句
（它会在 `Finish` 递归解尾句时由那个 handler 自己摘）。

### 改动清单

| `文件:行号` | 一句话 |
|---|---|
| `RuleEngine/Core/EffectText.cs:5193-5200` | `TryBlind`：`SplitAndTail(tok, …)` 之后加 **`if (PeelInstead(ref tok)) op.Instead = true;`**（在 `^(\d+\|two\|three)` 取数量**之前**） |
| `RuleEngine/Core/EffectText.cs:5232-5239` | `TryDestroy`：`SplitAndTail(tok, …)` 之后加同一行（在 `SplitTargetList` **之前**） |

⚠️ **为什么摘 `tok`（目标段）而不是整个 `low`**：`blind … instead and <尾句>` 这类句子里，
` instead` 可能属于**尾句**；先切尾句再摘头句 = 尾句不会被误摘（`TryDeal`/`TryStun`/`TryHeal`
三条路都是这个次序）。`PeelInstead` 对不含那两种写法的句子**是纯 no-op**（不改串、返回 false）。

### 证据（改前 vs 改后读数）

**(1) 解析层**（离线驱动 `out_run.txt`，逐条 `Parse`）：

| 句子 | 改前 | 改后 |
|---|---|---|
| `2 ☀: Destroy an enemy troop. Destroy 2 enemy troops instead` | op[1] `instead=0` · tgt=`'2 enemy troops instead'` | op[1] **`instead=1`** · tgt=`'2 enemy troops'` |
| `2 ☀: Blind a random enemy. Blind 2 random enemies instead` | op[1] `instead=0` · tgt=`'random enemies instead'` | op[1] **`instead=1`** · tgt=`'random enemies'` |
| `Destroy 2 enemy troops. Destroy an enemy troop instead` | op[1] `instead=0` · tgt=`'an enemy troop instead'` | op[1] **`instead=1`** · tgt=`'an enemy troop'` |
| `Blind 2 random enemies. Blind a random enemy instead` | op[1] `instead=0` · tgt=`'a random enemy instead'` | op[1] **`instead=1`** · tgt=`'a random enemy'` |
| 句中形：`Blind a random enemy instead and give it Blind` | op[0] `instead=0` · tgt=`'a random enemy instead'` · tail=`'give it blind'` | op[0] **`instead=1`** · tgt=`'a random enemy'` · tail **一字不变** |
| `Destroy it instead`（**没有可替换的那条**） | `partial`（半懂）· tgt=`'-'` | op tgt=`'it'` · `instead=1`（不再半懂） |

**(2) 反向对照（改前改后**逐字**相同）**：
`Blind an enemy troop and Give +2 Attack to a friendly troop instead`（**该由 `give` 摘**——
实测两边都是 op[0] `instead=0` / op[1] `instead=1`，**尾句没被头句抢走**）·
`Destroy an enemy troop and deal 2-3 damage to adjacent units`（`Methodical Destruction`）·
`Destroy a friendly troop and a random enemy troop`（`Summary Execution`，`SplitTargetList` 那条）·
`Blind all enemies, and they lose Stealth and Camouflage`（`Solar Pulse`）—— **四句两边逐字相同**。

**(3) 结算层真打一发**（放 4 个敌人；`RuleCore.PlayTactic`）：

| 夹具（战术卡文本） | 改前 | 改后 | 卡面正解 |
|---|---|---|---|
| `Destroy an enemy troop. Destroy 2 enemy troops instead` | 死 **3** 个（两条都跑：1+2） | 死 **2** 个 | **2**（后者替换前者） |
| `Destroy 2 enemy troops. Destroy an enemy troop instead` | 死 **3** 个（2+1） | 死 **1** 个 | **1** |
| `Blind 2 random enemies. Blind a random enemy instead` | 失明 **2** 个（1+1） | 失明 **1** 个 | **1** |

**(4) 全池签名对拍（回归）**：`D:/tmp/wf_esc/texts.tsv`（1126 张卡的 `desc` ＋ 全部 `keywords` 原文
= **2147 条**）逐条 `Parse`，签名含每条 op 的 `verb/amount/payload/cost/CostKind/CostShared/Instead/
condition/Target.Raw` ＋ `unparsed`/`partial` 清单 ——
**改前 vs 改后 `diff` = 0 行**（`sig.diff` 0 行 / 退出码 0）。
⇒ 真池**一个字都没被这一改动到**（与台账「`destroy`/`blind` 那两笔今天 0 张可达」一致）。

**(5) 真池 `instead` 现核（独立读数，与台账互证）**：全池 **1246** 个 op 里 `Instead=1` 的 **12** 个，
动词分布 = `deal` ×7 · `give` ×2 · `gain` ×1 · **`destroy` ×2**（`SW55` / `TAU53`）· `blind` **0**；
带条件的 **10** 个（走 `TryIf`，`Cond` 非空）· **无条件 2 个**（`SOR6` / `SOR49`，都是 `give`）。
⇒ **与台账口径完全吻合**；`SW55`/`TAU53` 那两个 `destroy` 的签名**改前改后一字不变**（走条件句那条路）。

### 还差什么

- Unity 下的 `RuleEngineTest.Run`（调度台在同步点跑）——**离线驱动不替代它**。
- 真池今天 **0 张可达**（`destroy`/`blind` 两句真卡都走条件句），本笔是**按铁律 11 补齐**，
  不是「修了一张现有的卡」。

---

## 三、怎么复现这些读数（**下批可复用**）

- **离线驱动**：`D:/tmp/wf_eng2/`（**工程外**，临时件）。
  `after/after.csproj`（吃工作区 `RuleEngine/Core/*.cs`）·
  `before/before.csproj`（`Exclude` 掉 `EffectText.cs`/`CardDef.cs`，改吃 `../before/` 的**动手前快照**）。
  跑：`cd /d/tmp/wf_eng2/{after,before} && dotnet build -v q && dotnet run --no-build -- run`
  ⇒ 产物 `out_run.txt`；另有 `siga` / `sigb <tsv>` 两种模式（全池签名）+ `a1404` / `a1427` 单跑。
- **快照怎么拍的**：`D:/tmp/wf_eng2/mk_before.py` —— 把本笔新加的那三段**摘掉**得 `before/`，
  再**原样放回**与工作区文件**逐字节比对**（round-trip 一致才继续）。
  ⚠️ 用的是「工作区快照」而不是 `git show HEAD:` —— 那两个文件**本来就带着别的会话的未提交改动**，
  拿 HEAD 当「改前」会把别人的改动一起算进差异里。
- **类型检查**：`TMPDIR=/tmp/wf_eng2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- **行尾**：`EffectText.cs` **纯 LF**（`0 / 8055`）· `CardDef.cs` **纯 LF**（`0 / 3155`）·
  `CardText.cs` **纯 CRLF**（`617 / 617`）—— 三个文件改完都没翻（`b.count(b'\r\n')` vs `b.count(b'\n')` 现核）。

---

## 四、顺手发现（**一条一句话 + 出处**；⛔ 一条都没顺手改）

1. ⚠️ **还有 11 个 handler 用 `(.+)$` 取目标、且没有 `PeelInstead`**：
   `TrySpendSpirit` / `TryEachUnitDeal` / `TryForceAttack` / `TryLowerCost` / `TryReanimate` /
   `TryCostWhenStub` / `TryDeploy` / `TryReturn` / `TryGiveInner` / `TryLose` / `TryGain`
   （判据 = 逐块扫 `EffectText.cs` 的 `static EffectOp Try*`，块内含 `(.+)$` 且不含 `PeelInstead`）。
   ⚠️ **它与 `deal`/`heal` 那族的失败形态不同**：那三个是 `$` 锚住了整条**结构**⇒整条失配（落 `unparsed`）；
   这 11 个是「目标串被污染」（仍能匹配、`ParseTarget` 常还能解出东西 ⇒ **静默**）。
   真池今天 **0 张可达**（§二·(5)：12 个 `instead` op 的动词只落在 `deal`/`give`/`gain`/`destroy`）。
   **要不要按铁律 11 逐个补齐 = 请调度台裁**（`A1427` 的台账只点名了两个 handler，我没扩大）。
   出处 = `RuleEngine/Core/EffectText.cs`（各 `Try*` 块）+ 本报告 §二·(5)。
2. ⚠️ **离线夹具的坑：`PlayTactic(ctx, p, handIdx, -1)` 对「要选目标」的卡恒返回 `18`**
   （`ErrNoTargetAvailable`）—— 看起来像「这张卡坏了」，实际是 `CanPlayTactic` 要求给格位
   （`EffectResolver.cs:1120`：`targetSlot < 0 || !BoardSpec.IsValid` ⇒ 18）。我第一版 `destroy`
   夹具就是这么拿到「rc=18 / 死了 0 个」的**假读数**，改成 `targetSlot=3` 才现出真形状（3 → 2）。
   出处 = `RuleEngine/Core/EffectResolver.cs:1120-1122` + `D:/tmp/wf_eng2/Driver2.cs` 的 §③。
3. ✅ **`CardText.IsBodyKeyword` 那一层包装留着了**（没删）：它现在只剩「判空 + 转调」，
   留着是为了保住 `A1375` 那条「用途只此一处」的注释锚点与 `KeywordSegment` 的单调用点
   （`CardText.cs:290`）。若调度台认为该直接内联掉，是一行的活。
4. ⚠️ **同一批 11 个 handler 里 `TryGain` 与 `TryGive` 是两套**：真池那两个**无条件** `instead`
   op 都属于 `give`（`SOR6` / `SOR49`），而 `gain` 那一个（`DA37` op[2]）**带条件**走了 `TryIf` ——
   也就是说 `TryGain` 这条路的贴点**至今没有任何真卡覆盖**，它的行为未经实卡检验。
   出处 = §二·(5) 的 12 个 op 清单。
