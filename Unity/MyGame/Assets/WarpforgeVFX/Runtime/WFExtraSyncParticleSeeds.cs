// WFExtraSyncParticleSeeds.cs — 原版 `SyncParticleSeeds` 的等价实现（40 挂载 / 36 件 prefab）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `WarpforgeVFX/Runtime/WFEffectExtras.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`。
//
// ✅ 已复刻（2026-11-04 · B139-G · decomp_full/SyncParticleSeeds__Awake.c:20-45）—— 条目 13-03 订正（铁律 5 保留更正痕迹）：
//    本类原来取 `GetComponentsInChildren<ParticleSystem>(true)` —— **整棵子树**。原版字段
//    `particlesSystems` 是**手挑的一小串**；现读 40 个挂载共 **88 个槽位**（其中 9 个是原版空槽 `fileID: 0`），
//    实际点名 **79 颗**；我们那版覆盖 **435 颗（4.83×）**，29/40 个挂载点是超集。
//    ⇒ 原版只用同一个种子锁「同一件特效里**本该同相位**的那几层」；我们把其余十几层也锁成同一随机序列 ⇒
//      那些层本来各自的随机相位（速度/方向/旋转）被强制同步（**参数没变，随机分布变了**）。
//    ⇒ 现在只同步数据里点名的那几颗（`WFEffectExtra.ps`，由 `工具/gen_effect_extras.py` 从原版
//      `particlesSystems` 数组逐个 PS → GameObject 路径烘出来）。⛔ 别再退回整棵子树。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `SyncParticleSeeds`（40 挂载 / 36 件 prefab）。
    /// 语义：`seed = Random.Range(0,10000)`；**逐「手挑的」粒子系统** —— `playOnAwake` 就先 `Stop()`，
    /// `useAutoRandomSeed = false`、`randomSeed = seed`，`playOnAwake` 再 `Play()`
    /// （`SyncParticleSeeds.cs:8-40`）。⇒ **同一 prefab 里那几层粒子共用同一个随机种子**（层间相位同步）。</summary>
    public class WFExtraSyncParticleSeeds : MonoBehaviour
    {
        /// <summary>真的设过种子的粒子系统数（累计，断言读数）。</summary>
        public static int Synced { get; private set; }
        /// <summary>数据里点名的路径解析失败数（累计；出声，不静默）。</summary>
        public static int PathMisses { get; private set; }
        /// <summary>**旧版超集行为**被启用的次数（`ps == null`，即表里还没有这个字段）—— 必须为 0。</summary>
        public static int LegacySuperset { get; private set; }
        static bool _warnedLegacy;
        public static void ResetDiagnostics() { Synced = 0; PathMisses = 0; LegacySuperset = 0; _warnedLegacy = false; }

        /// <param name="root">特效根（`effect_extras.json` 的节点路径从它起算）；null ⇒ 用本组件所在节点。</param>
        /// <param name="nodes">原版 `particlesSystems` 里每个 PS 所在**节点路径**（与 `WFEffectModule.ResolvePath`
        /// 同一套 `名字#下标` 约定）。**空串 = 原版数组里的空槽**（`fileID: 0`）⇒ 从此处**停止**（原版是 `break`）。
        /// <para>⚠️ `null` = **表里没有这个字段**（旧数据）⇒ 退回旧版整棵子树行为并**出声**（不静默）；
        /// **空数组** = 原版数组确实是空的 ⇒ **一颗都不同步**（那是原版行为，⛔ 别当缺失）。</para></param>
        public void Apply(Transform root, string[] nodes)
        {
            int seed = UnityEngine.Random.Range(0, 10000);

            if (nodes == null)
            {
                LegacySuperset++;
                if (!_warnedLegacy)
                {
                    _warnedLegacy = true;
                    Debug.LogWarning("[WarpforgeVFX] `SyncParticleSeeds` 的 `particlesSystems` 名单**不在表里**" +
                                     "（`Resources/WarpforgeVFX/effect_extras.json` 的 `ps` 字段）⇒ 退回旧版" +
                                     "「同步整棵子树」的**超集**行为。重跑 `工具/gen_effect_extras.py` 生成这份表即可消失。" +
                                     "（原版 40 个挂载只挑 90 颗，我们这版会锁到 435 颗。）");
                }
                SyncAll(GetComponentsInChildren<ParticleSystem>(true), seed);
                return;
            }

            if (nodes.Length == 0) return;            // 原版数组为空 ⇒ 什么都不做

            var rootT = root != null ? root : transform;
            for (int i = 0; i < nodes.Length; i++)
            {
                // 🔴 **空槽 = 原版的 `break`，不是 `continue`**：`SyncParticleSeeds__Awake.c:22-25`
                //    `if (*(longlong *)(lVar1 + lVar2) == 0) break;` —— 数组里遇到 null 元素**整条循环停掉**。
                //    生成器把原版 `fileID: 0` 的空槽写成空串，保持数组长度与语义（现读 9 个空槽 / 88 槽）。
                if (string.IsNullOrEmpty(nodes[i])) break;
                var t = WFEffectModule.ResolvePath(rootT, nodes[i], "SyncParticleSeeds");
                var ps = t != null ? t.GetComponent<ParticleSystem>() : null;
                if (ps == null)
                {
                    PathMisses++;
                    Debug.LogWarning($"[WarpforgeVFX] 《{name}》的 `SyncParticleSeeds` 点名了节点 `{nodes[i]}`，" +
                                     "但在 prefab 里解析不出那一颗 `ParticleSystem` ⇒ **这一颗没同步种子**（宁可认输，不猜）。");
                    continue;
                }
                SyncOne(ps, seed);
            }
        }

        void SyncAll(ParticleSystem[] all, int seed)
        {
            for (int i = 0; i < all.Length; i++) if (all[i] != null) SyncOne(all[i], seed);
        }

        void SyncOne(ParticleSystem ps, int seed)
        {
            bool playOnAwake = ps.main.playOnAwake;
            if (playOnAwake) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // ⚠️ `useAutoRandomSeed` / `randomSeed` 是 **ParticleSystem 自己的属性**，
            //    不在 `MainModule` 里（写进 `main` 编不过 —— 2026-10-25 踩过）。
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)seed;
            if (playOnAwake) ps.Play(true);
            Synced++;
        }
    }
}
