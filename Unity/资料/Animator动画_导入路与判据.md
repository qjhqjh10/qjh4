# Animator / 动画片段的导入路与判据（2026-10-01 晚）

> **这一页解决什么**：原版 prefab 上的 `Animator` 在导出到我们工程时**整条链断掉** ——
> 控制器落不下盘、里面的动画片段也落不下盘。本文记**根因、试过的路、定案做法、判据、命令**。
> **代码入口**：`MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`（导出侧）·
> `MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeAnimatorBridge.cs` + `WarpforgeEffectBinder.cs`（运行时侧）·
> `MyGame/Assets/WarpforgeArena1/Editor/AnimClipProbe.cs`（体检）·
> `工具/gen_animator_controllers.py` + `工具/extract_missing_shaders.py --prefabs`（数据/打包侧）。

---

## 一、症状与**被推翻的**旧根因

**症状**：`Card 3D Death Explosion.prefab` / `Necrons death explosion.prefab`（两个阵亡爆散体）
的 `Animator.m_Controller` 是

```
m_Controller: {fileID: 5573443068254224052, guid: 00000000000000000000000000000000, type: 0}
```

—— `guid` 全 0 的伪引用 ⇒ **运行时解析成 null、动画一帧不播**，而编辑器里看不出来（组件都在）。

🔴 **旧记录写的根因是错的**（`项目任务.md` §〇 第 13 条，2026-10-01 晚写下 ——
⚠️ **2026-10-18 之后·第六会话订正（铁律 5）：那个编号所指的段落【已不存在】**（§〇 被重写过多轮），
下面这段**留痕只作「当时是怎么错判的」**用，⛔ 别再去 §〇 找它）：
> 「`extract_missing_shaders.py --prefabs` 的依赖树要**跟着 `Animator.m_Controller` 走**（现在大概只走了渲染器/材质）」

**不成立**。实测：重打包产物的依赖树里**有** `AnimatorController×1` + `AnimationClip×2`，
`[P3]` 那行白纸黑字列着它们，控制器也在同一内层 CAB 里。
⇒ **依赖树那一跳本来就对，断点在【导出】这一跳**（bundle 资产 → 工程资产的转换）。

---

## 二、三条落盘路径**全部不通**（`AnimClipProbe.Run` 逐条量过）

| # | 做法 | 结果 |
|---|---|---|
| ① | `Object.Instantiate(controller/clip)` + `AssetDatabase.CreateAsset` | **空壳** |
| ② | `EditorUtility.CopySerialized(src, new AnimationClip())` + `CreateAsset` | **空壳** |
| ③ | 直接 `AssetDatabase.CreateAsset(bundle 对象)` | Unity 直接抛（`Creating asset … failed`）|

**「空壳」的判据是量出来的，不是看出来的**：

| 量什么 | 原版（bundle 里） | 落盘产物 |
|---|---|---|
| `clip.length` | **0.8167** | **1**（Unity 空 clip 的默认值） |
| `m_FloatCurves` / `GetCurveBindings()` | 0 条（**在 `m_MuscleClip` 里**） | 0 条 |
| `m_ClipBindingConstant.genericBindings` | **3 条** | **0 条** |
| `m_AnimationClipSettings.stopTime` | 0.8167 | 0.8167（**只有这个跟过来了**） |
| `.controller` 的 `layers.Length` | 1（包里） | **0** |

### 根因：**包里的资产是「运行时格式」，工程资产是「编辑器格式」**

- `AnimationClip`：包里是 **muscle / streamed 格式**（`m_MuscleClipSize: 2808`、
  `m_StreamedClip.data` 是压缩曲线块）。**编辑器那份曲线数据根本没打进包里** ⇒
  没有「拷出来」这回事（`Object.Instantiate` 只带得走编辑器侧字段）。
- `AnimatorController`：包里是 `m_Controller` / `m_TOS` / `m_StateMachineArray`；
  工程 `.controller` 要的是 `m_AnimatorLayers` / `m_AnimatorParameters` —— **两套字段不是一回事**。

---

## 三、定案做法（两半）

### ① 控制器 → **照数据表在编辑期建一份工程资产**

`工具/gen_animator_controllers.py` 把运行时格式摊平：

```
数据/游戏数据/animator_controllers.json
```

出处 = `d:/2/新解包资源/assets_full/<包>/AnimatorController/<PathID>.json`（逐字段实读）。
`EffectExporter.ImportAnimatorController` 按它用编辑器 API 建 `.controller`
（层 / 状态 / speed / cycleOffset / mirror / writeDefaultValues / loop / 动作 clip / 默认状态）。
**已有同名资产时清空重建**（保住 guid —— 两个 prefab 共用这一份）。

📌 **全库只有 6 份控制器**，结构同构（1 层 `Base Layer` · 1 状态 · 单节点 1D 混合树 · 0 参数 · 0 过渡）：

| 名字 | 源包 | 谁在用 |
|---|---|---|
| `Card 3D WH40K Explosion` | `battleprefabs_vfxandmisc` | **两个阵亡爆散体**（我们要的那份） |
| `Battle Arena Leviathan` | `battlesharedresources` | 战场利维坦触手待机 |
| `Cannon 1` | `scenes_battlearena1` | 炮台待机 |
| `Pump 1` · `Dynamic Skulls` · `Battle Arena 2` | `scenes_battlearena2` | 战场道具待机 |
（其余 5 份是**场景侧**的，本导出器不管；数据在表里留全了。）

### ② 动画片段 → **运行时从原版小包取原件**（与原版 shader 同一条路子）

工程那份 `.anim` **曲线是空的**（muscle 格式落不了盘）⇒ 真正播的由
`WarpforgeAnimatorBridge` 在 `WarpforgeEffectBinder.Awake` 时从
`StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle` 里 `LoadAsset<RuntimeAnimatorController>(名字)` 换上去。

- 名字由**导出器**写进 binder：`binder.animatorController = <原版控制器名>`。
- 🔴 **控制器自己也必须登记成那个包的容器项**（`extract_missing_shaders.py` 的 `controller_names()`）——
  `LoadAsset<T>(名字)` 只认容器项，不登记就取不到。
- ⚠️ 「同一份内容已在进程里加载过 ⇒ `AssetBundle.LoadFromFile` 返回 null」这条 Unity 限制照样适用；
  桥里有**从已加载的包里捡**的兜底（同 `WarpforgeShaderLoader.HarvestFromLoadedBundles` 的套路）。

---

## 四、判据（自检里三条，缺一条都能假绿）

`BattleScene.Run` 里「阵亡爆散体」那一节（`CardFeel.DeathBodyFx`）：

1. `prefab` 的 `Animator.m_Controller` **不是空**（原来坏在这里）；
2. binder 上记着原版控制器名 `Card 3D WH40K Explosion`；
3. 实例化 → `binder.Apply()` → 挂上的控制器里 `clip.length ≈ **0.8166667**`
   （= 原版 `AnimationClip.m_StopTime`，`assets_full` 那份 JSON 实读）。
   **必须用这个数**才分得出「原件」与「工程空壳」—— 空壳是 `1`。
4. 真的在动：`an.Play(0,0,0); an.Update(0)` 之后子物体 `Minion Death` 的 `activeSelf`
   **true → false**（原版 clip 在 t=0 把它关掉、≈0.1 s 又打开；`AnimClipProbe` 逐帧量出来的）。

> 🔴 **批处理下 Animator 是会跑的** —— `an.Update(dt)` 手动推进即可，
> `GetCurrentAnimatorStateInfo(0).normalizedTime` 会从 0 走到 1。
> ⚠️ 但**基准快照要在 `Update(0)` 之后取**，否则「clip 在 t=0 就改掉的属性」看着像没动过。

### 🔴 写这几条断言时踩的坑（别重踩）

- **`Apply()` 要调【实例上】那个 binder，不是 prefab 资产上那个**。第一版写成
  `prefab.GetComponent<...>().Apply()` ⇒ 挂上去的是**资产**的 Animator ⇒
  ① 断言读到 `clip.length = 1`（实例上那份还是工程空壳）、② 动画没驱动、
  ③ **把 `guid: 000…0` 写回了 prefab 资产里**（正是本文要修的那个症状，实测复现）。
  判据 → `资料/已知的坑.md`「在 prefab 资产上跑运行时代码 = 把运行时对象写进资产」。
- **自检的这两条一红就要重导**：修完断言之后必须再跑一次 `EffectExporter.RunListed` 把 prefab 救回来。

**自检条数**：`BattleScene.Run` 里这一节共 **4 条**（`m_Controller` 非空 · binder 记着控制器名 ·
clip 长 0.8167 · `Minion Death` 真的被切）。**判据看「合计」**：2026-10-01 晚实测 **1204 通过 / 0 失败**。

## 五、命令（顺序不能反）

```bash
# ① 数据表（改了控制器相关的东西才要重跑）
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_animator_controllers.py
# ② 重打小包（控制器 + 片段都登记成容器项；内层 CAB 会改名）
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/extract_missing_shaders.py --prefabs
#    产物自检（**五项**：内层文件 2 个 / 容器 **10** 条名字 / 6 张纹理字节与源包逐字节相同 / 瘦身长度合理）
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/_verify_prefab_bundle.py
# ③ 导出（会把控制器建成工程资产 + 把控制器名写进 binder）
... -executeMethod EffectExporter.RunListed -logFile -
# ④ 建库（漏了这步，新 prefab 进不了效果库、而且**不报错**）
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_effect_index.py
... -executeMethod EffectLibraryBuilder.Run -logFile -
# ⑤ 体检（可单独跑）
... -executeMethod AnimClipProbe.Run -logFile "d:/4/_tmp_view/animclip.log"    # 筛 ^ACP
```

---

## 六、⏭ 同族还没做的一件（**要做**，不是「暂缓」）

**`Vanguard Frame Animated VAT` 的 VAT 驱动器被导出器**删掉了 —— 它不会动。

- 原版那件 prefab 根上挂着一个 **MonoBehaviour**，字段
  `_vatAnimation`(PPtr→legacy 片段) · `_state = 0` · `_animationSpeed = 1`（bundle 里实读）。
- 配套的 **legacy 片段** `Vanguard Frame Animation` 的曲线**是明文可读的**
  （`m_Legacy: True`、**非** muscle 格式）：`attribute = "_state"`、`path = "Vanguard Frame Animated VAT"`、
  `_state` 从 **0.07**（t=0）→ **1.0**（t=1.7916666 s）⇒ **这就是那个「材质化」动画**。
- **为什么现在没有**：`EffectExporter.StripMissingScripts` 把「脚本解析不出来的 MonoBehaviour」**整条删掉**
  （`m_Script` 在 `Waprforge_monoscripts.bundle` 里，我们的工程里没有对应的脚本）⇒ 驱动器没了、
  `_state` 永远停在 prefab 手写的值上。
- 🔴 **2026-10-02 查实，并订正下面那条猜测**：`_state` 的真实去向 = **逐材质的 `Material.SetFloat("_State", …)`**
  （**大写 S**）—— ~~「它多半是**全局**量（`Shader.SetGlobalFloat`）」~~ **不成立**。证据链：
  · **脚本类找到了**：`StoryProgramming.VATGPUPlayer`（`Assembly-CSharp-firstpass`；prefab 的 `m_Script` →
    `bundle_Waprforge_monoscripts/MonoScript/MonoScript_3946243950355606049.json`；签名桩在
    `d:/2/Warpforge_code/Scripts/Assembly-CSharp-firstpass/StoryProgramming/VATGPUPlayer.cs`）。
    `Awake` 里 `_stateId = Shader.PropertyToID("_State")`（字面量在 `stringliteral.json` 地址 `0x4245678`）。
  · **推送点**：`StoryProgramming.VATGPUPlayer__SendDataToRenderer.c:53-54` = `Material.SetFloat(mat, _stateId, _state)`，
    对 renderer 的**每份材质各设一次**；全类 **`SetGlobal*` 命中 0**。
  · **为什么此前在材质/属性表里查不到**（那次排除没算错、结论下早了）：那件 shader 把 `_State`（连同
    `_PartsCount` / `_BoundsCenter` / `_StartBounds*` / `_PositionsTex` / `_RotationsTex`）**藏出 Properties 块、
    放进 `$Globals` 常量缓冲** ⇒ `m_Floats` 与 `m_PropInfo` 里都没有它，但 Unity 仍按材质属性上传 ⇒ `SetFloat` 有效。
  · **驱动方**：legacy 片段 `Vanguard Frame Animation` 的曲线（`classID 114` + `script` 指向该类）直接写该字段；
    播放者 = 父物件 `VanguardIdleEffect` 上的 legacy `Animation`（**`m_PlayAutomatically: true`**）。
- **复刻路径（两件，缺一不可）**：① 自建等价驱动组件（把 **0.07→1.0 / 1.7917 s** 那条曲线搬过来 →
  `renderer.material.SetFloat("_State", v)`；**别**再让 `StripMissingScripts` 删掉它）；
  ② **自写一件 VAT shader** —— 我们工程里**一个 VAT shader 都没有**（`_PositionsTex|_RotationsTex|_PartsCount` 全库 0 命中）。
  ⚠️ **查不到（不猜）**：`_State` 在 shader 内部的**选帧公式**（blob 的 `RDEF` 段被剥、HLSL 源码也没有）
  —— 要么反汇编 `SHDR`，要么对原版抓 RenderDoc。
- 判据（可现在就能核）：`工具/_probe_prefab_deps.py` 的依赖树里 `Animation（组件）×1`；
  以及 `assets_full/<包>/AnimationClip/AnimationClip_8189764618720115800.json` 的 `m_FloatCurves`。
