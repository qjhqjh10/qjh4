# -*- coding: utf-8 -*-
"""抓「P3 那批用官方**旧代** `Card_Description/<id>` 盖掉了我们本来对的自译」这一类回退。

判据链（只读，不落盘）：
  ① 池里的 `descZh` 逐字 == 官方 `Card_Description/<官方键>`（即：**这一格的来源是官方旧代**）
  ② 官方另有 `Card_Description/<官方键>_new`（**新代**）
  ③ 我们自译 `zh_cards.json`（键 = 英文卡名）与池里 `descZh` 的**数字多重集**不一致
     ⇒ 说明被官方盖掉后，数值**变了**（有可能就是变错）

归一化：GUID 卡引用 → [CARD]；圈号 ①…⑩ → 1…10；「N 个/N 张/N 名/N 支」这类**量词计数**不剔（噪声如实留着）。
"""
import json, io, re

POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
OFF = r"d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"
ZH = r"d:/4/Unity/数据/卡牌翻译/zh_cards.json"

CIRCLES = "①②③④⑤⑥⑦⑧⑨⑩"


def norm(s):
    s = re.sub(r"\$\$[0-9a-fA-F]{32}\$\$", "[CARD]", s)
    s = re.sub(r"\$\$[A-Za-z0-9_]+\$\$", "[CARD]", s)
    for i, ch in enumerate(CIRCLES):
        s = s.replace(ch, str(i + 1))
    return s


def nums(s):
    return sorted(int(x) for x in re.findall(r"\d+", norm(s)))


def main():
    off = json.load(io.open(OFF, encoding="utf-8"))["terms"]
    pool = json.load(io.open(POOL, encoding="utf-8"))
    cs = pool["cards"] if isinstance(pool, dict) and "cards" in pool else pool
    zh = json.load(io.open(ZH, encoding="utf-8"))["cards"]

    def zh_of(key):
        v = off.get("Card_Description/" + key)
        if v is None:
            return None
        return v[1] if isinstance(v, list) and len(v) > 1 else (v if isinstance(v, str) else None)

    has_new = set(k[len("Card_Description/"):-4] for k in off
                  if k.startswith("Card_Description/") and k.endswith("_new"))

    rows = []
    for c in cs:
        cid = str(c.get("id") or "")
        nm = c.get("name") or ""
        z = c.get("descZh") or ""
        if not z:
            continue
        # 来源判定：先按卡 id 键，再按英文卡名（同名卡那一路）
        src = None
        if zh_of(cid) == z:
            src = cid
        if src is None and zh_of(nm) == z:
            src = nm
        if src is None:
            continue                      # 不是官方那一层给的 ⇒ 本条不管
        newv = zh_of(src + "_new") if src in has_new else None
        ours = (zh.get(nm) or {}).get("d")
        rows.append((cid, nm, src, bool(newv), z, newv, ours))

    print("来源 = 官方层的卡：%d 张（其中另有 `_new` 新代的 %d 张）"
          % (len(rows), sum(1 for r in rows if r[3])))
    print()
    print("### 甲、来源 = 官方旧代、而官方另有 `_new` 新代的（**权威 supersede，逐张都要看**）")
    for cid, nm, src, hn, z, newv, ours in rows:
        if not hn:
            continue
        print(" - %-34s %s" % (cid, nm))
        print("     池(=旧代) = %r  数字 %s" % (z[:110], nums(z)))
        print("     新代      = %r  数字 %s" % ((newv or "")[:110], nums(newv or "")))
        print("     自译      = %r  数字 %s" % ((ours or "")[:110], nums(ours or "")))
    print()
    diff = [r for r in rows if r[6] and nums(r[4]) != nums(r[6])]
    print("其中「与我们自译**数字不一致**」的：%d 张" % len(diff))
    print()
    for cid, nm, src, hn, z, newv, ours in diff:
        print(" - %-34s %s" % (cid, nm))
        print("     池 = %r" % z[:100])
        print("     自译 = %r  %s" % (ours[:100], nums(ours)))
        if hn:
            print("     新代 = %r" % (newv or "")[:100])
        print("     _new 存在? %s" % hn)


if __name__ == "__main__":
    main()
