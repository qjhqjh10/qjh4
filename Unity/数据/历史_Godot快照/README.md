# 历史_Godot快照 —— Godot 时代的冻结快照（2026-10-19 归档）

> **账**：`A2112`（数据侧 · Godot 时代的账 · 总闸）· **施工图**：`资料/普查产出_1019/W_工具线_T01施工图_1019b.md` §三
> **归档日期**：2026-10-19。**只移动、⛔ 未删任何文件**（保留原相对路径）。
> **验收**：`python -I -X utf8 d:/4/Unity/数据/历史_Godot快照/verify_archive.py`

## 这里是什么

从 `Unity/数据/` 的**现役数据区**移出来的两族 **Godot 时代**派生件（我们上一版 Godot 复刻的中间格式）：

| 归档路径 | 原路径（已不存在） | 文件数 | 来源 |
|---|---|---|---|
| `界面布局/ui_layout/` | `Unity/数据/界面布局/ui_layout/` | 3 | `d:/warpforge/data/ui_layout`（`工具/sync_from_d2.py:98` 首次播种） |
| `粒子/particles/` | `Unity/数据/粒子/particles/` | 201 | `d:/warpforge/data/particles`（同上 `:94-96`） |
| `粒子/particles3d/` | `Unity/数据/粒子/particles3d/` | 863 | `d:/warpforge/data/particles3d` |
| `粒子/arena_particles/` | `Unity/数据/粒子/arena_particles/` | 54 | `d:/warpforge/data/arena_particles` |

## 为什么归档（不是删）

- **代码侧零引用**：`Unity/MyGame/Assets/**` 的 `.cs` / `.unity` 对这两族 **0 命中**；`Unity/数据/` 在 Unity 工程根 `Unity/MyGame/` **之外** ⇒ 进不了构建、`Resources.Load` 够不着。
- **有同源权威替代**：权威源 = 解包资源 `d:/2/新解包资源/assets_full/`（粒子原始 JSON 在 `d:/2/解包整理/08_预制体特效/战斗预制体/`，1.6 G / 115,690 件）；Unity 侧用 `工具/scripts快照/gen_unity_arena_manifest.py` 的内建解析器直接读 bundle。
- **`d:/warpforge` 是 Godot 时代的旧仓，⛔ 别拿它当判据**（用户 2026-10-15：「我们直接使用解包资料，不要被 Godot 时代的账这些干扰」）。
- 粒子产物是 **Godot 中间格式**（带 `res://assets/particles/…` 与 `godot_pos`），Unity 一个字节都用不了。

## 使用限制

- ⛔ **不要把它们当原版判据**；需要原版参数请直接读 `d:/2/新解包资源/assets_full/` 或 `d:/2/解包整理/`。
- ⛔ **不要再播种回 `Unity/数据/粒子/` / `Unity/数据/界面布局/ui_layout/`** —— `工具/sync_from_d2.py` 的 3+1 个落点已改指本目录（保留原相对路径）。
- ⚠️ 这些文件**仍被 git 跟踪**（1,121 个 / ≈19 MB）；移动后 `git status` 会显示「删除 + 未跟踪」，`git add -A` 后 git 会按内容识别为 **R（重命名）**（相似度 100%）。

## 参考

- 判据/清理背景：`资料/普查产出_1015战斗/D5_Godot时代的账_清点与清理.md` §A · `资料/普查产出_1019/W_工具线_T01施工图_1019b.md` §三
- 任务书：`资料/任务线/工具文档线/任务/T01_Godot时代清理.md`（`A2112`）
