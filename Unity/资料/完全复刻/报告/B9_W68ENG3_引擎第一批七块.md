# B9_W68ENG3 · 引擎真缺口第一批（七块）执行报告（2026-10-27）

> 代理：`B9_W68ENG3`（执行型）
> 判据：`资料/完全复刻/判据/引擎真缺口_施工判据.md` §二 G06/G08–G12/G16/G19/G37 + §三 第一批
> 范围：**只做** G08–G12（5 谓词）· G16（ta=230）· G37（Whispers 闸）· G19（队列查询口）· G06①（计数器字段）。
> ⛔ 未做：G34 / G28①（落点 `CardDef.cs` 被别的代理占用）；第二批及以后全部未动。

---

## 一、结论

**七块全部落地，纯增量、零行为改动。** 三条必跑验收全绿：

| 验收 | 结果 |
|---|---|
| `typecheck.sh`（`TMPDIR=/tmp/wf_w68`） | **运行时错误 0 / 编辑器错误 0 = 0/0** ✅ |
| `ruleprobe.sh check` | **rc=0** · 解析差异 0 行 · 哨兵失败 0 · 指纹 **`c9a64798…` 不变** ✅ |
| `ruleprobe.sh play check` | **rc=1**（仍是 `BL64` + `DA42` 两张·别人的）· **没有加第三张** ✅ |

- 账补丁已产：`资料/完全复刻/账/_parts/B9_W68ENG3.tsv`（10 行 · 11 列 · `merge_ledger_parts.py --dry --only B9_W68ENG3` 报「更新 10 · 新增 0 · 无变化 0」、无报错）。
- 新增 1 个 `partial` 文件 `BattleContextTurnCounter.cs`（G06① 字段落此，理由见下·零行号漂移）。

**行号漂移（供同步点 `--fix`）**：
- `BattleContext.cs`：**零漂移**（5 个谓词全加在 `MatchTypes` 尾部，账引用最大到 `:2177`，新代码从 `:2201` 起）✅
- `Enchantment.cs`：**零漂移**（就地改条件行 + 4 行 doc 等量替换，行数 1284→1284）✅
- `ActionQueue.cs`：**7 处漂移**（`ActionType` 字段插在 `QueuedAction` 里 ⇒ `ActionQueue` 类整体下移 ≈10 行；引用 `:185/:209/:230/:233/:246/:277/:298`）
- `AiTargets.cs`：**1 处漂移**（`case 230` 插在 `default:` 前 ⇒ 原 `:303` 注释下移 ≈22 行；但该行对应的账行落点本补丁已改，合并后 `--fix` 会重定位）

---

## 二、证据（文件:行号）

### G08–G12 五个 `MatchType` 谓词（`RuleEngine/Core/BattleContext.cs` · `MatchTypes` 类尾）
- `MatchTypes.IsAgainstAI`  → `RuleEngine/Core/BattleContext.cs:2201`
- `MatchTypes.IsAgainstHuman` → `:2223`
- `MatchTypes.IsAgainstOnlineBot` → `:2232`
- `MatchTypes.IsDuringBattle` → `:2265`
- `MatchTypes.IsReplayMatch` → `:2272`
- 原版判据：`decomp_full/BattleManager__IsAgainstAI.c`（43 行）· `…IsAgainstOnlineBot.c`（45 行）· `…IsDuringBattle.c`（19 行）· `…IsReplayMatch.c`（28 行）· `out_cpp2il_dev_ilspy/BattleManager.cs`（IsAgainstHuman 全文 2 行）。
- 真值集（照判据 §二 G08–G12，逐值还原）：
  - `IsAgainstAI` = `{20,50,80,100,120,130,140,180,190,210}`（白名单，10 值）
  - `IsAgainstOnlineBot` = `{10,20,90,110,120,150,170,180,200,210,211}` 且 `!isNetworkedGame`（11 值）
  - `IsReplayMatch` = `{140,160}` · `IsAgainstHuman = !IsAgainstAI`
  - `IsDuringBattle = !isOver && (includeMulligan || !mulliganOpen)`

### G16 `ta=230`（`RuleEngine/Data/AiTargets.cs`）
- `GetTargets` 的 `case 230:` → `RuleEngine/Data/AiTargets.cs:302`
- 原版判据：`decomp_full/BattleManager__GetHighestHealthWarlord.c`（全文）+ `decomp_full/AbilityLogic__GetTargets.c:809-825`（`iVar6 == 0xe6` 的消费支）+ `decomp_full/BattleManager__GetHero.c`。
- 玩家映射：`+0xc0` = Player 0（`GetHero(playerHeroY=true)`）· `+0xc8` = Player 1（判据 `资料/任务线/战斗线/任务/T05_逐卡演出与特效接线.md:170-171`：3 friendlyHero=`+0xc0` · 4 enemyHero=`+0xc8`）。

### G37 Whispers 闸（`RuleEngine/Core/Enchantment.cs`）
- `FilterByQuestCounter` 第一道闸 → `RuleEngine/Core/Enchantment.cs:515`（`e.Card.CardType != CardDef.CardTypeWhispers`）
- 常量 `CardDef.CardTypeWhispers = 40` → `RuleEngine/Core/CardDef.cs:6602`（`CardDef.CardType` 属性 `get`）
- 原版判据：`decomp_full/EntityScript__ReachedQuestCounter.c`（三条闸 `cardType==0x28 ∧ spellType==0x78 ∧ questTimes>1`）。

### G19 队列查询口（`RuleEngine/Core/ActionQueue.cs`）
- `QueuedAction.ActionType`（原版动作号，缺省 0）→ `RuleEngine/Core/ActionQueue.cs:174`
- `ActionQueue.Has(Predicate<QueuedAction>)` → `:341`
- 原版判据：`decomp_full/BattleManager__WaitingForBlockingAction.c`（Replay 早退；四道 `+0x350→+0x358→+0x368→+0x360`；`local_108[0]==0x1b`=27）。

### G06① 计数器字段（新 `partial` 文件）
- `BattleContext.ThisTurnActionCounter` → `RuleEngine/Core/BattleContextTurnCounter.cs:30`
- 原版判据：`decomp_full/BattleManager__RegisterPlayerAction.c:19-34`（`bm+0x3bc` 的 `op_Increment`）· `…RegisterPlayerActionCancelled.c:19-34`（`op_Decrement`）· 唯一读者 `decomp_full/EndTurnClick.c:135-152`。

---

## 三、逐块：施工步骤执行结果 / 验收口 / 是否纯增量

### G08–G12（✅ 全部实现）
- **执行结果**：5 个 `static` 纯函数照真值集写死（用 `MatchType` 枚举名，⛔ 魔数已避开）。`IsAgainstHuman` 走 `!IsAgainstAI`（未复制白名单）。`IsAgainstOnlineBot` 取 `(t, isNetworkedGame)`：联机位作**形参**（引擎层无权威位，见「没查清」）。`IsDuringBattle` 取 `(isOver, mulliganOpen, includeMulligan)`。
- **验收口**：均为纯函数，可 19 值 × 谓词单测（真值表见 §二）。⚠️ `IsDuringBattle` 不吃 `MatchType`，故「19×5=95」实为 4 个 MatchType 谓词 ×19=76 + `IsDuringBattle` 独立几个用例（见「没查清」）。
- **纯增量**：✅ 新方法今天**0 消费者**（`grep` `RuleEngine/` 里这五个名字此前 0 命中）。

### G16 ta=230（✅ 实现，休眠）
- **执行结果**：见 §二。**关键订正**：施工判据 §二 G16 写「相等 ⇒ 空表」——**只读了 `GetHighestHealthWarlord` 本体**。实际 `GetTargets.c:811` 的 ta=230 消费支在它返回 null 时**取 `GetHero(bm,1)`（= `+0xc0` = Player 0）顶上** ⇒ 端到端**永不空表**。已按 `.c` 实现（`Player1.Health > Player0.Health ⇒ Player1，否则 Player0`），并在代码注释里写明「以 .c 为准（铁律 2）」。顺手删掉过期注释「230 完整选法未读完」（铁律 5）。
- **验收口**：池内 `ta==230` **0 行** ⇒ 今天不可观测，只能合成夹具（本批未加 `RuleEngineTest` 夹具，见下）。逻辑可用「两督军血量 5/7、7/5、5/5」三态手验：7/5(Player0=7>B=5)→Player0；5/7→Player1；5/5→Player0（相等取 P0，非空表）。
- **纯增量**：✅ 0 行 `ta=230` ⇒ 零行为变化。

### G37 Whispers 闸（✅ 实现，休眠）
- **执行结果**：`FilterByQuestCounter` 首道闸加 `e.Card.CardType != CardDef.CardTypeWhispers`（三条闸齐）。**就地订正**了过期 doc（原写「CardDef 没有 cardType 字段、只判 SpellType」—— `CardTypeWhispers` 早在 `CardDef.cs:6602`）。
- **验收口**：池内 `spellType==120` **0 张** ⇒ 休眠、不可观测；合成夹具（Whispers 附魔 vs 非 Whispers）应证「只有前者推进 questCounter」。⚠️ 附魔卡 `CardType` 数据可能缺 `-1` ⇒ 数据侧待核（见「没查清」）。
- **纯增量**：✅ 现池 0 张可达 ⇒ 零行为变化。

### G19 队列查询口（✅ 查询口 / ⏳ 消费口）
- **执行结果**：`QueuedAction.ActionType`（原版号、缺省 0 不标类型）+ `ActionQueue.Has(predicate)`（道 0→1→2→3、道内升序）。
- **订正（铁律 2/5）**：施工判据 §二 G19 写「**倒序**扫四条队列」，但 `.c` 每条道内是 `iVar4=0; … iVar4=iVar4+1`（**升序**）。因 `Has` 是**布尔存在性**查询、前向/后向对结果无可观测差别 ⇒ 按 `.c` 升序实现，并在 doc 里如实标注该差异。
- **验收口**：单测 —— 入队三条不同 `ActionType` ⇒ `Has(a=>a.ActionType==27)` 正确 true/false。
- **纯增量**：✅ `ActionType` 缺省 0、今天无入队点写它；`Has` 0 消费者 ⇒ 零行为变化。

### G06① 计数器字段（✅ 字段 / ⏳ 写点·消费端）
- **执行结果**：`BattleContext.ThisTurnActionCounter`（`int`，缺省 0）落**新 partial 文件** `BattleContextTurnCounter.cs`（**不往 `BattleContext.cs` 中间插**——同 `BattleContextRandom.cs` 那条例律，避免账行号漂移）。写点（++/--）、回合归零、消费端均属 W54/W57 文件，本批未接。
- **验收口**：字段今天**无写点** ⇒ 恒 0；`StateHash`/`Fingerprint` 用**显式字段**、不枚举 ⇒ 新字段**不改变任何指纹**（已由 `ruleprobe check/play check` 不变证实）。
- **纯增量**：✅ 恒 0、无人读。

---

## 四、没查清的部分（如实标注 · ⛔ 未猜）

1. **`IsAgainstOnlineBot` 的 `IsNetworkedGame()`**：原版从 `BattleManager` 现取；**我们引擎层今天没有这个权威位**（施工判据 §四·D 未闭合；唯一来源 = 表现层 `BattleDriver._net != null`）。⇒ 实现成**形参 `bool isNetworkedGame`**，由调用方传入，⛔ 未在引擎里凭单例/静态猜。调用方接线属 W57。
2. **`IsDuringBattle` 的 `UIstate == 0`（inactive，开打前）**：我们无 `UIstate` 枚举，只有 `IsOver` / `MulliganOpen` 两字段 ⇒ 该档与「正常战斗态」不可区分，本实现会对 `UIstate==0` 判 true（原版 false）。今天 0 消费者、开打前无人问 ⇒ 先按判据映射实现并标注；要用到 `0` 那档时须补一个 `started` 位。
3. **G16 的玩家映射假设**：`Players[0] ↔ +0xc0(isPlayer=真)` 取自 `T05` 文档 + `GetHero.c`/`case 120` 反证，**未用运行时实况直接坐实**。因 `ta=230` 池内 0 行 ⇒ 不可观测。若将来发现 `Players` 索引与 `+0xc0/+0xc8` 反向，需对调本 case 的 0/1。
4. **G16 是否走筛选尾**：本实现 `break` 落 `ApplyFilters/Trim`（与 120/130「取督军」支同口径）。原版 `GetTargets.c` 该支是否 early-return **未逐句跟到底**（0 行可达 ⇒ 无法观测）。若原版 early-return，我们的筛选可能摘掉这个督军目标 —— 待有可观测卡时再核。
5. **G37 的附魔卡 `CardType` 数据**：`CardDef.CardType` 缺省会 `-1`（未知）。若附魔卡的 `cardType` 列在 `cards_engine.json` 里没落，则第一道闸会让附魔走「不是 Whispers ⇒ 放行」——与「附魔按定义就是 Whispers」的旧说法冲突。⇒ **数据侧待核**（附魔卡 cardType 是否已喂）。
6. **未新增 `RuleEngineTest` 夹具**：`RuleEngine/Core/../Editor/RuleEngineTest.cs` **不在本代理白名单**（§13·3 单写手）⇒ 未改。建议同步点后在 `RuleEngineTest` 补：① G08–G12 真值表（§二那几张集 ×19）；② G16 三态；③ G19 三条 ActionType；④ G37 Whispers vs 非。
7. **三条已知未闭合判据**（`IsTheSameTurnPlayed` 体未读 · `Combo 590 vs 1130` · `CheckForSkullEarned.c:21` 的 `iVar2 != 10`）—— 属 G20/G24/G33，**不在本批**，本批未碰到。

---

## 五、改动清单

| 文件 | 改动 | 是否纯增量 |
|---|---|---|
| `RuleEngine/Core/BattleContext.cs` | `MatchTypes` 类尾加 5 谓词（`:2201/2223/2232/2265/2272`）+ 段头注释 | ✅ |
| `RuleEngine/Core/BattleContextTurnCounter.cs` | **新建** `partial BattleContext`，加 `int ThisTurnActionCounter`（`:30`） | ✅ |
| `RuleEngine/Data/AiTargets.cs` | `GetTargets` 加 `case 230`（`:302`）；订正 `default:` 过期注释 2 行 | ✅ |
| `RuleEngine/Core/Enchantment.cs` | `FilterByQuestCounter` 加 Whispers 首闸（`:515`）+ 就地订正过期 doc（4 行等量替换） | ✅ |
| `RuleEngine/Core/ActionQueue.cs` | `QueuedAction.ActionType`（`:174`）+ `ActionQueue.Has`（`:341`） | ✅ |
| `资料/完全复刻/账/_parts/B9_W68ENG3.tsv` | **新建**账补丁（10 行、11 列） | — |
| `资料/完全复刻/报告/B9_W68ENG3_引擎第一批七块.md` | 本报告 | — |

行尾：全部目标文件 LF、改后 `bareCR=0`（已验）；`Enchantment.cs` 行数 1284→1284（零漂移）。`⛔ 未用 sed -i`、`⛔ 未改 .unity/ProjectSettings/生成器/数据 JSON/正本/账主表/他人报告`。

---

## 六、顺手发现（铁律 5·b / 13·4⑧）

1. 🔴 **施工判据 §二 G16「相等 ⇒ 空表」不完整**：`GetHighestHealthWarlord` 相等确实返回 null，但 `GetTargets.c:811` 消费支在 null 时取 `GetHero(bm,1)`（Player 0）⇒ 端到端永不空表。已在代码注释 + 本报告订正，并据 `.c` 实现。
2. 🔴 **施工判据 §二 G19「倒序扫四条队列」与 `.c` 不符**：`.c` 道内是**升序**（`iVar4=0; iVar4++`）。布尔 `Has` 下不影响结果，已按 `.c` 实现并标注。
3. ⚠️ **`BattleContext.cs:1992` 附近那句注释「账里引用本文件的最大行号是 `:1868`」已过期**：现账已引用到 `BattleContext.cs:2177`（`MatchTypes.UsesDefencePool` 一族）。未改那句（不在本批落点、避免动别处），登记于此。
4. ⚠️ **`AiTargets.cs` 的 `default:` 注释「230 完整选法未读完」已订正**（本批 G16）；`reachedQuestCounter` 行的「CardDef 没有 Whispers」也随 G37 订正（账补丁备注已记）。
5. 💡 **建议**：`IsDuringBattle` / `IsAgainstOnlineBot` 一旦有消费者，需（a）给 `BattleContext` 补联机权威位（§四·D）与 `started` 位；（b）把 `BattleDriver._net` / `Ctx.IsOver` / `Ctx.MulliganOpen` 接到这两个谓词（属 W57）。

---

### 交接给下一个代理（若续）
- 本批 5 个谓词 + G16 + G37 + G19 查询口 + G06① 字段**已全部落地**，三条必跑全绿。
- 待办（非本批）：① 在 `RuleEngineTest.cs` 补单测（见 §四·6）；② G16 的 early-return 语义如有可观测卡再核；③ G37 附魔卡 cardType 数据侧核；④ D 档联机位/started 位、消费端接线属 W54/W57。
- 账补丁未合并：`_parts/B9_W68ENG3.tsv`（同步点跑 `merge_ledger_parts.py --only B9_W68ENG3` 再 `check_ledger_points --all-A --fix` 重定位 ActionQueue/AiTargets 的漂移）。
