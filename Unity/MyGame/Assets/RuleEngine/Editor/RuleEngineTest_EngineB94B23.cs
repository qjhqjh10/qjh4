// RuleEngineTest_EngineB94B23.cs — 第九十四会话 · 波 6 · 执行代理 `B94_B23`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
// 本件只放本批的断言；宿主 `RuleEngineTest.cs` 里只加一个
//   `{ CheckB94B23AbilityBuffs(); }` 块（⚠️ 入口名**不用 `Test`/`Report` 打头**，也**不进 8 空格
//   缩进的顶层语句** ⇒ 不会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。
//
// 本批覆盖（判据 = `d:/2/tools/decomp_full/`，逐句亲读 2026-10-29）：
//   卡表 **v40 新列 `abilityBuffs`**（原版 `CardEffect` 的逐行引擎侧载荷；缺口 = 判据
//   `资料/完全复刻/判据/B94_R-W_战斗38条分诊.md` §二 **B-3**，账 `战斗逻辑驱动:686` / `:1053`）。
//   · 生成器那一侧 → `工具/gen_cards_engine.py` 的 `ability_buffs_of`
//   · 读取那一侧 → `Data/CardDatabase.cs` 文件末的 `MapAbilityBuffs` / `RecordAbilityBuffs` /
//     `CardDatabase.AbilityBuffsOf`（旁表，因为 `CardDef` 不在本代理白名单）
//   · 分派器 → `decomp_full/CardScript__AddEffect.c:209-967`（`buffType(+0x38)` 是分派键）
//
// 🔴 每组断言都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94B23AbilityBuffs()
    {
        Section("🆕 B94_B23：卡表 v40 `abilityBuffs` 列（原版 `CardEffect` 逐行载荷 · 账 686 / 1053）");
        // ---- ① 映射管道（**不依赖**卡表有没有重生成 ⇒ 今天就该绿）----
        B94B23T1MapPipeline();
        // ---- ② 真值表（⚠️ 前提 = 卡表已重生成到 v40；**未重生成时 T2 整段预期红**）----
        B94B23T2RealData();
    }

    // ---------------------------------------------------------------------------------------------
    //  T1：`abilityBuffs` 的映射管道 —— DTO → `AbilityBuffSpec` → `AbilityBuffsOf`
    //      🔑 **这一段用合成卡表跑**，所以它**不依赖** `cards_engine.json` 有没有重生成：
    //         列一旦存在，11 个 buff 分组就各自能命中一次 —— 这正是本批要证明的那件事。
    // ---------------------------------------------------------------------------------------------
    static void B94B23T1MapPipeline()
    {
        // ---- 前提：`Parse` 能把合成表解析出来 ----
        var realPool = CardDatabase.Load();
        List<CardDef> probe = null;
        try
        {
            probe = CardDatabase.Parse(B94B23ProbeJson());
            Check(probe.Count, 2, "（前提）两张合成探针卡解析出来了");
        }
        finally
        {
            // 合成卡的名字会进全局名字索引 ⇒ 收尾复原（同 `RuleEngineTest_EngineE9.cs` 的写法）。
            if (realPool != null) CreatePool.BuildNameIndex(realPool);
        }
        if (probe == null || probe.Count != 2) return;

        var rows = CardDatabase.AbilityBuffsOf("B23PROBE");
        // 🧨 把 `RecordAbilityBuffs` 从 `:1749` 那一句里删掉 ⇒ 这里立刻变 null。
        CheckTrue(rows != null, "前提：挂载生效（`CardDatabase.AbilityBuffsOf` 拿得到探针的行）");
        if (rows == null) return;

        // 探针卡 A 一共落了 **12** 行：11 个分组各 1 行 + 1 行 `handBuff` 的同类型载荷。
        // 🧨 生成器/映射把 `buffType == 0` 也落下来 ⇒ 这里会多一行（前提：0 = none 不落）。
        Check(rows.Count, 12, "行数 = 12（11 个分组各 1 行 + 1 行 `handBuff` 容器）");

        // ---- 11 个分组**各自命中一次**（判据 = `AddEffect.c` 的 switch 支；⛔ 不是「有哪些类型」的枚举抄写）----
        int[] groups = { 5, 6, 10, 11, 25, 30, 40, 45, 70, 85, 95 };
        var hit = new Dictionary<int, int>();
        foreach (var r in rows)
        {
            int n;
            hit.TryGetValue(r.BuffType, out n);
            hit[r.BuffType] = n + 1;
        }
        foreach (int bt in groups)
        {
            int n;
            hit.TryGetValue(bt, out n);
            // 🧨 映射按 `buffType` 过滤/归并（例如「只认 1/2/3」）⇒ 对应那条会变 0。
            Check(n, 1, $"分组 `{bt}` 命中一次（`BuffType = {bt}`）");
        }
        // 反向：没被点名的分组不该冒出来（探针里只写了这 11 种 + 第 12 行的 `addStats`）。
        int totalInGroups = 0;
        foreach (int bt in groups) { int n; hit.TryGetValue(bt, out n); totalInGroups += n; }
        Check(totalInGroups, 11, "11 个分组合计 11 行（不多不少）");

        // ---- 载荷逐格往返（⛔ 不从实现反算：期望值就是探针 JSON 里写死的数）----
        // 见 `B94B23ProbeJson()` 的那张对照表。
        CheckTrue(Find(rows, 5, "meleeAttackChange") == 5, "`swapStats(5)`：`meleeAttackChange` 往返 = 5");
        CheckTrue(Find(rows, 6, "newRace") == 460, "`changeRace(6)`：`newRace` 往返 = 460");
        CheckTrue(Find(rows, 10, "costChange") == -3, "`changeAbilityCost(10)`：`costChange` 往返 = -3（**负值**）");
        CheckTrue(Find(rows, 11, "extrinsic") == 1, "`removeExtrinsic(11)`：`extrinsic` 往返 = 1（`1` 是合法载荷，⛔ 别当缺值）");
        CheckTrue(Find(rows, 25, "healthChange") == 7, "`ephemeralCopy(25)`：`healthChange` 往返 = 7");
        CheckTrue(Find(rows, 30, "newRace") == 5, "`addRace(30)`：`newRace` 往返 = 5");
        CheckTrue(Find(rows, 40, "trait") == 400, "`replaceAbility(40)`：`trait` 往返 = 400（= `activeAbility`）");
        CheckTrue(Find(rows, 45, "armourChange") == 2, "`addStatsDropPod(45)`：`armourChange` 往返 = 2");
        CheckTrue(Find(rows, 70, "meleeAttackValueOptions") == 50, "`setMeleeAttack(70)`：`meleeAttackValueOptions` 往返 = 50（`ThisCardHealth`）");
        CheckTrue(Find(rows, 85, "rangeAttackValueOptions") == 10, "`setRangeAttack(85)`：`rangeAttackValueOptions` 往返 = 10（`TargetMeleeAttack`）");
        CheckTrue(Find(rows, 95, "costValueOptions") == 130, "`setCost(95)`：`costValueOptions` 往返 = 130（`TargetCost`）");

        // 🔴 **`0` 必须能往返**（这是「不稀疏」的理由：`setCost` 设成 0 = 免费，丢了就静默做错）。
        // 🧨 生成器改成「只落非 0 格」⇒ 这里会变 `-99`（哨兵 = 没找到/字段丢了）。
        CheckTrue(Find(rows, 95, "costChange") == 0, "🔴 `setCost(95)`：`costChange = 0` **照样在**（=「设成 0」有语义，⛔ 不是缺值）");

        // ---- 容器区分（`at`）----
        int handRows = 0;
        foreach (var r in rows) if (r.IsHandBuff) handRows++;
        // 🧨 把 `handBuff` 也标成 `buffEffect` ⇒ 这条变 0（消费端就会把「手牌效果」当 `AddEffect` 结算 = 做多）。
        Check(handRows, 1, "`handBuff.cardEffect` 那一行标成 `IsHandBuff = true`");

        // ---- 缺列 = 未知（`null`）而不是「没有行」（`[]`）----
        // 🧨 `MapAbilityBuffs` 里把 `return null;` 改成 `return new AbilityBuffSpec[0];` ⇒ 这条变 `未知` 的反面。
        CheckTrue(CardDatabase.AbilityBuffsOf("B23NOCOLUMN") == null,
                  "🔴 缺列 ⇒ `null`（**未知**）；⛔ 不许读成「这张卡没有 CardEffect」");
        CheckTrue(CardDatabase.AbilityBuffsOf("NO_SUCH_CARD") == null, "表里没有的 id ⇒ `null`（未知）");
        CheckTrue(CardDatabase.AbilityBuffsOf((CardDef)null) == null, "`def == null` ⇒ `null`（不炸）");
    }

    /// <summary>在探针行里找 `buffType == bt` 的那一行的 `field`；找不到 ⇒ `-99`（哨兵，让断言红得看得懂）。</summary>
    static int Find(IReadOnlyList<CardDatabase.AbilityBuffSpec> rows, int bt, string field)
    {
        foreach (var r in rows)
        {
            if (r == null || r.BuffType != bt) continue;
            switch (field)
            {
                case "meleeAttackChange": return r.MeleeAttackChange;
                case "meleeAttackValueOptions": return r.MeleeAttackValueOptions;
                case "rangeAttackChange": return r.RangeAttackChange;
                case "rangeAttackValueOptions": return r.RangeAttackValueOptions;
                case "armourChange": return r.ArmourChange;
                case "armourValueOptions": return r.ArmourValueOptions;
                case "healthChange": return r.HealthChange;
                case "healthValueOptions": return r.HealthValueOptions;
                case "costChange": return r.CostChange;
                case "costValueOptions": return r.CostValueOptions;
                case "randomValueChange": return r.RandomValueChange;
                case "trait": return r.Trait;
                case "traitValue": return r.TraitValue;
                case "newRace": return r.NewRace;
                case "extrinsic": return r.Extrinsic;
                case "extrinsicVariable": return r.ExtrinsicVariable;
                case "permanentBuff": return r.PermanentBuff;
                case "doNotStack": return r.DoNotStack;
                case "markOfChaosType": return r.MarkOfChaosType;
                case "daemonHostType": return r.DaemonHostType;
                case "mutationType": return r.MutationType;
                case "untilEndOfTurn": return r.UntilEndOfTurn;
                case "untilNextAttack": return r.UntilNextAttack;
                default: return -99;
            }
        }
        return -99;
    }

    /// <summary>合成探针卡表：A 卡带 11 个分组（各 1 行）+ 1 行 `handBuff`；B 卡**整列缺席**（= 未知）。</summary>
    static string B94B23ProbeJson()
    {
        // 一行 = `ab/lg/at/buffType` + 显式写出来的载荷格（其余格按 JSON 缺键 = 0）。
        // 🔴 这里就是 T1 里那些往返断言的**唯一期望值来源**（⛔ 别去读实现反算）。
        string a = "{\"id\":\"B23PROBE\",\"name\":\"B23 Probe\",\"type\":\"tactic\",\"desc\":\"probe\","
                 + "\"rarity\":\"Common\",\"faction\":\"Ultramarines\",\"cost\":1,\"attack\":0,"
                 + "\"health\":0,\"ranged\":0,\"subtype\":\"\",\"keywords\":[],\"cardTraits\":[],"
                 + "\"abilityBuffs\":["
                 + "{\"ab\":0,\"lg\":0,\"at\":\"buffEffect\",\"buffType\":5,\"meleeAttackChange\":5},"
                 + "{\"ab\":0,\"lg\":1,\"at\":\"buffEffect\",\"buffType\":6,\"newRace\":460},"
                 + "{\"ab\":0,\"lg\":2,\"at\":\"buffEffect\",\"buffType\":10,\"costChange\":-3},"
                 + "{\"ab\":0,\"lg\":3,\"at\":\"buffEffect\",\"buffType\":11,\"extrinsic\":1},"
                 + "{\"ab\":0,\"lg\":4,\"at\":\"buffEffect\",\"buffType\":25,\"healthChange\":7},"
                 + "{\"ab\":0,\"lg\":5,\"at\":\"buffEffect\",\"buffType\":30,\"newRace\":5},"
                 + "{\"ab\":0,\"lg\":6,\"at\":\"buffEffect\",\"buffType\":40,\"trait\":400},"
                 + "{\"ab\":0,\"lg\":7,\"at\":\"buffEffect\",\"buffType\":45,\"armourChange\":2},"
                 + "{\"ab\":0,\"lg\":8,\"at\":\"buffEffect\",\"buffType\":70,\"meleeAttackValueOptions\":50},"
                 + "{\"ab\":0,\"lg\":9,\"at\":\"buffEffect\",\"buffType\":85,\"rangeAttackValueOptions\":10},"
                 + "{\"ab\":0,\"lg\":10,\"at\":\"buffEffect\",\"buffType\":95,\"costChange\":0,"
                 + "\"costValueOptions\":130},"
                 + "{\"ab\":0,\"lg\":11,\"at\":\"handBuff\",\"buffType\":1,\"healthChange\":2}"
                 + "]}";
        // B 卡：**没有** `abilityBuffs` 键 ⇒ `null` = 未知（⛔ 不是 `[]`）。
        string b = "{\"id\":\"B23NOCOLUMN\",\"name\":\"B23 No Column\",\"type\":\"tactic\",\"desc\":\"probe\","
                 + "\"rarity\":\"Common\",\"faction\":\"Ultramarines\",\"cost\":1,\"attack\":0,"
                 + "\"health\":0,\"ranged\":0,\"subtype\":\"\",\"keywords\":[],\"cardTraits\":[]}";
        return "{\"version\":40,\"count\":2,\"cards\":[" + a + "," + b + "]}";
    }

    // ---------------------------------------------------------------------------------------------
    //  T2：**真值卡表**（`Resources/cards_engine.json`）
    //      🔴 前提 = 卡表已重生成到 v40。**未重生成时这一段整段预期红**（说明见前提那一条），
    //         依赖它的断言在 `version < 40` 时**跳过并出声**（⛔ 不是静默跳过：上面那条前提已经红了）。
    // ---------------------------------------------------------------------------------------------
    static void B94B23T2RealData()
    {
        var ta = Resources.Load<TextAsset>(CardDatabase.ResourcePath);
        CheckTrue(ta != null, "前提：卡表资源 `Resources/" + CardDatabase.ResourcePath + ".json` 读得到");
        if (ta == null) return;
        var fd = JsonUtility.FromJson<CardDatabase.FileDto>(ta.text);
        CheckTrue(fd != null && fd.cards != null && fd.cards.Length > 0, "前提：卡表能反序列化出 cards");
        if (fd == null || fd.cards == null || fd.cards.Length == 0) return;

        // 🧨 卡表没重生成（仍是 v39）⇒ 这一条红是**预期的**，重生成后转绿。
        CheckTrue(fd.version >= 40,
                  $"前提：卡表 version ≥ 40（现读 {fd.version}）—— `abilityBuffs` 列存在。"
                  + "⚠️ 未重生成时这条红是**预期的**（调度台在无 .cs 写手时串行重生成后转绿）");
        if (fd.version < 40)
        {
            Debug.Log("[RuleEngine] B94_B23：卡表仍是 v" + fd.version
                    + " ⇒ `abilityBuffs` 真值断言**跳过**（前提那一条已红）。重跑 工具/gen_cards_engine.py 后再看。");
            return;
        }

        // ---- 规模 ----
        int withCol = 0, nonEmpty = 0, rows = 0;
        foreach (var c in fd.cards)
        {
            if (c == null || c.abilityBuffs == null) continue;
            withCol++;
            if (c.abilityBuffs.Length > 0) nonEmpty++;
            rows += c.abilityBuffs.Length;
        }
        // 🧨 生成器改成「只在有行时才落列」⇒ 这条会红（`null` 与 `[]` 就分不开了）。
        Check(withCol, fd.cards.Length, "每一张卡都落这一列（`null` = 未知，⛔ 不该出现在当前表里）");
        // 现读（2026-10-29 干跑）：池 1,129 张里 636 张非空、共 954 行（`handBuff` 53 行）。
        Check(rows, 954, "行数 = 954（现读；卡池数据变了就更新这条）");
        Check(nonEmpty, 636, "非空卡 = 636 张（现读）");

        // ---- 分诊表点名的 11 个分组：**本 build 只有 3 个有数据**（其余 8 个是休眠分支）----
        //   🔴 这条**必须写死**，否则下一个人会以为「11 个分组都接上就没问题了」——
        //      实际 `5/6/10/11/30/40/45/85` 全池 **0 行**（`grep -o '"buffType": 5[,}]' cards_raw.json` = 0）。
        int[] named = { 5, 6, 10, 11, 25, 30, 40, 45, 70, 85, 95 };
        var byBuff = new Dictionary<int, int>();
        foreach (var c in fd.cards)
        {
            if (c == null || c.abilityBuffs == null) continue;
            foreach (var r in c.abilityBuffs)
            {
                if (r == null) continue;
                int n; byBuff.TryGetValue(r.buffType, out n);
                byBuff[r.buffType] = n + 1;
            }
        }
        foreach (int bt in named)
        {
            int n; byBuff.TryGetValue(bt, out n);
            int want = (bt == 25) ? 1 : (bt == 70) ? 1 : (bt == 95) ? 3 : 0;
            // 🧨 生成器把别的 buffType 也塞进这些编号（枚举错位）⇒ 对应那条会红。
            Check(n, want, $"分组 `{bt}` 的池内行数 = {want}"
                         + (want == 0 ? "（🔴 本 build 0 行 = 落列后仍是**休眠分支**，⛔ 别以为接上就完事）" : ""));
        }

        // ---- 抽样逐格核对（数据来自 `cards_raw.json` 现读；⛔ 不从实现反算）----
        var war = CardDatabase.FindById(CardDatabase.Load(), "SAU27");
        CheckTrue(war != null, "前提：`SAU27 Scarab Swarm` 在池里");
        if (war != null)
        {
            var rr = CardDatabase.AbilityBuffsOf(war);
            CheckTrue(rr != null && rr.Count == 1,
                      "`SAU27 Scarab Swarm` 恰有 1 行 `CardEffect`（`ab=0 lg=0` · `setMeleeAttack`）");
            if (rr != null && rr.Count == 1)
            {
                Check(rr[0].BuffType, 70, "`SAU27`：`buffType = 70 setMeleeAttack`");
                Check(rr[0].MeleeAttackValueOptions, 50, "`SAU27`：`meleeAttackValueOptions = 50`（`ThisCardHealth`）");
                Check(rr[0].Extrinsic, 1, "`SAU27`：`extrinsic = 1`（效果绑**施放者**）");
                Check(rr[0].IsHandBuff, false, "`SAU27`：这一行不是 `handBuff`");
            }
        }

        // 附魔三档（12/15/16）在池内 **132 行**里占 72 行 —— 这一列同时服务 v38 `enchantment` 列，
        // 两边同源（⛔ 别只改一边）。
        int ench = 0;
        foreach (var c in fd.cards)
        {
            if (c == null || c.abilityBuffs == null) continue;
            foreach (var r in c.abilityBuffs)
                if (r != null && (r.buffType == 12 || r.buffType == 15 || r.buffType == 16)) ench++;
        }
        // 现读：12 → 23 · 15 → 37 · 16 → 13（池内），合计 73。
        Check(ench, 73, "附魔三档（`12/15/16`）合计 73 行（现读；与 v38 `enchantment` 列同源）");
    }
}
