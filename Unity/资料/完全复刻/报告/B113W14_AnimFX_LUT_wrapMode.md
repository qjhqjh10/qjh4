# W14 · AnimFX 14 张 LUT 的 wrapMode（`B-119` 续账 · 同一缺陷的第二处）

> 写手代理 `W14` · 第一百一十三会话 · 第 1 波 · 2026-11-01
> 白名单内完成：① 14 份 `.meta` ② `工具/import_original_luts.py` ③ `B113W13_Assert.cs` 追加 F 组。
> ⛔ 未跑 Unity、未动 git、未改账/活状态表/`CLAUDE.md`/`项目任务.md`/`BattleScene.cs`。

---

## W14

### ① 14 份改动（文件名 + 改前/改后 + 字节长度未变自证）

**判据（我自己现读双证，未沿用 W13 的结论）**

| 来源 | 路径 | 现读 |
|---|---|---|
| ① 原版导出工程 | `d:/2/tools/assetripper_out/ExportedProject/Assets/Texture2D/LUT*.png.meta` | 20 份 `LUT*` **全部** `wrapU: 0 / wrapV: 0 / wrapW: 0`（含 `LUT_Dark`）；⚠️ 其中**没有 `LUT Normal`** |
| ② 包内实读 | `d:/4/Unity/数据/游戏数据/vfx_texture_mips.tsv`（列序 `name width height mipCount bundle wrapU wrapV filterMode aniso npotScale`） | `LUT *` 共 **20 行**（行 1864–1883），**`wrapU=0 / wrapV=0` 全部命中**，**含 `LUT Normal`**（bundle = `battlesharedresources_assets_all`） |

⇒ **14 张原版全是 Repeat（`0`），没有例外**（任务里「若有不是 Repeat 的，停下给反证」这一条不触发）。
枚举值：`TextureWrapMode.Repeat = 0` / `Clamp = 1`。

**改动明细（14/14，全部 `wrapU/V/W: 1 → 0`）**

| # | 文件 | 改前 | 改后 | 改前 len | 改后 len | 逐字节差异 |
|---|---|---|---|---|---|---|
| 1 | `LUT Blind.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 2 | `LUT Blizzard.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 3 | `LUT Blue Tint.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 4 | `LUT Dimensional Breach.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 5 | `LUT Invert.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 6 | `LUT Normal.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 7 | `LUT Nuclear.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 8 | `LUT Overexpose High.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 9 | `LUT Pink Emperors Children.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 10 | `LUT Pink Emperors Children Extreme.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 11 | `LUT Poster.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 12 | `LUT Red Hell.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 13 | `LUT Red Tint.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |
| 14 | `LUT_Dark.png.meta` | 1/1/1 | 0/0/0 | 2651 | 2651 | 3 |

**「只动了那三个数字」的自证**：每份**逐字节 diff 恰好 3 处**（`1`→`0` × 3），
`len` 全部 2651 → 2651（**等长**），行尾 `crlf=0 / lf=117 / 裸CR=0` **改前改后完全一致**。
读写一律 `io.open(p,'rb')` / `'wb'`（⛔ 不经文本模式）；**改完逐份回读**再由独立的第二遍读校验。
14 份 `.png` **一个字节没动**（mtime 全是 `1790824729`，与改动前一致；尺寸也未变）。

**改法**：临时脚本 `d:/tmp/wf_w14_fix_wrap.py`（**在仓库外**，不污染工程；先 `--check` 干跑再执行）。
⚠️ 注意：这个目录 **被 `.gitignore:46` 忽略**（`/Unity/MyGame/Assets/Resources/WarpforgeVFX/`），
所以这 14 份是**本地产物、不进 git** —— 真正落进仓库的修复是下面 ② 那个生成器。见「顺手发现 ①」。

---

### ② 根因修法（工具:行号 改前 → 改后 + 自检怎么加）

**改前**（`工具/import_original_luts.py:53-54` 常量 + `:138-145` 写盘）：
```python
META_TEMPLATE = ... "WarpforgeLUT Battle Arena Aeldari.png.meta"   # 只换 guid，其余**逐字克隆**
...
out = ["guid: " + g if ln.startswith("guid: ") else ln for ln in tpl.split("\n")]
io.open(dst + ".meta", "w", encoding="utf-8", newline="\n").write("\n".join(out))
```
⇒ 模板当时是 **Clamp**，14 份一起继承。**而且模板对不对完全靠外部文件「恰好是对的」**。

**改后**（同一文件，现读行号）：

| 位置 | 内容 |
|---|---|
| `:131` | `WRAP_KEYS = ("wrapU", "wrapV", "wrapW")` |
| `:134` | `def force_repeat_wrap(text)` —— 正则 `^([ \t]*)wrap([UVW]):[ \t]*\d+[ \t]*$` → `wrap%s: 0`，**三轴一起**写，返回命中数 |
| `:149` | `def meta_wrap_ok(path)` —— 读回 `.meta`，三轴申明，**任何一轴不是 `0` 就判不过**（并且 `sRGBTexture: 0` 也在判） |
| `:181` | `tpl, wrap_hits = force_repeat_wrap(tpl)`；**命中数 != 3 ⇒ 直接停手**（`return 1`），⛔ 不静默克隆 |
| `:220` | 写盘改成 **二进制** `io.open(dst + ".meta", "wb")`（不翻行尾） |
| `:223` | **写盘后读回自检** `ok, why = meta_wrap_ok(dst + ".meta")`；不过就 `return 1` 并印出原因 |

**三条设计取舍（都写进了文件头注释）**
1. **不依赖「模板恰好是对的」** —— 模板是外部文件，随时可能被改回 Clamp（这次就是这么错 14 份的）⇒ 克隆后**显式钉 0**。
2. **三轴一起写** —— `.meta` 里 `ti.wrapMode` 一次设三轴；只改 U/V 会让 meta 与运行期不一致。
3. **写盘类脚本的默认行为**（`:48` USAGE + `:57` 早退 + `:170` 门）：
   **不带参 / `-h` / `--help` ⇒ 只打印用法并退出 0，零读零写**；`--check` = 只读体检；**真写盘必须显式 `--apply`**；

**实测读数**

| 命令 | 结果 |
|---|---|
| `python -I -X utf8 import_original_luts.py --help` | rc=0，仅打印用法（5 行） |
| `python -I -X utf8 import_original_luts.py`（不带参） | rc=0，**只打印用法**，不建目录、不写文件 |
| `python -I -X utf8 import_original_luts.py --nonsense` | rc=2，打印用法（带了没用的参数也**不干活**） |
| `python -I -X utf8 import_original_luts.py --check` | rc=0，14 张逐张报；`LUT Overexpose High` 报 **64×4**（原版本来就是 64×4，见 `资料/普查产出_1001/资产导入路三件_侦察.md` §3·7，**不是缺陷**） |
| `python -m py_compile` | OK |

**端到端等价自证（内存里跑，不写 PNG）**：按 `main()` 的**同一算法**逐张生成 meta 文本，与**盘上那份**逐字节比 ——
**14/14 完全相同**，且三轴都是 `0`。⇒ 「盘上产物 = 生成器现产出」，不会「重跑一次又变回 1」。
另跑了两条负例：① 把模板人为改回 Clamp，`force_repeat_wrap` **仍产出 0**（证明修法不依赖模板）；
② 把一份 Clamp 文本喂 `meta_wrap_ok`，**判不过**（证明自检不是恒真）。

---

### ③ 断言 F 组（读什么、为什么不是同义反复）

**位置**：`A/CardPresentation/Editor/B113W13_Assert.cs` **类尾追加**（`F_AnimFxLuts()` + 两个辅助函数，A–E 组一行未动；
`Run()` 里只加了 1 行 `F_AnimFxLuts();`）。文件 320 → **552 行**（+232）。**共 131 条断言**。
（该文件**无任何 `账/*.tsv` 行号引用**，已 `grep` 确认 ⇒ 加行不会造成账漂移。）

**读什么（四面，不是一句话说两遍）**

| 面 | 读的东西 | 为什么不是同义反复 |
|---|---|---|
| **前提** F1–F3, F10a…, F30 | 目录存在 · `.png.meta` **份数 == 14** · `.png` **份数 == 14** · 14 份都读得到 | 防「改名/删掉一份之后，循环只检查剩下的 13 份 → 悄悄变绿」。⚠️ **不按 `LUT ` 前缀筛**（`LUT_Dark` 的前缀是 `LUT_`，按前缀会数成 13） |
| **产物** F10b…F23d（14×3） | 盘上 14 份 `.meta` 文本的 `wrapU/V/W` | 把任一轴改回 `1` ⇒ 当场红 |
| **生成器** F31–F40 | `工具/import_original_luts.py` **源码文本**，抠 `force_repeat_wrap` / `meta_wrap_ok` 的**函数体**（`ExtractPyFunc`，到下一个顶格 `def` 为止） | **另一个文件、另一个方向**：退回「逐字克隆」⇒ F34 红；退回写 Clamp ⇒ F36 红；自检只定义不调用 ⇒ F38 红（现读 2 处）。**结构上不可能被一次改动同时满足两边** |
| **判据源** F41–F57 | ① 生成器克隆的**模板源** `.meta`（F41/F42）② **原版导出** `.meta`（13 份，F4x-a/b）③ **`vfx_texture_mips.tsv`**（14 份全覆盖，F4x-c/d） | 证明期望值 `0` **来自原版**，⛔ 不是「我们自己的产物证明我们自己」。TSV 那一路**覆盖 `LUT Normal`**（原版导出工程里没有它） |

**反自证（防「解析器恒真」—— 0 正是期望值，用 0 兜底就恒绿）**
- F60/F61：`ExtractPyFunc` 喂合成 python 文本 —— 必须**停在下一个顶格 `def`**（否则别的函数里的 `: 1` 会误伤/误保），找不到名字必须 `found=false` + 空串（⛔ 不许退回整份源码）。
- F62/F63：`WrapFromTsv` 喂合成 TSV —— `wrapU=1 / wrapV=1` 必须读成 `1/1`；找不到名字必须返回哨兵 `-999`（⛔ 不是 `0`）。
- `WrapOf` 的哨兵沿用 W13 的 `SentinelMissing = -999`。

**前提/结论的写法**：每条都先写**硬前提断言**，结论**无条件**断言（⛔ 没有 `if (x != null) Check(...)` 这种「前提不成立就静默跳过」）。

**静态复核**（**不跑 Unity**，用 Python 像素级复刻三个解析器 + 逐条跑 F 组语义）：
`d:/tmp/wf_w14_verify_assert.py` —— **131 条全 ✅**；另跑三条反例确认「该红的时候真会红」：
① 生成器退回逐字克隆 ⇒ F34 判红；② 某份 meta 改回 `wrapU: 1` ⇒ 那条判红；③ 抠取器返回整份源码的假设不成立（F60 钉住）。

⚠️ **本组尚未在 Unity 里真跑过**（本波不跑 Unity）—— 以上是静态复核，接线已由调度台做好（`BattleScene` 合计行），下一波跑 `BattleScene.Run` 时看 `[B113W13] === 合计` 那一行。

---

## 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_w14 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0**（改了 3 次、跑了 3 次，最后两次都是 0/0） |
| 14 份 `.meta` 独立第二遍读 | `U=0 V=0 W=0` × 14；`len=2651`；`crlf=0 lf=117 裸CR=0` |
| 生成器现产出 vs 盘上 | **14/14 逐字节相同** |
| `import_original_luts.py --help / 不带参 / --nonsense` | rc = 0 / 0 / 2，**都只打印用法** |
| `import_original_luts.py --check` | rc=0，14 张逐张报（唯一「不符」是原版本来 64×4 的 `Overexpose High`） |
| `py_compile` | OK |
| 行尾 | 两个文件都仍是 **LF-only**（`crlf=0 / 裸CR=0`） |

---

## 改动清单

| 文件 | 净行数 | 说明 |
|---|---|---|
| `Unity/工具/import_original_luts.py` | **+89 / −8** | 文件头补判据 · USAGE + 不带参早退 · `force_repeat_wrap` · `meta_wrap_ok` · `--apply` 门 · 二进制写 · 写盘后自检 |
| `Unity/MyGame/Assets/Resources/WarpforgeVFX/LUT/*.png.meta` × **14** | ±0（各 3 字节，等长） | `wrapU/V/W: 1 → 0`。⚠️ 该目录 **被 `.gitignore:46` 忽略**，不进 git |
| `Unity/MyGame/Assets/CardPresentation/Editor/B113W13_Assert.cs` | **320 → 552（+232）** | 类尾追加 **F 组 131 条** + `ExtractPyFunc` / `CountOccurrences` / `WrapFromTsv`；`Run()` +1 行；文件头 +7 行 |

（临时脚本 `d:/tmp/wf_w14_fix_wrap.py` / `d:/tmp/wf_w14_verify_assert.py` **在仓库外**，不污染工程。）

---

## 顺手发现 / 尚需确认

### ① 🔴 这个目录**被 gitignore 了** ⇒ 「修 14 份」在下个环境里不算数

- `.gitignore:46` 有 `/Unity/MyGame/Assets/Resources/WarpforgeVFX/`（连 `Resources.meta` 也忽略）。
  ⇒ 那 14 份 `.meta` 是**本地产物**，`git status` 里**看不到**（我这份改动也不会进 commit）。
- **真正落进仓库的修复 = 生成器那 89 行**。任何新环境只要跑一次
  `python -I -X utf8 d:/4/Unity/工具/import_original_luts.py --apply`，14 份就会被建成 **Repeat**。
- 🔴 **副作用（要调度台知道）**：F 组的前提断言（F1/F2/F3）在**没有导过资产的干净环境**里会**红**。
  这与 W13 的 E1 同一口径（「判据源不存在 ⇒ 必须红而不是静默跳过」），但**跑 `BattleScene.Run` 时会看到红**——
  按 `资料/命令速查.md:409-410` 的说法，那是「资产还没导」而不是「代码坏了」。
  ⚠️ 这句解释**现在需要加一句「跑 `--apply`，不是裸跑」**（见 ②）。

### ② ⚠️ `资料/命令速查.md:415` 的命令**已经过期**（不在我白名单，没改）

那里写的是：
```
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/import_original_luts.py
```
按 W14 的新口径，**这条现在只打印用法、什么都不写**。应改成同一个命令加 `--apply`。
（同段还写着「先干跑一遍：后面加 `--check`」—— 那句仍然成立。）

### ③ ℹ️ `LUT Overexpose High` 是 **64×4**，`--check` 每次都会标黄

这是**原版本来如此**（7 个效果用它，原版同 UV 采样会把它拉伸填满），
判据 → `资料/AnimFX_实现与接线.md` §④ 末 + `资料/普查产出_1001/资产导入路三件_侦察.md` §3·7。
不是缺陷，agent 别被它带跑。

### ④ ⏳ W13 报告 §② 那两处非 wrap 差异仍然开着（本次不动）

`textureFormat`（原版导出 `3` vs 我们 `1`）与 `Standalone` 块 `textureCompression`（`2` vs `1`）。
W13 已论证「两块 `overridden: 0` ⇒ 不生效」，但**真正可当判据的是 bundle 里的 `Texture2D` 字段**，
那条还没人查。建议下一波按 `arena_texture_import.tsv` 的口径核一次（`m_TextureFormat` 到底是多少）。

### ⑤ ℹ️ 为什么这次是「改生成器 + 改产物」两件一起做

只改产物 ⇒ 下次谁跑一次脚本又变回 Clamp；只改生成器 ⇒ 盘上 14 份**现在仍是错的**。
两件都做、外加 F 组把「产物 + 生成器」两面钉住，才不会再犯第二次（这正是 `B-119` 与这条的共性）。
