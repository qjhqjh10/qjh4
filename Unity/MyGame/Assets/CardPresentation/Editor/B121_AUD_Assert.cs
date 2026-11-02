// B121_AUD_Assert.cs — 执行写手 `B121-AUD`（2026-11-03）· **音频四条收口**的回归断言
//
// ============================ 它盯的是什么 ============================
//  `B121-AUD-03`（生成器手写组）· `B121-AUD-04`（`BaseEventPopup` 开窗音）·
//  `B121-AUD-05`（`BattleDoors` 绕过 cue ⇒ 响 1.43 倍）· `B121-COLL-11`（Done 钮缺的那一声）。
//  逐条的判据链与「还差什么」→ `资料/完全复刻/报告/B121_AUD_四条.md`。
//
// ============================ 判据（逐条实读，出处写在断言里）============================
//  · **33 → 34 条**：`battle_sounds.json` 现读 `16 collection + 11 card + 5 scene + 2 event`。
//    `scene/event` 那 7 条 = `B121-D2` 的 6 条 + `B121-AUD-05` 补的 `Draw`
//    （`EndBattleDoors.drawAudioCue` +0xC0 × 13 场 → cue `Draw`；`B121-D2` 漏了**同一颗 MB**里的这一格）。
//  · **手写组已进生成器**：`工具/import_battle_sfx.py` 的 `HAND_WRITTEN`（只有「谁指向哪条 cue」写死，
//    clips / 音量 / 音高 / ttp 仍当场从 shipped cue 束重解）⇒ 重跑生成器**不再丢**这 7 条。
//  · **音量 = cue 的 0.7**：`Victory` / `Defeat` / `Draw` 三条包装实测 `min/maxVolume = 0.7`；
//    我们原来直读 clip 按 1.0 播 ⇒ 比原版响 1/0.7 = **1.43 倍**。
//  · ⚠️ **`Draw` 在两张表里各有一份**：`animfx_sounds.json` 那条是**历史手写**（现 `import_original_sfx.py` 的取材面
//    产不出它：`"Draw"` / `MatchDraw` / 该 cue 的 pathID 在 `animfx_modules.json` 里 **0 命中**），本批把它放进
//    **可重生成**的 `battle_sounds.json`。两条参数逐值相同 ⇒ `WFSoundBank` 撞名保留先装的、行为无差别；A7 盯住这一点。
//  · **Done 钮那声 = `Deck Editing Valid Deck`**（`DeckEditingWindow.doneButtonEnabled` →
//    PPtr(fileID 6, pid `-8540943293993850510`) → `soundcollection_assets_all` 的 cue 包装）。
//  · **`Toggle(valid, 0.3f)` 的 0.3**：由 `UIEffectTogglerController.Toggle` 的**默认实参**给
//    （`duration = 0.3f`）⇒ 断那个默认值，而不是另找一处常量。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 1（A2 + A3）：A2 读 **`battle_sounds.json` 原文**的 `group` 列，A3 读 **生成器源码**里 `HAND_WRITTEN` 的
//      名字集合 —— **两个独立文件**必须相等。只手改 JSON（不更新生成器）⇒ A3 红；只改生成器（不重跑）⇒ A2 红。
//  🧨 2（B1 + B2）：B1 断「事件数据 → GUID 表 → cue 名」，B2 断「这条 cue 在表里且 clip 对」。
//      把表里那条删掉 ⇒ B2 红；把 `OpenSoundCueByGuid` 改成别的名字 ⇒ B1 红。
//  🧨 3（C1 + C3）：C1 断 `CueName` 现在是 **cue 资产名**（不是 clip 名），C3 断那条 cue 的音量 = 0.7。
//      把 `CueName` 改回 clip 名（`MatchVictory`）⇒ C1 红 **且** C3 查不到那条 cue 也红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不读存档 / 不写 `PlayerPrefs` / 不需要 driver / 不建 UI 树）。
//  唯一副作用：B 组为了断「`Open()` 那一跳真的接上了」会建**一台临时空 GO** 调 `PlayOpenSoundForTest`
//  （它在 `finally` 里 `DestroyImmediate`），并**喊一次** `WindowSounds.PlayJingles`。
//  挂法（由调度台决定）：接在 `Editor/BattleScene.cs` 那条**方法尾聚合行**里（同一物理行追加 ⇒ 行数零变化）：
//      B121AUD_Assert.Run(); pass += B121AUD_Assert.Pass; fail += B121AUD_Assert.Fail;
//  判绿红：`grep "^B121AUD"` 的**合计行**；逐条按**行首** `B121AUD ✓` / `B121AUD ✗` 数（⛔ 别 `grep -c ✗`）。
//
// ⚠️ 批处理下**没有声卡** ⇒ 只验「表 / cue / 接线口 / 参数」，⛔ 不写「跑不出声就失败」那一类断言。
// ⚠️ `Resources/Art/` 是 `.gitignore` 的 ⇒ 全新 clone 上 A7/C4 会因为**音频没导入**而红 —— 那是**环境前提**
//    不是缺陷（跑 `python 工具/import_battle_sfx.py`）。本条与 `B121_D2_Assert` 的 B2 同一口径。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using WarpforgeVFX;
using CardPresentation;

/// <summary>执行写手 `B121-AUD`：音频四条（生成器手写组 / 开窗音 / 开门音 / Done 钮音）的回归断言。</summary>
public static class B121AUD_Assert
{
    const string Tag = "B121AUD";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    /// <summary>`group = scene | event` 这 7 条 —— 键 = **cue 资产名**（③④ 的形状：`name == srcCue`）。</summary>
    static readonly string[] HandCues =
    {
        "Victory", "Defeat", "Draw", "OvertimeStart", "Blackout",
        "Ork Expansion 1 jingle", "Space Wolves Relase Jingle",
    };

    /// <summary>三条结算 cue 的音频参数 = 原版实读（`soundcollection_assets_all` 的 cue 包装）。</summary>
    static readonly Dictionary<string, float> Vol070 = new Dictionary<string, float>
    {
        { "Victory", 0.7f }, { "Defeat", 0.7f }, { "Draw", 0.7f },
    };

    // ---- 路径：`Application.dataPath` = `<…>/MyGame/Assets` ----------------
    static string ProjectRoot { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
    static string UnityRoot { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../..")); } }
    static string TableJson { get { return Path.Combine(Application.dataPath, "CardPresentation/Resources/battle_sounds.json"); } }
    static string GeneratorPy { get { return Path.Combine(UnityRoot, "工具/import_battle_sfx.py"); } }

    // ---- 只解析用到的那几列（`JsonUtility` 允许多余字段）------------------
    [Serializable] public class AudTable { public AudRow[] cues; }
    [Serializable] public class AudRow { public string name; public string group; public string srcCue; public string[] clips; public float minVolume; public float maxVolume; }

    /// <summary>入口（零前提）。</summary>
    public static void Run()
    {
        _pass = _fail = 0;
        WFSoundBank.Reset();                 // 表可能刚被重生成 ⇒ 别拿进程里的旧缓存判
        GroupA_HandWritten();
        GroupB_EventPopup();
        GroupC_BattleDoors();
        GroupD_DoneButton();
    }

    // ==================================================================
    //  A · `B121-AUD-03`：手写组（生成器写死名字 + 参数重解）
    // ==================================================================
    static void GroupA_HandWritten()
    {
        AudTable tbl = null;
        string why = null;
        if (!File.Exists(TableJson)) why = "文件不存在：" + TableJson;
        else
        {
            try { tbl = JsonUtility.FromJson<AudTable>(File.ReadAllText(TableJson, System.Text.Encoding.UTF8)); }
            catch (Exception e) { why = "JSON 解析不了：" + e.Message; }
        }
        Check(tbl != null && tbl.cues != null && tbl.cues.Length > 0,
              $"A0 `battle_sounds.json` 读得到并解析得出（{why ?? "条数 " + (tbl != null && tbl.cues != null ? tbl.cues.Length : -1)}）");

        var inJson = new SortedSet<string>(StringComparer.Ordinal);
        var rows = new Dictionary<string, AudRow>(StringComparer.Ordinal);
        if (tbl != null && tbl.cues != null)
            foreach (var c in tbl.cues)
            {
                if (c == null || (c.group != "scene" && c.group != "event")) continue;
                inJson.Add(c.name);
                rows[c.name] = c;
            }

        // A1 逐名在表里（= `B121-D2` 的 6 条 + `B121-AUD-05` 的 `Draw`；⛔ 不写死总数，写死名字）
        int miss = 0; string missList = "";
        foreach (var n in HandCues)
            if (!inJson.Contains(n)) { miss++; missList += " " + n; }
        Check(miss == 0,
              $"A1 `group=scene|event` 那 **{HandCues.Length}** 条 cue 全在表里（缺 {miss}{missList}；实得 {inJson.Count} 条）"
            + "｜🧨 重跑生成器丢掉手写组 ⇒ 本条红");

        // A2 ③④ 的形状：`srcCue == name`（这两组的 name 是 **cue 资产名**，不是字段名）
        int shape = 0; string shapeList = "";
        foreach (var n in HandCues)
        {
            AudRow r;
            if (!rows.TryGetValue(n, out r)) continue;
            if (r.srcCue != n || r.clips == null || r.clips.Length == 0) { shape++; shapeList += " " + n; }
        }
        Check(shape == 0,
              $"A2 这 {HandCues.Length} 条的 `srcCue == name` 且 `clips` 非空（违例 {shape}{shapeList}）"
            + "｜判据：③④ 两组 `name` = cue 资产名（本文件头 + `battle_sounds.json` 的 note）");

        // A3 🧨 生成器源码里的 `HAND_WRITTEN` 名字集合 == 表里 scene/event 的集合（两个独立文件）
        var inPy = new SortedSet<string>(StringComparer.Ordinal);
        string pyErr = null;
        if (!File.Exists(GeneratorPy)) pyErr = "生成器不在：" + GeneratorPy;
        else
        {
            string src = File.ReadAllText(GeneratorPy, System.Text.Encoding.UTF8);
            if (!src.Contains("HAND_WRITTEN"))
                pyErr = "源码里没有 `HAND_WRITTEN` 这个手写组";
            else
                foreach (Match m in Regex.Matches(src, "\\(\"([^\"]+)\",\\s*\"(?:scene|event)\""))
                    inPy.Add(m.Groups[1].Value);
        }
        Check(pyErr == null, $"A3 `工具/import_battle_sfx.py` 里那个 `HAND_WRITTEN` 手写组在（{pyErr ?? "✓"}）"
                           + "｜🧨 它被删掉 ⇒ 重生成就丢 7 条");
        bool same = pyErr == null && inPy.SetEquals(inJson);
        string onlyPy = string.Join(",", new List<string>(inPy.Except(inJson)).ToArray());
        string onlyJson = string.Join(",", new List<string>(inJson.Except(inPy)).ToArray());
        Check(same,
              $"A3b **生成器手写组的名字集合 == 表里 scene/event 的名字集合**"
            + $"（生成器 {inPy.Count} 条 / 表 {inJson.Count} 条；只在生成器：{onlyPy}；只在表：{onlyJson}）"
            + "｜🧨 只改 JSON 不更新生成器 ⇒ 红；只改生成器不重跑 ⇒ 也红");

        // A4 真能从 bank 查到（`Ensure()` 装的三张表合一）
        miss = 0; missList = "";
        foreach (var n in HandCues)
            if (!WFSoundBank.HasCue(n)) { miss++; missList += " " + n; }
        Check(miss == 0, $"A4 这 {HandCues.Length} 条 `WFSoundBank.HasCue` 全真（查不到 {miss}{missList}）");

        // A5 音量：结算三条 = **0.7**（改回直读 clip 就丢了它）
        int badVol = 0; string badVolList = "";
        foreach (var kv in Vol070)
        {
            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(kv.Key, out cue) || cue == null) { badVol++; badVolList += " " + kv.Key + "(查不到)"; continue; }
            if (Mathf.Abs(cue.minVolume - kv.Value) > 1e-3f || Mathf.Abs(cue.maxVolume - kv.Value) > 1e-3f)
            { badVol++; badVolList += $" {kv.Key}({cue.minVolume:F2}/{cue.maxVolume:F2})"; }
        }
        Check(badVol == 0,
              $"A5 `Victory`/`Defeat`/`Draw` 的 cue 音量 = **0.7**（原版三条包装实读；违例 {badVol}{badVolList}）"
            + "｜🧨 这三条按 1.0 播就是原版的 1.43 倍");

        // A6 clip 真的加载得到（⚠️ 环境前提：`Resources/Art/` 是 gitignore 的）
        miss = 0; missList = "";
        foreach (var n in HandCues)
        {
            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(n, out cue) || cue == null) { miss++; missList += " " + n + "(无 cue)"; continue; }
            if (WFSoundBank.PickClip(cue, null) == null) { miss++; missList += " " + n + "(clip 加载不到)"; }
        }
        Check(miss == 0, $"A6 这 {HandCues.Length} 条的 clip 都能 `Resources.Load`（缺 {miss}{missList}；"
                       + "⚠️ 环境前提：跑过 `工具/import_battle_sfx.py`）");

        // A7 `Draw` 在 **两张表里各有一份**（`animfx_sounds.json` 那条是历史手写、现取材面产不出）⇒
        //    两份**参数必须逐值相同**，否则 `WFSoundBank` 撞名保留先装的那条，行为会随「哪张先装」漂。
        string animfxPath = Path.Combine(Application.dataPath, "CardPresentation/Resources/animfx_sounds.json");
        AudTable anim = null;
        if (File.Exists(animfxPath))
        {
            try { anim = JsonUtility.FromJson<AudTable>(File.ReadAllText(animfxPath, System.Text.Encoding.UTF8)); }
            catch (Exception) { anim = null; }
        }
        AudRow a1 = null, a2 = null;
        if (anim != null && anim.cues != null)
            foreach (var c in anim.cues) if (c != null && c.name == "Draw") { a1 = c; break; }
        AudRow b1;
        if (rows.TryGetValue("Draw", out b1)) a2 = b1;
        bool sameDraw = true; string drawWhy = "（两张表里只有一份，无需比对）";
        if (a1 != null && a2 != null)
        {
            sameDraw = a1.minVolume == a2.minVolume && a1.maxVolume == a2.maxVolume
                       && a1.clips != null && a2.clips != null && a1.clips.Length == a2.clips.Length
                       && string.Join(",", a1.clips) == string.Join(",", a2.clips);
            drawWhy = $"animfx vol {a1.minVolume:F2}/{a1.maxVolume:F2} · clips {string.Join(",", a1.clips)}；"
                    + $"battle vol {a2.minVolume:F2}/{a2.maxVolume:F2} · clips {string.Join(",", a2.clips)}";
        }
        Check(sameDraw, "A7 `Draw` 若在两张表里都有 ⇒ **参数逐值相同**（撞名时 `WFSoundBank` 保留先装的，"
                      + "两份漂了就会「行为取决于哪张先装」）｜" + drawWhy);
    }

    // ==================================================================
    //  B · `B121-AUD-04`：`BaseEventPopup.Open()` 的开窗音
    // ==================================================================
    static void GroupB_EventPopup()
    {
        // B1 事件数据 → GUID 表 → cue 名（**真的走一遍那条链**，不是读常量）
        var ev = new Dictionary<string, string>
        {
            { "ExpansionOrks1", "Ork Expansion 1 jingle" },
            { "SWRelease",      "Space Wolves Relase Jingle" },
        };
        GameObject go = null;
        try
        {
            go = new GameObject("B121AUD_EventPopupProbe");
            var pop = go.AddComponent<BaseEventPopup>();
            int bad = 0; string badList = "";
            foreach (var kv in ev)
            {
                string got = pop.PlayOpenSoundForTest(kv.Key);
                if (got != kv.Value) { bad++; badList += $" {kv.Key}→{got ?? "<null>"}(期望 {kv.Value})"; }
            }
            Check(bad == 0,
                  $"B1 事件数据 `EventAssets[key=8 OpenSound]` → cue：`ExpansionOrks1`/`SWRelease` 各解出 {ev.Count} 条（违例 {bad}{badList}）"
                + "｜判据：il2csharp `BaseEventPopup.cs:56-60`（`HasAsset(…,8)` → `LoadAsset<AudioCue>` → `Play2D(Jingles)`）");

            // B2 空事件 / 没有 key=8 的事件 ⇒ **不播**（原版 `HasAsset` 为假那一支）
            Check(pop.PlayOpenSoundForTest(null) == null && pop.PlayOpenSoundForTest("ExpansionUM1") == null,
                  "B2 没有事件 id / 该事件没有 `EventAssets[key=8]` ⇒ 这一跳**不播**（原版 `HasAsset` 为假的分支，⛔ 不是失败）");
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }

        // B3 两条 cue 在表里、clip 对（与 B1 独立的一条链：`WFSoundBank`）
        var clip = new Dictionary<string, string>
        {
            { "Ork Expansion 1 jingle", "Ork expansion pop up jingle" },
            { "Space Wolves Relase Jingle", "Ragnar Howl" },
        };
        int bad2 = 0; string bad2List = "";
        foreach (var kv in clip)
        {
            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(kv.Key, out cue) || cue == null || cue.clips == null
                || Array.IndexOf(cue.clips, kv.Value) < 0)
            { bad2++; bad2List += " " + kv.Key; }
        }
        Check(bad2 == 0, $"B3 两条开窗 cue 在表里且 clip 对（`Ork expansion pop up jingle` / `Ragnar Howl`；违例 {bad2}{bad2List}）");

        // B4 那张 GUID→cue 表 == 原 SO 里 key=8 的 GUID（悬挂那条**故意不在**表里）
        Check(BaseEventPopup.OpenSoundCueByGuid != null && BaseEventPopup.OpenSoundCueByGuid.Count == 2
              && BaseEventPopup.OpenSoundCueByGuid.ContainsKey("11c215f6b9e254461a3ec5d917c4f147")
              && BaseEventPopup.OpenSoundCueByGuid.ContainsKey("baa1a292a3fe04b6097feefbfea91919"),
              "B4 `OpenSoundCueByGuid` = 恰好那两条**本地解得出**的（Orks1 / Space Wolves；Emperors Children 的 GUID 本地 0 命中 ⇒ 故意不在表里，⛔ 不拿别的 cue 顶）");
    }

    // ==================================================================
    //  C · `B121-AUD-05`：`BattleDoors` 的三支开门音走 cue（0.7）
    // ==================================================================
    static void GroupC_BattleDoors()
    {
        // C1 走的是 **cue 资产名**，不是 clip 名
        var want = new Dictionary<BattleDoors.Result, string>
        {
            { BattleDoors.Result.Victory, "Victory" },
            { BattleDoors.Result.Defeat,  "Defeat"  },
            { BattleDoors.Result.Draw,    "Draw"    },
        };
        int bad = 0; string badList = "";
        foreach (var kv in want)
        {
            string got = BattleDoors.CueAssetOf(kv.Key);
            if (got != kv.Value) { bad++; badList += $" {kv.Key}→{got ?? "<null>"}"; }
        }
        Check(bad == 0, $"C1 `BattleDoors` 三档走的是 **cue 资产名** `Victory`/`Defeat`/`Draw`（违例 {bad}{badList}）"
                      + "｜🧨 改回 clip 名（`MatchVictory`…）⇒ 本条红、C2/C3 一起红");

        // C2 `HasCue` 三个都真（判据已改成「cue 表里有没有」）
        bad = 0; badList = "";
        foreach (var kv in want)
            if (!BattleDoors.HasCue(kv.Key)) { bad++; badList += " " + kv.Key; }
        Check(bad == 0, $"C2 `BattleDoors.HasCue` 三档全真（缺 {bad}{badList}）");

        // C3 那三条 cue 的音量 = 0.7（= 我们不再按 1.0 播）
        bad = 0; badList = "";
        foreach (var kv in want)
        {
            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(kv.Value, out cue) || cue == null
                || Mathf.Abs(cue.minVolume - 0.7f) > 1e-3f || Mathf.Abs(cue.maxVolume - 0.7f) > 1e-3f)
            { bad++; badList += " " + kv.Value; }
        }
        Check(bad == 0, $"C3 三档 cue 的 min/maxVolume = **0.7**（原版 `EndBattleDoors` 三个字段解出的包装；违例 {bad}{badList}）"
                      + "｜🧨 我们原来直读 clip 按 1.0 播 ⇒ 响 1.43 倍");

        // C4 clip 在位（⚠️ 环境前提）
        bad = 0; badList = "";
        foreach (var kv in want)
        {
            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(kv.Value, out cue) || cue == null || WFSoundBank.PickClip(cue, null) == null)
            { bad++; badList += " " + kv.Value; }
        }
        Check(bad == 0, $"C4 三档 cue 的 clip（`MatchVictory`/`MatchDefeat`/`MatchDraw`）都加载得到（缺 {bad}{badList}）");
    }

    // ==================================================================
    //  D · `B121-COLL-11`：Done 钮「变合法」的那一声 + 0.3 s 淡入
    // ==================================================================
    static void GroupD_DoneButton()
    {
        // D1 cue 名（= 原版 `DeckEditingWindow.doneButtonEnabled` 那条）
        Check(DeckRuntime.DoneEnabledCue == "Deck Editing Valid Deck",
              $"D1 Done 钮那声的 cue 名 = **`Deck Editing Valid Deck`**（实得 `{DeckRuntime.DoneEnabledCue}`）"
            + "｜判据：`bundle_menus_assets_all/MonoBehaviour_5918047927837519652.json` 的 `doneButtonEnabled`"
            + " = PPtr(fileID 6, pid -8540943293993850510) → `soundcollection_assets_all` 的 cue 包装");

        // D2 表里查得到、clip = `Progress Jingle`
        WFSoundCue cue;
        bool has = WFSoundBank.TryGetCue(DeckRuntime.DoneEnabledCue, out cue) && cue != null;
        Check(has && cue.clips != null && Array.IndexOf(cue.clips, "Progress Jingle") >= 0,
              "D2 那条 cue 在 `animfx_sounds.json` 里且 clip = `Progress Jingle`"
            + "｜它由 `工具/import_original_sfx.py` 的 `EXTRA_CUES` 收（本批**没有**改音效表）");

        // D3 0.3 s 淡入 = `UIEffectTogglerController.Toggle` 的**默认实参**
        var mi = typeof(UIEffectTogglerController).GetMethod("Toggle", BindingFlags.Public | BindingFlags.Instance);
        ParameterInfo[] ps = mi != null ? mi.GetParameters() : null;
        bool ok03 = ps != null && ps.Length >= 2 && ps[1].ParameterType == typeof(float)
                    && ps[1].HasDefaultValue && Mathf.Abs((float)ps[1].DefaultValue - 0.3f) < 1e-6f;
        Check(ok03, "D3 `UIEffectTogglerController.Toggle(bool, float duration = **0.3f**)` —— 原版 `UpdateDoneButton`"
                  + " 那条 `Toggle(true, 0.3f)` 就是它（`FootShine` 吃不到这个时长：原版那颗的 `DoUpdate` 是空桩）"
            + "｜🧨 把默认值改成 0 ⇒ 淡入变硬切 ⇒ 本条红");

        // D4 接线口在那（本批没权改 `DeckRuntime.cs`，所以要断「口留着」）
        var t = typeof(DeckRuntime);
        bool hasPlay = t.GetMethod("PlayDoneButtonEnabledCue", BindingFlags.NonPublic | BindingFlags.Instance) != null;
        bool hasUpdate = t.GetMethod("UpdateDoneButtonSound", BindingFlags.NonPublic | BindingFlags.Instance) != null;
        Check(hasPlay && hasUpdate,
              "D4 `DeckRuntime` 上有 `PlayDoneButtonEnabledCue` / `UpdateDoneButtonSound` 两个口"
            + "（前者已由 `PunchDoneButton` 在「不合法→合法」那一拍调用；后者的接线补丁见报告 §补丁）");
    }
}
