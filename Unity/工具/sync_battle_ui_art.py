# -*- coding: utf-8 -*-
"""把原版战斗 UI 的切片图从缓存同步进 `Resources/Art/`（运行时真正加载的那一份）。

为什么要这个脚本：`工具/slice_ui_atlas.py` 切出来的是 `Art/原版/<图集>/`（**备查库**，
不参与打包）；运行时 `CardArt.Ui()` 读的是 `Resources/Art/ui/`。两处之间原来靠手工拷，
拷漏了就会「图明明有、代码里取不到」（踩过：`Card_Frame_Cost_Icon` / `40K_display`）。

源：`d:/2/Warpforge_tools/data/ui_extract/<bundle>/Sprite/<原版 sprite 名>.png`（5557 张切片）
    —— 按 `Sprite/*.json` 的 `textureRect` 从图集里切的**原始像素**。
目标：三个目录（`CardArt` 那三个取图口各对应一个，别混）：
    · `Resources/Art/ui/<名字下划线化>.png`      ← `NAMES`（战斗 HUD）
    · `Resources/Art/ui_deck/…`                  ← `NAMES_DECK`（卡面/卡组编辑）
    · `Resources/Art/ui_menu/…`                  ← `NAMES_MENU`（阶段二外壳；🆕 2026-10-07 · A204 加）
    + 一份和已有多媒体一致的 `.meta`
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
DST_MENU = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu"
# `.meta` 模板：拿一张**已经在用的**同级图，导入设置照抄（textureType=Default、sRGB、双线性、alphaIsTransparency）
# ⚠️ 模板只取一份、**三个目标目录共用** —— 实测这三个目录里的 `.meta` **除了 `guid:` 那一行逐字节相同**
#    （2026-10-07 核过：`ui_menu/UI_Deck_button_click.png.meta` vs `ui_menu/40K_ArmyTrack_bar.png.meta` vs
#      本模板，三份 `diff` 只差 guid），所以不必给 `ui_menu/` 另配一份。
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
    # 2026-09-20 tooltip 面板底：`Sprite/Smooth background square.json` 的 `m_Border` 原样
    "Smooth_background_square":       (12, 2, 12, 12),
    "40k_Smooth_shadow_background":   (50, 50, 50, 50),
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

    # ============================================================ 2026-09-20：卡组编辑界面那一批
    # 为什么要补：建「卡组编辑界面」的运行时控制器时，节点树里每个元素都带 sprite id
    # （`资料/说明书/04_界面UI/菜单全树.md` 的 `Deck Editing Menu` 段 = :9249-9496），
    # 把那些 id 拿去 `ui_extract/**/Sprite/*.json` 的 `pathid` 反查得到名字 ——
    # **图都在缓存里，只是没同步进 `Resources/`**（和 `Card_Frame_Cost_Icon` 是同一个坑）。
    # 逐条对照见 `资料/卡组编辑界面_查证_0920.md` 第四节那张 rect 表。
    "40k_main_tab_background",          # Sidebar 底板 + Card Filters 面板底板（节点树两处同一个 id）
    "40k_main_tab_shadow",              # Card Filters 的 Shadow 层
    "40k_main_line",                    # Header 底下那条 Separator Line（显示 1753×10）
    "40k_menu_bt",                      # Header 的 `Filters` 圆钮底（47×47）
    "40k_main_bt_selected BW",          # Window Options 页签的 Highlight（选中态底）
    "40k_collection_bt_cosmetics",      # 第三个页签 Cosmetics 的图标（cards/decks 两张已在）
    "40k_topmarquee_currency_display BW",   # Wildcard Counter 的 Background（显示 320×44）
    "40k_general_wildcard_common_small",    # ↓ 四张 = Header 右上那四个稀有度计数图标（显示 30×44）
    "40k_general_wildcard_rare_small",
    "40k_general_wildcard_epic_small",
    "40k_general_wildcard_legendary_small",
    # 🆕 2026-10-04（A12 尾巴 · 野牌判据图）：**不带 `_small` 的那四张 = 328×497 的【平铺卡面】**
    #   —— 那是 `ItemDrawer.WildcardDrawer` 真正的判据图（`iconsByRarity` 与 `wildcardBackgrounds`
    #   是**同一组四张**，见 `资料/普查产出_1003/ItemDrawer_抽屉系统.md` §四）。
    #   🔴 原来它们**只在 `Art/原版/0_mainmenu/` 里、不在 `Resources/` 下** ⇒ 运行时
    #   `CardArt.MenuUi` 取不到、`ItemDrawer` 退到 `_small`（**42×51 斜置小卡，不是同一姿态**）并出声。
    #   `CardArt.MenuUi` 走 `ui_menu/ → ui_deck/ → ui/` 三级兜底 ⇒ 落在 `ui_deck/` 就能被取到。
    "40k_general_wildcard_common",
    "40k_general_wildcard_rare",
    "40k_general_wildcard_epic",
    "40k_general_wildcard_legendary",
    "UI_Card_name_background_normal BW",    # 卡组条目那一行的底（显示 325×55.7）
    "40k_general_icon_card amount",     # Footer 里 Done 右边那枚小图标（显示 50×40）
    "FX Square UI SDF",                 # `Done Highlight`（按钮外发光；原版按「能不能保存」开关）
    "40_main_bt_toggle_on",             # Owned / Upgradable 两个开关的两态
    "40_main_bt_toggle_off",
    "40k_menu_search_icon_warlord",     # Type 筛选里「督军」那个 Toggle 的底（36×51）
    "40k_icon_search",                  # 输入框右端那枚清除钮（显示 35×30）
    "40k_DeckSelection_icon_FactionAstraMilitarum",     # ↓ 13 个阵营徽记
    "40k_DeckSelection_icon_FactionBlackLegion",        #   （Header 的 Army Icon 80×85 +
    "40k_DeckSelection_icon_FactionDarkAngels",         #    Card Filters 里 Army 那一组 Toggle）
    "40k_DeckSelection_icon_FactionEmperorsChildren",
    "40k_DeckSelection_icon_FactionLeviathan",
    "40k_DeckSelection_icon_FactionOrks",
    "40k_DeckSelection_icon_FactionSaimHann",
    "40k_DeckSelection_icon_FactionSautekh",
    "40k_DeckSelection_icon_FactionSororitas",
    "40k_DeckSelection_icon_FactionTauEmpire",
    "40k_DeckSelection_icon_FactionUM",
    "40k_DeckSelection_icon_Genestealers",
    "40k_DeckSelection_icon_SpaceWolves",
    # 2026-09-20：**tooltip 面板的底 + 阴影** —— 原版 tooltip 的面板 prefab 是 `BasicToolTip`
    #   （`assets_full/bundle_duplicateassetisolation_assets_all/GameObject/BasicToolTip.json`，
    #   脚本类 = `EverguildTooltipItem`），底图就是这两张：
    #     · `Smooth background square`   32×32、**`m_Border = (12,2,12,12)`**（九宫格）⇒ 必须走九宫格
    #     · `40k_Smooth shadow background` 102×102、`m_Border = (50,50,50,50)`
    #   面板的 TMP 实测：**字号 28**（自动缩到 10）、`m_fontColor` 纯白、HAlign=2(Center)；
    #   **基础 tooltip 只有正文、没有标题**（带标题的是 `EverguildTooltipWithTitle` / Trait 版）。
    "Smooth background square", "40k_Smooth shadow background",
]

# 阶段二外壳那批（`CardArt.MenuUi` 走 `ui_menu/ → ui_deck/ → ui/` 三级兜底）。
# 🆕 **2026-10-07（A204）**：⚠️ 先订正一句口径 —— `ui_menu/` **不是「没有任何工具在管」**：
#     `import_original_art.py` 的 `MENU_IMAGES` 导的正是这个目录（`MENU_OUT`，`--only-menu` 一趟 730 张）。
#     真正的洞是：**下面这 2 张那张名单里没有** —— 2026-10-07 有写手往 `ui_menu/` **手拷**进去
#     （调度台批准，源图 tracked），而整棵 `Resources/Art/` 在 `.gitignore` 里（构建产物）
#     ⇒ **新克隆走完重建路线，这 2 张会缺席（静默）**。
#     ⇒ 收进本表，走和其余图**同一条**重建路（按切片名从 `ui_extract` 取 + 克隆 `.meta`）。
#     ⚠️ 两张名单**名字不重叠**（2026-10-07 核过）⇒ 不会两个脚本抢同一个文件；
#        以后往 `ui_menu/` 加图，**只挑一处**登记（同一条规矩：别两处写同一件事）。
#   · 为什么落 `ui_menu/` 而不是 `ui/`：`CardArt.MenuUi` 是**三级兜底、`ui_menu/` 优先**
#     （`Core/CardArt.cs:591`）⇒ 只有放它原本那一层，重建前后的取图结果才**逐字节一致**；
#     而且同名图若在别处另有一份，改目录会悄悄改变兜底命中项（踩过：`40k_dropdown_bg` 大小写两份）。
#   · 源名照抄切片名（空格原样写，落盘时会换成 `_`）——
#     `Purity Seal_02`（注意**空格只在 Seal 前面**，与工程里的 `Purity_Seal_02.png` 是同一张）。
NAMES_MENU = [
    # 练习模式窗（`Shell/PracticeModePopup.cs:877`）卡组格的 `Highlight`（169×169 Simple）
    "UI_Deck_button_click",
    # 同一格的 `Purity_Seal_02`（128×256；`Shell/PracticeModePopup.cs:893`）
    "Purity Seal_02",
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
    for name, dst_dir in ([(n, DST) for n in NAMES]
                          + [(n, DST_DECK) for n in NAMES_DECK]
                          + [(n, DST_MENU) for n in NAMES_MENU]):
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
        # 回读校验（同下面 border 那条一个道理）：落盘的字节必须与源**逐字节相同** ——
        # 「拷看着成功了、内容不对」要当场炸，不能静默（工程红线：不许静默失败）
        with open(src, "rb") as _a, open(dst, "rb") as _b:
            assert _a.read() == _b.read(), f"{dst} 落盘后与源不一致"
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
    # 🔴 **不静默**（2026-10-07 · A204）：缺一张就**不是退出码 0** —— 原来这里只印一行就返回，
    #    而整棵 `Resources/Art/` 在 `.gitignore` 里（构建产物）⇒ 新克隆的人「跑过了、看着像成功」，
    #    实际那几张图从没落盘、运行时静默取不到（A204 要补的正是这个洞）。
    if missing:
        print(f"🔴 有 {len(missing)} 张**没同步**（切片缓存里按名字找不到）：{missing}")
        print("   ⇒ **这不是成功**：那几张图现在**不在工程里**，运行时取不到。"
              "先确认 `d:/2/Warpforge_tools/data/ui_extract/` 在不在、名字有没有写错（空格/下划线）。")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
