# H6 · `Embers` 的贴图 + 两个生成器的注释（A418 + A419 + A420）（写手 · 2026-10-12）

> 一句话：**A419 已修好并验证**（`RectTransform` 那个 `KeyError`：90 → 0；产物逐字节不变 —— 今天生成器只覆盖 1 场）；
> **A420 的 3 处 + 顺手多找的 3 处已订正**；🔴 **A418 查到根因了，但那一步写盘在件里的白名单之外**
> （`Assets/WarpforgeArena1/**`）⇒ 本文给**逐条判据 + 一条命令 + 改完长什么样**，**没动那个目录一个字节**。

---

## 一、结论（三条各一句）

| 件 | 结论 | 落地 |
|---|---|---|
| **A418** | ✅ **根因 = 清单陈旧**（不是「生成器收不到贴图」、也不是「原版本来就没有」）：原版那颗粒子**有材质、材质上有贴图**（`Default-Particle`），**现版解析器也解得到**、**PNG 就在兜底目录里** —— 只是仓库里那份 `battlearena2_manifest.json` 是 **2026-09-30** 生成的，**早于 2026-10-06 的跨包锚点修复（A160）**。现读重跑 ⇒ **全清单只有 3 条变化**（`Embers` + 两个 `Light`，全是 `tex/texFile`）。 | ⛔ **未写盘**（白名单外）—— 见 §1.5 的接手单 |
| **A419** | ✅ **做完**：`gen_arena_groups.Scene` 现在把 `RectTransform` 也收进索引 ⇒ `chain()` 对 tauviorla 1377 个 GO **全部读得出来**（改前 90 个抛 `KeyError`）；**产物逐字节不变**、`gen_env_blendables.py --check` 逐字不变。 | `工具/gen_arena_groups.py`（+2 处过期注释） |
| **A420** | ✅ **做完**：`AnimFXController.cs` **4 处**（件里点名的 3 处 + 「四处」那个计数）＋ 同族的 **2 处**在 `ScenarioBlendables.cs`（同类说法，留着就是「两份说法打架」）。 | 两份 `.cs`，**只动注释** |

---

## 二、A418 —— `battlearena2/RocketTrail/Embers` 的 `texFile = None`

### 2.1 现核：原版那颗粒子**有材质、也有贴图**（UnityPy 直读，⛔ 不用 grep）

宿主 = `Scenario/Battle Arena 2 Particles/RocketTrail/Embers`，**GO pid 1255**（该名字的 GO 有 **2** 个：
另一个在 `…/RocketTrail/BigExplosion/Embers`，GO pid 495 —— 两条链、两个不同材质，别混）：

| 层 | 值 | 出处 |
|---|---|---|
| `ParticleSystemRenderer` | pid **1925**（`m_RenderMode = 1`） | `scenes_scenes_battlearena2.bundle` 直读 |
| `m_Materials[0]` | `{m_FileID: 8, m_PathID: -3007032700292194130}` | 同上 |
| → 材质 | 名 = **`Embers`**（跨包，`CAB-6e07b2d1…`） | `bundle.mat_data()` 解出来 |
| → `m_TexEnvs._BaseMap` | `{m_FileID: 4, m_PathID: 5681093337556782924}` | 同上 |
| → Texture2D | 名 = **`Default-Particle`** | 同上 |
| 现版解析器（走生成器自己那条路） | `tex_name = 'Default-Particle'` ✅ | `BundleResolver` + `Assembler.mat_of` 的**逐行复制版** |
| PNG 在不在 | ✅ 在兜底目录 `d:/2/解包整理/12_主程序资源/内置资源/Texture2D/Default-Particle.png`（生成器 `FALLBACK_TEX_DIRS` 第 3 项，2026-09-20 就是为它补的） | `Resolver.find()` |

对照（同父的另外两颗粒子，它们**有**贴图，说明「解析器整体是好的」）：
`Smoke` → `smokeysteam` · `Fire Small` → `Flame01` · `Twinkle` → `Twinkle` · `BigExplosion` → `Explosion`。

### 2.2 那为什么清单里是 `None` —— **清单比修复旧**

* 仓库里 `MyGame/Assets/WarpforgeArena1/arenas/battlearena2/battlearena2_manifest.json`
  的 mtime = **2026-09-30 06:47**。
* `工具/scripts快照/unity_scene_to_godot.py` 的 `BundleResolver.__init__` 那个
  「**恒取到 `.sharedAssets`** ⇒ 每场约 40% 外部引用指错包」的锚点缺陷，**2026-10-06 才修**（A160）。
* 这一颗的**材质引用与贴图引用两条都是跨包的**（`m_FileID = 8` / `4`）⇒ 旧锚点下这两条**都指不到对的那份包**
  ⇒ 清单里 `tex = None`、`texFile = None`（⚠️ 具体是「材质没解出来」还是「材质解出来了、贴图 ref 指错包」
  下面那段说了：**没复现**）（**且不报任何警告**，见 §七·5）。

⇒ **既不是「生成器没收到那张贴图」，也不是「清单输入里本就缺」** —— 是**清单生成于修复之前**。
（这也是为什么磁盘上 `Textures/Default-Particle.png` 是 **2026-08-15** 的：更早那一次生成把它拷进来过。）

⚠️ **旧版具体卡在哪一步，无法复现**（`mats` 整个空 = 材质没解出来，还是 `tex_ref` 空 = 材质解出来了、
贴图 ref 指错包 —— 这两条都能给出同一个 `texFile = None`，而那份旧代码 + 那次运行的日志**都不在了**）
⇒ **不写成结论**。能确证的只有两件：① 那条时间线（清单 09-30 < 锚点修复 10-06）；
② **现版代码对同一条引用解得出 `Default-Particle`**（§2.3 实跑）。

### 2.3 现读重跑一遍（**只算不写**），逐条 diff

探针 `_tmp_view/h6_manifest_fresh.py` 调的是生成器自己的 `build_manifest()`（**纯函数，不落盘**），
产物写 `_tmp_view/h6_fresh_battlearena2_manifest.json`：

```text
particles=46 meshes=55 skipped_ui=26
warn.tex_missing = []
warn.tex_fallback = {内置资源/Texture2D: ['Default-Particle.png']}

===== meshes：仓库里 55 条 · 现读 55 条 · 只在旧 0 · 只在新 0 =====（字段有差异 0 条）
===== particles：仓库里 46 条 · 现读 46 条 · 只在旧 0 · 只在新 0 =====
   Δ Light     ['matColor','matName','matProps','matShader','tex','texFile']  tex 旧=None → 新='Default-Particle'
   Δ Light     （同上）
   Δ Embers    ['tex','texFile']                                              tex 旧=None → 新='Default-Particle'
   （字段有差异的条目共 3 条）
```

* 顶层六节（`scene` / `camera` / `light` / `ambient` / `defaultEnv` / `postFx`）**逐字节相同**（另算过）。
* ⇒ **重跑是外科手术级的**：只补那 3 条的贴图（其中两个 `Light` 顺带把材质那一族字段也补上了）。
* ⚠️ **行尾**：仓库那份是 **CRLF（59943/59943）**，而 `run_one` 走的是**文本模式** `open(..., 'w')`
  ⇒ Windows 上写出 CRLF。⛔ **别把我留下的临时文件直接 `cp` 过去**（它是 LF，会把整份翻行尾）——
  照 §1.5 跑生成器。真要 copy，用我另存的 `_tmp_view/h6_fresh_battlearena2_manifest_crlf.json`。

### 2.4 修完的下游效果（A393 那条链）

`gen_env_blendables.py` 的 `scene_standalone_build()` 拿**两份清单**各算一遍（探针 `h6_a418_downstream.py`）：

| 清单 | `nodes[]` | `reparent[]` | 闸门计数 |
|---|---|---|---|
| 仓库那份（09-30） | 3 | **5** = BigExplosion · Smoke · Twinkle · Fire Small · Launch Smoke | `psTexNoMesh = 1`（就是 `Embers`） |
| 现读那份 | 3（不变） | **6** = 上列 + **`Embers`** | `psTexNoMesh = 0` |

⇒ 修完 `Embers` 会**进 `reparent[]`** —— 这正是它该在的位置：`RocketTrail` 那颗 `destroyTime = 6`
一销毁要**连带 6 个子件**（H2 §二那张表），少一个就是**静默少一半效果**。

### 2.5 还差哪一步（**主对话的腿** —— 本件白名单 ⛔ 不含 `Assets/WarpforgeArena1/**`）

```bash
# ① 重生成清单（cwd 必须是这个目录；生成器**两边同一份**、以仓库这份为准）
cd d:/4/Unity/工具/scripts快照
D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py --arena battlearena2
#    ⚠️ 别用 d:/2/Warpforge_tools/scripts/ 那份跑（历史上曾少 120 行；2026-10-01 已同步，
#       但口径 = 仓库这份，见 工具/README.md 的 `scripts快照/` 那一行）

# ② 重生成两份旁挂（`reparent` 5 → 6；`--check` 要过）
D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_env_blendables.py --check     # 先看
D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_env_blendables.py             # 再写

# ③ Unity 腿：重打战场 prefab（清单变了必须跟一次 —— 命令速查里就是这么规定的）
#    -batchmode -quit -projectPath D:\4\Unity\MyGame -executeMethod ArenaBuilder.BuildArenaPrefabs
```

**怎么验**：① 之后 `battlearena2_manifest.json` 里那条 `go == "Embers"` 的 `tex == "Default-Particle"`；
② 之后 `数据/游戏数据/env_blendables.json` 的 `sceneStandaloneBuild` 里 `battlearena2.reparent` 有 **6** 条（含 `Embers`）；
③ 之后 `BattleScene.Run` 的建场日志里 arena2 的「另跳过无贴图」计数少 **1**（那一条不再被闸门①挡掉）。

⚠️ **要重生成几场**：本件**实跑过 3 场**现读重跑 —— `battlearena2`（3 条差异）· `battlearenaleviathan`（1 条）·
`battlearenadarkangels`（0 条）。⇒ **确证要跑的只有前两场**；另 10 场**没跑**（`--all --no-copy` 一次跑全 13 场
约 **45 分钟**，要大面积重打就一次跑完更省事 —— 但 ⚠️ 那会把 13 份清单一起写盘，**先备份再对**）。

---

## 三、A419 —— `gen_arena_groups.py` 的 `Scene.chain()` 碰 `RectTransform` 会 `KeyError`

### 3.1 复现（改前）

探针 `_tmp_view/h6_groups_probe.py keyerror`（对**每一个** GameObject 调一次 `chain()`）：

```text
=== battlearenatauviorla：GO 1377 个 · chain 成功 1287 · 抛异常 90 {'KeyError': 90}
    index 里收进去的 transform 类型: {'Transform': 389}
    包里【所有】带 m_Father 的对象按类型: {'Transform': 389, 'RectTransform': 988}
    首个复现：GO pid 52 → KeyError: 3385
    父链（pid: 类型）: 1463:Transform → 3385:RectTransform → 1521:Transform → 3207:RectTransform → …
```

**根因**：`Scene.__init__` 只收 `Transform`（`self.tr = {p: d … if k == 'Transform'}`），
而 `chain()` 顺着 `m_Father` 往上走、**父可能是 `RectTransform`**（那一场 988 个）。

### 3.2 改动

`工具/gen_arena_groups.py` 的 `Scene.__init__`（现读行号 **84-95**）：`self.tr` 改成**两种都收**
（原版这两种类型的序列化字段**同名同义**：`m_GameObject`/`m_Father`/`m_Children`/`m_LocalPosition`/
`m_LocalRotation`/`m_LocalScale` ⇒ 下面 `chain`/`world_of`/`local_of`/`children_of` **一处都不用分家**；
一个 GO 上二者只会有一个 ⇒ `go2tr` 仍是一条对一条）。理由与实测数写在原地注释里。

顺手（同族、同类「记错了」）改掉 `gen_env_blendables.py` **2 处**解释守卫的旧注释（现读 **913-920** 与 **1330-1333**）：
那句「`G.Scene.chain` 碰上 `RectTransform` 会炸」——**根因已修**，守卫照留（仍可能因 typetree 读不出而 `KeyError`），
措辞从「已知会炸」改成「万一读不了就出声」。告警文案里「那个类的 `chain` 只索引 `Transform`、不认 `RectTransform`」也一并订正。

### 3.3 验证（改后）

| 项 | 改前 | 改后 |
|---|---|---|
| `chain()` 全量（tauviorla 1377 个 GO） | 成功 1287 · **KeyError 90** | **成功 1377 · 异常 0** |
| `build()` 产物（覆盖的场，逐字节） | — | **sha256 相同**（`b44caddfe2a74f4d…` · 3641 字节） |
| `gen_env_blendables.py --check` | ✅ 全过 | ✅ 全过（**输出逐字节相同**） |

⚠️ **今天 `want` 只覆盖 `battlearenatauviorla` 一场**（`_missingTargets` 已被 W3 清空、只剩 lookat 那几条）
⇒ `battlearenadarkangels_groups.json` 那份**已经是孤儿**（重跑不会重写它）——见 §六·5。

---

## 四、A420 —— `Battle/AnimFXController.cs` 的过期注释（**只动注释**）

件里点名的是「`preventDestroy = 0` 那 3 个 ⇒ 我们根本不建它们」这个说法。现读**一共 4 处**带这句
（件里数了 3 处，第 4 处在类摘要里），逐处订正；另在 **`ScenarioBlendables.cs`** 里又找到 **2 处**同类说法
（H2 那次漏掉的），一起订正 —— ⛔ 否则就是「两份说法打架」，比没有更糟。

### 4.1 逐处「原文 → 改后」（`AnimFXController.cs`）

| # | 现读位置 | 原文（要点） | 改后（要点） |
|---|---|---|---|
| ① | 文件头「场景侧实测」 | 「…**`preventDestroy = 0`** —— 但它们**不归任何 blendable 管**、我们这条链根本不建它们 ⇒ 「自毁」那条支路今天仍走不到」 | 「…`preventDestroy = 0`（逐条表出处照旧）＋ ✅ **2026-10-12（A393 数据 + A417 接线）**：那 3 个**我们这条链现在也建**（`BuildSceneAnimFx`，建在 `Warpforge_<场>` 之下、按传进来的 root 去重，调用点 `ArenaRuntimeLoader.cs:114`，**只此一处**；⚠️ 另一处 `EnvironmentApplier` **故意不接**：① 它第一次跑已是「打出一张进攻卡」② root 是 `Arena3D`、去重键不同 ⇒ 接了会建两遍）⇒ 「自毁」**走得到了**；⚠️ 但**真的会自毁的只有 `battlearena2` 的 `RocketTrail` 一个**（`preventDestroy=0` ∧ 组件 `m_Enabled=1` ∧ GO 开着 ∧ `destroyTime=6`；`Lightning_Green` 那两个组件原版就是关的 ⇒ `selfDestroyOk=false` 不排销毁）」 |
| ② | 「有意偏离 ③」的计数段 | 「第 7 个（`AnimFXModuleScreenShake`）挂在 `Railgun BIG (1)` 上、不归任何 blendable 管 ⇒ 我们这条路上这个缺口**今天走不到**」 | 「`Railgun BIG (1)` 正是 `sceneStandalone` 那 5 条之一 ⇒ 工厂**建的那一刻**就为它那一层出声（`MakeAnimFx`→`WarnAnimFxModules`，`ScenarioBlendables.cs:1696`）⇒ 这一档从「走不到」变成「**会走到、但还没有模块线**」（A394 仍开着）；⚠️ 这里说的出声是**工厂那句**，不是 `SetData` 里那句 —— 原版那个模块靠 `SetData` 触发，而 `SetData` 的调用点全是**卡**的 ⇒ 场景侧这条**会不会真触发没查清**（H2 §七·3），⛔ 别写成「会触发」」 |
| ③ | 「旁挂里的三层」的 `modules` 那行 | 「…而它不归任何 blendable 管 ⇒ 我们这条路上**今天走不到**模块那一支」 | 「…🔴 它**现在走得到**（`sceneStandalone` 那 5 条之一）⇒ 这条警告会**真的出声**（不是「走不到」了）」 |
| ④ | 类摘要（`class AnimFXController` 的 `<summary>`） | 「全库场景侧 7 个里另有 3 个是 `0`，但不归任何 blendable 管」 | 「🔴 **A393/A417 起那 3 个也走我们的工厂了** ⇒ 「自毁」这一档**已经真的会走到**」 |

### 4.2 顺手订正的两处（**件里没点，但同族**）

| 文件:行 | 原文 | 改后 | 为什么 |
|---|---|---|---|
| `ScenarioBlendables.cs:1815`（`WarnAnimFxModules` 的注释） | 「…而它**不归任何 blendable 管** ⇒ 这条路上模块数今天**恒为 0**（不会造出假警报）」 | 「🔴 **A393 + A417 之后不成立**：`Railgun BIG (1)` 会被建 ⇒ 这条警告**会真出现**，那时它是**正确的出声**，⛔ 别当假警报去消」 | 与 4.1 是**同一句话的副本**（铁律 6：清单与数字只留一处；铁律 5：顺手 grep 把副本一起改） |
| `ScenarioBlendables.cs:1122`（`OutBounce` 的注释） | 「⚠️ **这一支今天走不到**（`animFXController` 我们工程里没有）⇒ 真验它得等那件资产补齐」 | 「⚠️ **A420 订正**：这句已过期（同文件 `TauCannonAnimationStopper` 类注释 ③ 就写着 `animFXController` **现在解析得到了、2/2 都指到了** `Railgun turret` 上那颗 —— 两处打架）⇒ 这一支**跑得到**；`OutBounce` 自己**没有直接断言**（自检断到的是那一层的 `sounds`，`Editor/BattleScene.cs:1715`）⇒ 留给「画面/手感」那一档」 | 与**同一个文件里 80 行外的另一段注释直接矛盾**（那段是 A196 写的） |

### 4.3 一处**计数**过期

文件头「⚠️ 有意的偏离（**四处**，都有出处，别当缺陷改）」—— 实际列了 **①～⑤ 五条**（⑤ 是 A341 补的，
当时没跟着改这个数）⇒ 改成「五处」并留了订正痕迹。

**⛔ 没碰**：本文件与 `ScenarioBlendables.cs` 的**任何一行实现**（只动 `//` 与 `///` 注释）。

---

## 五、改动的文件（逐处 · 现读行号）

| 文件 | 现读行 | 改了什么 | 为什么 |
|---|---|---|---|
| `Unity/工具/gen_arena_groups.py` | 84-95 | `Scene.tr` 两种 transform 都收（+10 行注释写明实测数） | A419 正体 |
| `Unity/工具/gen_env_blendables.py` | 913-920 | 守卫的注释订正（「已知会炸」→「万一读不了就出声」） | 根因已修，旧注释不再成立 |
| `Unity/工具/gen_env_blendables.py` | 1330-1333 | 同上（`_unresolved` 那句告警文案里也删了「只索引 `Transform`」） | 同上 |
| `MyGame/Assets/CardPresentation/Battle/AnimFXController.cs` | 58-68 · 87-94 · 119-121 · 188-189 · 70-71 | A420 的 4 处 + 计数 | 见 §4 |
| `MyGame/Assets/CardPresentation/Battle/ScenarioBlendables.cs` | 1122-1127 · 1819-1823 | A420 同族的 2 处 | 见 §4.2 |

* **没动**：`battlearena2_manifest.json` 与 `Assets/WarpforgeArena1/**`（**白名单外**）、
  `Resources/EnvBlendables.json` 与 `数据/游戏数据/env_blendables.json`（**A419 的改动不影响它们的内容** ——
  `--check` 输出逐字节相同 ⇒ 没有重生成的理由；重生成要等 §2.5 那一步先做）。
* **行尾**：改完用二进制数过 —— 5 个文件**全是纯 LF、一个都没翻**（改前改后同一口径）。

---

## 六、跑过的检查（原文）

```text
# A419 复现/验证（只读，不需要 Unity）
D:/2/Warpforge_tools/py312/python.exe _tmp_view/h6_groups_probe.py keyerror
   改前：battlearenatauviorla GO 1377 · 成功 1287 · 抛异常 90 {'KeyError': 90}   首个 GO pid 52 → KeyError: 3385
   改后：battlearenatauviorla GO 1377 · 成功 1377 · 抛异常 0 {}
D:/2/Warpforge_tools/py312/python.exe _tmp_view/h6_groups_probe.py dump …    （改前/改后各一次）
   产物 sha256 相同（b44caddfe2a74f4d… · 3641 字节）⇒ **逐字节不变**

# 生成器自检（本件的验收条件之一）
PYTHONIOENCODING=utf-8 …/python.exe 工具/gen_env_blendables.py --check
   ⇒ ✅ 两侧自检全过（`_unresolved` 空）· 条目 139 · standalone 13 根/27 条 · sceneStandalone 3 场 5 条
   ⇒ 改动前后**输出逐字节相同**（`diff` 无差异）· 退出码 0

# A418 现读重跑（`build_manifest()` 纯函数，**不落盘**）
D:/2/Warpforge_tools/py312/python.exe _tmp_view/h6_manifest_fresh.py battlearena2
   ⇒ 46 粒子 / 55 网格 · tex_missing = [] · 只有 3 条不同（Embers + 两个 Light）
D:/2/Warpforge_tools/py312/python.exe _tmp_view/h6_a418_downstream.py battlearena2
   ⇒ 仓库清单：nodes 3 / reparent 5（闸门①吃掉 Embers）· 现读：nodes 3 / reparent 6

# 秒级类型检查（改完 .cs 立刻跑；最后一次 = 全部改动落定之后）
TMPDIR=/tmp/wf_h6 bash d:/4/Unity/工具/typecheck.sh
   运行时错误数: 0
   编辑器错误数: 0
```

* ⛔ **没跑 Unity**（红线）· ⛔ 没动 git · ⛔ 没改两张正本 · ⛔ 没越白名单
  （动过的文件 = §五 那一张表）· ⛔ 没跑自检（用户口径：A 表清完再跑）。
* 探针留在 `d:/4/_tmp_view/`：`h6_groups_probe.py` · `h6_manifest_fresh.py` · `h6_a418_downstream.py` ·
  `h6_embers_probe.py` / `h6_embers_probe2.py`（逐层解材质/贴图）· `h6_texsurvey.py`（13 场普查）。

---

## 七、顺手发现（⛔ 一个都没改，只报）

1. 🔴 **`battlearenaleviathan` 的 `Embers` 是同一个缺陷**（**已确证**，不是嫌疑）：现读重跑那一场，
   全清单**只有 1 条不同** —— `Embers` 的 `tex: None → 'Default-Particle'`（44 网格 / 45 粒子集合逐个相同）。
   ⇒ §2.5 那条命令**应该对 leviathan 也跑一遍**（`--arena battlearenaleviathan`）。
   （对照组：`battlearenadarkangels` 现读重跑 **0 条差异** ⇒ 它的清单不陈旧。）
2. 🟡 **13 场里一共 18 条 `tex == None` 的粒子条目**，逐条去原版对了一遍（`h6_texsurvey.py`，UnityPy 直读）：

   | 场 · 条目 | 原版那颗粒子解出来的是 | 判 |
   |---|---|---|
   | `battlearena2` `Embers` ×1 · `Light` ×2 | `'Default-Particle'` | 🔴 **清单陈旧**（§2.3 已实测） |
   | `battlearenaleviathan` `Embers` ×1 | `'Default-Particle'` | 🔴 **同上**（发现 1，已确证） |
   | `battlearena3` `Necrons Close Monolith Rays` ×2 | 材质 `Necrons Flying Monolyth Rays` **没有 `_BaseMap`/`_MainTex` 槽** | ✅ 原版本来就没有 —— **不是缺陷** |
   | `battlearenablacklegion` `Heat Distortion` ×4 | 材质 `Heat Distortion` **无贴图槽** | ✅ 同上（热扰动就靠 distortion，不采样贴图） |
   | `battlearenatauviorla` `Muzzle Flash view distort` ×5 | 材质 `RippleSubtle Distort` **无贴图槽** | ✅ 同上 |
   | `battlearenatauviorla` `Big Gun Effect` ×1 | `m_Materials[0]` 是一条**空引用**（`0/0`） | ⚠️ **没查清**（是「原版就没挂材质」还是「挂了个空槽」）—— 该对象宿主 GO 还关着，**归战场线** |
   | `battlearenadarkangels` `Lance Fire (5)` ×2 | 两个同名 PSR：一个解出 `'Shine trail'`（有贴图）、另一个是 `HiddenPass`（**无贴图槽**） | ✅ **不是陈旧**：现读重跑那一场 **58 网格 / 34 粒子逐条相同、0 条差异** ⇒ 清单与现版生成器一致；那两条 `tex = None` 是**现状**（哪条 GO 对哪条 PSR 没逐条定）—— **要动归战场线**，不是本件那个根因 |

   * 现读重跑实跑过 **3 场**：`battlearena2`（3 条差异）· `battlearenaleviathan`（1 条）· `battlearenadarkangels`（**0 条**）
     ⇒ 「陈旧清单」这个根因**已确证只在 2 场**（另 4 场本来就是空的 `mats` 那一档）。
3. 🟡 **`battlearena2` 没有 `battlearena2_psmesh.json`**（生成器每次都打一条「renderMode=4 的粒子会退回 Billboard」）。
   今天**零影响**（那一场 `renderMode == 4` 的条目 = **0**）；但**别的场有 19 条**（arena3 2 · blacklegion 6 ·
   darkangels 1 · tauviorla 10）⇒ 那些场有没有这份 sidecar **没核**。
4. 🟡 **`battlearenadarkangels_groups.json` 是孤儿产物**：今天 `wanted_paths()` 只覆盖 `battlearenatauviorla`
   一场（`_missingTargets` 已被 W3 清空、只剩 lookat 的 target 那几条）⇒ **重跑生成器不会重写 darkangels 那份**。
   ⚠️ 那份还在被建场侧读（`ArenaBuilder.ApplyGroupNodes`）—— 是「不再更新」而不是「已被删」，**要不要补回覆盖范围请调度台裁**。
5. 🟡 **清单生成器有一处静默口**（`工具/scripts快照/gen_unity_arena_manifest.py`，**本件白名单外，没动**）：
   粒子那段只在「`tex_name` 有值但文件找不到」时报 `tex_missing`、在「`mats` 整个空」时报 `note_default`；
   **「材质解出来了、但 `tex_name` 仍是 `None`」这一格一条警告都没有** ⇒ 这正是 §2.2 那个缺陷藏了一个月的通道。
   建议：加一档 `warn['tex_slot_missing']`（名字 + 材质名）。
6. ⚪ `资料/命令速查.md:188-194` 仍写着用 `d:/2/Warpforge_tools/scripts/gen_unity_arena_manifest.py` 跑，
   而 `工具/README.md` 与 `资料/战场场景线_交接.md:267-278` 已把口径改成**仓库 `工具/scripts快照/` 那一份**
   （2026-10-01 两边已同步、md5 相同 ⇒ **今天跑哪份结果一样**，但两份文档口径不一致，**归文档线**）。
