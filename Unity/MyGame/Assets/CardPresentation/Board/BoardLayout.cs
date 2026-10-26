// BoardLayout.cs — 战场的 9 个格位（督军居中 + 两侧各 4）
//
// 和手牌一样用归一化坐标，换分辨率/宽高比都成立。
// 格位间距/缩放搬自原版实测（下面的常量）；🔴 **原版【不镜像】敌方槽位**（2026-10-26 复核，判据 → `ArenaSlots.Position`）。
//
// ⚠️ 格数 **9** 是硬决定，**三处来源指向同一个数**（⚠️ 2026-10-18 更正：原来写「**权威来源**
//    三处一致」—— 那个「权威」用错了；这三处里**只有第二处是判据**）：
//    · ⚠️ 规则书 :30 ——「玩家棋盘 9 个空格，督军永远占据中央格，两侧各 4 格共可部署 8 张部队卡」
//      —— **粉丝实体版规则书，只算旁证**（非官方）
//    · ✅ **解包资源字段**（判据②）—— battlearena1 的 `MinionArea` `slotsPerSide=4`
//    · ⚠️ `rule_core.gd:44` —— BOARD_SIZE 9 / WARLORD_SLOT 4
//      —— **我们自己的上一版 Godot 复刻，只算旁证**（不是原版语义判据）
//    之前这里是 7 格，那是被推翻过的误读（推翻它的判据 = 上面那条 `slotsPerSide=4`；
//    `rule_core.gd:42` 的修正记录是我们的旁证）。
//    规则引擎那边对应 `RuleEngine.BoardSpec`，**两边必须一致**。
//    🔴 判据顺序 = ① 原版全量反编译 `D:/2/tools/decomp_full/` → ② 解包资源字段 → ③ 成品卡图卡面文字。
//
// ---- 间距/缩放是量出来的，不是拍的 ----
//   ⚠️ **2026-10-19 就地订正（铁律 5 · `T01` §四·3）**：本段原写
//     「出处：`d:/warpforge/scripts/battle.gd` 的场卡尺寸体系（8-27 审查定案，对着原版冻结帧量的）」
//     +「玩家行中心 y = 708，敌方行中心 y = 466」——
//     **那句出处是我们自己的上一版 Godot 复刻**（`CLAUDE.md` 铁律 2：只作旁证），
//     **那两个行心是它的旧校准、不是原版值**（原版实测：我方 **636 px** / 敌方 **425 px**，
//     见下面 `lineY` 的 doc ⇒ 旧值比原版低 72 / 41 px）。
//   ✅ **仍然成立的是尺寸那两条**（判据 = `资料/3DBody_原版场上卡体规格.md` §四之五），原版 1920×1080 下：
//     · 场卡      137.2 × 218.4 px   （2DCard 2.0927 宽 × desiredScale 0.36 × k 182.14）
//     · 相邻中心距 149.3 px          （MinionSeparation 0.82 × 182.14）
//   🔴 **行心 y 不在本段**：它由 `lineY`（我方 0.4110 / 敌方 0.6063）承载，⛔ 别再把 708/466 抄回来。
//   换到我这套（可见高 10 单位 = 1080 px，可见宽 17.78）：
//     placedScale **0.607** → 卡宽 1.271 世界 = 137.2 px；spacing 0.0778 → 1.382 世界 = 149.3 px
//   🔴 **2026-09-17 更正**：`placedScale` 的默认值原来是 **0.876**，与它同一行的这段注释**直接打架**
//      （注释写「0.876 → 卡宽 1.271 世界」，可 2.0927 × 0.876 = **1.833** 世界 = 197.9 px）。
//      **产品侧没受影响**：`BattleScene` 一直显式赋 `BoardScale = 137.2/(2.0927×108) = 0.607`，
//      所以 Battle 场景里一直是对的。**中招的是「取默认值」的那条路** —— `CardBaseDemo` 自建的那块板
//      没赋过这个字段 ⇒ 它的场卡**大了 44%**，而它正是「四档分辨率」的版面验收图。
//      一直没人发现，因为**它当时一个断言都没有**（2026-09-17 补断言时当场抓到）。
//      ⇒ 现在默认值改成 `DefaultPlacedScale`（= 0.607），且**与 `BattleScene` 共用这一个判据**。
//   ⚠️ 原版敌方那一行是**投影收窄**的（131.9 px 步进、卡也更小），**2D 里**不适用 ——
//      这边同尺寸、**也不镜像左右**（原版不镜像 ⇒ `mirror` 只表示「这是敌方那一行」）。
//   🔴 **2026-09-20：真 3D 那条路不吃这一套了** —— 场上的卡搬进透视层之后，落点/缩放
//      逐值照原版（**玩家敌各一份**：步距 0.82/1.53、`desiredScale` 0.36/0.69），
//      判据在 `Board/ArenaSlots.cs`。本文件这份（屏幕空间）**只剩命中判定与底片**在用。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public class BoardLayout : MonoBehaviour
    {
        public const int SlotCount = 9;

        /// <summary>原版场卡宽度（px @1920×1080）—— 出处见文件头（`2.0927 × 0.36 × 182.14`）。
        /// 🔴 **判据只此一处**：落位缩放与 `spacing` 都由它推出来，`BattleScene` 也引用这里，别再各写一份。</summary>
        public const float OriginalCardWidthPx = 137.2f;
        /// <summary>原版相邻格中心距（px @1920×1080）= `MinionSeparation 0.82 × 182.14`</summary>
        public const float OriginalSlotPitchPx = 149.3f;

        /// <summary>落位后的缩放 = 原版卡宽 ÷ (`2DCard` 宽 2.0927 × 我们**每单位像素数** 108)。
        /// = `137.2 / (2.0927 × 108)` = **0.607**（1080p 下卡宽 1.271 世界 = 137.2 px）。</summary>
        public const float DefaultPlacedScale = OriginalCardWidthPx / (CardView.Width * 108f);

        /// <summary>督军槽 —— 正中间那格（原版也是中间）</summary>
        public const int WarlordSlot = 4;

        /// <summary>🔴 **本行的【棋盘行线】的 y（归一化，从下算）** —— 2D 槽点 / 落点底片 / 2D 兜底判据都用它。
        ///
        /// <para>🔴🔴 **2026-10-16（`T02` / `A2104①`）：这个字段原来是「一个字段担两个用途」，现已拆开 ——**
        /// 本字段**只**表示**棋盘那一行**；**手牌 y 守卫**改用 <see cref="HandLimitY"/>（另一条线）。
        /// 拆开是 `A2104①` 的明令（「⛔ 别直接改 0.3444」），因为两条线**根本不是同一根**：
        /// 棋盘行线是**投影收窄后**的卡心、手牌那条是 UI 上的一根限位线。</para>
        ///
        /// <para>**取值（原版实测真值，逐条出处 → `资料/3DBody_原版场上卡体规格.md` §四之五）**：
        /// 我方行卡心 **636 px（距顶）** ⇒ 归一化 **0.4110**；敌方行 **425 px** ⇒ **0.6063**。
        /// 🔴 **`708 / 466` 那对旧值是从 `battle.gd:694-695` 抄来的** —— 那是**我们自己的上一版 Godot 复刻**
        /// （`CLAUDE.md` 铁律 2：只作旁证），比原版**低 72 px / 41 px**。⛔ 别再抄回去。</para>
        ///
        /// <para>⚠️ 这一格被**烘进场景**（`Scenes/Battle.unity` 里那两个 `BoardLayout` 各存一份）
        /// ⇒ 改完必须重跑生成器（`BattleScene.BuildAndSaveScene`），否则 Play 模式读的是场景里那份旧值。</para></summary>
        [Tooltip("棋盘行线的 y（归一化，从下算）—— 我方 0.4110（原版实测 636px 卡心）/ 敌方 0.6063（425px）")]
        public float lineY = 0.4110f;

        /// <summary>🔴 **手牌 y 守卫那条线**（归一化，从下算）—— 与 <see cref="lineY"/> **不是同一根线**。
        ///
        /// <para>判据（`A2104①` 已查实）= 原版场景节点 **`HandLimitArea`**：实况 dump = `pos(0,300)`、
        /// `anchor = pivot = (0.5, **0**)`、根 `Canvas` 高度 1080 ⇒ **底边上方 300 设计 px**
        /// ⇒ 归一化 `300 / 1080` = **0.2777778**。
        /// ⚠️ 旧记录里的「779」是**距顶**读法，与 `300 距底` **本来就是同一根线**（`1080 − 300 = 780 ≈ 779`）。</para>
        ///
        /// <para>**唯一消费点** = `Hand/CardInteraction.UpdateDrag` 那道「指针在不在手牌带」的守卫
        /// （那个守卫**本身**是我们自加的 —— 原版那条 y 判据守的是「要不要试着把手牌打到场上」，
        /// 见那个方法体里的长注释）。⛔ **别拿它去算棋盘行**（那是 `lineY` 的活）。</para></summary>
        public const float HandLimitY = 300f / 1080f;

        [Tooltip("相邻格位的间距（归一化宽度）—— 原版实测 149.3 px / 1920")]
        public float spacing = 0.0778f;

        [Tooltip("敌方那一行：只表示「这是敌方的棋盘」（行分层 / 取敌我参数）。⚠️ **不再左右翻槽号** —— 原版不镜像，见 ArenaSlots.Position")]
        public bool mirror = false;

        [Tooltip("落位吸附的容差（归一化**宽度**）—— 松手点离格位多近算落上。"
               + "必须 < 半间距（0.0778/2），否则相邻两格的判定区会互相咬")]
        public float snapTolerance = 0.036f;

        [Tooltip("纵向容差（归一化**高度**）—— 和上面那个不是同一个物理尺度（可见宽 17.78 / 高 10），"
               + "所以单独给。要小于「相邻两行之间的距离」和「手牌最上沿到本行」两处，"
               + "否则会把牌吸到别的行上")]
        public float snapToleranceY = 0.09f;

        [Tooltip("落上去的卡缩放 —— 原版实测场卡 137.2 px / 卡设计宽 1.45×108")]
        public float placedScale = DefaultPlacedScale;

        public bool IsWarlord(int slot) { return slot == WarlordSlot; }

        // ==================================================================
        //  「一张卡 = 一段渲染队列」的【场上号段】（🆕 2026-10-12 · `A1672`）
        // ==================================================================
        // 机制（把**这张卡的每一层**盖成同一个号）在 `Core/CardView.cs` 的 `SetQueue` —— **判据只此一处**；
        // 本文件只回答「**哪一格给哪个号**」，与 `Hand/HandLayout.queueTop` 是同一套分工
        // （手牌：`queueTop − top`；场上：本类的 `QueueOf`）。⛔ 别在别处再算一遍号。
        // 调用点只一个 = `Battle/BattleDriver.cs` 的 `SyncBoard`（落位那一刻，排在**这一格所有动作之后**）。
        //
        // 🔴 **整段 ≤ `CardView.QCard`(3000)，而且要压在【手牌段】之下**（`HandLayout.queueTop = 3000`
        //    往下排、下限 `HandLayout.QueueFloor = 2980`）⇒ 顶号取 **2979**：两段不留缝、也不撞号，
        //    于是「从手里拖一张牌掠过场上」时那张牌（手牌段）**恒压在场上的卡之上**。
        //    全仓另有几处按「卡 ≤ 3000」写死（别往上排）：`MarkerQ = 3100`（注释明写「必须大于卡的 3000」）·
        //    `CardWinBox.QEffect = 3020` · `Battle/CardDisplayWindow.QChrome = 3000`（遮罩靠 z 压住手牌）。
        //
        // 🔴 **方向** —— **号大的画在上面**（Transparent 队列里号大的后画、ZWrite 关 ⇒ 压住先画的）：
        //   ① **行**：`mirror == false`（我方那一行 —— 画面下方、离相机近）**整行压住**敌方那一行。
        //      与透视相机里的真实远近同向：`ArenaSlots.PlayerZ = −6.655` 比 `EnemyZ = 1.043` 近。
        //   ② **行内**：越靠**中间**（督军位）越靠前。3D 那条路一行各格 **z 相同**
        //      （`ArenaSlots.Position` 只有 x 随格号变），到相机的距离只差 `|x|`，而相机在正中
        //      （`NxOf(WarlordSlot) = 0.5` = 屏幕中心）⇒「中间那张最近」。督军位另有
        //      `heroExtraOffset` 的 z 偏移（我 −0.42 / 敌 −0.75）**方向也是更近** ⇒ 不冲突。
        //      🔴 **原版场上卡不按格号排序** —— 🔴 **2026-10-23（D2）就地订正（铁律 5）**：
        //      本行原来写「`SetUnitInPlayLayer` 给场上所有卡**同一个** order 10」—— **错的**。
        //      实读 `decomp_full/CardScript__SetUnitInPlayLayer.c`：传的是
        //      `BattleCardUI.SetSortingOrder(**-(isPlayer != 0) & 10**)` ⇒ **玩家 10 / 敌人 0**；
        //      `BattleCardUI__SetSortingOrder.c` 里还有第二句 `transform.SetSiblingIndex(order)`。
        //      而且那个 order 落在 `BattleCardUI.canvas`（= `2DCard` 的 Canvas）上，而场上那张 2D 卡
        //      是**关掉**的（`BattleCardUI__ChangeCardToMinion.c` 只 `ToggleBody3D(true)`；
        //      `ShowBoardObjects(true)` 会 `Card2DController.Toggle(!on)`）⇒ **场上画面实际交给距离**
        //      （`3DBody` 的 SortingGroup 两边都是 Default/0）。
        //      ⇒ 我们这条就是把那个次序**写成确定的号**、逐对同向（玩家行压敌方行、中格压两侧）。
        //
        // ⚠️ **静止时本来不可观测**：2D 那条路九格 `z` 恒 0（全共面，距离排序退化成不确定），
        //    而格距 149.3 px > 卡宽 137.2 px（按设计不重叠）⇒ 这条的价值在**重叠期**
        //    （出牌飞行 / 让位滑动 / 阵亡演出）让「谁压谁」有定义，以及**场上卡身上漏盖章的层
        //    （停在默认 3000）不再随距离乱穿**（那正是 `A1665` 那个「多张卡的文本互相透出」的症状）。
        //    照铁律 11 做，⛔ 别拿「看着没差」当理由。

        /// <summary>场上队列段的**顶号**（= 我方那一行正中间那一格）—— `HandLayout.QueueFloor − 1`
        /// （与手牌那一段**不留缝也不撞号**）。改它要同时看 <see cref="QBoardFloor"/> 与手牌那两个常量；
        /// `Editor/BattleScene.cs` 里有一条交叉断言盯着「场上整段 &lt; 手牌段下限」。</summary>
        public const int QBoardTop = 2979;

        /// <summary>场上队列段的**底号**（= 敌方那一行最外侧那一格）= 顶号 −（2 行 × 9 格 − 1）= 2962。</summary>
        public const int QBoardFloor = QBoardTop - (SlotCount * 2 - 1);

        /// <summary>行内名次（0 = 最靠前、画在最上面）：离督军位越远越靠后，**取值 0..8 且不重**。
        /// 对称的那两格（如槽 3 与槽 5）到相机的距离**完全相等**（相机在正中、两格 ±同一个 x）
        /// ⇒ 3D 那种排序在那两格之间本来就是**平局**、没有判据 ⇒ 这里定一条**纯次序用**的分先后
        /// （左的在前）。它唯一的作用是让 9 格号互不相同：同号的「谁压谁」又变回不确定，正是这条要治的病。
        /// ⚠️ 名次必须**连续**（0..8）：<see cref="QBoardFloor"/> 是按「9 格连续」算出来的。</summary>
        public static int RankInRow(int slot)
        {
            if (slot == WarlordSlot) return 0;
            int k = Mathf.Abs(slot - WarlordSlot);
            return k * 2 - (slot < WarlordSlot ? 1 : 0);
        }

        /// <summary>**这一格上那张卡的渲染队列段号**（`A1672` 唯一的号源）。
        /// `mirror == true` ⇒ 敌方那一行（整行排在我方之下，见上面那段）。</summary>
        public int QueueOf(int slot)
        {
            if (slot < 0 || slot >= SlotCount)      // 不静默失败（铁律：没实现/越界要出声）
            {
                if (!_queueBad)
                {
                    _queueBad = true;
                    Debug.LogError($"[BoardLayout] QueueOf({slot}) 越界（合法 0..{SlotCount - 1}）"
                                 + $" ⇒ 退回顶号 {QBoardTop}。查调用点（`BattleDriver.SyncBoard`）传的格号。");
                }
                return QBoardTop;
            }
            return QBoardTop - ((mirror ? SlotCount : 0) + RankInRow(slot));
        }
        static bool _queueBad;

        // ---- 格位底片（给玩家看「能往哪儿放」）----
        // 底片由这里创建而不是场景里摆 —— 它得跟着 LayoutSpace 的分辨率缩放走，
        // 而且拖拽时要整体变色，放在一处最省事。
        readonly List<Renderer> _markers = new List<Renderer>();
        // 🔴 **2026-10-16（`A2104②`）：`_band`（自加的「槽带」）【已删除】。**
        //   三条一起判它该走：① **原版没有这件** —— 格位分界来自 **3D 地板本身**（本文件 `:187` 自己记的，
        //   出处 `资料/战场还原度_差距清单_0917.md` §一「我们挑的」第 2 条）；② 它的 `BandColor`
        //   本来就是 **(0,0,0,0)**（注释写着「同样不画」）⇒ **一个像素都没画过**；
        //   ③ 本仓既有口径「**⛔ 别建死件**」（`任务/T04` §四 第 5 条同一条）。
        //   ⇒ 一个「原版没有 + 永不可见」的象限，留着只会每次换分辨率白算一遍位置（而且它算的还是
        //     **2D 行线** —— `A2104②` 记的「两处还没走投影」里就是它）。⛔ 别建回来。

        // 有战场背景图之后，底片要**很轻**：原版平时不画格子，只在拖拽时亮起来提示落点。
        // 所以 idle 只是一层「淡阴影」（让两行棋盘看得出是两块区域），拖拽时才明显。
        //
        // 🔴 **2026-09-19：战场变成真 3D 之后，这两层平时**一律不画**（alpha 0）。**
        //    原版的格位分界来自 **3D 地板本身**，静帧上根本没有我们这套「槽带 + 九宫底片」——
        //    `资料/战场还原度_差距清单_0917.md` §一 把它列为「我们挑的」第 2 条（🔴 整屏观感来源之一）。
        //    现在地板是真的了 ⇒ 我们这套叠上去的压暗层就是**多余的自设计**，去掉。
        //    **拖拽高亮保留**（原版也有落点提示，见 `Minion Position Highlight` 那三件套）。
        static readonly Color MarkerIdle = new Color(0f, 0f, 0f, 0f);                // 平时不画
        /// <summary>**落点指示**那一格的颜色 —— 照原版那枚 `Glow_674` 的 `m_Color`
        /// = **(0.40732, 0.78302, 0.14405)**（`SpriteRenderer_1752`，`bundle_scenes_scenes_battlearena1`）。
        /// ⚠️ **α = 0.45 是我们挑的**：原版那枚 `SpriteRenderer` 的 `m_Sprite` **13 个场景全是 null**
        /// （`SetCardShadow` 方法体里也没有一次 `set_sprite`）⇒ 它自己画不出东西、**没有可照抄的透明度**。
        /// 出处与结论 → `资料/待办判据_战场与战斗视图.md` §8b。</summary>
        static readonly Color DropSlotColor = new Color(0.40732f, 0.78302f, 0.14405f, 0.45f);
        /// <summary>落点指示相对**卡**的尺寸比 —— 做成**跟卡一样大**（原来的 1.05 是高了一点点，照抄）。
        /// 🔴 2026-10-01 走过的弯路（别重犯）：先按原版那枚 `Shadow` 的 **3.2×3.2**（比卡宽 1.53×）放大、
        /// 又把落点从卡心挪到「脚底」，**都还是看不见** —— 肉眼看图 + 像素探针确认：
        /// 那枚方块被**拖拽中的卡**整块压住（卡周围 380×340 里只剩 **706 个绿像素**）。
        /// ⇒ 真正的解法是**渲染队列**：见 <see cref="MarkerQ"/>。</summary>
        public const float DropMarkW = 1f, DropMarkH = 1.05f;

        /// <summary>落点指示的**渲染队列** —— 🔴 **必须大于卡的 3000**（原版材质的 `m_CustomRenderQueue = 3000`，
        /// 见 `CardView.cs` 里那几处 `renderQueue = 3000`）。透明队列里同队列是**按到相机的距离**排的，而拖拽中的卡被抬高了
        /// (`dragLiftZ`) ⇒ 它离相机更近、永远后画 ⇒ 同队列的指示**一定被它盖住**。
        /// 取 **3100**：在卡之上、在战斗日志面板（4000）与各式窗口（3600+）之下。
        /// 🔶 自己设计（非原版）：落点指示的渲染队列 `MarkerQ = 3100` · 原版那枚影子画在卡下面、图在这个 build 里是空的，没有可照抄的观感（原版判据拿不到） · TODO(谁定的)｜原注：⚠️ 这条是**我们挑的**（原版那枚影子是画在卡**下面**的，但它的图在这个 build 里是空的、
        /// 没有可照抄的观感）—— 判据与两条弯路 → `资料/已知的坑.md` 那条「断言验得到亮没亮、
        /// 验不到看不看得见」。</summary>
        public const int MarkerQ = 3100;

        public void EnsureMarkers()
        {
            if (_markers.Count == 0) BuildMarkers();
            // 🔴 **2026-10-01：这两层留在默认层（= HUD 相机画）**，**别挂 `ArenaSlots.ArenaLayer`**。
            //   试过挂那一层（以为「在战场空间里就该归战场相机」）—— 结果它被**战场相机**投影到
            //   **左边 600 px 外**（实测：根活动、层对、cullingMask 也含它，就是位置错），
            //   因为它的位置是**按 HUD 平面的映射**算的（见 `PlaceMarkers` 里那段）。
            //   A/B 实拍（`2d_落点指示_单独亮.png`）看得很清楚：它出现在 hover 弹窗右边那条。
            PlaceMarkers();
        }

        void BuildMarkers()
        {
            // 🔴 **2026-10-11（A218）判「不改」**（这一层**故意**保持**裸 `Transform`**，⛔ 别补 `RectTransform`）：
            //    判据 = 原版棋盘那一族挂点**全是裸 `Transform`** —— `bundle_scenes_scenes_battlearena1` 实读：
            //    `Board Center`（go_pid 289）· `MinionArea`(339/344) · `LeftMinionArea` · `RightMinionArea` ·
            //    `HandArea`(318/898) —— 一个 `RectTransform` 都没有（它们活在世界空间里，靠 `localPos`/`scale` 定位）。
            //    本节点下面挂的是 `GameObject.CreatePrimitive(Quad)` 这类**世界空间的 3D 片子**（见下一段）
            //    ⇒ 没有矩形语义，写 `sizeDelta` 只会造一个**没有判据的数**（铁律 3）。
            var root = new GameObject("Slots");
            root.transform.SetParent(transform, false);
            var baseMat = new Material(Shader.Find("Sprites/Default"));

            // 🔴 **2026-10-16：这里原来还建一条「槽带」(`Band`)—— 已删**（理由见 `_markers` 上面那一段：
            //   原版没有 + α 恒 0 + 别建死件）。

            for (int i = 0; i < SlotCount; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = $"Slot_{i}{(i == WarlordSlot ? "_warlord" : "")}";
                q.transform.SetParent(root.transform, false);
                var r = q.GetComponent<MeshRenderer>();
                // ⚠️ 每格一份材质：共用的话 `MarkOccupied(i)` 会把 9 格一起变色（踩过）
                // 🔴 **必须给一张白图**：`Sprites/Default` 是**采样 `_MainTex` × 顶点色 × `_Color`** 的，
                //   `_MainTex` 为空时采出来是**黑的** ⇒ 压在暗场上**什么也看不见**（而 `isVisible` 照样是 `True`、
                //   位置/层/队列/深度**全对**）。2026-10-01 打了七轮就是栽在这 —— 详见 `资料/已知的坑.md`。
                r.sharedMaterial = new Material(baseMat) { color = MarkerIdle };
                r.sharedMaterial.mainTexture = Texture2D.whiteTexture;
                // 🔴 **每格一份材质 ⇒ 改队列是安全的**（同 `DeckRuntime` 那条分层注释）。
                //    不给这一档的话，亮起来的那一格会被**拖拽中的卡**整块盖住（2026-10-01 实测）。
                r.sharedMaterial.renderQueue = MarkerQ;
                UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
                _markers.Add(r);
            }
        }

        /// <summary>
        /// 按**当前**可见宽度重算底片和槽带的位置/尺寸。
        /// ⚠️ 必须能重复调用：`LayoutSpace.VisibleWidth` 跟着宽高比走，
        ///    切分辨率后不重算的话底片还停在上一档的位置上（批处理自检里就是 4:3 建、16:9 拍）。
        /// </summary>
        void PlaceMarkers()
        {
            float s = placedScale * LayoutSpace.Scale;
            for (int i = 0; i < _markers.Count; i++)
            {
                var r = _markers[i];
                if (r == null) continue;
                // 🔴 **2026-10-17：被借走的底片【不许碰】** —— `AttachDropSlotTo` 把它挂到了那张卡的
                //   底下，`localPosition` 的语义是「相对那张卡」（`(0,0,−1.0)`）。本函数是
                //   `EnsureMarkers()`（每帧 `ReanchorHud` 都调）的一部分 ⇒ 不跳过的话，拖拽期间
                //   它会被按**棋盘局部系**重摆一次，亮着的那格当场跳走（真机每帧都会发生）。
                //   ⚠️ 还回来由 `SetDropSlot(-1)` 负责（`_borrowed` 置空后本函数自然会重新摆它）。
                if (_borrowed != null && r.transform == _borrowed) continue;
                // 🔴🔴 **2026-10-16 就地订正（铁律 5）—— 这里原来那段 2026-10-01 的注释【错因记错了】，别照它改回去。**
                //   · **原来写的是**：「`DropTargetWorld` 走 `ScreenToWorld`（相机反投影）⇒ 实测**偏左 600 px** ⇒
                //     正确做法 = `FromPixel`（HUD 平面那把尺子）」。**那两条都不成立**：
                //   · **600 px 的真根因是【层】** —— 当年底片被挂到了 `ArenaLayer`（于是由**透视**战场相机画），
                //     那一处后来修好了，**功被记到了「换算」头上**。
                //   · `LayoutSpace.ScreenToWorld` 用的是 `LayoutSpace.Cam` = **HUD 那台正交相机**
                //     （战斗建场时 `LayoutSpace.Apply(cam)` 传的就是它）⇒「拿战场相机的反投影摆 HUD 上的东西」这个前提**不成立**。
                //   · 而 `FromPixel(msp.x, msp.y)` **两个轴都错**：`FromPixel` 收的是**设计像素 1920×1080、左上原点**
                //     （`Core/LayoutSpace.cs:188-189`），`Camera.WorldToScreenPoint` 给的是**实际像素、左下原点**
                //     ⇒ x 只在 1920 宽下重合、**y 是镜像（任何分辨率都错）**。
                //   ⇒ **正解 = `LayoutSpace.ScreenToWorld`**，也就是 `DropTargetWorld` 那一支（同一个口，别再抄第二份）。
                //   ⚠️ 它在今天的画面上**看不出来**：真亮的那格走 `AttachDropSlotTo` 挂到卡上、其余 alpha=0
                //     ⇒ 所以没人发现；改它**同批配了断言**（`Editor/BattleScene.cs` 的「底片位置」两条）。
                //   判据全文 → `资料/普查产出_1016战斗/W2_PlaceMarkers与督军位判据.md`
                // 🔴🔴 **2026-10-17（W5 · BSC62 `20915`）：这里原来在 `DropTargetWorld(i)` 之后还有一段【重复计算】，
                //   把它的结果整体覆盖掉 —— 那段恒用 `ArenaSlots.CardScale(mirror)`、丢了 `HeroScale`
                //   ⇒ 督军位（槽 4）的底片会与真落位差 ~9px（`BattleDriver.PoseFor` 走
                //   `u.IsWarlord ? HeroScale : CardScale`）。那是**两份判据**（违反「判据只此一处」）
                //   ⇒ **删掉那段**，位置**直调 `DropTargetWorld`**：它内部已按 `ProjectionScale(i)`
                //   （督军 `HeroScale` / 其余 `CardScale`）取卡心，是**唯一判据**。
                //   ⛔ 别把那段抄回来、也别在别处再写一份「卡心投到 HUD 平面」。
                Vector3 markerPos = DropTargetWorld(i);
                r.transform.localPosition = markerPos + new Vector3(0f, 0f, 0.05f);
                // 🔴 **2026-09-20：真 3D 时底片的位置与尺寸都要按「投影」来。**
                //    底片平时不可见（alpha 0），但**拖拽时是亮的**（alpha 0.45）——
                //    卡搬进 3D 之后不跟着走的话，玩家看到的高亮格会停在**上面 150 px 的老位置**，
                //    于是「亮着的那格」和「牌真的会落到的格」指的不是同一处。
                //    尺寸也不能用 `placedScale`：那是在**正交平面**上量的尺子，
                //    透视相机下场卡的屏幕尺寸随行不同（我方 137.2 px / 敌 124.5 px）。
                if (use3D && boardCam != null)
                {
                    float sc = ArenaSlots.CardScale(mirror);
                    var c = ArenaSlots.CardCenter(i, mirror, sc);
                    float halfW = CardView.CardUnitW * sc * 0.5f;
                    float pxW = Mathf.Abs(boardCam.WorldToScreenPoint(c + Vector3.right * halfW).x
                                        - boardCam.WorldToScreenPoint(c - Vector3.right * halfW).x);
                    float pxH = Mathf.Abs(boardCam.WorldToScreenPoint(c + Vector3.up * halfW).y
                                        - boardCam.WorldToScreenPoint(c - Vector3.up * halfW).y);
                    float pxPerUnit = boardCam.pixelWidth / Mathf.Max(0.001f, LayoutSpace.VisibleWidth);
                    r.transform.localScale = new Vector3(pxW * DropMarkW / pxPerUnit,
                                                         pxH * DropMarkH / pxPerUnit, 1f);
                }
                else
                {
                    r.transform.localScale = new Vector3(CardView.Width * DropMarkW * s,
                                                         CardView.Height * DropMarkH * s, 1f);
                }
            }

            // 🔴 **2026-10-16：这里原来还给「槽带」重算位置/尺寸 —— 那件已删**（见 `_markers` 上面那一段）。
        }

        /// <summary>**落点指示**：点亮「它会落的那一格」（`slot &lt; 0` = 全灭），
        /// 并把那一格摆到 `hudWorld`（**玩家指针**在 HUD 平面上的位置）。
        ///
        /// 🔴 **2026-10-01 换掉了原来的 `SetDragHighlight(bool)`** —— 那个是把九格**一起**染绿，
        /// 只回答「哪些格能放」、**不回答「它会落在哪一格」**，而原版没有这种「全亮」：
        /// 它给的是**一格** —— `MinionManager.ReassembleMinionsWhilePlayingUnit` 里那句
        /// `SetCardShadow(GetLeft/RightSlotPos(落点), active:1, rotate:1)`，**那枚影子落在它真正会去的那一格**
        /// （判据 → `资料/待办判据_战场与战斗视图.md` §8b）。颜色改用原版 `Glow_674` 的绿（见上）。
        /// ⚠️ 原版那一步是 `SetActive` **硬切、没有补间**（`MinionManager__SetCardShadow.c` 方法体亲读）⇒ 硬切。
        ///
        /// 🔴🔴 **位置为什么取【被拖那张卡自己的 transform + 它的层】**（2026-10-01 打了六轮才收敛，别推翻）：
        ///   试过**四套**摆法，**每套都错**（每张都拍了实拍 + 像素探针看的）：
        ///   ① `DropTargetWorld`（= `LayoutSpace.ScreenToWorld` 的相机反投影）⇒ 偏左 **600 px**；
        ///   ② `ArenaSlots.CardCenter`（3D 点 + 挂 `ArenaLayer`）⇒ 被战场相机投到别处；
        ///   ③ `ArenaSlots.RootPosition`（名字像「卡根」，其实是「我们卡心」）；
        ///   ④ 「指针的世界坐标 + 站 HUD 平面」⇒ **整块绿盖到了左边的 hover 弹窗上**
        ///      （`2c_落点指示_拖拽中.png`：30963 个绿像素全在弹窗那块）。
        ///   **根因**：卡是 **3D 空间**里的东西（战场相机画），而这枚底片是 `CreatePrimitive` 的 HUD 平面
        ///   物件（默认层）⇒ 任何「屏幕↔世界」换算都**跨了两台相机**，怎么算都对不上。
        ///   ⇒ **不换算了**：调用方把**那张卡自己的 `transform.position`** 和**它的层**递进来 ——
        ///   「同空间 + 同层」⇒ 两台相机里画的必然是同一台、位置必然是同一个点 ✓
        ///   （画序用 <see cref="MarkerQ"/>：比卡的 3000 大 ⇒ 压在卡上面）。
        /// </summary>
        public void AttachDropSlotTo(int slot, Transform host)
        {
            SetDropSlot(slot);
            if (slot < 0 || slot >= _markers.Count) return;
            var r = _markers[slot];
            if (r == null || host == null) return;
            // 🔴 **挂成「被拖那张卡」的子物体** —— 见上面那段（四套坐标换算全错之后的结论）。
            //   卡自己的子物体（比如它的 blob 阴影）**一定跟着卡画**（实拍里看得见）⇒ 这是唯一的保证。
            //   ⚠️ 还回去之前 `PlaceMarkers` 的 `localPosition` 语义会变 ⇒ `SetDropSlot(-1)` 里复位。
            if (_borrowed == null) { _borrowed = r.transform; _borrowedHome = r.transform.parent; }
            r.transform.SetParent(host, false);
            // ⚠️ z 要**足够靠前**：卡自己内部也是按 z 叠层的（卡体 / 卡框 / 立绘 / 文字各自一层），
            //    给 −0.05 可能还压在它前面几层之后 ⇒ 推到 −1.0（远在卡所有层之前）。
            r.transform.localPosition = new Vector3(0f, 0f, -1.0f);
            // 卡本体在它自己的局部系里就是 `CardView.Width × Height` ⇒ 照这个摆正好盖住卡
            r.transform.localScale = new Vector3(CardView.Width, CardView.Height, 1f);
            r.gameObject.layer = host.gameObject.layer;
            _dropAt = host.position;
        }
        Transform _borrowed, _borrowedHome;

        /// <summary>自检用：最近一次 `SetDropSlot(slot, hudWorld)` 摆到的那个世界点（没摆过 = `Vector3.zero`）。</summary>
        public Vector3 DropSlotAt { get { return _dropAt; } }
        Vector3 _dropAt;

        /// <summary>**落点指示**：只点亮「它会落的那一格」（`slot &lt; 0` = 全灭），位置沿用底片自己的排布。
        /// ⚠️ 3D 时的位置**不准**（见上面那条注释）⇒ 拖拽那条路请用带 `hudWorld` 的那个重载。</summary>
        public void SetDropSlot(int slot)
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                var r = _markers[i];
                if (r == null || r.sharedMaterial == null) continue;
                r.sharedMaterial.color = (i == slot) ? DropSlotColor : MarkerIdle;
            }
            // 全灭 = 收摊：把借出去的那一格**还回 `Slots` 根** —— 不还的话它的 `localPosition`
            // 是相对**那张卡**的（`PlaceMarkers` 下次摆位会把它扔到别处）。
            if (slot < 0 && _borrowed != null)
            {
                _borrowed.SetParent(_borrowedHome, false);
                _borrowed = null; _borrowedHome = null;
            }
        }

        /// <summary>某个格位被占了就单独标暗（原型阶段用不到，留给规则引擎接进来后调）。
        /// ⚠️ **全仓目前没有调用者**；落点指示走 `SetDropSlot`（两者都写同一批材质色，别同时用）。</summary>
        public void MarkOccupied(int slot, bool occupied)
        {
            if (slot < 0 || slot >= _markers.Count) return;
            var r = _markers[slot];
            if (r != null && r.sharedMaterial != null)
                r.sharedMaterial.color = occupied ? new Color(0.32f, 0.20f, 0.20f, 1f) : MarkerIdle;
        }

        /// <summary>自检用：第 `i` 格底片**当前的颜色**（盯落点指示用）。</summary>
        public Color SlotMarkerColor(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return (r != null && r.sharedMaterial != null) ? r.sharedMaterial.color : new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>自检用：现在**亮着几格**（判据 = α &gt; 0.001）。落点指示同时只该亮一格。</summary>
        public int LitSlotCount
        {
            get { int n = 0; for (int i = 0; i < _markers.Count; i++) if (SlotMarkerColor(i).a > 0.001f) n++; return n; }
        }

        /// <summary>自检用：亮着的那一格（没有 = −1）。同时亮多格时返回**最小**的那个行号。</summary>
        public int LitSlot
        {
            get { for (int i = 0; i < _markers.Count; i++) if (SlotMarkerColor(i).a > 0.001f) return i; return -1; }
        }

        /// <summary>自检用：那一格的 MeshRenderer（诊断「相机到底画没画到它」用 —— `isVisible` 是 Unity 自己的旗）。</summary>
        public Renderer SlotMarkerRenderer(int i)
        {
            return (i >= 0 && i < _markers.Count) ? _markers[i] : null;
        }

        /// <summary>自检用：某一格那块底片的**渲染队列** —— 盯「它得画在被拖的卡之后」（见 `MarkerQ`）。</summary>
        public int SlotMarkerQueue(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return (r != null && r.sharedMaterial != null) ? r.sharedMaterial.renderQueue : -1;
        }

        /// <summary>自检用：某一格那块底片所在的**层** —— 盯「相机画不画得到它」
        /// （3D 时必须 = `ArenaSlots.ArenaLayer`，见 `EnsureMarkers`）。</summary>
        public int SlotMarkerLayer(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return r != null ? r.gameObject.layer : -1;
        }

        /// <summary>自检用：某一格那块底片的**世界位置**（诊断「它到底落在哪儿」用）。</summary>
        public Vector3 SlotMarkerWorldPos(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return r != null ? r.transform.position : Vector3.zero;
        }

        /// <summary>自检用：那一层底片的根（`Slots`）**活动着没有**。</summary>

        public Vector3 SlotPosition(int slot)
        {
            return LayoutSpace.ToWorld(NxOf(slot), lineY);
        }

        public float NxOf(int slot)
        {
            float d = (slot - WarlordSlot) * spacing;
            return 0.5f + d;                       // 不镜像：敌方槽 0 也在画面左边（判据 → ArenaSlots.Position）
        }

        /// <summary>把世界坐标解析成格位号。离任何格位都太远就返回 false（不合法落点）。</summary>
        public bool TryResolveSlot(Vector3 worldPos, out int slot)
        {
            // 🔴 **2026-09-20：真 3D 那条路单独走一套判据。**
            //    场上的卡站在地面上、由**透视相机**画，屏幕位置和下面那套 2D 行心
            //    **不是一个地方**（实测我方行差 ≈150 px）—— 不改的话「看着落在卡上、实际判到别处」。
            //    拖拽那一路传进来的是**正交平面**里的世界坐标（整个拖拽都活在正交平面里），
            //    所以先把它换回**屏幕点**，再交给 3D 判据（`ArenaSlots.TryResolveSlot`）。
            if (use3D && boardCam != null)
            {
                var n = LayoutSpace.ToNormalized(worldPos);
                var sp = new Vector2(n.x * boardCam.pixelWidth, n.y * boardCam.pixelHeight);
                return ArenaSlots.TryResolveSlot(boardCam, sp, mirror, out slot);
            }

            var nn = LayoutSpace.ToNormalized(worldPos);
            slot = -1;

            // ⚠️ **纵向必须先判**。只比 x 的话，「拖到战场上方的空白处」只要 x 恰好落在某格附近
            //    就会被算成合法落点 —— 实测踩到过：非法落点用例丢在 (0.12, 0.88)，
            //    而槽 0 的 nx 是 0.148，|0.12-0.148|=0.028 < 0.055 就判合法了，
            //    牌直接落到战场上（本该回弹）。7 格时槽 0 在 0.236 恰好躲过，改成 9 格才暴露。
            if (Mathf.Abs(nn.y - lineY) > snapToleranceY) return false;

            float best = float.MaxValue;
            for (int i = 0; i < SlotCount; i++)
            {
                float d = Mathf.Abs(nn.x - NxOf(i));
                if (d < best) { best = d; slot = i; }
            }
            return best <= snapTolerance;
        }

        /// <summary>**真 3D 那条路要用**：这一格的卡**现在画在屏幕上的哪一点**（返回**本物体局部坐标**，
        /// 和 `SlotPosition` 同一个约定 —— 拖拽/回弹/自动对局那几路全都拿它当 `SlotPosition` 用，
        /// 换函数名就够了）。
        /// 没有 3D 战场时就是老那套 `SlotPosition`。判据只有这一处，别在调用点各写一份。</summary>
        public Vector3 DropTargetWorld(int slot)
        {
            if (use3D && boardCam != null)
            {
                float sc = ProjectionScale(slot);
                var sp = boardCam.WorldToScreenPoint(ArenaSlots.CardCenter(slot, mirror, sc));
                if (sp.z > 0f)
                    return transform.InverseTransformPoint(LayoutSpace.ScreenToWorld(new Vector2(sp.x, sp.y)));
            }
            return SlotPosition(slot);
        }

        /// <summary>本口投「`slot` 这一格的卡心」时用的缩放 —— **督军位是 `HeroScale`，别的格是 `CardScale`**。
        ///
        /// <para>🔴 **2026-10-16（卡牌飞行族普查 §二 H）**：原来这里恒用 `CardScale`（我方 0.36 / 敌 0.69），
        /// 而**真落位**走的是 `BattleDriver.PoseFor` 的
        /// `u.IsWarlord ? ArenaSlots.HeroScale(foe) : ArenaSlots.CardScale(foe)`（我方 0.40 / 敌 0.77）
        /// ⇒ **督军位那个投影点偏 ~9.6 px（我方）/ 9.1 px（敌方），且只在 y**。
        /// `A359` 之后伤害飘字的基准也走这个口 ⇒ 影响面比原来大。</para>
        /// <para>⛔ **逆运算 `ArenaSlots.TryResolveSlot` 不用跟着动**：它的判据是 x（与 scale 无关），
        /// 差那 0.05/0.11 世界单位也远小于它的容差。单开这个只读口是**给断言用**的
        /// （没有它，「督军位走的是哪个 scale」这件事在自检里无法区分）。</para></summary>
        public float ProjectionScale(int slot)
        {
            return slot == ArenaSlots.WarlordSlot ? ArenaSlots.HeroScale(mirror)
                                                  : ArenaSlots.CardScale(mirror);
        }

        // ==================================================================
        //  🆕 2026-10-16：「手牌 → 棋盘」那三段里的**第二段要用的口**
        //     （原版 `MinionManager.MoveMinionToConversionPoint` 那条 tween 的**终点**）
        //  ⚠️ 本节是**纯新增** —— 上面 `DropTargetWorld` 一个字节都没动（它的返回值是拖拽落点
        //     /底片/飘字/根特效的判据，不许变）。
        // ==================================================================

        /// <summary>原版 `MinionManager.minionConversionHeight`（**我方**）= **1.0**。
        /// 出处：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json`
        /// （`MinionArea` local z = −6.655 那一份 = 玩家侧；敌方那份是 `_4373.json`，z = +1.043）。
        /// ⚠️ **量纲 = 竞技场单位**（`MinionArea` 及其父链 scale 全是 1），
        /// ⛔ **不是** HUD 平面的 108 px/单位那一套 —— 别拿它去乘像素。
        /// ⚠️ 它们本该和 `ArenaSlots` 里那一批 MinionManager 字段放在一起；本件白名单不含
        /// `Board/ArenaSlots.cs` ⇒ 先落在这里，**判据仍然只此一处**（`MinionConversionHeight`）。</summary>
        public const float PlayerMinionConversionHeight = 1.0f;
        /// <summary>原版 `minionConversionHeight`（**敌方**）= **2.1**（出处同 `PlayerMinionConversionHeight`，
        /// 换成 `MonoBehaviour_4373.json`）。敌行更远、卡更大 ⇒ 抬得更高，两个数各自都对。</summary>
        public const float EnemyMinionConversionHeight = 2.1f;

        /// <summary>本场棋盘的 `minionConversionHeight`（`mirror` 位 = 敌方那一行 ⇒ 取敌方那份）。</summary>
        public float MinionConversionHeight
        {
            get { return mirror ? EnemyMinionConversionHeight : PlayerMinionConversionHeight; }
        }

        /// <summary>**「这一格的卡抬高 `upWorld` 之后」画在屏幕上的哪一点**（返回**本物体局部坐标**，
        /// 与 `DropTargetWorld` 同一个约定）。
        ///
        /// 为什么要它：原版「手牌→棋盘」是**两段移动** —— 先飞到 `槽位 + up × minionConversionHeight`，
        /// 再**垂直落**到槽位（判据 `MinionManager__MoveMinionToConversionPoint.c:41-96` ·
        /// `CardScript._MinionPlayedIntoField_d__314__MoveNext.c`）。第二段的终点就是 `DropTargetWorld`，
        /// 第一段的终点 = 「同一个点，只是抬高 `upWorld`」⇒ 这里就是它。
        /// ⚠️ `upWorld` 是**竞技场单位**（见 `MinionConversionHeight`），不是 HUD 单位。
        /// ⛔ 判据只此一处 —— 别在调用点自己写一次 `WorldToScreenPoint`。
        ///
        /// ⚠️ **非 3D（2D 兜底布局）时逐位退回 `SlotPosition(slot)`**：2D 平面里没有「战场高度」
        /// 这个概念（卡本来就摆在那条行线上），于是 `above == land`、第二段退化成一记直飞 ——
        /// 与 `DropTargetWorld` 的兜底同一个口径（那里的 `return SlotPosition(slot)` 就是这么定的）。
        ///
        /// 🔴 **2026-10-19（`W57` · `T02·补`）：缩放走 `ProjectionScale(slot)`，⛔ 不是恒 `CardScale`。**
        /// 原来这里写死 `ArenaSlots.CardScale(mirror)`，而 `DropTargetWorld` 走 `ProjectionScale`
        /// （督军位 `HeroScale`、其余 `CardScale`）⇒ **督军格（槽 4）的两个口不是同一个点**
        /// （`A359` 普查实测量级 ≈ 9.6 px 我方 / 9.1 px 敌方，只在 y）。
        /// 两段飞行（「抬到槽位正上方」→「垂直落下」）的**起点与终点必须同源**，否则第二段不是垂直落下。
        /// 判据 = 同文件 `ProjectionScale` 的 doc（`:527-541`）+ `DropTargetWorld`（`:515-525`）
        /// + `资料/普查产出_1017/BSC62/W5_PlaceMarkers与卡背.md` §五·2（本笔即那条「顺手发现」的收口）。</summary>
        public Vector3 DropTargetWorldAtHeight(int slot, float upWorld)
        {
            if (use3D && boardCam != null)
            {
                float sc = ProjectionScale(slot);
                // `upWorld` 加在**卡心**上 = 「整张卡原地抬高 upWorld」——
                // 卡是直立站着的（`ArenaSlots` 文件头：世界旋转恒 identity），所以世界 up 就是它的长轴。
                var world = ArenaSlots.CardCenter(slot, mirror, sc) + Vector3.up * upWorld;
                var sp = boardCam.WorldToScreenPoint(world);
                if (sp.z > 0f)
                    return transform.InverseTransformPoint(LayoutSpace.ScreenToWorld(new Vector2(sp.x, sp.y)));
            }
            return SlotPosition(slot);
        }

        /// <summary>这一格上的卡是不是**由透视相机画**（真 3D）。`BattleScene` 建场时置。
        /// ⚠️ 与 `BattleDriver.use3DBoard` 是同一个开关的两个落点，必须一起设。</summary>
        public bool use3D;
        /// <summary>画 3D 战场那台透视相机（`use3D` 时必须有）。</summary>
        public Camera boardCam;

        /// <summary>
        /// 这一格能不能放。**这里只表达「表现层觉得能放」** —— 真值由规则引擎说了算
        /// （`RuleCore.CanPlayCard`，含费用和格位占用）。接上驱动层之后这个方法就退化成查询缓存。
        /// </summary>
        public bool IsSlotFree(int slot) { return slot >= 0 && slot < SlotCount; }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            for (int i = 0; i < SlotCount; i++)
            {
                var p = SlotPosition(i);
                Gizmos.DrawWireCube(p, new Vector3(1.2f, 1.8f, 0.02f));
            }
        }
    }
}
