# 卡牌中文化产物归档（2026-08-27 会话）

> 本目录=8-27 中文全覆盖轮的翻译产物与生成器。用途：卡牌 csv 翻译表可重跑/复查/扩充。

## 文件说明

| 文件 | 内容 |
|---|---|
| **`zh_cards.json`（唯一权威）** | **合并后的中文卡名/效果表**：键=英文卡名，值 `{n: 中文名, d: 中文效果}`，**1194 个键**，`conflicts` 字段里记着 12 处同名冲突（大多是 `Normal Conditions` 这类**关键词术语**被各分组译得略有出入；真正的卡名冲突只有 4 张已知重名卡）。**`工具/gen_cards_engine.py` 读的是它。** ⚠️ 2026-09-12 由下面那 7 个 `_tmp_zhcards_g*.json` 合并而成，**那 7 个已删**（同一份数据留两处 = 迟早不一致）。 |
| ~~`_tmp_zhcards_g1..g7.json`~~ | **已删（2026-09-12）**。原来是 7 组翻译产物：g1=UM+SW 214 / g2=DA+BL 180 / g3=EC+GSC 173 / g4=Goff 123 / g5=Sautekh+AM 170 / g6=SaimHann+Soro 179 / g7=Tau+Levi 173。内容全部并入 `zh_cards.json`。 |
| `zh_rulebook_terms.json` | ⚠️ **2026-09-15 更正**：原来是「**官方**中文规则书」，实测**那份规则书自己写着「非官方粉丝保存项目」**（`Warpforge_Offline_Rulebook_1_5-3_中文翻译.md` 开头，翻译日期 2026-08-15）⇒ 它是**粉丝翻译**，不是官方术语表。**用它当术语参考仍然成立**（同一份译法一贯），但别再说「官方」。提取的术语/卡名中英对照 246 对（权威术语参考：Stealth=潜行/Blast=爆裂/Flank=侧翼/Secret=隐秘 等；程序提取自 `Warpforge部队卡片/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md`） |
| `_tmp_gen_zh_g4.py` / `_tmp_zh_g2.py` / `_tmp_translate_g1.py` / `_tmp_gen_g3_zh.py` / `_tmp_zh_build_g7.py` / `_tmp_gen_zhcards_g6.py` | 各分组的翻译生成器（可重跑复核） |
| `_tmp_2dcard_tree.py` | 2DCard 分层树遍历+组件贴图解析脚本（battlearena1，可复用核查） |

## 已合入

- 全部卡名/效果/关键词 146/subtype 65 已合入 `d:/warpforge/data/i18n/zh_CN.csv`（~6900 键，键=英文原文全串，含卡名/desc 键）
- 显示层接线：`tr(卡名名)/tr(desc原文)` 全串键；2DCard 分层卡（sd_card.gd）文字层=同一 tr 通道

## 待办参考（翻译相关）

- 子代理报告 ~70 条卡名为自拟音译（如 Zoanthrope=兽人脑/Gorkanaut=高克机甲），官方译名复查清单见 git 历史/任务文件
- 战斗日志/战斗内 fx 文本=规则引擎生成（机制级 tr 轮，**事件文本禁止 tr 红线**）

## 🔴 官方中文（游戏内）—— 本地没有，也拿不到（2026-09-15 查实）

原版游戏的官方本地化走 I2 Localization，**12 种语言（含 Chinese）都装在同一个远程 bundle**
`localization_assets_all.bundle` 里，而它**只在 Unity CCD 的 CDN 上**：

- 本地**从没有过**这一包（下载缓存目录是空的；全盘 `*localization*.bundle` 零命中；
  解包资源 **240,173 个 json 搜中日韩字符 = 0 命中**）。
- 实测那个 CDN：**域名通、TLS 通，但 `/client_api/v1/...` 一律 HTTP 400 + 5 字节 `null`**
  （拿伪造的 project UUID 做对照组也是 400 ⇒ 边缘根本不校验，**没有任何可用服务**）。
- URL 里那三个变量（`EnvironmentName`/`BucketId`/`Badge`）由 **PlayFab TitleData** 注入
  （`AddressablesManager.SetupCCDManager` ← `AddressablesConfig` ← `ConfigManager.Unpack(titleData)`），
  **服务器已关 ⇒ 永久丢失**，本地也没有副本。

⇒ **卡牌中文只能用我们自己译的那一份**（本目录的 `zh_cards.json`）。要改中文就在这里改。
同一份 remote-only 名单里还有：`allcards_assets_all`（卡数据）· `alternateartstyles_assets_all`（异画）·
`remotemisccontentdata_assets_all`（LiveOps 配置）。
