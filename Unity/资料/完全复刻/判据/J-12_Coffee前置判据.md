# J-12 · `Coffee.UIEffect` 前置判据（账 455）

> 2026-10-27 · 执行代理 `E2UDW` · **只读调查**（⛔ 未改任何 `.cs` / 资产 / 主表 / git；⛔ 未跑 Unity）
> 派活包 → `判据/B9_特效C_派活包.md` §2 J-12 · 施工单 → `判据/B9_特效C_施工单.tsv` 行 455

## 结论（一句话）

**能做，但要用户拍板**：Coffee 是**三个第三方 dll**（不是源码），本机**有 IL2CPP 重建产物**可直接参考但不是发行包；
游戏侧包装类只做「开关 + 播一次」两件事。⇒ 两条路：**(A) 把 dll 搬进工程**（省事、但引入第三方二进制）、
**(B) 只复刻「开关 + 播一次」的外壳、视觉本体另做**（不引入 dll，但要自己写 shader/效果）。

---

## ① Coffee 是什么？在哪？（证据）

- **是 dll，不是源码**：`d:/2/tools/il2cpp_out/dump.cs` 的 image 表三行 ——
  `// Image 40: Coffee.UIEffect.dll - 20305` · `// Image 68: Coffee.UIParticle.dll - 21783` · `// Image 127: Coffee.UIEffect.R.dll - 22640`
  ⇒ 原版**以三个独立程序集**引用它（`R` 那个是 "UIEffect Reverse/高级版" 一类的附加包）。
- **游戏侧只有一个包装类**：`public class UIEffectTogglerCoffeeEffects : UIEffectTogglerBase`（`dump.cs:114051`），
  字段**只有一个**：`[SerializeField] private UIEffect[] uiEffects; // 0x28`。
- **`Warpforge_code/Scripts/Assembly-CSharp/` 里只有那个包装类的桩**（`UIEffectTogglerCoffeeEffects.cs`）——
  即 Coffee 本体**不在** Assembly-CSharp 源码树里（它是外部 dll）。
- **decomp_full 里 8 个方法体**（`ls d:/2/tools/decomp_full | grep -i coffee` = 8）：
  · 游戏侧 4 个：`UIEffectTogglerCoffeeEffects__{Toggle, DoUpdate, AnimationFinished, .ctor}.c`
  · Coffee 侧 4 个：`Coffee.UIEffects.UIEffect_AvailableFilters__{ctor, Start, SetEnableTweeners, SetEnableUIEffects}.c`
  （只反编译到这几个 —— 那 4 个是 il2cpp 里被内联/保留下来的少数，**Coffee 的其余方法体没进 `decomp_full`**）
- **本机 dll 位置**：
  · `d:/2/unity_run_ref/MelonLoader/Il2CppAssemblies/Il2CppCoffee.UIEffect.dll`（+ `Il2CppCoffee.UIEffect.R.dll`、`Il2CppCoffee.UIParticle.dll`）
  · `d:/2/unity_run_ref/MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/cpp2il_out/Coffee.UIEffect.dll`（+ `Coffee.UIEffect.R.dll`、`Coffee.UIParticle.dll`）
  ⚠️ **两处都是 IL2CPP 重建产物**（MelonLoader 从 `GameAssembly.dll` 还原的），**不是原作者发布的 dll**。

## ② 那 8 个方法各做什么？对应我们哪个系统？

### 游戏侧 `UIEffectTogglerCoffeeEffects`（🥇 逐条，全在本机）
| 方法 | 判据 | 做什么 |
|---|---|---|
| `Toggle(bool option, float duration = 0.3)` | RVA `0x86F2E0`，`decomp_full/UIEffectTogglerCoffeeEffects__Toggle.c:20-45` | `targetStateEnabled(+0x21) = option`；遍历 `uiEffects(+0x28)`：`if (uiEffects[i] != null) { Behaviour.enabled = option; if (option) { var tw = uiEffects[i].GetComponent<UIEffectTweener>(); if (tw != null) tw.Play(); } }` ⇒ **= 开/关 + 开时播一次**。`duration` **没用**（没有补间）|
| `DoUpdate(float, float)` | `dump.cs` RVA = **`0x4B33B0`** | **空方法**（`0x4B33B0` = 全局共享空桩，机器码 `c2 00 00` = `ret 0`）⇒ 本类**不参与补间**（`UIEffectTogglerController` 驱动的那套对它无效）|
| `AnimationFinished()` | `dump.cs` RVA = **`0x4B33B0`** | **空方法**（同上）|
| `.ctor()` | `decomp_full/…__.ctor.c`（首行是 `SupportLogger___ctor` ⇒ ICF 折叠，体仍可读）| `this+0x20 = 1`（= 基类 `deactivateOnDisable` 默认 true）+ `Behaviour..ctor()` |

### Coffee 侧（只有 4 个能读到体）
`Coffee.UIEffects.UIEffect.AvailableFilters` 的 `ctor` / `Start` / `SetEnableTweeners` / `SetEnableUIEffects` ——
属于 **Coffee 内部**（跟「可用滤镜（灰度/棕褐/…）」的开关有关），**不是游戏侧调用面**。

### 对我们
- 我们的对应物 = `CardPresentation/Effects/UIEffectTogglerBase.cs` / `UIEffectTogglerImage.cs` / `UIEffectCanvasGroupToggler.cs` 那一族（**同一个基类** `UIEffectTogglerBase`）。
- 缺口只在「`uiEffects` 那一格要装什么」—— 原版装的是 **Coffee 的 `UIEffect` 组件**（一个挂在 uGUI `Graphic` 上的材质滤镜），
  而我们的 UI 是 `ImageQuad`（**不是 `Graphic`**）⇒ **Coffee 的 uGUI 版在我们这里装不上去**（派活包 §2 J-12 原话已写：uGUI 版只服务 Battle 那批）。

## ③ 能不能搬？

| 项 | 结论 | 证据 |
|---|---|---|
| dll 在不在本地 | ✅ 在（两处） | 见 ① 最后两条 |
| 是不是**发行版** | ❌ **不是** —— 是 MelonLoader 从 il2cpp 重建的 | 路径在 `MelonLoader/Il2CppAssemblies`、`…/Cpp2IL/cpp2il_out` |
| 依赖 | `UnityEngine` + `UnityEngine.UI`（`UIEffect` 是 `Graphic` 上的 `IMaterialModifier`/`IMeshModifier`） | 派活包 §2 J-12 + `UIEffectTogglerCoffeeEffects` 的 `GetComponent<UIEffectTweener>` |
| license | 🔴 **查不到**：本地两个目录里**没有** `LICENSE`/`NOTICE` 文件（我 ls 过这两个目录下的 Coffee 文件）| —— |

⇒ **搬 = 把「IL2CPP 重建出来的第三方二进制」放进工程**。技术上可行（我们的 shader 也是运行时从 bundle 取的），
但它**不是原作者发布物**，且本机判不了 license ⇒ 这条**必须用户拍板**。
（用户已允许「原版美术/语音照用」，但那是**游戏自己的资产**；第三方 dll 的再分发是另一件事 ⇒ 我只报不下结论。）

## ④ 搬的话，最小路径（可执行步骤）

1. **`Assets/ThirdParty/Coffee.UIEffect/`** 新建目录，放 `Il2CppCoffee.UIEffect.dll`（+ `UIEffectTweener` 所在同一程序集即可；
   `Coffee.UIParticle` 本域**用不到**，先不放）。
   ⚠️ 需在 Inspector 里把该 dll 的 **platform 勾上**（Editor + Standalone），否则编译期引用不到。
2. **`Assembly-CSharp` 侧直接 `using Coffee.UIEffects;`**（本工程无 asmdef ⇒ 引用 dll 即可，不必改 asmdef）。
3. **shader 载体**：`UIEffect` 用的是它自己的 shader（`UIEffect` 系列）。⚠️ **bundle 里的 shader 落不了工程资产，只能运行时加载**
   ⇒ 要么把那批 shader 也搬成工程资产，要么走我们已有的运行时取 shader 的路（`WarpforgeShaderMap.TryResolve` 那条）。
   **这一步我没核**：Coffee 的那几个 shader 名在 `assets_full` 里叫什么、有几个（见 ⑤-1）。
4. **7 个实例的接线**：`Shell/*` 那 7 处各挂 `UIEffectTogglerCoffeeEffects` + 填 `uiEffects[]`（派活包 §8-4：逐实例指向**未全读**）。
5. **我们的等价物**：`CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs`（**本波 ✅ 目录**）——
   把 `Toggle` 的语义照抄（`enabled = option` + 开时 `Play()`），`DoUpdate`/`AnimationFinished` **留空**（原版就是空的，⛔ 别自己加补间）。

## ⑤ 不搬的话，能不能等价替代？

**能替代「开关 + 播一次」这层外壳，替代不了「滤镜本体」。**
- 外壳：`Toggle` 只有两句语义（`enabled = option`、`option == true` 时 `UIEffectTweener.Play()`）⇒ 用我们自己的
  `UIEffectTogglerImage` 那种 `fadeTargets` + 自写 `Play()` 完全能表达，**⛔ 不改行为**（原版 DoUpdate 是空的）。
- 本体：`UIEffect` 是「uGUI 材质滤镜（灰阶/亮度/色调/模糊…）」的现成实现。要在 `ImageQuad` 上重现，得自己写等价 shader
  —— 那是**新的一件美术/渲染活**，不是接线。

⇒ **建议**：先用 **(B) 只做外壳**（把 `UIEffectTogglerCoffeeEffects` 写成 `CardPresentation/Effects/` 下的等价类，
语义照 ②-Toggle），**视觉本体暂缺并出声**；要不要搬 dll 由用户拍板（拍板后再走 ④）。

## ⑥ 没查清（写「查不到 + 搜过哪里」）

1. **Coffee 那几个 shader 在 `assets_full` 里叫什么、有几个** —— 本轮没查（只查了 dll 与类型）。
   搜法：`grep -rl "Coffee" d:/2/新解包资源/assets_full/*/Shader/` / 按 `UIEffect` 命名的 Shader JSON。
2. **7 个实例的逐实例 `uiEffects[]` 指向与触发时机** —— 未读（派活包 §8-4 同结论）。
3. **license** —— 本地两处 Coffee 目录下**没有** LICENSE/NOTICE（我 ls 过）；查不到就写查不到。
4. `Coffee.UIEffect.R`（123 号 image）与原版的差别 —— 未查（本域用不到 `.R` 的概率高，但没证据）。

## ⑦ 若要我做（等拍板）

- **B 路（不搬 dll）**：`CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` 写「开关 + 开时 Play()」等价类，
  `DoUpdate`/`AnimationFinished` 保持空（原版空桩），视觉本体缺**出声**。→ 我可在 1 轮内做完 + typecheck 0/0。
- **A 路（搬 dll）**：需要用户先拍板 license/来源；之后按 ④ 走，第 3 步（shader 载体）要先解 ⑥-1。
