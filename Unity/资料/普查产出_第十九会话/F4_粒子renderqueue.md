# F4 · 粒子材质的**运行时 render queue** 落错（`TinyFlame` 3005 → 我们 3000）

> 2026-10-11 · **动手修**（没跑 Unity · 没跑 `typecheck.sh` · 没动 git · 没改正本 · 没碰 `d:/2`）。
> 上游证据 = `资料/普查产出_第十九会话/R2_火为什么糊.md`（候选 ① 与实验 E1）与主对话实测的那五组读数。
> 度量框沿用 R2：**1280×720 · x 995–1115 · y 420–510**。坐标 / 场号 / 粒子号一律照 R2 的约定。

---

## 一、根因链（**两层要分开**，R2 说它没区分开的那一点，本节结掉）

`matQueue = -1` **既不是「清单陈旧」，也不是「这个字段没人填」** —— 是 **resolver 的【跨包兜底路】漏带了 `m_CustomRenderQueue` 这一个字段**。
**两层各自的判据如下（逐条现核，不是推测）**：

### 层 A —— 数据层：清单 `matQueue` 为什么是 −1

| 判据 | 读数 |
|---|---|
| **不是「清单陈旧」** | ① 清单里 `matQueue` 这个**键在 578 颗粒子上全都有**（陈旧 = 键缺失，不是值 −1）；② `battlearena1_manifest.json` mtime = **10-11 20:25**（本会话当天重生成）。 |
| **不是「resolver 不填」** | 736 个**网格**子材质槽里 **416 条有值**，且与真值**0 处不符**；粒子里也有 **51 条**有值（例 `Flame03` = **3005** ✓）。**同一个解析器、同一个字段，只是材质在不在本包**。 |
| **对照实验（决定性）** | `battlearena2` 的 `Flame03` 材质在 `bundle_scenes_scenes_battlearena2/Material/Material_18.json`（**本包**）⇒ `matQueue = 3005` **正确**；`battlearena1` 的 `TinyFlame` 材质在 `bundle_battlesharedresources_assets_all/Material/Material_6863891194852548491.json`（**跨包**）⇒ `matQueue = -1`。 |
| **机制（读代码坐实）** | `d:/2/Warpforge_tools/scripts/unity_scene_to_godot.py:879` 先走 **本包** `self.MAT.get(pid)`（带全字段）；取不到再走 `:536 mat_data()` —— 而 `mat_data()` 在 `:552-554` 重建出来的 dict **只有 `m_Name` / `m_ValidKeywords` / `m_SavedProperties` / `m_Shader`**，**`m_CustomRenderQueue` 被丢掉** ⇒ `parse_mat` 的 `_cq = raw.get('m_CustomRenderQueue')` = `None` ⇒ `custom_q = -1` ⇒ 生成器 `gen_unity_arena_manifest.py:1495` 那条 `else -1`。 |
| **⚠️ R2 那句话要订正** | R2 与 `ArenaBuilder.cs` 原有注释都写「材质**本体不在本场包里 ⇒ 读不到**」。**方向对、机制错**：材质**读得到**（跨包那份 dict 里 `props` 全在），丢的是**那一个标量**。区别有实际意义 —— 「读不到」只能靠旁挂补数据；「丢了一个字段」可以**从 `_QueueOffset` 现算**，而 `_QueueOffset` 正好在 `m_SavedProperties.m_Floats` 里、跨包带得住。 |
| **⚠️ 生成器这一层改不了** | `gen_unity_arena_manifest.py` / `unity_scene_to_godot.py` 真身都在 `d:/2`（白名单外）⇒ **根因挂着**，落在本仓的只能是「消费侧从 `_QueueOffset` 现算」。两条路都**必须另记一笔待办**（见 §五·1）。 |

### 层 B —— 消费层：为什么 −1 会变成 3000

- **建场期（`.mat` 资产）早就是对的**：`ArenaBuilder.GetOrCreateParticleMaterial` 自 **2026-09-27** 起算 `3000 + _QueueOffset`（现 `ArenaBuilder.cs:5133-5142`）。**现核**：`arenas/battlearena1/Materials/PS_TinyFlame.mat:36` = `m_CustomRenderQueue: 3005` ✓、`PS_SmokeySteam No Color.mat` = `3003` ✓。
- **运行时（真在画的那一份）是错的**：`ArenaOriginalMaterial.Rebuild()` 是 `Awake()`（真机）与 `ArenaBuilder.RenderPreview()`（预览，显式调）**两条路共用的同一个口**，它 `new Material(sh)` **另起一份**，队列取自组件序列化的 `queue` 字段；而那个字段由 `AttachOriginalMaterial(..., p.matQueue, ...)` 灌进去 = **−1** ⇒ `ApplyRenderState` 里 `if (queue > 0)` 不成立 ⇒ 停在上一句兜底的 `RenderQueue.Transparent`（**3000**）。
- 🔴 **所以修法不能在数据层**（`matQueue` 是产物、上游生成器一跑就被冲掉）**，也不只是在建场期** —— 落地点是 `ArenaOriginalMaterial`。

### 与 R2 §〇 的实测吻合（机制自洽）

火 **3000** 画在烘焙光斑 `Barrels Light FX`（**3002**，`ArenaBuilder.cs` 的网格路**序列化正确**）**之前** ⇒ 那块暖橙**盖在火上** ⇒ 亮黄芯被压、暗根被抬 ⇒ 「两头都丢」。这与主对话那组消融读数一致（关掉光斑后火区均值 96.6 → **73.5**，原版 71.7）。

---

## 二、逐颗对照表

**表 A** = R2 点名的「64 颗不符」**逐颗**（口径与 R2 完全相同：清单 `matQueue` vs `数据/游戏数据/mat_renderqueue.tsv`）。
**结论：64 行里只有 17 颗是真缺陷** —— 另外 **47 颗的原件 `m_CustomRenderQueue` 本身就写着 `-1`**（= 用 shader 的 tag，而 `battlearena1` 这批 shader 的 tag 就是 3000）⇒ **我们的 3000 本来就对，它们只是「`matQueue` 这一列缺数据」而不是「值错了」**。R2 的那条「64 颗」把这两种混在一起了。

表 A 见同目录 **`F4_表A_64颗逐颗.md`**（64 行，含「真缺陷?」列，避免本文件过长）。
**表 B = 本批真正会变的 18 颗：**

| # | 场 | 粒子 | 材质 | 改前 | 改后 | `_QueueOffset` | 原版 material JSON 的 `m_CustomRenderQueue` |
|---|---|---|---|---|---|---|---|
| 1 | battlearena1 | #19 TinyFlames | TinyFlame | 3000 | **3005** | 5 | 3005 |
| 2 | battlearena1 | #21 SmokeEffect | SmokeySteam No Color | 3000 | **3003** | 3 | 3003 |
| 3 | battlearena2 | #0 PoisonGas Black | wispySmoke Black | 3000 | **3001** | 1 | 3001 |
| 4 | battlearena2 | #1 Gas (1) | SmokeySteam Black | 3000 | **3003** | 3 | 3003 |
| 5 | battlearena2 | #6 TinyFlames | TinyFlame | 3000 | **3005** | 5 | 3005 |
| 6 | battlearena2 | #11 Glow | Glow Additive Soft | 3000 | **3004** | 4 | 3004 |
| 7 | battlearena2 | #42 RocketTrail | Smoke02 | 3000 | **2995** | −5 | 2995 |
| 8 | battlearena2 | #43 SandParticles | SandParticle | 3000 | **3001** | 1 | 3001 ⚠️ 见 §三·4 |
| 9 | battlearena3 | #15 Stuff | Embers Circle | 3000 | **3004** | 4 | 3004 |
| 10 | battlearenablacklegion | #4 Second eye | Embers Circle | 3000 | **3004** | 4 | 3004 |
| 11 | battlearenablacklegion | #5 Second eye | Embers Circle | 3000 | **3004** | 4 | 3004 |
| 12 | battlearenablacklegion | #6 Skull Eyes (1) | Embers Circle | 3000 | **3004** | 4 | 3004 |
| 13 | battlearenablacklegion | #7 Skull Eyes | Embers Circle | 3000 | **3004** | 4 | 3004 |
| 14 | battlearenablacklegion | #10 Second eye | Embers Circle | 3000 | **3004** | 4 | 3004 |
| 15 | battlearenablacklegion | #11 Skull Eyes (2) | Embers Circle | 3000 | **3004** | 4 | 3004 |
| 16 | battlearenablacklegion | #16 Gas (1) | SmokeySteam Black | 3000 | **3003** | 3 | 3003 |
| 17 | battlearenablacklegion | #17 PoisonGas Black | wispySmoke Black | 3000 | **3001** | 1 | 3001 |
| 18 | battlearenaleviathan | #40 Smoke_trail_2 | Smoke02 | 3000 | **2995** | −5 | 2995 |

**两个方向都在**（R2 猜「也可能有比原版小的」—— 有：`Smoke02` **2995**，比 3000 **还低**，×2 颗）。
**按场计数**：battlearena1 **2** · battlearena2 **6** · battlearena3 **1** · blacklegion **8** · leviathan **1** = **18**。

### 「`3000 + _QueueOffset`」这条公式的判据（**逐颗实读原版 `Material/*.json`，11/11 全中**）

| 材质 | 原件 `m_CustomRenderQueue` | `_QueueOffset` | 3000+off | 落在哪个包 |
|---|---|---|---|---|
| `TinyFlame` | 3005 | 5.0 | 3005 ✓ | sharedresources（**跨包**） |
| `SmokeySteam No Color` | 3003 | 3.0 | 3003 ✓ | sharedresources（跨包） |
| `SmokeySteam Black` | 3003 | 3.0 | 3003 ✓ | sharedresources（跨包） |
| `wispySmoke Black` | 3001 | 1.0 | 3001 ✓ | sharedresources（跨包） |
| `Embers Circle` | 3004 | 4.0 | 3004 ✓ | duplicateassetisolation（跨包） |
| `Glow Additive Soft` | 3004 | 4.0 | 3004 ✓ | duplicateassetisolation（跨包） |
| `Smoke02` | **2995** | **−5.0** | 2995 ✓ | sharedresources（跨包） |
| `SandParticle`（shared） | 3001 | 1.0 | 3001 ✓ | sharedresources（跨包） |
| `SandParticle`（prefabs） | 3000 | 0.0 | 3000 ✓ | battleprefabs（跨包） |
| `Flame03` | 3005 | 5.0 | 3005 ✓ | **scenes_battlearena2（本包）** |
| `Embers 1` | 3000 | 0.0 | 3000 ✓ | battleprefabs（跨包） |

---

## 三、改动清单

**公式**集中到**一个方法**：`WarpforgeVFX.ArenaOriginalMaterial.ResolveQueue(MatProp[] props, int matQueue)`
（`matQueue >= 0` 就用它；否则 `3000 + props._QueueOffset`）。三处消费全部改调它 —— **判据只此一处**。

| 文件:行号 | 改了什么（≤40 字） |
|---|---|
| `MyGame/Assets/WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs:437-489` | **新增** `ResolveQueue`（唯一公式 + 全套判据注释） |
| `…/ArenaOriginalMaterial.cs:491-495` | `ApplyRenderState` 加尾参 `MatProp[] props = null` |
| `…/ArenaOriginalMaterial.cs:523-533` | 🔴 **落地那一段**：`else if (transparent) mat.renderQueue = ResolveQueue(props, queue);` |
| `…/ArenaOriginalMaterial.cs:153-158` | `Rebuild()` 把 `props` 传进 `ApplyRenderState` |
| `MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs:4457-4486` | `AttachOriginalMaterial`：`transparent && queue<0` 时补真值 + **逐颗日志** |
| `…/ArenaBuilder.cs:5133-5142` | 建场期 `wantQueue` 改调 `ResolveQueue`（原写死 `3000+qoff`） |

**⚠️ 简报里的两条事实要订正**（现核）：
1. `ArenaOriginalMaterial.cs` 真身 **不在** `WarpforgeArena1/Editor/`，在 **`MyGame/Assets/WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs`**；它是 **LF**（简报写 CRLF —— 不成立）。
2. `arenas/<场>/<场>_manifest.json` 是 **CRLF**（简报写 LF —— 不成立）；`ArenaBuilder.cs` 是 CRLF ✓。**我只改了这两个 `.cs`，行尾都保住了**（`git diff --numstat` 见下）。

**为什么改两处（不是「两处写同一条规则」）**：公式只有一份（`ResolveQueue`），两个**施加点**分工不同 ——
- `AttachOriginalMaterial`（建场期）⇒ **场景/prefab 里序列化的 `queue` 就是真值**，`grep` 得到、可复算；
- `ApplyRenderState`（运行时落地点）⇒ **已经在盘上的旧场景（`queue` 还是 −1）不用重建也立刻正确**，也是 R2 §二 E1-b 提的那条路。

**为什么闸门必须带 `transparent`（现核，防回归）**：13 场有 **44 颗不透明粒子** + **55 个不透明网格子材质槽**的 `matQueue = -1`（那批是 `Mobile/Particles/Additive` / `Legacy Shaders/Particles/Alpha Blended Premultiply` / `Spine/Special/HiddenPass` 这类，`-1` 是「用 shader 的 tag」）—— 补成 3000 会把它们从 `AlphaTest`(2450) 那一档**挪进透明段**。实测这 44 颗的 `_QueueOffset` **全是 0**，闸门一加等于**逐条复算都不变**。

**为什么没加旁挂脚本（`工具/gen_arena_particle_queue.py`）**：`matQueue` 在全仓**只有一个消费者**（`ArenaBuilder.cs:2771` 的 `p.matQueue`），而那个点已经改成从 `props` 现算；**旁挂写的是产物、上游生成器一跑就被冲掉**（`gen_unity_arena_manifest.py` 在 `d:/2`，本仓没有真身），加它只会多一份会被静默冲掉的第二真相。**改消费点等价且不会被冲掉。**

**为什么没动 `data/游戏数据/mat_renderqueue.tsv`**：见 §三·4 与 §五·4 —— 它是**按材质名做键**的，本身有同名冲突，拿它当唯一判据会**反向引入错误**。

### 三·4 ⚠️ tsv 有一个**会咬人的名字冲突**（本批实测踩到）

`SandParticle` 在原件里有**两个不同材质**：`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_406942044651542579` 是 **3000 / `_QueueOffset` 0**，
而 `bundle_battlesharedresources_assets_all/Material/Material_6754647309651436631` 是 **3001 / `_QueueOffset` 1**。
两份 tsv/json 的 `_conflicts` 里**自己就写着** `{"SandParticle": [3000, 3001]}`，而它**取了 3000**。
`battlearena2 #43 SandParticles` 的 `matProps._QueueOffset` 是 **1** ⇒ 我们用的是**后者** ⇒ **真值是 3001**。
⇒ **照 tsv 修会把这一颗修错一格**；而 `_QueueOffset` 现算天然分得开。**这就是为什么不拿 tsv 当判据。**

---

## 四、改前 / 改后读数（⚠️ 我跑不了 Unity —— 下面三条**全部由主对话跑**）

### 4·1 主对话要跑的命令（顺序不能反，见简报 ⑤）

```bash
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
# 0) 按 CLAUDE.md §二 先留场景副本
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.BuildAll        -logFile -
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.BuildArenaPrefabs -logFile -
# 预览读的是【场景】（不是 prefab），但 preview 之前 prefab 也要跟着重建
unset ELECTRON_RUN_AS_NODE && WF_ARENA=battlearena1 WF_PSFIXSEED=1 "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.RenderPreviewFromCLI -logFile -
```

### 4·2 **可复算读数（三条）**

**(1) 日志计数** —— 新增一行 `[Arena/OS] 队列补真值：…`，**只在真的补了值时打**：

| 范围 | 期望行数 |
|---|---|
| 全 13 场（一次 `BuildAll`） | **18** |
| `WF_ARENA=battlearena1` | **2**（`TinyFlames`→3005、`SmokeEffect`→3003） |
| battlearena2 / 3 / blacklegion / leviathan | **6 / 1 / 8 / 1** |
| 其余 **8** 场 | **0** |

> 判据一句话：`grep -c "队列补真值" <日志>`。**多于 18 就是闸门写错**（我把闸门从「值变了」改成「`queue<0` 且算出≠3000」正是因为第一版会把 **483** 条全刷出来 —— 已修）。

**(2) 产物自检** —— 场景与 prefab 里**序列化**的 `queue` 字段（现基线 = 10-11 20:29 那版场景 / 20:30 那版 prefab）：

```bash
for v in -1 3000 3002 3003 3005; do
  printf 'scene  queue:%-5s -> ' $v; grep -c "^ *queue: $v$" MyGame/Assets/WarpforgeArena1/Scenes/battlearena1.unity
  printf 'prefab queue:%-5s -> ' $v; grep -c "^ *queue: $v$" MyGame/Assets/Resources/ArenaPrefabs/battlearena1.prefab
done
```

| `queue:` | 基线（改前·现核） | **改后期望** |
|---|---|---|
| `-1` | 35 | **33** |
| `3000` | 6 | **6**（不变 —— 这是对照组） |
| `3002`（两颗烘焙光斑） | 2 | **2**（不变 —— **它们本来就对**，正是「遮挡物」那一半） |
| `3003` | 0 | **1** |
| `3005` | 0 | **1** |
| `-` 合计 | 43 | 43 |

**(3) 渲染读数（真正验收）** —— R2 §二那条**同一个度量口**，火区框 `995,420,1115,510`：

```bash
python -I - <<'PY'
from PIL import Image; import numpy as np
im=np.asarray(Image.open(r'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/battlearena1/preview_battlearena1.png').convert('RGB')).astype(np.int16)
s=im[420:510,995:1115]; rb=s[...,0]-s[...,2]; v=s.max(2)
print('R-B>140',(rb>140).sum(),' R-B>180',(rb>180).sum(),' V>200',(v>200).sum(),
      ' mean %.1f'%s.mean(),' V<60',(v<60).sum())
PY
```

| 读数 | 原版（靶子） | 我们 20:31 基线 | **改后该看到** |
|---|---|---|---|
| `R−B>140` | 383 | 407 | ≈ 400 上下（**不是主判据**） |
| `R−B>180`（饱和亮黄芯） | **186** | — | **从低位明显回升**（E1 消融时到 166） |
| `V>200` | 113 | — | 回升（E1 时 172） |
| 框内均值 | **71.7** | 96.6 | **回落向 71.7**（E1 时 73.5） |
| `V<60`（暗根） | **2240** | — | **升到 2000 量级** |

> ⚠️ **判据是「向靶子靠」而不是「逐值等于 E1 那一版」**：E1 是**把光斑整块删掉**（暗部一路冲到 4041，**过火**）；本批只是把火**排到光斑之后**，所以暗部应落在 **2240（原版）与 4041（E1 过火）之间**。

**(4) 整图亮度比不能变差**（用户验收口）：

```bash
python d:/4/Unity/工具/arena_compare.py     # 逐场亮度比表；只看 battlearena1 那一行的比值
```
期望：battlearena1 的**整图亮度比与改前差 < 0.01**（本批在 arena1 只动了 **2 颗粒子**的**绘制顺序**，不改任何强度/颜色）。**这一条与 (3) 要一起看** —— (3) 变好而 (4) 不动 = 修对了；若 (4) 变差 > 0.01 就停下回报（说明动到了不该动的东西）。

---

## 五、没查清 / 没做的 + 顺手发现

### 5·1 没做（**判据在 `d:/2` 白名单外 ⇒ 必须由主对话落账，不是我判断「不做」**）

1. 🔴 **生成器那条根因仍在，且只差一行**：`d:/2/Warpforge_tools/scripts/unity_scene_to_godot.py:552-554`
   的 `mat_data()` 重建 dict 时补一个键即可（`'m_CustomRenderQueue': int(getattr(obj,'m_CustomRenderQueue',-1) or -1)`），
   `matQueue` 这一列就会**自然全对**（`d:/2` 是只读白名单外 ⇒ 我没动）。**这是层 A 的真修**，本批只是消费侧兜底。
   ⇒ 期望效果：重跑生成器后 `matQueue` 由「51/578 有值」变成「578/578」，届时 §四·2(1) 的 18 行日志会**归零**（那是**修好的标志**，不是回归）。
2. **`工具/gen_unity_arena_manifest.py` 的真身也不在本仓**（在 `d:/2/Warpforge_tools/scripts/`）⇒ 本批**没有**旁挂会被冲掉的键（我没加旁挂），但**上游一重跑，`matQueue` 仍会回到 −1** —— 不过**渲染不受影响**（消费点是现算的）。

### 5·2 顺手发现（**未修，未下结论**）

3. ⚠️ **网格 `.mat` 资产一律 3000**：`ArenaBuilder.GetOrCreateMaterial` 那条 `ApplyRenderState` 调用
   （`ArenaBuilder.cs:4887-4889`，只传 8 个实参）**根本没传 `queue`** ⇒ `.mat` 只可能是 3000。
   现核：`arenas/*/Materials/*.mat` **219 个**里 `m_CustomRenderQueue` = 3000 的有 **189 个**，**没有一个 3011**（而清单里 5 个 `Runes` 的 `queue` 是 **3011**、两颗光斑是 **3002**）。
   **运行期影响≈0**（`ArenaOriginalMaterial` 重建后以组件字段为准），**但 `Rebuild()` 失败时退回的就是这份 `.mat`** ⇒ 那时顺序是错的。**我没改**（不在本批主题、且改它只能靠 Unity 才能验证 `.mat` 重生成）。
4. ⚠️ **`数据/游戏数据/mat_renderqueue.tsv` 有同名冲突且取了错的那一个**（§三·4 的 `SandParticle`）。
   它同时是 `renderqueue_truth.json` / `mat_renderqueue.json` 的问题（两处 `_conflicts` 都点名了 `SandParticle`）。
   ⇒ **凡拿这份表当「材质名 → 队列」唯一判据的地方都要复核**（R2 §一① 就是拿它当判据的，方向对、单颗可能错）。
5. ⚠️ **`battlearenatauviorla` 有 1 颗粒子的材质整条解析不出来**：`#152 Big Gun Effect` —— `matName` / `matShader` **都是 null**、`matProps` 空 ⇒ 走 `ArenaOriginalMaterial` 把它整条跳过（"清单里没有 `props` ⇒ 不挂"）。**与队列无关**，记一笔。
6. ⚠️ **R2 的「64 颗」这个数本身会误导**（§二 已述）—— 47 颗不是缺陷。**凡用「清单 `matQueue` vs tsv」这个口径报数的地方，都要先把 `tsv == -1` 的那批摘出去。**

### 5·3 我**没有**做的（红线 / 边界）

- ⛔ 没跑 Unity、没跑 `typecheck.sh`、没动 git、没改任何正本、没碰 `CardPresentation/**` 与 `RuleEngine/**`、没碰 `d:/2`。
- ⛔ **没跑 `typecheck.sh` ⇒ 编译未验**（这是本批最大的未验项）。我按秒级检查的口径自查过：类型全对（`WarpforgeVFX.MatProp[]` ⇄ `ResolveQueue(MatProp[] …)`）、`using UnityEngine.Rendering` 在 `ArenaOriginalMaterial.cs:18` 已有、`ArenaBuilder.cs` 也在用 `RenderQueue`、**两文件花括号 63/63 与 1064/1064 配平**、行尾未翻。**仍需主对话跑一次类型检查**。
- ⚠️ **改前/改后的渲染读数我一个都没跑**（无 Unity）⇒ §四 那三条**全是「主对话该看到什么」的期望值**，不是实测。

### 5·4 `git diff --numstat`（改完立刻看，确认没翻行尾）

```
64      2       Unity/MyGame/Assets/WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs      (LF 保住)
412     2       Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs              (CRLF 保住)
```
🔴 **`ArenaBuilder.cs` 的 412 行里只有约 32 行是本批的** —— 其余是**本会话之前就有的未提交改动**
（会话开始时 `git status` 就已经把 `ArenaBuilder.cs` 标成 `M`，那不归本批）。**别把这 412 当成 F4 的改动量。**
本批**实际只碰了 4 个文件**：上面两个 `.cs` + 报告 `F4_粒子renderqueue.md` 与附表 `F4_表A_64颗逐颗.md`；
**清单 JSON 一个都没改、`git` 一次都没动。**

---

## 六、一句话交接

**根因** = 生成器跨包兜底路（`unity_scene_to_godot.py:552`）漏带 `m_CustomRenderQueue` ⇒ 清单 `matQueue=-1` ⇒ **运行时重建**（不是 `.mat` 资产）落 3000，而火真值 3005、烘焙光斑 3002 ⇒ **火被暖橙盖住**。
**修** = 新增唯一公式 `ArenaOriginalMaterial.ResolveQueue`（`3000 + _QueueOffset`，11/11 实读验证），三处消费改调它，闸门带 `transparent`。
**规模** = **18 颗**（R2 的 64 颗里 47 颗是「缺数据但值本来就对」）。
**待办** = ①`d:/2` 那行补丁（层 A 真修）②`GetOrCreateMaterial` 漏传 `queue` ③tsv 名字冲突 ④`typecheck` + §四三读数由主对话跑。
