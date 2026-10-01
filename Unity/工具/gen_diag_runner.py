# gen_diag_runner.py — 重新生成 `RuleEngineTest_Diag.cs`（诊断入口 `RunSafe`）
#
# 为什么要有它：`RuleEngineTest.RunSafe` 是**按 `Run()` 里一模一样的顺序**手工展开的一长串
# `Safe("用例名", () => 用例());` —— **一改 `Run()` 的用例表，它就会过期**（漏跑新用例、或调到删掉的用例）。
# 这个脚本按当前 `Run()` 的调用顺序重生成它，保证两者永远同步。
#
# 用法：`PYTHONIOENCODING=utf-8 python 工具/gen_diag_runner.py`
# 产物：`MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs`（**整份重写**）
import io, os, re

SRC = r"D:\4\Unity\MyGame\Assets\RuleEngine\Editor\RuleEngineTest.cs"
OUT = r"D:\4\Unity\MyGame\Assets\RuleEngine\Editor\RuleEngineTest_Diag.cs"

src = io.open(SRC, encoding="utf-8").read()
start = src.index("public static void Run()")
i = src.index("{", start)
depth = 0
end = None
for j in range(i, len(src)):
    if src[j] == "{":
        depth += 1
    elif src[j] == "}":
        depth -= 1
        if depth == 0:
            end = j
            break
calls = re.findall(r"^\s{8}([A-Z]\w+)\(\);\s*$", src[i + 1:end], re.M)
assert calls, "没从 Run() 里抽到任何调用 —— 正则或格式变了，先看源头"

body = "\n".join('        Safe("%s", () => %s());' % (c, c) for c in calls)
out = u'''// RuleEngineTest_Diag.cs — **诊断入口**：一条用例抛异常不掐死整轮（2026-10-01 加）
//
// 🔴 **本文件由 `工具/gen_diag_runner.py` 生成，别手改** —— 它按 `RuleEngineTest.Run()` 里的
//    **一模一样的顺序**重生成；`Run()` 的用例表一变就要重跑那个脚本。
//
// 为什么要它：`Run()` 是一串直接调用，**任一条抛异常（哪怕是测试自己解引用 null）整轮就断在那儿**，
// 后面几百条根本跑不到 —— 2026-10-01 改棋盘模型时踩到过（`TestAuraSettle` 里一句 `Board[2].Armor`
// 就吃掉了整轮，只看得到 4 条失败）。
//
// 每条包一层 try/catch：抛异常的记一条失败（**带用例名** + 异常类型 + 消息）继续往下跑。
// ⚠️ **它不是替代品**：验收仍然跑 `RuleEngineTest.Run`（那个才是正本）。
using System;
using UnityEngine;

public static partial class RuleEngineTest
{
    static void Safe(string name, Action a)
    {
        try { a(); }
        catch (Exception e)
        {
            _fail++;   // ⚠️ 异常是从 `a()` 里冒出来的，没有 Check 帮它计数 ⇒ 自己记一条
            string line = $"[{name}] 抛异常：{e.GetType().Name} {e.Message}";
            _failures.Add(line);
            Debug.LogError(P + "   \\u2717 " + line);
        }
    }

    [UnityEditor.MenuItem("Tools/RuleEngine/规则引擎自检（诊断：单条异常不中断）")]
    public static void RunSafe()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Debug.Log(P + "=== 规则引擎自检（诊断模式）开始 ===");

__BODY__

        Debug.Log(P + $"=== 结果：{_pass} 通过 / {_fail} 失败 ===");
        foreach (var f in _failures) Debug.Log(P + "   \\u2717 " + f);
        if (Application.isBatchMode)
            UnityEditor.EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }
}
'''.replace("__BODY__", body)
io.open(OUT, "w", encoding="utf-8", newline="\n").write(out)
print(f"写了 {len(calls)} 条用例 → {OUT}")
