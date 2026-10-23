# gen_diag_runner.py — 重新生成 `RuleEngineTest_Diag.cs`（诊断入口 `RunSafe`）
#
# 为什么要有它：`RuleEngineTest.RunSafe` 是**按 `Run()` 里一模一样的顺序**展开的一长串
# `Safe("用例名", () => 用例());` —— **一改 `Run()` 的用例表，它就会过期**（漏跑新用例、
# 或调到删掉的用例）。这个脚本按当前 `Run()` 的调用顺序重生成它。
#
# 🔴 2026-10-11（`A1394` 收口）——「静默少跑」这件事现在有**三道闸**，本脚本是第 ① 道：
#   ① **生成时（本文件）**：每次跑都读两样东西并对账，不一致就**大声打出来**：
#        · `Run()` 的用例表（抽 `Step(X);` 与裸 `X();`，见 `run_cases()`）
#        · 「用例方法」的清单（`case_method_names()`，口径 = `RuleEngineTest` 里
#          **static ＋ 无参 ＋ 返回 void ＋ 名字 `Test`/`Report` 打头后接大写或 `_`**）
#        另：`Run()` 里出现**认不出的调用语句**直接报错退出（旧版就是「认不出 ⇒ 静默当 0 条」）。
#   ② **产物里（`RunSafe()` 跑完）**：`VerifyDiagCoverage()` 把
#        【实际跑的条数/顺序】↔【生成器写进去的 `RunCaseNames`】↔【反射看到的用例方法集合】
#        三样对一遍，**不一致就是 `Check` 失败（会红、带名字）**。
#   ③ **每次 Unity 起来**：产物里的 `[InitializeOnLoadMethod] DiagDriftAlarm()` 用反射对一次，
#        过期就 `Debug.LogError` —— 跑哪条自检都会在日志里看到，**不用等谁想起来跑 `RunSafe`**。
#   ⛔ 为什么非要三道：本文件 2026-10-01 出生时正则只认裸调用 `Xxx();`，而 `Run()` 后来全改成
#      `Step(Xxx);` ⇒ 命中 **0**、脚本一 `assert` 就挂 ⇒ **没人再跑它** ⇒ 产物**静默停在 132 条**
#      （当时 `Run()` 197 条，**少跑 65 条**，连 `A985⑥①` 与那批 `TestS14` 都不在里面）。
#      ⇒ 教训不是「补一次数」，是「**它得自己会响**」。
#
# 用法：
#   PYTHONIOENCODING=utf-8 python 工具/gen_diag_runner.py            # 重生成（整份重写）
#   PYTHONIOENCODING=utf-8 python 工具/gen_diag_runner.py --check    # 只对账：盘上的产物 == 重生成的？
#                                                                    # 不等 ⇒ 打差异行 + 退出码 1（不写文件）
# 产物：`MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs`（**整份重写**，LF）
import glob
import io
import os
import re
import sys

# ⚠️ 本机的 python stdout/stderr 默认是 **GBK** —— 本脚本的提示里带 🔴 / ✅ 这类字符，
#    GBK 编不出来会**直接抛 `UnicodeEncodeError`**（= 又一个「脚本一跑就挂 ⇒ 没人再跑」的形状）。
#    这里显式改成 UTF-8（并容错），就不必依赖调用方记得设 `PYTHONIOENCODING=utf-8` 了。
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/gen_diag_runner.py [参数...] [--check]\n  选项：--check（脚本内按字符串匹配；一个都不带 = 默认模式）\n  位置参数：sys.argv[1:] 全收（数量以脚本自身判断为准）')
    _wf_help_sys.exit(0)

for _s in (sys.stdout, sys.stderr):
    try:
        _s.reconfigure(encoding="utf-8", errors="backslashreplace")
    except Exception:
        pass

ROOT = r"D:\4\Unity"
ASSETS = os.path.join(ROOT, "MyGame", "Assets")
EDITOR = os.path.join(ASSETS, "RuleEngine", "Editor")
SRC = os.path.join(EDITOR, "RuleEngineTest.cs")
OUT = os.path.join(EDITOR, "RuleEngineTest_Diag.cs")
GEN = os.path.join(ROOT, "工具", "gen_diag_runner.py")

# 一条用例：`Step(Xxx);` 或裸 `Xxx();`（⛔ 两种都要认 —— 只认一种就是 2026-10-01 那个坑），
# 容忍**行尾注释**；缩进必须是 8 空格（`Run()` 里的顶层语句）。
CASE_CALL = r"^\s{8}(?:Step\(([A-Z]\w+)\)|([A-Z]\w+)\(\));(?:\s*//.*)?\s*$"
# 「一条裸调用语句」——用来抓**认不出的**那些（`Section(...)` 是宿主调用，白名单）。
BARE_CALL = r"^\s{8}(\w+)\(.*\);\s*(?://.*)?$"
HOST_CALLS = {"Section"}          # `Run()` 里除用例之外的宿主调用（不算用例，也不报错）
CASE_DECL = r"^([^\n]*)\bvoid\s+((?:Test|Report)[A-Z_]\w*)\s*\(([^)\n]*)\)"


def run_body(src):
    """`Run()` 的正文（**按大括号配对**切，别用「下一个 `}`」这种近似）。"""
    start = src.index("public static void Run()")
    i = src.index("{", start)
    depth = 0
    for j in range(i, len(src)):
        if src[j] == "{":
            depth += 1
        elif src[j] == "}":
            depth -= 1
            if depth == 0:
                return src[i + 1:j]
    raise SystemExit("🔴 找不到 `Run()` 的收尾大括号 —— 先看源头")


def run_cases(src):
    """`Run()` 的用例表（顺序 = 文件里的顺序）。"""
    body = run_body(src)
    cases = [a or b for a, b in re.findall(CASE_CALL, body, re.M)]

    # 加固 ①：认不出的裸调用一律报出来（不许静默当「没有」）
    unknown = sorted({m.group(1) for line in body.split("\n")
                      for m in [re.match(BARE_CALL, line)]
                      if m and not re.match(CASE_CALL, line) and m.group(1) not in HOST_CALLS})
    if unknown:
        raise SystemExit(f"🔴 `Run()` 里有【认不出的调用语句】：{unknown}\n"
                         f"   要么它是新写法（那就改本脚本的 CASE_CALL），要么加进 HOST_CALLS。"
                         f"\n   ⛔ 别让它静默漏掉 —— 这正是 `A1394` 那一轮丢 65 条用例的原因。")

    # 加固 ②：空表 = 正则/格式变了，报错退出（旧版是 `assert`，效果一样但话说不清）
    if not cases:
        raise SystemExit("🔴 没从 `Run()` 里抽到任何调用 —— 正则或格式变了，先看源头")

    # 加固 ③：同名用例跑两遍 ⇒ 对账（条数）会打架，直接报
    dup = sorted({c for c in cases if cases.count(c) > 1})
    if dup:
        raise SystemExit(f"🔴 `Run()` 里同一条用例出现了不止一次：{dup}")
    return cases


def case_method_names():
    """「用例方法」清单 —— 口径**必须**与产物里的 `LooksLikeCaseMethod()` + 反射同口径：
    `RuleEngineTest` 里【static ＋ 无参 ＋ 返回 void ＋ 名字 `Test`/`Report` 打头后接大写或 `_`】。
    ⚠️ 跨**所有** partial 分册（`RuleEngineTest.cs` / `_BoardModel` / `_S14` / `_Diag` / `DeckRulesTest.cs`），
    所以这里按「文件里有没有 `partial class RuleEngineTest`」筛，不按文件名筛。"""
    names = []
    for path in glob.glob(os.path.join(ASSETS, "**", "*.cs"), recursive=True):
        text = io.open(path, encoding="utf-8", errors="replace").read()
        if "partial class RuleEngineTest" not in text:
            continue
        for m in re.finditer(CASE_DECL, text, re.M):
            head, name, args = m.group(1), m.group(2), m.group(3).strip()
            if args == "" and "static" in head:
                names.append(name)
    return names


def render(cases):
    """整份产物文本（LF）。"""
    per = 6
    lines = []
    for k in range(0, len(cases), per):
        chunk = ", ".join('"%s"' % c for c in cases[k:k + per])
        lines.append("        " + chunk + ",")
    names_block = "\n".join(lines)
    body = "\n".join('        Safe("%s", () => %s());' % (c, c) for c in cases)
    return u'''// RuleEngineTest_Diag.cs — **诊断入口**：一条用例抛异常不掐死整轮（2026-10-01 加）
//
// 🔴 **本文件由 `工具/gen_diag_runner.py` 生成，别手改** —— 重生成：
//    `PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`
//    它按 `RuleEngineTest.Run()` 里**一模一样的顺序**重出这一串；`Run()` 的用例表一变就要重跑它。
//
// 为什么要它：`Run()` 是一串直接调用，**任一条抛异常（哪怕是测试自己解引用 null）整轮就断在那儿**，
// 后面几百条根本跑不到 —— 2026-10-01 改棋盘模型时踩到过（`TestAuraSettle` 里一句 `Board[2].Armor`
// 就吃掉了整轮，只看得到 4 条失败）。
//   ⚠️ **2026-10-17 起 `Step()` 也加了逐条 try/catch** ⇒ `Run()` 自己不会再断了；
//      本入口今天多出来的那一半用处 = **它自带「少没少跑」的对账**（见下面 `VerifyDiagCoverage`）。
//      两条都留着：`Step` 的兜底在正本入口上，本入口是**旁路诊断**（顺序、计数、名字都独立可比）。
//
// 每条包一层 try/catch：抛异常的记一条失败（**带用例名** + 异常类型 + 消息）继续往下跑。
// ⚠️ **它不是替代品**：验收仍然跑 `RuleEngineTest.Run`（那个才是正本）。
//
// 🆕 **2026-10-11 `A1394` 收口 —— 这个文件从今往后自己会报「静默少跑」**：
//   · `RunCaseNames` = 生成器从 `Run()` 读出来的**用例总数正本**（顺序同 `Run()`）；
//   · `RunSafe()` 跑完调 `VerifyDiagCoverage()`：
//       ① **实际跑的条数/顺序** ↔ `RunCaseNames`（会红、带首处差异）；
//       ② `RunCaseNames` ↔ **反射看到的用例方法集合**（判据 = `LooksLikeCaseMethod`，同样会红）；
//   · 另有 `[InitializeOnLoadMethod] DiagDriftAlarm()`：**每次 Unity 起来**（含每条批处理自检）
//     对一次，过期就 `LogError` —— 不用等谁想起来跑本入口。
//   由来：2026-10-01 出生时生成器正则只认裸调用 `Xxx();`，`Run()` 改成 `Step(Xxx);` 后**命中 0**，
//   脚本一 assert 就挂 ⇒ **没人再跑它** ⇒ 本文件**静默少跑 65 条**（停在 132 / 当时 `Run()` 197）。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  `Run()` 的用例表 —— 生成器写进来的**正本**（顺序 = `Run()` 里的顺序）
    //  ⚠️ 别手改这一段，也别手改下面那串 `Safe(...)`：改 `Run()` 后重跑生成器。
    // ==================================================================
    static readonly string[] RunCaseNames = new string[]
    {
__NAMES__
    };

    // 本次诊断真正跑过的用例（顺序同 `RunCaseNames`）—— 只给对账用
    static readonly List<string> _diagRan = new List<string>();

    static void Safe(string name, Action a)
    {
        _diagRan.Add(name);
        try { a(); }
        catch (Exception e)
        {
            _fail++;   // ⚠️ 异常是从 `a()` 里冒出来的，没有 Check 帮它计数 ⇒ 自己记一条
            string line = $"[{name}] 抛异常：{e.GetType().Name} {e.Message}";
            _failures.Add(line);
            Debug.LogError(P + "   \\u2717 " + line);
        }
    }

    // ==================================================================
    //  「少没少跑」的对账（`A1394`）—— 判据在**下面两个方法里**，本段注释只是说明
    // ==================================================================

    /// <summary>
    /// 「用例方法」的口径 —— ⚠️ **必须与 `工具/gen_diag_runner.py` 的 `case_method_names()` 同口径**：
    /// `RuleEngineTest` 里【`static` ＋ 无参 ＋ 返回 `void` ＋ 名字 `Test`/`Report` 打头且后接大写或 `_`】。
    /// 实测（2026-10-11）：这类方法 **213** 个，与 `Run()` 的用例表**逐个相等**（0 多 0 少）。
    /// ⛔ 新加**辅助方法**时别用 `Test`/`Report` 打头 —— 会被算成用例（改个名就行）。
    /// </summary>
    static bool LooksLikeCaseMethod(string n)
    {
        int k = n.StartsWith("Test") ? 4 : (n.StartsWith("Report") ? 6 : -1);
        return k > 0 && n.Length > k && (char.IsUpper(n[k]) || n[k] == '_');
    }

    /// <summary>
    /// **反射**看到的用例方法集合（排序后）—— 它跨 `RuleEngineTest` 的**所有 partial 分册**
    /// （`RuleEngineTest.cs` / `_BoardModel` / `_S14` / `_Diag` / `DeckRulesTest.cs`），
    /// 也就是「**编译出来的**应该是哪些用例」= 与 `Run()` 的正文无关的第二条腿。
    /// </summary>
    static List<string> DiagCompiledCaseMethods()
    {
        var names = new List<string>();
        foreach (var m in typeof(RuleEngineTest).GetMethods(
                     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (m.ReturnType != typeof(void)) continue;
            if (m.GetParameters().Length != 0) continue;
            if (!LooksLikeCaseMethod(m.Name)) continue;
            names.Add(m.Name);
        }
        names.Sort(StringComparer.Ordinal);
        return names;
    }

    static string DiagFew(List<string> xs)
        => xs.Count == 0 ? "（无）"
         : string.Join("、", xs.GetRange(0, Math.Min(8, xs.Count)))
           + (xs.Count > 8 ? " …等 " + xs.Count + " 个" : "");

    /// <summary>
    /// 🔴 **本入口的「静默少跑」闸**（`A1394`）：`RunSafe()` **跑完必须调它**。
    /// 三条断言：
    ///   ① 实际跑的条数/顺序 == 生成器写进来的用例表（**多少条都对得上**）；
    ///   ② `Run()` 的用例表 == 反射看到的用例方法集合 ⇒ **「`Run()` 加了用例、产物没重生成」直接红**
    ///      （这一条管的是**两个入口都少跑**：`Run()` 那边也漏）；
    ///   ③ 反射清单非空 —— 防「判据自己坏了」变成**假绿**（`LooksLikeCaseMethod` 写错会返回 0 个，
    ///      那时 ② 的「缺」是空集、看着全对）。
    /// </summary>
    static void VerifyDiagCoverage()
    {
        // ---- ① 实际跑的 ↔ 生成器写进来的用例表（逐条同序）----
        var exp = new List<string>(RunCaseNames);
        int diff = 0;
        string first = "";
        for (int k = 0; k < Math.Max(_diagRan.Count, exp.Count); k++)
        {
            string ran = k < _diagRan.Count ? _diagRan[k] : "<无>";
            string want = k < exp.Count ? exp[k] : "<无>";
            if (ran == want) continue;
            diff++;
            if (first.Length == 0) first = $"第 {k + 1} 条：实跑 [{ran}] / 用例表 [{want}]";
        }
        Check(diff, 0, $"诊断入口实际跑了 {_diagRan.Count} 条，与用例表（{exp.Count} 条）逐条同序"
                     + (diff > 0 ? $" —— **{diff} 处不一致**，首处 {first}" : ""));

        // ---- ② `Run()` 的用例表 ↔ 反射看到的用例方法集合 ----
        var compiled = DiagCompiledCaseMethods();
        var missing = new List<string>();   // 类里有、`Run()` 的用例表里没有 ⇒ **两个入口都少跑它**
        var unknown = new List<string>();   // 用例表里有、类里没有 ⇒ 本该编译期就挡住（挡住了说明判据漏了）
        foreach (var n in compiled) if (!exp.Contains(n)) missing.Add(n);
        foreach (var n in exp) if (!compiled.Contains(n)) unknown.Add(n);
        Check(missing.Count, 0, $"用例方法（反射 {compiled.Count} 个）全在 `Run()` 的用例表里"
                              + (missing.Count > 0
                                 ? $" —— **缺 {missing.Count} 个**：{DiagFew(missing)}"
                                   + "（处置：若它本来就是用例 ⇒ 加进 `Run()` 再重生成；"
                                   + "若它是辅助方法 ⇒ 改个名，别用 `Test`/`Report` 打头）"
                                 : ""));
        Check(unknown.Count, 0, $"`Run()` 的用例表里没有「类里不存在的名字」"
                              + (unknown.Count > 0 ? $" —— **多 {unknown.Count} 个**：{DiagFew(unknown)}" : ""));
        Check(compiled.Count > 0, true, "反射清单非空（口径 `LooksLikeCaseMethod` 没坏）");
    }

    /// <summary>
    /// 🔴 **过期警报**：每次 Unity 起来（含 `-batchmode -executeMethod ...`）对一次，
    /// 产物比 `Run()` 旧就 `LogError`。
    /// **为什么非要有它**：`RunSafe` 是「人手想起来才跑」的入口
    /// （`工具/_run_8_checks.sh` 跑的是 `RuleEngineTest.Run`，**不含本入口**）——
    /// 光把断言放进 `RunSafe` 就还是「没人跑 = 没人知道」，正是 `A1394` 那个洞的形状。
    /// ⚠️ 这里**只报警、不碰任何计数**：`Run()` 开头就把 `_fail` / `_failures` 清零了，
    ///    写进去会被抹掉 ⇒ 只能靠日志（`LogError`，⛔ 不用 `Log`，否则又成静默）。
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    static void DiagDriftAlarm()
    {
        try
        {
            var compiled = DiagCompiledCaseMethods();
            var exp = new List<string>(RunCaseNames);
            var missing = new List<string>();
            int extra = 0;
            foreach (var n in compiled) if (!exp.Contains(n)) missing.Add(n);
            foreach (var n in exp) if (!compiled.Contains(n)) extra++;
            if (missing.Count == 0 && extra == 0) return;
            missing.Sort(StringComparer.Ordinal);
            Debug.LogError(P + "   \\u2717 [诊断入口过期] `RuleEngineTest_Diag.cs` 跟不上 `Run()` 的用例表 —— "
                + $"反射看到 {compiled.Count} 个用例方法，产物只认 {exp.Count} 个（缺 {missing.Count} / 多 {extra}）"
                + (missing.Count > 0 ? $"；缺：{DiagFew(missing)}" : "")
                + "。重生成：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`"
                + "（⛔ 别手改那个文件）");
        }
        catch (Exception e)
        {
            // 警报器自己坏了要说出来，但它不该影响别的东西 —— 所以只 Warning、不重抛
            Debug.LogWarning(P + "   [诊断入口过期检查] 自身出错（不影响别的）：" + e.Message);
        }
    }

    [UnityEditor.MenuItem("Tools/RuleEngine/规则引擎自检（诊断：单条异常不中断）")]
    public static void RunSafe()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        _diagRan.Clear();
        Debug.Log(P + "=== 规则引擎自检（诊断模式）开始 ===");

__BODY__

        VerifyDiagCoverage();

        Debug.Log(P + $"=== 结果：{_pass} 通过 / {_fail} 失败 ===");
        foreach (var f in _failures) Debug.Log(P + "   \\u2717 " + f);
        if (Application.isBatchMode)
            UnityEditor.EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }
}
'''.replace("__NAMES__", names_block).replace("__BODY__", body)


def main():
    check_only = "--check" in sys.argv[1:]
    src = io.open(SRC, encoding="utf-8").read()
    cases = run_cases(src)
    inv = case_method_names()

    only_class = sorted(set(inv) - set(cases))   # 类里有、`Run()` 没跑
    only_run = sorted(set(cases) - set(inv))     # `Run()` 跑了、类里没有（本该编译期就挡住）
    print(f"`Run()` 用例表 {len(cases)} 条 · 用例方法（口径）{len(inv)} 个")
    if only_class:
        print("🔴 「用例方法」里有 %d 个 **不在** `Run()` 的用例表里（⇒ `Run()` 与诊断入口**都少跑**）：%s"
              % (len(only_class), "、".join(only_class)))
        print("   处置：它本来就是用例 ⇒ 加进 `Run()`；它是辅助方法 ⇒ 改个名，别用 `Test`/`Report` 打头。")
    if only_run:
        print("🔴 `Run()` 的用例表里有 %d 个名字在类里不存在：%s（编译期本该就挡住 ⇒ 先看判据）"
              % (len(only_run), "、".join(only_run)))

    out = render(cases)

    if check_only:
        disk = io.open(OUT, "rb").read() if os.path.exists(OUT) else b""
        want = out.encode("utf-8")
        if disk == want:
            print(f"✅ --check：盘上的产物与重生成结果**逐字节相同**（{len(want)} 字节）→ {OUT}")
            return 0
        dl = disk.decode("utf-8", "replace").split("\n")
        wl = out.split("\n")
        n = min(len(dl), len(wl))
        for k in range(n):
            if dl[k] != wl[k]:
                print(f"🔴 --check：第 {k + 1} 行起不一样 ——\n    盘上：{dl[k]}\n    重生成：{wl[k]}")
                break
        else:
            print(f"🔴 --check：行数不一样 —— 盘上 {len(dl)} 行 / 重生成 {len(wl)} 行")
        print(f"🔴 --check：产物**过期**，重生成：`PYTHONIOENCODING=utf-8 python {GEN}`")
        return 1

    io.open(OUT, "w", encoding="utf-8", newline="\n").write(out)
    print(f"写了 {len(cases)} 条用例 → {OUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
