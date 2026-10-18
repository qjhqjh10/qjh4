#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""migrate_pool_ids_k06.py —— 把**我们卡池的旧 id**改成原版真 id（`K06` 一次性迁移）

## 为什么要有这一份

`K06`：池里有 **114 张**卡的 id 还是旧编号（SW/UM/DA 整族 + 若干 hex），真值在
`数据/游戏数据/card_ids.json` 的 **`poolTrueIds`**。2026-10-18 把两个生成器的真值源
换成当前 build 的 `allcards`（1,351 张）之后，`cards_engine.json` 重生成**自动**落到了真 id ——
实测 **114 张全改、与 `poolTrueIds` 逐条一致（0 处不符）**。

但 id 是**一张卡的身份**，改完之后所有「按 id 写的静态产物」都要跟着走：
  · `.cs` 里硬编码的卡 id（断言 / 特判 / 注释）—— ⛔ 不跟就会「查不到那张卡」
  · `Resources/CardPresentation/.../voice_lines.json` 的 `cards[].id`
  · `Resources/Art/cards/art_<id>.png`（+ `.meta`）—— **按 id 命名**，不跟就是孤儿图
  · 生成器里的硬编码表（`gen_icon_plan.py` 的还原表 / `gen_trait_dict.py` 的 ferocity 表）
生成出来的表（vfx / icon_plan / anim_order / ability_anim_durations / prebuilt_decks /
initiative / inventory_options / related_cards / talent_cards / lore）**不在这里改** ——
它们由各自的生成器从 `cards_engine.json` 重出（⛔ 别手改，两份会分叉）。

## 🔴 为什么必须**一次性并发**替换（不能一条一条顺序改）

`poolTrueIds` 里有 **73 条链**：例 `SW32 → SW24`，而 `SW24 → SW19`。
顺序替换会把 `SW32` 先改成 `SW24`、再被下一条改成 `SW19` ⇒ **静默改成第三张卡**。
本脚本用「原文 → 一次性正则替换（回调查表）」：每个 token 只按**它原来的值**换一次。

## ⛔ 不碰的东西（它们用的是**原版真 id**，不是我们的池 id）

`官方本地化_*.json` · `card_ids.json` · `decks.json` · `decklists.json` ·
`prebuilt_decks_full.json` · `数据/索引/card_index.json` · `资料/**` · `_oneoff_*` 历史脚本。

用法：
    "D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/migrate_pool_ids_k06.py --dry
    ... --apply     # 真改（每个文件先备份到 _tmp_view/backup_1018/k06/replace/）
"""
import argparse
import io
import json
import os
import re
import shutil
import sys

ROOT = "d:/4/Unity"
OLD_POOL = "d:/4/_tmp_view/backup_1018/k06/cards_engine.json.BEFORE"
NEW_POOL = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
IDS_JSON = ROOT + "/数据/游戏数据/card_ids.json"
BACKUP = "d:/4/_tmp_view/backup_1018/k06/replace"
ART = ROOT + "/MyGame/Assets/CardPresentation/Resources/Art/cards"
VOICE = ROOT + "/MyGame/Assets/CardPresentation/Resources/voice_lines.json"

CODE_ROOTS = [
    (ROOT + "/MyGame/Assets", ".cs"),
    (ROOT + "/工具", ".py"),
    (ROOT + "/工具/ruleprobe", ".cs"),
]
SKIP_DIR_PARTS = ("/Library/", "/obj/", "/Temp/", "/_tmp_view/", "/资料/",
                  "/_oneoff_", "/node_modules/", "/.git/")
SELF_NAME = "migrate_pool_ids_k06.py"


def id_map():
    old = json.load(io.open(OLD_POOL, encoding="utf-8"))["cards"]
    new = json.load(io.open(NEW_POOL, encoding="utf-8"))["cards"]

    def key(c):
        return (c.get("name"), c.get("faction"), c.get("type"))

    nk = {key(c): c["id"] for c in new}
    if len(nk) != len(new):
        sys.exit("❌ 新卡池里 (name,faction,type) 有重复 —— 不能拿它建映射")
    out = {}
    for c in old:
        nid = nk.get(key(c))
        if not nid:
            sys.exit("❌ 旧卡在新卡池里找不到：%s / %s" % (c.get("name"), c.get("faction")))
        if nid != c["id"]:
            out[c["id"]] = nid
    return out


def verify(mp):
    pt = json.load(io.open(IDS_JSON, encoding="utf-8"))["poolTrueIds"]
    bad = [(k, v, pt.get(k)) for k, v in mp.items() if pt.get(k) != v]
    if not bad:
        return
    print("❌ 映射与 `card_ids.json.poolTrueIds` 不一致（%d 处）：" % len(bad))
    for x in bad[:10]:
        print("   ", x)
    sys.exit(1)


def compile_re(mp):
    keys = sorted(mp, key=len, reverse=True)
    return re.compile(r"(?<![A-Za-z0-9_])(" + "|".join(re.escape(k) for k in keys)
                      + r")(?![A-Za-z0-9_])")


def iter_files():
    seen = set()
    self_path = os.path.abspath(__file__)
    for root, ext in CODE_ROOTS:
        for dp, dn, fn in os.walk(root):
            plat = dp.replace("\\", "/") + "/"
            if any(s in plat for s in SKIP_DIR_PARTS):
                continue
            for f in fn:
                if not f.endswith(ext):
                    continue
                # ⛔ 历史一次性脚本（`_` 开头）不改：它们是**记录**，不是活代码
                if f.startswith("_") or f == SELF_NAME:
                    continue
                p = os.path.join(dp, f)
                if os.path.abspath(p) == self_path or p in seen:
                    continue
                seen.add(p)
                yield p


def backup(path):
    rel = os.path.relpath(path, ROOT).replace("\\", "_").replace("/", "_")
    os.makedirs(BACKUP, exist_ok=True)
    dst = os.path.join(BACKUP, rel + ".BEFORE")
    if not os.path.exists(dst):
        shutil.copy2(path, dst)


def replace_text(path, cre, mp, apply):
    """返回 (命中数, 明细 Counter)。⛔ 用二进制读写，行尾原样保留。"""
    raw = io.open(path, "rb").read()
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        return 0, {}
    hits = {}

    def sub(m):
        k = m.group(1)
        hits[k] = hits.get(k, 0) + 1
        return mp[k]

    new = cre.sub(sub, text)
    n = sum(hits.values())
    if n and apply:
        backup(path)
        io.open(path, "wb").write(new.encode("utf-8"))
    return n, hits


def migrate_voice(mp, apply):
    d = json.load(io.open(VOICE, encoding="utf-8"))
    cards = d["cards"]
    changed, unknown = 0, []
    for c in cards:
        old = c.get("id")
        if old in mp:
            c["id"] = mp[old]
            changed += 1
    ids = {c.get("id") for c in cards}
    pool = {x["id"] for x in json.load(io.open(NEW_POOL, encoding="utf-8"))["cards"]}
    for x in sorted(ids - pool):
        unknown.append(x)
    print("voice_lines.json：改 id %d 条 · 改完不在新池里的 %d 条 %s"
          % (changed, len(unknown), unknown[:6]))
    if apply and changed:
        backup(VOICE)
        with io.open(VOICE, "w", encoding="utf-8", newline="") as fh:
            json.dump(d, fh, ensure_ascii=False, indent=2)
            fh.write("\n")


def migrate_art(mp, apply):
    # ⚠️ 文件名里的 id 是**小写**（`art_da76.png`），映射表里是原样大小写。
    have = {f.lower() for f in os.listdir(ART) if f.endswith(".png")}
    pairs = [(("art_%s.png" % o.lower()), ("art_%s.png" % n.lower()))
             for o, n in mp.items()]
    todo = [(o, n) for o, n in pairs if o in have]
    miss = [o for o, n in pairs if o not in have]
    print("立绘：要迁 %d 张 · 旧 id 没图的 %d 张 %s" % (len(todo), len(miss), miss[:6]))
    if not apply:
        print("  （--dry：不动文件）")
        return
    # ① 全部先改成临时名（避免链式覆盖）
    tmp = []
    for o, n in todo:
        for ext in ("", ".meta"):
            src = os.path.join(ART, o + ext)
            if not os.path.exists(src):
                continue
            t = os.path.join(ART, o.replace(".png", "__K06TMP.png") + ext)
            os.replace(src, t)
            tmp.append((t, os.path.join(ART, n + ext)))
    # ② 临时名 → 新 id（覆盖旧目标）
    for t, dst in tmp:
        if os.path.exists(dst):
            os.remove(dst)
        os.replace(t, dst)
    print("  ✅ 已迁 %d 个文件（含 .meta）" % len(tmp))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true", help="真写；缺省 = 只报告")
    args = ap.parse_args()
    apply = args.apply
    if not os.path.exists(OLD_POOL):
        sys.exit("❌ 缺旧卡池备份：%s" % OLD_POOL)
    mp = id_map()
    verify(mp)
    # 🔴 **只跑一次**：迁移完成之后，映射里的「旧 id」会变成**别的卡的新 id**（73 条链），
    #   再跑一次就会把卡改成第三张。这里用「当前卡池 == 期望的新 id 集合」判重跑。
    old = json.load(io.open(OLD_POOL, encoding="utf-8"))["cards"]
    cur = {c["id"] for c in json.load(io.open(NEW_POOL, encoding="utf-8"))["cards"]}
    expected = {mp.get(c["id"], c["id"]) for c in old}
    if cur == expected:
        sys.exit("⛔ 这份迁移**已经做过**（当前卡池 = 期望的新 id 集合）—— 再跑会把 73 条链改成第三张卡，拒绝执行。")
    print("映射 %d 条（与 `poolTrueIds` 逐条一致 ✅）" % len(mp))
    cre = compile_re(mp)

    total_files = total_hits = 0
    for p in iter_files():
        n, hits = replace_text(p, cre, mp, apply)
        if n:
            total_files += 1
            total_hits += n
            rel = os.path.relpath(p, ROOT).replace("\\", "/")
            print("  %-4d %s  %s" % (n, rel, ",".join(sorted(hits)[:5])))
    print("代码/工具：%d 个文件 · %d 处（%s）"
          % (total_files, total_hits, "已写" if apply else "dry-run"))

    migrate_voice(mp, apply)
    migrate_art(mp, apply)
    if not apply:
        print("\n（dry-run 结束；加 --apply 才写）")


if __name__ == "__main__":
    main()
