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
          ⚠️ **为什么读产物、不重解 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`**：
          那条链（SO 字段 → GUID → `assetName`）**只有一处正本** = `工具/gen_card_vfx_by_card.py`
          （`CARD_SO_TRUTH`），在这儿再实现一遍就是「同一条规则两处写」（本仓红线）。
  · 归属② **兜底** = `d:/4/Unity/数据/索引/card_index.json` 的 `cards[].voice`
          （602 张卡 / 1778 条引用；实测 **1778/1778 在音频源里都找得到**）
  · 归属③ **兜底之后再猜** = 前两者都没收录的孤儿，按文件名比卡名（`match_owner`，标 `src=orphan`）
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
       `BL4`「Chaos Sergeant」→ 记在 `BL46`「Gifts of Chaos」· `DA34` → `DA83` ·
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
     `card_vfx_by_card.json`、也不在 `guid_map.tsv`** 里，只在 SO 本体
     （`cards_1195_2022时代.json`：`defaultChatSound` 59 / `introChatSound` 59 /
     `mirrorChatSound` 59 / `concedeChatSound` 59 / `hurryChatSound` 57 /
     `iCantDoThatChatSound` 58 / `greetChatSound` 59 / `threatChatSound` 59 /
     `wellPlayedChatSound` 59 / `generic1..3ChatSound` 59/59/59，卡数口径）。
   · GUID → 资产名：**并集**取 `card_vfx_by_card.json` 的 `_assetRefs`（带 `guid`+`assetName`）
     与 `数据/索引/anim_address_map.json` 的 `guid_to_asset`（10,325 条）—— 实测只剩
     1–2 个 GUID 两边都没有（EC55 `Lucius the Eternal` / EC56 `Lord Kaphrael` 整套，
     以及 `BL47` 的 summon、`GOF2` 的 death），**逐条打在运行输出里**，⛔ 不静默。
   · SO `uniqueId` → 我们卡 id：走 `card_vfx_by_card.json` 的 `_assetRefs[].origCardId`
     （实测 1,009 对，且**逐条 `origCardId == byCard 的键`**）。
   · ⛔ **3 个战斗字段的「字段名 → 事件名」表不在这里重写** —— 那是
     `gen_card_vfx_by_card.py:195` `AUDIO_SO_FIELD_EVENT` 的正本，本脚本 `import` 它。

**事件名回真值**（`ev` 那一路）：以前 `ev` 一律由**文件名后缀**推（`parse_name`）。
   现在多一道**校正**：任何一条 clip，只要它在**卡 SO 全池（1,195 张，不分我们卡表）**里
   被**恰好一个**原版字段引用过，就按那个字段定 `ev`，并带上 `so` = 原版字段名。
   · 校正表按 **clip** 建（不是按卡）⇒ `card_index` / 孤儿那两级沾的错事件名也一起被纠正。
   · 一个 clip 被**多个**字段引用（如 `attackAudio` 与 `defaultChatSound` 实测**同 GUID**，
     59 张卡全同）⇒ **认不出来就不动它**，保留文件名推的那个（宁可认不出，不可认错）。
   · ⇒ 因此 `line` 这个**我们自己造的名字退场**，改叫原版的 `summonAudio` 的译名 `deploy`
     （译名同 `AUDIO_SO_FIELD_EVENT`，**只此一处**）。判据：出场台词那一族
     `VO_<阵营>_<卡名> - <台词>.ogg` 实测 **448/536 条**就是 `summonAudio` 的落点。

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
CARD_SO_TRUTH = "d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json"
# GUID → 资产名 的第二来源（第一来源 = `card_vfx_by_card.json` 的 `_assetRefs`，两处**取并集**）
ANIM_ADDRESS_MAP = "d:/4/Unity/数据/索引/anim_address_map.json"
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

# 🔴 **卡 SO 的 12 个 `*ChatSound` → 我们的事件名**（与 `VoiceLines.cs` 那一族逐字一致）。
#    判据（**两条独立证据，都现核过**）：
#      ① `RawCardScript__GetChatSound.c` 的 `switch(chatMsgIndex)` 给出**枚举号 → 字段偏移**：
#         0→0x190 intro · 1→0x198 mirror · 2→0x1A8 hurry · 3→0x1B0 iCantDoThat ·
#         4→0x1A0 concede · 5→0x1B8 greet · 6→0x1C0 threat · 7→0x1D0 wellPlayed ·
#         8→0x1C8 generic1 · 9→0x1D8 generic2 · 10→0x1E0 generic3；
#         `default:` 那支取不到时落回 `+0x188` = `defaultChatSound`。
#      ② `dump.cs:23300-23315` 的**字段名 ↔ 偏移**逐行对得上上面那组偏移。
#      ③ 枚举名 = `dump.cs:23189-23207`（`ChatMessage`），与 `资料/语音线_原版规格与ASR管道.md` §1.1 同表。
#    ⛔ `generic4..7` 在字段表里**不存在**（类在 0x1E0 结束）⇒ 不认，别把它当第 13–16 个字段。
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
       这条是**推断**：`so` 字段为空即表示「没佐证」。"""
    stem = os.path.splitext(base)[0]
    if " - " in stem:
        text = stem.split(" - ", 1)[1].strip()
        # 去掉结尾的配音演员名（`(Vivienne)` 这种）
        text = re.sub(r"\s*\([^)]*\)\s*$", "", text).strip()
        return "deploy", text
    # 没有 " - " 的：看尾段是不是事件
    tail = stem.rsplit("_", 1)[-1] if "_" in stem else ""
    ev = norm_event(tail)
    return (ev, "") if ev else ("deploy", "")


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
    判据：**整词**匹配（两侧补空格再找），先全名、后首段短名；命中**必须恰好一张卡**。"""
    header = " " + _norm(stem.split(" - ", 1)[0]) + " "
    for tbl in tables:
        hit = None
        for key, cards in tbl.items():
            if " " + key + " " not in header:
                continue
            if hit is not None and hit is not cards[0]:
                return None          # 撞车 ⇒ 不认（宁可认不出）
            hit = cards[0]
        if hit is not None:
            return hit
    return None


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
    值是 **list** —— 同一个 `AudioClip` 可能被**两张卡**引用（实测 2 处：
    `VO_BL_Terminator Champion - …` = `BL29`+`BL_Unhallowed_Gifts` ·
    `VO_UM_Primaris Intercessor - …` = `UM6`+`EC57`）⇒ 调用方按「唯一才认」处置
    （项目原则：**宁可认不出，不可认错**）。

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
    """`{guid 小写: 资产名}` —— **两个现成产物的并集**，本脚本**不自己解 GUID**。

      · `card_vfx_by_card.json` 的 `_assetRefs[*]`（带 `guid` + `assetName`）——
        这一路覆盖 `guid_map.tsv` 解出来的那批（实测比下面那份多 109 个）
      · `数据/索引/anim_address_map.json` 的 `guid_to_asset`（10,325 条）

    两路都解不出 = 真的没有（实测只剩 EC55/EC56 整套 + 2 条），调用方**逐条出声**。"""
    idx = {}
    for _cid, v in (by_card or {}).items():
        for r in (v or {}).get("_assetRefs", []):
            g = (r.get("guid") or "").lower()
            n = r.get("assetName")
            if g and n:
                idx.setdefault(g, n)
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
    """🔴 **归属①·B + 事件名真值表**。

    返回 `(own, truth, own_field, stats, problems)`：
      · `own`   —— `{clip stem: [我们卡 id, ...]}`，**只收 12 个 chat 字段 + 能 join 上我们卡表的卡**（归属①·B）
      · `truth` —— `{clip stem: {原版字段名: 我们的事件名}}`，**全池**（1,195 张，不分我们卡表）
                   —— 它给「事件名回真值」用（见文件头）
      · `own_field` —— `{(我们卡 id, clip stem): 原版字段名}`，**15 个字段全收** —— 它给
                   `apply_event_truth` 判「这条真的是这张卡自己的字段吗」（⛔ 别拿别张卡的字段当它的）
      · `stats` —— 逐字段的引用数 / 已解 / 未解，⛔ 不静默吞异常
      · `problems` —— 该出声的异常

    ⛔ **3 个战斗字段的字段名 → 事件名**从 `gen_card_vfx_by_card.AUDIO_SO_FIELD_EVENT` **import**
    （那是正本，见文件头）。"""
    problems, stats = [], {}
    own, truth, own_field = {}, {}, {}

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

    # ---- SO `uniqueId` → 我们卡 id（实测 1,009 对；`origCardId` **逐条等于** byCard 的键）----
    uid2our = {}
    for cid, v in (by_card or {}).items():
        for r in (v or {}).get("_assetRefs", []):
            o = r.get("origCardId")
            if o:
                uid2our.setdefault(o, cid)

    try:
        so_cards = json.load(io.open(CARD_SO_TRUTH, encoding="utf-8"))
    except Exception as e:                                  # noqa: BLE001
        problems.append(f"🔴 读不到卡 SO 本体 `{CARD_SO_TRUTH}`（{e}）"
                        "⇒ **归属①·B 整级失效**（12 个 chat 音只剩按名猜）")
        return own, truth, own_field, stats, problems

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
            truth.setdefault(name, {}).setdefault(f, ev)
            if our:
                own_field.setdefault((our, name), f)
            # ⚠️ `own` **只收 12 个 chat 字段** —— 3 个战斗字段那一批已经被归属① 认走了
            #    （`card_vfx_by_card.json`），在这儿再收一遍会**同一条 clip 记两次**。
            if our and f in CHAT_SO_FIELD_EVENT:
                if our not in own.setdefault(name, []):
                    own[name].append(our)
    return own, truth, own_field, stats, problems


def apply_event_truth(lines, truth, own_field):
    """🔴 **事件名回真值**：`ev` 不再一律由文件名后缀推。

    判据 = **卡 SO 全池**里这个 clip 被哪些原版字段引用过：
      · 恰好 1 个字段 ⇒ 按它定 `ev`
      · 0 个 / ≥2 个     ⇒ **不动**（认不出来就保留文件名推的那个 —— 宁可认不出，不可认错）

    🔴 **`so` 只在「这条真的是这张卡自己的字段」时才写** —— `truth` 是 **clip 级**的，
       而 `so` 是 **卡级**的声称。实测踩到过：`VO_Ultramarines_Ventris_gen4.ogg` 是
       **另一个条目**（uid 是裸 GUID 的 `Uriel Ventris (tutorial)`）的 `generic2ChatSound`，
       若照 clip 级往 `UM47` 头上写 `so=generic2ChatSound` 就是**张冠李戴**（复核代理会当场抓到）。

    返回 `(ev 改了几条, so 写了几条, 多字段共用没敢动几条)`。"""
    fixed = wrapped = amb = 0
    for cid, rows in lines.items():
        for r in rows:
            if r.get("so"):
                continue                      # 归属①·B 建的，本来就是真值
            stem = os.path.splitext(r["file"])[0]
            t = truth.get(stem)
            if not t:
                continue
            if len(t) > 1:
                amb += 1
                continue
            f, ev = next(iter(t.items()))
            if r["ev"] != ev:
                fixed += 1
            r["ev"] = ev
            if own_field.get((cid, stem)) == f:
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
    so_taken = so_ambig = so_nocard = so_dupstem = 0

    for stem in sorted(so_map):
        bases = by_stem.get(stem, [])
        if not bases:
            missing_so.append(stem)
            continue
        if len(bases) > 1:                 # 同 stem 两个扩展名 —— 判不了，不猜
            so_dupstem += 1
            print(f"⚠️ 音频源里 stem 撞车、跳过：{stem} → {bases}")
            continue
        owners = so_map[stem]
        if len(owners) != 1:               # 两张卡引用同一条 clip ⇒ 不认（宁可认不出）
            so_ambig += 1
            continue
        eng = byid.get(owners[0])
        if eng is None:                    # SO 里有一张卡不在我们的卡表里 —— 出声，不静默
            so_nocard += 1
            continue
        cid = eng["id"]
        base = bases[0]
        ev, text = parse_name(base)
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
    chat_map, clip_truth, clip_own_field, chat_stats, chat_problems = load_card_so_audio_truth(
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
        owners = chat_map[stem]
        if len(owners) != 1:               # 两张卡引用同一条 clip ⇒ 不认（宁可认不出）
            so_chat_ambig += 1
            continue
        eng = byid.get(owners[0])
        if eng is None:
            so_chat_nocard += 1
            continue
        cid = eng["id"]
        base = bases[0]
        # 🔴 **这一条的 `ev` 直接来自原版字段名**（⛔ 不是文件名后缀）——
        #    `so` = 那个字段名，留痕给自检 / 对账用（`VoiceLines.cs` 的 DTO 不声明它，会被忽略）。
        #    ⚠️ `own` 只由 chat 字段建；但一个 clip 可能被**两个 chat 字段**引用
        #       （实测 `introChatSound`+`mirrorChatSound` 这类 3 处）⇒ 那时**判不了就别塞**，
        #       留给 ②/③ 按文件名后缀定（宁可认不出，不可认错）。
        #    ⚠️ `attackAudio` 与 `defaultChatSound` **实测同 GUID**（59 张卡全同）⇒ 这里拿到的是
        #       chat 那一个（`defaultChatSound`），于是同一个 clip 在表里**出现两次**
        #       （一次 `attack`、一次 `default`）—— **这是原版事实**（两个字段真的都指它），不是重复导入。
        chat_fields = [k for k in clip_truth[stem] if k in CHAT_SO_FIELD_EVENT]
        if len(chat_fields) != 1:            # 判不了就别塞（宁可认不出）
            so_chat_ambig += 1
            continue
        f = chat_fields[0]
        ev = CHAT_SO_FIELD_EVENT[f]
        card_meta[cid] = (eng["name"], eng["faction"])
        lines.setdefault(cid, []).append({"ev": ev, "file": base,
                                          "text": parse_name(base)[1], "src": "cardSO", "so": f})
        so_files.add(base)
        used_files.add(base)
        so_chat_taken += 1

    # ---- ② 按 card_index 的归属建表（**兜底**；① 认走的文件名一律跳过）----
    overridden = []
    for c in index_cards:
        if not c.get("voice"):
            continue
        nm = (c["name"] or "").strip()
        eng = ekey.get((c["faction"].lower(), nm))
        if eng is None and nm.lower() in alias:
            eng = ekey.get((c["faction"].lower(), alias[nm.lower()]))
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
                if so_owner and so_owner[0] != cid:
                    overridden.append((base, cid, so_owner[0]))
                continue
            if base not in audio:
                missing.append(base)
                continue
            ev, text = parse_name(base)
            lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text})
            used_files.add(base)

    # ---- ③ 孤儿：前两级都没收录、但文件名能对上我们某张卡的（**这一步是猜的，标出来**）----
    # 判据在 `match_owner`（2026-09-18 重写）：归一化 + 整词边界 + 唯一性护栏。
    tables = build_name_tables(eng_cards)
    orphans = []
    for base, path in sorted(audio.items()):
        if base in used_files:
            continue
        hit = match_owner(os.path.splitext(base)[0], tables)
        if hit is None:
            orphans.append(base)
            continue
        ev, text = parse_name(base)
        cid = hit["id"]
        card_meta.setdefault(cid, (hit["name"], hit["faction"]))
        lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text, "src": "orphan"})
        used_files.add(base)

    # ---- ③·B 🔴 **事件名回真值**（2026-10-16 · 账 A1768）----
    # 归属定下来之后统一校正一轮：`ev` 以前一律由文件名后缀推，现在只要那个 clip 在
    # **卡 SO 全池**里被**恰好一个**原版字段引用过，就按那个字段定（并写 `so` = 字段名）。
    # ⚠️ 必须放在 ④/⑤ 之前 —— 写盘的是校正后的表。
    ev_fixed, ev_so, ev_ambig = apply_event_truth(lines, clip_truth, clip_own_field)

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
                "与 12 个 *ChatSound）→ card_index.json（兜底，无 src）→ 按文件名猜（src=orphan）。"
                "`lines[].so` = 该条音频在卡 SO 上的**原版字段名**（没佐证的条目不写）；"
                "`lines[].text` 只作气泡文字用。",
        "source": {
            "audio": "d:/2/新解包资源/assets_full/bundle_*cardassets*_assets_all/AudioClip/",
            "owner": "① 卡 SO 的 summonAudio/attackAudio/deathAudio（经 "
                     "d:/4/Unity/数据/游戏数据/card_vfx_by_card.json 的 byCard[*]._assetRefs）"
                     "**+ 12 个 *ChatSound**（经 SO 本体 "
                     "d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json，GUID → 资产名走 "
                     "card_vfx_by_card.json._assetRefs ∪ 数据/索引/anim_address_map.json 的 guid_to_asset）**权威**；"
                     "② d:/4/Unity/数据/索引/card_index.json 的 cards[].voice **兜底**",
            "script": "工具/import_original_audio.py",
        },
        "cards": [
            {"id": cid, "name": card_meta[cid][0], "faction": card_meta[cid][1], "lines": rows}
            for cid, rows in sorted(lines.items())
        ],
    }
    if not args.check:
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(out, ensure_ascii=False, indent=1))

    total_lines = sum(len(v) for v in lines.values())
    evs = {}
    for rows in lines.values():
        for r in rows:
            evs[r["ev"]] = evs.get(r["ev"], 0) + 1
    n_so = sum(1 for v in lines.values() for r in v if r.get("src") == "cardSO")
    n_orph = sum(1 for v in lines.values() for r in v if r.get("src") == "orphan")
    print("=" * 78)
    print("卡 SO（归属① · 权威）：引用 {0} 条 / 唯一文件名 {1} 个 / 卡 {2} 张".format(
        sum(len(v) for v in so_map.values()), len(so_map),
        len({x for v in so_map.values() for x in v})))
    print(f"  并进我们卡表 {so_taken} 条 · "
          f"**两张卡共用、不认** {so_ambig} 条 · **卡不在我们卡表** {so_nocard} 条 · "
          f"**同 stem 两个扩展名** {so_dupstem} 条 · **音频源里没有** {len(missing_so)} 条")
    for s in missing_so:                  # 应该恒空；非空 = 上游产物脏了，逐条复核
        print(f"    ❌ 卡 SO 引用但音频源里没有：{s}")
    print(f"card_index（归属② · 兜底）：被归属①/①·B **改判** {len(overridden)} 条"
          f"（index 记在别的卡名下 → 卡 SO 说了算）")
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
    print(f"  并进我们卡表 {so_chat_taken} 条 · **两张卡共用、不认** {so_chat_ambig} 条 · "
          f"**卡不在我们卡表** {so_chat_nocard} 条 · **同 stem 两个扩展名** {so_chat_dupstem} 条")
    print(f"🔴 事件名回真值（`ev` 不再一律按文件名后缀推）：改正 {ev_fixed} 条 · "
          f"另标上 `so`（**真的是这张卡自己的字段**）{ev_so} 条 · "
          f"**一个 clip 被多个字段引用、没敢动** {ev_ambig} 条")
    print("=" * 78)
    print(f"卡片：有语音 {len(lines)} 张 · 台词 {total_lines} 条"
          f"（其中 归属① {n_so} 条 · 归属③按名字猜 {n_orph} 条）")
    print(f"对不上卡表的 index 卡：{len(no_join)} 张 {no_join[:6]}")
    print(f"音频源里找不到的引用：{len(missing)} 条 {missing[:3]}")
    print(f"孤儿（按文件名并进我们卡表的）：{n_orph} 条；**仍未并上的 {len(orphans)} 条**：")
    for b in orphans:                     # 全列出来 —— 残差要能逐条复核，不能只报个数
        print(f"    {b}")
    print("事件分布：", dict(sorted(evs.items(), key=lambda kv: -kv[1])))
    if args.check:
        print("（--check：没有写盘）")
    else:
        print(f"已拷 {copied} 条 · 已存在跳过 {skipped} 条 → {DST_AUDIO}")
        print(f"映射已写：{DST_JSON}")


if __name__ == "__main__":
    main()
