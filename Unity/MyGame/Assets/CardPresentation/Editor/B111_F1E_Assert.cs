// B111_F1E_Assert.cs — B-70「选项 b」：`inheritChild` / `inheritTarget` 接在**展示窗**那棵树上的断言
// （`B111_F1E` · 2026-11-01）
//
// 判据（详见 `Battle/CardDisplayWindowPartial_B111F1E.cs` 文件头）：
//   · 原版 clip `Level Up Card` 的 `Animation` 宿主**就是展示卡本身**（`…/Cards/CardUI`，GameObject &88）
//     ⇒ 这一跳该待在**同时拥有 `Front` 与 `Level Up Effect`** 的那棵树里 = 本窗。
//   · 光效层只有一处 builder：`CardDisplayEffects.Build`（唯一调用点 `CardDisplayWindow.cs:646`）。
//   · 节点名必须**精确**匹配 `Level Up Effect` —— 同层还有 `Level Up Effect Down`（它是前者的子件）。
//   · 只接**槽 0（主卡）**：原版 5 张 `CardUI` 只有 1 张挂 `ParentConstraint`。
//
// 分两半：A 运行时（真跑 `CardDisplayEffects.Build` + 真建一张 `CardView`）· B 源码级（本窗实例化要整套外壳）。
//
// 怎么跑（主对话 · 串行 · 要跑 Unity）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B111_F1E_Assert.Run -logFile -
using System.IO;
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B111_F1E_Assert
{
    const string P = "[B111_F1E] ";
    const string WinPath  = "CardPresentation/Battle/CardDisplayWindow.cs";
    const string PartPath = "CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs";
    const string FxCpPath = "CardPresentation/Shell/CardDisplayEffects.cs";

    static int _ok, _bad;

    /// <summary>调度台 2026-11-01 补：给宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static string ReadSrc(string rel)
    {
        try
        {
            var p = Path.Combine(Application.dataPath, rel);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (System.Exception) { return null; }
    }

    static int Count(string hay, string needle)
    {
        if (hay == null || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    static Transform FindExact(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var hit = FindExact(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    static int CountPrefix(Transform root, string prefix)
    {
        if (root == null) return 0;
        int n = root.name.StartsWith(prefix, System.StringComparison.Ordinal) ? 1 : 0;
        for (int i = 0; i < root.childCount; i++) n += CountPrefix(root.GetChild(i), prefix);
        return n;
    }

    public static void Run()
    {
        _ok = _bad = 0;
        GameObject host = null, card = null;
        try
        {
            // ================================================================ A · 运行时
            // A1 光效层真的建得出来、且里面有 `Level Up Effect`
            host = new GameObject("B111_F1E_Host");
            var fxRoot = CardDisplayEffects.Build(host.transform, 1000f);
            Check(fxRoot != null,
                  "A · 前提：`CardDisplayEffects.Build` 建得出光效层（判据 json + 材质齐）");
            if (fxRoot != null)
            {
                var target = FindExact(fxRoot, "Level Up Effect");
                Check(target != null, "A · 光效层里有名为 `Level Up Effect` 的节点（= `inheritTarget` 的取值口）");
                Check(target != null && target.name == "Level Up Effect",
                      "A · 那个节点的名字**逐字**是 `Level Up Effect`（不是 `… Down`）"
                      + "（实际 " + (target != null ? target.name : "null") + "）");
                // 精确名为什么必要：同层还有 `Level Up Effect Down`（前者是后者的父件）
                Check(CountPrefix(fxRoot, "Level Up Effect") >= 2,
                      "A · 佐证：以 `Level Up Effect` 开头的节点**不止一个**（还有 `… Down`）"
                      + $" ⇒ 必须精确名匹配（实际 {CountPrefix(fxRoot, "Level Up Effect")} 个）");
                // ⚠️ 第 0 子件是它自己的 `… Gfx` quad（`CardDisplayEffects.cs:253` → `ImageQuad.cs:124-125`），`… Down` 是第 1 个
                // ⇒ 层级本身是对的，数子件序会数错。按名字递归找。
                Check(target != null && FindExact(target, "Level Up Effect Down") != null,
                      "A · `Level Up Effect Down` 确实是 `Level Up Effect` 的子件（⛔ 用 `Transform.Find` 只查直接子件会漏）");
            }

            // A2 主卡那一张的 `2DCard/Front`
            var d = CardData.Simple("B111 F1E", 3, 2, 4);
            d.faction = "Ultramarines";
            d.rarity = "common";
            var v = CardView.Create(null, d);
            card = v != null ? v.gameObject : null;
            Check(v != null, "A · 前提：`CardView.Create` 建得出来");
            if (v != null)
            {
                var front = v.FaceContentRoot;
                Check(front != null && front.name == "Front",
                      "A · 主卡的 `FaceContentRoot` 名字是 `Front`（= `inheritChild` 的取值口）"
                      + "（实际 " + (front != null ? front.name : "null") + "）");
                Check(front != null && front.parent != null && front.parent.name == "2DCard",
                      "A · 它的父件是 `2DCard`（原版 `2DCard/Front` 那条路径）"
                      + "（实际 " + (front != null && front.parent != null ? front.parent.name : "null") + "）");
            }

            // ================================================================ B · 源码级
            var win = ReadSrc(WinPath);
            var part = ReadSrc(PartPath);
            var fxCp = ReadSrc(FxCpPath);
            Check(win != null, "B · 前提：读得到 `" + WinPath + "`");
            Check(part != null, "B · 前提：读得到 `" + PartPath + "`");

            if (win != null && part != null)
            {
                // B1 · 调用点接上了（**灭自证：删掉这一跳 ⇒ 红**）
                Check(Count(win, "EnsureLevelUpFx();") == 1,
                      "B1 · `CardDisplayWindow.cs` 里**恰好一处**调 `EnsureLevelUpFx();`"
                      + $"（实际 {Count(win, "EnsureLevelUpFx();")} 处）");
                Check(Count(win, "PlayOpenAnimation(); EnsureLevelUpFx();") == 1,
                      "B1 · 调用与 `PlayOpenAnimation();` **同一行** ⇒ 在 `BuildFan()` 末尾（卡重建后自动重绑）");

                // B2 · 只接槽 0（**灭自证：改成 SlotViews[1] ⇒ 红**）
                Check(Count(part, "var v = SlotViews[0];") == 1,
                      "B2 · 绑定只取 **槽 0（主卡）**（原版 5 张卡里只有 1 张挂约束）"
                      + $"（`var v = SlotViews[0];` 出现 {Count(part, "var v = SlotViews[0];")} 次）");
                Check(Count(part, "SlotViews[1]") == 0 && Count(part, "SlotViews[2]") == 0,
                      "B2 · 没有顺手挂到相关卡（`SlotViews[1..2]` 出现 0 次）");

                // B3 · 两个字段真的被赋值、且取自本窗真实节点
                Check(Count(part, "fx.inheritChild = ") == 1 && Count(part, "fx.inheritTarget = ") == 1,
                      "B3 · `inheritChild` / `inheritTarget` 各被赋值一次"
                      + $"（实际 {Count(part, "fx.inheritChild = ")} / {Count(part, "fx.inheritTarget = ")}）"
                      + "（**灭自证③：删掉任一赋值 ⇒ 红**）");
                Check(part.Contains("v.FaceContentRoot"),
                      "B3 · `inheritChild` 取的是 `SlotViews[0].FaceContentRoot`（= `2DCard/Front`）");
                Check(part.Contains("FindFxNode(_fx, \"Level Up Effect\")"),
                      "B3 · `inheritTarget` 取的是光效层里的 `Level Up Effect`（`_fx` 那棵树）");

                // B4 · ⛔ 不伪造 target（**灭自证：拿窗根/卡体容器顶替 ⇒ 红**）
                Check(Count(part, "LevelUpInheritTarget = ") == 1
                      && part.Contains("LevelUpInheritTarget = FindFxNode(_fx, \"Level Up Effect\");"),
                      "B4 · `LevelUpInheritTarget` **只有一处赋值**，且必须来自 `FindFxNode(_fx, …)`"
                      + $"（实际 {Count(part, "LevelUpInheritTarget = ")} 处）");
                Check(!part.Contains("LevelUpInheritTarget = transform")
                      && !part.Contains("LevelUpInheritTarget = _root")
                      && !part.Contains("LevelUpInheritTarget = _cards"),
                      "B4 · 没有拿窗根 / 卡体容器顶替 target（那会让「钉住」变成原版没有的行为）");

                // B5 · 精确名匹配（防 `StartsWith`/`Contains` 误取 `Level Up Effect Down`）
                Check(part.Contains("root.name == name"),
                      "B5 · `FindFxNode` 用的是**逐字相等**（`root.name == name`），不是前缀/包含匹配");
            }
            if (fxCp != null && win != null)
            {
                Check(win.Contains("CardDisplayEffects.Build(_cards"),
                      "B6 · 光效层确实挂在本窗的 `_cards` 下（`CardDisplayEffects.Build(_cards, …)`）"
                      + "—— 这正是 `Front` 与 `Level Up Effect` 能同处一棵树的理由");
            }
        }
        catch (System.Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 断言抛异常：" + e);
        }
        finally
        {
            if (card != null) Object.DestroyImmediate(card);
            if (host != null) Object.DestroyImmediate(host);
        }
        Done();
    }

    static void Done()
    {
        var line = $"[B111_F1E] === {_ok} 过 / {_bad} 红 ===";
        if (_bad == 0) Debug.Log(line); else Debug.LogError(line);
    }
}
