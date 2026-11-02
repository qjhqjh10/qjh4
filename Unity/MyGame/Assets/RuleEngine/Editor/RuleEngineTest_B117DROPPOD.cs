// RuleEngineTest_B117DROPPOD.cs — 执行代理 `B117_WDROPPOD`（2026-11-02 · 第一百一十七会话 · 引擎线）
//   文件所有权 = `RuleEngine/Core/UnitStatePartial_B117DROPPOD.cs`（新建）＋ 本文件（新建）＋ `判据/B117-WDROPPOD_dropPod支.md`
//
// ============================ 它盯的是什么 ============================
//  活状态表 `B117-47`：原版 `CardScript__ActivateTraitsOnSummonOrEnchantment.c:70-78` 的 **`0xe6`（dropPod）支**
//  我们没实现。本批补上，断言分两段：
//    · **D 段**（单位级）：新方法 `UnitState.ApplyDropPodOnSummon()` 的语义 —— 这一段**现在就该绿**。
//    · **W 段**（接线）：真入口 `RuleCore.ApplyDeployTurnState` / `ActivateTraitsOnSummonOrEnchantment`
//      到底有没有把那一跳接上。🔴 **W 段在接线补丁落地前会红**（补丁写在 `RuleCore.cs`，本批由 `WREPLAY2` 占用）
//      —— 红 = 补丁没应用，**不是** D 段那个方法写错。补丁全文 → `资料/完全复刻/判据/B117-WDROPPOD_dropPod支.md` §六。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/CardScript__ActivateTraitsOnSummonOrEnchantment.c`
//       `:70-74` `iVar1 = *(int *)(lVar6 + 0x18); cVar7 = 0; if (iVar1 == 0xe6) cVar7 = param_2;`
//         ⇒ **dropPod 支只在 `onSummon(实参2) != 0` 时进**；附魔那条路（`param_2 == 0`）不进。
//       `:102-135` 支体：`+0xcc..+0xdc ← GameStaticData.dropPodMaxHealth(+0x1fc, 值 2)` ·
//         `+0xe0..+0xf0 ← GameStaticData.dropPodAttack(+0x1f8, 值 0)` · 末尾 `UpdateFigures`。
//   · `d:/2/tools/decomp_full/CardScript__CardSetup.c:158-160`：**另一处**写点带 `cardType == 10` 闸
//     （⇒ 只有督军在「建卡」那一刻拿初值；**普通单位的池子只能靠本支初始化**）—— 这就是 D3 在验的事。
//   · 常量真值 = `cpp2il/out_cpp2il_dev_ilspy/GameStaticData.cs:2404-2405`（`dropPodAttack = 0; dropPodMaxHealth = 2;`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · D 段：把 `ApplyDropPodOnSummon` 的 `if (!HasCurrentTrait(DropPod)) return false;` 删掉 ⇒ **D2 红**
//     （不带 dropPod 的单位也会被刷成 2/0）。
//   · D 段：把两句赋值写反（health ← 0 / attack ← 2）⇒ **D1 红**（原版 `+0xcc` 拿 `+0x1fc`、`+0xe0` 拿 `+0x1f8`）。
//   · D 段：把方法改成「只在池子为 0 时刷」（= 一次性初始化而不是每次召唤重刷）⇒ **D1′ 红**（先把池改成 2/7 再调）。
//   · D3：把 `SetupDropPodStats` 的 `cardType == CardTypeHero` 闸去掉 ⇒ **D3 红**（非督军构造后也变成 2/0）。
//   · W 段：不接 `RuleCore.cs` 那一句 ⇒ **W1/W3 红**（这正是它存在的理由）；把 `onSummon` 那一半忽略掉
//     （附魔路也刷）⇒ **W4′**（补丁落地后建议补，见报告 §六·末尾）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()`。🔴 **该文件本波由 `B117-WRED-RE` 占用** ⇒
//  本代理**没有**加这一行。请在任一条既有 `Guard(...)` 的**同一物理行**上追加（零行数变化）：
//      Guard("B117DROPPOD_Assert", B117DROPPOD_Assert.Run); _pass += B117DROPPOD_Assert.Pass; _fail += B117DROPPOD_Assert.Fail;
//  ⚠️ **没接上 = 一条都不跑**（同样别当成已验收）。
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B117_WDROPPOD`：`B117-47` —— `ActivateTraitsOnSummonOrEnchantment` 的 `0xe6`（dropPod）支。</summary>
public static class B117DROPPOD_Assert
{
    const string P = "[B117DROPPOD] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            SectionD_Helper();
            SectionW_Wiring();
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B117DROPPOD 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================ 夹具
    //  ⚠️ `Exhausted` 不必置 —— 本支只读写空投舱那两格，与疲劳/攻击位无关。
    //  ⚠️ 阵营别写 `"Test"` 给**要进真部署链**的卡（`CLAUDE.md` 记过：残骸/阵营相关判据扫不到）——
    //     这里不跑死亡链，用 `"Test"` 只求构造简单；W 段也只用 `ApplyDeployTurnState`（不查阵营）。
    static CardDef Unit(string id, params string[] keywords)
    {
        return new CardDef(id, id, "unit", "", null, "Test", 1, 2, 5, 0, keywords: keywords);
    }

    static CardDef Hero(string id, params string[] keywords)
    {
        return new CardDef(id, id, "hero", "", null, "Test", 0, 0, 30, 0, keywords: keywords, subtype: "Warlord");
    }

    // 🔴 2026-11-02（`B117-FIXG` 就地订正）：**夹具必须把原版 `cardType` 挂上** —— `CardDef.CardType` 缺省
    //   是 `GateUnknown(-1)`（只有 `CardDatabase.Parse` / 自检自己 `AttachCardType` 才写）。D3⁗ 期望
    //   「督军构造时就是 2」，而 `SetupDropPodStats` 的第一道闸是 `Card.CardType != CardTypeHero` ⇒
    //   cardType 留 -1 时**在闸上就返回了**（`HasCurrentTrait` 根本轮不到）⇒ 恒 0。这不是实现错。
    //   同族先例 = `CardPresentation/Editor/B97W22_Assert.cs:93`（那边的 `Hero()` 就是 `AttachCardType(10)`）。
    static UnitState Make(CardDef card) { if (card.CardType < 0) card.AttachCardType(card.Subtype == "Warlord" ? CardDef.CardTypeHero : CardDef.CardTypeMinion); return new UnitState(card, false); }

    // ============================================================
    //  D · 单位级：`ApplyDropPodOnSummon` 的语义（原版 `.c:70-78` + `:102-135`）
    // ============================================================
    static void SectionD_Helper()
    {
        Debug.Log(P + "--- D · `UnitState.ApplyDropPodOnSummon()`（原版 `.c:70-78` / `:102-135`）---");

        // ---- D0 前提：夹具真的把 `droppod` 挂上去了（否则后面全是空转绿）
        {
            var with = Make(Unit("B117DROPPOD_D0a", KeywordTable.DropPod));
            var without = Make(Unit("B117DROPPOD_D0b"));
            Ok(with.HasCurrentTrait(KeywordTable.DropPod), "D0 **前提**：带 `droppod` 的夹具 `HasCurrentTrait` 为真");
            Ok(!without.HasCurrentTrait(KeywordTable.DropPod), "D0′ **前提**：不带的那张 `HasCurrentTrait` 为假");
        }

        // ---- D1：带 dropPod ⇒ 刷成 GameStaticData 的默认（2 / 0）
        //      ⚠️ 先把池子改成「已被打过两下 + 攻击被加过」的形状，才能证明这是**重刷**而不是「本来就是默认值」
        {
            var u = Make(Unit("B117DROPPOD_D1", KeywordTable.DropPod));
            u.CurrentDropPodHealth = 0; u.CurrentDropPodAttack = 7;
            bool hit = u.ApplyDropPodOnSummon();
            Ok(hit, "D1 命中 dropPod 支（返回 true）");
            Eq(u.CurrentDropPodHealth, UnitState.DropPodMaxHealthDefault, "D1′ 池子刷回 `dropPodMaxHealth`（原版 `.c:106→:116-120`）");
            Eq(u.CurrentDropPodAttack, UnitState.DropPodAttackDefault, "D1″ 舱体攻击刷回 `dropPodAttack`（原版 `.c:123→:129-133`）");
            Eq(u.CurrentDropPodHealth, 2, "D1‴ 值本身 = 2（`GameStaticData.cs:2405`，⛔ 别让它跟着常量一起漂）");
            Eq(u.CurrentDropPodAttack, 0, "D1⁗ 值本身 = 0（`GameStaticData.cs:2404`）");
        }

        // ---- D1′：幂等 —— 已经是默认值也照刷（原版就是无条件赋值，没有「if 为 0 才写」）
        {
            var u = Make(Unit("B117DROPPOD_D1p", KeywordTable.DropPod));
            u.CurrentDropPodHealth = 2; u.CurrentDropPodAttack = 0;
            Ok(u.ApplyDropPodOnSummon(), "D1′-0 第二次召唤照样命中");
            Eq(u.CurrentDropPodHealth, 2, "D1′-1 池子仍是 2（赋值是无条件的）");
            Eq(u.CurrentDropPodAttack, 0, "D1′-2 舱体攻击仍是 0");
        }

        // ---- D2 🔴 灭自证：不带 dropPod 的单位**一格都不许动**
        {
            var u = Make(Unit("B117DROPPOD_D2"));
            u.CurrentDropPodHealth = 0; u.CurrentDropPodAttack = 7;
            bool hit = u.ApplyDropPodOnSummon();
            Ok(!hit, "D2 未命中（返回 false）");
            Eq(u.CurrentDropPodHealth, 0, "D2′ 池子**不动**（仍是 0）");
            Eq(u.CurrentDropPodAttack, 7, "D2″ 舱体攻击**不动**（仍是 7）");
        }

        // ---- D3：与 `CardSetup` 那一格的口径差（原版 `.c:158-160` 的 `cardType == 10` 闸）
        //      ⇒ 非督军构造后池子是 0/0（字段默认值），**只能靠本支初始化**；督军构造时就已经是 2/0。
        //      ⚠️ 这一条同时钉住「别把本方法改成转调 `SetupDropPodStats`」（那样非督军就永远刷不上）。
        {
            var body = Make(Unit("B117DROPPOD_D3a", KeywordTable.DropPod));
            Eq(body.CurrentDropPodHealth, 0, "D3 非督军**构造后**池子仍是 0（`CardSetup` 那一格有 `cardType==10` 闸）");
            Eq(body.CurrentDropPodAttack, 0, "D3′ 非督军构造后舱体攻击仍是 0");
            Ok(body.ApplyDropPodOnSummon(), "D3″ 走一次召唤支之后才拿到初值");
            Eq(body.CurrentDropPodHealth, 2, "D3‴ 召唤后池子 = 2（⇒ 本支是普通单位**唯一**的初始化口）");

            var hero = Make(Hero("B117DROPPOD_D3b", KeywordTable.DropPod));
            Eq(hero.CurrentDropPodHealth, 2, "D3⁗ 督军**构造时**就已经是 2（`SetupDropPodStats`，原版 `CardSetup.c:158-200`）");
            Eq(hero.CurrentDropPodAttack, 0, "D3⁵ 督军构造时舱体攻击 = 0");
        }
    }

    // ============================================================
    //  W · 接线：真入口有没有把那一跳接上
    //  🔴 补丁（`RuleCore.cs`，归 `WREPLAY2`）落地前，本段必然红 —— 红 = 没接线，不是语义错。
    // ============================================================
    static void SectionW_Wiring()
    {
        Debug.Log(P + "--- W · 接线：`RuleCore.ApplyDeployTurnState` / `ActivateTraitsOnSummonOrEnchantment` ---");

        // ---- W1：部署入口（= 原版 `_MinionPlayedIntoField` → `ActivateTraitsOnSummonOrEnchantment(card, 1, 1, 0)`）
        {
            var u = Make(Unit("B117DROPPOD_W1", KeywordTable.DropPod));
            u.CurrentDropPodHealth = 0; u.CurrentDropPodAttack = 7;
            RuleCore.ApplyDeployTurnState(u);
            Eq(u.CurrentDropPodHealth, 2,
               "W1 `ApplyDeployTurnState` 里接上了那一跳（池子 0 → 2）｜红 = `RuleCore.cs` 的补丁没应用");
            Eq(u.CurrentDropPodAttack, 0, "W1′ 同上（舱体攻击 7 → 0）");
        }

        // ---- W2 🔴 灭自证：同一条入口，不带 dropPod 的单位不受影响
        {
            var u = Make(Unit("B117DROPPOD_W2"));
            u.CurrentDropPodHealth = 0; u.CurrentDropPodAttack = 7;
            RuleCore.ApplyDeployTurnState(u);
            Eq(u.CurrentDropPodHealth, 0, "W2 不带 dropPod ⇒ 池子仍是 0（召唤病那两位照旧，别串味）");
            Eq(u.CurrentDropPodAttack, 7, "W2′ 不带 dropPod ⇒ 舱体攻击仍是 7");
        }

        // ---- W3：另一条 `onSummon: true` 的入口（起始兵 `TutorialScript.PlaceTroops` / 督军开局 走的是它）
        {
            var u = Make(Unit("B117DROPPOD_W3", KeywordTable.DropPod));
            u.CurrentDropPodHealth = 0; u.CurrentDropPodAttack = 7;
            RuleCore.ActivateTraitsOnSummonOrEnchantment(u);
            Eq(u.CurrentDropPodHealth, 2,
               "W3 `ActivateTraitsOnSummonOrEnchantment(onSummon:true)` 里接上了那一跳｜红 = 补丁没应用");
            Eq(u.CurrentDropPodAttack, 0, "W3′ 同上（舱体攻击 7 → 0）");
        }

        // ---- W4：`fast`/`flank`/`ferocity`/`oath` 那三句**不能被本批改坏**（同一方法、同一批改动）
        {
            var fast = Make(Unit("B117DROPPOD_W4a", "fast"));
            fast.Exhausted = true;
            RuleCore.ActivateTraitsOnSummonOrEnchantment(fast);
            Ok(!fast.Exhausted, "W4 `fast` 照旧抬 `Exhausted`（本批没碰那三句）");

            var fer = Make(Unit("B117DROPPOD_W4b", KeywordTable.Ferocity));
            fer.Exhausted = true; fer.CannotAttackThisTurn = false;
            RuleCore.ActivateTraitsOnSummonOrEnchantment(fer);
            Ok(!fer.Exhausted, "W4′ `ferocity` 照旧抬 `Exhausted`");
            Ok(fer.CannotAttackThisTurn, "W4″ `ferocity` 照旧压 `CannotAttackThisTurn`");
        }

        // ⚠️ 缺一条（补丁落地后补）：「`onSummon: false`（附魔路）**不许**刷池子」——
        //    那要调 `ActivateTraitsOnSummonOrEnchantment(u, onSummon: false)`，而该重载由补丁一起加进来，
        //    **现在写会编不过**（`typecheck` 必须 0/0）。补丁落地后请补 `W5`，见报告 §六。
    }
}
