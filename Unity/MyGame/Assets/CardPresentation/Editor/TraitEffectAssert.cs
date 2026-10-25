// TraitEffectAssert.cs — `N15` + `E5` · `TraitEffectController` 钩子层的断言（**13 组**：8 组控制器层 + 5 组接线端到端，含 3 条灭自证）
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
//
// 🆕 **2026-10-25（`E5` · 追加轮：W0–W8 接线后的端到端那 5 组 `A-W*`）**
//   ① 前 8 组（A-E1…灭自证 2）**只用假 GO**；新增的 5 组 `A-W1 / A-W2 / A-W3W4 / A-W8 / A-W8灭自证`
//      **要用真 prefab + 真效果库** —— 因为 W0–W8 的行为本身就是「**真的实例化出一张框**、再往框上的
//      计数器写数」。它们**仍然不建场景、不碰 `BattleDriver`**：只 `new GameObject` + `TraitFrames.Attach`。
//      ⚠️ 效果库取不到那件 prefab ⇒ 会**显式报「没验到」**（`(前提 …)` 那一条红），⛔ **不会**报通过。
//   ② 判据（原版）→ `BattleManager__CreateAnim.c:62-81`（出生那拍调 `UpdateValue`）·
//      `BattleCardUI__DisplayStatusAnim.c:86-119 / :126-198`（Start 档每次都刷 / 已有框只归位+刷）·
//      `…__DisplayTriggerAnim.c:97-104`（触发档也刷）· `HuntMarkController__UpdateValue.c:13-30`（写文本+弹，**无同值早退**）。
//   ③ `RefreshAll` 里 **`PlaySignals()` 必须排在 `SyncBoard()` 之前**（W7 先刷、W8 兜底后判 ⇒ 不双弹）——
//      这条**不是**本文件的断言能盖住的（它要 `BattleDriver`），所以在 `BattleDriver.RefreshAll` 里写了禁改注释。
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
        Debug.Log(P + "=== TraitEffectController 钩子层断言（N15 的 8 组 + E5 追加的 5 组端到端）开始 ===");

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

            // 🆕 **2026-10-25（`E5` · 追加轮）**：W0–W8 接线后的**端到端**那 5 组（真 prefab + 真效果库）
            Safe("A-W1", Block_W1_FirstSpawn);
            Safe("A-W2", Block_W2_Reshow);
            Safe("A-W3W4", Block_W3W4_StartAndTrigger);
            Safe("A-W8", Block_W8_ValueFallback);
            Safe("A-W8灭自证", Block_W8_AntiSelfProof);
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
    //  🆕 2026-10-25（`E5` · 追加轮）：**W0–W8 接线后的端到端**（真 prefab + 真效果库）
    //
    //  与上面 8 组的区别：那 8 组只碰 `TraitEffectController` 自己（假 GO + 假 TMP，不依赖效果库）；
    //  这 5 组走**生产那条链** `TraitFrames.DisplayStatus / DisplayTrigger / SyncKeywords`
    //  ⇒ 真的从 `WarpforgeEffectLibrary` 实例化出 prefab、真的 `AddComponent` 出控制器。
    //  ⛔ 仍然**不建场景、不碰 `BattleDriver`**（所以「W7 的调用点」那一半不在这里，见 A-W2 的说明）。
    //
    //  ⚠️ **数据前提（现读 `Resources/trait_effect_bindings.json`，期望值都是从这儿推出来的）**：
    //     `huntMark` = Start(0)→`HuntMark_StackEffect`（**无控制器**）·
    //                  OnPlay(2)→`HuntMark_IdleEffect`（**有控制器**）·
    //                  Trigger(6)→`HuntMark_StartEffect`（无控制器）
    //     `armour`   = **只有** Start(0)→`ArmourEffect`（无控制器）
    //     `oath`     = **只有** Trigger(3)→`Oath_Effect`（无控制器）
    //  ⇒ 「全库只有 `huntMark` / `markerlight` 那两张 OnPlay 框带控制器」这句话，在这 5 组里是**可观测**的。
    //  ⚠️ 四件 prefab 现读**都在** `Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 里；
    //     万一将来取不到，`(前提 …)` 那一条会**显式红并说「没验到」**，⛔ 不会假绿。
    // ════════════════════════════════════════════════════════════════════════════

    // ────────────────────────────────────────────────────────────────────────────
    // A-W1 · **W0 + W1**：框出生这一拍就挂控制器 + 把值写上去
    //   期望值依据：
    //     · 挂载 = 判据 §6.2（`TraitFrames.Play` 是**唯一**出生口；按 prefab 名挂，⛔ 不按 trait 名）
    //     · `UpdateValue` 在 `Instantiate` **那一拍**就调 = `BattleManager__CreateAnim.c:62-81`
    //     · 写文本 + 弹一次、且**没有同值早退** = `HuntMarkController__UpdateValue.c:13-30`
    //     · `UpdateValueCalls == 2` = Start(0) 那条（`…DisplayStatusAnim.c:86-119`）**加**
    //       OnPlay(2) 那条（`:121-198`）—— **两条都发生了调用**，只有后一条真的弹
    //   改坏法：删掉 `TraitFrames.Play` 里那句 `TraitEffectControllers.Attach` ⇒ `AttachedCount 0` / 控制器 null ⇒ 红；
    //           删掉 `DisplayStatus` **首次**那支的 `RefreshCounter` ⇒ 文本停在 prefab 写的 `"1"`、`UpdateValueCalls 1` ⇒ 红。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_W1_FirstSpawn()
    {
        GameObject host = null;
        try
        {
            host = NewRoot("TraitFramesHost");
            var tf = TraitFrames.Attach(host.transform);
            Check(tf != null && TraitFrames.BindingsLoaded,
                  "(前提 A-W1) `TraitFrames` 挂得上 且 绑定表载得进来（`BindingsProblem` = "
                + (TraitFrames.BindingsProblem ?? "null") + "）—— 载不进 ⇒ 本节**没验到**，⛔ 别读成通过");
            if (tf == null || !TraitFrames.BindingsLoaded) return;

            bool created = tf.DisplayStatus("huntmark", 2);
            var h = HuntOn(host);
            Check(created && tf.PlayCount == 1,
                  "A-W1·首次 `DisplayStatus(\"huntmark\", 2)` ⇒ **新挂了一张框**（返回 " + created
                + " / `PlayCount` " + tf.PlayCount + "；原版 `DisplayStatusAnim.c:121-198` 字典里没有才播）"
                + " —— 🧨 效果库取不到 `HuntMark_IdleEffect` ⇒ 这条红且本节**没验到**（看上面有没有 `[TraitFrames]` 警告）");
            Check(h != null && tf.AttachedCount == 1,
                  "A-W1·**W0**：框上挂出了 `HuntMarkController`（现得 " + TypeName(h)
                + "；`AttachedCount` = " + tf.AttachedCount + "）"
                + " —— 🧨 删掉 `TraitFrames.Play` 里那句 `TraitEffectControllers.Attach` ⇒ 红（判据 §6.2）");
            if (h == null) return;
            Check(h.TextForTest == "2" && h.PunchCount == 1 && h.LastValue == 2 && h.UpdateCalls == 1,
                  "A-W1·**W1**：出生那拍就写上了值（文本 \"" + (h.TextForTest ?? "null") + "\" / `PunchCount` "
                + h.PunchCount + " / `LastValue` " + h.LastValue + " / 控制器侧 `UpdateCalls` " + h.UpdateCalls
                + "；期望 2 / 1 / 2 / 1，依据 `CreateAnim.c:62-81` + `UpdateValue.c:13-30`）"
                + " —— 🧨 删掉首次那支的 `RefreshCounter` ⇒ 文本停在 prefab 写的 \"1\"、不弹 ⇒ 红");
            Check(tf.UpdateValueCalls == 2 && tf.LastTraitValue == 2,
                  "A-W1·`TraitFrames` 侧记账 = **2 次调用**（Start(0) 那条空转 + OnPlay(2) 那条真的写）"
                + "（现得 " + tf.UpdateValueCalls + "；`LastTraitValue` = " + tf.LastTraitValue + "）"
                + " —— 🧨 把记账挪到「实例上有控制器才记」⇒ 得 1 ⇒ 红（A-E3 要的就是「调用必须发生」）");
        }
        finally { Kill(host); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-W2 · **W2**：框已在字典里 ⇒ 只归位 + 激活 + **再刷一次计数器**（⛔ 不重建、不重播）
    //   期望值依据：
    //     · 已有 ⇒ 只归位/SetActive/不重播 = `BattleCardUI__DisplayStatusAnim.c:126-198`
    //     · 那一支里的 `UpdateValue(GetCurrentTraitValueWithModifiers(trait))` = `…:190-196`
    //     · `UpdateValueCalls` 每次显示 **+2**（Start(0) 空转 + OnPlay 真的写；数据见本节顶部的行表）
    //   改坏法：删掉「已在字典」那支的 `RefreshCounter` ⇒ 文本停在上一回的值、`PunchCount` 不涨 ⇒ 红；
    //           把那一支改成「先销毁再重建」⇒ 控制器 `InstanceID` 变 ⇒ 红。
    //   📌 **本组只覆盖 W2 那一半**：W7 的**调用点**在 `BattleDriver.PlaySignal` 的 `EvtKind.StatusApplied` 支
    //      （那一行是 `DisplayStatus(e.CardId, StatusValueOf(e.Player, e.Slot, e.CardId))`）——
    //      它的端到端要由 `Editor/BattleScene.cs` 那种**带 `BattleDriver`** 的宿主补，⛔ 别把本节当成「W7 已验」。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_W2_Reshow()
    {
        GameObject host = null;
        try
        {
            host = NewRoot("TraitFramesHost");
            var tf = TraitFrames.Attach(host.transform);
            if (tf == null || !TraitFrames.BindingsLoaded)
            { Check(false, "(前提 A-W2) `TraitFrames`/绑定表没就绪 ⇒ 本节**没验到**（`BindingsProblem` = "
                         + (TraitFrames.BindingsProblem ?? "null") + "）"); return; }

            bool c1 = tf.DisplayStatus("huntmark", 2);
            var h1 = HuntOn(host);
            if (h1 == null || !c1)
            { Check(false, "(前提 A-W2) 第一张框没实例化出来（效果库缺 `HuntMark_IdleEffect`?）⇒ 本节**没验到**"); return; }

            int ctlId = h1.GetInstanceID();
            int p0 = h1.PunchCount;              // 期望 1
            int calls0 = tf.UpdateValueCalls;    // 期望 2
            int play0 = tf.PlayCount;            // 期望 1

            bool c2 = tf.DisplayStatus("huntmark", 3);
            var h2 = HuntOn(host);
            Check(!c2,
                  "A-W2·再次显示 **不新挂框**（`DisplayStatus` 返回 " + c2 + "）"
                + "（原版 `DisplayStatusAnim.c:121-198`：「已有 ⇒ 只归位、不重播」）"
                + " —— 🧨 每次显示都重播一张新框 ⇒ 红");
            Check(h2 != null && h2.GetInstanceID() == ctlId && tf.PlayCount == play0,
                  "A-W2·**实例一个都没换**（控制器 `InstanceID` "
                + (h2 == null ? "<null>" : (h2.GetInstanceID() == ctlId ? "不变" : "**变了**"))
                + " / `PlayCount` " + play0 + " → " + tf.PlayCount + "）"
                + " —— 🧨 用「销毁旧框重建新框」冒充刷新 ⇒ 红");
            Check(h2 != null && h2.TextForTest == "3" && h2.PunchCount == p0 + 1 && h2.LastValue == 3,
                  "A-W2·**W2**：那一支**再刷了一次**（文本 \"" + (h2 != null ? h2.TextForTest : "-") + "\" / `PunchCount` "
                + p0 + " → " + (h2 != null ? h2.PunchCount : -1) + "；期望文本 3、弹 2 次）"
                + "（依据 `DisplayStatusAnim.c:190-196`）—— 🧨 删掉那一支的 `RefreshCounter` ⇒ 红");
            Check(tf.UpdateValueCalls == calls0 + 2,
                  "A-W2·`TraitFrames` 侧再记 **2 次**调用（Start(0) 空转 + OnPlay 那一支真的写）"
                + "（" + calls0 + " → " + tf.UpdateValueCalls + "）"
                + " —— 🧨 漏掉 Start(0) 那一条 ⇒ 只 +1 ⇒ 红（判据 §6.4 A-E3）");
        }
        finally { Kill(host); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-W3W4 · **W3 + W4**：Start(0) 档与触发档**也要发调用**，即使那件 prefab 上没有控制器
    //   期望值依据（数据见本节顶部的行表）：
    //     · `armour` 只有 Start(0)→`ArmourEffect`（无控制器）⇒ `UpdateValueCalls` **恰好 +1**，一个都不弹
    //     · `oath` 只有 Trigger(3)→`Oath_Effect`（无控制器）⇒ `UpdateValueCalls` **恰好 +1**，一个都不弹
    //     · 「`traitValue` 一起传下去」= `…DisplayStatusAnim.c:86-119` / `…DisplayTriggerAnim.c:97-104`
    //     · 「这一档的 prefab 本来就不带控制器」= 判据 §1.3 的全库唯一性（只有那两个 idle prefab 带）
    //   改坏法：删掉 `PlayStartRows` 里的 `RefreshCounter` ⇒ `armour` 那段 +0 ⇒ 红；
    //           删掉 `DisplayTrigger` 里的 `RefreshCounter` ⇒ `oath` 那段 +0 ⇒ 红。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_W3W4_StartAndTrigger()
    {
        GameObject host = null;
        try
        {
            host = NewRoot("TraitFramesHost");
            var tf = TraitFrames.Attach(host.transform);
            if (tf == null || !TraitFrames.BindingsLoaded)
            { Check(false, "(前提 A-W3W4) `TraitFrames`/绑定表没就绪 ⇒ 本节**没验到**"); return; }

            int u0 = tf.UpdateValueCalls;
            tf.DisplayStatus("armour", 3);                       // Start(0) 档 → `ArmourEffect`（无控制器）
            int u1 = tf.UpdateValueCalls;
            Check(u1 == u0 + 1 && host.GetComponentInChildren<TraitEffectController>(true) == null,
                  "A-W3·`DisplayStatus(\"armour\", 3)` ⇒ `UpdateValueCalls` **+1**（" + u0 + " → " + u1 + "），"
                + "而实例上**没有控制器**（所以一次都不弹）"
                + "（依据 `DisplayStatusAnim.c:86-119`：Start(0) 这一档同样把 `traitValue` 传下去；"
                + "「这一档 prefab 不带控制器」= 判据 §1.3）"
                + " —— 🧨 删掉 `PlayStartRows` 里的 `RefreshCounter` ⇒ 得 +0 ⇒ 红；"
                + "🧨 取不到 `ArmourEffect` ⇒ 也是 +0 ⇒ **没验到**（看有没有 `[TraitFrames]` 警告）");

            int u2 = tf.UpdateValueCalls;
            bool played = tf.DisplayTrigger("oath", false, 1);   // Trigger(3) 档 → `Oath_Effect`（无控制器）
            int u3 = tf.UpdateValueCalls;
            Check(played && u3 == u2 + 1,
                  "A-W4·`DisplayTrigger(\"oath\", false, 1)` ⇒ 播了（" + played + "）且 `UpdateValueCalls` **+1**（"
                + u2 + " → " + u3 + "）"
                + "（依据 `DisplayTriggerAnim.c:97-104`）—— 🧨 删掉 `DisplayTrigger` 里的 `RefreshCounter` ⇒ 得 +0 ⇒ 红");

            Check(tf.PlayCount == 0,
                  "A-W3W4·这两个 trait 都**没有 OnPlay(2)** 那条 ⇒ 一张持久框都不该建（现得 `PlayCount` "
                + tf.PlayCount + "）—— 🧨 把 Start(0)/Trigger(3) 也塞进 `_play` 字典 ⇒ 红");
        }
        finally { Kill(host); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-W8 · **W8**：值变化兜底 —— **值变了才刷、值没变一个都不刷**
    //   期望值依据：
    //     · 兜底闸 = 判据 §6.3 末段（原版还有「modifier 变化」那条路，我们没有对应事件 ⇒ 靠每轮同步对差）
    //     · 第一次 `SyncKeywords` 走的是**「新出现」**那条路（W1），不是兜底 ⇒ 弹一次
    //     · 同值再来 ⇒ 兜底判「没变」⇒ **+0**（⛔ 否则 `DOPunchScale` 会让计数器一直抖）
    //     · 值 3 → 5 ⇒ 兜底刷 **1 次**，文本变 `"5"`
    //   改坏法：把 `RefreshChangedValues` 里的 `onlyIfChanged` 改成 `false` ⇒ 同值那两次也刷 ⇒ 红；
    //           删掉 `SyncKeywords` 里对 `RefreshChangedValues` 的调用 ⇒ 3→5 那一次不刷 ⇒ 红。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_W8_ValueFallback()
    {
        GameObject host = null;
        try
        {
            host = NewRoot("TraitFramesHost");
            var tf = TraitFrames.Attach(host.transform);
            if (tf == null || !TraitFrames.BindingsLoaded)
            { Check(false, "(前提 A-W8) `TraitFrames`/绑定表没就绪 ⇒ 本节**没验到**"); return; }

            int added = tf.SyncKeywords(new string[] { "huntmark" }, k => 3);   // 「新出现」⇒ 建框 + 显式刷
            var h = HuntOn(host);
            if (h == null || added != 1)
            { Check(false, "(前提 A-W8) 第一张框没实例化出来 / `added` 不是 1（现得 " + added + "）⇒ 本节**没验到**"); return; }

            int p1 = h.PunchCount, u1 = tf.UpdateValueCalls;

            tf.SyncKeywords(new string[] { "huntmark" }, k => 3);               // 值没变 ⇒ 兜底不刷
            int p2 = h.PunchCount, u2 = tf.UpdateValueCalls;
            Check(p2 == p1 && u2 == u1,
                  "A-W8·**值没变 ⇒ 兜底一个都不刷**（`PunchCount` " + p1 + " → " + p2
                + " / `UpdateValueCalls` " + u1 + " → " + u2 + "）"
                + "（判据 §6.3 末段：⛔ 不能变成「每帧刷」，否则计数器一直抖）"
                + " —— 🧨 兜底那一路不加去重 ⇒ 每轮同步都弹 ⇒ 红");

            tf.SyncKeywords(new string[] { "huntmark" }, k => 5);               // 值 3 → 5 ⇒ 兜底刷 1 次
            Check(h.PunchCount == p1 + 1 && h.TextForTest == "5" && h.LastValue == 5,
                  "A-W8·**值变了（3 → 5）⇒ 兜底刷 1 次**（`PunchCount` " + p1 + " → " + h.PunchCount
                + " / 文本 \"" + (h.TextForTest ?? "null") + "\"）"
                + "（依据：`UpdateValue` 写的是「当前总值」= `GetCurrentTraitValueWithModifiers`）"
                + " —— 🧨 删掉 `SyncKeywords` 里对 `RefreshChangedValues` 的调用 ⇒ 不刷 ⇒ 红");
        }
        finally { Kill(host); }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // A-W8灭自证 · 判据 §6.4 灭自证 1 的**原话**：同一帧内连跑 3 次同步（值不变）⇒ `PunchCount` **仍为 1**
    //   为什么这条必须单独存在：它挡的是「每次 sync 都无脑刷新」那个**看着对、实际每帧抖**的实现
    //   —— 上面 A-W8 只验「两次之间」，这条验「连跑三次」的累积效应。
    //   改坏法：把 `RefreshChangedValues` 的 `onlyIfChanged` 改成 `false` ⇒ 掉到 4 ⇒ 红；
    //           把 `_prevValue` 的写在「没控制器」那条路上也做（空转也会写脏）⇒ 第一次显式刷之后兜底会误判 ⇒ 红。
    // ────────────────────────────────────────────────────────────────────────────
    static void Block_W8_AntiSelfProof()
    {
        GameObject host = null;
        try
        {
            host = NewRoot("TraitFramesHost");
            var tf = TraitFrames.Attach(host.transform);
            if (tf == null || !TraitFrames.BindingsLoaded)
            { Check(false, "(前提 A-W8灭自证) `TraitFrames`/绑定表没就绪 ⇒ 本节**没验到**"); return; }

            tf.SyncKeywords(new string[] { "huntmark" }, k => 3);       // 第 1 次：「新出现」那条路 ⇒ 弹 1 次
            var h = HuntOn(host);
            if (h == null)
            { Check(false, "(前提 A-W8灭自证) 第一张框没实例化出来 ⇒ 本节**没验到**"); return; }
            int p0 = h.PunchCount;                                     // 期望 1

            for (int i = 0; i < 3; i++)
                tf.SyncKeywords(new string[] { "huntmark" }, k => 3);    // 同一帧连跑 3 次，值不变

            Check(h.PunchCount == p0 && p0 == 1,
                  "A-W8灭自证·**同一帧内连跑 3 次 `SyncKeywords`（值不变）⇒ `PunchCount` 仍为 1**"
                + "（现得 " + h.PunchCount + "，期望 " + p0 + "）"
                + "（判据 §6.4 灭自证 1 的**原话**）"
                + " —— 🧨 兜底那一路「无脑刷」⇒ 变 4 ⇒ 红；🧨 空转（没控制器）那条也写 `_prevValue` ⇒ 会漏掉/多弹 ⇒ 红");
        }
        finally { Kill(host); }
    }

    /// <summary>宿主上挂着的那个 `HuntMarkController`（框是 `TraitFrames` 的 `effectsAnchor` 子件）。</summary>
    static HuntMarkController HuntOn(GameObject host)
    {
        return host != null ? host.GetComponentInChildren<HuntMarkController>(true) : null;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // ⏳ 还是写不了 / 不属于本文件的（⛔ 别用「跳过」的假断言占位）
    //
    //   ✅ **已经补上了（2026-10-25 `E5` · 追加轮）**：
    //      · W0+W1 → `A-W1`；W2 → `A-W2`；W3/W4 → `A-W3W4`；W8 → `A-W8`；W8 灭自证 → `A-W8灭自证`；
    //      · A-E3 的原版形（「实例上没有控制器也要发生调用」）已由 `A-W1 / A-W3W4` 的 `UpdateValueCalls` 覆盖。
    //
    //   ⏳ **仍然没写的**（各有各的原因，⛔ 别硬凑）：
    //      · **A-LIFE**（`HuntMark_IdleEffect` / `MarkerlightIdle` 实例化 **>6 s 仍 `!= null`**）：
    //        批处理**没有帧循环**、`Update()` 不跑 ⇒ 时间型断言在这里**测不了**。要验只能进 player 实测
    //        （判据 §6.0 P0-1 / §七 F1；`TraitFrames.Play` 的 `held` 已把 `destroyTime < 0` 的框设成 ∞）。
    //      · **W7 的端到端**（`EvtKind.StatusApplied` → `DisplayStatus(key, value)`）：
    //        要一个真的 `BattleDriver`（`PlaySignal` 是它的私有实例方法）⇒ 属 `Editor/BattleScene.cs` 那种宿主。
    //      · **「`UpdateValue` 不改引擎状态」的强形**（前后**局面哈希**相等）：同上，要 `BattleContext`。
    //      · **W9 / W10**（`MarkerLightUsed` 的引擎事件、`AimingAttack` + `MaterialFader`）：
    //        两侧都还没接完（缺 `EvtKind` / 缺 `MarkerlightController` 的改动 + 攻击起拍事件）——
    //        见 `资料/完全复刻/报告/E5_TraitEffect接线_第2轮.md` §①。
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
