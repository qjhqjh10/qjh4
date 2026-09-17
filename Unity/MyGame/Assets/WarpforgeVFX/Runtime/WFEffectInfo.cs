// WFEffectInfo.cs — **你自己做的特效**在 prefab 上挂这个，声明「它叫什么 / 该活多久」。
//
// 为什么需要它：原版那 958 个效果的名字与寿命，是从 bundle 里 `AnimFXController` 组件 dump 出来的
// （`数据/游戏数据/animfx_components.json` → `工具/gen_effect_index.py`）。**自制特效没有那份数据**，
// 得有个地方写。
//
// 为什么是「手挂组件」而不是写进某个 json：
//   · 编辑器里点开 prefab 就看得见、手能改（和将来 AnimFX 模块「手挂组件」的形态一致）；
//   · **不会被任何「重新生成」抹掉** —— `effect_index.json` 是每次由 `工具/gen_effect_index.py`
//     重生成的，把自制特效写进去迟早会没，而且不报错（同 `EffectExporter.LoadReport` 那个
//     丢数据的形状，2026-09-17 刚踩过）。
//
// 🔴 **prefab 放哪**：`Assets/CardPresentation/Effects/`（见 `EffectLibraryBuilder.UserPrefabDir`）。
//    **绝不能放进 `Assets/WarpforgeVFX/`** —— 那下面的 `{Materials,Textures,Meshes,Prefabs}`
//    会被 `EffectExporter.ClearGenerated()` **整个删掉重建**，放进去下一次全量重导就没了。
//
// 用法：把 prefab 丢进上面那个目录 → 挂本组件（可选）→ 跑 `EffectLibraryBuilder.Run`（菜单
// Tools > Warpforge > 生成效果库）→ 就可以 `WarpforgeEffectPlayer.Play("你的名字", ...)` 了。
// 验收：`UserEffectTools.Run`（`-executeMethod UserEffectTools.Run`）。
using UnityEngine;

namespace WarpforgeVFX
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Warpforge/特效信息（自制特效用）")]
    public class WFEffectInfo : MonoBehaviour
    {
        [Tooltip("播它时用的名字（= 效果库里的键）。留空 = 用 prefab 的文件名。")]
        public string displayName = "";

        [Tooltip("多久之后自动进收尾。负 = 用测出来的自然时长（不循环时才有）。")]
        public float destroyTime = -1f;

        [Tooltip("收尾时长：停止发射后还留多久、让已飞出的粒子飘完。负 = 用播放器的默认 3 秒。")]
        public float exitDestroyTime = -1f;

        [Tooltip("勾上 = 不让它自己死，由牌局代码调 Kill() 收。循环类（光环 / 常驻）要勾。")]
        public bool preventDestroy;

        [TextArea(2, 4)]
        [Tooltip("给自己看的备注，不进库。")]
        public string notes = "";

        /// <summary>这个名字就是效果库里的键（`Play(name, …)` 用它）。</summary>
        public string EffectName
        {
            get
            {
                if (!string.IsNullOrEmpty(displayName)) return displayName.Trim();
                return gameObject != null ? gameObject.name : name;
            }
        }
    }
}
