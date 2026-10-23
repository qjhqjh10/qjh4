# G4 —— 三个音频导入脚本补 `.meta`（收敛成一份共用实现）

> 执行代理 G4 · 2026-10-24 · 起于 `资料/普查产出_1023e/G3_虫群音.md` §六·2
> 白名单内改动，⛔ 未跑 Unity、⛔ 未动 git（只读了 `git diff`）。

---

## 一、结论

1. **三个导入脚本现在都会在拷完音频后顺手补 `.meta`，且只有一份实现** —— 新建共用模块
   `d:/4/Unity/工具/_sfx_meta.py`（365 行），`import_original_sfx.py` / `import_battle_sfx.py` /
   `import_remnant_sfx.py` 各加 5 行 `sys.path` 引导 + `import _sfx_meta`，并在「拷音频」那个循环里
   逐条调 `_sfx_meta.ensure_meta(path)`。
2. **模板 = 同目录 + 同扩展名兄弟 meta 的众数，逐字节照抄，只换 `guid`**（⛔ 不手写字段）。
   实测同目录 `.wav.meta` 与 `.ogg.meta` **本来就不是一套设置**（wav：`loadType 1 / compressionFormat 2 /
   preload 0`；ogg：`loadType 2 / compressionFormat 1 / preload 1`）⇒ 「一套模板走天下」会把 ogg 写错，
   所以模板按扩展名各投各的票（这也是选「众数」而不是「固定常量」的理由）。
3. **已存在的 meta 一个字节都不碰**（`ensure_meta()` 直接返回 `exists`）；重跑幂等。
4. **有独立的「只补 meta、不重拷 wav」路径**：`python -I -X utf8 工具/_sfx_meta.py --backfill`
   （不需要解包资源 / 不需要 UnityPy；`--dry-run` 只数）。三个脚本自身重跑也等价 —— 音频同尺寸跳过、
   meta 缺了才补。
5. **本次回填 = 0 条**：`Resources/Art/audio/sfx/` 现在 **511 个 wav 全部已有配对 meta**（G3 已把唯一缺的
   `Trait Swarm Tyranid.wav.meta` 补上，guid `58840af8e11047bda8f8aeee6b4f3f05` 未变）；5 个 ogg 也都有。
   ⇒ 这一轮**没有新增任何 meta 文件**，也没有改写任何既有文件。
6. 全量校验 **0 红 / 2 黄**（2 黄都是**既有**历史遗留，不是本件引入，见 §五）。

### 为什么是「新建小模块」而不是「写在其中一个脚本里、另两个 import 它」

- 三个脚本里 **两个都把 `import UnityPy` 放在文件头**；把通用件挂进 `import_original_sfx.py` 会让
  「只想补几条 meta」的一次性活也先背上 UnityPy + 552 行 cue 解析；
- `_sfx_meta.py` **零依赖**（只用标准库）、与既有内部共用件 `_guid_resolve_probe.py` 同一命名/用法惯例；
- 不会和 `import_remnant_sfx._shared_sfx_scan()`（它 `importlib.import_module("import_original_sfx")`）
  形成互相 import 的三角。

## 二、改动清单（全部在白名单内）

| # | 文件 | diff（vs HEAD） | 改了什么 |
|---|---|---|---|
| 1 | `d:/4/Unity/工具/_sfx_meta.py`（**新建**） | 365 行 · LF · 无 BOM | 共用实现：`ensure_meta()` / `backfill()` / `verify()`；CLI `--backfill [--dry-run]` / `--verify [--dir]` |
| 2 | `d:/4/Unity/工具/import_original_sfx.py` | `+38 / −8`（其中 **G3 基线 `+12 / −1`** ⇒ 本件 `+26 / −7`） | 文件头补 5 行引导；② 拷音频循环改成「拷/跳过都调 `ensure_meta`」；docstring + 统计行 |
| 3 | `d:/4/Unity/工具/import_battle_sfx.py` | `+17 / −5` | 同上（docstring 1 处 + 引导 5 行 + 循环 + 统计行） |
| 4 | `d:/4/Unity/工具/import_remnant_sfx.py` | `+18 / −6` | 同上（注意它的 `DEST` 本来就是相对路径，未动） |

- 行尾：四个文件 **CRLF = 0 / 无 BOM**（改前改后一致，`git diff --numstat` 没有整篇翻）。
- ⛔ **没有**改：任何 `.cs` / 另外的 `工具/*.py` / 既有 wav / 既有 meta / 两张正本 / git。
  `import_original_sfx.py` 里 `EXTRA_CUES` 多出的 `Trait Swarm Tyranid` 是 **G3 的改动**（工作树里本来就有），不是本件加。
- `Resources/Art/` 整个被 `.gitignore:93` 排除 ⇒ 这个目录下的 meta 不进 git（与既有 516 条同命运），
  进 git 的只有上面 4 个 `.py`。

### 新模块的口径（写在 `_sfx_meta.py` 文件头，别再改）

1. **模板 = 同目录同扩展名众数**：`pick_template(dirpath, ext)` 把每份兄弟 meta 的 `guid:` 行归一化
   后投票，取众数；一条兄弟都没有才用内置兜底（`.wav` / `.ogg` 两份，均从本工程实测众数**逐字节**抄）。
2. **已存在不覆盖**：`ensure_meta()` 见到 `<path>.meta` 就返回 `"exists"`，不读不写。
3. **guid 全局唯一**：写前扫全工程 `*.meta`（`d:/4/Unity`，**24,074 份**，实测 ~1.8 s）的 `guid:`，
   `uuid4().hex` 撞了就重摇；格式恒 32 位小写 hex。
4. **二进制写**（`io.open(..., "wb")`）+ 行尾从模板原样带过来（`(?P<eol>\r?)`）⇒ 不会翻行尾。

## 三、补了多少条 meta

| 项 | 读数 |
|---|---|
| 回填前扫描 | `Resources/Art/audio/sfx/` 下 **511 个 wav + 5 个 ogg = 516 条音频**，`*.wav.meta` **511**、`*.ogg.meta` **5** ⇒ **缺 meta 的 = 0 条**（wav 缺 0、ogg 缺 0；孤儿 meta 0） |
| `--backfill --dry-run` | `音频 516 条 · 补 meta 0 条 · 已存在 516 条` |
| `--backfill`（真写） | `音频 516 条 · 补 meta 0 条 · 已存在 516 条` ⇒ **新增 0 个文件** |
| 为什么是 0 | G3 报告 §三·4 已经把它找到的那一条（`Trait Swarm Tyranid.wav.meta`）手工补上了 ⇒ 本轮遍历时目录已无缺口 |

> 也就是说：**脚本修好了「以后不会再漏」**，但**这一轮没有可回填的欠账**。
> 三个脚本的写入路径已由合成测试覆盖（§四·①），下次真正导入新音频时会自动补。

## 四、验证读数

### ① 合成测试（一次性、临时目录，不落盘）

在 `%TEMP%` 造 5 条音频（3 条带 meta 的兄弟 + 2 条故意缺 meta 的 `NEW.wav` / `NEW.ogg`）：

```
backfill: ['NEW.ogg', 'NEW.wav'] | exists: 3
✅ 合成测试全过（模板/只补不改/guid/行尾/幂等/dry-run）
```

逐条断言：新建 2 条且顺序正确；**3 份既有 meta 逐字节未变**（前后 sha 比对）；`NEW.wav.meta` 与
`Add card to deck.wav.meta` **只差 guid**；`NEW.ogg.meta` 与 `MatchVictory.ogg.meta` 只差 guid；
**wav 模板 ≠ ogg 模板**（证明按扩展名分流生效）；5 个 guid 全 `^[0-9a-f]{32}$` 且互不相同；
新建 meta **LF / 23 行 / CRLF=0**；**第二次跑 0 新建**；`--dry-run` 只报不写。

### ② 真目录只读校验

```bash
cd d:/4/Unity && python -I -X utf8 工具/_sfx_meta.py --verify
```

```
音频 516 条 · 配对后 guid 516 个 · 全工程 meta 24073 份（重复 guid 0 组）
  模板 .ogg: 众数 4/5 · 取自 `Draft Mode GainSkull.ogg`
  模板 .wav: 众数 510/511 · 取自 `Add card to deck.wav`
  ⚠️ 模板与同目录众数不同（1 条这一款）：StartMatch.ogg.meta
  ⚠️ 模板与同目录众数不同（1 条这一款）：OvertimeStart.wav.meta
=== 0 红 / 2 黄 ===
```

- **① 配对**：516/516 有 meta，0 缺。
- **② guid**：516 个全 32 位小写 hex、目录内无重复；**全工程 24,074 个 `*.meta` 里 24,073 个带 guid、0 组重复**
  （差的 1 个是 `MyGame/Library/PackageCache/.../FullScreenPassRendererFeature_OldGUID.cs.meta` —— 60 字节、
  本来就没有 `guid:` 行，不是 Unity 序列化的 meta）。
- **③ 模板逐字段**：`.wav` 众数 **510/511**（= 既有 509 + G3 补的 1，逐字节一致）；`.ogg` 众数 4/5。
  两条黄见 §五（都是**本件之前就存在**的例外，本件没有改它们）。

### ③ 三个脚本自身还能跑（`--check` 只读，不写盘）

| 脚本 | 读数 |
|---|---|
| `import_original_sfx.py --check` | 解析成功 **460/460** cue · 要用到 482 个音频 · 查漏（反向）**0 个候选** · 未写盘 |
| `import_battle_sfx.py --check` | 29 个音频「本地已有 29 ⇒ 要拷 0」· 未写盘 |
| `import_remnant_sfx.py --check` | 六条 cue → 五条真 clip 全 ✅ · 未写盘 |

三处都证明新加的 `import _sfx_meta`（在 `-I` 隔离模式下需显式插 `sys.path`）不会把脚本带崩。

## 五、没查清的部分

| # | 项 | 差什么 |
|---|---|---|
| ① | **`compressionFormat: 2` 到底是不是 Vorbis** | G3 报告 §三·4 和本次任务描述都写成「`compressionFormat: 2` = Vorbis」。按 Unity 的 `AudioCompressionFormat` 枚举（PCM=0 · **Vorbis=1** · **ADPCM=2**），**2 是 ADPCM**。本目录的旁证：`.ogg.meta`（ogg 原生码流就是 Vorbis）用 `1`，wav 用 `2` ⇒ 两者显然不是同一格式。**本件按任务要求「逐字段照抄」执行，既不裁决也不改既有 meta**；如果要改口径，得先拿 Unity 版本/运行期判据确认（按红线本件没跑 Unity）。 |
| ② | **`OvertimeStart.wav.meta` 为什么是「ogg 那套」** | 它是 1 MB / 48 kHz / 立体声 PCM wav，meta 却是 `loadType 2` Streaming / `compressionFormat 1` / `preload 1`；mtime `2026-10-20 21:13`（比 wav 的 09-22 晚）。`工具/rebuild_overtime_start_ogg.py` 里 **grep 不到任何 meta/guid 逻辑** ⇒ 这份 meta 的来源（手写？Unity 生成？别的脚本？）**没查清**。它在白名单外，本件没动。 |
| ③ | **`StartMatch.ogg.meta` 的 `preloadAudioData: 0`** | 同目录另 4 份 ogg 是 `1`。既有文件、来源不明；本件没动。将来的新 ogg 会按众数写 `1`。 |
| ④ | **三个脚本的「真写」路径没有端到端跑** | 非 `--check` 会重写 `animfx_sounds.json` / `battle_sounds.json` / `remnant_cue_props.json` —— **都在白名单外**，所以只验到：语法编译通过 + `--check` 三处通过 + 合成测试覆盖 `ensure_meta` 本身 + 循环代码是 6 行直调。⚠️ 下次主对话真正跑导入时，留意统计行是否出现「补 meta N 条」。 |
| ⑤ | `Resources/Art/audio/` 其它子目录 | 顺手只读扫过：`vo/`（1810 ogg + 626 wav）、`music/`、`bgm/` **当前 0 缺 meta**；且 `vo/*.ogg.meta` 的众数是 `loadType 1/1/0`，与 `sfx/*.ogg.meta`（`2/1/1`）不同 ⇒ 再次证明模板必须按目录取。这些目录本件**没动**。 |

## 六、怎么复跑

```bash
# 只补 meta（幂等；不碰任何 wav / 既有 meta）
cd d:/4/Unity && PYTHONIOENCODING=utf-8 python -I -X utf8 工具/_sfx_meta.py --backfill
# 只数不写
cd d:/4/Unity && PYTHONIOENCODING=utf-8 python -I -X utf8 工具/_sfx_meta.py --backfill --dry-run
# 一次性校验（配对 / guid 全局唯一与格式 / 模板逐字段）
cd d:/4/Unity && PYTHONIOENCODING=utf-8 python -I -X utf8 工具/_sfx_meta.py --verify
# 改动面
cd d:/4 && git diff --numstat -- Unity/工具/import_original_sfx.py \
    Unity/工具/import_battle_sfx.py Unity/工具/import_remnant_sfx.py
```
