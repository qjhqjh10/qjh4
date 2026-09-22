// ArenaParticleIndex.cs —— 给建出来的每颗粒子挂上「**它在清单 `particles[]` 里的下标**」。
//
// 🔴 **为什么需要它**（2026-09-22）：
//   诊断开关要能**一次只关掉一个对象**（`资料/战场13场_逐场对账_0920.md` 的坑 ⑧）。
//   可 arena1 里有 **3 个 GameObject 都叫 `SmokeEffect`**（其中两个贴图不同：`smokeysteam` /
//   `SmokePuff01`），而且建出来的是**平铺**层级（每个都 `SetParent(root)`，不还原原版的
//   `Scenario/Particles/Scenario/…` 嵌套）⇒ `WF_HIDE` 的**名字匹配与路径匹配都分不开**
//   —— 实测三条日志打出来是一模一样的字符串，`WF_HIDE` 一次关仨。
//   于是「烟囱那团黑球到底是谁画的」这个问题，靠名字问不出来。
//
// 判据只有一个：**下标 = `arenas/<场>/<场>_manifest.json` 里 `particles[]` 的顺序**
// （`ArenaBuilder.BuildContent` 建的，跳过的对象根本不建、也就没有这个组件）。
// 配合 `工具/arena_particle_audit.py`（它按同一份清单列出每个下标的对象名/路径/参数）
// 就能把「第 21 号」翻译回「原版 `Scenario/Particles/SmokeEffect` 那颗」。
using UnityEngine;

namespace WarpforgeVFX
{
    public class ArenaParticleIndex : MonoBehaviour
    {
        public int index = -1;
    }
}
