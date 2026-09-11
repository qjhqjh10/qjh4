# Warpforge 解包资源 — 使用说明

> 战锤40K Warpforge（Unity IL2CPP 卡牌游戏）的完整解包资源。
> **计数口径**（2026-09-10 实测，不含本目录下 `_审计报告_0910.md` / `_修复记录_0910.md` 两个元文件）:
> 449,842 文件 / 5,744,408,720 字节 = **5.35 GiB（5.74 GB）逻辑大小**
> （磁盘占用按 NTFS 簇算约 6–7 GB）。其中约 48% 是同一次解包被跑第二遍产生的**逐字节重复**（见审计报告 §B1，
> 隔离尚未执行），**真实唯一对象约 233,836 个**。**本 README 与说明书默认报「唯一对象数」；凡报文件数处标注 `(含重复)`。**
> 本文件是**新会话的第一份资料**——先读这里，再动手。

## ⚠️ 引擎视角: 主力是 Unity（2026-09-10 增补）

**当前目标 = 在 Unity 里复刻原版游戏。** 三条先记住，能省几小时：

1. **Unity 侧不需要任何手性/镜像转换**：场景说明书里的坐标就是 Unity 左手系原始数值，直接照抄；
   `07_场景/**` 的 JSON 是 Unity type tree 原文，字段名与 Unity API 一一对应；
   PNG / TTF / OBJ / WAV / OGG / MP4 全是 Unity 原生格式。
2. **唯一必须处理的坑：`06_模型/**/*.obj` 在 UnityPy 导出时已被 X 镜像**（顶点 x 取负 + 面绕序反转，UV 未镜像）。
   Unity 导入前必须还原，方案与脚本见 `_审计报告_0910.md` §A1；材质透明判据也不能只看 `_Blend`（§A2）。
3. **Godot 相关内容一律是历史管线记录**：§8.B 以及说明书里出现的「y 向下坐标 / `unity_scene_to_godot.py` /
   gdshader / Z 反射方案」等，做 Unity 时请忽略，**不要照搬它的转换结论**（Godot 说明保留仅作参考）。

## 0. 配套目录（D:\2 下）

| 目录 | 内容 |
|---|---|
| `解包整理/` | 本目录：按内容分类的资源（12 大类） |
| `Warhammer 40k Warpforge/` | 游戏本体（资源源文件，1.8G） |
| `Warpforge_code/` | 反编译的 C# 游戏代码（14996 文件，130 程序集）——**查逻辑/字段/枚举先翻这里** |
| `Warpforge_tools/` | Python 工具集（py312 + UnityPy，提取/解码/验证脚本，见其 README） |

## 1. 目录结构（12 大类）

| 目录 | 文件数(含重复) | 内容 |
|---|---|---|
| `01_卡牌/` | ~10.6K | 13 阵营 ×（Texture2D 立绘 / Sprite 精灵 / AudioClip 语音）+ 动画 + 卡组数据 |
| `02_装饰品/` | ~5.5K | 卡背 / 头像 / 督军立绘 / 边框 / 战役奖励 |
| `03_界面UI/` | ~154K | 菜单 / 图集 / 图标 / 按钮 / 横幅（UI 层级 JSON 为主） |
| `04_音频/` | ~1.9K | 音效库(618) / 音乐 / 督军语音 / 音频控制 |
| `05_视频/` | 18 | 8 个视频（h264×1 / VP8×7，mp4 容器） |
| `06_模型/` | 1,033 | 全部 OBJ 网格（跨包收集，子目录=来源包名；其中 1,030 个 `.obj`，1 个 0 字节见 §6） |
| `07_场景/` | ~139K | 13 个战斗场景（GameObject/组件/灯光/材质 JSON） |
| `08_预制体特效/` | ~119K | 战斗预制体(15734 GO) / 共享资源 |
| `09_游戏数据/` | ~4.6K | 卡包 / 教程 / 动画曲线 / 脚本定义(monoscripts) |
| `10_字体/` | 189 | TTF 字体（4 个已于 2026-09-10 从 JSON 还原，见 `_修复记录_0910.md` A4）+ 配套贴图材质 |
| `11_着色器/` | 527 | 全部 Shader 转储（跨包收集） |
| `12_主程序资源/` | ~12.9K | 引擎设置 / 全局管理器 / 主数据文件对象 |

## 2. 文件格式速查

| 扩展名 | 内容 | 引擎可用性 |
|---|---|---|
| `.png` | 贴图 | ✅ Unity / Godot 都直接导入 |
| `.ogg` / `.wav` | 音频（wav=IMA 解码，ogg=vorbis 重建） | ✅ Unity / Godot 都直接导入 |
| `.ttf` | 字体（2026-09-10 前是 0 字节，已还原） | ✅ 直接导入 |
| `.obj` | 网格模型 | ⚠️ Unity 可导入**但几何是 X 镜像的，必须先还原**（§A1）；Godot 建议转 glb/gltf |
| `.mp4` | 视频（h264/vp8） | ✅ Unity 直接可播；⚠️ Godot 4 不支持 h264，需转 webm(VP8)/ogv |
| `.json` | Unity 对象转储（见下节） | Unity: `JsonUtility`/Newtonsoft 直接读（字段名即 Unity 字段）；Godot: 用 JSON 解析读字段 |
| `.vorbis` | 1 个未重建的原始音频包（OvertimeStart） | ❌ 需解码 |

## 3. JSON 转储是什么、怎么读

每个 JSON = **Unity 序列化对象的一个完整字段转储**（type tree 原文），不是人读的导出数据。
文件命名：`<对象名>.json`；无名字的对象 = `<类名>_<pathID>.json`（如 `Material_2.json`）。

子目录名 = Unity 对象类名，常见类：
- `MonoBehaviour` — 游戏数据（ScriptableObject）/ 组件。**卡牌、卡组、商店、成就等一切游戏数据都在这**
- `GameObject` / `Transform` / `RectTransform` — 场景/预制体层级结构
- `Material` — 材质（Unity 序列化格式，导入引擎后需重建）
- `Sprite` / `Texture2D` — 精灵元数据 + 贴图（同名的 .png 是它的图）
- `AnimationClip` / `Animator` / `ParticleSystem` — 动画/特效
- `Shader` — 着色器转储（不可直接运行，需翻译成目标引擎的 shader）

关键字段：
- `m_Name` — 对象名（搜资源先搜名字）
- `m_Script` — `{m_Collection: "cab-<哈希>", m_PathID: N}` — 指向脚本类；cab 哈希=来源包
- `m_GameObject` / `m_PathID` — 对象间引用（跨文件引用靠 cab+pathID 对应）

**想查对象是什么脚本类**：跑 `Warpforge_tools/scripts/classify.py`（用 monoscripts 包把 pathID 映射成类名），或直接翻 `Warpforge_code/Scripts/Assembly-CSharp/` 同名类。

## 4. 卡牌数据（做卡牌游戏的核心）

- **卡牌定义**：`01_卡牌/卡组数据/MonoBehaviour/*.json`（2210 个）— `PrebuiltPack` 类：
  ```json
  { "m_Name": "Canoness", "packName": "Canoness", "packId": "SOR_Canoness",
    "packArmy": 80, "cardIds": [3 个卡牌ID] }
  ```
  `packArmy` = 阵营 ID；`cardIds` 指向具体卡牌数据（卡牌本体定义在 `08_预制体特效/战斗预制体` 的组件 JSON 里）
- **卡牌立绘**：`01_卡牌/<阵营>/Texture2D/*.png`（与 `Sprite/*.json` 同名配对）
- **卡牌语音**：`01_卡牌/<阵营>/AudioClip/`（索引已挂载 602/1193 张卡，含督军全套语音；无语音的卡多为战术卡/测试卡）
- **卡牌动画**：`01_卡牌/<阵营>/动画/`（Spine 骨骼动画数据）
- **阵营 ID 表**（来自反编译代码 CardArmy.cs，权威）：
  Neutral=0, Ultramarines=10, Goff(兽人)=20, SaimHann(灵族)=30, Sautekh(死灵)=40,
  BlackLegion=50, Leviathan(泰伦)=60, TauEmpire=70, Sororitas=80, Genestealers=90,
  AstraMilitarum=100, DarkAngels=110, EmperorsChildren=120, SpaceWolves=130

## 5. 音频/视频说明

- 音频源格式是 **FSB5（FMOD）**，已解码为 wav（IMA-ADPCM 手写解码器）/ ogg（vorbis 重建）
- 卡牌语音在各阵营目录；战斗音效 618 个在 `04_音频/音效库`；音乐 2 首在 `04_音频/音乐`
- 视频在 `05_视频/`：`Gacha Crate Opening` 是 h264，其余 7 个 VP8 —— Godot 用需转 webm

## 6. 已知限制（不完整清单）

1. `OvertimeStart.vorbis` — 48kHz 立体声，缺 setup 头无法重建 ogg（1/2555 音频）
2. 2 张 `Font Texture` 特殊格式贴图未解码
3. 3 个空网格（CandleFlame）只有 JSON
4. ~0.4% 对象（899 个，主数据文件 UI 组件）type tree 解析失败，无转储
5. 重名对象以 **`_<pathID>` 后缀**区分（`extract_full.py` 第二遍导出产生，见审计 §B1）。
   > ⚠️ 原文写「少数文件以 `_来源包` 后缀区分（manifest 可查）」——**`manifest.json` 从未生成**，
   > 该指引作废（2026-09-10 核实；`classify_move.py` 重跑不可逆，**不要**为了找回它而重跑）。
6. ~~`07_场景/_battlearena1` 因系统占用未重命名~~ — **已过期**：2026-09-10 核实该目录就叫
   `07_场景/battlearena1`，全树不存在 `_battlearena1`。
7. `06_模型/scenes_scenes_battlearena3/Background Structures.0041.obj` 是 0 字节（UnityPy 导出失败，
   回 bundle 复核中）—— 所以可读 OBJ 是 1,029 个。

## 7. 常见需求速查

| 想找 | 路径 |
|---|---|
| X 阵营的卡图 | `01_卡牌/<阵营>/Texture2D/` |
| 全部卡背 | `02_装饰品/卡背/` |
| 头像/督军立绘 | `02_装饰品/头像/` `02_装饰品/督军立绘/` |
| 某个音效 | `04_音频/音效库/AudioClip/` |
| 主菜单背景图 | `03_界面UI/主菜单/` |
| 战斗场地模型 | `06_模型/` + `07_场景/` |
| 卡牌数值/卡组构成 | `01_卡牌/卡组数据/MonoBehaviour/` |
| 某个脚本的字段含义 | `Warpforge_code/Scripts/Assembly-CSharp/<类名>.cs` |
| 素材来自哪个包 | 子目录名（06/11 类带来源包名）；分类映射见 `Warpforge_tools/scripts/classify_move.py` |

## 8. 引擎接入

### 8.A Unity（主用途，2026-09-10 增补）

- **直接可用，不需要格式转换**：PNG / TTF / WAV / OGG / MP4 / OBJ 都是 Unity 原生格式
  （OBJ 例外：**几何是 X 镜像的，导入前必须先还原**，见 `_审计报告_0910.md` §A1）
- **坐标**：`07_场景/**` JSON 里的 position / quaternion / scale 就是 Unity 左手系原始值，直接赋给 Transform；
  **不需要任何手性转换**（说明书里所有「转 Godot 要反射」的说法都是历史管线）
- **需重建**：材质 / Shader / 预制体（Unity 序列化 JSON → 在 Unity 里重做）。
  材质透明判据**不能只看 `_Blend`**，要看 `_SrcBlend`/`_DstBlend`/`_AlphaClip`（§A2）
- **贴图**：15 个场景引用的贴图已于 2026-09-10 补齐进 `07_场景/<arena>/Texture2D/`（§A3），
  全树 名字→路径 索引在 `Warpforge_tools/data/texture_index.json`
- **字体**：`10_字体/` 的 4 个 TTF 已于 2026-09-10 从同名 JSON 还原（§A4），可直接导入
- **卡牌数据**：`01_卡牌/卡组数据/MonoBehaviour/*.json`（`PrebuiltPack`: packArmy/packId/cardIds），
  用 `JsonUtility` / Newtonsoft 读
- **动画**：Spine 骨骼数据用 Spine-Unity 运行时（或导出 glTF）
- **资源命名即索引**：文件名就是对象名，递归扫描 + 名字匹配即可建资源库
- **视觉副模型**：见 §8.C

### 8.B Godot（历史管线，保留供参考）

- **直接导入**：PNG / OGG / WAV / TTF / OBJ
- **需转换**：MP4→webm（Godot 4.3+ VideoStreamWebm）；OBJ→glb（Blender/trimesh）
- **需重建**：材质、着色器、预制体（Unity 格式，Godot 里用标准材质/Shader 重做）
- **卡牌数据**：用 GDScript `JSON.parse_string()` 读 `卡组数据` 的 JSON，按 `packArmy`/`packId`/`cardIds` 组织
- **动画**：Spine 数据需用 Godot 社区 Spine 插件（或导出 glTF）
- **资源命名即索引**：文件名就是对象名，用 `DirAccess` 递归扫描 + 名字匹配即可建资源库
- **手性**：历史定案是 **Z 反射方案**（2026-08-25 白盒探针终审；旧「整世界 scale.x=-1 镜像」论**已证伪**），
  见 `Warpforge_tools/scripts/unity_scene_to_godot.py` 顶部注释；OBJ 的 X 镜像由 `fix_obj_xmirror()` 对副本处理
  （**勿就地跑**，会在 `06_模型/` 打 `.xfixed` 标记污染原文件）

### 8.C 视觉副模型（vision MCP）

主模型（Claude Code）无法直接查看图片，图像理解通过视觉副模型 MCP 完成：

- **工具**：`vision_analyze(image_path, prompt)` 单图识别；`vision_batch(inputs|directory, prompt_template)`
  批量（并发 2 / 重试 / 错误隔离 / 摘要，单批 ≤20 张）
- **状态（2026-09-10 核实）**：本节原文引用的三个路径**在磁盘上均已不存在**，已从文档中移除 ——
  `Warpforge_tools/scripts/vision_mcp_server.py`、`Warpforge_tools/scripts/test_vision_mcp.py`、
  `~/.claude/skills/vision/SKILL.md`。
  MCP 中若仍注册着 `vision` 服务器（stdio）则上述工具可直接调用；否则该能力已失效，需重建 server 脚本。

## 9. 重新提取/工具链

需要重新提取或解码时用 `Warpforge_tools/`（自带的 py312 运行，勿用系统 Python 3.14）：
- `extract_full.py` 全量提取 → `fix_exports.py` 补充导出 → `verify_extract.py` 验证 → `classify_move.py` 分类
- `build_card_index.py` 生成卡牌索引（`--compare <旧索引>` 做零回退断言）
- `fsb_audio.py` / `ogg_build.py` — FSB5 音频解码与 OGG 重建
- 详见 `Warpforge_tools/README.md`
