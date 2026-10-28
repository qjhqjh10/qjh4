// TutorialPremulligan.cs — 原版开局「换牌前脚本」的**谓词 + 取值器**（数据侧那一半）
//
// ==================================================================================================
//  这是什么（第九十四会话 · 波 2 · 执行代理 `B3` · 账 `战斗逻辑驱动:372 / :422`）
// ==================================================================================================
//  原版两个方法（第一权威，逐句现读 `d:/2/tools/decomp_full/`，2026-10-29）：
//    · `BattleManager.GetPremulliganScriptedData()`（`BattleManager__GetPremulliganScriptedData.c` 全文）：
//        取 `GetCurrentTutorialStage().preMulliganScriptedActions`，**先过一遍可用性**再返回那一个对象；
//        任一项不满足 ⇒ 返回 `null`。逐句：
//          `stage == null` ⇒ null；`pre == null` ⇒ null；`pre.scriptedTurn(+0x18) == false` ⇒ null；
//          `pre.scriptedActions(+0x20) == null` ⇒ null；`scriptedActions.Count(+0x18) < 1` ⇒ null；
//          否则返回 `stage.preMulliganScriptedActions`。
//    · `BattleManager.IsPremulliganScriptAvailable()`（`BattleManager__IsPremulliganScriptAvailable.c` 全文）：
//        与上面**完全同一套闸**，结论压成 bool ⇒ `GetPremulliganScriptedData(...) != null`。
//      （两方法同源：`.c` 里 `IsPremulligan…` 的三段 = `+0x50 != 0 && +0x18(scriptedTurn) != 0 && +0x20(list) != 0 && list.Count > 0`。）
//
//  我们的数据侧等价物 = `TutorialStageData.preMulligan`（`TutorialScript.cs` 的 `TutorialTurn`）：
//    `preMulligan != null && preMulligan.scriptedTurn && preMulligan.actions != null && preMulligan.actions.Length >= 1`。
//
//  ⚠️ **只做了「① 谓词 + 取值」这一半**（本文件，自包含、可被任意调用方消费）。
//     **② 执行那一半仍未接**（原版 `_StartBattleSequence` 在换牌之前起它、并把换牌/首回合延后到它跑完；
//     我们的 `Begin` 是同步的 ⇒ 要延后得给开局链加异步桥）——那在 `BattleDriver`（非本代理白名单）。
//     如实标：**谓词已建，执行接线待 `BattleDriver`**，账 `372/422` 由 C 提为 B。
//  📌 数据现状：6 关 `preMulligan.scriptedTurn` **全 false**（`tutorial_stages.json`）⇒ 本谓词**恒假**，
//     19 条 preMulligan 动作原版从不执行 ⇒ 接上也不改任何对局（休眠如实标，⛔ 不判「没有」）。
// ==================================================================================================
namespace RuleEngine
{
    /// <summary>原版开局「换牌前脚本」的谓词 + 取值器（见文件头）。</summary>
    public static class TutorialPremulligan
    {
        /// <summary>原版 `BattleManager.GetPremulliganScriptedData()`
        /// （`decomp_full/BattleManager__GetPremulliganScriptedData.c` 全文，逐句）。
        /// <para>可用（`preMulligan != null && scriptedTurn && actions.Length >= 1`）⇒ 返回那一个 `TutorialTurn`；
        /// 否则 <c>null</c>（与原版「任一闸不过 ⇒ 返回 null」逐字对齐）。</para></summary>
        public static TutorialTurn GetPremulliganScriptedData(TutorialStageData stage)
        {
            if (stage == null) return null;
            var pre = stage.preMulligan;
            if (pre == null) return null;
            if (!pre.scriptedTurn) return null;                                  // 原版 `+0x18(scriptedTurn) == false`
            if (pre.actions == null) return null;                               // 原版 `+0x20(list) == null`
            if (pre.actions.Length < 1) return null;                            // 原版 `list.Count(+0x18) < 1`
            return pre;
        }

        /// <summary>原版 `BattleManager.IsPremulliganScriptAvailable()`
        /// （`decomp_full/BattleManager__IsPremulliganScriptAvailable.c` 全文）——
        /// 与 <see cref="GetPremulliganScriptedData"/> **同一套闸**，结论压成 bool。
        /// <para>⚠️ 6 关 `scriptedTurn` **全 false** ⇒ 今天恒 <c>false</c>（激活后零行为变化，休眠如实标）。</para></summary>
        public static bool IsPremulliganScriptAvailable(TutorialStageData stage)
        {
            return GetPremulliganScriptedData(stage) != null;
        }
    }
}
