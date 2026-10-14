using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RuleEngine;

/// <summary>
/// 全池解析 dump —— 一行一卡，用来做「解析器改前/改后」的离线对拍。
/// 输出格式：`id|name|unparsed|partial|op;op;...`
///   · 第 1 列 `id|name` = **键**（比较时只有它变 ⇒ 算「改名」）
///   · 第 2 列往后 = **定义**（变了 ⇒ 算「解析差异」，那才是引擎真动了）
///   · 每条 op = `verb/chooseSrc/chooseWhat/chooseAct/amount/payload` **＋ 末尾七列**
///     `cost/shared/condKind/countRef`（🆕 2026-10-10 `A1368`）
///     `costKind/countScope/condition`（🆕 2026-10-10 `A1374`）
///     **＋ 末尾目标块** `/{T:…}`（🆕 2026-10-11 `A1477`）—— 见下三段
///
/// 🔴 **2026-10-10 `A1368`：op 列尾部追加四列** —— 原来这四样**一个字都不打**，
///   而它们正是「付费 / 条件 / 计数」三族改动的**全部可观测面** ⇒ 凡改这四样的活，
///   `check` 都给不出信号（台账 `A1368`；第十三会话 `W6b` 被这个坑撞过一次 ——
///   它写的一个回归让 **28 张**卡的 `cost` 全变 0，`check` 全绿，是它另建的一次性副本才看出来的）。
///   四列分别是：
///     · `cost`      ← `EffectOp.Cost`（付费激活的代价，0 = 不付费）
///     · `shared`    ← `EffectOp.CostShared`（`1` = 这份代价是**同一前缀组**里别人付的，
///                       本 op 不再收钱；`0` = 本 op 就是付钱那条）—— 输出 `1`/`0`，⛔ 不是 `True`/`False`
///     · `condKind`  ← `EffectOp.ConditionKind`（条件的**规范名**，空 = 没有条件或认不出）
///     · `countRef`  ← `EffectOp.CountRef`（`for each …` 的计数对象，空 = 不计数）
///   ⚠️ **只追加、⛔ 不许插在中间**：前 6 列必须保持逐字节不变（基线的 diff 才只表现为
///      「行尾变长、老内容一字不动」—— 这是本笔唯一能自证「没顺手改坏老列」的形状）。
///   ⚠️ **`countRef` 原样打、不做转义**：`board` 族的取值自带两个 `|`（`enemy|troop|all`），
///      于是那一行的 `|` 数会 >4。**这是既有的形状**，不是格式坏了 ——
///      `chooseone` 的 `chooseWhat` 早就这样了（旧基线里 19 行是 5~6 个 `|`），
///      而 `CheckMain` 只认**前两个** `|`（`KeyOf`/`DefOf`）⇒ 不受影响。
///      🔴 **不做 `|`→`,` 的替换**：那会让 dump 里的值与引擎字段**不同**，
///      下一个人照 dump 抄进断言就抄错了 —— 正是本笔要治的「静默」那一族。
///
/// 🔴 **2026-10-10 `A1374`：同「只追加」的形状，再补三列** —— 上一笔（`A1368`）补齐了
///   「付费 / 条件 / 计数」四列之后**还剩三样一个字都不打**，于是三类改动**仍然进不了**
///   `check`（台账 `A1374`，出处 = `W9_A1368探针补列.md` 顺手发现 1/2）：
///     · `costKind`   ← `EffectOp.CostKind`（货币名：`energy` / `faith` / `""`）——
///       🔴 **`A1357` 正是这一类改动**（`SOR72` 的 `Energy` → `☀`，货币由 `energy` 变 `faith`）：
///       ⚠️ 2026-10-14：那张卡（`Adelaide the Serene`）的 id 现已是 `SOR71`（换真值那批改的），
///          `SOR72` 现在是 `Castigator` —— 本行是**当时**的记录，别照它去认卡。
///       它当时能被 `check` 看见**纯属侥幸** —— 只因动词也跟着从 `gain` 变成了 `gainfaith`。
///       若**只**改货币名、动词不动，`check` 依然全绿 ⇒ **这不是将来的洞，是已经发生过的**
///       （`Program.Dump` 一直打 `ck=`，`scan` 不打 ⇒ 两个入口口径不一致）。
///     · `countScope` ← `EffectOp.CountScope`（`board` / `draw` / `died` / `played` / `spiritspent` /
///       `darkpact`）—— 单看 `countRef=own|unit|all` 能猜到是 `board`，但 `countRef=board`
///       （`darkpact` 那支）与「**不计数**」在 dump 里看起来只差一个字。
///     · `condition`  ← `EffectOp.Condition`（条件的**原文**）—— `condKind` 为空时分不出
///       「**没条件**」与「**有条件但认不出**」（后者是已知的静默风险，`EffectText.cs:1595-1599`）。
///   ⚠️ **仍然是「只追加、⛔ 不许插在中间」**：上一笔那 4 列（连同前 6 列）必须保持逐字节不变，
///      基线的 diff 才只表现为「行尾再变长、老内容一字不动」—— 这是本笔唯一能自证
///      「没顺手改坏老列」的形状（判据 = `旧行前N列 == 新行前N列` 且 `旧行 ops == strip3(新行 ops)`）。
///      ⇒ 所以 `costKind` **没**挨着 `cost` 放（那样更好读，但会插在中间）—— 排在了尾部。
///   ⚠️ **`condition` 也是原样打、不做转义**（同 `countRef` 那条取舍）：它可能自带 `|`，
///      让那一行的 `|` 数再变多。`CheckMain` 只认**前两个** `|` ⇒ 不受影响；
///      而「原样」保证 dump 的值与引擎字段**逐字相同**（照 dump 抄进断言不会错）。
///
/// 🔴 **2026-10-11 `A1477`：同「只追加」的形状，末尾再补一个【目标块】**
///   —— `/{T:Raw=…|Side=…|…}`（`Target == null` ⇒ `/{T:null}`），`Target` 非空且
///   `EffectOp.DeathWatchTarget != null` 时再跟一个 `/{DW:…}`。
///   病灶：**dump 原来一个字都不打 `Target`** —— 只有 `verb/chooseSrc/chooseWhat/chooseAct/amount/payload`
///   ＋ 上面那七列。而 `A1467` 改的**正是「目标怎么解」**（`All enemies lose …` 句首那个 `All `
///   被正则吃掉 ⇒ `T=[enemies]…n=1` 变成 `T=[all enemies]…n=0`）⇒ **`check` 一行都没多**、
///   「解析差异 0 行」照旧 ⇒ **凡改目标解析的笔都会静默过关**（`A1467` 写手只能自建一份签名 dump 才拿到读数）。
///   ⚠️ 与 `A1459`（`willrun` 补「无头」档）、`A1466`（`ruleprobe.sh` 拿旧 dll 跑）**同一条链**
///   ——「**探针看不见哪一类改动**」这件事本身还有缝。
///   · 块里打哪几个字段 = `Program.TargetCols`（28 个，顺序写死 ⇒ 基线稳定）；
///     它与 `EffectTargetSpec` 一致与否由 `Sentinel` 第 ④ 条**每次 `check` 现算**（加字段没跟进 = 出声）。
///   · 格式与 `|`/`/` 两条取舍同上：**原样打、不转义**（照 dump 抄进断言的值必须与引擎字段逐字相同）。
///   · 🔴 **块前面那个 `/` 是必须的** —— `Sentinel.Legacy` 就是靠 `/{T:` 这个起点把 legacy 段切出来，
///     再从后往前数尾部七列的（块里 `Raw` 是卡面原文、**可以含 `/`** ⇒ 不切就会静默拿错列）。
///
/// 🔴 **`A1568①`（`A1590②`）：同「只追加」的形状，末尾再补一个【`Source` 块】**
///   —— `/{SRC:…}`（`Source == null` ⇒ `/{SRC:}`），渲染口 = `Program.SourceText`（唯一口）。
///   病灶：`EffectOp.Source` 原来**三个 dump 入口（`scan`/`seg`/`card`）一个都不打**、基线里也没有
///   ⇒ 凡改 `Source` 的活 `check` 一行都不多（`A1439` 那条「两条路 `Source` 不是同一个东西」
///   因此**没仪器可证**，只能按代码读）。
///   ⚠️ **块必须排在 `/{T:…}` 【之后】**（`Program.TargetBlock` 末尾追加）：`Sentinel.Legacy`
///   按 `/{T:` 切、`Tail()` 再从**后往前**数尾部列 —— 排在前面会把**卡面原文**（可含 `/`）
///   吞进 legacy 段，`Tail()` 就**静默拿错列**了（成本 / 货币名两条哨兵当场失真）。
///   排在最末尾（`TargetBlock` 里那句 `s += SourceText(op);`）⇒ `Legacy()` 连它一起摘掉，
///   尾部列位置**一个都不动**。
///   ⚠️ 形状失真（`Source` 原文自带 `;` / `/{T:`）由哨兵 ⑧ **出声但不算失败**（同 `Raw` 那条取舍）。
///
/// 🔴 **`A1877②`（2026-10-14 · 第二十八会话 · 写手 `W16`）：同「只追加」的形状，末尾再补两个块**
///   —— `/{AM:n}`（`EffectOp.AmountMax > 0` 时）· `/{TAIL:…}`（`EffectOp.Tail` 非空时），
///   渲染口 = `Program.TargetBlock`。病灶 = **区间伤害只比得上下界**、**「…and stun it」那半句
///   根本没法比**（`A1759` 逐条对账的 §查不清最后一行点名）。
///   ⚠️ **排位**：在 `/{T:…}` 之后、`/{SRC:…}` **之前** —— `Sentinel.Legacy` 按 `/{T:` 切，
///     尾部七列位置**一个都不动**；而 `/{SRC:…}` 必须仍然**排在最末尾**（哨兵 ⑧ 靠它收尾）。
///   ⚠️ **只在非默认值时打**（同 `/{DW:}` / `/{T2:}` 那条取舍）⇒「只追加」的自证形状还在：
///     **把这两个块删掉之后，新行必须与旧行【逐字节】相同**。
///   🔴 **本笔落地时【没有】重生成基线、也【没有】重钉三个指纹 —— 这是有意的。**
///     2026-10-14 当次实测（脚本 `_tmp_view/w16_attr.py`，逐行比对、**不是**看 diff 行数）：
///     新旧 dump 差 **201 行**，归因 = **A 类 115 行**（只多了本笔那两个块，删掉即逐字节相同）
///     ＋ **B 类 86 行**（`unparsed`/`partial`/`ops` **真变了** —— 例 `AM1` 的 `Talent: Lord Commander`
///     从「不解析」变成一条 `create` op；**那是另一个写手在改 `RuleEngine/Core/EffectText.cs`**，
///     本件一个字节都没碰 `RuleEngine/**`）。
///     ⇒ 按 `Program.PinnedParseMd5` 那条纪律「**重钉前必须先确认这批变化是对的**」，
///       此刻重钉会把 **B 类那 86 行未经确认的漂移一起盖成绿**（正是哨兵 ⑦ 要抓的事）⇒ **不做**。
///     ⇒ **下一步（建议由主对话在同步点做，等 B 类的改动被确认之后）**：
///       ① `ruleprobe.sh scan 工具/ruleprobe/基线/out_baseline.txt`（重生成基线）；
///       ② 手改 `Program.PinnedBaselineMd5`、`Program.PinnedParseMd5` 到 `check` 打出来的现值；
///       ③ 跑 `check` ⇒ 应当「解析差异 0 行 · 哨兵 0 条失败 · rc=0」。
///
/// ⚠️ **2026-10-18 收编时修了两处**（原来那份在 D:/tmp/wf_b14_probe）：
///   ① 参数名 `kind` 其实是 `unparsed`（EffectText.Parse 的第二个 out）—— 名字叫反了，正名。
///   ② 原来写 `.Append(kind)` ⇒ 命中 `StringBuilder.Append(object)` ⇒ **每张卡都打成字面量
///      `System.Collections.Generic.List\`1[System.String]`**，等于那一列恒为常数、**白扔**。
///      更要命的是「**没解析出来的碎片**」（`unparsed`，解析器最该被对拍的东西）被整个丢掉了。
///      修法 = `string.Join(",", unparsed)`。⚠️ 改完**基线必须重生成**（每一行都变了）。
/// </summary>
class Scan
{
    public static void ScanMain(string[] args, bool quiet = false)
    {
        string outp = args.Length > 0 ? args[0] : "scan.txt";
        string poolPath = Program.PoolPath;
        if (!File.Exists(poolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + poolPath); Environment.Exit(2); }
        // 🔴 **2026-10-10 `A1339`：这条入口【不走 `LoadPool`】**（它自己再读一遍 JSON、直接调 `EffectText.Parse`）
        //   ⇒ 光修 `Program.LoadPool` **救不了 `scan`/`check`**，必须在这里显式过一遍那道闸，
        //   否则就是「只补一处 = 静默偏一半」。索引建成后 `CreatePool.MatchCardName` 才认得出
        //   卡面文本里写的卡名 ⇒ `unparsed`/`partial` 两列才与引擎同口径（`scan` 的输出**不含** `Target` 列，
        //   但目标解析成功与否会改变这两列与 ops 列 —— 这正是基线会动的原因）。
        // ⚠️ `LoadPool` 幂等（`Pool != null` 就返回）；它建的是**全池**索引，与本函数 `continue` 掉
        //   `desc` 为空那几张**不冲突**（那几张照样该能被别的卡指到，引擎就是这么建的）。
        Program.LoadPool();
        var doc = JsonDocument.Parse(File.ReadAllText(poolPath));
        var sb = new StringBuilder();
        foreach (var c in doc.RootElement.GetProperty("cards").EnumerateArray())
        {
            string S(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            string name = S("name"), desc = S("desc");
            // 🔴 **2026-10-14（第二十九会话 · 账 `A1941`）**：这一句就是「**全池 1120**」与「卡池 `count = 1126`」对不上的**全部**原因 —— `desc` 为空的卡**不进全池 dump**，现读正好 **6 张**：`ASH61` · `BL2` · `BL26` · `GOF8` · `TL61` · `UM6`（`1126 − 6 = 1120`）。⚠️ **后果**：这 6 张的效果**永远不在基线里** ⇒ 改 `EffectText` 时动到它们，`check` **不会出声**。⇒ 本句**保留**（没 desc 本来就没得解析），但**这条盲区要写在案上**，⛔ 别再把 1120 当「卡池张数」。
            if (string.IsNullOrEmpty(desc)) continue;
            var ops = EffectText.Parse(desc, out var unparsed, out var partial, false);
            sb.Append(S("id") ?? "").Append("|").Append(name ?? "").Append("|")
              .Append(unparsed == null ? "" : string.Join(",", unparsed)).Append("|")
              .Append(partial == null ? "" : string.Join(",", partial)).Append("|");
            if (ops != null)
                foreach (var op in ops)
                    sb.Append(op.Verb).Append("/").Append(op.ChooseSrc).Append("/").Append(op.ChooseWhat)
                      .Append("/").Append(op.ChooseAct).Append("/")
                      .Append(op.Amount).Append("/").Append(op.Payload)
                      // 🔴 **2026-10-10 `A1368`：末尾追加这四列**（前 6 列一字不动 —— 见文件头）。
                      .Append("/").Append(op.Cost)
                      .Append("/").Append(op.CostShared ? "1" : "0")
                      .Append("/").Append(op.ConditionKind ?? "")
                      .Append("/").Append(Program.CountRefText(op))
                      // 🔴 **2026-10-10 `A1374`：再追加这三列**（同上那 4 列一字不动 —— 见文件头）。
                      //    `Program.Dump` 打的是**同样的三个值**（那边的标签是 `ck=` / `countScope=` / `cond=`），
                      //    两个入口口径一致；`null` → 空串，与 `condKind` 同一条规则。
                      .Append("/").Append(op.CostKind ?? "")
                      .Append("/").Append(op.CountScope ?? "")
                      .Append("/").Append(op.Condition ?? "")
                      // 🔴 **2026-10-11 `A1477`：末尾再追加【目标块】**（同上：前 13 列一字不动）。
                      //    `/{T:Raw=…|Side=…|…}`（`Target == null` ⇒ `/{T:null}`）＋ `Target` 非空时
                      //    再跟一个 `/{DW:…}`（`EffectOp.DeathWatchTarget`）。
                      //    ⚠️ 渲染在 `Program.TargetBlock`（唯一口，`Dump` 打的是同一批值）；
                      //       取尾部列前必须先 `Sentinel.Legacy()` 摘掉它（块里是卡面原文、可含 `/`）。
                      .Append(Program.TargetBlock(op))
                      .Append(";");
            sb.Append("\n");
        }
        var dir = Path.GetDirectoryName(Path.GetFullPath(outp));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outp, sb.ToString());
        if (!quiet) Console.WriteLine("wrote " + outp + " lines=" + sb.ToString().Split('\n').Length);
    }
}
