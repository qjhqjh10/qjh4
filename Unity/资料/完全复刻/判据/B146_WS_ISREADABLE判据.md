# B146 · WS-ISREADABLE 判据（2026-11-05）

> 只读代理 `WS-ISREADABLE` · 未跑 Unity / typecheck · 未动 git · 未改任何源码、资产、账、表
> 对象 = `B140-WS-ISREADABLE`（79 对网格 `m_IsReadable`：原版 0 / 我们 1；写口 `WarpforgeArena1/Editor/B140_WS_MeshFlags.cs:22-23` 明写「故意不动」）

## 一、79 对名单（稳定复现）

**复现规则（3 步，纯文件读，不需 Unity）**：
① 取 `B140_WS_MeshFlags.Manifest`（101 条，`:56-159`）；
② 对每条读 `Assets/<相对路径>` 与 `d:/2/tools/assetripper_out/ExportedProject/Assets/Mesh/<同名>.asset` 的 `m_IsReadable` 行；
③ 不等的 = **79**（全部 我们 1 / 原版 0），相等的 = **22**。

**79 份中 78 份在 `Assets/WarpforgeVFX/Meshes/`，1 份是 `CardPresentation/Resources/Art/vfx/Cylinder.asset`**：

```text
400px-Choppa · Axe_1 · Background Wolf · Background space · Blade_DA · BoosterPack · Bullet_Body
Card 3D Subdivided · Card_Remnant_HO Optimization 2 · ChemVial · Chitin barb · Cylinder curved shallow
Cylinder_Ring · Cylinder_curve · Disk · Dynamic Lights 11/21/31/51/61 · DynamiteBundle · EC_Sword
Full Moon 101/111/121/131/14/21/31/41/51/61/71/81/91 · Full Moon1 · GSC Dagger · Grenade · Hammer
Harpoon · Helspear · Hemisphere · Ice · Keep Light1 · KrootJavelin · Missile.001 · Missile · Moon 4
Mountain Background First Light · Mushroom · Pray VFX Mesh3 · Rock003/004/005 · RocketWarhead
Shatter Piece 1/3 · Shell Bolter Imperial/Traitor · Shell Ork · Shield Ico · Shield_1
Ship 11/21/31/41/51 · Slash · Spear · Spike_1 · Spore Mine · Squig1 · Squig2 · Squig_low Merged
Stikka.001 · Tyranid_Claw · Tyranid_Rib · stone001
```

**22 份「本来就一致」里，两条最关键的都是 1（原版也 1）** —— 它们正是**唯一两个 `Resources.Load<Mesh>` 目标**：
`CardPresentation/Resources/Art/card3d/Card 3D WH40k.asset`（`CardArt.cs:150`）· `.../card3d/FxObject_cylinder_short.asset`（`CardArt.cs:296`）。
其余 20 份：`Tower 11`/`Cone.006`/`Cone.007` 原版 0（已对）· `WarpforgeVFX/Meshes/Card 3D WH40k`、`FxObject_cylinder_short`、`IceShard01/Tiny`、`RockDebris_Low`、`RockSpike_Low`、`Rock_A_02_LOD00/03/04`、`Tube_pf_sh_*`×4、`Vanguard Frame Animated_Mesh`、`debris_1_sm`、`debris_2_sm`、`sword-mesh` 原版 1。

> 原版侧唯一来源 = `ExportedProject/Assets/Mesh/`（595 份，分布 **575 个 0 / 20 个 1**，与「稀疏可读」自洽）。
> `d:/2/新解包资源/assets_full/*/Mesh/` 只有 `.obj`（13 个目录），**没有 JSON ⇒ 该字段无法第二源复核**（与 `判据/B142_对账_W130资源_W131工具.md:15` 记的一致）。

## 二、运行时读取普查（全仓 grep，`Assets/**` 排除 `/Editor/`）

**结论：0 处读取这 79 份的顶点/索引数据。** 运行时（非 Editor）全部命中只有 9 个文件，逐个判完：

| 位置 | 读的是什么 | 与 79 对的关系 |
| --- | --- | --- |
| `Core/CardView.cs:3792` `mf.sharedMesh.vertices` | 徽标底板网格 `"BadgePlateQuad_"+name` —— `CardView.cs:4236` **运行时 `new Mesh`** 建（可读） | ⛔ **不在 101 份内**（断言 B-4 已钉死） |
| `CardView.cs:10282/10355/10377/10383`、`:11601`、`:11737`、`:12707-12722` | `SpriteQuad`/`FrameMesh`/`ArtMeshInFrame` 等**运行时建的卡面 quad**（`m.vertices = …` 就在同文件 `:4095-4110/:11461…`） | 无关 |
| `Battle/ImageQuad.cs:1018`、`Battle/Label.cs`、`Battle/BattleBackdrop.cs` | 自己 `new Mesh` 的 UI quad（`RebuildMesh`） | 无关 |
| `Shell/MenuDraw.cs:1399-1411`（`TMP_MeshInfo`）、`:1526`（`Label` 点阵 quad）、`MenuTutorialHighlight` | TMP 网格 / Label 自有网格 | 无关 |
| `Shell/UIBorderGlow.cs:439` `GetUVs(0,…)` | ImageQuad 的 quad 网格 | 无关 |
| `WarpforgeVFX/Runtime/UIViewportUvWriter.cs:125` `mesh.uv` | ImageQuad 的 quad 网格 | 无关 |
| `Core/TMProWarpText.cs:159` | `TMP_TextInfo.meshInfo[].vertices`（结构体，不是 `Mesh`） | 无关 |
| **`WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs:321` `m.HasVertexAttribute(VertexAttribute.Color)`** | **元数据**（顶点布局查询），经 `:207` 逐渲染器调用 | ⚠️ **唯一会碰到 79 份的运行时调用**（见 §三） |

**另外四条「本来可能出事」的口，现读全部没有**：

- `MeshCollider`：全仓 `.cs` 无 `MeshCollider` 新增/赋值；脚本复核 VFX prefab 里的 `MeshCollider` 段 **0 个引用这 79 份**（38 个 VFX prefab 有 MeshCollider，但都不用这批网格）。`Physics.BakeMesh` 0 处。
- `MeshFilter.mesh`（会实例化网格、对不可读网格报错）：运行时 **0 处**（全部走 `sharedMesh`）。
- `RecalculateNormals/Tangents`、`CombineMeshes`、`StaticBatchingUtility`：运行时 **0 处**（`RecalculateBounds` 有 18 处，但都在运行时新建的 `new Mesh` 上）。
- `GetVertexBuffer/GetIndexBuffer/SetVertexBufferData`：**只在 `WarpforgeArena1/Editor/`（EffectExporter / MeshImportMethodProbe）**，属导期；与 `报告/B105_R14.md:41`「运行时读回顶点重建不存在」一致（该口径**成立**）。

**Editor 侧**（改 0 只影响编辑器工具，不影响 Player）：`MeshSanityProbe.cs:42` 本来就 `if (m.isReadable)` 才读顶点（会变成「跳过+打不可读」，不是崩）；`EffectExporter` 只用 `CopySerialized` 搬序列化数据、不读顶点；`ArenaBuilder.cs:846` 只把 `WarpforgeVFX/Meshes/Card 3D WH40k`（不在 79 内）挂到 MeshFilter。

## 三、结论：**可以改 0**（附一条出包待验）

- **顶点/索引数据：0 处运行时读取** ⇒ 改 0 不会打断任何现有读口。
- **唯一需要留意的一条**：`WarpforgeEffectBinder.MeshHasColor`（`:319-322`）对这个网格查 `HasVertexAttribute(Color)`。
  实际落在 79 份里的是 **`Ship 11/21/31/41/51`**：`Environmental Condition Dark Angels Void Combat.prefab` 的 `Ship 1..6` 六个渲染器引用的就是这 5 份（`Ship 6` 复用 `Ship 41` 的网格，脚本按 prefab 的 `MeshFilter.m_GameObject`→`m_Mesh.guid` 解析），
  而它们正是 `资料/普查产出_1018/W_特效线_ExtraColor与UnlitAmbient.md` 记的「38 个基础 `UnlitAmbient` 渲染器里**6 个有 ch3**」那一批（现读 `Ship 11.asset:34 m_IsReadable: 1`，`m_Channels[3] dimension: 4`）。
- `HasVertexAttribute` 查的是**顶点布局元数据**，Unity 文档（`UnityEngine.CoreModule.xml:26726`）无任何「需可读」限制；外部资料亦明确「读 layout、不需要可读」；工程内同源证据 = `MeshImportMethodProbe.cs:144` 对**来自 bundle 的不可读网格**调 `GetVertexAttributes()` 成功（且同文件 `:189-193` 明说 `GetVertexBuffer` 才需要可读）。
  ⇒ **判：不需 readable**。⚠️ 但这只是编辑器侧 + 文档，**Player 内未验**（见 §五）。
- 万一 Player 里它返回 false：症状 = 那 6 个渲染器退回 `tex*_Color`（偏暗），**不报错**。兜底极便宜：这 5 份保留 1 并在此文件注明「实现依赖」（其余 74 份照改 0）。
- **改 0 本身与原版一致**（原版这 79 份就是 0，而原版的 `Ship` 网格同样有 ch3 顶点色 ⇒ 原版也不是靠运行时读数据决定的）。

## 四、改法范围（文件 + 断言 + 重导）

1. **`WarpforgeArena1/Editor/B140_WS_MeshFlags.cs`（唯一写口）**
   - `Fields`（`:52`）末尾追加 `"m_IsReadable"`（顺序 = 第 5 列）；`Expected(i)`（`:248-252`）把硬编码的 `p[1..4]` 改成按 `Fields.Length` 循环；`ExpectedCount`（`:48`）**保持 101**（语义从「四字段」变成「五字段」，101 这个数不变）。
   - `Manifest`（`:56-159`）每行加第 5 列：79 份填 `0`、22 份填 `1`（**逐份按本文件 §一 的原版值**，⛔ 别一律填 0）。
   - `ReadFlagsFromFile` / `PatchFileFields` / `Show` / 顶点指纹护栏**都按 `Fields.Length` 泛化，零改动**；`WriteOne` 的「SerializedObject → 文本级 → 回读」三段式自动覆盖。
   - 头部注释 `:19-23` 的「② 不动 `m_IsReadable`」要**就地改写**（⛔ 零行数变化，账按行号引用本文件）。
2. **`CardPresentation/Editor/B140_WS_Assert.cs`**
   - `CheckA_Flags`（`:82-107`）按 `Fields` 循环 ⇒ 4 条变 5 条；前提文案「四个字段都读得出」（`:77`）改成五个；现有 18 处 `Check(` 调用点 → 19 处（A 组多 1）。
   - **必须补一条灭自证**（照 C-1 的样子）：把临时副本的 `m_IsReadable` 改成 1 ⇒ A 组必须报红；并断言**顶点指纹不变**（改这个字段不该动顶点字节）。
3. **重导 / 生效顺序**：不必重导整个网格 —— 写口自己的 `ImportAsset(ForceUpdate)` + 回读就够。
   ⚠️ **但 `EffectExporter.ImportMesh` 一重跑就会把它冲回 1**（新 `Mesh` 默认值）⇒ 这条与 `B140-WS-RERUN` 是同一个时序：**任何网格重生成之后都要再跑一次 `B140_WS_MeshFlags.Run`**。
4. **`Core/CardView.cs:3792` 不需要改**（读的是运行时 `new Mesh`，且那 22 份里两个 `Resources.Load` 目标原版就是 1）。
5. 附带收益：79 份网格在 Player 里不再保留 CPU 副本（内存）；与原版内存行为一致。

## 五、没查清的

1. 🔴 **`HasVertexAttribute` 在 Player（非编辑器）里对不可读网格的返回值** —— 离线无法证。**一条出包待验**：进 `Environmental Condition Dark Angels Void Combat`，确认 6 个 `Ship` 渲染器的 `_VertexColorLerpOn` 仍为 1（或并排看画面）。这是本判据唯一的运行时风险，且有便宜的兜底（5 份保留 1）。
2. **原版 `m_IsReadable` 只有 `ExportedProject` 一个来源**（assets_full 的 Mesh 目录只有 `.obj`），无法第二源交叉。分布 575:20 自洽，但没有独立复核。
3. **手工改 YAML 的 `m_IsReadable` 经 `ImportAsset(ForceUpdate)` 是否原样保留** —— 预期保留（工程里现读 **679 份 `.asset` 带 `m_IsReadable: 0`**，除 1 份 TMP SDF 外全是网格；写口每步都回读校验），但没在 Unity 里跑过这次的新增字段。
4. Editor 侧的诊断能力会退化一项：`MeshSanityProbe` 对这 79 份不再打顶点 dump（它本来就 `if (m.isReadable)` 守卫，不会崩）。
5. 未查：包内/第三方（TMP）是否会碰这批网格 —— 无引用路径，判不影响。

---

**Sources（外部资料，仅用于 §三 `HasVertexAttribute` 的可读性判断）**：
[Unity Mesh.HasVertexAttribute](https://docs.unity.com/zh-cn/engine/6000.7/script-reference/unityengine/mesh/hasvertexattribute) ·
[Unity Mesh.isReadable](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/mesh/isreadable) ·
[Unity Manual · Mesh vertex data](https://docs.unity.cn/6000.0/Documentation/Manual/AnatomyofaMesh.html)
