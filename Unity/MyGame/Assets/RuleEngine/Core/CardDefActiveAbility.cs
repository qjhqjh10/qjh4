// CardDefActiveAbility.cs — `ActiveAbility` 对象级的几个标量（`CardDef` 的 partial 半边）
//
// 🔴 **为什么单独开一个文件，而不是写进 `CardDef.cs` 类尾**：
//   ① `CardDef.cs` 的 `KeywordTable`（类尾之后）里还有 1 处账落点按【行号】引用
//      （`grep -o 'CardDef\.cs:[0-9]*' 资料/完全复刻/账/*.tsv` 现读最大 = `:7864`）
//      ⇒ 在 `CardDef` 类尾插成员会让它整体漂移（同族教训 → `资料/完全复刻/报告/B8_Q_trait口径与卡定义.md` §⑥）。
//   ② `Core/*.cs` 是**通配**编进 `工具/ruleprobe/ruleprobe.csproj` 的 ⇒ 新文件自动被带上；
//      而 `Data/CardDatabase.cs` 是**显式列出**的那一份（漏了就是构建期 `CS0246`）⇒ 它只能原地改。
//   ⇒ 所以：`CardDef` 这一半走新文件（零行号漂移），`CardDatabase` 那一半原地追加（见那边的注）。
//
// ==================================================================================================
//  📌 本文件同时是「**原版卡的主动能力（`activeAbility`）在我们引擎里走哪条路**」的判定记录
//     （2026-10-27 · 执行代理 `B9_W65ABIL`，判据全文 → `资料/完全复刻/判据/督军activeAbility入口_判定.md`）
// ==================================================================================================
//  原版三跳（第一权威 = `d:/2/tools/decomp_full/`，逐句读过）：
//    ① `RawCardScript.activeAbility(+0x260)` / `alternativeAbility(+0x2B8)` 的 `activeAbilityLogic(+0x48)`
//       —— 谁进哪一支看 `hasAlternativeAbility(+0x2AA)` 与 `BoardAnalysis.MeetsCriteria(normalAbilityBoardCriteria +0x2B0)`
//       （`RawCardScript__ResolveActiveAbility.c:16-38` · `RawCardScript__GetActiveAbility.c:5-16`）；
//    ② 逐行 `AbilityLogic__PlayAbility(logic, bm, abilityData, 400 /* ThisUnitAbilityUsed */, 0)`
//       （`RawCardScript__ResolveActiveAbility.c:58-67`）；
//    ③ 入口那一道闸 = `CardScript.hasActiveAbility(+0x220)`：
//       **为 0 就 `LogWarning` 直接 return，整条路径不跑**（`CardScript__ResolveActiveAbilityPlayed.c:26-31`），
//       而 `+0x220` 由卡数据 `RawCardScript.hasActiveAbility(+0x2A9)` 灌（`CardScript.cs:20085-20089`：
//       卡型 ≠ Hero 时压成 false，督军除外）。
//
//  ⇒ **我们这一侧的等价物（三跳 × 落地位置）**：
//    | 跳 | 原版 | 我们的落点 |
//    |---|---|---|
//    | ① 数据 | `activeAbility(+0x260).activeAbilityLogic` | `CardDef.ActiveAbilityLogic`（`CardDatabase.MapActiveAbilityLogic`，卡表 v23+）· 分组 `ActiveAbilityLogicGroup(ab)` |
//    | ①' 选支 | `hasAlternativeAbility` + `normalAbilityBoardCriteria` | `CardDef.HasAlternativeAbility` / `UsesAlternativeAbility`（`CardDef.cs` 那段） |
//    | ② 正文 | `PlayAbility(…, 400, 0)` 逐行 | **两条等价实现**：替代行动 → `TriggerOps(duty/pray/ferocity/agenda)`（`RuleCore.UseAlternative`）；誓约 → `OathOps`（`RuleCore.UseOathAbility`）；我们自定的 `Ability:` → `CardDef.Ability`（`RuleCore.UseAbility`） |
//    | ③ 闸 | `hasActiveAbility(+0x220)` | `CardDef.HasActiveAbility`（数据，`ConditionEval` 190/280 已读）· 按钮 = `BattleDriver.OpenCommand` 的 `HasAbility \|\| alt \|\| oath` |
//
//  🔴 **2026-10-27 实测口径（`B9_W65ABIL`，⛔ 别按旧说法「45/56 张督军发不出主动能力」改回去）**：
//    · 全池 `hasActiveAbility == 1` = **87 张**，且与「有 `gate=1` 的 `activeAbilityLogic` 行」**逐张一致**
//      （87 ⟺ 87，双向 0 例外）—— 数据侧是自洽的。
//    · 这 87 张里 **86 张**今天**已经能发**：82 张走替代行动关键词（`duty` 4 / `pray` 4 / `agenda` 3 是督军，
//      其余是部队），4 张走 `Oath`（`UM92/97/103/105`）；读数为证 —— `ruleprobe.sh play <卡>` 的电池步里
//      `alt(duty)=0` / `oath=0`（`资料/完全复刻/报告/B9_W65ABIL_督军主动能力入口.md` §二）。
//    · **只有 1 张没有任何入口**：`SOR66 Armorium Cherub`（`hasActiveAbility=1`，而卡面正文只有 `Rally:` /
//      `At the start of your turn…`，既无替代行动关键词也无 `Oath`）⇒ 技能钮**不出现**、`UseAbility` 恒 `ErrNoAbility(14)`。
//    · **45/56 张督军「没有主动能力」不是缺陷** —— 原版真值 `hasActiveAbility == 0`（`cards_truth.json` 逐张：
//      40 张 0 + 8 张名字对不上 + 8 张里 3 张 = 1 ⇒ 与卡表口径 11/45 吻合），
//      它们卡面只有 `Talent:`（被动触发），原版 `ResolveActiveAbilityPlayed.c:26-31` 同样早退。
//      ⇒ ⛔ 别为了「让 56 张都能发」去把 `CardDef.Ability` 造假数据。
//    · `CardDef.Ability`（自定 `EffectSpec` 封闭文法）**继续只服务我们自己设计的那批卡** —— 原版卡
//      `desc` 里 `Ability:` 一条都没有（现读 0 张），这一格**不是**缺口的载体。
//
//  🔴 **本文件的四个标量补的是【跳 ①② 之间缺的那一格数据】：原版 `ActiveAbility` 对象自己的
//     费用与「要不要点目标」**（`ActiveAbility.cs` 的 `manaCost` / `psychicCost` / `plasmaCost` / `targeted`）。
//     没有它就**没法按原版收费**（`BattleManager__CanUseActiveAbility.c:60-70` 的
//     `PlayerManager.HasEnoughMana(玩家, 卡, AbilityUsesPsychicEnergy, AbilityUsesPlasmaEnergy)`），
//     所以任何「把 `activeAbilityLogic` 真正执行起来」的写法都先在费用这一跳卡住。
//     ⚠️ **今天行为零变化**：`cards_engine.json` 还没有这四列（现读 v34）⇒ 一律 `GateUnknown(-1)`；
//        `AiScoring.ActiveAbilityManaCost` 那个钩子（它自己的 doc 明写「要经 工具/gen_cards_engine.py +
//        Core/CardDef.cs 两处改动，均不在本代理白名单」）**这一半已经落地**，接上钩子就能出非 0。
//     📌 还差的那一半 = 生成器（`工具/gen_cards_engine.py` 出 v35 四列，源 = 真值 `activeAbility` 对象）
//        + 接线（`AiScoring.ActiveAbilityManaCost = c => c.ActiveAbilityManaCost;`）—— 见报告 §四 改动请求。
using System;

namespace RuleEngine
{
    public partial class CardDef
    {
        /// <summary>原版 `ActiveAbility.manaCost`（`int @+0x14`，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ActiveAbility.cs`）。
        /// <para>`-1` = **未知**（旧卡表 &lt; v35 / 真值里没这张卡）—— ⛔ 别当成 `0`（`0` 是**合法的「免费」**，
        /// 现池 `SOR66` 就是 0，见 `cards_truth.json` 那张的 `activeAbility.manaCost`）。</para>
        /// <para>读点（将来的）= 主动能力的费用闸（原版 `BattleManager__CanUseActiveAbility.c:60-70`）与
        /// `RuleEngine/Data/AiScoring.cs` 的 `ActiveAbilityManaCost` 钩子（⑥⑦ 两段）。</para>
        /// <para>⚠️ **只有 `HasActiveAbility == 1` 时才有意义**；`0` 的卡这一格是「数据仍在、本支不消费」。</para></summary>
        public int ActiveAbilityManaCost { get; private set; } = GateUnknown;

        /// <summary>原版 `ActiveAbility.psychicCost`（`bool`）—— 「这个主动能力花的是**灵力**」。
        /// <para>三态：`1` = 是 · `0` = 否 · `-1` = **未知**（旧卡表 / 真值里没这张卡）。
        /// ⛔ 别把它当数值费用（费用只有一个 `int`，在 <see cref="ActiveAbilityManaCost"/>；
        /// 原版是把它当**币种开关**传给 `PlayerManager__HasEnoughMana`）。</para></summary>
        public int ActiveAbilityPsychicCost { get; private set; } = GateUnknown;

        /// <summary>原版 `ActiveAbility.plasmaCost`（`bool`）—— 「这个主动能力花的是**等离子**」。
        /// <para>三态同 <see cref="ActiveAbilityPsychicCost"/>：`1` / `0` / `-1 = 未知`。</para></summary>
        public int ActiveAbilityPlasmaCost { get; private set; } = GateUnknown;

        /// <summary>原版 `ActiveAbility.targeted`（`bool`）—— 「发这个主动能力**要不要先点一个目标**」。
        /// <para>三态：`1` = 要 · `0` = 不要 · `-1` = **未知**。</para>
        /// <para>⚠️ **与我们的 `EffectTargets.NeedsPick` 不是同一格、今天也不许互相顶**：
        /// 我们那一格是从**卡面正文**推的（`RuleCore.CanUseAbility` 读 `caster.Ability.Target`），
        /// 原版这一格是**能力对象上的序列化布尔**。两格在「正文推得出、原版标 0」这类卡上会不一致 ⇒
        /// 谁要改 `NeedsPick` 的判据，先拿这一格做对账（今天 0 消费者）。</para></summary>
        public int ActiveAbilityTargeted { get; private set; } = GateUnknown;

        /// <summary>这四格**来自 v35 卡表**的正面信号（`CardDatabase.Parse` 的构造后挂载口写）。
        /// <para>`false` = 旧表 / 夹具直接 `new CardDef(...)` ⇒ 上面四格一律 `-1`（未知），
        /// ⛔ **不是**「免费、不要目标」。与 `ActiveAbilityLogicVersioned` 同一口径。</para></summary>
        public bool ActiveAbilityCostKnown { get; private set; }

        /// <summary>构造后挂载口（`CardDatabase.Parse` 调；⛔ 别在别处调）。
        /// <para>四个实参各自 `-1` = 那一格未知（旧表 / JSON 没这个键 —— DTO 的缺键初值就是 `-1`，
        /// 与 `CardDto.cardType` / `cardDto.keyword` 同一写法）。</para>
        /// <para>语义 / 判据 / 为什么今天零行为变化 → 本文件头部那一段。</para></summary>
        public void AttachActiveAbilityCost(int manaCost, int psychicCost, int plasmaCost, int targeted)
        {
            ActiveAbilityManaCost = manaCost;
            ActiveAbilityPsychicCost = psychicCost;
            ActiveAbilityPlasmaCost = plasmaCost;
            ActiveAbilityTargeted = targeted;
            ActiveAbilityCostKnown = manaCost >= 0 || psychicCost >= 0 || plasmaCost >= 0 || targeted >= 0;
        }
    }
}
