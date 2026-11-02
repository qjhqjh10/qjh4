// B120_W10_Assert.cs — 执行写手 `B120-W10`（2026-11-02）· `资料/完全复刻/报告/B120_Q_未决判据.md` 的 Q6 / Q7 / Q9②
//
// ============================ 它盯的是什么 ============================
//  Q6   多张部署间隔：`EventTiming.DeploySummonHold` **1.0 → 0.45**（= `_ResolveSummonUnit` 那条协程的整条寿命
//       0.1 + 0.05 + 0.2 + 0.1；原版**没有**「部署间隔」字段、动作泵无额外等待）
//  Q7   `Card3DAnimationController.Update` 的到期判据：把原式 `OneFrameSlack / targetFrameRate + remaining < 0`
//       抽成 `FrameSlackExpired(fps, remaining)`（原版照抄、⛔ 不加钳制）。**行为仍按 60 fps 算** ——
//       照抄原版默认档（`targetFrameRate = -1`）会让 82.6% 的卡体动画当帧收尾 ⇒ 已停手交调度台裁（反证见报告 §Q7）。
//  Q9② `BattleCampaignScriptHost.PlaySoundAsset` 改成原版形状：**只入队** + 每帧出队一条
//       + `SoundFxVolume > 0` 闸 + `PlayOneShot(clip, SoundFxVolume × volume)` + **无混音组（Master）**
//
// ============================ 🔴 灭自证（哪一处改回去会让哪一条红）============================
//  🧨 Q6：把 `DeploySummonHold` 改回 `1.0f` ⇒ A2/A3/A6/A7/A8 红（0.45 那两档、兜底两档、与「四段之和」全对不上）。
//  🧨 Q6：把四段之一改掉（0.1/0.05/0.2/0.1）而不同步常数 ⇒ A7 红（那是**算出来**的恒等式，不是抄常数）。
//  🧨 Q7：把 `FrameSlackExpired` 改成带钳制（`fps <= 0 ⇒ 60`）⇒ B2 红（`fps = -1, remaining = 0.5` 必须**到期**）。
//          把它写成 `>= 0` 反向 ⇒ B1/B3 全红。
//  🧨 Q9②：把 `PlaySoundAsset` 改回「立刻 `PlayOneShot`」⇒ C1 红（入队后 `LastSoundQueueVolume` 仍是 -1 才算没播）。
//          把音量改回 `AudioListener.volume` ⇒ C3 红（会记到 0.25 而不是 0.2）；把闸删掉 ⇒ C2 红；
//          把混音组挂回 `VoicesGroup` ⇒ C4 红；把出队改成「一次抽干」⇒ C5 红（第二条必须还在队里）。
//
// ============================ 覆盖边界（如实记）============================
//  · **Q7 的 82.6% 是数据侧证据**（`Resources/cardanim_clips.json` 现读 881/1067 条 ≤ 1.0 s）——
//    它证明「照抄 -1 档会怎样」，**不证明**「我们该不该照抄」；后者待调度台裁，⛔ 本件不替它下结论。
//  · **Q9② 没有出声验证**（批处理里 `AudioSource.PlayOneShot` 不出声也返回）：只验**队列/闸/音量实参/路由**。
//  · **Q9② 的泵今天无人调用**（战役脚本数据是服务端 LiveOps 资产、本地 0 条 ⇒ 整条链休眠）——
//    本件的 C 组是**手动**调 `PlayNextSoundInQueue()`，⛔ 不代表宿主已经把它接进 `AdvanceTimeline`。
//  · **Q6 只验时间常量**：`DeployHoldOf` 的消费点在 `BattleDriver` 的时间线上（不在本代理白名单）。
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B120_W10_Assert
{
    const string Prefix = "[B120W10] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    static void Near(float got, float want, float tol, string what)
    {
        Check(Mathf.Abs(got - want) <= tol, what + $"（实得 {got:F5} / 期望 {want:F5} ±{tol:F5}）");
    }

    /// <summary>宿主入口。`driver` 可以为 null（A 组不需要），C 组需要它来挂 `AudioSource`。</summary>
    public static void Run(BattleDriver driver)
    {
        Debug.Log(Prefix + "=== B120-Q 未决判据 · W10 三条（Q6 部署间隔 / Q7 帧率裕量 / Q9② 战役音效）===");
        RunDeploySummonHold();
        RunFrameSlack();
        RunCampaignSoundAsset(driver);
        Debug.Log(Prefix + $"B120W10 合计：{_ok} 通过 / {_bad} 失败");
    }

    // ==================================================================
    //  A · Q6：效果召唤 / 复活那一档 `Deploy` 占 0.45 s
    // ==================================================================
    static void RunDeploySummonHold()
    {
        Debug.Log(Prefix + "--- A · Q6 `DeploySummonHold`（`_ResolveSummonUnit` 整条协程）---");

        Near(EventTiming.DeploySummonHold, 0.45f, 1e-5f,
             "A1 ★ 效果召唤/复活档 = **0.45 s**（改动前 = 1.0，出处是**非阻塞**的 `Summon Troop Tween`）");

        Near(EventTiming.DeployHoldOf(DeploySource.Summon), 0.45f, 1e-5f,
             "A2 ★ `DeployHoldOf(Summon)` = 0.45");
        Near(EventTiming.DeployHoldOf(DeploySource.Reanimate), 0.45f, 1e-5f,
             "A3 ★ `DeployHoldOf(Reanimate)` = 0.45（与 Summon 共用本常量）");

        // 其余三档**不许被这次改动带走**（它们各有各的出处）
        // ⚠️ 手牌/自动打出这两档走 `SkipScaled`：**回放「跳过动画」档会把攻击前摇 ÷3**
        //    （`EventTiming.ReplaySkipAnims` = 原版 `matchType==0xA0 ∧ +0x521`）⇒ 那时本段**不适用**，
        //    出声跳过（⛔ 不静默跳过、也⛔ 不按原值硬断）。
        bool skipAnims = EventTiming.ReplaySkipAnims != null && EventTiming.ReplaySkipAnims();
        if (skipAnims)
        {
            Debug.Log(Prefix + "A4/A5 **跳过**：本局处于「回放跳动画」档（`ReplaySkipAnims` 为真 ⇒ `SkipScaled` ÷3）");
        }
        else
        {
            Near(EventTiming.DeployHoldOf(DeploySource.HandPlayed), 0.4f, 1e-5f,
                 "A4 ✋ 手牌打出档仍是 **0.4**（`attackStepTime` 0.1 + `_ResolvePlayCardFromHand` 的 0.3）");
            Near(EventTiming.DeployHoldOf(DeploySource.AutoPlayFromHand), 0.1f, 1e-5f,
                 "A5 ✋ 自动从手牌打出档仍是 **0.1**（只有 `_MinionPlayedIntoField` 那一段）");
        }
        Near(EventTiming.DeployHoldOf(DeploySource.Unknown), 0.75f, 1e-5f,
             "A6 ✋ 没写来源的兜底 = 0.45 + 0.3 = **0.75**（原口径 1.3 随本常量一起变；如实记在 `DeployHoldOf` 的 doc）");

        Near(EventTiming.DurationOf(EvtKind.Deploy), 0.75f, 1e-5f,
             "A7 ✋ 只按 `EvtKind` 的重载 = 兜底 **0.75**（= `DeploySummonHold + DeployLandHold`）");

        // ★ 灭自证：0.45 不是凭空写的数，是**四段相加**（判据逐条在 `DeploySummonHold` 的 doc 里）
        float four = 0.1f + 0.05f + 0.2f + 0.1f;   // timeToScaleIntoMoving + timeBeforeLand + timeToLand + AttackStepTime
        Near(EventTiming.DeploySummonHold, four, 1e-5f,
             "A8 ★★ **灭自证**：0.45 = `timeToScaleIntoMoving(0.1) + timeBeforeLand(0.05) + timeToLand(0.2) + AttackStepTime(0.1)`"
           + "｜🧨 只改常数不改分段（或反过来）⇒ 本行红");
    }

    // ==================================================================
    //  B · Q7：`Update` 的到期判据（原式照抄，⛔ 不加钳制）
    // ==================================================================
    static void RunFrameSlack()
    {
        Debug.Log(Prefix + "--- B · Q7 `FrameSlackExpired`（原版 `1.0 / targetFrameRate + remaining < 0`）---");

        // B1/B3：正常帧率档 —— 到期线是 `remaining < −1/fps`（60 fps ⇒ −0.0167；30 fps ⇒ −0.0333）
        Check(!Card3DAnimationController.FrameSlackExpired(60, 0.02f),
              "B1 60 fps · 还剩 0.02 s ⇒ **继续演**（60 fps 的裕量 = 1/60 ≈ 0.0167）");
        Check(Card3DAnimationController.FrameSlackExpired(60, -0.05f),
              "B2 60 fps · 剩 −0.05 s（越过 −1/60 那道线）⇒ **收尾**");
        Check(!Card3DAnimationController.FrameSlackExpired(30, 0.5f) &&
               Card3DAnimationController.FrameSlackExpired(30, -0.05f),
              "B3 30 fps 两档同形（裕量 = 1/30 ≈ 0.0333 ⇒ 到期线 = −0.0333）");

        // ★ 灭自证：原版默认档 `targetFrameRate = -1` 的**真实语义**必须被这条式子忠实表达
        Check(Card3DAnimationController.FrameSlackExpired(-1, 0.5f),
              "B4 ★★ 原版默认档 `targetFrameRate = -1` ⇒ `1/(-1) + 0.5 = -0.5 < 0` ⇒ **剩 0.5 s 也当帧收尾**"
            + "（这就是「怪值」本身；🧨 给它加 `fps <= 0 ⇒ 60` 的钳制 ⇒ 本行红）");
        Check(!Card3DAnimationController.FrameSlackExpired(-1, 1.5f),
              "B5 ✋ `-1` 档 · 还剩 1.5 s ⇒ 仍继续演（只有 < 1 s 才收尾 —— 语义是「砍掉最后 1 秒」，不是「永远收尾」）");
        Check(!Card3DAnimationController.FrameSlackExpired(0, 0.001f),
              "B6 ✋ `fps = 0` ⇒ `1.0f/0f = +∞` ⇒ **永不到期**（原版同；⛔ 别加特判）");

        // B7：真控制器跑一遍（行为**没变**：调用点仍把 fps 钳成 60）
        var go = new GameObject("B120W10_Card3D");
        try
        {
            var ctl = new Card3DAnimationController(go.transform);
            int done = 0;
            bool started = ctl.DoAnim(new CardAnimPlayback { Prefab = "Probe", Duration = 0.2f, Label = "b120w10" },
                                      false, () => done++);
            Check(started && ctl.PlayedCount == 1, "（前提）0.2 s 的拍起来播了");
            ctl.Update(0.25f);
            Check(done == 1 && ctl.FinishedCount == 1 && !ctl.Enabled,
                  "B7 ★ 0.2 s 的拍推 0.25 s ⇒ **收尾 1 次**（与改动前逐位相同：调用点按 60 fps 算裕量）");
            ctl.Detach();
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }

        // B8：★ 数据侧反证 —— 「照抄 -1 档会切掉多少卡体动画」
        int total, cut;
        if (CountCardAnimDurations(out total, out cut))
        {
            float ratio = total > 0 ? (float)cut / total : 0f;
            Check(ratio > 0.5f,
                  $"B8 ★★ **数据反证**：`Resources/cardanim_clips.json` 现读 {total} 条卡体动画里 **{cut} 条 ≤ 1.0 s**"
                + $"（{ratio:P1}）⇒ 照抄原版 `-1` 档会让它们在**第一次 `Update` 就当帧收尾**"
                + "｜🧨 这条一旦 % 掉到 50 以下 ⇒ 「不该照抄」的前提变了，**必须重裁**（报告 `B120_W10_执行.md` §Q7）");
        }
        else
        {
            // ⛔ 不静默跳过：前提不成立要说出来
            Check(false, "B8 ✗ **前提不成立**：`Resources/cardanim_clips.json` 读不到或 `rows` 为空 —— 反证无法复核");
        }
    }

    /// <summary>读 `Resources/cardanim_clips.json`，数 `rows[]` 里 `vd+ts+tm+te ≤ 1.0` 的条数。
    /// 返回 false ⇒ 资产读不到 / 一条都没解析出来（调用方必须**报红**，⛔ 不许当通过）。</summary>
    static bool CountCardAnimDurations(out int total, out int cut)
    {
        total = 0; cut = 0;
        var ta = Resources.Load<TextAsset>("cardanim_clips");
        if (ta == null || string.IsNullOrEmpty(ta.text)) return false;
        var rx = new Regex(@"""vd""\s*:\s*([0-9.eE+-]+)\s*,\s*""ts""\s*:\s*([0-9.eE+-]+)\s*," +
                           @"\s*""tm""\s*:\s*([0-9.eE+-]+)\s*,\s*""te""\s*:\s*([0-9.eE+-]+)");
        var inv = CultureInfo.InvariantCulture;
        foreach (Match m in rx.Matches(ta.text))
        {
            float vd, ts, tm, te;
            if (!float.TryParse(m.Groups[1].Value, NumberStyles.Float, inv, out vd)) continue;
            if (!float.TryParse(m.Groups[2].Value, NumberStyles.Float, inv, out ts)) continue;
            if (!float.TryParse(m.Groups[3].Value, NumberStyles.Float, inv, out tm)) continue;
            if (!float.TryParse(m.Groups[4].Value, NumberStyles.Float, inv, out te)) continue;
            total++;
            if (vd + ts + tm + te <= 1.0f + 1e-6f) cut++;
        }
        return total > 0;
    }

    // ==================================================================
    //  C · Q9②：`PlaySoundAsset` = 只入队 + 每帧出队一条（原版 `soundsToPlay` / `PlayNextSoundInQueue`）
    // ==================================================================
    static void RunCampaignSoundAsset(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- C · Q9② `BattleCampaignScriptHost.PlaySoundAsset` ---");

        if (driver == null)
        {
            // ⛔ 不静默跳过：这条前提没有就说红
            Check(false, "C0 ✗ **前提不成立**：`driver == null` ⇒ 挂不了 `AudioSource`，C 组整组无法复核");
            return;
        }

        var host = new BattleCampaignScriptHost(driver);
        AudioClip clip = null;
        float fxWas = WarpforgeAudio.SoundFx;
        float listenerWas = AudioListener.volume;
        try
        {
            clip = AudioClip.Create("B120W10Probe", 256, 1, 44100, false);
            Check(clip != null, "（前提）探测 clip 建出来了");

            // ---- C1：只入队、**不立刻播** ----
            host.PlaySoundAsset(new CampaignSoundAsset { clip = clip, volume = 1f });
            Check(host.QueuedSoundCount == 1 && Mathf.Approximately(host.LastSoundQueueVolume, -1f),
                  "C1 ★ `PlaySoundAsset` ⇒ **只入队**（队里 1 条、`LastSoundQueueVolume` 仍是 -1）"
                + "｜🧨 改回「立刻 `PlayOneShot`」⇒ 本行红（判据 `BattleManager__PlaySoundAsset.c:18-52` 整支只有 append）");

            // ---- C6：clip 为空 ⇒ **不入队**（原版那道 `clip != null` 闸） ----
            host.PlaySoundAsset(new CampaignSoundAsset { clip = null, volume = 1f });
            Check(host.QueuedSoundCount == 1,
                  "C2 ✋ `clip == null` ⇒ **不入队**（`PlaySoundAsset.c:18-32` 的 `clip != null` 闸）");

            // ---- C2：`SoundFxVolume = 0` ⇒ 出队但**不播** ----
            WarpforgeAudio.SetSoundFx(0f);
            bool drained = host.PlayNextSoundInQueue();
            Check(drained && host.QueuedSoundCount == 0 && Mathf.Approximately(host.LastSoundQueueVolume, -1f),
                  "C3 ★ `SoundFx = 0` ⇒ **照样出队、但整条不播**（`PlayNextSoundInQueue.c:44-46` 的闸；"
                + "不是「以 0 音量播」）｜🧨 删掉 `if (vol > 0f)` ⇒ 本行红");

            // ---- C3：音量实参 = `SoundFxVolume × soundAsset.volume`，⛔ 不是 `AudioListener.volume` ----
            WarpforgeAudio.SetSoundFx(0.5f);
            AudioListener.volume = 0.25f;                 // 故意设成另一个值 ⇒ 谁被采信一眼看出
            host.PlaySoundAsset(new CampaignSoundAsset { clip = clip, volume = 0.4f });
            host.PlayNextSoundInQueue();
            Near(host.LastSoundQueueVolume, 0.2f, 1e-4f,
                 "C4 ★ 音量 = `SoundFx(0.5) × soundAsset.volume(0.4)` = **0.2**"
               + "｜🧨 改回 `AudioListener.volume × volume` ⇒ 本行红（会记到 0.1）");

            // ---- C4：落点 = 无混音组 ⇒ Master ----
            var src = host.SoundSourceForTest;
            Check(src != null && src.outputAudioMixerGroup == null,
                  "C5 ★ 那颗 `AudioSource` **不挂混音组**（原版 `AudioSource_1595.json` 的 `OutputAudioMixerGroup {0,0}` ⇒ Master）"
                + (src != null && src.outputAudioMixerGroup != null ? "（实得挂上了 " + src.outputAudioMixerGroup.name + "）" : "")
                + "｜🧨 改回 `WarpforgeAudio.VoicesGroup` ⇒ 本行红");
            if (src != null)
            {
                Near(src.volume, 1f, 1e-4f, "C6 ✋ 源自己的 `volume` = **1.0**（原版 `m_Volume`；音量全在 `PlayOneShot` 的实参上）");
                Check(src.priority == 154, "C7 ✋ 源 `priority` = **154**（原版 `Priority 154`）");
            }
            else
            {
                // ⛔ 不静默跳过：前提没了就报红（本项目踩过「`if (x != null) Check(...)` 空转绿」）
                Check(false, "C6/C7 ✗ **前提不成立**：C4 播完之后 `SoundSourceForTest` 仍是 null（源没建出来）");
            }

            // ---- C5：FIFO 一次一条（⛔ 不是「一次抽干」） ----
            host.PlaySoundAsset(new CampaignSoundAsset { clip = clip, volume = 1f });
            host.PlaySoundAsset(new CampaignSoundAsset { clip = clip, volume = 1f });
            Check(host.QueuedSoundCount == 2, "（前提）又入队两条");
            host.PlayNextSoundInQueue();
            Check(host.QueuedSoundCount == 1,
                  "C8 ★★ 每调一次 `PlayNextSoundInQueue` 只消费**一条**（原版 `Update` 每帧一句；`RemoveAt(0)` 一次一个）"
                + "｜🧨 改成「一次抽干」⇒ 本行红");
            host.PlayNextSoundInQueue();
            Check(host.QueuedSoundCount == 0, "C9 ★ 再调一次 ⇒ 抽干（`soundsToPlay` 归 0）");
        }
        catch (Exception e)
        {
            Check(false, "C 组抛异常：" + e.Message);
        }
        finally
        {
            WarpforgeAudio.SetSoundFx(fxWas);
            AudioListener.volume = listenerWas;
            if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
