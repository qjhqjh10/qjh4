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
//   B. **`collisionEvent` 的订阅者**：原版是在 prefab 的 Inspector 里连的 UnityEvent；我们的数据里这一层
//      **根本没有**（`animfx_modules.json` 里没有 `collisionEvent` 这个键）⇒ 事件没有订阅者时就是静默
//      （**原版此处即静默**：`UnityEvent.Invoke()` 没有监听者就什么都不发生）。为了「不许静默失败」，
//      广播次数在 `WFModuleParticleCollisionNotifier` 的静态计数里记着。
//   C. **卡片上下文缺失 / 未知枚举值**：**不抛异常、不 NRE**（原版这两种情况是 NRE / 抛
//      `ArgumentOutOfRangeException`）⇒ LogError 或计一次数 + 跳过，让整条特效装配别被一个模块带崩。
//   D. **`_planeCache`**：我们按 id 缓存 plan 查找结果，**每帧/每次都不会重复问钩子**（原版每次现查）。
//      纯粹是我们这边图省事，语义无差别。
//
// ---- 🔴 没还原的（**数据侧断的，不是代码没写**）----
//   每条 `collisionAndParticles[i].particleSystemsDefinition`（= 平面加到哪些粒子系统上、谁要收碰撞消息）
//   在 `animfx_modules.json` 里**完全没有** —— 原始 dump 把这一层记成了 `<深>`，而 `工具/gen_animfx_modules.py`
//   故意跳过它（"太深了，宁可不给，也别给错"）。⇒ **数据装配时一个平面也落不到粒子系统上**
//   （会打一次性警告 + 计数 `CapsWithNoDefinitions`）。
//   要修只能走**数据侧**：重跑 `工具/dump_animfx.py` 让它把这一层下沉出来（449 个 def 全在这里面，
//   见块1 §6 的实例统计）。**自制特效**不用等：在 Inspector 里给
//   `collisionAndParticles[i].particleSystemsDefinition` 手填 `ParticleSystem` + `collisionEvent` 就能用。
using System;
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
                [Tooltip("要加碰撞平面 / 要收碰撞消息的那个粒子系统（原版字段名就是 `particleSystem`）")]
                public ParticleSystem particleSystem;

                [Tooltip("原版 `receiveCollisionMessage`：开了才在 ps 所在 GameObject 上挂通知组件并注册自己")]
                public bool receiveCollisionMessage;

                [Tooltip("原版 `collisionEvent`（UnityEvent）。数据里没有它 ⇒ 数据装配出来的都是**空事件**" +
                         "（没有订阅者时 Invoke 什么都不做 —— 原版此处即静默）。")]
                public UnityEvent collisionEvent = new UnityEvent();

                [HideInInspector] public string particleSystemRef = "";   // 数据装配用（`@node:ParticleSystem:…`）

                /// <summary>原版 `ParticleCollisionDefinition.Initialize(Transform)`。</summary>
                public void Initialize(Transform referenceCollisionPlane, WFModuleCollisions owner)
                {
                    // 数据装配路径：引用字符串在这一步才解析（要 prefab 根，见 WFModuleScaleByTarget 文件头 D）
                    if (particleSystem == null && !string.IsNullOrEmpty(particleSystemRef))
                    {
                        var root = owner != null && owner.Controller != null ? owner.Controller.transform
                                                                            : (owner != null ? owner.transform : null);
                        particleSystem = WFEffectModule.ResolveNode<ParticleSystem>(root, particleSystemRef, "Collisions");
                    }

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
            }

            [Tooltip("原版 Tooltip：Opponent: in front of minions if target is minion or warlord if target is warlord \n" +
                     "OpponentForceInFrontOfWarlord: Always in front of the opponent warlord")]
            public WFCollisionPlane collisionPlane = WFCollisionPlane.Floor;

            [Tooltip("这个平面加在哪些粒子系统上。🔴 数据装配出来的这一项**一定是空的**（dump 把这一层截断了，" +
                     "见 WFModuleCollisions.cs 文件头「没还原的」）—— 自制特效可以手填。")]
            public ParticleCollisionDefinition[] particleSystemsDefinition = new ParticleCollisionDefinition[0];

            /// <summary>原版 `CollisionAndParticles.Initialize(CardScript actingCard, CardScript targetCard)`。
            /// 我们签名改成传属主模块：卡片信息的**载体**不同（我们这边在模块的公开字段上，见文件头 A）。</summary>
            public void Initialize(WFModuleCollisions owner)
            {
                if (owner == null) return;

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
        /// <summary>**数据里一条 `particleSystemsDefinition` 都没有**的 cap 数（dump 截断，见文件头「没还原的」）。</summary>
        public static int CapsWithNoDefinitions;

        static bool _noLookupWarned, _noContextWarned, _truncatedWarned;

        public static void ResetDiagnostics()
        {
            DroppedPlanes = 0; InvalidPlanes = 0; PlanesAdded = 0; NotifiersRegistered = 0;
            MissingParticles = 0; MissingCardContext = 0; CapsWithNoDefinitions = 0;
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

                // 🔴 当前数据里这一层**恒为空**（dump 把它记成 `<深>` 后跳过了），见文件头「没还原的」。
                //    下面这套键名是照原版类的字段顺序推的（`particleSystem` / `receiveCollisionMessage` /
                //    `collisionEvent`）—— **数据侧一旦把这一层补出来，这里不用改就能吃上**。
                int nd = WFModuleScaleByTarget.CountIndexed(def, p + ".particleSystemsDefinition", ".particleSystem");
                if (nd == 0) CapsWithNoDefinitions++;
                var list = new CollisionAndParticles.ParticleCollisionDefinition[nd];
                for (int j = 0; j < nd; j++)
                {
                    string q = p + ".particleSystemsDefinition[" + j + "]";
                    list[j] = new CollisionAndParticles.ParticleCollisionDefinition
                    {
                        particleSystemRef = def.GetString(q + ".particleSystem"),
                        receiveCollisionMessage = def.GetBool(q + ".receiveCollisionMessage"),
                        // `collisionEvent` 是 UnityEvent，数据里没有 ⇒ 留空事件（没有订阅者时就是静默）
                    };
                }
                cap.particleSystemsDefinition = list;
                arr[i] = cap;
            }
            collisionAndParticles = arr;
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
            Debug.LogWarning("[WarpforgeVFX] Collisions：**数据里 `particleSystemsDefinition` 这一层是空的**" +
                             "（已遇 " + CapsWithNoDefinitions + " 条）—— 原始 dump 把它记成了 `<深>`，" +
                             "`工具/gen_animfx_modules.py` 故意跳过 ⇒ **平面落不到任何粒子系统上**。" +
                             "这不是模块没实现，是数据侧缺一层：修法是重跑 `工具/dump_animfx.py` 让它下沉这层，" +
                             "或自制特效在 Inspector 里手填 `particleSystemsDefinition`。" +
                             "详见 WFModuleCollisions.cs 文件头「没还原的」。");
        }
    }
}
