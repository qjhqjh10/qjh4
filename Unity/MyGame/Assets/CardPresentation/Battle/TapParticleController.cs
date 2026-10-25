// TapParticleController.cs — 点战场地面的反馈（原版同名类 · 独立组件的**按下沿 + 物理射线**）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【判据正本】`Unity/资料/完全复刻/判据/Tap族_地面点击反馈.md`（§1 路由 / §3.2 伪码 / §4 八道闸）。
//   反编译原文（**第一权威**，逐行读过）：`D:/2/tools/decomp_full/TapParticleController__Update.c`
//   （385 行、**0 条 `//IL_` 注记**、控制流完整）+ `__Awake.c` / `__Initialize.c`。
//   ⚠️ 判据文件 §2 那张表把 `CheckCollision` 的归属写成了 `TapHitMaterialDefinition` 的 ——
//      实际是 **`TapParticleController.CheckCollision`**（`ILSPY/TapParticleController.cs:475`），
//      而且它**零调用点**（逻辑已内联进 `Update`）。本类不实现它。
//
// 【原版行为（顺序即代码顺序）】
//   ① `battleManager == null` ⇒ return
//   ② `battleStage != MatchProper(50)` ⇒ return（换牌期 40 / 战后 60 都不播）
//   ③ `WindowsManager.currentWindow != null` ⇒ return（**任何窗开着就整帧不收**）
//   ④ `EventSystem.current == null` ⇒ return
//   ⑤ 移动端分支取 `pointerId`（桌面恒 −1；见判据 §3.4 B-1，本类走桌面口径）
//   ⑥ `IsPointerOverGameObject(pointerId)` ⇒ return（指针压在 uGUI 射线靶上）
//   ⑦ **`!Input.GetMouseButtonDown(0)` ⇒ return（按下沿，不是松手）**
//   ⑧ `Camera.main` 取不到 ⇒ return
//   ⑨ `ray = cam.ScreenPointToRay(mousePosition)`
//   ⑩ **`Physics.RaycastNonAlloc(ray, hits[3], float.MaxValue, 0x40000001)`**
//   ⑪ `n < 1` ⇒ return
//   ⑫ 挑 **`hit.distance` 最小**那条（`RaycastNonAlloc` 不排序）；**不按组件过滤**
//   ⑬ 那条身上的 `TapHitMaterialDefinition`；**取不到 ⇒ return（⛔ 不顺延到下一条）**
//   ⑭ 冷却闸 → `PlayParticle(hit.point, hit.normal, def.targetAnchor)` → 置冷却
//   另：`Initialize()` 里 `enabled = (matchType != Tutorial(100))` —— **只有教程局整件关掉**。
//
// 【三条别推翻（踩过/判据明确）】
//   · ⛔ **不要复用 `BattleDriver._downEdge`**：那是一次性边沿（谁先取谁得）。原版是**另一个组件
//     自己读输入**。本类自己读 `Mouse.current.leftButton.wasPressedThisFrame`（本工程只有新输入系统，
//     `ProjectSettings.asset activeInputHandler: 1`）。
//   · ⛔ **不要给死字段补逻辑**（`useTargetAnchor` / `instancedParticle`）。
//   · ⛔ **不要为了让「点棋盘」有反馈去改层**：掩码 = 原版 `0x40000001` = layer 0 + 30；
//     棋盘那颗 `CardLowBoardLimit` 在原版是 **layer 11** ⇒ **原版射线本来就打不到它**。
//     照原版建、照原版设层，别自己发明。
//
// 【与 `BattleDriver` 的接口（只读，不共享边沿）】
//   · `BattleDriver.Ctx.MatchType` / `Ctx.IsOver` / `InMulligan` —— 对局模式与战斗阶段那两道闸
//   · `BattleDriver.PointerOverBlockingUi()` —— **模态窗闸**（内部 = 设置/聊天/换牌/选牌/多卡/放大/联盟/日志）。
//     ⚠️ 判据 §5.2-4 建议由 `BattleDriver` 暴露一个 `TapBlocked`；本批**不改 `BattleDriver`**（文件归他人），
//     直接复用现成的公开口 `PointerOverBlockingUi()`（它的实现就是「八扇模态窗任一 `Visible`」）。
//   · `BattleDriver.PointerOverUiForTest` —— 自检钉死「指针在 UI 上」那一位（与 `BattleDriver.PointerOverUi`
//     共用同一个测试开关；生产恒 `null` ⇒ 走真 `EventSystem`）。
//
// 【生命周期】原版挂在 `BattlePrefab/BattleBoardElements/TapParticleController`（空物体、只挂脚本），
//   由 `InitializableBehaviour.Start → SceneInitializer.QueueForInitialization` 驱动 `Initialize()`。
//   我们：`ArenaBuilder` 把它建进战场 prefab 根下（名字沿用原版），运行时自己等到 `BattleDriver`
//   出现再 `Initialize()`（见 `Bind()`）。**教程局 / 拿不到驱动 ⇒ `enabled=false`，一帧都不耗。**
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.InputSystem;          // `Mouse.current`（New Input System only）

namespace CardPresentation
{
    /// <summary>点地面反馈的**触发器**（原版 `TapParticleController`）。
    /// 每帧自己做「按下沿 + 物理射线」，命中后交给 `TapHitMaterialDefinition` 播。</summary>
    [DisallowMultipleComponent]
    public class TapParticleController : MonoBehaviour
    {
        /// <summary>原版 `collisionMask.m_Bits = 1073744897`（13 个场景逐份同值）=
        /// layer **0**（Default）+ layer **30**（`GroundCollider`，见原版 `TagManager`）。</summary>
        public const int CollisionMask = 0x40000001;

        /// <summary>原版 `new RaycastHit[3]`（构造函数里定的上限）。</summary>
        public const int MaxHits = 3;

        /// <summary>粒子落点的法线预偏移（原版常量 `0x1834b2dc4` = `0.1f`，已直读 `GameAssembly.dll`）。</summary>
        public const float NormalOffset = 0.1f;

        /// <summary>绑不上 `BattleDriver` 时的放弃时限（秒）。超时后**出声并永久关闭**，不空转。</summary>
        const float BindGiveUpSeconds = 10f;

        readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

        BattleDriver _driver;
        bool _initialized;
        float _firstFrameTime = -1f;
        bool _warnedNoDriver, _warnedNoCamera, _warnedNoMouse, _warnedNoEventSystem;

        /// <summary>场景里那一只（自检读它；**别在别处再 FindObjectOfType**）。</summary>
        public static TapParticleController Instance { get; private set; }

        /// <summary>原版序列化字段 `debugCollisions`（13 场全为 0 ⇒ 默认关）。
        /// 打开后打的行与原版逐字一致（`Detected N collisions` / `Checking collision against …` / `Selected …`）。</summary>
        public bool debugCollisions;

        // ---- 自检读口（只读）----
        public bool InitializedForTest { get { return _initialized; } }
        public int LastHitCountForTest { get; private set; }
        public TapHitMaterialDefinition LastSelectedForTest { get; private set; }
        /// <summary>真正播出去过几次（过了全部闸 + 有定义 + 过冷却）。</summary>
        public int PlaysForTest { get; private set; }

        void Awake()
        {
            Instance = this;
            // 原版 `Awake` 是「先关掉，等 `Initialize()` 再按模式打开」（`TapParticleController__Awake.c`：
            //   `Behaviour.set_enabled(0)`）。我们那张「初始化队列」不存在 ⇒ 用一个自绑定的 `Bind()`
            //   代替：拿不到 `BattleDriver` 之前**只做一次便宜的查找**，不跑射线、不碰输入。
            enabled = true;
            _firstFrameTime = -1f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!_initialized)
            {
                Bind();
                return;
            }
            Tick();
        }

        /// <summary>等 `BattleDriver` + `Ctx` 就绪，然后照原版 `Initialize()` 决定开不开。
        /// 拿不到就**每 0.5 s** 重试一次（不是每帧 `FindFirstObjectByType`）；超过 `BindGiveUpSeconds`
        /// 出声并永久关闭（独立战场预览场景本来就没有 `BattleDriver`）。</summary>
        void Bind()
        {
            if (_firstFrameTime < 0f) _firstFrameTime = Time.realtimeSinceStartup;

            if (_driver == null)
            {
                float now = Time.realtimeSinceStartup;
                if (now - _firstFrameTime < 0.5f) return;      // 节流：0.5 s 一次
                _driver = Object.FindFirstObjectByType<BattleDriver>(FindObjectsInactive.Include);
                if (_driver == null)
                {
                    if (now - _firstFrameTime > BindGiveUpSeconds)
                    {
                        if (!_warnedNoDriver)
                        {
                            _warnedNoDriver = true;
                            Debug.LogWarning("[Tap] 本场景没有 `BattleDriver`（独立战场预览？）"
                                           + " ⇒ 点地面反馈**保持关闭**（原版那条初始化链也不会跑）。");
                        }
                        enabled = false;                    // 永久关掉，不空转
                    }
                    return;
                }
            }
            if (_driver.Ctx == null) return;                    // 引擎还没起局

            Initialize();
        }

        /// <summary>原版 `Initialize()`：`enabled = (battleManager.matchType != 100)`。
        /// `MatchType.Tutorial = 100`（`ILSPY/MatchType.cs`）⇒ **只有教程局整件关掉**，其余 17 档全开。
        /// <para>公开是为了自检能显式调（批处理没有帧循环）。</para></summary>
        public void Initialize()
        {
            if (_initialized) return;
            if (_driver == null) _driver = Object.FindFirstObjectByType<BattleDriver>(FindObjectsInactive.Include);
            if (_driver == null || _driver.Ctx == null) return;

            _initialized = true;
            bool on = _driver.Ctx.MatchType != RuleEngine.MatchType.Tutorial;
            enabled = on;
            if (!on)
                Debug.Log("[Tap] 教程局（`MatchType.Tutorial = 100`）⇒ 点地面反馈**整件关闭**（原版 `Initialize` 同）");
        }

        /// <summary>一帧的判定（全部照原版顺序）。</summary>
        void Tick()
        {
            var ctx = _driver != null ? _driver.Ctx : null;
            if (ctx == null) return;                                        // ①

            // ② battleStage == MatchProper(50)。我们的等价物 = 「不在换牌期、对局没结束」
            //    （原版 40 = 换牌、60 = 战后；我们这两个状态就是 `MulliganOpen` / `IsOver`）。
            if (_driver.InMulligan) return;
            if (ctx.IsOver) return;

            // ③ 任何模态窗开着 ⇒ 整帧不收（原版 `WindowsManager.currentWindow != null`）。
            if (_driver.PointerOverBlockingUi()) return;

            // ④ EventSystem
            var es = BattleEventSystem.Instance;
            if (es == null) es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                if (!_warnedNoEventSystem)
                {
                    _warnedNoEventSystem = true;
                    Debug.LogWarning("[Tap] 没有 `EventSystem` ⇒ 点地面反馈这一帧不做事"
                                   + "（原版 `.c:87-92` 同；跑一次 `BattleEventSystem.Ensure()` 即可）");
                }
                return;
            }

            // ⑥ 指针压在 uGUI 射线靶上 ⇒ 不算点地面（原版 `IsPointerOverGameObject(pointerId)`，桌面 −1）
            if (PointerOverUi()) return;

            // ⑦ 按下沿（⛔ 不是松手；⛔ 不用 `BattleDriver` 的那条一次性边沿）
            var mouse = Mouse.current;
            if (mouse == null)
            {
                if (!_warnedNoMouse)
                {
                    _warnedNoMouse = true;
                    Debug.LogWarning("[Tap] `Mouse.current` 是 null（本工程 New Input System only）"
                                   + " ⇒ 点地面反馈收不到按下沿。");
                }
                return;
            }
            if (!mouse.leftButton.wasPressedThisFrame) return;

            // ⑧ 相机：**照原版取 `Camera.main`**；取不到退 `BattleDriver.cam` 并出声（⛔ 不静默）
            var cam = Camera.main;
            if (cam == null && _driver != null) cam = _driver.cam;
            if (cam == null)
            {
                if (!_warnedNoCamera)
                {
                    _warnedNoCamera = true;
                    Debug.LogWarning("[Tap] `Camera.main` 与 `BattleDriver.cam` 都取不到 ⇒ 点地面反馈跳过"
                                   + "（原版 `.c:140-144` 取不到就整段不做事）。");
                }
                return;
            }

            // ⑨⑩ 射线
            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            int n = Physics.RaycastNonAlloc(ray, _hits, float.MaxValue, CollisionMask);
            LastHitCountForTest = n;
            if (debugCollisions)
                Debug.Log($"[{Time.frameCount}] Detected {n} collisions");
            if (n < 1) return;

            // ⑫ 取 `distance` 最小那条（`RaycastNonAlloc` **不排序**）
            int best = 0;
            float bestDist = _hits[0].distance;
            for (int i = 1; i < n; i++)
                if (_hits[i].distance < bestDist) { bestDist = _hits[i].distance; best = i; }
            var hit = _hits[best];

            if (debugCollisions)
            {
                for (int i = 0; i < n; i++)
                {
                    var c = _hits[i].collider;
                    bool has = c != null && c.GetComponent<TapHitMaterialDefinition>() != null;
                    Debug.Log($"Checking collision against {(c != null ? c.name : "<null>")}. "
                            + $"Distance {_hits[i].distance}. Has Material definition {has}");
                }
                Debug.Log($"[{Time.frameCount}] Selected {(hit.collider != null ? hit.collider.name : "<null>")}");
            }

            // ⑬ 最近那条没挂定义 ⇒ **白点一下**（⛔ 不顺延到下一条）
            var def = hit.collider != null ? hit.collider.GetComponent<TapHitMaterialDefinition>() : null;
            LastSelectedForTest = def;
            if (def == null) return;

            // ⑭ 冷却闸
            if (def.delayTimeToPlayAgain != 0f && def.remainingNotPlayTime > 0f) return;

            def.particleDefinition.PlayParticle(hit.point, hit.normal, def.targetAnchor);
            def.enabled = true;                                // 自己的 `Update` 从这一帧开始减冷却
            def.remainingNotPlayTime = def.delayTimeToPlayAgain;
            PlaysForTest++;
        }

        /// <summary>「指针压在 uGUI 的射线靶上吗」—— 与 `BattleDriver.PointerOverUi()` **同判据**
        /// （原版 `EventSystem.IsPointerOverGameObject(pointerId)`，桌面 `pointerId = -1`）。
        /// <para>⚠️ 那一条是 `private`、且 `BattleDriver.cs` 本批不归我改 ⇒ 这里照同一判据写一份（三行）。
        /// 自检开关共用一个：`BattleDriver.PointerOverUiForTest`（生产恒 `null`）。</para></summary>
        bool PointerOverUi()
        {
            if (BattleDriver.PointerOverUiForTest.HasValue) return BattleDriver.PointerOverUiForTest.Value;
            var es = BattleEventSystem.Instance;
            if (es == null) es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
