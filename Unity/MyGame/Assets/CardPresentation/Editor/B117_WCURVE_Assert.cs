// B117_WCURVE_Assert.cs — 执行代理 `B117-WCURVE`：**关键帧斜率（Hermite）**的断言
//
// ============================ 它盯的是什么 ============================
// `Battle/CardAnimOrder.EaseAt` / `EaseAtRaw` 的求值形状，以及两个生成器烘出来的**关键帧格式**：
//   · 旧的 2 数格式 `[t0,v0,t1,v1,…]`（**线性曲线仍然用它**，逐字节不变）；
//   · 新的 4 数格式 `[-1, t0,v0,inSlope0,outSlope0, …]`（**只在曲线非线性时**才有；
//     首格 `-1` 是标记 ⇒ 长度 = `1+4n`（%4==1），与旧格式的偶数长度**互斥**）。
// 为什么要有这文件：2026-11-03 现读发现 `Resources/cardanim_clips.json` / `card_anim_order.json`
// 只存 `(t,v)`、把原版 `AnimationCurve` 每键的 `inSlope/outSlope` 丢掉了 ⇒ 非线性曲线
// （`Atk_StikkaThrow` 的先快后慢、`Atk_Deathspinner_arc_alt` 的弧线、`Jain Zar` 的飞出去再飞回来）
// 按分段线性读**形状是错的**（最大偏 0.25）。本文件把「修好了没有」钉成断言。
//
// ============================ 判据（第一权威 · ⛔ 别在这里抄第二份）============================
//   · `d:/2/tools/decomp_full/CardAnimController__Update.c:36-97`：
//       `easeCurve.Evaluate((t−timeAtStartPos)/timeMoving)`，输出 clamp 到 [0,1]；
//       三轴 `offsetMovementAxis.{x,y,z}Axis.Evaluate(...)`（**没有** clamp）。
//   · Unity `AnimationCurve.Evaluate` = **分段三次 Hermite**：
//       `h00 = 2u³−3u²+1` · `h10 = u³−2u²+u` · `h01 = −2u³+3u²` · `h11 = u³−u²`
//       `v = h00·v0 + h10·dt·outSlope0 + h01·v1 + h11·dt·inSlope1`   （🔴 切线要乘**段长 dt**）
//       —— 序列化侧佐证：`assets_full` 的每条 `m_Curve` 键都带 `inSlope/outSlope/weightedMode`，
//          全表 `weightedMode = 0`（⇒ 直接取两个斜率，不涉及 in/outWeight）。
//   · 数据侧（第一权威 = 原版 clip）：`bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json`
//       的 `animInfo.easeCurve` / `animInfo.offsetMovementAxis.*`。
//
// ============================ 怎么用 ============================
//   `B117WCurveAssert.Run();`   （静态类 + 无参 `Run()`；宿主由调度台接，本文件不改任何 `Editor/*Scene.cs`）
//   ⚠️ 本文件**不建资产、不动盘**，只读 `Resources` 里那两张表。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · W2b ← 去掉「切线 × dt」：`EaseAt` 从 **0.625** 变 **0.75** ⇒ 红（本表绝大多数曲线 `dt = 1`，
//            那是这个错最容易静默躲过的地方 —— 所以这条**专门**造一条 `dt = 0.5` 的曲线）。
//   · W3a ← 生成器不写斜率 / `EaseAt` 不读 4 数格式：`Atk_StikkaThrow` 中点从 **0.75** 掉回 **0.5** ⇒ 红。
//   · W3c ← 同上，`Atk_GrotGrenade` 的 y 弧在 25% 处从 **0.6466** 掉回 **0.5** ⇒ 红。
//   · W1a ← 把旧的 2 数格式也塞进 Hermite 分支（误判格式）：`[0,0,1,1]` 在 0.3 处得 0.3（不变），
//            但 `[0,-0.5,1,-0.5]` 那种**常数**曲线也仍该得 −0.5 —— 真正被判红的是 `HasSlopes` 的长短判据（W1e）。
using UnityEngine;
using CardPresentation;

public static class B117WCurveAssert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B117WCURVE";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5")
                  + "；实得 " + got.ToString("F5") + "）");
    }

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        SectionW1_Format();
        SectionW2_HermiteShape();
        SectionW3_RealData();
        SectionW4_LegacyCompat();
        Debug.Log(Tag + " —— 合计 " + (Pass - pass0) + " 通过 / " + (Fail - fail0) + " 失败");
    }

    // ============================================================ W1：格式判别（纯合成数组）
    static void SectionW1_Format()
    {
        // 旧的 2 数格式：2 键 = 4 个数 —— **不是** 4 数格式（1 键的曲线不存在）
        Check(!CardAnimOrder.HasSlopes(new float[] { 0f, 0f, 1f, 1f }),
              "W1a 2 数格式 `[0,0,1,1]` ⇒ `HasSlopes = false`（4 个数不是 4 数格式：长度 %4 == 0）");
        Check(!CardAnimOrder.HasSlopes(new float[] { 0f, 0f, 0.5f, 1f, 1f, 0f }),
              "W1b 3 键的 2 数格式（6 个数，%4 == 2）⇒ `HasSlopes = false`");
        // 4 数格式：首格 -1 + 每键 4 格 ⇒ 长度 = 1 + 4n（%4 == 1）
        Check(CardAnimOrder.HasSlopes(new float[] { -1f, 0f, 0f, 0f, 2f, 1f, 1f, 0f, 0f }),
              "W1c 4 数格式 `[-1,0,0,0,2, 1,1,0,0]`（9 个数，%4 == 1）⇒ `HasSlopes = true`");
        Check(!CardAnimOrder.HasSlopes(new float[] { 0f, 0f, 0f, 2f, 1f, 1f, 0f, 0f, 0f }),
              "W1d ★ 首格不是 -1（= 把标记写成 0）⇒ `HasSlopes = false` ⇒ 会被当旧格式读"
            + "｜🧨 标记丢失时这条必须红（否则「奇偶互斥」就只剩一半判据）");
        Check(!CardAnimOrder.HasSlopes(null) && !CardAnimOrder.HasSlopes(new float[0]),
              "W1e `null` / 空数组 ⇒ `HasSlopes = false`（不抛）");
        // 长度奇偶互斥：2 数格式永远是偶数长度，4 数格式永远是 1+4n（奇数）⇒ 两个集合不相交
        bool disjoint = true;
        for (int n = 2; n <= 8; n++) if ((1 + 4 * n) % 2 == 0) disjoint = false;
        Check(disjoint, "W1f 长度互斥：4 数格式 `1+4n` 恒为**奇数**、2 数格式恒为**偶数** ⇒ 不需要版本列"
            + "（`BattleDriver` 直接拿 `float[]` 调本类，没有地方传版本号）");
    }

    // ============================================================ W2：Hermite 形状（含切线×dt）
    static void SectionW2_HermiteShape()
    {
        // 2 键、两端切线都是 0 ⇒ 平滑阶跃 3u²−2u³：25% 处 = 0.15625（线性会得 0.25）
        var step = new float[] { -1f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f };
        Near(CardAnimOrder.EaseAt(step, 0.25f), 0.15625f, 1e-5f,
             "W2a 两端切线 0 ⇒ 平滑阶跃：`EaseAt(0.25)` = 3u²−2u³ = **0.15625**（线性会得 0.25）");
        Near(CardAnimOrder.EaseAt(step, 0.5f), 0.5f, 1e-6f,
             "W2a′ 同一条曲线中点仍 = 0.5（对称）—— ⛔ 别拿中点当唯一判据，它分不出直线与平滑阶跃");

        // ★ 切线 × dt 的判别式：dt = 0.5、out0 = 2（每秒单位）
        //   u = 0.5 ⇒ h10 = 0.125 ⇒ 0.125 × 0.5 × 2 = 0.125；h01 = 0.5 ⇒ 合计 **0.625**
        //   🧨 漏乘 dt ⇒ 0.125 × 2 = 0.25 ⇒ 得 0.75（同一条断言，两个值差 0.125）
        var shortSeg = new float[] { -1f, 0f, 0f, 0f, 2f, 0.5f, 1f, 0f, 0f };
        Near(CardAnimOrder.EaseAt(shortSeg, 0.25f), 0.625f, 1e-5f,
             "W2b ★ 切线**乘段长**：dt = 0.5、out0 = 2 ⇒ `EaseAt(0.25)` = **0.625**"
           + "｜🧨 漏乘 dt ⇒ 0.75 ⇒ 红（本表绝大多数曲线 dt = 1，只有这条能抓住它）");

        // 输出 clamp：进 0 以下 / 1 以上
        Near(CardAnimOrder.EaseAt(step, -1f), 0f, 1e-6f, "W2c 进度 < 0 ⇒ clamp 到 0");
        Near(CardAnimOrder.EaseAt(step, 2f), 1f, 1e-6f, "W2d 进度 > 1 ⇒ clamp 到末键值（判据 = `Update` 对 `Evaluate` 输出也 clamp）");

        // 偏移曲线：**不** clamp（原版那三句 Evaluate 没有 clamp）
        var negStep = new float[] { -1f, 0f, -0.5f, 0f, 0f, 1f, -0.5f, 0f, 0f };
        Near(CardAnimOrder.EaseAtRaw(negStep, 0.3f), -0.5f, 1e-5f,
             "W2e `EaseAtRaw` **不** clamp（−0.5 原样返回 —— 偏移曲线会往两边摆）");
        Near(CardAnimOrder.EaseAtRaw(null, 0.3f), 0f, 1e-6f,
             "W2f `EaseAtRaw(null)` = **0**（偏移曲线的默认是「无偏移」；与 `EaseAt(null)` 的线性恒等是两条默认）");
        Near(CardAnimOrder.EaseAt(null, 0.4f), 0.4f, 1e-5f,
             "W2g `EaseAt(null)` = 进度本身（原版默认曲线 `(0,0)→(1,1)`）");
    }

    // ============================================================ W3：真实数据（生成器烘出来的表）
    static void SectionW3_RealData()
    {
        // ① `Atk_StikkaThrow`：首端 out0 = 2、末键 in1 = 0 ⇒ 中点 = 0.75（线性 0.5）
        var q = CardAnimClips.PlacementOf("Atk_StikkaThrow");
        Check(q.HasValue && q.Value.Ease != null && CardAnimOrder.HasSlopes(q.Value.Ease),
              "W3a（前提）`Atk_StikkaThrow` 在表里、且 `ease` 是 4 数格式"
            + "｜🧨 生成器不写斜率 ⇒ `HasSlopes = false` ⇒ 本条红");
        if (q.HasValue && q.Value.Ease != null)
            Near(CardAnimOrder.EaseAt(q.Value.Ease, 0.5f), 0.75f, 1e-4f,
                 "W3a ★ `Atk_StikkaThrow` 中点 = **0.75**（原版 out0 = 2 ⇒ 先快后慢；线性读会得 0.5）");

        // ② `Atk_Deathspinner_arc_alt`：out0 = 1.175、in1 = 0 ⇒ 中点 = 0.5 + 0.125×1.175 = 0.646875
        var qd = CardAnimClips.PlacementOf("Atk_Deathspinner_arc_alt");
        Check(qd.HasValue && qd.Value.Ease != null,
              "W3b（前提）`Atk_Deathspinner_arc_alt` 在表里且有 `ease`");
        if (qd.HasValue && qd.Value.Ease != null)
            Near(CardAnimOrder.EaseAt(qd.Value.Ease, 0.5f), 0.646875f, 1e-4f,
                 "W3b `Atk_Deathspinner_arc_alt` 中点 = **0.646875**（弧线，不是直线）");

        // ③ `Atk_GrotGrenade` 的 y 弧（偏移曲线）：25% 处 = 0.6466（线性 0.5）、中点 = 1.0
        var qg = CardAnimClips.PlacementOf("Atk_GrotGrenade");
        Check(qg.HasValue && qg.Value.Ofy != null && CardAnimOrder.HasSlopes(qg.Value.Ofy),
              "W3c（前提）`Atk_GrotGrenade` 的 `ofy` 是 4 数格式（抛物线弧）");
        if (qg.HasValue && qg.Value.Ofy != null)
        {
            Near(CardAnimOrder.EaseAtRaw(qg.Value.Ofy, 0.25f), 0.646552f, 1e-3f,
                 "W3c ★ `Atk_GrotGrenade` 的 y 弧在 25% 处 = **0.6466**（线性会得 0.5）"
               + "｜🧨 偏移曲线不写斜率 ⇒ 0.5 ⇒ 红");
            Near(CardAnimOrder.EaseAtRaw(qg.Value.Ofy, 0.5f), 1f, 1e-4f,
                 "W3c′ 中点 = 1.0（抛物线最高点 —— 这一条线性也过，只作形状对照）");
        }

        // ④ `Jain Zar`：飞出去再飞回来 —— 末键值 0（`ease(1) = 0`），且是 4 数格式
        var qj = CardAnimClips.PlacementOf("Atk_JainasMor_Throw_Return");
        Check(qj.HasValue && qj.Value.Ease != null && CardAnimOrder.HasSlopes(qj.Value.Ease),
              "W3d（前提）`Atk_JainasMor_Throw_Return` 的 `ease` 是 4 数格式（4 键）");
        if (qj.HasValue && qj.Value.Ease != null)
        {
            Near(CardAnimOrder.EaseAt(qj.Value.Ease, 1f), 0f, 1e-5f,
                 "W3d `Jain Zar` 末键 = **0**（飞出去再飞回来）");
            Near(CardAnimOrder.EaseAt(qj.Value.Ease, 0.75f), 0.78113f, 1e-4f,
                 "W3d′ `Jain Zar` 在 0.75 处 = **0.78113**（按原版 clip 的 in/outSlope 直接算 Hermite 的值）");
        }

        // ⑤ 常数曲线（`UnholySmite_random` 的 `ease` 是 (0,1)→(1,1)、两端切线 0）
        //    ⇒ **非线性判据把它判成「与线性逐值相同」⇒ 保持 2 数格式**（省 4 格、且逐字节不变）
        var qu = CardAnimClips.PlacementOf("UnholySmite_random");
        Check(qu.HasValue && qu.Value.Ease != null && !CardAnimOrder.HasSlopes(qu.Value.Ease),
              "W3e `UnholySmite_random` 的 `ease` 是**常数曲线** ⇒ 仍是 2 数格式（写斜率没有意义）");
        if (qu.HasValue && qu.Value.Ease != null)
            Near(CardAnimOrder.EaseAt(qu.Value.Ease, 0.3f), 1f, 1e-5f,
                 "W3e′ 常数曲线任何进度都 = **1**");

        // ⑥ 表内普查（`CardAnimOrder.Items` 是公开枚举口）：所有曲线数组的**格式不变量**
        int slopedEase = 0, slopedOf = 0, legacy = 0, bad = 0;
        foreach (var card in CardAnimOrder.Items)
        {
            if (card == null || card.steps == null) continue;
            foreach (var s in card.steps)
            {
                if (s == null) continue;
                if (s.ease != null && s.ease.Length >= 4)
                {
                    if (CardAnimOrder.HasSlopes(s.ease)) { slopedEase++; if (!OkSloped(s.ease)) bad++; }
                    else legacy++;
                }
                if (OkAny(s.ofx)) { if (CardAnimOrder.HasSlopes(s.ofx)) { slopedOf++; if (!OkSloped(s.ofx)) bad++; } else legacy++; }
                if (OkAny(s.ofy)) { if (CardAnimOrder.HasSlopes(s.ofy)) { slopedOf++; if (!OkSloped(s.ofy)) bad++; } else legacy++; }
                if (OkAny(s.ofz)) { if (CardAnimOrder.HasSlopes(s.ofz)) { slopedOf++; if (!OkSloped(s.ofz)) bad++; } else legacy++; }
            }
        }
        Check(bad == 0,
              "W3f 表内普查：4 数格式数组全部满足「长度 %4 == 1 · 首格 == -1 · 时间严格递增」（坏 " + bad + " 条）");
        Check(slopedEase >= 1 && slopedEase <= 12,
              "W3g 逐卡演出表里**带斜率的 `ease`** = **" + slopedEase + "** 拍（现读 3：Morvenn Vahl / Lhaska Szenari /"
            + " Primus Saffa Rhiannor 那三张共用 `Atk_BulletImpact_MissileToTarget_warlord`）"
            + "｜🧨 生成器不写斜率 ⇒ 0 ⇒ 红；把线性曲线也写成 4 数 ⇒ 远超上限 ⇒ 红");
        Check(slopedOf >= 1 && slopedOf <= 12,
              "W3h 逐卡演出表里**带斜率的偏移曲线** = **" + slopedOf + "** 条（现读 4：ofx 3 · ofy 1）");
        Check(legacy >= 1,
              "W3i 逐卡演出表里仍是**旧 2 数格式**的曲线数组 = **" + legacy + "** 条（线性曲线逐字节不变）");
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

    static bool OkAny(float[] a) { return a != null && a.Length >= 4; }

    // ============================================================ W4：旧 2 数格式逐值不变
    static void SectionW4_LegacyCompat()
    {
        // 线性曲线（原版 (0,0)→(1,1) 且两端切线 == 弦斜率）⇒ 仍是 2 数格式，求值逐值不变
        var linear = new float[] { 0f, 0f, 1f, 1f };
        Near(CardAnimOrder.EaseAt(linear, 0.3f), 0.3f, 1e-5f,
             "W4a 旧 2 数格式 `[0,0,1,1]` ⇒ 0.3 处仍是 **0.3**（分段线性，逐值不变）");
        var constCurve = new float[] { 0f, 1f, 1f, 1f };
        Near(CardAnimOrder.EaseAt(constCurve, 0.3f), 1f, 1e-5f,
             "W4b 旧 2 数格式的常数曲线 `[0,1,1,1]` ⇒ **1**");
        // 3 键旧格式（原版偏移弧线大量是 3 键）
        var three = new float[] { 0f, 0f, 0.5f, 1f, 1f, 0f };
        Near(CardAnimOrder.EaseAt(three, 0.25f), 0.5f, 1e-5f,
             "W4c 旧 2 数格式的 3 键曲线 `[0,0,0.5,1,1,0]` ⇒ 0.25 处 = **0.5**（前段线性）");
        Near(CardAnimOrder.EaseAt(three, 0.75f), 0.5f, 1e-5f,
             "W4d 同一条曲线 0.75 处 = **0.5**（后段线性下降）");
        // 单段 / 空段边界
        Near(CardAnimOrder.EaseAt(new float[] { 0f, 0f, 0f, 1f }, 0.5f), 1f, 1e-5f,
             "W4e 零长段（两键同一时刻）⇒ 取右键值（旧行为不变）");
    }
}
