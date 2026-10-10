# `W` · `A1467` —— `ReLose` 的正则把句首 `All ` 吃掉 ⇒ 3 张卡「打不出去」

> 第十四会话（2026-10-10）· 执行代理（写手）报告。
> ⛔ 没跑 Unity、没动 git、没改正本、没碰黑名单；🔴 **本笔只改了【一个】工程文件**
> —— `RuleEngine/Core/EffectText.cs`（**一行正则** ＋ 一段注释）。
> **新建的文件只有本报告**（`资料/普查产出_第十四会话/W_A1467ReLose.md`）。
>
> **验收**：`TMPDIR=/tmp/wf_a1467 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
> **额外验法**：工程外 scratch 容器 `D:/tmp/wf_a1467/`（**只读**工程源码，`<Compile Include>` 用绝对路径）
> ⇒ 同一份驱动跑两遍（`core_before/` vs `core_after/`，**两份只差 `EffectText.cs` 一个文件**）。
>
> ⚠️ **本笔所有行为读数都是「夹具直调」**：走 `RuleCore.NewBattle` / `PlayTactic` / `PlayCard` 这些
> **引擎边界**，**没有**经过 `BattleDriver.LocalAct(...)` / `SimpleAI.ExecuteAction(...)` 两个记账口
> ⇒ 那些局面**不会被本地录像录到**。本笔也不为此负责（它只为拿读数）。
> ⚠️ **它不替代** Unity 下的 `RuleEngineTest.Run`（那条由调度台在同步点跑）。

---

## 一、根因与改法

### 1·1 一行正则（`Core/EffectText.cs:7239`，唯一改动）

```diff
-        static readonly Regex ReLose = new Regex(
-            @"^(?:all\s+)?(.+?)\s+loses?\s+(.+)$|^(?:all\s+)?loses?\s+(.+)$", RegexOptions.Compiled);
+        static readonly Regex ReLose = new Regex(
+            @"^(.+?)\s+loses?\s+(.+)$|^(?:all\s+)?loses?\s+(.+)$", RegexOptions.Compiled);
```

（另加 25 行注释：机理 · 受影响 3 张 · 改前/改后读数 · 判据出处 + 「另一条备选的 `(?:all\s+)?` 为什么照留」。）

**机理**：第一条备选的 `(?:all\s+)?` 把句首 `All ` **消费掉** ⇒ group 1（主语）只剩 `enemies`，
而 `:7206` 又把 group 1 **原样交给 `ParseTarget`** ⇒ `all` 这个词**永远到不了**
`ParseTarget` 里那句「`StartsWith("all ")` ⇒ `Count = 0`」（**`:7729`**，逐字现读）
⇒ 目标从「全体」退化成「挑 1 个」。

### 1·2 为什么改完**不会**动到 `gain` 那条（反向对照）与其它 `lose` 句

🔴 **两条备选【逐字】在 .NET 里 A/B 对拍过**（`D:/tmp/wf_a1467/Drv.cs` 的 `rx` 命令，
用的就是那两条 pattern 原文）：

| 输入 | 旧 alt1 group1 | 新 alt1 group1 |
|---|---|---|
| `all enemies lose stealth and camouflage` | `enemies` ❌ | **`all enemies`** ✅ |
| `all troops lose stealth` | `troops` ❌ | **`all troops`** ✅ |
| `all other enemies lose stealth` | `other enemies` ❌ | **`all other enemies`** ✅ |
| `all lose 1 attack` | `all` | `all`（**逐字相同**）|
| `lose 1 attack` / `loses 1 attack` | 走**第二条备选**（g3=`1 attack`）| **逐字相同** |
| `they lose stealth` / `it loses stealth` / `enemies lose stealth` | g1=`they`/`it`/`enemies` | **逐字相同** |
| `each of your units loses stealth` | g1=`each of your units` | **逐字相同** |

⇒ **删掉那个前缀，只影响「`All X lose Y`」这一种句子 —— 而它正是要修的那一种。**

🔴 **那个前缀【没有任何补偿作用】**（这一条是本笔新查的，推翻了「它也许在给 alt2 让路」的猜测）：
非贪婪组的**回溯**会让 `All lose X` 这种**无主语**写法照旧落进**第一条**备选（g1 = `all`，两条正则都一样），
所以它既没兜住无主语写法、也没别的用途 —— 它**只干了一件坏事**。

**`gain` 那条从来没这个问题**：`All enemies gain Stealth` 走 `TryGain` → `ReGain`（`:7242` 附近），
那条**不含** `(?:all\s+)?`，所以 `Raw=«all enemies» Count=0` 一直是对的 —— 本笔改前/改后**逐字未动**
（见 §二·3 的全池签名 diff：`gain` 一族的行**一行都没出现**）。

**第二条备选的 `(?:all\s+)?` 照留**：它那条上无害（`Lose X` 本来就没主语），而它是**共用**写法，
本笔不动它 —— ⛔ 别把它也删了（那会把「删干净」误当成「更彻底」）。
⚠️ **但它也不是**「`All lose X` 的兜底」—— 上面表里那条走的是**第一条**备选，别照字面推。

### 1·3 判据出处

- 主判据（这笔账的由来 + 全部读数）→ `资料/普查产出_第十四会话/W_A1457多目标.md` 顺手发现 1；
  同形判据 → 同目录 `W_A1436付费能力.md` §六·3。
- **卡面英文原文**（成品图，判据③）：`D:/2/Warpforge部队卡片/Astra Militarum/4计策/Warpforge_48_Recon-Operation.png`
  · `Aeldari/3部队/Warpforge_32_Dire-Avenger-Exarch.png` —— 卡面写的就是 `All enemies lose …`
  （**`All` 是卡面原字**，所以它必须进目标短语；这是「印刷品上的字」，可逐张核）。
  ⚠️ `UM_Tyrannic_War_Veteran` 的 PnP 卡图**本地没找到**（`Ultramarines/` 下**没有**这个文件名，
  `find -iname "*Tyrannic*"` 全树 0 命中，只找到 `Warpforge_34_Bladeguard-Veteran.png`）
  ⇒ 那张的卡面判据**只到 `card_stats.json` 的 `desc` 一层**，如实记在这儿。
- 同形写法（「全体」= 收全场列表、不是挑一个）→ 原版反编译
  `AbilityLogic__GetTargets.c:972-976`（`TargetsAffected` = `10 allFiltered` / `0xd2 allFilteredCountTraits`
  ⇒ `BattleManager__GetUnitsInPlayList(...)`），出处与读法见 `W_A1457多目标.md` §1·4。
- ⚠️ `rule_core.gd:3082` 只是**旁证**（那是**我们自己的 Godot 复刻**，不是原版）。

---

## 二、读数

### 2·1 `AM48 Recon Operation`（真战术卡，`PlayTactic` 真跑）

夹具：0 方督军@4 ＋ Caster@2；**1 方督军@4 ＋ FoeA@3 / FoeB@5 / FoeC@6**（血 9/7/4）。
**必须自己 `Place`** —— `NewBattle` 之后棋盘上**只有督军**（`Core/RuleCore.cs:528`）。

| 用例 | 改前 | 改后 |
|---|---|---|
| **A** 只有 FoeB(slot5) 带 Stealth | `PlayTactic(...,-1)` → **rc=18**；点 3 号格 → rc=0；日志「给了 **1** 个目标」；**FoeB 的 Stealth 仍在** | `PlayTactic(...,-1)` → **rc=0**；日志「给了 **3** 个目标」（FoeA · 督军 · FoeC）|
| **B** 敌方 3 个部队**全带** Stealth | `(...,-1)` → **rc=18**，`(...,3)` 也 **rc=18** ⇒ **整张卡打不出去**、一条日志都没有 | `(...,-1)` → **rc=0**（**能打出去了**）；日志「给了 **1** 个目标」（只剩督军，见 §四·1）|
| **C**（对照）都不带 Stealth | `(...,-1)` → rc=18；点 3 号格 → rc=0；**1** 个目标 | `(...,-1)` → **rc=0**；**4** 个目标（3 部队 ＋ 督军）|

### 2·2 `ASH32 Dire Avenger Exarch` / `UM_Tyrannic_War_Veteran`（Rally —— **部署真打**）

走的不是 `PlayTactic` 而是 `RuleCore.PlayCard` → `FireTriggerOnBoard(unit, Rally)`（`Core/RuleCore.cs:2363`）：

| 卡 | 该局敌人的 Stealth | 改前 | 改后 |
|---|---|---|---|
| `ASH32` | FoeB 一个带 | `PlayCard rc=0` · RALLY 日志「给了 **1** 个目标」 | `PlayCard rc=0` · 「给了 **3** 个目标」 |
| `ASH32` | 3 个全带 | rc=0 · 「给了 **1** 个目标」 | rc=0 · 「给了 1 个目标」（**不变**，见 §四·1）|
| `UM_Tyrannic_War_Veteran` | FoeB 一个带 | rc=0 · 「给了 **1** 个目标」 | rc=0 · 「给了 **3** 个目标」 |
| `UM_Tyrannic_War_Veteran` | 3 个全带 | rc=0 · 「给了 1 个目标」 | rc=0 · 「给了 1 个目标」（**不变**）|

⚠️ **Rally 那两半的 rc 改前也是 0** —— 它不经过「玩家点目标」那道闸，所以**改前不会「打不出去」**，
它丢的只是**目标张数**（静默少给）。**真正「打不出去」的只有 `AM48`（战术卡那一支）**。

### 2·3 全池解析签名对拍（**1126 张 `desc` ＋ 1497 条 `keywords` 原文**）

仪器 = 本笔自建的**带 `Target.*` 的**签名 dump（`D:/tmp/wf_a1467/sig_desc_{before,after}.txt`，2623 行）。
🔴 **为什么不能只用 `ruleprobe check`**：`Scan.cs` 的 dump **不打 `Target`**
（只有 `verb/chooseSrc/chooseWhat/chooseAct/amount/payload` ＋ 尾部七列）——
**本笔改的正是 `Target.Raw`/`Target.Count`，它一个字都看不见**（见 §二·4 的实测）。

| 项 | 读数 |
|---|---|
| **diff 行数** | **8 行**（`<`4 ＋ `>`4）= **4 条记录**（diff 按两侧行计） |
| 变了哪 4 条 | `ASH32\|Dire Avenger Exarch\|D` · `ASH32\|…\|**K2**`（它的 `keywords[2]` = `Rally: All enemies lose Stealth and Camouflage`）· `AM48\|Recon Operation\|D` · `UM_Tyrannic_War_Veteran\|Tyrannic War Veteran\|D` |
| **方向** | 4 条**逐字同形**：`T=[enemies]…n=1` → **`T=[all enemies]…n=0`**（`side=enemy,kind=any` 不变）；**其余 13 列一字未动** ✅ |
| **没变** | 另外 **2 张**「分段开头写 `All …`」的卡（`GOF_Krump_da_Gitz` · `SOR51 Conviction of Faith`，**都是 `gain`**）逐字不变；**含 `lose` op 的卡 9 张**（`AM48 / ASH32 / ASH_Orian_Laratharjos / DA30 / GOF98 / GSC74 / SAU55 / TL20 / UM_Tyrannic_War_Veteran`）改前=改后**除这 3 张外全同**；`Blind all enemies, and they lose …` / `… and they lose …` / `… and it loses …` 那些**句中**写法（走尾句 handler）**逐字不变** |

### 2·4 `ruleprobe check`（哨兵 ＋ 基线）

| 项 | 改前 | 改后 |
|---|---|---|
| **哨兵合计：失败** | **0 条** ✅ | **0 条** ✅ |
| 全池 / 解析差异 | 1120 张 · **差异 1 行** | 1120 张 · **差异 1 行** |
| 那 1 行是谁 | `SAU67\|Resurrection Vault` | **同一行** —— 与本笔无关（`EffectText.cs` 里两处点名 `SAU67` 的注释是 `A1447` 写手的在飞改动）⇒ **本笔给 `check` 加的差异 = 0 行** |

🔴 **两条仪器备注（都实测过，别当「全绿」读）**：
① **`check` 对 `Target` 是瞎的** —— 本笔改完，`check` 的「解析差异」**一行都没多**，
   是真改了、而它测不到（原因见 §二·3）。**本笔靠的是自建的 `Target` 签名 dump**。
② **`ruleprobe.sh` 会拿旧 dll 接着跑（`A1466`）** ⇒ 本笔**没有**在仓库里
   `cd 工具/ruleprobe && dotnet build`（那里**有别的写手在动** `工具/ruleprobe/**`，`A1459`），
   而是把 `Program.cs/Scan.cs/Stub.cs/*.cs` **只读拷**到工程外，`<Compile Include>` 指向
   **仓库外的一份 Core 快照**，`dotnet build` **退出码 0 / 0 警告 0 错误** 后才跑 ——
   **没有「旧 dll 假绿」这条缝**。

### 2·5 类型检查 / 行尾

| 项 | 读数 |
|---|---|
| **秒级类型检查** | `TMPDIR=/tmp/wf_a1467 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0** ✅ |
| **行尾** | 改前 `CRLF=0 / LF=8256` ⇒ 改后 **`CRLF=0 / LF=8256`**（纯 LF，**一行没翻**）✅ |
| **`git diff --numstat`** | `328/41`（对本笔动手前那次是 `303/40`）⇒ 本笔净增 **+25 / −1** = **24 行注释 ＋ 1 行正则**，与改动内容逐字对得上 |
| **碰了几个文件** | **1 个**：`Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs`（`git status --short` 里只有它是 `M`，且本笔之前它就已是 `M`） |

---

## 三、我没做的 / 判不了的

1. ⛔ **没修 §四·1 那条**（隐身单位被「全体」池子整批滤掉）—— 它落在 **`EffectResolver.AddSide`**
   （`Core/EffectResolver.cs:1500` 一带），**在黑名单上**，本笔**不许碰**。
2. ⛔ **没跑 Unity 的 `RuleEngineTest.Run`**（本笔是执行代理，Unity 入口由调度台在同步点跑）。
3. ⚠️ **`ASH32` / `UM_Tyrannic_War_Veteran` 的「改后真打」只覆盖到 Rally 触发**（`PlayCard` 部署那条路），
   **没覆盖**它们在别的入口（如 `FireTriggerAlways` / 教程链）里的表现 —— 但**解析层是同一个 op**（读数同形）。
4. ⚠️ **`UM_Tyrannic_War_Veteran` 的 PnP 卡图本地没找到**（见 §1·3）⇒ 那张的「卡面 `All`」只有
   `card_stats.json` 一层判据，**没有第二来源**。
5. ⚠️ **`All lose X`（无主语 + `All`）这个形状全池 0 张卡**，本笔只做了解析层对拍
   （改前=改后=**同一结果**：`un=[All lose 1 Attack]`、op 的 `Target=null`）——
   **没去追它为什么是 `null` 而不是「Subjectless 规格」**（不在本笔范围，也不影响任何真卡）。

---

## 四、顺手发现（一条一句话 + 出处；⛔ **一条都没顺手改**）

1. 🔴 **「全体」池子会把【隐身/伪装】的单位整批滤掉 —— 于是「反隐身」这类卡永远清不掉隐身**
   （**真缺陷、另一族、落点在黑名单上**）。机理：`ResolveTargets` 的池子经
   `AddSide(pool, 敌方, isFoe=true, …, caster=owner)`（`Core/EffectResolver.cs:928`），
   而 `AddSide` 里有一句 `if (isFoe && caster >= 0 && (u.Has(Stealth) || u.Has("camouflage"))) continue;`
   （`Core/EffectResolver.cs:1507-1508`）⇒ **隐身单位进不了「all enemies」这个集合**。
   本笔改后实测（`D:/tmp/wf_a1467/am48_after.txt` 用例 B）：`AM48` 现在**能打出去**（rc=0），
   但日志是「给了 **1** 个目标」（只剩督军）、**3 个带 Stealth 的敌人一个都没掉 Stealth**；
   `ASH32`/`UM` 的全隐身那一格同样停在「1 个目标」。⇒ **要做**（铁律 11），**但它不是本笔那一条**
   （本笔只治「`All ` 被正则吃掉」；这条治「隐身的根本进不了池子」）——
   `W_A1457多目标.md` 顺手发现 2 已记为同一件事（那边只测了 `SAU55`，那边判「判不了」）。
   🆕 **本笔补一条线索（仍是线索、⛔ 不是结论）**：原版**似乎**带着一个「这一下要不要把不可选中的单位也算进来」的
   **逐效果开关** —— `AbilityLogic__GetTargets.c:989-992` 给
   `BattleManager__GetUnitsInPlayList` 传了 **10 个实参**，其中第 7 个来自
   `*(undefined1 *)(param_2 + 0x1b)`；而下游 `BattleManager__GetUnitsInPlayPooledList.c:47-52`
   里那句判据正是 **`if ((param_6 != '\0') || (*(char *)(lVar3 + 0x65) == '\0'))`**
   （=「这个开关打开 **或** 单位身上那个 flag 没置位，才收它」）。
   ⇒ 要看的是「`+0x1b` 那个字段叫什么、谁把它置 1、反隐身那几张卡是不是置了 1」——
   ⛔ **本笔没查**（超出白名单与范围），**如实记成线索**。
2. ⚠️ **`W_A1457多目标.md` §五·1 写的「全池 6 张走 `lose`」比实际少** —— 实测签名里带 `lose` op 的是
   **9 张**（`AM48 / ASH32 / ASH_Orian_Laratharjos / DA30 / GOF98 / GSC74 / SAU55 / TL20 / UM_Tyrannic_War_Veteran`）。
   「6 张」大概是「**头句**走 `TryLose`」那个口径（另 3 张的 `lose` 在**尾句**里）。
   ⛔ 本笔**没去改那份报告**（不在白名单）。出处 = 本笔 `sig_desc_before.txt`。
3. ⚠️ **`RuleEngine/Core/EffectText.cs` 现在有 8256 行、`git diff` 已累积 328/41**（第十四会话多位写手同一文件）
   —— 本笔动手前=303/40、动手后=328/41 ⇒ 本笔净增 **+25/−1**。
   这是「**一个文件被多个写手串行接手**」的现状，记一笔供调度台判提交粒度。出处 = `git diff --numstat`。
