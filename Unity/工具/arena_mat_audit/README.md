# 战场粒子材质「渲染状态」普查 —— 工具与做法（2026-09-26）

> **为什么有这一份**：2026-09-26 派了三路子代理把 13 场的**粒子材质**逐颗摊开对了一遍，
> 查出四条真缺陷 + 一条共同病根（见 `项目任务.md` §三 第 3 条 **第 11 项**，那里是**结论正本**）。
> 脚本原地在 `_tmp_view/mataudit/` 与 `d:/tmp/`（**都会被清掉**）⇒ 收进这里。
> ⚠️ **这些脚本只读、不碰工程**（不跑 Unity），可以反复跑。

## 两套是**两个子代理各写的一套**，别混着用

| 套 | 谁写的思路 | 干什么 |
|---|---|---|
| `套1_pathID索引/` | 先建全局索引再解 | `build_mat_index.py` 把 `assets_full/**/Material_<pathID>.json` 建索引（915 份）→ `resolve.py` 按 pathID 反查 → `read_ours.py` 读我们的 `arenas/<场>/Materials/PS_*.mat` → `diff.py` 出差异表（产物 `diff.txt`） |
| `套2_逐场对账/` | 直接开 bundle 读 | `orig2.py` / `ourmat_probe.py` / `psmat_probe.py` 各自抽原版与我方 → `join*.py` 拼 → `final_report.py` 出表 |

两套**结论互相印证**（都指出了 `_QueueOffset` 丢失与 shader 被换成 URP 那两条）。

## 判据（照抄，别自己发明）

- 🔴 **解析引用一律按 `pathID`，别信 `m_FileID`**（那批包里不可信，踩过）。
- 🔴 **pathID 只在同一个文件内唯一** —— 拿一个 PPtr 的 pathID 去别的包扫会**静默拿错**同名号。
  外链要看 `m_FileID` 指向哪个包；解不出来时再建全库索引。
- 原版 shader 名要读 **`m_ParsedForm.m_Name`**（`Shader.m_Name` 是空的）。
- 原版材质的「渲染状态」在 `m_SavedProperties` 的 `m_Floats` / `m_Colors` / `m_TexEnvs`，
  加上 `m_ValidKeywords` / `m_InvalidKeywords` / `m_CustomRenderQueue`。
- 我们这边落盘规则 = `PS_` + `Sanitize(原版材质名)`（`WarpforgeArena1/Editor/ArenaBuilder.cs:3475-3564`）。

## 自检（跑完怎么知道没跑偏）

- 原版材质**一份都不该解不出**（**首轮 8 场**实测 0 个未解出 —— ⚠️ 后 5 场这条自检**没单独记**，
  别把它读成「13 场都验过」，见本节末「归档缺口」）；解不出就先怀疑 pathID/包的范围。
- 我们磁盘上的 `.mat` 数 = 「清单里 active 且有贴图且 `renderMode≠5`」的粒子数 ——
  差一个就要说得出为什么（已知 4 个是按设计跳过的，见 `项目任务.md` §三 第 3 条 第 11 项末）。

## 还没做完

✅ **13 场已全部盘完**。
⚠️ **2026-09-27 更正**：这一节原来写「`genestealers` · `emperorschildren` 两场还没盘（其余 11 场已盘完）」——
那是**第三波跑完之前**记的。判据 = `项目任务.md` §三 第 3 条 第 11 项：三波分组 **4+5+4 覆盖全 13 场**
（含 EC 与 genestealers），且 ⑩ 里已引用 **EC 的 `bubble_light`** 与 **genestealers 的 `_Color`** 两条发现。
新开一场照 `套2_逐场对账/orig2.py` 改个场名跑即可。

⚠️ **同一天查出的另一笔账（归档缺口，不是结论缺口）**：`套3_五场逐材质/` 是**空目录** ——
第三波那套脚本**没被收进来**（前两套在）。结论不受影响（正本在 `项目任务.md` §三 第 3 条 第 11 项），
但**要重跑第三波那条路得把脚本重写**。
