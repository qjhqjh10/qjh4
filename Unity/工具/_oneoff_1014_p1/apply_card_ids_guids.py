# -*- coding: utf-8 -*-
"""P1 · 把真值里那 259 条 **GUID 形 `uniqueId`** 收进 `card_ids.json` 的 `mapping`，并改 `note`。

判据（`资料/普查产出_第三十会话/卡表块_施工设计_1014.md` §七 已裁 ②）：
  **GUID 型 `uniqueId` 就是游戏的合法卡 id** —— `CardTypeOptions`（Minion 0 / Hero 10 / **Tactic 20**）
  分出来 259 条全是真卡（Tactic 232 · Minion 17 · Hero 10，259/259 有卡名）；
  `decklists.json` 里实有 98 个 GUID 形态 id，其中 **26 个与真值 `uniqueId` 重合**
  ⇒ 收了这 259 行，`card_ids.json` 当桥时能多解出那 26 条。

⛔ 只**增行 + 改 note**：现有 1058 行（933 真值短 id + 123 旧表残留 + 2 个 `#HB_` 标记）**一字不动**。
（旧表残留里 DA65/DA66/DA75/SW* 那一批 `decklists.json` 还在引用 —— 见施工设计 §五·4 的取舍。）

用法：python -I -X utf8 d:/4/Unity/工具/_oneoff_1014_p1/apply_card_ids_guids.py [--dry]
"""
import argparse
import collections
import io
import json
import re
import sys

IDS = "d:/4/Unity/数据/游戏数据/card_ids.json"
TRUTH = "d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json"
ADD_NOTE = (
    "🆕 **2026-10-14（第三十一会话 · P1 id 置换批 · 账 `A1800`/`A1782`）**：按已裁裁定"
    "（`资料/普查产出_第三十会话/卡表块_施工设计_1014.md` §七·②）**把真值里 259 条 32 位 GUID 形 `uniqueId` 收进本表**"
    "（值 = 真值 `cardName`）—— 判据：`CardTypeOptions` 分出来 259 条全是真卡（Tactic 232 · Minion 17 · Hero 10，259/259 有卡名），"
    "且 `decklists.json` 里实有 98 个 GUID 形态 id、其中 26 个与真值 `uniqueId` 重合 ⇒ GUID 是**游戏内卡 id 的合法形态**。"
    "本表因此从 1,058 行变成 {n} 行（933 真值短 id + 259 真值 GUID + 123 旧表残留 + 2 个 `#HB_` 标记）。"
    "⚠️ **池内 id 的判定不再只靠本表** —— 另有一层真值索引 "
    "（`Unity/数据/游戏数据/card_id_truth_index.json`，生成器 `工具/gen_card_id_truth_index.py`），"
    "它管三类按卡名查不到的卡（**我们卡名 = 美术/VO 资产名** · **GUID 形 id** · **同名组里的附魔/教程副本**）；"
    "`gen_cards_engine.py` 先查索引、查不到才退回本表。"
    "⚠️ 本表仍是**预组牌/教程牌那座桥**（`decklists.json` 的 id → 卡名）。"
    "🔴 **上面那句「32 位 hex 形态的 `uniqueId` 未收进本表（`pick_id` 只认前缀+纯数字，收了也用不上）」已被本行取代**（铁律 5 就地订正）。"
)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true")
    args = ap.parse_args()

    b = io.open(IDS, 'rb').read()
    assert b.count(b'\r\n') == b.count(b'\n'), "card_ids.json 行尾混了 —— 先查"
    d = json.loads(b.decode('utf-8'))
    mp, note = d['mapping'], d['note']
    n_before = len(mp)
    if n_before != 1058:
        sys.exit("❌ 现读 mapping %d 行（预期 1058）—— 先确认没被别人动过" % n_before)

    truth = json.load(io.open(TRUTH, 'r', encoding='utf-8'))
    guid = {}
    for t in truth:
        u = t.get('uniqueId') or ''
        if re.match(r'^[0-9a-f]{32}$', u):
            guid[u] = t.get('cardName') or ''
    dup = [u for u in guid if u in mp]
    print("真值 GUID 行 %d；已在表里的 %d" % (len(guid), len(dup)))
    if dup:
        sys.exit("❌ 表里已经有这些 GUID 键了：%s" % dup[:5])
    if not all(guid.values()):
        print("⚠️ 有 GUID 行的卡名为空：", [u for u, v in guid.items() if not v])

    if args.dry:
        print("（--dry：不落盘）新增后 mapping = %d 行" % (n_before + len(guid)))
        return

    for u in sorted(guid):
        mp[u] = guid[u]
    note_new = note + ADD_NOTE.format(n=n_before + len(guid))
    out = {"mapping": mp, "note": note_new}
    text = json.dumps(out, ensure_ascii=False, indent=1)
    # ⚠️ 行尾照抄原文件（**CRLF**）：`newline='\r\n'` 才会把 json.dumps 里的 `\n` 翻成 CRLF；
    #    写成 `'wb'` + `+ "\r\n"` 那种只在末尾补一个 CRLF，整篇会变成 LF（踩过一次，`git diff` 一眼就露）
    io.open(IDS, 'w', encoding='utf-8', newline='\r\n').write(text + "\n")
    print("✅ 落盘：mapping %d → %d 行" % (n_before, len(mp)))

    chk_b = io.open(IDS, 'rb').read()
    assert chk_b.count(b'\r\n') == chk_b.count(b'\n'), "行尾翻了 LF —— 立刻还原"
    chk = json.loads(chk_b.decode('utf-8'))['mapping']
    assert len(chk) == n_before + len(guid) and all(chk.get(u) == v for u, v in guid.items())
    print("✅ 回读校验通过（重开文件读了一遍，%d 行 · CRLF %d）" % (len(chk), chk_b.count(b'\r\n')))


if __name__ == '__main__':
    main()
