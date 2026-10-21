# W61b 特效线 · `ShapeModule` 资产引用导出 + E 组可判清单

> 执行代理 C · 2026-10-21 第六十一会话。**改了 1 个 `.cs`**（`EffectExporter.cs`，白名单内）。
> **没有跑 Unity**（铁律 12 / 13·3）；没动 git、没改任何正本与台账。
> 重导与扫 A/B **要主对话执行** —— 命令在 §3、§4。

---

## 0. 一句话

1. **`ShapeModule` 的资产引用（`m_Mesh` / `m_Sprite` / `m_Texture`）补上了导出分支** —— 原来那三条网格路一条都收不到它，
   落盘就是「`fileID` 留着原包 pathID、`guid` 全 0」的伪引用。现扫 = **`m_Mesh` 45 条 / 12 件 · `m_Sprite` 16 条 / 2 件 · `m_Texture` 2 条 / 1 件**。
2. ⚠️ **量级要说实话**：这 45 条 `m_Mesh` **全是死字段**（原版没有任何 PS 用 Mesh 形状发射）。修它 = 清伪引用，
   **不解释** Necron 那条 E 的亮度，**也不是**「补回一块缺的视觉」。判据见 §2.2。
3. E 组两条（`Raging Storm` 0.65 · `ShieldTraitEffect_Idle Necron Variant` 1.42）**判据仍未闭合** ——
   本报告只给**可执行的判别方案**（§4），**不给结论**。

---

## 1. 结论（改了什么）

`EffectExporter.Export()` 里新增一段 **`ShapeModule` 资产引用**的导出（插在 `SkinnedMeshRenderer` 那条之后、
SpriteRenderer 那条之前），逐 `ParticleSystem` 处理三个字段：

| 字段 | 类型 | 处理 | 读法（两条路都走） |
|---|---|---|---|
| `m_Mesh` | `Mesh` | `ImportMesh` → 写回 | `ps.shape.mesh`；为空时再读原生序列化字段 `ShapeModule.m_Mesh` |
| `m_Sprite` | `Sprite` | `ImportSprite` → 写回 | `ps.shape.sprite`；为空时读 `ShapeModule.m_Sprite` |
| `m_Texture` | `Texture2D` | `ImportTexture` → 写回 | `ps.shape.texture`；为空时读 `ShapeModule.m_Texture` |

四条纪律（都写在代码注释里，判据见 §2）：

- **只导「不是工程资产」的**：`AssetDatabase.GetAssetPath(x)` 非空 ⇒ 跳过（重复导 = 自己导自己）。
- **Unity 内置图元不碰**：`Sphere/Capsule/Cylinder/Cube/Plane/Quad` 按**对象同一性**判断
  （`Resources.GetBuiltinResource<Mesh>(名 + ".fbx")` 回的就是同一颗），⛔ 不按名字下结论。
  它们在 prefab 里本来就序列化成 `guid: 0000000000000000e000000000000000`，落盘完全正常。
- **写完读回来**：`ps.shape.X == 导入物`；不成立就退到 `SerializedObject`（`ShapeModule.m_Mesh` 等原生序列化名）再写一次；
  还不行就 **`Debug.LogWarning` 点名**（⛔ 不许静默）。
- **整段包 `try/catch`**：某个 PS 出问题只跳过它自己，不让整件 prefab 导出失败。

另外把 **7 件**要重导的 prefab 加进了 `ListedPrefabs`（`RunListed()` 的唯一入口 ——
它只认这个硬编码数组，**没有**环境变量能传名单），并**新增了一个过滤器 `WFEFFECT_ONLY`**
（`ListedTargets()`，逐字白名单过滤）。**为什么必须有它**：那张表里现在**还躺着
`EnvironmentalCondition Sororitas Raging Storm`** —— 不筛就一起重导，而 W61 明确「Raging Storm 暂不重导」
（21:54 刚导过、还是 `尺子不稳定名单` 第 1 名）⇒ 两件事会**再也分不开**。见 §3。

---

## 2. 证据

### 2.1 缺陷面（2026-10-21 现扫 `MyGame/Assets/WarpforgeVFX/Prefabs/*.prefab`，964 件）

判据 = `工具/check_prefab_refs.py` 的口径：**`fileID != 0` 且 `guid` 全 0**（`fileID: 0` 是合法空引用，别数）。

| 字段 | 坏引用 | 分布 |
|---|---|---|
| `ShapeModule.m_Mesh` | **45 条 / 12 件** | `Godspear Warhead Full` 19 · `Vortex Explosion Massive` 15 · `Goff_ProperKilly` 2 · `ShieldTraitEffect_Idle` **5 个变体各 1** · `EC_Target_user` / `EC_Target_user Lucius` / `Sword_10x_EC` / `Will Of Gork` 各 1 |
| `ShapeModule.m_Sprite` | **16 条 / 2 件** | `Buff_Sororitas_quick_fire` 7 · `Buff_Sororitas_quick_fire_intense` 9 |
| `ShapeModule.m_Texture` | **2 条 / 1 件** | 同上两件各 1 |

`ShieldTraitEffect_Idle Necron Variant.prefab` 的实样（修前）：

```
行 15473:  m_Mesh: {fileID: 8804033814099603246, guid: 00000000000000000000000000000000, type: 0}
```

- 那段 `ShapeModule` 属于 GameObject **`Lightning Burst`**（组件 `!u!198 &8572015859573066892`，行 14774），`type: 18`。
- `fileID 8804033814099603246` 对应的原版网格 = **`Shield Ico`**（`assets_full/…/Mesh/Shield Ico.obj`，
  `grep -c "^v " = 187` 顶点）—— 工程里**已经有**这颗网格：`Assets/WarpforgeVFX/Meshes/Shield Ico.asset`
  （guid `4a834e3b56e043d4b9ef315f25f0ecf7`），而且同 prefab 的 `MeshFilter`（行 9833）**就指着它**。
  ⇒ 缺的只是「把同一颗网格也赋给 `ShapeModule`」，**不缺资产**。

### 2.2 「这 45 条是死字段」—— 判据（这是本轮最重要的一条，别被 W61 的推测带偏）

W61 §2 原来写「它只影响 0.15–0.5s」—— **那条推测不成立**。现核两路：

**① 导出 prefab 侧**：45 条坏引用按所在 `ShapeModule.type` 分布 =

```
0（Sphere）14 · 10（Circle）10 · 2（Hemisphere）7 · 4（Cone）6 · 18（Rectangle）5 · 12（SingleSidedEdge）2 · 5（Box）1
```

**type 6（Mesh 形状）一条都没有。**

**② 原版解包侧**：扫 `d:/2/新解包资源/assets_full/bundle_*/ParticleSystem/*.json` **15,311 个 PS**：

```
type 分布：0:2769  2:822  4:8161  5:186  6:46  8:47  10:2534  12:385  13:12  15:12  16:13  17:3  18:319  19:2
非空 m_Mesh 的 type：0:156  2:7  4:6  5:1  10:47  12:4  16:4  18:5      ← type 6 = 0 个
```

⇒ **原版 46 个 Mesh 形状（type 6）的发射器自己就一个网格都没接**；所有非空 `m_Mesh` 都挂在「不是 Mesh 形状」的模块上
（Unity 改形状类型时**不会清** `m_Mesh` ⇒ 陈旧值）。`m_Mesh` 只在 `type == 6` 时参与取样。

**enum 映射的反证**（我用的 type 编号不是猜的）：`type 13` 的 12 个里 11 个有非空 `m_MeshRenderer`、
`type 19` 的 2 个有非空 `m_Sprite` + `m_Texture` —— 与 Unity `ParticleSystemShapeType` 的
`MeshRenderer = 13 / Sprite = 19` 逐一对上。

**③ 台账侧**：这 12 件里 8 件在 `特效还原台账.tsv` 有行，**全部 Z（对得上）**：

| 效果 | 判定 | 亮度比中位 |
|---|---|---|
| Godspear Warhead Full | W（两边全程空） | — |
| Vortex Explosion Massive | Z | 0.99 |
| Goff_ProperKilly | Z | 1.00 |
| EC_Target_user / EC_Target_user Lucius | Z / Z | 0.99 / 0.99 |
| Sword_10x_EC | Z | 1.00 |
| Will Of Gork | Z | 1.00 |
| ShieldTraitEffect_Idle（本体 + EC/Sororitas/TAU 变体） | Z ×4 | 1.00 / 1.00 / 1.13 / 1.37 |

⇒ **修这条不会让任何一件的亮度变**。要动 Necron 的 1.42，得走 §4.3 那条 A/B，别指望这里。

### 2.3 代码位置（改后行号）

| 内容 | 位置 |
|---|---|
| `ShapeModule` 导出段（三段：mesh / sprite / texture） | `MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:1912-2055` |
| `BuiltinMeshNames` + `IsUnityBuiltinMesh` | `EffectExporter.cs:4303-4325` |
| `ForceShapeRef`（`SerializedObject` 兜底写 + 序列化字段复核） | `EffectExporter.cs:4327-4348` |
| `SerializedShapeRef<T>`（原生序列化字段兜底读） | `EffectExporter.cs:4350-4358` |
| `ListedPrefabs` 新增 7 件 | `EffectExporter.cs:861-878` |
| `ListedTargets()` + `WFEFFECT_ONLY` 过滤器 | `EffectExporter.cs:883-905`（`RunListed()` 里三处改用它：`:963` / `:969` / `:1008`） |

> 为什么「读」也要两条路：`ShapeModule.mesh` 这个 getter 在**形状类型不是 Mesh 族**时会不会挡住，
> **本机没有实测过**（不能跑 Unity）。只看 getter 的话，万一它返回 null，这一支会**静默跳过** ——
> 重导一场、画面上什么都没变、日志里也一句没有，那是**最坏的结果**。所以 getter 空时再读一次原生序列化字段。

---

## 3. 重导执行单（主对话照抄）

### 3.0 前置

```bash
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
# 🔴 必须先 unset ELECTRON_RUN_AS_NODE，否则 Unity 静默失败（CLAUDE.md §二）
mkdir -p d:/4/_tmp_view/bak_w61b
```

**这一批「会存盘」吗？** `RunListed()` **不**跑自检宿主、**不**重建场景，只重写这 7 件 prefab +
可能新增/覆盖 `Meshes/*.asset` / `Textures/*.png` / `Materials/*.mat`（固定路径覆盖写 ⇒ guid 不变）。
⚠️ 但它**会整件重写 prefab**（YAML 全文重排）⇒ 跑完**必须** `git diff --numstat` 看那 7 件的行数变化，
并单独核一遍「除了 `ShapeModule` 那几行，还有没有别的地方变了」。

### 3.1 Batch A（必做 · 5 个 ShieldTraitEffect_Idle 变体 + 2 件同族精灵）

`ListedPrefabs` 已含这 7 件（`EffectExporter.cs:861-878`）：5 个 `ShieldTraitEffect_Idle*` +
`Buff_Sororitas_quick_fire` / `_intense`。

```bash
unset ELECTRON_RUN_AS_NODE && \
WFEFFECT_ONLY="ShieldTraitEffect_Idle,ShieldTraitEffect_Idle Emperor's Children Variant,ShieldTraitEffect_Idle Necron Variant,ShieldTraitEffect_Idle Sororitas Variant,ShieldTraitEffect_Idle TAU Variant,Buff_Sororitas_quick_fire,Buff_Sororitas_quick_fire_intense" \
"$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectExporter.RunListed \
  -logFile "d:/4/_tmp_view/export_shapemodule_w61b.log"
```

> ⚠️ `WFEFFECT_ONLY` 是**本轮新加的**（`ListedTargets()`，`EffectExporter.cs:883`）：**逐字**匹配 `ListedPrefabs`，
> 逗号分隔。**不设它 = 老行为（整张表 43 件全导）** —— 那会把 `EnvironmentalCondition Sororitas Raging Storm`
> 一起重导（W61 明确「暂不重导」）⇒ **别省这一行**。
> 名字打错 = 那件不导，日志里会有 `WFEFFECT_ONLY 里的 … 不在 ListedPrefabs 表里`。

**日志里看什么**（`grep "^EX1 "`）：

0. `EX1 \`WFEFFECT_ONLY\` 生效：本趟只导 **7** 件（表里共 43 件）—— …` —— 先确认这一行是 7。
1. `bundle 已加载：源包目录 N 个（其中特效主包 √）+ 重打的小包 M 个` —— 主包必须是 **√**。
2. 每一件都要有一行 `已导出 ...`；最后一行 `指定 prefab 导出完成：**7/7**`
   ⚠️ 不等于 ⇒ 哪件没取到，日志里有逐条 `按名字加载 ... 没取到`。
3. 🆕 **本轮新增的那一行**：

   ```
   EX1 `ShapeModule` 资产引用（m_Mesh/m_Sprite/m_Texture）：接回 **23** 条
   ```

   **期望 ≈ 23**（Shield 5 条 mesh + `Buff_Sororitas_quick_fire` 7 sprite + 1 texture +
   `_intense` 9 sprite + 1 texture = 23）。差几条就要看紧跟着的逐条 `LogWarning`
   （`导不成工程 ./精灵/贴图` 或 `**写不进去**`）。
4. ⛔ **出现 `**写不进去**` 或 `兜底路也不通` ⇒ 停手回传**（那说明 API setter 与
   `SerializedObject` 两条路都不落地，是我没实测到的那一档）。

**验收（三样，缺一不可）**：

```bash
# ① 坏引用归零（三个字段都查）
python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py \
  "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle Necron Variant.prefab" \
  "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle.prefab" \
  "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle TAU Variant.prefab" \
  "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle Sororitas Variant.prefab" \
  "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle Emperor's Children Variant.prefab" \
  "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/Buff_Sororitas_quick_fire.prefab" \
  "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/Buff_Sororitas_quick_fire_intense.prefab" \
  --fields m_Mesh,m_Sprite,m_Texture
```

   期望：7 件 × 3 字段全部 `坏引用 0`（修前：Shield 5 件各 `m_Mesh 1`；Buff 两件 `m_Sprite 7 / 9`、`m_Texture 1 / 1`）。

```bash
# ② 逐行对一眼（Necron 那一行应该变成指向工程里那颗 Shield Ico）
grep -n "m_Mesh:" "d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle Necron Variant.prefab" | sed -n '1,40p'
```

   期望出现：`m_Mesh: {fileID: 4300000, guid: 4a834e3b56e043d4b9ef315f25f0ecf7, type: 2}`
   （guid = `Assets/WarpforgeVFX/Meshes/Shield Ico.asset.meta` 那条）。
   同理 `Buff_Sororitas_quick_fire` 的 `m_Sprite` 应指到工程精灵（guid 非 0）。

```bash
# ③ 改了哪些、改了多少
cd d:/4/Unity && git diff --numstat -- MyGame/Assets/WarpforgeVFX/Prefabs
```

   ⚠️ 整件重写 ⇒ 行数会很大（正常）。**要核的是「有没有别的字段跟着变」**：
   抽一件 `git diff` 里 `^[-+]` 的行，按字段名归类（`m_Mesh` / `m_Sprite` / `m_Texture` 之外的东西
   应为 0 行 —— 若出现 `m_Controller` / `m_Animation` / `material` 之类的变化，回传我）。

**收尾（按 `RunListed` 头注的规矩）**：

```bash
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectLibraryBuilder.Run -logFile "d:/4/_tmp_view/effectlib_w61b.log"
```

> 这 7 件是**原地覆盖**（名字/路径不变）⇒ 效果库理论上不用重建；头注写「导完必须跟一次」。
> 跑它幂等、只重写那份 ScriptableObject。若不想跑，**要在交接里写明没跑**。

### 3.2 Batch B（后续 · 同族剩下 7 件，**要做，但排在 A 验过之后**）

同一种伪引用、同样 45 条里的另外 25 条：

```
Godspear Warhead Full（19）· Vortex Explosion Massive（15）· Goff_ProperKilly（2）
EC_Target_user（1）· EC_Target_user Lucius（1）· Sword_10x_EC（1）· Will Of Gork（1）
```

把它们逐行加进 `ListedPrefabs`（一处），再用 `WFEFFECT_ONLY="<这 7 个名字>"` 跑一次 3.1 那条命令
（⚠️ **必须先加进表** —— `WFEFFECT_ONLY` 只是过滤器，选不出表里没有的名字）。
⚠️ 这 7 件**都是整件重导**、台账当前全 Z ⇒ **必须配「重导前后各扫一趟」**（见 §4.1 的协议）才能说「没碰坏别的」。
⛔ 不要和 Batch A 混在一趟里做（混了就分不清是谁引起的）。

⚠️ **`Raging Storm` / `Tidewall Gunrig` / `BulletTrail_Single` 本轮不重导**（W61 已定：假阳性 / 21:54 刚重导过）。

---

## 4. E 组可判清单（不是结论）

### 4.1 通用协议 —— ⚠️ 先读，`EffectSweepBatch` 会**覆盖**基线文件

`EffectSweepBatch.Run` 写的是**固定路径、整份重写**（`EffectSweepBatch.cs:405,410,484`）：

```
d:/4/Unity/资料/比对基线/sweep_orig.tsv
d:/4/Unity/资料/比对基线/sweep_exp.tsv
d:/4/Unity/资料/比对基线/sweep_frames.tsv     ← orig 那侧写（exp 那侧读）
```

⇒ **单效果两跑会把 960 目标的全量基线冲成 1 个目标**。**必须**：

```bash
mkdir -p d:/4/_tmp_view/bak_w61b
cp d:/4/Unity/资料/比对基线/sweep_orig.tsv    d:/4/_tmp_view/bak_w61b/
cp d:/4/Unity/资料/比对基线/sweep_exp.tsv     d:/4/_tmp_view/bak_w61b/
cp d:/4/Unity/资料/比对基线/sweep_frames.tsv  d:/4/_tmp_view/bak_w61b/
```

🔴 **跑单效果期间 ⛔ 不要跑 `工具/analyze_sweep.py`** —— 它读那两个 tsv、**直接覆盖写 `特效还原台账.tsv`**
（`analyze_sweep.py:213`，没有输出路径参数）⇒ 会得到一份只有一两行的台账。

**目标文件**（`EffectSweepBatch.cs:94`）：`d:/4/_tmp_view/sweep_targets.txt`，**一行一个效果名**（逐字）。

**单变量原则**：`WFBIND_*` 只影响**导出侧**（`WarpforgeEffectBinder`），原版侧渲染的是 bundle 里的原件。
⇒ A/B 时**保持第一次的 `sweep_orig.tsv` 不动**，只重跑 `WFSWEEP_SIDE=exp`，两次用**同一个分母**。
（`exp` 那趟要读 orig 留下的取景缓存 ⇒ 第一趟必须先是 `orig`/`both`。）

**判读口径**（与台账同一把尺子，`analyze_sweep.py:162-190`）：
取「两侧 `lit ≥ 60`」的时刻，算逐时点 `sum_exp / sum_orig`，**取中位**；
`0.7 ≤ 中位 ≤ 1.4` ⇒ Z，否则 E。**判定带宽不是验收数字**，要看**逐时点比值**。

**噪声底**（同一配置两次跑之间的差，判「真差异」的门槛）：
全池 733 个可比效果 `|Δ|ln||` 中位 / 90 分位 = 0 / 0.09、**最大 0.412**（即 `尺子不稳定名单.tsv` 第 1 名就是 Raging Storm 0.4120）。
⇒ 单条效果要判「真差异」，**必须先用两趟完全相同的跑把这条自己的噪声底量出来**。

### 4.2 `EnvironmentalCondition Sororitas Raging Storm`（台账 E · 0.65 偏暗 · 不稳定名单第 1 名 0.4120）

**现状数字**（`sweep_orig.tsv:4018-4025` × `sweep_exp.tsv`，22:00 那趟；`sum` 列，下面是我手算的）：

| t | orig sum | exp sum | 比值 |
|---|---|---|---|
| 0.15 | 1358 | 1321 | 0.973 |
| 0.30 | 1417 | 1378 | 0.972 |
| 0.50 | 790 | 750 | 0.949 |
| 0.75 | 86 | 47 | **0.547** |
| 1.00 | 91 | 52 | **0.571** |
| 1.50 | 98 | 59 | **0.602** |
| 2.00 | 104 | 66 | **0.635** |
| 3.00 | 113 | 76 | **0.673** |

中位 = (0.635+0.673)/2 = **0.654**（台账 0.65 · |ln| 0.353 对得上）。
**症状很干净**：0.15–0.50s 对得上（0.95–0.97），**0.75s 之后整段偏暗**，而且**一路在往回收**。

**步骤**：

```bash
# 准备（只这一行）
printf 'EnvironmentalCondition Sororitas Raging Storm\n' > d:/4/_tmp_view/sweep_targets.txt

# ① 基线（同一进程两侧）
unset ELECTRON_RUN_AS_NODE && WFSWEEP_SIDE=both "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run \
  -logFile "d:/4/_tmp_view/sweep_w61b_rs_00_base.log"

# ② 复跑一遍（量这条自己的噪声底）
unset ELECTRON_RUN_AS_NODE && WFSWEEP_SIDE=both "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run \
  -logFile "d:/4/_tmp_view/sweep_w61b_rs_01_repeat.log"

# ③ A：只翻 `Alpha Mask One Layer`（尾部那 4× Smoke 的主材质）
unset ELECTRON_RUN_AS_NODE && WFSWEEP_SIDE=exp WFBIND_FORCE_BUILTIN="Alpha Mask One Layer" \
  "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_w61b_rs_02_alphaMask.log"

# ④ B：只翻 `Extra Color`
unset ELECTRON_RUN_AS_NODE && WFSWEEP_SIDE=exp WFBIND_FORCE_BUILTIN="Extra Color" \
  "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_w61b_rs_03_extraColor.log"
```

> ③④ 之间**要把 `sweep_exp.tsv` 的 8 行抄下来**（会被下一次覆盖），或者每跑完一次立刻另存一份。
> ② 也会覆盖 `sweep_exp.tsv` —— 所以每趟跑完**立刻** `cp sweep_exp.tsv sweep_w61b_rs_0N_exp.tsv`。

**每趟怎么算**（只读，不写台账）：

```bash
python -I -X utf8 - <<'PY'
import io,statistics
def load(p):
    d={}
    for i,l in enumerate(io.open(p,encoding='utf-8-sig')):
        if i==0: continue
        f=l.rstrip('\n').split('\t')
        if len(f)>=4: d[(f[0],f[1])]=(int(f[2]),int(f[3]))
    return d
o=load(r'd:/4/Unity/资料/比对基线/sweep_orig.tsv')
e=load(r'd:/4/Unity/资料/比对基线/sweep_exp.tsv')     # ← 换成分趟另存的那份
tgt='EnvironmentalCondition Sororitas Raging Storm'
rs=[]
for t in ('0.15','0.30','0.50','0.75','1.00','1.50','2.00','3.00'):
    a,b=o.get((tgt,t)),e.get((tgt,t))
    if not a or not b: continue
    if a[0]>=60 and b[0]>=60 and a[1]>0:
        r=b[1]/a[1]; rs.append(r); print(f't={t:4s} ratio={r:.3f}  (orig sum={a[1]} exp sum={b[1]})')
print('median =',round(statistics.median(rs),3),' |ln| =',round(abs(__import__("math").log(statistics.median(rs))),3))
PY
```

**判据（写成「什么结果说明什么」，跑完直接对号）**：

| 结果 | 说明 | 下一步 |
|---|---|---|
| ①② 两趟的逐时点比值 `|Δln| ≤ 0.02` | 这条**稳**（0.412 是历史两跑，不是当前） | 基线可信，直接看 ③④ |
| ①② 差 > 0.05（尤其 0.75s 后那段） | 这条在**当前条件下**仍在漂 | **先别改任何东西**，把两趟 tsv 都回传 —— 先把漂移源（跑序/前驱效果）钉死 |
| ③ 的尾部比值**大幅下降**（如 0.55→0.03，与 1021 的 0.349→0.031 同向） | **自建那份 `Alpha Mask One Layer` 更暗** ⇒ 白名单走原件是对的，**偏暗不是这个 shader 的锅** | 转去 ④ 与 §4.4 的「材质槽分」 |
| ③ 的尾部比值**明显回升**（→ ≥0.9） | 原版那一份在我们这边**没生效**（取件/关键字/队列哪一环） | 查 `WarpforgeShaderMap` 白名单取件日志（`ShaderResolveProbe`） |
| ④ 的比值基本不变 | `Extra Color` 不是主因 | 见下 |

**还没有尺子的一条**：0.75s 之后由 4× `Smoke Traily Background` 主导（length 5.0 / looping / prewarm），
**但「谁在 0.75s 后暗」目前只有整体比值**，没有逐发射器证据。要逐发射器 → `EffectIso`
（⚠️ 它 `SimTime` 写死 **1.2f**，`EffectIso.cs:73`；`Targets` 也是硬编码表 `EffectIso.cs:41`，
外加要把 `EnvironmentalCondition Sororitas Raging Storm` 加进去）。这不是我能改的文件，列为建议。

### 4.3 `ShieldTraitEffect_Idle Necron Variant`（台账 E · 1.42 偏亮 · TAU 1.37 只差 3.9%）

**现状数字**：

| t | orig sum | exp sum | 比值 |
|---|---|---|---|
| 0.15 | 257 | 304 | 1.183 |
| 0.30 | 291 | 339 | 1.165 |
| 0.50 | 198 | 255 | 1.288 |
| 0.75 | 105 | 162 | **1.543** |
| 1.00 | 112 | 162 | 1.446 |
| 1.50 | 127 | 178 | 1.402 |
| 2.00 | 75 | 133 | **1.773** |
| 3.00 | 67 | 127 | **1.896** |

中位 = 1.424（台账 1.42 对得上）。**症状**：早期只高 17%（和 1021 那趟 **Z 1.183** 一致），
**0.75s 起跳到 1.5–1.9** —— 说明「同一个 prefab 两跑一个 Z 一个 E」很可能就发生在这一段。

**步骤**（顺序不能改：先量漂移，再做 A/B，最后才谈重导）：

```bash
printf 'ShieldTraitEffect_Idle Necron Variant\n' > d:/4/_tmp_view/sweep_targets.txt

# ① 基线（both）
unset ELECTRON_RUN_AS_NODE && WFSWEEP_SIDE=both "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run \
  -logFile "d:/4/_tmp_view/sweep_w61b_nc_00_base.log"      # 跑完立刻 cp sweep_{orig,exp}.tsv 另存

# ② 同配置再跑一遍（噪声底 + 判定会不会自己从 E 变 Z）
… 同上，log = …_01_repeat.log

# ③ A/B 四趟（都只跑 exp —— 分母沿用 ①/② 的 orig）
WFSWEEP_SIDE=exp WFBIND_NO_PROP_FILTER=1        …  _02_nopropfilter.log
WFSWEEP_SIDE=exp WFBIND_NOQUEUE=1               …  _03_noqueue.log
WFSWEEP_SIDE=exp WFBIND_FORCE_BUILTIN=1         …  _04_builtin.log
WFSWEEP_SIDE=exp WFSWEEP_GLOBALS=1              …  _05_globals.log   # 打每个目标开跑前的全局 shader 关键字
```

**判据**：

| 结果 | 说明 |
|---|---|
| ①② 两次**自己**就从 E 变 Z（或反过来） | 这条在噪声里 —— 按 W61 §3「`|ln| ≲ 0.6` 分不出真假」，**别改任何东西**，记为「尺子不确定」 |
| ② 与 ① 逐时点 `|Δln| ≤ 0.02`，且 2–3s 那段仍 ~1.8 | 是**真差异**，往下做 |
| ③ `NO_PROP_FILTER` 让 2–3s 比值明显回落 | 主因 = **孤儿属性过滤闸**（`WarpforgeEffectBinder.cs:342`），不是资产 |
| ③ `NOQUEUE` 让比值变 | 主因 = **renderQueue 覆盖**（`:451`）导致的前后遮挡/叠加次序 |
| ③ `FORCE_BUILTIN=1` 让比值变 | 主因 = 白名单里某个 shader 走原件 vs 自建 |
| 四趟都**不动** 2–3s 那一段 | 排除材质/绑定，转「发射器层面」：`EffectIso` 逐槽（`iso_stats_exp.tsv` 的 `contrib`），
或先核 `Lightning Burst` 之外还有谁活到 2–3s |

**与 ShapeModule 的关系（重要，别搞混）**：本轮重导会改 `Lightning Burst` 的 `ShapeModule.m_Mesh`，
但那是 **`type: 18`（Rectangle）** ⇒ **不影响取样**（§2.2）⇒ **预期亮度不变**。
⇒ 重导**前后**各跑一趟 ①② 是**必须的**：如果重导后 2–3s 真变了，那也**不是** shape mesh 的功劳，
而是这次整件重导顺手改了别的（材质/贴图/网格变体路径）—— 那时要比 `git diff` 才知道是谁。

### 4.4 判读表之外的两条硬口径（照抄 W61，别推翻）

- **不重导 Raging Storm**（21:54 刚重导过，重导会把「orig 侧尾部 64→86」这条已知变量再动一次）。
- ⛔ **不撤 `UseOriginal` 白名单**（W60 已证 78 条退步）；只做**目标化** A/B（`WFBIND_FORCE_BUILTIN="名字"`）。
- **C / D 两条（Tidewall Gunrig / BulletTrail_Single）判 Z，不重导**（`LIT_MIN=60` 稀疏阈值假阳性，两侧 `sum` 逐位相同）。

---

## 5. 没查清 / 未验证（如实写）

1. 🔴 **`ShapeModule.mesh` getter / setter 在非 Mesh 形状下会不会被 Unity 挡住 —— 没实测过（不能跑 Unity）。**
   代码里两条路都走了（getter → 原生序列化字段；setter → `SerializedObject` 兜底 + 序列化字段复核），
   但**哪条真正生效只有跑一次 3.1 才知道**。判据 = 日志里有没有 `**写不进去**`。
2. 🔴 **`SerializedObject` 的 `ShapeModule.m_Mesh` 属性路径没在工程里找到先例**（本仓没有第二处用 PS 模块的序列化路径）。
   找不到时 `FindProperty` 返回 null ⇒ 会打 `兜底路也不通：…找不到 ShapeModule.m_Mesh`。**这条告警本身就是判据**。
3. **原版 `Shield Ico` 网格的顶点数**只核到 OBJ 的 `v` 行 = **187**（W61 给的是 187，对得上）；
   没有核「落盘 `.asset` 的顶点极值」（`ImportMesh` 的深拷贝路没在这颗上回读过 —— 它是 `simple` 网格、
   走 `DeepCopyMesh`，按既有结论应当是健康的，但**本轮没验**）。
4. **Batch A 重导会不会让 prefab 里别的字段跟着变** —— 未知，要靠 §3.1 的 ③ 那步逐行归类。
5. **Raging Storm 尾部那 4× `Smoke Traily Background` 到底是谁暗** —— 只有整体比值，没有逐发射器证据
   （`EffectIso.SimTime` 写死 1.2s，量不到 0.75s 之后）。
6. **Necron 2–3s 那段到底哪个发射器在贡献** —— 未查（W61 也没查；§4.3 最后一行给了入口）。

---

## 6. 改动清单

| 文件 | 改了什么 |
|---|---|
| `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs` | ① `Export()` 新增 `ShapeModule` 资产引用段（`m_Mesh`/`m_Sprite`/`m_Texture`，`:1912-2055`）<br>② 新增 `BuiltinMeshNames` / `IsUnityBuiltinMesh` / `ForceShapeRef` / `SerializedShapeRef<T>`（`:4303-4358`）<br>③ `ListedPrefabs` 新增 7 件（`:861-878`）<br>④ 新增 `ListedTargets()` + `WFEFFECT_ONLY` 过滤器（`:883-905`），`RunListed()` 三处改用它（`:963` / `:969` / `:1008`） |
| `d:/4/Unity/资料/普查产出_1021/W61b_特效_ShapeModule导出.md` | 本报告 |

**没碰**：`CardPresentation/**` · `Assets/RuleEngine/**` · `CLAUDE.md` · `项目任务.md` · `任务线/**` · `d:/2/**` · git。
**没跑**：Unity（任何 `-executeMethod`）、任何自检宿主。

**typecheck**（白名单内唯一写盘动作，秒级那一档）：

```
TMPDIR=/tmp/wf_c3 bash d:/4/Unity/工具/typecheck.sh
运行时错误数: 0
编辑器错误数: 0
```

> 注：中途几次跑到过别的数（运行时 1/5 条 · 编辑器一串 CS0246 或 `CS0117 VfxMap 未包含 SummonCircleOf`）——
> **全部集中在 `Assets/RuleEngine/Core/RuleCore.cs`、`CardPresentation/**`（含 `VfxMap.cs` / `BattleScene.cs`）**，
> 都是**别的代理当时正在写**的文件，其中 `EffectExporter.cs:1329 WarpforgeVatDriver` 那条是
> 「运行时程序集没编过 ⇒ 编辑器程序集看不见运行时类型」的**级联**，不是本文件的问题。
> 它们落盘后重跑 = **0/0**（最后一次 = 0/0）。

---

## 7. 顺手发现（其中两条**已顺手修**，其余只报不改）

1. ✅ **已修 · `ShapeModule.m_Sprite` 同一种伪引用（16 条 / 2 件）** —— `Buff_Sororitas_quick_fire` 7 + `_intense` 9。
   ⚠️ 其中 **2 条在 `type 19`（Sprite 形状）上 —— 那条是活的**（会改取样位置），另外 14 条在 type 4/10（死字段）。
   台账两件当前 Z（1.00 / 0.95）⇒ 修了不预期改亮度，但伪引用该清。
2. ✅ **已修 · `ShapeModule.m_Texture`（2 条 / 1 件）** —— 同两件，`type 19`。
3. ❗ **只报不改 · `工具/check_prefab_refs.py` 的默认字段集看不到这三列**（默认 `m_Script,m_Sprite,m_fontAsset,m_sharedMaterial`）。
   建议把 **`m_Mesh` / `m_Texture`** 加进 `DEFAULT_FIELDS`（`工具` 不在我白名单）。
   现状：不加 `--fields` 扫这 7 件是**全绿**的，但 `m_Mesh` 其实坏着 —— 属于「工具给了假绿」。
4. ❗ **只报不改 · W61 §2 的推测要就地订正**：「`ShapeModule.m_Mesh` 只影响 0.15–0.5s」**不成立** ——
   它是死字段（§2.2），对亮度**零影响**。W61 原文在 `资料/普查产出_1021/W61_特效线_新EDC四条.md:10,28`，
   正本不是我这份文件，**请主对话改**（铁律 5）。
5. ℹ️ `Godspear Warhead Full` / `Vortex Explosion Massive` 两件占了 45 条里的 34 条（19 + 15），
   而且**同一颗网格（pathID `1046982395047586700`）被 34 个 PS 共用** ⇒ 重导一次能一次清掉这 34 条。
6. ℹ️ `Will Of Gork.prefab` 有条坏引用挂在 `type 5`（Box）上；`EC_Target_user*` 两条挂在 `type 12`（SingleSidedEdge）上 ——
   都印证「改形状类型不清 m_Mesh」这个成因。
