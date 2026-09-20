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
        public const float EnemyExtra = 0.09f;           // ⚠️ 这一条**没实读到**敌方的值，按玩家侧取
        public const float EnemyZ = 1.043f;
        public const float EnemyCardScale = 0.69f;
        public const float EnemyHeroScale = 0.77f;

        public static float Pitch(bool enemy) { return enemy ? EnemyPitch : PlayerPitch; }
        /// <summary>部队卡的根缩放 —— 原版 `desiredScale`。</summary>
        public static float CardScale(bool enemy) { return enemy ? EnemyCardScale : PlayerCardScale; }
        /// <summary>督军卡（原版 `cardType == 10`）的根缩放 —— 原版 `heroScale`。</summary>
        public static float HeroScale(bool enemy) { return enemy ? EnemyHeroScale : PlayerHeroScale; }

        /// <summary>这个槽在 3D 战场里的世界落点。
        /// ⚠️ **y 恒为 0**（`ExtraYOnBoard = 0`）—— 卡是**站在地面上**的，不是浮起来的。</summary>
        public static Vector3 Position(int slot, bool enemy)
        {
            float z = enemy ? EnemyZ : PlayerZ;
            int k = slot - WarlordSlot;
            if (k == 0) return new Vector3(0f, 0f, z);           // 督军位在正中
            int side = k > 0 ? 1 : -1;
            int n = Mathf.Abs(k);                                 // 1..4
            float x = side * (n * Pitch(enemy) + (enemy ? EnemyExtra : PlayerExtra));
            // ⚠️ **敌方要镜像**（我们的槽号 → 屏幕左右）。判据沿用 2D 那一份
            //    （`BoardLayout.mirror`，注释里记着原版的 `leftSlotPosNormal` 对敌方是 +x）——
            //    两处**必须是同一个方向**，否则「我打你的槽 3」两边指的不是同一个物理格。
            return new Vector3(enemy ? -x : x, 0f, z);
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
        /// 做法 = 把 9 个格的**卡心**逐个投影到屏幕，取屏幕距离最近的那个。
        /// 🔴 **不能用「从相机打射线到地面」那条常规做法** —— `BoardCamera` 的朝向是**单位四元数
        /// （没有俯仰）**，射线是水平的、**永远打不到地面**（`ray.direction.y == 0`）。
        /// 容差按**实际投影**算，不写死 px：横向 = 相邻两格投影间距的一半多一点，
        /// 纵向 = 两行投影间距的一半。
        /// </summary>
        public static bool TryResolveSlot(Camera cam, Vector2 screenPos, bool enemy, out int slot)
        {
            slot = -1;
            if (cam == null) return false;

            float sc = CardScale(enemy);
            var rowMe = Project(cam, CardCenter(WarlordSlot, enemy, sc));
            var rowFoe = Project(cam, CardCenter(WarlordSlot, !enemy, CardScale(!enemy)));
            float tolY = Mathf.Abs(rowFoe.y - rowMe.y) * 0.5f;
            float pitchPx = Mathf.Abs(Project(cam, CardCenter(WarlordSlot + 1, enemy, sc)).x - rowMe.x);

            int bestS = -1; float bestDx = float.MaxValue;
            for (int s = 0; s < SlotCount; s++)
            {
                var sp = Project(cam, CardCenter(s, enemy, sc));
                if (Mathf.Abs(sp.y - screenPos.y) > tolY) continue;
                float dx = Mathf.Abs(sp.x - screenPos.x);
                if (dx < bestDx) { bestDx = dx; bestS = s; }
            }
            if (bestS < 0 || bestDx > pitchPx * 0.55f) return false;
            slot = bestS;
            return true;
        }

        static Vector3 Project(Camera cam, Vector3 world) { return cam.WorldToScreenPoint(world); }
    }
}
