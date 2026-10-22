# W64 · B04 —— `ToBoardFX` / `Spawn Particles` / `Trail` 到底是什么、在哪、能不能本地复刻

> 只读侦查。**结论先给**：⑥ 未闭合的那个问题**已经闭合** ——
> `ToBoardFX` **不是** `normalSummon`；`ToBoardFX` 是**空锚点**（`CardScript.toBoardVFXAnchor`）。
> **`ToBoardFX/Spawn Particles` 才是 `normalSummon`** —— 它是 `CardSetup` 在运行时
> **把 `normalSummon` 的 prefab 实例挂到锚点下、并改名成 `"Spawn Particles"`** 的产物。
> `Trail` 是这个 prefab **自带的子节点**（一个 ParticleSystem）。
> 三件资产**本地全都有、我们也已经导进工程了**；缺的是**运行时的挂载 + 改名 + 时间轴**，
> 以及一个我们**没导的脚本 `ParticleEmitOnEnable`**。

---

## 一、三条曲线在 clip 里的绑定形态（第 1 问）

**判据**：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/AnimationClip_-3614292623624764332.json`
（`m_Name = "Card Hand To Board"` · `m_Legacy = true` · `m_SampleRate = 60` · 全长 0.9167 s · 17 条曲线 · **`m_PPtrCurves` 为空** · 1 个 AnimationEvent @0.55 `CardHandToBoardAnimationFinished`）

三条全部落在 **`m_FloatCurves` + `attribute = m_IsActive` + `classID = 1`（GameObject）** ——
即 **GameObject 激活开关曲线**，**不是** PPtr 曲线、**也不是** ParticleSystem 参数曲线：

| path | attribute | classID | 关键帧（原值，`inSlope/outSlope` 全 `inf` = 恒量段/阶跃） |
|---|---|---|---|
| `ToBoardFX` | `m_IsActive` | 1 | t=0.0000 → **1.0**（只有这一个键 ⇒ 全程常开） |
| `ToBoardFX/Spawn Particles` | `m_IsActive` | 1 | t=0.0000 → **0.0** · t=**0.0167** → **1.0** |
| `ToBoardFX/Spawn Particles/Trail` | `m_IsActive` | 1 | t=0.0000 → **0.0** · t=**0.5500** → **1.0** |

**⇒ 判定**：这三条是**层级节点开关**，路径是**相对动画根**的相对路径。
`Trail` 在 **0.55** 才被打开 —— 与同一个 clip 的 AnimationEvent（0.55，`CardHandToBoardAnimationFinished`）**同一时刻**。

> 顺带（同一张表里其它 `m_IsActive`）：`Board Elements` 0 → 1 @0.1667 · `2DCard` 1 → 0 @0.7。

---

## 二、`ToBoardFX` 在 prefab 里的真实身份（第 2 问）

**判据**：`d:/2/tools/assetripper_out/ExportedProject/Assets/CardPrefab.prefab`
（AssetRipper 全量导出，**层级完整**；`d:/2/新解包资源/.../bundle_battleprefabs_vfxandmisc_assets_all` 那份 JSON 抽取
**丢了 `2DCard` 的两个兄弟 Transform**，不可用于判子树 —— 见 §七「坑」）

### 2·1 `CardPrefab` 根的直接子节点（4 个）

| 序 | 名字 | 组件 | 备注 |
|---|---|---|---|
| 1 | `Board Elements` | Transform | 3D 体那一套 |
| 2 | `InBattleId` | RectTransform + MB + MeshRenderer | `m_IsActive = 0` |
| 3 | `2DCard` | RectTransform + `class61` + 4×MB + CanvasRenderer + **CanvasGroup** + 4×MB + Canvas | 2D 卡面那一套 |
| 4 | **`ToBoardFX`** | **Transform（仅此一个）** | **layer 5 · `m_IsActive = 1` · localPos = (0.0033340454, 0.007981658, 0.046000004) · `m_Children: []`** |

### 2·2 全树里名字含 `FX` / `Particle` / `Trail` / `Spawn` / `ToBoard` 的节点（`CardPrefab` 子树内）

| 名字 | 组件 | 说明 |
|---|---|---|
| `ToBoardFX` | Transform | **空锚点**，无渲染器、无 ParticleSystem |
| `CanActParticles` | Transform + ParticleSystem + ParticleSystemRenderer | 「可行动」提示，与登场无关 |
| `EffectAnchor`（×2） | Transform(+MB) / Transform+SortingGroup | 技能特效锚点 |
| ~~`Spawn Particles`~~ | —— | **子树里不存在**（见 §三） |
| ~~`Trail`~~ | —— | **子树里不存在**（见 §三） |

### 2·3 它不是渲染节点，而是脚本字段

`CardPrefab` 根上的 `CardScript`（`m_Script → MonoScript_-2492490160465947507`，类名 `CardScript`）
JSON = `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_1744609728290659264.json`，
其中三个**连续**的 PPtr：

```
"normalSummon":      { "m_FileID": 0, "m_PathID": -1225021812410638111 }
"legendarySummon":   { "m_FileID": 0, "m_PathID":  2726653606669894234 }
"toBoardVFXAnchor":  { "m_FileID": 0, "m_PathID":  1809300772220869568 }   ← = ToBoardFX 的 Transform
```

🔴 **`toBoardVFXAnchor` 指向的就是 `ToBoardFX` 的 Transform（PathID 1809300772220869568）** ——
这是「`ToBoardFX` 是谁」的**直接判据**。

---

## 三、全库按名字搜（第 3 问）—— 搜过的包与词

**搜过的范围**

| 范围 | 做法 |
|---|---|
| `d:/2/新解包资源/assets_full/*/GameObject/*.json`（**全部 84 个 bundle**） | `grep -rl '"m_Name": "<词>"'` |
| `d:/2/新解包资源/assets_full` 下**任意** `.json`（含 clip / MB / AssetBundle） | `grep -rl "<词>"` |
| `d:/2/tools/assetripper_out/ExportedProject/Assets`（1,314 prefab + 场景） | `grep -rl "m_Name: <词>"` |
| `d:/4/Unity/MyGame/Assets`（我们工程） | Grep |
| `d:/4/Unity/资料`（全部已有普查报告） | Grep |

**搜过的词**：`ToBoardFX` · `ToBoard FX` · `toboard`(不分大小写) · `Spawn Particles` · `SpawnParticles` ·
`spawn`(不分大小写) · `Trail` / `trail`(不分大小写) · `^…FX$`(不分大小写) · `Particle`

**命中**

| 词 | 命中 | 结论 |
|---|---|---|
| `ToBoardFX` | **仅 1 个** GameObject：`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/ToBoardFX.json` + 1 处 clip 引用 | 唯一的节点，`CardPrefab` 专用 |
| `Spawn Particles` | **0 个 GameObject**；**全库唯一命中 = clip 本身** | ⚠️ 预置体里**根本没有这个节点** |
| `Trail` | 94 个不同名的 GameObject（`BulletTrail*` / `Smoke Trail*` / `Lightning Trail*` …），**`CardPrefab` 子树内 0 个** | ⚠️ 卡上也没有 |
| `FX` 结尾 | **0 个** | —— |

**⇒ 光靠静态资源定位不到 `Spawn Particles` / `Trail`** —— 它们**不在卡的预制体里**。
必须先回答「谁在运行时造了它」，否则会误判成「原版预制体自带、我们没导」。

---

## 四、运行时创建链 —— `Spawn Particles` 是**改名**出来的（关键发现）

**判据（第一权威，反编译方法体）**：`d:/2/tools/decomp_full/CardScript__CardSetup.c:284-336`

```
284:  uVar1 = *(undefined8 *)(param_1 + 0x318);            // （别的字段，略）
286:    lVar13 = *(longlong *)(param_1 + 0x180);           // ← legendarySummon
290:    lVar13 = *(longlong *)(param_1 + 0x178);           // ← normalSummon
        （286/290 的分叉条件 = HasCurrentTrait(0x4b0) / cardRarity == 4，即「传说档」）
312:  lVar13 = BattleManager__CreateAnimFromAnimFxController(
                    manager, /*uVar1 = lVar13 + 0x18 = CardAnim.animInfo*/,
                    toBoardVFXAnchor.transform, ×3, this, 0,0,0);
317:  UnityEngine_Transform__set_parent(lVar17, *(undefined8 *)(param_1 + 0x188), 0);   // ← 挂到 toBoardVFXAnchor
328:  UnityEngine_Transform__set_localPosition(lVar17, &local_48, 0);                   // ← 保留 prefab 自身 localPos
330:  UnityEngine_Object__set_name(lVar17, DAT_18425a8b0, 0);                           // ★ 改名
333:  UnityEngine_GameObject__SetActive(lVar13, 0, 0);                                  // ★ 先关掉
```

**字段偏移自证**：JSON 里 `normalSummon` / `legendarySummon` / `toBoardVFXAnchor` 是**连续三个 8 字节 PPtr**
⇒ 反编译里 `0x178` / `0x180` / `0x188` **正好是它们**。并且 `0x188` 在全文只被 `CardSetup` 用、
且被拿去调 `UnityEngine_Component__get_transform` ⇒ **它确实是个 Component（Transform）**。

**`DAT_18425a8b0` 是什么字符串 —— 已经是现成产物里的逐字节答案**：
`d:/4/Unity/资料/普查产出_1017/_b1018_scratch/g2b_scratch/va_map.txt:6254`

```
18425a8b0 'Spawn Particles'
```

（同表相邻项：`18425a8a8 'Cs'` · `18425a8b8 'levelUp'` —— 是 VA → 字符串字面量映射表。）

### 4·1 于是运行时层级是

```
CardPrefab
├── Board Elements
├── InBattleId
├── 2DCard
└── ToBoardFX                                   ← 锚点（空节点，预制体自带）
    └── Spawn Particles                         ← ★ 运行时 Instantiate 出来的，被改名为 "Spawn Particles"
        ├── Energy                              ← 自带子节点
        ├── Energy 2                            ← 自带子节点
        └── Trail                               ← 自带子节点（clip 在 0.55 打开它）
```

**⇒ clip 里三条 `m_IsActive` 曲线的 path 全部能对上**，一条不多一条不少。
**⇒ 未闭合问题⑥ 闭合：`ToBoardFX/Spawn Particles` = `normalSummon`（传说档 = `legendarySummon`）的运行时实例。**

---

## 五、`normalSummon` / `legendarySummon` 到底是什么（第 4 问：逐字段对照）

### 5·1 它们不是 prefab，是 **`CardAnim` 这个 ScriptableObject**

| 项 | `normalSummon` | `legendarySummon` |
|---|---|---|
| 字段 `m_PathID`（`CardScript` 上） | `-1225021812410638111` | `2726653606669894234` |
| 资产文件 | `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/Invoke Minion Card Fade Default.json` | `…/Invoke Minion Card Fade Legendary.json` |
| `m_GameObject` | **0** ⇒ 是 **SO/资产**，不是场景里的组件 | 0 |
| `m_Script` | `MonoScript_8650191106899952245` = **`CardAnim`** | 同 |
| `m_Name` | `Invoke Minion Card Fade Default` | `Invoke Minion Card Fade Legendary` |
| `animInfo.animAdressable.m_AssetGUID` | `5e977521119914de5b08bba7f4d2eed6` | `1f109ed2b02e54433853eb7b3bf62ead` |
| **该 GUID 在本 bundle 的 `m_Container` 里指向** | `GameObject/Invoke Minion Card Fade Default.json`（`m_PathID 5105821803623563093`） | `GameObject/Invoke Minion Card Fade Legendary.json`（`m_PathID 7256851925915064502`） |
| `animInfo` 其余参数（两件都一样） | `startPosOption=0 · endPosOption=0 · pointTowardsOption=0 · vfxDelayTime=0 · startDelay=0 · timeAtStartPos=0 · timeMoving=0 · timeAtEndPos=0 · shouldMoveVFX=0 · orientToVelocity=0 · applyMovementOffsets=0` | 同 |
| 音效 | `battleCardSounds.invokeCUE`（传说档另有 `invokeCUELegendary`） | 同槽 |

> `normalSummon` → 具体文件这一步是**按字段名 + `animInfo` GUID 链**判的；
> 另有两条旁证同向：（a）我们已有的派生表 `d:/4/Unity/数据/游戏数据/card_vfx_by_card.json`
> `generic.byEvent.deploy` 第一条 `src = "CardPrefab.normalSummon"` / `prefab = "Invoke Minion Card Fade Default"` / `conf = exact`；
> （b）`Core/VfxMap.cs:131` 的注释同口径。
> ⚠️ `m_PathID → 那个 SO 文件` 这一步**没有逐字节解**（JSON 抽取的 SO 文件不带 PathID，见 §七），**如实记在 §十一**。

### 5·2 逐字段对照：`ToBoardFX` vs `normalSummon`

| 维度 | `ToBoardFX` | `normalSummon` | 同一件？ |
|---|---|---|---|
| 对象类型 | **Transform**（GameObject `ToBoardFX` 的唯一组件） | **ScriptableObject `CardAnim`**（`m_GameObject = 0`） | ❌ |
| 在 `CardScript` 上的字段 | `toBoardVFXAnchor` | `normalSummon` | ❌ 两个不同字段 |
| 有没有粒子 | **无**（无 ParticleSystem / 无 Renderer） | 指向的 prefab 有 **3 个 ParticleSystem** | ❌ |
| 生命周期 | **预制体静态存在**（`m_IsActive = 1`，常开） | 运行时 `Instantiate` → 挂到前者下 → 改名 → `SetActive(false)` | ❌ |
| clip 里的角色 | **父路径第一段**（`ToBoardFX` 恒 1） | 提供 `Spawn Particles` 那一层的内容 | ❌ |

**⇒ 结论矩阵**

| 断言 | 判定 | 判据 |
|---|---|---|
| `ToBoardFX` == `normalSummon` | **❌ 不同件** | `CardScript` 两个字段 · 类型不同（Transform vs CardAnim SO）· clip 里 `ToBoardFX` 是长开的父节点 |
| `ToBoardFX/Spawn Particles` == `normalSummon`（的实例） | **✅ 同一件**（改名） | `CardSetup.c:330` + `va_map.txt:6254` + clip path 只有这一个解释 |
| `Trail` 是 `TrailRenderer` 吗 | **❌ 不是** | `Trail` 的组件 = Transform + **ParticleSystem** + ParticleSystemRenderer + **`ParticleEmitOnEnable`** |

### 5·3 `normalSummon` 那件 prefab 的完整树 + 粒子参数

`Invoke Minion Card Fade Default`（`GameObject/Invoke Minion Card Fade Default.json`，`m_IsActive = 0`，layer 0，localPos −0.0038/−0.027/−0.0141）
根组件 = Transform + ParticleSystem + ParticleSystemRenderer + **`AnimFXController`**（`MonoScript_5369004874005584895`）

| 子节点 | 组件 | `lengthInSec` | `looping` | `playOnAwake` | `stopAction` | `rateOverTime` | `startSize` | `startLifetime` | `maxParticles` | 材质 |
|---|---|---|---|---|---|---|---|---|---|---|
| `Energy` | Transform+PS+PSR | 1.0 | 0 | 1 | 0 (None) | **6.53** | **0.70**（minMax=RandomTwoConstants） | **0.25** | 1000 | `m_FileID 4`（外部）`/ -5453729611301113537` |
| `Energy 2` | Transform+PS+PSR | 0.5 | 0 | 1 | 0 (None) | **6.53** | **0.41** | **0.25** | 1000 | `Material_-7985079189713184936` |
| `Trail` | Transform+PS+PSR+**`ParticleEmitOnEnable`** | **2.0** | 0 | 1 | **1 (Disable)** | **0.0**（靠脚本触发） | **0.98** | **0.40** | 1000 | `Material_-3784157391489366364` |

- `Energy` / `Energy 2` 的 `startColor` = `minMaxState: 2`（双色随机），min ≈ (0.461, 0.239, 0.877) 紫 / max ≈ (0.243, 0.744, 0.972) 蓝。
  `Trail` 的 `startColor` = `minMaxState: 0`（单色）+ `maxGradient` 键 (1,1,1,1)→(0,0.560,1,1)。
- **`Trail` 的 `rateOverTime = 0`** ⇒ 它自己不会持续出粒子；**全靠 `ParticleEmitOnEnable`**：
  `ParticleEmitOnEnable__OnEnable.c` = `if (!ps.isPlaying) ps.Play();`
  ⇒ 这正是 clip 在 0.55 把它 `SetActive(true)` 会产生效果的原因；而 `stopAction = 1 (Disable)`
  意味着**播完 2 s 它自己会把 GameObject 关掉**，所以下一次登场必须由动画再打开。

---

## 六、我们工程的现状（第 6 问）

| 件 | 我们有没有 | 证据 |
|---|---|---|
| `CardPrefab` 里的 `ToBoardFX` 节点 | ✅ **有**（根的直接子件，`m_Children: []`，localPos 与原版逐位相同） | `MyGame/Assets/WarpforgeVFX/Prefabs/CardPrefab.prefab:10634`（GameObject）/ `:10647-10655`（Transform，`m_Father = 9201524896398680446` = 根） |
| `Card Hand To Board` clip | ✅ **有**（15,292 B、含三条 `ToBoardFX*` path） | `MyGame/Assets/WarpforgeVFX/Animations/Card Hand To Board.anim:160 / :190 / :280`；`.meta` guid `103da2c7d6fa2214b86c6c1db6d3ebc9` |
| `CardPrefab` 根上的 `Animation` 组件 + 该 clip | ✅ **有**（`m_PlayAutomatically: 0`） | `CardPrefab.prefab:3301-3316`（`!u!111 &6043378366376651393`） |
| `Invoke Minion Card Fade Default` prefab | ✅ **有**，且 **`Energy` / `Energy 2` / `Trail` 三个子节点齐全**，`Trail` 的 `lengthInSec 2 / looping 0 / playOnAwake 1 / stopAction 1` 与原版逐项一致 | `MyGame/Assets/WarpforgeVFX/Prefabs/Invoke Minion Card Fade Default.prefab`（4 个 GameObject：根 / `Energy`:5424 / `Energy 2`:10318 / `Trail`:15212） |
| `Invoke Minion Card Fade Legendary` prefab | ✅ 有 | 同目录 |
| `AnimFXController` | ✅ 有我们自己的移植 | `MyGame/Assets/WarpforgeVFX/Runtime/AnimFXController.cs` |
| **`ParticleEmitOnEnable`** | ❌ **没有**（全工程 0 命中） | Grep `ParticleEmitOnEnable|EmitOnEnable` on `MyGame/Assets` → **No matches** |
| **「实例挂到 `ToBoardFX` 下 + 改名 `Spawn Particles` + 先 SetActive(false)」** | ❌ **没有** —— 我们走的是**根实例化**（`DeployVfxCarrier`，世界位置上播一次，**不挂父节点**） | `BattleDriver.cs:16393-16412`（`FireDeployVfxInArenaSpace` doc + 实现）· `BattleDriver.cs:17077-17087` |
| **按 clip 开 `Spawn Particles`(0.0167) / `Trail`(0.55)** | ❌ **没有** | `CardFeel.cs:1289-1352`（`HandToBoard` 的 doc 自述：「`ToBoardFX` / `Spawn Particles` / `Trail` —— 我们这套卡没有」） |

> ⚠️ 我们那份 `Invoke Minion Card Fade Default.prefab` 只有 **1 个 MonoBehaviour**：
> 根上的 `WarpforgeVFX.WarpforgeEffectBinder`（guid `d9d66f84463c8544594099fc3457f1f7`）。
> 原版那 **2 个**（根的 `AnimFXController`、`Trail` 的 `ParticleEmitOnEnable`）**都被换掉了**。
> ⇒ 即便今天把 `Trail` 的 `SetActive` 拨上去，**它的 `rateOverTime` 是 0，一发粒子都不会出**。

---

## 七、坑 / 容易误判的点（写给下一个人）

1. 🔴 **`d:/2/新解包资源/.../bundle_battleprefabs_vfxandmisc_assets_all` 的 Transform 抽取不完整**：
   `2DCard` 的 Transform（`-8834377289179948096`）与 `InBattleId` 的（`3756633904034061248`）**文件不存在**，
   尽管 `CardPrefab` 的 `m_Children` 里明明列着它们、且 `_stats.json` 写着 `fail: 0`。
   ⇒ **判层级一律用 `d:/2/tools/assetripper_out/ExportedProject/Assets/CardPrefab.prefab`**（完整），
   ⛔ 别用 assets_full 的 Transform 判「有没有这个子节点」。
2. 🔴 **`ToBoardFX` 的 `m_Children: []` 是「真没有」，不是「丢了」** ——
   AssetRipper 那份完整 prefab 也是空。它的子节点是**运行时**造的（§四）。
3. `Spawn Particles` **只在两处出现**：clip + 反编译 `CardSetup.c:330` 的改名常量（经 `va_map.txt` 解出）。
   ⛔ 别再按「预制体自带节点」这个方向找。
4. `Trail` 是 **ParticleSystem**，不是 `TrailRenderer`。全库 `TrailRenderer/` 只有 13 个文件，**都不在卡上**。

---

## 八、复刻配方（照原版，本地全都能做）

**判据齐、不需要任何远端资产。** 落地顺序：

1. **挂载**（原版 `CardSetup.c:305-333`）：卡登场时把效果库里的 `Invoke Minion Card Fade Default`
   （传说档换 `… Legendary`）实例化到 **`CardView` 的 `ToBoardFX` 节点下**，
   `localPosition` 用 prefab 自己的值，**改名为 `"Spawn Particles"`**，**`SetActive(false)`**。
   ⚠️ 这**取代不了**现在 `FireDeployVfxInArenaSpace` 的根实例化吗？—— 两者是**不同的一拍**：
   `CardSetup` 是「卡自己身上的那一层」（跟着卡、吃卡的缩放/父链），
   `_MinionPlayedIntoField` state 3 是**另一处** `Instantiate(..., 卡.position, ...)` 的根实例化。
   ⚠️ 后者那个 `AssetReference` 在 `RawCardScript + 0xb0`，**是不是 `normalSummon` 我这次没核实**（见 §十一）。
2. **时序**：`PlayInvokeAnimation`（0.3 s）→ `Animation.Play("Card Hand To Board")` 之后，
   照 clip 拨两个开关：**0.0167 把 `Spawn Particles` 置 true**、**0.55 把 `Trail` 置 true**。
   `ToBoardFX` 自身恒 true（原版就是 `m_IsActive = 1 → 1`，不用动）。
3. **补 `ParticleEmitOnEnable`**（一个 5 行的脚本：`OnEnable` 里 `if (!ps.isPlaying) ps.Play();`），
   挂在 `Trail` 上 —— **不补就等于没有**（`Trail` 的 `rateOverTime = 0`）。
4. 播完之后把 `Spawn Particles` 收掉（原版没写这一句；`Trail` 有 `stopAction = Disable` 会自关，
   `Energy` / `Energy 2` 不会自关 ⇒ **收的时机原版未查到**，见 §十一）。

---

## 九、结论（≤10 行）

1. 三条全是 **GameObject 的 `m_IsActive` 开关曲线**（classID = 1），**不是** PPtr、**不是**粒子参数曲线。
2. `ToBoardFX` = `CardScript.toBoardVFXAnchor`，**空锚点**，预制体自带、无粒子。
3. `ToBoardFX/Spawn Particles` = **`normalSummon` 的运行时实例**（`CardSetup.c:330` 改名，`va_map.txt:6254` 证字符串）。
4. `…/Trail` = 该 prefab 自带的**第三颗粒子**（ParticleSystem + `ParticleEmitOnEnable`），0.55 被打开。
5. `ToBoardFX` **≠** `normalSummon`；`ToBoardFX/Spawn Particles` **=** `normalSummon`。
6. `normalSummon` 是 `CardAnim` SO（`animInfo.animAdressable` GUID `5e9775…`）→ prefab `Invoke Minion Card Fade Default`。
7. **本地三件全都有**，我们也**都导进工程了**；缺的是挂载/改名/时序 + **`ParticleEmitOnEnable` 这个脚本**。
8. 复刻不需要远端资产。

---

## 十、搜过的包 / 词（「没有」类结论的凭据）

- **包**：`d:/2/新解包资源/assets_full` 全部 84 个 `bundle_*`（`GameObject/` 全量 15,734+ 个 JSON 扫 `m_Name`）·
  全库任意 `.json` 文本 · `d:/2/tools/assetripper_out/ExportedProject/Assets`（1,314 prefab）·
  `d:/2/tools/decomp_full`（26,282 个方法体）· `d:/2/unity_run_ref/Warpforge_Data/il2cpp_data/Metadata/global-metadata.dat`
  （二进制 grep，`Spawn Particles` 命中 1 处 @offset 679152）· `GameAssembly.dll`（`Spawn Particles` / `ToBoardFX` 均 0）
- **词**：`ToBoardFX` · `ToBoard FX` · `toboard` · `Spawn Particles` · `SpawnParticles` · `spawn` ·
  `Trail` / `trail` · `FX$` · `Particle` · `normalSummon` / `NormalSummon` / `normal_summon` ·
  `Invoke Minion` · `toBoardVFXAnchor`

---

## 十一、没查清 / 只到「强证据」的

1. ⚠️ **`normalSummon` 的 `m_PathID -1225021812410638111` → 那个 SO 文件** 这一步没做逐字节解
   （JSON 抽取的 SO 文件不带 PathID；`m_Container` 里也没有它）。现在靠 **字段名 + `animInfo` GUID 链 +
   `card_vfx_by_card.json` 的 `conf: exact`** 判。**要做实**：从 `AssetBundle_1.json` 的 `m_PreloadTable`
   或另做一份「PathID → 文件」索引即可。
2. ⚠️ **`Spawn Particles` 实例在什么时候被收掉**（原版 `CardSetup` 里只有开、没看到关；
   clip 也没给它关的曲线）。可能在 `DestroyCard` / 归还手牌里，**没查到**。
3. ⚠️ **`BattleDriver.cs:16393` 注释说 `_MinionPlayedIntoField_d__314` state 3 的 `Instantiate` 是 `normalSummon`** ——
   我读那段是 `Instantiate(<RawCardScript + 0xb0 的 AssetReference>, card.position, …)`，
   **`+0xb0` 这个字段的语义我没核实**（可能是 `swarmObject`，也可能是别的）。
   ⛔ 这条注释**尚未被这次侦查证实**，别当判据继续引用。
4. ⚠️ 没读 clip 的 `m_ClipBindingConstant`（17 条 `m_FloatCurves` 已足够判定绑定形态）。
5. ⚠️ `Energy` 的材质在 `m_FileID 4`（外部文件），**没有解析到具体名字**（我们 prefab 里是 `Wispy Smoke`，
   但那是我们导入时贴的名字，**未与原版字段逐项核**）。
   🔴 **2026-10-22 就地订正（铁律 5 · P5 现核）**：**已闭合，不是缺口**。
   - `m_FileID = externals 下标 + 1` 实证成立 ⇒ `m_FileID 4` = `externals[3]` = `CAB-6e07b2d1…`
     = **`duplicateassetisolation_assets_all.bundle`**；目标材质 = **`Glow Additive`**
     （shader `Universal Render Pipeline/Particles/Unlit`），我们 `WarpforgeVFX/Materials/Glow Additive.mat`
     已正确挂在 prefab 的 `Energy` PSR（`:10227`）上，四条材质与原版逐条吻合。
   - **错因**：上面说的「我们 prefab 里是 `Wispy Smoke`」是**看错了节点** —— 那是**根节点**的材质，
     不是 `Energy` 那一格的。⇒ 不需要补做任何东西。判据 → `P5_四未查清.md` §⑤。
