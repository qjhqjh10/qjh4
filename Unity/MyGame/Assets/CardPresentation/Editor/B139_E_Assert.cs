// B139_E_Assert.cs — 执行代理 `B139-E`（`B138` 派活索引 W5：`Battle/PopNumber.cs` + `Core/CardFeel.cs`）的断言
//
// ============================ 它盯的是什么 ============================
// 本包 8 条（判据来源 = `报告/B138_审计汇总_问题清单.md` §1/§5/§11，原报告 `B129` §三 · `B131` 色彩 · `B135` 动画）：
//   · `1-01` 伤害飘字多一个 `-` 前缀
//   · `1-02` 飘字每次新建节点，不重播同一颗
//   · `5-02` 伤害飘字缺描边（`_OutlineWidth 0.097` / `_OutlineColor (0,0,0,0.7882353)` / `_FaceDilate 0.105`）
//   · `5-13` 治疗图标 `HealIcon` 未建（α 0.853 · order 100 · scale 0.56046）
//   · `11-01` 伤害血痕缩放的切线装反了（首键 out 0、末键 in 0.06277556）
//   · `11-06` `Impact Light Tween` 三条补间的**数据**必须逐值与 SO 相同（见下方「判据订正」）
//   · `11-07` 血痕静态 z 少 0.05（`−0.05000019`）
//   · `11-09` 单位抖动 ease 1 vs 6（原版无 `SetEase` ⇒ `DOTween.defaultEaseType` = 6 OutQuad）
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `decomp_full/CardDamageCounterController__DamageReceived.c:10-18` —— `SetText(Int32.ToString)`（**无符号**）
//     + `SetActive(true)` + `Animation.Play()`（**重启同一颗**，不叠加）
//   · `decomp_full/CardScript__Heal…:155-204` —— 治疗文本 `"+"+量`
//   · `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5890981294211439552.json`（Damage TMP）
//     → `m_sharedMaterial` = `bundle_fonts_assets_all/Material/Material_-455814643003338116.json` 逐值实读
//     `_OutlineWidth 0.097` · `_OutlineColor (0,0,0,0.7882353)` · `_FaceDilate 0.105`
//   · 同上 `MonoBehaviour_-7677963611639080000.json`（Heal TMP）→ `Material_-5050103597772954521.json`
//     `_OutlineWidth 0` / `_FaceDilate 0` ⇒ **治疗档不描边**
//   · `AnimationClip_-3234148433819516763.json`（`InBattleDamageCounter Variation 1`）——
//     `DamageIcon` 的 `m_ScaleCurves` 段 `[0.11666667, 0.2]`：**首键 in/out 全 0**、**末键 `inSlope = 0.06277556`**
//   · `CardPrefab.prefab`（本工程已导入的同一份 prefab）——
//     `DamageIcon` `Transform` `(0.09400177, −0.104, **−0.05000019**)`、`sortingOrder 1`；
//     `HealIcon` `localScale 0.56046` / `m_Color.a 0.853` / `sortingOrder 100`，自己 `(0,0,0)`（z 在父件 `HealText` = −0.05）
//   · `decomp_full` / VA `0x1805EB7A0`（`CardScript.DoUnitShake`）—— `DOShakePosition` **没有 `SetEase`**
//     ⇒ `DOTweenSettings.asset:31 defaultEaseType = 6`（OutQuad）
//   · `数据/游戏数据/tween/Impact Light Tween.json` —— 四条补间（0.3/10/punch position · 0.5/InQuad/punch rotation
//     · ResetBody 0.25 ×2），运行期表 = `CardPresentation/Resources/UnitTweens.json`
//
// 🔴 **`11-06` 的判据订正（不是「缺实现」）**：`B135` 判它「零消费者 / 没有任何补间在跑」——
//    那是**只看了 `CardFeel` 的常量**。实际那三条补间走**数据驱动**那条链（`UnitTweens.json` → `UnitTweenRuntime`
//    → `UnitTweenTable.Build`，入口 = `BattleDriver` 装的 `WFModuleTween.OnInvoke`），池内 73 个效果引用它。
//    ⛔ 所以修法**不是**再手写一份补间（会播两遍），而是**把它钉在数据上**：本文件 E9 逐值核那四条。
//    ⚠️ 实机画面未逐帧验（要跑一次 Battle 才看得到），已在报告里写明。
//
// ============================ 怎么用 ============================
//   `B139EAssert.Run();`   （静态类 + 无参 `Run()`；宿主由调度台接，本文件不改任何 `Editor/*Scene.cs`）
//   ⚠️ 本文件**不建持久资产**：临时 GameObject 在 `finally` 里 `DestroyImmediate`，
//      并把 `CardTween.Mode` 原样还原（否则会把后面所有宿主的补间一起冻住）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · E2  ← 把伤害档改回 `"-" + Abs(amount)` ⇒ 文本变 `-7` ⇒ 红（而且治疗档那条仍绿 ⇒ 不是「两边一起改」能过的）。
//   · E3  ← 去掉 `key` 复用分支（每下新建）⇒ 两次拿到**不同**的 `Label` ⇒ 红；反过来「不传 key 也复用」⇒ E3b 红。
//   · E4  ← 删掉 `ApplyBannerOutline` ⇒ 描边三项回落到 0 ⇒ 红；给它多贴到治疗档 ⇒ E4b（治疗必须 0）红。
//   · E5  ← 治疗图标不建 ⇒ 红；把 `localScale` 写成裸 `0.56046`（漏桥）⇒ 差 39% ⇒ 红；α 写 1 ⇒ 红。
//   · E6  ← 把切线装回首键（2026-11-03 之前那版）⇒ `IconRampAt(0.5)` 从 0.4947 变 0.5053 ⇒ 两条各红一条。
//   · E7  ← z 写回 0 ⇒ 差 0.0304（世界单位）⇒ 红。
//   · E8  ← 缓动写回 `Ease.Linear`（= 1）⇒ 与 `DOTween.defaultEaseType`（6）不等 ⇒ 红。
//   · E9  ← 表里少一条 / 某格改值（例：把旋转 punch 的 `ease` 改回 6、或 resetbody 的 `targetUnit` 对调）⇒ 红。
using DG.Tweening;
using TMPro;
using UnityEngine;
using CardPresentation;

public static class B139EAssert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B139E";

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

        // A0 = 前提硬检查（⛔ 不静默跳过 —— 字体不可用时飘字根本不建，后面全部等于没跑）
        Check(TmpFont.Available, "A0 **前提**：TMP 字体资产可用（`TmpFont.Available`）");
        if (!TmpFont.Available) return;

        var mode0 = CardTween.Mode;
        var root = new GameObject("B139E_ROOT");
        CardTween.Mode = UpdateType.Manual;
        PopNumber.ResetLive();
        try
        {
            E1_Sign(root);
            E2_Reuse(root);
            E3_Outline(root);
            E4_HealIcon(root);
            E5_IconRamp();
            E6_IconZ(root);
            E7_ShakeEase(root);
            E8_ImpactLightTweenData();
        }
        finally
        {
            PopNumber.ResetLive();
            CardTween.Mode = mode0;
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- 1-01
    static void E1_Sign(GameObject root)
    {
        var d = PopNumber.PopDamage(root.transform, Vector3.zero, 7);
        var h = PopNumber.PopHeal(root.transform, Vector3.zero, 7);
        Check(d != null && h != null, "E1a（前提）伤害 / 治疗两颗飘字都建得出来");
        if (d == null || h == null) return;

        Check(d.Text == "7",
              "E1 ★ 伤害飘字 = **纯整数无符号**（实得「" + d.Text + "」）"
            + "｜判据 `CardDamageCounterController__DamageReceived.c:10-16`（`Int32.ToString()` 直接塞 TMP）"
            + "｜🧨 改回 `\"-\" + Abs(amount)` ⇒ 变「-7」⇒ 红");
        Check(h.Text == "+7",
              "E1b 治疗飘字仍带 `+`（实得「" + h.Text + "」；判据 `CardScript._Heal…:155-204` 的 `\"+\"+量`）"
            + "｜🧨 一刀切成都不带符号 ⇒ 红");
        Check(d.Text != "-7", "E1c（灭自证·反例）伤害档**不是** `-7`");
    }

    // ---------------------------------------------------------------- 1-02
    static void E2_Reuse(GameObject root)
    {
        PopNumber.ResetLive();
        var key = new object();

        var a = PopNumber.PopDamage(root.transform, Vector3.one, 5, false, key);
        CardTween.Advance(0.5f);                                   // 推到「保持」段（α 已到 1）
        var b = PopNumber.PopDamage(root.transform, Vector3.one, 9, false, key);

        Check(a != null && b != null, "E2a（前提）两次带 key 的飘字都建出来了");
        if (a == null || b == null) return;

        Check(ReferenceEquals(a, b),
              "E2 ★ 同一张卡的第二次伤害 = **重播同一颗**（原版每卡只有一颗 `DamageCounter`："
            + "`SetActive(true)` + `SetText` + `Animation.Play()`）"
            + "｜🧨 每次新建 ⇒ 两个不同 `Label` ⇒ 红");
        Check(b.Text == "9", "E2b 重播后文本已换成新的量（实得「" + b.Text + "」）");
        Check(PopNumber.ReusedCount == 1, "E2c 复用计数 = 1（实得 " + PopNumber.ReusedCount + "）");
        Check(b.color.a < 0.05f,
              "E2d 重播 = 从头再放一遍（α 复位到 0，实得 " + b.color.a.ToString("F3") + "）"
            + "｜🧨 只改文本不重播 ⇒ α 还停在 1 ⇒ 红");

        // E2e 灭自证：**不传 key 的路径必须仍是「每次新建」**（否则自检里「同帧连建两颗比字号」那批会失效）
        var x = PopNumber.PopDamage(root.transform, new Vector3(2f, 2f, 0f), 5);
        var y = PopNumber.PopDamage(root.transform, new Vector3(2f, 2f, 0f), 9);
        Check(!ReferenceEquals(x, y), "E2e（灭自证）不传 key ⇒ 仍然每次新建（两颗不同节点）");

        // E2f 治疗档与伤害档**各一张表**（原版是两颗不同的 TMP）
        var hk = new object();
        var h1 = PopNumber.PopHeal(root.transform, Vector3.one, 3, false, hk);
        var h2 = PopNumber.PopHeal(root.transform, Vector3.one, 4, false, hk);
        Check(ReferenceEquals(h1, h2) && h2.Text == "+4",
              "E2f 治疗档独立复用（「" + (h2 != null ? h2.Text : "<null>") + "」）");
    }

    // ---------------------------------------------------------------- 5-02
    static void E3_Outline(GameObject root)
    {
        var d = PopNumber.PopDamage(root.transform, new Vector3(0f, 3f, 0f), 4);
        var h = PopNumber.PopHeal(root.transform, new Vector3(0f, 3f, 0f), 4);
        Check(d != null && h != null, "E3a（前提）两颗飘字都建得出来");
        if (d == null || h == null) return;

        var dTmp = d.GetComponentInChildren<TMP_Text>(true);
        var hTmp = h.GetComponentInChildren<TMP_Text>(true);
        Check(dTmp != null && hTmp != null, "E3b（前提）两颗都拿到了 TMP 组件");
        if (dTmp == null || hTmp == null) return;

        var dm = dTmp.fontMaterial;
        var hm = hTmp.fontMaterial;

        float ow = Has(dm, ShaderUtilities.ID_OutlineWidth) ? dm.GetFloat(ShaderUtilities.ID_OutlineWidth) : 0f;
        float fd = Has(dm, ShaderUtilities.ID_FaceDilate) ? dm.GetFloat(ShaderUtilities.ID_FaceDilate) : 0f;
        Color oc = Has(dm, ShaderUtilities.ID_OutlineColor) ? dm.GetColor(ShaderUtilities.ID_OutlineColor) : Color.clear;

        Near(ow, 0.097f, 1e-4f,
             "E3 ★ 伤害字材质 `_OutlineWidth` = 0.097（`Material_-455814643003338116.json` 实读）"
           + "｜🧨 不贴描边 ⇒ 0 ⇒ 红");
        Near(fd, 0.105f, 1e-4f, "E3b ★ 伤害字材质 `_FaceDilate` = 0.105");
        Check(Mathf.Abs(oc.r) < 1e-4f && Mathf.Abs(oc.g) < 1e-4f && Mathf.Abs(oc.b) < 1e-4f
              && Mathf.Abs(oc.a - 0.7882353f) < 1e-3f,
              "E3c ★ 伤害字材质 `_OutlineColor` = (0,0,0,0.7882353)（实得 " + oc.ToString("F4") + "）");
        Check(dm.IsKeywordEnabled("OUTLINE_ON"), "E3d 描边关键字 `OUTLINE_ON` 已开");

        float how = Has(hm, ShaderUtilities.ID_OutlineWidth) ? hm.GetFloat(ShaderUtilities.ID_OutlineWidth) : 0f;
        Check(how < 1e-4f,
              "E3e（灭自证）**治疗档不描边**：`_OutlineWidth` = 0（实得 " + how.ToString("F4") + "）"
            + "｜判据 = 治疗那颗的材质 `Material_-5050103597772954521.json`（`_OutlineWidth 0` / `_FaceDilate 0`）"
            + "｜🧨 一刀切给两档都贴 ⇒ 红");
    }

    // ---------------------------------------------------------------- 5-13
    static void E4_HealIcon(GameObject root)
    {
        Check(WarpforgeVFX.WarpforgeEffectLibrary.Instance != null,
              "E4a0 **前提**：效果库在（`CardPrefab` 从 `WarpforgeEffectLibrary` 取 ⇒ 要在 `BattleScene.Run` 这类宿主里跑）");
        if (WarpforgeVFX.WarpforgeEffectLibrary.Instance == null) return;

        var h = PopNumber.PopHeal(root.transform, new Vector3(0f, 5f, 0f), 6);
        var d = PopNumber.PopDamage(root.transform, new Vector3(0f, 5f, 0f), 6);
        Check(h != null && d != null, "E4a（前提）治疗 / 伤害两颗都建出来了");
        if (h == null || d == null) return;

        var iconT = h.transform.Find("PopHealIcon");
        Check(iconT != null,
              "E4 ★ 治疗飘字底下建出了 `HealIcon`（原版 `CardPrefab.prefab` 的 `HealIcon`）"
            + "｜🧨 不建 ⇒ 红");
        Check(d.transform.Find("PopHealIcon") == null, "E4a2（灭自证）伤害档**不建**治疗图标");

        if (iconT == null) return;
        var sr = iconT.GetComponent<SpriteRenderer>();
        Check(sr != null, "E4b（前提）`HealIcon` 上有 `SpriteRenderer`");
        if (sr == null) return;

        float bridge = PopNumber.CardLocalToOurs;      // `PopHeal(..., enemy:false)` ⇒ 我方那一档
        Check(sr.sortingOrder == 100,
              "E4c ★ `HealIcon.sortingOrder` = 100（prefab 实读；实得 " + sr.sortingOrder + "）");
        Near(sr.color.a, 0.853f, 1e-3f,
             "E4d ★ `HealIcon` 的 α = 0.853（prefab 实读、**恒定**，没有 clip 驱动它）");
        Near(iconT.localScale.x, 0.56046f * bridge, 1e-3f,
             "E4e ★ `HealIcon.localScale` = 0.56046 × 卡局部→世界桥（" + bridge.ToString("F4") + "）"
           + "｜🧨 写成裸 0.56046（漏桥）⇒ 差 39% ⇒ 红");
        Near(iconT.localPosition.z, -0.05f * bridge, 1e-3f,
             "E4f ★ z = −0.05 × 桥（原版那一跳在父件 `HealText` 的 `localPosition.z` 上）");
    }

    // ---------------------------------------------------------------- 11-01
    static void E5_IconRamp()
    {
        Check(Mathf.Abs(PopNumber.IconScaleRampInTangent) < 1e-6f,
              "E5 ★ 血痕缩放曲线的**首键出切线 = 0**（原版 clip：起步平；实得 "
            + PopNumber.IconScaleRampInTangent.ToString("F6") + "）");

        // 弦斜率 = (0.7 − 0.5763395) ÷ (0.2 − 0.1167) ≈ 1.4845 ⇒ 归一化入切线 = 0.06277556 / 1.4845 ≈ 0.0423
        Near(PopNumber.IconScaleRampEndInTangent, 0.0423f, 5e-4f,
             "E5b ★ **末键入切线** = 0.06277556 ÷ 弦斜率 ≈ 0.0423（原版 clip 实读）"
           + "｜🧨 装回首键（2026-11-03 之前那版）⇒ 末键 0 ⇒ 红");

        float at50 = PopNumber.IconRampAt(0.5f);
        Check(Mathf.Abs(at50 - 0.4947f) < 2e-3f && Mathf.Abs(at50 - 0.5053f) > 5e-3f,
              "E5c ★ 形状判别式：`u = 0.5` 处 = **0.4947**（非零切线在末键 ⇒ 前半段慢）；"
            + "切线装反是 0.5053（实得 " + at50.ToString("F5") + "）"
            + "｜🧨 两端切线对调 ⇒ 落到 0.5053 ⇒ 红");
    }

    // ---------------------------------------------------------------- 11-07
    static void E6_IconZ(GameObject root)
    {
        Check(WarpforgeVFX.WarpforgeEffectLibrary.Instance != null,
              "E6a0 **前提**：效果库在（`DamageIcon` 从 `WarpforgeEffectLibrary` 的 `CardPrefab` 取）");
        if (WarpforgeVFX.WarpforgeEffectLibrary.Instance == null) return;

        var d = PopNumber.PopDamage(root.transform, new Vector3(0f, 7f, 0f), 3);
        Check(d != null, "E6a（前提）伤害飘字建出来了");
        if (d == null) return;
        var t = d.transform.Find("PopDamageIcon");
        Check(t != null, "E6b（前提）血痕节点在（`CardPrefab` 取得到 `DamageIcon`）");
        if (t == null) return;

        float bridge = PopNumber.CardLocalToOurs;
        float want = -0.05000019f * bridge;
        Near(t.localPosition.z, want, 2e-3f,
             "E6 ★ 血痕静态 z = **−0.05000019 × 桥**（prefab `Transform_-8063575699247555648` 实读）"
           + "｜🧨 写回 0 ⇒ 差 " + Mathf.Abs(want).ToString("F4") + " 世界单位 ⇒ 红");
        Check(Mathf.Abs(t.localPosition.z) > 1e-3f, "E6b（灭自证）z **不是 0**");
    }

    // ---------------------------------------------------------------- 11-09
    static void E7_ShakeEase(GameObject root)
    {
        Check(CardFeel.UnitShakeEase == Ease.OutQuad,
              "E7 ★ 单位抖动缓动 = **OutQuad**（原版 `DoUnitShake` **不写 `SetEase`** ⇒ 吃默认，"
            + "本工程 `Resources/DOTweenSettings.asset:31` `defaultEaseType = 6`；实得 "
            + (int)CardFeel.UnitShakeEase + "）"
            + "｜🧨 写回 `Ease.Linear`(1) ⇒ 红");
        if (DOTween.defaultEaseType != Ease.OutQuad)
            Debug.LogWarning(Tag + " ⚠ `DOTween.defaultEaseType` 现读 = " + (int)DOTween.defaultEaseType
                           + "（本工程该是 6 OutQuad）—— 这条**不影响 E7**（判据盯的是原版资产值），但要查一下 "
                           + "`Resources/DOTweenSettings.asset` 是不是被改了");

        var go = new GameObject("B139E_shake");
        CardView card = null;
        try
        {
            card = CardView.Create(go.transform, CardData.Simple("B139E", 1, 1, 1), "B139E_card");
            Check(card != null, "E7a（前提）抖动夹具卡建出来了");
            if (card == null) return;

            var tw = CardFeel.ShakeUnit(card, 2, 0.3f);
            Check(tw != null, "E7b（前提）`ShakeUnit` 返回了补间");
            Ease got = Ease.Linear;
            bool ok = tw != null && TryEaseOf(tw, out got);
            if (ok)
            {
                Check(got == CardFeel.UnitShakeEase,
                      "E7c ★ `ShakeUnit` 那条补间实际吃到的缓动 = " + (int)got
                    + "（期望 " + (int)CardFeel.UnitShakeEase + "）"
                    + "｜🧨 直接传 `Ease.Linear` ⇒ 实得 1 ⇒ 红");
            }
            else
            {
                Debug.LogWarning(Tag + " ⚠ E7c 读不到那条补间的 ease 字段（DOTween 版本差异）—— "
                               + "这一格**没验到**，别当通过（E7/E7b 仍然有效）");
            }
            if (tw != null) tw.Kill(false);
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ---------------------------------------------------------------- 11-06
    static void E8_ImpactLightTweenData()
    {
        const string name = "Impact Light Tween";
        Check(UnitTweenTable.Loaded,
              "E8a **前提**：`Resources/UnitTweens.json` 载入正常（`UnitTweenTable.Loaded`；"
            + "错误 = " + (UnitTweenTable.LastError ?? "<无>") + "）");
        var e = UnitTweenTable.Get(name);
        Check(e != null && e.tweens != null && e.tweens.Length == 4,
              "E8 ★ 运行期补间表里有 `" + name + "` 的**四条**（原版 SO `数据/游戏数据/tween/Impact Light Tween.json`；实得 "
            + (e != null && e.tweens != null ? e.tweens.Length.ToString() : "<无表项>") + "）"
            + "｜🧨 删掉/漏搬一条 ⇒ 红");
        if (e == null || e.tweens == null || e.tweens.Length != 4) return;

        var s0 = e.tweens[0];
        Check(s0.kind == "punch" && Near3(s0.duration, 0.3f) && s0.punchType == 1
              && s0.targetUnit == 10 && s0.awayFromUnit == 20 && s0.vibratto == 10
              && Mathf.Abs(s0.elasticity) < 1e-6f && s0.ease == 6 && s0.appendType == 0
              && s0.vec != null && s0.vec.Length >= 3
              && Near3(s0.vec[0], 0f) && Near3(s0.vec[1], 0f) && Near3(s0.vec[2], -0.1f),
              "E8b ★ #0 位置 punch = 0.3 s / (0,0,−0.1) / vibrato 10 / elasticity 0 / ease 6 / targetUnit 10 / away 20");

        var s1 = e.tweens[1];
        Check(s1.kind == "punch" && Near3(s1.duration, 0.5f) && s1.punchType == 2
              && s1.targetUnit == 30 && s1.vibratto == 7
              && Near3(s1.elasticity, 1f) && s1.ease == 5 && s1.appendType == 5
              && s1.vec != null && s1.vec.Length >= 3
              && Near3(s1.vec[0], -3f) && Near3(s1.vec[1], -3f) && Near3(s1.vec[2], 0f),
              "E8c ★ #1 旋转 punch = 0.5 s / (−3,−3,0) / **ease 5 InQuad** / vibrato 7 / elasticity 1 / Join(appendType 5)"
            + "｜🧨 把 ease 改回 6 ⇒ 红");

        var s2 = e.tweens[2];
        Check(s2.kind == "resetbody" && Near3(s2.duration, 0.25f) && s2.ease == 6
              && s2.targetUnit == 20 && s2.appendType == 0
              && s2.resetPosition != 0 && s2.resetRotation != 0 && s2.resetScale != 0,
              "E8d ★ #2 ResetBody = 0.25 s / ease 6 / targetUnit 20 / Append(0) / 三复位全开");

        var s3 = e.tweens[3];
        Check(s3.kind == "resetbody" && Near3(s3.duration, 0.25f) && s3.ease == 6
              && s3.targetUnit == 10 && s3.appendType == 5
              && s3.resetPosition != 0 && s3.resetRotation != 0 && s3.resetScale != 0,
              "E8e ★ #3 ResetBody = 0.25 s / ease 6 / targetUnit 10 / Join(5) / 三复位全开"
            + "｜🧨 #2 与 #3 的 `targetUnit` 对调 ⇒ 红");

        Check(UnitTweenTable.WaitOf(name) == 0,
              "E8f `waitAnimation` = 0（原版 `Impact Light Tween.json` 的 `waitAnimation: 0` ⇒ fire-and-forget）");
    }

    static bool Near3(float a, float b) { return Mathf.Abs(a - b) < 1e-3f; }

    static bool Has(Material m, int id) { return m != null && m.HasProperty(id); }

    /// <summary>把补间实例上那个 `Ease` 字段读出来（DOTween 没公开 getter ⇒ 反射找「类型是 `Ease` 的实例字段」）。
    /// 找不到 ⇒ 返回 false（调用方要**出声**，⛔ 不许当通过）。</summary>
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
