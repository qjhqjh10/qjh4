# V8 — `A1300`：第三份 `TMP_FontAsset`（`RobotoCondensed-Regular SDF`）

执行代理：`V8`。**红线**：⛔ 一次 Unity 都没跑 · ⛔ 没动 git · ⛔ 没碰两张正本 · ⛔ 没改一个 `.cs`。

---

## ① 结论

**导进来了。** `Assets/CardPresentation/Resources/Fonts/RobotoCondensed-Regular SDF.asset`（+`.meta`）——
**Static（`popMode 0`）· 304 字形 / 304 字符 · 图集原版那张 512×512**，与另两份**同一套判据、同一支工具**产的。

🔴 **但它比前两份多带两样东西**（图集 PNG + 材质 `.mat`）：**工程里原本【没有】这两样** ——
全工程 `grep -rl` 它的三个原版 pathID **0 命中**（`Pragati`/`Asar` 是 `EffectExporter` 顺带导进来的，
它没有引用者、从没被导过）。这两样我落在**同一个 `Fonts/` 目录下**，而前两份在
`Assets/WarpforgeVFX/{Textures,Materials}/` —— **成因是白名单边界，不是原版的做法**，见 §④·1。

## ② 产物（6 个文件，全在 `Assets/CardPresentation/Resources/Fonts/`）

| 文件 | 大小 / 行数 | `popMode` | 字形 / 字符 | 图集 | padding | renderMode | pointSize | guid |
|---|---|---|---|---|---|---|---|---|
| `RobotoCondensed-Regular SDF.asset` (+meta) | 156724 B / **7731 行** | **0** | **304 / 304** | 512×512 | 3 | 4165 | 35.0 | `4e0f3efa47de6a695dafcfc4da373526` |
| `RobotoCondensed-Regular SDF Atlas.png` (+meta) | 142596 B | — | — | 512×512 | — | — | — | `1faf7f74b457d5b5bfc2787e6c264fc8` |
| `RobotoCondensed-Regular Atlas Material.mat` (+meta) | 3044 B / 111 行 | — | — | — | — | — | — | `f438d68202c0916827aac5183e063be7` |
| 对照 `Pragati-Regular SDF.asset` | 6457 行 | 0 | 250 / 250 | 1024×2048 | 20 | 32806 | 95.0 | 未变 |
| 对照 `Asar-Regular SDF.asset` | 6489 行 | 0 | 250 / 250 | 1024×1024 | 5 | 32806 | 94.0 | 未变 |

材质名照抄原版 = **`RobotoCondensed-Regular SDF Material`**（与前两份的 `… Atlas Material` **不同名** —— 原版自己就不一致，照抄不改）。

## ③ 判据（写读数）

**A. 三路互证 —— ⚠️ 第一路对 Roboto【不成立】，我换了更强的一路**

- ① ❌ `m_CreationSettings.referencedFontAssetGUID` = `c40768a749abc4d9986ffb980755dcaf` → `AssetBundle_1.json` 的 `m_Container` 里 **0 行**（Pragati/Asar 各 3 行；**这条不是通则**，见 §⑧·1）。
- ①′ ✅ 改用**直接链接**：该字体材质的 `_MainTex` → 图集 pathID `-103505083911436099`；含它的容器键 = **`dd3767790be4e42f289eec359047edc5`**（`preloadIndex 247` / `preloadSize 6`），该组**同时**含字体资产自身 `-6988545807179970371`、材质 `5701932905277360317`、图集 `-103505083911436099`、shader `683148379888368288`。
- ② ✅ `Material_5701932905277360317.json` 的 `m_Name` = **`RobotoCondensed-Regular SDF Material`**（原版自己点名）。
- ③ ✅ 旧解包 `d:/2/解包整理/10_字体/字体资源/Texture2D/RobotoCondensed-Regular SDF Atlas.png` 与 `assets_full` 那份 **逐字节相同**（sha256 `a9a5c764f8ca2c1f…`）；旁证 `数据/游戏数据/vfx_texture_mips.tsv:2171` = `RobotoCondensed-Regular SDF Atlas 512 512 1 fonts_assets_all.bundle` · `mat_renderqueue.tsv:784` = `RobotoCondensed-Regular SDF Material -1`。

**B. 逐字段比（独立复算，⛔ 不经生成器）**：PyYAML 重新解析**产物**、与原版 JSON 比（float32 归一 · 空串↔YAML null 归一 · 引用归一到 pathID；跳过 MonoBehaviour 头 10 个 + 刻意覆盖的 `m_Material`/`m_AtlasTextures`）⇒ **比 7086 个键 · 不等 0 处**（Pragati 5920 / Asar 5952，也都 0）。产物侧关键值：`m_Name` 对 · `m_AtlasPopulationMode 0` · `512/512` · `padding 3` · `renderMode 4165` · `m_FaceInfo.m_FamilyName = Roboto Condensed` · `m_FallbackFontAssetTable []` · `m_SourceFontFile {fileID: 0}` · `m_SourceFontFileGUID d52ff40c3d95f4596b0d22341a71c5a0`（照抄）。

**C. 材质转写规则【先自证】= 期望值不是我的生成器**：拿**本工程这台 Unity 自己写的** `WarpforgeVFX/Materials/Pragati-Regular Atlas Material.mat` 当期望值，照本脚本规则从 Pragati 的**原版**材质 JSON 重建 ⇒ **逐字节相同**（111 行；51 float + 10 color 全对）。这条过了，Roboto 那份才敢照做（只差 8 个 float：`_GradientScale 4` · `_TextureWidth/Height 512` · `_ScaleRatioA 0.75` · `_ScaleRatioB/C 0.6` · `_StencilRead/WriteMask 255` · `_WeightBold 0.8` · `_WeightNormal -0.3`）。

**D. 其余断言**：图集「工程 vs 原版」解码后 RGBA **逐位相同**（且本次是**文件字节也相同**，前两份是重编过的）· 派生 5 个 guid 与工程既有 **10012** 个 guid **不冲突** · **幂等**：连跑 4 次（含改完脚本代码前后各一次）、同目录 12 个文件 sha256 **每次全部不变** · 行尾 **CRLF 0 / 纯 LF**（脚本与报告也是）· 工具 `--verify` 三份均 `48/48 字段 · 无多余 · 字形条目==原版 · guid0 0 · CRLF 0 · popMode 0 · 引用[图集/材质] OK`。

## ④ 没查清 / 做不到的

1. 🔴 **图集与材质的落点与前两份不一致（这是本件最大的偏离）**：前两份在 `Assets/WarpforgeVFX/{Textures,Materials}/`（导出器落点），本份在 `Resources/Fonts/`。**成因 = 白名单**（本会话只准写 `Resources/Fonts/**`；`WarpforgeVFX/**` 不在里面）。**两条路的内容都对，落点是【我们挑的】**。要严格同落点：把那 4 个文件移到 `WarpforgeVFX/Textures/RobotoCondensed-Regular SDF Atlas.png` + `WarpforgeVFX/Materials/RobotoCondensed-Regular SDF Material.mat`，再把 `工具/gen_tmp_font_assets.py` 的 `FONTS` 第三行改成这两个路径重跑即可。⚠️ 反倒是：`WarpforgeVFX/**` 是 `.gitignore` 的（`.gitignore:26`）⇒ 移过去**就不入库**了。
2. ⚠️ **没跑 Unity** ⇒ 「Unity 真认这份 `.asset` / 这份 `.mat`」**没验**（红线）。本报告结论的上限 = **文件层面**（YAML 结构 + 字段值 + guid 引用 + 行尾）。最便宜的复核 = 主对话跑一次 `BoosterPackExporter.Run`，看 `TMP：字体 接回 …` 那行会不会把 `RobotoCondensed` 也算进去（**今天不会** —— 两扇窗里没有它）。
3. ⚠️ **源 `.ttf` 仍然全库零命中** ⇒ `m_SourceFontFile {fileID: 0}`；两个 GUID 字符串照抄原版（解不到任何东西，但是原版真值）。Static 下 TMP 运行时不读源字体。
4. ⚠️ **`referencedFontAssetGUID` 指向哪里没查清**：`c40768a749abc4d9986ffb980755dcaf` 在 `assets_full` **只出现在它自己那份 JSON 里**（其余全库零命中）⇒ 是个解不到的字符串，照抄保留。
5. ⚠️ 材质/贴图放进 `Resources/` ⇒ **会随包打出去**；前两份同样进包（被 `.asset` 引用）。**打包差异没验过**。
6. ⚠️ **这份 `.asset` 今天仍然没有任何使用者**（`grep -rl` 三个原版 pathID = 0 命中）⇒ 只是「备好了」，要起作用得等 `A1097③`（fallback 链 / 按用途选字体）。**本件一个字没改 `.cs`**。

## ⑧ 顺手发现（只报，⛔ 没自己改）

1. 🔴 **`fix_booster_prefabs.py:21-24` + `V3_Booster两笔.md:84` 那条判据「用 `m_CreationSettings.referencedFontAssetGUID` 当容器键」不是通则** —— 对 Pragati/Asar 各 3 行、对 Roboto **0 行**；拿它当唯一判据会在第三份上停手。
2. ⚠️ Roboto 的 `m_AtlasRenderMode = 4165`（SDFAA）而 Pragati/Asar = `32806`；`TmpSetup.cs:53` 早记过「只有它和 RobotoCondensed 用 SDFAA」—— 两条独立对上。
3. ⚠️ `AssetBundle_1.json` 的 `m_Container` 形状不一致：Pragati/Asar 的同一个 `(preloadIndex, preloadSize)` **重复 3 行且各自带不同 `asset.m_PathID`**，Roboto 只有 1 行 ⇒ 读它的脚本别假设「一行 = 一个资产」。
4. ⚠️ `V3_Booster两笔.md:115` 说第三份「512×512、304 字形」—— 现读**对上**（304/304 · 512×512）；它没提的两条 = `renderMode 4165` · `popMode 0`。
