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
  · 归属  `d:/4/Unity/数据/索引/card_index.json` 的 `cards[].voice`
          （602 张卡 / 1778 条引用；实测 **1778/1778 在音频源里都找得到**）
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
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/vo"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/voice_lines.json"

# 认得出的事件后缀（**小写**比较）。⚠️ `wp` 的语义未证实（只在文件名里出现），
# 但**照原样收进来**不解释 —— 不认识的照样进表，事件名就叫它自己。
KNOWN_EVENTS = {
    "greet", "attack", "death", "concede", "hurry", "intro", "mirror", "threat",
    "wp", "tbc", "cantdo", "cant", "backup",
}
_EVENT_RE = re.compile(r"^(gen[1-9]|backup\d+|vs.*)$")


def norm_event(suffix):
    """文件名尾段 → 规范事件名（认不出返回 None）。"""
    s = suffix.lower()
    if s in KNOWN_EVENTS:
        return "cantdo" if s == "cant" else ("backup" if s == "backup" else s)
    if _EVENT_RE.match(s):
        return s
    return None


def parse_name(base):
    """文件名 → (事件, 台词原文)。`<...> - <台词> (配音演员)` 这种是**出场台词**（普通单位就这一条）。"""
    stem = os.path.splitext(base)[0]
    if " - " in stem:
        text = stem.split(" - ", 1)[1].strip()
        # 去掉结尾的配音演员名（`(Vivienne)` 这种）
        text = re.sub(r"\s*\([^)]*\)\s*$", "", text).strip()
        return "line", text
    # 没有 " - " 的：看尾段是不是事件
    tail = stem.rsplit("_", 1)[-1] if "_" in stem else ""
    ev = norm_event(tail)
    return (ev, "") if ev else ("line", "")


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

    # ---- ① 按 card_index 的归属建表（键 = 我们的 id）----
    lines = {}          # id -> [ {"ev","file","text"} ]
    card_meta = {}      # id -> (name, faction)
    used_files = set()
    missing = []
    no_join = []

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
            if base not in audio:
                missing.append(base)
                continue
            ev, text = parse_name(base)
            lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text})
            used_files.add(base)

    # ---- ② 孤儿：索引没收录、但文件名能对上我们某张卡的（**这一步是猜的，标出来**）----
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

    # ---- ③ 拷贝音频 ----
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

    # ---- ④ 写映射 ----
    # ⚠️ `cards` 是**数组不是字典** —— Unity 的 `JsonUtility` 反序列化不了字典
    #    （`VoiceLines.cs` 直接吃这个形状；要用字典就得再引一个 JSON 库，不值当）。
    out = {
        "version": 1,
        "note": "原版单位语音表。id = 我们卡表的 id（cards_engine.json）；"
                "file = Resources/Art/audio/vo/<名>（无扩展名时按文件名去掉扩展名）；"
                "ev 见 语音索引.md §四（`wp` 语义未证实）。",
        "source": {
            "audio": "d:/2/新解包资源/assets_full/bundle_*cardassets*_assets_all/AudioClip/",
            "owner": "d:/4/Unity/数据/索引/card_index.json 的 cards[].voice",
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
    print(f"卡片：有语音 {len(lines)} 张（其中并到我们卡表 {len(lines) - 0} 张）· 台词 {total_lines} 条")
    print(f"对不上卡表的 index 卡：{len(no_join)} 张 {no_join[:6]}")
    print(f"音频源里找不到的引用：{len(missing)} 条 {missing[:3]}")
    print(f"孤儿（按文件名并进我们卡表的）：{sum(1 for v in lines.values() for r in v if r.get('src') == 'orphan')} 条；"
          f"**仍未并上的 {len(orphans)} 条**：")
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
