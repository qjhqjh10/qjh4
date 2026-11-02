// B117_G10_10a_Assert.cs — 战斗域第 10 部分「切出的第一片：**特效主链**」的断言
//   执行代理 `B117-G10-10a` · 2026-11-02
//
// ============================ 它盯的是什么 ============================
// 本片 4 类文件的**行为**（不是「文件在不在」）：
//   ① `CardPresentation/VfxMap.cs`          —— `IsRoutedTriggerName` 白名单的双向锁（名字级）
//   ② `CardPresentation/Core/MaterialFader.cs` —— 淡变的步进/收尾/`Toggle` 取反怪癖
//   ③ `CardPresentation/Battle/AnimFXController.cs` —— `PlaySoundOnTime` 计时门 + 生命周期广播（`Exit` 不防重入）
//   ④ `CardPresentation/Effects/UIEffectToggler*.cs` —— 控制器的同态不重播 / 非运行期瞬间档 / 只推目标槽
//
// ============================ 怎么跑（主对话 · 串行 · 要 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_10a_Assert.Run -logFile -
//   判绿红：日志里 `[B117_G10_10a] === X 过 / Y 红 ===`；宿主聚合行读 `Pass` / `Fail`。
//
// ⚠️ 只建临时 GameObject / 临时材质，结尾 `DestroyImmediate`；**不存盘、不动任何工程资产**。
// ⚠️ 本文件**不改任何既有宿主**（施工单第六节），由调度台决定接到哪个 `Editor/*Scene.cs`。
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B117_G10_10a_Assert
{
    const string P = "[B117_G10_10a] ";
    static int _ok, _bad;

    /// <summary>给宿主聚合行用的读口（否则本条失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static void Section(string s) { Debug.Log(P + "—— " + s); }

    public static void Run()
    {
        _ok = _bad = 0;
        Run1_VfxMapWhitelist();
        Run2_MaterialFader();
        Run3_AnimFXController();
        Run4_Toggler();
        Debug.Log(P + "=== " + _ok + " 过 / " + _bad + " 红 ===");
    }

    // ==================================================================
    //  ① VfxMap：白名单双向锁（名字级）
    // ==================================================================
    //  这里**不重抄** `MustRouteTriggerNames` 的内容 —— 期望值来自被测方的公开自检口
    //  （`SelfCheckRoutedTriggerNames`），⛔ 抄一份表就变成自证。
    //  灭自证靠**反射直调**那两条负控：把 `IsRoutedTriggerName` 改成 `return true;` ⇒ 负控红。
    // ==================================================================
    static void Run1_VfxMapWhitelist()
    {
        Section("① VfxMap 白名单（双向锁 + 大小写三态）");
        try
        {
            var bad = VfxMap.SelfCheckRoutedTriggerNames();
            if (bad == null || bad.Length == 0)
                Check(true, "`SelfCheckRoutedTriggerNames()` 空（NoEmit 的不在白名单 / MustRoute 的都在 / 三态大小写都命中）");
            else
            {
                for (int i = 0; i < bad.Length; i++) Check(false, "白名单违规：" + bad[i]);
            }

            var mi = typeof(VfxMap).GetMethod("IsRoutedTriggerName",
                                              BindingFlags.NonPublic | BindingFlags.Static);
            Check(mi != null, "（前提）取到私有静态 `IsRoutedTriggerName(string)` —— 取不到则下面两条**未执行**");
            if (mi != null)
            {
                Func<string, bool> routed = s => (bool)mi.Invoke(null, new object[] { s });
                // 正控：一条确有发射口的（引擎发小写、原版枚举 PascalCase）
                Check(routed("Mob") && routed("mob") && routed("MOB"),
                      "正控：`Mob` / `mob` / `MOB` 三态都命中（大小写归一没被改回 Ordinal `switch`）");
                // 负控（灭自证）：乱编一个名字必须是 false
                Check(!routed("B117DefinitelyNotATrigger"),
                      "灭自证：乱编的 trigger 名**不**命中（若有人把白名单改成 `return true;` ⇒ 本行红）");
                Check(!routed("") && !routed(null),
                      "空串 / null 都**不**命中（原版没有「空名字也放行」这条路）");
            }
        }
        catch (Exception e) { Check(false, "① 抛异常：" + e.Message); }
    }

    // ==================================================================
    //  ② MaterialFader
    // ==================================================================
    //  判据 → `d:/2/tools/decomp_full/MaterialFader__{Update,Toggle,CacheOriginalMaterials,
    //          RestoreOriginalMaterials}.c`（逐句读；摘录见 `Core/MaterialFader.cs` 文件头）
    //  灭自证：
    //    · 「取反」那条 —— 同一状态连调两次 `Toggle`，`doFade` 必须**翻回 false**；
    //      写成 `doFade = true;` 的实现会让它恒 true ⇒ 红。
    //    · 「跳过 null 条目」那条 —— 原版在这里抛 NRE；我们跳过并出声。断言「不抛 + 其余照淡」，
    //      有人把跳过删掉 ⇒ 抛异常 ⇒ 红。
    // ==================================================================
    static void Run2_MaterialFader()
    {
        Section("② MaterialFader（步进 / 收尾 / Toggle 取反 / null 条目）");
        GameObject go = null, go2 = null, go3 = null;
        try
        {
            go = new GameObject("B117_MF_A");
            var mf = go.AddComponent<MaterialFader>();
            Check(mf != null, "（前提）`MaterialFader` 加得上去 —— 加不上则本节其余**未执行**");
            if (mf == null) return;

            // --- 2.1 没绑 renderer：`Toggle` 不生效（原版这里抛 NRE；我们出声后返回）
            mf.Toggle(false, 0.5f);
            Check(mf.TargetFadeForTest == 0f && !mf.DoFadeForTest,
                  "没绑 `renderers` ⇒ `Toggle` 不写任何状态（`targetFade` 仍 0、`doFade` 仍 false）");

            // --- 2.2 绑一个真 renderer，走「瞬间档」（blendTime = 0）
            go2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go2.name = "B117_MF_Quad";
            var r = go2.GetComponent<Renderer>();
            Check(r != null, "（前提）取到 `Renderer` —— 取不到则 2.2 起**未执行**");
            if (r == null) return;

            mf.BindRenderers(new Renderer[] { r });
            mf.ApplyPrefabFlags(false, false, true);   // = `MarkerlightIdle` 两个 prefab 实例的实读值
            Check(mf.BoundCount == 1 && mf.OriginalMaterialCount == 0,
                  "（前提）绑上 1 条、材质缓存仍是空的（`Toggle` 时才记 `sharedMaterial`）");

            mf.Toggle(false, 0f);                       // 目标 0，时长 0
            Check(mf.BlendTimeForTest == 0f && mf.CurrentBlendTimeForTest == 0f && mf.TargetFadeForTest == 0f,
                  "`Toggle(false, 0)`：`blendTime = currentBlendTime = 0`、`targetFade = 0`");
            Check(mf.DoFadeForTest, "`Toggle` 把 `doFade` 从 false **取反**成 true（原版 `doFade = (doFade == 0)`）");
            Check(mf.OriginalMaterialCount == 1, "第一次 `Toggle` 记下了 `sharedMaterial`（缓存 1 条）");

            float alphaBefore = r.material.color.a;
            mf.SendMessage("Update");                   // `Update` 是 private，编辑模式下靠 SendMessage 推
            Check(mf.UpdateCalls == 1, "（前提）那一帧真的进了淡变逻辑");
            float alphaAfter = r.material.color.a;
            Check(!float.IsNaN(alphaAfter),
                  "灭自证：`blendTime == 0` 那一帧 α 不是 NaN（裸写 `dt / blendTime` 在 dt=0 时是 0/0 = NaN ⇒ 红）");
            Check(Mathf.Abs(alphaAfter) < 1e-5f,
                  $"`blendTime == 0` ⇒ 当帧到位（α {alphaBefore:F3} → {alphaAfter:F3}，应为 0）");
            Check(!r.enabled, "`disableOnFadeOut && targetFade == 0` ⇒ 收尾把 renderer 关掉");
            Check(!mf.DoFadeForTest, "收尾那一帧把 `doFade` 置回 false");

            // --- 2.3 `Toggle` 取反怪癖（灭自证）
            mf.BindRenderers(new Renderer[] { r });
            mf.Toggle(true, 1f);
            bool afterFirst = mf.DoFadeForTest;
            mf.Toggle(true, 1f);
            bool afterSecond = mf.DoFadeForTest;
            Check(afterFirst, "`Toggle(true, 1)` 之后 `doFade == true`");
            Check(!afterSecond,
                  "灭自证：同状态**再** `Toggle(true, 1)` ⇒ `doFade` **翻回 false**（原版取反；"
                  + "写成 `doFade = true;` 的实现这里恒 true ⇒ 红）");
            Check(mf.TargetFadeForTest == 1f, "两次 `Toggle(true, …)` 之后 `targetFade` 仍是 1");

            // --- 2.4 `renderers[i] == null` 不抛（原版抛 NRE，我们跳过 + 出声）
            go3 = new GameObject("B117_MF_B");
            var mf3 = go3.AddComponent<MaterialFader>();
            mf3.BindRenderers(new Renderer[] { null, r });
            bool threw = false;
            try { mf3.Toggle(false, 0f); mf3.SendMessage("Update"); }
            catch (Exception e) { threw = true; Debug.LogWarning(P + "2.4 抛了：" + e.GetType().Name); }
            Check(!threw, "灭自证：`renderers` 里有 null 条目 ⇒ **不抛**（原版在 `Toggle.c:30-32` 抛 NRE；"
                        + "我们把跳过那句删掉就会抛 ⇒ 红）");

            // --- 2.5 还原开关（`restoreOriginalMaterialsOnFadeOut`）
            var mf5go = new GameObject("B117_MF_C");
            var mf5 = mf5go.AddComponent<MaterialFader>();
            var quad5 = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var r5 = quad5.GetComponent<Renderer>();
            mf5.BindRenderers(new Renderer[] { r5 });
            mf5.ApplyPrefabFlags(true, false, true);
            mf5.Toggle(false, 0f);
            mf5.SendMessage("Update");
            Check(mf5.RestoreCalls == 1,
                  "`restoreOriginalMaterialsOnFadeOut = true` ∧ 淡到 0 ⇒ 收尾还原材质一次");

            var mf6 = go.AddComponent<MaterialFader>();   // 同 GO 第二个（本类无 DisallowMultipleComponent）
            if (mf6 != null)
            {
                mf6.BindRenderers(new Renderer[] { r });
                mf6.ApplyPrefabFlags(false, false, true);  // = 实读的 `MarkerlightIdle` 覆写值
                mf6.Toggle(false, 0f);
                mf6.SendMessage("Update");
                Check(mf6.RestoreCalls == 0,
                      "`restoreOriginalMaterialsOnFadeOut = false`（prefab 实读值）⇒ 收尾**不**还原材质");
            }
            if (quad5 != null) UnityEngine.Object.DestroyImmediate(quad5);
            if (mf5go != null) UnityEngine.Object.DestroyImmediate(mf5go);
        }
        catch (Exception e) { Check(false, "② 抛异常：" + e); }
        finally
        {
            if (go3 != null) UnityEngine.Object.DestroyImmediate(go3);
            if (go2 != null) UnityEngine.Object.DestroyImmediate(go2);
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ==================================================================
    //  ③ AnimFXController / PlaySoundOnTime
    // ==================================================================
    //  判据 → `AnimFXController__{OnEnable,Update,Exit,DoDestroy,SetData}.c` +
    //          `PlaySoundOnTime__{Update,Reset}.c`（摘录见 `Battle/AnimFXController.cs` 文件头）
    //  灭自证：
    //    · `Exit()` **不防重入** —— 连调两次 `OnFinished` 必须发两次。有人「顺手修好」加一道闸 ⇒ 红。
    //    · `PlaySoundOnTime` 的门槛是 `k*timeInterval + time`（不是「每帧掷一次」）⇒ 逐拍计数断言。
    // ==================================================================
    static void Run3_AnimFXController()
    {
        Section("③ AnimFXController / PlaySoundOnTime（计时门 + 生命周期广播）");
        GameObject go = null;
        try
        {
            // --- 3.1 字段默认值（原版 ctor 写死的两个数）
            go = new GameObject("B117_AFX");
            var fx = go.AddComponent<AnimFXController>();
            Check(fx != null, "（前提）`AnimFXController` 加得上去 —— 加不上则本节其余**未执行**");
            if (fx == null) return;
            Check(AnimFXController.SAFE_DESTROY_TIME == 3f && fx.exitDestroyTime == 3f,
                  "`SAFE_DESTROY_TIME == 3f` 且 `exitDestroyTime` 初值 = 它（原版 ctor 写 0x40400000）");
            Check(fx.destroyTime == 4f, "`destroyTime` 初值 = 4f（原版 ctor 写 0x40800000）");
            Check(fx.preventDestroy, "有意偏离 ⑤：`preventDestroy` 默认 **true**（运行时 `AddComponent` 会先跑 `OnEnable`）");

            // --- 3.2 PlaySoundOnTime：`repeat=false` ⇒ 总次数 1，且 `time = 0` 当帧就播一次
            var single = new PlaySoundOnTime { time = 0f, sound = null, is2d = true, repeat = false, loops = 9, timeInterval = 0f };
            fx.sounds = new[] { single };
            fx.Tick(1f / 60f);
            Check(single.PlayedCountForTest == 1,
                  "`repeat = false` ⇒ 总次数是 **1**（`loops = 9` 不起作用）：第一帧播一次");
            fx.Tick(10f);
            Check(single.PlayedCountForTest == 1, "第二帧起不再播（总次数 1 已用完）");

            // --- 3.3 门槛式（`k * timeInterval + time`）
            var rep = new PlaySoundOnTime { time = 0.5f, sound = null, is2d = true, repeat = true, loops = 3, timeInterval = 1f };
            fx.sounds = new[] { rep };
            var fx3 = new GameObject("B117_AFX3").AddComponent<AnimFXController>();
            fx3.sounds = new[] { rep };
            fx3.Tick(0.4f);
            Check(rep.PlayedCountForTest == 0, "`time = 0.5` / 累计 0.4 ⇒ **还没到**门槛（0 次）");
            fx3.Tick(0.1f);
            Check(rep.PlayedCountForTest == 1, "累计 0.5 ⇒ 第 1 次（门槛 `0*1 + 0.5`）");
            fx3.Tick(0.9f);
            Check(rep.PlayedCountForTest == 1, "累计 1.4 < 1.5 ⇒ 仍 1 次（门槛不是「每帧一次」）");
            fx3.Tick(0.1f);
            Check(rep.PlayedCountForTest == 2, "累计 1.5 ⇒ 第 2 次（门槛 `1*1 + 0.5`）");
            fx3.Tick(5f);
            Check(rep.PlayedCountForTest == 3, "跳过后追上第 3 次（门槛 `2*1 + 0.5`，且 `loops = 3` 是**总次数**）");
            fx3.Tick(5f);
            Check(rep.PlayedCountForTest == 3, "用完 3 次后彻底停（`total <= 已播 ⇒ return`）");
            rep.Reset();
            Check(rep.PlayedCountForTest == 0, "`Reset()` 只把计数归零（`PlaySoundOnTime__Reset.c` 只有一句）");
            UnityEngine.Object.DestroyImmediate(fx3.gameObject);

            // --- 3.4 `exitSounds` 只在 `exiting` 之后推
            var exitSlot = new PlaySoundOnTime { time = 0f, sound = null, is2d = true, repeat = false, loops = 1, timeInterval = 0f };
            var fx4 = new GameObject("B117_AFX4").AddComponent<AnimFXController>();
            fx4.sounds = new PlaySoundOnTime[0];
            fx4.exitSounds = new[] { exitSlot };
            fx4.Tick(1f);
            Check(exitSlot.PlayedCountForTest == 0, "`exiting == false` 时 `exitSounds` **不推**（原版 `Update` 的第二段有闸）");

            int finished = 0;
            fx4.OnFinished = () => finished++;
            fx4.Exit();
            Check(finished == 1, "`Exit()` 开头发一次 `OnFinished`");
            fx4.Tick(1f);
            Check(exitSlot.PlayedCountForTest == 1, "`Exit()` 之后 `exitSounds` 开始推（`exiting = true`）");
            fx4.Exit();
            // 灭自证：原版**不置空、也不防重入** ⇒ 第二次 `Exit()` 再发一次
            Check(finished == 2,
                  "灭自证：`Exit()` **不防重入** ⇒ 连调两次 `OnFinished` 发两次（有人加一道闸 ⇒ 红）");

            // --- 3.5 三个生命周期广播（`SetData` / `Exit` / `DoDestroy`）
            var fx5 = new GameObject("B117_AFX5").AddComponent<AnimFXController>();
            var stub = fx5.gameObject.AddComponent<B117StubModule>();
            Check(stub != null, "（前提）把测试模块挂上同一 GameObject —— 挂不上则 3.5 余下**未执行**");
            if (stub != null)
            {
                fx5.modules = new List<WarpforgeVFX.WFSceneModule> { stub };
                var acting = new GameObject("B117_Acting").transform;
                var target = new GameObject("B117_Target").transform;
                try
                {
                    fx5.SetData(acting, target);
                    Check(stub.InitCount == 1, "`SetData` ⇒ 逐模块 `Initialize(this)` 一次");
                    Check(ReferenceEquals(stub.Controller, fx5),
                          "`Initialize` 把本控制器注入模块（`WFSceneModule.Controller`）");
                    Check(ReferenceEquals(fx5.ActingCard, acting) && ReferenceEquals(fx5.TargetCard, target),
                          "`SetData` 先填 `actingCard` / `targetCard` 两个字段");

                    fx5.Exit();
                    Check(stub.ExitCount == 1, "`Exit()` ⇒ 逐模块 `Exit()` 一次");

                    fx5.DoDestroy();
                    Check(stub.DestroyCount == 1, "`DoDestroy()` ⇒ 逐模块 `DoDestroy()` 一次");
                    Check(fx5 != null,
                          "`modules` 非空 ⇒ `DoDestroy` **只广播、不销毁自己**（原版 `bVar1` 那一跳）");
                }
                finally
                {
                    if (acting != null) UnityEngine.Object.DestroyImmediate(acting.gameObject);
                    if (target != null) UnityEngine.Object.DestroyImmediate(target.gameObject);
                    UnityEngine.Object.DestroyImmediate(fx5.gameObject);
                }
            }
            else UnityEngine.Object.DestroyImmediate(fx5.gameObject);
        }
        catch (Exception e) { Check(false, "③ 抛异常：" + e); }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ==================================================================
    //  ④ UIEffectToggler 一族
    // ==================================================================
    //  判据 → `UIEffectTogglerController.cs`（ILSpy 全文）+ `decomp_full/UIEffectTogglerController__{Toggle,Update}.c`
    //          + `UIEffectTogglerPlaySound__{DoUpdate,AnimationFinished}.c`
    //  灭自证：
    //    · 「非运行期瞬间档」—— 删掉 `Toggle` 里那句 `if (!Application.isPlaying) duration = 0f;`，
    //      编辑模式（没有帧循环）`Update` 永远不被调 ⇒ `CompletedCount` 保持 0、`DoUpdate` 一次都不跑 ⇒ 红。
    //    · 「同态不重播」—— 删掉 `_targetStateEnabled == option && !forceChange` 那道闸 ⇒ 子 toggler 被调两次 ⇒ 红。
    // ==================================================================
    static void Run4_Toggler()
    {
        Section("④ UIEffectToggler（控制器瞬间档 / 同态不重播 / PlaySound 只推目标槽）");
        GameObject go = null;
        try
        {
            go = new GameObject("B117_TG");
            var ctl = go.AddComponent<UIEffectTogglerController>();
            Check(ctl != null, "（前提）`UIEffectTogglerController` 加得上去 —— 加不上则本节其余**未执行**");
            if (ctl == null) return;

            var childGo = new GameObject("B117_TG_Child");
            childGo.transform.SetParent(go.transform, false);
            var stub = childGo.AddComponent<B117StubToggler>();
            ctl.Rescan();
            Check(ctl.togglers.Count == 1, "`Rescan()` 收得到子件上的 toggler（`GetComponentsInChildren`）");

            // --- 4.1 非运行期（编辑/批处理）：瞬间档一次跑完
            ctl.Toggle(true, 0.3f);
            Check(ctl.CompletedCount == 1,
                  "灭自证：非运行期 `duration` 被压成 0 ⇒ 瞬间档在一次 `Update` 里跑完（`CompletedCount == 1`）；"
                  + "删掉那句 `!Application.isPlaying ⇒ duration = 0f` ⇒ 没有帧循环 ⇒ 恒 0 ⇒ 红");
            Check(stub.DoUpdateCount == 1 && stub.AnimFinishedCount == 1,
                  "瞬间档：`DoUpdate` 与 `AnimationFinished` 各一次");
            Check(Mathf.Abs(stub.LastN - 1f) < 1e-5f,
                  "瞬间档的归一化时间 `n == 1`（`currentTime = float.MaxValue` ⇒ `Clamp01` 到 1）");
            Check(stub.LastOption, "子 toggler 收到 `option = true`");
            Check(!ctl.enabled, "走完 ⇒ `enabled = false`（不再靠 Unity 每帧调 `Update`）");

            // --- 4.2 同态不重播 / forceChange
            int before = stub.ToggleCount;
            ctl.Toggle(true, 0.3f);
            Check(stub.ToggleCount == before,
                  "灭自证：同态再 `Toggle(true, …)` ⇒ **不重播**（子 toggler 一次都没被调）；"
                  + "把 `_targetStateEnabled == option && !forceChange` 那道闸删掉 ⇒ 红");
            ctl.Toggle(true, 0.3f, true);   // forceChange
            Check(stub.ToggleCount == before + 1, "`forceChange = true` ⇒ 即使同态也广播一次");

            // --- 4.3 UIEffectTogglerPlaySound：只推「目标态那一个槽」
            var sndGo = new GameObject("B117_TG_Snd");
            sndGo.transform.SetParent(go.transform, false);
            var snd = sndGo.AddComponent<UIEffectTogglerPlaySound>();
            var onSlot = new PlaySoundOnTime { time = 0f, sound = null, is2d = true, repeat = false, loops = 1, timeInterval = 0f };
            var offSlot = new PlaySoundOnTime { time = 0f, sound = null, is2d = true, repeat = false, loops = 1, timeInterval = 0f };
            snd.onSound = onSlot; snd.offSound = offSlot;
            Check(snd.Ready, "（前提）两槽都填上 ⇒ `Ready == true`");

            snd.Toggle(true, 0.3f);
            snd.DoUpdate(0f, 0f);
            Check(onSlot.PlayedCountForTest == 1 && offSlot.PlayedCountForTest == 0,
                  "目标是「开」⇒ 只推 `onSound`（`offSound` 一点不动）");
            snd.Toggle(false, 0.3f);
            snd.DoUpdate(0f, 0f);
            Check(offSlot.PlayedCountForTest == 1 && onSlot.PlayedCountForTest == 1,
                  "目标是「关」⇒ 只推 `offSound`（`onSound` 保持上一次的 1）");
            snd.AnimationFinished();
            Check(onSlot.PlayedCountForTest == 0 && offSlot.PlayedCountForTest == 0,
                  "`AnimationFinished()` ⇒ **两个槽都** `Reset()`（漏了它，同一句音效第二次就不会响）");
            UnityEngine.Object.DestroyImmediate(sndGo);
            UnityEngine.Object.DestroyImmediate(childGo);

            // --- 4.4 `UIEffectCanvasGroupToggler`：α 是「原 α × fade」，但 **RGB 取当前值**
            //   判据 = `decomp_full/UIEffectCanvasGroupToggler__DoUpdate.c:31` —— 原版这一句**只有**
            //   `CanvasGroup__set_alpha`，从不写子件的 RGB ⇒ 我们映射到逐件 α 时也不能把 RGB 写回 Capture 时的值。
            //   灭自证：把 `var c = g.color;` 改回 `var c = _graphicBase[i];` ⇒ 下面第一条红。
            var cgGo = new GameObject("B117_TG_CG", typeof(RectTransform));
            var img = cgGo.AddComponent<UnityEngine.UI.Image>();
            if (img == null) Check(false, "（前提）uGUI `Image` 加不上 ⇒ 4.4 未执行");
            else
            {
                img.color = Color.white;
                var cg = cgGo.AddComponent<UIEffectCanvasGroupToggler>();
                cg.Capture();                                  // 记下「白」这一档作为基线
                img.color = new Color(1f, 0f, 0f, 1f);         // 接管之后**别人**改了 RGB
                cg.Toggle(true, 0.3f);
                cg.DoUpdate(0f, 1f);
                Check(img.color.r > 0.99f && img.color.g < 0.01f && img.color.b < 0.01f,
                      "灭自证：`DoUpdate` **不写回** Capture 时的 RGB（红仍是红；写回基线的实现这里会变白 ⇒ 红）");
                Check(Mathf.Abs(img.color.a - 1f) < 1e-3f,
                      "α = 原 α × fade = 1 × 1 = 1（`CanvasGroup.alpha` 是乘子 ⇒ 用「原 α × fade」等价实现）");
            }
            UnityEngine.Object.DestroyImmediate(cgGo);
        }
        catch (Exception e) { Check(false, "④ 抛异常：" + e); }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ==================================================================
    //  测试替身（**嵌套类** = 没有脚本资产 ⇒ `AddComponent` 不会被 Unity 拒绝；
    //  见 CLAUDE.md 「Unity 拒 AddComponent<T> 的条件是 T 有自己的脚本资产」）
    // ==================================================================
    class B117StubModule : WarpforgeVFX.WFSceneModule
    {
        public int InitCount, ExitCount, DestroyCount;
        public override void Initialize(CardPresentation.AnimFXController controller)
        {
            base.Initialize(controller);   // ⛔ 不调这一句 ⇒ `Controller` 恒 null
            InitCount++;
        }
        public override void Exit() { ExitCount++; }
        public override void DoDestroy() { DestroyCount++; }
    }

    class B117StubToggler : UIEffectTogglerBase
    {
        public int ToggleCount, DoUpdateCount, AnimFinishedCount;
        public float LastN = -1f;
        public bool LastOption;
        // ⚠️ 覆写**不重复**默认值（C# 会在覆写上重报默认值时给 CS1066；调用点一律显式传两个实参）
        public override void Toggle(bool option, float duration)
        {
            base.Toggle(option, duration);
            ToggleCount++;
            LastOption = option;
        }
        public override void DoUpdate(float currentTime, float normalizedTime)
        {
            DoUpdateCount++;
            LastN = normalizedTime;
        }
        public override void AnimationFinished() { AnimFinishedCount++; }
    }
}
