# RECON · `A799` 与 `A562` 两笔待裁账（只读现核）

> 代理：只读现核 · 2026-10-18（第三会话）· ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改任何代码/数据/正本。
> 唯一写盘 = 本文件。唯一跑过的进程 = 只读 python（UnityPy 实读 bundle，**零写盘**，产物只在内存里）。

---

## 一、`A799`

### 1. 那 5 处到底是哪 5 处（判据 = `资料/普查产出_1015/R2_A799全量表.md` §一① #4–#8）

R2 §一① 的「会新裁」生产 9 处里，属于本账的 **#4–#8 = 5 处**（#1–3 在 `PurchasePremiumWindow`/`RankedRewardEventWindow`，
#9 在 `SocialWindow` ⇒ 不属本 5 行）。**逐处落到符号**（不是行号 —— R2 的行号已漂）：

| # | 站点（符号） | 生产实现处（现读） |
|---|---|---|
| #4 | `LeaderboardRow.Build` → `MenuDraw.Text(row, …, "Ranking", …)` | `Shell/LeaderboardRow.cs:173` |
| #5 | 同 builder → `"Name"`（`holder` 之下） | `Shell/LeaderboardRow.cs:221` |
| #6 | 同 builder → `"Guild Name"`（`holder` 之下） | `Shell/LeaderboardRow.cs:233` |
| #7 | 同 builder → `"Points"`（`row` 直接作父） | `Shell/LeaderboardRow.cs:248` |
| #8 | `MatchLogRow.Text`（`Match Log` 每一行**所有字段共用的 builder**） | `Shell/MatchLogRow.cs:164`（调用方：`BattleLogPopup._content` / `BattleLogTab._content`） |

### 2. 逐处判定 —— 有没有一条断言**真的**在钉「部分越界 ⇒ mesh 被夹到视口沿」

| # | 站点（符号） | 断言有了吗 | 在哪（`文件:符号`/行号） | 判据（怎么分两态 · 怎么改坏会让它红） |
|---|---|---|---|---|
| #4 | `LeaderboardRow.Build` · `"Ranking"` | ✅ **有** | `Editor/MainMenuScene.cs` §A833(C) · `:6058`（★ 上下界）+ **`:6061` `CheckNear(eMaxY, VpBot, 0.5f, …)`**；`:6023` 的 `foreach (a833Field in {"Ranking","Points"})` | 量 **TMP 渲染网格顶点**（`ShellScene.TmpSpanPx`，⛔ 不是 `Label.WorldW`/`textBounds`）。**两态成对**：`a833Ref`（整颗在视口内的参照行，`:6052` 断「没被夹」）⟷ `a833Edge`（压出下沿 55px）。**改坏法**：把 `LeaderboardRow.cs:173` 那句改成喂 `MenuDraw.NoClip` ⇒ `eMaxY` 回到不裁位 ⇒ 红；或删 `MenuDraw.Text` 末尾的 `ClipText` ⇒ 红。另配 `:6074`（只截下沿）、`:6083`（真被截短过）。 |
| #5 | `LeaderboardRow.Build` · `"Name"` | ✅ **有**（**在上沿档**） | `Editor/MainMenuScene.cs` §A833(D) · `:6152`（★ `eMinY >= VpTop − 0.5`）+ **`:6155` `CheckNear(eMinY, VpTop, 0.5f, …)`**；夹具 `A833TopOver = 28f`（`:6104`） | 同上量法。**两态成对**：`:6150` 断参照行 `Name` 没被夹 ⟷ `:6123` 断最高那颗行顶边越出上沿 >10px。**改坏法**：把 `LeaderboardRow.cs:221` 那句喂 `MenuDraw.NoClip` ⇒ 顶点越过上沿 ⇒ 红。另配 `:6161`（只截上沿）、`:6166`（真被截短过）。<br>⚠️ **为什么换到上沿**：下沿档可见区 `0..45`，`Name` 字墨 `≈14.6..51.8` 与 `Ranking`/`Points` 字墨在同一沿位**互斥**（`:6018-6022` 作者自陈）⇒ 这是**实质判断**。 |
| #6 | `LeaderboardRow.Build` · `"Guild Name"` | ❌ **没有**（只有**另外那一态**） | 最接近的是 §A833(B) · `:6005` `CheckTrue(a833Edge != null && FindChild(a833Edge, "Guild Name") == null, …)` | 它钉的是「**整块在框外 ⇒ 连节点都不建**」（`MenuDraw.Text` 开头 `if (!Visible(r, _st.RenderClip)) return null;`），配 `:6012` 的「参照行里它建出来了」。**这正是题面点名「不算」的另两态之一** ⇒ 本处「部分越界 ⇒ mesh 被夹到沿」**一条都没有**。 |
| #7 | `LeaderboardRow.Build` · `"Points"` | ✅ **有** | 同 #4（同一循环体，两个字段各跑一遍） | 同 #4。 |
| #8 | `MatchLogRow.Text`（12 颗共用） | ✅ **有**，且**两扇宿主各一份** | ① `Editor/MainMenuScene.cs` §A833（`BattleLogPopup` 宿主）· `:9598`（★）+ **`:9601` `CheckNear(eMaxY, MBot, 0.5f, …)`**；`:9567` 的 `foreach (mSide in {"Player Info","Enemy Info"})` 两边各一遍<br>② 同文件 §A833（`BattleLogTab` 宿主）· `:4261`（★）+ **`:4264` `CheckNear(eMaxY, BltVpBot, 0.5f, …)`**；`:4238` 同两半边 | 量法/形状**逐字同 #4**（作者 §3 自陈「逐字照抄、没另造口径」）。两半边各走一遍是因为 `ClanSz` 我方 250×50 / 敌方 338×50 是**两个不同矩形**。**改坏法**：把 `MatchLogRow.cs:164` 那句换成不沿父链解析的内层（或喂 `NoClip`）⇒ 红。另配「只截下沿」「真被截短过」两条 + 参照行对照。 |

**跑绿证据**：`资料/交接_1018第三会话.md` §一 —— `MainMenuScene` **2550 / 0 ✅**（此前 2487/12，12 条全是 `A983` 的量错轴之红，已修）。⇒ 表里这些断言**已实跑过、是绿的**。

### 3. 结论：`A799` 可不可以销账

**❌ 不可销 —— 还差 1 处：`#6` `LeaderboardRow.Build` 的 `"Guild Name"`（`Shell/LeaderboardRow.cs:233`）。**

- 其余 **4/5 处**（`Ranking` / `Name` / `Points` / `MatchLogRow.Text`）**已经**有钉「部分越界 ⇒ mesh 被夹到视口沿」的断言，且都是**两态成对**（参照行对照）+ 三条（停在沿上 / 只截一侧 / 真被截短过），改坏法明确 ⇒ **这 4 行可以销**。
- `#6` 现在只有「整块在框外 ⇒ 不建节点」那一态。**要补的是一条新档**（现成的两条档都够不着它）：
  - **可行做法**（判据齐，照 §A833(C) 的形状即可）：把夹具的下沿压到**行内 ≈70**（而不是现在的 45）——
    此时行内可见区 `0..70`：`Guild Name` 框 `50..96` **部分越界 ⇒ 该被夹**，而 `Name` 框 `6..52` **整颗在视口内 ⇒ 不该被夹**
    （正好又是一对两态，比现在 (B) 那一档判别力更强）。框值出处 = `Shell/LeaderboardRow.cs:98`（`NameR = 6..52`）/ `:100`（`GuildR = 50..96`）。
  - 落点 = `Editor/MainMenuScene.cs` 的 §A833 段（宿主 `MainMenuScene.Run`，已跑绿的那一条）。
  - ⛔ 不要把它塞进 (B) 那一档 —— (B) 的存在价值正是「整块不建」这一态。

> 📌 一句话：**`A799` 的 5 处里 4 处已足额、1 处只钉了邻态。** 补掉 `#6` 那一条即可整条销账。

---

## 二、`A562`

### 1. 谁生成它 + 命令

- **生成者** = `工具/extract_missing_shaders.py` 的 **默认（extra）模式** → `repack()`（该模式 `main()` 里 `:1561` `repack(SRC_BUNDLE, args.out)`）。
  源包 = `d:/2/.../StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle`（78,190,043 B · 1 个 CAB · 64,302 对象 · **42 个 Shader**）。
- **命令**（`资料/命令速查.md:250` 已登记）：
  ```
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/extract_missing_shaders.py
  ```
  （无参数 = 写 `Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle`；`--check` 只体检不写。）

### 2. 消费方 —— 搜过的词 / 目录 · 命中清单

**搜过的词**：`wf_shaders_extra` · `ExtraBundleRelPath` · `WarpforgeVFX/wf_shaders` · `streamingAssetsPath` · `StreamingAssets` ·
`699808` / `1338640` / `645fe837` / `ecebd2f8` · 以及「行内真实消费的 shader 名」`3D Card Dissolve` · `Particle Dissolve Mask` ·
`Rays For Trail` · `Spiral Trail FX` · `ShieldVfx` · `ParticlePremultiply` · `Spine/Special/HiddenPass`。
**搜过的目录**：`Unity/MyGame/Assets/**`（`.cs` / `.shader`）· `Unity/工具/**`（`.py` / `.sh` / `.md`）· `Unity/资料/**`（`.md`）·
`Unity/MyGame/Library/**`（构建产物，噪声）· `find /d -maxdepth 4 -name "wf_shaders_extra*"`（**盘上只有 1 份**：工程里那份）。

**命中清单（四类，唯一「按名字写死」的只有第 1 类）**

| 类 | 消费方 | 怎么读 |
|---|---|---|
| **① 运行时（真正的消费方）** | `MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderLoader.cs`：常量 `:25` `ExtraBundleRelPath` · **`:95` `LoadOne(ExtraBundleRelPath, out _)`** | 路径拼串 `Path.Combine(Application.streamingAssetsPath, "WarpforgeVFX/wf_shaders_extra.bundle")` → **`AssetBundle.LoadFromFile`** → **`LoadAllAssets<Shader>()`**（不按容器名取）→ 进 `_byName` 字典，由 `Ready` / `TryGetShader` / `ShaderNames` 暴露 |
| **② 解析链上真正用它的人** | `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:408` / `:445` `WarpforgeShaderLoader.TryGetShader(originalName, …)`（解析链最后一步）；白名单点名 3 件「在补充包里」= `Everguild/FX/Spiral Trail FX` · `ShieldVfx` · `Glow Shader`（`:101`）<br>`CardPresentation/Core/CardView.cs:2345` 解析 `Everguild/Cards/3D Card Dissolve` ⇒ **卡体溶解（阵亡/回手）** 直接依赖它；取不到 ⇒ `CardFeel.cs:853` 出声退回 | 全走 ① 的加载口 |
| **③ 编辑器（目录扫描，不写全名）** | `WarpforgeArena1/Editor/EffectExporter.cs:448` 与 `:683`：`Directory.GetFiles(StreamDir /* Assets/StreamingAssets/WarpforgeVFX */, "*.bundle")` 逐个 `LoadFromFile` ⇒ **整个目录读**，`wf_shaders_extra.bundle` 在其中。另有 `EffectCompare.cs` / `EffectIso.cs` / `EffectSweepBatch.cs` / `EffectShaderProbe.cs` 经 `WarpforgeShaderLoader` 间接用 | 目录拼串 + 通配 |
| **④ python 只读分析（把当「载体包」实读）** | `工具/`：`audit_effect_shaders.py:60` · `survey_shader_originals.py:23` · `_missing_shaders_audit.py:30` · `dump_shader.py:22` · `dump_shader_blob.py:34` · `gen_shader_blend.py:22` · `disasm_dxbc.py:36` · `diff_iso_props.py` · `extract_mirror_shaders.py:22` | 写死绝对/相对路径 + UnityPy 实读 |

- ⛔ **没有**任何 `.meta` guid 引用它（`wf_shaders_extra.bundle.meta` 的 guid `ee5ce0e7…` 全仓 0 处引用）—— 唯一引用方式是**运行期文件路径**。
- `.gitignore:33` 排除的是**整个目录** `/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/`（不止这一个包）⇒ 干净克隆上 4 个包全都不存在。

### 3. 两版差异 —— **1.34 MB 版并没有多一个 shader，多的是一张 86,348 条的陈旧预加载表**

**方法**：用 UnityPy 在**内存里**复现两种打法（零写盘），与盘上副本对 md5。

| 打法 | 结果 |
|---|---|
| 盘上副本（现状） | `1,338,640 B` · md5 `ecebd2f811a8389bab405940ecfe6e07` |
| **内存复现「现行工具」**（重写 `m_Container` **和** `m_PreloadTable` 各 42 条） | **`699,808 B` · md5 `645fe8370277218566521bdb9bcc72a4`** ← 与账上「工具现在跑出来」**逐位相同** |
| **内存复现「09-11 旧版工具」**（重写 `m_Container`，**不碰** `m_PreloadTable`） | **`1,338,640 B` · md5 `ecebd2f811a8389bab405940ecfe6e07`** ← 与盘上副本**逐位相同** |

**⇒ 差异 100% 定位**（实读盘上副本的 `AssetBundle` 对象）：

| 字段 | 1.34 MB 版（09-11） | 699 KB 版（现行工具） |
|---|---|---|
| Shader 对象数 | **42**（逐个读名核对，与新版**同一批 42 个**） | 42 |
| `m_Container` | 42 条（**同一批名字**） | 42 条 |
| 🔴 `m_PreloadTable` | **86,348 条**（全是 `m_FileID=1` 的**陈旧**指向 —— 指进产物里已不存在的那一份） | **42 条**（`m_FileID=0`，指对） |
| `m_Name`（内层 CAB 名） | `CAB-d47690319398b604c3bb5a35a8ed2499` | 同（本模式不改名） |
| 体积差 | `638,832 B` ≈ 那张 86,348 × 12B 的表（压缩后） | |

**为什么是这张表**：`工具/extract_missing_shaders.py` 的注释自己记着 ——「上一个源包恰好带着一张 **86348 条**的预加载表，
索引 0 落在界内 ⇒ **侥幸能跑**」。`m_PreloadTable` 的正确重写是 **2026-09-19** 才补进工具的（同一次加的还有
「容器 `preloadIndex` 必须与预加载表一起写，否则 `AddAssetsToPreload` **直接段错误**」那条实测）。
⇒ 09-11 那份产物 = **旧工具留下的、带着一张陈旧表的包**，靠「长度够大 + 索引 0 有效」侥幸加载成功。

### 4. 结论：**重打**（不是销账）

**理由**
1. **没有消费方依赖那 1.3 MB 版的任何东西** —— 两版的 **42 个 shader 与 42 条容器名逐位相同**，唯一差异是那张陈旧预加载表；没有任何代码/断言读它的字节、md5 或表长。
2. 那张表是**陈旧指向**（`m_FileID=1`），属于**已知的侥幸**（工具注释白纸黑字），重打 = 把侥幸换成对的。
3. 顺带省 **638,832 B**（该目录整目录被 `.gitignore` 排除、不进仓库，所以**不影响仓库体积**，只影响本地磁盘与产物一致性）。
4. **风险面极小**：重打只动这一个 StreamingAssets 数据文件，源码一个字不改。

**命令（照 `资料/命令速查.md:250`）**
```
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/extract_missing_shaders.py
```
**重打后自检（`A801` 那条口径仍在，注意看输出）**：脚本会印 `[6] ⚠️ 内层文件沿用源包名`；
今天安全（进程里不装源包），⚠️ **但重打前先确认没有 Unity 批处理正在跑**（本代理没跑）。

**要跑哪些腿（按铁律 12 的覆盖面）**
- 该文件是**运行时数据资产**（不是 `.cs`、不是共用件）⇒ 严格说**代码覆盖面为零**；但它被**每个带特效的腿**读取。
- ✅ **最小验证**（真读它、且能看出加载成功）：`EffectShaderProbe.Run`（**不在那 12 条里**）——
  它会印 `WarpforgeShaderLoader.Ready` 并**点名查**补充包那 3 个 shader（`EffectShaderProbe.cs:102-105`）；或
  `BuiltinShaderProbe.Run` 同族的 `LoadFromFile` 路。
- ✅ **12 条里该跟的**：`BattleScene.Run`（特效 + **卡体溶解**最集中）+ `CardBaseDemo.Run`（卡面族）。
  真要保守就等**下一次同步点**跑全套（`bash d:/4/Unity/工具/_run_8_checks.sh`）。
- ⛔ **不需要**为它单独跑全套 —— 与铁律 12「纯数据/纯工具不逐条复跑」同口径；**但加载本身只有在 Unity 里才验得真**。

---

## 三、没查清的部分（⛔ 不拿猜测填空）

1. **`A799` #5 的「下沿档」为什么盖不住 `Name`** —— 我只核到作者在 `:6018-6022` 的自陈（`Name` 与 `Ranking`/`Points`
   在同一沿位互斥）。**字墨行内区间（`≈14.6..51.8` 等）是作者按静态推算给的、我没实跑复核**。
   这不影响「#5 已有断言」这个结论（上沿档那条是实打实的 `CheckNear(eMinY, VpTop, ±0.5)`）。
2. **`A799` #6 要补那一条新档的期望值**（下沿压到行内 ≈70 时 `Guild Name` 的字墨底应在哪）**我没算** ——
   它得靠实测的 `TmpSpanPx` 反推（照 §A833(C) 那套「参照行 + 行距差」的写法）。本报告只给「该补什么、放哪、判据在哪」。
3. **`A562` 那份 699 KB 版从没落过盘** ⇒ 第 3 节的对比是**内存复现 + md5 逐位吻合**（两侧都复现到逐位相同），
   **不是**「拿两个真文件 diff」。我把复现脚本的每一句都贴进了本报告的方法表，可复算。
4. **重打后 Unity 侧能不能加载**（`LoadAllAssets<Shader>()` 对新的 42 条预加载表）**本代理没验**（不能跑 Unity）。
   风险低（现行工具产出的 `wf_builtin` / `wf_arena_*` 三个同口产物**仓库里正在用、且逐字节相同**），但**加载成功这件事只有 Unity 能证**。
5. **`A562` 没查的**：`MyGame/Library/**` 里的构建产物（`LastBuild.buildreport` / Bee 缓存）含该包路径字符串 ——
   那些是 Unity 的构建缓存，**我没逐条追它们是不是「消费方」**（判为缓存，不是运行时代码路径）。
