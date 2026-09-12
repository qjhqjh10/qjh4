# -*- coding: utf-8 -*-
"""把原版战斗 UI 的切片图从缓存同步进 `Resources/Art/`（运行时真正加载的那一份）。

为什么要这个脚本：`工具/slice_ui_atlas.py` 切出来的是 `Art/原版/<图集>/`（**备查库**，
不参与打包）；运行时 `CardArt.Ui()` 读的是 `Resources/Art/ui/`。两处之间原来靠手工拷，
拷漏了就会「图明明有、代码里取不到」（踩过：`Card_Frame_Cost_Icon` / `40K_display`）。

源：`d:/2/Warpforge_tools/data/ui_extract/<bundle>/Sprite/<原版 sprite 名>.png`（5557 张切片）
    —— 按 `Sprite/*.json` 的 `textureRect` 从图集里切的**原始像素**。
目标：`Resources/Art/ui/<名字下划线化>.png` + 一份和已有多媒体一致的 `.meta`
    （直接克隆模板的导入设置，只换 guid —— 手写 .meta 容易把压缩/PPU 写错）。

用法：
  PYTHONIOENCODING=utf-8 python 工具/sync_battle_ui_art.py            # 同步下面 NAMES 里缺的
  PYTHONIOENCODING=utf-8 python 工具/sync_battle_ui_art.py --check    # 只报告，不写盘
"""
import os
import sys
import json
import shutil
import hashlib

sys.stdout.reconfigure(encoding="utf-8")

SRC_ROOT = "d:/2/Warpforge_tools/data/ui_extract"
DST = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui"
DST_DECK = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_deck"
# `.meta` 模板：拿一张**已经在用的**同级图，导入设置照抄（textureType=Default、sRGB、双线性、alphaIsTransparency）
META_TEMPLATE = os.path.join(DST, "40k_battle_Win_Skull.png.meta")

# 原版 sprite 名 → 我们文件名（空格换下划线，其余照旧）
NAMES = [
    # 牌库张数底板 / 手牌数底板（原版 `Player Deck Size Container` / `CardsInHand Bg`）
    "40K_display",
    # 本回合已出牌数的三枚小方块（原版 `CardsPlayedInTurn 1..3`）
    "40k_general_bt_yellow",
    # 墓地/战斗日志面板（原版 `CemeteryLogPanel`）
    "40k_battlelog_frame_TOP", "40k_battlelog_frame_Bottom",
    "40k_battlelog_frame_Left", "40k_battlelog_frame_Right",
    "40k_battlelog_display_neutral",
    # 边角按钮群
    "UI_Settings_Icon", "40k_UI_bt_battlelog", "40k_UI_bt_voicelines",
    "40k_UI_bt_center_camera", "40k_battle_icon_environmental",
    # 换牌（Mulligan）那一套
    "40k_bt_underbutton", "40k_UI_bt_play", "40k_UI_bt_eye",
    # 设置面板（原版 `BattleSettingsPanel` / `BattleSettingsWindow`）——
    # 投降按钮就在这个面板里（`resignButton`），面板自己带 `40k_popup` 底 + 圆形关闭钮
    "UI_Button_Round_background", "40k_bt_close", "40k_popup_texture",
    "40K_button",          # 面板里的通用按钮底（Debug 那排用的就是它）
]

# 卡面组件（和稀有度宝石、`Card_Frame_Cost_Icon` 同一批，落在 `Resources/Art/ui_deck/`）
NAMES_DECK = [
    # 能量水晶底下那块底板（原版 `Energy Player`，已在 `ui_deck/` 里，别重复拷一份）
    "Card Frame Cost Icon",
    "pedestal_icon_armor",   # 护甲盾牌底（原版 `Armour Container/Image` 0.3067×0.3784）
]


def find_src(sprite_name):
    """在切片缓存里找这张图（按 sprite 名精确匹配文件名）。"""
    for dirpath, _dirs, files in os.walk(SRC_ROOT):
        if os.path.basename(dirpath) != "Sprite":
            continue
        for f in files:
            if f.lower() == (sprite_name + ".png").lower():
                return os.path.join(dirpath, f)
    return None


def guid_for(name):
    """稳定的 guid（同一张图反复同步不会换 guid，否则场景引用会断）。"""
    return hashlib.md5(("battleui:" + name).encode("utf-8")).hexdigest()


def main():
    check = "--check" in sys.argv
    with open(META_TEMPLATE, encoding="utf-8") as f:
        meta_tpl = f.read()

    missing, added, present = [], [], []
    for name, dst_dir in [(n, DST) for n in NAMES] + [(n, DST_DECK) for n in NAMES_DECK]:
        dst_name = name.replace(" ", "_") + ".png"
        dst = os.path.join(dst_dir, dst_name)
        if os.path.exists(dst):
            present.append(dst_name)
            continue
        src = find_src(name)
        if not src:
            missing.append(name)
            continue
        if check:
            added.append(dst_name + "（待同步）")
            continue
        shutil.copyfile(src, dst)
        # 克隆导入设置，只换 guid
        lines = []
        for ln in meta_tpl.splitlines():
            lines.append("guid: " + guid_for(dst_name) if ln.startswith("guid:") else ln)
        with open(dst + ".meta", "w", encoding="utf-8", newline="\n") as f:
            f.write("\n".join(lines) + "\n")
        added.append(dst_name)

    print(f"已在工程里 : {len(present)} 张 {present}")
    print(f"本次同步   : {len(added)} 张 {added}")
    print(f"缓存里没有 : {len(missing)} 张 {missing}")


if __name__ == "__main__":
    main()
