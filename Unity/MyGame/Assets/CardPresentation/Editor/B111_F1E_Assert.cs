// B111_F1E_Assert.cs — B-70「选项 b」：`inheritChild` / `inheritTarget` 这一跳的断言
// （`B111_F1E` · 2026-11-01；🔴 **2026-11-01（`B-148`）就地改口径**：选项 b 的 partial 已整体退役，见下）
//
// 🔴 **现行口径（`B-148` 退役之后，2026-11-01）**：
//   · 方案 A 的核心**已落地**，升级播放留在 `Shell/CardDetailPopup` ——
//     光效层 `CardDisplayEffects.BuildUnder2DCard(SlotViews[0].FaceRoot)` 挂在**主卡 `2DCard`** 下（`B-195`），
//     驱动 `LevelUpEffect.DoAnim();` 在 `PlayUpgradeAnimation()` 里（唯一调用点 `RebuildKeepingState(); PlayUpgradeAnimation(after);`）。
//   · **`Battle/CardDisplayWindowPartial_B111F1E.cs` 已整体删除**：战斗窗**不建**光效层
//     （`_fx` 恒 null；原版战斗窗里也没有 `Level Up Effect` 那一族）⇒ 原来开窗那一拍的
//     `EnsureLevelUpFx();` 只会每次挂一个**永不播**的 `CardDisplayLevelUp` 并打一条「接不上」告警。
//
// 判据（原版）：
//   · 原版 clip `Level Up Card` 的 `Animation` 宿主**就是展示卡本身**（`…/Cards/CardUI`，GameObject &88）
//     ⇒ 这一跳该待在**同时拥有 `Front` 与 `Level Up Effect`** 的那棵树里 = 主卡 `2DCard` 那一层。
//   · 节点名必须**精确**匹配 `Level Up Effect` —— 同层还有 `Level Up Effect Down`（它是前者的子件）。
//   · 只接**槽 0（主卡）**：原版 5 张 `CardUI` 只有 1 张挂 `ParentConstraint`。
//
// 分三段：A 运行时（真跑 `CardDisplayEffects.Build` + 真建一张 `CardView`）· B 源码级（退役形态 + 生产链）
//         · 🔴 **C 真实例化（`B-222` 补的盲区）**。
// ⚠️ B 段是**读两个文件的文本**：一条证「旧的没了」（展示窗 + 那个 partial 文件）、一条证「新的在」（`CardDetailPopup`）——
//    ⛔ 别只留一条，否则「两边一起改回去」也会绿。
// 🔴 **C 段为什么必须有（`B-222`）**：A/B 两段**从不实例化 `CardDisplayWindow`** ⇒「战斗窗开窗那一拍会挂一个永不播的
//    `CardDisplayLevelUp` + 打一条『接不上』告警」这件事，**24 个宿主全绿也照不出来**（`B-148` 退役那一半就是这样漏掉的）。
//    C 段建**真窗**、真开一次窗，断 ① 编译产物里没有那些成员（反射）② 树里 `CardDisplayLevelUp` = 0
//    ③ 没有「接不上」告警；C5 是**正控**（临时挂一个必须数到 1），防 C3 空转。
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
    const string WinPath    = "CardPresentation/Battle/CardDisplayWindow.cs";
    /// <summary>已退役的那个部分类 —— 本文件断言它**不存在**（`B-148`）。</summary>
    const string OldPartPath = "CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs";
    /// <summary>方案 A 的生产链所在（光效层挂点 + 升级驱动）。</summary>
    const string PopupPath  = "CardPresentation/Shell/CardDetailPopup.cs";
    const string FxCpPath   = "CardPresentation/Shell/CardDisplayEffects.cs";

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
                // ⚠️ 第 0 子件是它自己的 `… Gfx` quad（`CardDisplayEffects.BuildImage` 里的 `ImageQuad.Create(... im.name + " Gfx")`），`… Down` 是第 1 个
                //    2026-11-01 起改指**方法名**：`W-8`/`W-17` 两次改动已让行号漂过两回（`:253`→`:313`→…），⛔ 别再写死行号
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
            var popup = ReadSrc(PopupPath);
            var fxCp = ReadSrc(FxCpPath);
            Check(win != null, "B · 前提：读得到 `" + WinPath + "`");
            Check(popup != null, "B · 前提：读得到 `" + PopupPath + "`");

            if (win != null && popup != null)
            {
                // ── B1 · 「**旧的没了**」（`B-148` 退役）
                //     🧨 把 `CardDisplayWindowPartial_B111F1E.cs` 放回来、或把 `EnsureLevelUpFx` 加回展示窗 ⇒ 红
                Check(!File.Exists(Path.Combine(Application.dataPath, OldPartPath)),
                      "B1 · `CardDisplayWindowPartial_B111F1E.cs` **已删除**（`B-148` 退役：战斗窗不建光效层）"
                      + "（实际 " + (File.Exists(Path.Combine(Application.dataPath, OldPartPath)) ? "**还在**" : "不存在") + "）");
                Check(Count(win, "EnsureLevelUpFx") == 0,
                      "B1 · `CardDisplayWindow.cs` 里**不再出现** `EnsureLevelUpFx`（开窗那一拍已不再挂空组件）"
                      + $"（实际 {Count(win, "EnsureLevelUpFx")} 处）"
                      + "｜🧨 把那一跳加回去 ⇒ 红");
                Check(Count(win, "LevelUpFx") == 0 && Count(win, "BindLevelUpInheritTargets") == 0,
                      "B1 · `CardDisplayWindow.cs` 里不再引用 partial 的读数口 / 绑定方法（退役后不留悬挂引用）"
                      + $"（`LevelUpFx` {Count(win, "LevelUpFx")} 处 · `BindLevelUpInheritTargets` {Count(win, "BindLevelUpInheritTargets")} 处）");

                // ── B2 · 「**新的在**」：方案 A 的生产链仍在 `CardDetailPopup`（⛔ 别又搬回展示窗）
                //     🧨 删驱动的 `DoAnim` / 删那一句调用 / 把光效层挂到别处 ⇒ 红
                Check(Count(popup, "LevelUpEffect.DoAnim();") == 1,
                      "B2 · 升级演出的驱动 `LevelUpEffect.DoAnim();` 仍在 `CardDetailPopup`"
                      + $"（实际 {Count(popup, "LevelUpEffect.DoAnim();")} 处）");
                Check(Count(popup, "RebuildKeepingState(); PlayUpgradeAnimation(after);") == 1,
                      "B2 · 唯一调用点仍接在升级流程里（`RebuildKeepingState(); PlayUpgradeAnimation(after);`）");
                Check(Count(popup, "void PlayUpgradeAnimation(") == 1,
                      "B2 · `PlayUpgradeAnimation` 仍在 `CardDetailPopup` 里定义"
                      + $"（实际 {Count(popup, "void PlayUpgradeAnimation(")} 处）");
                Check(Count(popup, "CardDisplayEffects.BuildUnder2DCard(SlotViews[0].FaceRoot)") == 1,
                      "B2 · 光效层仍挂在**主卡 `2DCard`** 下（`B-195`：⛔ 不是战斗窗的层根）"
                      + "｜🧨 换成 `_cards` / 窗根 ⇒ 红");
            }
            if (fxCp != null && win != null)
            {
                // 🔴 2026-11-01（`B112W_B195`）：光效层**不再**建在战斗窗的 `_cards` 下 ——
                //    那一层来自**主菜单**判据（`mainmenu_effects.json`），原版只在菜单版 `CardDisplayWindow` 里有；
                //    我们的战斗窗建它是**搬错了窗**。现在改由 `CardDisplayEffects.BuildUnder2DCard(...)`
                //    挂在**主卡 `2DCard`** 之下（`Front` 与 `Level Up Effect` 仍是同一棵树，只是树根对了）。
                Check(!win.Contains("CardDisplayEffects.Build(_cards"),
                      "B6 · 光效层**不再**建在战斗窗的 `_cards` 下（`B-195`：那一层属主菜单判据，建在这边是搬错窗）"
                      + "｜🧨 把 `CardDisplayEffects.Build(_cards, …)` 加回去 ⇒ 红");
            }

            // ================================================================ C · 真实例化（`B-222` 补的盲区）
            //   🔴 **为什么要有这一段**：A 段测的是 `CardDisplayEffects` **自己建**的那一层、B 段读的是**源码文本** ——
            //   两段**从不实例化 `CardDisplayWindow`** ⇒「战斗窗里 `_fx` 恒 null ⇒ 开窗那一拍挂一个**永不播**的
            //   `CardDisplayLevelUp` + 打一条『接不上』告警」这件事，**24 个宿主全绿也照不出来**。
            //   ⇒ 这里建一具**真窗**、真走一次开窗，把退役那一层的三种痕迹分别断出来：
            //     ① **编译产物**里没有那些成员（反射，⛔ 不是文本 grep —— 换个文件把它写回来也逃不掉）
            //     ② 开完窗，整棵窗树里 `CardDisplayLevelUp` = **0**（旧代码每次开窗都挂一个）
            //     ③ 开窗那一拍**没有**「接不上」告警
            //   末尾再补一条 **C5 正控**：临时挂一个 `CardDisplayLevelUp`，同一条扫描口必须数到 1
            //   （否则 ② 是**空转**：扫描口写错时它也恒 0 ⇒ 把 `EnsureLevelUpFx` 加回去也不会红）。
            CardDisplayWindow cdw = null;
            GameObject probe = null;
            try
            {
                try { cdw = CardDisplayWindow.Create(null); }
                catch (System.Exception ex) { Debug.LogWarning(P + "C 段建真窗抛了：" + ex); cdw = null; }
                // C0 = 前置硬检查（⛔ 不静默跳过）
                Check(cdw != null,
                      "C0 **前提**：`CardDisplayWindow.Create(null)` 建得出真窗（推荐宿主 = `Editor/BattleScene.cs`）"
                      + "—— 这条红就说明 C1–C5 **没跑**");
                if (cdw != null)
                {
                    var ty = typeof(CardDisplayWindow);
                    const System.Reflection.BindingFlags Any =
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic;

                    // ── C1 · 退役那一层在**编译产物**里彻底不存在
                    Check(ty.GetMethod("EnsureLevelUpFx", Any) == null,
                          "C1 · 类型上没有 `EnsureLevelUpFx`（`B-148` 退役的那一跳在**编译产物**里也没了，"
                          + "⛔ 不是只靠源码 grep）"
                          + "｜🧨 把 partial 的 `EnsureLevelUpFx` 加回来（放在**任何**文件里都算）⇒ 红");
                    Check(ty.GetProperty("LevelUpFx", Any) == null && ty.GetProperty("LevelUpInheritTarget", Any) == null,
                          "C1-b · 也没有 `LevelUpFx` / `LevelUpInheritTarget` 两个读数口（退役不留悬挂 API）");
                    Check(ty.GetField("_fx", Any) == null,
                          "C1-c · 也没有 `_fx` 字段（战斗窗**不建**光效层 ⇒ 它只能恒 null，留着只会误导）"
                          + "｜🧨 把 `Transform _fx;` 加回去 ⇒ 红");
                    // C1-d **正控**：同一套反射参数必须找得到**已知存在**的私有成员——
                    //   否则「找不到」可能只是 flags 写错（那样 C1/C1-b/C1-c 三条会一起空转成绿）
                    Check(ty.GetMethod("BuildFan", Any) != null && ty.GetField("_closePending", Any) != null,
                          "C1-d · 正控：同一套反射参数**找得到**私有 `BuildFan` 与私有字段 `_closePending`"
                          + "（证明上面三条的「找不到」不是 flags 写错造成的空转）");

                    // ── C2 · 真走一次开窗（先断前提，再断结论 —— ⛔ 别让「窗没开」把 C3/C4 变成空转）
                    var shown = CardData.Simple("B111 F1E 真窗", 3, 2, 4);
                    shown.faction = "Ultramarines";     // ⚠️ `Simple` 的 `faction` 恒 null（本工程踩过 ⇒ 卡框/卡背取不到）
                    var logs = new System.Collections.Generic.List<string>();
                    Application.LogCallback onLog = delegate(string cond, string stack, LogType type)
                    {
                        if (!string.IsNullOrEmpty(cond) && (cond.Contains("接不上") || cond.Contains("[展示窗]")))
                            logs.Add(cond);
                    };
                    Application.logMessageReceived += onLog;
                    try { cdw.Show(shown, null, null); }
                    catch (System.Exception ex) { Check(false, "C2 · `Show(...)` 抛了：" + ex.Message); }
                    finally { Application.logMessageReceived -= onLog; }

                    Check(cdw.Visible, "C2 **前提**：开完窗 `Visible == true`（开窗流程真的跑了）");
                    Check(cdw.Card != null, "C2-b **前提**：槽 0 的主卡建出来了（`Card != null`）");

                    // ── C3 · 开窗那一拍不再挂 `CardDisplayLevelUp`（旧代码每次开窗都挂一个永不播的）
                    int fxCount = cdw.GetComponentsInChildren<CardDisplayLevelUp>(true).Length;
                    Check(fxCount == 0,
                          "C3 · 开完窗，整棵窗树里 `CardDisplayLevelUp` = **0**（旧 `EnsureLevelUpFx` 每次都挂一个）"
                          + $"（实际 {fxCount} 个）"
                          + "｜🧨 把 `EnsureLevelUpFx()` 加回开窗那一拍 ⇒ 红");

                    // ── C4 · 开窗那一拍不再打「接不上」告警
                    bool warned = false;
                    for (int i = 0; i < logs.Count; i++) if (logs[i].Contains("接不上")) warned = true;
                    Check(!warned,
                          "C4 · 开窗过程中没有「接不上」告警（旧 `BindLevelUpInheritTargets` 每次开窗打一条）"
                          + $"（捕获 `[展示窗]`/`接不上` 日志 {logs.Count} 条，「接不上」{(warned ? "有" : "0")} 条）");

                    // ── C5 · 正控 / 灭自证：同一条扫描口必须**能数到 1**
                    probe = new GameObject("B111_F1E_LevelUpProbe");
                    probe.transform.SetParent(cdw.transform, false);
                    probe.AddComponent<CardDisplayLevelUp>();
                    int probeCount = cdw.GetComponentsInChildren<CardDisplayLevelUp>(true).Length;
                    Check(probeCount == 1,
                          "C5 · 正控：临时挂一个 `CardDisplayLevelUp` ⇒ 同一条扫描口数到 **1**"
                          + $"（实际 {probeCount}）—— 证明 C3 不是空转（扫描口写错时 C3 恒绿）");
                }
            }
            finally
            {
                if (probe != null) Object.DestroyImmediate(probe);
                if (cdw != null)
                {
                    // ⛔ 只拆本段建的那两个节点（窗本体 + 它的世界壳）—— 共享子画布归 `HudCanvas`，别动
                    var cdwShell = cdw.CardsShell;
                    var cdwGo = cdw.gameObject;
                    if (cdwShell != null) Object.DestroyImmediate(cdwShell.gameObject);
                    if (cdwGo != null) Object.DestroyImmediate(cdwGo);
                }
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
