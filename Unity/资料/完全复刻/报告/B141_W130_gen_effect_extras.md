# B141 · W130 · `gen_effect_extras.py`（O3 包）

- 批次 `B141` · 会话 128 · 写手 `B141-W2`
- 独占文件：`Unity/工具/gen_effect_extras.py`（产物 `Resources/WarpforgeVFX/effect_extras.json` 被 `.gitignore` 忽略，由生成器写出）
- 判据：`资料/完全复刻/判据/B141_W130_对账_资源.md` **§一 16-09** · **§二 W130-O3** · `资料/完全复刻/报告/B138_审计汇总_问题清单.md` **§16 16-09**
- 未做：未跑 Unity · 未动 git · 未碰 `d:/2` 一个字节（导出集只读）

---

## 一、R10 顺手发现 #1：裸跑会写盘（纪律缺陷）

**改前**：`main()` 只判 `dry = "--dry" in sys.argv` / `check = "--check" in sys.argv`；
两者都没有就**直接落到** `io.open(OUT,"w")` ⇒ **裸跑一次就把盘上 JSON 重写**。
这违反项目纪律「会写盘的脚本，不带显式动作参数时必须什么都不做」（先例：`merge_ledger_parts.py` 冲掉 232 行手工修订）。

**改后**：动作参数三选一，**必须显式**。

| 命令行 | 行为 | rc |
|---|---|---|
| 裸跑（无参数） | 打印 `USAGE`，**零写盘** | **2** |
| `--dry` | 扫描 + 打印统计，**零写盘** | 0 |
| `--check` | 与盘上文件逐字节比对（一致 / 不一致），**零写盘** | 0 / 1 |
| `--write` | 写盘 | 0 |
| 未知参数（`--all`）· 一次带多个（`--dry --write`）· 裸位置参数（`foo`） | 打印 `USAGE`，**零写盘** | **2** |

实现：`main()` 开头取 `acts = [a for a in sys.argv[1:] if a.startswith("--")]` +
`stray = [...非 -- 开头...]`，不是「恰好一个合法动作」就 `print(USAGE); return 2`。
`USAGE` 常量与模块头「用法」一节同步改写。

## 二、16-09：`Card 3D Death Explosion` 漏收 `DestroyByTime`

### 判据原文（两处一致）

- `B138` §16 16-09：现读 `effect_extras.json` 8 件只收 7 → 「`Card 3D Death Explosion`」「lifeTime = 2.2」「缺条目 ⇒ 走播放器默认寿命」
- `B141_W130` §一 16-09：`DestroyByTime` **9 条**，`Card 3D Death Explosion` **无条目**（88 件特效）；该改哪 = `effect_extras.json` 补一条 `DestroyByTime lifeTime=2.2`

### 根因（现读，不是猜）

脚本用 `os.path.join(EXPORT, nm + ".prefab")` **只在导出集根目录**找 prefab。
而 `Card 3D Death Explosion.prefab` 在 **`ExportedProject/Assets/GameObject/`** 子目录
（`d:/2/tools/assetripper_out/ExportedProject/Assets/GameObject/Card 3D Death Explosion.prefab`）⇒
被 `missing += 1` **静默跳过**。旧基线打的「导出集缺 15 件」里，**13 件**其实都在 `GameObject/`。

原版组件本体（只读实据）：该 prefab 的 `!u!114 &114044625135789424`（`m_Script.guid = e645387f3fa791242d3d9217cdf87310`
→ `Scripts/Assembly-CSharp/DestroyByTime.cs.meta` 反查 = 类 `DestroyByTime`）`m_Enabled: 1`、`lifeTime: 2.2`。

### 改法

新增 `prefab_paths()`：**整棵导出树**建「prefab 基名 → 完整路径」索引（同名取第一份），`main()` 改用它查。
- 实测这 978 个特效名里**重名 0 组**，不会取错。
- 只此一处改动即解决 16-09，未改 `WANT` 类表、未改任何抽取逻辑。

### 结果（`--write` 后的实差）

`effect_extras.json` diff = **10 行新增**（唯一一条新增条目，其余逐字节不变）：

```
+   "effect": "Card 3D Death Explosion",
+   "extras": [
+    {
+     "kind": "DestroyByTime",
+     "node": "Card 3D Death Explosion",
+     "f0": 2.2
+    }
+   ]
```

| 指标 | 改前 | 改后 |
|---|---|---|
| 特效条数 | 978 | 978 |
| 导出集缺件 | **15** | **2**（`Explosion Hand UI 0,5s delay` · `Vanguard Frame Animated VAT`，`find` 全树 0 命中） |
| 带额外组件的件数 | 88 | **89** |
| 挂载总数 | 164 | **165** |
| `DestroyByTime` | **9** | **10** ✅（16-09 目标） |
| 其余 10 种 kind | — | **逐值不变**（MaterialFlickerEffect 51 · SyncParticleSeeds 40 · ShakeAndSoundObject 35 · SubEmitterManualTrigger 12 · ParticleFindFloorCollider 9 · ParticleEmitOnEnable 2 · ShaderProperySetter 2 · SpriteFlickerEffect 2 · InitiMaterialWithRandomValue 1 · ParticleForceForceFieldController 1） |

`node` 写的是根路径 `Card 3D Death Explosion`（无 `#` 段）—— `WFEffectModule.ResolvePath` 首段与根名逐字相符 ⇒ `start=1`、循环不跑、返回 `root`，**根节点路径可解析**。
运行侧 `WFEffectExtras.cs:198` 已有 `case "DestroyByTime"`，无需新增 kind。

## 三、验证记录（全跑过）

| # | 命令 | 结果 |
|---|---|---|
| 1 | `python -m py_compile 工具/gen_effect_extras.py` | OK |
| 2 | **裸跑** `python -I -X utf8 工具/gen_effect_extras.py` | **rc=2**，打印 USAGE；盘上 `mtime=1793723810 size=41919 sha256=a7e1e270…3ee09` **跑前跑后逐字节相同** |
| 3 | `--dry` | rc=0，打印统计；文件 sha **不变** |
| 4 | `--check`（改后首次） | **rc=1**（盘上还是旧表，符合预期：差 10 行）；文件 sha **不变** |
| 5 | `--write` | rc=0，写 42,085 字节 |
| 6 | `--check`（写后） | **rc=0 一致 ✅** |
| 7 | 负例 `--all` / `--dry --write` / `foo` | 三条**全部 rc=2**、零写盘 |
| 8 | 耗时 `--dry` | 2.1 s（多一次 `os.walk` 可忽略，`asset_names()` 本来就整树走一遍） |

## 四、顺手发现（均已在本包内顺手修 / 或仅记录）

1. ✅ **已顺手修**：旧代码 `print("已写 …（%d 字节）" % len(text))` 印的是**字符数**却写成「字节」
   （中文一字 3 字节，实测少报 44：`len(text)`=42041 vs 真字节 42085）。
   改成 `len(text.encode("utf-8"))`。⚠️ 这行**不影响产物内容**，`--write` 后 `--check` 仍 rc=0。
2. 🟠 **未改（不在本包范围）**：旧基线的「导出集缺 15 件」是两个数混出来的 —— 13 件在 `GameObject/`（本轮已覆盖），
   另 2 件 `Explosion Hand UI 0,5s delay` · `Vanguard Frame Animated VAT` **导出工程里真的没有**。
   现读脚本会把缺件名单**打印出来**（新增一行「导出集缺件（prefab 名）：…」）⇒ 以后不再静默。
   是否要从 `assets_full` 另行补这两件，属资源覆盖线，**未做**。
3. ⚠️ **记录、未验证**：`Card 3D Death Explosion` 现在会在运行时被挂上 `DestroyByTime(2.2)`，
   而它的播放口是 `CardFeel.DeathExplosion`（`CardPresentation/Core/CardFeel.cs:2344`）。
   本包没跑 Unity，**未在 Play 里确认**「2.2 s 后整件被收掉」是否与原版观感一致 —— 需要一次 Play 验收（W132 或真机批次）。
   `WarpforgeArena1/Editor/AnimFXCheck.cs:2169-2178` 的 R28 断言只涉及另一件（`Blast_Big_Ork_Missiles` 那条 `m_Enabled=0`），
   与本条新增**不冲突**（静态判断，未跑）。

## 五、还差什么

- 盘上产物已重生成并 `--check` 一致；**换机器 / 清库后仍需重跑**（该 JSON 被 gitignore）。
- 16-09 的**运行期生效**未验（要 Unity / Play）。其余 16-xx 条目不属本包。
