# -*- coding: utf-8 -*-
"""把规则书关键词表转成运行时能读的 `Resources/trait_tips.json`（Trait tooltip 的文案）。

**为什么要有这一步**：tooltip 要显示「这个关键词是什么意思」，而这句话的**唯一权威**是规则书
（`资料/关键词图标/_规则书关键词表.md`，来源 = `规则书/…_中文翻译.md:161-225` 的 61 条）。
手抄进 C# 迟早和规则书对不上（改一处、另一处不动），所以**从那张表生成**。

**键用中文名**（不是我们的规范键）—— 这样本脚本**不用去解析 C#**：
运行时要查的时候，先 `CardText.KeywordZh(规范键)` 拿到中文名，再查这张表。
⚠️ 我们自造的那个词（`ability`）**不在规则书 61 条里** ⇒ 表里没有它，查不到就如实返回 null
（`TipText.Trait` 会回退成「只有名字、没有解释」），**不编一句话**。

用法：
  PYTHONIOENCODING=utf-8 python 工具/gen_trait_tips.py            # 干跑，只报告
  PYTHONIOENCODING=utf-8 python 工具/gen_trait_tips.py --write    # 落盘
"""
import io
import re
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))          # d:/4/Unity
SRC = os.path.join(ROOT, "资料/关键词图标/_规则书关键词表.md")
OUT = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/trait_tips.json")
WRITE = "--write" in sys.argv

# 规则书表里的「效果（规则书原文）」列，头两行是表头/分隔
COLS = ("中文名", "英文名", "效果（规则书原文）", "出处行号", "带数值?", "备注")


# ⚠️ 这张表 **125 行 > 61 条** —— 表体之后还接着**附录**（「触发时机 → 对应事件」的 A/B 段），
#    那些行的**列结构不一样**（第一格写成 `失明 Blind`，第二格是中文效果）。
#    不加判据的话会生成一堆 `"失明 Blind"` 这样的垃圾键 ⇒ **第二格必须是纯英文关键词名**。
EN_ONLY = re.compile(r"^[A-Za-z][A-Za-z' \-]*$")


def main():
    rows = []
    skipped = 0
    started = False
    for ln in io.open(SRC, encoding="utf-8"):
        # 🔴 **遇到下一个 `##` 标题就停** —— 那张表在 61 条关键词之后还接着一节
        #    「## 关键词之外的图标（规则书里不是关键词、但卡面当图标用）」，
        #    里面是**卡面数值**（近战攻击/远程攻击/生命/费用/类型/能量/骷髅头/稀有度宝石/任务点）。
        #    那些**不是 trait**，收进来会让 tooltip 表里混进 9 条不是关键词的条目。
        if ln.startswith("## "):
            if started:
                break
            continue
        if not ln.startswith("|"):
            continue
        p = [x.strip() for x in ln.strip().strip("|").split("|")]
        if len(p) < 4 or p[0] in ("中文名", "") or set(p[0]) <= set("-: "):
            continue
        if not EN_ONLY.match(p[1]):
            skipped += 1
            continue
        started = True
        rows.append(p)

    tips = {}
    for p in rows:
        zh, en, body, line = p[0], p[1], p[2], p[3]
        if not zh or not body:
            continue
        # 同一中文名出现两次时**不覆盖**（取**第一次**出现的 —— 定义区在最前）。
        tips.setdefault(zh, {"en": en, "body": body, "line": line})

    print("规则书表：列形合格 %d 行（附录行跳过 %d 行）→ 去重后 %d 个中文名"
          % (len(rows), skipped, len(tips)))
    dup = len(rows) - len(tips)
    if dup:
        print("（其中 %d 行是同一中文名的重复/附录行，取首次出现的那条）" % dup)
    no_line = [k for k, v in tips.items() if not v["line"]]
    if no_line:
        print("⚠️ 没写出处行号的 %d 个：%s" % (len(no_line), no_line[:8]))

    if not WRITE:
        print("（干跑，没写文件）")
        for k in list(tips)[:5]:
            print("   %s → %s" % (k, json.dumps(tips[k], ensure_ascii=False)[:90]))
        return 0

    payload = {
        "version": 1,
        "note": "关键词（trait）tooltip 的文案。由 工具/gen_trait_tips.py 从 "
                "资料/关键词图标/_规则书关键词表.md 生成，别手改。"
                "键 = 规则书里的**中文名**（运行时要先用 CardText.KeywordZh(规范键) 换过来）。"
                "⚠️ 这是**我们照规则书写的那一份**，不是原版的 I2 词条 —— "
                "原版词条表在远端 CCD，见 资料/tooltip_原版规格与实现.md §二。"
                "⚠️ 形状是**数组**不是字典 —— `JsonUtility` 读不了字典（同 `card_icon_plan.json`）。",
        "source": "资料/关键词图标/_规则书关键词表.md",
        "entries": [{"zh": k, "en": v["en"], "body": v["body"], "line": v["line"]}
                    for k, v in tips.items()],
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(payload, f, ensure_ascii=False, indent=1)
    print("落盘 %s（%d 条）" % (OUT, len(tips)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
