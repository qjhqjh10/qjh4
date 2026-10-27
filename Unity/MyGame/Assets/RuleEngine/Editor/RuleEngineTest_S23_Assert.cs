// RuleEngineTest_S23_Assert.cs — 第二十三会话（2026-10-12）· 写手 `W5`
//
//   · `A1337`【后半】：「`Implemented` 里每个词都要能在引擎里找到一条【**判据落点**】」
//   · `A1590①`：`A1413` 两格改动（`repeats == 0` 照收一次 / 付不起整组跳过）的**专属断言**
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（已有兄弟
// `RuleEngineTest.cs` / `_S14` / `_Diag` / `_BoardModel`），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批一律写在这里，`RuleEngineTest.cs` **一个字都没动**（`Run()` 里的 `Section` / `Step` 由调度台挂）。
//
// 🔴 **本笔是「只写测试 / 检测器」的活** —— ⛔ 生产代码一行没改。两笔的**被测对象**都在别人手里：
//    · `A1337` 的**前半**在 `RuleEngineTest.cs:2128`（`TestImplementedKeywordSites` + `KeywordSiteScan`）。
//      本文件**只补后半**：前半那两份实现按【判据】读、**不复制** —— 只调它两个现成的口
//      （`KeywordSiteScan.WordsWithoutSite` 与 `KeywordConstantNames()`），另复用它的
//      `StripConstStringDecls`（「声明不是落点」这条规则**不写第二份**）。
//    · `A1590①` 的被测对象是 `Core/EffectResolver.cs` 的 `ChargePaidGroups`（`A1412` 那一笔把
//      `A1413` ① ② 两个补丁**删掉**、改成「**先收钱、再执行**」一条不变量）—— 见 §二 的 §0。
//
// 🔴 **期望值的取法**（铁律 2 / 铁律 10）：
//    · `A1337`：期望值 = **下面 §一 那张「读点」定义表** ＋ 一组**合成源文电池**（空向 / 表向 /
//      声明向 / 赋值向 / 读点向 / 大小写各挡一支）—— 检测器自己证伪自己。
//    · `A1590①`：期望值 **不是**从我们的实现读回来的。
//      **原版判据** = `d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:27`
//      （一次激活只调**一次** `PlayerManager__UseMana`，代价取自**实体自己的** trait 值 ⇒ 与
//      「效果正文跑了几遍」无关）＋ `BattleManager__CanUseActiveAbility.c:68-71`
//      （`PlayerManager__HasEnoughMana` 对**整条** ability 判一次，不够就 `return 0`
//      ⇒ **整条 ability 不激活**，它下面那串 logic 一条都不跑）。
//      **行为读数** = 离线探针**实测**（`D:/4/_tmp_view/w5probe`，照本宿主的夹具语义
//      `ProbeBattle` / `ToP1Turn` / `Place` / `PlayCard` / `PlayTactic` 跑），每条断言的
//      「实测」都写在它的文案里。⛔ 没有一条是从 `ChargePaidGroups` 的源码里读出来的。
//
// 🔴 **夹具直调，不进录像**（本仓那条铁律）：下面凡直接调 `RuleCore.*` / 写 `Board[..]` 的
//    都是**断言夹具**，不走 `BattleDriver.LocalAct` / `SimpleAI.ExecuteAction` 两个记账口。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  一、`A1337` 后半：**「读点」的可操作定义** ＋ 它的检测器
    //
    //  **账要的是什么**：前半那条（`RuleEngineTest.cs:2029` 的 `unimplemented.Count == 0`）守
    //  「卡池里出现过的词都登记了」；`TestImplementedKeywordSites` 守「登记了的词，名字**出现过**」。
    //  两者都**挡不住**「名字出现过、但**没人读**」—— 而那正是「把没做的词塞进 `Implemented`」
    //  最省事的伪装法（往某张表里抄一遍名字、或写一句 `string s = "kw";` 就够了）。
    //
    //  ---- 判据（本笔定出来的【可操作定义】）------------------------------
    //  前提：**剥注释**；`public const string X = …;` 的**声明**剔除（声明不是落点）。
    //  词 `w` 在**任何**一份 `Core/*.cs`（⚠️ **含** `CardDef.cs`，理由见下）里有一处【读点】，
    //  当且仅当下面任一条成立 —— 「**取值**的语法位」五种：
    //    · `call`  ：处在一次**调用的实参表**里（`u.Has("flank")` · `KwValue(…Sentinel)` ·
    //                 `FireTriggerAt(…, KeywordTable.Slay, …)` · `MatchCardName(…)` …）
    //    · `cmp`   ：是 `==` / `!=` / `<=` / `>=` 的**操作数**（`if (k == "flank")` · `kw != Destroyer`）
    //    · `case`  ：是 `switch` 的 **`case` 标签**（`case "armour":`）
    //    · `return`：处在 `return` / `throw` 的**值位**
    //    · `index` ：处在 `[…]` 里
    //  匹配面：**该词的字面量**（`"flank"`，⚠️ **大小写无关**）**或**该词的**常量标识符**
    //  （`KeywordTable.Flank` / 裸 `Noncombatant`）。
    //  ⛔ **不算读点**（正是本判据相对前半**多出来的那一档**）：
    //    ① `{ … }` 集合 / 数组 / 对象**初始化器里的成员**（`Implemented` · `Prefixes` ·
    //       `RoutableTriggers` · `GivePayload` 那几张表）；
    //    ② `const string NAME = …;` 的**声明**（含它右值那个字面量）；
    //    ③ **注释**；④ **赋值右值**（`string s = "flank";`）。
    //
    //  ---- ⚠️ 为什么**不**排除 `CardDef.cs`（前半那份排除它，本判据**故意不排除**）----
    //  前半排除 `CardDef.cs` 是因为它「靠**出现**当落点」⇒ 那三张登记表会把「认得出」冒充成「有机制」。
    //  本判据靠 **①初始化器不是读点** 这条**结构上**躲开那三张表（合成电池第 3、4 行钉着它），
    //  ⇒ **按文件名排除是多余的**，而且会**逼出一个白名单条目**（本仓禁止「把词偷偷搬进白名单」）。
    //  **硬理由（实测）**：真池 `Implemented` 现读 **70 个词**，**若照前半那样排除 `CardDef.cs`**
    //  ⇒ 现读 **1 条假红**：`talent`（它**唯一**的读点就是 `CardDef.cs:1573` 那句
    //  `k.Trim().Equals("Talent", StringComparison.OrdinalIgnoreCase)` —— 那是真读点，
    //  `CardDef.HasTalentWord` 真的用它放行 `CollectTalent`）。
    //  ⚠️ **别把这两个数混读**（本段第一版就是这么错的）：现读 **7 个词的【第一个】读点在
    //  `CardDef.cs`** 里（`ability`（`CardDef.cs:232` 的 `if (name_ == KeywordTable.Ability)`）·
    //  `codex` · `companion` · `destroyer`（`kw != Destroyer`）· `ecstasy` · `sabotage` · `talent`）
    //  —— 但那 7 个里**只有 `talent` 的读点【只】在那里**（其余 6 个在别的文件里还有别的读点）
    //  ⇒ 「排除之后还剩几个词」这把尺子量出来才是 **1**。
    //  🔴 **2026-10-12 就地订正（铁律 5）**：本段原来写「**8 个词的读点【只】落在 `CardDef.cs`**
    //  ⇒ 排除它就是 8 条假红」—— **两个数都不是这回事**：8 是「**有**一处读点落在 `CardDef.cs` 里」
    //  的词数，被我读成了「**只**落在那里」；两个正确的数（7 与 1，都是**用被测的那份 C# 代码本身**
    //  在真池上现跑出来的）逐条写在上两行。
    //
    //  ---- ⚠️ 为什么字面量按**大小写无关**匹配 ----
    //  关键词词在本工程**本来就是大小写无关的**（`KeywordTable.Normalize` 归一小写；
    //  `CardDef.HasTalentWord` 用 `OrdinalIgnoreCase` 比 `"Talent"`）⇒ 按大小写无关匹配是
    //  「**跟着词域走**」，不是放宽。**硬理由（实测）**：`talent` 的唯一读点就是
    //  `CardDef.cs:1573` 那句 `.Equals("Talent", …)` —— 大小写**敏感**匹配会给它一条**假红**。
    //
    //  ---- ⚠️ 本判据的边界（如实标着，⛔ 不假装它挡得住）----
    //  它挡「**只登记 / 只进表 / 只在注释里 / 只赋给一个变量 / 只写成常量声明**」五档；
    //  ⛔ **挡不住**「把词当**参数**传给一个并不把它当判据的函数」—— 例如把关键词当**来源标签**
    //  用（实测命中一处：`RuleCore.cs:7836` 的 `Hurt(ctx, target, dmg, "Unstable")`，
    //  那个 `"Unstable"` 只是伤害来源的名字、不是判据，而它**会被算成读点**）。
    //  要再严一档得能判「那个被调函数是不是判据」，那是另一个量级的事 ⇒ **如实记着，不假装**。
    // ==================================================================

    /// <summary>`A1337` 后半的落点：一份源文里某个字符串字面量的**位置**。
    /// `Start` = 开引号所在处（与原文下标一一对应），`End` = 闭引号之后一位，`Text` = **原文**内容
    /// （转义序列按原样，与 <see cref="KeywordSiteScan.SplitCodeAndLiterals"/> 收的那份一致）。
    /// ⚠️ `public` 是给 `Blank(…, List<S23Lit>)` 用的（CS0051：参数类型不许比方法更不可见）。</summary>
    public struct S23Lit
    {
        public int Start;
        public int End;
        public string Text;
    }

    /// <summary>
    /// `A1337` 后半的检测器：词在引擎生产源码里有没有一处【**读点**】。
    /// 纯函数（词表 / 常量名表 / 源文全由调用方给）—— 合成样本才喂得进来，它才能自证伪。
    /// </summary>
    static class S23ReadScan
    {
        /// <summary>
        /// 剥注释**并**挖空字符串 / 字符字面量的内容与注释 —— ⚠️ **长度与 `src` 逐字符一一对应**
        /// （挖掉的位置填空格、`\n` 原样保留）。这是它与
        /// <see cref="KeywordSiteScan.SplitCodeAndLiterals"/> 的**唯一契约差别**：
        /// 那一份把注释与内容**抽走**（长度会变），这一份**就地挖空**（长度不变）。
        /// 🔴 **为什么必须这样**：判「这个 token 处在什么语法位上」要靠**位置**（往后扫那个未闭合的
        /// `(` / `{` / `[`），抽走长度就丢了位置。⇒ 两次实现的**分工**是：
        /// 「剥注释」这条规则只在 `SplitCodeAndLiterals` 里对外可见（前半那份用它）；
        /// 本函数是**同一台状态机的第二种输出形式**，两条规则的一致性由合成电池
        /// （`TestS23AssertImplementedReadSites` ① 的 12 行）**双向钉着**。
        /// </summary>
        public static string Blank(string src, List<S23Lit> literals)
        {
            char[] buf = src.ToCharArray();
            bool inStr = false, inChr = false, inLine = false, inBlock = false;
            int litStart = -1;
            var raw = new System.Text.StringBuilder();
            int i = 0, n = src.Length;
            while (i < n)
            {
                char c = src[i];
                char d = (i + 1 < n) ? src[i + 1] : '\0';
                if (inLine)
                {
                    if (c == '\n') inLine = false; else buf[i] = ' ';
                    i++; continue;
                }
                if (inBlock)
                {
                    if (c == '*' && d == '/') { buf[i] = ' '; buf[i + 1] = ' '; inBlock = false; i += 2; continue; }
                    if (c != '\n') buf[i] = ' ';
                    i++; continue;
                }
                if (inStr)
                {
                    if (c == '\\' && i + 1 < n) { buf[i] = ' '; buf[i + 1] = ' '; raw.Append(c).Append(d); i += 2; continue; }
                    if (c == '"')
                    {
                        inStr = false;
                        if (raw.Length > 0)                       // ⚠️ 空字面量不进表（同前半那一份）
                            literals.Add(new S23Lit { Start = litStart, End = i + 1, Text = raw.ToString() });
                        raw.Length = 0; i++; continue;
                    }
                    buf[i] = ' '; raw.Append(c); i++; continue;
                }
                if (inChr)
                {
                    if (c == '\\' && i + 1 < n) { buf[i] = ' '; buf[i + 1] = ' '; i += 2; continue; }
                    if (c == '\'') { inChr = false; i++; continue; }
                    buf[i] = ' '; i++; continue;
                }
                if (c == '/' && d == '/') { inLine = true; buf[i] = ' '; buf[i + 1] = ' '; i += 2; continue; }
                if (c == '/' && d == '*') { inBlock = true; buf[i] = ' '; buf[i + 1] = ' '; i += 2; continue; }
                if (c == '"') { inStr = true; litStart = i; raw.Length = 0; i++; continue; }
                if (c == '\'') { inChr = true; i++; continue; }
                i++;
            }
            return new string(buf);
        }

        /// <summary>`pos` 之前**最近的未闭合**那个括号（`(` / `{` / `[`）。返回它的下标，`bc` 出括号字符。</summary>
        static int Enclosing(string code, int pos, out char bc)
        {
            int depth = 0;
            for (int i = pos - 1; i >= 0; i--)
            {
                char c = code[i];
                if (c == ')' || c == ']' || c == '}') depth++;
                else if (c == '(' || c == '[' || c == '{')
                {
                    if (depth == 0) { bc = c; return i; }
                    depth--;
                }
            }
            bc = '\0';
            return -1;
        }

        /// <summary>`brace` 那个 `{` 开的是**初始化器**（一张表）而不是语句块。
        /// 判据 = 「从上一个 `;` / `{` / `}` 之后到 `{` 之前这段里出现过 `new`」或「这段以 `=` 收尾」。</summary>
        static bool IsInitializerBrace(string code, int brace)
        {
            int from = Math.Max(0, brace - 120);
            string pre = code.Substring(from, brace - from).TrimEnd();
            int cut = Math.Max(Math.Max(pre.LastIndexOf(';'), pre.LastIndexOf('{')), pre.LastIndexOf('}'));
            string seg = (cut >= 0) ? pre.Substring(cut + 1) : pre;
            if (seg.IndexOf("new", StringComparison.Ordinal) >= 0) return true;
            string s = seg.TrimEnd();
            return s.EndsWith("=", StringComparison.Ordinal)
                   && !s.EndsWith("==", StringComparison.Ordinal)
                   && !s.EndsWith("!=", StringComparison.Ordinal);
        }

        static bool EndsWithWord(string t, string w)
        {
            if (t == w) return true;
            if (!t.EndsWith(w, StringComparison.Ordinal) || t.Length <= w.Length) return false;
            char before = t[t.Length - w.Length - 1];
            return !(char.IsLetterOrDigit(before) || before == '_');
        }

        /// <summary>`tokenStart` 那个 token 处在**取值的语法位**上吗（空串 = 不是读点）。
        /// 返回的串就是 §一 表里那五个名字之一，只用来打日志。</summary>
        public static string ReadKind(string code, int tokenStart)
        {
            char bc;
            int b = Enclosing(code, tokenStart, out bc);
            string inner = (b >= 0) ? code.Substring(b + 1, tokenStart - b - 1) : code.Substring(0, tokenStart);
            if (bc == '[') return "index";
            if (bc == '(')
            {
                int hf = Math.Max(0, b - 60);
                string head = code.Substring(hf, b - hf).TrimEnd();
                if (head.Length > 0)
                {
                    char last = head[head.Length - 1];
                    // 调用 / 条件括号：前面是一个标识符、`)` 或 `]`。
                    // ⛔ 其余的 `(`（分组、`(Type)` 强制转换、数组类型位）不算。
                    if (char.IsLetterOrDigit(last) || last == '_' || last == ')' || last == ']') return "call";
                }
                return "";
            }
            // ---- 一个 `{`：初始化器（表）**不算读点**；语句块要看最后一个「取值词」 ----
            if (bc == '{' && IsInitializerBrace(code, b)) return "";
            string t = inner.Trim();
            if (EndsWithWord(t, "case")) return "case";
            if (EndsWithWord(t, "return") || EndsWithWord(t, "throw")) return "return";
            if (t.EndsWith("==", StringComparison.Ordinal) || t.EndsWith("!=", StringComparison.Ordinal)
                || t.EndsWith("<=", StringComparison.Ordinal) || t.EndsWith(">=", StringComparison.Ordinal))
                return "cmp";
            return "";
        }

        /// <summary>逐词找读点。`missing` = 一处读点都没有的词（按输入顺序）；
        /// `firstSite` = 词的**第一个**读点（`文件:常量X[种类]` / `文件:字面量"x"[种类]`，给人看的）；
        /// `sites` = **读点总数**（⚠️ 不是「有读点的词数」—— 每个词有几处就计几处，用来当锚点）。
        /// </summary>
        public static void Scan(IEnumerable<string> words,
                                IDictionary<string, List<string>> identOf,
                                IEnumerable<KeyValuePair<string, string>> sources,
                                out List<string> missing,
                                out Dictionary<string, string> firstSite,
                                out int sites)
        {
            var files = new List<KeyValuePair<string, string>>();
            var codes = new List<string>();
            var strippeds = new List<string>();
            var lits = new List<List<S23Lit>>();
            foreach (var kv in sources)
            {
                var ls = new List<S23Lit>();
                string code = Blank(kv.Value, ls);
                files.Add(kv);
                codes.Add(code);
                strippeds.Add(KeywordSiteScan.StripConstStringDecls(code));
                lits.Add(ls);
            }

            missing = new List<string>();
            firstSite = new Dictionary<string, string>();
            sites = 0;
            foreach (string w in words)
            {
                List<string> ids = null;
                if (identOf != null) identOf.TryGetValue(w, out ids);
                string found = null;
                // ---- ① 常量标识符那一支 ----
                for (int f = 0; f < files.Count; f++)
                {
                    if (ids == null) break;
                    string code = strippeds[f];
                    int i = 0, n = code.Length;
                    while (i < n)
                    {
                        char c = code[i];
                        if (char.IsLetter(c) || c == '_')
                        {
                            int s = i;
                            while (i < n && (char.IsLetterOrDigit(code[i]) || code[i] == '_')) i++;
                            string nm = code.Substring(s, i - s);
                            bool isOne = false;
                            for (int k = 0; k < ids.Count; k++) if (ids[k] == nm) { isOne = true; break; }
                            if (isOne)
                            {
                                string kind = ReadKind(code, s);
                                if (kind.Length > 0)
                                {
                                    sites++;
                                    if (found == null)
                                        found = files[f].Key + ":常量" + nm + "[" + kind + "]";
                                }
                            }
                        }
                        else i++;
                    }
                }
                // ---- ② 字面量那一支（大小写无关；19 个词没有同名常量，只走这一条路）----
                for (int f = 0; f < files.Count; f++)
                {
                    for (int k = 0; k < lits[f].Count; k++)
                    {
                        if (!string.Equals(lits[f][k].Text, w, StringComparison.OrdinalIgnoreCase)) continue;
                        string kind = ReadKind(codes[f], lits[f][k].Start);
                        if (kind.Length > 0)
                        {
                            sites++;
                            if (found == null)
                                found = files[f].Key + ":字面量\"" + lits[f][k].Text + "\"[" + kind + "]";
                        }
                    }
                }
                if (found == null) missing.Add(w);
                else firstSite[w] = found;
            }
        }
    }

    /// <summary>
    /// ★★【`A1337` 后半】`Implemented` 里**每个词都能在引擎里找到一条判据落点**。
    ///
    /// 与前半两条的分工（三条合起来才是一个闭环）：
    ///   · `RuleEngineTest.cs:2029` —— 「卡池里出现过的词**都登记了**」（卡池 → 表）；
    ///   · `TestImplementedKeywordSites` —— 「登记了的词**名字出现过**」（表 → 源文）；
    ///   · **本格** —— 「登记了的词**真的有人读**」（源文 → **判据**）。
    ///
    /// `A1337` 那一行要的正是**第三条**：光「名字出现过」挡不住「往表里抄一遍名字」。
    /// </summary>
    static void TestS23AssertImplementedReadSites()
    {
        // ==============================================================
        //  ① 检测器自己的【灭自证】—— 合成源文电池（**双向**：该报的必须报、不该报的不许报）
        //
        //  ⚠️ 这一节同时把**前半那份判据**一起钉住（最后两列）：
        //     任何一份被改成「出现即落点」，第 3 / 4 / 5 / 6 行立刻红 ——
        //     那正是「把被测实现和它的检测器一起改回旧写法」这条自证路。
        // ==============================================================
        {
            var zw = new List<string> { "zzbogus" };
            var zid = new Dictionary<string, List<string>> { { "zzbogus", new List<string> { "Zzbogus" } } };

            // 行 = { 名字, 源文, 前半(弱判据)期望的「缺」数, 后半(本判据)期望的「缺」数 }
            var rows = new[]
            {
                new object[] { "① 空（哪儿都没有）", "namespace X { class Y { static void Z() { } } }", 1, 1 },
                new object[] { "② 只在注释里", "// 这段注释提了 zzbogus\nnamespace X { class Y { } }", 1, 1 },
                new object[] { "★★ ③ 只在**表**里（字面量）",
                    "class Y { static readonly string[][] T = { new[] { \"zzbogus\", \"zzbogus\" } }; }", 0, 1 },
                new object[] { "★ ④ 只在**表**里（常量）",
                    "class Y { static readonly string[][] T = { new[] { \"\", Zzbogus } }; }", 0, 1 },
                new object[] { "★ ⑤ 只在 `const` **声明**里",
                    "class Y { public const string Zzbogus = \"zzbogus\"; }", 0, 1 },
                new object[] { "★ ⑥ 只**赋给一个变量**",
                    "class Y { static void Z() { string s = \"zzbogus\"; } }", 0, 1 },
                new object[] { "⑦ 当**调用实参**",
                    "class Y { static void Z(UnitState u) { bool b = u.Has(\"zzbogus\"); } }", 0, 0 },
                new object[] { "⑧ 当 `==` 操作数",
                    "class Y { static void Z(string k) { if (k == \"zzbogus\") return; } }", 0, 0 },
                new object[] { "⑨ 当 `case` 标签",
                    "class Y { static void Z(string k) { switch (k) { case \"zzbogus\": break; } } }", 0, 0 },
                new object[] { "⑩ **常量**当调用实参",
                    "class Y { static void Z(UnitState u) { bool b = u.Has(KeywordTable.Zzbogus); } }", 0, 0 },
                new object[] { "⑪ 大小写无关（`\"ZZBOGUS\"` 也算读）",
                    "class Y { static bool Z(string k) { return k.Trim().Equals(\"ZZBOGUS\", System.StringComparison.OrdinalIgnoreCase); } }", 1, 0 },
            };

            foreach (var row in rows)
            {
                string name = (string)row[0];
                var srcs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("FakeRuleCore.cs", (string)row[1]),
                };

                int weak = KeywordSiteScan.WordsWithoutSite(zw, zid, srcs).Count;
                bool discriminating = ((int)row[2] == 0) && ((int)row[3] == 1);
                Check(weak, (int)row[2],
                      $"★ `A1337` 灭自证「{name}」：**前半**（弱判据 = 出现即落点）"
                      + (discriminating
                         ? " —— ⚠️ **它在这一行上是绿的**（名字出现过就算落点），"
                           + "而那正是本笔要补的那一档：**绕过它只需要往某张表里抄一遍名字**"
                         : " —— 这一行两边判据应当一致（差异只在 ③④⑤⑥ 那四行）"));

                List<string> strongMissing;
                Dictionary<string, string> first;
                int sites;
                S23ReadScan.Scan(zw, zid, srcs, out strongMissing, out first, out sites);
                Check(strongMissing.Count, (int)row[3],
                      $"★★ `A1337` 灭自证「{name}」：**后半**（本判据 = 必须有人**读**它）"
                      + (((int)row[3]) == 1
                         ? " —— 这里**必须报**（🧨 检测器被改成「出现即落点」／恒返回空 ⇒ 本支红）"
                         : " —— 这里**不许报**（🧨 检测器被改成恒返回全部 ⇒ 本支红）"));
            }

            // ---- ⑫ 文件名那一格：**本判据不按文件名排除**（前半排除 `CardDef.cs`）----
            {
                var reg = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>(KeywordSiteScan.RegistryFile,
                        "class Y { static bool Z(string kw) { return kw != Zzbogus; } }"),
                };
                Check(KeywordSiteScan.WordsWithoutSite(zw, zid, reg).Count, 1,
                      "★ `A1337` 灭自证「⑫ 登记文件」：**前半**把整份 `CardDef.cs` 排除 ⇒ 它在这里**报**"
                      + "（⚠️ 真池上「排除之后变成无读点」的词**现读 1 个**：`talent`"
                      + " —— 本笔**不**采纳这条排除，理由与逐条见 §一 那段）");
                List<string> m12; Dictionary<string, string> f12; int s12;
                S23ReadScan.Scan(zw, zid, reg, out m12, out f12, out s12);
                Check(m12.Count, 0,
                      "★★ `A1337` 灭自证「⑫ 登记文件」：**后半**只看「是不是读点」、**不看文件名** ⇒ 不报"
                      + " —— ⭐ 它与上一句**同一份输入、要求相反的结果**（🔴 **结构上不可能同时满足**："
                      + "任何「两边一起改回按文件名排除」的改法必红其中一句）");
            }
        }

        // ==============================================================
        //  ② 真池：现读 `Core/*.cs`，逐词找读点
        // ==============================================================
        string dir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "RuleEngine", "Core");
        if (!System.IO.Directory.Exists(dir))
        {
            CheckTrue(false, $"找得到 `{dir}`（源文扫描的前提；⛔ 不静默跳过）");
            return;
        }
        string[] files = System.IO.Directory.GetFiles(dir, "*.cs");
        Array.Sort(files, StringComparer.Ordinal);
        CheckTrue(files.Length >= 20,
                  $"扫到 {files.Length} 个 `Core/*.cs`（**锚点**：太少 = 路径不对，别当成全绿）");

        var texts = new List<KeyValuePair<string, string>>();
        int blanked = 0;
        foreach (string f in files)
        {
            string src = System.IO.File.ReadAllText(f);
            // 🔴 **长度守恒**这条契约要当场钉一下：`Blank` 若变成「抽走」而不是「挖空」，
            //    下面所有位置判断都会静默错位（读数会「看着像对的」）。
            var probe = new List<S23Lit>();
            if (S23ReadScan.Blank(src, probe).Length == src.Length) blanked++;
            texts.Add(new KeyValuePair<string, string>(System.IO.Path.GetFileName(f), src));
        }
        Check(blanked, files.Length,
              $"★ **锚点**：{blanked}/{files.Length} 份源文过 `Blank` 后**长度不变**"
              + "（本检测器唯一那条与前半不同的契约 —— 破了它，位置判断全部静默错位）");

        var impl = new List<string>(KeywordTable.Implemented);
        impl.Sort(StringComparer.Ordinal);
        CheckTrue(impl.Count >= 40,
                  $"`KeywordTable.Implemented` 现读 {impl.Count} 个词（**锚点**：太少 = 读的不是那张表，"
                  + "⛔ 那会让下面那条变成假绿）");

        List<string> noRead;
        Dictionary<string, string> firstSite;
        int siteCount;
        S23ReadScan.Scan(impl, KeywordConstantNames(), texts, out noRead, out firstSite, out siteCount);

        // **锚点**：读点**总数**要明显多于词数（现读 221 处 / 70 个词 ≈ 3.2 处/词）。
        // 这一句挡的是「检测器退化成只认一种语法位 / 恒返回 0 处」那种**假绿**
        //（只断 `missing.Count == 0` 是挡不住的 —— 一个恒返回「这个词读过」的检测器也能过）。
        CheckTrue(siteCount >= impl.Count * 2,
                  $"**锚点**：{impl.Count} 个词共找到 {siteCount} 处读点"
                  + $"（现读 ≈ 3.2 处/词；阈值取 2×词数 = {impl.Count * 2}）"
                  + " —— ⛔ 检测器退化成只认一种语法位 / 恒返回 0 处 ⇒ 这一句先红");

        UnityEngine.Debug.Log(P + $"   `Implemented` {impl.Count} 个词的**第一个**读点：");
        foreach (string w in impl)
        {
            string s;
            UnityEngine.Debug.Log(P + $"     {w,-16} {(firstSite.TryGetValue(w, out s) ? s : "*** 无读点 ***")}");
        }

        Check(noRead.Count, 0,
              "★★【`A1337` 后半】**`Implemented` 里每个词都能在引擎里找到一条【判据落点】** —— "
              + "落点 = 词的字面量或常量标识符出现在**取值的语法位**上（实参 / 比较 / `case` / `return` / `[..]`）；"
              + "⛔ 集合初始化器里的成员（`Prefixes` / `RoutableTriggers` / `Implemented` 那几张表）、"
              + "`const` 声明、注释、赋值右值 **都不算**。"
              + "⚠️ 红了先逐条判：**是真谎报**（那个词本来就不该进 `Implemented` ⇒ 从 "
              + "`CardDef.Implemented` 删掉）还是**判据太窄**（真做了但落点形状不在上面那五种里 ⇒ "
              + "改**本判据**、并把新形状写进 §一 那张表；⛔ **不许**把词搬进白名单了事）"
              + $"（现读 {impl.Count} 个词 / {siteCount} 处读点 / {noRead.Count} 个缺；缺的是："
              + (noRead.Count == 0 ? "（无）" : string.Join(" / ", noRead.ToArray())) + "）");
    }

    // ==================================================================
    //  二、`A1590①`：`A1413` 两格的**专属断言**
    //
    //  §0 **被测对象在哪 / 为什么现在钉的是行为而不是那两行补丁**
    //  `A1413` 当年（第十九会话 `E6`）落的是两处**补丁**：
    //    ① `ResolveOps` 里 `repeats <= 0 && pays[i]` ⇒ 单独送一趟 `ResolveOne(…, payOnly: true)`；
    //    ② `ResolveOps` 里 `groupUnpaid[]` —— 组首付不起就标同组、主循环跳过。
    //  🔴 **2026-10-12（`A1412` · 第二十二会话 `W11`）这两个补丁【被删掉了】** ——
    //     收费抬到 ability 层（`Core/EffectResolver.cs` 的 `ChargePaidGroups`，`:517`），
    //     「先收钱、再执行」这一条不变量**一并覆盖**了它们（见 `EffectResolver.cs:288` 那段注释）。
    //  ⇒ **钉代码行会立刻过期**（这一笔就是被删过的）。本格钉的是**行为**（下面两条），
    //     —— 行为来自原版判据（文件头）、两条**补丁在不在**都不影响它。
    //
    //  §1 这两格的**原版判据**（铁律 10；出处都在 `d:/2/tools/decomp_full/`）：
    //    · **① ** `BattleManager__PayActiveAbilityCostOath.c:24-27`：
    //        `uVar2 = EntityScript__GetCurrentTraitValue(param_2, 0x4fb, 0)`（代价 = **实体自己的** trait 值）
    //        → `PlayerManager__UseMana(lVar4, uVar2, 0, 0, 0)`。
    //        **一次激活只调一次**，而且**读的是 ability 自己的代价** ⇒ **与「效果正文跑了几遍」无关**。
    //        ⇒ `for each` 数出 **N = 0** 也**照收一次**（原来的实现：N=0 时一遍都不跑 ⇒ **一分不收**）。
    //    · **② ** `BattleManager__CanUseActiveAbility.c:68-71`：
    //        `PlayerManager__HasEnoughMana(...)` 对**整条** ability 判一次，`cVar1 == '\0'` ⇒ `return 0`
    //        ⇒ **这一条 ability 不激活** ⇒ 它下面那串 `AbilityLogic` **一条都不跑**。
    //        ⇒ 付不起时**标着 `CostShared` 的后半句也不许落地**（改前：付费逐 op 判、后半句不查余额
    //          ⇒ **白给**后半句）。
    // ==================================================================
    static void TestS23AssertA1413PayGroup()
    {
        // ==============================================================
        //  ① `A1413` ①：`4 Energy:` 落在一条 `for each` 上 ⇒ **N = 0 也照收一次**
        //
        //  形状出处：`Core/EffectResolver.cs:416` 那段注释记的原形状
        //  「`4 Energy: Deal 1 damage to an enemy for each friendly troop`」。
        //  ⚠️ **全池今天 0 张**（现扫 `cards_engine.json` 1126 张：带 `Energy:` 前缀的只有 `SOR6` / `SOR47`
        //     两张，都没有 `for each`）⇒ 本格只能拿**合成卡**钉（与 `W_A1369付费三洞.md` 同一口径）。
        //  🔴 **实测**（离线探针 `D:/4/_tmp_view/w5probe`，照本宿主夹具语义）：
        //     N=0 ⇒ 能量 10 → 6（Δ=−4）、敌方 0 点伤害；N=2 ⇒ 能量 10 → 6（Δ=−4）、敌方 −2。
        // ==============================================================
        {
            var feCard = Tactic("W5 ForEach Probe", 0,
                                "4 Energy: Deal 1 damage to an enemy troop for each friendly troop");

            int deltaN0, deltaN2, dmgN0, dmgN2, rcN0, rcN2;
            PlayPaidForEach(feCard, 0, out deltaN0, out dmgN0, out rcN0,
                            out _, out _);
            PlayPaidForEach(feCard, 2, out deltaN2, out dmgN2, out rcN2,
                            out int payLogsN2, out _);

            CheckCode(rcN0, RuleCodes.OK, "（前提）N=0 那张战术卡**打得出去**");
            CheckCode(rcN2, RuleCodes.OK, "（前提）N=2 那张战术卡**打得出去**");

            // ①-a **不是「一分不收」**（改前的形状）
            Check(deltaN0, 4,
                  "★★【`A1413` ①】**`for each` 数出 N = 0 也照收一次钱** —— "
                  + "判据 = 原版 `PayActiveAbilityCostOath.c:24-27`（代价取自实体自己的 trait、"
                  + "一次激活只调一次 `UseMana`）⇒ 收钱与「正文跑几遍」无关。"
                  + "🧨 改回「N = 0 时一遍都不跑 ⇒ 不收钱」（= `A1412` 之前那个 `payOnly` 补丁被删掉、"
                  + "而 `ChargePaidGroups` 又没接上）⇒ 本行实得 **0**");
            Check(dmgN0, 0,
                  "……而**效果一遍都不跑**（N=0 就是一次都不结算 —— 收钱 ≠ 结算）；"
                  + "⚠️ 这里**只报伤害总数**（目标是谁不重要）");

            // ①-b **不是「收 N 遍」**
            Check(deltaN2, 4,
                  "★【`A1413` ① 的另一半】**N = 2 也只收一次**（不是 4×2 = 8）—— "
                  + "判据同上一句（`for each` 在原版是 `AbilityLogic` 上的**乘数**，"
                  + "不是把 ability 重跑 N 遍）。🧨 改回「每跑一遍收一次」⇒ 本行实得 **8**");
            Check(dmgN2, 2, "……而 N=2 时**真的结算了两遍**（各 1 点）—— 用来钉住「N 真的是 2」");

            // ①-c 🔴 **判别式（结构上不可能同时满足）**：Δ 与 N 无关
            Check(deltaN0, deltaN2,
                  "★★【`A1413` ① 的判别式】**同一张卡、同一份付费前缀，N=0 与 N=2 的扣费必须相等** —— "
                  + "「按遍收费」的实现在这两个格子里**必然给出一对不同的值**（0 与 8），"
                  + "⛔ 任何「把两边一起改回旧写法」的改法都同时要满足这一句与上面那两句");
            CheckTrue(payLogsN2 >= 1,
                      "……而且**出声**：付费那一次要留一行「付了 4 点能量」（⛔ 不静默扣钱）");
        }

        // ==============================================================
        //  ② `A1413` ②：付费那条**付不起** ⇒ 同组其余 op **一条都不落地**
        //
        //  夹具出处：`A1590` 账文与 `W11` 的交件（`资料/普查产出_第二十二会话/W11_引擎A1597A1412.md` §三）
        //  都点名这张合成卡 `Oath 2: Lower its cost by 2 and give it Flank.` ——
        //  它是**前半句不花额外钱、后半句标 `CostShared`** 的形状，正好把「白给后半句」逼出来。
        //  🔴 **实测**（同探针）：
        //     - 激活前余额 1（差 1 点）⇒ `UseOathAbility` = 13（`ErrUnimplemented`，≠ OK）、
        //       余额**一分不动**、`Has("flank")` = **false**、`OathUsesThisTurn` = 0、日志里有「一并跳过」；
        //     - 激活前余额 2（刚好）⇒ rc = 0、余额 2 → **0**、`Has("flank")` = **true**、次数 = 1。
        //     ⚠️ 余额为 3 时读数同「刚好那一档」（多出来的钱留在池子里）⇒ 本格用「刚好」当正控。
        // ==============================================================
        {
            var oathCard = new CardDef("W5_OATH", "W5 Oath Probe", "unit",
                                       "Oath 2: Lower its cost by 2 and give it Flank.",
                                       "common", "Ultramarines", 1, 1, 5, 0, new[] { "Oath" });

            UnitState uEnough, uShort;
            BattleContext ctxEnough = OathProbeBattle(oathCard, 2, out uEnough);
            BattleContext ctxShort = OathProbeBattle(oathCard, 1, out uShort);

            // ---- 正控：刚好付得起 ⇒ 两条都走 ----
            CheckTrue(uEnough != null && ctxEnough != null, "（前提）正控夹具摆上了场");
            if (uEnough != null)
            {
                CheckTrue(!uEnough.Has("flank"), "（前提）激活之前没有 `flank`");
                int rc = RuleCore.UseOathAbility(ctxEnough, 0, 3);
                CheckCode(rc, RuleCodes.OK,
                          "（正控）余额 2 = `Oath 2` 刚好付得起 ⇒ 激活**成功**"
                          + " —— 这一句与下面那句**必须成对**（只有一边能动 = 夹具坏了）");
                Check(ctxEnough.Players[0].Energy, 0,
                      "（正控）★ **真的付了 2 点**（2 → 0）—— 判据 `PayActiveAbilityCostOath.c:27`"
                      + "（一次激活、一次 `UseMana`）");
                CheckTrue(uEnough.Has("flank"),
                          "（正控）★ **后半句真的落地**（拿到 `flank`）");
                Check(uEnough.OathUsesThisTurn, 1, "（正控）激活次数记了 1");
            }

            // ---- 反控（判别式）：差 1 点 ⇒ 后半句**不许落地** ----
            CheckTrue(uShort != null && ctxShort != null, "（前提）反控夹具摆上了场");
            if (uShort != null)
            {
                int evBefore = ctxShort.Events.Count;
                int rc = RuleCore.UseOathAbility(ctxShort, 0, 3);
                CheckCode(rc, RuleCodes.ErrCost,
                          "★★【`A1413` ②】余额 1 < `Oath 2` ⇒ 激活**没生效**，且**判定阶段就回 `ErrCost`**"
                          + "（判据 = `CanUseActiveAbility.c:68-71`：余额对**整条** ability 判一次、"
                          + "不够就 `return 0` ⇒ **整条不激活**；余额那一跳 = `HasEnoughMana` 读 `unusedMana(+0x4C)`，"
                          + "⛔ **不是「能量 − 已预留」**）"
                          + "（🔴 2026-10-27：原来这句只断 `!= OK`、文案记「实测 rc = 13 = `ErrUnimplemented`」——"
                          + " 余额闸落地后码值定下来了 ⇒ **锁成 `ErrCost`**）");
                Check(ctxShort.Players[0].Energy, 1, "★ 付不起 ⇒ **一分钱都不扣**");
                CheckTrue(!uShort.Has("flank"),
                          "★★★【`A1413` ② 的**判别式**】**标着 `CostShared` 的后半句也不许落地**"
                          + "（🧨 改前：付费逐 op 判、后半句不查余额 ⇒ **白给 `flank`**，本行实得 true；"
                          + "`A1412` 之后由 `ChargePaidGroups` 的 `ok[]` 一并拦下）");
                Check(uShort.OathUsesThisTurn, 0, "★ ……激活次数也不涨（这次不算激活过）");
                // 🔴 **2026-10-27（`B9_E5ENG` · 第九批）：「一声出声」的载体换了。**
                //   这条原来断结算层那句 `ctx.Log("…其余效果一并跳过…")`（`EffectResolver.cs:727`）——
                //   余额闸现在在 `CanUseOathAbility` 就拒 ⇒ **走不到那一层**，且**刻意不在闸里写 `ctx.Log`**：
                //   ① 没有任何效果「开始过」，说「一并跳过」是假话；② 写日志会把 `ctx.Events` 加长，
                //   而 `NetProtocol.Fingerprint` 含 `Events.Count` ⇒ 一次「本地被拒」就让联机两端差 N 行（`D6NB` 55 vs 51）。
                //   ⇒ 判据 = **返回码可区分**（`ErrCost` 的人话 ≠ `ErrUnimplemented`）＋ **诊断流一行不加**。
                CheckTrue(RuleCodes.Describe(RuleCodes.ErrCost) != RuleCodes.Describe(RuleCodes.ErrUnimplemented),
                          "★ ……而且**不静默**：拒的载体是可区分的返回码（`ErrCost` =「" + RuleCodes.Describe(RuleCodes.ErrCost)
                        + "」）；玩家可见的那一声在表现层 `BattleDriver` 的 `code != OK` 分支（本宿主够不到）");
                Check(ctxShort.Events.Count, evBefore,
                      "★ ……**一行诊断都不写**（`ctx.Events` 不增）—— 写了的话 `Fingerprint`（含 `Events.Count`）会让联机两端分叉");
            }
        }
    }

    // ------------------------------------------------------------------
    //  `A1590①` 的两块夹具
    // ------------------------------------------------------------------

    /// <summary>打一次「付费前缀 + `for each`」那张战术卡，报出**扣了多少 / 打掉多少血 / 返回码 /
    /// 有几句「付了 N 点能量」的日志**。N = 场上友方部队数（`friendlyCount`）。
    /// ⚠️ 走**真实**的 `RuleCore.PlayTactic`（不是手算），所以「收钱」那条链真的跑过一遍。</summary>
    static void PlayPaidForEach(CardDef feCard, int friendlyCount,
                                out int energyDelta, out int damage, out int rc,
                                out int payLogs, out int repeats)
    {
        var troop = Unit("W5 Troop", 0, 1, 9);
        var victim = new CardDef("W5 Victim", "W5 Victim", "unit", "", null, "Goff",
                                 0, 1, 9, 0, null, subtype: "Infantry");
        // ⚠️ 牌库顺序照 `ProbeBattle`：**督军在最前、其余倒序**（抽牌是 `pop_back`，
        //    所以最后入列的先抽到）—— 于是起手 3 张 = 数组前 3 个。
        var ctx = ProbeBattle(new[] { feCard, troop, troop, troop },
                              new[] { victim, victim, victim });

        // 照 `ToP1Turn(ctx, 1)` 推到「P1 的第 1 个回合」（⛔ 不自己写一份回合推进 ——
        // 那是宿主那份现成的私有助手，两处写同一条规则迟早不一致）
        ToP1Turn(ctx, 1);

        for (int k = 0; k < friendlyCount; k++)
        {
            int landed;
            int dRc = RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "W5 Troop"), 4, out landed);
            if (dRc != RuleCodes.OK) CheckCode(dRc, RuleCodes.OK, "（前提）友方部队部署得下");
        }
        PassTurn(ctx);                                        // → P1 回合，给对面摆一个靶子
        int victimSlot;
        {
            int vRc = RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, "W5 Victim"), 4, out victimSlot);
            if (vRc != RuleCodes.OK) CheckCode(vRc, RuleCodes.OK, "（前提）敌方靶子部署得下");
            else CheckTrue(ctx.Players[1].Board[victimSlot] != null, "（前提）靶子真的在场上");
        }
        PassTurn(ctx);                                        // → 回到 P0

        // 🔴 能量在 `BeginTurn` 里会按回合重算 ⇒ **必须在这一行之后**设（实测就是这么量的）
        ctx.Players[0].Energy = 10; ctx.Players[0].MaxEnergy = 10;

        int hpBefore = 0;
        foreach (var u in ctx.Players[1].Board) if (u != null) hpBefore += u.Health;

        int before = ctx.Events.Count;
        rc = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W5 ForEach Probe"), victimSlot);
        energyDelta = 10 - ctx.Players[0].Energy;

        int hpAfter = 0;
        foreach (var u in ctx.Players[1].Board) if (u != null) hpAfter += u.Health;
        damage = hpBefore - hpAfter;

        payLogs = 0; repeats = 0;
        for (int i = before; i < ctx.Events.Count; i++)
        {
            string e = ctx.Events[i];
            if (string.IsNullOrEmpty(e)) continue;
            if (e.IndexOf("付了 4 点能量", StringComparison.Ordinal) >= 0) payLogs++;
            if (e.IndexOf("计 " + friendlyCount + " 遍", StringComparison.Ordinal) >= 0) repeats++;
        }
    }

    /// <summary>`A1413` ② 的夹具：一张 `Oath 2:` 单位 + 指定余额，摆到槽 3。
    /// ⚠️ 走 `Place`（直写棋盘）而不是 `PlayCard`：本格要的是「**激活前余额恰好是 `energy`**」，
    /// 部署费会把它挪掉。（`Place` 会补 `DeployedTurn`，那是誓约激活的前提之一。）</summary>
    static BattleContext OathProbeBattle(CardDef oathCard, int energy, out UnitState placed)
    {
        var ctx = ProbeBattle(new[] { oathCard }, new CardDef[0]);
        ToP1Turn(ctx, 1);                       // ⛔ 不自己写一份回合推进（宿主有现成的）
        ctx.Players[0].Energy = energy; ctx.Players[0].MaxEnergy = energy;
        placed = Place(ctx, 0, 3, oathCard, exhausted: false);
        return ctx;
    }
}
