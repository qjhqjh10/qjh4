#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：修 `日常_奖励` 里 `CardPlayedCount.OnCardPlayed` 的落点格（文件搬家 + 落点格式）。
旧：`CardPresentation/Shell/CardPlayedCount.cs（`CardPlayedCount : MissionChallenge`）· RuleEngine/Core/CardPlayedSignal.cs（…）· 发点 = EffectResolver.cs:2618、RuleCore.cs:3982`
新：全部给「路径:行（裸声明名）」—— 调度台 2026-10-30。
用法：python -I -X utf8 d:/4/Unity/工具/b103_fix_cpc_landing.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv"
METHOD = "OnCardPlayed(RawCard card)"
OLD_HEAD = "CardPresentation/Shell/CardPlayedCount.cs（"
NEW = ("CardPresentation/Shell/CardPlayedCount.cs:54（CardPlayedCount）· "
       "RuleEngine/Core/CardPlayedSignal.cs:82（CardPlayedSignals）· "
       "RuleEngine/Core/EffectResolver.cs:2618 · RuleEngine/Core/RuleCore.cs:3982")


def main():
    apply = "--apply" in sys.argv
    raw = io.open(PATH, "rb").read()
    nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
    lines = raw.decode("utf-8").split(nl)
    hit = []
    for i, l in enumerate(lines):
        if not l.strip():
            continue
        c = l.split("\t")
        if len(c) > 6 and c[2].strip() == METHOD:
            hit.append(i)
    print("命中 %d" % len(hit))
    if len(hit) != 1:
        return
    i = hit[0]
    c = lines[i].split("\t")
    cell = c[6]
    if "发点 = EffectResolver.cs:2618、RuleCore.cs:3982" not in cell:
        print("⛔ 尾段与预期不符：%s" % cell[-120:])
        return
    c[6] = cell.replace("CardPresentation/Shell/CardPlayedCount.cs（`CardPlayedCount : MissionChallenge`）", NEW.split("· RuleEngine/Core/RuleCore.cs")[0], 1)
    c[6] = c[6].replace("· RuleEngine/Core/CardPlayedSignal.cs（`CardPlayedSignal` DTO + `CardPlayedSignals` 注册表/发点）", "", 1)
    c[6] = c[6].replace("发点 = EffectResolver.cs:2618、RuleCore.cs:3982",
                        "RuleEngine/Core/RuleCore.cs:3982")
    lines[i] = "\t".join(c)
    print("新落点：%s" % c[6][:220])
    if apply:
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        back = io.open(PATH, "rb").read().decode("utf-8").split(nl)
        assert len(back) == len(lines), "行数变了"
        print("✅ 已写")
    else:
        print("（预演）")


if __name__ == "__main__":
    main()
