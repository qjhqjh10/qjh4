# E3 · `eventToFire` 护栏**真落盘** + `EffectName` 收口

> 代理 E · 2026-10-23 · 承接 `E2_dump护栏与EffectName审计.md`（干跑已拍板）
> ⛔ 没跑 Unity（效果库重建由主对话做）· ⛔ 没动 git · ⛔ 没改 `CardPresentation/**`

---

## ① 结论

### 一、落盘完成，两个产物与干跑**逐字节一致**

| 产物 | 落盘后 md5 | 字节 | 与干跑 `after` 比 |
|---|---|---|---|
| `数据/游戏数据/animfx_components.json` | `be0641747320e1c5236232d63062dd97` | **2,298,577** | ✅ **md5 完全相同**（`after.json` 也是 `be064174…`） |
| `数据/游戏数据/animfx_modules.json` | `f3b4c1f12cc46c34e59bb86f44ee55b9` | **2,641,576** | ⚠️ md5 不同，但**只差 `generated` 时间戳一行**；去掉它之后**整体相等**（已逐键核过） |
| `数据/游戏数据/module_materials.tsv` | `41bed0a3ec5880b9a9ccedcfe62fe36a` | **419** | ✅ **逐字节不变**（改前 / 干跑 `mats_after.tsv` / 现在，三者同一个 md5） |

- 改前 → 改后：`animfx_components.json` 2,296,976 → **2,298,577 B（+1,601 B / +0.070%）**；
  `animfx_modules.json` 2,639,304 → **2,641,576 B（+2,272 B / +0.086%）**。
- 新增哨兵**当场生效**：`gen_animfx_modules.py` 跑出来的那行现在是
  `collisionEvent / eventToFire 子树被深度护栏截断的键：0 个`（`eventToFire` 已并入，见 ④-1）。
- `refs.node 3123 → 3125`（+2，就是那两条 call 的目标）、`asset 1928` 不变、
  `effect_count 912` / `module_count 2346` 不变。

### 二、三条**互相独立**的取数路径现在一致

落盘后我又对了一次账（脚本见 ②-3）：

- ① `assets_full` 的 prefab 原始 JSON（`工具/gen_module_events.py` → `module_events.json`）
- ② dump 流水线（`dump_animfx.py` → `gen_animfx_modules.py` → `animfx_modules.json` 里的 `@node:` 键）
- ③ `WFModuleEvent.cs` 里那张 C# 常量表（`gen_module_events.py --check`）

三条的 **target 路径 / 类型名 / 方法名 / `m_Mode=6` / `m_BoolArgument=1` / `m_CallState=1,2`**
**逐字段相同**，`--check` 仍然 `== C# 生成区与 json 一致（2 条）`。

### 三、`EffectName` 收口 —— 三处都改完了，**生产路径零影响**

- `WarpforgeEffectPlayer.EffectName`：`_entry == null` 时剥 `(Clone)` + **每实例报一次**。
- `WFEffectModule.EffectName`：`Controller == null` 时**返回空串**（⛔ 不再退回 `name`）+ 每模块报一次。
- `WFModuleAnimation.EnsureClips()`：别名一条都取不到时**不再静默** —— 计入新的 `ClipAliasMisses`
  **和** `PlayFailures`（按主对话口径与 R24 统一），并把根因写进 `_failReason` 供后续告警复用。
- **`Controller == null` 会读到空串的调用点：生产路径 0 处**（逐个列在 ②-4）。
  只有三类**边角**会看到空串，且**全部只影响日志文案**（不影响任何按名字拼的键）。

---

## ② 证据

### 2·1 落盘的执行与核对

```
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 工具/dump_animfx.py
   → 已写出 d:/4/Unity/数据/游戏数据/animfx_components.json  (2245 KB)
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 工具/gen_animfx_modules.py
   → 写出 d:/4/Unity\数据\游戏数据\animfx_modules.json (2580 KB)
     collisionEvent / eventToFire 子树被深度护栏截断的键：0 个
     效果 912 个 / 模块 2346 个 / 引用 node 3125 · asset 1928
     模块引用的材质：3 条（3 个效果）
```

`md5sum` 三方对照（含干跑）：
```
be0641747320e1c5236232d63062dd97  数据/游戏数据/animfx_components.json   ← 落盘
be0641747320e1c5236232d63062dd97  _tmp_view/E2_dry/after.json            ← 干跑   ✅ 同一份
f3b4c1f12cc46c34e59bb86f44ee55b9  数据/游戏数据/animfx_modules.json      ← 落盘
d2da8a89e3517bed08df56958522be45  _tmp_view/E2_dry/modules_after.json    ← 干跑（差 generated）
41bed0a3ec5880b9a9ccedcfe62fe36a  数据/游戏数据/module_materials.tsv     ← 现在
41bed0a3ec5880b9a9ccedcfe62fe36a  _tmp_view/E2_dry/mats_after.tsv        ← 干跑   ✅ 逐字节不变
```
`modules` 那对的差异**只有**：
```python
{a[k] != b[k] for k in a} == {'generated'}
a["generated"] = "2026-10-23 07:13"   b["generated"] = "2026-10-23 07:09"
# 去掉 generated 后 a == b → True
```

**行尾没翻**：`animfx_components.json` **CRLF 91208/91208**（`dump_animfx.py` 用的是文本模式写，
Windows 下就是 CRLF —— **这是改前就有的行为，我没动它**）；`animfx_modules.json` **LF 84902/84902**；
`module_materials.tsv` **LF**。三者行尾与改前一致。

**diff 大小**（不是整篇重写）：
```
48 +/1 −   数据/游戏数据/animfx_components.json     （−1 = 那行 "m_Calls": "<深>"）
42 +/2 −   数据/游戏数据/animfx_modules.json        （+20 keys +20 values；−2 = generated + 旧 <深> 相关）
```

### 2·2 `EffectName` 三处改动的代码位置

| 文件:行（改后） | 改动 |
|---|---|
| `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:120-160` | `EffectName` 从一行表达式属性改成块属性：`_entry != null` → `_entry.name`；否则剥 `(Clone)` + `WarnNoEntry`（`_warnedNoEntry` 每实例一次） |
| `WarpforgeVFX/Runtime/WFEffectModule.cs:136-167` | `EffectName`：`Controller != null` → `Controller.EffectName`；否则 **`return ""`** + `_warnedNoController` 每模块一次的警告 |
| `WarpforgeVFX/Runtime/WFModuleAnimation.cs:63-72, 105-130, 137-152` | `PlayFailures` 文档更新 + 新增 `ClipAliasMisses`（`ResetDiagnostics` 一并清）；`EnsureClips()` 两条失败分支改为调 `AliasMiss(why)`；新增 `AliasMiss()` 出口 |

**为什么 `ClipAliasMisses` 要单开一格**（我自己加的，如实写）：
主对话要求「也 `PlayFailures++`」，我照做了（`AliasMiss` 里两者都加）；
但「别名取不到」和「`Exit` 播失败」根因不同（前者 = 表没登记 / 名字拼错；后者 = 没有 controller / clip），
混成一个数就分不出来 —— 而这正是 E2 里 §⑤·3 指出的问题。两个计数器并存 = 口径统一**且**根因可分。

### 2·3 三条来源一致性核对（落盘后现跑）

```python
# 从落盘的 animfx_modules.json 里取 AnimFXModuleEvent 模块的两个 call，
# 与 module_events.json 逐字段比（target 路径按 @node: 约定拼回去）
OK  ('@node:MonoBehaviour:VanguardIdleEffect',
     'AnimFXModuleChangeMaterial, Assembly-CSharp', 'ToggleMaterial', 6, '1', 1)
OK  ('@node:MonoBehaviour:VanguardIdleEffect/Vanguard Frame Animated VAT#1',
     'StoryProgramming.VATGPUPlayer, Assembly-CSharp-firstpass', 'UpdateMaterials', 6, '1', 2)
三条独立来源一致: True
```
加上 `gen_module_events.py --check` → `== C# 生成区与 json 一致（2 条）`。

### 2·4 `Controller == null` 会读到**空串**的调用点（逐个列）

我把全仓 `EffectName` 的每个出现处都定位到了**所属成员**（脚本按最近的成员声明行反查），
再判「该成员是否可能在 `Initialize` 之前被调用」：

| 文件:行 | 所属成员 | 触发时机 | `Controller == null` 可达吗 | 看到空串的后果 |
|---|---|---|---|---|
| `WFModuleAnimation.cs:105,106` | `EnsureClips`（← `Initialize`） | Initialize 之后 | ❌ 不可达 | — |
| `WFModuleAnimation.cs:199` | `Play`（← `Exit`/`Tick`） | 同上 | ❌（`_hasClips` 要求 `EnsureClips` 成功过） | — |
| `WFModuleAnimation.cs:174` | `Exit` | 同上 | ⚠️ **边角**：对象在 `Initialize` 之前就被 `Exit()` | 日志里效果名空 |
| `WFModuleCardback.cs:110,122` | `Initialize` | Initialize 之后 | ❌ | — |
| `WFModuleChangeMaterial.cs:524,532` | `ToggleOnCardMaterial` | ← `ToggleMaterial` | ⚠️ **边角**：见 ⑤-2（Event 在 ChangeMaterial 之前 Initialize） | 日志里效果名空 |
| `WFModuleChangeMaterial.cs:548` | `ToggleOnCustomRenderers` | ← `ToggleMaterial` | ⚠️ 同上 | 日志里效果名空 |
| `WFModuleChangeMaterial.cs:652` | `WarnNoCardContext`（← `Restore`/`RecoverOnDestroy`） | `OnDestroy` 可能在 Initialize 前 | ⚠️ **边角**：手工挂的模块从未 Initialize 就被销毁 | 日志里效果名空 |
| `WFModuleChangeVelocity.cs:151,198,303` | `ApplyFromContext`/`ModifyVelocity`/`ResolveSystems` | Initialize / Tick | ❌ | — |
| `WFModuleDestroyInTime.cs:96` | `WarnNoBranch`（← `Initialize`） | Initialize 之后 | ❌ | — |
| `WFModuleDoDestroyAnimation.cs:95` | `DoDestroy` | 播放器广播 | ⚠️ **边角**：未 Initialize 就被销毁 | 日志里效果名空 |
| `WFModuleEvent.cs:249,267,287,395,405,488` | `Fire`/`TryWire`/`TryWireOne`/`WarnAboutUnbranchableEntries` | Initialize / Exit | ❌（`Fire` 只在 Initialize/Exit 里调） | — |
| `WFModuleInstanceParticleAdjacent.cs:192,233` | `Initialize`/`ExecuteEffect` | Initialize 之后 | ❌ | — |
| `WFModuleMoveParticlesToTarget.cs:200,209,246,321` | `Initialize`/`ResolveSystems`/`UpdateTargetPosition` | Initialize / Tick | ❌ | — |
| `WFModulePostProcess.cs:318,336,391,432` | `DoBloomAim`/`DoLUTAnim`/`ResetPostFX`/`BlendLutTextures` | Tick / 内部 | ❌ | — |
| `WFModulePostProcess.cs:452` | **`OnValidate`**（编辑器消息） | 编辑器改字段 | ⚠️ **边角**：`isPlaying` 且手挂模块 | `PostFxRequest.effect = ""` —— **下游 `BattlePostFx.Handle` 从不读它**，无影响 |
| `WFModuleTransformModifier.cs:353,476,515` | `Initialize`/`ApplyModification` | Initialize 之后 | ❌ | — |
| `WFModuleTween.cs:249,316` | `Pump`/`Drop` | Tick | ❌ | — |

**结论：生产路径（`Play()` → `BuildModules` → `Initialize` → `Tick`/`Exit`）一处都读不到空串。**
能读到空串的只有「模块在 `Initialize` 之前就被 `Exit`/`DoDestroy`/`OnDestroy`/`ToggleMaterial`/`OnValidate` 摸到」
这一类**非正常时序**，且**全部只落在 `Debug.LogWarning` 的文案**上 ——
**没有任何一处把 `EffectName` 的空串当成字典键或资源名**（唯一的键用法 `WFModuleAnimation` 那三处都在 `Initialize` 之后）。

### 2·5 类型检查

```
TMPDIR=/tmp/wf_E3 bash d:/4/Unity/工具/typecheck.sh
  --- 运行时程序集 ---  运行时错误数: 0
  --- 编辑器程序集 ---  编辑器错误数: 0
```

---

## ③ 没查清的部分

| # | 没查清 | 缺什么 |
|---|---|---|
| 1 | **效果库没重建**（`WarpforgeEffectLibrary.asset` 还是旧的 `WFModuleDef`） | 要 Unity（主对话做）。**切「读 def」之前必须重建**，否则新键根本进不了运行时 |
| 2 | **R25 的断言没在落盘后复跑**（`AnimFXCheck.Run` 要 Unity） | 主对话同步点跑一次，看 `WiredCount/FiredCount/WiringProblems` 有没有变化（**按预期应当一模一样**：表是按同一份数据生成的） |
| 3 | `WFEffectInfo.EffectName`（另一个类，`WarpforgeVFX/Runtime/WFEffectInfo.cs:45`）有一模一样的 `gameObject.name` 兜底 | 它是**构建期**读 prefab 资产（`EffectLibraryBuilder.cs:261`），不走实例路径 ⇒ **没改**。将来若有人在运行期读它，要重新评估 |
| 4 | `EnsureClips()` 里还有**两条静默早退**：`node == null`、`_animator == null` | 这两条**没加计数**（不在本批授权范围）。它们会让「别名表明明有、但节点没 Animator」也静默 —— 见 ⑤-3 |

---

## ④ 改动清单

| # | 文件 | 一句话 |
|---|---|---|
| 1 | `数据/游戏数据/animfx_components.json` | **覆盖**（`dump_animfx.py` 真跑）：`m_Calls` 从 `<深>` 展开成 2 条 · 2,298,577 B |
| 2 | `数据/游戏数据/animfx_modules.json` | **覆盖**（`gen_animfx_modules.py` 真跑）：多 20 个键 · 2,641,576 B |
| 3 | `工具/gen_animfx_modules.py` | **改**（8+/2−）：`enc()` 的 `DEEP_KEYS` 判据加 `or "eventToFire" in prefix`；`DEEP_KEYS` 声明注释与 `main()` 那行打印文案同步改成 `collisionEvent / eventToFire` |
| 4 | `MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs` | **改**（82+/9−）：`EffectName` 剥 `(Clone)` + 每实例一次警告 |
| 5 | `MyGame/Assets/WarpforgeVFX/Runtime/WFEffectModule.cs` | **改**（40+/2−）：`EffectName` 在 `Controller == null` 时返回空串 + 每模块一次警告 |
| 6 | `MyGame/Assets/WarpforgeVFX/Runtime/WFModuleAnimation.cs` | **改**（75+/13−）：`ClipAliasMisses` + `AliasMiss()`；`EnsureClips` 两条失败分支出声 |
| 7 | `d:/4/Unity/资料/普查产出_1023/E3_护栏落盘与EffectName收口.md` | **新建**：本报告 |

**✅ 按主对话要求**：`数据/游戏数据/module_events.json`（2,568 B，07:05）与 `WFModuleEvent.cs` 里的
`TableCall[] Table`（仍是 **2 行**）**都还在，没删没改**。

---

## ⑤ 顺手发现（只报不改）

### ⑤-1 🔴 **`AnimFXModuleEvent` 的 `whereToFire = 0 (Initialize)` 是个潜在时序地雷**
`WarpforgeEffectPlayer.BuildModules` 是**先建全部模块、再按 `_modules` 列表顺序逐个 `Initialize`**
（`WarpforgeEffectPlayer.cs:323-324`），列表顺序 = `entry.modules` 顺序。
⇒ 若将来某个效果的 `AnimFXModuleEvent` 排在 `AnimFXModuleChangeMaterial` **前面**、且 `whereToFire = 0`，
那么事件会在 ChangeMaterial **还没 `Initialize`（`ResolveAssets()` 没跑）**时调 `ToggleMaterial(true)`
—— 那时 `material`/`customRenderers` 全是 null（走 `useCustomRenderers` 那支还能退到 `renderers[0].sharedMaterial`，
但 `isCardMaterial` 那支会拿到空卡片上下文），**静默半失效**。
今天不可达（全库唯一的 Event 实例 `whereToFire = 5`），**只报不改**。
建议将来给 `WFModuleEvent.TryWireOne` 加一句「目标模块的 `Controller` 是否已注入」的前置检查。

### ⑤-2 `WFModuleChangeMaterial.ToggleMaterial` 现在如果被提前调用，**只会少一条日志名**，不会更糟
这是 ②-4 里那条「Event 在 ChangeMaterial 之前」的直接后果 —— 但要强调：
**改动前后都不影响功能**（`ToggleMaterial` 的函数体一次都没读 `EffectName`），
改动只是把「日志里印一个错的节点名」换成「印空串 + 一条明确的时机警告」。**这是净收益。**

### ⑤-3 `WFModuleAnimation.EnsureClips()` 还有两条**静默早退**
```csharp
if (node == null) return;                 // 连节点都没有
if (_animator == null) return;            // 节点上没有 Animator
```
这两条**没加计数**（本批只授权了 `def == null && exit == null` 那条）。它们的效果是：
「别名表明明登记了，但那个节点上没有 Animator」⇒ 还是静默。
建议下一批一起收进 `AliasMiss`（口径已经现成了）。**只报不改。**

### ⑤-4 干跑基线陷阱值得写进 `资料/已知的坑.md`
E2 里我第一次做「修前」基线时，`lambda path: DEFAULT_MAX_DEPTH` 一把抹掉了**全部三条**放宽规则，
diff 出来 `collisionEvent` 那 355 个组件全进「新增」，看着像是我放开了几百个字段。
**判据：验证「给某一条路径放宽」的基线，必须只去掉那一条，不能把 `_depth_limit()` 整个短路。**
（这条在 E2 报告 ⑤·4 写过一次；这里再点一次，因为它差点让我误报「影响面巨大」。）

### ⑤-5 `WFEffectInfo.EffectName` 是同一族的第二个「剥名字」点
`WFEffectInfo.cs:45`：`displayName` 为空时退回 `gameObject.name` —— 与 `WarpforgeEffectPlayer` 同款兜底，
但它是**构建期**（`EffectLibraryBuilder`）读 prefab 资产，不经过 `Instantiate` ⇒ 目前无风险。
本批**没动**；若将来运行期也开始读它，记得走 `WarpforgeEffectPlayer.EffectName` 那套（别各写一份）。
