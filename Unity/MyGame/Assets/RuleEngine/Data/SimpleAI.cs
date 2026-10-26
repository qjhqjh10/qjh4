// SimpleAI.cs — 对手 AI
//
// 🔴 **2026-09-17 按原版 `AI` 类重写**（正本 `资料/AI_原版反编译_0917.md`，那里有公式/常量/数据类/死代码清单）。
//
// 为什么重写：老版本是「能出就出**最贵**的牌 / 能放技能就放 / 能打就挑收益最高的一刀」三条贪心 ——
// 能对打，但它不是原版的行为。原版 `AI` 类那 41 个方法体 2026-09-17 全部反编译出来了，形状是这样的：
//
//     PlayTurn(manager, mode)                ← 一次调用**只做一步**（循环在 BattleManager 的协程里）
//       ① GetAvailableActions(...)          ← 动作表：出牌 / 近战 / 远程 / 技能 / waystone / **endTurn 基准**
//       ② 逐条 GetActionScore(...)          ← 打分族（见下）
//       ③ TweakAvailableActions(...)        ← **唯一随机源、唯一难度旋钮**：把最低分的几条随机丢掉
//       ④ SelectBestAction(...)             ← 严格 `>`，平手保留先出现的；最佳是 endTurn 就收手
//       ⑤ ExecuteAction(...)
//
// 打分族的量级（照 `GameAssembly.dll` 浮点池读出来的真值）：
//   · 打死督军 **1e6**（`ScoreFromDamagingBuffedUnit`）· 合击够斩杀 **+10000**（`GetActionScore`）
//   · 打督军非致命 = `伤害 × (剩余血 > 15 ? 1.2 : 1.8)`
//   · 小兵：打死 = `卡价值 × 1.0`（带 survivor 0.8）；没打死 = `卡价值 × 0.6 × 实际伤害 / 血上限`
//   · **卡价值 = 费用 × 2 + 1**（`GetCardReferenceValueInPlay`）+ 关键词分值 × 伤势/眩晕衰减
//   · 攻击 = `伤害分 − 反击分`，被反击后还活着 `+0.1`
//   · `endTurn` 恒为 **0** ⇒ **「分数 ≤ 0 的动作就不做」** —— 原版的「留牌」其实就写在这条上
//     （`ScoreFromKeepingTheCard` 那版构建里 0 调用点，见正本 §六）
//
// 🔴 **权重那层的口径（2026-10-19 起，T06 / A1797）**：**出牌这条主链已经改成原版逐卡评分表** ——
//    `ScoreFromPlayingCard = (单位 ? GetValueInPlay : 0) + ScoreFromPlayingNow`，
//    `ScoreFromPlayingNow` 读 `nonTargetScoringCriteria`（所有卡都跑）＋
//    `cardTargetScoringCriteria`（`targeted=1` 且带目标时），数据源 = `Resources/ai_scoring.json`
//    ← SO 真值（`cards_truth.json`）。**缺两列时原版没有兜底**（单位仍走 `ValueInPlay`，战术卡 = 0）。
//    ⚠️ 技能 / 誓约 / 替代行动：**2026-10-19 续工已切到原版 `ActiveAbility` 两列 criteria**
//    （数据源 `cards_raw.json`）；只有**不在真值表里的卡**（我们自己那套 StarterCards / 自检夹具）
//    还走旧的 `ScoreOps` 兜底。判据全文 → `资料/普查产出_1018/AUD_T06_AI评分口径.md`。
//
// ⚠️ **两条不能改的**：
//   ① 随机**只用 `ctx.AiRng`**（种子派生，一局必须可复现）——绝不用 `UnityEngine.Random`；
//   ② 「近战还是远程」的**兼容判据**仍然只此一处（`UseRanged`），别在别处另写一份。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 动作种类。**取值照原版 `PlayerActions`**（`dump.cs:45579`）：
    /// `playCard=0 / attackMelee=1 / attackRanged=2 / useHeroPower=3 / endTurn=4 / activeAbility=5 / clickWaystone=6`。
    ///
    /// ⚠️ 原版把**主动技能 / 誓约能力 / 替代行动**（`duty` / `pray` / `ferocity` / `agenda`）**都归在
    ///    `activeAbility`(5) 一类**里（判据是 `CardScript.hasActiveAbility`）。我们引擎里这三条是
    ///    三条不同的入口（`RuleCore.CanUseAbility` / `CanUseOathAbility` / `CanUseAlternative`），
    ///    所以动作上带一个 <see cref="AiAction.AltKeyword"/> 区分，**类型值照旧用 5**。
    ///
    /// ⚠️ `useHeroPower`(3) 原版**从不产出**（`GetAvailableActions` 里没有这一支）；我们同样不产出。
    /// ✅ **`clickWaystone`(6) 2026-09-25 接上了** —— 引擎那半（`RuleCore.CollectWaystone`）已经做出来，
    ///    原版把它当**独立一项动作**（`GetAvailableActions` 的第 4 支，只看「灵族残骸 + `CanUseWaystone`」，
    ///    与单位行不行动无关）⇒ 这里也是独立一支，**不在那个 `u.Exhausted` 的循环里**。
    /// </summary>
    public enum AiActionKind
    {
        PlayCard = 0,
        AttackMelee = 1,
        AttackRanged = 2,
        EndTurn = 4,
        ActiveAbility = 5,
        CollectWaystone = 6,
    }

    /// <summary>
    /// 难度。**取值照原版 `DeckDifficultyLevel`**（`SuperEasy=0 / Easy=5 / Normal=10 / Hard=15`）。
    /// 它只调**一个**东西：`TweakAvailableActions` 的 `minSkips / maxSkips / skipChance`
    /// —— 也就是「把 AI 本来想做的事，按最低分往下砍掉多少」。**原版没有搜索深度这回事。**
    /// </summary>
    public enum AiDifficulty { SuperEasy = 0, Easy = 5, Normal = 10, Hard = 15 }

    /// <summary>
    /// 一条可用动作（照原版 `AvailableAction.cs`：`actionType` / `actingCard` / `targetCard` /
    /// `actionScore` / `manaCost`）。我们这边用**下标**代替对象引用（`HandIdx` / `Slot` / `TargetSlot`）——
    /// 引擎的状态是裸数组（`PlayerState.Board`），存引用会在棋盘变动后指向旧对象。
    /// </summary>
    public class AiAction
    {
        public AiActionKind Kind;
        /// <summary>出牌时的**手牌下标**</summary>
        public int HandIdx = -1;

        /// <summary>
        /// 🔴 **第 7 行第 4 步（2026-09-18）**：枚举时挑中的是**手牌里的哪一份**。
        /// 执行时按它**重算下标**（`Hand.IndexOf(HandInst)`）——
        /// 动作表和执行之间隔着一个「挑最优」的过程，手牌在这期间变了的话，旧下标会指到别的牌上。
        /// ⚠️ 拿不到实例时（老路径 / 手牌里已经没有了）退回用 `HandIdx`。
        /// </summary>
        public RuleEngine.CardInstance HandInst;
        /// <summary>行动单位所在的格位（出牌时 = **落点**格位）</summary>
        public int Slot = -1;
        /// <summary>目标方（0/1）。`-1` = 这个动作没有目标</summary>
        public int TargetP = -1;
        /// <summary>目标格位</summary>
        public int TargetSlot = -1;
        /// <summary>这一刀用近战还是远程（只有攻击动作有意义）</summary>
        public bool Ranged;
        /// <summary>`ActiveAbility` 的子类：`null` = 普通主动技能 · `"oath"` = 誓约能力 ·
        /// 其余 = 替代行动的关键词（`duty` / `pray` / `ferocity` / `agenda`）</summary>
        public string AltKeyword;

        /// <summary>🆕 2026-09-27：**执行前**从 `ctx.ChoosePicks` / `ChooseCardIds` 快照下来的面板答案。
        /// 🔴 录像是**必须**在「执行前」抓的 —— 引擎一结算就把队列吃空了，执行后再读**只剩空**
        /// （`TakePick` 那时已经改走 `ctx.Rng.Next` 兜底）⇒ 回放时会**多抽一次随机数**，
        /// 整条 RNG 流从此错位（2026-09-27 实测：分叉点稳稳落在第一条「要选东西」的技能上）。</summary>
        public int[] CapturedPicks;
        public string[] CapturedPickIds;

        /// <summary>原版把它写在 `AvailableAction.actionScore`（`+0x28`）</summary>
        public float Score;
        /// <summary>花多少能量（原版 `+0x2C`）</summary>
        public int ManaCost;

        public override string ToString()
        {
            return Kind + (HandIdx >= 0 ? " hand#" + HandIdx : "")
                 + (Slot >= 0 ? " @" + Slot : "")
                 + (TargetSlot >= 0 ? " →P" + (TargetP + 1) + "@" + TargetSlot : "")
                 + (AltKeyword != null ? " [" + AltKeyword + "]" : "")
                 + " = " + Score.ToString("F2");
        }
    }

    public static class SimpleAI
    {
        // ==================================================================
        //  常量（照原版：`资料/AI_原版反编译_0917.md` §五·一）
        // ==================================================================

        /// <summary>打死督军/英雄 —— 原版 `ScoreFromDamagingBuffedUnit` 的出口值</summary>
        const float WarlordKillScore = 1000000f;
        /// <summary>合击够斩杀 —— 原版 `GetActionScore` 加的分</summary>
        const float CombinedKillBonus = 10000f;
        /// <summary>🆕 **收集灵魂石（原版 `clickWaystone`）的固定分 = 50.0** ——
        /// 原版 `GetActionScore` 对这一路**不打分、不看局势**，就是一个常数
        /// （`资料/AI_原版反编译_0917.md` §四「`clickWaystone` ｜ 固定 ｜ **50.0**」）。
        /// 50 比 `endTurn` 的 0 高得多 ⇒ AI 只要看到能收集就会去收，和原版一致。</summary>
        const float WaystoneScore = 50f;
        /// <summary>被反击之后还活着 —— 原版 `ScoreFromBuffedAttack` 的小奖励</summary>
        const float SurvivalBonus = 0.1f;
        /// <summary>打督军非致命时的系数：剩余血 &gt; 15 用 1.2，否则 1.8（原版同）</summary>
        const float HeroDamageLow = 1.2f, HeroDamageHigh = 1.8f;
        /// <summary>小兵没被打死时的系数（原版 `0.6 × 伤害 / 血上限`）</summary>
        const float MinionPartial = 0.6f;
        /// <summary>带 `survivor` 的小兵被打死时打折（原版 0.8）</summary>
        const float SurvivorDiscount = 0.8f;
        /// <summary>卡价值 = 费用 × 2 + 1（原版 `GetCardReferenceValueInPlay`）</summary>
        public static float RefValue(CardDef c) { return c == null ? 0f : c.Cost * 2f + 1f; }

        // ==================================================================
        //  ①·b 放位（原版 `MinionManager.GetNextSlotCheckingAdjacents`）
        //
        //  🔴 **原版 AI 根本不能选格位** —— `AI.AvailableAction` 里只有
        //  `actionType / actingCard / targetCard / actionScore / manaCost`
        //  （`dump.cs:118939-118945`），**没有 slot 字段**；格位是 `AI.ExecuteAction`
        //  当场问 `MinionManager.GetNextSlotCheckingAdjacents()` 要的
        //  （`d:/2/tools/decomp_full/AI__ExecuteAction.c:60,62,63`，只有 `cardType == 0`
        //   —— 也就是小兵 —— 才走这条，英雄不走）。
        //
        //  ⚠️ 我们老版本是「逐格枚举 + 打分挑最小槽号」⇒ 永远先把左边 4 格**从外往里**填满，
        //     **与原版正好相反**（2026-09-29 用户点名查实，Q5）。
        // ==================================================================

        /// <summary>
        /// 这张小兵卡是不是「**带相邻效果**」（原版 `CardScript.HasWhileInPlayAdjacentEffect`）。
        ///
        /// **原版判据是两条【或】**（`CardScript__HasWhileInPlayAdjacentEffect.c`）：
        ///   ① `HasCurrentTrait(DefinedTrait.requiem)` —— `requiem = 760`（`dump.cs` 的 `DefinedTrait`）；
        ///   ② 有 `whileInPlay` 能力，且某条 `cardAbilities[]` 的
        ///      `targetCriteria.targetsAffected == TargetsAffected.adjacentToSelfIfMeetsCriteria`（= `100`）。
        ///   （字段链：`RawCardScript.cardAbilities` 在 `+0x290` → `CardAbility.targetCriteria` 在 `+0x30`
        ///    → `TargetCriteria.targetsAffected` 在 `+0x10`。）
        ///
        /// 🔴 **我们只落地第 ② 条，两条如实标注**：
        ///   · **`requiem` 那一支在我们卡池里 0 命中**（`Resources/cards_engine.json` 全库 grep
        ///     `requiem` = **0 条**，2026-09-29 实测）⇒ 是**空支**，不是被我们砍掉的功能；
        ///   · 第 ② 条在我们这边 = **光环里 `Adjacent == true` 那一条**（<see cref="AuraSpec.Adjacent"/>）
        ///     —— 那正是「在场上时作用于相邻格」的那族，与原版那个 `targetsAffected` 指同一件事。
        ///     ⚠️ **别在这儿另写一套相邻筛选**：「谁算相邻」全仓只读 `BoardSpec.AdjacentSlots`。
        /// </summary>
        public static bool HasAdjacentEffect(CardDef c)
        {
            if (c == null || c.AuraSpecs == null) return false;
            for (int i = 0; i < c.AuraSpecs.Count; i++)
                if (c.AuraSpecs[i] != null && c.AuraSpecs[i].Adjacent) return true;
            return false;
        }

        /// <summary>
        /// **AI 出小兵时该落哪一格** —— 照原版 `MinionManager.GetNextSlotCheckingAdjacents()` 逐句搬
        /// （`d:/2/tools/decomp_full/MinionManager__GetNextSlotCheckingAdjacents.c`
        /// ＋ `MinionManager__GetNextSlotWithoutDisplacing.c`；字段偏移表 = `dump.cs` 的 `MinionManager`）。
        ///
        /// **原版规则三步，顺序不能换**：
        ///   1. **左侧**：若未满，**从督军往外**扫；遇到第一个「带相邻效果」的小兵 ⇒
        ///      返回它**外侧紧邻**那一格（原版 `return -i - 2`，`i` 是列表下标）。
        ///   2. **右侧**：左侧没命中（或已满）才扫右侧，同样从督军往外（原版 `return i + 2`）。
        ///   3. 都没有 ⇒ **平衡**：`左侧人数 &lt; 右侧人数` 才放左，**否则放右**
        ///      （原版 `leftMinionsOccupation.Count &lt; rightMinionsOccupation.Count ? ~leftCount
        ///      : rightCount + 1`）⇒ **两侧相等时先放【右】**
        ///      ⇒ 真实序列 = **右近 → 左近 → 右次 → 左次 …**
        ///      （⚠️ 文档里一度写成「先左」，**那是反的** —— 2026-09-29 更正）。
        ///
        /// ⚠️ **两处如实标注的读法**（不是推断，是 `.c` 里就这么写的）：
        ///   · 原版 `leftMinions` / `rightMinions` 是**从督军往外**排的列表
        ///     （`InsertMinion` 拿 `~slot` 当插入下标 ⇒ 下标 `i` ↔ 距督军 `i+1` 格）；
        ///     扫描**遇到空位就停**（`get_Item` 返回 null 就 `break`）。
        ///   · 「该侧已满」的判据是 `slotPos.Count(恒 4) − minions.Count &lt; 1`。
        ///
        /// 🔴 **2026-10-22（`W63`）**：第三支（平衡）改走 `BoardSlots.NextFreeWithoutDisplacing` ——
        /// 原版那一步同样只读 `List.Count`、**没有「目标格被占」这一档**（原版两侧是 `List`，
        /// 结构上不可能有洞）；我们这边 `Board[]` 是定长数组、且 `CountSide` 只数活人
        /// ⇒ 非法局面下 `WarlordSlot ± (count+1)` 会指到**已被占**的那一格，
        /// 再喂给 `RuleCore.PlayCard` 就会**静默抹掉**外侧单位。判据与措辞口径见方法体第 3 段。
        ///
        /// 返回 **棋盘槽号（0..8）**；两处都放不下（格不合法）时返回 **-1**。
        /// </summary>
        public static int NextDeploySlot(BattleContext ctx, int p)
        {
            if (ctx == null || p < 0 || p > 1) return -1;
            var board = ctx.Players[p].Board;

            // ---- 1. 左侧（下标 i ↔ 槽号 WarlordSlot-(i+1)，i=0 最靠督军）----
            int leftCount = CountSide(board, -1);
            if (leftCount < BoardSpec.SlotsPerSide)
            {
                for (int i = 0; i < leftCount; i++)
                {
                    int s = BoardSpec.WarlordSlot - (i + 1);
                    var u = board[s];
                    if (u == null || !u.IsAlive) break;          // 原版遇空位就停
                    if (HasAdjacentEffect(u.Card)) return BoardSpec.WarlordSlot - (i + 2);
                }
            }

            // ---- 2. 右侧（没命中才轮到这儿）----
            int rightCount = CountSide(board, +1);
            if (rightCount < BoardSpec.SlotsPerSide)
            {
                for (int i = 0; i < rightCount; i++)
                {
                    int s = BoardSpec.WarlordSlot + (i + 1);
                    var u = board[s];
                    if (u == null || !u.IsAlive) break;
                    if (HasAdjacentEffect(u.Card)) return BoardSpec.WarlordSlot + (i + 2);
                }
            }

            // ---- 3. 平衡：左少 ⇒ 左；否则右（两侧相等 ⇒ 先右）----
            // 🔴 **2026-10-22（`W63` 普查 · `T02` 同族的**第五处**写入点）：第三支也必须判「这一格真的可占」。**
            //   原版这一步就是 `GetNextSlotCheckingAdjacents.c:44` 那一跳
            //   `GetNextSlotWithoutDisplacing` —— 它**只比两条 `List.Count`、没有任何占用检查**
            //   （`MinionManager__GetNextSlotWithoutDisplacing.c:14-30`），因为原版两侧是 `List`、
            //   结构上不可能有洞。⚠️ 我们这边的差异有两层，**都只可能出现在非法局面上**：
            //     · `CountSide` 数的是「贴督军起**连续且 `IsAlive`**」的段 ⇒ 段首那个
            //       **非空但已死**的残留单位会让它**提前停**（引擎自己的死亡路径当场 `RemoveAt`，
            //       正常对局打不出这种局面，只有夹具能写出来）；
            //     · `Board[]` 是**定长数组**，自检夹具可以直接写出洞。
            //   ⇒ 这时原来那句 `WarlordSlot ± (count + 1)` 会指到**已经被占**的那一格，而
            //      `RuleCore.PlayCard` 会把它喂给 `BoardSlots.Insert`：有洞时那条降级路径落到插推，
            //      会把外侧单位**静默抹掉**（同一族的原始症状见 `A1692` / `FirstFreeSlot`）。
            //   ⇒ 改走**同一个判据的第二档** `BoardSlots.NextFreeWithoutDisplacing`
            //     （`FirstFreeSlot` / `RuleCore.DeployFree` / `EffectResolver.DoTakeControl` /
            //      `RuleCore.EndTurn` 四处已经用它）；⛔ 别在这里另写一份「空格 = 没单位」。
            //   ⚠️ **连续棋盘上行为逐字不变**：那一档的第一支原样返回 `NextWithoutDisplacing`，
            //     而它与原版 `GetNextSlotWithoutDisplacing.c:14-30` 是同一条算式（左 < 右 ⇒ 左、否则右，
            //     平手走右），落点 = 该侧最外那一格。
            //   ⚠️ **原版行为【不可比】**：原版根本没有「目标格被占」这一档 ⇒ 这是**补我们表示法**的洞，
            //     ⛔ 别在注释里写成「照原版」（同 `A1692` 的措辞口径）。
            int slot = BoardSlots.NextFreeWithoutDisplacing(ctx.Players[p], out int blockedSlot);
            if (blockedSlot >= 0)
            {
                UnityEngine.Debug.LogWarning("[SimpleAI] ⚠️ 棋盘不连续：`NextDeploySlot`（平衡支）按原版规则算出的 "
                    + blockedSlot + " 号格已被 " + (board[blockedSlot] != null ? board[blockedSlot].Name : "?")
                    + " 占着 —— **不覆盖**，改选该侧最外的空格 "
                    + (slot >= 0 ? slot.ToString() : "（该侧没有空格）")
                    + "。引擎的增删路径都维持「每侧从贴督军那格起连续无洞」，出现这种局面说明"
                    + "**有地方（多半是自检夹具）直接写了 `Board[]`**（`T02` / `A1692` 同族）。");
            }
            return slot;
        }

        /// <summary>
        /// 这一侧**从督军往外数、连着的**小兵有几个（原版 `leftMinions` / `rightMinions` 那个
        /// `List.Count` 的同口径读法 —— 列表是按下标连续的，遇到空位就是到头）。
        /// <paramref name="dir"/> = `-1` 左 / `+1` 右。
        /// </summary>
        static int CountSide(UnitState[] board, int dir)
        {
            int n = 0;
            for (int i = 1; i <= BoardSpec.SlotsPerSide; i++)
            {
                int s = BoardSpec.WarlordSlot + dir * i;
                if (!BoardSpec.IsValid(s)) break;
                var u = board[s];
                if (u == null || !u.IsAlive) break;
                n++;
            }
            return n;
        }

        // ==================================================================
        //  ① 动作表（原版 `BattleManager.GetAvailableActions`）
        // ==================================================================

        /// <summary>
        /// 枚举当前行动方**所有能做的动作**。
        ///
        /// 照原版那个顺序（出处见正本 §三）：手牌 → 场上单位的近战/远程 → 技能 → **最后追加 endTurn**。
        /// 每一条都是「引擎说合法」的（一律问 `RuleCore.CanPlayCard` / `IsValidTarget` /
        /// `CanUseAbility` / `CanUseOathAbility` / `AvailableAlternative`）——
        /// **不在这儿复制第二份合法性判据**（两处写迟早不一致，工程铁律）。
        ///
        /// ⚠️ 近战和远程**各列一条**（原版就是这样：`attackMelee` / `attackRanged` 是两条动作，
        ///    各自打分再挑高的）。老版本那句「远程攻击力更高就用远程」是另一种近似。
        /// </summary>
        public static List<AiAction> EnumerateActions(BattleContext ctx)
        {
            var list = new List<AiAction>();
            if (ctx == null || ctx.IsOver) return list;

            int me = ctx.Active, foe = 1 - me;
            var mine = ctx.Players[me];
            var foeBoard = ctx.Players[foe].Board;

            // ---- 手牌 ----
            for (int i = 0; i < mine.Hand.Count; i++)
            {
                var card = mine.Hand[i].Card;      // 第 7 行第 2 步：手牌存实例，评分看的是模板
                if (card == null) continue;
                int cost = RuleCore.CostOf(ctx, me, mine.Hand[i]);   // 第 7 行第 3 步：算的是**这一份**
                if (cost > mine.Energy) continue;                       // 付不起：原版在 CanPlayCard 里挡

                if (card.IsUnit)
                {
                    // 🔴 **小兵：只发一条动作，格位不由 AI 选** —— 照原版 `AI.ExecuteAction` 那条路
                    //    （`cardType == 0` 才问 `MinionManager.GetNextSlotCheckingAdjacents()`）。
                    //    判据只在 <see cref="NextDeploySlot"/> 一处，这儿**不另写一遍**。
                    int s = NextDeploySlot(ctx, me);
                    if (s >= 0 && RuleCore.CanPlayCard(ctx, me, i, s) == RuleCodes.OK)
                        list.Add(new AiAction { Kind = AiActionKind.PlayCard, HandIdx = i, HandInst = mine.Hand[i],
                                                Slot = s, ManaCost = cost });
                }
                else
                {
                    // 战术卡：`slot` 的含义是「**效果打谁**」（见 `RuleCore.CanPlayCard` 里那句注释）
                    // ⇒ **每个合法目标各发一条**，打分再挑最高 —— 这正是原版 `GetAvailableActions` 的形状。
                    //
                    // 🆕 2026-10-19（T06）：**目标在哪一方**填进 `TargetP` —— 原版评分
                    //   `ScoreFromPlayingNow` 是按 `targetCard`（一个真实对象）算的，
                    //   而我们的 `Slot` 只是格号 ⇒ 侧必须另外带上，否则打分不知道目标属于谁。
                    //   判据 = `EffectText.PickTarget`（和 `CanPlayTactic` / 结算同一条路）。
                    int tp = TargetSideOf(ctx, me, card);
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        if (RuleCore.CanPlayCard(ctx, me, i, s) != RuleCodes.OK) continue;
                        list.Add(new AiAction { Kind = AiActionKind.PlayCard, HandIdx = i, HandInst = mine.Hand[i],
                                                Slot = s, TargetP = tp, ManaCost = cost });
                    }
                }
            }

            // ---- 场上单位：近战 / 远程各一条 ----
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = mine.Board[s];
                if (u == null || !u.IsAlive || u.Exhausted || u.IsStunned) continue;

                for (int t = 0; t < BoardSpec.Size; t++)
                {
                    if (foeBoard[t] == null) continue;
                    for (int r = 0; r <= 1; r++)
                    {
                        bool ranged = r == 1;
                        if (RuleCore.IsValidTarget(ctx, me, s, foe, t, ranged) != RuleCodes.OK) continue;
                        list.Add(new AiAction
                        {
                            Kind = ranged ? AiActionKind.AttackRanged : AiActionKind.AttackMelee,
                            Slot = s, TargetP = foe, TargetSlot = t, Ranged = ranged,
                        });
                    }
                }
            }

            // ---- 技能 / 誓约 / 替代行动（原版都算 `activeAbility`）----
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = mine.Board[s];
                if (u == null || !u.IsAlive || u.Exhausted || u.IsStunned) continue;

                // 誓约能力（原版 `CardScript.CanUseOathAbility`；不点目标）
                if (u.Card != null && u.Card.OathOps != null && RuleCore.CanUseOathAbility(ctx, me, s) == RuleCodes.OK)
                    list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s, AltKeyword = "oath" });

                // 替代行动（`duty` / `pray` / `ferocity` / `agenda`；至多一条）
                string alt = RuleCore.AvailableAlternative(ctx, me, s);
                if (alt != null)
                {
                    // 要选目标的那种（`AlternativeTargetSide` 给出目标在哪一方，`-1` = 不用点）：
                    // 每个合法目标一条 —— 判据**问引擎**（`CanPickAlternativeTarget`），
                    // 这边不重写一遍「谁能被选中」。
                    int side = RuleCore.AlternativeTargetSide(ctx, me, s, alt);
                    if (side >= 0)
                    {
                        var tb = ctx.Players[side].Board;
                        for (int t = 0; t < BoardSpec.Size; t++)
                        {
                            if (tb[t] == null) continue;
                            if (!RuleCore.CanPickAlternativeTarget(ctx, me, s, alt, t)) continue;
                            list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s, AltKeyword = alt,
                                                    TargetP = side, TargetSlot = t });
                        }
                    }
                    else
                    {
                        list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s, AltKeyword = alt });
                    }
                }

                // 普通主动技能
                var spec = u.Ability;
                if (spec == null) continue;
                if (EffectTargets.NeedsPick(spec.Target))
                {
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        if (foeBoard[t] == null) continue;
                        if (RuleCore.CanUseAbility(ctx, me, s, t) != RuleCodes.OK) continue;
                        list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s,
                                                TargetP = foe, TargetSlot = t });
                    }
                }
                else if (RuleCore.CanUseAbility(ctx, me, s) == RuleCodes.OK)
                {
                    list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s });
                }
            }

            // ---- 🆕 收集灵魂石（灵族路标石；原版动作表第 4 支 `clickWaystone`）----
            // ⚠️ **独立一段、不并进上面那个单位循环**：上面那两圈都有 `u.Exhausted` 的前置跳过，
            //    而**残骸恒为 `Exhausted`**（见 `RuleCore` 造残骸那几行）⇒ 塞进去一条都产不出来，
            //    而且是**静默**的（AI 永远不收石头，没人报错）。
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                if (RuleCore.CanCollectWaystone(ctx, me, s) != RuleCodes.OK) continue;
                list.Add(new AiAction { Kind = AiActionKind.CollectWaystone, Slot = s });
            }

            // ---- 兜底：endTurn（原版恒为最后一条、分数恒为 0）----
            list.Add(new AiAction { Kind = AiActionKind.EndTurn });
            return list;
        }

        /// <summary>
        /// 战术卡/防御卡**这一下打的是哪一方**（返回**绝对玩家下标**；`-1` = 不用点目标）。
        /// 判据 = `EffectText.PickTarget`（与 `CanPlayTactic` / 结算**同一条路**，⛔ 不另写一份）。
        /// 按卡 id 缓存（同一张卡在一局里反复枚举，解析一次就够）。
        /// </summary>
        static readonly Dictionary<string, int> _targetSideCache = new Dictionary<string, int>();

        static int TargetSideOf(BattleContext ctx, int me, CardDef card)
        {
            if (card == null || card.IsUnit) return -1;
            int rel;
            if (!_targetSideCache.TryGetValue(card.Id, out rel))
            {
                List<string> unparsed, partial;
                var ops = EffectText.Parse(card.Desc, out unparsed, out partial, card: card);
                var spec = EffectText.PickTarget(ops);
                rel = spec == null ? -1 : (spec.Side == "enemy" ? 1 : spec.Side == "own" ? 0 : -1);
                _targetSideCache[card.Id] = rel;
            }
            if (rel < 0) return -1;
            return rel == 0 ? me : 1 - me;
        }

        // ==================================================================
        //  ② 打分（原版 `GetActionScore` 的分派）
        // ==================================================================

        /// <summary>
        /// 一条动作值多少分。分派照原版 `GetActionScore`：
        /// 出牌 → `ScoreFromPlayingCard` · 攻击 → `ScoreFromAttack`（**够合击斩杀再 +10000**）·
        /// 技能 → 技能自己的 criteria · `endTurn` → **0**（⇒ 分 ≤0 的动作就不做）。
        /// </summary>
        public static float ScoreAction(BattleContext ctx, AiAction a)
        {
            if (ctx == null || a == null) return 0f;
            int me = ctx.Active, foe = 1 - me;

            switch (a.Kind)
            {
                case AiActionKind.PlayCard:
                    return ScoreFromPlayingCard(ctx, a);

                case AiActionKind.AttackMelee:
                case AiActionKind.AttackRanged:
                {
                    float s = ScoreFromAttack(ctx, a);
                    // 合击够斩杀：照原版给一个压倒性的加分（`AI__GetActionScore.c:62`）
                    if (a.TargetSlot >= 0 && a.TargetP == foe)
                    {
                        var tgt = ctx.Players[foe].Board[a.TargetSlot];
                        if (tgt != null && tgt.IsWarlord && CombinedAttackCanKillWarlord(ctx))
                            s += CombinedKillBonus;
                    }
                    return s;
                }

                case AiActionKind.ActiveAbility:
                    return ScoreAbility(ctx, a);

                case AiActionKind.CollectWaystone:
                    // 原版 `GetActionScore` 对 `clickWaystone` 给的是**固定 50.0**，不看局势、不算目标。
                    return WaystoneScore;

                default:
                    return 0f;      // endTurn 基准
            }
        }

        /// <summary>
        /// 出一张牌值多少分 = **卡本身的价值** + **它写在卡面上的事值多少**。
        /// 原版：`(非法术 ? GetValueInPlay : 0) + ScoreFromPlayingNow`（`AI__ScoreFromPlayingCard.c:21-30`）。
        ///
        /// 🔴 **2026-10-19（T06 / A1797）：这一层已从「我们推导的 ScoreOps」切到原版逐卡评分表** ——
        ///    `ScoreFromPlayingNow` = `nonTargetScoringCriteria`（所有卡）＋ `cardTargetScoringCriteria`
        ///    （`targeted=1` 且带目标时），数据源 `Resources/ai_scoring.json`
        ///    ← SO 真值（判据 → `资料/普查产出_1018/AUD_T06_AI评分口径.md`）。
        ///    **缺两列时原版没有兜底**：单位仍拿 `GetValueInPlay`，战术卡就是 0（⛔ 别再造 fallback）。
        ///    `ScoreOps` 只剩「技能 / 誓约 / 替代行动」那条路在用（原版那层读 `ActiveAbility` 的
        ///    两列，而真值里没有落这两列 —— 见 `AiScoring.cs` 文件头）。
        /// </summary>
        public static float ScoreFromPlayingCard(BattleContext ctx, AiAction a)
        {
            // 第 7 行第 4 步：有实例就按实例取（枚举之后手牌变了也不会评到别的牌上）
            var card = (a.HandInst != null && a.HandInst.Card != null)
                     ? a.HandInst.Card
                     : ctx.Players[ctx.Active].Hand[a.HandIdx].Card;
            if (card == null) return 0f;

            // `IsSpellType` = `cardType 20 (Tactic) || 40 (Whispers)` ⇒ 法术不拿 GetValueInPlay
            float s = card.IsUnit ? (2f * card.Cost + 1f + AiScoring.ValueOfEnchantments(card)) : 0f;
            // 🔴 `a.Slot` 的含义**分两种**（`RuleCore.CanPlayCard` 里写着）：
            //    · **小兵** = **落点**，由 `NextDeploySlot` 定、AI 不选 ⇒ 打分**不该**受它影响；
            //    · **战术卡** = **效果打谁** ⇒ 目标参与打分（原版 `ScoreFromPlayingNow` 就是按目标算的）。
            //      目标在哪一方由 `EnumerateActions` 填进 `a.TargetP`（判据 = `EffectText.PickTarget`）。
            s += AiScoring.ScoreFromPlayingNow(ctx, card, a.HandInst, a.Slot,
                                               card.IsUnit ? -1 : a.TargetP);
            return s;
        }

        /// <summary>
        /// 一刀值多少分 = `伤害分 − 反击分`（原版 `ScoreFromBuffedAttack` 的无 buff 版）。
        ///
        /// 反击口径**照引擎**（`RuleCore.DeclareAttack:5598` 的
        /// `counterAtk = FieldAttack(ctx, tgtP, target, ranged)`）＝原版
        /// `GetCombatDamage(defender, attackType)`：**远程攻击引发的反击取守方的【远程】攻击力**
        /// （`EntityScript__GetCombatDamage.c:19-24`：`attackType == 2 ⇒ CurrentRangeAttack`，
        /// 否则 `CurrentMeleeAttack`）⇒ 这里同样走 `RuleCore.FieldAttack(ctx, foe, target, a.Ranged)`。
        /// 只有 `LongRange`（远程）与 `Sniper`（远程击杀）免 —— 别退回「远程不吃反击」那个错口径。
        /// ⚠️ **2026-10-22 订正（铁律 5）**：上面原来写「目标用**近战攻击力**反击」、实现是
        ///    `int counter = target.Attack`（`UnitState.Attack` = **当前近战**）⇒ 远程攻击时取错档
        ///    （守方远程≠近战时估值就偏）。判据与出处 →
        ///    `资料/普查产出_1022/W63_飞行与卡ID演出_调查.md` §B 最后一条。
        /// </summary>
        public static float ScoreFromAttack(BattleContext ctx, AiAction a)
        {
            if (ctx == null || a == null) return 0f;
            int me = ctx.Active, foe = 1 - me;
            var u = ctx.Players[me].Board[a.Slot];
            var target = ctx.Players[foe].Board[a.TargetSlot];
            return ScoreFromAttack(ctx, me, u, target, a.Ranged);
        }

        /// <summary>
        /// 一刀值多少分 —— **出手方显式给**的那一版（上面那条 `AiAction` 版的实现体）。
        ///
        /// 🔴 为什么拆出这一版（2026-10-23 `W64·B-01`）：原版 `AI.ScoreFromAttack` 的入口带
        ///   `attackType`，而**出手方不是隐含的「当前行动方」** —— `AI__SelectBestAttackAction.c:24`
        ///   用的是 `*(actingCard + 0x40)`（该卡自己的 side）。`Core` 的 `IAiScorer` 要按这个口径
        ///   问分（`Core/IAiScorer.cs`），所以 `side` 必须能显式传。
        ///   现有的 `AiAction` 版**行为不变**（它照旧传 `ctx.Active`，那是 AI 出招时的事实）。
        /// </summary>
        public static float ScoreFromAttack(BattleContext ctx, int side, UnitState u, UnitState target, bool ranged)
        {
            if (ctx == null || u == null || target == null) return 0f;
            if (side < 0 || side > 1) return 0f;
            int foe = 1 - side;

            int atk = RuleCore.FieldAttack(ctx, side, u, ranged);
            if (atk <= 0) return 0f;

            var tr = TraceAttack;       // 🔬 只给 `工具/ruleprobe ai` 用的读数钩子（产品恒 null）
            float s = ScoreDamaging(target, atk);       // 里面已经按「是不是督军」分了两支
            if (tr != null) tr("① 伤害分 = " + s.ToString("0.###") + "（atk=" + atk + "）");

            // 反击：取守方**本次打法那一档**的攻击力（原版 `GetCombatDamage(守方, 本次打法)`；
            //   引擎侧同一口径 = `RuleCore.DeclareAttack` 的
            //   `counterAtk = FieldAttack(ctx, tgtP, target, ranged)`）
            // 🔴 2026-10-23（引擎线 `新账-97` ①）：四道豁免的判据**只剩一处** ——
            //   `CounterAttack.NoCounter`（sneakAttack / terror / LongRange / Sniper）。
            //   这里原来自己写 `ranged && Has(LongRange)` + `sniper && WouldKill`，是第三份；
            //   ⛔ 别再写回来（改一处漏两处的先例就在本行上方那段订正里）。
            bool noCounter = CounterAttack.NoCounter(ctx, side, u, foe, target, ranged, RuleCore.WouldKill(target, atk));
            int counter = RuleCore.FieldAttack(ctx, foe, target, ranged);
            // 🔴 2026-10-26（`E4` · 判据 `AI__ScoreFromBuffedAttack.c:47-53`）：**守方带 `ambush(1200)` ⇒
            //   反击伤害被【替换成常数 4】**（原版重建一条 `DamageInfo(target, 4, …)`；**不是 +4**）。
            //   旧实现用守方的实际攻击力 ⇒ 12 张 ambush 卡的反击估值偏高。
            //   ⚠️ 这一句也决定下面 `u.Health > counter` 那个 0.1 分的闸（原版闸比的就是「替换后」的值）。
            if (target.Has(KeywordTable.Ambush)) counter = 4;
            if (!noCounter)
            {
                float before = s;
                // 🔴 2026-10-26（`E4` · 判据 §6·4 逐句复核）：原版这一笔是
                //   `ScoreFromDamagingBuffedUnit(thisCard, …, /*ignoreDroppod*/true, /*shouldCountDamagedBastion*/false)`
                //   —— **反击那一笔不把「攻方自己的堡垒」算进血量**（`bastion = shouldCountDamagedBastion ? CurrentBastion : 0`）。
                //   改前我们走默认（= 算堡垒）⇒ 带 `bastion(850)` 的攻方被反击时估值偏乐观。
                //   池内 `bastion` 现读 **0 张** ⇒ 今天零行为变化；铁律 11 照做。
                s -= ScoreDamaging(u, counter, countDamagedBastion: false);
                // 🔴 2026-10-25（`R11` · `R6-07`）：原版判据是 **`thisCard.health > tgtDmg`（扣甲【前】）**
                //   —— `AI__ScoreFromBuffedAttack.c:87-91` + `DAT_1834b2dc4 = 0.1f`（判据 §6·4）。
                //   改前是 `DamageAfterReduction(u, counter) < u.Health`（扣甲【后】）⇒ 有护甲时偏乐观
                //   （把「其实会被反杀」判成「还活着」而白给 0.1 分）。
                if (u.Health > counter) s += SurvivalBonus;
                if (tr != null)
                    tr("② 反击/存活 = " + (s - before).ToString("0.###")
                     + "（counter=" + counter + " · 存活奖励=" + (u.Health > counter ? SurvivalBonus.ToString("0.###") : "0") + "）");
            }
            else if (tr != null)
                tr("② 反击 = 0（豁免）");    // 走到这里只可能是 `noCounter`（原版没有「守方攻击 0 就跳过」这道闸）

            // ---- 毒（poisonous 380）：**只有这一下打不死时**才加分 ----
            // 判据 = `AI__ScoreFromBuffedAttack.c:96-123`（判据 §6·4）：
            //   `has(poisonous) && !t.dodge && !t.shield && !t.dropPod && atkDmg < t.health + t.CurrentBastion`
            //   ⇒ `+= ScorePoisoning(t) × 0.7f`（常量 `DAT_1834b3188`）。
            // ⚠️ 闸门里用**扣甲前**的 `atk`（原版 `atkDmg` 同样是扣甲前）。
            // ⚠️ 池内 0 张卡带 `poisonous`（现数 `cards_engine.json` 的 keywords / cardTraits 均 0）
            //    ⇒ 今天不可观测；铁律 11 照做。
            if (u.Has(PoisonousKeyword)
                && !target.Has(KeywordTable.Dodge) && !target.Has(KeywordTable.Shield)
                && !target.Has(KeywordTable.DropPod)
                && atk < target.Health + target.Bastion)
            {
                float add = ScorePoisoning(target) * PoisonousScale;
                s += add;
                if (tr != null) tr("③ 毒 = +" + add.ToString("0.###"));
            }

            // ---- 献祭（sacrifice 470）：只有「这一刀会把自己换掉」时才值钱 ----
            // 判据 = `AI__ScoreFromAttack.c:39-70`（判据 §6·3；`.c` 的结构完整）：
            //   dmg = 守方【本次打法那一档】的攻击力（近战 ⇒ CurrentMeleeAttack / 远程 ⇒ CurrentRangeAttack）
            //   if (thisCard.health <= dmg && !dodge && !shield)
            //       非 special ⇒ +2.0f ；special ⇒ + ScoreFromCriteria(rawCard.attackScoringCriteria)
            // ⚠️ 池内 0 张卡带 `sacrifice`（现数）⇒ 今天不可观测；铁律 11 照做。
            if (u.Has(KeywordTable.Sacrifice))
            {
                int back = ranged ? target.RangedAttack : target.Attack;
                if (u.Health <= back && !u.Has(KeywordTable.Dodge) && !u.Has(KeywordTable.Shield))
                {
                    float add = HasSpecialAttackScoring(u)
                              ? AttackScoringCriteriaScore(u)     // ⚠️ 数据未接 ⇒ 恒 0（见该方法 doc）
                              : SacrificeBonus;                   // 2.0f
                    s += add;
                    if (tr != null) tr("④ 献祭 = +" + add.ToString("0.###") + "（守方攻击 " + back + " ≥ 我方血 " + u.Health + "）");
                }
            }

            // ---- 爆裂 Blast X：`+= CurrentBlast × 目标的相邻单位数` ----
            // 判据 = `AI__ScoreFromAttack.c:71-78`（结构实读）+ cpp2il 的
            //   `num2 += (float)currentBlast * adjacentUnits.Count`（增量算式外推 —— 很直白）。
            // ⚠️ 相邻 = **目标**那一行的左右紧邻（原版 `BattleManager.GetAdjacentUnits(targetCard)`）
            //   ⇒ 走 `BoardSpec.AdjacentSlots`（「谁算相邻」全仓唯一判据），数**非空格**。
            // ⚠️ 池内 41 张卡带 `blast` ⇒ **这一条会真的改变 AI 出招偏好**（见 `R11` 报告读数）。
            int blast = u.KwValue("blast");              // 原版 `CurrentBlast` = trait 0x15e 的当前值
            if (blast > 0)
            {
                int adj = OccupiedAdjacentCount(ctx, foe, target);
                if (adj > 0)
                {
                    float add = blast * adj;
                    s += add;
                    if (tr != null) tr("⑤ 爆裂 = +" + add.ToString("0.###") + "（blast " + blast + " × 相邻 " + adj + "）");
                }
            }

            // ---- 标记光：**只对远程这一下加分** ----
            // 判据 = `AI__ScoreFromAttack.c:80-86`：
            //   `if (target != null && target.CurrentMarkerlight > 0 && attackType == 2) s += CurrentMarkerlight`。
            // ⚠️ 池内 0 张卡带 `markerlight`（现数）⇒ 今天不可观测。
            if (ranged)
            {
                int ml = target.KwValue("markerlight");
                if (ml > 0)
                {
                    s += ml;
                    if (tr != null) tr("⑥ 标记光 = +" + ml);
                }
            }

            return s;
        }

        /// <summary>
        /// 技能/誓约/替代行动值多少分（原版 `activeAbility` 那一支：累加它自己的 criteria）。
        /// ⚠️ 我们引擎里技能是**封闭文法**（`EffectSpec`：damage / heal / draw），誓约与替代行动是 op 表
        ///    —— 两条路分别打分，最后都汇到同一个分数域里和攻击比。
        /// </summary>
        public static float ScoreAbility(BattleContext ctx, AiAction a)
        {
            int me = ctx.Active;
            var u = ctx.Players[me].Board[a.Slot];
            if (u == null || u.Card == null) return 0f;

            // 🆕 2026-10-19（续工）：**主动技这一支也切到原版数据** ——
            //   原版 `AI.GetActionScore` case 5 读 `ActiveAbility` 自己的两列 criteria
            //   （`+0x50 cardTarget` / `+0x58 nonTarget`，`dump.cs:23766-23767`）。
            //   数据源 = `cards_raw.json` 的 `activeAbility`（1,351 张都有；生成器已切进
            //   `ai_scoring.json` 的 `activeAbility`）。
            //   原版对 `GetActiveAbility()==null` 是 **LogWarning + 0 分** ⇒ 我们同样回 0
            //   （⛔ 不再拿我们配的 `ScoreOps` 冒充）。
            var dto = AiScoringData.Get(u.Card);
            if (dto != null)
            {
                if (dto.activeAbility == null) return 0f;
                UnitState target = null;
                if (a.TargetSlot >= 0 && a.TargetP >= 0 && a.TargetP <= 1
                    && BoardSpec.IsValid(a.TargetSlot))
                    target = ctx.Players[a.TargetP].Board[a.TargetSlot];
                return AiScoring.ScoreActiveAbility(ctx, u.Card, u.Instance, me, target, dto.activeAbility);
            }

            // ---- 以下只服务**不在真值表里的卡**（我们自己那套 StarterCards / 自检夹具）----
            //   ⚠️ 这些卡没有 `ai_scoring` 条目，`GetActiveAbility()` 在原版里也没有对象可读
            //   —— 保留旧 `ScoreOps` 是为了不让自检夹具的 AI 行为突变；产品路径（池内 1,127 张）
            //   永远走上面的数据分支。
            int tgt = a.TargetSlot >= 0 ? a.TargetSlot : a.Slot;
            bool tgtPicked = a.TargetSlot >= 0;
            if (a.AltKeyword == "oath") return ScoreOps(ctx, u.Card.OathOps, tgt, tgtPicked);
            if (a.AltKeyword != null) return ScoreOps(ctx, u.Card.TriggerOps(a.AltKeyword), tgt, tgtPicked);

            var spec = u.Ability;
            if (spec == null) return 0f;

            int amount = spec.Amount;
            if (spec.IsDamage)
            {
                if (spec.Target == EffectTargets.EnemyWarlord)
                {
                    var w = ctx.Players[1 - me].Warlord;
                    return w == null ? 0f : ScoreWarlordDamage(w, amount);
                }
                if (spec.Target == EffectTargets.EnemyUnit && a.TargetSlot >= 0)
                {
                    var t = ctx.Players[1 - me].Board[a.TargetSlot];
                    return t == null ? 0f : ScoreDamaging(t, amount);
                }
                if (spec.Target == EffectTargets.Self) return 0f;        // 打自己：不加分
                return 0f;
            }
            if (spec.IsHeal)
            {
                var w = ctx.Players[me].Warlord;
                if (spec.Target == EffectTargets.OwnWarlord && w != null) return ScoreHealing(w, amount);
                if (spec.Target == EffectTargets.Self) return ScoreHealing(u, amount);
                return 0f;
            }
            return ScoreDraw(ctx);      // draw
        }

        // ------------------------------------------------------------------
        //  子打分器
        // ------------------------------------------------------------------

        /// <summary>
        /// **卡在场上的价值**（原版 `GetCardValueInPlay`，RVA 0x939030 逐指令读出来的）：
        /// `2 × rawCard.manaCost + 1 + ValueOfEnchantments`；**受伤**再
        /// `× (0.5 × 当前血/上限 + 0.5)`；**眩晕 / 压制**再 `× 0.8`。
        ///
        /// 🔴 **2026-10-19（`A2144`）订正**：原来这里加的是 `KeywordValue`（= `Σ CardTrait._aIScore`），
        ///    **那不是原版 `ValueOfEnchantments`** —— 原版数的是卡上的 `activeEffects`
        ///    （buff 属性增减 ×0.5 ＋ 附魔每条 ±1/2），`ValueOfTraits` 只服务 `ScoreFromJam`。
        ///    现在的近似见 <see cref="AiScoring.ValueOfEnchantments(UnitState)"/>（字段语义差异已标）。
        /// ⚠️ 基础费取 **`rawCard.manaCost`（卡面基础费）**，⛔ 不是 `CostOf`（当前费）——
        ///    反汇编里读的是 `[rawCard + 0x78]`。
        /// </summary>
        public static float ValueInPlay(UnitState u)
        {
            if (u == null || u.Card == null) return 0f;
            // ⚠️ 2026-10-25（`R11` · `R6-08`）：原版基础值是
            //   `rawCard.overrideDefaultValueInPlay ? (int)rawCard.manualValueInPlay : rawCard.manaCost`
            //   （判据 §6·6 `GetCardValueInPlay` / §8 `R6-08`）。我方**做不到**：`CardDef` 上
            //   **没有这两个字段**（`Core/` 本批由 `E1` 独占），`cards_engine.json` 也没有这两列。
            //   真值分布现读：`cards_raw.json` 的 `full_raw` 里 `overrideDefaultValueInPlay` **1,351/1,351 = 0**、
            //   `manualValueInPlay` 全 0 ⇒ **今天逐字等价**、无行为影响。**改动请求已写进 `R11` 报告**。
            float v = 2f * u.Card.Cost + 1f + AiScoring.ValueOfEnchantments(u);
            if (u.MaxHealth > 0 && u.Health < u.MaxHealth)
                v *= 0.5f * u.Health / u.MaxHealth + 0.5f;
            if (u.IsStunned || u.Has("pindown")) v *= 0.8f;
            return v;
        }

        /// <summary>
        /// 小兵挨这一下的分数（原版 `ScoreFromDamagingBuffedUnit`，RVA 0x93B1E0 逐指令读出来的）。
        ///
        /// **分支顺序就是原版的顺序**（⛔ 别调换 —— 前四支各自 `return` 一个常数/小值）：
        ///   ① `ambush`(1200) → **10.0**（我们原来是 **0**，`A2144` 直证偏差）
        ///   ② `invulnerable`(310) → **−10.0**（我们原来**没有这一支**，`A2144`）
        ///   ③ `dodge`(240) / `shield`(80) → `CurrentMeleeAttack / 3 + 1`
        ///      🔴 `A2144` 只记了「有这一支」，**没记它用的是【目标的近战攻击】而不是这一下的伤害** ——
        ///         本次按反汇编（`0x18093B4DB`：`EntityScript.get_CurrentMeleeAttack(target)`）订正。
        ///   ④ `longrange`(1160) → **3.0**（我们原来是 **0**，`A2144`）
        ///   ⑤ `dropPod`(230) 且 `!ignoreDroppod` → **0.5**
        ///   ⑥ 督军：致死 **1e6** / 非致死 `伤害 × (剩余血 > 15 ? 1.2 : 1.8)`
        ///   ⑦ 小兵：致死 `卡价值 × (survivor ? 0.8 : 1.0)` /
        ///      非致死 `卡价值 × 0.6 × 扣甲前伤害 / (血上限 + 堡垒)`
        ///   ⑧ 最后 `+= extraScoreFromDamaging`（致死再 `+= extraScoreFromKilling`）——
        ///      **真值里 0 张非零**（`AUD_T06` §三表 #6）⇒ 本批不读（报告已记）。
        ///
        /// 🔴 **伤害口径（2026-10-25 `R11` 第二段订正，判据 §6·5）**：原版这一族**整份都用扣甲前的
        ///   `damageValue`**（含易伤），**不是** `DamageAfterReduction` 之后的值 —— 闸（`dmg < totalCur`）
        ///   与比值（`dmg / totalMax`）读的是**同一个** `dmg`。
        ///   ⚠️ 本段原来写着「护甲那层照我们引擎改写 ⇒ 用实际伤害判死」—— **那句已废**：
        ///   它让有护甲的目标被低估（4 血 1 甲挨 4 伤：原版判「打死 = 满额」，改前判「没打死 = 0.45V」）。
        ///   现在两处都是 `PreArmorDamage`。
        /// </summary>
        public static float ScoreDamaging(UnitState target, int dmg, bool ignoreDroppod = false,
                                          bool countDamagedBastion = true)
        {
            if (target == null || !target.IsAlive) return 0f;

            if (target.Has(KeywordTable.Ambush)) return 10f;
            if (target.Has("invulnerable")) return -10f;
            if (target.Has(KeywordTable.Dodge) || target.Has(KeywordTable.Shield))
                return target.Attack / 3f + 1f;
            if (target.Has(KeywordTable.LongRange)) return 3f;
            if (target.Has(KeywordTable.DropPod) && !ignoreDroppod) return 0.5f;

            if (target.IsWarlord) return ScoreWarlordDamage(target, dmg, countDamagedBastion);

            // 🔴 2026-10-25（`R11` 第二段 · 调度台裁定「两条真缺口都做」）：
            //   **「打没打死」的闸也改成【扣甲前】**，与下面那条比值同口径。
            //   原版 `AI__ScoreFromDamagingBuffedUnit`（判据 §6·5）：
            //     `dmg = damageValue; if (CurrentVulnerable>0 && !IsVulnerableDamage(type)) dmg += CurrentVulnerable;`
            //     `if (dmg < totalCur) 比值支 else 满额支`，其中 `totalCur = currentHealth + bastion`
            //   —— 两处（闸 + 比值）读的都是**同一个扣甲前的 `dmg`**。
            //   改前闸用的是扣甲后的 `dealt` ⇒ 有护甲时分叉（4 血 1 甲挨 4 伤：原版判「打死 = 满额」，我们 0.45V）。
            //   · `buff.healthChange`：原版是「本次施加的那个 buff」，无 buff 这一路恒 0；而我们的
            //     `MaxHealth` **本来就是** `currentMaxHealthProcessed`（含全部属性 buff）
            //     ⇒ 分母 = `MaxHealth + Bastion`，⛔ 别再加一次 `healthChange`。
            int bastion = countDamagedBastion ? target.Bastion : 0;
            int totalMax = target.MaxHealth + bastion;
            if (totalMax <= 0) return 0f;      // 原版 `LogError("Trying to divide by 0! in ScoreFromDamagingUnit…") + return 0`
            int preArmor = PreArmorDamage(target, dmg);
            int totalCur = target.Health + bastion;

            float v = ValueInPlay(target);
            if (preArmor >= totalCur)                               // 打死（原版 `dmg < totalCur` 取反）
                return v * (target.Survivor > 0 ? SurvivorDiscount : 1f);
            // 没打死：`卡价值 × 0.6 × 扣甲前伤害 / (血上限 + 堡垒)`（判据 §6·5 的 `num6 / num3` 支）
            return v * MinionPartial * preArmor / totalMax;
        }

        /// <summary>
        /// 原版 `AI.ScoreFromDamagingBuffedUnit` 里那个 **`damageValue`（扣甲【前】）**：
        /// `dmg = damageInfo.damageValue; if (target.CurrentVulnerable > 0 && !IsVulnerableDamage(damageInfo.damageType))
        /// dmg += target.CurrentVulnerable;`（判据 §6·5 逐句）。
        ///
        /// ⚠️ 我们打分这一族**没有伤害类型**参数（所有调用点传的都是普通攻击/普通效果伤害）
        ///   ⇒ `IsVulnerableDamage(...)` 那道闸按「不成立」处理（= 加易伤）。真值池内 `vulnerable` = **0 张**
        ///   （现数 `cards_engine.json`），所以今天这一项恒 0；照原版补齐，不靠「反正是 0」省掉。
        /// </summary>
        static int PreArmorDamage(UnitState target, int dmg)
        {
            int v = dmg + RuleCore.VulnerableRaw(target);
            return v > 0 ? v : 0;
        }

        /// <summary>
        /// 打**督军**值多少分（原版 `ScoreFromDamagingBuffedUnit` 的英雄那一支）：
        /// 打死 → **1e6**（压倒性）；没打死 → `伤害 × (剩余血 &gt; 15 ? 1.2 : 1.8)`。
        /// 🔴 2026-10-25（`R11` 第二段）：**伤害与「剩余血」都改成【扣甲前】**，与小兵那一支同口径
        ///   （原版两处读的都是同一个 `dmg`，判据 §6·5；改前用的是 `DamageAfterReduction` 之后的 `dealt`）。
        /// </summary>
        public static float ScoreWarlordDamage(UnitState warlord, int dmg, bool countDamagedBastion = true)
        {
            if (warlord == null || !warlord.IsAlive) return 0f;
            int preArmor = PreArmorDamage(warlord, dmg);
            if (preArmor <= 0) return 0f;
            int totalCur = warlord.Health + (countDamagedBastion ? warlord.Bastion : 0);
            if (preArmor >= totalCur) return WarlordKillScore;      // 原版 `dmg < totalCur` 取反
            return preArmor * (totalCur - preArmor > 15 ? HeroDamageLow : HeroDamageHigh);
        }

        /// <summary>
        /// 治疗一个单位值多少分（原版 `ScoreFromHealingUnit`，RVA 0x93BC30 逐指令读出来的）：
        ///   · `healed = min(治疗量, 已掉的血)`；`healed ≤ 0` → **0**。
        ///   · **督军**：`当前血 > 10 ? healed : 2 × healed`（阈值 **10**；`0x18093BD33-0x18093BD45`）。
        ///   · **小兵**：`GetValueInPlay(t) × healed / 血上限 + extraScoreFromHealing`
        ///     （`0x18093BCC7` 起；`extraScoreFromHealing` 真值 0 张非零 ⇒ 不加）。
        ///
        /// 🔴 **`A2144` 订正**：原来写的是「督军恒 ×1.5」—— 那是**我们配的推断值**，
        ///    原版是 **×1 / ×2 两档**；小兵那一支原来写成「healed × 1.0」，原版是
        ///    **按卡价值 × 治疗量 / 血上限**（量级完全不同）。
        /// </summary>
        public static float ScoreHealing(UnitState u, int amount)
        {
            if (u == null || !u.IsAlive) return 0f;
            int missing = System.Math.Max(0, u.MaxHealth - u.Health);
            int healed = System.Math.Min(amount, missing);
            if (healed <= 0) return 0f;                             // 满血：一点分都不加

            if (u.IsWarlord)
                return u.Health > 10 ? healed : 2f * healed;

            if (u.MaxHealth <= 0) return 0f;
            return ValueInPlay(u) * healed / u.MaxHealth;
        }

        /// <summary>
        /// 晕一个目标值多少分（原版 `AI.ScoreFromStun`，`d:/2/tools/decomp_full/AI__ScoreFromStun.c:1-42` 逐句）。
        ///
        /// **原版结构（顺序即语义）**：
        /// <code>
        /// if (card.stunnedAtStartOfTurn(+0x55)) return 0;                      // ① 回合开局就晕着 ⇒ 再晕没用
        /// if (card.hasCantAttack(0x96) &amp;&amp; !card.hasActiveAbility(+0x220)) return 0;  // ② 本来就打不了、也没技能
        /// if (card.has(unstunnable 0xfa)) return 0;                            // ③ 免疫
        /// if (card.mightAct)  return 英雄 ? CurrentMeleeAttack : CurrentMeleeAttack × 0.5;  // ④ 它会出手
        /// return card.hasActiveAbility ? GetValueInPlay × 0.2 : 0;             // ⑤ 它不会出手
        /// </code>
        ///
        /// 🔴 **2026-10-26（`E4` · 判据复核）订正：改前的实现在两处系统性高估**（保留订正痕迹）：
        ///   旧写法 = 「`cantAttack` 或 `unstunnable` ⇒ `ValueInPlay × 0.2`；否则 `Attack × (督军 ? 1 : 0.5)`」
        ///   ⇒ ① 少了 `stunnedAtStartOfTurn` 闸；② `cantAttack` **且没有主动技** 原版是 **0**，旧实现白给
        ///   0.2×价值；③ 「不会出手且没主动技」原版是 **0**，旧实现给 0.5×攻击。两处都是**高估**。
        ///
        /// ⚠️ `mightAct` 走 <see cref="AiTargets.MightAct"/>（= `ConditionEval` 的 case 20，**唯一判据**），
        ///   ⛔ 不在这里另写一份。
        /// ⚠️ `hasActiveAbility(+0x220)` 读 **卡数据**那一格（`CardDef.HasActiveAbility`；`GateUnknown=-1` ⇒ 当假）。
        /// ⚠️ `unstunnable` 走 `RuleCore.StunBlockedByTraits`（同一判据的共用口）。
        /// </summary>
        public static float ScoreStun(BattleContext ctx, UnitState target)
        {
            if (target == null || !target.IsAlive) return 0f;
            if (target.StunnedAtStartOfTurn) return 0f;                            // ① 原版 `+0x55`
            bool hasAbility = target.Card != null && target.Card.HasActiveAbility > 0;
            if (target.Has(KeywordTable.CantAttack) && !hasAbility) return 0f;     // ②
            if (RuleCore.StunBlockedByTraits(target)) return 0f;                   // ③ unstunnable(0xfa)
            if (AiTargets.MightAct(ctx, new AiTargetRef { Unit = target, Inst = target.Instance }))
                return target.Attack * (target.IsWarlord ? 1f : 0.5f);             // ④ 它会出手
            return hasAbility ? ValueInPlay(target) * 0.2f : 0f;                   // ⑤ 只有「关掉主动技」这一点价值
        }

        /// <summary>下毒一个目标值多少分（原版 `ScoreFromPoisoning`）。</summary>
        public static float ScorePoisoning(UnitState target)
        {
            if (target == null || !target.IsAlive) return 0f;
            if (target.IsWarlord || target.Has("poisoned") || target.Has("resistant")) return 0f;
            float f = target.IsStunned ? 0.8f
                    : (target.Has(KeywordTable.CantAttack) ? 0.5f
                       : (target.Health < 3 ? 0.2f : 0.5f));
            return ValueInPlay(target) * f;
        }

        /// <summary>把一个单位弹回手牌值多少分（原版 `ScoreFromReturnToHand`：
        /// `当前费用 − (2×费+1 − 目标价值) × 1.5 − 集结/法典的 2 分`）。</summary>
        public static float ScoreReturnToHand(UnitState target, int cost)
        {
            if (target == null || !target.IsAlive) return 0f;
            float v = (2f * cost + 1f - ValueInPlay(target)) * 1.5f;
            float bonus = (target.Has(KeywordTable.Rally) ? 2f : 0f) + (target.Has(KeywordTable.Codex) ? 2f : 0f);
            return cost - v - bonus;
        }

        /// <summary>抽一张牌值多少分（原版 `ScoreFromDrawingCard` / `ScoreFromCriteria` case 35）：
        /// `手牌上限 − 手牌数 − 1`；**牌库空 ⇒ `−2 − 疲劳`**。
        ///
        /// 🔴 **`A2144` 订正**：原来写的是 `−2 − HandMax`（手牌上限），
        ///    而 `A2144` 那条账写成「原版是 `−2 − 手牌张数`」—— **那句也是误读**。
        ///    反汇编 RVA 0x93B71B / 0x18093B08B：`mov eax,-2; sub eax,ebp`，而 `ebp` 是
        ///    `ObscuredInt.op_Implicit(BattleManager.playerFatigue/enemyFatigue)` 的结果
        ///    （`0x18093B6DB` / `0x18093B03B` 那两处）⇒ **减的是疲劳值**。
        ///    判据只此一处 = <see cref="AiScoring.DrawValue"/>。
        /// </summary>
        public static float ScoreDraw(BattleContext ctx)
        {
            return AiScoring.DrawValue(ctx, ctx.Active);
        }

        /// <summary>
        /// 关键词分值表（原版 `ScoreFromBuff` 的 trait → 分值表，44 行，见正本 §五·三）。
        /// ⚠️ 只列**我们卡池里真有的**那些（其余原版也是 0 分）；名字用我们 `KeywordTable` 的写法。
        /// </summary>
        public static float TraitScore(string keyword)
        {
            switch (keyword)
            {
                case KeywordTable.Shield: return 2.0f;
                case KeywordTable.Vanguard: return 2.0f;
                case KeywordTable.Stealth: return 1.5f;
                case "pack": return 1.5f;
                case "camouflage": return 1.5f;          // 无 const：照 `RuleCore` 的写法用字面量
                case KeywordTable.LongRange: return 1.5f;
                case KeywordTable.Backlash: return 1.0f;
                case KeywordTable.Flying: return 1.0f;
                case "shuriken": return 0.0f;
                case "vulnerable": return -1.0f;
                case "markerlight": return -1.0f;
                case "stun": return -1.5f;
                case "blind": return -1.5f;
                case KeywordTable.CantAttack: return -2.0f;
                default: return 0.0f;                                // 原版：**没列出的就是 0 分**
            }
        }

        /// <summary>一张卡上所有关键词的分值合计（原版 `ValueOfTraits` = `Σ CardTrait._aIScore`）。
        /// ⚠️ 原版那个 `_aIScore` 是**挂在数据资产上**的 ⇒ 这里用 <see cref="TraitScore"/> 那张表，
        /// 数值来自反编译（见正本 §五·三），**表本身是原版的**。</summary>
        public static float KeywordValue(CardDef card)
        {
            if (card == null || card.Keywords == null) return 0f;
            float sum = 0f;
            foreach (var kv in card.Keywords)
                if (kv.Value > 0) sum += TraitScore(kv.Key.ToLowerInvariant());
            return sum;
        }

        // ------------------------------------------------------------------
        //  卡面效果 → 分（原版 `ScoreFromPlayingNow` → `ScoreFromCriteria`）
        // ------------------------------------------------------------------

        /// <summary>
        /// 一串效果的分数合计。**2026-10-19 起产品路径只剩「技能 / 誓约 / 替代行动」在用**
        /// （`ScoreAbility`）—— **出牌**已经切到原版逐卡评分表（<see cref="AiScoring"/>），
        /// ⛔ 别把它再接回 `ScoreFromPlayingCard`（原版没有「正文→分」这一层）。
        /// 老口径：原版是遍历卡资产上的 `ScoringCriteria` 表（`RawCardScript 0x2E8/0x2F0`），
        /// 我们从**自己解析出来的 `EffectOp`** 推（见 <see cref="ScoreOp"/>）——
        /// ⚠️ 这一层的系数**是我们配的**，只用于技能那一支（`ActiveAbility` 两列真值未落表的替代品）。
        /// </summary>
        public static float ScoreOps(BattleContext ctx, IReadOnlyList<EffectOp> ops, int slot,
                                     bool slotIsTarget = false)
        {
            if (ctx == null || ops == null) return 0f;
            float sum = 0f;
            foreach (var op in ops) sum += ScoreOp(ctx, op, slot, slotIsTarget);
            return sum;
        }

        /// <summary>一条 op 值多少分（<see cref="ScoreOps"/> 的逐条版）。
        ///
        /// <paramref name="slotIsTarget"/>（🆕 2026-09-29，Q6）= **<paramref name="slot"/> 是「这一下打谁」**，
        /// 不是「施放者/落点在哪」。原版 AI 就是这么算的：`AI__ScoreFromPlayingNow.c` 拿
        /// `param_4`（= 这条动作的**目标**）去 `FilterMethods.CheckIfMeetsCriteria` 逐条筛卡的 criteria
        /// ⇒ **同一张卡打不同目标 = 不同的分**。
        ///
        /// 🔴 **我们原来没这一层** ⇒ `slot` 形参函数体里**一次都没读** ⇒ 同一张战术卡的所有格位
        /// **同分**、平手留先（`NextPlay` / `SelectBestAction` 都是严格 `>`）⇒ **永远打「槽号最小的合法目标」**。
        /// ⚠️ **2026-10-24 订正（铁律 5）**：本段原来接着写「后果是**裸 `Deal N damage` 不会打最低血的那个**
        /// （用户 2026-09-18 拍的『裸伤害走 `lowestHealth`』在 AI 出牌这条路上没生效）」—— **那个前提本身已被证伪**：
        /// 全池 **33/33** 张裸 `Deal N damage` 卡是 **`ta = 30 (target)`**（由施放方选一个目标；卡面没写
        /// `with lowest Health`），「生命最低」是**另一档** `ta = 240 lowestHealth`（全池只有 `SAU70` / `SAU73`
        /// 那两张明写 `with lowest Health` 的卡走它）。判据 → `EffectText.TryDeal` 的裸 Deal 支（`G5` 改后
        /// = `Auto = false` + `Count = 1`，`Raw` 写的是「未写目标短语：原版 ta=30 target ⇒ 由施放方选一个敌方单位」）
        /// ＋ `资料/普查产出_1024/G5_裸Deal与ta200.md` §1·1。
        /// ⇒ 现在这条**不是「没生效」而是「原版本来就不按最低血挑」**：AI 对**每个合法目标各发一条动作**、
        /// 按评分挑最高（`EnumerateActions` → `ScoreFromPlayingCard` → `AiScoring.ScoreFromPlayingNow`），
        /// 本函数下面 `slotIsTarget` 那几支就是「按这一格的目标算分」的入口。见正本 §10·9 ②。
        /// </summary>
        public static float ScoreOp(BattleContext ctx, EffectOp op, int slot, bool slotIsTarget = false)
        {
            if (op == null) return 0f;
            int me = ctx.Active, foe = 1 - me;
            var foeBoard = ctx.Players[foe].Board;
            var mineBoard = ctx.Players[me].Board;
            var myW = ctx.Players[me].Warlord;

            switch (op.Verb)
            {
                case "deal":
                {
                    int dmg = op.Amount > 0 ? op.Amount : 1;
                    bool hitsAll = op.Target != null && op.Target.Count == 0;
                    // 🆕 目标已选定（战术卡：`slot` = 「效果打谁」）⇒ **就按那一格算**
                    //    （原版就是按目标算的）。落了空才退回下面的「全场最好」兜底 —— 不猜归属。
                    if (slotIsTarget && !hitsAll && BoardSpec.IsValid(slot))
                    {
                        var tu = foeBoard[slot];
                        if (tu != null && tu.IsAlive) return ScoreDamaging(tu, dmg);
                    }
                    float best = 0f; int n = 0;
                    // ⚠️ **督军也在 `Board[WarlordSlot]` 里**（`RuleCore.NewBattle` 把它放进棋盘、
                    //    和 `PlayerState.Warlord` 是同一个对象）⇒ 这里**不要再单独算一次督军**，
                    //    否则 `all` 那种群伤会把目标数算重。
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = foeBoard[t];
                        if (tu == null || !tu.IsAlive) continue;
                        n++;
                        float v = ScoreDamaging(tu, dmg);
                        if (v > best) best = v;
                    }
                    return hitsAll ? best * System.Math.Max(1, n) : best;
                }

                case "heal":
                {
                    int amount = op.Amount > 0 ? op.Amount : 1;
                    if (slotIsTarget && BoardSpec.IsValid(slot))
                    {
                        var tu = mineBoard[slot];
                        if (tu != null && tu.IsAlive) return ScoreHealing(tu, amount);
                    }
                    float best = myW != null ? ScoreHealing(myW, amount) : 0f;
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = mineBoard[t];
                        if (tu == null || tu.IsAlive == false) continue;
                        float v = ScoreHealing(tu, amount);
                        if (v > best) best = v;
                    }
                    return best;
                }

                case "draw": case "drawtype": case "drawref":
                    return System.Math.Max(1, op.Amount) * ScoreDraw(ctx);

                case "destroy":
                {
                    // 🆕 目标已选定 ⇒ 就按那一格算（同 `deal`，原版按目标算）
                    if (slotIsTarget && BoardSpec.IsValid(slot))
                    {
                        var tu = foeBoard[slot];
                        if (tu != null && tu.IsAlive) return ValueInPlay(tu);
                    }
                    float best = 0f;
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = foeBoard[t];
                        if (tu == null || !tu.IsAlive) continue;
                        float v = ValueInPlay(tu);                      // 直接摧毁 = 按它的价值算
                        if (v > best) best = v;
                    }
                    return best;
                }

                case "stun":
                {
                    if (slotIsTarget && BoardSpec.IsValid(slot))
                    {
                        var tu = foeBoard[slot];
                        if (tu != null && tu.IsAlive) return ScoreStun(ctx, tu);
                    }
                    float best = 0f;
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = foeBoard[t];
                        if (tu == null || !tu.IsAlive) continue;
                        float v = ScoreStun(ctx, tu);
                        if (v > best) best = v;
                    }
                    return best;
                }

                case "return":
                    return 0f;        // 弹回手牌：原版按「目标当前费用」算，我们的 op 里没有那个数 ⇒ 不猜，记 0

                case "give": case "gain":
                {
                    float g = PayloadScore(op.Payload) * System.Math.Max(1, op.Amount);
                    // 🔴 **2026-10-18 之后·第十二会话（`A1098`）：`give fast` 另有原版专门的一档。**
                    //   原版那条判据挂在卡资产的 `ScoringCriteria.scoringOption` 上
                    //   （`ScoringCriteria.ScoringOption.SumValueOfGivingCharge = 30`，
                    //   `AI__ScoreFromCriteria.c` 的 `case 0x1e`）；我们手上没有那份数据
                    //   （在服务端）⇒ 用「载荷里给了 `fast`」当触发条件，见 `ScoreGivingCharge`。
                    //
                    // 🔴 **2026-10-11（`A1311`）：`slot` 是【哪一侧】棋盘上的格号 —— 由目标的侧说了算。**
                    //   `slotIsTarget == true` 那条路（战术卡）传进来的 `slot` 是「**效果打谁**」
                    //   （口径见 `ScoreFromPlayingCard` 上面那段），而它落在**对方**棋盘还是
                    //   **自己**棋盘上，只有目标的侧知道 ——
                    //   原来硬编码成 `me`（= `mineBoard`）⇒ 全池唯一带 `fast` 的战术卡
                    //   `Telephatic Domination`（`Take control of an enemy troop this turn and
                    //   give it Fast`）读到的是**我这边同一格号上的另一个无关单位**。
                    //   侧从 `EffectTargetSpec.ResolvedSide` 取（判据只此一处）——
                    //   代词那个侧是解析层 `LinkPrevAntecedent` 从**紧邻上一条 op** 抄来的
                    //   （那一条是 `takecontrol … an enemy troop` ⇒ `enemy`）。
                    //   ⛔ **有目标、但侧判不出来（`any` / `eventtarget` / 代词没先行词）⇒ 不给这一档分**，
                    //      绝不回落到「就当是自己人」（那是「拿自己顶」 = 静默读错一个单位）。
                    //   ⚠️ **`Target == null`**（解析器没给出目标规格：光环合成的那条 `give` op、
                    //      自检夹具）走**旧口径**那条重载 —— 简报没覆盖这一格，这里**保持行为不变**、
                    //      不新发明语义。实测：全池「战术/防御卡 + `Target == null`」的 `give`/`gain`
                    //      只有 **1** 条（`Angels of Death` 的 `+1 ranged`），**载荷含 `fast` 的 0 条**。
                    if (PayloadIsCharge(op.Payload) && slotIsTarget && BoardSpec.IsValid(slot))
                    {
                        if (op.Target == null) g += ScoreGivingCharge(ctx, me, slot);
                        else if (op.Target.ResolvedSide != null)
                            g += ScoreGivingCharge(ctx, me, slot, op.Target.ResolvedSide);
                    }
                    return g;
                }

                case "blind":
                    return 1.0f;      // 失明＝远程攻击力算 0（原版给目标的负分是 −1.5）—— 我们配的小正值

                case "lowercost":
                    return op.Amount * 1.0f;    // 降费：原版没有对应 criteria，**我们配的**

                case "gainenergy": case "refill":
                    return 1.0f * System.Math.Max(1, op.Amount);

                default:
                    // 没写的（`deploy` / `create` / `repeat` / `chooseone` …）一律 0 ——
                    // **不猜**。要给它分值就得先有原版依据，见正本 §八。
                    return 0f;
            }
        }

        /// <summary>
        /// `give` / `gain` 的载荷值多少分。载荷原文形如 `+1 attack` / `armour 2` / `shield` / `flank`。
        /// 属性增减益的系数**是我们配的**（原版 `ScoreFromBuff` 的 `addStats` 出口是
        /// `(近战+远程+血)` 的原始和 —— 那个口径直接搬过来会跟卡价值量级不搭）；
        /// 关键词那半**照原版的表**（<see cref="TraitScore"/>）。
        /// </summary>
        public static float PayloadScore(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return 0f;
            string s = payload.Trim().ToLowerInvariant().Replace("+", " ").Replace("-", " ");

            string[] parts = s.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            int num = 0; string word = null;
            for (int i = 0; i < parts.Length; i++)
            {
                int v;
                if (int.TryParse(parts[i], out v)) { num += v; continue; }
                if (word == null) word = parts[i];
            }
            if (word == null) return 0f;
            if (num <= 0) num = 1;

            switch (word)
            {
                case "attack": case "melee": return num * 1.0f;
                case "ranged": return num * 1.0f;
                case "health": return num * 0.7f;
                case "armour": case "armor": return num * 0.6f;
                case "cost": return num * 0.5f;
                default: return TraitScore(word);      // 关键词：整条载荷算一份（原版那张表）
            }
        }

        /// <summary>载荷里是不是给了 `fast`（= 原版 `ScoringOption.SumValueOfGivingCharge`
        /// 认的那一档「给迅捷/冲锋」）。载荷原文形如 `fast` / `+1 attack` / `armour 2`。</summary>
        public static bool PayloadIsCharge(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return false;
            string s = payload.Trim().ToLowerInvariant().Replace("+", " ").Replace("-", " ");
            foreach (string w in s.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries))
                if (w == "fast") return true;
            return false;
        }

        /// <summary>
        /// **给一个单位「冲锋 / 迅捷」值多少分**。判据 = 原版 `AI__ScoreFromGivingCharge`
        /// （`d:/2/tools/decomp_full/AI__ScoreFromGivingCharge.c`，现读）：
        /// ```
        /// cVar1 = CardScript__CanActNow(param_2);
        /// if ((cVar1 == '\0') && (*(char *)(param_2 + 0x58) != '\0')) { … get_CurrentMeleeAttack … }
        /// ```
        /// ⇒ **先读 `CanActNow`、为假【再】读裸 `+0x58`** —— **两位一起用**，这正是
        /// `UnitState.SummonSickness` 必须单独存在的理由之一：`Exhausted` 把
        /// 「不能动，因为刚上场」与「不能动，因为已经动过」挤在同一位里，AI 就分不出
        /// 下面这一句「给它冲锋值多少」。
        /// `AI__ScoreFromCriteria.c:289-302`（`case 0x1e` = `SumValueOfGivingCharge`）**同形**，
        /// 逐目标取一遍这个值 ⇒ 这里是**同一份判据的唯一入口**（两处共用，别各写一份）。
        ///
        /// 🔴 **`CanActNow` 走引擎那个共用口**（`RuleCore.CanActNow` = `CanAttackNow` 的近战/远程各问一次），
        ///    ⛔ 别在这儿重写一份「`!u.Exhausted && !u.IsStunned`」（两处写同一条规则 = 迟早不一致）。
        /// ⚠️ **取值 = 当前的近战攻击力**（`EntityScript.get_CurrentMeleeAttack`）⇒ 用 `u.Attack`
        ///    （buff 后的值），⛔ **不是**卡模板上的那个数。
        ///
        /// 🔴 **2026-10-11（`A1311`）：目标在哪一侧是【参数】，不再是「施放者自己那一侧」。**
        ///    `give` 这一档的 `slot` 是「**效果打谁**」的格号（战术卡那条路），
        ///    它可能落在**对方**棋盘上（`Telephatic Domination` 就是）——
        ///    原来固定读 `ctx.Players[p].Board[slot]`（`p` 恒传 `ctx.Active`）
        ///    ⇒ 读到的是**我这边同一格号上的另一个无关单位**。
        /// </summary>
        /// <param name="p">**施放者**（我方）的玩家下标 —— 调用点恒传 `ctx.Active`。
        ///   ⚠️ `A1311` 之前它的含义是「目标所在棋盘的主人」（两个含义在旧口径下同值，
        ///   所以那三处老调用点无需改）；现在 `p` 只说「**谁在给**」。</param>
        /// <param name="side">目标在哪一侧（<see cref="EffectTargetSpec.ResolvedSide"/>）：
        ///   `"own"` / `"enemy"`。别的值（含 `null` = **判不出来**）⇒ 返回 `0f`
        ///   —— ⛔ **不许回落到「自己那侧」**（工程红线：宁可认不出，也别静默读错一个单位）。</param>
        public static float ScoreGivingCharge(BattleContext ctx, int p, int slot, string side)
        {
            if (ctx == null || !BoardSpec.IsValid(slot)) return 0f;
            if (side != "own" && side != "enemy") return 0f;       // 判不出侧 ⇒ 不给分（⛔ 不猜）
            int owner = side == "enemy" ? 1 - p : p;
            var u = ctx.Players[owner].Board[slot];
            if (u == null || !u.IsAlive) return 0f;
            if (RuleCore.CanActNow(ctx, owner, slot)) return 0f;   // 原版 `CanActNow` 为真 ⇒ 不给分
            if (!u.SummonSickness) return 0f;                      // 原版那第二个合取项（裸 `+0x58`）
            return u.Attack;
        }

        /// <summary>**旧口径重载**（`A1311` 之前唯一的那一条）：`p` **既当施放者、也当目标所在那一侧**。
        ///
        /// 🔴 **只留给「这条 op 根本没有目标规格」的调用点** —— 解析器没给出目标时没有「侧」可判，
        ///    这一支**保持行为不变**（光环合成的那条 `give` op · 自检夹具）。
        ///    ⛔ **新调用点别用它**：有目标规格的一律走 4 参那条，侧取
        ///    <see cref="EffectTargetSpec.ResolvedSide"/>。
        /// </summary>
        public static float ScoreGivingCharge(BattleContext ctx, int p, int slot)
        {
            return ScoreGivingCharge(ctx, p, slot, "own");
        }

        // ==================================================================
        //  ③ 斩杀线（原版 `CombinedAttackCanKillWarlord`）
        // ==================================================================

        /// <summary>
        /// 这一回合**最多能对对面督军造成多少伤害**。
        ///
        /// 把「还能动的单位」逐个数一遍，每个单位取它**能打出的最大那一份**：
        ///   · **攻击**：近战 / 远程两路各问一次引擎（`IsValidTarget`），取**攻击力**高的那一路
        ///     —— 一个单位一回合只出一刀，**不能两路相加**。
        ///
        /// 🔴 **2026-10-23（`W64 B-02`）已改成原版口径，两处旧偏差都修掉了**（判据 =
        ///    `decomp_full/AI__CombinedAttackCanKillWarlord.c` 全文，现读）：
        ///   ① **不再累加「打脸的伤害技能」**。原版只读**攻击动作**（`:60-125` 的
        ///      `action.attackType ∈ {1 近战, 2 远程}`，其它类型直接跳过），技能根本不在这个列表里。
        ///      旧注释写「技能二选一，算进去才是真上界」——那是**我们自己放宽的**，不是原版。
        ///   ② **不再过 `RuleCore.DamageAfterReduction`**。原版累加的是
        ///      `GetCurrentMeleeAttack` / `GetCurrentRangeAttack` **原始攻击力**（`:140-155`），
        ///      最后只跟 `督军 Health + CurrentBastion`（`:160-185`）比 —— **护甲 / 盾 / 易伤一律不参与**。
        ///      ⇒ 我们以前「带盾的督军一律算 0」那条**偏差也随之消失**（不是靠补模型，是照原版不套减伤）。
        ///      ⚠️ 结果是：这个数**不是**「实际会掉多少血」，而是原版那个**上界估计**；
        ///      别拿它当伤害预测用（真要预测走 `DamageAfterReduction`）。
        /// ⚠️ **定死的规则，不掷骰** —— 种子只该影响洗牌（一局必须可复现）。
        /// </summary>
        public static int DamageToFoeWarlord(BattleContext ctx)
        {
            return ctx == null ? 0 : DamageToFoeWarlord(ctx, ctx.Active);
        }

        /// <summary>同上的**显式出手方**版（2026-10-23 `W64·B-01`）。
        /// 原版这一路从 `action.actingCard` 的 side 取值（`AI__SelectBestAttackAction.c:24`），
        /// 不是隐含的「当前行动方」⇒ `Core` 的 `IAiScorer` 要能按 side 问。
        /// ⚠️ 桩/夹具里出现 `side` 越界 ⇒ 返回 0（⛔ 别静默按 `ctx.Active` 顶替）。</summary>
        public static int DamageToFoeWarlord(BattleContext ctx, int side)
        {
            if (ctx == null || ctx.IsOver) return 0;
            if (side < 0 || side > 1) return 0;
            int me = side, foe = 1 - me;
            var myBoard = ctx.Players[me].Board;
            var foeW = ctx.Players[foe].Warlord;
            if (foeW == null) return 0;

            int total = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = myBoard[s];
                if (u == null || u.Exhausted || u.IsStunned) continue;

                // 一个单位**只算一刀**、取它**能打出的最高那一路**（原版按攻击动作去重，
                // 同一攻击者保留攻击力最高的那一条 —— `…CanKillWarlord.c:80-125` 的
                // `List.Find` + `Remove` 那一段）。
                int best = 0;
                for (int ranged = 0; ranged <= 1; ranged++)
                {
                    int atk = RuleCore.FieldAttack(ctx, me, u, ranged == 1);
                    if (atk <= 0) continue;
                    if (RuleCore.IsValidTarget(ctx, me, s, foe, BoardSpec.WarlordSlot,
                                               ranged == 1) != RuleCodes.OK) continue;
                    if (atk > best) best = atk;
                }
                total += best;
            }
            return total;
        }

        /// <summary>这一回合够不够把对面督军打死（= 斩杀线到了）。
        ///
        /// 🔴 **2026-10-23（`W64 B-02`）改成原版口径**：`…CanKillWarlord.c:160-185` 的闸是
        /// `总伤害 < 督军 Health(+0x68) + CurrentBastion` ⇒ **打不死**；否则报一句 LogWarning 返回 1。
        /// ⇒ 我们这边 = `总伤害 >= Health + Bastion`。
        /// 本笔之前漏了 `Bastion` 那一项（带堡垒的督军会被判成「这一回合就能打死」）。</summary>
        public static bool CanLethal(BattleContext ctx)
        {
            return ctx != null && CanLethal(ctx, ctx.Active);
        }

        /// <summary>同上的**显式出手方**版（2026-10-23 `W64·B-01`；理由见 `DamageToFoeWarlord(ctx, side)`）。</summary>
        public static bool CanLethal(BattleContext ctx, int side)
        {
            if (ctx == null || ctx.IsOver) return false;
            if (side < 0 || side > 1) return false;
            var foeW = ctx.Players[1 - side].Warlord;
            return foeW != null && foeW.Health > 0
                   && DamageToFoeWarlord(ctx, side) >= foeW.Health + foeW.Bastion;
        }

        /// <summary>原版 `CombinedAttackCanKillWarlord` 的名字，判据就是 <see cref="CanLethal"/>（**只此一处**）。</summary>
        public static bool CombinedAttackCanKillWarlord(BattleContext ctx) { return CanLethal(ctx); }

        /// <summary>同上、**显式出手方**版（`W64·B-01`：`IAiScorer` 的评分里那 `+10000` 要用它）。</summary>
        public static bool CombinedAttackCanKillWarlord(BattleContext ctx, int side) { return CanLethal(ctx, side); }

        // ==================================================================
        //  ④ 难度：随机挑掉最低分的动作（原版 `TweakAvailableActions`）
        // ==================================================================

        /// <summary>三个难度旋钮（原版在 `AIBotsConfig.BotConfig` 里：`skipChance / minSkips / maxSkips`）。</summary>
        public struct Knobs
        {
            public int MinSkips;      // 无条件删掉最低分的几条
            public int MaxSkips;      // 最多删几条
            public int SkipChance;    // 之后每条以这个百分比（0-100）被删
        }

        /// <summary>
        /// 难度 → 旋钮。
        /// 🔴 **这四个档的取值是我们配的**：原版那三旋钮的**值**在远端 LiveOps 配置
        /// `AIBotsConfig.BotConfig`（`GetNormalBotConfig` 按 `minRank..maxRank` + `minLossStreak..maxLossStreak`
        /// 在 `BotList` 里挑一条、**命中就用它、一条都不命中就用 `[0]`**；`GetFTUEBotConfig` 按
        /// `CurrentModeVictories == winCount` 挑；`GetFastModeBotConfig` 同 Normal 但查 `BotListFastMode` ——
        /// 判据 `d:/2/tools/decomp_full/MatchData__GetNormalBotConfig.c` / `__GetFTUEBotConfig.c` /
        /// `__GetFastModeBotConfig.c`）。
        /// **本机拿不到那份数据**：`d:/2/新解包资源/assets_full`、`d:/2/关服前存档`（含安卓 Unity 缓存）
        /// 全盘搜 `skipChance` / `minSkips` / `BotListFTUE` / `AIBotsConfig` ⇒ **0 个文件**（2026-10-26 现搜）。
        /// ⇒ 下面四个档的数值**是我们挑的，不是原版值** —— ⛔ 别在别处当成原版。
        /// 结构照原版：**越简单 = 砍得越狠**（砍掉的都是分最低的那些，所以 AI 会做出菜的选择）。
        ///
        /// ⚠️ **`MinSkips` 一律给 0**（原版那个值是「无条件先砍掉几条」）—— 给正数的话，
        ///    当 AI 只剩一两条动作时会把它们**全砍光**，表现成「对手发呆、什么都不做」。
        ///    改成「每条按概率砍 + 最多砍 N 条」就没有这个死角，而难度梯度照样在。
        /// </summary>
        public static Knobs KnobsOf(AiDifficulty d)
        {
            switch (d)
            {
                case AiDifficulty.SuperEasy: return new Knobs { MinSkips = 0, MaxSkips = 3, SkipChance = 90 };
                case AiDifficulty.Easy:      return new Knobs { MinSkips = 0, MaxSkips = 2, SkipChance = 60 };
                case AiDifficulty.Normal:    return new Knobs { MinSkips = 0, MaxSkips = 1, SkipChance = 30 };
                default:                     return new Knobs { MinSkips = 0, MaxSkips = 0, SkipChance = 0 };
            }
        }

        /// <summary>
        /// 难度旋钮（原版 `AI.TweakAvailableActions`，`d:/2/tools/decomp_full/AI__TweakAvailableActions.c:1-144`）。
        ///
        /// **原版形状（逐句，2026-10-26 `E4` 复核）**：
        /// <code>
        /// if (FeatureConfig.ShouldDisableNewMatchMaking()) return list;            // 远端开关（我们无此开关 ⇒ 恒不成立）
        /// switch (MatchType.Effective) { … 见 ShouldDisableTweak … }               // **模式闸**（:22-52）
        /// minSkips / maxSkips / skipChance = MatchData.GetBotConfig{Min,Max}Skips / GetBotConfigSkipChance
        /// list = currentActions.OrderByDescending(a =&gt; a.actionScore).ToList()     // 降序
        /// for (round = 0; round &lt; maxSkips; round++) {                             // **恰好 maxSkips 轮**
        ///     for (i = list.Count-1; i &gt;= 0; i--) {                                // 每轮**从表尾重扫**
        ///         if (list[i].actionType == 4 /*endTurn*/) continue;               // 跳过基准
        ///         if (round &lt; minSkips) { 删; break; }                            // 前 minSkips 轮**无条件删**
        ///         if (skipChance &gt; GetRandomInt(0,100)) { 删; break; }           // 掷骰命中才删
        ///         // 没命中 ⇒ 继续往前扫（**不回滚、不重掷当前这条**）
        ///     }
        ///     // 扫到底还没删成 ⇒ 直接进下一轮
        /// }
        /// </code>
        /// ⇒ 关键两点：① **删掉一条后下一轮从表尾重扫** ⇒ 上面的条目会被**重新掷骰**（这就是原版的 RNG
        /// 消耗比「每条只掷一次」多的原因）；② `endTurn` **永远不删**。
        ///
        /// 🔴 **2026-10-26（`E4`）改前 vs 改后**：改前是「单趟、每条只掷一次、最多删 `MaxSkips` 条」
        /// —— 语义结局相同（都砍最低分、上限 `MaxSkips`），但**随机数消耗序列不同**（原版会重掷）。
        /// 现在按原版形状重写；随机源仍是 <see cref="BattleContext.AiRng"/>（种子派生，同一局可复现）。
        /// ⚠️ **三个旋钮的「值」仍取不到**（`AIBotsConfig.BotConfig` 在远端 LiveOps 配置里，
        ///   本地 `assets_full` / 关服前存档 0 命中）⇒ 见 `KnobsOf`，仍是**我们挑的常量**。
        /// </summary>
        public static void TweakAvailableActions(BattleContext ctx, List<AiAction> list, AiDifficulty d)
        {
            if (ctx == null || list == null) return;
            if (ShouldDisableTweak(ctx)) return;                  // 原版模式闸（`ShouldDisableTweakAvailableActionsByMode`）
            var k = KnobsOf(d);
            if (k.MaxSkips <= 0 || list.Count <= 1) return;

            list.Sort((x, y) => y.Score.CompareTo(x.Score));      // 降序（原版 `OrderByDescending(actionScore)`）

            // 原版：**共 `MaxSkips` 轮，每轮从表尾（最低分）往前扫一次**；删掉一条就进下一轮。
            for (int round = 0; round < k.MaxSkips; round++)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].Kind == AiActionKind.EndTurn) continue;      // 基准永不删
                    bool cut = round < k.MinSkips                            // 前 minSkips 轮：无条件
                             || k.SkipChance > ctx.AiRng.Next(0, 100);       // 之后：原版 `skipChance > roll`
                    if (!cut) continue;                                      // 没命中 ⇒ 继续往前扫
                    list.RemoveAt(i);
                    break;                                                   // 删一条 ⇒ 进下一轮（**从表尾重扫**）
                }
            }
        }

        /// <summary>
        /// 挑最好的一条（原版 `SelectBestAction`）：先拿 `endTurn` 当基准，
        /// **严格大于才换**（平手保留先出现的 —— 所以**这个过程本身不掷骰**）；
        /// 唯一的例外：当前最佳还是 `endTurn` 而候选是攻击时，攻击胜出 ——
        /// **除非**出手单位是英雄 / 带 `stealth` / `vanguard` / 特殊攻击打分（原版那四类排除），
        /// 带 `berzerk` 的则**一定**胜出。
        /// </summary>
        public static AiAction SelectBestAction(BattleContext ctx, List<AiAction> list)
        {
            if (list == null || list.Count == 0) return null;

            AiAction best = null;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Kind == AiActionKind.EndTurn) { best = list[i]; break; }
            if (best == null) best = list[0];

            foreach (var cur in list)
            {
                if (cur.Kind == AiActionKind.EndTurn) continue;
                if (cur.Score > best.Score) { best = cur; continue; }
                if (best.Kind == AiActionKind.EndTurn
                    && (cur.Kind == AiActionKind.AttackMelee || cur.Kind == AiActionKind.AttackRanged)
                    && KillerOverride(ctx, cur)) best = cur;
            }
            return best;
        }

        /// <summary>原版 `CardScript.HasSpecialAttackScoring()` =
        /// `rawCardData.specialAttackScoringCriteria`（`+0x2F8`）**∧ 当前不带 `jam`**
        /// （`decomp_full/CardScript__HasSpecialAttackScoring.c`，逐句：先读那个 bool、为假直接 false，
        /// 再 `HasCurrentTrait(jam)` ⇒ `!jam`）。
        /// <para>**消费点**：<see cref="KillerOverride"/> 的排除项 —— 原版 `AI.SelectBestAction:702` 与
        /// `AI.IsBetterAction:822` 都是「这类卡**不参与**『宁可出手也不结束回合』那个例外」。
        /// ⚠️ 真值现读 1,351 张**全 0** ⇒ 今天恒 `false`、无可观测差异（数据出现即自动生效）。</para></summary>
        public static bool HasSpecialAttackScoring(UnitState u)
        {
            if (u == null || u.Card == null) return false;
            return u.Card.SpecialAttackScoring && !u.Has(KeywordTable.Jam);
        }

        /// <summary>原版 `SelectBestAction` 里那个例外：这一刀哪怕分不高也该打出去。</summary>
        static bool KillerOverride(BattleContext ctx, AiAction a)
        {
            var u = ctx.Players[ctx.Active].Board[a.Slot];
            if (u == null) return false;
            if (u.Has("berzerk")) return true;                        // 原版：狂暴一定出手
            if (u.IsWarlord) return false;                            // 督军不参与这个例外
            if (u.Has(KeywordTable.Stealth) || u.Has(KeywordTable.Vanguard)) return false;
            // 🆕 2026-10-25（账 3 条 C · 判据 `CardScript__HasSpecialAttackScoring.c`）：
            //   带「特殊攻击打分」的卡**不参与**这个例外（原版 `AI.cs:702` / `:822` 同一形状）。
            if (HasSpecialAttackScoring(u)) return false;
            return true;
        }

        // ==================================================================
        //  ⑤ 一步（原版 `PlayTurn`）
        // ==================================================================

        /// <summary>
        /// **做一步**（原版 `PlayTurn` 的形状：一次调用只挑一条动作）。
        /// 返回 false = 这回合没动作可做（最佳是 `endTurn`）—— 调用方该收手了。
        /// 返回 true 时 <paramref name="best"/> 就是要执行的那条。
        /// ⚠️ **只挑不动手** —— 执行仍由调用方（表现层 / 自检）走 `RuleCore.*`，这样才播得出动画。
        /// </summary>
        public static bool NextAction(BattleContext ctx, AiDifficulty diff, out AiAction best)
        {
            return NextAction(ctx, diff, null, out best);
        }

        /// <summary>
        /// 同上，但可以**把若干条动作排除掉**（🆕 2026-09-29，Q6 的「退次优」那一半）。
        ///
        /// 🔴 **为什么需要**：原版 `ExecuteAction` 失败只是 `return false` → `PlayTurn` 里 `break`
        /// ⇒ **不换目标重试、也不退次高分**（用户 2026-09-29 点名）。我们发现引擎拒绝之后
        /// 重新枚举**未必**会把那条剔掉（枚举的合法性判据与 `PlayCard`/`DeclareAttack` 的
        /// 执行判据不是同一段代码）⇒ 没有排除名单就会**反复挑中同一条**。
        ///
        /// ⚠️ 排除发生在**打分之前**（难度旋钮 `TweakAvailableActions` 只看见剩下那些）。
        /// 传 `null` / 空表时行为与老版本**逐位相同**。
        /// </summary>
        public static bool NextAction(BattleContext ctx, AiDifficulty diff,
                                      IList<AiAction> exclude, out AiAction best)
        {
            best = null;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            if (exclude != null && exclude.Count > 0)
                list.RemoveAll(x =>
                {
                    for (int i = 0; i < exclude.Count; i++) if (SameAction(x, exclude[i])) return true;
                    return false;
                });
            if (list.Count == 0) return false;
            foreach (var a in list) a.Score = ScoreAction(ctx, a);
            TweakAvailableActions(ctx, list, diff);
            best = SelectBestAction(ctx, list);
            if (best == null || best.Kind == AiActionKind.EndTurn) { best = null; return false; }
            return true;
        }

        /// <summary>
        /// 两条动作是不是「**同一步**」。
        ///
        /// ⚠️ **必须按值比，不能按引用比** —— 每次 `EnumerateActions` 都新建一批对象，
        ///    被拒的那条与重新枚举出来的那条**永远不是同一个引用**。
        /// 判据 = 种类 + 手牌下标 + 自己那一格 + 目标方/目标格 + 替代关键词 + 近战还是远程。
        /// </summary>
        static bool SameAction(AiAction a, AiAction b)
        {
            if (a == null || b == null) return ReferenceEquals(a, b);
            return a.Kind == b.Kind
                && a.HandIdx == b.HandIdx
                && a.Slot == b.Slot
                && a.TargetP == b.TargetP
                && a.TargetSlot == b.TargetSlot
                && a.AltKeyword == b.AltKeyword
                && a.Ranged == b.Ranged;
        }

        /// <summary>连着被引擎拒这么多条就收手（防止死循环；正常情况一条都不会被拒）。</summary>
        public const int MaxRejectedActions = 8;

        /// <summary>默认难度的一步（`Normal`）。自检里要指定难度就用三参那个重载。</summary>
        public static bool NextAction(BattleContext ctx, out AiAction best)
        {
            return NextAction(ctx, AiDifficulty.Normal, out best);
        }

        /// <summary>
        /// 现在**到底有没有事可做**（不掷骰、不吃难度旋钮）。
        /// 用途是表现层的「这回合卡住了吗 → 自动结束回合」那个判断：
        /// 拿 <see cref="NextAction"/> 去问会**误判** —— 难度那套会随机砍掉最低分的动作，
        /// 玩家明明还能出牌也可能被砍成只剩 `endTurn`。
        /// </summary>
        public static bool HasAnyAction(BattleContext ctx)
        {
            if (ctx == null || ctx.IsOver) return false;
            foreach (var a in EnumerateActions(ctx))
                if (a.Kind != AiActionKind.EndTurn) return true;
            return false;
        }

        /// <summary>🆕 2026-09-27：**动作执行完的通知口**（引擎侧唯一的出口）。
        /// 本地录像（`BattleDriver`）订阅它来记 AI 走的每一步。
        ///
        /// 🔴 **为什么挂在这里、不挂在 `BattleDriver.DriveAiTurn`**：AI 的动作有**两个入口** ——
        ///    产品那条走 `NextAction` + `ExecuteAction`（一帧一步），而自检与兼容层走
        ///    `PlayTurn`（内部自己循环）。挂在调用点会**漏掉 `PlayTurn` 那一半**，
        ///    而漏记 = 回放悄悄演成另一局。挂在引擎边界，两处都覆盖。
        ///
        /// 参数：`(ctx, 动作, 成功没有)`。⚠️ 它**纯通知、不改任何行为** ——
        /// 引擎里**不许**读它的返回值，也**不许**靠它做分支。
        /// </summary>
        public static System.Action<BattleContext, AiAction, bool> Executed;

        /// <summary>执行一条动作（调用方拿着 <see cref="NextAction"/> 给的动作时用它）。
        /// 返回 true = 执行成功。</summary>
        public static bool ExecuteAction(BattleContext ctx, AiAction a)
        {
            bool ok = ExecuteActionCore(ctx, a);
            var cb = Executed;                 // 先取出来：回调里改订阅也不会影响这一下
            if (cb != null) cb(ctx, a, ok);
            return ok;
        }

        static bool ExecuteActionCore(BattleContext ctx, AiAction a)
        {
            if (ctx == null || a == null || ctx.IsOver) return false;
            // 🆕 2026-09-27（录像）：**先把面板答案快照到动作上**（引擎结算会把队列吃空，之后再读就没了）
            a.CapturedPicks = ctx.ChoosePicks.Count > 0 ? ctx.ChoosePicks.ToArray() : null;
            a.CapturedPickIds = ctx.ChooseCardIds.Count > 0 ? ctx.ChooseCardIds.ToArray() : null;
            int me = ctx.Active;
            switch (a.Kind)
            {
                case AiActionKind.PlayCard:
                {
                    // 🔴 **把解析出来的手牌下标写回动作上** —— `NextAction` 给的动作带的是 `HandInst`
                    //    （对象引用），而 `HandIdx` 往往是 -1 或过期的。录像/回放走的是**下标**
                    //    （`MsgAction.handIdx`，与联机同一条规矩：两端手牌逐位相同 ⇒ 下标就够）。
                    //    不写回的话，录下来的是 `handIdx = -1` ⇒ 回放**出的牌全不对**，
                    //    而且**只有指纹对账抓得到**（2026-09-27 就是这么抓到的）。
                    int hi = HandIdxOf(ctx, me, a);
                    if (hi >= 0) a.HandIdx = hi;
                    return RuleCore.PlayCard(ctx, me, hi, a.Slot) == RuleCodes.OK;
                }
                case AiActionKind.AttackMelee:
                case AiActionKind.AttackRanged:
                    return RuleCore.DeclareAttack(ctx, me, a.Slot, a.TargetP, a.TargetSlot, a.Ranged) == RuleCodes.OK;
                case AiActionKind.ActiveAbility:
                    if (a.AltKeyword == "oath")
                        return RuleCore.UseOathAbility(ctx, me, a.Slot) == RuleCodes.OK;
                    if (a.AltKeyword != null)
                        return RuleCore.UseAlternative(ctx, me, a.Slot, a.AltKeyword,
                                                       a.TargetSlot >= 0 ? a.TargetSlot : -1) == RuleCodes.OK;
                    return RuleCore.UseAbility(ctx, me, a.Slot, a.TargetSlot >= 0 ? a.TargetSlot : -1) == RuleCodes.OK;
                case AiActionKind.CollectWaystone:
                    return RuleCore.CollectWaystone(ctx, me, a.Slot) == RuleCodes.OK;
                default:
                    return false;
            }
        }

        // ==================================================================
        //  ⑥ 兼容层 —— 老的调用点还在用这几个入口
        //     （`BattleDriver` / `BattleAutoDrive` / `BattleScene` / `RuleEngineTest` 共 30 处）
        //     语义 = 「从这一类动作里挑分最高的那条」，判据全部走上面的打分族，**不另写一份**。
        // ==================================================================

        /// <summary>
        /// 这个单位这一刀用近战还是远程：**远程攻击力更高就用远程**。
        /// ⚠️ 和 `BattleDriver.UseRanged` 是**同一条规则**，两处必须一致 ——
        ///    各写一份的话，AI 算出来的刀和画面上播出来的刀会对不上。
        /// ⚠️ **2026-09-17 之后 AI 自己不再用它挑刀**（照原版：近战/远程**各算一次分再挑高的**），
        ///    这条留着给表现层与旧调用点用。
        /// </summary>
        public static bool UseRanged(UnitState u)
        {
            return u != null && u.RangedAttack > u.Attack;
        }

        /// <summary>
        /// 这一手出什么牌、落哪一格。**单位卡和战术卡都算**，取打分最高的那条。
        /// 落点由**引擎**说了算（`RuleCore.CanPlayCard`），这里不重写一遍合法性判断。
        /// </summary>
        public static bool NextPlay(BattleContext ctx, out int handIdx, out int slot)
        {
            handIdx = -1; slot = -1;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            AiAction best = null;
            foreach (var a in list)
            {
                if (a.Kind != AiActionKind.PlayCard) continue;
                a.Score = ScoreAction(ctx, a);
                if (best == null || a.Score > best.Score) best = a;
            }
            if (best == null) return false;
            handIdx = HandIdxOf(ctx, ctx.Active, best); slot = best.Slot;
            return true;
        }

        /// <summary>
        /// **现在该打第几张**（第 7 行第 4 步）：优先按**那一份**反查下标，
        /// 那份已经不在了（被打出/弃掉）才退回枚举时记下的下标。
        /// </summary>
        static int HandIdxOf(BattleContext ctx, int me, AiAction a)
        {
            if (a.HandInst != null)
            {
                int k = ctx.Players[me].Hand.IndexOf(a.HandInst);
                if (k >= 0) return k;
            }
            return a.HandIdx;
        }

        /// <summary>该出哪张手牌（返回手牌索引，-1 = 不出）。判据就是 <see cref="NextPlay"/>。</summary>
        public static int NextCardToPlay(BattleContext ctx)
        {
            int i, s;
            return NextPlay(ctx, out i, out s) ? i : -1;
        }

        /// <summary>挑一个空格部署 —— 🔴 **2026-10-01 改成原版那一条**：
        /// `MinionManager.GetNextSlotWithoutDisplacing`（**人少的那一侧的最外一格**，平手走右）。
        ///
        /// 为什么改：旧版是「从 0 号格起第一个空格」，那是**我们自己造的**，而且跟棋盘模型冲突 ——
        /// 棋盘现在是**连续无洞**的（`BoardSlots`），请求「0 号格」会被夹到该侧最里那一格，
        /// 于是**「请求的格」和「落下的格」不是同一个**，调用方（自检 / 演示）拿它当落点就会对不上。
        /// 改成原版那条之后，**请求值 = 落点值**（那一格按定义是空的、且在最外 ⇒ 插入下标 = 人数）。
        /// 满场返回 -1。
        ///
        /// 🔴 **2026-10-22（引擎线 `T02` 同族 · `A1692` 的最后一处残留）：这里也必须判「这一格真的可占」。**
        ///   `NextWithoutDisplacing` 的前提是**棋盘连续无洞**（原版两侧是 `List`、结构上不可能有洞 ——
        ///   `MinionManager__GetNextSlotWithoutDisplacing.c:14-30` 全文只比两个 `List.Count`、
        ///   不做任何占用检查），而我们的 `Board[]` 是定长数组，**自检夹具**可以直接写出洞 ⇒
        ///   它算出的 `SlotOf(side, CountOnSide(side))` **可能已经有人**。调用方（演示 / 自检 /
        ///   `BattleDriver` 的拖放兜底）随后把这个值喂给 `RuleCore.PlayCard`，而
        ///   `BoardSlots.Insert` 的「有洞降级」分支在**请求格被占**时会落到插推路径上
        ///   ⇒ **外侧那个单位被静默抹掉**（`WarnHoley` 只报错、不救）。
        ///   ⇒ 改走**同一个判据的第二档** `BoardSlots.NextFreeWithoutDisplacing`
        ///     （`RuleCore.DeployFree` / `EffectResolver.DoTakeControl` / `RuleCore.EndTurn`
        ///      三处已经用它）；⛔ 别在这里另写一份「空格 = 没单位」
        ///     （`CLAUDE.md` §三：两处写同一条规则 = 迟早不一致）。
        ///   ⚠️ **连续棋盘上行为逐字不变**（那一档的第一支就是原样返回）；只有**有洞**时才会换格
        ///     ⇒ 这是**补我们表示法的洞**，⛔ 别在注释里写成「照原版」。
        ///   ⚠️ 有洞还要**出声**（本仓红线「不许静默失败」）—— 这种棋盘只可能来自夹具直接写 `Board[]`。
        /// </summary>
        public static int FirstFreeSlot(PlayerState p)
        {
            int slot = BoardSlots.NextFreeWithoutDisplacing(p, out int blocked);
            if (blocked >= 0)
            {
                UnityEngine.Debug.LogWarning("[SimpleAI] ⚠️ 棋盘不连续：`FirstFreeSlot` 按原版规则算出的 "
                    + blocked + " 号格已被 " + (p.Board[blocked] != null ? p.Board[blocked].Name : "?")
                    + " 占着 —— **不覆盖**，改选该侧最外的空格 "
                    + (slot >= 0 ? slot.ToString() : "（该侧没有空格）")
                    + "。引擎的增删路径都维持「每侧从贴督军那格起连续无洞」，出现洞说明"
                    + "**有地方（多半是自检夹具）直接写了 `Board[]`**（`T02` / `A1692` 同族）。");
            }
            return slot;
        }

        /// <summary>
        /// 该不该放技能（含誓约 / 替代行动）。
        ///
        /// 🔴 **比较口径：和「**同一个单位**的平A」比**（老版本写的是「技能伤害 ≥ 攻击力就放」，
        ///    两者是同一件事的两种写法：技能**不吃反击**，打得一样多就没道理去平A）。
        /// ⚠️ **不能拿「全场最高的那一刀」来比** —— 那样督军的一刀会把场上单位的好技能全压掉
        ///    （2026-09-17 踩过：`Reef Guard` 那族断言当场红）。比较范围限定在**本单位**。
        /// </summary>
        public static bool NextAbility(BattleContext ctx, out int slot, out int targetSlot)
        {
            slot = -1; targetSlot = -1;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            foreach (var a in list) a.Score = ScoreAction(ctx, a);

            AiAction best = null;
            foreach (var a in list)
            {
                if (a.Kind != AiActionKind.ActiveAbility) continue;
                if (a.Score <= 0f) continue;                 // endTurn 基准是 0

                // 本单位「**打同一个目标**」的平A值多少（近战/远程取高的那路）
                // 🔴 **必须同目标比**：拿「本单位最高的那一刀」来比是错的 —— 打督军那一刀天生比
                //    打小兵分高，会把「打小兵的技能」全压掉（2026-09-17 踩过：`TestAiUsesAbility`
                //    的 2 攻打 2 伤技能当场红）。原版的技能和攻击本来就**各自对应同一个目标**。
                float ownAttack = 0f;
                if (a.TargetSlot >= 0)
                {
                    foreach (var b in list)
                        if (b.Slot == a.Slot && b.TargetSlot == a.TargetSlot
                            && (b.Kind == AiActionKind.AttackMelee || b.Kind == AiActionKind.AttackRanged)
                            && b.Score > ownAttack) ownAttack = b.Score;
                }

                if (ownAttack > a.Score) continue;           // 平A更划算就不放技能
                if (best == null || a.Score > best.Score) best = a;
            }
            if (best == null) return false;
            slot = best.Slot; targetSlot = best.TargetSlot;
            return true;
        }

        /// <summary>
        /// 该打哪一刀（分最高的那条攻击动作）。
        /// ⚠️ 近战/远程**各算一次分**（照原版），返回的那条带着它自己的 `ranged`。
        /// </summary>
        public static bool NextAttack(BattleContext ctx, out int atkSlot, out int targetP, out int targetSlot,
                                      out bool ranged)
        {
            atkSlot = -1; targetP = -1; targetSlot = -1; ranged = false;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            AiAction best = null;
            foreach (var a in list)
            {
                if (a.Kind != AiActionKind.AttackMelee && a.Kind != AiActionKind.AttackRanged) continue;
                a.Score = ScoreAction(ctx, a);
                if (best == null || a.Score > best.Score) best = a;
            }
            if (best == null) return false;
            atkSlot = best.Slot; targetP = best.TargetP; targetSlot = best.TargetSlot; ranged = best.Ranged;
            return true;
        }

        /// <summary>
        /// **该把哪几张起手牌换掉**（原版 `AI.GetAiMulliganCards`，2026-09-17 反编译解开 ——
        /// 见正本 §二）：
        ///
        /// ```
        /// 遍历手牌：
        ///   · mulliganOption == whispersOfChaos(30) → 保留
        ///   · 否则 manaCost > 4                      → 换掉
        ///   · 其余保留
        /// ```
        ///
        /// ⚠️ **我们这边的两处对应/差别**（如实标着）：
        ///   ① `whispersOfChaos` 这一支**我们卡池里没有对应物**（那是原版的卡种类，
        ///      我们的 `type` 只有 unit / tactic / hero / defence）⇒ 这条分支不适用，不是被我们砍了；
        ///   ② **防御卡排除在外** —— 🔴 **2026-10-08（`D28` 施工单 A）：这一条的理由原来写的是错的，
        ///      已就地订正（铁律 5）。** 原文写「那是**引擎自己的规矩**（`RuleCore.Mulligan` 里写着
        ///      「防御卡不许换掉」）」—— **那条守卫【已被删】**（现读 `RuleCore.Mulligan`：它只做
        ///      「去重 → 弃回牌库 → 重洗 → 补抽同样张数」，**没有任何按 `Type` 挡的判据**；
        ///      删它的理由写在那个 `idx.Add(k);` 上方的注释里）。
        ///      现状（现读，不是跑出来的）：防御卡照原版改成「**换牌阶段结束之后**才进手牌」
        ///      （`RuleCore.NewBattle` 的 `mulliganOpenNow` 分支 / `EndMulligan` → `GrantDefenceCard`）
        ///      ⇒ `ctx.MulliganOpen` 为真的**整个期间**手里都不可能有 `defence` 卡
        ///      （本方法开头又先判了 `if (!ctx.MulliganOpen) return idx;`）⇒ **下面那句是空转的**。
        ///      **行为一行不动**（照旧留着它是**防御性**的），但它**不再是**「引擎规矩」。
        ///      原版 AI 那边没有这一条，因为原版的防御卡不是这么发的。
        /// </summary>
        public static List<int> AiMulliganIndices(BattleContext ctx, int p)
        {
            var idx = new List<int>();
            if (ctx == null || !ctx.MulliganOpen) return idx;
            var hand = ctx.Players[p].Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var c = hand[i].Card;              // 第 7 行第 2 步：换牌判据只看卡面
                if (c == null) continue;
                // 🆕 **2026-10-25（引擎线 `A2` · `A2-3`）：原版 `AI__GetAiMulliganCards.c:39` 的第一句
                //   —— `mulliganOption == 30` 的牌**跳过（保留）**。**不换** = 不放进换牌下标表。
                //   ⚠️ 今天 30 档本路径不可达（见 `CardInstance.MulliganOption` 的 doc）⇒ 零行为变化。
                if (hand[i].MulliganOption == 30) continue;
                // 防御卡跳过 —— 🔴 **2026-10-08（`D28` 施工单 A）订正：这里原来写「引擎规矩：防御卡不许换」，
                //   那条守卫**已被删**（见上面 summary ②）。留着这句是**防御性**的，**行为不变**；
                //   ⛔ 但别再由它反推「换牌阶段手牌里会有防御卡」—— 那正是这条过期注释误导的方向。
                if (c.Type == "defence") continue;
                if (c.Cost > 4) idx.Add(i);
            }
            return idx;
        }

        /// <summary>
        /// 一口气把这回合打完（自检里跑完整对局用它；表现层要分部播的话别用）。
        /// 难度固定 <see cref="AiDifficulty.Hard"/> —— **不掷骰**，让批量自检只受种子影响。
        /// </summary>
        public static void PlayTurn(BattleContext ctx)
        {
            // 🆕 2026-09-29（Q6 后半段）：**引擎拒绝 ⇒ 退次优，不是收手**。
            //   原版（我们照抄的那个形状）是 `ExecuteAction` 失败就 `break` ⇒ 一条被拒整回合就停。
            //   现在把被拒的那条**记进排除名单**再挑下一条；连着被拒 `MaxRejectedActions` 条才收手。
            //   ⚠️ 每次都用**新的** `NextAction`（局面可能已被前一次失败改过）。
            var rejected = new List<AiAction>();
            for (int guard = 0; guard < 64 && ctx != null && !ctx.IsOver; guard++)
            {
                AiAction a;
                if (!NextAction(ctx, AiDifficulty.Hard, rejected, out a)) break;
                if (ExecuteAction(ctx, a)) { rejected.Clear(); continue; }
                rejected.Add(a);
                if (rejected.Count >= MaxRejectedActions) break;
            }
        }

        // ==================================================================
        //  ⑥ `Core` 的 AI 评分钩子（`W64 · B-01`，2026-10-23）
        //
        //  `Core` 不许依赖 `Data/`（探针只编 `Core/*.cs`），而原版的选靶评分住在
        //  `AI.GetActionScore` 那一族里 ⇒ `Core` 开一个 `IAiScorer` 接口（`Core/IAiScorer.cs`），
        //  由本文件实现并注入。**评分只有这一份实现** —— 两处写同一条规则 = 迟早不一致。
        // ==================================================================

        /// <summary>
        /// `Core/IAiScorer.cs` 的实现 —— **原版 `AI.GetActionScore` 的攻击支**
        /// （`AI__GetActionScore.c:45-63` 的 `case 1 / case 2` 两支都汇到 `AI.ScoreFromAttack`）：
        ///   · `s = ScoreFromAttack(出手方, 出手单位, 目标, 打法档)`（本文件同名重载）；
        ///   · 目标是**督军**（原版 `EntityScript.get_cardType(targetCard) == 10`，`:60`）
        ///     且 `CombinedAttackCanKillWarlord` ⇒ `s += 10000`（`:60-63` 的 `DAT_1834b32f4`
        ///     = <see cref="CombinedKillBonus"/>）。
        /// 更上层（`AI__SelectBestAttackAction.c:46-65`）是「逐条打分 → `SelectBestAction`」，
        /// 严格 `>` ⇒ **平手留先出现的**；调用方按槽号升序枚举即可复现。
        /// 无状态 ⇒ 单例安全（⛔ 别往这里存局面）。
        /// </summary>
        sealed class SimpleAiScorer : IAiScorer
        {
            public float ScoreAttack(BattleContext ctx, int side, int slot, int foeSlot, bool ranged)
            {
                if (ctx == null) return 0f;
                if (side < 0 || side > 1) return 0f;
                if (!BoardSpec.IsValid(slot) || !BoardSpec.IsValid(foeSlot)) return 0f;

                var u = ctx.Players[side].Board[slot];
                var target = ctx.Players[1 - side].Board[foeSlot];
                if (u == null || target == null) return 0f;

                float s = ScoreFromAttack(ctx, side, u, target, ranged);

                // `AI__GetActionScore.c:60-63`：目标 cardType == 10（督军）+ 合击够斩杀 ⇒ +10000。
                if (target.IsWarlord && CombinedAttackCanKillWarlord(ctx, side)) s += CombinedKillBonus;
                return s;
            }
        }

        /// <summary>共享的评分器实例（无状态）。</summary>
        public static readonly IAiScorer Scorer = new SimpleAiScorer();

        /// <summary>
        /// 把 <see cref="Scorer"/> 挂到 `Core` 的 <see cref="AiScorerHook"/> 上（**幂等**）。
        ///
        /// 三条入口都会调它：
        ///   ① 本类的**静态构造** —— 第一次用到 `SimpleAI` 时；
        ///   ② `[RuntimeInitializeOnLoadMethod]` —— 进 play 模式 / player 启动时。
        ///      ② 是必需的：狂暴强制出手发生在**玩家回合结束时**，而 AI 可能**还没动过一次**
        ///      （先手第 1 回合）⇒ 只靠 ① 会漏掉那一回合的选靶；
        ///   ③ 自检 / 任何宿主显式调（`-executeMethod` 批处理**不会**触发 ②）。
        /// ⚠️ 这是一次**全局**副作用（`AiScorerHook.Scorer` 是进程级的）——
        ///    自检里换过假评分器之后**记得换回来**（`RuleEngineTest.TestB01AiScorerHook` 用 `finally`）。
        /// </summary>
        public static void InstallAiScorer()
        {
            AiScorerHook.Scorer = Scorer;
        }

        static SimpleAI()
        {
            InstallAiScorer();
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InstallAiScorerOnLoad()
        {
            InstallAiScorer();
        }

        // ==================================================================
        //  ⑦ 🆕 2026-10-25（第八十会话 · `R11`）：`ScoreFromAttack` 新增四支的常量与子过程
        //     判据 = `资料/完全复刻/判据/AI打分与动作选择_第2轮.md` §6·3 / §6·4（`R6-03` / `R6-04`）
        //     ⚠️ 一律**追加在类尾** —— 插进成员之间会让账里的 `A` 行号整体下移（`check_ledger_points`）。
        // ==================================================================

        /// <summary>`poisonous`（原版 `DefinedTrait.poisonous = 380`）。
        /// ⚠️ 我们 `KeywordTable` 里**没有**这个常量（全池 0 张卡带它），按既有惯例用裸串
        /// （同 `ScoreDamaging` 里的 `"invulnerable"` / `ValueInPlay` 里的 `"pindown"`）。
        /// 数据里一旦出现这个 trait，`KwValue/ Has` 照样认（键就是字符串）。</summary>
        public const string PoisonousKeyword = "poisonous";

        /// <summary>毒追加分的系数（`DAT_1834b3188` = `0.7f`；判据 §6·1 逐字节核过）。</summary>
        public const float PoisonousScale = 0.7f;

        /// <summary>献祭那一支「非 special」的固定加分（原版 `+2.0f`，`AI__ScoreFromAttack.c:52`）。</summary>
        const float SacrificeBonus = 2f;

        /// <summary>🔬 只给 `工具/ruleprobe ai` 用的**攻击分分解**读数钩子（🆕 2026-10-25 · `R11` 第二段）。
        /// 非 `null` 时 <see cref="ScoreFromAttack(BattleContext,int,UnitState,UnitState,bool)"/> 每算完一段就报一行
        /// （① 伤害分 / ② 反击+存活 / ③ 毒 / ④ 献祭 / ⑤ 爆裂 / ⑥ 标记光）。
        /// 🔴 **产品路径恒 `null`（默认值）⇒ 行为零变化**；⛔ 别在游戏代码里给它赋值。
        /// 为什么需要它：`ruleprobe play all dump` 是结算层读数，**结构上看不见 AI**（详见 `R11` 报告 §四）。</summary>
        public static System.Action<string> TraceAttack;

        /// <summary>
        /// 原版 `AI.ScoreFromAttack` 里 `sacrifice` 的 **special** 那一支：
        /// `s += ScoreFromCriteria(manager, thisCard, targetCard, rawCard.attackScoringCriteria /* +0x300 */)`。
        ///
        /// 🔴 **本轮只能返回 0 —— 是「数据层没有这一列」，**不是**「原版也是 0」**：
        ///   · `AiScoringData.cs:10-13` 明写「`attackScoringCriteria` 不在里面，**也别加回来**」，
        ///     理由是它 1,351/1,351 是**同一份序列化默认模板**（本会话现读复核：
        ///     `cards_raw.json` 的 `full_raw.attackScoringCriteria` 只有 **1 种**取值）。
        ///   · 那份模板 `baseValueFriend/Enemy` 缺省 0、`scoringOption` 缺省 0（= `FixedValue`）
        ///     ⇒ 就算喂进我方 `AiScoring.ScoreFromCriteria` 也**恰好返回 0**。
        ///   ⇒ 今天「有数据 / 无数据」两支**同值**；等真值里出现非默认模板时，
        ///     这里要换成「把那一列切进 `ai_scoring.json` 再调 `AiScoring.ScoreFromCriteria`」。
        ///   ⚠️ 可达性：池内 **0 张**卡带 `sacrifice` ⇒ 这一支今天一次都不会走到。
        /// </summary>
        static float AttackScoringCriteriaScore(UnitState u)
        {
            return 0f;
        }

        /// <summary>目标的**相邻单位数**（原版 `BattleManager.GetAdjacentUnits(targetCard)`）。
        /// 「谁算相邻」只有 `BoardSpec.AdjacentSlots` 一处判据 ⇒ 转发，别在这里写 `slot ± 1`
        /// （原版 `MinionManager.GetAdjacentUnits` 只管**同一方棋盘行内**的左右格）。
        /// 只数**非空格**（原版那张表里装的是单位、不是格子）。</summary>
        static int OccupiedAdjacentCount(BattleContext ctx, int foeSide, UnitState target)
        {
            if (ctx == null || target == null) return 0;
            if (foeSide < 0 || foeSide > 1) return 0;
            var board = ctx.Players[foeSide].Board;
            int slot = -1;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(board[s], target)) { slot = s; break; }
            if (slot < 0) return 0;
            var slots = new List<int>(2);
            BoardSpec.AdjacentSlots(slot, slots);
            int n = 0;
            for (int i = 0; i < slots.Count; i++)
                if (BoardSpec.IsValid(slots[i]) && board[slots[i]] != null) n++;
            return n;
        }

        // ==================================================================
        //  ⑧ 🆕 2026-10-26（`E4` 批次 3）：原版 `AI.ShouldDisableTweakAvailableActionsByMode`
        //     （`d:/2/tools/decomp_full/AI__ShouldDisableTweakAvailableActionsByMode.c:1-41`
        //      ＋ `AI__TweakAvailableActions.c:22-52` 是同一段的两个入口）
        //     ⚠️ 追加在类尾（铁律：新成员追加类尾，免得账里 A 行号整体下移）
        // ==================================================================

        /// <summary>
        /// **这一局到底套不套难度旋钮** —— 原版 `AI.ShouldDisableTweakAvailableActionsByMode`。
        /// 返回 **`true` = 不套**（动作表原样返回）、**`false` = 照做**。
        ///
        /// **判据（`.c:9-40` 逐句）** —— 读的是 **`MatchData.matchType`（`SetBotOpponent` 翻转后）**：
        /// <code>
        /// if (matchType == 20  RankedBot)     return false;
        /// if (matchType == 120 ClosedDeckBot) return false;
        /// if (matchType == 210 FastModeBot)   return false;
        /// if (matchType == 50  PracticeOffline)
        ///       return MatchData.IsInFTUEState();     // 在 FTUE 里 ⇒ true（不套）
        /// return true;                                // 其余档**一律不套**
        /// </code>
        /// ⚠️ 前面还有一道 `FeatureConfig.ShouldDisableNewMatchMaking()` ⇒ 为真时**整段不跑**；
        ///   那是远端 feature 表，我们**没有**这个开关 ⇒ 按「新匹配已启用」处理（恒不成立）。
        /// ⚠️ `IsInFTUEState` 读的是**远端 FTUE 配置**（`AIBotsConfig.BotListFTUE` + `CurrentModeVictories`，
        ///   见 `d:/2/tools/decomp_full/MatchData__IsInFTUEState.c`）；**FTUE 整套未建** ⇒ 本地恒 `false`。
        ///
        /// 🔴 **没有 `BotSeat` 时的一处本地迁移**：原版这一格恒在「对手是电脑」的局里被调（`matchType`
        ///   已被 `MatchData.SetBotOpponent` 翻成 `…Bot`）。我们若没声明 `BotSeat`（自检夹具 / 兼容层），
        ///   <see cref="MatchTypes.Effective"/> 会给**未翻转**值（`Ranked 10` / `ClosedDeck 110` / `FastMode 200`）
        ///   ⇒ 这里**把这三档按「会翻转」处理**。
        ///   ⛔ **这不是第二张翻转表** —— 翻转规则仍只在 `MatchTypes.Effective` 一处，这里只是「同一档的
        ///   Bot 变体也算数」。⚠️ `BotSeat < 0` 时本来就没有 AI 在跑，这一支对产品路径不可达。
        /// </summary>
        public static bool ShouldDisableTweak(BattleContext ctx)
        {
            if (ctx == null) return true;                  // 原版 `matchData == null` ⇒ NRE；我们保守「不动」
            switch (MatchTypes.Effective(ctx))
            {
                case MatchType.RankedBot:          //  20
                case MatchType.ClosedDeckBot:      // 120
                case MatchType.FastModeBot:        // 210
                    return false;
                case MatchType.PracticeOffline:    //  50 —— 原版只在 FTUE 里不套；我们无 FTUE ⇒ 套
                    return false;
                // ↓ 仅在「没声明 BotSeat」时可达（`Effective` 会翻转成 …Bot）—— 见 doc
                case MatchType.Ranked:             //  10 → 对 bot 时原版会翻成 20
                case MatchType.ClosedDeck:         // 110 → 120
                case MatchType.FastMode:           // 200 → 210
                    return false;
                default:
                    return true;                   // 其余档（含 Tutorial / Practice / Campaign / Duel …）
            }
        }

        // ==================================================================
        //  🆕 2026-10-26（`B8_E`）：原版 `BattleManager.MightAttackBuffedCard`
        //  —— 追加在类尾（铁律 18·b：上面的行号都是账里的落点，别插在中间）
        // ==================================================================

        /// <summary>
        /// 「这个单位**叠上这条 buff 之后**还出得了手吗」—— 原版
        /// `BattleManager.MightAttackBuffedCard(CardScript card, CardEffect buffEffect)`。
        /// 它是 `AI.GetPotentialBuffedAttackActions` 的**第一道闸**（能不能出手），
        /// 也是 `ScoreFromBestAttack` / `ScoreFromBuffedAttack` 那条链的前置。
        ///
        /// <para>**判据（第一权威，逐句）**：🥇 `decomp_full/BattleManager__MightAttackBuffedCard.c:1-33`
        /// （全文 33 行，逐段对照如下）：
        /// <code>
        /// if (card != null) {
        ///   if (card.mightAct) {                                            // .c:10
        ///     v = card.CurrentMeleeAttack + buff.meleeAttackChange(+0x48);  // .c:12-14
        ///     if (v &lt; 1) {                                                  // .c:16
        ///       v = card.CurrentRangeAttack + buff.rangeAttackChange(+0x50);// .c:17-19
        ///       if (v &lt; 1) return false;                                    // .c:20
        ///     }
        ///     if (card.state(+0x228) == 2) return true;                     // .c:22-24  (2 = 在场)
        ///   }
        /// }
        /// return false;
        /// </code>
        /// ⇒ 合取三条：`mightAct` ∧（`近战+Δ ≥ 1` **或** `远程+Δ ≥ 1`）∧ `状态 == 2`（在场）。</para>
        ///
        /// <para>**机制差异 / 我们等价在哪**：
        /// ① **第三条（`state == 2`）我们不另写判据** —— 交付进来的 `t.Unit` 本来就是
        ///    **棋盘上**的单位（调用点先按 `ctx.Players[side].Board` 枚举），恒真；
        /// ② `mightAct` 走引擎唯一判据 <see cref="AiTargets.MightAct"/>
        ///    （→ `ConditionEval.MeetsUnit(..., 20, ...)`，同源 `CardScript__get_mightAct.c`），
        ///    ⛔ 不在这里另写一份；
        /// ③ 🔴 **本条是本轮补上的缺口**：`AiScoring.BestAttackScore` 原来只判
        ///    `u.Exhausted || u.IsStunned` —— 那是我们侧 `canAct`（`+0x230`）的近似，
        ///    而原版这个闸读的是 `mightAct`：`CardScript__get_mightAct.c:7-27` 逐句 =
        ///    「已攻击过须 `bloodThirst` 且 `== 1` · 是单位 · **`!displaySummonSickness`** ·
        ///    `!stun(100)` · `!cantAttack(0x96=150)` · `!nonCombatant(0x370=880)`」——
        ///    **它一处都不读 `canAct`**（同族佐证：`UnitState.cs` 的 `CannotAttackThisTurn` doc
        ///    明写「不能拿 `Exhausted` 顶替」，那是另一位）。
        ///    ⇒ 旧写法把「本回合已行动（用过主动技等）」误当成「不能出手」，会让这一类单位
        ///    整族不进 option 5 的候选；现在「带 buff」那一档的闸收在这一处。
        ///    ⚠️ **只对「带 buff」那一档生效**（见 ⑤）；`AiScoring.BestAttackScore` 的
        ///    `meleeDelta == 0` 是「不带 buff」的对照分，原版那条路不经过本闸。
        ///    🔴 **两个闸谁管这一格待裁决**：本方法（原版 `mightAct`）与 `AiScoring` 的旧口径
        ///    （`Exhausted || IsStunned` = 我们侧 `canAct`）在「已行动但未攻击」的单位上结论相反；
        ///    2026-10-26 `B8_E` 取**保守合取**（两个都过才进候选，只会收紧不会放宽），
        ///    并在实测夹具上看到两者确实分叉（`ai "Path of the Warrior"` 的 option 5 从 0 变 −1.2）
        ///    ⇒ 已记账交引擎线裁决，⛔ 别单方面删掉 `Exhausted` 那一句；
        /// ④ 两个 Δ 由调用方从**它自己那套 buff 表示**里取（原版是 `CardEffect` 的
        ///    `+0x48 / +0x50` 两格）；`AiScoring.BestAttackScore` 那条链目前只有近战 Δ
        ///    （远程 Δ 传 0 —— 因为 `GetPotentialBuffedAttackActions` 只枚举**近战** action，
        ///    远程那一支进了门之后也会被 `atk &lt;= 0` 挡掉，净结果相同）；
        /// ⑤ **为什么「不带 buff」那一档不套本闸**：原版 `AI__ScoreFromBestAttack.c:21`
        ///    **只调一次** `GetPotentialBuffedAttackActions(manager, actingCard, buff)`（带 buff），
        ///    拿到的列表逐条打分取 max；option 5 里那个「不带 buff」的对照分是我们自己重建的
        ///    （判据里没有对应方法体），所以那一侧仍走原来的 `Exhausted || IsStunned`。</para>
        ///
        /// <para>**可观察量**：`bash d:/4/Unity/工具/ruleprobe.sh ai &lt;卡id&gt;` 的 `[出牌分·逐条]`
        /// 那一行 `cardTarget[0] option=5 … mightAct=真 M / 假 K` —— `M`/`K` 是这张卡
        /// option 5 候选里「过闸 / 被闸掉」的目标数（见 `工具/ruleprobe/AiProbe.cs` 文件头
        /// 第 34-37 行给的验法）。</para>
        /// </summary>
        public static bool MightAttackBuffedCard(BattleContext ctx, AiTargetRef t,
                                                 int meleeDelta, int rangedDelta)
        {
            var u = t.Unit;
            if (u == null || !u.IsAlive) return false;
            if (!AiTargets.MightAct(ctx, t)) return false;      // `.c:10`
            if (u.Attack + meleeDelta >= 1) return true;        // `.c:12-16`（近战那支过闸）
            return u.RangedAttack + rangedDelta >= 1;           // `.c:17-20`（近战不行再看远程）
        }
    }
}
