// B144_B2_Assert.cs — 执行写手 `B144-B2`（2026-11-05）· 音频三补丁的断言
//
// 覆盖的三条（判据全文 → `报告/B144_B2_纹理与音频.md` §三 P1/P2/P3 + `报告/B131_审计_音频参数.md` D2/D6/D7）：
//   P1 战斗纯语音源 `priority = 154`（原版 `AudioSource_1595.json` 的 `Priority: 154`）
//   P2 卡详情窗语音：音量 1.0 + 在「这张卡真有的后缀」里**均匀随机**（原版 `RandomElement(GetAvailableSounds())`）
//   P3 FTUE 选阵营：点阵营卡播督军 `introChatSound`（Jingles 组）
//
// ============================ 三组断言（⛔ 别只留一组） ============================
//   A 组 = **源码活行扫描**：防「补丁被改回去 / 被行尾 `//` 注释吞掉」。不跑 Unity、不依赖数据。
//   B 组 = **行为**：证明 P2 的随机**真的会变**，并配一条**灭自证**——
//          同样的输入喂【旧写法】（`TryPick(…, ForVoiceOver, null)`）必须 **恒同一句**（40 次 1 个文件名），
//          喂【新写法】必须 **出现 ≥2 个文件名**。只断「新写法有输出」是自证（恒取第一条也过）。
//   C 组 = **壳侧同族**（`Shell/CardDetailPopup.PlayVoice` 也恒取第一条）：断言它走的是**共用**的那一条
//          （行为已由 B2b 覆盖，这里只补「壳侧没另抄一份 + 音量 1.0」的源级三条）。
//
// ============================ 怎么接 ============================
//  `Editor/BattleScene.cs` 的 `Run()` 里（`B139_S_CameraSerialized_Assert.Run();` 那种独立入口旁）加一行：
//      B144_B2_Assert.Run();
//  判绿红：`grep "^B144_B2 "` 的合计行；逐条按**行首** `B144_B2 ✓` / `B144_B2 ✗` 数（⛔ 别 `grep -c ✗`）。

using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEngine;
using WarpforgeVFX;   // D 组要直接调 `WFSoundPlayer.CanPlayByCooldown` / `WFSoundBank.TryGetCue`（2026-11-05 `B144-B2` 把前者从 internal 提成 public 就是为了这里）

/// <summary>`B144-B2` 的三条音频补丁断言（源码活行 + 随机性灭自证）。</summary>
public static class B144_B2_Assert
{
    const string P = "[B144_B2]";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + " ✓ " + msg); }
        else { _fail++; Debug.LogError(P + " ✗ " + msg); }
    }

    static void Group(string name, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, name + "：**这一段抛异常**（后面的断言没跑到，⛔ 别读成通过）—— "
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Group("A·源码活行", GroupA);
        Group("B·语音随机性", GroupB);
        Group("C·壳侧卡详情同族", GroupC);
        Group("D·冷却表合并成一张", GroupD);
        Group("E·cue 引用闭合", GroupE);
        Debug.Log(P + " 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ---------------------------------------------------------------- A 组
    static List<string> LiveLines(string rel)
    {
        var list = new List<string>();
        string path = Path.Combine(Application.dataPath, rel);
        if (!File.Exists(path)) return list;
        foreach (var l0 in File.ReadAllLines(path))
        {
            if (l0.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
            list.Add(l0);
        }
        return list;
    }

    static bool Has(List<string> lines, string must)
    {
        foreach (var l in lines) if (l.Contains(must)) return true;
        return false;
    }

    /// <summary>**注释之后的部分不算**（本项目一天踩过 6 次「代码落进行末 `//` 注释被静默吞掉」）。</summary>
    static bool HasInCode(List<string> lines, string must)
    {
        foreach (var l in lines)
        {
            int c = l.IndexOf("//", StringComparison.Ordinal);
            string code = c >= 0 ? l.Substring(0, c) : l;
            if (code.Contains(must)) return true;
        }
        return false;
    }

    static void GroupA()
    {
        var drv = LiveLines("CardPresentation/Battle/BattleDriver.cs");
        var cdw = LiveLines("CardPresentation/Battle/CardDisplayWindow.cs");
        var ftue = LiveLines("CardPresentation/Shell/FTUESelectDeckWindow.cs");

        Check(drv.Count > 0 && cdw.Count > 0 && ftue.Count > 0,
              "A0 三个源文件都读到了（drv=" + drv.Count + " cdw=" + cdw.Count + " ftue=" + ftue.Count + " 行）"
              + " —— 读不到就说明路径变了，后面几条**全部不算数**");

        // ---- P1 ----
        Check(HasInCode(drv, "_unitVoiceSfx.priority = 154"),
              "A1 P1：`EnsureUnitVoiceSfx` 里**在活代码上**写了 `_unitVoiceSfx.priority = 154`（原版 `AudioSource_1595.json`）");
        Check(!HasInCode(drv, "_unitVoiceSfx.priority = 128"),
              "A2 P1 灭自证：**没有** `_unitVoiceSfx.priority = 128`（128 = Unity 默认值，写它等于没修）");

        // ---- P2 ----
        Check(HasInCode(cdw, "TryPickRandomVoice(key, out file, out text)"),
              "A3 P2：卡详情窗走的是 `TryPickRandomVoice`（随机挑），不是旧的定序挑法");
        Check(!HasInCode(cdw, "VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null"),
              "A4 P2 灭自证：**旧写法** `TryPick(key, ForVoiceOver, null)`（= 恒取第一条）**0 残留**");
        Check(HasInCode(cdw, "_voiceAudio.volume = 1f"),
              "A5 P2：音量 = `1f`（原版 `SoundManager__Play.c` 的 `set_volume(1.0)`）");
        Check(!HasInCode(cdw, "_voiceAudio.volume = 0.85f"),
              "A6 P2 灭自证：`0.85f` **0 残留**（那是我们自定的值）");

        // ---- P3 ----
        Check(HasInCode(ftue, "PlayWarlordChatSound(view.HeroCardId)"),
              "A7 P3：`RefreshContinueButton` 真的调了 `PlayWarlordChatSound(view.HeroCardId)`");
        Check(Has(ftue, "WarpforgeAudio.JinglesGroup"),
              "A8 P3：那条语音走 **Jingles** 组（原版 `Play2D(..., 3)`），⛔ 不是 FX");
        Check(Has(ftue, "new[] { \"intro\" }"),
              "A9 P3：取的是 **`intro`** 单元素（原版 `GetChatSound(0)` 读 `+0x190` = `introChatSound`；"
              + "用 `ForIntro` 会多兜一句 `default`，⛔ 与原版不符）");
    }

    // ---------------------------------------------------------------- B 组
    /// <summary>四位起始督军（`FTUESelectDeckWindow.Starters` 的 `HeroCardId`）—— 语音数据里都有多条。</summary>
    static readonly string[] Warlords = { "UM47", "GOF29", "BL35", "SAU36" };

    static void GroupB()
    {
        if (!VoiceLines.Ready)
        {
            Check(false, "B0 前提不成立：`VoiceLines.Ready == false`（`" + (VoiceLines.LoadError ?? "-") + "`）"
                       + " ⇒ 下面三条**一条都没跑**，⛔ 不是通过");
            return;
        }
        Check(true, "B0 前提：语音表已载入（" + VoiceLines.CardCount + " 卡 / " + VoiceLines.LineCount + " 条）");

        const int N = 40;

        // ---- 灭自证：旧写法必须 **恒同一条** ----
        bool oldIsConstant = true;
        int oldCards = 0;
        foreach (var id in Warlords)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < N; i++)
            {
                string f, t;
                if (VoiceLines.TryPick(id, VoiceLines.ForVoiceOver, null, out f, out t) && !string.IsNullOrEmpty(f))
                    seen.Add(f);
            }
            if (seen.Count == 0) continue;
            oldCards++;
            if (seen.Count != 1) oldIsConstant = false;
        }
        Check(oldCards > 0 && oldIsConstant,
              "B1 灭自证：**旧写法** `TryPick(ForVoiceOver, null)` 跑 " + N + " 次恒定只出 1 个文件"
              + "（有语音的督军 " + oldCards + "/" + Warlords.Length + " 位）—— 这条成立，B2 才有鉴别力");

        // ---- 新写法：至少一位督军会出现 ≥2 个文件 ----
        int varied = 0, total = 0;
        string detail = "";
        foreach (var id in Warlords)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < N; i++)
            {
                string f, t;
                if (CardDisplayWindow.TryPickRandomVoice(id, out f, out t) && !string.IsNullOrEmpty(f))
                    seen.Add(f);
            }
            if (seen.Count == 0) continue;
            total++;
            if (seen.Count >= 2) varied++;
            if (detail.Length < 120) detail += id + "=" + seen.Count + " ";
        }
        Check(total > 0, "B2a 前提：`TryPickRandomVoice` 至少对一位督军取到了语音（" + total + "/" + Warlords.Length + "）");
        Check(varied > 0,
              "B2b 新写法**真的随机**：" + N + " 次里至少一位督军出了 ≥2 个不同文件（分布 " + detail.Trim() + "）"
              + "—— 不是「恒取第一条」");

        // ---- 取到的文件必须真的能解出 clip（防「随机到一个不存在的文件」）----
        string f2 = null, t2 = null;
        bool got = false;
        foreach (var id in Warlords)
            if (CardDisplayWindow.TryPickRandomVoice(id, out f2, out t2)) { got = true; break; }
        Check(got && VoiceLines.Clip(f2) != null,
              "B2c 随机挑出来的那条**能解出 AudioClip**（" + (got ? f2 : "<一条都没取到>") + "）—— 不静默");
    }

    // ---------------------------------------------------------------- C 组（壳侧卡详情 · 同族缺陷）
    /// <summary>壳侧 `CardDetailPopup.PlayVoice` 与战斗那扇是**同一处缺陷**（恒取第一条）。
    /// 改法是**共用** `CardDisplayWindow.TryPickRandomVoice`（判据只此一处，⛔ 不复制第二份实现）
    /// ⇒ 它的**行为**已被 <see cref="GroupB"/> 的 B2b 覆盖（40 次 ≥2 个不同文件），
    /// 本组只补「壳侧真的走的是共用那条 + 音量 = 1.0」这几条**源级**断言。</summary>
    static void GroupC()
    {
        var cdp = LiveLines("CardPresentation/Shell/CardDetailPopup.cs");
        Check(cdp.Count > 0, "C0 `CardDetailPopup.cs` 读到了（" + cdp.Count + " 行）");

        Check(HasInCode(cdp, "CardDisplayWindow.TryPickRandomVoice(key, out file, out text)"),
              "C1 壳侧卡详情走的是**共用**的 `CardDisplayWindow.TryPickRandomVoice`（⛔ 不是又抄了一份实现）");
        Check(!HasInCode(cdp, "VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null"),
              "C2 灭自证：壳侧**旧写法**（`TryPick(key, ForVoiceOver, null)` = 恒取第一条）**0 残留**");
        Check(HasInCode(cdp, "WFSoundPlayer.Play(clip, true, 1f, 1f)"),
              "C3 音量 = **1.0**（`WFSoundPlayer.Play(clip, is2d:true, volume:1f, pitch:1f)`）—— 这条本轮**没改**，加断言只为锁住"
              + "（战斗那扇原来是 0.85，壳侧一直是对的）");
    }

    // ---------------------------------------------------------------- D 组（B131 C6 · 冷却表合并成一张）
    /// <summary>原版 `lastTimePlayed` **只住在 cue 资产上**、全项目**一张表**（`AudioCue__ConfigAudioSource.c:19-21`）。
    /// 合并前我们有三张（`WFSoundPlayer` / `WindowSounds` / `RemnantSfx`）⇒ 同一个 cue 经不同路径播时**不共享** 0.1 s 窗口。
    /// 本组：源级三条（两个类不再自持 cue 表 + 都走共享那条）+ 行为两条（共享表真的在挡 / 清完真的放开）。</summary>
    static void GroupD()
    {
        var ws = LiveLines("CardPresentation/Shell/WindowSounds.cs");
        var rs = LiveLines("CardPresentation/Battle/RemnantSfx.cs");

        Check(HasInCode(ws, "CanPlayByCooldown(cue)"),
              "D1 `WindowSounds` 走的是**共享表**那条（`CanPlayByCooldown(cue)`，不传自己的字典）");
        Check(!HasInCode(ws, "CanPlayByCooldown(_lastAt"),
              "D2 灭自证：`WindowSounds` 里**旧写法** `CanPlayByCooldown(_lastAt, cue, …)` **0 残留**");
        Check(HasInCode(rs, "CanPlayByCooldown(cue)") && !HasInCode(rs, "_lastAtCue"),
              "D3 `RemnantSfx` 同理：cue 那一支走共享表，本类那张 `_lastAtCue` **0 残留**（只剩注释）");

        // ---- 行为：同一张表真的在挡 ----
        WFSoundCue cue;
        if (!WFSoundBank.TryGetCue(CueWith.cooldown, out cue) || cue == null)
        {
            Check(false, "D4 前提不成立：`WFSoundBank.TryGetCue(\"" + CueWith.cooldown + "\")` 取不到 cue"
                       + " ⇒ 下面两条**没跑**，⛔ 不是通过");
            return;
        }
        Check(true, "D4 前提：拿到 cue `" + CueWith.cooldown + "`（timeToPlayAgain=" + cue.timeToPlayAgain + " s）");

        WFSoundPlayer.ResetThrottle();
        bool first = WFSoundPlayer.CanPlayByCooldown(cue);
        bool second = WFSoundPlayer.CanPlayByCooldown(cue);
        Check(first && !second,
              "D5 **共享表生效**：同一个 cue 连问两次 ⇒ 第一次放行、第二次被挡（first=" + first + " second=" + second + "）"
              + " —— 这一条同时就是「同名 cue 经不同路径播也共享同一个窗口」的行为证据（两条路走的是同一个函数）");

        WFSoundPlayer.ResetThrottle();
        Check(WFSoundPlayer.CanPlayByCooldown(cue),
              "D6 灭自证：`ResetThrottle()` 之后**又放行** ⇒ 证明 D5 的 false 来自冷却表，不是别的闸");
    }

    /// <summary>D 组用的那个 cue：必须有 `timeToPlayAgain > 0`，否则永远放行、断不出东西。</summary>
    static class CueWith { public const string cooldown = "drawCardFromDeck"; }

    // ---------------------------------------------------------------- E 组（B131 C7 的回归闸）
    /// <summary>**cue 引用闭合闸**：生产代码里凡是**字面量**喂给 cue 入口的，`WFSoundBank` 必须解析得出。
    /// <para>为什么需要它：`B131` C7 的病根是「表里没有 ⇒ 运行时只剩一条 warning、听起来像原版就没声音」（静默失败）。
    /// 这条把「代码点了名、表里却没有」变成**当场红**。</para>
    /// <para>⚠️ 只扫**生产**代码（跳过 `Editor/`）—— 自检里故意传不存在的名字做负例是正常的。
    /// ⚠️ **不收 `WFSoundBank.Clip("…")`** —— 那个参数是**clip 名**（`Resources/Art/audio/sfx/`），不是 cue 名。</para></summary>
    static void GroupE()
    {
        var root = Path.Combine(Application.dataPath, "CardPresentation");
        if (!Directory.Exists(root))
        {
            Check(false, "E0 前提不成立：`" + root + "` 不存在 ⇒ 本组一条都没跑，⛔ 不是通过");
            return;
        }

        var hits = new List<KeyValuePair<string, string>>();   // cue 名 → 文件:行
        var rx = new System.Text.RegularExpressions.Regex(
            "(?:PlayFx|PlayJingles|Play2D|Play3D|Play2DDelayed|PlayWithoutCooldown|TryGetCue)\\s*\\(\\s*\"([^\"]{2,80})\"");
        foreach (var f in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = f.Replace('\\', '/');
            if (rel.Contains("/Editor/")) continue;
            string[] lines;
            try { lines = File.ReadAllLines(f); } catch (Exception) { continue; }
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (l.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
                int c = l.IndexOf("//", StringComparison.Ordinal);
                string code = c >= 0 ? l.Substring(0, c) : l;
                foreach (System.Text.RegularExpressions.Match m in rx.Matches(code))
                    hits.Add(new KeyValuePair<string, string>(m.Groups[1].Value, Path.GetFileName(f) + ":" + (i + 1)));
            }
        }

        // 🔴 前提断言先写死：正则失配 ⇒ 0 条 ⇒ 后面「全部可解析」是**假绿**（本项目踩过多次静默跳过）
        // ⚠️ 2026-11-05（`B150-W-D`）：门槛由 5 降到 2 —— 生产已改成「常量喂 cue」，现读**只剩 2 处字面量**
        //   （`Shell/DraftModeWindow.cs:2763/2772` 的 `Draft Mode Progress Jingle` / `Draft Mode Loss Jingle`，
        //   判据 = 原版 `DraftRunDisplay.progressSound` / `lossesSound`）⇒ 把这两条当成目标，别拿旧门槛判「正则失配」。
        bool hasProgress = false, hasLoss = false;
        foreach (var kv in hits)
        {
            if (kv.Key == "Draft Mode Progress Jingle") hasProgress = true;
            if (kv.Key == "Draft Mode Loss Jingle") hasLoss = true;
        }
        Check(hits.Count >= 2 && hasProgress && hasLoss,
              "E0 前提：生产代码里扫到 **" + hits.Count + " 处**字面量 cue 调用（< 2 ⇒ 多半是正则失配；"
              + "且必须含 `Draft Mode Progress Jingle` / `Draft Mode Loss Jingle` = " + hasProgress + "/" + hasLoss + "）");

        int bad = 0;
        var detail = new System.Text.StringBuilder();
        foreach (var kv in hits)
        {
            WFSoundCue cue;
            if (WFSoundBank.TryGetCue(kv.Key, out cue) && cue != null) continue;
            bad++;
            if (detail.Length < 300) detail.Append(kv.Key + "@" + kv.Value + " ");
        }
        Check(bad == 0, "E1 每一处字面量 cue 名都能在表里解析（" + hits.Count + " 处，解析不到 " + bad + " 处 "
                        + (bad == 0 ? "" : "：" + detail) + "）");

        // ---- 灭自证：这条闸**真的会红** ----
        WFSoundCue dummy;
        Check(!WFSoundBank.TryGetCue("__B144_B2_不存在的cue__", out dummy),
              "E2 灭自证：随便编一个不存在的名字 ⇒ `TryGetCue` 必须返回 false"
              + "（否则 E1 恒绿 = 这条闸没有鉴别力）");
    }
}
