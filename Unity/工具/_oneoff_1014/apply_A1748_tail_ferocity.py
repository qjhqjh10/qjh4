# -*- coding: utf-8 -*-
"""A1748 尾巴：给 SW13 `Blackmane Reiver` / SW31 `Fenrisian Drop Pod` 补 `Ferocity` 关键词。

判据（2026-10-14 第三十一会话 · 主对话亲核）：
  ① 判别式（`cardface_fixes.json` 的 `_2026-09-19_Ferocity误抽` 那条给的唯一判据）：
     **真有该能力的卡，卡面写 `Ferocity: <正文>`（冒号 + 正文）**；只在句子里引用的不算。
     两张卡面主对话逐张开图亲读：
       · `d:/2/Warpforge部队卡片/Space Wolves/3部队/Warpforge_13_Blackmane-Reiver.png`
         = `〔图标〕Camouflage.` / `〔图标〕Ferocity: Draw a troop. Give it +1〔拳〕, +1〔枪〕 and +1 Health` / `Infantry`
       · `d:/2/Warpforge部队卡片/Space Wolves/3部队/Warpforge_31_Fenrisian-Drop-Pod.png`
         = `〔图标〕Flying. 〔盾〕Armour 1. 〔图标〕Ferocity: Deploy two Grey Hunter` / `Vehicle`
  ② 先例（池内已有 7 张同样的）：`SW11 Blood Claw` / `SW12 Hunting Wolf` / `SW16 Fenrisian Wolf` /
     `SW19 Grey Hunter Pack Leader` / `SW28 Wulfen Pack Leader` / `SW36 Morkai Eliminator` /
     `SW42 Bjorn the Fell-Handed` —— `keywords` 里都带裸词 `Ferocity`（不带数字）。
  ③ 机制：原版 `CardTraitCollection__GetTraitsFromAbilitiesRawCard` 把 ability 触发器映射成 trait；
     `DefinedTrait.ferocity = 1265` / `AbilityTrigger.Ferocity = 725` / `CardTraitCollection.ferocitySprite` 本地全有。
  ④ 上一次挂起的理由（`A1748` 账面：「真值快照 CardTraits 里 ferocity 0 次 ⇒ 本地无判据」）
     **不成立** —— 0 次的真因是那份 2025-07-30 快照**整个 SpaceWolves 都不在内**（阵营前缀 12 个无 SW，
     最大 `_trait` = 1255 < 1265）；而**卡面这一层判据本来就在手上**。

纪律：`cardface_fixes.json` 是 **纯 CRLF**（本次现核 1885/1885）⇒ 按【字节】读写，⛔ 不用文本模式。
      两列（`keywords` / `_manual_keywords`）**一起写成同一个值** —— 照 `_2026-10-12_误抽清洗被旧阵营键静默覆盖`
      那条的先例，免得两键打架。值是**整份关键词表**（`_manual_keywords` 的规矩：写全，不是增量）。
"""
import io
import json
import shutil
import sys

P = "d:/4/Unity/数据/游戏数据/cardface_fixes.json"
BK = "d:/4/Unity/工具/_oneoff_1014/cardface_fixes.json.bak_A1748tail"

NEW = [
    ("Blackmane Reiver", ["Camouflage", "Ferocity"]),
    ("Fenrisian Drop Pod", ["Flying", "Armour 1", "Ferocity"]),
]


def arr(vals):
    """按本文件既有风格渲染一个字符串数组：`[\r\n   "a",\r\n   "b"\r\n  ]`"""
    body = ",\r\n".join('   "%s"' % v for v in vals)
    return "[\r\n%s\r\n  ]" % body


def main():
    raw = io.open(P, "rb").read()
    crlf, lf = raw.count(b"\r\n"), raw.count(b"\n")
    assert crlf == lf, "行尾不是纯 CRLF（CRLF=%d LF=%d）⇒ 停手" % (crlf, lf)
    s = raw.decode("utf-8")

    # ---- 断言（先确认锚点唯一、目标状态与预期一致，再动手）----
    for anchor in ('"_manual_keywords": {', '"keywords": {'):
        assert s.count(anchor) == 1, "锚点不唯一：%s（%d 处）" % (anchor, s.count(anchor))
    d = json.loads(s)
    mk = d["_manual_keywords"]
    kw = d["keywords"]
    for name, _ in NEW:
        assert name not in mk, "%s 已在 _manual_keywords 里 ⇒ 停手（先看现值）" % name
    assert "Blackmane Reiver" not in kw, "Blackmane Reiver 已在 keywords 里 ⇒ 停手（先看现值）"
    assert kw["Fenrisian Drop Pod"] == ["Flying", "Armour 1"], \
        "Fenrisian Drop Pod 现值不是预期：%r" % (kw["Fenrisian Drop Pod"],)

    # ---- 算好内容（⛔ 先算再写，别让 write 和求值同时发生）----
    ins = "".join('  "%s": %s,\r\n' % (n, arr(v)) for n, v in NEW)
    out = s.replace('"_manual_keywords": {\r\n', '"_manual_keywords": {\r\n' + ins, 1)
    out = out.replace('"keywords": {\r\n', '"keywords": {\r\n' + ins, 1)
    out = out.replace(
        '"Fenrisian Drop Pod": [\r\n   "Flying",\r\n   "Armour 1"\r\n  ]',
        '"Fenrisian Drop Pod": [\r\n   "Flying",\r\n   "Armour 1",\r\n   "Ferocity"\r\n  ]',
        1,
    )

    # ---- 回读校验（改完立刻验，⛔ 别信「写完当场回读」）----
    d2 = json.loads(out)
    for name, vals in NEW:
        assert d2["_manual_keywords"][name] == vals, name
        assert d2["keywords"][name] == vals, name
    assert len(d2["keywords"]) == len(kw) + 1, "keywords 条数不增反降"
    assert d2["keywords"]["Fenrisian Drop Pod"] == ["Flying", "Armour 1", "Ferocity"]
    ob = out.encode("utf-8")
    assert ob.count(b"\r\n") == ob.count(b"\n"), "写入后行尾被翻"

    shutil.copyfile(P, BK)
    with io.open(P, "wb") as f:
        f.write(ob)

    # ---- 真·落盘校验：重开文件读一遍 ----
    back = json.loads(io.open(P, "rb").read().decode("utf-8"))
    for name, vals in NEW:
        assert back["_manual_keywords"][name] == vals and back["keywords"][name] == vals, name
    print("OK · 两列各写 2 条 · keywords %d -> %d · 备份 %s" % (len(kw), len(back["keywords"]), BK))
    return 0


if __name__ == "__main__":
    sys.exit(main())
