// SoundCueAssertT.cs — 执行代理 `T`（2026-10-25）· 卡牌放大窗「换位音」的断言
//
// 宿主：**`CardFaceAssertE5.Run()` 末尾的 `RunSoundCueAssertT()`** 会调本类的 `Run()`（2026-10-25 接上）。
//   ⇒ 批跑/手动跑 `-executeMethod CardFaceAssertE5.Run` 就会带上这 10 条硬断 + 1 条元断言 + 1 条软检查。
//   ⚠️ 为什么不是自己单开一个入口：**单开过，结果全仓没有一个宿主调它**（台账
//   `资料/任务线/音效线/任务总表.md:20` 记的就是这条缺陷）⇒ 断言永远跑不到 = 等于没写。
//   ✅ 本类仍然可以**单独** `-executeMethod SoundCueAssertT.Run`（保留，方便只验这一件）。
//   计数：本类的 `Passed`/`Failed` 由宿主 `+=` 并进 `E5 合计`；行前缀固定 `BT T`（同 `BT W10` 先例）。
//
// 断的是什么（判据全部来自**原版**，⛔ 不是拿我们自己的常量当尺子）：
//  · **cue 存在**：`CardDisplayWindow.soundOnCardSwap` 指的那条 cue 在我们表里查得到。
//    判据 = 战场版 MB `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4750.json`
//    的 `soundOnCardSwap = PPtr(fileID 12, m_PathID 4812904303840730115)`；
//    菜单版 `…mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2033.json` **同一个 pid**。
//  · **四个 clip 名** = `bundle_soundcollection_assets_all/MonoBehaviour/CardMove_4812904303840730115.json`
//    （`m_Name = "CardMove"`）的 `clipList` 4 条 PathID 在 bundle 里解出来的 AudioClip 名。
//  · **换位入口请求的就是它**：`CardDisplayWindow.SwapToFront()` 里那一句
//    `WindowSounds.PlayFx(SwapSoundCue, …)` —— **位置也是判据**（原版
//    `CardDisplayWindow__ChangeCardPosition.c:68-73` 在 `SetImageOverDraw` 与两条补间**之前**）。
//    这一条**读源码行**来断（本仓先例：`Editor/BattleScene.cs:1655` / `:6427` / `:6754` 都这么干）
//    —— 纯数据断言证明不了「调用点还在」。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEngine;
using WarpforgeVFX;

/// <summary>`T` 的断言集合。`Run()` 除 `WindowSounds` 的计数器外无副作用
/// （那次 `PlayFx` 会把 `WFSoundPlayer.Enabled` 临时置 false ⇒ **不建对象、不出声**）。
/// <para>🔴 **每条消息末尾的 🧨 都回答「改回旧写法会不会红」** —— 只断「现在是对的」不够。</para></summary>
public static class SoundCueAssertT
{
    public static int Passed { get; private set; }
    public static int Failed { get; private set; }

    /// <summary>原版 `CardMove` cue 的 `clipList` 4 条（顺序照资产；表里是**排序去重后**存的，
    /// 所以比较用集合、不用顺序）。</summary>
    static readonly string[] WantClips = { "CardMove", "CardMove_1", "CardMove_2", "CardMove_3" };

    /// <summary>原版 cue 资产里那三组随机参数（`CardMove_…json` 实读，2026-10-25）。</summary>
    const float WantMinPitch = 0.8f, WantMaxPitch = 1.1f;
    const float WantMinVol = 1.0f, WantMaxVol = 1.0f;
    const float WantTtp = 0.1f;

    public static void Run()
    {
        Passed = 0; Failed = 0;
        Debug.Log("BT T --- 执行代理 T（2026-10-25）：卡牌放大窗「换位音」`CardMove` cue ---");

        // ── 0. 前提：音效表载得进来（载不到的话下面每条都**没验到**，别读成通过）─────────
        Check(WFSoundBank.Loaded && WFSoundBank.CueCount > 400,
              "(前提) `Resources/animfx_sounds.json` 载得到（现读 " + WFSoundBank.CueCount + " 条 cue）"
            + " —— 表没进仓库 / 解析失败时，下面「cue 在不在」的结论全是假的");

        // ── 1. cue 存在 ────────────────────────────────────────────────────────────────
        WFSoundCue cue;
        bool ok = WFSoundBank.TryGetCue(CardDisplayWindow.SwapSoundCue, out cue) && cue != null;
        Check(ok,
              "★ T-1：换位 cue `" + CardDisplayWindow.SwapSoundCue + "` **在表里**"
            + "（原版 `soundOnCardSwap` = pid 4812904303840730115；产出方 = `工具/import_original_sfx.py` 的 "
            + "`EXTRA_CUES`）—— 🧨 从 `EXTRA_CUES` 里删掉它 / 不跑生成器 ⇒ `TryGetCue` false ⇒ 本条红"
            + "（且 `WFSoundBank.BadCues` 会在真机上涨）");
        if (!ok) { Summary(); return; }

        // ── 2. 四个 clip 名 = 原版 `clipList` 解出来的那四个 ────────────────────────────
        var got = new HashSet<string>(cue.clips ?? new string[0]);
        var want = new HashSet<string>(WantClips);
        var extra = new List<string>(); var miss = new List<string>();
        foreach (var g in got) if (!want.Contains(g)) extra.Add(g);
        foreach (var w in want) if (!got.Contains(w)) miss.Add(w);
        Check(cue.clips != null && cue.clips.Length == 4 && extra.Count == 0 && miss.Count == 0,
              "★ T-2：cue 的 clip 恰好是原版那 **4 条**（`CardMove` / `CardMove_1` / `CardMove_2` / `CardMove_3`；"
            + "实得 " + (cue.clips == null ? 0 : cue.clips.Length) + " 条："
            + string.Join(" / ", cue.clips ?? new string[0]) + "）"
            + (miss.Count == 0 ? "" : "｜✗ 少：" + string.Join(" / ", miss.ToArray()))
            + (extra.Count == 0 ? "" : "｜✗ 多：" + string.Join(" / ", extra.ToArray()))
            + " —— 🧨 收成 `CardStartDrag` 那条 1-clip 的、或把 `Cardmove`（小写 m）那条拿错 ⇒ 红");

        // ── 3. 随机参数 = 资产实读（pitch 0.8–1.1 · vol 1–1 · ttp 0.1）────────────────
        Check(Mathf.Abs(cue.minPitch - WantMinPitch) < 1e-4f && Mathf.Abs(cue.maxPitch - WantMaxPitch) < 1e-4f
           && Mathf.Abs(cue.minVolume - WantMinVol) < 1e-4f && Mathf.Abs(cue.maxVolume - WantMaxVol) < 1e-4f
           && Mathf.Abs(cue.timeToPlayAgain - WantTtp) < 1e-4f,
              "★ T-3：随机参数 = 资产实读 —— pitch " + cue.minPitch + "–" + cue.maxPitch
            + "（要 0.8–1.1）· vol " + cue.minVolume + "–" + cue.maxVolume + "（要 1–1）· ttp "
            + cue.timeToPlayAgain + "（要 0.1）"
            + " —— 🧨 认错资产 ⇒ 多半连 pitch 也一起错（`AU4` 报告曾把这条写成 pitch=1，"
            + "**资产实读是 0.8–1.1**，以资产为准）");

        // ── 4. 换位入口用的常量就是它（⛔ 别让代码名和 cue 名各说各的）────────────────
        Check(CardDisplayWindow.SwapSoundCue == "CardMove",
              "★ T-4：`CardDisplayWindow.SwapSoundCue` == `\"CardMove\"`（现读 `"
            + CardDisplayWindow.SwapSoundCue + "`）"
            + " —— 🧨 代码侧改名去迁就一个错 cue（或反过来）⇒ 红");

        // ── 5. 负例①：**没拿错**那条小写 `Cardmove`（1 clip · pitch 1.0 的另一个资产）─────
        Check(!WFSoundBank.HasCue("Cardmove"),
              "★ T-5：表里**没有**小写 `Cardmove`（`bundle_soundcollection_assets_all/MonoBehaviour/"
            + "Cardmove.json` 是**另一个** 1-clip cue，本笔不要它）"
            + " —— 🧨 把 `EXTRA_CUES` 写成 `Cardmove` ⇒ 本条红（顺带 T-2 也红）");

        // ── 6. 负例②：起拖音**没被这条顶掉**（`CardStartDrag` 仍是原来那 1-clip cue）────
        WFSoundCue drag;
        bool hasDrag = WFSoundBank.TryGetCue("CardStartDrag", out drag) && drag != null;
        Check(hasDrag && drag.clips != null && drag.clips.Length == 1 && drag.clips[0] == "CardMove",
              "★ T-6：`CardStartDrag` 仍是原样 —— 1 条 clip、名字 `CardMove`（**clip** 名，与 cue 同名属巧合；"
            + "现读 " + (hasDrag && drag.clips != null ? drag.clips.Length : 0) + " 条）"
            + " —— 🧨 两条 cue 合并 / 让起拖音也变成 4-clip ⇒ 红（原版 `DraggableController.dragSound` 就是单 clip）");

        // ── 7. **换位入口真的请求它** —— 读源码行（纯数据断言证明不了调用点还在）────────
        CheckSwapCallSite();

        // ── 8. 同一条路走得到（`WindowSounds.PlayFx` = 换位调用点用的那个口）────────────
        {
            bool prev = WFSoundPlayer.Enabled;
            try
            {
                WFSoundPlayer.Enabled = false;          // 自检：不建对象、不出声（它自己文档就写着这个用途）
                WindowSounds.ResetCounters();
                bool played = WindowSounds.PlayFx(CardDisplayWindow.SwapSoundCue, "(自检 `SoundCueAssertT`)");
                Check(played && WindowSounds.Played == 1 && WindowSounds.LastCue == CardDisplayWindow.SwapSoundCue,
                      "★ T-7：`WindowSounds.PlayFx(\"CardMove\")` **真的走通**（返回 " + played
                    + " · Played " + WindowSounds.Played + " · LastCue `" + WindowSounds.LastCue + "`）"
                    + " —— 🧨 cue 不在表里（或名字改了）⇒ 返回 false ⇒ 红；"
                    + "🧨 换位改走 `PlayJingles` 也不对（原版 `Play2D(..., 0)` = **FX**，见 T-8 的顺序断言）");
            }
            finally { WFSoundPlayer.Enabled = prev; WindowSounds.ResetCounters(); }
        }

        // ── 9·b. **元断言**：本文件的 `Run()` 至少有一个**外部宿主**在调 ───────────────
        //    2026-10-25 台账 `AUD` 查出的真缺陷就是「写好了却全仓没人调 ⇒ 10 条断言永远跑不到」。
        //    ⚠️ **局限（如实写）**：若**唯一**那个宿主调用被删，本断言自己也跑不到（自指）。
        //    它挡的是「主宿主那条被摘掉、别处还有」与「这个类哪天没人引用了」的早期信号；
        //    真正的非自指保障 = `CardFaceAssertE5.Run` 在批跑清单里（`资料/任务线/卡牌线/任务总表.md:16`
        //    的收口读数 + 界面线总表第 62 行的「待主对话跑」）。
        CheckHasExternalHost();

        // ── 9. 软检查：4 个 clip 的**音频文件**加载得到（`Resources/Art/` 是 gitignore 的，
        //        新克隆里没有 ⇒ 只报不红，与本仓既有口径一致：`Editor/BattleScene.cs:14373-14374`）─
        {
            var bad = new List<string>();
            foreach (var cl in WantClips) if (WFSoundBank.Clip(cl) == null) bad.Add(cl);
            if (bad.Count == 0)
                Debug.Log("BT T ✓ T-8（软）：4 条 clip 的音频文件都加载得到（`Resources/Art/audio/sfx/`）");
            else
                Debug.LogWarning("BT T ⚠ T-8（软）：这 " + bad.Count + " 条 clip 音频文件加载不到："
                               + string.Join(" / ", bad.ToArray())
                               + " —— `Resources/Art/` 是 gitignore 的（新克隆没有属正常）；"
                               + "若本机也缺，跑 `python 工具/import_original_sfx.py`");
        }

        Summary();
    }

    // ======================================================================================
    //  元断言：`SoundCueAssertT.Run()` 有没有**外部宿主**在调（防「写好了永远跑不到」）
    // ======================================================================================
    //   2026-10-25 台账查出的缺陷就是这一条：本类当时**全仓零调用** ⇒ 所有断言等于不存在。
    //   做法 = 扫 `Assets/**/*.cs` 的非注释行，找 `SoundCueAssertT.Run()`，**排除本文件自己**。
    static void CheckHasExternalHost()
    {
        var hosts = new List<string>();
        try
        {
            foreach (var f in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(f) == "SoundCueAssertT.cs") continue;    // 自己不算宿主
                var ls = File.ReadAllLines(f);
                for (int i = 0; i < ls.Length; i++)
                {
                    string s = ls[i].Trim();
                    if (s.StartsWith("//") || s.StartsWith("*") || s.StartsWith("/*")) continue;
                    if (s.Contains("SoundCueAssertT.Run()"))
                    {
                        hosts.Add(Path.GetFileName(f) + ":" + (i + 1));
                        break;                                                  // 一个文件只记一次
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            // 不许静默：扫不动 = **没验到**，不能让它看起来像「查过、有宿主」
            Check(false, "T-9（元）：扫 `Assets/**/*.cs` 找宿主时抛异常 ⇒ **没有验到**："
                       + e.GetType().Name + ": " + e.Message);
            return;
        }

        Check(hosts.Count > 0,
              "★ T-9（元）：`SoundCueAssertT.Run()` **至少有一个外部宿主**在调（现读 " + hosts.Count
            + " 处：" + (hosts.Count == 0 ? "**没有**" : string.Join(" / ", hosts.ToArray())) + "）"
            + " —— 🧨 把宿主的调用删掉（本类就此变成「写了永远跑不到」，正是 2026-10-25 台账那条缺陷）"
            + " ⇒ 0 处 ⇒ 红。⚠️ 自指局限：若**唯一**宿主被删，本断言自己也跑不到 —— 非自指保障 = "
            + "`CardFaceAssertE5.Run` 在批跑清单里");
    }

    // ======================================================================================
    //  换位入口的调用点（读源码）：三件事一起断 —— ①存在 ②只一处 ③**位置**对
    // ======================================================================================
    static void CheckSwapCallSite()
    {
        const string rel = "CardPresentation/Battle/CardDisplayWindow.cs";
        string path = Path.Combine(Application.dataPath, rel);
        if (!File.Exists(path))
        {
            // 不许静默：读不到源码 = 这一条**没验到**，别让输出看起来像「验过、没问题」
            Failed++;
            Debug.LogError("BT T ✗ T-8：找不到源码 `" + path + "` ⇒ 「换位入口请求的是它」**没有验到**"
                         + "（不是「验过没问题」）");
            return;
        }

        var lines = File.ReadAllLines(path);
        int iSwapTrue = -1, iPlay = -1, iOverdraw = -1, nPlay = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string s = lines[i].Trim();
            if (s.StartsWith("//") || s.StartsWith("*") || s.StartsWith("/*")) continue;   // 注释行不算
            if (s == "_swapping = true;") iSwapTrue = i;
            if (s.Contains("WindowSounds.PlayFx(SwapSoundCue")) { nPlay++; if (iPlay < 0) iPlay = i; }
            if (s.StartsWith("b.ArtOverdraw = true;")) iOverdraw = i;
        }

        Check(nPlay == 1,
              "★ T-8a：`SwapToFront` 里 `WindowSounds.PlayFx(SwapSoundCue, …)` **恰好一处**（非注释行，现读 "
            + nPlay + " 处）"
            + " —— 🧨 删掉这行（换位变默片）⇒ 0 处 ⇒ 红；🧨 换成写死别的 cue 名 ⇒ `SwapSoundCue` 不再被引用 ⇒ 红");
        Check(nPlay == 1 && iSwapTrue >= 0 && iOverdraw >= 0 && iSwapTrue < iPlay && iPlay < iOverdraw,
              "★ T-8b：**位置**也是判据 —— `_swapping = true;`（行 " + (iSwapTrue + 1) + "） < PlayFx（行 "
            + (iPlay + 1) + "） < `b.ArtOverdraw = true;`（行 " + (iOverdraw + 1) + "）"
            + "（原版 `ChangeCardPosition.c:68-73`：播音在 `SetImageOverDraw` 与两条补间**之前**）"
            + " —— 🧨 把它挪进 `OnComplete`（= `CardSwapFinished` 之后）/ 挪到补间之后 ⇒ 顺序不成立 ⇒ 红");
    }

    static void Check(bool ok, string msg)
    {
        if (ok) { Passed++; Debug.Log("BT T ✓ " + msg); }
        else { Failed++; Debug.LogError("BT T ✗ " + msg); }
    }

    static void Summary()
    {
        string line = "BT T === 断言合计：" + Passed + " 通过 / " + Failed + " 失败 ===";
        if (Failed == 0) Debug.Log(line);
        else Debug.LogError(line + "（失败条目见上面每一条 `BT T ✗`）");
    }
}
