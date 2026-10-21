# W59 · 引擎线 `T06` · P5 全池 sweep（1,127 张）

> 2026-10-21 · 执行代理（引擎线 `T06` P5）· **只读**：没跑 Unity、没改任何 `.cs`、没动 git / `Assets/**` / `资料/任务线/**` / 基线指纹。
> 工具：`工具/ruleprobe.sh willrun`（腿 A = 执行层 `EffectText.WillRunOps`；腿 B = 卡面 `desc` ∪ `keywords` 的 `Head: body` 段）。
> 中间产物（逐卡 TSV，本报告的大表就是它的转写）：`D:/4/Unity/_tmp_view/w59/willrun_1021.tsv`（1,128 行 = 表头 + 1,127）· 全量 stdout `willrun_1021.log`。

## 〇 一句话结论

**卡池 1127 张：解析层无缺口（`rc=0`）** —— ②「收漏了」0 张 · 段级收漏 0 / 付费段 0 · ⑤「无消费点」0 条 · ⑦⑧⑨ 三条判据自检各 0 条红；**「通」971 张**、**「本来没正文」92 张**、**「未归因（空表但正文由别的层消费）」64 张**，**partial / unparsed / 有未消费 op 三档均为 0**。

🔴 **但要分清**：本笔做的是**解析层 sweep**（卡面正文有没有被执行层收下来、收下来的有没有机制）。`T06` 台账里 W57 记的那条「P5 的 1,127 张**新分 vs 旧分**」是 **AI 算分级** sweep（需要 Data-inclusive harness），**仍没做**（见 §五·3）。

## 一 怎么跑（可直接粘贴）

```bash
cd /d/4
bash d:/4/Unity/工具/ruleprobe.sh willrun dump "D:/4/Unity/_tmp_view/w59/willrun_1021.tsv"   # 全池 sweep，≈3 秒；rc=0
bash d:/4/Unity/工具/ruleprobe.sh check                                                     # 旁证：解析器没被改坏；rc=0
```

- `ruleprobe.sh` 自己拿 `dotnet build` 的退出码（`A1466`）：**构建失败即停，不会拿旧 dll 出假绿**。本趟两行日志首部都是 `已成功生成。`。
- 判据是**摘要行 + 六项实况**，不是退出码（`ruleprobe.sh` 头第 5 行）。本趟实况见 §三。
- 本趟 `check` 现读：`=== 全池 1121 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 ===` · `哨兵合计：失败 0 条` · 输出指纹 `c9f6c642…`（与钉住一致）⇒ **rc=0**。

## 二 分档统计（池 1127 张 · 现读 `cards_engine.json.count`）

| 档 | 张数 | 判据（= TSV 列） | 计入 rc |
|---|---|---|---|
| **通** | **971** | `runOps>0` ∧ `noMech` 空 ∧ `orphans=0` ∧ `headlessNone=0` | ✅ |
| **本来没正文** | **92** | `runOps=0` ∧ `emptyKind=本来没正文` | 否（但必须出声，⛔ 不许并进「通」） |
| **未归因（别层消费）** | **64** | `runOps=0` ∧ `emptyKind=未归因` | 否（工具**不判**；本报告逐族补了消费者，见 §四） |
| **partial** | **0** | `runOps>0` 但 `noMech`/`orphans` 非空 | ✅ |
| **unparsed（收漏了）** | **0** | `emptyKind=收漏了` | ✅ |
| **有未消费 op** | **0** | `headlessNone>0` ∨ `orphans>0` | ✅ |
| 合计 | **1127** | = 池 | — |

> ⚠️ 老口径（`RuleEngineTest` 报表的形状：只对 `WillRunOps` **返回的** op 逐条问 `OpHasMechanism`）本趟仍是 **1127/1127 全通** —— 它把「一条 op 都没收到」也算通（`A1432` 的病灶）。⇒ **两行并排才是现场**：老口径 1127/1127 vs 新口径空表 **156** 张。

## 三 六项判据实况（**本趟现算**，⛔ 不是照抄判据式）

| 项 | 读数 | 结论 |
|---|---|---|
| ②「收漏了」 | **0 张** | ✅ |
| ④ 段级总账（只 unit/hero） | 卡面带正文段 **326** 条 · 已收到 **326** 条 · 收漏 **0** · 付费段 **0** · 同头两写 **0** | ✅ |
| ⑤「无头正文」 | **108 条**落在 **108 张卡**：事件层 81 · 回合起止 19 · 光环 7 · 降费 1 · 🔴 **无消费点 0** | ✅ |
| ⑦ ④ 档判据自检（`A1593`） | 红 **0** 条（A/B/C/D 全过） | ✅ |
| ⑧ 头→通道键近似自检（`A1598②`） | 红 **0** 条（池侧 9 处 = `ecstasy N`，基词只有 `ecstasy`） | ✅ |
| ⑨ 收集层 `Source` 自检（`A1694`） | 红 **0** 条（`SpiritOps` 28 张全小写；`A1439` 现场 16 对两路逐字不同） | ✅ |
| (a) `scan` 原始口径 | `unparsed` 非空 **51** 行 · `partial` 非空 **2** 行 | ⛔ 不是缺陷数（`A1433` ①） |
| (b) `EffectText.Coverage` | tactic 446 / unit 586 / hero 56 / defence 39，**部分 0 · 完全不懂 0** | ✅ |
| (d) 未实现关键词 | **0** 个 | ✅ |

## 四 按族归纳（156 张空表的着落）

### 4·1 「本来没正文」92 张

| 子族 | 张数 | 说明 |
|---|---|---|
| `desc` 与关键词都为空 | **6** | `ASH61` `BL2` `BL26` `GOF8` `TL61` `UM6` —— 裸单位卡，不是缺陷 |
| 整条 `desc`/`keywords` 全是裸关键词 | **79** | `Vanguard` 9 · `Flank` 4 · `Blast 2` 3 · `Remnant. Vanguard` 3 · `Flying. Flank` 2 … 全池级证据 = (d) 未实现关键词 0 |
| 裸关键词 + **`Companion N:`** | **4** | `TAU27` `TAU26` `TAU22` `TAU31` → `CardDef.HandledByOtherLayer`（`CardDef.cs:1231-1233`） |
| 裸关键词 + **静态改战斗规则**一句 | **3** | `SAU7`（ignore enemy Vanguard）`SAU27`（Melee = Health）`SAU33`（Warlord 攻击改打它）→ `CardDef.cs:1535/1559/1542` ④-ter |

> ⚠️ 后两格（7 张）**不是「没有正文」** —— 它们有散文句，只是主解析器解不出、由别的层认领。工具把「解不出 + 没进无头档」一并归 ①，见 §五·1。

### 4·2 「未归因」64 张 —— 逐族补消费者（判据 = 引擎代码读点，非推测）

| 族 | 张数 | 样例 | 消费者（判据） |
|---|---|---|---|
| **天赋层（具名 `Talent: X`）** | **42** | `ASH1` `BL34` `GSC3` `TAU1` … | `CardDef.cs:1520 TalentName` + `Data/CardDatabase.cs:288 talentNameIds`（186 键）+ `TalentCardData.cs:156` 按名查 id |
| **天赋层（`Talent: A random Black Legion Psychic Power`）** | **2** | `BL23` `BL36` | `RuleCore.cs:10344`「天赋名是池子 ⇒ 从池里抽一张」 |
| **回合起止 `At the end/start of … turn`** | **17** | `AM18` `GOF90` `SOR70` `TAU13` `TL37` … | `EffectResolver.ResolveAtTurn:6013`（棋盘那一趟 `:6070` 逐 clause `Parse`）；16 张的 clause body 用 `seg` 实测**零残渣**，`countRef=own／vehicle／all`、`condKind=anypraying` 都解析出来了 |
| **光环 `Your X cost 1 less`** | **3** | `BL27` `TAU20` `TAU32` | `AuraSpecs` 光环层（`willrun` ⑤ 认领格 = `光环（AuraSpecs）`） |
| **降费触发器 `Lower cost by 2 every time …`** | **1** | `TL70` | `CardDef.cs:3071 FireCostWhen`（调用于 `EffectResolver.cs:7284`） |
| **开局上手 `Start the game with … in hand`** | **1** | `SW1`（`BL36` 同族） | `CardDef.cs:1952/3024 StartWithInHand` |
| **静态规则（誓约加倍）** | **1** | `UM100` | `CardDef.cs:1597`（`Friendly Oath abilities apply an additional time`） |

> 张数按**主归因**计（一张卡可同时属两族，例 `SAU36` = 天赋 + 光环 + 回合起止）⇒ 合计 67 > 64 属正常重复。

### 4·3 108 张带「无头句」的卡

87 张是「通」（另有 op），21 张是「未归因」；**无消费点 0**。分布：事件层 WhenTriggers **81** · 回合起止 **19** · 光环 **7** · 降费 **1**。

## 五 建议新账（按族 · ⛔ 我没改引擎、没改正本，请主对话分流）

1. **[仪器口径] `willrun` ① 档把「有散文句但主解析器解不出、也没别层认领」的卡并进「本来没正文」。**
   今天 7 张（§4·1 后两格）都由 Companion / ④-ter 认领 ⇒ **无实际缺口**；但这条口径**结构上看不见**「有正文、没人认领、解析器也解不出」的**新**卡（`A1432` 同族的另一种空转形状）。建议：① 档再分一格「含非关键词散文」。
   样例 = `SAU7` / `SAU27` / `SAU33` / `TAU22` / `TAU26` / `TAU27` / `TAU31`；判据 = `WillRun.cs` ① 判据（`desc` 空 ∨ 整条裸关键词）。
2. **[静默失配隐患] `TalentCardData._nameMap` 缺 `pathofwarrior`。**
   `talentNameIds` 里三份 `Path of …` 只有 `pathofcommand` / `pathoftheseer` 两条裸键 + 三条 `zncsaimhannpathof*`，⇒ `ASH2 Path of the Warrior` 查 nameMap 落空、退回 `CreatePool.FindByName`（按**显示名**）。今天池里正好有一张同名天赋卡 ⇒ **零行为差异**；一旦改名 / 两阵营同名就静默失配。出处 `Core/TalentCardData.cs:118-160` · `cards_engine.json.talentNameIds`。样例 `ASH2`。
3. **[台账] `T06` 的「P5 1,127 张逐张新分 vs 旧分」= AI 算分级 sweep，本笔没做。**
   `ruleprobe` 只编 `RuleEngine/Core/*.cs`（`AiScoring`/`SimpleAI`/`AiTargets` 不在编译项）⇒ 仍需 Data-inclusive harness（W57 §四·2）。本笔覆盖的是**解析/正文接收/未消费 op**这一层。
4. **[记录过期 · 不是缺陷] W57 §五 记 `ruleprobe check` rc=1（4 解析差异 + 1 哨兵红）；现读 rc=0。**
   `解析差异 0 行 · 哨兵 0 条 · 输出指纹 c9f6c642…` ⇒ 基线/指纹已在后续会话重钉过，那行读数应就地订正（按铁律 5，⛔ 我没动 `资料/任务线/**`）。

## 六 没查清 / 顺手发现

1. **「消费者真的执行对了」属于结算层，本笔只证「有消费者 + body 解得开」。** 64 张未归因 / 108 张无头句我都按代码读点确认了消费链，并用 `seg` 实测了回合起止那 16 条 body；但**没有逐卡真打**（`play` 是另一条腿，`T06` 不含）。
2. **`TAU7` 的从句抽取没单独验**：它的 `desc` 是 `Flying. At the start of your turn, give Markerlight 1 …`（关键词与从句同一行），我只用 `seg` 验了 body 文本，没验 `AtTurnClauses` 对这张的切点。
3. **顺手发现（只报不改）**：`ruleprobe check` 的 `[Source 列]` 哨兵自己报了一条 ⚠️ 形状失真 —— `Source` 原文自带 `;` 被多切出 **6** 条假 op、截断块 **5** 条（真 op 数 1439 / `Ops()` 切出 1445）。它是 ⚠️ 不计 rc，但「假 op」会污染任何拿 op 数做分母的读数。

## 七 附：逐卡明细（1127 行 · 机器可读原件 = `_tmp_view/w59/willrun_1021.tsv`）

| 卡 id | 卡名 | 类型 | 档位 | 一句话原因 | 出处 |
|---|---|---|---|---|---|
| `ASH83` | Autarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH77` | Death Spinner Warp Spider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH63` | Dire Avenger Exarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH62` | Dire Avenger | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH80` | Farseer | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH22` | Fire Dragon | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH4` | Guardian Defender | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH69` | Howling Banshee Exarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH15` | Howling Banshee | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH79` | Orian Laratharjos | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH6` | Ranger | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH28` | Spiritseer Qelenaris | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH17` | Spiritseer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH5` | Storm Guardian | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH23` | Striking Scorpion Exarch | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH11` | Striking Scorpion | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH70` | Swooping Hawk Exarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH12` | Swooping Hawk | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH24` | Vengeful Wraithblade | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH7` | Veteran Guardian | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH8` | Warlock | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH78` | Warp Spider Exarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH13` | Warp Spider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH61` | Weapons Platform | unit | 本来没正文 | desc 与关键词都为空（裸单位卡） | `W59 tsv（desc/keywords 逐字）` |
| `ASH31` | Winged Autarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH19` | Witchblade Warlock | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `ASH14` | Wraithblade | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH20` | Wraithguard | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH75` | Yrlla the Huntress | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH34` | Avatar of Khaine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH46` | Aerial Dominion | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH84` | Ahnakh-Yth Shrine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH55` | Aspect Shrine | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH45` | Banshee Mask | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH47` | Bladestorm | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH48` | Cosmic Serpent | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH71` | Craftworld Convergence | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH42` | Dance of Death | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH39` | Deadly Ambush | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH64` | Devoted of Khaine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH49` | Eldritch Storm | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `dc7e575c0a328f446be1521c23ec850b` | Empyric Ambush | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH50` | Evasive Manoeuvre | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH85` | Fate Inescapable | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH36` | Fire and Fade | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `67adbc7f7bce2bd4c8f6937fab3134e5` | Flickerjump | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH86` | Forewarned | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH72` | Graceful Avoidance | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH52` | Hosts of the Dead | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH51` | Infinity Circuit | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH56` | Inspiring Legacy | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH40` | Nightshade Interceptors | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9c3562ffa183ede41b1626d9c9dc1e2f` | Path of Command | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `09bcb1b3b8794c4429d82d4b2e68fa1f` | Path of the Seer | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `1bfe0893592c9dd40bed8d68d5d2b516` | Path of the Warrior | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH53` | Reclaim the Stars | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `e59ac9e46f4ea804784e954c3b820e77` | Skyborne Deployment | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `851f73daf747eb642934a16429b391db` | Storm of Silence | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH43` | Sudden Assault | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH54` | Wailing Doom | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH57` | Webway Entrance | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH41` | Webway Gate | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH37` | Webway Incursion | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH73` | Whirling Death | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH44` | Wild Host | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH38` | Will of Asuryan | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `d3e99cb18b8cbba4081486f57f510f75` | Witchfire | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `33eabec155f628440a426a4ba480ec24` | Wrath of Khaine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH81` | Bright Lance Vyper | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH21` | Crimson Hunter | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH27` | Falcon | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH82` | Farseer Skyrunner | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH30` | Fire Prism | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH33` | Hemlock Wraithfighter | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH67` | Hornet | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH68` | Night Spinner | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH16` | Nuadhu 'Fireheart' | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH66` | Shining Spear Exarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH65` | Shining Spear | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH10` | Shroud Runner | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH18` | Vyper | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH26` | War Walker | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH25` | Warlock Skyrunner | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH29` | Wave Serpent | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `ASH9` | Windrider | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `ASH35` | Wraithknight | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH32` | Wraithlord | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ASH1` | Anvirr Keltoc | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `ASH2` | Eliac Zephyrblade | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `ASH3` | Medreyal Ghaelyn | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `ASH74` | Jain Zar | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `ASH76` | Lhykhis | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `AM2` | Ursula Creed | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM15` | Minka Lesk | unit | 未归因(别层消费) | 空表；正文由 光环 AuraSpecs 消费 | `Core/Aura.cs（AuraSpecs）/ Core/CardDef.cs:3071 FireCostWhen` |
| `AM61` | Defence Batteries | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM62` | Munitorum Supplies | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM60` | Timely Reinforcements | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM59` | Astropath | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM19` | Attilan Rough Rider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM24` | Bullgryn Bone'ead | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `AM31` | Cadian Castellan | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM8` | Cadian Medic | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM10` | Cadian Standard Bearer | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM17` | Commissar | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `AM4` | Faeburn Vanquisher | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM12` | Flamer Trooper | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM9` | Kasrkin | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM36` | Maelon Dhrost | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM18` | Master of Ordnance | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `AM70` | Scion Medic | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM27` | Nork Deddog | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `AM13` | Ogryn | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `AM22` | Primaris Psyker | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM6` | Ratling | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM11` | Shock Trooper Sgt. | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM5` | Shock Trooper | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM21` | Tech-Priest Enginseer | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM69` | Tempestor Sergeant | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM71` | Tempestus Aquilon | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM68` | Tempestus Scion | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM72` | Vitus Gryf | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM7` | Vox Operator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `AM76` | Armoured Lightning | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM52` | Armoured Squadron | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM53` | Artillery Strike | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM54` | At All Costs | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM39` | Born Soldier | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM51` | Cadia Stands | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `3a52026b99bb56f40aa9dc130fc66f34` | Cadian Honour | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `1ff4815a506d2f34e94ec2f50900d352` | Efficiency and Excellence | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM74` | Forged Killers | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM40` | Full Throttle | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM57` | Godspear Warhead | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM46` | High Command | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `cc11838649ff68a40b7d5d2b9fccd2bc` | Lead by Example | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `af98d423c7279a14c84c0d0382c53541` | Legendary Commandant | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM55` | Mechanised Infantry | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM41` | Move! Move! Move! | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM42` | Pinning Fire | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM47` | Press the Attack | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM43` | Recon Operation | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM48` | Regimental Doctrine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `98cd4c021d615174aba88df6012219c4` | Renowned Bodyguard | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM44` | Shock Troops | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM58` | Siege Warfare | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM49` | Summary Execution | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM50` | Thermal Weaponry | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM45` | Thunderous Charge | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM75` | Valkyrie Drop | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM56` | Vengeance for Cadia | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM67` | Aquilon Servo-Sentry | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `AM25` | Armoured Sentinel | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM14` | Autocannon Squad | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM38` | Baneblade Tank | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM33` | Basilisk | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `AM23` | Bombast Field Gun | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM28` | Chimera | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM29` | Hydra | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM35` | Leman Russ Vanquisher | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM32` | Leman Russ Tank | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM30` | Manticore | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM37` | Rogal Dorn Tank | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM16` | Scout Sentinel | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM73` | Taurox Prime | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM26` | Taurox | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM34` | Valkyrie | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM3` | Commissar Denkler | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM66` | Hektor Thenmann | hero | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `AM1` | Lord Solar Leontus | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL34` | Abaddon the Despoiler | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `BL12` | Aspiring Champion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL2` | Black Legionary | unit | 本来没正文 | desc 与关键词都为空（裸单位卡） | `W59 tsv（desc/keywords 逐字）` |
| `BL18` | Bringer of Decay | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL3` | Chaos Legionary | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL30` | Chaos Lord | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL4` | Chaos Sergeant | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL1` | Chaos Spawn | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `BL19` | Chaos Terminator | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `BL20` | Child of Torment | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL24` | Chosen Champion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL13` | Chosen | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL14` | Daemon Blade Legionary | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL25` | Dark Apostle | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL62` | Dark Disciple | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL67` | Ghallaron's Champion | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `BL66` | Ghallaron's Disciple | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL71` | Havoc Champion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL63` | Havoc | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL5` | Hound of Abaddon | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `BL32` | Iskandar Khayon | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL7` | Khorne Berzerker | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL21` | Lheorvine Ukris | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL15` | Master of Executions | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL22` | Master of Possession | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `BL69` | Meltagun Legionary | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL8` | Noise Marine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL26` | Obliterator | unit | 本来没正文 | desc 与关键词都为空（裸单位卡） | `W59 tsv（desc/keywords 逐字）` |
| `BL10` | Plague Marine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL16` | Possessed Champion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL9` | Possessed Marine | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `BL6` | Rubric Marine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL23` | Sorcerer | unit | 未归因(别层消费) | 空表；由 天赋层（随机池） 消费 | `Core/RuleCore.cs:10344` |
| `BL29` | Terminator Champion | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `BL70` | Veteran Havoc | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL11` | Veteran Legionary | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `BL17` | Warp Talon | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `BL68` | Zealot Legionary | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL37` | Abaddon's Chosen | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `36892cd906b0e5e4c9bbbe249db009b8` | Bitter Blows | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL55` | Black Crusade | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL43` | Daemonic Frenzy | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL44` | Daemonic Pact | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL38` | Dark Oratory | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL49` | Delightful Agonies | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `7bf8ce67fb6be4744ae44345a5b465fd` | Diabolic Strength | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL47` | Disgustingly Resilient | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL54` | Drach'nyen | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL50` | Execution | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL46` | Gifts of Chaos | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL53` | Heldrake Strike | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `61046837c330b48978b4af5201230b6e` | Helspear Assault | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL39` | Hideous Mutation | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL51` | Hosts of Chaos | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL75` | Idolatrous Despoilers | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `453b0e8f88e094a17866a6979f389ef5` | Infernal Gaze | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL40` | Legacy of Vengeance | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9176318eaf469354e858f51af2333aa3` | Litany of Despair | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL73` | Malicious Volleys | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL64` | Murderous Desires | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL41` | Possession | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `3f38d2e2c83df4b4c9052ef4a510cfbb` | Rites of Possession | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL48` | Skeins of Fate | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL45` | Spawndom | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL74` | Spreading Corruption | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL56` | Throne of the Heretic | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL42` | Traitors' Hate | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `141c5ed661a9e42bd9a793f0d2a1c682` | Unhallowed Gifts | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL52` | Unholy Smite | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL72` | Accursed Helbrute | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL28` | Helbrute | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `BL31` | Maulerfiend | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL27` | Venomcrawler | unit | 未归因(别层消费) | 空表；正文由 光环 AuraSpecs 消费 | `Core/Aura.cs（AuraSpecs）/ Core/CardDef.cs:3071 FireCostWhen` |
| `BL33` | Vex Machinator | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL35` | Haarken Worldclaimer | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL36` | Sylar Hexscorn | hero | 未归因(别层消费) | 空表；由 天赋层（随机池） 消费 | `Core/RuleCore.cs:10344` |
| `BL65` | Ghallaron the Pious | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA9` | Aggressor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA5` | Apothecary | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA23` | Bladeguard Veteran | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA16` | Captain | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA11` | Chaplain | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA24` | Company Champion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA28` | Company Master | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA12` | Company Veteran | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA32` | Deathwing Champion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA37` | Deathwing Knight Master | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA25` | Deathwing Knight | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA30` | Deathwing Strikemaster | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA17` | Deathwing Terminator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA31` | Ezekiel | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `DA18` | Inner Circle Companion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA4` | Intercessor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA19` | Interrogator Chaplain | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA13` | Librarian | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA20` | Master Lazarus | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA8` | Reiver | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA6` | Sergeant Taaman | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA22` | Techmarine | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA39` | Ancient Reliquary | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA47` | Blacksword Missiles | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `3e99603a7b7b2c54c8f1ad40250ee2ae` | Convoke the Circle | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA51` | Covert Operation | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA57` | Deadly Hunters | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA48` | Deathwing Assault | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA60` | Defensive Turrets | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA56` | Desperate Mission | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA58` | Fury of the Unforgiven | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA59` | Grim Effigy | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA43` | Grim Resolve | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA49` | Hunt the Fallen | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA40` | Inner Circle | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA52` | March of Vengeance | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA41` | Martial Superiority | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `a84a5b49f0ca9144585058db6987c2a0` | Master Interromancer | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `7c45a641ec845994097d185af58715f6` | Master of Manoeuvre | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9e0eecf8cb9a8df49bf18cc5d9051109` | Master of the Deathwing | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `24fcaf45556573549b922c06cad3aa2c` | None Must Know | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b3364cdc54882b04e8a931e86700ac23` | Obscure Ritual | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA61` | Plasma Generator | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA42` | Reconnaissance Mission | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `e01e8e933ad574b44bce5473ae486667` | Rites of Penance | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA50` | Sacred Standard | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA44` | Secret Agenda | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `708c0306e73fd494eb0f718ddaeea48a` | Smothering Decree | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA53` | Steadfast Warriors | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA45` | Stubborn Defiance | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `dd535f7a54bcf6241b24ca6d0e81bbb4` | Supreme Grand Master | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA46` | The Rock | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA54` | Unwavering Zeal | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA55` | Wages of Retribution | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA10` | Black Knight | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA35` | Dark Talon | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA29` | Darkshroud | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA21` | Land Speeder Vengeance | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA33` | Nephilim Jetfighter | unit | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA26` | Ravenwing Ancient | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA7` | Ravenwing Bikes | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA15` | Ravenwing Champion | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA14` | Ravenwing Speeder | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA27` | Ravenwing Talonmaster | unit | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA38` | Repulsor | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA36` | Sammael | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA34` | Unforgiven Redemptor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA3` | Asmodai | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA1` | Azrael | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA2` | Belial | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `38871853fabb5244d8148766829230cd` | Refuse to Yield | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA67` | Assault Terminator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA65` | Chaplain Gabutheron | unit | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA68` | Vengeful Brethren Bladeguard | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA69` | Vengeful Brethren Hellblaster | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA70` | Vengeful Brethren Intercessor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA71` | Watcher in the Dark | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA74` | Hunters of Heretics | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA73` | Relic Munitions | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA72` | Vengeful Brethren | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `DA66` | Ballistus Dreadnought | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `DA75` | Master Zacharial | hero | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC33` | Alluress | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC35` | Blissbringer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC32` | Daemonette | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC34` | Fiend | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC24` | Slaanesh's Spawn | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC15` | Winged Daemon Prince | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC2` | Blastmaster Noise Marine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC3` | Disharmonist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC16` | Screamer Kakophonist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC12` | Flawless Blade Champion | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC11` | Flawless Blade | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC18` | Glittering Myriad Infractor | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC9` | Icon of Excess | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC10` | Infractor Obsessionist | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC8` | Infractor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC17` | Konstrictus Tormentor | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `EC13` | Lord Exultant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC14` | Varius, Lord Kakophonist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC21` | Malgarash the Adamant | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC6` | Plasma Gun Tormentor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC27` | Rugged Disharmonist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC23` | Slaanesh Sorcerer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC1` | Sonic Blaster Noise Marine | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC26` | Terminator Champion | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC25` | Terminator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC19` | Threnodic Noise Marine | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC7` | Tormentor Obsessionist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC5` | Tormentor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC20` | Turyan Ghauze | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC4` | Veteran Noise Marine | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `EC42` | Beautiful Death | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC48` | Carnival of Excess | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC36` | Chosen of Slaanesh | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC51` | Coterie of the Conceited | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC54` | Dark Apparitions | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `6ed51d59b9f8ea24ab0dc7ceaef7956c` | Duelist's Hubris | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `5a8a11b2a89796f48b34fbec8db6f452` | Antrak Silk | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `70199b6f183ec364082ca16e62f3d0cb` | Heliotrophos | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `6e6d14006d8e9514b83a6c99e572081c` | Quail | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9215799995ff7a941b14737e27021032` | Shivversplint | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `4eec3b13d157dae48b14ed412169e692` | Skorflense | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `aa45079b714e3ff498549e533c5954a1` | Xylocil | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC40` | Embrace the Pain | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC39` | Eternal Servitude | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b9665eadf1207ff49bcf0a730b0b7f9d` | Euphoric Strike | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `2621d4280735baf4c952e6afea8d0686` | Excessive Vigour | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC47` | Exquisite Swordsmanship | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC41` | Internal Rivalry | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC45` | Mechanised Murder | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC52` | Mercurial Host | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC46` | Peerless Bladesmen | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC43` | Plains of Excess | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC38` | Pledge to the Dark Prince | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC44` | Quicksilver Grace | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC50` | Rapid Evisceration | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC53` | Rapturous Ruination | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC49` | Terrifying Crescendo | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC37` | Thrill Seekers | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC58` | Vengeful Surge | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC59` | Vial Tanks | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC28` | Chaos Land Raider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC31` | Chaos Rhino | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC29` | Heldrake | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC30` | Maulerfiend | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC55` | Lucius The Eternal | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC57` | Xarahan, Daemon Prince | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC63` | Alien Idol | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC65` | Pilfered Supplies | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC64` | Rusted Vent | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC26` | Aberrant Hypermorph | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GSC20` | Aberrant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC32` | Abominant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC16` | Acolyte Heavy | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC9` | Acolyte Hybrid | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC14` | Acolyte Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GSC15` | Acolyte Specialist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC35` | Benefictus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC10` | Biophagus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC21` | Clamavus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC5` | Concealed Explosives | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「回合起止 AtTurnClauses」认领（无消费点 0） | `W59 tsv` |
| `GSC4` | Genestealer Familiar | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC28` | Hulking Aberrant | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC23` | Hybrid Metamorph | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC34` | Kelermorph | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC17` | Locus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC29` | Metamorph Iconbearer | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC30` | Metamorph Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「光环 AuraSpecs」认领（无消费点 0） | `W59 tsv` |
| `GSC11` | Neophyte Heavy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC7` | Neophyte Hybrid | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC12` | Neophyte Iconbearer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GSC6` | Neophyte Initiate | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC18` | Neophyte Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC13` | Neophyte Specialist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC8` | Neophyte Trooper | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC24` | Nexos | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC37` | Patriarch | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC19` | Purestrain Genestealer | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GSC31` | Reductus Saboteur | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC25` | Sanctus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC56` | A Trap Sprung | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC40` | Backstab | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC46` | Bore Through | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `fd67b47572987b84cb2990ea1ecfe982` | Brood Prophet | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC50` | Clandestine Operation | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `7c666a04c1b9e8b4e9a96247e9fe115a` | Cult Propaganda | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC76` | Cut Them Off | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC59` | Day of Ascension | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `f45dd53721b645b4ab45d334edb986af` | Deadliest Killer | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC53` | Enhanced Aggression | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC42` | Enhanced Musculature | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC43` | Enhanced Resilience | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC55` | Eve of the Uprising | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `4959cd9e99597f74ca9af0bd65ae6307` | Experimental Bio-Horrors | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC58` | Gestalt Energy | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `334a32d196d46ed4387bf3740cce069b` | Improvised Barricade | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `d01a5ba5865ded4418194ea50e333b21` | Inscrutable Cunning | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `68909cf02f472734ea391665e0d28e15` | Jammed Communications | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC51` | Living Icon | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC39` | Lying in Wait | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `c433e3dde4952cc448220908226127ae` | Master Outrider | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `7831725387c8a774dbdf5b6492d928e0` | Nexus of Devotion | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC41` | Open Insurrection | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC57` | Perfect Ambush | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC47` | Perfect Hosts | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `a7bb23355a30d344e9d2decf976d91e1` | Poisoned Supplies | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC54` | Rampant Infestation | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC75` | Roaming Outriders | tactic | 通 | 腿A 收到 4 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC74` | Rogue Informant | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC44` | Subterranean Ambush | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC52` | Summon the Cult | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC49` | Telepathic Domination | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC45` | Underground Network | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `e9952f50921d2844a94d39ce883430f4` | Vox-Hacker | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC27` | Achilles Ridgerunner | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC68` | Atalan Jackal | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GSC71` | Atalan Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「光环 AuraSpecs」认领（无消费点 0） | `W59 tsv` |
| `GSC72` | Atalan Wolfquad | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC38` | Cult Leman Russ | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC22` | Cult Sentinel | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GSC36` | Goliath Rockgrinder | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GSC33` | Goliath Truck | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC69` | Jackal Outrider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC67` | Jackal Scout | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC70` | Jackal Specialist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC73` | Spotter Ridgerunner | unit | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GSC3` | Iconward Malak Vorenth | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GSC66` | Lhaska Szenari | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GSC2` | Magus Uthrel Naas | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GSC1` | Primus Saffa Rhiannor | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `SAU9` | Ophydian Destroyer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU2` | Canoptek Plasmacyte | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU1` | Canoptek Scarab | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU13` | Canoptek Spyder | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU7` | Canoptek Wraith | unit | 本来没正文 | 裸关键词 + 一句静态改战斗规则（④-ter 认领） | `Core/CardDef.cs:1535/1542/1559` |
| `SAU20` | Chronomancer | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `SAU14` | Cryptothrall | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU70` | Damaged Hexmark | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU67` | Damaged Plasmacyte | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU15` | Deathmark | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU3` | Flayed One | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU69` | Flayed Ones Pack | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU8` | Gauss Immortal | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU4` | Gauss Warrior | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU25` | Hexmark Destroyer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU71` | Immortals Phalanx | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU68` | Lokhust Destroyer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU22` | Lokhust Heavy Destroyer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU72` | Lokhust Lord | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU23` | Lychguard | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU29` | Necron Lord | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `SAU5` | Necron Warrior | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU26` | Plasmancer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU17` | Psychomancer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU65` | Royal Warden | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU27` | Scarab Swarm | unit | 本来没正文 | 裸关键词 + 一句静态改战斗规则（④-ter 认领） | `Core/CardDef.cs:1535/1542/1559` |
| `SAU18` | Skorpekh Destroyer | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU32` | Skorpekh Lord | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU11` | Technomancer | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU6` | Tesla Immortal | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU10` | Triarch Praetorian | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU33` | Vargard Obyron | unit | 本来没正文 | 裸关键词 + 一句静态改战斗规则（④-ter 认领） | `Core/CardDef.cs:1535/1542/1559` |
| `SAU19` | Canoptek Reanimator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU75` | Awakened Dynasty | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU54` | Conquering Tyrant | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU47` | Curse of the Phaeron | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU50` | Dimensional Breach | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU74` | Dimensional Corridor | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU44` | Disruption Blades | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU60` | Divination Menhir | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU38` | Extermination Protocol | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `461354d98aa5e3142afa4eb4922e5456` | Hardwired Destruction | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU45` | Hyperlogical Strategist | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU55` | Immortal Pride | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU58` | Implacable Conqueror | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `65b2be828179515458a282cf1b027182` | Indomitable Will | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU76` | Inescapable Retribution | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `30007162b68992e41a422f2061705b65` | Lord of the Storm | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `cbabeb234380634479feab1a1b4c733a` | Master Chronomancer | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU56` | Methodical Destruction | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `eb17cd22c6a5d7941a7456838d927ab1` | Particle Whip | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU40` | Protection Protocol | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `27a4b748a7bf358499f038aa09895ead` | Reanimate | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU41` | Reanimation Protocol | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU39` | Reconstitution Protocol | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU42` | Relentless Advance | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `d8a14171b08b54144ba9dbf324b0a042` | Relentless March | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU49` | Resurrection Orb | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU61` | Resurrection Vault | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU51` | Scythe Assault | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU52` | Self-Destruction | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU43` | Solar Pulse | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU46` | Spyder Nest | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU57` | Tomb World | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU73` | Annihilation Barge | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU24` | Command Barge | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `SAU21` | Doom Scythe | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU31` | Doomstalker | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU28` | Ghost Ark | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU34` | Monolith | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU16` | Night Scythe | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU12` | Tomb Blade | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SAU30` | Triarch Stalker | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU35` | Imotekh the Stormlord | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `SAU36` | Nemesor Zahndrekh | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName／AtTurn／光环层 AuraSpecs 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `SAU37` | Orikan the Diviner | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SAU66` | Ramatekh the Cruel | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF28` | Ghazghkull Thraka | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GOF79` | Beast Snagga Boy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF91` | Beastboss on Squigosaur | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF97` | Gargantuan Squiggoth | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF80` | Grot Orderly | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「回合起止 AtTurnClauses」认领（无消费点 0） | `W59 tsv` |
| `GOF83` | Grot on Bombsquig | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF93` | Hunta Rig | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF95` | Kill Rig | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF88` | Nob on Smasha Squig | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF94` | Painboss | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF84` | Runtherd | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF78` | Snakebite Grot | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF89` | Squighog Boyz | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF81` | Thump Gun Beast Snagga | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF90` | Wurrboy | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `GOF87` | Zodgrod Wortsnagga | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF100` | Cyber‑augmentation | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF103` | Da Hunt is On | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF101` | Da Old Ways | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `8f35df0536e4f7e4d86f3885e2597404` | Dok’s Toolz | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF104` | Drag it Down | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF107` | Fasta Than Yooz | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `84d4d192e0f57a34a90ac0f6172c3358` | Ferocious Rage | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF98` | Gnarled and Rugged | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF105` | Monster Hunters | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF106` | Power of the Waaagh! | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF99` | Skrag Every Stash! | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF102` | Thundering Stampede | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF96` | Snakebite Battlewagon | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF77` | Beastboss Morgrim | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GOF76` | Mozrog Skragbad | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GOF82` | Beast Snagga Nob | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `GOF85` | Snakebite Nob | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF86` | Unleashed Tramplasquig | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `GOF92` | Weirdboy | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `64a657fc5ed5bbb4fa7b585cf29c1e95` | Da Bigger Dey Iz... | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `d19e8b68aaa9a884781d06697ad237a6` | Speshul Dose | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `e3d67d86f73f7564eb0bf886ec848af6` | Waaagh! Energy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF23` | 'Ardshell Gurk | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GOF11` | Banner Nob | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF68` | Big Choppa Nob | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GOF16` | Big Krumpaz | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF59` | Big Shoota Boy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF3` | Bomb Squig | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF67` | Boss Nob | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF15` | Burna Boy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF13` | Deffgun Loota | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GOF1` | Grot | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF8` | Hornhelmz' Boy | unit | 本来没正文 | desc 与关键词都为空（裸单位卡） | `W59 tsv（desc/keywords 逐字）` |
| `GOF62` | Kommando | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF2` | Makari the Grot | unit | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF19` | Meganob Ugrak | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF21` | Mekboy Gazmek | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF55` | Ork Boy | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF57` | Ork Nob | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GOF12` | Spanner | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF18` | Painboy Sniklaw | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF66` | Rokkit Boy | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF9` | Shoota Boy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF14` | Skarboy Nob | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF4` | Slugga Boy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF65` | Stikkbomb Boy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF6` | Stormboy | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF60` | Tankbusta | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF5` | Trukk Boy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF20` | Veteran Stormboy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `GOF33` | Ard as Nails | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF30` | Attack Squig | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF31` | Cloud of Smoke | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF37` | Da Green Horde | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF42` | Da Irongob | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `20d0f4451f16fc441836a02f99bf77da` | Da Red Waaagh | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF72` | Dead Choppy | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF53` | Extractor Rig | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `6ba4b132d24528844a86304f8ce67833` | Follower of Gork | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF45` | Get'em ladz! | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `3ebb159036778d14ebd1c48451aa6ca0` | Greatest Warboss | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF43` | Gretchin Mob | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF49` | Gretchin Tower | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF74` | Grizzled Skarboy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF32` | Grot Bomb | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF71` | Krump da Gitz | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9dcbe3d0c04b02f4fbb19974ae0e37b2` | Mekaniak | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9f4e1b7ea9f00da458c6a859a4e958c8` | More Dakka | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF40` | No Mukkin' About | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF35` | Ork Encampment | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF38` | Proper Killy | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF34` | Prophet of the Waaagh! | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF64` | Pyromaniaks | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `98d3c65702746b04baf7f5ead1a9e507` | Ramshackle | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF48` | Rok Invasion | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b4d6d186eb95a3f468013c5be649ee3b` | Sawbonez | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF36` | Scrag ’Em | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF44` | Skorcha Assault | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF73` | Stomp 'Em | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF46` | Stormboyz Strike | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF39` | Tide of Muscle | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF52` | Toxic Bonfire | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `6899b199f5e36144ebbdd7ed9d9e7afe` | ’Uge Choppa | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF41` | Unbridled Carnage | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF47` | Will of Gork | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `8425b809e9e64e24f9035f709f5aaee4` | Worst Temper | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `508002f5d7ee54f4686aefa2e6adce41` | Wreckin' Ball | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF24` | Battlewagon | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF58` | Boomdakka Snazzwagon | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF22` | Deff Dread | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF56` | Deffkopta | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF25` | Gorkanaut | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF10` | Grot Tank | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF63` | Gunwagon | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `GOF17` | Killa Kan | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF70` | Krumpaklaw | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF61` | Rukkatrukk Squigbuggy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF26` | Stompa | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF7` | Warbiker | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF27` | Boss Zagstruk | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GOF29` | Grukk Face-Rippa | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `GOF75` | Warboss Gordrang | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `GOF69` | Mega-Blasta Deffkopta | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR71` | Adelaide the Serene | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR26` | Aestred-Thurga | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SOR14` | Amalia Novena | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR8` | Arco-flagellant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR66` | Armorium Cherub | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「回合起止 AtTurnClauses」认领（无消费点 0） | `W59 tsv` |
| `SOR6` | Battle Sister Gunner | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR5` | Battle Sister | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR23` | Canoness | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR30` | Celestian Sacresant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR34` | Celestian Superior | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR15` | Celestian | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR7` | Crusader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR21` | Death Cult Assassin | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR18` | Dialogus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR22` | Dominion Superior | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR11` | Dominion | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SOR29` | Ephrael Stern | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR20` | Hospitaller | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR25` | Imagifier | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR68` | Ministorum Priest | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR16` | Missionary | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR9` | Preacher | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR12` | Retributor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR67` | Sacred Rose Sister | unit | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR33` | Saint Celestine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR19` | Seraphim | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR13` | Simulacrum Bearer | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR24` | Simulacrum Celestian | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR31` | Dogmata | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR4` | Sister Novitiate | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR17` | Sister Superior | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR10` | Sister Repentia | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SOR37` | Triumph of St. Katherine | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SOR69` | Zephyrim Superior | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR28` | Zephyrim | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SOR56` | Beacon of Faith | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR53` | Blade of Faith | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR43` | Cleansing Flames | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR41` | Conviction of Faith | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `e01f4d54e8a474d96b5a1167cc28eebf` | Daemonbreaker | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR45` | Defenders of the Faith | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR75` | Devout Serenity | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR58` | Devout Warriors | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR51` | Divine Intervention | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR74` | Emperor's Judgement | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR54` | Fiery Conviction | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR46` | Furious Reprisal | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `8901e35c5de30eb48804da6651cfb132` | Fury of the Righteous | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR61` | Guidance of the Saints | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR57` | Hallowed Martyrs | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR60` | Holy Fire | defence | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `4d348cff71208c24d8e39252d913ff9d` | Hymn of Battle | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR48` | Imperial Creed | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR40` | Inspiring Oratory | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `65cfec7d2bc719040b3b4b88622931b2` | Lead the Righteous | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR47` | Miraculous Feat | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR39` | Prayer | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR50` | Relics of St. Katherine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `f104d4c21abe84546abc9365d9eda8dd` | Righteous Repugnance | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR42` | Rite of Restoration | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR59` | Sacred Altar | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR73` | Sacred Rose | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR49` | Shrineworld | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR52` | Spirit of the Martyr | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR55` | Trial of Suffering | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR72` | Castigator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SOR36` | Exorcist | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR35` | Immolator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR32` | Mortifier | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR38` | Paragon Warsuit | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SOR27` | Penitent Engine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR70` | Sororitas Rhino | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `SOR65` | Celestian Sacresant Aveline | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR1` | Erika Luminas | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR2` | Junith Eruita | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR3` | Morvenn Vahl | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW1` | Logan Grimnar | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName／开局上手 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `SW19` | Iron Priest | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW26` | Arjac Rockfist | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW28` | Blackmane Aggressor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW8` | Blackmane Reiver | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW6` | Blood Claw Pack Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW4` | Blood Claw | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW7` | Deathwolf Scout | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW9` | Fenrisian Wolf | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW24` | Frost Claw Wulfen | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SW15` | Fyrri Askar | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW11` | Grey Hunter Pack Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW10` | Grey Hunter | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW18` | Hrolf the Ironhowl | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「光环 AuraSpecs」认领（无消费点 0） | `W59 tsv` |
| `SW5` | Hunting Wolf | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW16` | Long Fang | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW29` | Sons of Morkai Eliminator | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW30` | Terminator Rune Priest | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW20` | Thunderwolf Cavalry Pack Leader | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW12` | Thunderwolf Cavalry | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `6e4f9040e8cc59641a32f29e33ea4cbc` | Tyrnak and Fenrir | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW25` | Wolf Guard Battle Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW13` | Wolf Guard Headtaker | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW27` | Wolf Guard Terminator Pack Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW31` | Wolf Guard Terminator | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SW14` | Wolf Priest | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW21` | Wulfen Pack Leader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW17` | Wulfen | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `SW43` | Anvil of Endurance | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW57` | Birth of a Saga | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW40` | Bjorn's Shrine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW39` | Canis Helix | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW44` | Death Shun | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW53` | Embers of Prospero | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW49` | Fenrisian Blizzard | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW54` | Fenrisian Monstrosities | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW60` | Fenrisian Runestones | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW58` | Fenrisian Wolfpack | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW61` | Halls of Legend | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b3dce3f10cc118541910156ec8da1d9b` | High King of Fenris | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `a51adc8f5c311584385c3117cfd919e9` | High Rune Priest | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW50` | Hordeslayer | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW51` | Hunter's Guile | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW55` | Legendary Tenacity | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW56` | Let Loose | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `7a01882fe4610244292d16b15f4b873e` | Living Lightning | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `4d4b9170663891f488c0e96ee8e55ba1` | Murderous Hurricane | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW59` | Orbital Defences | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW41` | Pack of One | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW52` | Proper Hunt | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW46` | Raid Tactics | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW47` | Saga of the Bold | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW42` | Savage Legacy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b551c8351ebe5784faf9912b573eaf93` | Tempest's Wrath | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW48` | The Fang | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW45` | Unbridled Fury | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `e3b5735d801fcbd49aa8d3975a5b0c31` | War Howl | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW36` | Bjorn the Fell-Handed | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW22` | Blackmane Inceptor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW23` | Fenrisian Drop Pod | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW38` | Land Raider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW34` | Murderfang | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW32` | Stormwolf | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW35` | Venerable Dreadnought | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW33` | Vindicator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW37` | Wulfen Dreadnought | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SW2` | Njal Stormcaller | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `SW3` | Ragnar Blackmane | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TAU27` | Broadside Battlesuit | unit | 本来没正文 | 整条 desc 只裸关键词 + `Companion N:`（Companion 层） | `Core/CardDef.cs:1231-1233` |
| `TAU34` | Coldstar Battlesuit | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TAU11` | Crisis Battlesuit | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU17` | Crisis Bodyguard | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU28` | Enforcer Battlesuit | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU36` | Riptide Battlesuit | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TAU21` | Stealth Battlesuit | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TAU26` | Stealth Shas'ui | unit | 本来没正文 | 整条 desc 只裸关键词 + `Companion N:`（Companion 层） | `Core/CardDef.cs:1231-1233` |
| `TAU38` | Stormsurge | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TAU4` | Guardian Drone | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU6` | Gun Drone | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `TAU7` | Marker Drone | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `TAU8` | Missile Drone | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU15` | Sniper Drone | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TAU10` | Stealth Drone | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `TAU32` | Aun'Shi | unit | 未归因(别层消费) | 空表；正文由 光环 AuraSpecs 消费 | `Core/Aura.cs（AuraSpecs）/ Core/CardDef.cs:3071 FireCostWhen` |
| `TAU14` | Fire Warrior Breacher | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU23` | Breacher Shas'ui | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU29` | Cadre Fireblade | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU12` | Darkstrider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU18` | Fire Warrior Marksman | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU5` | Fire Warrior | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU25` | Firesight Marksman | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TAU70` | Flesh Shaper | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TAU19` | Honoured Ethereal | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU66` | Kroot Carnivore | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU67` | Kroot Hound | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU71` | Krootox Rampager | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU69` | Krootox Rider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU72` | Lone-spear | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU9` | Pathfinder | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU22` | Strike Team | unit | 本来没正文 | 整条 desc 只裸关键词 + `Companion N:`（Companion 层） | `Core/CardDef.cs:1231-1233` |
| `TAU68` | Trail Shaper | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU16` | Vespid Stingwings | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU57` | Burgeoning Empire | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU58` | Coordinated Engagement | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `507f79fa60945d04e93fe806aca912b7` | Deploy Anchors | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `c4f5c920744030c4e84428dc5a0f1bf2` | Drone Companion | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU47` | Dynamic Offensive | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU48` | Emergency Dispensation | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `f8260229254a09746af6d9ff7a9b0b54` | Ethereal Supreme | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU42` | Experimental Drones | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU43` | For The Greater Good | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU53` | Fortified Positions | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b3735ef3d1408d544b0947e31f898e20` | Grisly Feast | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9f75a6fe11922eb409ffbad4ecdeb405` | Hero of the Empire | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU73` | Hidden Hunters | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `1688c6146abb1e04f98e540b4cbbabd7` | Homing Beacon | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU75` | Hunter's Instincts | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU49` | Inspired to Greatness | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU39` | Kauyon Tactics | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU50` | Priority Target | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU44` | Pulse Onslaught | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU54` | Razorshark Strike | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU45` | Recon Sweep | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU55` | Relentless Fusillade | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU51` | Saviour Protocols | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU46` | Sense of Stone | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `f2b80d575cbf8f64597b991022beaf35` | Serene Unifier | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `684196db974ab254090f39ef6e89868c` | Shield Generator | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU52` | Storm of Fire | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `f279c5cf1c3e16e4db6b9df4d941ad11` | Structural Analyzer | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU40` | Technological Supremacy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU74` | Thundering Rampage | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU64` | Tidewall Droneport | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU63` | Tidewall Gunrig | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU56` | Valued Sacrifice | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU62` | Watch Tower | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU41` | Zephyr's Grace | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU13` | DS8 Support Turret | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `TAU24` | Devilfish | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU31` | Hammerhead | unit | 本来没正文 | 整条 desc 只裸关键词 + `Companion N:`（Companion 层） | `Core/CardDef.cs:1231-1233` |
| `TAU35` | Longstrike | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU20` | Piranha | unit | 未归因(别层消费) | 空表；正文由 光环 AuraSpecs 消费 | `Core/Aura.cs（AuraSpecs）/ Core/CardDef.cs:3071 FireCostWhen` |
| `TAU33` | Razorshark Fighter | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU30` | Sky Ray Gunship | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TAU37` | Sun Shark Bomber | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU2` | Aun'Va | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TAU3` | Commander O'Maisos | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TAU1` | Shadowsun | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TAU65` | War Shaper | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL38` | Hive Guard | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL74` | Terror of Vardenghast | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL67` | Barbgaunt | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL6` | Biovore | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `TL19` | Broodlord | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TL17` | Deathleaper | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL7` | Gargoyle | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL8` | Genestealer | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `TL2` | Hormagaunt | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL13` | Lictor | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL3` | Mucolid Spore | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL60` | Neurogaunt Nodebeast | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL66` | Neurogaunt | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL65` | Neuroloid | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL9` | Pyrovore | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TL62` | Ravener | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL1` | Ripper Swarm | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL37` | Spore Mine | unit | 未归因(别层消费) | 空表；正文由 回合起止 AtTurnClauses 消费 | `Core/EffectResolver.cs:6070 + Core/EffectText.cs:4391` |
| `TL61` | Stranded Termagant | unit | 本来没正文 | desc 与关键词都为空（裸单位卡） | `W59 tsv（desc/keywords 逐字）` |
| `TL75` | Termagant Brood | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TL4` | Termagant | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL15` | Tyranid Prime | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TL5` | Tyranid Warrior | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL12` | Tyrant Guard | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL11` | Venomthrope | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL10` | Von Ryan's Leaper | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL18` | Zoanthrope | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TL20` | Carnifex | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL27` | Exocrine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL24` | Harpy | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TL28` | Haruspex | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL22` | Hive Crone | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL25` | Hive Tyrant | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TL26` | Maleceptor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL16` | Mawloc | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL69` | Neurotyrant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `TL70` | Norn Emissary | unit | 未归因(别层消费) | 空表；正文由 降费 CostWhens 消费 | `Core/Aura.cs（AuraSpecs）/ Core/CardDef.cs:3071 FireCostWhen` |
| `TL68` | Psychophage | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL21` | Screamer-Killer | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `TL14` | Sporocyst | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL23` | Tyrannocyte | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TL30` | Tyrannofex | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL29` | Winged Tyrant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL33` | Adrenal Surge | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `c0ddba6cc45944f46a65f4fd93bcc0a5` | Aerial Seeding | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `8f2da557fe484ee4bbf427c13eccd4cb` | Alpha Warrior | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL39` | Ambush Predator | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL45` | Apex Predator | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL43` | Augmented Ferocity | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL73` | Bestial Rage | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL63` | Bounding Leap | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `cbc9d98e39cc7fb4fa6f94c7e69bda18` | Brood Progenitor | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL42` | Darkened Skies | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL54` | Digestion Pool | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL32` | Encircle the Prey | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL31` | Enhanced Organism | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `468db99b4b10d0745bab346df37d3fb2` | Feeding Frenzy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL44` | Hardened Biology | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `5b5425f6e2848b449807bd1643d0d936` | Hive Commander | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL47` | Hive Fleet Leviathan | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL49` | Hyper-adaptation | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL40` | Infestation Node | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL50` | Infinite Biomorphologies | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL52` | Leviathan's Tendrils | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL41` | Overwhelming Swarm | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL48` | Predator Instincts | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL46` | Rapacious Hunger | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL64` | Ravenous Hunter | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `760bd3afde513774d96d6492f995fd37` | Spirit Leech | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL53` | Swarming Masses | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `a58e05916eb5e994daa0349cbe237001` | Synaptic Imperative | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL71` | Toxic Entanglement | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL51` | Toxic Miasma | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL56` | Toxic Vapours | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL72` | Tyranid Invasion | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL36` | Neurothrope | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TL35` | Swarmlord | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `TL34` | Tervigon | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `UM76` | Lieutenant Titus | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `UM58` | Desolation Marine | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `UM101` | 1st Company Terminator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM19` | Aggressor Sergeant | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `UM12` | Antaro Chronus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `UM15` | Apothecary Polixis | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM27` | Assault Centurion | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `UM4` | Assault Intercessor | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `UM104` | Bladeguard Ancient | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM23` | Bladeguard Lieutenant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM80` | Bladeguard Sergeant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM29` | Captain Sicarius | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM77` | Chairon | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM100` | Chaplain Cassius | unit | 未归因(别层消费) | 空表；由 天赋层 TalentName／静态规则 ④-ter 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `UM96` | Chapter Champion | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM18` | Company Ancient | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM25` | Devastator Centurion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM98` | Devastator Marine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM81` | Eliminator Sergeant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `UM8` | Eliminator | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `UM26` | Epistolary Librarian | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM106` | Ferren Areios | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM3` | Firstborn | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM78` | Heavy Intercessor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM10` | Hellblaster | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `UM14` | Honour Guard | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM22` | Inceptor Sergeant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM89` | Incursor | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM56` | Intercessor Sergeant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM24` | Lieutenant Calsius | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM2` | Octavio Infiltrator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM99` | Phobos Librarian | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM90` | Phobos Lieutenant | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM13` | Primaris Chaplain | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM79` | Primaris Eradicator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM9` | Primaris Inceptor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM6` | Primaris Intercessor | unit | 本来没正文 | desc 与关键词都为空（裸单位卡） | `W59 tsv（desc/keywords 逐字）` |
| `UM21` | Primaris Judiciar | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM5` | Primaris Reiver | unit | 本来没正文 | desc/keywords 全为裸关键词（无可执行正文） | `W59 tsv（desc/keywords 逐字）` |
| `UM68` | Primaris Techmarine | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM94` | Scout Sniper | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM1` | Scout | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM16` | Sergeant Allectius | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM82` | Sergeant Gadriel | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM20` | Sergeant Telion | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM55` | Sternguard Sergeant | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM97` | Suppressor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM91` | Tactical Marine | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM102` | Terminator Captain | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM103` | Terminator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM7` | Tyrannic War Veteran | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM93` | Vico Therbeus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM109` | Adaptive Strategy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `4de4fcfb5d7e0554a88b9f419a2cd7c2` | Angel's Wrath | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM64` | Angels of Death | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `0740581c2c0523745a42dbec1b2ef82a` | Might of Heroes | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM75` | Armoured Offensive | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM44` | Armoured Support | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `40667e346ae58a84dbda1033dff70ccb` | Author of the Codex | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM40` | Avenging Zeal | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM62` | Battlefield Supremacy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `8482077f309b08943b190f18b5874871` | Catechism of Death | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM112` | Champions of Humanity | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM63` | Chapter Council | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM35` | Codex Discipline | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM37` | Death from Above | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM113` | Duty's End | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `315d9f293e4f56d4f813584b43f0511c` | Exhortation of Rage | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM110` | Fall Back | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM52` | Firestrike Turrets | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM42` | Forward Deployment | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM45` | Gather the Company | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `0544047df88d2ba4cad9c17ff080c0eb` | Greatest Deeds | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM36` | Hero's Respite | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM53` | Hive Factory | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM60` | Humanity's Shield | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM118` | Indomitus Crusade | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM39` | Inspired Retribution | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM43` | Knights of Macragge | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM114` | Lead From the Front | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM54` | Light Cover | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `dcbd28525888ce54aaa93a47ba4de1e6` | Master Tactician | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `f923701f207d23a4f81d6ff5891b4a0c` | Master of Arcana | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `55b8e3d2040f86d4190ffb1a91abb5ed` | Master of Arms | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b14d1db2b0730d649b3bec35af100363` | Master of Battle | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9238c27730a0c424288d46ed0f4a742c` | Master of the Fleet | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM116` | No Mercy | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM117` | No Respite | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM73` | Oath of Moment | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `7693f2b21b2f85644b739da990bc2216` | Paragon of Ultramar | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM33` | Pariah Vanguard | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM34` | Point-Blank Shot | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `e874c2bd84895e64694daa44bd5fd5c2` | Primarch of the XIII | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM74` | Rapid Deployment | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `26d4ded12b28a3742b5c8fc155e29803` | Righteous Fury | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM41` | Sacred Bolter | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `c4ff279078605aa42942c8dd16b77147` | Scryer's Gaze | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM111` | Shall Know No Fear | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM31` | Sheltered Location | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `cf0b346f70e748c4bbfc4dc03ab39ead` | Smite | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `d94f91051ecfe014f97577ff573571aa` | Spear of Macragge | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM115` | Stormhawk Interception | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM38` | Stormraven | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `ef596edae1828bd46bfc4348535a3acc` | Supreme Strategist | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM32` | Tactical Insight | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM61` | The Chapter's Due | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM11` | 7th Company Bike | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM95` | Attack Bike | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM108` | Brutalis Dreadnought | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM105` | Dreadnought | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM92` | Firestrike Servo-Turret | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM70` | Invictor Warsuit | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `UM69` | Land Speeder | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM67` | Outrider | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM17` | Primaris Impulsor | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM57` | Primaris Invader | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM28` | Redemptor Dreadnought | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM30` | Repulsor Executioner | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM66` | Scout Bike | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `UM71` | Storm Speeder Hailstrike | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM59` | Stormtalon | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM83` | Valtus | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 · 另 1 条无头句由「事件层 WhenTriggers」认领（无消费点 0） | `W59 tsv` |
| `UM107` | Whirlwind | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM65` | Chaplain Letharius | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM46` | Marneus Calgar | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `UM87` | Roboute Guilliman | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM47` | Uriel Ventris | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `UM88` | Valius Paxor | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM48` | Varro Tigurius | hero | 未归因(别层消费) | 空表；由 天赋层 TalentName 消费 | `Core/CardDef.cs:1520 + Data/CardDatabase.cs:288 + Core/TalentCardData.cs:156` |
| `UM86` | Assault Doctrine | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM84` | Devastator Doctrine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM85` | Tactical Doctrine | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `c06453b330af61745beb5fa567ba863e` | Lord Commander | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `4a48457c24877f345be2e96f867c1e94` | Dark Pact of Fate | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `9a2cadc64c80a6b42912441105bb1cee` | Dark Pact of Blood | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `b8e81b2fe85cda747a68c4d6326b669c` | Dark Pact of Excess | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `1dfc9bc738bfc4f4d93d48f165aee510` | Dark Pact of Resilience | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `4cdc1b5f965f99945b685bca7660e9fa` | Master of Repentance | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC56` | Lord Kaphrael | hero | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC22` | Veldras the Sublime | unit | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC60` | Armoury of Excess | defence | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `EC61` | Decadent Throne | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU53` | Undying Legions | tactic | 通 | 腿A 收到 3 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SOR44` | Moment of Grace | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `cc1400347b15ee3449bc18bbdb9ddec9` | Exemplary Warrior | tactic | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `UM72` | Predator Annihilator | unit | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL57` | Hellfire Torch | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `BL58` | Hellfire Pit | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `SAU59` | Awakened Obelisk | defence | 通 | 腿A 收到 2 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `TL55` | Protective Bio-structure | defence | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
| `97e959444b53f424ea4285e2ca02c7e6` | Chosen of the Four | tactic | 通 | 腿A 收到 1 条 op · NoMech 0 · 孤儿 0 | `W59 tsv` |
