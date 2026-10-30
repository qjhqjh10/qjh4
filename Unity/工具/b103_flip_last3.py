#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：把最后 3 条 B（DisplayEnemyCardInCenter + ResolveActiveAbility ×2）翻 A。
证据（2026-10-30 全套复跑）：`BattleScene.Run` rc=0（含 B103W5_Assert 79 条）· `RuleEngineTest.Run` 12391/12391。
用法：python -I -X utf8 d:/4/Unity/工具/b103_flip_last3.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
NOTE_DISP = ("【B103 2026-10-30 判 A】0.6 s→**1.078 s** 的阻塞已建模：`CardFeel.CardCenterBlockingSeconds` 唯一常量表 · "
             "`EventTiming` 把 `SabotageCreated` 从「零时长」挪进 `TimedKinds`（净 0 行）· `BattleDriver` 四个 const 改引 `CardFeel`（P1/P2）。"
             "断言实际跑绿（`RuleEngineTest` `B103R5` 19/19、`BattleScene` rc=0）。")
NOTE_RA = ("【B103 2026-10-30 判 A】`ca` 执行器 86 支已全部落地或判明不可达：已实现/等价 **66** + 本轮新建 `135/340/200/345/80/162/310` + "
           "`230/233` 族映射 + `handmove` 四动词；其余 ca 值在**全量 1,351 张** `cards_truth.json` 里 `chosenAbility` 计数 **= 0**（休眠，铁律 11 照建）。"
           "`play check` rc=0（指纹未动）；断言实测绿（`BattleScene` 里 `B103W5_Assert` 含 162/310 共 79 条）。"
           "⚠️ 遗留（另记）：`310` 的墓地动作日志 `displayedCard(30)` 无容器（落 `ctx.Log`）· 「中央亮牌」无事件通道 · "
           "`162` 的表现侧仍发 `EvtKind.Return`（原版是溶解特效 + `DissolveCardInHand` 音效）。")


def main():
    apply = "--apply" in sys.argv
    raw = io.open(PATH, "rb").read()
    nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
    lines = raw.decode("utf-8").split(nl)
    hits = [i for i, l in enumerate(lines) if l.strip() and len(l.split("\t")) > 10
            and l.split("\t")[5].strip() == "B"]
    print("B 行共 %d 条" % len(hits))
    for i in hits:
        c = lines[i].split("\t")
        m = c[2]
        if m.startswith("DisplayEnemyCardInCenter("):
            note = NOTE_DISP
        elif m.startswith("ResolveActiveAbility("):
            note = NOTE_RA
        else:
            print("  跳过（不认识）：%s" % m[:40]); continue
        c[5] = "A"
        c[10] = c[10] + "｜" + note
        lines[i] = "\t".join(c)
        print("  ✅ %s" % m[:50])
    if apply:
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        back = io.open(PATH, "rb").read().decode("utf-8").split(nl)
        assert len(back) == len(lines)
        print("已写")
    else:
        print("（预演）")


if __name__ == "__main__":
    main()
