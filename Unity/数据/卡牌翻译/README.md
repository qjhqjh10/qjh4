# 卡牌中文化产物归档（2026-08-27 会话）

> 本目录=8-27 中文全覆盖轮的翻译产物与生成器。用途：卡牌 csv 翻译表可重跑/复查/扩充。

## 文件说明

| 文件 | 内容 |
|---|---|
| **`zh_cards.json`（唯一权威）** | **合并后的中文卡名/效果表**：键=英文卡名，值 `{n: 中文名, d: 中文效果}`，**1194 个键**，`conflicts` 字段里记着 12 处同名冲突（大多是 `Normal Conditions` 这类**关键词术语**被各分组译得略有出入；真正的卡名冲突只有 4 张已知重名卡）。**`工具/gen_cards_engine.py` 读的是它。** ⚠️ 2026-09-12 由下面那 7 个 `_tmp_zhcards_g*.json` 合并而成，**那 7 个已删**（同一份数据留两处 = 迟早不一致）。 |
| ~~`_tmp_zhcards_g1..g7.json`~~ | **已删（2026-09-12）**。原来是 7 组翻译产物：g1=UM+SW 214 / g2=DA+BL 180 / g3=EC+GSC 173 / g4=Goff 123 / g5=Sautekh+AM 170 / g6=SaimHann+Soro 179 / g7=Tau+Levi 173。内容全部并入 `zh_cards.json`。 |
| `zh_rulebook_terms.json` | 官方中文规则书提取的术语/卡名中英对照 246 对（权威术语参考：Stealth=潜行/Blast=爆裂/Flank=侧翼/Secret=隐秘 等；程序提取自 `Warpforge部队卡片/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md`） |
| `_tmp_gen_zh_g4.py` / `_tmp_zh_g2.py` / `_tmp_translate_g1.py` / `_tmp_gen_g3_zh.py` / `_tmp_zh_build_g7.py` / `_tmp_gen_zhcards_g6.py` | 各分组的翻译生成器（可重跑复核） |
| `_tmp_2dcard_tree.py` | 2DCard 分层树遍历+组件贴图解析脚本（battlearena1，可复用核查） |

## 已合入

- 全部卡名/效果/关键词 146/subtype 65 已合入 `d:/warpforge/data/i18n/zh_CN.csv`（~6900 键，键=英文原文全串，含卡名/desc 键）
- 显示层接线：`tr(卡名名)/tr(desc原文)` 全串键；2DCard 分层卡（sd_card.gd）文字层=同一 tr 通道

## 待办参考（翻译相关）

- 子代理报告 ~70 条卡名为自拟音译（如 Zoanthrope=兽人脑/Gorkanaut=高克机甲），官方译名复查清单见 git 历史/任务文件
- 战斗日志/战斗内 fx 文本=规则引擎生成（机制级 tr 轮，**事件文本禁止 tr 红线**）
