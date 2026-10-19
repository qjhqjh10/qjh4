# W57 特效线 · FX-2 · `nPOTScale` 落地（账 V1）

> 执行代理 **FX-2** · 2026-10-19 · 本波口径 = **只做不验**（⛔ 没跑 Unity / 没起 player / 没跑 `*.Run`）。
> 判据来源：`资料/普查产出_1019/W57_特效线_T15_V1_T02静态.md` **§B（V1）** 与 **§B·4**。
> 白名单：`EffectExporter.cs` + 13 个 `.png.meta`。**白名单外一个文件没动。**

---

## 一、两件结论（改前 / 改后 + 判据）

| 件 | 改前 | 改后 | 判据 |
|---|---|---|---|
| **A · 13 张 NPOT `.meta` 的 `nPOTScale`** | 13 张 = **`ToNearest(1)`** | **13 张全部 = `None(0)`**；`WarpforgeVFX/Textures/` 里「NPOT 且 `nPOTScale != 0`」= **0** | 原版 runtime `Texture2D` 尺寸就是 NPOT（`Glow UI W40K` **123×123**、12 张 `trait_icon_*` **80×80**）⇒ 原版导入器**没做 POT 缩放** ⇒ `nPOTScale = None(0)`（FX-1 §B·1 反证链） |
| **B · `EffectExporter` 补 `nPOTScale`** | `ImportTexture` / `FixTextureImportSettings` **都不设**（全文件只有 `RestoreVatTextures` 设） | `ImportTexture` **无条件**写 `None`；`FixTextureImportSettings` 读旁挂表第 10 列 `npotScale` 并按需写 | 同上 + FX-1 §B·4 的改法（`0 = None` / `-1 = 不动`） |

⚠️ **`.meta` 改动不会立即生效** —— 需**下一次 Unity 导入**。本波按口径**没跑 Unity**，所以「运行时读回 80×80 / 123×123」这一条**没验**（留给下一个渲染/导入批）。

### B 的具体改法（`Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`）

| 位置（改后行号） | 改法 |
|---|---|
| `:467` | `int np = ParseOr(c, 9);` —— 旁挂表 `vfx_texture_mips.tsv` 第 10 列（现读 10 列 × 2,984 行；`0:54 · -1:2930`） |
| `:476` | `same` 判据加 `&& (np < 0 \|\| (int)ti.npotScale == np)` —— `-1`（POT/未知）**不动** |
| `:490` | `if (np >= 0) ti.npotScale = (TextureImporterNPOTScale)np;`（`0` = `None`） |
| `:493` | 改动日志带上 `np{np}`（便于下次批处理对账） |
| `:2926`（`ImportTexture`） | **无条件** `ti.npotScale = TextureImporterNPOTScale.None;`（对 POT 输入是 **no-op**；方向 = 不缩放 NPOT，⛔ 不是补成 POT） |

两处都写了判据注释（原版 `None(0)` + FX-1 报告 §B）。**其它导入设置（maxSize / compression / sRGB / mip / wrap / filter / aniso）一格没动。**

顺手订正了两条会因本次改动而**过期**的注释（铁律 5）：
- `FixTextureImportSettings` 的文档注释补上 `nPOTScale` 判据（`:400-401`）。
- `:1072-1076` 那段原写「`ImportTexture` **不设** `nPOTScale` / `sRGBTexture`」⇒ 改成「`nPOTScale` 这一格已不冲突；**sRGB / mip / 不压缩三格仍会被冲** ⇒『别用它重导 VAT』照旧成立」。

---

## 二、13 张逐张清单

路径前缀 = `Unity/MyGame/Assets/WarpforgeVFX/Textures/`

| # | 文件 | 真实像素（PNG IHDR 实读） | POT? | 改前 | 改后 |
|---|---|---|---|---|---|
| 1 | `Glow UI W40K.png` | **123×123** | ❌ NPOT | `1` | **`0`** |
| 2 | `trait_icon_agenda.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 3 | `trait_icon_ambush.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 4 | `trait_icon_Cruelty.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 5 | `trait_icon_Ecstasy.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 6 | `trait_icon_ferocity.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 7 | `trait_icon_oath.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 8 | `trait_icon_sentry.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 9 | `trait_icon_shuriken.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 10 | `trait_icon_stealth.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 11 | `trait_icon_stimulation.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 12 | `trait_icon_synapse.png` | **80×80** | ❌ NPOT | `1` | **`0`** |
| 13 | `trait_icon_vulnerable.png` | **80×80** | ❌ NPOT | `1` | **`0`** |

**改法**：二进制级替换 `\n  nPOTScale: 1\n` → `\n  nPOTScale: 0\n`，**只动这一行**。
13 个文件改前/改后都是 **2,652 字节 · 纯 LF（CRLF=0）· `nPOTScale` 键只出现 1 次**；改完逐个**重开文件回读**比对，13/13 一致。

**没改的一张（按白名单与判据）**：`CardPresentation/Icons/40k_Trait_icon_atlas.png` = **1024×1024 = POT** ⇒ `nPOTScale` 对它不生效，**保持原样（`1`）**。另：`WarpforgeVFX/Textures/` 里其余 NPOT meta（`_sprite` 图集等）现读本来已经全是 `0`。

---

## 三、没查清的部分

1. **原版 importer 的字段本身仍然拿不到**：`TextureImporter` 是编辑器类、不随 player build 打包（FX-1 §B·1 已全库核过 `grep` = 0 文件）⇒ `None(0)` 是**由 runtime 尺寸反证**出来的，不是直接读到的字段。这条**沿用 FX-1 的边界**，本波没有新增证据，也没有推翻它。
2. **`.meta` 改了但没生效**：本波不跑 Unity ⇒ 没有运行时证据（`Texture2D.width/height` 应读回 80/123）。**下一个导入批**跑 `EffectExporter.FixTextureImportSettings` 或任一 Unity 导入后，可用 FX-1 §B·4 第 4/5 条复扫。
3. **`FixTextureImportSettings` 的新分支没有跑过**：`np >= 0` 的写回路径、`same` 里新增的那一格，都只过了**编译**（类型检查 0 错），**没有运行时执行过**（本波口径）。风险低（与既有 `wrapModeU/V` 同一写法）。
4. **POT 贴图上的 `nPOTScale: 1` 仍是 388 张**：按 FX-1 判据这是**无害**的（POT 上该字段不生效），本波**没有**去动它们；`ImportTexture` 无条件写 `None` 以后，将来重导会让它们变成 `0` —— 也只是数值变化、无画面差异。

---

## 四、顺手发现（⛔ 只报不改）

1. **`Unity/MyGame/Assets/WarpforgeVFX/` 整个目录在 `.gitignore:26` 里被忽略** ⇒ 这 13 个 `.meta` 的修改**不会出现在 `git status` / 提交里**（`git ls-files` 对这 13 个路径 = 空）。它们属于**本地生成产物**：下次 `EffectExporter.Run`（非 Resume）会 `ClearGenerated()` 清掉 `Textures/` 再重导 —— 而这正是 **B 件（`ImportTexture` 补 `None`）必须做的原因**，否则重导后 13 张又会回 `ToNearest`。⇒ **B 件是 A 件的「防回退」配套，不是可选优化。**
2. **本目录 NPOT meta 实际是 68 张**（不是 FX-1 报告里那个「25 张」—— 那个数是「413 张按名对上的 PNG」里的子集）。现读 **68/68 全部 `nPOTScale = 0`**；其中 43 张是 `_sprite` 图集切图 / VAT / 我们自己的导出件。
3. `nPOTScale: 1` 的总数从 **401 → 388**（= 401 − 13），与 FX-1 预期一致；剩下 388 张经判据全是 POT。
4. `FixTextureImportSettings` 的 `changedNames` 日志现在会带 `np{-1|0}` —— 下次跑它时能直接看出「这张是不是因为 `npotScale` 才被判为不同」。

---

## 五、验收读数 + typecheck

### A 件复扫（FX-1 §B·4 第 4 条原样）

```bash
python -I -X utf8 - <<'PY'
import glob,os,re,struct
def sz(p):
    b=open(p,'rb').read(33); return struct.unpack('>II',b[16:24])
def pot(n): return n>0 and n&(n-1)==0
bad=[]
for m in glob.glob(r'd:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/*.png.meta'):
    p=m[:-5]
    if not os.path.exists(p): continue
    w,h=sz(p); s=open(m,encoding='utf-8').read()
    v=int(re.search(r'^  nPOTScale: (\d+)',s,re.M).group(1))
    if not(pot(w) and pot(h)) and v!=0: bad.append(os.path.basename(p))
print(len(bad), bad)
PY
```

**改前**：`13 ['Glow UI W40K.png', 'trait_icon_agenda.png', … ×12]`
**改后**：`0 []` ✅

```bash
grep -rl "nPOTScale: 1" d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/ --include="*.png.meta" | wc -l
# 改前 401 / 改后 388（余下 388 张经判据全是 POT，无害）
```

### 生效命令（下一次 Unity 导入；⛔ 本波没跑）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectExporter.FixTextureImportSettings -logFile -
```
预期：13 张已经是 `0`，`same` 命中 ⇒ 它们应计入「本来就对」而不再被改；日志行应为
`[TEXFIX] 表里 2984 行 · **改了 N** · 本来就对 M · …`（N 不含这 13 张的 `npotScale` 贡献）。
可选运行时探针：读这 13 张 `Texture2D.width/height` ⇒ 期望 **80×80 / 123×123**；若读到 **64/128 = 仍在 ToNearest**。

### typecheck

```bash
TMPDIR=/tmp/wf_fx2 bash d:/4/Unity/工具/typecheck.sh
```
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
✅ **运行时 0 / 编辑器 0**（一次通过，无「别人半成品文件」干扰）。

---

## 六、改动清单（绝对路径）

- `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`（+25 / −4，CRLF 保持：3,656→3,677 行，无混行尾）
- `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/Glow UI W40K.png.meta`
- `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/trait_icon_{agenda,ambush,Cruelty,Ecstasy,ferocity,oath,sentry,shuriken,stealth,stimulation,synapse,vulnerable}.png.meta`
  （以上 13 个：各 1 行改动，2,652 B 不变，纯 LF 不变；**目录被 `.gitignore` 忽略，git 不可见**）
