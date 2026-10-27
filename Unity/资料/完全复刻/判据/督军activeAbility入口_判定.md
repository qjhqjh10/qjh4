# 督军 / 原版卡「主动能力入口」判定表（`B9_W65ABIL` · 2026-10-27）

> 用法：以后要动「原版卡的主动能力」这条链，**直接照这张表做**，⛔ 别重新查一遍。
> 配套：报告 `资料/完全复刻/报告/B9_W65ABIL_督军主动能力入口.md` · 代码记录 `MyGame/Assets/RuleEngine/Core/CardDefActiveAbility.cs` 文件头。
> 量法：数据 = `RuleEngine/Resources/cards_engine.json`（v34）+ `cards_truth.json`；
> 引擎 = `bash 工具/ruleprobe.sh play <id>`（电池步 `steps`）/ `play all pf <out> preset hero=1`（督军档 56 行）。

| # | 查的是什么 | 判据结论（值 + 出处） | 还差什么 |
|---|---|---|---|
| **J1** | 「56 张督军里 45 张发不出主动能力」是不是缺口？ | **不是**。那 45 张真值 `hasActiveAbility = 0`（`cards_truth.json` 逐张：40 张按名直对 + 8 张名字有差异，其中 3 张 = 1 ⇒ 45 / 11 与卡表口径吻合）。原版 `CardScript__ResolveActiveAbilityPlayed.c:26-31`：`+0x220 == 0` ⇒ `LogWarning` + return。它们卡面只有 `Talent:`（被动）。 | 无（结论已闭合） |
| **J2** | 数据侧自洽吗？`hasActiveAbility=1` 与 `gate=1` 行是同一批卡吗？ | **是，逐张一致**：各 **87 张**，双向 0 例外。`gate` 由生成器 `branch_gate()` 从 `hasActiveAbility` 算出（`activeAbilityLogic` 133 行 = `gate=1` 88 + `gate=0` 45）。 | 无 |
| **J3** | 87 张里今天各走哪条入口？ | 督军 11 = 替代行动（duty 4 / pray 4 / agenda 3）· 部队 57 = 替代行动（duty 19 / pray 19 / agenda 10 / ferocity 9）· 部队 18 = 誓约（`oath=0`）· **部队 1 = 无入口（`SOR66`）**。读数 = `alt(…)=0` / `oath=0` 出现在 `steps` 里。 | 无 |
| **J4** | `CardDef.Ability` 恒空是不是缺口？ | **不是**，而且**不该**让它承载原版卡：它是我们自定 `EffectSpec` 封闭文法（`CardDef.cs:1423` 的 `if (!FromOriginalPool)`），原版卡 `desc` 里 `Ability:` 现读 **0 张**。原版那张「一个按钮」在我们这边由 `RuleCore.UseAlternative` / `UseOathAbility` 代言。 | 无（⛔ 别改回「给原版卡填 Ability」） |
| **J5** | 唯一没有入口的卡是哪张、为什么？ | `SOR66 Armorium Cherub`：`hasActiveAbility=1`，但 `HasAbility`（`Ability:` 语法）/ alt 关键词 / `Oath` **三项全假** ⇒ `BattleDriver.OpenCommand` 的 `skill` 恒假 ⇒ 技能钮永不出现，`UseAbility` 恒 `ErrNoAbility(14)`。 | **先查清它的技能钮在原版做什么**（见 J6）—— 查清前 ⛔ 别实现 |
| **J6** | `SOR66` 的 `activeAbility` 数据可信吗？ | **可疑，未查清**：**原始 SO dump**（`资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `cards[uniqueId=SOR66].activeAbility`）里 `activeAbilityLogic` = `[chosenAbility 30 buffOthers ta=30 fm=1, chosenAbility 462 triggerPray ta=20 fm=1]`，而它**不是 `Pray` 卡**；卡面（PnP 实拍）只有 `Rally:` 与 `At the start of your turn…`。⚠️ **不是提取错**（原始 SO 就这么写），是 SO 本身可疑（最可能是把 `cardAbilities[0]` 那一段复用进了 `activeAbility`）。 | 需原版实况点这张卡的技能钮（受真Play 窗口限制）—— 查清前 ⛔ 别实现 |
| **J6'** | 「某张卡 `activeAbilityLogic` 的真值」读哪个文件？ | **只能读 `资料/普查产出_1018/allcards_1350/cards_raw.json`**（原始 SO dump，键 = `activeAbilityLogic`）。`Resources/cards_truth.json` 是**缩编**版：键叫 `logic`、现读 143 张有对象但 **0 张带行** ⇒ 拿它当「这张卡没有主动能力行」是假结论。 | 无 |
| **J7** | 原版主动能力的**费用**我们有没有？ | **没有**（本片补了代码侧）：缺 `ActiveAbility.manaCost` / `psychicCost` / `plasmaCost` / `targeted` 四格。原版费用闸 = `BattleManager__CanUseActiveAbility.c:60-70` 的 `PlayerManager__HasEnoughMana(…, AbilityUsesPsychicEnergy, AbilityUsesPlasmaEnergy)`。 | 生成器 v35 出四列（R1）+ `AiScoring` 接线（R2）；**`AbilityUsesPsychicEnergy`/`AbilityUsesPlasmaEnergy` 的方法体还没读** |
| **J8** | 「把 `activeAbilityLogic` 的行当效果执行」这台执行器做了吗？ | **没做**。原版逐行 `switch (chosenAbility)` 分派到 `doDamage` / `buffOthers` / `summonUnit` / `recallToOwnerHand` / `stun` … （`AbilityLogic__PlayAbility.c`）；我们今天是**用卡面正文**（`EffectText` → `TriggerOps`）代言。 | 大工程，且一改就动 `play` 指纹 ⇒ 要排重钉窗口（R3） |
| **J9** | `gate=0` 那 45 行该删吗？ | **不该**。它们是「已知不消费」（`gen_cards_engine.py` 的 `branch_gate()` + `资料/普查产出_1024/D11_activeAbility列.md` §2）：保留落列才能在换 build 翻上去时直接用；删掉就把「已知不消费」读成「没收到」。 | 无 |
| **J10** | `ActiveAbility.minHeroLevel` 要落吗？ | **今天不落**：`int`（`ActiveAbility.cs` 第一格），但**无消费者、也无判据说明它在 PvP 生效**。 | 战役 / 英雄等级线要用时再按同一形状补 |
| **J11** | 把 `abil` 对 11 张督军改成 0（让 `UseAbility` 成功）行不行？ | **不行**：那会让同一张卡在一趟电池里结算两次（`abil` 先花掉行动 ⇒ 随后 `alt` 变 `ErrExhausted`），并且改 `play` 指纹里 82 张部队卡的 `energyDelta` / `changes`。本片实测：改前 / 改后 56 行 `H\|` 读数**逐字节相同**（应有状态）。 | 无 |

## 附：三条「别推翻」

1. ⛔ **别用 `CardDef.Ability != null` 去顶 `hasActiveAbility`**（池内 `desc` 带 `Ability:` 的卡 0 张，而 `hasActiveAbility==1` 有 87 张 ⇒ 会把 87 张全判死）。
2. ⛔ **别拿 `abil=14` 当缺陷**：`RuleCore.UseAbility` 是我们自定 `Ability:` 那一档；原版卡的主动能力由 `alt` / `oath` 两条读数代言。
3. ⛔ **别给 45 张 `gate=0` 的督军造正文**：原版自己也不给它们主动能力（J1）。
