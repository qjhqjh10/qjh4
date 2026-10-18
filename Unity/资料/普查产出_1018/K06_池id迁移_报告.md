# K06 · 池内 114 张旧 id 重编号 —— 收口报告

> 日期：2026-10-18（第五十会话）· 线：**卡牌ID逆推线 `K06`**（大迁移）
> ⚠️ 本轮按现行口径：**只做不验** —— 没跑 `*.Run`、没进游戏；只跑了**秒级 typecheck** 与**对账类**工具。

---

## 一、结论（一句话 + 数字）

**池内 `cards_engine.json` 的 114 张旧 id 已全部换成原版真 id，与 `card_ids.json.poolTrueIds` 逐条一致（0 处不符）；
全池 1,127 张的 id 现在 100% 是真 id（自造 id 0 张）。**

| 项 | 之前 | 现在 |
|---|---|---|
| 旧 id 的卡 | **114** | **0** |
| 自造 id 的卡 | 14（生成器口径）/ **23**（旧池里「自造形态」的总数） | **0** |
| 真值快照里查不到的池内卡 | 118（旧快照）/ 1（新快照） | **0** |
| 卡池 `card_ids.json` 键命中 | 1,089 / 1,127 | **1,127 / 1,127** |
| 逐卡 VFX 可达覆盖（P3 §2·1） | 983 → 1,051 / 1,127 | **1,071 / 1,127（95.0%）** |

---

## 二、根因（为什么之前有 114 张旧 id）

生成 id 的两条上游都指向 **2022/2025-07-30 的旧快照**（`cards_1195_2022时代.json`，1,195 张）：

- `工具/gen_card_id_truth_index.py` 的 `TRUTH`
- `工具/gen_cards_engine.py` 的 `TRUTH_CARDS_SRC`

旧快照里没有 SW / UM / DA 的那批新卡 ⇒ `pick_truth_id` 落空、退回旧 `card_ids.json` 或**自造 id**。
2026-10-18 早上找到当前 build 的 `allcards`（1,351 张）之后，这两处**没有跟着换源** ⇒ id 仍停在旧编号。

**换源后**：`card_id_truth_index.json` 重生成（1,351 行）→ `cards_engine.json` 重生成 ⇒ **1127/1127 走真值索引、自造 0**。
实测映射 **114 条**，与 `poolTrueIds` **逐条一致**。

---

## 三、改动清单

### 3·1 生成器换源（4 个）

| 文件 | 改动 |
|---|---|
| `工具/gen_card_id_truth_index.py` | `TRUTH` → `资料/普查产出_1018/allcards_1350/cards_raw.json`；展平 `{cards:[{full_raw}]}` |
| `工具/gen_cards_engine.py` | `TRUTH_CARDS_SRC` 同上；`load_truth_cards()` 展平；**新增 `RARITY_TRUTH_ACCEPTED`**（1 条已核过的快照差异，见 §四） |
| `工具/gen_card_initiative.py` | `TRUTH` 同上；`load_truth()` 展平 |
| `工具/gen_card_inventory_options.py` | `TRUTH` 同上；`build()` 展平；`source` 文案同步 |

### 3·2 一次性迁移（新工具）

`工具/migrate_pool_ids_k06.py` —— **只跑一次**（脚本自带重跑护栏：当前卡池 == 期望的新 id 集合时拒绝执行）：

- **代码/工具**：29 个文件 / **247 处**（`.cs` + 活工具 `.py`；`_` 开头的历史一次性脚本**不动**）
- **语音表**：`Resources/voice_lines.json` **62 条** id
- **立绘**：`Art/cards/art_<id>.png`（+ `.meta`）**228 个文件**（两阶段改名，避免 73 条链互相覆盖）
- 判据：**一次性正则替换**（每个 token 只按原值换一次）—— `poolTrueIds` 里 **73 条是链**（`SW32→SW24`、`SW24→SW19`），顺序替换会改成第三张卡

### 3·3 重生成的产物

`cards_engine.json` · `card_id_truth_index.json` · `card_vfx_by_card.json`（全量+运行时）· `card_icon_plan.json`（两份）
· `card_anim_order.json` · `ability_anim_durations.json`（两份）· `card_initiative.json` · `card_inventory_options.json`
· `prebuilt_decks.json` / `tutorial_decks.json` · `related_cards.json`（两份）· `talent_cards.json` · `lore_zh+en.json`（两份）

### 3·4 探针基线

`工具/ruleprobe/基线/out_baseline.txt` 与 `out_play_baseline.txt` 重生成；
`Program.cs` 三条指纹重钉（`PinnedPoolMd5` / `PinnedBaselineMd5` / `PinnedParseMd5`）。
**判据**：用 HEAD 卡池与当前卡池各扫一遍全池、**按卡名对齐后 0 处内容差异**（1,118 个名字）⇒ 只有 id 列变了。
重生成后 `check`：**解析差异 0 行 · 改名 0 · 新增 0 · 消失 0 · 哨兵 0 条**（rc=0）。

### 3·5 `card_ids.json` 加标志

新增 **`"poolIdsAreTrue": true`**（+ `poolIdsAreTrue_note`）。`poolTrueIds` **保留为历史迁移表**（旧池 id → 真 id）。
🔴 两个消费方（`gen_prebuilt_decks.py` / `check_prebuilt_decks.py`）已改成：**为 true 时直接按 id 查、⛔ 不再反转 `poolTrueIds`**。

---

## 四、必须记的三条（⛔ 会静默骗人）

1. 🔴 **`poolTrueIds` 现在是【历史表】，不是翻译表。**
   迁移后把 `{v:k}` 一反转，真 id 会被映回旧 id ⇒ **认成另一张卡**（实测把 `SW4 Blood Claw` 认成
   `Grey Hunter Pack Leader`、SW 一族整体错位，`suspect` 14 → **65**）。判据 = `card_ids.json.poolIdsAreTrue`。
2. 🔴 **稀有度真值换了 1 张**：`e59ac9e46f4ea804784e954c3b820e77 / Skyborne Deployment`
   旧快照 `rare` → 当前 build **`common`**（两个快照 `uniqueId`/`m_Name` 逐字相同 ⇒ 游戏侧真的改了）。
   已加 `RARITY_TRUTH_ACCEPTED` 显式放行；**守卫没有被拔掉**，其余变化照旧进 `_bad`。
3. 🔴 **快照换源带来的字段变化（都是「从缺值变成有值」）**：
   `targeted` 122 · `attackDamageType` 118 · `cantBeRandomlyCreated` 118 · `cardShortId` 118 ·
   `expansionPack` 118 · `useOverDraw` 118 · `cardTraits` 93 · `keywords` 9 · `fullHeroSpriteReference` 6 ·
   `itemDescriptionKey` 3 · `cost` 2（`Roboute Guilliman` / `Valius Paxor`：`0 → 2`，督军 cost 照真值那条规则）·
   `rarity` 1。另有 **`nameZh` 84 / `descZh` 85** 变化 —— 那是 `K07` 新官方表经 `gen_cards_engine` 流下来的（例：`Fire prism` 6→8）。

---

## 五、存档

实测本机现有存档 **0 处旧 id**：
`AppData/LocalLow/Unity Technologies/**/WarpforgeDecks.json`（2 份）与 `WarpforgeDaily/daily.json` 全扫，`old∩cur` 的 id 一个都没有。
⇒ **不需要迁移**。若将来出现带旧 id 的存档，按 `card_ids.json.poolTrueIds` 一次性改写即可（⛔ 别在运行时猜）。

---

## 六、仍开 / 没做

- ⛔ **没跑任何 `*.Run` 自检、没进游戏**（现行口径）；`typecheck` = **运行时 0 / 编辑器 0**。
- ⚠️ `工具/ruleprobe/scan.txt` 是**未跟踪的旧 dump**（含旧 id），本趟没重写；它不被任何脚本消费。
- ⚠️ **立绘只按 id 改名**（没重新渲染）：新 id 的图 = 旧 id 那张图的字节。若发现某张图与卡不匹配，
  跑 `工具/import_original_art.py` 重出（它按 id + 真值链确定性生成，可自愈）。
- 🔴 **`Resources/Art/` 被 `.gitignore` 忽略** ⇒ 这 228 个改名**不进 git**、只在本机生效；
  换机器后必须重跑 `工具/import_original_art.py`，否则 114 张卡没有立绘。

---

## 七、复现

```bash
# ① 真值索引 + 卡池（换源后）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_card_id_truth_index.py
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_cards_engine.py

# ② 一次性 id 迁移（⛔ 只跑一次；脚本会拒绝重跑）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/migrate_pool_ids_k06.py          # dry
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/migrate_pool_ids_k06.py --apply

# ③ 下游重生成（顺序无关，但 vfx 先于 ability_anim_durations）
gen_card_vfx_by_card.py · gen_icon_plan.py --write · gen_card_anim_order.py ·
gen_ability_anim_durations.py · gen_card_inventory_options.py · gen_card_initiative.py ·
gen_prebuilt_decks.py · gen_related_cards.py · gen_talent_cards.py · gen_lore.py

# ④ 验收读数
check_prebuilt_decks.py        → missing_unknown=0 · suspect=14 · full=228/236
ruleprobe.sh check             → 解析差异 0 · 哨兵 0（rc=0）
P3 §2·1 覆盖率脚本              → reachable 1071 / 1127
```

---

## 八、出处

| 内容 | 位置 |
|---|---|
| 当前 build allcards | `资料/普查产出_1018/allcards_1350/cards_raw.json` · `报告.md` |
| 迁移映射 | `数据/游戏数据/card_ids.json` 的 `poolTrueIds`（历史表） |
| 换源前的全部产物 | `d:/4/_tmp_view/backup_1018/k06/` |
| 探针判据 | `工具/ruleprobe/Program.cs` 的 `PinnedPoolMd5` 段 |
| 覆盖率口径 | `资料/普查产出_1017/诊断_第四十八会话/P3_动画特效覆盖率.md` §2·1 |
