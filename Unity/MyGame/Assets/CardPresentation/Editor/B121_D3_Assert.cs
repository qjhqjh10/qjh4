// B121_D3_Assert.cs — 执行写手 `B121_D3`（第一百二十一会话 · A 类 P0 `B121-03`）
//
// ============================ 它盯的是什么 ============================
//   `RuleCore.AbilityTargetListFromCriteria` 的 **`ta = 0 (any)`** 那一档：
//   原来那条门写着「`0 any` —— 池内 0 张、原版休眠，裁定 ④ 不接线」⇒ 这一档返回 `null`。
//   **那条前提已被现读数推翻**：`cards_engine.json` 里 `abilityTargets[].ta == [0]` 有
//   **224 行 / 209 张**（`activeAbilityLogic` 另有 17 行 / 14 张）。
//
//   判据（🥇 逐句现读）= `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:884-891`：
//     `if (*(int *)(param_2 + 0x10) == 0)`（= `TargetCriteria.targetsAffected == 0`）
//       ⇒ `BattleManager.GetUnitsInPlayList(ref list, clear 0, inclHero 1, inclMinions 1,
//          inclHeroParts 1, inclWeapons 1, inclRemnants 1, inclPlayer 1, inclEnemy 1, includeDying 0)`
//       ⇒ `goto LAB_180818485`（**= return**，`:1378-1381`）。
//   字段表 `d:/2/tools/il2cpp_out/dump.cs:23823-23872`（`targetsAffected` 的 tooltip 原文
//   「'Any' disregards all other filters」）。
//
//   ⇒ 语义三句：**双方全场的单位** · **不看任何 36 格筛** · **不走 `maxTargets` 截断**。
//
// ============================ 灭自证（哪一处改动会让哪一条红）============================
//   A1 `ta=0` 只取己方 ⇒ 变 4（不是 6）⇒ 红。
//   A3 `AddSide` 不过 `IsAlive` ⇒ 死单位混进来 ⇒ 变 6 ⇒ 红。
//   A4 让 `maxTargets` 对 `ta=0` 也生效 ⇒ 变 1 ⇒ 红。
//   A5 把 `ta=0` 拿去落 36 格筛（`friendlyUnits=0` 之类）⇒ 变 0 ⇒ 红。
//   A6 把整条 `ta=0` 改回 `null` ⇒ A1/A5 全红。
//   A7 **反证**：`ta=10` 己方那一档若被 `ta=0` 的新分支顺手改宽 ⇒ 变 6（应 4）⇒ 红。
//   A8 **反证**：没接的档（`ta=65`）必须仍回 `null`（⛔ 用空表冒充「原版也没目标」）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   纯引擎侧（不碰场景 / 不碰 `BattleDriver`）⇒ 同 `B104_MT_Assert` 的接法：在任一跑在
//   **Editor 程序集**的自检宿主（建议 `RuleEngine/Editor/RuleEngineTest.cs`）里，
//   **同一物理行末尾追加**：
//     ` B121_D3_Assert.Run(); _pass += B121_D3_Assert.Pass; _fail += B121_D3_Assert.Fail;`
//   ⚠️ `Pass` / `Fail` 是**累计值** ⇒ 只加一次。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B121_D3`：`ta=0 (any)` 建池语义（双方全场 / 不筛 / 不截断 / 不含死者）。合成夹具 —— 见文件头。</summary>
public static class B121_D3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B121_D3] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 夹具（与 `B104_MT_Assert` 同形）

    static CardDef Hero(string id) =>
        new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef PlainUnit(string id) =>
        new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 3, 0, (string[])null);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B121D3_w0") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("B121D3_f" + i));
        var d1 = new List<CardDef> { Hero("B121D3_w1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    /// <summary>⚠️ 督军格要 `warlord: true`（`UnitState.IsWarlord` 是构造期只读）。</summary>
    static UnitState Put(BattleContext ctx, int p, int slot, CardDef card, bool warlord = false)
    {
        var u = new UnitState(ctx.NewInstance(card), warlord);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>一份**14 格都已知**的 `countSet`（`Unknown` 是另一档语义）；字段顺序与 `CardDef.CountSetSpec` 逐字对齐。</summary>
    static CardDef.CountSetSpec Set(int ta, int friendly, int enemy, int hero, int minions, int remnants,
                                    int maxTargets = 0, int method = 0)
    {
        var s = new CardDef.CountSetSpec();
        s.PlayerTurn = 0;
        s.TargetsAffected = ta;
        s.ChosenArmy = 0; s.ArmyFilter = 0; s.KeywordFilter = 0; s.TraitsFilter = 0;
        s.MaxTargets = maxTargets; s.TargetSelectMethod = method;
        s.AttackFilter = 0; s.AttackFilterOption = 0;
        s.CurrentHealthFilter = 0; s.CurrentHealthFilterOption = 0;
        s.ManaCostFilter = 0; s.ManaCostFilterOption = 0;
        s.RarityFilter = 0; s.RarityFilterOption = 0;
        s.SpellType = 0;
        s.FriendlyUnits = friendly; s.EnemyUnits = enemy;
        s.IncludeHero = hero; s.IncludeMinions = minions;
        s.IncludeSpells = 0; s.IncludeWhispers = 0; s.IncludeWeapons = 0;
        s.IncludeRemnants = remnants; s.IncludeHeroParts = 0; s.IncludeSelf = 0;
        s.ExcludeNotSelf = 0; s.ExcludeSimilarCards = 0; s.ExcludeTargetted = 0;
        s.ExcludeFriendlyHero = 0; s.ExcludeEnemyHero = 0;
        s.ExcludeRace = 0; s.ExcludeTrait = 0; s.SelectFromChooseResolution = 0;
        s.FilterSpecificCardIds = new string[0];
        s.ExcludeEnchantmentIds = new string[0];
        return s;
    }

    static string Slots(List<BattleEventTarget> l)
    {
        if (l == null) return "(null)";
        var b = new System.Text.StringBuilder();
        for (int i = 0; i < l.Count; i++) { if (i > 0) b.Append(','); b.Append(l[i].Player).Append(':').Append(l[i].Slot); }
        return b.ToString();
    }

    /// <summary>棋盘：我方 2 / 3(施放者) / 4(督军) / 5 共 4 张；敌方 4(督军) / 6 共 2 张 ⇒ 全场 6 张。</summary>
    static BattleContext Board(out UnitState src)
    {
        var ctx = NewCtx();
        ctx.Active = 0;
        Put(ctx, 0, 2, PlainUnit("B121D3_a2"));
        Put(ctx, 0, 5, PlainUnit("B121D3_a5"));
        Put(ctx, 0, 4, Hero("B121D3_h0"), warlord: true);
        Put(ctx, 1, 4, Hero("B121D3_h1"), warlord: true);
        Put(ctx, 1, 6, PlainUnit("B121D3_e6"));
        src = Put(ctx, 0, 3, PlainUnit("B121D3_src"));
        return ctx;
    }

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B121_D3：`ta=0 (any)` 建池（双方全场 / 不筛 / 不截断 / 不含死者）");
        SectionA_Any();
        Debug.Log(_sink.P + "B121_D3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void SectionA_Any()
    {
        // ---- A1：`ta=0` ⇒ 双方全场的单位（4 + 2 = 6）----
        {
            UnitState src;
            var ctx = Board(out src);
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(0, 1, 1, 1, 1, 1), src, null);
            string got = Slots(list);
            C(list == null ? -1 : list.Count, 6,
              "A1 **`ta=0 any` ⇒ 双方全场 6 张**（我方 2/3/4/5 ＋ 敌方 4/6，实得 " + got + "）"
            + "｜判据 `AbilityLogic__GetTargets.c:884-891`：`GetUnitsInPlayList(clear 0, inclHero/Minions/HeroParts/Weapons/Remnants/Player/Enemy 全 1, includeDying 0)`"
            + "｜🧨 只取己方（照 `friendlyUnits` 那格）⇒ 变 4 ⇒ 红");
        }

        // ---- A2：`ta=0` **不看** 36 格 —— 把 `friendlyUnits/enemyUnits/include*` 全填 0（本该全淘汰）----
        {
            UnitState src;
            var ctx = Board(out src);
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(0, 0, 0, 0, 0, 0), src, null);
            C(list == null ? -1 : list.Count, 6,
              "A2 ★★ **`ta=0` 的 36 格一律不参与**（`friendlyUnits=0 / enemyUnits=0 / include* = 0` 仍然 6 张）"
            + "｜判据 = `TargetsAffected` 的 tooltip「'Any' disregards all other filters」（`dump.cs:23826`）＋ 原版这一支建完池**直接 return**（`:891`）"
            + "｜🧨 顺手让它落常规筛 ⇒ 变 0 ⇒ 红");
        }

        // ---- A3：`includeDying 0` —— 棋盘上放一个 `Health = 0` 的单位，它**不该**进池 ----
        {
            UnitState src;
            var ctx = Board(out src);
            var dying = ctx.Players[1].Board[6];
            dying.Health = 0;                                   // `UnitState.IsAlive` = `Health > 0`
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(0, 1, 1, 1, 1, 1), src, null);
            string got = Slots(list);
            Tr(list != null && list.Count == 5,
              "A3 ★ **`includeDying 0`**：敌方 6 号位那个 `Health = 0` 的单位**不在池里**（应 5 张，实得 " + got + "）"
            + "｜判据 `:890` 最后一个实参 = 0；我们的等价物 = `EffectResolver.AddSide` 的 `!u.IsAlive ⇒ continue`"
            + "｜🧨 池不加死活闸 ⇒ 变 6 ⇒ 红");
        }

        // ---- A4：`maxTargets` 对 `ta=0` **无效**（原版建完池直接 return，`maxTargets` 那一层根本没到）----
        {
            UnitState src;
            var ctx = Board(out src);
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(0, 1, 1, 1, 1, 1, maxTargets: 1, method: 7), src, null);
            C(list == null ? -1 : list.Count, 6,
              "A4 ★ **`ta=0 ∧ maxTargets=1` ⇒ 仍然 6 张（不截断）**（现池有 **13 行** `ta=0 ∧ maxTargets>=1`，全靠这一条）"
            + "｜判据 `:891` 的 `goto LAB_180818485` 在建池之后、截断（`:1164-1371`）之前"
            + "｜🧨 让截断对 `ta=0` 生效 ⇒ 变 1 ⇒ 红");
        }

        // ---- A5：`ta=0` 含残骸（原版 `inclRemnants = 1`）—— 与 `ta=10` 靠 `includeRemnants` 那格控制**不同** ----
        {
            UnitState src;
            var ctx = Board(out src);
            var rem = Put(ctx, 1, 5, PlainUnit("B121D3_rem"));
            rem.IsRemnant = true;
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(0, 1, 1, 1, 1, 0), src, null);
            string got = Slots(list);
            Tr(list != null && list.Count == 7 && got.Contains("1:5"),
              "A5 **`ta=0` 含残骸**（`inclRemnants = 1` 是原版写死的）；本夹具的 `includeRemnants` 格填 **0** 也照样进池（应 7 张且含 `1:5`，实得 " + got + "）"
            + "｜判据 `:890`：那 8 个实参是**常量**（`1,1,1,1,1,1,1,0`），⛔ 不读 criteria 的白名单位"
            + "｜🧨 让 `ta=0` 改读 `criteria.IncludeRemnants` ⇒ 变 6 ⇒ 红");
        }

        // ---- A6（反证）：既有 `ta=10 allFiltered` **不受影响**（只有 `friendlyUnits=1` ⇒ 己方 4 张）----
        {
            UnitState src;
            var ctx = Board(out src);
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(10, 1, 0, 1, 1, 0), src, null);
            string got = Slots(list);
            C(list == null ? -1 : list.Count, 4,
              "A6 ★ **反证：`ta=10 allFiltered` 仍按 `friendlyUnits/enemyUnits` 建池**（只己方 ⇒ 4 张，实得 " + got + "）"
            + "｜🧨 `ta=0` 的新分支顺手改宽了 10 那一档 ⇒ 变 6 ⇒ 红");
        }

        // ---- A7（反证）：没接的档必须仍回 `null` ----
        {
            UnitState src;
            var ctx = Board(out src);
            Tr(RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(65, 1, 0, 0, 1, 0), src, null) == null,
              "A7 **`ta=65 inDeckFiltered` 仍未接 ⇒ `null`**（⛔ 不拿空表冒充「原版也没有目标」）"
            + "｜🧨 把 `ta=0` 那条 `return null` 门整个删掉 ⇒ 这一档会掉进 switch 之后那条路 ⇒ 红");
        }
    }
}
