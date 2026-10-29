// CardDefPartial_B97W5.cs — 第九十七会话 · 波 1 · 执行代理 `B97_W5`
//   `CardDef` 的又一半，只装本波两件**数据面**：
//     ① `BoardGateSpec.ConditionsFilter`（账 `战斗逻辑驱动:676` 的 (b) 格 —— 原版
//        `BoardCriteria.countCriteria.conditionsFilter(+0x38)`）；
//     ② `startingCardInHand`（账 `:607` 的 ① 格 —— 原版 `RawCardScript.startingCardInHand`,
//        `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:97`）。
//
// 🔴 **为什么另起一个 `partial class CardDef` 块、写在【新文件】**：
//   完全同 `CardDefPartial_B94B16.cs` / `CardDefExtraTraits.cs` 的文件头纪律 ——
//   `CardDef.cs` 里有一批账落点按【行号】引用（现读 81 处，最大 `CardDef.cs:9137`），
//   在既有文件里插成员会让它们整体漂移（铁律 18·b）。
//   ⚠️ 唯一动到 `CardDef.cs` 本体的两处都是**同行改写**（行数不变、零漂移）：
//     · `:267` `public sealed class BoardGateSpec` → `public sealed partial class BoardGateSpec`
//       （不加 `partial` 这里就没法开第二个声明块）；
//     · `UsesAlternativeAbility` 的 `CountEval.MeetsBoardCriteria(...)` 调用补一个 `cond:` 实参。
//
// 📌 本块**零行为变化**，两件都如实标注（铁律 11：休眠件也做，只排先后）：
//   ① 池内 `normalAbilityBoardCriteria` 只 2 张卡（`SW9` / `SW21`）、`conditionsFilter` 现读**全 0**
//      （`cards_raw.json` 的 `full_raw` 逐张读）⇒ 传进去也是 `0` = 无条件，与今天等价；
//   ② `startingCardInHand` 非空**只 3 张**（`SW1 Logan Grimnar` → `Tyrnak and Fenrir` ·
//      `BL36 Sylar Hexscorn` → `Abaddon's Chosen` · 同卡的教程副本 `b5ee585c…`），
//      而池内那 2 张今天已经靠**卡面文本**（`Start the game with X in hand.`）拿到同一批名字
//      ⇒ 落上列之后行为相同，**只是判据从「文本解析」换成「SO 真值」**。
using System.Collections.Generic;

namespace RuleEngine
{
    public partial class CardDef
    {
        // ==========================================================================================
        //  ① 账 `:676` (b) 格 —— `BoardGateSpec.ConditionsFilter`
        // ==========================================================================================
        //  判据：`decomp_full/BoardAnalysis__MeetsCriteria.c:110-113` —— 原版数候选那一步是
        //    `AbilityLogic.GetTargets(manager, countCriteria, thisCard, chosen, param_5, 1, 0)`
        //    ⇒ **候选逐个过的是 `countCriteria` 自己的 `conditionsFilter`**（`TargetCriteria +0x38`），
        //    不是卡级 `normalAbilityBoardCriteria` 上另有一个条件。
        //  ⇒ 所以这一格必须**跟着 `countCriteria` 走**（`CountEval.MeetsBoardCriteria(..., cond: …)`），
        //    今天 `CountEval` 的 `cond` 缺省 `0` = 无条件 —— 卡级调用点（`UsesAlternativeAbility`）
        //    从来没传过，那正是本格要补的缺。
        //  ⚠️ **0 是合法值**（= `UnitConditions.none` = 不筛）⇒ 默认 `0` 不是「未知」；
        //    「未知」由 `CardDatabase` 的缺键哨兵挡在**映射层**（`AbilityGateDto.conditionsFilter`
        //    缺键 = `-99` ⇒ 出声 + 落 `-1`，而 `CountEval` 会把 `-1` 当「判不了」出声）。
        public sealed partial class BoardGateSpec
        {
            /// <summary>原版 `BoardCriteria.countCriteria.conditionsFilter`（`TargetCriteria +0x38`）。
            /// `0` = `UnitConditions.none`（不筛，**合法值**）；`-1` = 未知/坏值（⇒ 求值口出声、返 `null`）。
            /// <para>消费点 = <see cref="UsesAlternativeAbility"/> → `CountEval.MeetsBoardCriteria(cond:)`。</para></summary>
            public int ConditionsFilter = 0;
        }

        // ==========================================================================================
        //  ② 账 `:607` ① 格 —— `startingCardInHand`
        // ==========================================================================================
        //  判据（🥇 `decomp_full/BattleManager._StartBattlePhase_d__334__MoveNext.c:213-247` 逐句现读）：
        //    对**双方督军**各做一次：`hero.rawCard(+0x28).startingCardInHand(+0x148)` 非空 ⇒
        //    `AddNewCardToHand(bm, 那张卡, hero, isPlayer, …priority 1, block 0, 0, 0)`。
        //    ⇒ 语义 = 「**开局直接进手牌**」（不是进牌库、不是替换起手牌）。
        //  🔴 **与 `StartWithInHand` 的分工**（⛔ 别把两者混成一处）：
        //    · `StartWithInHand` = 我们**从卡面文本**抽的（`Start the game with X in hand.`，
        //      `EffectText` 段解析，见 `CardDef.cs:4703`），它是**今天的生产路径**、有断言；
        //    · 本属性 = **卡 SO 真值**（判据第一权威）。两者在现池**完全一致**（各 2 张、名字相同），
        //      但**将来**若出现「SO 有、卡面没写」的卡，原版会发而我们不会 ⇒ 必须由真值说话。
        //    · 三态（⛔ 别合并）：`null` = **未知**（卡表 < v43 / 手写夹具）· `""` = **明确没有** ·
        //      其它 = 那张卡的名字（**卡池里的正式名**，不是 `an Abaddon's Chosen` 那种带冠词的原文）。
        public string StartingCardInHandRaw { get; private set; }

        /// <summary>`startingCardInHand` 这一列**来自卡表**的正面信号（`null` = 整列未知/旧表）。
        /// 同 `HasAbilityMutations` / `HasPermanentBuffs` 的口径。</summary>
        public bool HasStartingCardInHand { get { return StartingCardInHandRaw != null; } }

        /// <summary>由 `CardDatabase.Parse` 在**构造之后**挂载（同 `AttachAbilityMutations` 族；
        /// 理由 = `CardDef` 构造函数上那四十处账落点，⛔ 不往它加形参）。
        /// <paramref name="name"/>：`null` = 整列未知（旧表）；`""` = 明确没有；其它 = 卡名。</summary>
        public void AttachStartingCardInHand(string name)
        {
            StartingCardInHandRaw = name;
        }

        /// <summary>「开局进手牌」要发哪些卡 —— **卡片数据的唯一取值口**（`RuleCore.SetupStartWith` 只读它）。
        ///
        /// <para>取值规则（⛔ 不许静默二选一）：</para>
        /// <list type="number">
        /// <item>`StartingCardInHandRaw == null`（旧表 / 夹具）⇒ 原样返回文本抽出来的那份
        ///   （= 改前行为，零变化）；</item>
        /// <item>真值已知（`""` = 明确没有，或非空 = 那一张）⇒ **以真值为准**（原版 `.c:213-247` 只看 SO 字段）；
        ///   文本抽出但真值没有的那些**不发**，逐条写进 <paramref name="notes"/> 让调用方出声
        ///   —— 既不能静默多发（改了原版对局），也不能静默少发（玩家看到强度缺口却查不到原因）。</item>
        /// </list>
        /// <para>⚠️ `StartWithInHand` 是 `List`（卡面可能有多句），真值这一列原版是**单个** PPtr
        /// ⇒ 真值已知时返回表长度 ∈ {0,1}；未知时可能多条。</para></summary>
        public List<string> StartWithNames(List<string> notes = null)
        {
            var text = StartWithInHand ?? new List<string>();
            string raw = StartingCardInHandRaw;
            if (raw == null)
            {
                var same = new List<string>(text.Count);
                for (int i = 0; i < text.Count; i++) if (!string.IsNullOrEmpty(text[i])) same.Add(text[i]);
                return same;
            }
            // 真值已知（`""` = 明确没有 / 非空 = 那一张）⇒ **以真值为准**（原版 `.c:213-247` 只看 SO 字段）。
            // 文本抽出但真值没有的那些**不发**，但逐条写进 `notes` 让调用方出声 ——
            // ⛔ 既不能静默多发（改了原版对局），也不能静默少发（玩家看到强度缺口却查不到原因）。
            var outList = new List<string>();
            if (raw.Length > 0) outList.Add(raw);
            for (int i = 0; i < text.Count; i++)
            {
                string n = text[i];
                if (string.IsNullOrEmpty(n)) continue;
                bool dup = false;
                for (int j = 0; j < outList.Count; j++)
                    if (string.Equals(outList[j], n, System.StringComparison.OrdinalIgnoreCase)) { dup = true; break; }
                if (dup) continue;
                if (notes != null)
                    notes.Add($"卡面写着「开局上手 {n}」，而卡 SO 的 `startingCardInHand` 是"
                            + (raw.Length == 0 ? "**空的**" : $"「{raw}」")
                            + " —— 原版只认 SO 字段（`_StartBattlePhase…c:213-247`）⇒ 本版**不发**这一张，"
                            + "请复核这张卡的数据");
            }
            return outList;
        }
    }
}
