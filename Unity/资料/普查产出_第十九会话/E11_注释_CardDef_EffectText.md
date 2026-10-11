# E11 · 补注释 —— `RuleEngine/Core/CardDef.cs` + `RuleEngine/Core/EffectText.cs`

> 第十九会话 · 2026-10-11
> 任务：给这两个文件里**缺 doc 注释的公开成员**补 doc，并把**已有 doc 但缺 `<param>`**（`CS1573`）的补齐。
> 🔴 **纯注释、零行为改动。**

---

## 一、改动清单（行号 = 改完之后的最终行号）

### `CardDef.cs`（`d:/4/Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs`）

| doc 行 | 声明行 | 补了什么 |
|---|---|---|
| 37–43 | 44 | `Id`：**不是卡名**（现核 0/1126 相等、3 个重名）⇒ 键一律用它 |
| 45 | 46 | `Name`：英文名；重名是真的 |
| 47–53 | 54 | `Type`：四类 + **它决定「谁走路」**；多处按字符串相等判卡类 |
| 55–60 | 61 | `Desc`：`EffectText` 的**唯一输入**；含义随卡类变（自设计卡是风味文字） |
| 62–66 | 67 | `Rarity`：五种取值；决定「能带几张」（`DeckRules.CopyLimit`） |
| 68–72 | 73 | `Faction`：英文枚举名，与 `CardbackTable.army` 逐字同名 |
| 105–109 | 110 | `Cost`：**印的基准值**，真正付多少看降费/加费那几族，判据只 `RuleCore.CostOf` |
| 111–116 | 117 | `Attack`：近战；与远程**两套独立数值**；「近战还是远程」判据只 `SimpleAI.UseRanged` |
| 118–121 | 122 | `Health`：**与护甲不是一回事**（银盾 = 护甲、绿框 = 生命） |
| 508–522 | 523 | `TriggerText`：与 `TriggerOps` **是两件事、可能对不上**（正文 vs op）；没有返回 null |
| 1711–1713 | 1714 | `TriggerTexts`（**扩写**原有那一行，不是新增重复 summary） |
| 1738 | 1739 | `CostWhen.Ev` |
| 1774–1776 | 1777 | `CostWhens`：事件触发式降费，与 `CostIfControls` 不是一件事 |
| 1796–1798 | 1799 | `HandTrapWhens`：不在棋盘上，⛔ 别拿 `WhenTriggers` 判它 |
| 1874–1876 | 1877 | `CostIfControls`：**常驻条件**，不登记 `ctx.CostMods` |
| 2250–2251 | 2252 | `Title`：`Name` 的别名；中文名在 `NameZh` |
| 2254–2261 | 2262 | `IsUnit`：**打字的分流口**（现核全仓 31 处读它，列了四处典型） |
| 2289–2297 | 2298 | `Keywords`：值 1 ≠ 卡面印了 1；三件事（`Has` / `KwValue` / `NumericKeywords`）别混 |
| 2316–2323 | 2324 | `HasAbility`：判据是 `Ability != null`，**不是「卡面写没写」**（解析失败时为 false） |
| 2333–2351 | 2352 | `Has`：**三件必须知道的事**（静态 vs 运行时 / 说不出「有没有机制」/ 认不出的词不在这里）+ `<param>` |
| 2420–2429 | 2430 | `Stealth`：三条机制（不可被攻击 / 攻击即揭示 / **回合开始到期**） |
| 2432–2436 | 2437 | `Flying`：只有一条 —— 近战打不到；**反向不成立** |
| 2439–2447 | 2448 | `Armour`：按点数减伤；`UnitState.Armor` 只在三处同步；JSON 的 `armor` 是远程 |
| 2450–2457 | 2458 | `Shield`：**摘关键词**消费；`UnitState.HasShield` 是派生只读属性 |
| 2719–2725 | 2726 | `CantAttack`：与 `noncombatant` **同一道闸门**（`AttackBannedByTraits`） |
| 2728–2733 | 2734 | `LongRange`：仅远程档免反击；与 `sniper` 是两道独立闸 |
| 2742–2745 | 2746 | `Rally`：**部署之后**触发（不是「打出时」）|
| 2748–2750 | 2751 | `Strike`：攻击后触发、打死也算；与 `Mob` 的差别 |
| 2753–2756 | 2757 | `Slay`：与卡面 `When this unit kills…` 共用**同一发射口**；`subject`/`actor` 别混 |
| 2759–2762 | 2763 | `Backlash`：触发时**已不在棋盘上**，格位必须显式传 |
| 2765–2768 | 2769 | `Penitence`：真掉血**且**没死；⛔ 别放宽成「掉血就触发」 |
| 3566–3589 | 3590 | `Parse`：输入形状 / 输出给谁用 / **坏输入静默跳过**（留痕归调用方）/ 同名不是相加 + `<param>` `<returns>` |

> `ToString()` 覆写（`CardDef.cs:1745 / 1770 / 1870 / 2364`）**按任务口径跳过**（加了是噪声）。

### `EffectText.cs`（`d:/4/Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs`）

| doc 行 | 声明行 | 补了什么 |
|---|---|---|
| 951–965 | 966 | **类级** doc：这是全工程唯一的「卡面 → `EffectOp`」文法；入口三件（`Split` / `Parse` / `ParseSegment`）别混 |
| 1485–1493 | 1494 | `SegResult` 类级 doc：per-段（不是 per-卡），除 `Ops` 还捎带两条跨句信道 |
| 1496–1506 | 1507 | `SegKind Kind`：默认 `Unknown` ⇒ 新增分支忘了写 `Kind` 会**静默记成「认不出」** |
| 1509–1512 | 1513 | `SegResult.Ops`：可能为 null / 空表 |
| 1603–1605 | 1606 | `TextCoverage.Cards` |
| 1631–1633 | 1634 | `ImpreciseFreq`：键 = `OpHasMechanism` 的 `why` |
| 1636–1639 | 1640 | `SegTotal / SegKeyword / SegOk / SegPartial / SegUnknown`（**句级**账，与卡级那组不是一层） |
| 1642–1644 | 1645 | `UnknownExamples`：**上限 40、去重（按分句、跨卡共享）** |
| 1647–1649 | 1650 | `PartialExamples`：**上限 25、不去重**（与上一个口径不同） |
| 1654–1655 | 1656 | `PartialFreq` |
| 1107–1128 | 1129 | `Parse`：**补 `<param name="desc">`**（`CS1573`）+ 把一行 summary 扩成四行（分句→逐句→跨句补齐 / 两份对象别按引用比 / 「认出来」不等于「有机制」） |
| 1855–1868 | 1869 | `Coverage`：**补 `<param name="cards">` `<param name="type">`**（`CS1573`） |
| 6661–6692 | 6693 | `CostKindOf`：**把它自己那份被挤掉的 doc 搬回来** + `<param>` `<returns>` + 订正一处过时说法（见下） |
| 6596 附近 | — | `MentionsFaith` 的 doc：**就地订正**那条过时说法（见下） |

> `ToString()` 覆写（`EffectText.cs:571 / 910`）跳过。

### 就地订正（铁律 5，保留更正痕迹）

1. **`CostKindOf` 的 doc 曾粘在 `MentionsFaith` 头上**（两段连续 `<summary>`）——
   内容（「付费前缀里的『货币名』→ 规范取值」）明明是 `CostKindOf` 的，却因为
   `RePaidBare` / `PaidVerbs` / `MentionsFaith` 被插在中间而**挂错了方法**，
   于是 `CostKindOf` 一直**没有自己的 doc**、而 `MentionsFaith` 头上有两段。
   ⇒ 把那一段**搬到 `CostKindOf` 上方**（注释搬家，零行为改动）。
2. `MentionsFaith` 里原写「**所以它那边 `☀` 要整词相等**」—— **今天不成立**：
   `CostKindOf` 现在就是 `if (MentionsFaith(s)) return "faith";`，两处**都是 `Contains`**
   （历史上 `CostKindOf` 确实写过 `s == "☀"`，见 `git show 9e88674` 那一版 ⇒ 这句话当年不算错）。
   已按「⚠️ 2026-10-11 就地订正」的写法保留痕迹。

---

## 二、自证「只有注释」

`git diff --numstat`（改完，最终）：

```
415  29   Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs
207  18   Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs
```

（行数 415/29、207/18，**远小于**文件本身 3676 / 8446 行 ⇒ **行尾没被翻**；
实测 `CRLF=0 / LF=3676`、`CRLF=0 / LF=8446`，两个文件都是**纯 LF**，改完仍是纯 LF。）

**怎么确认没有代码行被改**（脚本 `d:/tmp/wf_e11/hunkcheck.py`）：
对 `git diff -U0` 的**每一个 hunk**，把 `-` / `+` 两侧的每一行**剥掉注释**（含字符串字面量保护），
再比两侧的代码行集合：

- `CardDef.cs`：**44 个 hunk，36 个纯注释 hunk，8 个含代码行**
- `EffectText.cs`：**22 个 hunk，18 个纯注释 hunk，4 个含代码行**

那 **12 个含代码行的 hunk 全部不是我写的** —— 这两个文件**在我接手之前就已经是脏的**
（会话开头的 `git status` 就把它们列成 ` M`）。逐条列出来（全部**只在别的写手那一批里**）：

| 文件 | 代码 hunk 内容 |
|---|---|
| `CardDef.cs` | `IsBodyKeyword(...)` → `IsBodyKeywordHead(...)`（2 处）· 新增 `StripParamSuffix` / `IsBodyKeywordHead` · 新增 `WhenBodyAt` · `StartsAnotherThing` 内同改 · `AddWhenTrigger` 内联改调用 · `CollectWhenTriggers` 改成按 `WhenBodyAt` 取正文 |
| `EffectText.cs` | 新增 `EffectTargetSpec.Clone()` · 新增 `op.Instead` 配对那段循环 · `MentionsFaith` 方法**搬家**（HEAD:6498 → 现 6691 附近） |

我**一个字都没动**它们。我自己的改动**全部是 `///` 行**：
`git diff -U0 | grep -c '^+ *///'` = **415**（= 上面 numstat 的 415 个 `+` 行里，正好就是这 415 个 `///` 行；
其余 `+` 行来自上面那批别的写手的代码）。

另外自查了两条「补注释位最常见的自伤」：

- **没有新造双 `<summary>` 块**：`EffectText.cs` 从 HEAD 的 **6** 处降到 **5** 处（我修掉 1）；
  `CardDef.cs` 保持 **1** 处（`TriggerKeywordOfKind` 前面那一处，**HEAD 就有**，不是我加的）。
  ⚠️ 中途我给 `TriggerTexts` **误加过一次重复 summary**（它原本已有 doc，是扫描的漏网），
  **已合并成一段**并复验 —— 现在 `CardDef.cs` 的双 summary 仍是 1 处。
- **没新增 `CS1573` 候选**：两个文件的含 `<param>` 的 doc 块里，**缺参的 = 0**
  （改之前 `EffectText.cs` 有 **2** 条：`Parse(string desc, …)` 缺 `desc`、
  `Coverage(cards, type, createPool)` 缺 `cards` 与 `type` —— 就是这两条补掉了）。
- **字段顺序没动**：中途我把 `SegResult.Ops` 挪到了 `Kind` 前面（代码行的顺序变化），
  **已还原成原顺序**（`Kind` 在前、`Ops` 在后），所以 `EffectText.cs` 的代码行集合与改前一致。

⚠️ **本件没跑任何编译**（按任务口径：`typecheck.sh` 由主对话统一跑）。
⇒ 顺带提醒：`工具/typecheck.sh` **默认档（`WF_DOC=0`）根本不解析 XML doc**，
要看 `CS1573` / `CS1574` 得跑 **`WF_DOC=1`** 那一档（脚本头部自己写着这件事）。
我用的 `<see cref="…"/>` 目标**都逐个核过存在**
（`ICardProvider.Id` · `EffectText.Split` · `EffectSpec.Parse` · `RuleCore.CostOf` ·
`UnitState.Has` / `HasShield` · `KeywordTable.*` · `DeckRules.CopyLimit` · 等等），
但**没编译过 ⇒ 有没有 `CS1574` 只能看主对话那一趟**。

---

## 三、没补的 / 补不出的

**逐条列，一条都没含糊过去。**

### 1. `ToString()` 覆写 —— **故意跳过**（6 处）

`CardDef.cs:1745`（`WhenTrigger`）· `:1770`（`CostWhen`）· `:1870`（`CostIfControl`）· `:2364`（`CardDef` 本体）
· `EffectText.cs:571`（`EffectOp`）· `:910`（`EffectTargetSpec`）。

理由 = 任务口径：「18 个 `ToString()` 覆写**跳过**，加了是噪声」。它们的方法体**本身就是一行可读的格式化**，
再写一句 `<summary>把它格式化成字符串</summary>` 属于**签名抄一遍**。

### 2. `<param>` —— 本件覆盖到的 doc 块**全部补齐**，**没有「写不出所以跳过」的**

两个文件里「有 doc 且带 `<param>`」的成员**共 2 个缺参**，都补了（见上表 `Parse` / `Coverage`）。
其余带参数的成员**一个 `<param>` 都没有**（所以**不产生 `CS1573`** ——
编译器的原话是「but other parameters do」，**一个都没标就不报**）。
⇒ 它们的参数语义**没有加 `<param>`**，但**在 `<summary>` 正文里点了关键的**（例如
`CostKindOf` 的「吃的是**一个货币词**」写在 `<param name="raw">` 里、`Coverage` 的 `type` 写在
`<param name="type">` 里）。
📌 **要不要给那批「一个 `<param>` 都没有」的成员也补全套 `<param>`**，是一个**独立的口径问题**
（本件的范围是「补 `CS1573`」，那批**不产生 `CS1573`**）⇒ 没做，**如实留在这里**。

### 3. `KeywordTable` 里**其余没有 doc 的常量** —— 不在本件范围 / 已确认本来就有

HEAD 扫描出来的 NODOC 常量只有 **11 个**（`Stealth` `Flying` `Armour` `Shield` /
`CantAttack` `LongRange` / `Rally` `Strike` `Slay` `Backlash` `Penitence`），**全部补了**。
其余关键词常量（`Vanguard` `Dodge` `Resistant` `Remnant` …）**本来就有 doc**。

### 4. 未验证项（**如实说**，不是「查不到」）

- **编译没过**（本件不许跑）⇒ 我新加的 `<see cref="…"/>` 有没有一条 `CS1574`，
  **只能由主对话那一趟（`WF_DOC=1`）判定**。我这边做到的是「引用的成员逐个 grep 存在」。
- 我**没有**去核「`Rally` / `Strike` / … 的触发时机在原版里是不是就是这些调用点」——
  写进 doc 的全是**本仓现有注释里已写死的事实 + 我自己读到的调用点**，没有新判据。

---

## 四、顺手发现的（只报，没改）

1. **`ICardProvider.cs:15` 的注释与事实不符**（铁律 5 该改，但**该文件不在我白名单**）：
   原写「稳定 id（**现在用卡名** —— OCR 产物里同名卡罕见，真出问题再加 faction 前缀）」。
   **现核**（`RuleEngine/Resources/cards_engine.json`，1126 张）：
   `id` 与 `name` **0 张相等**（形如 `ASH_Autarch` / `ASH32`），且**卡名真有重复 3 个**
   （`Terminator Champion` / `Maulerfiend` / `Terminator`）⇒ 两半都不成立。
2. **`EffectText.cs` 还有 5 处「双 `<summary>`」块**（`HEAD` 是 6 处，我修掉 1 处）：
   行 `6071` `6201` `6805` `7148` `8304`。`CardDef.cs` `1928` 处也有 1 处
   （`TriggerKeywordOfKind` 头上挂着两段 summary，第一段描述的其实是**另一个方法** `AddWhenTrigger`）。
   ⇒ 都是「doc 挂错方法」，成因与我在 `CostKindOf` 上遇到的那一处**同族**。修起来很便宜，
   但**不属于本件范围**（本件是「缺 doc」，这些是「doc 挂错」）⇒ **留账**。
3. **`EffectText.cs` `Coverage(...)` 声明后紧跟 `{            var cov = new TextCoverage();`**
   —— 大括号与第一条语句**挤在同一行**（排版瑕疵，不是缺陷）。
4. **这两个文件在我接手时就已经是脏的**（见 §二）⇒ 主对话合并时**别把 diff 里的代码 hunk 算到我头上**。
