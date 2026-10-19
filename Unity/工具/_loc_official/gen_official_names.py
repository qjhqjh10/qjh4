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

🔴 `A1854`：`PrebuiltDecks/*` 是**按英文名的生成期 join**（我们的 `deckId` 与官方键不同源）——
  `MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json` **一变（增删副 / 改名 / 重排版）就必须重跑本脚本**，
  否则那 95 条会**静默停在旧 deckId 上**（运行时按 deckId 取，取不到 = 回兜底英文名）。
  本脚本在「上游比产物新」时会**出声**（见 `resolve_prebuilt_decks` 末尾那段），但它只报不拦。
"""
import io
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json"

FAMILIES = [
    "Titles", "Avatars", "Achievements", "Cardbacks", "PrebuiltDecks", "Event_Description",
    # 🆕 **2026-10-15（P3 相 · `A1763` 数据层）：卡面正文那两族**（`Card_Description` 里真用到的、
    #    而 `Resources/` 下**一条都没有**的两族）—— 它们是 `CardIcons.RewriteMarks` ① ② 两支的**必需半边**：
    #    · `Modular_Description/*` = **340** 条 —— ① `((Key,a,b))` 那一支查的就是它
    #      （现读 `Card_Description` 正文里用到 **86** 种键 / **1013** 处，逐条都在表里）；
    #    · `Card_Name/*` = **1065** 条 —— ② `$$ID$$` 那一支（现读 `$$…$$` 用到 **116** 种卡 id / **300** 处，
    #      **116 种全部在这张表里**）。⚠️ 不收它 = 现池 **120 张**卡的中文卡面会**原样印出 `$$<id>$$`**
    #      （`RewriteMarkCardRefs` 的词条缺失档是「原样留着 + 出声」）—— 那是**看得见的退步**。
    # 🔴 **和 `Core/Loc.cs` 撞不撞**：不撞。`Loc.HasEntry`（手写表）**排在 `HasBulk`（本产物）之前**
    #    ⇒ 手写表里那 26 条 `Card_Name/*` 是**我们自造卡**的键（`Ember Warlord` 那种），
    #    与本族的官方 id 键**键空间不相交**（见 `gen_cards_engine.py` 的 `ZH_NAME_ALIAS` 那一段）。
    "Modular_Description", "Card_Name",
]
# ⚠️ **只收「本批真用得着的那几族」**（`A1803` 点名的 6 族 + 上面 `A1763` 加的两族）。
#   其余「数据面」族（`PackName` 771 · `Missions` 138 · `ShopOffers` 129 · `PlayfabErrors` 189 ·
#   `Item_Description` 71 · `Ranked` 20 · `PCTips` 30 · `CustomErrors` 41 · `MenuLogin` 50 ·
#   `PlayerProfile` 19 · `Rewards Menu` 9 · `Card_Abilities` 85 · `CardTraitDescription` 76 ·
#   `Card_Targeted` 31 · `Card_Race` 21）**今天一个消费点都没有**（对应的窗/功能还没建）
#   ⇒ ⛔ **先不收**（收进来只会让这个产物胖几百 KB、还是死数据）。
#   哪天要接，**往这张表里加一族 + 重跑**即可（判据在同一个官方表里，随时可复算）。
#
# 🔴🔴 **2026-10-15 就地订正（铁律 5 · P3 相）**：这一行原来写的是
#   「⛔ `Card_Trait/*`（77）**永远别加进来** —— 那一族的官方值在我们的 `Core/Loc.cs` 里已经有一份」。
#   **那半句只对【两边都有的 51 条】成立**，而 **77 − 51 = 26 条在 `Loc.cs` 里根本没有**
#   （`frenzied`/`rage`/`concussive`/`markOfChaos`/`markOfKhorne`/`markOfNurgle`/`markOfSlaanesh`/
#    `markOfTzeentch`/`battleHonour`/`berzerk`/`chooseOne`/`combo`/`counterOffensive`/`courage`/
#    `dropPod`/`firstStrike`/`frontLine`/`jam`/`large`/`relentless`/`resolution`/`sneak`/`support`/
#    `questPoints`/`frenzied_old`/`markOfChaosOLD`）⇒ 那 26 条今天**查不到词条**，
#   `RewriteMarkTraits` 只能退回**原版自己的兜底「只印枚举名」**。
#   **空着的 26 条补进来是纯增益、且不产生第二份** —— 因为键**不相交**，`HasEntry` 依旧优先
#   （`Loc.cs` 里那 62 条一个都不会被盖）。判据（口径）：
#   `资料/普查产出_第三十一会话/G_卡表P0.md` §七·5 + §三③ 那条「8 种 / 56 处查不到」。
#   ⇒ 本脚本改成**按 `Loc.cs` 现算补集**（见 `card_trait_complement()`），⛔ 别手抄一份名单。
CARD_TRAIT_FAMILY = "Card_Trait"
LOC_CS = r"d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs"

NOTE = (
    "官方 zh-CN 词条表里**那几族「数据面」词条**（键太多、不走手写的 `Core/Loc.cs` 表）。"
    "来源 = `数据/游戏数据/官方本地化_en+zh-CN.json`（原版 I2 `I2Languages` 远端包，"
    "2026-10-14 解开）。**键 = 原版词条键原样**（拼法见 `工具/_loc_official/gen_official_names.py` 文件头）。"
    "运行时取值口 = `Core/Loc.cs` 的 `Loc.BulkOr(键, 兜底)`（跟语档、缺键出声）；"
    "🔴 **手写表 `Loc.HasEntry` 排在它之前** ⇒ 两处同键时手写表赢（所以本产物只补手写表里没有的）。"
    "⚠️ 值里可能带 I2 记号（`[[..]]` / `((..))` / `<sprite name=..>`）—— 那是官方模板，"
    "印之前要过记号渲染层（`A1763`）。⚠️ 值带记号时 `Loc.BulkOr` 会先过 `CardIcons.RewriteMarks`；"
    "渲完仍留着记号才回兜底 + 出声（判据 → 资料/普查产出_第三十一会话/AC_Loc记号闸.md）。"
)


def card_trait_complement(off):
    """`Card_Trait/*` 里**`Core/Loc.cs` 手写表没有的那些**（2026-10-15 · P3 相 `A1763` 数据层）。

    **为什么是「补集」而不是整族**：见上面 `CARD_TRAIT_FAMILY` 那一段长注释 —— 整族放进来会与
    `Loc.cs` 那 62 条**同键两份**（铁律 6）；只补手写表里**没有**的那些，键**不相交**、
    `HasEntry` 依旧优先，两份永远不会打架。

    **怎么算**：直接**读 `Core/Loc.cs`**（现算，⛔ 不留一份手抄名单 —— 手抄的迟早过期）。
    比较口径与 `CardIcons.TraitTermOf` **同一把**：先精确比，再比**归一化**形式
    （只留小写字母数字：`bloodThirst` → `bloodthirst`）—— 因为 `Loc.cs` 的键是**规范键**
    （`cantattack`/`spiritstone`/`huntmark`…），而官方枚举名是驼峰。

    返回 `(要加进去的 {键: 值}, 诊断串)`；诊断串里带着两个数（`Loc` 里几条 / 官方几条 / 补几条），
    **跑的时候打出来** —— 它一变就说明 `Loc.cs` 或官方表被人动过。
    """
    src = ""
    if os.path.exists(LOC_CS):
        src = io.open(LOC_CS, encoding="utf-8").read()
    loc_keys = set(re.findall(r'"Card_Trait/([^"]+)"\s*,\s*new Entry\(', src))

    def norm(s):
        return "".join(ch.lower() for ch in s if ch.isalnum())

    loc_norm = {norm(k) for k in loc_keys}
    out, official_n = {}, 0
    for k, v in off.items():
        if not k.startswith(CARD_TRAIT_FAMILY + "/"):
            continue
        official_n += 1
        short = k[len(CARD_TRAIT_FAMILY) + 1:]
        if short in loc_keys or norm(short) in loc_norm:
            continue
        out[k] = v
    diag = (f"`Loc.cs` 手写 {len(loc_keys)} 条 · 官方 {official_n} 条 ⇒ **补 {len(out)} 条**")
    if not src:
        diag += "  🔴 读不到 `Core/Loc.cs`（路径变了？）—— 补集算成了整族，先查这个"
    if not out:
        diag += "  🔴 补集为空 —— 要么官方表变了、要么正则没匹配上，**先查这里**"
    return out, diag



def resolve_prebuilt_decks(off, terms):
    """`PrebuiltDecks/*` 要做一次**生成期 join**：官方键是原版 SO 名（`AstraMilitarumDeck1`），
    而我们的 `deckId` 是另一种写法（`AM_SK_1`）⇒ **两者不同源**。

    判据：**按英文名匹配**（我们 `prebuilt_decks.json` 的 `name` 列**就是原版 `deckName`**）。
    现读（2026-10-19）：103 副里 **95 副**能配上（官方 `PrebuiltDecks/*` 现读 215 条，含变体）。
    ⚠️ **同名歧义 73 例**（一副牌与它的 `_Starter` / `_PRACTICE` 变体同名）⇒ 取**不带那两种后缀**的那条。
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
    # 🔴 `A1854`：这一族是**生成期 join** ⇒ 上游变了、产物就静默过期（旧 deckId / 旧英文名）。
    #    这里只**出声**（不拦写入）：mtime 比产物新就告诉你要重跑。
    if os.path.exists(OUT) and os.path.getmtime(src) > os.path.getmtime(OUT):
        print("🔴 上游 %s 比产物新 ⇒ `PrebuiltDecks/*` 的 join 已过期（`A1854`）"
              "—— **必须重跑本脚本**（--write）" % src)
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
    # 🆕 2026-10-15（P3 相 · `A1763` 数据层）：`Card_Trait/*` **只补 `Loc.cs` 手写表没有的那些** ——
    #    判据与理由见 `card_trait_complement()` 的 docstring。整族放进来会与手写表**同键两份**（铁律 6）。
    ct_terms, ct_diag = card_trait_complement(off)
    print("Card_Trait：  " + ct_diag)
    if ct_terms:
        print("             补的是：" + "、".join(sorted(k.split("/", 1)[1] for k in ct_terms)))
    terms.update(ct_terms)
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
