// B97W37_Assert.cs — 第九十七会话 · 执行代理 `B97_W37`（2026-10-29 · 波 4-D 表现层）
//
// ============================ 它盯的是什么 ============================
// 本代理**没有改任何行为**（6 条里 5 条是核销 / 判档，1 条卡在白名单外）⇒ 本件只钉**四条「账已经翻档」之后
// 就不该再变的东西**，让下一次改动把旧账撞红，而不是静默漂回去：
//
//   · **账 `战斗逻辑驱动:381`**（`BattleManager.GetTargetInPos`）—— 我们侧的对应物 = `BattleDriver.HitSlot`
//     （原版真生效支 `decomp_full/BattleManager__GetTargetInPos.c:204-265`：`boardCamera.ScreenPointToRay`
//      → `Physics.RaycastNonAlloc(200f, 1<<BoardElements)` → 首条 `GetComponentInParent<CardScript>()`）。
//     ⇒ 钉两件：**卡心投影点必中**（`PointerAtWorldForTest` 那一跳桥）· **远处点必须 −1**。
//
//   · **账 `战斗逻辑驱动:457`**（`BattleManager.PlayAi`）—— 判 D 的**唯一理由**是「三个动作延迟区间在远端
//     PlayFab `TitleData.AIBotsConfig`、本地 0 文件」⇒ 我们那三档是 **🔶 自己设计**（`BattleDriver.cs:34691-34700`）。
//     ⇒ 钉：区间必须 0 < min < max ≤ 3s、24 次取样都落在区间内、三档不许是同一个函数（否则等于区间丢了两档）。
//
//   · **账 `战斗逻辑驱动:483`**（`BattleManager.RemoveSelfBuffsFromQueues`）—— `B97_W3` 已经把**表现层那一句**
//     接在「刚翻面成残骸」那一拍（`BattleDriver.cs:21855`，原版唯一调用点 = `_ResolveTransformIntoRemnant…:39`）。
//     `B97W3_Assert` 断的是**被调方法的语义**，**断不了调用点还在**（它自己的文件头写明「只能靠人工/审查」）
//     ⇒ 本件补一条**源码文本**检查（恰好 1 处）。
//
//   · **账 `战斗逻辑驱动:804`**（`CardScript.MinionPlayedIntoField`）—— 全池**唯一**一张带 `summonAnimDelayed`
//     的卡 = `SOR33 Saint Celestine`（判据 → 报告 §四）。⇒ 钉**数据线**：那张卡的「召唤延迟动画」prefab 名还在
//     运行时特效表里（且只有 1 条）。⚠️ 这条**同时是一条「待修形态」的钉子**（见 W37-804-2 的说明）。
//
// ============================ ⛔ 本件**故意**没有的断言 ============================
//   · 账 `:89`（`AiScripted.CheckIfWaitingToClickCard`）**不写**：等价物两边**都已经有断言**，且都在本件白名单外 —
//     ① `RuleEngine/Editor/RuleEngineTest_EngineB94B19.cs:247-252`（`TutorialScript.Matches` 的 `TapCard` 支：
//        认 `useWaystone`、其**余动作不掉到 `ret 0`**，带灭自证说明）；
//     ② `BattleDriver.TapBoardCard` 那六档口径写在头注里（`:33860-33896`）、推进链由教程那一组断言覆盖。
//     ⇒ 再写一份 = 同一口径三处（本仓红线）。
//   · 账 `:381` 的「换口前后逐点一致」（`HitSlotForTest` vs `HitSlotByContainsForTest`）**已有断言**
//     （`Editor/B97_UI2_Assert.cs:97-101`、`Editor/BattleScene.cs:38804-38810`）⇒ ⛔ 不重复。
//   · 账 `:1178`（`PlayerHand.MoveCardToMulliganPos`）**不写**：判据与几何表已出（报告 §五），
//     但**几何值是等真机 `A1749` 复测**的东西（画面可见、不进指纹）⇒ 现在写期望值就是拿实拍冒充算法。
//   · 账 `:804` 的 ①「summon 件从通用 `deploy` 桶分出」+ 闸、② state 4 的 GO 隐藏 —— **没有代码改动**，
//     所以也没有可断的行为（数据盘上产物未重生成 + 消费端 DTO 在 `Core/VfxMap.cs`，均白名单外，见报告 §四）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B97W37_Assert.CheckB97W37(driver);`
//   · 381 段要**这一局已经打起来**（`Ctx != null`）且棋盘上至少有一个我方单位**且有视图**；
//     找不到 ⇒ **红**（⛔ 不静默跳过 —— 那是本仓踩过 3 次的坑）。
//   · 457 / 804 段**不需要**驱动实例（纯静态/数据），任何位置都能接。
//   · 483 段要读**源码文件**（`Application.dataPath` 下的 `.cs`）⇒ 只在编辑器里成立（本文件本来就在 `Editor/`）。
//   · 本件**不改任何状态**（381 只读 `HitSlot`，不做点击；不排 cue、不写棋盘）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W37`：账 `战斗逻辑驱动:381 / 457 / 483 / 804` 的**翻档后不变量**读数口。
/// <para>⛔ 期望值一律**字面量**（−1 / 1 / 具体 prefab 名），不从被测实现里反算。</para></summary>
public static class B97W37_Assert
{
    static readonly CheckSink _sink = new CheckSink("[W37] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>本件唯一入口（宿主 `Run()` 里用一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，同 `B97W3_Assert` 的理由）。</summary>
    public static void CheckB97W37(BattleDriver drv = null)
    {
        MenuCheck.Section(_sink, "W37：账 381 / 457 / 483 / 804 —— 核销与判档之后的**不变量**");
        Check381HitSlotIsBoardElements(drv);
        Check457AiDelayRanges(drv);
        Check483CallSitePresent();
        Check804SummonAnimDataLine();
        Debug.Log(_sink.P + "W37 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  账 381：`GetTargetInPos` 的等价物 = `HitSlot`（只认【棋盘元素】）
    //    判据 = `decomp_full/BattleManager__GetTargetInPos.c:204-265`（真生效支）
    //         · 2D 支 `Physics2D.GetRayIntersectionNonAlloc(ray, buf, -1f)` 负距离 = **死支**、照抄不实现
    //    我们侧 = `BattleDriver.HitSlot`（`:33202`）→ `CardHit`（`:33233`，3D 档 4 角投影 / 2D 档 `HitByCollider`）
    //    桥 = `PointerAtWorldForTest`（卡的世界点 → 生产指针那个正交平面）
    // ==================================================================
    static void Check381HitSlotIsBoardElements(BattleDriver drv)
    {
        if (drv == null) drv = Object.FindFirstObjectByType<BattleDriver>();
        Tr(drv != null, "W37-381-0（前提）取得到 `BattleDriver`（⛔ 取不到就红，不静默跳过）");
        if (drv == null) return;
        Tr(drv.Ctx != null, "W37-381-0b（前提）这局已经打起来（`Ctx != null`）—— 本段要读棋盘与场上视图");
        if (drv.Ctx == null) return;

        int side = drv.MySideForTest;
        var board = drv.Ctx.Players[side].Board;
        int slot = -1;
        Vector3 ptr = Vector3.zero;
        for (int s = 0; s < board.Length; s++)
        {
            if (board[s] == null) continue;
            var v = drv.BoardViewForTest(side, s);
            if (v == null) continue;
            Vector3 p = drv.PointerAtWorldForTest(v.transform.position);
            if (drv.HitSlotHitCountForTest(side, p) >= 1) { slot = s; ptr = p; break; }
        }
        Tr(slot >= 0, "W37-381-1（前提）找得到一个「卡心投影点确实落进某张卡的命中区」的我方在场单位"
                    + "（棋盘上没单位 / 没视图 / 谁都命中不了 ⇒ 红：这一段没有判别力）");
        if (slot < 0) return;

        C(drv.HitSlotForTest(side, ptr), slot,
            "W37-381-2 ★ 把一张卡**画在屏幕上的那个位置**喂给 `HitSlot` ⇒ 必须返回**这一格**"
          + "（= 原版真生效支：射线命中 `BoardElements` 层的棋盘元素）"
          + "｜🧨 把 `CardHit` 的「3D 档 4 角投影桥」删掉（直接拿指针点问 `Physics2D`）⇒ 我方整排点不到 ⇒ 本条红；"
          + "🧨 把 `HitSlot` 改成只认手牌 `CardInteraction.Frontmost` ⇒ 红");

        C(drv.HitSlotForTest(side, new Vector3(9000f, 9000f, 0f)), -1,
            "W37-381-3 ★ 远离所有卡的指针点 ⇒ **−1**"
          + "｜🧨 让 `CardHit` 恒 true、或给 `HitSlot` 加「没命中就兜底第 0 格」⇒ 本条红"
          + "（原版那条 `GetComponentInParent<CardScript>()` 空 ⇒ 返回 null，同样是「没点中」）");
    }

    // ==================================================================
    //  账 457：`PlayAi` —— 判 D，理由 = 三档延迟在远端 `AIBotsConfig`
    //    🔶 自己设计（非原版）：`AiActionDelay*` / `AiFirstActionDelay*` / `AiEndTurnDelay*`（`BattleDriver.cs:34691-34700`）
    //       · 为什么必须自定：`AIBotsConfig` / `skipChance` / `minLossStreak` 在 `assets_full` 与关服前存档里 0 文件，
    //         唯一来源 = 远端 `ConfigManager.GetConfig<AIBotsConfig>()`（时间限定 = 当前 build）
    //       · 调度台 2026-10-29 判丙（判据空 ⇒ D），值保留为本仓挑的区间
    // ==================================================================
    static void Check457AiDelayRanges(BattleDriver drv)
    {
        float[] mins = new float[3], maxs = new float[3];
        for (int which = 0; which < 3; which++)
        {
            float min, max;
            BattleDriver.AiDelayRangeForTest(which, out min, out max);
            mins[which] = min; maxs[which] = max;
            string nm = which == 0 ? "每步 `GetActionDelay`" : which == 1 ? "回合首步" : "收手 `GetEndTurnDelay`";
            Tr(min > 0f && max > min && max <= 3f,
                "W37-457-" + which + "a ★ " + nm + " 的区间必须 `0 < min < max ≤ 3s`，实得 [" + min + ", " + max + "]"
              + "（🔶 自己设计：原版值在远端 `AIBotsConfig`、本机拿不到 ⇒ 只钉「区间存在且可用」）"
              + "｜🧨 把常量删成 0 / 把 min 写到 max 之上 / 把区间拉成几十秒 ⇒ 本条红（AI 会立刻连打或永远不动）");

            if (drv == null) continue;                       // 取样要实例方法；宿主没给驱动就只做区间那一条
            bool allIn = true;
            float got = 0f;
            for (int i = 0; i < 24; i++)
            {
                got = drv.NextAiDelayForTest(which);
                if (got < min || got > max) { allIn = false; break; }
            }
            Tr(allIn, "W37-457-" + which + "b ★ 24 次取样都落在 [" + min + ", " + max + "] 内"
                    + (allIn ? "" : "（越界那次 = " + got + "）")
                    + "｜🧨 让 `NextAi…Delay()` 用错上下界 / 返回常量以外的东西 ⇒ 本条红");
        }

        bool same01 = Mathf.Approximately(mins[0], mins[1]) && Mathf.Approximately(maxs[0], maxs[1]);
        bool same12 = Mathf.Approximately(mins[1], mins[2]) && Mathf.Approximately(maxs[1], maxs[2]);
        Tr(!(same01 && same12), "W37-457-3 ★ 三档区间**不许完全同一**（" + mins[0] + "–" + maxs[0] + " / "
                              + mins[1] + "–" + maxs[1] + " / " + mins[2] + "–" + maxs[2] + "）"
                              + "｜🧨 把 `NextAiFirstActionDelay` / `NextAiEndTurnDelay` 接回同一个函数（三档合一）⇒ 本条红");
    }

    // ==================================================================
    //  账 483：`RemoveSelfBuffsFromQueues` 的**唯一**调用点必须在（表现层那一句）
    //    原版调用点 = `BattleManager._ResolveTransformIntoRemnant_d__554.MoveNext.c:39`（全树只此一处）
    //    我们 = `BattleDriver.cs:21855`（`PlayRemnantSfx` 的 `now.IsRemnant` 支）
    //    ⚠️ 被调方法的**语义**由 `B97W3_Assert` 覆盖（撤几条 / 再撤 0 条 / 不碰 cueAction）⇒ 这里只管**调用点还在**
    // ==================================================================
    static void Check483CallSitePresent()
    {
        const string Needle = "RemoveSelfBuffsFromQueues(e.Player, e.Slot)";
        string p = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        Tr(File.Exists(p), "W37-483-0（前提）源码文件在：" + p + "（读不到 ⇒ 红：本段是源码级检查，⛔ 别改成跳过）");
        if (!File.Exists(p)) return;

        string src = File.ReadAllText(p);
        int n = 0, at = 0;
        while (true)
        {
            at = src.IndexOf(Needle, at, System.StringComparison.Ordinal);
            if (at < 0) break;
            n++; at += Needle.Length;
        }
        C(n, 1, "W37-483-1 ★ `" + Needle + "` 在 `BattleDriver.cs` 里必须**恰好 1 处**（原版这方法的唯一调用点）"
              + "，实得 " + n
              + "｜🧨 删掉 `PlayRemnantSfx` 的 `now.IsRemnant` 支那一句 ⇒ 0 处 ⇒ 红；"
              + "把它再挂到「普通阵亡 / 单位离场」上 ⇒ ≥2 处 ⇒ 红（那不是原版这个调用点）");
    }

    // ==================================================================
    //  账 804：`MinionPlayedIntoField` 的 `summonAnimDelayed` 数据线
    //    判据 = `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:186-205`（state 3）：
    //           `shouldTriggerSummon == 0` ∨ `HasCurrentTrait(5 ephemeral)` ∨ `HasCurrentTrait(0x4b0 ambush)` ⇒ 直接等 AttackStepTime；
    //           否则先 invoke `m_OnDisplaySummonMessage`、再按 `rawCardData +0xB0` 的 AssetReference
    //           在**卡的位置**实例化那件动画（写在 `LandOnGround()` 之后、尾拍 `WaitForSeconds(AttackStepTime)` 之前）
    //    全池普查：1,351 张卡 SO 里该字段非空 **1 张** = `SOR33 Saint Celestine` ⇒ prefab `Sororitas Saint Custom summon`
    // ==================================================================
    static void Check804SummonAnimDataLine()
    {
        const string Prefab = "Sororitas Saint Custom summon";

        Tr(VfxMap.CardTableCards > 0, "W37-804-0（前提）运行时逐卡特效表已载入（`VfxMap.CardTableCards > 0`）");
        if (VfxMap.CardTableCards <= 0) return;

        string p = Path.Combine(Application.dataPath, "CardPresentation/Resources/card_vfx_by_card.json");
        Tr(File.Exists(p), "W37-804-0b（前提）运行时特效表文件在：" + p);
        if (!File.Exists(p)) return;

        string js = File.ReadAllText(p);
        int n = 0, at = 0;
        while (true)
        {
            at = js.IndexOf("\"" + Prefab + "\"", at, System.StringComparison.Ordinal);
            if (at < 0) break;
            n++; at += Prefab.Length;
        }
        C(n, 1, "W37-804-1 ★ 全池唯一一张带 `summonAnimDelayed` 的卡（`SOR33 Saint Celestine`）的那件 prefab"
              + "「" + Prefab + "」在运行时表里必须**恰好 1 条**，实得 " + n
              + "｜🧨 重跑 `gen_card_vfx_by_card.py` 时把这个 SO 字段漏掉 / 多记一次 ⇒ 红");

        C(VfxMap.ResolvePerCard(VfxMap.Deploy, "Sororitas", "SOR33"), Prefab,
            "W37-804-2（**现状钉**，不是「已对齐」）★ 今天这件 summon 动画**仍从通用 `deploy` 桶**解析出来"
          + " ⇒ 原版那条「state 3 在 `LandOnGround` 之后单独实例化、且过 `shouldTriggerSummon ∧ 非 ephemeral/ambush`」"
          + "**还没分桶**（账 `:804` 保持 B；块点 = 数据盘上产物待重生成 + 消费端 DTO 在 `Core/VfxMap.cs`，均白名单外）"
          + "｜🧨 数据重生成后条目会带 `so=\"summonAnimDelayed\"`、消费端改走独立桶 ⇒ 本条**会红**："
          + "到那时按报告 §四 的三步把它改成「`so` 桶」断言（⛔ 别直接删掉它 —— 那等于把这条缺口洗白）");
    }
}
