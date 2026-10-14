#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
_mine_allcards_fields.py  ——  原版 allcards（1195 张卡 SO 全字段）未挖字段盘点抽取器

用途：把 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` 里
      **每一个键**收全、统计非空张数、给取值样例，产出一张可复算的总表。

用法：
    python -I -X utf8 d:/4/Unity/工具/_mine_allcards_fields.py <cards.json> [--used used.txt] [--tsv out.tsv]

  --used  一份文本文件，每行一个「已用过」的顶层字段名（# 开头是注释）。
          给了就多输出一列 USED=Y/N，并只打印未用过的。
  --tsv   把完整表写成 TSV（给报告引用）。

判据出处：字段全来自原版资产本身 = 第一权威。
只读，不写任何 d:/2 或 Assets/ 下的东西。
"""
import sys, json, io, collections, argparse

# ---------------------------------------------------------------- 已用过的字段
# 出处：本轮任务简报 §②「已用掉的那批字段」
USED_DEFAULT = {
    # 单值
    "uniqueId", "cardName", "cardArmy", "cardType", "spellType", "manaCost",
    "meleeAttack", "rangeAttack", "maxHealth", "armour", "keyword", "initiative",
    "activeAbility", "choicesDefinition", "cardTargetCriteria",
    "CardTraits", "ShowTraits", "cardImageReference",
    "summonAudio", "attackAudio", "deathAudio",
    "customRangedAttackParticles", "customDeathParticles", "remnantBodyPrefab",
    "whileInPlayAnim", "goSecondCardInHand", "startingCardInHand",
    "alternateArts",
    "cardDescriptionTextReferenceId",
    # cardAbilities 里面的子键（另有 cardAbilities 本身）
    "cardAbilities", "abilityLogic", "chosenAbility", "targetCriteria",
    "multiplier", "cardAnimations", "chatMessagesReferenceId",
}


def is_empty(v):
    """空 = None / '' / [] / {} ；数字 0 和 False 【算有值】（0 费用、0 攻是合法值）。"""
    if v is None:
        return True
    if isinstance(v, str):
        return v.strip() == ""
    if isinstance(v, (list, dict)):
        return len(v) == 0
    return False


def is_meaningful(v):
    """有意义的取值 = 非空 **且** 不是「全默认」（0 / False / 全空引用 / 全 0 结构）。
    ⚠️ 关键：`extraScoreFromDamaging = 0.0` 在 1192 张卡上都是 0 ⇒ 那是**默认**，
    不是「1192 张都有值」。只看 is_empty 会把它算成「全非空」，读起来像「这字段人人都有值」。"""
    if is_empty(v):
        return False
    if isinstance(v, bool):
        return v
    if isinstance(v, (int, float)):
        return v != 0
    if isinstance(v, str):
        return True
    if isinstance(v, dict):
        return any(is_meaningful(x) for x in v.values())
    if isinstance(v, list):
        return any(is_meaningful(x) for x in v)
    return True


def walk_keys(obj, path, acc):
    """把 (路径, 值) 收集起来；路径用 key / [] 表示列表层级。"""
    if isinstance(obj, dict):
        for k, v in obj.items():
            p = f"{path}.{k}" if path else k
            acc.setdefault(p, [])
            acc[p].append(v)
            walk_keys(v, p, acc)
    elif isinstance(obj, list):
        for v in obj:
            walk_keys(v, path + "[]", acc)


def sample(v):
    s = json.dumps(v, ensure_ascii=False)
    return s if len(s) <= 60 else s[:57] + "..."


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("cards")
    ap.add_argument("--used", default=None)
    ap.add_argument("--tsv", default=None)
    a = ap.parse_args()

    used = set(USED_DEFAULT)
    if a.used:
        for line in io.open(a.used, encoding="utf-8"):
            line = line.strip()
            if line and not line.startswith("#"):
                used.add(line.split("\t")[0].split(" ")[0])

    cards = json.load(io.open(a.cards, encoding="utf-8"))
    print(f"# 卡片数 = {len(cards)}")
    if not isinstance(cards, list):
        print("!! 顶层不是数组，停手"); return 1

    # 顶层键（取并集，卡与卡之间键可能不同）
    toplevel = collections.Counter()
    for c in cards:
        if isinstance(c, dict):
            for k in c:
                toplevel[k] += 1
    print(f"# 顶层键种数 = {len(toplevel)}")

    # 全路径（含嵌套）
    acc = {}
    for c in cards:
        walk_keys(c, "", acc)

    rows = []
    for path, vals in acc.items():
        n_total = len(vals)
        ne = [v for v in vals if not is_empty(v)]
        n_nonempty = len(ne)
        # 样例：最多 3 个互不相同的**非空**值；没有非空就取空值形态一次
        smp, seen = [], set()
        for v in ne:
            s = sample(v)
            if s not in seen:
                seen.add(s); smp.append(s)
            if len(smp) >= 3:
                break
        if not smp:
            s = sample(ne[0] if ne else (vals[0] if vals else None))
            smp = [f"(全空: {s})"]
        # 「已用」按顶层第一段判
        top = path.split(".")[0].split("[]")[0]
        # 有意义计数 + 取值分布（只看顶层字段，嵌套太碎）
        n_mean = sum(1 for v in vals if is_meaningful(v))
        dist = collections.Counter()
        if "." not in path:
            for v in vals:
                if is_empty(v):
                    continue
                try:
                    dist[json.dumps(v, ensure_ascii=False, sort_keys=True)] += 1
                except TypeError:
                    pass
        rows.append({
            "path": path, "top": top, "n_card": n_total, "n_nonempty": n_nonempty,
            "n_mean": n_mean, "ndist": len(dist),
            "top_vals": " ".join(f"{k}×{c}" for k, c in dist.most_common(6)),
            "used": "Y" if (path in used or top in used) else "N",
            "sample": " | ".join(smp),
        })

    rows.sort(key=lambda r: (-r["n_nonempty"], r["path"]))

    if a.tsv:
        with io.open(a.tsv, "w", encoding="utf-8", newline="") as f:
            f.write("path\ttop\tn_card_with_key\tn_nonempty\tn_meaningful\tUSED\tsample\n")
            for r in rows:
                sm = r["sample"].replace("\t", " ").replace("\n", " ")
                f.write(f'{r["path"]}\t{r["top"]}\t{r["n_card"]}\t{r["n_nonempty"]}\t{r["n_mean"]}\t{r["used"]}\t{sm}\n')
        print(f"# TSV -> {a.tsv}")

    # 控制台：先打**顶层未用**的（按非空降序），再打**已用的**（对照）
    def dump(title, pred):
        print("\n" + "=" * 130)
        print("## " + title)
        print("=" * 130)
        print(f'{"字段":40s} {"有意义":>5s}/{"出现":<5s} {"非空":>5s} {"取值种":>5s}  USED  取值分布（前 6）')
        for r in rows:
            if "." in r["path"] or "[]" in r["path"]:
                continue
            if not pred(r):
                continue
            tv = r["top_vals"] if len(r["top_vals"]) <= 62 else r["top_vals"][:59] + "..."
            print(f'{r["path"]:40s} {r["n_mean"]:5d}/{r["n_card"]:<5d} {r["n_nonempty"]:5d} {r["ndist"]:5d}  '
                  f'{r["used"]:<4s} {tv}')

    dump("顶层字段 · 未标为已用（USED=N）", lambda r: r["used"] == "N")
    dump("顶层字段 · 已标为已用（USED=Y）—— 仅作对照", lambda r: r["used"] == "Y")

    # 嵌套路径单独一节（cardAbilities 里面还有多少子键）
    print("\n" + "=" * 100)
    print("## 嵌套路径（含 . 或 []）· 非空 >= 1 · 前 80 条")
    print("=" * 100)
    n = 0
    for r in rows:
        if "." not in r["path"] and "[]" not in r["path"]:
            continue
        if r["n_nonempty"] < 1:
            continue
        sm = r["sample"] if len(r["sample"]) <= 70 else r["sample"][:67] + "..."
        print(f'{r["path"]:56s} {r["n_nonempty"]:5d}/{r["n_card"]:<5d}  {sm}')
        n += 1
        if n >= 80:
            break
    return 0


if __name__ == "__main__":
    sys.exit(main())
