#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
export_allcards.py —— 原版 allcards bundle（UnityFS / Addressables `__data`）→ 全字段卡 SO 导出

这是 K02：把 2026-10-18 散在 `d:/4/_tmp_view/_allcards_*.py` 的临时脚本固化成正式工具。
**只读 d:/2**（源 bundle），**只写 `--out`**（默认 `d:/4/_tmp_view/allcards_export/`，不进 git）。

用法（路径全部写死以 `d:/4/Unity` 为根，不依赖当前工作目录）：
    python -I -X utf8 d:/4/Unity/工具/export_allcards.py
    python -I -X utf8 d:/4/Unity/工具/export_allcards.py --bundle <__data> --out <dir>
    python -I -X utf8 d:/4/Unity/工具/export_allcards.py --check      # 与现有 cards_raw.json 对账

产出（写盘是原子的：先写 `<name>.tmp`，成功后 os.replace；**失败绝不留下半截文件**）：
    <out>/cards_raw.json   全字段 JSON（每张卡 = 摘要字段 + cardAbilities/CardTraits/activeAbility
                           + 全部 `m_AssetGUID`（带 JSON 路径）+ `full_raw`（SO 原始 typetree 全字段））
    <out>/cards.tsv        一行一张卡的摘要 TSV（给人工过目 / 给别的脚本当索引）
    <out>/report.json      （仅 --check）对账结果机读版

判据 / 出处：
  * 卡 SO 的识别键 = `m_Script.m_PathID == 8864902669260471755`
    （`资料/普查产出_1017/卡牌ID/R_SO与触发代码链_可行性.md:31` ·
     `资料/普查产出_1018/allcards_1350/报告.md:37`）
  * 当前 build 的 allcards 包路径与内容见 `资料/普查产出_1018/allcards_1350/报告.md:9-11`
  * 字段清单见 `资料/卡牌制作_正本.md` §2·2（正本 = 解包字段实测）
  * 「非空」判据沿用 `工具/_mine_allcards_fields.py:45-53`：None / "" / [] / {} = 空；
    **数字 0 与 False 算有值**（0 费、0 攻是合法值）

已知坑（别重犯）：
  * **没有 type tree 的原始文件**要用别的手段解（`m_Script` pathID 找实例、`m_FileID` 按 externals 解）
    —— 本工具的做法是：逐对象 `read_typetree()`，**读不出来就出声记账**；若一张卡都没读到，
    **直接非 0 退出、不写任何产物**，并把「看到了哪些 m_Script pathID / 第一个异常」打出来
    （⛔ 不许静默导出 0 张卡）。
  * `m_Name` ≠ id：SO 资产名形如 `33 EmperorsChildren-Heldrake`，主键是 `uniqueId`
    （正本 §1·1）。
  * `m_AssetGUID` 在 SO 里**到处都是**（每个 `AssetReferenceTyped` 都有这个键，**空串也序列化**）
    ⇒ 本工具把**全部出现**都带 JSON 路径收进 `assetGuids`，另建「非空 GUID」索引。
"""
import argparse
import collections
import json
import os
import sys
import time

ROOT = "d:/4/Unity"                      # 🔴 工程根：所有默认路径以它为准（不依赖 cwd）
DEFAULT_BUNDLE = ("D:/2/关服前存档/新建文件夹/Everguild_Warpforge/"
                  "54bdb7b1114a2de41519cac560a4bce4/"
                  "ea8bca4f5c6909d3124fd4975dc96173/__data")   # 🔴 只读
DEFAULT_OUT = "d:/4/_tmp_view/allcards_export"
REFERENCE_CARDS = os.path.join(ROOT, "资料/普查产出_1018/allcards_1350/cards_raw.json")
FORBIDDEN_WRITE_ROOT = "d:/2"            # 铁律：⛔ 不写 d:/2

CARD_SCRIPT_PATH_ID = 8864902669260471755

SCHEMA = "allcards_export/v1"

# 摘要字段（放在 full_raw 之外，方便别的工具直接读；取值一律来自同一个 typetree）
SUMMARY_FIELDS = [
    "uniqueId", "cardName", "cardArmy", "cardType", "spellType", "cardRarity",
    "keyword", "manaCost", "meleeAttack", "rangeAttack", "maxHealth", "armour",
    "initiative", "attackDamageType", "expansionPack", "inventoryOptions",
    "showInCollectionOptions", "cardShortId", "cantBeRandomlyCreated", "useOverDraw",
    "refNameId", "nameModifier", "hasActiveAbility", "hasAlternativeAbility",
    "targeted", "altArtFromId", "questTimes",
]

# 逐卡表现层字段（AssetReferenceTyped / 直接引用）；导出时拍平成 `assets`
ASSET_FIELDS = [
    "cardImageReference", "fullHeroSpriteReference",
    "summonAudio", "attackAudio", "deathAudio", "summonAnimDelayed",
    "customRangedAttackParticles", "customDeathParticles", "remnantBodyPrefab",
]

TSV_COLUMNS = [
    "uniqueId", "cardName", "cardArmy", "cardType", "spellType", "cardRarity", "keyword",
    "cost", "melee", "ranged", "health", "armour", "initiative", "attackDamageType",
    "expansionPack", "inventoryOptions", "showInCollectionOptions",
    "hasActiveAbility", "hasAlternativeAbility", "abilityCount", "traitCount",
    "guidCount", "m_Name", "path_id",
]


# ------------------------------------------------------------------ 小工具
def die(msg, code=2):
    sys.stderr.write("ERROR: %s\n" % msg)
    sys.exit(code)


def is_under(path, root):
    p = os.path.normcase(os.path.abspath(path)).replace("\\", "/").rstrip("/")
    r = os.path.normcase(os.path.abspath(root)).replace("\\", "/").rstrip("/")
    return p == r or p.startswith(r + "/")


def is_empty(v):
    """空 = None / '' / [] / {}。数字 0 与 False 【算有值】（出处：_mine_allcards_fields.py）。"""
    if v is None:
        return True
    if isinstance(v, str):
        return v.strip() == ""
    if isinstance(v, (list, dict)):
        return len(v) == 0
    return False


def atomic_write_text(path, text):
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    os.replace(tmp, path)


def walk_asset_guids(obj, path, acc):
    """把 typetree 里**每一个** `m_AssetGUID` 收进 acc（带 JSON 路径）。空串也收。"""
    if isinstance(obj, dict):
        if isinstance(obj.get("m_AssetGUID"), str):
            acc.append({
                "field": path,
                "guid": obj.get("m_AssetGUID"),
                "subObjectName": obj.get("m_SubObjectName", ""),
                "subObjectType": obj.get("m_SubObjectType", ""),
            })
        for k, v in obj.items():
            walk_asset_guids(v, (path + "." + k) if path else k, acc)
    elif isinstance(obj, list):
        for i, v in enumerate(obj):
            walk_asset_guids(v, "%s[%d]" % (path, i), acc)


def as_guid(v):
    """把 AssetReferenceTyped 形态或裸 GUID 字符串统一成 GUID（没有就 None）。"""
    if isinstance(v, dict):
        g = v.get("m_AssetGUID")
        return g if isinstance(g, str) and g else None
    if isinstance(v, str) and v:
        return v
    return None


# ------------------------------------------------------------------ 解析
def extract(bundle, script_path_id):
    import UnityPy   # 延迟导入：`--help` 不需要它

    env = UnityPy.load(bundle)
    if env is None or not getattr(env, "objects", None):
        raise RuntimeError("UnityPy.load() 没有返回任何对象：%s（不是 UnityFS/AssetBundle？）" % bundle)
    hist = collections.Counter()
    script_hist = collections.Counter()
    typetree_errors = []
    others = []
    cards = []

    for obj in env.objects:
        t = obj.type.name
        hist[t] += 1
        if t != "MonoBehaviour":
            continue
        try:
            tt = obj.read_typetree()
        except Exception as e:                       # ⛔ 出声，不静默跳过
            typetree_errors.append({
                "path_id": getattr(obj, "path_id", None),
                "error": "%s: %s" % (type(e).__name__, e),
            })
            continue
        script = tt.get("m_Script") if isinstance(tt.get("m_Script"), dict) else {}
        pid = script.get("m_PathID")
        script_hist[pid] += 1
        if pid != script_path_id:
            others.append({
                "path_id": getattr(obj, "path_id", None),
                "m_Script": pid,
                "m_Name": tt.get("m_Name"),
                "keys": len(tt),
            })
            continue

        guids = []
        walk_asset_guids(tt, "", guids)
        abilities = tt.get("cardAbilities") or []
        traits = tt.get("CardTraits") or []
        show_traits = tt.get("ShowTraits") or []

        card = {"path_id": getattr(obj, "path_id", None), "m_Name": tt.get("m_Name")}
        for f in SUMMARY_FIELDS:
            card[f] = tt.get(f)
        card["abilityCount"] = len(abilities)
        card["traitCount"] = len(traits)
        card["showTraitCount"] = len(show_traits)
        card["assetGuidCount"] = sum(1 for g in guids if g["guid"])
        card["assets"] = {f: as_guid(tt.get(f)) for f in ASSET_FIELDS}
        # 规则效果 / 词条 / 主动技 —— 明文要求必含这三块
        card["cardAbilities"] = abilities
        card["activeAbility"] = tt.get("activeAbility")
        card["alternativeAbility"] = tt.get("alternativeAbility")
        card["CardTraits"] = traits
        card["ShowTraits"] = show_traits
        # 所有 m_AssetGUID（含空串，带 JSON 路径）
        card["assetGuids"] = guids
        card["full_raw"] = tt
        cards.append(card)

    return {
        "env": env,
        "hist": hist,
        "script_hist": script_hist,
        "typetree_errors": typetree_errors,
        "others": others,
        "cards": cards,
    }


def bundle_name(env):
    try:
        ab = env.objects and next((o for o in env.objects if o.type.name == "AssetBundle"), None)
        if ab is not None:
            return ab.read().m_Name
    except Exception:
        pass
    try:
        return os.path.basename(list(env.files.keys())[0])
    except Exception:
        return None


# ------------------------------------------------------------------ 写盘
def build_document(bundle, res, script_path_id):
    cards = sorted(res["cards"], key=lambda c: (c.get("uniqueId") or "", c.get("path_id") or 0))
    index = collections.defaultdict(list)
    for c in cards:
        for g in c["assetGuids"]:
            if g["guid"]:
                index[g["guid"]].append({"uniqueId": c.get("uniqueId"), "field": g["field"]})
    return {
        "schema": SCHEMA,
        "generatedBy": "d:/4/Unity/工具/export_allcards.py",
        "generatedAt": time.strftime("%Y-%m-%d %H:%M:%S"),
        "bundle": os.path.abspath(bundle),
        "bundleName": bundle_name(res["env"]),
        "script_path_id": str(script_path_id),
        "count": len(cards),
        "object_hist": dict(res["hist"]),
        "m_script_hist": {str(k): v for k, v in res["script_hist"].items()},
        "typetree_errors": res["typetree_errors"],
        "other_monobehaviours": res["others"],
        "asset_guid_index": dict(sorted(index.items())),
        "cards": cards,
    }


def build_tsv(doc):
    lines = ["\t".join(TSV_COLUMNS)]
    for c in doc["cards"]:
        row = {
            "cost": c.get("manaCost"), "melee": c.get("meleeAttack"),
            "ranged": c.get("rangeAttack"), "health": c.get("maxHealth"),
            "armour": c.get("armour"),
        }
        vals = []
        for col in TSV_COLUMNS:
            v = row.get(col, c.get(col))
            vals.append("" if v is None else str(v).replace("\t", " ").replace("\n", " "))
        lines.append("\t".join(vals))
    return "\n".join(lines) + "\n"


def write_products(doc, tsv, outdir):
    os.makedirs(outdir, exist_ok=True)
    json_path = os.path.join(outdir, "cards_raw.json")
    tsv_path = os.path.join(outdir, "cards.tsv")
    # 先序列化到内存（失败就不落盘），再原子写
    text = json.dumps(doc, ensure_ascii=False, default=str)
    atomic_write_text(json_path, text)
    atomic_write_text(tsv_path, tsv)
    return json_path, tsv_path


# ------------------------------------------------------------------ 对账
def load_reference(path):
    if not os.path.isfile(path):
        die("对账基准不存在：%s" % path)
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    if not isinstance(d, dict) or not isinstance(d.get("cards"), list):
        die("对账基准形状不对（期望 {bundle,count,cards:[…]}）：%s" % path)
    return d


def nonempty_field_counts(cards):
    """每张卡：full_raw 里非空顶层字段数。"""
    out = {}
    for c in cards:
        raw = c.get("full_raw") or {}
        out[c.get("uniqueId")] = sum(1 for v in raw.values() if not is_empty(v))
    return out


def per_field_counts(cards):
    """每个顶层字段：有多少张卡非空。"""
    cnt = collections.Counter()
    total = collections.Counter()
    for c in cards:
        for k, v in (c.get("full_raw") or {}).items():
            total[k] += 1
            if not is_empty(v):
                cnt[k] += 1
    return cnt, total


def run_check(doc, ref_path):
    ref = load_reference(ref_path)
    ref_cards = ref["cards"]
    new_cards = doc["cards"]
    problems = []
    notes = []

    # 1) 卡数
    if len(new_cards) != len(ref_cards):
        problems.append("卡数不一致：新导出 %d / 基准 %d" % (len(new_cards), len(ref_cards)))
    else:
        notes.append("卡数一致：%d" % len(new_cards))

    # 2) uniqueId 集
    ids_new = {c.get("uniqueId") for c in new_cards}
    ids_ref = {c.get("uniqueId") for c in ref_cards}
    only_new = sorted(x for x in (ids_new - ids_ref) if x)
    only_ref = sorted(x for x in (ids_ref - ids_new) if x)
    if only_new or only_ref:
        problems.append("uniqueId 集有差异：仅新导出 %d 个 %s / 仅基准 %d 个 %s"
                        % (len(only_new), only_new[:20], len(only_ref), only_ref[:20]))
    else:
        notes.append("uniqueId 集 0 差异（%d 个）" % len(ids_new))

    # 3) 每卡字段非空数
    ne_new = nonempty_field_counts(new_cards)
    ne_ref = nonempty_field_counts(ref_cards)
    diff_cards = []
    for uid in sorted(set(ne_new) & set(ne_ref)):
        if ne_new[uid] != ne_ref[uid]:
            diff_cards.append({"uniqueId": uid, "new": ne_new[uid], "ref": ne_ref[uid]})
    if diff_cards:
        problems.append("每卡字段非空数有差异：%d 张（前 20：%s）"
                        % (len(diff_cards), diff_cards[:20]))
    else:
        notes.append("每卡字段非空数全部一致（%d 张，分布 %s）"
                     % (len(ne_new), dict(sorted(collections.Counter(ne_new.values()).items()))))

    # 4) 每字段非空数（补充，比「每卡数」定位更细）
    f_new, t_new = per_field_counts(new_cards)
    f_ref, t_ref = per_field_counts(ref_cards)
    field_diff = []
    for k in sorted(set(t_new) | set(t_ref)):
        if f_new.get(k, 0) != f_ref.get(k, 0):
            field_diff.append({"field": k, "new": f_new.get(k, 0), "ref": f_ref.get(k, 0)})
    if field_diff:
        problems.append("字段级非空数有差异：%d 个字段（前 20：%s）"
                        % (len(field_diff), field_diff[:20]))
    else:
        notes.append("字段级非空数全部一致（顶层字段 %d 个，其中 %d 个至少一张卡非空）"
                     % (len(t_new), len(f_new)))

    # 5) 顶层键集
    keys_new = set(t_new)
    keys_ref = set(t_ref)
    if keys_new != keys_ref:
        problems.append("顶层字段键集有差异：仅新 %s / 仅基准 %s"
                        % (sorted(keys_new - keys_ref)[:20], sorted(keys_ref - keys_new)[:20]))
    else:
        notes.append("顶层字段键集一致（%d 个）" % len(keys_new))

    return {
        "reference": ref_path,
        "reference_count": len(ref_cards),
        "export_count": len(new_cards),
        "uniqueId_only_export": only_new,
        "uniqueId_only_reference": only_ref,
        "per_card_nonempty_diff": diff_cards,
        "per_field_nonempty_diff": field_diff,
        "notes": notes,
        "problems": problems,
        "ok": not problems,
    }


# ------------------------------------------------------------------ main
def main(argv=None):
    ap = argparse.ArgumentParser(description="allcards bundle → 全字段卡 SO 导出 / 对账")
    ap.add_argument("--bundle", default=DEFAULT_BUNDLE, help="Allcards UnityFS `__data`（默认=当前 build）")
    ap.add_argument("--out", default=DEFAULT_OUT, help="输出目录（默认 d:/4/_tmp_view/allcards_export）")
    ap.add_argument("--script-path-id", default=str(CARD_SCRIPT_PATH_ID),
                    help="RawCardScript 的 m_Script.m_PathID")
    ap.add_argument("--check", action="store_true",
                    help="与 %s 对账（卡数 / uniqueId 集 / 每卡字段非空数）" % REFERENCE_CARDS)
    ap.add_argument("--reference", default=REFERENCE_CARDS, help="对账基准 cards_raw.json")
    ap.add_argument("--limit", type=int, default=0, help="只导出前 N 张（调试用；与 --check 互斥）")
    ap.add_argument("--no-write", action="store_true", help="只解析不写盘（配合 --check 试跑）")
    args = ap.parse_args(argv)

    try:
        script_path_id = int(args.script_path_id)
    except ValueError:
        die("--script-path-id 不是整数：%r" % args.script_path_id)

    if args.limit and args.check:
        die("--limit 与 --check 互斥（限流后卡数必然对不上）")
    if not os.path.isfile(args.bundle):
        die("bundle 不存在：%s" % args.bundle)
    if is_under(args.out, FORBIDDEN_WRITE_ROOT):
        die("⛔ 拒绝写 d:/2（--out=%s）" % args.out)

    t0 = time.time()
    try:
        res = extract(args.bundle, script_path_id)
    except Exception as e:
        die("解析 bundle 失败（%s: %s）—— 未写任何产物" % (type(e).__name__, e))

    if not res["cards"]:
        sys.stderr.write("ERROR: 一张 RawCardScript 都没读到 —— 拒绝写产物。\n")
        sys.stderr.write("  bundle: %s\n" % os.path.abspath(args.bundle))
        sys.stderr.write("  对象类型直方图: %s\n" % dict(res["hist"]))
        sys.stderr.write("  m_Script pathID 直方图: %s\n" % dict(res["script_hist"]))
        if res["typetree_errors"]:
            sys.stderr.write("  第一个 typetree 异常: %s\n" % res["typetree_errors"][0])
        sys.stderr.write("  ⇒ 若 pathID 直方图里有一个大数（≠%s），用 --script-path-id 指定它；\n"
                         "     若 typetree 全失败 = 该包没有 type tree，需要另一套解析路径（别静默跳过）。\n"
                         % script_path_id)
        return 2

    if args.limit:
        res["cards"] = sorted(res["cards"], key=lambda c: (c.get("uniqueId") or ""))[: args.limit]

    doc = build_document(args.bundle, res, script_path_id)
    tsv = build_tsv(doc)

    json_path = tsv_path = None
    if not args.no_write:
        json_path, tsv_path = write_products(doc, tsv, args.out)
    dt = time.time() - t0

    print("bundle      : %s" % doc["bundle"])
    print("bundleName  : %s" % doc["bundleName"])
    print("对象直方图  : %s" % doc["object_hist"])
    print("typetree 异常: %d" % len(doc["typetree_errors"]))
    print("非卡 MB     : %d %s" % (len(doc["other_monobehaviours"]),
                                   [o.get("m_Name") for o in doc["other_monobehaviours"][:8]]))
    print("卡数        : %d" % doc["count"])
    ids = collections.Counter(c.get("uniqueId") for c in doc["cards"] if c.get("uniqueId"))
    print("uniqueId    : 非空 %d / 不同 %d / 重复 %s"
          % (sum(1 for c in doc["cards"] if c.get("uniqueId")), len(ids),
             [k for k, v in ids.items() if v > 1][:10] or "无"))
    print("GUID 索引   : %d 个不同非空 GUID / %d 条引用"
          % (len(doc["asset_guid_index"]),
             sum(len(v) for v in doc["asset_guid_index"].values())))
    if json_path:
        print("写出        : %s (%d B)" % (json_path, os.path.getsize(json_path)))
        print("写出        : %s (%d B)" % (tsv_path, os.path.getsize(tsv_path)))
    else:
        print("（--no-write：未写盘）")
    print("耗时        : %.1fs" % dt)

    if not args.check:
        return 0

    print("\n=== --check：与 %s 对账 ===" % args.reference)
    result = run_check(doc, args.reference)
    for n in result["notes"]:
        print("  [OK]   %s" % n)
    for p in result["problems"]:
        print("  [DIFF] %s" % p)
    if not args.no_write:
        rp = os.path.join(args.out, "report.json")
        atomic_write_text(rp, json.dumps(result, ensure_ascii=False, indent=1))
        print("对账结果 -> %s" % rp)
    print("结论：%s" % ("一致 ✅" if result["ok"] else "有差异 ❌（见上）"))
    return 0 if result["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())
