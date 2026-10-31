// B109_E2_Booster_Assert.cs — 执行代理 `B109_E2`（2026-10-31 · 第一百零九会话第 1 波）
//
// ============================ 它盯的是什么：开包时间线的**起点** ============================
//  原版：`BoosterPackOpenWindow.Initialize` 实例化卡包实物（根上 `Animation` 的 `m_PlayAutomatically = 1`）
//        ⇒ 那条 `Booster Open <阵营>` clip 自己播；**播到 t = 2.0** 时打 `StartCardAnimationEvent`
//        ⇒ 才转调 `BoosterPackOpen()`（`windowAnimation.Play()` + 背景 shake）⇒ 0.6167 s 后
//        `AnimationEndEvent` 把 5 张卡 `ChangeState(2)`（可点）。
//  我们（修前）：`Build()` 之后 `Advance` **第一拍**就把开包时间线跑起来了 ⇒ **比原版早 2 s**。
//  本文件断的就是「早于 t=2.0 时牌面动画一步都没走 / 事件到了才走」。
//
// ============================ 判据出处（2026-10-31 本代理现读）============================
//  ① `d:/2/tools/decomp_full/BoosterPackOpenWindow__BoosterPackOpen.c`
//       `if (*(this + 0x98) != 0) { Animation.Play(this + 0x98, 0);
//                                   Animation.Play(this + 0xb0, <"Booster Window - Background Shake On Open">); }`
//     —— 就是 `windowAnimation.Play()` + `backgroundAnimation.Play(名字)`；**没有**自己 begin 的路径。
//  ② `d:/2/tools/decomp_full/BoosterPackVisualItem__StartCardAnimationEvent.c`
//       `lVar1 = *(this + 0x48)`（= `Initialize` 存下来的宿主窗）→ 对 `lVar1 + 0x98 / +0xb0` 做同样两次 `Play`。
//       ⇒ 这个事件的**唯一效果**就是「让开包时间线开始」。
//  ③ `d:/2/tools/decomp_full/BoosterPackOpenWindow__AnimationEndEvent.c`：逐个
//       `CardInBoosterPack__ChangeState(card, 2)` —— 开包 clip 播完 ⇒ 5 张卡可点。
//  ④ 事件时刻 / clip 长度（数据备份，`Resources/misc_clips.json`，生成器 `工具/gen_misc_clips.py`）：
//       `Booster Open Standard`   `length = 2.366666555404663` · `events = [{t: 2.0, function: "StartCardAnimationEvent"}]`
//       `Booster Open Sororitas`  `length = 2.0`                · `events = [{t: 2.0, function: "StartCardAnimationEvent"}]`
//     ⇒ 🔴 **2.0 = AnimationEvent 自己的时间，不是 clip 长度**（Standard 末帧 2.3667、事件仍在 2.0）。
//  ⑤ 开包 clip `Booster Window Open` 的时长 = `BoosterPackAnim.WindowOpenDuration`（0.6166667）；
//     缩放曲线 = `BoosterPackAnim.WindowOpenScale`（t ≤ 0.2167 是 157.58151/151，末帧收到 1）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  🧨 把 `Advance` 里那句 `if (OpenTimelineGate())` 改回 `if (!WindowOpenDone)`（= 修前行为）⇒ **B 段全红**
//     （B1/B3/B5：事件还没来时间线就跑了；B2：卡节点被开包 clip 缩了一截）。
//  🧨 把 `StartOpenAnimations` 里 `_openRequested = true;` 删掉（事件不再起播）⇒ **C 段全红**、D2 红。
//  🧨 把 `Advance` 里 `_openT += dt` 那段删掉（时间线永远不动）⇒ C2/C4 红（B 段仍绿 —— 两面结构上互斥）。
//  🧨 把 `Build` 里 `_openRequested = false` 删掉 ⇒ D0（重开一包必须重新等）红。
//  🧨 把 `OpenTimelineGate` 的兜底删掉 ⇒ D3 红（包实物取不到时窗会永久卡死）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  建议接在 `Editor/ShopScene.cs` 的聚合行（开包窗真正的生产路径在商店）；`ShellScene` 亦可（本文件自带夹具）。
//  追加写法（同一物理行末尾，⛔ 别加行）：
//      B109_E2_Booster_Assert.Run(); _sink.Pass += B109_E2_Booster_Assert.Pass; _sink.Fail += B109_E2_Booster_Assert.Fail;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B109_E2`：开包时间线的起点必须由包实物 clip 的 `StartCardAnimationEvent`（t=2.0）触发，
/// 而不是 `Build()` 之后第一拍。见文件头。</summary>
public static class B109_E2_Booster_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void E<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    static BoosterPackOpenWindow _win;
    static GameObject _camGo;

    /// <summary>`Booster Window Open` 的时长（判据 = clip 末帧 0.6166667，这里写**原版字面量**，
    /// ⛔ 不读 `BoosterPackAnim.WindowOpenDuration` 当期望值 —— 那是被测实现自己）。
    /// 断言里用它推「开包 clip 播完」那一拍。</summary>
    const float OpenClipLen = 0.6166667f;

    /// <summary>`Booster Window Open` 首段缩放倍率（`157.58151 / 151`，判据 = clip `m_ScaleCurves`）。</summary>
    const float OpenClipPeak = 157.58151f / 151f;

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B109_E2：开包时间线的起点 = 包实物 clip 的 t=2.0 事件（账 `锻造:24`）");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  A · 前提（硬断言；⛔ 不用 `if (x != null) Check(...)` —— 那会让前提不成立时整段静默跳过）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("A 前提：一扇真窗 + 包实物 + 它自带的那条 clip");
        BuildFixture();

        var win = _win;
        T(win != null, "（A0 前提）窗口建出来了");
        if (win == null) { Cleanup(); S("A0 不成立 ⇒ 后面全跳过（已在上一条红）"); return; }

        T(win.Cards != null && win.Cards.Length == 5, "（A0 前提）这一包正好 5 张卡");
        var item = win.VisualItem;
        T(item != null, "（A1 前提）卡包实物建出来了（原版 `Instantiate(prefab, boosterPackAnchor)`）");

        var anim = item != null ? item.GetComponent<Animation>() : null;
        T(anim != null, "（A1 前提）实物根上有 `Animation` 组件（原版 prefab 实读 `m_PlayAutomatically = 1`）");
        var clip = anim != null ? anim.clip : null;
        T(clip != null, "（A1 前提）那颗 `Animation` 上有 clip（= `Booster Open <阵营>`）");

        // A2 ★ 判据锚：事件在 t=2.0（这一条是**数据面**，clip 自带，⛔ 不来自我们的常量）
        bool found = false; float at = -1f;
        if (clip != null && clip.events != null)
            for (int i = 0; i < clip.events.Length; i++)
                if (clip.events[i].functionName == "StartCardAnimationEvent") { found = true; at = clip.events[i].time; }
        T(found, "★ A2 clip 里有 AnimationEvent `StartCardAnimationEvent`");
        N(at, 2.0f, 1e-3f,
          "★ A2 它在 **t = 2.0 s**（判据 = `misc_clips.json` 两条 `Booster Open *` 的 `events[]`；"
          + "⚠️ **不是 clip 长度** —— Standard 末帧 2.3667）");

        // A3 出厂态：Build 之后**什么都不该播**
        T(win.OpenTimelineRequested == false, "★ A3 `Build()` 之后开包时间线**还没开始**（`OpenTimelineRequested == false`）");
        N(win.OpenT, 0f, 1e-6f, "★ A3 `OpenT` = 0（开包 clip 一秒都没走）");
        T(!win.WindowOpenDone, "★ A3 `WindowOpenDone == false`");
        T(AllBack(win), "★ A3 5 张卡都在 **state 1（卡背态）**（原版 `Initialize` 末尾 `ChangeState(1)`）");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  B · 早于 t=2.0（事件没来）⇒ 牌面动画一步都不许动
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("B 事件没来时：`Advance` 推多久都不许起播");
        win.Advance(0.2f);
        T(win.OpenTimelineRequested == false, "★ B1 推 0.2 s：时间线**仍未开始**（修前这里已经起播了）");
        N(win.OpenT, 0f, 1e-6f, "★ B1 推 0.2 s：`OpenT` 仍 = 0");
        T(win.WindowOpenDone == false, "★ B1 推 0.2 s：`WindowOpenDone == false`");
        T(AllSlotsAtScaleOne(win), "★ B2 推 0.2 s：5 个卡节点 `localScale` 仍 = **1**"
                                   + "（修前这里会被开包 clip 缩到 " + OpenClipPeak.ToString("F6") + " 倍）");

        win.Advance(1.8f);                                   // 累计 2.0 s
        T(win.OpenTimelineRequested == false, "★ B3 累计推 **2.0 s**（= 原版事件那一拍的时刻）：时间线**仍未开始**");
        N(win.OpenT, 0f, 1e-6f, "★ B3 累计 2.0 s：`OpenT` 仍 = 0");
        T(win.WindowOpenDone == false, "★ B3 累计 2.0 s：`WindowOpenDone == false`");
        T(AllBack(win), "★ B4 累计 2.0 s：5 张卡仍全 = state 1（可点不了）");
        T(AllSlotsAtScaleOne(win), "★ B4 累计 2.0 s：5 个卡节点仍没被缩");

        win.Advance(1f);                                     // 累计 3.0 s（再多推也没用 —— 起点不是墙钟）
        T(win.OpenTimelineRequested == false && !win.WindowOpenDone,
          "★ B5 累计 3.0 s：**仍然没起播**（起点是 clip 的事件，不是我们的钟）");
        T(win.Dump().Contains("等包炸完"),
          "★ B5 `Dump()` 把这一档如实说成「等包炸完 (t=2.0)」（⛔ 不冒充 `0.000s`）");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  C · 事件到达之后才起
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("C 事件到达（= 原版 t=2.0 的 `StartCardAnimationEvent`）之后才起");
        int startsBefore = win.OpenAnimStarts;
        if (item != null) item.StartCardAnimationEvent();
        else T(false, "（C0 前提）包实物在 ⇒ 才能打这一拍（已在上一条红）");
        E(win.OpenAnimStarts, startsBefore + 1, "★ C1 事件转发到窗（`OpenAnimStarts` +1，= 原版 `Play()` 那一跳）");
        T(win.OpenTimelineRequested == true, "★ C1 事件到达 ⇒ 时间线**现在**才被允许起播");
        N(win.OpenT, 0f, 1e-6f, "★ C1 事件那一拍 `OpenT` 还是 0（`Play()` 从第 0 帧开始）");
        T(!win.WindowOpenDone, "★ C1 事件那一拍开包 clip 还没播完");

        win.Advance(0.1f);
        N(win.OpenT, 0.1f, 1e-4f, "★ C2 事件后推 0.1 s ⇒ `OpenT` = 0.1");
        N(SlotScale(win, 0), OpenClipPeak, 1e-4f,
          "★ C2 …卡节点被开包 clip 缩到 **157.58151 / 151**（⛔ 不是 1 —— 这一条与 B2 结构上互斥）");
        T(AllBack(win), "★ C2 …但 5 张卡还没到可点态（开包 clip 未播完）");

        win.Advance(0.5f);                                   // 累计 0.6 s
        T(!win.WindowOpenDone, "★ C3 累计 0.6 s（< 0.6167）：开包 clip **还没**播完");
        T(AllBack(win), "★ C3 …5 张仍 state 1");

        win.Advance(0.05f);                                  // 累计 0.65 s
        T(win.WindowOpenDone, "★ C4 累计 0.65 s（> 0.6167）⇒ 开包 clip 播完（原版 `AnimationEndEvent`）");
        T(AllReady(win), "★ C4 …5 张全部转 **state 2（可交互）**（原版逐张 `ChangeState(2)`）");
        T(AllSlotsAtScaleOne(win), "★ C4 …5 个卡节点回到 **1**（`Booster Window Open` 末帧）");

        win.Advance(0.5f);
        T(win.WindowOpenDone && AllReady(win), "★ C5 再推不改状态（时间线跑完就停在那）");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  D · 灭自证：重开一包必须**重新等**；事件确实驱动时间线；原版走不到的那档不卡死
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("D 重开一包 / 兜底（结构上互斥的两面）");
        win.Show(0, 3);                                      // 重开一包（= 商店再买一件）
        T(win.OpenTimelineRequested == false && !win.WindowOpenDone,
          "★ D0 重开一包 ⇒ 时间线**回到「等事件」**（`Build()` 复位）");
        win.Advance(3f);
        T(win.OpenTimelineRequested == false && !win.WindowOpenDone && AllBack(win),
          "★ D1 重开后再推 3 s（不打事件）⇒ **仍然不动**"
          + "｜🧨 任何「Build 后自己起播」的实现都会让这一条红（与 C4 互斥）");

        var item2 = win.VisualItem;
        T(item2 != null, "（D2 前提）重开之后包实物还在");
        if (item2 != null) item2.StartCardAnimationEvent();      // 前提不成立时**不静默跳过**：下面两条照样执行、照样红
        win.Advance(0.1f);                                       // 停在前段（t ≤ 0.2167 ⇒ 缩放恒是峰值那一档）
        N(win.OpenT, 0.1f, 1e-4f, "★ D2 事件一到就真的跑（`OpenT` = 0.1）"
                                 + "｜🧨 「事件不影响时间线」的实现 ⇒ 这一条红（与 B1/B2 互斥）");
        N(SlotScale(win, 0), OpenClipPeak, 1e-4f, "★ D2 …卡节点真的被缩了（与 B2 互斥）");

        // D3 兜底：原版**走不到**的一档（包实物/clip 取不到 ⇒ 那条事件永远不来）。
        //    我们不让窗永久卡死 ⇒ 立刻起播 + 出声（判据与「谁定的」见 `BoosterPackOpenWindow.OpenTimelineGate`）。
        win.Show(0, 3);
        var item3 = win.VisualItem;
        T(item3 != null, "（D3 前提）重开之后包实物在（下面才把它拆掉）");
        if (item3 != null) Object.DestroyImmediate(item3.gameObject);   // 模拟「取不到 / 重打包丢了」那档
        T(win.VisualItem == null, "（D3 前提）包实物已经不在（Unity 伪 null）");
        win.Advance(0.1f);
        T(win.OpenTimelineRequested,
          "★ D3 包实物取不到 ⇒ 兜底**立刻起播**（`OpenTimelineRequested == true`）"
          + "｜⛔ 不这么做窗会永久卡死：5 张卡永远点不动、`Tap to close` 永远不出现"
          + "｜🔶 这一档原版走不到（prefab/clip 是序列化引用）");
        win.Advance(0.6f);
        T(win.WindowOpenDone && AllReady(win),
          "★ D3 …兜底那条时间线正常跑完 ⇒ 5 张卡可点（**不永久卡死**）");

        Cleanup();
        S("小结：✓ " + _sink.Pass + " · ✗ " + _sink.Fail
          + "（「跑完之后再打事件不重播」那条留在 `B98A5_Assert` §6，⛔ 这里不写第二份）");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  夹具：与 `B98A5_Assert` §2 / `B102PACK_Assert` 同一件（第 0 页第 4 件 = 一包 Leviathan）
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void BuildFixture()
    {
        BoosterPackAssets.Reset();
        WindowSounds.ResetCounters();
        BoosterPackOpenWindow.ResetReOpenProtectionForTest();      // 静态跨窗计数（防连开抖）推回出厂值

        _camGo = new GameObject("Main Camera (B109_E2)");
        _camGo.tag = "MainCamera";
        var cam = _camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.aspect = LayoutSpace.DesignAspect;
        LayoutSpace.Apply(cam);

        var go = new GameObject("Booster Pack Open Window (B109_E2)");
        _win = go.AddComponent<BoosterPackOpenWindow>();
        _win.Show(0, 3);                                          // 第 0 页第 4 件 = Leviathan
    }

    static void Cleanup()
    {
        if (_win != null) { Object.DestroyImmediate(_win.gameObject); _win = null; }
        if (_camGo != null) { Object.DestroyImmediate(_camGo); _camGo = null; }
    }

    // ---- 读口（都是公开字段/属性，⛔ 不去反射私有成员）----
    static bool AllBack(BoosterPackOpenWindow w)
    {
        for (int i = 0; i < 5; i++)
            if (w.CardStates[i] != (int)BoosterPackOpenWindow.CardState.Back) return false;
        return true;
    }

    static bool AllReady(BoosterPackOpenWindow w)
    {
        for (int i = 0; i < 5; i++)
            if (w.CardStates[i] != (int)BoosterPackOpenWindow.CardState.Ready) return false;
        return true;
    }

    static bool AllSlotsAtScaleOne(BoosterPackOpenWindow w)
    {
        for (int i = 0; i < 5; i++)
        {
            var t = w.SlotNodes[i];
            if (t == null) return false;
            if (Mathf.Abs(t.localScale.x - 1f) > 1e-5f
                || Mathf.Abs(t.localScale.y - 1f) > 1e-5f
                || Mathf.Abs(t.localScale.z - 1f) > 1e-5f) return false;
        }
        return true;
    }

    static float SlotScale(BoosterPackOpenWindow w, int i)
    {
        var t = w.SlotNodes[i];
        return t != null ? t.localScale.x : -1f;
    }
}
