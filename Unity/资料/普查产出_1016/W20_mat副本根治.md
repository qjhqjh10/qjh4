# W20 · `X N.mat` 副本根治（现核 + 改法，**未落刀**）

> 2026-10-16 · 子代理 W20。**本件没改任何文件** —— 原因见 ①·2，改法在 ②，可原样贴。
> 口径：每个数字都是本轮现核；括号里写「亲数 / 亲读」的是我自己跑的，其余注明出处。

## ① 结论

1. 🔴 **要改的不是 `工具/import_original_art.py`。** 那个文件 131,127 B / 1599 行，亲数：`Material` = **0** · `material` = **0** · `.mat` = **0** · `ImportClip` = **0** —— 它导的是卡图 / 卡背 / 立绘（`cardback_jobs` / `portrait_jobs`）。
   **真产地是 C#**：`Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`
   - 病灶：`ImportMaterial` 的 **`:1415-1416`**
   - 账上点名的先例：`ImportClip` = **同一个文件的 `:1922-1958`**（不是另一个文件）
   ⚠️ 错点在 `资料/普查产出_1016/盘点_GH段剩余小账.md` 的「§三 第9条 · 3 件」行第 ③ 列（它写「要碰的文件 = `工具/import_original_art.py`」）。
   **`项目任务.md` 那条正本（§三 第9条，:252）是对的** —— 它只写函数名、**没写文件名**。`ImportMaterial` 全仓命中 3 处：`EffectExporter.cs`（定义 `:1299` + 调用 `:886/:901/:927`）· `BoosterPackExporter.cs`（`:349/:364` 转调）· `工具/_extract_dropped_props.py`（只在校验报告里提了一句名字）。
2. 🔴 **本件未落刀**：我的白名单只有那个 `.py`，黑名单写着「任何 `.cs`」⇒ **真产地正落在黑名单里**。按铁律 13·4 ⑦「**卡住就停手写进报告**，不许自己发明口径」「越界改 = 撞车 + 无人审查」，**我没有改那个 `.cs`**。改法完整写在第 ② 节，**逐句可直接替换**。

## ② 改前 / 改后（逐句对照）

**病灶（`EffectExporter.cs:1415-1416`，亲读）**
```csharp
var path = AssetDatabase.GenerateUniqueAssetPath($"{MatDir}/{Sanitize(src.name)}.mat");
AssetDatabase.CreateAsset(mat, path);
```
· `GenerateUniqueAssetPath` = 「**路径不确定**」：目标被占就吐 `X 1.mat` / `X 2.mat` …
· `CreateAsset` = 「**每次新建**」：旧的那份被删掉、**guid 换一批**

**先例（同文件 `ImportClip:1929-1944`，亲读）**
```csharp
string path = $"{AnimDir}/{copy.name}.anim";                                 // 确定路径
var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
if (existing != null) { EditorUtility.CopySerialized(copy, existing); … }    // 原地覆盖、guid 不变
else                  { AssetDatabase.CreateAsset(copy, path); }
```

**建议改法（照 `ImportClip`，替换 `:1415-1416` 这两行）**
```csharp
string path = $"{MatDir}/{Sanitize(src.name)}.mat";
var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
Material asst;
if (existing != null)
{
    EditorUtility.CopySerialized(mat, existing);   // 原地覆盖：guid 不变
    EditorUtility.SetDirty(existing);
    UnityEngine.Object.DestroyImmediate(mat);      // 与 ImportClip 同：不留临时对象
    asst = existing;
}
else { AssetDatabase.CreateAsset(mat, path); asst = mat; }
```
· 配套改 `MatCache[src] = asst;`（原来存的是 `mat`），返回值也用 `asst`。
· **不需要额外 `SaveAssets`** —— `Run()` 每 10 个存一次（`:504`）、收尾再存一次（`:511-512`）；`RunListed()` 收尾存（`:745-746`）。逐材质调一遍会白等 958 次。
⚠️ **与 `ImportClip` 的差异只有这一处**（材质这里不调 `SaveAssets`，`ImportClip:1956` 有）；其余同形。另外 `ImportClip` 的 `copy` 来自 `Instantiate`、这里 `mat` 来自 `new Material(sh)`，但两者都是**非资产对象**，`CopySerialized` 的用法一致。

## ③ 改完再跑一次，理论上会怎样

先看**谁会清**：`Resume` 是 `const bool Resume = false`（`:44`）⇒ **`Run()` 每次都先 `ClearGenerated()`**（`:399-401` → `:554-566`，把 `MatDir/TexDir/MeshDir/PrefabDir` 下**每一个**资产删掉）；`RunListed()` 开头**没有**那段（`:654-656`）。

| 场景 | 今天 | 改完 |
|---|---|---|
| 下一次**全量** `EffectExporter.Run` | 开跑先删光 → 跑完又长回来 | 开跑先删光 → **跑完只剩「一个名字一份」** |
| 下一次 `RunListed` | 不清 ⇒ 每个撞名又添一份 | 不清，但**原地覆盖**，份数不增 |
| 现存 749 个 | —— | **由同一次 `ClearGenerated()` 带走** |

⇒ **「改脚本」和「清掉旧副本」是同一次全量重导一并完成的** —— 这就是判据④「⛔ 别手删」的根据。
**但两条副作用要一起认**：① 这次重导会换掉 Materials 目录下**所有 guid**（凡同一次没重导到的消费者会变 null 引用 —— `CLAUDE.md` §三「生成资产别用 `GenerateUniqueAssetPath` + 每次先删光」那条坑）；② 它同时重建 Prefabs / Bundles，**跑完必须跟一次 `BoosterPackExporter.Run`**（`:22` 已注明）。

## ④ 没查清 / 顺手发现（按值排序）

1. 🔴 **改法有真代价，账上没写 —— 必须先裁**：`X N.mat` 里有 **107 个文件 / 18 个名字**，内容**确实互不相同**（亲读逐份比 YAML 正文、忽略 `m_Name` 与 `&锚`）：`Embers` 24 份 / 3 种 · `LightningTrail_intense` 12 / 2 · `RockDebris` 10 / 2 · `Chestrays` 8 / 2 · `Glow Additive Extra Color Soft` 8 / 2 · `Alpha Masks Two Layer Variant Webway` 6 / 2 · `EarthCrack` 6 / 2 · `RippleSubtle Distort` 6 / 3 · `SandParticle` 6 / 2 …… 差异是**真语义**（不同贴图 guid / `_Cull` / `m_DoubleSidedGI` / `_BaseColor` 31.3 vs 0.31）。
   ⇒ 照 ② 改完 = **后写的赢，前一种被静默盖掉**（18 个名字各点名一个「输家」）。**建议**：贴 ② 的同时**加一条出声** —— 一个 `static readonly Dictionary<string, Material> MatByName`，第二个**不同 `src`** 抢同一个名字时 `Debug.LogWarning` 打出两个源名与 shader；⛔ 不许静默覆盖（铁律「不许静默失败」）。
   另一半是好消息：**171 个名字 / 811 个文件**正文等价（消除 `m_Name` 行与 `--- !u!21 &锚` 之后逐行相同），塌掉**无害**。
   撞名是**原件就有的**（不是在工程里长出来的）：`Necron Skull` ×2 · `Embers` ×2 · `RockDebris` ×2 都在 `bundle_battleprefabs_vfxandmisc_assets_all/Material` 里；`Chestrays` ×2 **跨两个 bundle**；`Embers` 那两条还在一个叫 **`bundle_duplicateassetisolation_assets_all`** 的包里（名字本身就叫「重复资产隔离」）。
2. 🔴 **判据「`* [0-9].mat` > 0 就是没清干净」永远到不了 0** —— **原版本来就有以数字结尾的材质名**，亲扫 27 个 bundle 的 `Material/*.json`（1092 个）证实：`Lens Flare 1` · `Glow Sphere 01` · `Flare 3` · `Iron_Halo 2` · `fireball 1` · `Tau_Letters 1` · `Waterfall_Middle 2` · `DCannon_Trail_MultiRay 1` · `Laser_Stream_MultiRay 1` · `Glow Noise Stylized Yellow add 1` —— **10 个名字各命中 1 次**。改完它们照旧在。
   ⇒ **判据该换成**：「形如 `X N.mat` **且同目录存在 `X.mat`**」。亲数今天 = **729**（原始 glob `* [0-9].mat` = 718 · 含两位数 = 749 · 全部 `.mat` = 1399）。③ 里写的「718 只是当日快照」这句**成立**。
3. 🔴 **同一个病还在 `ImportMesh`**（账上只记了 Materials）：`:1756-1757` 同样是 `GenerateUniqueAssetPath($"{MeshDir}/…")` + `CreateAsset` ⇒ `Meshes/` 亲数 **164 个 `X N.asset`**。而 `Textures/`（`:1564`）与 `Prefabs/`（`:1016`）**已经是确定路径**，各有 11 / 10 个编号文件 —— 那 21 个应是旧代码遗留、下次全量重导会自己消失（**未逐个核，属推断**）。**反证很干净**：已改成确定路径的 `Animations/`（`ImportClip`）与 `Controllers/` 亲数**都是 0** 个编号文件 —— 与「确定路径 ⇒ 不长副本」完全一致。
4. **`Embers` 那 3 种变体的来源没查透**：原版数据里只有 **2** 个 `Embers`，却有 24 份 / 3 种。第二来源可能是 `Assets/StreamingAssets/WarpforgeVFX/*.bundle`（**我们自己重打的小包，本轮没扫**）。
5. **账上「只在单次运行的同一批 prefab 之间产生、不是跨轮累积」这句话只对了一半**：`LightningTrail_intense` 在原版里**只有 1 个**源材质，我们却有 **12** 份 —— 单次运行内缺缓存解释不了 12，**`RunListed` 不清产物**（③ 那张表第 2 行）也是成因。⇒ 两个成因并存：**(a) 同名不同源材质（原件就有）** + **(b) `RunListed` 反复跑、`GenerateUniqueAssetPath` 逐次进位**（名字已以数字结尾时会**进位**：`Lens Flare 1` → `Lens Flare 2`，这正好解释为什么 `Lens Flare 2..5` · `Glow Sphere 02..05` **在解包资源里一个都找不到**，而 `… 1` / ` 01` 找得到）。
6. **改法未经验证**：本件没跑 Unity、没编译，`CopySerialized(mat, existing)` 这条路**没实跑过**（`ImportClip` 那条是既成事实、可作旁证）。裁完派谁去改时，请一并跑一次**秒级类型检查**。
