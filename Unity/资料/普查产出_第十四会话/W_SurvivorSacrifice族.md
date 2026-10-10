# W_SurvivorSacrifice族 —— `A1386` / `A1387` / `A1389` 三笔收口报告

> 第十四会话（2026-10-21）· 执行代理（写手）报告。
> **产出**：只改了 `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs`（**三处，同一族**）。
> **验收**：`TMPDIR=/tmp/wf_sursac bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
> **额外验法（本笔独有，两样都比 Unity 便宜）**：
> ① **离线探针真跑**（只编 `Core/*.cs` + `Data/SimpleAI.cs` + 自写驱动，⛔ 不改任何工程文件）
> ② **全池签名对拍**：把 `git show HEAD:…CardDef.cs`（改前）与工作区（改后）各编一份，
>    对 **1126 张卡**导出同一份「解析签名」再 diff ⇒ **逐字节无差异**（见 §四）。

---

## 一、`A1386` —— `CardDef.RoutableTriggers` 缺 `survivor` / `sacrifice`

### 结论

**做了，判据满足。** 两个词已进 `RoutableTriggers`；`Card.TriggerOps("survivor")` 与
`Card.TriggerOps("sacrifice")` 从**恒 `null`** 变成**收得下正文**，`FireTriggerAt` 那条路不再空转。

### 改动清单

| 位置 | 一句话 |
|---|---|
| `Core/CardDef.cs:206-233` | `RoutableTriggers` 末尾追加 `KeywordTable.Survivor, KeywordTable.Sacrifice`（**225 行**的判据/后果注释一并写在那一处） |
| `Core/CardDef.cs:325-347` | `BodyKeywords` 末尾追加同样两个词 —— **不是可选项**，理由见下「我做的判断」 |

### 证据（改前 → 改后；探针原始输出）

夹具 `new CardDef(…, keywords: […], subtype:"Infantry")`，读 `TriggerOps` / `TriggerText`：

| 卡面（`keywords`） | `TriggerOps("survivor")` | `TriggerOps("sacrifice")` |
|---|---|---|
| `["survivor 3", "Sacrifice: Gain +1 Attack"]` | `null` → `null`（这张卡**没有** `Survivor:` 正文，**应当**恒 null） | **`null` → `[1] {gain|+1 attack}`** |
| `["survivor 3", "Survivor: Gain +2 Attack"]` | **`null` → `[1] {gain|+2 attack}`** | —— |
| `["survivor 3"]`（`#24`/`#25` 的无正文档） | `null` → `null` ✓（**没被改坏**，那一格的判别力保住了） | —— |

`TriggerText` 同步：`"Gain +1 Attack"` / `"Gain +2 Attack"`（改前都是 `null`）。

### 我做的判断

**① 为什么必须**同时**进 `BodyKeywords`（这一条不在 A 表原文里，是现测出来的）。**

不进 `BodyKeywords` ⇒ `TriggerBodyAt` 并句时靠的 `StartsAnotherThing` → `IsBodyKeyword` **不会**在
`Survivor:` 处停 ⇒ 那半段会被并进**上一条**触发正文里。**实测读数**（探针「判断项」那一格）：

```
Desc = "Rally: Deal 3 damage to an enemy. Survivor: Gain +2 Attack"，keywords = ["Rally"]

                          TriggerText("rally")                                  TriggerOps("rally")
只补 RoutableTriggers  ⇒  "Deal 3 damage to an enemy. Survivor: Gain +2 Attack"  [2] {deal} {gain|+2 attack}   ← 静默吞并
再补 BodyKeywords      ⇒  "Deal 3 damage to an enemy"                            [1] {deal}                    ← 正确
互补之后 survivor 自己也拿得到：TriggerText("survivor") = "Gain +2 Attack"
```

⇒ 只补 `RoutableTriggers` 会让**同一张卡的 Rally 白拿一次 +2 攻**（`Rally` 的 op 表里多出一条
`{gain|+2 attack}`）。⚠️ 这个吞并**改前就存在**（`Rally:` 后面跟任何「可路由但不带正文」的词都会漏），
只是因为 `survivor` / `sacrifice` 今天池里 0 张卡用才没显形 —— **本笔是把 `survivor`/`sacrifice`
补成「可路由」之后，它们才成为唯一的漏点**，所以必须一并堵上。

**② 没有顺手把 `landing` 一起补进来** —— 见 `A1395`（`项目任务.md` §29·b）与文末「顺手发现 3」。

### 还差什么

- 🔴 **`Editor/RuleEngineTest_S14.cs:677-679` 会因此翻红（1 条）**：
  ```csharp
  CheckTrue(sacCard.TriggerOps(KeywordTable.Sacrifice) == null,
            "（如实记：`Sacrifice:` 那段**正文**今天收不下来 —— `RoutableTriggers` 里没有它；…）");
  ```
  它**断的就是改前的缺陷**（当时如实记的、不是期望）。**该文件在本笔黑名单里**
  （`RuleEngine/Editor/**` = 断言批、本会话产物）⇒ **由调度台另派**。
  实测：我把那 29 个方法整份离线跑了一遍 ⇒ **271 通过 / 1 失败**，唯一那条失败就是它。
- A 表原文还要求「**补上「攻 +1 / +2」那半的断言**」—— 同样落在那个文件里，**未做**。
  断言该写什么已经可达（见 §三的读数），可直接照写。
- `W_断言_引擎.md` §四①（「今天不可达」那一段）与 `RuleEngineTest_S14.cs:660-670 / 795-797` 的
  **头注**现在都过期了 —— 一并交给调度台。

---

## 二、`A1387` —— `KeywordTable.Parse` 同名「后写覆盖」把 `survivor 3` 冲成 1

### 结论

**做了，判据满足。** 判据改成「**同一个名字里，印了数字的那一条压过没印数字的**；两条都印了
（或都没印）⇒ 照旧后写覆盖」。`["Survivor 3", "Survivor: Gain +2 Attack"]` 从 **1 → 3**。

### 改动清单

| 位置 | 一句话 |
|---|---|
| `Core/CardDef.cs:3059-3103` | `Parse` 里加一个 `numSeen`（已经收到过「印了数字」那一条的名字）＋ 一句 `if (!printed && numSeen.Contains(name)) continue;`；判据复用现成的 `HasNumber`（⛔ 没另写一份切分） |

### 证据（`Parse` 逐条夹具；`旧` = 改前口径的等价实现，`现` = 工作区实现）

| 原始 `keywords` | 旧 | 现 |
|---|---|---|
| `["Survivor 3", "Survivor: Gain +2 Attack"]` | **1** ❌ | **3** ✅ |
| `["Survivor: Gain +2 Attack", "Survivor 3"]` | 3 | 3（**不变**） |
| `["Sacrifice 2", "Sacrifice: Gain +1 Attack"]` | **1** ❌ | **2** ✅（同族、一并修好） |
| `["Tide 2", "Tide"]` | **1** ❌ | **2** ✅（同形、非本族的旁证） |
| `["Rally 2", "Rally 3"]` | 3 | 3（两条都印了数字 ⇒ 行为不变） |

端到端（同一张 `["survivor 3", "Survivor: Gain +2 Attack"]` 的卡，打它 5 伤）：
**救回后生命 `1` → `3`**（`UseSurvivor` 写回 `Min(CurrentSurvivor, MaxHealth)`）。

**全池对账（两处独立量过，都 0）**：
- Python 侧：1126 张卡逐张找「同名关键词」，**命中 0 张**（也即今天**没有任何卡**会走到这条新分支）。
- C# 侧：`ParseOld` 与 `KeywordTable.Parse` 在 1126 × 每一张卡的 raw 上逐 `(卡, 键)` 比 ⇒ **0 处不同**。
- ⇒ 与 `A1342` 同一个形状：**这是收口，不是行为改动**。

### 我做的判断

- **为什么不是「相加」**：原版一条 `CardTrait` 就是一个 `value` 字段
  （`CardTrait__GetNewTrait.c`：`+0x18` = traitId · `+0x1c` = value），
  取值是 `EntityScript__GetCurrentTraitValueWithModifiers.c` 在列表里 **`List.Find` 取第一条命中** ——
  **没有累加这回事**。⇒ 我们的字典模型只能在多条里**挑一条**，所以判据写成「谁有资格说话」，
  而不是「加起来」。
- **为什么是「印了数字的压过没印的」，而不是「第一条赢」**：原版里「数值 trait」与
  「带正文的 ability」本来就住在**两张不同的表**里（`CardTraitCollection` 的 trait 列表 vs ability 列表），
  **根本不会撞**；是我们把两者拍平进了同一个 `keywords` 数组，才产生同名。
  ⇒ 正文那一条**本来就不该参与「值几」的表决**（它压根没有值）。「印了数字的压过没印的」正好表达这件事。
- ⚠️ **一处如实标着的未定**：**两条都印了数字**时仍取**后写**（原版 `Find` 取**第一条**）。
  今天池里 0 处可达（`["Rally 2","Rally 3"]` 那种写法一张都没有），要改口径得先有卡面判据。

### 还差什么

无（本笔自足）。⚠️ 唯一「不干净」的是：它今天**不可观测**（全池 0 张卡带这两个词），
属于「一有卡就会静默错」那一类 —— 与 `A1386` 同因。

---

## 三、`A1389` —— `Survivor:` / `Sacrifice:` 的「事件」与「正文」不同步

### 结论

**`Survivor:` 那一半：完全修好（事件 + 数值都到）。**
**`Sacrifice:` 那一半：正文现在「收得到、也执行」了，但落点仍然错** —— 被救那一刻单位 `Health ≤ 0`
⇒ `IsAlive == false` ⇒ **无主语 op 的目标静默回落到「己方全体」，实测加到了督军身上**。
根因在 `EffectResolver.ResolveTargets`（**本笔黑名单**），**如实报，未修**。

### 改动清单

**没有为本笔单独加改动** —— 它由 §一 那一处（`RoutableTriggers`）直接带出来：
`RuleCore.CleanupDeaths` 的幸存者支路走的是 `FireTriggerAlways`
（`RuleCore.cs:5320` / `:5344`），而它在 `u.FxOps(kw) != null` 时会**转发给 `FireTriggerAt`**
（`RuleCore.cs:6968-6979`）⇒ 正文一收下来，两半立刻就同步了。

### 证据（离线真打一局；`ctx.Active == 1`、P1 的单位被打自己 5 伤）

| 读数 | 改前 | 改后 |
|---|---|---|
| `["survivor 3", "Survivor: Gain +2 Attack"]` 救回后 `Attack` | 2 | **4** ✅（`+2` 真加上了） |
| 同上、救回后 `Health` | 1（`A1387` 的坑） | **3** ✅ |
| `EvtKind.Trigger{survivor}` | 1 | 1 |
| `["survivor 3", "Sacrifice: Gain +1 Attack"]` 救回后**施放者自己**的 `Attack` | 2 | **2** ❌（应为 3） |
| 同上、**P1 督军**的 `Attack` | 2 | **3** ❌（**错落到督军身上**） |
| 同上、`Trigger{sacrifice}` 事件的 `Effect` 字段 | （无正文分支） | `"Gain +1 Attack"`（**确实走了 `FireTriggerAt`**） |
| 同上、事件日志 | —— | `S14_Sac 触发 SACRIFICE：「Gain +1 Attack」` / `SACRIFICE：「Gain +1 Attack」给了 1 个目标` / `S14_Sac 的 **Survivor 3** 生效：生命 -3 → 3` |

**对照格**（用来把「正文根本没跑」与「自目标被挡掉」分开）：把正文换成
`Give +1 Attack to a friendly unit`（打**别人**）⇒ 邻居 `Attack 2 → 3` ✅
⇒ **正文确实在跑，只有「目标是自己」那一档落错**。

**根因（亲读代码）**：`EffectResolver.cs:683-697`

```csharp
if (spec.Subjectless)
{
    if (source != null && source.IsAlive) list.Add(source);      // ← 被救那一刻 Health=-3 ⇒ IsAlive=false
    else if (chosen != null && chosen.IsAlive) list.Add(chosen);
    else AddSide(list, ctx.Players[owner], false, …);            // ← 静默回落到「己方全体」
    return list;
}
```

`UnitState.IsAlive` = `Health > 0`；被救那一刻是 `-3`；而 `RuleCore.CleanupDeaths` 里
**献祭那一跳排在 `UseSurvivor` 之前**（照原版 `CheckIfDead.c:152-158` 在 `:160-165` 之前）⇒
**必然**落进 `else`。`AddSide` 把「己方全体里活着的那几个」收进来 —— 台上只有督军 ⇒ 就是督军。

### 我做的判断

- **判据取原版**：`CardScript__CheckIfDead.c` 那一段的守卫是
  `health < 1` ∧ `*(int*)(param_1 + 0x228) ∈ {2, 0xf, 3, 0x11}`（= **在场上**）∧ `CurrentSurvivor >= 1`；
  `TriggerSacrifice` 就在这个块**里面**、`UseSurvivor` **之前**。
  ⇒ 原版那一刻这张卡**在场上**（`cardState` 没变），它的 ability 打「自己」**应当**还是自己。
  ⚠️ **未核的一半**：原版 `CardAbility` 对「无主语正文」的默认目标**到底是自己还是别的**，
  我**没查到判据** —— 这两个词今天池里 **0 张卡**，**没有卡面可核**，也找不到原版卡数据。
  ⇒ 所以这条我按「**可疑 / 与原版不符**」报，⛔ 没有把结论写成「原版一定打自己」。
- **为什么不顺手修**：要改的是 `EffectResolver.ResolveTargets`（本笔**黑名单**，且简报写明
  「若你判定非动白名单外的文件不可 ⇒ 停手、写进报告」）。

### 还差什么（**新账**，按铁律 14 应插队）

1. 🔴 **`ResolveTargets` 的 `Subjectless` 支在 `source` 不 `IsAlive` 时静默退回「己方全体」** ——
   注释写着「并把这件事说清楚」，但**那一支里一行日志都没有**（静默换语义）。
   建议判据：`source != null`（只是死了）与 `source == null`（战术卡那条路）**要分开**，
   前者**不该**退回全体。影响面不止 `sacrifice`：**任何「单位已死才触发的触发」+ 无主语正文**
   都会走这一支（`Backlash` 那一族同形）。出处 = `EffectResolver.cs:685-696`。
2. 上面那条**没有卡面判据可核**（0 张卡）⇒ 建议与 `A1395`（同样「0 张卡、只能靠反编译定案」）
   一起派一个**只读查证**：回 `d:/2/tools/decomp_full/` 读原版 ability 的默认目标解析，
   再定我们照哪一支。

---

## 四、本笔怎么验的（**下批可复用**）

### 4·1 离线探针（真跑，≈2 秒一轮）

- 目录：**`D:/tmp/wf_sursac/probe/`**（`sursac.csproj` + `SursacProbe.cs`；`Compile Include` 全是**只读**引用，
  ⛔ 工程文件一个没改）：`工具/ruleprobe/Stub.cs` ＋ `Core/*.cs` ＋ `Data/SimpleAI.cs`
  ＋ `_tmp_as14/probe/S14Harness.cs`（复用它的夹具原语与 `CardDatabase` 壳，**只读**）
  ＋ `Editor/RuleEngineTest_S14.cs`（**只读**，用来量「哪几条断言会翻」）。
- 跑：`cd /d/tmp/wf_sursac/probe && dotnet build -v q && dotnet run --no-build`
  （结果同时写 `bin/Debug/net8.0/sursac_result.txt` 并打到 stdout）。
- 读数：**S14 29 个方法 ⇒ 271 通过 / 1 失败**，失败那条 = §一「还差什么」里点名的那一条。

### 4·2 全池签名对拍（**改前 vs 改后，1126 张逐字节比**）

- 目录：**`D:/tmp/wf_sursac/cmp/`**。做法：`git show HEAD:Unity/…/Core/CardDef.cs` 导出改前版本
  （`old/CardDef_old.cs`，**只读 git，没动工作区**）⇒ 两个 csproj 各编一次
  （旧版靠 `<Compile Include="Core\*.cs" Exclude="…\CardDef.cs" />` 排除新文件）
  ⇒ 各自导出同一份签名 ⇒ `diff`。
- 签名内容（`Sig.cs`）：每张卡的 `id|name` ＋ ①`Keywords` 全部键值（排序）②`TriggerTexts` 里每个词的
  **op 条数 + 正文原文** ③`NumericKeywords` ④`UnparsedEffects`。
- **结果：`1126` 行 vs `1126` 行，`diff` 无任何输出。**
  ⇒ 本笔三处改动对**当前卡池**是**零行为改动**（纯收口），也顺带覆盖了「有没有把别的卡解析弄坏」。

### 4·3 类型检查

`TMPDIR=/tmp/wf_sursac bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
（本笔期间**没有** `ruleprobe` 的构建在跑，所以**没出现**简报里说的那类假错。）

⚠️ **本笔没有跑任何 Unity 自检**（简报禁止；同一工程同一时刻只能一个实例）。
`RuleEngineTest.Run` 那一条由调度台在同步点跑 —— 见 §一「还差什么」里预期的 1 条红。

---

## 五、顺手发现（**一条一句话 + 出处**；⛔ 一条都没顺手改）

1. 🔴 **`EffectResolver.ResolveTargets` 的无主语支在「施放者已死」时静默退回「己方全体」**，
   且那一支**没有任何日志**（注释却写着「并把这件事说清楚」）——
   实测把 `Sacrifice: Gain +1 Attack` 加到了**督军**身上。出处 = `Core/EffectResolver.cs:683-697`。
   （**已单列成新账**，见 §三「还差什么」1；要动 `EffectResolver.cs`。）
2. ⚠️ **`BodyKeywords` 的「带正文关键词唯一才敢认」那道闸有已知取舍**：进了本表之后，
   一张 `keywords = {"Survivor 3"}` 且 `desc` 是**裸写正文**的卡，会被认成「`desc` 就是 `survivor` 的正文」。
   今天 0 张卡命中（`cards_engine.json` 整条记录文本扫 `surviv` / `sacrific` 均 0 命中，8 处 `surviv`
   全是正文里的 `survives`、1 处 `sacrific` 是卡名 `TAU65 Valued Sacrifice`）。
   ⇒ 与 `Ecstasy` 同一个既有取舍，**如实标着**，判据不足不敢收窄。出处 = `Core/CardDef.cs:338-345`。
3. ⚠️ **`landing` 不在 `Prefixes`、也不在 `RoutableTriggers`** ⇒ 卡面写 `Landing:` 会被 `Normalize`
   静默丢掉。`RuleCore.cs:4564-4573` 有一条明确 hand-off（「接手 `CardDef.cs` 的那一笔请把它挪成
   `KeywordTable.Landing`，并按需要登记 `Prefixes` / `RoutableTriggers`」）—— **本笔没做**：
   它属于 `A1395`，而 `A1395` 还卡着第二道坎（同步模型下怎么表达「取消一次已声明未结算的攻击」），
   **只挪常量、不挪 `RuleCore.LandingKeyword`，就会留下两份定义**（正是铁律 5 要防的那种）。
   建议：`A1395` 整笔一次做完。出处 = `Core/RuleCore.cs:4560-4573` · `项目任务.md` §29·b `A1395`。
4. ⚠️ `EffectText.cs:1953` 的注释还写着「`CardDef.RoutableTriggers` 那 **8** 个」——
   该表早已超过 8（本笔又 +2），是**会随每次改动过期的活数字**。出处 = `Core/EffectText.cs:1953`。
   （同族：`CardDef.RoutableTriggers` 自己的头注里也有「那 8 个」这种写法。）
5. ✅ **顺带修好一条同族缺陷**（未单独立账）：`Sacrifice 2` + 正文那条以前也会被冲成 **1**，
   本笔随 `A1387` 一并变 **2**（读数见 §二）。今天同样 0 张卡可达。
