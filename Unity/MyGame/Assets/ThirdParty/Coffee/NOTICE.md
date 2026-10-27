# Coffee.UIEffect（第三方 dll）—— 来源、许可、已知缺陷

> 2026-10-27 · 执行代理 `B9_W5COF` · 用户当日批准「要搬」
> 判据 → `d:/4/Unity/资料/完全复刻/判据/J-12_Coffee前置判据.md`
> 搬迁取证 → `d:/4/Unity/资料/完全复刻/报告/B9_W5COF_Coffee搬运.md`

## 1. 这个文件是什么

`Coffee.UIEffect.dll` —— 原版《Warpforge》引用的第三方 uGUI 材质滤镜包 **Coffee UI Effect**
（作者 mob-sakai / Coffee；Asset Store 商品，**非** MIT 开源版）。
原版以三个独立程序集引用它（`d:/2/tools/il2cpp_out/dump.cs` 的 image 表）：

| Image | 程序集 | TypeDefIndex 区间 | 本工程是否需要 |
|---|---|---|---|
| 40 | `Coffee.UIEffect.dll` | 20305–20417 | ✅ 需要（`UIEffect` 20378 / `UIEffectTweener` 20392 在这里） |
| 68 | `Coffee.UIParticle.dll` | 21783–21808 | ❌ 不需要（全库无引用点） |
| 127 | `Coffee.UIEffect.R.dll` | 22640–22641 | ❌ 不需要（两个 index 里只有 `<Module>` 和一个空接口壳） |

## 2. 来源（⚠️ 不是发行包）

| 项 | 值 |
|---|---|
| 本文件实际来源 | `d:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll`（329,216 B） |
| 怎么来的 | **Cpp2IL 从 `GameAssembly.dll` 的 IL2CPP 元数据重建**出来的 .NET 程序集 |
| 生成时间 | 2026-10-16（`out_cpp2il_dev` 那一批） |
| 同源候选（本机共 5 份，只有这份有真方法体） | `…/MelonLoader/Il2CppAssemblies/Il2CppCoffee.UIEffect.dll`（**Il2CppInterop 代理，引用 `Il2Cppmscorlib`，本工程不可用**）· `…/Cpp2IL/cpp2il_out/`（156 KB）· `d:/2/tools/cpp2il/out_cpp2il/`（53 KB，**方法体全空**）· `d:/2/tools/assetripper_out/AuxiliaryFiles/GameAssemblies/`（61 KB，**方法体全空**，如 `UIEffect.OnEnable(){}`） |
| 程序集引用 | `mscorlib` · `UnityEngine` · `UnityEngine.CoreModule` · `UnityEngine.UI` · `Unity.TextMeshPro` —— **全部能在本工程解析**（csc 实测 0 错） |
| 本文件 SHA/大小 | 329,216 B |

## 3. 许可 / 合规（如实写）

- 本机**两处** Coffee 目录下**都没有** `LICENSE` / `NOTICE`（`find d:/2/unity_run_ref/MelonLoader -iname "*licen*" -o -iname "*notice*"` 只命中 MelonLoader 自己的两份）。
- 这是 **Asset Store 上架商品**，不是开源包 ⇒ **默认不可再分发**。
- 本项目是**个人学习用途**，用户 2026-09-18 已取消「原版美术/语音/文本」的自我约束；**第三方 dll 是另一件事**，
  但用户 **2026-10-27 明确批准「要搬」** ⇒ 按个人学习用途保留。
- ⛔ **若将来对外发布/上传，必须先重新评估这一条**（连同 `Resources/Art/` 那条一起）。

## 4. 🔴 已知缺陷（⚠️ 这份 dll **不是能跑的实现**）

Cpp2IL 的 IL 恢复在这份产物上**大面积失败**（实测，不是推测）：

| # | 缺陷 | 判据 |
|---|---|---|
| 1 | **自定义特性全被剥掉** | 在 dll 字节里搜 `SerializeField` / `ExecuteAlways` / `DisallowMultipleComponent` / `RequireComponent` / `Serializable` / `AddComponentMenu` —— **全部 0 命中**；只有 `Cpp2ILHelpers` / `Cpp2ILInjected` 两个字符串。⇒ `UIEffect` 那 81 个 `protected/private` 字段**不会被 Unity 序列化**（Inspector 里配不了），`[ExecuteAlways]` / `[RequireComponent]` 也全丢 |
| 2 | **自启钩子随之失效** | `ImageProxy.InitializeOnLoad` / `TmpProxy.InitializeOnLoad` / `TmpProxy.RuntimeInitializeOnLoadMethod` 本体在，但**没有 `[RuntimeInitializeOnLoadMethod]` 特性** ⇒ 永不被调 ⇒ `GraphicProxy.Register(...)` 不执行 ⇒ Coffee 认不出 `Image`/`TMP_Text` 类型。**副作用（相对于风险）是这份 dll 不会在开机时自动跑坏代码** |
| 3 | **IL 类型错乱** | ILSpy 全量反编译出 **88 个文件**，其中 **1,430 处** `Cpp2ILHelpers.NoteDecompilerIssue(...)`（callee 只能靠猜），外加 **~800 条** `//IL_xxxx: Expected Obj/I/F4/VT …` 栈类型错乱注记 |
| 4 | **核心方法语义已坏** | `UIEffectTweener.Play()` 反编译出来带 8 条类型错乱；`ImageProxy.IsValid` 变成 `if (0 == (nint)typeof(Image)) return true;`；`UIExtraCallbacks.InitializeOnLoad` 把 `Canvas.willRenderCanvases += x` 变成 `-= x` 且两个分支都置 `false` |

⇒ **结论：能编译、能被引用、能被 new，但跑起来不可信。**
`UIEffectTogglerCoffeeEffects.cs`（`CardPresentation/Effects/`）已按原版 1:1 端口到本 dll 的类型名，
**但真机行为必须由调度台验一次**（见 `报告/B9_W5COF_Coffee搬运.md` §待验）。

## 5. 顺带缺的第三样东西：shader

Coffee 的 `UIEffectBase.GetModifiedMaterial` 会把底材质换成 `Hidden/{底材质名} (UIEffect)`：

- 原版只有 **1 只**：`Hidden/UI/Default (UIEffect)`（`globalgamemanagers/Shader/Shader_77.json`，
  `m_AssetGUID` = `3786818153 / 1548391642 / 1960798618 / 1897553079`；46 个关键字见报告）。
- **HLSL 源码被剥**（`m_Script` = null）⇒ 按 CLAUDE.md「bundle 里的 Shader 落不了工程资产」，
  它**不能**变成工程资产。`Shader.Find` 找不到 ⇒ Coffee 退回底材质 ⇒ **一个关键字都不生效**。
- 本工程 48 个 `.shader` 里没有它；`wf_shaders.bundle` / `wf_shaders_extra.bundle` / `wf_menus_extra.bundle` 里也没有。
- 补法（未做，需要 Unity 批处理）：照账 455 备注里 G12 给的 `extract_mirror_shaders.py` 套路，把它烘进载体包，
  再让 Coffee 走 `Shader.Find`（⚠️ 工程外 shader 只能运行时加载，见 CLAUDE.md）。

## 6. 想删掉它时

删 `Assets/ThirdParty/` 整个目录 + 把 `UIEffectTogglerCoffeeEffects.cs` 退回 J-12 的「B 路」外壳即可。
该类目前**没有任何调用者**（`grep -rn UIEffectTogglerCoffeeEffects Assets --include=*.cs` 只命中它自己）⇒ 删除零影响。
