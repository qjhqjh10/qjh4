// B98A8_Assert.cs — 第九十八会话 · 波 5c · 执行代理 `B98_A8`
//   盯一件事：**开包窗三层「谁压谁」的遮挡顺序**（背景 / 卡包实物 / 5 张卡）。
//
// ============================ 它盯的是什么 ============================
//   A5 把三层摆成 🔶 自己设计（背景 z=0 · 实物 z=−0.25 · `Cards` z=−0.5），当时说「画布 local −z 朝相机还是
//   离相机**本机拿不到判据**」。`B98_A8` 把这个判据查实了，并把它固化成一条**顺序不变式**：
//     · 方向判据（`资料/完全复刻/报告/B98_A8.md` §甲）：主菜单相机 `Camera_602` 的 `m_LocalRotation` = 恒等
//       ⇒ 视线沿 +z；原版全部画布 `Canvas/*` 的 `m_LocalRotation` 也是恒等；`ScreenSpaceCamera` 根画布
//       `Canvas_1077` 被 Unity 摆在相机前方 `planeDistance`、旋转 = 相机旋转 ⇒ **画布 local +z 恒 = 相机 forward**
//       ⇒ **local −z 恒 = 朝相机 = 更近**。
//     · 原版前后：`Booster pack Background`(z=0) 最远 → 包实物(`anchor −202` ⇒ 更负 ⇒ 更近) → 5 张卡
//       **不靠 z**：`Cards` 是一颗嵌套 WorldSpace `Canvas`（`m_OverrideSorting=1 · m_SortingOrder=2`）⇒ 卡叠在最上。
//     · 我们的等价：`CardsFrontZ(−0.5) < VisualItemZ(−0.25) < 0`（z 越小越近）⇒ 背景 < 实物 < 卡（近→远反读亦然）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B98A8_Assert.Run -logFile "d:/4/_tmp_view/b98a8.log"
//   判绿 = 小结那行 `✗` 为 0。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `VisualItemZ` 改成 `0` 或正数 ⇒ 第 1 段「实物 z < 0」红。
//   · 把 `CardsFrontZ` 改成比 `VisualItemZ` 大（更远，例如 −0.1）⇒ 第 1 段「卡比实物更近」红、第 2 段运行态也红。
//   · `BuildVisualItemLayer` 里不摆 `localPosition.z`（留 0）⇒ 第 2 段「实物 world z = VisualItemZ」红。
//   · 把 `Cards` 那层的 z 从 `CardsFrontZ` 改回 0（不挪到前面）⇒ 第 2 段「卡 world z < 实物 world z」红。
//   · 把背景挪到包前面（背景 world z 变负）⇒ 第 2 段「包 world z < 背景 world z」红。
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B98A8_Assert
{
    const string P = "B98A8 ";

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    static void CheckNear(float got, float want, string label, float eps = 1e-4f)
        => Check(Mathf.Abs(got - want) <= eps,
                 label + "（got " + got.ToString("F4") + " · want " + want.ToString("F4") + "）");

    static void CheckLt(float a, float b, string label)
        => Check(a < b, label + "（" + a.ToString("F4") + " < " + b.ToString("F4") + "）");

    static BoosterPackOpenWindow _win;
    static GameObject _camGo;

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 开包窗三层遮挡顺序自检（`B98_A8`）开始 ===");

        Section1_Constants();
        Section2_RuntimeOrder();

        if (_win != null) { Object.DestroyImmediate(_win.gameObject); _win = null; }
        if (_camGo != null) { Object.DestroyImmediate(_camGo); _camGo = null; }

        Debug.Log(P + "=== 小结：✓ " + _ok + " · ✗ " + _bad + " ===");
        if (_bad > 0) Debug.LogError(P + "有 " + _bad + " 条失败");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  1 · 三层顺序常量（不建场景）—— 「−z = 朝相机 = 近」下的顺序不变式
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section1_Constants()
    {
        Debug.Log(P + "--- 1 · 三层顺序常量 ---");

        // 背景平面 = z 0（`MenuDraw.Rect` 建在窗根、窗根在原点 ⇒ local z = 0）。
        // 判据（报告 §甲）：−z 朝相机 ⇒ 包实物必须「更负」才在背景前；卡必须再「更负」才在实物前。
        Check(BoosterPackOpenWindow.VisualItemZ < 0f,
              "包实物 z < 0（比背景平面 z=0 更近 ⇒ 不被背景透明 quad 盖住）");
        CheckLt(BoosterPackOpenWindow.CardsFrontZ, BoosterPackOpenWindow.VisualItemZ,
                "5 张卡那层比包实物更近（原版靠画布 sortingOrder=2 叠最上，我们靠更负的 z 复现）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  2 · 建一扇真窗（第 0 页第 4 件 = Leviathan，与 A5 同一件）⇒ 量三层的**世界 z**
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section2_RuntimeOrder()
    {
        Debug.Log(P + "--- 2 · 建窗 + 三层世界 z ---");

        BoosterPackAssets.Reset();

        _camGo = new GameObject("Main Camera (B98A8)");
        _camGo.tag = "MainCamera";
        var cam = _camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.aspect = LayoutSpace.DesignAspect;
        LayoutSpace.Apply(cam);

        var go = new GameObject("Booster Pack Open Window (B98A8)");
        _win = go.AddComponent<BoosterPackOpenWindow>();
        _win.Show(0, 3);

        // ---- 前提（硬检查，⛔ 不用 `if (x != null) Check(...)` 静默跳过）----
        Check(_win.BgQuad != null, "（前提）`Booster pack Background` quad 建出来了");
        Check(_win.AnchorNode != null, "（前提）`Booster Animation Parent`（= 原版 `boosterPackAnchor`）建出来了");
        Check(_win.VisualItem != null, "（前提）卡包实物建出来了（原版 `Instantiate(prefab, boosterPackAnchor)`）");
        var cards = _win.AnchorNode != null ? _win.AnchorNode.Find("Cards") : null;
        Check(cards != null, "（前提）`Booster Animation Parent/Cards` 在");

        if (_win.BgQuad == null || _win.VisualItem == null || cards == null) return;

        float bgZ = _win.BgQuad.transform.position.z;
        float packZ = _win.VisualItem.transform.position.z;
        float cardsZ = cards.position.z;

        Debug.Log(P + "三层 world z：背景=" + bgZ.ToString("F4") + " · 实物=" + packZ.ToString("F4")
                  + " · 卡=" + cardsZ.ToString("F4") + "（−z = 朝相机 = 近）");

        CheckNear(bgZ, 0f, "背景在画布平面 z=0");
        CheckLt(packZ, bgZ, "包实物在背景**前**（更近）");
        CheckNear(packZ, BoosterPackOpenWindow.VisualItemZ, "实物 world z = `VisualItemZ`");
        CheckLt(cardsZ, packZ, "5 张卡那层在包实物**前**（更近 ⇒ 叠在上面）");
        CheckNear(cardsZ, BoosterPackOpenWindow.CardsFrontZ, "卡那层 world z = `CardsFrontZ`");

        // ---- 粒子队列：必须 ≥ `QCard`(3171) 才画在本窗透明背景(`QShade` 3170)之上（A5 的 LiftPackParticles）----
        var prs = _win.VisualItem.GetComponentsInChildren<ParticleSystemRenderer>(true);
        Check(prs.Length > 0, "（前提）卡包实物子树里有粒子（本段才可判队列）");
        if (prs.Length > 0)
        {
            bool allLifted = true;
            for (int i = 0; i < prs.Length; i++)
            {
                var r = prs[i];
                if (r == null || r.sharedMaterial == null) { allLifted = false; continue; }
                if (r.sharedMaterial.renderQueue < BoosterPackOpenWindow.QShade) allLifted = false;
            }
            Check(allLifted, "包面粒子的 renderQueue 全部 ≥ QShade(3170)（否则透明背景后画会盖掉它）");
        }
    }
}
