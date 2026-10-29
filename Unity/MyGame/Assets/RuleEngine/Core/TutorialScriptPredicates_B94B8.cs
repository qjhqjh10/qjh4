// TutorialScriptPredicates_B94B8.cs — 第九十四会话 · 波 2 · 执行代理 `B94_B8`
//
// 🔴 为什么单开文件：`RuleEngine/Core/TutorialScript.cs`（`TutorialScript` / `TutorialStageData` 都是
//   **非 partial** 的既有类）不在本批白名单里（只许新建文件）。⇒ 把三条「无落点」判据做成
//   **独立的静态谓词类**，成员全在**本新文件**里；调用点（`BattleDriver`）由调度台按报告里的
//   「可粘贴接线」补。
//
// 本批（B94_B8）覆盖的战斗域「无落点」三条（都属于「休眠 + 判据齐 + 落点明确」那一档）：
//   · `IsPremulliganScriptAvailable(stage)`   —— 账 `战斗逻辑驱动:422`
//   · `PremulliganScriptedActions(stage)`     —— 账 `战斗逻辑驱动:372`（`GetPremulliganScriptedData` 的取数口）
//   · `IsScriptedStageAvailable(ctx)`         —— 账 `战斗逻辑驱动:424`
//
// **第一权威**（本笔逐行现读 2026-10-29，`d:/2/tools/decomp_full/`）：
//   · `BattleManager__IsPremulliganScriptAvailable.c:1-33`
//   · `BattleManager__GetPremulliganScriptedData.c:1-45`
//   · `BattleManager__IsScriptedStageAvailable.c:1-15`

namespace RuleEngine
{
    /// <summary>
    /// 战斗脚本（教程/战役）那几条「无落点」判据的**结构等价物** —— 只做判定 / 取数，
    /// 不碰 `BattleDriver`（那在 `CardPresentation/**`，本批白名单外）。
    /// ⚠️ 落地为引擎侧静态谓词，是为了让 `BattleDriver` 的调用点与引擎只有**一处判据**（别在两处写同一规则）。
    /// </summary>
    public static class TutorialScriptPredicatesB94B8
    {
        /// <summary>
        /// **`preMulligan` 脚本是否要跑** —— 原版 `BattleManager__IsPremulliganScriptAvailable.c:1-33`：
        /// `GetCurrentTutorialStage()` 非空 ∧ `stage+0x50`(preMulligan) 非空 ∧ `*(+0x50+0x18)`(scriptedTurn) != 0
        /// ∧ `*(+0x50+0x20)`(actions) 非空 ∧ `Count >= 1`。
        ///
        /// <para>⚠️ **池内实测**：`tutorial_stages.json` 6 关的 `preMulligan.scriptedTurn` **全是 false**
        /// ⇒ 本谓词今天**恒假** ⇒ 那 19 条动作一次都不执行（休眠）。⛔ 别把「恒假」当成「数据没有」。</para>
        /// </summary>
        public static bool IsPremulliganScriptAvailable(TutorialStageData stage)
        {
            // 🔴 单一正本 = `Core/TutorialPremulligan.cs`：`stage/pre/scriptedTurn/actions/Count` 五道闸逐句照
            //    `BattleManager__IsPremulliganScriptAvailable.c:1-33` / `__GetPremulliganScriptedData.c:1-45` 写在那边；
            //    本条**只转调** —— ⛔ 别两边各写一套（铁律 6：两处写同一条规则 = 迟早不一致）。
            //    原版闸：`*(+0x50+0x18)`(scriptedTurn) != 0 ∧ `*(+0x50+0x20)`(actions) 非空 ∧ `Count >= 1`。
            return TutorialPremulligan.IsPremulliganScriptAvailable(stage);
            // （转调后语义逐位相同：`RuleEngineTest_EngineB94B8` 那四条断言仍然断同一批行为。）
        }

        /// <summary>
        /// 取 `preMulligan` 那条脚本动作组 —— 原版 `BattleManager__GetPremulliganScriptedData.c:1-45`
        /// （四道早退，与 <see cref="IsPremulliganScriptAvailable"/> **同一套闸**；过了才返回 `+0x50`）。
        /// ⛔ **调用方必须先判 <see cref="IsPremulliganScriptAvailable"/> 再取**（原版就是这样两段），
        /// 这里再判一次只为「取不到就返回 null」的安全，不改变语义。
        /// </summary>
        public static TutorialTurn PremulliganScriptedActions(TutorialStageData stage)
        {
            return TutorialPremulligan.GetPremulliganScriptedData(stage);   // 单一正本同上（转调；语义 = 原版 `GetPremulliganScriptedData` 的五道闸）
        }

        /// <summary>
        /// **当前是否处于「脚本关」（教程或战役）** —— 原版 `BattleManager__IsScriptedStageAvailable.c:1-15`：
        /// `IsTutorialStageAvailable() || IsCampaignStageAvailable()`（先教程、再战役），**每个操作数各四道闸**：
        /// ① match 类型（`IsTutorialMatch()` / `IsCampaignLike()`）② `currentTutorialStage|currentCampaignStage` 非空
        /// ③ `turnCounter(+0x3f8) > 0` ④ `turn-1 < stage[+0x48].Count`（🥇 现读 `decomp_full/BattleManager__IsTutorialStageAvailable.c`
        /// 与 `__IsCampaignStageAvailable.c`，两份同形、逐句核过；⚠️ 原版**不要求** `scriptedTurn`）。
        /// 🔴 2026-10-29（`B97_TUT`，判据 = `判据/B97_可推A七条_审查.md` §一·3）：原来只有两条 null 判断 ⇒ 现按原版四道闸补齐。
        /// ⚠️ 战役半 `ctx.CampaignStage` 今天恒 null（无写点）⇒ 教程那一半是唯一可达来源。</summary>
        public static bool IsScriptedStageAvailable(BattleContext ctx)
        {
            if (ctx == null) return false;
            return SingleStageAvailable(ctx.Tutorial != null ? ctx.Tutorial.Stage : null, ctx.Turn) || SingleStageAvailable(ctx.CampaignStage, ctx.Turn);
        }

        /// <summary>上面那一半：**一个操作数的后三道闸（②③④）**（① match 类型由调用方那两个 `!= null` 判）。
        /// 判据 = `decomp_full/BattleManager__IsTutorialStageAvailable.c`（`__IsCampaignStageAvailable.c` 同形）：
        /// `stage == null ⇒ false`（原版 `op_Inequality` 那道）· `turnCounter > 0` · `stage[+0x48] == null ⇒ false` ·
        /// 末句 `CONCAT31(…, iVar2 + -1 &lt; *(int *)(stage[+0x48] + 0x18))` = **`turn - 1 &lt; turns.Count`**（List `_size` 在 `+0x18`）。
        /// ⚠️ 原版那两道重取的 `FUN_1805e3920`（= 再取一次 `currentXxxStage`）在这里由同一个 `stage` 实参表达。</summary>
        static bool SingleStageAvailable(TutorialStageData stage, int turn)
        {
            if (stage == null) return false;                                   // ② base stage 非空
            if (turn <= 0) return false;                                       // ③ turnCounter > 0
            var turns = stage.turns;                                           // stage[+0x48]
            if (turns == null) return false;                                   // 原版 `*(+0x48) == 0 ⇒ goto LAB ⇒ false`
            return (turn - 1) < turns.Length;                                  // ④ turn-1 < Count
        }
    }
}
