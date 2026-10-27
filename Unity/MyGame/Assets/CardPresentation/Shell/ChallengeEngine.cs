// ChallengeEngine.cs — 「挑战」引擎的**记数口 + 挑战钮模型**（🆕 2026-10-27 · 执行代理 `B88_E3` · 账 `B22`）
//
// 它顶的是原版的哪几件（判据全部是 `.c` 实读，⛔ 不要重新论证）：
//   · `ChallengeLogMgr.TroopKilled(BattleAction)` / `LogHeal(BattleAction)` / `LogAddEffect(CardEffect)`
//     → 三条**计数口**（击杀部队数 / 累计治疗量 / 施加效果数）＋「对敌方战将伤害」那一格。
//   · `ChallengeButton.Initialize()` / `OnSignalRaise(Type)` / `Toggle(bool)` / `Awake()` 的**模型那一半**：
//     显隐判据 = `PendingPracticeMatches.Count > 0`（判据 → 账 9 的备注，⛔ 不是 `challengeManager.challenges`）、
//     收到徽标信号就重算、`Toggle` 只是把「该显」写下来（真正的 `SetActive` 在视图侧）。
//   ⛔ **`BattleEventsController.CheckHealth()` / `Update()` 不在本文件** —— 那两行本波由 `B88_E5` 在
//     `BattleDriver.UpdateHud` 里内联落地（`_foeSkullHpSeen` / `_myWarlordMinHp`），账上已判 **A**。
//     ⇒ 这里**⛔ 不重算血量差**（同一规则的第二份，迟早不一致），只留 `OnWarlordDamaged` 这个计数口。
//
// 🔴 **本文件为什么是「拉」而不是「推」**（这条决定了下游怎么接）：
//   `CheckHealth` 在原版就是**每帧读当前血量再比对**（它自己存上一次的值）⇒ 它天生是**拉**。
//   🔴 **那两行不由本文件顶**：本波 `B88_E5` 已在 `BattleDriver.UpdateHud` 里内联落地
//   （`_foeSkullHpSeen` / `_myWarlordMinHp` 缓存），账里那两行已判 **A** ⇒ 本文件
//   **⛔ 不重算血量差**（同一规则的第二份），只保留挑战计数口 `OnWarlordDamaged`。
//
// 🔴 **三条计数口为什么是「推」**：`TroopKilled` / `LogHeal` / `LogAddEffect` 在原版由**动作结算那一刻**
//   回调（`BattleAction` 里带 `CardEffect`），事件流里没有「治疗量」这种量（`ctx.Signals` 也没有 `Heal`）。
//   我们能做的是：**计数 + 记录 API 在这里（唯一一份）**，发射点留给结算侧
//   （`RuleCore` / `EffectResolver` / `BattleDriver` —— 全在白名单外）。⇒ 账里那三行的残余 = 「发射点」，
//   ⛔ **不是**「这个机制没建」。
//   ⚠️ 也**没有**去 drain `ctx.Signals` —— 那是表现层的地盘（`BattleContext.DrainSignals` 搬完即空，
//      在这里搬会把特效信号偷走，是静默的坏）。
//
// ⛔ 别在别处再记一份计数：`ChallengeData.TroopsKilled` 等五个字段是**唯一正本**。
using System;
using RuleEngine;

namespace CardPresentation
{
    public static class ChallengeEngine
    {
        // ============================================================ ① 血线计数（⛔ 不重算血量差）

        /// <summary>敌方督军掉了 <paramref name="amount"/> 血（挑战里的「对敌方战将造成 N 点伤害」那一类）。
        /// <para>🔴 **这里只记数，⛔ 不做「血量差」**：`BattleEventsController.CheckHealth` / `Update` 那两行
        /// 已经由 `B88_E5` 在本波落地（等价实现 = `BattleDriver.UpdateHud` 里的 `_foeSkullHpSeen` /
        /// `_myWarlordMinHp` 内联缓存）⇒ 在这里再比一次血量就是**同一条规则的第二份**（迟早不一致）。
        /// 本口只服务**挑战计数**，发射点与 `LogHeal` / `LogAddEffect` 同一族（结算侧，白名单外）。</para></summary>
        public static void OnWarlordDamaged(bool isPlayerWarlord, int amount)
        {
            if (amount <= 0) return;
            if (!isPlayerWarlord) ChallengeData.DamageEnemyWarlord += amount;   // 只数「对敌方」的伤害
            UiBadgeNotificationManager.Refresh(typeof(ChallengeMatch));
        }

        // ============================================================ ② 三条计数口（`ChallengeLogMgr`）

        /// <summary>原版 `ChallengeLogMgr.TroopKilled(BattleAction)`：数**被击杀的部队**（督军不算部队）。
        /// <para>⚠️ `isTroop = false`（督军/英雄倒下）时**不计数** —— 原版那一条只服务 `TroopsKilledCount`
        /// 类挑战；别为了「有数」把督军也数进去（那是静默错账）。</para></summary>
        public static void TroopKilled(bool isTroop)
        {
            if (!isTroop) return;
            ChallengeData.TroopsKilled++;
            UiBadgeNotificationManager.Refresh(typeof(ChallengeMatch));   // 进度变了 ⇒ 让读进度的界面重算（复用既有总线）
        }

        /// <summary>原版 `ChallengeLogMgr.LogHeal(BattleAction)`：累计**治疗量**（`amount ≤ 0` 不算）。</summary>
        public static void LogHeal(int amount)
        {
            if (amount <= 0) return;
            ChallengeData.HealAmount += amount;
            UiBadgeNotificationManager.Refresh(typeof(ChallengeMatch));
        }

        /// <summary>原版 `ChallengeLogMgr.LogAddEffect(CardEffect)`：数**施加的效果条数**（每条 +1，不看数值）。</summary>
        public static void LogAddEffect()
        {
            ChallengeData.EffectsAdded++;
            UiBadgeNotificationManager.Refresh(typeof(ChallengeMatch));
        }

        /// <summary>生存回合（「活过 N 回合」那一类）。原版由服务端按对局回合算；我们按引擎回合数记。</summary>
        public static void SurviveTurn()
        {
            ChallengeData.SurviveTurns++;
        }

        // ============================================================ ③ 「挑战」钮的模型那一半（`ChallengeButton`）

        static bool _visible;

        /// <summary>原版 `ChallengeButton.Initialize()` 的核心那一句：
        /// **有未处理的练习挑战就显**（判据 → 账 9 备注：显隐看 `PendingPracticeMatches.Count > 0`，
        /// ⛔ 不是 `challengeManager.challenges`）。
        /// <para>⚠️ 真正的 `SetActive` 在视图侧（`Shell/TopBar.cs`，本批白名单外）⇒ 这里只给**该不该显**。</para></summary>
        public static bool Initialize() { SetVisible(ChallengeData.PendingCount > 0); return _visible; }

        /// <summary>显隐变了（视图侧订阅这一条去 `SetActive`；原版 `ChallengeButton.Toggle` 就是那一下）。
        /// ⚠️ 只在**真变了**的时候发 —— 徽标每次 `Refresh` 都会问一遍，不该每问一次就 `SetActive` 一次。</summary>
        public static event Action<bool> VisibleChanged;

        static void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            var h = VisibleChanged;
            if (h != null) h(v);
        }

        /// <summary>原版 `ChallengeButton.Toggle(bool)`：只是把「该显」写下来（原版就是 `SetActive(option)` 那一句）。</summary>
        public static void Toggle(bool option) { SetVisible(option); }

        /// <summary>现在该不该显（视图侧问这一句，⛔ 别自己再算一遍）。</summary>
        public static bool Visible { get { return _visible; } }

        /// <summary>原版 `ChallengeButton.OnSignalRaise(Type signal)`：徽标信号来了就**重算一次**显隐。
        /// <para>判据 = 原版订阅 `UiBadgeNotificationManager.OnNotify`，且只对**自己那类**通知响应
        /// （`IsRelated` 那一层）—— 我们复用同一个 `IsRelated`，⛔ 不另写一份类型比较。</para></summary>
        public static bool OnSignalRaise(Type signal)
        {
            if (signal == null) return _visible;
            if (!UiBadgeNotificationManager.IsRelated(signal, typeof(ChallengeMatch))) return _visible;
            return Initialize();
        }

        /// <summary>挂进徽标总线（幂等）。挂上之后 `UiBadgeNotificationManager.Refresh` 会回调 `OnSignalRaise`。
        /// ⚠️ 生产路径应由「挑战钮建出来的那一刻」调用（`TopBar` = 白名单外）；这里是**唯一入口**，
        /// 自检/探针也可以直接调。</summary>
        public static void Attach()
        {
            ChallengeData.RegisterProviders();
            UiBadgeNotificationManager.OnNotify -= OnNotifyHandler;
            UiBadgeNotificationManager.OnNotify += OnNotifyHandler;
            Initialize();
        }

        /// <summary>拆线（自检用；生产不收）。</summary>
        public static void Detach()
        {
            UiBadgeNotificationManager.OnNotify -= OnNotifyHandler;
            ChallengeData.UnregisterProviders();
        }

        static void OnNotifyHandler(Type t) { OnSignalRaise(t); }

        // ============================================================ ④ 进度（挑战清单 × 计数）

        /// <summary>当前进度（`0..Target`）—— 视图/自检只看这一个口。</summary>
        public static int ProgressOf(ChallengeDefinition d)
        {
            if (d == null) return 0;
            int v = ChallengeData.CountOf(d.Kind);
            return v < 0 ? 0 : (v > d.Target ? d.Target : v);
        }

        // ============================================================ ⑤ 复位

        /// <summary>开局/换局复位：血线基线 + 计数 + 显隐。⚠️ 生产路径应在战斗开始时调（`BattleDriver`，白名单外）。</summary>
        public static void Reset()
        {
            _visible = false;
            ChallengeData.Reset();
        }
    }
}
