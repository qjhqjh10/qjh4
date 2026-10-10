# -*- coding: utf-8 -*-
"""按 `CardPresentation/Core/CardIcons.cs` 的 `Rewrite` **逐分支静态复刻**，
把一份计划表对全池卡铺一遍。用法：

    python -I sim_rewrite.py <plan.json> <out.txt>

⚠️ 静态复刻，**不是 Unity 实跑**（不模拟 `<link>` 那一层的运行时行为）。

🔴 **2026-10-10（`A1443`）：本文件原来与 `CardIcons.cs` 【不等价】** —— `if/elif/else` 结构把
`CardIcons.cs:337` 那句**无条件** `s.Replace(it.token, tag)` 漏在了「灵魂石 / 数字烘在图里」
那一支之外（C# 那支**没有 `continue`**、走完两条正则还会落到函数尾）⇒ **裸 `①`/`②` 一个都换不掉**
（影响 27 张灵族卡的误伤面读数）。已按逐分支等价修回，另两处类型级差异
（`isdigit` → ASCII 数字、`lower()` → 逐字符序数折叠）一并照 C# 对齐，见 `_fold` / `_ordinal_icontains`。
"""
import json, io, os, re, sys

sys.stdout.reconfigure(encoding="utf-8")

ROOT = "D:/4/Unity"
CARDS = os.path.join(ROOT, "MyGame/Assets/RuleEngine/Resources/cards_engine.json")

# `Badges.SpriteKey`（Badges.cs:173-179）—— 只有这四条是反向映射，其余 = 小写化图名
SPRITE_KEY = {
    "frenzied":   "destroyer",
    "rage":       "penitence",
    "markOfChaos": "darkpact",
    "concussive": "concussion",
}


def key_of(sprite):
    if not sprite:
        return None
    s = re.sub(r"_?\d+$", "", sprite)
    if len(s) == 0:
        return None
    return SPRITE_KEY.get(s) or s.lower()


def _fold(ch):
    """逐字符大小写折叠 —— 与 .NET `OrdinalIgnoreCase` 同口径（**一对一**映射）。"""
    lo = ch.lower()
    return lo if len(lo) == 1 else ch


def _ordinal_icontains(hay, needle):
    """复刻 .NET `String.IndexOf(value, StringComparison.OrdinalIgnoreCase)`
    （`CardIcons.cs:275` 用它判「token 在不在图名里」）。

    ⛔ 别写成 `needle.lower() not in hay.lower()` —— Python 的 `str.lower()` 是**整串全量**折叠
    （`'İ'.lower()` 会变成两个字符），与 .NET 的**逐字符**序数比较不是一回事。
    """
    if not needle:
        return True
    h, n = len(hay), len(needle)
    if n > h:
        return False
    fh = [_fold(c) for c in hay]
    fn = [_fold(c) for c in needle]
    return any(fh[i:i + n] == fn for i in range(h - n + 1))


def rewrite(items, text):
    """items = [(token, sprite), ...]（= 计划表某一字段的条目）。返回换完的串。"""
    if not text:
        return text
    order = sorted(items, key=lambda it: (-len(it[0] or ""), it[0] or ""))
    s = text
    for token, sprite in order:
        if not token:
            continue
        if not sprite:
            continue                      # 缺口：原样留着
        sprite_tag = '<sprite name="%s">' % sprite
        link_id = key_of(sprite)
        tag = sprite_tag if not link_id else '<link=%s>%s</link>' % (link_id, sprite_tag)
        stone = "SpiritStone" in sprite
        if token not in s:
            continue
        c0 = token[0]
        symbol = not (c0.isalpha() or ("0" <= c0 <= "9"))
        # 判据与 C# 一致 = **ASCII 数字**（`CardIcons.cs:274` 的 `IndexOfAny('0'..'9')`）——
        # 原来的 `ch.isdigit()` 是 **Unicode** 数字类（`①`/`²` 也算）⇒ 会造出 C# 不会有的命中。
        di = next((i for i, ch in enumerate(sprite) if "0" <= ch <= "9"), -1)
        num_in_art = di >= 0 and not _ordinal_icontains(sprite, token)
        if stone or num_in_art:
            # 先把「数字 + 空格 + 整串 token」换掉，再把「数字 + 整串 token」换掉。
            # ⚠️ 两条都只在**真的匹配得上**时才生效。
            # ⚠️ 用 `lambda m: tag` 而不是直接给替换串 —— C# 的 `Regex.Replace` 替换串里
            #    `$` 有特殊含义、Python 的是 `\`，包成 lambda 两边都退化成纯字面量。
            s = re.sub(r"[0-9]+\s+" + re.escape(token), lambda m: tag, s)
            s = re.sub(r"[0-9]+" + re.escape(token), lambda m: tag, s)
        elif not symbol and not token.startswith("["):
            bare = "<nobr>" + sprite_tag + token + "</nobr>"
            if link_id:
                bare = "<link=%s>%s</link>" % (link_id, bare)
            if bare in s:
                continue
            s = s.replace(token, bare)
            continue
        # 🔴 **这一句是上面两支共用的**（`A1443` 的修正点）：C# 那边「灵魂石 / 数字烘在图里」
        #    那一支**没有 `continue`**（`CardIcons.cs:289` 之后直接落到函数尾），
        #    `:337` 的 `s.Replace(it.token, tag)` 照样会执行 ⇒ **裸 `①`/`②`（没有前置数字）
        #    就是靠这一句换掉的**。原来这里是 `if/elif/else` ⇒ 灵魂石那一支走完就完了。
        s = s.replace(token, tag)
    return s


def load_plan(path):
    d = json.load(io.open(path, encoding="utf-8"))
    out = {}
    for c in d["cards"]:
        out[c["id"]] = {f["name"]: [(i["token"], i["sprite"]) for i in f["items"]]
                        for f in c["fields"]}
    return out


def main():
    plan_path, out_path = sys.argv[1], sys.argv[2]
    plan = load_plan(plan_path)
    cards = json.load(io.open(CARDS, encoding="utf-8"))["cards"]
    rendered = {}
    for c in cards:
        cid = c["id"]
        fields = plan.get(cid)
        if not fields:
            continue
        for fld in ("desc", "descZh"):
            if fld not in fields:
                continue
            rendered[cid + "|" + fld] = rewrite(fields[fld], c.get(fld) or "")
    # 幂等：应用两遍 == 一遍
    diff2 = {}
    for c in cards:
        cid = c["id"]
        fields = plan.get(cid)
        if not fields:
            continue
        for fld in ("desc", "descZh"):
            if fld not in fields:
                continue
            raw = c.get(fld) or ""
            one = rewrite(fields[fld], raw)
            two = rewrite(fields[fld], one)
            if one != two:
                diff2[cid + "|" + fld] = [one, two]
    with io.open(out_path, "w", encoding="utf-8", newline="\n") as f:
        json.dump({"rendered": rendered, "not_idempotent": diff2}, f,
                  ensure_ascii=False, indent=1, sort_keys=True)
    print("wrote", out_path, "rendered:", len(rendered), "not_idempotent:", len(diff2))


if __name__ == "__main__":
    main()
