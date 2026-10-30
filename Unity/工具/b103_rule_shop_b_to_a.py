#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：把两条「已修 + 断言已跑绿」的商店 B 行改为 A。
证据（2026-10-30 本会话实跑）：
  · ShopScene 2982/0，B102PACK ✓26/✗0  ⇒ ArmyText 已修好且断言真跑过
  · ShellScene 3467/5 里 B98A5 ✓74/✗0 · B98A8 ✓13/✗0 ⇒ 卡包粒子材质那条的断言已绿
    （ShellScene 另外那 5 红与本条无关：B97W32 1 + B102AUD03R 4）
用法：python -I -X utf8 d:/4/Unity/工具/b103_rule_shop_b_to_a.py [--apply]
"""
import io, sys

LEDGER = "d:/4/Unity/资料/完全复刻/账/"
TARGETS = [
    ("商店.tsv", "BoosterPack(ArmyText)",
     "【B103 2026-10-30】判 A：`ShopScene.Run` 实跑 **2982 通过 / 0 失败**，`B102PACK` **✓26 / ✗0**（6 条断言真跑过）⇒ 卡包 `Army Text` 两格已建且值与 17 棵 prefab 序列化值逐格对齐。"
     "账原备注「断言还没跑过」已不成立。"),
    ("商店.tsv", "BoosterPack(ParticleRenderQueue)",
     "【B103 2026-10-30】判 A：按 `报告/B102_D10_卡包材质定性.md` 的结论（Unity 不认同一 bundle 内兄弟 CAB 的引用）**照原版分包**后，"
     "探针 `BoosterPackMaterialProbe.Run` 实测 **空槽 200→0 · 坏 shader 172→0**；`Core/BoosterPackAssets.TryEnsureLoaded()` 先加载 6 只依赖包并保持不卸载；"
     "宿主侧 `ShellScene.Run` 里 `B98A5` **✓74/✗0** · `B98A8` **✓13/✗0**（该宿主其余 5 红与本条无关：`B97W32` 1 + `B102AUD03R` 4）。"
     "⚠️ 同族的另 4 个包（`wf_menus_extra 29` · `wf_prefabs_extra 31` · `wf_battleshared_extra 13` · `wf_mainmenu_extra 58`）**要按新尺子重新定级**，另立账。"),
]

def main():
    apply = "--apply" in sys.argv
    for fn, method, note in TARGETS:
        path = LEDGER + fn
        raw = io.open(path, "rb").read()
        nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
        lines = raw.decode("utf-8").split(nl)
        hits = [i for i, l in enumerate(lines)
                if l.strip() and len(l.split("\t")) > 5
                and l.split("\t")[2].strip() == method and l.split("\t")[5].strip() == "B"]
        print("== %s | %s | 命中 %d" % (fn, method, len(hits)))
        if len(hits) != 1:
            print("   ⛔ 命中数不是 1，跳过")
            continue
        i = hits[0]
        c = lines[i].split("\t")
        print("   原状态=%s 列数=%d" % (c[5], len(c)))
        if apply:
            c[5] = "A"
            c[10] = (c[10] + "｜" + note) if len(c) > 10 else note
            lines[i] = "\t".join(c)
            io.open(path, "wb").write(nl.join(lines).encode("utf-8"))
            back = io.open(path, "rb").read().decode("utf-8").split(nl)[i].split("\t")
            assert back[5] == "A" and len(back) == len(c), "往返校验失败"
            print("   ✅ 已写：A · 列数 %d" % len(back))
    print("完成" if apply else "（预演）")

if __name__ == "__main__":
    main()
