# `ChooseValueOptions` 剩余档的「原版在哪算 / 我们写死在哪 / 差多少」

> 代理：B88 · 波 3 · 执行代理 `E2` · 日期：2026-10-27
> 上游：`账/战斗逻辑驱动.tsv` 行 77（`AbilityLogic.GetValueFromCriteria`）· `判据/ValueOptions异常值_判定.md`（异常值 8 / 17 已判）
> 本文件解决的是**另一件事**：那 46 行「超纲值」之外，**池内正常使用**的 9 档里，`0/80/90/202/204` 五档
> 「我们自己写死 / 靠解析期近似」的**误差面**是什么（`101` / `140` / `210` 三档已在波 2 / 波 3 对齐，见账 77）。
> 复现口径：`cards_truth.json`（1,351 张、每卡一份、无 `full_raw` 翻倍）递归扫**所有**以 `valueOptions` 结尾的字段。

---

## 〇 一句话结论

**5 档里 3 档（`0` / `202` / `204`）语义对齐（不需改码）；2 档（`80` / `90`）是【真差异】，要做，只是排在后面。**
⚠️ 两档的**判据精度只到「枚举名 + 字段」级**，不是逐句：`AbilityLogic.GetValueFromCriteria` 的 ILSpy 版把
switch 渲染成**间接跳转**（`obj + 4294967096L` + `Indirect jump` 注释，`AbilityLogic.cs:2067-2200` 一带），
`.c` 只读到入口与 min/max 夹取 ⇒ 逐档分支**读不到**。⛔ 别把下表当「逐句核过」。

---

## 一 逐档表（现读行数 / 卡数）

| 档 | 原版名 | 行数 / 卡数（现读） | 原版在哪算 | 我们写死在哪 | 差 |
|---|---:|---:|---|---|---|
| **0** | `Normal` | **3,476 行 / 1,351 张** | `GetValueFromCriteria` 的 `default` 支 ⇒ 取 `minValue`（字面量），过 min/max 夹取 | 解析期把字面量写进 `EffectOp.Amount`（`EffectText` 的各 `Re` 组）；结算直接读 | **0**（字面量 → 字面量） |
| **202** | `AllFriendlyMarkOfChaos` | **1 行 / 1 张**（`BL31`） | 名字即口径：**友方全体**身上的黑暗契约（`markOfChaos`）层数之和 | `EffectResolver.CountFor` 的 `CountScope == "darkpact"` 支：本方棋盘 `Σ KwValue(DarkPact)` | **0**（同为「契约层数求和」）。⚠️ 我们**不分神**（Khorne/Nurgle/…），原版也不分（`AllFriendly` 未带 `markOfChaosType`）⇒ 一致 |
| **204** | `FaithValue` | **3 行 / 2 张**（`SOR14` / `SOR37`） | 名字即口径：玩家**信仰**当前值 | `EffectText` 把 `equal to your Faith` 剥成 `AmountRef = "faith"` ⇒ `TryApplyAmountRef` 取 `ps.Faith` | **0**（现读验过：`SOR14` 信仰 **12** ⇒ Rally 打 **12** 点，`ruleprobe play SOR14`） |
| **80** | `MinPlusMaxTimesMultiplier` | **3 行 / 2 张**（`GSC35` / `GSC59`） | 名字即口径：`(min + max) × multiplier` 的形状（`GetValueFromCriteria` 的第三个参数 `multiplierFactor`） | 解析期把 `1-3` 这类区间写成 `Amount` / `AmountMax`；`EffectResolver.DoDeal` 用 `ctx.GetRandomInt(Amount, AmountMax+1)` **随机取一个** | 🔴 **真差异**：原版取 `(min+max)×倍率`（**确定值**），我们取**区间内随机一个** ⇒ 数值分布完全不同。⚠️ 这两张卡正好是「等于敌方手牌数」那两张（`AmountRef = "enemyhand"` 覆盖了数值）⇒ **今天可观察差 = 0**，但形状是错的 |
| **90** | `RandomizedOnResolution` | **87 行 / 63 张** | 🔴 原版**不是每次掷骰**：`MatchData` 开局把 **100 个整数**烧进 `randomNumbers`（`MatchData.cs:213-228`），结算按**下标**取（`GetRandomInt` `:541-573`） | **没有这一档**：我们走 `ctx.GetRandomInt(...)`（每调用掷一次） | 🔴 **真差异**：同一局内原版**完全确定**（但不是种子可复现）、我们每掷一次；分布与「同一次结算里多条 90 档是否取同一个数」都不同。87 行 / 63 张是**当前最大的一条** |

> 「行数/卡数」口径：`cards_truth.json` 递归扫所有 `*valueOptions` 与 `valueOptions` 字段。
> 与 `ValueOptions异常值_判定.md` §一 的分布（`80:10` / `90:88`）**不同**：那份是**含伤害/其它字段的另一种切法**，
> 本文按「卡池现读、单份树」重新数的。⚠️ 两个口径都要写出来，⛔ 别只挑一个（本工程已经因「口径不同」误判过两次）。

---

## 二 施工建议（按「先做哪个」排，⛔ 不是「做不做」）

1. **`90 RandomizedOnResolution`（87 行 / 63 张，量最大）** —— 做法：`BattleContext` 加「预烧 100 整数 + 下标」两格
   （与 `BattleContextExcessDamage.cs` 同一条 partial 纪律），开局烧、`GetRandomInt` 那一族加一个 `GetResolutionRandom()`
   按下标取。⚠️ 判据已够（`MatchData.cs:213-228` / `:541-573`，`CLAUDE.md` §三 也记着「⛔ 不要在结算里直接 `ctx.Rng.Next`」）。
   ⚠️ 会改**全池 63 张卡的指纹** ⇒ 必须由调度台在**没有 `.cs` 写手**时一次做完并重钉基线。
2. **`80 MinPlusMaxTimesMultiplier`（3 行 / 2 张）** —— 做法：`.c`/ILSpy 都读不到该分支，**先补判据**
   （用 VA 级反汇编找 `multiplierFactor` 的乘法点），再决定「取 `(min+max)×mult`」还是别的形状。
   ⚠️ 这两张卡的数值今天被 `enemyhand` 覆盖 ⇒ 排第 2。
3. `0` / `202` / `204`：**不改**（已对齐，理由见表）。若将来 `202` 需要按神分层（`markOfChaosType`），再翻。

---

## 三 与账的关系

- 本文件是**新发现**的账源：`80` / `90` 两档在账里**没有独立行**（它们挂在 `GetValueFromCriteria` 那一行里，
  而那一行的判据要点只点名了 `101/140/210`）。
- ⛔ 本代理不写账主表 ⇒ **请调度台按本文件立一条（或两条）新账**，`等级` 建议：
  **`90` = P1**（87 行 / 63 张，量最大、判据齐）· **`80` = P2**（3 行 / 2 张，判据缺、今天可观察差 0）。
