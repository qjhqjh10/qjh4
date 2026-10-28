// AudioSetup.cs — 编辑器入口：**建 / 复核音频通道（AudioMixer）**
//
// 为什么要有它：原版的音量三滑块走 `AudioMixer.SetFloat("Volume"+MixerType枚举名, dB)`，
// 而 `SetFloat` **只认 exposed parameter** ⇒ 必须有一份**参数名对得上**的 mixer 资产。
// 工程里原本一个都没有（`find -iname *.mixer` 零命中），原版那份在
// `bundle_audiocontrol_assets_all` 里（native 资产，导不进工程）⇒ 只能自己建。
//
// 🔴 **三步顺序不能换**（第 2 步是 python，因为 Unity 内部 API 有两处它自己搞不定）：
//
//   ① `-executeMethod AudioSetup.CreateMixer` —— 建 `Resources/Audio/Main Mixer.mixer`
//      （`UnityEditor.Audio.AudioMixerController.CreateMixerControllerAtPath` + `CreateNewGroup`）
//   ② `PYTHONIOENCODING=utf-8 python 工具/setup_audio_mixer.py`
//      —— 补两样 API 补不了的：**组挂到 Master 下**（`CreateNewGroup` 建出来的是孤儿，
//        `Master.m_Children` 是空的 ⇒ 没有信号通路、声音出不来）·
//        **按原版名暴露 `VolumeFX/Music/Voices/Jingles`**（`AudioParameterPath` 只有一个 GUID 字段，
//        没有「哪种参数」的信息）。`.mixer` 是文本 YAML，**组的音量参数 GUID 就是它自己的
//        `m_Volume` 字段**（⚠️ **不是** `m_Effects[0]` 那个 `Attenuation` 效果的 `m_MixLevel` ——
//        取错时 `AudioMixer.GetFloat("VolumeFX")` 返回 false，已经踩过一次，别再改回去）。
//   ③ `-executeMethod AudioSetup.Verify` —— 复核（读回参数名 + 设进去再读出来）
//
// ⚠️ `CreateMixer` **不覆盖已存在的文件**（覆盖会把第 ② 步的补丁抹掉）。要重建就先删。
using System;
using System.Collections.Generic;   // 🆕 2026-10-27：`VerifySnapshots` 的 `List<string>`
using System.IO;
using System.Reflection;
using CardPresentation;             // 🆕 `WarpforgeAudio.SnapshotNames`（快照名只此一份）
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class AudioSetup
{
    public const string MixerAssetPath = "Assets/CardPresentation/Resources/Audio/Main Mixer.mixer";
    /// <summary>运行时按 `Resources.Load&lt;AudioMixer>(MixerResource)` 取。</summary>
    public const string MixerResource = "Audio/Main Mixer";

    /// <summary>原版 `MixerType` 里我们要用的四路（`Jingles` 在原版与 FX 同值）。</summary>
    public static readonly string[] Groups = { "FX", "Music", "Voices", "Jingles" };

    [MenuItem("Warpforge/音频/建 AudioMixer")]
    public static void CreateMixer()
    {
        if (File.Exists(MixerAssetPath))
        {
            Debug.Log($"AUD 已存在，**不覆盖**（覆盖会抹掉 setup_audio_mixer.py 的补丁）：{MixerAssetPath}");
            Debug.Log("AUD 要重建：先删掉它，再跑本入口 + 工具/setup_audio_mixer.py");
            return;
        }
        Type ctl = FindControllerType();
        if (ctl == null)
        {
            // 不许静默失败：这条不通就等于**音量三滑块做不了**
            Debug.LogError("AUD 找不到 `UnityEditor.Audio.AudioMixerController` —— " +
                           "这个 Unity 版本里内部 API 改名了，得换建法（见文件头）");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(MixerAssetPath));
        var mixer = ctl.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static)
                       .Invoke(null, new object[] { MixerAssetPath });
        var createGroup = ctl.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.Instance);
        foreach (var g in Groups)
        {
            var grp = createGroup.Invoke(mixer, new object[] { g, false });
            Debug.Log($"AUD 建组 {g} → {(grp != null ? "好" : "**失败**")}");
        }

        EditorUtility.SetDirty(mixer as UnityEngine.Object);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"AUD 建好 {MixerAssetPath}（大小 {(File.Exists(MixerAssetPath) ? new FileInfo(MixerAssetPath).Length : 0)} 字节）");
        Debug.Log("AUD 下一步：PYTHONIOENCODING=utf-8 python 工具/setup_audio_mixer.py");
    }

    /// <summary>复核：参数名读得回、设得进；组树从 YAML 里数。</summary>
    public static void Verify()
    {
        int pass = 0, fail = 0;
        Action<bool, string> chk = (ok, what) =>
        {
            if (ok) { pass++; Debug.Log("AUD   ✓ " + what); }
            else { fail++; Debug.LogError("AUD   ✗ " + what); }
        };

        var mixer = Resources.Load<AudioMixer>(MixerResource);
        chk(mixer != null, $"`Resources/{MixerResource}` 加载得到");
        if (mixer == null) { Done(pass, fail); return; }
        // 音频子系统在这个进程里到底起没起来 —— 决定下面那条「读回」算不算数
        Debug.Log($"AUD   ⓘ AudioSettings: outputSampleRate={AudioSettings.outputSampleRate} · "
                + $"driverCapabilities={AudioSettings.driverCapabilities} · "
                + $"speakerMode={AudioSettings.speakerMode}（batchmode 通常是 0 = 没有设备）");

        // ---- 🔬 对照实验：`SetFloat` 到底生不生效（不是「读回时机」的问题吧？）----
        {
            float a, b, c;
            mixer.GetFloat("VolumeFX", out a);
            mixer.SetFloat("VolumeFX", -20f);
            mixer.GetFloat("VolumeFX", out b);                      // 立刻读
            System.Threading.Thread.Sleep(300);                     // 给音频线程 300ms
            mixer.GetFloat("VolumeFX", out c);                      // 再读
            Debug.Log($"AUD 🔬 VolumeFX 初值 {a:F2} → 设 −20 → 立刻读 {b:F2} → 300ms 后读 {c:F2} dB");
            var mi = typeof(AudioMixer).GetMethods(System.Reflection.BindingFlags.Public
                                                   | System.Reflection.BindingFlags.Instance);
            var sig = new System.Text.StringBuilder();
            foreach (var m in mi)
                if (m.Name.StartsWith("Set") || m.Name.StartsWith("Get") || m.Name == "Update")
                    sig.Append(m.Name).Append('(')
                       .Append(string.Join(",", System.Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name)))
                       .Append(") ");
            Debug.Log("AUD 🔬 AudioMixer 相关 API：" + sig);
            mixer.SetFloat("VolumeFX", 0f);
        }

        foreach (var g in Groups)
        {
            string p = "Volume" + g;
            float before;
            bool found = mixer.GetFloat(p, out before);
            chk(found, $"暴露参数 `{p}` 在（`GetFloat` 认得它；初值 {before:F2} dB = 满音量）");
            if (!found) continue;

            // ⚠️ **读回往返在这个环境里验不了**，如实说明、不假装：
            //    批处理**没有音频设备也没有音频更新循环**，`SetFloat` 的结果读回来还是旧值
            //    （实测「设 −13.5 → 读到 0.00」）。参数**存在**（上面那条）是硬证据 ——
            //    名字对不上时 `GetFloat` 会返回 false。真跑一遍要在**带音频的 Play / 真包**里做。
            mixer.SetFloat(p, -13.5f);
            float after;
            mixer.GetFloat(p, out after);
            Debug.Log($"AUD   ⓘ `{p}` 批处理下 SetFloat(−13.5) 读回 {after:F2} dB " +
                      "（**这不是失败**，是批处理没有音频更新循环；见本方法注释）");
            mixer.SetFloat(p, 0f);                       // 还原成 0 dB（= 满音量）
        }

        // 组树从 YAML 里数（运行时读不到父子关系）
        if (File.Exists(MixerAssetPath))
        {
            var yaml = File.ReadAllText(MixerAssetPath);
            int names = 0;
            foreach (var g in Groups) if (yaml.Contains("m_Name: " + g)) names++;
            chk(names == Groups.Length, $"YAML 里 {Groups.Length} 个组名都在（实测 {names}）");
            var m = System.Text.RegularExpressions.Regex.Match(yaml,
                @"m_Name: Master\n(?:  .*\n)*?  m_Children:[ ]*(\[\])?(?:\n  - \{fileID: -?\d+\})+");
            // ⚠️ 挂好的写法是 `m_Children: `（**带尾空格**）换行后跟 `  - {fileID: …}` —— 不是 `[]`。
            bool parented = m.Success && m.Groups[1].Value != "[]";
            chk(parented, "`Master.m_Children` 不是空的（**空的 = 组是孤儿、没有信号通路**）");
        }
        Done(pass, fail);
    }

    static void Done(int pass, int fail)
    {
        Debug.Log($"AUD === 合计：{pass} 通过 / {fail} 失败 ===");
        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    // ==================================================================
    //  Play 模式验证：`SetFloat` 究竟生不生效
    //
    //  为什么单开一个：**编辑器（非 Play）里 `SetFloat` 是空操作** —— 实测
    //  「设 −20 → 立刻读 0.00 → 等 300ms 再读还是 0.00」，而音频子系统是活的
    //  （`outputSampleRate=48000 / Stereo`）。原因：**暴露参数只在 mixer 的「运行时实例」上可写**，
    //  edit 模式下 `Resources.Load` 拿到的是资产本身。
    //  ⇒ 「三根滑块到底有没有用」**只有 Play 模式 / 真包能回答**。
    //
    //  🔴 **2026-09-19 实测：这个入口在 `-batchmode` 下跑不起来** ——
    //     `EditorApplication.EnterPlaymode()` 之后进程**卡住、不退出、一行日志都没有**
    //     （等了 4 分多钟、CPU 仍在跑）。⇒ **只给 GUI 编辑器用**（下面那个 `[MenuItem]`）；
    //     批处理里会**直接报错退出**，不会静默挂死。
    //     真包那条路见 `资料/特效还原_进度与交接.md` §七 步 5（`PlayerBuild` + `-wfdrive`）。
    // ==================================================================
    static int _playPass, _playFail;
    static float _probeFrame;

    [MenuItem("Warpforge/音频/Play 模式验 SetFloat")]
    public static void PlayCheckMenuItem()
    {
        if (Application.isBatchMode)
        {
            Debug.LogError("AUD-P 本入口**在 batchmode 下跑不起来**（进入 Play 会卡住、进程不退出，实测过）。" +
                           "批处理里请用真包那条路：`PlayerBuild` + `-wfdrive`，" +
                           "见 `资料/特效还原_进度与交接.md` §七 步 5。");
            EditorApplication.Exit(2);
            return;
        }
        _playPass = _playFail = 0;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.EnterPlaymode();
    }

    /// <summary>批处理入口 —— **会直接拒绝**（见上面的实测结论），只为了让误用的人看到一句人话而不是挂死。</summary>
    public static void PlayCheck() { PlayCheckMenuItem(); }

    static void OnPlayModeChanged(PlayModeStateChange s)
    {
        if (s != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.update += PlayStep;
        _probeFrame = 0f;
    }

    static void PlayStep()
    {
        _probeFrame += 1f;
        if (_probeFrame < 10f) return;                 // 等几帧，让 mixer 实例化
        EditorApplication.update -= PlayStep;

        var mixer = Resources.Load<AudioMixer>(MixerResource);
        PChk(mixer != null, "Play 模式下 mixer 加载得到");
        if (mixer == null) { PDone(); return; }

        foreach (var g in Groups)
        {
            string p = "Volume" + g;
            float v0;
            if (!mixer.GetFloat(p, out v0)) { PChk(false, $"暴露参数 `{p}` 认得"); continue; }
            mixer.SetFloat(p, -20f);
            float v1;
            mixer.GetFloat(p, out v1);
            PChk(Mathf.Abs(v1 + 20f) < 0.05f, $"`{p}` 设 −20 → 读回 {v1:F2}（**这条过了 = 滑块真能改音量**）");
            mixer.SetFloat(p, 0f);
        }

        // 顺带把总线也过一遍
        CardPresentation.WarpforgeAudio.SetSoundFx(0.5f);
        PChk(Mathf.Abs(CardPresentation.WarpforgeAudio.SoundFx - 0.5f) < 0.001f, "总线 `SetSoundFx(0.5)` 记下了");
        var fx = WarpforgeVFX.WFSoundPlayer.FxGroup;
        PChk(fx != null && fx.name.StartsWith("FX"), $"特效音源挂到了 FX 组（{(fx == null ? "null" : fx.name)}）");
        CardPresentation.WarpforgeAudio.SetSoundFx(1f);
        PDone();
    }

    static void PChk(bool ok, string what)
    {
        if (ok) { _playPass++; Debug.Log("AUD-P   ✓ " + what); }
        else { _playFail++; Debug.LogError("AUD-P   ✗ " + what); }
    }

    static void PDone()
    {
        Debug.Log($"AUD-P === 合计：{_playPass} 通过 / {_playFail} 失败 ===");
        EditorApplication.Exit(_playFail == 0 ? 0 : 1);
    }

    static Type FindControllerType()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType("UnityEditor.Audio.AudioMixerController", false);
            if (t != null) return t;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  🆕 **2026-10-27（音频线 `B9` · 执行代理 `B2AU`）：6 个**快照**（A-02 的前置）
    //  背景：`Resources/Audio/Main Mixer.mixer` 现读 **只有 1 条默认快照**、也没有 `BattleGroup *`
    //        ⇒ `WarpforgeAudio.GetSnapshot()` **恒 null**（已实现且会出声）。本入口把「建快照」这步变成一键。
    //  名字**只此一份** = `Core/WarpforgeAudio.SnapshotNames`（⛔ 别在这里再抄一次）：
    //        `SFX On` / `SFX Off` / `Music Low` / `Music Medium` / `Music High` / `Music None`
    //  🔴 **数值判据没查清**（派活包 A-02 只给了「哪 4~6 个快照 + 3 个组」，**没给 dB 值**；
    //     原版 `SoundManager.ToggleSFX` 只做 `snapshot.TransitionTo(instant?0:2)`，值在 SO/资产里）。
    //     ⇒ 本入口**只建快照壳、不动任何组音量**（⛔ 不编数值）。数值要么由主对话在 Mixer 窗口手工调一次，
    //       要么等「原版 mixer 各快照的 dB」被解出来再补一版。
    //  ⚠️ **幂等**：已存在的同名快照**不重建、不改任何值**。
    //  入口：
    //    -executeMethod AudioSetup.VerifySnapshots   ← **只读**：现在有哪些快照、6 个名字各缺哪些（先看这条）
    //    -executeMethod AudioSetup.CreateSnapshots   ← 建缺的那些（已存在的跳过）
    // ══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>只读校验：打印 mixer 里**现在有哪些快照** + 逐个查 <see cref="WarpforgeAudio.SnapshotNames"/>。
    /// 不写盘、不改任何值（先跑这一条看现状）。</summary>
    public static void VerifySnapshots()
    {
        var mixer = Resources.Load<AudioMixer>(MixerResource);
        if (mixer == null) { Debug.LogError("AUD 快照校验：`Resources.Load<AudioMixer>` 取不到 `" + MixerResource + "`"); return; }

        var names = ListingSnapshotNames();
        Debug.Log($"AUD 快照校验：mixer 里现有 **{names.Count}** 条 —— [{string.Join(", ", names.ToArray())}]");

        int ok = 0;
        foreach (var want in WarpforgeAudio.SnapshotNames)
        {
            var s = mixer.FindSnapshot(want);
            if (s != null) { ok++; Debug.Log($"AUD   ✅ `{want}` 找得到"); }
            else Debug.Log($"AUD   ❌ `{want}` **缺**（跑 `-executeMethod AudioSetup.CreateSnapshots`）");
        }
        Debug.Log($"AUD 快照校验：**{ok}/{WarpforgeAudio.SnapshotNames.Length}** 命中"
                + (ok == WarpforgeAudio.SnapshotNames.Length ? " ⇒ A-02 的前置已就绪（数值另说）" : " ⇒ 还缺快照"));
    }

    /// <summary>建**缺的**快照（幂等；已存在的跳过、不改值）。用 `AudioMixerController` 的内部 API，
    /// 走与 <see cref="CreateMixer"/> 同一套反射；内部 API 改名 ⇒ **出声**并打印手工做法（⛔ 不静默）。</summary>
    public static void CreateSnapshots()
    {
        var path = AssetDatabase.GetAssetPath(Resources.Load<AudioMixer>(MixerResource));
        if (string.IsNullOrEmpty(path)) { Debug.LogError("AUD 建快照：先跑 `AudioSetup.CreateMixer`（mixer 不存在）"); return; }
        Type ctl = FindControllerType();
        if (ctl == null) { Debug.LogError("AUD 建快照：找不到 `UnityEditor.Audio.AudioMixerController`（内部 API 改名）"); return; }

        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
        if (mixer == null) { Debug.LogError("AUD 建快照：`LoadAssetAtPath<AudioMixer>` 失败：" + path); return; }

        // 找一个「加快照」的实例方法（不同 Unity 版本名字不同 —— 先按名字近似匹配，匹配不到就出声）
        MethodInfo add = null;
        foreach (var m in ctl.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (m.Name.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (m.Name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (m.GetParameters().Length != 0) continue;      // 无参那一版 = 「新建一条并返回它」
            add = m; break;
        }

        int made = 0, skipped = 0;
        foreach (var want in WarpforgeAudio.SnapshotNames)
        {
            if (mixer.FindSnapshot(want) != null) { skipped++; Debug.Log($"AUD   跳过（已存在）`{want}`"); continue; }
            if (add == null) continue;
            object snap = null;
            try
            {
                // AudioMixerController 挂在 mixer 资产上；先按 `AudioMixerController` 拿实例
                var ctrl = FindControllerInstance(path);
                if (ctrl != null) snap = add.Invoke(ctrl, null);
            }
            catch (Exception e) { Debug.LogWarning("AUD   建 `" + want + "` 抛异常：" + e.Message); }
            if (snap is UnityEngine.Object so)
            {
                so.name = want;
                EditorUtility.SetDirty(so);
                made++;
                Debug.Log($"AUD   建快照 `{want}` → 好（**值为默认/未设** —— 见本段红字：数值判据没查清）");
            }
            else Debug.LogWarning($"AUD   建 `{want}` **失败**（内部 API 形态不符）—— 见下面的手工做法");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"AUD 建快照结束：新建 {made} · 跳过 {skipped}");
        if (add == null || made < WarpforgeAudio.SnapshotNames.Length - skipped)
        {
            Debug.LogWarning("AUD **没全建成**。手工做法（1 分钟）：Window → Audio → Audio Mixer 打开 `Main Mixer`，"
                           + "在 Snapshots 列表点 `+` 六次，逐个改名成：" + string.Join(" / ", WarpforgeAudio.SnapshotNames)
                           + "；然后重跑 `-executeMethod AudioSetup.VerifySnapshots` 确认 6/6。");
        }
        VerifySnapshots();
    }

    /// <summary>列出 mixer 资产里**现有快照名**（只读：顺 `m_Snapshots` 的序列化名取）。取不到 ⇒ 空列表。</summary>
    static List<string> ListingSnapshotNames()
    {
        var list = new List<string>();
        var path = AssetDatabase.GetAssetPath(Resources.Load<AudioMixer>(MixerResource));
        if (string.IsNullOrEmpty(path)) return list;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is AudioMixerSnapshot s && !string.IsNullOrEmpty(s.name)) list.Add(s.name);
        }
        return list;
    }

    /// <summary>从 mixer 资产上取 `AudioMixerController` 实例（内部类型；取不到 ⇒ null）。</summary>
    static object FindControllerInstance(string path)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o != null && o.GetType().FullName == "UnityEditor.Audio.AudioMixerController") return o;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  🆕 **2026-10-28（`B89` · 执行代理 `E13`）：把原版 6 条快照的【真实数值】写进 mixer**
    //
    //  🔴 **要解决的问题**：快照壳建好了（6/6 命中），但 6 条全是空的（`m_FloatValues: {}`）
    //     ⇒ `WarpforgeAudio.ToggleSFX` 切了**等于没切**（`SoundManager.ToggleSFX` 账 `音频/语音:80`）。
    //
    //  ==== 判据（解包资源，逐字段亲读；⛔ 不是按名字猜的）====
    //  ① 原版有 **3 份** AudioMixer（不是 1 份）：
    //       · `Main Mixer`（pathID 4889565729752889476）组 = Master / FX / Music / Voices / Jingles
    //       · `Music`（−371249942676188183）组 = Master / General / BattleGroupLow / BattleGroupMedium / BattleGroupHigh
    //       · `FX`（−3989599372716181902）组 = Master / General
    //     出处：`d:/2/新解包资源/assets_full/bundle_audiocontrol_assets_all/AudioMixerController/*.json`
    //           ＋ `sharedassets0/AudioMixerController/{308,309,310}.json`（两份 `m_MixerConstant.snapshots` 逐值相同）
    //
    //  ② `SnapshotsType → 快照` 的映射**是从原版 SO 实读的**（⛔ 不是按快照名相似度猜的）：
    //     `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3594.json`（类 `AudioMixerAddresableReferences`）
    //     的 `snapshots[]`：
    //       customType 0 → 6764119139241457266  FX `General`
    //       customType 1 → 8068468039719941746  FX `NoVolume`
    //       customType 2 → −3926211066113174551 Music `BattleMusicLow`
    //       customType 3 → 774629946988549097  Music `BattleMusicMedium`
    //       customType 4 → −1533776937643420695 Music `BattleMusicHigh`
    //       customType 5 → 2527524072104055785 Music `NoBattleMusic`
    //     同一份 SO 的 `groups[]`（customType 0..6）= FX.General · Music.General · Main.Voices ·
    //     Main.Jingles · BattleGroupLow / Medium / High。
    //
    //  ③ 每条快照的 dB 值 = `AudioMixerController.m_MixerConstant.snapshots[i].values`；
    //     `values` 的下标由 `m_MixerConstant.groups[j].volumeIndex / pitchIndex` 定
    //     （Music 组序：Master 0/1 · General 2/3 · Low 4/5 · Medium 6/7 · High 8/9；
    //       FX 组序：Master 0/1 · General 2/3）⇒ 逐条拆出来是：
    //       FX `General`             : Master   0.0  · General 0.0 · pitch 全 1.0
    //       FX `NoVolume`            : Master −80.0  · General 0.0 · pitch 全 1.0
    //       Music `BattleMusicLow`   : Master 0 · General 0 · **Low 0.0**  · Medium −80 · High −80
    //       Music `BattleMusicMedium`: Master 0 · General 0 · Low −80 · **Medium 0.025578511878848076** · High −80
    //       Music `BattleMusicHigh`  : Master 0 · General 0 · Low −80 · Medium −80 · **High 0.0**
    //       Music `NoBattleMusic`    : Master 0 · General 0 · Low −80 · Medium −80 · High −80
    //
    //  ④ 过渡覆盖：Music 那 4 条 = `Attenuation`（`UnityEditor.Audio.ParameterTransitionType` 的第 6 号）
    //     打在**三个 BattleGroup 的音量参数**上（`transitionIndices: [4,6,8]`）；FX 那 2 条 =
    //     **一条覆盖都没有**（`transitionTypes: []`）。
    //
    //  ==== 🔶 自己设计（非原版）：`原版快照 → 我们哪个组` 这一层【投影】====
    //  原版快照落在**子 mixer** 上（FX 的 Master / Music 的 BattleGroup*），而我们是**一份混好的
    //  mixer**（Master + FX / Music / Voices / Jingles），既没有子 mixer、也没有 BattleGroup* ⇒
    //  只能投到 `FX` / `Music` 两个组：
    //    · `SFX *`   → `FX`    组 = FX 子树的有效增益（其 Master 与 General 都是 0 dB ⇒ 就取 Master 那档）
    //    · `Music *` → `Music` 组 = Master + General + **当前听得见的那一条 BattleGroup** 之和
    //  两条代价（如实记，⛔ 别当成等价）：
    //    ① `Music Low / Medium / High` 数值上几乎同值（差 ≤ 0.026 dB）—— 因为「放哪一条轨道」在我们
    //       这儿是 `Battle/BattleMusic.cs` 用**代码线性淡变**做的（那是最早的等价实现），不是 mixer
    //       切的；6 条里只有 `Music None`(−80 dB) 听得出差别。
    //    ② 原版快照落子 mixer ⇒ **不与音量滑块抢**；我们落主 mixer 的组 ⇒ 与本 mixer 的
    //       `m_ExposedParameters` 里 `VolumeFX` / `VolumeMusic` **是同一根浮点**（那两条 guid 就是
    //       该组的 `m_Volume`）⇒ **切快照会覆盖滑块设过的值**。根治 = 给 `FX` / `Music` 各加一个
    //       子组（如 `FX Content` / `Music Content`）并把音源改挂子组 —— 那是**运行时路由**改动，
    //       不在本入口权限内 ⇒ 已写进报告交调度台（`报告/B89_E13_mixer快照取值.md` §4）。
    //  ⚠️ 另记：本 mixer 的 `m_TargetSnapshot` = `Music None`（编辑器状态字段）；写值之后它带
    //     −80 dB 的 Music 值。运行时起始快照是 `m_StartSnapshot` = `SFX On`（不带 Music 值），
    //     所以不影响开局；但**在 Mixer 窗口里看到的当前快照**会是 `Music None`，别当成 bug。
    //
    //  幂等：`SetValueForVolume` 是**覆盖写字**，同一组值写两次结果相同。
    //  入口：`-executeMethod AudioSetup.SetSnapshotValues`（写完**逐条读回**打印，不一致就红）
    //        `-executeMethod AudioSetup.VerifySnapshotValues`（只读复核，不写盘）
    // ══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>一条「写进哪个组 / 写成多少 dB / 要不要 Attenuation 过渡覆盖」。
    /// **下标 = `SnapshotsType` 的值**（快照名只此一份 = <see cref="WarpforgeAudio.SnapshotNames"/>，⛔ 别在这里再抄）。</summary>
    struct SnapshotValueRow
    {
        public string Group; public float Db; public bool Attenuation;
        public SnapshotValueRow(string group, float db, bool attenuation)
        { Group = group; Db = db; Attenuation = attenuation; }
    }

    /// <summary>原版 6 条快照的组音量 dB（判据见上「🆕 2026-10-28（B89 · E13）」那一段的值表）。
    /// 顺序 = <see cref="SnapshotsType"/> 的枚举值顺序。</summary>
    static readonly SnapshotValueRow[] SnapshotValuesTable =
    {
        new SnapshotValueRow("FX",     0f,           false),   // FxNormal            ← FX `General`            : Master   0.0 dB
        new SnapshotValueRow("FX",    -80f,          false),   // FxNoVolume          ← FX `NoVolume`           : Master −80.0 dB
        new SnapshotValueRow("Music",  0f,           true),    // MusicBattleMusicLow    ← Music `BattleMusicLow`   : Low 0.0 dB
        new SnapshotValueRow("Music",  0.025578512f, true),    // MusicBattleMusicMedium ← Music `BattleMusicMedium`: Medium 0.025578511878848076 dB
        new SnapshotValueRow("Music",  0f,           true),    // MusicBattleMusicHigh   ← Music `BattleMusicHigh`  : High 0.0 dB
        new SnapshotValueRow("Music", -80f,          true),    // MusicBattleMusicNoMusic← Music `NoBattleMusic`   : 三条全 −80 dB
    };

    /// <summary>原版那 6 条快照的**原名**（⚠️ 只用于日志/报告里标注「这条数是从哪儿抄的」；
    /// ⛔ 不参与查找 —— mixer 里存的是 `WarpforgeAudio.SnapshotNames` 那一组名字）。</summary>
    static readonly string[] OriginalSnapshotLabels =
    {
        "FX/General", "FX/NoVolume",
        "Music/BattleMusicLow", "Music/BattleMusicMedium", "Music/BattleMusicHigh", "Music/NoBattleMusic",
    };

    /// <summary>上一次 <see cref="VerifySnapshotValuesReport"/> 里「与原版不一致 / 取不到」的条数（0 = 全对）。</summary>
    public static int SnapshotValueFailures { get; private set; }

    /// <summary>把原版 6 条快照的组音量写进 mixer（幂等；写完逐条读回，读不回期望值就出声）。</summary>
    [MenuItem("Warpforge/音频/写快照数值")]
    public static void SetSnapshotValues()
    {
        object ctl; Type ctlType; System.Collections.IList groups; Array snaps;
        MethodInfo setVolume, getVolume;
        if (!TryOpenMixerForSnapshotValues(out ctl, out ctlType, out groups, out snaps,
                                           out setVolume, out getVolume))
        { DoneSv(0, SnapshotValuesTable.Length); return; }

        Type groupType = null, snapType = null;
        foreach (var g in groups) if (g != null) { groupType = g.GetType(); break; }
        foreach (var s in snaps) if (s != null) { snapType = s.GetType(); break; }

        // 过渡覆盖那半边（原版 Music 的 4 条 = Attenuation）。拿不到 ⇒ 只少一条「整复刻」，⛔ 不挡数值写入。
        MethodInfo setTr = snapType.GetMethod("SetTransitionTypeOverride", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo clrTr = snapType.GetMethod("ClearTransitionTypeOverride", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo getGuid = groupType.GetMethod("GetGUIDForVolume", BindingFlags.Public | BindingFlags.Instance);
        object trAttenuation = null;
        if (setTr != null)
        {
            var ps = setTr.GetParameters();
            if (ps.Length == 2 && ps[1].ParameterType.IsEnum) trAttenuation = Enum.ToObject(ps[1].ParameterType, 6);
        }
        if (setTr == null || getGuid == null || trAttenuation == null)
        {
            Debug.LogWarning("AUD-SV 拿不到 `SetTransitionTypeOverride` / `GetGUIDForVolume` / 枚举值 6 ⇒ "
                           + "**过渡覆盖那半边这次不写**（原版 Music 的 4 条是 `Attenuation`）；**数值照写**。");
            setTr = clrTr = null;
        }

        int ok = 0, fail = 0;
        for (int i = 0; i < SnapshotValuesTable.Length; i++)
        {
            string snapName = WarpforgeAudio.SnapshotNames[i];
            var row = SnapshotValuesTable[i];
            object snap = FindObjectByName(snaps, snapName);
            object group = FindObjectByName(groups, row.Group);
            if (snap == null || group == null)
            {
                fail++;
                Debug.LogError($"AUD-SV [{i}] `{snapName}` → 组 `{row.Group}`："
                             + (snap == null ? "**快照不存在**（先跑 `-executeMethod AudioSetup.CreateSnapshots`）" : "")
                             + (group == null ? $"**组 `{row.Group}` 不存在**（先跑 `AudioSetup.CreateMixer` + `工具/setup_audio_mixer.py`）" : ""));
                continue;
            }

            setVolume.Invoke(group, new object[] { ctl, snap, row.Db });
            float got = ReadVolumeForSnapshot(getVolume, group, ctl, snap);
            if (Mathf.Abs(got - row.Db) < 0.001f) ok++;
            else
            {
                fail++;
                Debug.LogError($"AUD-SV [{i}] `{snapName}` / `{row.Group}` **写不进**：期望 {row.Db:F3} dB · 读回 {got:F3} dB");
            }

            string tr = "（本入口没写）";
            if (setTr != null)
            {
                try
                {
                    object guid = getGuid.Invoke(group, null);
                    if (row.Attenuation) { setTr.Invoke(snap, new[] { guid, trAttenuation }); tr = "Attenuation"; }
                    else { clrTr.Invoke(snap, new[] { guid }); tr = "无（原版这两条也是空的）"; }
                }
                catch (Exception e) { tr = "**写失败**：" + e.Message; Debug.LogWarning($"AUD-SV [{i}] `{snapName}` 过渡覆盖：{e.Message}"); }
            }
            Debug.Log($"AUD-SV [{i}] `{snapName}` → 组 `{row.Group}` = {got:F3} dB · 过渡覆盖 {tr}"
                    + $" · 原版快照 `{OriginalSnapshotLabels[i]}`");
        }

        // 内部 API 是 native 写：SetDirty + SaveAssets 才真落盘（判据 → 本仓「SetDirty 之后回读抓不到没落盘」那条坑）
        if (ctl is UnityEngine.Object ctlObj) EditorUtility.SetDirty(ctlObj);
        foreach (var s in snaps) if (s is UnityEngine.Object so) EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"AUD-SV === 合计：{ok} 条写入 / {fail} 条失败 ===（复核：`-executeMethod AudioSetup.VerifySnapshotValues`）");
        DoneSv(ok, fail);
    }

    /// <summary>只读复核入口：逐条比对 6 条快照的组音量与原版 dB（不写盘）。</summary>
    public static void VerifySnapshotValues()
    {
        string rep = VerifySnapshotValuesReport();
        Debug.Log(rep);
        DoneSv(SnapshotValuesTable.Length - SnapshotValueFailures, SnapshotValueFailures);
    }

    /// <summary>只读复核（返回可打印的报告；顺便更新 <see cref="SnapshotValueFailures"/>）。
    /// 供 <c>Editor/AudioSnapshotCheck.cs</c> 复用（那里只加断言、不重复一份反射）。</summary>
    public static string VerifySnapshotValuesReport()
    {
        SnapshotValueFailures = 0;
        var sb = new System.Text.StringBuilder();
        sb.Append("AUD-SV-V 6 条快照的组音量（对照原版解包值）\n");

        object ctl; Type ctlType; System.Collections.IList groups; Array snaps;
        MethodInfo setVolume, getVolume;
        if (!TryOpenMixerForSnapshotValues(out ctl, out ctlType, out groups, out snaps,
                                           out setVolume, out getVolume))
        {
            SnapshotValueFailures = SnapshotValuesTable.Length;
            sb.Append("  ✗ 打开 mixer / 取内部 API 失败（上面已有 LogError）\n");
            return sb.ToString();
        }

        for (int i = 0; i < SnapshotValuesTable.Length; i++)
        {
            string snapName = WarpforgeAudio.SnapshotNames[i];
            var row = SnapshotValuesTable[i];
            object snap = FindObjectByName(snaps, snapName);
            object group = FindObjectByName(groups, row.Group);
            if (snap == null || group == null)
            {
                SnapshotValueFailures++;
                sb.Append($"  ✗ [{i}] `{snapName}` / `{row.Group}`：")
                  .Append(snap == null ? "快照不存在（跑 `AudioSetup.CreateSnapshots`）" : "组不存在（跑 `AudioSetup.CreateMixer`）")
                  .Append('\n');
                continue;
            }
            float got = ReadVolumeForSnapshot(getVolume, group, ctl, snap);
            bool ok = Mathf.Abs(got - row.Db) < 0.001f;
            if (!ok) SnapshotValueFailures++;
            sb.Append($"  {(ok ? "✓" : "✗")} [{i}] `{snapName}` / `{row.Group}`：期望 {row.Db:F3} dB · 实得 {got:F3} dB"
                    + $"（原版快照 `{OriginalSnapshotLabels[i]}`）\n");
        }
        return sb.ToString();
    }

    /// <summary>反射取齐写快照要的 5 样：controller 实例 / 它的类型 / 组集合 / 快照集合 / 两个音量读写方法。
    /// 失败 ⇒ **出声**并返回 false（⛔ 不静默）。
    /// <para>实测签名（`ilspycmd` 反编译 Unity 6000.3.23f1 的 `UnityEditor.CoreModule.dll`）：
    /// <c>AudioMixerGroupController.SetValueForVolume(AudioMixerController, AudioMixerSnapshotController, float)</c> ·
    /// <c>...GetValueForVolume(AudioMixerController, AudioMixerSnapshotController) → float</c> ·
    /// <c>AudioMixerController.GetAllAudioGroupsSlow() → List&lt;AudioMixerGroupController&gt;</c> ·
    /// <c>AudioMixerController.snapshots → AudioMixerSnapshotController[]</c>。</para></summary>
    static bool TryOpenMixerForSnapshotValues(out object ctl, out Type ctlType,
                                              out System.Collections.IList groups, out Array snaps,
                                              out MethodInfo setVolume, out MethodInfo getVolume)
    {
        ctl = null; ctlType = null; groups = null; snaps = null; setVolume = null; getVolume = null;

        if (SnapshotValuesTable.Length != WarpforgeAudio.SnapshotNames.Length
            || OriginalSnapshotLabels.Length != SnapshotValuesTable.Length)
        {
            Debug.LogError($"AUD-SV **前提不成立**：值表 {SnapshotValuesTable.Length} 条 / 快照名 "
                         + $"{WarpforgeAudio.SnapshotNames.Length} 条 / 原版标签 {OriginalSnapshotLabels.Length} 条 —— "
                         + "三者必须一样长（下标 = `SnapshotsType` 的值），否则会静默错档");
            return false;
        }

        var path = AssetDatabase.GetAssetPath(Resources.Load<AudioMixer>(MixerResource));
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("AUD-SV 找不到 mixer 资产 —— 先跑 `-executeMethod AudioSetup.CreateMixer`（再跑 `工具/setup_audio_mixer.py`）");
            return false;
        }
        ctlType = FindControllerType();
        ctl = FindControllerInstance(path);
        if (ctlType == null || ctl == null)
        {
            Debug.LogError("AUD-SV 取不到 `UnityEditor.Audio.AudioMixerController` 实例（内部 API 改名？见本文件头三步）");
            return false;
        }

        groups = ReadMember(ctlType, ctl, "GetAllAudioGroupsSlow") as System.Collections.IList;
        snaps  = ReadMember(ctlType, ctl, "snapshots") as Array;
        if (groups == null || snaps == null)
        {
            Debug.LogError("AUD-SV 取不到 `AudioMixerController.GetAllAudioGroupsSlow()` 或 `.snapshots`"
                         + "（本 Unity 版本的内部 API 形态变了）");
            return false;
        }

        Type groupType = null, snapType = null;
        foreach (var g in groups) if (g != null) { groupType = g.GetType(); break; }
        foreach (var s in snaps) if (s != null) { snapType = s.GetType(); break; }
        if (groupType == null || snapType == null)
        {
            Debug.LogError("AUD-SV mixer 里没有组或没有快照（先跑 `AudioSetup.CreateMixer` / `AudioSetup.CreateSnapshots`）");
            return false;
        }

        // ⚠️ 形参类型用 `ctl.GetType()`（**实例的真类型**）而不是 `FindControllerType()` 找回来的那个 ——
        //    两者正常是同一个 `Type`，但若哪天 AppDomain 里出现两份同名程序集，用实例类型才是稳的。
        Type ctlActual = ctl.GetType();
        setVolume = groupType.GetMethod("SetValueForVolume", BindingFlags.Public | BindingFlags.Instance, null,
                                        new[] { ctlActual, snapType, typeof(float) }, null);
        getVolume = groupType.GetMethod("GetValueForVolume", BindingFlags.Public | BindingFlags.Instance, null,
                                        new[] { ctlActual, snapType }, null);
        if (setVolume == null || getVolume == null)
        {
            Debug.LogError("AUD-SV `AudioMixerGroupController.SetValueForVolume(AudioMixerController, "
                         + "AudioMixerSnapshotController, float)` 不存在 —— 内部 API 变了。"
                         + "⛔ 别手改 `.mixer`（`m_FloatValues` 的键是参数 GUID，手改必错）");
            return false;
        }
        return true;
    }

    /// <summary>按名字在**组集合 / 快照集合**里找一个（元素都是 `UnityEngine.Object`）。取不到 ⇒ null。</summary>
    static object FindObjectByName(System.Collections.IEnumerable items, string name)
    {
        if (items == null || string.IsNullOrEmpty(name)) return null;
        foreach (var it in items)
        {
            if (it is UnityEngine.Object o && o.name == name) return it;
        }
        return null;
    }

    /// <summary>读某组在某快照里的音量（dB）。读不到 ⇒ `NaN`（⛔ 别拿 0 顶替 —— 0 dB 是合法值）。</summary>
    static float ReadVolumeForSnapshot(MethodInfo getVolume, object group, object ctl, object snap)
    {
        try
        {
            var r = getVolume.Invoke(group, new object[] { ctl, snap });
            if (r is float f) return f;
            if (r is double d) return (float)d;
        }
        catch (Exception e) { Debug.LogWarning("AUD-SV 读回组音量抛异常：" + e.Message); }
        return float.NaN;
    }

    /// <summary>先按属性取、再按无参方法取（`GetAllAudioGroupsSlow` 是方法、`snapshots` 是属性）。</summary>
    static object ReadMember(Type t, object target, string name)
    {
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.GetIndexParameters().Length == 0)
        {
            try { return p.GetValue(target); } catch { }
        }
        var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null, Type.EmptyTypes, null);
        if (m != null)
        {
            try { return m.Invoke(target, null); } catch { }
        }
        return null;
    }

    /// <summary>`SetSnapshotValues` / `VerifySnapshotValues` 的收尾：打印合计，批处理里**按失败数退出**。
    /// （与 <see cref="Done"/> 同形，只是前缀用 `AUD-SV`，免得两条入口的合计行混在一起。）</summary>
    static void DoneSv(int pass, int fail) { DoneSv("AUD-SV", pass, fail); }

    /// <summary>带自定义前缀的那一版（`SetMixerDefaults` 用 `AUD-SVD`，免得两种入口的合计行混在一起）。</summary>
    static void DoneSv(string tag, int pass, int fail)
    {
        Debug.Log($"{tag} === 合计：{pass} 通过 / {fail} 失败 ===");
        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  🆕 **2026-10-28（`B89` · 执行代理 `E13b`）：补上原版 `Main Mixer` 的组默认音量**
    //
    //  🔴 **判据**：原版 `Main Mixer`（pathID 4889565729752889476）**只有一条**快照 `Snapshot`，
    //     它的 `values` 头两项是 `[-0.16685187816619873, 1.0]` ⇒ `Master` 组音量 =
    //     **−0.16685188 dB**（pitch 1.0）；`FX` / `Music` / `Voices` / `Jingles` 全是 0.0 dB。
    //     出处：`assets_full/bundle_audiocontrol_assets_all/AudioMixerController/AudioMixerController_4889565729752889476.json`
    //          ＋ `sharedassets0/AudioMixerController/AudioMixerController_309.json`（逐值相同）。
    //     我们现在 6 条快照里 `Master` 都是默认 0 dB ⇒ 整体响度比原版高 0.17 dB。
    //
    //  ==== 🔴 单位是 **dB、不是线性 0–1**（派活简报说的「要 10^(dB/20)」不成立；三条反证）====
    //   ① 原版 `m_MixerConstant.snapshots[i].values` 里**同一个数组**既装音量又装 pitch：
    //      音量是 `0.0` / `−80.0`（**只能是 dB** —— 线性不可能出现负数），pitch 是 `1.0`（线性）。
    //   ② 编辑器侧 `AudioMixerGroupController`（Unity 6000.3.23f1）**没有 `volume` 属性**：
    //      `ilspycmd` 反编译 `UnityEditor.CoreModule.dll` 得到的成员只有
    //      `GetGUIDForVolume()` / `GetValueForVolume(controller, snapshot)` /
    //      `SetValueForVolume(controller, snapshot, float)` ⇒ 它读写的就是上面那个 `values` 的格子
    //      ⇒ **与快照同一个单位 = dB**。
    //   ③ 「线性 0–1」那条口径属于 **运行时** `UnityEngine.Audio.AudioMixerGroup.volume`，
    //      与编辑器侧的 `SetValueForVolume` 不是同一个 API。
    //   ⇒ 本入口**直接写 dB**（`-0.16685188f`）。若按线性写 `10^(−0.16685/20) = 0.9810`，
    //      落进资产会被当 **+0.98 dB** 读出来（方向相反、还差 1.15 dB）⇒ 下面那条读回自检会立刻报红。
    //  ⚠️ **不与滑块抢**：`Master` 组在本 mixer 里**没有被暴露**（`m_ExposedParameters` 只有
    //     `VolumeFX` / `VolumeMusic` / `VolumeVoices` / `VolumeJingles`），运行时的
    //     `WarpforgeAudio.ApplyAll` 也不写它 ⇒ 这一步不会覆盖任何滑块设过的值。
    //
    //  ==== 🔶 自己设计（非原版）：写进【每一条】快照，而不是「组默认值」 ====
    //  原版那个值在 `Main Mixer` 的**唯一一条**快照里（Music/FX 两个子 mixer 的 6 条快照根本不碰
    //  Main Mixer 的 Master）；我们的 mixer 有 6 条快照、且**没有「组默认值」这一格**
    //  （见反证②：组音量只按快照存）⇒ 只能往 6 条里各写一份，效果等价（切到哪条 `Master` 都一样）
    //  · 调度台 2026-10-28 转岗（派活包 §2）。
    //
    //  幂等：覆盖写字，同值重写结果不变。
    //  入口：`-executeMethod AudioSetup.SetMixerDefaults`（写完**逐条读回**）
    //        `-executeMethod AudioSetup.VerifyMixerDefaults`（只读复核，不写盘）
    // ══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>原版 `Main Mixer` 快照里**非 0** 的组音量（其余组原版就是 0 dB，不用写）。
    /// **单位 = dB**（反证见上面那段「🔴 单位是 dB」）。</summary>
    static readonly string[] MixerDefaultGroups = { "Master" };

    /// <summary>与 <see cref="MixerDefaultGroups"/> 一一对应的 dB 值。原版精确值 `-0.16685187816619873`。</summary>
    static readonly float[] MixerDefaultDb = { -0.16685188f };

    /// <summary>上一次 <see cref="VerifyMixerDefaultsReport"/> 里「与原版不一致 / 取不到」的格数（0 = 全对）。</summary>
    public static int MixerDefaultFailures { get; private set; }

    /// <summary>把原版 `Main Mixer` 起始快照的组音量（`Master = −0.16685188 dB`）写进**每一条**快照。
    /// 幂等；写完逐格读回，读不回期望值就出声。</summary>
    [MenuItem("Warpforge/音频/写 Master 默认音量")]
    public static void SetMixerDefaults()
    {
        object ctl; Type ctlType; System.Collections.IList groups; Array snaps;
        MethodInfo setVolume, getVolume;
        if (!TryOpenMixerForSnapshotValues(out ctl, out ctlType, out groups, out snaps,
                                           out setVolume, out getVolume))
        { DoneSv("AUD-SVD", 0, MixerDefaultGroups.Length); return; }

        int ok = 0, fail = 0, cells = 0;
        for (int gi = 0; gi < MixerDefaultGroups.Length; gi++)
        {
            string groupName = MixerDefaultGroups[gi];
            float db = MixerDefaultDb[gi];
            object group = FindObjectByName(groups, groupName);
            if (group == null)
            {
                fail++;
                Debug.LogError($"AUD-SVD 组 `{groupName}` 不存在（先跑 `AudioSetup.CreateMixer` + `工具/setup_audio_mixer.py`）");
                continue;
            }
            foreach (var s in snaps)
            {
                var so = s as UnityEngine.Object;
                if (so == null || string.IsNullOrEmpty(so.name)) continue;
                cells++;
                setVolume.Invoke(group, new object[] { ctl, s, db });
                float got = ReadVolumeForSnapshot(getVolume, group, ctl, s);
                bool good = Mathf.Abs(got - db) < 0.001f;
                if (good) ok++;
                else
                {
                    fail++;
                    Debug.LogError($"AUD-SVD 快照 `{so.name}` / 组 `{groupName}` **写不进**："
                                 + $"期望 {db:F5} dB · 读回 {got:F5} dB");
                }
                Debug.Log($"AUD-SVD 快照 `{so.name}` / 组 `{groupName}` = {got:F5} dB（原版 −0.16685188 dB）");
            }
        }

        if (ctl is UnityEngine.Object ctlObj) EditorUtility.SetDirty(ctlObj);
        foreach (var s in snaps) if (s is UnityEngine.Object so) EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"AUD-SVD **{snaps.Length} 条快照 × {MixerDefaultGroups.Length} 个组 = {cells} 格**"
                + "（复核：`-executeMethod AudioSetup.VerifyMixerDefaults`）");
        DoneSv("AUD-SVD", ok, fail);
    }

    /// <summary>只读复核入口：逐格比对 `Master` 组在每条快照里的音量与原版 dB（不写盘）。</summary>
    public static void VerifyMixerDefaults()
    {
        string rep = VerifyMixerDefaultsReport();
        Debug.Log(rep);
        DoneSv("AUD-SVD", MixerDefaultChecks - MixerDefaultFailures, MixerDefaultFailures);
    }

    /// <summary>上一次 <see cref="VerifyMixerDefaultsReport"/> 实际检查过的格数（`snapshots × groups`）。</summary>
    public static int MixerDefaultChecks { get; private set; }

    /// <summary>只读复核（返回可打印的报告；顺便更新 <see cref="MixerDefaultFailures"/> /
    /// <see cref="MixerDefaultChecks"/>）。供 `Editor/AudioSnapshotCheck.cs` 复用。</summary>
    public static string VerifyMixerDefaultsReport()
    {
        MixerDefaultFailures = 0;
        MixerDefaultChecks = 0;
        var sb = new System.Text.StringBuilder();
        sb.Append("AUD-SVD-V 组默认音量（对照原版 Main Mixer 起始快照）\n");

        object ctl; Type ctlType; System.Collections.IList groups; Array snaps;
        MethodInfo setVolume, getVolume;
        if (!TryOpenMixerForSnapshotValues(out ctl, out ctlType, out groups, out snaps,
                                           out setVolume, out getVolume))
        {
            MixerDefaultFailures = 1;
            sb.Append("  ✗ 打开 mixer / 取内部 API 失败（上面已有 LogError）\n");
            return sb.ToString();
        }

        for (int gi = 0; gi < MixerDefaultGroups.Length; gi++)
        {
            string groupName = MixerDefaultGroups[gi];
            float db = MixerDefaultDb[gi];
            object group = FindObjectByName(groups, groupName);
            if (group == null)
            {
                MixerDefaultFailures++;
                sb.Append($"  ✗ 组 `{groupName}` 不存在（跑 `AudioSetup.CreateMixer`）\n");
                continue;
            }
            foreach (var s in snaps)
            {
                var so = s as UnityEngine.Object;
                if (so == null || string.IsNullOrEmpty(so.name)) continue;
                MixerDefaultChecks++;
                float got = ReadVolumeForSnapshot(getVolume, group, ctl, s);
                bool ok = Mathf.Abs(got - db) < 0.001f;
                if (!ok) MixerDefaultFailures++;
                sb.Append($"  {(ok ? "✓" : "✗")} 快照 `{so.name}` / 组 `{groupName}`：期望 {db:F5} dB · 实得 {got:F5} dB\n");
            }
        }
        return sb.ToString();
    }
}
