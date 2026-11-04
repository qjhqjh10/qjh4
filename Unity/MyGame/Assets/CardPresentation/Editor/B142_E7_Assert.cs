// B142_E7_Assert.cs — 执行代理 `B142-E7`（2026-11-04）· 条目 **`B141-CHOICE-GRAY3`**
//   选卡面板 `Continue Button`：禁用时**三颗**图形件一起换灰化材质（原版 3 颗 vs 我们改前 1 颗）
//
// ============================ 它盯的是哪几条 ============================
//  原版判据（逐件现读，`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1`；
//  场景 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity`）：
//   · `Continue Button` = GO **779**，组件 `3196/1826/4413/4262/4897/5159`：
//     `4413` = 根**透明图** `Image`（α=0）· `4262` = `UnityEngine.UI.Button`（`m_TargetGraphic` → `BG` 的 `Image` 4210）
//     `4897` = `EverguildButton`（`m_TargetGraphic` → 4413，`colorTintGreyOnDisable = 1`，
//     `graphicsToAvoidSetMaterials: []`）· `5159` = `EverguildButtonMaterialModifier`。
//   · 子树里 `EverguildButtonMaterialModifier`（`m_Script` pathID `-9096086197743689236` =
//     `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-9096086197743689236.json` 的 `m_Name`）共 **4 颗**：
//     根透明图 MB **5159**（GO 779）· `BG` MB **5186**（GO 1153）· `ContinueText` MB **4762**（GO 444）· `ContinueButton` MB **4996**（GO 926）。
//   · 🔴 **有效灰化 = 3 颗**：`EverguildButtonMaterialModifier__GetModifiedMaterial.c` 开头两跳
//     `is TextMeshProUGUI` / `is TMP_SubMeshUI` ⇒ **原样返回 `baseMaterial`**
//     （ILSpy 同形：`/d/2/tools/cpp2il/out_cpp2il_dev_ilspy/EverguildButtonMaterialModifier.cs` 两个 `typeof(…)` 早退）
//     ⇒ `ContinueText` 那颗 **空转**，真正会灰的是 根透明图 / `BG` / `ContinueButton`。
//   · 触发链：`EverguildButton__DoStateTransition.c` 末段 → `SwitchMaterial(interactable)` →
//     `EverguildButtonHelper__DoMaterialRefresh.c`（按 list 逐颗 `ToogleGreyScale = !interactable` 再 `SetMaterialDirty`）
//     → `…__GetModifiedMaterial.c` 返回 `EverguildButtonHelper.disabledMaterial`（`Everguild/UI/Greyscale`，**全局一份**）。
//
//  我们这一侧（改后）：`Battle/CardChoicePanel.BuildFeedback` 建完 fx 后
//  `_fxBar.AddGrayTarget(_doneParentHit); _fxBar.AddGrayTarget(_play);`（`_bar` 本来就是 `_target`）
//  ⇒ 禁用时三颗一起换灰化材质；`ContinueText`（`_confirm`）**不挂**（TMP 空转，见上）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 B6：第四颗**没登记**的件必须**不灰** —— 若有人把 `ApplyGray` 改成「场景里见 Image 就灰」，
//      B5（三颗都灰）与 B6（第四颗不灰）**同时**红。
//  🧨 B7：追加件**只灰不变色**（色键仍只打 `_target`）—— 若有人把追加件也塞进色键状态机，
//      「追加件底色仍 == 白」这条当场红。
//  🧨 B10：**已经灰着**时追加的那一颗必须**当场**补上灰化（不是等下一条 `SetDisabled`）——
//      删掉 `AddGrayTarget` 里那条 `if (_grayOn)` 分支就红。
//  🧨 A3/A6：源码守卫 —— `AddGrayTarget(_confirm)` 一旦出现（把 TMP 也挂上）就红；
//      `AddGrayTarget(` 少于 2 处（接线被删/只改注释）也红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  本文件**目前零宿主**（`B142-E7` 的白名单只有 `CardChoicePanel.cs` / `BattleButtonFx.cs` / 本文件）。
//  接线 = 在 `Editor/BattleScene.cs` 那条**方法尾聚合行**（现读 `:47035`）追加：
//      B142_E7_Assert.Run(); pass += B142_E7_Assert.Pass; fail += B142_E7_Assert.Fail;
//  **同一物理行追加 ⇒ 行数零变化**。本件**零前提**（自带临时根 + 收尾 `DestroyImmediate`；不碰场景 / 存档 / 真实 HUD）。
//  判绿红：`grep "^\[B142E7\] 合计"`；逐条按**行首** `[B142E7] ✓` / `[B142E7] ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CardPresentation;

/// <summary>执行代理 `B142_E7`：选卡面板 `Continue Button` 禁用时「三颗图形件一起灰」的落地断言。</summary>
public static class B142_E7_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    const string Prefix = "[B142E7] ";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Prefix + "✓ " + msg); }
        else { _fail++; Debug.LogError(Prefix + "✗ " + msg); }
    }

    // ============================================================ 入口
    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B142_E7_Root");
        try
        {
            A_PanelWiring();
            B_MultiGray(root.transform);
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
        Debug.Log(Prefix + "合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ============================================================ A · 面板接线（源码守卫）
    static void A_PanelWiring()
    {
        string path = Path.Combine(Application.dataPath, "CardPresentation/Battle/CardChoicePanel.cs");
        string src = null;
        try { if (File.Exists(path)) src = File.ReadAllText(path); }
        catch (Exception e) { Check(false, "A0 读不到 `" + path + "`：" + e.Message); return; }
        if (src == null) { Check(false, "A0 读不到 `" + path + "`（文件不存在）"); return; }

        Check(Has(src, "_fxBar.AddGrayTarget(_doneParentHit)"),
              "A1 根透明图（原版 MB 5159）已追加进灰化组");
        Check(Has(src, "_fxBar.AddGrayTarget(_play)"),
              "A2 `ContinueButton` 圆钮（原版 MB 4996）已追加进灰化组");
        Check(!Has(src, "AddGrayTarget(_confirm)"),
              "🧨 A3 `ContinueText`（TMP）**没**挂 —— 原版那颗 modifier 对 `TextMeshProUGUI` 原样返回 `baseMaterial`（挂了也白挂，还会把字体图集换掉）");
        Check(Has(src, "_fxBar.SetDisabled(!on)"),
              "A4 禁用态仍走同一条 `SetDisabled`（灰化由它触发）");
        Check(Has(src, "BattleButtonFx.Tint(_bar, \"Continue Button\""),
              "A5 色键目标仍是 `BG`（`_bar`）—— 原版 `Button.m_TargetGraphic` 只打那一颗");
        Check(CountOf(src, "AddGrayTarget(") >= 2,
              "🧨 A6 源码里 `AddGrayTarget(` 至少 2 处（接线被删 / 只改注释不改代码 ⇒ 红）");
        Check(CountOf(src, "AddGrayTarget(") == 2,
              "A7 恰好 2 处（⛔ 别把 TMP 那颗也补上，有效灰化 = 3 颗）");
        Check(Has(src, "_fxBar.SetDisabled(false)"),
              "A8 重建前先把旧 fx 换上去的灰化材质还回去（否则「开→关→再开」会卡在灰态 —— 见报告 §六）");
    }

    // ============================================================ B · 多目标灰化（行为）
    static void B_MultiGray(Transform parent)
    {
        var pTarget = MakePiece(parent, "BG-like(target)", BattleButtonFx.ContinueNormal);
        var pRoot = MakePiece(parent, "RootHit(extra)", Color.white);
        var pPlay = MakePiece(parent, "ContinueButton(extra)", Color.white);
        var pSpare = MakePiece(parent, "NotRegistered", Color.white);

        var fx = BattleButtonFx.Tint(pTarget, "Continue Button(e7)",
                                     BattleButtonFx.ContinueNormal, BattleButtonFx.ContinueHighlighted,
                                     BattleButtonFx.ContinuePressed, BattleButtonFx.Disabled,
                                     BattleButtonFx.ContinueColorMultiplier);
        Check(fx != null, "B0 反馈对象建出来了");
        if (fx == null) return;
        fx.ApplyInstant();

        Check(fx.GrayTargetCountForTest == 1, "B1 只有 `_target` 时灰化组 = 1 颗（旧行为不变）");

        fx.AddGrayTarget(pRoot);
        fx.AddGrayTarget(pPlay);
        fx.AddGrayTarget(pRoot);      // 幂等
        Check(fx.GrayTargetCountForTest == 3, "B2 追加后 = 3 颗（重复传同一件不重复计数）");
        Check(!fx.GrayedForTest && !fx.IsGrayedForTest(pRoot) && !fx.IsGrayedForTest(pPlay),
              "B3 可交互时谁都不灰");

        fx.SetDisabled(true);
        Check(fx.GrayedForTest != fx.GrayShaderMissingForTest,
              "🧨 B4 禁用后「要么真灰了、要么明确记了没灰成」—— 恰好一条为真（⛔ 不许静默不变灰）");
        if (!fx.GrayedForTest)
            Debug.Log(Prefix + "ℹ 本机取不到 `" + WindowButton.GrayShaderName + "` ⇒ 三颗的**材质**断言按「已明确记录」放行"
                             + "（`GrayShaderMissingForTest = true`；⛔ 这不是通过，是如实记录）");

        Check(!fx.GrayedForTest || (fx.IsGrayedForTest(pTarget) && fx.IsGrayedForTest(pRoot) && fx.IsGrayedForTest(pPlay)),
              "B5 禁用 ⇒ 三颗（`BG` / 根透明图 / `ContinueButton`）**同时**换着灰化材质");
        Check(!fx.IsGrayedForTest(pSpare),
              "🧨 B6 第四颗**没登记**的件不灰（灰化是按 list、不是「场景里所有 Image」）");

        // 🧨 B7：追加件**只灰不变色** —— 色键（`m_Colors`）原版只打 `m_TargetGraphic` 那一颗
        Check(Mathf.Abs(pRoot.Tint.r - 1f) < 1e-6f && Mathf.Abs(pRoot.Tint.a - 1f) < 1e-6f
              && Mathf.Abs(pPlay.Tint.r - 1f) < 1e-6f && Mathf.Abs(pPlay.Tint.a - 1f) < 1e-6f,
              "🧨 B7 追加件的底色仍是白（只换灰化材质、⛔ 不吃 `m_DisabledColor`）");
        Check(Mathf.Abs(pTarget.Tint.r - 1f) > 1e-3f,
              "🧨 B7b `_target` 的色键**变了**（禁用色 × 2 生效）—— 与 B7 同时成立 ⇒ 两条路确实分开");

        // B8：已经灰着时追加 ⇒ 当场补上（原版改的是同一个 bool）
        var pLate = MakePiece(parent, "LateAdded", Color.white);
        fx.AddGrayTarget(pLate);
        Check(fx.GrayTargetCountForTest == 4, "B8 灰着时追加后计数 = 4");
        Check(!fx.GrayedForTest || fx.IsGrayedForTest(pLate),
              "🧨 B9 已经灰着时追加的那一颗**当场**补上灰化（删掉 `AddGrayTarget` 里 `if (_grayOn)` 分支就红）");

        fx.SetDisabled(false);
        Check(!fx.GrayedForTest && !fx.IsGrayedForTest(pRoot) && !fx.IsGrayedForTest(pPlay) && !fx.IsGrayedForTest(pLate),
              "B10 恢复可交互 ⇒ 四颗全部还原（材质还回去）");
        Check(Mathf.Abs(pRoot.Tint.r - 1f) < 1e-6f && Mathf.Abs(pPlay.Tint.r - 1f) < 1e-6f,
              "B11 恢复正常后追加件仍是白（没有残留色键）");

        // B12：单目标回归 —— 不追加时行为与改前逐条相同
        var pOne = MakePiece(parent, "SingleTarget", Color.white);
        var f1 = BattleButtonFx.Tint(pOne, "single");
        if (f1 == null) { Check(false, "B12 单目标夹具没建出来"); return; }
        f1.ApplyInstant();
        f1.SetDisabled(true);
        Check(f1.GrayTargetCountForTest == 1 && f1.GrayedForTest != f1.GrayShaderMissingForTest,
              "B12 单目标（既有 20+ 调用点的形状）行为不变");
        f1.SetDisabled(false);
    }

    static bool Has(string src, string needle)
    {
        return src != null && src.IndexOf(needle, StringComparison.Ordinal) >= 0;
    }

    static int CountOf(string src, string needle)
    {
        if (src == null || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = src.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // ============================================================ 夹具
    static HudPiece MakePiece(Transform parent, string name, Color baseColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var img = go.AddComponent<Image>();
        img.color = baseColor;
        img.raycastTarget = false;
        return HudPiece.Of(rt, img);
    }
}
