# B9_E2UDW3 · 特效派活包（J-01 · J-02 · J-03 · J-11 · J-07 · J-08）

> 2026-10-27 · 执行代理 `E2UDW` · 判据 = `判据/B9_特效C_派活包.md` + `判据/B9_特效C_施工单.tsv`
> ⛔ 全程未跑 Unity、未动 git、未改正本与账主表。
> ⚠️ **本文件 2026-10-27 被重建过一次**：一次 `io.open(p,'wb').write(打开p读…)` 把主文件先截断再求值（CLAUDE.md 里那条坑），
> 旧内容（第一批~第五批）按会话内原文重建，**结论与行号引用未变**。⛔ 以后追加一律「先 Write 临时文件、再用二进制拼接」。

## 总览（各批做了什么）

| 批 | 内容 | 落盘 |
|---|---|---|
| 一 | J-01/J-02/J-03 判据核对；**生成 3 条缺的 legacy clip** + 修掉 `Booster Window…anim` 的零 GUID 死绑定 | `工具/gen_anim_clips.py` + `WarpforgeVFX/Animations/*.anim`（4 条）|
| 二 | **J-03 语义修正落地**（α 绝对值覆盖）；J-04 因 `ImageQuad` 高险停手；J-11 因缺数据停手 | `CardPresentation/Effects/UIEffectTogglerImage.cs` |
| 三 | **解出 `defensiveCards`**（13 阵营 × 3 槽 = 39 条）+ 生成器 + 并进 flat JSON | `工具/gen_defensive_cards.py` · `数据/游戏数据/defensive_cards.json` · `工具/gen_offensive_cards_flat.py` · `Resources/OffensiveCards.json` |
| 四 | **J-11 C# 半**：DTO + `ChoicesForDraft` + 自检口 + 可跑自检 | `CardPresentation/Core/OffensiveCards.cs` · `工具/check_draft_choices.py` |
| 五 | J-05 停手（BattleDriver）· **J-07 注释订正** · **J-08 `doFade` 取反开关抄上** | `Battle/CombatAutoZoom.cs` · `Battle/ScenarioMaterialFader.cs` |
| 六 | **`doFade` 风险静态定死** + 4 条「悬空落点」就地订正 | 无（纯读） |

---

# 第一批 · J-01 / J-02 / J-03

## ① 结论

| 包 | 结果 |
|---|---|
| **J-01（84/86/87/88）** | 前置 **J-06 的三条 clip 已生成**；接线（`Shell/`）未做；🔴 **可见面被 shader 挡死**（见 ②-D）|
| **J-02（177–182/184/185）** | 前置 `Level Up Card` clip 已生成（含 `ChangeFrame @1.45 s` / `AnimationFinished @3.4833 s`）；代码全在 `Shell/` ⇒ 未动，逐条对照见下 |
| **J-03（462–465）** | 463 的语义修正**已落地**（第二批）；462/464/465 的两处接线在 `Deck/`·`Shell/` ⇒ 未动 |

## ② 证据

### A. 三 条 clip 的源 / 格式 / 产物
- 源（`assets_full`，含完整 `m_FloatCurves` + `m_Events` + `m_Legacy: True` + `m_SampleRate: 60`）：
  `bundle_menus_assets_all/AnimationClip/…4418588262184220504.json`（**Rank Up**，10 曲线，`AnimationFinished @3.9167`）·
  `…6413171015603015678.json`（**Division Up**，11 曲线，`AnimationFinished @3.8333`）·
  `bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`（**Level Up Card**，10 曲线，**`ChangeFrame @1.45`** + `AnimationFinished @3.4833`）。
- 格式模板 = 工程里已有的 `WarpforgeVFX/Animations/Booster Window - Background Shake On Open.anim`
  （`m_Legacy: 1` · `m_Compressed: 0` · `m_UseHighQualityCurve: 1` · `m_SampleRate: 60` · `m_WrapMode: 0` · **`m_StopTime: 1`** · 权重 1/3 · 前/后无穷 2 · `m_RotationOrder: 4`）。
- 生成器 **`工具/gen_anim_clips.py`**（默认 dry-run；`--write` 写；`--check` 重读逐字节复验，本批 4/4 ✅）。

### B. 🔴 `script` 引用（绑空气的那一格）
- 原版每条 `floatProperty*` 曲线的 `script` = `{m_FileID: 1, m_PathID: <原版 MonoScript>}`。
- 工程里原有的 `Booster Window - …anim` 写的是 `{fileID: -1887485317901086766, guid: 00000000000000000000000000000000, type: 0}` ⇒ **死绑定**
  （今天无害：`Shell/BoosterPackAnim.cs:13-14` 与 `Shell/BoosterPackOpenWindow.cs:94-95` 都写着「没做」、没人播它；**接线那天就是静默失败**）。
- 生成器统一写 `{fileID: 11500000, guid: <AnimateUIMaterialProperty.cs.meta 的 guid>, type: 3}`（guid 运行时从 `.meta` 读，⛔ 不写死）。
  并顺手用同一份原版数据重写了那条 `Booster Window - …`（只动两个 `script:` 行，`.meta` guid 不变）。

### C. 逐条「能不能绑上」（生成器按 classID 判定）
- 能：`floatProperty1.value` / `floatProperty2.vector4Value.*` + 原生类曲线 `m_IsActive`(1) · `m_Alpha`(225) · `ShapeModule.m_Scale.*`(198)。
- 不能：走 uGUI 的 `classID 114` 曲线（`m_Color.a` / `m_fontColor.a` / `inherit*`）—— 我们节点是 `ImageQuad`/`Label`。
- ⚠️ 三条 clip 的 `path` 是**原版节点路径**，我们 Shell 树的节点名未核 ⇒ 接线时要逐节点对。

### D. 🔴 J-01 的可见面被 shader 挡死
- `Booster pack Background` 的原材质 = **`Booster Pack Open Menu Background`**（`bundle_menus_assets_all/Material/Material_-1341631206307795618.json`），
  shader = **`Shader Graphs/Nebula`**（`…/Shader/Shader_-5564698372089388815.json` 的 `m_ParsedForm.m_Name`），
  默认值：`_Distort_Scale 0` · `_Distort_Strength 0` · `_Blend 0` · `_SrcBlend 5` · `_DstBlend 10` · `_StarsIntensity 2616` · `_StarScale 2085`。
- 组件实例真值（`…/MonoBehaviour/MonoBehaviour_-2907234576846799837.json`）：`initializeOnEnable = 1`，
  `floatProperty1 = (_Distort_Scale, 0)` · `floatProperty2 = (_Distort_Strength, 0)` · `changeMaterialOnAnimation = null`
  —— 与 `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` 文件头那张「6 个实例」表**逐一吻合**。
- 我们工程 `ShaderMap` **没有** `Shader Graphs/Nebula`：`WarpforgeArena1/Editor/BoosterPackExporter.cs:574` 自述会落到 `URP/Unlit*` 近似；
  而 `URP/Unlit*` 没有这三个属性 ⇒ `SetFloat` 静默 no-op（`AnimateUIMaterialProperty.cs:85-89` 也记着这条已知）。
⇒ **即便 84 的接线全做完，J-01 今天也没有可见效果**；要真生效 = 让那个 shader 进工程（bundle shader 落不了工程资产 ⇒ 只能运行时加载）。

### E. J-02 的 8 个方法 → 我们的落点（接线方照做）

| 账行 | 原版方法 | 我们的落点 |
|---|---|---|
| 182 | `OnEnable`（三图 α 归零）| 新建 `Shell/CardLevelUpAnim.OnEnable` |
| 179 | `DoAnim`（开物体/播 clip/抖/背景 α→0.7/2D cue）| 同上；抖 = **窗容器抖**（先例 `Shell/BoosterPackAnim.cs:309-332`，🔶 必须标）|
| 181/180 | `FadeOutImages` / `FadeOutImage`（三件、无补间）| 同上 |
| 178 | `ChangeFrame`（抖/换框/背景 `DOFade(0,0.7)`→关物体）| 由 clip 的 `ChangeFrame @1.45` 事件驱动 |
| 177/184 | `AnimationFinished`（解输入锁）| 由 clip 的 `AnimationFinished @3.4833` 驱动 |
| 185 | 转发入口 | — |

原版真值：`DOFade(0.7, **0.3**)` / `DOFade(**0**, **0.7**)`；字面量 `0.7 = DAT_1834b3188` · `0.3 = DAT_1834b2dc8`（施工单给的，**本批没复核**）。

## ③ 没查清
1. `Division Image` / `Rank Icon` 的原材质与 shader 名未核（只核了 Booster 背景）。
2. J-02 的 `Level Up Effect` / `…Down` 的 shader 名仍未解（施工单 §8-2）⇒ 只能验「浮点写对」。
3. 施工单给的两个字面量 `0.7/0.3` 未复核。
4. J-03 两处 `Toggle(option, duration)` 的实参与触发时机未读方法体（施工单 §8-3）。
5. 三条 clip 的 `path` 对应我们 Shell 树的哪些节点 —— 未核。

## ④ 需要的 Unity 命令
1. **导入 4 条 `.anim`**（写盘后 Unity 才生成 `.meta`）：跑任意一次批处理即自动 import，最省的是
   `unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod ShellScene.Run -logFile -`
2. **J-01 接线（`Shell/`）**：
   `BoosterPackOpenWindow.cs` 的背景 `ImageQuad` ⇒ `AnimateUIMaterialProperty.Attach(quad, Slot("_Distort_Scale",0f), Slot("_Distort_Strength",0f))`；
   `RankedDivisionChangeWindow.cs` 的 `Division Image` 与 `Division Image/Rank Icon` ⇒ `Attach(quad, Slot("_Blend",0f))`；
   同一 GameObject 上还要 `Animation` 组件 + clip（原版 `playAutomatically = 0`，按事件播）。
   读数口：`comp.InstancedMaterial.name` 以 `(Instanced)` 结尾；`GetFloat("_Distort_Scale")` 逐关键帧 == `_tmp_view/b8ab/animate_ui_clips.json` 的 `menus/booster`。
3. **J-01 的 shader 件**（新账，大件）：`Shader Graphs/Nebula` 进工程。
4. **J-03 的接线**：`DeckRuntime.SetOn` 加组件 + `DraftModeWindow.BuildPackCard` 的 `fadeTargets.Add(highlightNode)`。

## 顺手发现（第一批）
1. 🔴 `.gitignore:26` = `/Unity/MyGame/Assets/WarpforgeVFX/*` ⇒ **整个目录不入库**；4 条 clip 只在盘上 ⇒ 靠生成器复现（这也是把生成器放 `工具/` 的原因）。
2. 手写 `.anim` 绑 MonoBehaviour 浮点必须走生成器（⛔ 别抄 `guid: 0000…` 那行）。
3. J-03 派活包的 目标文件**漏了** `CardPresentation/Effects/UIEffectTogglerImage.cs`（463 的修正真正落在那里）。
4. `Booster pack Background` 的组件序列化字段与 `AnimateUIMaterialProperty.cs` 文件头的「6 个实例」表**逐一吻合**。

---

# 第二批 · J-03 真修正已落 · J-04 / J-11 受白名单与数据所限

## ① 结论
| 包 | 结果 |
|---|---|
| **J-03（462–465）** | ✅ **463 的语义修正已落地**（`CardPresentation/Effects/UIEffectTogglerImage.cs`）；两处接线仍在 `Deck/`·`Shell/`（未动）|
| **J-04（425/430/432/433/438）** | ⛔ 做不了：缺口在 `Battle/ImageQuad.cs`（`m.uv` 2 分量 → `SetUVs(0, Vector4[])`）与 `Shell/ForgeTab.cs`；`WarpforgeVFX/Runtime/UIViewportUvWriter.cs` 侧**本来逐句等价**（0 行可改）|
| **J-11（208）** | ⛔ 当时只有进攻半边有数据（防御数据缺）⇒ 未写（第三批已解出来）|

## ② 证据
### A. 463 的判据（🥇 `.c` 逐句，`decomp_full/UIEffectTogglerImage__DoUpdate.c:17-45`）
```
fVar6 = targetStateEnabled(+0x21) ? 1 : 0        // to
fVar5 = 1.0f(DAT_1834b2bb8) - fVar6              // from = 1 − to（与「当前 α」无关）
n     = clamp(param_3, 0, 1)
逐图：color = image.color (getter) → color.a = (to − from) × n + from → image.color = color
```
⇒ **α 绝对值覆盖、RGB 取当前值**，**不是** `基线α × fade`。

### B. 🔴 同族 `UIEffectCanvasGroupToggler.cs:107-125` **必须保持乘性**（⛔ 别一起改）
那个类映射的是 `CanvasGroup.alpha`（**本身是乘子**）⇒ 我们落到逐层 α 时乘基线才对。两个类长得一样、语义相反。

### C. 本次改动今天**行为中性**：`AddComponent<UIEffectTogglerImage>` = 0、`*.prefab`/`*.unity` 引用 = 0。

## ③ 没查清
J-03 两处 `Toggle` 的实参/时机未读（施工单 §8-3）；防御卡数据当时缺（第三批已补）。

## ④ 改动清单：`CardPresentation/Effects/UIEffectTogglerImage.cs`（**+17 / −5**，纯 LF）
1. `DoUpdate` 三支从 `base × CurrentFade` 改成 `c.a = CurrentFade`，RGB 改成每帧取当前值（`q.Tint`/`l.color`/`g.color`）。
2. `CurrentFade` doc：「0..1 乘子」→「直接写进那张图的 α（绝对值）」。
3. 类头 `DoUpdate` 说明改成原版算式 + 「⛔ 不是乘性」。
4. 三份「基线」列表保留但注明**不再参与 α 写入**（只作诊断）。

🔴 **账行号漂移（8 条）**：`8→8`（未动）· `21→23` · `45→49` · `53→57` · `73→77` · `81→85` · `109→121` · `115→127`。

## 该跑：今天可不跑（0 挂载点）；接线落地后 `DeckScene.Run` + `ShellScene.Run`。

---

# 第三批 · `defensiveCards` 数据（J-11 的前置）

## ① 结论
| 项 | 结果 |
|---|---|
| 原版数据 | ✅ **存在且完整**：`EnviromentalEffectCardsSO.enviromentalEffects[].defensiveCards`，**13 阵营 × 3 槽 = 39 条**，`forgeLevel` 全 `[1, 20, 40]` |
| 提取脚本 | ✅ `工具/gen_defensive_cards.py`（默认 dry-run；`--apply` 写；`--check` 复验；内置 208 验收口复算）|
| 数据落盘 | ✅ `数据/游戏数据/defensive_cards.json`（9,450 B / LF）+ 并进 `Resources/OffensiveCards.json` 的每阵营 `defensiveCards`（**+260 / −0**）|
| 合并入口 | ✅ 扩了 `工具/gen_offensive_cards_flat.py`：读到防御表就并（没有则老行为不变）|
| 验收口自核 | ✅ `victories = 0/2/3/6/9` ⇒ 防 **1/1/1/2/3**（13 阵营取值集合全为单值）|
| C# | 本批未动（第四批补）|

## ② 证据
- 源 = `素材/Warpforge原版/游戏数据/去重定义/MonoBehaviour/EnvironmentalEffectCardsSO.json`（= `assets_full/bundle_duplicateassetisolationso_assets_all/…`；⚠️ 资产名 `Environmental…` 比类名多一个 `n`）。
- 每条 = `{ "defensiveCard": {"m_FileID": 4, "m_PathID": …}, "forgeLevel": … }`（**没有** envSO/prefab/卡面）。
- 39 条 `PathID` 两两互异；与进攻侧 52 条（39 卡 + 13 空卡）**交集 0** ⇒ 两套不同的卡。
- 阵营号 → 阵营名**只从** `offensive_cards.json` 的 `armyId` 取（脚本断言两表阵营集合相同）。

## ③ 就地订正（铁律 5）
草稿先写「52 张 = 13×(3 进攻 + 1 默认) ⇒ 防御卡没有配套卡面贴图」——**证据不足，已改口**：
每个 `bundle_*cardassets*_assets_all/Texture2D/` 有 **91–112 张**（整阵营卡图），52 只是**进攻表里抽到的** ⇒ 只能说「进攻表没抽到」。
真相：防御卡是**普通卡**，卡面混在同阵营卡图里、**按 PathID 定位不到** ⇒ 无候选卡名。
（`cardName` 解不出的根因：`m_FileID: 4` = 外部文件、卡数据在远端 CCD。）

## ④ 改动清单
`工具/gen_defensive_cards.py`（新）· `数据/游戏数据/defensive_cards.json`（新）· `工具/gen_offensive_cards_flat.py`（+合并段）· `Resources/OffensiveCards.json`（重生成 +260/−0）。

🔴 给下一个写手：`cardRefPathID` 是 **64 位**（2.7e17 ~ 8.9e18，含负数）⇒ DTO 必须 `long`；`name` 恒空串 ⇒ 判空用 `string.IsNullOrEmpty`。

---

# 第四批 · J-11 的 C# 半（账 208）

## ① 结论
| 项 | 结果 |
|---|---|
| DTO | ✅ `Card` 加 `long cardRefPathID`；`Army` 加 `Card[] defensiveCards` |
| 逻辑 | ✅ `ChoicesForDraft(string faction, bool offensive, int victories)`（六步照 `.c`，阈值**严格大于**，不复用 `ChoicesOwnedAt`）|
| 自检口 | ✅ `DumpDraftAvailabilityForTest()`（C#，返回整表 + 不符格数）|
| 可跑自检 | ✅ `工具/check_draft_choices.py`：13 阵营 × v0/2/3/6/9 = 防 1/1/1/2/3 · 攻 0/0/1/2/3；另 **victories 0..9 全扫 260 格 ✅ 全对**，退出码 0 |
| 消费点 | ⛔ `Shell/DraftModeWindow.cs` 不在白名单 ⇒ 未接线 |

## ② 判据（`decomp_full/EnviromentalEffectCardsSO__GetEnvCardListDraft.c:1-120`）
① 空表；② `victories < 3 && offensive ⇒ 空`；③ 找不到阵营 ⇒ 空；
④ 防御：恒 `[0]` + `>5` 加 `[1]` + `>8` 加 `[2]`；⑤ 进攻：`>2` 加 `[0]`、`>5` 加 `[1]`、`>8` 加 `[2]`；⑥ 返回。
⇒ 三条闸都是**严格大于**；返回表**不含**「不使用进攻卡」那张。

## ③ 没查清 / 边界
阵营匹配走既有 `For(faction)`（阵营名、大小写不敏感），原版比的是 `CardArmy` 枚举 —— 不是枚举直比（如实记）；防御卡名/卡图仍解不出。

## ④ 改动清单：`CardPresentation/Core/OffensiveCards.cs`（**+107 / −0**，纯 LF）
类尾新增 `ChoicesForDraft` · `AddSlot` · `_warnedNoDefensive` · `DumpDraftAvailabilityForTest` · `DefensiveSlotsShort`；防御数组缺失（旧 JSON）⇒ **出声一次**并写清重跑哪两个生成器。

🔴 **账行号漂移（3 条，各 +2）**：`55→57`（`For`）· `72→74`（`Choices`）· `98→100`（`ChoicesOwnedAt`）。

## 待接线（`Shell/DraftModeWindow.cs`）
`var envCards = OffensiveCards.ChoicesForDraft(faction, /*offensive:*/false, victories);`（进攻侧传 `true`）。
⚠️ 该层今天不存在；**用户已把进攻/防御面板关掉** ⇒ 接出来也要**默认关** + 注释写明。

## 该跑：今天不需要；接线后 `ShellScene.Run` 断言 `DumpDraftAvailabilityForTest()` 的「不符格数 = 0」。

---

# 第五批 · J-05 停手 / J-07 注释订正 / J-08 `doFade` 取反开关已抄

## ① 逐条
| 包 | 结果 |
|---|---|
| **J-05（175/403/505/507/508/498/510）** | ⛔ 没做：7 条全在 `BattleDriver.cs`（`E4BDR` 独占）|
| **J-07（150/154/155）** | ✅ 注释级订正（`CombatAutoZoom.cs`，**5 行 → 5 行、零漂移**）；`BattleCameraSreenSize.cs` 侧本来就已对齐 |
| **J-08（362/367）** | ✅ `doFade` 取反开关已抄（`ScenarioMaterialFader.cs`）；风险见第六批（已定死为「保留」）|

## ② J-07 证据
原文写「我们那份（`virtualCameraLensShift`）**从不读**相机的 `lensShift`」——**与现行代码不符**：
`CalculateFraming` 现在**就**读 `boardCamera.lensShift` / `sensorSize`（`:456-460`，只为保住 `.x`/`.y` 不动，正是原版 `:204-206`/`:216-218` 的形状）。
订正后：与「把**缓存值**写进相机」真正差的只是「取当前值 vs 取缓存值」，缓存即抓取那一刻的相机值 ⇒ 现读相等、**无可观测差**。

## ③ J-08 判据与改动
- `DoScenarioBlend.c:78` 与 `:110` 两处同一句 `doFade = !doFade`；`Update.c:15` 首句 `if(!doFade) return;`；ctor 不写 `0x40` ⇒ 出厂 `false`。
- 改动 3 处：新字段 `bool _doFade` · `DoScenarioBlend` 在 `_opts = o;` 之前 `_doFade = !_doFade;` · `Advance` 首句 `if (!_doFade) return;`。
- 同时把类头「⚠️ 仍未抄的一条」与旧注释「⛔ 我们没有抄它」**就地改掉**（铁律 6）。

## ④ 账行号漂移（`ScenarioMaterialFader.cs`）
`32→34` · `41→43` · `103→118` · `156→174` · `168→186` · `175→193`（`CombatAutoZoom.cs` 0 漂移）。

---

# 第六批 · `doFade` 风险定死（静态）+ 4 条「悬空落点」就地订正

> 本批**未改任何文件**（纯读）；⛔ 未跑 Unity、未动 git、未改正本与账主表。

## ① 结论：同一次环境变更里，同一实例**只被调 1 次** ⇒ **保留 `_doFade`**（不改调用方次数）

| 调用点（`EnvironmentApplier.cs`）| 什么时候 | 对谁 |
|---|---|---|
| `AttachBlendables` → `DoScenarioBlend`（`:288`，由 `:173` 调）| **只在 `Instantiate` 了新 prefab 之后**（`:170-173`，仅「prefab 变了」那条支）| 新实例自己的 blendables |
| `for (…_sceneBlendables…) DoScenarioBlend(opts)`（`:216`）| **每次 `Apply` 一遍** | 战场自带那批（`EnsureSceneBlendables` 缓存，`:440/:476`）|
| `BeginFadeOut` → `DoScenarioBlend(direction:false)`（`:884-893`）| **撤旧实例那一程** | **旧**实例的 blendables |

⇒ 一次变更里：场景侧 1 次；实例侧要么「新建（1 次）」要么「撤旧（1 次）」——**不会落在同一实例的同一次变更上**
（`Instantiate` 只在 prefab 变化时发生，`BeginFadeOut` 是对旧实例的另一次变更）。
⇒ **不需要改 `EnvironmentApplier` 的调用次数**，`_doFade` 按原版保留。

### 跨变更会翻转（原版语义，不是我们引入的）
同一实例**第 2 次**被调时 `_doFade` 变回 `false` ⇒ 那次 `Advance` 直接 return ⇒ **那次不淡**。
原版逐字如此：两支各 flip 一次；`SetTargetFade` 只写 `0x48/0x4c/0x44`、**不碰** `doFade`
（grep 全部 `ScenarioMaterialFader__*.c`，写 `+0x40` 的**只有** `DoScenarioBlend`）。
最典型的「第 2 调」= **撤环境那一程的淡出**。🔥 与已记过的原版怪癖连起来：原版 `BeginFadeOut` 用 `onComplete` 计数当销毁闸，
冻住 ⇒ **不回调 ⇒ 旧实例永不销毁**（`:884-886` 注释写着原版那套计数本来就归不了零）。

## ② 缺的唯一读数（同步点，`BattleScene.Run` 可断言）
静态结论是「1 次/实例/变更」，但运行时可能出现「同一 prefab 再应用一次」这类路径。断言配方：
```csharp
// 变更前：场景里所有 ScenarioMaterialFader 的「每实例计数」快照
// 换一次环境卡 → Advance 到停 → 逐实例比
Check(每实例 delta == 1, "一次环境变更对同一 ScenarioMaterialFader 只应调 1 次 DoScenarioBlend");
```
⚠️ 现在只有**静态**累加计数 `ScenarioMaterialFader.BlendCalls`（够判「总数 = 实例数」，**不够判**「每实例 1 次」）。
要更硬得加 `public int InstanceBlendCalls { get; private set; }`（1 行 + 1 行自增），但那在 `Battle/`（本波 ⛔）⇒ **只给配方**。
另：撤环境那条顺带读一次「旧实例是怎么没的」—— 正常回调销毁 vs `超时兜底销毁`（后者会打 `[Env] … 没全部回调 …` 警告）。

## ③ 🔴 就地订正（铁律 5）：那 4 条**不是**悬空落点，是我上一批判错了
账 `特效动画.tsv` 行 361/362/363/364 的「**判据出处**」列写的是 `ScenarioMaterialFader.cs:410 / :403 / :508 / :374` ——
指的是 **ILSpy 反编译文件** `cpp2il/out_cpp2il_dev_ilspy/ScenarioMaterialFader.cs`（**561 行**），那一列只是**省略了目录前缀**
（同行的「我们的落点」列写的是带前缀的 `CardPresentation/Battle/ScenarioMaterialFader.cs:156`）。逐条对得上：

| 账行 | 引用 | ILSpy 实际内容 | 我们的同名方法 |
|---|---|---|---|
| 361 `CacheOriginalMaterials()` | `:410` | `private void CacheOriginalMaterials()` | `…:156` ✓ |
| 362 `DoScenarioBlend(...)` | `:374` | `bool flag15 = !doFade;`（两支收尾的 flip 之一）| `…:54` 一带 ✓ |
| 363 `RestoreOriginalMaterials()` | `:508` | `private void RestoreOriginalMaterials()` | `…:175` ✓ |
| 364 `SetTargetFade(...)` | `:403` | `private void SetTargetFade(float targetValue, float blendTime)` | **D**（未实现，原版 private 且 0 调用）✓ |

⇒ **主表不用改**；建议调度台在那 4 条的「判据出处」前**补 `cpp2il/out_cpp2il_dev_ilspy/` 前缀**，免得下一个人再误判成悬空。

## ④ 另一处就地订正
上一批写的「冻结后再来时原版接着上次 elapsed、我们 `_left = o.duration` 重头算」——**不成立**：
ILSpy `:377-378` / `:393-394` 每次 `DoScenarioBlend` 都 `blendTime = currentBlendTime = o.BlendTime`（计时器**重头**）
⇒ 我们的 `_left = o.duration` 与原版一致，**那条已知差异作废**。

## ⑤ 该跑
`BattleScene.Run`（`CombatAutoZoom` / `ScenarioMaterialFader` 都在战斗链上）+ 上面那条「每实例 1 次」读数。

---

# 第七批 · 剩余包清点（J-06 ✅ 已完成 / J-09·J-10·J-12 受白名单与前置所限）

> 2026-10-27 · 执行代理 `E2UDW` · 本批**未改任何文件**（清点 + 转交清单 + 一条读数配方）。
> ⛔ 未跑 Unity、未动 git、未改正本与账主表。

## ① 逐包状态（J-06 起）

| 包 | 状态 | 说明 |
|---|---|---|
| **J-06**（3 条 clip 导入 = J-01/J-02 的前置）| ✅ **已完成（第一批）** | `工具/gen_anim_clips.py` 生成 `Rank Up` / `Division Up` / `Level Up Card`，并顺手修掉 `Booster Window - …anim` 的零 GUID 死绑定（共 4 条）。**只差调度台跑一次 Unity 让它 import 生成 `.meta`**（命令见第一批 ④-1）。 |
| **J-09**（130 `DoSimpleShake` 四跳）| ⛔ **做不了** | 6 行改动全在 `RuleEngine/Core/EffectResolver.cs`（`EmitTraitRemoved` 加可选 `effect`）+ `RuleEngine/Core/RuleCore.cs`（两处破舱点各发 `"droppod-open"` / `"droppod-destroyed"`）+ `BattleDriver.cs` 1 行消费 ⇒ 三个文件都不在本波 ✅ 白名单（RuleEngine / BattleDriver 均 ⛔）。`WarpforgeVFX/**` 侧 **0 行**（组件/preset/`OnShake` 全已就位）。 |
| **J-10**（187–190 判重族）| ⛔ **做不了** | 只需求补四条单测，落点 `RuleEngine/Core/Enchantment.cs` + `EffectResolver.cs`（本波 ⛔）。结论本身仍成立：`IsEnchantmentBuffType`（`{12,15,16}`）与 `EffectResolver.IsSameHandEffect` 已逐字段等价。 |
| **J-12**（455 Coffee.UIEffect）| ⛔ **前置缺** | 现读本工程**零** Coffee 资产（`Assets/ThirdParty/` 不存在；`find -iname "*Coffee*"` 无命中），而原版有 8 个 Coffee 相关方法体（`decomp_full` 计 8）。且派活包 §2 J-12 写明：`UIEffectBase` 依赖 uGUI `Graphic`/`IMeshModifier`/`IMaterialModifier`，而 Shell 是 `ImageQuad` ⇒ **必须另写 ImageQuad 版**（`CardPresentation/Effects/**` 那半在我白名单里，但**没有 Coffee 本体就没法写等价物**——写了就是死壳）。⇒ 需要先决定「导 dll + 搬 shader 载体」这一步（ThirdParty / 包管理器，不在本波白名单）。 |
| **J-04**（425/430/432/433/438）| ⏸ **按指令冻结** | `Battle/ImageQuad.cs` 改 `SetUVs(0, Vector4[])` —— 高险，等单独派。 |
| **J-05**（175/403/505/507/508/498/510）| ↩️ **转给 `E4BDR`** | 见 ② 的清单。 |

## ② 转给 `E4BDR` 的清单（J-05 · 7 条，全在 `BattleDriver.cs`）

按派活包 §2 J-05 的建议顺序（原文照转，⛔ 我没动）：

| 序 | 账行 | 要做什么 |
|---|---|---|
| ① | **505** | **1 行**：`SlerpUnclamped` → `LerpUnclamped` |
| ② | **508** | **补 3 句**：`StopShowingCodex` / `StopReflected` / `ToggleHighlight(false)` |
| ③ | **403** | 补 `SetBody3DVisible(false)` + 核材质交接 |
| ④ | **507** | `autoPlayFromHand` 那条路补 `PlayInvokeAnimation` + `ToggleBody3D(true)`；**先判是否复用 `CardFeel.HandToBoard`**（派活包 §8-5：`PlayInvokeAnimation` 播的 clip 字段没读）|
| ⑤ | **175** | **0 行**，只补守护断言（已对齐：`boardCenter` 支在 `:23085-23109`）|
| ⑥ | **498** | **判据待读，别动手**（方法体只有 `Destroy(this+0x370)`，前置的逐卡 `targetedSpellAnim` PPtr 没读）|
| ⑦ | **510** | **建议改判 D**（`decomp_full` + ILSpy 只有定义、0 调用点；`assets_full` 全库 0 字符串引用）⇒ 别动手 |

## ③ `InstanceBlendCalls`（更硬的那条读数）—— 只写配方

`ScenarioMaterialFader.cs` 在 **`CardPresentation/Battle/`**（本波 ✅ 名单里**没有** `Battle/**`）⇒ 按你的口径**只写配方、不改**：

```csharp
// 字段（类尾追加）：public int InstanceBlendCalls { get; private set; }
// DoScenarioBlend 开头自增：InstanceBlendCalls++;
// 自检（BattleScene.Run）：
//   换一次环境卡 → Advance 到停 → 对场景里每个 ScenarioMaterialFader 断言 InstanceBlendCalls 的增量 == 1
//   （累加的 BlendCalls 只够判「总数 = 实例数」，判不了「每实例」）
```
⚠️ 加法要跟账行号漂移一起算：`ScenarioMaterialFader.cs` 已在第五批漂过（`32→34` · `41→43` · `103→118` · `156→174` · `168→186` · `175→193`），再加两行会再 +2 ⇒ 同步点统一 `--fix`。

## ④ 结论

- **能做的都做完了**：J-01（clip + 判据）、J-02（clip + 逐条对照）、J-03（真修正）、J-06（4 条 clip）、J-07（注释订正）、J-08（`doFade` + 风险定死）、J-11（数据 + C# 半 + 260 格自检）。
- **做不了的都给了硬理由**：J-04（冻结）、J-05（转 `E4BDR`）、J-09/J-10（RuleEngine ⛔）、J-12（前置缺 Coffee 本体）。
- **待调度台**：4 条 `.anim` 的 Unity 导入；J-01 的 `Shader Graphs/Nebula` 新账；`BattleScene.Run` + 两条读数（每实例 1 次 / 撤环境旧实例怎么没的）。

---

# 第八批 · J-12 `Coffee.UIEffect` 前置判据（只读）

> 全文 → **`判据/J-12_Coffee前置判据.md`**（五问逐条 + 证据 + 最小施工单）。本批**未改任何文件**。

## 一句话结论：**能做，但要用户拍板**
Coffee 是**三个第三方 dll**（`Coffee.UIEffect` / `Coffee.UIParticle` / `Coffee.UIEffect.R`，见 `dump.cs` image 表 40/68/127），
**不是源码**；游戏侧只有一个包装类 `UIEffectTogglerCoffeeEffects`（`dump.cs:114051`），**字段只有一个** `UIEffect[] uiEffects(+0x28)`。

## 五问要点
1. **是什么/在哪**：三个 dll；`Warpforge_code` 里只有包装类桩；`decomp_full` 有 8 个体（游戏侧 4 + Coffee `UIEffect.AvailableFilters` 4）。
   本机 dll 在 `unity_run_ref/MelonLoader/Il2CppAssemblies/Il2CppCoffee.UIEffect.dll`（+ `.R`/`UIParticle`）与
   `…/Cpp2IL/cpp2il_out/Coffee.UIEffect.dll` —— 🔴 **都是 IL2CPP 重建产物，不是发行包**。
2. **8 个方法各做什么**：游戏侧 `Toggle`（RVA `0x86F2E0`）= `targetStateEnabled = option` + 遍历 `uiEffects`：`enabled = option`，
   开时取 `UIEffectTweener` 并 `Play()`（`duration` **没用**）；`DoUpdate` 与 `AnimationFinished` 的 **RVA 都是 `0x4B33B0` = 全局空桩 ⇒ 空方法**；
   `ctor` 只写 `+0x20 = 1`。Coffee 侧那 4 个属「可用滤镜开关」，不是游戏调用面。
3. **能不能搬**：dll 本地有，但**不是发行版**；依赖 `UnityEngine.UI`（`UIEffect` 是挂在 `Graphic` 上的材质/网格修改器）；
   **license 查不到**（两处目录下没有 LICENSE/NOTICE）⇒ **必须用户拍板**。
4. **最小路径**：`Assets/ThirdParty/Coffee.UIEffect/` 放 dll（勾 Editor+Standalone）→ 直接 `using Coffee.UIEffects;`（本工程无 asmdef）→
   **shader 载体**（bundle shader 落不了工程资产 ⇒ 只能运行时加载或搬资产；**这一步未核**）→ 7 个 Shell 实例接线。
5. **不搬能否等价**：**外壳能**（`Toggle` 两句语义，`DoUpdate`/`AnimationFinished` 原版就是空 ⇒ ⛔ 别自己加补间）；
   **滤镜本体不能**（那是 Coffee 的 shader，要在 `ImageQuad` 上重现 = 新写一套 shader）。

## 建议
先走 **(B) 只做外壳**：`CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` 等价类（语义照 `Toggle`，两个回调留空，视觉本体缺**出声**）——
这一步在本波白名单内，1 轮可做完；要不要搬 dll（A 路）等用户拍板。

## 没查清
① Coffee 的 shader 在 `assets_full` 里叫什么/有几个（未查）；② 7 个实例的逐实例 `uiEffects[]` 指向与触发时机（未读，派活包 §8-4 同结论）；
③ license（本地无文件）；④ `Coffee.UIEffect.R` 与原版的差别。

---

# 第九批 · Coffee 外壳（J-12 的 B 路）

> 2026-10-27 · 执行代理 `E2UDW` · 新增 **`CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs`**
> 类型检查：`TMPDIR=/tmp/wf_E2UDW bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
> ⛔ 未碰 `Assets/ThirdParty/**`（(A) 搬 dll 那条没动，等用户拍板）；未跑 Unity；未动 git；未改正本与账主表。

## ① 落了什么

| 项 | 做法（全部照原版） |
|---|---|
| 类 | `public class UIEffectTogglerCoffeeEffects : UIEffectTogglerBase`（与原版同名同基类）|
| 字段 | `public List<ImageQuad> uiEffects`（= 原版 `UIEffect[] uiEffects(+0x28)`；只装本层、不递归子件）+ `public List<Action> playOnEnable`（与 `uiEffects` **同下标**的「开时播一次」钩子，空元素 = 没有 tweener）|
| `Toggle(bool option, float duration = 0.3f)` | `targetStateEnabled = option`；逐件 `q.enabled = option`；**开时**调 `playOnEnable[i]()`。⚠️ **`duration` 原版没用** ⇒ 这里也不接成补间（注释写死）|
| `DoUpdate` / `AnimationFinished` | **留空**（原版两个的 RVA 都是 `0x4B33B0` = 全局空桩）⇒ 类里写明 ⛔ 别加补间 |
| 视觉本体缺失 | `WarnMissingVisualBody(...)` **一次性出声**（按 `name` 去重，静态 `HashSet<string>`），文案点明「只复刻了外壳、Coffee 的 `UIEffect` 滤镜没有、dll 无 LICENSE 等拍板」；`uiEffects` 为空时也走同一条并附「（`uiEffects` 是空的）」|

## ② 为什么是「钩子」而不是直接 `Play()`

原版那两句是 `uiEffects[i].GetComponent<UIEffectTweener>().Play()` —— 两者都是 **Coffee 的类型**（`UIEffect` 是挂在 uGUI `Graphic` 上的材质/网格修改器）。
我们这侧：① 工程零 Coffee 资产、装不上；② 我们的 UI 是 `ImageQuad`（不是 `Graphic`）⇒ 即便有 dll 也挂不上。
⇒ 把「那一件」换成 `ImageQuad`、把「`Play()`」换成调用方给的 `Action`，**语义一一对应**，而且不引入任何第三方类型。
接线方（Shell，⛔ 不在本波）要做的就两件事：填 `uiEffects`、可选地填 `playOnEnable`。

## ③ 待办 / 未做

1. **7 个实例的接线**：`Shell/*` 那 7 处挂本组件 + 填 `uiEffects`（派活包 §8-4：逐实例指向**未全读**）—— 不在本波白名单。
2. **(A) 搬 dll**：等用户拍板（`判据/J-12_Coffee前置判据.md` §③：本地两处 dll 都是 il2cpp 重建产物、无 LICENSE）。
   拍板后先解 ⑥-1（Coffee 的 shader 在 `assets_full` 里叫什么）——**本轮按要求没去搜**。
3. 账行号：**新文件** ⇒ 对既有落点**零漂移**（该文件还没有任何账引用）。

---

# 第十批 · `_Reveal` shader（`UIBorderGlow` 的材质 shader）

> 2026-10-27 · 执行代理 `E2UDW` · 新增 **`WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`**
> ⛔ 未跑 Unity（**shader 未编译验证**）、未动 git、未改正本与账主表。

## ① 原版那颗 shader 到底是什么（本机实读，两跳）

| 跳 | 证据 |
|---|---|
| 材质 | `assets_full/bundle_menusharedresources_assets_all/Material/Material_-678375647445659077.json` = **`Border Highlight Appear Yellow`**，`m_Shader = {m_FileID: 1, m_PathID: -4463273156108355430}` |
| shader | `assets_full/bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json` 的 `m_ParsedForm.m_Name` = **`Everguild/UI/UI Border Highlight Appear Pulsating Scale`**（1 个 subshader；`compressedBlob` 在，但本轮**没解 DXBC**）|
| 属性表（19 条，逐条照抄） | `_ColorMain` · `_ColorSecondary` · `_Color_offsets` · `_DEBUG` · `_Noise_R` · `_Noise_Strength_X_Speed_Y_Scale_Z` · `_FLIPDIRECTION` · `_Scale_X_Speed_Y` · `_StartOffset` · `_Reveal` · `_Smoothness` · `_Rotation` · `_AUTOROTATION` · `_AutoRotationSpeed` · `_QueueOffset` · `_QueueControl` · `unity_*` |
| 材质默认值（`m_SavedProperties.m_Floats` 实读） | `_Reveal 0.723` · `_Smoothness 0.032` · `_NoiseStrength 0.04` · `_Range 89` · `_RangeStartOffset 196` · `_AutoRotationSpeed -180` · `_Add 0.09` · `_ColorMask 15` · `_Rotation 0` · `_StartOffset 0`；贴图槽只有 `_Noise_R`（+ Unity 内置阴影掩码）|

⇒ **`_Reveal` 就是「揭开进度」那一格**（材质出厂 0.723），配 `_StartOffset` / `_Rotation` / `_AutoRotationSpeed` / `_Smoothness` 一起用 —— 与 `B1UI` 说的「参数接好、画面出不来（无 `_Reveal` 属性）」完全对得上。

## ② 工程里有没有同族？（没有）

`grep -rl "_StartOffset\|_Noise_R\|_Range\|_Color_offsets" --include=*.shader Assets` = **0 命中**
⇒ `WarpforgeVFX/Shaders/` 那套自建 shader（`WFAlphaMask*` / `WFDistortion` / `WFLUTBlender` / `WFMultiRay` / `WFMatcap` …）**没有可改的同族**，
只能新写。

## ③ 本轮做了什么 / **没做什么**（如实）

**做了**：`WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` ——
- **属性名逐条照抄原版那 19 条**（C# 侧已经在按这些名字读写 ⇒ 换材质即可生效，不用改代码）；
- 默认值照抄上面那批实读数；
- 形态：单 pass、`Blend SrcAlpha One`（加色）、**不写 `LightMode`** ⇒ URP 走 `SRPDefaultUnlit`、内置管线走默认 pass，两边都能渲染；
- 观感：噪声沿**极角**滚动 + 按 `_Reveal`（−1..1 映射成 0..1）揭开 + `_Smoothness` 柔化 + 径向只留边框。

**没做**：🔴 **没有**按 `compressedBlob` 的 DXBC 逐指令重建原版曲线（时间不够）。
⇒ 噪声取模、揭开方向、柔化与径向衰减这几条**都是我们挑的**，代码头已按铁律 18 标
`🔶 自己设计（非原版）… 调度台 2026-10-27 裁定`。**画面像不像要用户自己看**（⛔ 不做截图并排比）。

## ④ 需要调度台跑 Unity（shader 我验不了）

1. **导入**（让 Unity 编译这个 shader）：跑任意一次批处理即会编译；若报错，把日志贴回给我，我按报错改。
2. **建材质**：`Resources/` 下新建材质（或复用原版那份 `Border Highlight Appear Yellow` 的属性的子集），
   shader 选 `WarpforgeVFX/UI/UI Border Highlight`，**`_Noise_R` 指一张噪声图**（原版那张 `_Noise_R` 的贴图名/来源**本轮没解**，见 ⑤），
   其余按上面默认值填。
3. **换材质**：把 `UIBorderGlow` 用到的那几处（`Level Up Effect Down` 等）的材质换成它 ⇒ `[UIBorderGlow] 材质上没有 _Reveal` 那条警告应当消失。
4. **观感**：由用户看（`_Reveal` 从 −1 推到 1 时边框亮带应沿角揭开/收起）。

## ⑤ 没查清
1. **DXBC 没解**（`Shader.compressedBlob` → DXBC 的逐指令重建）——所以本 shader 是「属性表兼容的替身」，不是像素级复刻。
   工具现成：`工具/dump_shader_blob.py` / `工具/dump_shader.py`；下一步就解那条像素路径。
2. **`_Noise_R` 是哪张贴图**没解（材质里 `m_TexEnvs` 有那一格，但没往上追 `Texture2D` 文件名）。
3. **`_Range` / `_RangeStartOffset` 的确切语义**没核（本 shader 里它们只作装饰参数，未参与计算 —— 如实记）。

---

# 第十一批 · DXBC（`Everguild/UI/UI Border Highlight Appear Pulsating Scale`）

> 2026-10-27 · 执行代理 `E2UDW` · 工具：`工具/dump_shader_blob.py`（RDEF 资源名）+ `工具/disasm_dxbc.py`（SHDR 指令流，已落盘）
> ⛔ 未跑 Unity、未动 git、未改正本与账主表；⛔ 未做截图并排比。

## ① 解出来了什么

1. **字节码解得开**：`dump_shader_blob.py "UI Border Highlight Appear Pulsating Scale"` ⇒
   `shaders_assets_all.bundle` 与 `wf_shaders.bundle` **各一份、753,156 字节、魔数 `93 00 00 00`**（同一个 shader，两份包）
   —— 注意 ⛔ 不是 `m_SubProgramBlob`（那格是 None），走的是 `compressedBlob`。
2. **PS 指令流解得开**：`disasm_dxbc.py "…" --stage ps --out uni/` ⇒ **4 个变体、18,671 行**，
   落在 `d:/4/Unity/_tmp_view/dxbc/ui_border_ps.asm`（跑 Windows 自带 `d3dcompiler_47.dll` 的 `D3DDisassemble`）。
3. **26 个常量/纹理名（RDEF 实读）**：
   `_AUTOROTATION` `_AutoRotationSpeed` `_ClipRect` `_ColorMain` `_ColorSecondary` `_Color_offsets` `_DEBUG`
   `_FLIPDIRECTION` `_GlobalMipBias` `_LightDirection` `_MainTex` `_Noise_R` `_Noise_Strength_X_Speed_Y_Scale_Z`
   `_NonJitteredViewProjMatrix` `_PrevViewProjMatrix` `_ProjectionParams` `_Reveal` `_Rotation` `_Scale_X_Speed_Y`
   `_ScaledScreenParams` `_ScreenParams` `_ShadowBias` `_Smoothness` `_TimeParameters` `_UIMaskSoftnessX` `_UIMaskSoftnessY`
   ⇒ 它是一只 **URP UI shader**（带 `_ClipRect` + `_UIMaskSoftnessX/Y` 的 UI 遮罩裁切、`_ScreenParams`/`_ScaledScreenParams` 屏幕空间）。
   `_GrabPassTransparent` / `_CameraOpaqueTexture` / `_CameraColorTexture` / `_CameraDepthTexture` / `_RTHandleScale` 那一组
   在工具输出里**显示为「—」（未命中）** ⇒ 它**不**采样抓屏。

## ② 已经**坐实**的三条（⛔ 不是推测）

| 结论 | 证据 |
|---|---|
| **`_Range` / `_RangeStartOffset` / `_Add` / `_ColorMask` / `_NoiseStrength`（旧名）是死值** —— 连 shaderlab **属性都没声明** | shader JSON 的 `m_ParsedForm.m_PropInfo.m_Props` 只有 19 条，**这 5 个不在里面**；它们只出现在**材质**的 `m_SavedProperties.m_Floats` ⇒ 经典的「原版材质带残留值」（CLAUDE.md §三 那条坑的又一实例）|
| **`_StartOffset` 原版像素代码【不读】** | 它是**声明属性**，但**不在**上面那 26 个常量名里 ⇒ ⇒ 已从我们的替身算式里**移除**（见 ③） |
| **参与计算的确认名单** | `_ColorMain` · `_ColorSecondary` · `_Color_offsets` · `_DEBUG` · `_Noise_R` · `_Noise_Strength_X_Speed_Y_Scale_Z` · `_FLIPDIRECTION` · `_Scale_X_Speed_Y` · `_Reveal` · `_Smoothness` · `_Rotation` · `_AUTOROTATION` · `_AutoRotationSpeed` + URP/UI 自带那批 |

## ③ 本轮改动（`WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`）

- **移除** `_StartOffset` 对噪声坐标的贡献（原版不读它，见 ②），并在注释里写清判据与「`_Range` 那 5 个是死值」。
- 其余仍按 `🔶 自己设计`（噪声取模、揭开方向、柔化、径向衰减）—— **因为 ④ 卡住了，那 4 问今天没坐实**。

## ④ 🔴 卡在哪（如实）：cb0 偏移 → 属性名 的映射**这份导出里拿不到**

- 反汇编里只有 `cb0[131].xxyx` 这种**数字偏移**，没有名字（DXBC 本来就不带名）。
- 按你说的路走 `SerializedPass.m_CommonParameters` —— **这份 JSON 导出里是空的**：
  `sub0 pass0..pass3` 的 `vectors = 0`、`textures = []`（我逐 pass 打印过）⇒ **偏移→名字对不上**。
- RDEF 里**有**名字（工具打出的 26 个），但工具**没有**解析 `ConstantBuffer.Variables[]` 的 `startOffset`。
  **下一步很具体**：用 `工具/dxbc_seg.py` 切出 RDEF 段 → 解析每个 `ConstantBuffer` 的 `Variables[]`（`nameOffset` / `startOffset` / `size`）
  ⇒ 得到 `cb0[byteOffset] → 名字` 表 ⇒ 再把已落盘的 `ui_border_ps.asm`（4 个变体）里对 `_Reveal` / `_Smoothness` / `_Noise_R` 的用法逐条读出来。
  **那 4 个问题（`_Reveal` 怎么参与 / 噪声取模 / 柔化进哪一端 / 径向是否靠 `_Range`）今天仍未定，⛔ 不编一个答案。**

## ⑤ 另一条发现（影响替身，但今天不改）

原版是 **URP UI shader**（`_ClipRect` + `_UIMaskSoftnessX/Y`）⇒ 它**会跟着 UI 遮罩裁切**。
我们这侧 UI 是 `ImageQuad`，**不提供 `_ClipRect`**（该属性默认 (0,0,0,0)）⇒ 若照抄那段 `UNITY_UI_CLIP_RECT`，整块会被**裁没**。
⇒ 替身**不加**裁切，并在报告里记成「已知形态差」（要加的话得先让 `ImageQuad` 提供 `_ClipRect`，那是 J-04 那一带的活）。

---

# 第十二批 · RDEF 偏移表（工具已写；本条 shader **RDEF 被剥** ⇒ 四问仍未定）

> 2026-10-27 · 执行代理 `E2UDW` · 新增 **`工具/dxbc_rdef.py`**（RDEF 常量缓冲解析 + asm 交叉引用统计）
> ⛔ 未跑 Unity、未动 git、未改 dotnet/正本。报告外**只改了两处**：新工具 + `WFUIBorderHighlight.shader`（上一批那处 `_StartOffset`）。

## ① 工具写出来了（可用、可复核）

`工具/dxbc_rdef.py`：
- 复用 `dump_shader_blob.blob_of()` 解压（**不重复实现** LZ4），扫 `BUNDLES + BUILTIN_FILES` 里所有 `Shader`；
- 名字取 **`m_ParsedForm.m_Name`**（`m_Name` 对内置 shader 恒空 —— 这是 `dump_shader_blob.py:118-140` 的老经验，照抄）；
- 在解压后的字节流里找 `RDEF` → 解析 `ConstantBuffer`（`nameOffset / varCount / varOffset / size`，条目 24 B）
  与 `Variables`（`nameOffset / startOffset / size`，条目 24 B）⇒ 输出 `cbN[reg] → 属性名`（`reg = startOffset / 16`）；
- `--asm <反汇编文件>` ⇒ 顺便统计每个名字所在寄存器被引用多少次；`--only a,b,c` 只看关心的名字。

## ② 🔴 本条 shader 上跑出来的结果：**RDEF 被剥，名字拿不到**

```
==================== Everguild/UI/UI Border Highlight Appear Pulsating Scale （shaders_assets_all.bundle） 字节 753156
  DXBC 容器 128 个 · SHDR 128 · RDEF 0
```
（`wf_shaders.bundle` 里那一份同样 753,156 字节、同样 **RDEF 0**）

⇒ 这与 `工具/disasm_dxbc.py` 文件头记的那条**同一种情况**：「20 个变体都带 `SHDR` 指令段（**只缺 RDEF**）」。
**128 个 DXBC 容器全部只有 `SHDR` / `ISGN` / `OSGN`** ⇒ 字节码里**没有**常量缓冲的名字表；
而另一条路（`SerializedPass.m_CommonParameters`）在 `assets_full` 的导出里是**空的**（逐 pass 核过）
⇒ **`cb0[131].xxyx` 这样的偏移，今天在本机对不上名字**。

## ③ 四问的现状（⛔ 全部仍未坐实，不编）

| 问 | 现状 | 依据 |
|---|---|---|
| `_Reveal` 怎么参与（映射/取样 vs 阈值/方向）| ❌ 未定 | 只有数字偏移 |
| 噪声取模（`_Noise_Strength_X_Speed_Y_Scale_Z` 三分量 + `_Noise_R` 采样坐标）| ❌ 未定 | 同上 |
| `_Smoothness` 进 `smoothstep` 哪一端 | ❌ 未定 | 同上 |
| 径向只留边框靠什么 | ✅ **已排除 `_Range`/`_RangeStartOffset`**（那两个**连属性都没声明**、也没进 26 个名字）| 第十一批 ② |
| （附带）哪些属性真参与 | ✅ 已列 13 个 + URP/UI 自带那批；`_StartOffset` **不读** | 第十一批 ② |

## ④ 下一步（**具体、可执行**，留给接手的代理）

1. **借偏移表**：扫 `Everguild/UI/*` 同族 shader，找一只**带 RDEF** 的（`dxbc_rdef.py` 会告诉我们 RDEF 是否 0）；
   同族的 include 结构一致 ⇒ `cb0` 里那批 `_ColorMain/_Reveal/...` 的**字节偏移应当一致**，借过来即可读本条。
2. **差分实验**（要 Unity，⛔ 我做不了）：同材质把 `_Reveal` 设两个值各渲一帧，比对输出的哪一路变化 ⇒ 反推它进的是取样还是阈值。
3. **查 URP 的 include 顺序**：若 `#include "UnityUI.cginc"` 等决定了 cb 布局，那 26 个名字的**声明顺序**本身就能给出偏移（`m_ProperyInfo` 的 19 条 + URP 7 条）。这条路**不需要 RDEF**，但需要逐条核对 include 的 cbuffer 声明顺序 —— 我没做（上下文/时间不够）。

## ⑤ 结论
- **工具交付了**（`工具/dxbc_rdef.py`，带 `--asm` 交叉引用、RDEF=0 时会明确报「被剥」而不是静默）。
- **本条 shader 的四问仍未定**，原因写死在 ②：**RDEF 被剥 + `m_CommonParameters` 空** ⇒ 双路都不通；⛔ 依旧不编答案。
- `WFUIBorderHighlight.shader` 保持第十一批的状态（属性名兼容 + 已按证据移除 `_StartOffset`；其余 `🔶` 保留），**未编译验证**。

---

# 第十三批 · 借偏移表（`Everguild/UI/*` 全扫）—— **这条路也断了**

> 2026-10-27 · 执行代理 `E2UDW` · 只读扫包（`d:/2`）+ 报告；⛔ 未跑 Unity、未动 git、未改代码/正本。

## ① 全扫结果（`bundle_shaders_assets_all` + wf_shaders，`Everguild/UI/*` 共 10 只）

| DXBC 容器 | RDEF | 与本条属性名重合（共 12 个可比） | shader |
|---:|---:|---:|---|
| 128 | **0** | **12** | `Everguild/UI/UI Border Highlight Appear Pulsating Scale` ← **本条** |
| 160 | **0** | **11** | `Everguild/UI/UI Border Highlight Appear` ← 最近的兄弟 |
| 48 | **0** | 0 | `Everguild/UI/Campaign Points` |
| 8 | **0** | 0 | `Everguild/UI/Card ImageUI Simple` / `… Simple GreyScale` / `Card Ready for level up` / `Color Change` / `Greyscale` / `UI Border Mask` / `UI Distort Color Change`（各 8）|

**`with RDEF: 0`（10 只里 0 只带 RDEF）** ⇒ **借兄弟 shader 偏移表这条路，断。**
（最近的兄弟 `…Appear` 属性重合 11/12，**但它同样 RDEF=0** ⇒ 借不到。）

## ② 🔴 副产品（全局结论，写死了免得以后白跑）

- **`Everguild/UI/*` 这一族 10 只 shader，RDEF 一律被剥**（只有 `SHDR`/`ISGN`/`OSGN`）。
- 与 `工具/disasm_dxbc.py` 文件头已记的 `Extra Color`（「20 个变体只缺 RDEF」）**同一种情况**
  ⇒ **「按 `cbN[reg] → 名字` 读算式」这条路，在 `Everguild/*` 这族上不要指望**；
  以后谁想读 shader 算式，**先跑 `工具/dxbc_rdef.py` 看 RDEF 是不是 0**（它现在会明确报「被剥」）。
- ⚠️ **范围限定**：本轮只扫了 `Everguild/UI/*`（10 只）。**全库 shader 的 RDEF 覆盖率没测**（
  每只都要 `blob_of` 解压 700+ KB，全库跑会很久）⇒ 全局比例**查不到**，我只报这一族。

## ③ 四问现状（与第十二批相同，未变）

- ❌ `_Reveal` 怎么参与 · ❌ 噪声取模 · ❌ `_Smoothness` 进哪一端 —— **仍定不了**（缺偏移→名字）。
- ✅ 与 `_Range`/`_RangeStartOffset` 无关（第十一批，属性都没声明）。
- ✅ 参与名单 13 个 + `_StartOffset` 不读（第十一批）。

## ④ 唯一剩下的路（按代价排）

1. 🟢 **第三条路（我没验，最值得先试）**：`m_CommonParameters` 在 **`assets_full` 的 JSON 导出里是空的**，
   但那可能只是**导出丢了**；用 **UnityPy 直接读 bundle**（本工具已经在做）取
   `sh.m_ParsedForm.m_SubShaders[i].m_Passes[j].m_CommonParameters.m_VectorParams/m_TextureParams`
   —— 若那里有 `m_Index`，**偏移→名字立刻就有**，根本不需要 RDEF。
   **一行验证**：在 `工具/dxbc_rdef.py` 里把 `pf.m_SubShaders[*].m_Passes[*].m_CommonParameters` 打一遍即可（本轮没做，上下文不够）。
2. 🟡 **include 声明顺序反推**：`m_PropInfo` 的 19 条 + URP 那 7 条的 cbuffer 声明顺序本身能给出偏移（不需要 RDEF），
   但要逐条核 include —— 没做。
3. 🔴 **差分实验（要 Unity，我做不了）**：同材质把 `_Reveal` 设两值各渲一帧、比哪一路变化 ⇒ 反推「取样 vs 阈值」；
    噪声同理改 `_Noise_Strength_X_Speed_Y_Scale_Z` 的分量看画面/`GetVector` 的哪一格起作用。

## ⑤ 结论
「借偏移表」第 1 条**走不通**（同族 0 只带 RDEF），已按你的要求明说；四问照旧**不编**。
下一步建议从 ④-1 起（代价最小，一行验证）。

---

# 第十四批 · 直读 bundle 的 `m_CommonParameters`（④-1）—— **这条路也断了**

> 2026-10-27 · 执行代理 `E2UDW` · UnityPy 直读 `d:/2/Unity/...shaders_assets_all.bundle`（⛔ **没读**已落盘的 JSON 导出）
> ⛔ 未跑 Unity、未动 git、未改代码/正本。

## ① 实跑输出（原文）

```
Everguild/UI/UI Border Highlight Appear Pulsating Scale | subshaders 1
 sub0 pass0 cp=None vec=0 tex=0
 sub0 pass1 cp=None vec=0 tex=0
 sub0 pass2 cp=None vec=0 tex=0
 sub0 pass3 cp=None vec=0 tex=0
```

- 取法：`sh.m_ParsedForm.m_SubShaders[i].m_Passes[j].m_CommonParameters`（UnityPy 直读 bytes ⇒ `obj.read()`）。
- 结果：**`m_CommonParameters` 这个字段在这份 build 里根本不存在**（`getattr(..., 'm_CommonParameters', None)` = **None**，
  不是"空数组"）⇒ 4 个 pass 全 None。
- ⇒ **不是"JSON 导出丢了"，是序列化里就没有**（与我们同一轮读到的 `m_ParsedForm`/`m_SubShaders`/`m_Passes` **都能读出来**，
  说明不是取法问题）。

## ② 结论：④-1 断，而且这条**从此可以写死**

- 先前项目里那条成功案例（「读 Unity 序列化反射 `SerializedPass.m_CommonParameters`，不必再找 RDEF」）
  **在本 build（Unity 6000.3 序列化 / 这份 shader）上不成立** —— 字段不在。
- 叠加第十二/十三批：**RDEF=0**（10/10 同族）+ **`m_CommonParameters` = None**
  ⇒ 「`cbN[reg] → 属性名`」这条路在本机**三种取法全断**：
  ① RDEF（字节码里没有）② JSON 导出的 m_CommonParameters（空）③ UnityPy 直读 bundle 的 m_CommonParameters（字段不存在）。

## ③ 四问现状（**第四批未变，仍未定**）

| 问 | 状态 |
|---|---|
| `_Reveal` 怎么参与（映射 / 取样 vs 阈值 / 方向）| ❌ 定不了 |
| 噪声取模（三分量各进哪里、`_Noise_R` 采样坐标）| ❌ 定不了 |
| `_Smoothness` 进 `smoothstep` 哪一端 | ❌ 定不了 |
| 「径向只留边框」靠什么 | ✅ 已排除 `_Range`/`_RangeStartOffset`（第十一批） |

## ④ 转 ④-2（下一件，我没做）

**include 声明顺序反推**：`m_PropInfo` 的 19 条属性 + URP/UI 自带那 7 条（`_ClipRect`/`_UIMaskSoftnessX,Y`/`_ScreenParams`/`_ScaledScreenParams`/`_ProjectionParams`/`_TimeParameters`）
在 `UnityUI.cginc` / URP core 里的**声明顺序**决定 `$Globals` 的 `cb0` 槽位顺序 ⇒ 按顺序数就能把 `cb0[reg]` 对到名字。
做法：① 取 URP 包（`PackageCache/com.unity.render-pipelines.universal@*/`）里那几个 cginc 的 cbuffer 声明顺序；
② 把本 shader `m_PropInfo` 19 条按声明顺序接在后面（Unity 的规则：**属性声明顺序 = cb 里的顺序**，中间会插入系统项）；
③ 生成 `cb0[reg] → 名字` 候选表 ⇒ 回读 `ui_border_ps.asm`（已落盘、4 个变体 18,671 行）核对**引用次数/上下文是否自洽**（例：`_Noise_R` 必须是 `sample` 指令的纹理寄存器、`_Reveal` 应是标量比较/插值）。
⚠️ 这条路是**推断**（顺序规则 + 自洽核对），不是硬证据 —— 报告里必须标「候选表」。

## ⑤ 结论
④-1 **断**，且**三种取法全断**这件事建议写进正本（以后谁想按偏移读 shader 算式，先看这三条）。
四问仍未定，⛔ 没编。

---

# 第十五批 · CardFeel 三条（7 震屏幅度表 / 8 ResolveUnitShake / 9 ReturnToDeckAnim）

> 2026-10-27 · 执行代理 `E2UDW` · 类型检查 `TMPDIR=/tmp/wf_E2UDW bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**（改完即跑）
> ⛔ 未跑 Unity、未动 git、未改正本与账主表、未碰 `BattleDriver.cs` / `CardDisplayWindow.cs`。

## 0. 先纠一处编号（否则会做错活）

`判据/特效_施工单_20261027.tsv` 的 **第 7/8/9 行**是另外三条 —— `VoiceLinesPopUpShowedUp` / `ShowPlayerActionAnim` / `PlayAnim`，
**三条全阻塞在 `BattleDriver.cs`**（`16642` / `17203` / `22665`）。
⇒ 你这次说的「7/8/9」是**方法名**（`ResolveAttackAnimationEffects` / `ResolveUnitShake` / `ReturnToDeckAnim`），不是那张表的行号。
本批按**方法名**做。⛔ 若你要的是那三行，得等 `E4BDR` 放 `BattleDriver.cs`。

## 7. `ResolveAttackAnimationEffects` 震屏幅度表 —— ✅ **逐格核对：已对齐**（只改了一处分支形状）

### 判据（🥇 `.c` 逐句 + 常量实读）
`decomp_full/CardScript__ResolveAttackAnimationEffects.c:28-52`：
```
iVar2 = SupportMethods____GetUnitSize(this)
iVar2 ∈ {0,1} ⇒ uVar8 = DAT_1834b2bbc
iVar2 == 2    ⇒ uVar8 = DAT_1834b2df0
else { if (iVar2 != 3) goto LAB_1805f8b18;   ← 非 0/1/2/3 **落回 2.0 那一支**
       uVar8 = DAT_1834b2e00 }
… OverwriteCameraShakePreset.SetDirection(dir) → SetAmplitude(uVar8) → Play()
```
常量实读（`工具/read_literal.py`，4B float）：`0x1834b2bbc = **2.0**` · `0x1834b2df0 = **4.0**` · `0x1834b2e00 = **8.0**`。

### 对照我们（`CardPresentation/Core/CardFeel.cs:309-317`）
| `GetUnitSize` | 原版 | 我们（改前）| 我们（改后）|
|---|---|---|---|
| 0 / 1 | 2.0 | 2.0 | 2.0 ✅ |
| 2 | 4.0 | 4.0 | 4.0 ✅ |
| 3 | 8.0 | 8.0 | 8.0 ✅ |
| 其它（>3）| **2.0**（`!= 3 ⇒ goto`）| **8.0**（`>= 3`）❌ | **2.0** ✅ |

⇒ **表本身原来就是对的**（2/4/8 三档逐格相同），唯一偏差是**分支形状**：我们把 `>= 3` 归进 8.0 档，原版是 `== 3`、其余落回 2.0。
已改成 `== 3` 并把这行 `.c` 的形状写进注释。**行为上今天不变**（`GetUnitSize` 的契约是 0..3，见 `SupportMethods__GetUnitSize.c:11-16`），但形状现在与原版一致（铁律 11：逐句，不留"看起来等价"的口子）。

## 8. `ResolveUnitShake` —— ❌ 本批**没做**（如实）

- 已知：真体 = `decomp_full/BattleManager._ResolveUnitShake_d__472__MoveNext.c`（多段：`WaitForSeconds` → `IsInPlay` 闸 → `DoUnitShake`）；
  我们的对应物：`CardFeel.ShakeUnit` + `ShakeAmplitudeForUnitSize`（档位 2/4/8 已与第 7 条同一份，doc 引 `:167-181`、`:139-147` 两处互证）。
- **差什么**：要把 `MoveNext` 的**段序**（等待 → 在场上才抖 → 抖的参数）逐段读出来，与我们 `BattleDriver` 的调用点对齐；
  而那一个调用点在 `BattleDriver.cs`（⛔ 不归我）⇒ **本批即便读完也无法落地**。
- ⇒ **建议**：这条与 `ResolveAttackAnimationEffects` 的**消费侧**一起，等 `BattleDriver.cs` 空出来再做（判据可以先读完落成文件）。

## 9. `ReturnToDeckAnim` —— ❌ 本批**没做**（如实）

- 你已指出 `.c` 体薄、疑丢 ⇒ 正确顺序是**先读 ILSpy**（`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs` 的 `ReturnToDeckAnim` 段）。
- **差什么**：还没读（本批上下文用尽）。读法固定：`grep -n "ReturnToDeckAnim" out_cpp2il_dev_ilspy/CardScript.cs` →
  读体 → 若也薄，按「RVA 0x4B33B0 = 空桩」或「ICF 折叠」二分（两块先例都记在 CLAUDE.md）。

## 该跑哪些宿主自检
`BattleScene.Run`（震屏幅度只在战斗链上）；本批改动**行为中性**（分支形状），但同一函数被 `BattleScene` 的断言引用，跑一次稳。
