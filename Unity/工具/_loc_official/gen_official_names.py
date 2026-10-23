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
| `PrebuiltDecks/*` | `"PrebuiltDecks/" + <deckId>` | 🔴 **我们的 `deckId` 与原版键【同源】**（原版 `LocalizedName` 读的就是 `deckId`，见下）⇒ 直接同形取；取不到才退"按英文名 join" |
| `Cardbacks/*` | `"Cardbacks/" + <SO 名>` | `Resources/Cardbacks.json` 的 `items[].name`（= SO 名）|
| `Event_Description/*` · `Item_Description/*` · `Ranked/*` · `PackName/*` · `PlayfabErrors/*` · `CustomErrors/*` · `MenuLogin/*` · `PCTips/*` · `PlayerProfile/*` | 全族原样落 | 见报告「哪族没做」 |

⚠️ 值里带 `[[token]]` / `((X_y,…))` / `<sprite name=…>` 的条**照原样落**（那是官方值）——
运行时**必须先有记号渲染层**（`A1763`）才印得对；`Achievements/*` 里确实有这类（`Ach_64` = `[[pray]]`）。

用法：
  python -I -X utf8 d:/4/Unity/工具/_loc_official/gen_official_names.py            # 干跑
  python -I -X utf8 d:/4/Unity/工具/_loc_official/gen_official_names.py --write

🔴 `A1854`：`PrebuiltDecks/*` 是**生成期 join**（键 = `"PrebuiltDecks/" + deckId`，见 `resolve_prebuilt_decks`）——
  `MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json` **一变（增删副 / 改 deckId / 重排版）就必须重跑本脚本**，
  否则那 101 条会**静默停在旧 deckId 上**（运行时按 deckId 取，取不到 = 回兜底英文名）。
  本脚本在「上游比产物新」时会**出声**（见 `resolve_prebuilt_decks` 里那段），但它只报不拦。
  🆕 **2026-10-23（界面线 W65 尾账）**：**池外那 78 副**（`数据/游戏数据/prebuilt_decks_extra.json`，
  练习窗「一对两副」落地时补的）**也在本族里** —— 它们的 `PrebuiltDecks/<deckId>` 官方键**78/78 全有**
  （旧结论「0/78 命中」是把 `official_names.json` 的**产物**当成了**源表**：产物只 join 了 103 副池）。
  ⇒ 一并 join 进来（`official_names.json` 的 `PrebuiltDecks/*` 101 → **179** 条），
  否则练习窗左列那 48 副会回落英文名。extra 文件变了同样要重跑（mtime 也在检查里）。
  🔴 **2026-10-22 就地订正（铁律 5 · `W62`）**：这一行原来写「按英文名的生成期 join / 我们的 `deckId` 与官方键不同源」
  —— **两半都是错的**：原版 `LocalizedName` 读的是 `deckId`（`+0x48`），我们的 `deckId` 与官方键**同源**
  （现读 103 副里 **101 副**同形直中；旧口径按英文名只中 95 副）。判据 → `resolve_prebuilt_decks` 的 docstring。
"""
import io
import json
import os
import re
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n  python -I -X utf8 d:/4/Unity/工具/_loc_official/gen_official_names.py            # 干跑\n  python -I -X utf8 d:/4/Unity/工具/_loc_official/gen_official_names.py --write\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json"
# 🆕 2026-10-23（界面线 W65）：池外那 78 副（练习窗一对两副）—— 数据面 + 运行期副本（见 resolve_prebuilt_decks）
EXTRA_SRC = r"d:/4/Unity/数据/游戏数据/prebuilt_decks_extra.json"
EXTRA_RES = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks_extra.json"

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
    """`PrebuiltDecks/*` 要做一次**生成期 join**：产物里的键必须是**运行时真去取的那一个**。

    🔴🔴 **2026-10-22 就地订正（铁律 5 · `W62`）—— 原来这段写「官方键是原版 SO 名、与我们的
    `deckId` 不同源 ⇒ 只能按英文名匹配」，那是【错的】**：
    原版 `PrebuiltDeck.get_LocalizedName` 读的是 **`deckId`**（`+0x48`），不是 `deckName`（`+0x40`）
    ⇒ 原版键 = **`"PrebuiltDecks/" + deckId`**，与我们的 `deckId` **同源**。
    判据：
      · `d:/2/tools/decomp_full/PrebuiltDeck__get_LocalizedName.c:15`
        `System_String__Concat(DAT_1842cf0f0, *(undefined8 *)(param_1 + 0x48), 0)`（前缀字面量 = `PrebuiltDecks/`）；
      · `d:/2/tools/il2cpp_out/dump.cs:22915-22916`（`deckName` = `0x40` · **`deckId` = `0x48`**）。
    现读（2026-10-22）：我们的 **103 副里 101 副**能按 `PrebuiltDecks/<deckId>` **直接命中**；
    旧口径（按英文名 join）只中 95 副 —— 差的 6 副（`AeldariDeck4` · `Deck_UM_Titus` · `ORK_SK_6` ·
    `SpaceWolvesDeck1/3/5`）是因为我们 `name` 列的值与官方值**字面不同**
    （例 `SpaceWolvesDeck5` 我们叫 `Wolves - Ferocity+HM Control`、官方 `Lupine Companions`）。

    ⇒ 次序：**① 先按 `PrebuiltDecks/<deckId>` 同形取**（原版口径）；② 取不到才退**按英文名 join**
    （旧口径，只作保底：万一某副的 `deckId` 被改写过、而 `name` 还是原版 `deckName`）。
    英文名那一支的**同名歧义**照旧：一副牌与它的 `_Starter` / `_PRACTICE` 变体同名 ⇒ 取不带那两种后缀的。

    ⚠️ **仍然取不到的 2 副**（`DA_SK_4` · `DarkAngelsDeck7`，两个 SO 的 `deckName` 都是
    "The Vengeful Brethren"、`isPracticeDeck=1`）：官方 `PrebuiltDecks/*` 215 条里**彻底没有**这两个键
    ⇒ **原版的 `LocalizedName` 就是 null（画出来是空名）**。我们保留 `nameZh` 兜底（比原版多做的），
    但**在这里出声**（见 `main()`），⛔ 别当成 join 写错了去「修」。

    返回 `(按 deckId 命中数, 按英文名兜底数, 英文名歧义例数, 仍缺的 deckId 列表,
             池外按 deckId 命中数, 池外仍缺的 deckId 列表)`。
    """
    byen = {}
    for k, v in off.items():
        if k.startswith("PrebuiltDecks/"):
            byen.setdefault(v[0], []).append(k[len("PrebuiltDecks/"):])
    src = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json"
    if not os.path.exists(src):
        return 0, 0, 0, [], 0, []
    # 🔴 `A1854`：这一族是**生成期 join** ⇒ 上游变了、产物就静默过期（旧 deckId / 旧英文名）。
    #    这里只**出声**（不拦写入）：mtime 比产物新就告诉你要重跑。
    for up in (src, EXTRA_SRC):
        if os.path.exists(up) and os.path.exists(OUT) and os.path.getmtime(up) > os.path.getmtime(OUT):
            print("🔴 上游 %s 比产物新 ⇒ `PrebuiltDecks/*` 的 join 已过期（`A1854`）"
                  "—— **必须重跑本脚本**（--write）" % up)
    decks = json.load(io.open(src, encoding="utf-8")).get("decks") or []
    n_direct, n_fb, amb, missing = 0, 0, 0, []
    seen = set()
    for d in decks:
        did = d.get("deckId")
        if did:
            seen.add(did)
        # ① 原版口径：`PrebuiltDeck.get_LocalizedName` = GetTranslation("PrebuiltDecks/" + deckId)
        if did and ("PrebuiltDecks/" + did) in off:
            terms["PrebuiltDecks/" + did] = off["PrebuiltDecks/" + did]
            n_direct += 1
            continue
        # ② 保底：旧口径按英文名 join（我们 `name` 列 = 原版 `deckName` 时才成立）
        cand = byen.get(d.get("name"))
        if cand:
            # 同名歧义：优先「不是 Starter / PRACTICE 变体」的那条（那就是卡组本身）
            plain = [c for c in cand if "Starter" not in c and "PRACTICE" not in c]
            pick = plain[0] if plain else cand[0]
            if len(plain) > 1:
                amb += 1
            en, zh = off["PrebuiltDecks/" + pick]
            terms["PrebuiltDecks/" + did] = (en, zh)
            n_fb += 1
            continue
        missing.append(did)

    # 🆕 2026-10-23（界面线 W65）：**池外那 78 副**（练习窗「一对两副」里落不进 103 副池的那批）。
    #    来源 = `数据/游戏数据/prebuilt_decks_extra.json`（生成器 `工具/gen_practice_decks_extra.py`；
    #    运行期副本 `MyGame/Assets/RuleEngine/Resources/prebuilt_decks_extra.json` 与它**必须逐字节相同**）。
    #    ⚠️ 这里**只走 deckId 直取**、不做英文名兜底 —— 这 78 副的 `name` 有 11 对与原版 `deckName` 不同，
    #       按名兜底会静默挑到另一副（D3 报告 §四 的 `nameCandidates` 全是内容不同的同名陷阱）。
    #    判据：官方 `PrebuiltDecks/*` 现读 215 条，这 78 个 deckId **78/78 直中**（2026-10-23 现读）。
    n_extra, extra_missing = 0, []
    extra = []
    if os.path.exists(EXTRA_SRC):
        extra = json.load(io.open(EXTRA_SRC, encoding="utf-8")).get("decks") or []
        # 数据面与运行期副本漂移 ⇒ 产物按哪份算都不对，出声（`gen_practice_decks_extra.py --check` 能定位）
        if os.path.exists(EXTRA_RES):
            if io.open(EXTRA_SRC, "rb").read() != io.open(EXTRA_RES, "rb").read():
                print("🔴 `prebuilt_decks_extra.json` 的数据面与运行期副本**不一致** ⇒ 先跑 "
                      "`工具/gen_practice_decks_extra.py` 刷新，本产物按数据面那份 join")
    else:
        print("⚠️ 读不到 `%s` ⇒ 池外 78 副不会进本产物（练习窗那 48 副会回落英文名）" % EXTRA_SRC)
    for d in extra:
        did = d.get("deckId")
        if not did:
            continue
        if did in seen:
            # 两个来源有交集 = extra 不该含这一副（生成器里有硬断言）；出声、跳过、不覆盖已 join 的值
            print("⚠️ 池外产物里的 `%s` **同时出现在 103 副池里** ⇒ 跳过（先查 gen_practice_decks_extra.py）" % did)
            continue
        seen.add(did)
        key = "PrebuiltDecks/" + did
        if key in off:
            terms[key] = off[key]
            n_extra += 1
        else:
            extra_missing.append(did)
    return n_direct, n_fb, amb, missing, n_extra, extra_missing


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
    n_direct, n_fb, amb, missing, n_extra, extra_missing = resolve_prebuilt_decks(off, terms)
    print("PrebuiltDecks：按 deckId 命中 **%d** 副 / 按英文名兜底 %d 副（同名歧义 %d 例） / **仍缺 %d 副**"
          % (n_direct, n_fb, amb, len(missing)))
    if missing:
        # ⛔ 这几副**不是 join 写错了**：原版按 `PrebuiltDecks/<deckId>` 也取不到 ⇒ 原版画出来就是空名。
        print("   ⚠️ 仍缺（官方 `PrebuiltDecks/*` 表里没有这些键 ⇒ **原版 `LocalizedName` 也是 null/空名**）："
              + "、".join(missing))
        print("   ⇒ 运行时回 `nameZh` 兜底（`DisplayName` 第二档）—— **这是我们比原版多做的**，不是漏")
    print("PrebuiltDecks（池外 · 练习窗一对两副）：按 deckId 命中 **%d** 副 / **仍缺 %d 副**"
          % (n_extra, len(extra_missing)))
    if extra_missing:
        # ⛔ 同样**不是 join 写错**：这三档的判据是官方表本身，缺键 = 原版也画空名。
        print("   ⚠️ 池外仍缺（官方表里没有这些键 ⇒ 原版 `LocalizedName` 也是空名）：" + "、".join(extra_missing))
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
