# B9_W65ABIL — 原版卡 `activeAbility` 的可执行入口：链路判定 + 代码侧补齐

> 执行代理 `B9_W65ABIL` · 2026-10-27 · 白名单 = `RuleEngine/Core/CardDef.cs` · `RuleEngine/Data/CardDatabase.cs`
> 上一路 `B9_W41S3B` 的报告 §六·3 立了这一条（「原版卡的 `activeAbility` 在本引擎里没有可执行入口」）。
> 本片把那条**查透并就地纠偏**：它描述的**症状全都成立**（`abil=14` 56/56 张督军），但**结论「缺一条入口」只对 1 张卡成立**。

---

## 一 结论

### 1.1 三跳 × 落地位置（本轮的核心表）

| # | 原版这一跳（第一权威 = `d:/2/tools/decomp_full/`） | 我们这一侧的落地位置 | 状态 |
|---|---|---|---|
| ① **数据** | `RawCardScript.activeAbility(+0x260)` / `alternativeAbility(+0x2B8)` 的 `activeAbilityLogic(+0x48)`（`RawCardScript__ResolveActiveAbility.c:16-38` · `RawCardScript__GetActiveAbility.c:5-16`） | `CardDef.ActiveAbilityLogic`（`CardDatabase.MapActiveAbilityLogic`，卡表 **v23+**）· 分组 `ActiveAbilityLogicGroup(ab)` · 逐行规格 `ActiveAbilityLogicSpec` | ✅ 在 |
| ①' **选支** | `hasAlternativeAbility(+0x2AA)` + `BoardAnalysis.MeetsCriteria(normalAbilityBoardCriteria +0x2B0)` ⇒ 取 `+0x260` 还是 `+0x2B8` | `CardDef.HasAlternativeAbility` / `CardDef.UsesAlternativeAbility` | ✅ 在 |
| ①'' **对象标量** | `ActiveAbility` 自己的 `manaCost(+0x14)` / `psychicCost` / `plasmaCost` / `targeted` | **本轮新增** `CardDef.ActiveAbilityManaCost` / `…PsychicCost` / `…PlasmaCost` / `…Targeted`（`Core/CardDefActiveAbility.cs`）+ `CardDatabase` 的 DTO 四格与挂载调用 | 🆕 **代码侧已落**，数据列待生成器（v35） |
| ② **正文（谁执行）** | `RawCardScript__ResolveActiveAbility.c:58-67`：**逐行** `AbilityLogic__PlayAbility(logic, bm, abilityData, 400 /* ThisUnitAbilityUsed */, 0)` | **两条等价实现**：替代行动 → `TriggerOps(duty/pray/ferocity/agenda)`（`RuleCore.UseAlternativeCore`）；誓约 → `OathOps`（`RuleCore.UseOathAbilityCore`）；我们自定的 `Ability:` → `CardDef.Ability`（`RuleCore.UseAbilityCore`） | ✅ 86/87 在；**1 张没有** |
| ③ **入口闸** | `CardScript.hasActiveAbility(+0x220)`：**为 0 ⇒ `LogWarning` + return，整条路径不跑**（`CardScript__ResolveActiveAbilityPlayed.c:26-31`）；`+0x220` 由卡数据 `+0x2A9` 灌，卡型 ≠ Hero 时压成 false（`CardScript.cs:20085-20089`） | 数据侧 `CardDef.HasActiveAbility`（`ConditionEval` 190/280 已在读）· 按钮侧 `BattleDriver.OpenCommand` 的 `u.HasAbility \|\| alt \|\| oath` | ✅ 在（判据一致） |

**⇒ 缺的那一跳不是「替代行动」，也不是「`CardDef.Ability` 恒空」** ——
`CardDef.Ability` 是**我们自定 `EffectSpec` 封闭文法**那一格（`CardDef.cs:1423` 那句 `if (!FromOriginalPool)`），
原版卡 `desc` 里 `Ability:` 现读 **0 张**，它**本来就不该**承载原版卡，⛔ 不是缺口的载体。
真正缺的是**能力对象自己的四个标量**（没有它就没法按原版收费/判「要不要点目标」），
以及**「把 `activeAbilityLogic` 的行直接当效果执行」那台执行器**（大工程，见 §四·R3）。

### 1.2 全池口径：87 张 `hasActiveAbility=1`，86 张今天已能发，只有 1 张没有入口

数据侧自洽（现读 `cards_engine.json` v34）：

- `hasActiveAbility == 1` 的卡 = **87 张**；
- 带 `gate=1` 的 `activeAbilityLogic` 行的卡 = **87 张**；
- **两者逐张相等，双向 0 例外**（`gate` 的语义 = `hasActiveAbility`，由 `工具/gen_cards_engine.py` 的 `branch_gate()` 算出）。

引擎侧实测（`ruleprobe`，逐张）：**86 张今天已经能发出主动能力**，唯一的例外是 `SOR66`：

| 分类 | 张数 | 入口 | 读数证据（`ruleprobe`） |
|---|---|---|---|
| 督军 · 替代行动 | **11** | `RuleCore.UseAlternative` | `H\|` 行 `alt(duty)=0` ×4（`AM1 AM2 AM3 AM66`）· `alt(pray)=0` ×4（`SOR1 SOR2 SOR3 SOR65`）· `alt(agenda)=0` ×3（`DA1 DA3 DA75`） |
| 部队 · 替代行动 | **57** | 同上 | `play <id>` 电池步 `alt(duty)=0` ×19 · `alt(pray)=0` ×19 · `alt(agenda)=0` ×10 · `alt(ferocity)=0` ×9 |
| 部队 · 誓约 | **18** | `RuleCore.UseOathAbility` | `oath=0`（`UM89 UM90 UM91 UM92 UM93 UM94 UM95 UM96 UM97 UM98 UM99 UM101 UM102 UM103 UM104 UM105 UM106 UM108`） |
| **部队 · 没有任何入口** | **1** | ❌ | `SOR66 Armorium Cherub`：`abil=14` + `oath=14`，且 `alt` 步**根本没出现** |
| 合计 | **87** | | |

### 1.3 🔴 就地纠偏：「45/56 张督军发不出主动能力」**不是缺口**（铁律 5 / 铁律 11 例外①）

上一路写的是「现读只有 11/56 张督军带替代行动关键词能走到（其余 45 张今天发不出主动能力）」。
**前半句对，后半句的语义判断错**：那 45 张的**原版真值 `hasActiveAbility == 0`** ⇒
原版自己 `ResolveActiveAbilityPlayed.c:26-31` 也是 `LogWarning` + return，**原版同样不给它们主动能力**。

证据（三条独立）：

1. `cards_truth.json` 逐张：56 张督军里 **45 张 `hasActiveAbility = 0`**、11 张 `= 1`
   （40 张按卡名直接对上；8 张卡名有空格/标点差异，其中 `AM66/DA3/DA1` 卡表侧是 1，`DA2/EC55/EC56/EC57/GOF28` 是 0 —— 合起来仍是 45 / 11）。
2. 那 45 张的卡面 `desc` **只有 `Talent:`（被动触发）**，没有第二句正文：
   例 `ASH1 Anvirr Keltoc` = `Talent: Path of the Seer` · `BL34 Abaddon` = `Talent: Chosen of the Four` · `TAU1 Shadowsun` = `Talent: Hero of the Empire`。
3. 它们的 `activeAbilityLogic` 行**全是 `gate = 0`**（生成器现读：133 行里 45 行 `gate=0`）—— 那批是**本 build 的死数据**，
   落列是**故意的**（`gen_cards_engine.py` 的 `gate` 注释 + `资料/普查产出_1024/D11_activeAbility列.md` §2：换个 build 翻上去时行数据还得在）。

⇒ 所以 `useAbility` 对它们返回 `ErrNoAbility(14)` **是对的**。⛔ **别为了让 56 张都能发去给 `CardDef.Ability` 造假数据**
（那会让 45 张在原版里没有的按钮出现在真游戏里，且 `abil` 会去结算一段并不存在的正文）。

### 1.4 唯一真缺口：`SOR66 Armorium Cherub`

- `hasActiveAbility = 1`（真值 `cards_truth.json`），`activeAbility = {manaCost:0, targeted:1, targetCriteria:{ta:10, friendlyUnits:1, includeSelf:1}, logic:[ca=30 buffOthers ta=30, ca=462 triggerPray ta=20]}`；
- 卡面（PnP 实拍 `d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_03_Armorium-Cherub.png`）印的是
  `Flying. Rally: Give +2 [Ranged] to a friendly unit this turn. At the start of your turn, gain 1 ☀ and return to your hand`；
- 它**既没有替代行动关键词、也没有 `Oath`**，且 `CardDef.Ability == null`
  ⇒ `BattleDriver.OpenCommand` 的 `skill` 三项全假 ⇒ **技能钮永不出现**，`UseAbility` 恒 `ErrNoAbility(14)`。
- ⚠️ **修它之前必须先查清这张卡的技能钮在原版到底做什么**（见 §三·1：它的 `activeAbilityLogic` 里有一行 `ca=462 triggerPray`，
  而它并不是一张 `Pray` 卡 —— 这一行可疑，⛔ 别照着实现）。

### 1.5 本轮落了什么代码（零行为变化）

补上了 **①'' 那一跳的代码侧**（`ActiveAbility` 对象的四个标量），并把「数据一旦有就能工作」的通道全部打通：

- `CardDef.ActiveAbilityManaCost` / `ActiveAbilityPsychicCost` / `ActiveAbilityPlasmaCost` / `ActiveAbilityTargeted`
  + `ActiveAbilityCostKnown` + 挂载口 `AttachActiveAbilityCost(...)`（新文件 `Core/CardDefActiveAbility.cs`）；
- `CardDatabase`：`CardDto` 四格（缺键初值 `-1`）+ `Parse` 的「构造后挂载」块里一行 `def.AttachActiveAbilityCost(...)`。

今天卡表 v34 没有这四列 ⇒ 一律 `-1`（未知）⇒ **对局、解析、结算全零变化**（读数见 §二·3）。
它解的是 `RuleEngine/Data/AiScoring.cs:1107-1121` 那条**仓库自己写下的缺口**
（`ActiveAbilityManaCost` 钩子的 doc：原话「那份数据列还没落进 `cards_engine.json`……
要经 `工具/gen_cards_engine.py` + `Core/CardDef.cs` 两处改动，均不在本代理白名单」）——**`Core/CardDef.cs` 这一半本片补齐了**。

---

## 二 证据（文件:行号 / 资产路径 + 字段）

### 2.1 原版链路（逐句读过）

| 判据 | 位置 | 说了什么 |
|---|---|---|
| 入口闸 | `decomp_full/CardScript__ResolveActiveAbilityPlayed.c:26-31` | `if (*(char *)(param_1 + 0x220) == '\0') { LogWarning(卡名); return; }` —— **`hasActiveAbility == 0` ⇒ 整条主动能力路径不跑** |
| 选支 | `decomp_full/RawCardScript__ResolveActiveAbility.c:16-38` | `+0x2AA`（`hasAlternativeAbility`）为假 ⇒ 取 `+0x260`（`activeAbility`）；为真则 `BoardAnalysis__MeetsCriteria(bm, +0x2B0, …)` 真 ⇒ `+0x260`、假 ⇒ `+0x2B8`（`alternativeAbility`） |
| 正文 | `decomp_full/RawCardScript__ResolveActiveAbility.c:58-67` | `lVar6 = *(lVar6 + 0x48)`（= `activeAbilityLogic` 列表）→ `while` 逐行 `AbilityLogic__PlayAbility(lVar4, bm, uVar1, 400, 0)` |
| 费用闸 | `decomp_full/BattleManager__CanUseActiveAbility.c:60-70` | `PlayerManager__HasEnoughMana(玩家, 匿名对象, AbilityUsesPsychicEnergy(card), AbilityUsesPlasmaEnergy(card), …)` —— **需要 `ActiveAbility` 的费用标量**；不足则按 `param_4 & param_3` 决定要不要 `NotifyCantDoAction` |
| 卡型压位 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardScript.cs:20085-20089` | `if (cardType != Hero) hasActiveAbility = false;`（督军除外） |
| 对象字段 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/ActiveAbility.cs` | `int minHeroLevel` · `int manaCost` · `bool targeted` · `bool psychicCost` · `bool plasmaCost` · `bool needsLoading` · `TargetCriteria targetCriteria` · `List<AbilityLogic> activeAbilityLogic` |

### 2.2 数据侧（现读，`RuleEngine/Resources/*.json`）

| 读数 | 值 | 出处 |
|---|---|---|
| `type == "hero"` 的卡 | **56** 张 | `cards_engine.json` |
| `hasActiveAbility == 1` | **87** 张（hero 11 / unit 76） | 同上 |
| 带 `gate=1` 行的卡 | **87** 张（与上一条**逐张一致**） | 同上 |
| `activeAbilityLogic` 行数 | **133** 行（`gate=1` 88 / `gate=0` 45） | 同上 |
| 督军 `hasActiveAbility` 分布 | 11 张 = 1 / 45 张 = 0 | `cards_engine.json` + `cards_truth.json` 逐张交叉（0 处不一致） |
| `SOR66` 的 `activeAbility` | `manaCost 0` · `targeted 1` · 2 行 logic（`ca=30 ta=30` / `ca=462 ta=20`） | `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `cards[uniqueId=SOR66].activeAbility`（**原始 SO dump**） |

> ⚠️ **两个「真值」文件别混**（本片踩过一次）：
> `Resources/cards_truth.json` 是**缩编**版（每卡一份、无 `full_raw`，`activeAbility` 里的列表键叫 **`logic`**，现读 143 张有对象、**0 张带行**）；
> 而 `工具/gen_cards_engine.py` 读的是 **`资料/普查产出_1018/allcards_1350/cards_raw.json`**（原始 SO dump，键叫 `activeAbilityLogic`）。
> ⇒ 要「某张卡 `activeAbilityLogic` 的真值」只能读**后者**；前者只作旁证。

### 2.3 我们的落点 + 回归读数

| 项 | 值 |
|---|---|
| 新增代码 | `RuleEngine/Core/CardDefActiveAbility.cs`（新文件，`CardDef` 的 partial 半边：`:71` `ActiveAbilityManaCost` · `:100` `AttachActiveAbilityCost`）<br>`RuleEngine/Data/CardDatabase.cs:633`（DTO 四格）· `:1690`（构造后挂载调用）<br>`RuleEngine/Core/CardDef.cs:48`（`public class` → `public partial class`，**同行改写、零行号变化**） |
| 为什么 `CardDef` 走新文件 | `CardDef.cs` 的 `KeywordTable` 里还有 1 处账落点按行号引用（`CardDef.cs:7864`，B 行）⇒ 类尾插成员会漂移；`Core/*.cs` 是**通配**编进 `ruleprobe.csproj` 的，新文件自动带上 |
| 为什么 `CardDatabase` 只能原地改 | `ruleprobe.csproj` 把它**显式列出**（`<Compile Include="../../MyGame/Assets/RuleEngine/Data/CardDatabase.cs" />`）⇒ 拆 partial 新文件会让探针构建失败 |
| `TMPDIR=/tmp/wf_w65 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `bash 工具/ruleprobe.sh check` | **rc=0** · 「全池 1121 张 · 解析差异 0 行 · 改名 0 · 新增 0 · 消失 0」· 输出指纹 **`c9a64798…`（与本片改前逐字相同）** |
| `bash 工具/ruleprobe.sh play check` | **rc=1（预期）** · ①-a 差异 = **只有 `BL64` + `DA42`（别人的两张）** · ①-b 电池 **586/586 逐字节一致** · ② 哨兵 ✓ ⇒ **没有第三张** |
| `python -I -X utf8 工具/check_ledger_points.py` | 我的两个文件**零新增漂移**（`CardDatabase.cs` 的 5 处行号引用全在 B/C 行的备注列、不在 A 档落点列；`CardDef.cs` 只有 1 处 B 行 `:7864`，本片没有改动它的行数） |

### 2.4 56 张督军 `abil` 读数（改前 / 改后 **逐行一致**）

量法：`ruleprobe play all pf <out> preset hero=1`（督军档电池：被测卡当 P0 督军 → 打天赋 → 电池步 → 主动能力），
读 `H|` 行的 `steps` 列（56 行）。改前 / 改后各跑一次，**56 行逐字节相同**。

| 组 | 张数 | `abil` 读数 | 替代行动读数 | 卡 id |
|---|---|---|---|---|
| A 替代行动 = duty | 4 | `abil=14`（×2 步） | `alt(duty)=0` | `AM1` `AM2` `AM3` `AM66` |
| A 替代行动 = pray | 4 | `abil=14`（×2） | `alt(pray)=0` | `SOR1` `SOR2` `SOR3` `SOR65` |
| A 替代行动 = agenda | 3 | `abil=14`（×2） | `alt(agenda)=0` | `DA1` `DA3` `DA75` |
| B 原版就没有主动能力（`hasActiveAbility=0`） | **45** | `abil=14`（×2） | **无 alt 步**（正确） | `ASH1 ASH2 ASH3 ASH74 ASH76` · `BL34 BL35 BL36 BL65` · `DA2` · `EC55 EC56 EC57` · `GOF27 GOF28 GOF29 GOF75 GOF76 GOF77` · `GSC1 GSC2 GSC3 GSC66` · `SAU35 SAU36 SAU37 SAU66` · `SW1 SW2 SW3` · `TAU1 TAU2 TAU3 TAU65` · `TL34 TL35 TL36 TL74` · `UM46 UM47 UM48 UM65 UM76 UM87 UM88` |
| 合计 | 56 | **`abil=14` 56/56（改前 = 改后）** | `alt(…)=0` 11/56 | |

**统计口径的变化 = 0**，且**这是本片要的结果**：`abil`（`RuleCore.UseAbility` = 我们自定 `Ability:` 那一档）对原版卡
本来就该是 14；原版那张「一个按钮」在我们这边由 `alt=` / `oath=` 两条读数代言（11/56 张督军 + 18 张部队）。
⚠️ 如果要的是「`abil` 对 11 张督军变成 0」，那是**错的实现**：那会让同一张卡在一趟电池里被结算两次
（`abil` 先花掉行动 → `alt` 紧接着 `ErrExhausted`），并且会改 `play` 指纹里 82 张部队卡的 `energyDelta` / `changes`
（本片被明令禁止：`play check` 只许 `BL64`+`DA42`）。

### 2.5 87 张的逐类读数（引擎侧，`ruleprobe play <id>` 的电池 `steps`）

```
督军 11  = alt: duty 4 / pray 4 / agenda 3          （H| 行，见 §2.4）
部队 57  = alt: duty 19 / pray 19 / agenda 10 / ferocity 9
部队 18  = oath=0
部队  1  = 无（SOR66：abil=14 · oath=14 · 无 alt 步）
```

---

## 三 没查清的部分

1. 🔴 **`SOR66` 的技能钮在原版到底做什么（未查清）**。**原始 SO dump**（`资料/普查产出_1018/allcards_1350/cards_raw.json`
   的 `cards[uniqueId=SOR66].activeAbility`）里 `activeAbilityLogic` 实打实是两行
   `[chosenAbility 30 buffOthers ta=30 fm=1, chosenAbility 462 triggerPray ta=20 fm=1]`，
   而它**不是** `Pray` 卡；卡面（PnP 实拍）也**没有** `Ability:` 那类文案，只有 `Rally:` 与 `At the start of your turn…`。
   ⚠️ 所以**不是我们提取错了**（原始 SO 就这么写），是 SO 本身可疑：最可能是作者把 `cardAbilities[0]`（Rally 那段）
   **复用**进了 `activeAbility`（那么按钮 = 每回合再叠一次 `+2 ⚔远程`），但 `ca=462 triggerPray` 这一行还解释不通。
   **要查清需要**：跑原版实况进战斗点这张卡的技能钮（受 `资料/真Play待验清单.md` 的窗口限制）。
   ⛔ 在那之前**别实现**它的效果（尤其别照 `ca=462` 那行）。
2. **「`activeAbilityLogic` → 效果」那台执行器整体没做**（不是本片能做的）：原版是逐行按 `chosenAbility`
   分派到 `doDamage` / `buffOthers` / `summonUnit` / `recallToOwnerHand` / `stun` … 十几种 handler
   （`AbilityLogic__PlayAbility.c` 的 `switch (*(int *)(param_1 + 0x10))`）。
   我们现在是**用卡面正文**（`EffectText` → `TriggerOps`）代言这件事，所以「正文没印出来 / 印得含糊」的行就没有机制。
   本片没有量化「87 张里有多少行是正文代言不到的」（要逐行比 `chosenAbility` ↔ op 的 `Verb`，属另一件活）。
3. **`ActiveAbility.minHeroLevel` 没落**（`int`，`ActiveAbility.cs` 第一个字段）。今天**没有消费者、也没有判据说明它在 PvP 里生效**，
   所以本片没加（加了就是「没有消费端的死字段」）。若战役/英雄等级那条线要用，按同一形状补。
4. **`AbilityUsesPsychicEnergy` / `AbilityUsesPlasmaEnergy`**（`EntityScript` 上那两个方法）**没读方法体** ——
   它们决定 `HasEnoughMana` 的币种开关怎么从卡/能力上算。`ActiveAbility` 上那两个 `bool` 是**最近的来源**，但**不是**同一个方法；
   接费用闸之前要先把那两个方法体读了（本片只保证数据通到 `CardDef`）。

---

## 四 改动清单

### 4.1 本片已落（代码，零行为变化）

| 文件 | 改动 | 行号 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/CardDefActiveAbility.cs` | **新文件**：`CardDef` 的 partial 半边 —— `ActiveAbilityManaCost` / `ActiveAbilityPsychicCost` / `ActiveAbilityPlasmaCost` / `ActiveAbilityTargeted` / `ActiveAbilityCostKnown` / `AttachActiveAbilityCost(…)` + 文件头那份「三跳 × 落地位置」与 87/87 判定记录 | `:71` `:81` `:86` `:91` `:94` `:100` |
| `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs` | `public class CardDef` → `public partial class CardDef`（**同行改写，零行数变化**） | `:48` |
| `Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs` | `CardDto` 四格（缺键初值 `-1`）+ `Parse` 构造后挂载块一行 `def.AttachActiveAbilityCost(…)` | `:633` · `:1690` |

### 4.2 改动请求（不在本片白名单 / 需要调度台排期）

| # | 给谁 | 改什么 | 判据 / 为什么 |
|---|---|---|---|
| **R1** | 生成器 `工具/gen_cards_engine.py`（调度台在无写手窗口做，**会改池指纹**） | 卡表 **v35** 新增四列 `activeAbilityManaCost` / `activeAbilityPsychicCost` / `activeAbilityPlasmaCost` / `activeAbilityTargeted`，源 = **生成器自己的真值源** `D:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `cards[].activeAbility` 同名四格（`manaCost` 是 `int`、其余三个是 `bool` ⇒ 落 `0/1`；**`activeAbility` 不是 dict / 缺键 = `-1`**）。⚠️ **不许用「非 0 才落」**（`SOR66` 的 `manaCost` 真值就是 0）。⚠️ 别拿 `Resources/cards_truth.json` 那份**缩编**真值 —— 它的键叫 `logic`（不是 `activeAbilityLogic`），且现读 143 张有对象、**没有一张带行**，它只作旁证 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/ActiveAbility.cs` · `cards_raw.json` 的 `activeAbility`（SOR66 实读：`manaCost 0` / `targeted 1` / `psychicCost 0` / `plasmaCost 0` / 2 行 logic） |
| **R2** | `RuleEngine/Data/AiScoring.cs` 的写手（一行） | `AiScoring.ActiveAbilityManaCost = c => c == null ? -1 : c.ActiveAbilityManaCost;` | 那份 doc 自己写着「要经 `Core/CardDef.cs` 一处改动」—— 那一半已在 §4.1 落地；接上之后 `AbilityManaCostInRange` 的 ⑥⑦ 两段才会出非 0 |
| **R3** | 引擎线（`RuleCore.cs` / `EffectResolver.cs`，属 `W54`） | **主动能力执行器**：`activeAbilityLogic` 逐行按 `chosenAbility` 分派（原版 `AbilityLogic__PlayAbility.c` 的 switch），并把费用闸接上（`CardDef.ActiveAbilityManaCost` + `…PsychicCost` + `…PlasmaCost`，原版 `CanUseActiveAbility.c:60-70` 的 `HasEnoughManpower` 那一跳） | 这是 87 张卡的「真」实现路线；今天用卡面正文代言。⚠️ 一改就会动 `play` 指纹 ⇒ 必须排进**重钉窗口** |
| **R4** | `CardPresentation/Battle/BattleDriver.cs`（属 `W57`） | 唯一一张缺入口的卡：`SOR66`。**先查清 §三·1** 再决定是「技能钮 + 走哪条正文」还是「数据侧订正」 | `hasActiveAbility=1` 而 `HasAbility \|\| alt \|\| oath` 三项全假 ⇒ 按钮永不出现（`BattleDriver.cs` 的 `skill` 那三行） |

---

## 五 顺手发现

1. **`RuleCore.ActiveAbilityDamageEntries`（技能伤害预览）只认 alt / oath / `Ability:`** ——
   对 `SOR66` 这类「有 `hasActiveAbility` 却无正文入口」的卡，预览恒 0；与 R4 是同一个洞的**表现侧**一半。
2. **`ConditionEval.cs:302` 里挂着一个没填的标记**：`// 🔶 自己设计（非原版）：… · TODO(谁定的)｜…`
   —— 按 CLAUDE.md 铁律 18 的标记格式，第三样「谁定的 + 日期」写的是 `TODO(谁定的)`，**这条标记不完整**（`grep -n "TODO(谁定的)"` 全仓可复现）。
3. **`gate=0` 那 45 行是「已知死数据」，不是「没收到」** —— 我在核对时重新确认了 `gen_cards_engine.py` 的 `branch_gate()`
   与 `D11_activeAbility列.md` §2 的口径一致：**保留落列**是对的，⛔ 别为了「数据干净」把它们删掉（换 build 翻上去时行数据还得在）。
4. **`ruleprobe` 的督军档 `H|` 行不进 `play check` 基线**（`PlayProbe.cs:3925` 的注释明写）⇒
   任何只影响督军档的改动（例如本片 §2.4 那种读数变化）**不会**被 `play check` 看见 ——
   要拿督军档当回归门，必须显式 `preset hero=1` 自己 diff（本片就是这么做的）。
5. **`工具/ruleprobe/ruleprobe.csproj` 关掉了 `EnableDefaultCompileItems`** ⇒ `Data/` 下新增文件**不会**自动编进探针
   （`Core/*.cs` 才是通配）。谁要在 `Data/` 开新文件，必须同时改那个 csproj，否则构建期 `CS0246`。本片就是被这条约束逼成「`CardDef` 走新文件、`CardDatabase` 原地改」的。
6. 🔴 **一条既有的账失败落在本片白名单文件上**（**不是本片造成的**）：
   `python -I -X utf8 工具/check_ledger_points.py` 的「失败明细」里有
   `特效动画.tsv | CardEffect.IsEnchantment() | RuleEngine/Core/CardDef.cs:5876（Type=="enchantment"） | 注释行：`/// </summary>``
   —— 落点指到了一条注释行。本片改的是 `CardDef.cs:48`（同行改写），**没有移动任何行**，所以它改前就在。
   要修它得把那一行的落点重新按符号定位（`--fix` 修不了「落在注释行」这一档，见 CLAUDE.md 那条「`--fix` 只修行号漂移」）。
