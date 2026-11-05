// B145_E8_Assert.cs — `B145-E8`：A-25（`🔶 自己设计` 标记）+ A-26（粒子渲染器三字段）的钉死断言
//
// ============================ 盯的是什么 ============================
//  A-25：`ArenaBuilder.ArenaUseOriginalShaders` 上方那段 doc 注释
//        = 「本体网格 shader 一律 URP/Unlit」这个**自设计**的声明处；
//        必须带 `🔶 自己设计（非原版）`（铁律 18 三样：是什么 / 为什么必须自定 / 谁定的+日期），
//        且原来那两段重复的 `<summary>` 必须合并掉。
//        判据 → `资料/完全复刻/报告/B144_D_战场环境.md` §2（§一·M1）；`B145_施工单汇总_离线.md` A-25。
//  A-26：`ParticleEntry` 的三个字段（`renderingLayerMask`/`lightProbeUsage`/`sortingOrder`）
//        + `BuildContent` 里把值写到 `ParticleSystemRenderer` 上的那一段。
//        判据 → `资料/完全复刻/报告/B144_R1_网格结构.md` §8·6（原版 battlearena1 的 34 条清单粒子实读：
//        `renderingLayerMask` All×28 / 1×6 · `lightProbeUsage` 1×14 · `sortingOrder` 1×2）。
//
// ============================ 🔴 灭自证 ============================
//  · A2：把 `renderingLayerMask` 改回 `int` ⇒ 红（Type 不等）。
//  · B2：`4294967295` 按 int 读是 −1 —— 这条不是同义反复，它证明「int 装不下」这个**改法的反例**。
//  · C5 / C3：把 `ApplyRendererParams` / `ApplyParticleRendererParams` 里任一写行删掉 ⇒ 读回值不达期望 ⇒ 红。
//  · D7：`WF_ORIGSHADER=1` 必须还能打开开关 —— 防止有人「只删注释顺手把行为也改了」。
//  ⛔ 期望值全部来自**原版 PSR JSON 的逐颗读数**与**方法名的语义**，不是拿被测代码自己的常量证明自己。
//
// ============================ 怎么接 ============================
//  `B145_E8_Assert.Run(); _sink.Pass += ...; _sink.Fail += ...;`（宿主由调度台统一接）
//  只读工程文件 + 在编辑期建两个临时 GameObject（跑完 `DestroyImmediate`）；不建场景/资产。
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public static class B145_E8_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B145E8]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Root { get { return Directory.GetParent(Application.dataPath).FullName; } }

    static string Read(string rel)
    {
        string p = Path.Combine(Root, rel);
        if (!File.Exists(p)) { Debug.LogError(Tag + " 读不到：" + p); return null; }
        return File.ReadAllText(p);
    }

    static readonly Type PE = typeof(ArenaBuilder.ParticleEntry);

    static FieldInfo Field(Type t, string n)
    {
        return t == null ? null : t.GetField(n, BindingFlags.Public | BindingFlags.Instance);
    }

    static MethodInfo StaticMethod(string name, Type[] args)
    {
        return typeof(ArenaBuilder).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static,
                                              null, args, null);
    }

    public static void Run()
    {
        _pass = _fail = 0;

        // ============================================================
        // A · DTO（`ParticleEntry`）—— 判据 B144_R1 §8·6 ①
        // ============================================================
        FieldInfo fMask = Field(PE, "renderingLayerMask");
        FieldInfo fLp = Field(PE, "lightProbeUsage");
        FieldInfo fSo = Field(PE, "sortingOrder");

        Check(fMask != null, "A1 `ParticleEntry.renderingLayerMask` 存在｜🧨 删字段 ⇒ 红");
        Check(fMask != null && fMask.FieldType == typeof(uint),
              "A2 `renderingLayerMask` 类型必须是 `uint`（实得 "
            + (fMask == null ? "<缺字段>" : fMask.FieldType.Name)
            + "）｜🧨 改成 int ⇒ 红（All = 4294967295 装不下）");
        Check(fLp != null && fLp.FieldType == typeof(int),
              "A3 `lightProbeUsage` 存在且为 `int`（实得 "
            + (fLp == null ? "<缺字段>" : fLp.FieldType.Name) + "）");
        Check(fSo != null && fSo.FieldType == typeof(int),
              "A4 `sortingOrder` 存在且为 `int`（实得 "
            + (fSo == null ? "<缺字段>" : fSo.FieldType.Name) + "）");

        var peDefault = new ArenaBuilder.ParticleEntry();
        Check(peDefault.renderingLayerMask == 1u,
              "A5 缺键默认 `renderingLayerMask = 1`（= Unity 新建 PSR 默认）实得 " + peDefault.renderingLayerMask);
        Check(peDefault.lightProbeUsage == 0 && peDefault.sortingOrder == 0,
              "A6 缺键默认 `lightProbeUsage/sortingOrder = 0/0`（实得 "
            + peDefault.lightProbeUsage + "/" + peDefault.sortingOrder + "）");

        // ============================================================
        // B · uint 装得下 4294967295 + Unity API 收得住
        // ============================================================
        var peAll = new ArenaBuilder.ParticleEntry { renderingLayerMask = 4294967295u };
        Check(peAll.renderingLayerMask == uint.MaxValue,
              "B1 `4294967295` 能装进字段（实得 " + peAll.renderingLayerMask + "）");
        Check(unchecked((int)peAll.renderingLayerMask) == -1,
              "B2 同一个值按 int 读是 −1 ⇒ 证明 `int` 装不下"
            + "｜🧨 把字段改成 int ⇒ A2 红、这条也红（反例，不是同义反复）");

        GameObject psGo = new GameObject("B145E8_PSR");
        psGo.AddComponent<ParticleSystem>();
        var psr = psGo.GetComponent<ParticleSystemRenderer>();
        Check(psr != null, "B3 前提：`ParticleSystem` 上取得到 `ParticleSystemRenderer`");
        bool b4 = false;
        try
        {
            psr.renderingLayerMask = 4294967295u;
            b4 = psr.renderingLayerMask == 4294967295u;
        }
        catch (Exception ex) { Debug.LogError(Tag + " B4 异常：" + ex.Message); }
        Check(b4, "B4 Unity API 收得住 All 并回读相等｜🧨 回读被夹成 1 ⇒ 红");

        // ============================================================
        // C · 应用段对两种 renderer 都写值
        // ============================================================
        var pAll = new ArenaBuilder.ParticleEntry
        {
            renderingLayerMask = 4294967295u,
            lightProbeUsage = 2,          // UseProxyVolume
            sortingOrder = -1,            // ⚠️ 原版 arena2 有负值 ⇒ 不能被夹成 0..1
        };
        MethodInfo mApplyP = StaticMethod("ApplyParticleRendererParams",
            new[] { typeof(ParticleSystemRenderer), typeof(ArenaBuilder.ParticleEntry) });
        Check(mApplyP != null,
              "C1 前提：找到 `ArenaBuilder.ApplyParticleRendererParams(ParticleSystemRenderer, ParticleEntry)`"
            + "｜🧨 删方法 ⇒ 红");
        bool c1 = false;
        try
        {
            if (mApplyP != null)
            {
                mApplyP.Invoke(null, new object[] { psr, pAll });
                c1 = psr.renderingLayerMask == 4294967295u
                  && psr.lightProbeUsage == LightProbeUsage.UseProxyVolume
                  && psr.sortingOrder == -1;
            }
        }
        catch (Exception ex) { Debug.LogError(Tag + " C2 调用异常：" + ex.Message); }
        Check(c1, "C2 `ParticleSystemRenderer` 三字段逐值落上（All / UseProxyVolume / −1）｜🧨 删任一写行 ⇒ 红");

        // C3：缺键（0/0）不能把 PSR 的默认值洗掉
        GameObject psGo2 = new GameObject("B145E8_PSR2");
        psGo2.AddComponent<ParticleSystem>();
        var psr2 = psGo2.GetComponent<ParticleSystemRenderer>();
        psr2.renderingLayerMask = 1u;          // 钉死成 Unity 新建 PSR 的默认，免得吃构建机的差异
        uint defMask = psr2.renderingLayerMask;
        LightProbeUsage defLp = psr2.lightProbeUsage;
        int defSo = psr2.sortingOrder;
        var pZero = new ArenaBuilder.ParticleEntry
        {
            renderingLayerMask = defMask,
            lightProbeUsage = 0,
            sortingOrder = 0,
        };
        bool c3 = false;
        try
        {
            if (mApplyP != null) mApplyP.Invoke(null, new object[] { psr2, pZero });
            c3 = psr2.renderingLayerMask == defMask
              && psr2.lightProbeUsage == defLp
              && psr2.sortingOrder == defSo;
        }
        catch (Exception ex) { Debug.LogError(Tag + " C3 调用异常：" + ex.Message); }
        Check(c3, "C3 清单缺键（lightProbeUsage/sortingOrder = 0）不洗掉 PSR 默认（默认 "
                + defLp + "/" + defSo + " 保持不变）");

        // C3b：缺键（renderingLayerMask = 0）必须兜底成 1，⛔ 不能把掩码写成 0（会把渲染层全灭）
        var pZeroMask = new ArenaBuilder.ParticleEntry { renderingLayerMask = 0u };
        bool c3b = false;
        try
        {
            if (mApplyP != null) mApplyP.Invoke(null, new object[] { psr2, pZeroMask });
            c3b = psr2.renderingLayerMask == 1u;
        }
        catch (Exception ex) { Debug.LogError(Tag + " C3b 调用异常：" + ex.Message); }
        Check(c3b, "C3b `renderingLayerMask = 0`（缺键）兜底成 1（实得 " + psr2.renderingLayerMask
                 + "）｜🧨 直接写 0 ⇒ 渲染层掩码全灭 ⇒ 红");

        // C4/C5：MeshRenderer 侧仍照旧写（防「改 A-26 时把 MeshRenderer 那条路碰坏」）
        GameObject mrGo = new GameObject("B145E8_MR");
        var mr = mrGo.AddComponent<MeshRenderer>();
        var me = new ArenaBuilder.MeshEntry
        {
            renderingLayerMask = 4294967295L,
            lightProbeUsage = 1,
            sortingOrder = 3,
        };
        MethodInfo mApplyM = StaticMethod("ApplyRendererParams",
            new[] { typeof(MeshRenderer), typeof(ArenaBuilder.MeshEntry) });
        Check(mApplyM != null, "C4 前提：找到 `ArenaBuilder.ApplyRendererParams(MeshRenderer, MeshEntry)`");
        bool c5 = false;
        try
        {
            if (mApplyM != null)
            {
                mApplyM.Invoke(null, new object[] { mr, me });
                c5 = mr.renderingLayerMask == uint.MaxValue
                  && mr.lightProbeUsage == LightProbeUsage.BlendProbes
                  && mr.sortingOrder == 3;
            }
        }
        catch (Exception ex) { Debug.LogError(Tag + " C5 调用异常：" + ex.Message); }
        Check(c5, "C5 `MeshRenderer` 侧三字段逐值落上（All / BlendProbes / 3）｜🧨 碰坏 ApplyRendererParams ⇒ 红");

        // ============================================================
        // D · A-25 标记 + 应用段接线（结构）
        // ============================================================
        string src = Read("Assets/WarpforgeArena1/Editor/ArenaBuilder.cs");
        Check(src != null, "D0 前提：读得到 `WarpforgeArena1/Editor/ArenaBuilder.cs`");
        string s = src ?? "";

        Check(s.Contains("ApplyParticleRendererParams(rend, p);"),
              "D1 `BuildContent` 里真的**调了** `ApplyParticleRendererParams(rend, p)`"
            + "｜🧨 只定义不接线（新账里出现过 4 次的形状）⇒ 红");
        Check(!s.Contains("「用原版网格 shader」这条路当前是关的"),
              "D2 A-25：旧的第一段 `<summary>⚠️ 2026-09-20 …当前是关的` 已删（重复 summary 合并）");
        Check(!s.Contains("「用原版网格 shader」这条路当前默认关"),
              "D3 A-25：旧的第二段 `…这条路当前默认关` 已删");
        Check(s.Contains("🔶 自己设计（非原版）：**本体网格 shader 一律用"),
              "D4 A-25：`🔶 自己设计（非原版）` 标记存在（铁律 18）");
        Check(s.Contains("谁定的+日期：调度台 2026-11-05"),
              "D5 A-25：标记写全三样（是什么 / 为什么必须自定 / 谁定的+日期）");

        // D6/D7：A-25 只改注释、行为不变（`WF_ORIGSHADER=1` 仍能打开）
        PropertyInfo prop = typeof(ArenaBuilder).GetProperty("ArenaUseOriginalShaders",
            BindingFlags.NonPublic | BindingFlags.Static);
        Check(prop != null, "D6 前提：找到 `ArenaBuilder.ArenaUseOriginalShaders` 属性");
        string oldEnv = Environment.GetEnvironmentVariable("WF_ORIGSHADER");
        bool off = true, on = false;
        try
        {
            Environment.SetEnvironmentVariable("WF_ORIGSHADER", null);
            off = prop != null && (bool)prop.GetValue(null);
            Environment.SetEnvironmentVariable("WF_ORIGSHADER", "1");
            on = prop != null && (bool)prop.GetValue(null);
        }
        catch (Exception ex) { Debug.LogError(Tag + " D7 异常：" + ex.Message); }
        finally { Environment.SetEnvironmentVariable("WF_ORIGSHADER", oldEnv); }
        Check(!off && on,
              "D7 `WF_ORIGSHADER=1` 仍能打开开关（off=" + off + " / on=" + on
            + "）｜🧨 顺手把常量写死 ⇒ 红");

        UnityEngine.Object.DestroyImmediate(psGo);
        UnityEngine.Object.DestroyImmediate(psGo2);
        UnityEngine.Object.DestroyImmediate(mrGo);

        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }
}
