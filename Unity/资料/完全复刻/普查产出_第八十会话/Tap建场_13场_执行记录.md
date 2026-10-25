# Tap 族建场 · 13 场执行记录（2026-10-25 第八十会话）

> 铁律 15：**一场一场跑，⛔ 不用 `--all`、不写 for 循环**。命令逐场：
> `unset ELECTRON_RUN_AS_NODE && WF_ARENA=<场> "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.BuildPrefabFromCLI -logFile -`
> 日志在 `Unity/_tmp_view/builds/*.log`。

## 一、结果：**13/13 全部 rc=0 · Tap 自检相等 · 「锚点找不到」告警 0 条**

| # | 场 | 清单 | 建碰撞体 | 空宿主 | 锚点（接上/失败） | 自建 | 自检 | 与 Q2 预期 |
|---|---|---:|---:|---:|---|---:|---|---|
| 0 | **battlearena1**（校准） | 20 | 20 | 5 | **0 / 0** | 0 | ✅ 20/20 | ✅ **逐字相符** |
| 1 | battlearena2 | 27 | 27 | 21 | **1 / 0** | 1 | ✅ 27/27 | ✅ 相符（自建 1 = 预期 1） |
| 2 | battlearena3 | 14 | 13 | 3 | **2 / 0** | **1** | ✅ 13/13 | ⚠️ **自建 1，预期 0** |
| 3 | battlearenaaeldari | 13 | 13 | 4 | 0 / 0 | 0 | ✅ 13/13 | ✅ 相符 |
| 4 | battlearenaastramilitarum | 27 | 27 | 1 | 0 / 0 | 0 | ✅ 27/27 | ✅ 相符 |
| 5 | battlearenablacklegion | 15 | 15 | 5 | **7 / 0** | **6** | ✅ 15/15 | ⚠️ **自建 6，预期 4** |
| 6 | battlearenadarkangels | 6 | 6 | 1 | 0 / 0 | 0 | ✅ 6/6 | ✅ 相符 |
| 7 | battlearenaemperorschildren | 1 | 1 | 1 | 0 / 0 | 0 | ✅ 1/1 | ✅ 相符（原版整场无反馈） |
| 8 | battlearenagenestealers | 9 | 9 | 1 | 0 / 0 | 0 | ✅ 9/9 | ✅ 相符 |
| 9 | battlearenaleviathan | 11 | 11 | 1 | 0 / 0 | 0 | ✅ 11/11 | ✅ 相符 |
| 10 | battlearenasororitas | 11 | 11 | 1 | **2 / 0** | **2** | ✅ 11/11 | ⚠️ **自建 2，预期 0** |
| 11 | battlearenaspacewolves | 1 | 1 | 1 | 0 / 0 | 0 | ✅ 1/1 | ✅ 相符（原版整场无反馈） |
| 12 | battlearenatauviorla | 11 | 11 | 2 | 0 / 0 | 0 | ✅ 11/11 | ✅ 相符 |

**产物**：`Assets/Resources/ArenaPrefabs/*.prefab`（13 份，23:20–23:24 全部重建）。
⚠️ **该目录在 `.gitignore:131`** ⇒ `git status` 里看不到 —— **不是没写盘**。

## 二、⚠️ 唯一偏差：**实际自建 10 条，Q2 预期 5 条**

| 场 | Q2 预期自建 | 实际自建 | 差 |
|---|---:|---:|---:|
| battlearena2 | 1 | 1 | 0 |
| **battlearena3** | **0** | **1** | **+1** |
| **battlearenablacklegion** | **4** | **6** | **+2** |
| **battlearenasororitas** | **0** | **2** | **+2** |
| 合计 | **5** | **10** | **+5** |

**含义**：`FindBuiltByGo` 的「按名/按路径找已建节点」**比 Q2 估计的弱得多** —— 有 5 条锚点其实在场景里，却**没能按名匹配上**，于是走了「按清单世界 TRS 自建空节点」的兜底。

**影响判断（功能等价，但有前提）**：
- Q2 已论证：清单里 `anchor.go/pos/rot/scale` 全是**世界口径**、`root` 是 identity ⇒ 自建节点的**世界位姿与原版逐字段一致**；而原版 `PlayParticle` 只拿它 `SetParent` + 本地零位 ⇒ **等价**。
- ⚠️ **前提**：该锚点**不被动画/父级移动**。若某个锚点原本挂在会动的父件下，自建节点就跟不上。
  ⇒ **下一批要查**：这 5 条（arena3 ×1、blacklegion ×2、sororitas ×2）**在原版是不是静态锚点**；不是的话自建不等价。

## 三、下一批要做的两件事

1. **查 `FindBuiltByGo` 为什么漏匹配**（5 条）：是**节点名差异**（空格/下划线/序号，同 `Barrel Left1` → `Barrel_Left1_0` 那一族）还是**路径差异**。修好后重跑对应 3 场，自建数应回落到预期。
2. **核 5 条锚点是否静态**（见上）。

## 四、已知且**不该动**的两场

`battlearenaemperorschildren` / `battlearenaspacewolves`：原版**整场无反馈**（各自只有 1 条 `CardLowBoardLimit`，layer 11 不在射线掩码 `0x40000001` 内）⇒ 已照原版保留，⛔ 别去「修」。
