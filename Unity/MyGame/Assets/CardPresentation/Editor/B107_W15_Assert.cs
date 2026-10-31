// B107_W15_Assert.cs — 执行代理 `B107_W15`（第一百零七会话 · 2026-10-31）
//
// ============================ 它盯的是什么（三条新账）============================
//  A **N104 · FP-SOCIAL-06**：收件箱正文里的 `<link>` ⇒ `Application.OpenURL`（原版 `TextLinkProcessing.TryProcessText`）。
//    生产在 `Shell/InboxWindow.cs:1385 OnMessageBodyClick`；原来**没有任何断言**（`B101R3_Assert` 只断外链图标贴图）。
//    判据（第一权威 = 反汇编）：
//      · `decomp_full/MessageDisplay__OnPointerClick.c` 全 body 一句 = `TextLinkProcessing.TryProcessText(text, eventData)`；
//      · `decomp_full/TextLinkProcessing__TryProcessText.c`：`TMP_TextUtilities.FindIntersectingLink(text, eventData.position, Camera.main)`
//        → `-1` ⇒ 什么都不做；否则 `textInfo.linkInfo[i].GetLinkID()` → **`Split(';')`** → 取第 0 段
//        → `int.TryParse` **失败** ⇒ `Application.OpenURL(第0段)` 并 return；成功 ⇒ `RemoveAt(0)` 后 return
//        —— 而下一个消费者 `TextLinkProcessing__ProcessText.c` 是**空桩**（机器码只有 `ret`）⇒ 数字型点了什么都不做。
//
//  B **N106 · FP-MISC-03**：`SkewedImage` 的非零 shear（`Battle/ImageQuad.cs:457 SetShear`）。
//    生产口 = `Shell/MenuDraw.cs:1624 Rect(..., skewX, skewY, rotZDeg)`（`skewX/skewY != 0` 时调 `SetShear`）。
//    判据 = `decomp_full/SkewedImage__OnPopulateMesh.c`：先 `base.OnPopulateMesh(vh)`，再逐顶点
//      `new_y = y + x * *(this+0x124)` · `new_x = x + y * *(this+0x120)`（顶点在**图自己的局部空间**、中心为原点）
//      ⇒ `x' = x + y·skewX` · `y' = y + x·skewY`（0x120/0x124 与 `skewVector.x/y` 的对应见 `SetShear` 的 doc）。
//    ⚠️ `offsetX/offsetY`（九宫子块的块心偏移）**原版没有**（原版没有九宫这一层）⇒ 那一段是 🔶 自己设计的超集，
//     本件只做**回归锁**，⛔ 不当原版判据。
//
//  C **N105 · FP-AUDIO-03**：混音组路由（Music / FX / Voices / Jingles）。
//    判据 = `Core/WarpforgeAudio.cs:76 RouteEffectSounds`（原版 `SoundManager.mixerGroup` 那一族）+ 文件头的四通道口径。
//    ⚠️ `VoiceWaveProbe` **仍 0 宿主**：它 `Run()` 末尾 `Application.isBatchMode` 时会 `EditorApplication.Exit`
//      ⇒ ⛔ **不能**从别的宿主里调（会当场结束那个宿主进程）。接线 = 在 `工具/_run_8_checks.sh` 里
//      `AudioSnapshotCheck.Run`（现读 `:58`）之后**加一条独立宿主**：
//          `run VoiceWaveProbe.Run waveprobe.log # 音波线：A 段驱动器 + B 段 shader`
//      —— **改脚本归调度台**（本代理不动脚本、不动 git）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · A 段 `B107W15_Assert.RunInboxLink(inbox)` —— 宿主 **`Editor/RewardsScene.cs`**（§六 收件箱那一节）。
//    接在 **Claim 链那块 `}` 之后、`// ---- 还原成空态（Initialize(null) …）----` 之前**（现读 `:12140` 那条注释之上）。
//    ⚠️ 它自己灌一条**假消息**（正文里带 `<link>`）再 `finally` **原样灌回** ⇒ 12141 那条 `Initialize(null)` 照常跑。
//  · B 段 `B107W15_Assert.RunShear()` —— **自建临时节点/临时贴图，不读宿主任何状态** ⇒ 任意宿主可接；
//    建议与 A 段同一个宿主（`\Editor/RewardsScene.cs`，紧跟 A 段）免得再动第三个文件。
//  · C 段 `B107W15_Assert.RunAudioRouting()` —— 只读 `WarpforgeAudio` / `WFSoundPlayer` 的组引用，
//    建议接 **`Editor/ShellScene.cs`** 那段混音断言（现读 `:4803` 一带）**之后**。
//  · 每一段的收尾都写 `pass += B107W15_Assert.Pass; fail += B107W15_Assert.Fail;`（口径同 `B107_W2_Assert`）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · A②：指针**不在**链接上 ⇒ 出口一次都不许调。谁把 `FindIntersectingLink == -1` 那一支写成「照样开」⇒ 红。
//  · A③：数字型 link id ⇒ 出口一次都不许调。谁把 `int.TryParse` 那一支删掉 ⇒ 红（会去开 `12345`）。
//  · A④：link id 带 `;` ⇒ 出口只收**第 0 段**。谁去掉 `Split(';')` ⇒ 红（原版逐字有这一步）。
//  · B②：非零 skew 必须**同时**动 x 与 y，且逐顶点等 `x+y·skewX` / `y+x·skewY`。
//    只断「方法被调过」或把两条公式写反（`x+y·skewY`）都会红。
//  · C：四个组名逐个按**原版枚举名**断（`Music/FX/Voices/Jingles`），不是「非 null 就算过」。
//
// ============================ 判据 / 报告 ============================
//  报告：`资料/完全复刻/报告/B107_W15.md`（⛔ 别在这里抄第二份）
using System.Collections.Generic;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行代理 `B107_W15`：收件箱正文链接（N104）· 非零 shear（N106）· 混音组路由（N105）。</summary>
public static class B107W15_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B107W15] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ==================================================================
    //  A · N104 FP-SOCIAL-06：正文 `<link>` ⇒ Application.OpenURL
    // ==================================================================
    /// <summary>A 段入口。`inbox` = 宿主手里那颗开着的 `InboxWindow`（RewardsScene §六 的 `inbox`）。</summary>
    public static void RunInboxLink(InboxWindow inbox)
    {
        S("🆕 B107_W15 · A：收件箱正文链接 ⇒ Application.OpenURL（原版 `TextLinkProcessing.TryProcessText`）");
        T(inbox != null, "（前提）拿到 `InboxWindow` 实体（没有 ⇒ 本段等于没验）");
        if (inbox == null) return;

        var hit = inbox.MessageHitNode;
        T(hit != null, "（前提）正文命中区 `MessageText Hit` 建出来了（`InboxWindow` Build 那一句，QText+1）");
        var hb = hit != null ? hit.GetComponent<WindowButton>() : null;
        T(hb != null, "（前提）命中区上挂着 `WindowButton`");
        T(hb != null && hb.onDown != null && hb.onDown == (System.Action)inbox.OnMessageBodyClick,
          "（前提）`onDown` 接的就是本窗 `OnMessageBodyClick`（原版 `MessageDisplay.OnPointerClick` 那一下）");

        var cam = Camera.main;
        T(cam != null, "（前提）`Camera.main` 在（原版 `TryProcessText` 就把它喂给 `FindIntersectingLink`；没有相机 ⇒ 整条链不动作）");
        var body = inbox.MessageBodyForTest;
        T(body != null, "（前提）拿到正文 `Label`（`MessageBodyForTest`）");
        if (cam == null || body == null) return;

        var saved = inbox.MessagesForTest;
        T(saved != null, "（前提）拿到当前消息表的副本（测完原样灌回，⛔ 不留假状态给后面的断言）");
        if (saved == null) return;

        const string UrlPlain = "https://example.invalid/warpforge-terms";
        const string UrlSemi = "https://example.invalid/a;b=2";
        const string DigitId = "12345";

        int calls = 0; string got = null;
        InboxWindow.UrlExitCaptureForTest = u => { calls++; got = u; };

        try
        {
            // ---- ① 普通 URL：点击 ⇒ 出口收到它 ----
            ShowSynthetic(inbox, UrlPlain, "terms of service");
            Vector3 world;
            bool hasLink = ProbeLinkPoint(body, cam, UrlPlain, out world);
            T(hasLink,
              "（前提）正文里那条 `<link>` 排版后**量得到**（用 `Label.LinkAt` = 生产同一条命中链在正文框内扫到它；"
              + "扫不到 ⇒ 下面几条都是空转）");
            if (hasLink)
            {
                calls = 0; got = null;
                InboxWindow.PointerOverrideForTest = cam.WorldToScreenPoint(world);
                inbox.OnMessageBodyClick();
                C(calls, 1, "★ 指针压在链接上 ⇒ 出口**恰好调一次**（原版 `TryProcessText` 命中后 `OpenURL` 并 return）");
                C(got, UrlPlain, "★ 出口拿到的是**该链接的 URL**（原版 `Application.OpenURL(GetLinkID())`）");
            }

            // ---- ② 灭自证①：指针在空处 ⇒ 一次都不调 ----
            calls = 0; got = null;
            InboxWindow.PointerOverrideForTest = new Vector3(-100f, -100f, 0f);   // 屏幕外，绝对不在正文上
            inbox.OnMessageBodyClick();
            C(calls, 0, "★ 灭自证①：指针不在链接上 ⇒ 出口**一次都不调**（原版 `FindIntersectingLink == -1` 静默 return）");

            // ---- ③ 灭自证②：数字型 link id ⇒ 一次都不调（原版 `ProcessText` 是空桩）----
            ShowSynthetic(inbox, DigitId, DigitId);
            if (ProbeLinkPoint(body, cam, DigitId, out world))
            {
                calls = 0; got = null;
                InboxWindow.PointerOverrideForTest = cam.WorldToScreenPoint(world);
                inbox.OnMessageBodyClick();
                C(calls, 0, "★ 灭自证②：数字型 link id ⇒ 出口**一次都不调**（`int.TryParse` 成功 ⇒ 原版 `ProcessText` 空桩；"
                           + "谁删掉那一支 ⇒ 会去开 `12345` ⇒ 红）");
            }
            else T(false, "（前提）数字型链接也量得到（量不到 ⇒ 上面那条空转）");

            // ---- ④ `Split(';')` 取第 0 段 ----
            ShowSynthetic(inbox, UrlSemi, "semi");
            if (ProbeLinkPoint(body, cam, UrlSemi, out world))
            {
                calls = 0; got = null;
                InboxWindow.PointerOverrideForTest = cam.WorldToScreenPoint(world);
                inbox.OnMessageBodyClick();
                C(got, "https://example.invalid/a",
                  "★ link id 带 `;` ⇒ 只开**第 0 段**（原版 `Split(';')` → `list[0]`；⛔ 别把整串/后半段当 URL）");
            }
            else T(false, "（前提）带 `;` 的链接也量得到（量不到 ⇒ 上面那条空转）");
        }
        finally
        {
            InboxWindow.PointerOverrideForTest = null;
            InboxWindow.UrlExitCaptureForTest = null;
            inbox.Initialize(saved);            // 原样灌回（RewardsScene 紧接着那句 `Initialize(null)` 仍走空态支）
        }
    }

    /// <summary>把一条**假消息**（正文 = 单个 `<link>`）灌进收件箱 ⇒ 正文 `Label` 重新排版。</summary>
    static void ShowSynthetic(InboxWindow inbox, string linkId, string visible)
    {
        var list = new List<InboxWindow.Message>
        {
            new InboxWindow.Message("B107_W15 probe", "", false)
            {
                Id = "",
                Body = "<link=\"" + linkId + "\">" + visible + "</link>",
            },
        };
        inbox.Initialize(list);
    }

    /// <summary>在正文 `Label` 的**排版框**里扫一个**确实压在链接上**的世界点（用的就是生产同一条 `Label.LinkAt`）。
    /// 找到第一个就返回（链接在正文左上角，扫描从左上角开始，通常几十次调用内命中）。
    /// <para>⚠️ 采样中心/尺寸取 **TMP 自己的 `textBounds`（center + size）**，⛔ 不是 `Label.transform.position`：
    /// 正文建完之后会走 `MenuDraw.AlignLeft` / `SetVAlign`，那两刀挪的是 **TMP 子节点**、不是 `Label` 本身
    /// ⇒ 按 Label 原点采样会整体偏到框心之外。采样带再放宽到 ±0.65 个排版框（`RefreshBounds` 的 `_vOffset` 字墨校正）。</para></summary>
    static bool ProbeLinkPoint(Label body, Camera cam, string id, out Vector3 world)
    {
        world = Vector3.zero;
        var tr = body != null ? body.transform.Find("text") : null;      // `Label.BuildTmp` 建的 TMP 子节点名恒为 "text"
        var tmp = tr != null ? tr.GetComponent<TMP_Text>() : null;
        if (tmp == null || cam == null) return false;
        var b = tmp.textBounds;
        // 哨兵/未测量（4.29e9 那个已知值）⇒ 不猜，直接判「量不到」
        if (!(b.size.x > 1e-4f) || !(b.size.y > 1e-4f) || b.size.x > 1e4f || b.size.y > 1e4f) return false;

        const int NX = 40, NY = 20;
        for (int iy = NY; iy >= 0; iy--)
        {
            float ly = (iy / (float)NY - 0.5f) * b.size.y * 1.3f;
            for (int ix = 0; ix <= NX; ix++)
            {
                float lx = (ix / (float)NX - 0.5f) * b.size.x * 1.3f;
                var wp = tmp.transform.TransformPoint(b.center + new Vector3(lx, ly, 0f));
                if (body.LinkAt(wp, cam) == id) { world = wp; return true; }
            }
        }
        return false;
    }

    // ==================================================================
    //  B · N106 FP-MISC-03：非零 shear（走 MenuDraw.Rect 生产口）
    // ==================================================================
    /// <summary>B 段入口。自建临时父节点 + 临时贴图，不读宿主任何状态。</summary>
    public static void RunShear()
    {
        S("🆕 B107_W15 · B：`SkewedImage` 非零 shear（`x' = x + y·skewX` · `y' = y + x·skewY`）");
        var root = new GameObject("B107W15_shear");
        var tex = new Texture2D(8, 4);
        try
        {
            var pxr = new PxRect(100f, 100f, 500f, 300f);     // 400×200 设计 px（aspect = 2，与贴图 8:4 同）
            var a = MenuDraw.Rect(root.transform, tex, pxr, "shear0", 3000);
            var b = MenuDraw.Rect(root.transform, tex, pxr, "shear05", 3000, skewX: 0.5f, skewY: 0.25f);
            T(a != null && b != null, "（前提）`MenuDraw.Rect` 建出两块 quad（`tex` 非空）");
            if (a == null || b == null) return;

            var ma = a.GetComponent<MeshFilter>() != null ? a.GetComponent<MeshFilter>().sharedMesh : null;
            var mb = b.GetComponent<MeshFilter>() != null ? b.GetComponent<MeshFilter>().sharedMesh : null;
            T(ma != null && mb != null && ma.vertexCount == 4 && mb.vertexCount == 4,
              "（前提）两块各有 4 个顶点（`ImageQuad.RebuildMesh` 的四边形）");
            if (ma == null || mb == null || ma.vertexCount != 4 || mb.vertexCount != 4) return;

            var va = ma.vertices; var vb = mb.vertices;
            for (int i = 0; i < 4; i++)
            {
                // 期望值**只从 baseline 顶点 + 原版公式**算，⛔ 不读 `SetShear` 存下来的 `_skewX/_skewY`
                float wantX = va[i].x + va[i].y * 0.5f;
                float wantY = va[i].y + va[i].x * 0.25f;
                N(vb[i].x, wantX, 1e-3f, "★ 顶点 " + i + "：x' = x + y·skewX（skewX = 0.5）");
                N(vb[i].y, wantY, 1e-3f, "★ 顶点 " + i + "：y' = y + x·skewY（skewY = 0.25）");
            }

            // ---- 灭自证：只断公式时，若实现把两式写反/只做一半，上面就会红；这里再显式钉「两个轴都真的动了」----
            int topRight = 2;                                   // 顶点序：0=左下 1=右下 2=右上 3=左上
            T(Mathf.Abs(vb[topRight].x - va[topRight].x) > 1e-3f,
              "★ 非零 `skewX` **真的动了 x**（右上角 y≠0 ⇒ 位移 = y·0.5；只断「调过方法」会漏）");
            T(Mathf.Abs(vb[topRight].y - va[topRight].y) > 1e-3f,
              "★ 非零 `skewY` **真的动了 y**（右上角 x≠0 ⇒ 位移 = x·0.25；两式写反时这条不会红，上面公式那两条会红）");

            // ---- 🔶 自己设计（非原版）的九宫块心偏移那一半：只做回归锁 ----
            //   `SetShear(..., offsetX, offsetY)` 的 `（y+offsetY）` / `（x+offsetX）` 是**原版没有的**超集
            //   （原版没有九宫这一层；语义见 `SetShear` 的 doc）。⛔ 别把它当原版判据。
            var c = ImageQuad.Create(root.transform, tex, Vector3.zero, 100f, new Vector2(0.5f, 0.5f), "shear_off");
            if (c != null)
            {
                var mc = c.GetComponent<MeshFilter>() != null ? c.GetComponent<MeshFilter>().sharedMesh : null;
                var v0 = mc != null ? mc.vertices : null;
                c.SetShear(0.5f, 0.25f, 30f, -20f);
                mc = c.GetComponent<MeshFilter>() != null ? c.GetComponent<MeshFilter>().sharedMesh : null;
                var v1 = mc != null ? mc.vertices : null;
                T(v0 != null && v1 != null && v0.Length == 4 && v1.Length == 4, "（前提）偏移那条同样有 4 个顶点");
                if (v0 != null && v1 != null && v0.Length == 4 && v1.Length == 4)
                {
                    N(v1[0].x, v0[0].x + (v0[0].y - 20f) * 0.5f, 1e-3f,
                      "🔶 自己设计（非原版）：九宫块心偏移 x' = x + (y+offsetY)·skewX（offsetY = -20）");
                    N(v1[0].y, v0[0].y + (v0[0].x + 30f) * 0.25f, 1e-3f,
                      "🔶 自己设计（非原版）：九宫块心偏移 y' = y + (x+offsetX)·skewY（offsetX = 30）");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tex);
        }
    }

    // ==================================================================
    //  C · N105 FP-AUDIO-03：混音组路由（Music / FX / Voices / Jingles）
    // ==================================================================
    /// <summary>C 段入口。只读组引用 —— 不建音源、不改音量、不播任何东西。</summary>
    public static void RunAudioRouting()
    {
        S("🆕 B107_W15 · C：混音组路由（Music / FX / Voices / Jingles）");
        T(WarpforgeAudio.Ready, "（前提）`Resources/Audio/Main Mixer` 加载得到（`WarpforgeAudio.Ready` 会触发一次 `Ensure()`）");
        if (!WarpforgeAudio.Ready) return;

        var m = WarpforgeAudio.MusicGroup;
        var v = WarpforgeAudio.VoicesGroup;
        var j = WarpforgeAudio.JinglesGroup;
        var f = WarpforgeVFX.WFSoundPlayer.FxGroup;

        T(m != null, "★ 音乐有组：`WarpforgeAudio.MusicGroup` 非空（原版 `MusicManager` 走 `MixerType.Music`）");
        T(v != null, "★ 单位语音有组：`WarpforgeAudio.VoicesGroup` 非空（原版 `Play2D(cue, MixerType.Voices)`）");
        T(j != null, "★ 结算开门音有组：`WarpforgeAudio.JinglesGroup` 非空（原版 `Play2D(cue, MixerType.Jingles /*=3*/)`）");
        T(f != null, "★ 特效音有组：`WarpforgeVFX.WFSoundPlayer.FxGroup` 非空（原版 `MixerType.FX`；`RouteEffectSounds` 写这一个）");

        // 组名逐个按**原版枚举名**钉（`MixerType` 的 `Music / FX / Voices / Jingles`）—— 不是「非 null 就算过」。
        T(m != null && m.name == "General" && m.audioMixer != null && m.audioMixer.name == "Music",
          "★ 音乐组 = **Music 子 mixer 的 `General`**（三份 mixer 化：菜单音乐 → `Music.General`；实得 "
        + (m != null ? m.name + " @" + (m.audioMixer != null ? m.audioMixer.name : "?") : "<null>") + "）");
        T(f != null && f.name == "General" && f.audioMixer != null && f.audioMixer.name == "FX",
          "★ 特效组 = **FX 子 mixer 的 `General`**（运行时特效音 → `FX.General`；实得 "
        + (f != null ? f.name + " @" + (f.audioMixer != null ? f.audioMixer.name : "?") : "<null>") + "）");
        T(v != null && v.name == "Voices", "★ 语音组的**名字** = `Voices`（实得 `" + (v != null ? v.name : "<null>") + "`）");
        T(j != null && j.name == "Jingles", "★ Jingles 组的**名字** = `Jingles`（实得 `" + (j != null ? j.name : "<null>") + "`）");
    }

    // ==================================================================
    //  C 的接线口（N105）：VoiceWaveProbe 仍 0 宿主
    // ==================================================================
    /// <summary>`VoiceWaveProbe` 的**独立宿主入口**（⚠️ 只能作为单独的 `-executeMethod` 跑）。
    /// <para>为什么不能从别的宿主里调：`VoiceWaveProbe.Run()` 末尾在 `Application.isBatchMode` 时
    /// `EditorApplication.Exit(...)` ⇒ 从别处调用会**当场结束那个宿主进程**（其余断言一条都不跑）。</para>
    /// <para>调度台接线（⛔ 本代理不动脚本）：`工具/_run_8_checks.sh` 在现读 `:58` 的
    /// `run AudioSnapshotCheck.Run audiosnap.log` 之后加一行
    /// <c>run VoiceWaveProbe.Run waveprobe.log</c>（串行，同其它条目）。</para></summary>
    public static void RunVoiceWaveProbeHost()
    {
        VoiceWaveProbe.Run();
    }
}
