# `VAT收口` —— A1089（地雷：重导会删掉 VAT 驱动）+ A1090（另 5 件 Remnant VAT）（第六会话 · 动手写手）

> 本件 = 一件两支。**① A1089 已做完**（`EffectExporter` 加了一支 `AttachVatDrivers`）。
> **② A1090 按派工单的逃生口停手**（原因见 §⑤·1 —— 一句话：**要跑一次 Unity 才导得出来，而本轮红线上写着一律不许 `-executeMethod`**）。
> **本次没跑 Unity**（用户本轮要求：待办没做完之前不跑自检）· **没动 git** · **没改正本** · **没动 `d:/2`** · 行尾逐个数过。

---

## ① 结论

- **A1089（地雷）**：✅ **已拆**。`EffectExporter` 现在有一支 `AttachVatDrivers(inst, src.name)`，在 `Export()` 里紧跟 `AttachPoolables` 之后执行 —— 重导 `Vanguard Frame Animated VAT` / `VanguardIdleEffect` 时会把 `WarpforgeVFX.WarpforgeVatDriver` **连 12 个字段的数据一起挂回去**（贴图引用按路径取，取不到就出声）。**`AttachPoolables` 一行未改。**
- **A1090（另 5 件 Remnant VAT）**：⛔ **停手**。查清了「它们挂在哪个 prefab 上」（**就是 `RemnantBody3D Necrons.prefab` 里那个名叫 `Card Remnant` 的子物件 —— 它其实早就在我们工程里**），但**导出这一跳必须在 Unity 里跑 `EffectExporter`**，被本轮红线挡住；而且「驱动该指向 5 条里的哪一条 / 谁来换」**没有判据**（原版的换法在一个我们也没有的脚本里）⇒ 补上去就是猜。**没有硬上**。

---

## ② 改动清单

| 文件:行号 | 改前 | 改后 |
|---|---|---|
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:1056-1058` | （无） | 新增 2 行注释 + `AttachVatDrivers(inst, src.name);`（在 `StripMissingScripts(inst);`（`:1053`）与 `AttachPoolables(inst, src.name);`（`:1055`）之后） |
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:874-1047`（插在原 `:873` 空行之后、`static void Export`（现 `:1049`）之前） | （无） | 新增一整支：**头注**（`:874-917`，根因 / 为什么抄表 / 顺序 / 贴图 / 本支不管的那一处）+ `sealed class VatSpot`（`:920-943`）+ `static readonly VatSpot[] VatSpots`（`:945-977`，2 行记录）+ `AttachVatDrivers`（`:979-1003`）+ `AttachOneVat`（`:1005-1034`）+ `LoadVatTex`（`:1036-1047`） |
| `Unity/MyGame/Assets/CardPresentation/Battle/WarpforgeVatDriver.cs` | —— | ⛔ **一行未改**（不需要它加挂回口：`_vatAnimation` / `_state` / `_animationSpeed` 本来就是 `public` 字段，表直接写进去） |

**净改动**：`178 行新增 / 0 行删除`，**只有 1 个文件**（`EffectExporter.cs`）。

**两个调用点无须改名单**（现读确认）：`ListedPrefabs` 里**已经有** `"Vanguard Frame Animated VAT"`（`:600`）与 `"VanguardIdleEffect"`（`:615`）⇒ 这一支落下去就覆盖到。

---

## ③ 证据：`AttachVatDrivers` 与 `AttachPoolables` 的逐行对应关系

`AttachPoolables` = `EffectExporter.cs:847-872`（**本件一行未动**）；本件新的一支 = `:986-1010`（+ 两个被它调的小函数）。

| `AttachPoolables`（现读原文） | 本件对应处 | 说明 |
|---|---|---|
| `:849` `if (inst == null) return;` | `AttachVatDrivers` 第 1 句 | 同 |
| `:850` `int n = 0, miss = 0;` | 同 | 「挂上几个 / 没对上几个」双计数 |
| `:851` `foreach (var e in PoolableSpots)` | `foreach (var e in VatSpots)` | 同一形态：**本文件里的静态表** |
| `:853` `if (e[0] != rootName) continue;` | `if (e.Root != rootName) continue;` | 按宿主 prefab 名过滤（`Export()` 对**每一件**导出都调一次这两支） |
| `:854` `var tr = FindChildByPath(inst.transform, e[1]);` | `FindChildByPath(inst.transform, e.Path)` | **复用同一个助手**（`:830-845`，归一化名字逐级下沉 —— 尾随空格/大小写都不敏感） |
| `:855-860` 找不到 ⇒ `miss++` + 一条 `Debug.LogWarning` 点名「哪条、在哪个宿主」 | 同（文案逐字仿写，只把 `A210` 换成 `A1089`、表名换成 VAT 表） | **出声，不静默** |
| `:861` `tr.gameObject.AddComponent<CardPresentation.ParticleSystemPoolable>()` | `AttachOneVat` 里 `GetComponent<...>() ?? AddComponent<WarpforgeVatDriver>()` | ⚠️ **一处有意的差别**（下详） |
| `:862` `if (pl == null) { miss++; continue; }` | `if (AttachOneVat(...) == null) { miss++; continue; }` | 同 |
| `:863-866` 挂上后**接引用**（`GetComponent<ParticleSystem>()` + `AssignParticleSystemReference`）；**接不上也照样挂**，只多一条告警 | `AttachOneVat` 里把 12 个字段逐条写进 `drv._vatAnimation`；两张贴图由 `LoadVatTex` 取，**取不到也照样挂**，只多一条告警 | **同一句设计原则**：「组件挂上 ≠ 引用接上」，两件事各自出声 |
| `:867` `n++;` | 同 | |
| `:869-871` 收尾一条汇总（`挂上 N 个` / `没对上 M 个`） | 同 | |
| **调用点** `:874-880`：`Export()` → `StripMissingScripts(inst)` → `AttachPoolables(inst, src.name)` | `:1053` → `:1054-1058`：`StripMissingScripts(inst)` → `AttachPoolables(inst, src.name)` → **`AttachVatDrivers(inst, src.name)`** | **顺序判据同一条**：必须**在 `StripMissingScripts` 之后**（否则刚挂上的又会被那一趟当 missing 删掉）、`PrefabUtility.SaveAsPrefabAsset`（`:1216`）之前 |

**四处有意的不一样（都写在代码注释里了）**

1. **多一整份数据**。`ParticleSystemPoolable` 只要「挂上 + 接一个引用」；`WarpforgeVatDriver` 要**原版那份 12 字段数据**（bounds 四组 / Frames / PartsCount / Duration / 两个 bool / 两张贴图），而那份数据活在一个 `m_Script` 也解析不了的 `MonoBehaviour`（`StoryProgramming.VATAnimation`）上 ⇒ **`LoadAsset` 拿不回来** ⇒ `VatSpots` 表**就是重导时的唯一来源**（值逐字照抄 `assets_full/.../MonoBehaviour/Vanguard Frame Animation VAT.json`，注释里写了出处）。这是与 `AttachPoolables` 唯一实质性的差别。
2. **幂等写法**：`AttachPoolables` 直接 `AddComponent`（原版那件本来就没有它）；`WarpforgeVatDriver` 带 `[DisallowMultipleComponent]`，**重复 `AddComponent` 在编辑器里会报错并返回 null** ⇒ 先 `GetComponent` 再决定挂不挂。
3. **多一步贴图解析**（`LoadVatTex`）：按路径从**工程资产**取；取不到就发一条点名路径的告警，**并且把「为什么不能拿 `ImportTexture` 重导」写在告警里**（`ImportTexture` 不设 `nPOTScale`/`sRGBTexture`，一导就把 NPOT 的 28×41 缩放成 32×64、**贴图布局全毁**）。
4. **表是手工摘的**，不像 `PoolableSpots` 有 `工具/a210_a211_gap.py --cs` 生成器 ⇒ 头注里点名了「加新行时先去 `<包>/MonoBehaviour/<名>.json` 读一遍」。

**表里两条记录的落点（现读证据，不是推断）**

- `Vanguard Frame Animated VAT` → **路径 = 根本身**。判据：`WarpforgeVFX/Prefabs/Vanguard Frame Animated VAT.prefab` 的根 GO `9037655486986414483` 的组件表是 `[Transform, MeshFilter, MeshRenderer, WarpforgeEffectBinder, WarpforgeVatDriver]` —— 驱动就在那个 `MeshRenderer` 同一个 GameObject 上（与 `d:/2/.../GameObject/Vanguard Frame Animated VAT.json` 的 4 个组件对得上，第 4 个 = `VATGPUPlayer`）。
- `VanguardIdleEffect` → **路径 = `VanguardIdleEffect/Vanguard Frame Animated VAT`（直接子件）**。判据：把那件 124252 行的 prefab 的 `m_Father` 链解出来，`1944070609802876137`（GO `5838308720746842456`，名 `Vanguard Frame Animated VAT`，带组件 `7791699558725360724`）的父是 `438590108953010361`（GO 名 `VanguardIdleEffect`）—— **直接子件**。
- **出牌与待机是两个入口**（CLAUDE.md §十 第 5 条那族坑）⇒ 表里**两处都要有**，只补一处 = 另一个入口静默失效。

---

## ④ 验证

**秒级类型检查**（`TMPDIR=/tmp/wf_vat2 bash d:/4/Unity/工具/typecheck.sh`）
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
跑了 **3 次**：① 加完调用点+整支之后 ② 改完注释里的行号引用之后。**每次都 0/0**。
⚠️ 本轮有别的写手在改 `Core/` 等文件，**0 条错**说明没撞上别人的半成品。
✅ 另核过「我的文件真被编进去了没有」：`/tmp/wf_vat2/wf_csc_editor.rsp:559` 附近有 `EffectExporter.cs`、`wf_csc.rsp:559` 有 `WarpforgeVatDriver.cs` ⇒ **不是「没编所以 0 错」**。

**行尾**（二进制读，改前 → 改后；⛔ 没用 `sed -i`、⛔ 没用 python 文本模式写）

| 文件 | 改前 CRLF / LF | 改后 CRLF / LF |
|---|---|---|
| `EffectExporter.cs` | **2348 / 2348**（CRLF 文件） | **2526 / 2526**（`178` 行整行新增，CRLF 未翻） |
| `WarpforgeVatDriver.cs` | 0 / 365 | **0 / 365**（未改） |

**`git diff --numstat`**（改完立刻跑的）
```
178     0       Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs
```
⇒ 只有 1 个文件、纯新增、无删除；`WarpforgeVatDriver.cs` **不在 numstat 里**（证明没碰它）。

**⚠️ 被 `.gitignore` 挡住、`git status` 看不见的产物**（**单列一节，因为这是我这一支能不能活下来的前提**）

`git check-ignore -v` 逐条核过，命中同一条 `.gitignore:26: /Unity/MyGame/Assets/WarpforgeVFX/*`（只放行 `Runtime/` `Shaders/`）：

| 产物（绝对路径） | 入库? | 字节 | md5(前 12) | mtime |
|---|---|---|---|---|
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/Vanguard Frame Animated VAT.prefab` | ⛔ **ignored** | 6714 | `aa0309ab6450` | 2026-10-08 23:34 |
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/VanguardIdleEffect.prefab` | ⛔ **ignored** | 3008659 | `a82e0a059df8` | 2026-10-08 23:34 |
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Animations/Vanguard Frame Animation.anim` | ⛔ **ignored** | 7845 | `88388c3792f3` | 2026-10-08 23:34 |
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Animations/Vanguard Frame Animation.anim.meta` | ⛔ **ignored** | 188 | `0e5391661550` | 2026-10-08 23:34 |
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/Vanguard Frame Animation VAT_PositionTex.png` | ⛔ **ignored** | 544 | `0e2481439826` | 2026-10-08 23:33 |
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/Vanguard Frame Animation VAT_PositionTex.png.meta` | ⛔ **ignored** | 2650 | `0d908f143dbf` | 2026-10-08 23:33 |
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/Vanguard Frame Animation VAT_RotationTex.png` | ⛔ **ignored** | 584 | `339d21b4448b` | 2026-10-08 23:33 |
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/Vanguard Frame Animation VAT_RotationTex.png.meta` | ⛔ **ignored** | 2650 | `601b3fe32421` | 2026-10-08 23:33 |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/WarpforgeVatDriver.cs` | ✅ 未跟踪、**没被 ignore**（`??`） | 22797 | `e7ae465e338a` | 2026-10-08 23:32 |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/WarpforgeVatDriver.cs.meta` | ✅ 同上（`??`） | 59 | `84eff69985c2` | 2026-10-08 23:33 |

⇒ **本支写进 `.cs` 的代码会入库；它要「挂回去」的那些对象（两个 prefab / clip / 两张贴图）一个都不入库。**
「我这支改了没有」**不能拿 `git status` 判**（本件按派工单要求直接看 mtime / 字节 / md5，上表就是）。

---

## ⑤ 没查清 / 停手的（逐条：卡在哪、试过什么）

### 1. A1090 停手（另 5 件 `Card Remnant_Remnant Anim {1,3,5,8,9}`）

**派工单的逃生口命中了**：「若你判断它超出本批能安全做完的范围（例如要重打包 bundle、要动 `d:/2`）⇒ 停手」。实际卡点**不是**重打包，而是两条：

**(a) 🔴 导出这一跳必须在 Unity 里跑，本轮红线写着一律不许。**
`A1090` 的产物是「5 件 VAT 能被挂上驱动」。把驱动挂上**只有一条路**：跑 `EffectExporter`（`Run()` 或 `RunListed()`）—— 因为 `AttachVatDrivers` 只在导出时执行。
而本轮红线：「⛔ **不许跑 Unity 自检**（`-executeMethod` / `_run_8_checks.sh` 一律不许）」。
⇒ 我**可以**把表行和贴图先摆好（见下），但那只会留下**没被执行过、也验不了**的半成品 ⇒ 按「⛔ 不许硬上」停手。

**(b) 「驱动指向 5 条里的哪一条、谁负责换」没有判据 —— 补上去就是猜。**
现读到的原版结构（`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`）：

| 事实 | 出处（逐字节读的） |
|---|---|
| 全库 `VATGPUPlayer` 实例**只有 2 个** | `grep -rl 3946243950355606049 MonoBehaviour/` ⇒ `MonoBehaviour_-6996853648599967635.json`（Vanguard）与 `MonoBehaviour_7929411139690315314.json`（Remnant） |
| 第 2 个实例挂在 **`Card Remnant`** 这个 GameObject 上 | `MonoBehaviour_7929411139690315314.json` 的 `m_GameObject` = `-7151946301295935950`；`grep -rl 7929411139690315314 GameObject/` ⇒ **只有 `GameObject/Card Remnant.json`** |
| 它的 `_vatAnimation` = `2745102283983744083`（**5 条里的某一条**） | 同上文件 `_vatAnimation.m_PathID` |
| 那 5 条是**一个控制器脚本的字段**，不是 5 个 prefab | `MonoBehaviour_9217091644567076402.json`（`m_Script` = `8925332351926007966`）的 `deathAnimationList` **正好 5 条**，其中一条就是 `2745102283983744083`；同文件还有 `vatPlayer → 7929411139690315314`、`myAnimation`、`toRemnantAnimationName: "To Remnant Necrons"`、`reanimateAnimationName: "From Remnant Necrons"` |
| 🔴 **那个控制器脚本我们也没有**（`8925332351926007966` 在工程里解析不了）⇒ 原版「按阵营挑哪条动画」的那段逻辑**我们完全没有** | 同上；而且这正是「哪一条」查不出来的原因 |
| `2745102283983744083` **不是 addressable**：只在包的 `m_PreloadTable` 里、**不在 `m_Container` 里** | `AssetBundle/AssetBundle_1.json`：全文件搜这个 ID **只命中 1 次**，位置在 `m_PreloadTable` |
| 10 张贴图**确实在源包里**（不是「本地没有」） | `Texture2D/Card Remnant_Remnant Anim {1,3,5,8,9}_{Position,Rotation}Tex.png` 共 10 张；尺寸 **150×15 / 75×15**（= 2×75 宽 / 15 帧高），出处 `d:/4/Unity/数据/游戏数据/vfx_texture_mips.tsv:1144-1153` |

**⇒ 派工单里那句「那 5 件没 prefab」要就地订正（铁律 5）**：宿主 GameObject **早就在我们工程里**了 ——
`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/RemnantBody3D Necrons.prefab` 里有一个**名叫 `Card Remnant` 的子物件**（GO `8296231477147596771`），组件表 = `[Transform, MeshFilter, MeshRenderer]` + **两个材质**：
`Remnant Shatter`（原版 shader `Everguild/Cards/Necrons Base Death`，guid `63466ea163121c24f871e351c2bb06b9`）与
`Card Shatter Inner Material`（`Everguild/Cards/Shatter Inner Pieces`，guid `dd30175e10f4ad24d99d192aa97bd1f1`）。
**缺的只是那个 `VATGPUPlayer`（被 `StripMissingScripts` 删了）+ 10 张贴图 + 那个控制器脚本。**

**要给下一批的「还差什么 / 要谁点头」**（按依赖顺序）：
1. **先查**：这 5 条 VAT 动画在画面上**由哪个材质消费**。⚠️ 现读**对不上**：`Card Remnant` 的两个材质**都不是 VAT shader**，而全库用 `Everguild/Matcap/Matcap Full Options VAT` 的材质只有 3 个（`Stikka_mat` / `Vanguard_Frame VAT` / `Vanguard_Frame VAT Dissolve`，出处 = 在 `Material/*.json` 里搜 shader pathID `6331056250094909814`）。**这一条没落地之前补驱动也看不见差别**。
2. **再查**：`2745102283983744083` 是 5 条里的哪一条（pathID→名字的映射在这份解包里**没有**；`m_Container` 的键是 32 位哈希，不是名字）。**⇒ 查不到就要走另一条判据，⛔ 不许按 `1/3/5/8/9` 猜。**
3. **要点头**：跑一次 Unity `EffectExporter.RunListed`（需要把 `RemnantBody3D Necrons` 加进 `ListedPrefabs`）+ 一次 `EffectLibraryBuilder.Run`。
4. 10 张贴图的导入设置（**照 Vanguard 那两张的配方**，可直读）：`nPOTScale: 0`（150×15 / 75×15 **是 NPOT**）、`enableMipMap: 0`、`sRGBTexture: 0`（原资产 `m_ColorSpace: 0` = 线性数据贴图）、`textureFormat: 4`(RGBA32) + `textureCompression: 0`、`filterMode: 1`、`wrapU/V: 1`。

### 2. `Material.SetFloat` 到底生不生效？（**没查清，且它就在 A1089 的前提上**）

- **硬事实**：`Everguild/Matcap/Matcap Full Options VAT`（`Shader_6331056250094909814.json`）的 `m_PropInfo.m_Props` **共 35 条，逐条列完了**（`_Color` / `_MainTex` / `_MatCap` / `_Intensity` / `_AlphaClipThreshold` / `_USEEMISSION` / … / `_QueueControl` + 3 条 `unity_*`）—— **里面没有** `_State`、`_PositionsTex`、`_RotationsTex`、`_BoundsCenter`、`_BoundsExtents`、`_StartBoundsCenter`、`_StartBoundsExtents`、`_PartsCount`、`_PartsIdsInUV3`、`_HighPrecisionMode`、`_PositionsTexB` **这 11 个里的任何一个**。
  第二条硬事实：**原版材质本身也没有**——`Material/Material_3895008143569226419.json`（`Vanguard_Frame VAT`）的 `m_SavedProperties` 里 `m_Floats` 42 条、`m_TexEnvs` 8 条，**同样一个都没有**。
- **⇒ 直接后果（这条是确定的）**：`Material.HasProperty("_State")` **必然返回 false**。所以上一件报告 §七 断言建议第 10 条若拿 `HasProperty` 当闸门，会**恒定误报**。
- **没查清的**：Unity 在「uniform 声明在 `UnityPerMaterial` cbuffer 里、但没写进 `Properties` 块」时，`Material.SetFloat(nameID, v)` **到底绑不绑得进 cb0**。两说都有道理：① 若绑得上 ⇒ 原版这条路是通的、`HasProperty` 为假属于正常；② 若绑不上 ⇒ **驱动推出去的 11 个值全部落空**（`SetFloat`/`SetTexture` 对未声明属性是**静默丢弃**），那 VAT 整个不动。
  - **试过的**：`m_ParsedForm.m_CommonParameters`（想拿 `TextureParams[].m_Index` 反推槽号）—— **这份解包 JSON 里没有这个字段**（`ParsedForm` 只有 `m_PropInfo` / `m_SubShaders` / `m_KeywordNames` / `m_KeywordFlags` / `m_Name` / …），5 个 pass 的 `m_CommonParameters` 全空。
  - **怎么收口**：一次 Unity 运行就能定 —— 挂上驱动跑一帧，`Debug.Log(mat.GetFloat("_State"))` 读回来比。**本批跑不了**（红线）。⚠️ 别拿 `HasProperty` 当判据（见上）。

### 3. 本件的新代码**一条都没被执行过**

没跑 Unity ⇒ `AttachVatDrivers` / `AttachOneVat` / `LoadVatTex` 只在**编译**层面验过（类型检查 0 错），**运行时路径没跑过**。具体没验的三处：① `AddComponent<WarpforgeVatDriver>()` 在编辑器的 `Export()` 时刻返回非 null；② `PrefabUtility.SaveAsPrefabAsset` 把 `WarpforgeVatData`（`[Serializable]` 普通类）正确序列化进 prefab（**上一件手写的那两个 prefab 里它是能落盘的**，但那是我手写的 YAML，不是引擎写的）；③ `AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/WarpforgeVFX/Textures/…png")` 在导出中途取得到。
**这是本件最大的未验证面。**

### 4. 上一件（`VAT①`）遗留的三条，本件**没有**动

`.anim` 的 `tangentMode`、四个 vec4 的 `.w` 那四个无消费者读取、`PlayRecording()` 的调用者 —— 都还在 `W_VAT驱动.md` §五。本件与它们无关，**没有复查**。

---

## ⑥ 顺手发现的

1. 🔴 **legacy `Animation` 的片段引用 guid 全 0 是【系统性】的，不是 `VanguardIdleEffect` 一处。**
   实测扫 `WarpforgeVFX/Prefabs/*.prefab` **963 件**：`--- !u!111`（`Animation`）共 **50 个组件**，其中 **45 个**的 `m_Animation` 是
   `{fileID: …, guid: 00000000000000000000000000000000, type: 0}`，分布在 **40 件 prefab**（`AcidSpraySweepAttack` / `BolterSweep` / `CardPrefab` / `EC Heldrake Attack` / 一串 `Environmental Condition *` …）。
   ⇒ 上一件**手改好的那一处，下一趟重导就会被打回 guid 全 0**（导出器 `:1194` 那段只处理 `Animator.runtimeAnimatorController`，**没有 legacy `Animation` 那一支**）。
   ⇒ **这是一件独立 A 项**（形状 = 照 `:1194` 补一支），**⛔ 不该在 `AttachVatDrivers` 里给单个 prefab 打补丁**（CLAUDE.md §三「两处写同一条规则」）。**建议落进待办正本。**
   ⚠️ 与 A1089 的**区别**：A1089 是「组件连数据一起没了」（**驱动这一支会补回来**）；这一条是「引用变 guid 0」（**本件没补**）⇒ 重导之后 VAT 驱动会活着，但那件 mesh 仍可能因为 clip 断链而不显示。

2. 🔴 **`HasProperty("_State")` 必为 false**（见 §⑤·2）—— 上一件报告 §七 断言建议第 10 条若照写会**恒定误报**。要么改成「读回材质值比」，要么明确把这条断言写成「已知恒假，只当出声用」。

3. ✅ **`FixTextureImportSettings` 不会冲掉那两张 VAT 贴图的手设导入设置**（我一开始担心它会）。
   现读：它**只改 `mipmapEnabled` 与 `maxTextureSize`** 两个字段；而 `vfx_texture_mips.tsv:2852-2853` 有这两张的行（`28×41 / 14×41，原版 mips=1`）⇒ `wantMip = false`，与手写的 `enableMipMap: 0` **一致**；`wantMax = max(2048, NextPowerOfTwo(41)) = 2048`，与手写的 `maxTextureSize: 2048` **也一致** ⇒ 那一趟对它们是 **no-op**。
   **真正会冲掉的是 `ImportTexture`**（它写整套 `.meta`，不设 `nPOTScale`/`sRGBTexture`）⇒ 我把「⛔ 别拿 `ImportTexture` 重导」写进了 `LoadVatTex` 的告警文案里。

4. 🔴 **`WarpforgeVatDriver.cs` 在库里、它要服务的资产全不在库里。**
   `git status --porcelain` ⇒ `?? …/WarpforgeVatDriver.cs` + `?? …/WarpforgeVatDriver.cs.meta`（未跟踪、但**没被 ignore**，会入库）；
   而两个 prefab / 那个 `.anim` / 两张贴图及其 `.meta`（共 8 个）**全部命中 `.gitignore:26`** ⇒ **换一台机器 clone 出来，只有组件、没有挂点**。
   ⚠️ 这**不是本件造成的**，但下一趟「真跑导出」的人必须先知道这件事（否则会以为「怎么挂不上去」）。

5. **全库用 VAT shader 的材质只有 3 个**：`Stikka_mat`（`Material_-2433086634806877593`）、`Vanguard_Frame VAT`、`Vanguard_Frame VAT Dissolve`（在 `assets_full/*/Material/*.json` 里搜 shader pathID `6331056250094909814` 的**全部命中**）。
   ⚠️ **`Card Remnant` 的两个材质都不在其中** ⇒ A1090 第 1 步「谁消费这 5 条动画」是个**真缺口**，不是形式检查。

6. `Card Remnant` 的 GameObject 在包里是 `m_IsActive: false`（`GameObject/Card Remnant.json`），与 `BattleDriver.cs:10400` 记的「关 3D 体」自洽 —— 只是记录，没动。

---

## ⑦ 300 字以内摘要

**A1089 已拆**：`EffectExporter.cs` 新增 `AttachVatDrivers(inst, src.name)`（`:1054-1058` 调用、`:874-1191` 定义），
照 `AttachPoolables` 的同一套写法（静态表 → `FindChildByPath` → `AddComponent` → 填字段 → 没对上就出声），
`AttachPoolables` **一行未改**；表里 2 条（`Vanguard Frame Animated VAT` 根上 / `VanguardIdleEffect` 的同名直接子件），
12 字段逐字照抄原资产 JSON。净改动 **178 行 / 1 个文件**，类型检查 **0/0**，CRLF 未翻（2526/2526）。

**A1090 停手**：那 5 条 Remnant VAT **不是「没 prefab」**——宿主 GameObject `Card Remnant` **早就在工程里**
（`RemnantBody3D Necrons.prefab` 的子物件）；真缺的是驱动 + 10 张贴图（150×15 / 75×15）+ 一个我们也没有的控制器脚本
（`8925332351926007966`，`deathAnimationList` = 那 5 条）。卡两处：**跑不了 Unity**（红线）、**「指向哪一条 / 谁换」没判据**。

**另报**：legacy `Animation` 片段 guid 全 0 是**系统性**的（45 个组件 / 40 件 prefab）；
`HasProperty("_State")` **必为假**（shader 属性表里没有那 11 个名字）；
VAT 相关 8 个资产**全被 `.gitignore` 挡住**；用 VAT shader 的材质全库只有 3 个，**`Card Remnant` 那两个都不在其中**。
