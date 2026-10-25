// TraitEffectAssert.cs — `N15` · `TraitEffectController` 钩子层的断言（8 组，含 2 条灭自证）
//
// 判据正本 → `Unity/资料/完全复刻/判据/TraitEffectController_钩子层.md`（下称「判据」）
// 上一轮报告 → `Unity/资料/完全复刻/报告/N15_TraitEffect_第1轮.md`（接线点表 W0–W10）
//
// 用法（本批**没有**跑，只写）：
//   Unity.exe -batchmode -quit -projectPath d:/4/Unity/MyGame -executeMethod TraitEffectAssert.Run -logFile -
//   筛输出：grep "^N15 " <日志>
//
// 🔴 **本文件只断、不接线、不改任何既有文件**：不建场景、不碰 `TraitFrames` / `BattleDriver` / 引擎状态，
//    只在自己的**假 GO** 上跑（`Editor/` 程序集里，命名空间用全局 —— 与 `BadgeFilterAssert` / `CardRootAssert` 同款）。
//
// ⚠️ 三条**必须知道的前提**（踩过/核过才这么写）：
//   ① **不依赖真实 prefab 与效果库**：批处理里 prefab 可能没导入 ⇒ 全部用 `new GameObject` 造
//      （TMP 走工程唯一那条造法 `TmpFont.NewText`，它带字体资产；造不出就报「**没验到**」，⛔ 不报通过）。
//   ② **编辑模式下 Unity 不自动跑 `Awake` / `Start`**（这两个是运行时消息）⇒ `MarkerlightController.Start`
//      在这里**显式反射调**（等价于运行时「下一拍」）；`HuntMarkController` 靠 `UpdateValue` 里的
//      `BindIfMissing()` 自绑，不依赖 `Awake`（这一条本身也是**设计要求**：原版 `UpdateValue` 早于 `Start`）。
//   ③ **DOTween 在批处理里不推进** ⇒ 断言**只测**「状态 / 调用次数 / 文本值」，⛔ **不测**「0.5 秒后弹完」；
//      时长参数只做**常量比对**（期望值来自 `.rdata` 实读，见 §「常量」那段）。
//      本模块把 `CardTween.Mode` 临时切成 `Manual` 并在 `finally` 还原。
//
// ⏳ 还**不能**写、但接线后必须补的（见文件末「待接线」那段注释）：
//   A-E3 的原版形（「prefab 上没有控制器也要发生调用」= `TraitFrames` 侧）、A-E2 的 `InstanceId` 部分、
//   A-LIFE（idle 框 >6 s 不自毁）、灭自证「同帧连刷 3 次 ⇒ `PunchCount` 仍 1」（= W8 的帧锁）。
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CardPresentation;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>`N15` 的断言集合。<see cref="Run"/> 幂等（只碰自己造的假 GO；`finally` 里 `DestroyImmediate` + `DOTween.KillAll`）。</summary>
public static class TraitEffectAssert
{
    const string P = "N15 ";

    public static int Passed { get; private set; }
    public static int Failed { get; private set; }

    public static void Run()
    {
        Passed = 0;
        Failed = 0;

        UpdateType oldMode = CardTween.Mode;
        CardTween.Mode = UpdateType.Manual;   // 批处理没有帧循环（见文件头 ③）
        Debug.Log(P + "=== TraitEffectController 钩子层断言（N15）开始 ===");

        try
        {
            Safe("A-E1", Block_E1);
            Safe("A-E2", Block_E2);
            Safe("A-E3", Block_E3);
            Safe("A-E4", Block_E4);
            Safe("A-ML", Block_ML);
            Safe("A-ML2", Block_ML2);
            Safe("灭自证1", Block_AntiSelfProof1);
            Safe("灭自证2", Block_AntiSelfProof2);
        }
        finally
        {
            DOTween.KillAll(false);     // 只清本模块造的补间（本模块是独立入口，不与其他宿主共用一轮）
            CardTween.Mode = oldMode;
            Summary();
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-E1 · 首次挂载：挂对类 → 写值 → 弹一次（并比对「原版数值」那一组常量）
    //   判据 §6.4 A-E1 · §3.2（`HuntMarkController__UpdateValue.c:13-30`）· §6.2（按 prefab 名挂）
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_E1()
    {
        GameObject root = null;
        try
        {
            root = NewRoot("HuntMark_IdleEffect");
            TextMeshPro counter = TmpFont.NewText(root.transform, "Counter", "1", 6f, Color.white);
            Check(counter != null,
                  "(前提 A-E1) 造得出假 `Counter`（TMP + 字体资产）—— 造不出的话本节**没验到**，别读成通过");

            // 「原版数值」比对：期望值 = `.rdata` / 指令立即数实读（判据 §3.2），⛔ 不是「等于我们代码里的常量」
            //   0.5 = DAT_1834b2bb4 · 10 = `mov r9d,0xa` · 1.0 = DAT_1834b2bb8 · 0.1 = DAT_1834b2dc4
            Check(HuntMarkController.PunchDuration == 0.5f
                  && HuntMarkController.PunchVibrato == 10
                  && HuntMarkController.PunchElasticity == 1.0f
                  && HuntMarkController.PunchScale == 0.1f,
                  "A-E1·punch 四个参数 = **0.1 / 0.5 s / vibrato 10 / elasticity 1.0**"
                + "（`.rdata` 实读：DAT_1834b2dc4 / DAT_1834b2bb4 / `mov 0xa` / DAT_1834b2bb8）"
                + "｜现得 " + HuntMarkController.PunchScale + " / " + HuntMarkController.PunchDuration
                + " / " + HuntMarkController.PunchVibrato + " / " + HuntMarkController.PunchElasticity
                + " —— 🧨 按手感调参（例如 0.2 / 1 s）⇒ 红");

            TraitEffectController ctl = TraitEffectControllers.Attach(root, "HuntMark_IdleEffect");
            Check(ctl is HuntMarkController,
                  "A-E1·`Attach(\"HuntMark_IdleEffect\")` 挂的是 `HuntMarkController`（现得 " + TypeName(ctl) + "）");
            Check(root.GetComponents<TraitEffectController>().Length == 1,
                  "A-E1·根上 `TraitEffectController` **恰好 1 个**（不重复挂；判据 §6.2 的幂等前提）");
            if (ctl == null) return;

            ctl.UpdateValue(2);
            var h = ctl as HuntMarkController;
            Check(h != null && h.TextForTest == "2",
                  "A-E1·`UpdateValue(2)` ⇒ `Counter.text == \"2\"`（现得 "
                + (h == null ? "<不是 HuntMarkController>" : "\"" + h.TextForTest + "\"") + "）"
                + " —— 🧨 不写文本 / 写死 prefab 的 `1` ⇒ 红");
            Check(h != null && h.UpdateCalls == 1 && h.LastValue == 2 && h.PunchCount == 1,
                  "A-E1·调用记账 = `UpdateCalls 1 / LastValue 2 / PunchCount 1`"
                + "（现得 " + (h == null ? "-" : h.UpdateCalls + " / " + h.LastValue + " / " + h.PunchCount) + "）"
                + " —— 🧨 写了文本但不弹 / 弹了但没记 `LastValue` ⇒ 红");
        }
        finally { Kill(root); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-E2 · 再次刷新：**不重建**、不重复挂、计数累加
    //   判据 §6.4 A-E2 · §1.4 ③ 号调用点（`DisplayStatusAnim.c:126-198`：已有 ⇒ 归位 + SetActive + UpdateValue）
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_E2()
    {
        GameObject root = null;
        try
        {
            root = NewRoot("HuntMark_IdleEffect");
            TextMeshPro counter = TmpFont.NewText(root.transform, "Counter", "1", 6f, Color.white);
            Check(counter != null, "(前提 A-E2) 造得出假 `Counter` —— 造不出 ⇒ 本节**没验到**");

            TraitEffectController a = TraitEffectControllers.Attach(root, "HuntMark_IdleEffect");
            if (a == null) { Check(false, "A-E2·`Attach` 返回 null ⇒ 本节没验到"); return; }

            int rootId = root.GetInstanceID();
            int counterId = counter != null ? counter.GetInstanceID() : 0;
            int ctlId = a.GetInstanceID();

            a.UpdateValue(2);

            // 「再次显示」= 同一个实例上再调一次（原版不重建框）
            TraitEffectController b = TraitEffectControllers.Attach(root, "HuntMark_IdleEffect");
            Check(ReferenceEquals(a, b),
                  "A-E2·第二次 `Attach` 返回**同一个**控制器实例（幂等）—— 🧨 每次都 `AddComponent` 一个新的 ⇒ 红");

            a.UpdateValue(3);
            var h = a as HuntMarkController;
            Check(h != null && h.TextForTest == "3" && h.UpdateCalls == 2 && h.PunchCount == 2,
                  "A-E2·再来一次 `UpdateValue(3)` ⇒ 文本 `\"3\"` / `UpdateCalls 2` / `PunchCount 2`"
                + "（现得 " + (h == null ? "-" : "\"" + h.TextForTest + "\" / " + h.UpdateCalls + " / " + h.PunchCount) + "）"
                + " —— 🧨 第二次不刷（只首次生效）/ 每次都重建 ⇒ 红");
            Check(root.GetInstanceID() == rootId
                  && (counter == null || counter.GetInstanceID() == counterId)
                  && a.GetInstanceID() == ctlId,
                  "A-E2·**实例一个都没换**（根 / `Counter` / 控制器 三个 `InstanceID` 全不变）"
                + " —— 🧨 用「销毁旧框重建新框」冒充刷新 ⇒ 红");
            Check(root.GetComponents<TraitEffectController>().Length == 1,
                  "A-E2·刷完之后根上仍然只有 **1 个**控制器 —— 🧨 每次刷新加一个 ⇒ 红");
        }
        finally { Kill(root); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-E3 · 「调用必须发生」+ 绑不上必须出声（本层能验的那一半）
    //   判据 §6.4 A-E3 的原版形在 `TraitFrames` 那侧（prefab 上没有控制器也要调）；本层验：
    //   `UpdateValue` **先记账再判能不能写**，绑不上 ⇒ `UpdateCalls` 照涨、`PunchCount` 不涨、**LogError 出声**。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_E3()
    {
        GameObject root = null;
        try
        {
            root = NewRoot("HuntMark_IdleEffect");   // 故意**不造** `Counter` 子件

            var ctl = TraitEffectControllers.Attach(root, "HuntMark_IdleEffect") as HuntMarkController;
            if (ctl == null) { Check(false, "A-E3·`Attach` 返回 null ⇒ 本节没验到"); return; }

            List<string> errs = CaptureStart();
            try { ctl.UpdateValue(7); }
            finally { CaptureStop(); }

            Check(ctl.UpdateCalls == 1 && ctl.LastValue == 7,
                  "A-E3·**绑不上也照样记账**（`UpdateCalls 1 / LastValue 7`；现得 "
                + ctl.UpdateCalls + " / " + ctl.LastValue + "）"
                + " —— 🧨 把计数挪到「绑上了才记」或提前 return ⇒ 红（判据：调用发生了就必须可观测）");
            Check(ctl.PunchCount == 0 && ctl.TextForTest == null && !ctl.Bound,
                  "A-E3·绑不上 ⇒ **不弹、不写文本**（`PunchCount 0` / `TextForTest null`；现得 "
                + ctl.PunchCount + " / " + (ctl.TextForTest ?? "null") + "）");
            Check(HasError(errs, "[HuntMarkController]"),
                  "A-E3·绑不上**必须出声**（捕获到含 `[HuntMarkController]` 的 `LogError`；本轮错误 "
                + errs.Count + " 条）—— 🧨 改成静默 return/null 检查 ⇒ 红（铁律「不许静默失败」）");
        }
        finally { Kill(root); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-E4 · **按 prefab 名挂，⛔ 不按 trait 名挂**（判据 §6.2 + §6.4 灭自证 2 的「挂载范围」那一半）
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_E4()
    {
        GameObject a = null, b = null, c = null;
        try
        {
            a = NewRoot("WhateverRootName");
            Check(TraitEffectControllers.Attach(a, "MarkerlightIdle") is MarkerlightController,
                  "A-E4·`Attach(\"MarkerlightIdle\")` ⇒ `MarkerlightController`（判据 §1.3 的第二个挂载点）");

            b = NewRoot("HuntMark_StartEffect");
            TraitEffectController bad1 = TraitEffectControllers.Attach(b, "HuntMark_StartEffect");
            Check(bad1 == null && b.GetComponent<TraitEffectController>() == null,
                  "A-E4·**同名家族的别的 prefab**（`HuntMark_StartEffect`）**不许挂**控制器"
                + "（判据 §1.3：全 build 只有 `HuntMark_IdleEffect` / `MarkerlightIdle` 两个）"
                + " —— 🧨 改成「名字里含 `HuntMark` 就挂」⇒ 红");

            c = NewRoot("TraitNameOnly");
            TraitEffectController bad2 = TraitEffectControllers.Attach(c, "huntmark");
            Check(bad2 == null && c.GetComponent<TraitEffectController>() == null,
                  "A-E4·按 **trait 名**（`huntmark`）挂**挂不上**"
                + " —— 🧨 改成按 trait 名挂（判据 §6.4 灭自证 2 明令禁止）⇒ 红");

            Check(TraitEffectControllers.Attach(null, "HuntMark_IdleEffect") == null
                  && TraitEffectControllers.Attach(a, "") == null
                  && TraitEffectControllers.Attach(a, null) == null,
                  "A-E4·入参防御：`go == null` / `prefabName` 空串 / `null` ⇒ 一律返回 null（⛔ 不抛、⛔ 不挂）");
        }
        finally { Kill(a); Kill(b); Kill(c); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-ML · `MarkerlightController.Start` 的两态 + `floors` 绑定 + `fadeTime`
    //   判据 §3.2（`__Start.c:25-48`）· §1.3 / §七 F7（`fadeTime` = prefab 覆写 0.5）· §6.4 A-ML
    //   ⚠️ 判据 §6.2 写「两个 `Grid floor` 都是根的直接子件」—— **实测不成立**（报告 §⑤·b F-A）：
    //      这里**故意**造成真实层级 `…/Fixed/Grid floor`，并放一个诱饵渲染器，钉死「按名字过滤」这件事。
    //   ⚠️ `_RippleScale` 的**数值**验不了：工程里没有带该属性的 shader（`grep -rl _RippleScale *.shader` 0 命中）
    //      ⇒ 只验「属性名 = `_RippleScale`」+「绑到哪两个 renderer」。⛔ 不把「没验到」写成通过。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_ML()
    {
        GameObject root = null;
        try
        {
            root = NewRoot("MarkerlightIdle");
            TextMeshPro counter = TmpFont.NewText(root.transform, "Counter", "1", 6f, Color.white);
            // 原版 prefab 里这两层**序列化时都是 active=1**（判据 §3.2）⇒ 假物体照抄这个前提
            GameObject normal = NewChild(root.transform, "Normal State");
            GameObject expiring = NewChild(root.transform, "Expiring State");
            Transform normalFixed = EnsurePath(normal.transform, "Base VFX/Fixed");
            Transform expiringFixed = EnsurePath(expiring.transform, "Base Vfx/Fixed");
            ParticleSystemRenderer floorN = NewRenderer(normalFixed, "Grid floor");
            ParticleSystemRenderer floorE = NewRenderer(expiringFixed, "Grid floor");
            ParticleSystemRenderer decoy = NewRenderer(normalFixed, "Ground Effect");   // 诱饵：同名父链、不同名字

            Check(counter != null && normal != null && expiring != null && floorN != null && floorE != null && decoy != null,
                  "(前提 A-ML) 假 `MarkerlightIdle` 造齐（TMP + 两态 + 两个 `Grid floor` + 诱饵）—— 造不齐 ⇒ 本节**没验到**");

            var ctl = TraitEffectControllers.Attach(root, "MarkerlightIdle") as MarkerlightController;
            if (ctl == null) { Check(false, "A-ML·`Attach` 返回 null ⇒ 本节没验到"); return; }

            Check(InvokeVoid(ctl, "Start"),
                  "A-ML·`Start` 调得到（编辑模式不自动跑生命周期 ⇒ 显式反射调，等价运行时「下一拍」）"
                + " —— 调不到的话下面三格都是**没验到**");

            Check(ctl.FloorsBound == 2,
                  "A-ML·`floors` 绑到 **2 个**（现得 " + ctl.FloorsBound + "）"
                + " —— 🧨 用 `transform.Find(\"Grid floor\")`（判据 §6.2 那句的老写法）⇒ 恒 0 ⇒ 红；"
                + "🧨 取「全部 `ParticleSystemRenderer`」⇒ 3 个（含诱饵）⇒ 红");
            Check(ctl.FloorsForTest != null && ctl.FloorsForTest.Length == 2
                  && SameSet(ctl.FloorsForTest, new Renderer[] { floorN, floorE })
                  && !Contains(ctl.FloorsForTest, decoy),
                  "A-ML·绑的正是**那两个** `Grid floor` 的渲染器（`Ground Effect` 诱饵不在里面）"
                + " —— 🧨 不按名字过滤 / 过滤条件写错 ⇒ 红");
            Check(normal.activeSelf && !expiring.activeSelf,
                  "A-ML·`Start` 之后 **Normal = on / Expiring = off**（现得 "
                + (normal.activeSelf ? "on" : "off") + " / " + (expiring.activeSelf ? "on" : "off") + "）"
                + " —— 🧨 删掉 `Start` 里那两句 `SetActive` ⇒ 两层叠着画 ⇒ 红（判据 §3.2 的原话）");
            Check(ctl.FadeTimeForTest == 0.5f,
                  "A-ML·`fadeTime == 0.5`（prefab 覆写值；现得 " + ctl.FadeTimeForTest + "）"
                + " —— 🧨 照 ILSpy 字段默认值 `1f` 写 ⇒ 慢一倍 ⇒ 红（判据 §七 F7 / 铁律 5·c）");
            Check(MarkerlightController.RippleScaleProperty == "_RippleScale",
                  "A-ML·shader 属性名字面量 = `_RippleScale`（判据 §3.2 的原版源码字面量）"
                + "｜⚠️ 该属性的**数值**本节**没验到**（工程里没有带它的 shader，`Material.GetFloat` 读不回来）");
        }
        finally { Kill(root); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-ML2 · 两态切换 + 两个 `AimingAttack` 的「未实现出口」语义
    //   判据 §6.1③（`MarkerLightUsed :88-93`）· §1.1（HuntMark 的 `AimingAttack` **throw**）
    //   · §6.6 P2（Markerlight 的 `AimingAttack` 依赖还没有的 `MaterialFader`）
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_ML2()
    {
        GameObject root = null, hRoot = null;
        try
        {
            // ── ① `MarkerLightUsed()`：Normal → Expiring（原版 :88-93 直接三句）─────────
            root = NewRoot("MarkerlightIdle");
            TextMeshPro mlCounter = TmpFont.NewText(root.transform, "Counter", "1", 6f, Color.white);
            GameObject normal = NewChild(root.transform, "Normal State");
            GameObject expiring = NewChild(root.transform, "Expiring State");
            // 两个 `Grid floor` 都要造（否则 `Start` 会刷一条「绑到 1 个」的告警，污染自检日志）
            NewRenderer(EnsurePath(normal.transform, "Base VFX/Fixed"), "Grid floor");
            NewRenderer(EnsurePath(expiring.transform, "Base Vfx/Fixed"), "Grid floor");

            var ml = TraitEffectControllers.Attach(root, "MarkerlightIdle") as MarkerlightController;
            if (ml == null || mlCounter == null) { Check(false, "A-ML2·`Attach`/TMP 没造出来 ⇒ 本节没验到"); return; }
            InvokeVoid(ml, "Start");
            Check(!ml.AlreadyAttackedForTest && normal.activeSelf && !expiring.activeSelf,
                  "A-ML2(前提)·`Start` 之后 = Normal on / Expiring off / `alreadyAttacked == false`");

            ml.MarkerLightUsed();
            Check(ml.AlreadyAttackedForTest && !normal.activeSelf && expiring.activeSelf,
                  "A-ML2·`MarkerLightUsed()` ⇒ `alreadyAttacked = true` + Normal **off** / Expiring **on**"
                + "（现得 " + ml.AlreadyAttackedForTest + " / " + (normal.activeSelf ? "on" : "off")
                + " / " + (expiring.activeSelf ? "on" : "off") + "）"
                + " —— 🧨 只置标志不切态 / 切反 ⇒ 红");
            Check(ml.TextForTest == "1",
                  "A-ML2·`MarkerLightUsed()` **不刷计数器**（文本仍是 `\"1\"`；现得 \"" + ml.TextForTest + "\"）"
                + " —— 🧨 顺手在里面调 `UpdateValue` ⇒ 无端多弹一次 ⇒ 红");

            // ── ② `HuntMarkController.AimingAttack` 必须 **throw**（原版 :31-37）───────────
            hRoot = NewRoot("HuntMark_IdleEffect");
            TmpFont.NewText(hRoot.transform, "Counter", "1", 6f, Color.white);
            var hm = TraitEffectControllers.Attach(hRoot, "HuntMark_IdleEffect") as HuntMarkController;
            bool threw = false;
            if (hm != null)
            {
                try { hm.AimingAttack(false); }
                catch (NotImplementedException) { threw = true; }
                catch (Exception ex) { Debug.LogWarning(P + "  (A-ML2) `HuntMarkController.AimingAttack` 抛的不是 "
                                                      + "`NotImplementedException` 而是 " + ex.GetType().Name); }
            }
            Check(threw,
                  "A-ML2·`HuntMarkController.AimingAttack` **抛 `NotImplementedException`**（原版就是这么写的）"
                + " —— 🧨 改成空 body「假装实现」⇒ 红");

            // ── ③ `MarkerlightController.AimingAttack`：不抛、但**必须出声**且**不改任何状态** ──
            if (ml != null)
            {
                string before = Snapshot(root);
                List<string> errs = CaptureStart();
                bool threwMl = false;
                try
                {
                    try { ml.AimingAttack(true); }
                    catch (Exception) { threwMl = true; }
                }
                finally { CaptureStop(); }
                Check(!threwMl && HasError(errs, "[MarkerlightController]") && Snapshot(root) == before,
                      "A-ML2·`MarkerlightController.AimingAttack` **不抛** + **出声** + **一个状态都不改**"
                    + "（明确「未实现」出口；现得 抛=" + threwMl + " / 错误 " + errs.Count + " 条 / 状态 "
                    + (Snapshot(root) == before ? "未变" : "变了") + "）"
                    + " —— 🧨 写成空 body 静默 / 或顺手把两态切了却仍没接 `MaterialFader` ⇒ 红（判据 §6.6 P2）");
            }
        }
        finally { Kill(root); Kill(hRoot); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 灭自证 1 · 「不调用就不动」+「防抖**不在**这一层」
    //   🔴 为什么这么写（与调度台给的例子有一处**刻意的偏差**，理由写死在这里）：
    //      判据 §6.4 的灭自证 1 原文是「**同帧连刷 3 次（值不变）⇒ `PunchCount` 仍为 1**」，
    //      但那道闸门在 **`TraitFrames` 的帧锁（W8）**，**不在**控制器里 —— 原版 `UpdateValue`
    //      每被调一次就弹一次（`HuntMarkController__UpdateValue.c:13-30` 无任何「同值就跳过」的判断）。
    //      ⇒ 在本层断「同值不弹」会**逼出一个原版没有的实现**。这里改成两条结构上等价、方向相反的钉子：
    //         (a) **没人调用** ⇒ 文本/计数**一个都不动**（挡「在 Awake/Start 里自己刷」）；
    //         (b) **同值连调两次 ⇒ 弹两次**（挡「把防抖塞进控制器」）。
    //      真正的「值没变不刷」等 W8 接好后，在 `TraitFrames` 侧补判据那条原话（见文件末「待接线」）。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_AntiSelfProof1()
    {
        GameObject root = null;
        try
        {
            root = NewRoot("HuntMark_IdleEffect");
            TmpFont.NewText(root.transform, "Counter", "1", 6f, Color.white);
            var h = TraitEffectControllers.Attach(root, "HuntMark_IdleEffect") as HuntMarkController;
            if (h == null) { Check(false, "灭自证1·`Attach` 返回 null ⇒ 本节没验到"); return; }

            Check(h.TextForTest == "1" && h.PunchCount == 0 && h.UpdateCalls == 0,
                  "灭自证 1a·**没人调用**时：文本仍是 prefab 的 `\"1\"` / `PunchCount 0` / `UpdateCalls 0`"
                + "（现得 \"" + h.TextForTest + "\" / " + h.PunchCount + " / " + h.UpdateCalls + "）"
                + " —— 🧨 在 `Awake`/`Start` 里自己调一次 `UpdateValue`（「顺手初始化」）⇒ 计数先涨 ⇒ 红");

            h.UpdateValue(2);
            h.UpdateValue(2);   // 同一个值再来一次
            Check(h.PunchCount == 2 && h.UpdateCalls == 2 && h.TextForTest == "2",
                  "灭自证 1b·**同一个值连调两次 ⇒ 弹两次**（`PunchCount 2 / UpdateCalls 2`；现得 "
                + h.PunchCount + " / " + h.UpdateCalls + "）"
                + " —— 🧨 把 `if (newValue == LastValue) return;` 塞进控制器（防抖放错层）⇒ 红；"
                + "防抖属 W8 的 `TraitFrames` 帧锁，⛔ 不在本层");
        }
        finally { Kill(root); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 灭自证 2 · `UpdateValue` **只改 UI**（判据 §3.3：输出只有 UI，不写引擎状态、不改别的）
    //   结构上不可能同时满足：把「顺手改样式 / 直接把 scale 改了 / 加组件 / 挪节点 / 改 activeSelf」
    //   写进 `UpdateValue`，快照就会变 ⇒ 红。反之删掉写文本那一句 ⇒ 上面 A-E1 红。
    //   ⚠️ 本层**没有引擎句柄**（这正是判据 §3.3 的结论）⇒ 「引擎状态不变」在这里的可操作形式 =
    //      「假 GO 的可观测状态一个都没动」。W7/W8 接上 `BattleDriver` 之后，这条要升级成
    //      「`UpdateValue` 前后 **局面哈希不变**」（见文件末「待接线」）。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_AntiSelfProof2()
    {
        GameObject root = null;
        try
        {
            root = NewRoot("HuntMark_IdleEffect");
            TextMeshPro counter = TmpFont.NewText(root.transform, "Counter", "1", 6f, Color.white);
            GameObject sibling = NewChild(root.transform, "Trait Icon");     // 兄弟节点：不该被动到
            var h = TraitEffectControllers.Attach(root, "HuntMark_IdleEffect") as HuntMarkController;
            if (h == null || counter == null) { Check(false, "灭自证2·`Attach`/TMP 没造出来 ⇒ 本节没验到"); return; }

            string before = Snapshot(root);
            int counterId = counter.GetInstanceID();
            int siblingId = sibling.GetInstanceID();

            h.UpdateValue(9);

            string after = Snapshot(root);
            Check(h.TextForTest == "9",
                  "灭自证 2(前提)·`text` 确实变成了 `\"9\"`（现得 \"" + h.TextForTest + "\"）"
                + " —— 这条不成立的话下面那条「其余没动」没有意义");
            Check(before == after,
                  "灭自证 2·除 `counter.text` 外**可观测状态逐项不变**"
                + "（层级 / 名字 / 兄弟序 / 本地位置 / 本地旋转 / 本地缩放 / activeSelf / layer / 组件数"
                + " / TMP 字号·颜色·对齐·enabled·字体）"
                + " —— 🧨 顺手改样式、直接写 `localScale` 冒充 punch、`AddComponent`、挪节点、开关别的状态 ⇒ 红");
            Check(counter.GetInstanceID() == counterId && sibling != null && sibling.GetInstanceID() == siblingId,
                  "灭自证 2·`Counter` 与兄弟节点**都没被换掉/销毁**（`InstanceID` 不变）"
                + " —— 🧨 重建子节点冒充刷新 ⇒ 红");
        }
        finally { Kill(root); }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // ⏳ 待接线（W5 / W7 / W8 / P0-1）之后才能写、也必须补的断言 —— **现在写不了**（相应 API 还不存在，
    //    写进来会编译不过）。⛔ 别用「跳过」的假断言占位。
    //
    //   · A-E3 的原版形：`TraitFrames.PlayStartRows` / `DisplayTrigger` 里**即使实例上没有控制器**，
    //     也必须调一次 `UpdateValue`（判据 §6.4 A-E3）⇒ 观测量在 **`TraitFrames`** 那侧（`UpdateCalls`）。
    //   · A-E2 的「`InstanceId` 不变 / `PlayCount` 不增」：断的是 **`TraitFrames._play` 里那个 `WarpforgeEffectPlayer`**。
    //   · A-LIFE：`HuntMark_IdleEffect` / `MarkerlightIdle` 实例化 **>6 s 仍 `!= null`**
    //     （挡 `AutoLifetime` 回落到 `natural` 4.3 s / 4 s —— 判据 §6.0 P0-1、§七 F1）。
    //   · 灭自证 1 的**原版原话**：同一帧内把 `SyncKeywords` / `SyncBoard` 连跑 3 次（值不变）
    //     ⇒ `PunchCount` **仍为 1**（值 = `TraitFrames` 的帧锁，属 W8；判据 §6.4）。
    //   · 「`UpdateValue` 不改引擎状态」的**强形**：调前/调后 **局面哈希**（`BattleContext` 的局面摘要）相等
    //     （W7 接上 `BattleDriver` 之后写）。
    //   · W9 / W10（`MarkerLightUsed` 的引擎事件、`AimingAttack` + `MaterialFader`）接线后的断言。
    // ════════════════════════════════════════════════════════════════════════════

    // ── 工具 ────────────────────────────────────────────────────────────────────
    static void Safe(string label, Action body)
    {
        try { body(); }
        catch (Exception ex)
        {
            Check(false, label + "·断言过程中抛异常（该组后面的子项可能**没验到**）：" + ex.Message);
            Debug.LogException(ex);
        }
    }

    static void Check(bool ok, string msg)
    {
        if (ok) { Passed++; Debug.Log(P + "✓ " + msg); }
        else { Failed++; Debug.LogError(P + "✗ " + msg); }
    }

    static void Summary()
    {
        string line = P + "=== 断言合计：" + Passed + " 通过 / " + Failed + " 失败 ===";
        if (Failed == 0) Debug.Log(line);
        else Debug.LogError(line + "（失败条目见上面每一条 `" + P + "✗`）");
    }

    static GameObject NewRoot(string name)
    {
        var go = new GameObject(name);
        go.hideFlags = HideFlags.DontSave;   // 不进场景存盘（本模块随时可跑）
        return go;
    }

    static GameObject NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.hideFlags = HideFlags.DontSave;
        return go;
    }

    static Transform EnsurePath(Transform root, string path)
    {
        Transform cur = root;
        string[] segs = path.Split('/');
        for (int i = 0; i < segs.Length; i++)
        {
            Transform n = cur.Find(segs[i]);
            if (n == null) n = NewChild(cur, segs[i]).transform;
            cur = n;
        }
        return cur;
    }

    static ParticleSystemRenderer NewRenderer(Transform parent, string name)
    {
        if (parent == null) return null;
        GameObject go = NewChild(parent, name);
        var ps = go.AddComponent<ParticleSystem>();      // 连带挂 `ParticleSystemRenderer`（RequireComponent）
        if (ps != null) ps.playOnAwake = false;
        return go.GetComponent<ParticleSystemRenderer>();
    }

    static void Kill(GameObject go)
    {
        if (go == null) return;
        UnityEngine.Object.DestroyImmediate(go);   // 批处理没有帧循环；补间靠 `CardTween.Use` 的 `SetLink` 随销毁回收
    }

    // 反射调**任意可见性**的方法（编辑模式下 `Start` 不会自己跑，见文件头 ②）
    static bool InvokeVoid(object target, string method)
    {
        if (target == null) return false;
        Type t = target.GetType();
        while (t != null && t != typeof(object))
        {
            MethodInfo m = t.GetMethod(method,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (m != null && m.GetParameters().Length == 0)
            {
                m.Invoke(target, null);
                return true;
            }
            t = t.BaseType;
        }
        return false;
    }

    // ── 日志捕获（断「必须出声」）──────────────────────────────────────────────
    static List<string> _errs;
    static void LogHook(string msg, string stack, LogType type)
    {
        if (_errs == null) return;
        if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) _errs.Add(msg ?? "");
    }
    static List<string> CaptureStart()
    {
        _errs = new List<string>();
        Application.logMessageReceived += LogHook;
        return _errs;
    }
    static void CaptureStop()
    {
        Application.logMessageReceived -= LogHook;
        _errs = null;
    }
    static bool HasError(List<string> errs, string needle)
    {
        if (errs == null) return false;
        for (int i = 0; i < errs.Count; i++)
            if (errs[i] != null && errs[i].IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    // ── 状态快照（灭自证 2）：**故意不含 `text`** —— 那一项由单独的 Check 断 ────────────
    static string Snapshot(GameObject root)
    {
        var sb = new StringBuilder();
        Transform[] trs = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < trs.Length; i++)
        {
            Transform t = trs[i];
            sb.Append(t.name).Append('|').Append(t.GetSiblingIndex()).Append('|')
              .Append(V3(t.localPosition)).Append('|').Append(V3(t.localEulerAngles)).Append('|')
              .Append(V3(t.localScale)).Append('|').Append(t.gameObject.activeSelf ? '1' : '0').Append('|')
              .Append(t.gameObject.layer).Append('|')
              .Append(t.GetComponents<Component>().Length).Append('\n');
        }
        TMP_Text[] tx = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tx.Length; i++)
        {
            TMP_Text x = tx[i];
            sb.Append("TMP|").Append(x.fontSize.ToString("F3")).Append('|')
              .Append(ColorUtility.ToHtmlStringRGBA(x.color)).Append('|')
              .Append(x.alignment).Append('|').Append(x.enabled ? '1' : '0').Append('|')
              .Append(x.font != null ? x.font.GetInstanceID() : 0).Append('|')
              .Append(x.raycastTarget ? '1' : '0').Append('\n');
        }
        return sb.ToString();
    }
    static string V3(Vector3 v)
    {
        return v.x.ToString("F3") + "," + v.y.ToString("F3") + "," + v.z.ToString("F3");
    }

    static bool Contains(Renderer[] arr, Renderer r)
    {
        if (arr == null || r == null) return false;
        for (int i = 0; i < arr.Length; i++) if (ReferenceEquals(arr[i], r)) return true;
        return false;
    }
    static bool SameSet(Renderer[] arr, Renderer[] expect)
    {
        if (arr == null || expect == null || arr.Length != expect.Length) return false;
        for (int i = 0; i < expect.Length; i++) if (!Contains(arr, expect[i])) return false;
        return true;
    }
    static string TypeName(object o) { return o == null ? "<null>" : o.GetType().Name; }
}
