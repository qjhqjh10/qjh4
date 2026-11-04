// B142_E51_Assert.cs — 执行写手 `B142-E51`（第一百二十九会话 · 2026-11-04）· CHK-05 块3 的两条
//
// ============================ 它盯的是什么 ============================
//  账 `资料/完全复刻/账/菜单_外壳.tsv` 的 **L179 / L231（禁用态 RGB）** 与 **L229（空 cue 静音）**。
//
//  | 账行 | 题目 | 判据（原版） |
//  |---|---|---|
//  | L179 | `EverguildButton.SetUpButtonColors` ⇒ **禁用态要乘 `ColorBlock.disabledColor` 的 RGB(+α)** | `decomp_full/EverguildButton__SetUpButtonColors.c`（54 行）+ UGUI `Selectable.DoStateTransition`（`case Disabled ⇒ colors.disabledColor`，再 `StartColorTween(tintColor * colorMultiplier)`）；prefab 逐颗 `m_Colors.m_DisabledColor` 分布（0.7843 ×3 **1208 颗** / 0.8019 ×3 **19** / 白 **49**） |
//  | L231 | `EverguildToggle.SetUpButtonColors` 同上（Toggle 侧 **24 颗** ColorTint 会触发） | `EverguildToggle__SetUpButtonColors.c`（同形，偏移 `+0x148`/`+0x14C`） |
//  | L229 | `PlayButtonSound` 的 `HasAudios()` 那一跳 | `decomp_full/EverguildToggle__PlayButtonSound.c`（69 行逐句） |
//
//  🔴 **L179/L231 的现读**：`KeyOf(st)` 在 `!_interactable` 时返回 `DisabledKeyNow`（= `DisabledKey` + α 判据），
//  再经 `ApplyTint → ComposeVertexColor(底座, 色键, ColorMultiplier)` **四通道一起乘进底图**
//  —— 这两半 **2026-11-04 `B141-W34`（5-08）就已落地**（账里「我们只换材质不乘 ⇒ 偏亮 ≈27%」那句与
//  `WindowButton.cs:979-981` 的「没有对应物」都是**改写前的旧结论**，本轮按铁律 5 就地订正了那三行）。
//  本文件把这条**钉成运行时逐值断言** —— `B141_W34_Assert` 只钉了源码结构（B7/B8），没有钉「乘后的数值」。
//
// ============================ 断言 ============================
//  A 组（运行时，真建 `WindowButton` + 子树一颗 `ImageQuad`）：
//    A1  白底 + 出厂 `DisabledKey` + `grey=1` ⇒ 禁用档 **逐值** = (0.7843137 ×3, α 1.0)
//    A1b `grey=0` + 青族 `DisabledKey`(0.8018868 ×3, a 0.5019608) ⇒ 禁用档 **α 保留 0.502**（⛔ 别一刀切成 1.0 / 0.5）
//    A1c 🧨 灭自证：禁用档必须**不等于白**（「禁用什么都不做」也能让它变绿 ⇒ 这条专治那种假绿）
//    A2  正常档 = 白（逐值）· A2b 悬停 = 0.9607843 · A2c 按下 = 0.7843137 · A2d 离开 = 白  ⇒ **四态逐位不回归**
//    A3  禁用 → 启用 → 再禁用：两次禁用档**逐值相同**（= 幂等；`RefreshGray` 换材质不许把色弄漂）
//    A4  `SpriteSwap` 族（`tintOnHover == false`）禁用时**底色一点不动** = UGUI「SpriteSwap 不走色偏」
//    A5  底座非白（0.5 灰）⇒ 禁用档 = **0.5 × 0.7843137 = 0.39215685**（证明是「乘进底图」而不是「把底图改成禁用色」）
//
//  B 组（cue 决议 + 一次真 `Click()`）：
//    B1  自身 cue 为空 + `useDefaultSound`（出厂 true）⇒ 决议 = `ButtonClick`（1022+151 颗那族一步不移）
//    B2  自身 cue 为空 + `useDefaultSound = false` ⇒ 决议 = **null（静音）**
//    B3  表里查不到的名字 + `useDefaultSound = false` ⇒ 决议 = null
//    B4  同上 + `useDefaultSound = true` ⇒ 决议 = `ButtonClick`（**不是**那个不存在的名字）
//    B5  表里有声音的自身 cue + `useDefaultSound = false` ⇒ 决议 = **它自己**（① 档真的优先于默认音）
//    B6  🧨 行为：B3 那件 `Click()` ⇒ `WindowSounds.LastCue` 仍 null / `Played` 仍 0（= `PlayFx` 没被调用）
//    B6b 🧨 正向对照：该播的那一档（默认音）`Click()` ⇒ `LastCue = ButtonClick`（证明 B6 的探针**不是恒绿的空断**）
//    B7  回归：`playClickSound = false` ⇒ `Click()` 同样不请求任何 cue
//    B8  🧨 源码：`PlayClickSound` 不再有 `PlayFx(clickCue ?? …)`；`ResolveClickCue` / `CueHasAudios` 在位
//    B9  🧨 源码：**没有**「`clickCue` 为空就 return」那种会把默认音一起掐掉的实现（会杀掉 564+18 颗的默认音）
//    B10 行为：查不到的名字**不污染** `WFSoundBank.BadCues`（先 `HasCue` 再 `TryGetCue`，先例 `RemnantSfx.cs:340`）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 把 `KeyOf` 禁用档改回 `NormalKey`（= 账里那句旧结论照做）⇒ A1 / A1b / A1c / A3 / A5 全红。
//  · 只乘 RGB 不乘 α（或反过来）⇒ A1b 红（α 0.502 被吞成 1.0）。
//  · 把 `ApplyTint` 改成「把底图**替换**成禁用色」（不是乘）⇒ A5 红（非白底座会得 0.7843 而不是 0.3921）。
//  · 给 `SpriteSwap` 族也叠色偏 ⇒ A4 红。
//  · 把 `ResolveClickCue` 改回 `clickCue ?? Default` ⇒ B2/B3 红（静音档不见了）、B6 红（`PlayFx` 被调用）。
//  · 把 `PlayButtonSound` 改成「`clickCue` 空就 return」（过度收紧）⇒ B1/B4 红。
//  · 用 `TryGetCue` 直接判（不先 `HasCue`）⇒ B10 红（`BadCues` 被 +1）。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只在临时 GameObject 上建件；收尾 `DestroyImmediate` +
//  还原 `MissingGrayArt` 快照）。挂法（由调度台决定）：同一物理行追加
//      B142_E51_Assert.Run(); pass += B142_E51_Assert.Pass; fail += B142_E51_Assert.Fail;
//  判绿红：按行首 `B142E51 ✓` / `B142E51 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B142_E51`：禁用态 `disabledColor` 乘进底图（L179/L231）+ 空 cue 静音（L229）。</summary>
public static class B142_E51_Assert
{
    const string Tag = "B142E51";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    /// <summary>原版 `m_Colors.m_DisabledColor` 最大那一族（1208/1276 颗），α 取 prefab 序列化值。</summary>
    static readonly Color DisabledMajority = new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f);
    /// <summary>原版「青族」那 19 颗（`0.8018868` ×3，a = 1.0）。</summary>
    static readonly Color DisabledCyanFamily = new Color(0.8018868f, 0.8018868f, 0.8018868f, 1f);

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static bool Near(Color a, Color b, float eps = 1e-6f)
    {
        return Mathf.Abs(a.r - b.r) < eps && Mathf.Abs(a.g - b.g) < eps
            && Mathf.Abs(a.b - b.b) < eps && Mathf.Abs(a.a - b.a) < eps;
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        // 🔴 `RefreshGray` 取不到原版灰化 shader 时会把**探针名**记进这张静态表（各宿主会断言它为空）
        //    ⇒ 跑完原样还回去（本文件只借这颗钮验色偏，灰那一路不属本条）。
        var graySaved = new List<string>(WindowButton.MissingGrayArt);
        // 🔴 批处理里**不真发声**（先例 `Editor/B109_E23_DailyClick_Assert.cs:65`）—— B6b 那条正向对照会真的请求
        //    默认音，`WindowSounds.LastCue` 在拨给播放器**之前**就记下，所以静音不影响读口。
        bool saveSfx = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;
        var root = new GameObject("B142E51");
        try
        {
            GroupA_DisabledColor(root.transform);
            GroupB_ClickCueSilence(root.transform);
        }
        finally
        {
            Object.DestroyImmediate(root);
            WindowButton.MissingGrayArt.Clear();
            WindowButton.MissingGrayArt.AddRange(graySaved);
            WarpforgeVFX.WFSoundPlayer.Enabled = saveSfx;
            WindowSounds.ResetCounters();   // 本文件借过这个读口（B6/B6b/B7）⇒ 收尾清零，别把基线留给下一个宿主
        }
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 禁用档 = `ColorBlock.disabledColor` 四通道乘进底图
    // ==================================================================
    static void GroupA_DisabledColor(Transform parent)
    {
        // ---- A0 · 前提（走不通时下面全是空断）----
        ImageQuad face;
        var wb = MakeButton(parent, Color.white, out face);
        Check(wb != null && face != null, "A0-前提：`ImageQuad.Create` + `AddComponent<WindowButton>()` 都建出来了");
        if (wb == null || face == null) return;

        // ---- A1 · 出厂配置（`grey = 1` 那 270 Button + 24 Toggle 的形态）----
        wb.DisabledKey = DisabledMajority;      // = 出厂值，这里显式写死方便读
        wb.ColorTintGreyOnDisable = true;       // 出厂 true（原版 `Awake` 把 α 重写成 1.0）
        wb.ApplyTintInstant();                  // 先让 `Collect()` 记下**底座 = 白**
        wb.Interactable = false;
        var want = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);
        Check(Near(face.Tint, want),
              "A1 L179/L231 禁用档 = `m_DisabledColor` 的 **RGB 与 α 一起**乘进底图 ⇒ 白底给 (0.7843137 ×3, α 1.0)｜实得 " + face.Tint);
        // 🧨 灭自证：禁用档**必须**与白拉开距离（「禁用什么都不做」这一档也能让上面那条绿 ⇒ 补这条专治假绿）
        Check(face.Tint.r < 0.99f && Mathf.Abs(face.Tint.r - 1f) > 0.1f,
              "A1c 🧨 禁用档确实**变暗**（≠ 白）—— 防「`KeyOf` 禁用档改回 `NormalKey` 也照样绿」｜实得 r=" + face.Tint.r.ToString("F7"));

        // ---- A1b · `grey = 0` 那一族：α **保留 prefab 原值** 0.502（⛔ 两档都不能一刀切）----
        //   ⚠️ 这里把**两个独立判据**搭在一起验：RGB 取「青族」那 19 颗的值、α 取 `grey=0` 那一族（66 颗 a=0.5019608）
        //      的序列化值 —— 两个旋钮各自都不能被 `colorTintGreyOnDisable` 抹平。
        wb.Interactable = true;
        wb.ColorTintGreyOnDisable = false;
        wb.DisabledKey = new Color(DisabledCyanFamily.r, DisabledCyanFamily.g, DisabledCyanFamily.b, 0.5019608f);
        wb.Interactable = false;
        Check(Near(face.Tint, new Color(0.8018868f, 0.8018868f, 0.8018868f, 0.5019608f)),
              "A1b `grey=0` 那档 α **保留 0.5019608**（原版 `SetUpButtonColors` 的闸不触发）+ RGB = 青族 0.8018868｜实得 " + face.Tint);

        // ---- A2 · 正常 / 悬停 / 按下 / 离开 **逐位不回归** ----
        wb.Interactable = true;
        Check(Near(face.Tint, Color.white),
              "A2 正常档 = 白（`m_NormalColor` 默认那一族 1198/1276）｜实得 " + face.Tint);
        wb.Enter();
        Check(Near(face.Tint, new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f)),
              "A2b 悬停档 = **0.9607843**（`m_HighlightedColor` 默认档）—— 禁用档那一笔不许把它带偏｜实得 " + face.Tint);
        wb.Press();
        Check(Near(face.Tint, new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f)),
              "A2c 按下档 = **0.7843137**（`m_PressedColor`）｜实得 " + face.Tint);
        wb.Release();
        Check(Near(face.Tint, new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f)),
              "A2c2 松开 ⇒ 回到悬停档 0.9607843（`Pressed` 优先级最高，退出即回落）｜实得 " + face.Tint);
        wb.Exit();
        Check(Near(face.Tint, Color.white), "A2d 离开 ⇒ 回常态白｜实得 " + face.Tint);

        // ---- A3 · 禁用 → 启用 → 再禁用：两次禁用档逐值相同（`RefreshGray` 换材质不许把色弄漂）----
        wb.DisabledKey = DisabledMajority; wb.ColorTintGreyOnDisable = true;
        wb.Interactable = false;
        var once = face.Tint;
        wb.Interactable = true;
        wb.Interactable = false;
        Check(Near(once, face.Tint) && Near(face.Tint, want),
              "A3 禁用 → 启用 → 再禁用：禁用档**幂等**（两次逐值相同）｜一次=" + once + " 二次=" + face.Tint);
        wb.Interactable = true;

        // ---- A4 · `SpriteSwap` 族（`tintOnHover == false`）禁用时**不做色偏** ----
        ImageQuad face2;
        var wb2 = MakeButton(parent, Color.white, out face2);
        if (wb2 != null && face2 != null)
        {
            wb2.ColorTintGreyOnDisable = false;      // 隔离：只问「有没有色偏」
            wb2.tintOnHover = false;                 // = `Bind()` 对 SpriteSwap 那一档做的事
            wb2.ApplyTintInstant();
            wb2.Interactable = false;
            Check(Near(face2.Tint, Color.white),
                  "A4 `m_Transition = 2(SpriteSwap)` 一族禁用时**底色不动**（UGUI 那一档走 `m_DisabledSprite`、不走色偏）｜实得 " + face2.Tint);
            wb2.Interactable = true;
            Object.DestroyImmediate(wb2.gameObject);
        }
        else Check(false, "A4 夹具没建出来");

        // ---- A5 · 底座非白 ⇒ **乘**进底图（不是「把底图替换成禁用色」）----
        ImageQuad face3;
        var wb3 = MakeButton(parent, new Color(0.5f, 0.5f, 0.5f, 1f), out face3);
        if (wb3 != null && face3 != null)
        {
            wb3.DisabledKey = DisabledMajority; wb3.ColorTintGreyOnDisable = true;
            wb3.ApplyTintInstant();
            wb3.Interactable = false;
            Check(Near(face3.Tint, new Color(0.39215685f, 0.39215685f, 0.39215685f, 1f)),
                  "A5 底座 0.5 灰 ⇒ 禁用档 = **0.5 × 0.7843137 = 0.39215685**（乘进底图，不是替换）｜实得 " + face3.Tint);
            wb3.Interactable = true;
            Object.DestroyImmediate(wb3.gameObject);
        }
        else Check(false, "A5 夹具没建出来");

        Object.DestroyImmediate(wb.gameObject);
    }

    // ==================================================================
    //  B · `PlayButtonSound` 的三档 cue 决议 + 静音档真的不请求播放
    // ==================================================================
    static void GroupB_ClickCueSilence(Transform parent)
    {
        // ---- B0 · 前提：cue 表在、默认音那条 cue 有声音（否则下面「决议 = ButtonClick」等于空断）----
        bool hasDefault = WarpforgeVFX.WFSoundBank.HasCue(WindowSounds.DefaultButtonCue);
        Check(hasDefault, "B0-前提：音效表里有默认按钮音 `" + WindowSounds.DefaultButtonCue
                        + "`（`Resources/animfx_sounds.json`；缺 ⇒ 全工程按钮音哑）");
        if (!hasDefault) return;

        var go = new GameObject("B142E51_cue", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var wb = go.AddComponent<WindowButton>();
        Check(wb.useDefaultSound, "B1-前提：`useDefaultSound` 出厂 = **true**（原版 1022/1022 + 151/151 颗 `useDefaultAudioClipIfNull = 1`）");

        // ---- B1 · 自身 cue 空 + useDefaultSound ⇒ 默认按钮音（564 + 18 颗那一族，一个字节都不许变）----
        wb.clickCue = null; wb.useDefaultSound = true;
        Check(wb.ClickCueForTest == WindowSounds.DefaultButtonCue,
              "B1 自身 cue 为空 ⇒ 走 `SoundManager.defaultButtonSound`（= `ButtonClick`）｜实得 " + (wb.ClickCueForTest ?? "<null>"));

        // ---- B2 · 自身 cue 空 + 不给默认音 ⇒ **静音** ----
        wb.clickCue = null; wb.useDefaultSound = false;
        Check(wb.ClickCueForTest == null,
              "B2 自身 cue 为空 **且** `useDefaultSound = false` ⇒ 静音档（原版整段 `return`）｜实得 "
              + (wb.ClickCueForTest ?? "<null>"));

        // ---- B3/B4 · 表里查不到的名字（= 原版 `HasAudios() == false` 的那一档）----
        const string bogus = "B142E51 不存在的 cue";
        wb.clickCue = bogus; wb.useDefaultSound = false;
        Check(wb.ClickCueForTest == null,
              "B3 名字在表里查不到 + 不给默认音 ⇒ 静音档｜实得 " + (wb.ClickCueForTest ?? "<null>"));
        wb.clickCue = bogus; wb.useDefaultSound = true;
        Check(wb.ClickCueForTest == WindowSounds.DefaultButtonCue,
              "B4 名字在表里查不到 + 给默认音 ⇒ 落到 `ButtonClick`（**不是**那个不存在的名字）｜实得 "
              + (wb.ClickCueForTest ?? "<null>"));

        // ---- B5 · ① 档优先：自身 cue 有声音时**不许**被默认音顶掉 ----
        wb.clickCue = WindowSounds.SelectToggleCue; wb.useDefaultSound = true;
        Check(wb.ClickCueForTest == WindowSounds.SelectToggleCue,
              "B5 自身 cue（`" + WindowSounds.SelectToggleCue + "`，表里有 clip）⇒ 决议就是**它自己**，默认音不许顶掉｜实得 "
              + (wb.ClickCueForTest ?? "<null>"));

        // ---- B6 · 🧨 行为：静音档 `Click()` 之后 `PlayFx` **一次都没被调用** ----
        //   🔴 读口 = `WindowSounds.LastCue`（它在拨给播放器**之前**就记下）⇒ 先 `ResetCounters()` 拿一个确定的基线
        //      （`null`），再断「点完仍是 `null`」；同先例 `Editor/B117_G10_14_Assert.cs:195-199`。
        wb.clickCue = bogus; wb.useDefaultSound = false; wb.playClickSound = true;
        WindowSounds.ResetCounters();
        wb.Click();
        Check(WindowSounds.LastCue == null && WindowSounds.Played == 0,
              "B6 🧨 静音档 `Click()` ⇒ `PlayFx` **没被调用**（`LastCue` 仍 null、`Played` 仍 0）｜LastCue="
              + (WindowSounds.LastCue ?? "<null>") + " Played=" + WindowSounds.Played);

        // ---- B6b · 🧨 灭自证：同一个探针在**该播**的那一档必须能看见调用（否则 B6 是恒绿的空断）----
        wb.clickCue = null; wb.useDefaultSound = true;
        WindowSounds.ResetCounters();
        wb.Click();
        Check(WindowSounds.LastCue == WindowSounds.DefaultButtonCue,
              "B6b 🧨 默认音那一档 `Click()` ⇒ `LastCue` = `" + WindowSounds.DefaultButtonCue
              + "`（证明 B6 的探针**真的能看见调用**；同时锁住 564 + 18 颗的默认音没被掐掉）｜实得 "
              + (WindowSounds.LastCue ?? "<null>"));

        // ---- B7 · 回归：`playClickSound = false`（压暗层 / 开包窗的卡）⇒ 任何 cue 都不请求 ----
        wb.clickCue = null; wb.useDefaultSound = true; wb.playClickSound = false;
        WindowSounds.ResetCounters();
        wb.Click();
        Check(WindowSounds.LastCue == null && WindowSounds.Played == 0,
              "B7 `playClickSound = false` ⇒ `Click()` 不请求任何 cue（压暗层那一族不响）｜LastCue="
              + (WindowSounds.LastCue ?? "<null>") + " Played=" + WindowSounds.Played);
        wb.playClickSound = true;

        // ---- B10 · 查不到的名字**不污染** `WFSoundBank.BadCues`（先 `HasCue` 再 `TryGetCue`）----
        int bad0 = WarpforgeVFX.WFSoundBank.BadCues;
        wb.clickCue = bogus; wb.useDefaultSound = false;
        string resolvedAfter = wb.ClickCueForTest;
        Check(WarpforgeVFX.WFSoundBank.BadCues == bad0,
              "B10 查不到的名字**不推进** `WFSoundBank.BadCues`（= 先 `HasCue` 再 `TryGetCue`，不占别人的读口）｜"
              + bad0 + " → " + WarpforgeVFX.WFSoundBank.BadCues + "（决议 = " + (resolvedAfter ?? "<null>") + "）");

        Object.DestroyImmediate(go);

        // ---- B8/B9 · 源码结构（钉住实现口径，⛔ 不是拿我们的常量自证）----
        var src = Src("Shell/WindowButton.cs");
        Check(!src.Contains("WindowSounds.PlayFx(clickCue ?? WindowSounds.DefaultButtonCue"),
              "B8 🧨 `PlayClickSound` 里**不再**是 `PlayFx(clickCue ?? Default)`（改回去 ⇒ 静音档消失、B2/B3/B6 一起红）");
        Check(src.Contains("string cue = ResolveClickCue();")
              && src.Contains("if (CueHasAudios(clickCue)) return clickCue;")
              && src.Contains("return useDefaultSound ? WindowSounds.DefaultButtonCue : null;"),
              "B8b 三档决议（`ResolveClickCue`）在位：① 自身有声音 ② `useDefaultSound` ⇒ 默认音 ③ 否则 null");
        Check(!src.Contains("if (string.IsNullOrEmpty(clickCue)) return;"),
              "B9 🧨 **没有**「`clickCue` 空就 return」那种过度收紧（那会把 564 + 18 颗的**默认音**一起掐掉）");
        Check(src.Contains("public bool useDefaultSound = true;"),
              "B9b `useDefaultSound` 出厂 = **true**（= 原版 `useDefaultAudioClipIfNull` 1022/1022 · 151/151）");
        Check(src.Contains("if (!_interactable) return DisabledKeyNow;"),
              "B8c `KeyOf` 禁用档仍返回 `DisabledKeyNow`（A 组的运行时证据背后的那一句）");
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    /// <summary>`WindowButton` 夹具：按钮节点 + 子树里一颗 `ImageQuad`（`Collect()` 收的就是它）。
    /// ⚠️ 底座色必须在**第一次** `ApplyTint` 之前写好（`Collect()` 只收一次基准色）。</summary>
    static WindowButton MakeButton(Transform parent, Color baseColor, out ImageQuad face)
    {
        var go = new GameObject("B142E51_btn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var wb = go.AddComponent<WindowButton>();
        face = ImageQuad.Create(go.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                new Vector2(0.5f, 0.5f), "face");
        if (face != null) face.SetTint(baseColor);
        return wb;
    }
}
