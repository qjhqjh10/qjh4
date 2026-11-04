# B141-W51 · W129 · `*clips*.json` 键名横切普查（生成器 ↔ C# DTO）

> 执行写手 `W51` · 2026-11-04 · 批次 `B141` · 会话第一百二十八 · 任务 `B141-CLIPS-KEY-SWEEP`。
> 起因 = `报告/B141_W129_BattleHudAnim.md`（W46）：`gen_battlehud_clips.py` 写 `is`/`os`，而 `BattleHudKey` 字段是 `si`/`so`
> ⇒ `JsonUtility` 按**字段名**绑定 ⇒ 斜率**恒 null 恒 0**（哑弹，断言与画面都看不出来）。
> 本件 = 把同一类缺陷在**全仓 `*clips*.json` 家族**横切查一遍。

## 〇、一句话结论

**扫了 8 个生成器 / 8 份产物（14 个文件，含运行期镜像）/ 7 个 C# DTO 文件 / 1 个 python 消费者。**
**新增不一致 = 0 处**；家族里唯一的那处（battlehud `is`/`os`）已由 W46 修掉，本次**复核通过**（本批普查确认它是唯一例外）。
**本代理只做了一处实质改动**：`gen_arena_scene_clips.py` 里一句**已过期的跨件说明**（"battlehud 用的是 is/os，同义"）就地订正 + 重生成产物；
其余 6 个生成器各加一条普查标记（零语义改动）。**未动任何 `.cs`** ⇒ C# 自检零条。

---

## 一、普查总表

| 生成器 | 产物 | 产物键数 | C# DTO（`文件:类`） | 不一致键 | 影响 | 修法 |
|---|---|---|---|---|---|---|
| `工具/gen_arena_scene_clips.py` | `数据/游戏数据/arena_scene_clips.json`（`Resources/` **没有**副本） | 33 | **无 C# 消费者**（`ArenaSceneAnims.cs` 读的是另一份 `arena_scene_anims.json`，由 `gen_arena_scene_anims.py` 产） | 0 | 无（数据件，工具/人读） | 已订正生成器里**过期的跨件说明**，见 §二 |
| `工具/gen_camera_intro_clips.py` | `Resources/camera_intro_clips.json` + `数据/游戏数据/camera_intro_clips.json`（两份**逐字节相同**） | 23 | `Battle/CameraIntro.cs:CameraIntroKey/Src/Clip/File` | 0 | 无 | 无需改（只加标记） |
| `工具/gen_battlehud_clips.py` | `Resources/battlehud_clips.json`（运行期，25 键）+ `数据/游戏数据/battlehud_clips.json`（全量，83 键，多 `perArena`/`reference`） | 25 / 83 | `Battle/BattleHudAnim.cs:BattleHudKey/Curve/Clip/Ref/File` | **0（W46 已修）** | 修前 = `si`/`so` 恒 null 恒 0 | 复核：`BattleHudKey.si/.so` ↔ JSON `si`/`so`，`--check` rc=0，hash `ae1b8380c3b9…` |
| `工具/gen_cardanim_clips.py` | `Resources/cardanim_clips.json` + `数据/游戏数据/cardanim_clips.json`（两份**逐字节相同**） | 101 | `Battle/CardAnimClips.cs:RtRow/RtUnitShake/RtMinionClip/RtFile` | 0 | 无 | 无需改（`ss*`/`mm*` 两族**无 DTO 字段**，见 §四-1） |
| `工具/gen_division_clips.py` | `Resources/division_clips.json` + `数据/游戏数据/division_clips.json`（逐字节相同） | 35 | `Shell/RankedDivisionChangeWindow.cs:DivisionClip{Key,Curve,Event,File}` | 0 | 无 | 无需改（顶层 `animation`/`src` 是 provenance，DTO 有意忽略） |
| `工具/gen_misc_clips.py` | `Resources/misc_clips.json` + `数据/游戏数据/misc_clips.json`（逐字节相同） | 43 | `Editor/B107_R3c_Assert.cs:B107MiscClips`（自检用最小 DTO）· `Editor/B108_A13_R3_Assert.cs`（另一份最小 DTO） | 0 | 无 | 无需改（两处都是「只声明要用的字段」，缺键 = 默认值，**不是漏绑**） |
| `工具/gen_simple_animation_clips.py` | `数据/游戏数据/simple_animation_clips.json`（**不进 `Resources`**） | 6（+ 每包动态键） | **无 C# DTO** —— 唯一消费者 = `工具/extract_missing_shaders.py:802-804`（python） | 0 | 无 | 无需改（打包期把 `<效果名>竖线<状态名>` 别名登记到 bundle；运行期 `WFModuleAnimation.cs:123` 的拼法与 `extract_missing_shaders.py:2794` 一致） |
| `工具/gen_anim_clips.py` | `Resources/Animations/*.anim` + `WarpforgeVFX/Animations/*.anim`（**Unity YAML，不是 JSON**） | — | 无（`.anim` 由 Unity 自己的序列化器读） | 0 | 无（键名 `time/value/inSlope/outSlope` 由 Unity 定义，**不经 `JsonUtility`**） | 无需改 |
| （邻接复查）`工具/gen_card_anim_order.py` | `Resources/card_anim_order.json` | 45 | `Battle/CardAnimOrder.cs:CardAnimOrderCard/CardAnimBeat` | 0 | 无 | 无需改（曲线是**扁平数组** `[-1,t,v,in,out,…]`，不按键名绑定） |

**怎么比的**（三个方向都跑，不只是看键名）：
1. **正向**：JSON 里出现的每个键是否能在 DTO 里找到同名字段（`is`/`os` 这类就是在这里现形的）—— 跳过文档/溯源键（`schema`/`note`/`src`/`pkg`/`bundles`/`generatedBy`/`stats`…）。
2. **反向**：DTO 声明的字段是否**从没**在 JSON 里出现过（= 恒默认值，`si`/`so` 那条的另一半）。命中的只有**运行期才用**的字段（`eval`/`clip`/`isHand`/`scales` 等），没有一个是漏绑。
3. **类型与镜像**：逐字段比 python 类型 ↔ C# 类型（`wrapMode` 必须是 string、`classId` int、`v`/`si`/`so` 的分量形式）；再比**运行期镜像件 vs 全量件**的键集合是否一致（battlehud 两者**故意不同**：运行期 25 键含 `refs`，全量 83 键含 `perArena`/`reference`，两边各自与 DTO 对齐）。

---

## 二、本代理改了什么

### 1. `工具/gen_arena_scene_clips.py`（唯一实质改动，+3/−1 行）

| 行（改后现读） | 改前 | 改后 |
|---|---|---|
| `:348-349`（`note` 字段） | `⚠️ 键名用 si/so（与 camera_intro_clips.json 一致；battlehud_clips.json 用的是 is/os，同义）。` | `⚠️ 键名用 si/so；2026-11-04（B141-W51 普查）起 camera_intro / division / misc / arena_scene / battlehud 五份全仓统一为 si/so（battlehud 原用 is/os，已于 B141-W46 改掉 —— 它当时是唯一例外）。` |

**为什么必须改**：这句话是**生成进产物 JSON 的 `note`** 的（不只是一句注释），W46 之后它已成**假话** ——
下个会话读到「battlehud 用的是 is/os」会**以为 `si`/`so` 有两套合法写法**，正好把这颗哑弹再种一次（铁律 5：发现记错就地改掉）。
W46 报告 §五-1 已把它点名交调度台，本次由本代理接掉。

### 2. 产物重生成

`python -I -X utf8 d:/4/Unity/工具/gen_arena_scene_clips.py --write` ⇒ `数据/游戏数据/arena_scene_clips.json`（1,046,879 B）。

```
1  1  Unity/数据/游戏数据/arena_scene_clips.json      ← 只有 note 那一行，`git diff` 全篇仅 1 行改动
```

行尾：**CRLF 0 · bareCR 0 · LF 51,513**（纯 LF，未翻）。

### 3. 普查标记（7 个生成器，零语义改动）

按铁律 20「做完一处就在代码里留标记」，在**每个已核过的生成器**里加一条机器可 grep 的
`✅ 已复刻（2026-11-04 · B141-W51 · 键名普查：…）`：

| 文件 | 落点 | 行数变化 |
|---|---|---|
| `gen_arena_scene_clips.py` | `## schema` 标题下一行 | +1（另含 §二-1 的 +2/−1） |
| `gen_camera_intro_clips.py` | `## schema` 标题下一行 | +1 |
| `gen_cardanim_clips.py` | 模块 docstring 之后（`import` 之前） | +3（注释折行） |
| `gen_division_clips.py` | `## schema` 标题下一行 | +1 |
| `gen_misc_clips.py` | `## schema` 标题下一行 | +1 |
| `gen_simple_animation_clips.py` | 模块 docstring 之后（`import` 之前） | +3（注释折行） |
| `gen_anim_clips.py` | 模块 docstring 之后（`import io` 之前） | +3（注释折行） |

`gen_battlehud_clips.py` **不加**（W46 已在 `:181` 打过同一族标记，一段只打一条）。

> ⚠️ **`gen_anim_clips.py` 正被 `B141-W45` 在飞**（`git diff` 里那 149 行新增是 W45 的 `SCALE_PATHS` 工作，不是本代理的）；
> 本代理只在 docstring 之后加了 3 行注释，**未碰 W45 的任何内容**。若调度台担心并发，删掉那 3 行即可（零语义损失）。

---

## 三、验证读数

| 验证 | 命令 | 结果 |
|---|---|---|
| `gen_arena_scene_clips.py` | `--check`（改前基线） | **rc=0** 与现生成一致 |
| 同上 | 改后未重生成 `--check` | **rc=1**「与 …arena_scene_clips.json 不一致」⇒ 证明改动确实进了产物（不是空转） |
| 同上 | `--write` → `--check` | **rc=0** ✅「两份… 与现生成一致」 |
| `gen_camera_intro_clips.py` | `--check` | **rc=0** 两份产物一致 |
| `gen_battlehud_clips.py` | `--check` | **rc=0** 两份一致 · 自检 hash `ae1b8380c3b9` · 斜率全 0 已保留 |
| `gen_cardanim_clips.py` | `--check` | **rc=0** `check: OK` |
| `gen_division_clips.py` | `--check` | **rc=0** 两份一致 |
| `gen_misc_clips.py` | `--check` | **rc=0** 两份一致（11 条 legacy · 端点/事件/键数抽核一致 · 四元数 4 分量） |
| `gen_simple_animation_clips.py` | `--check` | **rc=0** 一致 ✅ |
| `gen_anim_clips.py` | `--check` | **rc=1** —— **既有、与本次无关**（见 §四-2）：4 条里 3 条逐字节一致，只有 `Booster Window - …` 那条不一致 |
| 行尾 | 二进制计数 | `arena_scene_clips.json`：CRLF 0 · bareCR 0（纯 LF）；`.py` 均为 LF |
| diff 范围 | `git diff --numstat` | 7 个 `.py` + 1 个 `.json`；JSON 仅 1 行（note） |
| C# 自检 | —— | **零条**（本批**未动任何 `.cs`** ⇒ 无 typecheck、无 Unity 宿主） |

---

## 四、❓ / 顺手发现（白名单外，未改）

1. **`cardanim_clips.json` 的 `ss*`（17 个）与 `mm*`（11 个）键在 DTO 里没有对应字段 —— 但这不是缺陷。**
   生成器自述（`gen_cardanim_clips.py:66-81`）与产物 `what` 字段都写明：这两族是**原样全量记录**，
   运行期读的是**同一批数据的投影列**（`mv*`/`mo*`/`shk*`，`RtRow` 里都有、且都绑上了）。
   ⇒ 属既有设计，⛔ 别为了「键都能对上」把它们删掉或加进 DTO（那会变成无人消费的死列）。
2. **`gen_anim_clips.py --check` 的 rc=1 是既有状态，与键名无关**：只有 `Booster Window - Background Shake On Open.anim`
   一条不一致，原因是**磁盘上那份已被 Unity 重新序列化**（`inWeight 0.33333334` / `m_Events: []`），
   生成器文件头 `:57-60` 早就写明「全量 `--check` 必然 rc=1」；git status 里那份 `.anim` **未被任何人改过**（= HEAD 里就是这样）。
   本代理**未动它**（重写它要碰 `WarpforgeVFX/Animations/`，属特效线，不在键名范围内）。
3. **`arena_scene_clips.json` 目前没有 C# 消费者**（`Resources/` 里没有副本，`ArenaSceneAnims` 读的是 `arena_scene_anims.json`）。
   ⇒ 它的键名今天**不可能**造成运行期哑弹，但也意味着「改了没人验」。是否要接线/瘦身 = 调度台裁决（本代理无结论）。
4. **W46 报告 §五-2/§五-3 的两条仍未做**（历史报告里的旧 `clipsHash` 订正注 · 「斜率真的写进 Keyframe」的断言）——
   不在本件白名单（要动 `资料/普查产出_1022/*` 与 `Editor/*.cs`），原样留给调度台。

---

## 五、一句话

**这一类缺陷在全仓只剩 battlehud 一处，且已修；本次横切普查未再发现第二处。**
本代理的实际产出 = **1 处过期说明订正 + 1 份产物重生成（1 行）+ 7 份普查标记**，`--check` 全绿（除 `gen_anim_clips` 那条既有例外）。
