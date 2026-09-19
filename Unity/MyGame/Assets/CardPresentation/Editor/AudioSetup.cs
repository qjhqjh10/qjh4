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
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class AudioSetup
{
    public const string MixerAssetPath = "Assets/CardPresentation/Resources/Audio/Main Mixer.mixer";
    /// <summary>运行时按 `Resources.Load<AudioMixer>(MixerResource)` 取。</summary>
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
}
