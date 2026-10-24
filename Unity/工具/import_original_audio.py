#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_original_audio.py — 把**原版单位语音**导进 Unity 工程 + 生成运行时映射

产出两块：

  ① **音频**：`MyGame/Assets/CardPresentation/Resources/Art/audio/vo/<原文件名>.ogg|wav`
     —— `Resources/Art/` 整个被 `.gitignore` 排除（原版资产不进仓库，和美术同一条规矩）。
     这一份是**运行时真正加载的那份**（`Resources.Load<AudioClip>("Art/audio/vo/<名>")`）。
  ② **映射**：`MyGame/Assets/CardPresentation/Resources/voice_lines.json`
     —— **我们的分析产物，进仓库**。键 = 我们卡表的 `id`，值是每张卡的台词表
     （事件 + 文件名 + 台词原文）。运行时 `VoiceLines.cs` 读它。

数据源（**唯一来源**，2026-09-12 起「以 `assets_full` 为准」那条规矩）：
  · 音频  `d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/AudioClip/`
          （13 个包、**1857 条 / 70.59 MiB**；`d:/2/解包整理/01_卡牌/*/AudioClip/` 是同内容第二份）
  · 归属① 🔴 **权威** = 原版**卡 SO** 的 `summonAudio` / `attackAudio` / `deathAudio`
          （`RawCardScript`，签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:54/56/58`）。
          **读它的落地产物**：`d:/4/Unity/数据/游戏数据/card_vfx_by_card.json` 的
          `byCard[*]._assetRefs`（取 `assetKind == "audio"` 且 `assetName` 非空）；键 `byCard` 的键
          = **我们的卡 id**（该文件里 `origCardId` 与之逐条相等）。
          ⚠️ **为什么读产物、不重解卡 SO 本体**：
          那条链（SO 字段 → GUID → `assetName`）**只有一处正本** = `工具/gen_card_vfx_by_card.py`
          （它的 `CARD_SO_TRUTH`，2026-10-18 起= `资料/普查产出_1018/allcards_1350/cards_raw.json`），
          在这儿再实现一遍就是「同一条规则两处写」（本仓红线）。
          🔴 **2026-10-24（`B2`）**：本脚本**另一处**要读 SO 本体（12 个 `*ChatSound`，见下「归属①·B」），
          那一处用的是**同一个** `cards_raw.json` —— 两条路同源，⛔ 不要再指向 2022 时代的
          `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1,195 张，EC57 的 chat 全空）。
          🔴 **2026-10-24（`D10(C4)`）**：**一个 clip 被多张卡的 SO 引用时，每张卡都挂** ——
          现读 1 处：`VO_UM_Primaris Intercessor - For the Imperium` = `UM6` **与** `EC57`（Xarahan）
          的 `summonAudio`（**原版自己抄错**）。调度台裁定**照原版播**（该行 `src=cardSO`，
          `so=summonAudio`，`file=VO_UM_Primaris Intercessor - For the Imperium.ogg`）。
          历史口径「两张卡共用 ⇒ 不认」已作废（它防的是②/③按名字猜的错配，SO 这一级不含猜测）。
  · 归属② **兜底** = `d:/4/Unity/数据/索引/card_index.json` 的 `cards[].voice`
          （602 张卡 / 1778 条引用；实测 **1778/1778 在音频源里都找得到**）
          🔴 **2026-10-24（`D5`）**：`card_index` 的 `name` 是**本地化键口径**，与我们卡表（**卡面口径**）
          **词序/拼写不同**（`Medic Scion` vs 我们 `Scion Medic` · `Land Rider` vs `Land Raider`）
          ⇒ 按名字 join 不上的那批改走**两条身份级路**（`load_index_identity_join`，⛔ 不靠词序启发式）：
          路 A `name`+`factionId` → 原版 SO `cardName`+`cardArmy` → `uniqueId` → 我们卡 id；
          路 B（A 对不上时）`art` 资产名 → 卡 SO 的 `cardImageReference` 资产名 → 我们卡 id。
          现读 38 张 `no_join` **38/38 全部并上**（A 19 · B 19）。
  · 归属③ **兜底之后再猜** = 前两者都没收录的孤儿，按文件名比卡名（`match_owner`，标 `src=orphan`）
          🔴 **2026-10-24（`D4`）**：③ 这一步是**猜的**，所以再加一道「原版根本不会播的猜行就丢掉」——
          判据与实测见 `orphan_is_dead`（两类：文件名自带 `VO_NOT USED_` · 同卡同事件**已有卡 SO 指名**）。
          现读**只命中 2 条**（`AM70` / `UM21` 各 1 条 `deploy`），⛔ 不是「孤儿全剔」：
          `_vs_` 对手台词与「同事件没有任何 SO 行」的组（`BL35` 四条 `vs*` · `UM48` 的 `Hexscorn_vs_Varro`）**保留**。
          判据 → `资料/普查产出_1024/D4_孤儿deploy行.md`。
  · 归属③·D 🔴 **2026-10-24（`D9`）：开场白 clip 的权威归属改判** =
          `Resources/card_chat_intro.json` 的 `introByOpponent/introByArmy[*].soundAsset`
          （这两档来自原版卡 SO 的 `customIntroChatData[].customSound`）⇒ ②/③ 把它挂错主人时**改回来**
          （现读 **4 条**：`BL5`→`BL34` ×3 对手开场白 · `BL67`→`BL65` ×1 `gen6`）。
          判据 → `资料/普查产出_1024/D9_开场白兜底顺序与脏标.md`。
  · 卡名  `MyGame/Assets/RuleEngine/Resources/cards_engine.json`
          （拿 `(阵营, 卡名)` 把 index 的卡并到**我们的 id** 上 —— index 里没有 id 字段）

⚠️ **四条坑**（`资料/普查产出_0913/语音索引.md` §三 + 2026-09-17 实测）：
  1. **别只 copy `*.ogg`** —— DarkAngels 31 条 + Ultramarines 84 条是 `.wav`，
     `Guilliman` / `Valius Paxor` 两个督军**全套都是 wav**，按 ogg 过滤会整个漏掉。
  2. **死灵族有 44 条不带 `VO_` 前缀**（`Sautekh_Orikan_attack.ogg`），其中 **21 条是被引用的**。
  3. **扩展名/大小写/写法都不统一**：`_attack` 与 `_Attack` · `_cant`（死灵）与 `_cantDo` ·
     `_backup` 与 `_Backup1..N` ⇒ 事件名一律**小写规范化**后再比。
  4. **卡名本身含 `_`**（`Blissbringer_High-pitch_Screech`）⇒ **不能按最后一个 `_` 切分**，
     归属一律用 `card_index.json`，脚本只对**孤儿**（索引没收录的）才按名字猜，并标出来。
     🔴 **2026-10-14 更正（账 A1832）**：原来写「归属一律用 `card_index.json`」—— **已降为兜底**。
       实测 `card_index.json` 的 `cards[].voice` **归属本身就错**（**34 条** clip 记在别的卡名下：
       `BL4`「Chaos Sergeant」→ 记在 `BL46`「Gifts of Chaos」· `DA34` → `DA66` ·
       `EC21` → `EC_Terminator` …；成因见坑 6），而**卡 SO 是对的** ⇒
       改成 **卡 SO 权威 → index 兜底 → 孤儿按名字猜** 三级。逐级条数由脚本运行输出打印。
  🔴 **5.（2026-09-18 更正第 4 条的后半句）「只对孤儿才按名字猜」这个设计漏了 70 条。**
     当时只有一条判据 —— `("_" + 卡名 + "_") in stem or stem.endswith("_" + 卡名)` —— 三类都漏：
       · **卡名两侧不是下划线**：`VO_AM_Bullgryn Bone'ead - Get behind me!.ogg`，名字后面跟的是 ` - `
         （**481 条「文件名自带台词」的文件全是这个形状**）；
       · **撇号变体**：卡面是 `Bone’ead`（U+2019）、文件名是 `Bone'ead`（ASCII）；
       · **文件名用短名**：`VO_Sautekh_Imotekh_attack.ogg`，而卡名是 `Imotekh the Stormlord`。
     实测有**三张卡的整条语音线**就这么没了：`Imotekh the Stormlord`(22) · `Orikan the Diviner`(23)
     —— 这两张的卡名当时还是**美术文件名尾段**（`stormlord` / `Diviner`），已一并按 `STAT_FIXES` 改名；
     `Lord Kaphrael`(17) 靠旧判据勉强并上。
     ⇒ 现在改成**归一化 + 整词边界**（`_norm` / `match_owner`）：
       只拿 `- ` 之前那一段（`header`）去比，撇号/大小写/标点全部归一化，
       允许**卡名首段**当短名 —— 但**必须过唯一性护栏**（首段在池里只指向这一张卡），
       否则不认（项目原则：**宁可认不出，不可认错**）。
     🔴 **2026-10-24（`F5`）补：full / short 两张表改为【统一比较、词数多者优先】。**
       原先「full 先返回」会让**一个词的全名**（`AM17 "Commissar"`）抢赢**另一个更长的首段短名**
       （`AM3 "Commissar Denkler"` 的 `commissar`）⇒ `VO_AM_Commissar Elan_*` 整批挂错卡
       （D5 §四·1 观测到，本轮修）。现在同词数 ⇒ 多义 ⇒ 不认。判据与实例见 `match_owner`。
  🔴 **6.（2026-10-14 新增 · 账 A1832）`card_index.json` 的归属错在**它自己**，不在本脚本。**
     它是**上游产物**（`d:/2/解包整理/card_index.json`，2026-08-15 生成，由 `工具/sync_from_d2.py`
     原样搬进 `数据/索引/`；两份 **md5 逐字节相同**）—— 那时**卡 SO 还没解出来**，
     它的 `cards[].voice` 是靠名字/目录对出来的，对不上就**并到邻近那张卡**上
     （实测 `Gifts of Chaos` 名下挂着 `Chaos Sergeant` + `Chaos Spawn` 两条，而这两张卡的
     `voice` 是**空数组**）。⇒ **本脚本不修它**（`d:/2/` 不在本仓的写范围），
     只把它从「唯一来源」降为「兜底」，并在它和卡 SO 冲突时**以卡 SO 为准**。

🔴🔴 **2026-10-16（账 `A1768`）新增两块 —— 目的是把「猜」换成原版真值**：

**归属①·B = 卡 SO 的 12 个 `*ChatSound`**（原版字段 → clip → 卡）。
   · 为什么单开一级：`gen_card_vfx_by_card.py` 的 `SO_ASSET_FIELDS` 只收
     `summonAudio`/`attackAudio`/`deathAudio` **三个** ⇒ 这 12 个**既不在
     `card_vfx_by_card.json`、也不在 `guid_map.tsv`** 里，只在 SO 本体。
   · 🔴 **2026-10-24（`B2`）换源**：SO 本体从**2022 时代 1,195 张**换成**当前 build 的
     1,351 张**（`资料/普查产出_1018/allcards_1350/cards_raw.json`）。旧快照里
     **EC57（Xarahan）的 12 个 chat 字段 + attack/death 全是空 GUID** ⇒ 那张卡的
     13 条 chat 语音只能靠文件名猜，而文件名是 `Daemon Prince Xarahan`（词序与卡名相反）⇒ 全成孤儿。
     新快照这些字段**全有值**。新旧逐卡比：**1,192 张共有卡里只有 EC57 这一张有字段差异**
     （14 个字段 空→有）；新增 **159 张**卡（其中 6 张督军带 chat 字段）；旧表独有 3 个键
     （`#HB_BL34` / `#HB_DA1` 两个异画占位 + 1 个 `uniqueId` 为 null 的）。判据 → `B2_语音归属换真值SO.md`
   · GUID → 资产名：**并集**取 `card_vfx_by_card.json` 的 `_assetRefs`（带 `guid`+`assetName`）
     与 `数据/索引/anim_address_map.json` 的 `guid_to_asset`，中间夹一层 catalog 补过短键的
     `guid_map_bundles.tsv`（2026-10-23 · `C5`，见 `load_guid_name_index`）。实测新源 15 个字段
     **2,000+ 条引用里只剩 3 条解不出**（`BL47` summon · `GOF2`/`77a7…` 同一条 death），**逐条打在运行输出里**，⛔ 不静默。
   · SO `uniqueId` → 我们卡 id：走 `card_vfx_by_card.json` 的 `_assetRefs[].origCardId`
     （实测 **1,127 对**，覆盖 `cards_engine.json` 全部 1,127 张，缺 0）。
   · ⛔ **3 个战斗字段的「字段名 → 事件名」表不在这里重写** —— 那是
     `gen_card_vfx_by_card.py:195` `AUDIO_SO_FIELD_EVENT` 的正本，本脚本 `import` 它。

**事件名回真值**（`ev` 那一路）：以前 `ev` 一律由**文件名后缀**推（`parse_name`）。
   现在多一道**校正**：任何一条 clip，只要它在**这张卡自己的 SO** 里被**恰好一个**原版字段引用过，
   就按那个字段定 `ev`，并写 `so` = 原版字段名。
   · 🔴 **2026-10-24（`B2`）口径从「clip 级」改成「卡级」** —— 这是本轮的第二处根治：
     旧口径按 **clip** 建真值表（「这个 clip 在全池里被哪些字段引用过」），于是**别的卡**
     引用同一 clip 时会**张冠李戴**。实测：`VO_Ultramarines_Ventris_gen4.ogg` 只有
     **教程版 Ventris**（`ec0d25fa…`）的 `generic2ChatSound` 指着它，而旧口径把它算进
     `UM47` 头上 ⇒ 那一行的 `ev` 被从 `gen4` 改写成 **`gen2`**（`gen7`→`gen3` 同理）。
     现在真值表键是 `(我们卡 id, clip)`，**只认这张卡自己的字段** ⇒ 那两行保持 `gen4`/`gen7`。
     同时 `UM47` 的 `intro/gen1/gen3` 三条**不再被判成「两个 chat 字段共引、不认」** ——
     共引来自教程版，卡级看它们各自只有一个字段（`introChatSound`/`generic1ChatSound`/`generic3ChatSound`）。
   · ⇒ 因此 `line` 这个**我们自己造的名字退场**，改叫原版的 `summonAudio` 的译名 `deploy`
     （译名同 `AUDIO_SO_FIELD_EVENT`，**只此一处**）。判据：出场台词那一族
     `VO_<阵营>_<卡名> - <台词>.ogg` 实测 **448/536 条**就是 `summonAudio` 的落点。

🔴 **2026-10-24（`D7`）两处脏记法订正**（判据 = `资料/普查产出_1024/C5_剩余判据普查.md` §4 / §5）：

   a. **`_vs_` 对手台词不再记 `deploy`**（现读 **19 条**：`AM66`×1 · `ASH3`×3 · `BL35`×4 ·
      `BL36`×7 · `SAU36`×4）。`<主人>_vs_<对手>` / `<主人>_vs<对手>_v2` 的尾段是**对手名**或
      **版本号** ⇒ `parse_name` 解析失败，以前一律落 `deploy`。**那是错的**：
      · `deploy` = 卡 SO 的 `summonAudio`，而这 5 张卡的 `summonAudio` **全是空 GUID**
        （`cards_raw.json` 现读）⇒ 原版登场**不响**；
      · 正确通道 = **开场白 Intro**（`ChatManager.GetCustomIntro`，按键 = 对手督军），
        权威数据 = `Resources/card_chat_intro.json`（197 条），消费点 = `BattleDriver.SpeakIntro`。
      ⇒ 这批行改记 **`ev="intro-vs"`**，并标 `src="orphan"`（归属/事件都未经卡 SO 佐证）。
      `VoiceLines.TryPick` 对非 `vs~` 请求是**精确比较**、`vs~<对手>` 只认以 `vs` 开头的真 ev
      ⇒ `intro-vs` **永远不会被选中**（结构上不可能播错）。⛔ 别把它加进任何 `For*` 表。
      （19 条里有 14 条能在 `card_chat_intro.json` 里按 `soundAsset` 找到、5 条找不到 ——
      逐条见 `D7_语音脏数据订正.md`；找不到的那 5 条**只报不改**。）

   b. **`gen4..gen7` 是死通道**（现读 **215 行**）。原版 `RawCardScript` 只有 3 个
      `generic*ChatSound`（→ `gen1..3` = Taunt/Sorry/Oops），`ChatMessage` 11 个值只映射 0..10，
      `GetAvailableSounds` 的 15 个字段里也没有 `generic4..7` ⇒ 这 215 行**永远选不中**。
      数据**保留**（留证据），只把归属标 `src="orphan"`（判据 `C5` §5）。⛔ 别当「待接的 ev」。

用法：
    PYTHONIOENCODING=utf-8 python 工具/import_original_audio.py --check   # 只报告，不写盘
    PYTHONIOENCODING=utf-8 python 工具/import_original_audio.py           # 拷音频 + 写映射
"""
import argparse
import glob
import io
import json
import os
import re
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")

UNPACK = "d:/2/新解包资源/assets_full"
CARD_INDEX = "d:/4/Unity/数据/索引/card_index.json"
CARDS_ENGINE = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
# 🔴 **归属①（权威）**：卡 SO 的 `summonAudio`/`attackAudio`/`deathAudio` 的落地产物。
# ⚠️ 取**全量那份**（`数据/` 下），不是 `Resources/` 下那份运行时的投影 ——
#    全量的带 `assetKind`/`assetName`/`soField`，且不进包体。
CARD_SO_VFX = "d:/4/Unity/数据/游戏数据/card_vfx_by_card.json"
# 🔴 **归属①·B（权威）**：卡 SO 的 12 个 `*ChatSound` —— 只在 SO **本体**里（见文件头）。
# 🔴 **2026-10-24（`B2`）换源**：原来是 **2022 时代快照**（`d:/2/远端缓存_旧profile_1013/导出/
#   cards_1195_2022时代.json`，1,195 张）—— 它**没有 EC57（Xarahan）的 12 个 chat 字段**（全空 GUID），
#   于是那张卡的 13 条 chat 语音一条都挂不上（详见 `资料/普查产出_1024/B2_语音归属换真值SO.md`）。
#   当前 build 的 `allcards` 已在本地：**1,351 张 `RawCardScript`**（独立复核过：
#   `d:/2/Warpforge_tools/py312/python.exe -I` + UnityPy 读 bundle，`has cardAbilities` = 1,351）。
#   判据 → `资料/普查产出_1018/allcards_1350/报告.md`。
#   ⚠️ 形状是 `{bundle,count,cards:[{…,full_raw}]}`（**卡 SO 全字段在 `full_raw`**）——
#   `load_card_so_audio_truth` 自己展平；同一做法见 `gen_card_vfx_by_card.py:1086`（那边 2026-10-18 已换源）。
CARD_SO_TRUTH = "d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
# GUID → 资产名 的三个来源（**优先级从高到低**：`_assetRefs` → `GUID_MAP_TSV` → `ANIM_ADDRESS_MAP`；
#   🆕 2026-10-23 起 `ANIM_ADDRESS_MAP` **只作兜底** —— 它对短键束有 12 条真错，见 `load_guid_name_index`）
ANIM_ADDRESS_MAP = "d:/4/Unity/数据/索引/anim_address_map.json"
# 🆕 2026-10-23（C5）：**catalog 补过短键的 guid_map** —— 优先级**高于** `anim_address_map`（判据见
#   `load_guid_name_index` 的 docstring；只用其中 `type == AudioClip` 的行）。
GUID_MAP_TSV = "d:/4/Unity/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"
# 🔴 **归属③·D（权威改判）**：`Resources/card_chat_intro.json`（生成器 `工具/gen_card_chat_intro.py`；
#   它的 `id` 就是**我们卡表的 id**）。它的 `introByOpponent[*].soundAsset` /
#   `introByArmy[*].soundAsset` 来自原版卡 SO 的 `customIntroChatData[].customSound` GUID
#   ⇒ **比 `card_index.json` 的 `cards[].voice` 可信**（后者 2026-08-15 生成时卡 SO 还没解出来）。
#   用途：把 ②/③ 按名字/目录挂错主人的开场白 clip **改判**回原版 SO 说的那张卡（判据 → `D9` 报告）。
CHAT_INTRO_RUNTIME = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_chat_intro.json"
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/vo"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/voice_lines.json"

# 认得出的事件后缀（**小写**比较）。⚠️ `wp` 的语义曾经「未证实」——
# 🔴 **2026-10-16（`A1768`）已坐实**：`RawCardScript__GetChatSound.c` 的 `switch` 里
#    `case 7 → *(RawCardScript + 0x1D0)`，而 `dump.cs:23311` 写着 `wellPlayedChatSound; // 0x1D0`
#    ⇒ **`wp` = `wellPlayedChatSound`（原版 `ChatMessage.WellPlayed` = 枚举 7）**。
#    同一条 switch 把另外 10 个也钉死了（下表）。
KNOWN_EVENTS = {
    "greet", "attack", "death", "concede", "hurry", "intro", "mirror", "threat",
    "wp", "tbc", "cantdo", "cant", "backup",
}
_EVENT_RE = re.compile(r"^(gen[1-9]|backup\d+|vs.*)$")
# 🔴 **2026-10-24（`D7`）**：文件名里有没有「对手台词段」（`_vs_<对手>` / `_vs<对手>`）。
#   只在**尾段解析失败**（否则会落 `deploy`）时才用 —— 见 `parse_name`。
_VS_SEG_RE = re.compile(r"(?:^|_)vs[^_]*", re.I)

# 🔴 **2026-10-24（`D7`）**：原版**没有**的 `gen4..gen7` 通道（`C5` §5）。
#   原版 `RawCardScript` 只有 3 个 `generic*ChatSound`（→ `Taunt/Sorry/Oops` = `gen1..3`），
#   `ChatMessage` 11 个值（0..10）只映射到它们；`GetAvailableSounds` 的 15 个字段里也没有
#   `generic4..7`。⇒ `voice_lines.json` 里 `ev=gen4..7` 的行**永远选不中**
#   （`VoiceLines.ForChatButton` 只请求 `gen1..gen3`，`TryPick` 是精确匹配）。
#   数据**保留**（留证据），只把归属**标成 `orphan`**（= 按文件名猜的、未经卡 SO 佐证）。
DEAD_EVENTS = {"gen4", "gen5", "gen6", "gen7"}

# 🔴 **卡 SO 的 12 个 `*ChatSound` → 我们的事件名**（与 `VoiceLines.cs` 那一族逐字一致）。
#    判据（**两条独立证据，都现核过**）：
#      ① `RawCardScript__GetChatSound.c` 的 `switch(chatMsgIndex)` 给出**枚举号 → 字段偏移**：
#         0→0x190 intro · 1→0x198 mirror · 2→0x1A8 hurry · 3→0x1B0 iCantDoThat ·
#         4→0x1A0 concede · 5→0x1B8 greet · 6→0x1C0 threat · 7→0x1D0 wellPlayed ·
#         8→0x1C8 generic1 · 9→0x1D8 generic2 · 10→0x1E0 generic3。
#         🔴 **2026-10-24（`B2`）就地订正**：原来这里写「`default:` 那支取不到时落回
#         `+0x188` = `defaultChatSound`」—— **现核不成立**：那条 `default:` 分支只调
#         `CustomDebug__LogWarning` 然后返回空，**根本没有读 `+0x188`**；`ChatMessage` 枚举
#         也只有 0..10（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ChatMessage.cs`），
#         没有 default 成员 ⇒ **`defaultChatSound` 在这个 build 的取音链上是不可达的**。
#         ⚠️ 但**表里仍保留 `defaultChatSound` 这一行**（数据照原版落，不删字段）——
#         消费者的「`default` 兜底」是 `VoiceLines.cs:313-374` 的既有设计，不在本脚本范围。
#         详见 `资料/普查产出_1024/B2_语音归属换真值SO.md` §顺手发现。
#      ② `dump.cs:23300-23315` 的**字段名 ↔ 偏移**逐行对得上上面那组偏移。
#      ③ 枚举名 = `dump.cs:23189-23207`（`ChatMessage`），与 `资料/语音线_原版规格与ASR管道.md` §1.1 同表。
#    ⛔ `generic4..7` 在字段表里**不存在**（类在 0x1E0 结束）⇒ 不认，别把它当第 13–16 个字段。
#       ⇒ EC57 的 `gen4.ogg` / `gen5.ogg` 等**在 1,351 张 SO 里没有任何字段引用**（原版空），
#         ⛔ 别按文件名猜着补（`资料/普查产出_1024/B2_语音归属换真值SO.md` §EC57）。
CHAT_SO_FIELD_EVENT = {
    "introChatSound":       "intro",
    "mirrorChatSound":      "mirror",
    "hurryChatSound":       "hurry",       # 原枚举名 `Bored`，字段改名前叫 `boredChatSound`
    "iCantDoThatChatSound": "cantdo",
    "concedeChatSound":     "concede",
    "greetChatSound":       "greet",
    "threatChatSound":      "threat",
    "wellPlayedChatSound":  "wp",          # ← `wp` 的语义就在这一行（见上）
    "generic1ChatSound":    "gen1",        # 原 `tauntChatSound`
    "generic2ChatSound":    "gen2",        # 原 `sorryChatSound`
    "generic3ChatSound":    "gen3",        # 原 `oopsChatSound`
    "defaultChatSound":     "default",     # 原版聊天的**兜底**（`GetChatSound` 取不到词条时落它）
}


def norm_event(suffix):
    """文件名尾段 → 规范事件名（认不出返回 None）。"""
    s = suffix.lower()
    if s in KNOWN_EVENTS:
        return "cantdo" if s == "cant" else ("backup" if s == "backup" else s)
    if _EVENT_RE.match(s):
        return s
    return None


def parse_name(base):
    """文件名 → (事件, 台词原文)。`<...> - <台词> (配音演员)` 这种是**出场台词**（普通单位就这一条）。

    🔴 **2026-10-16（`A1768`）：`line` 这个名字退场，改叫 `deploy`。**
    理由：`line` 是**我们自己**造的词，而原版这一族有字段名 —— `RawCardScript.summonAudio`
    （`dump.cs:23270`，偏移 `0xB8`），译名 `deploy`（`gen_card_vfx_by_card.AUDIO_SO_FIELD_EVENT`）。
    实测佐证：`summonAudio` 指到的 clip 里 **448/536** 正是这种「` - <台词>`」命名；
    消费点 = `VoiceLinesController__DisplaySummonMessage.c:31` → `RawCardScript__GetSummonSound`
    ⇒ 「登场那句话」与「出场台词」**是同一件事**（这一条 2026-10-14 还写着「无判据」）。
    ⚠️ 没有 SO 佐证的那些（自造卡 / 2025-07 之后的新卡）**按同一命名规律归入**，
       这条是**推断**：`so` 字段为空即表示「没佐证」。
    🔴 **2026-10-24（`D7`）`_vs_` 对手台词不再落 `deploy`**：`<主人>_vs_<对手>` /
       `<主人>_vs<对手>_v2` 这类文件的尾段是**对手名**（`_vs_Eliac` → `Eliac`）或**版本号**
       （`_vsTau_v2` → `v2`）⇒ 以前全部落到 `deploy`，那是**记法错误**（现读 19 条）。
       它们的权威通道是**开场白 Intro**（`ChatManager.GetCustomIntro`；数据在
       `Resources/card_chat_intro.json`，消费点 `BattleDriver.SpeakIntro`），而 `deploy` =
       卡 SO 的 `summonAudio` —— 这 5 张卡（`AM66/ASH3/BL35/BL36/SAU36`）的 `summonAudio`
       **全空**，原版登场不响。⇒ 现记成 **`intro-vs`**：`VoiceLines.TryPick` 对非 `vs~` 请求
       是**精确比较**、`vs~<对手>` 只认以 `vs` 开头的真 ev ⇒ 这个值**永远不会被选中**
       （不会播错），同时把「开场白对手台词、不是 deploy」写进数据。
       判据 → `资料/普查产出_1024/C5_剩余判据普查.md` §4。"""
    stem = os.path.splitext(base)[0]
    if " - " in stem:
        text = stem.split(" - ", 1)[1].strip()
        # 去掉结尾的配音演员名（`(Vivienne)` 这种）
        text = re.sub(r"\s*\([^)]*\)\s*$", "", text).strip()
        return "deploy", text
    # 没有 " - " 的：看尾段是不是事件
    tail = stem.rsplit("_", 1)[-1] if "_" in stem else ""
    ev = norm_event(tail)
    if ev:
        return ev, ""
    # 🔴 2026-10-24（`D7`）：见 docstring —— `_vs_` 对手台词**不是**登场台词。
    if _VS_SEG_RE.search(stem):
        return "intro-vs", ""
    return "deploy", ""


def load_audio_index():
    """{文件名(basename): 完整路径} —— 13 个 bundle 的 AudioClip 全平铺。"""
    idx = {}
    for d in sorted(glob.glob(os.path.join(UNPACK, "bundle_*cardassets*_assets_all", "AudioClip"))):
        for f in os.listdir(d):
            idx.setdefault(f, os.path.join(d, f))
    return idx


# ======================================================================
#  卡名 ↔ 音频文件名 的比对（**2026-09-18 重写**，改动理由见文件头「坑 5」）
# ======================================================================

# 撇号有**三种码位**在混用（卡面用弯的、文件名用直的）。不归一化的话
# `Bullgryn Bone’ead` 永远认不出 `VO_AM_Bullgryn Bone'ead - ...ogg`。
_APOS = str.maketrans({"’": "'", "‘": "'", "ʼ": "'", "`": "'"})


def _norm(s):
    """归一化：撇号统一 → 小写 → **非字母数字一律变空格** → 压空格。
    例：`Bone’ead` 与 `Bone'ead` → 都是 `bone ead`。"""
    s = (s or "").translate(_APOS).lower()
    return " ".join(re.sub(r"[^a-z0-9]+", " ", s).split())


def build_name_tables(cards):
    """建两张**只收唯一键**的表，供 `match_owner` 用：
      · `full`  —— 归一化**全名** → [卡, ...]（正常情况就这一条生效）
      · `short` —— 归一化**首段**（≥5 字符）→ [卡, ...]，**首段必须唯一指向一张卡**
    名字在池里本来就重名的（`Terminator` 之类）**整条不收** —— 宁可认不出。"""
    full, short = {}, {}
    for c in cards:
        n = _norm(c["name"])
        if n:
            full.setdefault(n, []).append(c)
        head = n.split(" ")[0] if n else ""
        if len(head) >= 5 and head != n:
            short.setdefault(head, []).append(c)
    return ({k: v for k, v in full.items() if len(v) == 1},
            {k: v for k, v in short.items() if len(v) == 1})


def match_owner(stem, tables):
    """文件名 stem → 池里的那张卡（认不出返回 `None`）。

    🔴 **只拿 `- ` 之前那一段去比**。为什么：481 条文件的 `- ` 后面是**台词原文**，
       而台词里完全可能出现**别的卡的名字**（`... - Morkai's claws reach longer ...`）——
       拿整条名字串去比会把这些**误配到别人头上**。截断之后 header 只剩
       `<前缀>_<阵营>_<卡名>`，干净得多。
    🔴 **2026-10-24（`D5`）`_vs_` 对手台词：只认 `_vs_` 之前那一段当主人。**
       这些文件是 `<主人>_vs_<对手>`，**两头都是卡名**。以前拿整条 header 去比 ⇒ 命中的是
       **对手**：`VO_BlackLegion_Hexscorn_vs_Varro` 的主人 `Sylar Hexscorn` 全名/首段
       （`sylar`）都不在 header 里，唯一命中是**对手短名** `varro`（`Varro Tigurius`）
       ⇒ 这条黑军团台词被挂到 `UM48` 头上，而 `UM48` 的 SO `summonAudio` **是空的**
       （原版 Varro 登场没有语音，我们却会播一条黑军团台词）。
       现在只拿 `_vs_` 之前那段（`VO_BlackLegion_Hexscorn`）去比 ⇒ 认不出 ⇒ **不猜**。
       ⛔ 不许「两边都试」（那正是这次错配的来源）；判据 →
       `资料/普查产出_1024/D5_语音归属两处.md`。
    🔴 **2026-10-24（`F5`）命中优先级：`full` 与 `short` 两张表【统一比较】，
       取 **最具体** 的那个键（**词数多者优先**；词数相同 ⇒ 视为同名多义 ⇒ **不认**）。
       以前是 `for tbl in tables:`（**full 先返回**）—— 那是「先全名、后短名」的本意，
       但**一个只有 1 个词的全名，会抢赢另一个词更多的首段短名**，正好相反。实测（D5 §四·1）：
       `AM17 "Commissar"` 的**全名就是一个词** `commissar`，而 `AM3 "Commissar Denkler"`
       的**首段**也是 `commissar` ⇒ `VO_AM_Commissar Elan_*` 这 10 条在 full 层就命中
       `AM17` 并返回，`short["commissar"] → AM3` 根本没轮到（`AM17` 的卡 SO 只指
       `VO_AM_Commissar - Do your duty...` 一条，其余它一条都不该有）。
       现在同样这两条命中是**同词数（1）不同卡** ⇒ 判为多义 ⇒ **不认**（既不猜 `AM17`，
       也不猜 `AM3`）；它们真正的主人由 `card_index`（②）与卡 SO（①）定。
       ⚠️ **具体度用「词数」不用「字符数」**：字符数会让**阵营词**翻车 ——
       `VO_Tyranid_Leviathan_Biovore` 里 `short["leviathan"]`（`TL52 "Leviathan's Tendrils"`
       的首段）比 `full["biovore"]` 长，按字符数会把虫族登场音挂到 `TL52` 头上。
    判据：**整词**匹配（两侧补空格再找）；命中**必须恰好一张卡**（词数相同且不同卡 = 多义）。"""
    header_raw = stem.split(" - ", 1)[0]
    # 只在**主人侧**找归属：`_vs_` 之后是对手，**一律不看**。
    header_raw = re.split(r"_vs_", header_raw, maxsplit=1, flags=re.I)[0]
    header = " " + _norm(header_raw) + " "
    best_words = -1          # 命中的键**含几个词**（越多 = 覆盖得越具体）
    best = None
    ambiguous = False
    for tbl in tables:                       # ⛔ 两张表**一起**比，别再「full 命中就返回」
        for key, cards in tbl.items():
            if " " + key + " " not in header:
                continue
            card = cards[0]
            words = len(key.split())
            if words > best_words:
                best_words, best, ambiguous = words, card, False
            elif words == best_words and card is not best:
                ambiguous = True             # 同词数、不同卡 ⇒ 多义 ⇒ 最后不认
    if ambiguous:
        return None                          # 宁可认不出（⛔ 不许靠长度/顺序任选一个）
    return best


def orphan_is_dead(base, cid, ev, lines):
    """🔴 **2026-10-24（`D4`）**：③ 猜出来的孤儿行里，「原版根本不会播」的两类 ⇒ 生成侧**丢掉**。

    判据（可复跑；**缺一不可**地只对这几种情形生效）：

      (a) **文件名自带 `VO_NOT USED_`** —— 原版自己标了这条废弃。
          实测全库**只有 1 条**：
          `VO_NOT USED_UM_Primaris Judiciar - You're not ready to take on a foe like me.ogg`
          （`d:/2/新解包资源/assets_full/bundle_spacemarinesultramarinescardassets_assets_all/AudioClip/`）。

      (b) **同一张卡、同一个事件，已经有卡 SO 指名的行**，且这条**不是对手台词**（文件名不带 `_vs_`）。
          为什么丢：卡 SO 才是原版实际播放的落点（`RawCardScript` 对应字段），而这一条
          **在 1,351 张 SO 的 15 个音频字段里零引用**（2026-10-24 现读，3 条解不出的 GUID 都不是它）
          ⇒ 原版不会播它，留着只会让表现层多一次「猜的中奖」。
          实测只命中 `AM70` 的 `VO_AM_Medic Scion - No soul left behind.ogg` ——
          该卡 SO 的 `summonAudio` 指的是另一条 `... -On your feet!...ogg`。
          ⚠️ 它之所以走到 ③：`card_index.json` 把这张卡记作 `Medic Scion`（本地化键的词序），
          我们卡表是卡面印的 `Scion Medic` ⇒ ② 那条 join 不上，才掉进按名字猜。

    ⛔ **不推广成「孤儿全剔」**：`_vs_` 对手台词（`BL35` 的 4 条 `vs*`、`UM48` 的 `Hexscorn_vs_Varro`）
       与「同事件一条 SO 行都没有」的组，**全部保留** —— 它们没有 SO 真值可依，按文件名认是设计内的兜底。

    🔴 **2026-10-24（`D5`）调用点从 1 处变 2 处：② 级也用同一条判据。**
       原因不是新口径，而是**② 的覆盖面变大了**：`D5` 让 `card_index` 里词序不同的一批卡
       靠 `uniqueId` 并上了（`load_index_uid_join`），于是原来走 ③ 才会被这条规则碰到的行，
       现在从 ② 进来 —— 实测 `AM70` 的 `VO_AM_Medic Scion - No soul left behind.ogg`
       就是这样**绕过 D4 的剔除又回来了**（那张卡的 SO `summonAudio` 指的是另一条）。
       判据本身一字未改（「同一张卡的 SO 已经不播它 ⇒ 猜的那条丢掉」对 ②/③ 同样成立），
       只是**在需要的地方都调一次**；⛔ 不是把孤儿的范围放大。
       ⚠️ ①/①·B（卡 SO）**仍然不受它约束** —— 那两级是原版真值。

    返回 `None` = 保留；返回 `str` = 丢弃理由（调用方**逐条打印**，⛔ 不静默）。"""
    stem = os.path.splitext(base)[0]
    if stem.upper().startswith("VO_NOT USED_"):
        return "文件名带 `VO_NOT USED_`（原版自己标了废弃）"
    if "_vs_" in base.lower():
        return None
    if any(r.get("so") or r.get("src") == "cardSO"
           for r in (lines.get(cid) or []) if r.get("ev") == ev):
        return "同卡同事件已有卡 SO 指名的行（本行在全部卡 SO 里零引用）"
    return None


def load_chat_intro_owner():
    """🔴 **2026-10-24（`D9`）开场白的权威归属**：clip 名 → 我们卡 id。

    数据 = `Resources/card_chat_intro.json`（生成器 `工具/gen_card_chat_intro.py`；`cards[].id`
    就是我们的卡 id）。取 `introByOpponent[*].soundAsset` 与 `introByArmy[*].soundAsset` ——
    这两个值来自原版卡 SO 的 **`customIntroChatData[].customSound` GUID**（生成器经
    `guid_map_bundles.tsv` 解成资产名）⇒ **比 `card_index.json` 的 `cards[].voice` 可信**。

    为什么需要它（现读 **4 条**错配，全是 `card_index` 的锅 —— 它 2026-08-15 生成时卡 SO 还没解出来，
    靠名字/目录对，对不上就并到邻近那张卡，见文件头坑 6）：
      · `VO_BlackLegion_Abaddon_vsGrukk` / `_vsHexscorn` / `_vsOrikan` 被记在
        **`BL5`「Hound of Abaddon」** 名下，而 `card_chat_intro` 证明它们是
        **`BL34`「Abaddon the Despoiler」** 的三条对手开场白（对手 `GOF29` / `BL36` / `SAU37`）；
      · `VO_BL_Ghallaron_gen6` 被记在 **`BL67`「Ghallaron's Champion」** 名下，
        而它是 **`BL65`「Ghallaron the Pious」** 对 `UM65` 的定制开场白音（卡 SO 的
        `customIntroChatData` 指的就是它；⚠️ `gen6` 本身是**死通道**，改判不改变运行时行为）。
    判据 → `资料/普查产出_1024/D9_开场白兜底顺序与脏标.md`。

    护栏（宁可认不出，不可认错）：**同一个 clip 被两张不同卡认领 ⇒ 整键不收并出声**（现读 0 条）。
    ⚠️ 调用方**只拿它改判 ②/③ 的行**；`src=cardSO` / 带 `so` 的行是原版 15 个字段的真值，一律不动。

    返回 `(map, problems)`。"""
    problems = []
    try:
        db = json.load(io.open(CHAT_INTRO_RUNTIME, encoding="utf-8"))
    except Exception as e:                                  # noqa: BLE001
        problems.append(f"⚠️ 读不到开场白表 `{CHAT_INTRO_RUNTIME}`（{e}）"
                        "⇒「按开场白权威改判归属」（③·D）这一级失效，那几条 `card_index` 的错配会留着")
        return {}, problems

    owner, amb = {}, {}
    for c in (db.get("cards") or []):
        cid = (c or {}).get("id")
        if not cid:
            continue
        for field in ("introByOpponent", "introByArmy"):
            for _k, v in (c.get(field) or {}).items():
                sa = (v or {}).get("soundAsset")
                if not sa:
                    continue                       # GUID 解不出时生成器留空 —— 这一条没有归属证据
                prev = owner.get(sa)
                if prev is None:
                    owner[sa] = cid
                elif prev != cid:
                    amb[sa] = (prev, cid)
    for sa, (a, b) in amb.items():
        owner.pop(sa, None)
        problems.append(f"⚠️ 开场白 clip `{sa}` 被两张卡认领（{a} / {b}）⇒ 整键不收（宁可认不出）")
    return owner, problems


def load_engine_name_alias():
    """`{旧名小写: 新名}` —— 直接读 `gen_cards_engine.py` 的 `STAT_FIXES` 与 `ZH_NAME_ALIAS`。

    🔴 **为什么从那个文件读、而不在这里另抄一份**：改名这件事**只有一处正本** ——
    `STAT_FIXES`（卡表生成器用的就是它）。抄第二份 = 迟早不一致（项目反复强调的坑）。
    ⚠️ 2026-09-18 实测踩过：`card_index.json` 用的是**旧名**（`Land Rider`），
      卡表里是**新名**（`Land Raider`）⇒ 路径① 按 `(阵营, 卡名)` join 全落空，
      那 8 张卡的语音一起丢。
    ⚠️ **两张表都要读**：`STAT_FIXES` 是「旧名 → 新名」（键 = 旧名，直接可用）；
      `ZH_NAME_ALIAS` 是「**新名 → 中文表里的旧键**」（键 = 新名，要**反过来**用）。
      实测漏掉后者时，`Fire Warrior Sniper`（卡表里叫 `Fire Warrior Marksman`）仍 join 不上。"""
    try:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import gen_cards_engine as g
    except Exception as e:                                  # noqa: BLE001
        print(f"⚠️ 读不到 gen_cards_engine（{e}）—— 改过名的卡会 join 不上")
        return {}
    alias = {k.lower(): v["name"] for k, v in g.STAT_FIXES.items() if "name" in v}
    for new_name, old_name in g.ZH_NAME_ALIAS.items():       # 反向：旧键 → 引擎里的新名
        alias.setdefault(old_name.lower(), new_name)
    return alias


def load_card_so_audio():
    """🔴 **归属①（权威）**：原版**卡 SO** 的逐卡语音绑定 → `{文件名 stem: [卡 id, ...]}`。

    读的是它的**落地产物**（见文件头「为什么读产物、不重解 SO」）。
    值是 **list** —— 同一个 `AudioClip` 可能被**两张卡**引用（现读 **1 处**：
    `VO_UM_Primaris Intercessor - For the Imperium` = `UM6`+`EC57` —— **原版自己抄错**，
    EC57 的 `summonAudio` 指到了 UM 的登场语音）。
    🔴 **2026-10-24（`D10(C4)`）改口径**：调用方**不再「唯一才认」** —— 这一级的 owners 全部来自
    卡 SO 的 `_assetRefs`（不含猜测）⇒ **多 owner 时每张卡都挂**（原版会照 SO 播，我们照播）。
    历史口径「唯一才认」防的是**按名字猜**那一级（②/③）的错配，本函数不适用。

    返回 `(map, problems)`：`problems` 是该出声的异常（读不动 / 形状变了），⛔ 不静默吞。
    """
    problems = []
    try:
        by = json.load(io.open(CARD_SO_VFX, encoding="utf-8"))["byCard"]
    except Exception as e:                                  # noqa: BLE001
        problems.append(f"🔴 读不到卡 SO 产物 `{CARD_SO_VFX}`（{e}）"
                        "⇒ **归属①整级失效**，只剩 `card_index.json` 兜底（那套归属是错的）")
        return {}, problems
    out = {}
    for cid, v in by.items():
        for r in (v or {}).get("_assetRefs", []):
            if r.get("assetKind") != "audio":
                continue
            name = r.get("assetName")
            if not name:
                continue        # 实测 2 条（`GOF2` death / `BL47` summon）：GUID 在 guid_map 里就没有
            if cid not in out.setdefault(name, []):
                out[name].append(cid)
    return out, problems


# ======================================================================
#  🆕 2026-10-16 · 账 `A1768`：归属①·B（12 个 `*ChatSound`）+ 事件名回真值
# ======================================================================

def _load_vfx_json():
    """读一次全量那份 `card_vfx_by_card.json`（两处要用，读一次省 40 MB 的重复解析）。"""
    return json.load(io.open(CARD_SO_VFX, encoding="utf-8"))["byCard"]


def load_guid_name_index(by_card):
    """`{guid 小写: 资产名}` —— **三个现成产物的并集**，本脚本**不自己解 GUID**。

    🔴 **优先级（2026-10-23 · `C5`，⛔ 别调换）**：
      ① `card_vfx_by_card.json` 的 `_assetRefs[*]`（带 `guid` + `assetName`）—— 逐卡 VFX 表自己的解；
      ② **`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`**（**catalog 补过短键的那份**）——
         只收 `type == AudioClip` 的行；同一 GUID 多行时取第一条（音频没有 Sprite/Texture2D 那种类型对）；
      ③ `数据/索引/anim_address_map.json` 的 `guid_to_asset` —— **最后的兜底**。
    **三处不一致时以 ②（catalog-backed）为准** —— ③ 是旧链路（目录退键）建的，对**短键束**
    （EC cardassets 键 `8/a/9/c/3/…`）有 12 条真错（6 音频 + 4 美术/卡框 + 2 无法核，2026-10-23 现核）。
    实测（2026-10-23）：不加 ② 时 EC56 的 concede/gen1/greet/threat 会被 ③ 指到
    `Daemonette / Blastmaster / Xarahan_gen5 / Icon of Excess` 的音频上（**错配**）；
    加 ② 后 = 该卡 SO 真正引用的 `Kaphrael_concede / _gen1 / _greet / _threat`。
    ⚠️ `anim_address_map.json` **本批不改**（另立账）；这里只是让它不再压过 catalog 的解。

    三路都解不出 = 真的没有（实测只剩 EC55/EC56 整套 + 2 条），调用方**逐条出声**。"""
    idx = {}
    for _cid, v in (by_card or {}).items():
        for r in (v or {}).get("_assetRefs", []):
            g = (r.get("guid") or "").lower()
            n = r.get("assetName")
            if g and n:
                idx.setdefault(g, n)
    # ② catalog 补过的那份 guid_map（只收 AudioClip；**优先于 ③**）
    try:
        import csv as _csv
        n_tsv = 0
        with io.open(GUID_MAP_TSV, encoding="utf-8") as fh:
            for row in _csv.DictReader(fh, delimiter="\t"):
                if (row.get("type") or "") != "AudioClip":
                    continue
                g = (row.get("assetGuid") or "").lower()
                n = row.get("name") or ""
                if len(g) == 32 and n:
                    if g not in idx:
                        n_tsv += 1
                    idx.setdefault(g, n)
        if n_tsv:
            print(f"  （GUID→音频名：`{os.path.basename(GUID_MAP_TSV)}` 多解出 {n_tsv} 条）")
    except Exception as e:                                  # noqa: BLE001
        print(f"⚠️ 读不到 `{GUID_MAP_TSV}`（{e}）—— 退回只用 `_assetRefs` ∪ `anim_address_map`"
              "（短键音频会继续按旧表的错名走）")
    # ③ 最后兜底（旧链路，短键有错；⛔ 别提到 ② 前面）
    try:
        am = json.load(io.open(ANIM_ADDRESS_MAP, encoding="utf-8"))["guid_to_asset"]
        for g, a in am.items():
            n = (a or {}).get("name")
            if n:
                idx.setdefault(g.lower(), n)
    except Exception as e:                                  # noqa: BLE001
        print(f"⚠️ 读不到 `{ANIM_ADDRESS_MAP}`（{e}）—— 那 12 个 chat 音会多一批解不出")
    return idx


def load_card_so_audio_truth(by_card, audio_stems):
    """🔴 **归属①·B + 事件名真值表**（**卡级**，2026-10-24 `B2` 起）。

    返回 `(own, card_truth, stats, problems)`：
      · `own`        —— `{clip stem: [我们卡 id, ...]}`，**只收 12 个 chat 字段**（归属①·B）
      · `card_truth` —— `{(我们卡 id, clip stem): {原版字段名: 我们的事件名}}`，**15 个字段全收**
                        —— 它同时给「事件名回真值」用（见文件头）。
      · `stats` —— 逐字段的引用数 / 已解 / 未解，⛔ 不静默吞异常
      · `problems` —— 该出声的异常

    🔴 **为什么键是「卡」不是「clip」**（2026-10-24 `B2`）：clip 级真值表会把**别的卡**的字段
       算到这张卡头上。实测 `VO_Ultramarines_Ventris_gen4.ogg` 只有教程版 Ventris 的
       `generic2ChatSound` 指着它 ⇒ clip 级会把 `UM47` 的 `gen4` 行改写成 `gen2`（错）。
       卡级只看这张卡自己的字段 ⇒ 不会张冠李戴；`UM47` 的 `intro/gen1/gen3` 也因此
       不再被判成「两个 chat 字段共引」（共引来自教程版，那张卡不在我们卡表里）。

    匹配只看 `(our, name)`：同一张卡用**两个 chat 字段**指同一 clip 的情形实测 **0 条**
    （用两个字段指同一 clip 的 66 张督军是 `attackAudio` + `defaultChatSound`，跨战斗/chat 两族，
    不是同族冲突）；真出现同族冲突时调用方会**逐条出声**并不认。

    ⛔ **3 个战斗字段的字段名 → 事件名**从 `gen_card_vfx_by_card.AUDIO_SO_FIELD_EVENT` **import**
    （那是正本，见文件头）。"""
    problems, stats = [], {}
    own, card_truth = {}, {}

    # ---- 事件名表：3 个战斗字段（import 正本）+ 12 个 chat 字段（本文件）----
    combat = {}
    try:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import gen_card_vfx_by_card as _gv
        combat = dict(getattr(_gv, "AUDIO_SO_FIELD_EVENT", {}) or {})
    except Exception as e:                                  # noqa: BLE001
        problems.append(f"🔴 `import gen_card_vfx_by_card` 失败（{e}）⇒ **三个战斗字段的"
                        "「字段名 → 事件名」取不到**，那三条只能继续按文件名后缀推（会出声）")
    if not combat:
        problems.append("🔴 `gen_card_vfx_by_card.AUDIO_SO_FIELD_EVENT` 是空的 ⇒ "
                        "同上：`deploy`/`attack`/`death` 三条不进真值表")
    field_ev = dict(combat)
    field_ev.update(CHAT_SO_FIELD_EVENT)

    # ---- SO `uniqueId` → 我们卡 id（实测 1,127 对，覆盖 cards_engine 全部 1,127 张）----
    uid2our = load_uid_to_our(by_card)

    try:
        so_cards = json.load(io.open(CARD_SO_TRUTH, encoding="utf-8"))
    except Exception as e:                                  # noqa: BLE001
        problems.append(f"🔴 读不到卡 SO 本体 `{CARD_SO_TRUTH}`（{e}）"
                        "⇒ **归属①·B 整级失效**（12 个 chat 音只剩按名猜）")
        return own, card_truth, stats, problems
    # 🔴 当前 build 的 allcards 产物是 `{bundle,count,cards:[{…,full_raw}]}` ——
    #    卡 SO 的**全字段在 `full_raw`**（同一形状兼容见 `gen_card_vfx_by_card.py:1086`）。
    #    旧快照（`cards_1195_2022时代.json`）是**平的 list**，两种都收。
    if isinstance(so_cards, dict) and isinstance(so_cards.get("cards"), list):
        so_cards = [c.get("full_raw") or c for c in so_cards["cards"]]

    g2n = load_guid_name_index(by_card)
    for c in so_cards:
        uid = (c or {}).get("uniqueId")
        our = uid2our.get(uid)
        for f, ev in field_ev.items():
            g = ((c or {}).get(f) or {}).get("m_AssetGUID") or ""
            if not g:
                continue
            st = stats.setdefault(f, {"ref": 0, "ok": 0, "noguid": 0, "ev": ev})
            st["ref"] += 1
            name = g2n.get(g.lower())
            if not name or name not in audio_stems:
                st["noguid"] += 1
                continue
            st["ok"] += 1
            if our:
                card_truth.setdefault((our, name), {})[f] = ev
                # ⚠️ `own` **只收 12 个 chat 字段** —— 3 个战斗字段那一批已经被归属① 认走了
                #    （`card_vfx_by_card.json`），在这儿再收一遍会**同一条 clip 记两次**。
                if f in CHAT_SO_FIELD_EVENT:
                    if our not in own.setdefault(name, []):
                        own[name].append(our)
    return own, card_truth, stats, problems


def load_uid_to_our(by_card):
    """`原版 uniqueId → 我们卡 id` —— 走 `card_vfx_by_card.json._assetRefs[].origCardId`。

    实测 **1,127 对，覆盖 `cards_engine.json` 全部 1,127 张，缺 0**（`B2` 现读）。
    ⚠️ 同一个 uniqueId 指到两张我们卡的情形实测 **0** —— 出现时先到先得，但要出声（调用方对账）。
    """
    uid2our = {}
    for cid, v in (by_card or {}).items():
        for r in (v or {}).get("_assetRefs", []):
            o = r.get("origCardId")
            if o:
                uid2our.setdefault(o, cid)
    return uid2our


def load_index_identity_join(by_card):
    """🔴 **2026-10-24（`D5`）归属② 的两条身份级 join 路（`card_index` 卡名 → 我们卡 id）。**

    为什么需要：`card_index.json` 的 `cards[]` **没有 uniqueId 字段**（现读形状：
    `name/faction/factionId/type/art/sprite/altArts/voice`），而它的 `name` 是**本地化键口径**，
    与我们卡表（**卡面口径**）**词序/拼写不同** —— `Medic Scion` vs 我们 `Scion Medic`
    （`gen_cards_engine.py:242` 的注释「卡面印 Scion Medic」）· `Land Rider` vs `Land Raider` ·
    `Sisters Repentia` vs `Sister Repentia`。`load_engine_name_alias` 只收 `STAT_FIXES` 的**改名**，
    不处理这些 ⇒ 那一族只能掉到 ③ 按文件名猜（实测 38 张 `no_join`，`AM70` 就是这样被猜错的）。
    判据 → `资料/普查产出_1024/D4_孤儿deploy行.md` §六·2。

    **两条路都走原版卡 SO 自己的引用，不是猜、⛔ 也不是重排词序去凑名字：**

      · **路 A（uniqueId）** —— 卡 SO 的 `cardName`/`cardArmy` 与我们卡 id 的对应：
        `card_index.name` + `factionId`  ↦  `cards_raw.json` 的 `cardName` + `cardArmy`
        （**归一化后逐字相等** —— 两者本来就是**同一套本地化键**）
        ↦ 原版 `uniqueId` ↦ 我们卡 id（`load_uid_to_our`）。
        ⚠️ `card_index.factionId` 与 `cards_raw.cardArmy` 是**同一套编号**（现读 13 个阵营各 1 个值，
           `30=SaimHann` · `10=Ultramarines` · `100=AstraMilitarum` …）⇒ 用它把阵营也一起卡住。
      · **路 B（cardImageReference）** —— 路 A 对不上时（卡名拼写/词序不同）走这条：
        `card_index.art` 的**文件名 stem**（例 `AstraMilitarum_veh_Leman Russ`）
        ↦ 卡 SO 的 `cardImageReference` 解出的**资产名**（`card_vfx_by_card.json._assetRefs`
           里 `assetKind == "art"`、`soField == "cardImageReference"`，覆盖 1,127/1,127 张）
        ↦ 我们卡 id。**同一张卡的立绘资产名是唯一身份**，与卡叫什么无关 ⇒ 词序/拼写差异一次解决。
        实测：38 张 `no_join` 里 A 解决 19 张、B 再解决其余 19 张（**38/38**）。
        与「按名字 join 成功」的 564 张交叉校验：**只有 1 张冲突**
        （`EmperorsChildren / Lord Exultant`：名字指 `EC13`，立绘指 `EC56 Lord Kaphrael`）——
        所以 **B 只在 A 与名字都失败时才用**，且再过一道**阵营相等**校验（实测 38/38 阵营一致）。

    护栏（宁可认不出，不可认错）：
      · 路 A：同一个 `(factionId, 归一化卡名)` 在 1,351 张 SO 里对到**多个 uniqueId** ⇒ **整键不收**
        （实测 105 个键如此）；uniqueId 不在 `uid2our` 里 ⇒ 不收。
      · 路 B：同一个立绘资产名对到**多张卡** ⇒ 整键不收（实测 1 个：`SM_UM_strat_Righteous Fury`）；
        解出的卡**阵营与 index 那行不同** ⇒ 不收。

    返回 `(uid_join, art_join)`：
      `uid_join` = `{(factionId, 归一化卡名): 我们卡 id}` · `art_join` = `{立绘资产名: 我们卡 id}`。
      两者都是 `card_index` 那行**没有**的键，调用方用 index 行自己的 `factionId` / `art` 去查。"""
    uid2our = load_uid_to_our(by_card)

    # ---- 路 B：立绘 `cardImageReference` 资产名 ----
    # ⚠️ 这一段**只依赖 `card_vfx_by_card.json`**（不读 `cards_raw.json`）⇒ 放在最前，
    #    这样 `cards_raw.json` 缺失时路 B 仍然可用（两条路不该一起死）。
    art_sets = {}
    for cid, v in (by_card or {}).items():
        for r in (v or {}).get("_assetRefs", []):
            if r.get("assetKind") != "art" or not r.get("assetName"):
                continue
            art_sets.setdefault(r["assetName"], set()).add(cid)
    art_join, art_amb = {}, 0
    for name, cids in art_sets.items():
        if len(cids) != 1:               # 同一立绘被多张卡引用（实测 1 个）⇒ 判不了，整键不收
            art_amb += 1
            continue
        art_join[name] = next(iter(cids))
    if art_amb:
        print(f"  （② 路 B 立绘：**同一资产名对到多张卡** ⇒ 整键不收 {art_amb} 个）")

    try:
        so_cards = json.load(io.open(CARD_SO_TRUTH, encoding="utf-8"))
    except Exception as e:                                  # noqa: BLE001
        print(f"⚠️ 读不到卡 SO 本体 `{CARD_SO_TRUTH}`（{e}）—— ② 的**路 A（uniqueId）失效**"
              "（那批靠 uniqueId 的卡会继续掉到 ③ 按文件名猜；路 B 立绘仍然可用）")
        return {}, art_join
    if isinstance(so_cards, dict) and isinstance(so_cards.get("cards"), list):
        so_cards = [c.get("full_raw") or c for c in so_cards["cards"]]

    # ---- 路 A：uniqueId ----
    keys = {}          # (army, 归一化卡名) -> set(uniqueId)
    for c in so_cards:
        army = (c or {}).get("cardArmy")
        uid = (c or {}).get("uniqueId")
        if army is None or not uid:
            continue
        keys.setdefault((army, _norm((c or {}).get("cardName"))), set()).add(uid)
    uid_join, amb = {}, 0
    for k, uids in keys.items():
        if len(uids) != 1:               # 同名多张 SO（变体/异画）⇒ 判不了，整键不收
            amb += 1
            continue
        our = uid2our.get(next(iter(uids)))
        if our:
            uid_join[k] = our
    if amb:
        print(f"  （② 路 A uniqueId：**同名对到多个 uniqueId** ⇒ 整键不收 {amb} 个）")
    return uid_join, art_join


def art_key_of_index_row(card):
    """`card_index.json` 那行的 `art` 路径 → 立绘资产名（= 文件名去扩展名）。
    例：`01_卡牌\\Aeldari_灵族\\Texture2D\\Craftworld_SaimHann_inf_Autarch.png`
        → `Craftworld_SaimHann_inf_Autarch`。"""
    a = (card or {}).get("art") or ""
    return os.path.splitext(os.path.basename(a.replace("\\", "/")))[0]


def apply_event_truth(lines, card_truth):
    """🔴 **事件名回真值**（**卡级**）：`ev` 不再一律由文件名后缀推。

    判据 = **这张卡自己的 SO** 里，这个 clip 被哪些原版字段引用过：
      · 恰好 1 个字段 ⇒ 按它定 `ev`，并写 `so` = 字段名
      · 0 个          ⇒ **不动**（这张卡的 SO 没引用它 —— 保留文件名推的那个）
      · ≥2 个         ⇒ **不动**（同一张卡用两个字段指同一 clip ⇒ 判不了，宁可认不出）

    🔴 **2026-10-24（`B2`）从 clip 级改成卡级** —— 旧的 clip 级真值表会把**别的卡**的字段
       算进来：`VO_Ultramarines_Ventris_gen4.ogg` 是**教程版 Ventris**的 `generic2ChatSound`，
       旧口径于是把 `UM47` 那行 `gen4` 改写成 `gen2`（`gen7`→`gen3` 同理）。
       卡级之后这两行保持 `gen4`/`gen7`，且**不写 `so`**（UM47 自己的 SO 里没有这两个字段）。
       ⚠️ 判据仍与「宁可认不出，不可认错」一致：**没有这张卡自己的字段就不动它。**

    返回 `(ev 改了几条, so 写了几条, 同卡多字段没敢动几条)`。"""
    fixed = wrapped = amb = 0
    for cid, rows in lines.items():
        for r in rows:
            if r.get("so"):
                continue                      # 归属①·B 建的，本来就是真值
            stem = os.path.splitext(r["file"])[0]
            t = card_truth.get((cid, stem))
            if not t:
                continue                      # 这张卡的 SO 没引用它 —— 保留文件名推的 ev
            if len(t) > 1:
                amb += 1
                continue
            f, ev = next(iter(t.items()))
            if r["ev"] != ev:
                fixed += 1
            r["ev"] = ev
            r["so"] = f
            wrapped += 1
    return fixed, wrapped, amb


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    args = ap.parse_args()

    audio = load_audio_index()
    print(f"音频源：{len(audio)} 个文件（{len(set(os.path.basename(p) for p in audio.values()))} 个不同文件名）")

    index_cards = json.load(io.open(CARD_INDEX, encoding="utf-8"))["cards"]
    eng_cards = json.load(io.open(CARDS_ENGINE, encoding="utf-8"))["cards"]
    # join 键**两侧都 strip**：`card_index.json` 里有脏数据（`' Iron Priest'` 带**前导空格**，
    # 池里是 `Iron Priest`）⇒ 不去空格永远 join 不上，那张卡的语音静默丢。
    ekey = {(c["faction"].lower(), c["name"].strip()): c for c in eng_cards}
    # 外加**改名别名**：`card_index.json` 是**改名之前**建的，里面留的还是旧名
    #（`Land Rider` / `Sister Dogmata` / `Morkai Eliminator` …，见 `load_engine_name_alias`）。
    alias = load_engine_name_alias()

    # ---- ① 卡 SO（🔴 **权威**）：逐卡 `summonAudio` / `attackAudio` / `deathAudio` ----
    # 这一级是 2026-10-14（账 A1832）新加的：以前只有 ② 那一级，而 ② 的归属**本身就是错的**。
    # 顺序 = 「权威 → 兜底 → 猜」，冲突时**以卡 SO 为准**。
    lines = {}          # id -> [ {"ev","file","text",[src]} ]
    card_meta = {}      # id -> (name, faction)
    used_files = set()
    missing = []
    missing_so = []     # 归属① 引用了、音频源里却没有的（应该恒空；非空=上游产物脏了）
    no_join = []

    byid = {c["id"]: c for c in eng_cards}
    # 音频源按**去扩展名的 stem** 再建一份索引 —— 卡 SO 给的是 AudioClip **名**（不带扩展名）
    by_stem = {}
    for base in audio:
        by_stem.setdefault(os.path.splitext(base)[0], []).append(base)

    so_map, so_problems = load_card_so_audio()
    for p in so_problems:
        print(p)
    so_files = set()      # 被归属① 认走的**实际文件名**（② 必须跳过它们，否则归属又错回去）
    so_taken = so_shared = so_nocard = so_dupstem = 0

    for stem in sorted(so_map):
        bases = by_stem.get(stem, [])
        if not bases:
            missing_so.append(stem)
            continue
        if len(bases) > 1:                 # 同 stem 两个扩展名 —— 判不了，不猜
            so_dupstem += 1
            print(f"⚠️ 音频源里 stem 撞车、跳过：{stem} → {bases}")
            continue
        # 🔴 **2026-10-24（`D10(C4)`）：一个 clip 被多张卡的 SO 引用 ⇒ 每张卡都挂**。
        #   原版真事（现读 **1 条**）：`VO_UM_Primaris Intercessor - For the Imperium` 同时是
        #   `UM6`（Primaris Intercessor）与 **`EC57`（Xarahan, Daemon Prince）的 `summonAudio`**
        #   —— **原版自己抄错**（EC57 的卡 SO 指到了 UM 的登场语音），而本地有该 ogg。
        #   调度台 2026-10-24 裁定：**照原版播**（复刻口径优先，⛔ 不要「留空出声」）。
        #   ⚠️ 上一版这里是 `len(owners) != 1 ⇒ 跳过`（宁可认不出）—— 那条护栏防的是**按名字猜**
        #   造成的错配；而这一级的 owners **全部来自卡 SO 的 `_assetRefs`**（`load_card_so_audio`），
        #   不含任何猜测 ⇒ 多 owner 也照挂，这才等于原版的实际行为。
        #   仍不出声的只剩「同 stem 两个扩展名」与「卡不在我们卡表」。
        owners = so_map[stem]
        matched = [byid[oid] for oid in owners if oid in byid]
        if len(matched) < len(owners):     # SO 里有的卡不在我们的卡表里 —— 出声，不静默
            so_nocard += len(owners) - len(matched)
        if not matched:
            continue
        base = bases[0]
        ev, text = parse_name(base)
        if len(owners) > 1:
            so_shared += 1
            print("⚠️ 同一 clip 被多张卡的卡 SO 引用（照原版都挂）：%s → %s"
                  % (stem, [e["id"] for e in matched]))
        for eng in matched:
            cid = eng["id"]
            card_meta[cid] = (eng["name"], eng["faction"])
            lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text, "src": "cardSO"})
            so_files.add(base)
            used_files.add(base)
            so_taken += 1

    # ---- ①·B 卡 SO 的 12 个 `*ChatSound`（🔴 **权威**；2026-10-16 · 账 A1768 新增）----
    # 这一级**没进** `card_vfx_by_card.json`（那份只收 3 个战斗字段）⇒ 只能回 SO 本体读。
    # 冲突顺序不变：**卡 SO 说了算**；②/③ 认过的文件名一律让给它。
    by_card = _load_vfx_json()
    # ⚠️ 传**去扩展名的 stem 集合**：卡 SO 给的是 AudioClip **名**（不带扩展名）。
    chat_map, card_truth, chat_stats, chat_problems = load_card_so_audio_truth(
        by_card, set(by_stem))
    for p in chat_problems:
        print(p)
    so_chat_taken = so_chat_ambig = so_chat_nocard = so_chat_dupstem = 0
    for stem in sorted(chat_map):
        bases = by_stem.get(stem, [])
        if not bases:
            continue                       # 上游产物脏了才算异常，逐条在下面 chat_stats 里报
        if len(bases) > 1:
            so_chat_dupstem += 1
            print(f"⚠️ 音频源里 stem 撞车、跳过：{stem} → {bases}")
            continue
        base = bases[0]
        # 🔴 **2026-10-24（`B2`）按「卡」解析字段** —— 一个 clip 可能被**多张卡**引用
        #    （实测 `chat_map` 每条都只有 1 张我们卡；但**别的卡**（教程版 / 重复 SO）
        #    引用同一 clip 很常见 —— 那是 `card_truth` 键里**没有**它们的原因，
        #    也正是旧「clip 级」口径张冠李戴的来源）。多 owner 时**每张卡按自己的字段挂**，
        #    与归属① 的 `D10(C4)` 口径一致。
        for oid in chat_map[stem]:
            eng = byid.get(oid)
            if eng is None:
                so_chat_nocard += 1
                continue
            # 🔴 **这一条的字段与 `ev` 直接来自原版字段名**（⛔ 不是文件名后缀）——
            #    `so` = 那个字段名，留痕给自检 / 对账用（`VoiceLines.cs` 的 DTO 不声明它，会被忽略）。
            #    ⚠️ `attackAudio` 与 `defaultChatSound` **实测同 GUID**（66 张卡全同）
            #       ⇒ 这里只取 **chat 族**那一个（`defaultChatSound`），于是同一个 clip 在表里
            #       **出现两次**（一次 `attack`、一次 `default`）—— **这是原版事实**，不是重复导入。
            fields = [k for k in (card_truth.get((oid, stem)) or {})
                      if k in CHAT_SO_FIELD_EVENT]
            if len(fields) != 1:            # 同一张卡两个 chat 字段指同一 clip ⇒ 判不了就别塞
                so_chat_ambig += 1
                print("⚠️ 同一张卡的两个 chat 字段指同一 clip，跳过：%s → %s（%s）"
                      % (stem, oid, fields))
                continue
            f = fields[0]
            ev = CHAT_SO_FIELD_EVENT[f]
            card_meta[oid] = (eng["name"], eng["faction"])
            lines.setdefault(oid, []).append({"ev": ev, "file": base,
                                              "text": parse_name(base)[1], "src": "cardSO", "so": f})
            so_files.add(base)
            used_files.add(base)
            so_chat_taken += 1

    # ---- ② 按 card_index 的归属建表（**兜底**；① 认走的文件名一律跳过）----
    # 🔴 **2026-10-24（`D5`）**：`card_index` 的 `name` 是**本地化键口径**，与我们卡表（卡面口径）
    #   **词序不同**（`Medic Scion` vs `Scion Medic`）⇒ 按名字 join 不上的那批，改走
    #   **name+factionId → 原版 SO cardName+cardArmy → uniqueId → 我们卡 id**
    #   （`load_index_uid_join`；⛔ 不是拿词序去凑）。判据 → `资料/普查产出_1024/D5_语音归属两处.md`。
    uid_join, art_join = load_index_identity_join(by_card)
    uid_joined = []       # 走 uniqueId（路 A）并上的 (faction, 原名, 我们 id)
    art_joined = []       # 走立绘 cardImageReference（路 B）并上的 (faction, 原名, 我们 id)
    dead_index = []       # 🔴 D4 判据在 ② 级丢弃的行（同一张卡 SO 已经不播它）
    dead_files = set()    # 已被 ② 判死的文件名 —— ③ 别再把它当孤儿重算一遍（否则重复打印）
    overridden = []
    for c in index_cards:
        if not c.get("voice"):
            continue
        nm = (c["name"] or "").strip()
        eng = ekey.get((c["faction"].lower(), nm))
        if eng is None and nm.lower() in alias:
            eng = ekey.get((c["faction"].lower(), alias[nm.lower()]))
        if eng is None:
            our = uid_join.get((c.get("factionId"), _norm(nm)))
            eng = byid.get(our) if our is not None else None
            if eng is not None:
                uid_joined.append((c["faction"], nm, eng["id"]))
        if eng is None:
            # 路 B：立绘资产名（`cardImageReference`）。⚠️ 必须**阵营相等**才算 —— 实测
            #   `EmperorsChildren / Lord Exultant` 的立绘指 `EC56`（Lord Kaphrael 那条 warlord 图），
            #   而名字指 `EC13` ⇒ 这一路只在名字与 uniqueId 都失败时才用，且阵营必须对得上。
            our = art_join.get(art_key_of_index_row(c))
            cand = byid.get(our) if our is not None else None
            if cand is not None and cand["faction"].lower() == c["faction"].lower():
                eng = cand
                art_joined.append((c["faction"], nm, eng["id"]))
        if eng is None:
            no_join.append((c["faction"], nm))
            continue
        cid = eng["id"]
        card_meta[cid] = (eng["name"], eng["faction"])
        for v in c["voice"]:
            base = os.path.basename(v.replace("\\", "/"))
            if base in so_files:
                # 🔴 **归属① / ①·B 说了算**：index 把这条记在**别的卡**名下（实测 34 处），
                #    或记对了（同卡）—— 两种都跳过：上面那两级已经把它挂到正确的卡上了。
                so_owner = so_map.get(os.path.splitext(base)[0]) or chat_map.get(os.path.splitext(base)[0])
                if so_owner and cid not in so_owner:
                    overridden.append((base, cid, so_owner[0]))
                continue
            if base not in audio:
                missing.append(base)
                continue
            ev, text = parse_name(base)
            # 🔴 2026-10-24（`D5`）：② 这一级是**兜底**（不是卡 SO），所以同样受
            #   `orphan_is_dead` 的「原版根本不会播」判据管 —— 否则 `uniqueId` 并上来的卡
            #   会把 D4 已经剔掉的猜行**从 ② 绕回来**（实测 `AM70` 的 `No soul left behind`）。
            why = orphan_is_dead(base, cid, ev, lines)
            if why:
                dead_index.append((base, cid, ev, why))
                dead_files.add(base)      # ③ 别再把它当孤儿重算一遍
                print(f"🗑 丢弃 index 兜底行（原版不会播）：{base} → {cid} [{ev}] · {why}")
                continue
            lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text})
            used_files.add(base)

    # ---- ③ 孤儿：前两级都没收录、但文件名能对上我们某张卡的（**这一步是猜的，标出来**）----
    # 判据在 `match_owner`（2026-09-18 重写）：归一化 + 整词边界 + 唯一性护栏。
    tables = build_name_tables(eng_cards)
    orphans = []
    dead_orphans = []     # 🔴 2026-10-24（`D4`）：③ 猜到卡、但**原版不会播**的（判据见 `orphan_is_dead`）
    for base, path in sorted(audio.items()):
        if base in used_files or base in dead_files:
            continue
        hit = match_owner(os.path.splitext(base)[0], tables)
        if hit is None:
            orphans.append(base)
            continue
        ev, text = parse_name(base)
        cid = hit["id"]
        why = orphan_is_dead(base, cid, ev, lines)
        if why:
            dead_orphans.append((base, cid, ev, why))
            print(f"🗑 丢弃孤儿行（原版不会播）：{base} → {cid} [{ev}] · {why}")
            continue                       # ⛔ 不进表、不算 used（音频留在 Resources 里不删，见报告）
        card_meta.setdefault(cid, (hit["name"], hit["faction"]))
        lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text, "src": "orphan"})
        used_files.add(base)

    # ---- ③·D 🔴 **2026-10-24（`D9`）：按开场白表的权威归属改判 ②/③ 的行** ----
    #   `card_chat_intro.json` 的 `introByOpponent/introByArmy[*].soundAsset` 来自原版卡 SO 的
    #   `customIntroChatData[].customSound` ⇒ 它说这条 clip 是哪张卡的开场白，就是哪张卡。
    #   现状（改前）**4 条**被 `card_index.json` 挂错主人（判据 → `D9` 报告）：
    #     · `VO_BlackLegion_Abaddon_vsGrukk/_vsHexscorn/_vsOrikan`：`BL5` → **`BL34`**
    #       （`BL5` 的 5 条 `_vs_` 孤儿因此降到 2 条：`vsCalgar` / `vsMinka`）；
    #     · `VO_BL_Ghallaron_gen6`：`BL67` → **`BL65`**（死通道，不改运行时行为）。
    #   ⚠️ **只改判「没有卡 SO 佐证」的行**（无 `so`、`src != cardSO`）——
    #      原版 15 个字段指名的行是真值，⛔ 一律不动（那条规矩在 `apply_event_truth` / ①/①·B）。
    #   ⚠️ 放在 `apply_event_truth` **之前**：改对主人之后，事件名回真值才按**正确那张卡**的 SO 算。
    chat_owner, chat_owner_problems = load_chat_intro_owner()
    for p in chat_owner_problems:
        print(p)
    owner_fixed = []
    if chat_owner:
        for cid in list(lines.keys()):
            keep = []
            for r in lines[cid]:
                if r.get("so") or r.get("src") == "cardSO":
                    keep.append(r)                     # 卡 SO 指名 —— 真值，不动
                    continue
                right = chat_owner.get(os.path.splitext(r["file"])[0])
                if right and right != cid and right in byid:
                    lines.setdefault(right, []).append(r)
                    card_meta.setdefault(right, (byid[right]["name"], byid[right]["faction"]))
                    used_files.add(r["file"])
                    owner_fixed.append((r["file"], cid, right))
                    continue
                keep.append(r)
            lines[cid] = keep
        for cid in [k for k, rows in lines.items() if not rows]:
            del lines[cid]                             # 改判之后变空的卡不留空壳
    for base, wrong, right in owner_fixed:
        print(f"🔴 按开场白权威改判归属（③·D）：{base}：{wrong} → {right}")

    # ---- ③·B 🔴 **事件名回真值**（2026-10-16 · 账 A1768；2026-10-24 `B2` 改卡级）----
    # 归属定下来之后统一校正一轮：`ev` 以前一律由文件名后缀推，现在只要**这张卡自己的 SO**
    # 里那个 clip 被**恰好一个**原版字段引用过，就按那个字段定（并写 `so` = 字段名）。
    # ⚠️ 必须放在 ④/⑤ 之前 —— 写盘的是校正后的表。
    ev_fixed, ev_so, ev_ambig = apply_event_truth(lines, card_truth)

    # ---- ③·C 🔴 2026-10-24（`D7`）：`gen4..gen7` 死通道 + `_vs_` 记法订正后的归属标记 ----
    #   · `gen4..gen7`（判据 → `C5` §5）：原版 `RawCardScript` 没有 `generic4..7` 字段，
    #     `ChatMessage` 11 个值（0..10）只映射到 `gen1..3`（Taunt/Sorry/Oops）⇒ `voice_lines.json`
    #     里 `ev=gen4..7` 的行**永远选不中**（`ForChatButton` 只请求 `gen1..gen3`，`TryPick` 精确匹配）。
    #     数据**保留**（留证据），只把归属标成 `orphan`（= 按文件名猜的、未经卡 SO 佐证）。
    #   · `intro-vs`（判据 → `C5` §4）：`parse_name` 把 `_vs_` 对手台词从 `deploy` 挪出来之后，
    #     它们同样**没有卡 SO 佐证**（`customIntroChatData` 是对象数组、不在这 15 个 GUID 字段里）
    #     ⇒ 一并标 `src=orphan`（表达「未经 SO 佐证」，⛔ 不改变任何运行时行为）。
    #   ⚠️ 必须放在 `apply_event_truth` **之后** —— 被卡 SO 指到的那些（`ASH2 gen1→_gen4` ·
    #     `UM88 gen3→_gen4` · 教程 Uriel）已经在上面把 `ev` 改成真值并写了 `so`，不该再标。
    dead_ev_marked = intro_vs_marked = 0
    for rows in lines.values():
        for r in rows:
            if r.get("so") or r.get("src") == "cardSO":
                continue                     # 卡 SO 指名的（现读 `gen4..7` 0 条）—— 真值，不动
            ev = r.get("ev")
            if ev not in DEAD_EVENTS and ev != "intro-vs":
                continue
            if r.get("src") != "orphan":
                r["src"] = "orphan"
                if ev == "intro-vs":
                    intro_vs_marked += 1
                else:
                    dead_ev_marked += 1

    # ---- ④ 拷贝音频 ----
    copied = skipped = 0
    if not args.check:
        os.makedirs(DST_AUDIO, exist_ok=True)
        for base in sorted(used_files):
            src, dst = audio[base], os.path.join(DST_AUDIO, base)
            if os.path.exists(dst) and os.path.getsize(dst) == os.path.getsize(src):
                skipped += 1
                continue
            shutil.copy2(src, dst)
            copied += 1

    # ---- ⑤ 写映射 ----
    # ⚠️ `cards` 是**数组不是字典** —— Unity 的 `JsonUtility` 反序列化不了字典
    #    （`VoiceLines.cs` 直接吃这个形状；要用字典就得再引一个 JSON 库，不值当）。
    # ⚠️ `lines[].src` 只有**不是兜底②**时才写（`"cardSO"` / `"orphan"`）——
    #    与 2026-09-18 起 `orphan` 那条同一规矩；`VoiceLines.cs` 的 DTO 不声明它，`JsonUtility` 会忽略。
    out = {
        "version": 2,
        "note": "原版单位语音表。id = 我们卡表的 id（cards_engine.json）；"
                "file = Resources/Art/audio/vo/<名>；"
                "ev = 事件名（**按原版字段名定**，见 lines[].so）；`wp` = 原版 `wellPlayedChatSound`。"
                "归属三级：卡 SO（权威，lines[].src=cardSO —— 含 summon/attack/death 三个战斗字段"
                "与 12 个 *ChatSound）→ card_index.json（兜底，无 src；卡名与我们卡表不同的那批走两条"
                "身份级 join：① name+factionId → 原版 SO cardName+cardArmy → uniqueId → 我们卡 id；"
                "② ① 对不上时 art 资产名 → 卡 SO 的 cardImageReference 资产名 → 我们卡 id）"
                "→ 按文件名猜（src=orphan；`_vs_` 对手台词**只认 `_vs_` 之前那一段当主人**；"
                "命中取**词数最多**的那个卡名（全名/首段短名两张表一起比），词数相同且指向不同卡 ⇒"
                "**同名多义、不认**）；"
                "猜出来的行里**原版不会播的已丢弃**（文件名带 `VO_NOT USED_` · 同卡同事件已有卡 SO 指名，"
                "判据见 `工具/import_original_audio.py` 的 `orphan_is_dead`）。"
                "🔴 2026-10-24（`D9`）：②/③ 挂错主人的开场白 clip 已按 `Resources/card_chat_intro.json` 的"
                "**权威归属**（原版卡 SO 的 `customIntroChatData[].customSound`）改判 —— 现读 4 条"
                "（`BL5`→`BL34` ×3 对手开场白 · `BL67`→`BL65` ×1 `gen6`）；`src`/`so` 留痕不变，"
                "判据 `资料/普查产出_1024/D9_开场白兜底顺序与脏标.md`。"
                "🔴 2026-10-24（`D7`）：`_vs_` 对手台词（`<主人>_vs_<对手>`）**不再记 `deploy`** —— "
                "它们记成 `ev=\"intro-vs\"`（权威通道 = `Resources/card_chat_intro.json` 的开场白 Intro，"
                "`VoiceLines` 永不选这个 ev；判据 `C5` §4）；原版**没有**的 `gen4..gen7` 通道那批行**保留**"
                "（留证据）但标 `src=\"orphan\"`（死数据，判据 `C5` §5）。"
                "`lines[].so` = 该条音频在卡 SO 上的**原版字段名**（没佐证的条目不写）；"
                "`lines[].text` 只作气泡文字用。",
        "source": {
            "audio": "d:/2/新解包资源/assets_full/bundle_*cardassets*_assets_all/AudioClip/",
            "owner": "① 卡 SO 的 summonAudio/attackAudio/deathAudio（经 "
                     "d:/4/Unity/数据/游戏数据/card_vfx_by_card.json 的 byCard[*]._assetRefs）"
                     "**+ 12 个 *ChatSound**（经 SO 本体 "
                     "d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
                     "（当前 build 1,351 张 RawCardScript，卡 SO 全字段在 full_raw），GUID → 资产名走 "
                     "card_vfx_by_card.json._assetRefs ∪ 资料/普查产出_1018/cardanim_guid_map/"
                     "guid_map_bundles.tsv ∪ 数据/索引/anim_address_map.json 的 guid_to_asset）**权威**；"
                     "② d:/4/Unity/数据/索引/card_index.json 的 cards[].voice **兜底**",
            "script": "工具/import_original_audio.py",
        },
        "cards": [
            {"id": cid, "name": card_meta[cid][0], "faction": card_meta[cid][1], "lines": rows}
            for cid, rows in sorted(lines.items())
        ],
    }
    if not args.check:
        # 🔴 2026-10-23：**缩进必须是 2**，与仓库里那一版一致。
        #    原来这里是 `indent=1` ⇒ 每次重跑都把 16k 行整篇重排（`git diff --numstat` 16k/16k），
        #    真正的内容变化（几百行）全被格式噪声埋掉。改 2 之后 diff 只剩内容。
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(out, ensure_ascii=False, indent=2))

    total_lines = sum(len(v) for v in lines.values())
    evs = {}
    for rows in lines.values():
        for r in rows:
            evs[r["ev"]] = evs.get(r["ev"], 0) + 1
    n_so = sum(1 for v in lines.values() for r in v if r.get("src") == "cardSO")
    n_orph = sum(1 for v in lines.values() for r in v if r.get("src") == "orphan")
    n_introvs = sum(1 for v in lines.values() for r in v if r.get("ev") == "intro-vs")
    n_gen4to7 = sum(1 for v in lines.values() for r in v if r.get("ev") in DEAD_EVENTS)
    # 「按名字猜」的**原义**（`src=orphan` 且不是 `D7` 那两批）—— 单独报，别让两个增量混进来
    n_orph_other = sum(1 for v in lines.values() for r in v
                       if r.get("src") == "orphan"
                       and r.get("ev") not in DEAD_EVENTS and r.get("ev") != "intro-vs")
    print("=" * 78)
    print("卡 SO（归属① · 权威）：引用 {0} 条 / 唯一文件名 {1} 个 / 卡 {2} 张".format(
        sum(len(v) for v in so_map.values()), len(so_map),
        len({x for v in so_map.values() for x in v})))
    print(f"  并进我们卡表 {so_taken} 条 · "
          f"**多卡共用同一 clip（照原版都挂）** {so_shared} 条 · **卡不在我们卡表** {so_nocard} 条 · "
          f"**同 stem 两个扩展名** {so_dupstem} 条 · **音频源里没有** {len(missing_so)} 条")
    for s in missing_so:                  # 应该恒空；非空 = 上游产物脏了，逐条复核
        print(f"    ❌ 卡 SO 引用但音频源里没有：{s}")
    print(f"card_index（归属② · 兜底）：被归属①/①·B **改判** {len(overridden)} 条"
          f"（index 记在别的卡名下 → 卡 SO 说了算）")
    print(f"  🔴 其中 **{len(uid_joined)} 张是走 uniqueId（路 A）并上的**（卡名词序与我们卡表不同，"
          f"按名字 join 不上；判据 `load_index_identity_join`）：")
    for fac, nm, cid in uid_joined:
        print(f"    · {fac} / {nm} → {cid}")
    print(f"  🔴 另外 **{len(art_joined)} 张是走立绘 cardImageReference（路 B）并上的**"
          f"（卡名拼写/词序与我们卡表不同，路 A 也对不上 —— ⛔ 不是模糊匹配）：")
    for fac, nm, cid in art_joined:
        print(f"    · {fac} / {nm} → {cid}")
    if not uid_joined and not art_joined:
        print("    （0 张）")
    for base, wrong, right in overridden[:12]:
        print(f"    · {base}：index={wrong} → cardSO={right}")
    if len(overridden) > 12:
        print(f"    …（共 {len(overridden)} 条，全表见 voice_lines.json 的 src=cardSO 行）")
    print("=" * 78)
    print("🆕 卡 SO 的 15 个音频字段（归属① / ①·B · 权威）：逐字段引用 / 已解 / 未解")
    for f in sorted(chat_stats, key=lambda k: -chat_stats[k]["ref"]):
        st = chat_stats[f]
        print(f"    {f:<24} 事件 `{st['ev']:<8}` 引用 {st['ref']:>3} · "
              f"已解 {st['ok']:>3} · **GUID 或音频源里没有** {st['noguid']}")
    # ⚠️ 2026-10-24（`B2`）口径再更正：`so_chat_ambig` 现在只剩**一种**情形 ——
    #   **同一张卡的两个 chat 字段指同一 clip**（现读 **0**）。旧口径里那 3 条
    #   （`VO_Ultramarines_Ventris_{intro,gen1,gen3}`）是**教程版 Ventris 与我们共引**造成的
    #   伪冲突，卡级解析之后已正常落位（判据 → `资料/普查产出_1024/B2_语音归属换真值SO.md`）。
    print(f"  并进我们卡表 {so_chat_taken} 条 · **同一张卡两 chat 字段共指、不认** {so_chat_ambig} 条 · "
          f"**卡不在我们卡表** {so_chat_nocard} 条 · **同 stem 两个扩展名** {so_chat_dupstem} 条")
    print(f"🔴 事件名回真值（卡级；`ev` 不再一律按文件名后缀推）：改正 {ev_fixed} 条 · "
          f"另标上 `so`（**真的是这张卡自己的字段**）{ev_so} 条 · "
          f"**这张卡的两字段指同一 clip、没敢动** {ev_ambig} 条")
    print("=" * 78)
    print(f"卡片：有语音 {len(lines)} 张 · 台词 {total_lines} 条"
          f"（其中 归属① {n_so} 条 · 标 `src=orphan` {n_orph} 条 = 按名字猜 {n_orph_other} 条"
          f" + 死通道 gen4..7 {n_gen4to7} 条 + `intro-vs` {n_introvs} 条）")
    print(f"🔴 `_vs_` 记法订正（`D7`）：`ev=intro-vs` {n_introvs} 条（**不进 `deploy` 桶**；"
          f"本轮新标 `src=orphan` {intro_vs_marked} 条；"
          f"权威通道 = `card_chat_intro.json` 的开场白 Intro，判据 `C5` §4）")
    print(f"🔴 死通道 `gen4..gen7`（`D7`）：{n_gen4to7} 条**保留**、全部标 `src=orphan`"
          f"（本轮新标 {dead_ev_marked} 条；其余 {n_gen4to7 - dead_ev_marked} 条本来就是 orphan；"
          f"判据 `C5` §5 —— 原版无此通道，`VoiceLines` 永不选它）")
    print(f"🔴 按开场白权威改判归属（`D9` · ③·D · 判据 `card_chat_intro.json`）：共 {len(owner_fixed)} 条"
          f"（②/③ 挂错主人的；`src=cardSO` 的行一律不动）")
    print(f"对不上卡表的 index 卡：{len(no_join)} 张 {no_join[:6]}")
    print(f"音频源里找不到的引用：{len(missing)} 条 {missing[:3]}")
    print(f"孤儿（按文件名并进我们卡表的）：{n_orph} 条；**仍未并上的 {len(orphans)} 条**：")
    for b in orphans:                     # 全列出来 —— 残差要能逐条复核，不能只报个数
        print(f"    {b}")
    print(f"🔴 孤儿里**原版不会播、已丢弃**：{len(dead_orphans)} 条" +
          ("" if dead_orphans else "（判据见 `orphan_is_dead`）"))
    for base, cid, ev, why in dead_orphans:
        print(f"    🗑 {base} → {cid} [{ev}] · {why}")
    print(f"🔴 index 兜底（②）里**原版不会播、已丢弃**：{len(dead_index)} 条（同一判据，见 `orphan_is_dead`）")
    for base, cid, ev, why in dead_index:
        print(f"    🗑 {base} → {cid} [{ev}] · {why}")
    print("事件分布：", dict(sorted(evs.items(), key=lambda kv: -kv[1])))
    if args.check:
        print("（--check：没有写盘）")
    else:
        print(f"已拷 {copied} 条 · 已存在跳过 {skipped} 条 → {DST_AUDIO}")
        print(f"映射已写：{DST_JSON}")


if __name__ == "__main__":
    main()
