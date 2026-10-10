using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using RuleEngine;

/// <summary>
/// 引擎离线对拍探针 —— **快速内环 / 旁证**。
/// ⚠️ **正式验收仍然是 `RuleEngineTest.Run`（断言的真宿主）**，本工具只是让「改完立刻能对一眼」变便宜。
/// 原理：`RuleEngine/Core/` 按工程成文规矩**只许碰 `UnityEngine.Debug` / `UnityEngine.Random`**
/// （见 Core 各文件头注 + `TutorialScript.cs:344-352`）⇒ 三十行桩就能把整个 Core 编成控制台程序，
/// **不跑 Unity、不占 Unity 实例、不用等 1–9 分钟编译**（实测重编 ~2 秒）。
/// 收编自 `D:/tmp/wf_b14_probe/`（2026-10-18；那份在临时目录里、迟早被清）。
/// </summary>
class Program
{
    static List<CardDef> Pool;
    public static string OutDir;

    /// <summary>
    /// 🆕 **`keywords` 的【原文条目】、与 <see cref="Pool"/> 同序**（下标一一对应）。
    ///
    /// 🔴 **为什么要留一份**：`CardDef.Keywords` 存的是 `KeywordTable.Parse` **规范化之后的键**
    ///   （`survivor` / `armour` 这种），**看不到原文** —— 而「带正文的触发关键词」在卡表里
    ///   是**整条字符串**（`"Sacrifice: Gain +1 Attack"`，见 `A1386` 那个实测用例）。
    ///   `willrun` 要判「卡面上写着哪几段正文」，就必须看得见这些原文；
    ///   ⛔ 这不是重新解析一遍，只是**把入参留个底**（`CardDef` 没有给它开只读窗口）。
    /// </summary>
    public static List<List<string>> PoolRawKeywords = new List<List<string>>();

    /// <summary>卡池路径 —— 由仓库根推出来，⛔ 不写死盘符绝对路径。</summary>
    public static string PoolPath { get { return Path.Combine(OutDir, "RuleEngine", "Resources", "cards_engine.json"); } }

    /// <summary>已装载的卡池（<see cref="LoadPool"/> 之后非 null）—— 新子命令取池的**唯一**出口。</summary>
    public static List<CardDef> PoolList { get { LoadPool(); return Pool; } }

    static void Main(string[] args)
    {
        // 🔴 本机默认是 GBK（实测中文输出 `B8 B4 C5 DC`）⇒ 不设这个，所有中文都乱码，
        //    「摘要行一眼判读」这个用途就废了。
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
        OutDir = ResolveOutDir();
        if (args.Length == 0)
        {
            Console.WriteLine("usage: seg <text...> | card <name> | scan <out> | check [baseline] | filter <kw> | "
                              + "willrun [dump <out>] | jackal | emergency | suppressor | played | b19test");
            Environment.Exit(1);
        }
        if (args[0] == "scan")
        {
            Scan.ScanMain(new[] { args.Length > 1 ? args[1] : "scan.txt" });
            return;
        }
        if (args[0] == "check" && !File.Exists(PoolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + PoolPath); Environment.Exit(2); }
        int i = 0;
        while (i < args.Length)
        {
            switch (args[i])
            {
                case "seg": Dump(args[i + 1]); i += 2; break;
                case "card": Card(args[i + 1]); i += 2; break;
                case "filter": LoadPool(); FilterAdd.Run(Pool, args[i + 1]); i += 2; break;
                case "jackal": LoadPool(); BattleProbe.Jackal(Pool); i += 1; break;
                case "emergency": LoadPool(); BattleProbe.Emergency(Pool); i += 1; break;
                case "suppressor": LoadPool(); BattleProbe.Suppressor(Pool); i += 1; break;
                case "played": LoadPool(); BattleProbe19.Run(Pool); i += 1; break;
                case "b19test": LoadPool(); if (B19Test.Run(Pool) > 0) Environment.Exit(1); i += 1; break;
                // 🆕 **`willrun`**（`A1432`）：量「卡面正文收到没有」，把「空表」拆成
                //    ① 本来没正文 ② 收漏了 ③ 未归因（⛔ 不再让空表混进「全通」）。
                //    `willrun dump <out>` 附带一份逐卡 TSV。⚠️ 退出码可信：有「收漏了」⇒ rc=1。
                case "willrun":
                    {
                        string dumpPath = null;
                        if (args.Length > i + 2 && args[i + 1] == "dump") dumpPath = args[i + 2];
                        Environment.Exit(WillRun.Run(PoolList, PoolRawKeywords, dumpPath));
                    }
                    break;
                case "check": Environment.Exit(CheckMain.Run(args.Length > 1 ? args[1] : null)); break;
                default: Dump(args[i]); i += 1; break;
            }
        }
    }

    /// <summary>从**程序集所在目录**往上找 `MyGame/Assets`（三条腿通用：bin/Debug/net8.0 → ruleprobe → 工具 → Unity）。</summary>
    static string ResolveOutDir()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        while (d != null)
        {
            var p = Path.Combine(d.FullName, "MyGame", "Assets");
            if (Directory.Exists(p)) return p;
            d = d.Parent;
        }
        return @"D:/4/Unity/MyGame/Assets";   // 兜底：找不到就按标准盘符（找不到时 caller 会出声，⛔ 不静默）
    }

    /// <summary>装载卡池并**建卡名索引** —— 探针的**唯一**卡池入口（`Scan` / `Dump` 也都要经过它）。
    /// 🔴 2026-10-10 `A1339`：它原来**不建索引** ⇒ 见下面 `CreatePool.BuildNameIndex` 那段注释。</summary>
    public static void LoadPool()
    {
        if (Pool != null) return;
        if (!File.Exists(PoolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + PoolPath); Environment.Exit(2); }
        var doc = JsonDocument.Parse(File.ReadAllText(PoolPath));
        var list = new List<CardDef>();
        var rawKw = new List<List<string>>();      // 🆕 与 list **同序**的 `keywords` 原文（见 PoolRawKeywords）
        foreach (var c in doc.RootElement.GetProperty("cards").EnumerateArray())
        {
            string S(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            int I(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
            var kws = new List<string>();
            if (c.TryGetProperty("keywords", out var kw) && kw.ValueKind == JsonValueKind.Array)
                foreach (var k in kw.EnumerateArray()) if (k.ValueKind == JsonValueKind.String) kws.Add(k.GetString());
            list.Add(new CardDef(S("id"), S("name"), S("type"), S("desc"), S("rarity"), S("faction"),
                                 I("cost"), I("attack"), I("health"), I("ranged"), kws,
                                 S("nameZh"), S("descZh"), true, S("subtype")));
        rawKw.Add(kws);
        }
        Pool = list;
        // 🆕 **同序留一份 `keywords` 原文**（见 `PoolRawKeywords` 的说明）——
        //    `CardDef.Keywords` 只有规范化之后的键，`willrun` 要判「卡面上写着哪几段正文」得看原文。
        PoolRawKeywords = rawKw;
        // 🔴 **2026-10-10 `A1339`：探针必须与引擎【同口径】—— 就是缺了这一句。**
        //   引擎侧：`Data/CardDatabase.cs:113` 在读完卡表之后调 `CreatePool.BuildNameIndex(list)`；
        //   探针原来两边都不建 ⇒ `CreatePool.MatchCardName` 恒返回 null（`CreatePool.cs:613` 明写
        //   「没建索引时一律返回 null ⇒ 解析行为与从前完全一致」）⇒ 凡「目标靠**卡名**指」的句子，
        //   探针一律报 `target=[]` ＋ 落 `partial` 列。**它已经产出过两次假证据**：
        //   ① `A1334` 的前提（`UM_Angels_of_Death` 的 `Target == null`）
        //   ② `A1311` 的「它是 3 参重载唯一的真卡用户」（引擎里它 `Side=own`、走 4 参）。
        //   ⇒ 修法见 `资料/普查产出_第十三会话/D_A1334诊断.md` 顺手发现 2。
        // ⚠️ 建在**全池**上（含 `desc` 为空那几张卡）—— 与引擎一致：那几张照样能被别的卡的文本指到。
        // ⚠️ 它同时建**两张**表（英文 `_nameIndex` ＋ 中文 `_nameIndexCjk`），别只当英文那张看。
        CreatePool.BuildNameIndex(list);
    }

    static void Dump(string line, string tag = "SEG")
    {
        // 🔴 **2026-10-10 `A1339`**：`seg` 这条入口原来**也不建卡名索引**（它只调 `EffectText.Parse`，
        //   不经过任何装池动作）⇒ 手搓一句含**卡名**的卡面原文时会静默报 `target=[]`，
        //   和 `card`/`scan`/`check` 一样是**与引擎不同口径**。⇒ 统一走这一道闸（`LoadPool` 幂等）。
        LoadPool();
        var ops = EffectText.Parse(line, out var unparsed, out var partial, false);
        Console.WriteLine(tag + " = [" + line + "]");
        Console.WriteLine("  unparsed=" + (unparsed == null ? "-" : string.Join(" | ", unparsed))
                          + " partial=" + (partial == null ? "-" : string.Join(" | ", partial)));
        if (ops == null) { Console.WriteLine("  (null)"); return; }
        foreach (var op in ops)
        {
            Console.WriteLine("  op: verb=" + op.Verb
                + " amount=" + op.Amount
                + " payload=[" + op.Payload + "]"
                + " target=[" + (op.Target == null ? "" : op.Target.Raw) + "]"
                + " tail=[" + (op.Tail ?? "") + "]"
                + " cost=" + op.Cost + " ck=" + op.CostKind
                // 🔴 **2026-10-10 `A1368`**：这三样原来也不打 ⇒ 「付费怎么分账 / 条件认成什么 /
                //   `for each` 数谁」在**手搓一句**时同样看不见。与 `scan` 那四列**同一批**，
                //   两个入口打印的是**同一个值**（`CountRefText` 是唯一渲染口）。
                + " shared=" + (op.CostShared ? "1" : "0")
                + " condKind=[" + (op.ConditionKind ?? "") + "]"
                + " countRef=[" + CountRefText(op) + "]"
                // 🔴 **2026-10-10 `A1374`**：`scan` 又补了三列（`CostKind` / `CountScope` / `Condition`）
                //   ⇒ 这里也把**同一个值**打全，免得两个入口再出现「一个打、一个不打」的口径差
                //   （`A1374` 的病灶就是 `ck=` 只在这边打）。`null` → 空串，与上面同一规则。
                + " countScope=[" + (op.CountScope ?? "") + "]"
                + " cond=[" + (op.Condition ?? "") + "]"
                + " chooseSrc=" + op.ChooseSrc + " what=[" + op.ChooseWhat + "] act=[" + op.ChooseAct + "]"
                + " deadScope=" + op.ChooseDeadScope);
        }
    }

    /// <summary>🔴 `A1368`：`EffectOp.CountRef` 在 dump 里的**唯一**渲染口（`Scan` 的 ops 列与
    /// `Dump` 共用 —— 本仓红线「两处写同一条规则 = 迟早不一致」）。
    /// **原样返回**（只把 `null` 归一成空串）：`board` 族的取值自带两个 `|`（`enemy|troop|all`），
    /// ⛔ **不许转义/替换** —— 那会让 dump 的值与引擎字段不同，下一个人照 dump 抄进断言就抄错了。
    /// `scan` 那边靠「只认前两个 `|`」的 `CheckMain` 兜住（见 `Scan.cs` 文件头）。</summary>
    public static string CountRefText(EffectOp op) { return op.CountRef ?? ""; }

    /// <summary>🔴 **2026-10-10 `A1349`：`ParseSegment` 的【唯一入口闸】** —— 先过 `LoadPool`
    /// （＝建卡名索引）再解析。
    ///
    /// **病灶**：`BattleProbe.cs` 里原来有**三处**直调 `EffectText.ParseSegment`（硬编码字面量）
    /// （`BattleProbe19.Run` 两处 · `B19Test.Run` 一处）—— 它们**不过 `LoadPool`**，
    /// 是 `A1339` 那族的**第四种**解析入口（前三种：`card`/`seg` 走 `Dump`、`scan`/`check` 自己读 JSON）。
    /// ⚠️ 今天那三句**句子里没有卡名** ⇒ **今天无影响**；但**哪天有人塞一句带卡名的进去**，
    ///   就会重演 `A1339` 那条假象（「探针报 `target=[]`、引擎有目标」）——
    ///   而那正是**最贵**的一种错：它看起来像「引擎在这句话上不认卡名」。
    ///
    /// ⇒ **收口方式** = 让它们**走这条闸**，⛔ 不许再直调 `EffectText.ParseSegment`。
    ///   **判据**：`grep -rn "EffectText\.ParseSegment" 工具/ruleprobe/` 只该命中**本文件这一行**。
    ///   （`LoadPool` 幂等 ⇒ 重复过闸零成本；`BattleProbe` 的调用点本来就由 `Main` 先装过池。）</summary>
    public static EffectText.SegResult ParseSeg(string seg)
    {
        LoadPool();
        return EffectText.ParseSegment(seg);
    }

    static void Card(string name)
    {
        LoadPool();
        var hits = Pool.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (hits.Count == 0) { Console.WriteLine("!! no card named " + name); return; }
        foreach (var c in hits)
        {
            Console.WriteLine("CARD " + c.Name + " id=" + c.Id + " type=" + c.Type + " subtype=" + c.Subtype
                              + " rarity=" + c.Rarity + " faction=" + c.Faction + " cost=" + c.Cost
                              + " kw=[" + string.Join(",", c.Keywords.Keys) + "]");
            Console.WriteLine("  desc = " + c.Desc);
            var ops = PlayerChooseOpsOf(c);
            foreach (var op in ops)
                Console.WriteLine("  ASK verb=" + op.Verb + " src=" + op.ChooseSrc + " what=[" + op.ChooseWhat
                                  + "] act=[" + op.ChooseAct + "] payload=[" + op.Payload + "]");
            Dump(c.Desc, "  parse");
        }
    }

    static List<EffectOp> PlayerChooseOpsOf(CardDef c)
    {
        return RuleCore.PlayerChooseOps(c);
    }
}

/// <summary>
/// `check` 的**哨兵** —— **与基线无关**的机器级自证（🆕 2026-10-10 `A1348`）。
///
/// 🔴 **为什么需要**：`check` 的判据是「拿当前输出与基线比」，而**基线同一个工具就能重写**
///   （`scan 基线/out_baseline.txt` 一条命令就覆盖它）⇒ 谁红了顺手重跑一次 `scan`，
///   真回归就被**静默**抹平、界面还显示全绿（台账 `A1348` · 出处 `W5d` 顺手发现 2）。
///   `README.md` 那条 `:1024` 哨兵是**文档级**防呆 —— 文档拦不住一个正在乱敲命令的人。
///   ⇒ 把几条判据**写死在源码里**（`scan` 重写得了基线文件、重写不了 `.cs`），每次 `check` 都现算一遍。
///
/// ⚠️ **判据里的期望值必须有【原版出处】**（卡图 / 反编译 / 引擎字段语义）——
///   ⛔ **不许拿我们自己的 dump 反填**，那会变成「拿我们的实现证明我们的实现」（本仓红线）。
/// ⚠️ 每条都读**当次现算的 dump**（`newLines`），⛔ 不读基线 ⇒ 重生成基线抹不掉它们。
/// ⚠️ 哨兵卡**按 `id` 找、⛔ 不按卡名** —— 卡名是**会变的产物**（本仓已改过 6 张），
///   `scan` 的第 1 列就是 `id|name`，拿名字当锚点第一天就会漂。
/// ⚠️ 哨兵出问题**一律出声**（`✗` ＋ 现值 vs 期望），⛔ 不许静默放过 —— 本仓红线「不许静默失败」。
/// ⚠️ **哨兵集是【有意的极小集】**：宁少而准 —— 一条会经常「合理地」变红的哨兵，
///   会把「红了不用管」训练成习惯（`CheckMain` 头注那条教训）。要加之前先问：它的期望值有原版出处吗？
/// </summary>
class Sentinel
{
    /// <summary>一条 op = `verb/chooseSrc/chooseWhat/chooseAct/amount/payload` ＋ **尾部七列**：
    /// `cost/shared/condKind/countRef/costKind/countScope/condition`（见 `Scan.cs` 文件头）。
    /// 🔴 取尾部列**必须从后往前数** —— `payload` 里**可以**含 `/`（实测 `ASH65` 的
    /// `<i>shuriken</i> 2` 就是，那条 op 有 14 个字段）⇒ 从前往后数下标会**静默拿错列**。</summary>
    static string Tail(string op, int fromEnd) { var f = op.Split('/'); return f[f.Length - fromEnd]; }

    /// <summary>取某张卡那一行。返回 `[id, name, unparsed, partial, ops]`；`null` = 不在 dump 里。
    /// ⚠️ **只切 4 刀** —— ops 列里可能还有 `|`（`countRef` 的 `board` 族 / `chooseone` 的 `chooseWhat`）。</summary>
    static string[] Row(string[] lines, string id)
    {
        foreach (var l in lines)
        {
            if (l.Length == 0) continue;
            var p = l.Split(new[] { '|' }, 5);
            if (p.Length == 5 && p[0] == id) return p;
        }
        return null;
    }

    static List<string> Ops(string opsCol)
    {
        var r = new List<string>();
        foreach (var o in opsCol.Split(';')) if (o.Length != 0) r.Add(o);
        return r;
    }

    /// <summary>返回**失败条数**（`0` = 全过）。</summary>
    public static int Run(string[] newLines)
    {
        int fail = 0;
        Console.WriteLine("=== 哨兵（与基线无关 · 重生成基线抹不掉）===");

        // ---------------------------------------------------------------- ① 卡名索引自证
        // 直接问引擎：索引建了没有。判据 = `CreatePool.cs:613`：「没建索引时 `MatchCardName`
        // **一律返回 null**」。它掉了的代价是**静默**的 —— 凡「目标靠**卡名**指」的句子一律退化成
        // `target=[]` ＋ 落 `partial` 列（`A1339`，已经产出过**两次假证据**）。
        Program.LoadPool();                      // 幂等 —— `scan` 那条路已经调过一次
        const string ProbeName = "Primaris Intercessor";   // 池里真实存在的卡名（`UM_Angels_of_Death` 的 desc 就指着它）
        var idxHit = CreatePool.MatchCardName(ProbeName);
        if (idxHit == null)
        {
            fail++;
            Console.WriteLine("  ✗ [索引] `CreatePool.MatchCardName(\"" + ProbeName + "\")` == null ⇒ 卡名索引**没建**。"
                              + "先看 `Program.LoadPool` 尾部那句 `CreatePool.BuildNameIndex(list)` 还在不在。");
        }
        else
            Console.WriteLine("  ✓ [索引] 卡名索引在（`" + ProbeName + "` → `" + idxHit + "`）");

        // ---------------------------------------------------------------- ② `scan` 那条路上索引真的生效了（数据级）
        // ⚠️ ① 只证明「库函数调用得通」，证明不了「**`scan` 自己读 JSON 那条路**也过了 `LoadPool`」
        //    （`Scan.ScanMain` 是**第二条**装池路径 —— `A1339` 的病灶正是「只补一处 = 静默偏一半」）。
        //    ⇒ 这条读**当次现算的 dump**：索引不在时，`UM_Angels_of_Death` 的第 3 句
        //    （`Codex: Give +1 [Ranged] to your Primaris Intercessor`）会**落进 `partial` 列**。
        //    这一段就是 `README.md` 那条 `:1024` 哨兵的**机器级**版本。
        const string SentinelCard1 = "UM_Angels_of_Death";
        var r1 = Row(newLines, SentinelCard1);
        if (r1 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [索引·scan 路] 哨兵卡 `" + SentinelCard1 + "` **不在 dump 里** —— "
                              + "它被改名/删了、或 `desc` 变空了？哨兵要**跟着换**（⛔ 别静默放过，也别拿基线重生成糊过去）。");
        }
        else if (r1[3].Length != 0)
        {
            fail++;
            Console.WriteLine("  ✗ [索引·scan 路] `" + SentinelCard1 + "` 的 `partial` 列**非空** = `" + r1[3]
                              + "` ⇒ 卡名索引在 `scan` 那条路上**没生效**。两个可能，按序看："
                              + "① `Scan.ScanMain` 头那句 `Program.LoadPool()` 在不在；"
                              + "② `Program.LoadPool` 尾那句 `CreatePool.BuildNameIndex(list)` 在不在。");
        }
        else
            Console.WriteLine("  ✓ [索引·scan 路] `" + SentinelCard1 + "` 的 `partial` 列为空（卡名目标认出来了）");

        // ---------------------------------------------------------------- ③ 货币名（`A1357` 那一类）
        // 「**只改货币名**」（`energy` ↔ `faith`）在 `A1374` 补列**之前**是 `check` 的盲区
        // （⚠️ 而 `A1357` 正是这种改动，今天之前它被看见纯属侥幸 —— 动词也跟着变了）。
        // `SOR72` 的期望值出处 = **卡图**：`Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png`
        // 亲读是**金太阳**；`☀` = **信仰**、⛔ 不是能量（`EffectText.cs:7802-7812` 的 `MentionsFaith`，
        // 那里写着「`Contains("energy")` 失配 ⇒ 判不了 ⇒ 那半句永远不发生」）。
        // 我们 `desc` 原来错抄成 `Energy`（`cardface_fixes.json` 已按卡图改正）。
        const string SentinelCard2 = "SOR72";
        var r2 = Row(newLines, SentinelCard2);
        if (r2 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [货币名] 哨兵卡 `" + SentinelCard2 + "` **不在 dump 里**（同 ②：改名/删卡？哨兵要跟着换）。");
        }
        else
        {
            var ops2 = Ops(r2[4]);
            // 取「付费前缀那条 op」= `cost` 列非 0 的那条（⛔ 不用下标 0：下标会被将来插进来的 op 挪走）
            string pay = null;
            foreach (var o in ops2) if (Tail(o, 7) != "0") { pay = o; break; }
            if (pay == null)
            {
                fail++;
                Console.WriteLine("  ✗ [货币名] `" + SentinelCard2 + "` 那行**没有付费 op 了**（`cost` 列全 0）"
                                  + " —— 卡面 `6 ☀ Gain Flank and Shield.` 的付费前缀掉了？");
            }
            else
            {
                string cost = Tail(pay, 7), ck = Tail(pay, 3);
                if (cost != "6" || ck != "faith")
                {
                    fail++;
                    Console.WriteLine("  ✗ [货币名] `" + SentinelCard2 + "` 付费那条 op 现在是 `cost=" + cost
                                      + "` `ck=" + ck + "`，期望 `cost=6` `ck=faith`"
                                      + "（判据 = 卡图 `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png`：`6 ☀`，`☀`=信仰）。"
                                      + "\n             现值 op = " + pay);
                }
                else
                    Console.WriteLine("  ✓ [货币名] `" + SentinelCard2 + "` 付费那条 op = `cost=6` `ck=faith`（照卡图）");
            }
        }

        Console.WriteLine("=== 哨兵合计：失败 " + fail + " 条 ===");
        return fail;
    }
}


/// 🔴 两个数必须分开（照 `D:/tmp/wf_b14_probe` 那次的教训）：
///   · **解析差异** = 第 2 列往下变了 ⇒ **红**（引擎解析器真动了，要逐行看）
///   · **改名** = 只有第 1 列（`id|name`）变了 ⇒ **黄**（本工程每批都在改卡名/重跑产物，通常无害）
/// ⛔ 别拿「逐字节相同」当判据 —— 卡名是**会变的产物**（B16/B20 两批共改 6 张），
///    拿它当基线判据 ⇒ 第一天就红、而且红的是**噪声**，会把「红了不用管」训练成习惯。
/// 退出码：0 = 解析差异 0 行 **且** 哨兵全过 · 1 = 有解析差异**或**哨兵失败 · 2 = 基线缺失/读不到。
/// 🔴 **2026-10-10 `A1348`**：除「与基线比」之外，还跑一组**与基线无关的哨兵**（见 `Sentinel`）——
///   因为**基线同一个工具就能重写**（`scan` 一条命令），「红了顺手重生成基线」会把**真回归静默抹平**。
///   哨兵的判据写在**源码**里，重生成基线抹不掉。
/// </summary>
class CheckMain
{
    public static int Run(string baselinePath)
    {
        if (string.IsNullOrEmpty(baselinePath))
            baselinePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "基线", "out_baseline.txt");
        baselinePath = Path.GetFullPath(baselinePath);
        if (!File.Exists(baselinePath))
        {
            Console.Error.WriteLine("!! 没有基线：" + baselinePath);
            Console.Error.WriteLine("   生成办法：先修好解析器，再跑  scan 工具/ruleprobe/基线/out_baseline.txt  把它定为新基线。");
            return 2;
        }
        var tmp = Path.Combine(Path.GetTempPath(), "wfprobe_check.txt");
        Scan.ScanMain(new[] { tmp }, true);
        var oldLines = File.ReadAllLines(baselinePath);
        var newLines = File.ReadAllLines(tmp);

        // 🔴 **2026-10-10 `A1348`：先跑哨兵** —— 它读的是**当次现算的 dump**、
        //   ⛔ 不看基线 ⇒ **重生成基线抹不掉它**。这就是「基线可被重写」那条洞的机器级堵法。
        //   ⚠️ 它必须算进退出码 —— 否则又是「静默」。
        int sentFail = Sentinel.Run(newLines);

        string KeyOf(string l) { int p = l.IndexOf('|', l.IndexOf('|') + 1); return p < 0 ? l : l.Substring(0, p); }
        string DefOf(string l) { int p = l.IndexOf('|', l.IndexOf('|') + 1); return p < 0 ? "" : l.Substring(p + 1); }

        var oldMap = new Dictionary<string, string>();
        foreach (var l in oldLines) { if (l.Length == 0) continue; oldMap[KeyOf(l)] = DefOf(l); }
        var newMap = new Dictionary<string, string>();
        foreach (var l in newLines) { if (l.Length == 0) continue; newMap[KeyOf(l)] = DefOf(l); }

        int parseDiff = 0, renamed = 0, added = 0, removed = 0;
        var parseNames = new List<string>();
        foreach (var kv in newMap)
        {
            if (!oldMap.TryGetValue(kv.Key, out var od)) { added++; continue; }
            if (!string.Equals(od, kv.Value, StringComparison.Ordinal)) { parseDiff++; if (parseNames.Count < 20) parseNames.Add(kv.Key); }
        }
        foreach (var kv in oldMap) if (!newMap.ContainsKey(kv.Key)) removed++;
        // 改名：同一张卡的 id 在两侧都有、但 name 段不同（键是 `id|name` ⇒ 会被当 added+removed 各一条）
        var oldById = new Dictionary<string, string>();
        foreach (var l in oldLines) { var k = KeyOf(l); int p = k.IndexOf('|'); if (p > 0) oldById[k.Substring(0, p)] = k.Substring(p + 1); }
        for (int n = 0; n < newLines.Length; n++)
        {
            var k = KeyOf(newLines[n]); int p = k.IndexOf('|'); if (p <= 0) continue;
            var id = k.Substring(0, p); var nm = k.Substring(p + 1);
            if (oldById.TryGetValue(id, out var on) && on != nm) renamed++;
        }
        added -= renamed; removed -= renamed;
        if (added < 0) added = 0; if (removed < 0) removed = 0;

        Console.WriteLine("=== 全池 " + newMap.Count + " 张 · 解析差异 " + parseDiff + " 行 · 改名 " + renamed
                          + " 行 · 新增 " + added + " · 消失 " + removed + " · 基线 " + Path.GetFileName(baselinePath) + " ===");

        // ---------------------------------------------------------------- 🔴 绝对读数（`A1433` ②）
        // 病灶：上面那一行**只有「与基线比」出来的 diff** ⇒ 「解析差异 0 行」被读成「没问题」，
        //   而它只说明「**与基线一致**」。实测（第十四会话 `逐卡效果检查_方法与边界` §2·1）：
        //   `scan` 的 `unparsed` 列有 **176/1120 行非空** 的同一时刻，这里照样报「解析差异 0 行」。
        // ⇒ 把**绝对数**打在同一段里 —— 读的人不会再拿「没变」当「没有」。
        int absRows = 0, absUn = 0, absPa = 0;
        foreach (var l in newLines)
        {
            if (l.Length == 0) continue;
            var p = l.Split(new[] { '|' }, 5);      // ⚠️ 只切 4 刀（ops 列里可能还有 `|`，同 `Sentinel.Row`）
            if (p.Length < 5) continue;
            absRows++;
            if (p[2].Length != 0) absUn++;
            if (p[3].Length != 0) absPa++;
        }
        Console.WriteLine("--- 绝对读数（**与基线无关**：上面那几个数说的是「有没有变」，这几个说的是「有多少」）---");
        Console.WriteLine("  全池 " + absRows + " 行 · `unparsed` 非空 " + absUn + " 行 · `partial` 非空 " + absPa + " 行");
        Console.WriteLine("  ⚠️ **别把「`unparsed` 非空」当缺陷计数**（`A1433` ①）—— `EffectText.Coverage` 把"
                          + "「已由别的层接手」");
        Console.WriteLine("     的句子（`When <事件>,` / `Talent:` / `Companion N:` / 光环 / 开局上手）**先筛掉**之后，"
                          + "四栏**全 0**。");
        Console.WriteLine("     两个口径的完整换算 ⇒ 跑 `willrun` 的 ⑤ 段。");
        Console.WriteLine("  ⚠️ 而「解析差异 0 行」= **解析器没被改坏**，⛔ **不等于**「解析全通」——"
                          + "一条**早就在**的缺口这里永远绿。");
        Console.WriteLine("     要问「**卡面正文到底收到没有**」⇒ 跑 `willrun`（`A1432` 修掉的就是那一栏）。");
        if (parseDiff > 0)
        {
            Console.WriteLine("⚠️ 解析差异（前 " + parseNames.Count + " 张）：" + string.Join(" · ", parseNames));
            Console.WriteLine("⚠️ 这**不一定**是缺陷 —— 逐行看过再决定是「修基线」还是「修解析器」。");
        }
        // ⚠️ 哨兵红了**不许**靠重生成基线消掉 —— 它读的不是基线（见 `Sentinel` 头注）。
        //   退出码把两者合并，免得「基线绿、哨兵红」被当成绿。
        return (parseDiff > 0 || sentFail > 0) ? 1 : 0;
    }
}
