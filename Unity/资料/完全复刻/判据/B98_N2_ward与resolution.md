# B98_N2 · ward 四词 / resolution 徽标（只按原版）

## 〇 结论
- **甲 = 原版算「已实现」**：原版对 `ward`(140)/`wardWb`(145)/`tainted`(160)/`summonRift`(730) 四词**都有真闸**（见 §一证据链）；原版**没有** `Implemented` 这种清单，`Implemented` 是我们自己的概念。**该怎么做**：照原版把四词都做到「与原版同覆盖」——`ward/wardWb/tainted` 现已同覆盖可直接入表，`summonRift` **缺一处攻击侧闸**（先补再入表）。
- **乙 = 原版图标系统「认」resolution，但卡池 0 张带它 ⇒ 两端都永不显示**。我们**已经照做**（两套图标表都在 + 图在），无缺层、无需改动，**不是自设计**。

## 一 ward 四词（证据链：现读注释 → 原版处理 → 池内可达性）

### 1.1 现读注释（`RuleEngine/Core/CardDef.cs`）
- `:8613-8644` 四词已登记进 `Prefixes`（认得出、不静默丢弃），`:8632-8640` 明写「**不进 `Implemented`（如实）**」。
- `:7963-8060` `Implemented` 集合**现读不含** ward/wardwb/tainted/summonrift（亦不含 resolution）。
- 池内（`cards_engine.json`，count=**1129**，现读）：`keywords`/`cardTraits`/`desc`/`descZh` 四列命中——**ward 0 · wardwb 0 · tainted 0 · summonrift 0**。
  （名字列里 `ward` 命中 3 张纯属卡名子串 `Iconward Malak Vorenth`/`Royal Warden`/`Forward Deployment`，卡名不过 `Normalize`。）
  ⇒ **四词今天在池内全不可达**，入不入 `Implemented` 对运行**零影响**，只有合成夹具走得到。

### 1.2 原版处理（第一权威 = `d:/2/tools/decomp_full/`）
原版无 `Implemented` 概念，等价物 = **有没有执行分支**。逐词：
- **`ward`/`wardWb`**：`EntityScript__get_isCurrentlyWarded.c` = `HasCurrentTrait(0x8c=140) || HasCurrentTrait(0x91=145)`。该 getter 全量只被 **4 个目标校验函数**调用：
  `RawCardScript__IsValidTarget` · `BattleManager__IsValidSpellTarget` · `RawCardScript__IsValidActiveAbilityTarget` · `BattleManager__IsValidActiveAbilityTarget`。
  **攻击侧（`BattleManager__IsValidAttackTarget`）不查 ward**（现读 `grep isCurrentlyWarded/0x8c/0x91` = 0 命中）⇒ 原版 ward 只在**目标选择层**挡，且**只挡异侧**。
- **`tainted`(160)**：只在 `RawCardScript__IsValidActiveAbilityTarget`（主动能力）那一闸（`HasCurrentTrait(候选,0xa0)` + vanguard 例外 + 载体是 Hero）。
- **`summonRift`(730)**：**两处裸闸**——主动能力 `BattleManager__IsValidActiveAbilityTarget.c`（0x2da⇒return 0）**＋ 攻击** `BattleManager__IsValidAttackTarget.c:211-213`（`HasCurrentTrait(目标,0x2da)⇒return 0`，现读确认）。

### 1.3 我们的覆盖 vs 原版（关键）
| 词 | 原版闸所在层 | 我们的落点 | 是否同覆盖 |
|---|---|---|---|
| ward / wardWb | 通用目标 + 战术目标 + 主动能力（`isCurrentlyWarded`） | `EffectResolver.AddSide(stealthGate:true)`（`EffectResolver.cs:3010-3013`）＋ `RuleCore.CanUseAbilityTarget`（`RuleCore.cs:11417 IsCurrentlyWarded`） | **同覆盖** |
| tainted | 仅主动能力 | `RuleCore.cs:11433`（CanUseAbilityTarget ③） | **同覆盖** |
| summonRift | 主动能力 **＋ 攻击** | 仅 `RuleCore.cs:11421`（CanUseAbilityTarget ②）；**攻击侧 `RuleCore.IsValidTarget`(5402-5620) 无 SummonRift 检查** | **缺攻击侧** |

- 🔴 **确认缺口（summonRift 攻击侧）**：`CardDef.cs:6963` 自述「我们的落点 = `CanUseAbilityTarget` 的 ②」——只落了能力侧；原版 `IsValidAttackTarget.c:211-213` 那半**没接**。
- ⚠️ **残留待审计（ward 结算路径）**：`EffectResolver.AddSide` 只有选择侧 4 处传 `stealthGate:true`（`:2083/2085/2145/2147`）；**结算侧 `ResolveTargets` 的 `:1744/1745/1683` 不传**。若这些结算池未经选择侧预筛（如「全体/随机」类），ward 在此**挡不住** ⇒ 需逐动词核「是否都经选择侧过滤」（这正是注释里「其余动词若绕过 AddSide 就挡不住」那句的具体形状）。

## 二 resolution 徽标（证据链：图在不在 → 谁设置它 → 显隐条件）
- **图在**：`Assets/CardPresentation/Resources/Art/traits/resolution.png`（+`.meta`）存在。
- **谁设置它**：`CardPresentation/Core/Badges.cs` 的 `Badges.For()`（`:570`）从单位关键词表挑徽标；`SpriteOf()`（`:367`）读 `Resources.LoadAll<Texture2D>("Art/traits")` 的**真名**→ 能解析出 `resolution`。
- **显隐条件（= 原版那 4 列）**：`trait_collection.json`（77 条，现读）里 `resolution` = `hasIcon=true · isInPlayIcon=true · traitIcon=true`。据此现算的两张表**都已含 resolution**：
  `InPlayIconKeys`（47 条，场上 7 枚徽标）`:419` · `CardIconKeys`（59 条，卡面/收藏/详情窗图标）`:448`。
  原版对应筛选器 = `CardTraitCollection__GetTraitInPlayListForIcons`（BoardTraitIcon）与 `__GetTraitListForCardIcons`（卡图标）。
- **可达性**：`cards_engine.json` 1129 张的 `keywords/cardTraits/desc/descZh` 里 **`resolution` 命中 0 张** ⇒ 两端都**永不实际显示**（图标系统认它，但没有卡带它）。
- ⇒ **原版有（认作可画），我们也已有，零缺层**。**不是自设计**——两表由 `trait_collection.json` 三列现算（`Badges.cs:436-451` 注释：数据变就重跑，⛔ 别凭印象加减）。

## 三 若照原版要改代码（甲：补 summonRift 攻击侧闸；乙：无）
1. **`RuleEngine/Core/RuleCore.cs` 的 `IsValidTarget`（`:5402-5620` 内）** —— 加一处攻击侧 summonRift 闸，镜像原版 `BattleManager__IsValidAttackTarget.c:211-213`：
   `if (target.Has(KeywordTable.SummonRift)) return RuleCodes.ErrTarget;`
   - 落点建议：紧跟 GiantKiller 闸之后（`:5611` 后、召唤病 `:5613` 前）；原版次序在 destroyer 之后、stunned 之前，池内 0 张 ⇒ 位置只影响「多闸同时命中时先报哪条」，**零行为变化**。
   - **零行数要求**：新增行会往下推同文件落点 ⇒ 若下方有账 A 行落在本区，**先把新检查并入既有 `if` 链或追加到方法尾**，改完核 `check_ledger_points` 无漂移（⚠️ 别插进别的成员之间）。
   - **断言**：合成夹具「攻方攻击带 summonRift 的目标 ⇒ `RuleCodes.ErrTarget`」；灭自证 = 同时断言能力侧仍拒（已有 `RuleEngineTest_EngineE7.E7Case2SummonRift`，勿削弱）。
2. **补完后再评估入表**：summonRift 补上后四词 = 原版同覆盖 ⇒ 逐个加入 `CardDef.Implemented`（`:7963` 集合），并在注释写判据「原版 `isCurrentlyWarded`／tainted／summonRift 双闸」。**但** ward 结算路径的 §1.3 残留项需先审清，否则入表=「声称完整但可能有漏洞」。
3. **乙：无代码改动**（`resolution` 已在两表、图在、可解析；0 张卡 ⇒ 如实保持现状）。

## 四 顺带发现
- `Implemented` 集合是**我们自建**的概念，原版无对应物（原版只靠「有没有执行分支」）⇒ 讨论「原版算不算已实现」必须在「执行分支」这一层比，⛔ 别拿我们的命名当判据。
- 四词 + resolution 今天**池内全 0 张**（`cards_engine.json` 1129 张现扫）⇒ 本批任何改动都是「将来写得出」的忠实性补强，**零行为变化**，别用「不可达」当不做理由（铁律 11）。
- `resolution` 另有**触发语义面**（`AbilityTrigger.Resolution=45→trait 840`，`CardScript__OnTurnEnd.c:81` 真发；`CardDef.cs:8863-8947` 已列为 ReachableCounterExample）—— 与「徽标」是两件事，徽标这层已齐。
