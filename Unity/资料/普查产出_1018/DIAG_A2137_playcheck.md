# DIAG · `A2137`：卡牌线 `play check` 的既有红（只读诊断，2026-10-19）

> 执行者 = 只读诊断代理。**没跑 Unity / `*.Run` / player；未改仓库任何文件**（只写本报告与 `_tmp_view/diag_a2137/` 临时输出）。
> 跑过 = `bash Unity/工具/ruleprobe.sh play check` · `play all pf`（当前池 / 开工前快照池 / 两份只改卡名的对照池）· `play <id>` 若干。
> 工作树状态 = 本波（`W_卡牌线_数据侧2`）的 12 张 `keywords` 尚未提交；`RuleEngine/Core` 的在飞改动经核为**注释级**（`CardDef.cs` / `CreatePool.cs` 的 diff 全是注释与对账表），`DeckRules.cs` 的行为改动会编进探针（`ruleprobe.csproj` 编译 `工具/ruleprobe/*.cs` ＋ `RuleEngine/Core/*.cs`）但**不在 `play` 的执行路径上** —— 判据 = 同一份代码 ＋ 开工前卡池复现出开工前指纹（逐字节一致）。

---

## §一 结论

`play check` 当前 **rc=1**，三条失败项：

| 段 | 失败读数 | 判定 |
|---|---|---|
| ①-a 主档（打出去那一拍） | **70 张卡变了** · 新增 0 · 消失 0 | **全部有归因**：γ 59 ＋ δ 1 ＋ α 10（另 2 张 = γ＋α 叠加） |
| ①-b 事件电池 | **10 张卡变了** · 新增 0 · 消失 0 | γ 4 ＋ α 6 |
| ② 哨兵（基线文件 md5 vs 源码常量） | 基线 `2754c07f…` ≠ 钉住 `76ced8a6…` | **α**（K06 重生成基线时忘了重钉） |

**按 80 个变化行（70 主档 ＋ 10 电池）分桶：**

| 桶 | 行数 | 内容 |
|---|---:|---|
| **β 真行为缺陷** | **0** | 见下面的「排除 β 的两条硬证据」 |
| **α 基线过期**（该重生成＋重钉） | **16**（另有 2 行叠加） | 本波 12 张 `keywords` 回卡面：主档 **10 个新增红点** ＋ 电池 **6 行**；`TL74`/`UM87` 本来就在 γ 的 60 里（叠加） |
| **γ 数据漂移**（已裁定、预期内） | **63** | 49 张 `health` ＋ 6 张 `cost` ＋ 1 张 `attack`（= 2026-10-18 `4995622` 的 **SO 数值真值覆盖 56 处**）＋ 6 行 W28 官方改名连带（4 个督军的「手牌总价」行 ＋ `E|GOF92`/`E|GOF94`）＋ 电池 `TL25`/`TL66` 的费用连带 |
| **δ 工具前提错误** | **1 行**（＋两条结构性发现） | `GOF48 Rok Invasion`：候选池按**卡名排序**后按下标抽 ⇒ 改名会换掉随机结果。另两条：② 的「重生成基线没重钉」流程缺口；`SpawnTalents` 用**显示名**做键 |

**排除 β 的两条硬证据（这是本诊断的关键）：**

1. **同一份代码 + 同一份卡池 ⇒ 基线可逐字节复现。** 用当前编译出来的探针、把卡池换回 K06 提交（`3251df0`）的 `cards_engine.json` 跑 `play all pf`，得到的合集 md5 = **`2754c07fac4227283235b3e9988fd8ad`**，与仓库里的基线文件**逐字节吻合**。⇒ 当前引擎/探针在「相同输入」下**没有任何回归**；70/10 全部来自**卡池内容**变化，不是代码漂移。
2. **逐条 A/B 复现了每一类变化**：
   - 把当前池里**只**把 6 张天赋卡的显示名改回旧写法（W28 之前）：`EC55/EC56/EC57` 的指纹行**与基线逐字节相同**；`GOF76` 的「手牌总价」行消失（只剩 `4995622` 的血量差）；`E|GOF92`/`E|GOF94` 的净差**回到 0 且与基线逐字节相同**；
   - 把当前池里**只**把 `GOF12 Ork Spanner → Spanner` 改回旧名，`GOF48` 的随机部署**逐张回到基线那一组**（`GOF13 / GOF4 / GOF78 / GOF65 / GOF12`）。

**建议（一句话）**：**先确认 `BL71`/`SW13` 两张单位血量（`R_督军10血成因.md` §5 明写「未单独定性」）与 12 张关键词（`W` 报告已三源核过）；可选先修 δ（`SortByName` 改按 `Id`）；然后在工作树收口后重生成基线 → 把 `PinnedPlayMd5` 改成新基线 md5 → 复跑 `play check` 至 rc=0。** 详见 §五。

---

## §二 逐段失败说明

### 2·1 `play check` 到底查什么（`PlayProbe.cs`）

| 段 | 代码 | 含义 | 计入 rc |
|---|---|---|---|
| **①-a 主档** | `PlayProbe.cs:2134-2147` | 当次 `play all` 算出的**主档逐卡指纹**与基线文件逐行比（键 = 卡 id；出「哪张卡、哪一列、旧值→新值」） | ✅ |
| **①-b 事件电池** | `PlayProbe.cs:2149-2162` | 同一套对拍，行是 `E|<id>` 前缀（只覆盖 `type=unit`；量「打出去之后走一段事件，局面动没动」） | ✅ |
| **①-c 覆盖面** | `PlayProbe.cs:2232-2277` | 电池净差 = 0 的候选逐条列出（**提示**，不是判决） | ❌（不计 rc） |
| **② 哨兵** | `PlayProbe.cs:2279-2296` | **基线文件内容**的规范化 md5 必须 = 源码常量 `PinnedPlayMd5`（`PlayProbe.cs:1900`）。这是「重生成基线把一次真回归抹平」的堵法 | ✅ |
| rc | `PlayProbe.cs:2307-2308` | ①-a 全等 ∧ ①-b 全等 ∧ ② 过 ⇒ 0，否则 1 | — |

指纹行内容（`FingerprintRow`，`PlayProbe.cs:1934-1946`）：`id|type|warlord|playRc|played|faceOps|自落场|机械|实得|应付|能量Δ|手牌价Δ|伤害|变化条数|crash|档|fixtureFit|injected|变化逐条全文`；
电池行（`FingerprintERow`，`PlayProbe.cs:1990-2003`）：`E|id|type|bucket|cand|playRc|played|ctrlPlayed|steps|raw|ctrl|net|netDmg|note|netFull`。
**⛔ 不含卡名** —— 设计意图正是「改名不该让这条天天红」；本次 δ 一条（`GOF48`）说明这个意图**没有完全做到**（见 2·4）。

### 2·2 ①-a 主档 · 70 张（全部拿到，不止 `play check` 打印的 60 张）

`play check` 只打印前 60 张。完整 70 张是拿**当次现算的 `play all pf` 输出**与基线文件逐行 diff 出来的（脚本只读，见 `_tmp_view/diag_a2137/`）。

| 类 | 行数 | 卡 | 变化列 |
|---|---:|---|---|
| SO 血量（47 督军） | 47 | `AM1…UM87`（见 §三·A） | 只有 `changesFull`（督军格 `WARLORD …/血` 变） |
| SO 血量（2 单位） | 2 | `BL71` 7→8 · `SW13` 4→3 | 只有 `changesFull` |
| SO 近战 | 1 | `TAU12` 3→2 | 只有 `changesFull` |
| SO 费用 | 6 | `Infernal Gaze` 2→1 · `BL74` 4→3 · `TAU63` 0→1 · `TL66` 2→1 · `UM32` 0→1 · `Synaptic Imperative` 2→1 | `expectedPay`/`energyDelta`/`handCostDelta`/`changesFull`（`TAU63`/`UM32` 另有 `mech`/`changes`） |
| W28 改名连带（天赋卡不再按旧名解析不了） | 3 | `EC55`/`EC56`/`EC57` | `other`/`changes`/`changesFull`（新增「💰 手牌总价 P0 +2」一行） |
| δ 随机候选按名排序 | 1 | `GOF48 Rok Invasion` | 只有 `changesFull`（部署的 5 张换了） |
| **小计（既有 60）** | **60** | | |
| 本波关键词 · 新增红点 | 10 | `SW2` `SW3` `SW27` `SW34` `SW37` `UM12` `UM100` `UM91` `UM95` `UM96` | 只有 `changesFull`（关键词/带正文条目进 `{…}`） |
| 本波关键词 · 已在 60 内（γ＋α 叠加） | 2 | `TL74` `UM87` | `changesFull` |
| **合计** | **70** | | |

> `SW2`/`SW3` 在**本波之前**不在红名单里（它们的 `keywords` 当时是 `[]`，天赋名只从 `desc` 收，不进 `UnitState.Keywords`）；本波把 `Talent: <名>` 写进 `keywords` 后 `{talent1}` 才出现在督军格。`TL74`/`UM87` 的 `changesFull` 同时含「血量 33→23 / 38→28」与「关键词新增」，所以行数只 +10（12 − 2 叠加）。

### 2·3 ①-b 事件电池 · 10 张

| 卡 | 列 | 旧 → 新 | 归因 |
|---|---|---|---|
| `GOF92 Weirdboy` | `raw`/`net`/`netFull` | 24/0/空 → 25/1/`✋ 手牌 +「Waaagh! Energy」` | γ · W28 改名（天赋卡 `Waaagh! Energy (Weirdboy Talent)` → `Waaagh! Energy`，`SpawnTalents` 的按名查找从此命中） |
| `GOF94 Painboss` | 同上 | 25/0/空 → 26/3/`✋ 手牌 +「Dok's Toolz」`＋两条手牌总价 | γ · W28 改名（同上） |
| `TL25 Hive Tyrant` | `netFull` | 手牌总价 `2→6(+4) / 6→7(+1)` → `2→5(+3) / 5→6(+1)` | γ · `Synaptic Imperative` 费用 2→1（SO 覆盖） |
| `TL66 Neurogaunt` | `raw`/`ctrl` | 22/22 → 21/21 | γ · 自身费用 2→1（SO 覆盖） |
| `SW27` `SW34` `SW37` | `netFull` | 单位身上多 `slay1` / `rally1` 等条目 | α · 本波关键词 |
| `UM91` `UM95` `UM96` | `netFull` | 单位身上多 `oath1` | α · 本波关键词 |

### 2·4 δ（工具前提）三条

1. **`GOF48` 的随机结果被卡名移动**：`CreatePool.SortByName`（`CreatePool.cs:1210-1215`）把候选池**按卡名 ordinal 排序**，结算层再按 `ctx.Rng` 的**下标**抽。W28 把 `Ork Spanner` 改成 `Spanner`，24 张候选里第 11 名之后的顺序整体前移一位 ⇒ **同一串随机下标抽到不同的卡**。实测：只把 `GOF12` 的名字改回旧写法，`GOF48` 的 5 张部署**逐张回到基线**（`GOF13 / GOF4 / GOF78 / GOF65 / GOF12`）。
   ⇒ 「指纹不含卡名」这条设计**在随机部署类卡上不成立**：卡名通过排序**间接**进了指纹。建议把排序键从 `Name` 改成 `Id`（见 §五·2）。
2. **② 哨兵的根因是流程缺口，不是数据错**：K06（`3251df0`）重生成基线时**只重钉了 `Program.cs` 的三条指纹**（`K06_池id迁移_报告.md:64-67` 原文），没动 `PlayProbe.PinnedPlayMd5`。而在 K06 之前，**每一次**重生成都同时重钉（见 §四表：5 次全配平）。
3. **`SpawnTalents` 用显示名做键**（`RuleCore.cs:8666-8678`：先 `CreatePool.FindByName(ctx.CardPool, u.Card.TalentName)`，失败才退回 `PickTalentFromPool`）⇒ 卡名一改，天赋 token 的生成**行为跟着变**。本次变化本身是**修好**（旧名带 `(… Talent)` 后缀时按名查不到、天赋 token 不生成），但这是一条「显示名进机制」的结构性脆弱点，改名/本地化都可能再次静默改行为。

---

## §三 逐张表

> 「旧值→新值」列：主档写 `changesFull` 里的实际读数（督军格那一段），括号里附**卡表数据值**；费用/攻击写数据值。
> 「最近改动」= 引入该变化的提交（数据源列在同一格）。当前工作树里本波 12 张 `keywords` 为**未提交**（`Unity/数据/游戏数据/cardface_fixes.json` ＋ `cards_engine.json`）。

### 3·A ①-a 既有 60 —— 47 张督军血量（γ · `4995622`）

> 背景与裁定 → `资料/普查产出_1018/R_督军10血成因.md` §1「结论 A：当前 build 的 SO 就是真值」；
> 落地清单 → `资料/普查产出_1018/W_SO数值覆盖.md`（49 health / 6 cost / 1 attack = 56 处）。
> ⚠️ 督军格的显示值与卡表 `health` 有一个**既有固定偏移**：47 张里 **46 张显示 = 卡表 − 2**（`AM1` 40→38、`AM2` 35→33），只有 `TAU3` 显示 = 卡表值。本次只关心**变化量**（两档偏移都逐张自洽），这个偏移本身的来源不属于本账。

| id | 卡名 | 变化字段 | 旧值→新值 | 归因 | 最近改动 / 数据源 |
|---|---|---|---|---|---|
| `AM1` | Lord Solar Leontus | `changesFull`（督军格） | WARLORD 2/2/0/38 → 2/2/0/28（数据 health 40→30） | γ | `4995622` · `cards_truth.json`←`allcards_1350/cards_raw.json`（maxHealth） |
| `AM2` | Ursula Creed | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `AM3` | Commissar Denkler | 同上 | 2/2/0/28 → 2/2/0/23（30→25） | γ | 同上 |
| `AM66` | Hektor Thenmann | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `ASH1` | Anvirr Keltoc | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `ASH2` | Eliac Zephyrblade | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `ASH3` | Medreyal Ghaelyn | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `ASH74` | Jain Zar | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `BL34` | Abaddon the Despoiler | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `BL35` | Haarken Worldclaimer | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `BL36` | Sylar Hexscorn | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `BL65` | Ghallaron the Pious | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `DA1` | Azrael | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `DA2` | Belial | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `DA3` | Asmodai | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GOF27` | Boss Zagstruk | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GOF28` | Ghazghkull Thraka | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `GOF29` | Grukk Face-Rippa | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GOF75` | Warboss Gordrang | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GOF76` | Mozrog Skragbad | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上（另有 W28 的手牌总价行，见 3·D） |
| `GOF77` | Beastboss Morgrim | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GSC1` | Primus Saffa Rhiannor | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GSC2` | Magus Uthrel Naas | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GSC3` | Iconward Malak Vorenth | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `GSC66` | Lhaska Szenari | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `SAU35` | Imotekh the Stormlord | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `SAU36` | Nemesor Zahndrekh | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `SAU37` | Orikan the Diviner | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `SAU66` | Ramatekh the Cruel | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `SOR1` | Erika Luminas | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `SOR2` | Junith Eruita | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `SOR3` | Morvenn Vahl | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `SOR65` | Celestian Sacresant Aveline | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `TAU1` | Shadowsun | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `TAU2` | Aun'Va | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `TAU3` | Commander O'Maisos | 同上 | 2/2/0/35 → 2/2/0/25（35→25） | γ | 同上 |
| `TAU65` | War Shaper | 同上 | 2/2/0/28 → 2/2/0/18（30→20） | γ | 同上 |
| `TL34` | Tervigon | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `TL35` | Swarmlord | 同上 | 3/1/0/38 → 3/1/0/23（40→25） | γ | 同上（−15 是 SO 里的逐卡差异，不是全局增量） |
| `TL36` | Neurothrope | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `TL74` | Terror of Vardenghast | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ（＋α 叠加） | 同上；本波再加 `Talent: Alpha Warrior` |
| `UM46` | Marneus Calgar | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ | 同上 |
| `UM47` | Uriel Ventris | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `UM48` | Varro Tigurius | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `UM65` | Chaplain Letharius | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `UM76` | Lieutenant Titus | 同上 | 2/2/0/33 → 2/2/0/23（35→25） | γ | 同上 |
| `UM87` | Roboute Guilliman | 同上 | 2/2/0/38 → 2/2/0/28（40→30） | γ（＋α 叠加） | 同上；本波再加 `Talent: Author of the Codex` |

### 3·B ①-a 既有 60 —— 非督军 10 张（γ / δ）

| id | 卡名 | 变化字段 | 旧值→新值 | 归因 | 最近改动 / 数据源 |
|---|---|---|---|---|---|
| `BL71` | Havoc Champion | `changesFull` | 落场 `BL71 8/6/0/7` → `8/6/0/8`（health 7→8） | γ | `4995622` · SO `maxHealth`（⚠️ `R_督军10血成因.md` §5 明写「未单独定性」） |
| `SW13` | Wolf Guard Headtaker | `changesFull` | 落场 `SW13 3/2/0/4` → `3/2/0/3`（health 4→3） | γ | 同上（⚠️ 同上） |
| `TAU12` | Darkstrider | `changesFull` | 落场 `TAU12 3/4/0/3` → `2/4/0/3`（attack 3→2） | γ | `4995622` · SO `meleeAttack` |
| `453b0e8f…` | Infernal Gaze | `expectedPay`/`energyDelta`/`handCostDelta`/`changesFull` | cost 2→1（−2→−1） | γ | `4995622` · SO `manaCost` |
| `BL74` | Spreading Corruption | 同上 | cost 4→3 | γ | 同上 |
| `TAU63` | Tidewall Gunrig | `mech`/`expectedPay`/`energyDelta`/`handCostDelta`/`changes`/`changesFull` | cost 0→1 | γ | 同上 |
| `TL66` | Neurogaunt | `expectedPay`/`energyDelta`/`handCostDelta`/`changesFull` | cost 2→1；`handCostDelta` 2→1 | γ | 同上 |
| `UM32` | Tactical Insight | `mech`/`expectedPay`/`energyDelta`/`handCostDelta`/`changes`/`changesFull` | cost 0→1 | γ | 同上 |
| `a58e0591…` | Synaptic Imperative | `expectedPay`/`energyDelta`/`handCostDelta`/`changesFull` | cost 2→1 | γ | 同上 |
| `GOF48` | Rok Invasion | `changesFull` | 随机部署 5 张：`GOF13/GOF4/GOF78/GOF65/GOF12` → `GOF13/GOF78/GOF85/GOF65/GOF66` | **δ** | `579bcad`（W28）改 `GOF12 Ork Spanner→Spanner` ⇒ `CreatePool.SortByName` 的候选顺序移动 |

### 3·C 本波 12 张关键词（α · 未提交工作树）

> 判据 = PnP 成品卡图 ＋ `_还原效果文字.md` 双写转录 ＋ SO `CardTraits` 三票同向（`资料/普查产出_1018/W_卡牌线_数据侧2.md` §一·1/§二·2·1）。
> 落地文件 = `Unity/数据/游戏数据/cardface_fixes.json`（`keywords` ＋ `_manual_keywords` 各 12 条）；生成器重跑后进 `cards_engine.json`。

| id | 卡名 | 类型 | 红点状态 | `keywords` 变化 | 归因 |
|---|---|---|---|---|---|
| `SW2` | Njal Stormcaller | hero | 新增 | `[]` → `["Talent: High Rune Priest"]` | α |
| `SW3` | Ragnar Blackmane | hero | 新增 | `[]` → `["Talent: War Howl"]` | α |
| `SW27` | Wolf Guard Terminator Pack Leader | unit | 新增 | ＋`Slay: Gain +1 [Attack], +1 [Ranged] and +1 Health` | α（另有电池行） |
| `SW34` | Murderfang | unit | 新增 | ＋`Slay: Heal 2 and gain Blood Thirst this turn` | α（另有电池行） |
| `SW37` | Wulfen Dreadnought | unit | 新增 | ＋`Rally: Give +2 [attack] to all friendly units with Pack` | α（另有电池行） |
| `UM12` | Antaro Chronus | unit | 新增 | `[]` → `["Talent: Spear of Macragge"]` | α |
| `UM100` | Chaplain Cassius | unit | 新增 | `[]` → `["Talent: Catechism of Death"]` | α |
| `UM91` | Tactical Marine | unit | 新增 | `[]` → `["Oath"]` | α（另有电池行） |
| `UM95` | Attack Bike | unit | 新增 | `[]` → `["Oath"]` | α（另有电池行） |
| `UM96` | Chapter Champion | unit | 新增 | `["Armour 1","Codex"]` → ＋`Oath` | α（另有电池行） |
| `TL74` | Terror of Vardenghast | hero | 已在 60 内 | `["Slay"]` → ＋`Talent: Alpha Warrior` | α（叠加在 γ 血量行上） |
| `UM87` | Roboute Guilliman | hero | 已在 60 内 | `["Codex"]` → ＋`Talent: Author of the Codex` | α（叠加在 γ 血量行上） |

### 3·D ①-a / ①-b 的 W28 改名连带（γ · `579bcad`）

| id | 卡名 | 变化字段 | 旧值→新值 | 归因 | 最近改动 / 数据源 |
|---|---|---|---|---|---|
| `EC55` | Lucius The Eternal | `other`/`changes`/`changesFull` | 1→2；新增 `💰 手牌总价 P0 4 → 6（2）` | γ | `579bcad`（W28）把 `Duelist's Hubris (Lucius' Talent)` → `Duelist's Hubris` ⇒ 天赋 token 按名命中、进手牌 |
| `EC56` | Lord Kaphrael | 同上 | 1→2；`4 → 6（2）` | γ | 同上（`Euphoric Strike`） |
| `EC57` | Xarahan, Daemon Prince | 同上 | 1→2；`5 → 7（2）` | γ | 同上（`Excessive Vigour`） |
| `GOF76` | Mozrog Skragbad | 同上（叠加在血量行上） | 1→2；`5 → 7（2）` | γ | 同上（`Da Bigger Dey Iz...`）＋ `4995622` 血量 |
| `E\|GOF92` | Weirdboy | `raw`/`net`/`netFull` | 24/0/空 → 25/1/＋`Waaagh! Energy` | γ | 同上（`Waaagh! Energy (Weirdboy Talent)` → `Waaagh! Energy`） |
| `E\|GOF94` | Painboss | 同上 | 25/0/空 → 26/3/＋`Dok's Toolz` | γ | 同上（`Dok's Toolz (Painboss' talent)` → `Dok's Toolz`） |
| `E\|TL25` | Hive Tyrant | `netFull` | 手牌总价 `2→6(+4)/6→7(+1)` → `2→5(+3)/5→6(+1)` | γ | `4995622`（同 `a58e…` 费用 2→1） |
| `E\|TL66` | Neurogaunt | `raw`/`ctrl` | 22/22 → 21/21 | γ | `4995622`（自身费用 2→1） |

**A/B 证据（可复跑）**：只把上面 6 张天赋卡名改回旧写法（池副本在 `_tmp_view/diag_a2137/pool_tokens_oldnames.json`，用 `WFPROBE_POOL=` 喂给探针）跑 `play all pf`，与当前指纹逐行比 = **只有这 6 行变化**；其中 `EC55`/`EC56`/`EC57`/`E|GOF92`/`E|GOF94` 的指纹行与**基线逐字节相同**（`GOF76` 只差血量那一段 —— 那是 `4995622` 的 γ，与 W28 无关，它的「手牌总价」行已消失）。

---

## §四 基线与常量历史（git log）

### 4·1 基线文件

| 项 | 值 |
|---|---|
| 路径 | `d:/4/Unity/工具/ruleprobe/基线/out_play_baseline.txt`（469,762 B；1713 非 `#` 行 = 主档 1127 ＋ 电池 586） |
| 生成方式 | `bash Unity/工具/ruleprobe.sh play all pf <out.txt>`（`PlayProbe.FingerprintFileText`，`PlayProbe.cs:2060-2080`） |
| md5 口径 | **丢掉 `#` 开头行 → 按整行 ordinal 排序 → `\n` 连接 → UTF-8 md5**（`FingerprintMd5`，`PlayProbe.cs:2050-2056`） |
| 缺省路径解析 | `DefaultPlayBaseline()`（`PlayProbe.cs:1903-1919`）：从程序集目录往上找第一条 `基线/out_play_baseline.txt` |
| 现行 md5 | **`2754c07fac4227283235b3e9988fd8ad`**（工作树 = HEAD，`git status` 干净） |

### 4·2 `PinnedPlayMd5` 与基线逐次对账

| 提交 | 日期 | `PinnedPlayMd5` | 基线 md5（按上行口径现算） | 配平？ |
|---|---|---|---|---|
| `ac5a952`（基线**新增**，第二十一会话 `E5`） | 2026-10-12 | `7175ec95…` | `7175ec95…`（1126 行，只有主档） | ✅ |
| `db1a834`（第二十三会话 · 内容重钉，仍是主档 1126 行） | 2026-10-12 | `83655a10…` | `83655a10…` | ✅ |
| `be31b7e`（第二十四会话 · **事件电池**，1126→1712 行） | 2026-10-13 | `d1fe0d96…` | `d1fe0d96…` | ✅ |
| `843e78a` / `1c3d27a` | 2026-10-14 | `d1fe0d96…`（未变） | `d1fe0d96…` | ✅ |
| `70f4147`（第三十会话 · `A1943` 夹具起始派系资源） | 2026-10-14 | `76ced8a6…` | `76ced8a6…` | ✅ |
| **`3251df0`（第五十会话 · K06 池 id 全真值）** | **2026-10-18** | **`76ced8a6…`（没改）** | **`2754c07f…`（重生成，519/518 行）** | **🔴 不配平** |

**根因**：`K06_池id迁移_报告.md` §3·4 原文只写了「`out_baseline.txt` 与 `out_play_baseline.txt` 重生成；**`Program.cs` 三条指纹重钉**（`PinnedPoolMd5`/`PinnedBaselineMd5`/`PinnedParseMd5`）」——`PlayProbe.PinnedPlayMd5` **不在那三条里**，也没有任何一处记录说「故意不钉」。
K06 那一次重生成的**正当理由**是 114 张池 id 重编号（指纹行以 id 为键，不重生成会变成 114 增 + 114 消）；但按 `PlayProbe` 自己的纪律，重生成必须**同时**手改常量（`PlayProbe.cs:1823-1826` / `README.md:845-852`）。

⚠️ **另外记一条历史缺口（不属于本次 70/10，但重钉前应当知道）**：`70f4147 → 3251df0` 之间基线变了 **160 行主档 ＋ 86 行电池**（含 2026-10-15~18 的多批 `keywords`/`desc` 改动），K06 报告里的验证写的是「按卡名对齐后 0 处内容差异（1,118 个名字）」——那验证的是 **`scan`/`check` 的解析档**，**不是 `play` 的结算档**。本次已核：`3251df0` 的基线对**它自己的卡池**可逐字节复现（`2754c07…`），所以**内容没有坏**；但「那 160/86 行是否逐张对过卡面」在文档里**找不到记录**。

### 4·3 K06 之后到现在的两次数据变化（本次 70/10 的来源）

| 提交 | 日期 | 改了什么 | 对指纹的作用 |
|---|---|---|---|
| `4995622` | 2026-10-18 20:10 | `gen_cards_engine.py` 新增「SO 数值真值覆盖层」：`health←maxHealth` · `cost←manaCost`（仅 ≥0）· `attack←meleeAttack`，共 **56 处**（`W_SO数值覆盖.md`） | 主档 56 行（49 health ＋ 6 cost ＋ 1 attack）＋ 电池 2 行（`TL25`/`TL66`） |
| `579bcad` | 2026-10-18 21:04 | W28 **64 张卡名改官方显示名**（只动 `name`/`nameZh`） | 主档 4 行（3 个督军手牌总价 ＋ `GOF48`）＋ 电池 2 行（`GOF92`/`GOF94`） |
| 未提交（本波 `W_卡牌线_数据侧2`） | 2026-10-18 晚 | `cardface_fixes.json` 给 12 张补 `keywords`/`_manual_keywords`（三源核过） | 主档 12 行（10 新增红点）＋ 电池 6 行 |

---

## §五 建议动作（按顺序）

### 5·0 前置（缺一不可）

1. **等工作树收口**：现在还有别的线在飞（`RuleEngine/Core/CardDef.cs`、`CreatePool.cs` 是注释级；`DeckRules.cs` 有并发行为改动，虽会编进探针但**不在 `play` 的执行路径上**）。重生成基线会把**当时盘上的一切**烘进去 ⇒ 必须在「本波数据改动已定稿、并且决定要提交」之后做，**别在中间重生成**。
2. **确认两张单位血量**：`BL71` 7→8、`SW13` 4→3 来自 SO `maxHealth`，但 `R_督军10血成因.md` §5 明写「两张单位血量变化不属于督军平衡，**未单独定性**」。重钉前至少对着当前 SO 行（`cards_raw.json`）与 PnP 卡图各看一眼，把结论写进重钉理由。
3. **确认本波 12 张关键词**：`W_卡牌线_数据侧2.md` 已做 PnP ＋ 转录 ＋ SO `CardTraits` 三票；重钉前只需主对话签字（不需要再查）。

### 5·1（可选，但建议先做）修 δ 的排序键

把 `CreatePool.SortByName`（`CreatePool.cs:1210-1215`）的排序键从 `a.Name` 改成 `a.Id`（或 `CardShortId`）。理由：
- id 现在是**真 `uniqueId`**（K06 起池内 1127/1127，自造 0）⇒ 稳定、与显示名解耦；
- 否则**每一次改卡名**都会重排随机候选池，`GOF48` 这类卡会以「看起来像结算漂移」的方式变红；
- ⚠️ 改了它，**所有按池随机**的卡的确定性结果都会变一次（同一批重生成一起吸收）；如果不想动引擎，就把它记成一条**新账**，并接受「下次改名还会红 1~N 行随机卡」。

同类但**不建议本轮动**的是 `SpawnTalents` 按显示名找天赋卡（`RuleCore.cs:8666`）—— 换成 id 需要一张「天赋名 → 卡 id」表（`talent_cards.json` 已有 89 条，可作后续账），本轮只记录。

### 5·2 重生成基线

先在仓库根目录跑（**绝对路径**；`ruleprobe.sh` 会把参数原样转发、并按调用者目录解析相对路径）：

```bash
bash d:/4/Unity/工具/ruleprobe.sh play all pf D:/4/Unity/工具/ruleprobe/基线/out_play_baseline.txt
```

跑完确认两件事：
1. 日志里 `--- ⑤ 结算指纹 ---` 的「**合起来**」那一串 = 新基线 md5；
2. 用 `play check` 再跑一次，①-a/①-b 应为**全等**（0 张变化），只剩 ② 哨兵红 —— 那是预期的，因为常量还没改。

### 5·3 重钉常量（手改源码）

把 `d:/4/Unity/工具/ruleprobe/PlayProbe.cs:1900` 改成新基线 md5：

```csharp
public const string PinnedPlayMd5 = "<上一步打出来的 32 位 md5>";
```

⚠️ **用 Edit 工具改，⛔ 别用 `sed -i`**（会把 CRLF 翻成 LF，见 `CLAUDE.md` §二）。并在常量上方的注释块里补一段「**有意重钉**」的理由（格式照现有 `2026-10-14` 那段）：日期 · 归因（`4995622` SO 56 处 / `579bcad` W28 改名连带 / 本波 12 张关键词）· 证据（本报告 §一 §三 的 A/B）· 说明「**不是为抹红而重钉**」。

### 5·4 复跑验收

```bash
bash d:/4/Unity/工具/ruleprobe.sh play check
```

预期：`结算指纹：当次现算 = <新 md5>（一致）`、①-a ✅、①-b ✅、② ✓、`rc=0`。
（`play check` **不是** Unity 自检，不在 `_run_8_checks.sh` 里；它全池实测 **≈4 秒**（2026-10-18 23:30:04→08，另加 ~1.5 秒 `dotnet build`），可随时复核。）

### 5·5 影响面

| 引用点 | 说明 |
|---|---|
| `Unity/工具/ruleprobe/PlayProbe.cs` | `PinnedPlayMd5` 共 9 处（常量 1 ＋ 注释/打印 8），**唯一需要改的只有 `:1900`** |
| `Unity/工具/ruleprobe/基线/out_play_baseline.txt` | 重生成的产物本身；`git diff` 只应出现在「本次确认过的行」上 |
| 文档 | `ruleprobe/README.md:526`（口径）、`资料/任务线/引擎线/任务总表.md:78`、`资料/任务线/卡牌线/任务总表.md:28`（`A2137` 本账）—— 关账时改状态，不改数字 |
| 自动化 | **无**：`_run_8_checks.sh` 不跑它；仓库里只有 `_oneoff_1014/*` 的一次性脚本提到过 `play check`，不参与例行流程 |

---

## §六 没查清 / 明确不查的

1. **SO 包与将来实际运行的 exe 是否同 build**：`R_督军10血成因.md` §5 已列同名疑点，本次未再核（不影响「相对基线是否变化」的判定）。
2. **`BL71`/`SW13` 两张单位血量的逐卡理由**：`R_督军10血成因.md` §5 明写「未单独定性」；本次只确认它们确实来自 SO `maxHealth`，**没有**再对 PnP/SO 逐张开图核。
3. **`EC55/56/57/GOF76` 的「手牌总价 +2」与 token 费用不逐一对应**：token 费用分别是 1/1/2/2，但四张的手牌总价都 +2（`play` 单卡模式与 `play all` 模式的手牌基数也不同）。已用 A/B 证明**全部由 W28 改名引起**，但「+2 具体由几张 token、哪个时机加进去」没有逐行追（未影响归因）。
4. **K06 那一次基线重生成（`2754c07…`）是否逐张对过卡面**：文档里找不到记录（K06 报告只写了**解析档**「按卡名对齐 0 差异」）。本次只证明它**可复现、未损坏**，不能证明「内容已被确认」。
5. **是否还有其它随机类卡受按名排序影响**：本次 70 行里只有 `GOF48` 一行是这种形态；我没有穷举全池所有「按池随机」的卡在 W28 改名后是否恰好抽到同一组（只按行差找，行差里没有第二张）。
6. **`SpawnTalents` 的完整链路**：只追到 `RuleCore.cs:8666-8678` 的 `FindByName` 第一跳；失败后的 `PickTalentFromPool` 分支在旧名时代具体是「返回 null」还是「抽到别的卡」，没有逐张打日志核实（A/B 已够定性）。

---

## 附：本次只读产出（可复跑）

| 文件 | 内容 |
|---|---|
| `_tmp_view/diag_a2137/playcheck_now.txt` | 当前 `play check` 原始输出（rc=1） |
| `_tmp_view/diag_a2137/pf_now.txt` | 当前池的 `play all pf` 指纹（合集 `32b37953…`） |
| `_tmp_view/diag_a2137/pf_before_wave.txt` | 本波改动前快照池的指纹（合集 `f3afcdbe…`，与 `W_卡牌线_数据侧2.md` §一·9 一致） |
| `_tmp_view/diag_a2137/pool_3251df0.json` / `pool_4995622.json` / `pool_579bcad.json` | 三个提交时的卡池快照（`git show` 导出） |
| `_tmp_view/diag_a2137/pf_k06pool_nowcode.txt` | **当前代码 ＋ K06 卡池**的指纹（合集 = 基线 `2754c07…`，排除 β 的关键证据） |
| `_tmp_view/diag_a2137/pool_tokens_oldnames.json` / `pool_gof12_oldname.json` | 只回退 W28 卡名的对照池（A/B 用；`WFPROBE_POOL=` 喂给探针） |
