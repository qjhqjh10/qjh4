# -*- coding: utf-8 -*-
"""把「教程变体」卡（原版 SO 名带 `(tutorial)` / `(tutortial)` 的那批）派生成 **`cards_engine` 同 schema** 的独立表。

跑法（🔴 写盘脚本必须显式给动作档，见 CLAUDE.md §三「写盘类脚本默认只打印用法」）：
    python -I -X utf8 d:/4/Unity/工具/gen_tutorial_variants.py --dry     # 只跑计算、打印读数、零写盘
    python -I -X utf8 d:/4/Unity/工具/gen_tutorial_variants.py --check   # 只对账：与盘上两份产物逐字比（一致 rc=0 / 不一致 rc=1）
    python -I -X utf8 d:/4/Unity/工具/gen_tutorial_variants.py --write   # 真跑并覆盖写两份产物
    ⛔ 一个档都不给（或不认识）= 打印用法并退出 2，不计算、不写盘。

产出（两份，逐字节相同）：
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_variants.json   ← 运行时 `Resources.Load<TextAsset>("tutorial_variants")`
    d:/4/Unity/数据/游戏数据/tutorial_variants.json                        ← 数据快照（脚本产物，⛔ 别手改）

────────────────────────────────────────────────────────────────────────────────────────────
🔴 **为什么必须单独一张表，⛔ 绝不能并进 `cards_engine.json`**
   · 这 18 张是原版**教程关卡专用**的老版本卡（牌组 SO 的 `cardLibraryIds` 里写的是它们的 32 位 GUID）；
     并进可玩池 ⇒ **收藏窗 / 卡组编辑 / 商店会看见它们**（原版里它们对玩家不可见：
     `EffectResolver.IsHiddenCard` 按「卡名以 `(tutorial)` 结尾」把它们藏起来）。
   · 判据：`判据/B142_R14_四查.md` §四 · `报告/B143_W6_存档数据.md` §八（引用链逐跳）。

🔴 **怎么派生的（⛔ 不重写那 8 千行）**
   直接把 `gen_cards_engine.build()` 的**同一套派生逻辑**跑一遍，只是把 18 张变体当成「多出来的统计行」喂进去：
     ① 从 `cards_engine.json` 找到同名池卡，克隆它在 `card_stats.json` 里的那一行（`ocrName` 命中算同类）；
     ② 改名成 SO 的 `cardName`（带 `(tutorial)`）、数值换成 `cards_truth.json` 里**变体自己的**那一行；
     ③ 把「源表 + 18 行」写成临时统计表，`g.SRC` 指过去，调 `g.build()`；
     ④ 只挑出这 18 个 id 的行 ⇒ 其余列（技能树 / 触发 / 目标 / 动画…）**与池卡走完全同一条派生链**。
   `nameZh` / `desc` / `descZh`：**按原版 SO 的引用字段取**（⛔ 不按「像不像」猜）——
     · 中文名：SO `refNameId`（18/18 = 同名池卡的 id）⇒ 取那张池卡的 `nameZh`（原版复用同一份词条）。
     · 卡面描述：SO `cardDescriptionTextReferenceId`（原版取值链 `ref ?? uniqueId`）；
       指向某张池卡 ⇒ 用那张的 `desc`/`descZh`；没有该引用 ⇒ 键 = 自己的 GUID ⇒ 官方表查不到 ⇒ **留空**
       （⛔ 原版就是空的，别从池卡抄文案 —— 那会造出原版没有的文本）。
"""
import contextlib
import importlib.util
import io
import json
import os
import re
import sys
import tempfile

ROOT = "d:/4/Unity"
TRUTH = os.path.join(ROOT, "MyGame/Assets/RuleEngine/Resources/cards_truth.json")
ENGINE = os.path.join(ROOT, "MyGame/Assets/RuleEngine/Resources/cards_engine.json")
STATS = os.path.join(ROOT, "数据/游戏数据/card_stats.json")
SO_RAW = os.path.join(ROOT, "资料/普查产出_1018/allcards_1350/cards_raw.json")   # 原版 SO 快照（取 `refNameId` / 描述引用）
GEN = os.path.join(ROOT, "工具/gen_cards_engine.py")

OUT_RES = os.path.join(ROOT, "MyGame/Assets/RuleEngine/Resources/tutorial_variants.json")
OUT_DATA = os.path.join(ROOT, "数据/游戏数据/tutorial_variants.json")

_USAGE = ("用法：python 工具/gen_tutorial_variants.py {--dry | --check | --write}\n"
          "  --dry   = 只跑计算、打印读数、零写盘\n"
          "  --check = 与盘上两份产物逐字比（一致 rc=0 / 不一致 rc=1），零写盘\n"
          "  --write = 真跑并覆盖写 tutorial_variants.json（Resources + 数据/游戏数据 两份）\n"
          "⛔ 一个都不给 = 只打印本用法并退出 2。")

DRY = "--dry" in sys.argv[1:]
CHECK = "--check" in sys.argv[1:]
WRITE = "--write" in sys.argv[1:]


def _usage(code):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(_USAGE)
    sys.exit(code)


if any(a in ("-h", "--help") for a in sys.argv[1:]):
    _usage(0)
if (DRY + CHECK + WRITE) != 1:
    _usage(2)


def base_name(n):
    """`X (tutorial)` / `X (tutorial1)` / `X (tutortial)` → `X`（⚠️ 原版有一张把名字拼成 `(tutortial)`）。"""
    return re.sub(r"\s*\(tutor.*?\)\s*$", "", n or "", flags=re.I).strip()


def load_json(p):
    return json.load(io.open(p, encoding="utf-8"))


def build_variant_rows():
    """跑一遍 `gen_cards_engine.build()`，只留 18 张变体。返回 (rows, report)。"""
    truth_doc = load_json(TRUTH)
    truth = truth_doc["cards"]
    variants = [c for c in truth
                if isinstance(c, dict) and isinstance(c.get("name"), str)
                and re.search(r"\(\s*tutor", c["name"], re.I)]
    if not variants:
        sys.exit("❌ cards_truth.json 里一张 `(tutorial)` 变体都没有 —— 输入不对，停手。")

    stats = load_json(STATS)
    rows = stats["cards"]
    pool_row_by_name = {}
    for r in rows:
        for k in ("name", "ocrName"):
            nm = base_name(r.get(k) if isinstance(r.get(k), str) else "")
            if nm:
                pool_row_by_name.setdefault(nm, r)

    added, no_stats = [], []
    for c in variants:
        b = base_name(c["name"])
        p = pool_row_by_name.get(b)
        if p is None:
            no_stats.append(c["name"])
            continue
        r = dict(p)
        r["name"] = c["name"]
        r["ocrName"] = c["name"]
        r["cost"] = c.get("manaCost")
        r["attack"] = c.get("melee")
        r["ranged_attack"] = c.get("range")
        r["health"] = c.get("health")
        r["armor"] = c.get("armour")
        r["hasStats"] = True
        r["desc"] = ""
        r["keywords"] = []
        r["keyword"] = ""
        ct = c.get("cardType")
        r["type"] = "hero" if ct == 10 else ("tactic" if ct == 20 else "unit")
        added.append(r)
    if no_stats:
        sys.exit("❌ 这些变体找不到可克隆的统计行（源表里连 ocrName 都对不上）：%s" % no_stats)

    merged = dict(stats)
    merged["cards"] = rows + added
    fd, tmp = tempfile.mkstemp(prefix="wf_tut_variants_stats_", suffix=".json")
    os.close(fd)
    with io.open(tmp, "w", encoding="utf-8") as f:
        json.dump(merged, f, ensure_ascii=False)

    spec = importlib.util.spec_from_file_location("gen_cards_engine", GEN)
    g = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(g)
    g.SRC = tmp                       # ⛔ 只改「读哪张统计表」这一处，派生逻辑一个字不动
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        res = g.build()
    doc = res[0]
    os.remove(tmp)

    want = {c["id"]: c for c in variants}
    got = [x for x in doc["cards"] if x.get("id") in want]
    got.sort(key=lambda x: x["id"])
    missing = sorted(set(want) - {x["id"] for x in got})
    if missing:
        sys.exit("❌ 这些变体没有从 build() 里产出（id 没解析到）：%s" % missing)
    return doc, got, want, variants


def traits_of(rec):
    out = []
    for t in rec.get("cardTraits") or []:
        if isinstance(t, str):
            out.append(t)
        elif isinstance(t, dict):
            out.append(t.get("name"))
    return sorted(x for x in out if x)


def trait_key(s):
    """traits 归一化：引擎侧写成 `Blast 2`（带数值）、真值侧写 `blast` ⇒ 统一成「小写 + 去掉空格与数字」。"""
    return re.sub(r"[^a-z]", "", (s or "").lower())


# 「能力载荷」列 —— ⚠️ 2026-11-05 第二轮：文本列已改由**原版 SO 的引用字段**决定（见 `apply_text_and_verify` 的 A/B），
# 不再用「traits/载荷一样就抄池卡」这条启发式（那会把 4 张 SO 自带描述引用的卡漏掉）。


def load_so_refs():
    """从 `cards_raw.json`（原版 SO 快照）取每张卡的 `refNameId` / `cardDescriptionTextReferenceId`。
    🔴 为什么必须读它：这两格**不在** `cards_truth.json` 里（真值表只落玩法列）。
    判据口径（原版取值链）= `(cardDescriptionTextReferenceId ?? uniqueId)` 拼 `Card_Description/<键>`
    —— `gen_cards_engine.py` v36 那段注释逐字写着，⛔ 没有「ref 缺键 ⇒ 退回自己 id」之外的第三支。"""
    raw = load_json(SO_RAW)["cards"]
    out = {}
    for d in raw:
        uid = d.get("uniqueId")
        if not uid:
            continue
        fr = d.get("full_raw") or {}
        out[uid] = {"refNameId": d.get("refNameId") or "",
                    "descRef": fr.get("cardDescriptionTextReferenceId") or ""}
    return out


def apply_text_and_verify(rows, want, pool_doc, so_refs, report):
    """① 名字/描述按原版 SO 的**引用字段**取（⛔ 不按「像不像」猜）；② 逐条核对数值 / traits。"""
    pool_by_id = {c["id"]: c for c in pool_doc["cards"]}
    pool_by_base = {}
    for c in pool_doc["cards"]:
        pool_by_base.setdefault(base_name(c.get("name") or ""), c)

    text_copied, text_blank = [], []
    bad = []
    for r in rows:
        t = want[r["id"]]
        # ---- `artId`：变体与原版池卡**共用同一张插图**（原版变体 SO 的 `cardSprite` 就是那张）——
        #      运行时 `BattleDriver.ArtKey` 用它把立绘指回池卡 id（否则带 `(tutorial)` 的名字取不到立绘）。
        p_for_art = pool_by_base.get(base_name(t["name"]))
        r["artId"] = p_for_art.get("id") if p_for_art else ""
        # ---- 数值逐项核对（真值 vs 派生行）----
        checks = (("cost", t.get("manaCost")), ("attack", t.get("melee")),
                  ("ranged", t.get("range")), ("health", t.get("health")), ("armour", t.get("armour")))
        for k, exp in checks:
            if r.get(k) != exp:
                bad.append((t["name"], k, r.get(k), exp))
        # ---- traits 逐项核对 ----
        exp_tr = sorted(x.get("name") for x in (t.get("cardTraits") or []) if isinstance(x, dict))
        got_tr = sorted(trait_key(x) for x in traits_of(r))
        exp_tr_l = sorted(trait_key(x) for x in exp_tr)
        if got_tr != exp_tr_l:
            bad.append((t["name"], "cardTraits", got_tr, exp_tr_l))
        # ---- A：中文名 = `refNameId` 指的**那张池卡**的 `nameZh`（原版复用同一份词条）----
        ref = so_refs.get(t["id"], {})
        ref_name = ref.get("refNameId") or r.get("artId") or ""
        if ref_name != (r.get("artId") or ""):
            bad.append((t["name"], "refNameId≠artId", ref_name, r.get("artId")))
        p_name = pool_by_id.get(ref_name)
        r["nameZh"] = (p_name or {}).get("nameZh") or ""
        if not r["nameZh"]:
            bad.append((t["name"], "nameZh", "", "非空"))
        # ---- B：描述 = `cardDescriptionTextReferenceId` 指的卡的文字（空 ⇒ 退回自己 uniqueId ⇒ 查不到 ⇒ 留空）----
        dkey = ref.get("descRef") or t["id"]
        p_desc = pool_by_id.get(dkey)
        if p_desc is not None:
            r["desc"] = p_desc.get("desc") or ""
            r["descZh"] = p_desc.get("descZh") or ""
        else:
            r["desc"] = ""
            r["descZh"] = ""
        # ⚠️ 分类按**结果**算（不是按「有没有引用」）：引用得住但那张池卡本身没文案（例：`UM6` 的 desc 是空串）
        #    ⇒ 卡面还是空的 ⇒ 归「留空」。
        (text_copied if (r["desc"].strip() or r["descZh"].strip()) else text_blank).append(t["name"])
    report["text_copied"] = text_copied
    report["text_blank"] = text_blank
    if bad:
        for b in bad:
            print("❌ 派生行与真值不符：", b)
        sys.exit("❌ 上面 %d 处不符 —— 一个字节都不写。" % len(bad))


def main():
    pool_doc = load_json(ENGINE)
    doc, rows, want, variants = build_variant_rows()
    report = {}
    apply_text_and_verify(rows, want, pool_doc, load_so_refs(), report)

    out = {
        "version": pool_doc.get("version"),
        "source": "cards_truth.json（%d 条 `(tutor…)` 变体，含原版拼写 `(tutortial)` 的那 1 条）+ gen_cards_engine.build() 的同一套派生列" % len(rows),
        "note": "教程关卡专用卡的独立表（⛔ 别并进 cards_engine：收藏/卡组/商店会看见它们）。"
                "生成器 = 工具/gen_tutorial_variants.py；改这张表只改生成器后重跑，⛔ 别手改。",
        "count": len(rows),
        "cards": rows,
    }
    text = json.dumps(out, ensure_ascii=False, indent=1) + "\n"
    data = text.encode("utf-8")

    print("变体数        %d" % len(rows))
    print("数值/traits   ✅ 逐条与 cards_truth 一致（cost/attack/ranged/health/armour + cardTraits）")
    print("中文名        ✅ %d/%d 按 SO `refNameId` 取引用池卡的 `nameZh`" % (len(rows), len(rows)))
    print("卡面描述取回  %d 张（SO `cardDescriptionTextReferenceId` 指向某张池卡 ⇒ 用那张的 desc/descZh）：%s"
          % (len(report["text_copied"]), "、".join(report["text_copied"])))
    print("卡面描述留空  %d 张（SO 没有该引用 ⇒ 原版取值链退回自己 uniqueId ⇒ 官方表里查不到 ⇒ **原版就是空的**）：%s"
          % (len(report["text_blank"]), "、".join(report["text_blank"])))
    print("产物          %d 字节 / %d 行" % (len(data), text.count("\n")))

    if DRY:
        print("\n--dry：零写盘。")
        return

    if CHECK:
        ok = True
        for p in (OUT_RES, OUT_DATA):
            if not os.path.exists(p):
                print("❌ --check：产物不存在 %s" % p)
                ok = False
                continue
            old = io.open(p, "rb").read()
            if old == data:
                print("--check：%s 一致 ✅" % os.path.basename(p))
            else:
                ok = False
                print("❌ --check：%s **不一致**（盘上 %d 字节 / 本次 %d 字节）" % (p, len(old), len(data)))
        sys.exit(0 if ok else 1)

    for p in (OUT_RES, OUT_DATA):
        d = os.path.dirname(p)
        if not os.path.isdir(d):
            sys.exit("❌ 目录不存在：%s" % d)
        with io.open(p, "wb") as f:
            f.write(data)
        print("写入          %s（%d 字节）" % (p, len(data)))


if __name__ == "__main__":
    main()
