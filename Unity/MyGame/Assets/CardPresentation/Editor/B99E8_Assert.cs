// B99E8_Assert.cs — 执行代理 `B99_E8`（第九十九会话 · 战斗线写手）的断言集
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动 :870`（`CardScript.ResolveDeadCard(BattleAction battleAction)`）的**真缺口**
//   —— `chosenAbility 390 = AddTriggerRequiem` 那条 430/435 的派发（`B99_E1` 回源反证后定位的三处）：
//
//   (a) `RuleCore.ActiveRowEntersPlan` 加 `ca == 390`（同一物理行）——
//       原版 `AbilityLogic__PlayAbility.c:2073-2088`（`if (iVar8 == 0x186)`）那一支不看正文 op，
//       不加这格闸 ⇒ 正文 op 为空的 390 行被当 `unimplemented` 剔掉、执行器成死代码。
//   (b) `EffectResolver.RunActiveRowOpsOrGrantedTrigger` 加 `ca == 390` 分派 →
//       `RuleCore.RunTriggerRequiemRow` → `ResolveTriggerRequiem`（430 自己 + 435 别人）。
//       判据：`AddTriggerRequiem(targetCard, actingCard, …)`（`out_il2csharp BattleManager.cs:17546` 签名）→
//       `ResolveAction.c:5574` `case 0x40` → `ResolveTriggerRequiem.c:29` 入口闸（`targetCard` 必须 IsInPlayOrDying）。
//   (c) `RuleCore.FireObserverRequiemOnDeath` —— `ResolveDeadCard.c:264-283`「相邻观察者看着死者死 → 跑它自己的 430
//       + `BroadcastUnitRequiem`」那一支；`uStack_210`（= `BattleAction.actingCard` = 凶手）本代理回源核实。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · A1：删掉 `ActiveRowEntersPlan` 里的 `|| ca == 390` ⇒ `ActiveRowEntersPlan(u,0,390)` 变 false ⇒ **A1 红**。
//   · A2：把那一格写成「无条件 true」⇒ `…(u,0,391)`/`…(u,0,389)` 也会 true ⇒ **A2 红**（近值守卫）。
//   · B1：把 `ResolveTriggerRequiem` 的 `FireTriggerAt` 换回 `FireTriggerAlways`
//           ⇒ 无正文时凭空发一条 `EvtKind.Trigger` ⇒ `ctx.Events.Count` 多 1 ⇒ **B1 红**。
//   · B2：去掉 `RunTriggerRequiemRow` 的入口闸（目标不在场也照样跑）⇒ **B2 红**。
//   · C1/C2：把 `FireObserverRequiemOnDeath` 的邻接换算改回「裸 `slot±1`（摘除前坐标）」
//           ⇒ 摘除后外侧邻居已内移进 `slot`、`slot±1` 会点到**隔一个**的单位 ⇒ probe 里是 obs5/obs8
//             而不是 obs5/obs7 ⇒ **C1 红**。
//   · C4：去掉「死者非督军」闸 ⇒ 督军死也跑 ⇒ **C4 红**。
//
// ============================ 怎么用（宿主）============================
//   `B99E8_Assert.Run(); _sink.Pass += B99E8_Assert.Pass; _sink.Fail += B99E8_Assert.Fail;`
//   · 推荐宿主 = `Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本件**只碰纯引擎**，
//     不建 GameObject、不建资产、不需要场景（同 `B99E3_Assert`）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B99_E8`：账 `870` 的 (a) `ca=390` 进计划 · (b) `ca=390` 分派 · (c) 相邻观察者 430。</summary>
public static class B99E8_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E8] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具

    static CardDef Warlord(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static List<CardDef> DeckOf(string faction)
    {
        return new List<CardDef> { Warlord("B99E8Hero_" + faction, faction) };
    }

    static BattleContext NewCtx()
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves"), DeckOf("Goff"), seed: 0, shuffle: false);
    }

    /// <summary>白盒放盘（`RuleEngineTest.Place` 的等价物；⛔ 不走棋盘钩子）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool warlord = false)
    {
        var u = new UnitState(ctx.NewInstance(card), warlord);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>手搓一张卡（`cardTraits` / `keyword` 两列显式挂 —— 理由同 `B99E3_Assert.Card`）。</summary>
    static CardDef Card(string name, string faction, int cost, int attack, int health)
    {
        var def = new CardDef(name, name, "unit", "", "common", faction, cost, attack, health, 0, null,
                              subtype: "troop");
        def.AttachCardTraits(new string[0]);
        def.AttachKeywordId(0);
        return def;
    }

    // ============================================================ 入口

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B99_E8 账 870：(a) `ca=390` 进计划表 · (b) `ca=390` 分派 · (c) 相邻观察者 430");

        SectionA_EntersPlan();
        SectionB_Dispatch();
        SectionC_ObserverRequiem();

        Debug.Log(_sink.P + "B99E8 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 账 870 (a)

    static void SectionA_EntersPlan()
    {
        MenuCheck.Section(_sink, "A `ca=390 AddTriggerRequiem` 不看正文 op 也进计划表"
                              + "（判据 `AbilityLogic__PlayAbility.c:2073-2088`；⛔ 这不是「无条件 true」）");

        var ctx = NewCtx();
        var plain = Place(ctx, 0, 5, Card("B99E8PlainA", "SpaceWolves", 1, 1, 1));

        // A0 **前提硬断言**（⛔ 不许写成 `if (条件) Check(...)`）
        Tr(plain.Card.TriggerOps("requiem") == null,
           "A0 ★ 前提：`B99E8PlainA` 没有正文 op（`ActiveRowHasOps` 应为 false —— 否则 A1 会被「正文 op」顶上、假绿）");
        Tr(!RuleCore.ActiveRowHasOpsForTest(plain, 0),
           "A0 ★ 前提：`ActiveRowHasOpsForTest(plain,0) == false`（这张卡第 0 行没有配到正文 op）");

        // A1 主结论：ca == 390 进计划表
        Tr(RuleCore.ActiveRowEntersPlanForTest(plain, 0, 390),
           "A1 🔴 `ActiveRowEntersPlan(…, ca=390)` == true（正文 op 为空也进 —— 390 那一支不看 op）"
         + "｜🧨 删掉 `ActiveRowEntersPlan` 里的 `|| ca == 390` ⇒ 落回 `ActiveRowHasOps`(=false) ⇒ 红");

        // A2 近值守卫：390 不是「无条件 true」
        Tr(!RuleCore.ActiveRowEntersPlanForTest(plain, 0, 391),
           "A2 🔴 近值守卫：`…, ca=391` == false（390 加了、别把邻居 389/391 也放进来）"
         + "｜🧨 把那格写成无条件 true ⇒ 本行红（+ A2b）");
        Tr(!RuleCore.ActiveRowEntersPlanForTest(plain, 0, 389),
           "A2b 近值守卫：`…, ca=389` == false");
    }

    // ============================================================ B 账 870 (b)

    static void SectionB_Dispatch()
    {
        MenuCheck.Section(_sink, "B `ca=390` 分派到 `RunTriggerRequiemRow`（430 自己 + 435 别人；入口闸 target 必须在场/将死）");

        var ctx = NewCtx();
        var caster = Place(ctx, 0, 5, Card("B99E8Caster", "SpaceWolves", 2, 3, 3));
        var target = Place(ctx, 0, 6, Card("B99E8Target", "SpaceWolves", 1, 1, 1));

        // B0 前提：caster 无 requiem 正文（否则 B1 测不出「凭空发事件」）
        Tr(caster.Card.TriggerOps("requiem") == null,
           "B0 ★ 前提：`B99E8Caster` 没有 requiem 正文");

        // B2 入口闸：目标不在场/将死 ⇒ 返回 0（原版 `ResolveTriggerRequiem.c:29`）
        var ghost = new UnitState(ctx.NewInstance(Card("B99E8Ghost", "SpaceWolves", 1, 1, 1)), false);   // 不在任何棋盘上
        ghost.Health = 0;   // ⇒ `IsAlive == false` 且不在场 ⇒ `IsUnitInPlayOrDying == false`（否则 Health>0 的 IsAlive 会让闸放行）
        C(RuleCore.RunTriggerRequiemRowForTest(ctx, 0, caster, 0, 20, ghost), 0,
          "B2 🔴 `target` 不在场/将死 ⇒ `RunTriggerRequiemRow` 返回 **0**、不结算（原版入口闸）"
        + "｜🧨 去掉那道闸 ⇒ 照样往下跑 ⇒ 红");

        // B3 目标缺失（null、且本行无 target spec）⇒ 返回 0
        C(RuleCore.RunTriggerRequiemRowForTest(ctx, 0, caster, 0, 20, null), 0,
          "B3 `chosen == null` 且本行无 target spec ⇒ 返回 **0**（没有目标就不跑 —— 与 B2 同一条入口闸）");

        // B1 主结论：目标在场 ⇒ 尝试触发（返回 1），且**不凭空发 EvtKind.Trigger**（无正文时 FireTriggerAt 提前返回）
        int evBefore = ctx.Events.Count;
        int rc = RuleCore.RunActiveRowDispatchForTest(ctx, 0, caster, 0, 390, 20, target);
        RuleCore.PumpActionQueues(ctx);
        int evDelta = ctx.Events.Count - evBefore;
        C(rc, 1,
          "B1 🔴 `ca=390` 分派到 `RunTriggerRequiemRow`，目标在场 ⇒ 返回 **1**（已尝试触发 430/435）"
        + "｜🧨 删掉 `RunActiveRowOpsOrGrantedTrigger` 里的 `if (ca == 390)` ⇒ 落到 `RunActiveRowOps`（无 op）⇒ 0 ⇒ 红");
        C(evDelta, 0,
          "B1b 🔴 **不凭空多发事件**：无正文时 `ctx.Events` **一条都不该多**（`FireTriggerAt` 的 FxOps 判空筛子）"
        + "｜🧨 把 `ResolveTriggerRequiem` 的 `FireTriggerAt` 换回 `FireTriggerAlways` ⇒ 无正文也发一条 `EvtKind.Trigger` ⇒ 本行红");
    }

    // ============================================================ C 账 870 (c)

    static void SectionC_ObserverRequiem()
    {
        MenuCheck.Section(_sink, "C 死者相邻观察者各跑一次 430（判据 `ResolveDeadCard.c:264-283`；死者非督军 ∧ 观察者非将死）");

        var ctx = NewCtx();
        // 右侧 = 槽 5,6,7,8（index 0..3）。把「死者」放 6 号格，观察者放 5 / 7 / 8 号格。
        var obs5 = Place(ctx, 0, 5, Card("B99E8Obs5", "SpaceWolves", 1, 1, 1));
        var dead = Place(ctx, 0, 6, Card("B99E8Dead", "SpaceWolves", 1, 1, 1));
        var obs7 = Place(ctx, 0, 7, Card("B99E8Obs7", "SpaceWolves", 1, 1, 1));
        var obs8 = Place(ctx, 0, 8, Card("B99E8Obs8", "SpaceWolves", 1, 1, 1));

        // 模拟 `CleanupDeaths`：摘掉死者（连续无洞棋盘 ⇒ 外侧单位内移一格）。
        // 摘除 6（index 1）后：board5=obs5、board6=obs7、board7=obs8、board8=null。
        var removed = BoardSlots.RemoveAt(ctx.Players[0], 6);
        C(ReferenceEquals(removed, dead) ? 1 : 0, 1,
          "C0 ★ 前提：`BoardSlots.RemoveAt(0, 6)` 摘掉的正是 `B99E8Dead`");
        C(ReferenceEquals(ctx.Players[0].Board[6], obs7) ? 1 : 0, 1,
          "C0 ★ 前提：摘除后 `board[6] == obs7`（外侧邻居内移进死者原格）");

        var probe = new List<UnitState>();
        int evBefore = ctx.Events.Count;
        int fired = RuleCore.FireObserverRequiemOnDeathForTest(ctx, 0, 6, dead, obs5, probe);
        RuleCore.PumpActionQueues(ctx);
        int evDelta = ctx.Events.Count - evBefore;

        C(probe.Count, 2,
          "C1 🔴 死者(原 6 号格)的两个**原邻里**被选中 = obs5(5) + obs7(现 6) ⇒ probe 里 **2** 个"
        + "｜🧨 邻接换算写回裸 `slot±1`(摘除前坐标) ⇒ 会点到 obs5+obs8、漏 obs7 ⇒ 本行仍 2 但 C2 红");
        C(fired, 0,
          "C1b 观察者都没有 requiem 正文 ⇒「真正结算 430」的计数 = **0**（无正文不结算、不计噪声）");
        Tr(probe.Contains(obs5) && probe.Contains(obs7),
           "C2 🔴 probe 恰好含 **obs5** 与 **obs7**（死者的真邻里）"
         + "｜🧨 用裸 `slot±1` 换算 ⇒ probe 里换成 obs5/obs8 ⇒ 本行红（这是分辨「换算对没对」的唯一条）");
        Tr(!probe.Contains(obs8),
           "C2b 隔一个的单位 **obs8 不在** probe 里（它不邻接死者）");
        C(evDelta, 0,
          "C3 🔴 **不凭空多发事件**：观察者都没有 requiem 正文 ⇒ `ctx.Events` **一条都不该多**"
        + "｜🧨 用 `FireTriggerAlways` ⇒ 每个观察者多一条 `EvtKind.Trigger` ⇒ 本行红");

        // C4 死者是督军 ⇒ 整支跳过（原版 `get_cardType(死者) != 10`）
        var ctx2 = NewCtx();
        var hero = Place(ctx2, 1, 3, Card("B99E8DeadHero", "Goff", 1, 1, 1), warlord: true);
        var near = Place(ctx2, 1, 2, Card("B99E8Near", "Goff", 1, 1, 1));
        var probe2 = new List<UnitState>();
        C(RuleCore.FireObserverRequiemOnDeathForTest(ctx2, 1, 3, hero, null, probe2), 0,
          "C4 🔴 死者是**督军** ⇒ 返回 0、谁都不跑（原版 `:270` 死者 `cardType != 10` 的闸）"
        + "｜🧨 去掉 `dead.IsWarlord` 那道闸 ⇒ 返回 2 ⇒ 红");
        Tr(probe2.Count == 0,
           "C4b 督军那一档 probe 为空（一个观察者都没派发）");
    }
}
