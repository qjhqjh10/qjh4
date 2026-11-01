# B113X3 · 脚本零 guid（`B-227`）＋ 战场 shader 包重出（`B-226`）

> 执行型写手 `X3` · 2026-11-01 · 第一百一十三会话第 2 波（资源/包小组）。
> 白名单：`A/WarpforgeBooster/Prefabs/*.prefab`（16 份，只改 `m_Script`）· `工具/extract_missing_shaders.py`。
> ⛔ 未跑 Unity · 未动 git · 未改账/活状态表/`CLAUDE.md`/`项目任务.md`/`EffectExporter.cs` · 未写 `d:/2` 一个字节
> · 未覆盖任何 `StreamingAssets/WarpforgeVFX/*.bundle`（dry 产物在 `_tmp_view/x3_dry/`）。

---

## ① `B-227` —— `WarpforgeBooster/Prefabs/*.prefab` 那 16 条 `m_Script` guid 全 0

### 那个 pid 到底是哪个类（判据）

| 项 | 值 | 出处 |
|---|---|---|
| pathID | `-1105334708447906859` | prefab 里 `m_Script: {fileID: -1105334708447906859, guid: 000…0, type: 0}` |
| **`m_ClassName`** | **`BoosterPackVisualItem`** | `d:/2/新解包资源/assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_-1105334708447906859.json` 第 3 行 |
| `m_Namespace` | **`""`（全局命名空间）** | 同上 |
| `m_AssemblyName` | **`Assembly-CSharp`** | 同上 |

> ⚠️ 按简报的提醒走的是**包内 `MonoScript_<pid>.json`**（文件名就是 pathID，不是 `globalgamemanagers` 里的小序号）。
> 不需要去 `assetripper_out` 反查 —— 一次就命中。
> 旁证（我改了之后才知道，两处本来就写着这个 pid）：
> `CardPresentation/Shell/BoosterPackVisualItem.cs:24` 文件头「`m_Script.m_PathID = -1105334708447906859` 的**共 17 份**」
> · `WarpforgeArena1/Editor/EffectExporter.cs:5604` 的注释。

### 我们工程里**有没有**这个类 —— **有**

| 项 | 值 |
|---|---|
| 文件 | `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/BoosterPackVisualItem.cs` |
| 类声明 | `public class BoosterPackVisualItem : MonoBehaviour`（`:59`，**全局命名空间**，⛔ 不在 `namespace CardPresentation` 里） |
| 脚本 guid | **`6887142eed1514a469119b825671b66e`**（读 `BoosterPackVisualItem.cs.meta` 第 2 行） |
| 程序集 | `Assembly-CSharp`（`Assets/CardPresentation/` 下 **0 份 `.asmdef`**） |

**字段也逐字对得上**（`myRenderer` / `armyTexts` / `boosterName` / `shakePresets`，见该文件 `:62-71`）⇒ 序列化按名字回填，安全。

### 改了什么

**16 个文件、每个恰好 1 处**，字节级同一物理行原地换（**行数 / 行尾 / 锚点全不变**）：

```
- m_Script: {fileID: -1105334708447906859, guid: 00000000000000000000000000000000, type: 0}
+ m_Script: {fileID: 11500000, guid: 6887142eed1514a469119b825671b66e, type: 3}
```

- `fileID` 用 **`11500000` / `type: 3`** 不是猜的：同一目录的 `Booster Pack Standard.prefab:54265` **本来就是对的**
  （`{fileID: 11500000, guid: 6887142eed1514a469119b825671b66e, type: 3}`），且全目录 12 种 `m_Script` 的
  Unity 脚本一律是 `11500000 … type: 3`。⚠️ 只出现了 **2 种** 非 `11500000` 的引用
  （`1693583408` / `725169324`，都是 `09ad0e0aa7f340ae89ad474f30024412`，**不是**本 pid，未动）。
- 每个文件 **−12 B**（旧 fileID 20 字符 → 新 8 字符；guid 与 type 长度不变）。
- 备份：`d:/4/_tmp_view/x3_bak/`（22 个文件，改前原样）。
- 改动脚本：`d:/4/_tmp_view/x3_fix_script_guid.py`（**不带 `--apply` 只打印 + 校验，不写盘**；
  写前逐个校验「行数不变 + 裸 CR = 0」，不符就 refuse）。

### 验证

```
python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py --fields m_Script \
    d:/4/Unity/MyGame/Assets/WarpforgeBooster/Prefabs
```
- **改前基线**：16 件各 **坏引用 1**（共 16 条，`fileID -1105334708447906859`）；`Booster Pack Standard.prefab` 已是 0。
- **改后**：**19 件全部 `坏引用 0`，退出码 0** ✅
- 默认全字段（`m_Script,m_Sprite,m_fontAsset,m_sharedMaterial,m_Mesh,m_Texture,m_Asset`）复扫同一目录：
  `m_Script 4/0/0/0`（总/空/本地/坏）· `m_Sprite 17/17/0/0` · `m_fontAsset 2/0/0/0` ·
  `m_sharedMaterial 2/0/0/0` · `m_Mesh 35/34/0/0` · `m_Texture 17/17/0/0` · **坏引用 0** ✅
- 行尾复核（二进制数）：19 件全部 `crlf 0 / bareCR 0`，**行数与改前逐件相同**（85698 / 90945 / … 见下表）。
- 全仓 grep `1105334708447906859`：**只剩两处注释文字**（`BoosterPackVisualItem.cs:24`、
  `EffectExporter.cs:5604`），**代码/资产里 0 处**。

### 🔴 为什么这一改「只在本机有效」—— 持久的是代码那一行（交给你粘贴）

`Unity/MyGame/Assets/WarpforgeBooster/` **整棵树在 `.gitignore:216`**（`/Unity/MyGame/Assets/WarpforgeBooster/`）
⇒ 我改的这 16 份 prefab **不进 git**（`git status` 看不到它们是正常的）。
谁重跑一次 `BoosterPackExporter.Run`（或 clone 后第一次导出），这 16 条会**原样回零**。

**根因**：`RepairZeroGuidScripts` 只认 `KnownScriptPids` 表里那三条；这个 pid 不在表里 ⇒ 每次都「出声、改不回来」。

**可粘贴片段 1/2** —— `Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:1734-1740`（现读）：

```csharp
    /// <summary>腿② 认的三种（判据 = 包内 `MonoScript.m_ClassName`，出处见上面那段）。
    /// ⚠️ **不在表里的 pid 照样出声**（⛔ 不猜、也不静默）—— 要补就照同样的出处核一遍再加一行。</summary>
    static readonly (long pid, string cls)[] KnownScriptPids =
    {
        (350208831926335389L,  "UnityEngine.UI.Image"),
        (7477354737935883349L, "TMPro.TextMeshProUGUI"),
        (6627610752530350053L, "TMPro.TextMeshPro"),
        // 🆕 2026-11-01（`B-227`）：`WarpforgeBooster/Prefabs/*.prefab` 那 16 条。
        //   判据 = `assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_-1105334708447906859.json`
        //   → `m_ClassName = "BoosterPackVisualItem"` · `m_Namespace = ""` · `m_AssemblyName = "Assembly-CSharp"`；
        //   工程里那份 = `CardPresentation/Shell/BoosterPackVisualItem.cs`（全局命名空间，guid 6887142e…）。
        (-1105334708447906859L, "BoosterPackVisualItem"),
    };
```

同时把上一行注释的「认的三种」改成「认的四种」（`EffectExporter.cs:1731`，同一次改）。
另外 `:1917` 的 `/// <summary>腿②：…**已知三种** pid…` 也要一起改（⛔ 不改就是两份说法打架）。

**可粘贴片段 2/2**（可选，但建议）—— `EffectExporter.cs:5602-5606` 那段注释**已经过期了**，现读：

```csharp
            // ⚠️ **故意不含 `m_Script`**：那是 `A1298`/`A1299` 的另一族（脚本接不回），与「导出顺序」无关；
            //   现读 `WarpforgeBooster/Prefabs/*.prefab` 有 **16 条** `m_Script` guid0（pid -1105334708447906859，
            //   不在 `KnownScriptPids` 表里）—— 把它们算进来，这条闸会**每次都红**，等于没有闸。
```

改后建议写成（**加进 `KnownScriptPids` 之后**，这 16 条会被腿②改回来；但**仍不建议**把 `m_Script`
塞进 B-144 的闸 —— 那 16 件是**参考件**、不是 B-144 要保的产物，混进来会让闸对导出顺序之外的东西报警）：

```csharp
            // ⚠️ **故意不含 `m_Script`**：那是 `A1298`/`A1299` 的另一族（脚本接不回），与「导出顺序」无关。
            //   ✅ 2026-11-01（`B-227`）订正：原来这里写「16 条 pid -1105334708447906859 不在 `KnownScriptPids` 表里」——
            //   **已补进表**（= `BoosterPackVisualItem`）⇒ 腿②会把这 16 条改回来；盘上那 16 件也已按同一形状修好。
            //   本闸仍**不查 `m_Script`**：这 16 件是**参考件**（`BoosterPackExporter.cs:18`），不是 B-144 要保的产物。
```

⚠️ **我没改 `EffectExporter.cs`（不在白名单）** —— 上面两段是**可粘贴**的，由该文件的写手/调度台落。

> **腿① 为什么没拦住它（顺手查清，供参考）**：`FixUnresolvedScripts` 判的是组件**运行期类型**
> （`c.GetType()`）不是 `m_Script` 的当前值。这 16 件渲出来的组件在内存里**能**解析成我们那个类，
> 理论上腿①该接得回去 —— 但**实测产物落盘仍是 guid0**（`EffectExporter.cs:1702-1707` 那段「内存里的判据会骗人」），
> 所以腿②这张表才是兜底。**这一条我没法在本波证实**（不跑 Unity），列进「尚需确认」。

---

## ② `B-226` —— `wf_arena_shaders.bundle` 没有任何模式能重出

### 现读：这份包是什么（两侧实扫，UnityPy 1.25.3）

| | `wf_arena_shaders.bundle`（盘上） | `battlesharedresources_assets_all.bundle`（源包） |
|---|---|---|
| 字节 | **143,037** | — |
| mtime | **2026-09-21 23:02** | — |
| 内层 CAB | `CAB-68cc81d35f88d5ad277f2717d6e1ac25` | **同一个名字** |
| `AssetBundle` 对象 | `(CAB-68cc81…, pid 1)` ×1 | **同一个 (cab, pid)** |
| `Shader` | **7 个** | **7 个，名字 + pid 逐条相同** |

7 个 shader（pid 逐条列出）：

| pid | 名字 |
|---|---|
| `-3332872045011497075` | `Everguild/FX/Tyranids/Pulsating Mesh`（23 个战场网格在用） |
| `-3691949067916811134` | `Everguild/FX/Tyranids/Tyranid Tentacle`（9 个在用） |
| `-7102121157087500272` | `Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad` |
| `-6664125690151156262` | `Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad` |
| `-3899860870456588838` | `Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad` |
| `2056482399784420826` | `Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad` |
| `-777811199038765606` | `Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad` |

⇒ **源包里恰好也只有 7 个 Shader**、内层 CAB 名与 pid 全同 ⇒ 这一份就是 `repack(battlesharedresources)`
的一次产物，**不存在「源包有而产物没有」的东西**。

### 会写 `*arena*.bundle` 的入口（`工具/` 下 python 全量 grep 的结果）

| 脚本 | 入口 | 输出文件名 | 源包 | 备注 |
|---|---|---|---|---|
| **`extract_missing_shaders.py`** | `run_arenas()`（`--arenas`） | `wf_arena_{slug}.bundle` —— **改前** `battlesharedresources` → **`wf_arena_shared.bundle`（盘上不存在）**；其余按场景名 `wf_arena_{arena1,arena2,arena3,aeldari,…}` | 13 个 `scenes_scenes_battlearena*.bundle` **＋** `battlesharedresources_assets_all.bundle` | **唯一**用 `repack()` 的 arena 口 |
| 同上 | `run_arena_flood()`（`--arena-flood`） | `wf_arena_genestealers.bundle` + `wf_arena_genestealers_noise.bundle` | `scenes_scenes_battlearenagenestealers.bundle` + `duplicateassetisolation_assets_all.bundle` | `repack_tree()` ＋ `merge_imports_into_bundle()`（`B113W10` 昨天新加） |
| 同上 | `run_arena_anims()`（`--arena-anims`） | `wf_arena_anims_{arena1,arena2,shared,astra,blacklegion,darkangels,emperorschildren,genestealers,tau}.bundle`（9 只） | 9 个源包 | `repack_tree()`；`shared` **是另一类东西**（动画 clip，不是 shader） |
| 同上 | `run_arena_prefabs()`（`--arena-prefabs --arena <场>`） | `wf_arena_prefabs_<场>.bundle` | 对应场次场景包 | `repack_tree()` |
| **`extract_mirror_shaders.py`** | `main()`（无模式开关，`--check` 可 dry） | `wf_arena_mirror.bundle` | **源 = `d:/2/unity_run_ref/…/globalgamemanagers.assets`**；**但拿 `wf_arena_shaders.bundle` 当「壳」**（`:76` `SHELL`） | ⚠️ **它读这份包**（只借结构，内容全丢）⇒ 这份包被删它就跑不了 |
| `gen_arena_*.py` / `scripts快照/*` | —— | **不写 `.bundle`** | — | 只生成 manifest / 场景 / 贴图导入参数，与本条无关 |

> 结论：**改前确实没有任何入口能产出 `wf_arena_shaders.bundle`**（`--arenas` 会写成 `wf_arena_shared`，
> 而 `--arenas` 的 `have` 集合**包含它自己** ⇒ 文件在的时候 `miss` 为空、**直接 `return 0`**，
> 连「重出」这件事都不会发生）。

### 改了什么（三条，全在 `工具/extract_missing_shaders.py`）

1. 🔴 **修 `run_arenas()` 的 slug 规则**（`battlesharedresources` 那份**写回 `DEST_ARENA`** = `wf_arena_shaders.bundle`）——
   这就是报告 `B113W10` 建议的那条。⛔ 以后不会再冒出一只内容重复的 `wf_arena_shared.bundle`。
2. 🆕 **新入口 `--arena-shaders`**（`run_arena_shaders()`）：**无条件** `repack(ARENA_SRC, args.out)`，
   不看「当前缺哪些 shader」的差值 ⇒ 文件在的时候也能重出。`--out` 可指到临时目录先 dry 复核。
3. 🆕 **只读对账 `--verify`**（`verify_arena_shaders()`）：产物 ↔ 源包按
   **内层 CAB + pid + 名字**逐条比（3 条判据：Shader 集合相同 / CAB 名相同 / `AssetBundle` 恰 1 个），
   不通过**退出码 1**。`_scan_shader_objects()` 是它的读数口（与 `collect_shader_names()` 不同，**保留 pid 与 CAB**）。

**A523 那张「重打包口」表**也补了一行（`--arena-shaders` 走 **`repack()`**，与 `--arenas` 同一个口，
**没有开第三个口**）；文件头新增 `--arena-shaders` 一节（含「旧 slug 为什么错」的完整判据）。

### 新入口怎么跑（四条）

```bash
# ① 只读：印源包那 7 个 shader（名字 + pid + 内层 CAB），不写盘
python -I -X utf8 d:/4/Unity/工具/extract_missing_shaders.py --arena-shaders --check

# ② 打到临时目录（⛔ 不碰随包）
python -I -X utf8 d:/4/Unity/工具/extract_missing_shaders.py --arena-shaders \
    --out d:/4/_tmp_view/x3_dry/wf_arena_shaders.bundle

# ③ 只读对账（产物 ↔ 源包；不通过退出码 1）
python -I -X utf8 d:/4/Unity/工具/extract_missing_shaders.py --arena-shaders \
    --out d:/4/_tmp_view/x3_dry/wf_arena_shaders.bundle --verify

# ④ 对盘上那一份（只读）：--verify 不带 --out 时默认就查 DEST_ARENA
python -I -X utf8 d:/4/Unity/工具/extract_missing_shaders.py --arena-shaders --verify
```

### dry 复扫读数（7 个 shader 在不在）

| 检查 | 读数 |
|---|---|
| `--check`（源包） | **7 个 Shader** · CAB `['CAB-68cc81d35f88d5ad277f2717d6e1ac25']` · AssetBundle `[('CAB-68cc81…', 1)]` ✅ |
| `--out _tmp_view/x3_dry/…`（真打包，落临时目录） | **143,023 B**（源产物 143,037 B，差 **14 B**）；日志 `保留 7 个 shader + AssetBundle(True)，丢弃 1980 个对象` · `m_Container 重写为 7 条，m_PreloadTable 补齐 7 条` |
| `--verify`（临时产物 ↔ 源包） | ✅① **7/7 逐条相同**（CAB+pid+名字）· ✅② 内层 CAB 同名 · ✅③ AssetBundle 恰 1 个 · **退出码 0** |
| `--verify`（**盘上那份** ↔ 源包，只读） | **同样三项全 ✅，退出码 0** ⇒ 盘上那一份确实正确、不需要被覆盖 |
| **独立第三视角**（我另写的 UnityPy 扫描，不经本工具） | 临时产物：CAB 同名 · AB pid 1 · **Shader 7 个、pid 逐条与源包相同** ✅ |
| **负例**（`--verify --out wf_arena_2.bundle`，故意指错） | 🔴② CAB 名不同 → **`对账结论：🔴 不通过`，退出码 1** ✅（说明这条闸真会红，不是恒绿） |
| **回归**：`--arena-flood --check`（W10 昨天那条） | **rc=0**，日志与昨天一致（`CAB-wfarenagsnoise` 2 个对象、流瘦身 16648→64 KB），未写盘 |
| **回归**：`--arenas --check` | **rc=0** · 19 个名字 · 还缺 4 个（全在场景包里，与 `battlesharedresources` 无关）⇒ **我的 slug 改动没有改变 `--check` 的输出** |
| 盘上 `wf_arena_shaders.bundle` | **一个字节没动**：143,037 B · mtime **Sep 21 23:02** |

> ⚠️ **`--arenas` 不是推荐的重出路径**：它现在（`miss` 非空时）会一并写出多只 `wf_arena_{1,2,3,aeldari,…}.bundle`
> （其中 `wf_arena_2` / `wf_arena_tauviorla` 盘上已存在，会被**覆盖**）。要「只重出这一份」就用 `--arena-shaders`。

---

## 改动清单（文件 + 净行数）

| 文件 | 净行数 | 说明 |
|---|---|---|
| `MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack All Armies Variant.prefab` | **0**（85698 → 85698） | 同一物理行原地换 `m_Script`（−12 B） |
| `…/Booster Pack Standard Aeldari SaimHann.prefab` | **0**（85698） | 同上 |
| `…/Booster Pack Standard Astra Militarum.prefab` | **0**（90945） | 同上 |
| `…/Booster Pack Standard Chaos SM Black Legion.prefab` | **0**（90721） | 同上 |
| `…/Booster Pack Standard Emperors Children.prefab` | **0**（90593） | 同上 |
| `…/Booster Pack Standard Genestealers.prefab` | **0**（85794） | 同上 |
| `…/Booster Pack Standard Necrons Sautekh.prefab` | **0**（85698） | 同上 |
| `…/Booster Pack Standard Orks Goff Expansion.prefab` | **0**（95790） | 同上 |
| `…/Booster Pack Standard Orks Goff.prefab` | **0**（95790） | 同上 |
| `…/Booster Pack Standard Sororitas.prefab` | **0**（85931） | 同上 |
| `…/Booster Pack Standard Space Marines Dark Angels.prefab` | **0**（85698） | 同上 |
| `…/Booster Pack Standard Space Marines Space Wolves.prefab` | **0**（100671） | 同上 |
| `…/Booster Pack Standard Space Marines Ultramarines Expansion 1.prefab` | **0**（85698） | 同上 |
| `…/Booster Pack Standard Space Marines Ultramarines.prefab` | **0**（85698） | 同上 |
| `…/Booster Pack Standard TauEmpire.prefab` | **0**（85942） | 同上 |
| `…/Booster Pack Standard Tyranids Leviathan.prefab` | **0**（85794） | 同上 |
| `工具/extract_missing_shaders.py` | **+163**（3,034 → **3,197** 行 · 209,719 B） | 头注一节 + A523 表一行 + `run_arenas` slug 修正 + 3 个新函数（`_scan_shader_objects` / `verify_arena_shaders` / `run_arena_shaders`）+ argparse 两条（`--arena-shaders` / `--verify`）+ `args.out` 默认链 + `main()` 分发 |

行尾复核（二进制数）：
- 16 份 prefab：全部 **`crlf 0 / bareCR 0`**，行数与改前**逐件相同**（见上表），**未翻行尾**。
- `extract_missing_shaders.py`：**`crlf 0 / lf 3,197 / bareCR 0`**（改前 `lf 3,034`，**纯 LF，未翻**）；`py_compile` 通过。
- `.cs` 一个都没改 ⇒ **不需要 `typecheck`**。

临时产物 / 备份（都在 `_tmp_view/`，不入库）：
`_tmp_view/x3_bak/`（16 份 prefab 改前原样）· `_tmp_view/x3_dry/wf_arena_shaders.bundle`（143,023 B dry 产物）·
`_tmp_view/x3_fix_script_guid.py`（改动脚本）· `_tmp_view/x3_list_shaders.py`（独立 UnityPy 扫描器，第三视角复核用）。

---

## 顺手发现（⛔ 我没动，交调度台）

1. 🔴 **`extract_mirror_shaders.py:76` 拿 `wf_arena_shaders.bundle` 当「壳」**（`SHELL`）。
   它只借结构（包里的东西全丢），但**文件必须存在** ⇒ 这份包被删的话，`wf_arena_mirror.bundle`
   也重不出来（`--mirror` 直接崩在 `UnityPy.load(SHELL)`）。
   ⇒ 现在有 `--arena-shaders` 可以先把壳重出来，链条不死了；但**更好的做法**是把 `SHELL` 改成
   「随便哪个 arena 小包」（`extract_mirror_shaders.py:74` 的注释本来就说「借哪个包都行」）。
   **该文件不在本波白名单**，我没动。
2. ⚠️ **`--arenas` 现在（`miss` 非空时）会覆盖盘上已有的 `wf_arena_2.bundle` / `wf_arena_tauviorla.bundle`**，
   并且会新写 `wf_arena_1/3/aeldari/…` 一批。这是**改前就有的行为**（不是我引入的），
   但 `--arenas` 的 `--help` 文字写着「源 battlesharedresources → 目标 wf_arena_shaders」，
   **实际会写 15 只包** —— 措辞与行为不符，建议改成「13 个场景包 ＋ 1 个共享包」。
   （我只在 `--arena-shaders` 的 `--help` 里把这件事说清了。）
3. ℹ️ `Booster Pack Standard.prefab` 那件**本来就是对的**（`m_Script` guid 已经指向我们的类）——
   说明这类「16 件里恰好 1 件对」的形状不是导出器的确定性行为，**更像某次手工修过那一件**
   （或那一件的组件走了腿①）。这条可以帮「尚需确认 1」定位。
4. ℹ️ `extract_missing_shaders.py` **不带参数 = 真写 `wf_shaders_extra.bundle`**（不是打印用法）。
   与 CLAUDE.md「写盘类脚本默认必须打印用法并退出」不符，但那是**全仓统一的老约定**
   （`-h/--help` 是对的：argparse 自动加、提前退出、写过任何东西之前）⇒ 我没去改它，
   只在 `--arena-shaders` 这一条上保证了「`--check` / `--verify` 都只读」。

---

## 尚需确认

1. **`EffectExporter` 那两段补丁还没落**（`EffectExporter.cs` 不在本波白名单）⇒ **重跑一次
   `BoosterPackExporter.Run` 就会把这 16 条打回 guid0**。落补丁之前，本条的「修好」只到「盘上这一份」为止。
   落完之后建议跑一次 `BoosterPackExporter.Run`，看日志有没有
   `A1298 落盘后改回 **1** 条 m_Script = BoosterPackVisualItem`（16 件各 1 条）。
2. **腿① 为什么没接回去**（见上「腿① 为什么没拦住它」）：本波不跑 Unity，没验。
   若落了 `KnownScriptPids` 之后腿② 报 `repaired 16 / left 0`，这条就**不必再查**（兜底已生效）；
   若报「左边还剩」，那要回头看腿①。
3. **`wf_arena_shaders.bundle` 真覆盖没做**（按简报要求：真覆盖留给调度台在 Unity 窗口里做）。
   要做的话：先 `--arena-shaders`（不覆盖前先跑 `--verify` 确认盘上那份是好的，本波已验 = 好的），
   覆盖后**务必再跑一次 `--verify`**，并进 **利维坦战场** 看肉搏动 / 触手 —— 那 2 个 shader 是这次的主要受益者。
4. **跨包依赖未验**（沿用 `B113W10` 的同一未验点）：本波没有新增跨包引用，`wf_arena_shaders.bundle` 里
   7 个 shader **没有 `m_Texture`/别包依赖**（`repack()` 只留 Shader + AssetBundle，容器 7 条）⇒ 风险低于水漫那条。
