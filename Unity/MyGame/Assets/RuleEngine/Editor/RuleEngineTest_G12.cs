// RuleEngineTest_G12.cs — 引擎线 `G12` 的断言（2026-10-24）
//
// 本件钉 `cards_engine.json` **v23 新列 `activeAbilityLogic`** 的整条消费链：
//   ① `CardDatabase.ActiveAbilityLogicDto` → `CardDef.ActiveAbilityLogicSpec` 的读数（132 张 / 193 行 / …）；
//   ② **`null`（旧表 = 未知） ≠ `[]`（明确没有行）**；
//   ③ 旧表（< v23）**出声**（⛔ 不许静默读成「全池都没有主动能力」）；
//   ④ `CardDef.ApplyActiveAbilityLogic` 的归位：**只挂「1 个对象 ↔ 1 行 ↔ 1 条 op」那一档**，
//      其余留 `-1` 并把原因记进 `ActiveAbilityMap`（⛔ 不按行号硬配，判据见那个方法的 doc）。
//
// 判据（按权威顺序）：
//   ① **原版全量反编译** `d:/2/tools/decomp_full/`：
//      · `RawCardScript__ResolveActiveAbility.c:18` / `:26-31`（选 `activeAbility` 还是 `alternativeAbility`）
//        / `:50`（取那个对象的 `activeAbilityLogic`）/ `:58-67`（逐行 `PlayAbility(…, 400, 0)`）；
//      · `AbilityLogic__PlayAbility.c:262`（目标 = 该行的 `targetCriteria`）/ `:235-236`（`local_178 *= fM`）；
//      · `CardScript__ResolveActiveAbilityPlayed.c:22`（`hasActiveAbility == 0` ⇒ 整条路短路 ⇒ 本 build 死数据）。
//   ② 数据侧读数（v23 落列的口径与逐张表）→ `资料/普查产出_1024/D11_activeAbility列.md`；
//      为什么**不能**并进 `abilityTargets` → `资料/普查产出_1024/D10_abilityTargets管线.md` §三。
//   ③ 本件自己的判定与「还差什么」→ `资料/普查产出_1024/G12_activeAbility消费.md`。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量（132 / 193 / 30 / "gap:shape" …），⛔ 不从实现常量反算（那就是自证）。
//
// ⚠️ **本文件还没接线**：`RuleEngineTest.cs`（`Run()` 里加一行 `Step(...)`）与
//    `RuleEngineTest_Diag.cs`（`工具/gen_diag_runner.py` 重生成）都**不在 G12 的白名单** ⇒
//    由主对话补。补之前 `Run()` 的覆盖闸会红并点名 `TestG12ActiveAbilityLogic`（那是**设计如此**：
//    它宁可红，也不要「文件在、用例从没跑过」）。
using System;
using System.Collections.Generic;
using System.Text;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestG12ActiveAbilityLogic);`）。
    /// 子用例是 `G12…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestG12ActiveAbilityLogic()
    {
        Section("🆕 引擎线 G12：`activeAbilityLogic`（v23）的读数 / null≠[] / 版本守卫 / 归位");
        var pool = CardDatabase.Load();
        G12DataReadings(pool);
        G12NullVsEmpty(pool);
        G12OldTableWarns(pool);
        G12Attribution(pool);
        G12GroupSelection(pool);
    }

    // ==============================================================================================
    //  ① 数据读数（DTO → `CardDef.ActiveAbilityLogic`）—— 与 D11 的现读逐项对账
    // ==============================================================================================

    /// <summary>v23 的**每一张卡**都带这一列（没行 = `[]`）；读数 = D11 §三 的表。</summary>
    static void G12DataReadings(List<CardDef> pool)
    {
        // 🔴 2026-10-25（`X20`）就地改期望值：1127 → 1128（池内新增隐藏附魔模板 `Pray Enchantment`）。
        Check(pool.Count, 1129, "(前提) 卡池 1129 张｜2026-10-27 `A1a` 收 `DA42` 附魔模板 `Reconnaissance Mission`（v35）");

        int notVersioned = 0, nullColumn = 0, cardsWith = 0, cardsEmpty = 0;
        int objects = 0, logicRows = 0, gate0Cards = 0, gate0Rows = 0, fmPositive = 0;
        int activeObjects = 0, altObjects = 0;
        int badAb = 0, badGate = 0, badTrigger = 0, lenMismatch = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (!c.ActiveAbilityLogicVersioned) notVersioned++;
            var rows = c.ActiveAbilityLogic;
            if (rows == null) { nullColumn++; continue; }     // 旧表 / 没落
            if (rows.Count == 0) { cardsEmpty++; continue; }
            cardsWith++;
            bool anyGate0 = false;
            foreach (var r in rows)
            {
                if (r == null) { badAb++; continue; }
                objects++;
                if (r.Ability == CardDef.ActiveAbilityObject) activeObjects++;
                else if (r.Ability == CardDef.AlternativeAbilityObject) altObjects++;
                else badAb++;
                if (r.Gate != 1 && r.Gate != 0 && r.Gate != CardDef.GateUnknown) badGate++;
                if (r.Trigger != 400) badTrigger++;            // `ThisUnitAbilityUsed`
                if (r.TargetsAffected != null && r.FurtherMultiplier != null
                    && r.TargetsAffected.Length != r.FurtherMultiplier.Length) lenMismatch++;
                int n = r.LogicCount;
                logicRows += n;
                if (r.Gate == 0) { gate0Rows += n; anyGate0 = true; }
                if (r.FurtherMultiplier != null)
                    foreach (int f in r.FurtherMultiplier) if (f > 0) fmPositive++;
            }
            if (anyGate0) gate0Cards++;
        }

        Check(notVersioned, 0,
              "★ v23 卡表 ⇒ **1128/1128** 张卡都带 `activeAbilityLogic` 的正面信号"
            + "｜🧨 `CardDatabase.Parse` 里写成 `>= 24` / 漏传那一格 ⇒ 红");
        Check(nullColumn, 0,
              "★ **没有一张卡的这一列是 `null`**（v23 每张卡都落；`null` 只该出现在旧表 / 调用方没给）"
            + "｜🧨 生成器改成「只给有行的卡落列」⇒ 红（那会把「明确没有」读成「未知」）");
        Check(cardsWith, 132,
              "★ 有 active/alt 行的卡 = **132 张**（D11 §3·3 现读）"
            + "｜🧨 生成器的 `active_ability_logic_of()` 少扫一个能力对象 / 生成器换了数据源 ⇒ 红");
        // 🔴 2026-10-25（`X20`）：995 → **996**（池内新增的附魔卡没有 active/alt 行 ⇒ `[]`；
        //    1128 − 132 = 996）。
        // 🔴 2026-10-27（`A1a`）：996 → **997**（池 1,128→1,129，新模板 `Reconnaissance Mission`
        //    同样没有 active/alt 行 ⇒ 1129 − 132 = 997）。
        Check(cardsEmpty, 997,
              "★ 明确没有行的卡 = **997 张**（1129 − 132）"
            + "｜🧨 把「没有行」写成不落列 ⇒ 这个数掉、上面 `nullColumn` 涨 ⇒ 两条一起红");
        Check(objects, 133,
              "★ 能力对象总数 = **133**（132 个 `activeAbility` ＋ `SW21` 的 1 个 `alternativeAbility`）"
            + "｜🧨 `alternativeAbility` 那一项漏收（键名也 `activeAbilityLogic`）⇒ 掉到 132");
        Check(activeObjects, 132, "★ `ab = -1`（activeAbility）的对象 = **132**｜🧨 `ab` 读错键 ⇒ 红");
        Check(altObjects, 1,
              "★ `ab = -2`（alternativeAbility）的对象 = **1**（`SW21 Wulfen Pack Leader`）"
            + "｜🧨 生成器按 `alternativeAbilityLogic`（**不存在的键**）去取 ⇒ 掉到 0");
        Check(logicRows, 193,
              "★ logic 行合计 = **193**（192 active ＋ 1 alternative，D11 §3·3）"
            + "｜🧨 只收对象不收逐行 `ta` / `ta` 读错字段 ⇒ 红");
        Check(gate0Cards, 45, "★ `gate = 0`（本 build 死数据）的卡 = **45 张**｜🧨 把死数据丢掉 ⇒ 掉到 0");
        Check(gate0Rows, 52, "★ `gate = 0` 的 logic 行 = **52 行**｜🧨 同上");
        Check(fmPositive, 109,
              "★ `furtherMultiplier > 0` 的行格 = **109**（`fm=1`:107 · `fm=2`:2，D11 §3·3）"
            + "｜🧨 `fm` 读成空 / 落成 0 ⇒ 红");
        Check(badAb + badGate + badTrigger + lenMismatch, 0,
              "★ 每行的 `ab` ∈ {-1,-2}、`gate` ∈ {1,0,-1}、`trigger` 恒 400、`ta`/`fm` 同长"
            + $"（现读违规 {badAb + badGate + badTrigger + lenMismatch} 处）"
            + "｜🧨 生成器把 `trigger` 写成别的时机 / 丢掉 `gate` 列 / `ta`·`fm` 长度不一致 ⇒ 红");
    }

    // ==============================================================================================
    //  ② `null`（旧表 = 未知） ≠ `[]`（明确没有行）
    // ==============================================================================================

    /// <summary>两张**合成表**（v23 空数组 / v22 整列没有）—— 同一段解析路径、两种含义必须分得开。</summary>
    static void G12NullVsEmpty(List<CardDef> pool)
    {
        const string v23Empty =
            "{\"version\":23,\"count\":1,\"cards\":[{\"id\":\"G12E1\",\"name\":\"G12 空列探针\","
          + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"activeAbilityLogic\":[]}]}";
        const string v22NoColumn =
            "{\"version\":22,\"count\":1,\"cards\":[{\"id\":\"G12E2\",\"name\":\"G12 旧表探针\","
          + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\"}]}";

        CardDef empty = null, old = null;
        try
        {
            var a = CardDatabase.Parse(v23Empty);
            var b = CardDatabase.Parse(v22NoColumn);
            if (a.Count == 1) empty = a[0];
            if (b.Count == 1) old = b[0];
        }
        finally
        {
            // ⚠️ `Parse` 会把合成卡的卡名并进 `CreatePool` 的名字索引 ⇒ 用真池重建一次，别污染后面的用例。
            CreatePool.BuildNameIndex(pool);
        }

        CheckTrue(empty != null && old != null, "(前提) 两张合成探针卡都解析出来了");
        if (empty == null || old == null) return;

        // ---- `[]` = 明确没有行 ----
        CheckTrue(empty.ActiveAbilityLogic != null,
                  "★ v23 的 `\"activeAbilityLogic\": []` ⇒ `CardDef.ActiveAbilityLogic` **不是 null**"
                + "（= 明确没有主动能力行）｜🧨 把空数组也归成 `null` ⇒ 本行红");
        CheckTrue(empty.ActiveAbilityLogic != null && empty.ActiveAbilityLogic.Count == 0,
                  "★ 且 `Count == 0`｜🧨 同上");
        CheckTrue(empty.ActiveAbilityLogicVersioned,
                  "★ v23 的表 ⇒ `ActiveAbilityLogicVersioned = true`｜🧨 那格写成 `>= 24` ⇒ 红");
        Check(empty.ActiveAbilityMap ?? "(null)", "",
              "★ 空表 ⇒ 归位结论是**空串 = 不适用**（⛔ 不是 `gap:*`，那不是「做不到」）"
            + "｜🧨 让空表也走 `gap:no-ops` 那条 ⇒ 红");

        // ---- `null` = 这一列没落（未知）----
        CheckTrue(old.ActiveAbilityLogic == null,
                  "★ v22（整列没有）⇒ `ActiveAbilityLogic` **是 null = 未知**"
                + "｜🧨 给旧表 `new List<>()`（空表）⇒ 本行红，且「未知」被静默读成「明确没有」= 红线");
        CheckTrue(!old.ActiveAbilityLogicVersioned,
                  "★ v22 ⇒ `ActiveAbilityLogicVersioned = false`｜🧨 写成 `>= 22` ⇒ 红");

        // ---- 🔴 灭自证：两种含义**不可能被同一个实现同时满足** ----
        bool emptyIsNull = empty.ActiveAbilityLogic == null;
        bool oldIsNull = old.ActiveAbilityLogic == null;
        CheckTrue(emptyIsNull != oldIsNull,
                  "★ **灭自证**：同一段 `Parse`、同样「没有行」，但 v23 `[]` 与 v22 缺列**必须落在两侧**"
                + $"（现读：v23 空列 isNull={emptyIsNull} · v22 缺列 isNull={oldIsNull}）"
                + "｜🧨 「一律 null」或「一律空表」两种简化都会让这一条红 —— 它挡的正是「把两种含义并成一个」");
    }

    // ==============================================================================================
    //  ③ 旧表（< v23）**出声** —— 缺列 ≠ 全池都没有
    // ==============================================================================================

    /// <summary>v22 表 ⇒ 点名 `activeAbilityLogic` 的 `LogWarning` **两条**（`<23` 那条整列、`<24` 那条逐行三格）；
    /// v23 表 ⇒ **一条**（只差 `<24` 那格）；v24（当前版本）表 ⇒ **一条也没有**。</summary>
    static void G12OldTableWarns(List<CardDef> pool)
    {
        const string oldTable =
            "{\"version\":22,\"count\":1,\"cards\":[{\"id\":\"G12W1\",\"name\":\"G12 版本守卫探针\","
          + "\"type\":\"unit\",\"desc\":\"\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\"}]}";
        const string newTable =
            "{\"version\":23,\"count\":1,\"cards\":[{\"id\":\"G12W2\",\"name\":\"G12 新表守卫探针\","
          + "\"type\":\"unit\",\"desc\":\"\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"activeAbilityLogic\":[]}]}";
        // 🆕 2026-10-24（`E1` 把卡表推到 v24）：再探一格「当前版本不出声」——
        //   否则 v24 的真实表每次装载都要刷一条「< 24」，而那正是要避免的噪声。
        const string curTable =
            "{\"version\":24,\"count\":1,\"cards\":[{\"id\":\"G12W3\",\"name\":\"G12 当前表守卫探针\","
          + "\"type\":\"unit\",\"desc\":\"\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"activeAbilityLogic\":[]}]}";

        int oldWarns = 0, midWarns = 0, curWarns = 0;
        string firstOld = null;
        bool countingOld = false, countingMid = false, countingCur = false;
        Application.LogCallback cb = (text, stack, type) =>
        {
            if (type != LogType.Warning || text == null) return;
            if (!text.Contains("activeAbilityLogic")) return;      // 只数**这一列**那两条守卫
            if (countingOld) { oldWarns++; if (firstOld == null) firstOld = text; }
            if (countingMid) midWarns++;
            if (countingCur) curWarns++;
        };
        Application.logMessageReceived += cb;
        try
        {
            countingOld = true;
            CardDatabase.Parse(oldTable);          // v22：整列没有 + 每行三格没有 ⇒ 两条都要出声
            countingOld = false;
            countingMid = true;
            CardDatabase.Parse(newTable);          // v23：列有了、三格没有 ⇒ 只该剩 `<24` 那一条
            countingMid = false;
            countingCur = true;
            CardDatabase.Parse(curTable);          // v24：当前版本 ⇒ 一条都不该有
            countingCur = false;
        }
        finally
        {
            countingOld = countingMid = countingCur = false;
            Application.logMessageReceived -= cb;
            CreatePool.BuildNameIndex(pool);
        }

        Check(oldWarns, 2,
              "★ **v22 旧表出声两条**：`< 23`（整列缺）+ `< 24`（每行三格缺）各一条，且都点名 `activeAbilityLogic`"
            + "（⛔ 缺列不许静默读成「全池都没有主动能力」）"
            + "｜🧨 删掉 `CardDatabase.Parse` 里任一版本守卫 ⇒ 掉成 1/0");
        CheckTrue(firstOld != null && firstOld.Contains("null") && firstOld.Contains("-1"),
                  "★ 那条警告把后果写清了（`null = 未知` / `-1 = 读不到`），⛔ 不是一句「版本低」"
                + "｜🧨 改成没有后果说明的空话 ⇒ 红");
        Check(midWarns, 1,
              "★ **v23 表只出一条**（列有了，只差 `chosenAbility`/`playersAffected`/`damageCriteria` 三格）"
            + "｜🧨 把 `< 24` 守卫写成 `<= 24` / 漏掉它 ⇒ 红");
        Check(curWarns, 0,
              "★ **v24 当前表不出一条**（版本够就不该有警告）"
            + "｜🧨 无条件出声 ⇒ 每次正常装载刷屏 ⇒ 红");
    }

    // ==============================================================================================
    //  ④ 归位：只挂「1 个对象 ↔ 1 行 ↔ 1 条 op」；其余留 -1 + 记原因
    // ==============================================================================================

    /// <summary>正例 + 四道闸门的反例（每一档各挑一张真卡）。</summary>
    static void G12Attribution(List<CardDef> pool)
    {
        // ---- ④-a 正例 `AM38 Baneblade Tank`：`Duty: Deal 8 damage` ↔ 行0 `ta=30` / `fm=0` ----
        {
            // 判据（原版）：`RawCardScript__ResolveActiveAbility.c:50-67` 逐行施放；行0 的
            // `targetCriteria.targetsAffected = 30 (target)` ⇒ 由施放方选一个敌方单位（`GetTargets.c:192-207`）。
            var am38 = CardDatabase.FindById(pool, "AM38");
            CheckTrue(am38 != null, "(前提) `AM38 Baneblade Tank` 在池内");
            if (am38 != null)
            {
                CheckTrue(am38.ActiveAbilityLogic != null && am38.ActiveAbilityLogic.Count == 1
                          && am38.ActiveAbilityLogic[0].LogicCount == 1,
                          "(前提) `AM38` 的主动能力组 = 1 个对象 / 1 行");
                Check(am38.ActiveAbilityMap ?? "(null)", "attached",
                      "★ `AM38` 的归位结论 = `attached`（1 对象 ↔ 1 行 ↔ 1 条 op）"
                    + "｜🧨 闸门收得过紧（例如要求 op 数 ≥ 2）⇒ 红");
                var ops = am38.TriggerOps(KeywordTable.Duty);
                CheckTrue(ops != null && ops.Count == 1, "(前提) `AM38` 的 `Duty:` 正文解析出 1 条 op");
                var op = ops != null && ops.Count == 1 ? ops[0] : null;
                CheckTrue(op != null && op.ActiveAbilityRow == 0,
                          "★ **归位标记**：那条 `deal 8` 的 op 挂上 `ActiveAbilityRow = 0`"
                        + "｜🧨 不在 `CollectTriggerOps` 末尾调 `ApplyActiveAbilityLogic` ⇒ 红（-1）");
                CheckTrue(op != null && op.SubjectTargetsAffected == 30,
                          "★ **`ta` 挂对**：`AM38` 的 `Duty: Deal 8 damage` ⇒ `ta = 30 (target)`"
                        + "｜🧨 按行号硬配 / 把 `ta` 挂到别的 op ⇒ 红");

                // ---- 🔴 灭自证：卡自己的 `cardAbilities` 是**另一个值**（100 = adjacentToSelf）----
                int cardAbilTa = -1;
                if (am38.AbilityTargets != null && am38.AbilityTargets.Count > 0
                    && am38.AbilityTargets[0] != null && am38.AbilityTargets[0].TargetsAffected != null
                    && am38.AbilityTargets[0].TargetsAffected.Length > 0)
                    cardAbilTa = am38.AbilityTargets[0].TargetsAffected[0];
                Check(cardAbilTa, 100,
                      "★ **灭自证**：`AM38` 的 `cardAbilities[0]` 自己那条是 `ta = 100`（相邻 +1 护甲）"
                    + "—— 与 `Duty` 那条的 `30` **不是同一个数**"
                    + "｜🧨 任何「把 `cardAbilities` 的值抄过来 / 两个来源混用」的实现都不可能同时满足这两条");
                CheckTrue(op != null && op.SubjectTargetsAffected != cardAbilTa,
                          "★ 且 op 上挂的确实**不是** `cardAbilities` 那个值"
                        + "｜🧨 退化成「片段分支贴什么就留什么」（改前 `ta=100`）⇒ 红");
                CheckTrue(op != null && op.AbilityFurtherMultiplier == 0,
                          "★ `fm` 也挂上了（`AM38` 的 active 行 `fm = 0` = 原版**明确不乘**）"
                        + "｜🧨 只挂 `ta` 不挂 `fm` / 把 0 当未知 ⇒ 红（-1）");
            }
        }

        // ---- ④-b `AM16 Scout Sentinel`：`fm = 2`（**只接线、不放大** —— 正文已给 `Draw 2`）----
        {
            var am16 = CardDatabase.FindById(pool, "AM16");
            CheckTrue(am16 != null, "(前提) `AM16 Scout Sentinel` 在池内");
            if (am16 != null)
            {
                var ops = am16.TriggerOps(KeywordTable.Duty);
                var op = ops != null && ops.Count == 1 ? ops[0] : null;
                CheckTrue(op != null && op.AbilityFurtherMultiplier == 2,
                          "★ `AM16` 的 `Draw 2 cards`（`Duty`）拿到 `fm = 2` —— 数据真的接到了 op 上"
                        + "（⛔ 不是「结算成 4 张」：正文已给 2，消费端只对账不乘）"
                        + "｜🧨 `fm` 一格没接 ⇒ 红（-1）");
                CheckTrue(op != null && op.Amount == 2,
                          "★ 同一条 op 的正文数值仍是 **2**（`fm` 不覆盖正文）"
                        + "｜🧨 在归位时把 `Amount` 也乘上去 ⇒ 红（那就成了平方）");
            }
        }

        // ---- ④-c 闸门「行数 ≠ op 数」：`SOR17 Sister Superior`（2 行 / 1 条 op）----
        {
            // `Pray: Deal 1 damage` 在原文里配 **2 行** logic（行0 `ta=30` 打伤害、行1 `ta=20`
            // 另一条 logic）。🔴 **2026-10-24 订正（铁律 5 · E1 把卡表推到 v24）**：
            // v23 那套「1 对象/1 行/1 条 op」闸在这里给的是 `gap:shape`；**v24 按能力种类归位后它挂上了**
            // （两行分属不同 `chosenAbility` 族，各挑各的 op ⇒ 不再「行数≠op数就放弃」）。
            // ⛔ 这里**不再断言具体 ta** —— `ta` 的真值由 E1 自己的逐卡表兜（`RuleEngineTest_EngineE1.cs`），
            // 本行只管「闸门形态变了没有」。
            var s17 = CardDatabase.FindById(pool, "SOR17");
            CheckTrue(s17 != null, "(前提) `SOR17 Sister Superior` 在池内");
            if (s17 != null)
            {
                Check(s17.ActiveAbilityMap ?? "(null)", "attached",
                      "★ `SOR17`（2 行 / 1 条 op）⇒ **`attached`**（v24 起按能力种类归位）"
                    + "｜🧨 退回 `gap:shape`（只认「1 对象/1 行/1 条 op」）⇒ 红");
                var ops = s17.TriggerOps(KeywordTable.Pray);
                bool anyMarked = false;
                if (ops != null) foreach (var o in ops) if (o != null && o.ActiveAbilityRow >= 0) anyMarked = true;
                CheckTrue(anyMarked,
                          "★ 且它那条 `pray` op **有** `ActiveAbilityRow` 标记（≥ 0）"
                        + "｜🧨 挂了结论却忘了标 op ⇒ 红");
            }
        }

        // ---- ④-d 闸门「gate=0 死数据」：`SAU37 Orikan the Diviner` ----
        {
            var sa = CardDatabase.FindById(pool, "SAU37");
            CheckTrue(sa != null && sa.ActiveAbilityLogic != null && sa.ActiveAbilityLogic.Count == 1,
                      "(前提) `SAU37 Orikan the Diviner` 有 1 个主动能力对象");
            if (sa != null && sa.ActiveAbilityLogic != null && sa.ActiveAbilityLogic.Count == 1)
            {
                Check(sa.ActiveAbilityLogic[0].Gate, 0,
                      "★ `SAU37` 的 active 对象 `gate = 0`（`hasActiveAbility = 0`，D11 §3·3 抽样）"
                    + "｜🧨 `gate` 读错键 / 生成器把死数据丢掉 ⇒ 红");
                Check(sa.ActiveAbilityMap ?? "(null)", "gate-0",
                      "★ 归位结论 = `gate-0`（本 build 死数据 ⇒ **不挂**，判据 "
                    + "`CardScript__ResolveActiveAbilityPlayed.c:22`）"
                    + "｜🧨 把 `gate=0` 也挂上去 ⇒ 红（那会让原版**不消费**的数据生效）");
            }
        }

        // ---- ④-e 闸门「卡上没有替代行动关键词」：`SOR66 Armorium Cherub` ----
        {
            // `SOR66` 的 `keywords` 只有 `Flying` / `Rally`（没有 duty/pray/ferocity/agenda/oath），
            // 而它的 active 行是 `gate=1` ⇒ v23 那套「必须挂在替代行动键下」的闸挂不上去（`gap:no-keyword`）。
            // 🔴 **2026-10-24 订正（铁律 5 · E1）**：v24 起改走「**整卡恰好一段正文**」闸
            // （`CardDef.cs:2424-2460`）⇒ 它**挂上了**（`Rally` 那条 `give` 拿 `ta=30 / fm=1`）。
            var s66 = CardDatabase.FindById(pool, "SOR66");
            CheckTrue(s66 != null, "(前提) `SOR66 Armorium Cherub` 在池内");
            if (s66 != null)
            {
                Check(s66.ActiveAbilityMap ?? "(null)", "attached",
                      "★ `SOR66`（gate=1、卡上无替代行动关键词、整卡一段正文）⇒ **`attached`**"
                    + "｜🧨 退回 `gap:no-keyword`（只认关键词那一档）⇒ 红");
                var ops66 = s66.TriggerOps(KeywordTable.Rally);
                bool marked66 = false;
                int ta66 = -1;
                if (ops66 != null)
                    foreach (var o in ops66)
                        if (o != null && o.ActiveAbilityRow >= 0) { marked66 = true; ta66 = o.SubjectTargetsAffected; }
                CheckTrue(marked66 && ta66 == 30,
                          "★ 且 `Rally` 那条 op 挂上 `ta = 30`（真值，E1 逐卡表）"
                        + "｜🧨 退回不挂 / 贴成 `-1` ⇒ 红");
            }
        }

        // ---- ④-f 两组一致（`SW21` 的 `active` 与 `alternative` 两行都是 `ta=10`）----
        {
            var sw = CardDatabase.FindById(pool, "SW21");
            CheckTrue(sw != null, "(前提) `SW21 Wulfen Pack Leader` 在池内");
            if (sw != null)
            {
                Check(sw.ActiveAbilityMap ?? "(null)", "attached",
                      "★ `SW21`（`ab=-1` / `ab=-2` 两组**逐格一致**）⇒ `attached`"
                    + "｜🧨 一见两组就放弃（要求只有一组）⇒ 红");
                var ops = sw.TriggerOps(KeywordTable.Ferocity);
                var op = ops != null && ops.Count == 1 ? ops[0] : null;
                CheckTrue(op != null && op.ActiveAbilityRow == 0 && op.SubjectTargetsAffected == 10,
                          "★ 且挂上的是那一行真值（`ta = 10`）"
                        + "｜🧨 拿 `ab=-2` 那组覆盖错 / 取反方向 ⇒ 红（现读两组同值，靠 ④-a 的灭自证兜底）");
            }
        }

        // ---- ④-g 汇总读数（⛔ 别只验一张卡就说「接完了」）----
        {
            int withRows = 0, attached = 0, gate0 = 0, gapKind = 0, other = 0;
            foreach (var c in pool)
            {
                if (c == null || c.ActiveAbilityLogic == null || c.ActiveAbilityLogic.Count == 0) continue;
                withRows++;
                switch (c.ActiveAbilityMap)
                {
                    case "attached": attached++; break;
                    case "gate-0": gate0++; break;
                    case "gap:kind-no-op":
                    case "gap:kind-ambiguous":
                    case "gap:kind-unknown": gapKind++; break;
                    default: other++; break;
                }
            }
            Check(withRows, 132, "★ 有行的卡 132 张都拿到了一个归位结论（没有一张是空串）");
            // 🔴 **2026-10-24 读数更新（铁律 5 · `E1` 把卡表推到 v24、按能力种类归位）**：
            //    v23 那套「1 对象 / 1 行 / 1 条 op」只挂 38 张；v24 按 `chosenAbility` 分族后 **84 张**。
            //    只剩 3 张给不出结论：`AM22`（`chooseone` 一条 op 盖两个分支 = `gap:kind-no-op`）·
            //    `SOR13`（同族两行 ta=10/120 = `gap:kind-ambiguous`）· `SW9`（`ca=320 followAfterResolution`
            //    故意不映射 = `gap:kind-unknown`）。
            Check(attached, 84,
                  "★ 挂上 op 的卡 = **84 张**（v24 按能力种类归位；v23 是 38）"
                + "｜🧨 退回「只认 1 对象/1 行/1 条 op」⇒ 掉到 38；按行号硬配 ⇒ 冲上 100+");
            Check(gapKind, 3,
                  "★ 给不出结论的 = **3 张**（`AM22` / `SOR13` / `SW9`，三类各一张，全是**真歧义**）"
                + "｜🧨 把歧义档也硬挂 ⇒ 掉到 0（那会静默贴错目标）");
            Check(gate0, 45, "★ `gate=0` 死数据 45 张（不挂）｜🧨 挂上去 ⇒ 掉到 0");
            Check(other, 0,
                  "★ 其余归位结论 0 张（老那几档 `gap:shape` / `gap:no-keyword` 今天已是空集）"
                + "｜🧨 出现这一类 = 归位逻辑分叉了，要回来看是不是两套闸并存");
            Check(attached + gapKind + gate0, 132,
                  "★ **灭自证**：四档加起来正好 132（每一张有行的卡都被归了一类，没有漏账）"
                + "｜🧨 有卡悄悄落进「既没挂也没归因」⇒ 加起来对不上 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ⑤ 选组（`ActiveAbilityGroupFor`）—— 数据取哪一组留给运行期闸
    // ==============================================================================================

    /// <summary>`RawCardScript__ResolveActiveAbility.c:18/26-31`：**走哪一组是运行期决定的**
    /// （`CardDef.UsesAlternativeAbility`），所以这里只验**两组都取得到**、`null` / 空表分得开。</summary>
    static void G12GroupSelection(List<CardDef> pool)
    {
        var sw = CardDatabase.FindById(pool, "SW21");
        CheckTrue(sw != null, "(前提) `SW21` 在池内");
        if (sw != null)
        {
            var normal = sw.ActiveAbilityGroupFor(false);
            var alt = sw.ActiveAbilityGroupFor(true);
            CheckTrue(normal != null && normal.Count == 1,
                      "★ `SW21.ActiveAbilityGroupFor(false)` = `activeAbility` 那组（1 个对象）"
                    + "｜🧨 `ab` 比较写反 ⇒ 本行与下一行**同时**红");
            CheckTrue(alt != null && alt.Count == 1,
                      "★ `SW21.ActiveAbilityGroupFor(true)` = `alternativeAbility` 那组（1 个对象）"
                    + "｜🧨 同上（`-1` / `-2` 混用就分不出两组）");
            CheckTrue(normal != null && normal[0].TargetsAffected != null
                      && normal[0].TargetsAffected.Length == 1 && normal[0].TargetsAffected[0] == 10,
                      "★ 普通支那一行的真值 = `ta = 10`（D11 §3·3 抽样）｜🧨 `ta` 读错字段 ⇒ 红");
        }

        // ---- 只有一组 / 一组都没有：**空表 ≠ null** ----
        var am5 = CardDatabase.FindById(pool, "AM5");
        CheckTrue(am5 != null, "(前提) `AM5 Shock Trooper` 在池内");
        if (am5 != null)
        {
            var alt = am5.ActiveAbilityGroupFor(true);
            CheckTrue(alt != null && alt.Count == 0,
                      "★ `AM5` 没有 `alternativeAbility` ⇒ `ActiveAbilityGroupFor(true)` = **空表**（⛔ 不是 null）"
                    + "｜🧨 让「没有这一组」也返回 null ⇒ 调用方会把它读成「整列没落 = 未知」");
            var normal = am5.ActiveAbilityGroupFor(false);
            CheckTrue(normal != null && normal.Count == 1,
                      "★ 而 `ActiveAbilityGroupFor(false)` 有 1 个对象｜🧨 返回反了 ⇒ 红");
        }

        // ---- 旧表：整列没落 ⇒ **null**（未知）----
        const string oldTable =
            "{\"version\":22,\"count\":1,\"cards\":[{\"id\":\"G12G1\",\"name\":\"G12 选组探针\","
          + "\"type\":\"unit\",\"desc\":\"\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\"}]}";
        CardDef old = null;
        try
        {
            var a = CardDatabase.Parse(oldTable);
            if (a.Count == 1) old = a[0];
        }
        finally { CreatePool.BuildNameIndex(pool); }
        CheckTrue(old != null, "(前提) 旧表探针卡解析出来了");
        if (old != null)
            CheckTrue(old.ActiveAbilityGroupFor(false) == null && old.ActiveAbilityGroupFor(true) == null,
                      "★ 旧表（整列没落）⇒ 两组**都是 null**（未知，⛔ 不是「没有这一组」的空表）"
                    + "｜🧨 给旧表返回空表 ⇒ 与上一条的语义并成一个 ⇒ 红");
    }
}
