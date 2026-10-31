#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B109_E16 的账补丁生成器（只读主表、只写 _parts/B109_E16.tsv）。

补丁策略 = **按 (原版类, 方法) 更新已有那一行**：
  `ScreenAspectRatioController` / `Start()` 这一行本来就是「组件本体已 A」，
  本批补的是**壳侧宿主挂接** ⇒ 在原行的 `我们的落点` / `备注` 两列上追加，
  其余 9 列**逐字节照抄主表现值**（⛔ 不重打，防手抄走样）。

为什么不用新键：`merge_ledger_parts.py` 拒「方法含全角括号」（注记不是签名），
而原版这一类只有 4 个方法、4 行都已被占 ⇒ 新键只能编一个不存在的原版方法（会污染分母）。

跑法：python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B109_E16.py
"""
import io

LEDGER = r"d:/4/Unity/资料/完全复刻/账/菜单_外壳.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B109_E16.tsv"

HEADER = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
          "我们的落点", "差异", "等级", "轮次", "备注"]

EXTRA_LANDING = (" · Shell/ShellRuntime.cs:1424（EnsureAspectClamp）")

EXTRA_NOTE = (
    "[B109-E16 2026-10-31] 壳侧宿主挂接补齐（AUD05 S4 = N80）。"
    "原版把该件挂在**主菜单场景的 UI 相机本体**（MB1987：m_Enabled=1 · targetCameras=[Camera_602] · "
    "max=2.444443941116333 · min=1.3333330154418945；`Camera_602` 与 `1987` 同属 GO pid 47 = `UI Camera`，本代理现读）。"
    "⚠️ 就地订正：R5 判据里那句「场景 cleanFrameBuffer=1」**不成立** —— 该字段 private、不进场景 JSON"
    "（ctor 出厂 = 1、SetCamerasRect 每次置 1）。"
    "全解包 grep -rl maxAspectRatio = 14 份 = 13 战场 + 主菜单 ⇒ Intro/level0 没有。"
    "我们原来壳层**根本没挂** ⇒ 5:4 / 32:9 不黑边、直接拉伸。"
    "现在挂两处：Shell/MainMenuRuntime.cs:456（Build 里 LayoutSpace.Apply 之后，判据直取 MB1987）与 "
    "Deck/DeckRuntime.cs:1280（Build 里，**只独立场景档 HostWindow==null** —— 原版卡组编辑是主菜单下的一扇窗、"
    "渲染相机同一颗；窗化档再挂一次会把它带进 Shell.unity 存盘）。⛔ ShellRuntime.Build 不挂（原版 Intro/level0 无该件）。"
    "分辨率重算的发射源 = Shell/ShellRuntime.cs:1450（TickAspectClamp）：原版「谁抬 ScreenResolutionChangeSignal」"
    "全反编译查不到、壳侧又没有 BattleCameraSreenSize ⇒ 🔶 自己设计（非原版，机制等价）。"
    "断言 = CardPresentation/Editor/B109_E16_AspectClamp_Assert.cs（B 接线 / C rect+aspect / D 分辨率重算 / "
    "E 两族读像素；**未接宿主**，建议接 Editor/MainMenuScene.cs 与 Editor/DeckScene.cs 的聚合行）；typecheck 运行时 0 / 编辑器 0。"
    "顺手发现已修：主菜单模式卡视差的 _ScreenPosition.x 原来按「设计x/1920」算，夹取后会差 rect.x 那一截 ⇒ "
    "改走 rect.x + 设计x/1920×rect.width（MainMenuRuntime.ScreenNormalizedXForDesignPx；整幅档与旧式逐位相同）。"
)


def main():
    raw = io.open(LEDGER, "rb").read()
    text = raw.decode("utf-8-sig")
    lines = text.split("\r\n") if raw.count(b"\r\n") else text.split("\n")
    hit = None
    for ln in lines:
        f = ln.split("\t")
        if len(f) == 11 and f[1] == "ScreenAspectRatioController" and f[2] == "Start()":
            hit = f
            break
    if hit is None:
        raise SystemExit("主表里找不到 ScreenAspectRatioController / Start() —— 先别写补丁，回去核账面")

    if EXTRA_LANDING.strip(" ·") not in hit[6]:
        hit[6] = hit[6] + EXTRA_LANDING
    if "[B109-E16" not in hit[10]:
        hit[10] = (hit[10] + " " if hit[10] else "") + EXTRA_NOTE

    for i, x in enumerate(hit):
        if "\t" in x or "\n" in x or "\r" in x:
            raise SystemExit("第 %d 列含制表/换行，补丁会被拒" % i)
    if len(hit) != 11:
        raise SystemExit("列数不是 11")

    body = "\t".join(HEADER) + "\n" + "\t".join(hit) + "\n"
    io.open(OUT, "wb").write(body.encode("utf-8"))
    print("wrote", OUT)
    print("落点 =", hit[6])
    print("备注长度 =", len(hit[10]))


main()
