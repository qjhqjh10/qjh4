// BattleMiscAssertE4.cs — 执行代理 `E4`（2026-10-24）的三件断言宿主
//
// 本文件**只放断言**（⛔ 不改任何产品行为、不建场景、不进 `Editor/BattleScene.cs`）。
// 调用方式（主对话在 `BattleScene.RunCore` 的收尾处加一行）：
//
//     BattleMiscAssertE4.Run();          // ← 建议插在 `Debug.Log(P + $"=== 结束：{pass} 通过 / {fail} 失败 ===");`
//                                        //     **那一行之前**（即所有既有小节之后、`CheckSavedScene` 之前）
//
// 为什么不放进 `BattleScene.cs`：本轮白名单不许动它（`E4` 只能新建本文件）。本类自己记一份
// `Pass` / `Fail`（主对话想并进总数就直接加这两格）。
//
// ==================================================================
//  三件覆盖的内容（判据逐条写在每条断言的消息里）
// ------------------------------------------------------------------
//  A. **`mulliganEnabled` 进录像头**（`ReplayStore.mulliganPlus1` · 判据 → `普查产出_1023e/F2_战斗红修复.md`
//     的「顺手发现 3」/ `普查产出_1024/A2_战斗VFX五件.md` §四）。
//     验收标准 = **同一局在开关两种取值下回放结果不同、加了字段后一致** ⇒ 这里做成**差分实验**：
//       · 正面：driver 的字段故意设成 `false`、录像头说 `true` ⇒ 回放**演成同一局**；
//       · 反面（灭自证）：**只把录像头那一格抹成 `0`**（= 老录像）、其余一个字不动 ⇒ 回放**演不成同一局**；
//       · 差分只动了一个变量 ⇒ 用两份录像 JSON 逐字节对账（`mulliganPlus1` 之外必须逐字节相同）。
//  B. **换牌阶段 / 换牌刚结束就投降的录像，终局指纹不再差一格**（判据 → `普查产出_1023e/A29_回放展开.md` §6.4）。
//     两种形状各验一遍：① 动作流里**没有** `MulliganDone`（回合从来没开过）；② 有 `MulliganDone`（录制端回合已开 = 1）。
//  C. **对手聊天「发言触发面」**（判据 → `VoiceLinesController__DisplayWarmonger…` 见下）：
//     · `attack` / `death` 两档**只放音、不弹气泡**（原版 `BattleManager.PlaySoundClip`）；
//     · `deploy` 仍然弹气泡（回归闸：别把气泡一起关掉）；
//     · 「有出场语音、但台词为空 ⇒ 不弹气泡」（原版 `DisplaySummonMessage.c:36-75` 的 `IsNullOrEmpty` 支）。
// ==================================================================
using System.Collections.Generic;
using CardPresentation;
using CardPresentation.Net;      // `NetActionKind.MulliganDone`
using RuleEngine;
using UnityEngine;

public static class BattleMiscAssertE4
{
    const string P = "[E4] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进 `BattleScene` 的总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        bool CheckTrue(bool ok, string msg)
        {
            if (ok) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
            Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
        }
        bool Check<T>(T got, T want, string msg)
        {
            bool ok = EqualityComparer<T>.Default.Equals(got, want);
            return CheckTrue(ok, msg + $"（实得 {got} / 期望 {want}）");
        }

        var driver = Object.FindObjectOfType<BattleDriver>();
        if (driver == null)
        {
            CheckTrue(false, "找不到 `BattleDriver`（本节全部断言没法验 —— ⛔ 不许静默跳过）");
            Debug.Log(P + $"=== E4 合计：{Pass} 通过 / {Fail} 失败 ===");
            return;
        }

        RunReplayHeader(driver, CheckTrue, Check);
        RunMulliganForfeitReplay(driver, CheckTrue, Check);
        RunChatTriggerSurface(driver, CheckTrue, Check);

        Debug.Log(P + $"=== E4 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  A · 录像头里那一格 `mulliganPlus1`（差分实验：加字段前/后结果不同）
    // ==================================================================
    static void RunReplayHeader(BattleDriver driver,
                                System.Func<bool, string, bool> CheckTrue,
                                System.Func<int, int, string, bool> CheckInt)
    {
        Debug.Log(P + "--- A · 录像头 `mulliganPlus1`（差分：同一局、两种开关取值）---");

        bool recSave = BattleDriver.RecordReplays;
        bool mulSave = driver.mulliganEnabled;
        bool traceSave = ReplayStore.VerboseTrace;
        try
        {
            ReplayStore.VerboseTrace = true;        // 逐条轨迹（回放的「首处分叉」就靠它）
            BattleDriver.RecordReplays = true;
            driver.mulliganEnabled = true;          // 录一局**开着换牌**的

            driver.Begin("Ultramarines", "Goff", 20261024);
            if (!CheckTrue(driver.Ctx != null && driver.Ctx.MulliganOpen,
                           "（前提）A：这一局**开着换牌阶段**（`Ctx.MulliganOpen`）"))
            { Debug.Log(P + "   （前提不成立 ⇒ A 余下各条跳过，且**已经红了一条**）"); return; }

            if (driver.InMulligan) driver.SimulateMulliganDone();
            int guard = 0;
            while (driver.Ctx != null && !driver.Ctx.IsOver && guard++ < 400) driver.SimulateAiTurn();
            CheckTrue(driver.Ctx != null && driver.Ctx.IsOver && driver.Ctx.Turn > 0,
                      "（前提）A：这一局真的打完了（循环 ≤ 400 步、`Ctx.IsOver`）");

            string file = driver.LastReplayFile;
            var rec = string.IsNullOrEmpty(file) ? null : ReplayStore.Load(file);
            if (!CheckTrue(rec != null && rec.start != null && rec.actions != null && rec.actions.Count > 0,
                           "（前提）A：这一局的录像读得回来、动作流非空"))
            { Debug.Log(P + "   （前提不成立 ⇒ A 余下各条跳过，且**已经红了一条**）"); return; }

            CheckRecordedHeader(rec, driver, CheckTrue, CheckInt);

            // ---- 正面：driver 的字段**故意**设成与录像头相反 ----
            driver.mulliganEnabled = false;
            bool same = driver.PlayReplay(rec);
            CheckTrue(same && driver.ReplayDivergenceForTest < 0,
                      "★★ A2：**录像头在场 ⇒ 同一份动作流演成同一局**"
                    + $"（`PlayReplay` 返回 {same}，逐条轨迹首处分叉 = {driver.ReplayDivergenceForTest}，期望 -1）"
                    + " —— 录制时 `mulliganEnabled = true`、此刻 driver 字段是 `false`，"
                    + "只有录像头那一格能把它救回来｜🧨 删掉 `PlayReplay` 里按头恢复那一句 ⇒ 本条红");
            CheckInt(driver.MulliganSeenByBeginForTest, 2,
                     "★ A3：它**真的传到了 `RuleCore.NewBattle(openMulligan:)`**（`2` = true）"
                   + " —— 🧨 只在驱动字段上写一下、没在 `BeginFromPendingCore` **之前**恢复 ⇒ 红");
            CheckInt(driver.ReplayMulliganPlus1AppliedForTest, rec.mulliganPlus1,
                     "★ A3b：回放**读口**（`ReplayMulliganPlus1AppliedForTest`）读到的就是录像头那一格"
                   + " —— 这一格与 A3 分开断：`AppliedForTest` 证明「读了口」、`SeenByBeginForTest` 证明「真喂进去了」");
            CheckTrue(driver.mulliganEnabled == false,
                      "★ A4：回放收尾的 `finally` 把 `mulliganEnabled` **还原成回放前的值**（这里是 `false`）"
                    + " —— 🧨 删掉那个 `finally` ⇒ 回放的档会留给下一局 ⇒ 红");

            // ---- 反面（灭自证）：**只**抹掉录像头那一格 = 老录像 ----
            var oldRec = ReplayStore.Load(file);
            if (oldRec == null) { CheckTrue(false, "（前提）A：第二份录像读不回来 ⇒ 反例没法验"); return; }
            oldRec.mulliganPlus1 = 0;
            driver.mulliganEnabled = false;         // 与正面**同一个** driver 字段值（唯一的差别就是那一格）
            bool sameOld = driver.PlayReplay(oldRec);
            CheckTrue(!sameOld,
                      "★★ A5（灭自证）：**抹掉录像头那一格 ⇒ 同一局回放结果不同**"
                    + $"（返回 {sameOld}，期望 false）"
                    + " —— 这一条正面证明了「不记这一格会怎样」，与 A2 合起来才是"
                    + "「同一局在开关两种取值下回放结果不同、加了字段后一致」");
            CheckTrue(driver.ReplayDivergenceForTest >= 0,
                      $"★ A6：…而且失败原因是**真的轨迹分叉**（首处分叉 = 第 {driver.ReplayDivergenceForTest} 条，期望 ≥ 0）"
                    + " —— 而不是「开局头解不出 / 录像读不出来」那一类（那些会让 A5 因为错的原因变绿）");

            // ---- 差分只动了一个变量：两份录像 JSON 除这一格外必须逐字节相同 ----
            string jGood = JsonUtility.ToJson(rec);
            string jOld = JsonUtility.ToJson(oldRec);
            string jPatched = jOld.Replace("\"mulliganPlus1\":0", "\"mulliganPlus1\":" + rec.mulliganPlus1);
            CheckTrue(jGood != jOld && jPatched == jGood,
                      "★★ A7：两次回放用的录像**只差 `mulliganPlus1` 这一格**（其余逐字节相同）"
                    + " —— 这样才能把 A2 / A5 的差别**归因**到这一格上"
                    + $"（正面长 {jGood.Length} 字节 / 反面 {jOld.Length} 字节 / 补回来的相等 = {jPatched == jGood}）");
        }
        finally
        {
            BattleDriver.RecordReplays = recSave;
            driver.mulliganEnabled = mulSave;
            ReplayStore.VerboseTrace = traceSave;
        }
    }

    /// <summary>录像头的哨兵纪律（`0` = 没记 / `1` = false / `2` = true）+ 这一局真记了。</summary>
    static void CheckRecordedHeader(ReplayRecord rec, BattleDriver driver,
                                    System.Func<bool, string, bool> CheckTrue,
                                    System.Func<int, int, string, bool> CheckInt)
    {
        var unit = new ReplayRecord();
        CheckTrue(unit.MulliganEnabledForReplay == null,
                  "★ A1a（哨兵）：**没记**（`mulliganPlus1 = 0`）读口回 `null`、**不是 `false`**"
                + " —— 老录像的 JSON 里没有这个键 ⇒ 必须退回「driver 当时的字段值」");
        unit.mulliganPlus1 = 1;
        CheckTrue(unit.MulliganEnabledForReplay == false, "★ A1b：存 `1` ⇒ 读回 `false`");
        unit.mulliganPlus1 = 2;
        CheckTrue(unit.MulliganEnabledForReplay == true,
                  "★ A1c：存 `2` ⇒ 读回 `true` —— 🧨 把字段改成 `bool`（默认 `false`）⇒「没记」与「真的 false」会混 ⇒ A1a 红");
        CheckInt(rec.mulliganPlus1, 2,
                 "★ A1d：这一局是**开着换牌**录的 ⇒ 录像头必须记 `2`"
               + " —— 🧨 删掉 `RecBegin` 里 `mulliganPlus1 = mulliganEnabled ? 2 : 1` ⇒ 0 ⇒ 红");
    }

    // ==================================================================
    //  B · 换牌阶段 / 换牌刚结束就投降 ⇒ 回放终局指纹不差一格
    // ==================================================================
    static void RunMulliganForfeitReplay(BattleDriver driver,
                                         System.Func<bool, string, bool> CheckTrue,
                                         System.Func<int, int, string, bool> CheckInt)
    {
        Debug.Log(P + "--- B · 换牌阶段就投降的录像（A29 §6.4 的「差一格」）---");

        bool recSave = BattleDriver.RecordReplays;
        bool mulSave = driver.mulliganEnabled;
        bool traceSave = ReplayStore.VerboseTrace;
        try
        {
            ReplayStore.VerboseTrace = true;
            BattleDriver.RecordReplays = true;
            driver.mulliganEnabled = true;

            // ---- B1 · 动作流里**没有** `MulliganDone`（回合从来没开过）----
            driver.Begin("Ultramarines", "Goff", 20261025);
            if (!CheckTrue(driver.Ctx != null && driver.Ctx.MulliganOpen,
                           "（前提）B1：开局停在换牌阶段"))
            { Debug.Log(P + "   （前提不成立 ⇒ B1 跳过）"); }
            else
            {
                int turnAtRecord = driver.Ctx.Turn;
                driver.Forfeit();
                CheckTrue(driver.Ctx.IsOver && driver.Ctx.Winner != 0, "（前提）B1：投降即终局");
                var recB = ReplayStore.Load(driver.LastReplayFile ?? "");
                if (!CheckTrue(recB != null, "（前提）B1：录像读得回来"))
                { Debug.Log(P + "   （前提不成立 ⇒ B1 余下跳过）"); }
                else
                {
                    bool noDone = !recB.actions.Exists(a => a != null && a.kind == NetActionKind.MulliganDone);
                    CheckTrue(noDone,
                              "（前提）B1：动作流里**没有 `MulliganDone`** ⇒ 这一份真的是「换牌阶段就投降」那个形状"
                            + "（⛔ 不是拿普通对局冒充）");
                    CheckInt(turnAtRecord, 0, "（前提）B1：录制时回合号 = 0（回合从来没开过）+ 终局指纹已记");
                    bool sameB = driver.PlayReplay(recB);
                    CheckTrue(sameB,
                              "★★ B1：**换牌阶段就投降的录像，回放终局指纹与录制时【相等】**"
                            + "（A29 §6.4 那条「终局指纹仍可能差一格」＝已闭合）"
                            + "｜🧨 在回放侧多开一个回合（或漏补）⇒ 终局 `StateHash` 差一格 ⇒ 红");
                    CheckInt(driver.ReplayDivergenceForTest, -1,
                             "★ B1：…而且逐条轨迹**一条都没分叉**（回放真的按同一串动作走完）");
                    CheckInt(driver.Ctx.Turn, turnAtRecord,
                             "★ B1：回放终局回合号 = 录制时（**没有**多开一个回合 —— 「差一格」就是这个症状）");
                }
            }

            // ---- B2 · 动作流里**有** `MulliganDone`，紧接着就是 `Forfeit`（录制端回合已开 = 1）----
            driver.Begin("Ultramarines", "Goff", 20261026);
            if (!CheckTrue(driver.Ctx != null && driver.Ctx.MulliganOpen,
                           "（前提）B2：开局停在换牌阶段"))
            { Debug.Log(P + "   （前提不成立 ⇒ B2 跳过）"); }
            else
            {
                if (driver.InMulligan) driver.SimulateMulliganDone();
                int turnAtRecord2 = driver.Ctx.Turn;
                CheckInt(turnAtRecord2, 1,
                         "（前提）B2：换牌一结束，**录制端回合已经开了 = 1** —— 这正是「差一格」那个形状");
                driver.Forfeit();
                var recC = ReplayStore.Load(driver.LastReplayFile ?? "");
                if (!CheckTrue(recC != null, "（前提）B2：录像读得回来"))
                { Debug.Log(P + "   （前提不成立 ⇒ B2 余下跳过）"); }
                else
                {
                    CheckTrue(recC.actions.Exists(a => a != null && a.kind == NetActionKind.MulliganDone),
                              "（前提）B2：动作流里有 `MulliganDone`（紧接着是那条 `Forfeit`）");
                    bool sameC = driver.PlayReplay(recC);
                    CheckTrue(sameC,
                              "★★ B2：**「换牌一结束就投降」的录像，回放终局指纹相等**"
                            + " —— 回放侧必须在 `Forfeit` **落地之前**把回合补开"
                            + "（`CompleteReplayBeginTurn`：先消费 `NetApply` 的待办标记，"
                            + "被 `IsOver` 挡成空操作时才走 `RuleCore.BeginTurnForReplayExpansion`）"
                            + "｜🧨 删掉 `PlayReplay` 的 `RecKindForfeit` 支里那一句 ⇒ 回合少开一次 ⇒ 红");
                    CheckInt(driver.ReplayDivergenceForTest, -1, "★ B2：逐条轨迹零分叉");
                    CheckInt(driver.Ctx.Turn, turnAtRecord2,
                             "★ B2：回放终局回合号 = 录制时的 1（不多不少）");
                }
            }
        }
        finally
        {
            BattleDriver.RecordReplays = recSave;
            driver.mulliganEnabled = mulSave;
            ReplayStore.VerboseTrace = traceSave;
        }
    }

    // ==================================================================
    //  C · 对手聊天「发言触发面」：哪一档弹气泡、哪一档只放音
    // ==================================================================
    static void RunChatTriggerSurface(BattleDriver driver,
                                      System.Func<bool, string, bool> CheckTrue,
                                      System.Func<int, int, string, bool> CheckInt)
    {
        Debug.Log(P + "--- C · 聊天触发面：`attack`/`death` 只放音、`deploy` 才弹气泡 ---");

        bool recSave = BattleDriver.RecordReplays;
        bool mulSave = driver.mulliganEnabled;
        try
        {
            BattleDriver.RecordReplays = false;      // 这一段只碰表现层，不必留录像
            driver.mulliganEnabled = false;
            driver.Begin("Ultramarines", "Goff", 20261027);
            if (!CheckTrue(driver.Ctx != null && driver.UnitChat != null,
                           "（前提）C：新开一局、`UnitChat` 在位"))
            { Debug.Log(P + "   （前提不成立 ⇒ C 余下跳过）"); return; }

            // 棋盘上找一张真单位当语音探针（督军格也行 —— 我们注入的是精确绑定，不看数据有没有 attackAudio）
            CardDef probe = null; int probeSeat = -1, probeSlot = -1;
            for (int seat = 0; seat < 2 && probe == null; seat++)
            {
                var board = driver.Ctx.Players[seat].Board;
                for (int s = 0; s < board.Length; s++)
                {
                    if (board[s] == null || board[s].Card == null) continue;
                    probe = board[s].Card; probeSeat = seat; probeSlot = s; break;
                }
            }
            if (!CheckTrue(probe != null, "（前提）C：棋盘上有一张真单位（拿它当语音探针）"))
            { Debug.Log(P + "   （前提不成立 ⇒ C 余下跳过）"); return; }

            // 找一条**装得进 Resources 且有台词**的出场语音，当下面三档的精确绑定
            string clipWithText = FindClip(probe.Id, wantText: true);
            if (clipWithText == null)
            {
                var pool = driver.Ctx.CardPool;
                for (int i = 0; i < pool.Count && i < 400 && clipWithText == null; i++)
                    if (pool[i] != null) clipWithText = FindClip(pool[i].Id, wantText: true);
            }
            if (!CheckTrue(!string.IsNullOrEmpty(clipWithText),
                           "（前提）C：找得到一条**能加载、且有台词**的出场语音（拿来当 attack/death 的精确绑定）"))
            { Debug.Log(P + "   （前提不成立 ⇒ C 余下跳过）"); return; }

            // ---- C1 · attack ⇒ 只放音、不弹气泡 ----
            using (var sc = VoiceLines.SetExactForTest(probe.Id, "attack", clipWithText))
            {
                CheckTrue(sc.WasInjected, "（前提）C1：精确表注入口可用");
                driver.UnitChat.HideAll();
                int before = driver.UnitVoiceAudioOnlyCountForTest;
                driver.SpeakForForTest(new BattleEvent
                {
                    Kind = EvtKind.Attack, Player = probeSeat, Slot = probeSlot, CardId = probe.Id
                });
                CheckInt(driver.UnitVoiceAudioOnlyCountForTest, before + 1,
                         "★★ C1：`attack` 走**纯音频**那一路（原版 `BattleManager.PlaySoundClip`：只 `PlayOneShot`）"
                       + " —— 🧨 改回 `_unitChat.Speak`（改前那版）⇒ 计数不涨 ⇒ 红");
                CheckInt(driver.UnitChat.ShownSide, -1,
                         "★★ C1：……而且**不弹气泡**（原版攻击那一档整支没有 `ShowChatBox`）"
                       + "｜🧨 改回 `_unitChat.Speak` ⇒ 弹一条「卡名」气泡 ⇒ 红"
                       + "（判据 = `CardScript._AttackMeleeAnim_d__357__MoveNext.c:76-83`）");
            }

            // ---- C2 · death ⇒ 同一条路（单位已经离场，按 `CardId` 找卡）----
            using (var sc = VoiceLines.SetExactForTest(probe.Id, "death", clipWithText))
            {
                driver.UnitChat.HideAll();
                int before = driver.UnitVoiceAudioOnlyCountForTest;
                driver.SpeakForForTest(new BattleEvent
                {
                    // ⚠️ 阵亡那一档原版是「**人已经离场**」⇒ 不走棋盘那一格，走 `CardId` 那条查找。
                    //   🔴 但 `SpeakFor` 那里用的是 `CardDatabase.Find(pool, name, faction)` ——
                    //   它比的是 **`CardDef.Name`**（⛔ 不是 `Id`；`Id` 形如 `ASH32`、按 id 找要用 `FindById`）。
                    Kind = EvtKind.Death, Player = probeSeat, Slot = -1, CardId = probe.Name
                });
                CheckInt(driver.UnitVoiceAudioOnlyCountForTest, before + 1,
                         "★★ C2：`death` 同样走纯音频（判据 = `CardScript._UnitDeath_d__446__MoveNext.c:82-97`）");
                CheckInt(driver.UnitChat.ShownSide, -1, "★★ C2：……而且不弹气泡");
            }

            // ---- C3 · deploy 仍然弹气泡（回归闸：别把出场气泡一起关掉）----
            using (var sc = VoiceLines.SetExactForTest(probe.Id, "deploy", clipWithText))
            {
                driver.UnitChat.HideAll();
                int audioBefore = driver.UnitVoiceAudioOnlyCountForTest;
                int skipBefore = driver.SummonChatMissingSkipsForTest;
                driver.SpeakForForTest(new BattleEvent
                {
                    Kind = EvtKind.Deploy, Player = probeSeat, Slot = probeSlot, CardId = probe.Id
                });
                CheckInt(driver.UnitVoiceAudioOnlyCountForTest, audioBefore,
                         "★ C3：`deploy` **不**走纯音频那一路（它要的是气泡 + 声音）");
                CheckInt(driver.SummonChatMissingSkipsForTest, skipBefore,
                         "★ C3：……也没被「没有台词」那道闸挡下（我们注入的那条有台词）");
                CheckInt(driver.UnitChat.ShownSide, probeSeat == driver.MyIndex ? 0 : 1,
                         "★ C3：**出场气泡真的弹出来了**（`0` = 我方那颗 / `1` = 敌方那颗）"
                       + "｜🧨 把 `SpeakFor` 里 `e.Kind != Deploy` 那道闸写成无条件 `return` ⇒ 红");
            }
            driver.UnitChat.HideAll();

            // ---- C4 · 「有出场语音、但没有台词 ⇒ 不弹气泡」（原版 `DisplaySummonMessage.c:36-75`）----
            {
                CardDef noText = FindCardWithVoicelessSummon(driver, probe, out int voicelessCount);
                if (noText == null)
                {
                    // 🔴 **2026-10-24 同步点订正（铁律 5）**：原来这条「找不到样本」只是打一行日志就过
                    //   （= 静默），而它刚才红过一次 —— 红因是**找样本用的谓词与闸门不是同一条**
                    //   （`ChatTermKey`+`Loc.HasEntry` vs 闸门的 `UnitChatPanel.OfficialChatText`）。
                    //   现在两边**同一条判据**（见 `FindCardWithVoicelessSummon` 的注释）。
                    //   ⇒ 找不到样本就是**真的零可达**：那就把「这一档今天 0 张卡」钉成一条数据事实，
                    //   将来数据变了（某张卡变成「有语音没台词」）⇒ 这里**会红**并提醒重新启用 C4。
                    CheckInt(voicelessCount, 0,
                             "★ C4（数据事实）：现读**全池 0 张卡**处于「有出场语音、但官方词条与 "
                           + "`voice_lines` 两处都空」这一档 ⇒ 该闸今天**不改变任何可见行为**"
                           + "（`DisplaySummonMessage.c:36-75` 那一支零可达）"
                           + "｜🧨 数据里出现这种卡 ⇒ 这条红 ⇒ 把下面的端到端两断恢复");
                    Debug.Log(P + "   （C4 端到端两断**没跑**：零可达 —— 已在上面钉成数据事实，⛔ 不是静默通过）");
                }
                else
                {
                    int skipBefore = driver.SummonChatMissingSkipsForTest;
                    driver.UnitChat.HideAll();
                    driver.SpeakForForTest(new BattleEvent
                    {
                        // ⚠️ `CardId` 传的是 **`Name`**（`SpeakFor` 的兜底查找走 `CardDatabase.Find(pool, name, faction)`）
                        Kind = EvtKind.Deploy, Player = probeSeat, Slot = -1, CardId = noText.Name
                    });
                    CheckInt(driver.SummonChatMissingSkipsForTest, skipBefore + 1,
                             $"★★ C4：`{noText.Name}` 有出场语音、但**没有台词** ⇒ 原版那一支只 `LogError(<卡名>)`、"
                           + "**不 `ShowChatBox`**（我们以前会印卡名当台词）⇒ 这次被那道闸拦下了");
                    CheckInt(driver.UnitChat.ShownSide, -1,
                             "★★ C4：……而且**没有**弹出「卡名台词」那个假气泡");
                }
            }
        }
        finally
        {
            BattleDriver.RecordReplays = recSave;
            driver.mulliganEnabled = mulSave;
        }
    }

    /// <summary>找一条**装得进 `Resources`** 的出场语音（可选：要求它带台词）。
    /// ⚠️ `VoiceLines.Clip` 会 `Resources.Load` —— 只有 `text` 条件先过了才去加载，别把整池都读一遍。</summary>
    static string FindClip(string cardId, bool wantText)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        string file, text;
        if (!VoiceLines.TryPickExact(cardId, VoiceLines.ExactDeploy, out file, out text)) return null;
        if (wantText && string.IsNullOrEmpty(text)) return null;
        if (!wantText && !string.IsNullOrEmpty(text)) return null;
        return VoiceLines.Clip(file) != null ? file : null;
    }

    /// <summary>找一张「有出场语音、但台词为空」的卡：**官方词条与 `voice_lines` 的 text 都没有**。
    /// 判据里那两个条件必须**同时**成立才是原版 `IsNullOrEmpty` 那一支（只有一个空 ⇒ 还有另一处台词可印）。
    /// <para>🔴 **2026-10-24 同步点订正（铁律 5）**：① 判「有没有官方台词」改成**闸门自己那条**
    /// （`UnitChatPanel.OfficialChatText(c.Id, "Deploy", …)`）—— 原来用 `ChatTermKey`+`Loc.HasEntry`，
    /// 与闸门不是同一条判据 ⇒ 挑出来的样本到闸门那边其实有台词 ⇒ C4 因为**错的原因**红。
    /// ② 新增 <paramref name="population"/>：**全池**（不按阵营筛、不要求音频在本地）有多少张处于这一档 ——
    /// 样本为零时用它把「今天零可达」钉成一条数据事实。</para></summary>
    static CardDef FindCardWithVoicelessSummon(BattleDriver driver, CardDef like, out int population)
    {
        population = 0;
        var pool = driver.Ctx != null ? driver.Ctx.CardPool : null;
        if (pool == null || like == null) return null;

        CardDef sample = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || string.IsNullOrEmpty(c.Id)) continue;
            // ① 官方词条：**直接用闸门那条判据**。
            string summonTerm;
            if (!string.IsNullOrEmpty(UnitChatPanel.OfficialChatText(c.Id, "Deploy", null, out summonTerm)))
                continue;                                                       // 有官方台词 ⇒ 不是样本
            // ② `voice_lines` 的 text。
            string file, text;
            if (!VoiceLines.TryPickExact(c.Id, VoiceLines.ExactDeploy, out file, out text)) continue;
            if (!string.IsNullOrEmpty(text)) continue;                          // 有 voice_lines 台词 ⇒ 不是样本

            population++;                                                       // ← 数据事实：全池计数
            if (sample != null) continue;                                       // 样本只需一张
            // ⚠️ `SpeakFor` 的兜底查找是 `CardDatabase.Find(pool, name, faction)` —— **按阵营过滤**。
            //    所以样本必须与探针**同阵营**（否则那一跳根本找不到卡 ⇒ 这条断言会因为错的原因变绿）。
            if (!DeckRules.SameFaction(c.Faction, like.Faction)) continue;
            if (VoiceLines.Clip(file) == null) continue;                        // 音频不在本地 ⇒ 走不到气泡那一层
            sample = c;
        }
        return sample;
    }
}
