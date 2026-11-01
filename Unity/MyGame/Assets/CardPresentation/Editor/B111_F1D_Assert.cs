// B111_F1D_Assert.cs — 「`inheritChild` / `inheritTarget` 接上了、且只给该挂的那一张挂」的断言（B-70 最后一跳 · 2026-11-01）
//
// 判据（详见 `Shell/CardDetailPopupPartial_B111F1D.cs` 文件头）：
//   ① 原版 `Level Up Card` 的 `Animation` 挂在**那张展示卡**（`…/Cards/CardUI`，GameObject &88）上，
//      不是 `Upgrade Panel` ⇒ clip 的 `2DCard/Front` 是相对这张卡解析的。
//   ② 同批 5 张 `CardUI` 里**只有 1 张**（无后缀）的 `2DCard/Front` 有 `ParentConstraint` ⇒ 「只挂一张」是原版事实。
//   ③ 我们这边 `inheritChild` = `SlotView(0).FaceContentRoot`（= `CardView` 的 `2DCard/Front`）；
//      `inheritTarget` = `FindNewest("Level Up Effect")` —— ⚠️ **本窗不建这个节点**（只在 `Battle/CardDisplayWindow`），
//      所以今天恒 `null`。这条**不是接线 bug**，是结构性缺口（报告 §没查清/§顺手发现）。
//
// ⚠️ 本断言分两半：
//    A 运行时（真的建一个 `CardView`，验 `FaceContentRoot` 就是 `2DCard/Front`）
//    B 源码级（本批**不跑 Shell**，`CardDetailPopup` 实例化要整套外壳 ⇒ 只能读源码核「接上了 / 只接一张 / 没伪造 target」）
//
// 怎么跑（主对话 · 串行 · 要跑 Unity）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B111_F1D_Assert.Run -logFile -
using System.IO;
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B111_F1D_Assert
{
    const string P = "[B111_F1D] ";
    const string PopupPath = "CardPresentation/Shell/CardDetailPopup.cs";
    const string PartPath  = "CardPresentation/Shell/CardDetailPopupPartial_B111F1D.cs";

    static int _ok, _bad;

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
        if (hay == null || needle == null || needle.Length == 0) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    public static void Run()
    {
        _ok = _bad = 0;
        GameObject go = null;
        try
        {
            // ================================================================ A · 运行时
            var d = CardData.Simple("B111 F1D", 3, 2, 4);
            d.faction = "Ultramarines";
            d.rarity = "common";
            var v = CardView.Create(null, d);
            go = v != null ? v.gameObject : null;
            Check(v != null, "A · 前提：`CardView.Create` 建得出来");
            if (v != null)
            {
                var front = v.FaceContentRoot;
                Check(front != null, "A · `CardView.FaceContentRoot` 非空（= `inheritChild` 的取值口）");
                Check(front != null && front.name == "Front",
                      "A · `FaceContentRoot` 的名字是 `Front`（不是退化成 `2DCard` 那一档）"
                      + "（实际 " + (front != null ? front.name : "null") + "）");
                Check(front != null && front.parent != null && front.parent.name == "2DCard",
                      "A · 它的父件是 `2DCard`（原版 `2DCard/Front` 那条路径）"
                      + "（实际 " + (front != null && front.parent != null ? front.parent.name : "null") + "）");
            }

            // ================================================================ B · 源码级
            var popup = ReadSrc(PopupPath);
            var part = ReadSrc(PartPath);
            Check(popup != null, "B · 前提：读得到 `" + PopupPath + "`");
            Check(part != null, "B · 前提：读得到 `" + PartPath + "`");

            if (popup != null && part != null)
            {
                // B1 · 调用点接上了（**灭自证：删掉这一跳 ⇒ 这条红**）
                Check(Count(popup, "BindLevelUpInheritTargets();") == 1,
                      "B1 · `CardDetailPopup.cs` 里**恰好一处**调 `BindLevelUpInheritTargets();`"
                      + $"（实际 {Count(popup, "BindLevelUpInheritTargets();")} 处）");
                Check(Count(popup, "LevelUpEffect.pulseTarget = SlotView(0); BindLevelUpInheritTargets();") == 1,
                      "B1 · 调用与 `pulseTarget = SlotView(0)` **同一行** ⇒ 它在 `RewireLevelUpTargets()` 里"
                      + "（那一处同时覆盖「`DoAnim` 之前」与「重建之后」两条路）");

                // B2 · 只给槽 0 挂（**灭自证：改成 SlotView(1) ⇒ 这条红**）
                Check(Count(part, "var v = SlotView(0);") == 1,
                      "B2 · 绑定只取 **槽 0**（原版 5 张卡里只有 1 张挂约束）"
                      + $"（`var v = SlotView(0);` 出现 {Count(part, "var v = SlotView(0);")} 次）");
                Check(Count(part, "SlotView(1)") == 0 && Count(part, "SlotView(2)") == 0
                      && Count(part, "SlotView(3)") == 0,
                      "B2 · 没有遍历/顺手挂到别的槽（`SlotView(1..3)` 出现 0 次）");

                // B3 · 两个字段真的被赋值
                Check(Count(part, "fx.inheritChild = ") == 1 && Count(part, "fx.inheritTarget = ") == 1,
                      "B3 · `inheritChild` / `inheritTarget` 各被赋值一次"
                      + $"（实际 {Count(part, "fx.inheritChild = ")} / {Count(part, "fx.inheritTarget = ")}）");
                Check(part.Contains("v.FaceContentRoot"),
                      "B3 · `inheritChild` 取的是 `SlotView(0).FaceContentRoot`（= `2DCard/Front`）");
                Check(part.Contains("FindNewest(\"Level Up Effect\")"),
                      "B3 · `inheritTarget` 取的是 `FindNewest(\"Level Up Effect\")`（精确名匹配 ⇒ 不会误取 `… Down`）");

                // B4 · ⛔ 不伪造 target（**灭自证：拿窗根/卡根顶替 ⇒ 这条红**）
                Check(Count(part, "LevelUpInheritTarget = ") == 1
                      && part.Contains("LevelUpInheritTarget = FindNewest(\"Level Up Effect\");"),
                      "B4 · `LevelUpInheritTarget` **只有一处赋值**，且必须来自 `FindNewest`"
                      + $"（实际 {Count(part, "LevelUpInheritTarget = ")} 处）");
                Check(!part.Contains("LevelUpInheritTarget = transform")
                      && !part.Contains("LevelUpInheritTarget = LevelUpInheritChild"),
                      "B4 · 没有拿窗根 / 卡面层顶替 target（那会让「钉住」变成原版没有的行为）");

                // B5 · 🔴 2026-11-01（`B112W_B195`/`B-196`）：**语义已反转** —— 原来这里是「本窗不建 `Level Up Effect`」
                //    的结构性缺口声明；现在主卡 `2DCard` 下**真的建了**那层，旧口径必须写成「已作废」而不是继续挂着。
                Check(part.Contains("已作废") && part.Contains("BuildUnder2DCard"),
                      "B5 · 文件头把**旧口径「本窗不建 `Level Up Effect`」标成已作废**、并写明现在由 `BuildUnder2DCard` 建"
                      + "｜🧨 把旧缺口声明改回「现役」口径 / 删掉 `BuildUnder2DCard` 那句 ⇒ 红");
            }
        }
        catch (System.Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 断言抛异常：" + e);
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
        Done();
    }

    static void Done()
    {
        var line = $"[B111_F1D] === {_ok} 过 / {_bad} 红 ===";
        if (_bad == 0) Debug.Log(line); else Debug.LogError(line);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  🆕 2026-11-01（`B-216` 接线）：**宿主读回口**
    //  本件此前 `grep -rln "B111_F1D_Assert"` 只命中它自己 ⇒ **17 条 `Check` 从未执行**。
    //  要挂进宿主就必须有 `Pass`/`Fail`（宿主聚合行的形状 = `X.Run(); _sink.Pass += X.Pass; _sink.Fail += X.Fail;`）。
    //  ⚠️ 追加在类尾 = 不动上面任何行号。
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _bad; } }
}
