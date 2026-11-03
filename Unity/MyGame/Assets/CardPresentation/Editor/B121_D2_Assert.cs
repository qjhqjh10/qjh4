// B121_D2_Assert.cs — 执行写手 `B121-D2`（2026-11-03）· **音效 cue 覆盖率**的回归断言
//
// ============================ 它盯的是什么 ============================
//  `B121-08`（A/D 类）「原版音效 cue 覆盖」＋ `B121-31`（D 类）「`Trait *` 9 条 cue 的持有者」。
//  本文件只钉**能力面**（表能装、能查、clip 能加载、查不到会出声），逐条缺口清单在
//  `资料/完全复刻/报告/B121_D2_音效cue覆盖.md`（那里有 PathID / 持有者 / 搜过的目录）。
//
// ============================ 判据（逐条实读，出处写在断言里）============================
//  · **cue 资产总量**：`bundle_soundcollection_assets_all/MonoBehaviour/` 现读 **610 个 cue 对象 / 608 个不同
//    `m_Name`**（2 对同名：`Stun Effect` ×2 · `Will of Gork` ×2 —— `工具` 侧用 UnityPy 直读束得到，
//    见报告 §缺口清单 的「现读更正」）。
//  · **三表合一**：`WFP` 口径见 `WarpforgeVFX/Runtime/WFSoundBank.cs:94-128`（`TablePath` / `BattleTablePath` /
//    `RemnantTablePath` 装进同一个字典，**撞名只警告、保留先装的**）。
//  · **`B121-D2` 补的 7 条**（`battle_sounds.json` 的 `group=scene|event`；= `B121-D2` 的 6 条 + `B121-AUD-05` 的 `Draw`）：持有者逐条实读 ——
//    ① `Victory` / `Defeat` = 13 份战场场景的 `EndBattleDoors.victoryCue`(`0xB0`) / `defeatAudioCue`(`0xB8`)
//       （例 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_5238.json`；
//        消费点 `decomp_full/EndBattleDoors__SetupDoor.c:113` = `SoundManager.Play2D(cue, Jingles)`）；
//    ② `OvertimeStart` = 同 13 份场景的 `OvertimeUi.enteringOvertimeSound`
//       （例 `…battlearena1/MonoBehaviour/MonoBehaviour_4883.json`）；
//    ③ `Blackout` = `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_9031176634359415475.json`
//       的 `sounds[0].sound`（`m_Script` pathID `8184164905518033017` → 类 `ShakeAndSoundObject`；
//        = 环境条件 `Saim Hann Blackout` 那颗震屏/环境音宿主）；
//    ④ `Ork Expansion 1 jingle` / `Space Wolves Relase Jingle` = 活动 SO 的
//       `EventElements.EventAssets[key=8]`（`EventAssetReferenceType.OpenSound`），资产
//       `bundle_cosmeticsso_assets_all/MonoBehaviour/{Expansion Orks1,Release Space Wolves} Parent Event.json`；
//       消费点 `cpp2il/out_il2csharp/Assembly-CSharp/BaseEventPopup.cs:56-60`（`AudioCue.Play2D(Jingles)`）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 1（A2 + A3）：A2 是「三表 union 的 distinct 名字数」，A3 是「`WFSoundBank.CueCount`」—— **两个独立来源**
//     的数必须相等。把某张表的装载删掉 ⇒ A1 逐名红 **且** A3 少一截；把某个 cue 名手改成别的 ⇒ A1 红。
//  🧨 2（B2 + B3）：B2 走 `WFSoundBank.Clip()`（真的 `Resources.Load`），B3 读 JSON 原文里的 `group` 列。
//     把 cue 的 clip 改成自己的名字（凑数）⇒ B2 红；把它塞回 `group=collection`（当字段名用）⇒ B3 红。
//  🧨 3（C0 + C1）：C0 断「查不到 ⇒ 返回 false 且 `BadCues` +1」，C1 断「`PickClip(null)` 回 null」。
//     把 `TryGetCue` 改成「查不到也造一条空 cue 返回 true」⇒ C0 红（而 C1 仍绿，单看 C1 看不出来）。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不读存档 / 不写 `PlayerPrefs` / 不需要 driver / 不建音源）。
//  ⚠️ 它**不改** `WFSoundPlayer.Enabled`、**不播任何声音** —— 只查表与 `Resources.Load`。
//  挂法（由调度台决定）：接在 `Editor/BattleScene.cs` 那条**方法尾聚合行**里（同一物理行追加 ⇒ 行数零变化）：
//      B121D2_Assert.Run(); pass += B121D2_Assert.Pass; fail += B121D2_Assert.Fail;
//  判绿红：`grep "^B121D2"` 的**合计行**；逐条按**行首** `B121D2 ✓` / `B121D2 ✗` 数（⛔ 别 `grep -c ✗`）。
//
// ⚠️ 批处理下**没有声卡** ⇒ 只验「表 / clip 资源 / 查表契约」，⛔ 不写「跑不出声就失败」那一类断言。
// ⚠️ `Resources/Art/` 是 `.gitignore` 的 ⇒ 全新 clone 上 B2 会因为**音频没导入**而红，
//    那是**环境前提**不是缺陷：跑 `python 工具/import_battle_sfx.py` / `import_original_sfx.py` 补音频。

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WarpforgeVFX;

/// <summary>执行写手 `B121_D2`：cue 覆盖率（三表合一 / 7 条新增 scene·event cue / 「查不到不许静默」）的回归断言。</summary>
public static class B121D2_Assert
{
    const string Tag = "B121D2";
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

    /// <summary>`B121-D2` 补进 `battle_sounds.json` 的 7 条（`group = scene` / `event`）——
    /// 键 = **cue 资产名**（⛔ 不是原版字段名；与同表另外 27 条的唯一形状差异）。
    /// ⚠️ **2026-11-03（`B121-FIX`）**：原为 6 条、**漏了 `Draw`** —— 它由 `B121-AUD-05` 从
    /// `EndBattleDoors.drawAudioCue` 解出并写进表（`clips = ["MatchDraw"]`，参数同 `Victory`/`Defeat` 0.7）。
    /// 本表不收它 ⇒ 这条 cue 只有 `B117_G10_12_Assert` C1b 在钉（漏了就等于没人管）⇒ 现补齐成 7 条，
    /// 与 `B117_G10_12_Assert` C1b 逐名对齐。</summary>
    static readonly string[] AddedCues =
    {
        "Victory", "Defeat", "OvertimeStart", "Blackout", "Draw",
        "Ork Expansion 1 jingle", "Space Wolves Relase Jingle",
    };

    /// <summary>每条 cue 的唯一 clip（逐条实读 `soundcollection_assets_all.bundle`；⛔ cue 名 ≠ clip 名的那几条已按真名写）。</summary>
    static readonly Dictionary<string, string> AddedClip = new Dictionary<string, string>
    {
        { "Victory",                    "MatchVictory"              },
        { "Defeat",                     "MatchDefeat"               },
        { "OvertimeStart",              "OvertimeStart"             },
        { "Blackout",                   "Aeldari Blackout"          },
        { "Draw",                       "MatchDraw"                 },   // 2026-11-03（`B121-FIX`）补齐：`EndBattleDoors.drawAudioCue` → cue `Draw` → clip `MatchDraw`（现读 `battle_sounds.json` 逐字）
        { "Ork Expansion 1 jingle",     "Ork expansion pop up jingle" },
        { "Space Wolves Relase Jingle", "Ragnar Howl"               },
    };

    /// <summary>音量/音高区间（逐条实读资产；`Victory`/`Defeat` = **0.7** 不是 1.0，`Blackout` = 0.5 / ttp 0.5）。</summary>
    static readonly Dictionary<string, float[]> AddedParams = new Dictionary<string, float[]>
    {
        //                       minPitch, maxPitch, minVolume, maxVolume, timeToPlayAgain
        { "Victory",                    new[] { 1f, 1.0f, 0.7f, 0.7f, 0.1f } },
        { "Defeat",                     new[] { 1f, 1.0f, 0.7f, 0.7f, 0.1f } },
        { "OvertimeStart",              new[] { 1f, 1.0f, 1.0f, 1.0f, 0.1f } },
        { "Blackout",                   new[] { 1f, 1.0f, 0.5f, 0.5f, 0.5f } },
        { "Draw",                       new[] { 1f, 1.0f, 0.7f, 0.7f, 0.1f } },   // 2026-11-03（`B121-FIX`）补齐：与 `Victory`/`Defeat` 同包装（0.7 音量、ttp 0.1；现读 `battle_sounds.json`）
        { "Ork Expansion 1 jingle",     new[] { 1f, 1.0f, 1.0f, 1.0f, 0.1f } },
        { "Space Wolves Relase Jingle", new[] { 1f, 1.0f, 1.0f, 1.0f, 0.1f } },
    };

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        try
        {
            GroupA_TablesMerged();
            GroupB_AddedCuesPlayable();
            GroupC_MissingCueIsNotSilent();
            GroupD_EnvShakeSoundsCovered();
        }
        finally
        {
            Debug.Log(Tag + " === 合计：✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "） ===");
        }
    }

    // ==================================================================
    //  A · 三表合一：覆盖数按**现读**算，⛔ 不写死 537/610 那类旧数
    // ==================================================================
    static void GroupA_TablesMerged()
    {
        Check(WFSoundBank.Loaded, "A0 前提：`WFSoundBank` 装起来了（三张表都在 `Resources/` 下）"
                                + "｜🧨 表文件没进仓库 ⇒ 本条红，下面全失去意义");

        // A1 逐表：非空 name 的每一条都必须真的在字典里（= 装载没丢、撞名没把哪条顶掉而不吭声）。
        var union = new HashSet<string>(StringComparer.Ordinal);
        int tableCount = 0;
        foreach (var path in new[] { WFSoundBank.TablePath, WFSoundBank.BattleTablePath, WFSoundBank.RemnantTablePath })
        {
            var ta = Resources.Load<TextAsset>(path);
            Check(ta != null, $"A1a 表 `Resources/{path}.json` 在（现读：{(ta != null ? "有" : "**没有**")}）"
                            + "｜🧨 删掉任一张表 ⇒ 那一族 cue 整片不响，且本行红");
            if (ta == null) continue;
            WFSoundTable tbl = null;
            try { tbl = JsonUtility.FromJson<WFSoundTable>(ta.text); }
            catch (Exception e) { Debug.LogWarning(Tag + " 解析 `" + path + "` 失败：" + e.Message); }
            Check(tbl != null && tbl.cues != null && tbl.cues.Length > 0,
                  $"A1b 表 `{path}` 解析得出且非空（实得 {(tbl != null && tbl.cues != null ? tbl.cues.Length : -1)} 条）");
            if (tbl == null || tbl.cues == null) continue;

            int miss = 0, empty = 0; string missList = "";
            foreach (var c in tbl.cues)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) { empty++; continue; }
                union.Add(c.name);
                if (!WFSoundBank.HasCue(c.name)) { miss++; if (miss <= 5) missList += " " + c.name; }
            }
            Check(empty == 0, $"A1c 表 `{path}` 没有空名条目（实得 {empty}）");
            Check(miss == 0, $"A1d 表 `{path}` 的每个 cue 名都真在 `WFSoundBank` 字典里（缺 {miss}{missList}）"
                           + "｜🧨 撞名被「保留先装的」顶掉却不报 ⇒ 本条红");
            tableCount++;
        }
        Check(tableCount == 3, $"A1e 三张表都读到了（实得 {tableCount}）");

        // A2 + A3：两个**独立来源**的数必须相等（`WFSoundBank.CueCount` 是装载结果，`union.Count` 是逐表现数）。
        Check(union.Count > 0, $"A2 前提：现数出 {union.Count} 个不同的 cue 名");
        Check(WFSoundBank.CueCount == union.Count,
              $"A3 ★ 装载后的 `CueCount` = 三表 union 的 distinct 数（`CueCount={WFSoundBank.CueCount}` · union={union.Count}）"
            + "｜🧨 某张表的装载被删掉 / 被换成空表 ⇒ 两个数分叉 ⇒ 本条红（⛔ 这里**不写死**任何历史数字）");
    }

    // ==================================================================
    //  B · `B121-D2` 补的 7 条：表里在、clip 加载得出来、参数与资产一致
    // ==================================================================
    static void GroupB_AddedCuesPlayable()
    {
        string json = ReadTableText(WFSoundBank.BattleTablePath);
        Check(!string.IsNullOrEmpty(json), "B0 前提：读得到 `battle_sounds.json` 原文（B3 要用它的 `group` 列）");

        foreach (var name in AddedCues)
        {
            // B1 在表里 + 有 clip
            WFSoundCue cue = null;
            bool has = WFSoundBank.TryGetCue(name, out cue) && cue != null;
            Check(has && cue.clips != null && cue.clips.Length > 0,
                  $"B1 cue `{name}` 在表里且 clips 非空"
                + "｜🧨 删掉 `battle_sounds.json` 里那条（或重跑 `import_battle_sfx.py` 把它冲掉）⇒ 本行红");

            // B2 clip 真的能 `Resources.Load` 出来（= 音频在 `Resources/Art/audio/sfx/` 下）
            string wantClip;
            if (AddedClip.TryGetValue(name, out wantClip))
            {
                Check(cue != null && cue.clips != null && cue.clips.Length == 1 && cue.clips[0] == wantClip,
                      $"B2a cue `{name}` 的 clip = `{wantClip}`（实得 {(cue != null && cue.clips != null && cue.clips.Length > 0 ? cue.clips[0] : "—")}）"
                    + "｜🧨 拿 cue 名去当 clip 名（`cue 名 ≠ clip 名`）⇒ 本行红");
                Check(WFSoundBank.Clip(wantClip) != null,
                      $"B2b clip `Resources/{WFSoundBank.ClipRoot}{wantClip}` 加载得出来"
                    + "｜🧨 音频没导入（`Resources/Art/` 是 gitignore 的）⇒ 本行红，跑 `python 工具/import_battle_sfx.py`");
            }

            // B3 参数逐值（区间照资产；这几个数是「听感」的唯一来源，别被凑数改掉）
            float[] p;
            if (cue != null && AddedParams.TryGetValue(name, out p))
            {
                bool ok = Near(cue.minPitch, p[0]) && Near(cue.maxPitch, p[1])
                       && Near(cue.minVolume, p[2]) && Near(cue.maxVolume, p[3])
                       && Near(cue.timeToPlayAgain, p[4]);
                Check(ok, $"B3 cue `{name}` 参数照资产（pitch {p[0]}–{p[1]} · vol {p[2]}–{p[3]} · ttp {p[4]}；"
                        + $"实得 {cue.minPitch}–{cue.maxPitch} / {cue.minVolume}–{cue.maxVolume} / {cue.timeToPlayAgain}）");
            }

            // B4 形状：这 7 条必须挂在 `group=scene|event`（⛔ 别混进 `collection`/`card` 那两组「字段名」里）
            if (!string.IsNullOrEmpty(json))
            {
                int at = json.IndexOf("\"name\": \"" + name + "\"", StringComparison.Ordinal);
                // 🔴 2026-11-03（`B124-W27`）：原来是 `Slice(json.Substring(at), "{", "}")` —— 从**名字之后**找第一个 `{`
                //   ⇒ 拿到的是**下一条** cue 的对象体（实测 `battle_sounds.json:454-465` 这条 `"group": "event"` 完全正确，是断言错）。
                //   ⇒ 改成从 `at` **往前**找最近的 `{`（= 本 cue 的对象头），再按花括号配平切到对应的 `}`。
                string block = at < 0 ? null : EnclosingObject(json, at);
                bool sceneOrEvent = block != null
                    && (block.Contains("\"group\": \"scene\"") || block.Contains("\"group\": \"event\""));
                Check(sceneOrEvent, $"B4 cue `{name}` 的 `group` = `scene`/`event`（不是 `collection`/`card`）"
                    + "｜🧨 把它当「原版字段名」塞回前两组 ⇒ 本条红（那会让 `BattleSfx` 的字段名口径失真）");
            }
        }
    }

    // ==================================================================
    //  C · 反证：表里没有的 cue **不许静默**当作「播了空 clip」
    // ==================================================================
    static void GroupC_MissingCueIsNotSilent()
    {
        Check(WFSoundBank.Loaded, "C0 前提：表装起来了");

        const string nope = "__B121D2_no_such_cue__";
        WFSoundCue cue = null;
        int bad0 = WFSoundBank.BadCues;
        bool found = WFSoundBank.TryGetCue(nope, out cue);
        int bad1 = WFSoundBank.BadCues;

        Check(!found, $"C1 ★ 表里没有的 cue `{nope}` ⇒ `TryGetCue` 返回 **false**（⛔ 不是「返回一条空 cue 让它静默播 0 秒」）"
                    + "｜🧨 把 `TryGetCue` 改成查不到也 `new WFSoundCue()` 返回 true ⇒ 本条红，而 C2 仍绿（单看 C2 看不出来）");
        Check(cue == null, "C2 ★ 查不到时 `out cue` = **null**（调用方一眼能判，不会拿到半条 cue）");
        Check(bad1 == bad0 + 1, $"C3 ★ 查不到要**记账**：`BadCues` {bad0} → {bad1}（+1）"
                              + "｜🧨 把 `BadCues++` 删掉 ⇒ 本行红：那等于「静默地什么都没播」");
        Check(WFSoundBank.PickClip(null, new System.Random(1)) == null, "C4 `PickClip(null)` 回 null（不是 NRE、也不是空 clip）");
        Check(WFSoundBank.Clip("") == null, "C5 `Clip(\"\")` 回 null（空名不落到 `Resources.Load`）");
    }

    // ==================================================================
    //  D · 环境音（`ShakeAndSoundObject` 那一族）：**表里的 cue 名必须都查得到**
    // ==================================================================
    //  判据：`CardPresentation/Effects/ShakeAndSoundObject.cs` 读 `Resources/env_shake_sounds.json`，
    //  逐条把 `sounds[i].sound`（**cue 名**）交给 `PlaySoundOnTime.Play` → `AnimFXController.PlayCue` →
    //  `WFSoundBank` 查表 ⇒ **表里没有 = 环境那一声不响（只有一条警告）**。
    //  `sound = "PID:0"` 是生成器对「原版那一格是空 PPtr / 解不出」的占位（`_stats.unresolvedCue`）⇒ 跳过。
    //  🔴 2026-11-03（`B121-D2`）：这 20 条里 `Blackout` 是**最后一条缺口**（本批补进表）。
    [Serializable] public class EnvSound { public string sound; }
    [Serializable] public class EnvEntry { public string prefab; public EnvSound[] sounds; }
    [Serializable] public class EnvTable { public EnvEntry[] entries; }

    static void GroupD_EnvShakeSoundsCovered()
    {
        var ta = Resources.Load<TextAsset>("env_shake_sounds");
        Check(ta != null, "D0 前提：`Resources/env_shake_sounds.json` 在（产出方 `工具/gen_env_shake_sounds.py`）"
                        + "｜🧨 表没了 ⇒ 环境 prefab 的定时音全哑 ⇒ 本条红");
        if (ta == null) return;

        EnvTable tbl = null;
        try { tbl = JsonUtility.FromJson<EnvTable>(ta.text); }
        catch (Exception e) { Debug.LogWarning(Tag + " 解析 env_shake_sounds.json 失败：" + e.Message); }
        Check(tbl != null && tbl.entries != null && tbl.entries.Length > 0,
              $"D1 表解析得出（entries = {(tbl != null && tbl.entries != null ? tbl.entries.Length : -1)}）");
        if (tbl == null || tbl.entries == null) return;

        int checkedN = 0, missing = 0, unresolved = 0;
        string missList = "";
        foreach (var e in tbl.entries)
        {
            if (e == null || e.sounds == null) continue;
            foreach (var s in e.sounds)
            {
                if (s == null || string.IsNullOrEmpty(s.sound)) continue;
                if (s.sound.StartsWith("PID:", StringComparison.Ordinal)) { unresolved++; continue; }
                checkedN++;
                if (!WFSoundBank.HasCue(s.sound))
                {
                    missing++;
                    if (missing <= 6) missList += $" [{e.prefab}→{s.sound}]";
                }
            }
        }
        Check(checkedN > 0, $"D2 前提：环境音表里数出 {checkedN} 条真 cue 名（`PID:` 占位 {unresolved} 条跳过）");
        Check(missing == 0,
              $"D3 ★ 环境音那 {checkedN} 条 cue 名**全部**在 `WFSoundBank` 表里（缺 {missing}{missList}）"
            + "｜🧨 删掉 `battle_sounds.json` 里的 `Blackout`（或重跑生成器把它冲掉）⇒ 本条红"
            + "：Saim Hann Blackout 那份环境 prefab 的进场音会静默不响，且只在日志里留一句警告");
    }

    // ==================================================================
    //  小工具
    // ==================================================================
    static bool Near(float a, float b) { return Mathf.Abs(a - b) < 1e-4f; }

    /// <summary>读 `Resources/&lt;path&gt;.json` 的原文（读不到 = null；调用方必须当场红）。</summary>
    static string ReadTableText(string path)
    {
        var ta = Resources.Load<TextAsset>(path);
        return ta != null ? ta.text : null;
    }

    /// <summary>从 `at`（某条 `"name": …` 的位置）**往前**找最近的 `{`，再按花括号配平切到对应的 `}`（找不到 = null）。
    /// <para>🔴 2026-11-03（`B124-W27`）：原来的 `Slice(src.Substring(at), "{", "}")` 从**名字之后**找 `{`
    /// ⇒ 拿到的是**下一条** cue 的对象体 ⇒ B4 假红（被判红的那条数据其实 `"group": "event"` 完全正确）。
    /// ⛔ 别改回「从名字往后找」—— JSON 里 `name` 是本 cue 对象的**第一个键**，对象头一定在它**之前**。</para></summary>
    static string EnclosingObject(string json, int at)
    {
        int open = json.LastIndexOf('{', at);
        if (open < 0) return null;
        int depth = 0;
        for (int i = open; i < json.Length; i++)
        {
            if (json[i] == '{') depth++;
            else if (json[i] == '}' && --depth == 0) return json.Substring(open, i - open + 1);
        }
        return null;
    }
}
