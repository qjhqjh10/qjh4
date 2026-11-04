# B141-W42 · W131 · 仓库策略两件（`B125-ARENAGITIGNORE` + `B139-G2②`）

- 日期：2026-11-04
- 执行：写手代理 `B141-W42` · 会话第一百二十八
- 判据来源：`资料/完全复刻/判据/B141_W131_对账_工具.md`（R10 报告 #7 / #8）
- 性质：**纯文档 / 配置**（`.gitignore` 注释 + 本报告），零行代码改动 ⇒ **零条自检**

## 结论一句话

两处策略都**保留 ignore 不变**，只把「为什么故意不入库 · 怎么重建 · 新克隆预期红」**显式写进 `.gitignore` 注释**，
并汇总到本报告；`ArenaOriginalPrefabs` 另外**显式点名**一条 ignore（不再是「被通配顺带挡掉」的隐形行为）。

## 一、当前命中的 `.gitignore`

| 项 | 现读命中 | 备注 |
|---|---|---|
| `.gitignore` 文件本身 | **仓库根 `d:/4/.gitignore`** | `find . -maxdepth 3 -name .gitignore` 只有这一份；`Unity/MyGame/` 下**没有**另一份 |
| `ArenaOriginalPrefabs` | 改前 `:26`（`/Unity/MyGame/Assets/WarpforgeVFX/*`）→ 改后 **`:52` 显式规则** | 见 §二 |
| `effect_extras.json` | 改前/改后都命中 **`:71`**（`/Unity/MyGame/Assets/Resources/WarpforgeVFX/`） | 见 §三 |

⚠️ 任务简报里写的「`Assets/RuleEngine/Resources/effect_extras.json`」是**错路径**；真身 = `Unity/MyGame/Assets/Resources/WarpforgeVFX/effect_extras.json`（`find` 现读，仅此一份）。

## 二、`B125-ARENAGITIGNORE` —— 保留 ignore，但要显式点名

### 事实（`ArenaOriginalPrefabs`，现读）

- `Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/`：**2,202 个文件 / 332 MB**；`git ls-files` = **0**（确实一个都没入库）。
- 改前它「被忽略」靠的是 `WarpforgeVFX/*` 通配；negation 块只放行 `Runtime/ Shaders/ Editor/ Animations/` 与对应 `.meta`，
  **`ArenaOriginalPrefabs` 没有被 negation 放行** ⇒ 一直隐式忽略，**看不出是有意还是漏了**。这正是本条要解决的问题。

### 改动（`.gitignore` 第 40–53 行，+15 行注释 + 2 行显式规则）

在 negation 块之后加：

- 一条**显式注释**：写明「故意不入库 · 为什么（体积 332 MB + 派生自 `d:/2`）· 怎么重建 · 新克隆为什么红」；
- **显式 ignore 规则**：`/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/` 与 `…/ArenaOriginalPrefabs.meta`。
  ⛔ **零行为变化**（本来就被 `WarpforgeVFX/*` 挡住）；⛔ **没有** `git add` 这 2,202 个文件。

### 重建命令（写进注释，可直接照抄；逐场一场一场跑，⛔ 无 `--all`）

```bash
python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena <场> --root Scenario \
    --out-root d:/4/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs
```

- `--arena` 取值形如 `battlearena1 … battlearenatauviorla`；`--out-root` 默认是 staging（`d:/4/_tmp_view/arena_prefabs_out`），
  **只有显式给 `Assets/.../ArenaOriginalPrefabs` 才写进工程**（工具 docstring 原话）。
- **依赖本机 `d:/2/tools/assetripper_out/ExportedProject`** ⇒ 新克隆重建不了。

### 「新克隆预期红」的具体命令与预期

```bash
python -I -X utf8 d:/4/Unity/工具/check_arena_dangling_refs.py
```

| 场景 | 预期 | 说明 |
|---|---|---|
| **新克隆（无 `ArenaOriginalPrefabs/`）** | **rc=2**，打印「🔴 目录不存在：…」 | 工具 `:104-106` 现读：根目录不存在直接 return 2 |
| **本机重建后** | **rc=1**：`引用对：ok 1536 · 全0 0 · 内建 21 · 悬空 38（19 个 guid）` | 2026-11-04 实测 |
| 本机 + `--expect …/_shared/arena_closure_guid_map.tsv --allow-status runtime-bundle` | **rc=1**（放行 18 个 guid 后**仍剩 1 个**：`8dce83de…`） | 实测；所以「全绿」不现实 |
| 对照：`工具/check_prefab_refs.py`（查「guid 全 0 且 fileID≠0」） | 另一类，**别混** | 本工具查「guid 非 0 但工程无 `.meta` 拥有」 |

⇒ **这是「缺 d:/2 派生资产」的预期状态，不是缺陷**；⛔ 别为了让它变绿去改工程代码或把 prefab 塞进库（注释里已写死）。

## 三、`B139-G2②` —— `effect_extras.json` 策略写死

### 事实（`effect_extras.json`，现读）

- 文件：`Unity/MyGame/Assets/Resources/WarpforgeVFX/effect_extras.json`（另有 `.meta`），被 `:71` 整目录规则忽略。
- 生成器：`Unity/工具/gen_effect_extras.py`，**显式动作三选一** `--dry` / `--check` / `--write`；
  **裸跑 = 打印用法 + rc=2、零写盘**（2026-11-04 `B141-W2` 修的就是「裸跑会直接写盘」）。
- 数据源：只读 `d:/2/tools/assetripper_out/ExportedProject`。

### 幂等验收（实测）

```bash
python -I -X utf8 d:/4/Unity/工具/gen_effect_extras.py --check
# 输出：特效 978 条 / 导出集缺 2 件 / 带额外组件的 89 件 / 挂载 165 处
#       --check：盘上文件与本次生成一致 ✅
# rc = 0
```

⇒ **`--check` rc=0 = 幂等基线成立**。这是本条唯一的验收口（不写盘、不需要 Unity）。

### 写死的策略（已进 `.gitignore` 注释 + 本报告）

1. **本机 + 有 `d:/2` 才能重建** —— 换机器 / 干净 clone 里该文件不存在；运行时 `WarpforgeVFX/Runtime/WFEffectExtras.cs:109-117` 会「空表 + 出声一次」（不静默，但特效缺额外组件）⇒ 必须本机重跑 `--write`。
2. **`--check` 为幂等验收** —— 一致 rc=0 / 不一致 rc=1（⛔ 不写盘）。
3. **⛔ 别用旧生成器覆盖** —— 2026-11-04 之前的版本裸跑就写盘，会把表写坏；重生成只认带 `--write` 的新版。
4. 重建命令（写进注释）：`python -I -X utf8 d:/4/Unity/工具/gen_effect_extras.py --write`

## 四、验证记录（本批实际跑过）

```text
$ git check-ignore -v Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs
.gitignore:52:/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/   Unity/.../ArenaOriginalPrefabs

$ git check-ignore -v Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/battlearena1/battlearena1__Scenario.prefab
.gitignore:52:/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/   Unity/.../battlearena1/battlearena1__Scenario.prefab

$ git check-ignore -v Unity/MyGame/Assets/Resources/WarpforgeVFX/effect_extras.json
.gitignore:71:/Unity/MyGame/Assets/Resources/WarpforgeVFX/   Unity/.../effect_extras.json
```

- 行尾：`.gitignore` **纯 LF**（CRLF 0 · LF 298 · 裸 CR 0）—— 改前后一致，没翻行尾。
- `git diff --numstat -- .gitignore` = **24 / 0**（只加不删，全在注释区 + 2 条显式规则）。
- `git status --porcelain` 里本代理的产物 = `M .gitignore` + 本报告（其余 `??` 是同期并行代理的文件，不属本批）。
- 自检：**零条**（纯文档 / 配置，按铁律 12 第 3 条）。

## 五、交接节（待会话收尾时并入 `交接_第一百二十八会话.md`）

> ⚠️ 写本报告时 `资料/完全复刻/交接_第一百二十八会话.md` **尚不存在** ⇒ 未新建（避免与收尾写手撞车）。
> 收尾时请把下面这段并进交接的「本波做了什么」：

- **`B125-ARENAGITIGNORE`（已做）**：`.gitignore:40-53`（注释 40-51、显式规则 52-53）加显式注释 + 两条显式 ignore 规则，
  `ArenaOriginalPrefabs`（2,202 文件 / 332 MB）**仍不入库**；重建 = `工具/extract_arena_prefab_yaml.py --out-root …`（依赖 d:/2）；
  新克隆 `check_arena_dangling_refs` 预期 rc=2（目录不存在），本机重建后 rc=1（38 悬空 / 19 guid，`--expect` 放行 18 后仍剩 1）。
- **`B139-G2②`（已做）**：`.gitignore:62-69` 写死 `effect_extras.json` 策略（本机 + d:/2 才能重建；`--check` 幂等验收 rc=0；⛔ 别用旧生成器覆盖）。
- 报告：`报告/B141_W131_RepoPolicy.md` · 零条自检 · 未跑 Unity · 未动 git（只读 `check-ignore` / `status`）。
