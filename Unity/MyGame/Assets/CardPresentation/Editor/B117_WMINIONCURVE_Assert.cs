// B117_WMINIONCURVE_Assert.cs — 执行代理 `B117-WMINIONCURVE`：**`minionMovementInfo` 那族曲线的关键帧斜率**
//
// ============================ 它盯的是什么 ============================
// `Resources/cardanim_clips.json` 里 `minionMovementInfo` 的三族曲线（共 9 列：
// `mmCx/mmCy/mmCz` · `mvcx/mvcy/mvcz` · `mmRxC/mmRyC/mmRzC`）**带没带 `inSlope/outSlope`**，
// 以及运行期 `BattleDriver.MinionCurve` 把这两套格式**读对没有**。
//
// 为什么：这一族曲线在原版是直接喂 `DOTween.SetEase(AnimationCurve)` /
// `curve.Evaluate(t01)` 的（`CardScript.MoveAndDisappearAnim`），而 Unity 的 `AnimationCurve.Evaluate`
// 是**分段三次 Hermite**（每键带 `inSlope/outSlope`）。2026-11-02 之前生成器每键只写 `[t,v]` ⇒
// ① 非线性曲线的形状（`SporeMine_Explosion` 的 rotateX「先反向拧 −43° 再甩到 45°」、
//    `DA Teleport *` 的 y 弧、`Scarab_Explosion` 的 x/z 弧）**整段被抹平**；
// ② 更隐蔽的一条：`MinionCurve` 旧实现对 2 数格式用 `new Keyframe(t, v)`，那个构造函数把
//    `inTangent/outTangent` **都置 0** ⇒ 求值出来是**零切线 Hermite（3u²−2u³ 平滑阶跃）**、
//    不是线性 —— 于是连「本来就是线性」的那几条也偏：`DA Teleport Recall` 的 `mmCy`（`[0,0,1,1]`）
//    在 `t = 0.3` 处得 **0.216**，原版是 **0.3**（位移中段少走 8%）。（本笔一并改掉。）
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · 数据格式：`资料/完全复刻/判据/B117-WCURVE_动画曲线斜率.md` §2.1（4 数格式 `[-1, t,v,in,out, …]`，
//     长度 `%4==1`；线性曲线仍是 2 数格式）；生成器 = `工具/gen_cardanim_clips.py` 的 `_curve_flat`。
//   · 原版语义：`d:/2/tools/decomp_full/CardScript__ApplyMovementDisappearAnimRotations.c:54`
//     （`Quaternion.LerpUnclamped(startRot, AngleAxis(maxValue, axis), curve.Evaluate(t01))`）
//     + `CardScript._MoveAndDisappearAnim_d__358.MoveNext`（三条位移 tween 各自 `SetEase(curve)`）。
//   · 源 clip（第一权威）：`assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json`
//     的 `animInfo.minionMovementInfo.*.m_Curve[*].inSlope/outSlope`（全表 `weightedMode = 0`）。
//   · 本笔报告 → `资料/完全复刻/判据/B117-WMINIONCURVE_佣兵移动曲线.md`
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主 `CardPresentation/Editor/BattleScene.cs` 的 `Run()` 尾部那条**聚合行**（`:46910` 一带，
//   与 `B117WCurveAssert.Run()` 同一行；**同一物理行追加 ⇒ 行数零变化**）：
//       B117WMinionCurveAssert.Run(); pass += B117WMinionCurveAssert.Pass; fail += B117WMinionCurveAssert.Fail;
//   ⚠️ 本件只读 `Resources` 表 + 反射调一个**纯静态私有方法**，不建 GameObject、不要 driver、
//      不动盘 ⇒ 接在方法尾（`driver` 被销毁之后）也安全。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · M2b ← 4 数格式的 `in/out` 顺序写反（`Keyframe(t,v,out,in)`）：合成曲线 `[-1,0,0,0,4, 1,1,0,0]`
//           在 0.25 处从 **0.71875** 掉到 **0.15625** ⇒ 红（本表里 `inSlope == outSlope` 成对出现，
//           ⛔ 真实数据分不出顺序，所以这条**专门**造一条 in ≠ out 的）。
//   · M3a ← 生成器不给这一族写斜率（退回 2 数格式）：`SporeMine_Explosion` 的 rotateX 在 0.5 处
//           从 **−0.7569** 掉回 **−0.4168**（M3b 的判别式直接量化了这 0.34 的差） ⇒ 红。
//   · M4a ← 把 2 数格式改回 `new Keyframe(t, v)`（零切线）：0.3 处从 **0.3** 掉回 **0.216** ⇒ 红。
//   · M1a/M1b ← 格式判据（长度 `%4==1` + 首格 `-1`）坏掉 ⇒ 前提条直接红。
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B117-WMINIONCURVE`：`minionMovementInfo` 曲线带斜率 + `MinionCurve` 两套格式都读对。</summary>
public static class B117WMinionCurveAssert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    const string Tag = "B117WMINIONCURVE";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F6") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F6") + "）");
    }

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        SectionM1_数据格式();
        SectionM2_4数格式的求值();
        SectionM3_真实数据();
        SectionM4_旧2数格式();
        Debug.Log(Tag + " —— 合计 " + (Pass - pass0) + " 通过 / " + (Fail - fail0) + " 失败");
    }

    // ============================================================ 产线入口（反射）
    static MethodInfo _minionCurve;
    static bool _probed;

    /// <summary>真产线：`BattleDriver.MinionCurve(float[])` 是 `private static` ⇒ 反射取一次。
    /// ⚠️ 用产线那个方法（而不是在这里重搭一遍 `Keyframe`）就是为了**不断言自己**。</summary>
    static AnimationCurve MinionCurveOf(float[] flat)
    {
        if (!_probed)
        {
            _probed = true;
            _minionCurve = typeof(BattleDriver).GetMethod(
                "MinionCurve", BindingFlags.NonPublic | BindingFlags.Static);
        }
        if (_minionCurve == null) return null;
        return (AnimationCurve)_minionCurve.Invoke(null, new object[] { flat });
    }

    /// <summary>4 数格式的结构不变量：长度 `1+4n` · 首格 `-1` · `t` 严格递增（n ≥ 2）。</summary>
    static bool OkSloped(float[] a)
    {
        if (a == null || a.Length < 9 || (a.Length % 4) != 1 || a[0] != -1f) return false;
        float last = -1f;
        for (int o = 1; o + 3 < a.Length; o += 4)
        {
            if (o > 1 && a[o] <= last) return false;
            last = a[o];
        }
        return true;
    }

    /// <summary>任一曲线数组都必须是「旧 2 数（偶数长度）」或「4 数（首格 −1、长度 %4==1）」之一 ——
    /// 长度落在别的值上说明格式坏了（运行期会**静默**按 2 数读错）。</summary>
    static bool OkAnyFormat(float[] a)
    {
        if (a == null || a.Length == 0) return true;
        if ((a.Length % 4) == 1) return OkSloped(a);
        return (a.Length % 2) == 0;
    }

    // ============================================================ M1：数据格式
    static void SectionM1_数据格式()
    {
        var sp = CardAnimClips.PlacementOf("SporeMine_Explosion");
        bool spOk = sp.HasValue;
        Check(spOk, "M1a（前提）`SporeMine_Explosion` 在 `cardanim_clips` 表里"
                  + "｜🧨 表没载/行被删 ⇒ 本段其余各条一并失效");
        if (!spOk) return;
        var p = sp.Value;

        Check(p.RotXCurve != null && CardAnimOrder.HasSlopes(p.RotXCurve),
              "M1b `SporeMine_Explosion.rotateXAxis` 的曲线是 **4 数格式**（原版三键：0/−0.952/1，"
            + "切线各自 −4.01 / −1.47 / +2.94）｜🧨 生成器不写斜率 ⇒ 退回 2 数格式（长度 6）⇒ 红");
        Check(p.RotXCurve != null && p.RotXCurve.Length == 13 && OkSloped(p.RotXCurve),
              "M1c 同一条曲线 = 3 键 ⇒ 长度 **13**（`1+4×3`）、首格 −1、`t` 严格递增"
            + "（现读 " + (p.RotXCurve == null ? "null" : p.RotXCurve.Length.ToString()) + " 格）");

        Check(p.MoveCurveY != null && CardAnimOrder.HasSlopes(p.MoveCurveY) && p.MoveCurveY.Length == 21,
              "M1d `SporeMine_Explosion.minionMovementAnimCurveY` = 5 键 ⇒ **4 数格式 / 21 格**"
              + "（原版是一条冲到 2.04 再回落到 1.0 的弧 —— 2 数格式只剩折线）");
        Check(p.MoveCurveX != null && !CardAnimOrder.HasSlopes(p.MoveCurveX) && p.MoveCurveX.Length == 4,
              "M1e `SporeMine_Explosion.minionMovementAnimCurveX` 是**线性**曲线（(0,0)→(1,1)、切线 = 弦）"
              + "⇒ 仍是**旧 2 数格式 / 4 格**（逐字节不变）｜🧨 把线性曲线也写成 4 数 ⇒ 红");

        var da = CardAnimClips.PlacementOf("DA Teleport Recall");
        Check(da.HasValue && da.Value.RotYCurve != null && CardAnimOrder.HasSlopes(da.Value.RotYCurve),
              "M1f `DA Teleport Recall.rotateYAxis`（三键 0 → 0.9941 → 0）是 4 数格式 —— "
            + "原版这条是「转出去再转回来」的弧，线性读会把中段抹平");
        Check(da.HasValue && da.Value.MoveCurveY != null && !CardAnimOrder.HasSlopes(da.Value.MoveCurveY)
              && da.Value.MoveCurveY.Length == 4
              && Mathf.Abs(da.Value.MoveCurveY[1]) < 1e-6f && Mathf.Abs(da.Value.MoveCurveY[3] - 1f) < 1e-6f,
              "M1g `DA Teleport Recall.minionMovementAnimCurveY` = `[0,0,1,1]`、仍是 2 数格式"
              + "（线性：两端切线 = 弦斜率 1）");

        var sc = CardAnimClips.PlacementOf("Scarab_Explosion");
        Check(sc.HasValue && sc.Value.MoveCurveX != null && CardAnimOrder.HasSlopes(sc.Value.MoveCurveX)
              && OkSloped(sc.Value.MoveCurveX),
              "M1h `Scarab_Explosion` 的 x/z 位移弧（4 键）是 4 数格式（17 格）");
        Check(sc.HasValue && sc.Value.RotYCurve != null && sc.Value.RotYCurve.Length == 2
              && !CardAnimOrder.HasSlopes(sc.Value.RotYCurve),
              "M1i `Scarab_Explosion.rotateYAxis` 只有 **1 键** ⇒ 仍是 2 数格式（2 格）"
              + "（单键没有「段」，谈不上斜率；运行期 `MinionCurve` 会因 < 2 键返回 null）");

        // 普查：表里所有能看到的 minion 曲线列，格式必须落在两套之一
        int bad = 0, sloped = 0, legacy = 0;
        string[] names = { "SporeMine_Explosion", "Scarab_Explosion", "DA Teleport Recall",
                           "DA Teleport Summon Next", "DA Teleport Trigger Board Only",
                           "DA Teleport Trigger Effect", "Generic Recall", "Summon Generic",
                           "TeleportCardAnim", "CleansingFlamesEffect" };
        for (int i = 0; i < names.Length; i++)
        {
            var q = CardAnimClips.PlacementOf(names[i]);
            if (!q.HasValue) continue;
            float[][] cols = { q.Value.MoveCurveX, q.Value.MoveCurveY, q.Value.MoveCurveZ,
                               q.Value.RotXCurve, q.Value.RotYCurve, q.Value.RotZCurve };
            for (int c = 0; c < cols.Length; c++)
            {
                float[] a = cols[c];
                if (a == null || a.Length == 0) continue;
                if (!OkAnyFormat(a)) bad++;
                if (CardAnimOrder.HasSlopes(a)) sloped++; else legacy++;
            }
        }
        Check(bad == 0, "M1j 普查（10 行 × 6 列）：所有 minion 曲线数组的格式都合法（坏 " + bad + " 条）");
        Check(sloped == 13 && legacy == 15,
              "M1k 普查规模（这 10 行 × 表里能取到的 6 列）：**带斜率 13 列 / 旧 2 数格式 15 列**"
            + "（现读 " + sloped + " / " + legacy + "）"
            + "｜🧨 生成器把斜率全去掉 ⇒ 13→0 ⇒ 红；把线性曲线也写 4 数 ⇒ 15 变小 ⇒ 红"
            + "（⚠️ 全表 9 列的口径是 17 / 23 —— 另 3 列 `mmCx/mmCy/mmCz` 不经 `CardAnimPlacement` 暴露）");
    }

    // ============================================================ M2：4 数格式的求值（合成）
    static void SectionM2_4数格式的求值()
    {
        MinionCurveOf(null);                       // 触发一次反射探测
        Check(_minionCurve != null,
              "M2a0（前提）反射找到了 `BattleDriver.MinionCurve(float[])`（`private static`）"
            + "｜🧨 方法被改名/删掉 ⇒ 本段全部失去被测对象");
        Check(MinionCurveOf(null) == null && MinionCurveOf(new float[] { 0f, 0f }) == null,
              "M2a（前提）`MinionCurve` 反射调用得通；`null` / 不足 2 键 ⇒ `null`（= 那一轴不 SetEase）");

        // 合成曲线：in ≠ out，专门用来判「哪一格进 Keyframe 的 inTangent」
        //   keys: (0,0, in=0, out=4) → (1,1, in=0, out=0)
        //   u = 0.25 ⇒ h10 = u³−2u²+u = 0.140625 ⇒ 0.140625 × dt(1) × out0(4) = 0.5625
        //              h01 = −2u³+3u²   = 0.15625  ⇒ 合计 **0.71875**
        //   🧨 in/out 写反 ⇒ out0 = 0 ⇒ 0.15625（差 0.5625）
        var swap = new float[] { -1f, 0f, 0f, 0f, 4f, 1f, 1f, 0f, 0f };
        var curveSwap = MinionCurveOf(swap);
        Check(curveSwap != null, "M2b0（前提）4 数格式数组能建成 `AnimationCurve`（反射返回非 null）");
        if (curveSwap != null)
            Near(curveSwap.Evaluate(0.25f), 0.71875f, 1e-4f,
                 "M2b ★ 4 数格式把 `[t,v,in,out]` 的第 3 格当 `inTangent`、第 4 格当 `outTangent`："
               + "合成曲线在 0.25 处 = **0.71875**｜🧨 两个切线写反 ⇒ 0.15625 ⇒ 红"
               + "（真实数据里 `inSlope == outSlope` 成对出现，分不出顺序 —— 所以必须用这条合成的）");

        // 与 `CardAnimOrder.HermiteAt`（另一份独立实现）逐点对齐 —— 两边同一条公式、两种写法
        float maxd = 0f;
        for (int i = 0; i <= 400; i++)
        {
            float x = i / 400f;
            float a = curveSwap.Evaluate(x);
            float b = CardAnimOrder.EaseAtRaw(swap, x);
            maxd = Mathf.Max(maxd, Mathf.Abs(a - b));
        }
        Check(maxd < 1e-4f,
              "M2c 同一条 4 数曲线：`AnimationCurve.Evaluate`（产线）与 `CardAnimOrder.EaseAtRaw`"
            + "（本工程的 Hermite）在 401 个点上逐值一致（最大差 " + maxd.ToString("E5") + " < 1e-4）");
    }

    // ============================================================ M3：真实数据（源 clip 烘出来的期望值）
    static void SectionM3_真实数据()
    {
        var sp = CardAnimClips.PlacementOf("SporeMine_Explosion");
        Check(sp.HasValue && sp.Value.RotXCurve != null,
              "M3a0（前提）`SporeMine_Explosion.rotateXAxis` 曲线在表里");
        if (sp.HasValue && sp.Value.RotXCurve != null && sp.Value.RotXCurve.Length == 13)
        {
            var c = MinionCurveOf(sp.Value.RotXCurve);
            Check(c != null, "M3a0′（前提）能建成 `AnimationCurve`");
            if (c != null)
            {
                // 真值（源 clip 全精度 → Hermite）= −0.756829；数据 4 位截断后 = −0.756863
                Near(c.Evaluate(0.5f), -0.75686f, 1e-3f,
                     "M3a ★ `SporeMine_Explosion.rotateX` 在 0.5 处 = **−0.7569**"
                   + "（原版三键 0 / −0.952 / 1 的 Hermite 插值；线性读会给 **−0.2797**、零切线给 **−0.4168**）");

                // 判别式：同一条数据的**零切线**版本（= 旧 `new Keyframe(t,v)` 的求值）必须明显不同，
                // 否则上面那条断言没有鉴别力。
                float[] z = new float[sp.Value.RotXCurve.Length];
                System.Array.Copy(sp.Value.RotXCurve, z, z.Length);
                var kf = new Keyframe[3];
                for (int i = 0; i < 3; i++)
                    kf[i] = new Keyframe(z[1 + i * 4], z[2 + i * 4]);       // 故意丢掉 in/out
                float zeroTan = new AnimationCurve(kf).Evaluate(0.5f);
                Check(Mathf.Abs(zeroTan - (-0.75686f)) > 0.3f,
                      "M3b ★ 判别式：丢掉切线会得 **" + zeroTan.ToString("F4") + "**，与上条期望差 "
                    + Mathf.Abs(zeroTan + 0.75686f).ToString("F3") + "（> 0.3）⇒ 斜率真在起作用"
                    + "｜🧨 若这条差变小，说明上一条 M3a 抓不住「切线丢了」");
            }
        }

        var da = CardAnimClips.PlacementOf("DA Teleport Recall");
        if (da.HasValue && da.Value.RotYCurve != null)
        {
            var c = MinionCurveOf(da.Value.RotYCurve);
            if (c != null)
                Near(c.Evaluate(0.25f), 0.63940f, 1e-3f,
                     "M3c ★ `DA Teleport Recall.rotateY` 在 0.25 处 = **0.6394**"
                   + "（原版 (0,0)→(0.4854,0.9941)→(1,0)、out0 = 2.0479；线性读给 0.5120、"
                   + "零切线给 0.5195）");
        }

        if (sp.HasValue && sp.Value.MoveCurveY != null)
        {
            var c = MinionCurveOf(sp.Value.MoveCurveY);
            if (c != null)
                Near(c.Evaluate(0.30f), 1.55530f, 1e-3f,
                     "M3d ★ `SporeMine_Explosion` 的 y 位移（5 键弧，冲到 2.04 再回落）在 0.30 处 = **1.5553**"
                   + "｜🧨 线性读给 **1.4893**、零切线给 **1.6718** ⇒ 三档互不相同，是这一族最灵敏的一格");
        }
    }

    // ============================================================ M4：旧的 2 数格式 —— 现在是「弦切线」= 分段线性
    static void SectionM4_旧2数格式()
    {
        var da = CardAnimClips.PlacementOf("DA Teleport Recall");
        Check(da.HasValue && da.Value.MoveCurveY != null && !CardAnimOrder.HasSlopes(da.Value.MoveCurveY),
              "M4a0（前提）`DA Teleport Recall.minionMovementAnimCurveY` 仍是旧 2 数格式（线性曲线）");
        if (da.HasValue && da.Value.MoveCurveY != null)
        {
            var c = MinionCurveOf(da.Value.MoveCurveY);
            if (c != null)
            {
                Near(c.Evaluate(0.3f), 0.3f, 1e-4f,
                     "M4a ★ 线性曲线 `[0,0,1,1]` 在 0.3 处 = **0.3**"
                   + "｜🧨 改回 `new Keyframe(t, v)`（零切线）⇒ **0.216** ⇒ 红（那是 3u²−2u³ 的平滑阶跃，"
                   + "不是原版这条曲线的形状；原版两端切线 = 弦斜率 1 ⇒ 逐值等于线性）");
                Near(c.Evaluate(0.5f), 0.5f, 1e-5f,
                     "M4b 同一条线性曲线中点 = 0.5（对称点 —— ⛔ 它分不出线性与平滑阶跃，只作对照）");
            }
        }

        // 常数曲线（`CleansingFlamesEffect` 的 y/z 是 (0,1)→(1,1)）⇒ 任何进度都 1
        var cf = CardAnimClips.PlacementOf("CleansingFlamesEffect");
        Check(cf.HasValue && cf.Value.MoveCurveZ != null && !CardAnimOrder.HasSlopes(cf.Value.MoveCurveZ),
              "M4c（前提）`CleansingFlamesEffect.minionMovementAnimCurveZ` 是常数曲线 ⇒ 仍 2 数格式");
        if (cf.HasValue && cf.Value.MoveCurveZ != null)
        {
            var c = MinionCurveOf(cf.Value.MoveCurveZ);
            if (c != null)
                Near(c.Evaluate(0.3f), 1f, 1e-5f,
                     "M4d 常数曲线 `[0,1,1,1]` 任何进度都 = **1**（零切线/弦切线都给 1 —— 这条只保不回归）");
        }
    }
}
