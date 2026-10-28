// BoardAnalysis_B94B8.cs — 第九十四会话 · 波 2 · 执行代理 `B94_B8`
//
// 🔴 为什么单开文件：`RuleEngine/Core/` 里**原本没有** `BoardAnalysis`（原版是独立类，`.c` 里那一族是纯查询谓词）。
//   本批只落「无落点」里唯一一个纯查询谓词 —— `DeckMeetsCriteria`（账 `战斗逻辑驱动:248` 的
//   `CheckForHeroDeckTransform` 依赖它）。其余成员等有消费点再加（⛔ 不提前造死代码）。
//
// **第一权威**（本笔逐行现读 2026-10-29）：`d:/2/tools/decomp_full/BoardAnalysis__DeckMeetsCriteria.c`（101 行）。
//
// ⚠️ 边界：本批**只做谓词本体**。真正接进对局的两件事都在白名单外：
//   ① 战役关卡数据（`rawBattleData+0x50/+0x58` 那两格）——远端/P3，本机没有；
//   ② 「换牌库」写口 + `BattleDriver` 开局链的 2 个调用点（原版 `SetupBoardPhase.c:56/66`）。
//   ⇒ 报告里给了可粘贴接线；本文件只保证「判据本体」正确且可单测。

using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 原版 `BoardAnalysis` 一族的**纯查询谓词**（只读、无副作用）。本批只有 `DeckMeetsCriteria` 一件。
    /// ⚠️ 与原版同名的还有别的谓词（`GetManaLeft` 等）—— 本批没加（无消费点）。
    /// </summary>
    public static class BoardAnalysis
    {
        /// <summary>原版 `DeckMeetsCriteria` 的比较档（读 `criteria+0x18`）：`1 = EqualTo`。</summary>
        public const int ModeEqual = 1;
        /// <summary>`2 = AtLeast`（过滤后张数 ≥ 目标）。</summary>
        public const int ModeAtLeast = 2;
        /// <summary>`3 = AtMost`（过滤后张数 ≤ 目标）。</summary>
        public const int ModeAtMost = 3;

        /// <summary>
        /// **这副牌库是否满足一条「按卡逐张筛 + 比较张数」的构筑条件** —— 原版
        /// `BoardAnalysis__DeckMeetsCriteria.c` 的算法等价物（逐句对照见下）。
        ///
        /// <para>原版流程（`.c` 现读）：`param_2`（= 变身卡）的 `deckCriteria(+0x1f8)` 非空、
        /// 其子判据 `(+0x1f8+0x10)` 非空；子判据 count `(+0x10)` **== 0 ⇒ 直接 return 1（无条件即满足）**；
        /// 否则把牌表 `AddRange` 进临时表，逐张 `FilterMethods.CheckIfRawCardMeetsCriteria` 不过就 `Remove`，
        /// 再拿**剩下的张数**与 `(+0x1f8+0x18)` 档位、`(+0x1f8+0x1c)` 目标张数比：
        /// `1 = 相等 · 2 = ≥ · 3 = ≤`；其它档位 `CustomDebug.LogError` 后 **return 1**。</para>
        ///
        /// <para>本方法把这个过程拆成：调用方给 `deck`（牌表）+ `meets`（逐张判据）+ `mode` + `targetCount`。
        /// <c>meets == null</c> 对应原版「子判据 count == 0 ⇒ return 1」（无条件即满足）。</para>
        /// </summary>
        /// <returns>满足条件返回 true。</returns>
        public static bool DeckMeetsCriteria(IReadOnlyList<CardDef> deck,
                                             System.Func<CardDef, bool> meets,
                                             int mode, int targetCount)
        {
            // 原版：子判据 count == 0 ⇒ return 1（没有条件 = 满足）。调用方用 `meets == null` 表达。
            if (meets == null) return true;
            if (deck == null) return false;

            int count = 0;
            for (int i = 0; i < deck.Count; i++)
            {
                var c = deck[i];
                if (c == null) continue;            // 原版对 null 项没有显式处理；这里保守跳过（不影响正常牌表）
                if (meets(c)) count++;
            }

            switch (mode)
            {
                case ModeEqual:   return count == targetCount;
                case ModeAtLeast: return count >= targetCount;
                case ModeAtMost:  return count <= targetCount;
                default:
                    // 原版：认不出的比较档 ⇒ LogError 后 **return 1**（⛔ 不是 false —— 照抄）。
                    UnityEngine.Debug.LogError("[BoardAnalysis] DeckMeetsCriteria 遇到未定义的比较档 " + mode
                        + "（原版 `BoardAnalysis__DeckMeetsCriteria.c` 这一支 LogError 后 return 1）⇒ 按「满足」处理。");
                    return true;
            }
        }
    }
}
