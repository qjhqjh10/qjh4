// WFExtraParticleFloorCollider.cs — 原版 `ParticleFindFloorCollider` 的等价实现（9 挂载 / 5 件 prefab）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `WarpforgeVFX/Runtime/WFEffectExtras.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`。
//
// ⚠️ 本块**逐字搬**（只把缩进重新对齐到 namespace 里）。表 `effect_extras.json` 的读取与
//    `AttachAll` 装配仍在 `WFEffectExtras.cs`；`IWFEffectExtraTick` / `WFEffectExtra` /
//    `WFSoundSlot` 也在那里（同一 `WarpforgeVFX` 命名空间）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `ParticleFindFloorCollider`（9 挂载 / 5 件 prefab）：
    /// 先 LogError「deprecated, use AnimFxModules instead」；按 tag `FloorPositionReference` 找地面，
    /// 找到就 `ps.collision.SetPlane(0, floor)`，找不到再 LogError
    /// （`ParticleFindFloorCollider.cs:10-35`）。
    /// <para>✅ **2026-10-25 订正**：原来这里写「我们的碰撞平面没打这个 tag」—— 已由代理 A 补上，
    /// 但**只补在唯一那块法线朝上的平面上**（`i == 0`）。原版 `ParticleFindFloorCollider__Start.c:20-45`
    /// 的 `floorCollider` 是**静态缓存**、`FindGameObjectWithTag` 只取**第一个匹配**；另外 6 块法线是
    /// **±Z（竖直的墙）** ⇒ 全打会让碰撞面随机变成一面墙。工程 `TagManager.asset:7` 已注册该 tag。</para>
    /// ⚠️ 取的是 `floor.transform`（**位置**）而不是 `Collider` —— `CollisionModule.SetPlane(int, Transform)` 只吃 Transform。</summary>
    public class WFExtraParticleFloorCollider : MonoBehaviour
    {
        public void Apply()
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps == null) ps = GetComponentInChildren<ParticleSystem>(true);
            if (ps == null) return;
            Debug.LogError("[ERROR] ParticleFindFloorCollider is deprecated, use AnimFxModules instead");
            GameObject floor = null;
            try { floor = GameObject.FindGameObjectWithTag("FloorPositionReference"); }
            catch (UnityException) { /* tag 没在 TagManager 里注册 —— 下面统一报 */ }
            if (floor == null)
            {
                Debug.LogError("[ERROR] ParticleFindFloorCollider: Can't find a GameObject tagged "
                             + "`FloorPositionReference` ⇒ 粒子碰撞平面**没有钉到地面**。"
                             + "（我们的碰撞平面建在 `BattleDriver`，但**没打这个 tag** —— 见 "
                             + "`WFEffectExtras.cs` 该类注释。）");
                return;
            }
            var col = ps.collision;
            col.enabled = true;
            col.SetPlane(0, floor.transform);
        }
    }
}
