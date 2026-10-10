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
///     `costKind/countScope/condition`（🆕 2026-10-10 `A1374`）—— 见下两段
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
/// ⚠️ 2026-10-18 收编时修了两处（原来那份在 D:/tmp/wf_b14_probe）：
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
                      .Append(";");
            sb.Append("\n");
        }
        var dir = Path.GetDirectoryName(Path.GetFullPath(outp));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outp, sb.ToString());
        if (!quiet) Console.WriteLine("wrote " + outp + " lines=" + sb.ToString().Split('\n').Length);
    }
}
