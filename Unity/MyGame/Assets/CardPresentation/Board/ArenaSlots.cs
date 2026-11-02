// ArenaSlots.cs — 「已部署的卡」在 **3D 战场**里的落点（2026-09-20）
//
// 为什么单开一个文件：这是**原版的规格**，不是我们挑的 —— 逐值都有出处，
// 而且判据只能有一处（`BoardLayout` 那份是**屏幕空间**的槽位，给命中判定/底片用；
// 真 3D 落点是另一套坐标，两者**不该混写在一个类里**）。
//
// ---- 出处（全部逐值实读，不是推的）----
// · `MinionManager` 挂在战场场景的 `MinionArea` 节点上：
//     玩家 `MonoBehaviour_4372.json`（`MinionArea` local z = **−6.655**）
//     敌方 `MonoBehaviour_4373.json`（`MinionArea` local z = **+1.043**）
//   父链 → `BattleBoardElements`(100, 0, 0)，**链上 scale 全是 1** ⇒ local 就是有效值。
//   （我们建 `Arena3D` 时整体平移了 −100 ⇒ 本文件里的 x/z **直接用**即可。）
// · 落点公式 `MinionManager__FillMinionPositions.c:33-35,55-56`：
//     x = **±((k+1) · MinionSeparation + minionExtraDistanceFromHero)**，**y 被显式写成 0**。
// · 🆕 **督军那一格另有 z 偏移** `MinionManager.heroExtraOffset`（`BattleManager__GetHeroBoardPos.c`，
//   `Vector3` 实读值见下面那两个常量）—— 2026-09-25 补，之前**一条都没实现**。
// · 缩放 `BattleManager__GetUnitSizeInPlay.c:21-29`：普通部队 = `Vector3.one * MinionManager.desiredScale`。
//   取哪个 MinionManager 由 `EntityScript.isPlayer`（卡 +0x40）决定 ⇒ **两边各用自己的那份**。
// · 旋转 `MinionManager__MoveMinionToConversionPoint.c:56-59,66-72` + `CardScript__UpdateMinionInPlayPosition.c:105-118`：
//     进场时 `DOLocalRotate(zero)` / `set_localRotation(identity)`、落位时 `set_rotation(identity)`
//     ⇒ **卡根恒为 identity**（倾斜只存在于 `Card 3D` 那个子节点上）。
//
// 🔴 **「MB 4373 是旧预设」那条已经作废**（2026-09-20）：它和 `CardsHorizontalLayout` 那次
//    是**同一个误判** —— 4372/4373 不是「新的/旧的」，是**玩家/敌方两份**（`isPlayer` 分）。
//    证据：`MinionArea` 的两个实例 local z 一个 −6.655（玩家、近）一个 +1.043（敌、远）；
//    而敌行更远 ⇒ **缩放更大才对**（0.69 vs 0.36），步距也更大（1.53 vs 0.82）。
//    屏幕上一验就自洽：玩家步距 0.82 × 182.14 = **149.3 px**、敌 1.53 × 86.2 = **131.9 px**；
//    玩家卡宽 2.0927 × 0.36 × 182.14 = **137.2 px**、敌 2.0927 × 0.69 × 86.2 = **124.5 px**
//    （182.14 / 86.2 = 两行各自深度的 px/单位 —— 逐值见 `资料/3DBody_原版场上卡体规格.md` §四之三）。
//
// 🔴 **2026-09-20 更正（这条原来写反了，照它会踩两个坑）**：
//    原文写「两行位置与我们对得上（玩家 65.4% vs 原版 708 px / 敌 42.7% vs 466 px）
//    ⇒ 屏幕空间的命中判定不用动」—— **两句都不成立**：
//    · 那两个百分比是在 **`lensShift` 被 Unity 静默忽略**的状态下算的（没开物理相机），
//      接上原版取景算法之后实测是 **我方 58.9% = 636 px · 敌 39.4% = 425 px**
//      （见 `资料/3DBody_原版场上卡体规格.md` §四之五）。
//    · 而且 **`708 / 466` 本身是 `battle.gd` 时代的旧校准值、不是原版字段**
//      （`战斗重建_0827/审查更正清单_0827.md:95` 自标「待核」）—— **别拿它当靶**。
//    · ⇒ **命中判定要跟着走**：现在 `BoardLayout.TryResolveSlot` 在 `use3D` 时
//      改按**透视投影**解（`ArenaSlots.TryResolveSlot`，取最近格），拖拽目标走 `DropTargetWorld`。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版成品局里「一张已部署的卡」在世界空间的落点与缩放。**唯一判据**。</summary>
    public static class ArenaSlots
    {
        /// <summary>战场 3D 那一层的 layer 号（透视相机只画这一层，正交相机把它摘掉）。
        /// 判据只此一处 —— `Editor/BattleScene.cs` 也引用它。</summary>
        public const int ArenaLayer = 8;

        /// <summary>中央格 = 督军位；0 号在 `MinionManager` 里不是小兵槽。</summary>
        public const int WarlordSlot = 4;

        // 玩家侧（`MonoBehaviour_4372.json`，`MinionArea` 在 z=−6.655）
        public const float PlayerPitch = 0.82f;          // MinionSeparation
        public const float PlayerExtra = 0.09f;          // minionExtraDistanceFromHero
        public const float PlayerZ = -6.655f;            // MinionArea local z
        public const float PlayerCardScale = 0.36f;      // desiredScale
        public const float PlayerHeroScale = 0.4f;       // heroScale

        // 敌方侧（`MonoBehaviour_4373.json`，`MinionArea` 在 z=+1.043）
        public const float EnemyPitch = 1.53f;
        // 🔴 **2026-09-25 更正：原来是 0.09、注释自承「没实读到」** —— 实读 `MonoBehaviour_4373.json`
        //    的 `minionExtraDistanceFromHero` = **0.12999999523162842**。照 0.09 摆的话敌行每格偏 0.04
        //    世界单位（敌行 86.2 px/单位 ⇒ ≈3.4 px）。出处见 `项目任务.md` §三 第 12 条 第 3 项。
        public const float EnemyExtra = 0.13f;
        public const float EnemyZ = 1.043f;
        public const float EnemyCardScale = 0.69f;
        public const float EnemyHeroScale = 0.77f;

        // ---- 督军位额外的 z 偏移（原版 `MinionManager.heroExtraOffset`，Vector3）----
        // 出处：`MonoBehaviour_4372.json`（我）= (0, 0, **−0.41999998688697815**) ·
        //       `MonoBehaviour_4373.json`（敌）= (0, 0, **−0.75**)。
        // 用法：`BattleManager__GetHeroBoardPos.c` = `heroExtraOffset` **加在**「督军基准位」上；
        //   而 `PlayerManager__SetupFullHero.c:26-46` 说明督军是 **被挂到 `MinionManager` 底下**的
        //   （`set_parentInternal(hero.transform, minionManager.transform)`）⇒ 那个位置是**局部坐标**。
        //   gizmo 那边 `MinionManager__OnDrawGizmosSelected.c:50-57` 印证同一件事：
        //   它把基准点 `TransformPoint` 到世界再加 `heroExtraOffset` 画线。
        // ⚠️ **两个 MinionArea 的旋转都是单位四元数、scale 都是 1**（`Transform_1309/1314.json`），且父链到根
        //   `1319/1315 → 1301(BattleBoardElements, x=100) → 1300` 全 identity/scale 1 ⇒ 局部 x/z 原样就是世界 x/z。
        // ⚠️ **我们原来一条都没实现** ⇒ 督军比原版**近** 0.42（我）/ 0.75（敌）个世界单位。
        //    ⚠️ **还没做实况复核**：`heroExtraOffset` 里那个「基准点」是个静态量（反编译读不出它到底是不是原点），
        //    这里按「基准 = MinionArea 原点」算 —— 与 `OnDrawGizmosSelected` 把督军画在原点那句是一致的。
        public const float PlayerHeroExtraZ = -0.42f;
        public const float EnemyHeroExtraZ = -0.75f;

        public static float Pitch(bool enemy) { return enemy ? EnemyPitch : PlayerPitch; }
        /// <summary>部队卡的根缩放 —— 原版 `desiredScale`。</summary>
        public static float CardScale(bool enemy) { return enemy ? EnemyCardScale : PlayerCardScale; }
        /// <summary>督军卡（原版 `cardType == 10`）的根缩放 —— 原版 `heroScale`。</summary>
        public static float HeroScale(bool enemy) { return enemy ? EnemyHeroScale : PlayerHeroScale; }

        /// <summary>这个槽在 3D 战场里的世界落点 = 原版 `MinionManager.GetSlotPos` 的那条式子。
        /// ⚠️ **y = `ExtraYOnBoard`**（原版 `SlotPosToBoardPos.c:15-21` 的 `+up×ExtraYOnBoard`；13 场 × 2 实例实读全 0 ⇒ 恒 0）——
        ///    **督军位那一支不吃这一项**：它走原版另一条方法 `BattleManager.GetHeroBoardPos`（`.c:16-28`，只加 `heroExtraOffset`）。
        /// ⚠️ **督军位（<see cref="WarlordSlot"/>）多一个 z 偏移** —— 原版 `MinionManager.heroExtraOffset`（= 上面那两个常量）；其余 8 格没有这一项。</summary>
        public static Vector3 Position(int slot, bool enemy)
        {
            float z = enemy ? EnemyZ : PlayerZ;
            int k = slot - WarlordSlot;
            if (k == 0) return new Vector3(0f, 0f, z + (enemy ? EnemyHeroExtraZ : PlayerHeroExtraZ));   // 督军位在正中
            int side = k > 0 ? 1 : -1;
            int n = ClampSideIndex(slot, Mathf.Abs(k));           // 1..4；越界照原版夹取并出声
            float x = side * (n * Pitch(enemy) + (enemy ? EnemyExtra : PlayerExtra));
            // 🔴 **原版【不镜像】敌方槽位**（2026-10-26 · B8_M 复核 F 后确认）：两个 `MinionArea` 跑**同一段** `Awake`
            //    （`Awake.c:54-56` 左=负 / `:99-101` 右=正；取负 = `bits ^ 0x80000000`，VA `0x1834b2c80` 实读 int32 −2147483648）
            //    ⇒ 两侧 left 恒 −x；两 MinionArea 及父链全 identity/scale 1 ⇒ 局部 −x 即世界 −x。详见 `报告/B8_M_敌方槽位镜像.md`。
            return new Vector3(x, ExtraYOnBoard(enemy), z);
        }

        /// <summary>**卡根**的世界落点 = 地面落点 + 半个卡高 × 缩放。
        ///
        /// 为什么还要加这半格：原版的**卡根就在地面上**（`3DBody` 的 y=0 就是卡底边，
        /// 网格 `Card 3D WH40k` 自身 y 从 0.012 起），而**我们的卡根是「卡中心」**
        /// （`CardView` 那一整套坐标 —— 卡本体 2.0927 × 3.3313 —— 全是中心原点）。
        /// 两种约定差半张卡，补偿掉之后**身体落在同一个世界位置**（原版身体占 y 0.004…0.948，
        /// 我们同样 0…0.948）。⚠️ 判据只此一处，别在调用点各自加。
        /// </summary>
        public static Vector3 RootPosition(int slot, bool enemy, float scale)
        {
            return Position(slot, enemy) + Vector3.up * (CardView.CardUnitH * 0.5f * scale);
        }

        /// <summary>格子数（督军位 + 两侧各 4）—— 与 `BoardSpec.Size` / `BoardLayout.SlotCount` 必须一致，
        /// 由 `BattleScene` 的断言盯着。</summary>
        public const int SlotCount = 9;

        /// <summary>**「身体」在卡单位里占的 y 区间的中点**（`3DBody` 那一套：底边 −1.6550、顶 +0.9683）。
        /// 拖拽提示、投影诊断这些「要指到卡身上」的地方用它。</summary>
        public const float BodyMidOffset = (0.9683f - 1.6550f) * 0.5f;   // = −0.34335

        /// <summary>这一格上**卡心**的世界位置。</summary>
        public static Vector3 CardCenter(int slot, bool enemy, float scale)
        {
            return RootPosition(slot, enemy, scale) + Vector3.up * (BodyMidOffset * scale);
        }

        /// <summary>把**屏幕上的一个点**解析成格位（真 3D 那条路）。
        ///
        /// <para>🔴 **2026-11-02（`B120-W21`）按原版重写** —— 判据 = `MinionManager.GetClosestAvailableSlot`
        /// （可读源 `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:321-390`，
        /// 与 `decomp_full/MinionManager__GetClosestAvailableSlot.c:35-145` 互验一致），四件事逐条：
        ///   ① 候选集 = **每侧「最内空格 + 内圈已占格」**（满侧整条剔除、**不含督军位**）；
        ///   ② 距离 = **2D 欧氏屏幕像素** `sqrt(dx² + dy²)`；
        ///   ③ **没有容差**（只要还有空格，指针在任何位置都返回一个格）；
        ///   ④ 平手留**更早下标**（原版 `best > d` 严格不等；我们按格号升序循环 = 原版下标升序）。
        /// 投影基准 = **地面槽点**（`Position`，y = `ExtraYOnBoard` = 0；原版 `GetSlots` 的 `TransformPoint(x,0,0)`），
        /// ⛔ **不是卡心** —— 卡心比地面高半张卡，透视相机下屏幕 y 差一截。</para>
        /// <para>返回：`true` + 格号（我们 0..8，`ourSlot = 4 + 原版有符号格号`）；`false` **只在两侧都满**时出现
        /// —— 原版同一情形返回 `0`（= 无），语义逐字对上。</para>
        ///
        /// <para>⚠️ 候选集与「最近」的**结构**另由 <see cref="IsCandidate"/> / <see cref="PickNearest"/> 表达，
        /// 断言直接喂屏幕点集、不需要相机（`Editor/B120_W21_Assert.cs` 的 A/B/C 段）。</para>
        ///
        /// <para>🔶 自己设计（非原版）：这里以前那两道容差（`tolY = 两行投影间距 × 0.5` 与 `0.55 × pitchPx`）
        /// **是我们自己挑的**，原版**没有** —— `B120-W21` 已删（留痕用；⛔ 别再写回来，见断言 C 段）。</para>
        ///
        /// <para>真判据在下面那个带「每侧已占数」的重载里；**本签名没给占用数 ⇒ 按「两侧皆空」处理**
        /// （= 原版刚开局那一刻的候选集）。产品路径**必须**走带数的重载 —— ⛔ 别把旧「9 格全算」当默认。</para>
        ///
        /// <para>🔴 **不能用「从相机打射线到地面」那条常规做法** —— `BoardCamera` 的朝向是**单位四元数`
        /// （没有俯仰）**，射线是水平的、**永远打不到地面**（`ray.direction.y == 0`）。</para>
        /// </summary>
        public static bool TryResolveSlot(Camera cam, Vector2 screenPos, bool enemy, out int slot)
        {
            // 未给占用数 ⇒ 原版「两侧皆空」那一档（候选 = 两侧最内空格，即 3 号与 5 号）。
            return TryResolveSlot(cam, screenPos, enemy, 0, 0, out slot);
        }

        static Vector3 Project(Camera cam, Vector3 world) { return cam.WorldToScreenPoint(world); }

        // ==================================================================
        //  原版 `MinionManager` 里与「槽位几何」有关、但 `Position` 那条式子没直接表达的几项
        //  （2026-10-26 · B8_AR）⚠️ **一律追加在类尾**：`Position` 在 :98-110，账按行号引用它。
        // ==================================================================

        /// <summary>原版 `MinionManager.slotsPerSide`（**每侧**几格）—— 序列化实读 **4**；
        /// 但 `Awake` **先用服务器值 `GameplayVariablesData.slotsPerSide` 覆盖 `mm+0x58`、再按它重建**两条槽位数组
        /// （`MinionManager__Awake.c:30-46`）⇒ **序列化的 4 只是被覆盖的回退值**（2026-11-02 订正）。⛔ 别和 <see cref="SlotCount"/>（9 = 4 + 督军 + 4）混用。</summary>
        public const int SlotsPerSide = (SlotCount - 1) / 2;

        /// <summary>原版 `MinionManager.ExtraYOnBoard`（**我方**）= **0**
        /// （`MonoBehaviour_4372.json` 实读；13 个战场 × 2 个实例**逐场实读全是 0**）。
        /// 用法 = `SlotPosToBoardPos` 的 `落点 + Vector3.up × ExtraYOnBoard`
        /// （`MinionManager__SlotPosToBoardPos.c:15-21`）。
        /// <para>🔑 **「那枚静态向量是 `Vector3.up`」的推导链**（不是猜的）：
        /// ① `SlotPosToBoardPos.c:16-20` 读 `Il2CppStaticFields&lt;Vector3&gt;` 的 **+0x18 / +0x1c / +0x20** 三个分量；
        /// ② `GetHeroBoardPos.c:19-27` 与 `GetLeftMostPos.c:54-56` 读的是**同一个 static_fields 的 +0x00 那一枚**，
        ///    而它们语义上是「加一个零」（督军世界位 = `heroExtraOffset` 本身、空数组返回 `Vector3.zero`）
        ///    ⇒ **+0x00 = `Vector3.zero`**；
        /// ③ Unity 的 `Vector3` 静态字段按 **12 字节**一档紧排，顺序 `zero → one → up → down → left → right → forward → back → ±inf`
        ///    ⇒ `+0x0C = one`、**`+0x18 = up`**（独立佐证：ILSpy 对本方法 z 分量注的 `+20` 读出来是 0 ⇒ 排除 `one`/`forward`）。</para></summary>
        public const float PlayerExtraYOnBoard = 0f;

        /// <summary>原版 `ExtraYOnBoard`（**敌方**）= **0**（`MonoBehaviour_4373.json` 实读）。见上。</summary>
        public const float EnemyExtraYOnBoard = 0f;

        /// <summary>这一侧落点要抬多高（原版 `SlotPosToBoardPos` 的那一项）。当前恒 0，但**照原版留着这一跳**。</summary>
        public static float ExtraYOnBoard(bool enemy) { return enemy ? EnemyExtraYOnBoard : PlayerExtraYOnBoard; }

        /// <summary>侧内下标 `n`（1..<see cref="SlotsPerSide"/>）越界时**照原版夹取到最外一格并出声**。
        ///
        /// <para>判据 = `MinionManager__GetLeftSlotPos.c:25-42` / `__GetRightSlotPos.c:25-42`：
        /// 下标 ≥ 数组长度 ⇒ `CustomDebug.LogError("Trying to GetLeftSlotPos with slotNum too high " + slot)`
        /// （原字符串见 `cpp2il/out_cpp2il_dev_ilspy/MinionManager.cs:466 / :507`）**然后夹到 `Length − 1`**；
        /// 负下标经 `uint` 变成极大的数，走**同一条**支（`.c:25` 比的是 uint）。
        /// ⇒ 两侧越界都落在**最外一格**（left 的 last = 最负、right 的 last = 最正）。</para>
        /// <para>我们的 `Position` 把「侧内下标」表达成 `n = |slot − WarlordSlot|` ⇒ 夹到
        /// <see cref="SlotsPerSide"/>（= 最外一格）。`n &lt; 1` 不可达（`slot == WarlordSlot` 已在上面分流），
        /// 留着只为不静默吞掉异常输入。</para></summary>
        static int ClampSideIndex(int slot, int n)
        {
            if (n >= 1 && n <= SlotsPerSide) return n;
            Debug.LogError($"[ArenaSlots] 槽号越界 slot={slot}（侧内下标 {n}，合法 1..{SlotsPerSide}）"
                         + " ⇒ 照原版 `GetLeft/RightSlotPos` 夹到最外一格"
                         + "（原版同一情形打 \"Trying to GetLeftSlotPos with slotNum too high\"，MinionManager.cs:466/507）");
            return n < 1 ? 1 : SlotsPerSide;
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-28（`B89-R4b` · 账 `战斗界面_HUD:713`）
        //  `BattleManager.GetUnitSizeInPlay(cardType, isPlayer)` 的**第三支**
        //  （`decomp_full/BattleManager__GetUnitSizeInPlay.c:1-…` 全文亲读，🥇）：
        //   · `cardType == Minion(0)`  ⇒ `Vector3.one × MinionManager.desiredScale`  → 本类 `CardScale`
        //   · `cardType == Hero(10)`   ⇒ `Vector3.one × MinionManager.heroScale`     → 本类 `HeroScale`
        //   · **其余（非部队、非督军的在场卡）** ⇒
        //       `Vector3.one × globalVars.minionInPlayScale(+0x68) × (isPlayer ? 1.0 : globalVars.enemyUnitsScaleRatio(+0x74))`
        //  🔴 **账 713 原来的框架是错的**（本轮回源订正，铁律 5）：它盯的 `BattleManager.GetUnitSizeRatio(bool)`
        //     在 `decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp` **三份源里都只有定义、0 调用者**
        //     ⇒ 那是**死方法**；「我方 1 / 敌方 0.86」这个语义**只活在**上面那条 `else` 支里。
        //  🔴 **0.86 的出处**：资产真值 `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/GlobalVariables.asset:38`
        //     `enemyUnitsScaleRatio: 0.86`；`VarsGlobal` 的 **C# 默认值是 `0.93`**（`cpp2il/out_il2cpp…/VarsGlobal.cs:52`）
        //     —— 那是**死值、被资产覆盖**，⛔ 别用 0.93。
        //  ⚠️ **今天没有观测面**：`PlayerState.Board` 是 `UnitState[]`，卡池里非部队/非督军的在场卡
        //     （`defence` 39 张 / `enchantment` 2 张）**还没进 Board** ⇒ 这一支接上也不会有画面差异。
        //     仍按铁律 11 建出来（只分先后、不分做不做），验收只能靠**算式断言**。
        //  ⚠️ **接线是别人的活**：真消费者 `BattleDriver.ArenaCardScale` 按 `u.Card.Type` 分派的
        //     ✅ **已接线**（2026-11-02 `B117-G10-07` 现核）：`BattleDriver.ArenaCardScale`（`:22128`）= `!u.Card.IsUnit ? InPlayScale(foe) : CardScale(foe)`。
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `VarsGlobal.minionInPlayScale`（`+0x68`）= **0.75**（`资料/VarsGlobal_原版数值.md` 同表，
        /// 与 `targettingAnimTime 0.5` / `minionDeathTime 0.2` 三条互证）。</summary>
        public const float MinionInPlayScale = 0.75f;

        /// <summary>原版 `VarsGlobal.enemyUnitsScaleRatio`（`+0x74`）= **0.86**（资产真值；
        /// ⚠️ C# 默认值 `0.93` 是**死值**）。</summary>
        public const float EnemyUnitsScaleRatio = 0.86f;

        /// <summary>非部队 / 非督军的**在场卡**的根缩放 —— 原版 `GetUnitSizeInPlay` 的第三支。
        /// <para>`InPlayScale(false) == 0.75f` · `InPlayScale(true) == 0.75f × 0.86f == 0.645f`。</para></summary>
        public static float InPlayScale(bool enemy)
        {
            return MinionInPlayScale * (enemy ? EnemyUnitsScaleRatio : 1f);
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-11-02（执行写手 `B120-W19`）：原版 **层 10 = `BoardElements`**
        //  （⚠️ 一律**追加在类尾**：本文件在账 `战斗界面_HUD:713` 有 A 行落点 `:253`，插进成员之间会整片漂移）
        // ════════════════════════════════════════════════════════════════════════════════════
        /// <summary>原版 **`BoardElements` = 层 10** —— **棋盘上的卡**专用层。
        /// <para>判据（一手）：原版 `ProjectSettings/TagManager.asset` 的 `layers` 索引 **10 = `BoardElements`**；
        /// 原版 `BattleManager.GetTargetInPos` 的射线掩码 = `1 &lt;&lt; LayerMask.NameToLayer("BoardElements")`
        /// （`cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:46450-46495` 明文 + `decomp_full/BattleManager__GetTargetInPos.c:204-265`）。
        /// 原版层 10 上**只有卡** —— 这正是那条射线能「字面吃 `hits[0]`」的前提。</para>
        /// <para>🔴 **和 <see cref="ArenaLayer"/>(8) 的分工（⛔ 别把两个合并，也⛔ 别改 8 的值）**：
        /// `ArenaLayer`(8) = **arena prefab 整棵树**（地面 / 背景 / 灯光 / 半透明道具 / tap 宿主 / VFX 宿主）；
        /// `BoardCardLayer`(10) = **只有卡**。原版就是这么分的。</para>
        /// <para>⚠️ **当前状态（2026-11-02 现读）**：`TagManager.asset` 已把索引 10 命名为 `BoardElements`，
        /// 但**场上卡仍挂在 8**（`BattleDriver` 六处 `SetLayer(ArenaSlots.ArenaLayer)`）⇒ 迁移清单、
        /// 还没迁的项、以及要改的相机掩码 → `资料/完全复刻/报告/B120_W19_层依赖审计.md`；
        /// 自检闸 `Editor/B120_W19_Assert.cs` 的 C 段把**未迁项显式标红**（迁移落地后自动转绿）。</para></summary>
        public const int BoardCardLayer = 10;

        // ════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-11-02（执行写手 `B120-W21` · 账 `B120-Q04c`）：落点解析按原版重写
        //  ⚠️ **一律追加在类尾** —— 本文件在账 `战斗界面_HUD:713` 有 A 行落点（`InPlayScale`），
        //     插进既有成员之间会让那片账整段漂移（判据 → CLAUDE.md 派活纪律 / `check_ledger_points`）。
        //  判据 = `cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:321-390`
        //         ＋ `decomp_full/MinionManager__GetClosestAvailableSlot.c:35-145`（两份互验一致）。
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `GetClosestAvailableSlot` 的**候选集判据**（纯函数，⛔ 不需要相机）。
        ///
        /// <para>原版逐句（`MinionManager.cs:326-342`）：`start = 4 − 左已占数 − 1`（左满了就 `= 4`）、
        /// `end = 4 + 1 + 右已占数`（右满了就 `= 4`），`start >= end ⇒ 返回 0`（无）；
        /// 候选 = 拼接数组 `[GetSlots(-1) 下标 0..3 = 左最外 → 贴督军, GetSlots(1) 下标 0..3 = 贴督军 → 右最外]`
        /// 的 `[start, end)`。逐条翻译到我们的格号（`ourSlot = 4 + 原版有符号格号`，侧内下标 0 = 贴督军）：</para>
        /// <list type="bullet">
        /// <item>**满侧整条剔除**（那一侧已占 `SlotsPerSide` ⇒ 一个候选都没有）；</item>
        /// <item>**该侧最内空格 + 内圈已占格** ⇒ 侧内下标 `idx ∈ [0, 已占数]`（`idx = |slot − 4| − 1`）；</item>
        /// <item>**不含督军位**（4 号格恒 `false`）—— 原版候选里根本没有督军格，返回值 `0` 是「无」不是督军格。</item>
        /// </list>
        /// </summary>
        public static bool IsCandidate(int slot, int occupiedLeft, int occupiedRight)
        {
            if (slot == WarlordSlot || slot < 0 || slot >= SlotCount) return false;   // 督军位 / 越界：不在原版那 8 个候选里
            int side = slot < WarlordSlot ? -1 : 1;
            int count = side < 0 ? occupiedLeft : occupiedRight;
            if (count < 0) count = 0;                  // −1 = 调用方数不出来（见带数重载的 doc）⇒ 该侧按「皆空」
            if (count >= SlotsPerSide) return false;   // 满侧整条剔除（原版那一侧 `start = 4` / `end = 4`）
            int idx = Mathf.Abs(slot - WarlordSlot) - 1;   // 0 = 贴督军、SlotsPerSide−1 = 最外
            return idx <= count;                        // 候选 = 最内空格（idx == count）与它内圈的已占格（idx < count）
        }

        /// <summary>原版 `GetClosestAvailableSlot` 的**取最近**那一跳（纯函数：喂**已投影好的屏幕点**）。
        /// <para>逐句（`MinionManager.cs:359-369`）：`d = sqrt((sp.x−pos.x)² + (sp.y−pos.y)²)` ·
        /// `if (best > d) { best = d; bestIdx = i }` —— **2D 欧氏**、**没有任何容差**、
        /// **严格不等 ⇒ 平手留更早下标**。</para>
        /// <para>⚠️ 循环**必须按格号升序**：原版遍历的是「左半（最外→贴督军）+ 右半（贴督军→最外）」的拼接数组，
        /// 我们的格号 0..8 与它同序（左 0..3 升、右 5..8 升，督军 4 恒非候选）⇒ 平手判据逐字相同。</para>
        /// <param name="slotScreenPts">长度 = <see cref="SlotCount"/>（索引 = 我们的格号；非候选位上放什么都行）。</param>
        /// <returns>命中的格号；`-1` = **一个候选都没有**（原版同一情形返回 `0` = 无）。</returns>
        public static int PickNearest(Vector2[] slotScreenPts, Vector2 screenPos, int occupiedLeft, int occupiedRight)
        {
            if (slotScreenPts == null || slotScreenPts.Length < SlotCount) return -1;
            int bestSlot = -1;
            float bestD = float.PositiveInfinity;
            for (int s = 0; s < SlotCount; s++)
            {
                if (!IsCandidate(s, occupiedLeft, occupiedRight)) continue;
                float dx = slotScreenPts[s].x - screenPos.x;
                float dy = slotScreenPts[s].y - screenPos.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);        // 2D 欧氏（原版 `Math.Sqrt`，只取 x/y）
                if (d < bestD) { bestD = d; bestSlot = s; }     // 严格 < ⇒ 平手留更早下标
            }
            return bestSlot;
        }

        /// <summary>把 <paramref name="slot"/> 的**地面槽点**投到屏幕
        /// （= 原版 `GetSlots` 之后那句 `boardCamera.WorldToScreenPoint(slots[i])` 的输入）。
        /// <para>判据：`GetSlots` 算的是 `transform.TransformPoint(new Vector3(x, 0, 0))`（`MinionManager.cs:149-176`）
        /// ⇒ **y = 0**；我们的等价物 = <see cref="Position"/>（y = `ExtraYOnBoard`，13 场 × 2 实例实读恒 0），
        /// ⛔ **不是 <see cref="CardCenter"/>**（卡心比地面高半张卡 ⇒ 透视相机下屏幕 y 差一截）。</para>
        /// ⚠️ `cam == null` 返回 `Vector2.zero` 只是兜底：**调用方必须先判 null**（产品路径恒非 null）。</summary>
        public static Vector2 GroundScreenPoint(Camera cam, int slot, bool enemy)
        {
            if (cam == null) return Vector2.zero;
            return cam.WorldToScreenPoint(Position(slot, enemy));
        }

        /// <summary>原版 `GetClosestAvailableSlot(Vector2 pos)` 的等价物 —— **带「每侧已占数」的那一支**
        /// （= 产品路径；不带数的那支见上面 `TryResolveSlot` 的 doc）。
        ///
        /// <para><paramref name="occupiedLeft"/> / <paramref name="occupiedRight"/> = **这一块棋盘那一方**
        /// 两侧各已占几格 —— 原版是 `leftMinionsOccupation.Count / rightMinionsOccupation.Count`
        /// （`MinionManager.cs:327/335`）；我们的等价物 = `RuleEngine.BoardSlots.CountOnSide(ps, Left/Right)`
        /// （`Hand/CardInteraction.SideCount` 现数后传进来）。</para>
        /// <para>`-1` = 调用方数不出来（自检夹具 / 没有驱动）⇒ 该侧按「皆空」处理
        /// （⛔ 不静默猜成「满」—— 那会把整侧候选剔光，玩家怎么拖都打不出去）。</para>
        /// <para>⚠️ 只服务 <c>use3D</c> 那条路：2D 兜底档仍是我们自己的阈值判据
        /// （见 `BoardLayout.TryResolveSlot` 里那段 `🔶 自己设计`）。</para>
        /// </summary>
        public static bool TryResolveSlot(Camera cam, Vector2 screenPos, bool enemy,
                                          int occupiedLeft, int occupiedRight, out int slot)
        {
            slot = -1;
            if (cam == null) return false;
            var pts = new Vector2[SlotCount];
            for (int s = 0; s < SlotCount; s++) pts[s] = GroundScreenPoint(cam, s, enemy);
            slot = PickNearest(pts, screenPos, occupiedLeft, occupiedRight);
            return slot >= 0;      // `false` 只在「两侧都满」时出现（原版同一情形返回 0 = 无）
        }
    }
}
