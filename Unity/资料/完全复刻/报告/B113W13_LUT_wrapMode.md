# W13 · B-119 —— 战场 7 张 LUT 的 `wrap` 从 Clamp 改回原版的 Repeat

- 会话 / 波次：第一百一十三会话 · 第 1 波
- 写手代理：`W13`（白名单 = `ArenaBuilder.cs` · `A/WarpforgeArena1/Textures/LUT/*.png.meta`（7）· 新建 `A/CardPresentation/Editor/B113W13_Assert.cs`）
- 日期：2026-11-01
- 账：`B-119`（用户 2026-11-01 点名「要做、彻底解决」）
- **不跑 Unity**（本波无 `-executeMethod`）；**未动 git / 账 / 活状态表 / `CLAUDE.md` / `项目任务.md` / 宿主 / 场景 / prefab**

---

## 判据（2026-11-01 现读，三源一致）

| 源 | 读数 |
|---|---|
| `d:/2/tools/assetripper_out/ExportedProject/Assets/Texture2D/LUT Battle Arena *.png.meta`（**7 份**） | `textureSettings:` → `wrapU: 0` / `wrapV: 0` / `wrapW: 0` |
| `d:/4/Unity/数据/游戏数据/arena_texture_import.tsv:62-68`（← `工具/gen_arena_texture_import.py` 用 UnityPy 读**源 bundle** 的 `m_TextureSettings`） | 那 7 行 `wrapU=0 / wrapV=0` |
| `TextureWrapMode` 枚举 | `Repeat = 0` / `Clamp = 1` |

⇒ **原版 = Repeat**，**三轴（U/V/W）都是 0**。我们原来是 `wrapU/V/W = 1`（Clamp），**7 张全错**。

> ⚠️ 这条**不是纯形式差异**：LUT 是 256×16 的表，`ColorLookup` 采样到 uv≈0 / ≈1 时，Bilinear 会取到半像素外的样点 ——
> Clamp 复制边缘、Repeat 从**对侧边缘**取 ⇒ **极端色（表头和表尾）的取值真的不一样**。所以「改对」要能改出画面差异。

---

## ① 代码改动（`A/WarpforgeArena1/Editor/ArenaBuilder.cs`）

**先答「这个方法是不是通用函数」**：`EnsureLutImportSettings` **只服务这 7 张 LUT** ——
全文件唯一调用点在 `ApplyPostFx` 的 `ColorLookup` 分支（`:3606-3607`），实参是 `"Assets/WarpforgeArena1/Textures/LUT/LUT {base0}.png"`。
另一条会遍历 LUT 目录的路 `ApplyTextureImportSettings` 走到 `ApplyOriginalImport` 时，LUT 分支**提前 `return false`** ⇒ 调用方 `continue`、**不写任何 wrap**。
⇒ **可以就地对整段改 `Repeat`，不必分「LUT 那一支」**（它本来就只有 LUT 一支）。

| 行号 | 改前 | 改后 |
|---|---|---|
| **3547**（方法文档） | `…本方法仍写 Clamp，那是**我们挑的**、不是原版值…` | `…② 而本方法**当时仍写 Clamp**，那是我们挑的、不是原版值 ⇒ **已于 2026-11-01 按原版改成 Repeat**（账 B-119 结案…三轴 wrapU/V/W 全 0）…` |
| **3555**（**判定式**） | `&& ti.wrapMode == TextureWrapMode.Clamp && …` | `&& ti.wrapMode == TextureWrapMode.Repeat && …` |
| **3563**（**写入**） | `ti.wrapMode = TextureWrapMode.Clamp;` | `ti.wrapMode = TextureWrapMode.Repeat;` |

**另外顺手把 5 行「已过期」的注释就地改掉**（铁律 5：文档写错就地改，⛔ 别只在新文件里说）——
这些行都还写着「原版 Repeat / **我们 Clamp**」「唯一差距属账 B-119」，账结案之后它们就是**下一个会话的误导源**：

| 行号 | 改前 | 改后 |
|---|---|---|
| **1582** | ``…（7 份，**只核对不写** —— 归 `EnsureLutImportSettings`，**唯一差距 wrap 属账 `B-119`**）；…`` | ``…（7 份，**只核对不写** —— 归 `EnsureLutImportSettings`；**wrap 已按原版对上**，账 `B-119` 2026-11-01 结案）；…`` |
| **6134-6136** | `// 🔴 LUT：**只核对、不写**。…它唯一与原版不一致的是 wrap（原版 Repeat / 我们 Clamp）—— // 那是**账 B-119**，属战场线执行窗口…` | `// 🔴 LUT：**只核对、不写**。…sRGB / 压缩 / maxTextureSize / **wrap** 全部是 EnsureLutImportSettings 定的 // ⚠️ B-119 已于 2026-11-01 结案：wrap 原版是 **Repeat(0)**，我们**已改过去**了 // ⇒ 下面那条警告现在是**回归哨**…⛔ 不是「已知差异」。本方法仍只核对、不写。` |
| **6141-6142**（警告文案） | `" ⇒ 账 B-119，本方法**不改**（LUT 的导入设置归 EnsureLutImportSettings）"` | `" ⇒ **回归**（账 B-119 2026-11-01 已结案、原版就是 Repeat），本方法**不改**、由 EnsureLutImportSettings 修"` |

> 那条警告代码本身**保留**（它是「有人再写回 Clamp 就出声」的哨兵），只改文案。
> 全部改动**逐行原地替换** ⇒ `ArenaBuilder.cs` 行数 **6179 → 6179（零变化）**、行尾 **CRLF 6179 / 裸 CR 0（未翻）**。

---

## ② 资产改动（`A/WarpforgeArena1/Textures/LUT/` 7 份 `.meta`）

| 文件 | wrapU | wrapV | wrapW |
|---|---|---|---|
| `LUT Battle Arena Aeldari.png.meta` | `1 → 0` | `1 → 0` | `1 → 0` |
| `LUT Battle Arena Emperors Children.png.meta` | `1 → 0` | `1 → 0` | `1 → 0` |
| `LUT Battle Arena Genestaler Cults.png.meta` | `1 → 0` | `1 → 0` | `1 → 0` |
| `LUT Battle Arena Leviathan.png.meta` | `1 → 0` | `1 → 0` | `1 → 0` |
| `LUT Battle Arena Sororitas.png.meta` | `1 → 0` | `1 → 0` | `1 → 0` |
| `LUT Battle Arena Space Wolves.png.meta` | `1 → 0` | `1 → 0` | `1 → 0` |
| `LUT Battle Arena Tau Viorla.png.meta` | `1 → 0` | `1 → 0` | `1 → 0` |

**为什么连 `wrapW` 一起改**：原版那份是 `wrapU/V/W = 0/0/0`（三轴全 0），而 `ti.wrapMode = Repeat` 这个 setter **一次写三轴**
⇒ 改完代码后**生成器产出的 meta 就是 0/0/0**。只改 U/V 会让「手上的 meta」和「下次构建产出的 meta」不一致。
⚠️ 如果调度台认为不该动 `wrapW`，回退那一行即可（`wrapW` 对 2D 贴图不参与采样，回退无副作用）。

**行尾 / 长度自证（二进制读写，前后各数一次）**：

| 指标 | 改前（7 份**全部**） | 改后（7 份**全部**） |
|---|---|---|
| `crlf` | 0 | 0 |
| `lf` | 117 | 117 |
| 裸 CR（`\r` − `\r\n`） | 0 | 0 |
| 字节数 | 2651 | **2651**（三处 `1`→`0` 等长，长度不变） |

只改了 `wrapU/wrapV/wrapW` 三行，**其它键一个没动**（读回逐张确认，见上表）。
⚠️ 这 7 份 `.meta` 被 `.gitignore:61`（`/Unity/MyGame/Assets/WarpforgeArena1/Textures/`）忽略 ⇒ **`git diff` 看不到它们**，
所以 ④ 的**资产侧断言是这条改动唯一的长效守卫**（不是「顺手加一条」，是「必须加」）。

---

## ③ 其它写入点普查（「彻底」的关键）

> 结论先说：**全仓再没有第二条路会把这 7 张写回 Clamp**；唯一的写手就是 `EnsureLutImportSettings`（已改）。

| # | `文件:行号` | 会不会把这 7 张写成 Clamp | 依据 |
|---|---|---|---|
| 1 | `ArenaBuilder.cs:3555` | **会**（改前） | `EnsureLutImportSettings` 的 `ok` 判定式。**已改 Repeat** |
| 2 | `ArenaBuilder.cs:3563` | **会**（改前） | 同一方法的写入。**已改 Repeat** |
| 3 | `ArenaBuilder.cs:6160-6161` | **不会** | `ApplyOriginalImport` 的通用支按旁挂表写 `wrapModeU/V`。**LUT 到不了这里** —— `:6137` `if (IsLutTexture(path)) { … return false; }` 提前返回；且表里那 7 行本来就是 0（`arena_texture_import.tsv:62-68`） |
| 4 | `ArenaBuilder.cs:1591-1615`（`ApplyTextureImportSettings`） | **不会** | 它确实会遍历 `Textures/LUT`（`ArenaTextureRoots` 第三项），但对 LUT：`ApplyTextureType`（`:6071`）**首句就 return**；`ApplyOriginalImport` 返回 false ⇒ `continue` ⇒ **既不写 wrap、也不 `SaveAndReimport`** |
| 5 | `ArenaBuilder.cs:6140-6142` | **不会** | 只**读** `ti.wrapModeU` 与旁挂表比对 + 出声，**没有任何赋值**。（B-119 结案后它是回归哨） |
| 6 | `ArenaBuilder.cs:6175/6178`（`ApplyAllTextureImportsFromCLI`） | **不会** | 只是循环调 `ApplyTextureImportSettings`，同上 |
| 7 | `ArtBaker.cs:475-481`（`ApplyImportSettings`） | **不会** | 只扫 `ArtDir = Assets/CardPresentation/Resources/Art`（`:26`）；`RepeatTextures` 白名单（`:56-68`）里没有 LUT。LUT 不在 `Resources/Art` 下 |
| 8 | `ArtBaker.cs:629` | **不会** | 那条方法扫描的是 `Assets/WarpforgeArena1/arenas`（`:598`），而 LUT 在 `Assets/WarpforgeArena1/Textures/LUT`（顶层，**不在** `arenas/` 下） |
| 9 | `EffectExporter.cs:3780-3785`、`:3885`（`ImportSprite`） | **不会** | 输出目录 `TexDir = Assets/WarpforgeVFX/Textures`（`:27`），与 LUT 目录**不相交**；它只重导「导进 VFX 的那份拷贝」 |
| 10 | `EffectExporter.cs:513-533`（TSV 驱动的 `FixTextureImportSettings`） | **不会** | 目标同样是 `{TexDir}/{name}.png`（`:489`），不碰 LUT |
| 11 | `LutBlendProbe.cs:361` | **不会** | `new Texture2D(...)` **内存纹理**（blit 输入），不落盘、不是资产 |
| 12 | `BattlePostFx.cs:264` | **不会** | `new RenderTexture(...)`，运行时 RT；不写任何 `.meta` |
| 13 | `Label.cs:2428` · `VoiceWaveProbe.cs:209/246/257/350` · `MenuTutorialHighlight.cs:184` | **不会** | 全是运行时/探针自建的内存 `Texture2D`/图集，与 LUT 资产无关 |
| 14 | **导入期覆盖**：`AssetPostprocessor` / `OnPreprocessTexture` / `PresetManager` | **不会** | `Assets/**/*.cs` 里 `AssetPostprocessor` / `OnPreprocessTexture` **0 命中**；`ProjectSettings/PresetManager.asset` 的 `m_DefaultPresets: {}` 为**空** ⇒ 没有「导入时按预设改 wrap」这条路 |
| 15 | `工具/import_original_luts.py:53` | **不会写这 7 份** | 它**只读** `LUT Battle Arena Aeldari.png.meta` 当模板，克隆给 **`Assets/Resources/WarpforgeVFX/LUT/`（另外 14 张 AnimFX LUT）** ⇒ 不写 `Textures/LUT/`。（⚠️ 但见「顺手发现」①） |
| 16 | `工具/gen_arena_texture_import.py` | **不会**（反而是证据） | 它重生成 `arena_texture_import.tsv`，LUT 那 7 行的 `wrapU/wrapV` 来自 **bundle 实读 = 0** ⇒ 不会把我们拉回 Clamp |

---

## ④ 断言（`A/CardPresentation/Editor/B113W13_Assert.cs`，新建 · 320 行 · **56 条** = A28 + B5 + C6 + D3 + E14）

入口 `B113W13_Assert.Run()`；`Pass` / `Fail` 供宿主累加；日志前缀 `[B113W13] `，合计行 `=== 合计：X 通过 / Y 失败 ===`。

| 组 | 读什么 | 条数 | 说明 |
|---|---|---|---|
| **A** | **盘上 7 份 `.meta` 的文本**（`WrapOf` 抠 `    wrapU: N`） | 7×4 = 28 | 每张：前提（读得到且非空）+ `wrapU/wrapV/wrapW` 各 == 0 |
| **B** | **生成器源码**：`ArenaBuilder.cs` 里 `EnsureLutImportSettings` 的**方法体**（按花括号配平抠出） | 5 | B2 抠得出（前提）· B3 含 `Repeat` · **B4 不含 `Clamp`** · B5「每一行提到 `wrapMode` 的都必须带 `Repeat`」 |
| **C** | 合成输入（**灭自证**） | 6 | `WrapOf` 必须把 `1` 读成 1、把「找不到」读成哨兵 −999、分得清 U/V 轴；`ExtractMethodBody` 必须能从合成源码里抠出带 `Clamp` 的体、且找不到签名时返回空 |
| **D** | 目录枚举 | 3 | 目录存在 + `.png.meta` 共 **7** 份 + 其中 `LUT ` 开头 **7** 份 |
| **E** | **原版**那 7 份 `.meta`（`d:/2/.../ExportedProject/Assets/Texture2D/`） | 7×2 = 14 | 前提（文件读得到）+ 原版 `wrapU/V/W` 也是 `0/0/0` |

**为什么不是同义反复（这是本条的关键）**：

1. **A 读产物、B 读生成器 —— 两个不同文件、两个不同方向。**
   「只改了 `.meta` 没改代码」⇒ **B 红**；「只改了代码没重导」⇒ **A 红**。
   一次错误改动**结构上不可能同时让两组为绿**（没有任何一处代码同时在两边）。
2. **C 组证明 A/B 的解析器不是恒真**：`WrapOf` 若恒返回 0（= 我们的期望值），C1 立刻红；`ExtractMethodBody` 若恒返回空串，C5 立刻红。
   （工程里的先例：「期望值和被测值取自同一个函数」= 两边一起错也全绿 —— 本件刻意避开。）
3. **E 组证明期望值 `0` 来自原版**，⛔ 不是「拿我们自己的产物证明我们自己」。
4. **D 组挡「删/改名后 A 组只查剩下的几份 → 悄悄变绿」。**
5. 全部是**前提硬断言在前、结论在后**；**没有** `if (x != null) Check(...)` 那种「前提不成立就静默跳过」的写法（取不到时喂哨兵 `-999`，结论照红）。

**静态复核（不跑 Unity，用等价的 Python 复现同一套算法，对真实文件跑）**：

```
marker count = 1                      # EnsureLutImportSettings 签名全文件只出现 1 次
found = True  body_len = 946
has Repeat = True | has Clamp = False
bad wrap lines = 0
7 份 meta: ['wrapU: 0','wrapV: 0','wrapW: 0']  × 7   （lf=117  loneCR=0  len=2651，逐份一致）
7 份原版 meta: wrapU/V/W = 0/0/0      × 7
```
⇒ **A/B/D/E 全组应绿；C 组四条合成输入逐条验过（1→1 / 0→0 / 缺失→−999 / 轴分得清）。**

---

## 接线请求（宿主不在我白名单，请调度台代插）

- **宿主**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`
- **行**：**46896**（现读；行内是「方法尾聚合行」）
- **安全性现读**：该行**第一个 `//` 在偏移 4564**，而锚点在偏移 ≈4500 附近（`B111_F1E_Assert.Fail;` 之后紧跟 `Debug.Log(P + $"=== 合计…`）⇒ **插入点在注释之前**，不会落进 `//`（本仓踩过 6 次「代码落进行末注释」）。

**精确替换（唯一一处，可直接 `Edit`）**：

```
old: B111_F1E_Assert.Fail; Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");
new: B111_F1E_Assert.Fail; B113W13_Assert.Run(); pass += B113W13_Assert.Pass; fail += B113W13_Assert.Fail; Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");
```

⇒ **同一物理行追加，行数零变化**；判绿红看 `[B113W13] === 合计：56 通过 / 0 失败 ===`。
⚠️ 若调度台更希望接在别的宿主（本件只读文件、无 driver 依赖、不建对象），任何宿主都能跑，改锚点即可。

---

## 验证（本波能做的全做了；Unity 由调度台跑）

| 项 | 读数 |
|---|---|
| **秒级类型检查** | `TMPDIR=/tmp/wf_w13 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0** |
| `ArenaBuilder.cs` 行数 / 行尾 | **6179 → 6179（零变化）** · CRLF 6179 · 裸 CR **0** |
| `ArenaBuilder.cs` diff | `git diff --numstat` = **8 / 8**（8 行原地改，无增删行） |
| 7 份 `.meta` | `wrapU/V/W = 0/0/0`（逐份回读）· LF 117 · 裸 CR 0 · len **2651**（改前改后等长） |
| 7 份 `.meta` 是否被 git 跟踪 | **否**（`.gitignore:61` 忽略 `WarpforgeArena1/Textures/`）⇒ 只能靠断言守 |
| `B113W13_Assert.cs` | 新建 320 行 · LF · 裸 CR 0 · **尚未接线** |

⚠️ **过程中撞到两次「别人的活」**（不是我的文件，均已自行恢复，**我没有改它们**）：
① `CardPresentation/Shell/ForgeTab.cs:1120` `CS1503 PxRect→Transform`（mtime 在我编译过程中变化，另一代理正在写）；
② `WarpforgeArena1/Editor/EffectExporter.cs:5457` `CS0103 ContainsAscii`（同上）。
两次都隔一跑重编即回到 **0/0**。

---

## 改动清单

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` | 3 处实质（判定式 / 写入 / 方法文档）+ 5 处过期注释就地订正 | **0**（8 行原地替换） |
| `Unity/MyGame/Assets/WarpforgeArena1/Textures/LUT/LUT Battle Arena {Aeldari, Emperors Children, Genestaler Cults, Leviathan, Sororitas, Space Wolves, Tau Viorla}.png.meta` | `wrapU/V/W: 1 → 0`（各 3 行） | 0（等长替换；git 忽略） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B113W13_Assert.cs` | **新建** | **+320** |
| `Unity/资料/完全复刻/报告/B113W13_LUT_wrapMode.md` | **新建**（本报告） | —— |

（临时脚本 `工具/_w13_fix_lut_wrap.py` / `工具/_w13_verify_assert_logic.py` 用完**已删**，⛔ 没留在工程里。）

---

## 顺手发现 / 尚需确认

### ① 🔴 同一缺陷还在 **14 张 AnimFX LUT** 上（`Assets/Resources/WarpforgeVFX/LUT/`）——**不在我白名单，没动**

- 现读：那 14 份 `.meta` **全部 `wrapU: 1`**（`LUT Blind` / `Blizzard` / `Blue Tint` / `Dimensional Breach` / `Invert` / `Normal` / `Nuclear` / `Overexpose High` / `Pink Emperors Children` / `Pink Emperors Children Extreme` / `Poster` / `Red Hell` / `Red Tint` / `LUT_Dark`）。
- **原版是 Repeat**，两源一致：
  - `ExportedProject/Assets/Texture2D/LUT *.png.meta` 这 13 份扫描下来 **`wrapU: 0` 全命中**（`LUT Normal` 那份导出工程里没有，但 `LUT_Dark` 等都有）；
  - `数据/游戏数据/vfx_texture_mips.tsv` 里所有 `LUT …` 行（含 `LUT Normal`，bundle = `battlesharedresources_assets_all`）**`wrapU=0 / wrapV=0`**。
- **根因**：`工具/import_original_luts.py:53-54` 拿 `LUT Battle Arena Aeldari.png.meta` 当**模板**克隆（只换 `guid`），而模板当时是 Clamp ⇒ 14 份一起错。
- **这次改完 Aeldari 那份之后**，那个脚本若重跑会**输出 Repeat**（方向是对的）；但**盘上现有 14 份还是 1**。
- **建议**：让 `Assets/Resources/WarpforgeVFX/LUT/` 的写手按同一手法改 14 份（同样是三行 `1 → 0`），或重跑一次 `import_original_luts.py`。
  ⚠️ 建议**先确认重跑会覆盖哪些文件**（它是 `wb` 写 PNG + `.meta`，会改 mtime/guid 之外的内容）。
  影响面：`BattlePostFx` 用 `Resources.Load<Texture2D>("WarpforgeVFX/LUT/<名>")` 取这几张，wrap 只影响表头/表尾半像素 —— 与战场那 7 张同理。

### ② ⚠️ 那 7 份 `.meta` 与原版还有 **2 处非 wrap 差异**（**没查清，未动**）

拿 `diff -u` 逐行比（原版 vs 我们，以 Aeldari 为例）：

| 键 | 原版（AssetRipper 导出） | 我们 | 影响判断 |
|---|---|---|---|
| 顶层 `textureFormat` | `3`（= RGB24） | `1` | **没查清 `1` 是不是等价**。两块 `platformSettings` 的 `overridden` 都是 `0`，实际生效的是 `DefaultTexturePlatform` 那块的 `textureCompression: 0`(Uncompressed) + `maxTextureSize: 256` |
| `Standalone` 块 `textureCompression` | `2`（Compressed） | `1`（Compressed） | 该块 `overridden: 0` ⇒ **不生效** |

⚠️ 还要注意：那份「原版 `.meta`」是 **AssetRipper 生成的**，不是 Unity 导出的原始 importer 设置 ——
其中 `spriteMode: 1` / `spriteMeshType: 0` / `alphaIsTransparency: 1` / `output:` 整块都是它自己的合成默认值。
**真正可当判据的是 bundle 里的 `Texture2D` 字段**（`m_ColorSpace` / `m_MipCount` / `m_FilterMode` / `m_WrapMode` / `m_Aniso` / `m_TextureFormat`），
这也是 `arena_texture_import.tsv` 的口径。⇒ 本条**只记录、不下结论**，留给后续对账。

### ③ 记账建议（调度台执行，我不改账）

- `B-119` ⇒ **可核销**（代码 + 7 份资产 + 56 条断言 = 三处都落地）。
  ⚠️ 但**断言尚未接线**（宿主不在白名单）⇒ 严格说「已实现待接线」；接线跑绿后再核销更稳。
- 新账（建议）：`AnimFX 14 张 LUT 的 wrap 也是 Clamp（原版 Repeat）` —— 判据同 ①，落点 = `Assets/Resources/WarpforgeVFX/LUT/*.png.meta` + `工具/import_original_luts.py` 的模板说明。
