// AnimFXController.cs — 原版 `AnimFXController`（**2026-10-11 · A196 移植**）
//
// 为什么要有它：原版 `TauCannonAnimationStopper.animFXController`（炮塔场 `tauviorla` 的
// `Railgun turret` 节点上那一个）就是**这个类**；而且 `Toggle(option)` 的**第 ③ 支整支被
// `animFXController != null` 挡着**（关 `animationComponent` + 炮塔转回 `finalRotation`）——
// 我们工程里原来**没有这个类**⇒ 那一支照抄就是不执行、`Railgun turret` 上也挂不上东西。
//
// 🔴 **本类与 `WarpforgeVFX.WarpforgeEffectPlayer` 的关系（先读这段，别踩）**
// ------------------------------------------------------------------
//   原版**一个类用在两个角色上**；本仓把它拆成了两边，各自都有明确的判据：
//     · **特效 prefab 那条线**（958 件效果）的控制器 = **`WarpforgeEffectPlayer`**
//       （`WarpforgeVFX`，按 `animfx_modules.json` 的**数据装配**）。那边的文件头写着
//       「本类就是这条线上唯一的控制器，别再加第二个 controller」—— **那条规矩仍然成立**。
//     · **战场场景侧**（本件 A196）= **本类**：挂在原版的场景对象上、由别的组件只做两件事
//       （`!= null` 当开关 + `enabled = option`），**不装配任何来自 `animfx_modules.json` 的模块**。
//   ⇒ **⛔ 别把本类挂到 `WarpforgeVFX/Prefabs/**` 的任何一件特效上**（两套销毁计时会互相打架）；
//     反过来也别让 `WarpforgeEffectPlayer` 去当场景侧那个 `animFXController`（它要 `WFEffectEntry`）。
//
// 判据（第一权威，逐字对照）
// ------------------------------------------------------------------
//   · 签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXController.cs`
//   · 方法体：`d:/2/tools/decomp_full/AnimFXController__{ctor,OnEnable,Update,Exit,DoDestroy,SetData,PlaySound,get_ActingCard,get_TargetCard}.c`
//
//   **字段 = 偏移 → 值**（`.c` 里逐句读出来的；这个顺序与签名桩的声明顺序**完全一致**）：
//   | 字段 | 偏移 | 来源 |
//   |---|---|---|
//   | `sounds` / `exitSounds` | +0x20 / +0x28 | `Update` 里按数组遍历（`+0x18` 长度 / `+0x20` 数据） |
//   | `preventDestroy` | +0x30 | `OnEnable` / `Exit` 的守卫 |
//   | `destroyTime` | +0x34 | **ctor 写 `0x40800000` = 4f** |
//   | `modules` | +0x38 | **ctor 里 `List<>..ctor`** |
//   | `exitDestroyTime` | +0x40 | **ctor 写 `0x40400000` = 3f**（= 签名桩那个 `SAFE_DESTROY_TIME`） |
//   | `OnFinished` | +0x48 | `Exit` 开头那个委托调用 |
//   | `actingCard` / `targetCard` | +0x50 / +0x58 | `SetData` 两句赋值 · 两个 getter 各返回一个 |
//   | `currentTime` / `exiting` | +0x60 / +0x64 | `OnEnable` 置 0 · `Exit` 置 1 |
//
//   **四个方法体**：
//   ```
//   OnEnable(): currentTime = 0;  if (destroyTime > 0 && !preventDestroy) Destroy(gameObject, destroyTime);
//   Update():   currentTime += Time.deltaTime;
//               foreach (s in sounds)     s.Update(currentTime, transform);
//               if (exiting) foreach (s in exitSounds) s.Update(currentTime, transform);
//   Exit():     OnFinished?.Invoke();                 // ⚠️ 原版**不置空**、也**不防重入** ⇒ 照抄
//               foreach (m in modules) m.Exit();      // 虚表槽 0x188
//               exiting = true;
//               if (!preventDestroy) Destroy(gameObject, exitDestroyTime);
//   DoDestroy(): if (modules 为空) Destroy(gameObject); else foreach (m in modules) m.DoDestroy();  // 槽 0x198
//   SetData(a,t): actingCard = a; targetCard = t; foreach (m in modules) m.Initialize(this);         // 槽 0x178
//   ```
//   （三个虚表槽的**先后**给出了 `AnimFXModuleBase` 的声明序 `Initialize < Exit < DoDestroy`。）
//
//   **场景侧实测**（`bundle_scenes_scenes_battlearenatauviorla/MonoBehaviour/MonoBehaviour_{4767,5247,5360,5474}.json`）：
//   **tauviorla 这 4 个**实例，**全部 `preventDestroy = 1`**（⇒ 一个都不会自毁）、`exitSounds = []`、
//   `exitDestroyTime = 3.0`；挂在 `Railgun turret`（trauviorla 两个炮塔各一个）上的那两个
//   `destroyTime = 1.8`、`modules = []`、各有 **1 条 `sounds`**（3D、`repeat=1`/`loops=5`/`timeInterval=0.75`）。
//   ⚠️ **2026-10-12（A340）订正**：这 4 个**只是 tauviorla 那一场**的；全库 15 个 `scenes_scenes_*` 包里
//      一共 **7** 个实例，另 3 个（`battlearena2` 的 `RocketTrail` · `battlearena3` 的两个 `Lightning_Green`）
//      **`preventDestroy = 0`**（判据与逐条表在 `资料/普查产出_1012/W3_AnimFX旁挂.md` §七·1）。
//   ✅ **2026-10-12（A393 数据 + A417 接线）**：那 3 个**我们这条链现在也建** ——
//      `ScenarioBlendableFactory.BuildSceneAnimFx`（读旁挂 `sceneStandalone` / `sceneStandaloneBuild`）
//      建在 `Warpforge_<场>` 之下、按**传进来的那个 root** 去重；调用点 = `ArenaRuntimeLoader.Load`
//      （`ArenaRuntimeLoader.cs:114`，**只此一处**）。
//      ⚠️ 另一处**故意不接**：`EnvironmentApplier.EnsureSceneBlendables`（① 它第一次跑已经是「打出一张
//      进攻卡」⇒ 这 5 条大半局都不存在，与原版不符；② 它的 root 是 `Arena3D`、与去重键不同
//      ⇒ 两个都接会建两遍）。判据 → `ScenarioBlendables.cs` 文件头 A393 那一节。
//      ⇒ 「自毁」那条支路**走得到了**。⚠️ 但**具体是几个**要按原版的开关算，别一律说「3 个都会自毁」：
//        真会排销毁的只有 **`battlearena2` 的 `RocketTrail`** 那一个（`preventDestroy = 0` ∧ 组件
//        `m_Enabled = 1` ∧ GO 开着 ∧ `destroyTime = 6`）；`battlearena3` 那两个组件原版就是关的
//        ⇒ 按 A393 的口径 `selfDestroyOk = false`、**不排销毁**。落地那一句在 `ScenarioBlendables.MakeAnimFx`。
//
// ⚠️ **有意的偏离（五处，都有出处，别当缺陷改）**
//    （A420 订正：这行原来写「四处」—— ⑤ 是 A341 补的、当时没跟着改这一个数。）
// ------------------------------------------------------------------
//   ① `PlaySoundOnTime.sound`：原版是 `AudioCue`（一个资产）。**本工程里声音的唯一载体是
//      「cue 名」**（`WarpforgeVFX.WFSoundBank` 的表键；`WarpforgeEffectPlayer` 那条线也是这么存的）
//      ⇒ 这里用 `string`。**当且仅当**将来把 cue 资产也导进工程，这里才该改回引用型。
//   ② `SoundManager.Play3D/Play2D` → **`WarpforgeVFX.WFSoundPlayer`**（那条线已经实现过一次的同一件事：
//      随机挑 clip + 音高/音量随机 + 2D/3D）。⚠️ **本类不自己挑 clip、不自己随机** —— 那是
//      `WFSoundBank`/`WFSoundPlayer` 的判据（本仓铁律：同一条规则只留一处）。
//   ③ `List<AnimFXModuleBase>` → **`List<WarpforgeVFX.WFEffectModule>`**（我们这条线的模块基类）。
//      `Exit()` / `DoDestroy()` 两个广播**逐字一致**（都是无参虚方法）；**只有 `SetData` 里那句
//      `m.Initialize(this)` 落不了地** —— 我们的 `WFEffectModule.Initialize` 收的是
//      `WarpforgeEffectPlayer`（另一条线的控制器）⇒ 见方法体那条**出声**，**不静默**。
//      （实测：场景侧一共 **7** 个实例（15 个 `scenes_scenes_*` 包全扫），**6 个 `modules` 是空的**、
//        第 7 个（`AnimFXModuleScreenShake`）挂在 `Railgun BIG (1)` 上。
//        ⚠️ 原来这里写「4 个实例里 3 个空」—— 那是**只数了 tauviorla 那 4 个**的旧计数，
//          2026-10-12（A340）全库重扫后订正，见 `资料/普查产出_1012/W3_AnimFX旁挂.md` §七·1。
//        🔴 **2026-10-12（A393）再订正**：原来还写着「它不归任何 blendable 管 ⇒ 这个缺口今天走不到」——
//          **不成立了**：`Railgun BIG (1)` 正是 `sceneStandalone` 那 5 条之一（A393），
//          `BuildSceneAnimFx` 会把它建出来，工厂**建的那一刻**就为它那一层出声
//          （`ScenarioBlendables.MakeAnimFx` → `WarnAnimFxModules`，`ScenarioBlendables.cs:1696`）
//          ⇒ 这一档从「走不到」变成了「**会走到、但还没有模块线**」。要真做得多建一条**场景侧模块线**
//          （路线见 `资料/普查产出_1012/H2_场景侧AnimFX.md` §四 · A394，**仍开着**）。
//          ⚠️ 这里说的出声是**工厂自己那句**，不是 `SetData` 里那句 —— 原版那个模块挂在
//          `actionStart = Initialize` 上（= 靠 `SetData` 触发），而 `SetData` 的调用点全是**卡**的
//          ⇒ 场景侧这一条**会不会真触发、我们还没查清**（见 H2 §七·3，**别写成「会触发」**）。）
//   ④ `CardScript actingCard/targetCard` → **`Transform`**：我们工程里没有 `CardScript` 这个类，
//      「一张卡」的可比载体是它的 `Transform` —— 与 `WFEffectCardContext.actingCard/targetCard`
//      **同一类型、同一语义**（判据：`WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:101-110`
//      那段注释写的就是「原版 `AnimFXController.actingCard` / `.targetCard` 的对应物」）。
//   ⑤ 🔴 **`preventDestroy` 的字段默认值改成 `true`**（原版 ctor 不碰它 ⇒ 默认 `false`，真值来自场景序列化）：
//      我们是**运行时 `AddComponent`**，而 `AddComponent` 会**先跑一次 `OnEnable`** —— 那一刻字段还是默认值，
//      `Destroy(gameObject, 4f)` **排定了就取消不掉** ⇒ 默认必须是「不自毁」那一档。
//      ✅ **2026-10-12（A341）工厂那条路已经补上了**（`ScenarioBlendables.MakeAnimFx` 末尾照抄了
//      `OnEnable` 的第二句 `if (!preventDestroy && destroyTime > 0) Destroy(go, destroyTime)`）
//      ⇒ 这一档**不再**是缺口；原地那句「将来真出现 `preventDestroy = false` 的实例要补一次」已兑现。
//      ⚠️ 这一条**不是**「拿默认值顶数据」：工厂那边**仍然要求旁挂里有这个字段**，没有就**不建 + 出声**。
//
// ✅ **旁挂里的三层（2026-10-12 · A340 起【已收】）**：`sounds` / `exitSounds` / `modules` 原来
//   **没收进旁挂**（都是数组、元素还带外部资产引用，`pack_fields` 解不了）⇒ 工厂建出来的实例这三项
//   恒空、还会打一条警告。现在 `gen_env_blendables.py` 另开了 `pack_animfx_defs`（照 `pack_controller_defs`
//   那套摊平成 `TargetField`，键名 `sounds.<i>.time/sound/is2d/repeat/loops/timeInterval` 等）：
//     · `sounds` / `exitSounds` —— **两层都建得出来**：`sound` 那条跨包引用（原版 `AudioCue` 在
//       `soundcollection_assets_all` 里）由生成器的 `CueNames` 解析成 **cue 名**（= `animfx_sounds.json`
//       的键，`WarpforgeVFX.WFSoundBank` 认的就是它）⇒ 运行时按 `PlaySoundOnTime` 逐字段装配。
//     · `modules` —— ⚠️ **仍然建不出来**（如实记着）：场景侧没有 `AnimFXModuleBase` 的对应物
//       （特效那条线的 `WFEffectModule.Initialize` 收的是 `WarpforgeEffectPlayer`，见偏离 ③）
//       ⇒ 旁挂只把**类名**收下来（`modules.<i>`），运行时有模块就**点名出声**（`WarnAnimFxModules`）。
//       场景侧 7 个实例里只有 1 个有模块（`AnimFXModuleScreenShake`，挂在 `Railgun BIG (1)` 上）——
//       🔴 **2026-10-12（A393）订正**：原来这句接着写「它不归任何 blendable 管 ⇒ 今天走不到」，
//       **不成立了**：它是 `sceneStandalone` 那 5 条之一 ⇒ 工厂建它的时候 `WarnAnimFxModules`
//       会**真的出声**（不是「走不到」了）。要真建得先给场景侧一条模块线（A394，仍开着）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `PlaySoundOnTime`（`[Serializable]`，住在 `PlaySoundOnTime.cs`）。
    /// **放在本文件里**：它是本类**独有**的依赖（全工程只有 `AnimFXController.sounds/exitSounds` 用它），
    /// 而本件的白名单只开了这一个新文件 —— 单开一个 `PlaySoundOnTime.cs` 反而越界。
    ///
    /// 判据 = 签名桩 ＋ `d:/2/tools/decomp_full/PlaySoundOnTime__{ctor,Update,Reset}.c`。
    /// 字段偏移（`[Serializable]` 不是 MonoBehaviour ⇒ 从 +0x10 起）：`time +0x10` ·
    /// `sound +0x18` · `is2d +0x20` · `repeat +0x21` · `loops +0x24` · `timeInterval +0x28` ·
    /// `numberOfTimesPlayed +0x2c`（私有、非序列化）。</summary>
    [Serializable]
    public class PlaySoundOnTime
    {
        /// <summary>从**开始计时的那一刻**起算的第几秒播第一次（`Update` 收的 `currentTime`）。</summary>
        public float time;

        /// <summary>原版是 `AudioCue`（资产引用）。⚠️ **有意偏离 ①**（见文件头）：本工程按 **cue 名** 存，
        /// 玩法与 `WarpforgeVFX.WFSoundBank` 的表键一致；查不到会**出声**（不静默）。</summary>
        public string sound;

        /// <summary>true = 2D（不定位）· false = 3D（在 `transform.position` 上响）。
        /// 判据 = `PlaySoundOnTime__Update.c`：`if (!is2d && transform != null) Play3D else Play2D`。</summary>
        public bool is2d;

        /// <summary>要不要重复（**不是**「无限重复」）：`总次数 = repeat ? loops : 1`。</summary>
        public bool repeat;

        /// <summary>重复模式下的**总次数**（判据同 `repeat` 那条）。</summary>
        public int loops;

        /// <summary>两次之间的间隔（秒）。第 k 次（0 基）的门槛 = `k * timeInterval + time`。</summary>
        public float timeInterval;

        /// <summary>已经播过几次（原版 `private int numberOfTimesPlayed`，**不序列化**）。</summary>
        int numberOfTimesPlayed;

        /// <summary>判据 = `PlaySoundOnTime__Update.c`（四条判据，顺序照原版）：
        /// ① `总次数 <= 已播` ⇒ return ② `currentTime < 已播 * 间隔 + 起始` ⇒ return
        /// ③ `!is2d && transform != null` ⇒ 3D，**否则 2D**（`transform` 为空也走 2D —— 反编译里那条
        ///    `op_Implicit(transform)` 为假时就是落到 `Play2D`）④ 播完 `已播 + 1`。</summary>
        public void Update(float currentTime, Transform transform)
        {
            int total = repeat ? loops : 1;                       // 判据 ① 的那句三元（`0x21` 与 `0x24`）
            if (total <= numberOfTimesPlayed) return;
            if (currentTime < numberOfTimesPlayed * timeInterval + time) return;

            bool spatial = !is2d && transform != null;
            Play(spatial ? transform.position : Vector3.zero, !spatial);

            numberOfTimesPlayed++;
        }

        /// <summary>判据 = `PlaySoundOnTime__Reset.c`（只有一句 `numberOfTimesPlayed = 0`）。</summary>
        public void Reset() { numberOfTimesPlayed = 0; }

        /// <summary>把 cue 名交给**那条线唯一的那份实现**（`WarpforgeVFX.WFSoundPlayer` 播、
        /// `WFSoundBank` 解 cue 与随机化）。⛔ 别在这里另写一套挑 clip / 随机的逻辑。</summary>
        void Play(Vector3 worldPos, bool is2d) { AnimFXController.PlayCue(sound, worldPos, is2d); }
    }

    /// <summary>原版 `AnimFXController` —— **战场场景侧**的那个控制器（与 `WarpforgeEffectPlayer`
    /// 的分工见文件头那段红字）。**能走到我们工厂的**实例都 `preventDestroy = 1`（不自毁）；
    /// ⚠️ 全库场景侧 7 个里另有 3 个是 `0` —— 🔴 **2026-10-12（A393）起那 3 个也走我们的工厂了**
    /// （`BuildSceneAnimFx` 那条链），所以「自毁」这一档**已经真的会走到**（见文件头那次订正）。
    /// <para>🔴 **2026-10-14（A552）就地补上 `[DisallowMultipleComponent]`（用户拍板「照原版补」）** ——
    /// 判据：`Battle/ScenarioBlendables.cs` 文件头那段自己写着「`AnimFXController` **没有**
    /// `[DisallowMultipleComponent]` ⇒ 同一个对象上会**静默多一颗**」「组件重复挂这一档**今天没有任何守卫**」
    /// （WB2 报告 §六 顺手发现）。补上之后：重复挂**不会发生**，而 `AddComponent` 那一处
    /// （`ScenarioBlendables.cs` 的 `host.AddComponent<AnimFXController>()`）**本来就有 `c == null` 的出声支**
    /// ⇒ 一次静默重挂变成**一条出声的告警**（本仓红线：不许静默失败）。
    /// ⚠️ **如实标**：原版**读不到**这条属性（IL2CPP 把 attribute 剥了）⇒ 这不是「照原版抄的」，
    /// 是**按本仓红线条 + 现场注释**补的守卫。</para></summary>
    [DisallowMultipleComponent]
    public class AnimFXController : MonoBehaviour
    {
        /// <summary>原版常量（签名桩里的 `private const float SAFE_DESTROY_TIME = 3f`）。
        /// `.c` 的 ctor 里没有它的痕迹 —— 它是 `exitDestroyTime` 的**默认值**那一档（ctor 写的就是 3f）。</summary>
        public const float SAFE_DESTROY_TIME = 3f;

        // ---- 序列化字段（顺序**照原版声明序**，见文件头那张表）----
        // ⚠️ 原版这四个都是 `[SerializeField] private`；我们由 `ScenarioBlendables` 的工厂
        //    **在运行时装配**（与这条线其它类同一个口径）⇒ 字段公开。

        /// <summary>特效开始后按时间播的音效（`Update` 遍历它）。</summary>
        public PlaySoundOnTime[] sounds = new PlaySoundOnTime[0];
        /// <summary>`Exit()` 之后按时间播的音效（`exiting == true` 时才遍历它）。</summary>
        public PlaySoundOnTime[] exitSounds = new PlaySoundOnTime[0];

        /// <summary>true = **不由自己管销毁**（`OnEnable` 与 `Exit` 两句销毁都被它挡着）。
        /// 🔴 它来自**数据**：工厂建组件时**必须**问到它（旁挂里没有 ⇒ 不建、出声）。
        /// 🔴🔴 **默认值是 `true`（有意偏离 ⑤，别改成 `false`）**：原版 ctor 不碰它（默认 `false`，
        ///   真值来自**场景序列化**）；而我们是**运行时 `AddComponent`** —— `AddComponent` 会**先跑一次
        ///   `OnEnable`**（那时字段还是默认值），`Destroy(gameObject, 4f)` **一旦排定就取消不掉**
        ///   ⇒ 默认必须是「不自毁」那一档，否则工厂刚建好组件、宿主就在 4 秒后自己没了。
        ///   ✅ **2026-10-12（A341）**：`preventDestroy = false` 那一档**已由工厂补上** ——
        ///   `ScenarioBlendables.MakeAnimFx` 设完字段后照抄 `OnEnable` 的第二句再排一次自毁
        ///   （批处理/编辑模式那一档走 `DestroyImmediate`，见那里注释与 `SelfDestroyScheduled`）。</summary>
        public bool preventDestroy = true;

        /// <summary>多久后自己销毁（**原版 ctor 的默认值就是 4f**）。`preventDestroy` 为真时不起作用。</summary>
        public float destroyTime = 4f;

        /// <summary>原版 `List<AnimFXModuleBase>` → 我们的 `WarpforgeVFX.WFEffectModule`（**有意偏离 ③**）。
        /// ⚠️ 2026-10-12（A340）：旁挂把原版那层的**类名**收进来了（`modules.<i>`），但**不建** ——
        /// 场景侧没有模块基类（`WFEffectModule.Initialize` 收的是另一条线的控制器）⇒ 工厂
        /// `WarnAnimFxModules` 有模块就点名出声。见文件头「旁挂里的三层」。</summary>
        public List<WarpforgeVFX.WFEffectModule> modules = new List<WarpforgeVFX.WFEffectModule>();

        /// <summary>`Exit()` 之后再活多久才真销毁（**原版 ctor 的默认值就是 3f**）。</summary>
        public float exitDestroyTime = SAFE_DESTROY_TIME;

        /// <summary>原版 `public Action OnFinished;`（**字段、不是 event**）。`Exit()` **开头**发一次。
        /// ⚠️ 原版发完**不置空**、`Exit()` 也**不防重入** ⇒ 连调两次会发两次（照抄，别「顺手修好」）。</summary>
        public Action OnFinished;

        // ---- 运行时状态 ----
        Transform actingCard;      // 原版 `CardScript actingCard`（有意偏离 ④）
        Transform targetCard;
        float currentTime;         // 原版 +0x60：`OnEnable` 置 0、`Update` 累加
        bool exiting;              // 原版 +0x64

        /// <summary>原版 `get_ActingCard`（`return *(this + 0x50)`）。</summary>
        public Transform ActingCard { get { return actingCard; } }
        /// <summary>原版 `get_TargetCard`（`return *(this + 0x58)`）。</summary>
        public Transform TargetCard { get { return targetCard; } }

        /// <summary>判据 = `AnimFXController__OnEnable.c`：`currentTime = 0` 然后
        /// `if (destroyTime > 0 && !preventDestroy) Destroy(gameObject, destroyTime)`。
        /// ⚠️ **每次 `enabled` 由 false 变 true 都会跑**（`TauCannonAnimationStopper.Toggle` 就是
        /// 靠 `enabled = option` 开关它的），所以「计时」会在每次重新启用时**归零** —— 照抄。</summary>
        void OnEnable()
        {
            currentTime = 0f;
            if (destroyTime > 0f && !preventDestroy)
                Destroy(gameObject, destroyTime);
        }

        /// <summary>判据 = `AnimFXController__Update.c`（三句，见文件头）。</summary>
        void Update() { Tick(Time.deltaTime); }

        /// <summary>🆕 **我们加的入口**（原版只有 `Update()`，没有这个成员 —— 如实记着）：
        /// **批处理下没有帧循环**（CLAUDE.md §三）⇒ 自检要能**手动推**才验得了「到点播没播」。
        /// 实时那条路走 `Update()` → 这里，**同一份实现**（别写第二份）。
        /// ⚠️ **play 模式下别同时手动推**（`Update` 与 `Tick` 两边都推 = 双倍速）——
        /// 与 `WarpforgeEffectPlayer.autoTick` 是同一个坑；我们**没有**那个开关
        /// （原版 `AnimFXController` 里没有那个字段，**不加**）。</summary>
        public void Tick(float dt)
        {
            currentTime += dt;

            for (int i = 0; i < sounds.Length; i++)
                if (sounds[i] != null) sounds[i].Update(currentTime, transform);

            if (exiting)
                for (int i = 0; i < exitSounds.Length; i++)
                    if (exitSounds[i] != null) exitSounds[i].Update(currentTime, transform);
        }

        /// <summary>判据 = `AnimFXController__Exit.c`（四句，顺序见文件头）。
        /// 调用点（原版）：`BattleCardUI.CleanStatusAnims` / `RemoveStatusAnim`。
        /// ⚠️ **不防重入**（原版没有那道守卫）—— 调用方自己保证只调一次。</summary>
        public void Exit()
        {
            var cb = OnFinished;
            if (cb != null) cb();                    // 原版：`OnFinished?.Invoke()`，**不置空**

            if (modules != null)
                for (int i = 0; i < modules.Count; i++)
                    if (modules[i] != null) modules[i].Exit();

            exiting = true;

            if (!preventDestroy)
                Destroy(gameObject, exitDestroyTime);
        }

        /// <summary>判据 = `AnimFXController__DoDestroy.c`：**`modules` 为空 ⇒ 销毁自己**；
        /// 否则**只广播 `DoDestroy()`、自己不动**（销毁交给模块）。调用点（原版）：
        /// `BattleManager.RemoveDestroyableAnims`。</summary>
        public void DoDestroy()
        {
            if (modules == null || modules.Count == 0)
            {
                Destroy(gameObject);
                return;
            }
            for (int i = 0; i < modules.Count; i++)
                if (modules[i] != null) modules[i].DoDestroy();
        }

        /// <summary>判据 = `AnimFXController__SetData.c`：先填两张卡，再**逐模块** `Initialize(this)`。
        /// 🔴 **有意偏离 ③**：我们那句 `Initialize` 落不了地（我们的模块基类收的是
        /// `WarpforgeEffectPlayer`）⇒ **有模块时出声点名**，不静默。
        /// 调用点（原版）：`CardScript.PlayTriggerAnim` · `CardAnimController.Initialize` ·
        /// `RemnantBody.DoDestroyByAttack` · `RemnantAeldari.CollectWaystoneEffect`。</summary>
        public void SetData(Transform actingCard, Transform targetCard)
        {
            this.actingCard = actingCard;
            this.targetCard = targetCard;

            if (modules != null && modules.Count > 0)
                Debug.LogWarning($"[AnimFX] `AnimFXController`({name}) 上有 {modules.Count} 个模块 —— "
                               + "原版这里会逐个 `Initialize(this)`，而我们的 `WFEffectModule.Initialize` "
                               + "收的是 `WarpforgeEffectPlayer`（另一条线的控制器）⇒ **这一跳我们不做**（出声，不静默）。"
                               + "判据见 `Battle/AnimFXController.cs` 文件头「有意偏离 ③」");
        }

        /// <summary>判据 = `AnimFXController__PlaySound.c`：`SoundManager.Play3D(cue, transform.position)`
        /// —— **总是 3D**（没有 `is2d` 那个开关；那个开关在 `PlaySoundOnTime` 上）。</summary>
        public void PlaySound(string cue) { PlayCue(cue, transform.position, false); }

        /// <summary>cue 名 → 响一次。**本文件里唯一的一处**（`PlaySoundOnTime.Update` 也走它）——
        /// 挑 clip / 音高 / 音量随机的判据全在 `WarpforgeVFX.WFSoundBank` + `WFSoundPlayer` 里，
        /// 这里只负责「解不出就出声、别静默」。⛔ 别在别处再抄一份。</summary>
        internal static void PlayCue(string cueName, Vector3 worldPos, bool is2d)
        {
            WarpforgeVFX.WFSoundCue cue;
            if (!WarpforgeVFX.WFSoundBank.TryGetCue(cueName, out cue))
            {
                if (cueName != null && _badCues.Add(cueName))      // 只报一次，别每帧刷屏
                    Debug.LogWarning($"[AnimFX] `AnimFXController` 的 cue `{cueName}` 在音效表里没有 —— "
                                   + "这一条不会响（出声，不静默）。跑 `工具/import_original_sfx.py` 重生成表。");
                return;
            }
            WarpforgeVFX.WFSoundPlayer.Play(cue, worldPos, is2d);
        }

        /// <summary>解不出的 cue 名（去重；与 `WarpforgeEffectPlayer` 那边同一个做法：只报一次）。</summary>
        static readonly HashSet<string> _badCues = new HashSet<string>();
    }
}
