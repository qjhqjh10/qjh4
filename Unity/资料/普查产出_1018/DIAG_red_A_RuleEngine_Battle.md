# DIAG · 批次 A —— `RuleEngineTest` 5 红 ＋ `BattleScene` 7 红（只读分诊）

> 只读诊断（2026-10-18，批次 A）。⛔ 本报告没有改任何代码 / 数据 / 正本；没跑 Unity、没跑 C# 编译。
> 基线：HEAD = **`007ace1`**（工作区有 2 个场景 + 1 个 `.meta` 未提交，与本次红无关）。
> 日志：`d:/4/_tmp_view/ruleengine.log`（22:22，`RE === 结束：5479/5484 通过，**5 条失败** ❌ ===`，`:86493`）
> · `d:/4/_tmp_view/battle.log`（22:23，`BT === 合计：3271 通过 / 7 失败 ===`，`:298643`）。
> 上一轮全绿对照：`d:/4/_tmp_view/battle_run3.log`（2026-10-18 **13:33**，`3274 / 0`）· `d:/4/_tmp_view/rule_run2.log`（2026-10-18 **13:18**，部署触发段**跑了 5 次**）。
> 两次全绿 → 本次全红的窗口里，只落了两个提交：**`4995622`（20:10，第五十三会话）** 与 **`579bcad`（21:04，第五十三会话续 · 卡名改官方）**。
> ⚠️ 日志里这 5 条 RuleEngine 红**各出现了两次**（`:53169/:86494` 等）—— 同一进程里跑了两次、不是 10 条不同的红；最终计数以 `:86493` 的 **5** 为准。

## 〇、总览

| # | 宿主:行 | 断言原话（短） | 判定 | 一句话根因 |
|---|---|---|---|---|
| 1 | `RuleEngineTest.cs:8603`（日志 `:53169`/`:86494`） | 整局里部署触发段真的跑过 —— True/False | **(δ) 夹具前提不成立** | 卡名改官方（GOF12 `Ork Spanner`→`Spanner`）改了 Goff 对手的行为 ⇒ 4 局种子轨迹变了，注册后不再部署 Drone；机制本身由同测试 ① 段直接夹具证明是好的 |
| 2 | `RuleEngineTest.cs:8604`（`:53184`/`:86495`） | 整局里真的筛中了 Drone —— True/False | **(δ) 夹具前提不成立** | 同 #1（同一条 `fired`/`granted` 计数的下游） |
| 3 | `RuleEngineTest.cs:19545` 断言（`:56167`/`:86498`） | 每张有中文名的卡，用中文名都查得回某一张 —— 查不回来 1 张 | **(α) 断言过期** | `AM24 Bullgryn Bone'ead` 的 `nameZh` **是英文**（官方表只有英文列，生成器按「保持英文原文」写入）；断言把「NameZh 非空」当成「有中文名」 |
| 4 | `RuleEngineTest.cs:19334`（`:55962`/`:86496`） | 被点到的卡数 —— 期望 66，实得 67 | **(α) 断言过期** | GOF12 改名后，`GOF15 Burna Boy` 的 `… a Spanner …` **第一次真的点到那张卡** ⇒ +1 张 |
| 5 | `RuleEngineTest.cs:19341`（`:55976`/`:86497`） | 全池「被点名的卡」总处数 —— 期望 127，实得 128 | **(α) 断言过期** | 同 #4（同一次命中的两个计数） |
| 6 | `BattleScene.cs:452` 助手（日志 `:156813`） | `TitleText_Me` 折行 = 0（实得 1） | **(γ) 本批回归** | `Label.ReplayFitForFace`（`4995622` 的 `N55`）在**文本翻面**时重放 `SetAutoFitBox` ⇒ 内部 `SetWrapWidth` 把折行无条件开回 `Normal(1)`，调用方先前的 `SetWrappingMode(0)` 被覆盖 |
| 7 | 同上（`:156943`） | `TitleText_Foe` 折行 = 0（实得 1） | **(γ) 本批回归** | 同 #6 |
| 8 | 同上（`:157096`） | `EnemyPlateText` 折行 = 0（实得 1） | **(γ) 本批回归** | 同 #6 |
| 9 | 同上（`:157226`） | `PlayerPlateText` 折行 = 0（实得 1） | **(γ) 本批回归** | 同 #6 |
| 10 | `BattleScene.cs:23510`（`:295881`） | A1192 `NameHolder/Name Text` 折行 = 0（实得 1） | **(γ) 本批回归** | 同 #6（`AlliancePanelWindow.Txt` 建完字后 `SetWrapping(false)`，之后 `Apply(...)` 灌中文名时翻面重放） |
| 11 | 同上（`:296001`） | A1192 `TitleHolder/Title Text` 折行 = 0（实得 1） | **(γ) 本批回归** | 同 #10 |
| 12 | 同上（`:296121`） | A1192 `Alliance/Alliance Name` 折行 = 0（实得 1） | **(γ) 本批回归** | 同 #10 |

**归并成两组**：**#1–#5 = 2026-10-18「卡名改官方」的数据后果**（两组：GOF12 改名同时解释了 #1/#2 的轨迹变化与 #4/#5 的 +1）；
**#6–#12 = `N55`（`4995622`）的 `Label` 翻面重放漏还原折行模式**（7 条一个根因）。**没有一条是引擎实现缺陷。**

---

## 一、逐条

### 1) / 2) 部署触发段从未被消费 / 从未筛中 Drone —— **(δ) 夹具前提不成立**

- **断言**：`RuleEngineTest.cs:8603` `CheckTrue(fired > 0, "整局里**部署触发段真的跑过**…")`；`:8604` `CheckTrue(granted > 0, "整局里**真的筛中了 Drone**…")`。
- **实得（日志原话，`ruleengine.log:53141`）**：
  `实战：4 局里 \`Experimental Drones\` 被登记 2 次 · 部署触发段跑了 0 次 · 筛中 Drone 0 次`
- **不是实现缺陷的硬证据**：同一条测试的 ① 段（`RuleEngineTest.cs:8495-8508`）用**直接夹具**验了同一条机制：
  `FixtureDrone` 部署 ⇒ 常驻效果**真的把护盾加上了**、Vehicle 不给、早先在场的不补 —— 这些断言**全绿**（不在红名单里）。
  ⇒ `ResolveDeploy`（`EffectResolver.cs:5752`）这条链本身是通的；红的是「4 局实战里恰好没再部署 Drone」。
- **为什么轨迹变了**（本报告的核心证据）：
  1. 上一轮绿（`_tmp_view/rule_run2.log`，10-18 13:18）同一条日志是 **`被登记 4 次 · 部署触发段跑了 5 次 · 筛中 Drone 5 次`**。
  2. 13:18 之后引擎代码（`RuleCore`/`SimpleAI`/`EffectResolver`/`EffectText`）**一行没动**：
     `git diff --stat 0a276fb..HEAD -- Unity/MyGame/Assets/RuleEngine` 里只有 `CreatePool.cs`（仅注释）、
     `CardDatabase.cs`（缺 id 跳过，本次池内 1127/1127 有 id ⇒ 空转）、`Deck*`（卡组串迁移，本测试不走）、
     `cards_engine.json`（**只有 64 处 `name`/`nameZh`，其余字段逐字段现比 0 差异**）、`talent_cards.json`（新增 7 条 da75/SW1-3/UM100/UM87/UM88，**没有 TauEmpire / Goff**）。
  3. 唯一能改这局行为的是**改名**：`GOF12 Ork Spanner → Spanner`（官方名）。`GOF15 Burna Boy` 的原文
     `Rally: If you control a Spanner or Mekboy Gazmek, deal 2-3 damage to all enemies`
     ⇒ 条件走 `EffectResolver.cs:7547-7572`（`controlcount`）→ `UnitMatchesControlAlt`（`:8827-8832`）按
     **`u.Card.Name` 与短语全等（忽略大小写）**判。改名前卡名是 `Ork Spanner` ≠ `spanner` ⇒ **永远不成立**；
     改名后 **成立** ⇒ 对手（Goff，`StarterDeck("Goff")`，`RuleEngineTest.cs:8579`）的 Burna Boy 会真的对全体敌人放 2-3 伤
     ⇒ **同种子下的整局走向变了**。
- **该照哪个判据**：机制判据 = 同测试 ① 段 + 卡面原文（`d:/2/Warpforge部队卡片` 的 `Experimental Drones`）；
  夹具判据 = 本测试自己的注释 `:8548-8568`「换成全 Drone 牌组 ⇒ 登记之后任何一次部署都是 Drone，断言才有意义」——
  **它假设了「登记之后还会继续部署」，而这条假设挂在固定种子的轨迹上**（`:8551` 也自己写着「引擎是种子化的…不会 flaky」，那只在数据不动时成立）。
- **建议改法（只写建议，需跑一次验证）**：⚠️ 先别改断言语义。两条路：
  - **(a) 让「消费」不再依赖轨迹**：在 `for (g…)` 循环里（`:8584-8591` 的 AI 回合之间），如果
    `ctx.PersistentEffects` 里已有 owner=AI 的 `deploy` 效果、且 AI 手里有 Drone，就直接
    `RuleCore.PlayCard(ctx, owner, HandIdx(...), slot)` 打一张 Drone —— 让 `fired` 由夹具保证，而不是靠运气。
  - **(b) 只重挑种子**：`seed: 9000+g` / `new Random(400+g)`（`:8580/:8582`）换一组，直到 4 局里至少一局在登记后仍有 Drone 部署。
    代价：下次任何数据改动都可能再红一次 —— 所以推荐 (a)。
  - 两种都不该把断言删掉：它盯的是「登记了但从不消费」这条真缺陷形态。

### 3) 中文名索引「查不回来的 1 张」 —— **(α) 断言过期**

- **断言**：`RuleEngineTest.cs:19545-19547`（循环在 `:19533-19543`，过滤条件 `:19535` `if (string.IsNullOrEmpty(c.NameZh)) continue;`）。
- **实得**：`noKey == 1`。**哪一张（本次用 python 复算，1127 张逐张过 `NormCjk`）**：
  `AM24 Bullgryn Bone'ead`，其 `nameZh` = `"Bullgryn Bone'ead"`（**就是英文名**）。
- **为什么它是「有意的」**：
  - `gen_cards_engine.py:1624-1641`（`load_official_display_names`）写明：`AM24` 的官方条目是**裸字符串**（只有英文列，
    13lang 的 `zh` 列也是英文）⇒ 按「**保持英文原文**（⛔ 不自译）」处理，代码 `by_id[_id] = (en, zh or en)`。
  - 提交 `579bcad` 的提交说明原话：「**AM24 无官方中文保持英文**」。
  - 索引侧的判据也是对的：`CreatePool.BuildNameIndexFromNames`（`CreatePool.cs:741`）用 `HasCjk(nameZh)` 当闸 ——
    **没有汉字的 NameZh 本来就不该进中文索引**（`NormCjk` 那张表「键个个含汉字」是 `MentionedCards` 中文短路的依据，
    `CreatePool.cs:627-629`）。⇒ **实现正确、数据按已拍板口径正确，错的是断言的过滤谓词**。
- **该照哪个判据**：`CreatePool.cs:623-629`（`_nameIndexCjk` 的不变量 = 「只收 `NameZh` 里含汉字的卡」）
  ＋ `gen_cards_engine.py:1624-1641`（裸字符串分支）＋ `579bcad` 提交说明。
- **建议改法（只写建议）**：`RuleEngineTest.cs:19535` 那一行改成「非空 **且含汉字** 才进循环」，并把「有 NameZh 但无汉字」的卡单独计数、钉成 1（AM24）：
  ```csharp
  if (string.IsNullOrEmpty(c.NameZh)) continue;
  bool cjk = false;
  foreach (char ch in c.NameZh) if (ch >= '\u4e00' && ch <= '\u9fff') { cjk = true; break; }
  if (!cjk) { nonCjk++; continue; }          // AM24：官方表只有英文列（gen_cards_engine.py 的裸字符串分支）
  …
  Check(nonCjk, 1, "★ 有 `nameZh` 但**不含汉字**的卡 = 1 张（AM24 官方只有英文；⛔ 不是索引漏建）");
  ```
  ⛔ **不建议**改数据（把 AM24 的 `nameZh` 清空）—— 那会推翻 `579bcad` 已写进生成器的口径，且显示侧本来就靠 `NameZh` 为空回落英文，收益为零。

### 4) / 5) 被点到的卡数 66→67 · 总处数 127→128 —— **(α) 断言过期**

- **断言**：`RuleEngineTest.cs:19334` `Check(distinct.Count, 66, …)`；`:19341` `Check(mentions, 127, …)`。
- **实得**：`67` / `128`（日志 `:55962`/`:55976`）。
- **复算（本次做的，能逐位复现）**：把 `MentionedCards` 的英文那一趟按 `CreatePool.cs:859-943` 逐行移植成 python
  （`Norm` = 只留 a-z0-9 · `Singular` 去尾 s · `MaxNameWords=6` · `PickNamed` 跳过自己/同阵营优先 · `StripTalentSegments` + `TalentCutAt` 两种写法 · `AddMention` 按 Id 去重）：
  - HEAD（`007ace1` 的 `cards_engine.json`）：**mentions 128 / distinct 67** —— 与实得**逐位吻合**；
  - 上一版（`git show 4995622:…/cards_engine.json`）：**mentions 127 / distinct 66** —— 与旧期望**逐位吻合**。
  - 两版的差集：**`only in HEAD: ['GOF12']`，`only in PREV: []`** ⇒ **+1 处 / +1 张，全部来自 GOF12**。
- **根因**：`579bcad` 把 `GOF12 Ork Spanner` 改成官方名 **`Spanner`**；而 `GOF15 Burna Boy` 的效果原文里有
  `… If you control a Spanner or Mekboy Gazmek …`。`CreatePool.MatchCardName`（`CreatePool.cs:758`）按 `Norm` 全等查索引：
  改名前 `norm("Ork Spanner")="orkspanner"`，文本里的 `Spanner` 查不到；改名后 `"spanner"` **命中** ⇒
  **这是一处真实的「点名」**（卡面确实写了那个词、卡池里确实有那张卡）。⇒ 新值 67/128 是**对的**。
- **⚠️ 铁律 5：断言上方那段 2026-10-18 的注释把成因写错了，要一起改。**
  `RuleEngineTest.cs:19316-19321` 写「两个期望值各 −1（68→67 · 129→128）… 成因 = `S13` 把 `UM84 Chaplain Cassius` 的
  `[Talent]:` 改成裸写 `Talent:`」——**两处不成立**：
  1. `S13` 那次改动在 **`41fed28`**，**早于** `4995622`（上一版数据里 `UM100` 已经是裸 `Talent:`，本次 python 亲读）⇒
     它**已经算进 66/127 那个基线**（`:19323-19331` 那段 `68 − 1(S13) − 1(②·b) = 66` 正是这么算的），再减一次 = 双重扣减；
  2. 注释里的 `68 / 129` 不是历史值，而是**「把裸写天赋那半段停掉」的 HEAD 读数**（本次复算：HEAD 停掉裸写 = **129 / 68**）。
  ⇒ 真实成因 = **GOF12 改名**，不是 Chaplain Cassius。
- **该照哪个判据**：判据 = 「卡面写了一个**池里存在的卡名**就算一处点名」（正本 §9·3，`CreatePool.cs:791-811`）＋ 本次复算的差集。
- **建议改法（只写建议）**：
  - `:19334` `66` → **`67`**；`:19341` `127` → **`128`**（两处文案里的「2026-10-15 起 64→66」补一句
    「2026-10-18 再 +1：GOF12 `Ork Spanner`→`Spanner`，`GOF15 Burna Boy` 原文里的 `a Spanner` 首次命中」）。
  - `:19316-19321` 的成因段改写成 GOF12；删掉「S13 −1」那句（S13 已在 66/127 基线里）。

### 6) – 12) 7 颗字「折行 = 原版 0（实得 1）」 —— **(γ) 本批回归（一个根因）**

- **断言**：
  - `BattleScene.cs:452-454` 的 `CheckHudBox` 助手（`Check(lb.WrappingMode == wrap, …)`）；
    调用点 `:15438`（`TitleText_Me`）、`:15440`（`TitleText_Foe`）、`:15468`（`EnemyPlateText`）、`:15470`（`PlayerPlateText`），期望 0。
  - `BattleScene.cs:23510-23514`（A1192 七颗字那条 `Check(lb1192.WrappingMode == 0, …)`）。
- **实得**：7 颗全部 `1`（`battle.log:156813/156943/157096/157226/295881/296001/296121`）。
- **是不是本批**：**是**。上一轮全绿 `_tmp_view/battle_run3.log`（10-18 **13:33**，`3274 / 0`）；
  之后只落了 `4995622` / `579bcad`；本组红的引入点就在 **`4995622`**（`git log -S"ReplayFitForFace" -- Battle/Label.cs` = 只有它）。
- **根因链（逐行）**：
  1. `Label.SetTextTmp`（`Battle/Label.cs:1413-1421`）新增：
     `if (_fitValid && !_fitReplaying && _fitFace != ActualFace) ReplayFitForFace();`（`:1420`）；
  2. `ReplayFitForFace`（`:1224-1232`）用**记下的四格实参**重跑 `SetAutoFitBox(...)`；
  3. `SetAutoFitBox` → `SetWrapWidth` → `TmpFont.SetWrapWidthRect` **无条件**把 `textWrappingMode` 写成 `Normal(1)`
     （`Label.cs:672-675` 的 doc 原话：「`SetAutoFitBox` 内部会调 `SetWrapWidth`，而那个**无条件**把 `textWrappingMode` 设成 `Normal`」）；
  4. 调用方当初在 `SetAutoFitBox` **之后**已经还原过：HUD 在 `BattleDriver.cs:16958-16961`（`l.SetWrappingMode(wrapMode)`），
     联盟窗在 `AlliancePanelWindow.cs:740` → `:748`（`lb.SetWrapping(false)`）；
     但**文本是后来才灌的**（`BattleDriver.cs:17249` `_myText.SetText(ProfileName…)`；`AlliancePanelWindow.cs:610-617` `Apply(...)` 里 `SetText`）——
     灌中文时 `ActualFace` 从 `LatinUi` 翻成 `Cjk` ⇒ 触发第 1 步 ⇒ 折行被开回 1，**没人再关**。
  5. **为什么恰好是这 7 颗**：只有「**建的时候是拉丁面、文本留空、之后灌汉字**」的标签才会翻面。
     这 4 颗 HUD 字的 `Hud(...)` 都显式传了 `face: TmpFont.Face.LatinUi` 且初始 `""`（`BattleDriver.cs:14502/14515/15563/15566`）；
     `ActualFace`（`Label.cs:939-942`）= `_face != Cjk && HasCjk(_text) ? Cjk : _face`。
     对照：`MatchSkullsScore` / `*FaithText` / `*SpiritStoneText` 期望折行 = **1**，就算被重放成 1 也不红；A1192 的
     `PlayerLabel` / `TitleLabel` 等是**建的时候就有文本**、之后不再 `SetText` ⇒ 不翻面。
     A1192 那 3 颗（`Name Text` / `Title Text` / `Alliance Name`）同样是 `face: TmpFont.Face.LatinUi` + 空串，测试灌的是 `"测试名"/"某称号"/"某联盟"`（`BattleScene.cs:23363/23396`）。
- **该照哪个判据**：原版 prefab 的 `m_TextWrappingMode`（`TitleText_Me/Foe`、两张名牌、A1192 那 7 颗全是 **0**；
  断言文案和 `资料/普查产出_第十一会话/PH_A1213Hud补框.md` §二·A 已经写明来源）⇒ **实现要还原 0，不是断言过期**。
- **建议改法（只写建议，改一处）**：**在 `Label.ReplayFitForFace` 里把折行模式一起保住** ——
  `SetAutoFitBox` 前记下当前 `WrappingMode`，重放后用 `SetWrappingMode(saved)` 还原：
  ```csharp
  void ReplayFitForFace()
  {
      float w = _fitW, h = _fitH, lo = _fitMinPx, hi = _fitMaxPx, bc = _fitBasePx;
      int wrap = WrappingMode;                       // 调用方此刻的档（0/1/2/3；-1 = 点阵后端）
      _fitValid = false; _fitReplaying = true;
      try { SetAutoFitBox(w, h, lo, hi, bc); }
      finally { _fitReplaying = false; }
      if (wrap >= 0) SetWrappingMode(wrap);          // 重放会经 SetWrapWidth 开回 Normal ⇒ 还原
  }
  ```
  （`SetWrappingMode` 自带「值没变就早退」+ `ForceRelayout()`，正好把版面推回去。）
  ⛔ **不建议**改 `SetAutoFitBox` 本身去「记住并保留折行」—— 那会改变所有依赖「`SetAutoFitBox` 之后折行=1」的既有调用点；
  ⛔ **也不建议**只在这 7 个调用点再补一句 `SetWrappingMode(0)` —— 文本是**运行期**灌的，补在建字那一刻之后无效
  （正是这次漏掉的那一跳）。

---

## 二、根因归并

| 根因 | 涉及条目 | 证据 |
|---|---|---|
| **A. 2026-10-18「卡名改官方」（`579bcad`）的连带** | **#1 #2 #4 #5** | 64 处改名里唯一有语义的一处 = `GOF12 Ork Spanner → Spanner`；它在**两处**同时生效：① `GOF15 Burna Boy` 的 `If you control a Spanner …` 现在 `UnitMatchesControlAlt` 能配上（`EffectResolver.cs:8843-8848`）⇒ 对手 AI 行为变了 ⇒ 4 局种子轨迹变化（#1/#2）；② 同一句 `a Spanner` 现在能命中卡名索引 ⇒ `MentionedCards` +1 处 / +1 张（#4/#5）。**python 复算：PREV 127/66 → HEAD 128/67，差集只有 `GOF12`。** |
| **B. 中文名索引的断言谓词与数据口径脱节** | **#3** | `AM24` 的 `nameZh` 是英文（`gen_cards_engine.py:1624-1641` 裸字符串分支 + `579bcad` 提交说明「AM24 无官方中文保持英文」）；索引侧 `HasCjk` 闸正确（`CreatePool.cs:741`），断言的 `string.IsNullOrEmpty(NameZh)` 把它当成「有中文名」。 |
| **C. `N55` 的翻面重放没保住折行模式（本批回归）** | **#6 #7 #8 #9 #10 #11 #12** | `Label.cs:1420` → `:1224-1232` → `SetAutoFitBox` → `SetWrapWidth`（无条件 `Normal`）；调用方的 `SetWrappingMode(0)` 发生在**灌文本之前**，之后没人再关。上一轮 13:33 全绿、本批 22:23 全红，`git log -S` 指向唯一提交 `4995622`。 |

**一句话**：**5 条 RuleEngine 红全是「已拍板的数据改动没跟断言」；7 条 Battle 红是本批 `Label` 新代码把折行开回去。**
0 条是「引擎实现错了」。

---

## 三、没查清的部分（如实写）

1. **#1/#2 的夹具修法没有实跑验证**（本代理只读、不跑 Unity、不编 C#）——
   建议 (a)「夹具自己补打一张 Drone」与 (b)「换种子」都只是方案；哪一组种子/哪种驱动能让 4 局重新覆盖到「登记后有 Drone 部署」，**没验证**。
2. **逐局事件看不到**：`ctx.Events` 只在内存里，测试只打印聚合数（`RuleEngineTest.cs:8600`）⇒
   「那 2 次登记发生在第几局、第几回合、当时场上是否已满」**没查到**。
3. **`CreatePool.cs:840-845` 那段注释的 `125 / 64` 与本次复算对不上**：
   我用同一套算法复算 HEAD = **128/67（裸写天赋半段 ON）**、停掉半段 = **129/68**；上一版 = **127/66 / 128/67**。
   那段说「ON=125/64、OFF=128/67」——**只有 OFF 那个数与上一版吻合**。它是哪一版数据/哪一种口径测的，**没查到**
   （可能与 `A1078` 落地时点的数据快照有关，未继续追）。
4. **其他宿主的同型风险没查**：本批只跑了 `RuleEngineTest` / `BattleScene`；`N55` 的翻面重放对
   `DeckScene` / `ShellScene` / `CollectionScene` 等宿主里「拉丁面 + 空串 + 后灌汉字 + 期望折行 0」的标签是否有同型红，**没查**（不在本批 12 条范围内）。
5. **#4/#5 的文案里「正本 §9·3 记的是 126」与实测 128 的关系没重新裁**（旧注释自己说「差得不是同一把尺子」）——
   本次只确认「+1 来自 GOF12、新值 67/128 与现算法一致」，**没有**去复核正本那个 126 的口径。

### 搜过/用过的判据（便于复查）

- 断言源码：`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`（`:8603/:8604` · `:19316-19341` · `:19534-19547`）·
  `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（`:436-455` 助手 · `:15438/:15440/:15468/:15470` · `:23510-23514`）。
- 实现：`RuleEngine/Core/CreatePool.cs`（`Norm` `:540` · `NormCjk` `:589` · `BuildNameIndexFromNames` `:723` · `MentionedCards` `:859` ·
  `StripTalentSegments` `:1014` · `TalentCutAt` `:1062` · `PickNamed` `:1074`）· `RuleEngine/Core/EffectResolver.cs`（`ResolveDeploy` `:5767` ·
  `controlcount` `:7548-7570` · `ParseControlAsk` `:8789` · `UnitMatchesControlAlt` `:8843`）·
  `CardPresentation/Battle/Label.cs`（`SetWrappingMode` `:699` · `SetAutoFitBox` 尾 `:1200-1222` · `ReplayFitForFace` `:1224` · `SetTextTmp` `:1413`）·
  `CardPresentation/Battle/BattleDriver.cs`（`Hud` `:16925-16965` · `_myText.SetText` `:17249`）· `Shell/AlliancePanelWindow.cs`（`Txt` `:710-758` · `Apply` `:608-617`）。
- 数据：`RuleEngine/Resources/cards_engine.json`（现读 `count=1127`）· `git show 4995622:…/cards_engine.json`（上一版）·
  `工具/gen_cards_engine.py:1615-1650`（官方显示名 + AM24 裸字符串分支）· `工具/gen_cards_engine.py:2544-2575`（改名最后一步）。
- git：`007ace1`（HEAD）· `4995622`（`N55` + 真机 7 轮）· `579bcad`（卡名改官方）· `41fed28`（S13 的 `Talent:` 订正，早于本批）。
- 日志：`_tmp_view/ruleengine.log`（`:86493` 结束行 · `:53141` 实战术 · `:56167` 中文名）·
  `_tmp_view/battle.log`（`:298643` 合计 · 7 条折行红）· `_tmp_view/rule_run2.log` / `_tmp_view/battle_run3.log`（上一轮绿）。
