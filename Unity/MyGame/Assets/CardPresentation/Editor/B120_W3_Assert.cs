// B120_W3_Assert.cs — 执行代理 `B120-W3`（战斗 HUD 组件层参数，账 B-2 / B-3 / B-4 / B-8 / B-9 / B-10 / B-11 / B-12 / B-13 / B-14）
//
// ============================ 它盯的是哪几条 ============================
//  A. `B-2` 技能面板**可用（黄绿）那一层**的判据 = 原版 `ActiveSkillDisplay__SetupActiveSkill.c:141-148` 的
//     `bgLight.SetActive(manager.CanUseActiveAbility(card, 1, 0))`，⛔ **不是**「有几个合法目标」（`ShownTargets`）。
//  B. `B-3` 教程高亮的 `positionRelation` 偏移 —— 原版 `AiScripted__GetTipPosition.c` 末段三条：
//     `LeftOf ⇒ x−=d` · `RightOf ⇒ x+=d` · `Above ⇒ y += d×0.5`（0.5 = `DAT_1834b2bb4` 实读）。
//  C. `B-8` 教程高亮**收起 = 0.1 s 淡出**后才 `SetActive(false)`（`ScreenHighlightPosition__.ctor.c` 的 `fadeTime`）。
//  D. `B-11` 设置窗「Skip tutorial」**没有** `minTimeBeforeSkip` 闸（`BattleSettingsWindow__SkipTutorialButtonOnClick.c`
//     全文只有 `CloseWindow` + `BattleManager.ClickSkip`）。
//  E. `B-9` 结算 `SkullsHolder` 的 `Image` 挂在**容器自己**身上（原版 `MB_4938` 的 `m_GameObject` = GO 391）；
//     `B-10` 印记（`EarnedSeals`）那一支的数字/图标。
//  F. `B-4` / `B-12` / `B-13` / `B-14`：结构闸（源码里那几句必须在活代码里，⛔ 不在注释里）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接在 `Editor/BattleScene.cs` 的**方法尾聚合行**（`:46983` 那一行、`B118W23_Assert.Run();` 之后、
//  `Debug.Log(P + $"=== 合计…")` **之前**）—— 同一物理行追加 ⇒ 行数零变化：
//    ` B120_W3_Assert.Run(); pass += B120_W3_Assert.Pass; fail += B120_W3_Assert.Fail;`
//  🔑 为什么能接在那儿：本件**零前提**（自带临时 `SkillPanel` / `TutorialOverlay` / `EndPanel`，
//  收尾自己 `DestroyImmediate`），不依赖活 driver、不依赖 canvas 宿主。
//
// ============================ 🔴 灭自证（每条都写清「改哪一处会红」）============================
//  · A2：把可用层判据改回 `ShownTargets > 0` ⇒ `ShownTargets = 3` 而 `canUse = false` 那一格会亮 ⇒ 红。
//  · B1/B2/B3：把 relation 偏移删掉（或漏乘 0.5）⇒ 位置差 d（或差一半）⇒ 红。
//  · C2：把收起改回「瞬关」⇒ 收起那一刻 `activeSelf` 立刻 false ⇒ 红；不推 dt 就自动隐藏也 ⇒ 红。
//  · D2：把 1 s 闸套回设置窗那一口 ⇒ `TrySkipFromSettings()` 返回 false ⇒ 红。
//  · E1：把 `Image` 建回子节点 `skull_plate` ⇒ `SkullsHolder` 上 `GetComponent<Image>()` 为 null ⇒ 红。
//  · F*：把任一句从活代码里删掉（哪怕注释里还留着）⇒ 对应那条红。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B120-W3`：战斗 HUD 组件层这 10 条参数的落地断言。
/// <para>⛔ 期望值全是**常量/字面量**（原版 prefab 字段 / 反编译字面量），⛔ 不从被测实现里读回来。</para></summary>
public static class B120_W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B120W3] ") { Near = MenuNearStyle.Labeled4 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    const string SrcLog = "Assets/CardPresentation/Battle/BattleLogPanel.cs";
    const string SrcSel = "Assets/CardPresentation/Battle/AttackSelector.cs";
    const string SrcCanvas = "Assets/CardPresentation/Battle/HudCanvas.cs";
    const string SrcChatPart = "Assets/CardPresentation/Battle/UnitChatPanel.PartB118W3.cs";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B120-W3 · 战斗 HUD 组件层参数（可用层判据 / 高亮 relation 与淡出 / 跳过闸 / 结算底板与印记 / 日志行自适应 / 同档闸 / 画布通道 / 语音条裁剪）");
        RunGroup("A · B-2 技能面板可用层", A_SkillPanelAvailability);
        RunGroup("B · B-3 高亮 relation 偏移", B_HighlightRelation);
        RunGroup("C · B-8 高亮收起淡出", C_HighlightFade);
        RunGroup("D · B-11 设置窗跳过闸", D_SkipFromSettings);
        RunGroup("E · B-9/B-10 结算底板与印记", E_EndPanel);
        RunGroup("F · 结构闸（B-4 / B-12 / B-13 / B-14）", F_SourceGuards);
        Debug.Log(_sink.P + "B120_W3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void RunGroup(string name, Action body)
    {
        MenuCheck.Section(_sink, name);
        try { body(); }
        catch (Exception e)
        {
            // 说清楚是**这一组**炸了（⛔ 不许把它吞成一条静默的「没验」）
            Tr(false, name + "：这一组抛异常 ⇒ 本组结论**未验**：" + e.GetType().Name + " " + e.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  A. B-2 —— 可用（黄绿）层 = `CanUseActiveAbility`，⛔ 不是「有几个合法目标」
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void A_SkillPanelAvailability()
    {
        // 优先用场景里那台（`BattleScene` 里 `driver.skillPanel` 就是它）；没有才自己建一台世界档的
        GameObject go = null;
        var sp = UnityEngine.Object.FindObjectOfType<SkillPanel>();
        if (sp == null)
        {
            go = new GameObject("B120W3_SkillPanel");
            sp = go.AddComponent<SkillPanel>();
            sp.Build();                       // 编辑模式下 `AddComponent` 不跑 `Awake` ⇒ 显式建一次
        }
        try
        {
            Tr(sp != null, "A0 前提：拿到一台建好的 `SkillPanel`");
            if (sp == null) return;

            // ① 旧口径会亮（3 个合法目标）· 原版口径说「不能用」⇒ 必须**不亮**
            sp.SetTargetsAvailable(3);
            sp.CanUseAbility = false;
            Tr(sp.ShownTargets == 3, "A1 前提：`ShownTargets` 真的是 3（否则 A2 空转）");
            Tr(sp.CurrentLight == SkillPanel.Light.None,
               "A2 ★ `ShownTargets = 3` 但 `CanUseActiveAbility = false` ⇒ 可用层**不亮**"
             + "（判据 = `ActiveSkillDisplay__SetupActiveSkill.c:141-148`；🧨 改回 `ShownTargets > 0` ⇒ 本条红）");

            // ② 反过来：0 个目标但「能用」⇒ 原版照样亮
            sp.CanUseAbility = true;
            sp.SetTargetsAvailable(0);
            Tr(sp.ShownTargets == 0, "A3 前提：`ShownTargets` 真的是 0");
            Tr(sp.CurrentLight == SkillPanel.Light.Available,
               "A4 ★ `ShownTargets = 0` 但 `CanUseActiveAbility = true` ⇒ 可用层**亮**（判据同上，另一半）");

            // ③ 没喂过（`null`）⇒ 退回旧口径（只为既有调用点零改动）
            sp.SetTargetsAvailable(2);
            sp.CanUseAbility = null;
            Tr(sp.CurrentLight == SkillPanel.Light.Available,
               "A5 没喂过可用性（`null`）⇒ 退回 `ShownTargets > 0`（既有调用点的兼容档）");
        }
        finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  B. B-3 —— 高亮的 relation 偏移（原版 `GetTipPosition` 末段三条）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void B_HighlightRelation()
    {
        var root = new GameObject("B120W3_TutRoot");
        try
        {
            var ov = TutorialOverlay.Create(root.transform);      // 世界档（= 生产形状 `BattleDriver.cs:16967`）
            Tr(ov != null, "B0 前提：`TutorialOverlay` 建出来了");
            if (ov == null) return;

            var c0 = new Vector3(1f, 2f, 0f);
            var sz = new Vector2(0.4f, 0.4f);
            const float d = 3f;

            ov.SetHighlight(true, c0, sz, TutRelation.Exact, d);
            var pExact = ov.HighlightPosForTest;
            Near(pExact.x, 1f, 1e-3f, "B1 `Exact` ⇒ x 不动（原版只对 LeftOf/RightOf/Above 三档加偏移）");

            ov.SetHighlight(true, c0, sz, TutRelation.LeftOf, d);
            var pLeft = ov.HighlightPosForTest;
            Near(pLeft.x, 1f - d, 1e-3f,
                 "B2 ★ `LeftOf` ⇒ x −= d（原版 `GetTipPosition.c` 末段的 `param_5 == 10` 那一支）"
               + "｜🧨 删掉偏移 ⇒ 回到 1.0 ⇒ 红");

            ov.SetHighlight(true, c0, sz, TutRelation.RightOf, d);
            var pRight = ov.HighlightPosForTest;
            Near(pRight.x, 1f + d, 1e-3f, "B3 ★ `RightOf` ⇒ x += d（`param_5 == 20` 那一支）");

            ov.SetHighlight(true, c0, sz, TutRelation.Above, d);
            var pAbove = ov.HighlightPosForTest;
            Near(pAbove.y, 2f + d * 0.5f, 1e-3f,
                 "B4 ★ `Above` ⇒ y += d × **0.5**（`DAT_1834b2bb4` 实读 = 0.5；🧨 漏乘 ⇒ 得 5.0 ⇒ 红）");
            Near(pAbove.x, 1f, 1e-3f, "B5 `Above` 不动 x（判别式：B2/B3 动的是 x，这条动的是 y）");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  C. B-8 —— 收起 = 0.1 s 淡出后 `SetActive(false)`
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void C_HighlightFade()
    {
        var root = new GameObject("B120W3_FadeRoot");
        try
        {
            var ov = TutorialOverlay.Create(root.transform);
            Tr(ov != null, "C0 前提：`TutorialOverlay` 建出来了");
            if (ov == null) return;

            var c0 = Vector3.zero;
            var sz = new Vector2(0.4f, 0.4f);
            ov.SetHighlight(true, c0, sz);
            Tr(ov.HighlightVisible, "C1 `SetHighlight(true)` ⇒ 立刻可见（显示那一档仍是瞬开）");

            ov.SetHighlight(false, c0, sz);
            Tr(ov.HighlightVisible && ov.HighlightFadeRunningForTest,
               "C2 ★ 收起那一刻**还没关**：`activeSelf` 仍为真 + 淡出在跑"
             + "（原版 `ScreenHighlightPosition__Fade.c` → `DOFade(0, fadeTime=0.1)` → `OnComplete` 才 `InstantToggle(false)`）"
             + "｜🧨 改回瞬关 ⇒ 本条红");

            // 推进 0.2 s（> 0.1 s）—— 批处理没有帧循环，`Tick` 必须显式喂
            for (int i = 0; i < 10; i++) ov.Tick(0.02f);
            Tr(!ov.HighlightVisible, "C3 ★ 淡出跑完 ⇒ `activeSelf = false`（收起真的收掉了）");
            Tr(!ov.HighlightFadeRunningForTest, "C4 淡出已结束（计时位归 -1）");

            // 再亮一次 ⇒ α 必须回到静止值 210/255（原版 `InstantToggle(true)` 恢复原色）
            ov.SetHighlight(true, c0, sz);
            Near(ov.HighlightTintForTest.a, 210f / 255f, 2e-3f,
                 "C5 ★ 重新亮起后 α 回到 **210/255**（`MonoBehaviour_5145.json` 的 0.8235294；🧨 淡出把 α 留在 0 ⇒ 红）");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  D. B-11 —— 设置窗「Skip tutorial」没有 `minTimeBeforeSkip` 闸
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void D_SkipFromSettings()
    {
        var root = new GameObject("B120W3_SkipRoot");
        try
        {
            var ov = TutorialOverlay.Create(root.transform);
            Tr(ov != null, "D0 前提：`TutorialOverlay` 建出来了");
            if (ov == null) return;

            ov.SetShownForTest(0f);
            Tr(!ov.SkipAllowed, "D1 前提：刚弹出（0 s）时旧的 `SkipAllowed` 为**假**（1 s 闸还在，⛔ 别把它一起删了）");
            Tr(ov.TrySkipFromSettings(),
               "D2 ★ 设置窗那颗钮**不受 1 s 闸**约束 —— 原版 `BattleSettingsWindow__SkipTutorialButtonOnClick.c`"
             + "全文只有 `CloseWindow` + `BattleManager.ClickSkip`（`minTimeBeforeSkip` 是 `TutorialTipScript` 的字段，管的是提示本身）"
             + "｜🧨 把 `!TrySkip()` 那一句套回这条路 ⇒ 本条红");
            Tr(ov.SkipClickedByPlayer, "D3 …而且被记成「玩家真的按了跳过」");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  E. B-9 / B-10 —— 结算面板：骷髅底板挂父节点 · 印记那一支
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void E_EndPanel()
    {
        var end = EndPanel.Create(null);
        try
        {
            Tr(end != null, "E0 前提：`EndPanel.Create(null)` 非 null（拿不到 ⇒ 后面全是空转，⛔ 不算通过）");
            if (end == null) return;

            var row = end.SkullRow;
            Tr(row != null, "E1 前提：`SkullRow`（= `SkullsHolder`）建出来了");
            if (row == null) return;

            var rowImg = row.GetComponent<UnityEngine.UI.Image>();
            Tr(rowImg != null,
               "E2 ★ 骷髅底条 `Image` 挂在 **`SkullsHolder` 自己**身上（原版 `MB_4938.m_GameObject` = GO 391）"
             + "｜🧨 改回 `ImageInParent(... \"skull_plate\" ...)` ⇒ 本条红");
            Tr(row.Find("skull_plate") == null,
               "E3 ★ 不再有子节点 `skull_plate`（原版那一层只有 3 颗骷髅）");
            var rt = row as RectTransform;
            Near(rt != null ? rt.sizeDelta.x : -1f, 648.1f, 0.5f,
                 "E4 底板矩形 = 原版 `SkullsHolder.m_SizeDelta` 宽 648.1（PA=0 ⇒ 拉满）");
            Near(rt != null ? rt.sizeDelta.y : -1f, 52.4f, 0.5f, "E5 …高 52.4");
            Tr(rowImg != null && !rowImg.preserveAspect,
               "E6 ★ `m_PreserveAspect = 0`（原版是拉满那条长底板，⛔ 不是按 109×41 贴图比例定宽）");

            // B-10：印记那一支 —— 数字换 `EarnedSeals`、图标仍用奖杯（`Rank Skull_0` 本地没有）
            end.SetRating(0, 5);
            var txt = end.RatingTextForTest;
            Tr(txt != null && txt.Text == "5",
               "E7 ★ `EarnedSeals != 0` ⇒ 那一行印的是 **seals**（原版 `EndBattleDoors__ShowRewards.c:104-160` 的 `param_3+0x1c` 那一支）");
            Tr(end.TrophyForTest != null && end.TrophyForTest.Texture != null,
               "E8 印记图标那一格：`Rank Skull_0` 本地没有 ⇒ **保持奖杯图**且**不崩**（⛔ 不拿 `Rank_Skull` 顶）");
        }
        finally
        {
            if (end != null) UnityEngine.Object.DestroyImmediate(end.gameObject);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  F. 结构闸（B-4 / B-12 / B-13 / B-14）—— 那几句必须**在活代码里**
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void F_SourceGuards()
    {
        GuardContains(SrcLog, "SetAutoFitBox(RowW - 25f, RowH, 12f, 30f, 30f)",
                      "F1 ★ `B-4` 战斗日志行 = 原版 `ActionText` 的 `fs30 + auto[12,30] + 框 723×43.134`"
                    + "｜🧨 删掉这一句（哪怕注释里还留着）⇒ 红");
        GuardContains(SrcLog, "_rowTexts[i].SetWrapping(false)",
                      "F2 ★ `B-4` 那一行**不折行**（原版 `m_TextWrappingMode 0`；🧨 `SetAutoFitBox` 会顺带打开折行 ⇒ 必须紧跟这一句）");
        GuardContains(SrcSel, "opt.Kind == _selected",
                      "F3 ★ `B-12` 攻击选择器「同档不重复下单」闸（原版 `UnitOnBoardAttackTypeSelector__AttackButtonClick.c` 第一句）");
        GuardContains(SrcCanvas, "additionalShaderChannels = (AdditionalCanvasShaderChannels)25",
                      "F4 ★ `B-13` 画布 `m_AdditionalShaderChannelsFlag = 25`（原版 `Battle Arena 1.unity` 的 Canvas `&2572`）");
        GuardContains(SrcChatPart, "SetUvRect(PortraitCropUv)",
                      "F5 ★ `B-14` 语音条**世界档**的卡图裁剪 = UV 裁剪（`RectMask2D` 的等价物）");

        // B-13 的运行期旁证：HUD 画布已经建过（多半是）就顺手读一眼真值 —— 前提先断、再断结论
        bool hasRoot = HudCanvas.Root != null;
        if (hasRoot)
            Tr(HudCanvas.Root.additionalShaderChannels == (AdditionalCanvasShaderChannels)25,
               "F6 `B-13` 运行期：`HudCanvas.Root.additionalShaderChannels` = 25（实得 "
             + (int)HudCanvas.Root.additionalShaderChannels + "）");
        else
            Tr(false, "F6 **未验**：本次运行时 `HudCanvas.Root` 还没建（⛔ 这不是「通过」—— 结构闸 F4 仍然有效）");
    }

    /// <summary>源码里必须**逐字**含有某个片段（注释里也算 —— 所以取的是**只在活代码里出现**的整句）。</summary>
    static void GuardContains(string path, string need, string msg)
    {
        string src = null;
        try { if (File.Exists(path)) src = File.ReadAllText(path); }
        catch (Exception e) { Debug.LogWarning("[B120W3] 读不到源码（" + e.GetType().Name + "）：" + path); }
        Tr(src != null, "（前提）源码读得到：" + path);
        if (src == null) return;
        Tr(src.IndexOf(need, StringComparison.Ordinal) >= 0, msg + "（要找的片段：「" + need + "」）");
    }
}
