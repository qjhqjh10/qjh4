// B117_G10_09_Assert.cs — 执行代理 `B117-G10-09`（战斗域第 9 部分「主动技能面板」）的断言宿主
//
// 覆盖这一批**在 `Battle/SkillPanel.cs` 里改掉的四件事**（判据全部来自原版 prefab 原件 + 反编译，逐组写在头里）：
//   A. 前提：场景里有一台建好的 `SkillPanel`，而且它的色片件 `Lights` 在。
//   B. **三层色片按原版【叠放】，⛔ 不是「三选一」**（判据 → `SkillPanel.ComposeLightTint` 的文件尾大段）：
//      原版 `Lights` 下三颗 Image（`LightActing`/`LightAvailable`/`LightPressed`）**矩形完全重叠**，
//      且**没有任何一处代码**在点亮一层时关掉别的层 ⇒ 可以同时可见、按 `SrcAlpha OneMinusSrcAlpha` 叠加。
//      旧实现按 `CurrentLight` 只画最上面那一层 ⇒ 「可用 + 按下」那一下少画一整层。
//   C. **acting 白层的淡入起点 = 0**（⛔ 不是序列化值 0.392）：
//      `ActiveSkillDisplay__OnTouchUpAsButton.c:44` 先 `HideActingLight()`（`SetSpriteAlpha(actingLight, 0)`）
//      再 `DOFade(1, 0.4)`；`SetupActiveSkill.c:141-148` 收尾也调它 ⇒ 面板一弹出 acting 层恒为 0。
//   D. **`Hide()`（= 原版 `FadeOut`）当场熄两层**：`FadeOut.c:40-56` → `pressingLight.SetActive(false)`；
//      `FadePower.c:20` / `SetAlpha.c:11` → `HideActingLight()`（杀补间 + α 0）。
//   E. **`TargetsAvailableText` 画在 `Lights` 之上**：原版 `ActiveSkillDesc`(RT 2911) 的 `m_Children`
//      = `HideAbilityButton` → `AbilityContainer` → `Lights` → **`TargetsAvailableText`**（最后一个 = 最上面）。
//   F. **费用图标那块不画**：原版 `CostIcon`（13/13 战场 `m_IsActive: 0`）+ `costSprite`/`m_Sprite` 双 null
//      + 全量反编译里 `costSprite` **零写点** ⇒ 那一格从来不可见（我们原来画的能量宝石是自己加的）。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_09_Assert.Run -logFile -
//
// 判绿红：`grep "^B117G1009"` 的**合计行**；逐条按**行首** `B117G1009 ✓` / `B117G1009 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求（给调度台挂进 `Editor/BattleScene.cs` 时看）：
//   · 只要**一台建好的 `SkillPanel`**（`SkillPanel.Build()` 跑过：`Awake` 或驱动里 `UseHost` 之后都行）；
//     **不需要** `BattleDriver` / 棋盘 / 存档。找不到面板 ⇒ 一条**红**（⛔ 不静默跳过）。
//   · 面板在**画布档 / 世界档**都能跑（E 那一组按档走两条等价判据，报告里会印出走了哪一条）。
//   · 收尾把 `Visible` / `ShownName` / `ShownTargets` **恢复原状**；`CardTween.Mode` / `LinkEnabled` 同样还原。
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B117_G10_09_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B117G1009";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5")
                  + "；实得 " + got.ToString("F5") + "）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  原版 prefab 的**字面量**（⛔ 别读 `SkillPanel.Light*Color` 反推 —— 那就是拿实现证明实现）
    //    `_tmp_view/orig_ui_proj/Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity`
    //    Image `4313`(LightActing) / `4521`(LightAvailable) / `4225`(LightPressed) 的 `m_Color`
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    const float OrigActingA = 0.392f;          // Image 4313: {r:1, g:1, b:1, a:0.392}
    const float OrigAvailableR = 0.9254298f;   // Image 4521: {r:0.9254298, g:1, b:0, a:0.392}
    const float OrigPressedB = 1f;             // Image 4225: {r:0.006026745, g:0, b:1, a:0.392}

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            // 批处理没有帧循环 ⇒ 只有 `CardTween.Advance(dt)` 推得动补间（同 `HandAssertE8` / `B117F11_Assert`）。
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;

            // ⚠️ 每组各包一层：一组炸掉**不许**把后面几组一起拖没（否则报告里只剩一条异常、别的全是「没验」）。
            Group("A·前提", A_Prepare);
            Group("B·三层叠放", B_Stack);
            Group("C·acting 起点", C_ActingFromZero);
            Group("D·收起当场熄两层", D_HideKillsLights);
            Group("E·目标数字在色片之上", E_TargetsAboveLights);
            Group("F·费用图标不画", F_NoCostIcon);
            Group("G·复原", G_Restore);
        }
        finally
        {
            CardTween.LinkEnabled = keepLink;
            CardTween.Mode = keepMode;
            Debug.Log(Tag + " 合计：通过 " + _pass + " · 失败 " + _fail);
        }
    }

    // ── 组间状态（`_sp` 由 A 组填）────────────────────────────────────────────────────────────
    static SkillPanel _sp;
    static bool _wasVisible;
    static string _wasName;
    static int _wasTargets;
    static float _wasAlpha;

    /// <summary>一个**保证落在面板命中框内**的世界点（喂 `SetPointer` 用）。
    /// <para>⚠️ 别直接用 `sp.transform.position`：画布档下 `Contains` 量的是**子画布局部**的
    /// `PanelCvRect`，而那个点与「世界空间那层壳」只在排过版之后才对得上（批处理里画布常常没排版）
    /// ⇒ 这里按**被测实现自己的那条反算**取矩形中心的世界点（`_cv.TransformPoint(rect.center)`），
    /// 数值上必然落在框里（`Contains` 做的是同一个 `InverseTransformPoint`）。</para></summary>
    static Vector3 InsidePoint(SkillPanel sp)
    {
        if (sp.UguiMode && sp.CanvasRect != null)
        {
            var c = sp.PanelCvCenter;
            return sp.CanvasRect.TransformPoint(new Vector3(c.x, c.y, 0f));
        }
        return sp.transform.position;
    }

    static void Group(string name, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            // 说清楚是**这一组**炸了（⛔ 不许把它吞成一条静默的「没验」）
            Check(false, name + "：这一组抛异常 ⇒ 本组结论**未验**：" + e.GetType().Name + " " + e.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  A. 前提（硬检查 —— 先断前提、再断结论；⛔ 不许 `if (前提) Check(结论)` 那种静默跳过）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void A_Prepare()
    {
        _sp = Object.FindObjectOfType<SkillPanel>();
        Check(_sp != null,
              "前提：场景里有一台 `SkillPanel`"
            + " —— 找不到 ⇒ 后面各组**全部未验**（⛔ 这不是「通过」）");
        if (_sp == null) return;

        _wasVisible = _sp.Visible;
        _wasName = _sp.ShownName;
        _wasTargets = _sp.ShownTargets;
        _wasAlpha = _sp.Alpha;

        Check(_sp.LightForTest != null,
              "前提：色片那件 `Lights` 建出来了（`LightForTest` 非空）");

        _sp.Show("B117-G10-09", (EffectSpec)null, 2);
        CardTween.Advance(0.4f);            // 淡入 0.2 s 走完 ⇒ Alpha = 1

        Check(_sp.Visible, "前提：面板弹得出来（`Visible`）");
        CheckNear(_sp.Alpha, 1f, 1e-3f, "前提：淡入走完 ⇒ `Alpha` = 1");
        Check(_sp.ShownTargets == 2, "前提：面板记的可选目标数 = 2（= 铺 `LightAvailable` 的那一档）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  B. 三层色片叠放（核心 · 灭自证：旧「三选一」实现下 B2/B3 必红）
    //     按下时的期望 = `LightPressed` 叠在 `LightAvailable` 之上：
    //       A = 1 − (1−0.392)² = 0.630336
    //       （两层 α 都是 prefab 里的 0.392 ⇒ 这个数只由原版字面量决定，与我们的常量无关）
    //     旧实现（只画最上面那层）⇒ A = 0.392、g = 0 ⇒ B2/B3 都红。
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void B_Stack()
    {
        if (_sp == null || _sp.LightForTest == null) { Check(false, "B：前提不成立（见 A 组）⇒ 未验"); return; }

        _sp.SetPointer(InsidePoint(_sp), true);
        Check(_sp.PressedForTest,
              "B1 前提：指针按在面板上 ⇒ 蓝层（`_pressed`）真的点亮了 —— ⛔ 没点亮的活后面两条无意义");

        var t = _sp.LightForTest.Tint;
        float wantA = 1f - (1f - OrigActingA) * (1f - OrigActingA);   // 两层 0.392 的 over 合成
        CheckNear(t.a, wantA, 2e-3f,
                  "B2 ★ 可用 + 按下 ⇒ 色片 α = **两层叠放**的合成 α（`1−(1−0.392)²`）"
                + "｜🧨 退回「三选一」⇒ α 恒 0.392 ⇒ 本条红");
        Check(t.g > 0.30f,
              "B3 ★ 可用 + 按下 ⇒ tint 的 **g 通道被下面的黄绿层拉起来**（" + t.g.ToString("F4") + " > 0.30）"
            + "｜🧨 「三选一」只画纯蓝 ⇒ g = 0 ⇒ 本条红（黄绿那一层整层丢了）");
        Check(t.b > 0.5f && t.r > 0.2f,
              "B4 合成色落在蓝 + 黄绿两层的混合区（r=" + t.r.ToString("F4") + " / b=" + t.b.ToString("F4") + "）");

        _sp.SetPointer(InsidePoint(_sp), false);
        Check(!_sp.PressedForTest, "B5 前提：松开 ⇒ 蓝层熄（`_pressed=false`）");
        var t2 = _sp.LightForTest.Tint;
        CheckNear(t2.a, OrigActingA, 2e-3f,
                  "B6 ★ 只剩可用层 ⇒ α 回到单层 0.392（= 原版 `LightAvailable.m_Color.a`）");
        CheckNear(t2.r, OrigAvailableR, 2e-3f,
                  "B7 ★ 只剩可用层 ⇒ r = 原版 `LightAvailable.m_Color.r`（压到这一层自己身上）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  C. acting 白层淡入**从 0 起**（灭自证：旧实现起点 0.392 ⇒ C1 必红）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void C_ActingFromZero()
    {
        if (_sp == null) { Check(false, "C：前提不成立（见 A 组）⇒ 未验"); return; }

        _sp.SetActing();
        CheckNear(_sp.ActingAlphaForTest, 0f, 1e-4f,
                  "C1 ★ `ShowActingLight` 的淡入起点 = **0**（原版先 `HideActingLight()` 再 `DOFade(1,…)`）"
                + "｜🧨 起点写成序列化值 0.392 ⇒ 本条红（那会让白层**先闪一下 39%**）");
        Check(_sp.CurrentLight == SkillPanel.Light.Acting,
              "C2 ★ 起点 α = 0 的那一帧，`CurrentLight` **仍然是 Acting**（⛔ 不许掉回 Available/Pressed）");

        CardTween.Advance(SkillPanel.ActingFadeIn + 0.05f);   // 0.4 s 走完
        CheckNear(_sp.ActingAlphaForTest, 1f, 2e-3f,
                  "C3 0.4 s（`ActingFadeIn`）走完 ⇒ 白层 α = 1");
        Check(_sp.CurrentLight == SkillPanel.Light.Acting, "C4 白层满亮时 `CurrentLight` 仍是 Acting");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  D. `Hide()`（原版 `FadeOut`）**当场**熄蓝层 + 白层（灭自证：旧实现两者都会跟着面板一起淡 ⇒ D1/D2 红）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void D_HideKillsLights()
    {
        if (_sp == null) { Check(false, "D：前提不成立（见 A 组）⇒ 未验"); return; }

        // 先把两层都点亮：白层已经在 C 组满亮；再按一下拿蓝层
        _sp.SetPointer(InsidePoint(_sp), true);
        Check(_sp.PressedForTest && _sp.ActingFlagForTest,
              "D1 前提：蓝层与白层**同时**亮着（这一组才有意义）");

        _sp.Hide();

        Check(!_sp.PressedForTest,
              "D2 ★ `Hide()` ⇒ 蓝层 `_pressed` 当场 false（原版 `FadeOut.c` 的 `pressingLight.SetActive(false)`）"
            + "｜🧨 不退 ⇒ 淡出那 0.1 s 里还压着一层蓝 ⇒ 本条红");
        CheckNear(_sp.ActingAlphaForTest, 0f, 1e-4f,
                  "D3 ★ `Hide()` ⇒ 白层 α 当场归 0（原版 `FadePower`/`SetAlpha` 的 `HideActingLight()`）"
                + "｜🧨 不退 ⇒ 淡出里还看得到一层白 ⇒ 本条红");
        Check(!_sp.ActingFlagForTest, "D4 `Hide()` ⇒ acting 标志位也清掉了");

        // ⚠️ 这一刻**只剩可用层**：原版 `FadeOut` 不动 `bgLight`（它靠 `FadePower` 跟着面板一起淡）
        // ⇒ 刚 Hide 完 tint α 应当仍是单层 0.392，而**不是** 0（白/蓝两层已经熄掉了）。
        var t0 = _sp.LightForTest != null ? _sp.LightForTest.Tint : Color.white;
        CheckNear(t0.a, OrigActingA, 5e-3f,
                  "D5 ★ `Hide()` 之后只剩可用层（原版 `FadeOut` **不动** `bgLight`，它随 `FadePower` 淡）"
                + "｜🧨 把可用层也当场熄掉 ⇒ 与 `FadePower` 的六件集不符 ⇒ 本条红");

        CardTween.Advance(SkillPanel.FadeOutTime + 0.05f);
        Check(_sp.Alpha <= 1e-3f, "D6 0.1 s（`FadeOutTime`）走完 ⇒ 面板 α = 0");

        var t1 = _sp.LightForTest != null ? _sp.LightForTest.Tint : Color.white;
        Check(t1.a <= 0.01f,
              "D7 ★ 淡出走完 ⇒ 色片 α ≈ 0（可用层被面板 `Alpha` 一起淡掉；实得 " + t1.a.ToString("F4") + "）"
            + "｜🧨 可用层不吃面板 α ⇒ 面板没了色片还在 ⇒ 本条红");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  E. `TargetsAvailableText` 画在 `Lights` 之上（原版 `ActiveSkillDesc` 的 `m_Children` 序）
    //     画布档 = 比兄弟序（`HudCanvas.Add`：z 大的先画=在底下 ⇒ 下标大 = 在上面）
    //     世界档 = 比 `localPosition.z`（本工程口径：越负越靠前）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void E_TargetsAboveLights()
    {
        if (_sp == null) { Check(false, "E：前提不成立（见 A 组）⇒ 未验"); return; }

        var targets = _sp.TextForTest(3);            // 3 = `TargetsAvailableText`
        var light = _sp.LightForTest;
        Check(targets != null && light != null,
              "E1 前提：目标数那一行与色片那件都建出来了");

        var tt = targets != null ? targets.transform : null;
        var lt = light != null ? light.transform : null;
        if (tt == null || lt == null) { Check(false, "E2 前提不成立 ⇒ 未验"); return; }

        if (tt.parent == lt.parent)
        {
            // 画布档：同一父（子画布）⇒ 比兄弟序
            int it = tt.GetSiblingIndex(), il = lt.GetSiblingIndex();
            Check(it > il,
                  "E2 ★（画布档）`TargetsAvailableText` 排在 `Lights` **之后** = 画在它**上面**"
                + "（兄弟序 " + it + " vs " + il + "）"
                + "｜🧨 与 `Lights` 同 z（`ZText`）⇒ 它落到色片**下面** ⇒ 本条红");
        }
        else
        {
            // 世界档：比 z（本工程「越负越靠前」）
            float zt = tt.localPosition.z, zl = lt.localPosition.z;
            Check(zt < zl,
                  "E2 ★（世界档）`TargetsAvailableText` 的 z 比 `Lights` **更负** = 更靠前"
                + "（" + zt.ToString("F3") + " vs " + zl.ToString("F3") + "）"
                + "｜🧨 与 `Lights` 同 z ⇒ 它落到色片**下面** ⇒ 本条红");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  F. 费用图标那块不画（原版 `CostIcon` 13/13 `m_IsActive: 0` + `costSprite` 恒 null + 零写点）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void F_NoCostIcon()
    {
        if (_sp == null) { Check(false, "F：前提不成立（见 A 组）⇒ 未验"); return; }

        Check(_sp.ManaIconForTest == null,
              "F1 ★ 费用图标那一件**不建**（原版 `CostIcon` 的 GameObject 在 13 个战场里全是 `m_IsActive: 0`）"
            + "｜🧨 又建一颗能量宝石 ⇒ 面板上多一个原版没有的图标 ⇒ 本条红");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  G. 复原（跑完把面板摆回进来时的样子 —— 后面别的断言还要接着用）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void G_Restore()
    {
        if (_sp == null) { Check(false, "G：前提不成立（见 A 组）⇒ 未验"); return; }

        if (_wasVisible && !string.IsNullOrEmpty(_wasName))
        {
            _sp.Show(_wasName, (EffectSpec)null, _wasTargets);
            CardTween.Advance(SkillPanel.FadeTime + 0.05f);
            Check(_sp.Visible, "G1 复原：面板摆回「开着」（" + _wasName + " / " + _wasTargets + " 个目标）");
        }
        else
        {
            _sp.Hide();
            CardTween.Advance(SkillPanel.FadeOutTime + 0.05f);
            Check(!_sp.Visible, "G1 复原：面板摆回「收起」");
        }
        // ⚠️ 这一行**故意不计入通过数**（它只是把终态打出来，不是断言 —— ⛔ 别拿它凑绿）
        Debug.Log(Tag + " · G2 复原终态：`Alpha` = " + _sp.Alpha.ToString("F3")
                + "（进来时 " + _wasAlpha.ToString("F3") + "）· Visible = " + _sp.Visible);
    }
}
