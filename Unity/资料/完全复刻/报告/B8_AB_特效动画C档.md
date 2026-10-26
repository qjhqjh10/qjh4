# B8_AB · 特效动画域 C 档（`WarpforgeVFX/**` 侧）执行报告

> 执行代理 **B8_AB** · 2026-10-26 · 只改 `MyGame/Assets/WarpforgeVFX/**` + 账补丁 `账/_parts/B8_AB.tsv`
> 判据输入（⛔ 未重查，只补证据）：`判据/B8_特效动画与战斗界面_判据表.tsv` · `判据/战斗逻辑驱动_B2A_口径.md` · `判据/B8_三条缺口_判据与资源.md` · `账/特效动画.tsv`

---

## 一、结论（先看这段）

**本域 C 档 48 条的处置**（逐条清点，不重不漏）：

| 处置 | 条数 | 账行（`特效动画.tsv` 物理行） |
|---|---|---|
| **C → D**（原版本身就没有，铁律 11 例外①） | **6** | 290 / 291 / 292 / 293 / 294 / 297（`ObjAnimEffects` 全族） |
| **转派**（落点在别人白名单，本代理不写代码） | **37** | 130 · 84/86/87/88 · 150/154/155 · 175 · 177–182/184/185 · 187–190 · 208 · 340/341 · 362/367 · 403 · 425/430/432/433/438 · 455 · 462–465 |
| **P3，等调度台裁定**（本代理不自行判 D） | **5** | 381–385（`SegmentationController`） |
| 合计 | **48** | ✔ 与 `ledger_slice --domain 特效动画 --status C` 一致 |

**本代理实际落地的两件**（其余是核实 + 施工图）：

1. **`ObjAnimEffects` 全族 → D（6 行）**：全 build **0 个实例**、**0 处代码引用** ⇒ 原版这条链一次都不会走到。证据链见 §二（机器级：84 个 bundle / 66,459 个 MonoBehaviour / 722 份 bundle 内置 MonoScript 全解析，`ObjAnimEffects` 不在其中）。**同时推翻了「9 个 prefab 的 `AnimEffectData` 数组」这个既有说法**（无出处、与扫描冲突）。
2. **`SwarmVFXController.OnAnimationEndEvent` 的 5 秒尾巴（账行 404，B 档）**：原版是 `Destroy(gameObject, **5f**)`，我们是**立即销毁** ⇒ 比原版少活 5 秒（≈ 6.2 s vs 1.2 s）。已按判据改（`WarpforgeEffectPlayer.cs`，见 §三）。

**另交付一份判据成果**（§四）：`AnimateUIMaterialProperty` 的 **6 个实例 / 4 个用例 / 5 条驱动 clip 的逐键曲线** —— 这是账行 84/86/87/88 缺的那一层（账上只写了「覆盖 1/3 用例」），F7 拿到就能直接施工。

**自检**（全部通过）：

| 项 | 结果 |
|---|---|
| `bash 工具/typecheck.sh`（`TMPDIR=/tmp/wf_b8ab`） | **运行时 0 · 编辑器 0** |
| `工具/check_ledger_points.py` | 191 条漂移**全是别人在飞的文件**（`CardView.cs` / `NetBattle.cs` / `MainMenuRuntime.cs`…）；**`WarpforgeVFX` 一条都没有**（本文件落点 0 漂移，见 §三） |
| `工具/check_patch_conflicts.py` | ✅ 无跨补丁重复键（4 份补丁） |
| `工具/verify_annot.py` | 缺注解 **0**（本补丁无 A 行，不涉） |
| `工具/check_a_vs_diff.py` | 29 条弱候选，**无一条来自本补丁**（同上） |

---

## 二、`ObjAnimEffects` 六行 C → D（证据链）

### 2·1 判据侧（原版方法体，已读；账行「判据要点」保留不动）

`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs`（🥈 有真体，346 行）+ `d:/2/tools/decomp_full/ObjAnimEffects__*.c`（🥇 8 个方法体 + 1 个状态机全在）。语义无需再查；`AnimEffectType` = `Undefined 0 / Fade 10 / AnimPrefab 20 / PlaySound 30 / ScreenShake 40`。

### 2·2 可达性：四段独立证据，全指向「0 次」

| # | 查法 | 结果 |
|---|---|---|
| ① | 84 个 bundle 的 **66,459 个 MonoBehaviour** 的 `m_Script` 逐个解析到 **bundle 内置的 722 份 MonoScript** | **66,458 命中 + 1 个 `m_Script` 为空，0 个未解析**；**`ObjAnimEffects` 不在这 722 份里** ⇒ 任何 bundle 里的对象都不可能挂它 |
| ② | 主文件 `level0` / `level1` / `sharedassets0.assets` / `sharedassets1.assets` / `resources.assets` / `globalgamemanagers.assets` 里 `m_Script` = `ObjAnimEffects`（PathID **4383**，从 `globalgamemanagers.assets` 的 4,453 份 MonoScript 里按 `m_ClassName` 找到） | **0 个**（同一查法在 `level0` 上能正确解出 `SteamManager` / `PlayFabHttp` …，**方法自证有效、miss=0**） |
| ③ | `grep -rl ObjAnimEffects d:/2/tools/decomp_full/` | 只命中**本类自己的 8 个方法体 + 1 个状态机**；**0 处外部引用**（没有 `GetComponent` / `AddComponent` / `FindObjectsOfTypeAll` / `Instantiate`） |
| ④ | 解包镜像全库 `grep -rl "animEffects"` / `"waitToPlayEffects"` | **0 命中**（同一批 JSON 的 type tree 是**在的** —— 例如 `Bloom.json` 的 `threshold` 等字段名都解出来了；旧镜像 `d:/2/解包整理/` 也是 **0**） |

⇒ 结论：**该类在本 build 里既没有序列化实例，也没有任何代码/字符串指向它 ⇒ 不可能被创建**（唯一的创建路是序列化或 `AddComponent`，两条都空）。按 **铁律 11 例外①（原版本身就没有）** 判 **D**。
✅ 与既有先例同口径：`CameraShake` 一族（账 104–109）就是「0 实例 + 0 调用点 ⇒ D」。

### 2·3 🔴 铁律 5 更正：「9 个 prefab 的 `AnimEffectData` 数组」

账/报告里流传的那句话（`报告/B8_J_特效动画与战斗界面判据.md:54`、`报告/G12_第6轮.md:262`）**本轮找不到出处**，而且与上面 ④ 直接冲突（镜像里 `animEffects` 0 命中）⇒ **该说法已推翻**，⛔ 下一批不要再照着「导出 9 个 prefab 的数据」开工。
辅证：`AnimEffectData` 全工程**只被 `ObjAnimEffects` 引用**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 里 `grep -rn AnimEffectData` 只命中 `ObjAnimEffects.cs` 自己）⇒ 不存在「第二个消费者」。

### 2·4 复跑方法（脚本在临时目录，方法写在这里以免丢失）

`d:/4/Unity/_tmp_view/b8ab/`：`scan6.py`（①② 的覆盖性证明）· `scan3.py`（主文件）· `ms2.py`（导出 722 份 MonoScript 的 `path_id → 类名` 表）· `count_classes.py`（§七 的实例计数表）· `dump_details2.py`（§四 的实例字段）· `dump_clips2.py`（§四 的曲线）。
核心三步：① `UnityPy.load(Warpforge_Data/globalgamemanagers.assets)` 取 `m_ClassName == "ObjAnimEffects"` 的 MonoScript（PathID 4383）；② `UnityPy.load(Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/Waprforge_monoscripts.bundle)` 取 **bundle 内置**的 722 份 MonoScript 的 `path_id → 类名`；③ 遍历 84 个 bundle 的 MonoBehaviour，把 `m_Script.m_PathID` 查第 ② 张表（**⛔ 不能只查第 ① 张**：bundle 里的 `m_Script` 用的是 bundle 内置副本的大 PathID，直接拿 4383 去比会得到假 0 —— 本代理第一版就是这么错的，见 §六·1）。

---

## 三、代码改动（唯一的 `.cs` 改动）

**文件**：`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs`
**净新增 30 行，全部落在类尾（`Describe` 之后，行 851 起）** ⇒ 账里指向本文件的 **10 个落点（191 / 354 / 413 / 483 / 556 / 581 / 609 / 632 / 777 / 801）一个都没漂**（已逐行回读确认）。

| 位置 | 改了什么 |
|---|---|
| `:639`（原地改写，行数不变） | 原版第 ③ 条从 `Object.Destroy(gameObject)` 订正为 **`Object.Destroy(gameObject, 5f)`** |
| `:642`（原地改写，行数不变） | 🔴 就地订正旧结论「我们比原版多活 2.3 s」——**那条是错的**（按「原版立即销毁」推的）；原版是 5 秒尾巴 ⇒ 我们其实**少活 5 秒** |
| `:662-664`（3 行换 3 行） | `OnAnimationEndEvent()` 的体从 `DoDestroy()`（立即）改为 `SwarmAnimationEnded()` |
| `:853-880`（类尾新增） | `void SwarmAnimationEnded()` + `public const float SwarmAnimationEndTail = 5f;`，含判据、字面量、仍差两处的说明 |

**判据（机器级，逐字读过）**：
- 🥇 `d:/2/tools/decomp_full/SwarmVFXController__OnAnimationEndEvent.c:3-27`：`onFinish?.Invoke()` → `StopAllCoroutines()` → `UnityEngine_Object__Destroy(gameObject, DAT_1834b2df4, 0)`
- 字面量现读：`工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b2df4` ⇒ **4B float = 5.0 ✅**（8B = 5.3568e-315 ⇒ 垃圾，选 4B）
- 为什么必须同时抬 `_lifetime`：该 effect 在我们库里是 `destroyTime -1 / modules [] / natural 3.5 s`（`Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 的 `Swarm Card Merge VFX`），而原版那份 prefab **没有 `AnimFXController`** ⇒ 原版根本没有计时销毁。不抬寿命的话 3.5 s 会先到、5 秒尾巴**等于没接**（典型的「改了但看不出来」）。

**机制差异（G2）**：动画事件（`Swarm Triat Merge.anim` t = 1.2 s）之后，原版该 GameObject 再活 **5 s**（≈ 6.2 s 才消失）并**继续发粒子**；我们原来在 1.2 s 当场销毁。

**可观察量（G3，留真 Play；本代理不跑 Unity）**：动画事件之后 `WarpforgeEffectPlayer.ActiveCount` 应保持 `1` 直到 **t ≈ 6.2 s**；原版同刻同口径。**未跑** ⇒ 账行 404 **保持 B**（没把 C/B 硬改 A）。

**仍差（如实记，⛔ 别当成已对齐）**：① `onFinish` 回调无对应物（原版闭包接 `CardScript.ExecuteSwarm` / `Battle/**`）；② `SafeExit` 看门狗（4 s 内没收到事件 ⇒ `LogError` + 回调 + 5 s 尾巴）没做 —— 我们靠自然寿命兜底（这就是账行 405 判 D 的依据，仍然成立）。

---

## 四、`AnimateUIMaterialProperty` 判据补齐（账行 84/86/87/88 缺的那一层，给 F7）

账上写「覆盖 1/3 用例」。实测 = **6 个实例 / 4 个用例**，且 **5 条驱动它的 clip 我们只导入了 2 条**。

### 4·1 六个实例（`UnityPy` 现读序列化字段，非推测）

| # | GameObject | 所在 bundle | `initializeOnEnable` | `floatProperty1` | `floatProperty2` | `changeMaterialOnAnimation` |
|---|---|---|---|---|---|---|
| 1 | **CardFrame** | battleprefabs_vfxandmisc | 1 | `_DissolveAmount` 0 | （空） | **非空** |
| 2 | **Booster pack Background** | menus | 1 | `_Distort_Scale` 0 | `_Distort_Strength` 0 | null |
| 3 | **Rank Icon** | menus | 1 | `_Blend` 0 | （空） | null |
| 4 | **Division Image** | menus | 1 | `_Blend` 0 | （空） | null |
| 5 | **Level Up Effect** | scenes_scenes_mainmenuwarpforge | 1 | `_Reveal` **−0.5** | （空） | **非空** |
| 6 | **Level Up Effect Down** | scenes_scenes_mainmenuwarpforge | 1 | `_Reveal` **−0.5** | `_Noise_Strength_X_Speed_Y_Scale_Z` **vector4 = (0.02, −0.1, 8.17, 0)** | **非空** |

⇒ **4 个用例**：①卡框溶解（我们已做，`CardView`/`CardFeel` 里硬编码 `_DissolveAmount`）；②开包背景抖（2 个属性）；③段位图 `_Blend`（**两处**：`Rank Icon` + `Division Image`）；④升级庆祝的两张特效图（`_Reveal` + 噪声 vector —— 属账 177–185 那条升级动画）。

### 4·2 谁在动 `floatProperty.value`：5 条 legacy clip（键值现读）

| clip | 绑定的 path / 属性 | 关键帧（time, value） |
|---|---|---|
| `Card Hand To Board`（0.917 s） | `2DCard/Front/CardFrame.floatProperty1.value` | (0, 1) (0.0167, 1) (0.7, 0) |
| `Booster Window - Background Shake On Open`（0.917 s） | `floatProperty1.value`（根 = Booster pack Background） | (0, −2) (0.5833, 0.2) (0.9167, 0.4) |
| 同上 | `floatProperty2.value` | (0, 0) (0.2167, 0.35) (0.5833, 0.35) (0.9167, 0) |
| `Rank Up`（3.917 s，事件 `AnimationFinished`） | `Division Image/Rank Icon.floatProperty1.value` | 见 `_tmp_view/b8ab/animate_ui_clips.json` |
| `Division Up` | `Division Image.floatProperty1.value` | 同上 |
| `Level Up Card` | `2DCard/Level Up Effect.floatProperty1.value` · `…/Level Up Effect Down.floatProperty1.value` · `…Down.floatProperty2.vector4Value.x` | 同上 |

**组件本体语义**（判据已在账上，实现时照这个）：`OnEnable`：`initializeOnEnable ⇒ CreateInstanceMaterial()` 再 `image.material = instancedMaterial`；`Update()`：**每个槽先 `struct.Equals(default)` 判空**（空槽跳过），非空再 `useVector4 ? SetVector : SetFloat`。
⚠️ 空槽那半的语义已从 `.c` 的 `il2cpp_value_box + ValueType.Equals` 读出，**逐字节等价仍未证**（G12 已记）—— 实操上 6 个实例的空槽都是 `{materialProperty: "", value: 0, useVector4: 0, vector4: 0}` ⇒ 「名字为空就跳过」在这 6 处与之一致，但那是**我们挑的判法**，实现时请按 🔶 自己设计 打标。

### 4·3 我们这边的真实差距（现读，非账上转述）

| 项 | 现状 |
|---|---|
| `AnimateUIMaterialProperty` 等价组件 | **没有**（`grep -rn floatProperty Assets --include=*.cs` 只命中 `CardFeel.cs` / `CardView.cs` 的**注释**，没有实现） |
| 已导入并带 `floatProperty` 曲线的 clip | **2 / 5**：`WarpforgeVFX/Animations/Booster Window - Background Shake On Open.anim` · `Card Hand To Board.anim` |
| 未导入的 3 条 | `Rank Up` · `Division Up` · `Level Up Card`（工程里 `find -name "Rank Up.anim"` = 0） |
| 后果 | **`Booster Window - Background Shake On Open.anim` 的两条 `floatProperty1/2` 曲线今天在空转**（没有组件接它）。`Shell/BoosterPackOpenWindow.cs:94` 自己已经记了「背景那条只动 floatProperty1/2，没做」 |

---

## 五、转派单（别人文件里的改动；一行一条）

> 每条格式：**文件 / 方法 → 改成什么** · 判据出处 · 账行。全部**未改**（不在本代理白名单），也**未自证**。

**A. `DoSimpleShake` 的四跳接线（账行 130；引擎 3 处 + 表现 1 处 = 5 行代码）**

1. `RuleEngine/Core/EffectResolver.cs:7742` `EmitTraitRemoved(...)` → **加一个可选形参** `string effect = null` 并透传给 `ctx.Emit(..., effect: effect)`（`Emit` 那个重载本来就有 `effect`，见 `RuleCore.cs:2915` 的 `effect: "ephemeral"`）。⛔ 不要新开 `EvtKind`。· 判据 `decomp_full/CardScript__RemoveDropPod.c:3-27` · 账 130
2. `RuleEngine/Core/RuleCore.cs:7670`（`OpenDropPod` 里 `u.RemoveAll(KeywordTable.DropPod)` 之后）→ `EffectResolver.EmitTraitRemoved(ctx, u, KeywordTable.DropPod, left, "droppod-open");`
3. `RuleEngine/Core/RuleCore.cs:8369`/`:8373`（第一发打空：`if (after < 1)` 那一支）→ `…, pod, "droppod-destroyed");`
4. `RuleEngine/Core/RuleCore.cs:8426`（**易伤第二发**打空那支，`after2 < 1`）→ 同样补 `"droppod-destroyed"`（`amountBefore = after`）。⚠️ **两个破舱点都要发**，只补一处会让易伤那一档没声
5. `CardPresentation/Battle/BattleDriver.cs`（`TraitRemoved` 目前**零消费者**，新加一支）→
   `else if (e.Kind == EvtKind.TraitRemoved && e.CardId == KeywordTable.DropPod) WarpforgeVFX.WFModuleScreenShake.DoSimpleShake(e.Effect == "droppod-destroyed" ? 3f : 0.5f);`
   ⚠️ 用 **`e.CardId`** 不是 `e.Keyword`（本工程踩过的坑）· 力度 0.5 / 3.0 现读 `DAT_1834b2bb4` / `DAT_1834b2e8c`
6. **`WarpforgeVFX/**` 侧：一行都不用改**（`WFModuleScreenShake.DoSimpleShake` 已逐句对齐、`Shake Big` 在 `shake_presets.json` 里、`OnShake` 已挂在 `BattleDriver.cs:3555`）。本代理已现核 `grep -rn DoSimpleShake Assets --include=*.cs` = **0 个调用点**（只有声明/注释）

**B. `AnimateUIMaterialProperty`（账行 84/86/87/88，归 F7·Shell）**

7. 新建通用组件（`Shell/` 或 `CardPresentation/Effects/`）按 §4·2 的语义；先接 **`Booster pack Background`**（2 个 float 属性）与 **`Division Image`/`Rank Icon`**（`_Blend`）两处 —— 我们的 UI 是 `ImageQuad`（`m.uv` 是 **Vector2**、没有 uGUI `Image`），所以 **`Image.material` 那一跳要换成 `ImageQuad` 的材质口**；`CardView` 的卡框溶解那一路已硬编码，**新组件不要重复驱动它**（否则 `_DissolveAmount` 被写两遍）
8. 导入 3 条缺的 clip：`Rank Up`（`bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json`）· `Division Up`（`…6413171015603015678.json`）· `Level Up Card`（`bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`）—— 曲线键值已导出在 `_tmp_view/b8ab/animate_ui_clips.json`，**用同一套生成器**（`WarpforgeVFX/Animations/` 里那 47 条的风格：`m_Legacy: 1` / `m_WrapMode: 1` / `m_SampleRate: 60`）

**C. 升级庆祝动画整条（账行 177/178/179/180/181/182/184/185，归 Shell）**

9. 组件 = `CardDisplayLevelUpAnimationController`（**全 build 只有 1 个实例**：`Upgrade Panel`，`scenes_scenes_mainmenuwarpforge`）＋ 通知者 `CardDisplayLevelUpAnimationNotifier`（**也只有 1 个**：`CardUI`，`levelUpController` 指向 `Upgrade Panel`）。⚠️ 账上写的「原版 8 处 / 3 处实例」**与本轮计数不符**（见 §七）
10. 它的序列化字段（真值，供照做）：`myAnimation` · `impulseSource` · `levelUpEffect1`=Level Up Effect · `levelUpEffect2`=Level Up Effect Down · `upgradePanel` · `fadeBackground` · `sound`(AudioCue) · `cameraShakePreset` = `{presetSO, delay 0, overwriteAmplitude 1 → amplitude **20**, overwriteFrequency 1 → frequency **0.15**, overwriteDecayTime 1 → decayTime **1**}`
11. 缺的口（逐方法）：`DoAnim`（开物体 + 播 Animation + `impulseSource.GenerateImpulse()` + 背景淡到 0.7/0.3 s + 播 2D cue）· `ChangeFrame`（抖屏 + 换框 + 背景 DOFade(0, 0.7)）· `FadeOutImages`（**三张**图：两张特效图 + 背景）· `AnimationFinished`（= 重新开输入）

**D. 战斗侧取景/朝向/后期/环境（归 Battle，账行 150/154/155/175/340/341/362/367）**

12. `CameraVerticalFramer`：`playerCardAreaSizeHelper.sizeDelta.y` 那一跳 + 把 `originalSensorSize/originalLensShift` 写回相机 + `CalculateFraming` 收尾那份 sensorSize（`CombatAutoZoom.cs:438-`；`BattleCameraSreenSize.cs:167/184/208`）。现核：13/13 场都有该组件（本代理计数 = **13**）
13. `CardAnimController.RotateAnim`：补 `pointTowardsOption = boardCenter` 那一支（用两翼最左/最右位算 up 与 −90°；`BattleDriver.cs:18443` 今天只有「指向目标」）· ⚠️ `CardAnimController` **0 序列化实例**是正常的 —— 原版是运行时 `AddComponent`（`decomp_full/BattleManager__CreateAnim.c` 里 `UnityEngine_GameObject__AddComponent<object>(go, DAT_1842c3348)` 之后立刻 `CardAnimController__Initialize`）
14. `PostFXController.ResetBloom/ResetLUT`：`instant = false` 那条 **1.0 s 过渡**（`TIME_TO_RESET`）+ 先 Kill 旧 tween（`BattlePostFx.cs:356-369`）
15. `ScenarioMaterialFader`：⑤ `doFade = !doFade` 那一句（`ScenarioMaterialFader.cs:54-101` / `:103-113`）—— ⚠️ 盲抄会把该淡的那次取消，**先加计数探针测「同一件环境连续两次 Blend」**再决定
16. `SwarmVFXController.Initialize`（账行 403）：`BattleDriver.PlaySwarmMergeFeel`（`:23950`）补 `CardUI.ToggleBody3D(false)` + `CardMaterialHelper.ConfigureMaterial` 那一跳。⚠️ VFX 侧的「接收口 + 5 秒尾巴」本代理已做完（§三）

**E. 引擎（归 RuleEngine，账行 187/188/189/190）**

17. `CardEffect.IsEnchantment`：`buffType ∈ {12, 15, 16}`（我们现在只按 `Type == "enchantment"` 一个维度）· `IsSameEffect`：**按字段内容**判重（buffType / enchantingCard / heroCheck / enchantmentCardId / extrinsic / 四时长开关 / 四数值），我们按 `RecordId` ⇒ 同一 payload 连挂两次会叠两层 · `SameIsPlayerEffect` + `SameTimingAsEffect` 同批补

**F. 段位/扫光/开包等 UI 特效（归 Shell，账行 425/430/432/433/438 · 462–465 · 455 · 208）**

18. `UIBorderGlow`（5 行）：shader `Everguild/UI/UI Border Highlight Appear` **不能独立完成扫光**（PS 直接读 `uv0.z/uv0.w`）⇒ 必须配 `ModifyMesh` 写 `uv0.zw = Camera.main.WorldToViewportPoint(transform.position).xy`。**我们的 `ImageQuad` 只写 Vector2 uv**（`ImageQuad.cs:493-496`）⇒ 要先给它一个 `SetUVs(0, Vector4)` 的等价口 + 布局/父级变动重刷。现核 `UIBorderGlow` **89 个实例**
19. `UIEffectTogglerImage`（4 行）：组件本体已在 `CardPresentation/Effects/UIEffectTogglerImage.cs` 且逐句对齐 ⇒ **只差接线**（**2 个实例**：`Done Highlight`、`Prebuilt Pack UI`，都在 `menus`）
20. `UIEffectTogglerCoffeeEffects`（1 行）：要导入第三方 `Coffee.UIEffect`（组件 dll 有方法体 + shader 只有 1 只 `Hidden/UI/Default (UIEffect)`）；**7 个实例**（`Featured Icon` / `Icon`×4 / `Army Icon` / `Done`，全在 `menus`）—— 主成本是「Shell 那批用 `ImageQuad` 得另写一版」
21. `EnviromentalEffectCardsSO.GetEnvCardListDraft`（轮抽线）：按胜场解锁环境卡（防御恒定给第 1 张、>5 第二、>8 第三；进攻 >2/>5/>8 三档；胜场 <3 且要进攻 ⇒ 空表）

**G. `UI Click 1`（账外缺口，`工具/` + `Shell/`）**

22. 现核四项：cue `ButtonClick 1` **不在** `CardPresentation/Resources/animfx_sounds.json` 的 473 条 cues 里；`Resources/Art/audio/sfx/` 下 6 个「UI Click*」文件里**没有 `UI Click 1.wav`**（有 `UI Click 2_no delay` / `Card 2` / `Deck 1`）；`WFSoundBank.TryGetCue` 在；步骤照 `判据/B8_三条缺口_判据与资源.md` §2·4（① `工具/import_original_sfx.py:104` 的 `EXTRA_CUES` 加 `"ButtonClick 1"` ② `--check` ③ 真跑 ④ **Unity 批处理（只有调度台能跑）** ⑤ `Shell/ItemDrawer.cs:1971` 换 3 行播放）

---

## 六、没查清的部分（如实记）

1. **我第一版扫描给过假 0**：直接拿 `globalgamemanagers.assets` 里的 PathID(4383) 去比 bundle 里的 `m_Script` ⇒ 全 0（连已知 434 实例的 `AnimFXModuleScreenShake` 也是 0）。**根因**：bundle 里的 `m_Script` 指向 **bundle 内置的 MonoScript 副本**（大 PathID），不是主文件那份。改用「先解 bundle 内置 722 份 MonoScript 的名字表」之后才对。⇒ 以后任何人做同类统计，**必须先用一个已知有实例的类做正控**。
2. **「9 个 prefab」的原始出处没找到**（只在两份报告里被转述），已按 §2·3 推翻；若有人还留着原始脚本，请回填。
3. **`AnimateUIMaterialProperty` 曲线 → 实例的绑定**：靠 clip 的 `path` + 属性名与上表逐一吻合推定（6/6 名对得上），但曲线里的 `script` PPtr 跨文件（`m_FileID != 0`）**没有解析到具体实例** ⇒ 「哪条 clip 被哪个 prefab 的 `Animation` 播」还差最后一跳（需要 externals 解引用）。
4. **Unity 对 legacy clip 的 `length` 到底取 `m_StopTime` 还是「最晚的键」** —— 本代理没跑 Unity，**未证**。但两点已确认：(a) 我们 47 条里 34 条的 `m_StopTime: 1` 与曲线末端不符 —— **不是缺陷**：原版同一条 clip 序列化的 `m_MuscleClip.m_StopTime` 也是 **1.0**（`Swarm Triat Merge` / `Booster Window - Background Shake On Open` 逐个现读；AssetRipper 导出的原版同样写 1）⇒ 我们照抄是对的；(b) ⚠️ `资料/普查产出_第六会话/W_VAT驱动.md:249` 写的「`Vanguard Frame Animation` 的 `m_StopTime ≈ 1.7916666`」**与现读不符**（原版 JSON = **1.0**，max key = 1.7916666）⇒ 建议由该文件归属方订正（本代理不改别人的文档）。
5. 本代理**没有可观察量**（不跑 Unity）⇒ 账行 130 / 404 保持 C / B，**没有硬改 A**。§三 的观测量留给真 Play。

---

## 七、顺手发现（现核，全部有出处）

**7·1 本域 13 个类的「序列化实例数」（`UnityPy` 全 84 bundle 现数）** —— 用来校正账里「N 处实例」的说法：

| 类 | 实例数 | 分布 |
|---|---|---|
| `ObjAnimEffects` | **0** | —— |
| `SegmentationController` | **0** | 0 实例 ⇒ 账行 381–385 的「建议 D」多了 1 条硬证据 |
| `CameraShake` | **0** | 与账 104–109 判 D 一致 |
| `CardAnimController` | **0** | ⚠️ **不是缺陷**：运行时 `AddComponent`（见 §五·13） |
| `CardDisplayLevelUpAnimationController` | **1** | `Upgrade Panel`@mainmenuwarpforge（账上写「8 处」） |
| `CardDisplayLevelUpAnimationNotifier` | **1** | `CardUI`@mainmenuwarpforge（账上写「3 处」） |
| `SwarmVFXController` | **1** | battleprefabs |
| `CameraVerticalFramer` | **13** | 13 个战场各 1 |
| `PostFXController` | **13** | 13 个战场各 1 |
| `CameraShakerManager` | **14** | 13 战场 + 主菜单 |
| `AnimateUIMaterialProperty` | **6** | 见 §4·1 |
| `ScenarioMaterialFader` | **26** | battleprefabs / battleshared / 两个战场… |
| `UIBorderGlow` | **89** | generalgamewindows / menus / mainmenu… |
| `UIEffectTogglerImage` | **2** | menus（`Done Highlight` / `Prebuilt Pack UI`） |
| `UIEffectTogglerCoffeeEffects` | **7** | menus |

**7·2 开包背景那条 clip 今天在空转**（见 §4·3）：`Booster Window - Background Shake On Open.anim` 已在我们工程里、曲线绑的是 `floatProperty1/2.value`，但接收这些属性的组件**不存在** ⇒ 播了等于没播。`Shell/BoosterPackOpenWindow.cs:94` 已自记「没做」。

**7·3 方法学（给下一批）**：「bundle 里的 `m_Script` 要按 **bundle 内置**的 MonoScript 副本解析」这条值得写进 `CLAUDE.md`（§六·1 的假 0）。同一坑的判据：**任何实例普查都必须带一个已知有实例的正控类**。

---

## 附：本代理动过的文件清单

| 文件 | 改动 |
|---|---|
| `MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs` | 类尾 +30 行（`SwarmAnimationEnded` + `SwarmAnimationEndTail`）+ 2 处原地订正（`:639` / `:642`）+ `:662-664` 改调用；**净 +30 行，账落点 0 漂移** |
| `资料/完全复刻/账/_parts/B8_AB.tsv` | 8 行（6 条 C→D + 账 130 备注刷新 + 账 404 落点/差异刷新）· LF · 每行 11 字段 |
| `资料/完全复刻/报告/B8_AB_特效动画C档.md` | 本文件 |
| `_tmp_view/b8ab/*.py` | 只读扫描/导出脚本（临时目录，方法已在 §2·4 写全） |

**未动**：`账/*.tsv` 主表 · 任何 `CardPresentation/**` / `RuleEngine/**` / `Editor/**` / `工具/**` / `资料/` 正本。
