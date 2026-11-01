// B114_W40_Assert.cs — 第一百一十四会话 · 写手 `W-40` · 账 `B-269` + `B-278` + `B-279`
//
//  ① `B-269`（通用断言）：`CardDisplayEffects`（**建件时**一次性推色 ⇒ `ImageQuad.SetVertexTint`）与
//     `UIBorderGlow`（**播放时**改 α ⇒ `SetVertexTintChannel(true)` 再 `SetTint`）是**同一个病的两条独立路**
//     （判据 → `报告/B114_W31_顶点色与CLIP关键字.md` §七·2）。两条今天都对，但**没有任何一条断言把它们绑在一起**
//     ⇒ 谁哪天动了 `ImageQuad.ApplyTintColor` 的「通道没开」分支，两条会一起静默退回去。
//     ⇒ 本文件立**一条通用不变量**：「这颗 quad 用的材质**没有 `_Color`** ⇒ `UsesVertexTint` 必须为真」，
//        并对**两条路各自**跑一遍（同一句判据、两份夹具）。
//  ② `B-278`：`PlayTargetPool` 的**棋盘那批**仍传 `thisCard = null`（`EffectResolver.cs:2189`），而预览那批传了
//     ⇒ 依赖载体的那几格（`excludeSimilarCards`、四组 stat 的 `*FilterOption`）对棋盘候选**仍未判**
//     （判据 → `报告/B114_W34_预览侧筛与整闸.md` §五·1）。原版对**每一个**候选都传 `thisCard`
//     （`RawCardScript__IsValidTarget.c:44` 第 1 实参）。
//  ③ `B-279`：`spec.Side == "self"` 会让 `PlayTargetPool` 建出**空池**（`AddSide` 两条都不命中）。
//     ⚠️ 本文件**不改代码**：`spec` 的唯一生产者 `EffectText.PickTarget` 的出口闸只放行 `own` / `enemy`
//     ⇒ 那个形状**结构上不可达**（不是缺陷）。断言钉住的正是**这道闸**（详见 `CheckB114W40BoardThisCardAndSelfSide`）。
//
// ============================ 🔴 灭自证（什么改动能让它红）============================
//  · `B-269`
//    - A1 / B1（两条路各自满足不变量）：把 `CardDisplayEffects` 的 `SetVertexTint` 改回 `SetTint`、
//      或把 `UIBorderGlow.ChangeGraphicAlpha` 里那句 `SetVertexTintChannel(true)` 摘掉 ⇒ 对应那条红。
//    - D1（**材质有 `_Color`** 的对照件）：不变量写成「一律要求顶点色」⇒ D1 红（那会与 `_Color` **双乘**）。
//    - E1（**无 `_Color` 且通道没开**的对照件）：不变量写成恒真 ⇒ E1 红（说明 A1/B1 什么都没证明）。
//    - F1 / F2 / F3（源码级）：两条生产路各自**真的**走 guarded API（运行时夹具只能证明「这样调就对」，
//      证明不了「生产代码是这样调的」）。
//  · `B-278`
//    - E1：把 `EffectResolver.cs:2189` 那句的 `thisCard: asIfOnBoard` 摘掉 ⇒ criteria **整条不筛**
//      ⇒ 同卡单位留在池里、E1 红。
//    - E3（并排对照）：同一夹具**只把 `excludeSimilarCards` 翻成 0** ⇒ 同卡单位必须留在池里。
//  · `B-279`
//    - C1：`PickTarget` 的出口闸改成也放行 `self`（或别处再写一个生产者）⇒ C1 红 ——
//      那一天必须同时决定「`self` 的侧」，⛔ 不能只放行不管 `AddSide`。
//    - C2（并排对照）：同样形状只把 `Side` 换成 `own` / `enemy` ⇒ `PickTarget` 必须非 null。
//
// ============================ 怎么接（宿主 = `RuleEngine/Editor/RuleEngineTest.cs`）============================
//  在 `Run()` 的聚合行（**同一物理行**、行尾 `//` **之前**）追加：
//      Guard("CheckB114W40VertexTintInvariant", CheckB114W40VertexTintInvariant);
//      Guard("CheckB114W40BoardThisCardAndSelfSide", CheckB114W40BoardThisCardAndSelfSide);
//  ⚠️ 本仓已 6 次踩「追加落到行尾 `//` 后面 ⇒ 代码被注释吞掉、断言一条没跑」。
//  ⚠️ 入口名**不以 `Test` / `Report` 打头**（`VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例）。
//
// ============================ 副作用 ============================
//  · `B-269` 段会**建/毁临时 GameObject**（`ImageQuad` + `UIBorderGlow`），材质一律用**克隆**（⛔ 不碰工程里的资产材质，
//    `ImageQuad.SetMaterial` 会写 `mainTexture` ⇒ 直接传资产会把它弄脏）。全程 `finally` 里 `DestroyImmediate`。
//  · 引擎段只读卡表 + 只在**自建的两局**里造实例；不写存档、不动全局状态、不跑回合。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==========================================================================================
    //  ① `B-269` · 通用不变量：「材质无 `_Color` ⇒ `UsesVertexTint` 必须为真」
    // ==========================================================================================
    const string W40P = "[B114_W40] ";
    const string W40BorderMatRes = "Art/vfx/Border Effect For Level up";
    const string W40BorderShader = "WarpforgeVFX/UI/UI Border Highlight";

    /// <summary>**通用不变量本身**（唯一一处；两条路都用它判，⛔ 别各写一份）。
    /// <para>「材质没有 `_Color`」= `HasProperty("_Color")` 为假 ⇒ 写 `_Color` 的那一路**静默落空**
    /// （uGUI 里原本是 `CanvasRenderer` 顶点色）⇒ 必须走 <see cref="ImageQuad.UsesVertexTint"/>。</para>
    /// <para>⚠️ 材质 `null` 判**不满足**（那是「根本没材质」，不是「没有 `_Color`」）。</para></summary>
    static bool W40NoColorImpliesVertexTint(ImageQuad q, out string detail)
    {
        if (q == null) { detail = "quad == null"; return false; }
        var mr = q.GetComponent<MeshRenderer>();
        var mat = mr != null ? mr.sharedMaterial : null;
        if (mat == null) { detail = "材质 == null（不是「没有 `_Color`」）"; return false; }
        bool hasColor = mat.HasProperty("_Color");
        bool ok = hasColor || q.UsesVertexTint;
        detail = "材质「" + mat.name + "」HasProperty(_Color)=" + (hasColor ? "真" : "假")
               + " · UsesVertexTint=" + (q.UsesVertexTint ? "真" : "假")
               + " ⇒ " + (ok ? "满足" : "**不满足**");
        return ok;
    }

    static bool W40Near(float a, float b, float tol = 1e-3f) { return Mathf.Abs(a - b) <= tol; }

    static string W40DumpAlpha(Mesh m)
    {
        if (m == null) return "null";
        var c = m.colors;
        if (c == null || c.Length == 0) return "无 colors";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < c.Length && i < 4; i++) { if (i > 0) sb.Append('/'); sb.Append(c[i].a.ToString("F3")); }
        return sb.ToString();
    }

    static bool W40AllAlpha(Mesh m, float a)
    {
        if (m == null) return false;
        var c = m.colors;
        if (c == null || c.Length != 4) return false;
        for (int i = 0; i < 4; i++) if (!W40Near(c[i].a, a)) return false;
        return true;
    }

    static string W40ReadSrc(string rel)
    {
        try
        {
            var p = System.IO.Path.Combine(Application.dataPath, rel);
            return System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p) : null;
        }
        catch { return null; }
    }

    static int W40Count(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    /// <summary>`B-269`：**通用不变量**（材质无 `_Color` ⇒ 必须走顶点色通道）对
    /// 「`CardDisplayEffects` 建件推色」与「`UIBorderGlow` 播放改 α」两条**独立路**各跑一遍。</summary>
    public static void CheckB114W40VertexTintInvariant()
    {
        var prod = Resources.Load<Material>(W40BorderMatRes);
        CheckTrue(prod != null, W40P + "前提 0：`Resources.Load<Material>(\"" + W40BorderMatRes + "\")` 取得到"
                                  + "（= 两条路**共用**的那颗生产材质）");
        if (prod == null) return;
        CheckTrue(!prod.HasProperty("_Color"),
                  W40P + "★ 前提 0b：生产材质 **没有 `_Color`**（判据 = `报告/B114_R5_Reveal_DXBC.md` §2·1 `o2 = v4` / §2·2 ⑧；"
                       + "若哪天补了 `_Color`，本段会先红 —— 那意味着这个不变量不再需要，⛔ 别默默删断言）");
        var sh = Shader.Find(W40BorderShader);
        CheckTrue(sh != null, W40P + "前提 0c：`Shader.Find(\"" + W40BorderShader + "\")` 取得到");

        var roots = new List<GameObject>();
        var mats = new List<Material>();
        try
        {
            var rootA = new GameObject("B114W40_A"); roots.Add(rootA);
            var rootB = new GameObject("B114W40_B"); roots.Add(rootB);
            var rootV = new GameObject("B114W40_VETO"); roots.Add(rootV);
            var tint = new Color(1f, 0.8f, 0.6f, 0.6f);

            // ==============================================================================
            //  A · `CardDisplayEffects.BuildImage` 那条路（建件时一次性推色）
            //    生产次序（`Shell/CardDisplayEffects.cs`）：`ImageQuad.Create`（出厂 `Sprites/Default`）
            //    → `if (im.hasAmp) quad.SetVertexTint(tint);` → **之后**由 `AnimateUIMaterialProperty`
            //    把材质换成 `Border Effect For Level up`（无 `_Color`）⇒ 只有顶点色那一笔活下来。
            // ==============================================================================
            var qA = ImageQuad.Create(rootA.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                      new Vector2(0.5f, 0.5f), "W40_A_Quad");
            CheckTrue(qA != null, W40P + "前提 A0：`ImageQuad.Create` 建得出载体");
            if (qA != null)
            {
                var mrA = qA.GetComponent<MeshRenderer>();
                CheckTrue(mrA != null && mrA.sharedMaterial != null && mrA.sharedMaterial.HasProperty("_Color"),
                          W40P + "前提 A0b：出厂材质（`Sprites/Default`）**有 `_Color`** ⇒ 下面那句 `SetVertexTint` 是**显式换路**，"
                               + "不是默认行为（否则 A1 测的就是「本来就对」）");
                var instA = new Material(prod); mats.Add(instA);
                CheckTrue(!instA.HasProperty("_Color"), W40P + "前提 A0c：克隆出来的生产材质也没有 `_Color`");

                qA.SetVertexTint(tint);      // = `CardDisplayEffects.BuildImage` 的 `if (im.hasAmp)` 那一支（唯一判据）
                qA.SetMaterial(instA);       // = 之后 `AnimateUIMaterialProperty` 的换材质（次序同生产）
                string dA;
                CheckTrue(W40NoColorImpliesVertexTint(qA, out dA),
                          W40P + "★ A1【结论 · `B-269`】**CardDisplayEffects 那条路**满足通用不变量（材质无 `_Color` ⇒ 必须走顶点色）：" + dA
                               + "｜🧨 把 `if (im.hasAmp) quad.SetVertexTint(tint)` 改回 `SetTint` ⇒ 这条红");
                CheckTrue(W40Near(qA.Tint.a, tint.a),
                          W40P + "A2 推上去的颜色**读得回来**（`Tint.a` 期望 " + tint.a.ToString("F3")
                               + "，实得 " + qA.Tint.a.ToString("F3") + "）");
                var mfA = qA.GetComponent<MeshFilter>();
                CheckTrue(mfA != null && mfA.sharedMesh != null, W40P + "前提 A3：载体有网格（`MeshFilter.sharedMesh`）");
                if (mfA != null && mfA.sharedMesh != null)
                {
                    CheckTrue(W40AllAlpha(mfA.sharedMesh, tint.a),
                              W40P + "A3b **顶点色 α 真的写进了网格**（期望 " + tint.a.ToString("F3")
                                   + "，实得 " + W40DumpAlpha(mfA.sharedMesh) + "）—— ⛔ 不是只翻了个开关");
                }
            }

            // ==============================================================================
            //  B · `UIBorderGlow.ChangeGraphicAlpha` 那条路（播放时改 α）
            //    生产次序：材质已经是那颗无 `_Color` 的生产材质 → `ChangeGraphicAlpha(target)`
            //    （内部 `if (!UsesVertexTint && !QuadMaterialHasColor()) SetVertexTintChannel(true)` 再 `SetTint`）。
            // ==============================================================================
            var qB = ImageQuad.Create(rootB.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                      new Vector2(0.5f, 0.5f), "W40_B_Quad");
            CheckTrue(qB != null, W40P + "前提 B0：第二条路的载体建得出来");
            if (qB != null)
            {
                var instB = new Material(prod); mats.Add(instB);
                qB.SetMaterial(instB);
                CheckTrue(!qB.UsesVertexTint, W40P + "前提 B0b：换完材质、**还没**调 `ChangeGraphicAlpha` 时通道是关的");
                var glow = qB.gameObject.AddComponent<UIBorderGlow>();
                CheckTrue(glow != null, W40P + "前提 B0c：`UIBorderGlow` 挂得上（挂在**渲染件那个 GO** 上）");
                if (glow != null)
                {
                    glow.ChangeGraphicAlpha(0.35f);
                    string dB;
                    CheckTrue(W40NoColorImpliesVertexTint(qB, out dB),
                              W40P + "★ B1【结论 · `B-269`】**UIBorderGlow 那条路**满足**同一条**不变量：" + dB
                                   + "｜🧨 把 `ChangeGraphicAlpha` 里那句 `SetVertexTintChannel(true)` 摘掉 ⇒ 这条红");
                    CheckTrue(W40Near(qB.Tint.a, 0.35f),
                              W40P + "B2 α 读得回来（期望 0.350，实得 " + qB.Tint.a.ToString("F3") + "）");
                    var mfB = qB.GetComponent<MeshFilter>();
                    if (mfB != null && mfB.sharedMesh != null)
                        CheckTrue(W40AllAlpha(mfB.sharedMesh, 0.35f),
                                  W40P + "B2b 顶点色 α 真的写进网格（期望 0.350，实得 " + W40DumpAlpha(mfB.sharedMesh) + "）");
                    else
                        CheckTrue(false, W40P + "B2b 前提失败：第二条路没有网格（那 B2 是假绿）");
                }
            }

            // ==============================================================================
            //  C · **通用**：按渲染件逐颗扫一遍（不是只看上面两颗）——不变量要在**整棵树**上成立
            // ==============================================================================
            var guads = new List<ImageQuad>();
            guads.AddRange(rootA.GetComponentsInChildren<ImageQuad>(true));
            guads.AddRange(rootB.GetComponentsInChildren<ImageQuad>(true));
            CheckTrue(guads.Count >= 2, W40P + "前提 C0：扫描覆盖到 >= 2 颗（实得 " + guads.Count + "）—— 否则 C1 是空转");
            int noColor = 0;
            for (int i = 0; i < guads.Count; i++)
            {
                var q = guads[i];
                if (q == null) continue;
                var mrq = q.GetComponent<MeshRenderer>();
                if (mrq != null && mrq.sharedMaterial != null && !mrq.sharedMaterial.HasProperty("_Color")) noColor++;
                string dq;
                CheckTrue(W40NoColorImpliesVertexTint(q, out dq),
                          W40P + "★ C1 逐颗扫「" + q.name + "」满足通用不变量：" + dq);
            }
            CheckTrue(noColor >= 2, W40P + "前提 C1b：扫到的件里 >= 2 颗材质**没有 `_Color`**（实得 " + noColor
                                     + "）—— 否则 C1 全是「左侧不成立 ⇒ 恒真」的假绿");

            // ==============================================================================
            //  D · 灭自证 ①：材质**有** `_Color` ⇒ 不变量左侧不成立 ⇒ 通道**不许**被强开
            // ==============================================================================
            var qD = ImageQuad.Create(rootA.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                      new Vector2(0.5f, 0.5f), "W40_D_ColorQuad");
            CheckTrue(qD != null, W40P + "前提 D0：对照件建得出来");
            if (qD != null)
            {
                var mrD = qD.GetComponent<MeshRenderer>();
                CheckTrue(mrD != null && mrD.sharedMaterial != null && mrD.sharedMaterial.HasProperty("_Color"),
                          W40P + "前提 D0b：对照件出厂材质**有** `_Color`");
                qD.SetTint(new Color(0.2f, 0.3f, 0.4f, 0.5f));
                CheckTrue(!qD.UsesVertexTint,
                          W40P + "★ D1【灭自证】材质**有** `_Color` ⇒ **不强制**走顶点色"
                               + "｜🧨 把不变量写成「一律要求顶点色」⇒ 这条红（且会与 `_Color` **双乘**）");
                CheckTrue(mrD != null && mrD.sharedMaterial != null && W40Near(mrD.sharedMaterial.color.a, 0.5f),
                          W40P + "★ D2【同一路的正对照】有 `_Color` 时**旧路真的写进去了**（`_Color.a` 期望 0.500，实得 "
                               + (mrD != null && mrD.sharedMaterial != null ? mrD.sharedMaterial.color.a.ToString("F3") : "n/a")
                               + "）—— 说明 D1 不是「什么都没生效」");
            }

            // ==============================================================================
            //  E · 灭自证 ②：**同一颗无 `_Color` 材质**、通道没开 ⇒ 不变量必须**不成立**
            //    （⛔ 故意不调 `SetTint`：那会在无 `_Color` 的材质上打一条 Unity 错误日志，噪声盖过断言）
            // ==============================================================================
            var qE = ImageQuad.Create(rootV.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                      new Vector2(0.5f, 0.5f), "W40_E_Quad");
            CheckTrue(qE != null, W40P + "前提 E0：否决件建得出来");
            if (qE != null)
            {
                var instE = new Material(prod); mats.Add(instE);
                qE.SetMaterial(instE);
                string dE;
                CheckTrue(!W40NoColorImpliesVertexTint(qE, out dE),
                          W40P + "★ E1【灭自证】**无 `_Color` 且顶点色通道没开** ⇒ 不变量**不成立**（实得 " + dE + "）"
                               + "｜🧨 若这条也绿，说明不变量恒真 —— A1 / B1 / C1 就什么都没证明");
            }

            // ==============================================================================
            //  F · 源码级：两条**独立路**各自真的走 guarded API
            //    （运行时夹具只能证明「这样调就对」；生产代码改回旧路它看不出来 ⇒ 这里绑一次源码）
            // ==============================================================================
            var cdeSrc = W40ReadSrc("CardPresentation/Shell/CardDisplayEffects.cs");
            CheckTrue(cdeSrc != null, W40P + "前提 F0：读得到 `CardPresentation/Shell/CardDisplayEffects.cs`");
            if (cdeSrc != null)
            {
                CheckTrue(W40Count(cdeSrc, "if (im.hasAmp) quad.SetVertexTint(tint);") == 1,
                          W40P + "★ F1 `CardDisplayEffects.BuildImage` 的 `hasAmp` 那一支仍然走 `SetVertexTint`"
                               + "（实际 " + W40Count(cdeSrc, "if (im.hasAmp) quad.SetVertexTint(tint);") + " 处）"
                               + "｜🧨 改成 `SetTint` ⇒ 这一条红（A1 看不出来：它自己也是这么调的）");
                CheckTrue(W40Count(cdeSrc, "else quad.SetTint(tint);") == 1,
                          W40P + "★ F2 另一支仍然走 `SetTint`（材质**有** `_Color` 的那档，⛔ 别把它也换成顶点色 —— 会双乘）");
            }
            var glowSrc = W40ReadSrc("CardPresentation/Shell/UIBorderGlow.cs");
            CheckTrue(glowSrc != null, W40P + "前提 F0b：读得到 `CardPresentation/Shell/UIBorderGlow.cs`");
            if (glowSrc != null)
            {
                CheckTrue(W40Count(glowSrc, "if (!_quad.UsesVertexTint && !QuadMaterialHasColor()) _quad.SetVertexTintChannel(true);") == 1,
                          W40P + "★ F3 `UIBorderGlow.ChangeGraphicAlpha` 的换路闸仍在"
                               + "（实际 " + W40Count(glowSrc, "if (!_quad.UsesVertexTint && !QuadMaterialHasColor()) _quad.SetVertexTintChannel(true);") + " 处）");
            }
        }
        finally
        {
            for (int i = 0; i < mats.Count; i++) if (mats[i] != null) UnityEngine.Object.DestroyImmediate(mats[i]);
            for (int i = 0; i < roots.Count; i++) if (roots[i] != null) UnityEngine.Object.DestroyImmediate(roots[i]);
        }
    }

    // ==========================================================================================
    //  ② `B-278` · 棋盘那批也传 `thisCard`     ③ `B-279` · `spec.Side == "self"` 的现状
    // ==========================================================================================
    static string W40DumpPool(List<UnitState> pool)
    {
        if (pool == null) return "null";
        var sb = new System.Text.StringBuilder("[" + pool.Count + "] ");
        for (int i = 0; i < pool.Count; i++)
        { if (i > 0) sb.Append(", "); sb.Append(pool[i] != null && pool[i].Card != null ? pool[i].Card.Name : "null"); }
        return sb.ToString();
    }

    /// <summary>37 格填「全已知」的卡级 criteria（形状同 `B114_W34` 的 `W34Spec`，只多两个可调维度）。</summary>
    static CardDef.CountSetSpec W40Spec(int friendly, int includeSelf, int excludeSimilar, int includeHero)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = CardDef.CountSetSpec.Unknown,
            TargetsAffected = 10,
            ChosenArmy = 0,
            ArmyFilter = 0,
            KeywordFilter = 0,
            ExcludeRace = 0,
            TraitsFilter = 0,
            ExcludeTrait = 0,
            MaxTargets = 0,
            TargetSelectMethod = 0,
            AttackFilter = 0,
            AttackFilterOption = 0,
            CurrentHealthFilter = 0,
            CurrentHealthFilterOption = 0,
            ManaCostFilter = 0,
            ManaCostFilterOption = 0,
            RarityFilter = 0,
            RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendly,
            EnemyUnits = 0,
            IncludeHero = includeHero,
            IncludeMinions = 1,
            IncludeSpells = 0,
            IncludeWhispers = 0,
            IncludeWeapons = 0,
            IncludeRemnants = 1,
            IncludeHeroParts = 0,
            IncludeSelf = includeSelf,
            ExcludeNotSelf = 0,
            ExcludeSimilarCards = excludeSimilar,
            ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0,
            ExcludeEnemyHero = 0,
            SelectFromChooseResolution = 0,
            FilterSpecificCardIds = new string[0],
            ExcludeEnchantmentIds = new string[0],
        };
    }

    /// <summary>落地后带一个「指友军」正文的单位卡（`targeted = 1`）。`criteria = null` ⇒ 不挂卡级 criteria。</summary>
    static CardDef W40Unit(string name, string desc, CardDef.CountSetSpec criteria, int keywordId)
    {
        var c = new CardDef(name, name, "unit", desc, "common", "Test",
                            0, 1, 5, 0, null, subtype: "Infantry", targeted: 1);
        c.AttachKeywordId(keywordId);
        if (criteria != null) c.AttachCardTargetCriteria(criteria, true);
        return c;
    }

    public static void CheckB114W40BoardThisCardAndSelfSide()
    {
        // ======================================================================================
        //  0 · 数据面前提：`B-278` 今天**零行为变化**的两条理由
        // ======================================================================================
        var db = CardDatabase.Load();
        CheckTrue(db != null && db.Count > 1000, W40P + "前提 0：卡表装上了（" + (db != null ? db.Count : -1) + " 张）");
        if (db == null) return;
        int simN = 0, optN = 0, optTargetedN = 0;
        for (int i = 0; i < db.Count; i++)
        {
            var c = db[i];
            if (c == null || !c.CardTargetCriteriaVersioned || c.CardTargetCriteria == null) continue;
            var s = c.CardTargetCriteria;
            if (s.ExcludeSimilarCards != 0 && s.ExcludeSimilarCards != CardDef.CountSetSpec.Unknown) simN++;
            bool anyOpt = (s.AttackFilterOption != 0 && s.AttackFilterOption != CardDef.CountSetSpec.Unknown)
                       || (s.CurrentHealthFilterOption != 0 && s.CurrentHealthFilterOption != CardDef.CountSetSpec.Unknown)
                       || (s.ManaCostFilterOption != 0 && s.ManaCostFilterOption != CardDef.CountSetSpec.Unknown)
                       || (s.RarityFilterOption != 0 && s.RarityFilterOption != CardDef.CountSetSpec.Unknown);
            if (anyOpt)
            {
                optN++;
                if (c.Targeted == 1) optTargetedN++;
            }
        }
        CheckTrue(simN == 0, W40P + "★ 前提 0a：全池 `excludeSimilarCards != 0` = **" + simN + " 张**"
                               + "（`B114_W34` §五·1 现读 0）—— 这是「`B-278` 今天零行为变化」的一半");
        CheckTrue(optTargetedN == 0, W40P + "★ 前提 0b：四组 `*FilterOption != 0` 的卡 = " + optN + " 张，其中 `targeted == 1` 的 = **"
                                     + optTargetedN + " 张**（唯一那张 `Aerial Seeding` 是 `targeted == 0` ⇒ "
                                     + "`ApplyCardTargetCriteriaFilter` 在 `card.Targeted == 0` 就整条不筛）"
                                     + " —— 若哪天 > 0，本段的两条结论要按新卡重算（那是**数据**变了，不是断言错）");

        // ======================================================================================
        //  D/E · `B-278`：棋盘那批也拿 `thisCard`（`excludeSimilarCards` 是现成能判的那一格）
        // ======================================================================================
        {
            var sim = W40Unit("W40Sim", "Rally: Heal 2 to a friendly unit",
                              W40Spec(friendly: 1, includeSelf: 1, excludeSimilar: 1, includeHero: 0), keywordId: 5);
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            var same = Place(ctx, 0, 1, sim);                                    // 棋盘上「同一张原卡」
            var other = Place(ctx, 0, 2, Unit("W40Other", 1, 1, 5));             // 棋盘上另一张
            var prev = new UnitState(ctx.NewInstance(sim), false);               // 待落地那张自己（预览）

            var ops = EffectText.Parse(sim.Desc, out _, out _, card: sim);
            var spec = EffectText.PickTarget(ops);
            CheckTrue(spec != null && spec.Side == "own",
                      W40P + "前提 D1：夹具解析出 `Side == \"own\"` 的选目标规格（实得 " + (spec == null ? "null" : spec.Side)
                           + "）—— 否则侧筛会先把预览挡掉，测的就不是 `thisCard`");
            CheckTrue(same.Card == sim && other.Card != sim && prev.Card == sim,
                      W40P + "前提 D2：棋盘上有一张**同一原卡**（`same.Card == sim`）、另有一张不同卡；预览就是 `sim`");

            string whyOld;
            bool? vOld = RuleCore.CardTargetCriteriaAccepts(ctx, 0, sim, same, out whyOld);   // 不传 thisCard = 改前的棋盘口径
            CheckTrue(vOld == null,
                      W40P + "★ 前提 D3【机制】：**不传 `thisCard`** ⇒ 逐候选谓词给 `null`（`excludeSimilarCards` 要拿载体比原卡，本版没有载体）"
                           + "；实得 " + (vOld == null ? "null" : vOld.ToString()) + "（" + (whyOld ?? "ok") + "）"
                           + " —— 这就是改前「棋盘那批这一格没判」的直接证据");
            string whyNew;
            bool? vNew = RuleCore.CardTargetCriteriaAccepts(ctx, 0, sim, same, out whyNew, prev);
            CheckTrue(vNew == false,
                      W40P + "★ 前提 D4：传 `thisCard = 待落地那张` ⇒ 判得出 `false`（同一张原卡）；实得 "
                           + (vNew == null ? "null" : vNew.ToString()) + "（" + (whyNew ?? "ok") + "）");

            var pool = RuleCore.PlayTargetPool(ctx, 0, sim, prev);
            CheckTrue(pool != null, W40P + "前提 D5：`PlayTargetPool` 有返回");
            if (pool != null)
            {
                CheckTrue(pool.Count == 1 && pool.Contains(other),
                          W40P + "★ E1【结论 · `B-278`】**棋盘那批也拿 `thisCard`** ⇒ 与待落地那张同一原卡的棋盘单位被淘汰、其余保留"
                               + "（期望 1 个 = W40Other，实得 " + W40DumpPool(pool) + "）"
                               + "｜🧨 把 `EffectResolver.cs:2189` 那句的 `thisCard: asIfOnBoard` 摘掉 ⇒ 那条 criteria **整条不筛** ⇒ 同卡单位留在池里、E1 红");
                CheckTrue(!pool.Contains(same) && !pool.Contains(prev),
                          W40P + "★ E2 同一条的另一面：**同卡单位**与**预览自己**都不在池里（`excludeSimilarCards` 把它们都淘汰了）");
            }
        }
        {
            // 并排对照：**同一夹具**，只把 `excludeSimilarCards` 翻成 0
            var keep = W40Unit("W40SimKeep", "Rally: Heal 2 to a friendly unit",
                               W40Spec(friendly: 1, includeSelf: 1, excludeSimilar: 0, includeHero: 0), keywordId: 5);
            var ctx2 = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx2, 2);
            var same2 = Place(ctx2, 0, 1, keep);
            var other2 = Place(ctx2, 0, 2, Unit("W40Other2", 1, 1, 5));
            var prev2 = new UnitState(ctx2.NewInstance(keep), false);
            var pool2 = RuleCore.PlayTargetPool(ctx2, 0, keep, prev2);
            CheckTrue(pool2 != null && pool2.Count == 3
                      && pool2.Contains(same2) && pool2.Contains(other2) && pool2.Contains(prev2),
                      W40P + "★ E3【灭自证 · 与 E1 并排】只把 `excludeSimilarCards` 翻成 0 ⇒ **同卡单位留在池里**、预览也进池（期望 3 个，实得 "
                           + W40DumpPool(pool2) + "）"
                           + "｜🧨 说明 E1 的差异只可能来自那一格 criteria（不是「池本来就少一个」）");
        }

        // ======================================================================================
        //  C · `B-279`：`spec.Side == "self"` 的现状 —— 钉住**唯一生产者**的出口闸
        //    `PlayTargetPool` 的 `spec` 只来自 `EffectText.PickTarget(ops)`（它自己上一句）。
        //    `PickTarget` 的出口闸 = `if (t.Side == "enemy" || t.Side == "own") return t;`
        //    ⇒ `"self"`（以及 `"any"` / `"prev"` / `"eventtarget"` / 空）**结构上到不了 `AddSide`**
        //      ⇒ 「`self` ⇒ 空池」是**死支**，不是缺陷（原版也没有这个文本侧；`ResolvedSide` 只是把
        //        `self` 当 `own` 的**别名**读，见 `EffectText.ResolvedSide`）。
        // ======================================================================================
        {
            EffectOp W40Op(string side) { return new EffectOp { Verb = "heal", Amount = 2,
                Target = new EffectTargetSpec { Raw = "(W40 " + side + ")", Side = side, Kind = "unit", Count = 1 } }; }

            foreach (var side in new[] { "self", "any", "prev", "eventtarget", "" })
            {
                var t = EffectText.PickTarget(new[] { W40Op(side) });
                CheckTrue(t == null,
                          W40P + "★ C1【结论 · `B-279`】`PickTarget` 对 `Side == \"" + side + "\"` 返回 **null**"
                               + "（= `PlayTargetPool` 在 `spec == null` 那道前置闸就返回空池，**根本到不了** `AddSide`）"
                               + "｜🧨 哪天把这道出口闸放宽到放行 `" + side + "` ⇒ 这条红 —— 那一天必须同时定「`" + side
                               + "` 的侧」，⛔ 不能只放行不管 `AddSide`");
            }
            // 灭自证：同样形状，只把 `Side` 换成两个**真的会被放行**的值
            var tOwn = EffectText.PickTarget(new[] { W40Op("own") });
            var tFoe = EffectText.PickTarget(new[] { W40Op("enemy") });
            CheckTrue(tOwn != null && tOwn.Side == "own",
                      W40P + "★ C2【灭自证 · 与 C1 并排】同一构造函数、只把侧换成 `own` ⇒ `PickTarget` **非 null**"
                           + "（说明 C1 不是「夹具永远解析不出」）");
            CheckTrue(tFoe != null && tFoe.Side == "enemy",
                      W40P + "★ C2b 同上，换成 `enemy` ⇒ 非 null（= 出口闸放行的**只有**这两个值）");
        }
    }
}
