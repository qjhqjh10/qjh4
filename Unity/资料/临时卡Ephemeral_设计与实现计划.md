# `Ephemeral`（临时）+「移出游戏」区域：设计与实现计划（2026-09-13 第三十二轮）

> 用户 2026-09-13 明确要求：**引擎 + 卡面标记 + 按规则书，一起做满，一劳永逸。**
> 「不要害怕麻烦，否则在真实运行时可能还会出现别的问题。」
>
> 所以这份文档**先把规则书的每一句话、原版代码的每一处出处找齐**，再写实现。
> 出处都带 `文件:行号`；查不到的一律写「查不到」或「这是我们挑的」。

---

## 一、规则书怎么说的（**权威，逐句**）

全部出自 `资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md`：

| 行 | 原文 | 要点 |
|---|---|---|
| `:183` | **临时（Ephemeral）** \| 回合结束时若在手牌则移除 | 关键词表里的定义 |
| `:229` | **临时卡**：天赋、伴生生成的部队、带潮涌的复制（在手牌时）均临时，回合结束未打出即消失——**从游戏中移除（非弃置）**。督军天赋每回合循环，故可反复使用 | 🔴 **「从游戏中移除（非弃置）」** + **`未打出`** 这个限定 |
| `:218` | **天赋（Talent）** \| 回合开始时在手牌中生成临时战术 | Talent 是**生成临时卡**的来源 |
| `:220` | **潮涌 X（Tide X）** \| 从手牌打出时：本回合可打出 X 张额外复制；费用与首张相同 | Tide 复制**在手牌时**也是临时的 |
| `:176` | **伴生 X（Companion X）** | `:229` 点名它生成的部队也是临时的 |
| `:155` | 向牌库加回卡牌**不重置**疲劳计数 | （与「移出游戏」无直接关系，但同属「牌去哪了」这一族，记着别混） |

**英文原版**（`…_英文原版.md:421-427`，这一段的**标题就叫 Ephemeral Cards**）：
> "Cards like Talents, troops created by Companion, and copies of troops with Tide (while in hand)
> are ephemeral and vanish at end of turn if not played. For the physical game, place these cards
> to the side away from the [deck/discard]."

⇒ **实体版的处置方式是「放到一边，离牌库和弃置堆都远」** —— 这就是「第三个区域」的物理解释，
**它不是墓场、不是牌库、不是手牌**。

### 从规则书能确定的四条

1. **触发时机 = 回合结束时**（自己的回合结束，不是每个回合结束）。
2. **条件 = 还在手牌**（打出去了就不是「未打出」，自然不适用）。
3. **去向 = 从游戏中移除**，**不是**弃牌堆。
4. **不是只有带 `Ephemeral` 关键词的卡才有** —— `:229` 点名的三族
   （天赋 / 伴生生成 / 潮涌复制）**都是临时的**，而那些卡**卡面上不一定印 `Ephemeral`**。

### ⚠️ 规则书**没说**的（要么查原版、要么我们自己定并标出来）

- 「移除」之后那些卡**还能不能被任何效果拿回来**（规则书没写）
- 移除**要不要发事件 / 要不要有动画**（规则书是实体版，没这个概念）
- 对手能不能看见「你移除了什么」（同）

---

## 二、原版代码怎么做的（出处齐全）

### 2.1 数据侧：Ephemeral 是**关键词 + BuffType** 两条腿

| 出处 | 内容 |
|---|---|
| `DefinedTrait.cs:4` | `ephemeral = 5` —— 关键词枚举值 |
| `BuffType.cs:20-21` | **`ephemeralCopy = 25`** / **`tideCopy = 26`** —— **两个独立的 buff 类型** |

🔑 **这是最关键的一条**：原版把「临时」分成**两个来源**，而且用的是 **buff（挂在具体那张牌实例上）**，
不是只看关键词。⇒ **印证了 `:229` 那句**：潮涌复制**本身卡面没有 Ephemeral 关键词**，
它是被**打上 `tideCopy` 这个 buff** 才变临时的。

⇒ **我们这边要跟着改成两路**：
- ① **关键词路**：卡面/数据里带 `Ephemeral` 的卡（98 张）—— 牌在手里就是临时的
- ② **标记路**：`create`/`tide` 造出来的复制 —— **造的时候打一个「临时」标记**
  （⚠️ 走 `BuffType` 那套精神：**标记挂在「这一张牌」上，不是挂在卡的模板上**）

### 2.2 表现侧：**打卡面标记 + 故障（glitch）材质**

| 出处 | 内容 |
|---|---|
| `CardScript__AddEffect.c:949-950` | `AddTraitSilently(card, 5, …)` 之后立刻 `BattleCardUI__ShowEphemeral(cardUI)` |
| `CardScript__ActivateCard.c:141-143` | `HasCurrentTrait(card, 5)` 为真 ⇒ `Card2DController__ToggleGlitch(controller, 1)` |
| `Card2DController.cs:38` | `[SerializeField] private Material glitchMaterial;` |
| `Card2DController.cs:92` | `public void ToggleGlitch(bool option)` |
| `BattleCardUI.cs:242` | `[ColorUsage(true, true)] public Color ephemeralCardPlayableColor;` |
| `BattleCardUI__ShowEphemeral.c`（77 行） | 在**卡的位置**实例化一个状态图标 prefab，再 `SetupStatusFrame(...)` |

⇒ **原版的卡面标记是「故障/干扰」视觉**（glitch material 换掉卡面材质），
外加一个状态图标。**这是一处「原版有、我们完全没有」的表现**。

### 2.3 ⚠️ **原版「回合结束移除」那一段没找到**

- `BattleManager.cs`（7437 行签名桩）里搜 `ephemeral` **零命中**
- 反编译的 1800 个 `.c` 里，`ephemeral` 只出现在
  `BattleCardUI__ShowEphemeral.c` · `CardScript__ActivateCard.c` · `CardScript__AddEffect.c`
- `PlayerHand.cs` / `CardScript.cs` / `EntityScript.cs` 里也**没有**

⇒ **「什么时候、按什么条件把临时卡扫掉」这一段的原版实现，本地查不到。**
   规则书 `:183`+`:229` 写得够清楚，**按规则书做**，并如实标「原版实现没查到」。

### 2.4 🔴 glitch 素材**本地没有**

- `d:/2/新解包资源/` 全盘 `*glitch*` **零命中**
- `d:/2/` 全盘 `*glitch*` **零命中**
- 我们的特效导出报告 `WarpforgeVFX/导出报告.tsv` 里 **零命中**

⇒ 按工程既有做法（77 个原版 shader 逐个自建替代），**自建一个 glitch shader**。
   **这是我们挑的视觉**，不是原版参数 —— 会在代码注释里标明。

---

## 三、引擎侧：要动什么

### 3.1 关键词补进表（**一行的事，但少一行就是静默失败**）

`CardDef.Prefixes` 加 `ephemeral`，`Implemented` 加进去。
⚠️ **不补 `Prefixes` 的话**：`Normalize` 返回 null ⇒ 被 `KeywordTable.Parse` 与构造函数**双双丢弃**
⇒ **既不在未实现清单上、卡面也不打 `*`** —— 就是第三十二轮刚修过的
`talent`/`ferocity`/`quest` 那个坑（见 `资料/关键词三列对账.md` §不一致·3）。

### 3.2 「移出游戏」区域 —— **新开一个，和弃牌堆分开**

规则书 `:229` 明写「**非弃置**」。所以：

| 区域 | 现有 | 新增 |
|---|---|---|
| 牌库 `Deck` | ✅ | |
| 手牌 `Hand` | ✅ | |
| 弃牌堆 `Discard` | ✅ | |
| **移出游戏** | ❌ | **`BattleContext.Removed`**（`List<RemovedCard>`，仿 `DeadUnits` 的写法） |

`RemovedCard` 记 `{ Card, Owner, Turn, Reason }`：
- `Card` —— 哪张（**卡模板**，和 `Discard`/`DeadUnits` 一致）
- `Owner` —— 谁的
- `Turn` —— 第几回合移出的（`since your last turn` 那类窗口要它）
- `Reason` —— `ephemeral` / 以后的其它（日志与排查用）

⚠️ **为什么不用一个「已移除」集合了事**：`DeadUnits` 当初就是这么做的
（`资料/…` 里记着它是「`Choose a … that died this game` 的候选来源」）。
**同一类需求会长出第二批**（`this battle 移出过什么`），所以一次做成能查询的结构。

### 3.3 「这一张牌是临时的」——**实例级标记，不是模板级**

🔑 **这是本活最容易做错的一处。**

`CardDef` 是**共享不可变**的模板（`cards_engine.json` 里一张卡一个对象）。
- ✅ **卡面/数据里带 `Ephemeral` 的卡**：那张卡的**所有实例**天生临时 ⇒ 看 `CardDef.Has("ephemeral")` 即可
- ❌ **潮涌/天赋造出来的复制**：**只有那一张是临时的**，而它和原件**是同一个 `CardDef`**
  ⇒ 看 `CardDef` 会**把原件也当成临时的**

原版对这件事的答案是 **`BuffType.ephemeralCopy`（buff 挂在牌的实例上）**（`BuffType.cs:20`）。

⇒ **我们这边**：`BattleContext.EphemeralCards`（`List<CardDef>` 或计数表），
造牌时登记、移出时销账。判据**只有一处**：

```csharp
bool IsEphemeral(BattleContext ctx, CardDef c)
    => (c != null && c.Has("ephemeral")) || ctx.MarkedEphemeral(c);
```

⚠️ **和 `资料/事件层_数据与设计.md` §三 那个「卡实例身份」是同一族问题的第二个实例** ——
第一处是**费用修正按卡名**（规则书审计第③条），这里是第二处。
**做这一轮的时候要顺手看一眼**：能不能一次把「卡实例身份」这件事做成一个小而通用的东西
（`BattleContext` 上一张按 `CardDef` 计数的表），把两处一起解掉。
**如果做不到就先各做各的，但要如实记着「这是第二次撞上同一个问题」。**

### 3.4 回合结束的清扫（**新增一段结算**）

位置：`RuleCore.EndTurn`（或 `BeginTurn` 的开头，二者取一，见下）。

```
对**即将结束回合的那一方**：
  遍历 Hand，凡是 IsEphemeral 的 ⇒ 从 Hand 拿走 → 进 Removed → 发事件 → 日志
```

⚠️ **放在 `EndTurn` 而不是 `BeginTurn`**：规则书 `:183` 是「**回合结束时**」。
   ⚠️ 但要想清楚**谁**的回合结束 —— `EndTurn` 结束的是**当前行动方**的回合，
   而临时卡是**持有者自己的**回合结束才消失。
   ⇒ **扫的是「当前行动方」的手牌**（对方手里的临时卡要等他自己回合结束），
   这一条**和规则书字面一致**，且和「天赋每回合循环」对得上（`:91`）。

**遍历要快照**：移除会改 `Hand` 列表，边遍历边改是未定义行为
（和 `ResolveDeploy` / `BroadcastWhen` 同一条教训）。

### 3.5 要一起看的几处「牌去哪了」的判据

| 判据 | 现在 | 要不要管「移出游戏」 |
|---|---|---|
| 弃牌堆（`Discard`） | `Discard.Add` | ✅ **移除的卡不进弃牌堆**（规则书 `:229`） |
| `DeadUnits`（阵亡登记） | 只记**单位阵亡** | ❌ 不相关（那是场上单位，不是手牌） |
| 手牌上限 | `create` 超上限进弃牌堆 | ⚠️ **要确认**：临时卡算不算「手牌」？规则书没写 ⇒ **算**（它就在手里），如实标 |
| 疲劳（`:155`） | 抽空计数 | ❌ 不相关 |

---

## 四、表现侧：要动什么

### 4.1 卡面「临时」标记（**原版是 glitch，素材没有，自建**）

| 项 | 怎么做 | 出处 |
|---|---|---|
| **视觉** | 卡面材质换成自建 glitch shader（UV 抖动 + 扫描线） | 原版 `Card2DController.ToggleGlitch` / `glitchMaterial` —— **但那是原版资产，本地没有**。⇒ **自建**，文件头标「这是我们挑的」 |
| **时机** | 手牌里 `IsEphemeral` 为真时打开 | 原版 `CardScript__ActivateCard.c:141-143`（`HasCurrentTrait(5)` 就开） |
| **颜色** | 原版有 `ephemeralCardPlayableColor`（可打出时的着色） | `BattleCardUI.cs:242` —— ⚠️ 具体色值在预制体里，**本地拿不到**，自己挑 |

⚠️ **落地顺序**：先做「有标记」，glitch shader 可以后补（先用一个纯色描边/角标顶上），
**但必须显式标出「这是占位」**，别让它看起来像原版。

### 4.2 移除时的表现

规则书没说。原版查不到。
⇒ **我们挑**：卡从手牌**淡出 + 缩小**消失（复用 `CardFeel` 现成的手感补间），**不播阵亡特效**
（它不是死了，`EvtKind.Return` 那条注释里正好讨论过同类问题）。

---

## 五、验收：**怎么证明它真的对**

⚠️ 用户特别强调「否则在真实运行时可能还会出现别的问题」—— 所以断言要**分三层**：

| 层 | 断言什么 | 为什么必须 |
|---|---|---|
| **1 机制** | 回合结束 → 临时卡**从手牌消失**、**出现在 `Removed`**、**不在 `Discard`** | 钉住规则书 `:229` 的「非弃置」 |
| **2 反例** | **非**临时卡**不动** · **打出去的**临时卡**留着**（在场上/弃牌堆） · **对手手里**的临时卡这一回合**不动** | 「不该生效的没生效」（第三十轮的教训） |
| **3 实例级** | 造一张复制并标成临时 ⇒ **原件不受影响**，只有复制被移除 | 钉住 §3.3 那个最容易错的地方 |
| **4 实战** | 整局跑完：**没有卡凭空消失/凭空多出来**（`Hand + Deck + Discard + Board + Removed` 守恒） | 这一类「牌去哪了」的 bug **只会在大局里暴露** |
| **5 表现** | 卡面**真的画出了标记**（截图 + 断言判据） | 「改动要加断言」和「改版面要看图」是两条并行规矩 |

---

## 六、这一轮**不**做的（如实记着）

| 不做 | 为什么 |
|---|---|
| **`Talent`（91 张）** | 它是**生成**临时卡的来源（`:218`），要「回合开始往手里塞一张战术」那一整套。`Ephemeral` 是它的**地基**，先地基后它 |
| **`Companion`（9 张）** | 同上，`伴生 X` = 打出时从牌库带出 X 张 |
| **`Tide X`（24 张）** | 要「本回合可打出 X 张额外复制；费用与首张相同」。⚠️ **但它的复制是临时的**（`:220`+`:229`）⇒ 这一轮把「标记成临时」的**接口**留好（§3.3），下一轮接 |
| **glitch shader 的原版参数** | 本地没有素材，拿不到参数 |

---

## 七、动手顺序（一轮之内）

1. `CardDef.Prefixes` + `Implemented` 补 `ephemeral`（**先跑一次自检，看未实现清单会不会从 26 掉到 25**）
2. `BattleContext.Removed` + `RemovedCard`（新区域，含查询接口）
3. `BattleContext` 上「哪些牌实例被标成临时」+ `IsEphemeral(ctx, card)`（**判据只此一处**）
4. `create`/造牌那几条路上打标记（**接口留好，Tide 下一轮接**）
5. `RuleCore.EndTurn` 加清扫段（快照遍历 + 发事件 + 日志）
6. 断言 1–4 层（`RuleEngineTest` 新增一节）
7. 表现侧：卡面标记 + 移除补间（`CardView` / `CardFeel`）
8. 跑**四条自检** + 看截图
9. 更新 `资料/阵营推进_清单与交接.md` §五 的数字 + 本文档

---

## 八、做完了什么（2026-09-13 第三十二轮收工）

| # | 件 | 落点 | 状态 |
|---|---|---|---|
| 1 | `ephemeral` 进关键词表 | `CardDef.Prefixes` + `KeywordTable.Implemented` | ✅ 未实现清单 **26 → 25** |
| 2 | 「移出游戏」区域 | `BattleContext.Removed` + `RemovedCard`（`:Card/Owner/Turn/Reason`） | ✅ |
| 3 | 临时身份的判据 | `BattleContext.IsEphemeral` / `MarkEphemeral` / **`TryTakeOneEphemeral`** | ✅ **判据只此一处** |
| 4 | 造牌那条路打标记 | —— | ⏸ **接口留好了，Tide/Talent 实现时调 `MarkEphemeral`** |
| 5 | 回合结束清扫 | `RuleCore.EndTurn` → `SweepEphemeral` | ✅ 位置与理由见下 |
| 6 | 断言 | `RuleEngineTest.TestEphemeral`（4 层） | ✅ 自检 **1313/1313** |
| 7 | 卡面角标 | `CardView.ShowEphemeral` + `CardArt.Trait` + 导入 78 张关键词图标 | ✅ 代码就位，**图标待导入**（见下） |
| 8 | 四条自检 + 截图 | —— | ⏳ |
| 9 | 更新交接文档 | —— | ⏳ |

### 🔴 实现途中**测试抓出来的一个真 bug**（值得记下来）

**症状**：手里有两张**同名**卡、只把其中一张标成临时 ⇒ 回合结束**两张都被移走**。

**根因**：`CardDef` 是**共享不可变**模板，「标记」只能**按卡记份数**，
而 `IsEphemeral(cardDef)` 对**同名的每一份**都返回 true。
第一版清扫写的是 `foreach (手牌) if (ctx.IsEphemeral(c)) 移除` —— 判断用的是卡模板，不是那一份。

**修法**：把「认领一份」这件事做成一个方法 `TryTakeOneEphemeral(c)`：
① 卡**自己带关键词**（98 张那种）⇒ 每一份都该走；② 否则**吃掉一份标记**、其余同名的不受影响。
清扫段改用它，**并且不在别处再销一次标记**（销两次会把同名另一份的标记也吃掉）。

⇒ 这条正好命中用户担心的那类问题（「真实运行时才会发现」）：它**不在单卡场景里出现**，
要**两张同名卡**才露头 —— 而这在有 1130 张卡的实战里是会发生的。

### 清扫段的位置（顺序是挑过的）

放在 `EndTurn` 里，**`本回合限时增益到期` 之后** → **`ResolveAtTurn("turn_end")` 之前** →
**`ctx.Active` 换边之前**。三条理由都写在 `RuleCore.SweepEphemeral` 上面，摘要：
① 和 `rule_core.gd:2010` 那一串的开头对得上；② 规则书 `:229` 是「回合结束**未打出即消失**」，
放 `turn_end` 触发之前 ⇒ **手牌里等回合结束的临时卡看不到那一下触发**（⚠️ 规则书没写谁先，**我们挑的**）；
③ 扫的是**这一方自己的**手牌（`:91`「督军天赋每回合开始加入、回合结束移除」的循环靠它）。

### 卡面角标：**用原版真有的关键词图标，不是 glitch**

- 原版路径是 `BattleCardUI.ShowEphemeral()`（在卡的位置实例化状态图标 prefab）
  + `Card2DController.ToggleGlitch()`（换 `glitchMaterial`）。
- ⚠️ **glitch 素材本地没有**（`d:/2` 全盘 `*glitch*` 零命中，特效导出报告也零命中）。
- ⇒ 改用原版图集里的 **`Atlas_trait_icon_ephemeral.png`**（80×80，78 张 trait 图标之一，
  **切片文件名就是英文关键词**）。导入走 `工具/import_original_art.py` 新增的 `TRAIT_SRC` 段。
- ⚠️ **位置/大小是「我们挑的」**：那个 prefab 不在我们手上，dump 里也看不到它的 rect。
  挑**左上角**是因为卡面那儿是空的（右上费用宝石、左下两圆、右下盾+绿框、中间卡名/效果/兵种）。
- ⚠️ **还没做**：glitch 那种「卡面材质抖动」的观感。要做就得自建 shader
  （工程有先例：77 个原版 shader 逐个自建替代）。
