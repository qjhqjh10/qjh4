# W21 · `EffectExporter` 两条路改「确定路径 + 原地覆盖」（`ImportMaterial` / `ImportMesh`）

> 2026-10-16 · 子代理 W21。**只改了一个文件**：`Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`
> （白名单内；⛔ 没碰任何 `.mat` / `.asset` / `.py` / 两张正本）。判据来自 `资料/普查产出_1016/W20_mat副本根治.md`（已整份读完）。
> 数字标「**复算**」= 本件自己跑的；标「W20」= 引它的读数。

## ① 结论

1. ✅ 两处病灶都改成「**确定路径 + `CopySerialized` 原地覆盖**」，与同文件先例 `ImportClip`（`:2119-2154`）同形：
   · `ImportMaterial` 的落盘段 = `EffectExporter.cs:1551-1586`（病灶原在 `:1415-1416`）
   · `ImportMesh` 的落盘段 = `EffectExporter.cs:1922-1952`（病灶原在 `:1756-1757`）
2. ✅ 两条路都加了**撞名守卫**（裁定硬要求）：`MatByName:167` / `MeshByName:169` · `GuardMatName:1397` / `GuardMeshName:1417`。
   **出声条件 = 同一个目标文件名被第二个「内容不同」的源占用**；内容等价的**不出声**（W20：171 个名字 / 811 个文件正文等价 ⇒ 塌成一份无害）。
3. ✅ **验收**：`TMPDIR=/tmp/wf_w21 bash /d/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**（本件跑过三遍，**最后一遍在全部改动落定之后**）。
   行尾：改前 `CRLF 2151 / LF 2151 / 裸 LF 0` ⇒ 改后 `CRLF 2348 / LF 2348 / 裸 LF 0`（**没翻**）；`git diff --numstat` = **207 / 10**（不是整篇重写）。
4. ⛔ **没跑导出器**（`Run()` 会 `ClearGenerated()` 删库，主对话另排）⇒ 见 §④·1：这两条改法**还没有实跑证据**。
5. 顺手改掉两处**已经变成假话**的注释（铁律 5）：`NameFilter` 上头那句「每跑一次都会在 Materials/ 里留下 `X 1.mat` 副本」（`:50`）·
   `ClearGenerated` 的 doc「不清的话 `GenerateUniqueAssetPath` 会不断产出 `X 1.mat`」（`:571`）—— 成因已不存在（**函数照旧要跑**，只为「旧贴图/网格和新的一起被 binder 引用」那条理由）。

## ② 逐处 改前 / 改后

**（A）`ImportMaterial` 落盘段（`:1551-1586`）**

改前（`:1415-1419`）：
```csharp
var path = AssetDatabase.GenerateUniqueAssetPath($"{MatDir}/{Sanitize(src.name)}.mat");
AssetDatabase.CreateAsset(mat, path);
MatCache[src] = mat;
MatMeta[src] = (approx, origShader);
return mat;
```
改后（核心 4 步，注释见源码）：
```csharp
string stem = Sanitize(src.name);
GuardMatName(stem, src);                                   // ← 撞名出声（见 ③）
string path = $"{MatDir}/{stem}.mat";                      // 确定路径
var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
if (existing != null) { EditorUtility.CopySerialized(mat, existing); EditorUtility.SetDirty(existing);
                        UnityEngine.Object.DestroyImmediate(mat); asst = existing; }   // guid 不变
else                  { AssetDatabase.CreateAsset(mat, path); asst = mat; }
MatCache[src] = asst;  MatMeta[src] = (approx, origShader);  return asst;
```
· 与 `ImportClip` 唯一的差别：**不逐份 `SaveAssets`**（`Run()` 每 10 个存一次 `:523`、收尾 `:530`；`RunListed()` 收尾 `:765-766`）。
· `DestroyImmediate(mat)` 安全 —— 核过 `ApplyRenderState`（`:1650`）与 `ApplyDerivedParticleDefaults` 都只改 `dst`、**不登记 `mat`**。

**（B）`ImportMesh` 落盘段（`:1922-1952`）**：改前 `:1756-1757` 同样是 `GenerateUniqueAssetPath` + `CreateAsset`；改后同形
（`LoadAssetAtPath<Mesh>` → 有就 `CopySerialized` + `DestroyImmediate` + `MeshCache[m] = existing`，没有才 `CreateAsset`）。

**（C）新判据写进注释（`:1558-1562`）**：旧的「`* [0-9].mat` 清到 0」**作废**（原版本来就有数字结尾的名字，W20 扫 1092 个原版 Material JSON 证实 10 个）；
新判据 = **「形如 `X N.mat` 且同目录存在 `X.mat`」** = 今日 **730 个文件 / 190 个基名**（**复算**；旧 glob `* [0-9].mat` = 718 · 含两位 = 749 · 全部 `.mat` = 1399。
W20 亲数 729 ⇒ 同判据差 1，属快照差）。`Meshes/` 同判据 = **102**（**复算**；`X N.asset` 形状 = 164 = W20 那个数；全部 `.asset` = 250）。
**⇒ 判据那句话可以原样搬进 `项目任务.md` §三第9条。**

## ③ 撞名守卫怎么写的

1. **记谁**：`MatByName` / `MeshByName` 存「**第一个**源对象 + 它当时的内容字段表」。后来每个**不同的**源都跟这一个比。
   ⇒ 3 个源抢一个名字 = 打 **2** 行警告（第 2、3 个各一行）——**这是有意的**：每个「输家」都点名。
2. **比什么**：`MatFields:1323`（`源名` · `shader` · `renderQueue` + 逐属性 `值`；贴图取**对象名**、不取像素）·
   `MeshFields:1353`（`源名` · 顶点数 · 子网格 · 索引格式 · 索引合计 · 各子网格 `start+count` · 顶点通道 · bounds · blendShape · bindpose）。
   **只读元数据**：网格**不碰顶点缓冲**（流式网格的顶点本来就不可靠，拿它当判据会误报）。
   `FieldDiff:1375` **按字段名对齐**（不是按下标），列到 6 条为止。
3. **出声长这样**（材质版，网格版同形）—— 原文：
   `[EffectExporter] 材质**撞名且内容不同**：Embers —— 先到 Embers（shader Everguild/FX/…）、后到 Embers（shader …）；确定路径写同一份 ⇒ **后到的赢，先到的那种在工程里没有对应文件**。差异 N 处（最多列 6）：_Cull: 0 → 2 / _BaseColor: … → …`
   ⇒ **两个源名 + 两边 shader + 差异字段逐条**，够直接定位「谁被盖了、差在哪」。
4. **读属性全程 try/catch**（读不出写 `=<读不出>`）—— 纯诊断，**不许因为它把导出打断**。
5. 🔴 **顺带记一个我自己的计数错误（已就地订正）**：初稿把 `.mat` 的正则套在 `.asset` 上数，得出「Meshes 副本 = 0」的**假 0**，
   写进了注释；复算后是 **102**（164 形状命中）。教训已写进注释：**判据换后缀时正则要跟着换**。

## ④ 没查清的 / 还欠什么

1. 🔴 **还欠「跑一次导出器」验证**（主对话排；本件只过了类型检查）：
   · **(a) 材质那条路** —— 旁证很硬：同族 `ArenaBuilder.SaveOrReuse`（`ArenaBuilder.cs:3435-3450`）**已经在用**「确定路径 + `CopySerialized` 覆盖材质」跑构建，
     `ImportClip` 也是既成事实 ⇒ 风险低。跑完核：全量 `Run` 之后新判据应为 **0**（原版那 10 个数字结尾的名字**不算**）。
   · **(b) 网格那条路 —— 这一条真有风险**：`EditorUtility.CopySerialized` 落到**已有网格资产**上**没验过**。
     M3 那条源码在 `MeshImportMethodProbe.cs:162-172`（从**包里的**网格 `CopySerialized` 进 `new Mesh()`）；
     本文件 `ImportMesh` 的注释记着「**8 种导法七种全给出全 0**」，但**那一批的输入是流式源**（本来就该丢），
     与这里「源是 `DeepCopyMesh` 出来的健康网格」**不是同一个输入** ⇒ **那条结论既不能用来判它死、也不能用来判它活**。
     ⇒ 跑完**必须回读** `.asset` 的 `_typelessdata`
     （判据/读法见 `MeshImportMethodProbe.ReadBackVertexRange:56-86`：NaN/Inf 或 |值|>100 = 还是垃圾）。
     真出事：退路 = 把 `DeepCopyMesh` 改成往**已有资产**里 `SetVertexBufferData` 就地重灌，⛔ **别退回 `CreateAsset`**。
   · **(c) 撞名警告**：grep `撞名且内容不同`，预计 18 个名字 / 最多 ~107 行（W20 的数）——**一行都没有 = 守卫没触发，要么真没撞、要么守卫写漏了**，要当场分辨。
   · **(d)** 跑完**必须跟一次 `BoosterPackExporter.Run`**（全量重导换掉 Materials 全部 guid，见 `:22` / `:1566-1567` 注释）。
2. 🔴 **一个真·忠实度缺口（要做，只是先后 —— 铁律 11）**：确定路径 + 后到赢 ⇒ 那 **18 个名字 / 最多 107 个不同内容的材质**里，
   「先到的那种」**不只是没有独立文件，运行时拿到的也是后到那份的内容**（guid 相同 ⇒ 早先存盘的 prefab 也一起指过去）。
   现在只做到「**出声、不静默**」。要真复刻得给每个不同源**各自的确定路径**（例如按 源包文件名 + 源 pathID 加稳定后缀）——
   ⚠️ **判据还没定**（先要证 `pathID` / 源包键在同一 bundle 版本里稳定、且 18 个名字的「谁该配谁」查得清），⛔ 别凭感觉配。
3. 🔴 **文档漂移（不在我白名单，请调度台改）**：
   · `资料/普查产出_1016/盘点_GH段剩余小账.md:34` 仍把要改的文件写成 **`工具/import_original_art.py`**（W20 已指出是记错；**真产地是本件这个 `.cs`，现在已经改完**）；同文件 `:62` / `:63` 是同一句。
   · 同处 + `项目任务.md` §三第9条里的判据「`* [0-9].mat` **>0 就没清干净**」与数字「718」要换成 §②(C) 那句新判据（**718 永远到不了 0**）。
   · 副本**别手删**（W20 ④ · 同账）：下一次**全量** `Run` 的 `ClearGenerated()` 会把现存 730 / 102 一起带走。
4. `BoosterPackExporter.cs:349/364/371/373` 走的是**同一份实现**（转调这两个函数）⇒ 一并受益、**没有第二处判据**要改
   （已 grep 确认：全仓 `GenerateUniqueAssetPath(` 的**真调用点 0 处**，只剩本件源码注释里那一条引用）。
   `ArenaBuilder.ClearMaterialDir:3468` 清的是**另一个目录**（`Arenas/<scene>/Materials`）⇒ 与本件不冲突。
