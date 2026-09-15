#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""A7：把**天赋名**补回卡面效果文字 —— 只改 `descZh`（显示层）。

**问题**（`资料/PnP卡图_逐张对账_0915.md` §六末 A7）：原版卡面在效果文字里印一整句
`Talent: <名字>`，我们那边只剩一个光秃秃的「天赋」——因为 `CardText.KeywordSegment`
只会补**有显示名的词**（`talent` → 「天赋」），**名字本身在 `desc`/`descZh` 里丢了**。

**判据**：四个子代理逐张开 PnP 卡图放大读出来的（原句照抄，见 §六末 A7 的证据表）。
中文名**不自己编**，一律取 `数据/卡牌翻译/zh_cards.json` 里那张天赋卡自己的 `n` 字段：
  · Hymn of Battle → 战斗圣歌   · Ethereal Supreme → 至尊以太
  · Feeding Frenzy → 进食狂潮   · Cadian Honour    → 卡迪亚荣誉
  · Supreme Grand Master → 至高大师

⚠️ **只改 `descZh`，不动 `desc`**：给 `desc` 加 `Talent: X` 会**接进天赋连线**
   （`CardDef.CollectTalent` 从 desc/keywords 里读天赋名）⇒ 可能和 `keywords` 里已有的那条**重复连线**。
   那是玩法改动，得单独验，**另记在文档里**。

用法：PYTHONIOENCODING=utf-8 python Unity/工具/_fix_a7_talent.py [--write]
"""
import io, json, sys

sys.stdout.reconfigure(encoding="utf-8")
P = "Unity/数据/游戏数据/cardface_fixes.json"

# (卡 id, 旧 descZh 必须长这样, 新 descZh)
FIX = [
    # 原版：`Talent: Hymn of Battle` + `5 ☀: Gain Shield`
    ("SOR12", "5 ☀：获得护盾", "天赋：战斗圣歌。5 ☀：获得护盾"),
    # 原版：`Talent: Ethereal Supreme`（这张的 desc 就是天赋名本身）
    ("TAU1", "至尊以太", "天赋：至尊以太"),
    # 原版：`Talent: Feeding Frenzy.` 之后才是 `Rally: …`（⚠️ 旧值是 A6 刚改过的那份）
    ("TL80", "集结：获得 +2 近战攻击、+2 远程攻击，且每有 1 个受伤的敌人获得 +1 生命",
             "天赋：进食狂潮。集结：获得 +2 近战攻击、+2 远程攻击，且每有 1 个受伤的敌人获得 +1 生命"),
    # 原版：`Friendly Infantry costs 1 less.` + `Talent: Cadian Honour`
    ("AM16", "友方步兵的费用减少 1 点。", "友方步兵的费用减少 1 点。天赋：卡迪亚荣誉"),
    # 原版：`Agenda: …` + `Talent: Supreme Grand Master`（我们原来只有裸名）
    ("DA3", "从你的卡组选择一张牌置于卡组顶部。至高大师",
            "议程：从你的卡组选择一张牌置于卡组顶部。天赋：至高大师"),
]

NOTE_KEY = "_2026-09-15_A7天赋名"
NOTE = ("2026-09-15 **A7 批**：把**天赋名**补回卡面效果文字（**只改 `descZh`**）。"
        "原版在效果文字里印整句 `Talent: <名字>`，我们原来只剩光秃秃的「天赋」——"
        "`CardText.KeywordSegment` 只补**有显示名的词**，名字本身在正文里丢了就补不回来。"
        "中文名**不自己编**，取 `数据/卡牌翻译/zh_cards.json` 里那张天赋卡自己的 `n`："
        "战斗圣歌 / 至尊以太 / 进食狂潮 / 卡迪亚荣誉 / 至高大师。"
        "证据：`资料/PnP卡图_逐张对账_0915.md` §六末 A7。"
        "⚠️ 这 5 张的**英文 `desc` 同样缺 `Talent: X`**（`TAU1` 是连 `Talent:` 前缀都没有）——"
        "**没动**：给 `desc` 加它等于接进天赋连线（`CardDef.CollectTalent`），可能和 `keywords` 里"
        "已有的那条重复，属玩法改动，另记在文档里。")


def main():
    write = "--write" in sys.argv
    raw = io.open(P, "rb").read()
    d = json.loads(raw.decode("utf-8"))
    cs = {c["id"]: c for c in json.load(
        io.open("Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json", encoding="utf-8"))["cards"]}
    dz = d.setdefault("descZh", {})

    bad = []
    for cid, old, new in FIX:
        c = cs.get(cid)
        if c is None:
            bad.append((cid, "不在卡池")); continue
        cur = c.get("descZh")
        if cur != old:
            bad.append((cid, "现值对不上：%r" % (cur,))); continue
        name = c["name"]
        key = name
        if sum(1 for x in cs.values() if x["name"] == name) > 1:
            key = c["faction"] + "/" + name
        dz[key] = new
        print("  ✓ %-8s → %s" % (cid, new[:60]))

    if bad:
        print("\n🔴 有 %d 条对不上，**没有写盘**：" % len(bad))
        for b in bad:
            print("   ", b[0], b[1])
        sys.exit(1)

    d.setdefault("_manual_descZh_note", {})[NOTE_KEY] = NOTE
    out = json.dumps(d, ensure_ascii=False, indent=1).replace("\n", "\r\n")
    if write:
        io.open(P, "wb").write(out.encode("utf-8"))
        print("\n已写回 %s（%d 条）" % (P, len(FIX)))
    else:
        print("\n（只报告，没写盘 —— 加 --write 才写）")


if __name__ == "__main__":
    main()
