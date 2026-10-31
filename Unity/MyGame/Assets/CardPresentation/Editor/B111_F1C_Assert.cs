// B111_F1C_Assert.cs — 「`2DCard/Front` 在升级动画前 1.45 s 被钉在 `Level Up Effect` 上」的断言（B-70 · 2026-11-01）
//
// 判据（三源，全部 2026-11-01 现读；详见 `Core/CardDisplayLevelUpPartial_B111F1C.cs` 文件头）：
//   ① clip `Level Up Card.anim`：`path: 2DCard/Front` 上两条 float 曲线 `inheritPosition` / `inheritRotation`，
//      `t=0 → 1`、`t=1.45 → 0`，`inSlope/outSlope = Infinity`（**阶跃**）；1.45 = 同一 clip 的 `ChangeFrame` 事件时刻。
//   ② 场景实例：`Card Displayer Menu For Menu/…/CardUI/2DCard/Front` 的 `ParentConstraint`
//      = { targetParent `2DCard/Level Up Effect` · pos 1 · rot 1 · **scale 0** · updateMethod 0(Update) · alignAxis 0(X) · autoInit 1 }。
//      同场景另 4 张 `CardUI (1..4)` 的 `Front` **没有**这个组件（只有 RectTransform）。
//   ③ 方法体：`ParentConstraint.Init` 抓 `childLocalPos = target.InverseTransformPoint(child.position)` +
//      `childRotOffset = FromToRotation(target.right, child.right)`；`Sync` = `child.position = target.TransformPoint(childLocalPos)`、
//      `child.rotation = target.rotation * childRotOffset`。
//
// 怎么跑（主对话 · 串行 · 要跑 Unity）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B111_F1C_Assert.Run -logFile -
//   判绿红：日志里 `[B111_F1C] === X 过 / Y 红 ===`。
//
// ⚠️ 只读工程资产、不存盘（临时 GameObject，结尾销毁）。
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B111_F1C_Assert
{
    const string P = "[B111_F1C] ";

    // 场景实例的真值（`MainMenu Warpforge.unity`：`2DCard/Front` 与 `2DCard/Level Up Effect` 的 RectTransform）
    static readonly Vector3 TargetLocalPos   = new Vector3(-0.00102f, -0.023419991f, 0f);
    static readonly Vector3 TargetLocalScale = new Vector3(0.004545454f, 0.004545454f, 0.004545454f);
    static readonly Vector3 ChildLocalPos    = new Vector3(0f, 0.08f, 0f);   // = `CardView.FrontAposY`

    static int _ok, _bad;

    /// <summary>调度台 2026-11-01 补：给宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static bool NearV(Vector3 a, Vector3 b, float eps = 1e-5f)
    {
        return Mathf.Abs(a.x - b.x) <= eps && Mathf.Abs(a.y - b.y) <= eps && Mathf.Abs(a.z - b.z) <= eps;
    }

    static bool NearQ(Quaternion a, Quaternion b, float deg = 1e-3f)
    {
        return Quaternion.Angle(a, b) <= deg;
    }

    public static void Run()
    {
        _ok = _bad = 0;
        GameObject root = null;
        try
        {
            // ---------------------------------------------------------------- 0 · 常量 = clip 真值
            Check(Mathf.Abs(CardDisplayLevelUp.InheritEndTime - 1.4500000476837158f) < 1e-6f,
                  "0 · 截止时刻 = clip `inheritPosition/inheritRotation` 的第二个键 t = 1.4500000476837158"
                  + $"（实际 {CardDisplayLevelUp.InheritEndTime:R}）");
            Check(CardDisplayLevelUp.InheritEndTime == CardDisplayLevelUp.ClipChangeFrameTime,
                  "0 · 截止时刻 == 同一 clip 的 `ChangeFrame` 事件时刻（两条曲线与动画事件同刻）");
            Check(CardDisplayLevelUp.InheritAlignAxis == 0,
                  "0 · `alignAxis` = 0（X / `Transform.right`）—— 与场景实例一致");

            // ---------------------------------------------------------------- 1 · 建出场景那对节点
            root = new GameObject("B111_F1C_Root");
            var target = new GameObject("Level Up Effect").transform;
            target.SetParent(root.transform, false);
            target.localPosition = TargetLocalPos;
            target.localScale = TargetLocalScale;
            var child = new GameObject("Front").transform;
            child.SetParent(root.transform, false);
            child.localPosition = ChildLocalPos;

            var c = root.AddComponent<CardDisplayLevelUp>();
            Check(c != null, "1 · 前提：`CardDisplayLevelUp` 加得上");

            // ---- 1a · 没接线时**不许静默生效**
            c.DoAnim();
            Check(!c.InheritActive,
                  "1a · `inheritChild/inheritTarget` 没接线 ⇒ 不开窗（⛔ 不静默：会打一条警告），且不抛");

            // ---- 1b · 接线后重开
            c.inheritChild = child;
            c.inheritTarget = target;
            Vector3 childWorld0 = child.position;
            Quaternion childRot0 = child.rotation;

            c.DoAnim();
            Check(c.InheritActive, "1b · `DoAnim` ⇒ 进入钉住窗口（0 → 1.45 s）");
            Check(c.InheritCaptured, "1b · 原版 `Init` 的偏移量已抓到（`childLocalPos` / `childRotOffset`）");
            Check(NearV(c.InheritLocalOffset, target.InverseTransformPoint(childWorld0), 1e-4f),
                  "1b · 抓到的偏移 == `target.InverseTransformPoint(child.position)`（原版 `Init` 第一条）"
                  + $"（实际 {c.InheritLocalOffset}）");
            Check(NearV(child.position, childWorld0) && NearQ(child.rotation, childRot0),
                  "① t=0：卡面世界位/旋 **跟开播前一模一样**（钉住窗口起点不跳）");

            // ---- 2 · 目标动一下 ⇒ 子节点必须跟着（**这是「粘住」的全部含义**；删掉这一跳 ⇒ 红）
            int applied0 = c.InheritAppliedCount;
            target.localPosition = TargetLocalPos + new Vector3(0.05f, 0.02f, 0f);
            c.Tick(0.5f);
            Vector3 expectPos = target.TransformPoint(c.InheritLocalOffset);
            Check(NearV(child.position, expectPos, 1e-4f),
                  "② 0.5 s：target 移动后 `Front` 跟到 `target.TransformPoint(childLocalPos)`"
                  + $"（期望 {expectPos}，实得 {child.position}）—— 删掉这一跳 ⇒ 这条红");
            Check(c.InheritAppliedCount > applied0, "② `Front` 真的被写过（计数在涨）");

            // 旋转那一支（原版 `if (inheritRotation) transform.rotation = target.rotation * childRotOffset`）
            target.localRotation = Quaternion.Euler(0f, 0f, 15f);
            c.Tick(0.1f);
            Check(NearQ(child.rotation, target.rotation * c.InheritRotOffset, 1e-3f),
                  "② 0.6 s：target 旋转后 `Front` 的旋转 = `target.rotation * childRotOffset`"
                  + $"（实际 {child.rotation.eulerAngles}）");

            // ---- 3 · 1.45 s 之后解绑（**删掉 1.45 这道闸 ⇒ 红**）
            c.Tick(1.0f);            // 累计 1.5 s > 1.45
            Check(!c.InheritActive, $"③ {c.InheritTime:F3} s（>1.45）：已过 `ChangeFrame` ⇒ 钉住窗口关闭");
            Vector3 frozenPos = child.position;
            Quaternion frozenRot = child.rotation;
            int appliedAtStop = c.InheritAppliedCount;
            target.localPosition = TargetLocalPos + new Vector3(0.3f, -0.2f, 0f);
            target.localRotation = Quaternion.Euler(0f, 0f, -40f);
            c.Tick(0.2f);
            Check(NearV(child.position, frozenPos) && NearQ(child.rotation, frozenRot),
                  "③ 1.7 s：target 再动，`Front` **不跟**（停在最后一帧的世界位姿）—— 去掉 1.45 闸 ⇒ 这条红");
            Check(c.InheritAppliedCount == appliedAtStop, "③ 过了 1.45 s 之后**不再写** `Front`");

            // ---- 4 · 再开一次：窗口重置（原版每次 `DoAnim` 播一遍，曲线从头来）
            c.DoAnim();
            Check(c.InheritActive && Mathf.Abs(c.InheritTime) < 1e-4f,
                  "4 · 第二次 `DoAnim` ⇒ 窗口重新打开、计时归 0（clip 每次都从头播）");
            c.Tick(0.1f);
            Check(NearV(child.position, c.inheritTarget.TransformPoint(c.InheritLocalOffset), 1e-4f),
                  "4 · 重开后仍然按「当前 target 位姿 + 抓到的偏移」钉（偏移量不重抓 —— 引用没变）");
        }
        catch (System.Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 断言抛异常：" + e);
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
        }
        Done();
    }

    static void Done()
    {
        var line = $"[B111_F1C] === {_ok} 过 / {_bad} 红 ===";
        if (_bad == 0) Debug.Log(line); else Debug.LogError(line);
    }
}
