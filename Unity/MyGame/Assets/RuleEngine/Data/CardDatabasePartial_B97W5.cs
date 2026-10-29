// CardDatabasePartial_B97W5.cs — 第九十七会话 · 波 1 · 执行代理 `B97_W5`
//   本波两件**数据面**在 `CardDatabase` 侧的落点（两件都走 `partial`，⛔ 不在 `CardDatabase.cs`
//   中段加行 —— 那个文件被 `账/*.tsv` 按【行号】钉了一大批落点，加行会让它们整体漂移）。
//
// ① 账 `:676` 的 (b) 格：`normalAbilityBoardCriteria.countCriteria.conditionsFilter`
//    → `CardDef.BoardGateSpec.ConditionsFilter`（消费点 = `CardDef.UsesAlternativeAbility`
//      → `CountEval.MeetsBoardCriteria(cond:)`）。
//    判据：`decomp_full/BoardAnalysis__MeetsCriteria.c:110-113`（候选逐个过的是 `countCriteria` 自己的条件）。
//    ⚠️ 包装口 `MapBoardGateWithConditions` 只在 `Parse` 的那一行被调用（`normalAbilityBoardCriteria: …`），
//      `MapBoardGate` 本体**一个字节没动**。
//
// ② 账 `:607` 的 ① 格：`RawCardScript.startingCardInHand`（`RawCardScript.cs:97`）
//    → `CardDef.StartingCardInHandRaw`（消费点 = `CardDef.StartWithNames` → `RuleCore.SetupStartWith`）。
//    判据：`decomp_full/BattleManager._StartBattlePhase_d__334__MoveNext.c:213-247`
//      （对双方督军各读一次 `hero.rawCard(+0x28).startingCardInHand(+0x148)`，非空就 `AddNewCardToHand`）。
//
// 🔴 **三态契约（两件共用，⛔ 别合并「未知」与「明确没有」）**：
//    · 缺列 / 旧表 ⇒ `null`（未知）—— `CardDef` 那边退回**改前行为**（文本解析 / 不传 cond）；
//    · `""` ⇒ **明确没有**（原版那格是空 PPtr）；
//    · 其它 ⇒ 真值。
using UnityEngine;

namespace RuleEngine
{
    public static partial class CardDatabase
    {
        /// <summary>🆕 v43（待生成器落列）：本表 `AbilityGateDto.conditionsFilter` 的**缺键哨兵**。
        /// 🔴 与 `UnitConditionDto.MissingIndex`（`-99`）**故意不同值** —— `-99` 在 `CountSetDto`
        /// 里也当哨兵用，两个都出现时日志里要能分辨是哪一格缺键。</summary>
        public const int GateConditionsFilterMissing = -9999;

        public partial class AbilityGateDto
        {
            /// <summary>🆕 v43：原版 `BoardCriteria.countCriteria.conditionsFilter`（`TargetCriteria +0x38`）。
            /// <para>`0` = `UnitConditions.none`（**合法值**，不筛）；缺键 = <see cref="GateConditionsFilterMissing"/>
            /// ⇒ 出声 + 落 `-1`（未知），⛔ 不当成 `0`。</para></summary>
            public int conditionsFilter = GateConditionsFilterMissing;
        }

        public partial class CardDto
        {
            /// <summary>🆕 v43：原版 `RawCardScript.startingCardInHand`（`+0x148`）—— **已解成卡池显示名**的卡名。
            /// <para>三态：`null`（缺列 = 未知）· `""`（原版那格是空 PPtr = 明确没有）· 其它 = 卡名。</para>
            /// <para>⚠️ 生成器的口径：`m_PathID == 0` ⇒ `""`；能按 `path_id ↔ uniqueId` 桥解出来 ⇒
            /// **卡池里的显示名**；有引用但解不出 ⇒ **整键不落**（= 未知，出声）。</para></summary>
            public string startingCardInHand;
        }

        /// <summary>账 `:676` (b) 格的包装口：`MapBoardGate(c)` + `conditionsFilter` 那一格。
        /// <para>⚠️ 只在 `Parse` 的 `normalAbilityBoardCriteria:` 那一行调用；
        /// `MapBoardGate` 本体不动（它是 `private`，签名与行为都不变）。</para></summary>
        static CardDef.BoardGateSpec MapBoardGateWithConditions(CardDto c)
        {
            var s = MapBoardGate(c);
            if (s == null) return null;                       // 缺席 = 原版恒真默认 criteria（⛔ 不是判不了）
            var d = c != null ? c.normalAbilityBoardCriteria : null;
            if (d == null) return s;
            int raw = d.conditionsFilter;
            if (raw == GateConditionsFilterMissing)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 "
                             + "`normalAbilityBoardCriteria.countCriteria.conditionsFilter` **缺键** —— "
                             + "该格落 -1（未知；⛔ 不默认成 0 = 不筛）。重跑 工具/gen_cards_engine.py。");
                s.ConditionsFilter = -1;
                return s;
            }
            s.ConditionsFilter = raw;
            return s;
        }

        /// <summary>账 `:607` ① 格：`startingCardInHand` 列 → `CardDef.StartingCardInHandRaw`。
        /// <para>`null` = **整列未知**（旧表 &lt; v43 / 手写夹具）—— 调用方退回文本解析那条路。</para>
        /// <para>⚠️ 这里**不校验卡池里有没有这张卡**（映射期还没有卡池）；查不到由消费端出声
        /// （`RuleCore.SetupStartWith` 本来就是这么做的）。</para></summary>
        static string MapStartingCardInHand(CardDto c)
        {
            if (c == null) return null;
            return c.startingCardInHand;                      // `null` 原样（未知）· `""` 明确没有 · 其它 = 卡名
        }
    }
}
