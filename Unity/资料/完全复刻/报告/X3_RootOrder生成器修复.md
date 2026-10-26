# X3 · `gen_effect_extras.py` 兄弟下标修复（`m_RootOrder` → `m_Children`）

日期：2026-10-26 · 执行代理 X3 · 会话 83

## ① 根因确认（与 D2 报告一致，已独立复核）

- `Unity/工具/gen_effect_extras.py:134`（旧）`sib = re.search(r"m_RootOrder: (\d+)", body)` —— **`m_RootOrder` 在 Unity 2023+/6 已被删除**。
  实测：`D:/2/tools/assetripper_out/ExportedProject/Assets/Pray_Idle.prefab` 与工程里的
  `Unity/MyGame/Assets/WarpforgeVFX/Prefabs/Pray_Idle.prefab` **都不含 `m_RootOrder`**（`grep` 计数 0）⇒ `sib = None` ⇒ 段下标恒 0。
- 层级两边一致：`Pray_Idle`(根，`m_Father: 0`) → `m_Children = [Sparks, Pray VFX mesh]` ⇒ **`Pray VFX mesh` 的兄弟下标 = 1**。
- 后果：`effect_extras.json` 写成 `Pray_Idle/Pray VFX mesh`（下标 0 被省略），而
  `WFEffectModule.ResolvePath`（`WFEffectModule.cs:205-247`）是 **`GetChild(idx)` 取完再逐字比名字、不按名字筛**
  ⇒ 第 1 段拿到 `Sparks`、名字对不上 ⇒ 返回 null ⇒ `WFEffectExtras.Attach`（`WFEffectExtras.cs:149-156`）
  `NodeMisses++` + 警告 + **该组件整件不补**。与 `animfx.log:2320` 的日志逐字吻合。
- **影响面比「1 处」大得多**：离线全量复核（见 ④）显示 **164 处挂载点里有 94 处解析不出来**；
  `AnimFXCheck` 只报了 1，是因为它只实例化了少数几件特效，而 `effect_extras` 的节点**没有全量 sweep**
  （`AnimFXCheck.cs:178-211` 那一段全量 sweep 只覆盖 AnimFX 模块的 `__node` / `@node:`，不含 extras）。

## ② 改了哪几行（只动 `Unity/工具/gen_effect_extras.py`）

| 行 | 改动 |
|---|---|
| `:24-26` | 文件头「节点路径」节补口径：下标 = 父 `m_Children` 里的位置，⛔ 别读 `m_RootOrder`，并记下本次事故 |
| `:109-121` | **新增 `yaml_name()`** —— `m_Name` 值的 YAML 单引号脱壳（见 ⑤ 的第二步） |
| `:144` | `goname[pid] = yaml_name(...)`（原来是裸 `.strip()`） |
| `:151-163` | 删掉 `m_RootOrder` 读取；Transform 解析时改读父的 `m_Children` 列表 → 建 `sib_of`（子 pid → 在父列表里的下标）→ 回填 `tr` 的第三元 |

未改动：`path_of()` 的拼装逻辑、产物格式、`WANT` 类表、字段抽取（`f0..f4` / `presets` / `sounds`）。

## ③ 重跑生成器

- 跑法：`python -I -X utf8 d:/4/Unity/工具/gen_effect_extras.py`（先 `--dry` 看过统计：**978 件 / 导出集缺 15 件 / 带额外组件的 88 件 / 挂载 164 处**，与改前一致）。
- 产物：`Unity/MyGame/Assets/Resources/WarpforgeVFX/effect_extras.json`（35,483 B → **35,696 B**）。
  ⚠️ 该目录被 `.gitignore:40` 忽略 ⇒ **`git diff --numstat` 对它不显示**（这是预期的，不是没写）。
- 逐条对拍（改前备份 vs 改后）：`effects 88 → 88` · `records 164 → 164` ·
  **`node` 以外字段变化 = 0**（只动了路径字符串）· **`node` 变化 94 条**。
- 幂等：`--check` ⇒ 「盘上文件与本次生成一致 ✅」。
- 备份：`d:/4/_tmp_view/x3_bak/effect_extras.before.json`。

## ④ `#1` 后缀现在对不对（证据）

生成物里（`effect_extras.json:1468`）：
```
"node": "Pray_Idle/Pray VFX mesh#1",
```

**离线全量复核**（不跑 Unity）：写了 `d:/4/_tmp_view/x3_bak/verify_extras_paths.py`，
口径照抄 `WFEffectModule.ResolvePath`（段 `名字#N` → `GetChild(N)` → 名字逐字比；只有第一段按 `Trim` 比；
prefab 侧名字按 YAML 规则**脱引号**，即 Unity 真名），对我们 978 件 prefab 逐条解：

| 数据 | 结果 |
|---|---|
| 改前 `effect_extras.before.json` | **合计 164 处 / 解析失败 94** |
| 改后 `effect_extras.json` | **合计 164 处 / 解析失败 0** |

**非空转证明**（防「解析器恒真」）：同一解析器上
`Pray_Idle/Pray VFX mesh#1` → `(True)`；故意写成 `#0` → `(False, '第 1 段对不上（第 0 个叫 Sparks）')`。

## ⑤ 同类的「第二步」（顺手扫 + 修了 1 个）

1. **全仓 `m_RootOrder` 只剩这一处读者** —— `grep -rn "m_RootOrder"` 在 `*.py/*.cs/*.sh/*.js` 里
   只命中 `Unity/MyGame/Library/PackageCache/com.unity.collab-proxy@…/IgnoredProperties.cs:51`（Unity 自己的包，别动）。
   另一个同类生成器 `Unity/工具/dump_animfx.py:165-192`（`animfx_modules.json` 那条链）**早就是按
   `m_Children` 取下标**，与本次改法一致 ⇒ 两份数据现在同口径（此前 extras 与 animfx 对「同名且非首个子件」会给出不同路径）。
2. **【新查出并已顺手修】YAML 引号没脱**：Unity 给**带首尾空格**的名字加单引号（导出集 1314 件 prefab 里 **107 处**，
   例：`m_Name: 'Impact '`，真名是 `Impact `）。旧代码 `.strip()` 脱不掉引号 ⇒ 路径段变成 `'Impact '`
   ⇒ `ResolvePath` 逐字比必然失败。受影响记录 **2 条**：
   - `Blast_Big_Ork_Missiles/Bullet FX Ork/'Impact '#4/Blast_Big_Ork` → 现 `…/Impact #4/…`（解析 ✅）
   - `Sau_ParticleWhip/Beam#6/'Necron Gauss '` → 现 `…/Necron Gauss `（**尾随空格是名字的一部分，必须留**，解析 ✅）
   判定时排除了 3 条**名字本身含撇号**的记录（`Environmental Condition Emperor's Children …`）—— 它们没被误改。
3. **顺带发现的账（未做，超出本件白名单，交调度台）**：`AnimFXCheck` 对 `effect_extras.json` 的节点**没有全量 sweep**
   （`AnimFXCheck.cs:178-211` 只扫 AnimFX 模块的 `__node`/`@node:`）⇒ 本次这种「94 处坏了只报 1 处」的盲区会复发。
   建议在 `AnimFXCheck` 里补一段「把 88 件 × 164 处 extras 节点全解一遍、失败计 0」的断言（同 ③b 的写法）。
   另：`effect_extras.json` 属 `.gitignore` 生成物 ⇒ **换机器/清库必须重跑本脚本**，否则拿不到这次修复。

## 验收（本件范围内）

- `TMPDIR=/tmp/wf_x3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ⛔ 未跑 Unity（`AnimFXCheck.Run` 由调度台统一跑）。
- `git status --porcelain` 只多出 `M Unity/工具/gen_effect_extras.py`（29+/3-，LF 未被翻：`crlf 0 / lf 296 / bareCR 0`）。
