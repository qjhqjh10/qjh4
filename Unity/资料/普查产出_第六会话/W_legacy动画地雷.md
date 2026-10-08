# `legacy动画地雷` —— A1095（legacy `Animation` 的片段 guid 全 0 是【系统性】的）+ A1096①（半成品入库）

> 本件 = **一件两支**。**A1095 已做**（`EffectExporter` 在 `Animator` 那支的**同一处**补了一支 legacy `Animation` 的）。
> **A1096① 选 (B)**（在 `WarpforgeVatDriver.cs` 文件头**如实标死**，⛔ **没动 `.gitignore`**）。
> **本次没跑 Unity**（本轮红线：待办没做完之前不跑自检）· **没动 git** · **没改正本** · **没动 `d:/2`**（只读引用）· 行尾逐个数过。

---

## ① 结论

- **A1095：✅ 已做。** `EffectExporter.cs` 在 `:1194` 那支 `Animator` 的**正下方、同一形状**补了一支处理 legacy `Animation` 的（新 **`:1203-1268`**，调用面就是同一个 `Export()`、同一处 `PrefabUtility.SaveAsPrefabAsset` 之前）。
  **45 处逐条普查过**（见 §②）：**34 个不同的片段（34 个不同名字）全部能从原版包里找回**（**33 条**在特效主包 `battleprefabs_vfxandmisc`，**1 条** `ScytheAssault` 在 `cardanimsgeneral`）⇒ **重导即 45/45 全好**，⛔ **没造任何空 clip**。
  ⚠️ 但**这一趟没跑 Unity** ⇒ 那 45 处在**盘上仍是 guid 全 0**（新代码只在**编译**层面验过）—— 这是本件最大的未验证面，见 §⑥·1。
- **A1096①：选 (B)。** 在 `WarpforgeVatDriver.cs` 文件头加了一节「🔴 版本库边界」（**`:13-50`**）如实写死：本组件进仓库、它要挂的 8 个资产**一个都不进**、clone 下来要重导。**⛔ 没改 `.gitignore`。**
  **理由（关键一条是「(A) 根本不够」）**：实测这两个 prefab 分别引用 **10 / 26** 个 guid，其中 **8 / 24 个同样被 ignore**（材质 / 网格 / 贴图都在 `WarpforgeVFX/{Materials,Meshes,Textures}` 下）⇒ 只放行那 8 个 = 往仓库里塞一份**引用悬空的半个 prefab**，**比一份都不放更糟**；要真能用就得整棵 `WarpforgeVFX/*`（**1.8 GB**）进库，那正是 `.gitignore:24-30` 要避免的。另外 `.gitignore:4-8` 的仓库原则原文就是「原版资产的直接衍生物留在本地，随时可以用导出器重新生成」。

---

## ② 45 处清单（逐条 · 现读 `WarpforgeVFX/Prefabs/*.prefab` 963 件）

**怎么扫的**：按 `--- !u!111 &<pid>` 切块 → 取 `m_GameObject` → 回查 `--- !u!1` 的 `m_Name`；`m_Animation` 的 `fileID` **就是原版包里的 pathID**（所以能直接对到 `assets_full/…/AnimationClip/AnimationClip_<fileID>.json` 的 `m_Name`）。

**每条都「找得回」**：34 个 fileID → 34 个片段名（**1:1**），**34/34 都能在包里找到 `AnimationClip_<fileID>.json`**（原判据里那句「ScytheAssault 那条 clip 找不到」**就地订正**：它**不在特效主包里、在 `cardanimsgeneral_assets_all.bundle` 里**，`m_Name: ScytheAssault`）。
**但工程里现在一条都没有**：`WarpforgeVFX/Animations/` 只有 10 份 `.anim`（`Booster*` 8 + `Card Explosion` + `OpenCardbacks` + `Vanguard Frame Animation`），**与这 45 处要的 34 个名字零交集** ⇒ **不是「工程里有、只是引用断了」，是「工程里压根还没有这份 `.anim`」**（要靠新那一支 `ImportClip` 落出来）。

| # | prefab | `Animation` 组件 | 所在 GameObject | `m_Animation` fileID | 原版片段名（`AnimationClip_<fid>.json` 的 `m_Name`） | `m_Animations[]` 条数 | prefab 内行号 |
|---|---|---|---|---|---|---|---|
| 1 | `AcidSpraySweepAttack` | `4786195456726183908` | `RotationHolder` | `6985008914016165058` | `SweepBackAndForth_loop` | 1 | 30827 |
| 2 | `Axe_Slash_User_SW` | `3757411893318604746` | `'Axe '` | `6434415063491352858` | `Axe Swing` | 1 | 39960 |
| 3 | `BolterSweep` | `2970361839175760751` | `RotationHolder` | `9112845835116696623` | `SweepLeftToRight` | 1 | 44964 |
| 4 | `BolterSweep_2` | `828139886047119196` | `RotationHolder` | `9112845835116696623` | `SweepLeftToRight` | 1 | 53745 |
| 5 | `Buff_SW_Blizzard_Board` | `547484068237436161` | `Buff_SW_Blizzard_Board` | `8295435767382938789` | `Blizzard` | 1 | 37 |
| 6 | `BulletImpact_RiftCannon` | `7051277864056480528` | `BulletImpact_RiftCannon` | `-5604586061599474967` | `Stormsurge Cannon` | 1 | 34441 |
| 7 | `BulletImpact_Tau_PulseARCcannon` | `6452936270987004247` | `BulletImpact_Tau_PulseARCcannon` | `-5604586061599474967` | `Stormsurge Cannon` | 1 | 63775 |
| 8 | `CardPrefab` | `376850880879214440` | `Minion Death Icon` | `257802963501847154` | `Minion Death Icon` | 1 | 599 |
| 9 | `CardPrefab` | `1902525027918571464` | `CardPrefab` | `-3614292623624764332` | `Card Hand To Board` | 1 | 5273 |
| 10 | `CardPrefab` | `7426653607220555336` | `DamageCounter` | `-3234148433819516763` | `InBattleDamageCounter Variation 1` | 1 | 7070 |
| 11 | `EC Heldrake Attack` | `8186968347361474168` | `GameObject` | `7453939348510395111` | `SweepBackAndForth_Adjacents` | 1 | 4939 |
| 12 | `Environmental Condition Astra Militarum Planetary Invasion` | `1109556066915240365` | 同名根 | `-2891852251679511359` | `Planetary Invasion Animation` | 1 | 83492 |
| 13 | `Environmental Condition Black Legion Helfire Outburst` | `2736227172189926630` | `Helfire Parent` | `4439009046561175037` | `Helfire` | 1 | 488613 |
| 14 | `Environmental Condition Emperor's Children 1 Green` | `8860168305736767971` | 同名根 | `6598746115372763503` | `Emperor's Children Environmental Condition 1` | 1 | 152325 |
| 15 | `Environmental Condition Emperor's Children 1 Pink REJECTED` | `2691539558653416403` | 同名根 | `6598746115372763503` | 同上 | 1 | 230692 |
| 16 | `Environmental Condition Emperor's Children 2 Fumes` | `7175910159444528891` | 同名根 | `6598746115372763503` | 同上 | 1 | 92647 |
| 17 | `Environmental Condition GSC Sump Overspill` | `7851847460977275752` | `Particles floding` | `-2907703756482590602` | `Sump Overspill` | 1 | 240656 |
| 18 | `EnvironmentalCondition Sororitas Raging Storm` | `814471626983929759` | `Lightning Main Start` | `4113020123666412222` | `Thunderstorm Flash` | 1 | 24544 |
| 19 | `EnvironmentalCondition Tau Radiation Storm` | `5743385205749018459` | `Fire Tornado` | `3535329996230348736` | `Firestorm Tornado` | 1 | 162101 |
| 20 | `EnvironmentalCondition Tau Solar Eclipse` | `1952672847837831931` | `Sun Sprite` | `7544575172993598529` | `Solar Eclipse` | 1 | 10348 |
| 21 | `EnvironmentalCondition Ultramarines Aerial Clash` | `8127471716172658120` | `Barge Background` | `-7183400070537912434` | `Fleet Support Barges` | 1 | 24658 |
| 22 | `EnvironmentalCondition Ultramarines Thunderstorm` | `2046355585094362993` | `Lightning Main Start` | `4113020123666412222` | `Thunderstorm Flash` | 1 | 88131 |
| 23 | `Explosion Hive Fleet Arrival Explosions only` | `8747428902918294238` | `HiveFleetTendrilsGround` | `-2990494797455460989` | `Scene` | 1 | 333619 |
| 24 | `Explosion Hive Fleet Arrival Explosions only` | `6157468480744362230` | 同名根 | `6140914303460967852` | `Hive Fleet Arrival PP` | 1 | 438259 |
| 25 | `Explosion Hive Fleet Arrival Tendrils OLD` | `7849345077608248151` | 同名根 | `6140914303460967852` | `Hive Fleet Arrival PP` | 1 | 15227 |
| 26 | `Explosion Hive Fleet Arrival Tendrils OLD` | `60179534426959095` | `HiveFleetTendrils` | `2778023229599158438` | `HiveFleetTendrils` | 1 | 251333 |
| 27 | `Explosion Hive Fleet Arrival Tendrils` | `463629934917338791` | 同名根 | `6140914303460967852` | `Hive Fleet Arrival PP` | 1 | 98623 |
| 28 | `FlamethrowerSweepAttack_1` | `940234565707116491` | `RotationHolder` | `-8203330189142018204` | `SweepBackAndForth` | 1 | 10156 |
| 29 | `FlamethrowerSweepAttack_sororitas` | `1556057995385102102` | `RotationHolder` | `-8203330189142018204` | `SweepBackAndForth` | 1 | 25317 |
| 30 | `GatlingSweep` | `4058682366150125658` | `RotationHolder` | `9112845835116696623` | `SweepLeftToRight` | 1 | 50289 |
| 31 | `Godspear Warhead Full` | `3491940482706414997` | 同名根 | `7331486603526641762` | `Godspear Warhead` | 1 | 161542 |
| 32 | `Hammer_Throw_SW` | `536845024803805052` | `Hammer` | `6434415063491352858` | `Axe Swing` | 1 | 60453 |
| 33 | `NoMukkinAboutEffect` | `421757183745943066` | `Choppa` | `-3567400858081000579` | `Choppa Animation` | 1 | 54085 |
| 34 | `Pray_Idle` | `4044027703009065783` | `Pray VFX mesh` | `6501569820361429241` | `Pray Enter` | **2** | 15213 |
| 35 | `Rapturous Ruination Board` | `6351684289371314955` | 同名根 | `6598746115372763503` | `Emperor's Children Environmental Condition 1` | 1 | 73373 |
| 36 | `RemnantBody3D Aeldari` | `7371385936580652705` | 同名根 | `7976426721533028797` | `To Remnant Aeldari` | 1 | 83709 |
| 37 | `RemnantBody3D Aeldari` | `2698623880336777184` | `Idle` | `-6016683441228044944` | `Spirit Stone Idle` | 1 | 105791 |
| 38 | `RemnantBody3D Necrons` | `8038929106009293566` | 同名根 | `3126945503027080555` | `To Remnant Necrons` | **2** | 44330 |
| 39 | `Sau_ParticleWhip` | `6000170231073413734` | 同名根 | `456144989323822665` | `Particle Whip` | 1 | 58725 |
| 40 | `Sau_ResurrectionOrb` | `4281143643801252460` | 同名根 | `-6419630578326763624` | `Resurrection Orb` | 1 | 29509 |
| 41 | `Sau_SolarPulse` | `4235546238469408067` | 同名根 | `7762669931733622927` | `Solar Pulse` | 1 | 37 |
| 42 | `ScytheAssault` | `3488790369807353159` | 同名根 | `-5462575267959983568` | `ScytheAssault`（**在 `cardanimsgeneral` 包里**） | 1 | 58528 |
| 43 | `Swarm Card Merge VFX` | `6398126263261672440` | 同名根 | `-5385015516813041859` | `Swarm Triat Merge` | 1 | 9919 |
| 44 | `Valkyrie_MultiLaser_Hover` | `547552580549836153` | `MovementParent` | `-8669476881772964070` | `Valkyire Drop Movement` | 1 | 25517 |
| 45 | `Will Of Gork` | `8329555536638940161` | 同名根 | `2161858914032100247` | `Will of Grok` | 1 | 39495 |

**TOTAL = 45 处 / 34 个不同片段 / 40 件 prefab**（同一件 prefab 里出现 2~3 处的：`CardPrefab` 3 · `Explosion Hive Fleet Arrival Explosions only` 2 · `… Tendrils OLD` 2 · `RemnantBody3D Aeldari` 2）。
**`m_Animations[]` 条数 > 1 的那两条**（`Pray_Idle` = `[Pray Enter, Pray Exit]` · `RemnantBody3D Necrons` = `[To Remnant Necrons, From Remnant Necrons]`）**是「只补 `m_Animation` 会漏」的活证据** ⇒ 新那一支**两条入口都写**。

### `WarpforgeVFX/Prefabs` 里 `--- !u!111` 的**全量 50 个** = 45（上面）+ 5（下面）

| 分类 | 数 | 明细 | 是不是缺陷 |
|---|---|---|---|
| `m_Animation` guid 全 0 | **45** | 上表 | ✅ 是（本件修的） |
| `m_Animation` 有真 guid | **1** | `VanguardIdleEffect.prefab:59081/59083` → guid `067f477a302d4bfe86c5a04ddd081e6e` = `WarpforgeVFX/Animations/Vanguard Frame Animation.anim`（**上一件手改的，重导会被打回**） | ✅ 已好（但脆） |
| `m_Animation: {fileID: 0}` | **4** | `CardPrefab`(comp `7789851131101241589`, `m_Enabled: 0`, `m_Animations: []`) · `EnvironmentalCondition Sororitas Shrine Bombardment` · `PenitenceEffect` · `StrikeEffect`（后三个 `m_Animations: [- {fileID: 0}]`） | ⛔ **不是**。原版包里**同样 4 个** `m_Animation = 0`（形态也逐一对得上：3 个 `[0]` + 1 个空表）⇒ **原版本来就没接片段**，别给它造一个 |

---

## ③ 改动清单

| 文件 | 改前 | 改后 |
|---|---|---|
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`（**`:1203-1268`**，插在 `Animator` 那支的 `}`（`:1201`）与 `// ---- 挂 binder ----`（`:1270`）之间） | （无） | 新增一支：**头注 28 行**（`:1203-1230`：根因 / 45 处规模 / 那 4 个不算 / 两条入口 / 为什么不传 `loop` / 跨包）× + `foreach (var an in inst.GetComponentsInChildren<Animation>(true))` 主体（`:1231-1268`） |
| 同上 `:902` | `PrefabUtility.SaveAsPrefabAsset`（`:1216`）之前 | **`:1283`**（被本件 +67 行推下去了 —— 铁律 5：就地改掉） |
| `Unity/MyGame/Assets/CardPresentation/Battle/WarpforgeVatDriver.cs`（**`:13-50`**） | （无） | 新增「🔴 版本库边界」一节：8 个被 ignore 的资产**逐条带字节/md5/mtime** + 「clone 下来怎么补」三步 + 「⛔ 别拿 `ImportTexture` 重导那两张贴图」+ 「为什么 (A) 不够」 |
| 同上 `:7-8` | `:1309` / `:878` | **`:1554` / `:1053`**（现读刷新，都漂了；行尾注里留了原值） |

**净改动**：`EffectExporter.cs` **+67 行 / 0 删除**（我自己这一支；`:902` 是**同行改写**、不计行数）· `WarpforgeVatDriver.cs` **+39 行 / 0 删除**（`:7-8` 同行改写）。**没动 `.gitignore`**（选 (B)）。
⚠️ `git diff --numstat` 现在会显示 `245  0  EffectExporter.cs` —— 那是 **A1089 那份还没提交的 178 行 + 我的 67 行**（`2593 − 2526 = 67`，见 §⑤）。

---

## ④ 证据：与 `:1194` 那支的逐行对应（同一形状）

| `Animator` 那支（现读 `:1193-1201`） | 本件新那支（现读 `:1231-1268`） | 说明 |
|---|---|---|
| `:1194` `foreach (var an in inst.GetComponentsInChildren<Animator>(true))` | `:1232` `foreach (var an in inst.GetComponentsInChildren<Animation>(true))` | **同一形态**（`true` = 含未激活；实测 `CardPrefab` 有一个 `m_Enabled: 0` 的也在内） |
| `:1196` `var rc = an.runtimeAnimatorController;` | `:1237` `var clips = AnimationUtility.GetAnimationClips(an.gameObject);` / `:1238` `var def = an.clip;` | 都是「**把 bundle 那一侧的引用读出来**」。⚠️ legacy 要读**两处**（默认那条 + 数组），`Animator` 只有一处 |
| `:1197` `if (rc == null) continue;` | `:1239` `if (clips != null && clips.Length > 0)` / `:1244` `if (clips[i] == null) continue;` | 空槽跳过（**但不是 `continue` 整个组件** —— `Pray_Idle` 有第 2 条） |
| `:1199` `var proj = ImportAnimatorController(rc);` | `:1245` `var a = ImportClip(clips[i]);` | **同一个落盘套路**（`Instantiate` → `CreateAsset`/`CopySerialized` 原地覆盖、guid 不变） |
| `:1200` `if (proj != null) an.runtimeAnimatorController = proj;` | `:1256` `AnimationUtility.SetAnimationClips(an, imported);` + `:1263` `an.clip = d;` | **接回引用**。⚠️ 这一处**是有意的差别**：legacy 要写两张表，且**顺序是「先数组、后默认」**（`:1258-1259` 注释写了为什么 —— 不依赖 `clip` setter 会不会顺手往数组里补一条这个没查证的细节） |
| （那支不做出声：`rc == null` 静默跳过） | `:1248-1251` `Debug.LogWarning` + `:1266-1268` 汇总一条 | **不许静默失败**：接不上就点名「哪件 prefab / 哪个 GameObject / 第几条 / 片段名」，并且**明写「不拿空 clip 顶上」** |
| `:1198` `origController = rc.name;`（记名字给 binder） | —— | legacy 这边**没有**对应的消费方（`WarpforgeEffectBinder` 只有 `animatorController` 一个字段）⇒ 不记 |

**四处有意的不一样**（都写在代码注释里）：

1. **不传 `loop`**（`ImportClip(c)` 走默认 `null`）：`Animator` 那支要按控制器状态的 `m_Loop` 显式设，而 legacy clip 自己的 `m_WrapMode` / `m_AnimationClipSettings` 会随 `Instantiate` 一起过来 —— **实测证据**：落出来的 `Vanguard Frame Animation.anim` 就是 `m_WrapMode: 1` / `m_LoopTime: 0`（与原版的 `m_WrapMode: 1`(=Once) 一致）。
2. **用了非 obsolete 的重载**：`AnimationUtility.GetAnimationClips(Animation)` 在 Unity 文档里写着 obsolete（「has been replaced with GetAnimationClips(GameObject)」）⇒ 走 `GameObject` 那个。
3. **两条出口都写**（`m_Animations[]` + `m_Animation`），`Animator` 只有一个字段。
4. **零幻觉兜底**：导不出来就**留空 + 出声**，绝不 `CreateAsset` 一个空 clip。

---

## ⑤ 验证

**秒级类型检查**（`TMPDIR=/tmp/wf_anim bash d:/4/Unity/工具/typecheck.sh`）—— 跑了 **4 次**（加完 EffectExporter 那支 / 加完 VatDriver 头注 / 改完 `:902` 行号 / 最后一趟）
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
**每次都 0/0。** ✅ 另核「我的文件真被编进去了没有」：`/tmp/wf_anim/wf_csc_editor.rsp:1` 命中 `EffectExporter.cs`、`/tmp/wf_anim/wf_csc.rsp:1` 命中 `WarpforgeVatDriver.cs` ⇒ **不是「没编所以 0 错」**。
⚠️ 本轮别的写手在改 `Shell/` `Core/` `RuleEngine/`（`git diff --numstat` 里几十个文件）—— **0 条错**说明没撞上别人的半成品。

**行尾**（二进制读，改前 → 改后；⛔ 没用 `sed -i`、⛔ 没用 python 文本模式写）

| 文件 | 改前 CRLF / LF | 改后 CRLF / LF |
|---|---|---|
| `EffectExporter.cs` | **2526 / 2526**（纯 CRLF） | **2593 / 2593**（+67 行整行新增，CRLF 未翻） |
| `WarpforgeVatDriver.cs` | **0 / 365**（纯 LF） | **0 / 403**（+38 行；`:7-8` 是同行改写，不增行） |

**`git diff --numstat`**（改完立刻跑的）
```
245     0       Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs
```
⇒ 只有 1 个文件、**纯新增、0 删除**；**没有全文件重写**（`+67` 行不是 `2526` 行）。
⚠️ **245 = A1089 未提交的 178 + 我的 67**（自己那一份的判据是行数差 `2593 − 2526`，不是 numstat）。
⚠️ **`WarpforgeVatDriver.cs` 不在 numstat 里** —— 它是 `??`（未跟踪、**没被 ignore**）⇒ 本来就不出现在 `git diff`；「我改了它没有」的判据是 **行数 403 / 字节 26704**（改前 365 / 22797）。

**⚠️ 被 `.gitignore:26` 挡住、`git status` 看不见的产物**（**单列，因为这是 A1096① 的全部内容**）
`git check-ignore -v` 逐条核过，命中同一条 `.gitignore:26: /Unity/MyGame/Assets/WarpforgeVFX/*`：

| 产物（`Assets/WarpforgeVFX/` 下） | 入库? | 字节 | md5(前 12) | mtime |
|---|---|---|---|---|
| `Prefabs/Vanguard Frame Animated VAT.prefab` | ⛔ ignored | 6714 | `aa0309ab6450` | 2026-10-08 23:34 |
| `Prefabs/VanguardIdleEffect.prefab` | ⛔ ignored | 3008659 | `a82e0a059df8` | 2026-10-08 23:34 |
| `Animations/Vanguard Frame Animation.anim` | ⛔ ignored | 7845 | `88388c3792f3` | 2026-10-08 23:34 |
| `Animations/Vanguard Frame Animation.anim.meta` | ⛔ ignored | 188 | `0e5391661550` | 2026-10-08 23:34 |
| `Textures/Vanguard Frame Animation VAT_PositionTex.png` | ⛔ ignored | 544 | `0e2481439826` | 2026-10-08 23:33 |
| `Textures/Vanguard Frame Animation VAT_PositionTex.png.meta` | ⛔ ignored | 2650 | `0d908f143dbf` | 2026-10-08 23:33 |
| `Textures/Vanguard Frame Animation VAT_RotationTex.png` | ⛔ ignored | 584 | `339d21b4448b` | 2026-10-08 23:33 |
| `Textures/Vanguard Frame Animation VAT_RotationTex.png.meta` | ⛔ ignored | 2650 | `601b3fe32421` | 2026-10-08 23:33 |

**这些数与上一件报告 §④ 的表逐格相同**（本件自己重算了一遍 md5，不是照抄）⇒ **这批资产自 2026-10-08 23:33/23:34 起没被动过**。
`WarpforgeVatDriver.cs` / `.meta`（22797 / 59 字节）**不在表里** —— 它们是 `?? …（未跟踪、没被 ignore）`，与 8 个资产相反。

---

## ⑥ 没查清 / 停手的

### 1. 🔴 本件的新代码**一次都没被执行过**（最大的未验证面）

没跑 Unity ⇒ 新那一支只在**编译**层面验过（0 错）。三个**只有跑起来才知道**的点：

1. **`an.clip` / `GetAnimationClips(go)` 在导出那一刻真取得到吗？** 判据是**类比**：同一处的 `Animator` 那支能读出 `rc.name`（上一件已落地并跑过），而 legacy 片段同样是**同一个包/同一份依赖树**里的对象 —— 但**我没在实况里量过**。**尤其 `ScytheAssault` 那条是跨包引用**（`cardanimsgeneral`），加载段（`:681`）确实是「`BundleDir` 下全部 `*.bundle` 都载进来」，但**「载进来」是否等于「跨包引用解析得到」我只有代码依据、没有实测**。
   ⇒ **怎么收口**：跑一次 `EffectExporter.RunListed`（`ListedPrefabs` 只有 28 条，覆盖不到这 45 处里的绝大多数）**或者**一次全量 `Run()`，然后 `grep -c "guid: 00000000000000000000000000000000" WarpforgeVFX/Prefabs/*.prefab`，**目标 = 45 → 0**（外加那 4 个 `fileID: 0` 的本来就不该变成非 0）。
2. **`AnimationUtility.SetAnimationClips` 写出来的 `m_Animations` 形状**对不对（尤其是 `Pray_Idle` / `RemnantBody3D Necrons` 那两条 2 元素的）。
3. `ImportClip` 对 **33 条新片段**的落盘质量。**已有旁证**（不是猜）：① 同一函数给 `BoosterPackExporter` 落过 8 条 legacy clip，其中 `Booster Window Open.anim` = 991 行 / 10 条曲线 / 0 个 guid 0；② `Vanguard Frame Animation.anim` = 326 行 / 7 条 `m_FloatCurves`（`_state` classID 114 · `material._EmissionColor.*` classID 23 ×4 · `m_IsActive` classID 1 · `m_Enabled` classID 23），**逐条对得上** `WarpforgeVatDriver.cs` 头部记的那三族。

### 2. 「能不能在工程里找回那个 clip」的**两种读法**，本件把两种都答了

- **「工程里现在有没有那份 `.anim`」** ⇒ **0/34**（`Animations/` 只有 10 份，与这 34 个名字**零交集**）。
- **「原版包里那份片段找不找得回」** ⇒ **34/34**（33 在 `battleprefabs_vfxandmisc` + 1 在 `cardanimsgeneral`）。
⇒ **所以「能修的都修」= 把厂里的那一支补上**（重导即全好）；**在盘上手工把这 45 处改好是做不到的** —— 手工改要指向一份**不存在的 `.anim`**，那就成了「造一个空 clip 顶上」（⛔ 明令禁止）。**⛔ 我没有硬改任何 prefab。**

### 3. 没查的

- `WarpforgeBooster` 那 7 处（§⑦·1）—— **不在本件白名单**（`WarpforgeBooster/**` 不许动），只报不改。
- 原版包里 `CardPrefab` 那条 `m_Enabled: 0` 的 `Animation`（comp `7789851131101241589`）**原版是不是也关着** —— 没核（它两种状态下都不产生引用，本件不受影响）。
- `m_AnimationClipSettings` 在解包 JSON 里**不存在**这个键（`AnimationClip_8189764618720115800.json` 的键表里没有）⇒ `.anim` 的 `m_LoopTime: 0` 是 `Instantiate` 带过来的运行时值。**没查清**它从包里的哪个字段来（本件不需要它：不传 `loop` 就是「原样保留」，这正是我们要的）。

---

## ⑦ 顺手发现的

1. 🔴 **同一颗地雷在 `WarpforgeBooster` 里还有 7 处，而且那 7 处的目标 `.anim` 【工程里是有的】** ——
   `Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab` 有 **7 个** `--- !u!111`，`m_Animation` **全是 guid 全 0**：
   `:3334` `CardInBoosterPack UI 1` · `:7852` `… UI 2` · `:16848` `… UI 3` · `:24826` `Booster pack Background` · `:29978` `Booster Pack Open Window`（2 条）· `:33061` `… UI 5` · `:33248` `… UI 4`；引用的 fileID 是 `-7993935874865337289` / `8301228418192668656` / `-9033435933554767437` / `-1691631367813043119` / `-177604310092043705` / `5911697182262325120` / `-8254079481253810722`。
   **根因**：`BoosterPackExporter.Run()` 第 ①步（`:228-252`）**只把 8 条 clip 落成 `.anim`**，第 ③步导出窗口 prefab 时**没有任何一步把引用接回去**（`BoosterPackExporter.cs` 里 `grep Animation` 只有「clip 导入」那一支 + 一句注释）—— 与 A1095 **是同一个形状的漏**，只是宿主文件不同。
   ⚠️ 缓冲因素：该文件头写着那两个窗口 prefab「**我们运行时不用它**（参考/诊断用）」⇒ 影响面待判；**但引用确实是断的**。
   ⛔ 本件**白名单不含 `WarpforgeBooster/**` 与 `BoosterPackExporter.cs`** ⇒ **只报不改**，请落待办正本。
2. 🔴 **原版包里还有一件我们【压根没导出】的效果根：`Swarm Battle Card Icon`** ——
   包里它是**根 GameObject**（`m_Father = 0`），组件 = `Transform` + 一个 **MonoBehaviour**（`-220689081911957516`）+ **legacy `Animation`（`7363347156327180276`）→ clip `6836189287277267510` = `SwarmIconOn`**（在 `cardanimsgeneral` 包里）。
   而 `WarpforgeVFX/Prefabs/` 里**既没有这个名字、也没有任何 prefab 引到它**（`grep -rl "Swarm Battle Card Icon" Assets/` **零命中**）⇒ 这就是「包里 51 个 `Animation` 组件、工程里只有 50 个」**差的那一条**。
   ⇒ **它不是「引用变 guid 0」，是「这一件整个没进来」** —— 与 A1095 是两回事，**没修**（要修得先查清它该不该导、归哪条线）。
3. **`AnimationClip` 目录的文件名就是 pathID**（`AnimationClip_<pid>.json`）—— 这一条让「guid 全 0 的 `fileID` → 是哪条片段」**有确定答案**（不用猜）。同类事在 `Animation/`、`MonoBehaviour/` 也一样。⚠️ 但 **`GameObject/` 目录是按【名字】命名的**（`GameObject/Card Remnant.json`）⇒ 想按 pathID 找 GameObject 会**静默找不到**（我第一版脚本就栽在这，`go2comp` 得从 `m_Component` 反查）。
4. `.anim` 与 `.prefab` 的行尾**不一致**：`WarpforgeVFX/Prefabs/*.prefab` 与 `WarpforgeBooster/Prefabs/*.prefab` 都有**纯 LF** 的（`Booster Pack Open Window.prefab` = 0/33412 —— 那是 `SaveAsPrefabAsset` 的产物，Unity 写的）；改这类文件前照旧先二进制数。
5. 新那一支**也覆盖不到 `RunListed` 之外的东西** —— 它是挂在**唯一的** `Export()` 上（`PrefabUtility.SaveAsPrefabAsset` 全文件只有 `:1283` 一处）⇒ **没有第二个写 prefab 的出口**，这一点现读核过（免得以后有人以为还有别处要补）。

---

## ⑧ 摘要（300 字以内）

**A1095 已做**：`EffectExporter.cs` 在 `Animator` 那支（`:1194`）**正下方补了同一形状的 legacy `Animation` 一支**（新 `:1203-1268`）——
`GetComponentsInChildren<Animation>(true)` → `ImportClip` 逐条落盘 → `SetAnimationClips` + `an.clip` **接回引用** → 接不上就点名出声、**绝不造空 clip**；`Animator` 那支**一行未改**。净改动 **+67 行 / 1 个文件**，类型检查 **0/0**，CRLF 未翻（2593/2593）。

**45 处逐条普查**（40 件 prefab / 34 个不同片段）：**34/34 都能从原版包找回**（33 在主包 + `ScytheAssault` 在 `cardanimsgeneral`），但**工程里现在 0 份对应 `.anim`** ⇒「能修的都修」= **补厂里那一支**，**手工改盘上的 prefab 做不到**（要么造空 clip）。另 4 个 `m_Animation: {fileID: 0}` **原版就是空的**，不算缺陷。

**A1096① 选 (B)**：`WarpforgeVatDriver.cs` 文件头（`:13-50`）写死「8 个资产不在库里、clone 要重导」+ 逐资产字节/md5/mtime。**理由：(A) 不够** —— 两个 prefab 的 10/26 个 guid 里 **8/24 也 ignored**，放行 8 个 = 半个悬空 prefab。**没动 `.gitignore`**。

**另报**：`WarpforgeBooster` 同型地雷 **7 处**；原版 `Swarm Battle Card Icon` **整个没导出**（51 vs 50 差的就它）。
