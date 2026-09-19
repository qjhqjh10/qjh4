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
    # 2026-09-13：补摆「原版有、我们原来缺」的 HUD 件时要用到的两张
    #   · `Player Profile Border` —— 头像块 `Avatar Item Small` 的外框（原版场景态里
    #     `avatarImage` 是 `m_Enabled=0`，**只显示这圈框**，头像立绘由 `ItemDrawer` 运行时灌）
    #   · `40k_icon_overtime` —— 加时标记 `OvertimeIndicator`（**默认隐藏**，我们还没有加时机制）
    "Player Profile Border", "40k_icon_overtime",
    # 换牌（Mulligan）那一套
    #   ⚠️ 2026-09-13 更正：`资料/战斗UI_原版对账表.md` §三点七 写着「`UI_Button_Mulligan` 三态**已在工程**」——
    #   实测**不在**（只有 `40k_bt_underbutton` / `40k_UI_bt_play` / `40k_UI_bt_eye` 在）。
    #   三态图在缓存里好好的（`duplicateassetisolation_assets_all/Sprite/`），是**没同步**。
    "40k_bt_underbutton", "40k_UI_bt_play", "40k_UI_bt_eye",
    "UI_Button_Mulligan", "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
    # 设置面板（原版 `BattleSettingsPanel` / `BattleSettingsWindow`）——
    # 投降按钮就在这个面板里（`resignButton`），面板自己带 `40k_popup` 底 + 圆形关闭钮
    "UI_Button_Round_background", "40k_bt_close", "40k_popup_texture",
    "40K_button",          # 面板里的通用按钮底（Debug 那排用的就是它）
    # 2026-09-15：**关键词图标的底板** —— 原版棋盘上每个单位卡左 3 右 4 共 7 个
    #   `TraitIconContainer`，每个里面是 `TraitIcon` + `Trait Icon Background`；
    #   底板 sprite 就是这张（`battleatlasui` 里，`m_Rect` 105×214），
    #   棋盘组件 `BoardTraitIcon`（`traitIcons[2]` / `counterText` / `withCounter`）在
    #   `bundle_battleprefabs_vfxandmisc_assets_all` 里。
    #   ⚠️ 原来只导了 33/60 张 battleatlasui，**把它漏掉了**（`资料/卡面图标_现状与缺口.md:49` 记的就是它）。
    "Base3d Trait Background",
    # 2026-09-17：**回放条**（原版 `ReplayButtons`，4 枚 79.80×48.57 —— 容器 293.60×57.41）。
    #   ⚠️ 这四张**本来就在备查库里**（`Art/原版/battleatlasui/`），是**没同步进 `Resources/`**
    #   —— 和 `Card_Frame_Cost_Icon` / `40K_display` 是同一个坑（铁律 5 那一族）。
    #   图实测像素：restart/next 是 **95×59**、play/pause 是 **95×58**（四张**并非同一高度**）。
    #   源字段：`40K_replay_bt_restart_-739538110830474900.json` → `m_Rect {0,0,95,59}`、
    #   `m_PixelsToUnits=100`、`m_Pivot(0.5,0.5)`、`m_Border(0,0,0,0)`（**无九宫格**，`m_Type=0`）。
    "40K_replay_bt_restart", "40K_replay_bt_play", "40K_replay_bt_pause", "40K_replay_bt_next",
    # 2026-09-17：**单位语音条**（原版 `Unit Chat/PlayerChatDisplay` / `EnemyChatDisplay`）的两张图。
    #   ⚠️ `40k_UnitChat_Background_*` 那 4 张**不属于语音条** —— 它们是 `ChatPopup`
    #      （点 ChatButton 弹出的预设台词面板）的 `BGFrame` 四边框。
    #   `40k_voicelines_radio` 766×280、`m_Border` 全 0、`m_Type=0`（Simple）⇒ 纯拉伸。
    #   `…wave equalizer` **708×96、`m_AtlasTags: []`（不在任何图集里，是独立 Sprite）**——
    #      原图在 `素材/Warpforge原版/特效共享资源/`，切片缓存在 `scenes_scenes_battlearena1_sprites/`。
    #      显示 387.95×62.60 ⇒ **非等比拉伸**（x 0.548× / y 0.652×）。
    "40k_voicelines_radio", "40k_voicelines_radio_wave equalizer",
    # 2026-09-18：**`ChatPopup` 面板**（用户当轮要做的最后一件语音件）。规格见
    #   `资料/语音线_原版规格与ASR管道.md` §1.7。原话：「这 4 张**别一起导进来**」——
    #   **那是「还没做 ChatPopup」时的处置，现在要做面板了，改成导进来。**
    #   四边框原图/落地比例实测（`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md:186-189`）：
    #     Top 590×39→517.7×33.9 (0.877×) · Bottom 590×42→517.7×36.5 · Left 134×445→117.3×387.0 · Right 95×427→83.7×371.1
    #   `White Square` = 面板底色（**8×8 纯白**，靠 `Image.color` 染成深绿 `(0,0.07,0,1)`，
    #     面板体 557.3×301.3）。5 份副本**字节完全相同**（md5 核过）⇒ 取哪一份都行。
    "40k_UnitChat_Background_Top", "40k_UnitChat_Background_Bottom",
    "40k_UnitChat_Background_Left", "40k_UnitChat_Background_Right",
    "White Square",
    # 2026-09-18 同日补：**6 个台词钮自己的两张图**（子代理逐节点扫 `ChatButton` 子树才看到 ——
    #   只看面板那一层是看不见它们的）。
    #   · `40k_voicelines_bt_R` = 钮**底板**（原版 `bg` 节点：550.405×44.5 @ 钮内 x+23.724）
    #   · `40k_voicelines_bt_L` = 钮**左图标**（原版 `button` 节点：40×40 @ 钮内 x+23.5）
    #   ⚠️ 按钮本体那张 Image 的 `sprite` 是 **0**、`color.a = 0` —— 它是个**看不见的射线靶**，
    #      真正的观感全在这两张图上。别看到「按钮没图」就以为原版是纯色块。
    "40k_voicelines_bt_R", "40k_voicelines_bt_L",
    # 2026-09-19：**音量滑块**三张（原版 `BattleSettingsPanel` 的音量条 / 装它的滑钮）。
    #   源：`ui_extract/duplicateassetisolation_assets_all/Sprite/`（5 个包里同名副本**逐字节相同**）。
    #   ⚠️ 这三张是**九宫格**图（`m_Border` 非 0），本工程**头一回** —— 见下面 `BORDERS`。
    #   出处（原版字段）：`bundle_duplicateassetisolation_assets_all/Sprite/<名>.json`
    #     `m_Rect` 与 `m_RD.textureRect` 都是 (0,0,W,H) —— sprite 就是自己那张独立贴图的大小；
    #     `m_PixelsToUnits=100`、`m_Pivot=(0.5,0.5)`、`m_Extrude=1`、`m_IsPolygon=false`。
    #   裁切核对：从 `0_GeneralUI Atlas`（4096×2048 BC7）按
    #     `SpriteAtlas_4765312961718699286.json` 的 `m_RenderDataMap[*].atlasRectOffset`
    #     裁出来的像素与缓存切片 **逐字节相同**（mean|Δ|=0.00）—— 缓存里那三张就是原图，不必再切。
    "Volume_bar_inactive", "Volume_bar_active", "Volume_button",
]

# 九宫格 border（**只有列在这里的才写**，没列的照模板留全 0）。
# 值 = `Sprite.m_Border` 的 `(x, y, z, w)` **原样** —— Unity 的 `spriteBorder: {x,y,z,w}`
# 就是 `(left, bottom, right, top)`，与 `m_Border` 同序，直接抄，不要换位。
# 出处：三份 `Sprite/<名>.json` 的 `m_Border`（实测值）。
# ⚠️ 这三张是本工程**第一批带 border 的图**（此前 1509 份 `.meta` 全是 0）⇒
#   要用它的 `Image` 必须把 `type` 设成 `Sliced`，否则 border 不参与拉伸。
BORDERS = {
    "Volume_bar_active":   (30, 0, 30, 0),      # m_Border {x:30, y:0, z:30, w:0}
    "Volume_bar_inactive": (184, 0, 184, 0),    # m_Border {x:184, y:0, z:184, w:0}
    # `Volume_button` 的 `m_Border` 是 (0,0,0,0) —— **不列在这里**就是对的，别写 0 覆一遍
}

# 卡面组件（和稀有度宝石、`Card_Frame_Cost_Icon` 同一批，落在 `Resources/Art/ui_deck/`）
NAMES_DECK = [
    # 能量水晶底下那块底板（原版 `Energy Player`，已在 `ui_deck/` 里，别重复拷一份）
    "Card Frame Cost Icon",
    "pedestal_icon_armor",   # 护甲盾牌底（原版 `Armour Container/Image` 0.3067×0.3784）
    # 2026-09-15：**卡面的文字底板** —— 原版 `Front/Textbackgrounds/TextBackground Big UI`
    #   （sprite `Card Text smooth background`，32×32、border 7/0/7/9、九宫格）。
    #   实测字段：`m_Color=(0,0,0,0.647)`、`m_Type=1`(Sliced)、`m_PixelsPerUnitMultiplier=14.7`、
    #   RT `sizeDelta` 1.7766×1.55 @(-0.001,-0.65)（父 `Front` @y=+0.08）。
    #   出处：`d:/2/解包整理/07_场景/battlearena1/`（GameObject/RectTransform/MonoBehaviour）。
    "Card Text smooth background",
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
        # 克隆导入设置，只换 guid；九宫格图再把 `spriteBorder` 那一行换掉（见 `BORDERS`）
        # ⚠️ **判据要 strip 后再比**：模板里 `spriteBorder:` 是**缩进两格**的
        #    （`  spriteBorder: {…}`）。第一版写成 `ln.startswith("spriteBorder:")` ⇒ 永远不命中，
        #    而摘要行是按 `BORDERS` 字典打出来的 ⇒ **打印说写了、文件里是 0**（典型「自检绿口径错」）。
        #    2026-09-19 修正 + 末尾加了**回读校验**（写完再打开文件核一遍）。
        border = BORDERS.get(os.path.splitext(dst_name)[0])
        lines = []
        for ln in meta_tpl.splitlines():
            s = ln.strip()
            if s.startswith("guid:"):
                lines.append("guid: " + guid_for(dst_name))   # ⚠️ 模板里 `guid:` 是**顶格**的，别加缩进
            elif border and s.startswith("spriteBorder:"):
                # 保留模板原有的缩进（两个空格）
                lines.append("  spriteBorder: {x: %d, y: %d, z: %d, w: %d}" % border)
            else:
                lines.append(ln)
        with open(dst + ".meta", "w", encoding="utf-8", newline="\n") as f:
            f.write("\n".join(lines) + "\n")
        # 回读校验：文件里必须真的出现我们要的那行 border（别再靠字典自证）
        if border:
            with open(dst + ".meta", encoding="utf-8") as f:
                want = "spriteBorder: {x: %d, y: %d, z: %d, w: %d}" % border
                assert want in f.read(), f"{dst}.meta 没写进 {want}"
        added.append(dst_name + ("（border %s）" % (border,) if border else ""))

    print(f"已在工程里 : {len(present)} 张 {present}")
    print(f"本次同步   : {len(added)} 张 {added}")
    print(f"缓存里没有 : {len(missing)} 张 {missing}")


if __name__ == "__main__":
    main()
