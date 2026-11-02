// B120_W4_Assert.cs — 执行代理 `B120-W4`（2026-11-02）的断言：**动画/补间/时间轴 + 屏震**
//   判据与施工单 → `资料/完全复刻/报告/B120_W4_执行.md`（§C-2 / §C-3 / §C-4 / §D118-82 / §D118-85）
//
// ============================ 它盯的是什么 ============================
// ① `C-2` 回放跳过档下**近战整条 = 0.2 s**（不是把整条 0.4 一起 ÷3 得到的 0.133）：
//    原版 `_AttackMeleeAnim_d__357__MoveNext.c` 段1/2/4/5 走 `get_AttackStepTime()`（跳过档 ÷3），
//    段3 是 `AppendInterval(DAT_1834b2dc4)` = **字面量 0.1**（不除）⇒ `CardFeel.MeleeAttackDurationOf(1/3)`。
// ② `C-4` 远程收招的**段2**也随回放加速（`CardScript__AfterAttack.c:25-34` 的 `fVar5 = attackStepTime / 3`）
//    ⇒ `CardFeel.AttackRecoverDurationOf(1/3)` = 0.71667（段1 + `SetDelay(0.65)` + 段2）。
// ③ `C-3` 命中拍间隔 = `0.1 + 2×step`（正常 0.3 / 跳过 0.16667），不是 `0.4/3 − 0.1/3 = 0.1`。
// ④ `D118-82` 近战挨打震屏：**幅度 = amplitude(2/4/8) × |direction|，而 |direction| ≡ 1**（原版把
//    `normalize(目标 − 攻方)` 喂进 `SetDirection`）⇒ 比 preset 自带那档大 **1/0.28284 = 3.5356 倍**；
//    **方向** = 棋盘平面内的单位向量 ⇒ 屏幕上**只横抖**（preset 那档是**竖抖**）。
// ⑤ `D118-85` 震屏包络：attack = **`t²`**、decay = **三次 Hermite**（不是线性）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进宿主的**方法尾聚合行**（**同一物理行、该行第一个 `//` 之前**；⛔ 追加到行尾那段 `//` 之后 = 永不执行）：
//      B120W4_Assert.Run(); pass += B120W4_Assert.Pass; fail += B120W4_Assert.Fail;
//  建议落点 = `CardPresentation/Editor/BattleScene.cs:46983`（已挂着 `B120_W5/W3/W6` 等 20+ 件，
//  末尾紧跟 `Debug.Log(P + $"=== 合计：…")`；现读行号，用前再核）。
//
//  ⚠️ 本件**会建临时相机 / 临时 GameObject** 并真的调 `CardFeel.ShakeCamera*` / `CardFeel.MeleeAttack`
//     （与 `BattleScene.cs:24702-24714` 同一条打法），收尾 `SettleShake()` + `Kill()` + `DestroyImmediate` ⇒ 不残留。
//  ⚠️ 会临时改 `EventTiming.ReplaySkipAnims` / `CardFeel.ShakesEnabled`，**try/finally 里复原**。
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>`B120_W4` 的断言集（纯函数 + 一次「真相机」端到端）。</summary>
public static class B120W4_Assert
{
    public static int Pass;
    public static int Fail;

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log("[B120W4] ✓ " + what); }
        else { Fail++; Debug.LogError("[B120W4] ✗ " + what); }
    }

    /// <summary>`CardFeel.ShakePresetDirMag` 的倒数 —— **手算写死**（1 / 0.28284 = 3.53557），⛔ 不从被测代码里取。</summary>
    const float ExpectedDirMagRatio = 3.53557f;

    public static void Run()
    {
        // ==================================================================
        //  ① C-2：回放跳过档下的近战整条 = 0.1/3 + **0.1** + 0.2/3 = 0.2 s
        // ==================================================================
        float meleeNormal = CardFeel.MeleeAttackDurationOf(1f);
        float meleeSkip = CardFeel.MeleeAttackDurationOf(1f / 3f);
        Check(Mathf.Abs(meleeNormal - 0.4f) < 1e-5f,
              $"★ C-2（前提）：近战整条的**正常档** = {meleeNormal:F4}s（原版 0.1+0.1+0.2 = 0.4）");
        Check(Mathf.Abs(meleeSkip - 0.2f) < 1e-5f,
              $"★ C-2：回放跳过档近战整条 = {meleeSkip:F4}s（应 = 0.2 = 0.0333 + **0.1（段3 不除）** + 0.0667）");
        Check(Mathf.Abs(meleeSkip - 0.4f / 3f) > 0.05f,
              $"★ C-2 **灭自证**：跳过档 {meleeSkip:F4}s ≠ 「整条 ÷3」{0.4f / 3f:F4}s（差 > 0.05 ⇒ 换回整条 ÷3 必红）");
        // 公式 ↔ 实际补间必须一致（只改 `MeleeAttack` 不改公式、或反过来 ⇒ 必红）。
        var meleeGo = new GameObject("B120W4_MeleeSeq");
        try
        {
            var twSkip = CardFeel.MeleeAttack(meleeGo.transform, new Vector3(3f, 0f, 0f), Vector3.zero,
                                              0f, true, 1f / 3f);
            float seqSkip = CardFeel.SequenceSeconds(twSkip);   // ⚠️ 2026-11-02：编辑器程序集看不到 `DG.Tweening.Tween.Kill`（DOTween 引用只在运行时集）⇒ 不 Kill，靠 `DestroyImmediate(meleeGo)` 收尾
            var twNorm = CardFeel.MeleeAttack(meleeGo.transform, new Vector3(3f, 0f, 0f), Vector3.zero);
            float seqNorm = CardFeel.SequenceSeconds(twNorm);
            Check(Mathf.Abs(seqSkip - 0.2f) < 1e-4f && Mathf.Abs(seqNorm - 0.4f) < 1e-4f,
                  $"★ C-2（实测）：`MeleeAttack` 的 DOTween 总长 —— 跳过档 {seqSkip:F4}s / 正常档 {seqNorm:F4}s"
                + $"（公式 {meleeSkip:F4} / {meleeNormal:F4} ⇒ 必须一致）");
        }
        finally { Object.DestroyImmediate(meleeGo); }

        // ==================================================================
        //  ② C-4：收招段2 也随回放加速（`CardScript__AfterAttack.c:25-34`）
        // ==================================================================
        float recNormal = CardFeel.AttackRecoverDurationOf(1f);
        float recSkip = CardFeel.AttackRecoverDurationOf(1f / 3f);
        Check(Mathf.Abs(recNormal - 0.85f) < 1e-5f,
              $"★ C-4（前提）：收招整条正常档 = {recNormal:F4}s（0.1 + 0.65 + 0.1）");
        Check(Mathf.Abs(recSkip - (0.1f / 3f * 2f + 0.65f)) < 1e-5f,
              $"★ C-4：收招跳过档 = {recSkip:F4}s（应 = 0.0333×2 + **0.65（SetDelay 是字面量、不除）** = 0.71667）");
        Check(Mathf.Abs(recSkip - 0.85f / 3f) > 0.4f,
              $"★ C-4 **灭自证**：跳过档 {recSkip:F4}s ≠ 「整条 ÷3」{0.85f / 3f:F4}s（段2 写死 `AttackStepTime` 时这条必红）");

        // ==================================================================
        //  ③ C-3：命中拍间隔 = `AttackHoldTime + 2 × step`
        // ==================================================================
        var wasSkip = EventTiming.ReplaySkipAnims;
        try
        {
            var atk = new BattleEvent { Kind = EvtKind.Attack, Ranged = false };
            var hit = new BattleEvent { Kind = EvtKind.Hit, Ranged = false };

            EventTiming.ReplaySkipAnims = () => false;
            float gapNormal = EventTiming.DelayBetween(atk, hit);
            float durNormal = EventTiming.DurationOf(atk);
            EventTiming.ReplaySkipAnims = () => true;
            float gapSkip = EventTiming.DelayBetween(atk, hit);
            float durSkip = EventTiming.DurationOf(atk);

            Check(Mathf.Abs(gapNormal - 0.3f) < 1e-5f && Mathf.Abs(durNormal - 0.1f) < 1e-5f,
                  $"★ C-3（前提）：正常档 Attack 拍 {durNormal:F4}s、到 Hit 的间隔 {gapNormal:F4}s（0.1 / 0.3）");
            Check(Mathf.Abs(durSkip - 0.1f / 3f) < 1e-5f,
                  $"★ C-3：跳过档 Attack 拍 = {durSkip:F4}s（前摇 ÷3 = 0.0333 —— 这一格原来就是对的）");
            Check(Mathf.Abs(gapSkip - (0.1f + 0.1f / 3f * 2f)) < 1e-5f,
                  $"★ C-3：跳过档「到伤害结算」的间隔 = {gapSkip:F4}s（应 = **0.1（段3 不除）** + 2×0.0333 = 0.16667）");
            Check(Mathf.Abs(gapSkip - (0.4f / 3f - 0.1f / 3f)) > 0.05f,
                  $"★ C-3 **灭自证**：{gapSkip:F4}s ≠ 旧式「整条÷3 − 前摇÷3」{0.4f / 3f - 0.1f / 3f:F4}s（差 0.0667 ⇒ 改回去必红）");
        }
        finally { EventTiming.ReplaySkipAnims = wasSkip; }

        // ==================================================================
        //  ④ D118-82（幅度）：|direction| 从 0.28284（preset）改成 1.0（归一化攻击方向）
        // ==================================================================
        float ampPreset = CardFeel.ShakeWorldAmplitudeFor(0);         // 0.0570
        float ampAttack = CardFeel.ShakeWorldAmplitudeFor(0, 1f);     // 0.2017
        // 手算期望：2.0(amplitude) × 1.0(|dir|) × 0.149(ΣbandAmp) × (73.1/108)
        float expectAttack = 2f * 1f * 0.149f * (73.1f / 108f);
        Check(Mathf.Abs(ampAttack - expectAttack) < 1e-4f,
              $"★ D118-82（幅度）：覆盖档峰值 = {ampAttack:F5}（手算 2×1×0.149×73.1/108 = {expectAttack:F5}）");
        Check(Mathf.Abs(ampAttack / ampPreset - ExpectedDirMagRatio) < 0.02f,
              $"★ D118-82：覆盖档 / preset 档 = {ampAttack / ampPreset:F3}（应 = 1/0.28284 = {ExpectedDirMagRatio:F3} ⇒ **大 3.54 倍**）");
        Check(ampAttack / ampPreset > 3.4f && ampAttack / ampPreset < 3.7f,
              $"★ D118-82 **灭自证**：这个比值必须真的大于 1（恒 1 = 还在用 preset 的 |dir| ⇒ 红；≈100 = 把棋盘坐标当方向 ⇒ 也红）");
        Check(Mathf.Abs(CardFeel.ShakeWorldAmplitudeFor(2, 1f) - ampAttack * 2f) < 1e-5f
              && Mathf.Abs(CardFeel.ShakeWorldAmplitudeFor(3, 1f) - ampAttack * 4f) < 1e-5f,
              $"★ D118-82：尺寸 2 / 3 档 = 2× / 4×（amplitude 4.0 / 8.0）⇒ gain = 4 / 8");

        // ==================================================================
        //  ⑤ D118-82（方向）：`normalize(目标 − 攻方)` ⇒ 屏幕**横**抖；preset 那档是**竖**抖
        // ==================================================================
        Vector3 dirRaw = CardFeel.ShakeAttackDirectionOf(new Vector3(0f, 0f, 0f), new Vector3(2f, 0f, 0f));
        Check(Mathf.Abs(dirRaw.x - 2f) < 1e-4f && Mathf.Abs(dirRaw.z) < 1e-4f,
              $"★ D118-82（方向端点）：`ShakeAttackDirectionOf(攻方, 目标)` = ({dirRaw.x:F3},{dirRaw.y:F3},{dirRaw.z:F3})"
            + " —— 从攻方指向目标；`OrigBoardPosOf` 的 +100 在差里抵消、y 两边都归 0");
        Vector3 screenAttack = CardFeel.ShakeScreenDirOf(dirRaw);
        Vector3 screenPreset = CardFeel.ShakeScreenDirOf(CardFeel.ShakeHitDirection);
        Check(Mathf.Abs(screenAttack.x + 1f) < 1e-4f && Mathf.Abs(screenAttack.y) < 1e-5f,
              $"★ D118-82（方向）：攻击方向 (2,0,0) 的屏幕投影 = ({screenAttack.x:F4},{screenAttack.y:F4})（应 = (−1, 0)：原版棋盘 +x ⇒ 屏幕 −x）");
        Check(Mathf.Abs(screenPreset.y + 0.7071f) < 1e-3f && Mathf.Abs(screenPreset.x) < 1e-5f,
              $"★ D118-82（方向）：preset (0,0.2,0.2) 的屏幕投影 = ({screenPreset.x:F4},{screenPreset.y:F4})（应 = (0, −0.7071)：竖抖）");
        Check(Mathf.Abs(Vector3.Dot(screenAttack, screenPreset)) < 1e-5f
              && Mathf.Abs(screenAttack.y) < 1e-5f && Mathf.Abs(screenPreset.y) > 0.5f,
              "★ D118-82 **灭自证**：两条屏幕方向**正交**（一条纯横、一条纯竖）—— 「仍然固定传 preset 方向」⇒ 两条相等 ⇒ 红");

        // ==================================================================
        //  ⑥ D118-85（包络）：attack = t²、decay = 三次 Hermite（都不是线性）
        // ==================================================================
        //  attack 档：attackTime 0.4 / sustain 0 → 在 t = 0.2 处 u = 0.5
        float envAttackMid = CardFeel.ShakeEnvelope(0.2f, 0.4f, 0f, 0.1f);
        Check(Mathf.Abs(envAttackMid - 0.25f) < 1e-5f,
              $"★ D118-85（attack）：t=0.5·attackTime 处 = {envAttackMid:F5}（原版 `m_AttackShape` = **t²** ⇒ 0.25）");
        Check(Mathf.Abs(envAttackMid - 0.5f) > 0.1f,
              $"★ D118-85 **灭自证**：t² 的 0.25 ≠ 线性 0.5（差 0.25 ⇒ 改回线性必红）");

        //  decay 档：attack 0 / sustain 0.1 / decay 0.4 ⇒ held = 0.1；三个内点各测一次。
        //  期望值**手算**（Hermite：h00·1 + h01·0.01 + h11·0.035396047，两键 0→1、切线 0 / 0.03539604693651199）：
        //    u=0.25 → 0.84375 + 0.0015625 − 0.00165919 = 0.84365331
        //    u=0.50 → 0.5      + 0.005     − 0.00442451 = 0.50057549
        //    u=0.75 → 0.15625  + 0.0084375 − 0.00497757 = 0.15970993
        float d25 = CardFeel.ShakeEnvelope(0.1f + 0.25f * 0.4f, 0f, 0.1f, 0.4f);
        float d50 = CardFeel.ShakeEnvelope(0.1f + 0.50f * 0.4f, 0f, 0.1f, 0.4f);
        float d75 = CardFeel.ShakeEnvelope(0.1f + 0.75f * 0.4f, 0f, 0.1f, 0.4f);
        Check(Mathf.Abs(d25 - 0.8436533f) < 1e-5f && Mathf.Abs(d50 - 0.5005755f) < 1e-5f
              && Mathf.Abs(d75 - 0.1597099f) < 1e-5f,
              $"★ D118-85（decay）：三个内点 = {d25:F6} / {d50:F6} / {d75:F6}"
            + "（手算 Hermite 0.843653 / 0.500575 / 0.159710 —— 判据 = 场景 `MonoBehaviour_4914.json` 的 `m_DecayShape`）");
        //  线性档在同一组点的值 = 1 − u·0.99 = 0.7525 / 0.505 / 0.2575
        Check(Mathf.Abs(d25 - 0.7525f) > 0.05f && Mathf.Abs(d50 - 0.505f) > 1e-3f && Mathf.Abs(d75 - 0.2575f) > 0.05f,
              $"★ D118-85 **灭自证**：Hermite ≠ 线性（对照 {0.7525f:F4} / {0.505f:F4} / {0.2575f:F4}）—— 改回 `Lerp(1,0.01,·)` 必红");
        Check(Mathf.Abs(CardFeel.ShakeHermite01(0f, 1f, 0f, 0.01f, CardFeel.ShakeDecayInSlope) - 1f) < 1e-6f
              && Mathf.Abs(CardFeel.ShakeHermite01(1f, 1f, 0f, 0.01f, CardFeel.ShakeDecayInSlope) - 0.01f) < 1e-6f,
              "★ D118-85（前提下限）：Hermite 两端必须落在两个键值上（1 → 0.01），⛔ 不是 0");

        // ==================================================================
        //  ⑦ 端到端：真相机上「横抖 vs 竖抖」+ 覆盖档的归一化（同向不同距 ⇒ 同幅）
        // ==================================================================
        bool wasEnabled = CardFeel.ShakesEnabled;
        var go = new GameObject("B120W4_ShakeCam");
        try
        {
            CardFeel.ShakesEnabled = true;
            var cam = go.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 2f, -13.57f);
            Vector3 home = cam.transform.position;

            CardFeel.SettleShake();
            CardFeel.ShakeCameraForHit(cam, null, 0, 0f, null, 777);                       // preset 档（不覆盖方向）
            Vector3 dPreset = cam.transform.position - home;
            CardFeel.SettleShake();
            // 前提（⛔ 不是结论）：这个种子下 t=0 的噪声必须非零 —— 否则「只有竖直分量」这类判断没有意义。
            Check(Mathf.Abs(CardFeel.ShakeNoise01(0f, CardFeel.LastShakePhase)) > 1e-2f,
                  $"★ 前提：种子 777 在 t=0 的噪声 = {CardFeel.ShakeNoise01(0f, CardFeel.LastShakePhase):F5}（必须显著非零，方向断言才有意义）");

            CardFeel.ShakeCameraForHit(cam, null, 0, 0f, null, 777, new Vector3(2f, 0f, 0f));   // 覆盖档：攻击方向 +x
            Vector3 dAttack = cam.transform.position - home;
            CardFeel.SettleShake();

            Check(Mathf.Abs(dPreset.x) < 1e-5f && Mathf.Abs(dPreset.y) > 1e-5f,
                  $"★ D118-82（端到端）：preset 档位移 = ({dPreset.x:F5}, {dPreset.y:F5}, {dPreset.z:F5}) ⇒ **只有竖直**分量");
            Check(Mathf.Abs(dAttack.y) < 1e-5f && Mathf.Abs(dAttack.x) > 1e-5f,
                  $"★ D118-82（端到端）：覆盖档位移 = ({dAttack.x:F5}, {dAttack.y:F5}, {dAttack.z:F5}) ⇒ **只有水平**分量（屏幕 −x）");
            Check(Mathf.Abs(dAttack.x) > 1e-5f && Mathf.Abs(dPreset.y) > 1e-5f
                  && Mathf.Abs(Vector3.Dot(dAttack.normalized, dPreset.normalized)) < 1e-3f,
                  "★ D118-82 **灭自证（端到端）**：两条位移方向正交 —— 方向没换过来时这条必红");

            // 归一化：同一方向、不同长度 ⇒ **同一幅度**（原版 `SetDirection` 收的是归一化向量）
            CardFeel.SettleShake();
            CardFeel.ShakeCameraForHitFrom(cam, null, new Vector3(0f, 0f, 0f), new Vector3(2f, 0f, 0f), 0, 0f, null, 777);
            float nearMag = (cam.transform.position - home).magnitude;
            CardFeel.SettleShake();
            CardFeel.ShakeCameraForHitFrom(cam, null, new Vector3(0f, 0f, 0f), new Vector3(6f, 0f, 0f), 0, 0f, null, 777);
            float farMag = (cam.transform.position - home).magnitude;
            CardFeel.SettleShake();
            Check(nearMag > 1e-6f && Mathf.Abs(farMag / nearMag - 1f) < 1e-4f,
                  $"★ D118-82 **灭自证（归一化）**：目标距离 2 / 6 的前置位移 = {nearMag:F5} / {farMag:F5}"
                + " —— 必须**相等**（原版把差向量归一化后传 `SetDirection`；按坐标模长算 ⇒ 3 倍 ⇒ 红）");
        }
        finally
        {
            CardFeel.SettleShake();
            CardFeel.ShakesEnabled = wasEnabled;
            Object.DestroyImmediate(go);
        }

        // ==================================================================
        //  ⑧ C-5：`SW21` 的**替代支 0.2 / 普通支 0.4**（按 `normalAbilityBoardCriteria` 二选一）
        //     判据 = 原版 `RawCardScript__GetActiveAbility.c` 全文 17 行：
        //       `if (hasAlternativeAbility(+0x2AA)) { if (!MeetsCriteria(param_2, *(this+0x2B0), …))
        //            return *(this+0x2B8); }  return *(this+0x260);`
        //       ⇒ **criteria 满足 = 普通支**、不满足 = 替代支（⛔ 方向别反）。
        // ==================================================================
        var wasAlt = EventTiming.AbilityUsesAlternative;
        try
        {
            var sw = new BattleEvent { Kind = EvtKind.Ability, CardId = "SW21" };
            float sNorm, sAlt;
            bool hasNorm = AbilityAnimDurations.TryGet("SW21", out sNorm);
            bool hasAlt = AbilityAnimDurations.TryGetAlternative("SW21", out sAlt);
            Check(hasNorm && Mathf.Abs(sNorm - 0.4f) < 1e-5f,
                  $"★ C-5（前提）：运行期表 `SW21` 普通支 = {(hasNorm ? sNorm : -1f):F3}s（应 = 0.4）");
            Check(hasAlt && Mathf.Abs(sAlt - 0.2f) < 1e-5f,
                  $"★ C-5（前提）：运行期表 `SW21` 替代支 = {(hasAlt ? sAlt : -1f):F3}s（应 = 0.2 —— 全池只有这一张带 `alt`）");

            EventTiming.AbilityUsesAlternative = _ => false;                 // criteria 满足 ⇒ 普通支
            float holdNormal = EventTiming.AbilityHoldOf(sw);
            EventTiming.AbilityUsesAlternative = _ => true;                  // criteria 不满足 ⇒ 替代支
            float holdAlt = EventTiming.AbilityHoldOf(sw);

            Check(Mathf.Abs(holdNormal - 0.4f) < 1e-5f && Mathf.Abs(holdAlt - 0.2f) < 1e-5f,
                  $"★ C-5：`AbilityHoldOf(SW21)` —— 普通支 {holdNormal:F3}s / 替代支 {holdAlt:F3}s（0.4 / 0.2）");
            Check(Mathf.Abs(holdNormal - holdAlt) > 0.1f,
                  $"★ C-5 **灭自证**：两支必须能区分（差 {Mathf.Abs(holdNormal - holdAlt):F3}s > 0.1）"
                + " —— 「永远走普通支」（`AbilityUsesAlternative` 没接）或「永远走替代支」都会让差 = 0 ⇒ 红");

            // 没有替代支的卡：两支必须**同值**（防「一律按 alt 查表」）。
            string otherId = null;
            foreach (var it in AbilityAnimDurations.Items)
            {
                if (it != null && !string.IsNullOrEmpty(it.id) && it.id != "SW21" && Mathf.Abs(it.alt) < 1e-6f && it.s > 0f) { otherId = it.id; break; }
            }
            Check(otherId != null, "★ C-5（前提）：表里找得到一张**没有替代支**的卡（用它做「两支同值」的对照）");
            if (otherId != null)
            {
                var oe = new BattleEvent { Kind = EvtKind.Ability, CardId = otherId };
                EventTiming.AbilityUsesAlternative = _ => false;
                float oNormal = EventTiming.AbilityHoldOf(oe);
                EventTiming.AbilityUsesAlternative = _ => true;
                float oAlt = EventTiming.AbilityHoldOf(oe);
                Check(Mathf.Abs(oNormal - oAlt) < 1e-6f && oNormal > 0f,
                      $"★ C-5 **灭自证（对照）**：`{otherId}`（无替代支）两支同值 = {oNormal:F3}s"
                    + " —— 「一律走 alt 表」会让它变 0 ⇒ 红");
            }

            // 数据侧：`cards_engine.json` 里那两列真的还在（防生成器把它们丢了）。
            var ta = Resources.Load<TextAsset>("cards_engine");
            Check(ta != null, "★ C-5（前提）：读得到 `Resources/cards_engine.json`");
            if (ta != null)
            {
                var defs = CardDatabase.Parse(ta.text);
                Check(defs != null && defs.Count > 0, "★ C-5（前提）：`cards_engine.json` 解析出卡表");
                if (defs != null && defs.Count > 0)
                {
                int altCount = 0, gateCount = 0;
                CardDef swDef = null;
                for (int i = 0; i < defs.Count; i++)
                {
                    var d = defs[i];
                    if (d.HasAlternativeAbility == 1) altCount++;
                    if (d.NormalAbilityBoardCriteria != null) gateCount++;
                    if (d.Id == "SW21") swDef = d;
                }
                // ⚠️ 这两个读数先算成局部量再插值 —— C# 9 的插值表达式里**不能直接写字符串字面量**。
                string swAltTxt = swDef != null ? swDef.HasAlternativeAbility.ToString() : "null";
                string swGateTxt = (swDef != null && swDef.NormalAbilityBoardCriteria != null) ? "有闸" : "无闸";
                Check(swDef != null && swDef.HasAlternativeAbility == 1 && swDef.NormalAbilityBoardCriteria != null,
                      "★ C-5（数据侧）：`SW21` 解析出 `HasAlternativeAbility == 1` 且 `NormalAbilityBoardCriteria != null`"
                    + $"（实得 {swAltTxt} / {swGateTxt}）");
                Check(altCount >= 1 && gateCount >= 1,
                      $"★ C-5（数据侧）：全池 `hasAlternativeAbility == 1` **{altCount}** 张 / 非默认闸 **{gateCount}** 张"
                    + "（⚠️ 张数**不写死**：2026-11-02 现读 = 1 / 2；判据是「两列都没丢」⇒ 生成器丢列时这里为 0 必红）");
                }
            }
        }
        finally { EventTiming.AbilityUsesAlternative = wasAlt; }
    }
}
