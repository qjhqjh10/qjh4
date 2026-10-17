# 卡牌 ID 合并 · 2026-10-18（第四十九会话）

> 触发：用户找到 `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/`，里面有一个 **UnityFS（6000.2.6f2）** 缓存 bundle，
> 路径内层 = `ea8bca4f5c6909d3124fd4975dc96173`（**正是当前 build catalog 期望的 `allcards` 哈希**）。
> 只读解析代理的完整报告 → `资料/普查产出_1018/allcards_1350/报告.md`；逐卡表 → 同目录 `cards.tsv` / `cards_raw.json`。

---

## 一、结论

| 项 | 结果 |
|---|---|
| 是不是 allcards | ✅ **是**：`CAB-fb8f01449289635703d83d52c3f18245` · **1,351 个 `RawCardScript`** · `uniqueId/cardName/cardArmy/五格` 全部非空、**0 重复** |
| 118 张缺卡 | ✅ **118/118 全部有解**：114 张需改 id、4 张 id 不变（`DA75`/`SW1`/`SW2`/`SW3`）· 0 未解 |
| 全池 1,127 张 | id 正确 **1,013** · **114 张仍是旧编号**（SW/UM/DA 整族 + 若干 hex）· 0 未解 |
| 附带 | 第三个 bundle = **当前 build 的 localization**，已用既有密钥解码（10,857 条 × 15 语言，含 zh-CN）→ `localization_2026_decoded.json` |

---

## 二、这次改了什么（**池 id 未重编号**）

1. **`Unity/数据/游戏数据/card_ids.json` 整表换真值**
   - `mapping`：**1,351 条** `真 id → 卡名`（包内全部卡；我们池里的 1,127 张用**我们池的卡名**，包内多出的 224 张用当前 build 官方英文名/SO 名）。
   - 新增 **`poolTrueIds`**：`我们的池 id → 真 id`（1,127 条）—— 这是「池 id 暂时不重编号」时的翻译口。
   - 旧表备份 → `d:/4/_tmp_view/backup_1018/card_ids.old.json`。
2. **`工具/gen_prebuilt_decks.py` / `工具/check_prebuilt_decks.py`**
   - `t0`（按原版卡号直查）**先经 `poolTrueIds` 反查成池 id**；不在表里的真 id **不直查**（否则会撞上旧的同号卡）。
   - 两个脚本同批同改（文件里本来就写着「改规则必须两边一起改」）。
3. **重生成预组 / 教程**
   - `Resources/prebuilt_decks.json` / `Resources/tutorial_decks.json`（备份在 `_tmp_view/backup_1018/`）。
   - 103 副练习池 **complete=103**；8 副内容变化，其中 `DarkAngelsDeck7` 14→30 张、`DA_SK_4` 4→12 张、SW 三副换成了正确卡。
4. **核对读数（`check_prebuilt_decks.py`）**

| 指标 | 旧 `card_ids.json` | 新 |
|---|---|---|
| `missing_unknown` | 9 | **0** |
| `suspect` | 32 | **14**（全部是 `(tutorial)` 变体走 t2，正常） |
| 能完整拼出 | 225/236 | **228/236** |
| `limit_bad` | 14 | **8** |

---

## 三、**没有做**的（都记了账）

- **池 id 重编号（114 张）** → `任务线/卡牌ID逆推线/任务总表.md` **K06**。会连带 `cards_engine` 及所有生成表/存档/代码里的硬编码 id，必须单独一批。
- **卡名不改**：`Land Rider` / `Morkai Eliminator` / `2nd Company Terminator` 是**卡图资产名**（用户 2026-10-16 拍板「卡名只认卡图资产名」）；PnP 显示名不同**不算错**。
- **包内 224 张多出的卡**（教程/附魔/同名副本）**没有逐张判定**是否对应池里已有卡；`cards_raw.json` 里全字段都在。
- **新 localization 还没并进工程**：`官方本地化_en+zh-CN.json` 仍是旧快照；`localization_2026_decoded.json` / `loc_card_names.tsv` 已落盘，可另开一批（会补 118 张卡的官方中文名/描述）。
- `AM66` / `GSC66` 的 SO 内部名与显示名差异（Tempestor Prime vs Hektor Thenmann；Shanus Daskovian vs Lhaska Szenari）—— **id 不受影响**，卡名要不要改需对卡图再核。

---

## 四、证据 / 备份

| 内容 | 位置 |
|---|---|
| 逐卡主表 | `资料/普查产出_1018/allcards_1350/cards.tsv`（1,351 行） |
| 全字段 | `…/cards_raw.json`（67 MB，116 字段） |
| 118 张逐条 | `…/missing118_resolved.tsv` |
| 全池对照 | `…/pool_true_ids.tsv` / `.json` |
| 新旧对账 | `…/reconcile_*.json`（同名不同 id 325 · 同 id 不同名 78 · 数值漂移 115） |
| 当前 build 本地化 | `…/localization_2026_decoded.json` · `loc_card_names.tsv` |
| 旧表 / 旧预组备份 | `d:/4/_tmp_view/backup_1018/` |
| archive.org（直连被墙，未取得） | `资料/普查产出_1018/远程_archive135.md` |
