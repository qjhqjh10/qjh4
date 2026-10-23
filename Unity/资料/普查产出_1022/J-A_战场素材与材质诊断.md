# J-A · 战场线「素材/材质一直没达到预期」诊断 + 战场素材导出备用方案

> **只读诊断（2026-10-22）**。⛔ 未改任何 `.cs` / 资产 / 现有 JSON / 正本；未跑 Unity；未动 git。
> 唯一写盘：本文件 + `_tmp_view/ja_diff_1022/`（`arena_diff.py` 的候选放大图，只读工具产物）。
> 用户原话（背景）：「尤其是战场线，之前我暂时不做就是可能是素材材质问题，一直没有达到预期，所以才停止的。」

---

## 〇、TL;DR（先看这 8 行）

1. **战场线的「几何 / 灯光 / 环境光 / 后处理 / 天空盒 / 贴图导入 / shader」其实已经搬完了**，而且我现核到一个反证：**13 场用到的 20 个原版 shader 名，在我们的 `wf_*.bundle` 里 20/20 全部存在**（无一个会静默退回占位材质）。
2. **「没达到预期」真正剩下的，是四层东西**：① 一条 13 场共有的**对比度压缩**（暗部偏亮 +7、亮部偏暗 −27，`A1630`，未定性）· ② **逐场的局部结构差**（今天现测 3 场，最差的仍是 `blacklegion` 1.074）· ③ **实时阴影整条链不跑**（`blacklegion` 那 1.074 的机制已定死）· ④ **动画/演出层几乎全空**（27 条 clip 只有 13 条相机 intro 有数据、5 个 AnimatorController 一个都没进工程、场景里 49 个 `Animation` 组件我们建的是**空组件**）。
3. **最容易被误当成「材质问题」的其实是第 ④ 层**：原版里会动的布景（炮台/泵/骷髅/抱脸虫孢子/雷尔贡）我们全是**静止的**，画面「死」，观感就是「素材不对」。这一层**不需要新材质**，需要的是 clip + controller。
4. **导出备用**：27 个 clip 对象（26 个不同名字）里**只有 `SporeArmature|LeviathanBigSpore` 是 addressable** ⇒ 其余 **26 个对象（25 个名字）** 全部要重打包；5 个 controller **全部不是 addressable** ⇒ 也要重打包；**场景树里的 15 个导出根（12 个 `Scenario` + astra/blacklegion 两个根级 `Baked` + leviathan 的 `Particle Effects`）也全部不是 addressable** ⇒ **这 15 个根全部要重打包**；另在 `battlesharedresources` 里有 **4 个同名的 `Battle Arena … Baked` 是 addressable**（与场包里嵌的**不是同一份，待核**）—— 它们是零重打包的 pilot 素材，不是那 15 个根。重打包的代价：**每个 `Scenario` 子树 = 482 个对象 + 17 类真外部引用 + 一份整 CAB 不裁剪**，远高于 clip/controller。
5. **推荐顺序**：先做**零风险的数据 dump**（22 条 legacy clip → JSON）→ 再做**clip + controller 小包**（体积小、价值最高）→ 再拿 **4 个 addressable 的 Baked prefab** 做一次导出管线验证 → 最后才一场一场重打包 `Scenario`。
6. 🔴 **重打包有一个已登记但未在 Unity 侧验证的形态**：arena 场景包的 `AssetBundle` 对象在 `.sharedAssets`、而 `Scenario` 根在主 CAB ⇒ `repack_tree` 会写**跨 CAB 容器引用**（`m_FileID ≠ 0`）。`资料` 自己写着「全库 13448 条 `m_Container` 的 fid 全是 0（本地无先例）⇒ Unity 侧仍待验」（`工具/extract_missing_shaders.py:1048` 注释 · `A230②/A231③`）。**这一条必须在 pilot 里先验**。
7. **`Assets/WarpforgeVFX/*` 是 gitignore 产物**（`.gitignore:26`，只放行 `Runtime/` 与 `Shaders/`），`StreamingAssets/WarpforgeVFX/`（`:33`）与 `Resources/ArenaPrefabs/`（`:129`）也是 ⇒ 导出物**换机就没了**，能进 git 的只有**数据（JSON）**。
8. **今天顺手量到的最大单点**：`darkangels` 蓝色发电机的辉光比原版强一大圈（`_tmp_view/ja_diff_1022/battlearenadarkangels_z01.png`）；`sororitas` 火盆的火焰发白、能看到 quad 硬边（`…sororitas_z01.png`）—— 这两条都是**材质/粒子**层，和既有账对得上。

---

## 一、「材质/素材没达到预期」的具体清单

### 1·1 全局渲染层（2 条，13 场共有）

| # | 症状 | 判据 | 状态 |
|---|---|---|---|
| **G1** | **对比度被压缩**：暗部我们偏亮、亮部我们偏暗 | 今天现跑 `arena_diff.py`（`_tmp_view/ja_diff_1022/`）三场全部打同一句：`blacklegion` 0-25 档 **+7.2** / 150-175 档 **−17.6**；`darkangels` **+7.3 / −0.8**；`sororitas` **+4.8 / −57.2**（175-200 档） | `A1630`。已排除：后处理（`WF_NOPOST=1` 形状不变）· HDR RT · 环境光/雾（真值逐项相同）· 贴图逐像素相同 · **shader 是同一份**。**未定性**：是尺子伪影（原版实拍=上屏、我们=渲到 LDR RT）还是真缺陷。正本 → `资料/待办判据_第三十三会话.md:2265` |
| **G2** | **恒定对位偏移 (dx=+0, dy=+2)**（640×360 尺度 ⇒ 1080p ≈ 6 px） | 今天三场**全部**报同一个 `(0,+2)`；与 2026-10-12 抽测的 `battlearena2`/`astramilitarum`/`leviathan` 一致 | `A1667`，**未查清**（预览取景 vs 原版相机微位）。⚠️ 它让所有逐像素统计都带一层系统误差 |

> 📌 **口径提醒**：`arena_diff.py` 的颜色统计是「按原版亮度分箱」，`G1` 的「压缩」在 **rank 分位匹配之前**；结构候选是**匹配之后**的，两者别混读（工具自己就打这句）。

### 1·2 逐场结构差（今天现测 3 场 + 既有结论）

| 场 | 亮度比（§三 表） | 今天平均差 / 团数 | 今天最靠前的候选（中心 · 面积 · 强度 · 方向） |
|---|---|---|---|
| `blacklegion` | **1.074**（13 场唯一还在 ±5% 外） | 4.1/255 · 8 团 | #1 (0.42,0.14) 17428px 9.9 **偏暗**（上半/背景）· #2 (0.11,0.44) 3802px 12.7 偏亮 · #3 (0.87,0.45) 2670px 15.3 偏亮 · #4 (0.69,0.39) 3340px 12.1 偏亮 |
| `darkangels` | 1.036~1.038 | 3.8/255 · 9 团 | #1 **(0.05,0.25) 2571px 19.8 偏亮** = 左侧蓝罐子/发电机辉光（已亲读放大图：我们那圈光晕明显更大更亮）· #3 (0.14,0.15) 826px 22.7 偏暗 |
| `sororitas` | 1.028 | 3.8/255 · **13 团** | #1 **(0.06,0.40) 1175px 31.8 偏暗** = 左下火盆（亲读：原版橙黄火苗充满盆口，我们发白、稀、还能看到 quad 硬边）· #2 (0.95,0.42) 861px 27.4 偏暗 = 右下火盆（同族） |
| `battlearena1` | 0.998 | 4.3/255 · 10 团（4 相位稳健） | 三条**已定性**的结构差：① 左下管道口蒸汽（964px 强度 53.5 我们偏亮，**硬直水平切边 = 软粒子淡出没生效**，2 分钟 A/B 开关 `WF_NOSOFT=1` 已备好没跑）② 火区暖光我们偏亮（1361px 30.8）③ 天空雾带我们偏暗（610px 39.2）→ `资料/战场场景线_交接.md:64` |

> ⚠️ 今天这三场都只有 **1 个相位**（工具自己警告：烟雾/蒸汽候选可能是相位噪声，账 `A1615`）。所以上面的候选里，**只有「非粒子」的那些**（蓝罐子网格/辉光、火盆火焰的核心、背景暗部）可以直接当结构差。

### 1·3 材质 / shader / 粒子层（**这一层最容易被误判成「全都不对」，其实大部分是对的**）

**✅ 现核「不是缺口」的反证（别再重查）**：

| 项 | 结论 | 判据 |
|---|---|---|
| **原版 shader 覆盖** | **20/20 全部在** `wf_shaders` / `wf_shaders_extra` / `wf_arena_*` 里（`UnlitAmbient 521` · `Unlit Wind 56` · `URP/Unlit 26` · `UnlitAmbient Emissive Flickker 24` · `Pulsating Mesh 23` · `URP Transparent Shadow Receiver 13` · `3D Card Stealth 13` · `Unlit UV scroll 13` · `Unlit shadows receiver 12` · `Tyranid Tentacle 9` · `Tau Generator Energy 4` · `Matcap With Texture 4` · `Floor Planar Reflections Grainny 3` · `Vortex 2` · `Burning 2` · `Simple Fake Water 1` · `Alpha Masks Two Layer 1` · 以及 tauviorla 两条专属与 `Hidden/LUTBlender`） | 我用 UnityPy 逐个读 `StreamingAssets/WarpforgeVFX/wf_*.bundle` 的 `Shader.m_ParsedForm.m_Name` 与清单 `meshes[].shader` 对表 |
| **光照贴图** | **原版 13 场 `MeshRenderer` 的 `m_LightmapIndex` 全是 `0xFFFF`**（无烘焙）⇒ 我们没导不是缺口 | 直读 `assets_full/bundle_scenes_scenes_battlearena*/MeshRenderer/*.json`，13 场逐个统计 |
| **贴图色彩空间** | 原版战场 `Texture2D.m_ColorSpace` **全是 1（sRGB）**，Unity Default 导入也是 sRGB ⇒ 没有 sRGB/线性错配 | UnityPy 读 arena1 / astramilitarum 包的 Texture2D |
| **`_AmbientColorBlend`** | 13 场全是 **0** ⇒ 那层 tint 是恒等，`_APPLYAMBIENTCOLOR` 不可能造成 G1 | `资料/战场13场_逐场对账_0920.md:1142-1145` + `CardPresentation/Battle/ArenaSceneState.cs:30,71` |
| **`CandleFlame_*` 网格** | 原版资产本身只有裸位置（`m_IndexBuffer = 0`）⇒ 画不出三角形；火苗是粒子画的 ⇒ 我们不缺东西 | `资料/普查产出_0920/场景光照与后处理_原版规格.md:556-583` |
| **`ScenarioMaterialFader` / `ScenarioParticleSpawnerBlender` / `FilterOptions`** | 都已收口（A135 / A136 / A137 / A191） | `资料/战场场景线_交接.md:686-693` |

**⏳ 真正还开着的材质/粒子条目（8 条）**：

| # | 条目 | 一句话 | 出处 |
|---|---|---|---|
| **M1** | **实时阴影整条链不跑** | 13 场里 25 个网格用的 shader 就是「收实时阴影」的两族（`Unlit shadows receiver` 12 + `URP Transparent Shadow Receiver` 13）；`blacklegion` 52/67 个网格原版 `cast+receive` 都为 1，**旁挂已逐对象对上、画面纹丝不动** ⇒ 卡在渲染链/平台，**归真 Play（E9）**。这是 `blacklegion` 1.074 的机制，也是「材质看着不对」的一大来源 | `资料/战场13场_逐场对账_0920.md:910-972` |
| **M2** | **`darkangels` 蓝罐子「叠着亮」** | Glow Additive 粒子 ≈8.6 + 两个 Generator 网格 ≈7.1 + bloom ≈2.8 + 自发光 ≈2.3。**三个子项都是「没查」** | `:974-1004`；今天放大图复核成立 |
| **M3** | **`battlearena3` 4 个 `LightShaft` 黑块** | 那 4 个**网格**用的 shader 是 `Universal Render Pipeline/Particles/Unlit`；原版是亮的、我们是暗的（现值 1.023）。与顶点色无关 | `资料/战场场景线_交接.md` §五 #2；清单现读 4 条 `LightShaft` 全是这个 shader |
| **M4** | **14 颗「材质本来就没有贴图」的粒子没建** | 扭曲/叠加辉光族（`Heat Distortion` / `Muzzle Flash view distort` / `Necrons Close Monolith Rays` / `Lance Fire (5)` / `Embers`…），硬建会渲成白方块 ⇒ 我们选择**不建 + 出声**。要还原必须给它们接原版 shader | `资料/普查产出_0920/场景光照与后处理_原版规格.md:518-551`；我现读清单 `texFile` 为空的 = **14 颗**（578 总数） |
| **M5** | **109/578 颗粒子被四道闸门挡掉** | 现读：`active=false` **80** · `renderMode==5` **24** · `texFile` 空 **14**（有重叠）；`tauviorla` 一场就 67 颗 inactive。**「这 80 颗是否都合法」没逐颗核过**（`A1580②`） | 我按 13 份 `<场>_manifest.json` 现算；账 `A1580` |
| **M6** | **`maxParticleSize` 语义没吃透** | §一 #3 写「故意不设」，但 `darkangels` 实测**设了就变好**（+18.11→+15.7x）⇒ 结论是「逐场/逐颗看」，不能一刀切 | `:996-1004`、`:1028-1033` |
| **M7** | **3 个环境 prefab 偏亮** | 台账 `亮度比`：`…Dark Angels Asteroid Zone` **1.24**（\|ln\| 0.219）· `…Dark Angels Void Combat` **1.16**（0.147）· `…Emperor's Children 2 Fumes` **1.24**（0.216）。**其余 24 条战场相关效果全是「对得上」**（另有 2 条「两边全程空」= 纯脚本驱动） | `资料/特效还原台账.tsv`（29 条战场相关）；兜底审计 `资料/比对基线/兜底路审计_1002j.tsv:13,14,33` |
| **M8** | **M7 前两条已被证明「用自建反而更准」** | 2026-10-21 受控 A/B：Asteroid Zone `\|ln\| 0.242 → 0.013`、Void Combat `0.103 → 0.004` ⇒ **偏差不在自建兜底路，而在「走原件」那一侧**。待办 = 评估把这两个名字从 `UseOriginal` 移除 | `资料/任务线/特效线/任务总表.md:21`（T02） |

> **M7/M8 的绝对值**：这两条是**环境 prefab**（进攻卡切 4 环境态时出场），不是 13 场的默认态；但它们直接决定「4 环境」那 52 个态里的一部分观感。

### 1·4 动画 / 演出层（**这是「素材没达到预期」里最大的一块，而且完全没被导出**）

| # | 条目 | 现读事实 | 影响 |
|---|---|---|---|
| **A1** | **27 条战场 clip（26 个不同名字）只有 13 条相机 intro 有数据** | `数据/游戏数据/camera_intro_clips.json` 逐键搬了 13 场相机 intro（含 `spacewolves` 那条**名字也叫 `Battle Arena Emperors Children Camera Intro`** 的重名件）；**其余 14 条一条都没有资产、也没有数据** | 炮台/泵/骷髅/孢子/场景装饰的演出全部缺失 |
| **A2** | **5 个 AnimatorController 一个都没进工程，而且我们连 `Animator` 组件都不建** | 我 grep `ArenaBuilder.cs`：**`Animator` 零命中**；清单里也没有 controller 字段。原始内容：`Cannon 1` · `Pump 1` · `Dynamic Skulls` · `Battle Arena 2`（都在各场包）· `Battle Arena Leviathan`（在 battleshared） | 这 5 个对象在我们的战场里**一帧都不动** |
| **A3** | **49 个原版 `Animation` 组件，我们建的是空组件** | 原版 13 场 `Animation/` 目录逐个计：3/3/4/3/4/4/5/4/4/3/3/3/6 = **49**；`ArenaBuilder.cs:3072-3073,3111-3112,3130-3131` 只 `AddComponent<Animation>()`，**`clip` 一个都没赋**（`grep ".clip = "` 零命中） | 雷尔贡炮塔、泵、骷髅、孢子、darkangels 的炮台全部静止 |
| **A4** | **只有 2 条缺口已经有「数据 + 运行时」两条腿** | `ScenarioBlendables.cs` 通过 `WarpforgeAnimatorBridge.TryGetClip` 从 `wf_prefabs_extra.bundle` 取 `Dark Angels Void Combat animations` 与 `LightAnimationOrbit`（按 assetGUID） | 证明「重打包 + 运行时取原件」这条路**已经在跑**，可直接复用到 A1/A2 |

> **这四条合起来的判断**：用户说的「素材材质问题」里，**A1–A3 是最扎眼的** —— 静态截图能对到 4~6 的亮度比，但一进游戏，原版里满场都在动的机械/火焰/孢子，我们全是定住的。**这部分缺的不是材质，是 clip 与 controller**。

---

## 二、导出备用：施工方案（⛔ 本节只是方案，没有动手）

### 2·1 事实基础（全部现读，可复查）

**A. 三类资产在哪个包、是不是 addressable**

| 类 | 数量 | 在哪个包 | 是否 addressable（以 **AssetBundle 对象所在的那份 CAB** 的 `m_Container` 为准） |
|---|---|---|---|
| **AnimationClip** | **27 个对象 / 26 个不同名字** | 25 个各在自己的 `scenes_scenes_battlearena*.bundle`；`Battle Arena 3 Animations` / `SporeArmature\|LeviathanBigSpore` 在 `battlesharedresources_assets_all.bundle` | **只有 1 条 addressable**：`SporeArmature\|LeviathanBigSpore`。**其余 26 个对象（25 个名字）全部不是** |
| **AnimatorController** | **5** | 4 个在各自场包（`Cannon 1`/`Pump 1`/`Dynamic Skulls`/`Battle Arena 2`）+ 1 个在 battleshared（`Battle Arena Leviathan`） | **0/5 addressable** |
| **Prefab（场景树里的 15 个导出根）** | **15** | 见下 B | **0/15 addressable**（我扫了 84 个源包**AssetBundle 对象所在 CAB** 的 `m_Container`）。⚠️ 另有 **4 个同名的 `Battle Arena … Baked`**（3 / Aeldari / Leviathan / Adepta Sororitas）在 `battlesharedresources` 里**是 addressable** —— 但它们在**自己的包里是独立根**，与场包里嵌的那 4 个同名子树**不保证是同一份**（见 §四 ④） |

**B. 15 个导出根的真实形状（我按场景树现核，与 A2 §1·2 X4 的名单有出入 → 见 §四 更正）**

| 场 | 场景根 | 世界内容根（AssetRipper 导出实测） | 备注 |
|---|---|---|---|
| arena1 / 2 / 3 / aeldari / darkangels / genestealers / sororitas / spacewolves / tauviorla | `Scenario` | **`Scenario` 的子件**（`… Baked` / `Scenario Fillers`；arena1/2 的 Baked 名字带 `Scenario` 前缀 + 尾随空格） | 直接导出 `Scenario` 整棵 |
| astramilitarum / blacklegion | `Scenario`（只有灯光/粒子） | **另一个独立根** `Battle Arena … Baked`（84 / 70 个子件） | 要导**两个根**（blacklegion 有 4 条跨根引用，必须合并） |
| emperorschildren | `Scenario`（21 节点） | **独立根** `Battle Arena Emperor's Children Baked`（78 子件） | ⚠️ **2026-10-23 更正：原来写「`Scenario` 的子件」是错的**，导出实测为独立根（`m_Father: {fileID:0}`） |
| **leviathan** | **有 `Scenario` 根**（154 节点） | `Particle Effects`（22 子件）是 **`Scenario` 的子件**；`Battle Arena Leviathan Baked`（99 节点）在 `Scenario` 下 | ⚠️ **2026-10-23 更正：原来写「没有 `Scenario` 根」「PSR 根 = `Particle Effects`」是错的**，导出实测 `Scenario` 存在且子树 154 节点 |

⇒ **导出根（AssetRipper 导出实测）= 16 颗 = 13 × `Scenario` + 3 颗独立 `Baked`（astra / blacklegion / EC）**。
⚠️ **2026-10-23 更正**：原来写「12 × `Scenario` + astra/blacklegion Baked + leviathan 的 `Particle Effects` = 15」——三处与导出不符（leviathan 有 `Scenario`、EC 的 Baked 是独立根、`Particle Effects` 是子件）。若要「一颗 prefab = 一场完整世界」，按 13 颗合并导出（3 个 Baked 并回 `Scenario`，根变换均为单位阵）。
判据全文 → `资料/普查产出_1023b/战场线_15prefab清单.md` §一/§二/§六。
这与 A2 §1·2 那张表「12 个 `Scenario` + 2 个 `Baked`（astra / blacklegion）= 14」**一致**（它漏了 leviathan 那 1 颗）；
而 A2 §1·2 **X4 行写的「`Battle Arena 3 Baked`」是错的** —— 我按 Transform 父链现核，它是 **arena3 `Scenario` 的子件**，不是根（见 §四 ③）。
⚠️ **另**：每场还有 4 颗 `Energy Accumulation VFX Off/On` 也是 PSR 根（战斗 HUD 的能量累积特效，属 HUD 不属布景），A2 与本表都**不计入**这 15。

**C. 重打包的硬事实（我用 `repack_tree(..., dry_run=True)` 对 `arena3/Scenario` 实跑过一趟，只体检不写盘）**

```
[P1] 1 个根在【别的 CAB】里 ⇒ 走跨 CAB 引用（m_FileID=22，新补 externals[21]）
     写的那一份 = CAB-5a65…sharedAssets（63 个对象，装着 AssetBundle 对象）
     根 `Scenario` 在主 CAB（5304 个对象）
[P3] 依赖树：482 个对象（GameObject×123 · Transform×123 · MeshFilter×48 · MeshRenderer×46
     · ParticleSystemRenderer×46 · ParticleSystem×46 · MonoBehaviour×27 · MeshCollider×10
     · LookAtConstraint×4 · SpriteMask×4 · BoxCollider×2 · Light×1 · Camera×1 · Animation×1）
[P3] 真外部引用 17 类：MeshFilter/MeshRenderer → battlesharedresources；
     ParticleSystem(-Renderer) → 另两个 CAB；MonoBehaviour ×27 → 另三个 CAB；Animation ×2 → battlesharedresources
[P3] 那份主 CAB 整组不裁剪保留（含 .resource）
```

3 个推论，直接决定方案：
1. **Prefab 导出不是「拷一个 prefab」** —— 它会把 482 个对象 + 一整份 CAB 拖进来，而且 `Animaion` 组件引用的 clip 在**别的包**（battlesharedresources），不一起重打包就是空引用。
2. **`Scenario` 的 482 个对象里没有 `Animator`**（只有 1 个 `Animation`）⇒ 5 个 controller 挂在**别处**（Baked 子树 / `BattleBoardElements`），所以「导 prefab 顺带把控制器带出来」这条路不成立，controller 必须单列。
3. **跨 CAB 容器（`m_FileID≠0`）在 Unity 侧没有先例、未验证** ⇒ 这是整条导出链的**唯一硬风险**，必须先做 pilot。

### 2·2 三种资产各走哪条路

#### 路线 ①：AnimationClip —— **两条腿，一条零风险、一条要 pilot**

| 子类 | 条数 | 做法 | 产物 |
|---|---|---|---|
| **legacy 曲线**（`m_Legacy=true`、`m_MuscleClipSize=0`） | **22 个对象 / 21 个名字**（13 相机 intro + 5 `Scene Animation(s)` + `Battle Arena 2/3 Animations` + `Railgun BIG`/`Turret 1`/`Turret 2`） | **零 Unity 的 python dump**：照 `工具/gen_camera_intro_clips.py` 的 schema（`t/v/si/so` 逐键 + `m_Path` + `sampleRate` + `wrapMode`），产出 `数据/游戏数据/arena_scene_clips.json` | **可进 git**；行为可重建、可对拍 |
| **muscle / streamed**（`m_MuscleClipSize > 0`） | **5 条**：`Cannon front Idle`(3308) · `Battle Arena 2 Animations`(3624) · `Dynamic Skulls`(6568) · `Pump Animation`(12192) · `SporeArmature\|LeviathanBigSpore`(30380) | **只能重打包**（`W64` A1 已证明：muscle clip 落不成 `.anim`，`CreateAsset` 出来是空壳、`length=1`） | `StreamingAssets/WarpforgeVFX/wf_arena_anims.bundle`（gitignore，可重建） |

> 顺带一条**已有的现成范例**：`Data/游戏数据/battlehud_clips.json` 就是这种「逐键 dump」，8 条全是 legacy（`工具/gen_battlehud_clips.py`）。

#### 路线 ②：AnimatorController —— **只能重打包**

- 数据侧已经齐了：`数据/游戏数据/animator_controllers.json` 现读 **count = 6**，**5 个战场控制器全在**（`Battle Arena Leviathan` / `Cannon 1` / `Pump 1` / `Dynamic Skulls` / `Battle Arena 2`），字段逐条可照着建（`工具/gen_animator_controllers.py`）。
- **资产侧**：`WarpforgeAnimatorBridge` 是现成的运行时取件口（`BundleRelPath = WarpforgeVFX/wf_prefabs_extra.bundle`，`TryGet(name)` / `TryGetClip(alias)`）。
- **做法**：把 5 个 controller 用 `repack_tree(..., extra_controllers=[...])` 打进**同一个新包**；建议**扩 `BundleRelPath` 成「主包 + 扩展包列表」或复用一个 `wf_arena_anims.bundle`**（⛔ 不要塞进 `wf_prefabs_extra` —— 那只是 `battleprefabs` 源包的产物，两个源包混在一个产物里会把 `--prefabs` 的重建链搅乱）。

#### 路线 ③：15 个 prefab —— **最贵、风险最高，放最后**

| 子类 | 件数 | 做法 | 备注 |
|---|---|---|---|
| **（pilot，不计入 15）addressable 的 4 个 Baked** | 4 | **零重打包**：`EffectExporter.RunListed` 里按名字加 4 行（本质与 `Swarm Battle Card Icon`/`M4` 同款：在容器里就走本表）→ 产 `.prefab` + 材质/网格/贴图 | ⚠️ 它们与场包里嵌的那 4 个**同名不同源**（待核）。价值 = **先验证「arena prefab 能不能导出成工程资产」**，代价最低 |
| **场景树里的 15 个导出根**（12 × `Scenario` + astra/blacklegion 两个根级 `Baked` + leviathan 的 `Particle Effects`） | 15 | **只能重打包**（`repack_tree`）→ `wf_arena_prefabs_<场>.bundle`；**一场一场做**（⛔ 别 `--all`，铁律 15） | **arena3 实测 482 个对象**（其余场没跑 dry-run，别按 482 死套）；产物会因「整份 CAB 不裁剪」明显变大 |

- **命名冲突**：13 个根里有 **12 个都叫 `Scenario`**、3 个叫 `Scenario Fillers` ⇒ 导出时**必须加场名前缀**（如 `battlearena3__Scenario.prefab`）。工程里已有确定的撞名守卫（`EffectExporter.ReportVariantPaths` / `ResolveVariantPath`，`EffectExporter.cs:200-233, 70-71`），但那是给**材质/网格**用的；prefab 这一层要自己保证唯一名。
- **⚠️ 名字撞车还有一个更隐蔽的**：`Battle Arena 3 Baked` / `Aeldari Baked` / `Leviathan Baked` / `Adepta Sororitas Baked` 这 4 个名字在 **`battlesharedresources` 里有一份 addressable 的**、在**各自场包里还有一份嵌在场景里的**。`RunListed` 会把 `BundleDir` 下**全部**源包 `LoadFromFile`（`EffectExporter.cs:548-560`）⇒ 按名字 `LoadAsset<GameObject>(name)` 拿到的**可能是 battleshared 那份**。导之前要先确认两份是不是同一棵树（**我没查**，见 §四）。

### 2·3 导到哪个目录、谁来重建、换机怎么办

| 产物 | 目录 | 是否进 git | 谁重建 | 换机怎么做 |
|---|---|---|---|---|
| **clip 曲线数据** | `Unity/数据/游戏数据/arena_scene_clips.json`（+ 运行期副本如需要） | ✅ **进 git** | `工具/gen_arena_scene_clips.py`（待写，照 `gen_camera_intro_clips.py`） | 不用做，git 里有 |
| **prefab 清单/树形状数据** | `Unity/数据/游戏数据/arena_original_prefabs.json`（根名 · 源包 · pathID · 父链 · 子件数 · 哪些节点带 `Animation`/`Animator`） | ✅ **进 git** | `工具/gen_arena_original_prefabs.py`（待写） | 不用做 |
| **重打包的 clip/controller 小包** | `Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_arena_anims.bundle` | ❌ gitignore（`.gitignore:33`） | **`工具/extract_missing_shaders.py --arena-extras`**（新加一个 `ARENA_GROUPS`，模板 = `MAINMENU_GROUPS`，`extract_missing_shaders.py:293-306`） | 新克隆/换机跑一次那条命令；**上游源包在 `d:/2/.../StandaloneWindows64/`，不在 git 里** ⇒ 还得有那份原版安装 |
| **重打包的 prefab 包** | `…/StreamingAssets/WarpforgeVFX/wf_arena_prefabs_<场>.bundle`（建议**按场分包**，便于逐场验收/回滚） | ❌ gitignore | 同上（`--arena-prefabs --arena <场>`） | 同上 |
| **导出成工程资产的 prefab/.anim/.mat** | `Unity/MyGame/Assets/WarpforgeVFX/{Prefabs,Animations,Materials,…}` | ❌ gitignore（`.gitignore:26`，只放行 `Runtime/`、`Shaders/`） | `EffectExporter.RunListed()`（Unity 批处理，**主对话串行跑**） | 重跑 `RunListed`；⚠️ `EffectExporter.ClearGenerated()` 会把它们一起删掉 |

> 🔴 **一句必须写进工具头注的话**：这批产物**全部可重建**、**全部在 gitignore 里** ⇒ 「导出备用」的意思是「**留下了可重建的工具 + 可进 git 的数据**」，不是「把二进制塞进仓库」。想真·离线留档，只能另建一个不在 `d:/4` 的归档目录（本次**没做**，需用户拍板）。

### 2·4 与我们自己的 arena 生成器会不会冲突

| 维度 | 结论 |
|---|---|
| **同名** | ⚠️ **会**。我们生成的战场 prefab 是 `Assets/Resources/ArenaPrefabs/<场>.prefab`（13 件，**gitignore**，`:129`），名字是**场名**；原版根叫 `Scenario` / `Battle Arena … Baked`。⇒ 只要**导到别的目录 + 加场名前缀**就不撞。⛔ **绝不能导进 `Resources/ArenaPrefabs/`** —— 那是 `ArenaBuilder.BuildArenaPrefabs`（`ArenaBuilder.cs:550-564`）的产物目录，重跑就覆盖。 |
| **同 guid** | ❌ 不会。原版 prefab 是从 bundle 里**新建**的资产（`EffectExporter` 走 `SaveAsPrefabAsset`），guid 由 Unity 新发；我们的是每次重建（`AssetDatabase` 原地覆盖 / 删了重发，`CLAUDE.md` §三 那条「生成资产别先删光」的坑）。**两套 guid 天然不同**。 |
| **同数据源** | ✅ 本来就是同一份：我们的 `gen_unity_arena_manifest.py` 与 `gen_arena_groups.py` 都是直读同一个场景包。导出原版 prefab **不会**改变 manifest / groups 的任何字段 ⇒ **对我们的生成链零影响**（只要不删不改它们）。 |
| **构建顺序** | 导出物**不参与** `BuildAll` / `BuildArenaPrefabs` / `BattleScene.BuildAndSaveScene` 这条链 ⇒ 不改变我们战场的产物。**它是纯旁挂**。 |

### 2·5 分批顺序（判据写死）

| 批 | 做什么 | 判据（做完怎么算过） | 需不需要 Unity |
|---|---|---|---|
| **第 1 批** | 22 条 legacy clip → `arena_scene_clips.json`；15 个根的树形状 → `arena_original_prefabs.json` | ① 每条 clip 的 `m_FloatCurves/m_RotationCurves` 键数与原 JSON **逐条相等**；② 跑两遍 md5 相同（幂等）；③ 根清单与我从场景树现算的 15 个根**逐个对上** | ❌ 不需要（纯 python，只读 `assets_full`） |
| **第 2 批** | 5 条 muscle clip + 5 个 controller → `wf_arena_anims.bundle`（`repack_tree` 的 `extra_clips`/`extra_controllers`） | ① `_write_bundle` 报的对象数 = 5 clip + 5 controller + 依赖；② **Unity pilot**：`AssetBundle.LoadFromFile` 后 `LoadAsset<AnimationClip>("Pump Animation")` **非 null**、`length` 与原版一致（`0.8167` 那种量级）；③ `LoadAsset<RuntimeAnimatorController>("Battle Arena Leviathan")` 非 null、`GetComponent` 能播 | ✅ 只要一次 pilot（可放进任意一次已有批处理里，不额外开实例） |
| **第 3 批** | 4 个 addressable Baked prefab → `RunListed` 导出成 `.prefab` | ① 4 件都有 `.prefab` 且子树节点数 = 原版（42/37/44/79）；② 材质/网格/贴图落在 `Assets/WarpforgeVFX/`；③ **贴图不是品红、材质不是 `InternalErrorShader`** | ✅ 一次 Unity 批处理 |
| **第 4 批** | 15 个场景根，**一场一场**重打包（**先 `battlearena3`** 5.4 MB 最小、再 `sororitas` 2.1 / `spacewolves` 2.2） | 每场：① 打出来的包能 `LoadAsset<GameObject>("Scenario")`；② 实例化后 482 级对象数对得上；③ 依赖没丢（`m_Mesh`/`m_Material` 非 null）；④ **`m_FileID≠0` 的容器引用 Unity 认不认**（A230 未验点）；⑤ 出图与原版并排看**没有变差** | ✅ 每场一次 |

**为什么这个顺序**：第 1 批零风险、立刻把「其余场景动画没有资产」这句话至少变成「有数据」；第 2 批是**唯一真正非重打包不可**的一类，而且体积极小；第 3 批用最便宜的 4 件验证「arena prefab → 工程资产」这条管线；第 4 批才是重活，且必须一场一场验（铁律 15）。

### 2·6 风险清单（照实写）

| # | 风险 | 现状 | 降风险的办法 |
|---|---|---|---|
| R1 | **跨 CAB 容器引用（`m_Content` 里 `m_FileID ≠ 0`）在 Unity 侧没先例** | 工具注释自陈「全库 13448 条 fid 全是 0 ⇒ Unity 侧仍待验（A230②/A231③ 没销）」`extract_missing_shaders.py:1050-1056` | pilot（第 2/4 批）；若不认，改走「把 `AssetBundle` 对象搬进主 CAB」的单 CAB 写法 —— `工具/extract_mirror_shaders.py` 已有「搬对象 + 改 `type_id` + 发新 `path_id`」的成功先例（`资料/战场13场_逐场对账_0920.md` §一 ② 那段） |
| R2 | **重打包产物会「整份 CAB 不裁剪」** ⇒ 体积可能远大于源包 | `[P3]` 现读：主 CAB 整组保留 | 按场分包；先跑 dry-run 拿对象数，再决定要不要裁剪 |
| R3 | **prefab 的 17 类真外部引用要靠「其它源包同时加载」** | `[P3]` 现读 | **把被引用的 clip 一起 `extra_clips` 收进同一个包**（同文件内解析，不跨包） |
| R4 | **名字撞车**（4 个 Baked 在 battleshared 与场包里各有一份；12 个 `Scenario` 同名） | 现读 | 导出名加场名前缀；按 guid/pathID 取而不是按名字（必要时先核对两份是否同树） |
| R5 | **`.anim` 对 muscle clip 无解** | W64 A1 已证 | 走运行时原件（`WarpforgeAnimatorBridge`），⛔ 别再试 `CreateAsset` |

---

## 三、最该先做的 3 条

1. **先把 22 条 legacy clip 逐键 dump 成 JSON**（`数据/游戏数据/arena_scene_clips.json`）。
   - 判据已经齐全（`legacy=true` / `muscle=0` / 曲线键数逐条可数），**不跑 Unity、不碰资产、零风险**；直接补上「其余场景动画连数据都没有」这个洞的一半。
   - 同批顺手产出 `arena_original_prefabs.json`（15 个根的树形状 + 谁带 `Animation`/`Animator`）—— 这是第 2/4 批的**输入清单**。
2. **把 5 条 muscle clip + 5 个 controller 打成一个 `wf_arena_anims.bundle`，并做一次 Unity pilot。**
   - 这是**唯一非重打包不可**的一类资产，体积最小；同时它能一次性验证 R1（跨 CAB 容器）与 `WarpforgeAnimatorBridge` 这条取件链。
   - 做完之后，A2/A3 那一层（5 个会动的布景）就只差「把 controller 挂回对象」这一步。
3. **用 4 个 addressable 的 `Battle Arena … Baked` 做一次 `RunListed` 导出，验证「arena prefab → 工程资产」这条管线。**
   - 零重打包（它们在 `battlesharedresources` 的容器里），拿到的就是「原版子树 → `.prefab` + 材质/网格/贴图」的端到端结果；它过不了，第 4 批（15 个场景根）就不用开。
   - ⚠️ 它们**不在**那 15 个导出根里（是另一份同名子树），所以这一步只验管线、不交付那 15 个。
   - ⚠️ 做之前先答一个 5 分钟的问题：这 4 份与场包里嵌的那 4 份**是不是同一棵树**（同名不同源的风险，见 §四 ④）。

> 额外一条**顺手可做**的画面改善（不属导出、属 M7/M8）：`Environmental Condition Dark Angels Asteroid Zone` 与 `… Void Combat` **从 `UseOriginal` 白名单里移除**（T02 已实测自建 |ln| 0.242→0.013 / 0.103→0.004），改完各渲一次对拍即可。⚠️ 它属**特效线**的账，改之前先由调度台判归属。

---

## 四、没查清（⛔ 不许当结论）

| # | 项 | 差什么 |
|---|---|---|
| ① | **`A1630` 到底是尺子伪影还是真缺陷** | 现有证据互相矛盾（`aeldari` 天空区差 ≤4 ⇒ 不是全局；但 7 场逐场都同一形状）。要判得**拿我们自己 player 的真机截图**与原版实拍比同一块（同管线类别）。本次没做（不跑 Unity） |
| ② | **`A1667` 的 `(0,+2)` 从哪来** | 是预览取景差 0.3%，还是原版实拍那一帧相机本来在某个微位；**13 张实拍是否共享同一相位**没量 |
| ③ | **A2 §1·2 X4 的名单有一处错（我已核到能定案）** | A2 写「3 个 Baked = `Battle Arena 3 Baked` / Astra / Black Legion」。我按 **Transform 父链 + 子树 PSR** 现算 13 场：`Battle Arena 3 Baked` 是 **arena3 `Scenario` 的子件**（不是根）、`Emperor's Children Baked` 也是 `Scenario` 的子件；**根级 Baked 只有 astra 与 blacklegion 两个**，第 15 颗是 **leviathan 的 `Particle Effects`**。⇒ 15 这个**数**对，**名字组成**要按本表 §2·1 B 更正。**倒计时/动工前再核一次这条**（我用的判据是 `m_Father.m_PathID==0` + 子树 `ParticleSystemRenderer`） |
| ④ | **4 个 Baked 的「battleshared 版」与「场包内嵌版」是否同一棵树** | 只确认了同名、且 battleshared 版的子件数（42/37/44/79）；**没逐节点比**。若不同，`RunListed` 按名字取可能取错 |
| ⑤ | **109 颗被闸门挡掉的粒子是否都合法** | `A1580②` 原文就写「这 80 颗是否都合法没逐颗核」。本次只统计了闸门原因，**没有逐颗回原版看它是不是真关着** |
| ⑥ | **`battlearena3` 4 个 `LightShaft` 黑块的真因** | 只知道它们用的是 `URP/Particles/Unlit`、与顶点色无关；**没查**（属 M3） |
| ⑦ | **5 条 muscle clip 播起来到底是什么效果** | 只有 `m_MuscleClipSize` 与名字；**没解肌肉曲线**（那是运行时格式，本地能播不能读）⇒ 只能等重打包后在 Unity 里看 |
| ⑧ | **跨 CAB 容器 Unity 认不认** | 见 R1。**本次没跑 Unity**（可能正有批处理在跑），所以这条必须在 pilot 里验 |
| ⑨ | **「材质没达到预期」里有多少是 G1（对比度）造成的错觉** | G1 是**逐场同形**的 ⇒ 它可能把「其实已经对上的材质」整体压成「看着不对」。**要把它和逐场结构差分开验收**（rank 分位匹配那一列才是结构差） |

---

## 五、本次搜过什么（报「没有」之前先看这一节）

- **文档**：`资料/战场场景线_交接.md`（全 804 行里 grep 定位 + 读 §一/§一·b/§二/§五）· `资料/战场13场_逐场对账_0920.md`（grep 定位 + 读 §〇/§一 ①-a/①-l/①-m/①-n/§二 场景材质坑/§二之二/§三）· `资料/战场还原度_差距清单_0917.md`（全文 97 行）· `资料/普查产出_0920/场景光照与后处理_原版规格.md` §十一/§十二 · `资料/待办判据_第三十三会话.md` 的 `A1630`/`A1580` · `资料/任务线/特效线/任务总表.md`（T02/W65 行）· `资料/普查产出_1022/A2_素材未导出与未使用.md` · `资料/普查产出_1022/W64_素材导出缺口.md`（全文 139 行）· `资料/交叉会话` 未查（与本件无关）。
- **数据**：`资料/特效还原台账.tsv`（961 行，按 `Environmental Condition`/`Scenario`/`Arena` 筛出 29 条）· `资料/比对基线/兜底路审计_1002j.tsv` · 13 份 `<场>_manifest.json` · 13 份 `<场>_groups.json` · `数据/索引/anim_address_map.json`（`guid_to_asset` 10,539 条，搜 `Baked` / `Scenario`）· `数据/游戏数据/animator_controllers.json` · `simple_animation_clips.json` · `camera_intro_clips.json`。
- **代码**：`WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs`（全文关键段）· `WarpforgeArena1/Editor/ArenaBuilder.cs`（groups/动画/材质/预置 prefab 段）· `WarpforgeVFX/Runtime/WarpforgeShaderLoader.cs` · `WarpforgeAnimatorBridge.cs` · `CardPresentation/Battle/ScenarioBlendables.cs` · `WarpforgeArena1/Editor/EffectExporter.cs`（ListedPrefabs / container / A1095）· `CardPresentation/Editor/ArtBaker.cs`（`ApplyArenaImportSettings`）。
- **资产（UnityPy 1.24.2 直读，只读）**：84 个源 bundle 的 **AssetBundle 对象所在 CAB 的 `m_Container`** 逐个扫（判 addressable）· 13 个 `scenes_scenes_battlearena*.bundle` 的 GameObject/Transform/Animation/Animator/AnimationClip/Texture2D · `battlesharedresources_assets_all.bundle`（466 条容器）· 我们自己的 `StreamingAssets/WarpforgeVFX/wf_*.bundle`（20 个战场 shader 逐个点名核在）。
- **工具（只读跑）**：`工具/arena_diff.py`（3 场，输出到 `_tmp_view/ja_diff_1022/`，**没写台账**）· `工具/extract_missing_shaders.py` 的 `repack_tree(..., dry_run=True)`（arena3/Scenario，只体检不写盘）。
- **没查**：远端 Addressables 包（本机没有）· 服务端配置 · `d:/2` 的任何写操作。
