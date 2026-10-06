// WFModuleCollisions.cs — 原版 `AnimFXModuleCollisions`（**357 实例 / 354 效果**）
//   还带两个嵌套类（原版就是这么嵌的，名字照搬）：
//     · `CollisionAndParticles`（每条 = 一个碰撞平面 + 挂到哪些粒子系统上）
//     · `ParticleCollisionDefinition`（某个粒子系统 + 要不要收碰撞消息 + 要点的 UnityEvent）
//   🔴 它与 `WFModuleParticleCollisionNotifier.cs` 是**一对**：注册 / 回调接口必须一起改，
//      不能只动一边（原版唯一的跨类硬依赖，见 `资料/AnimFX_18类方法体_块2.md` §8）。
//
// 原版语义出处（**逐方法体**）：
//   · 方法体读解：`资料/AnimFX_18类方法体_块1.md` §6（含「枚举 → 平面解析表」）
//   · 方法体原文：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/AnimFXModuleCollisions__Initialize.c`
//     · `d:/2/tools/decomp_animfx_nested/AnimFXModuleCollisions.CollisionAndParticles__Initialize.c`
//     · `.../AnimFXModuleCollisions.CollisionAndParticles__UpdateTargetCollisionPlanePosition.c`
//     · `.../AnimFXModuleCollisions.ParticleCollisionDefinition__{Initialize,OnParticleCollision}.c`
//   · 枚举名 / Tooltip / 字段：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXModuleCollisions.cs`
//   · 报错原文：`d:/2/tools/all_strings.txt`（`[ERROR] particle system not assigned to particle VFX`）
//
// ---- 照方法体还原的 ----
//   ① `Initialize` = `base.Initialize` + `foreach (cap in collisionAndParticles) cap.Initialize(actingCard, targetCard)`。
//   ② `CollisionAndParticles.Initialize`：
//      · 先按「**出招的是不是玩家方**（`actingCard.isPlayer`, 反编译 `+0x40`）」
//        与「**目标是督军吗**（`targetCard.IsWarlordEquivalent()`）」把 `CollisionPlane` 枚举解析成
//        `BattleCollider` id —— 逐支与反编译一致，见 `ResolveColliderId`；**未知枚举值原版抛
//        `ArgumentOutOfRangeException`**（⚠️ 我们改成 LogError + 跳过，理由写在那一行上）；
//      · `id` → 一个 `Transform`（原版 `BattleParticleColliderManager.Instance.GetColliderTransform(id)`）；
//      · **`id == GenericTarget(15)`** 时把这个 transform 立起来（"朝来袭方向"）：
//        `up = normalize(actingCard.position − targetCard.position)`、`position = targetCard.position`
//        （反编译里是先算差值 → `Vector3.Normalize` → `set_up` → `set_position`，顺序也是这个）；
//      · `foreach (def in particleSystemsDefinition) def.Initialize(planeTransform)`
//        —— 🔴 **元素为 null 就 `break`**（原版如此，不是 continue）。
//   ③ `ParticleCollisionDefinition.Initialize(plane)`：
//      · 粒子系统为空 → `LogError("[ERROR] particle system not assigned to particle VFX")` 后 return；
//      · `ps.collision.AddPlane(plane)`；
//      · 若 `receiveCollisionMessage`：在 **`ps` 所在的那个 GameObject** 上取/加
//        `AnimFXParticleCollisionNotifier` → `Register(this)` → `ps.collision.sendCollisionMessages = true`
//        （⚠️ 原版**不去重**，`Register` 就是 `List.Add`）。
//   ④ `ParticleCollisionDefinition.OnParticleCollision()`：`if (receiveCollisionMessage && collisionEvent != null) collisionEvent.Invoke()`。
//   ⑤ 还有一个 `UpdateTargetCollisionPlanePosition(plane, actingT, targetT)`：与 ② 的 `GenericTarget` 分支
//      **同一条公式的独立刷新版**。⚠️ 反编译集里**只有它的定义、没有调用点**（是不是废弃的没验证）
//      ⇒ 本类**不带它**（块1 §6 已记这条「未验证」）。
//
// ---- 我们自己定的（逐条在代码里也标着）----
//   A. **两个下游钩子**（不接就报，不静默）：
//      · `ColliderLookup`：`BattleCollider id → Transform`（原版是 `BattleParticleColliderManager` 单例 + 7 个
//        Transform 字段）。**没接 ⇒ 这一条不加平面**：计数 `DroppedPlanes` + 一次性警告。
//      · `ContextResolver`：把「这次特效的 actingCard / targetCard / 出招方是不是玩家 / 目标是不是督军」
//        填进本组件的 4 个公开字段（原版是直接读 `CardScript` 上的 `.transform` / `.isPlayer` /
//        `IsWarlordEquivalent()`）。
//   B. **`collisionEvent` 的订阅者**：原版是在 prefab 的 Inspector 里连的 UnityEvent。
//      ✅ **2026-10-16（A828）已从数据接上**：在这之前数据里这一层**根本不在**（`dump_animfx.py` 的
//      深度护栏把 `collisionEvent` 整棵子树写成 `<深>`，`gen_animfx_modules.py` 再把它跳过 ⇒
//      原版 **563 条 PersistentCall 一条都没进数据**）⇒ 现在按数据逐条 `AddListener`
//      （`ParticleCollisionDefinition.EventCall[]` → `BindCollisionEvent`）。
//      ⛔ 别再把「没有订阅者」当成原版语义 —— 那是数据侧断过一层（原版此处**确实**静默：
//      `UnityEvent.Invoke()` 没有监听者就什么都不发生；广播次数仍记在
//      `WFModuleParticleCollisionNotifier` 的静态计数里）。
//   C. **卡片上下文缺失 / 未知枚举值**：**不抛异常、不 NRE**（原版这两种情况是 NRE / 抛
//      `ArgumentOutOfRangeException`）⇒ LogError 或计一次数 + 跳过，让整条特效装配别被一个模块带崩。
//   D. **`_planeCache`**：我们按 id 缓存 plan 查找结果，**每帧/每次都不会重复问钩子**（原版每次现查）。
//      纯粹是我们这边图省事，语义无差别。
//
// ---- 🔴 2026-10-16（A828）订正：本节原来那条「数据侧断的」记录**是错的**（铁律 5）----
//   原文写：每条 `collisionAndParticles[i].particleSystemsDefinition`（平面加到哪些粒子系统上、
//   谁要收碰撞消息）在 `animfx_modules.json` 里**完全没有**。
//   **实测它一直都在**：656 条非空 `@node:ParticleSystem:…` 引用 + **691 条**
//   `receiveCollisionMessage`（本件对 `数据/游戏数据/animfx_modules.json` 亲数）。
//   真被 `<深>` 吃掉、再被生成器跳过的**只有 `collisionEvent` 这一棵子树**（563 条订阅）。
//   **错因**：把「`collisionEvent` 一个键不在」写成了「整层都不在」，没去数同一级的另两个字段
//   （`particleSystem` / `receiveCollisionMessage` 就在旁边，一数就知道）。
//   ⇒ 现在这一层与它的订阅**都从数据来**（重跑 `工具/dump_animfx.py` + `工具/gen_animfx_modules.py`）。
//
//   ⚠️ **还欠的**：`m_CallState`（原版 563 条**全是 2**）**没有还原** —— 「2 = `RuntimeOnly`（会响）」
//   是**推断**（没实读 Unity 源码，判据见 `资料/普查产出_1016/盘点_用户拍板三件.md` §③）。
//   我们一律按「会响」接，等于把 `Off`(0) / `EditorAndRuntime`(1) 也当成 2 处理。**判据不足，先记账。**
//   ⚠️ `m_Mode` 只读不用（4 种已知搭配：PlaySound=Object / TriggerCameraShake、AnimEventDoShake=Int /
//   PlayAnim=Void，共 563 条，见 `资料/普查产出_1016/W2_A828碰撞屏震.md`）。
//   ⚠️ **自制特效**不用等数据：在 Inspector 里给 `particleSystemsDefinition` 手填
//   `ParticleSystem` + `collisionEvent` 就能用。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleCollisions")]
    public class WFModuleCollisions : WFEffectModule
    {
        /// <summary>原版 `CollisionPlane`（私有嵌套枚举，我们挪成公开只为 Inspector 好填；**值照原版**）。</summary>
        public enum WFCollisionPlane
        {
            Floor = 0,
            Opponent = 5,
            OpponentForceInFrontOfWarlord = 10,
            OpponentForceInFrontOfWarlordFromCamera = 12,
            MySelf = 15,
            MySelfForceInFrontOfWarlord = 20,
            DynamicTarget = 25,
        }

        /// <summary>原版 `BattleCollider` 的 id（`BattleParticleColliderManager.GetColliderTransform` 的入参取值）。
        /// 逐值来自反编译：解析表里只会出现这 7 个。</summary>
        public enum WFBattleCollider
        {
            Floor = 0,
            Player = 5,
            PlayerWarlord = 7,
            PlayerWarlordFromCamera = 8,
            Enemy = 10,
            EnemyWarlord = 11,
            GenericTarget = 15,
        }

        [Serializable]
        public class CollisionAndParticles
        {
            // ---- 嵌套类：原版嵌的就是**这一层**（不是直接嵌在 WFModuleCollisions 上）----
            //   实据：`AnimFXParticleCollisionNotifier.collisionsModules` 的类型就是
            //   `List<AnimFXModuleCollisions.CollisionAndParticles.ParticleCollisionDefinition>`
            //   （桩文件 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXParticleCollisionNotifier.cs`）。
            //   ⇒ **别把这两个类拉平**：`WFModuleParticleCollisionNotifier` 是按全名引用的，拉平就编不过。
            [Serializable]
            public class ParticleCollisionDefinition
            {
                /// <summary>原版 `collisionEvent` 里连的**一条 PersistentCall**（= prefab 的 Inspector 里
                /// 那一行）。数据来自 `animfx_components.json` 的
                /// `collisionEvent.m_PersistentCalls.m_Calls[i].*`（`dump_animfx.py` 的深度护栏
                /// **只对这一条路径**放宽到 18 层，见那个文件上部）。
                /// 🔴 **2026-10-16（A828）之前这一层在数据里根本不存在** ⇒ 事件恒空，见文件头 B。</summary>
                [Serializable]
                public class EventCall
                {
                    [Tooltip("原版 `m_Target`：`@node:<类型>:<路径>`（prefab 内部引用）")]
                    public string targetRef = "";
                    [Tooltip("原版 `m_TargetAssemblyTypeName` 的**类名段**（如 `AnimFXModuleScreenShake`）")]
                    public string targetType = "";
                    [Tooltip("原版 `m_MethodName`（空 = 空槽，原版也不响）")]
                    public string methodName = "";
                    [Tooltip("原版 `m_Mode`（1=Void · 2=Object · 3=Int）。⚠️ 只记不用")]
                    public int mode;
                    public int intArg;
                    public float floatArg;
                    public string stringArg = "";
                    [Tooltip("`m_Arguments.m_ObjectArgument` 解出来的**资产名**（`PlaySound` 的 AudioCue）")]
                    public string argAssetName = "";
                    [Tooltip("原版 `m_CallState`（563 条全是 2）。⚠️ 语义没实读 Unity 源码，见文件头")]
                    public int callState;
                }

                [Tooltip("要加碰撞平面 / 要收碰撞消息的那个粒子系统（原版字段名就是 `particleSystem`）")]
                public ParticleSystem particleSystem;

                [Tooltip("原版 `receiveCollisionMessage`：开了才在 ps 所在 GameObject 上挂通知组件并注册自己")]
                public bool receiveCollisionMessage;

                [Tooltip("原版 `collisionEvent`（UnityEvent）。订阅者由 `calls` 装配出来（见 `BindCollisionEvent`）；" +
                         "没有订阅者时 Invoke 什么都不做 —— 原版此处即静默。")]
                public UnityEvent collisionEvent = new UnityEvent();

                [Tooltip("原版 `collisionEvent` 上连的那几条 PersistentCall（数据来的；空 = 原版就是空事件）")]
                public EventCall[] calls = new EventCall[0];

                [HideInInspector] public string particleSystemRef = "";   // 数据装配用（`@node:ParticleSystem:…`）

                /// <summary>原版 `ParticleCollisionDefinition.Initialize(Transform)`。</summary>
                public void Initialize(Transform referenceCollisionPlane, WFModuleCollisions owner)
                {
                    // 数据装配路径：引用字符串在这一步才解析（要 prefab 根，见 WFModuleScaleByTarget 文件头 D）
                    if (particleSystem == null && !string.IsNullOrEmpty(particleSystemRef))
                        particleSystem = WFEffectModule.ResolveNode<ParticleSystem>(owner != null ? owner.RefRoot : null,
                                                                                  particleSystemRef, "Collisions");

                    if (particleSystem == null)
                    {
                        // 原文案（all_strings.txt 69890000）
                        Debug.LogError("[ERROR] particle system not assigned to particle VFX");
                        MissingParticles++;
                        return;
                    }

                    // ⚠️ CollisionModule 是 struct：**不能**写 `ps.collision.xxx = …`（编译器不许改属性返回的临时值）；
                    //    取本地副本再改，副本内部仍写回 ParticleSystem（Unity 的标准写法，工程里也是这么写的）。
                    var col = particleSystem.collision;
                    col.AddPlane(referenceCollisionPlane);              // 原版没做 null 检查；我们上游已拦住 null
                    PlanesAdded++;

                    if (!receiveCollisionMessage) return;               // 原版：开关关着就到此为止

                    var go = particleSystem.gameObject;
                    var notifier = go.GetComponent<WFModuleParticleCollisionNotifier>();
                    if (notifier == null) notifier = go.AddComponent<WFModuleParticleCollisionNotifier>();
                    notifier.Register(this);                            // 原版 `Register` = `List.Add`（**不去重**）
                    NotifiersRegistered++;

                    col.sendCollisionMessages = true;                   // 反编译：set_sendCollisionMessages(true)
                }

                /// <summary>原版 `ParticleCollisionDefinition.OnParticleCollision()`。
                /// ⚠️ 原版这里是 `collisionEvent != null` 就 Invoke —— **没有订阅者时什么都不发生（原版此处即静默）**，
                /// 我们照原版写；广播次数记在通知组件那边。</summary>
                public void OnParticleCollision()
                {
                    if (!receiveCollisionMessage) return;
                    if (collisionEvent != null) collisionEvent.Invoke();
                }

                /// <summary>🆕 **2026-10-16（A828）**：把数据里的 PersistentCall 逐条装成**真的监听者**。
                /// 原版是 prefab 里序列化好的 persistent call；我们这条链的 prefab 是从 bundle 导出的
                /// （脚本被当缺失剥掉、UnityEvent 也不在），所以改成运行时 `AddListener`。
                /// ⚠️ **语义等价但形态不同**：`Invoke()` 一样会叫到，可 `GetPersistentEventCount()` 恒为 0
                /// —— ⛔ **别拿它当判据**（判据用 `CallsBound` / `Broadcasts` 这两个计数）。
                /// 装配时机 = `Initialize`（那时 prefab 根与各模块组件都已就位）。</summary>
                public void BindCollisionEvent(WFModuleCollisions owner)
                {
                    if (collisionEvent == null) collisionEvent = new UnityEvent();
                    if (calls == null || calls.Length == 0) return;

                    for (int i = 0; i < calls.Length; i++)
                    {
                        var c = calls[i];
                        if (c == null) continue;
                        if (string.IsNullOrEmpty(c.methodName))
                        {
                            SkippedEmptyCalls++;          // 原版那 4 条空方法名 = 空槽（原版也不响）
                            continue;
                        }
                        string why;
                        var target = ResolveCallTarget(owner, c, out why);
                        if (target == null) { CallsUnresolved++; WarnCallOnce(c, why); continue; }
                        var act = MakeAction(target, c, owner, out why);
                        if (act == null) { CallsUnwired++; WarnCallOnce(c, why); continue; }
                        collisionEvent.AddListener(act);
                        CallsBound++;
                    }
                }

                /// <summary>`m_Target` → 那个组件。**判据是 `[WFModuleKind]`**（与 `WFModuleFactory`
                /// 同一张表、同一个来源，⛔ 别在这里另抄一份 `原版类名 → 我们的类` 的 switch）。
                /// `AnimFXController` 特殊：它**不是模块**（就是播放器自己，贴不了那个特性）。</summary>
                static Component ResolveCallTarget(WFModuleCollisions owner, EventCall c, out string why)
                {
                    why = null;
                    if (owner == null) { why = "没有属主模块"; return null; }

                    if (c.targetType == "AnimFXController")
                    {
                        // 原版就是「这次特效的那个控制器」（一个 prefab 一份）
                        var pl = owner.Controller != null
                            ? owner.Controller
                            : owner.GetComponent<WarpforgeEffectPlayer>();
                        if (pl == null) why = "拿不到这次特效的控制器（手工挂的模块没有 Controller）";
                        return pl;
                    }

                    string kind, type, rest;
                    WFModuleDef.SplitRef(c.targetRef, out kind, out type, out rest);
                    if (kind != "node")
                    {
                        why = "`m_Target` 不是节点引用（数据里是 `" + c.targetRef + "`）";
                        return null;
                    }
                    var t = WFEffectModule.ResolvePath(owner.RefRoot, rest, "Collisions.collisionEvent");
                    if (t == null) { why = "`m_Target` 的路径解析不了（节点 `" + rest + "`）"; return null; }
                    var comp = FindByOriginalKind(t.gameObject, c.targetType);
                    if (comp == null) why = "那个节点上没有原版类 `" + c.targetType + "` 对应的组件";
                    return comp;
                }

                static Component FindByOriginalKind(GameObject go, string originalKind)
                {
                    if (go == null || string.IsNullOrEmpty(originalKind)) return null;
                    var comps = go.GetComponents<Component>();
                    for (int i = 0; i < comps.Length; i++)
                    {
                        var comp = comps[i];
                        if (comp == null) continue;
                        var attrs = comp.GetType().GetCustomAttributes(typeof(WFModuleKindAttribute), false);
                        for (int a = 0; a < attrs.Length; a++)
                        {
                            var k = (WFModuleKindAttribute)attrs[a];
                            if (k != null && k.Kind == originalKind) return comp;
                        }
                    }
                    return null;
                }

                /// <summary>原版那 4 个方法名 → 我们这侧的入口。**逐条对方法体**（不是按名字猜）：
                /// · `AnimFXModuleScreenShake.AnimEventDoShake(int)` / `TriggerCameraShake(int)`
                ///   → 同名方法（两个都读 `manualTriggerCameraShakes`，见 `WFModuleScreenShake.cs`）
                /// · `AnimFXModuleTween.PlayAnim()` → `WFModuleTween.PlayAnim()`
                /// · `AnimFXController.PlaySound(AudioCue)` → 原版方法体是
                ///   `SoundManager.Play3D(cue, this.transform.position)`
                ///   （`d:/2/tools/decomp_full/AnimFXController__PlaySound.c`）⇒ 我们也 3D 播放。
                /// 认不出的方法名**不静默**：返回 null 并给出 why，由 `BindCollisionEvent` 记账。</summary>
                static UnityAction MakeAction(Component target, EventCall c, WFModuleCollisions owner, out string why)
                {
                    why = null;
                    switch (c.methodName)
                    {
                        case "AnimEventDoShake":
                        {
                            var m = target as WFModuleScreenShake;
                            if (m == null) { why = "目标上不是 `WFModuleScreenShake`"; return null; }
                            int i = c.intArg;
                            return () => m.AnimEventDoShake(i);
                        }
                        case "TriggerCameraShake":
                        {
                            var m = target as WFModuleScreenShake;
                            if (m == null) { why = "目标上不是 `WFModuleScreenShake`"; return null; }
                            int i = c.intArg;
                            return () => m.TriggerCameraShake(i);
                        }
                        case "PlayAnim":
                        {
                            var m = target as WFModuleTween;
                            if (m == null) { why = "目标上不是 `WFModuleTween`"; return null; }
                            return () => m.PlayAnim();
                        }
                        case "PlaySound":
                        {
                            var pl = owner != null ? owner.Controller : null;
                            if (pl == null) { why = "拿不到这次特效的控制器"; return null; }
                            string cue = c.argAssetName;
                            if (string.IsNullOrEmpty(cue))
                            {
                                why = "`m_Arguments.m_ObjectArgument`（AudioCue）在数据里解不出来";
                                return null;
                            }
                            var tf = pl.transform;
                            return () =>
                            {
                                WFSoundCue sc;
                                if (!WFSoundBank.TryGetCue(cue, out sc))
                                {
                                    // 不静默：表里没有这条 cue（同 `WarpforgeEffectPlayer.PlayOneSound` 的口径）
                                    MissingSoundCues++;
                                    if (MissingSoundCues <= 5)
                                        Debug.LogWarning("[WarpforgeVFX] Collisions 的 collisionEvent 要播 cue `" +
                                                         cue + "`，音效表里没有 —— 这一条不会响。");
                                    return;
                                }
                                WFSoundPlayer.Play(sc, tf.position, false);   // 3D（原版 Play3D）
                            };
                        }
                        default:
                            why = "原版方法 `" + c.methodName + "` 我们这侧还没有对应入口";
                            return null;
                    }
                }

                static readonly HashSet<string> _callWarned = new HashSet<string>();
                /// <summary>清掉「已经报过的那几种」（自检用它，好让下一轮还能报一次）。</summary>
                public static void ResetCallWarnings() { _callWarned.Clear(); }
                static void WarnCallOnce(EventCall c, string why)
                {
                    if (string.IsNullOrEmpty(why)) why = "原因没记";
                    if (!_callWarned.Add(c.targetType + "." + c.methodName + " :: " + why)) return;
                    if (_callWarned.Count > 12) return;      // 只报前 12 种，别刷屏（计数照记，不静默）
                    Debug.LogWarning("[WarpforgeVFX] Collisions 的 collisionEvent 有一条订阅**没接上**：`" +
                                     c.targetType + "." + c.methodName + "` —— " + why +
                                     "。已接 " + CallsBound + " 条 / 没接 " +
                                     (CallsUnwired + CallsUnresolved) + " 条（计数见 WFModuleCollisions）。");
                }
            }

            [Tooltip("原版 Tooltip：Opponent: in front of minions if target is minion or warlord if target is warlord \n" +
                     "OpponentForceInFrontOfWarlord: Always in front of the opponent warlord")]
            public WFCollisionPlane collisionPlane = WFCollisionPlane.Floor;

            [Tooltip("这个平面加在哪些粒子系统上（原版字段）。数据装配出来的条目里 `particleSystem` 是 " +
                     "`@node:ParticleSystem:…` 引用，`Initialize` 时解析；`collisionEvent` 的订阅者由 calls 装配。" +
                     "自制特效可以手填。")]
            public ParticleCollisionDefinition[] particleSystemsDefinition = new ParticleCollisionDefinition[0];

            /// <summary>原版 `CollisionAndParticles.Initialize(CardScript actingCard, CardScript targetCard)`。
            /// 我们签名改成传属主模块：卡片信息的**载体**不同（我们这边在模块的公开字段上，见文件头 A）。</summary>
            public void Initialize(WFModuleCollisions owner)
            {
                if (owner == null) return;

                // ---- ⓪ 先把 `collisionEvent` 的订阅接上（**与平面无关**）----
                //   原版的事件是 prefab 上连好的，能不能响只由 `receiveCollisionMessage` 决定；
                //   平面解析失败（缺 ColliderLookup / 缺卡片上下文）**不该顺带把订阅也丢掉** ⇒ 放最前面。
                for (int i = 0; i < particleSystemsDefinition.Length; i++)
                {
                    var b = particleSystemsDefinition[i];
                    if (b == null) break;                        // 与下面那个循环同一个约定
                    b.BindCollisionEvent(owner);
                }

                // ---- ② 枚举 → BattleCollider id ----
                int id = ResolveColliderId(collisionPlane, owner.actingIsPlayer, owner.targetIsPlayer,
                                           owner.targetIsWarlord);
                if (id < 0) { InvalidPlanes++; return; }             // ResolveColliderId 已经 LogError 过

                // ---- ② id → Transform（原版：BattleParticleColliderManager.Instance.GetColliderTransform）----
                Transform plane = LookupCollider(id);
                if (plane == null)
                {
                    DroppedPlanes++;
                    owner.WarnNoColliderLookupOnce();
                    return;                                          // 不静默：计数 + 一次性警告
                }

                // ---- ② GenericTarget = 动态目标平面：朝来袭方向立起来 ----
                if (id == (int)WFBattleCollider.GenericTarget)
                {
                    if (owner.actingCard == null || owner.targetCard == null)
                    {
                        MissingCardContext++;
                        owner.WarnNoCardContextOnce();
                        return;                                      // 原版这里会 NRE，我们报一次就跳过
                    }
                    // 原版顺序：先取两张卡的位置 → 算差值 → normalize → 写 up → 再写 position
                    Vector3 dir = owner.actingCard.position - owner.targetCard.position;
                    plane.up = dir.normalized;
                    plane.position = owner.targetCard.position;
                }

                // ---- ② 逐个 def ----
                for (int i = 0; i < particleSystemsDefinition.Length; i++)
                {
                    var d = particleSystemsDefinition[i];
                    if (d == null) break;                            // 原版：元素为 null 就 break（不是 continue）
                    d.Initialize(plane, owner);
                }
            }
        }

        [Tooltip("原版字段（私有 [SerializeField]，我们改公开）。每条 = 一个平面 + 它管的粒子系统。")]
        public CollisionAndParticles[] collisionAndParticles = new CollisionAndParticles[0];

        // ---- 卡片上下文：原版从 controller 的 CardScript 上读（文件头 A）----
        [Tooltip("出招卡（原版 `AnimFXController.actingCard`）")]
        public Transform actingCard;
        [Tooltip("目标卡（原版 `AnimFXController.targetCard`）")]
        public Transform targetCard;
        [Tooltip("原版读 `actingCard.isPlayer`（反编译 `CardScript + 0x40`）—— 我们的卡没有这个字段，靠下游填")]
        public bool actingIsPlayer;
        [Tooltip("原版读 `targetCard.isPlayer`（只在 DynamicTarget 那支用得上）")]
        public bool targetIsPlayer;
        [Tooltip("原版读 `targetCard.IsWarlordEquivalent()`（解析表里那个 `W`）")]
        public bool targetIsWarlord;

        /// <summary>解析 prefab 内部引用（`@node:…`）用的根 = **本效果的 prefab 根**。
        /// `Controller` 由基类在 `Initialize` 时注入 ⇒ 数据装配那条路一定非空；手工挂的模块退到自己的 transform。
        /// 🆕 2026-10-16（A828）：从 `ParticleCollisionDefinition.Initialize` 里那段三元表达式收口过来的
        /// —— 一处读法两处用（那里原来自己算了一遍，等价）。</summary>
        internal Transform RefRoot { get { return Controller != null ? Controller.transform : transform; } }

        /// <summary>下游钩子（可选）：把这次特效的卡片上下文填进上面 5 个字段。
        /// **不接 ⇒ 依赖卡片的那些平面解析不出来**（会计数 + 一次性警告，不会静默）。</summary>
        public static Action<WFModuleCollisions> ContextResolver;

        /// <summary>下游钩子（可选）：`BattleCollider id → Transform`（原版是 `BattleParticleColliderManager`
        /// 单例上的 7 个 Transform 字段）。**不接 ⇒ 一条平面都加不上**（计数 `DroppedPlanes` + 一次性警告）。</summary>
        public static Func<int, Transform> ColliderLookup;

        // ---- 诊断计数（自检用）----
        /// <summary>因为 `ColliderLookup` 没接而**没加**的平面数。</summary>
        public static int DroppedPlanes;
        /// <summary>枚举值不在原版那张表里（原版会抛异常）的次数。</summary>
        public static int InvalidPlanes;
        /// <summary>真的调了 `AddPlane` 的次数。</summary>
        public static int PlanesAdded;
        /// <summary>真的注册进通知组件的 def 数。</summary>
        public static int NotifiersRegistered;
        /// <summary>粒子系统引用为空（原版 LogError 那一支）的次数。</summary>
        public static int MissingParticles;
        /// <summary>要做动态平面但缺卡片上下文的次数（原版会 NRE）。</summary>
        public static int MissingCardContext;
        /// <summary>**数据里一条 `particleSystemsDefinition` 都没有**的 cap 数
        /// （原版就没填 defs 的那种；**不再是**「dump 截断」—— 那条已在 2026-10-16 订正，见文件头）。</summary>
        public static int CapsWithNoDefinitions;

        // ---- `collisionEvent` 订阅的记账（🆕 2026-10-16 / A828；**纯计数，不改行为**）----
        /// <summary>真的 `AddListener` 上去的订阅条数。</summary>
        public static int CallsBound;
        /// <summary>`m_Target` 解析不了 / 那个节点上没有对应组件的条数。</summary>
        public static int CallsUnresolved;
        /// <summary>方法名我们这侧没有对应入口的条数（见 `MakeAction`）。</summary>
        public static int CallsUnwired;
        /// <summary>原版就是空槽（`m_MethodName` 为空）的条数 —— 原版那 4 条。</summary>
        public static int SkippedEmptyCalls;
        /// <summary>`PlaySound` 那条要播的 cue 在音效表里找不到的次数。</summary>
        public static int MissingSoundCues;

        static bool _noLookupWarned, _noContextWarned, _truncatedWarned;

        public static void ResetDiagnostics()
        {
            DroppedPlanes = 0; InvalidPlanes = 0; PlanesAdded = 0; NotifiersRegistered = 0;
            MissingParticles = 0; MissingCardContext = 0; CapsWithNoDefinitions = 0;
            CallsBound = 0; CallsUnresolved = 0; CallsUnwired = 0; SkippedEmptyCalls = 0;
            MissingSoundCues = 0;
            CollisionAndParticles.ParticleCollisionDefinition.ResetCallWarnings();
            _noLookupWarned = _noContextWarned = _truncatedWarned = false;
        }

        /// <summary>建 `CollisionAndParticles[]`（数据装配路径）。引用字符串留给 `Initialize` 解析
        /// （那时才知道 prefab 根 / 才有 `Controller`）。</summary>
        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;

            // ⚠️ 这里**不能**用 `ReadRefStrings(def, "collisionAndParticles")` —— 元素**自己没有值**，
            //    数据里只有元素里的字段（`collisionAndParticles[0].collisionPlane`）⇒ 用带探针的形式扫下标。
            int caps = WFModuleScaleByTarget.CountIndexed(def, "collisionAndParticles", ".collisionPlane");
            if (caps == 0) return;

            var arr = new CollisionAndParticles[caps];
            for (int i = 0; i < caps; i++)
            {
                string p = "collisionAndParticles[" + i + "]";
                var cap = new CollisionAndParticles
                {
                    // 原版 `CollisionPlane` 是枚举；数据里存的就是它的 int 值
                    collisionPlane = (WFCollisionPlane)def.GetInt(p + ".collisionPlane", 0),
                };

                // 🔴 2026-10-16（A828）**订正**：这里原来写「当前数据里这一层**恒为空**（dump 把它记成
                //    `<深>` 后跳过了）」—— **不成立**（铁律 5）：`particleSystemsDefinition` 一直都在，
                //    被吃掉的只有 `collisionEvent` 那一棵子树（详见文件头「订正」那一段）。
                //    下面这套键名照原版类的字段顺序（`particleSystem` / `receiveCollisionMessage` /
                //    `collisionEvent`）。
                //
                // ⚠️ 探针键从 `.particleSystem` 改成 **`.receiveCollisionMessage`**：前者**可能合理地不存在**
                //    （原版就有 35 个 def 的 `particleSystem` 是空引用 ⇒ dump 出来没有这个键），
                //    而 `CountIndexed` 遇到**两个连号缺失就停** ⇒ 会**把尾巴上的 def（连同它们的订阅）整段丢掉**
                //    （实测 450 个 cap 里有 29 个两种数法不一致、其中 8 个是断档）。
                //    `receiveCollisionMessage` 是 bool、每个 def 都写 ⇒ 数出来**恒等于真 def 数**（691）。
                //    代价：那 35 个空引用 def 现在会走到 `Initialize` 的 `[ERROR] particle system not
                //    assigned to particle VFX` 那一支（**原版也走**，那是原版的 LogError 文案），计 `MissingParticles`。
                int nd = WFModuleScaleByTarget.CountIndexed(def, p + ".particleSystemsDefinition",
                                                            ".receiveCollisionMessage");
                if (nd == 0) CapsWithNoDefinitions++;
                var list = new CollisionAndParticles.ParticleCollisionDefinition[nd];
                for (int j = 0; j < nd; j++)
                {
                    string q = p + ".particleSystemsDefinition[" + j + "]";
                    var d = new CollisionAndParticles.ParticleCollisionDefinition
                    {
                        particleSystemRef = def.GetString(q + ".particleSystem"),
                        receiveCollisionMessage = def.GetBool(q + ".receiveCollisionMessage"),
                    };
                    ReadCalls(def, q, d);
                    list[j] = d;
                }
                cap.particleSystemsDefinition = list;
                arr[i] = cap;
            }
            collisionAndParticles = arr;
        }

        /// <summary>读一个 def 上 `collisionEvent` 的 PersistentCall 列表（🆕 2026-10-16 / A828）。
        /// 键名 = `工具/gen_animfx_modules.py` 拍平后的点号键，形状照原版
        /// `&lt;def&gt;.collisionEvent.m_PersistentCalls.m_Calls[i].*`。没有这个键 = 原版就是空事件（留空数组）。</summary>
        static void ReadCalls(WFModuleDef def, string q, CollisionAndParticles.ParticleCollisionDefinition d)
        {
            string cp = q + ".collisionEvent.m_PersistentCalls.m_Calls";
            // ⚠️ 探针用 `.m_MethodName` —— 它在**每条** call 上都有（原版那 4 条是空串，不是没有这个键）
            int n = WFModuleScaleByTarget.CountIndexed(def, cp, ".m_MethodName");
            if (n == 0) return;

            var arr = new CollisionAndParticles.ParticleCollisionDefinition.EventCall[n];
            for (int k = 0; k < n; k++)
            {
                string ck = cp + "[" + k + "]";
                string tn = def.GetString(ck + ".m_TargetAssemblyTypeName");
                int comma = tn.IndexOf(',');
                if (comma > 0) tn = tn.Substring(0, comma).Trim();   // `AnimFXModuleScreenShake, Assembly-CSharp`
                var c = new CollisionAndParticles.ParticleCollisionDefinition.EventCall
                {
                    targetRef = def.GetString(ck + ".m_Target"),
                    targetType = tn,
                    methodName = def.GetString(ck + ".m_MethodName"),
                    mode = def.GetInt(ck + ".m_Mode"),
                    intArg = def.GetInt(ck + ".m_Arguments.m_IntArgument"),
                    floatArg = def.GetFloat(ck + ".m_Arguments.m_FloatArgument"),
                    stringArg = def.GetString(ck + ".m_Arguments.m_StringArgument"),
                    callState = def.GetInt(ck + ".m_CallState"),
                };
                // `m_ObjectArgument` 是 `@asset:<类型>:<名字>` —— 只要名字（`PlaySound` 的 AudioCue 名）
                string kind, type, rest;
                WFModuleDef.SplitRef(def.GetString(ck + ".m_Arguments.m_ObjectArgument"),
                                     out kind, out type, out rest);
                if (kind == "asset") c.argAssetName = rest;
                arr[k] = c;
            }
            d.calls = arr;
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);

            if (ContextResolver != null) ContextResolver(this);      // 我们自己定的（文件头 A）

            if (CapsWithNoDefinitions > 0) WarnTruncatedOnce();

            for (int i = 0; i < collisionAndParticles.Length; i++)
            {
                var cap = collisionAndParticles[i];
                if (cap == null) continue;
                cap.Initialize(this);
            }
        }

        // ---- 枚举 → collider id（**纯函数**，好断言）----
        //
        // 逐支与反编译一致（`AnimFXModuleCollisions.CollisionAndParticles__Initialize.c`）：
        //   plane             | acting 是玩家方            | acting 是敌方
        //   Floor 0           | Floor 0                    | Floor 0
        //   Opponent 5        | W ? EnemyWarlord 11 : Enemy 10      | W ? PlayerWarlord 7 : Player 5
        //   OpponentWarlord 10| EnemyWarlord 11            | PlayerWarlord 7
        //   …FromCamera 12    | EnemyWarlord 11            | PlayerWarlordFromCamera 8
        //   MySelf 15         | W ? PlayerWarlord 7 : Player 5      | W ? EnemyWarlord 11 : Enemy 10
        //   MySelfWarlord 20  | PlayerWarlord 7            | EnemyWarlord 11
        //   DynamicTarget 25  | 同阵营 → GenericTarget 15；对方 → 对方那侧（5/7 或 10/11）
        //   （W = `targetCard.IsWarlordEquivalent()`）
        /// <summary>未知枚举值返回 **-1**（并 LogError）。
        /// ⚠️ **与原版的一处有意偏离**：原版这里 `throw new ArgumentOutOfRangeException()`，
        /// 我们改成报错 + 跳过 —— 在模块装配里抛出去会把整条特效（连同别的模块）带崩，
        /// 而枚举值越界只可能来自数据，报出来就够定位了。</summary>
        public static int ResolveColliderId(WFCollisionPlane plane, bool actingIsPlayer, bool targetIsPlayer,
                                            bool targetIsWarlord)
        {
            switch (plane)
            {
                case WFCollisionPlane.Floor:
                    return (int)WFBattleCollider.Floor;

                case WFCollisionPlane.Opponent:
                    return actingIsPlayer
                        ? (targetIsWarlord ? (int)WFBattleCollider.EnemyWarlord : (int)WFBattleCollider.Enemy)
                        : (targetIsWarlord ? (int)WFBattleCollider.PlayerWarlord : (int)WFBattleCollider.Player);

                case WFCollisionPlane.OpponentForceInFrontOfWarlord:
                    return actingIsPlayer ? (int)WFBattleCollider.EnemyWarlord : (int)WFBattleCollider.PlayerWarlord;

                case WFCollisionPlane.OpponentForceInFrontOfWarlordFromCamera:
                    // 反编译：acting 是玩家方 → 11（EnemyWarlord，没有 FromCamera 那个值）；否则 8
                    return actingIsPlayer ? (int)WFBattleCollider.EnemyWarlord : (int)WFBattleCollider.PlayerWarlordFromCamera;

                case WFCollisionPlane.MySelf:
                    return actingIsPlayer
                        ? (targetIsWarlord ? (int)WFBattleCollider.PlayerWarlord : (int)WFBattleCollider.Player)
                        : (targetIsWarlord ? (int)WFBattleCollider.EnemyWarlord : (int)WFBattleCollider.Enemy);

                case WFCollisionPlane.MySelfForceInFrontOfWarlord:
                    return actingIsPlayer ? (int)WFBattleCollider.PlayerWarlord : (int)WFBattleCollider.EnemyWarlord;

                case WFCollisionPlane.DynamicTarget:
                    if (actingIsPlayer == targetIsPlayer) return (int)WFBattleCollider.GenericTarget;   // 同阵营
                    return actingIsPlayer
                        ? (targetIsWarlord ? (int)WFBattleCollider.EnemyWarlord : (int)WFBattleCollider.Enemy)
                        : (targetIsWarlord ? (int)WFBattleCollider.PlayerWarlord : (int)WFBattleCollider.Player);

                default:
                    Debug.LogError("[WarpforgeVFX] Collisions 的 collisionPlane 值 " + (int)plane +
                                   " **不在原版那张解析表里**（Floor/Opponent/OpponentForceInFrontOfWarlord/" +
                                   "…FromCamera/MySelf/MySelfForceInFrontOfWarlord/DynamicTarget）—— 原版这里抛" +
                                   " ArgumentOutOfRangeException，我们跳过这一条。" +
                                   "※ 这是数据问题（枚举值越界），不是模块没实现。");
                    return -1;
            }
        }

        /// <summary>问下游要 `id → Transform`。`ColliderLookup` 没接 ⇒ 返回 null（调用方会警告 + 计数）。</summary>
        public static Transform LookupCollider(int id)
        {
            if (ColliderLookup == null) return null;
            return ColliderLookup(id);
        }

        // ---- 一次性警告（照 WFModuleScreenShake 的写法：计数 + 只报前几次，避免刷屏）----
        internal void WarnNoColliderLookupOnce()
        {
            if (_noLookupWarned) return;
            _noLookupWarned = true;
            Debug.LogWarning("[WarpforgeVFX] 碰撞平面**没有下游接**：" +
                             "`WFModuleCollisions.ColliderLookup` 是 null ⇒ 粒子碰撞平面一条也加不上" +
                             "（原版这是 `BattleParticleColliderManager.GetColliderTransform`）。" +
                             "表现层/BattleDriver 接上它，或改用自制特效手填。" +
                             "本进程累计丢掉 " + DroppedPlanes + " 条（这个数会继续涨）。");
        }

        internal void WarnNoCardContextOnce()
        {
            if (_noContextWarned) return;
            _noContextWarned = true;
            Debug.LogWarning("[WarpforgeVFX] Collisions 的 `DynamicTarget` 平面要 acting/target 两张卡才立得起来，" +
                             "但现在卡片上下文是空的（`actingCard`/`targetCard` 为 null）—— 接 " +
                             "`WFModuleCollisions.ContextResolver` 填它们（原版这里会 NullReferenceException）。");
        }

        void WarnTruncatedOnce()
        {
            if (_truncatedWarned) return;
            _truncatedWarned = true;
            Debug.LogWarning("[WarpforgeVFX] Collisions：有一个「碰撞平面」条目 (cap) 底下**一条 " +
                             "particleSystemsDefinition 都没有**（已遇 " + CapsWithNoDefinitions +
                             " 条）⇒ 这个平面落不到任何粒子系统上。" +
                             "🔴 **2026-10-16（A828）订正**：这**不再**是「dump 把它截断了」—— " +
                             "数据里这一层是齐的（详见文件头「订正」），原版本来就有 defs 为空的 cap " +
                             "（那一处原版 prefab 自己没填）。自制特效可以在 Inspector 里手填 " +
                             "`particleSystemsDefinition`。");
        }
    }
}
