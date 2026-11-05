// B145_E25_Assert.cs — 执行代理 `B145-E25`（2026-11-05）
//   施工单 ① `B145-SHURIKEN-KILL-NO-COUNTER` ② `B145-COINFLIP-NOCALLER`
//
// ==========================================================================================
//  ① 星镖击杀 ⇒ 守方**不反击**；主伤害击杀 ⇒ **仍然反击**（两档必须分得开）
// ==========================================================================================
//  判据（第一权威 = 反编译 `d:/2/tools/decomp_full/`）：
//    `BattleManager._ResolveAttack_d__438__MoveNext.c`
//      · `:617` 结算段开头写 `*(undefined2 *)(param_1 + 0x138) = 1` —— **两个字节**：
//        `+0x138`（`defenderReturnsDamage`，守方能不能反击）= 1、`+0x139`（`defenderKilledByShuriken`）= 0；
//      · `:632` 星镖那一支判 `EnoughPendingDamageToDieWithDamageValues(守方, [CurrentShuriken], [0])` 为真时
//        改写 `*(undefined2 *)(param_1 + 0x138) = 0x100` —— **同一句写两个字节**：
//        `+0x138 = 0`（**不反击**）**且** `+0x139 = 1`（守方被星镖杀）；
//      · `:862 if (+0x13a == 0 && +0x139 == 0) ResolveAttackDamage…` —— 主伤害（以及下面那条反击链）的总闸；
//      · `:1114 if (*(char *)(param_1 + 0x138) != '\0')` 才是反击段 ⇒ **星镖杀 ⇒ 不进**。
//    ⇒ 我们原来只有一个 `targetDied`（「这一记打死了它」），它既含**星镖杀**也含**主伤害杀**，
//      而反击闸读的是它 ⇒ **星镖杀也会反击**（与原版相反）；主伤害杀免反击又会错（规则书 `:145` 那个
//      「被打死的初生者照样反击 1 点」的例子）。落地口 = `RuleCore.DeclareAttackExecute` 的
//      `defenderKilledByShuriken`（与 `targetDied` **各自独立**）。
//
//  灭自证（三条互斥的格子，任何一条被写成恒真/恒假都会红一条）：
//    A 星镖杀   ⇒ 不反击；B 星镖**没杀**（目标活着）⇒ 照常反击；C 主伤害杀 ⇒ 照常反击。
//
// ==========================================================================================
//  ② 开局掷硬币：`DrawFirstSeatRandomInt()` 必须**真在开局链上**，且结果可复现
// ==========================================================================================
//  判据：`SearchOpponentManager__StartBattle.c:20-21` —— 开局**无条件**
//    `uVar5 = MatchData.GetRandomInt(lVar2, 0, 2); *(undefined4 *)(lVar2 + 0xe8) = uVar5;`
//    （吃预烧表第 0 格，写 `MatchData.playerGoesFirstRandomInt`；平局支见 `SupportMethods.cs:3216-3228`）。
//  现状（2026-11-05 `B145-E25` 现读）：`B143-R3` 已把 `RuleCore.NewBattle(drawFirstSeat:)` 接到
//    `BattleDriver.Begin`（唯一生产入口，`drawFirstSeat: true`）⇒ 本组断的是**这条链接线与可复现性**，
//    ⛔ 不是「已经有这条 API 就算数」（`B145-R1` 那份对账就是拿「有 API」判了「没接」）。
//
//  🔴 建议接线（本代理**不改** `RuleEngineTest.cs` —— 不在白名单，接线由调度台做）：
//    `RuleEngineTest.Run()` 里两行：`Step(TestB145E25ShurikenKillNoCounter);` · `Step(TestB145E25CoinFlipOpening);`
//    （`Step` 会登记 `_ranCases`，覆盖闸才认；⛔ 别只加一行裸调用。）
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==========================================================================================
    //  ① 星镖杀 / 主伤害杀 / 星镖没杀 —— 三格
    // ==========================================================================================
    public static void TestB145E25ShurikenKillNoCounter()
    {
        Section("🆕 `B145-E25`：**星镖击杀 ⇒ 不反击**；主伤害杀 / 星镖没杀 ⇒ **照常反击**（原版 `+0x138`/`+0x139` 两个字节）");

        // ---- A 星镖杀 ⇒ 不反击 --------------------------------------------------------------
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("B145E25ShuA", 1, 3, 6, "Shuriken 5"), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145E25PreyA", 1, 4, 3), exhausted: true);

            // 前提（硬检查，⛔ 不写成 `if (前提) Check(结论)`）
            CheckTrue(atk.Has("shuriken") && atk.KwValue("shuriken") == 5,
                      "🧪 A-前提 攻击者带 `Shuriken 5`｜🧨 关键词没解析 ⇒ 下面三条都会假绿");
            Check(tgt.Health, 3, "🧪 A-前提 目标 3 血（星镖 5 必杀）");
            Check(atk.Health, 6, "🧪 A-前提 攻击者 6 血（吃下 4 点反击也死不了 ⇒ 血量差就是判别式）");

            int from = ctx.Events.Count;
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "星镖 5 打 3 血目标（守方 4 攻：若反击发生 ⇒ 攻击者 6 → 2）");

            CheckTrue(!tgt.IsAlive, "（前提）**星镖把目标打死了**" + LogTail(ctx));
            CheckTrue(B145E25HasLog(ctx, from, "不承受反击"),
                      "（前提）星镖击杀那一支出声了（原版 `:632` 写 `+0x138 = 0`）"
                    + "｜🧨 把 `defenderKilledByShuriken` 恒置假 ⇒ 本行红" + LogTail(ctx));
            Check(atk.Health, 6,
                  "★★ A **星镖击杀 ⇒ 守方不反击**：攻击者仍是 6 血"
                + "｜判据 = `_ResolveAttack_d__438__MoveNext.c:632`（`+0x138`/`+0x139` 同一次写）+ `:1114`"
                + "｜🧨 改前（只置 `targetDied`、反击闸读它以外的东西）⇒ 实得 2" + LogTail(ctx));
        }

        // ---- B 星镖**没杀** ⇒ 照常反击（⛔ 别把闸写成「带星镖就免反击」）-------------------------
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("B145E25ShuB", 1, 2, 6, "Shuriken 2"), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145E25PreyB", 1, 3, 20), exhausted: true);

            Check(atk.Health, 6, "🧪 B-前提 攻击者 6 血");
            Check(tgt.Health, 20, "🧪 B-前提 目标 20 血（星镖 2 + 主伤害 2 之后仍活着）");

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "星镖 2 + 2 攻打 20 血目标（打不死）");

            CheckTrue(tgt.IsAlive, "（前提）目标活着（20 − 2 星镖 − 2 主伤害 = 16）" + LogTail(ctx));
            Check(tgt.Health, 16, "（前提）实际掉 4 点（星镖 2 + 主伤害 2）" + LogTail(ctx));
            Check(atk.Health, 6 - 3,
                  "★★ B 反例：**星镖没打死目标 ⇒ 照常反击**（攻击者 6 − 3 = 3）"
                + "｜🧨 把闸写成「带星镖就免反击」/ 恒真 ⇒ 本行实得 6" + LogTail(ctx));
        }

        // ---- C 主伤害杀 ⇒ **仍然反击**（规则书 `:145` 那个初生者例子）-----------------------------
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("B145E25BigC", 1, 5, 6), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145E25PreyC", 1, 4, 4), exhausted: true);

            Check(tgt.Health, 4, "🧪 C-前提 目标 4 血（主伤害 5 必杀，**没有**星镖）");
            CheckTrue(!atk.Has("shuriken"), "🧪 C-前提 攻击者**没有**星镖");

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "5 攻打 4 血目标（主伤害杀）");

            CheckTrue(!tgt.IsAlive, "（前提）目标被**主伤害**打死" + LogTail(ctx));
            Check(atk.Health, 6 - 4,
                  "★★ C 反例：**主伤害击杀 ⇒ 仍然反击**（攻击者 6 − 4 = 2）"
                + "｜判据 = 规则书 `:145` 例子 + 原版只在**星镖杀**那一支把 `+0x138` 清 0"
                + "｜🧨 把 `targetDied` 当成免反击条件 ⇒ 本行实得 6（改前就是这么错的）" + LogTail(ctx));
        }

        // ---- D 结构探针：两个标志必须**分别**存在、且落在正确的闸上 ------------------------------
        //   ⚠️ 行为断言只看得到「今天这几格」；这一条挡的是**将来把两个标志并回一个**（静默回到改前）。
        {
            string src = System.IO.File.ReadAllText("Assets/RuleEngine/Core/RuleCore.cs");
            CheckTrue(src.Contains("defenderKilledByShuriken = true") && src.Contains("targetDied = true; defenderKilledByShuriken = true;"),
                      "★★ D1 星镖分支必须**同时**置两个标志（原版 `:632` 是一次 `u16` 写两个字节）"
                    + "｜🧨 只置 `targetDied` ⇒ 本行红，且 A/C 两条互相矛盾（不可能同时绿）");
            CheckTrue(src.Contains("!attackerKilledBySentry && !defenderKilledByShuriken && !noCounter"),
                      "★★ D2 反击闸必须读 `defenderKilledByShuriken`（原版 `:1114` 读 `+0x138`）"
                    + "｜🧨 把这个条件删掉 ⇒ A 红、C 仍绿（= 退回改前那条偏差）");
            CheckTrue(src.Contains("if (!defenderKilledByShuriken && !attackerKilledBySentry)"),
                      "★ D3 主伤害闸必须读 `defenderKilledByShuriken`（原版 `:862` 读 `+0x139`）"
                    + "｜🧨 改回 `!targetDied` ⇒ 本行红（今天结果等价，但语义错、主伤害杀也会被当成星镖杀）");
        }
    }

    // ==========================================================================================
    //  ② 开局掷硬币：接在开局链上 · 同种子可复现 · 吃预烧表第 0 格 · 教程只覆盖值不省这一掷
    // ==========================================================================================
    public static void TestB145E25CoinFlipOpening()
    {
        Section("🆕 `B145-E25`：**开局掷硬币**（`NewBattle(drawFirstSeat:)` 接线 · 同种子可复现 · 教程只覆盖值）");

        // ---- A 同种子两次开局 ⇒ 先手一致；且座位 = 预烧表第 0 格的奇偶 --------------------------
        //   找两颗「先后手不同」的种子：证明结果**真按种子变**（⛔ 不是写死成 0）。
        int seed0 = -1, seed1 = -1, seat0 = -1, seat1 = -1;
        for (int s = 1; s <= 64 && (seed0 < 0 || seed1 < 0); s++)
        {
            var c = RuleCore.NewBattle(B145E25HeroDeck("A"), B145E25HeroDeck("B"), seed: s, shuffle: false,
                                       drawFirstSeat: true, firstSeat: -1);
            if (c.FirstSeat == 0 && seed0 < 0) { seed0 = s; seat0 = c.FirstSeat; }
            if (c.FirstSeat == 1 && seed1 < 0) { seed1 = s; seat1 = c.FirstSeat; }
        }
        CheckTrue(seed0 > 0 && seed1 > 0,
                  $"🧪 A-前提 种子 1..64 里找到了「座位 0」（seed={seed0}）与「座位 1」（seed={seed1}）两档"
                + "｜🧨 只有一档 ⇒ 硬币没接上（恒 0 / 恒 1），下面的判别式也就没有意义");

        //   ⚠️ **⛔ 不写成 `if (前提) Check(...)`**：前提红时下面的闸**也照跑**（`useSeed` 只是把非法值换成 1，
        //      A1 会随前提一起红，而不是静默跳过）。见 `CLAUDE.md` 那条「前提不成立时静默跳过」。
        int useSeed = seed0 > 0 ? seed0 : 1;
        {
            var a1 = RuleCore.NewBattle(B145E25HeroDeck("A"), B145E25HeroDeck("B"), seed: useSeed, shuffle: false,
                                        drawFirstSeat: true, firstSeat: -1);
            var a2 = RuleCore.NewBattle(B145E25HeroDeck("A"), B145E25HeroDeck("B"), seed: useSeed, shuffle: false,
                                        drawFirstSeat: true, firstSeat: -1);
            Check(a1.FirstSeat, seat0,
                  $"★★ A1 种子 {useSeed} ⇒ 先手 = {seat0}（原版 `SearchOpponentManager__StartBattle.c:20-21`）"
                + "｜🧨 把 `drawFirstSeat: true` 从 `BattleDriver.Begin` 删掉 ⇒ 本行走的是 `firstSeat: -1` 且没掷 ⇒ 红");
            Check(a2.FirstSeat, a1.FirstSeat,
                  "★★ A2 **同一颗种子两次开局 ⇒ 先手一致**（对局可复现红线）"
                + "｜🧨 把硬币改成 `new Random()` / 时间派生 ⇒ 本行红");
            Check(a1.FirstSeatRandomInt, a2.FirstSeatRandomInt,
                  "★★ A3 两次掷出来的值也一致（存进 `FirstSeatRandomInt` 的那一格）");
            Check(a1.RandomNumbersIndex, 1,
                  "★★ A4 开局**恰好吃 1 格**（第 0 格；之后才建牌库/洗牌）"
                + "｜🧨 漏掉这一掷 ⇒ 实得 0；掷两次 ⇒ 实得 2");
            Check(a1.FirstSeat, a1.RandomNumbers[0] % 2,
                  "★★ A5 座位 = **预烧表第 0 格** 的奇偶（`==0 ⇒ 座位 0`）"
                + "｜🧨 换个随机源（`ctx.Rng.Next`）⇒ 表外取数、回放/联机对不上");
        }

        {
            CheckTrue(seat0 != seat1,
                      "★★ A6 **判别式**：两颗种子掷出**不同**的先手（⛔ 别把座位写死）"
                    + "｜🧨 硬币恒返 0 ⇒ 本行红");
        }

        // ---- B 教程局：**硬币照掷**（吃格子），值被关卡覆盖（原版同形）----------------------------
        {
            var tOn = new TutorialScript(new TutorialStageData
            { stage = 95, so = "<B145-E25 合成·playerStarts>", playerStarts = true });
            var tCtx = RuleCore.NewBattle(B145E25HeroDeck("A"), B145E25HeroDeck("B"), seed: 7, shuffle: false,
                                          tutorial: tOn, drawFirstSeat: true, firstSeat: -1);
            var tNo = RuleCore.NewBattle(B145E25HeroDeck("A"), B145E25HeroDeck("B"), seed: 7, shuffle: false,
                                         tutorial: tOn, drawFirstSeat: false, firstSeat: -1);
            Check(tCtx.FirstSeat, 0,
                  "★ B1 教程局先手照关卡（`playerStarts = true` ⇒ 座位 0）"
                + "｜🧨 让硬币覆盖关卡值 ⇒ 本行随种子红");
            Check(tCtx.RandomNumbersIndex, tNo.RandomNumbersIndex + 1,
                  "★★ B2 教程局**硬币照掷**（比不掷那一趟多吃恰好 1 格）"
                + "｜判据 = 原版 `StartBattle` **无条件**掷、之后才分派 ⇒ 教程只是**覆盖值**，不省这一掷"
                + "｜🧨 在 `tutorial != null` 时跳过 `DrawFirstSeatRandomInt()` ⇒ 本行红");
        }

        // ---- C 结构探针：生产入口**真的**传了 `drawFirstSeat: true` -----------------------------
        //   ⚠️ `B145-R1` 那份对账就是拿「`DrawFirstSeatRandomInt` 全仓只命中 doc + 断言」判了「0 生产调用点」——
        //     真相是它经 `RuleCore.NewBattle` 的 `drawFirstSeat` 分支被调，`grep` 字面量看不见这一跳。
        //     这一条把**生产链上的那一跳**钉住（⛔ 别再按字面量搜断言存在性）。
        {
            string core = System.IO.File.ReadAllText("Assets/RuleEngine/Core/RuleCore.cs");
            string drv = System.IO.File.ReadAllText("Assets/CardPresentation/Battle/BattleDriver.cs");
            CheckTrue(core.Contains("if (drawFirstSeat) ctx.DrawFirstSeatRandomInt();"),
                      "★★ C1 `RuleCore.NewBattle` 必须**真的调** `DrawFirstSeatRandomInt()`（不是只留 API）"
                    + "｜🧨 删掉这一跳 ⇒ A1/A4 红，本行也红");
            int newBattleCalls = B145E25Count(drv, "Ctx = RuleCore.NewBattle(");
            Check(newBattleCalls, 1,
                  "★★ C2 `BattleDriver` 里**只有一处** `Ctx = RuleCore.NewBattle(`（开局唯一入口；⚠️ 注释里提到 `RuleCore.NewBattle(` 不计）"
                + "｜🧨 多出第二处且没传 `drawFirstSeat` ⇒ 那条路不掷硬币（两条开局链口径会分叉）");
            CheckTrue(drv.Contains("drawFirstSeat: true"),
                      "★★ C3 那个唯一入口必须**显式**传 `drawFirstSeat: true`"
                    + "｜🧨 删掉这个实参 ⇒ 缺省 `false` ⇒ 真 Play 不掷硬币，只有 A1–A5 这类直调还绿"
                    + "（这正是本笔要防的：字面量断言全绿、真机不掷）");
        }
    }

    // ==========================================================================================
    //  小工具（本文件私有；⛔ 别在别处再造一份）
    // ==========================================================================================
    /// <summary>开局掷硬币用的最小卡组：一方一张督军就够（同 `B143_R1_Assert.CoinFlipDeck`）。</summary>
    static List<CardDef> B145E25HeroDeck(string tag)
    {
        return new List<CardDef>
        { new CardDef(tag, tag, "hero", "", null, "Test", 0, 0, 30, 0, null, subtype: "Warlord") };
    }

    /// <summary>从 `from` 起的日志里找一段子串（返回是否命中）。⚠️ 空/越界一律 false（菜单外不抛）。</summary>
    static bool B145E25HasLog(BattleContext ctx, int from, string needle)
    {
        if (ctx == null || ctx.Events == null || string.IsNullOrEmpty(needle)) return false;
        for (int i = from < 0 ? 0 : from; i < ctx.Events.Count; i++)
            if (ctx.Events[i] != null && ctx.Events[i].Contains(needle)) return true;
        return false;
    }

    /// <summary>数 `src` 里出现 `needle` 的次数（非重叠）。</summary>
    static int B145E25Count(string src, string needle)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = src.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }
}
