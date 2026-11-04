// B140_WO_Assert.cs — 执行代理 `B140_WO`（`Core/CardView.cs` 域：卡名字色 + 两条缓动）的断言
//
// ============================ 它盯的是什么 ============================
// 本包 3 条（判据来源 = `报告/B131_审计_色彩材质与渲染.md` 的 `B1` / §五·1 · `报告/B135_审计_动画曲线与状态机.md` top-5 #2 / #5）：
//   · A 卡名字色 = 原版 `m_fontColor` = **`#FD9A58`** = (253,154,88)（改前是 `#FDDDBB` = (253,221,187)）
//   · B 「会打死」预览图标那两条补间的缓动 = **smoothstep**（改前 `Ease.Linear`）
//   · C 卡体高亮放大 1.05 的缓动 = **OutQuad**（改前 `Ease.Linear`）
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · A：`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_-6860425428091954240.json`
//        `m_fontColor = (0.992157, 0.603922, 0.345098)`；材质 `Asar-Regular White-Card name` 的
//        `_FaceColor = (1,1,1,1)` ⇒ **屏上色 = 顶点色本身**（menus 副本 `MonoBehaviour_-722422701675818756.json` 同值）。
//        ⚠️ 我们原来那条注释引的 `Pragati-Regular Light Cream` **不是** `NameText*` 挂的那颗（6/6 都不挂）。
//   · B：原版 `Minion Death Icon` clip 的两条曲线（pos y 1.7100→2.8380 · α 0→1 @[0, 0.25]）**两端切线全 0**
//        ⇒ Hermite **精确等于** smoothstep `3u²−2u³`（不是线性）。实现 = `CardFeel.ClipSmoothStep`
//        （那条「两端切线为 0 的 `AnimationCurve`」，判据只留那一处）。
//   · C：`CardHighlight__ChangeTargetScale.c:36` 的 `DOScale(target, 0.1, snapping:0)` **不写 `SetEase`**
//        ⇒ 吃 `DOTween.defaultEaseType` = **6 OutQuad**（`Resources/DOTweenSettings.asset:31`）。
//   · 枚举值的来源：`/d:/2/tools/decomp_tween/out/DG.Tweening.TweenSettingsExtensions.SetEase_object___dc62e0.c`
//        `SetEase(AnimationCurve/EaseFunction)` 写 `*(undefined4 *)(param_1 + 0xb4) = 0x25`（= 37 `INTERNAL_Custom`）；
//        而 `SetEase(Ease,…)` 那一档把 `+0xb8`（customEase）清零。⇒ 「按枚举值断」= 读补间实例上那个 `Ease` 字段。
//
// ============================ 怎么用 ============================
//   `B140_WO_Assert.Run();`   （静态类 + 无参 `Run()`；全局命名空间、类名 == 文件名）
//   ⚠️ 本文件**不建持久资产**：临时 GameObject 在 `finally` 里 `DestroyImmediate`，
//      并把 `CardTween.Mode` 原样还原（否则会把后面所有宿主的补间一起冻住）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A  ← 把 `CardView.InkName` 改回 `(253,221,187)` ⇒ G 差 67、B 差 99 ⇒ A★/A4 各红一条。
//   · B  ← 把那条补间的缓动写回 `Ease.Linear` ⇒ 枚举 = 1（不是 37）⇒ B★ 红；
//           **同时** u=0.25 的 α 会变成 0.25（不是 0.15625）⇒ B5/B6 再红 —— 两处一起改也过不了。
//   · C  ← 写回 `Ease.Linear` ⇒ 枚举 = 1 ⇒ C★ 红；u=0.5 的 `HighlightMul` 会是 1.025（不是 1.0375）⇒ C5/C6 再红。
using DG.Tweening;
using TMPro;
using UnityEngine;
using CardPresentation;

public static class B140_WO_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B140WO";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + $"\n      （实得 {got:F6} · 期望 {want:F6} · 容差 {tol:G3}）");
    }

    public static void Run()
    {
        _pass = _fail = 0;

        // 前提硬检查（⛔ 不静默跳过 —— 字体不可用时卡名层根本不建，A 组会拿 null 假绿）
        Check(TmpFont.Available, "P0 **前提**：TMP 字体资产可用（`TmpFont.Available`）");
        if (!TmpFont.Available)
        {
            Debug.LogError(Tag + " ===== 前提不成立 ⇒ 本宿主什么都没验到（不是通过）=====");
            return;
        }

        var mode0 = CardTween.Mode;
        var root = new GameObject("B140WO_ROOT");
        CardTween.Mode = UpdateType.Manual;
        try
        {
            A_CardNameInk(root.transform);
            B_WillDieEase(root.transform);
            C_HighlightEase(root.transform);
        }
        finally
        {
            CardTween.Mode = mode0;
            Object.DestroyImmediate(root);
        }

        Debug.Log(Tag + " ===== 合计 通过 " + _pass + " / 失败 " + _fail + " =====");
    }

    // ---------------------------------------------------------------- A · 卡名字色
    static void A_CardNameInk(Transform parent)
    {
        GameObject go = null;
        try
        {
            go = new GameObject("B140WO_A");
            go.transform.SetParent(parent, false);
            var card = CardView.Create(go.transform, CardData.Simple("B140WO", 1, 1, 1), "B140WO_card");
            Check(card != null, "A0 **前提**：夹具卡建出来了（`CardView.Create`）");
            if (card == null) return;

            TextMeshPro title = null;
            foreach (var t in card.GetComponentsInChildren<TextMeshPro>(true))
                if (t.name == "title") { title = t; break; }
            Check(title != null, "A1 **前提**：卡名层 `title` 建出来了（`Fill(_title, \"title\", …)`）");
            if (title == null) return;

            var m = title.fontMaterial;
            bool hasFace = m != null && m.HasProperty(ShaderUtilities.ID_FaceColor);
            Check(hasFace, "A2 **前提**：卡名那层拿到了字体材质、且有 `_FaceColor`");
            if (!hasFace) return;

            // 前提：材质面是**白** ⇒ 屏上色 = 顶点色本身（原版那颗 `Asar-Regular White-Card name` 的 `_FaceColor` 也是白）
            var fc = m.GetColor(ShaderUtilities.ID_FaceColor);
            Check(Mathf.Abs(fc.r - 1f) < 2e-2f && Mathf.Abs(fc.g - 1f) < 2e-2f && Mathf.Abs(fc.b - 1f) < 2e-2f,
                  "A3 **前提**：卡名那层材质 `_FaceColor` = 白（实得 " + fc.ToString("F3") + "）"
                + " ⇒ 屏上色 = 顶点色；判据 = `Asar-Regular White-Card name` 的 `_FaceColor = (1,1,1,1)`");

            var c = title.color;
            Near(c.r * 255f, 253f, 1f,
                 "A★ 卡名 R = **253**（原版 `m_fontColor = (0.992157,0.603922,0.345098)` = `#FD9A58`）"
               + "｜🧨 改回 `#FDDDBB` 那一版 ⇒ 三通道都不等 ⇒ 红");
            Near(c.g * 255f, 154f, 1f, "A4 卡名 G = **154**（`#FD9A58`；旧值 `#FDDDBB` 是 221）");
            Near(c.b * 255f, 88f, 1f, "A5 卡名 B = **88**（`#FD9A58`；旧值 `#FDDDBB` 是 187）");
            Check(Mathf.Abs(c.g * 255f - 221f) > 3f && Mathf.Abs(c.b * 255f - 187f) > 3f,
                  "A6（灭自证）卡名**不是**旧值 `#FDDDBB`（G 221 / B 187）");
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ---------------------------------------------------------------- B · 预览图标缓动
    static void B_WillDieEase(Transform parent)
    {
        GameObject go = null;
        try
        {
            go = new GameObject("B140WO_B");
            go.transform.SetParent(parent, false);
            var card = CardView.Create(go.transform, CardData.Simple("B140WO", 1, 1, 1), "B140WO_card");
            Check(card != null, "B0 **前提**：夹具卡建出来了");
            if (card == null) return;

            card.SetWillDie(true);
            Check(card.WillDieTexName != null,
                  "B1 **前提**：`Minion Death Icon` 那层建出来了（贴图 = " + (card.WillDieTexName ?? "<null>") + "）"
                + "｜图取不到时 `BuildWillDie` 会早退 ⇒ 下面几条全部等于没跑");
            if (card.WillDieTexName == null) return;
            Check(card.WillDieVisible, "B2 **前提**：那层现在是亮的（`SetWillDie(true)` 生效）");

            var tw = TweenFieldOf(card, "_willDieTween");
            Check(tw != null, "B3 **前提**：读到了那条补间（私有字段 `_willDieTween`）");

            Ease got = Ease.Unset;
            bool found = tw != null && TryEaseOf(tw, out got);
            Check(found, "B4 **前提**：读得到补间实例上的 `Ease` 字段（DOTween 没公开 getter ⇒ 反射）");
            Check(got != Ease.Linear,
                  "B★ 预览图标缓动 **不是 Linear**（实得 " + (int)got + "）"
                + "｜原版两条曲线两端切线全 0 ⇒ smoothstep，⛔ 不是匀速"
                + "｜🧨 写回 `Ease.Linear`(=1) ⇒ 红");
            Check(got == Ease.INTERNAL_Custom,
                  "B5 缓动 = `Ease.INTERNAL_Custom`(37)（`SetEase(AnimationCurve)` 的固定档；实得 " + (int)got + "）"
                + "｜判据 = `DG.Tweening.TweenSettingsExtensions.SetEase_object___dc62e0.c` 写 `+0xb4 = 0x25`");

            float a0 = card.WillDieAlpha;
            Check(a0 < 1e-3f, "B6（前提）起播那一刻 α = 0（实得 " + a0.ToString("F4") + "）");

            // u = 0.25（0.0625 s / 0.25 s）—— smoothstep = 0.15625，线性会是 0.25
            CardTween.Advance(0.0625f);
            float a25 = card.WillDieAlpha;
            Vector3 p25 = card.WillDieLocalPos;
            Near(a25, 0.15625f, 0.02f,
                 "B7 ★ u=0.25 处 α = smoothstep(0.25) = **0.15625**（`3u²−2u³`）"
               + "｜🧨 线性 ⇒ 0.25 ⇒ 红");
            Check(Mathf.Abs(a25 - 0.25f) > 0.03f,
                  "B8（灭自证）u=0.25 处 α **不是**线性档 0.25（实得 " + a25.ToString("F4") + "）");

            float yFrom = CardView.WillDieFromForCheck.y;
            float ySpan = CardView.WillDieToForCheck.y - yFrom;
            float r = Mathf.Abs(ySpan) > 1e-6f ? (p25.y - yFrom) / ySpan : 0f;
            Near(r, 0.15625f, 0.02f,
                 "B9 ★ 同一时刻的 y 位移比也按 smoothstep 走（= 0.15625）"
               + "｜原版 `m_PositionCurves` 的 y 1.7100→2.8380 是**两条曲线之一**，⛔ 别只改 α 那一半");

            CardTween.Advance(0.25f);
            Near(card.WillDieAlpha, 1f, 1e-3f, "B10 收尾 α = 1（0.25 s 那条走得完）");
            Near(card.WillDieLocalPos.y, CardView.WillDieToForCheck.y, Mathf.Abs(ySpan) * 0.01f + 1e-4f,
                 "B11 收尾 y = 原版终点 2.8380（3DBody 换算后）");

            if (tw != null) tw.Kill(false);
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ---------------------------------------------------------------- C · 高亮放大缓动
    static void C_HighlightEase(Transform parent)
    {
        GameObject go = null;
        try
        {
            go = new GameObject("B140WO_C");
            go.transform.SetParent(parent, false);
            var card = CardView.Create(go.transform, CardData.Simple("B140WO", 1, 1, 1), "B140WO_card");
            Check(card != null, "C0 **前提**：夹具卡建出来了");
            if (card == null) return;

            Near(card.HighlightMul, 1f, 1e-4f, "C1（前提）起始高亮系数 = 1");
            card.SetHighlightScale(true);

            var tw = TweenFieldOf(card, "_hlTween");
            Check(tw != null, "C2 **前提**：读到了那条补间（私有字段 `_hlTween`）");

            Ease got = Ease.Unset;
            bool found = tw != null && TryEaseOf(tw, out got);
            Check(found, "C3 **前提**：读得到补间实例上的 `Ease` 字段");
            Check(got == Ease.OutQuad,
                  "C★ 高亮放大的缓动 = **OutQuad**(6)（原版 `DOScale` 不写 `SetEase` ⇒ `defaultEaseType` = 6；实得 "
                + (int)got + "）"
                + "｜🧨 写回 `Ease.Linear`(=1) ⇒ 红");

            // u = 0.5（0.05 s / 0.1 s）—— OutQuad(0.5) = 0.75 ⇒ 1 + 0.05×0.75 = 1.0375
            CardTween.Advance(CardHighlight.AnimTime * 0.5f);
            Near(card.HighlightMul, 1.0375f, 0.004f,
                 "C4 ★ u=0.5 处高亮系数 = **1.0375**（`1 + 0.05 × OutQuad(0.5) = 1 + 0.05×0.75`）"
               + "｜🧨 线性 ⇒ 1.025 ⇒ 红");
            Check(Mathf.Abs(card.HighlightMul - 1.025f) > 0.005f,
                  "C5（灭自证）u=0.5 处 **不是**线性档 1.025（实得 " + card.HighlightMul.ToString("F5") + "）");

            CardTween.Advance(CardHighlight.AnimTime);
            Near(card.HighlightMul, CardHighlight.ScaleFactor, 1e-3f,
                 "C6 收尾 = 原版 `ScaleFactor` 1.05");

            if (tw != null) tw.Kill(false);
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------------ 工具
    /// <summary>读 `CardView` 上那条私有补间字段（`_willDieTween` / `_hlTween`）—— 只读，不建新口。</summary>
    static Tween TweenFieldOf(object inst, string name)
    {
        if (inst == null) return null;
        var f = inst.GetType().GetField(name, System.Reflection.BindingFlags.Instance
                                             | System.Reflection.BindingFlags.NonPublic);
        return f != null ? f.GetValue(inst) as Tween : null;
    }

    /// <summary>把补间实例上那个 `Ease` 字段读出来（DOTween 没公开 getter ⇒ 反射找「类型是 `Ease` 的实例字段」）。
    /// 找不到 ⇒ 返回 false（调用方要**出声**，⛔ 不许当通过）。同 `B139_E_Assert.TryEaseOf`。</summary>
    static bool TryEaseOf(Tween t, out Ease e)
    {
        e = Ease.Linear;
        if (t == null) return false;
        var ty = t.GetType();
        while (ty != null && ty != typeof(object))
        {
            var fs = ty.GetFields(System.Reflection.BindingFlags.Instance
                                 | System.Reflection.BindingFlags.Public
                                 | System.Reflection.BindingFlags.NonPublic
                                 | System.Reflection.BindingFlags.DeclaredOnly);
            for (int i = 0; i < fs.Length; i++)
            {
                if (fs[i].FieldType == typeof(Ease))
                {
                    e = (Ease)fs[i].GetValue(t);
                    return true;
                }
            }
            ty = ty.BaseType;
        }
        return false;
    }
}
