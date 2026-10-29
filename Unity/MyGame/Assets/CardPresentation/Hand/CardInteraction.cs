// CardInteraction.cs — 手牌交互的驱动：悬停 / 拖拽 / 落位 / 回弹
//
// 这是把前面几个模块串起来的那一层：
//   指针 → 命中哪张牌 → 悬停（抬起 + 邻牌让位）
//        → 按下 → 拖拽（跟着指针走、其他牌补位）
//        → 松手 → 落在格位上（DeploySequence）或弹回手牌（DOTween）
//
// 输入用的是**新输入系统**（本工程 `activeInputHandler: 1`，旧输入 API 会直接抛异常）。
// 鼠标和触摸都读，同一套逻辑 —— 后面上手机不用改这里。
//
// 批处理自检怎么验：把 `CardTween.Mode` 设成 Manual，然后手动喂指针位置 + `Advance(dt)`，
// 见 `CardBaseDemo`。所以这里**不要**写死任何跟 Time.deltaTime 有关的东西。
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardPresentation
{
    public class CardInteraction : MonoBehaviour
    {
        public HandLayout hand;
        public BoardLayout board;
        /// <summary>
        /// **敌方半场**。战术卡不落格位、而是**打到某个单位身上**，所以它要能落到对面那一半
        /// （`Deal 3 damage to an enemy` 打的就是敌方单位）。没接也能跑 —— 就是打不了敌方目标。
        /// ⚠️ 两个棋盘的**槽号是同一套**（0–8），光看槽号分不出哪边，所以落点判定必须同时返回棋盘。
        /// </summary>
        public BoardLayout foeBoard;
        public Camera cam;

        // 🔴 2026-09-17：1.16 → **1.1**，换的是原版 `VarsGlobal.cardInHandMovingScale = 1.1`（手牌移动缩放）。
        //    它是**纯比值**（乘在手牌缩放上），与维度无关 ⇒ 原样照搬，不用换算。
        //    出处：`资料/VarsGlobal_原版数值.md` §一。原来那个 1.16 是「我们挑的」。
        [Tooltip("拖拽时放大到多少 —— **乘在手牌缩放上**（手牌缩放随分辨率变，写死绝对值会在 4:3 上炸开）。原版值 1.1")]
        public float dragScale = 1.1f;

        [Tooltip("拖起来时往前挪多少 z（越小越靠近相机）")]
        public float dragLiftZ = 0.6f;

        [Tooltip("悬停/拖拽中的牌额外前移，避免被旁边的压住")]
        public float frontZ = 0.5f;

        public event Action<CardView, int> OnDeployed;     // 落位成功
        /// <summary>回弹到手牌。🆕 **2026-10-25（`X19/P2`）加了第二个参数 `kind`** ——
        /// 订阅方要能分开「正常取消（`MissedSlot`）」与「非法出牌（`EngineRefused`）」：
        /// 后者**不该**再写 `Battle/Tips/DragToTarget`（它的文案已按拒因分四档，由
        /// <see cref="OnIllegalAction"/> 出）。判据只走 <see cref="IsIllegalAction"/>，⛔ 别比枚举值。</summary>
        public event Action<CardView, DropRejectKind> OnReturned;
        /// <summary>🔴🆕 2026-10-19（战斗线 B2 · 偏差 1/2 的残余①）：**出牌飞行开始的那一拍**。
        ///
        /// <para>实参 = (这张卡, 落向哪块棋盘, 真正落点格号, 这段飞行有多长)。</para>
        ///
        /// <para>判据（`资料/任务线/战斗线/任务总表.md` 残余① + `资料/普查产出_1018/R_战斗表现链路对拍.md`）：原版
        /// `_ResolvePlayCardFromHand` 是**入动作队列**之后才一步步演，`WaitingForBlockingAction`
        /// 从「这张牌离开手牌」那一刻就已经为真 —— 也就是说**0.65 s 飞行本身也在闸里**。
        /// 我们这边引擎要等飞行跑完（`DeploySequence.Play` 的 `onDone` → `OnDeployed` → `DoPlay`）才被调用，
        /// 所以在飞行的这 0.65 s 里 `WaitingForBlockingAction` 还是假的 ⇒ 手牌还能再抓一张、回合钮还能点。</para>
        ///
        /// <para>⇒ 这个事件就是给驱动层的**起手回调接口**：接上之后由 `BattleDriver` 把这段飞行
        /// 也纳入 `WaitingForBlockingAction`（并播原版「起飞」那两条 cue）。
        /// ⚠️ 不接也能跑（表现层自己的自检照样过）；接不接**不改**本组件的任何行为。</para>
        /// </summary>
        public event Action<CardView, BoardLayout, int, float> OnDeployFlightStart;
        /// <summary>🔴🆕 2026-10-19（`W57` · `T05(c)(d)`）：**出牌飞行到 0.4 s 那一刻的交接**。
        ///
        /// <para>实参 = (这张卡, 落向哪块棋盘, 真正落点格号)；返回 <see cref="DeploySequence.Handoff"/>
        /// （0.4 s 执行的动作 + 落体 transform + 竞技场局部坐标的落点）。</para>
        ///
        /// <para>判据：原版 `CardScript._MinionPlayedIntoField_d__314__MoveNext.c` 在
        /// `WaitForSeconds(timeToScaleIntoMoving = 0.1)` 之后（= 飞行 0.3 + 等待 0.1 = **0.4 s** 处）
        /// 做 `SetUnitInPlayLayer()`（手牌卡面 → 场上 3D 体）+ `DefaultScale` + `HideCardShadow`，
        /// **之后**那 0.2 s 的 `DOLocalMove` 是在**竞技场空间**里落的（见 `Board/DeploySequence.cs` 文件头）。</para>
        ///
        /// <para>🔴 **判据只留一处**：真正「怎么换」由驱动层一处给出（`BattleDriver.OnDeployHandoff`），
        /// 本组件只负责在正确时刻调用。⛔ 别在这里拼 `SetFace/SetParent/SetLayer`。
        /// ⚠️ 不接也能跑（表现层自检 / `CardBaseDemo` 没有驱动层）⇒ `default` = 退回旧的三段
        /// （HUD 平面直落；总长仍是 0.65 —— 那 0.1 的等待对单位卡恒定存在）。</para>
        /// </summary>
        public Func<CardView, BoardLayout, int, DeploySequence.Handoff> DeployHandoff;
        /// <summary>🔴 **落点落在格位上、但被拒了**（= 玩家做了一次非法操作）。
        /// 和 <see cref="OnReturned"/> 的区别：**拖回手牌/拖到空白处不算**（那是正常取消）。
        /// 原版在这个时机播 `cantdo`（"我不能这么做"）—— 见 `资料/语音线_原版规格与ASR管道.md` §1.3。
        /// 判据由 <see cref="IsIllegalAction"/> 一处给出，**调用方别再比字符串**。
        ///
        /// <para>🔴 **2026-10-25（`X19/P2`）签名加了第二个参数** —— 原版的拒因文案是**按因分档**的
        /// （`BattleManager__CanPlayCard.c:82-152` 四支：费用不够 / 格子满 / 没目标 / 不是你回合），
        /// 而「哪一因」有两处只有本层知道、驱动层拿不到：
        /// ① **指针压在错的那一侧棋盘上**（`DropReject` 的 `hitOther` 支）；
        /// ② 其余档（引擎拒）本层拿不到原因码。
        /// ⇒ 本层只把**它自己判得出来的那一条词条键**交出去（`tipTerm`，null = 让驱动层按引擎码取）。
        /// ⚠️ 词条键的**唯一出处**是 `BattleDriver.DragToTargetTerm`（那边管全部 `Battle/Tips/*` 常量），
        /// 本层不另抄一份字符串。</para></summary>
        public event Action<CardView, string> OnIllegalAction;
        /// <summary>**轻点**了一张手牌（按下→松开，几乎没移动）。
        /// 原版这个动作是 `BasicCardUI.ToggleOpenCardDisplayOnTouch` —— 开关卡牌展示窗。
        /// 和「拖出去又放回来」区分开：拖过就不算轻点。</summary>
        public event Action<CardView> OnTapped;

        /// <summary>算不算「轻点」的位移阈值（世界单位）。32.4 px @1080p —— 手抖一下不算拖。
        ///
        /// 🔴 **2026-09-17 换成正解**：原版值 = `VarsGlobal.minMovementToPlayCard = 0.3`
        /// （字面意思就是「要判成『在打这张牌』至少得移动多远」）。
        /// **原来写 0.12 是「我们挑的」**，已替换 —— 效果是「轻点」判定区从 13 px 放宽到 32.4 px。
        ///
        /// ⚠️ **单位不用换算**：原版**手牌那一层**的像素密度也是 108 px/世界单位
        /// （`Hand/HandLayout.cs` 里那条「165 px = 2DCard 2.0927 × m_scale 0.73 × 108」注：165 px 的卡 = 2DCard 2.0927 × scale 0.73 × 108），
        /// 与我们一致 ⇒ 手牌层的距离值**可以 1:1 搬**。（棋盘那一层是 182.14 px/单位，才要 ×1.6865。）
        /// 出处：`资料/VarsGlobal_原版数值.md` §一。
        ///
        /// ⚠️ **语义还有一层没对齐（不是换数能解决的）**：原版这个阈值是「轻点 vs 拖出去打牌」的分界；
        /// 我们的 `Release` 另有一道闸 —— 落点必须命中格位（`DropReject`），移动很小但落在格上照样出牌。
        /// 阈值只改「轻点」那一支，**没有**变成出牌的前置条件。要完全对齐得另加闸，那是行为改动。</summary>
        public const float TapThreshold = 0.3f;

        /// <summary>🔴 **2026-10-25（执行代理 `K2` · 战斗鼠标操作对照）—— 原版「长按 ⇒ 松手**不开**卡面展示窗」的时长门（秒）。**
        ///
        /// <para>判据（`d:/2/tools/decomp_full/CardScript__OnTouchUpAsButton.c:19-45`，亲读）：
        /// 手牌那一支（`cardState ∈ {inHand(1), inHandShowing(8)}`）先算
        /// `elapsed = Time.timeSinceLevelLoad − card.pressTime(+0x800)`（`+0x800` 由
        /// `CardScript__OnTouchDown.c:26-27` 在按下那一刻写入），然后：
        /// <code>
        /// if (0.2 &lt;= elapsed) goto 收尾;                 // ← 长按：**跳过 DisplayCard**
        /// if (bm != null) { DisplayCard(bm, card, 1, 0, 0); } // ← 短按：开窗
        /// 收尾: if (cardState != 8) return; StopShowingCardInHand(card, 1, 0);
        /// </code>
        /// 常量 = `DAT_1834b2bb0`，`工具/read_literal.py` 现读 = **`0.2f`**（4 字节 float，
        /// 同一地址按 8 字节读是 `3.05e-05` = 错的那一档）。</para>
        ///
        /// <para>⚠️ **只管「轻点开窗」这一支**：同一个字面量在 `CardScript__TryDraggingFromBoard.c:33-36`
        /// 被当成**屏幕像素²**的死区（≈0.45 px，被 uGUI 的 5 px 拖拽阈值完全盖住）—— 两处是**不同语义共用
        /// 一个字面量**，⛔ 别把这里的 0.2 **秒**搬去当拖拽阈值。
        /// ⚠️ **棋盘单位的轻点没有这道门**（`OnTouchUpAsButton` 的棋盘支 = 状态 2/3/0x11，直接
        /// `DisplayCard`，没有时间判据）⇒ 这一条只落在手牌层。</para>
        ///
        /// <para>🔴 **改前我们**：`Release` / `EndPressCandidate` 只看 `_travel &lt; TapThreshold` 与
        /// `_pressInside`（= 原版 `CardCollider.allowClick` 的等价物），**完全不看按了多久**
        /// ⇒ 「按住手牌 2 秒再松手」我们开窗、原版不开。</para></summary>
        public const float LongPressNoDisplaySeconds = 0.2f;

        // ==================================================================
        //  🔴 2026-10-19（战斗线 T10 · `C3` 结构差异 #11）：**原版两道起拖阈值**
        //     （只在一个平台档里拦 —— 见 `TouchInputOn`）
        // ==================================================================
        //  判据 = `d:/2/tools/decomp_full/CardScript__TryDraggingFromHand.c` 逐句 +
        //         调用链 `CardCollider__…OnDrag.c` → `CardScript__OnTouchDrag.c`：
        //    · 两个实参的来路（这一步决定 delta 的**符号**，别读反）：
        //      `OnTouchDrag(card, eventData, cardCollider.pointerDownPosition)` 里
        //      `local_38 = *(eventData + 0x144)` = **`PointerEventData.position`（当前点）**、
        //      `local_48 = (CardCollider + 0x38)` = **`pointerDownPosition`（按下点）**
        //      ⇒ `TryDraggingFromHand(param_2 = 当前, param_3 = 按下)`；
        //      `:36-39` 写的是 `fVar6/fVar7 = (view(param_2) − view(param_3))`
        //      ⇒ **delta = 当前 − 按下**（朝**上**拖 ⇒ delta.y > 0 ⇒ 角度小 ⇒ 放行）。
        //      ⚠️ `资料/普查产出_1016战斗判据/T10_换牌阶段判据.md` §C3#11 表第 1 行写成
        //      「原按点 − 当前点」—— **那个符号是反的**（照它实现会把「往上拖」判成 180° 拦掉）。
        //    · `:60` 位移闸：`delta.x² + delta.y² <= 0.00015`（常量 `DAT_1834b3150`）⇒ 拦；
        //    · `:49-57` 方向闸：`Vector3.Angle(delta, Vector3.up)` 超过 `65.0`（`DAT_1834b316c`）
        //      ⇒ 拦；`card + 0x249 != 0` 时用 `70.0`（`DAT_1834b3170`）；
        //    · `:64-69` 🔴 **上面四条只有 `GameStaticData.touchInput`（static `+0x11d`）为真时才 `return`**
        //      ⇒ PC 档一条都不拦（**按下即抓 = 我们原来的行为，不变**）。
        //      ⚠️ 原版那四条里，本类只**显式**实现位移与方向两条；另两条的等价物在结构上：
        //      · `!BattleManager.CanMoveHandCard(bm, 0)` ⇒ 我们由「`_cards` 只装当前可操作的手牌 +
        //        `Frozen`/换牌期 `enabled=false`」承担（原版那道闸也是整帧级，见 `Frozen` 的 doc）；
        //      · `card + 0x40 == 0`（非我方）/ `card + 0x228 ∉ {8,1}`（卡 UI 状态）⇒ 我们手里只有我方卡，
        //        换牌期整条输入链关掉 ⇒ 两档都到不了这里。
        //      ⛔ 别在本类再补一套重复的判断（两处写同一条规则 = 迟早不一致）。

        /// <summary>原版起拖**位移闸**（**视口²**）：`delta.x² + delta.y² <= 它` ⇒ 不起拖。
        /// 常量实读 `DAT_1834b3150 = 0.00015f`（判据 → `B3_战斗输入交互矩阵.md` §四 · `C3` #11）。</summary>
        public const float HandDragMinDeltaSq = 0.00015f;

        /// <summary>原版起拖**方向闸**：`Vector3.Angle(delta, Vector3.up) > 它` ⇒ 不起拖。
        /// 常量实读 `DAT_1834b316c = 65.0f`。</summary>
        public const float HandDragMaxAngleDeg = 65f;

        /// <summary>`card + 0x249 != 0` 那一档的方向闸 —— 常量实读 `DAT_1834b3170 = 70.0f`。
        /// ⚠️ **`+0x249` 是什么字段：没查到**（`dump.cs` 里没锚定，`C3` 没查清-6）⇒ 本类**不猜**：
        /// 生产路径恒走 65°；这一档只作为判据留在 <see cref="HandDragPastThreshold"/> 的 `altAngle` 形参上。</summary>
        public const float HandDragMaxAngleDegAlt = 70f;

        /// <summary>原版两道起拖阈值的**唯一判据**（纯函数，自检直接喂点）。
        /// <para>两个点可以给**世界坐标**：本仓的 `LayoutSpace.ToNormalized` 与原版
        /// `Camera.ScreenToViewportPoint` 同口径（x ÷ 可见宽、y ÷ 可见高，0–1），
        /// 而 `world` 本来就是从屏幕点经 `ScreenToWorldPoint` 来的。</para>
        /// <para>⚠️ 原版这段**只看平台/设置闸**（<see cref="TouchInputOn"/>）—— 调用方必须先过闸再问它；
        /// 这里**不**自带平台判断（否则自检没法单验判据）。</para></summary>
        public static bool HandDragPastThreshold(Vector3 pressWorld, Vector3 nowWorld, bool altAngle = false)
        {
            var a = LayoutSpace.ToNormalized(pressWorld);
            var b = LayoutSpace.ToNormalized(nowWorld);
            float dx = b.x - a.x, dy = b.y - a.y;          // = 当前 − 按下（见上面那段调用链判据）
            if (dx * dx + dy * dy <= HandDragMinDeltaSq) return false;
            float angle = Vector3.Angle(new Vector3(dx, dy, 0f), Vector3.up);
            return angle <= (altAngle ? HandDragMaxAngleDegAlt : HandDragMaxAngleDeg);
        }

        /// <summary>🔴 **原版 `GameStaticData.touchInput`（static `+0x11d`）的等价物** ——
        /// 就是**设置窗 General 页那颗 `Touch input`**（`Shell/SettingsWindow.cs` 的
        /// `SettingsWindow.GeneralFlags.TouchInput`；判据 = `GeneralTab__TouchInputToggle.c` 逐句
        /// `static_fields + 0x11d = param` **且** `+0x12d = 1`（手动改过））。
        /// <para>⚠️ 出厂默认值：原版 `LoadStarterData` / `LoadSharedDefaultValues` 都写
        /// `Application.isMobilePlatform`（Steam Deck 另判）——**桌面 = 0**；我们 `PlayerPrefs` 缺省也是 0
        /// ⇒ 桌面一致。**移动端的平台默认值那一格我们没接**（`GeneralFlags` 恒缺省 0）——
        /// 如实记在 T10 报告里；真要上移动端时主件在 `Shell/SettingsWindow.cs`，⛔ 不是这里。</para></summary>
        static bool TouchInputOn { get { return SettingsWindow.GeneralFlags.TouchInput; } }

        /// <summary>自检用：平台闸现在认的是哪一档（= 上面那颗开关）。</summary>
        public static bool TouchInputGateForTest { get { return TouchInputOn; } }

        /// <summary>
        /// 落点合法性 —— **由外部注入**（战斗驱动层接规则引擎的 `RuleCore.CanPlayCard`）。
        /// 返回 false 就回弹。默认全放行（不接引擎时，表现层自检照样能跑）。
        /// 拖拽过程中的高亮和松手时的判定**都走这一个委托**，不另写一份。
        /// </summary>
        public Func<int, CardView, bool> CanDropAtSlot = (slot, card) => true;

        /// <summary>🔴🆕 2026-10-18（`A915`）：**对手掉线/等待重连期间 ⇒ 这一层也禁操作**。
        ///
        /// 判据 = 原版 `BattleManager__Update.c:104-115` 那道**整帧闸**（`ConnectionStatus ∉ {0,3} ⇒ return`）
        /// —— 它拦的是「整个 `BattleManager` 这一帧」，而**手牌拖拽不走 `BattleDriver`**
        /// （本组件是个独立的 `MonoBehaviour`，有自己的 `Update`）⇒ 光在驱动那一侧拦，
        /// 掉线期间**手牌照样能拖、能打出去**（那是真越权）。
        ///
        /// ⛔ **不是**复用 `enabled`（那个位在本文件里已经承担**换牌**语义）——
        ///    这里另开一个**只读委托**，由 `BattleDriver.Begin` 接上（默认恒 false ⇒ 不接也能跑）。
        /// ⚠️ 语义与 `BattleDriver.PlayerInputFrozen` **同一个来源**（`NetClockPaused`），
        ///    ⛔ 别在这儿另写一条判断（两处写同一条规则 = 迟早不一致）。
        /// </summary>
        public Func<bool> Frozen = () => false;

        bool _frozenForTest;
        bool _frozenWarned;
        /// <summary>自检用：把「冻结」这一档直接拨过去（产品里由 `BattleDriver` 那条委托喂；
        /// 批处理里没有真掉线，只能显式拨）。⛔ **只影响 `Update` 那一条闸**，不碰别的语义。</summary>
        public void SetFrozenForTest(bool on) { _frozenForTest = on; }
        /// <summary>现在是不是「禁操作」那一档（自检用）。判据两半合一，⛔ 别在别处再拼一次。</summary>
        public bool FrozenNow { get { return _frozenForTest || (Frozen != null && Frozen()); } }

        // ==================================================================
        //  🔴 2026-10-25（`AUD8` #19）：**战斗状态闸** —— 原版起拖的另一半条件
        // ==================================================================
        //  判据（第一权威 = 全量反编译，现读）：`d:/2/tools/decomp_full/CardScript__OnTouchDrag.c:17-23`
        //    ① `matchData.matchType != 0xA0`（**回放局不许拖** —— 但**轻点看牌照旧允许**，见 `ReplayDragBlocked`）；
        //    ② `BattleManager.UIstate == 1`（`normalBattle`）—— 选目标(9/10/13)、打法选择器、
        //       选牌/选效果面板、换牌 这些状态下 **UIstate 都不是 1** ⇒ 原版根本走不到
        //       `TryDraggingFromHand`；
        //    ③ 同一行还有 `!IsRightButtonOrTwoFingerTouch()`（本类已有，见 `IsRightButtonOrTwoFingerTouch`）。
        //  另外原版那 8 扇模态窗靠**全屏压暗层的射线**挡住手牌（`Menu Dark Background` 一族）。
        //
        //  🔴 **为什么本类必须自己判**：手牌拖拽**不经过 EventSystem**（本类读裸指针 + 自己的
        //    `CardView.HitByCollider`）⇒ 原版那两道理所当然的闸，在我们这里**一条都不成立**。
        //    实测症状：选牌面板 / 设置 / 日志 / 多卡 / 联盟 / 聊天 / 展示窗 / 选目标 / 打法选择器 /
        //    回放 开着时，手牌照样悬停、拖起、落下、轻点开窗 ⇒ 窗开着还能把牌打出去。
        //
        //  ⚠️ **只读**：状态全部从 `BattleDriver` 的**公开只读口**取，⛔ 本类不另存一份状态
        //     （铁律「两处写同一条规则 = 迟早不一致」）。取的口就是显示层已经在用的那几个。
        //  ⚠️ **拿不到驱动**（`CardBaseDemo` / `Editor/HandAssertE8` / 纯表现层自检）⇒ 闸恒放行，
        //     行为与加它之前**逐位相同**。
        //  ⚠️ 与 `FrozenNow` 是**两件事、两种处置**（⛔ 别合并）：
        //     · `Frozen`（掉线 / 演出未收尾）—— 原版那道整帧闸也是直接 `return`，**拖拽态照旧留着**；
        //     · 本闸（模态窗 / 选目标 / 打法选择器 / 终局）—— 必须**把正在拖的那张收回手牌**，
        //       否则「窗打开前后松手」照样会走 `Release` 把牌打出去（见 `AbortDrag`）；
        //     · 回放那一档（`ReplayDragBlocked`）**只禁拖**：轻点看牌是原版允许的，⛔ 别一刀切关死。

        BattleDriver _driver;
        bool _driverSearched;

        /// <summary>场景里那个驱动（只读）。找到一次就缓存 —— `FindFirstObjectByType` 每帧调很贵。
        /// <para>⚠️ `GetComponent` 每次都会重试（便宜、且能自愈「旧引用被销毁」那一档）；
        /// 只有**全局查找**那一步用一次性闩（纯表现层自检里它恒找不到，别每帧扫场景）。
        /// `OnEnable` 会把闩复位（换局 / 换场景后重找）。</para></summary>
        BattleDriver Driver
        {
            get
            {
                if (_driver != null) return _driver;
                _driver = GetComponent<BattleDriver>();
                if (_driver != null) return _driver;
                if (_driverSearched) return null;
                _driverSearched = true;
                _driver = FindFirstObjectByType<BattleDriver>();
                return _driver;
            }
        }

        void OnEnable() { _driverSearched = false; }

        bool? _battleStateBlockedForTest;
        bool? _replayDragBlockedForTest;
        bool? _dragOnlyBlockedForTest;
        /// <summary>自检用：把**整帧硬闸**直接拨过去（模态窗 / 终局）。`null` = 读真机 ⇒ **生产恒为 null**。</summary>
        public void SetBattleStateBlockedForTest(bool? on) { _battleStateBlockedForTest = on; }
        /// <summary>自检用：把**回放档**（只禁拖、保留轻点看牌）直接拨过去。`null` = 读真机。</summary>
        public void SetReplayDragBlockedForTest(bool? on) { _replayDragBlockedForTest = on; }
        /// <summary>🆕 自检用（`K2`）：把「**只禁拖、保留轻点**」那一档（选目标 / 打法选择器）直接拨过去。</summary>
        public void SetDragOnlyBlockedForTest(bool? on) { _dragOnlyBlockedForTest = on; }
        /// <summary>自检读口：这一帧被**任何**一道闸挡着吗（= 整帧闸 ∨ 只禁拖那一档 ∨ 回放；**生产判据本身**）。</summary>
        public bool BattleStateBlockedNow { get { return FullBlocked() || DragOnlyBlocked() || ReplayDragBlocked(); } }
        /// <summary>自检读口：这一帧是「回放 ⇒ 只禁拖」吗。</summary>
        public bool ReplayDragBlockedNow { get { return ReplayDragBlocked(); } }
        /// <summary>自检读口：这一帧是「选目标 / 打法选择器 ⇒ 只禁拖」吗（`K2`）。</summary>
        public bool DragOnlyBlockedNow { get { return DragOnlyBlocked(); } }

        /// <summary>**整帧硬闸**：这一帧连「轻点看牌」都不许（原版那 8 扇模态窗的**全屏压暗层吃射线**
        /// ＋ 终局 `matchFinishedAndWaitingToLeave` 那一支）。两个来源都是 `BattleDriver` 的公开只读口。</summary>
        bool FullBlocked()
        {
            if (_battleStateBlockedForTest.HasValue) return _battleStateBlockedForTest.Value;
            var d = Driver;
            if (d == null) return false;                       // 没有驱动 ⇒ 纯表现层档
            // ① 终局：原版 `matchFinishedAndWaitingToLeave(+0x510)` 那一支只认「点一下离开」。
            if (d.Ctx != null && d.Ctx.IsOver) return true;
            // ② 8 扇模态窗（设置/聊天/换牌/选牌/多卡/放大/联盟/日志）——
            //    判据与 `BattleDriver.HudButtonsBlocked` **同一份**（`PointerOverBlockingUi` 就是转发它），
            //    ⛔ 本类不另抄一张窗口清单。
            if (d.PointerOverBlockingUi()) return true;
            return false;
        }

        /// <summary>🔴🆕 **2026-10-25（`K2` · 就地订正 `AUD8` #12 的一处误判）**：**只禁拖、保留轻点**那一档
        /// = 选目标（打法已定，原版 UIstate 9/10/13）＋ 打法三选一开着（原版 UIstate 6）。
        ///
        /// <para>为什么**不能**把它并进整帧闸（判据 = 全量反编译现读）：
        /// ① **拖**：`dc/CardScript__OnTouchDrag.c:17-23` 要求 `UIstate == 1` ⇒ 这两档**拖不起来**（我们照办）。
        /// ② **轻点看牌**：`dc/CardScript__OnTouchUpAsButton.c:14-17` 只挡 `UIstate == 0xe`（= `choosingResolutionCard`），
        /// 真正的开窗闸在 `dc/BattleManager__DisplayCard.c:14-18`：
        /// <c>UIstate != 0 &amp;&amp; UIstate−7 &gt; 1 &amp;&amp; UIstate−11 &gt; 1</c> ⇒ **只挡 {0,7,8,11,12}**
        /// （inactive / finishBattleSequence / postBattle / mulligan / mulliganFinished）。
        /// ⇒ 选目标(9/10/13) 与 打法选择器(6) 那一档，原版**照样能轻点手牌开窗**。
        /// ③ 悬停预览（`ShowCardInHand`）另有一道 `CanDisplayHandCard` = `isPlayer &amp;&amp; cardState==1 &amp;&amp; UIstate==1`
        /// （`dc/BattleManager__CanDisplayHandCard.c:7-11`）⇒ 这两档里**不抬牌、不重排**（见 `UpdateHover`）。
        ///
        /// <para>🔴 **改前（`X19` 那一版）**：这两档跟模态窗混在一个「硬闸」里 ⇒ `Update` 整帧 return
        /// ⇒ **选目标时点手牌一点反应都没有**（原版会开窗）。</para></summary>
        bool DragOnlyBlocked()
        {
            if (_dragOnlyBlockedForTest.HasValue) return _dragOnlyBlockedForTest.Value;
            var d = Driver;
            if (d == null) return false;
            if (d.IsTargeting) return true;       // 原版 UIstate 9/10/13
            if (d.SelectorOpen) return true;      // 原版 UIstate 6（打法三选一）
            return false;
        }

        /// <summary>**这一帧「不许拖出去打」吗** —— 三条闸合一：回放（`matchType == 0xA0`）· 选目标 / 打法选择器（`UIstate != 1`）· **手牌重排动画中**（`IsDoingAnim` = 原版 `CanMoveHandCard` 的 `!playerHand.animFlag`）。
        /// 第三条的判据 / 反证 / 为什么用 `IsDoingAnim` → **类尾「`DragBlocked` 的第三条」那一整段**（⛔ 别在这儿写第二份）。
        /// <para>⚠️ **不包含**整帧闸（那个连轻点都不许，见 <see cref="FullBlocked"/>）。</para></summary>
        bool DragBlocked() { return ReplayDragBlocked() || DragOnlyBlocked() || IsDoingAnim; }

        /// <summary>**回放档**：原版 `OnTouchDrag.c:17-23` 的 `matchType != 0xA0`
        /// —— 它**只禁「拖出去打」**，不禁「轻点看牌」：
        /// `OnTouchDown` / `OnTouchUpAsButton` 全文没有 0xA0 闸，而 `CanDisplayHandCard` 也只要求
        /// `cardState==1 && UIstate==1`（`dc/BattleManager__CanDisplayHandCard.c:7-11`，**不看 matchType**）。
        /// ⇒ 回放里：轻点手牌**照样开卡面展示窗**，但**拖不起来**（也不出牌、不播 cantdo）。</summary>
        bool ReplayDragBlocked()
        {
            if (_replayDragBlockedForTest.HasValue) return _replayDragBlockedForTest.Value;
            var d = Driver;
            return d != null && d.ReplaySession;
        }

        /// <summary>把「按下已记、还没起拖」那一档丢掉（**不动** `_dragging`，那是 `AbortDrag` 的事）。</summary>
        void DiscardPress()
        {
            _pressPending = false;
            _pressPendingCard = null;
            _pressInside = false;
        }

        /// <summary>🔴 **把正在拖的那张收回手牌**（战斗状态闸触发时）。
        ///
        /// <para>与原版同一结局：`BattleManager__Update.c` case 4 的松手支 = `CardScript.MoveBackToHand`
        /// + `PlayerHand.HandCardReleased` ⇒ 就是「回手 + 手牌重排」，**没有回弹专用补间**。</para>
        /// <para>⛔ **不结算、不发 `OnReturned` / `OnIllegalAction`**：这一下不是「玩家拖空了」，
        /// 而是「这一拖被打断」—— 播 `cantdo` 或写「拖到目标上再松手」都会误导玩家。
        /// 不静默：打一行日志。</para>
        /// <para>⚠️ `ClearDropIndicator()` 会顺手把驱动层的**让位预览**收回去
        /// （`OnDropPreview(null,-1)` → `EndPreviewReturn` → 被推开的单位补回真格位）。</para></summary>
        void AbortDrag(string why)
        {
            var card = _dragging;
            _dragging = null;
            _insertIndex = -1;
            SetHovered(-1);
            DiscardPress();
            DropAccepted = false;     // 🔴 2026-10-30（`B99_B1`）：⛔ 别让「取消」继承上一手残留的 `DropAccepted`（否则 `EndPreviewReturn` 见 `true` 就只清不送回）
            ClearDropIndicator();
            if (card != null)
            {
                card.SetHighlight(CardHighlightState.Normal);
                Debug.Log("[CardPresentation] 拖拽被中止（" + why + "）⇒ " + card.name
                        + " 收回手牌（原版 `MoveBackToHand` 同一结局，⛔ 不结算、不播 cantdo）");
            }
            Relayout(true);      // 回弹由手牌重排完成（原版同一个收口）
        }

        // ==================================================================
        //  🆕 2026-10-24（`J3`）**手牌重排的「动画闸」**
        // ==================================================================
        // 判据（全量反编译亲读；原版这里是**两件事**，都在 `PlayerHand.MoveCardsInHandToPosition` 里）：
        //   ① **`isDoingAnim`（`PlayerHand` +0x61）** —— 协程**开头**置 1、`WaitForSeconds(timeToPositionCard)`
        //      之后置 0（`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c`：置 1 在 `:33-35`、
        //      `WaitForSeconds` 在 `:104-106`、置 0 在 `:134`；读口 = `PlayerHand__IsDoingAnim.c`：
        //      `return *(bool*)(this + 0x61);`）。
        //      ⚠️ **只在「重排整把牌」时置位**：`:33` 那句是
        //      `if (*(int*)(state + 0x30) == -1) *(bool*)(hand + 0x61) = 1;`
        //      （`+0x30` = `MoveCardsInHandToPosition(int selectedCard, …)` 的 selectedCard 实参）
        //      ⇒ `CardScript.ShowCardInHand`（传**卡序号**）那条**不置** isDoingAnim；
        //      而 `StopShowingCardInHand(restart:true)` / `HandCardReleased` / `HandCardMovedToNewIndex`
        //      / 抽牌那几条协程传的都是 **-1** ⇒ 置位。
        //      ⚠️ **`decomp_full` 的 25,096 个方法体里没有任何一处调用 `PlayerHand.IsDoingAnim()`**
        //      （唯一命中是它自己的定义）⇒ 这个读口在这份 build 里**要么被内联进了没解出来的方法、要么是死码**；
        //      ⛔ **不能**拿它当「原版靠它挡输入」的唯一证据。
        //   ② **卡碰撞体开关**（**真正挡输入**的那一条）：协程状态 0 对名单里**每一张**牌调
        //      `CardCollider2D.Toggle(false)`（`:44-47`：`*(card + 0x140)` = `CardCollider2D`，
        //      虚表 +0x1F8 = `Toggle`、实参 0；体 = `Behaviour.set_enabled(boxCollider2D, false)`
        //      ＋ 顺手关掉 `colliderShowInBig`），状态 1（等完 0.2 s 之后，`:126-127`）再对每张调
        //      `CardCollider2D.SetColliderInHand()` —— 它的**末尾**就是 `Toggle(true)`。
        //      ⇒ 原版语义 = **重排补间这 0.2 s 里，手牌一张都点不到**；
        //        时长 `timeToPositionCard` = 0.2 s（`VarsGlobal +0xA4`）= `CardTween.RelayoutDuration` = 补间时长本身。
        //   ⚠️ 我们**没有卡碰撞体开关**这一层（命中走 `CardView.HitByCollider` 的常开 collider），
        //      所以闸落在**交互层**：闸在时 `UpdateHover` / `PickUp` / `BeginPressCandidate` 一律不收新动作。
        //   ⚠️ **只对「整把重排」那几条路开闸**（`SetCards` / `Release` / `EndPressCandidate`）；
        //      `UpdateHover` / `UpdateDrag` / `PickUp` 那几个叫位**不开** —— 原版那几条对应的是
        //      `ShowCardInHand(selectedCard ≠ -1)` 与 `DisplayHandWithCardSpace`（**后者根本不碰碰撞体**），
        //      而且我们的悬停是**连续**鼠标模型，开闸会让指针扫过手牌时每 0.2 s 冻一次。
        //      ⚠️ **如实记（没做的部分）**：原版 `ShowCardInHand` 那条路**也**会 `Toggle(false)`；
        //      它靠 `StopShowingCardInHand` 里的 `SetCollidersShowBig(false, 2)` → `Toggle(true)` **立刻**补回来。
        //      这一格**我们没复刻**（判据在本段，做法留待真机验收后再定）。
        //   ⚠️ **动画没开就不开闸**（`hand.animateRelayout == false` = 批处理/演示档，位置当场落）：
        //      没有动画就没有「重排期间」，开闸只会让自检凭空红。
        Tween _animGate;

        /// <summary>原版 `PlayerHand.IsDoingAnim()`（+0x61）的等价物：**手牌重排动画进行中**。
        /// ⚠️ 它只覆盖「整把重排」那几条路（见上面那段判据 ①），⛔ 别拿它当全局冻结（那个是 `FrozenNow`）。</summary>
        public bool IsDoingAnim { get { return _animGate != null && _animGate.IsActive(); } }

        /// <summary>自检用：闸现在开着吗（转调生产口径 <see cref="IsDoingAnim"/>，⛔ 别另算一份）。</summary>
        public bool IsDoingAnimForTest { get { return IsDoingAnim; } }

        /// <summary>自检用：闸还剩多久（秒；没闸 ⇒ 0）。</summary>
        public float AnimGateRemainingForTest
        {
            get
            {
                if (!IsDoingAnim) return 0f;
                return Mathf.Max(0f, _animGate.Duration(false) - _animGate.Elapsed());
            }
        }

        /// <summary>开闸 = 原版协程状态 0 的那两句（`isDoingAnim = 1` ＋ 每张 `Toggle(false)`），
        /// 时长 = `WaitForSeconds(timeToPositionCard)` = 重排补间时长。
        /// <para>⚠️ 计时用 `DOVirtual.DelayedCall`（走 DOTween 自己的时间轴），**不用 `Time.time`**：
        /// 批处理自检把 `CardTween.Mode` 拨成 `Manual` 之后，只有 DOTween 的补间会被
        /// `CardTween.Advance(dt)` 推进（`Time.time` 是墙钟，不会跟着喂的 dt 走 ⇒ 自检验不了闸）。</para></summary>
        void ArmAnimGate()
        {
            if (_animGate != null) _animGate.Kill();       // 新的重排开始时，旧的窗口作废（原版也是每次重开协程）
            _animGate = DOVirtual.DelayedCall(CardTween.RelayoutDuration, () => { _animGate = null; })
                                .SetUpdate(CardTween.Mode);
            if (CardTween.LinkEnabled) _animGate = _animGate.SetLink(gameObject);
        }

        /// <summary>
        /// 🔴 **2026-10-01 加：请求的落点 → 它真正会落在哪一格**（棋盘是**连续无洞**模型，
        /// 插进中间会把后面的单位整体外移一格 ⇒ 「拖到哪」和「落在哪」不是一回事）。
        /// 由战斗驱动层接 `BoardSlots.Resolve`（**同一份判据**，别在这里另算一套）。
        /// 没接时退化成恒等（表现层自检照样能跑）。
        ///
        /// 用途两处，都必须是**同一个**答案：① 落点指示画在哪一格；② 松手后那张卡飞向哪一格。
        /// </summary>
        public Func<int, int> DropLandingSlot;

        /// <summary>🔴 **2026-10-01：上一次松手是不是「真的把牌打下去了」**（单位卡落在合法格位上）。
        /// 用途只有一个：告诉驱动层**别把「让位预览」推开的那几张送回去** —— 引擎马上就会把这张牌插进去，
        /// 它们本来就该站在预览位上（送回去再插出来 = 抖两下）。驱动层在真正出牌那一刻（`DoPlay`）清掉它。</summary>
        public bool DropAccepted;

        /// <summary>
        /// 🔴 **2026-10-01 加：「让位」预览** —— 拖动单位牌时，原版会把**落点及其外侧**的单位
        /// **实时外移一格**（`MinionManager.ReassembleMinionsWhilePlayingUnit`，逐帧调），
        /// 让玩家看见「它会插在哪」。
        /// 实参 = (哪块棋盘, 请求的落点格号 或 −1 = 收工/没有预览)。由驱动层负责挪视图。
        /// </summary>
        public Action<BoardLayout, int> OnDropPreview;

        readonly List<CardView> _cards = new List<CardView>();
        readonly Dictionary<int, CardView> _placed = new Dictionary<int, CardView>();
        Vector3 _pressWorld;      // 按下时指针在哪（判「轻点」用）
        float _travel;            // 这一拖走了多远
        /// <summary>这一按**按下的时刻**（`Time.timeSinceLevelLoad`）= 原版 `CardScript +0x800`
        /// （写 = `CardScript__OnTouchDown.c:26-27`，读 = `CardScript__OnTouchUpAsButton.c:37`）。
        /// 唯一用途：长按 ≥ <see cref="LongPressNoDisplaySeconds"/> ⇒ 松手**不开**卡面展示窗。</summary>
        float _pressTime;
        /// <summary>🆕 **自检口**（`K2`）：把「这一按已经按了多久」直接钉成秒数。
        /// <para>为什么必须有它：批处理里 `Time.timeSinceLevelLoad` **不前进**（没有帧循环）⇒
        /// 真读的话 elapsed 恒 ≈0 ⇒ 长按那一档**永远验不到**（会退化成假绿）。
        /// `null`（生产恒为 null）= 读真时间。</para></summary>
        float? _pressElapsedForTest;
        /// <summary>最近一次 `Release` 打出去的那一行日志（自检读它，见 `LastReleaseLogForTest`）。</summary>
        string _lastReleaseLog;

        /// <summary>这一按已经按了多久（秒）。判据只此一处：`Time.timeSinceLevelLoad − _pressTime`
        /// （= 原版 `OnTouchUpAsButton.c:37` 那一句），自检可钉。</summary>
        float PressElapsedSeconds
        {
            get { return _pressElapsedForTest ?? Mathf.Max(0f, Time.timeSinceLevelLoad - _pressTime); }
        }

        /// <summary>**松手这一刻该不该当成「轻点」**（原版 `OnTouchUpAsButton` 的时长门）：
        /// `elapsed &lt; 0.2` ⇒ 开窗；`&gt;= 0.2`（长按）⇒ **不开**，只 `StopShowingCardInHand`。
        /// 判据 → <see cref="LongPressNoDisplaySeconds"/>。⛔ 别在别处再写一个时长阈值。</summary>
        bool PressShortEnough { get { return PressElapsedSeconds < LongPressNoDisplaySeconds; } }
        // 🆕 **2026-10-19（战斗线 `T16` · 命中区普查）**：这一按**是否还按在卡的命中区里**
        //   —— 原版 `CardCollider.allowClick`（`+0x44`）的等价物。
        //   判据 = `d:/2/tools/decomp_full/CardCollider__OnPointerDown.c`（置 1）与
        //          `CardCollider__OnPointerExit.c`（**清 0**，且**回到卡上不会自己恢复** —— 只有下一次
        //          `OnPointerDown` 才重新置 1）⇒ 这是**锁存**，不是「松手时在不在卡上」的实时判断。
        //   后果（改前 vs 原版）：按在卡缘、指针挪出卡外 0.1 再松手 —— 原版**不产生点击**，
        //   我们靠 `_travel < TapThreshold(0.3)` 会判成轻点 ⇒ **两版结果相反**（`C3` 结构差异 #8）。
        //   ⚠️ 只在**候选态**（`TouchInputOn` 档）能真正分辨出来：那一档卡还没跟手。
        //   拖拽态也锁存（卡跟手后指针几乎不会出框，属防御性对齐）。
        bool _pressInside;

        int _hovered = -1;
        CardView _dragging;
        int _insertIndex = -1;        // 拖拽中：这张牌会插到第几个位置（手牌边拖边让位）
        Vector3 _grabOffset;          // 抓起时指针和卡中心的差，拖拽时保持它，手感才不跳
        // 🔴 2026-10-19（`C3` #11）：**按下已记、还没过起拖阈值**那一档（只在 `TouchInputOn` 档用）。
        //   原版 `CardCollider.OnDrag` 每次拖拽事件都试一次 `TryDraggingFromHand`，过了两道阈值才
        //   `StartMovingFromHand`；PC 档四条条件一条都不拦 ⇒ 这个候选态**永远不出现**（行为不变）。
        bool _pressPending;
        CardView _pressPendingCard;   // 按下时压在指针下的那张（原版 `pointerDownPosition` 属于它）
        // 🆕 2026-10-01：**当前点亮着落点指示的那块棋盘与格号**（-1 = 没点）——
        //   换盘/灭掉时要知道该去清谁（两块棋盘各有一份九宫底片）。
        BoardLayout _dropLitBoard;
        int _dropLitSlot = -1;

        public int HoveredIndex { get { return _hovered; } }
        /// <summary>指针下那张手牌（`_hovered` 在 `_cards` 范围内才非空）。
        /// <para>🆕 2026-10-24（`J3`）：**重排动画闸**把 `UpdateHover` 冻住的那 0.2 s 里，`_cards`
        /// 可能已经被 `SetCards` 换掉（比如联机同步把牌打出去、手牌变短），而 `_hovered` 还是**旧下标**
        /// ⇒ 按下去会用旧下标取列表。这里统一夹一次，⛔ 别在三个调用点各写一遍 `_hovered &lt; Count`。</para></summary>
        CardView HoveredCard
        {
            get { return (_hovered >= 0 && _hovered < _cards.Count) ? _cards[_hovered] : null; }
        }

        /// <summary>🆕 2026-10-25（`E` · 账 `战斗界面_HUD` R42）：**抬牌态不许点开卡面展示窗** ——
        /// 原版 `BasicCardUI.allowOpenCardDisplayOnTouch` 的等价判据。
        /// <para>判据（🥇 ILSpy）：`CardScript.OnTouchEnter:10038`（悬停入口）→ `ShowCardInHand:10484` →
        /// **`ToggleOpenCardDisplayOnTouch(false):10597`** ⇒ 把 uGUI `OnClick` 的监听器**摘掉**
        /// （`BasicCardUI.ToggleOpenCardDisplayOnTouch:700-710` 是 `RemoveListener` + `if (option) AddListener`）；
        /// `StopShowingCardInHand:11001` → `:11034` `(true)` 才挂回。⇒ **抬牌期间点这张卡不开展示窗**。</para>
        /// <para>⚠️ 我们这边「抬牌」= `UpdateHover` 里的 `_hovered` 且**没被 `DragOnlyBlocked()` 挡**
        /// （那条闸与 `CanDisplayHandCard` 同源：选目标(9/10/13)/打法三选一(6) 那些态原版**根本不抬牌**
        /// ⇒ 那里点手牌照旧开窗，与原版一致；见 `UpdateHover` 里那段 `K2` 注释）。</para>
        /// <para>⛔ 别把它写成「只要是手牌就抑制」：`StopShowingCardInHand` 之后（含那些不抬牌的态）
        /// 原版**是允许开窗的**。</para></summary>
        public bool CardDisplaySuppressedFor(CardView card)
        {
            return card != null && card == HoveredCard && !DragOnlyBlocked();
        }
        public CardView Dragging { get { return _dragging; } }
        public bool IsDragging { get { return _dragging != null; } }
        public int InsertIndex { get { return _insertIndex; } }
        public IReadOnlyDictionary<int, CardView> Placed { get { return _placed; } }

        /// <summary>把「当前手牌是哪几张」推给交互层。
        ///
        /// <paramref name="relayout"/> = **要不要让手牌布局走「带补间的重排」**（默认 `true`，老调用点行为不变）。
        /// 🔴 **2026-10-22（W63 · 换牌那排卡是斜的）**：换牌期传 `false` —— 那几张牌的位姿归
        ///   `MulliganPanel`（`BuildCardButtons` / `ReapplyRowPoses`，**平 0° 横排**），
        ///   而 `HandLayout.Place` 在实机上开的是 **0.2 s 带旋转的重排补间**（`animateRelayout == 1`）：
        ///   它算的是**手牌扇形角** ⇒ 补间的旋转尾巴在那 0.2 s 末尾落盘，把换牌区那一行带歪
        ///   （位置/尺度会被面板当场改回来，角度不会）。这里**连补间都不让它开**，从根上断掉。
        /// <para>⚠️ **但位姿仍要当场算一遍**（`animateRelayout` 临时关掉，跑完还原）—— 两处都靠它：
        ///   ① `HandLayout._lastPose` 是 `CommitDealtPoses` 落实新牌的**唯一来源**；
        ///   ② `MulliganPanel.BuildCardButtons` 的 `_savedPos/_savedRotZ/_savedScale` 存的是
        ///   「搬进换牌区之前」的位姿（面板 `Close()` 时原样还回去）。不算这一遍的话那几个值会是
        ///   卡视图的初始位（驱动根原点 = **屏幕正中**）⇒ 点「完成换牌」那一刻手牌会从屏幕中间飞下来。
        ///   本方法写出来的位姿在**同一帧**里就会被 `MulliganPanel` 覆写成换牌区那一行（⛔ 不会被渲染出来）。</para>
        ///   ⚠️ `_cards` 照样更新（换牌面板的命中 `CardAt` 转发本类的 `HitTest`，靠的就是它）。</summary>
        public void SetCards(List<CardView> cards, bool relayout = true)
        {
            ResetShowInHandAll(); _cards.Clear();   // 换列表前把「抬起档」从旧卡上摘掉（判据/理由 → 类尾 ResetShowInHandAll）
            _cards.AddRange(cards);
            if (relayout) { Relayout(); return; }
            bool animate = hand.animateRelayout;
            hand.animateRelayout = false;
            try { Relayout(false); }      // 换牌期：位姿要算、补间一条都不开 ⇒ 也不开闸
            finally { hand.animateRelayout = animate; }
        }

        /// <summary>
        /// 清掉「哪些槽已经摆了牌」的记录。**重开一局必须调** ——
        /// 不清的话上一局摆过的槽全都还占着，新一局往那些槽拖会被判成「这格有牌」而弹回手上，
        /// 表现为「打过的格子再也放不了牌」（2026-09-12 接原版卡牌时在自检里撞到的：
        /// 第二次 `Begin()` 之后拖第一张就落不下去，根因就是这里）。
        /// </summary>
        public void ClearPlaced()
        {
            foreach (var kv in _placed)
                if (kv.Value != null) kv.Value.SetHighlight(CardHighlightState.Normal);
            _placed.Clear();
            _dragging = null;
            _insertIndex = -1;
            SetHovered(-1);
            ClearDropIndicator();     // 🆕 2026-10-01：重开一局时别把上一次的落点指示留着
        }

        void Update()
        {
            if (cam == null || hand == null || board == null) return;
            if (_cards.Count == 0) return;
            // 🔴 2026-10-25（`AUD8` #19）：**战斗状态闸 + 冻结闸**收成一处（`BlockedThisFrame`）——
            //    `Update` 与自检口 `SimulateTickForTest` 走**同一个**判据，⛔ 别再抄第二份。
            if (BlockedThisFrame()) return;

            // ⚠️ 用**带野值防护**的那个（见 `PointerWorldSafe` 的注释）——
            //    2026-10-15（`A2107`）起**落点判定与「推上线」守卫取的都是它** ⇒ 它野一帧，两处一起判错。
            Vector3 world = PointerWorldSafe();

            // 松手/按下在 Update 里轮询（批处理下没有输入事件，轮询才验得了）
            TickInput(world, PressedThisFrame(), ReleasedThisFrame());
        }

        /// <summary>这一帧的两道闸（返回 true = `Update` 就该 return）。判据只此一处。
        /// <para>① **整帧闸**（模态窗 / 终局）—— 原版那 8 扇窗的**全屏压暗层吃射线** ＋
        /// `matchFinishedAndWaitingToLeave` 那一支。**必须把 `_dragging` 收回**（`AbortDrag`），
        /// 否则这一档过去之后松手照样结算。</para>
        /// <para>①′ **「只禁拖」那一档**（回放 / 选目标 / 打法选择器）—— 原版 `OnTouchDrag` 的
        /// `matchType != 0xA0` ∧ `UIstate == 1`。**不整帧 return**：那几档里原版**照样能轻点手牌开窗**
        /// （判据 → `DisplayCard.c:14-18`，见 `DragOnlyBlocked` 的 doc）⇒ 只把手里那张收回，
        /// 之后的按下走「候选态」，过阈值那一步由 `TickPressCandidate` / `PickUp` 拒掉。</para>
        /// <para>② **冻结闸**（掉线 / 演出未收尾）—— 原版那道整帧闸也是直接 `return`，**`_dragging` 照旧留着**
        /// （重连回来那一下松手会照常结算，判据 → `Frozen` 的注释）。</para></summary>
        bool BlockedThisFrame()
        {
            // ① 整帧闸。⚠️ 排在冻结之前：两者同时成立时，宁可收回也不要留着（模态窗不会自己消失）。
            if (FullBlocked())
            {
                if (_dragging != null) AbortDrag("整帧闸（模态窗 / 终局）");
                DiscardPress();
                return true;
            }
            // ①′ 「只禁拖」那一档：**只**处理「已经拖在手里」那一种（闸是在拖到一半时落下的）。
            //     平时到不了这儿 —— `PickUp` 在这一档根本不放行（见 `DragBlocked`）。
            if (DragBlocked() && _dragging != null)
                AbortDrag(ReplayDragBlocked()
                          ? "回放局（原版 `CardScript__OnTouchDrag.c:17-23` 的 0xA0 闸）"
                          : (IsDoingAnim ? "手牌重排动画中（原版 `playerHand.animFlag` ⇒ `CanMoveHandCard` 假）" : "选目标 / 打法选择器（原版 `UIstate != 1` ⇒ `OnTouchDrag` 早退）"));

            // ② 冻结闸（原样；只把「丢候选」抽成 `DiscardPress`，语义一字未改）
            if (FrozenNow)
            {
                // 🔴 2026-10-19（`C3` #11）：候选态**直接丢弃** —— 它没有任何可见状态（卡还在原位），
                //   留着只会在解冻后拿一个过期的按下点去判阈值（甚至跨一次松手）。
                // ⚠️ 与 `_dragging` 那档**不同**：拖拽中的牌照旧留着（原版那道整帧闸也是直接 return）。
                DiscardPress();
                if (!_frozenWarned)
                {
                    _frozenWarned = true;
                    // 🔴 2026-10-19（B2）：**文案订正** —— 这一档现在有**两个**来源（`Frozen` 那两个或项），
                    //   原来那句只写「对手掉线/等重连」，而 `WaitingForBlockingAction`（演出没演完 / 出牌飞行中）
                    //   也会亮它 ⇒ 日志会误导后面查问题的人。
                    Debug.LogWarning("[Hand] 手牌这一层**暂时禁操作** —— 两种来源（见 `Frozen`）："
                        + "① 对手掉线 / 等重连（原版 `BattleManager__Update.c:104-115` 那道整帧闸）；"
                        + "② 上一条动作的演出还没演完 / 自己打出的牌还在 0.65 s 飞行里"
                        + "（等价原版 `WaitingForBlockingAction`）。—— 这一档结束会自动恢复。");
                }
                return true;
            }
            _frozenWarned = false;
            return false;
        }

        /// <summary>按下 / 松手 / 候选态推进（`Update` 与自检共用的**唯一**一份）。
        /// <para>⚠️ 边沿由调用方给：生产读设备（`PressedThisFrame` / `ReleasedThisFrame`），
        /// 自检直接喂（批处理里 `Mouse.current == null` ⇒ 设备那两支恒 false）。</para></summary>
        void TickInput(Vector3 world, bool pressedNow, bool releasedNow)
        {
            if (_dragging == null) UpdateHover(world);
            else UpdateDrag(world, Time.deltaTime);

            if (pressedNow)
            {
                var hc = HoveredCard;      // 🆕 `J3`：带范围夹取（闸把 `UpdateHover` 冻住时 `_cards` 可能已变短）
                if (hc != null && _dragging == null)
                {
                    // 🔴 2026-10-19（`C3` #11）：**两档行为**
                    //  · `TouchInputOn`（原版 `GameStaticData.touchInput`）**关** ⇒ PC 档：按下即抓。
                    //    原版 `TryDraggingFromHand` 那四道条件在 PC 上一条都不拦（`:64-69` 的
                    //    `if (touchInput) return;` 整段不成立）⇒ **玩家看到的行为与原来逐位相同**。
                    //  · **开** ⇒ 只记候选（原版 `CardCollider.OnDrag` 也是**拖拽事件**里才
                    //    `StartMovingFromHand`，不是按下那一刻）—— 过两道阈值见 `TickPressCandidate`。
                    // 🔴 2026-10-25（`AUD8` #19）：**回放档** —— 只允许「轻点看牌」，
                    //    用候选态承接（松手没位移 = `EndPressCandidate` → `OnTapped` → 卡面展示窗）；
                    //    真拖过阈值那一步由 `TickPressCandidate` / `PickUp` 拒掉（原版 0xA0 闸）。
                    // 🔴🆕 2026-10-25（`K2`）：**`DragBlocked()` 而不只是回放** —— 选目标 / 打法选择器
                    //    那两档原版也是「拖不动、但轻点看牌照旧」（判据 → `DragOnlyBlocked` 的 doc）⇒
                    //    同样走候选态。⛔ 别把它们并回整帧闸（那会让「选目标时点手牌」毫无反应）。
                    if (DragBlocked()) BeginPressCandidate(hc, world);
                    else if (TouchInputOn) BeginPressCandidate(hc, world);
                    else PickUp(hc, world);
                }
            }
            else if (releasedNow)
            {
                if (_dragging != null) Release(world);
                // 候选态松手 = 原版 `OnPointerClick` 那一档（卡从没离开手牌）—— 见 `EndPressCandidate`
                else if (_pressPending) EndPressCandidate(world);
            }
            else if (_pressPending)
            {
                // 原版每次拖拽事件都试一次；我们每帧试一次（阈值是位移的纯函数，多试不会多抓）
                TickPressCandidate(world);
            }
        }

        // ---- 悬停 ----
        void UpdateHover(Vector3 world)
        {
            // 🆕 2026-10-24（`J3`）：重排动画闸 —— 原版这 0.2 s 里手牌碰撞体全是关的
            //   （`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c` 的 `Toggle(false)`），
            //   指针进出**不会**产生新的 hover。判据/取舍见 `_animGate` 那段。
            if (IsDoingAnim) return;
            int hit = HitTest(world);
            // 🔴 2026-10-22（`W63` · 判据见 `IsRightButtonOrTwoFingerTouch` 上面那一整段）：
            //   原版 `OnTouchEnter` 第一句就是那道闸 ⇒ **右键按着 / 双指以上 ⇒ 不许「进入」新卡**。
            //   ⚠️ **离开照旧**：`OnTouchExit` 通篇没有 `GetMouseButton(1)` ⇒ 指针真挪出手牌时 hover 照样清掉。
            //   ⛔ 别把这两条「统一」成「右键期间整段冻结」—— 原版右键拖战场时手牌是会落下去的。
            if (hit != _hovered && hit >= 0 && IsRightButtonOrTwoFingerTouch()) return;
            if (hit == _hovered) return;
            SetHovered(hit);
            // 🔴🆕 2026-10-25（`K2`）：**选目标 / 打法选择器那一档不抬牌、不重排**。
            //   判据 = 原版手牌悬停预览走 `ShowCardInHand`，而它的闸 `BattleManager.CanDisplayHandCard`
            //   要求 `isPlayer && cardState==1 && UIstate==1`（`dc/BattleManager__CanDisplayHandCard.c:7-11`）
            //   ⇒ UIstate 9/10/13（选目标）/ 6（打法三选一）时**根本不进 `ShowCardInHand`**。
            //   ⚠️ 但 `_hovered` **必须照常更新**：原版那颗 `CardCollider` 一直开着，
            //   `OnPointerClick` 照样落到卡上（开窗那道闸只挡 {0,7,8,11,12}，见 `DragOnlyBlocked`）
            //   ⇒ 「选目标时点手牌看牌」在原版是**允许的**，我们靠 `_hovered` 才接得到那一下。
            //   ⛔ 别把这句写成 `return`（那会把轻点一起断掉，`HoveredCard` 恒 null）。
            if (DragOnlyBlocked()) return;
            Relayout(false);      // 悬停档 = 原版 `ShowCardInHand`（不置 isDoingAnim）⇒ 不开闸（见 `_animGate` 那段）
        }

        // ---- 拖拽 ----

        // 🔴 2026-10-19（`C3` #11）：`TouchInputOn` 档的三段式 —— 按下只记、过阈值才抓、没抓就松手算轻点。
        //  判据 = `CardScript__TryDraggingFromHand.c`（阈值）＋ `CardCollider__…OnDrag.c`（**拖拽事件**里才试，
        //  不是按下事件里）＋ `CardScript__StartMovingFromHand`（起拖用的**当前点**，不是按下点：
        //  `ScreenToWorldPoint(*param_3)` = 当前指针）。三段都只被 `Update` 与自检口调用。

        /// <summary>按下：**只记候选**，不抓（原版 `TryDraggingFromHand` 还没过阈值时什么也不做）。
        /// `_pressWorld` 就是原版 `CardCollider.pointerDownPosition` 那一格（按下点）。</summary>
        void BeginPressCandidate(CardView card, Vector3 world)
        {
            if (card == null) return;
            // 🆕 2026-10-24（`J3`）：重排动画闸（判据/取舍见 `_animGate` 那段）—— 候选态也不许起。
            if (IsDoingAnim) return;
            _pressPending = true;
            _pressPendingCard = card;
            _pressWorld = world;
            _travel = 0f;
            // 🆕 2026-10-25（`K2`）：按下时刻 —— 原版 `CardScript +0x800`（`OnTouchDown.c:26-27`）。
            //   候选态也是「按下」，所以这里就要记（`OnTouchUpAsButton` 量的是**按下→松手**的总时长，
            //   与中间有没有起拖无关）。
            _pressTime = Time.timeSinceLevelLoad;
            // 🆕 2026-10-19（T16）：按下那一刻 = 原版 `CardCollider.OnPointerDown` ⇒ `allowClick = 1`
            //   （`HitTest` 刚判过这张卡，所以这里恒 true；写成 `HitByCollider` 是为了**同一判据**、不另立口径）。
            //   🔴 **2026-10-19（`T09` 步骤 B · `W57` · B2a）就地换口（铁律 5）**：原来这里走
            //   `card.Contains`（手算矩形）。`Frontmost` 已改走真 collider ⇒ 锁存必须**同一个口**，
            //   否则「按在卡上」与「这一按还在卡上」会变成两套判据（原版两处都来自同一颗 `Collider2D`
            //   的 `OnPointerDown` / `OnPointerExit`）。
            _pressInside = card.HitByCollider(world);
        }

        /// <summary>候选态每帧推进：过两道阈值 ⇒ 真 `PickUp`（用**当前**指针位，见上面那条判据）。</summary>
        void TickPressCandidate(Vector3 world)
        {
            // 🔴 2026-10-22（`W63`）：过阈值这一跳与原版 `TryDraggingFromHand` 在**同一道闸**内
            //   （`CardScript__OnTouchDrag.c:19-21`，阈值那一段也在闸里）。
            //   ⚠️ 早退**不清候选态** —— 原版 `pointerDownPosition` 也不清，闸一放开接着按原按下点判。
            if (IsRightButtonOrTwoFingerTouch()) return;
            var card = _pressPendingCard;
            // 候选期间手牌列表可能被 `SyncHand` 换过（换掉的牌 / 打出的牌）—— 不在了就静默收工
            if (card == null || _cards.IndexOf(card) < 0)
            {
                _pressPending = false;
                _pressPendingCard = null;
                _pressInside = false;   // 🆕 T16：候选态被丢弃 ⇒ 这一按的锁存也一起清
                return;
            }
            float d = Vector3.Distance(world, _pressWorld);
            if (d > _travel) _travel = d;
            // 🆕 2026-10-19（T16）：原版 `CardCollider.OnPointerExit` ⇒ `allowClick = 0`（**锁存**）。
            //   候选态卡片原地不动 ⇒ 指针一旦出框就是真出框；回到卡上也不恢复（与反编译一致）。
            //   🔴 `T09` 步骤 B · B2a：判据 = 真 collider（与 `Frontmost`/按下那一处**同一个口**）。
            if (_pressInside && !card.HitByCollider(world)) _pressInside = false;
            if (!HandDragPastThreshold(_pressWorld, world)) return;

            // 🔴 2026-10-25（`AUD8` #19 · `K2` 扩档）：**「只禁拖」那一档** —— 过了阈值也**不许抓**
            //   （原版 `OnTouchDrag` 的 `matchType != 0xA0` ∧ `UIstate == 1` 两道闸）。
            //   丢掉候选态：这一按到此为止（松手不会再变成轻点 —— 玩家确实已经拖过了）。
            //   ⛔ 不静默：出声一次（每一下一行，不刷屏）。
            if (DragBlocked())
            {
                _pressPending = false;
                _pressPendingCard = null;
                _pressInside = false;
                Debug.Log("[CardPresentation] 这一档 **拖不起来**（原版 `CardScript__OnTouchDrag.c:17-23`："
                        + (ReplayDragBlocked() ? "`matchType != 0xA0`（回放）" : (IsDoingAnim ? "手牌重排动画中（原版 `playerHand.animFlag`）" : "`UIstate == 1`（选目标 / 打法选择器）"))
                        + "）⇒ 这一按作废；轻点看牌照常允许。");
                return;
            }

            _pressPending = false;
            _pressPendingCard = null;
            Debug.Log($"[CardPresentation] 起拖过阈值（视口位移² > {HandDragMinDeltaSq} 且与竖直向上夹角 ≤ {HandDragMaxAngleDeg}°）"
                      + $" ⇒ 抓起 {card.name}（原版 `CardScript__TryDraggingFromHand` 那两道）");
            // 🔴 2026-10-19：`PickUp` 会把 `_travel` 清零（PC 档在按下那一帧清零是对的）——
            //   而候选档**按下到起拖之间那一段位移也是「这一拖走了多远」**，丢了它会让
            //   `Release` 把「刚过阈值就松手」误判成轻点（`_travel < TapThreshold` ⇒ 弹展示窗）。
            //   ⇒ 把累计值原样还回去（之后 `UpdateDrag` 只会把它顶得更大）。
            float travelledBeforePickUp = _travel;
            PickUp(card, world, false);
            if (travelledBeforePickUp > _travel) _travel = travelledBeforePickUp;
        }

        /// <summary>候选态松手（**卡从没离开手牌**）：几乎没动 ⇒ 轻点（原版 `OnPointerClick` →
        /// `ToggleOpenCardDisplayOnTouch`）；动过但没过阈值 / 被方向角拦下 ⇒ 什么也不做。
        /// <para>⚠️ 「轻点」沿用本类既有的 <see cref="TapThreshold"/>（世界单位）—— 原版这里其实由
        /// EventSystem 的 drag 阈值决定（`allowClick` 在 `OnPointerExit` 清 0 那条另记在 `C3` #8），
        /// 两套口径今天只在这一处收敛；⛔ 别在别处再写第三个距离阈值。</para></summary>
        void EndPressCandidate(Vector3 world)
        {
            var card = _pressPendingCard;
            _pressPending = false;
            _pressPendingCard = null;
            float d = Vector3.Distance(world, _pressWorld);
            if (d > _travel) _travel = d;
            // 🆕 2026-10-19（T16）：原版 `OnPointerClick` 只在 `allowClick` 仍为 1 时转发
            //   `OnTouchUpAsButton` ⇒ 「按在卡缘、指针挪出卡外（哪怕只挪 0.1）再松手」**不产生点击**。
            //   ⚠️ 与 `_travel < TapThreshold` 是**两条独立判据、都要满足**：位移近 ≠ 还在卡上。
            // 🔴🆕 2026-10-25（`K2`）：第三条 —— **长按不开窗**（`PressShortEnough`，判据见
            //   `LongPressNoDisplaySeconds`）。原版 `OnTouchUpAsButton` 那一支先量按下→松手的时长。
            // 🔴 2026-10-26（`G21` · 账 `B9`/`B10`）：**先收抬牌、再转发轻点**。
            //   原来这里是「先 `OnTapped`、后 `SetHovered(-1)`」，而
            //   `BattleDriver.OnCardTapped` 的第一道闸是 `CardDisplaySuppressedFor(card)`
            //   （= `card == HoveredCard && !DragOnlyBlocked()`，本文件 `:560-563`）——
            //   候选态（触摸那条正路）松手时 `_hovered` **必然还等于这张卡**
            //   ⇒ 每次轻点都被自己吞掉、卡面展示窗**永远不开**。
            //   语义也更贴原版：`OnPointerClick` 到达时 `StopShowingCardInHand` 已经开始收牌。
            //   ⛔ 别把 `CardDisplaySuppressedFor` 删掉 —— 它对应原版
            //   `ToggleOpenCardDisplayOnTouch(false)`，是真闸（抬牌期间点这张卡不开展示窗）。
            //   下面紧跟 `_pressInside = false;` 那个 `SetHovered(-1)` 保留（幂等，`OnTapped` 之后的路径不变）。
            SetHovered(-1);
            if (card != null && _pressInside && _travel < TapThreshold && PressShortEnough
                && OnTapped != null) OnTapped(card);
            else if (card != null && _pressInside && _travel < TapThreshold && !PressShortEnough)
                Debug.Log($"[CardPresentation] 长按 {PressElapsedSeconds:F2}s ≥ {LongPressNoDisplaySeconds}s ⇒ "
                        + $"**不开卡面展示窗**（原版 `CardScript__OnTouchUpAsButton.c:37-44`）—— 卡「{card.name}」");
            _pressInside = false;
            SetHovered(-1);
            // 🆕 2026-10-24（`J3`）：轻点结束 ⇒ 原版 `CardScript.StopShowingCardInHand(restart: true)` ⇒
            //   `PlayerHand.MoveCardsInHandToPosition(-1, null)`（**传 -1**）⇒ 整把重排 + 开闸。
            Relayout(true);
        }

        void PickUp(CardView card, Vector3 world, bool freshPress = true)
        {
            // 🔴 2026-10-22（`W63`）：起拖也在那道闸内 —— 原版唯一能到 `StartMovingFromHand` 的路是
            //   `CardCollider.OnDrag → CardScript.OnTouchDrag`，而后者第一句就早退（判据见
            //   `IsRightButtonOrTwoFingerTouch` 上面那整段）。
            //   ⚠️ `OnTouchDown`（按下）**没有**闸，但它对手牌（cardState 8）那一支直接 `return`
            //   ⇒ 「右键按着时按下没有反应」两版落到同一个结果。
            if (IsRightButtonOrTwoFingerTouch()) return;
            // 🔴 2026-10-25（`AUD8` #19 · `K2` 扩档）：**「只禁拖」那一档**（回放 0xA0 / 选目标 / 打法选择器，
            //   原版 `CardScript__OnTouchDrag.c:17-23`）。
            //   这里是**兜底**那一道（`SimulatePress` / 自检也走这里）——
            //   生产那条路已经在 `TickInput` 改走候选态、并在 `TickPressCandidate` 里拒掉了。
            if (DragBlocked()) return;
            // 🆕 2026-10-24（`J3`）：重排动画闸 —— 原版这 0.2 s 里手牌碰撞体全关 ⇒ 按了也抓不起来。
            //   （`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c` 状态 0 的 `Toggle(false)`；
            //     判据/取舍见 `_animGate` 那段。⛔ 与 `FrozenNow` 是**两件事**，别合并。）
            if (IsDoingAnim) return;
            _dragging = card;
            _grabOffset = card.transform.position - world;
            _pressWorld = world;
            _travel = 0f;
            // 🆕 2026-10-25（`K2`）：`freshPress`（PC 档按下即抓）时这一帧就是「按下」⇒ 记时刻；
            //   从候选态转过来时**不要重设** —— 原版 `pressTime` 也只在 `OnTouchDown` 写一次
            //   （`CardScript__OnTouchDown.c:26-27`），起拖不重写。
            if (freshPress) _pressTime = Time.timeSinceLevelLoad;
            // 🆕 2026-10-19（T16）：PC 档「按下即抓」= 原版 `OnPointerDown` ⇒ `allowClick = 1`。
            //   从候选态转过来时（`freshPress = false`）**不许重设** —— 候选期已经锁存过
            //   `OnPointerExit` 的清零（原版只有新的 `OnPointerDown` 才会把它置回 1）。
            //   🔴 `T09` 步骤 B · B2a：判据 = 真 collider（同上，两处同一个口）。
            if (freshPress) _pressInside = card.HitByCollider(world);

            // 空位先落在它自己原来的位置 —— 之后跟手往哪拖，空位就跟到哪
            _insertIndex = Mathf.Clamp(_cards.IndexOf(card), 0, Mathf.Max(0, _cards.Count - 1));
            SetHovered(-1);

            card.SetHighlight(CardHighlightState.Selected);
            // 🔴 2026-10-24（`A26`）：ease 这一档的口径 —— **单补间传 `OutQuad`，Sequence 才传 `Linear`**。
            //   判据 = 原版 `CardScript.StartMovingFromHand`
            //   （`d:/2/tools/decomp_full/CardScript__StartMovingFromHand.c`，亲读）：拿起手牌只开**一条**
            //   `DOScale(transform, GetCardInHandScale() * globalVars+0x44, globalVars+0x78, 0)`
            //   —— `+0x44` = `cardInHandMovingScale` 1.1（= 本类的 `dragScale`）、
            //   `+0x78` = `timeToScaleIntoMoving` 0.1（= `CardTween.PickUpDuration`）；
            //   **没写 `SetEase`** ⇒ 吃 `DOTween.defaultEaseType`；原版 `resources.assets`
            //   pathID 1430 的 `DOTweenSettings` 实测 `defaultEaseType = 6`（`OutQuad`）。
            //   🔴 这里现在走 `CardView.TweenPose` = **单条补间** ⇒ 传 `OutQuad` 就是那条 `DOScale` 的形状；
            //   ⛔ 别照抄 `CardTween.ToPose` 的「Sequence 只能传 `Linear`」约定（那会变成匀速）。
            //
            // 🔴 **2026-10-24（`J3` · 口径差 1）：旋转那一跳改成 `DOLocalRotate`，角度也改成【局部】角。**
            //   判据同 `HandLayout.Place` 那一段（`CardScript__UpdateMinionInHandPosition.c` 用的是
            //   `DOLocalRotate`）。原来这里传的是 `card.transform.eulerAngles.z`（**世界**角）配
            //   `DORotate`（世界）—— 父链一有旋转，卡会被**反着拧**到世界 `(0,0,θ)`
            //   （= 局部角 `θ − 父件角`）；改成 `localEulerAngles.z` + `DOLocalRotate` 之后
            //   「保持它现在的倾角」才真的是保持（原版拿起时**根本没有角度补间**，
            //   我们这一跳的语义就是「保持在当前角」，所以目标值必须取局部角）。
            //
            // 🔴 **2026-10-24（`A3` · 新账-82②）：改走 `CardView.TweenPose`，⛔ 不再直写 `localScale`。**
            //   原来这里经 `J3` 那份 `HandPoseTween` → `transform.DOScale(...)`，**直接写 `localScale`**
            //   ⇒ `CardView._baseScale` 停在**上一次重排时的旧值**（今天零可见差，但这是全仓
            //   「直写 localScale」的第 4 个口；一旦紧接着有 `SetPose` / `SetHighlightScale`，
            //   卡会**静默弹回旧缩放** —— `CardView` 的 `T04` 那条 doc 记的就是这一族）。
            //   `TweenPose` 每帧经 `SetPose` 写：**局部位置 + 局部角度 + `_baseScale`** 一起带。
            //   ⚠️ **ease 这一档与 `HandLayout.Place` 相反**：`TweenPose` 是**单条补间**（不是 Sequence）
            //   ⇒ 传 `OutQuad` 才对（= 原版那条 `DOScale` 吃默认 `defaultEaseType`）；
            //   照抄 `CardTween.ToPose` 那条「Sequence 只能传 Linear」的约定会变成**匀速**（错的那一档）。
            //   ⚠️ 缩放目标照旧是「手牌档 × `dragScale`」—— `CardView` 的 `_hlMul`（高亮系数）
            //   会在它之上再乘（`Selected` 那一档原版**不缩放**，`_hlMul` 会回 1，见 `CardHighlight.ScaleOf`）。
            {
                var tr = card.transform;
                Vector3 worldTarget = tr.position + new Vector3(0f, 0f, -dragLiftZ);
                Vector3 localTarget = tr.parent != null ? tr.parent.InverseTransformPoint(worldTarget) : worldTarget;
                card.TweenPose(localTarget, tr.localEulerAngles.z,
                               hand.cardScale * LayoutSpace.Scale * dragScale,
                               CardTween.PickUpDuration, Ease.OutQuad);
            }

            // 🔴 2026-10-26（B9/E6P14 · 施工单 1157）：**起拖首帧 = 无洞 N-1 张**，⛔ 不是「立刻带洞」。
            //   判据：原版 `PlayerHand.StartMovingHandCard(card)` 起的是 `DisplayHandMinusCard(currentHand, card)`
            //   协程（起协程那一跳 = ILSpy `PlayerHand.cs:3306-3322`：`obj+40 = currentHand` / `obj+48 = card`）——
            //   排除 `cardToExclude`、跳过 `cardState == waitingToBePlayed(4)`、序号**连续递增**、
            //   `total = list.Count − 1`（`decomp_full/PlayerHand._DisplayHandMinusCard_d__80__MoveNext.c:32-90`，
            //   跳过那两句在 `:70-75`，Ghidra 吞了 `op_Inequality` 第二实参 ⇒ 用 ILSpy 同族读法补全）
            //   ⇒ 其余手牌**当场闭合**。带洞那一档是**下一帧起**的 `DisplayHandWithCardSpace`
            //   （`PlayerHand__DisplayHandWithCardSpace.c:25-84`：逐帧按指针取最近槽、把中间那格让出来）。
            //   ⚠️ `_insertIndex` 置 -1 只是让**下一帧** `UpdateDrag` 的 `slot != _insertIndex` 恒真
            //     （= 原版那一趟 `DisplayHandWithCardSpace` 的第一帧），⛔ 不是「拖拽中没有空位」的最终态。
            //   ⚠️ 原版这条协程**也**置 `+0x61`(isDoingAnim)=1、等 0.2 s 后清 0，但**不关碰撞体**
            //     （`Toggle(false)` 只在 `MoveCardsInHandToPosition` 里）⇒ 起拖这 0.2 s 原版照旧能交互；
            //     我们的 `_animGate` 是「输入闸」（等价的是**关碰撞体**那条）⇒ ⛔ 别在这里 ArmAnimGate。
            {
                _insertIndex = -1;
                Relayout(false);
                // 无洞那一趟不经过 `HandLayout` 的「补顶号」块（那块 `if (hole)` 只在带洞时跑）
                // ⇒ 自己把被拖那张顶上去（= 原版起拖时的 `SetSortingLayer(<HandMoving>)`，见 `CardView.QHandMoving`）。
                card.SetQueue(hand.queueTop);
            }
            // 🔴 **2026-10-01：拿起牌时不再把九格全点亮。** 原来这里调 `board.SetDragHighlight(true)`
            //    （九格一起染绿 = 「哪些格能放」），与原版不符：原版只有**一格**落点指示
            //    （`SetCardShadow(GetLeft/RightSlotPos(落点))`，判据 → `资料/待办判据_战场与战斗视图.md` §8b）。
            //    现在改成 `UpdateDrag` 里每帧按**解出来的落点**点一格 —— 拿起、还没移到棋盘上时**不亮**。
            DropAccepted = false;     // 🔴 2026-10-30（`B99_B1`）：新一手起拖 ⇒ 清掉上一手的结论（同 `AbortDrag`）
            ClearDropIndicator();
        }

        void UpdateDrag(Vector3 world, float dt)
        {
            // 🔴 2026-10-22（`W63`）：跟手也在那道闸内 —— 原版 `OnTouchDrag` 第一句早退
            //   ⇒ 右键按着拖战场时，手里这张牌**停在原地**（连 `_travel` 都不再累计）。
            //   ⚠️ **松手仍然照常结算**：原版 `OnTouchUpAsButton` / `OnTouchExit` 都没有这道闸
            //   ⇒ `Release` 那一侧不加闸（加了会让牌卡在手里，那是原版没有的行为）。
            if (IsRightButtonOrTwoFingerTouch()) return;
            // 拖拽跟手：直接跟，但带一点平滑（完全硬跟会显得很生硬）。
            // ⚠️ 平滑系数要**按 dt 算**，不能用固定的 0.45 —— 那样帧率一变手感就变，
            //    而且批处理自检里手动喂 dt 时推不到位（踩过）。
            var t = _dragging.transform;
            var target = world + _grabOffset + new Vector3(0f, 0f, -dragLiftZ);
            var prevPos = t.position;
            t.position = Vector3.Lerp(prevPos, target, 1f - Mathf.Exp(-20f * dt));
            // 账 767/768（第二批）：**拖卡倾斜**（原版 `CardScript__SetCardMovement.c:69-96`，每帧写）。
            //   原版：`rotationTarget.x = (max.x − min.x) × clamp01(Δy / movingRotationReduction + 0.5) + min.x`；
            //         `rotationTarget.y` 用 **|Δx|**（原文对 Δx 做了清符号位）；
            //         `transform.rotation = Quaternion.Euler((rotationTarget + forcedExtraRotation) × Deg2Rad)`；
            //         另有「距离过小 / 过大 ⇒ position 直接吸附、rotationTarget 归零」那一支（我们不抄 ——
            //         我们的跟手是平滑，没有那条硬吸附，见下面 🔶）。
            //   数值判据 = `CardScript` **构造函数默认值**（`decomp_full/CardScript__.ctor.c:41-45`：
            //     0x41A00000 = 20 · 0xC1A00000 = −20 · 0x41200000 = 10）—— 这三个字段
            //     （`maxCardRotation` / `minCardRotation` / `movingRotationReduction`）在
            //     `CardScript.cs:1437-1443` 里**没有 `[SerializeField]`** ⇒ 预制体不会覆盖，ctor 值就是真值。
            //   🔶 自己设计（非原版）：原版写**世界**四元数（3D 卡的 X/Y 俯仰）；我们是 2D 卡 ⇒ 只写**局部 X/Y**、
            //      **保留原 Z 角**（否则手牌扇形那点倾角会被抹平），`forcedExtraRotation` 取零（手牌路径的
            //      原版实参我们没有对应物）。
            //   ⚠️ 收手时 `CardView.SetPose` 会把 `localRotation` 整体写成 `Euler(0,0,rotZ)` ⇒ 倾斜**自动复位**，
            //      所以这里不需要另写「松手复原」（那是 SetPose 的既有语义，⛔ 别在这儿再抄一份）。
            {
                float dy = target.y - prevPos.y;
                float dx = Mathf.Abs(target.x - prevPos.x);
                float rx = (DragTiltMaxDeg - DragTiltMinDeg) * Mathf.Clamp01(dy / DragTiltReduction + 0.5f) + DragTiltMinDeg;
                float ry = (DragTiltMaxDeg - DragTiltMinDeg) * Mathf.Clamp01(dx / DragTiltReduction + 0.5f) + DragTiltMinDeg;
                t.localRotation = Quaternion.Euler(rx, ry, t.localEulerAngles.z);
            }

            // 记下这一拖最远走到哪 —— 松手时用它区分「轻点」和「真拖」（只有前者开关展示窗）
            float d = Vector3.Distance(world, _pressWorld);
            if (d > _travel) _travel = d;

            // ---- 手牌插槽：边拖边让位（原版 GetClosestInHandSlot）----
            // 只有插槽号变了才重排 —— 每帧重排会把补间反复打断。
            // ⚠️ 只在**手牌这一带**（棋盘线以下）才跟着走：拖到战场上以后还按 x 重排的话，
            //    你在上面选位置、下面的手牌会自己滑来滑去，看着像 bug
            //
            // 🔴 **2026-10-18（A896 · 调度台裁定一）：这道 y 守卫的量取【指针】，不取被拖那张卡。**
            //   判据（`d:/2/tools/decomp_full/BattleManager__Update.c`，亲读）：它是**两道量**在比：
            //     · `:151-157`（switch 之前）先取 `playerHandPosLimitObj`（`BattleManager +0x180`）的
            //       `transform.position` 存进 `local_5a8` ⇒ 后面各支里的 `local_5a8._4_4_` = **那条线的 y**。
            //       那条线是场景节点 `HandLimitArea`：`解包整理/07_场景/battlearena1/MonoBehaviour/
            //       MonoBehaviour_4371.json:184-187` 的 `m_PathID 141` → `GameObject/HandLimitArea_141.json`；
            //       13 张战场的全树里都是 `HandLimitArea [-0,779 1x1]`。
            //     · `:126-127` 每帧把 `GetMousePosInWorldSpaceCanvas()` 写进 `+0x434`（**y 在 `+0x438`**）。
            //     · `:195` / `:249` / `:291`（另外 `:539` / `:800` 是同一条写法）判别式一律是
            //       **`线.y < mouseCanvasPos.y`**；命中后再拿 `pointerCursorStart`（`+0x324`，读在
            //       `:196` / `:250` / `:292`）叠一个 `globalVars` 偏移去细化 ——
            //       **全篇一次都没读过「被拖那张卡」的 transform**。
            //   ⇒ 我们跟着取**指针**（本方法的形参 `world`），⛔ 不再取 `t.position`。
            //
            // ⚠️ **如实标：我们这道守卫【本身】在原版没有对应物。**
            //   原版那条 y 判据**守的不是「手牌让位」** —— 让位（`DisplayHandWithCardSpace`）在
            //   `case 2` 尾部（`:341-347`）是**无条件**跑的；原版那条守的是「**要不要试着把手牌打到场上**」。
            //   我们这道是自己加的，为的是「拖到战场上以后还按 x 重排 ⇒ 手牌会自己滑来滑去」。
            //   本件只做一件事：**把量取成原版那一处比较取的那个量**（指针）——
            //   ⛔ 不许删（删了改行为，且没有判据说原版没有这道守卫）· ⛔ 不许原样留着读卡的位置。
            //   🔴 **2026-10-15 就地订正（铁律 5）**：下面这条「**没验过**」**已经验清了**（`A2104①`）——
            //      原版 `HandLimitArea` 实况 dump = `pos(0,300)` · anchor=pivot=(0.5,**0**) · 根 Canvas 1080
            //      ⇒ 它 = **底边上方 300 设计 px = 距顶 780 px**；而旧记的「779」是**距顶**读法
            //      ⇒ **两处读数本来就是同一根线**。
            //      ⚠️ **2026-10-16（`T02` / `A2104①`）已按明令拆开** —— 这条比较**不再借 `board.lineY`**，
            //      改用 `BoardLayout.HandLimitY`（= 原版 `HandLimitArea` 的 `300/1080`）：
            //      两个用途以前**一个字段担着**（棋盘行心 vs 手牌限位线），而它们是**两根不同的线**
            //      （棋盘行心原版实测 636 px、手牌这条 780 px 距顶 ⇒ 差 144 px）。
            //      ⛔ 别改回 `board.lineY`：那会让「指针在不在手牌带」跟着棋盘行的校准一起漂。
            if (LayoutSpace.ToNormalized(world).y < BoardLayout.HandLimitY)
            {
                var visible = VisibleCards();
                // 🔴 **2026-10-17（B12）：喂进去的是【指针位置】，不是被拖那张卡的位置。**
                //   判据（`d:/2/tools/decomp_full/BattleManager__Update.c`）：
                //     `:123-127` 每帧把 `GetMousePosInWorldSpaceCanvas()`（= `Input.mousePosition`
                //       经 `Camera.ScreenToWorldPoint(…, canvas.planeDistance)`，就地读 `BattleManager__GetMousePosInWorldSpaceCanvas.c`）
                //       写进字段 `+0x434`/`+0x43c`；
                //     `:214` 把**那个字段**读回来 → `:220` 直接喂给 `GetClosestInHandSlot` ——
                //       全程没有经过任何「卡跟到哪」的量。
                //   ⚠️ 卡的位置是**带平滑**的（上面那句 `Lerp(…, 1f − Mathf.Exp(−20f·dt))`）⇒ 滞后于指针
                //      （手会「让位慢半拍」）。坐标系两边是同一个：`world` 走 `PointerWorldSafe()` →
                //      `LayoutSpace.ScreenToWorld`，而 `GetClosestInHandSlot` 的 `slotX` 也是
                //      `LayoutSpace.VisibleWidth` 量的**世界 x**（见 `Hand/HandLayout.cs` 那个方法）。
                int slot = hand.GetClosestInHandSlot(world, visible.Count);
                if (slot != _insertIndex)
                {
                    _insertIndex = slot;
                    Relayout(false);      // 边拖边让位 = 原版 `DisplayHandWithCardSpace`（不开闸，见 `_animGate` 那段）
                }
            }

            // 落点是否合法 → 实时反映在卡的着色上
            // 🔴 **2026-10-15（`A2107`）：判定量 = 【指针】，⛔ 不是被拖那张卡。**
            //   原版：`MinionManager.GetClosestAvailableSlot(MinionManager*, TouchInputManager.TouchPosition)`
            //     （`BattleManager__Update.c:428/588` ⇒ 第二实参就是 `FUN_180789a30` = `TouchInputManager.get_TouchPosition`，
            //      B3 §一 用机器码证实过；`GetClosestAvailableSlot` 把每个槽 `WorldToScreenPoint` 后比**屏幕距离**）。
            //   我们原来传 `t.position`（卡中心），而卡是**带平滑跟手**的（下面 `UpdateDrag` 那句 `Lerp`）
            //   ⇒ 卡恒**滞后**于指针 ⇒ 指针已经停在格上、卡还在半路，判的是**旧落点**。
            //   ⇒ 现在与「推上线」那道守卫（同文件、2026-10-18 已改）**同一口径 = 指针**（`world` = `PointerWorldSafe()`）。
            //   ⚠️ 量纲不用换：`BoardLayout.TryResolveSlot` 收的就是**世界坐标**，3D 那条路它自己换屏幕点
            //      （`BoardLayout.cs:450-455`）—— 产品里另一条同样取指针的路径也这么用（`BattleDriver.cs:3104`）。
            int boardSlot;
            BoardLayout which;
            bool ok = ResolveDrop(world, _dragging, out boardSlot, out which);
            _dragging.SetHighlight(ok ? CardHighlightState.ValidTarget : CardHighlightState.Selected);

            // 🆕 2026-10-01（§〇 第 14 条 (b)）：**落点指示** —— 只点亮**它会去的那一格**。
            //   判据 = 原版 `MinionManager.ReassembleMinionsWhilePlayingUnit` 里那句
            //   `SetCardShadow(GetLeft/RightSlotPos(落点), active:1, rotate:1)`（每帧调，见
            //   `BattleManager__Update.c:575`）⇒ 那枚影子落在**它真正会去的那一格**。
            //   ⚠️ 原版那条链只对**单位牌**跑（名字里的 `WhilePlayingUnit`）；战术卡不落格位 ⇒ 不指。
            //   ⚠️ 「落点」直接复用上面那一份 `ResolveDrop` —— 指的那一格和松手会落的那一格
            //      必须是**同一个判据**，别在这里另算一份。
            //   🔴 摆位用**这张卡自己的 `transform.position` 与它的层** —— 见
            //      `BoardLayout.SetDropSlot` 那条长注释（试过四套换算，全错）。
            var litB = (ok && !IsTactic(_dragging)) ? which : null;
            ShowDropIndicator(litB, litB != null ? LandingSlot(boardSlot) : -1, _dragging);
            // 🆕 2026-10-01：**让位预览** —— 和落点指示同一份判据（`litB` 为空就收工）。
            //   原版那条链只对**单位牌**跑（名字里的 `WhilePlayingUnit`）。
            if (OnDropPreview != null) OnDropPreview(litB, litB != null ? boardSlot : -1);
        }

        /// <summary>请求的落点 → **真正会落在的那一格**（没接 `DropLandingSlot` 时就是它自己）。
        /// 🔴 判据只此一处 —— 落点指示与落位动画都读它。</summary>
        int LandingSlot(int requested)
        {
            if (DropLandingSlot == null) return requested;
            int land = DropLandingSlot(requested);
            return land >= 0 ? land : requested;
        }

        /// <summary>点亮/熄灭落点指示（`b == null` 或 `slot &lt; 0` = 全灭）。
        /// 🔴 **底片挂成这张卡的子物体**（`BoardLayout.AttachDropSlotTo`）—— 见那条长注释：
        /// 试过四套「屏幕 ↔ 世界」换算**全错**（卡在 3D 空间、底片在 HUD 平面），
        /// 只有「跟卡自己的子物体同一条路」是稳的（卡自己的 blob 阴影就是这么跟着走的）。
        /// 🔴 **「让位」那半（场上已有单位实时腾空档）还没做**，如实记着 → `项目任务.md` §〇 第 14 条 (a)。</summary>
        void ShowDropIndicator(BoardLayout b, int slot, CardView card)
        {
            if (b == null || slot < 0 || card == null)
            {
                if (_dropLitBoard != null) { _dropLitBoard.SetDropSlot(-1); _dropLitBoard = null; _dropLitSlot = -1; }
                return;
            }
            if (!ReferenceEquals(b, _dropLitBoard) && _dropLitBoard != null) _dropLitBoard.SetDropSlot(-1);
            b.AttachDropSlotTo(slot, card.transform);
            _dropLitBoard = b;
            _dropLitSlot = slot;
        }

        /// <summary>熄掉落点指示（松手 / 取消 / 重开一局都要调）。</summary>
        public void ClearDropIndicator()
        {
            if (_dropLitBoard != null) _dropLitBoard.SetDropSlot(-1);
            _dropLitBoard = null;
            _dropLitSlot = -1;
            // 🆕 2026-10-01：**让位预览也要一起收**（否则那些被推出去的单位会一直停在预览位上）
            if (OnDropPreview != null) OnDropPreview(null, -1);
        }

        /// <summary>这张手牌是战术卡吗（战术卡**不落格位**，它是打到某个单位上的）</summary>
        static bool IsTactic(CardView c)
        {
            return c != null && !c.Data.isUnit;
        }

        // ==================================================================
        //  🔴 2026-10-25（`AUD8` #4）：**战术卡的目标侧** —— 落点不许跨侧
        // ==================================================================
        //  病根（真机可复现、静默）：原来 `DropReject` 对战术卡是「**先试敌方半场、再落回我方半场**」，
        //   而引擎侧的目标侧**由卡面正文定、与指针压在哪块棋盘无关**：
        //     `Core/EffectResolver.cs:1864`：`spec.Side == "enemy" ? 1 - p : p`，`targetSlot` 只是**格号**。
        //   ⇒ 友方目标卡（`Give Shuriken 2 to a friendly unit` 一族）拖到**敌方**单位上，
        //     只要我方同号格有一个合法友军 ⇒ 落点判「合法」⇒ 效果加在**自己人**身上，
        //     指针下的敌人毫无反应；反向（敌方目标卡拖到自己人身上）同理。
        //  判据（**唯一一份**，⛔ 不另写一套）：`RuleEngine.EffectText.PickSide(Parse(desc))`
        //    —— 与 `Core/EffectResolver.cs:1843`（`CanPlayTactic` 的 `PickTarget(ops).Side`）同一个口。
        //    ⚠️ 卡面数据在 `CardData` 里**只有已本地化的显示字段、没有 desc**
        //      （`Core/CardView.cs:29` 的字段表）⇒ 按 `CardData.id`（= 引擎卡 id）回查 `CardDatabase`。
        //  ⚠️ `"any"` 按**引擎的**口径当 `"own"`：`CanPlayTactic` 那一行对 `side != "enemy"` 一律取 `p`，
        //    所以「任意侧」的卡引擎只会解我方棋盘 ⇒ 落点也必须在**我方**，否则又变成跨侧错打。

        static Dictionary<string, string> _sideByCardId;
        static readonly HashSet<string> _sideUnknown = new HashSet<string>();
        static List<RuleEngine.CardDef> _pool;
        static Dictionary<string, RuleEngine.CardDef> _poolById;

        /// <summary>卡表（**一次**载入并缓存）。⚠️ `CardDatabase.Load()` 每次返回一份 1127 元素浅拷贝，
        /// 拖拽时逐帧调会很贵。载不到（`Count == 0`）时**不缓存**，下一帧还能重试。</summary>
        static List<RuleEngine.CardDef> Pool()
        {
            if (_pool != null && _pool.Count > 0) return _pool;
            _pool = RuleEngine.CardDatabase.Load();
            if (_pool.Count == 0) { _pool = null; _poolById = null; return null; }
            _poolById = RuleEngine.CardDatabase.IdIndex(_pool);
            return _pool;
        }

        /// <summary>视图 → 引擎卡表项。
        /// 🔴 **别拿 `CardData.id` 当引擎 id**：它是**英文卡名**（`BattleDriver.ToCardData` 的 `id = c.Name`，
        /// 那一行的注释写着「它兼着显示名/配对的活」）。引擎 id 在 **`CardData.artId`**
        /// （= `BattleDriver.ArtKey(c)` = `FromOriginalPool ? c.Id : c.Name`）。
        /// ⇒ 两级：① 按 `artId` 直取（池内 1127 张全部命中）；② 退回按**英文卡名**（自造卡 / 夹具）。</summary>
        static RuleEngine.CardDef DefOf(CardView c)
        {
            if (c == null) return null;
            var pool = Pool();
            if (pool == null) return null;
            string id = c.Data.artId;
            if (_poolById != null && !string.IsNullOrEmpty(id))
            {
                RuleEngine.CardDef hit;
                if (_poolById.TryGetValue(id, out hit)) return hit;
            }
            var byName = RuleEngine.CardDatabase.Find(pool, c.Data.id, c.Data.faction);
            return byName ?? RuleEngine.CardDatabase.Find(pool, c.Data.id);
        }

        /// <summary>这张战术卡**要打哪一侧**：`"enemy"` / `"own"`（含 `"any"`，见上）/ `""` = 不需要目标。
        /// <para>⚠️ 判不出（卡表查不到 / 卡 id 空）⇒ 回 `""` = **旧口径**（两块棋盘都试）并出声一次。
        /// ⛔ 不猜一个侧出来 —— 猜错就是静默打错目标。</para></summary>
        static string TacticTargetSide(CardView c)
        {
            if (c == null || c.Data.isUnit) return "";
            string key = (c.Data.artId ?? "") + "\u0001" + (c.Data.id ?? "") + "\u0001" + (c.Data.faction ?? "");
            if (_sideByCardId == null) _sideByCardId = new Dictionary<string, string>(StringComparer.Ordinal);
            string cached;
            if (_sideByCardId.TryGetValue(key, out cached)) return cached;

            var def = DefOf(c);
            if (def == null)
            {
                if (_sideUnknown.Add(key))
                    Debug.LogWarning("[CardPresentation] 卡表里查不到「" + (c.Data.id ?? "?") + "」（artId "
                                   + (c.Data.artId ?? "?") + "）⇒ 目标侧判不出，落点退回旧口径"
                                   + "（两块棋盘都试）。⚠️ 这一档仍然可能跨侧错打。");
                return "";
            }
            string side = RuleEngine.EffectText.PickSide(
                RuleEngine.EffectText.Parse(def.Desc, out _, out _, card: def)) ?? "";
            _sideByCardId[key] = side;
            return side;
        }

        /// <summary>
        /// 指针现在指着的落点合不合法 —— **判据正本**，四道闸按顺序问，返回**被拒的原因**
        /// （`null` = 合法，`slot` / `which` 有效）。
        ///
        /// 🔴 **2026-09-17 抽出来的**：原来 `ResolveDrop` 只说「行 / 不行」，于是
        ///    `BattleAutoDrive.DragOnce` 为了能报出是哪一道挡的，**在那边手抄了一份三道闸**。
        ///    两份判据当场就分叉了 —— 那边报「全绿」、这边判「不合法」，因为这边还有
        ///    **第四道 `_placed`**。这正是本工程反复踩的那类坑（**同一件事两处判 ⇒ 迟早不一致**）。
        ///    现在探针与真值都读这一个函数。
        ///
        /// **战术卡和单位卡的落点规则不一样**（2026-09-12 接战术卡时分的）：
        ///   · 单位卡：落在**自己半场的空格**上，而且这格本回合还没摆过（`_placed`）；
        ///   · 战术卡：落在**一个单位**上（自己或对面的都行，由卡面文本决定），**不占格位** ——
        ///     所以既不该要求「这格空着」，也不该因为「这格本回合摆过牌」就被拒。
        /// 🆕 **2026-10-26（执行代理 `A2`）**：**`targeted == 1` 的非单位卡**还有一档例外 ——
        ///   落在目标侧的**空格**上也放行（驱动层 `CanDropAtSlot` 对 `ErrNoTargetAvailable` 放行）：
        ///   原版「这张牌进 play area」那一跳**不校验目标**（`BattleManager__Update.c:255-280`），
        ///   目标是在 `playingTargetedSpell(4)` 里才判的（`:676-684`）⇒ 放行之后由
        ///   `BattleDriver.TryBeginPlayTargetPick` 进「点选目标」态（点不出就回手 + 提示）。
        /// 合法性本身仍然只有**引擎一处**说了算（`CanDropAtSlot` → `RuleCore.CanPlayCard`）。
        /// </summary>
        /// <summary>落点被拒的**类别** —— 加它是因为「拖回手牌」与「拖到格位上但被引擎拒」在日志上
        /// 以前只有一行字符串之差，而**两者对玩家是两回事**：
        ///   · **正常取消**（松手时卡没落在任何格位上）—— 玩家就是把牌放回去，**不是非法操作**；
        ///   · **非法操作**（卡落在格位上、但这一手打不了）—— 原版会播 `cantdo`（"我不能这么做"）。
        /// 🔴 判据**只在这一处算** —— 调用方不许再比字符串（2026-09-17 那条「别另抄一套判据」同理）。</summary>
        public enum DropRejectKind
        {
            Ok = 0,           // 合法
            NoCard,           // 没有正在拖的卡（内部错）
            NoBoard,          // 没有棋盘（内部错）
            MissedSlot,       // **命中不了格位** —— 拖回手牌/空白处 = **正常取消**，不是非法操作
            EngineRefused,    // **引擎说这一格打不了** —— ✅ 非法操作（原版 `CanPlayCard` 那条）
            SlotUsed,         // **这一格本回合已经摆过牌了** —— ✅ 非法操作（我们自己的占用规则）
        }

        /// <summary>这个类别算不算「玩家做了非法操作」—— `cantdo` 只对这一类播。
        /// 🔴 **一处判据**：别在 `BattleDriver` 里再写一遍 `== EngineRefused || == SlotUsed`。</summary>
        public static bool IsIllegalAction(DropRejectKind k)
        {
            return k == DropRejectKind.EngineRefused || k == DropRejectKind.SlotUsed;
        }

        string DropReject(Vector3 pos, CardView card, out int slot, out BoardLayout which)
        {
            DropRejectKind k;
            return DropReject(pos, card, out slot, out which, out k);
        }

        /// <summary>🆕 **2026-10-25（`X19/P2`）**：最近一次 `DropReject` 判出来的**词条键覆盖**
        /// （null = 本层不知道原因，交给驱动层按引擎码取）。
        /// <para>只有一档需要它：**指针压在错的那一侧棋盘上** —— 原版那一支是 `IsValidSpellTarget`
        /// 失败 ⇒ `NotifyCantDoAction(Battle/Tips/DragToTarget)`（`BattleManager__Update.c:660-673`），
        /// 这一档**不是**引擎那四个码能解释的。⛔ 别在这里加别的键（引擎拒一律由驱动层取）。</para></summary>
        string _lastRejectTip;

        string DropReject(Vector3 pos, CardView card, out int slot, out BoardLayout which,
                          out DropRejectKind kind)
        {
            slot = -1;
            which = board;
            kind = DropRejectKind.Ok;
            _lastRejectTip = null;          // 每问一次先清（拒因覆盖只对最新这一问有效）
            if (card == null) { kind = DropRejectKind.NoCard; return "没有正在拖的卡"; }
            bool tactic = IsTactic(card);
            int s;

            // 🔴 2026-10-25（`AUD8` #4）：**战术卡只认它自己那一侧棋盘**（判据/病根见 `TacticTargetSide`）。
            if (tactic)
            {
                string want = TacticTargetSide(card);
                if (want.Length > 0)
                {
                    bool wantEnemy = want == "enemy";          // `"any"` 归我方（= 引擎 `CanPlayTactic` 的口径）
                    BoardLayout wantBoard = wantEnemy ? foeBoard : board;
                    BoardLayout otherBoard = wantEnemy ? board : foeBoard;
                    int sWant = -1;
                    bool hitWant = wantBoard != null && wantBoard.TryResolveSlot(pos, out sWant);
                    // ⚠️ 另一块棋盘只用来**区分「拖到了错的一侧」与「拖到空白处」**（前者原版会出声），
                    //    它的格号**不参与任何落点判定**。
                    bool hitOther = otherBoard != null && otherBoard.TryResolveSlot(pos, out _);

                    if (hitWant && CanDropAtSlot(sWant, card))
                    { slot = sWant; which = wantBoard; return null; }

                    // ⛔ **不再回落到另一块棋盘**——那正是「指针下的敌人没反应、效果加到自己人身上」的错源。
                    if (hitWant)
                    {
                        kind = DropRejectKind.EngineRefused;
                        return $"目标侧（{want}）这一格打不了（槽 {sWant}）";
                    }
                    if (hitOther)
                    {
                        // 原版这一档 = `IsValidSpellTarget` 失败 ⇒ `NotifyCantDoAction(DragToTarget)`
                        // （语音+文字，`dc/BattleManager__Update.c:660-673`）⇒ 算**非法操作**，不是正常取消。
                        kind = DropRejectKind.EngineRefused;
                        // 🆕 `X19/P2`：这一档**引擎码解释不了**（引擎根本没被问到）⇒ 由本层把词条交出去。
                        _lastRejectTip = BattleDriver.DragToTargetTerm;
                        return $"指针压在**不是目标侧**（卡面要 {want}）的棋盘上 —— ⛔ 不回落";
                    }
                    kind = DropRejectKind.MissedSlot;
                    return $"命中不了格位（卡在 {pos.x:F2},{pos.y:F2},{pos.z:F2}）";
                }
                // `want == ""` = 这张战术卡**不需要目标**（`Refill 2 Energy` 一族）⇒ 引擎不看格位，
                // 两块棋盘任一处都能放（保持原样，先试敌方——历史上就是这一条）。
                if (foeBoard != null && foeBoard.TryResolveSlot(pos, out s) && CanDropAtSlot(s, card))
                {
                    slot = s; which = foeBoard; return null;
                }
            }

            if (board == null) { kind = DropRejectKind.NoBoard; return "没有棋盘"; }
            if (!board.TryResolveSlot(pos, out s))
            {
                kind = DropRejectKind.MissedSlot;
                return $"命中不了格位（卡在 {pos.x:F2},{pos.y:F2},{pos.z:F2}）";
            }
            if (!CanDropAtSlot(s, card))
            {
                kind = DropRejectKind.EngineRefused;
                return $"引擎说这一格打不了（槽 {s}）";
            }
            // 🔴 **2026-10-01：`_placed`（「这一格本回合已经摆过牌了」）那道闸已删。**
            //    它**不是原版的规则**（原版棋盘是连续列表，落点是一个**插入位置**而不是一个格子，
            //    「这一格用过没有」无从谈起 —— 判据 → `RuleEngine/Core/BoardSlots.cs` 文件头），
            //    而在新模型下它还**挡掉合法的出牌**：第一张落在 5 号格之后，第二张再拖到 5 号格
            //    本来应该「插在它前面、把它推出去一格」。`_placed` 只留作**登记**（自检/探针要看
            //    「哪张卡落到了哪一格」，见 `CardBaseDemo`），不再参与判据。
            slot = s; which = board;
            return null;
        }

        /// <summary>落点合不合法（`DropReject` 的一层壳；只想知道行不行时用它）。</summary>
        bool ResolveDrop(Vector3 pos, CardView card, out int slot, out BoardLayout which)
        {
            return DropReject(pos, card, out slot, out which) == null;
        }

        /// <summary>
        /// 把这一拖**逐道闸**报成一行字（给自检 / 自动驱动用的探针）。
        /// ⚠️ 只读，不改任何状态 —— 松手前随时可以调。
        /// 🔴 **探针必须用这一份**，别在调用方另抄一套判据（2026-09-17 的坑，见 `DropReject`）。
        /// </summary>
        public string ExplainDrop(Vector3 pos, CardView card)
        {
            int s; BoardLayout w;
            string why = DropReject(pos, card, out s, out w);
            var n = LayoutSpace.ToNormalized(pos);
            string head = $"卡在 ({pos.x:F2},{pos.y:F2},{pos.z:F2}) 归一化 ({n.x:F3},{n.y:F3})"
                        + $" 棋盘线 lineY={(board != null ? board.lineY.ToString("F3") : "无")}"
                        + $" 手牌限位线 HandLimitY={BoardLayout.HandLimitY:F3}";
            if (why == null)
                return $"{head} ⇒ ✅ 合法：{(w == foeBoard ? "敌方半场" : "我方半场")} 槽 {s}";
            return $"{head} ⇒ ❌ {why}";
        }

        void Release(Vector3 world)
        {
            var card = _dragging;
            _dragging = null;
            // 🔴🆕 **2026-10-30（`B99_B1` · AUD8·让位预览）：先判「这一手会不会真打出去」，再收指示器。**
            //   原先顺序相反 ⇒ `ClearDropIndicator()` → `OnDropPreview(null, -1)` → `EndPreviewReturn()`
            //   读到的是**上一手**残留的 `DropAccepted`（之前有一手真打出去过 ⇒ 残留 `true` ⇒ 早退支
            //   只 `_previewMoved.Clear()` 而**不送回** ⇒ 被推开的单位永远停在预览的假格位上）。
            //   判据 = 原版三条（拖动中 `MinionManager.ReassembleMinionsWhilePlayingUnit` /
            //   取消 `MinionManager.ReassembleMinions` / 打出去 `InsertMinion + AdjustedSlot` 自己定死位置
            //   ⇒ accept 时**必须不送回**）。
            int slot;
            BoardLayout which;
            DropRejectKind kind;
            string reject = DropReject(world, card, out slot, out which, out kind);
            bool ok = reject == null;
            DropAccepted = ok && card != null && card.Data.isUnit;   // 「这一手真打出去」= 合法 ∧ 单位卡（⚠️ `CardView.Data` 是**结构体** ⇒ 没有 `!= null` 这一判）
            ClearDropIndicator();     // ⚠️ 必须排在上面那几行**之后**：它内部会拿 `DropAccepted` 决定送不送回
            DropAccepted = false;     // 用完即清 —— ⛔ 别让下一手继承这一手的结论（驱动层 `DoPlay` 末尾也清一次）
            // 🔴🆕 2026-10-25（`K2`）：第三条 = **长按不开窗**（原版 `OnTouchUpAsButton.c:37` 那个 0.2 s 门）。
            bool tapped = _travel < TapThreshold && _pressInside && PressShortEnough;
            bool suppressByLongPress = _travel < TapThreshold && _pressInside && !PressShortEnough;
            _pressInside = false;

            // 🔴 松手时**先记下被拒的原因** —— 回弹不该是静默的：
            //    日志里要能直接看出是哪一道闸挡的（2026-09-17 加）。
            var cp = card.transform.position;          // ⚠️ 只给日志用（看卡滞后了多少）
            // 🔴 **2026-10-15（`A2107`）：判的是【指针】，⛔ 不再是卡的位置。**
            //   上面 `cp` 那一行**只为日志留着** —— 判据见 `UpdateDrag` 里同一段（原版
            //   `GetClosestAvailableSlot(TouchInputManager.TouchPosition)`，机器码判据在 `B3` §一）。
            //   ⚠️ 这次改动的**来由就是那条日志**：2026-09-17 抓到「卡已经飞到 (-2.14,-3.35) 才判」，
            //      当时判成「指针读野了」并给指针加了防护（`PointerWorldSafe`）—— 那只治了野值，
            //      没治**结构性滞后**；两个量本来就不该混用。
            // 🔴 **2026-10-30（`B99_B1`）**：`DropReject` 那一问已提到方法开头（见那段注释）——这里只剩日志要的 `kind`。
            // 🔴🆕 2026-10-25（`K2`）**就地订正（铁律 5 · 真机日志抓到的一处假象）**：
            //   这一行原来写 `拖动中={_dragging != null}` —— 而**上一句刚把 `_dragging` 置 null**
            //   ⇒ 恒打 `拖动中=False`。2026-10-25 第七十七会话的真机验收据此记了一条
            //   「一次合成拖拽未进拖拽态，日志却出现 `落位：Hand_Light Cover → 战术卡打向 3`
            //   （疑似把场上按压错认成手牌）」。
            //   🔑 **真相**：拖拽态一直是好的 —— `Release` 只有两个调用点（`TickInput` / `SimulateRelease`），
            //     两者都带 `_dragging != null` 前置；能进到这里就**必然**在拖拽态。
            //     那一下吃的也**确实是手牌**（`card.name` = `Hand_…`，视图由 `SyncHand` 建在手牌层；
            //     棋盘单位在 `_myUnits`/`_foeUnits` 里，**从不进 `_cards`** ⇒ 手牌命中不可能吃到场上按压）。
            //   ⇒ 现在打**松手那一刻的真实状态**，并把「这一下是哪一层吃的」写出来。
            string line = $"[CardPresentation] Release：指针世界 ({world.x:F2},{world.y:F2})"
                        + $" · 卡「{card.name}」在 ({cp.x:F2},{cp.y:F2},{cp.z:F2})"
                        + $" · 抓取偏移 ({_grabOffset.x:F2},{_grabOffset.y:F2})"
                        + $" · 拖动中={(card != null ? "True" : "False")} · 来源=手牌层(Release)"
                        + $" · 落点判定={kind}";
            _lastReleaseLog = line;
            Debug.Log(line);

            if (ok)
            {
                _insertIndex = -1;
                bool tactic = IsTactic(card);
                // 🔴 2026-10-01：**真正落在哪一格** —— 连续棋盘模型下它与「拖到哪一格」可能不是同一格
                //    （插进中间 ⇒ 后面的单位整体外移一格）。落位动画要飞向**真落点**，
                //    否则卡会先飞到请求格、再滑一下（而引擎里其实没动过）。
                int land = tactic ? slot : LandingSlot(slot);
                // 🆕 2026-10-01：单位卡打下去 ⇒ 引擎马上会插它（让位预览那几张**别送回去**）。
                // 🔴 **2026-10-30（`B99_B1`）**：这一句原来写 `DropAccepted = !tactic;` —— 已删：
                //   那个值现在在方法开头、`ClearDropIndicator()` **之前**就摆好并被消费掉了（第④步之后的结论
                //   不会再有人读它，留着只会变成「下一手继承上一手」的残留，正是本条 bug 的病根）。
                // ⚠️ 战术卡**不占格位** —— 记进 `_placed` 的话，那个格位本回合就再也放不了牌了
                if (!tactic) _placed[land] = card;
                // 从手牌里拿掉：之后的手牌重排不该再算它
                _cards.Remove(card);
                card.SetHighlight(CardHighlightState.Normal);
                // 🔴 **2026-10-11（A337）补间的【终点】必须用 `which.DropTargetWorld(land)`，不是 `SlotPosition`。**
                //    3D 战场下 `SlotPosition` 是**2D 行线**上那个点（屏上 708 px），而落位交接时
                //    `SyncBoard` 把这张卡摆到 `ArenaSlots.RootPosition`（屏上 631~657 px，**逐格不同**）
                //    ⇒ 交接那一帧整张卡**跳一下**。`DropTargetWorld` 投的正是「落位后那个位姿在屏幕上的
                //    同一点」（`Board/BoardLayout.cs:407-417`：3D 卡心 → 战场相机投屏 → HUD 平面）。
                //    ✅ **非 3D 时它逐位等于 `SlotPosition`**（`BoardLayout.cs:416` 那句兜底）⇒ 2D 那条路
                //       一个字都不动（同一个纯函数、同一个实例、同一组实参）。
                //    **判据只此一处**（铁律 6）：拖拽落点 / 底片 / 自动对局已经在用它
                //    （`Editor/BattleScene.cs:2111`/`:4898` · `BattleAutoDrive.cs`）⇒ 这一改还顺带把
                //    「拖拽落点 == 补间终点」变成**结构上成立**。⛔ 别在这里再算一次投影。
                //    🧨 **改坏法**：换回 `which.SlotPosition(land)` ⇒ 3D 下终点与真落位差 **0.73 世界单位**
                //    （≈79 px）⇒ `Editor/BattleScene.cs` 那条「卡到格位那一刻**真的被检测到**」当场红。
                // 🆕 2026-10-16：补上原版「手牌→棋盘」那几段里的**前两段**
                //   （先飞到**槽位正上方** → 再**垂直落下**），落地的扬尘/震镜头在
                //   `BattleDriver` 那一侧（跟着 `Deploy` 引擎事件走）。
                //   逐值出处、时长与「为什么 `onDone` 现在挂在整条序列的末尾（0.65 s 处）」
                //   → `Board/DeploySequence.cs` 文件头。
                //   🆕 2026-10-19（`W57` · `T05(c)(d)`）：中间还多了 **0.1 s 的纯等待 + 0.4 s 的交接**
                //   （手牌卡面 → 场上 3D 体，之后在竞技场空间落下）—— 判据与改法见同一份文件头。
                // 🔴🆕 2026-10-19（战斗线 B2 · 偏差 1/2 的残余①）：**先报「起飞」，再起飞** ——
                //   驱动层用这一个回调把这段 0.65 s 飞行纳入 `WaitingForBlockingAction`
                //   （原版从「牌离开手牌」那一刻闸就是真的；我们这边引擎要等 `onDone` 才被调）。
                //   ⚠️ 必须排在 `DeploySequence.Play` **之前**：那条序列**创建即开跑**，晚一步就漏掉第一帧。
                if (OnDeployFlightStart != null)
                    OnDeployFlightStart(card, which, land, DeploySequence.DurationFor(card));
                // 🆕 2026-10-19（`W57` · `T05(c)(d)`）：**0.4 s 那一次的交接** —— 由驱动层给出
                //   「手牌卡面 → 场上 3D 体」那一跳（判据见 `DeployHandoff` 的 doc）。
                //   没接（表现层自检 / `CardBaseDemo`）⇒ `default` = 退回旧的三段（HUD 平面直落）。
                var handoff = DeployHandoff != null
                    ? DeployHandoff(card, which, land)
                    : default(DeploySequence.Handoff);
                var tw = DeploySequence.Play(card,
                                             which.DropTargetWorldAtHeight(land, which.MinionConversionHeight),
                                             which.DropTargetWorld(land),
                                             which.placedScale * LayoutSpace.Scale,
                                             handoff,
                                             () => { if (OnDeployed != null) OnDeployed(card, slot); });
                tw.SetUpdate(CardTween.Mode);
                // 🔴 **出牌那个特效不在这里播**（2026-09-15 修）—— 驱动层收引擎的 `EvtKind.Play`
                //    事件时已经在**格位**上播了同一个 `VfxMap.PlayCard`，这里再播一次等于**打一张牌
                //    播两遍**（单位卡再叠一个 `Deploy`，一共三个）。
                //    留驱动层那一个，理由和别处一样：**事件只有引擎那一条**，表现层自己猜时机迟早会分叉。
                Debug.Log($"[CardPresentation] 落位：{card.name} → "
                        + (tactic ? "战术卡打向" : "槽")
                        + $" {slot}{(which == foeBoard ? "（敌方半场）" : "")}"
                        + (which.IsWarlord(slot) ? "（督军位）" : ""));
            }
            else
            {
                // 拖到哪就插到哪 —— 松手时把手牌顺序真的改成拖拽时让出来的那个顺序
                // 🔴 2026-10-26（B9/E6P14 · 施工单 1170）：**逐句对齐原版 `PlayerHand.HandCardMovedToNewIndex`**
                //   （`decomp_full/PlayerHand__HandCardMovedToNewIndex.c:16-38`；ILSpy 同族 `PlayerHand.cs:3565-3612`）：
                //     ① `idx = IndexOf(card)`（.c:16-18）；② `newIndex == idx` ⇒ **不动列表**、只重排、返回 0（.c:19-24）；
                //     ③ `Count <= newIndex` ⇒ 同样只重排、返回 0（.c:27）；④ 否则 Remove + Insert + 重排 +
                //     **`CheckForTutorialPointer()`**、返回 1（.c:28-38）。
                //   🔴 **`SendCardReorder` 不在这个方法里** —— 原版在 `BattleManager__Update.c:223-240`：
                //     只有 `HandCardMovedToNewIndex` **返回 1**（= 列表真的动了）且 `IsNetworkedGame()` 且
                //     `PhotonNetwork.connected` 才发，新下标取 **`IndexOf(card)`（移动之后）**。
                //     我们原来无条件发 ⇒ **同位置换位也发一条**（对面白收，且和原版返回 0 的语义相反）。
                if (_insertIndex >= 0)
                {
                    int from = _cards.IndexOf(card);
                    int to = _insertIndex;                      // 空位号 = 原版那个 `newIndex`
                    if (from >= 0 && to != from && to < _cards.Count)
                    {
                        _cards.RemoveAt(from);
                        _cards.Insert(to, card);
                        // = 原版 `BattleManager__Update.c:233-240` 那一跳（只在**真的动了**才发）
                        NotifyHandReordered(card);
                        // = 原版 `HandCardMovedToNewIndex.c:34-38` 末尾那句 `manager.CheckForTutorialPointer()`
                        //   （换位后**重算教程指针**：同一条动作重新解析锚点 → 箭头跟到新位置）。
                        //   ⚠️ 那一位在 `BattleDriver`（别人的文件）⇒ 本层只发事件，订阅方负责重算。
                        if (OnHandCardMovedToNewIndex != null) OnHandCardMovedToNewIndex(card);
                    }
                    // `from < 0` / `to == from` / `to >= Count` ⇒ 原版**不改列表、不发消息**（返回 0）；
                    // 这时唯一的动作就是下面那句 `Relayout(true)` —— 正是原版 `MoveCardsInHandToPosition(-1, …)` 那一趟。
                    _insertIndex = -1;
                }

                // ---- 回弹：**交给手牌重排**（原版就是这么做的；⛔ 原版没有「回弹专用补间」）----
                // 判据（2026-10-24 · `J3` 亲读；`BattleManager__Update.c` case 2 的松手那一支 + 两条协程）：
                //   · `BattleManager__Update.c:210` 松手（`FUN_180617800()` 为真的那一支）⇒
                //     `CardScript.MoveBackToHand(card)`：只做「sortingLayer 回手牌段 + `StopAnimsInPlay`
                //     + 单位卡 `DissolveMinionCard` + `SetStateInHand()`（卡状态 = 1、
                //     `SetParent(handAnchorLayout, worldPositionStays: true)`）」——**全文一个 `DOMove` 都没有**
                //     （`CardScript__MoveBackToHand.c` / `PlayerHand__SetParentOfCard.c`）；
                //   · 同支 `:220-223`：`GetClosestInHandSlot(指针)` → `PlayerHand.HandCardMovedToNewIndex(card, slot)`
                //     —— 先 `List.Remove` + `Insert` 改列表序，再 `StartCoroutine(MoveCardsInHandToPosition(-1, null))`；
                //   · `:246` 再 `PlayerHand.HandCardReleased()` ⇒ **又**起一条 `MoveCardsInHandToPosition(-1, null)`；
                //   · 两条协程最后都落到 `CardScript.UpdateMinionInHandPosition` —— 对**手牌里每一张**
                //     （含这张刚回来的）开三条单补间：`DOMove` / `DOLocalRotate` / `DOScale`，
                //     时长 = `globalVars + 0xA4` = `timeToPositionCard` = **0.2 s**，**全文不写 `SetEase`**
                //     （吃 `DOTween.defaultEaseType` = 6 `OutQuad`）。
                // ⇒ 原版的「回弹」**就是普通的手牌重排**：0.2 s、位置 + 角度 + 缩放、OutQuad。
                //   我们原来在这里自造了一条 `SnapBackDuration = 0.22` 的 `DOMove + DOScale`
                //   （Sequence 层再挂一次 OutQuad、**没有角度**），而这一帧末尾的 `Relayout()` 本来就会
                //   对同一张牌 `DOKill()` 再开一条正确的重排补间 ⇒ 那条自造补间**等于白开、还被白杀一次**。
                //   ⇒ 删掉它：回弹完全交给下面那句 `Relayout(true)`（时长 / 形状 / 角度 三样一次对齐）。
                //   ✅ **2026-10-24（`A3`）：`CardTween.SnapBackDuration`（0.22）已从 `Core/CardTween.cs` 删掉** ——
                //      判据就是上面这几条（原版没有回弹专用补间）；全仓已**没有**该常量的定义/调用
                //      （剩下的只是本段这条留痕注释）。
                card.SetHighlight(CardHighlightState.Normal);
                if (OnReturned != null) OnReturned(card, kind);
                // 🔴 **「非法操作」与「正常取消」在这里分开** ——
                //    拖回手牌/拖到空白处（`MissedSlot`）**不播** cantdo，只有落在格位上却被拒才播。
                // 🆕 2026-10-25（`X19/P2`）：把**本层判得出来的那条词条**一并交出去
                //   （`_lastRejectTip`；null = 引擎拒 ⇒ 由驱动层按引擎码取四档之一）。
                if (IsIllegalAction(kind) && OnIllegalAction != null) OnIllegalAction(card, _lastRejectTip);
                Debug.Log($"[CardPresentation] 落点不合法（{reject}）[{kind}]，{card.name} 回弹到手牌第 "
                        + $"{Mathf.Max(0, _cards.IndexOf(card))} 位（走**手牌重排**那条补间，见上面那段判据）");
            }

            // 轻点（按下→松开几乎没动）：**没落位**才算 —— 顺手拍了张牌上场不该弹展示窗。
            // 原版这个动作是 `BasicCardUI.ToggleOpenCardDisplayOnTouch`。
            // 🔴🆕 2026-10-25（`K2`）：**长按那一档不出声也不开窗**（原版那一支只 `StopShowingCardInHand`）。
            //   但不静默 —— 打一行日志（不然「按住不放再松手，窗口没开」看起来像没接上）。
            if (tapped && !ok && OnTapped != null) OnTapped(card);
            else if (suppressByLongPress && !ok)
                Debug.Log($"[CardPresentation] 长按 {PressElapsedSeconds:F2}s ≥ {LongPressNoDisplaySeconds}s ⇒ "
                        + $"**不开卡面展示窗**（原版 `CardScript__OnTouchUpAsButton.c:37-44`：`0.2 <= elapsed` "
                        + $"那一支只 `StopShowingCardInHand`，跳过 `DisplayCard`）—— 卡「{card.name}」");

            SetHovered(-1);
            // 🆕 2026-10-24（`J3`）：松手后这一趟重排 = 原版 `HandCardMovedToNewIndex` +
            //   `HandCardReleased` 那两条 `MoveCardsInHandToPosition(-1, null)`（**传 -1**）⇒ 带闸。
            //   回弹本身也由这一趟补间完成（见上面 else 支那段判据）。
            Relayout(true);
        }

        /// <summary>把手牌换位告诉对面（**原版 `PlayerHand.HandCardMovedToNewIndex` 里那条 `SendCardReorder`**）。
        /// <para>⚠️ 单机 / 不在对局中 ⇒ **不发**（原版也只在联网时发）；这条消息**不进权威动作流、不改指纹**
        /// （`Net/NetBattle.cs` 的 `CanSendPresentation` 段注释已写明），丢了只少一次对面手牌表现。</para>
        /// <para>⚠️ `newIndex` 取的是**我们视图列表 `_cards` 的下标**（= 原版 `PlayerHand` 那个列表的语义）；
        /// 本机收侧 `NetRemoteCardReorder` 目前只出声、不改视图（对面手牌是一排卡背、没有逐张顺序）——
        /// 这是账 12/27 里已如实记下的 P2 残余。</para></summary>
        void NotifyHandReordered(CardView card)
        {
            var d = Driver;
            var net = d != null ? d.Net : null;
            if (net == null || !net.InBattle || card == null || card.Inst == null) return;
            net.SendCardReorder(card.Inst.Id, _cards.IndexOf(card));
        }

        /// <summary>手牌里当前**看得见**的那几张（拖拽中的那张不参与重排）</summary>
        List<CardView> VisibleCards()
        {
            var list = new List<CardView>();
            foreach (var c in _cards) if (c != null && c != _dragging) list.Add(c);
            return list;
        }

        /// <summary>
        /// 手牌重排。拖拽中的那张不参与（它自己跟手），
        /// 但它会在 `_insertIndex` 处**留一个空位**，其余牌按多一张的间距让开。
        /// <para>🔴 **2026-10-24（`J3`）**：这个**公开口** = 原版 `PlayerHand.MoveCardsInHandToPosition(-1, …)`
        /// 那一档 ⇒ **带重排动画闸**（见 `_animGate` 那段判据）。连续路径（悬停 / 拖拽让位 / 拿起）走
        /// `Relayout(animGate: false)`，原因也写在那段。</para>
        /// </summary>
        public void Relayout() { Relayout(true); }

        /// <param name="animGate">这一趟要不要开「重排动画闸」。只在 `hand.animateRelayout` 为真时真开
        /// （没有动画就没有「重排期间」）。</param>
        void Relayout(bool animGate)
        {
            var list = VisibleCards();
            hand.Refresh(list, HoveredIn(list), _dragging != null ? _insertIndex : -1);
            if (animGate && hand != null && hand.animateRelayout) ArmAnimGate();
        }

        int HoveredIn(List<CardView> visible)
        {
            if (_hovered < 0 || _hovered >= _cards.Count) return -1;
            var h = _cards[_hovered];
            if (h == null) return -1;
            int i = visible.IndexOf(h);
            return i;
        }

        /// <summary>🔴 2026-10-19（`A1668`）：手牌 / 换牌区「点到哪一张」的**唯一判据** ——
        /// 命中集合里取 **z 最小（最前）** 的那张。
        /// <para>判据 = 原版 `BattleManager__GetTargetInPos.c:104-196`（2D 支）：`GetRayIntersectionNonAlloc` 之后**只取第 [0] 项**、**无排序** ⇒ 原版把「谁在最上面」交给物理层。
        /// 🔴 **2026-10-28（`B91` · W2 §3② / `B89-R1b`）**：那一支原版传的是 `-1f`（`Physics2D.GetRayIntersectionNonAlloc(ray, buf, -1f)`）⇒ 负距离 ⇒ **恒 0 命中**（原版怪值，照抄、⛔ 别改成 `Mathf.Infinity`）⇒ 原版**永远走不到**这条 2D 路；真正生效的是它自己的 3D 回退支（`boardCamera.ScreenPointToRay(TouchInputManager.TouchPosition)` → `Physics.RaycastNonAlloc(..., 200f, BoardElements)` → `GetComponentInParent<CardScript>()`，`.c:204-265`）。
        /// ⇒ 我们这条「逐卡 collider 取 z 最小」是原版那条**语义**的对应物，但**不是原版实际走的那条路**（出射相机 / 层掩码都不同）—— 如实标着，⛔ 别拿 `.c:104-130` 当「原版 2D 支可达」的证据。
        /// `Physics2D` 的 `OverlapPoint` **没有 z**（前后叠着的卡会同时命中、顺序也不保证）⇒
        /// 「z 最小」仍是原版那条语义的对应物（离相机最近 = 最先被射线打到），必须自己排。</para>
        /// <para>🔴🔴 **2026-10-19（`T09` 步骤 B · `W57` · B2a）起命中口 = 真 collider**：逐张问
        /// <see cref="CardView.HitByCollider"/>（= 原版 `2DCard` 上那颗 `BoxCollider2D`，
        /// 尺寸/偏移判据见 `CardView.HitRect`），**不再手算矩形**（旧实现是 `CardView.Contains`）。
        /// 换牌区那条 `MulliganPanel.CardAt` 是**转发本方法**的 ⇒ 一起换掉，判据仍只此一份。</para>
        /// <para>🔴 **查询前必须 `Physics2D.SyncTransforms()`**（每帧一次；`HitByCollider` 内部也调，幂等）：
        /// 卡的位移是每帧**直接写 `transform`** 的（布局 / 飞行 / 出牌 / 自检摆位），而 `Physics2D`
        /// 只在 `FixedUpdate` 或显式同步时把 transform 灌进物理世界（`autoSyncTransforms` 默认 `false`）
        /// ⇒ 不同步就会读到**上一帧**的 collider（画面完全正常、命中差一帧 = 静默）。
        /// ⚠️ 批处理（无帧循环）与「同一帧内改完卡位立刻查询」（自检把两张卡摆重叠再 `CardAt`）
        /// **必须**靠这一句。</para>
        /// <para>⚠️ **与 `Contains` 的语义差**（逐条记在 `CardView.HitByCollider` 的 doc 里）：
        /// ① 卡根 `SetActive(false)` 后 `Physics2D` 不收它（`Contains` 仍 true）—— 藏起来的卡本来就不该可点；
        /// ② `BoxCollider2D` 只跟 **Z 旋转**、不跟 X/Y ±10° 倾摆（**原版同构**，不是偷懒）；
        /// ③ collider 没建（`Build` 没跑过）⇒ `HitByCollider` 退回 `Contains` 并**出声一次**（⛔ 不静默）。</para>
        /// <para>🔴 **只此一处**：`CardInteraction.HitTest`（手牌）与 `MulliganPanel.CardAt`（换牌区）
        /// 都调它 —— 以前一处按下标序、一处按 z 序，扇形重叠区会选到不同的卡（`A1668`）。</para>
        /// <para>⚠️ 换牌那一行**本来就不重叠**（`m_useRotation=0`、平直横排、间距 &gt; 卡宽）⇒
        /// 收成同一条判据后，换牌期的行为**零变化**；只有扇形手牌的重叠区才分得出两套规则。</para></summary>
        public static CardView Frontmost(IReadOnlyList<CardView> cards, Vector3 world)
        {
            if (cards == null) return null;
            // 🔴 查询前同步物理世界（见上面 doc）：每帧一次即可；`HitByCollider` 内部也调一次（幂等）。
            Physics2D.SyncTransforms();
            CardView best = null;
            float bestZ = float.MaxValue;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                // 真 `BoxCollider2D`（逐颗比对本卡那一颗）；collider 没建时它自己退回 `Contains` 并出声一次。
                if (c == null || !c.HitByCollider(world)) continue;
                float z = c.transform.position.z;
                if (z < bestZ) { bestZ = z; best = c; }     // z 越小越靠前（顺序与平局规则同旧实现）
            }
            return best;
        }

#if UNITY_EDITOR
        /// <summary>🆕 **2026-10-19（`T09` 步骤 B · `W57` · B2a）自检用**：这个点上有**几张**候选卡的
        /// **真 collider** 命中（0 = 一张都没有）。
        /// <para>用途 = 「重叠区取 z 最小」那条断言的**前提探针**：先证明两张**都**命中
        /// （否则那条断言退化成一个候选、验不出排序），再断 `Frontmost` 取的是 z 小的那张。
        /// 判据走生产 API <see cref="CardView.HitByCollider"/>（与 `Frontmost` 同一个口），
        /// ⛔ 不在这里重算矩形（否则「实现与检测器一起改回去」仍然全绿）。</para></summary>
        public static int FrontmostHitCountForTest(IReadOnlyList<CardView> cards, Vector3 world)
        {
            if (cards == null) return 0;
            Physics2D.SyncTransforms();
            int n = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                if (c != null && c.HitByCollider(world)) n++;
            }
            return n;
        }
#endif

        /// <summary>命中哪张手牌。**从最前面往后测**，不然会选中被压住的那张。
        /// 🔴 判据本体 = <see cref="Frontmost"/>（与换牌区共用，`A1668`）。</summary>
        int HitTest(Vector3 world)
        {
            var c = Frontmost(_cards, world);
            return c == null ? -1 : _cards.IndexOf(c);
        }

        // ---- 输入（新输入系统）----
        static Vector2 PointerScreen()
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }

        // ==================================================================
        //  🔴 2026-10-22（`W63·审查-右键hover`）：**右键 / 双指那一整道闸**
        // ==================================================================
        //  原版**自己有这个名字**（不是散在各调用点里的一句 `if`）：
        //    `d:/2/tools/decomp_full/CardScript__IsRightButtonOrTwoFingerTouch.c:8-10`
        //        bVar1 = Input.GetMouseButton(1);      // ← 右键**按着**（电平，不是按下沿）
        //        iVar2 = Input.get_touchCount();
        //        return 1 < iVar2 | bVar1;             // ← 双指以上 **或** 右键按着 ⇒ 真
        //    （签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardScript.cs:2008`
        //      `private bool IsRightButtonOrTwoFingerTouch(PointerEventData eventData)`）
        //  ⚠️ **不是** `PointerEventData.button == Right` —— 那个形参在**函数体里一次都没读**，
        //     判据只读全局 `Input`。⇒ 与「这一下是哪个键按的」无关：右键按着时左键按在卡上照样被拦。
        //  原版两个入口在函数体**第一句**就用它早退（闸内什么都不做）：
        //    · `CardScript__OnTouchEnter.c:12-14` —— 悬停那条（`ShowCardInHand` / `TryCardHighlight` 都在闸内）
        //    · `CardScript__OnTouchDrag.c:19-21`  —— 拖拽那条（`TryDraggingFromHand` / `TryDraggingFromBoard` 都在闸内）
        //  ⚠️ **另三个入口原版【没有】这道闸**（全库 grep `GetMouseButton(1)` 只命中 5 个文件，
        //     `CardScript` 这族只有上面两个 + 本方法）：`OnTouchDown` / `OnTouchExit` / `OnTouchUpAsButton`
        //     ⇒ **按下登记、离开、松手成点击**照旧跑 —— ⛔ 别把闸「统一」到这三处（那是过度复刻）。
        //  落地：本类三个等价入口**全部走下面这一个函数**（⛔ 别在调用点再写一遍判断）：
        //    ① `UpdateHover` —— 进新卡不许；**离开照旧**（原版 `OnTouchExit` 无闸）
        //    ② `PickUp` —— 起拖（原版唯一到 `StartMovingFromHand` 的路在 `OnTouchDrag` 闸内）
        //    ③ `UpdateDrag` / `TickPressCandidate` —— 拖拽跟手与过阈值。

        /// <summary>自检用：把这道闸直接拨过去（`null` = 回到读真机）。生产路径永远是 `null`。</summary>
        bool? _rightHeldForTest;
        public void SetRightButtonHeldForTest(bool? on) { _rightHeldForTest = on; }

        /// <summary>原版 `CardScript.IsRightButtonOrTwoFingerTouch()`（`…c:8-10`）的等价物：
        /// **右键按着 或 双指以上触摸** ⇒ `true`。
        /// <para>🔴 **唯一判据** —— 悬停 / 起拖 / 拖拽三个入口都只问它。</para>
        /// <para>⚠️ 触摸那半照 `TouchInputManager.ReadRaw` 的既有口径（`Battle/TouchInputManager.cs:397-420`）：
        /// legacy `Input.touchCount` ≡ 新输入系统里数 `t.press.isPressed` 的活跃手指（`touches` 是定长 10 个）。
        /// 桌面 `Touchscreen.current == null` ⇒ 恒 false —— 那一半是**移动端才有的闸**，本机验不到，如实记。</para></summary>
        public bool IsRightButtonOrTwoFingerTouch()
        {
            if (_rightHeldForTest.HasValue) return _rightHeldForTest.Value;   // 自检口（生产恒为 null）
            if (Mouse.current != null && Mouse.current.rightButton.isPressed) return true;
            var ts = Touchscreen.current;
            if (ts == null) return false;
            int active = 0;
            foreach (var t in ts.touches) if (t.press.isPressed) active++;
            return active > 1;
        }

        static bool PressedThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }

        static bool ReleasedThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
            return false;
        }

        Vector3 PointerWorld()
        {
            return LayoutSpace.ScreenToWorld(PointerScreen(), cam);
        }

        Vector3 _ptrWorld;
        bool _ptrWorldOk;
        bool _ptrWildReported;

        /// <summary>
        /// 指针的世界坐标 —— **带单帧野值防护**。
        ///
        /// 🔴 为什么需要（2026-09-17 实测抓到）：当时 `Release` 判落点用的是**卡的当前位置**，
        ///    而卡的位置每帧被 `UpdateDrag` 拉向「指针 + 抓取偏移」。
        ///    ⇒ **指针只要野一帧，卡就被拽飞**，紧接着 `Release` 按飞走的卡位置判「落点不合法」
        ///    ⇒ 明明拖到了格位上却回弹。实测抓到的一帧：指针世界坐标 **(-37.55, 15.43)**
        ///    —— 可见区才 17.78 × 10（反推屏幕坐标约 **-4127 px**），那一帧卡已经从
        ///    槽 0 飞到了 (-2.14, -3.35)，于是判非法。**看起来像「拖得不准」，其实是指针读坏了。**
        /// 📌 **2026-10-15（`A2107`）起判据换成指针本身**（照原版）⇒ 这道防护现在是**直接**保命的那道
        ///    （以前是「经卡间接」）。⚠️ 但那次换口径**不是推翻上面这条**：野值防护治的是**读数坏**，
        ///    换指针治的是**两个量混用**，两件事都要。
        ///
        /// 阈值取「可见区 ±1.5 倍」：正常拖拽（含拖到画面边缘）远远到不了，到了就是读数不可信。
        /// 越界一律**丢掉这一帧、沿用上一帧**，并且**只报一次**（不刷屏）。
        /// </summary>
        Vector3 PointerWorldSafe()
        {
            var w = PointerWorld();
            bool wild = Mathf.Abs(w.x) > LayoutSpace.VisibleWidth * 1.5f
                     || Mathf.Abs(w.y) > LayoutSpace.VisibleHeight * 1.5f;
            if (wild)
            {
                if (!_ptrWildReported)
                {
                    _ptrWildReported = true;
                    Debug.LogWarning($"[CardPresentation] 指针读数越界：世界 ({w.x:F2},{w.y:F2})，"
                                   + $"可见区 {LayoutSpace.VisibleWidth:F2}×{LayoutSpace.VisibleHeight:F2}"
                                   + " —— 这一帧丢弃、沿用上一帧（照用会把正在拖的卡拉飞）");
                }
                if (_ptrWorldOk) return _ptrWorld;
                return w;      // 从来没拿到过合法值：只能照用（不引入新状态）
            }
            _ptrWildReported = false;
            _ptrWorld = w;
            _ptrWorldOk = true;
            return w;
        }

        // ---- 给批处理自检用的显式接口（不进 play 模式也能走完整流程）----
        public void SimulateHover(Vector3 world) { UpdateHover(world); }
        public void SimulatePress(Vector3 world) { var h = HoveredCard; if (h != null && _dragging == null) PickUp(h, world); }
        public void SimulateDrag(Vector3 world, float dt) { if (_dragging != null) UpdateDrag(world, dt); }
        public void SimulateRelease(Vector3 world) { if (_dragging != null) Release(world); }

        /// <summary>🆕 **自检口**（`K2`）：最近一次 `Release` 打出去的那一行日志原文
        /// （`null` = 还没松过手）。
        /// <para>为什么需要它：那一行里「拖动中=」这一格 2026-10-25 之前**恒为 False**
        /// （先清 `_dragging` 再读它）⇒ 真机验收据此误判成「拖拽根本没起来」。断言直接读这一行，
        /// ⛔ 不去抓 Unity 的 Console（批处理里拿不到）。</para></summary>
        public string LastReleaseLogForTest { get { return _lastReleaseLog; } }

        /// <summary>🆕 **自检口**（`K2`）：把「这一按已经按了多久」钉成给定秒数（`null` = 读真时间）。
        /// 见 <see cref="PressElapsedSeconds"/> 的注释（批处理里 `Time.timeSinceLevelLoad` 不前进）。</summary>
        public void SetPressElapsedForTest(float? seconds) { _pressElapsedForTest = seconds; }

        /// <summary>🆕 **自检口**（`K2`）：这一按已经按了多久（秒）—— 生产判据本体。</summary>
        public float PressElapsedForTest { get { return PressElapsedSeconds; } }

        /// <summary>🆕 **自检口**（`K2`）：`elapsed < 0.2` ⇒ 这一下算「轻点」（= 原版
        /// `OnTouchUpAsButton` 那道时长门的**判据本体**，⛔ 断言里别另算一份）。</summary>
        public bool PressShortEnoughForTest { get { return PressShortEnough; } }

        // ---- 🔴 2026-10-25（`AUD8` #19）：**走 `Update` 那两道闸**的自检口 ----
        //  ⚠️ 上面那四个 `Simulate*` 是**老口**：它们直接调 `PickUp` / `UpdateDrag` / `Release`，
        //     从不经过 `Update` 的「按下/松手边沿」与**闸**（批处理里没有输入设备，只能这样驱动）。
        //     ⇒ 要验「模态窗开着能不能拖」，必须用下面这个口（判据与生产**同一份** `BlockedThisFrame`）。

        /// <summary>自检：推一帧（**过 `Update` 那两道闸**，边沿由调用方喂）。
        /// 返回 true = 这一帧被闸挡下（`Update` 里那一句 `return`）。
        /// <para>⚠️ **不要求 `cam`**：本口直接喂**世界坐标**（`HitTest` / `DropReject` 都是世界空间的），
        /// 而 `Update` 那边要 `cam` 只是为了从屏幕点取指针。自检夹具因此不必造相机。</para></summary>
        public bool SimulateTickForTest(Vector3 world, bool pressedNow, bool releasedNow)
        {
            if (hand == null || board == null || _cards.Count == 0) return false;
            if (BlockedThisFrame()) return true;
            TickInput(world, pressedNow, releasedNow);
            return false;
        }

        /// <summary>自检：这张战术卡要打哪一侧（`"enemy"` / `"own"` / `""`）—— 生产判据本体。</summary>
        public static string TacticTargetSideForTest(CardView card) { return TacticTargetSide(card); }

        /// <summary>自检：落点判定的**逐道闸**读数（生产判据本体 `DropReject`，⛔ 断言里别另算一遍）。
        /// 返回 <see cref="DropRejectKind"/> 的枚举值；`why == null` = 合法。</summary>
        public int DropRejectKindForTest(Vector3 pos, CardView card, out int slot, out BoardLayout which, out string why)
        {
            DropRejectKind k;
            why = DropReject(pos, card, out slot, out which, out k);
            return (int)k;
        }

        // ---- 🆕 2026-10-19（`C3` #11）：`TouchInputOn` 那条**候选态**的自检口（走生产同一批方法）----
        //  ⛔ 不是另写一份状态机：下面三个都直接调 `Update` 里那三句（`Begin/Tick/EndPressCandidate`）。

        /// <summary>自检：现在是不是「按下已记、还没过起拖阈值」那一档。</summary>
        public bool PressPendingForTest { get { return _pressPending; } }

        /// <summary>自检：这一按**是否还按在卡的命中区里**（原版 `CardCollider.allowClick` 的等价物，
        /// 🆕 2026-10-19 战斗线 `T16`）。判别式：按下后在卡外推一帧 ⇒ 必须 `false`；
        /// 原地不动 ⇒ 必须 `true`。</summary>
        public bool PressInsideForTest { get { return _pressInside; } }

        /// <summary>自检：按 `TouchInputOn` 档按下（**不抓**）。前提与真实那条一样：已经 hover 到某张。</summary>
        public void SimulatePressPendingForTest(Vector3 world)
        {
            var h = HoveredCard;      // 🆕 `J3`：带范围夹取（同 `Update` 那一处）
            if (h != null && _dragging == null) BeginPressCandidate(h, world);
        }

        /// <summary>自检：推一帧候选态；返回这一帧有没有**真的抓起来**。</summary>
        public bool SimulatePendingTickForTest(Vector3 world)
        {
            bool was = _dragging != null;
            if (_pressPending) TickPressCandidate(world);
            return !was && _dragging != null;
        }

        /// <summary>自检：候选态松手（生产 `Update` 同一句）。</summary>
        public void SimulatePendingReleaseForTest(Vector3 world) { if (_pressPending) EndPressCandidate(world); }

        // ---- 🆕 2026-10-18（A964①）：**手牌命中区 / 实绘矩形**的只读口（⛔ 只给自检，不给生产用）----
        //  判据（`E2`）：「凡参与点击命中的节点，它的**命中区矩形**必须覆盖它**画出来的矩形**」。
        //  ⛔ 命中**一律调生产函数**（下面 `HitCardForTest` 就是私有的 `HitTest` 本体）——
        //    探针不重算一份算式（那是「实现与检测器一起改回去还全绿」的自证）。

        /// <summary>自检用：手牌里有几张（= `_cards` 的长度）。</summary>
        public int HandCardCountForTest { get { return _cards.Count; } }

        /// <summary>🆕 2026-10-19（`T03`）：第 `i` 张手牌（`_cards[i]`，越界 ⇒ null）。
        /// 为什么需要它：`HitCardForTest` 返回的是**本列表的下标**，而断言要拿那张卡的
        /// **可点矩形标架**去取边界点 ⇒ 必须拿到同一个对象，⛔ 别拿 `BattleDriver.HandViewAt(i)`
        /// 顶替（两条列表的顺序没有契约，`_cards` 是 `HandLayout.Refresh` 喂进来的）。</summary>
        public CardView HandCardForTest(int i)
        {
            return i >= 0 && i < _cards.Count ? _cards[i] : null;
        }

        /// <summary>自检用：点在手牌第几张上（-1 = 没点到任何一张）—— **就是真实输入那条** <see cref="HitTest"/>。</summary>
        public int HitCardForTest(Vector3 world) { return HitTest(world); }

        /// <summary>自检用：第 i 张手牌**实绘卡面**在世界里的某一点。
        /// `u,v ∈ [−0.5, 0.5]` 是**卡的局部系**里的归一化位置（× `CardView.Width/Height`）：
        /// `(0,0)` = 中心、`(±0.5, ±0.5)` = 实绘矩形的四角。
        ///
        /// 🔴 **只能在卡的局部系里取点** —— 手牌是扇形（每张都带旋转 + 缩放），
        ///    按世界轴挪 dx/dy 会挪到邻牌上去（`HitTest` 就判给别人了 ⇒ 假红）。
        /// 🔴 拿它去喂 <see cref="HitCardForTest"/> 正是本行要的那件事：
        ///    **实绘矩形的内侧必中 / 外侧必不中**（两态都断）。</summary>
        public bool HandCardPointForTest(int i, float u, float v, out Vector3 world)
        {
            world = Vector3.zero;
            if (i < 0 || i >= _cards.Count || _cards[i] == null) return false;
            world = _cards[i].transform.TransformPoint(
                        new Vector3(u * CardView.Width, v * CardView.Height, 0f));
            return true;
        }

        // ==================================================================
        //  🆕 2026-10-26（执行代理 `F6` · 批次 4）：**抬起档的成对开关**
        // ==================================================================
        //  判据（一手，`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardScript.cs`，**真方法体**那份）：
        //   · `StartShowingCardInHand`（`:10593-10601`）—— 抬牌（= 手牌「亮出来」）末尾三句：
        //       `card2DController.ToggleTooltips(true)`（本卡 tooltip **开**）
        //       → `cardCollider2D.SetCollidersShowBig(true, PositionInHand)`（第二颗 collider 开 + 主面回常态矩形）
        //       → `CardUI.ToggleOpenCardDisplayOnTouch(false)`（抬牌期不开展示窗 —— 这一条早在 `CardDisplaySuppressedFor`）。
        //   · `StopShowingCardInHand`（`:11013-11036`）—— 放回去那三句：`SetCollidersShowBig(false)`
        //       → `ToggleTooltips(false)` → `ToggleOpenCardDisplayOnTouch(true)`。
        //   ⇒ **成对**：进 = 开、出 = 关；**离开无条件复位**（原版 `StopShowingCardInHand` 没有任何闸）。
        //   ⚠️ **进入要走 `DragOnlyBlocked()` 那道闸**：原版抬牌的前置是
        //     `BattleManager.CanDisplayHandCard` = `isPlayer && cardState==1 && UIstate==1`
        //     （`dc/BattleManager__CanDisplayHandCard.c:7-11`）⇒ 选目标 / 打法选择器那两档**根本不进
        //     `ShowCardInHand`**（同 `UpdateHover` 里那段 `K2` 注释）。`_hovered` 本身照常更新（轻点看牌要用它）。
        //   🔴 **卡侧 API 是 `Core/CardView.SetCollidersShowBig`（`CardView.cs:13931`，E7 已做完）** ——
        //     本类只负责「什么时候调」，⛔ 不在这儿重写一遍几何（两处写同一条规则 = 迟早不一致）。
        //   🔴 **tooltip 那半是逐卡的、不是全局的**：`BattleDriver.TickTooltipAt` / `TickCardTip`
        //     读的就是 `CardView.TooltipsEnabled`（逐卡闸，早就在）。⛔ 别把这两句换成
        //     `Tooltip.SuppressTooltips`（那是窗口级的总闸，会把 HUD 那几条一起掐掉）。

        /// <summary>`_hovered` 的**唯一写口**（本类只有这一处改「指针压在哪个下标」）。
        /// <para>原版那两条成对方法（`StartShowingCardInHand` / `StopShowingCardInHand`）在本类的等价物
        /// 就是「下标从 A 变成 B」这一件事 ⇒ 全部走这里，⛔ 别再直接写 `_hovered = …`（那样会漏掉成对开关）。</para></summary>
        void SetHovered(int idx)
        {
            int prev = _hovered;
            _hovered = idx;
            ApplyShowInHand(prev, idx);
        }

        /// <summary>抬起档的成对开关（判据见上面那段）。两个实参都是 **`_cards` 的下标**（-1 = 没有）。
        /// <para>① 离开：`prev` **无条件**复位（`SetCollidersShowBig(false)` + tooltip 关）。</para>
        /// <para>② 进入：`now` 只有**没被 `DragOnlyBlocked()` 挡**才开（= 原版 `CanDisplayHandCard` 那道闸）。</para>
        /// <para>⚠️ 两个下标是同一张卡（含「都是 -1」）时**什么都不做**。</para></summary>
        void ApplyShowInHand(int prevIdx, int nowIdx)
        {
            var prev = (prevIdx >= 0 && prevIdx < _cards.Count) ? _cards[prevIdx] : null;
            var now  = (nowIdx  >= 0 && nowIdx  < _cards.Count) ? _cards[nowIdx]  : null;
            if (prev == now) return;
            if (prev != null)
            {
                prev.SetCollidersShowBig(false);
                prev.TooltipsEnabled = false; SetTargetsSuffix(prev, null);   // HUD:148 —— 放回 ⇒ 复原描述（判据见类尾）
            }
            if (now == null || DragOnlyBlocked()) return;
            now.SetCollidersShowBig(true);
            // 原版抬牌那一支的 `ToggleTooltips(true)`：**只有抬起来这一张**能吃 tooltip。
            now.TooltipsEnabled = true; SetTargetsSuffix(now, TargetSuffixFor);   // HUD:148 —— 抬牌 ⇒ 拼「可用目标」（判据见类尾）
        }

        /// <summary>把**当前这批手牌**全部复位成「没抬起」档（`SetCards` 换列表时调）。
        /// <para>为什么换列表也要复位：视图会被复用 / 搬去战场 —— 带着 `big = true` 走就是
        /// 「场上一张牌脚下多一块看不见的判定区」；tooltip 那半同理（原版新卡 `CreateCard` 末尾
        /// 就是 `ToggleTooltips(false)`，`BattleCardManager.cs:128`）。</para>
        /// <para>⚠️ 它顺带把 `_hovered` 清成 -1（旧下标对新列表没有意义）—— **必须在 `_cards.Clear()` 之前调**。</para></summary>
        void ResetShowInHandAll()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null) continue;
                c.SetCollidersShowBig(false);
                c.TooltipsEnabled = false; SetTargetsSuffix(c, null);   // HUD:148 —— 换列表 ⇒ 连描述后缀一起复原（视图会被复用/搬去战场）
            }
            _hovered = -1;
        }

        // ==================================================================
        //  🆕 2026-10-26（`B8_AL`）：**输入态一行可读**（账升 A 的 G3 读数口）
        // ==================================================================
        /// <summary>把「指针这一层此刻的状态」打成一行 —— 供自检断言 / 诊断读，⛔ 不参与任何逻辑。
        /// <para>为什么要有：账里 `OnTouchDown` / `OnTouchDrag` / `OnTouchEnter` / `OnTouchExit` /
        /// `ShowCardInHand` / `TryDraggingFromHand` 那几行的可观察量**都是同一批状态位**（悬停下标、
        /// 拖拽中的卡、空位下标、累计位移、按下时长、以及四道闸），它们散在十个私有字段里；
        /// 不集中打出来，自检就只能断言「没崩」。⛔ 别在这里做判断 —— 只读。</para>
        /// <para>刻意读**闸的当前值**（`DragBlocked` / `FrozenNow` / `TouchInputOn`）：这三条是原版
        /// `OnTouchDrag.c:17-23` 与 `TryDraggingFromHand.c:64-69` 的等价物，账里要的就是「此刻拦不拦」。</para></summary>
        public string DescribeInput()
        {
            var hov = HoveredCard;
            return $"悬停={(_hovered >= 0 ? _hovered + ":" + (hov != null ? hov.name : "null") : "无")}"
                 + $" 拖拽中={(_dragging != null ? _dragging.name : "无")}"
                 + $" 空位={_insertIndex} 累计位移={_travel:F3}（轻点门限 {TapThreshold:F2}）"
                 + $" 按下候选={(_pressPending ? "是" : "否")} 按下时长={PressElapsedSeconds:F3}s"
                 + $" 短按={PressShortEnough} 第二颗碰撞={((hov != null) ? (hov.ShowInBigColliderEnabled ? "开" : "关") : "-")}"
                 + $" 冻结={FrozenNow} 拖拽闸={(DragBlocked() ? "拦" : "放")} 触屏档={TouchInputOn}";
        }

        // ==================================================================
        //  换位之后的「教程指针重算」出口（原版 `HandCardMovedToNewIndex` 末尾那一句）
        // ==================================================================
        /// <summary>🔴 2026-10-26（B9/E6P14 · 施工单 1170）：**手牌列表真的换了位**（= 原版
        /// `PlayerHand.HandCardMovedToNewIndex` 返回 1 的那一支）。
        ///
        /// <para>判据（`decomp_full/PlayerHand__HandCardMovedToNewIndex.c:28-38`；ILSpy 同族
        /// `PlayerHand.cs:3565-3612` 逐句核过）：`Remove` + `Insert` + `MoveCardsInHandToPosition(-1, …)`
        /// 之后**还有一句** `manager.CheckForTutorialPointer()` —— 那句话把「当前这条脚本动作」的锚点
        /// 重新解析一遍（`AiScripted.ShowPlayerActionAnim`）⇒ **换位之后教程箭头跟到新位置**。</para>
        ///
        /// <para>⚠️ 我们这一句的落点在 `BattleDriver`（`ShowPlayerActionAnim` 是私有，`_tutHandIdx` /
        /// `_handViews` 也都在那边）⇒ 本组件**只发事件**，由驱动层订阅后做同一件事。
        /// ⛔ 别在这里自己算锚点 —— 锚点的唯一判据是 `BattleDriver.TutorialAnchor`。</para>
        ///
        /// <para>⚠️ 净结果相同的那两支（`newIndex == idx` / `Count &lt;= newIndex`）**不发** ——
        /// 原版在那两支里 `CheckForTutorialPointer` 之前就 `return 0` 了（.c:19-24 / :27）。</para></summary>
        public event Action<CardView> OnHandCardMovedToNewIndex;

        // ==================================================================
        //  🆕 2026-10-26（`W3K04` · 账 `战斗界面_HUD:148` 的**描述那一半**）：
        //    **抬牌时把「可用目标」印进卡面描述**（原版 `BattleCardUI.ShowAvailableTargets`）
        // ==================================================================
        //
        //  原版末句 = `SetDescription(desc + "<i>" + 那段 + "</i>")`。
        //  判据（🥇 `decomp_full/BattleCardUI__ShowAvailableTargets.c:1-133`）逐句：
        //   ① `card(+0x248) == null` ⇒ NRE；`card.isPlayer(+0x40) == 0` ⇒ **return**（只我方）
        //   ② `card.cardState(+0x228) != inHandShowing(8)` ⇒ **return**
        //      ⇒ 这段文案**只在「牌抬起来展示」那一档出现**，我们的等价位 = `ApplyShowInHand` 的
        //         「进入 / 离开」两支（`StartShowingCardInHand` / `StopShowingCardInHand` 的等价物）
        //   ③ `!CardScript.ShouldShowTargetsAvailable(false)` ⇒ return
        //   ④ `list = CardScript.GetTargetsAvailable()`
        //      = `AbilityLogic.GetTargets(manager, raw.targetsAvailableCounter(+0x298), this, this, 0, 1, 0)`
        //      ⇒ **目标池**
        //   ⑤ `targetsAvailableCounter.maxTargets(+0x40) == 1` 且 `list.Count > 0` ⇒
        //      `"(" + list[0] 的卡名 + ")"`；其中**对面那张是 Tactic + spellType = Trap(70) /
        //      SecretOrder(60)** 时原版换成一条 `Card_Trait/*` 词条（不印卡名）
        //   ⑥ 其余 ⇒ `GetTargetsAvailableText(list.Count)` = `Battle/HUD/TargetsAvailable` 的 `{0}` 换成 n
        //   ⑦ 最外层拼装（`"<i>" … "</i>"` 与重拟合）**不在本类** —— 写口 = `CardView.SetDescriptionSuffix`
        //      （W2K03 2026-10-26 建；它内部走 `BuildTextLayers`，两条建字路都覆盖）
        //  ⇒ 本类只做三件事：**什么时候挂**（抬牌成对开关）· **拼哪一段**（`AvailableTargetsSuffix`）·
        //     **怎么复原**（`null` ⇒ 裸描述）。
        //  ⛔ 与 `HUD:686`（棋盘按住 0.3 s 弹卡 / 手牌 0.2 s **不**弹）**是两件事**：
        //     那条是**时间闸**，这条是**文案**；别塞进同一处判断。

        /// <summary>抬牌那一刻「可用目标」后缀的来源（宿主接 —— 它手上有 `BattleContext` / 座位号 / `CardDef`）。
        /// 返回 `null`/空 = 这一张不拼后缀。
        /// <para>宿主接线一笔（`BattleDriver.Begin` 那一族里）：
        /// <c>interaction.TargetSuffixFor = v =&gt; CardInteraction.AvailableTargetsSuffix(Ctx, _me, DefOf(v));</c></para>
        /// <para>⚠️ 没接时**什么都不印**（不是空串后缀）—— 原版没接就等于没有这道文案。</para></summary>
        public Func<CardView, string> TargetSuffixFor;

        /// <summary>抬牌成对开关的**第三半**（前两半 = `SetCollidersShowBig` / `TooltipsEnabled`）：
        /// `src == null` ⇒ 复原成裸描述；否则现算一次再挂上。
        /// <para>幂等靠 `CardView.SetDescriptionSuffix` 自己那道闸（**同一个后缀重复调不重建**），
        /// ⛔ 别在这里再加一份缓存 —— 两处记同一条状态迟早不一致。</para>
        /// <para>⚠️ 场上形态的卡 `CardView.SetDescriptionSuffix` 内部直接返回（没有文字层），
        /// 这里不另判 `CardFace`（判据留一处）。</para></summary>
        void SetTargetsSuffix(CardView v, Func<CardView, string> src)
        {
            if (v == null) return;
            v.SetDescriptionSuffix(src == null ? null : src(v));
        }

        /// <summary>原版 `BattleCardUI.ShowAvailableTargets` 里**「那段」**的拼装（第 ③–⑥ 步）。
        /// 返回 `null`/空 = 这一张不拼后缀（原版那三条早退）。
        /// <para>🔴 **目标池只走 `RuleEngine.RuleCore.PlayTargetPool`** —— ⛔ 别在表现层另写一套
        /// 「谁能点」（那就是「两处写同一条规则 = 迟早不一致」）。</para>
        /// <para>🔴 那道「要不要显示」的闸只走 `CardFaceRules.ShouldShowTargetsAvailable`
        /// （原版 `CardScript.ShouldShowTargetsAvailable(false)` 的唯一正本），⛔ 别在这里重算
        /// `targetsAvailableCounter != null && targetsAffected != any`。</para>
        /// <para>⚠️ **一条查不到的判据（如实记，见报告 §五）**：第 ⑤ 步里「敌方 Trap / SecretOrder
        /// 战术卡换成词条」那两个词条名，本机取不到 —— 官方词表（10,862 键）里
        /// `Card_Trait/trap` / `Card_Trait/secretOrder` **都不存在**，而原版那两个常量是
        /// 元数据里的字符串字面量（`.c` 里只能看到 `DAT_1842ced40` / `DAT_1842ceb40`）。
        /// ⇒ 这一档**不印卡名**（印了就泄露对手隐藏卡的身份），暂按 ⑥ 的「N 个可用目标」印，并**出声一次**。</para></summary>
        public static string AvailableTargetsSuffix(RuleEngine.BattleContext ctx, int owner,
                                                   RuleEngine.CardDef card)
        {
            if (ctx == null || card == null) return null;
            if (!CardFaceRules.ShouldShowTargetsAvailable(card)) return null;            // ③
            var pool = RuleEngine.RuleCore.PlayTargetPool(ctx, owner, card);              // ④
            if (pool == null) return null;
            if (AvailMaxTargets(card) == 1 && pool.Count > 0)                             // ⑤
            {
                var t = pool[0];
                if (t == null || t.Card == null) return null;
                if (t.Card.Type == "tactic" && !OnSide(ctx, owner, t))
                {
                    int st = t.Card.SpellType;                                            // 原版 `EntityScript.get_spellType`
                    if (st == SpellTypeTrap || st == SpellTypeSecretOrder)
                    {
                        if (!_warnedTargetTraitTerm)
                        {
                            _warnedTargetTraitTerm = true;
                            Debug.LogWarning("[CardPresentation] 抬牌时单目标那张是敌方 "
                                           + (st == SpellTypeTrap ? "Trap" : "SecretOrder")
                                           + " 战术卡：原版这里把它换成一条 `Card_Trait/*` 词条再印，"
                                           + "而**那条词条名本机没取到**（官方 10,862 键里 `Card_Trait/trap` /"
                                           + " `Card_Trait/secretOrder` 都不存在）⇒ 暂按「N 个可用目标」印；"
                                           + "⛔ 不印卡名（印了就是泄露对手隐藏卡的身份）");
                        }
                        return CardText.TargetsAvailable(pool.Count);
                    }
                }
                return "(" + CardText.Name(t.Card) + ")";
            }
            return CardText.TargetsAvailable(pool.Count);                                 // ⑥
        }

        /// <summary>原版 `SpellType.Trap` / `SpellType.SecretOrder`（`dump.cs` 的 `SpellType` 枚举：
        /// `SecretOrder = 60` / `Trap = 70`）。</summary>
        const int SpellTypeSecretOrder = 60;
        const int SpellTypeTrap = 70;

        static bool _warnedTargetTraitTerm;

        /// <summary>原版 `RawCardScript.targetsAvailableCounter(+0x298).maxTargets(+0x40)`。
        /// <para>`-1` = **没落 / 未知**（⛔ 不是「不限」）；`<= 0` = 不限（原版注释口径）。</para>
        /// <para>取法与 `RuleEngine/Editor/RuleEngineTest_UnitRoles.cs:236-241` **同一个口**
        /// （`card.Conditions` 里 `Role == "avail"` 的那一行 ⇒ `AvailSet`）—— ⛔ 别另写一份解析。</para></summary>
        static int AvailMaxTargets(RuleEngine.CardDef card)
        {
            if (card == null) return -1;
            var conds = card.Conditions;
            if (conds == null) return -1;
            foreach (var r in conds)
            {
                if (r == null || r.Role != "avail") continue;
                if (r.AvailSet == null) return -1;
                return r.AvailSet.MaxTargets;
            }
            return -1;
        }

        /// <summary>这张单位是不是 `owner` 那一侧的 —— 原版是 `EntityScript.isPlayer(+0x40)`，
        /// 而我们的 `UnitState` **没有 owner 字段** ⇒ 只能按 `ctx.Players[owner].Board` 反查引用。
        /// ⚠️ 只服务上面第 ⑤ 步的「对面那张」判定；⛔ 别把它当成「谁是行动方」的通用判据。</summary>
        static bool OnSide(RuleEngine.BattleContext ctx, int owner, RuleEngine.UnitState u)
        {
            if (ctx == null || u == null) return false;
            if (ctx.Players == null || owner < 0 || owner >= ctx.Players.Length) return false;
            var board = ctx.Players[owner].Board;
            if (board == null) return false;
            for (int i = 0; i < board.Length; i++) if (ReferenceEquals(board[i], u)) return true;
            return false;
        }

        // ── 账 767/768（第二批）：拖卡倾斜的三个常量 ──
        //  判据 = `decomp_full/CardScript__.ctor.c:41-45`（`0x41A00000` / `0xC1A00000` / `0x41200000`）。
        //  ⚠️ 它们对应的三个字段在 `CardScript` 里**没有 `[SerializeField]`** ⇒ 预制体不覆盖 ⇒ ctor 值即真值
        //     （与 `chargeAttackAngle` 那三个**不同** —— 那几个有 `[SerializeField]`，取预制体值）。
        const float DragTiltMaxDeg = 20f;        // maxCardRotation（Vector2 两轴同值）
        const float DragTiltMinDeg = -20f;       // minCardRotation
        const float DragTiltReduction = 10f;     // movingRotationReduction

        // ════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-28（`B89-R4b`）· 账 `战斗界面_HUD` 的 695 与 761
        // ════════════════════════════════════════════════════════════════════════════════════
        //
        //  ── 695 `CanMoveHandCard` 的第三条闸（**已接**，见上面 `DragBlocked`）──
        //  判据：原版 `BattleManager.CanMoveHandCard()` = `if (playerHand.animFlag) return false; return UIstate−1 == 0;`
        //        （ILSpy `BattleManager.cs:46525-46542`，唯一调用点 = `CardScript.cs:10313` 的 `OnTouchDrag`）。
        //  🔴 **账里 `特效动画:497` 的备注说「原版 `PlayerHand.IsDoingAnim` 全量 0 调用、`animFlag` 0 写点」——不成立**：
        //     `animFlag` 在 ILSpy `PlayerHand.cs:858 / 950 / 1302 / 1457` **四处写**（手牌重排协程 `MoveNext` 的进/出），
        //     读点在 `BattleManager.cs:46532`（就在 `CanMoveHandCard` 里，**不经过** `IsDoingAnim` 那个包装）；
        //     `IsDoingAnim()`（`PlayerHand.cs:3300`）本身才是 0 调用者。⛔ 别照那句删第三条。
        //  等价物 = 本类 `IsDoingAnim`（`_animGate`，`Relayout` 时开、`CardTween.RelayoutDuration` 到点关；
        //     那个时长 = 原版 `timeToPositionCard` 0.2 s）。⚠️ **批处理 / `animateRelayout == false` 时不开闸**
        //     ⇒ 自检档行为与改前**逐字相同**（第三条恒假），要验它只能显式开 `Relayout`。
        //  ⚠️ 顺带的语义：`BlockedThisFrame` 里 `if (DragBlocked() && _dragging != null) AbortDrag(...)` 也会吃这一条
        //     ⇒ 「拖到一半重排动画起来了」会被收回。生产上重排只在松手后起，够不到；如实记这一处**比原版严**。
        //
        //  ── 761 敌方手牌的 `EnemyHandCardClickable`（**本波拒做，理由见下**）──
        //  判据：`decomp_full/CardScript__OnTouchUpAsButton.c:31-36` —— 非我方手牌只有 `spellType == 0xE6 (Sabotage)`
        //        才可点开；落点 `CardHighlight.EnemyHandCardClickable(isPlayer, hasRawCard, sabotageKind)`（`CardHighlight.cs:836`，
        //        已建、全树 0 生产调用点）。
        //  🔴 **为什么不能「接在轻点支」**：本类管的是**我方**手牌 —— 唯一喂料口 `SetCards(...)` 只被
        //     `BattleDriver.cs:27375` 用 `_handViews`（我方）调用，`CardBaseDemo` / `HandAssertE8` 是夹具；
        //     而敌方手牌在场上只有**卡背** `ImageQuad`（`_foeHandViews`），**没有 `CardView`、没有 `CardInteraction`**；
        //     亮出敌方手牌走的是**居中展示**（`_ResolveDisplayFromHand` 的等价物），不是「手牌位上翻面」。
        //     ⇒ 在这里加一句 `EnemyHandCardClickable(true, …)` 恒为真（我方恒 `isPlayer`）= **纯死代码**；
        //       真要接，得先有「敌方手牌翻面成可点的 `CardView`」这条**新链路**（那是 `BattleDriver` 的活，不是本类）。
        //     ⇒ 本波**零改动**，反证留给账（`B89_R4b` 报告的 §1·761）。
        // ════════════════════════════════════════════════════════════════════════════════════

        // ==================================================================
        //  🆕 2026-10-28（账 745）· 原版 `VarsDevice.GetCardInHandYOffsetTouchInput()`
        // ==================================================================
        //  原版（`d:/2/tools/decomp_full/VarsDevice__GetCardInHandYOffsetTouchInput.c` 逐句）：
        //    `if (ScreenUtilities.GetScreenPhysicalSize() > 7.0f) return cardInHandShownYOffsetMin(+0x20);
        //     return cardInHandShownYOffset(+0x1c);`
        //  其中 `GetScreenPhysicalSize()`（`decomp_full/ScreenUtilities__GetScreenPhysicalSize.c`）=
        //    `sqrt(w²+h²) / max(Screen.dpi, 100.0f)`（那个 dpi 下限常量 `read_literal.py 0x1834b2e08` = **100**）。
        //  ⚠️ 两个字段值：ctor 里只设了 `cardInHandShownYOffsetMin = 20.0f`，**没设 `cardInHandShownYOffset`**
        //     （⇒ 出厂 0）；`VarsDevice` 是 SO，资产侧可能覆盖 → 本机 `assets_full` 里没按名找到那份 SO
        //     ⇒ 用 ctor 出厂值（0 / 20）并如实标明「资产值未读到」。
        //  🔴 消费者（原版）= `CardScript.ShowCardInHand` 触摸支：卡片世界 Y = ScreenToWorldPoint.y + 本偏置。
        //     我们这一档**没有「抬牌跟指针重定位」那条路**（触摸悬停走 `Relayout` 的正常手牌位，不跟手）
        //     ⇒ 本读数先落地（自检可读），真正的消费点在抬牌跟手路径建成后才有（机制差，如实记）。
        //  ⚠️ 桌面无触摸 ⇒ 这一支在开发机上取不到真值（`Screen.dpi` 也常为 0）⇒ 只保证「逻辑与原版同形」。

        /// <summary>原版 `VarsDevice.cardInHandShownYOffset`（`+0x1c`，ctor 未设 ⇒ 出厂 0）。⚠️ SO 资产值未读到。</summary>
        public const float CardInHandShownYOffset = 0f;
        /// <summary>原版 `VarsDevice.cardInHandShownYOffsetMin`（`+0x20`，ctor = 20.0f）。</summary>
        public const float CardInHandShownYOffsetMin = 20f;
        /// <summary>原版 `GetCardInHandYOffsetTouchInput` 的屏幕对角阈值（`7.0f` 英寸）。</summary>
        public const float PhysicalSizeThreshold = 7f;
        /// <summary>`ScreenUtilities.GetScreenPhysicalSize()` 的 dpi 下限（`read_literal.py 0x1834b2e08` = 100）。</summary>
        public const float MinDpi = 100f;

        /// <summary>原版 `ScreenUtilities.GetScreenPhysicalSize()` = `sqrt(w²+h²) / max(dpi, 100)`（英寸）。</summary>
        public static float GetScreenPhysicalSize()
        {
            float w = Screen.width, h = Screen.height;
            float dpi = Screen.dpi;
            if (dpi < MinDpi) dpi = MinDpi;          // 原版：`if (100 <= dpi) use dpi else 100`
            return Mathf.Sqrt(w * w + h * h) / dpi;
        }

        /// <summary>原版 `VarsDevice.GetCardInHandYOffsetTouchInput()`：大屏（&gt; 7 英寸）用 `…Min`（20），
        /// 否则用 `…`（0）。消费者 = 抬牌跟手那条路（本类尚未建成，见上文）。</summary>
        public static float GetCardInHandYOffsetTouchInput()
            => GetScreenPhysicalSize() > PhysicalSizeThreshold ? CardInHandShownYOffsetMin : CardInHandShownYOffset;
    }
}
