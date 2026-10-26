// WFExtraSyncParticleSeeds.cs — 原版 `SyncParticleSeeds` 的等价实现（40 挂载 / 36 件 prefab）
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
    /// <summary>原版 `SyncParticleSeeds`（40 挂载 / 36 件 prefab）。
    /// 语义：`seed = Random.Range(0,10000)`；逐粒子系统 —— `playOnAwake` 就先 `Stop()`，
    /// `useAutoRandomSeed = false`、`randomSeed = seed`，`playOnAwake` 再 `Play()`
    /// （`SyncParticleSeeds.cs:8-40`）。⇒ **同一 prefab 里多层粒子共用同一个随机种子**（层间相位同步）。
    /// ⚠️ 原版字段 `particlesSystems` 是一份**手挑的**列表；我们取「本节点子树里的全部 ParticleSystem」
    /// （超集）。差异：原版只同步它挑的那几个；我们全同步 ⇒ 对**同一 prefab** 而言，多同步的那些本来也是
    /// 同一次实例化的，视觉上只会更同步，不会更差。</summary>
    public class WFExtraSyncParticleSeeds : MonoBehaviour
    {
        public void Apply()
        {
            int seed = UnityEngine.Random.Range(0, 10000);
            var all = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var ps = all[i];
                if (ps == null) continue;
                bool playOnAwake = ps.main.playOnAwake;
                if (playOnAwake) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ⚠️ `useAutoRandomSeed` / `randomSeed` 是 **ParticleSystem 自己的属性**，
                //    不在 `MainModule` 里（写进 `main` 编不过 —— 2026-10-25 踩过）。
                ps.useAutoRandomSeed = false;
                ps.randomSeed = (uint)seed;
                if (playOnAwake) ps.Play(true);
            }
        }
    }
}
