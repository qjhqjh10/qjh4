# B146-E10 · WS isReadable 写口与断言（2026-11-05）

> 执行型写手 E10 · 只改白名单三处 · 未跑 Unity / 未动 git / 未碰账与活状态表
> 判据 = `判据/B146_WS_ISREADABLE判据.md`（§一 名单 · §三 结论 · §四 改法）

## 一、改了什么

### 1. `MyGame/Assets/WarpforgeArena1/Editor/B140_WS_MeshFlags.cs`（唯一写口）

| 位置 | 改动 |
| --- | --- |
| `:22-23` 头部「故意不做」第 ② 条 | **就地改写、零行数变化**：原「**不动 `m_IsReadable`**（原版 79 份 = 0、我们 = 1）…只点了那四个字段」→ 现「**`m_IsReadable` 照原版写 0**（判据 `B146_WS_ISREADABLE判据.md`；运行时 0 处读这批网格的顶点数据）」＋ 第二行「⚠️ 例外：`Ship 11/21/31/41/51` 五份**保留 1** —— `WarpforgeEffectBinder.cs:321` 的 `HasVertexAttribute` 实现依赖，待出包验」 |
| `:50-52` `Fields` | 注释「四个字段 / `int[4]`」→「五个字段 / `p[1]..p[5]`」；数组末尾加 `"m_IsReadable"`（仍在同一行，零行数变化） |
| `:54-55` Manifest 格式注释 | 格式串补 `\|<IsReadable>`；并写明实算 **77×0 / 24×1** |
| `:58-158` `Manifest` 101 行 | **每行末尾追加第 6 段**（= 第 5 个数值列）。⛔ **不是手抄**：由脚本逐份读 `ExportedProject/Assets/Mesh/<同名>.asset` 的 `m_IsReadable` 算出来，`Ship 11/21/31/41/51` 五份覆盖为 1（脚本内断言「每条 rel 路径在全文件恰好命中 1 次」「CRLF/LF 计数与写前完全相同」「字节数 = 旧 +2×101」） |
| `:248-254` `Expected()` | 硬编码 `p[1..4]` → **按 `Fields.Length` 循环**：`var v = new int[Fields.Length]; for (int k = 0; k < v.Length; k++) v[k] = int.Parse(p[k + 1]); return v;`（+2 行） |
| `:33 / :295 / :328 / :371 / :420 / :449` | 「那 4 行 / 四个字段」文案 → 「那 5 行 / 五个字段」（纯文案，零行数变化） |
| `ExpectedCount = 101`（`:48`） | **未动**（语义从「四字段条数」变「五字段条数」，数不变） |
| `ReadFlagsFromFile` / `PatchFileFields` / `Show` / `VertexFingerprint` / 三段式 `WriteOne` | **零逻辑改动**（本来就按 `Fields.Length` 泛化） |

### 2. `MyGame/Assets/CardPresentation/Editor/B140_WS_Assert.cs`

- `CheckA_Flags`（`:82-106`）：`new List<string>[4]` / 三处 `k < 4` 全部改成 `B140_WS_MeshFlags.Fields.Length` / `bad.Length` ⇒ **A 组 4 条 → 5 条**（新增 `m_IsReadable` 一档）。
- `CheckPrereq`（`:73/:77`）：「四个字段读不全 / 四个字段都读得出」→ 五个。
- 文件头 `:3 / :18 / :20-21`、节标题 `:80`：四个 → 五个，C 组描述补上 C-4。
- `CheckC_SelfProof`（`:172`）：`broken = new[] { cur[0], cur[1], 0, 0 }` → `{ …, cur[4] }`（**必须**：`PatchFileFields` 按 `Fields.Length` 索引 `values[k]`，仍是 4 元素会 IndexOutOfRange）。
- **新增 C-4 灭自证**（`:194-225`）：样本 = `Assets/WarpforgeVFX/Meshes/400px-Choppa.asset`（清单第 5 列 = 0，⛔ 不用 `Card 3D WH40k` —— 那份原版就是 1，改回 1 恰好「正确」，测不出东西）。临时副本 `m_IsReadable` 置 1 ⇒ ①`got2[4] != want2[4]`（A 第五档必红）②顶点指纹不变 ③工程里那份未被动。期望值经新增 `ManifestIndex()`（`:231-237`）从 `Manifest` 现读，⛔ 不硬编码 0。
- `ManifestIndex` 追加在**类尾**（`ChatPanel`/`BattleDriver` 那类插中间导致漂移的坑不在这里重演）。

## 二、Manifest 第 5 列计数（实测）

**101 行 × 5 列全部合法；第 5 列 = `77 × 0` / `24 × 1`。**

⚠️ **与简报里「79 份填 0、22 份填 1」不一致，这是简报自身算错，已按判据/原版数据走**（三条反证）：

1. 简报同段又写「五份 `Ship` 例外保留 1」，而 79 对里的 `Ship 11/21/31/41/51` **正在这 79 之内**（原版 0 / 我们 1）⇒ 79−5 = **74** 才是「照原版改 0」的部分。
2. 判据 §一 明写 `Tower 11`/`Cone.006`/`Cone.007` **原版 0（已对）** —— 我们现值也是 0，**本来就不用改**；若按「22 份填 1」会把这三份从 0 掰成 1 = **反向偏离原版**。
3. 判据 §三 原文：「这 5 份保留 1 并注明实现依赖（**其余 74 份照改 0**）」⇒ 74 + 3（本来就 0）= **77×0**；19（原版本就 1）+ 5（Ship）= **24×1**。

脚本逐份复算（`Fields` 从源码解析、原版值从 `ExportedProject` 现读）复核：**errors: none**（每条目标值 = 原版值，唯 5 份 Ship 覆盖为 1）。

## 三、断言 A 组 4→5 + C-4

- A 组运行时条数：**4 → 5**（`Fields[4] = m_IsReadable`）。源码里 `Check(` 出现处：`18 → 22`（A 组 +1、C-4 组 +4：前提①/前提②/灭自证/指纹 + C-4c）。断言总数运行时 ≈ 21 → 26。
- C-4 不是同义反复：它把**同一份坏样本**同时喂给 `ReadFlagsFromFile` 与 `VertexFingerprint`，要求「字段判据报红 **且** 顶点指纹不变」—— 结构上不可能「两边一起改回去还全绿」。

## 四、typecheck

`TMPDIR=/tmp/wf_e10 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（独立 TMPDIR，无并发覆盖）。
`git diff --numstat`：MeshFlags **116/114** · Assert **55/13**（无整篇重写；两文件 CRLF 计数与写前一致，bare CR = 0）。

## 五、顺手发现（⛔ 未改，交调度台）

1. `WarpforgeArena1/Editor/B141_W18_Assert.cs:105` 文案「**四字段**名单条数 = 期望」—— 它只用 `Count/ExpectedCount`（101，不变），**逻辑没坏，只是措辞过期**。
2. `判据/B142_对账_W132接线普查.md:107` 记「`B140_WS_Assert.cs` **19** 条」、`判据/B142_对账_W130资源_W131工具.md:15/47`、`判据/B142_核销清单.md:151`、`判据/B145_对账_R3资源战场.md:17` 仍写「写口只有四个字段 / 故意不动 `m_IsReadable`」—— 全是**证据文件、允许过期**，但建议调度台核销时一并更新指针（我不在账/判据白名单内）。
3. **写口能力未验**：`SerializedObject.FindProperty("m_IsReadable")` 是否可写（`Mesh.isReadable` 在 API 层只读）—— 若不可写，`WriteOne` 的三段式会退回**文本级改写**并回读校验，行为上仍正确；但 `Run` 的日志会多 101 条 warning，W132 跑的时候留意即可。
4. `EffectExporter.ImportMesh` 重跑会把 `m_IsReadable` 冲回 1（新 `Mesh` 默认）⇒ 与 `B140-WS-RERUN` 同一时序约束：**每次网格重生成之后必须再跑一次 `B140_WS_MeshFlags.Run`**（判据 §四·3 已写，写口注释未提，未加以免动行数）。
