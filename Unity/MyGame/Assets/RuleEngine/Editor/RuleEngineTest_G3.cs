// RuleEngineTest_G3.cs — 引擎线 `G3`：`AiTargets.Trim` 的 `targetSelectMethod` 逐值口径（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineB6` / `_EngineF8` / `_G1b` / `_G2` … 都在），而**同一时刻只有一个写手改一个文件**
// ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一段 `Section(...)` +
// 一行 `Step(TestG3AiTargetsTrim)`**。
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1024/C5_剩余判据普查.md` §2：
//   第一权威 = `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:1164-1366`（尾部 switch）。
//   `targetSelectMethod`（`+0x44`）逐值：0/4/其它 = **随机取 N**（`default:`，`:1250-1273`）·
//   1 = `OrderBy(isRemnant).ThenByDescending(max(近战,远程))`（`:1172-1207`）·
//   2 = `OrderByDescending(ValueInPlay)` · 3 = `OrderBy(ValueInPlay)` ·
//   5 = 保尾（`:1274-1310`）· 6 = `OrderBy(CurrentIntrinsicHealth)`（`:1311-1331`）· 7 = 保头。
//   并列 = LINQ 稳定排序 ⇒ 源表序 = `unitsInPlay(+0x470)` 追加序 = 我们 `UnitState.PlayOrder`。
// 🔴 **随机流 = `ctx.AiRng`（有意偏离原版）**：原版 `GetRandomInt` 是单流，而本仓既有决定是
//    「AI 这一侧的随机不挪动共享 `ctx.Rng`」（`BattleContext.cs:371-377`，回放不重掷）
//    ⇒ 本件断言按 `AiRng` 写，并**直接钉住**「烧 `Rng` 不影响、烧 `AiRng` 才影响」（见 ⑧）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死**字面量**（卡名串 / 计数），⛔ 不从被测实现反算（那会自证）。
// 🔴 随机那两条**不写死抽中了谁**（那等于把实现的 RNG 序列抄进来）—— 改断**结构性质**：
//    「保前 N / 目标排最前」这两种旧写法在 30 次抽样里**必然**露馅，而随机**几乎必然**不露
//    （判据见各条注释里的概率）。这样断的是**规则**，不是某一次抽签结果。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestG3AiTargetsTrim)`）。
    /// 子用例用 `G3…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestG3AiTargetsTrim()
    {
        Section("🆕 引擎线 G3：`AiTargets.Trim` 的 `targetSelectMethod` 逐值口径（`C5` §2）");
        G3Case1RemnantAndHighestAttack();
        G3Case2And3ValueInPlay();
        G3Case0Random();
        G3Case4RandomNotTargetFirst();
        G3Case5And7KeepTailHead();
        G3Case6IntrinsicHealth();
        G3TieBreakByPlayOrder();
        G3RandomReproducible();
    }

    // ==================================================================
    //  夹具小工具
    // ==================================================================

    /// <summary>
    /// 本批统一的 criteria：`targetsAffected = 10 (allFiltered)` → 候选池 = 双方棋盘，
    /// 再用 `friendlyUnits` 收成自己一侧；`includeHero = false` 把督军（格 4）滤掉。
    /// ⚠️ `includeRemnants = false` 是原版默认 ⇒ **残骸会在 `ApplyFilters` 就被滤掉**，
    /// 所以验 `isRemnant` 主键那一条必须显式打开（见 `G3Case1RemnantAndHighestAttack`）。
    /// </summary>
    static AiTargetCriteriaDto G3Crit(int method, int maxTargets, bool includeRemnants = false)
    {
        return new AiTargetCriteriaDto
        {
            targetsAffected = 10,
            friendlyUnits = true,
            enemyUnits = false,
            includeHero = false,
            includeMinions = true,
            includeRemnants = includeRemnants,
            maxTargets = maxTargets,
            targetSelectMethod = method,
        };
    }

    /// <summary>结果表按序拼成名串（`A,B,C`）—— 断言直接跟字面量比。</summary>
    static string G3Joined(List<AiTargetRef> list)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var t = list[i];
            sb.Append(t.Unit != null ? t.Unit.Name : (t.Card != null ? t.Card.Name : "(null)"));
        }
        return sb.ToString();
    }

    /// <summary>拿第 `i` 个的名字（越界 ⇒ `"(none)"`，别让断言自己抛）。</summary>
    static string G3Name(List<AiTargetRef> list, int i)
    {
        if (i < 0 || i >= list.Count) return "(none)";
        var t = list[i];
        return t.Unit != null ? t.Unit.Name : (t.Card != null ? t.Card.Name : "(null)");
    }

    // ==================================================================
    //  ① `targetSelectMethod == 1`（highestAttack）：isRemnant 主键 + max(近战,远程)
    // ==================================================================

    /// <summary>
    /// 原版 `:1172-1207` 的排序键 = **先** `OrderBy(isRemnant)`（升序：非残骸在前），
    /// **再** `ThenByDescending(GetHighestAttackValue())`，而
    /// `GetHighestAttackValue = max(CurrentRangeAttack, CurrentMeleeAttack)`
    /// （`CardScript__GetHighestAttackValue.c:6-14`）。
    ///
    /// 夹具（三个键**故意互相打架**，单改哪一条都会被抓到）：
    ///   · 格 1 `G3C1Ranged` —— 近战 1 / **远程 5** / 非残骸 ⇒ 新键 5；
    ///   · 格 2 `G3C1Melee`  —— **近战 4** / 远程 0 / 非残骸 ⇒ 新键 4；
    ///   · 格 3 `G3C1Remnant` —— **近战 9**（最高）/ 残骸 ⇒ 主键把它压到最后。
    /// 期望（`maxTargets = 2`）= `G3C1Ranged,G3C1Melee`。
    /// </summary>
    static void G3Case1RemnantAndHighestAttack()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;

        Place(ctx, me, 1, Ranged("G3C1Ranged", 2, 1, 3, 5));       // 近 1 / 远 5
        Place(ctx, me, 2, Unit("G3C1Melee", 2, 4, 3));             // 近 4 / 远 0
        var rem = Place(ctx, me, 3, Unit("G3C1Remnant", 2, 9, 1)); // 近 9，残骸
        rem.IsRemnant = true;

        // （前提）残骸真的挂上了、且它的近战确实最高 —— 否则「被主键压到最后」这件事不可观测
        Check(rem.IsRemnant, true,
              "★ （前提）`G3C1Remnant` 的 `IsRemnant` 确实是 true"
            + "｜🧨 字段被改名 / `Place` 之后又被覆盖 ⇒ 红（说明主键夹具没建立）");
        Check(rem.Attack > 5, true,
              "★ （前提）残骸的近战（9）确实高于另外两张的排序键（5 / 4）"
            + "｜🧨 夹具数值被改小 ⇒ 红（那时「残骸被压后」与「按近战降序」不再可区分）");

        var c = G3Crit(1, 2, includeRemnants: true);
        var got = AiTargets.GetTargets(ctx, c, Unit("G3C1Self", 1, 1, 3), null, me, null);

        Check(got.Count, 2,
              "① `method=1` 截到 `maxTargets=2` 个"
            + "｜🧨 截断那一步丢了 ⇒ 红");
        Check(G3Joined(got), "G3C1Ranged,G3C1Melee",
              "①★ `method=1` = **非残骸在前**、且键 = **max(近战,远程) 降序**（远程 5 > 近战 4）"
            + "｜🧨 去掉 `isRemnant` 主键 ⇒ 残骸（近战 9）排第一 ⇒ 实得 `G3C1Remnant,G3C1Melee` ⇒ 红；"
            + "🧨 键退回只比近战 ⇒ 顺序变成 `G3C1Melee,G3C1Ranged` ⇒ 红");
    }

    // ==================================================================
    //  ② `method == 2 / 3`：ValueInPlay 降序 / 升序
    // ==================================================================

    /// <summary>
    /// 原版 `:1208-1249`：2 = `OrderByDescending(GetValueInPlay)`、3 = `OrderBy(GetValueInPlay)`。
    /// 我们的值函数 `SimpleAI.ValueInPlay`（`= 2×费用 + 1 + 附魔价值`，满血时不再衰减）
    /// ⇒ 用**费用**拉开差距（5 费 = 11 · 1 费 = 3），格号故意与价值**反向**。
    /// </summary>
    static void G3Case2And3ValueInPlay()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;

        Place(ctx, me, 6, Unit("G3C2Cheap", 1, 1, 4));     // 价值 3
        Place(ctx, me, 1, Unit("G3C2Pricey", 5, 1, 4));    // 价值 11

        var self = Unit("G3C2Self", 1, 1, 3);

        Check(G3Joined(AiTargets.GetTargets(ctx, G3Crit(2, 1), self, null, me, null)), "G3C2Pricey",
              "②★ `method=2 (highestValueInPlay)` 取**价值最高**的那张（5 费 = 11 > 1 费 = 3）"
            + "｜🧨 降序写成升序 ⇒ 实得 `G3C2Cheap` ⇒ 红");

        Check(G3Joined(AiTargets.GetTargets(ctx, G3Crit(3, 1), self, null, me, null)), "G3C2Cheap",
              "②★ `method=3 (lowestValueInPlay)` 取**价值最低**的那张"
            + "｜🧨 两档写反 ⇒ 实得 `G3C2Pricey` ⇒ 红");
    }

    // ==================================================================
    //  ③ `method == 0`（以及所有未列值）= **随机取 N**（不是「保前 N」）
    // ==================================================================

    /// <summary>
    /// 原版 `switch` **没有 case 0** ⇒ 落 `default:` = 无放回随机取 `maxTargets` 个
    /// （`:1250-1273`：`GetRandomInt(0, list.Count)` 取一个删一个）。
    ///
    /// 旧实现是「`default` ⇒ 保前 N」：8 个候选里**永远**只会给出源表前两个（格 0 / 格 1）。
    /// ⇒ 抽样 30 次，只要**有一次**拿到这两个以外的名字，就说明「保前 N」不成立。
    /// 随机侧误报概率 = 「30 次全部只抽前两个」= `(1/C(8,2))^30 = (1/28)^30 ≈ 0`。
    /// </summary>
    static void G3Case0Random()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;
        int[] slots = { 0, 1, 2, 3, 5, 6, 7, 8 };       // 9 格里刨掉格 4（督军）
        for (int i = 0; i < slots.Length; i++)
            Place(ctx, me, slots[i], Unit("G3C0U" + i, 1, 1, 3));

        var c = G3Crit(0, 2);
        var self = Unit("G3C0Self", 1, 1, 3);

        bool countOk = true, distinctOk = true, sawOutsideHead = false;
        for (int i = 0; i < 30; i++)
        {
            var got = AiTargets.GetTargets(ctx, c, self, null, me, null);
            if (got.Count != 2) countOk = false;
            if (got.Count == 2 && G3Name(got, 0) == G3Name(got, 1)) distinctOk = false;
            for (int j = 0; j < got.Count; j++)
            {
                string n = G3Name(got, j);
                if (n != "G3C0U0" && n != "G3C0U1") sawOutsideHead = true;
            }
        }

        Check(countOk, true,
              "③ 30 次抽样里每次都是 2 个（= `maxTargets`）"
            + "｜🧨 随机那一支把表清空后没补回来 / 多取少取 ⇒ 红");
        Check(distinctOk, true,
              "③ 同一次结果里**不重复**（原版「取一个删一个」= 无放回）"
            + "｜🧨 忘了 `RemoveAt` ⇒ 可能抽到同一张两次 ⇒ 红");
        Check(sawOutsideHead, true,
              "③★ `method=0` 是**随机取 N**，不是「保前 N」—— 30 次里出现过源表前两个以外的候选"
            + "｜🧨 退回旧写法「`default:` 保前 N」⇒ 永远只有 `G3C0U0/G3C0U1` ⇒ 红");
    }

    // ==================================================================
    //  ④ `method == 4`：原版**没有 case 4** ⇒ 同样落 `default:` = 随机（不是「目标排最前」）
    // ==================================================================

    /// <summary>
    /// `AbilityLogic__GetTargets.c` 的 `switch` 只有 1/2/3/5/6/7（外加 `default:`）——
    /// **没有 case 4**，`cardTarget` 这个枚举名在消费端根本不存在分支 ⇒ 行为 = 随机取 N。
    /// 旧实现把 `targetUnit` 排到最前（`AiTargets.cs` 旧 `:779-783`）⇒ 只要传了目标，
    /// 结果第 0 个**永远**是它。
    /// ⇒ 抽样 30 次，只要有一次第 0 个不是目标，就说明「目标排最前」不成立。
    /// 随机侧误报概率 = `(1/8)^30 ≈ 0`（每次第 1 抽恰为目标 = 1/8）。
    /// </summary>
    static void G3Case4RandomNotTargetFirst()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;
        int[] slots = { 0, 1, 2, 3, 5, 6, 7, 8 };
        UnitState target = null;
        for (int i = 0; i < slots.Length; i++)
        {
            var u = Place(ctx, me, slots[i], Unit("G3C4U" + i, 1, 1, 3));
            if (i == 4) target = u;                       // 格 5 那张当「这条动作选中的目标」
        }

        // （前提）目标确实在候选池里（否则「排最前」本来就无从观测，断言会假绿）
        Check(target != null && target.Name == "G3C4U4", true,
              "★ （前提）目标单位 `G3C4U4` 真的摆上了盘（格 5）"
            + "｜🧨 夹具没摆上 ⇒ 「目标排最前」与随机不可区分 ⇒ 红（不许假绿）");

        var c = G3Crit(4, 2);
        var self = Unit("G3C4Self", 1, 1, 3);
        bool sawFirstNotTarget = false, countOk = true;
        for (int i = 0; i < 30; i++)
        {
            var got = AiTargets.GetTargets(ctx, c, self, null, me, target);
            if (got.Count != 2) countOk = false;
            if (G3Name(got, 0) != "G3C4U4") sawFirstNotTarget = true;
        }

        Check(countOk, true,
              "④ 30 次抽样里每次都是 2 个"
            + "｜🧨 随机那一支长度不对 ⇒ 红");
        Check(sawFirstNotTarget, true,
              "④★ `method=4 (cardTarget)` 在原版里**没有分支** ⇒ 落 `default:` = 随机；"
            + "30 次里出现过「第 0 个不是目标」"
            + "｜🧨 退回旧写法「目标排最前」⇒ 第 0 个永远是 `G3C4U4` ⇒ 红");
    }

    // ==================================================================
    //  ⑤ `method == 5 / 7`：保尾 / 保头（位置型，不排序）
    // ==================================================================

    /// <summary>
    /// 原版 `:1274-1310`（case 5）建新表、从**尾部**取 `maxTargets` 个；
    /// `:1332-1360`（case 7）从头取 `maxTargets` 个。
    /// ⚠️ 本断言只钉**集合**（保尾 / 保头），不钉 case 5 的元素次序（见报告「顺手发现」）。
    /// </summary>
    static void G3Case5And7KeepTailHead()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;

        Place(ctx, me, 1, Unit("G3C5A", 1, 1, 3));
        Place(ctx, me, 2, Unit("G3C5B", 1, 1, 3));
        Place(ctx, me, 3, Unit("G3C5C", 1, 1, 3));
        Place(ctx, me, 5, Unit("G3C5D", 1, 1, 3));
        var self = Unit("G3C5Self", 1, 1, 3);

        Check(G3Joined(AiTargets.GetTargets(ctx, G3Crit(5, 2), self, null, me, null)), "G3C5C,G3C5D",
              "⑤★ `method=5 (lastAdded)` 保**尾**部两个（源表序 = 格 1,2,3,5）"
            + "｜🧨 写成保头 ⇒ 实得 `G3C5A,G3C5B` ⇒ 红");

        Check(G3Joined(AiTargets.GetTargets(ctx, G3Crit(7, 2), self, null, me, null)), "G3C5A,G3C5B",
              "⑤★ `method=7 (firstAdded)` 保**头**部两个"
            + "｜🧨 写成保尾 ⇒ 实得 `G3C5C,G3C5D` ⇒ 红");
    }

    // ==================================================================
    //  ⑥ `method == 6`：键是 `CurrentIntrinsicHealth`，不是裸 `Health`
    // ==================================================================

    /// <summary>
    /// 原版 `:1311-1331` 的键 = `EntityScript.CurrentIntrinsicHealth()`
    /// = `currentHealth(+0x68) − Σ(extrinsic(+0x30) && buffType==addStats(1) 的 healthChange(+0x60))`
    /// （`EntityScript__get_CurrentIntrinsicHealth.c:44-62`）。
    ///
    /// 我们的等价物 = `Health − Σ(PendingGrants 里 Source == Auras.GrantTag && Attr == "health")`
    /// （光环层 = 我们的 whileInPlay 一族；边界见 `AiTargets.CurrentIntrinsicHealth` 的 doc）。
    ///
    /// 夹具（三个单位**当前生命互相错开**，只改「减不减」就会翻）：
    ///   · 格 1 `G3C6Aura`  —— 底 1，光环 **+4** ⇒ 当前 5 / 本征 1；
    ///   · 格 2 `G3C6Pact`  —— 底 2，**黑暗契约式** grant `+3`（`Source` 不是光环）⇒ 当前 5 / 本征 5；
    ///   · 格 3 `G3C6Plain` —— 底 3，无账 ⇒ 当前 3 / 本征 3。
    /// 期望（`maxTargets = 2`，升序）= `G3C6Aura,G3C6Plain`。
    /// </summary>
    static void G3Case6IntrinsicHealth()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;

        var aura = Place(ctx, me, 1, Unit("G3C6Aura", 2, 1, 1));
        aura.RecordGrant("health", 4, Auras.GrantTag);          // extrinsic（whileInPlay）那一族
        var pact = Place(ctx, me, 2, Unit("G3C6Pact", 2, 1, 2));
        pact.RecordGrant("health", 3, "blood");                  // 永久关键词那一族（原版 extrinsic 为假）
        var plain = Place(ctx, me, 3, Unit("G3C6Plain", 2, 1, 3));

        // （前提）三张的**当前生命**确实是 5 / 5 / 3 —— 否则「用裸 Health」与「用本征」不可区分
        Check(aura.Health, 5,
              "★ （前提）`G3C6Aura` 当前生命 = 5（底 1 + 光环 4）"
            + "｜🧨 `RecordGrant` 没真的加上 ⇒ 红（说明本征夹具没建立）");
        Check(pact.Health, 5,
              "★ （前提）`G3C6Pact` 当前生命 = 5（底 2 + 契约 3）"
            + "｜🧨 `RecordGrant` 没真的加上 ⇒ 红");
        Check(plain.Health, 3,
              "★ （前提）`G3C6Plain` 当前生命 = 3（无账）"
            + "｜🧨 夹具数值被改 ⇒ 红");

        var got = AiTargets.GetTargets(ctx, G3Crit(6, 2), Unit("G3C6Self", 1, 1, 3), null, me, null);

        Check(G3Joined(got), "G3C6Aura,G3C6Plain",
              "⑥★ `method=6 (lowestHealth)` 的键是 **CurrentIntrinsicHealth**（减掉 extrinsic 生命增益）"
            + "｜🧨 用裸 `Health` ⇒ 3(Plain) 最前、5/5 并列 ⇒ 实得 `G3C6Plain,G3C6Aura` ⇒ 红；"
            + "🧨 把**所有** health grant 都减掉（不分来源）⇒ `G3C6Pact` 变 2 ⇒ 实得 `G3C6Aura,G3C6Pact` ⇒ 红");
    }

    // ==================================================================
    //  ⑦ 并列 = 进场序（`PlayOrder`），不是格号序
    // ==================================================================

    /// <summary>
    /// 原版 LINQ 稳定排序 ⇒ 并列保留源表序，而源表 = `unitsInPlay(+0x470)` 的**追加序**
    /// = 进场序（`C5` §2·3）。我们的等价物 = `UnitState.PlayOrder`。
    ///
    /// 🔑 夹具**把格号序与进场序故意摆反**，否则判别不了：
    ///   · 格 7 `G3TieFirstIn`  —— **第 1 个** `Place` ⇒ `PlayOrder` 更小；
    ///   · 格 1 `G3TieSecondIn` —— **第 2 个** `Place` ⇒ `PlayOrder` 更大。
    /// 而 `AddBoard` 的源表序 = **格号升序**（格 1 在前）⇒ 若并列时按源表下标/格号挑，会挑到
    /// `G3TieSecondIn`（错的那个）。
    /// </summary>
    static void G3TieBreakByPlayOrder()
    {
        // ---- 用 method=1（同近战、同远程 ⇒ 只有并列才能分）----
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;
        var first = Place(ctx, me, 7, Unit("G3TieFirstIn", 2, 3, 4));
        var second = Place(ctx, me, 1, Unit("G3TieSecondIn", 2, 3, 4));

        Check(first.PlayOrder < second.PlayOrder, true,
              "★ （前提）`G3TieFirstIn`（格 7）的 `PlayOrder` 确实**小于** `G3TieSecondIn`（格 1）"
            + "｜🧨 `Place` 不再取号 / 摆放顺序被改 ⇒ 红（那时并列序不可判别）");
        Check(G3Joined(AiTargets.GetTargets(ctx, G3Crit(1, 1), Unit("G3TieSelf", 1, 1, 3), null, me, null)),
              "G3TieFirstIn",
              "⑦★ 并列（近战/远程都相同）时按**进场序**取（`PlayOrder` 小的先）"
            + "｜🧨 去掉 `PlayOrder` 第二键、退回源表下标（= 格号序 1 < 7）⇒ 实得 `G3TieSecondIn` ⇒ 红");

        // ---- 用 method=6（同本征生命 ⇒ 同样只能靠并列分；两档共用同一个稳定排序实现）----
        var ctx6 = Battle(new CardDef[0], new CardDef[0]);
        int me6 = ctx6.Active;
        Place(ctx6, me6, 8, Unit("G3TieLowFirst", 2, 1, 2));
        Place(ctx6, me6, 0, Unit("G3TieLowSecond", 2, 1, 2));
        Check(G3Joined(AiTargets.GetTargets(ctx6, G3Crit(6, 1), Unit("G3TieSelf6", 1, 1, 3), null, me6, null)),
              "G3TieLowFirst",
              "⑦★ `method=6` 的并列同样按**进场序**（格 8 那张先摆 ⇒ 先取它）"
            + "｜🧨 该档自己另写一个不稳定 `Sort` ⇒ 并列次序不定 ⇒ 红（或间歇红）");
    }

    // ==================================================================
    //  ⑧ 随机可复现：同一 seed + 同一局面 ⇒ 同一次抽签
    // ==================================================================

    /// <summary>
    /// 随机源**必须是 `ctx.AiRng`**（不是共享的 `ctx.Rng`，⛔ 更不是 `UnityEngine.Random`）。
    /// 🔴 **这是有意偏离原版**：原版 `BattleManager.GetRandomInt` 是单流；本仓既有决定是
    /// 「AI 这一侧的随机走 `AiRng`，不挪动共享流」（`BattleContext.cs:371-377`，
    /// 理由 = 回放**不重掷**，共享流一动整条动作序列就错位）⇒ 本件照那条决定走。
    ///
    /// 三条断言构成**判别式**（不是「同 seed 同结果」这种什么都过的写法）：
    ///   · ① 两个同 seed 的独立对局，各调一次 ⇒ 结果**一字不差**（基本可复现）；
    ///   · ② 把**共享 `ctx.Rng`** 先烧掉 5 个数，再调 ⇒ 结果**仍一字不差**
    ///     （🧨 若用 `ctx.Rng` ⇒ 必然不同 ⇒ 红。这是「没挪动共享流」的**直接证据**）；
    ///   · ③ 把 **`ctx.AiRng`** 先烧掉 5 个数，再连续调 3 次 ⇒ 结果串**必须不同**
    ///     （🧨 若用别的流 ⇒ 与 ① 完全一致 ⇒ 红）。误判概率 ≈ `(1/P(8,3))³ ≈ 3e-8`。
    /// </summary>
    static void G3RandomReproducible()
    {
        var a = G3BattleSeed(7);
        var b = G3BattleSeed(7);
        var d = G3BattleSeed(7);
        int meA = a.Active, meB = b.Active, meD = d.Active;
        int[] slots = { 0, 1, 2, 3, 5, 6, 7, 8 };
        for (int i = 0; i < slots.Length; i++)
        {
            Place(a, meA, slots[i], Unit("G3RndU" + i, 1, 1, 3));
            Place(b, meB, slots[i], Unit("G3RndU" + i, 1, 1, 3));
            Place(d, meD, slots[i], Unit("G3RndU" + i, 1, 1, 3));
        }

        var c = G3Crit(0, 3);
        var self = Unit("G3RndSelf", 1, 1, 3);

        // ② 烧共享流（`AiRng` 派生自 `seed ^ 0x0A1A1A1A`，与 `Rng` 互不干扰）
        for (int i = 0; i < 5; i++) b.Rng.Next();
        // ③ 烧 AI 流（对照：用的若正是它，结果必须变）
        for (int i = 0; i < 5; i++) d.AiRng.Next();

        string ra = G3ThreeCalls(a, meA, c, self);
        string rb = G3ThreeCalls(b, meB, c, self);
        string rd = G3ThreeCalls(d, meD, c, self);

        Check(ra == rd, false,
              "⑧（反证）先烧掉 `ctx.AiRng` 5 个数 ⇒ 随机截断结果**确实变了**（说明抽签用的正是 AiRng）"
            + "｜🧨 抽签改用别的流（例如 `ctx.Rng` / `UnityEngine.Random`）⇒ 与未烧的那一局一致 ⇒ 红");

        Check(ra, rb,
              "⑧★ 先烧掉**共享 `ctx.Rng`** 5 个数 ⇒ 随机截断结果**一字不差**（AI 抽签不碰共享流）"
            + "｜🧨 抽签改用 `ctx.Rng` ⇒ 两次不同 ⇒ 红（这正是本仓「AI 随机不挪动共享流」那条决定）");

        // ① 同 seed + 同局面 ⇒ 同结果（放在最后：前两条已经把「哪条流」钉死了）
        var a2 = G3BattleSeed(7);
        int meA2 = a2.Active;
        for (int i = 0; i < slots.Length; i++)
            Place(a2, meA2, slots[i], Unit("G3RndU" + i, 1, 1, 3));
        Check(G3ThreeCalls(a2, meA2, c, self), ra,
              "⑧★ 同一 seed + 同一局面 ⇒ 随机截断的结果**完全一致**（可复现）"
            + "｜🧨 `AiRng` 改成无 seed 的 `new Random()` ⇒ 两次不同 ⇒ 红");
    }

    /// <summary>连续 3 次 `GetTargets` 的结果串（`|` 分隔）—— 把「一次巧合」的概率压到可忽略。</summary>
    static string G3ThreeCalls(BattleContext ctx, int me, AiTargetCriteriaDto c, CardDef self)
    {
        return G3Joined(AiTargets.GetTargets(ctx, c, self, null, me, null))
             + "|" + G3Joined(AiTargets.GetTargets(ctx, c, self, null, me, null))
             + "|" + G3Joined(AiTargets.GetTargets(ctx, c, self, null, me, null));
    }

    /// <summary>同 `Battle(...)`，但 seed 由调用方给（验可复现要用它）。</summary>
    static BattleContext G3BattleSeed(int seed)
    {
        return RuleCore.NewBattle(Deck(new CardDef[0]), Deck(new CardDef[0]),
                                  seed: seed, shuffle: false);
    }
}
