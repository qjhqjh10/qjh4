// B98A5_Assert.cs — 第九十八会话 · 波 5a · 执行代理 `B98_A5`（账 `锻造:23`：卡包实物 `BoosterPackVisualItem`）
//
// ============================ 它盯的是什么 ============================
// 一件活：原版开包窗中心那件**独立的 3D 卡包实物**（`BoosterPackVisualItem`，17 棵 prefab）：
//   ① 它**建出来了没有**、挂在原版那个锚点（`Booster Animation Parent` = `boosterPackAnchor`）上；
//   ② `Initialize` 的五步（I18 取字 → 存宿主窗 → 写 `armyTexts[]` → `Play2D(…, Jingles)` → 逐条抖）走没走；
//   ③ 它自带的那条 2 s clip（炸包 + t=2.0 的 `StartCardAnimationEvent`）在不在、转发有没有接上；
//   ④ 抖**真的落到了卡容器**上（本工程没有可抖的相机 —— 等价物见窗口的 `_bursts` / `StepBursts`）。
// 🔴 **顺序是有讲究的**：第 4 段（时间线那一拍）必须在第 5 段（抖 / 时钟）**之前**跑 ——
//    前者要求 `WindowOpenDone == false`，而后者会 `Advance` 过 0.6167 s 把时间线跑完。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B98A5_Assert.Run -logFile "d:/4/_tmp_view/b98a5.log"
//   判绿 = 小结那行 `✗` 为 0。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · `BoosterPackOpenWindow.BoosterTermOf("Sautekh")` 改成 `Armies/Sautekh` ⇒ 第 1 段红
//     （🔴 2026-10-30 就地订正（铁律 5）：原来这里写「官方表里没这个键」——**实测有**。`Armies/Sautekh`
//      与 `Armies/Sautekh_short` **两个键都在** `Resources/official_loc_13lang.json`（10,862 键）。
//      ⇒ 这条灭自证**仍然成立**，但红的理由是「got = `Armies/Sautekh` ≠ want = `Armies/Sautekh_short`」，
//      ⛔ 不是「表里查不到」。）。
//   · `OriginalShakePresets()` 里任一条 amplitude / delay 改一个数 ⇒ 第 1、3 段红。
//   · `BuildVisualItemLayer` 里那句 `Instantiate(prefab, anchor)` 删掉 ⇒ 第 2 段「★ 卡包实物建出来了」红。
//   · `BoosterPackVisualItem.Initialize` 里 `t.text = localizedArmyText;` 删掉 ⇒ 第 3 段「两颗 TMP 都写上了字」红。
//   · `Initialize` 里那句 `Loc.T(boosterName)` 删掉 ⇒ 第 3 段「空入参 ⇒ 取 I18 译文」红。
//   · `PlayCameraShake` 的转发（或 `_bursts.Add`）删掉 ⇒ 第 5 段「卡容器真的在抖」红。
//   · `StartOpenAnimations` 里 `OpenAnimStarts++` 删掉 ⇒ 第 4 段红；`_openT = 0f` 删掉 ⇒ 第 4 段的「不再重播」那条**不会**红
//     （它断的是「重播**会**让 `OpenAnimStarts` 再 +1」，见那一段的注释）。
//   · `LiftPackParticles` 删掉 ⇒ 第 5 段「粒子队列提上来了」红（前提：物件子树里确实有粒子）。
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B98A5_Assert
{
    const string P = "B98A5 ";

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    static void CheckEq(string got, string want, string label)
    {
        Check(got == want, label + "（got `" + (got ?? "<null>") + "` · want `" + (want ?? "<null>") + "`）");
    }

    static void CheckEq(float got, float want, string label, float eps = 1e-4f)
    {
        Check(Mathf.Abs(got - want) <= eps, label + "（got " + got.ToString("F4") + " · want " + want.ToString("F4") + "）");
    }

    static BoosterPackOpenWindow _win;
    static GameObject _camGo;

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 卡包实物 `BoosterPackVisualItem` 自检 开始（账 `锻造:23`）===");

        Section1_Data();
        Section2_Build();
        Section3_Initialize();
        Section4_OpenAnimationEvent();      // ⚠️ 必须在第 5 段之前（它要 `WindowOpenDone == false`）
        Section5_ShakeAndParticles();       // 这一段会把时间线 Advance 跑完
        Section6_NoReplay();

        if (_win != null) { Object.DestroyImmediate(_win.gameObject); _win = null; }
        if (_camGo != null) { Object.DestroyImmediate(_camGo); _camGo = null; }

        Debug.Log(P + "=== 小结：✓ " + _ok + " · ✗ " + _bad + " ===");
        if (_bad > 0) Debug.LogError(P + "有 " + _bad + " 条失败");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  1 · 数据面（不建场景）—— 词条键 / 抖动参数 / 缩放 / prefab 对照
    //  判据：17 份 MB 逐条实读 + 官方 13 语词条表 + prefab 根 Transform 17/17
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section1_Data()
    {
        Debug.Log(P + "--- 1 · 数据面 ---");

        // ① `boosterName` 词条键（我们的 4 个阵营 + 全卡池）
        CheckEq(BoosterPackOpenWindow.BoosterTermOf("Ultramarines"), "Armies/Ultramarines", "词条键 · Ultramarines");
        CheckEq(BoosterPackOpenWindow.BoosterTermOf("Sautekh"), "Armies/Sautekh_short",
                "词条键 · Sautekh（**带 `_short`** —— 17 份 MB 里 Sautekh 那一支就是这样）");
        CheckEq(BoosterPackOpenWindow.BoosterTermOf("SpaceWolves"), "Armies/SpaceWolves", "词条键 · SpaceWolves");
        CheckEq(BoosterPackOpenWindow.BoosterTermOf("Leviathan"), "Armies/Leviathan", "词条键 · Leviathan");
        CheckEq(BoosterPackOpenWindow.BoosterTermOf(null), "MenuShop/Alpha/AllArmies", "词条键 · 全卡池（null）");

        // ② 这 5 个键在官方词条表里**逐条命中**（原版 `LocalizationManager.GetTranslation` 的判据面）
        string[] keys = { "Armies/Ultramarines", "Armies/Sautekh_short", "Armies/SpaceWolves",
                          "Armies/Leviathan", "MenuShop/Alpha/AllArmies" };
        // 🔴 2026-10-30 就地订正：原来用 `Loc.HasEntry` —— 它**只认手写 `Table`**（`Core/Loc.cs:3607`），
        //   而这两个键（`Armies/Sautekh_short` / `MenuShop/Alpha/AllArmies`）在手写表里**没有**、
        //   却在官方 13 语全表 `Resources/official_loc_13lang.json`（10,862 键）**里** ⇒ `Loc.T` 的**第三档**兜到。
        //   ⇒ 判「官方词条表里有这条」的口用 `Loc.OfficialEnglish(key) != null`（`Core/Loc.cs:3104`，纯数据面、不看当前语档）。
        for (int i = 0; i < keys.Length; i++) Check(Loc.OfficialEnglish(keys[i]) != null, "官方词条表里有 `" + keys[i] + "`");

        // ③ 两条 shakePresets（17 份 MB 逐字节相同）
        var sh = BoosterPackOpenWindow.OriginalShakePresets();
        Check(sh != null && sh.Length == 2, "（前提）shakePresets 是 **2** 条");
        if (sh != null && sh.Length == 2)
        {
            CheckEq(sh[0].delay, 1.0f, "preset[0].delay");
            CheckEq(sh[0].amplitude, 10.0f, "preset[0].amplitude");
            CheckEq(sh[0].frequency, 10.0f, "preset[0].frequency");
            CheckEq(sh[0].sustainTime, 1.0f, "preset[0].sustainTime");
            Check(sh[0].overwriteAmplitude && sh[0].overwriteFrequency && sh[0].overwriteSustainTime,
                  "preset[0] 三个 overwrite 都是 1（MB 实读）");
            Check(!sh[0].overwriteAttackTime && !sh[0].overwriteDirection,
                  "preset[0] 的 attackTime / direction **没被覆盖**（= 取 presetSO 缺省值，本机查不到 ⇒ 我们留 0）");

            CheckEq(sh[1].delay, 2.0f, "preset[1].delay（= 炸包那一拍）");
            CheckEq(sh[1].amplitude, 100.0f, "preset[1].amplitude");
            CheckEq(sh[1].frequency, 1.0f, "preset[1].frequency");
            CheckEq(sh[1].sustainTime, 0.25f, "preset[1].sustainTime");
            CheckEq(sh[1].decayTime, 0.5f, "preset[1].decayTime");
            Check(sh[1].overwriteDecayTime, "preset[1] 的 decayTime **被覆盖**（MB 实读）");
        }

        // ④ 缩放 / 位移的换算（原版 150 画布单位 · 本工程 108 px/单位）
        CheckEq(BoosterPackOpenWindow.VisualItemScale, 150f / 108f, "prefab 根 scale 150 ⇒ 世界单位 150/108");
        var off = BoosterPackOpenWindow.VisualItemOffsetPx;
        CheckEq(off.x, 7.547150f, "prefab 根 localPosition.x（画布单位）", 1e-3f);
        CheckEq(off.y, 8.989313f, "prefab 根 localPosition.y（画布单位）", 1e-3f);
        CheckEq(off.z, -14.145021f, "prefab 根 localPosition.z（画布单位 —— **不作为我们的 z 用**，见 VisualItemZ）", 1e-3f);

        // ⑤ 阵营 → prefab 名（取件键；`BoosterPackAssets` 是唯一入口）
        string n;
        Check(BoosterPackAssets.TryGetPrefabForArmy("Leviathan", out n) && n == "Booster Pack Standard Tyranids Leviathan",
              "阵营 Leviathan ⇒ `Booster Pack Standard Tyranids Leviathan`");
        Check(BoosterPackAssets.TryGetPrefabForArmy("Sautekh", out n) && n == "Booster Pack Standard Necrons Sautekh",
              "阵营 Sautekh ⇒ `Booster Pack Standard Necrons Sautekh`");
        Check(BoosterPackAssets.TryGetPrefabForArmy(null, out n) && n == BoosterPackAssets.StandardPrefab,
              "全卡池（null）⇒ `Booster Pack Standard`");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  2 · 建一扇真窗（第 0 页第 4 件 = Leviathan，与商店自检同一件）⇒ 卡包实物建出来了没有
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section2_Build()
    {
        Debug.Log(P + "--- 2 · 建窗 + 卡包实物 ---");

        BoosterPackAssets.Reset();
        WindowSounds.ResetCounters();

        _camGo = new GameObject("Main Camera (B98A5)");
        _camGo.tag = "MainCamera";
        var cam = _camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.aspect = LayoutSpace.DesignAspect;
        LayoutSpace.Apply(cam);

        var go = new GameObject("Booster Pack Open Window (B98A5)");
        _win = go.AddComponent<BoosterPackOpenWindow>();
        _win.Show(0, 3);

        CheckEq(_win.Army, "Leviathan", "（前提）第 0 页第 4 件 ⇒ 阵营 Leviathan（与 `Editor/ShopScene.cs` 同一件）");
        Check(_win.AnchorNode != null, "（前提）`Booster Animation Parent`（= 原版 `boosterPackAnchor`）建出来了");
        Check(_win.Cards != null && _win.Cards.Length == 5, "（前提）这一包 5 张卡");
        Check(_win.VisualItem != null, "★ 卡包实物建出来了（原版 `Instantiate(prefab, boosterPackAnchor)`）");

        if (_win.VisualItem == null)
        {
            Debug.LogError(P + "卡包实物没建出来 ⇒ 第 3~6 段无法执行（见上面那条 `[BoosterPack]` 警告的根因）");
            return;
        }

        CheckEq(_win.VisualItemPrefab, "Booster Pack Standard Tyranids Leviathan", "用的是 Leviathan 那一棵 prefab");
        CheckEq(_win.VisualItem.gameObject.name, "BoosterPackVisualItem", "节点名");
        Check(_win.VisualItem.transform.parent == _win.AnchorNode,
              "挂在 `Booster Animation Parent` 下（原版 `Instantiate(prefab, boosterPackAnchor)`）");
        Check(_win.VisualItem.Renderer != null, "（前提）原版 `myRenderer` 绑上了（包面的 MeshRenderer）");
        CheckEq(_win.VisualItem.transform.localScale.x, 150f / 108f, "实物 scale = 150/108");
        CheckEq(_win.VisualItem.transform.localPosition.z, BoosterPackOpenWindow.VisualItemZ, "实物 z = `VisualItemZ`");
        CheckEq(_win.VisualItem.transform.localPosition.x, 7.547150f / 108f, "实物 x = 7.54715 / 108", 1e-5f);
        CheckEq(_win.VisualItem.transform.localPosition.y, 8.989313f / 108f, "实物 y = 8.989313 / 108", 1e-5f);

        var cards = _win.AnchorNode.Find("Cards");
        Check(cards != null, "（前提）`Booster Animation Parent/Cards` 在");
        if (cards != null)
            CheckEq(cards.localPosition.z, BoosterPackOpenWindow.CardsFrontZ, "5 张卡那层在实物**前面**（z = CardsFrontZ）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  3 · `Initialize(null, this)` 的五步
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section3_Initialize()
    {
        Debug.Log(P + "--- 3 · Initialize 五步 ---");
        var item = _win != null ? _win.VisualItem : null;
        if (item == null) { Check(false, "（前提）卡包实物在 ⇒ 本段可跑"); return; }

        Check(item.InitializeCount == 1, "`Initialize` 只被调了 **1** 次（原版窗口 `Initialize` 里那一跳）");
        Check(item.Window == _win, "② 宿主窗存下来了（原版 `this.boosterPackOpenWindow = 入参`）");

        // ① 入参是 null ⇒ 取 `boosterName` 的 I18 译文（原版 `LocalizationManager.GetTranslation(boosterName)`）
        string want = Loc.T("Armies/Leviathan");
        CheckEq(item.ArmyText, want, "① 空入参 ⇒ 写进去的是 `Loc.T(\"Armies/Leviathan\")` 的译文");
        CheckEq(item.BoosterName, "Armies/Leviathan", "`boosterName` = `Armies/Leviathan`");

        // ③ 两颗世界空间 TMP 都写了同一串字
        var texts = item.ArmyTexts;
        Check(texts != null && texts.Length == 2, "（前提）`armyTexts` 是 **2** 颗（原版 `TextMeshPro[2]`）");
        // 🔴 2026-10-30：原来这三条包在 `if (texts != null && texts.Length == 2) { … }` 里 ⇒ 前提不成立时
        //   **三条静默跳过**（文件头自己就写着「⛔ 不用 `if(前提) Check(结论)`」）。改成「前提折进每条的条件里」：
        //   前提那条（上一行）照旧硬断，这三条在前提不成立时**各报一条红**，而不是消失。
        bool armyTextsOk = texts != null && texts.Length == 2;
        Check(armyTextsOk && texts[0] != null && texts[0].text == want, "③ `armyTexts[0].text` 写对了");
        Check(armyTextsOk && texts[1] != null && texts[1].text == want, "③ `armyTexts[1].text` 写对了（原版是全数组写）");
        Check(armyTextsOk && texts[0] != null && texts[0].GetType() == typeof(TMPro.TextMeshPro),
              "`armyTexts` 是**世界空间** `TextMeshPro`（⛔ 不是 `TextMeshProUGUI`）");

        // ④ 拆包声（原版 `SoundManager.Play2D(openBoosterPack, MixerType.Jingles)`）
        CheckEq(item.OpenBoosterPack, "Booster Open Tyranid",
                "④ cue = `Booster Open Tyranid`（Leviathan 与 Genestealers 在原版 prefab 上**共用**这一条）");
        CheckEq(item.PlayedCue, "Booster Open Tyranid", "④ 真的把这一条交给了 `WindowSounds`");
        CheckEq(WindowSounds.LastCue, "Booster Open Tyranid", "④ `WindowSounds` 那边最后喊的 cue 也是它");
        Check(WindowSounds.Played >= 1, "④ `WindowSounds` 真的播了 ≥1 次（= 0 ⇒ 那条 cue 不在音效表里，跑 `工具/import_original_sfx.py`）");

        // ⑤ 两条抖动都递了出去（落点 = 本窗的卡容器，见第 5 段）
        Check(item.PlayedShakes.Count == 2, "⑤ 两条 shakePresets 都递出去了");
        if (item.PlayedShakes.Count == 2)
        {
            CheckEq(item.PlayedShakes[0].delay, 1.0f, "⑤ 第一条 delay");
            CheckEq(item.PlayedShakes[1].amplitude, 100.0f, "⑤ 第二条 amplitude");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  4 · 它自带的 2 s clip（炸包）+ t=2.0 的 `StartCardAnimationEvent` 转发
    //  ⚠️ 必须排在「Advance 跑完时间线」之前（这一段要 `WindowOpenDone == false`）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section4_OpenAnimationEvent()
    {
        Debug.Log(P + "--- 4 · 炸包 clip / StartCardAnimationEvent ---");
        var item = _win != null ? _win.VisualItem : null;
        if (item == null) { Check(false, "（前提）卡包实物在 ⇒ 本段可跑"); return; }

        var anim = item.GetComponent<Animation>();
        Check(anim != null, "（前提）实物根上有 `Animation` 组件（原版 prefab 实读：`m_PlayAutomatically = 1`）");
        if (anim == null) return;
        Check(anim.playAutomatically, "`Animation.playAutomatically` = true（原版 prefab 实读）");

        var clip = anim.clip;
        Check(clip != null, "（前提）clip 取得到（容器里那 2 条 `Booster Open *`）");
        if (clip == null) return;
        Check(clip.name.StartsWith("Booster Open"), "clip 名是 `Booster Open *`（got `" + clip.name + "`）");
        Check(clip.legacy, "clip 是 **legacy**（原版 `m_Legacy: 1` —— 靠 `Animation` 组件播，不是 Animator）");

        bool found = false;
        float at = -1f;
        var evs = clip.events;
        if (evs != null)
            for (int i = 0; i < evs.Length; i++)
                if (evs[i].functionName == "StartCardAnimationEvent") { found = true; at = evs[i].time; }
        Check(found, "★ clip 里有 AnimationEvent `StartCardAnimationEvent`");
        CheckEq(at, 2.0f, "★ 它在 **t = 2.0 s**（= 原版「包炸完 → 开包动画开始」那一拍）", 1e-3f);

        // 转发：时间线还没跑完 ⇒ 重开一次（= 原版 `windowAnimation.Play()` 那一跳）
        Check(!_win.WindowOpenDone, "（前提）此刻开包时间线还没跑完");
        int before = _win.OpenAnimStarts;
        item.StartCardAnimationEvent();
        Check(_win.OpenAnimStarts == before + 1, "★ `StartCardAnimationEvent` 转发到窗（`OpenAnimStarts` +1）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  5 · 抖真的落到卡容器上 + 粒子的队列被提上来了
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section5_ShakeAndParticles()
    {
        Debug.Log(P + "--- 5 · 抖卡容器 / 粒子队列 ---");
        var item = _win != null ? _win.VisualItem : null;
        if (item == null) { Check(false, "（前提）卡包实物在 ⇒ 本段可跑"); return; }
        var anchor = _win.AnchorNode;
        if (anchor == null) { Check(false, "（前提）锚点在 ⇒ 本段可跑"); return; }

        // 鉴别力前提：第一条 preset 的 delay = 1.0 s ⇒ 0.2 s 时**不该**动
        Vector3 before = anchor.localPosition;
        _win.Advance(0.2f);
        Check(Vector3.Distance(anchor.localPosition, before) < 1e-5f,
              "（鉴别力）0.2 s 时**还没到**第一条 preset 的 delay(1.0) ⇒ 卡容器不动");

        // 跨过 1.0 s ⇒ 动（`StepBursts` 写锚点的 localPosition）
        _win.Advance(0.85f);                    // 累计 1.05 s
        Check(Vector3.Distance(anchor.localPosition, before) > 1e-4f,
              "★ 1.05 s 时卡容器**真的在抖**（等价物：菜单层没有可抖的相机 ⇒ 抖 `Booster Animation Parent`）");

        // 粒子：原版粒子画在 UI 背景之上、卡之下；我们把它们提到 `QCard`（背景 = `QShade` 3170、卡从 3171 起）
        var ps = item.GetComponentsInChildren<ParticleSystemRenderer>(true);
        Check(ps.Length > 0, "（前提）实物子树里有粒子（原版 `Booster Pack Particles` 那一族）");
        int lifted = 0;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i] != null && ps[i].sharedMaterial != null
                && ps[i].sharedMaterial.renderQueue == BoosterPackOpenWindow.QCard) lifted++;
        Check(ps.Length > 0 && lifted == ps.Length,
              "★ 粒子材质的 renderQueue 全部提到 `QCard`（" + lifted + "/" + ps.Length + "）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  6 · 时间线跑完之后再打 `StartCardAnimationEvent` ⇒ **不重播**
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section6_NoReplay()
    {
        Debug.Log(P + "--- 6 · 跑完之后不重播 ---");
        var item = _win != null ? _win.VisualItem : null;
        if (item == null) { Check(false, "（前提）卡包实物在 ⇒ 本段可跑"); return; }

        Check(_win.WindowOpenDone, "（前提）第 5 段的 Advance 已把开包时间线跑完（>0.6167 s）");
        int after = _win.OpenAnimStarts;
        item.StartCardAnimationEvent();
        Check(_win.OpenAnimStarts == after,
              "时间线已跑完时**不重播**（`OpenAnimStarts` 不再 +1 —— ⛔ 不让 5 张卡在 2 s 时再缩一次）");
    }
}
