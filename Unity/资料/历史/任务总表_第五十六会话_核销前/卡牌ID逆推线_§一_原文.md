# 卡牌ID逆推线 · §一 原文（2026-10-19 第五十六会话核销前）

## 一、未完成

| # | 任务（做完之后有什么不同） | 状态 | 关键判据 / 索引 |
|---|---|---|---|
| **K12** | **正本 §七·2「没查清」清零** —— `activeAbilityTextReferenceId` 语义 · `customIntroReferenceId` 键族 · `cardShortId` 用途 · `CardEffect` 枚举 · `CardTraitCollection` / 128 个 `CardTrait` 资产 · 37 个未用 trigger 的落点。每条查清后就地改进正本 | `◐`（**2026-10-18 第一轮：12/12 条都处理、0 条原样不动** —— ✅ **收口 4 条**（`CardEffect` 实为 class · `activeAbilityTextReferenceId` · `customIntroReferenceId` 键族 `Chat_Messages/<ref>-Chat<N>` · `alternativeAbility` 语义）· ⚠️ **主体查清、剩一小块 6 条**（ResolveAction 92 值里 **10 个完全无落点**、PlayAbility 83/83 有分支但参数块缺、trait SO 值不在本地、12 个 trigger 无发射点、AI 公式、`CountShowTraits`）· ⚠️ **只推进 2 条**（`CardTraitCollection` 加载路径已定位但值不在本地；`cardShortId` 只查清分布、**用途 0 证据**，需运行期探针）· 🆕 **2026-10-18（第五十一会话）第二轮：8 条全部有最终状态、0 条原样不动** —— ✅ **收口 2**（12 个未用 trigger 12/12 定位完；`ShowTraits` = **仅展示**，规则侧 `HasCurrentTrait(590)` 0 命中，正面坐实）· ⚠️ **主体查清 3**（`ResolveAction`：`replaceHeroPower` 有生产者但**死码**、其余值无生产者已坐实；`PlayAbility` 公共乘算链已抽出；AI 评分归 AI 线）· ❌ **确认查不到 3**（`CardTraitCollection` / `CardTrait` **值全不在本地**——搜过 `D:/2` 全盘 78 个 CardTrait 文件、`assets_full` 24.7 万文件、`assetripper_out`、关服前缓存；`cardShortId` 静态 0 证据）—— 这三条**只能走运行期探针**，偏移与钩子方案已写进正本；⛔ 没猜用途。报告 → `资料/普查产出_1018/R_K12_第二轮_报告.md` | 正本 §七·2（逐条带「原：…」对照）· 报告 `资料/普查产出_1018/R_K11_K12_报告.md` §3 |

---

### 📌 常驻口径（不是待办）

- **K05 新卡新增路径**：**加数据行即可**；只有出现全新的 `AbilityEffect` / `AbilityTrigger` / 动词/关键词时才需要改代码。
  现行入口 = **`资料/卡牌制作_正本.md` §六 checklist + §3·7 扩展路径**。
- **结构化直读是升级、不是重写**（用户 2026-10-17 拍板）：拿到 SO 后把 `EffectText` 的「解析卡面文本」
  换成「直读 `cardAbilities`」，下游 `EffectResolver/RuleCore/BattleEvent/表现层` 一行不用改
  （它们吃的是 `EffectOp`/事件）。⚠️ **落地排期不在本线** —— 属引擎线，本线只保留这条口径。
- **卡 id 真值只认 SO 直读**（本线所有结论的共同前提）。

### 已完成（留痕，别点进去）

| 日期 | 件 | 做了啥 | 详情 |
|---|---|---|---|
| 2026-10-18 | **K08** 卡牌制作正本 | 建 `资料/卡牌制作_正本.md`：id 写法 / SO 全字段表 / trigger·effect·Add* 对照 / 动画特效链 / 本地化键规范 / 新卡 checklist / 未查清清单 | `资料/卡牌制作_正本.md` · 建档过程 `资料/普查产出_1018/R_卡牌制作正本_建法.md` |
| 2026-10-18 | **K01** 当前 build 的 allcards 包 | 找到并解析 **1,351 张 `RawCardScript`**；118/118 缺卡 id 有解 | `资料/普查产出_1018/allcards_1350/cards.tsv` · `卡牌ID合并_第四十九会话.md` |
| 2026-10-18 | **K03** 旧表偏移候选 | 114 改 id · 4 不变 · 0 未解；`card_ids.json` 整表换真值 | `missing118_resolved.tsv` · `pool_true_ids.tsv` |
| 2026-10-18 | **K06** 池内 114 张旧 id 重编号 | 池内 id 全换真 id；自造 0；下游表/语音/立绘一次迁移 | `资料/普查产出_1018/K06_池id迁移_报告.md` · `工具/migrate_pool_ids_k06.py` |
| 2026-10-18 | **K07** 当前 build 本地化并入 | en+zh **10,854** 条 · 13lang **10,862** 条；生成器加 `--input`/`--carry` | `资料/普查产出_1018/K07_本地化并入_报告.md` |
| 2026-10-18 | **K02** SO 导出/复核器 | `工具/export_allcards.py`：1,351 张 / 3,488 GUID / 1.7 s；`--check` 与 `cards_raw.json` **5 项全一致** | `资料/普查产出_1018/R_K02_K10_工具_报告.md` |
| 2026-10-18 | **K04** 效果参数手册 | `资料/卡牌制作_参数手册.md`（462 行）：8 参数块逐字段 + 12 族 40+ 张真卡 + 照填速查 | 报告 `R_K04_K09_报告.md` |
| 2026-10-18 | **K09** 三张引擎对照表 | `资料/卡牌制作_三张表.md`（357 行）：**97 / 83 / 75 逐行 0 mismatch**；trigger 用 60 · effect 用 57 · Add* 直调 49 | 报告 `R_K04_K09_报告.md` |
| 2026-10-18 | **K10** 新卡校验器 | `工具/check_new_card.py` + 11 个夹具（全按预期）；枚举运行时读桩（97/83） | 报告 `R_K02_K10_工具_报告.md` |
| 2026-10-18 | **K11** 样板卡库 | `资料/卡牌制作_样板库.md`（384 行）：13/13 阵营 · 3/3 现有卡型 · 7/7 效果族；41 条动画三档全过 | 报告 `R_K11_K12_报告.md` |

---

