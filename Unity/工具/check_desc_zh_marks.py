#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""只读闸：卡表正文里的**官方 I2 记号**能不能被渲染层展开（`B118_W19` 建）。

回答的问题
----------
`cards_engine.json` 的 `descZh` 里有 **233 张卡 / 263 处** `((Key,arg))`（`desc` 一张都没有）。
那些记号是**原版 `Card_Description/*` 的中文列原样**（不是我们写坏的），
渲染时会由 `MyGame/Assets/CardPresentation/Core/CardIcons.cs` 的 `RewriteMarks` 按**原版四支**展开：

  ① `((Key,a,b))` → `Modular_Description/<Key>`，`{0}`/`{1}` 代参（判据 `ModifyLocalization.c:75-186`）
  ② `$$ID$$`      → `Card_Name/<ID>`
  ③ `[[枚举名]]`   → 图 + 词（`GameStaticData.TraitNameToString`）
  ④ `(N)`         → 能量图

⇒ **本脚本不写任何数据**，只按同样的词条表把 ①②③ 模拟一遍，报「渲染之后还剩几处原始记号」。
「`descZh` 里有 `((…))`」本身 **不是缺陷**（原版数据就长这样）；**缺陷 = 渲染之后还留着**。

用法
----
    python -I -X utf8 d:/4/Unity/工具/check_desc_zh_marks.py            # 打印用量，rc=2（零动作）
    python -I -X utf8 d:/4/Unity/工具/check_desc_zh_marks.py --check    # 全池扫描（零写盘）
    python -I -X utf8 d:/4/Unity/工具/check_desc_zh_marks.py --check --card ASH46 --card ASH15
    python -I -X utf8 d:/4/Unity/工具/check_desc_zh_marks.py --check --verbose   # 逐卡打印展开后

数据源（全部只读）
------------------
* `MyGame/Assets/RuleEngine/Resources/cards_engine.json`  —— 卡表（`desc` / `descZh`）
* `MyGame/Assets/CardPresentation/Resources/official_names.json` —— **运行时**批量词条（`Modular_Description` 340 条等）
* `MyGame/Assets/CardPresentation/Resources/Fonts/Warpforge Trait TextSprites.asset` —— ③ 那张图的静态清单

⛔ 本脚本**不判**「中文该怎么写」（那是铁律 7 的 PnP 卡图 + 官方表的活），只判「渲染后有没有残留」。
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

P_CARDS = os.path.join(ROOT, "MyGame", "Assets", "RuleEngine", "Resources", "cards_engine.json")
P_NAMES = os.path.join(ROOT, "MyGame", "Assets", "CardPresentation", "Resources", "official_names.json")
P_SPRITES = os.path.join(ROOT, "MyGame", "Assets", "CardPresentation", "Resources", "Fonts",
                         "Warpforge Trait TextSprites.asset")

RX_MOD = re.compile(r"\(\((.*?)\)\)")
RX_CREF = re.compile(r"\$\$(.*?)\$\$")
RX_TRAIT = re.compile(r"\[\[(.*?)\]\]")

USAGE = __doc__.split("用法\n----\n", 1)[1].split("\n数据源", 1)[0].rstrip() + """

⛔ 不带显式动作 = 零动作（只打印本段）。
"""


def read_json(path):
    with io.open(path, "rb") as f:
        return json.loads(f.read().decode("utf-8"))


def load_terms():
    """`official_names.json` → {key: {"en":…, "zh":…}}（= 运行时 `Loc.BulkOr` 那张表）。"""
    doc = read_json(P_NAMES)
    return {e.get("k"): e for e in (doc.get("entries") or []) if e.get("k")}


def load_trait_sprites():
    """③ 的图名清单（静态读 `.asset`；运行时 `SpriteExists` 就是这个集合）。"""
    if not os.path.exists(P_SPRITES):
        return set()
    with io.open(P_SPRITES, "rb") as f:
        txt = f.read().decode("utf-8", "replace")
    return set(re.findall(r"Atlas_trait_icon_[A-Za-z0-9_]+", txt))


def term_of(tbl, key, zh=True):
    e = tbl.get(key)
    if not e:
        return None
    v = e.get("zh") if zh else None
    if zh and v:
        return v
    return e.get("en") or None


def expand(text, tbl, sprites, zh=True):
    """按 `RewriteMarks` 的 ①②③ 展开；返回 (新串, 未解开的 [(种类, 记号)])。"""
    unresolved = []

    def mod(m):
        parts = m.group(1).split(",")
        term = term_of(tbl, "Modular_Description/" + parts[0], zh)
        if not term:
            unresolved.append(("((…))", m.group(0)))
            return m.group(0)
        if len(parts) > 1 and parts[1]:
            term = term.replace("{0}", parts[1])
        if len(parts) > 2 and parts[2]:
            term = term.replace("{1}", parts[2])
        return term

    def cref(m):
        t = term_of(tbl, "Card_Name/" + m.group(1), zh)
        if not t:
            unresolved.append(("$$…$$", m.group(0)))
            return m.group(0)
        return t

    def trait(m):
        name = m.group(1)
        has_sprite = ("Atlas_trait_icon_" + name) in sprites
        has_term = bool(term_of(tbl, "Card_Trait/" + name, zh))
        if not has_sprite and not has_term:
            unresolved.append(("[[…]]", m.group(0)))
            return m.group(0)
        return "<" + ("图+词" if has_term else "词") + ">"

    s = RX_MOD.sub(mod, text)
    s = RX_CREF.sub(cref, s)
    s = RX_TRAIT.sub(trait, s)
    return s, unresolved


def main(argv):
    want_check = "--check" in argv
    verbose = "--verbose" in argv
    only = []
    for i, a in enumerate(argv):
        if a == "--card" and i + 1 < len(argv):
            only.append(argv[i + 1])
    if not want_check:
        sys.stdout.write(USAGE + "\n")
        return 2

    for p in (P_CARDS, P_NAMES):
        if not os.path.exists(p):
            print("❌ 缺文件：" + p)
            return 3

    cards = read_json(P_CARDS)
    tbl = load_terms()
    sprites = load_trait_sprites()
    rows = [c for c in (cards.get("cards") or []) if c.get("id")]
    if only:
        keep = set(only)
        rows = [c for c in rows if c.get("id") in keep]

    marked = 0          # `descZh` 带 `((…))` 的张数（本次关心那一档）
    occ = 0             # `((…))` 出现次数
    resid_cards = 0
    resid_items = []
    keys_missing = {}
    for c in rows:
        z = c.get("descZh") or ""
        hits = RX_MOD.findall(z)
        if hits:
            marked += 1
            occ += len(hits)
        out, unres = expand(z, tbl, sprites, True)
        left = RX_MOD.findall(out) + RX_CREF.findall(out) + RX_TRAIT.findall(out)
        if left:
            resid_cards += 1
            resid_items.append((c["id"], c.get("name"), left[:3]))
        for k, tok in unres:
            keys_missing.setdefault(tok, 0)
            keys_missing[tok] += 1
        if verbose and hits:
            print("— %s %s\n   RAW: %s\n   OUT: %s" % (c["id"], c.get("name"), z[:160], out[:160]))

    print("卡表：%s（count=%s · 扫了 %d 张）" % (os.path.basename(P_CARDS), cards.get("count"), len(rows)))
    print("`descZh` 带 `((…))` 的卡：**%d** 张 / **%d** 处（`desc` 自带记号的：%d 张）"
          % (marked, occ, sum(1 for c in rows if RX_MOD.search(c.get("desc") or ""))))
    print("渲染后仍残留原始记号：**%d** 张" % resid_cards)
    for cid, nm, left in resid_items[:10]:
        print("   ✗ %s %s ⇒ %s" % (cid, nm, left))
    if keys_missing:
        print("解不开的记号（按出现次数）：")
        for tok, n in sorted(keys_missing.items(), key=lambda kv: -kv[1])[:15]:
            print("   %4d  %s" % (n, tok))
    else:
        print("解不开的记号：**0**（①②③ 的词条/图全都在本地）")
    return 0 if resid_cards == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
