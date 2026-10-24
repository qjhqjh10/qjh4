// RuleEngineTest_EngineE1.cs — 执行代理 `E1` 的断言宿主（2026-10-24）
//
// 覆盖三件（判据全在方法头的注释里）：
//   A. 卡表 **v24**：`activeAbilityLogic` 每行新增的 `chosenAbility` / `playersAffected` /
//      `damageCriteria` 三格（`ca` / `pa` / `dmg`）**读得进来**（读数 = 193 行、`ca` 分布、长度对齐）；
//   B. **版本守卫**：v23 旧表（没有这三格）⇒ `ChosenAbility/PlayersAffected/Damage = null = 未知`
//      ＋ 恰好一条 `< 24` 的 `LogWarning`，且归位退回 v23 那一档（⛔ 不许静默当成「没有种类」）；
//   C. **按能力种类归位**（`CardDef.AttachByAbilityKind`）：84 / 87 张 `gate=1` 的卡挂上了目标集，
//      并把「`cardAbilities` 的 `ta` 被贴到替代行动 op 上」那 14 处**来源错**从源头修掉
//      （`EffectText.Parse(..., alternativeActionBody: true)`）。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod EngineE1Assert.Run -logFile -
//
// 判绿红：看末行 `E1 断言合计: N 通过 / M 失败`；逐条按**行首** `E1 ✓` / `E1 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。任何 `E1 ✗` 都是**缺陷**，不是警告。
//
// 判据（按权威顺序）：
//   ① 全量反编译 `d:/2/tools/decomp_full/`：
//      `AbilityLogic__PlayAbility.c` 的第一层 `switch (*(int *)(param_1 + 0x10))`
//      —— 按 `chosenAbility` 分派（`:1461 case 10` 伤害族、`:1805-1934` 的 `case 0x1cc..0x1db`
//      = `AddTriggerMob/Pray/Ambush/Uprising/Slay/Teleport`，`:2641+ case 0xe6..` 另一批）；
//      `AbilityLogic.cs:9` `public AbilityEffect chosenAbility;`
//   ② 枚举表 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`（`10 doDamage` / `20 doHeal` /
//      `30 buffOthers` / `40 buffSelf` / `60 buffMana` / `70 summonUnit` / `462 triggerPray` …）
//      ＋ `PlayersAffected.cs`（`Both=0` / `Self=10` / `Enemy=20`）。
//   ③ 数据侧存量（v23 口径）→ `资料/普查产出_1024/D11_activeAbility列.md`；
//      v24 的口径与逐张 before→after → `资料/普查产出_1024b/E1_chosenAbility与ta来源.md`。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量（84 / 45 / 30 / 100 …），⛔ 不从实现常量反算（那就是自证）。
// ⚠️ 本宿主**不建场景、不存盘**；合成探针卡跑完用真池重建名字索引（同 `G12` 的做法）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

public static class EngineE1Assert
{
    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();

    /// <summary>唯一入口（主对话挂在宿主里跑这一条）。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        var pool = CardDatabase.Load();
        if (pool == null || pool.Count == 0)
        {
            Debug.LogError("E1 ✗ (前提) 卡池装载失败 —— 后面的断言全部没有意义");
            Summary();
            return;
        }
        try
        {
            Section("A · 卡表 v24：`chosenAbility` / `playersAffected` / `damageCriteria` 的读数");
            E1KindColumnsReadings(pool);
            Section("B · 版本守卫：v23 旧表 ⇒ 三格 null + 恰好一条 `< 24` 警告 + 退回 v23 归位");
            E1VersionGuard(pool);
            Section("C · 按能力种类归位（84 / 87）与 `cardAbilities` 来源错的修复");
            E1Attribution(pool);
        }
        finally
        {
            Summary();
        }
    }

    // ==========================================================================================
    //  A · 三个新格子的读数
    // ==========================================================================================

    /// <summary>193 行全部带 `ca` / `pa` / `dmg`，且**逐格对齐** `ta`/`fm`；分布与生成器现读一致。</summary>
    static void E1KindColumnsReadings(List<CardDef> pool)
    {
        Check(pool.Count, 1127, "(前提) 卡池 1127 张");

        int rows = 0, caMissing = 0, lenBad = 0, dmgUnknown = 0;
        int ca10 = 0, ca30 = 0, ca462 = 0, ca0 = 0;
        int paSelf = 0, paUnknown = 0;
        int badKind = 0;
        foreach (var c in pool)
        {
            if (c == null || c.ActiveAbilityLogic == null) continue;
            foreach (var r in c.ActiveAbilityLogic)
            {
                if (r == null) continue;
                int n = r.LogicCount;
                rows += n;
                if (r.ChosenAbility == null) { caMissing += n; continue; }
                if (r.ChosenAbility.Length != n) { lenBad++; continue; }
                if (r.PlayersAffected == null || r.PlayersAffected.Length != n) lenBad++;
                if (r.Damage == null || r.Damage.Length != n) lenBad++;
                for (int j = 0; j < n; j++)
                {
                    int ca = r.ChosenAbility[j];
                    if (ca < 0) badKind++;
                    if (ca == 10) ca10++;
                    else if (ca == 30) ca30++;
                    else if (ca == 462) ca462++;
                    else if (ca == 0) ca0++;
                    if (r.PlayersAffected != null && j < r.PlayersAffected.Length)
                    {
                        if (r.PlayersAffected[j] == 10) paSelf++;
                        else if (r.PlayersAffected[j] < 0) paUnknown++;
                    }
                    if (r.Damage != null && j < r.Damage.Length && r.Damage[j] != null && r.Damage[j].IsUnknown)
                        dmgUnknown++;
                }
            }
        }

        Check(rows, 193,
              "★ `activeAbilityLogic` 的 logic 行合计 = **193**（v23 就是 193，加格不改行数）"
            + "｜🧨 生成器把 `ca`/`pa`/`dmg` 写成了**新行**而不是**每行加格** ⇒ 行数变 ⇒ 红");
        Check(caMissing, 0,
              "★ **每一行都带 `chosenAbility`**（v24 表；`null` 只该出现在旧表）"
            + "｜🧨 生成器漏写 `ca` / DTO 字段名与 JSON 键名不一致 ⇒ 红（且第 C 段会掉回 v23 那 38 张）");
        Check(lenBad, 0,
              "★ `ca` / `pa` / `dmg` 与 `ta` / `fm` **逐格同长**"
            + "｜🧨 三格少写一格 / 生成器对坏值 `continue` 而不是 `append(-1)` ⇒ 红");
        Check(badKind, 0,
              "★ `chosenAbility` 没有 `< 0` 的格（`-1` 只是坏值哨兵；现池 0 处）"
            + "｜🧨 生成器把缺键写成 `-1` 而不是出声 ⇒ 红");
        Check(ca10, 70, "★ `ca = 10`（`doDamage`）**70 行**｜🧨 生成器取错键（`chosenAbility` vs 别的）⇒ 红");
        Check(ca30, 27, "★ `ca = 30`（`buffOthers`）**27 行**｜🧨 同上");
        Check(ca462, 31,
              "★ `ca = 462`（`triggerPray`）**31 行** —— 全是 `SOR*` 的 `Pray:` 卡"
            + "（判据 `AbilityLogic__PlayAbility.c:1838` `case 0x1ce` → `BattleManager__AddTriggerPray`）"
            + "｜🧨 把它当正文行参与配 op ⇒ 第 C 段的 `give`/`deal` 会挂上 `ta=20` ⇒ 红");
        Check(ca0, 7, "★ `ca = 0`（`drawCard`）**7 行**｜🧨 同上");
        Check(paSelf, 31,
              "★ `playersAffected = 10`（`Self`，`PlayersAffected.cs`）**31 格**"
            + "｜🧨 生成器取错字段 / 把 `Both(0)` 当 Self ⇒ 红");
        Check(paUnknown, 0, "★ `playersAffected` 没有 `< 0` 的格｜🧨 坏值静默落 0 ⇒ 红");
        Check(dmgUnknown, 0,
              "★ `damageCriteria` 每一格都读到了（`IsUnknown == false`，现池 193 行全是 dict）"
            + "｜🧨 生成器漏 `damageCriteria` / DTO 键名不一致 ⇒ 红");
    }

    // ==========================================================================================
    //  B · 版本守卫（v23 旧表）
    // ==========================================================================================

    /// <summary>同一张合成卡、同一段正文，**只差版本与三个新格**：
    /// v23 ⇒ 三格 `null`、归位退回 `gap:shape`、正文 op **不挂** `cardAbilities` 的 `ta`；
    /// v24 ⇒ 三格有值、归位 `attached`、`ta = 30`。</summary>
    static void E1VersionGuard(List<CardDef> pool)
    {
        const string head =
            "{\"version\":%V%,\"count\":1,\"cards\":[{\"id\":\"%ID%\",\"name\":\"%NAME%\","
          + "\"type\":\"unit\",\"desc\":\"Pray: Deal 2 damage to an enemy\",\"rarity\":\"Common\","
          + "\"faction\":\"Ultramarines\",\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,"
          + "\"subtype\":\"Infantry\",\"keywords\":[\"Pray\"],\"cardTraits\":[],"
          + "\"activeAbilityLogic\":[{\"ab\":-1,\"gate\":1,\"trigger\":400,\"needsLoading\":0,"
          + "\"ta\":[30,20],\"fm\":[0,1]%EXTRA%}],"
          + "\"cardAbilities\":[{\"trigger\":0,\"abilityLogic\":[{\"furtherMultiplier\":0}]}]}]}";
        string v23Json = head.Replace("%V%", "23").Replace("%ID%", "E1V23").Replace("%NAME%", "E1 旧表探针")
                             .Replace("%EXTRA%", "");
        string v24Json = head.Replace("%V%", "24").Replace("%ID%", "E1V24").Replace("%NAME%", "E1 新表探针")
                             .Replace("%EXTRA%",
                                      ",\"ca\":[10,462],\"pa\":[0,0],"
                                    + "\"dmg\":[{\"min\":2,\"max\":2,\"opt\":0,\"type\":0},"
                                    + "{\"min\":1,\"max\":1,\"opt\":0,\"type\":0}]");

        int oldWarns = 0, newWarns = 0;
        string firstOld = null;
        bool countingOld = false, countingNew = false;
        Application.LogCallback cb = (text, stack, type) =>
        {
            if (type != LogType.Warning || text == null) return;
            if (!text.Contains("chosenAbility")) return;          // 只数 v24 那条守卫
            if (countingOld) { oldWarns++; if (firstOld == null) firstOld = text; }
            if (countingNew) newWarns++;
        };
        CardDef oldCard = null, newCard = null;
        Application.logMessageReceived += cb;
        try
        {
            countingOld = true;
            var a = CardDatabase.Parse(v23Json);
            countingOld = false;
            if (a.Count == 1) oldCard = a[0];
            countingNew = true;
            var b = CardDatabase.Parse(v24Json);
            countingNew = false;
            if (b.Count == 1) newCard = b[0];
        }
        finally
        {
            countingOld = countingNew = false;
            Application.logMessageReceived -= cb;
            CreatePool.BuildNameIndex(pool);   // 合成卡名并进了索引 ⇒ 复原，别污染后面的用例
        }

        CheckTrue(oldCard != null && newCard != null, "(前提) 两张合成探针卡都解析出来了");
        if (oldCard == null || newCard == null) return;

        // ---- v23：三格 null = 未知 ----
        {
            bool anySpec = false, caNull = true, paNull = true, dmgNull = true;
            if (oldCard.ActiveAbilityLogic != null)
                foreach (var r in oldCard.ActiveAbilityLogic)
                {
                    if (r == null) continue;
                    anySpec = true;
                    if (r.ChosenAbility != null) caNull = false;
                    if (r.PlayersAffected != null) paNull = false;
                    if (r.Damage != null) dmgNull = false;
                }
            CheckTrue(anySpec, "(前提) v23 探针卡有 1 个能力对象");
            CheckTrue(caNull,
                      "★ v23（没有 `ca` 键）⇒ `ChosenAbility = null = 未知`（⛔ 不是「每行都没有种类」）"
                    + "｜🧨 DTO 里把缺键读成空数组 / 填 0 ⇒ 红");
            CheckTrue(paNull && dmgNull,
                      "★ 同理 `PlayersAffected` / `Damage` 都是 `null`"
                    + "｜🧨 同上（三者必须同口径）");
            Check(oldCard.ActiveAbilityMap ?? "(null)", "gap:shape",
                  "★ 没有种类 ⇒ 退回 v23 那一档：2 行 / 1 条 op ⇒ `gap:shape`（op 保持 `-1`）"
                + "｜🧨 在「没有种类」时硬按行号配 ⇒ 会变成 `attached` 且贴错目标 ⇒ 红");
            var ops = oldCard.TriggerOps(KeywordTable.Pray);
            var op = ops != null && ops.Count == 1 ? ops[0] : null;
            CheckTrue(op != null, "(前提) v23 探针卡的 `Pray:` 正文解析出 1 条 op");
            CheckTrue(op != null && op.SubjectTargetsAffected == -1,
                      "★ **来源错已从源头修掉**：替代行动正文的 op **不再**从 `cardAbilities` 取 `ta`"
                    + "（改前这里会是 `cardAbilities[0]` 的值）"
                    + "｜🧨 把 `EffectText.Parse(..., alternativeActionBody:)` 那一行撤掉 ⇒ 红");
        }

        // ---- v24：三格有值 + `attached` ----
        {
            CheckTrue(newCard.ActiveAbilityLogic != null && newCard.ActiveAbilityLogic.Count == 1
                      && newCard.ActiveAbilityLogic[0].ChosenAbility != null,
                      "★ v24 ⇒ `ChosenAbility` 有值"
                    + "｜🧨 `MapActiveAbilityLogic` 漏映射 `ca` ⇒ 红");
            CheckTrue(newCard.ActiveAbilityLogic[0].ChosenAbility.Length == 2
                      && newCard.ActiveAbilityLogic[0].ChosenAbility[0] == 10
                      && newCard.ActiveAbilityLogic[0].ChosenAbility[1] == 462,
                      "★ 逐格值对：`[10 doDamage, 462 triggerPray]`"
                    + "｜🧨 数组错位（`ca` 落到别的对象上）⇒ 红");
            CheckTrue(newCard.ActiveAbilityLogic[0].Damage != null
                      && newCard.ActiveAbilityLogic[0].Damage[0] != null
                      && newCard.ActiveAbilityLogic[0].Damage[0].Min == 2
                      && newCard.ActiveAbilityLogic[0].Damage[0].Max == 2
                      && !newCard.ActiveAbilityLogic[0].Damage[0].IsUnknown,
                      "★ `dmg` 逐格映射对：第一行 `min=max=2`"
                    + "｜🧨 `min`/`max`/`opt`/`type` 键名对调 ⇒ 红");
            Check(newCard.ActiveAbilityMap ?? "(null)", "attached",
                  "★ 有种类 ⇒ 按种类配：`deal` 那一条挂上（`triggerPray` 那行找不到 `triggerability` op ⇒ 不挂）"
                + "｜🧨 把 `triggerPray` 也映射到 `deal`/`give` ⇒ 红");
            var ops = newCard.TriggerOps(KeywordTable.Pray);
            var op = ops != null && ops.Count == 1 ? ops[0] : null;
            CheckTrue(op != null && op.SubjectTargetsAffected == 30 && op.ActiveAbilityRow == 0,
                      "★ 挂的是第 0 行（`ta=30`），**不是**第 1 行的 `ta=20`（`triggerPray` 那行）"
                    + "｜🧨 按行号配 / 把 462 也当正文行 ⇒ 红");
        }

        // ---- 守卫出声 ----
        Check(oldWarns, 1,
              "★ **v23 旧表出声**：正好一条点名 `chosenAbility` 的 `LogWarning`"
            + "｜🧨 删掉 `CardDatabase.Parse` 里那条 `< 24` 守卫 ⇒ 掉到 0");
        CheckTrue(firstOld != null && firstOld.Contains("null") && firstOld.Contains("-1"),
                  "★ 那条警告写清了后果（`null = 未知` / `-1 = 读不到`），⛔ 不是一句「版本低」"
                + "｜🧨 改成没有后果说明的空话 ⇒ 红");
        Check(newWarns, 0,
              "★ v24 表**不出声**｜🧨 守卫写成 `<= 24` / 无条件出声 ⇒ 红（正常装载会刷屏）");
    }

    // ==========================================================================================
    //  C · 按能力种类归位（真卡）
    // ==========================================================================================

    static void E1Attribution(List<CardDef> pool)
    {
        // ---- C-a 汇总：132 张有行的卡，每一张都有结论；84 张挂上 ----
        {
            int withRows = 0, attached = 0, gate0 = 0, noOp = 0, ambiguous = 0, unknown = 0, other = 0, emptyMap = 0;
            foreach (var c in pool)
            {
                if (c == null || c.ActiveAbilityLogic == null || c.ActiveAbilityLogic.Count == 0) continue;
                withRows++;
                string m = c.ActiveAbilityMap ?? "";
                if (m.Length == 0) { emptyMap++; continue; }
                if (m == "attached") attached++;
                else if (m == "gate-0") gate0++;
                else if (m == "gap:kind-no-op") noOp++;
                else if (m == "gap:kind-ambiguous") ambiguous++;
                else if (m == "gap:kind-unknown") unknown++;
                else other++;
            }
            Check(withRows, 132, "★ 有 `activeAbilityLogic` 行的卡 = **132 张**（D11 现读）");
            Check(emptyMap, 0,
                  "★ **没有一张卡有行却没有结论**（⛔ 不许静默）"
                + "｜🧨 某个闸门直接 `return` 却不写 `ActiveAbilityMap` ⇒ 红");
            Check(attached, 84,
                  "★ **挂到 op 上的 = 84 张**（v23 只挂 38 张；余下 46 张按种类挂上）"
                + "｜🧨 种类表漏一族 / 相邻派生行没排除 ⇒ 掉下去 ⇒ 红");
            Check(gate0, 45, "★ `gate=0`（本 build 死数据）**45 张**（D11 现读，不变）");
            Check(noOp + ambiguous + unknown + other, 3,
                  "★ 仍然读不到 = **3 张**（`AM22` 的 `chooseone` 1 条 op 盖两个分支 · "
                + "`SOR13` 同族两行不同 `ta` · `SW9` 的 `followAfterResolution(320)` 未映射）"
                + "｜🧨 把这三张硬配上去（按行号挑第一行）⇒ 本行仍绿但 C-c 的反例会红");
            Check(attached + gate0 + noOp + ambiguous + unknown + other, 132,
                  "★ **灭自证**：84 + 45 + 3 = 132（每一张有行的卡都落在某一档里）"
                + "｜🧨 有卡既没挂也没归因（例如某闸门吞掉）⇒ 加起来不等于 132 ⇒ 红");
        }

        // ---- C-b 逐张：正例（每一张都钉「哪个 op / 哪个字段 / 值」）----
        {
            // `AM38 Baneblade Tank`：`Duty: Deal 8 damage` ⇒ 行0 `ta=30`（`cardAbilities[0]` 是 100）
            var c = CardDatabase.FindById(pool, "AM38");
            var op = OneOp(c, KeywordTable.Duty);
            CheckTrue(c != null && c.ActiveAbilityMap == "attached", "(前提) `AM38` = `attached`");
            CheckTrue(op != null && op.SubjectTargetsAffected == 30 && op.AbilityFurtherMultiplier == 0,
                      "★ `AM38`/duty：`ta 30` · `fm 0`（行0）｜🧨 种类族把 `10` 映射错 ⇒ 红");
            int cardAbilTa = CardAbilTa0(c);
            Check(cardAbilTa, 100,
                  "★ **灭自证**：`AM38` 自己的 `cardAbilities[0].ta = 100`（相邻 +1 护甲），与 30 不同"
                + "｜🧨 「两个来源混用」的实现不可能同时满足这两条");
        }
        {
            // `AM28 Chimera`：`Duty: Deploy two Shock Trooper` ⇒ 2 行都是 `summonUnit(70) ta=0`
            // **改前**：op 上挂着 `cardAbilities[0].ta = 10`（来源错，G12 §七·1）
            var c = CardDatabase.FindById(pool, "AM28");
            var op = OneOp(c, KeywordTable.Duty);
            CheckTrue(c != null && c.ActiveAbilityMap == "attached", "(前提) `AM28` = `attached`");
            CheckTrue(op != null && op.SubjectTargetsAffected == 0,
                      "★ `AM28`/duty：`deploy` 的 `ta = 0`（原版 `any`）—— **改前是 10（抄自 `cardAbilities`）**"
                    + "｜🧨 片段分支那行 `if (alternativeActionBody) return;` 被删 ⇒ 回到 10 ⇒ 红");
            CheckTrue(op != null && op.ActiveAbilityRow == 0,
                      "★ 两行同族、值相同 ⇒ 取第 0 行｜🧨 取第 1 行 / 不记行号 ⇒ 红");
        }
        {
            // `SOR6 Battle Sister Gunner`：`Pray: Deal 2 damage to an enemy` ⇒ `deal` `ta=30` `fm=1`
            // **改前**：`ta=20`（`cardAbilities[0]`）
            var c = CardDatabase.FindById(pool, "SOR6");
            var op = OneOp(c, KeywordTable.Pray);
            CheckTrue(op != null && op.SubjectTargetsAffected == 30 && op.AbilityFurtherMultiplier == 1,
                      "★ `SOR6`/pray：`ta = 30` · `fm = 1`（**改前 `ta=20`**）"
                    + "｜🧨 同上（片段分支回退）／种类族漏 `10` ⇒ 红");
        }
        {
            // `SOR25 Imagifier`：3 行（30/30/462）⇒ 两个 `buffOthers` 行**值相同**才能挂
            var c = CardDatabase.FindById(pool, "SOR25");
            var op = OneOp(c, KeywordTable.Pray);
            CheckTrue(op != null && op.SubjectTargetsAffected == 30,
                      "★ `SOR25`/pray：`give` 的 `ta = 30`（同族两行 `ta` 都是 30）"
                    + "｜🧨 把 `462` 也当正文行（它 `ta=20`）⇒ 变成歧义 ⇒ 红");
        }
        {
            // `SOR66 Armorium Cherub`：无替代行动关键词，唯一「只有一段正文」的例外
            var c = CardDatabase.FindById(pool, "SOR66");
            CheckTrue(c != null && c.ActiveAbilityMap == "attached",
                      "★ `SOR66`（卡上没有 `Duty/Pray/…`）⇒ **也挂上了**（唯一一段正文 = `Rally:`）"
                    + "｜🧨 把「无替代行动关键词 ⇒ `gap:no-keyword`」写死 ⇒ 红");
            var ops = c != null ? c.TriggerOps(KeywordTable.Rally) : null;
            var op = ops != null && ops.Count == 1 ? ops[0] : null;
            CheckTrue(op != null && op.SubjectTargetsAffected == 30 && op.AbilityFurtherMultiplier == 1,
                      "★ `SOR66`/rally：`give +2 [Ranged]` 的 `ta = 30` · `fm = 1`"
                    + "｜🧨 例外闸放宽成「任意一个关键词」⇒ 会挂到别的卡上（现池只有这 1 张）");
        }
        {
            // `AM23 Bombast Field Gun`：1 条 op / 2 行（`ta=30` ＋ `ta=110 adjacentToPreviousTarget`）
            var c = CardDatabase.FindById(pool, "AM23");
            var op = OneOp(c, KeywordTable.Duty);
            CheckTrue(op != null && op.SubjectTargetsAffected == 30,
                      "★ `AM23`：主目标 `ta = 30`；**相邻那行 `ta=110` 不参与**（`AdjacentAnchor` 派生集合）"
                    + "｜🧨 不排除派生行 ⇒ 变歧义（两行不同 `ta`）⇒ 这一条红");
            CheckTrue(c != null && c.ActiveAbilityLogic[0].TargetsAffected[1] == 110,
                      "★ **灭自证**：第 1 行确实是 `ta = 110`（数据里有），不是我们没读到"
                    + "｜🧨 生成器把 `ta` 读成单值 / 少收一行 ⇒ 红");
        }
        {
            // `AM35 Leman Russ Vanquisher`：2 行 `doDamage` 值相同（`ta=90`/`fm=0`）、2 条 `deal`
            var c = CardDatabase.FindById(pool, "AM35");
            var ops = c != null ? c.TriggerOps(KeywordTable.Duty) : null;
            int n = ops == null ? 0 : ops.Count;
            int both = 0;
            if (ops != null) foreach (var o in ops) if (o != null && o.SubjectTargetsAffected == 90) both++;
            Check(n, 2, "(前提) `AM35` 的 `Duty:` 正文解析出 2 条 `deal`（`8` / `12`）");
            Check(both, 2,
                  "★ `AM35`：同族两行 `(ta,fm)` 相同 ⇒ **两条 op 都挂 90**"
                + "｜🧨 同族多行一律判歧义 ⇒ 掉到 0 ⇒ 红（那一档会白丢目标集）");
        }
        {
            // `AM59 Astropath`：两族（`drawCard(0)` / `buffOthers(30)`）各配一条 op
            var c = CardDatabase.FindById(pool, "AM59");
            var ops = c != null ? c.TriggerOps(KeywordTable.Duty) : null;
            int n = ops == null ? 0 : ops.Count;
            Check(n, 2, "(前提) `AM59` 的 `Duty:` 正文 = `drawtype` + `lowercost`");
            CheckTrue(n == 2 && ops[0].SubjectTargetsAffected == 65 && ops[1].SubjectTargetsAffected == 150,
                      "★ `AM59`：`drawtype ← ta=65`（行0）· `lowercost ← ta=150`（行1）"
                    + "｜🧨 不按族分（按行号直配）或漏 `lowercost` 这个动词 ⇒ 红");
        }
        {
            // `UM105 Dreadnought`（oath）：1 条 op / 2 行，主目标 30，相邻 110
            var c = CardDatabase.FindById(pool, "UM105");
            var ops = c != null ? c.OathOps : null;
            var op = ops != null && ops.Count == 1 ? ops[0] : null;
            CheckTrue(op != null && op.SubjectTargetsAffected == 30,
                      "★ `UM105`/oath：`deal` 的 `ta = 30`（`oath` 走 `OathOps`，同一套种类归位）"
                    + "｜🧨 只对 `TriggerOps` 做归位、漏 `OathOps` ⇒ 红");
        }

        // ---- C-c 反例：这三张**必须仍然读不到**（⛔ 不许靠「挑第一行」蒙过去）----
        {
            var c = CardDatabase.FindById(pool, "SOR13");
            CheckTrue(c != null && c.ActiveAbilityMap == "gap:kind-ambiguous",
                      "★ `SOR13`（同族两行 `ta=10`/`120` ≠ 两条 `give` op）⇒ `gap:kind-ambiguous`"
                    + "｜🧨 放宽成「同族挑第一个」⇒ 变成 `attached` ⇒ 红（那条路会静默贴错目标）");
            var ops = c != null ? c.TriggerOps(KeywordTable.Pray) : null;
            int unset = 0;
            if (ops != null) foreach (var o in ops) if (o != null && o.SubjectTargetsAffected == -1) unset++;
            Check(unset, 2, "★ 且那两条 `give` 的 `ta` 仍是 `-1`（读不到）｜🧨 挂上去 ⇒ 红");
        }
        {
            var c = CardDatabase.FindById(pool, "AM22");
            CheckTrue(c != null && c.ActiveAbilityMap == "gap:kind-no-op",
                      "★ `AM22`（1 条 `chooseone` op 里盖着伤害 + 增益两个分支）⇒ `gap:kind-no-op`"
                    + "｜🧨 把 `chooseone` 塞进任何一族 ⇒ 红（一条 op 两个目标集，贴一个是错的）");
            var ops = c != null ? c.TriggerOps(KeywordTable.Duty) : null;
            CheckTrue(ops != null && ops.Count == 1 && ops[0].SubjectTargetsAffected == -1,
                      "★ 那条 `chooseone` 的 `ta` 仍是 `-1`｜🧨 同上");
        }
        {
            var c = CardDatabase.FindById(pool, "SW9");
            CheckTrue(c != null && c.ActiveAbilityMap == "gap:kind-unknown",
                      "★ `SW9`（`chosenAbility = 320 followAfterResolution`，故意不映射）⇒ `gap:kind-unknown`"
                    + "｜🧨 把它硬映射到 `forceattack` ⇒ 红（那不是「查到了」，是猜）");
            var ops = c != null ? c.TriggerOps(KeywordTable.Ferocity) : null;
            CheckTrue(ops != null && ops.Count == 2 && ops[0].SubjectTargetsAffected == -1,
                      "★ 那条 `forceattack` 的 `ta` 仍是 `-1`｜🧨 同上");
        }
    }

    // ---- 小工具 ----

    /// <summary>`card` 的那个关键词正文**恰好一条** op 时返回它，否则 `null`。</summary>
    static EffectOp OneOp(CardDef card, string kw)
    {
        if (card == null) return null;
        var ops = kw == KeywordTable.Oath ? card.OathOps : card.TriggerOps(kw);
        return ops != null && ops.Count == 1 ? ops[0] : null;
    }

    /// <summary>`cardAbilities[0]` 的第一条 `ta`（`-1` = 没有）—— 用来做「灭自证」（另一条来源的值）。</summary>
    static int CardAbilTa0(CardDef card)
    {
        if (card == null || card.AbilityTargets == null || card.AbilityTargets.Count == 0) return -1;
        var a = card.AbilityTargets[0];
        if (a == null || a.TargetsAffected == null || a.TargetsAffected.Length == 0) return -1;
        return a.TargetsAffected[0];
    }

    static void Section(string title) { Debug.Log($"E1 --- {title} ---"); }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want))
        {
            _pass++;
            Debug.Log($"E1 ✓ {msg}");
        }
        else
        {
            _fail++;
            string line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError($"E1 ✗ {line}");
        }
    }

    static void CheckTrue(bool cond, string msg) { Check(cond, true, msg); }

    static void Summary()
    {
        Debug.Log($"E1 断言合计: {_pass} 通过 / {_fail} 失败");
        for (int i = 0; i < _failures.Count; i++)
            Debug.LogError($"E1 失败[{i + 1}] {_failures[i]}");
    }
}
