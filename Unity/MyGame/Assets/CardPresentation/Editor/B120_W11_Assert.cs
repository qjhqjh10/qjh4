// B120_W11_Assert.cs — 执行代理 `B120-W11`（`B120-Q09③`：原版 `AudioSourceMixerGroupFixer` 80 个实例）
//
// ============================ 它盯的是哪几条 ============================
//  A. **数据表完整性**：`Resources/audio_mixer_fixers.json` 必须与原版 80 个实例一一对应
//     （13 场 × 6 + `MusicManager` × 2），且 13 场的 6 条路径**逐场同构**、`mixerType` 分布固定
//     （聊天三源 = 2；BGM 三轨 = 4/5/6；MusicManager 两颗 = 1）。
//  B. **组件语义**：`AudioSourceMixerGroupFixer.Apply()` 必须把 `mixerType` 对应的组写进
//     `AudioSource.outputAudioMixerGroup`（= 原版 `AudioSourceMixerGroupFixer__Start.c` 的全部方法体）；
//     `OnValidate()` 必须补 `audioSource`；同一 GO 上**不许叠第二件**。
//  C. **补挂链路**：按数据表路径把对象摆出来之后，`AttachArena` 的挂上数必须**等于清单数**，
//     且每一颗源的组 = `GroupFor(mixerType)`。13 场逐场验。
//  D. **结构闸**：arena prefab 里 `AudioSource` = 0（⇒ 补挂只能在三条创建链上，见报告）；
//     本组件与 `ArenaRuntimeLoader` 的接线句真的在**活代码**里。
//  E. **接线闸**：三条创建链（`BattleMusic` / `UnitChatPanel` / `ShellRuntime`）各一行 `Attach(...)`。
//     ⚠️ **未接线时这几条是红的**（显式 `Tr(false, …未接线…)`，⛔ 不静默）—— 补丁见
//     `资料/完全复刻/报告/B120_W11_执行.md` §接线。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接在 `Editor/BattleScene.cs` 的**方法尾聚合行**（`B120_W3_Assert.Run(); …` 那一行之后、
//  `Debug.Log(P + $"=== 合计…")` **之前**），同一物理行追加 ⇒ 行数零变化：
//    ` B120_W11_Assert.Run(); pass += B120_W11_Assert.Pass; fail += B120_W11_Assert.Fail;`
//  🔑 本件**零前提**：自带合成层级（每场建完自己 `DestroyImmediate`），不依赖活 driver / canvas 宿主。
//
// ============================ 🔴 灭自证 ============================
//  · C3：**没挂 fixer** 的源，组必须还是 null ⇒ 说明「组不是天上掉下来的」（挂上 fixer 才变）；
//    把 `Attach` 改成空操作 ⇒ C1 的挂上数变 0 ⇒ 红。
//  · B3：把 `mixerType` 从 `Voices` 挪成 `BattleGroupLow` ⇒ 组跟着变，且**不等于** `VoicesGroup` ⇒ 红/绿互换。
//  · A5：把表里某条 `mixerType` 改错 ⇒ 该场分布断言红（改数据文件即可复现）。
using System;
using System.IO;
using System.Text;
using CardPresentation;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>执行代理 `B120-W11`：`AudioSourceMixerGroupFixer`（80 个实例）的落地断言。
/// <para>⛔ 期望值全是**原版常量**（`MixerType` 七值 / 13 场 × 6 / MusicManager × 2），⛔ 不从被测实现里读回来。</para></summary>
public static class B120_W11_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B120W11] ") { Near = MenuNearStyle.Labeled4 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const string SrcFixer = "Assets/CardPresentation/Core/AudioSourceMixerGroupFixer.cs";
    const string SrcLoader = "Assets/CardPresentation/Battle/ArenaRuntimeLoader.cs";
    const string SrcMusic = "Assets/CardPresentation/Battle/BattleMusic.cs";
    const string SrcChat = "Assets/CardPresentation/Battle/UnitChatPanel.cs";
    const string SrcShell = "Assets/CardPresentation/Shell/ShellRuntime.cs";
    const string Prefab1 = "Assets/Resources/ArenaPrefabs/battlearena1.prefab";

    /// <summary>原版 13 个战场键（与 `Resources/audio_mixer_fixers.json` 的 `arena` 值同一份口径）。</summary>
    static readonly string[] Arenas =
    {
        "battlearena1", "battlearena2", "battlearena3",
        "battlearenaaeldari", "battlearenaastramilitarum", "battlearenablacklegion",
        "battlearenadarkangels", "battlearenaemperorschildren", "battlearenagenestealers",
        "battlearenaleviathan", "battlearenasororitas", "battlearenaspacewolves",
        "battlearenatauviorla",
    };

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B120-W11 · AudioSourceMixerGroupFixer（80 实例：13 场 × 6 + MusicManager × 2）");
        RunGroup("A · 数据表完整性", A_Table);
        RunGroup("B · 组件语义（Apply / OnValidate / 幂等）", B_Component);
        RunGroup("C · 补挂链路（13 场逐场）", C_AttachArena);
        RunGroup("D · 结构闸", D_SourceGuards);
        RunGroup("E · 三条创建链接线闸", E_Wiring);
        Debug.Log(_sink.P + "B120_W11 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void RunGroup(string name, Action body)
    {
        MenuCheck.Section(_sink, name);
        try { body(); }
        catch (Exception e)
        {
            // 说清是**这一组**炸了（⛔ 不许吞成一条静默的「没验」）
            Tr(false, name + "：这一组抛异常 ⇒ 本组结论**未验**：" + e.GetType().Name + " " + e.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  A. 数据表完整性（原版 80 = 13×6 + 2）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void A_Table()
    {
        var t = AudioSourceMixerGroupFixerInstaller.LoadTable();
        Tr(t != null, "A0 前提：`Resources/audio_mixer_fixers.json` 读得到并解析成功");
        if (t == null) return;

        Tr(AudioSourceMixerGroupFixerInstaller.TableTotal == 80,
           "A1 ★ 清单总数 = **80**（原版 13 场 × 6 + MusicManager × 2；实得 "
         + AudioSourceMixerGroupFixerInstaller.TableTotal + "）"
         + "｜🧨 少一条/多一条 ⇒ 本条红");

        Tr(t.arenas != null && t.arenas.Length == 13,
           "A2 表里正好 13 场（实得 " + (t.arenas != null ? t.arenas.Length : 0) + "）");
        if (t.arenas == null) return;

        int perArenaBad = 0, distBad = 0, voicesRows = 0, low = 0, mid = 0, high = 0;
        string firstSig = null;
        bool sigSame = true;
        foreach (var a in t.arenas)
        {
            if (a == null || a.rows == null) { perArenaBad++; continue; }
            if (a.rows.Length != 6) perArenaBad++;

            int v = 0, l = 0, m = 0, h = 0;
            var sig = new StringBuilder();
            foreach (var r in a.rows)
            {
                if (r == null) continue;
                if (r.mixerType == 2) v++;
                else if (r.mixerType == 4) l++;
                else if (r.mixerType == 5) m++;
                else if (r.mixerType == 6) h++;
                sig.Append(r.mixerType).Append(':').Append(r.path).Append('\n');
            }
            voicesRows += v; low += l; mid += m; high += h;
            if (v != 3 || l != 1 || m != 1 || h != 1) distBad++;
            if (firstSig == null) firstSig = sig.ToString();
            else if (firstSig != sig.ToString()) sigSame = false;
        }

        Tr(perArenaBad == 0, "A3 ★ 每场正好 **6** 条（实得 " + (13 - perArenaBad) + "/13 场合规）");
        Tr(distBad == 0, "A4 ★ 每场分布固定：Voices×3 + BattleGroupLow/Medium/High 各 1（不合规场数 " + distBad + "）"
                       + "｜🧨 把任一条 `mixerType` 改错 ⇒ 本条红");
        Tr(voicesRows == 39 && low == 13 && mid == 13 && high == 13,
           "A5 全表分布：Voices " + voicesRows + "(=39) · Low " + low + "(=13) · Medium " + mid + "(=13) · High " + high + "(=13)");
        Tr(sigSame, "A6 ★ 13 场的 (mixerType, path) 集合**逐场完全相同**（原版每场同构：同一套 6 条路径）");

        Tr(t.musicManager != null && t.musicManager.Length == 2,
           "A7 `MusicManager` 上 **2** 件（实得 " + (t.musicManager != null ? t.musicManager.Length : 0) + "；"
         + "原版 `bundle_staticgeneralassets_assets_all/GameObject/MusicManager.json` 两件都 `mixerType = 1`）");
        bool mmOk = true;
        if (t.musicManager != null)
            foreach (var r in t.musicManager)
                if (r == null || r.mixerType != 1 || r.path != "MusicManager") mmOk = false;
        Tr(mmOk, "A8 `MusicManager` 那两件都 `mixerType = 1 (Music)`、`path = MusicManager`");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  B. 组件语义（= 原版 `Start` / `OnValidate` 的全部方法体）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void B_Component()
    {
        Tr(WarpforgeAudio.VoicesGroup != null,
           "B0 前提：`WarpforgeAudio.VoicesGroup` 取得到（`Resources/Audio/Main Mixer` 的 `Voices` 组）");
        Tr(WarpforgeAudio.BattleGroupGroup(0) != null,
           "B0b 前提：`WarpforgeAudio.BattleGroupGroup(0)` 取得到（`Resources/Audio/Music` 的 `BattleGroupLow` 组）");

        var go = new GameObject("B120W11_Fixer");
        try
        {
            var src = go.AddComponent<AudioSource>();
            Tr(src.outputAudioMixerGroup == null, "B1 前提：新源一开始没有混音组（否则 B2 无判别力）");
            var f = AudioSourceMixerGroupFixerInstaller.Attach(src, MixerType.Voices);
            Tr(f != null, "B1b `Attach` 返回了一件 fixer");
            if (f == null) return;

            Tr(src.outputAudioMixerGroup == WarpforgeAudio.VoicesGroup,
               "B2 ★ `MixerType.Voices` ⇒ `outputAudioMixerGroup == WarpforgeAudio.VoicesGroup`"
             + "（判据 = `AudioSourceMixerGroupFixer__Start.c` 全部方法体）｜🧨 `Attach` 改成空操作 ⇒ 本条红");

            // 灭自证 A：换一档 ⇒ 组跟着换，且**不是**原来那个
            f.Setup(src, MixerType.BattleGroupLow);
            f.Apply();
            Tr(src.outputAudioMixerGroup == WarpforgeAudio.BattleGroupGroup(0),
               "B3 ★ 改成 `BattleGroupLow` ⇒ 组跟着换成 `BattleGroupGroup(0)`（灭自证：把 `mixerType` 挪错档 ⇒ 组错 ⇒ 红）");
            Tr(!(src.outputAudioMixerGroup == WarpforgeAudio.VoicesGroup),
               "B4 ★ 灭自证：此时**不再等于** `VoicesGroup`（两档的判别式，结构上不可能同时满足）");

            // OnValidate：字段为空时自动 GetComponent<AudioSource>()
            // 🔴 2026-11-03（`B124-W27`）：**编辑器下 `AddComponent` 会自动跑一次 `OnValidate`**
            //   （`Core/AudioSourceMixerGroupFixer.cs:90-93` 第一句就补 `GetComponent<AudioSource>()`）
            //   ⇒ 原来那句「新建的这一件 `audioSource` 是空的」前提**必假**。按真实时序改成两条：
            //   ① 建出来就该被自动补上（编辑器行为 = 真实兜底）；② 反射清空后再显式喊一次，验回调本身。
            var f2 = go.AddComponent<AudioSourceMixerGroupFixer>();
            Tr(f2.Source == src, "B5 ★ 编辑器 `AddComponent` 即跑 `OnValidate` ⇒ 字段一建就被补上（实得 "
                + (f2.Source == null ? "null" : f2.Source.name) + "，⛔ 不是像原来那样断「一建就是空」）");
            typeof(AudioSourceMixerGroupFixer).GetField("audioSource",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(f2, null);
            Tr(f2.Source == null, "B5b 前提：反射清空之后字段确实为空（否则 B6 无判别力）");
            f2.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);   // 私有、编辑器回调 ⇒ 显式喊一次
            Tr(f2.Source == src,
               "B6 ★ `OnValidate()` 把同 GO 上的 `AudioSource` 补进字段（`AudioSourceMixerGroupFixer__OnValidate.c`）");

            // 幂等：同一 GO 上不许叠第二件
            int before = go.GetComponents<AudioSourceMixerGroupFixer>().Length;
            AudioSourceMixerGroupFixerInstaller.Attach(src, MixerType.Voices);
            int after = go.GetComponents<AudioSourceMixerGroupFixer>().Length;
            Tr(before == after, "B7 ★ `Attach` 幂等：同一 GO 上 fixer 件数不变（" + before + " → " + after + "）");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  C. 补挂链路：按数据表的 6 条路径摆出层级 ⇒ `AttachArena` 挂上数 = 清单数（13 场逐场）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void C_AttachArena()
    {
        var table = AudioSourceMixerGroupFixerInstaller.LoadTable();
        Tr(table != null, "C0 前提：数据表读得到");
        if (table == null) return;

        int badCount = 0, badGroup = 0, arenasOk = 0;
        foreach (var arena in Arenas)
        {
            var rows = AudioSourceMixerGroupFixerInstaller.ArenaRows(arena);
            if (rows == null) { badCount++; continue; }

            var root = new GameObject(arena);                 // ⚠️ 根名 = 场名（原版路径以 `BattlePrefab` 起头，这里是子节点）
            try
            {
                // ① 按表把 6 条路径原样摆出来（每段一个节点），叶子挂 `AudioSource`
                var leaves = new Transform[rows.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    var tr = EnsurePath(root.transform, rows[i].path);
                    leaves[i] = tr;
                    tr.gameObject.AddComponent<AudioSource>();
                }

                int arenaBadGroup = 0;

                // ② 灭自证的**前提**：挂之前它们全是 null
                bool allNullBefore = true;
                foreach (var tr in leaves) if (tr.GetComponent<AudioSource>().outputAudioMixerGroup != null) allNullBefore = false;
                Tr(allNullBefore, "C1 [" + arena + "] 前提：挂 fixer 之前 6 颗源的组都是 null（否则挂上之后的变化没有判别力）");
                if (!allNullBefore) arenaBadGroup++;

                // ③ 补挂
                int n = AudioSourceMixerGroupFixerInstaller.AttachArena(root, arena);
                if (n != rows.Length) badCount++;
                Tr(n == rows.Length,
                   "C2 [" + arena + "] ★ `AttachArena` 挂上数 = 清单数（" + n + " / " + rows.Length + "）"
                 + "｜🧨 把 `Attach` 改成空操作或 `Resolve` 断链 ⇒ 本条红");

                // ④ 逐条验组
                for (int i = 0; i < rows.Length; i++)
                {
                    var got = leaves[i].GetComponent<AudioSource>().outputAudioMixerGroup;
                    var want = AudioSourceMixerGroupFixerInstaller.GroupFor((MixerType)rows[i].mixerType);
                    if (!(got == want)) arenaBadGroup++;
                }
                Tr(arenaBadGroup == 0, "C3 [" + arena + "] ★ 6 颗源的 `outputAudioMixerGroup` 全等于 `GroupFor(mixerType)`"
                                     + "（本场不符 " + arenaBadGroup + " 条）｜🧨 挂错档/漏挂 ⇒ 本条红");
                badGroup += arenaBadGroup;
                if (arenaBadGroup == 0) arenasOk++;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        Tr(badCount == 0, "C4 ★ 13 场全部「挂上数 = 清单数」（不合规场数 " + badCount + "）");
        Tr(arenasOk == 13, "C5 ★ 13 场全部「每颗源的组都对」（合规场数 " + arenasOk + "/13 · 累计不符 " + badGroup + " 条）");

        // ⑤ MusicManager 那两件（同一 GO 上两颗源，都是 `Music`）
        var mm = new GameObject("MusicManager");
        try
        {
            var s1 = mm.AddComponent<AudioSource>();
            var s2 = mm.AddComponent<AudioSource>();
            AudioSourceMixerGroupFixerInstaller.Attach(s1, MixerType.Music);
            AudioSourceMixerGroupFixerInstaller.Attach(s2, MixerType.Music);
            Tr(s1.outputAudioMixerGroup == WarpforgeAudio.MusicGroup && s2.outputAudioMixerGroup == WarpforgeAudio.MusicGroup,
               "C6 ★ `MusicManager` 那两颗（`mixerType = 1`）都挂到 `WarpforgeAudio.MusicGroup`"
             + "（原版 `AudioSource_2305…/8606…` 的序列化组 = `General`，即 Music 子 mixer 的 `General`）");
        }
        finally { UnityEngine.Object.DestroyImmediate(mm); }
    }

    /// <summary>按 `a/b/c` 逐段建（已存在就复用）。</summary>
    static Transform EnsurePath(Transform root, string path)
    {
        var cur = root;
        foreach (var seg in path.Split('/'))
        {
            var next = cur.Find(seg);
            if (next == null)
            {
                var go = new GameObject(seg);
                go.transform.SetParent(cur, false);
                next = go.transform;
            }
            cur = next;
        }
        return cur;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  D. 结构闸
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void D_SourceGuards()
    {
        // D1：arena prefab 里 AudioSource 组件 = 0（⇒ 补挂点只能在那三条创建链上，⛔ 不是「忘了做」）
        string prefab = Read(Prefab1);
        Tr(prefab != null, "D0 前提：读得到 `" + Prefab1 + "`");
        if (prefab != null)
        {
            int n = CountOccurrences(prefab, "\n--- !u!82 &");
            Tr(n == 0, "D1 ★ `battlearena1.prefab` 里 `AudioSource` 组件 = **0**（实得 " + n + "）——"
                     + " 这解释了为什么补挂点不在 prefab 上；🧨 若有人把音源烘回 prefab，本条会红（提示重看接线）");
        }

        GuardContains(SrcFixer, "audioSource.outputAudioMixerGroup = group;",
                      "D2 `AudioSourceMixerGroupFixer.Apply()` 的**唯一**动作在活代码里（= 原版 `Start` 的方法体）");
        GuardContains(SrcFixer, "private void OnValidate()",
                      "D3 `OnValidate()` 在活代码里（原版同名回调）");
        GuardContains(SrcFixer, "case MixerType.BattleGroupHigh:",
                      "D4 `GroupFor` 覆盖到 `BattleGroupHigh`（7 档映射收口在这一处，⛔ 不新造第二套 mixer 机制）");
        GuardContains(SrcLoader, "AudioSourceMixerGroupFixerInstaller.AttachArena(Current, arenaKey);",
                      "D5 `ArenaRuntimeLoader.Load()` 真的调了补挂（装配链接线）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  E. 三条创建链接线闸（未接线 ⇒ **红**，⛔ 不静默；补丁见报告 §接线）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void E_Wiring()
    {
        // 39 颗（13 场 × 3 条 BGM 轨）
        WiringCheck(SrcMusic, "AudioSourceMixerGroupFixerInstaller.Attach(",
                    "`BattleMusic.Initialize` 的 3 条 BGM 轨（13 场 × 3 = 39 件）");
        // 39 颗（13 场 × 3 颗聊天气泡；原版 3 个 `*ChatAudioSource` 与我们的 3 颗气泡一一对应）
        WiringCheck(SrcChat, "AudioSourceMixerGroupFixerInstaller.Attach(",
                    "`UnitChatPanel.MakeBubble` 的 3 颗聊天源（13 场 × 3 = 39 件）");
        // 2 颗（`MusicManager` 上的两件，`mixerType = 1`）
        WiringCheck(SrcShell, "AudioSourceMixerGroupFixerInstaller.Attach(",
                    "`ShellRuntime.MakeMusicSource` 的两颗菜单音乐源（= `MusicManager` 那 2 件）");
    }

    static void WiringCheck(string path, string needle, string what)
    {
        string src = Read(path);
        bool has = src != null && src.IndexOf(needle, StringComparison.Ordinal) >= 0;
        Tr(has, "E " + what + "：`" + needle + "` 在活代码里"
              + (has ? "" : " —— ⛔ **未接线**（补丁 → `资料/完全复刻/报告/B120_W11_执行.md` §接线；接上之前这 80 件只有数据与组件、没有装配）"));
    }

    // ============================================================ 小工具
    static string Read(string path)
    {
        try { if (File.Exists(path)) return File.ReadAllText(path); }
        catch (Exception e) { Debug.LogWarning("[B120W11] 读不到源码（" + e.GetType().Name + "）：" + path); }
        return null;
    }

    static void GuardContains(string path, string need, string msg)
    {
        string src = Read(path);
        Tr(src != null, "（前提）源码读得到：" + path);
        if (src == null) return;
        Tr(src.IndexOf(need, StringComparison.Ordinal) >= 0, msg + "（要找的片段：「" + need + "」）");
    }

    static int CountOccurrences(string haystack, string needle)
    {
        int n = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }
}
