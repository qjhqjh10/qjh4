# B9_W59ARENA —— 战场贴图旁挂三处真缺陷修复（+ 途中又抓到的三处）

> 执行代理：**B9_W59ARENA**（2026-10-27）· 承接 `报告/B9_W45ARENA_check补齐.md` §结论-4（「新 check 一上线就抓到两处真缺陷」）
> 任务：① 重跑 `工具/gen_arena_sprites.py` 补齐 11 场缺失产物 ② `ArenaBuilder.BuildSprites` 静默失败改出声
> ③ 重跑 `工具/gen_arena_meshkeywords.py`（astra 过期）④ 逐场核产物 + `--check` 验绿
> 口径：**没跑 Unity、没动 git、没碰别人的文件**；产物全部**由生成器写**（⛔ 没手改 JSON）。

---

## 结论

1. **三处指定缺陷全部修完，两个生成器 `--check` 现读 rc=0（绿）**：
   - ① **11 场缺 `<场>_sprites.json`** ⇒ 已逐场重跑，**13/13 齐**（36 个 sprite 条目）。
   - ② **`battlearenaastramilitarum_sprites.json` 旧格式** ⇒ 已重生成，**13 场 0 条缺 `active` 键**。
   - ③ **`battlearenaastramilitarum_meshkeywords.json` 过期** ⇒ 已重生成，**78 → 80 条**，
     补上的正是 W45 点名的 `Generator ` / `Bunker Gun `（**名字带尾随空格**，逐字对上）。
2. 🔴 **但 W45 对「缺陷 ②」的因果推断是错的，本件当场推翻并改了代码注释**：
   缺 `active` 键 **不是**「`JsonUtility` 读成 `false` ⇒ 整族静默跳过」，而是
   **「保持 C# 字段初始化值 `true` ⇒ 全部当 active ⇒ 原版关着的会被多建」**。
   **实证**：astra 那份旧格式 json 在 **2026-10-26 的建场**里建出了 **9 个 `SpriteRenderer`**
   （`Scenes/battlearenaastramilitarum.unity` 与 `Resources/ArenaPrefabs/battlearenaastramilitarum.prefab`
   **各 9 个 `!u!212`**，引用 `Glow.png` ×4 / `Glow Scattered.png` ×5），
   正好 = **10 条 − 1 条「无贴图」** ⇒ 缺键时 `active` 必须是 `true`（若是 `false` 就是 0 个）。
   ⇒ 这条**已从「真缺陷」降级为「格式隐患」**：astra 的实际产物**一个都没少**。
3. 🔴 **真正让 sororitas 缺东西的是另一条 —— 图集（SpriteAtlas）精灵的贴图从来没被解出来过**。
   11 场那 1 个 `Crosshair` 与 sororitas 的 4 个 `Bottom Filler *`，`m_RD.texture` 都是
   `{m_FileID:0, m_PathID:0}`（像素在图集里，没有独立 Texture2D）⇒ 生成器原来把 `tex` 写成空串
   ⇒ `ArenaBuilder.LoadSpriteAsset("")` 直接返 null ⇒ **建不出来**。
   **本件把图集裁切补上了**（判据见 §证据-E），并且**逐张裁完自检尺寸 == Sprite 的 `m_Rect`**。
   ⇒ **建场后实际会建出来的精灵：11 个 → 14 个，净增 3 个（全部在 sororitas）**。
4. **本件实际画面增量（给同步点一个可核的数）**：

   | 场 | 改前建出的 SR | 改后应建出 | 增量 | 来源 |
   |---|---|---|---|---|
   | `battlearenaastramilitarum` | **9** | **9** | **0** | sprites 无变化（见结论 2）；`meshkeywords` 多 2 条网格关键字（不增对象） |
   | `battlearenasororitas` | **0** | **3** | **+3** | 3 个 `Bottom Filler *`（图集裁切后才有贴图） |
   | 其余 11 场 | 0 | 0 | 0 | 那 1 条 `Crosshair` 原版 `activeInHierarchy=False` ⇒ 正确做法就是**不建** |
   | `battlearenatauviorla` | 2 | 2 | 0 | `Sun Sprite` / `Sun Glow`（本来就对） |
   | **合计** | **11** | **14** | **+3** | —— |
5. **两个生成器都验过「两遍幂等」**（35 个产物文件两次跑完 md5 逐一相同）与**红路径**（移走产物 → 🔴 点名 + rc=1）。
6. 🔴 **途中又抓到三处**（都不在任务清单里，判据已查清 ⇒ 按铁律 14 顺手做掉）：
   - **`--check` 的红行在本机 GBK 控制台会 `UnicodeEncodeError` 崩掉**（点名一行都没印出来）⇒ 已在
     `gen_arena_sprites.py` / `gen_arena_meshkeywords.py` 无条件 `sys.stdout.reconfigure(utf-8)`。
     ⚠️ **另外 7 个同族脚本还有同一个坑**（不在本件白名单，见 §顺手发现-1）。
   - **图集精灵的贴图解不出来**（= 结论 3）⇒ 生成器补 `atlas_crop()`。
   - **`SpriteRenderer.m_Size` 从来没被建**（JSON 里有 `size` 字段、`ArenaBuilder` 一行都没用过）
     ⇒ `sr.size` 已补；影响面**只有** sororitas 那 3 个 `m_DrawMode=2`(Tiled) 的条目，其余 33 条是 Simple（Unity 忽略该值）。
7. `ArenaBuilder.cs` 类型检查 **0/0**。

---

## 证据（文件:行号）

### A. 缺陷 ①：11 场没有 `_sprites.json`（改前现读）
改前 `ls */[场]_sprites.json`：**只有 2 份**——
`battlearenaastramilitarum`（2026-09-25 10:20）· `battlearenatauviorla`（2026-09-25 10:22），
其余 11 场**全缺**。`gen_arena_sprites.py --check` 改前点名 **11 条「缺产物」+ 1 条「产物过期」**、rc=1。
- 静默点：`WarpforgeArena1/Editor/ArenaBuilder.cs:5340` 原 `if (!File.Exists(path)) return 0;`（一行不打）。

### B. 缺陷 ②（**被本件推翻**）：旧格式的真正语义
- 改前 astra json：10 条**全部没有 `active` 键**（`git show HEAD:..._sprites.json`），
  其中 9 条 `tex` 是 `Glow` / `Glow Scattered`，第 10 条 `Crosshair` 的 `tex` 是 `""`。
- **产物实证**（不是推理）：`Scenes/battlearenaastramilitarum.unity` 与
  `Resources/ArenaPrefabs/battlearenaastramilitarum.prefab`（mtime **2026-10-26 02:55**）
  **各有 9 个 `--- !u!212 `（SpriteRenderer）**，`m_Sprite` 的 guid 落点：
  `b131682a…` = `arenas/battlearenaastramilitarum/Sprites/Glow.png.meta`（4 个）·
  `e433d834…` = `Glow Scattered.png.meta`（5 个）。
  ⇒ **9 = 10 − 1（无贴图那条被 `LoadSpriteAsset` 返 null 跳过）** ⇒ 缺 `active` 键时**读到 `true`**
  （`ArenaBuilder.cs:5176` 的 `public bool active = true;` 字段初始化值），**不是 `false`**。
- ⇒ W45 报告 §「🔴 新 check 一上线…」第 2 条与 §顺手发现-2 的因果**作废**；
  本件已把 `ArenaBuilder.cs:5329-5339` 的注释与 `:5359-5375` 的 Warning 文案改成正确口径。

### C. 缺陷 ③：astra meshkeywords 过期（已修）
- 改前盘上 **78 条**，重算 **80 条**；`git diff` = **+16 行 / 0 行**，新增的**只有这两条**：
  `{"go": "Generator ", …}` 与 `{"go": "Bunker Gun ", …}`，关键字都是
  `_APPLYAMBIENTCOLOR,_SURFACE_TYPE_TRANSPARENT`（**名字确实带尾随空格**，与 W45 描述逐字一致）。
- 全 13 场按**盘上**合计：**686 → 688 条**（`--check` 重算口径本来就是 688 条，rc=0）。

### D. `ArenaBuilder.BuildSprites` 静默失败（改后）
`WarpforgeArena1/Editor/ArenaBuilder.cs`：
- `:5345` 旁挂**不存在** → `Debug.LogWarning`（点名场次 + 告诉重跑哪条命令）
- `:5358` 旁挂**解析不出 `sprites`** → `Debug.LogWarning`
- `:5364-5375` **旧格式探测**（按 `"active"` 出现次数与条目数配平；**只出声、不改语义**）
- `:5417` 有 `nSkip` ⇒ `Debug.LogWarning`（原来是一行普通 `Log`）
- `:5396` `sr.size` 补建（`e.size` 原来**一处未用**）

### E. 图集（SpriteAtlas）精灵 ← 本件新增的判据（逐条核过）
- 两份目标 sprite 的 `m_RD.texture` 都是 `{m_FileID:0, m_PathID:0}`，而 `m_SpriteAtlas` 非空：
  `Card Text smooth background` → SpriteAtlas `0_GeneralUI Atlas`（`m_RenderDataMap` **139** 条）·
  `Atack_Icon BW` → `BattleAtlasUI`（**60** 条）。
- `m_RenderDataKey` = `({'data[0]'..}, 21300000)`（**二元组**），`m_RenderDataMap` 每条 =
  `[(同形状二元组), atlasData]`，`atlasData` 里有 `texture`（图集大图 PPtr）+ `textureRect`。
- `settingsRaw` 位域 `packed=bit0 / packingMode=bit1-2 / packingRotation=bit3-4`：两份实测都 = **1**
  ⇒ packed / Tight / **无旋转**；`textureRectOffset` = 0 且 `textureRect` **与 `m_Rect` 逐值相等**
  （32×32 / 180×182）⇒ **直接按 `textureRect` 裁整块就是这张 sprite**。
- 裁出来的两张已**人眼核过**：`Atack_Icon BW` 是一张准星图；`Card Text smooth background` 是
  32×32 的纯色块（该 SR 的 `m_Color` 是 `(0,0,0,1)` ⇒ 原版画出来就是**黑块**）。
- 落到代码：`工具/gen_arena_sprites.py:264`（`atlas_crop`）· `:406`（在 SR 循环里用）·
  `:520-537`（导出时裁切 + **自检尺寸 == `m_Rect`，不符就不写并 🔴 出声**）。

### F. `m_Size` 没被建
- `SpriteEntry.size` 由生成器一直在写（`gen_arena_sprites.py` 的 `'size': v3(sr.get('m_Size', {}))`），
  而改前 `ArenaBuilder.cs` 里 `e.size` **0 处引用**（`grep '\.size\b' ArenaBuilder.cs` 只命中粒子那句）。
- 实测受影响的只有 3 条：sororitas 的 `Bottom Filler *`，`drawMode=2`(Tiled)，`size` 分别
  `(2.729, 0.351)` / `(1.740, 0.196)` / `(2.729, 0.351)`（**不是默认 1×1**）。其余 33 条 `drawMode=0`。
- Tiled/Sliced 还要求 Sprite 的 Mesh Type 是 **FullRect** ⇒ `LoadSpriteAsset` 加了 `drawMode` 形参，
  用 `TextureImporterSettings` 读改写（⚠️ `TextureImporter.spriteMeshType` 在 Unity 6000.3 **不存在**，
  第一版按它写直接 `CS1061`，已改）。

### G. GBK 控制台崩溃（`--check` 红路径）
改前跑 `--check`（有红）会得到：
```
UnicodeEncodeError: 'gbk' codec can't encode character '\U0001f534' in position 2
  （gen_arena_sprites.py:78 / gen_arena_meshkeywords.py:106，都是 wf_report_artifacts 的 print）
```
⇒ 点名一行都看不到，只看到 traceback。已在两文件的顶部（`:43` / `:54`）
**无条件** `sys.stdout.reconfigure(encoding='utf-8', errors='replace')` 修掉；复验见下。

---

## 13 场 × `_sprites.json` 前后对照表

> 「会建」= `active == true` **且** 解析得到贴图（`tex` 非空且 `Sprites/<tex>.png` 在盘）。

| 场 | 改前 json | 改前会建 | 改后条目 | 其中 active | 改后会建 | `Sprites/*.png` | 备注（改动后的实况） |
|---|---|---|---|---|---|---|---|
| battlearena1 | **缺** | 0 | 1 | 0 | 0 | 1 | 唯一对象 `Crosshair`（原版 inactive） |
| battlearena2 | **缺** | 0 | 1 | 0 | 0 | 1 | 同上 |
| battlearena3 | **缺** | 0 | 1 | 0 | 0 | 1 | 同上 |
| battlearenaaeldari | **缺** | 0 | 1 | 0 | 0 | 1 | 同上 |
| battlearenaastramilitarum | 在（**旧格式**） | 9 | 10 | 9 | 9 | 3 | 9 个 `Fake Light Glow`（改前改后**都是 9**） |
| battlearenablacklegion | **缺** | 0 | 1 | 0 | 0 | 1 | `Crosshair` inactive |
| battlearenadarkangels | **缺** | 0 | 1 | 0 | 0 | 1 | 同上 |
| battlearenaemperorschildren | **缺** | 0 | 1 | 0 | 0 | 1 | 同上 |
| battlearenagenestealers | **缺** | 0 | 1 | 0 | 0 | 1 | 同上 |
| battlearenaleviathan | **缺** | 0 | 1 | 0 | 0 | 1 | 同上 |
| battlearenasororitas | **缺** | 0 | 4 | 3 | **3** | 2 | **3 个 `Bottom Filler *` 新增**（Tiled + 图集裁切） |
| battlearenaspacewolves | **缺** | 0 | 1 | 0 | 0 | 1 | `Crosshair` inactive |
| battlearenatauviorla | 在（新格式） | 2 | 12 | 2 | 2 | 3 | `Sun Sprite` / `Sun Glow`；9 个 `Fake Light Glow` 原版就是关着的 |
| **合计** | **2/13** | **11** | **36** | **14** | **14** | **18** | 净增 **+3** |

**「0 条缺 `active` 键」**：13 份 json 全量统计 `noKey = 0`（改前 astra 那份是 10）。
**「无 tex 条目」**：改前**只有 2 份 json**（共 22 条）里 **2 条**无贴图（astra 的 `Crosshair` · tauviorla 的 `Crosshair`）；
改后 **36 条里 0 条**（两个 `Crosshair` 都靠图集裁切解出来了）。
**`--check` 复验**（两个都 rc=0、零红行）：
```
python 工具/gen_arena_sprites.py --check        → RC=0，合计：36 个 sprite 对象
python 工具/gen_arena_meshkeywords.py --check   → RC=0，合计：688 个网格带原版关键字
```
**幂等复验**：13 场各跑一遍 → 取 35 个产物（13 json + 18 PNG + 4 既有 `.meta`）md5 → **再跑一遍 → diff 为空**。
**红路径复验**：临时移走 `battlearena1_sprites.json` / `battlearenaastramilitarum_meshkeywords.json`
⇒ 两脚本各 `rc=1` 并**点名那条路径**（无 traceback），随即还原。

---

## 同步点要跑哪几场的 `ArenaBuilder` 建场

> ⛔ 本件**没跑 Unity**。以下由调度台执行（铁律 15：**一场一场**，⛔ 别 `--all`）。

| 顺序 | 场 | 为什么 | 命令 |
|---|---|---|---|
| 1 | `battlearenasororitas` | 唯一**画面真的会变**的一场：新增 3 个 `Bottom Filler *` | `WF_ARENA=battlearenasororitas` → `ArenaBuilder.BuildFromCLI` + `ArenaBuilder.BuildPrefabFromCLI` |
| 2 | `battlearenaastramilitarum` | 精灵不变（9→9），但 **meshkeywords 多 2 条网格**（`Generator ` / `Bunker Gun `）⇒ 关键字要在建场期挂上去才生效 | `WF_ARENA=battlearenaastramilitarum` → `BuildFromCLI` + `BuildPrefabFromCLI` |
| — | 其余 11 场 | **判过「真的没变」**：那 1 条 `Crosshair` 原版 inactive ⇒ 建出来 0 个对象。**不需要重建**（省 11 场） | —— |

**建完要核的三件事（都可静态读盘、不需要跑 Unity 运行）**：
1. `grep -c '^--- !u!212 ' Resources/ArenaPrefabs/battlearenasororitas.prefab` ⇒ **0 → 3**；
   `Scenes/battlearenasororitas.unity` 同 ⇒ **0 → 3**。
2. `battlearenaastramilitarum` 的 `!u!212` 计数 ⇒ **9 → 9**（若变 10 = 旧格式探测那条路又出问题；
   若变 8/9 以外 = 图集裁切出错）。
3. 建场日志里**不许出现**本件新加的 `Debug.LogWarning`：
   `没有 _sprites.json` / `是**旧格式**` / `**跳过 N 个（贴图读不出）**`。

⚠️ **本件没有新增/重生成任何 `.mat` 资产**（`GetOrCreateSpriteMaterial` 是内存里 `new Material`），
⇒ 按 `CLAUDE.md` 那条「改完 ArenaBuilder 必须跟一次 `BattleScene.BuildAndSaveScene`」的**理由**（材质 GUID）
本件不触发；但这是**调度台**的判断项，本件只声明事实。
⚠️ **Unity 首次刷新会新生成 `.meta`**：11 个 `Sprites/` 目录、11 份新 `_sprites.json`、18 个 PNG
（astra / tauviorla 那 4 个旧 `.meta` 不变）。这些 `.meta` 是**产物的一部分**，别当成脏改动删掉。
⚠️ **战场线的口径**：用户 2026-10-25「战场线可以复刻，只是暂停的是**验收**」⇒ 本件是**复刻**，
⛔ **不是验收通过**；亮度比 / 并排比那套照旧不做。

---

## 没查清的部分

1. **`ArenaBuilder` 那三处 Warning 与 `sr.size` 只在「编译通过」这一层验过 —— 没跑 Unity**（本件红线）。
   「缺键 ⇒ `true`」这一条**是从 2026-10-26 的产物反推的**（9 个 `!u!212`），
   本件**没能**在被禁止的条件下做个 `JsonUtility` 对照实验。
   ⇒ 若同步点建场后 astra 的 `!u!212` 从 9 变 10，就说明这条反推错了，**回来改**（见上「核的三件事」第 2 条）。
2. **`SpriteMeshType.FullRect` 这条只能在 Unity 里见效**（Tiled 要求 FullRect 是 Unity 文档口径，
   但本件**没有实机验证**「不设会画错到什么程度」）。同步点建完请看一眼 sororitas 那 3 个黑块**大小/位置**对不对
   （`sr.size` = (2.729,0.351) / (1.740,0.196) 局部单位 × 变换缩放 10.83 / 30.95）。
3. **astra 那 2 条网格新关键字到底改多少画面 —— 判不出**。判据只到「原版材质 `m_ValidKeywords` 里有这 2 个」，
   `_APPLYAMBIENTCOLOR` / `_SURFACE_TYPE_TRANSPARENT` 本来就是 `ArenaOriginalMaterial.ApplyRenderState`
   会自己算的四个里的两个 ⇒ **大概率视觉增量 = 0**（但本件**没有实测亮度比**，按战场线口径也不做验收）。
4. **9 个 `Atack_Icon BW.png` 是同一张图各存一份**（每场 `Sprites/` 各一份，180×182）。
   这是生成器既有设计（PNG 必须落在各场 `Sprites/` 下、且 `ApplyTextureImportSettings` 对 `Textures/`
   会强设成 Default）⇒ **没改**，只是如实记一笔体积（约 9 × 十来 KB）。

---

## 改动清单

| # | 文件 | 改的是什么 | 行尾 |
|---|---|---|---|
| 1 | `工具/gen_arena_sprites.py` | ① `--check` 红行 GBK 崩溃修掉（`:43` 无条件 `reconfigure`）② **图集精灵裁切**：新增 `atlas_crop()`（`:264`）+ 在 SR 循环里用（`:406`）+ 导出时裁切并**自检尺寸==`m_Rect`**（`:520-537`）③ `Sprites/` 目录改成「真要写文件才建」（原来无条件 `makedirs`，11 场会留空目录） | LF 保持（551 行，`bareCR=0`） |
| 2 | `工具/gen_arena_meshkeywords.py` | `--check` 红行 GBK 崩溃修掉（`:54` 无条件 `reconfigure`）；**其余一字未改** | LF 保持（244 行，`bareCR=0`） |
| 3 | `WarpforgeArena1/Editor/ArenaBuilder.cs` | ① `BuildSprites` 三处静默 → 出声（`:5345` 缺文件 / `:5358` 解析失败 / `:5417` nSkip）② 旧格式探测 + **正确口径的注释**（`:5329-5339` / `:5359-5375`）③ `sr.size` 补建（`:5396`）④ `LoadSpriteAsset` 加 `drawMode` 形参、Tiled/Sliced 用 `TextureImporterSettings` 设 **FullRect**（`:5252-5280`） | CRLF 保持（5977 行全 CRLF，`bareCR=0`） |
| 4 | `WarpforgeArena1/arenas/<11 场>/<场>_sprites.json` | **新建**（11 份，生成器产出） | 生成器写 `newline='\n'` |
| 5 | `WarpforgeArena1/arenas/<11 场>/Sprites/Atack_Icon BW.png` | **新建**（11 份，图集裁切 180×182） | 二进制 |
| 6 | `WarpforgeArena1/arenas/battlearenasororitas/Sprites/{Card Text smooth background,Atack_Icon BW}.png` | **新建**（图集裁切 32×32 / 180×182） | 二进制 |
| 7 | `WarpforgeArena1/arenas/battlearenaastramilitarum/{_sprites,_meshkeywords}.json` | **重生成**（sprites：+`active` 10 行 + crosshair 的 `tex`/`texPathId`；meshkeywords：+2 条 / +16 行） | 生成器写 |
| 8 | `WarpforgeArena1/arenas/battlearenaastramilitarum/Sprites/Atack_Icon BW.png` | **新建**（图集裁切） | 二进制 |
| 9 | `WarpforgeArena1/arenas/battlearenatauviorla/{_sprites.json,Sprites/Atack_Icon BW.png}` | **重生成**（json 只变 2 行：crosshair 的 `tex` 由 `""` → `Atack_Icon BW`、`texPathId` 由 0 → 图集贴图 id） | 同上 |

**没写的东西（明说）**：⛔ 没写账补丁 `_parts/B9_W59ARENA.tsv` —— 本件动的是**生成物 + 建场工具**，
账的 A 档落点要求「原版**类 + 方法签名** + `文件:行号`」，这五处缺陷都**对不上一个原版方法**
（`m_Size` 是 Unity 内建组件的字段、图集裁切是**我们工具**的能力缺口）⇒ 硬凑一行会污染账。
⇒ 需要入账的**分流给调度台**（建议按「战场线·散件」记，不要在账里造假落点）。

---

## 顺手发现

1. 🔴 **`--check` 红行在 GBK 控制台崩掉 —— 这是整个 `gen_arena_*` 族的通病，本件只修了自己那 2 个**。
   `wf_report_artifacts` 里 `print('  🔴 …')` 在 `cp936` 控制台会抛 `UnicodeEncodeError`（红路径直接 traceback）。
   现读**已有全局 `sys.stdout.reconfigure`** 的只有 3 个：`gen_arena_groups` / `gen_arena_negscale` / `gen_arena_vertexcolors`。
   **还缺的 7 个**（都在本件白名单外，⛔ 没动）：
   `gen_arena_texslots.py`（`:111-121` 有 `🔴`、`:40` 只在 help 路径 reconfigure）·
   `gen_arena_quality_toggle.py` · `gen_arena_mirror.py` · `gen_arena_psmesh.py` ·
   `gen_arena_shadowflags.py` · `gen_vfx_texture_mips.py` · `gen_module_events.py`。
   ⇒ 建议派一个持有这批文件的代理，**每个文件加 3 行**（照 `gen_arena_groups.py:96`）。
   ⚠️ 这也解释了为什么 W45 报告说「红绿双验 12/12 全做过」却没发现：他大概带着 `PYTHONIOENCODING=utf-8` 跑的。
2. 🔴 **`JsonUtility` 对「缺的键」是「保持字段初始化值」，不是「填默认值」** ——
   这条纠正 W45 报告，也会影响所有「旁挂加字段」的判断：**旧格式不会静默关掉一整个族，反而会「全开」**
   （`bool x = true` 的字段缺键 ⇒ true；`bool x;`（无初始化）缺键 ⇒ false）。
   ⇒ 以后判「老文件会不会静默退化」，**必须先看那个字段有没有初始化值**，⛔ 不能一律按 `false` 推。
   ⚠️ 本件**没能改** `资料/已知的坑.md`（不在白名单），建议调度台把这条并进去
   （W45 报告 §顺手发现-2 的措辞是反的）。
3. **`ArenaBuilder.BuildSprites` 里 `LoadSpriteAsset("")` 是第二个静默点** ——
   空 `tex` 时它 `return null` **一个字不打**（第 5254 行），只有最外层聚合的 `nSkip` 会提到。
   本件把聚合那行升级成 `LogWarning` 了，但**逐条**还是不知道是谁 ⇒ 若要更细可以再做（本件没做，避免每场刷屏）。
4. **sororitas 的 3 个 `Bottom Filler` 原版颜色是纯黑 `(0,0,0,1)`** —— 建出来是**黑块**。
   这不是 bug（原版就是黑的），但同步点第一眼看到「三个黑方块」别当成做错了。
5. **`gen_arena_sprites.py` 的 `--check` 会写 `d:/4/_tmp_view/sprite_index.json`**（脚本自己的缓存，
   W45 报告已记）。本件第一次跑时缓存**已存在**（2026-10-27 19:06），没有再重建。
6. **`Sprites/` 下的空目录是本件改前那次跑留下的**（`makedirs` 无条件调用）——
   现在 11 场各有 1 个 PNG，**没有留下空目录**（已逐场 `ls` 核过）。

---

## 复核命令（可复跑，全部只读或幂等）

```bash
PY="d:/2/Warpforge_tools/py312/python.exe"
"$PY" d:/4/Unity/工具/gen_arena_sprites.py --check          # 期望 rc=0、36 个
"$PY" d:/4/Unity/工具/gen_arena_meshkeywords.py --check     # 期望 rc=0、688 个
TMPDIR=/tmp/wf_w59 bash d:/4/Unity/工具/typecheck.sh        # 期望 0/0
grep -c '^--- !u!212 ' d:/4/Unity/MyGame/Assets/Resources/ArenaPrefabs/battlearenasororitas.prefab   # 建场前 = 0
```
