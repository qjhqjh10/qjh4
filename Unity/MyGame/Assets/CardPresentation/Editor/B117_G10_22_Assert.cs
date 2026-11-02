// B117_G10_22_Assert.cs — 执行代理 `B117-G10-22`（第一百一十七会话 · 2026-11-03）
//
// 盯的是「战斗内聊天 · 单位语音条 · 大聊天」这一片**判据明确**的四件事（差异表 →
// `资料/完全复刻/判据/B117-G10-22_战斗内聊天与语音条.md`）：
//
//  A **气泡的 `AudioSource` 逐字段照原版**（本轮改的就是这两格）
//    判据 = 原版每颗气泡自己那颗 AudioSource（`bundle_scenes_scenes_battlearena{1,2,aeldari,darkangels}/
//    AudioSource/AudioSource_159{6,8,a0}.json`，4 场逐值相同）：
//      `m_Volume = 1.0` · `Priority = 154` · `Spatialize = false`(⇒ spatialBlend 0) ·
//      `m_PlayOnAwake = false` · `Loop = false` · `Mute = false` · `mixerType = 2 = MixerType.Voices`。
//    🔴 改前 `volume = 0.85` 还带着 `🔶 自己设计`（理由写「原版逐 source 的音量没查到」——**不成立**）。
//
//  B **停留时长 = 原版 `max(2.0, clip.length + 0.3)`**（`VoiceLinesController__GetCurrentClipLength.c:9-33`；
//    两个常量 `DAT_1834b2bbc = 2.0` / `DAT_1834b2dc8 = 0.3`）
//    ⇒ 短 clip（0.25 s）**不许**缩到 1.4 s（那是改前 `clamp(len,1,4)+0.4` 的行为）。
//
//  C **文字与语音是两条独立的档**：`clip == null` 时**照样出字**（原版 `VoiceLineBox.ShowChat` 无条件
//    设 `data.ChatMessage`），而「该有声、clip 却取不到」要**单独出声一次**（不是静默）。
//
//  D **台词文字框的落点**（本轮订正的是中线 134.55 → 135.53）
//    判据 = 原版 `ChatText` 的 `RectTransform_3566`（aMin/aMax/ap/sd 逐字段算）：
//      左沿 188.55 · 中线 135.53（相对气泡左上角）；`Background` 中心 = 236.505/2 = 118.252
//      ⇒ 台词中线比气泡中线**低 17.28 px**、左沿在**底图中心左侧 135.84 px**。
//    ⛔ 期望值写死在这里（原版 prefab 换算出来的），**不读** `UnitChatPanel.TextLeft/TextCy` ——
//      读它们就是拿实现证明实现。
//
//  E **大聊天 = 空桩**（VA 复核：`@0x180960580`「文案非空 ∧ sprite 非空」**直接 ret**，没有第三个调用）
//    ⇒ 被调用、**什么也不显示**（`Shown` 恒 0）。谁把它改回可见 ⇒ 红。
//
// ════════════════════════ 怎么接（⚠️ 本代理不动宿主文件）════════════════════════
//  宿主建议 = `Editor/BattleScene.cs` 的**语音条那一节末尾**（`B117F2_Assert.Run(chat)` 那一处之后），
//  三行即可（⛔ 本代理的写权限不含 `Editor/*Scene.cs`，由调度台接线）：
//      chat.HideAll();
//      B117G1022_Assert.Run(chat);
//      B117G1022_Assert.RunBigChatStub();
//  收尾把 `pass += B117G1022_Assert.Pass; fail += B117G1022_Assert.Fail;`。
//  ⚠️ `Run(chat)` 会在**第 2 颗（`RadioChat`）**上真的说两句（造的是临时 `AudioClip`，不读工程里的语音资源）
//     ⇒ 它必须排在本节**其它聊天断言之后**（末尾自己会 `HideBox(2)` 收回）。
//
// ════════════════════════ 🔴 灭自证（哪一处改动会让哪一条红）════════════════════════
//  · A 的 volume/priority：改回 `0.85` / 不写 `priority`（默认 128）⇒ 那两条红。
//  · B 的短 clip：把 `Duration` 改回 `clamp(len,1,4)+0.4` ⇒ 0.25 s 那条读出 1.4 ⇒ 红。
//  · C 的第二条：把 `voiceMissing` 并回 `expectsVoice`（改前那版）⇒ 缺口计数不动 ⇒ 红。
//  · D：把 `TextCy` 改回 `134.55` ⇒ 差值变 16.30 ⇒ 红；只改左沿不动中线 ⇒ 第二条红。
//  · E：`BattleBigChat.Display` 里把 `Shown++` 加回来 / 真的去 `Speak` ⇒ `Shown == 0` 那条红。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B117-G10-22`：战斗内聊天（语音条 AudioSource / 停留时长 / 文字与语音两档 / 文字框落点 / 大聊天空桩）。</summary>
public static class B117G1022_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B117G1022] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ==================================================================
    //  A + B + C + D：都要那颗活着的 `UnitChatPanel`
    // ==================================================================
    /// <summary>A/B/C/D 四段一次跑完。`chat` = 宿主手里那颗 `UnitChatPanel`（`BattleDriver.UnitChat`）。
    /// <para>⚠️ 本段会**真的说两句**（第 2 颗 `RadioChat`）并在末尾 `HideBox(2)` 收回 —— 必须接在聊天那一节的**末尾**。</para></summary>
    public static void Run(UnitChatPanel chat)
    {
        if (chat == null)
        {
            S("🆕 B117-G10-22 · 语音条（拿到 `UnitChatPanel` 了吗）");
            T(false, "（前提）拿到 `UnitChatPanel` 实体（`chat == null` ⇒ 本段什么也没验）");
            return;
        }
        RunAudioSource(chat);
        RunTextGeometry(chat);
        RunTimingAndTextAudio(chat);
    }

    // ── A：每颗气泡自己的 AudioSource 逐字段照原版 ──────────────────────
    static void RunAudioSource(UnitChatPanel chat)
    {
        S("🆕 B117-G10-22 · A：气泡 AudioSource = 原版 `m_Volume 1.0` / `Priority 154` / 2D");
        for (int side = 0; side < 3; side++)
        {
            var bg = chat.BubblePieceForTest(side, 0);          // part 0 = 底图（`_ugui` 档才有 RectTransform）
            T(bg != null && bg.gameObject != null, $"（前提）第 {side} 颗气泡的底图建出来了（原版 `Background`）");
            var a = (bg != null && bg.gameObject != null) ? bg.gameObject.GetComponent<AudioSource>() : null;
            T(a != null, $"（前提）第 {side} 颗的 `AudioSource` 挂在**它自己**的底图上（原版每颗 `VoiceLineBox` 各一颗）");
            if (a == null) continue;
            T(Mathf.Abs(a.volume - 1f) <= 1e-4f,
              $"第 {side} 颗 `AudioSource.volume = 1.0`（原版 `m_Volume`；实得 {a.volume:F3}）");
            T(a.priority == 154,
              $"第 {side} 颗 `AudioSource.priority = 154`（原版 `Priority`；实得 {a.priority}）");
            T(!a.loop && !a.mute && !a.playOnAwake,
              $"第 {side} 颗 `loop / mute / playOnAwake` 全 false（原版 `Loop / Mute / m_PlayOnAwake`）");
            T(Mathf.Abs(a.spatialBlend) <= 1e-4f,
              $"第 {side} 颗 `spatialBlend = 0`（原版 `Spatialize = false`；实得 {a.spatialBlend:F3}）");
        }
    }

    // ── D：台词文字框的落点（中线 135.53 / 左沿 188.55）────────────────
    static void RunTextGeometry(UnitChatPanel chat)
    {
        S("🆕 B117-G10-22 · D：台词文字框落点（原版 `RectTransform_3566` 逐字段换算）");
        var bg = chat.BubblePieceForTest(0, 0);
        var txt = chat.BubbleTextForTest(0);
        var tRT = txt != null ? txt.transform as RectTransform : null;
        T(bg != null && bg.Rect != null && tRT != null,
          "（前提）我方气泡的底图与台词都在 **uGUI 画布**上（世界档没有 `RectTransform` ⇒ 本段不成立）");
        if (bg == null || bg.Rect == null || tRT == null) return;

        // 同一颗气泡里两件用的是**同一个点锚**（`Move` 的 ax/ay）⇒ `anchoredPosition` 之差就是设计 px 之差。
        float dy = bg.Rect.anchoredPosition.y - tRT.anchoredPosition.y;   // y 向上：台词更低 ⇒ 正
        T(Mathf.Abs(dy - 17.28f) <= 0.05f,
          $"台词中线比气泡中线**低 17.28 px**（原版 135.53 − 236.505/2；实得 {dy:F3}）");
        float dx = tRT.anchoredPosition.x - bg.Rect.anchoredPosition.x;
        T(Mathf.Abs(dx - (-135.84f)) <= 0.05f,
          $"台词节点（`B117-R1` 之后 = **墨迹左沿**）在底图中心左侧 **135.84 px**（原版 324.387 − 188.55；实得 {dx:F3}）");
    }

    // ── B + C：停留时长 / 「只出字不出声」的两档 ────────────────────────
    static void RunTimingAndTextAudio(UnitChatPanel chat)
    {
        S("🆕 B117-G10-22 · B/C：停留时长 = `max(2, len+0.3)` · 无语音也出字 · 缺口要出声");
        // 造两条**临时** clip（不读工程里的语音资源；`length` = samples / frequency）
        var shortClip = AudioClip.Create("b117g1022_short", 11025, 1, 44100, false);   // 0.25 s
        var longClip = AudioClip.Create("b117g1022_long", 132300, 1, 44100, false);  // 3.0 s
        T(shortClip != null && Mathf.Abs(shortClip.length - 0.25f) < 0.005f,
          $"（前提）临时短 clip 的 `length = 0.25 s`（实得 {(shortClip != null ? shortClip.length : -1f):F3}）");
        if (shortClip != null && longClip != null)
        {
            // evt = "Attack"：`ChatTermSuffix` 对它返回 null ⇒ 不查官方词条（不刷 `Loc.T` 的缺键日志）
            bool ok1 = chat.Speak(2, "B117G1022S", "Attack", null, "ShortTest", "short", shortClip,
                                  null, null, null, false, true);
            T(ok1 && chat.BubbleShownForTest(2), "（前提）第 2 颗（`RadioChat`）说起来了");
            T(Mathf.Abs(chat.TimeLeft - 2.0f) < 0.01f,
              $"短 clip（0.25 s）⇒ 停留仍是 **2.0 s 下限**（原版 `max(2, len+0.3)`；改前 `clamp(len,1,4)+0.4` 会给 1.4 ⇒ 灭自证；实得 {chat.TimeLeft:F3}）");

            bool ok2 = chat.Speak(2, "B117G1022L", "Attack", null, "LongTest", "long", longClip,
                                  null, null, null, false, true);
            T(ok2, "（前提）长 clip 那一句也说起来了");
            T(Mathf.Abs(chat.TimeLeft - 3.3f) < 0.01f,
              $"长 clip（3.0 s）⇒ 停留 **3.3 s**（= len + 0.3；实得 {chat.TimeLeft:F3}）");
        }

        // C①：没有音频 ⇒ **照样出字**（原版 `ShowChat` 无条件设 `data.ChatMessage`）
        chat.ResetVoiceMissingWarningsForTest();
        int before = chat.VoiceMissingWarnCountForTest;
        chat.Speak(2, "B117G1022N", "Attack", null, "FallbackName", "still-text", null,
                   null, null, null, false, false);      // expectVoice:false = 「这一档本来就没声」
        T(chat.BubbleShownForTest(2), "无音频也**照样显示气泡**（原版 `ShowChat` 不看 clip）");
        T(!chat.BubbleAudioPlayingForTest(2), "这一档没有音频在播（`AudioSource.isPlaying = false`）");
        T(chat.VoiceMissingWarnCountForTest == before,
          $"（灭自证）`expectVoice:false` **不许**算成「真实资源缺口」⇒ 计数不动（实得 {chat.VoiceMissingWarnCountForTest}，改前 {before}）");

        // C②：该有声、clip 却取不到 ⇒ 单独出声一次（改前与上一档共用同一个 false ⇒ 静默）
        chat.Speak(2, "B117G1022M", "Attack", null, "MissingName", "missing-voice", null,
                   null, null, null, false, true);       // expectVoice:true + clip == null
        T(chat.VoiceMissingWarnCountForTest == before + 1,
          $"（灭自证）`expectVoice:true` 且 clip 取不到 ⇒ **缺口计数 +1**（实得 {chat.VoiceMissingWarnCountForTest}，期望 {before + 1}）");

        chat.HideBox(2);                                   // 收回，别把气泡留给后面的断言
        T(!chat.BubbleShownForTest(2), "（收尾）第 2 颗已收起");
        if (shortClip != null) Object.DestroyImmediate(shortClip);
        if (longClip != null) Object.DestroyImmediate(longClip);
    }

    // ==================================================================
    //  E：大聊天 = 空桩（VA `@0x180960580` 正常路径直接 ret）
    // ==================================================================
    /// <summary>E 段入口 —— **不需要**面板实体（原版重载 B 只碰文案与 sprite 两个入参）。</summary>
    public static void RunBigChatStub()
    {
        S("🆕 B117-G10-22 · E：大聊天 = 空桩（`Shown` 恒 0）");
        BattleBigChat.ResetCounters();
        var tex = new Texture2D(2, 2);

        T(BattleBigChat.Display(null, "", tex), "空文案 ⇒ 返回 true（= 已按原版处理完，**不是**显示了）");
        T(BattleBigChat.LastError == "empty", $"空文案那一支 = `empty`（原版 `{BattleBigChat.EmptyChatWarning}`；实得 `{BattleBigChat.LastError}`）");

        T(BattleBigChat.Display(null, "hi", (Texture2D)null), "立绘 null ⇒ 返回 true");
        T(BattleBigChat.LastError == "noportrait", $"立绘 null 那一支 = `noportrait`（原版 `{BattleBigChat.NoUnitError}`；实得 `{BattleBigChat.LastError}`）");

        T(BattleBigChat.Display(null, "hi", tex), "文案与立绘都齐 ⇒ 正常路径返回 true");
        T(BattleBigChat.LastError == null, $"正常路径不该留下错误档（实得 `{BattleBigChat.LastError}`）");
        T(BattleBigChat.Shown == 0,
          $"（灭自证）**一句都没显示**：`Shown` 恒 0（实得 {BattleBigChat.Shown}）—— 谁把这件改回可见（`Shown++` / 真去 `Speak`）就红");
        T(!BattleBigChat.LastPortraitApplied,
          "（灭自证）立绘**没有被换上**（`ApplyPortrait` 在空桩路径上不许被调用；实得 " + BattleBigChat.LastPortraitApplied + "）");

        Object.DestroyImmediate(tex);
    }
}
