# DIAG · 批次 C —— `RuleEngineTest` 3 红 ＋ `BattleScene` 1 红（只读分诊）

> 只读诊断（2026-10-19 00:00–00:30）。本报告没有改任何 `.cs` / 数据 / 正本；没跑 Unity；没动 git 写。
> 基线（现读，诊断期间可能有别的代理在写）：
> - 日志：`d:/4/_tmp_view/ruleengine.log`（2026-10-18 23:59，`RE === 结束：5482/5485 通过，**3 条失败** ===`，`:86510`）
>   · `d:/4/_tmp_view/battle.log`（2026-10-19 00:00，`BT === 合计：3277 通过 / 1 失败 ===`，`:301889`）。
> - 上一轮绿：`rule_run2.log`（10-18 13:18，同一条实战检查 **`被登记 4 次 · 部署触发段跑了 5 次 · 筛中 Drone 5 次`**，`:53004`）·
>   `battle_run3.log`（10-18 13:33，`3274 / 0`，本报告 #4 那条断言在 `:145375` 是 ✓）。
> - 源码现读时间：`RuleEngineTest.cs` mtime **2026-10-18 23:01**（早于本次日志）；
>   `CardView.cs` mtime **2026-10-19 00:10**（诊断期间被别的代理改过一次，本报告引用的是 00:10 版，行号可能小幅漂移）。
> - 数据现读：`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` mtime **2026-10-18 23:23**（`git status` = M）。

## 〇、总览

| # | 宿主:行 | 断言原话（短） | 判定 | 一句话根因 |
| --- | --- | --- | --- | --- |
| 1 | `RuleEngineTest.cs:8630`（日志 `:53172` / `:86511`） | 整局里**部署触发段真的跑过** —— True / False | **(δ) 夹具前提不成立** | 上轮补的「AI 回合前补打一张 Drone」只会在**登记之后的那个自己的回合**才有机会；4 局都结束得很早（离线复刻现读：当前池 4/4 局 `ctx.Turn=7` 就结束），登记一旦落在最后一个自己回合，它一次都跑不到。机制本身没问题（同测试 ① 段夹具绿、离线复刻也能强制点亮） |
| 2 | `RuleEngineTest.cs:8631`（`:53187` / `:86512`） | 整局里**真的筛中了 Drone** —— True / False | **(δ) 与 #1 是同一个红** | `fired` 与 `granted` 数的是**同一行日志**（`EffectResolver.cs:5785` 的 `—— 部署触发段：「…」盯上 …` 同时含两个子串）⇒ 两者恒等，不可能一红一绿。修 #1 即修 #2 |
| 3 | `RuleEngineTest.cs:20279`（日志 `:56642` / `:86513`） | （前提）`AM15 Minka Lesk` 的效果文本里**没有** `Cadian Honour` 这几个字 | **(α) 断言过期** | 数据侧本波按**卡面原文**给 AM15 补了 `Talent: Cadian Honour`（PnP 成品卡逐字可核）⇒ 前提不成立。**判别力没有丢**：`CreatePool.StripTalentSegments` 会把 `Talent:` 段剥掉，替身「点名」那一支仍然产不出它（离线现算 = 空表） |
| 4 | `BattleScene.cs:12705`（日志 `:143626`） | 绿光材质 … **队列 3000** | **(γ) 本批回归（实现是有意的，断言需同步）** | 本批 `A1674`（未提交，`CardView.cs` 工作区）把 `SetQueue` 扩到 `ParticleSystemRenderer`；场上卡那一段队列是 **2962..2979**（`BoardLayout.QueueOf`）⇒ 绿光材质的 `renderQueue` 被盖成那一格自己的号，不再是原 prefab 的 3000。⛔ 不要回退 A1674（它自己的断言 `CardFaceProbe.cs` 要求盖成卡段号）；改 BattleScene 这条断言 |

**归并**：#1/#2 = 一条（同一个计数口径）；#3、#4 各一条。**没有一条是引擎实现缺陷。**

---

## 一、逐条

### 1) / 2) 「部署触发段真的跑过 / 真的筛中了 Drone」 —— **(δ) 夹具前提不成立**

**断言与实得**

- `RuleEngineTest.cs:8630` `CheckTrue(fired > 0, "整局里**部署触发段真的跑过**…")`
- `RuleEngineTest.cs:8631` `CheckTrue(granted > 0, "整局里**真的筛中了 Drone**…")`
- 实得（`ruleengine.log:53144`）：`实战：4 局里 \`Experimental Drones\` 被登记 2 次 · 部署触发段跑了 0 次 · 筛中 Drone 0 次`
- 上一轮绿（`rule_run2.log:53004`）：`被登记 4 次 · 部署触发段跑了 5 次 · 筛中 Drone 5 次`

**先排除「Events 2000 条上限把回读冲掉」这个可能**

- 上限确实存在：`BattleContext.cs:534` `EventCapacity = 2000`、`:1407` 超了从**头部**裁。
- 但它不是本次的原因：本次 4 局每局的 `ctx.Events` 只有 **85–98 条**（离线复刻现算，当前池；见下），上一轮绿的旧池也是 **124–241 条**。要冲掉需要 ~140+ 个回合，而 `while` 的 `guard` 上限是 300、且这 4 局在 `ctx.Turn=7..15` 就分胜负了（`RuleCore.EndTurn` 的胜负判定 + 督军阵亡）。

**机制本身没有缺陷（硬证据）**

- 同一条测试的 ① 段（`RuleEngineTest.cs:8493-8499`）用 `FixtureDrone` 直接部署，断言「常驻效果真的把护盾加上了」——本次日志 `:52798` / `:52813` **都是 ✓**。
- `ResolveDeploy` 的出口只有一处日志：`EffectResolver.cs:5785`
  `ctx.Log($"—— 部署触发段：「{name}」盯上 {unit.Name}（{pe.Criteria}）→ {pe.Body} ——")`。
- 注意：`fired` 数的是 `部署触发段：`（`RuleEngineTest.cs:8623`），`granted` 数的是 `盯上`（`:8624`），**两个子串在同一行日志里**
  （全仓 `盯上` 只有这一处，`grep` 已核）⇒ **`fired` 与 `granted` 恒等**。#2 不是独立的红。

**为什么「上轮补的夹具」没救回来（核心）**

上轮按方案 (a) 在 `RuleEngineTest.cs:8597-8614` 补了一段：每个回合开始时，如果
`ctx.PersistentEffects` 里有 `Trigger=="deploy" && Owner==ctx.Active` 的条目、且 `ctx.Events` 里还没出现
`部署触发段：`，就从手里找第一张 Drone、走 `RuleCore.PlayCard` 打出去。

它的**结构**决定了它救不回这一局：

1. **它排在 `PlayAiTurn` 之前**（`:8597-8614` 在 `:8615` 之前）。而这张战术卡是 **AI 在 `PlayAiTurn` 内部打出的**
   （`SimpleAI.NextPlay` → `RuleCore.PlayCard` → `PlayTactic` → `DoPersist`，`EffectResolver.cs:4916-4960`）。
   ⇒ **登记发生的那个回合，这段代码已经跑完了**，它最早只能在**下一个自己的回合**起作用。
2. 4 局都很短。离线复刻（真实 `Core/` 源码 + 真卡池 + 相同种子/牌组）现算：
   **当前池：4/4 局都在 `ctx.Turn=7` 结束**（P1 的回合是 1/3/5/7；战术卡 4 费，P1 最早第 3 个自己的回合 = T5 才打得起）。
   登记落在 T7（最后一个 P1 回合）⇒ **补打段一次都轮不到**。
3. 就算轮到了，它也有两个「一次不成就放弃」的口子：只看**手牌第一张** Drone（`:8608-8609`，那张付不起就整轮不试）、
   `FirstFreeSlot` 满了就整轮不试（`:8610`）。
4. **它全程不打日志**（成功/失败都不 `ctx.Log`）⇒ 从 `ctx.Events` 里**看不出它到底跑没跑、为什么没打**。
   这正是这次只能靠离线复刻才能定性的原因。

**离线复刻的旁证（本轮做的，工具在仓库外：`%TEMP%/wf_diagC`）**

用 `RuleEngine/Core/*.cs` + `Data/{CardDatabase,DeckBuilder,SimpleAI,LegacyCardIds}.cs` 原样编成 net8 控制台，
把 `TestDeployBuffAndMore` 第③节的 4 局逐字复刻（同 `seed: 9000+g`、同 `new System.Random(400+g)`、同全 Drone 牌组）：

| 卡池 | 4 局结果 | 每局 events | 说明 |
| --- | --- | --- | --- |
| `87c5fe5`（10-18 11:46，13:18 绿跑那一代） | 登记 4 · 触发 4 · 筛中 4 | 124–241 | 与 `rule_run2.log:53004` 的「登记 4 次」吻合 |
| `HEAD`（10-18 22:15，改名后） | 登记 0 · 触发 0 · 筛中 0 | 85–98 | AI 一次都没打这张 4 费卡；4/4 局 T7 结束 |
| 当前工作区（10-18 23:23） | 登记 0 · 触发 0 · 筛中 0 | 85–98 | 同上 |

并且**旧池那次 g2 局里，正是这段补打代码点亮了机制**：T11 打印 `forced=[PLAY ok r=0 hi=0 sl=0]`，随后
`firedYet=True`、该局 `fired=3`。⇒ **代码路径是通的，缺的是「登记之后还有一个自己的回合」这个前提。**

**该照哪个判据**

- 机制判据：同测试 ① 段（`RuleEngineTest.cs:8493-8499`）+ 卡面原文（`Experimental Drones`：`For the rest of this battle, give Shield to all Drones you deploy`）。
- 夹具判据：本测试 `:8548-8568` 自己的注释「**登记之后任何一次部署都是 Drone，断言才有意义**」——
  它假设了「登记之后还会继续部署」，而这条假设仍然挂在**固定种子的轨迹**上（`:8551` 写的「不会 flaky」只在数据不动时成立）。
- 上轮定性（`普查产出_1018/DIAG_red_A_RuleEngine_Battle.md` §一·1)2)）的方向**仍然成立**：数据侧 `579bcad`
  （`GOF12 Ork Spanner` → `Spanner`）让 Goff 的 `GOF15 Burna Boy` 条件第一次真的成立 ⇒ 同一批种子下轨迹改变。

**建议修法（只写建议，需跑一次验证）**

1. **把「登记 + 消费」都变成确定性的**（推荐）。在 `RuleEngineTest.cs` 的 4 局循环里，挑一个**自己回合**（例如 `ctx.Active == 0 && ctx.Players[0].TurnCount >= 3`）：
   - 若 `Experimental Drones` 还在手里且 `RuleCore.CanPlayTactic(...) == OK`，**由夹具自己 `PlayTactic` 打它**（不再等 `SimpleAI`）；
   - 紧接着同一个回合内，找一张能付得起、且 `FirstFreeSlot >= 0` 的 Drone，`RuleCore.PlayCard(...)`。
   这样 `registered/fired/granted` 三个计数都由夹具保证，不再依赖 AI 何时想起这张 4 费卡、也不依赖局长。
2. **在补打段里补 `ctx.Log`**（成功和失败各一句，失败带上 `hand/energy/slot/CanPlayCard` 返回码）。
   否则下一次红仍然只能靠离线复刻才查得动。
3. **#1/#2 合并成一条断言**（或保留两条但在注释里写明「同一个计数」，免得下一轮把它当两条独立红去修）。

---

### 3) 「AM15 的效果文本里没有 `Cadian Honour`」 —— **(α) 断言过期**

**断言与实得**

- `RuleEngineTest.cs:20279-20282`：
  `CheckTrue((minka.Desc ?? "").IndexOf("Cadian Honour", StringComparison.Ordinal) < 0, "（前提）…")`
- 实得（`ruleengine.log:56642`）：`desc=\`Friendly Infantry costs 1 less. Talent: Cadian Honour\``。

**数据是对的（判据 = 成品卡图）**

- PnP 成品卡逐字核过：`d:/2/Warpforge部队卡片/Astra Militarum/3部队/Warpforge_16_Minka-Lesk.png`
  —— 卡面正文就是 `Friendly Infantry costs 1 less. [icon] Talent: Cadian Honour`。
- 数据侧落点：`数据/游戏数据/cardface_fixes.json` 的 `desc` 字典有一项 `"Minka Lesk": "Friendly Infantry costs 1 less. Talent: Cadian Honour"`
  （工作区，mtime 2026-10-18 23:23；`git show HEAD:…cards_engine.json` 里这一条**还没有** `Talent:` 尾巴）；
  现读 `cards_engine.json` AM15：`desc` 与 `keywords` 都是 `Talent: Cadian Honour`、`descZh` = `天赋：卡迪亚荣誉`。
  ⇒ **这是「按卡面补全」，不是数据错。**

**判别力没有丢（这一点很重要，别把断言删了了事）**

这段的意图是「**真值优先**」：`RelatedCards.Find` 的 ⓪ 支（`RelatedCards.cs:62-73`）在 `RelatedCardData.Get(id) != null`
时只列真值、**替身三支一支都不跑**。为了证明它，测试需要一张「替身结构上产不出真值那张卡」的卡。

- `MentionedCards`（点名那一支）在匹配前会先 `StripTalentSegments`（`CreatePool.cs:1019-1042`）：
  以 `.` / `。` / 换行为段，段里从 `Talent:` / `天赋：` 起整截挖掉。
  AM15 的 desc 是两段：`Friendly Infantry costs 1 less.` + ` Talent: Cadian Honour` ⇒ 第二段被整段剥掉。
- 离线现算（真实 `CreatePool.MentionedCards`，当前池）：
  `MentionedCards(pool, AM15, desc)` = **空**；把 `keywords`（`Talent: Cadian Honour`）也拼进去再算，**仍然空**。
  ⇒ 替身「点名」产不出 `Cadian Honour`，下一句 `rel.Count == 1` / id 校验**仍然只在真值优先成立时才绿**。

**该照哪个判据**：卡面原文（上面那张 PnP 卡图）+ `CreatePool.StripTalentSegments`（`CreatePool.cs:1019-1042`）
+ `RelatedCards.cs:62-73`（真值优先）。**不要改数据**（把 `Talent:` 删掉会推翻卡面判据）。

**建议修法（只写建议）**

把「原始 desc 不许出现这几个字」换成**真正要保证的那件事**——「替身产不出它」：

```csharp
// 2026-10-19：卡面补了 `Talent: Cadian Honour`（PnP 卡逐字核过）⇒ 旧的「原始 desc 不许出现」前提过期。
// 判别力要看的是【替身结构上产不产得出】：StripTalentSegments 会把 `Talent:` 段剥掉 ⇒ 仍然产不出。
string why0;
var alt = CreatePool.MentionedCards(pool, minka, minka.Desc, out why0, 8);
CheckTrue(!alt.Exists(c => c.Id == "3a52026b99bb56f40aa9dc130fc66f34"),
          "（前提）点名那一支产不出 `Cadian Honour`（它只出现在 `Talent:` 段里，`StripTalentSegments` 会剥掉）"
          + $"｜desc=`{minka.Desc}`");
```

并把 `:20280` 那句注释按新原因改写（保留「为什么这条前提重要」的原意）。

---

### 4) 「绿光材质 = 原版 `Circle_Hoop Additive`（… 队列 3000）」 —— **(γ) 本批回归（实现有意，断言需同步）**

**断言**

`BattleScene.cs:12702-12706`（`Check` 调用在 `:12702`，失败消息在 `:12706`）：

```csharp
Check(prA != null && prA.sharedMaterial != null
      && prA.sharedMaterial.shader != null
      && prA.sharedMaterial.shader.name == "Universal Render Pipeline/Particles/Unlit"
      && prA.sharedMaterial.renderQueue == 3000,   // ← 这一句现在不成立
      "★ 绿光材质 = 原版 `Circle_Hoop Additive`（shader 就是 **URP 自带的 Particles/Unlit**，队列 3000）");
```

日志 `battle.log:143626` 报的就是它；上一轮绿 `battle_run3.log:145375` 同一条是 ✓（13:33，早于 A1674）。

**为什么现在不成立（逐跳证据）**

1. 本批 `A1674`（**未提交**，`CardView.cs` 工作区，mtime 00:10）把 `SetQueue` 扩展到粒子层：
   `CardView.cs:1100` `foreach (var pr in GetComponentsInChildren<ParticleSystemRenderer>(true)) … StampQueue(pr);`
   （`StampQueue` 见 `:1116`，它写的是 `mr.material.renderQueue = _queue`）。
2. 场上卡**每一格都有自己的号**：`BoardLayout.QueueOf(slot)`（`BoardLayout.cs:161`）返回
   `QBoardTop(2979) − (mirror?9:0) − RankInRow(slot)`，整段 **2962..2979**（`BoardLayout.cs:142-145`）。
   落位时 `BattleDriver.SyncBoard` 调 `v.SetQueue(layout.QueueOf(s))`（`BattleDriver.cs:13516`）。
3. 绿光建出来时 `_queue` 还是默认 `QCard = 3000`，而 `BuildFxMaterial` 把 `m.renderQueue` 设成 3000
   （`CardView.cs:4325`）⇒ 建层那一次 `StampQueue(prA)`（`:4425`）是 no-op；随后 `SyncBoard` 的
   `SetQueue(2962..2979)` 才真正把它盖成**那一格的号**。
4. 这与 A1674 自己的断言一致、且是**有意的**：`CardFaceProbe.cs` 的 A1674 用例先 `v.SetQueue(2971)`、
   再断言粒子的 `renderQueue == 2971`（否则「卡重叠时绿光会盖到邻居卡上」）。

**该照哪个判据**

- A1674 的设计判据：原 prefab 的材质 `m_CustomRenderQueue = 3000`、`m_SortingFudge = −3 / −2` ⇒
  「绿光与卡面同队列、只靠负 fudge 排在后面」；我们引入「一张卡一段队列」后，卡间隔由队列段做，
  **卡内层序仍由距离 + fudge**（见 `CardView.cs:1087-1099` 与 `:4416-4424` 的注释）⇒ 绿光要**跟着这张卡的段号**。
- 本工程「一张卡一段队列」的机制正本：`CardView.SetQueue`（`CardView.cs:1079`，注释写明是唯一实现）。

**建议修法（只写建议；⛔ 不要回退 A1674）**

把绝对 3000 换成「**这张卡那一段**」，其余不变：

```csharp
Check(prA != null && prA.sharedMaterial != null
      && prA.sharedMaterial.shader != null
      && prA.sharedMaterial.shader.name == "Universal Render Pipeline/Particles/Unlit"
      && prA.sharedMaterial.renderQueue == probe.Queue,
      "★ 绿光材质 = 原版 `Circle_Hoop Additive`（shader = URP `Particles/Unlit`）"
    + $"—— 队列跟着这张卡那一段走（现读 {probe.Queue}；原 prefab 的 `m_CustomRenderQueue` 是 3000，"
    + "A1674 起按「一张卡一段队列」盖章，见 `CardView.SetQueue`）");
```

（`probe` 是 `CardView`，`Queue` 是 public，`CardView.cs:1066`。若想保留「原 prefab 是 3000」这一事实，
放在消息文案里即可，别再拿它当断言值。）

---

## 二、根因归并

| 组 | 条目 | 根因 | 性质 |
| --- | --- | --- | --- |
| A | #1 + #2 | 4 局实战检查的**前提靠 AI 轨迹**：登记发生在 `PlayAiTurn` 内部，而上轮补的「回合前补打 Drone」只可能在**登记之后的自己回合**生效；4 局在当前数据下太短（现读 4/4 局 `ctx.Turn=7` 结束），登记一旦落在最后一个自己回合就永远轮不到 | 夹具（δ）；引擎无缺陷 |
| B | #3 | 数据侧按**卡面**给 AM15 补了 `Talent: Cadian Honour`，旧的「原始 desc 不许出现」前提过期；替身判别力本身没丢（`StripTalentSegments` 会剥掉天赋段） | 断言（α）；数据正确 |
| C | #4 | 本批 A1674（未提交）有意让粒子层跟随「一张卡一段队列」；BattleScene 的旧断言还在比原 prefab 的绝对 3000 | 回归（γ），但实现有意，改断言侧 |

**三组互不相关**，不会互相掩盖。

## 三、没查清 / 边界（+ 搜过什么）

1. **离线复刻与 Unity 的轨迹在当前池上不完全一致**：同一份当前池、同一套种子，离线复刻得到「登记 0」，
   而 Unity 实跑是「登记 2」。已核过不是这些原因：
   - `System.Random` 序列（`new Random(0).Next() = 1559595546`、`new Random(400).Next() = 1495440923`，与 .NET Framework 的经典值一致）；
   - 引擎/数据文件 mtime（全部早于 23:59 的日志）；
   - 池内 `keywords/desc` 的 null、牌组构造（离线复刻的督军+防御卡组合 g0..g3 与 Unity 日志逐局一致：Grukk+Extractor / Morgrim+Toxic / Grukk+Extractor / Morgrim+Toxic）；
   - `UnityEngine.Random` 在 `RuleEngine/` 里零代码引用（只有注释，已 grep `Random.Range/value/insideUnit`）。
   ⇒ 仍差的那一步**没查到**（可能来自 Unity `JsonUtility` 与 `System.Text.Json` 的某处解析差异，或测试进程里更早用例留下的全局态）。
   **但这不影响结论**：无论登记发生在哪一局，补打段的「只能在下一个自己回合生效」是代码结构决定的；
   离线复刻也已直接证明**同一段代码在旧池的 g2 局里能点亮机制**（`forced=[PLAY ok …]` → `firedYet=True`）。
2. **Unity 实跑的每局回合数读不到**（`ctx.Log` 只进内存 `ctx.Events`，这 3 条失败断言都不打 `LogTail`）。
   报告里的「4 局 T7 结束」来自离线复刻（当前池），**不是** Unity 的实读数；只能说它给出了一个高度可能的解释。
3. `CardView.cs` 在诊断期间（00:10）被别的代理改过，本报告引用的 A1674 行号是那一版；内容（`SetQueue` 扫
   `ParticleSystemRenderer`）未变。
4. 搜过但没找到的东西：`盯上` 除 `EffectResolver.cs:5785` 外无第二处；`部署触发段：` 除该处与测试外无第二处；
   `CardDatabase.Load()` 除注释外无引擎运行时调用点（排除「探针里池为空」那条假说）。
