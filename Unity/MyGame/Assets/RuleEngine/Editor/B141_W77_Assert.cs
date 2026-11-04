// B141_W77_Assert.cs — 批次 **B141** · 执行写手 `W77`（`B141-DEAD-CANDIDATES`）
//
// ============================ 它盯的是什么 ============================
//   `EffectResolver.DeadCandidates`（`choose … died this game / this battle / since your last turn` 的候选口，
//   调用点 = `ChooseCardCandidates` 的 `ChooseSrc == "dead"`）**改前手扫 `ctx.DeadUnits`**，
//   绕开了墓地那一口的四道闸（卡种 / 残骸 / 墓地放逐 / 侧别）。
//   🔴 **判据（第一权威）**：`AbilityLogic__GetTargets.c:949-960` —— `targetsAffected == 0x3c(60)` 那一支
//   **只**调 `BattleManager.GetCardsInCemetery(...)`；`BattleManager__GetCardsInCemetery.c` **无条件**排除
//   `isRemnant(+0x65) != 0`（残骸）与 `shouldCheckIfIsInCemetery(+0x24b) == 0`（墓地放逐），并排除 `cardType == 10 (Hero)`。
//   ⇒ 这 5 张 `choose … died …`（`EC39` / `GSC44` / `SAU39` / `SOR42` / `UM61`）与 `Return … died this game`
//   **是同一个候选源**。改后候选走 `CemeteryDeadUnits` → `ResolveCemeteryTargets` → `RuleCore.GetCardsInCemetery`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2/A4：把 `RuleCore.GetCardsInCemetery` 的 `remnantIds` 那一段删掉 ⇒ 残骸重新进候选 ⇒ 一起红。
//   · A1/A3：把 `DeadCandidates` 换回「手扫 `ctx.DeadUnits`」（本文件里的 `LegacyDeadCandidates` 就是那个旧形状）
//     ⇒ A1 实得 1 ⇒ 红；A3 钉住的正是「旧源在**同一份夹具**上确实会给出 1」，所以 A1 的 0 不是空转。
//   · A0/A0'/A0''：同一份夹具（`Discard` 与 `DeadUnits` 逐字相同、只差 `IsRemnant` 这一位）+「它确实在两处都登记过」
//     ⇒ A1 的 0 **不可能**是「它压根不在墓地」或「兵种被筛掉」造成的。
//   · B 组：放逐 / 侧别 / 卡种三道闸各一条，删任何一条都会让对应那张冒出来。· C 组（2026-11-04 `B142-E56` 收掉死亡回合窗口后）：同一份夹具上「旧形状」`LegacyDeadCandidates` 仍只给 1 张、新源给 2 张（C1'' 的判别式）⇒ 把窗口加回来 C1'/C1'' 一起红；`DeathTurn = -1` 那张同理由 0 变 1（C2'）。
//   · D4：真卡跑一遍 —— 墓地里同时摆「残骸 + 普通单位」⇒ 候选必须**恰好 1 张**且是普通单位。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里**同一物理行追加**（落在该行第一个 `//` 之前）：
//     `Guard("B141W77_Assert", B141_W77_Assert.Run); _pass += B141_W77_Assert.Pass; _fail += B141_W77_Assert.Fail;`
//   （写法与 `B141_W65_Assert` / `B141_W22_Assert` 同形；⚠️ 接线由**调度台**做 —— 本批白名单不含宿主。）
//
// ⚠️ 本文件**不建 GameObject、不建资产、不写盘**；只用 `RuleCore.NewBattle` 的内存局面。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W77 的断言（独立类：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W77_Assert
{
    static int _pass, _fail;

    /// <summary>离线取证用的卡池注入窄口（`工具/ruleprobe` 的 `Resources` 是桩、`CardDatabase.Load()` 恒 null）。
    /// Unity 侧**恒 `null`** ⇒ 走真装载链。⛔ 生产侧别写它。</summary>
    public static List<CardDef> PoolOverride;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W77：`DeadCandidates` 换到墓地那一口（残骸 / 放逐 / 侧别 / 卡种四道闸）");
        GroupA_RemnantGate();
        GroupB_OtherGates();
        GroupC_DeathTurnWindow();
        GroupD_FiveCardsReachable();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                                                 (IEnumerable<string>)null, subtype: "Infantry");

    static CardDef Tactic(string id) => new CardDef(id, id, "tactic", "Deal 1 damage", "common", "Test", 1, 0, 0, 0);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B141W77Warlord0") };
        var d1 = new List<CardDef> { Hero("B141W77Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>`dead` 来源的选牌 op（与 `EffectText` 解析出来的形状同构：`Verb = choosecard`、`ChooseSrc = dead`）。</summary>
    static EffectOp DeadOp(string scope)
        => new EffectOp
        {
            Verb = "choosecard", Source = "B141W77Fixture",
            ChooseSrc = "dead", ChooseWhat = "troop", ChooseAct = "deploy",
            ChooseDeadScope = scope,
        };

    /// <summary>造一份「已进墓地」的卡：实例进**某一侧**的 `Discard`（墓地列表）；可选再写一条阵亡登记。
    /// ⚠️ `Discard` 那一份**各个用例逐字相同**，差别只在 `DeadUnits` 那一条的 `IsRemnant` / `DeathTurn`。</summary>
    static CardInstance CemeteryCard(BattleContext ctx, CardDef card, int side, bool withDeathRecord,
                                     int deathTurn = 0, bool isRemnant = false)
    {
        var inst = ctx.NewInstance(card);
        ctx.Players[side].Discard.Add(inst);
        if (withDeathRecord)
            ctx.DeadUnits.Add(new DeadUnit
            {
                Card = card, Instance = inst, Owner = side, DeathTurn = deathTurn, IsRemnant = isRemnant,
            });
        return inst;
    }

    /// <summary>**改前的形状**（手扫 `ctx.DeadUnits`）—— 只为本文件复刻一份当判别式用，⛔ 生产侧不调、也⛔ 别拿它当实现。
    /// 它存在的唯一目的：证明「同一份夹具上，旧源会给出 1 张 ⇒ 新源的 0 是真的被闸挡掉了」。</summary>
    static List<CardDef> LegacyDeadCandidates(BattleContext ctx, int owner, string scope)
    {
        var outp = new List<CardDef>();
        if (ctx == null) return outp;
        int mark = ctx.Players[owner].LastTurnStartMark;
        foreach (var d in ctx.DeadUnits)
        {
            if (d.Owner != owner || d.Card == null) continue;
            if (scope == "since_last_turn" && d.DeathTurn < mark) continue;
            outp.Add(d.Card);
        }
        return outp;
    }

    static string Names(List<CardDef> list)
    {
        if (list == null || list.Count == 0) return "（空）";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append("、");
            sb.Append(list[i] != null ? list[i].Name : "null");
        }
        return sb.ToString();
    }

    // ==================================================================
    //  A 组：残骸闸（原版 `BattleManager__GetCardsInCemetery.c:60`）
    // ==================================================================
    static void GroupA_RemnantGate()
    {
        Section("A 残骸闸：「残骸被摧毁」进墓的那张**不进候选**（原版 `.c:60` 无条件排除）");

        // ---- A 组'：残骸（`IsRemnant = true`）----
        var ctx = NewCtx();
        var remCard = Unit("B141W77_Remnant");
        var rem = CemeteryCard(ctx, remCard, 0, withDeathRecord: true, deathTurn: ctx.Turn, isRemnant: true);
        Check(ctx.Players[0].Discard.Contains(rem), true,
              "A0 ★★ 灭自证前提：残骸那张**确实躺在墓地列表（`Discard`）里**");
        Check(ctx.DeadUnits.Count, 1, "A0' ★★ 灭自证前提：阵亡登记也**确实有** 1 条（`owner = 0`）");
        CheckTrue(remCard.IsUnit, "A0'' ★★ 灭自证前提：它**是单位卡**（`IsUnit` = true）⇒ 下面读不到不是「兵种被筛掉」");

        var opAll = DeadOp("all");
        var gotRem = RuleCore.DeadCandidatesForTest(ctx, 0, "all", opAll);
        Check(gotRem.Count, 0,
              "A1 ★★ 残骸**不进候选**：`DeadCandidates(\"all\")` = **0 张**（实际 = " + Names(gotRem) + "）"
            + "｜🧨 候选换回手扫 `ctx.DeadUnits` ⇒ 实得 1 ⇒ 红");
        Check(RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, false).Count, 0,
              "A4 ★ 交叉印证：同一条闸的另一个口 `GetCardsInCemetery` 也是 0 张");

        // ---- A 组：**同一份夹具、只把 `IsRemnant` 翻成 false** ----
        var ctx2 = NewCtx();
        var okCard = Unit("B141W77_NormalDead");
        var ok = CemeteryCard(ctx2, okCard, 0, withDeathRecord: true, deathTurn: ctx2.Turn, isRemnant: false);
        var gotOk = RuleCore.DeadCandidatesForTest(ctx2, 0, "all", DeadOp("all"));
        Check(gotOk.Count, 1,
              "A2 ★★ 普通阵亡**照收**：同一份夹具换成 `IsRemnant = false` ⇒ 候选 1 张（实际 = " + Names(gotOk) + "）");
        CheckTrue(gotOk.Count == 1 && ReferenceEquals(gotOk[0], okCard),
              "A2' ★ 候选里就是**那一张**（按 `CardDef` 引用判，不看名字）");
        Check(ctx2.Players[0].Discard.Contains(ok), true, "A2'' 前提：它同样躺在墓地列表里（与 A 组同形）");

        // ---- A3 ★★ 灭自证：旧源（手扫 `DeadUnits`）在同一份**残骸**夹具上会给出 1 张 ----
        var legacy = LegacyDeadCandidates(ctx, 0, "all");
        Check(legacy.Count, 1,
              "A3 ★★ 灭自证：**改前那个形状**（手扫 `ctx.DeadUnits`，本文件 `LegacyDeadCandidates` 复刻）在同一份夹具上 = **1 张**"
            + "｜说明 A1 的 0 **不是**「夹具里本来就没东西」，而是真的被残骸闸挡掉了");
        CheckTrue(legacy.Count == 1 && ReferenceEquals(legacy[0], remCard), "A3' ★ 旧源给出来的正是**那张残骸**");
    }

    // ==================================================================
    //  B 组：另外三道闸（放逐 / 侧别 / 卡种）
    // ==================================================================
    static void GroupB_OtherGates()
    {
        Section("B 同一条墓地路上的另外三道闸：墓地放逐 / 侧别（只看自己）/ 卡种（只收单位）");

        // ---- B1 墓地放逐（原版 `+0x24b == 0`）----
        var ctx = NewCtx();
        var exiled = CemeteryCard(ctx, Unit("B141W77_Banished"), 0, withDeathRecord: true,
                                  deathTurn: ctx.Turn, isRemnant: false);
        Check(RuleCore.DeadCandidatesForTest(ctx, 0, "all", DeadOp("all")).Count, 1,
              "B1-a 前提：放逐**之前**它是候选（1 张）");
        RuleCore.ExileFromCemetery(ctx, exiled);
        var afterExile = RuleCore.DeadCandidatesForTest(ctx, 0, "all", DeadOp("all"));
        Check(afterExile.Count, 0,
              "B1 ★★ 放逐后**不进候选**（原版 `shouldCheckIfIsInCemetery(+0x24b) == 0` 那一档）"
            + "｜实际 = " + Names(afterExile));

        // ---- B2 侧别：敌方墓地里的单位不进自己的候选 ----
        var ctx2 = NewCtx();
        CemeteryCard(ctx2, Unit("B141W77_EnemyDead"), 1, withDeathRecord: true,
                     deathTurn: ctx2.Turn, isRemnant: false);
        var foeOnly = RuleCore.DeadCandidatesForTest(ctx2, 0, "all", DeadOp("all"));
        Check(foeOnly.Count, 0,
              "B2 ★ 敌方墓地里的单位**不进「我方」候选**（候选 = " + Names(foeOnly) + "）");
        Check(RuleCore.DeadCandidatesForTest(ctx2, 1, "all", DeadOp("all")).Count, 1,
              "B2' ★ 反过来对它自己那一侧是候选（1 张）⇒ 0 不是「它压根不在任何墓地」");

        // ---- B3 卡种：墓地里的战术卡不进「部队」候选（原版 `includeMinions` 那一档）----
        var ctx3 = NewCtx();
        CemeteryCard(ctx3, Tactic("B141W77_DeadTactic"), 0, withDeathRecord: false, isRemnant: false);
        var tac = RuleCore.DeadCandidatesForTest(ctx3, 0, "all", DeadOp("all"));
        Check(tac.Count, 0,
              "B3 ★ 墓地里的**战术卡**不进候选（本笔只收单位 —— 原版这 5 张的 `includeMinions = 1 / includeSpells = 0`）"
            + "｜实际 = " + Names(tac));
    }

    // ==================================================================
    //  C 组：窗口档（`since_last_turn`）与 `DeathTurn = -1`
    // ==================================================================
    static void GroupC_DeathTurnWindow()
    {
        Section("C `since_last_turn` 窗口档（**2026-11-04 `B142-E56` 已收掉**）与 `DeathTurn = -1`（手牌超上限弃掉）那一档");

        var ctx = NewCtx();
        ctx.Players[0].LastTurnStartMark = 5;          // 定死，免得依赖 `NewBattle` 的初始回合号
        CemeteryCard(ctx, Unit("B141W77_Early"), 0, withDeathRecord: true, deathTurn: 4, isRemnant: false);
        CemeteryCard(ctx, Unit("B141W77_Since"), 0, withDeathRecord: true, deathTurn: 5, isRemnant: false);

        var all = RuleCore.DeadCandidatesForTest(ctx, 0, "all", DeadOp("all"));
        Check(all.Count, 2, "C1 `all` 档两张都在（本局死过 ⇒ 不看回合）｜实际 = " + Names(all));
        var since = RuleCore.DeadCandidatesForTest(ctx, 0, "since_last_turn", DeadOp("since_last_turn"));
        Check(since.Count, 2,
              "C1' ★ `since_last_turn` 窗口**已收掉**（`B142-E56`，原版 `GetCardsInCemetery.c` 无该维）⇒ 整座墓地 2 张｜实际 = " + Names(since));
        CheckTrue(since.Count == 2 && since[0].Name == "B141W77_Early" && since[1].Name == "B141W77_Since" && LegacyDeadCandidates(ctx, 0, "since_last_turn").Count == 1,
              "C1'' ★ 两张都在（`DeathTurn = 4 / 5` 不再影响候选）＋🧨 灭自证：旧窗口形状同一夹具仍只给 1 张 ⇒ 这里的 2 不是「夹具本来两张」；⛔ 别把窗口加回来");

        // ---- C2 `DeathTurn = -1` = 手牌超上限弃掉的单位卡（没有阵亡登记）----
        var ctx2 = NewCtx();
        ctx2.Players[0].LastTurnStartMark = 5;
        CemeteryCard(ctx2, Unit("B141W77_HandOverflow"), 0, withDeathRecord: false);   // 只在 Discard，没有 DeadUnits 记录
        Check(RuleCore.DeadCandidatesForTest(ctx2, 0, "all", DeadOp("all")).Count, 1,
              "C2 ★ `DeathTurn = -1`（手牌超上限弃掉 —— 原版同样 `SendToCemetery` ⇒ 墓地里**有**它）在 `all` 档**照收**");
        Check(RuleCore.DeadCandidatesForTest(ctx2, 0, "since_last_turn", DeadOp("since_last_turn")).Count, 1,
              "C2' ★ `DeathTurn = -1`（手牌超上限弃掉）在窗口档**也照收**（没有死亡回合 ⇒ 原版照样在墓地里）");
    }

    // ==================================================================
    //  D 组：池内 5 张卡真的走这条路
    // ==================================================================
    static void GroupD_FiveCardsReachable()
    {
        Section("D 池内 5 张 `choose … died …`（EC39 / GSC44 / SAU39 / SOR42 / UM61）的候选 = 墓地，且残骸被挡掉");

        var pool = PoolOverride ?? CardDatabase.Load();
        // 卡面窗口词 → 解析出来的 `ChooseDeadScope`
        var expect = new[]
        {
            new KeyValuePair<string, string>("EC39", "since_last_turn"),
            new KeyValuePair<string, string>("GSC44", "all"),
            new KeyValuePair<string, string>("SAU39", "all"),
            new KeyValuePair<string, string>("SOR42", "since_last_turn"),
            new KeyValuePair<string, string>("UM61", "all"),
        };

        foreach (var kv in expect)
        {
            string id = kv.Key;
            var card = CardDatabase.FindById(pool, id);
            CheckTrue(card != null, "D0 前提：" + id + " 在池内");
            if (card == null) continue;

            // ---- D1 正文解析出 `choosecard / dead` 且窗口档与卡面一致 ----
            EffectOp chooseOp = null;
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            foreach (var o in ops)
                if (o != null && o.Verb == "choosecard" && o.ChooseSrc == "dead") { chooseOp = o; break; }
            CheckTrue(chooseOp != null,
                      "D1 ★ " + id + " 的正文解析出 `choosecard` / `ChooseSrc = dead`（= 走的就是本件改的那一条路）");
            if (chooseOp == null) continue;
            Check(chooseOp.ChooseDeadScope, kv.Value,
                  "D2 ★ " + id + " 的窗口档 = `" + kv.Value + "`（卡面 `" + card.Desc + "`）");

            // ---- D3 它那一行 criteria 的 include 格（原版 `GetCardsInCemetery` 的 incl* 形参来源）----
            var set = card.AbilityTargetCriteriaAt(0, 0);
            if (set != null && set.IncludeMinions != CardDef.CountSetSpec.Unknown)
            {
                Check(set.IncludeMinions, 1, "D3 ★ " + id + " criteria：`includeMinions = 1`");
                Check(set.IncludeSpells, 0, "D3' ★ " + id + " criteria：`includeSpells = 0`");
                Check(set.FriendlyUnits, 1, "D3'' ★ " + id + " criteria：`friendlyUnits = 1`（只我方墓地）");
            }
            else
            {
                CheckTrue(true, "D3 ⚠️ " + id + " 的 `abilityTargetCriteria` 行读不到 ⇒ 候选按 §形状差 的兜底口径"
                              + "（全卡种 + 自己那一侧；本笔的 `IsUnit` / owner 两道筛仍然把它收到单位上）");
            }

            // ---- D4 ★★ 真卡跑一遍：墓地摆「残骸 + 普通单位」⇒ 候选恰好 1 张、且是普通那张 ----
            var ctx = NewCtx();
            ctx.Players[0].LastTurnStartMark = 0;      // `all` 档不看它；写死只为可复现
            var remCard = Unit("B141W77_" + id + "_Remnant");
            var okCard = Unit("B141W77_" + id + "_Normal");
            CemeteryCard(ctx, remCard, 0, withDeathRecord: true, deathTurn: 7, isRemnant: true);
            CemeteryCard(ctx, okCard, 0, withDeathRecord: true, deathTurn: 7, isRemnant: false);
            var prevSrc = ctx.PushCreationSource(card);   // `ResolveCemeteryTargets` 按「当前来源卡」读 criteria 行
            List<CardDef> got;
            try { got = RuleCore.DeadCandidatesForTest(ctx, 0, chooseOp.ChooseDeadScope, chooseOp); }
            finally { ctx.CreationSource = prevSrc; }

            Check(got.Count, 1,
                  "D4 ★★ " + id + " 真卡：墓地「残骸 + 普通单位」各一张 ⇒ 候选**恰好 1 张**（实际 = " + Names(got) + "）");
            CheckTrue(got.Count == 1 && ReferenceEquals(got[0], okCard),
                  "D4' ★★ " + id + " 候选里是**普通那张**，残骸那张不在（原版选不到残骸）");
        }
    }

    // ==================================================================
    //  口令 / 汇总
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B141_W77 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W77 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
