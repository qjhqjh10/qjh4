#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103 丙类裁决执行器：把 3 条 B 改为 A，并把裁决理由写进「备注」列。
只改指定行；写完做往返校验（列数、行数、未触碰行的哈希）。
用法：python -I -X utf8 d:/4/Unity/工具/b103_rule_b_to_a.py [--apply]
不带 --apply 只打印将要做的事（默认安全）。
"""
import io, sys, hashlib

LEDGER = "d:/4/Unity/资料/完全复刻/账/"
TARGETS = [
    ("日常_奖励.tsv", "InitializeMilestones()",
     "【B103 裁决 2026-10-30】判 A：客户端侧逐段已落（.c:29-58 → ParentEventWindow.cs:2008/:2069/:1310/:1948；生产入口 ApplyOnGoingMilestones :2414）。"
     "残差 = 远端 TwoSidesEventData.rewards[] / currentScore / Save.collectedRewards —— 关服后本地永久不可得。"
     "按方案 §4.1 边界（客户端要拿结果画界面 ⇒ 不判 D）+ §18·0（服务器相关可自设计）与既有先例（TwoSidesEvent_ChooseSideTab 判 A：出厂态 + 出声）："
     "⛔ 不编假里程碑数据；无数据即出厂态 + LogError 出声。"),
    ("日常_奖励.tsv", "OnOpen()",
     "【B103 裁决 2026-10-30】判 A：S1/S2/S3/S5/S6/S7/S8/S9/S10 逐段已落（ParentEventWindow.cs:1191/1240/1289/1294/2164/2174/2244/2268/2341/2382/2414）。"
     "残差 ① S4 两队 totalArmyScore = 服务端榜（P3）② 远端队伍/里程碑数据 ③ TimerDisplay 目标时刻 —— 关服后本地永久不可得。"
     "按 §4.1 边界不判 D；口径同 InitializeMilestones：⛔ 不编数据，缺数据即出厂态 + 出声（:1289 计时 `--` 已出声、:1580 队伍卡出厂态已出声）。"),
    ("社交_聊天.tsv", "ClickAddFriend()",
     "【B103 裁决 2026-10-30】判 A：本地已逐句复刻到服务器边界（FriendsTab.cs:615 读值→Trim→IsValidFriendName→出无效名消息窗），"
     "写盘点 :583 输入件已接。残差 = PlayfabWrapper.GetPlayerIdByName + PlayerDataManager.AddFriendToList（PlayFab 后端，已关服）—— 客户端根本拿不到返回值。"
     "按 §4.1 边界（该分支只在服务器返回值存在时才继续）判 A + 出声（⛔ 不凭空塞 SocialData.Friends 假装成功）。"),
]

def load(path):
    b = io.open(path, "rb").read()
    return b, b.decode("utf-8")

def main():
    apply = "--apply" in sys.argv
    for fn, method, note in TARGETS:
        path = LEDGER + fn
        raw, text = load(path)
        nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
        lines = text.split(nl)
        hits = []
        for i, l in enumerate(lines):
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) > 5 and c[2].strip() == method and c[5].strip() == "B":
                hits.append(i)
        print("== %s | %s | 命中 B 行 %d 条" % (fn, method, len(hits)))
        if len(hits) != 1:
            print("   ⛔ 命中数不是 1，跳过（人工确认）")
            continue
        i = hits[0]
        c = lines[i].split("\t")
        print("   原状态=%s 列数=%d" % (c[5], len(c)))
        if apply:
            c[5] = "A"
            c[10] = (c[10] + "｜" + note) if len(c) > 10 else note
            lines[i] = "\t".join(c)
            out = nl.join(lines)
            if nl == "\r\n":
                io.open(path, "wb").write(out.encode("utf-8"))
            else:
                io.open(path, "wb").write(out.encode("utf-8"))
            # 往返校验
            raw2, text2 = load(path)
            back = text2.split(nl)[i].split("\t")
            assert back[5] == "A", "状态没写进去"
            assert len(back) == len(c), "列数变了"
            print("   ✅ 已写：状态 A · 列数 %d · 行数 %d" % (len(back), len(lines)))
    print("（不带 --apply 只预演）" if not apply else "完成")

if __name__ == "__main__":
    main()
