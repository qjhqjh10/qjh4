# X10 · GrabScreen 501 启用（两步前置 + 拷屏 RT ×0.25）—— 执行报告

> 2026-10-25 · 执行代理 · 判据沿用 `任务线/特效线/任务总表.md:28` 新账 + `普查产出_1025/AUD3_特效工具自检音效核查.md:13`。
> **一句话**：501 缺的两步（清 `m_TransparentLayerMask` 位 / 233 个失真效果挪层）＋ 原版 RT ×0.25 全部落成代码，
> **默认关、`WF_GRAB501=1` 一条命令全开**；默认档逐位等价（§4 逐条论证）。**没跑 Unity**（主对话统一跑）。

## 1. 两步各改了什么（file:line）

- **① 清透明层掩码位**（运行期、不改资产）：`rendererDataList` 里每个 `UniversalRendererData` 的 `transparentLayerMask`
  清掉 501 那一层 —— `GrabPassTransparentFeature.cs:514`（`EnsureTransparentMaskCleared`）· 调用点 `:136`（每帧调、只生效一次）。
- **② 失真效果挪层**：`FinalLayer(go,wantedLayer)` 是失真效果就返回 501 层、否则**原样返回**，并登记「源层」——
  `:553` · 判据 `UsesDistortion` `:597` · 调用点 `WarpforgeEffectPlayer.cs:284` **＋** `WarpforgeEffectHook.cs:48`
  （后者原来会把层刷回父层，**必须同时过一遍**，否则 501 画不到）。
- **②·附 给相机补 `cullingMask`**（层是两套过滤：相机看不看得见 / 透明 pass 画不画；只清后者物体从**所有**相机消失）——
  `:572`（只补「原本画得到源层」的相机，避免被两台相机各画一次）。
- **③ 拷屏 RT ×0.25** —— `:221`（`GrabPassPass.RecordRenderGraph`，**只在 501 档生效**）。**开关** —— `WF_GRAB501` / `WF_GRAB501_LAYER`(默认 9) / `WF_GRAB501_RT_SCALE`；旧名 `WF_GRAB_501(_LAYER)` 仍认；解析在 `:426`（`GrabScreen501`）。

**为什么①能运行期生效**（读的是本工程 `Library/PackageCache` 的 URP 源码）：`UniversalRendererData.transparentLayerMask`
有 **public setter**（`UniversalRendererData.cs:197`）→ `SetDirty()`（`ScriptableRendererData.cs:59`）→ `isInvalidated`
→ `UniversalRenderPipelineAsset`（`:902`/`:945`）下一帧 **销毁并重建 renderer** → `UniversalRenderer.cs:308/:430` 用新掩码重建
`DrawObjectsPass`。⇒ **不用反射、不改 `PC_Renderer.asset`**（PC+Mobile 两份 data 都清）。⚠️ 改的只是内存对象、**不落盘**
（URP 的 `SetDirty` ≠ `EditorUtility.SetDirty`，`git diff` 看不到 `PC_Renderer.asset`）。

## 2. 「233 个失真效果」怎么数据驱动

**没有名单、没有生成器、没有魔法数 233**。按效果名缓存（同名 = 同一 prefab，只扫第一次），判据取两处真实引用：
① `WarpforgeEffectBinder.materials[].shader` —— 导出器烘进来的**原版材质槽**，正是
`grep "Particle Distortion Affect Transparents" Prefabs/ | wc -l = 233` 数的那一列（只读，不影响渲染）；
② 运行时材质 `shader.name` —— 命中**原名**或 `WarpforgeShaderMap.Replacements[原名]`（替代名不在这里抄第二遍）。
⇒ 实跑时数量**自己报**：`RelocatedCount` ＋ 每效果一行 `失真效果《…》搬到第 9 层（源层 L）`。**无数据产物**，故「生成器连跑两次 md5 / `--check`」**不适用**（`工具/gen_*.py` 里也没有这张表）。

## 3. RT ×0.25（判据与主对话复核一致，本地实读同）

- `GrabScreenFeatureRenderGraphAPI.Settings__.ctor.c:11` 与 `GrabScreenFeature.Settings__.ctor.c:11`：
  `*(undefined4 *)(param_1 + 0x1c) = 0x3e800000;` = **0.25f**（`textureSizeMultiplier` 默认值）。
- `…GrabPass__RecordRenderGraph.c`：`iVar2=(int)((float)width * *(float *)(lVar3+0x1c)); if (0<iVar2) 宽=iVar2;`（高同款）⇒ 我们写 `Mathf.Max(1,(int)(desc.width*s))`。🔴 `AUD3` 的「41 个 `.c` grep `0.25` 0 命中」是**搜索形态的假阴性** —— 反编译里是**十六进制浮点字面量**，十进制串永远搜不到（与铁律 2「没找到 ≠ 不存在」同族，已写进代码注释防复发）。

## 4. 默认档（不设 `WF_GRAB501` / `=0`）为什么**逐位等价**

1. 四个入口（`EnsureTransparentMaskCleared`/`EnsureCameraSeesRelocated`/`FinalLayer`/静态解析）第一行都是 `if (!Enabled) …`；`Enabled` 是 `static readonly bool`、**只来自 `WF_GRAB501`**（与旧开关 `WF_GRAB_501` 无关）⇒ 不设 = 这三件**一行都不执行**（连日志都不打）。
2. `RtScale` 关着时**恒 1f** ⇒ `if (!Mathf.Approximately(rtScale,1f))` 整段跳过，`desc` 的 `width/height` **一个字段都不写**（与改前 `GetTextureDesc` + `name/clearBuffer/useMipMap` 同序同值）。
3. `RedrawEnabled()` 关着时仍是旧逻辑（最前面只多一句 `if (Enabled) return true;`，它为 false）⇒ 老 A/B 行为不变；`Play`/`Hook` 只多一句 `wantLayer = GrabScreen501.FinalLayer(go, wantLayer)`，关着时**原样返回入参** ⇒ 后续 `SetLayerRec` 同值同分支。
5. **`git diff` 自查 500 那条路**：整份 feature 只删 **8 行** —— 1 行告警文案、1 行 doc、6 行文件尾注释；**`GrabPassPass` 代码行一行没删**（新增 RT 那 4 行全在第 2 条的开关后面）。`EnqueuePass(_pass)` 原样。

## 5. A/B 两条命令（主对话照着跑）

```bash
P=d:/4/_tmp_view/player/WarpforgePlayer.exe
WF_ARENA=battlearena1 "$P" -wfscene Battle -wfdrive                                    # A 档 = 改前（默认档）
WF_ARENA=battlearena1 WF_GRAB501=1 "$P" -wfscene Battle -wfdrive                       # B 档 = 501 全开
WF_ARENA=battlearena1 WF_GRAB501=1 WF_GRAB501_RT_SCALE=1 "$P" -wfscene Battle -wfdrive  # 可选：只验清位+挪层
WF_ARENA=battlearena1 WF_GRAB501=1 WF_GRAB501_LAYER=9 "$P" -wfscene Battle -wfdrive     # 可选：显式指定层
```

B 档**该看到的三行日志**：`已把第 9 层从 K 份 RendererData 的 m_TransparentLayerMask 里清掉` · `失真效果《…》搬到第 9 层（源层 L）`（每效果一行）· `相机「…」的 cullingMask 补上第 9 层`（每相机一行）。

**typecheck 读数**：`TMPDIR=/tmp/wf_b14 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**（改完连跑三次，末次仍 0/0）。行尾自查：三份都仍是 **LF**；`git diff --numstat` = **326/8 · 8/1 · 6/1**（无整篇翻行尾）。

## 7. 没做 / 没查清

- **没跑 Unity**（红线）⇒「501 到底画出来没有 / RT×0.25 观感 / 233 实际搬了几个」**要主对话跑 A/B 才有读数**。
- **层号是我们挑的（9）**：原版 `Settings.LayerMask` 取哪层**查不到**（原版 `UniversalRendererData` 资产本地没解出来）。理由：全仓资产只用过 0/1/5/6/8/10/20 层，9 空着；⛔ 没用 31（`1<<31` 会让 `LayerMask` 变负数，`FilteringSettings` 对负值的处理看不到源码）。
- **`EffectIso` 没接**：它自己 `Object.Instantiate`、不走 `Play` ⇒ 自动档对它**零影响**；但要**在 EffectIso 里量 501 得另配**（它渲前把实例刷到隔离层 30、`cullingMask=1<<30`，与挪层互斥）。**`CardView.SetLayer`（出牌搬卡）会把已挂在卡上的子件刷回卡层** ⇒ 未播完的失真效果会退回卡层（不再走 501）；该文件在黑名单，没改（`CardView.cs:2196-2206`）。
- **没查清**：`FilteringSettings` 是否对负 `layerMask` 有「-1 = 全部」特判（源码不在 core 包里）—— 选 9 层绕开。

## 8. 顺手发现

1. `UniversalRenderer.transparentLayerMask` 是 **internal 且只在构造函数里从 data 缓存一次**（`UniversalRenderer.cs:240/308`）⇒「改 data 的 public 属性 + SetDirty 触发重建」是**唯一**不靠反射的路。
2. `WarpforgeEffectHook` 在 `Play` **之后又刷一次层**（`WarpforgeEffectHook.cs:46-48`）⇒ 任何「Play 里搬层」的新逻辑都必须同时改这里，否则被静默盖掉（这次差点漏）。
3. **⛔ 批处理里别设 `WF_GRAB501=1`**：没有帧循环 ⇒ 相机掩码补不上、而效果已被搬到 9 层 ⇒ 那些图整块空掉（还会让 `RootVfxLayerAssert` 那类「层要落在战场相机掩码里」的断言红）。已写进文件尾 §⑤。
