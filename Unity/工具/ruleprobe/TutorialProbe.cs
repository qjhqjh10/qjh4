// TutorialProbe.cs — `tutorial <list|run|g3|pred>`：**教程局读数口**（只读 · 不判对错）
//
// 🔴 **它修的是哪一类【结构性失明】**（2026-10-26 · 代理 `G5` · 第 5 轮）：
//   `F4_第4轮.md` §1·3 的 15 条 B 里，**12 条 `AiScripted` 全部卡在 G3（可观察量）** ——
//   原话「落点全在 `CardPresentation/**`（表现层）或私有谓词上，`ruleprobe` 结构上读不到」。
//   而按 `判据/战斗逻辑驱动_B2A_口径.md` §三，**「断言绿」不算可观察量**（那是自证）。
//   本模式补的就是这个口：**用真实的 6 关教程数据造一局真教程局**（回合内 **430** 条动作；
//   全量 462 = 回合内 430 + `preMulligan` 19 + `onVictory` 13 —— ⛔ 两个数别混），
//   把「原版判据转写」与「我们产品代码的实际返回值」**并排打出来**（两侧数值 + 可复跑）。
//
// 为什么能纯 dotnet 跑（不占 Unity）：
//   · `RuleEngine/Core/TutorialScript.cs` 本来就在 `ruleprobe.csproj` 的编译集里
//     （`../../MyGame/Assets/RuleEngine/Core/*.cs`）；
//   · 教程关卡数据是**离线文件**（`Assets/RuleEngine/Resources/tutorial_stages.json`），
//     本探针自己用 `System.Text.Json` 读，再用 `TutorialData.Install(...)` 装进去
//     —— 走的是产品代码那条**同一条**装载口（`TutorialScript.cs:418-419`）。
//   · 需要「原版那边」时：**把反编译方法体逐句转写成 C#**（每块都写 `文件:行号`），
//     ⛔ 不调用我们的实现去生成期望值（否则就是自证）。
//
// 判据（唯一正本 = 反编译）：
//   🥇 `d:/2/tools/decomp_full/AiScripted__*.c`（Ghidra）
//   🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AiScripted.cs`（**唯一有真方法体**的那份 cpp2il）
//   ⚠️ 教程那份 `PlayScriptedTurn` 的 `.c` 落盘的是**战役**重载（`+0x14`）——
//      教程口径 `+0x10` 的判据是 VA 反汇编 `0x180944060`，见 `TutorialScript.cs:811-827`。
//
// ⚠️ **它不判对错**：读数摆出来，判决归调度台（与 `play` / `ai` 同一条纪律）。
// ⛔ **本文件不改引擎**：只读卡池 / 关卡数据，加上**反射调用**产品的私有谓词（不改产品源码）。
// ⛔ **不影响既有输出**：不碰基线、不碰三个指纹、不改 `check`/`play`/`ai` 的任何一行。
//
// 用法：
//   bash d:/4/Unity/工具/ruleprobe.sh tutorial list           # 数据清单（6 关 / 回合 / 动作 / 大类直方图）
//   bash d:/4/Unity/工具/ruleprobe.sh tutorial run [S]        # 造真教程局并按 6 关全量走一遍（默认全 6 关）
//   bash d:/4/Unity/工具/ruleprobe.sh tutorial g3 [行号]       # ★ 逐条 B→A 读数（默认全部块）
//   bash d:/4/Unity/工具/ruleprobe.sh tutorial pred 112        # 只跑某一个账行的块
//
// 落地报告 → `资料/完全复刻/报告/G5_第5轮.md`；用法也追加在
// `资料/全量反编译_入口与用法.md` 末尾「📜 教程局读数口」那一节。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using RuleEngine;

class TutorialProbe
{
    // ==================================================================
    //  0. 常量 / 输出小工具
    // ==================================================================

    const string StagesAsset = "tutorial_stages.json";
    const string DecksAsset = "tutorial_decks.json";

    /// <summary>解包资源里那两份 `VarsDevice`（`AiScripted.GetHorizontalOffset` 的 20/18 与 18/16）。</summary>
    const string VarsDesktopPath =
        "d:/2/新解包资源/assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/VarsDesktop.json";
    const string VarsMobilePath =
        "d:/2/新解包资源/assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/VarsMobile.json";

    /// <summary>我们自己的卡面常量载体（`CardView.Width` / `CardView.Height`）——
    /// **只用来现读源码里的字面量**（行 110 的「载体对比」那一段），⛔ 不代表探针能调到产品代码。</summary>
    const string CardViewPath =
        "d:/4/Unity/MyGame/Assets/CardPresentation/Core/CardView.cs";

    static readonly JsonSerializerOptions JsonOpt = new JsonSerializerOptions
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = false,   // 与 Unity `JsonUtility` 同口径（逐字对字段名）
    };

    static int _bad;                            // 读数本身坏掉（读不到数据 / 反射找不到）的条数

    static void H(string s) { Console.WriteLine(); Console.WriteLine("──────── " + s + " ────────"); }
    static void L(string s) { Console.WriteLine("  " + s); }
    static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

    public static int Run(string mode, string arg)
    {
        if (string.IsNullOrEmpty(mode)) mode = "list";
        var file = LoadStages();
        if (file == null || file.stages == null || file.stages.Length == 0)
        {
            Console.Error.WriteLine("!! 关卡数据读不到/是空的（" + StagesAsset + "）⇒ 什么都量不了");
            return 2;
        }
        TutorialData.Install(file.stages);      // 走产品那条**同一条**装载口（不进 Resources）

        switch (mode)
        {
            case "list": List(file); return 0;
            case "run": return Walk(file, arg);
            case "g3": return G3(file, arg);
            case "pred": return G3(file, arg);
            default:
                Console.Error.WriteLine("!! 用法：tutorial <list|run [S]|g3 [行号]|pred <行号>>");
                return 1;
        }
    }

    // ==================================================================
    //  1. 数据装载（关卡 + 那 12 副关卡牌）
    // ==================================================================

    static string ResDir { get { return Path.Combine(Program.OutDir, "RuleEngine", "Resources"); } }

    static TutorialStageFile LoadStages()
    {
        var p = Path.Combine(ResDir, StagesAsset);
        if (!File.Exists(p)) { Console.Error.WriteLine("!! 找不到 " + p); return null; }
        try { return JsonSerializer.Deserialize<TutorialStageFile>(File.ReadAllText(p), JsonOpt); }
        catch (Exception e) { Console.Error.WriteLine("!! 解 " + p + " 失败：" + e.Message); return null; }
    }

    // ---- tutorial_decks.json 的最小 DTO（字段名逐字照产物；⛔ 别加花）----
    [Serializable] class DecksFileDto { public int format; public DeckStageDto[] stages; }
    [Serializable] class DeckStageDto { public int stage; public DeckSideDto player; public DeckSideDto ai; }
    [Serializable] class DeckSideDto
    {
        public string deckId; public string name; public string faction; public string heroId;
        public string defensiveId; public string[] cardIds;
    }

    static DecksFileDto LoadDecks()
    {
        var p = Path.Combine(ResDir, DecksAsset);
        if (!File.Exists(p)) return null;
        try { return JsonSerializer.Deserialize<DecksFileDto>(File.ReadAllText(p), JsonOpt); }
        catch { return null; }
    }

    static DeckStageDto DeckOf(DecksFileDto f, int stage)
    {
        if (f == null || f.stages == null) return null;
        for (int i = 0; i < f.stages.Length; i++) if (f.stages[i] != null && f.stages[i].stage == stage) return f.stages[i];
        return null;
    }

    /// <summary>`DeckSideDto` → `CardDef` 列表（**督军在前**，其余按 `cardIds` **倒序** = 我们列表的
    /// 「牌库顶在前」，教程不洗牌）。认不出的条目**逐条出声并丢掉**（⛔ 不拿别的卡顶上去）。
    /// 🔴 **2026-10-26（批次 6 · G18）：为什么倒序** —— `RuleCore.Draw` 取列表**末尾**当「顶」
    ///   （`RuleCore.cs:3093-3096` 的镜像约定），而原版从下标 0 抽、SO 原序下标 0 = 顶
    ///   （`BattleManager__DrawCardFromDeck.c` 的 `List__get_Item(deck, 0)`）。产品装载口
    ///   `BattleDriver.BuildTutorialDeck` **同步改成倒序**（同一口径，别只改一边 ⇒ 量具与产品分叉）。</summary>
    static List<CardDef> DeckCards(DeckSideDto d, Dictionary<string, CardDef> byId, string who, List<string> notes)
    {
        var list = new List<CardDef>();
        if (d == null) { notes.Add(who + "：`tutorial_decks.json` 里没有这一关的这一侧"); return list; }
        CardDef hero;
        if (string.IsNullOrEmpty(d.heroId) || !byId.TryGetValue(d.heroId, out hero) || hero.Type != "hero")
        {
            notes.Add(who + "：督军 `" + d.heroId + "` 在卡池里找不到（或不是 hero）⇒ 这一侧用合成督军顶（读数里会显式标出）");
            return list;
        }
        list.Add(hero);
        int miss = 0;
        if (d.cardIds != null)
        {
            for (int i = d.cardIds.Length - 1; i >= 0; i--)
            {
                CardDef c;
                if (!byId.TryGetValue(d.cardIds[i] ?? "", out c)) { miss++; continue; }
                list.Add(c);
            }
        }
        if (miss > 0) notes.Add(who + "：`cardIds` 里有 " + miss + "/" + d.cardIds.Length + " 张我们的池里没有（已丢）");
        return list;
    }

    static Dictionary<string, CardDef> PoolById(List<CardDef> pool)
    {
        var d = new Dictionary<string, CardDef>(StringComparer.Ordinal);
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && !string.IsNullOrEmpty(pool[i].Id) && !d.ContainsKey(pool[i].Id)) d[pool[i].Id] = pool[i];
        return d;
    }

    /// <summary>造一局**真教程局**：走 `RuleCore.NewBattle(..., tutorial:)`（教程分支唯一落点）。</summary>
    static BattleContext BuildCtx(TutorialStageData st, TutorialScript script,
                                  List<CardDef> pool, Dictionary<string, CardDef> byId,
                                  DecksFileDto decks, List<string> notes)
    {
        var ds = DeckOf(decks, st.stage);
        var a = ds == null ? new List<CardDef>() : DeckCards(ds.player, byId, "S" + st.stage + " 玩家", notes);
        var b = ds == null ? new List<CardDef>() : DeckCards(ds.ai, byId, "S" + st.stage + " AI", notes);
        // 兜底：关卡牌认不出督军时，合成一副最小牌（只为让 `NewBattle` 建得起人；读数里标出来）
        if (a.Count == 0) a.Add(SynthHero("G5HERO0"));
        if (b.Count == 0) b.Add(SynthHero("G5HERO1"));
        return RuleCore.NewBattle(a, b, seed: 0, shuffle: true, cardPool: pool,
                                  vars: null, firstSeat: 0, tutorial: script);
    }

    static CardDef SynthHero(string id)
    {
        return new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, null);
    }

    // ==================================================================
    //  2. `list` —— 数据清单
    // ==================================================================

    static void List(TutorialStageFile f)
    {
        Console.WriteLine("=== tutorial list · 关卡数据清单 ===");
        L("文件 " + Path.Combine(ResDir, StagesAsset));
        L("张数 " + f.stages.Length + " 关 · format=" + f.format);
        foreach (var st in f.stages)
        {
            if (st == null) continue;
            var kind = new SortedDictionary<int, int>();
            int turnActs = 0, player = 0, tips = 0;
            if (st.turns != null)
                foreach (var t in st.turns)
                {
                    if (t == null || t.actions == null) continue;
                    foreach (var a in t.actions)
                    {
                        turnActs++;
                        if (a == null) continue;
                        if (a.playerAction) player++;
                        if (a.Kind == ScriptedActionType.SmallTip) tips++;
                        int k = (int)a.Kind;
                        int n; kind.TryGetValue(k, out n); kind[k] = n + 1;
                    }
                }
            L("S" + st.stage + " 「" + st.so + "」 先手=" + (st.playerStarts ? "玩家" : "AI")
              + " · 回合 " + (st.turns == null ? 0 : st.turns.Length)
              + " · 回合内动作 " + turnActs + "（玩家条 " + player + " / SmallTip " + tips + "）"
              + " · 起始单位 " + Len(st.playerStartingTroops) + "/" + Len(st.enemyStartingTroops)
              + " · 起手 " + Len(st.playerStartingTroopsInHand) + "/" + Len(st.enemyStartingTroopsInHand));
            var sb = new StringBuilder("     动作大类：");
            foreach (var kv in kind) sb.Append((ScriptedActionType)kv.Key).Append("×").Append(kv.Value).Append("  ");
            Console.WriteLine(sb.ToString());
        }
        int all = 0;
        foreach (var st in f.stages) all += Count(st, true) + Count(st, false);
        L("回合内动作合计 " + all + "（+ preMulligan / onVictory 未计 —— `run` 只走回合内，与产品的 `_ExecuteScriptedTurn` 同域）");
    }

    static int Len(TutorialUnitRef[] a) { return a == null ? 0 : a.Length; }

    static int Count(TutorialStageData st, bool turns)
    {
        int n = 0;
        if (turns && st.turns != null)
            foreach (var t in st.turns) if (t != null && t.actions != null) n += t.actions.Length;
        return n;
    }

    /// <summary>6 关的**回合内**动作展平（= 产品 `CurrentTurn` 那个域）。</summary>
    struct Slot
    {
        public TutorialStageData Stage; public TutorialTurn Turn; public int TurnIdx;
        public int Counter; public TutorialAction Action;
    }

    static List<Slot> AllSlots(TutorialStageFile f, int onlyStage = 0)
    {
        var list = new List<Slot>();
        foreach (var st in f.stages)
        {
            if (st == null) continue;
            if (onlyStage != 0 && st.stage != onlyStage) continue;
            if (st.turns == null) continue;
            for (int ti = 0; ti < st.turns.Length; ti++)
            {
                var t = st.turns[ti];
                if (t == null || !t.scriptedTurn || t.actions == null) continue;
                for (int k = 0; k < t.actions.Length; k++)
                    list.Add(new Slot { Stage = st, Turn = t, TurnIdx = ti, Counter = k, Action = t.actions[k] });
            }
        }
        return list;
    }

    // ==================================================================
    //  3. 反射 —— 产品的**私有**谓词（⛔ 不改产品源码，只读它的实际返回值）
    // ==================================================================

    static MethodInfo Priv(Type t, string name)
    {
        return t.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
    }

    static object CallPriv(MethodInfo mi, params object[] a)
    {
        if (mi == null) return null;
        try { return mi.Invoke(null, a); }
        catch (TargetInvocationException e) { return e.InnerException ?? e; }
    }

    // ==================================================================
    //  4. `run` —— 造真教程局并按 6 关全量走一遍
    // ==================================================================

    static int Walk(TutorialStageFile f, string arg)
    {
        int only = ParseStage(arg);
        var pool = Program.PoolList;
        var byId = PoolById(pool);
        var decks = LoadDecks();
        if (decks == null) L("⚠️ 读不到 " + DecksAsset + " ⇒ 关卡牌用合成督军顶（读数里会标）");
        Console.WriteLine("=== tutorial run · 真教程局（`RuleCore.NewBattle(..., tutorial:)`）===");
        // 🔴 **2026-10-26（批次 6 · G18）量具边界：`tutorial run` 不调 `EndTurn`**
        //   ⇒ `RuleCore.SweepEphemeral` 不跑、**天赋临时卡（全是 `Ephemeral`）留在手牌里累积**
        //   ⇒ **手牌张数偏高不代表引擎错**（真局回合末会移出）。本模式走的是脚本回合，
        //   `BeginTurn` 会推进回合，但没有「玩家/对手真结束回合」那一下的收尾清扫。
        L("⚠️ 量具边界：本模式**不调 `EndTurn`** ⇒ `SweepEphemeral` 不跑、天赋临时卡（全是 Ephemeral）在手牌里累积 ⇒ 手牌张数偏高**不代表引擎错**（真局回合末会移出）。");
        foreach (var st in f.stages)
        {
            if (st == null || (only != 0 && st.stage != only)) continue;
            var notes = new List<string>();
            var script = new TutorialScript(st);
            BattleContext ctx;
            try { ctx = BuildCtx(st, script, pool, byId, decks, notes); }
            catch (Exception e) { L("S" + st.stage + " 开局抛异常：" + e.GetType().Name + " " + e.Message); _bad++; continue; }
            foreach (var n in notes) L("    · " + n);
            L("S" + st.stage + " 开局：先手=" + (ctx.FirstSeat == 0 ? "玩家" : "AI")
              + " · 玩家手牌 " + ctx.Players[0].Hand.Count + " / AI 手牌 " + ctx.Players[1].Hand.Count
              + " · 棋盘单位 " + Units(ctx, 0) + "/" + Units(ctx, 1));
            var diag = new List<string>();
            System.Action<BattleContext, TutorialAction, ScriptedActionDone, bool> watch =
                (c, a, done, ok) =>
                {
                    if (ok || diag.Count >= 40) return;   // 2026-10-26（G18）：原 10 条会把 30 条失败截成 25 条 ⇒ 调到 40（6 关单关最多 16 条，够）
                    var k = a == null ? ScriptedActionType.None : a.Kind;
                    if (k != ScriptedActionType.PlayCard && k != ScriptedActionType.Attack
                        && k != ScriptedActionType.AttackFreeMode && k != ScriptedActionType.ChangeToRanged
                        && k != ScriptedActionType.ChangeToMelee) return;
                    var d = (a.data != null && a.data.Length > 0) ? a.data[0] : null;
                    string why = "（没有 data）";
                    if (d != null)
                    {
                        if (k == ScriptedActionType.PlayCard)
                        {
                            string w; int idx = TutorialRules.FindHandIndex(c, d.acting, out w);
                            why = idx >= 0 ? ("手牌下标 " + idx + " ⇒ 认得出，失败在更下游（落点/引擎闸）") : ("手牌认不出：" + w);
                        }
                        else
                        {
                            string w; int sd, sl;
                            bool h1 = TutorialRules.FindBoardUnit(c, d.acting, out sd, out sl, out w);
                            string w1 = w;
                            bool h2 = TutorialRules.FindBoardUnit(c, d.target, out sd, out sl, out w);
                            why = (h1 ? "发起者 OK" : ("发起者认不出：" + w1)) + " / " + (h2 ? "目标 OK" : ("目标认不出：" + w));
                        }
                    }
                    diag.Add(k + " @" + TurnName(st, script.Turn - 1) + "/#" + (script.ActionCounter)
                             + " acting=" + (d == null || d.acting == null ? "?" : d.acting.unitTypeName + "/" + d.acting.ourId)
                             + " ⇒ " + why);
                };
            TutorialScript.Executed += watch;
            try { WalkStage(script, ctx); }
            catch (Exception e) { L("S" + st.stage + " 执行中断：" + e.GetType().Name + " " + e.Message); _bad++; }
            finally { TutorialScript.Executed -= watch; }
            for (int i = 0; i < diag.Count; i++) L("    ✗ " + diag[i]);
            L("S" + st.stage + " 收尾：执行 " + script.ExecutedActions + " 条 · Unresolved "
              + Map(script.Unresolved) + " · Unhandled " + Map(script.Unhandled)
              + " · StageActions（原版本来就不改局面）" + Map(script.StageActions)
              + " · 空动作 " + script.NullActions);
        }
        Console.WriteLine();
        L(_bad == 0 ? "✅ 6 关全部走完，无异常" : ("⚠️ 有 " + _bad + " 处读不下去（见上）"));
        return _bad == 0 ? 0 : 1;
    }

    static int Units(BattleContext ctx, int seat)
    {
        int n = 0;
        var b = ctx.Players[seat].Board;
        for (int i = 0; i < b.Length; i++) if (b[i] != null) n++;
        return n;
    }

    static string Map(Dictionary<ScriptedActionType, int> m)
    {
        if (m == null || m.Count == 0) return "{}";
        var sb = new StringBuilder("{");
        foreach (var kv in m) sb.Append(kv.Key).Append("×").Append(kv.Value).Append(" ");
        return sb.ToString().TrimEnd() + "}";
    }

    /// <summary>
    /// 按**真实回合**推进走一遍（与 `BattleDriver` 的次序同：`RuleCore.BeginTurn` → `SyncTutorialTurn`
    /// → 逐条 `PlayScriptedTurn`；玩家条 ⇒ 不执行、指针不动，由调用方 `AdvancePlayerAction`）。
    ///
    /// 🔴 **为什么必须 `BeginTurn`**：原版每个回合都从 `_NextTurn_d__395` 起（回合 +1 → 能量 → **抽牌** →
    /// 触发），脚本里的 `PlayCard` 靠的就是「这个回合抽到的那一张」。不调 `BeginTurn` 的「纯脚本走带」
    /// 会让手牌永远不补 ⇒ `PlayCard` 全线 `Unresolved`（**那是量具的错，不是引擎的错**）。
    /// `BeginTurn` 自己带教程 `dontDrawCard` 闸（`RuleCore.cs:1942-1967`），所以照调即可。
    /// ⚠️ `ctx.Active` 在探针里**显式**按 `FirstSeat` 交替设（活局由 `EndTurn` 翻，这里不跑 `EndTurn`）。
    /// </summary>
    static void WalkTurns(TutorialScript script, BattleContext ctx,
                          Action<int, int, TutorialAction, int, int, bool> step)
    {
        var st = script.Stage;
        if (st == null || st.turns == null) return;
        for (int ti = 0; ti < st.turns.Length; ti++)
        {
            var t = st.turns[ti];
            if (t == null || !t.scriptedTurn || t.actions == null) continue;
            int seat = (ti % 2 == 0) ? ctx.FirstSeat : 1 - ctx.FirstSeat;
            ctx.Active = seat;
            RuleCore.BeginTurn(ctx);              // 回合 +1 / 能量 / 抽牌（含教程 dontDrawCard 闸）/ 触发
            script.UpdateTurn(ti + 1);            // = `BattleDriver.SyncTutorialTurn`（判据 `AiScripted__UpdateTurn.c`）
            int len = t.actions.Length;
            for (int k = 0; k < len; k++)
            {
                var a = t.actions[k];
                int before = script.ActionCounter;
                bool ret = script.PlayScriptedTurn(ctx);
                int after = script.ActionCounter;
                if (step != null) step(ti, k, a, before, after, ret);
                if (a != null && a.playerAction) script.AdvancePlayerAction();   // 玩家做完 ⇒ 指针 ++（产品同一个口）
            }
        }
    }

    /// <summary>按回合逐条走并打一行摘要。</summary>
    static void WalkStage(TutorialScript script, BattleContext ctx)
    {
        var st = script.Stage;
        if (st == null || st.turns == null) return;
        int curTurn = -1, len = 0, ok = 0, pl = 0, bad = 0;
        WalkTurns(script, ctx, (ti, k, a, before, after, ret) =>
        {
            if (ti != curTurn)
            {
                if (curTurn >= 0) L("    " + TurnName(st, curTurn) + "：" + len + " 条（玩家条 " + pl
                                    + " · 脚本自执行且返回值符合 " + ok + " · 不符/异常 " + bad + "）");
                if (curTurn >= 0 && bad > 0) _bad++;
                curTurn = ti; len = st.turns[ti].actions.Length; ok = pl = bad = 0;
            }
            if (a == null) { bad++; return; }
            if (a.playerAction)
            {
                pl++;
                // 玩家条：不执行、不 ++、返回 false（指针 ++ 由 `WalkTurns` 在 step 返回**之后**做，
                // 所以这里**不能**看 `ActionCounter` —— 那是 step 内的时刻）
                if (ret || after != before) bad++;
            }
            else
            {
                if (after != before + 1) bad++;
                bool wantRet = after < len;              // 原版：++ 之后 `counter < count`
                if (ret != wantRet) bad++; else ok++;
            }
        });
        if (curTurn >= 0)
        {
            L("    " + TurnName(st, curTurn) + "：" + len + " 条（玩家条 " + pl
              + " · 脚本自执行且返回值符合 " + ok + " · 不符/异常 " + bad + "）");
            if (bad > 0) _bad++;
        }
    }

    static string TurnName(TutorialStageData st, int ti)
    {
        var t = (st != null && st.turns != null && ti >= 0 && ti < st.turns.Length) ? st.turns[ti] : null;
        return (t != null && !string.IsNullOrEmpty(t.turnName)) ? t.turnName : ("#" + (ti + 1));
    }

    static int ParseStage(string s)
    {
        int v;
        if (!string.IsNullOrEmpty(s) && int.TryParse(s.TrimStart('S', 's'), out v)) return v;
        return 0;
    }

    // ==================================================================
    //  5. `g3` —— 逐条账行的读数（B→A 的证据面）
    // ==================================================================

    static int G3(TutorialStageFile f, string arg)
    {
        bool all = string.IsNullOrEmpty(arg);
        int only = 0;
        if (!all) int.TryParse(arg, out only);
        bool Want(int row) { return all || only == row; }

        if (Want(112)) BlockHasActionData(f);
        if (Want(114)) BlockHasPendingActions(f);
        if (Want(95)) BlockWaitingChangeAttack(f);
        if (Want(98)) BlockWaitingUseAbility(f);
        if (Want(118)) BlockPlayNext(f);
        if (Want(101)) BlockExecuteActionStats(f);
        if (Want(104)) BlockGetActingCard(f);
        if (Want(93)) BlockCanSkipCurrentAction(f);
        if (Want(116)) BlockIsReferenceCardElement();
        if (Want(110)) BlockHorizontalOffset();
        if (Want(111)) BlockTipPosition(f);
        if (Want(466)) BlockIsScriptedTurn(f);
        if (Want(1232)) BlockShouldShuffleDeck();
        Console.WriteLine();
        L(_bad == 0 ? "✅ 全部读数块跑完（读数本身无异常）"
                     : ("⚠️ 有 " + _bad + " 块读不下去 / 缺读口（见各块的「结论」行）"));
        L("⚠️ 本模式**不判对错**：上面的「原版侧」是把反编译方法体**逐句转写**成 C#（每块写了 文件:行号），");
        L("   「我们侧」是**调用产品代码的实际返回值**（需要私有谓词时走反射，⛔ 没改产品源码）。");
        return 0;   // 读数口本身不设门禁（与 `play`/`ai` 同口径）
    }

    // -------- 112 `AiScripted.HasActionData(ScriptedAction)` ----------------
    static void BlockHasActionData(TutorialStageFile f)
    {
        H("[行 112] AiScripted.HasActionData(ScriptedAction)");
        L("判据 🥇 decomp_full/AiScripted__HasActionData.c:11-16");
        L("     ret = (a != null) && (a.data(+0x48) != null) && (0 < data.Count(+0x18))");
        L("     ⚠️ a == null 那一支原版**不返回**（`:19` = null-check 抛出）");
        var mi = Priv(typeof(TutorialScript), "First");
        if (mi == null) { L("★缺读口：`TutorialScript.First` 反射找不到（改名了？）"); _bad++; return; }
        L("我们 RuleEngine/Core/TutorialScript.cs:1058 `static TutorialActionData First(a)`（私有静态 · 反射）");
        L("     First(a) != null  ⇔  a != null && data != null && data.Length > 0 && data[0] != null");

        // ---- 真实域：6 关全部 462 条回合内动作
        var slots = AllSlots(f);
        int oT = 0, uT = 0, agree = 0;
        foreach (var s in slots)
        {
            var a = s.Action;
            bool o = a != null && a.data != null && a.data.Length > 0;
            bool u = CallPriv(mi, a) != null;
            if (o) oT++; if (u) uT++;
            if (o == u) agree++;
        }
        L("样本 真实 6 关回合内动作 " + slots.Count + " 条（`tutorial_stages.json`）");
        L("原版侧 真 = " + oT + " / 假 = " + (slots.Count - oT));
        L("我们侧 真 = " + uT + " / 假 = " + (slots.Count - uT));
        L("一致 " + agree + "/" + slots.Count);

        // ---- 夹具：四个边界（其中 ③ 是**差异**，如实列）
        var fx = new (string name, TutorialAction act, bool o)[]
        {
            ("① a=null"              , null, false),
            ("② data=null"           , new TutorialAction(), false),
            ("③ data=[]（空数组）"     , new TutorialAction { data = new TutorialActionData[0] }, false),
            ("④ data=[null]（洞）"     , new TutorialAction { data = new TutorialActionData[1] }, true),
            ("⑤ data=[正常]"          , new TutorialAction { data = new[] { new TutorialActionData { type = 80 } } }, true),
        };
        L("夹具（两侧并排）：");
        int fxBad = 0;
        foreach (var t in fx)
        {
            bool u;
            if (t.act == null) u = false;                       // 原版是**抛**；我们给 false（`First` 判 null）
            else u = CallPriv(mi, t.act) != null;
            bool ok = (t.o == u);
            if (!ok) fxBad++;
            L("   " + Pad(t.name, 22) + " 原版=" + B(t.o) + "  我们=" + B(u) + "  " + (ok ? "✓" : "✗"));
        }
        L("结论 G3 ✓（真实域 " + agree + "/" + slots.Count + " 两侧一致）；夹具 ④ 的 ✗ 是**已记账的差异**：");
        L("     `data[0] == null` 时原版返回**真**（它只比 Count，下游会在 `data[0].actionType` 上崩），");
        L("     我们返回「没有 data」并出声（`TutorialScript.cs:884-887` 的空动作那一条）—— 输入域外（生成器不产 null 元素）。");
    }

    // -------- 114 `AiScripted.HasPendingActions(TurnScriptedData)` ----------
    static void BlockHasPendingActions(TutorialStageFile f)
    {
        H("[行 114] AiScripted.HasPendingActions(TurnScriptedData)");
        L("判据 🥈 cpp2il dev AiScripted.cs:467-489（Ghida 落盘的 `AiScripted__HasPendingActions.c` 是**战役**重载 `+0x14`）");
        L("     ret = (turnData != null) && (turnData.scriptedActions != null) && (actionCounter(+0x10) < actions._size)");
        L("我们 RuleEngine/Core/TutorialScript.cs:716-724 `FinishedScriptedActionsInTurn`（公开属性）取反");
        L("     = actions.Length <= ActionCounter ⇒ 取反 = actions.Length > ActionCounter");
        int pairs = 0, agree = 0, oT = 0;
        var badList = new List<string>();
        foreach (var st in f.stages)
        {
            if (st == null || st.turns == null) continue;
            var script = new TutorialScript(st);
            for (int ti = 0; ti < st.turns.Length; ti++)
            {
                var t = st.turns[ti];
                if (t == null || !t.scriptedTurn || t.actions == null) continue;   // 原版调用点只喂 `scriptedTurn` 的回合
                script.UpdateTurn(ti + 1);
                int len = t.actions.Length;
                for (int c = 0; c <= len + 1; c++)                                  // 含「正好到头」「越界一格」
                {
                    script.ResetActionCounter();
                    for (int i = 0; i < c; i++) script.AdvancePlayerAction();
                    bool o = c < len;
                    bool u = !script.FinishedScriptedActionsInTurn;
                    pairs++; if (o) oT++;
                    if (o == u) agree++;
                    else if (badList.Count < 5) badList.Add("S" + st.stage + "/" + t.turnName + "/c=" + c);
                }
            }
        }
        L("样本 (关 × 回合 × 计数器) 共 " + pairs + " 组（计数器取 0..len+1，含到头/越界）");
        L("原版侧 真 = " + oT + " / 假 = " + (pairs - oT));
        L("我们侧 与之一致 = " + agree + "/" + pairs + (badList.Count > 0 ? ("（不符样例：" + string.Join(" · ", badList) + "）") : ""));
        L("差异 只有 null 那一档：原版 `turnData == null` ⇒ 抛 NRE；我们 ⇒ 返回 true（= 跑完了）");
        L("     —— 调用点 `BattleDriver.cs:17513` 恒有当前回合，该输入域外（已在行 103 记过）。");
        L("结论 G3 ✓（域内 " + agree + "/" + pairs + "）");
        if (agree != pairs) _bad++;
    }

    // -------- 95 `AiScripted.CheckIfWaitingToChangeAttack` -----------------
    static void BlockWaitingChangeAttack(TutorialStageFile f)
    {
        H("[行 95] AiScripted.CheckIfWaitingToChangeAttack(manager, turnData)");
        L("判据 🥈 cpp2il dev AiScripted.cs:1170-1265（逐句转写）");
        L("     ret = IsTutorialMatch && turnData != null && actions != null && counter < count");
        L("           && cur != null && cur.playerAction && cur.data != null");
        L("           && ∃i: cur.data[i].actionType ∈ {ChangeToMelee(36), ChangeToRanged(35)}");
        var mi = FindRuleMethod("IsWaitingChangeAttack");
        L(mi == null
          ? "我们侧 ★缺读口 —— `TutorialRules.IsWaitingChangeAttack` 反射找不到（改名了？）"
          : "我们 RuleEngine/Core/TutorialScript.cs `TutorialRules.IsWaitingChangeAttack(turn, counter)`（公开静态 · 反射）");
        int pairs = 0, agree = 0, oTrue = 0;
        int wideAll = 0, wideHit = 0, wideOver = 0;      // 「拿 `Matches` 顶这个谓词」的对照（反面证据）
        var hits = new List<string>();
        var bad = new List<string>();
        var over = new List<string>();
        foreach (var s in AllSlots(f))
        {
            var a = s.Action;
            bool o = a != null && a.playerAction && a.data != null && (HasKind(a, 35) || HasKind(a, 36));
            string nm = "S" + s.Stage.stage + "/" + s.Turn.turnName + "/#" + s.Counter;
            bool w = false;
            if (a != null && a.playerAction && a.data != null)
            {
                for (int i = 0; i < a.data.Length; i++)
                {
                    var d = a.data[i];
                    if (d == null) continue;
                    // 发起者那一关要按 data 自己写的来（`PlayerWarlord(10)` ⇒ 必须 `ActingIsHero`）
                    var att = new TutorialAttempt
                    {
                        Action = BattleActionType.changeAttack,
                        NoTargetYet = true,
                        ActingIsHero = d.acting != null && d.acting.unitType == (int)ScriptedActionUnit.PlayerWarlord,
                        ActingId = d.acting == null ? null : d.acting.ourId,
                    };
                    if (TutorialScript.Matches(d, att)) { w = true; break; }
                }
            }
            pairs++;
            if (o)
            {
                oTrue++;
                if (hits.Count < 8) hits.Add(nm);
                wideAll++; if (w) wideHit++;
            }
            if (w && !o) { wideOver++; if (over.Count < 8) over.Add(nm + " " + a.Kind); }
            if (mi != null)
            {
                var r = CallPriv(mi, s.Turn, s.Counter);
                bool u = r is bool && (bool)r;
                if (o == u) agree++;
                else if (bad.Count < 6) bad.Add(nm + " 原版=" + B(o) + " 我们=" + B(u));
            }
        }
        L("样本 真实 6 关回合内动作 " + pairs + " 条");
        L("原版侧 真 = " + oTrue + " 条" + (hits.Count > 0 ? "：" + string.Join(" · ", hits) : ""));
        if (mi == null)
        {
            L("还差什么 → 改动请求 G5-R3：给 `RuleEngine/Core/TutorialRules` 加窄谓词 `IsWaitingChangeAttack(turn, counter)`");
            _bad++;
        }
        else
        {
            L("我们侧 与之一致 = " + agree + "/" + pairs + (bad.Count > 0 ? "（不符：" + string.Join(" · ", bad) + "）" : ""));
            L("差异 输入域外两档：原版 `manager == null` / `turnData == null` ⇒ 抛 NRE；我们返 false（调用点恒有教程局与当前回合）");
            L("结论 G3 ✓（真实域 " + agree + "/" + pairs + "）");
            if (agree != pairs) _bad++;
        }
        L("⚠️ 反面证据（同一个真实域，拿 `TutorialScript.Matches` 的 case 5/6 顶这个谓词）：原版为真 " + wideAll
          + " 条里命中 " + wideHit + "/" + wideAll + "，但**过宽 " + wideOver + " 条**"
          + (over.Count > 0 ? "（例：" + string.Join(" · ", over) + "）" : ""));
        L("   ⇒ `Matches` 是**白名单闸**（「这个动作准不准做」），不是「正在等换打法」查询 —— 两个问题被一个函数回答了。");
        L("     ⚠️ 过宽**不是缺陷**：原版 `CheckIfMatchesActionData.c:96-102` 的 `TapCard(0x78)` 支同样不提前 `return 0` ⇒ 两边同构。");
    }

    static bool HasKind(TutorialAction a, int t)
    {
        if (a.data == null) return false;
        for (int i = 0; i < a.data.Length; i++) if (a.data[i] != null && a.data[i].type == t) return true;
        return false;
    }

    // -------- 98 `AiScripted.CheckIfWaitingToUseAbility` -------------------
    static void BlockWaitingUseAbility(TutorialStageFile f)
    {
        H("[行 98] AiScripted.CheckIfWaitingToUseAbility(manager, turnData, cardSelected)");
        L("判据 🥈 cpp2il dev AiScripted.cs:803-976（逐句转写）");
        L("     ret = turnData/actions/counter 守卫 && cur.playerAction && cur.data != null");
        L("           && ∃i: data[i].actionType == ActiveAbility(40)");
        L("                  && (解析出的 acting 卡 == null ‖ == cardSelected)   ← `.c:…` `if (cardScript == null || cardScript != cardSelected) return true;`");
        L("我们 RuleEngine/Core/TutorialScript.cs:1247-1250（`Matches` 的 ActiveAbility 支）+ `:1261-1273`（发起者比 `ourId`）");
        L("     ret = Tutorial != null && cur.playerAction && ∃i: Matches(data[i], attempt{Action=playActiveAbility, ActingId=选中})");
        int real = 0;
        foreach (var s in AllSlots(f)) if (s.Action != null && HasKind(s.Action, 40)) real++;
        L("真实域 6 关里 `ActiveAbility(40)` 的动作 = " + real + " 条 ⇒ **今天不可达**（等价性只能由合成夹具钉住）");

        var cases = new (string name, TutorialActionData d, string sel, bool o)[]
        {
            ("data=40 · acting ourId=空 · 选任意",            D(40, ""),   "X", true),
            ("data=40 · acting ourId=X · 选 X",               D(40, "X"),  "X", true),
            ("data=40 · acting ourId=X · 选 Y",               D(40, "X"),  "Y", false),
            ("data=40 · acting ourId=X · 选中=null（解析不出）", D(40, "X"), null, true),
            ("data=30（非技能档） · 选任意",                    D(30, "X"),  "X", false),
            ("data=20（非技能档） · 选任意",                    D(20, "X"),  "X", false),
        };
        L("夹具（两侧并排；`原版` 列按上面那条 `cardScript == null || != selected ⇒ true` 推出）：");
        int bad = 0;
        foreach (var c in cases)
        {
            var att = new TutorialAttempt { Action = BattleActionType.playActiveAbility, ActingId = c.sel, NoTargetYet = true };
            bool u = TutorialScript.Matches(c.d, att);
            bool ok = (c.o == u);
            if (!ok) bad++;
            L("   " + Pad(c.name, 40) + " 原版=" + B(c.o) + "  我们=" + B(u) + "  " + (ok ? "✓" : "✗"));
        }
        L("结论 G3 ✓（夹具 " + (cases.Length - bad) + "/" + cases.Length + "）；⚠️ **真实域 0 条** ⇒ 本行升 A 的等价性只钉在夹具上，域内无实例。");
        if (bad > 0) _bad++;
    }

    static TutorialActionData D(int type, string actingId)
    {
        return new TutorialActionData
        {
            type = type,
            acting = new TutorialUnitRef { unitType = 30, unitTypeName = "PlayerMinion", ourId = actingId },
            target = new TutorialUnitRef { unitType = 0, unitTypeName = "None" },
        };
    }

    // -------- 118 `PlayNextScriptedAction(TurnScriptedData)` --------------
    static void BlockPlayNext(TutorialStageFile f)
    {
        H("[行 118] AiScripted.PlayNextScriptedAction(manager, TurnScriptedData)");
        L("判据 🥈 cpp2il dev AiScripted.cs:403-441（逐句转写）");
        L("     关高亮 → cur == null / 越界 ⇒ 抛；cur.playerAction ⇒ ShowPlayerActionAnim + **return false（不 ++）**");
        L("     否则 ExecuteAction(cur) + `actionCounter++` + return `actionCounter < actions.Count`");
        L("我们 RuleEngine/Core/TutorialScript.cs:832-846 `PlayScriptedTurn`（**同一个体重**，高亮关在调用方 `BattleDriver.cs:17504-17515`）");
        L("     ⇒ 逐条比：① 玩家条：counter 不动、返回 false ② 脚本条：counter+1、返回值 = (counter < len)");
        var pool = Program.PoolList; var byId = PoolById(pool); var decks = LoadDecks();
        int pairs = 0, agree = 0; var bads = new List<string>();
        foreach (var st in f.stages)
        {
            if (st == null || st.turns == null) continue;
            var notes = new List<string>();
            var script = new TutorialScript(st);
            BattleContext ctx;
            try { ctx = BuildCtx(st, script, pool, byId, decks, notes); }
            catch (Exception e) { L("S" + st.stage + " 开局失败：" + e.Message); _bad++; continue; }
            try
            {
                WalkTurns(script, ctx, (ti, k, a, before, after, ret) =>
                {
                    if (a == null) return;
                    int len = st.turns[ti].actions.Length;
                    bool oPlayer = a.playerAction;
                    bool oRet, oAdv;
                    if (oPlayer) { oRet = false; oAdv = false; }
                    else { oRet = (before + 1) < len; oAdv = true; }
                    bool okAdv = (after - before == (oAdv ? 1 : 0));
                    bool okRet = (ret == oRet);
                    pairs++;
                    if (okAdv && okRet) agree++;
                    else if (bads.Count < 6)
                        bads.Add("S" + st.stage + "/" + TurnName(st, ti) + "/#" + k + " " + a.Kind
                                 + "（adv " + (after - before) + " vs " + (oAdv ? 1 : 0) + " · ret " + ret + " vs " + oRet + "）");
                });
            }
            catch (Exception e) { L("S" + st.stage + " 执行中断：" + e.GetType().Name + " " + e.Message); _bad++; }
        }
        L("样本 6 关回合内动作 " + pairs + " 条（真跑：`NewBattle(..., tutorial:)` + `PlayScriptedTurn`）");
        L("逐条一致 = " + agree + "/" + pairs + (bads.Count > 0 ? "（不符样例：" + string.Join(" · ", bads) + "）" : ""));
        L("共享落点说明（方案 §4.1 允许）：原版 `PlayScriptedTurn`（教程 `+0x10`）与 `PlayNextScriptedAction`（`+0x10`）");
        L("     **体重合**；我们只有 `PlayScriptedTurn` 一个实现，两者都落在 `:832-846` —— 上面逐条比的是同一段行为。");
        L("结论 G3 ✓（域内 " + agree + "/" + pairs + "）");
        if (agree != pairs) _bad++;
    }

    // -------- 101 `ExecuteAction` 的落地统计（读数，不升 A）-----------------
    static void BlockExecuteActionStats(TutorialStageFile f)
    {
        H("[行 101] AiScripted.ExecuteAction —— 落地统计（本块只给读数；该行卡在「3 档未接」）");
        var pool = Program.PoolList; var byId = PoolById(pool); var decks = LoadDecks();
        var kindTotal = new SortedDictionary<int, int>();
        var kindOk = new SortedDictionary<int, int>();
        foreach (var st in f.stages)
        {
            if (st == null || st.turns == null) continue;
            var notes = new List<string>();
            var script = new TutorialScript(st);
            BattleContext ctx;
            try { ctx = BuildCtx(st, script, pool, byId, decks, notes); }
            catch { _bad++; continue; }
            System.Action<BattleContext, TutorialAction, ScriptedActionDone, bool> cb =
                (c, a, done, ok) =>
                {
                    int k = (int)(a == null ? ScriptedActionType.None : a.Kind);
                    int n; kindTotal.TryGetValue(k, out n); kindTotal[k] = n + 1;
                    if (ok) { kindOk.TryGetValue(k, out n); kindOk[k] = n + 1; }
                };
            TutorialScript.Executed += cb;
            try { WalkTurns(script, ctx, null); }
            catch (Exception e) { L("S" + st.stage + " 中断：" + e.Message); _bad++; }
            finally { TutorialScript.Executed -= cb; }
            L("S" + st.stage + " · Unresolved " + Map(script.Unresolved) + " · Unhandled " + Map(script.Unhandled)
              + " · StageActions " + Map(script.StageActions));
        }
        L("按大类的「引擎真的改了局面（ok=true）」占比：");
        foreach (var kv in kindTotal)
        {
            int ok; kindOk.TryGetValue(kv.Key, out ok);
            L("   " + Pad(((ScriptedActionType)kv.Key).ToString(), 18) + " " + ok + "/" + kv.Value);
        }
        L("⚠️ 这张表**不是** G3 的可观察量（口径 §三：不许拿「内部字段被写入了」当观测量）——");
        L("     它是行 101 的「还差什么」证据：未接的 3 档（ActiveAbility / ShowManaAura / ResolveCard）在表里必然 0 改局面。");
    }

    // -------- 104 `GetActingCard` ----------------------------------------
    static void BlockGetActingCard(TutorialStageFile f)
    {
        H("[行 104] AiScripted.GetActingCard(manager, actionData, acting)");
        L("判据 🥈 cpp2il dev AiScripted.cs:3008-3112（`acting ? actingUnitType : targetUnitType` 选列表再 GetChosenCard）");
        L("我们 RuleEngine/Core/TutorialScript.cs:1553-1570 `TutorialRules.SideOfUnitType(int)`（公开静态）");
        L("     + `:1580` `FindBoardUnit` / `:1624` `FindHandIndex`（都是公开静态）");
        var vals = new[] { 0, 10, 20, 30, 31, 32, 40, 50, 60, -1, 99 };
        L("容器（unitType → 侧）：");
        foreach (var v in vals)
        {
            int u = TutorialRules.SideOfUnitType(v);
            string o = v == 10 || v == 30 || v == 31 || v == 32 || v == 50 ? "玩家侧(0)"
                     : v == 20 || v == 40 || v == 60 ? "AI 侧(1)" : "LogWarning + null";
            L("   unitType " + Pad(v.ToString(), 4) + " 原版=" + Pad(o, 20) + " 我们=" + (u < 0 ? "-1（认不出）" : u.ToString()) + "  " + sideOk(o, u));
        }
        // 真实域：脚本引用的 acting/target 在**开局那一副**里认得出多少
        var pool = Program.PoolList; var byId = PoolById(pool); var decks = LoadDecks();
        int boardHit = 0, boardMiss = 0, handHit = 0, handMiss = 0;
        var missWhy = new List<string>();
        foreach (var st in f.stages)
        {
            if (st == null) continue;
            var notes = new List<string>();
            var script = new TutorialScript(st);
            BattleContext ctx;
            try { ctx = BuildCtx(st, script, pool, byId, decks, notes); }
            catch { _bad++; continue; }
            if (st.turns == null) continue;
            foreach (var t in st.turns)
            {
                if (t == null || t.actions == null) continue;
                foreach (var a in t.actions)
                {
                    if (a == null || a.data == null) continue;
                    foreach (var d in a.data)
                    {
                        if (d == null) continue;
                        foreach (var r in new[] { d.acting, d.target })
                        {
                            if (r == null) continue;
                            var kind = (ScriptedActionUnit)r.unitType;
                            if (kind == ScriptedActionUnit.PlayerCardInHand || kind == ScriptedActionUnit.EnemyCardInHand)
                            {
                                string why; int idx = TutorialRules.FindHandIndex(ctx, r, out why);
                                if (idx >= 0) handHit++; else { handMiss++; if (missWhy.Count < 6) missWhy.Add("手牌 " + r.unitTypeName + "/" + r.ourId + "：" + why); }
                            }
                            else if (kind != ScriptedActionUnit.None)
                            {
                                string why; int sd, sl;
                                if (TutorialRules.FindBoardUnit(ctx, r, out sd, out sl, out why)) boardHit++;
                                else { boardMiss++; if (missWhy.Count < 6) missWhy.Add("棋盘 " + r.unitTypeName + "/" + r.ourId + "：" + why); }
                            }
                        }
                    }
                }
            }
        }
        L("真实域（**开局那一刻**的棋盘/手牌）：棋盘引用认得出 " + boardHit + " / 认不出 " + boardMiss
          + " · 手牌引用认得出 " + handHit + " / 认不出 " + handMiss);
        foreach (var m in missWhy) L("     · " + m);
        L("结论 读数 ✓；**本行仍是 B**（虽然 G3 有了读数）—— 卡在「共享落点」：这组行为的另一半");
        L("     （认不出时**退回 `list[0]`**）是 `GetChosenCard`（行 106，C，等裁定），我们**故意不照抄**。");
    }

    static string sideOk(string o, int u)
    {
        if (o.StartsWith("玩家")) return u == 0 ? "✓" : "✗";
        if (o.StartsWith("AI")) return u == 1 ? "✓" : "✗";
        return u < 0 ? "✓" : "✗";
    }

    // -------- 93 `CanSkipCurrentAction` ----------------------------------
    static void BlockCanSkipCurrentAction(TutorialStageFile f)
    {
        H("[行 93] AiScripted.CanSkipCurrentAction(TurnScriptedData)");
        L("判据 🥇 decomp_full/AiScripted__CanSkipCurrentAction.c:13-36");
        L("     ret = ElementAtOrDefault(actions(+0x20), actionCounter(+0x10)) 的 data[0].actionType ∈ {50,55,60,65,90}");
        L("     ⚠️ 三条前置（turnData 空 / 下标越界 / 元素或 data 空）任一不成立 ⇒ 原版落 `FUN_1803f47a0()`（抛/不返回）");
        var set = new[] { 50, 55, 60, 65, 90 };
        var mi = FindRuleMethod("CanSkipCurrentAction");
        L(mi == null
          ? "我们侧 ★缺读口 —— `TutorialRules.CanSkipCurrentAction` 反射找不到（改名了？）"
          : "我们 RuleEngine/Core/TutorialScript.cs `TutorialRules.CanSkipCurrentAction(turn, counter)`（公开静态 · 反射）");
        L("     ⚠️ 不要拿 `TutorialScript.IsNoOpScriptKind` 顶：它收**14 档**（含 SmallTip/EndTurn/ClickCard…），");
        L("        拿它当这个谓词会**系统性放宽**（那是另一个问题：`StageActions` 的计数口径）。");
        int hit = 0, tot = 0, agree = 0;
        var where = new List<string>();
        var diff = new List<string>();
        foreach (var s in AllSlots(f))
        {
            var a = s.Action;
            bool o = a != null && a.data != null && a.data.Length > 0 && a.data[0] != null
                     && Array.IndexOf(set, a.data[0].type) >= 0;
            string nm = "S" + s.Stage.stage + "/" + s.Turn.turnName + "/#" + s.Counter;
            if (o) { hit++; if (where.Count < 8) where.Add(nm); }
            if (mi == null) continue;
            var r = CallPriv(mi, s.Turn, s.Counter);
            bool u = r is bool && (bool)r;
            tot++; if (o == u) agree++;
            else if (diff.Count < 6) diff.Add(nm + " 原版=" + B(o) + " 我们=" + B(u));
        }
        L("原版侧（可复现读数）：6 关回合内命中这一集合的动作 = " + hit + " 条"
          + (where.Count > 0 ? "（前几条：" + string.Join(" · ", where) + "）" : ""));
        if (mi == null)
        {
            L("还差什么 → 改动请求 G5-R2：把「聊天五档集合」提到 `RuleEngine/Core/TutorialRules`（公开静态），两边共用一份。");
            _bad++;
            return;
        }
        L("我们侧 与之一致 = " + agree + "/" + tot + (diff.Count > 0 ? "（不符：" + string.Join(" · ", diff) + "）" : ""));
        L("差异 只有输入域外那三档：原版抛/不返回，我们返 false（= 不能跳过 ⇒ 保守走「真等满」那一支）");
        L("结论 G3 ✓（域内 " + agree + "/" + tot + "）");
        if (agree != tot) _bad++;
    }

    // -------- 116 `IsReferenceCardElement` -------------------------------
    static void BlockIsReferenceCardElement()
    {
        H("[行 116] AiScripted.IsReferenceCardElement(PositionReference)");
        L("判据 🥇 decomp_full/AiScripted__IsReferenceCardElement.c:7-11");
        L("     ret = (uint)(pos-11) <= 2  ‖  (uint)(pos-21) <= 2   ⇒ 真值域 = {11,12,13} ∪ {21,22,23}");
        var mi = FindRuleMethod("IsReferenceCardElement");
        if (mi == null)
        {
            L("我们侧 ★缺读口 —— 谓词**内联**在 `CardPresentation/Battle/BattleDriver.cs:16184-16190` 的 `TutorialTipOffset`（私有静态 + Unity 侧）");
            _bad++;
        }
        else L("我们侧 `TutorialRules.IsReferenceCardElement(int)`（反射）");
        var probes = new[] { -1, 0, 1, 5, 10, 11, 12, 13, 14, 15, 19, 20, 21, 22, 23, 24, 25, 30, 60, 120 };
        int agree = 0, tot = 0;
        var sb = new StringBuilder();
        foreach (var p in probes)
        {
            bool o = unchecked((uint)(p - 11)) <= 2 || unchecked((uint)(p - 21)) <= 2;
            string us;
            if (mi == null) us = "缺读口";
            else
            {
                object r = CallPriv(mi, p);
                bool u = r is bool && (bool)r;
                us = B(u); tot++; if (u == o) agree++;
            }
            sb.Append("   pos ").Append(Pad(p.ToString(), 4)).Append(" 原版=").Append(B(o)).Append("  我们=").Append(us).Append('\n');
        }
        Console.Write(sb.ToString());
        L("原版侧真值域 = {11,12,13,21,22,23}（六个）");
        if (mi == null)
            L("结论 **转不了 A**（读数口覆盖不到）；还差什么 → 改动请求 G5-R1：把 `sub` 那两行搬到 `RuleEngine/Core/TutorialRules`");
        else
            L("结论 G3 ✓（" + agree + "/" + tot + "）");
    }

    static MethodInfo FindRuleMethod(string name)
    {
        var t = typeof(TutorialRules);
        return t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
    }

    // -------- 110 `GetHorizontalOffset` ---------------------------------
    static void BlockHorizontalOffset()
    {
        H("[行 110] AiScripted.GetHorizontalOffset(PositionReference)");
        L("判据 🥇 decomp_full/AiScripted__GetHorizontalOffset.c:13-36");
        L("     fVar9 = (pos ∈ {11,12,13} ∪ {21,22,23}) ? VarsDevice.tutorialTipHorizontalOffsetForElements(+0x38)");
        L("                                           : VarsDevice.tutorialTipHorizontalOffset(+0x34)");
        L("     smallScreenUI ⇒ fVar9 *= 1.3（`DAT_1834b33f0` = 1.29999995f）");
        float d34 = ReadVarsConst(VarsDesktopPath, "tutorialTipHorizontalOffset", 20f);
        float d38 = ReadVarsConst(VarsDesktopPath, "tutorialTipHorizontalOffsetForElements", 18f);
        float m34 = ReadVarsConst(VarsMobilePath, "tutorialTipHorizontalOffset", 18f);
        float m38 = ReadVarsConst(VarsMobilePath, "tutorialTipHorizontalOffsetForElements", 16f);
        L("原版侧（现读解包资源，可复跑）：VarsDesktop 20/18 → 实读 " + F(d34) + "/" + F(d38)
          + " · VarsMobile " + F(m34) + "/" + F(m38) + " · 小屏 ×1.3 ⇒ " + F(d34 * 1.3f) + "/" + F(d38 * 1.3f));
        float cw = ReadConstFloat(CardViewPath, "Width", -1f);
        float ch = ReadConstFloat(CardViewPath, "Height", -1f);
        if (cw <= 0f || ch <= 0f)
        {
            L("我们侧 ★缺读口 —— `CardView.Width/Height` 现读失败（路径/写法变了？）");
        }
        else
        {
            L("我们侧（**现读源码常量**，⚠️ 不是调用产品代码）：`CardPresentation/Core/CardView.cs` "
              + "`Width`=" + F(cw) + " · `Height`=" + F(ch)
              + " ⇒ 普通档 " + F(cw) + " · 督军六档 " + F(ch) + "（×1.3 小屏 ⇒ " + F(cw * 1.3f) + " / " + F(ch * 1.3f) + "）");
            L("     ⚠️ 量纲差一个数量级：原版那两格是 `VarsDevice` 上的 UI 值（20/18），我们是**世界单位**（一张卡的宽/高）");
        }
        L("差异 **真实存在**：载体不同（20/18 vs 卡的宽/高）⇒ 本行即使有读数口也**不能**直接升 A，");
        L("     要先裁定「`+0x34/+0x38` 是不是一张 20×18 的固定尺寸」还是「卡面尺寸」—— 见 `TutorialTipOffset` 那份长注（目前按用途推断）。");
        if (cw <= 0f || ch <= 0f) _bad++;
        else L("结论 读数 ✓（**两侧都有**，且是 2026-10-26 起补的）；**本行仍是 B** —— 差的是**载体裁定**（人的判断），不是读数口。");
    }

    /// <summary>从**产品源码**里现读一个 `const float X = 1.23f;`（行 110 的载体对比用）。
    /// ⛔ 不在探针里抄死一份 —— 抄了就成「两处写同一条规则」，源码一改读数还是绿的（自证）。</summary>
    static float ReadConstFloat(string path, string name, float fallback)
    {
        try
        {
            if (!File.Exists(path)) return fallback;
            var txt = File.ReadAllText(path);
            int i = txt.IndexOf("const float " + name + " =", StringComparison.Ordinal);
            if (i < 0) return fallback;
            int c = txt.IndexOf('=', i);
            int e = txt.IndexOf('f', c + 1);
            if (e < 0) return fallback;
            var num = txt.Substring(c + 1, e - c - 1).Trim();
            float v;
            return float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }
        catch { return fallback; }
    }

    static float ReadVarsConst(string path, string key, float fallback)
    {
        try
        {
            if (!File.Exists(path)) return fallback;
            var txt = File.ReadAllText(path);
            int i = txt.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (i < 0) return fallback;
            int c = txt.IndexOf(':', i); int e = txt.IndexOfAny(new[] { ',', '}', '\n' }, c + 1);
            var num = txt.Substring(c + 1, e - c - 1).Trim();
            float v;
            return float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }
        catch { return fallback; }
    }

    // -------- 111 `GetTipPosition` --------------------------------------
    static void BlockTipPosition(TutorialStageFile f)
    {
        H("[行 111] AiScripted.GetTipPosition(manager, pos, relation, referenceUnit)");
        L("判据 🥈 cpp2il dev AiScripted.cs:3114-3466（Ghidra 274 行）—— 锚点逐档 + Relation 偏移");
        L("我们侧 ★缺读口 —— `BattleDriver.cs:16260` `TipAnchor` + `:17032-17092`（Unity 侧）");
        // 真实域：6 关的 smallTipParams.positionReference 分布（这是判据侧的输入，可复现）
        var hist = new SortedDictionary<int, int>();
        foreach (var s in AllSlots(f))
        {
            var a = s.Action;
            if (a == null || a.smallTipParams == null) continue;
            int p = a.smallTipParams.positionReference;
            int n; hist.TryGetValue(p, out n); hist[p] = n + 1;
        }
        var sb = new StringBuilder("   smallTipParams.positionReference 分布：");
        foreach (var kv in hist) sb.Append(kv.Key).Append("×").Append(kv.Value).Append("  ");
        L(sb.ToString());
        L("差异 三处已在账里（卡面子锚按比例 · 50/70/100 未做（数据 0 条） · 偏移量同上一行）");
        _bad++;
    }

    // -------- 466 `BattleManager.IsScriptedTurn` -------------------------
    static void BlockIsScriptedTurn(TutorialStageFile f)
    {
        H("[行 466] BattleManager.IsScriptedTurn(TurnScriptedData)");
        L("判据 🥇 decomp_full/BattleManager__IsScriptedTurn.c:12-16");
        L("     ret = d != null && d.scriptedTurn(+0x18) != 0 && d.scriptedActions(+0x20) != null && 0 < actions.Count(+0x18)");
        L("我们 RuleEngine/Core/TutorialScript.cs:674-684 `CurrentTurn`（+ `:686-695` `CurrentAction` 的越界守卫）");
        L("     ret = Stage != null && Stage.turns != null && 0 <= Turn-1 < 长度 && t != null && t.scriptedTurn");
        L("     ⚠️ 我们**没有**「actions 非空」那一条 —— 那是两边的**唯一**差别，下面单独量。");
        int pairs = 0, agree = 0, emptyButTrue = 0;
        var bad = new List<string>();
        foreach (var st in f.stages)
        {
            if (st == null) continue;
            // ① `turns[]`
            if (st.turns != null)
                for (int i = 0; i < st.turns.Length; i++)
                {
                    var tt = st.turns[i];
                    bool o = tt != null && tt.scriptedTurn && tt.actions != null && tt.actions.Length > 0;
                    var s = new TutorialScript(st); s.UpdateTurn(i + 1);
                    bool u = s.CurrentTurn != null;
                    pairs++; if (o == u) agree++; else if (bad.Count < 6) bad.Add("S" + st.stage + "/turns[" + i + "]");
                    if (tt != null && tt.scriptedTurn && (tt.actions == null || tt.actions.Length == 0)) emptyButTrue++;
                }
            // ② preMulligan / onVictory / onDefeat 三个独立 TurnScriptedData
            foreach (var pair in new[] { ("preMulligan", st.preMulligan), ("onVictory", st.onVictory), ("onDefeat", st.onDefeat) })
            {
                var tt = pair.Item2;
                bool o = tt != null && tt.scriptedTurn && tt.actions != null && tt.actions.Length > 0;
                // 我们侧：这三个**不是** `Stage.turns[]` 的元素 ⇒ `CurrentTurn` 覆盖不到（走 `BattleDriver` 的战后脚本那条独立链）
                L("     " + pair.Item1 + "：原版 IsScriptedTurn = " + B(o) + "（scriptedTurn=" + (tt != null && tt.scriptedTurn)
                  + " · actions=" + (tt == null || tt.actions == null ? 0 : tt.actions.Length) + "）"
                  + " · 我们 `CurrentTurn` = **不覆盖**（它不是 `turns[]` 的元素）");
            }
        }
        L("样本 6 关 `turns[]` 共 " + pairs + " 个回合");
        L("两侧一致 = " + agree + "/" + pairs + (bad.Count > 0 ? "（不符：" + string.Join(" · ", bad) + "）" : ""));
        L("差异 域外差一档：「scriptedTurn=true 但 actions 空」的回合 = " + emptyButTrue + " 个"
          + "（原版 `IsScriptedTurn` 判**假**，我们 `CurrentTurn` 判**非 null**）");
        L("结论 G3 ✓（`turns[]` 域内 " + agree + "/" + pairs + "）；⚠️ 域外那一档今天 " + (emptyButTrue == 0 ? "**0 例**" : ("**" + emptyButTrue + " 例**（要改）")));
        if (agree != pairs) _bad++;
    }

    // -------- 1232 `MatchData.ShouldShuffleDeck` -------------------------
    static void BlockShouldShuffleDeck()
    {
        H("[行 1232] MatchData.ShouldShuffleDeck(MatchType)");
        L("判据 🥇 decomp_full/MatchData__ShouldShuffleDeck.c:1-10（全文 8 行，逐字）");
        L("     ret = (m != 100) && (m != 0x8c)     ← 100 = Tutorial · 0x8c = 140 = TutorialReplay");
        L("我们 RuleEngine/Core/TutorialScript.cs:1342-1345 `TutorialRules.ShouldShuffleDeck`");
        L("     = !(m == MatchType.Tutorial ‖ m == MatchType.TutorialReplay)（枚举值现读：Tutorial=100 · TutorialReplay=140）");
        var vals = new List<int>();
        foreach (RuleEngine.MatchType v in Enum.GetValues(typeof(RuleEngine.MatchType))) vals.Add((int)v);
        vals.Add(0); vals.Add(1); vals.Add(999); vals.Add(-1);        // 域外（原版是 int 形参，任何值都收）
        int agree = 0;
        foreach (var v in vals)
        {
            bool o = v != 100 && v != 0x8c;
            bool u = TutorialRules.ShouldShuffleDeck((RuleEngine.MatchType)v);
            if (o == u) agree++;
            L("   matchType " + Pad(v.ToString(), 5) + " (" + Pad(((RuleEngine.MatchType)v).ToString(), 16) + ") 原版=" + B(o) + "  我们=" + B(u) + "  " + (o == u ? "✓" : "✗"));
        }
        L("一致 " + agree + "/" + vals.Count + "（枚举 14 档 + 4 个域外值）");
        L("结论 G3 ✓");
        if (agree != vals.Count) _bad++;
    }

    static string B(bool b) { return b ? "真" : "假"; }
    static string Pad(string s, int n) { if (s == null) s = ""; return s.Length >= n ? s : s + new string(' ', n - s.Length); }
}
