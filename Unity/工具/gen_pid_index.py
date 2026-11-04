#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""allcards pid 索引小表 —— 从 67 MB 的 `cards_raw.json` 派生 `path_id ⇄ uniqueId ⇄ cardName`。

为什么要有这一道（账 `B142-CARDSRAW-SMALL`）：
  `d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（67 MB）在 `.gitignore:274` 里
  ⇒ 新 clone / 换机器上**没有**它 ⇒ `gen_tutorial_stages.py` 的 `load_allcards_pid_index()`
  只剩空表 ⇒ 那 3 张「只被摆进起手」的 pid 又解不出名字（`B142-TUT-HAND3` 干的活白做）。
  本脚本把「pid ⇄ uniqueId ⇄ cardName」这三列（教程侧真正要用的全部）抽成 ~70 KB 小表**进库**，
  换机器也能复现 pid 映射。⛔ 不派生其它列 —— 数值在 `cards.tsv`、卡池真值 id 在 `pool_true_ids.tsv`，
  这一张只回答「这个 PathID 是哪张卡」。

源（**只读**，铁律 2）：`SRC`（`cards_raw.json`，1,351 张，2026 build 的工程内导出）
产物：`DST`（**同目录**小表 —— 与源同一个导出批次放一起，`cards.tsv` / `pool_true_ids.tsv` 也在这个目录；
      该目录**已跟踪**，`.gitignore` 只点名忽略那几个大文件 ⇒ 这张小表会进库）

格式（LF / UTF-8 无 BOM，与同目录 `cards.tsv` 一致）：
    # 两行注释（source + 口径），消费者跳过 `#`
    path_id	uniqueId	cardName
    -9220156507955906393	EC29	Heldrake
    ...                                        （按 `path_id` 的**数值**升序）

用法（🔴 不带显式动作 = 只打用法并 `rc=2`，⛔ 一个字节都不写）：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_pid_index.py --dry
    ... --check   与盘上小表逐行对账（不写盘；不一致 ⇒ rc=1）
    ... --write   显式落盘

判据（为什么不猜、为什么只有三列）：
  · `path_id` = 原版卡 SO 的 `m_PathID`（教程 SO 的 `RawCardScript` PPtr 就指它）；
  · `uniqueId` = 卡 SO 自己的 `uniqueId`，与 `cards_engine.json` 的 `id` 同源
    （同一 CAB `CAB-fb8f01449289635703d83d52c3f18245`）；
  · `cardName` = 卡 SO 的 `cardName`（**不是** `m_Name`；`m_Name` 带 `NN Faction-Name` 前缀）。
  · 三列都**逐字照抄**源里的值，⛔ 不做任何归一化/翻译/回退。
"""
import argparse
import io
import json
import os
import sys

SRC = "d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
DST = "d:/4/Unity/资料/普查产出_1018/allcards_1350/allcards_pid_index.tsv"
COLUMNS = ("path_id", "uniqueId", "cardName")
# 产物头部两行注释（消费者按 `#` 跳过；`--check` 从表头那一行起比）
HEADER_COMMENTS = (
    "# allcards 的 pid 索引（path_id / uniqueId / cardName）—— 由 工具/gen_pid_index.py 从 cards_raw.json 派生，⛔ 别手改",
    "# source=d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json（67 MB · 在 .gitignore 里 ⇒ 换机器没有它时用本表复现 pid 映射）",
)


def load_source(path=SRC):
    """`cards_raw.json` → [(int pid, uniqueId, cardName), ...]（**原样**，不归一化）。"""
    if not os.path.exists(path):
        raise SystemExit("!! 源不在：%s\n   本脚本只做派生，源由 2026 build 的 allcards 导出产出，⛔ 不猜。" % path)
    with io.open(path, "rb") as f:
        doc = json.loads(f.read().decode("utf-8"))
    cards = doc.get("cards")
    if not isinstance(cards, list) or not cards:
        raise SystemExit("!! 源里没有 `cards` 列表：%s" % path)
    rows = []
    for c in cards:
        pid, uid, name = c.get("path_id"), c.get("uniqueId"), c.get("cardName")
        if pid is None or pid == "":
            raise SystemExit("!! 有一条卡没有 `path_id`（uniqueId=%r cardName=%r）" % (uid, name))
        for col, v in (("uniqueId", uid), ("cardName", name)):
            if v is None:
                raise SystemExit("!! path_id=%s 的 `%s` 是 null（源形状变了？⛔ 不猜）" % (pid, col))
            if "\t" in v or "\n" in v or "\r" in v:
                raise SystemExit("!! path_id=%s 的 `%s` 含制表/换行（会切坏 TSV）：%r" % (pid, col, v))
        rows.append((int(pid), uid, name))
    return rows


def validate(rows):
    """→ dict(count, empty_name, dup_pid, dup_uid)。重复一律当**错误**（调用方 rc=1）。"""
    seen_pid, dup_pid = set(), []
    seen_uid, dup_uid = set(), []
    empty_name = 0
    for pid, uid, name in rows:
        if pid in seen_pid:
            dup_pid.append(pid)
        seen_pid.add(pid)
        if uid in seen_uid:
            dup_uid.append(uid)
        seen_uid.add(uid)
        if not name:
            empty_name += 1
    return dict(count=len(rows), empty_name=empty_name, dup_pid=dup_pid, dup_uid=dup_uid)


def render(rows):
    """按 `path_id` 数值升序渲染成小表（LF，末尾一个换行）。"""
    out = list(HEADER_COMMENTS)
    out.append("\t".join(COLUMNS))
    for pid, uid, name in sorted(rows, key=lambda r: r[0]):
        out.append("%d\t%s\t%s" % (pid, uid, name))
    return "\n".join(out) + "\n"


def body_lines(text):
    """去掉 `#` 注释行、去空行 —— `--check` 比的是「表头 + 数据」这一段。"""
    return [ln for ln in text.split("\n") if ln and not ln.startswith("#")]


def read_existing(path=DST):
    if not os.path.exists(path):
        return None
    with io.open(path, "rb") as f:
        return f.read().decode("utf-8")


def main(argv=None):
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--dry", action="store_true", help="只算 + 打印读数，不写盘")
    ap.add_argument("--check", action="store_true", help="与盘上小表逐行对账（不写盘）")
    ap.add_argument("--write", action="store_true", help="显式落盘")
    args, unknown = ap.parse_known_args(argv)
    if unknown or sum((args.dry, args.check, args.write)) != 1:
        sys.stderr.write(
            "用法：python 工具/gen_pid_index.py [--dry | --check | --write]（必须三选一）\n"
            "  --dry    只算 + 打印读数，零写盘\n"
            "  --check  与盘上小表逐行对账（不写盘）；不一致 ⇒ rc=1\n"
            "  --write  显式落盘\n"
            "  源=%s\n  产物=%s\n" % (SRC, DST))
        return 2

    rows = load_source()
    rep = validate(rows)
    text = render(rows)
    n_lines = len(body_lines(text)) - 1          # 数据行数（不含表头）
    size = len(text.encode("utf-8"))
    print("源       %s" % SRC)
    print("卡片     %d 张 -> 数据行 %d · `cardName` 空 %d 张" % (rep["count"], n_lines, rep["empty_name"]))
    print("唯一性   path_id 重复 %d 个 · uniqueId 重复 %d 个"
          % (len(rep["dup_pid"]), len(rep["dup_uid"])))
    print("产物     %s（%d B ≈ %.0f KB）" % (DST, size, size / 1024.0))
    if rep["dup_pid"]:
        print("!! `path_id` 重复（前 5）：%s" % rep["dup_pid"][:5])
    if rep["dup_uid"]:
        print("!! `uniqueId` 重复（前 5）：%s" % rep["dup_uid"][:5])

    if args.dry:
        print("\n干跑（未写盘）：要落盘请显式加 `--write`")
        return 0 if not (rep["dup_pid"] or rep["dup_uid"]) else 1

    if args.check:
        old = read_existing()
        if old is None:
            print("\n--check：盘上没有 %s ❌（重跑时加 `--write` 落盘）" % DST)
            return 1
        same = body_lines(old) == body_lines(text)
        print("\n--check：%s %s"
              % (DST, "一致 ✅" if same else "**已过期** ❌（重跑时加 `--write` 落盘）"))
        if not same:
            a, b = body_lines(old), body_lines(text)
            for i in range(min(len(a), len(b))):
                if a[i] != b[i]:
                    print("  首个不同行 #%d：盘上 %r / 现算 %r" % (i, a[i], b[i]))
                    break
            print("  行数：盘上 %d / 现算 %d" % (len(a), len(b)))
        return 0 if (same and not rep["dup_pid"] and not rep["dup_uid"]) else 1

    # --write
    if rep["dup_pid"] or rep["dup_uid"]:
        print("\n!! 有重复键 ⇒ **拒绝落盘**（pid/uniqueId 是这个映射的唯一键）")
        return 1
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with io.open(DST, "wb") as f:
        f.write(text.encode("utf-8"))
    print("\n写出 %s（%d B · %d 行数据）" % (DST, size, n_lines))
    return 0


if __name__ == "__main__":
    sys.exit(main())
