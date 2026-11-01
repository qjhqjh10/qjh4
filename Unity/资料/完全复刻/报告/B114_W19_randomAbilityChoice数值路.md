# B114_W19 · `B-251` `randomAbilityChoice` 接上**数值路**

> 会话：第一百一十四会话 · 写手 `W-19`｜日期：2026-11-01
> ⛔ 未跑 Unity · ⛔ 未跑 `play check` · ⛔ 未动 git · typecheck 用 `TMPDIR=/tmp/wf_w19`

---

## 〇、一句话

**回源结论：原版两条路是【同一条路】** —— `RawCardScript__TriggerAbilities.c` 的三处 `TriggerAbility` 调用点
（`:184` / `:231` / `:257`）调的是**同一个函数**，「触发值选哪条 ability」只在
`CardAbility__CanTriggerAbility.c:71` 比一次。⇒ `ability+0x48`（`randomAbilityChoice`）**对两条路都成立**，
**不存在「数值路本来绕过它」这一支**，所以**接**（brief ② 的停手条件不成立）。

已接：`FireTriggerByNumberWithSink` 的**队列体**里把 `ops` 包一层 `RandomAbilityChoiceOps(..., triggerName: tn)`；
判法仍然只有 `RandomAbilityChoiceOps` **一份**（⛔ 没有另写第二份）。

---

## 一、原版判据（`d:/2/tools/decomp_full/` 逐句现读）

| # | 结论 | 判据（`文件:行`） |
|---|---|---|
| 1 | `randomAbilityChoice` 是 `CardAbility` 的字段 `+0x48`，**在 `TriggerAbility` 里被读** | `RawCardScript__TriggerAbility.c:75` `cVar3 = *(char *)(param_2 + 0x48);` |
| 2 | 为真 ∧ `abilityLogic(+0x50).Count > 1` ⇒ `GetRandomInt(0, Count)` **抽一条**、`PlayAbility` 那一条之后 **直接 `return`** | 同文件 `:73`（`lVar5 = *(longlong *)(param_2 + 0x50)`）· `:82`（`if (cVar3 != '\0')`）· `:84`（`1 < Count`）· `:85`（`BattleManager__GetRandomInt`）· `:89-90`（`PlayAbility` + `return`） |
| 3 | 为假（或只有 1 条）⇒ **逐条** `PlayAbility`（`while` 遍历整个列表） | 同文件 `:95-106` |
| 4 | 🔴 **两条路共用同一个 `TriggerAbility`**：`TriggerAbilities` 的三处调用点**同一个函数、同一组实参形状** | `RawCardScript__TriggerAbilities.c:184`（附魔趟）· `:231`（无隐藏目标那趟）· `:257`（按隐藏目标展开那趟） |
| 5 | 「触发值选哪条 ability」**只在 `CanTriggerAbility` 里比一次**（`ability.trigger(+0x10) != param_2 ⇒ return 0`） | `CardAbility__CanTriggerAbility.c:71` |
| 6 | 原版**没有**「关键词触发 vs 数值触发」两套派发 —— 那是**我们**代码的结构（`FireTriggerAt` / `FireTriggerByNumber`） | 由 4、5 直接推出 |

⇒ **本站得住的解读**：既然原版只有一条路，那么我们的两条路**必须同语义**；
`FireTriggerAt` 早就接了（`RuleCore.cs:12933`），`FireTriggerByNumberWithSink` 没接 = **独立缺口**，补上就是复刻。
`报告/B114_W9_B211派发口.md` §五·2 记的那条正是它。

---

## 二、改动清单 + 净增净减

| 文件 | 增/删（行） | 说明 |
|---|---|---|
| `MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | **+28 / −0**（21,456 → 21,484 行） | ① **中部 5 处同物理行原地改写（行数零变化）** ② 文件尾追加 `RandomAbilityChoiceOpsForTest` 一族（28 行） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | **+0 / −0**（39,737 行不变） | `:1400` 行末**同物理行**追加接线（落在该行第一个 `//` **之前**） |
| `MyGame/Assets/CardPresentation/Editor/B114_W19_Assert.cs` | **新建 251 行** | 断言全文（独立类 `B114_W19_Assert`，16 条） |
| `MyGame/Assets/CardPresentation/Editor/B114_W19_Assert.cs.meta` | 新建 2 行 | `guid: 3c7f19a2b64d40e58f11ad4e19b11419` |
| `MyGame/Assets/RuleEngine/Core/RuleCore.cs` | **+0 / −0** | **一行未动**（白名单里有它，但本活不需要改） |

**行尾**：`EffectResolver.cs` 纯 CRLF / `bareCR = 0`（改前改后都是 21,456→21,484 条 CRLF）；
`RuleEngineTest.cs` 纯 CRLF / `bareCR = 0`；两个新建文件按同目录既有惯例（`B114_*_Assert.cs` **全 LF**）。
**行号纪律**：`EffectResolver.cs` 中部**零行数变化**，新成员**全部追加在文件尾** ⇒ 旧 A 行落点零漂。

### 2·1 中部那 5 处同物理行改写

| 行 | 改前 → 改后 | 为什么 |
|---|---|---|
| `:18640` | 形参表末尾加 `, string triggerName = null` | 数值路**没有关键词**，只有 `CardDef.TryAbilityTriggerNameOfNumber` 反查出的 trigger 名 |
| `:18645` | `string tn = TriggerNameOfKeyword(keyword);` → `… = !string.IsNullOrEmpty(triggerName) ? triggerName : TriggerNameOfKeyword(keyword);` | 同上。**缺省 `null` ⇒ 关键词路逐字不变**（表达式恒等回落） |
| `:18655` | `NoteRandomChoiceUnresolved(card, keyword)` → `…(card, string.IsNullOrEmpty(keyword) ? tn : keyword)` | 出声口在数值路下 `keyword == null` |
| `:18672` | `{keyword.ToUpperInvariant()}` → `{(string.IsNullOrEmpty(keyword) ? tn : keyword).ToUpperInvariant()}` | 同上（日志文案） |
| `:21087` | `ResolveOps(ctx, owner, u, ops, …)` → `ResolveOps(ctx, owner, u, RandomAbilityChoiceOps(ctx, u, ops, null, null, false, tn), …)` | **本活的正题**：数值路在**出队那一刻**做同一跳 |

### 2·2 文件尾新增（`:21457-21484`）

| 行 | 是什么 |
|---|---|
| `:21457` | 一块新的 `//` 头注释（原版判据 + 「两条路同一条」的反证） |
| `:21471` | `namespace RuleEngine { public static partial class RuleCore {`（本文件既有惯例：尾部追加独立块） |
| `:21474` | `public static IReadOnlyList<EffectOp> RandomAbilityChoiceOpsForTest(ctx, u, ops, keyword)` —— 自检门。**第 7 形参固定传 `null`** ⇒ 走的正是**关键词路**那一支，用来钉住「关键词路行为不变」 |

### 2·3 语义（与关键词路**完全同一份实现**）

```
RandomAbilityChoiceOps(ctx, u, ops, only, keyword, ignoreRandomTrigger, triggerName = null):
  ① ops == null / Count < 2 / ignoreRandomTrigger / ctx == null ⇒ 原样返回
  ② card = only?.Card ?? u?.Card；null ⇒ 原样返回
  ③ tn  = triggerName（非空）?? TriggerNameOfKeyword(keyword)
  ④ tn 为空 或 !card.TriggerRandomChoice(tn) ⇒ 原样返回（= 改前行为）
  ⑤ 按 EffectOp.LogicIndex 分组；任一条 LogicIndex < 0 ⇒ **不猜**，原样返回 + 一次性 LogWarning
  ⑥ groups.Count < 2 ⇒ 原样返回
  ⑦ ctx.GetRandomInt(0, keys.Count) 抽一组，**只返回那一组**
```

---

## 三、预计影响面（哪些卡 / 哪些 trigger）

### 3·1 **今天：零行为变化（重要）**

| 判据 | 现读 |
|---|---|
| 全池 `rc != 0` 的行 | **5 行 / 1 张卡**：`GOF12 Spanner`，`tn = Mob`，`t = 0.0/0.3/0.6/0.9/1.2`（`cards_engine.json` v47 `animTimes[]` 现扫） |
| `FireTriggerByNumber` / `FireTriggerByNumberWithSink` 的**生产调用点**收到的数值 | **只有 5 个**：`60`（`EffectResolver.cs:20605`）· `185`（`:20606`）· `187`（`:20607`）· `210`（`EffectResolverPartial_B97W2.cs:451`）· `410`（`RuleCore.cs:13036` / `EffectResolver.cs:20679`） |
| `Mob`(=300) 走哪条路 | **关键词路**：`RuleCore.cs:10680` `FireTriggerAt(ctx, attacker, KeywordTable.Mob, …)`。**没有任何调用点把 300 传给数值路** |
| ⇒ 结论 | 数值路今天**永远见不到** `tn == "Mob"` ⇒ `TriggerRandomChoice` 恒 `false` ⇒ 新加的那一跳**直接返回原 `ops`** |

**同一条推理的第二个推论**：`RandomAbilityChoiceOps` 在 `TriggerRandomChoice(tn) == false` 时
**不会调 `ctx.GetRandomInt`** ⇒ **随机数序列的消费点一个都没变** ⇒
`play check` 指纹**应当逐字节不变**（这一条请调度台用 `play check` 复核，我跑不了）。

### 3·2 数据一出现就生效（铁律 11：休眠件照建）

将来任何一张卡在**数值路可达的** trigger（`60/185/187/210/410`）上带 `rc = 1`，
本跳就会自动生效 —— 不需要再改引擎。

### 3·3 断言覆盖（不是"影响面"，是"能证明什么"）

| 组 | 条数 | 盯什么 |
|---|---|---|
| **A**（4） | 前提 | 夹具确实带 `randomAbilityChoice`（`TriggerRandomChoice("UnitDamaged") == true`）· 对照组不带 · 正文分 2 组 · `TriggerOpsByNumber(60)` 挂得上 |
| **B**（5） | **结论** | 走**数值路** `FireTriggerByNumber` ⇒ 护甲只可能是 **2 或 4**（两组 `Gain Armour 2` / `Gain Armour 4` 里**只跑一组**）· 且**≠ 6** |
| **C**（4） | **灭自证** | 同构夹具只改 `randomAbilityChoice` ⇒ `false` 那组**两组全跑 = 6**；两者**必须不等** |
| **D**（3） | **关键词路不变** | 不传第 7 形参 ⇒ 仍按 `TriggerNameOfKeyword(keyword)` 抽（`Mob` 命中）；无 rc ⇒ 原样返回全量 |

**灭自证怎么红**：
- 删掉 `:21087` 那层包装 ⇒ B1/B2 变 6 ⇒ **红**（C1 仍 6、C2 变"相等" ⇒ 更红）。
- 把 `:18645` 退回 `TriggerNameOfKeyword(keyword)`（数值路 `keyword == null`）⇒ 整跳短路 ⇒ 同样变 6 ⇒ **红**。
- 把 `tn` 写死 / 删掉 `TriggerRandomChoice` 闸 ⇒ D1 返回全量 ⇒ **红**。
- 🔑 **C1（=6）与 B1（2 或 4）结构上不可能被"两边一起改回去"同时满足** —— 这就是灭自证。

---

## 四、没查清的部分

1. ✅ **附魔那半条数值路当时没接** —— ~~只记账、没动手~~ 🔴 **2026-11-01 就地订正（第二轮）**：调度台补授权，**已接完**
   （`EffectResolverPartial_B109E6.cs:134`）。见 **§八**。
2. **`ctx.GetRandomInt` 的消费点与原版有时序差**：原版在 `TriggerAbility` 里**同步**抽（入队之前）；
   我们在**出队那一刻**抽。这是**关键词路早就存在的**结构性差异（`RuleCore.cs:12933` 也在 `FireTriggerDeferred` 里），
   本活只是让数值路**与它同形**，⛔ 没有引入新的差异面。今天零可观测（§3·1）。
3. **`RunFollowAfterResolutionRow` / `FireTriggerRowsByChosenAbility`（`ca` 数据行那一路）要不要也过
   `randomAbilityChoice`** —— 没查。它走的是 `chosenAbility` 数据行、不是 `abilityLogic` 列表，
   与 `ability+0x48` 不是同一个轴，但**没从反编译证否**。
4. **运行期真实可达性**：本会话不跑 Unity / `play check` ⇒ §3·1 的"零行为变化"是**数据面推的**，不是跑出来的。
   裁决口 = 调度台跑一次 `play check` 对指纹 + `RuleEngineTest.Run` 看 B114_W19 那 16 条。

---

## 五、顺手发现（请调度台分流）

1. ✅ ~~**附魔的数值路（`FireEnchantmentAbilitiesByNumber`）同样没接 `randomAbilityChoice`**~~ ——
   🔴 **2026-11-01 就地订正（第二轮）：已经接完了**，见 **§八**。下面是当时的记录（保留作判据）：
   `EffectResolverPartial_B109E6.cs:135` 也是 `ResolveOps(ctx, owner, u, eops, …, chosen: targetCard)`，
   **没有** `RandomAbilityChoiceOps` 那一层。而原版 `RawCardScript__TriggerAbilities.c:184` 那处
   （附魔趟）**同样调 `TriggerAbility`** ⇒ `ability+0x48` 对附魔能力**也成立**（我方关键词路的附魔递归
   `RuleCore.cs:12933` 传了 `only`，**已经覆盖**）。⇒ **同一缺口在附魔支上还开着一条**，
   修法与我这处**逐字同形**（`eops` 包一层 `RandomAbilityChoiceOps(ctx, u, eops, e, null, false, tn)`，
   注意 `only = e` 让 `card` 取附魔卡自己）。
   ⚠️ 该文件**不在本代理白名单**（brief ④），**一行未动**。建议派给它的当前写手，或单开一条。
   📊 今天同样零可观测（唯一 `rc=1` 的 `GOF12 Spanner` 不是附魔卡）。

2. 🟡 **`RandomAbilityChoiceOps` 在 `LogicIndex < 0` 时是"原样返回全部"**（= 跑**所有** abilityLogic），
   这与原版「为真就只跑一条」**不等价** —— 它是**已知的不猜档**（注释里写着）。
   今天唯一命中者 `GOF12 Spanner` 正好落在这一档（正文解析成一条 `chooseone + RandomPick` op）。
   ⛔ 别为了它按卡 id 打补丁（原注释已经写死这条）。

3. 🟡 **`EffectResolver.cs` 文件尾现在叠了 6 个 `namespace RuleEngine { partial class RuleCore { … } }` 块**
   （`:20810 / :20859 / :21126 / :21173 / :21232 / :21457`）。追加本身没问题，但**以后找成员别只看顶部那个块**。

4. 🟡 **`报告/B114_W9_B211派发口.md` §五·2 那条账可以关了** —— 它写的就是本活，
   结论已落地（`EffectResolver.cs:21087` + `:18640/18645`）。

---

## 六、验收对照（brief ①–⑥）

| # | 要求 | 结果 |
|---|---|---|
| ② | **先回源判**：原版两条路若不同就停手给反证 | ✅ **回源了，且结论是"相同"**（§一 · `TriggerAbilities.c` 三处调用点是同一个 `TriggerAbility`）⇒ 接。⛔ 没有硬接一个原版没有的语义 |
| 1 | 数值路也接上，语义有原版判据背书 | ✅ `EffectResolver.cs:21087`；判据 `RawCardScript__TriggerAbility.c:75 / :82-94` + `TriggerAbilities.c:184/231/257` + `CanTriggerAbility.c:71` |
| 3 | 保留关键词路现有行为不变 | ✅ 第 7 形参缺省 `null` ⇒ `tn` 表达式恒等回落；5 处改写**行数零变化**；断言 D 组专钉这一条 |
| 4 | 断言三件套（前提 / 结论 / 灭自证） | ✅ A 组（前提）· B 组（数值路下随机选择**被消费**）· C 组（关掉那一支 ⇒ 6 ⇒ 红）· D 组（关键词路不变） |
| 5 | `TMPDIR=/tmp/wf_w19 bash 工具/typecheck.sh` → 0 / 0 | ✅ 运行时 **0** / 编辑器 **0**（全部改动落地之后跑的） |
| 6 | ⛔ 不跑 Unity、⛔ 不跑 `play check` | ✅ 都没跑；git 一笔未动 |

**红线自查**：只改白名单内文件（`EffectResolver.cs` / `RuleEngineTest.cs` 同一物理行 / 新建 `B114_W19_Assert.cs`），
`RuleCore.cs` 一行未动；未碰 `ItemDrawer*` / `ImageQuad.cs` / `CardDisplayEffects.cs` / `_sfx_meta.py` /
`ArenaBuilder.cs` / `BattleScene.cs` / `ShellScene.cs` / 两份正本 / `账/*.tsv` / `Scenes/*`。

---

## 七、接线（已落地，供调度台核对）

`Assets/RuleEngine/Editor/RuleEngineTest.cs:1400`（**同一物理行**，落在该行第一个 `//` **之前**）：

```csharp
B114_W19_Assert.Run(); _pass += B114_W19_Assert.Pass; _fail += B114_W19_Assert.Fail;
```

---

## 八、第二轮（2026-11-01 · 调度台补授权）：**附魔那半条数值路也接上了**

> 本轮白名单只有两个文件：`Assets/RuleEngine/Core/EffectResolverPartial_B109E6.cs` + 本报告 §七 那个断言文件。
> ⛔ `EffectResolver.cs` **本轮一行未动**（上一轮刚交回）。

### 8·1 判据（与第一轮同一条，只是换了个能力归属）

| # | 结论 | 判据（`文件:行`） |
|---|---|---|
| 1 | 附魔趟**调的就是** `RawCardScript__TriggerAbility` | `RawCardScript__TriggerAbilities.c:184`（附魔趟派发点） |
| 2 | `ability+0x48` 那一跳在 `TriggerAbility` 内部，**与「能力属于哪张卡」无关** | `RawCardScript__TriggerAbility.c:75`（读 `param_2 + 0x48`）· `:82-94`（抽一条 + `return`） |
| 3 | ⇒ 附魔卡的 `ability+0x48` **同样成立**，⛔ 不存在「附魔趟不走 randomAbilityChoice」这一支 | 由 1 + 2 |

与第一轮的区别只有一处：**`only` 要传附魔条目 `e`** —— `RandomAbilityChoiceOps` 内部
`card = only != null ? only.Card : u.Card`，附魔趟必须判**附魔卡自己**的 `TriggerRandomChoice`（⛔ 不是载体的）。

### 8·2 改动清单 + 净增净减

| 文件 | 增/删（行） | 说明 |
|---|---|---|
| `Assets/RuleEngine/Core/EffectResolverPartial_B109E6.cs` | **+17 / −0**（185 → 202 行） | ① `:134` **同物理行原地改写**（行数零变化）② 类尾追加 `FireEnchantmentAbilitiesByNumberForTest`（自检门） |
| `Assets/CardPresentation/Editor/B114_W19_Assert.cs` | **+73 / −0**（251 → 324 行，净 0 删除） | 新增 **E 组（8 条）** + `PlainUnit` / `MakeBattleWithEnchant` 两个夹具助手；`Fixture` 改成「两组用同一张动画行、只差 `RandomChoice`」（见 8·5） |

**`:134` 改前 → 改后**

```text
改前：try { ResolveOps(ctx, owner, u, eops, tn.ToUpperInvariant(), chosen: targetCard); }
改后：try { ResolveOps(ctx, owner, u, RandomAbilityChoiceOps(ctx, u, eops, e, null, false, tn), tn.ToUpperInvariant(), chosen: targetCard); }
```

**与第一轮逐字同形**：同样的 `null keyword` + 第 7 形参直给 `tn`，唯一的差别是 `only` 传 `e`。

**行号**：本文件 **LF / `bareCR = 0`**（改前改后一致）。`:69`（`FireEnchantmentAbilitiesByNumber` 声明）与
`:165`（`EnchantmentHasAbilityForTrigger` 声明）**改前改后同一行号**（新成员全部在类尾，且 `:134` 是同行替换）。
⚠️ 账 `战斗逻辑驱动.tsv` 第 1285 行的第二处落点写的是 `:162`，而现读（= 改前 HEAD 也是）`EnchantmentHasAbilityForTrigger` 在 `:165`
—— **3 行偏差是本轮之前就有的**，本轮没有改变它（属 `SECOND` 档告警，不阻塞）。

### 8·3 断言（`E` 组 8 条）

| 条 | 盯什么 |
|---|---|
| `E0` | **前提**：附魔夹具卡确实带 `randomAbilityChoice`（`TriggerRandomChoice("UnitDamaged") == true`） |
| `E0b/E0c/E0d` | 前提：载体上场 · 附魔真的挂上（`Enchantments.HasAny`）· 派发前护甲 0 |
| `E0e` | 前提：`FireEnchantmentAbilitiesByNumberForTest(...) > 0` —— **附魔那一趟真的派发了**（否则后面的读数什么都证明不了） |
| `E1` | **结论**：`FireEnchantmentAbilitiesByNumber` ⇒ 护甲 **2 或 4**（两组正文只跑一组） |
| `E2` | 反面：护甲 **≠ 6** |
| `E3` | **灭自证（「不传 tn」那一档 = 卡上没有 `rc`）**：同构夹具 `RandomChoice = false` ⇒ 护甲 **6**（两组全跑 = 改前行为） |
| `E4` | **灭自证**：`带 rc` ≠ `不带 rc` |

🧨 怎么才能红：删掉 `:134` 那层包装 ⇒ E1/E2 变 6 ⇒ 红，E4 变「相等」⇒ 更红；
把第 7 形参 `tn` 丢掉（`RandomAbilityChoiceOps` 内部只剩 `TriggerNameOfKeyword(keyword)`，而数值路 `keyword` 是 null）
⇒ 两个都变 6 ⇒ 红。

### 8·4 typecheck

`TMPDIR=/tmp/wf_w19b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（全部改动落地后跑的）。
⛔ 未跑 Unity · ⛔ 未跑 `play check` · ⛔ 未动 git · ⛔ 未碰 `BattleScene.cs` / `ShellScene.cs`。

### 8·5 ⚠️ 顺带改掉的一处**夹具缺陷**（第一轮留下的）

第一轮的 `Fixture` 在 `randomChoice = false` 时把 `animTimes` 传 **`null`**。
放到数值路（B/C 组）上没暴露，但**附魔那条路会坏**：`EmitCardTrigger` → `HasTriggerSource`
在 `AbilityTriggerPresence == -1` 时**回退看动画行**（`EffectResolver.cs:8367-8378`），没有动画行 ⇒
附魔那一趟**整条发不出来** ⇒ 对照组会被读成「0（没跑）」而不是「6（两组全跑）」⇒**灭自证失效**。
⇒ 已改成「**两组都用同一张动画行，只差 `RandomChoice` 那一格**」。
🔑 这一改让 B/C/E 三组都变成**更强的同构**：夹具之间**只差 `rc` 这一个 bit**。

### 8·6 影响面（附魔那半条）

- 附魔能力要落进这一趟，前提是「载体身上有附魔 ∧ 附魔卡的这条 trigger 有正文 op」。
  现池唯一有非 0 trigger 的附魔只有 `Pray Enchantment`，它的 `TriggerOpsByNumber(60) == null`
  （`B109_E6` A3 已断）⇒ **今天零可观测**（与第一轮同一条推理）。
- 唯一 `rc = 1` 的 `GOF12 Spanner` 不是附魔卡 ⇒ 也够不到这一趟。
- `ctx.GetRandomInt` 的消费点同样不变 ⇒ **`play check` 指纹应仍逐字节不变**（请调度台复核）。

---

## 九、§六·2（`LogicIndex < 0` 时跑全部）的裁定：**不修**

> 调度台问「能不能给出不猜的判据」。**判不出来，所以不改。** 下面是查到的全部事实与理由。

### 9·1 `GOF12 Spanner` 的真实形状（`cards_engine.json` v47 现扫）

```text
name = Spanner · desc = "Give a random bonus to a friendly Vehicle" · keywords = ["Mob"]
abilityTriggers = [300]
triggerAbilityIndices = 5 行：
  {ab:0, lg:0, trigger:300, triggerValue:0, ca:30 (buffOthers)}
  {ab:0, lg:1, trigger:300, triggerValue:0, ca:30}
  {ab:0, lg:2, trigger:300, triggerValue:0, ca:30}
  {ab:0, lg:3, trigger:300, triggerValue:0, ca:30}
  {ab:0, lg:4, trigger:300, triggerValue:0, ca:30}
```

⇒ 原版这一刻做的是：**在 5 条 `abilityLogic` 里随机抽 1 条**、只跑那一条（`TriggerAbility.c:84-90`）。

### 9·2 为什么这不足以推出一个「不猜」的修法

1. **卡面文字里没有 5 个分支**：`desc` 只有一句 "Give a random bonus to a friendly Vehicle"，
   5 条 `abilityLogic` 的差异（各自的 `buff` 载荷）**不在文本层**——`abilities` 视图在 `cards_engine.json` 里是 `null`。
2. **我们手上的 op 是文本派生的**：`RandomAbilityChoiceOps` 按 `EffectOp.LogicIndex` 分组，
   而 Spanner 的 op 是由卡面文本解析出来的（`EffectText.EmitEitherOr` 那一类），**没有 5 条的对应关系**
   ⇒ `LogicIndex < 0`。要「不猜」，得先证明「第 i 个 op ↔ 第 j 条 abilityLogic」，**本机没有那个判据**。
3. **更糟的一种可能**：若文本层已经把它建模成「随机挑一个 bonus」的 op（`RandomPick`），
   那再叠一层原版的 `randomAbilityChoice` 就是**双重随机**——比「跑全部」离原版更远。
   这一条**没从反编译证否也没证成**（现有记录只说「解析成一条 `chooseone + RandomPick` op」，
   而我这一轮**没有**任何能跑的读数口去核 Spanner 实际的 op/`LogicIndex`）。

### 9·3 结论与「回来要什么」

- **不改**。理由：改它必然要在「op ↔ abilityLogic」上**猜一个映射**，或猜「文本层是否已经随机过一次」——
  两者都违反铁律 2（没查到就说没查到）与 18·0（先证明原版那条分支，再谈替代）。
- **`LogicIndex < 0` 的现有处置**（原样跑全部 + 一次性 `Debug.LogWarning`、不进 `ctx.Log`）**保持不变**：
  它至少**不静默**，且**不动指纹**。
- **要把它做对，先补这个读数**（不在本轮白名单，交调度台排）：
  在 `RuleEngineTest` 或 `ruleprobe` 里把 `GOF12` 的
  `TriggerOpsByNumber(300)` / `TriggerOps("mob")` 逐条打出来（`Verb` / `Tail` / `AbilityIndex` / `LogicIndex`），
  **看一眼实际有几条 op、`LogicIndex` 是不是真的 `-1`**。
  · 若**确实 ≥2 条且 `-1`** ⇒ 还要再证「op 与 lg 的对应关系」才谈得上修；
  · 若**只有 1 条** ⇒ `RandomAbilityChoiceOps` 在 `ops.Count < 2` 那一步就返回了，**这一档根本够不到**，
    §六·2 这条账应当按「判据本身错 / 前提不成立」核销，而不是当缺陷修。
- ⛔ **没有按卡 id 打补丁**（`B90` 的原注释已经写死这条）。
