# DIAG · RuleEngineTest 5 条红

> 只读诊断代理 `D-S23-1` · 2026-10-12 · 工程根 `D:/4`
> ⛔ 未跑 Unity · 未动 git 写操作 · 未改任何 `.cs` / `.json` / `资料/*.md` · 未改正本
> 用的工具：`ruleprobe.sh`（仓库内，只读）+ **仓库外**两棵对照树 `D:/tmp/ds23/{head,cur}/`
> （`head` = `git show HEAD:` 的 `Core/*.cs`；`cur` = 工作区现版；两边各自 `dotnet build`，
> 各自跑 `seg`/`card`/`scan`/`willrun`，**一行仓库文件都没碰**）
> 日志：`D:/4/_tmp_view/s23_ruleengine.log`（5 组 ✗ 在 **11546 / 16732 / 50865 / 52941 / 74094**）

---

## 一、一句话结论（5 条分别是哪类）

| # | 用例（现读行号） | 裁定 | 一句话 |
|---|---|---|---|
| **1** | `TestAdjacent` · `RuleEngineTest.cs:16307` | **(γ) 本批回归** | `W1` 的 8c-ter **硬编 `SegKind.Ok`**（`EffectText.cs:2724`）⇒ `FillAdjacentAnchors` ⑤ 那条「`When …` 里的相邻锚点定不出来 ⇒ 半懂」**被绕过**，整句冒充「解析干净」 |
| **2** | `TestAccursedHelbrute` · `RuleEngineTest.cs:8685` | **(α) 断言钉旧形状** | 它拿**裸解析**取 `op.Payload`；改前 `payload=[it as well]`，改后 `payload=[this troop gains it as well]` ⇒ `GivePayload.Parse` 返回 null。**用例自己下面那段注释已写「⛔ 别拿 `Parse(整条 desc)` 验」** |
| **3** | `TestPersistentEffects` · `RuleEngineTest.cs:7808` | **(α) 断言钉旧形状** | 那句话是**真卡** `GSC_Jammed_Communications`（tactic·破坏卡）。改前 `Unknown` 且 **0 条 op**（打出去=静默无操作），改后 `whenclause` 标记 op（只打日志）⇒ **新行为更好** |
| **4** | `TestListenerCardType` · `RuleEngineTest.cs:18071` | **(γ) 本批回归** | 8c-ter 从**卡类盲**的解析层调 `WhenEvents.ParseAll`，而 `ParseAll` **失败时会写全局桶**（`WhenEvent.cs:487`）⇒ 战术卡 `DA47` 的 `When played` 把 `played` 灌进报表桶 |
| **5** | `TestS14WhenBodyTargetIsSubjectless` · `RuleEngineTest_S14.cs:1647` | **(α) 断言钉旧状态** | 它断「那句被归进 `partial`」——那正是**改前**的形态；**断言文案自己写着**「如实记 … ⛔ 别拿它当判据」 |

**5 条全部来自 `W1` 的解析层改动**（`Core/EffectText.cs` + `Core/CardDef.cs`）。
实测把 `cur` 树里的 `EffectResolver.cs` / `RuleCore.cs` 换回 HEAD（= 只留 `W1` 那两个文件）后，
下面每一条读数**逐字不变** ⇒ **`W2` 那两笔（`EffectResolver` / `RuleCore`）与本批红无关**。

---

## 二、逐条

### 红 1 —— `★ When … adjacent … 不算解析干净（认不出的锚点不许冒充成功）`

- **断言本体**：`RuleEngineTest.cs:16302-16307`
  ```csharp
  const string when = "When an enemy attacks, deal 2 damage to adjacent enemies";
  CheckTrue(!EffectText.IsFullyParsed(when), "★ `When … adjacent …` **不算解析干净** …");
  ```
  ⭐ **用例自己的注释（`:16288-16290`）预先写明了期望**：
  「`When <事件>, … adjacent …` 那张（`Long Fang`）**等 `When` 短语落地后才会产生 op**，
  那时 `EffectText` 的判据 ⑤ 会把它标成认不出 —— 见 `FillAdjacentAnchors`。」

- **改前 / 改后（并排，`ruleprobe seg`，两棵树）**

| | `unparsed` | `partial` | op |
|---|---|---|---|
| **HEAD** | **= 整句** | 空 | **0 条 op** |
| **现版** | 空 | 空 | `verb=whenclause payload=[deal 2 damage to adjacent enemies] target=[]` |

  ⇒ `IsFullyParsed`：改前 `false`（✓ 期望）· 改后 **`true`**（✗ 实得）。

- **该句的【正确语义】**（判据 = 本仓自己的两条设计文字）
  - `EffectText.cs:1416-1429`（`FillAdjacentAnchors` 的 ⑤）：「**这句是 `When …` 触发的 ⇒ 锚点在事件参数里，
    文本层定不出来** → 标 `AdjacentFailed`、整句降级成**半懂**、卡面打 `*`」；
    ⑤·二：只有「事件**带宾语**」时才能定成 `AdjacentAnchor.TargetIfMeetsCriteria`（全池只 `Long Fang` 一张）。
  - `EffectText.cs:1447-1454`：降级的落地码 —— `if (adjFailed && segKind[i] == SegKind.Ok) { …Partial… }`。
  - `EffectText.cs:1481`：`t.AdjacentFailed = true;   // ⑤`

- **裁定：(γ) 本批引入的回归。**
  **机制**：8c-ter（`EffectText.cs:2723-2724`）产出的 op **`Target == null`**，而
  `FillAdjacentAnchors.FillOne` 里那一整段判据都包在 `if (t != null) { if (t.Adjacent && …) }` 之内
  （`EffectText.cs:1462-1490`）⇒ **`t == null` 直接跳过**，`adjFailed` 永远不置位，
  而 `r.Kind` 已被 8c-ter **硬编成 `Ok`** ⇒ 半懂信号彻底丢失。
  ⚠️ 这不是「报表面子问题」：`IsFullyParsed` 是「**认不出的锚点不许冒充成功**」这条红线的唯一落点，
  改前它是**唯一**把这类句子挡下来的尺子（⑦ 那条全池扫描也只盯 `Target.Raw` 前缀）。

- **该改哪一行、改成什么（⛔ 我只写不改）**
  - **推荐**：`Core/EffectText.cs` 的 `FillAdjacentAnchors`（现读 `:1462` 起 `FillOne` 内）——
    在 `if (t != null) { … }` **之外**补一支：`whenclause` 的标记 op **也要过 ⑤**：
    ```
    if (op.Target == null && op.Verb == "whenclause" && !string.IsNullOrEmpty(op.Payload)
        && !eventHasTarget && 该 Payload 含裸 adjacent)  ⇒  adjFailed = true; op…AdjacentFailed = true;
    ```
    （判据与 ⑤·二 共用 `eventHasTarget` 这一个入参 —— `FillAdjacentAnchors` 已经有它。）
  - **备选（不推荐）**：把 `:2724` 的 `r.Kind = SegKind.Ok` 改成走 `Finish(...)` ——
    但 `Finish` 是 `NeedsTarget(op) && op.Target == null ⇒ Partial`，`whenclause` 的默认 `NeedsTarget` 为 `true`
    ⇒ **会把全部 78 张无害地打成半懂**（含 `BL45` / `DA8` 这些没有相邻锚点的），爆炸半径太大。

- **没查清的部分**：`Long Fang`（`SW25`）在**事件层**解析正文时（`CardDef.cs:2440`
  `EffectText.Parse(body, …, evHasTarget)`）**⑤ 从不触发** —— 因为 ⑤ 的判据是
  `low.StartsWith("when ")`，而**正文不带 `when ` 前缀** ⇒ 走 ④ `Anchor = Self`。
  ⇒ 「事件层静默锚到自己」这一条**不是本批引入的**，但它和红 1 是**同一族**，我一并记在 §四·3，**没改也没验到结算层**。

---

### 红 2 —— `★ 载荷 it as well 认出来了（改之前五条判据一条都不命中 ⇒ 整条 op 空过）`

- **断言本体**：`RuleEngineTest.cs:8660-8685`（用例 `TestAccursedHelbrute`，`BL74`）
  ```csharp
  var ops = EffectText.Parse(desc, out _, out _);     // desc = 整条卡面
  foreach (var o in ops) if (!string.IsNullOrEmpty(o.Payload)) {
      var ps = GivePayload.Parse(o.Payload);
      if (ps != null) foreach (var p in ps) if (p.CopyEventPact) copy = true; }
  CheckTrue(copy, "★ 载荷 `it as well` 认出来了 …");
  ```

- **改前 / 改后（并排，`ruleprobe card "Accursed Helbrute"`，两棵树）**

| | op |
|---|---|
| **HEAD** | `verb=gain payload=[it as well] target=[when a friendly troop receives a dark pact, this troop]` |
| **现版** | `verb=whenclause payload=[this troop gains it as well] target=[]` |

  ⇒ 现版 `GivePayload.Parse("this troop gains it as well")` = **null** ⇒ `copy == false`。

- **该句的【正确语义】**（判据 = 用例**自己的注释**，`:8674-8680`）
  「⚠️ **别拿 `EffectText.Parse(整条 desc)` 的结果去验 `this troop` 的指代** —— 那是**裸解析**，
  `When <事件>, <正文>` 的切分发生在 `CardDef.AddWhenTrigger`，裸解析下整条事件从句会**漏进目标短语**
  （实测 raw `when a friendly troop receives a dark pact, this troop`）。引擎真正跑的是**事件层**那条路。」
  - ⭐ **改前那个 `payload=[it as well]` 本身就是这条注释在描述的那个 bug 的产物** ——
    用例拿它当输入，等于把「切错了」当成了「切对了」。
  - **实据（我实测的「对的那一份」）**：现版 `WhenTriggers[0]` =
    `ev=友方 darkpact troop · body=this troop gains it as well · ops=1`
    → `verb=gain payload=[it as well] target=[this troop] subj=True` ✅ **`CopyEventPact` 该命中**。

- **裁定：(α) 断言钉的是改动前的旧形状。**
  **该改哪一行、改成什么**：`RuleEngineTest.cs:8661` 那三行 —— 取 op 的来源从
  `EffectText.Parse(desc, out _, out _)` 换成**事件层**：
  ```csharp
  var card = new CardDef("FxHelbrute","FxHelbrute","unit",desc,"rare","BlackLegion",8,9,7,9,null);
  foreach (var o in card.WhenTriggers[0].Ops) { … GivePayload.Parse(o.Payload) … }
  ```
  （或退一步：对**正文**调 `EffectText.Parse("this troop gains it as well", …)` ——
  实测两棵树读数**逐字相同**：`gain` / `it as well` / `target=[this troop] Subj=1`。）
  ⛔ **别把 `copy` 的检查删掉** —— 它是这条 bug 的哨兵，只是**输入取错了层**。

- **没查清的部分**：无（两棵树读数都拿到了）。

---

### 红 3 —— `带条件从句的 X takes N damage 判不认识（不许把条件当主语）`

- **断言本体**：`RuleEngineTest.cs:7804-7808`
  ```csharp
  Check(EffectText.ParseSegment("When you play a Stratagem, your Warlord takes 1 damage").Kind,
        EffectText.SegKind.Unknown, "带条件从句的 `X takes N damage` **判不认识**（不许把条件当主语）");
  ```

- **改前 / 改后（并排，`ruleprobe seg`）**

| | `unparsed` | `partial` | op |
|---|---|---|---|
| **HEAD** | **= 整句** ⇒ `Kind = Unknown` | 空 | **0 条 op** |
| **现版** | 空 | 空 ⇒ `Kind = Ok` | `verb=whenclause payload=[your Warlord takes 1 damage] target=[]` |

- **该句的【正确语义】**
  - 🔴 **它是真卡**：`cards_engine.json` 命中 **1 张** ——
    `GSC_Jammed_Communications`（`Jammed Communications`，**tactic**，Genestealers 的**破坏卡**，
    卡面橙字 `Sabotage`）。卡图出处被引擎注释点名：`EffectText.cs:4158-4160`
    → `d:/2/Warpforge部队卡片/Genestealer Cult/6破坏卡/IMG_3817.jpg`。
  - **事件短语【认得出】**：`WhenEvent.cs:777-812` 的「打出族」有这一支
    （`Clean` 把 `you` 剥掉 ⇒ `play stratagem`，`Kind = Play`，`subjectIsYou` ⇒ `OwnerIs = RelFriendly`）。
    这正是 8c-ter 的第 ② 道闸放行它的原因。
  - **设计上的归属**：`EffectText.SplitHandTrapWhen`（`:4177`）的 doc **逐字点名这张卡**，
    并明确「**手牌陷阱**」这条路要「**吃 `CardDef` 而不是 `string`**」。

- **裁定：(α) 断言钉的是改动前的旧形状 —— 而且新行为【更好】。** 三条依据：
  1. **断言的理由已经作废**。它防的是「`(.+?)` 把条件从句吃成**主语** ⇒ 在**任何**时候都结算」。
     改后产出的是一条**标记 op**：`EffectResolver.cs:850` `"whenclause" → DoWhenClauseStub`，
     **只打一行日志**（`EffectText.cs:6955-6956` 明写）⇒ 没有任何「无条件结算」的路。
  2. **改前那条路才是坏的那条**：HEAD 对这句产出 **0 条 op** ⇒ `Parse(desc)` 空 ⇒
     `RuleCore.PlayTactic` 拿到空 op 表 = **扣了费什么都不发生**（本仓红线点名的最难查的一类）。
     改后至少有 `whenclause` 出声。
  3. **「判据必须在知道卡类的地方」这条原则没被破坏** —— 它由**另外三处**继续承载，
     而且**都是绿的**：`IsHandTrap(CardDef)`（`:4209`，`SplitHandTrapWhen` 非 null ⇒ **true**）·
     `RuleEngineTest.cs:12661`（反例：`HandledByOtherLayer` 对这张卡**返回 null**，继续绿）·
     `Coverage` 的 `if (IsHandTrap(c)) { Full++; FullAndMechanized++; continue; }`（`:1915`）。
  - **实测影响面 ≈ 0**（三条消费者逐个核过）：
    `EffectResolver.cs:1389`（`IsFullyParsed && !IsHandTrap` ⇒ 被 `IsHandTrap` 豁免，**行为不变**）·
    `DeckBuilder.cs:375`（`TacticPlayable` 里 `if (IsHandTrap(c)) return false;` 在前 ⇒ **不变**）·
    `BattleDriver.cs:12087` 的 `*`：`效果本版结算不了*` → `效果本版没作用*`（`MechanismFails` 走
    `WillRunOps` → `OpHasMechanism(whenclause)` 不认）⇒ **`*` 照旧在，只是换了措辞**。
- **该改哪一行、改成什么**：`RuleEngineTest.cs:7805-7808` —— 把期望从 `Unknown` 换成
  **钉 op 的形状**（保留原意「别把条件当主语」的可验证形式）：
  ```
  var r = EffectText.ParseSegment("When you play a Stratagem, your Warlord takes 1 damage");
  Check(r.Kind, EffectText.SegKind.Ok, "…现在由 `whenclause` 标记 op 收下（改前 = 0 条 op = 打出去静默无操作）");
  Check(r.Ops[0].Verb, "whenclause", "…是【标记 op】、不改结算");
  CheckTrue(r.Ops[0].Target == null, "★ 原意保留：条件从句**没有被当主语**（Target 为空 ⇒ 没有「任何时候都结算」）");
  ```
  ⚠️ **给调度台的一句**：若裁定「该句在解析层仍须判 `Unknown`」，那**在这个层做不到** ——
  「手牌陷阱形」与「单位卡的事件从句」在 `ParseSegment` 眼里**语法完全相同**（都只看一个 string），
  要区分就得看卡类，而 `SplitHandTrapWhen` 的 doc 正是为这件事才把判据搬到 `CardDef` 那一层。
- **没查清的部分**：`Jammed Communications` 作为**可打出的战术卡**打出去会发生什么（
  改前 0 op / 改后 `DoWhenClauseStub` 日志），我**没跑到真机**（⛔ 本代理不跑 Unity）。

---

### 红 4 —— `★ played 不在「认不出的短语」里`

- **断言本体**：`RuleEngineTest.cs:18049-18071`
  ```csharp
  WhenEvents.UnknownPhrases.Clear();
  var pool = CardDatabase.Load();     // 加载即重建监听器
  …
  CheckTrue(!WhenEvents.UnknownPhrases.Contains("played"), "★ `played` **不在「认不出的短语」里** …");
  ```

- **改前 / 改后（并排；我在两棵树上各加了一个只读 dump 模式，`Clear()` → `LoadPool()` → 逐条打印）**

| 树 | `UnknownPhrases` 内容 |
|---|---|
| **HEAD** | `''` · `deploy` · `friendly` · `friendly troop` · `friendly troop becomes` · `friendly troop or stratagem trigger codex icon codex`（**共 5 条，无 `played`**） |
| **现版** | 同上 **＋ `played`**（共 6 条） |
| **现版但 `EffectResolver.cs`/`RuleCore.cs` 换回 HEAD** | **同样是 6 条（含 `played`）** ⇒ **只由 `W1` 那两个文件造成** |

- **根因链（逐跳都有出处）**
  1. `CardDatabase.Load()` **不吃缓存**（`Data/CardDatabase.cs:54-63` 每次都 `Parse(asset.text)`）
     ⇒ **每一次 `Load()` 都会把全池 desc 重新解析一遍**。
  2. 解析每张卡 ⇒ `CardDef` ctor ⇒ `EffectText.Parse(desc)` ⇒ `Dispatch` 8c-ter
     （`EffectText.cs:2723`）对**任何**以 `When `/`Whenever ` 开头的句子都调 `TryWhenPrefixStub`（`:6986`）
     —— **⛔ 它拿不到卡类**（只有一个 `string`，与 `SplitHandTrapWhen` doc 说的问题是同一个）。
  3. `TryWhenPrefixStub` → `CardDef.TryParseWhenSentence`（`:2381`）→ `FinishWhenSentence`（`:2432`）
     → `WhenEvents.ParseAll(evPhrase)`。
  4. `WhenEvents.ParseAll` 认不出时**写全局桶**：`Core/WhenEvent.cs:487` `UnknownPhrases.Add(Clean(phrase));`
  5. 战术卡 `DA47 Reconnaissance Mission` 的 `When played, gain 3 Quest Points`
     ⇒ `ParseAll("played")` 认不出（`played` 省主语，`WhenEvent.cs:1190` 的注释明写它
     「**仍然收不到、而且【应该】收不到的只剩 `When played`**」）
     ⇒ **`played` 入桶**。
  **改前为什么不入桶**：`ParseAll` 只从**卡类感知**的注册层调（`CardDef.AddWhenTrigger`，
  只对 `CanListenForEvents` 的 unit/hero 走），非单位卡在 `:2570` 附近就被卡类卫挡住 ⇒ 战术卡那句
  **根本到不了 `ParseAll`**。

- **裁定：(γ) 本批引入的回归（无争议）。** 断言文案自己把这条判据说得很清楚：
  「**两处必须同一个判据**，各写一份迟早分家」+「它重新冒出来 = 报告那边**又按卡面文字不分卡类地数**了」。
  现在不是「报告分家」，而是**解析层自己把桶灌了** —— 比断言描述的病灶更靠上游。

- **该改哪一行、改成什么（⛔ 我只写不改）**
  **`Core/EffectText.cs` 的 `TryWhenPrefixStub`（`:6986`）—— 试探性调用必须【无副作用】**。
  两种改法，推荐第 ①：
  ① 给 `WhenEvents.ParseAll` 加一个**显式**的 `bool recordUnknown = true` 形参
     （`Core/WhenEvent.cs:483` 签名 + `:487` 处 `if (recordUnknown) UnknownPhrases.Add(…)`），
     `CardDef.TryParseWhenSentence` / `FinishWhenSentence` 逐层透传，`TryWhenPrefixStub` 传 `false`；
     ⚠️ 注册层（`AddWhenTrigger`）**保持 `true`** —— 那里才是「真的登记监听器」的地方。
  ② 退而求其次：`TryWhenPrefixStub` 里把 `WhenEvents.UnknownPhrases` 快照/还原
     （`SortedSet<string>` 逐条 `Clear`+`UnionWith`）—— 能修好，但是**隐式的**，⛔ 不推荐。
  - ⚠️ **同一个副作用还有第二个来源**：`W1` 自己的新断言
    `RuleEngineTest_S23_Parse.cs` ⑥（`When the moon rises, gain 1 Attack`，第 ② 道闸**故意要红**）
    也会把 `the moon rises` 写进桶。用改法 ① 一并消掉。

- **没查清的部分**：`UnknownPhrases` 是**跨用例的静态桶**，我只量了「Clear 后 Load 那一刻」的状态；
  全量测试里**还有没有别处**把别的短语灌进去（例如那些 `WhenEvents.Parse("…") == null` 的断言，
  实测 `WhenEvents.Parse` → `ParseAll` ⇒ **同样写桶**，全仓 20+ 处），我**没有逐个追**。

---

### 红 5 —— `（如实记：同一句经 EffectText.Parse(整条 desc) 会被归进 partial（半懂）…）`

- **用例定位**：日志 `_tmp_view/s23_ruleengine.log:74094` 的栈第二帧 =
  **`RuleEngineTest:TestS14WhenBodyTargetIsSubjectless () (at Assets/RuleEngine/Editor/RuleEngineTest_S14.cs:1647)`**
  ⇒ 用例方法 = `TestS14WhenBodyTargetIsSubjectless`，文件 `RuleEngineTest_S14.cs`。
- **断言本体**：`RuleEngineTest_S14.cs:1645-1648`（`c.Desc = "When Reanimated, gain Fast"`）
  ```csharp
  var whole = EffectText.Parse(c.Desc, out un, out pa);
  CheckTrue(pa != null && pa.Contains(c.Desc),
            "（如实记：同一句经 `EffectText.Parse(整条 desc)` 会被归进 **`partial`（半懂）** —— …）");
  ```

- **改前 / 改后（并排，`ruleprobe seg "When Reanimated, gain Fast"`）**

| | `partial` | op |
|---|---|---|
| **HEAD** | **= 整句** ⇒ `pa.Contains(desc) == true` ✓ | `verb=gain payload=[fast] target=[]` |
| **现版** | 空 ⇒ `false` ✗ | `verb=whenclause payload=[gain Fast] target=[]` |

  ⚠️ **紧挨着的下一格没红**（`CheckTrue(whole.Count == 0 || whole[0].Target == null, …)`）——
  现版 `whole[0].Target` 仍是 `null` ⇒ 那一格照旧绿。实测确认。

- **该句的【正确语义】**
  - 用例 doc（`:1610-1630`）已经把话说尽：「🔴 **⛔ 别拿 `EffectText.Parse(整条 desc)` 的结果去断** ——
    那正是今天这条**分叉** …… 两条路的 op **形状不同** …… 拿 `Parse` 那一路去断会得到一个**恰好相反**的答案」。
  - 权威 = **事件层**那一路（`CardDef.WhenTriggers[0].Ops[0]`）：`gain` / `fast` / `Target.Subjectless == true`
    —— **那一格本批之后照旧绿**（它才是这条的判别式）。
- **裁定：(α) 断言钉的是改动前的旧状态。**
  **该改哪一行、改成什么**：`RuleEngineTest_S14.cs:1647-1648` 这一格
  —— 断言文案**自称**「如实记 …… ⛔ 别拿它当判据」⇒ **直接删掉这一格**，
  或按新事实改写（`pa.Count == 0` ＋ `whole.Count == 1 && whole[0].Verb == "whenclause"`
  ＋ `whole[0].Target == null` —— 最后那半句就是把「两条路形状不同」这个原意**钉在 op 上**而不是钉在 `partial` 上）。
- **没查清的部分**：无。

---

## 三、有没有别的既有断言也受本批影响（**主动扫过**）

**扫描方法**：`grep -n "IsFullyParsed\|SegKind\|out un\|out pa\|WhenTriggers\.Count\|UnknownPhrases\|whenclause\|HandledByOtherLayer\|Coverage(" RuleEngine/Editor/RuleEngineTest*.cs`
（命中：`RuleEngineTest.cs` 179 · `RuleEngineTest_S14.cs` 17 · `RuleEngineTest_Diag.cs` 6 · `RuleEngineTest_S23_Parse.cs` 24 ·
其余 3 个文件 0）＋ **两棵树全池实测**（`scan` / `willrun` 逐行 diff）。

### 3·1 实测到的读数变化（**没有断言钉它们 ⇒ 不红 ⇒ 这是「下次会咬人」的一类**）

| 读数 | HEAD | 现版 |
|---|---|---|
| `scan` 的 `unparsed` 非空 | 176 行 | **129 行** |
| `scan` 的 `partial` 非空 | 10 行 | **1 行**（只剩 `BL25 Aspiring Champion` 的 `After receiving a Dark Pact, …` —— 那是 `TryParseAfterDarkPact`，**另一种形状**，不受本批影响） |
| `scan` 逐行 diff | —— | **78 行变**（与 `W1` 自报**完全一致**；其余 1042 行逐字节相同） |
| `willrun` ⑤「无头段」 | 53 条 / 53 张 | **108 条 / 108 张** |
| `willrun` ⑤「事件层（WhenTriggers）认领」 | 26 条 | **81 条** |
| `willrun` (c) 战术卡 | `收到 444 · 空表 1` | **`收到 445 · 空表 0`**（那 1 张 = `GSC_Jammed_Communications` ⇒ 红 3 的孪生读数） |
| `willrun`「⑤ 无消费点」「② 收漏了」「段级收漏/付费段」 | 0 / 0 / 0 | **0 / 0 / 0**（W1 这条自报**成立**，我复核过） |

### 3·2 会被碰到但**今天不红**的（逐条给判据）

| 断言 | 为什么绿 | 风险 |
|---|---|---|
| `RuleEngineTest.cs:12734-12739` 全池 `FullAndMechanized == Full`（四类） | `EffectText.Coverage`（`:1921-1933`）先按 `CardDef.HandledByOtherLayer` **把事件层认领的段剔掉** ⇒ 那 60 句**一直在 `Full` 里** | **本批不该动它**；若将来有人把 `HandledByOtherLayer` 那两行删掉，这条哨兵会**同时**把本批的 9 张 unit 变红（= 它会诚实地报出来，**是好事**） |
| `RuleEngineTest.cs:12716-12719` `covShell.Full == 1` / `covBare.Full == 1`（`When you gain [Quest Point], heal 2`，`DA15` 的形状） | 同上（过滤后两种实现都是 `Full`） | 🔴 **前提那颗断言 `:12707` `HandledByOtherLayer(shelled, shellSeg) != null` 是本批唯一可能直接碰到的** —— `W1` 只把 `TryParseWhenSentence` 从 `private` 抬到 `internal`、**没改体** ⇒ 实测仍绿。**下一个改这句话的人先看它** |
| `RuleEngineTest.cs:2693` `cov.SegUnknown == 0`（战术卡） | `Coverage` 对 `IsHandTrap(c)` **直接 `Full++` 并 `continue`**（`:1915`） | 本批**只会让它更松**（不会变红） |
| `RuleEngineTest.cs:12661` 反例 `HandledByOtherLayer(Jammed Communications, …) == null` | 未改动 | ✅ **它是「层归属」这条判据的哨兵，本批之后仍然立着** —— 红 3 的裁定**依赖它** |
| `RuleEngineTest.cs:911 / 12002 / 12287 / 14911 / 14978 / 27486`（`IsFullyParsed` **正向**断言） | 那些句子**都不以 `When ` 开头** | 无 |
| `RuleEngineTest.cs:16342`（`!IsFullyParsed("Adjacent units have Armour 1")`）· `:16352`（真卡 `Makari the Grot` / `Baneblade Tank` / `Honour Guard` 的 `!IsFullyParsed(c.Desc)`） | 光环句**不以 `When ` 开头** | 🔴 **「下次会咬人」第一名**：把 8c-ter 往前挪、或让它覆盖不带 `When` 前缀的形状 ⇒ 这三格**必红**（而它挡的正是「光环当成一次性 op」那条静默错打） |
| `RuleEngineTest.cs:12855-12883`（`ReportWhenCoverage`：`Clear()` → `Load()` → 只对 `CanListenForEvents` 的卡调 `Parse`） | **它自己不过滤不了 `Load()` 的灌桶** | 🔴 **它的「认不出的短语」一栏现在会多出 `played`**（口径错，正是红 4 断言 ② 要挡的）—— **没有断言钉这一栏**，而它会把下一个人引到「给 `played` 加解析分支」那条**断言文案点名过的错路**上。**修红 4 会一并修好它** |
| `_tmp_view/when_unparsed.md`（`RuleEngineTest.cs:12909` 写） | 只按 `CanListenForEvents` 的卡扫 | 内容不变 |
| `_tmp_view/unit_desc_unparsed.txt`（`:16486` 写） | 用 **raw** 口径 | 🔴 **内容会变**（`partial` 一栏 10 行 → 1 行）—— 没有断言钉行数 |

### 3·3 明说：**除这 5 条以外，我没有找到第 6 条会红的既有断言** —— 判据是「全池 `scan` 78 行 diff 逐行读完 + 上面 7 类断言逐条核过」。

---

## 四、顺手发现（只报不改）

1. 🔴 **`WhenEvents.Parse` 也写桶，而全仓有 20+ 处断言直接调它**。
   `Core/WhenEvent.cs:456-460`：`Parse(phrase)` 只是 `ParseAll(phrase)` 取第一条
   ⇒ 「认不出时写 `UnknownPhrases`」这个副作用**每个调用点都有**。
   实测命中处包括 `RuleEngineTest.cs:16563 / 17229 / 17269 / 17443 / 17912 / 17917 / 17931 …`
   （好几处断的就是 `== null`）。⇒「把试探性调用改成不写桶」（红 4 的修法 ①）**会一并把这些消掉**，
   但那是一个**跨文件的形参透传**，请调度台决定要不要一次做完。

2. ⚠️ **红 3 的卡面 `*` 措辞会变**：`BattleDriver.cs:12087-12096` ——
   `GSC_Jammed_Communications` 从 `效果本版结算不了*` 变成 `效果本版没作用*`（`MechanismFails` 走
   `WillRunOps` → `OpHasMechanism("whenclause")` 不认）。**`*` 照旧在**，只是换了措辞 ⇒
   卡面观感不变。⛔ **没有任何断言钉这两个字串**（全仓 grep 只有 `BattleDriver.cs:12087/12095` 两个定义点）。

3. 🔴 **红 1 的孪生问题（**不是本批引入**）**：`CardDef.FinishWhenSentence`（`Core/CardDef.cs:2440`）把**正文**交给
   `EffectText.Parse(body, …, evHasTarget)`，而 `FillAdjacentAnchors` 的 ⑤ 判据是
   `low.StartsWith("when ")`（`EffectText.cs:1478`）—— **正文不带 `when ` 前缀** ⇒ ⑤ **在事件层从不触发**，
   一律落到 ④ `Anchor = Self`（「相邻**我自己**」）。
   ⇒ 也就是说，`When <事件>, … adjacent …` 在**解析层**会被 ⑤ 正当地标成半懂（红 1 守的就是它），
   而在**真正跑的那条路（事件层）**上是**静默锚到自己**。
   ⚠️ 我**只读到了这一段代码**，**没有**构造结算级反例去证明它是错的（那要跑 Unity，⛔ 本代理不跑）。
   **判据出处**：`EffectText.cs:1474-1487`（⑤ 与 ⑤·二 两支）＋ `Core/CardDef.cs:2434-2443`。

4. ⚠️ **红 1 与红 3 其实是「同一个决定的两面」，调度台该一起裁**：
   8c-ter **要不要**在**解析层**把一个 `When <事件>, <正文>` 句子声明成 `Ok`？
   · 说「要」⇒ 红 1 / 红 3 都按 (α) 改断言，但**必须**同时修红 1 §「该改哪一行」那一支
     （否则「相邻锚点认不出」这个信号在**任何**一层都不再出现）；
   · 说「不要」⇒ 8c-ter 得整条撤回 —— 但 `TryWhenSuffixStub`（8c-bis，**HEAD 就有**）是同一条纪律的
     另一种语序，撤一半会留下**同形不同判**（工程红线）。
   ⇒ 我倾向「要」，理由是**卡类盲的层不该做卡类相关的判断**，而那个判断已经由
   `HandledByOtherLayer` / `IsHandTrap` / `SplitHandTrapWhen` 三处承载且**本批之后全部照旧**（§三·2 逐条核过）。

5. ℹ️ **`W1` 自报的两条读数我复核过，都对**：`scan` 78 行 diff（我也得到 78）·
   `T:Raw=when` 13 → 0（`scan` 里读不到 `T:Raw=when`）· `partial` 10 → 1 · `unparsed` 176 → 129 ·
   `willrun` ④/⑤ 的 rc 计数仍 0。⇒ **`W1` 的「改对了什么」没错，错的是它没跑 Unity、看不见这 5 条。**

---

## 五、我没做到的部分（如实）

1. ⛔ **没跑 Unity**（本代理红线）⇒ 5 条红的「实得」全部来自**日志**，
   我的「改前/改后」全部来自**仓库外两棵对照树**（`head`/`cur`），**不是** Unity 里的读数。
   两棵树的 `Core/*.cs` 与仓库**逐字节同源**（`head` 由 `git show HEAD:` 逐文件导出）。
2. ⛔ **没验证任何结算层行为**（红 3 的「打出去会发生什么」、顺手发现 3 的「事件层静默锚到自己」）。
3. `Dispatch` 8c-ter 之前的 8c-bis（`TryWhenSuffixStub`）在 HEAD 就是**硬编 `Ok`** ——
   我**没有**去找「有没有卡走那条路、它的 `partial` 因此被遮掉」的实例（红 1 的同族）。
   **判据在哪找**：`scan` 的 `T:Raw` 列里找 `<效果> when <事件>` 形状，
   或 `grep` `TryWhenSuffix` 的调用点；`W1` 的报告 §二说全池该形状**已注册**（用 `WFPROBE_POOL` 量的五张降费卡）。
