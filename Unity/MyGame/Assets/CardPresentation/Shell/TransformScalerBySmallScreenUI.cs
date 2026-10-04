// TransformScalerBySmallScreenUI.cs — 「Small Screen UI」那一套（**A165 这一件全在这一个文件里**）：
//   ① 开关本体（原版 `GameStaticData.smallScreenUI` + `smallUIChosenManually`）；
//   ② 窗口根上的缩放器（原版 `TransformScalerBySmallScreenUI`，类名照原版）。
//
// ============================ 出处（唯一判据） ============================
// 判据全文 → `资料/普查产出_1006/A154_A155_窗口档位与缩放.md` §② · `资料/阶段二_Shell_原版规格.md` §三 第 5 条
//   （那一节 2026-10-06 **已就地订正**；订正前的「窗口宽度 < 阈值」那半句是**错的**，判据里**没有任何宽度判定**）。
//
//  ① **谁挂它、什么时候** = `GameWindow__Open.c`（反编译逐句）：
//       ```
//       if (GameStaticData.smallScreenUI) {                              // ← 静态 bool，与窗口宽/屏宽【无关】
//           if (!Mathf.Approximately(extraScaleSmallScreen, 1f)) {      // ← 只是「这个值不等于 1」
//               var c = GetComponent<TransformScalerBySmallScreenUI>()
//                       ?? gameObject.AddComponent<TransformScalerBySmallScreenUI>();
//               c.SetScale(extraScaleSmallScreen);
//           }
//       }
//       ```
//     ⇒ **乘在哪一级 = 窗口根 Transform**（组件加在**窗口根 GO** 上，整扇窗一起放大，不是某个内层容器）。
//  ② **组件本体** = `TransformScalerBySmallScreenUI__{SetScale,Initialize,LateUpdate,OnEnable,ctor}.c`：
//       · `SetScale(x)`  : `menuScale = x; Initialize();`
//       · `Initialize()` : `enabled = (menuScale != 1f) && GameStaticData.smallScreenUI`
//                          ⚠️ 这里是**裸 `!=`**（`*(float*)(this+0x20) != 1.0f`）—— 而 `Open()` 那两个判据
//                          用的是 `Mathf.Approximately`。**两处照各自的原文**，别图省事统一成一个。
//       · `LateUpdate()` : 当前 `localScale` 与「上次自己设过的值」**逐分量差的平方和 < 0.0001** ⇒ 直接返回
//                          （这是**防重复乘**的守卫，字面量 `0x1834b2f48 = 0.0001`）；
//                          否则 `localScale = 当前 localScale × menuScale`（逐分量），并**回读**记下这一次的值。
//       · `ctor`         : `menuScale = 1.0f`（`0x3f800000`）；「上次设过的值」是**零初始化**的 Vector3。
//  ③ 🔴 **`extraScaleSmallScreen == 1.0` 的含义是「不覆盖」**：此时 `Open()` **连 `SetScale` 都不调**，
//     **prefab 里烤着的 `menuScale` 原样生效**（组件靠自己的 `OnEnable` → `Initialize()` / `LateUpdate` 起作用）。
//     全库 168 颗该组件带 `menuScale`（1.25×66 · 1.3×39 · 1.1×21 …），**窗口根上带成品的只有 3 扇**：
//       `Alliance Trophy Info Popup`(extra 1.0 / 烤 **1.35**) · `Member Options Panel`(1.0 / 1.35) · `Generic Options Panel`(1.0 / 1.35)
//     ⇒ **只读 `extraScaleSmallScreen` 会把这三扇做错**（我们这边只有 `TrophyInfoPopup` 建了 —— 见它的 `Open()`）。
//  ④ **那个 bool 从哪来**（`GraphicsTab__SmallScreenToggleClick.c`）：设置 → 图形页那颗开关
//     **同时**写 `smallScreenUI`(静态字段区 +0x11c) 与 `smallUIChosenManually`(+0x12e)。
//     全反编译里写 `+0x11c` 的**只有 5 处**：`GameStaticData__.cctor`（=0，**出厂关**）、
//     `PlayerDataManager.Load{PlayerData,StarterData,SharedDefaultValues}`（读存档）、上面那颗开关。
//     🔴 **没有任何一处拿 `Screen.width` / `dpi` / 物理吋去算它**（别想当然加个「小屏才开」）。
//
// 🔴 **两处如实标注（我们挑的，不冒充原版 —— 铁律 3）**：
//   ① **持久化**：原版存的是**玩家存档**（`PlayerDataManager`，服务器那一侧）；我们没有存档系统
//      ⇒ 用 `PlayerPrefs`（同族先例 = `Core/WarpforgeAudio.cs` 的 `MusicVolume`，那句注释写着「存档只存音乐」）。
//      **这是我们挑的**；默认值 0 与原版 `cctor` 的出厂值一致。
//   ② **`ChosenManually` 不落盘**：原版从存档读回来，我们只做「点过就置 1」这一半（**没查清**原版还有谁读它）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 「Small Screen UI」开关 —— 原版 `GameStaticData.smallScreenUI`(+ `smallUIChosenManually`) 的等价物。
    /// <para>🔴 **全工程唯一一处来源**（「两处写同一条规则 = 迟早不一致」）：缩放器、设置窗图像页那颗开关、
    /// 断言都读它，⛔ **别在别处再存一份**。</para>
    /// <para>消费链：设置窗图像页 → <see cref="Set"/> → <see cref="Enabled"/> →
    /// `GameWindow.TryOpen` 的 `ApplySmallScreenScale()` → <see cref="TransformScalerBySmallScreenUI"/>。</para>
    /// </summary>
    public static class SmallScreenUI
    {
        /// <summary>我们的持久化 key（原版走玩家存档 —— 见文件头「我们挑的」①）。</summary>
        public const string PrefKey = "SmallScreenUI";

        /// <summary>🔴 **自检注入点**：true ⇒ <see cref="Set"/> 只改内存、**不写 `PlayerPrefs`**
        /// （本工程规矩：自检不许动玩家的真设置/真存档 —— 同 `SettingsWindow.QualitySetterOverride` 那一族）。</summary>
        public static bool PersistOverride;

        static bool _enabled;

        static SmallScreenUI()
        {
            // 原版 `GameStaticData__.cctor` 把这个字段写成 **0**（出厂关）—— 我们这一层是「存档」的等价物：
            // 没有存档 / 没设过 ⇒ `GetInt(..., 0)` 也是 0。
            _enabled = PlayerPrefs.GetInt(PrefKey, 0) != 0;
        }

        /// <summary>= 原版 `GameStaticData.smallScreenUI`（静态 bool）。**与窗口宽/屏宽无关**。</summary>
        public static bool Enabled { get { return _enabled; } }

        /// <summary>= 原版 `GameStaticData.smallUIChosenManually`：玩家**手动**动过这颗开关
        /// （由 <see cref="Set"/> 置 1）。⚠️ 原版还有谁读它**没查清**（全反编译只有写入点）—— 我们如实只做写入这一半。</summary>
        public static bool ChosenManually { get; private set; }

        /// <summary>= 原版 `GraphicsTab.SmallScreenToggleClick(bool)`：**同时**写那两个字段（逐句实读）。</summary>
        public static void Set(bool on)
        {
            _enabled = on;
            ChosenManually = true;
            if (PersistOverride) return;
            PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>自检用：把内存态放回**出厂值**（`Enabled=false` / `ChosenManually=false`）。
        /// ⛔ **不动 `PlayerPrefs`**（自检跑完玩家真设置照旧）。</summary>
        public static void ResetForTest()
        {
            _enabled = false;
            ChosenManually = false;
        }
    }

    /// <summary>
    /// 窗口根上的小屏缩放器。**类名 / 字段名照原版**（`TransformScalerBySmallScreenUI`）。
    /// 行为逐句照反编译（见本文件头 ②）—— 三条别改：① 乘的是**当前** `localScale`（不是上次那个值）；
    /// ② 守卫是**逐分量差的平方和 &lt; 0.0001**（不是「每分量差 &lt; 0.01」）；③ `Initialize` 用**裸 `!=`**。</summary>
    public class TransformScalerBySmallScreenUI : MonoBehaviour
    {
        /// <summary>原版同名**序列化字段** = **prefab 里烤着的倍数**。
        /// 🔴 `GameWindow.Open()` 只在 `extraScaleSmallScreen != 1` 时**覆盖**它 ⇒ 这里留着 1.35 的那几扇
        /// （窗口根上带成品的 3 个）在小屏下就按 **1.35** 放大（见文件头 ③）。</summary>
        public float menuScale = 1f;

        /// <summary>原版 `lastUsedScale`（字段 @0x24，**零初始化** —— `ctor` 只写了 `menuScale`）。
        /// 只用来判「现在这个 localScale 是不是我上次设的那个」（防重复乘）。</summary>
        Vector3 _lastUsedScale;

        /// <summary>原版 `MenuScale` 属性（只读）。</summary>
        public float MenuScale { get { return menuScale; } }

        /// <summary>= 原版 `SetScale(scaleToUse)`：**运行时覆盖值**（`GameWindow.Open()` 那一支调的）。
        /// ⚠️ 我们的窗是**代码建的**（不是 prefab）：`AddComponent` 那一刻 `OnEnable` 就跑过了，
        /// 而 `menuScale` 是**下一行**才赋值的 ⇒ 赋完值**必须再显式 `Initialize()`**（同族先例：
        /// `WindowsManager.MakeHolder` 里那句「先赋字段**再**注册」）。</summary>
        public void SetScale(float scaleToUse)
        {
            menuScale = scaleToUse;
            Initialize();
        }

        /// <summary>= 原版 `Initialize()`：**`menuScale != 1` 且开关开着**才启用（两条都成立）。
        /// ⚠️ 裸 `!=`（照反编译），不是 `Mathf.Approximately` —— 见文件头 ②。</summary>
        public void Initialize()
        {
            enabled = menuScale != 1f && SmallScreenUI.Enabled;
        }

        void OnEnable() { Initialize(); }

        void LateUpdate() { Tick(); }

        /// <summary>🔴 `LateUpdate` 的**同一段逻辑**单独开出来，**只为自检** —— 批处理**没有帧循环**
        /// （同族先例：「粒子要手动 `Simulate`」`资料/命令速查.md`）。产品路径上只有 `LateUpdate` 调它。
        /// ⚠️ 本函数**不自己判 `enabled`**（Unity 只在启用时调 `LateUpdate`）——
        /// 自检要验「开关关着时不缩放」，请断 `enabled == false`，⛔ 别在这里补一句 `if (!enabled) return`。</summary>
        public void Tick()
        {
            var cur = transform.localScale;
            var d = cur - _lastUsedScale;
            if (d.sqrMagnitude < 0.0001f) return;        // 防重复乘：还是我上次设的那个值 ⇒ 不动（原版字面量 0.0001）
            transform.localScale = new Vector3(cur.x * menuScale, cur.y * menuScale, cur.z * menuScale);
            _lastUsedScale = transform.localScale;       // 原版是**回读** transform 之后再记（不是记算出来的那个）
        }
    }
}
