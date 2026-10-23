// Card3DAnimationController.cs — 原版 `Card3DAnimationController`（**卡体动画层**）的复刻
//
// ─────────────────────────────────────────────────────────────────────────────
// 【第一权威 · 逐方法亲读】（2026-10-23，`d:/2/tools/decomp_full/`）
//   `Card3DAnimationController__{Start,OnDestroy,Update,DoAnim,StartAnim,AnimFinished,
//    ClearClips,AddClipIfNecessary,Toggle,OnFlyingToggleFinish,ctor,cctor}.c` —— 全篇。
//   字段名取自签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Card3DAnimationController.cs`
//   （反编译里是 `+0x20/+0x48/+0x49/…`，两份逐一对上了，见下面每个字段的注释）。
//   调用链（逐跳读过）：`AbilityLogic.PlayAnim` → `BattleManager.AddCardAnim` →
//   `BattleManager.ResolveAnimAnimationClip` → `DelayedBodyAnimationPlay` →
//   **`CardScript.DoBodyAnimation`** → `Card3DAnimationController.DoAnim`。
//   ⇒ 只有 `CardScript__DoBodyAnimation.c` 一个调用点（`grep Card3DAnimationController__DoAnim`
//     全库 1 处命中），别处没有第二扇门。
//
// 【原版状态机（照抄，⛔ 别简化）】
//   · `DoAnim(animationClip, disableFlying, onAnimationEnd, safeResetOnEnd = true)`：
//       ① `clip == null` ⇒ **只调一次回调、返回 false**（`__DoAnim.c` 最前面那一段）；
//       ② 记 `safeResetOnEnd`、关掉自己与 `Animation`、`AddClipIfNecessary(clip)`、记回调与 clip；
//       ③ **`disableFlying == true` 时**（这才是「飞行单位先落地再演」那一条）：
//          - `unitWasFlying`(0x48) 还没判过 ⇒ 用 `FlyingController.IsFlying()` 判一次；
//            **不飞** ⇒ 直接 `StartAnim()`；
//          - `hasAlreadyToggledOffFlying`(0x49) 为假 ⇒ 置真、挂 `OnFlyingToggleFinish`、
//            `ToggleFlying(false, TOGGLE_OFF_FLYING_TIME=0.15)`、**返回 true（等落地回调）**；
//          - 已经置过 ⇒ `StartAnim()`（别重复落地）。
//   · `OnFlyingToggleFinish(bool)`：**true = 飞起来了 ⇒ `AnimFinished()`；false = 落地了 ⇒ `StartAnim()`**
//     （`__OnFlyingToggleFinish.c`：`if (param_2 != 0) AnimFinished(); else StartAnim();`）。
//   · `Update`：`currentAnimationTime -= dt`，当 `1.0 / targetFrameRate + time < 0`（= 过了一帧的裕量）时：
//       `unitWasFlying` 为真 ⇒ `ToggleFlying(true, TOGGLE_OFF_FLYING_TIME)` 并 **return**（飞回去，
//       真正的收尾交给飞起完成时的 `OnFlyingToggleFinish(true)`）；
//       否则 ⇒ `AnimFinished()`。
//   · `AnimFinished`：还在飞 ⇒ 摘回调、`ToggleFlying(true, TOGGLE_ON_FLYING_TIME=0.45)`、
//       清 `unitWasFlying`/`hasAlreadyToggledOffFlying`（原版写的是 2 字节 = 两位一起清）；
//       然后调 `onAnimationEnd`、`ClearClips()`、关自己与 `Animation`、
//       **`safeResetOnEnd` 时把变换复位**（`Start()` 那一刻缓存的 position/rotation/localScale）。
//   · `Start`：先把自己与 `Animation` **关掉**（原版 `enabled = 0`），再缓存变换。
//   · `Toggle(bool)`：`enabled = option` + `Animation.enabled = option`（两处一起）。
//
// 【常数（`.rdata` 直读，`工具/read_literal.py`）】
//   · `TOGGLE_ON_FLYING_TIME  = 0.45f` · `TOGGLE_OFF_FLYING_TIME = 0.15f`
//     —— 原版 `__cctor.c` 写静态区的那两个 4 字节：`0x3ee66666` / `0x3e19999a`；
//        签名桩的声明顺序（`TOGGLE_ON_FLYING_TIME` 在前）⇒ 偏移 0 / +4 按序对上。
//   · `CrossFadeTime = 0.2f` —— `__StartAnim.c` 的 `DAT_1834b2bb0`（4 字节 float 实读 = 0.2）。
//   · `OneFrameSlack = 1.0f` —— `__Update.c` 的 `DAT_1834b2bb8`（4 字节 float 实读 = 1.0），
//     那个式子是 `1.0 / Application.targetFrameRate + time < 0`。
//
// 【⚠️ 我们改的三处（如实记，铁律 3）】
//   ① 原版是 `MonoBehaviour`、靠 `Update` 每帧推；本工程批处理没有帧循环
//      ⇒ 改成**普通类**、由 `BattleDriver.AdvanceTimeline` 那个唯一的泵推进
//      （与 `Battle/FlyingCardController.cs` 文件头「我们改的两处 ①」同一个理由、同一条先例）。
//   ② 原版吃的是 `UnityEngine.AnimationClip`（`Animation` 组件 + `CrossFade`）。本 build 的
//      **卡体 clip 表是空的**（判据：`assets_full` 全库 1,152 个 `CardAnim` 里
//      `minionAnimationClipInfo.enable == 1` 的有 **0 个**、`minionClips` 非空的 **0 个** —— 见
//      `资料/普查产出_1023/A_飞行体动画.md` §③）⇒ 光有 clip 那一路等于永远不播。
//      因此 `DoAnim` 收的是 <see cref="CardAnimPlayback"/>：它**可以**装一条真 `AnimationClip`
//      （原版那一路，本类**完整实现并可用**），也可以装「技能动画 prefab + 时长」
//      （= 我们这边 `cardAnimations[]` 真正解出来的东西 —— 见 `Battle/CardAnimClips.cs`）。
//      ⛔ **不许拿别的件顶 `Clip`**：没有 `Clip` 就走 prefab 那一路，两条路的收尾都走同一个 `AnimFinished`。
//   ③ 原版 `OnDestroy` 只做「摘 `OnFlyingToggleFinish`」。我们多一个 <see cref="Detach"/>：
//      视图销毁时由驱动调一次，同一个动作（⛔ 不要指望 GC —— 飞行控制器是驱动持有的长命对象）。
//
// 【为什么「飞行」那两位要照抄而不是简化】`0x48/0x49` 不是缓存，是**闸**：
//   `0x48` 决定「演完要不要飞回去」（`Update` / `AnimFinished` 各读一次），
//   `0x49` 决定「这次要不要再落地」（否则每一次 `DoAnim` 都会把单位按下去 0.15 s）。
//   两位在 `AnimFinished` 里**一起清**，而且清之前 `0x48` 还要再读一次 —— 顺序抄错就变成
//   「飞回去之后再也不落」或「每拍都落地」。判据逐行在 `__AnimFinished.c`。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一次「卡体动画」的播放内容。
    /// <para><see cref="Clip"/> 非空 ⇒ **原版那一路**（`Animation` + `CrossFade`，时长 = `Clip.length`）。
    /// 为空、<see cref="Prefab"/> 非空 ⇒ 我们这边 `cardAnimations[]` 解出来的**技能动画**
    /// （时长 = 调用方算好的 `Duration`，= `AnimInfo.GetAnimDuration()`）。</para>
    /// <para>两个都空 = 「空动画」—— `DoAnim` 会**照原版**只调回调并出声（见 `DoAnim` 的注释）。</para></summary>
    public struct CardAnimPlayback
    {
        /// <summary>原版 `DoAnim` 的第一个参数（`AnimationClip`）。本 build 拿不到卡体 clip，见文件头 ②。</summary>
        public AnimationClip Clip;
        /// <summary>技能动画 prefab 名（走 `AnimationClip` 那一路时为 null）。</summary>
        public string Prefab;
        /// <summary>这一拍演多久（秒）。`Clip` 那一路忽略它（用 `Clip.length`，与原版一致）。</summary>
        public float Duration;
        /// <summary>出声用：这一拍是谁的（`卡名/事件名`），⛔ 别拿它当逻辑判据。</summary>
        public string Label;
        /// <summary>🆕 2026-10-23（账 `新账-76②`·原版 `MinionScaleByPositionSO` 的**第三处调用点**）：
        /// **这一拍演的期间，每帧按棋盘深度重算卡自己 `localScale`**。
        /// <para>判据 = `CardScript.__c__DisplayClass358_0___MoveAndDisappearAnim_b__1.c` / `_b__4.c`
        /// （两条逐字同形）：`card.transform.localScale = GetScale(isPlayer, card.localPosition, 卡的 DefaultScale)`
        /// 挂在「技能动画位移」那条序列的 `OnUpdate` 上。原版这两个回调只从
        /// `BattleManager._ResolveAnim_d__466` / `_ResolveTraitAnim_d__465` 被调 ⇒ **只有真的会移动的那一拍**才有。</para>
        /// <para>⇒ 调用方按 `CardAnimPlacement.Move`（原版 `AnimInfo` 那位 `move`）置位，⛔ 别对所有技能拍都开。</para></summary>
        public bool DepthScaleByBoardPosition;
        /// <summary>🆕 这张卡是不是玩家自己的（原版 `CardScript+0x40`，`GetScale` 第一个实参）。
        /// `null` = 按静止位离哪一行近来判（`CardFeel.IsPlayerRow`，结构上不会误判）。
        /// ⚠️ 调用点知道得准就显式传（`BattleDriver.PlayOneUnitAnim` 传 `e.Player == _me`）。</summary>
        public bool? DepthScaleIsPlayer;

        public bool IsEmpty { get { return Clip == null && string.IsNullOrEmpty(Prefab); } }
    }

    /// <summary>原版 `Card3DAnimationController` 的复刻。**判据与「我们改的三处」见文件头。**</summary>
    public sealed class Card3DAnimationController
    {
        // ── 原版静态字段（`__cctor.c` 的两个 4 字节 float，见文件头「常数」）──
        /// <summary>原版 `TOGGLE_ON_FLYING_TIME` = **0.45**：**飞回去**用的过渡时长
        /// （`__AnimFinished.c` 用的是静态区偏移 0）。</summary>
        public const float ToggleOnFlyingTime = 0.45f;
        /// <summary>原版 `TOGGLE_OFF_FLYING_TIME` = **0.15**：**落地**用的过渡时长
        /// （`__DoAnim.c` 用的是静态区 +4）。⚠️ `__Update.c` 里「飞回去」那一支
        /// **也用的这一档**（原版如此，照抄；见文件头状态机第 4 条）。</summary>
        public const float ToggleOffFlyingTime = 0.15f;
        /// <summary>`__StartAnim.c` 的 `CrossFade` 时长（`DAT_1834b2bb0`，4 字节 float 实读 = **0.2**）。</summary>
        public const float CrossFadeTime = 0.2f;
        /// <summary>`__Update.c` 的 `DAT_1834b2bb8`（4 字节 float 实读 = **1.0**）——
        /// 过期判据 = `OneFrameSlack / Application.targetFrameRate + currentAnimationTime &lt; 0`。</summary>
        public const float OneFrameSlack = 1.0f;

        readonly Transform _t;
        readonly Func<FlyingCardController> _flyingOf;

        // 原版 `[SerializeField] private Animation myAnimation`（+0x20）
        Animation _myAnimation;
        // 原版 `[SerializeField] private FlyingCardController flyingController`（+0x28）
        //   ⇒ 我们这边是**按需取**（驱动持有 `_flyingViews`；同一个视图可能一会儿在飞、一会儿落地）
        // 原版 `currentAnimationTime`（+0x30）
        float _currentAnimationTime;
        // 原版 `onAnimationEnd`（+0x38）
        Action _onAnimationEnd;
        // 原版 `currentClip`（+0x40）—— 我们这一档记整个 playback（clip 或 prefab）
        CardAnimPlayback _current;
        // 原版 `unitWasFlying`（+0x48）：这次动画**开始时**单位在不在飞（决定演完要不要飞回去）
        bool _unitWasFlying;
        // 原版 `hasAlreadyToggledOffFlying`（+0x49）：这一轮**已经**把它按下去过一次
        bool _hasAlreadyToggledOffFlying;
        // 原版 `playingClips`（+0x50）
        readonly List<AnimationClip> _playingClips = new List<AnimationClip>();
        // 原版 `originalPosition/Rotation/Scale`（+0x58/+0x64/+0x74）
        Vector3 _originalPosition;
        Quaternion _originalRotation;
        Vector3 _originalScale;
        // 原版 `safeResetOnEnd`（+0x80）
        bool _safeResetOnEnd;
        // ── 🆕 2026-10-23（`新账-76②`）：技能动画位移期间的「按深度重算缩放」（原版 `MoveAndDisappearAnim` 的
        //    `b__1` / `b__4` 两个 `OnUpdate`）。曲线口 = `CardFeel.DepthScaleFactor`（共用静态口）。
        bool _depthScale;        // 这一拍要不要逐帧写
        bool _depthIsPlayer;     // 原版 `GetScale` 第一个实参
        float _depthBase;        // 「这一侧静止该多大」—— `DoAnim` 时抓一次（⛔ 别每帧读 `CardView.BaseScale`）
        bool _depthWrote;        // 写过没有（收尾只在写过时才复位，免得平白覆盖别人的补间）
        CardView _depthCard;     // 卡视图（取一次；null ⇒ 退回直写 `localScale`）
        bool _depthOnArena;      // 这张卡在不在 **3D 战场**上（2D 兜底布局一律不算深度）
        /// <summary>🆕 本控制器**真按深度写过几次**缩放（自检用：证明「接线了」而不只是「有字段」）。</summary>
        public int DepthScaleWrites { get; private set; }

        /// <summary>造一个控制器（= 原版 `Start()`：**先关掉自己**，再缓存变换）。</summary>
        /// <param name="t">卡根（原版这个组件与 `Animation` 都挂在卡 prefab 根上）。</param>
        /// <param name="flyingOf">取这张卡的飞行控制器（原版是序列化引用）。返回 null = 这张卡没在飞
        /// ⇒ `IsFlying()` 那一档按 `false` 处理（与原版 `Landed` 等价），⛔ 不要因为 null 就去建一个。</param>
        public Card3DAnimationController(Transform t, Func<FlyingCardController> flyingOf = null)
        {
            _t = t;
            _flyingOf = flyingOf;
            Enabled = false;                       // 原版 `Start` 第一句：`set_enabled(this, 0)`
            if (t != null)
            {
                _originalPosition = t.position;
                _originalRotation = t.rotation;
                _originalScale = t.localScale;
            }
        }

        /// <summary>原版 `enabled`（`Toggle` 写的那一位）。</summary>
        public bool Enabled { get; private set; }
        /// <summary>原版 `unitWasFlying`（0x48）—— 自检要盯它。</summary>
        public bool UnitWasFlying { get { return _unitWasFlying; } }
        /// <summary>原版 `hasAlreadyToggledOffFlying`（0x49）—— 自检要盯它。</summary>
        public bool HasAlreadyToggledOffFlying { get { return _hasAlreadyToggledOffFlying; } }
        /// <summary>当前这一拍（诊断用）。</summary>
        public CardAnimPlayback Current { get { return _current; } }
        /// <summary>剩余时间（原版 `currentAnimationTime`）。</summary>
        public float RemainingTime { get { return _currentAnimationTime; } }
        /// <summary>正在播的 clip 数（原版 `playingClips.Count`）；`ClearClips` 之后回 0。</summary>
        public int PlayingClipCount { get { return _playingClips.Count; } }
        /// <summary>本类**一共真播过几拍**（自检用：区分「结构上接上了」与「一次都没走」）。</summary>
        public int PlayedCount { get; private set; }
        /// <summary>本类**一共收尾过几拍**（`AnimFinished` 的次数）。</summary>
        public int FinishedCount { get; private set; }
        /// <summary>因为「空动画」被挡下过几次（`DoAnim` 的第一个分支）。</summary>
        public int EmptyCount { get; private set; }

        /// <summary>`FlyingCardController.IsFlying()`（原版 `__DoAnim.c` 里那一跳）。
        /// 取不到控制器 ⇒ `false`（= 原版 `Landed`）。</summary>
        public bool IsFlying()
        {
            var f = Flying();
            return f != null && f.IsFlying;
        }

        FlyingCardController Flying() { return _flyingOf != null ? _flyingOf() : null; }

        /// <summary>原版 `DoAnim(animationClip, disableFlying, onAnimationEnd, safeResetOnEnd = true)`。
        /// ⚠️ 第二个参数的真名是 **`disableFlying`**（不是「loop」）——
        /// 签名桩逐字：`public bool DoAnim(AnimationClip animationClip, bool disableFlying, Action onAnimationEnd, bool safeResetOnEnd = true)`。
        /// 返回值：`false` = 空动画（没播）；`true` = 已起播（或已排进「等落地」那一支）。</summary>
        public bool DoAnim(CardAnimPlayback anim, bool disableFlying, Action onAnimationEnd, bool safeResetOnEnd = true)
        {
            if (anim.IsEmpty)
            {
                // 原版：`if (clip == null) { if (onEnd != null) onEnd(); return false; }`
                // ⚠️ 原版**不出声**；本仓红线是「不许静默失败」⇒ 我们出声（一次性去重），
                //    但**行为与原版逐字一致**（调回调、返回 false、⛔ 不动飞行状态）。
                EmptyCount++;
                SpeakEmpty(anim.Label);
                if (onAnimationEnd != null) onAnimationEnd();
                return false;
            }

            _safeResetOnEnd = safeResetOnEnd;       // 原版 `*(undefined1 *)(param_1 + 0x80) = param_5`
            // 🆕 2026-10-23（`新账-76②`）：技能动画**位移**期间的逐帧深度缩放（原版 `MoveAndDisappearAnim`
            //   的 `b__1` / `b__4`）。底数在这里抓一次 —— 与本拍起播时的静止缩放同值；⛔ 别每帧读
            //   `CardView.BaseScale`（它每帧被本类改写 ⇒ 会复利）。
            _depthScale = anim.DepthScaleByBoardPosition && _t != null;
            _depthWrote = false;
            _depthCard = null;
            if (_depthScale)
            {
                _depthIsPlayer = anim.DepthScaleIsPlayer ?? CardFeel.IsPlayerRow(_t.position.z);
                _depthCard = _t.GetComponent<CardView>();
                _depthBase = _depthCard != null ? _depthCard.BaseScale : _t.localScale.x;
                _depthOnArena = _depthCard != null ? CardFeel.IsArenaCard(_depthCard)
                                                   : _t.gameObject.layer == ArenaSlots.ArenaLayer;
            }
            Toggle(false);                          // 原版 `set_enabled(this, 0)` + `myAnimation.enabled = 0`
            AddClipIfNecessary(anim.Clip);
            _onAnimationEnd = onAnimationEnd;
            _current = anim;

            if (disableFlying)
            {
                if (!_unitWasFlying)
                {
                    _unitWasFlying = IsFlying();     // 原版 `FlyingCardController__IsFlying`
                    if (!_unitWasFlying) { StartAnim(); return true; }
                }
                if (!_hasAlreadyToggledOffFlying)
                {
                    _hasAlreadyToggledOffFlying = true;
                    var f = Flying();
                    if (f != null)
                    {
                        f.OnFlyingToggleFinish -= OnFlyingToggleFinish;   // 原版那条 `-=` 再 `+=`（防重复挂）
                        f.OnFlyingToggleFinish += OnFlyingToggleFinish;
                        f.Toggle(false, ToggleOffFlyingTime);            // 落地（0.15 s）→ 回调 `false` → StartAnim
                    }
                    else
                    {
                        // 原版这里 `flyingController == null` 是 `throw`（`FUN_1803f47a0`）。
                        // 我们**不抛**：取不到控制器就说明这张卡根本不在飞（原版 `IsFlying` 在上一句已经
                        // 返回过 false）⇒ 直接起播。⚠️ 出声，别让人以为「落地那 0.15 s」跑过。
                        Debug.LogWarning($"[Card3DAnim] `{LabelOf(anim)}`：`disableFlying` 那一位要求"
                                       + "「先落地再演」，但取不到这张卡的 `FlyingCardController` ⇒ 直接起播"
                                       + "（原版这一支是抛异常，`Card3DAnimationController__DoAnim.c` 的 LAB_18099f2b2）");
                        StartAnim();
                    }
                    return true;
                }
            }
            StartAnim();
            return true;
        }

        /// <summary>原版 `StartAnim`：开自己 + 开 `Animation`；`currentAnimationTime = clip.length`；
        /// `CrossFade(clip.name, 0.2)`。</summary>
        void StartAnim()
        {
            Toggle(true);
            if (_current.Clip != null && _myAnimation != null)
            {
                _currentAnimationTime = _current.Clip.length;
                _myAnimation.CrossFade(_current.Clip.name, CrossFadeTime);
            }
            else
            {
                // 我们这一路：技能动画 prefab 的时长由调用方给（= `AnimInfo.GetAnimDuration()`）。
                _currentAnimationTime = _current.Duration;
            }
            PlayedCount++;
        }

        /// <summary>原版 `AnimFinished`（判据逐行见文件头状态机第 5 条）。</summary>
        void AnimFinished()
        {
            if (_unitWasFlying)
            {
                var f = Flying();
                if (f != null)
                {
                    f.OnFlyingToggleFinish -= OnFlyingToggleFinish;
                    f.Toggle(true, ToggleOnFlyingTime);      // 飞回去（0.45 s）
                }
                _unitWasFlying = false;
                _hasAlreadyToggledOffFlying = false;         // 原版写 2 字节 = 0x48 与 0x49 一起清
            }

            if (_onAnimationEnd != null) _onAnimationEnd();
            _onAnimationEnd = null;
            ClearClips();
            Toggle(false);

            // 🆕 2026-10-23（`新账-76②`）：把逐帧深度缩放写回**静止值**。
            //   原版那条序列跑完就「停止写」，最后一帧的值一直留着；我们的卡随后还会被
            //   `SetPose` / 换位补间搬，停在半途的缩放会静默放大 —— 所以显式复位。
            //   ⚠️ 顺序在 `safeResetOnEnd` **之前**：那一位是「整条复位到 Start() 那一刻」，优先级更高。
            if (_depthScale && _depthWrote && _t != null)
            {
                if (_depthCard != null) _depthCard.SetBaseScale(_depthBase);
                else _t.localScale = Vector3.one * _depthBase;
            }
            _depthScale = false; _depthWrote = false; _depthCard = null;

            if (_safeResetOnEnd && _t != null)
            {
                // 原版 `set_position` / `set_rotation` / `set_localScale` 三连。
                // ⚠️ 用的是**世界** position/rotation + **局部** scale（原版就是这三句，逐字照抄）。
                _t.position = _originalPosition;
                _t.rotation = _originalRotation;
                _t.localScale = _originalScale;
            }
            FinishedCount++;
        }

        /// <summary>原版 `ClearClips`：`currentClip = null`；把加进去的 clip 逐个 `RemoveClip`；清表。</summary>
        void ClearClips()
        {
            _current = default(CardAnimPlayback);
            if (_myAnimation != null)
                for (int i = 0; i < _playingClips.Count; i++)
                    if (_playingClips[i] != null) _myAnimation.RemoveClip(_playingClips[i]);
            _playingClips.Clear();
        }

        /// <summary>原版 `AddClipIfNecessary`：已经加过就跳过；否则 `AddClip(clip, clip.name)`。</summary>
        void AddClipIfNecessary(AnimationClip clip)
        {
            if (clip == null) return;
            if (_playingClips.Contains(clip)) return;
            EnsureAnimation();
            if (_myAnimation == null) return;            // 出声已经在 EnsureAnimation 里做过
            _playingClips.Add(clip);
            _myAnimation.AddClip(clip, clip.name);
        }

        /// <summary>原版 `Toggle(bool)`：`enabled` 与 `Animation.enabled` **一起**写。</summary>
        public void Toggle(bool option)
        {
            Enabled = option;
            if (option) EnsureAnimation();
            if (_myAnimation != null) _myAnimation.enabled = option;
        }

        /// <summary>原版 `OnFlyingToggleFinish(bool option)`：**true = 飞起来了 ⇒ 收尾；false = 落地了 ⇒ 起播**。</summary>
        void OnFlyingToggleFinish(bool option)
        {
            if (option) AnimFinished();
            else StartAnim();
        }

        /// <summary>🆕 2026-10-23（`新账-76②`）：一帧深度缩放。
        /// <para>原版 `MoveAndDisappearAnim` 的 `b__1`/`b__4`：`card.localScale = GetScale(isPlayer, card.localPosition,
        /// 卡默认缩放)`；`localPosition` 是「相对自己那一行」，我们卡父节点恒在原点 ⇒ 用**世界 z** 喂
        /// `CardFeel.DepthScaleFactor`（换算与 `CardFeel` 那两处完全同一个口，⛔ 别在这里写第二份曲线）。</para></summary>
        void ApplyDepthScaleTick()
        {
            if (_t == null) return;
            // 🔴 只有活在 **3D 战场**上的卡才吃这条（与 `CardFeel.HookDepthScale` 同一个门）：
            //   2D 兜底布局的卡全在 HUD 正交平面（z = 0），套曲线恒得 1.75× = 纯错。
            if (!_depthOnArena) return;
            float f = CardFeel.DepthScaleFactor(_t.position.z, _depthIsPlayer);
            if (_depthCard != null) _depthCard.SetBaseScale(_depthBase * f);
            else _t.localScale = Vector3.one * (_depthBase * f);
            _depthWrote = true;
            DepthScaleWrites++;
        }

        /// <summary>原版 `Update`。由 `BattleDriver.AdvanceTimeline` 推（见文件头「我们改的三处 ①」）。</summary>
        public void Update(float dt)
        {
            if (!Enabled) return;
            // 🆕 2026-10-23（`新账-76②`）：**逐帧按战场深度重算卡自己的缩放**（原版 `MoveAndDisappearAnim`
            //   那两个 `b__1`/`b__4` 的 `OnUpdate`）。`CardFeel.DepthScaleFactor` 在**自己那一行**上恒 = 1
            //   （原版 `GetScale` 也如此：曲线值 × `默认值 ÷ 该端键值` 正好约掉）⇒ 卡不动时写的是同一个值。
            if (_depthScale) ApplyDepthScaleTick();
            _currentAnimationTime -= dt;
            // 🔴 **2026-10-23（第六十六会话实跑红的）**：原式 = `OneFrameSlack / Application.targetFrameRate`。
            //   ⚠️ **批处理里 `Application.targetFrameRate` 是 `-1`（= 「不覆盖平台默认」）** ——
            //   初稿为了防除零写成 `Mathf.Max(1, fps)` ⇒ 裕量被放大成 **1.0 秒**，
            //   一条 0.2 s 的 clip 推 0.33 s 还**不会收尾**（`BattleScene.Run` 当场红：
            //   「回调 False · enabled True · 剩 clip 1 · 收尾 0 次」）。
            //   ⇒ 现在：`fps <= 0`（没有显式帧率上限）时按 **60** 算 —— 裕量 = 1/60 s，与原版
            //   「一帧之后才算过期」同义。这一段是**我们挑的**（原版那一格在真包里恒为正），如实记。
            int fps = Application.targetFrameRate;
            if (fps <= 0) fps = 60;
            if (OneFrameSlack / fps + _currentAnimationTime >= 0f) return;

            if (_unitWasFlying)
            {
                // 原版这一支：先飞回去，**不**收尾 —— 收尾由飞起完成时的 `OnFlyingToggleFinish(true)` 做。
                // ⚠️ 原版这里传的是静态区 **+4** = `TOGGLE_OFF_FLYING_TIME`（0.15），不是
                //    `AnimFinished` 用的 0.45。照抄（那一档本身也是个怪值，如实记，铁律 3）。
                var f = Flying();
                if (f != null) f.Toggle(true, ToggleOffFlyingTime);
                return;
            }
            AnimFinished();
        }

        /// <summary>视图销毁时由驱动调（= 原版 `OnDestroy` 的「摘回调」）。</summary>
        public void Detach()
        {
            var f = Flying();
            if (f != null) f.OnFlyingToggleFinish -= OnFlyingToggleFinish;
            _unitWasFlying = false;
            _hasAlreadyToggledOffFlying = false;
            _onAnimationEnd = null;
        }

        /// <summary>自检用：造一条**真的** `AnimationClip`（`Animation` 那一路要能跑，就得有东西给它播）。
        /// ⛔ 只给断言用 —— 生产路径的 clip 来自 `cardAnimations[]`（本 build 为空）。
        /// 曲线写的是**卡根自己的** `localPosition.y`（原版那批卡体 clip 也是这么绑的：路径 = 卡根）。</summary>
        public static AnimationClip BuildProbeClip(string name, float seconds, float dy)
        {
            var clip = new AnimationClip { name = name, legacy = true };
            var keys = new[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(Mathf.Max(0.02f, seconds * 0.5f), dy),
                new Keyframe(Mathf.Max(0.04f, seconds), 0f),
            };
            clip.SetCurve("", typeof(Transform), "localPosition.y", new AnimationCurve(keys));
            return clip;
        }

        /// <summary>`Animation` 组件按需建（原版是序列化在卡 prefab 上；我们的卡是代码建的）。
        /// ⚠️ 挂的是**卡根**：原版那个 `Animation` 与 `Card3DAnimationController` 挂在同一个
        /// prefab 根上（`__DoAnim.c` 用的是 `GetComponent` 那个组件）。
        /// `playAutomatically = false` —— 原版 prefab 上是 true，但它**没有默认 clip**
        /// （`Animation.m_Animations` 只有 1 条、且 `DoAnim` 走的是 `CrossFade` 指名的 clip），
        /// 这里关掉是为了别让空 `Animation` 每帧自己转。</summary>
        void EnsureAnimation()
        {
            if (_myAnimation != null) return;
            if (_t == null) { SpeakNoAnimation(); return; }
            _myAnimation = _t.GetComponent<Animation>();
            if (_myAnimation == null)
            {
                _myAnimation = _t.gameObject.AddComponent<Animation>();
                _myAnimation.playAutomatically = false;
            }
        }

        static readonly HashSet<string> _spokenEmpty = new HashSet<string>();
        const int SpeakCap = 12;
        /// <summary>「这一拍没有内容」出声一次（每种 label 一次）。**⛔ 不静默**。</summary>
        static void SpeakEmpty(string label)
        {
            string k = string.IsNullOrEmpty(label) ? "<unnamed>" : label;
            if (_spokenEmpty.Count >= SpeakCap || !_spokenEmpty.Add(k)) return;
            Debug.LogWarning($"[Card3DAnim] `{k}` 拿到的是**空动画**（既没有 `AnimationClip`、也没有技能动画 prefab）"
                           + " ⇒ 只调回调、不起播（原版 `DoAnim` 对 null clip 就是这个行为）");
        }

        static bool _spokeNoAnimation;
        static void SpeakNoAnimation()
        {
            if (_spokeNoAnimation) return;
            _spokeNoAnimation = true;
            Debug.LogWarning("[Card3DAnim] 卡根是 null ⇒ 建不出 `Animation` 组件，"
                           + "`AnimationClip` 那一路播不了（技能动画 prefab 那一路不受影响）");
        }

        static string LabelOf(CardAnimPlayback a)
        {
            if (!string.IsNullOrEmpty(a.Label)) return a.Label;
            if (a.Clip != null) return a.Clip.name;
            return a.Prefab;
        }
    }
}
