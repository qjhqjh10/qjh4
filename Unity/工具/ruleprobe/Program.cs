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

    // ==================================================================================
    //  🔴 `A1382`：**基线指纹** —— 「任何真回归 ＋ 重生成基线 ⇒ 静默变绿」的机器级堵法
    // ==================================================================================
    //
    // **病灶**（台账 `A1382` · 出处 `资料/普查产出_第十三会话/W5d_A1339探针口径.md:173`）：
    //   `check` 的判据是「拿当前输出与**基线**比」，而**基线同一个工具就能重写**
    //   （`scan 基线/out_baseline.txt` 一条命令就覆盖它）⇒ 谁红了顺手重跑一次 `scan`，
    //   真回归被**静默抹平**、界面还显示全绿。
    //   `A1348` 那组哨兵只封了「**已知的**几类回归」（索引 / 货币名 / 目标列清单），
    //   **一般情形没封死** —— 这一条要治的正是「一般情形」。
    //
    // **为什么必须有【本工具写不动】的参照物**：哨兵写在同一份源码里、基线写在同一个目录里，
    //   两者都在本工具的写权限之内 ⇒ 光靠「两者互相印证」是**自证**（本仓红线）。
    //   ⇒ 参照物取**卡池文件**（`cards_engine.json`）—— 它的作者是**另一个工具**
    //     （`工具/gen_cards_engine.py`），本探针**只读**。
    //
    // **判据（两条 md5 配对，每次 `check` 现算）**：
    //   · `现算卡池 md5 == PinnedPoolMd5`（= **解析器的输入没变**）
    //     **且** `现算基线 md5 != PinnedBaselineMd5`（= 基线被改写了）
    //     ⇒ 🔴 **出声并计入 rc**：输入没变而基线变了，只有两种可能 ——
    //        ① **有人用 `scan` 把一次真回归抹平了**（要修解析器）；
    //        ② 有意重生成了一次（那就要**同时重钉下面两个常量** —— 改源码是**显式**动作、
    //           会出现在 `git diff` 里，这正是本哨兵要的「让静默动作变响」）。
    //   · 卡池 md5 对不上 ⇒ ⚠️ **只打印、不算失败**（卡池本身变了 ⇒ 两个指纹**都该重钉**）。
    //     ⛔ 不在这里判「卡池变没变坏」—— 那是**别的工具**的账。
    //
    // 🔴 **重钉的办法**（⛔ 没有「一条命令自动重钉」—— 那等于把这条洞又开回来）：
    //   跑 `check`，把它打印出来的两行现值抄进下面两个常量（**手改源码**）。
    //
    // ⚠️ **已知的假阴**：真回归 ＋ 重生成基线 ＋ **卡池也在同一批里变了** ⇒ 本哨兵**看不见**
    //   （卡池 md5 对不上那一支只打印）。如实标着，⛔ 不假装它封死了所有情形。
    // ==================================================================================

    /// <summary>钉住的卡池内容指纹（`MyGame/Assets/RuleEngine/Resources/cards_engine.json` 的 md5，**按字节**）。</summary>
    public const string PinnedPoolMd5 = "cfc13db1e5b96fa231a106d1de1ead6a";

    /// <summary>钉住的基线内容指纹（`工具/ruleprobe/基线/out_baseline.txt` 的 md5，**按字节**）。
    /// 与上面那个配对使用 —— 判据见上面那段长注释。</summary>
    public const string PinnedBaselineMd5 = "22ea1cde5e3990615eb6a63becd86e94";

    /// <summary>一个文件的 md5（按字节；读不到返回 `null` —— ⛔ 不静默当成空串）。</summary>
    public static string Md5File(string path)
    {
        try { return Md5Bytes(File.ReadAllBytes(path)); }
        catch { return null; }
    }

    static string Md5Bytes(byte[] b)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var h = md5.ComputeHash(b);
            var sb = new System.Text.StringBuilder(h.Length * 2);
            foreach (var x in h) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    static void Main(string[] args)
    {
        // 🔴 本机默认是 GBK（实测中文输出 `B8 B4 C5 DC`）⇒ 不设这个，所有中文都乱码，
        //    「摘要行一眼判读」这个用途就废了。
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
        OutDir = ResolveOutDir();
        if (args.Length == 0)
        {
            Console.WriteLine("usage: seg <text...> | card <name> | scan <out> | check [baseline] | filter <kw> | "
                              + "willrun [dump <out>] | play <id|卡名> | play all [dump <out>] | "
                              + "jackal | emergency | suppressor | played | b19test");
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
                // 🆕 **`play`**（`A1431`，2026-10-11）：**逐卡真打** —— 「这张卡打出去，数字对不对」。
                //    在本子命令之前，本仓 **9 类**检查手段**全部只回答「这句英文今天解析成什么样」**，
                //    没有一条回答「这张卡打出去数字对不对」⇒「**解析过、结算错**」这一整类
                //    （`A1388`/`A1407`/`A1426`/`A1428` 那一族）**任何全池尺子都照不到**。
                //    · `play <id|卡名>` 单卡读数（卡面 + 打前/打后局面指纹 + 变化逐条）；
                //    · `play all [dump <out.tsv>]` 全池 sweep（每张真打一发，出候选清单）。
                //    ⚠️ 退出码：单卡 = 崩了 / `ErrUnimplemented` ⇒ rc=1；全池 = 抛异常 > 0 ⇒ rc=1。
                //    ⛔ 它**不判对错** —— 判据在卡面上（`desc`/`descZh`/PnP 卡图），它只把实得数字摆出来。
                case "play":
                    {
                        if (args.Length <= i + 1)
                        {
                            Console.Error.WriteLine("!! 用法：play <id|卡名> | play all [dump <out.tsv>]");
                            Environment.Exit(2);
                        }
                        string pDump = null;
                        if (args.Length > i + 3 && args[i + 2] == "dump") pDump = args[i + 3];
                        Environment.Exit(PlayProbe.Run(PoolList, args[i + 1], pDump));
                    }
                    break;
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
                // 🔴 **2026-10-11 `A1477`**：目标规格打全（`scan` 那边打的是**同一个** `TargetText`）。
                //   `target=[…]` 那一格**保持原样不动**（外面有很多报告按它 grep），
                //   这里追加的是**整块**（含 `Raw`/`Count`/`Side`/`Kind`/相邻/滤镜/代词… 见 `TargetCols`）。
                + " T=" + TargetText(op.Target)
                + (op.DeathWatchTarget == null ? "" : " DW=" + TargetText(op.DeathWatchTarget, "DW"))
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

    // ==================================================================================
    //  `A1477`：**目标（`EffectTargetSpec`）在 dump 里的唯一渲染口**
    // ==================================================================================
    //
    // 🔴 **病灶**（台账 `A1477`）：`scan` 的 dump **一个字都不打 `Target`**
    //   —— 只有 `verb/chooseSrc/chooseWhat/chooseAct/amount/payload` ＋ 尾部七列（`A1368`/`A1374`）。
    //   而 **`A1467` 改的正是「目标怎么解」**（`T=[enemies]…n=1` → `T=[all enemies]…n=0`，
    //   `All enemies lose …` 句首那个 `All ` 被正则吃掉）⇒ **`check` 一行都没多**、
    //   「解析差异 0 行」照旧 ⇒ **那类改动会静默过关**。`A1467` 写手只能**自建**一份带 `Target.*`
    //   的签名 dump 才拿到读数（`D:/tmp/wf_a1467/sig_desc_{before,after}.txt`）。
    //   ⚠️ 与 `A1459`（`willrun` 补「无头」档）、`A1466`（`ruleprobe.sh` 拿旧 dll 跑）**同一条链** ——
    //     「**探针看不见哪一类改动**」这件事本身还有缝。
    //
    // **做法**：往每条 op 的**尾部再追加**一个目标块（仍然是「只追加、⛔ 不插在中间」的形状，
    //   见 `Scan.cs` 文件头）：`/{T:Raw=all enemies|Side=enemy|…|PickMost=}`
    //   · `Target == null` ⇒ `/{T:null}`（⛔ 别打成空 —— 「没有目标短语」本身就是要看的读数）
    //   · `EffectOp.DeathWatchTarget != null` 时再跟一个 `/{DW:…}`（同一族的**第二个**目标规格；
    //     原来同样一个字都不打，一并补上 ⇒ 不留第二道缝）
    //
    // 🔴 **为什么用「清单 ＋ 机器级守卫」而不是纯反射自动列**：
    //   · 纯反射（`GetFields()` 自动列）**列名会随声明顺序漂** ⇒ 基线里那一大块会无意义地全变，
    //     把「红了不用管」训练成习惯（`CheckMain` 头注那条教训）。
    //   · 手写清单**会腐烂** —— `EffectTargetSpec` 是**别人在改**的类（`A1311` 的
    //     `PrevSide`/`ResolvedSide` 就是后来加的），新字段**不会自己长进清单里**，那就是新的盲区。
    //   ⇒ 两者合起来：**列名与顺序写死**（基线稳定）＋ `TargetColsDrift()` 每次 `check` 现算一遍
    //     「类里有没有清单外的字段」（`Sentinel` 第 ④ 条，形状照 `A1348` 那三条）。
    //
    // ⚠️ **取尾部列必须先摘掉这个块** —— `Sentinel.Legacy()` 按 `/{T:` 切。块里**可以有任意字符**
    //   （`Raw` 是卡面原文），所以**不许**从后往前数字段（块内部自带 `/` 时会**静默拿错列**）。
    // ⚠️ **不做转义**（同 `countRef`/`condition` 那条取舍）：照 dump 抄进断言的值必须与引擎字段**逐字相同**。

    /// <summary>目标块里**该有哪些字段**（顺序 = dump 里的顺序）。
    /// 🔴 **加字段的地方在 `EffectTargetSpec`**（`RuleEngine/Core/EffectText.cs`）—— 那边加了字段、
    ///   这里没跟上 ⇒ `check` 的哨兵 ④ 会报 `✗`（`TargetColsDrift()`）。⛔ 别只改一边。</summary>
    public static readonly string[] TargetCols = {
        "Raw", "Side", "Kind", "Count",                       // ← `A1467` 改的两样（Raw / Count）在这
        "Random", "Auto", "Subjectless",
        "Adjacent", "Anchor", "AnchorInSet", "AdjacentAll", "AdjacentFailed",
        "Each", "KindUnfilterable", "SubtypeFilter",
        "Deployed", "AttackedBySelf", "KeywordFilter", "NotKeyword",
        "AlsoHand", "HandOnly", "PrevAntecedent", "PrevSide", "ResolvedSide",
        "NameFilter", "DamagedOnly", "PrayedOnly", "PickMost",
    };

    /// <summary>一个目标规格 ⇒ `{T:Raw=…|Side=…|…}`（`tag` 用来区分 `Target` 与 `DeathWatchTarget`）。
    /// ⚠️ 两个入口（`Scan` 的 ops 列、`Dump` 的 `seg`/`card`）打的是**同一个值** ——
    ///   本仓红线「两处写同一条规则 = 迟早不一致」（`A1374` 的病灶就是 `ck=` 只在一个入口打）。</summary>
    public static string TargetText(EffectTargetSpec t, string tag = "T")
    {
        if (t == null) return "{" + tag + ":null}";
        var sb = new System.Text.StringBuilder();
        foreach (var name in TargetCols)
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append(name).Append('=').Append(TargetValue(t, name));
        }
        return "{" + tag + ":" + sb.ToString() + "}";
    }

    /// <summary>要追加到 op 尾部的**整块**：`/` ＋ `{T:…}`（＋ `DeathWatchTarget` 非空时的 `/{DW:…}`）。
    /// `Sentinel.Legacy` 按其中的 `/{T:` 把 legacy 那一段切出来。</summary>
    public static string TargetBlock(EffectOp op)
    {
        var s = "/" + TargetText(op.Target);
        if (op.DeathWatchTarget != null) s += "/" + TargetText(op.DeathWatchTarget, "DW");
        return s;
    }

    /// <summary>取一个字段的值。`null` → `-`（与「空串」区分开：`EffectTargetSpec` 里
    /// `null` 与 `""` **语义不同** —— 见 `PrevSide` 的「判不出就留 null、⛔ 不许拿「大概」顶」）。
    /// `bool` → `1`/`0`（与 `shared` 那一列同规则）；枚举 → 名字（`Anchor=Unset` 这种）；
    /// 其它对象 → 它自己的 `ToString()`（`CardCriteria` 有 `override`）。</summary>
    static string TargetValue(EffectTargetSpec t, string name)
    {
        var ty = typeof(EffectTargetSpec);
        var mi = (MemberInfo)ty.GetField(name) ?? ty.GetProperty(name);
        if (mi == null) return "?";                       // 清单过期（哨兵 ④ 会报出来）
        object v = mi is FieldInfo f ? f.GetValue(t) : ((PropertyInfo)mi).GetValue(t);
        if (v == null) return "-";
        if (v is bool b) return b ? "1" : "0";
        if (v is string s) return s;
        var to = v.ToString();
        return string.IsNullOrEmpty(to) ? "-" : to;
    }

    /// <summary>🔴 `A1477` 的**机器级守卫**（形状照 `Sentinel`）：返回**两边对不上**的名字，空 = 一致。
    ///   ① **类里有、`TargetCols` 里没有** ⇒ **新字段没跟进 = 新的盲区**（这正是 `A1477` 要治的缝）；
    ///   ② **清单里有、类里没有** ⇒ 清单过期 ⇒ 那一列恒打 `?`、看着像「没值」。
    /// ⛔ 只查公开实例成员（`EffectTargetSpec` 没有基类字段）。</summary>
    public static List<string> TargetColsDrift()
    {
        var drift = new List<string>();
        var ty = typeof(EffectTargetSpec);
        var inType = new List<string>();
        foreach (var f in ty.GetFields(BindingFlags.Public | BindingFlags.Instance)) inType.Add(f.Name);
        foreach (var p in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.CanRead && p.GetIndexParameters().Length == 0) inType.Add(p.Name);
        foreach (var n in inType) if (Array.IndexOf(TargetCols, n) < 0) drift.Add("类里有、清单没有: " + n);
        foreach (var n in TargetCols) if (!inType.Contains(n)) drift.Add("清单里有、类里没有: " + n);
        return drift;
    }

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
    /// `<i>shuriken</i> 2` 就是，那条 op 有 14 个字段）⇒ 从前往后数下标会**静默拿错列**。
    /// 🔴 **2026-10-11 `A1477`：数之前必须先摘掉尾部的【目标块】** —— 块里是**卡面原文**
    ///   （`Raw`），它自己**可以**含 `/` ⇒ 不摘的话「从后往前数」也会**静默拿错列**
    ///   （尾部又多出 28 个字段，位置全漂）。见 <see cref="Legacy"/>。</summary>
    static string Tail(string op, int fromEnd) { var f = Legacy(op).Split('/'); return f[f.Length - fromEnd]; }

    /// <summary>`A1477`：摘掉 op 尾部那个目标块（`/{T:…}` 及它后面的一切，含可能的 `/{DW:…}`）。
    /// 块**内容不做解析**（只当字符串比），所以这里只需要一个**起点标记** ——
    /// `/{T:` 在 legacy 段里不会出现（那一段的字段里 `Target` 只在块内被打印）。</summary>
    static string Legacy(string op)
    {
        int i = op.LastIndexOf("/{T:", StringComparison.Ordinal);
        return i < 0 ? op : op.Substring(0, i);
    }

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

    /// <summary>返回**失败条数**（`0` = 全过）。
    /// `baselinePath` 只有哨兵 ⑤（`A1382` 基线指纹）要用 —— 其余哨兵读的是**当次现算的 dump**。</summary>
    public static int Run(string[] newLines, string baselinePath = null)
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

        // ---------------------------------------------------------------- ④ 目标列清单与类一致（`A1477`）
        // 🔴 这一条**不是**「目标解析对不对」（那要靠基线 diff），而是**「目标那几十个字段
        //   是不是全都进了 dump」** —— `A1477` 的病灶是「探针看不见 Target」，而它**会复发**：
        //    `EffectTargetSpec` 是**别人在改**的类（`A1311` 刚加过 `PrevSide`/`ResolvedSide`），
        //    新字段不会自己长进 `Program.TargetCols` 那份手写清单里。
        //    判据写死在源码里（重生成基线抹不掉），两边对不上就**出声**。
        var drift = Program.TargetColsDrift();
        if (drift.Count > 0)
        {
            fail++;
            Console.WriteLine("  ✗ [目标列] `EffectTargetSpec` 与 dump 的目标列清单**对不上**（" + drift.Count + " 处）："
                              + string.Join(" · ", drift)
                              + "\n             ⇒ 对不上的字段**在 `check` 里看不见**（= `A1477` 那条缝又开了）。"
                              + "修法：把 `Program.TargetCols` 按 `RuleEngine/Core/EffectText.cs` 的 `EffectTargetSpec` 补齐/删旧。");
        }
        else
            Console.WriteLine("  ✓ [目标列] 目标块与 `EffectTargetSpec` 一致（" + Program.TargetCols.Length + " 个字段全在 dump 里）");

        // ---------------------------------------------------------------- ⑤ 基线指纹（`A1382`）
        // 🔴 这一条治的是「**任何**真回归 ＋ 重生成基线 ⇒ 静默变绿」那条**一般情形**的洞 ——
        //    前四条只封了已知的几类。参照物 = **卡池文件**（另一个工具的作者，本探针只读）。
        //    判据与「为什么不许自动重钉」见 `Program.PinnedPoolMd5` 上面那段长注释。
        string poolMd5 = Program.Md5File(Program.PoolPath);
        string baseMd5 = string.IsNullOrEmpty(baselinePath) ? null : Program.Md5File(baselinePath);
        if (poolMd5 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [基线指纹] 卡池读不到（`" + Program.PoolPath + "`）⇒ **这一条判不了**"
                              + "（⛔ 不静默放过：判不了也要出声）。");
        }
        else if (baseMd5 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [基线指纹] 基线读不到（`" + baselinePath + "`）⇒ **这一条判不了**（同上一行）。");
        }
        else if (poolMd5 != Program.PinnedPoolMd5)
        {
            // ⚠️ **不算失败**：卡池本身变了 ⇒ 两个指纹都该重钉（那是**别的工具**的账）。
            Console.WriteLine("  ⚠️ [基线指纹] 卡池的 md5 与钉住的**不同** ⇒ 本哨兵**此时不判**"
                              + "（⛔ 不是「过了」）。");
            Console.WriteLine("             现值 卡池 md5=" + poolMd5 + " · 基线 md5=" + baseMd5);
            Console.WriteLine("             ⇒ **两个指纹都该重钉**（改 `Program.PinnedPoolMd5` / `PinnedBaselineMd5`）"
                              + "：卡池变了的时候，上面那条判据的「输入没变」这一半不成立。");
        }
        else if (baseMd5 != Program.PinnedBaselineMd5)
        {
            fail++;
            Console.WriteLine("  ✗ [基线指纹] 🔴 **卡池没动、而基线文件被改写了** —— 这正是 `A1382` 那条洞："
                              + "「任何真回归 ＋ 重生成基线 ⇒ 静默变绿」。");
            Console.WriteLine("             卡池 md5=" + poolMd5 + "（与钉住的一致 ⇒ **解析器的输入没变**）"
                              + " · 基线 md5=" + baseMd5 + "（钉住的是 " + Program.PinnedBaselineMd5 + "）");
            Console.WriteLine("             ⇒ 先看 `git diff 工具/ruleprobe/基线/out_baseline.txt`：");
            Console.WriteLine("                · **有意**重生成（解析器真改对、已用 `RuleEngineTest.Run` 或真机验过）"
                              + "⇒ 把 `Program.PinnedBaselineMd5` 改成上面的现值；");
            Console.WriteLine("                · **为了把红抹平** ⇒ 那是真回归，去修解析器（⛔ 别改这个常量）。");
            Console.WriteLine("             ⚠️ 这一条**没有**「一条命令重钉」的口子 —— 重钉只能**手改源码**"
                              + "（那才是显式动作、才会出现在 `git diff` 里）。");
        }
        else
            Console.WriteLine("  ✓ [基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写）");

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
    /// <summary>缺省基线 —— 🔴 **2026-10-11 修（台账 `A1380`）**。
    ///
    /// **原来写的是**：`程序集目录` 往上**两级** ＋ `基线/out_baseline.txt`。而 `Assembly.Location`
    /// 是 `工具/ruleprobe/bin/Debug/net8.0` ⇒ 两级只上到 **`bin`** ⇒ 算出来是
    /// **`工具/ruleprobe/bin/基线/out_baseline.txt`**（**不存在**）⇒ 谁直接敲 dll、**不带参数**，
    /// 一律 `!! 没有基线 …` · **`rc=2`**。日常躲过它只有一个原因：`ruleprobe.sh` **总是显式传**
    /// `"${2:-基线/out_baseline.txt}"`，而 README 的「怎么跑」那节**没写「必须带参数」**。
    /// ⚠️ **它是「响的」不是「静的」**（不会给出假绿），所以当时判「超范围」；但**仍然要修**（铁律 11）。
    /// ⚠️ 台账原文说「少两级」—— 实测**差一级**（`net8.0 → Debug → bin → ruleprobe` 是**三级**，
    ///   原文那个路径正好落在 `bin/基线`）。这处口径差已记进报告（铁律 5：正本不归本工具改）。
    ///
    /// ⇒ **修法 = 按名字往上找**（形状照 `ResolveOutDir` 找 `MyGame/Assets`）：
    ///   ① 先找 `基线/out_baseline.txt` **文件本身**（最稳：换 build 配置、换 TFM 都不受影响）；
    ///   ② 再找 `基线/` **目录**（文件还没生成时，报错信息也能指到**该在的位置**）；
    ///   ③ 都没有 ⇒ 回落到「按规则算的那一条」（三级 `..`），让 caller 的报错信息指对地方（⛔ 不静默）。
    /// </summary>
    public static string DefaultBaseline()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        DirectoryInfo dirHit = null;
        while (d != null)
        {
            var f = Path.Combine(d.FullName, "基线", "out_baseline.txt");
            if (File.Exists(f)) return f;
            if (dirHit == null && Directory.Exists(Path.Combine(d.FullName, "基线"))) dirHit = d;
            d = d.Parent;
        }
        if (dirHit != null) return Path.Combine(dirHit.FullName, "基线", "out_baseline.txt");
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                             "..", "..", "..", "基线", "out_baseline.txt"));
    }

    public static int Run(string baselinePath)
    {
        bool wasDefault = string.IsNullOrEmpty(baselinePath);
        if (wasDefault) baselinePath = DefaultBaseline();
        baselinePath = Path.GetFullPath(baselinePath);
        if (!File.Exists(baselinePath))
        {
            Console.Error.WriteLine("!! 没有基线：" + baselinePath
                                    + (wasDefault ? "（**没传参数** ⇒ 缺省值 = 程序集目录往上找到的第一条 `基线/out_baseline.txt`）" : ""));
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
        int sentFail = Sentinel.Run(newLines, baselinePath);

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
