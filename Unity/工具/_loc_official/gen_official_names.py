# -*- coding: utf-8 -*-
"""把官方 zh-CN 表里那几族**「数据面」词条**落成 `Resources/official_names.json`（`A1803`）。

**为什么单开一个产物、而不是塞进 `Core/Loc.cs`**：这几族的键**不是手写的**、也**不走 `Loc.T` 的字面量**
（账 `I2_全表可挖盘点.md` 的「读表须知」：`Titles`/`Avatars`/`Achievements`/`Cardbacks`/`PrebuiltDecks`
走的是**数据面**）—— `Titles` 368 条的键是 **32 位 GUID**、`Avatars` 419 条、`Achievements` 198 …
手写进 `Loc.cs` 那张表只会让那份文件变成一片 GUID。
⇒ 这里按**原版词条键原样**落成一个只读产物，运行时用 `Loc.BulkOr(拼出来的键, 兜底)` 取。

**键的拼法逐族**（就是原版 `GetTranslation("…")` 的拼法，⛔ 别在运行时另发明一种）：

| 族 | 运行时拼的键 | 我们这边拿什么拼 |
|---|---|---|
| `Titles/*` | `"Titles/" + <32hex id>` | `profile_cosmetics.json` 的 `titles[].id`（与官方键**同源**）|
| `Avatars/*` | `"Avatars/" + <显示名>` | `profile_cosmetics.json` 的 `avatars[].n`（我们拼的那个名）|
| `Achievements/Title/*` | `"Achievements/Title/" + eventId` | `achievements[].id`（`Ach_N`）|
| `Achievements/*`（**描述**）| `"Achievements/" + eventId` | 同上 |
| `Achievements/Types/*` | `"Achievements/Types/" + <枚举名>` | `ProfileData.TypeName` 今天回的那 4 个词 |
| `PrebuiltDecks/*` | `"PrebuiltDecks/" + <原版 SO 名>` | ⚠️ 我们的 `deckId`（`AM_SK_1`）**与官方键（`AstraMilitarumDeck1`）不同源** ⇒ 这一族**按英文名匹配**，见 `PrebuiltDecks.cs` |
| `Cardbacks/*` | `"Cardbacks/" + <SO 名>` | `Resources/Cardbacks.json` 的 `items[].name`（= SO 名）|
| `Event_Description/*` · `Item_Description/*` · `Ranked/*` · `PackName/*` · `PlayfabErrors/*` · `CustomErrors/*` · `MenuLogin/*` · `PCTips/*` · `PlayerProfile/*` | 全族原样落 | 见报告「哪族没做」 |

⚠️ 值里带 `[[token]]` / `((X_y,…))` / `<sprite name=…>` 的条**照原样落**（那是官方值）——
运行时**必须先有记号渲染层**（`A1763`）才印得对；`Achievements/*` 里确实有这类（`Ach_64` = `[[pray]]`）。

用法：
  python -I -X utf8 d:/4/Unity/工具/_loc_official/gen_official_names.py            # 干跑
  python -I -X utf8 d:/4/Unity/工具/_loc_official/gen_official_names.py --write
"""
import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json"

FAMILIES = [
    "Titles", "Avatars", "Achievements", "Cardbacks", "PrebuiltDecks", "Event_Description",
]
# ⚠️ **只收「本批真用得着的那几族」**（`A1803` 点名的 6 族）。
#   其余「数据面」族（`PackName` 771 · `Missions` 138 · `ShopOffers` 129 · `PlayfabErrors` 189 ·
#   `Item_Description` 71 · `Ranked` 20 · `PCTips` 30 · `CustomErrors` 41 · `MenuLogin` 50 ·
#   `PlayerProfile` 19 · `Rewards Menu` 9）**今天一个消费点都没有**（对应的窗/功能还没建）
#   ⇒ ⛔ **先不收**（收进来只会让这个产物胖 150 KB、还是死数据）。
#   哪天要接，**往这张表里加一族 + 重跑**即可（判据在同一个官方表里，随时可复算）；
#   ⛔ `Card_Trait/*`（77）**永远别加进来** —— 那一族的官方值在我们的 `Core/Loc.cs` 里
#   已经有一份（33 条**暂缓换回**、理由见那个文件头），两处都放 = 两份迟早打架（铁律 6）。

NOTE = (
    "官方 zh-CN 词条表里**那几族「数据面」词条**（键太多、不走手写的 `Core/Loc.cs` 表）。"
    "来源 = `数据/游戏数据/官方本地化_en+zh-CN.json`（原版 I2 `I2Languages` 远端包，"
    "2026-10-14 解开）。**键 = 原版词条键原样**（拼法见 `工具/_loc_official/gen_official_names.py` 文件头）。"
    "运行时取值口 = `Core/Loc.cs` 的 `Loc.BulkOr(键, 兜底)`（跟语档、缺键出声）。"
    "⚠️ 值里可能带 I2 记号（`[[..]]` / `((..))` / `<sprite name=..>`）—— 那是官方模板，"
    "印之前要过记号渲染层（`A1763`）。"
)


def resolve_prebuilt_decks(off, terms):
    """`PrebuiltDecks/*` 要做一次**生成期 join**：官方键是原版 SO 名（`AstraMilitarumDeck1`），
    而我们的 `deckId` 是另一种写法（`AM_SK_1`）⇒ **两者不同源**。

    判据：**按英文名匹配**（我们 `prebuilt_decks.json` 的 `name` 列**就是原版 `deckName`**）。
    现读：103 副里 **74 副**能配上（官方那 97 条只覆盖一部分卡池）。
    ⚠️ **同名歧义 10 例**（一副牌与它的 `_Starter` / `_PRACTICE` 变体同名）⇒ 取**不带那两种后缀**的那条。
    ⇒ 产物里落成 `PrebuiltDecks/<我们的 deckId>`，运行时就能直接按 deckId 取。

    返回 (落了几副, 配上英文名但没能挑出唯一键的例数)。
    """
    byen = {}
    for k, v in off.items():
        if k.startswith("PrebuiltDecks/"):
            byen.setdefault(v[0], []).append(k[len("PrebuiltDecks/"):])
    src = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json"
    if not os.path.exists(src):
        return 0, 0
    decks = json.load(io.open(src, encoding="utf-8")).get("decks") or []
    n, amb = 0, 0
    for d in decks:
        cand = byen.get(d.get("name"))
        if not cand:
            continue
        # 同名歧义：优先「不是 Starter / PRACTICE 变体」的那条（那就是卡组本身）
        plain = [c for c in cand if "Starter" not in c and "PRACTICE" not in c]
        pick = plain[0] if plain else cand[0]
        if len(plain) > 1:
            amb += 1
        en, zh = off["PrebuiltDecks/" + pick]
        terms["PrebuiltDecks/" + d["deckId"]] = (en, zh)
        n += 1
    return n, amb


def main():
    do = "--write" in sys.argv
    off = L.official_pairs()
    terms = {}
    stat = []
    for fam in FAMILIES:
        n = 0
        for k, v in off.items():
            if k == fam or k.startswith(fam + "/"):
                terms[k] = v
                n += 1
        stat.append((fam, n))
    # `PrebuiltDecks/*` 的两份键形只留**运行时真用的那一份**（按 deckId）—— 见上面那段 doc
    for k in [k for k in terms if k.startswith("PrebuiltDecks/")]:
        del terms[k]
    n_pre, amb = resolve_prebuilt_decks(off, terms)
    print("PrebuiltDecks：按英文名 join 上 **%d** 副（同名歧义 %d 例，取非 Starter/PRACTICE 那条）" % (n_pre, amb))
    payload = {"version": 1, "note": NOTE,
               "source": "数据/游戏数据/官方本地化_en+zh-CN.json",
               "count": len(terms),
               # ⚠️ 形状是**数组**不是字典 —— `JsonUtility` 读不了字典（同 `trait_tips.json` / `card_icon_plan.json`）。
               "entries": [{"k": k, "en": v[0], "zh": v[1]} for k, v in terms.items()]}
    print("族 → 条数：")
    for f, n in stat:
        print("   %-20s %d" % (f, n))
    print("合计 %d 条" % len(terms))
    tok = sum(1 for v in terms.values() if "[[" in v[1] or "((" in v[1] or "<sprite" in v[0])
    print("⚠️ 值里带 I2 记号的 %d 条（印之前要记号渲染层）" % tok)
    doc = json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
    print("产物大小 ≈ %.0f KB" % (len(doc.encode("utf-8")) / 1024.0))
    if not do:
        print("（干跑，没写文件）")
        return 0
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(doc)
    print("落盘 %s" % OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
