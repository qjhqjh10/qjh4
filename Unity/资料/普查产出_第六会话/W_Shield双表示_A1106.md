# W · `Shield` 的两个表示收成一处（`A1106`）（写手交件 · 2026-10-09）

> 范围：`Shield` 在引擎里曾有**两份表示**（`UnitState.HasShield` 字段 · 关键词 `"shield"`），
> 两个写点各只动一份 ⇒ **双向脱节**。本件把它**收成一处**（关键词为唯一表示、字段变派生属性）。
> 白名单只碰了三个文件：`RuleEngine/Core/UnitState.cs` · `RuleEngine/Core/RuleCore.cs` ·
> `RuleEngine/Editor/RuleEngineTest.cs`。
> **没有跑 Unity 自检**（用户本轮明确要求「待办没做完之前不跑」）；只跑了**秒级类型检查**。

---

## ① 结论

### 1. 开工前那三件事的核证结论

#### (1) `HasShield` 全仓的读点/写点（`grep -rn "HasShield"`，全仓 `d:/4`）

> ⚠️ 本节的行号**一律是「改前」的**（改后 `UnitState.cs` / `RuleCore.cs` 的行号都往后挪了；
> 改后行号在 §② 的改动清单里）。

**写点只有 3 处**（3 个 `=` 赋值，其中第 1 处在构造期）：

| # | 位置 | 写什么 | 改后 |
|---|---|---|---|
| 1 | `UnitState.cs:407` | `HasShield = Has(KeywordTable.Shield);`（构造期快照） | **删**（属性直接读 `_keywords`，这句是同一判据的第二次求值） |
| 2 | `UnitState.cs:579` | `SyncKeywordState` 里 `if (keyword == KeywordTable.Shield) HasShield = true;` | **删**（`_keywords` 在调它之前就写好了，自动为真） |
| 3 | `RuleCore.cs:3589`（**改动后** `:3604`） | `u.HasShield = false;`（**盾消耗时**） | 改成 **`u.RemoveAll(KeywordTable.Shield);`**（原版口径：摘 trait） |

（**写点行号一律是「改前」的** —— 改后 `UnitState.cs` 的长短都变了。）

**读点 4 处 + 断言若干**：

| 位置 | 读它干什么 | 收成一处后 |
|---|---|---|
| `RuleCore.cs:3295` `DamageAfterReduction` | 判「这一下会被盾全挡」 | 不变（属性转发关键词） |
| `RuleCore.cs:3377` `DamageAfterReductionOne` | 预览逐条、只跳第 0 条 | 不变 |
| `RuleCore.cs:3583` `ApplyDamage` | 取本次是否带盾 | 不变 |
| `CardPresentation/Battle/BattleDriver.cs:11325` | 卡面图标（表现层） | **不变**（读属性 = 读关键词；两态下与改前同值） |
| `RuleEngineTest.cs` 14 处 | 断言 | 逐条核过（见 §④·4） |

⇒ **判「收成一处会不会碰别的语义」的结论：不会。** `HasShield` 的语义**只有**「这个单位现在带不带盾」，
没有第二种用法；改派生属性后**所有读点的取值在「两个表示本来一致」的那些状态下逐字不变**
（唯一会变的是**本来脱节的那两个窗口** —— 而那正是要修的）。

#### (2) `shield` 关键词全仓的读点 —— 谁才是「该信的那一份」

| 位置 | 读它干什么 |
|---|---|
| **`Data/SimpleAI.cs:588`** `ScoreDamaging` | **对手 AI 的目标评分**：带盾 ⇒ 走「只给一点点」出口 |
| `Data/SimpleAI.cs:675` `TraitScore` | 卡价值表（读的是 `CardDef.Keywords`，与单位状态无关） |
| `RuleCore.cs:3295` / `:3377` | **经 `HasShield` 属性**间接读 |
| `Core/EffectResolver.cs:6046` | `gets shield` 广播（写在 `AddKeyword` 那条路的旁边） |
| `Core/GivePayload.cs:149` | 载荷解析表（`"shield" → "shield"`） |
| `Core/CardDef.cs:1924` / `:2553` | 常量与 `Prefixes` |

🔴 **「该信的那一份」= 关键词。** 三条理由：
① **原版就只有这一份**（下面 (3)）；② **读点最多、覆盖规则与 AI**；③ 字段版**没有任何**领域含义
（它只是关键词的一个缓存，而且是个**会写坏的缓存**）。

#### (3) 原版怎么写 —— **摘 trait，不写布尔字段**（这就是本件照抄的形状）

**原版护盾只有一个表示 = trait `0x50`。** 判据四条（逐条读过）：

| 环节 | 出处 | 原文要点 |
|---|---|---|
| **定名** | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:9` | `shield = 80`（= `0x50`；同行 `:23` `dodge = 240` = `0xf0`，与 `W_dodge整条.md` §4·3 一致） |
| **免伤** | `decomp_full/CardScript__GetAdjustedDamage.c:36-48` | `0xe6`(dropPod)/`0xf0`(dodge)/`0x136`(invulnerable)/`0x50`(shield) **四个并列**，任一命中写同一个 `uVar3 = 1` |
| **消耗** | `decomp_full/CardScript._ReceiveDamage_d__381__MoveNext.c:320-333` | dodge 与 shield **各占一段独立的 `if`**：`HasCurrentTrait(0x50)` → `RemoveBuffedTrait(0x50)` + `RemoveTrait(0x50)`。**整段没有写任何布尔字段**（那一带唯一的字段写是前一行 `*(undefined1 *)(lVar2 + 0x351) = 1;`，那是「挨过伤害」那种演出标志，**不是护盾**，两段 `if` 都在它后面） |
| **`RemoveTrait` 的实现** | `decomp_full/CardScript__RemoveTrait.c` | `List.Find(predicate: id == param_2)` → `List.Remove(那一项)` + 从 `+0x130` 那本账里也删 + `BattleCardUI.RemoveStatusAnim` ⇒ **整个摘掉，不写字段** |
| **AI 也读它** | `decomp_full/AI__ScoreFromDamagingBuffedUnit.c:58-61` | `HasCurrentTrait(param,0xf0)` **或** `HasCurrentTrait(param,0x50)` 命中 ⇒ 提前走「只给一点点」那条出口（正是我们 `SimpleAI.cs:588` 的对应物） |

🔴 **反向核证（原版**没有**护盾布尔字段）**：`grep -n "Shield" d:/2/tools/il2cpp_out/dump.cs` 全库只命中
10 行，**没有一行在 `EntityScript` / `CardScript` 的字段表里** —— 命中的是
`RankedFastModeData.lossShieldCount` / `RankedBaseData.lossShieldCount`（**天梯赛季的「掉分保护」次数**，
与战斗内护盾无关，`:57790` / `:63314`）· `buttonShieldIcon`（UI 图）· `RankedV3ArmyState.Shielded`（天梯状态枚举）。
⇒ **原版在战斗内就是「trait 一份表示」**，本件照它做。

### 2. 改法：把「字段」换成「关键词的派生只读属性」

`UnitState.HasShield` 由 `public bool HasShield;` 改成
`public bool HasShield { get { return Has(KeywordTable.Shield); } }`，并把**那两个多余的同步写点删掉**、
把**消耗那一处改成摘关键词**。于是：

| 事件 | 改前 | 改后 |
|---|---|---|
| 构造（卡上带 `Shield`） | 字段 = 关键词 | 只有一个表示（关键词），属性读它 |
| `AddKeyword("shield")` / `AddAuraKeyword` | 字段被另写一次 | 不用写（属性自动真） |
| **盾被消耗**（`ApplyDamage`） | **字段假、关键词留** ⇒ **脱节**（AI 仍当有盾） | **摘关键词** ⇒ 属性同时假（**不可能脱节**） |
| **`RemoveAll("shield")`**（`lose Shield` / 光环收回 / 减层到 0） | **关键词没了、字段还是真** ⇒ **脱节**（白送一次免伤） | 摘关键词 ⇒ 属性同时假 |

**逐条给出「改前为什么会脱节 / 改后怎么保证一致」**：

1. **正方向（消耗）**：改前 `ApplyDamage` 只写 `u.HasShield = false`，**从不摘关键词**
   （改前全仓 `grep -rn 'RemoveAll("shield")'` **0 命中**）⇒ `u.Has("shield")` 永远为真。
   而 `SimpleAI.ScoreDamaging`（`Data/SimpleAI.cs:588`）读的**正是关键词**
   ⇒ **AI 把已经用掉盾的单位继续当带盾、只给 `dmg/3+1` 那点分**（表现层读字段，所以看不出来）。
   **改后**：消耗点写的是 `RemoveAll(KeywordTable.Shield)`（照原版 `RemoveTrait(0x50)`），
   属性从同一个字典派生 ⇒ **同一句改动的效果同时作用于两个读法**。
2. **反方向（摘关键词）**：改前 `RemoveAll` 只处理 `armour` / `blind`（`UnitState.cs:604-622`）
   ⇒ 关键词没了、字段还留在真 ⇒ **那一刻还能再挡一下**（白送一次免伤）。
   **改后**：属性派生 ⇒ 自动一起假。
3. **为什么「收成一处」不会再退回去**：全仓**只剩一个写入点**（`ApplyDamage` 里那句 `RemoveAll`），
   而且它写的就是**关键词**本身；字段已经是只读的 ⇒ `HasShield = …` 这种写法**编译不过**
   （这是我选「属性」而不是「在 `RemoveAll` 里补一句 `HasShield = false`」的原因 —— 后者是**第二处写同一条规则**，
   项目铁律点名禁止：「两处写同一条规则 = 迟早不一致」）。
   两处旧注释里都写死了「⛔ 别改回字段 / 别再加第二份同步」，并注明成因。

---

## ② 改动清单

### A. `Unity/MyGame/Assets/RuleEngine/Core/UnitState.cs`（行尾 **LF**，改前 `0 / 963` → 改后 `0 / 1001`，**4 次编辑 / 5 个落点**）

| # | 位置（**改后**行号） | 改前 | 改后 |
|---|---|---|---|
| 1 | `:203-229` | `public bool HasShield;        // 抵挡下一次伤害后失去`（一个裸字段） | `public bool HasShield { get { return Has(KeywordTable.Shield); } }` + 27 行判据注释（原版四条出处 · 改前的双向脱节 · 「⛔ 别改回字段」） |
| 2 | `:434-438` | `Exhausted = …;` 下面一句 `HasShield = Has(KeywordTable.Shield);` | 删掉那句，留 5 行注释说明「属性派生 ⇒ 这句多余，且只读属性写不进去」 |
| 3 | `:609-613`（`SyncKeywordState` 文档块里 🔴 那一段） | （无） | 新增：`shield` 已删 · `_keywords` 在调本方法**之前**就写好 ⇒ 自动为真 · **别再往这里加 `if (…Shield) …`**（那就是脱节的成因） |
| 4 | `:614-624`（`SyncKeywordState` 方法体） | `if (keyword == KeywordTable.Shield) HasShield = true;` | **删该行**（其余四行原样） |
| 5 | `:582`（`AddKeyword` 的文档块） | `⚠️ armour/shield/stun 三个还要同步状态字段` | `⚠️ armour/stun **两个**…` + 🔴 一条（`shield` 已改为派生属性） |

（第 3、4 行是**同一次编辑**的前后两半。）

### B. `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`（行尾 **LF**，改前 `0 / 5924` → 改后 `0 / 5939`，**2 处**）

| # | 位置（改后行号） | 改前 | 改后 |
|---|---|---|---|
| 1 | `:3295-3297`（`DamageAfterReduction`） | `if (u.HasShield) return 0;   // 盾挡下（并会碎）…` | 同句 + 2 行注释（「`HasShield` 现在是派生属性 ⇒ 与 `SimpleAI` 读关键词是**同一个判据**」） |
| 2 | `:3592-3605`（`ApplyDamage` 的盾那一支） | `u.HasShield = false;` | **`u.RemoveAll(KeywordTable.Shield);`**（`:3604`）+ 12 行判据注释（原版 `:326-333` 摘 trait · 改前的脱节后果 · 属性同时变假） |
| 3 | `:3607-3612`（`hadDodge` 那一支的注释） | `shield 那边走的是字段 HasShield` | `shield 那边**同一条口径**（A1106 已改成摘关键词）`（`:3611`） |

### C. `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`（行尾 **CRLF**，改前 `23211/23211` → 改后 `23324/23324`，**2 处**）

| # | 位置（改后行号） | 改前 | 改后 |
|---|---|---|---|
| 1 | `:284`（Step 表） | `Step(TestDodgeKeyword);` 后直接 `Step(TestHealAndDraw);` | 中间插入 `Step(TestShieldOneRepresentation);` |
| 2 | `:19279-19390`（新方法 `TestShieldOneRepresentation`） | — | 4 组断言（见下） |

**`TestShieldOneRepresentation` 的 4 组**（期望值写死、不拿被测函数反推）：

1. **消耗（正方向）**：带 `Shield` 的目标挨 6 点 ⇒ `ApplyDamage` 返回 **0**、血没动、
   **关键词被摘**（`!Has("shield")`）**且字段也假**（`!HasShield`）。
   ⚠️ 第一条 `!Has("shield")` 在**旧写法下直接为红**（旧写法只清字段）。
2. **AI 那一侧**（本件的主断言）：卡**自己不带** `Shield`（运行时 `AddKeyword` 给），4 费 4 血 ——
   * 还没用掉：`SimpleAI.ScoreDamaging(t, 6)` = **3**（= `dmg/3 + 1`，原版那条「只给一点点」的出口）；
   * 用掉之后：= **9**（= 卡价值 `4×2+1`，走「这一下判死」那支），且**与「从来没带过盾的同形单位」同分**；
   * **【灭自证】** 且 `!= 3` —— 旧写法会让它**永远停在 3**，改回去这里必红。
3. **反向**：`RemoveAll("shield")` ⇒ 字段同时变假，随后 `ApplyDamage(…, 3, …)` **照常吃 3**
   （旧写法下会被残留字段白送一次免伤 ⇒ 改回去这组必红）；另加 `RemoveKeyword(…, 1)` 减到 0 那条路。
4. **没盾时不受影响**（回归）：没盾的单位照常吃满 **3**、血量 4→1；AI 那一侧照常走正常路 = **9**。

---

## ③ 证据

### 1. 原版（判据，逐条给 `文件:行号`）

```
d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs
   9:	shield = 80,          ← 0x50
  23:	dodge = 240,          ← 0xf0（与 W_dodge整条.md §4·3 一致）

d:/2/tools/decomp_full/CardScript__GetAdjustedDamage.c:36-48      ← 免伤（四个 trait 并列写 uVar3 = 1）
d:/2/tools/decomp_full/CardScript._ReceiveDamage_d__381__MoveNext.c:320-325  ← dodge 消耗
                                                                    :326-333  ← shield 消耗（同一形状、并列的 if）
d:/2/tools/decomp_full/CardScript__RemoveTrait.c                 ← List.Find(id == param_2) → List.Remove（摘掉、不写字段）
d:/2/tools/decomp_full/AI__ScoreFromDamagingBuffedUnit.c:58-61   ← AI 读 0xf0/0x50（= 我们 SimpleAI.cs:588）
d:/2/tools/il2cpp_out/dump.cs                                    ← 全库 grep "Shield" 只有 10 行，没有一行在
                                                                    EntityScript / CardScript 的字段表里（= 原版无护盾布尔字段）
```

`_ReceiveDamage` 那一段（`:320-333`）原文骨架：

```
cVar7 = EntityScript__HasCurrentTrait(lVar2,0xf0,0);
if (cVar7 != '\0') { … CardScript__RemoveBuffedTrait(lVar2,0xf0,0);
                         CardScript__RemoveTrait(lVar2,0xf0,0); … }        ← dodge
cVar7 = EntityScript__HasCurrentTrait(lVar2,0x50);
if (cVar7 != '\0') { … CardScript__RemoveBuffedTrait(lVar2,0x50);
                         CardScript__RemoveTrait(lVar2,0x50); … }          ← shield
```
（两段是**独立的 `if`**，不是 `else if` —— 与 `A1077` 给 `dodge` 写的判据同源。）

### 2. 改前为什么脱节（现读实证）

* `RuleCore.cs:3589`（改前）= `u.HasShield = false;` —— **全仓 `RemoveAll("shield")` 改前 0 命中**。
* `Data/SimpleAI.cs:588`（现读）= `if (target.Has(KeywordTable.Shield) || target.Has("dodge")) return dmg / 3f + 1f;`
  ⇒ AI 读**关键词**，与 `ApplyDamage` 写的**字段**不是同一个口。
* `UnitState.cs:604-622`（改前；改动后 `RemoveAll` 起于 `:642`）改前只处理 `armour` 与 `blind` ⇒ **不清** `HasShield`。
* `CardPresentation/Battle/BattleDriver.cs:11325` 读的是**字段** ⇒ 表现层看不出这个偏差（与简报一致）。

### 3. 与简报不符的两处（如实报回来）

| 简报说 | 实际是 |
|---|---|
| 「`RuleCore.cs:1243`（`EndTurn` 归还）与 `EffectResolver.cs:4750`（`DoTakeControl`）**也有写点**」 | 🔴 **现读两处都不是 `HasShield` 的写点** —— 现读 `:1243` 是一句 `ctx.Log(…)`（「还不回去」那条日志），`EffectResolver.cs:4750` 一带也没有 `HasShield`。全仓 `HasShield` 的**写点只有 3 处**（`UnitState.cs:407` / `:579` / `RuleCore.cs:3589` 改前行号），**全部在本件白名单内**。⇒ 那两处**不需要一起收口、也没有顺手改** |
| （简报未提）| `grep HasShield` 在**仓库根**还会命中 `_tmp_view/wf_eng4_probe/old/{UnitState,RuleCore}.cs`（**离线探针的旧快照副本**，只被 `资料/普查产出_1018第三会话/W_引擎族4.md` 引用）。**不在白名单 ⇒ 一个字节没动**，仍是旧字段版；真正参与编译的 `Assets/RuleEngine/Core/*.cs` 是新版 |

---

## ④ 验证

### 1. 秒级类型检查（`TMPDIR=/tmp/wf_shield bash d:/4/Unity/工具/typecheck.sh`）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（改完三个文件**一次性**跑的那一次；`UnitState` 那 4 处与 `RuleCore` 那 2 处在同一批里，
中间没有出现「错误全在别人正在写的文件上」的噪声。）

### 2. 行尾（**二进制读**，`b.count(b'\r\n')` vs `b.count(b'\n')`）

| 文件 | 改前 | 改后 | 判定 |
|---|---|---|---|
| `UnitState.cs` | `0 / 963`（纯 LF） | `0 / 1001`（纯 LF） | ✅ 没翻 |
| `RuleCore.cs` | `0 / 5924`（纯 LF） | `0 / 5939`（纯 LF） | ✅ 没翻 |
| `RuleEngineTest.cs` | `23211 / 23211`（纯 CRLF） | `23324 / 23324`（纯 CRLF） | ✅ 没翻 |

（全程**只用 Edit / Write 工具**，没有 `sed -i`、没有用 python 文本模式写。）

### 3. `git diff --numstat`

```
704	72	Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs
89	6	Unity/MyGame/Assets/RuleEngine/Core/UnitState.cs
1167	19	Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs
```

🔴 **这三个数【不是本件的改动量】** —— 三个文件在**本会话开工前就已经是 `M`（未提交）**，
`git status --porcelain RuleEngine/` 实测：`BattleContext.cs · CardDef.cs · CreatePool.cs ·
EffectResolver.cs · RuleCore.cs · UnitState.cs · DeckBuilder.cs · SimpleAI.cs · RuleEngineTest.cs` 全是 `M`
（上一会话 + 别的写手）。**本件的改动量**（按**行数差**净算）：
`UnitState.cs` **+38 行 / 4 处** · `RuleCore.cs` **+15 行 / 2 处** · `RuleEngineTest.cs` **+113 行 / 2 处**。
**判「行尾没翻」看的是上面那张表**（numstat 只用来排除「整篇重写」—— 数字远小于行数的量级）。

### 4. 既有断言逐条核过（**没有跑**，只做静态核对 —— 本轮禁止跑 Unity）

风险点是「有没有别的断言依赖**旧的两份表示**」。逐条读了 `grep -rn HasShield` / `grep -n 'KeywordTable.Shield\|"shield"'`
命中的**每一处测试**：

| 断言 | 为什么不受影响 |
|---|---|
| `TestShieldBlocks`（`:18370` / `:18374`） | 「带盾 ⇒ 真」与「消耗后 ⇒ 假」两态**改后同值** |
| `:7217/:7224/:7230/:7235/:7237`（常驻效果给 Drone 上盾） | `Place` 摆的早单位**本来就没盾**（关键词也没有）；部署后的 Drone 走 `AddKeyword` ⇒ 属性自动真 |
| `:1182`（没被打到 ⇒ 还有盾） | 关键词还在 ⇒ 属性真，同值 |
| `:12074`（面朝下 + 带盾前提） | 同上 |
| `:15356/:15363`（`Give Shield to a friendly unit` 前后） | 走 `AddKeyword("shield")` ⇒ 属性自动真 |
| `:1420/:1429`（`this turn` / `until your next turn` 两条路的盾） | 只读 `Has("shield")`，**没读字段** |
| `:12913/:12920`（残骸拿盾） | 只读 `Has("shield")` |
| `:18370/:19065/:19242/:19244/:19255`（`A1077` 的 dodge 那批） | 前提态与消耗态**改后同值**（消耗那一句由写字段换成摘关键词，属性跟着变假） |

⇒ **结论：改后这些断言的取值与改前逐条相同**（唯一变化的是「本来脱节」的那两个窗口，
而既有断言**没有一条**钉在那两个窗口上 —— 这正是它长期以来「全绿却带着一个真缺陷」的原因）。
⚠️ 新加的 `TestShieldOneRepresentation` 的 4 组**只做了手推、没真跑**（本轮禁止跑自检）。

---

## ⑤ 没查清 / 停手的

1. 🔴 **新断言没有真跑**（本件最大的未验项）—— 用户本轮明确要求「待办没做完之前不跑自检」，
   所以 4 组断言只做了**逐条手推**（每一步都对着 `ApplyDamage` / `SimpleAI.ScoreDamaging` 的实际代码走了一遍）。
   数值依据（写进断言注释）：`3 = 6/3 + 1`（原版那条「只给一点点」的出口）·
   `9 = 4 费卡价值 4×2 + 1`（`ValueInPlay` 在「这一下判死」那支、满血、且单位身上没有别的关键词时）。
   ⇒ 请调度台在收口那一轮跑 `RuleEngineTest.Run` 时看这一段。
   **风险点单列**：第 (2) 组依赖 `SimpleAI.ScoreDamaging` 的分支结构（`ambush`/`longrange`/`shield` 三个门 + 判死那支），
   本件**没有改动 `SimpleAI`**（不在白名单）⇒ 若那一组红，先怀疑夹具（`Duel` 摆位）而不是本件。
2. **`RemoveTrait` 与「护盾叠层」的关系没查透**：`CardScript__RemoveTrait.c` 是 `List.Find` → `List.Remove`，
   即**摘掉一项**；我们这边用的是 `RemoveAll`（**整条摘掉、不管叠了几层**）。
   两者在「同一个 trait 在列表里有 ≥2 项」时才可能不同，而**叠层形态查不到**
   （`HasCurrentTrait` 只判「在不在」，`AddEffect` 那条链没逐句读）。
   ⚠️ 这与 `A1077` 给 `dodge` 选的是**同一个口径**（当时也这么定的）⇒ 本件**沿用、不另开一套**，
   如实标着「没查透」。**今天全池没有一条效果能把 `shield` 叠到 2 层**（无 `AddKeyword("shield",…)` 调用点）。
3. **`AddKeyword("shield", 0)` 这个边界没查**：改前字段停在假、改后属性为真
   （`Has` 是 `ContainsKey`，只管在不在）。原版 `HasCurrentTrait` **也只看在不在**（不读值）⇒ **改后更贴原版**。
   全仓**没有**调用点传 0，所以只是语义对齐、不是行为变化 —— **如实标着，不当已验证**。

---

## ⑥ 顺手发现的（不在本件范围，交调度台分流）

1. **同一个病根还有第二个受害点**（本件**顺带修好了、但不是为它改的**）：
   `UnitState.ClearAuraGrants`（**改后** `:813-840`，改前 `:775-802`）收光环时**直接改 `_keywords`**（改后 `:822-823`
   `_keywords[kw] = now` / `_keywords.Remove(kw)`），**不走 `SyncKeywordState`、也不走 `RemoveAll`**。
   改前：光环给的 `shield` 在光环消失后，**关键词没了、`HasShield` 还是真** ⇒ 第二次脱节（同病根）。
   改后：属性派生 ⇒ 自动一起假。**没有为它单独写代码**（也不需要）。
2. **`UnitState.IsStunned` / `IsBlind` 是同一个形状的两个「缓存字段」**（不是本件范围）：
   `_keywords` 与状态字段仍然**两处表示**，`SyncKeywordState` / `RemoveAll` 各手动同步一遍。
   本件**只收了 `shield` 一处**（判据是原版「trait 一份表示」，`stun`/`blind` 在原版是**另外的机制**，
   没查过 ⇒ **不猜、不动**）。⇒ 建议另开一小件**先查原版**再决定要不要同样收口。
3. **本件没碰 `Data/SimpleAI.cs`**（虽然脱节的表现出在它那儿）：它的读法（读关键词）
   **本来就是原版的读法**（`AI__ScoreFromDamagingBuffedUnit.c:58-61` 读 trait）⇒ **它是对的、字段是错的**，
   修在**写点**这一侧。⛔ 别把 `SimpleAI` 改成「读字段」——那会把它改成原版没有的样子。
4. `RewardsScene`/`ShopScene` 等**与本件无关**，`git status` 里的其它 `M` 文件**一个都没动**。

---

## 摘要（300 字以内）

`Shield` 原来有**两份表示**：`UnitState.HasShield` 字段 + 关键词 `"shield"`。两个写点各动一份 ⇒ **双向脱节**：
① `RuleCore.ApplyDamage` 消耗盾时只写 `HasShield=false`、**从不摘关键词**（全仓 `RemoveAll("shield")` 改前 0 命中）
⇒ `SimpleAI.cs:588` 读的正是关键词 ⇒ **AI 把用掉盾的单位继续当带盾、只给 `dmg/3+1`**；
② `RemoveAll("shield")`（`lose Shield`/光环收回）**不清**字段 ⇒ 关键词没了还能再挡一次。

**照原版收成一处**（原版护盾 = trait `0x50`：`DefinedTrait.cs:9`；免伤 `GetAdjustedDamage.c:45`；
消耗 `_ReceiveDamage__d__381__MoveNext.c:326-333` 的 `RemoveTrait(0x50)`，**不写任何布尔字段**；
AI 也读 trait `AI__ScoreFromDamagingBuffedUnit.c:58-61`；`dump.cs` 全库无护盾字段）：
把 `HasShield` 改成**关键词的派生只读属性**、删掉两个多余同步写点、消耗点改成 `RemoveAll(KeywordTable.Shield)`。
⇒ 只剩**一个写入点、一份表示**，把字段改回去**编译不过**。

新增 `TestShieldOneRepresentation` 4 组（消耗双向一致 / **AI 用掉后 = 9 而非 3** / 反向 `RemoveAll` / 没盾回归），
各配灭自证；**只手推未跑**（本轮禁止自检）。改前 3 处「简报说什么 vs 实际是什么」已报。
类型检查 **0 错**；三文件行尾未翻（LF/LF/CRLF）。
