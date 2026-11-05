// B145_E13_Assert.cs — 执行代理 `B145-E13`（2026-11-05）· 施工单 `A-02` 的回归断言
//
// **断言什么**：攻击结算里 **星镖（Shuriken）在哨戒（Sentry）之前**。
//
// **判据（第一权威 = 反编译 `d:/2/tools/decomp_full/`）**：
//   · `BattleManager._ResolveAttack_d__438__MoveNext.c` 逐行：
//       `:618  get_CurrentShuriken`（攻击者）→ 星镖整段 `:619-716`
//       → `:721 get_CurrentSentry`（被打者）→ 哨戒整段 `:722-781`
//       → `:988 BroadcastUnitAttacked`（触发族）→ `:999` 主伤害。
//     ⚠️ 星镖那段起协程后 `state = 4; return 1;`（`:693-694`），恢复时 `:1393 goto LAB_1809adf5f`
//        才落到哨戒 —— **正常路径就是「先星镖、后哨戒」**。
//   · 规则书（粉丝实体版，**第二来源**）：`:205` 哨戒「对攻击者造成 X 伤害，然后照常结算攻击」、
//     `:207` 星镖「结算攻击伤害前对目标额外造成 X 伤害」—— **没有给两者先后**，
//     所以旧代码那句「规则书把攻击段列成 ①哨戒 ②星镖…」是**我们自己读出来的**，不是规则书原文。
//
// **灭自证**：
//   · 组合场景同时钉「两条日志都在」（前提，⛔ 不是 `if(前提) Check`）、「顺序对」、「伤害数对」——
//     若有人把两段（或标签）一起改回旧次序，顺序断言必红；
//   · 另配「**只有哨戒**」「**只有星镖**」两条单侧场景 ⇒ 「为了过顺序断言而删掉一整段」会在这里红。
//   · ⚠️ 本断言**只读 `ctx.Events` 里两段各自的结算日志**（`RuleCore.cs` 哨戒/星镖块里的 `ctx.Log`）——
//     那是引擎自己的执行序列记录，不是从被测顺序反算出来的期望值。
//
// 🔴 每条都带 `🧨`（什么改动能让它红）——没写 `🧨` 的断言一律当没写。
// ⚠️ 本文件是**夹具直调**（同 `RuleEngineTest_T03d.cs` 口径）：`ProbeBattle` + `Place` + `RuleCore.DeclareAttack`，
//    **不跑 Unity 批处理**，由宿主 `RuleEngineTest.Run()` 里的 `Step(TestB145E13SentryShurikenOrder)` 调起
//    （✅ 建议宿主 = `RuleEngineTest.Run`；本代理**不改** `RuleEngineTest.cs`（不在白名单），接线由调度台做）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>
    /// 攻击段次序：**星镖 → 哨戒**（施工单 `A-02`；判据 = `_ResolveAttack_d__438__MoveNext.c:618 → :721`）。
    /// 建议接线：`RuleEngineTest.Run()` 里一行 `Step(TestB145E13SentryShurikenOrder);`
    /// </summary>
    public static void TestB145E13SentryShurikenOrder()
    {
        Section("🆕 `B145-E13`：攻击段次序 = **星镖 → 哨戒**（原版 `_ResolveAttack_d__438__MoveNext.c:618` → `:721`）");

        // ==========================================================================================
        //  ① 组合场景：攻击者带 `Shuriken 3`、被打者带 `Sentry 4` —— 两段都触发，钉**次序**
        // ==========================================================================================
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            // 攻击者 2 攻 / 9 血；被打者 0 攻 / 12 血（0 攻 ⇒ 没有反击，算式只剩星镖 + 主伤害）
            var atk = Place(ctx, 0, 3, Unit("B145ShuAtk", 1, 2, 9, "Shuriken 3"), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145SentryTgt", 1, 0, 12, "Sentry 4"), exhausted: true);

            // 前提：夹具真的把两个关键词都挂上了（⛔ 别让「关键词没解析」静默把本段变成空转）
            CheckTrue(atk.Has("shuriken") && atk.KwValue("shuriken") == 3,
                      "🧪 夹具自检：攻击者带 `Shuriken 3`");
            CheckTrue(tgt.Has("sentry") && tgt.KwValue("sentry") == 4,
                      "🧪 夹具自检：被打者带 `Sentry 4`");

            int from = ctx.Events.Count;   // 只扫这一击产生的日志
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "近战打那个带 `Sentry 4` 的目标");

            // ---- 前提（硬检查，⛔ 不写成 `if (前提) Check`）------------------------------
            int iShu = B145E13FirstIndexOf(ctx, from, "Shuriken 3");
            int iSen = B145E13FirstIndexOf(ctx, from, "Sentry 4");
            CheckTrue(iShu >= 0,
                      "（前提）星镖那一段**执行了**（日志里有 `Shuriken 3：…`）｜🧨 星镖块被删/没进触发条件 ⇒ 本行红"
                    + LogTail(ctx));
            CheckTrue(iSen >= 0,
                      "（前提）哨戒那一段**执行了**（日志里有 `Sentry 4：…`）｜🧨 哨戒块被删 ⇒ 本行红"
                    + LogTail(ctx));

            // ---- ★ 被测断言：次序 ------------------------------------------------------
            CheckTrue(iShu >= 0 && iSen >= 0 && iShu < iSen,
                      "★★ `A-02`：**星镖先于哨戒**（星镖日志下标 " + iShu + " < 哨戒日志下标 " + iSen + "）"
                    + "｜判据 = `BattleManager._ResolveAttack_d__438__MoveNext.c:618 get_CurrentShuriken` "
                    + "→ `:721 get_CurrentSentry`｜🧨 把 `RuleCore.cs` 两段换回「哨戒 → 星镖」⇒ 下标反转、本行红"
                    + LogTail(ctx, 10));

            // ---- 数值：两条伤害都真的落了地 ---------------------------------------------
            Check(tgt.Health, 12 - 3 - 2,
                  "星镖 3 + 主攻击 2 都打在目标身上（12 → 9 → 7）"
                + "｜🧨 星镖被跳过 ⇒ 实得 10" + LogTail(ctx));
            Check(atk.Health, 9 - 4,
                  "哨戒 4 打在攻击者身上（9 → 5）"
                + "｜🧨 哨戒被跳过 ⇒ 实得 9" + LogTail(ctx));
        }

        // ==========================================================================================
        //  ② 单侧：**只有**被打者带哨戒 —— 哨戒必须仍然响（防「为过顺序断言删掉一段」）
        // ==========================================================================================
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("B145OnlyAtk", 1, 2, 9), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145OnlySentry", 1, 0, 12, "Sentry 4"), exhausted: true);
            CheckTrue(!atk.Has("shuriken"), "🧪 夹具自检：本攻击者**没有**星镖");

            int from = ctx.Events.Count;
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "单侧②：无星镖、只对哨戒目标出手");
            CheckTrue(B145E13FirstIndexOf(ctx, from, "Sentry 4") >= 0,
                      "★★ 单侧②：**哨戒仍然响**（与有没有星镖无关）"
                    + "｜🧨 哨戒段被删/被挪进星镖分支里 ⇒ 本行红" + LogTail(ctx));
        }

        // ==========================================================================================
        //  ③ 单侧：**只有**攻击者带星镖 —— 星镖必须仍然响（同上，另一侧）
        // ==========================================================================================
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("B145OnlyShu", 1, 2, 9, "Shuriken 3"), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145PlainTgt", 1, 0, 12), exhausted: true);
            CheckTrue(!tgt.Has("sentry"), "🧪 夹具自检：本被打者**没有**哨戒");

            int from = ctx.Events.Count;
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "单侧③：有星镖、目标无哨戒");
            CheckTrue(B145E13FirstIndexOf(ctx, from, "Shuriken 3") >= 0,
                      "★★ 单侧③：**星镖仍然响**（与目标有没有哨戒无关）"
                    + "｜🧨 星镖段被删/被挪进哨戒分支里，或两段被写成互斥 ⇒ 本行红" + LogTail(ctx));
            CheckTrue(B145E13FirstIndexOf(ctx, from, "Sentry 4") < 0,
                      "单侧③ 的**反例**：目标没有哨戒 ⇒ 日志里**不该**出现 `Sentry 4` 那一条"
                    + LogTail(ctx));
            Check(tgt.Health, 12 - 3 - 2, "星镖 3 + 主攻击 2（12 → 7）" + LogTail(ctx));
        }
    }

    /// <summary>从 `from` 开始找第一条含 `needle` 的日志，返回下标；找不到 = -1。⛔ 别整串搜（旧的同名日志会串味）。</summary>
    static int B145E13FirstIndexOf(BattleContext ctx, int from, string needle)
    {
        if (ctx == null || ctx.Events == null) return -1;
        for (int i = from; i < ctx.Events.Count; i++)
            if (ctx.Events[i] != null && ctx.Events[i].Contains(needle)) return i;
        return -1;
    }
}
