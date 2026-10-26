# B8_AS · 特效侧施工单（`WarpforgeVFX/**`）执行报告

> 执行代理 **B8_AS** · 2026-10-26
> 白名单 = `MyGame/Assets/WarpforgeVFX/**` + 账补丁 `账/_parts/B8_AS.tsv` + 本报告
> 输入（⛔ 未重查，只补证据）：`报告/B8_AB_特效动画C档.md` §五 转派单 · `报告/B8_J_特效动画与战斗界面判据.md` · `判据/战斗逻辑驱动_B2A_口径.md`
> 纪律：⛔ 未跑 Unity/批处理 · ⛔ 未碰 git · ⛔ 未改主表 · ⛔ 未越白名单

---

## ① 结论（先看这段）

| # | 施工单条目 | 处置 | 结果 |
|---|---|---|---|
| 1 | **`UIBorderGlow` 的 uv0.zw** | ✅ **落地**（uv 那一半） | 新建 `Runtime/UIViewportUvWriter.cs`（151 行）：`CalculateViewPosition` + 逐顶点写 **uv0.zw** + 网格重建后自动重写。判据 = `UIBorderGlow__CalculateViewPosition.c:15-48` / `__ModifyMesh.c:59-110` + DXBC（PS 读 `uv0.z/uv0.w`）。<br>⚠️ **`ImageQuad` 原生 `SetUVs(0, Vector4)` 口没做**（在 `CardPresentation/Battle/**`，白名单外）⇒ 转派，可直接粘的补丁写在 §三·1。 |
| 2 | **`UIEffectTogglerImage` 接线（2 实例）** | ⛔ **VFX 侧无落点 ⇒ 转派** | 组件本体在 `CardPresentation/Effects/`（逐句已对齐）。本轮把**两个实例的确切归属**现读出来了（§三·2）：`Done Highlight` → 卡组编辑 `DeckRuntime.cs:4869`；`Prebuilt Pack UI` → 轮抽 `DraftModeWindow.cs:2413`。 |
| 3 | **`AnimateUIMaterialProperty`** | ✅ **组件做出来了** | 新建 `Runtime/AnimateUIMaterialProperty.cs`（335 行）：8 个原版方法逐句 + 6 实例 / 4 用例的接线表 + 三个「属性到底在不在」的 shader 现读（§三·3）。<br>⛔ **3 条缺的 clip 我没生成**（要 Unity 导入 + Shell 接线，见 §三·4，口径与理由写在报告里）。 |
| 4 | **`OnAnimationEndEvent` 的 `_lifetime` 抬够没有** | ✅ **复核：已经抬够了，不用补** | `SwarmAnimationEnded()` 里 `if (_lifetime < _time + tail) _lifetime = _time + tail;`（`WarpforgeEffectPlayer.cs:874`）⇒ 本例 `natural 3.5` < `1.2 + 5` ⇒ 抬到 **6.2 s**；`preventDestroy` 档是 `PositiveInfinity`、比较恒 false ⇒ **不会压短**。已落账（补丁里 `SwarmVFXController.OnAnimationEndEvent()` 那行）。 |
| 5 | **判重族**（`CameraShakerManager` / `CameraVerticalFramer` / `PostFXController` / `ScenarioMaterialFader` / `CoffeeEffects` / `CardEffect`） | ✅ **查过：`WarpforgeVFX/**` 侧无一需要改代码** | 这些类的落点分别是 `BattlePostFx.cs` / `CardPresentation/Battle/ScenarioMaterialFader.cs` / 第三方包（未导入）/ `RuleEngine/**` —— **全部在白名单外**（§三·5 逐条注明归属）。VFX 侧唯一沾边的是 `WFModuleScreenShake.DoSimpleShake`（账 :130 C）—— 本代理现核：**组件侧逐句对齐、一行都不用改**（缺的是引擎→表现那 4 跳，已排给别人）。<br>顺带把账里 **两条 B 行过期的「差异」订正**了（`AnimFxModuleMoveParticlesToTarget.GetTarget` / `.Initialize`，§④·1）。 |

**数字**：新建 2 个 `.cs`（335 + 151 = **486 行**，另 2 个 `.meta`）· 账补丁 **12 行** · 代码里 `🔶 自己设计` 标记 **4 处**（2 个文件）· 净改既有文件 **0 处**。

**自检**：

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_b8as bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0** |
| `工具/check_patch_conflicts.py` | ✅ 无跨补丁重复键（5 份补丁） |
| `工具/check_a_vs_diff.py` | 30 条弱候选，**无一条来自本补丁**（本补丁 0 条 A 行） |
| `工具/verify_annot.py` | 缺注解 **0** |
| `工具/check_ledger_points.py 账/_parts/B8_AS.tsv` | A 0 条（本补丁无 A 行）⇒ 不涉 |
| `工具/merge_ledger_parts.py --dry --only B8_AS.tsv` | **更新 12 · 新增 0 · 无变化 0**（12 行全部按 `(原版类, 方法)` 命中既有行） |

---

## ② 改动清单

| 文件 | 改动 |
|---|---|
| `MyGame/Assets/WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` | **新建 335 行**（含 `.meta`，新 guid `c69dab8c…`）。原版 8 个方法逐句；`🔶 自己设计` 2 处 + 1 处接线帮手。 |
| `MyGame/Assets/WarpforgeVFX/Runtime/UIViewportUvWriter.cs` | **新建 151 行**（含 `.meta`，新 guid `1a263ad4…`）。`CalculateViewPosition` + uv0.zw 写入 + 变化检测。`🔶 自己设计` 2 处。 |
| `资料/完全复刻/账/_parts/B8_AS.tsv` | **12 行**（LF · 11 列 · 带表头）：4 条 `AnimateUIMaterialProperty` + 5 条 `UIBorderGlow`（落点/判据要点刷新，状态保持 C）+ 2 条 `AnimFxModuleMoveParticlesToTarget`（差异订正，状态保持 B）+ 1 条 `SwarmVFXController`（备注补 `_lifetime` 复核）。 |
| `资料/完全复刻/报告/B8_AS_特效侧施工单.md` | 本文件。 |

**没动**：`WarpforgeEffectPlayer.cs`（B8_AB 已经改过、本轮只复核）· 任何 `CardPresentation/**` / `RuleEngine/**` / `Editor/**` / `Shell/**` / `Net/**` / `工具/**` / `账/*.tsv` 主表。

---

## ③ 需要「别人做」的清单（全部带落点与判据）

### 3·1 `ImageQuad` 的原生 `SetUVs(0, Vector4)` 口（归 **`CardPresentation/Battle/**` 的写手**）

**为什么**：`ImageQuad.RebuildMesh()` 写的是 **2 分量** `m.uv = Vector2[]`（`ImageQuad.cs:RebuildMesh`）⇒ 原版 `UIBorderGlow` 要的 `uv0.zw` 永远是 0 ⇒ `Everguild/UI/UI Border Highlight Appear` 的扫光圆心恒在 `(0,0)`。

**我能做的已经做了**：`UIViewportUvWriter`（旁挂，零侵入）已在 `WarpforgeVFX/Runtime/` 落地 —— **现在就能用**：在 `UIBorderGlow` 那颗图所在 GameObject 上挂它 + 把材质换成原版那只 shader（`WarpforgeShaderMap.TryResolve("Everguild/UI/UI Border Highlight Appear", …)`，`Shell/ForgeTab.cs:1117` 已有先例）。

**更干净的做法（转派，若 Battle 写手愿意改 `ImageQuad`）**：在 `RebuildMesh()` 里改成四分量，并在 `SetUvRect`/布局变动时重刷：

```csharp
// 替换 `m.uv = new[]{ … }` 那一段（保留 xy 语义不变，另两个分量留 0）
m.SetUVs(0, new[] {
    new Vector4(_uv.xMin, _uv.yMin, 0f, 0f), new Vector4(_uv.xMax, _uv.yMin, 0f, 0f),
    new Vector4(_uv.xMax, _uv.yMax, 0f, 0f), new Vector4(_uv.xMin, _uv.yMax, 0f, 0f),
});
// 另加一个口给 uv0.zw（谁写谁负责，⛔ 不要在 ImageQuad 里算视口坐标）：
public void SetUv2(Vector2 zw) { /* 读回 uv0.xy + 写 zw */ }
```
⚠️ **两条都做会写两遍** ⇒ 真做了这一条，就把 `UIViewportUvWriter` 删掉（别两处都写）。

### 3·2 `UIEffectTogglerImage` 接线（归 **Shell / 卡组编辑 / 轮抽**）

组件本体 = `CardPresentation/Effects/UIEffectTogglerImage.cs`（逐句对齐）+ 驱动者 `UIEffectTogglerController.cs`（`Toggle(bool option, float duration = 0.3f, bool forceChange = false)`，`Awake` 里 `GetComponentsInChildren<UIEffectTogglerBase>(true)`）。

**原版两个实例的真身（本轮现读，`bundle_menus_assets_all`）**：

| 实例 | 组件所在 GO | `images[]` 指向 | 我们的对应节点 |
|---|---|---|---|
| **`Done Highlight`** | 自己（`Image, UIEffectTogglerImage, EverguildButtonMaterialModifier`，`rot=90°`，81.36×223.22，`deactivateOnDisable=1`） | **`images[0]` = 同一个 GO 上的 Image** ⇒ **同节点** | `CardPresentation/Deck/DeckRuntime.cs:4869` 的 `SetOn(_doneHl, err == DeckError.None)`（今天硬切） |
| **`Prebuilt Pack UI`** | 根（`PrebuiltPackUI, UIEffectTogglerController, UIEffectTogglerImage`） | **`images[0]` = 子件 `Highlight`**（`GO 3832911137508339186`）⇒ **不是同节点** | `CardPresentation/Shell/DraftModeWindow.cs:2413` 起的 `BuildPackCard`，那颗 `MenuDraw.Nine(… "Highlight" …)` |

**接线写法（一行）**：
```csharp
var t = node.AddComponent<UIEffectTogglerImage>();      // Done Highlight：不用填 fadeTargets（同节点自动收集）
t.fadeTargets.Add(highlightNode);                       // Prebuilt Pack UI：必须显式给子件 Highlight
var ctl = node.AddComponent<UIEffectTogglerController>();
ctl.Toggle(on);                                         // 替换原来的硬 SetActive / SetOn
```
**还差（我查不到 / 不在白名单）**：原版这两处的 `Toggle(option, duration)` **实参**（默认 0.3 s？）与**触发时机**（`PrebuiltPackUI.OpenAnim/SelectAnim/CloseAnim` 里哪一拍）我没读方法体 ⇒ 接线方按 `PrebuiltPackUI`/`UIEffectTogglerController` 的调用点再核一次。

### 3·3 `AnimateUIMaterialProperty` 的接线（归 **Shell**；组件已就绪）

**组件**：`WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`，API：
```csharp
AnimateUIMaterialProperty.Attach(quad,                        // Renderer（ImageQuad 的 MeshRenderer）
    AnimateUIMaterialProperty.Slot("_Distort_Scale", 0f),     // floatProperty1
    AnimateUIMaterialProperty.Slot("_Distort_Strength", 0f)); // floatProperty2（可省）
AnimateUIMaterialProperty.SlotV4("_Noise_Strength_X_Speed_Y_Scale_Z", new Vector4(0.02f, -0.1f, 8.17f, 0f));
```

**6 个出厂实例（`UnityPy` 现读）→ 我们的 4 个用例 → 属性在不在（本轮新查的 shader 现读）**：

| # | 原版 GameObject | floatProperty1 | floatProperty2 | 驱动它的 clip | **那块图原版用的 shader**（本轮现读） | 属性在不在 |
|---|---|---|---|---|---|---|
| 1 | `CardFrame`（`m_Enabled=0`） | `_DissolveAmount` 0 | — | `Card Hand To Board`（**已导入**） | —— | ✅ 但**别接**：`CardView`/`CardFeel` 已硬编码驱动这一层（接了会写两遍） |
| 2 | `Booster pack Background` | `_Distort_Scale` 0 | `_Distort_Strength` 0 | `Booster Window - Background Shake On Open`（**已导入**） | **`Shader Graphs/Nebula`**（`bundle_menus_assets_all/Material/Material_-1341631206307795618.json`） | ✅ 两个属性都在该材质的 `m_Floats` 里 |
| 3 | `Rank Icon` | `_Blend` 0 | — | `Rank Up`（**工程里没有**） | **`Everguild/UI/Division Change`** | ✅ `_Blend` 在 |
| 4 | `Division Image` | `_Blend` 0 | — | `Division Up`（**工程里没有**） | **`Everguild/UI/Division Change`**（与 Rank Icon 同一只） | ✅ `_Blend` 在 |
| 5 | `Level Up Effect` | `_Reveal` **−0.5** | — | `Level Up Card`（**工程里没有**） | 两者共用一份**外部**材质（`m_FileID 5 / m_PathID 4`）—— **shader 名没解**（要再解 scene 的 externals 第 5 项） | ？ |
| 6 | `Level Up Effect Down` | `_Reveal` **−0.5** | `_Noise_Strength_X_Speed_Y_Scale_Z` **v4(0.02,−0.1,8.17,0)** | 同上 | 同上 | ？ |

🔴 **这条现读推翻了一句旧结论**：`Shell/BoosterPackOpenWindow.cs:95` 写着「`floatProperty1/2` 是材质属性，**原版 shader 的逻辑本地没有** ⇒ 我们没有等价物可依」——
**属性与 shader 名都在本地解包里**（`Shader Graphs/Nebula` · `Everguild/UI/Division Change`），只是**我们没把它挂上去**。⇒ 建议 Shell 侧把那条注释改了（不是「本地没有」，是「没接」）。

### 3·4 需要导入的 3 条 clip（要 **Unity 批处理**，只有调度台能跑）

| clip | 出处（原版资产） | 曲线键值（已导出） | 我们的落点 |
|---|---|---|---|
| `Rank Up` | `bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json` | `_tmp_view/b8ab/animate_ui_clips.json` 的 `menus/rankup` | `WarpforgeVFX/Animations/Rank Up.anim` |
| `Division Up` | `…/AnimationClip_6413171015603015678.json` | 同上 `menus/divisionup` | `WarpforgeVFX/Animations/Division Up.anim` |
| `Level Up Card` | `bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json` | 同上 `mainmenu/levelup` | `WarpforgeVFX/Animations/Level Up Card.anim` |

**生成口径** = 照 `WarpforgeVFX/Animations/Booster Window - Background Shake On Open.anim` 那一份（`m_Legacy: 1` · `m_Compressed: 0` · `m_UseHighQualityCurve: 1` · `m_WrapMode: **0**` · `m_SampleRate: 60` · `m_StopTime: **1**`；每条曲线 `inSlope = outSlope =` JSON 里的 slope、`tangentMode 0`、`in/outWeight 0.33333334`、`m_PreInfinity/m_PostInfinity 2`、`m_RotationOrder 4`；MonoBehaviour 曲线 `classID: 114` + `script: {fileID: <pid>, guid: 000…0, type: 0}` + `flags: 16`）。
⚠️ **两条前提（接线方必须知道）**：
1. 这 3 条 clip 里**只有 `floatProperty1/2.value` 那几条曲线在我们这边能落地**；其余曲线绑的是 **uGUI 的属性**（`m_Color.a` / `m_fontColor.a` / `m_Alpha`）⇒ 我们是 `ImageQuad`/`Label`，**这些曲线会静默不生效**。要它们也生效，得 Shell 侧另做（那不是本组件的事）。
2. `Rank Up` / `Division Up` 各带一个**动画事件** `AnimationFinished`，`Level Up Card` 带 `ChangeFrame` + `AnimationFinished` —— 原版由 `CardDisplayLevelUpAnimationController` 接（账 :177–:185，归 Shell）。

### 3·5 判重族逐条归属（本代理查过、都不在 `WarpforgeVFX/**`）

| 类 | 我们的落点 | 归属 |
|---|---|---|
| `CameraShakerManager.DoSimpleShake` | `WarpforgeVFX/Runtime/WFModuleScreenShake.cs:209-216`（**逐句对齐 · 一行不用改**）；缺的是调用点（引擎 3 处 emit + `BattleDriver` 1 处消费） | 引擎线 + F5（AB 报告 §五·A 已列） |
| `CameraVerticalFramer` | `CardPresentation/Battle/CombatAutoZoom.cs` / `BattleCameraSreenSize.cs` | 战斗线 |
| `PostFXController.ResetBloom/ResetLUT` | `CardPresentation/Battle/BattlePostFx.cs:356-369` | 战斗线 |
| `ScenarioMaterialFader` | `CardPresentation/Battle/ScenarioMaterialFader.cs:54-113` | 战斗线 |
| `UIEffectTogglerCoffeeEffects` | **无**（第三方 `Coffee.UIEffect` 未导入；shader 只有 1 只 `Hidden/UI/Default (UIEffect)`） | Shell + 资源导入 |
| `CardEffect.IsEnchantment/IsSameEffect/…` | `RuleEngine/**` | 引擎线 |

---

## ④ 没查清的部分（如实记）

1. **`AnimFxModuleMoveParticlesToTarget` 的两条 B 行我没升 A**：账里原来的「差异」是**过期的**（说「相机换算没核对」，其实 `UpdateTargetPosition` 2026-10-01 就实现了）—— 本轮**只订正文字、状态仍 B**，理由是 **G3 没有观测量**（本代理不跑 Unity，`ConvertPositionBetweenCameras` 的两侧读数拿不到）。**还差的一段**：原版什么时候调 `UpdateTargetPosition` —— 全量反编译**零调用者** ⇒ 我们每帧刷（文件头已标「这是我们定的」）。
2. **`Level Up Effect` / `Level Up Effect Down` 的 shader 名没解**（两者共用外部材质 `m_FileID 5 / m_PathID 4`，要再解 `scenes_scenes_mainmenuwarpforge` 的 externals 第 5 项）。⇒ 这两件的 `_Reveal` / 噪声 vector 到底喂给哪只 shader **未证**。
3. **`AnimateUIMaterialProperty` 空槽判定「逐字节等价」仍未证**（原版 `struct.Equals(default)`；G12 只读到 `il2cpp_value_box + ValueType.Equals` 的调用形状）。出厂 6 实例的空槽都是 `materialProperty: ""` 这一档 ⇒ 两种判法可见结果相同；已按 `🔶 自己设计①` 标在代码里。
4. **组件行为一个字节都没跑过**（不跑 Unity）⇒ `AnimateUIMaterialProperty` / `UIViewportUvWriter` 都是**编译通过 + 逐句对齐**，**不是**运行验证过。观测量与读法见 §⑥。
5. **`preventDestroy` 档下 `SwarmAnimationEnded` 会不会把 `PositiveInfinity` 压短** —— 我按代码读（比较恒 false、不会）判的，**没跑**；若真有 `preventDestroy` 且同时收到这个动画事件，原版也是 `Destroy(go,5f)`（原版不受 `preventDestroy` 管）⇒ **这条语义我们与原版可能不同**，未证实、未改（记在这里）。
6. **`UIEffectTogglerImage` 两处的 `Toggle` 实参与触发时机**（见 §3·2 末）没读方法体。

---

## ⑤ 顺手发现（都有出处）

1. 🔴 **`Shell/BoosterPackOpenWindow.cs:95` 与 `Shell/BoosterPackAnim.cs:14` 的「原版 shader 的逻辑本地没有」是错的**（铁律 5）：
   `Booster pack Background` 的材质（`Material_-1341631206307795618`）**在解包里**，shader = **`Shader Graphs/Nebula`**，`m_Floats` 里就带着 `_Distort_Scale` / `_Distort_Strength`。
   同理 `Rank Icon` / `Division Image` → **`Everguild/UI/Division Change`**（也是本地的）。
   ⇒ 「拿不到判据」这件事不成立，只是**没接**。⚠️ 这两处在别人白名单，**我没改**。
2. 🟡 **`Rank Icon` 与 `Division Image` 用的是同一只 shader**（两件材质的浮点属性表逐项相同，shader PPtr 同 pid）⇒ 接线时只需解析一次 shader。
3. 🟡 **`CardFrame` 上那个原版组件 `m_Enabled = 0`**（`details.json` 第 1 条）⇒ 原版卡框那边**根本不靠这个组件跑**（`OnEnable`/`Update` 都被禁）。这独立佐证了 AB 的「别给它再挂一个」——而且说明 `CardView` 的硬编码路才是主线。
4. 🟡 **`UIBorderGlow.ModifyMesh` 里没有顶点重排**：账 `特效动画.tsv` 那一行的「判据要点」原写「按 `_Reveal` 重映射边框顶点（消费点在本方法）」—— **方法体逐句读下来只有 uv0.zw 两句改动**（`__ModifyMesh.c:59-110`，其余是把 uv0.xy/position/normal/color 原样搬过去）。重映射在 **PS** 里（`_Reveal` 是 UnityPerMaterial 的 float）。已在本轮补丁里订正那行的判据要点。
5. 🟡 **`Done Highlight` 在解包里**带 `EverguildButtonMaterialModifier`（第三颗组件）—— 我们的 `_doneHl` **没有**这个材质修饰器 ⇒ 就算接了 toggler，**「描边材质」那一层仍缺**（另开账，属卡组编辑线）。本轮没动。
6. 🟡 **`_parts/` 里旧的补丁正在被归档**（`B4_*` / `B5_*` / `B6_*` 等 35 个已 `D`、`_merged_B8_*` 一批 `??`）—— 这是调度台自己的收口动作，我**没碰**；只是提醒合并时别把已归档的再算进去（`merge_ledger_parts.py` 只认 `B<数字>_*.tsv` 且会撞同名键，务必带 `--only`）。

---

## ⑥ 给调度台的请求

1. **合并顺序**：`python -I -X utf8 工具/merge_ledger_parts.py --dry --only B8_AS.tsv` 已验 **更新 12 / 新增 0 / 0 冲突**；正式合并时**必须带 `--only B8_AS.tsv`**（`_parts/` 里还有 `B8_AM/AP/AQ/AR` 在同一目录）。
2. ⚠️ **与 F7（Shell）可能重叠的 4 行**：账 84 / 86 / 87 / 88（`AnimateUIMaterialProperty`）与 425 / 430 / 432 / 433 / 438（`UIBorderGlow`）原本在 AB 的转派单里都写着「归 F7·Shell」。**我改的是「我们的落点」这一列（指向新组件）**，F7 若同时改这几行会撞键 —— 请按 `check_patch_conflicts.py` 的结果排定谁先合并。
3. **要人做的 4 件**（都不在我白名单）：
   - `ImageQuad` 的原生 Vector4 uv 口（§3·1）—— 做完就**删掉** `UIViewportUvWriter`；
   - `UIBorderGlow` 的材质换成原版 shader + 挂 `UIViewportUvWriter`（Shell）；
   - `UIEffectTogglerImage` 两处接线（卡组编辑 / 轮抽，§3·2）；
   - `AnimateUIMaterialProperty` 三处（开包背景 / 段位两张图）接线（§3·3）。
4. **一次 Unity 批处理（只有你能跑）**：导入 §3·4 那 3 条 clip（若采纳「用同一套生成器手写 `.anim`」这条路，**不用**批处理 —— 但接线仍要 Unity 才能把 clip 塞进 `Animation` 组件的 `m_Animations`）。
5. **建议的一条可观察量（留给未来的探针/真 Play）**：
   - `AnimateUIMaterialProperty`：接好后读 `comp.InstancedMaterial.GetFloat("_Blend")` / `GetVector("_Noise_…")` 在 clip 各关键帧时刻的值（原版期望 = clip 的关键帧表，见 `_tmp_view/b8ab/animate_ui_clips.json`）。
   - `UIViewportUvWriter`：`mesh.GetUVs(0, list)` 的 `zw` 应 == `Camera.main.WorldToViewportPoint(quad.transform.position)`（原版同式；无相机 = `(0.5,0.5)`）。
   这两条都**没跑过**（本代理不跑 Unity）⇒ 账里对应行保持 C，⛔ 我没把它们硬改 B/A。

---

## 附：本代理动过的文件（全清单）

| 文件 | 状态 |
|---|---|
| `MyGame/Assets/WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` | 新增 335 行 |
| `MyGame/Assets/WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs.meta` | 新增（guid `c69dab8c0b494019bb4e17a0d92b9e79`） |
| `MyGame/Assets/WarpforgeVFX/Runtime/UIViewportUvWriter.cs` | 新增 151 行 |
| `MyGame/Assets/WarpforgeVFX/Runtime/UIViewportUvWriter.cs.meta` | 新增（guid `1a263ad489bf44ee84502fe258366c9e`） |
| `资料/完全复刻/账/_parts/B8_AS.tsv` | 新增 12 行 + 表头 |
| `资料/完全复刻/报告/B8_AS_特效侧施工单.md` | 本文件 |
| `_tmp_view/b8as/mk_patch.py` | 账补丁生成脚本（临时目录，可复跑） |
