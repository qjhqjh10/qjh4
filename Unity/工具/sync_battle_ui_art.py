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

🆕 **2026-10-07（A209）：两条【静默】的口子堵掉**（原来都只看 `os.path.exists` / 取「第一个命中」）：

  · **盘上已经有那张图 ≠ 它是对的** —— 现在**逐字节**与切片缓存比一遍：对不上就点名每一张
    （盘上 md5 vs 缓存源 md5）+ 非 0 退出，**两种模式都不写它**（`--check` 与同步模式同一判据）。
    ⚠️ **只比 PNG**：`.meta` 的**导入设置**不比 —— 实测 36 份 `.meta` 与「照模板现渲染」的那份不同
    （35 份 `maxTextureSize: 2048`，那是 **Unity 导入时自己改写的**；1 份 guid 是别的路进来的），
    拿它当判据会**天天误报**。`.meta` **只在盘上真没有时才按模板新建**；盘上已经有的话**不重写它**
    （最多补/改 `BORDERS` 里那一行 `spriteBorder` —— 见下面第四条）。
    （🔴 `.meta` 的 **guid** 是另一回事 —— 现在**会只读地比一下**，见下面第三条。）
  · **切片缓存里同名多副本**：内容**逐字节一致** ⇒ 按**路径排序取第一份**（确定性，不靠 `os.walk` 顺序）；
    内容**有分叉** ⇒ **不猜**、把每一份的路径/大小/md5 全点名 + 非 0 退出
    （原来取「第一个命中」且静默 ⇒ 副本一分叉就静默拷错内容）。

  · 🆕 **2026-10-08（A226）：盘上 `.meta` 的 guid 与 `guid_for()` 对不上 ⇒ 【只读地】点名**
    —— 上面那条「`.meta` 不比」说的是**导入设置**（`maxTextureSize` 那些，比了会天天误报）；
    **guid 是另一回事**：已存盘的三份场景是**按 guid** 引这些图的（实测 `Card_Frame_Cost_Icon.png`
    一张就 **357** 处），而 `guid_for()` 是**确定性**的 ⇒ 两者对不上，意味着
    **这张图「若连 `.meta` 一起被删掉再重跑」guid 会变、场景里那些引用全悬空**（少一张图、**不报错**）
    —— ⚠️ **只删 PNG、不删 `.meta`** 的那一格 **2026-10-08（A226 加固）起已经堵上**（见下面第四条）。
    ⚠️ 本检查**只报不修**，**也不计入退出码**：**盘上那份 guid 本来就是对的**（357 处按它落的），
    要动的是**重建次序**（先跑本脚本、再重建场景），不是这个文件；唯一的例外见
    `KNOWN_META_GUID_EXCEPTIONS`（历史遗留，**别当待修**）。

  · 🆕 **2026-10-08（A226 加固）：PNG 被删、`.meta` 还留在盘上 ⇒ 拷回 PNG 时【保留那份 `.meta`】**
    —— 这一格原来会**照模板重写整个 `.meta`**（`guid: <guid_for()>`）⇒ 盘上那份 guid 若是**别的路进来的**
    （= 已存盘场景按它落的引用，如 `Card_Frame_Cost_Icon.png` 那 **357** 处），guid 一变那些引用
    **全部悬空、不报错** —— 与上面第三条那个洞**是同一个**。现在：
    **盘上有 `.meta` ⇒ 只补 PNG、不重写它**（`guid:` 那一行一个字节都不碰；最多按 `BORDERS` 补/改
    `spriteBorder` 那一行，见 `patch_meta_border()`）；只有**真不在**时才按 `guid_for()` 新建（= 原行为）。
    ⚠️ 盘上有 `.meta` 但**读不出顶层 `guid:`** ⇒ **不写它**（那会换掉一个我们**读不出**的身份，换完也没法复核）
    + 点名；这一格**不计入退出码**（与下面第三条同族：`.meta` 的 guid 状态只报不修）
    —— 这一格谁都没覆盖过，处置与理由见 `资料/普查产出_1008/A226加固_保留meta guid.md`「没查清的部分」。

⇒ 退出码 0 的含义变严了：**「每一张图都能证明是对的」**（不是「没报错」）。
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


_SRC_INDEX = None


def src_index():
    """整棵切片缓存扫**一遍**，建 `文件名(小写) → [路径, …]`（只认 `Sprite/` 目录里的文件）。

    匹配规则与原 `find_src` **一字不差**（文件名小写后 == `<sprite 名>.png` 小写）；
    只是改成**整份索引建一次**（原来每张图各走一遍 `os.walk`），顺带把**全部同名副本**留下来
    —— A209 要的「副本分叉要点名」拿得到。
    """
    global _SRC_INDEX
    if _SRC_INDEX is None:
        idx = {}
        for dirpath, _dirs, files in os.walk(SRC_ROOT):
            if os.path.basename(dirpath) != "Sprite":
                continue
            for f in files:
                idx.setdefault(f.lower(), []).append(os.path.join(dirpath, f))
        _SRC_INDEX = idx
    return _SRC_INDEX


def file_md5(path):
    """整份文件的 md5（拿来「逐字节比 + 点名」用；图都很小，不必省）。"""
    h = hashlib.md5()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def find_src(sprite_name):
    """在切片缓存里找这张图（按 sprite 名精确匹配文件名）。

    返回 `(要用的那一份, 缓存里所有同名副本)` —— 🔴 **多副本不许静默取第一个**（2026-10-07 · A209）：

      · 一份都没有                ⇒ `(None, [])`       调用方按「缓存里没有」处理
      · 只有一份                  ⇒ `(那一份, [它])`
      · 多份、内容**逐字节一致**  ⇒ `(路径排序最小的那一份, 全部)`（**确定性**：不靠 `os.walk` 的顺序）
      · 多份、**有分叉**          ⇒ `(None, 全部)`      **不猜**：调用方必须点名 + 非 0 退出
    """
    copies = sorted(src_index().get((sprite_name + ".png").lower(), []))
    if not copies:
        return None, []
    if len(copies) == 1 or len({file_md5(p) for p in copies}) == 1:
        return copies[0], copies
    return None, copies


def guid_for(name):
    """稳定的 guid（同一张图反复同步不会换 guid，否则场景引用会断）。

    ⚠️ **2026-10-07 实测（A209 顺手发现）：「稳定」≠「与现状一致」** —— 83 张里 **82 份 `.meta` 的 guid
    正好等于本函数算出来的值**（⇒ 删掉重跑也不换 guid），**但有 1 份不是**：
    `ui_deck/Card_Frame_Cost_Icon.png` 盘上是 `44be35390df27074bac0aa900350987e`，
    本函数算的是 `2ac1b7026753c86dcb3b11aa107d7618`；而**已存盘场景按 guid 引用了前者 357 处**
    （`CollectionCheck.unity` 313 · `DeckEditor.unity` 29 · `CardBase.unity` 15 —— 三份都是 untracked 构建产物）
    ⇒ **这一张若连 `.meta` 一起被删掉再重跑，guid 会变、那 357 处引用会悬空**（要等场景重建才吸收）
    （🔴 **2026-10-08 A226 加固**：**只删 PNG、留 `.meta`** 的那一格**不会**换 guid 了 —— 脚本会保留盘上那份
    `.meta`，见 `patch_meta_border()` 与文件头第四条）。
    🔴 **2026-10-08（A226）调度台定的口径**：**不动 guid**（那 357 处引用是现成的、对的）——
    ① **把「重建次序」写死**：**先跑本脚本（图）→ 再重建场景**（`CollectionCheck.unity` /
    `DeckEditor.unity` / `CardBase.unity` 都是构建产物，重建时按**当时盘上**的 guid 落引用）；
    ② **让漂移可见**：`meta_guid_status()` 只读点名（**只报不修、不计入退出码**）。
    实测与清单 → `资料/普查产出_1008/波B2_生成器与资产同步.md` §六·1/§六·2；
    本件的处置与落地位置 → `资料/普查产出_1008/A226_卡面费用图guid口径.md`。
    """
    return hashlib.md5(("battleui:" + name).encode("utf-8")).hexdigest()


# 🔴 **盘上 `.meta` 的 guid 与 `guid_for()` 对不上的【已知例外】**（2026-10-08 · A226）
#
# 为什么会有例外：`guid_for()` 是 **2026-09-13** 起才有的（本文件那时才进仓库）；在那之前
# `Resources/Art/` 靠**手工拷**（见文件头那句「两处之间原来靠手工拷」）—— 手工那批的 `.meta`
# 是照当时的模板克隆的 ⇒ guid 是**别的路进来的**、不是本函数算的（A209 顺手查出那条也是这么记的）。
# `Card_Frame_Cost_Icon.png` 就是那一批之一：**证据在 `NAMES_DECK` 那句注释**
# 「（原版 `Energy Player`，**已在 `ui_deck/` 里，别重复拷一份**）」—— 它在脚本之前就在了，
# 它的 `.meta` **从没被本脚本写过**。
#
# 🔴 **它不是缺陷、⛔ 别当待修**：三份**已存盘**场景按**盘上这个 guid** 引用了它 **357** 处
# （`CollectionCheck.unity` 313 · `DeckEditor.unity` 29 · `CardBase.unity` 15，形态都是
# `m_Texture: {fileID: 2800000, guid: …}`）⇒ **盘上这个 guid 才是「对的」那个**；
# 把它改成 `guid_for()` 的值（或**连 `.meta` 一起删掉**再重跑）**才会**让那 357 处悬空（少一张卡面费用图标、**不报错**）。
# （🔴 **2026-10-08 A226 加固**：**只删 PNG、留 `.meta`** 的那一格**不会**换 guid —— 那时脚本保留盘上这份 `.meta`。
#   所以要弄坏它，得**连 `.meta` 一起删**。）
# ⇒ 处置是**重建次序**（先跑本脚本、再重建场景），写在 `.gitignore` 与 `资料/命令速查.md` 里。
# ⛔ **也别为了「让检查闭嘴」把这行删掉** —— 留着它，才能把「**换成了另一张图的 guid**」
#    （那才是真漂移）和「历史遗留的这一张」区分开。
KNOWN_META_GUID_EXCEPTIONS = {
    "Card_Frame_Cost_Icon.png": "44be35390df27074bac0aa900350987e",
}


def meta_guid(png_path):
    """读 `<png>.meta` **顶格**那行 `guid:`，返回 guid 字符串；读不出 ⇒ `None`。

    三种「读不出」一律 `None`（由调用方按「这个 `.meta` 没法核」处理 —— **核不了 ≠ 核过**）：
    `.meta` 不存在 · 里面没有顶格的 `guid:` · guid 不是 32 位十六进制。
    ⚠️ 只认**顶格**的（Unity 与 `main()` 写 guid 都是顶格；缩进过的那些是别人家的字段）。
    """
    mp = png_path + ".meta"
    if not os.path.exists(mp):
        return None
    with open(mp, encoding="utf-8", errors="replace") as f:
        for ln in f:
            if ln.startswith("guid:"):
                g = ln.split(":", 1)[1].strip()
                if len(g) == 32 and all(c in "0123456789abcdefABCDEF" for c in g):
                    return g.lower()
                return None
    return None


def meta_guid_status(dst, dst_name):
    """盘上 `<dst>.meta` 的 guid 与 `guid_for(dst_name)` 比一下（**只读：一个字节都不写**）。

    返回 `(状态, 盘上 guid 或 None)`，状态是这四种之一：

      · `"no-meta"` —— `.meta` 不在 / 读不出顶格 `guid:`（**核不了 ≠ 核过**）
      · `"same"`    —— 与 `guid_for()` **一致**（这类删掉重跑也不换 guid）
      · `"known"`   —— 与 `KNOWN_META_GUID_EXCEPTIONS` 记的那个值一致（历史遗留，**不是缺陷**）
      · `"drift"`   —— **对不上**（🔴 真漂移：这张图若删掉重跑，guid 会换成 `guid_for()` 的值
                        ⇒ 已存盘场景里按老 guid 落的引用会**全部悬空**，而且**不报错**）

    🔴 **刻意不进退出码**（`main()` 的返回式里没有它）：`drift` 的**处置是重建次序**，
    不是「这次同步失败了」；而 `known` 那张的盘上 guid **本来就是对的**。
    """
    got, want = meta_guid(dst), guid_for(dst_name)
    if got is None:
        return "no-meta", None
    if got == want:
        return "same", got
    if KNOWN_META_GUID_EXCEPTIONS.get(dst_name) == got:
        return "known", got
    return "drift", got


def write_meta_template(dst, dst_name, meta_tpl):
    """给**新落盘**的 PNG 写一份 `<dst>.meta`：克隆模板的导入设置，只换 `guid` + 九宫格那一行。

    🔴 **只在「盘上真没有 `.meta`」时才该调用** —— 盘上已经有一份的话走 `patch_meta_border()`：
    重写整个文件会**连 guid 一起换掉**（已存盘场景按老 guid 落的引用会静默悬空），正是 A226 堵的那个洞。

    返回这张图的 `m_Border`（没列在 `BORDERS` 里 ⇒ `None`），调用方拿它打摘要。
    """
    border = BORDERS.get(os.path.splitext(dst_name)[0])
    # ⚠️ **判据要 strip 后再比**：模板里 `spriteBorder:` 是**缩进两格**的
    #    （`  spriteBorder: {…}`）。第一版写成 `ln.startswith("spriteBorder:")` ⇒ 永远不命中，
    #    而摘要行是按 `BORDERS` 字典打出来的 ⇒ **打印说写了、文件里是 0**（典型「自检绿口径错」）。
    #    2026-09-19 修正 + 末尾加了**回读校验**（写完再打开文件核一遍）。
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
    return border


def patch_meta_border(dst, dst_name):
    """`<dst>.meta` **已经在盘上**时：**只补/改 `spriteBorder:` 那一行**（A226 加固 · 2026-10-08）。

    这一格（**PNG 被删、`.meta` 还留着**）原来会照模板**重写整个 `.meta`** ⇒ guid 换成 `guid_for()`，
    已存盘场景里按老 guid 落的引用**全部悬空、不报错**（A226 防的就是它）。现在改成：
    **保留盘上这份 `.meta`**，只把 `spriteBorder` 按 `BORDERS` 补/改一下 —— 那是本脚本**唯一自己拥有**的
    字段（模板里那些导入设置**一律不动**：实测 36 份与模板本就不同、是 Unity 自己改写的，见文件头）。

    🔴 **`guid:` 那一行永远不碰**，写完当场**回读复核**（guid 必须与进来时逐字相同）；复核不过就抛断言。

    返回：`None` = **一个字节都没写**（这张图本就不带 border / 那一行已经是对的）；
          `str` = 一行说明 —— **写过了**、或**想写但没写**（两种情况调用方都要打出来：不许静默失败）。
    """
    mp = dst + ".meta"
    border = BORDERS.get(os.path.splitext(dst_name)[0])
    if border is None:
        return None                       # 这张图本来就没有九宫格 —— 没有任何字段要补
    want = "  spriteBorder: {x: %d, y: %d, z: %d, w: %d}" % border
    with open(mp, "rb") as f:
        data = f.read()
    # **逐字节读**（文本模式会把 `\r\n` 折成 `\n`，行尾就数不出来了 —— 2026-09-18 踩过）。
    # 行尾不是「纯 LF」或「纯 CRLF」就**不猜怎么写回去**（工程踩过：整篇翻行尾 = 文件在 git 里被重写）。
    crlf, lf, cr = data.count(b"\r\n"), data.count(b"\n"), data.count(b"\r")
    if cr != crlf:
        return (f"⚠️ 盘上这份 `.meta` 的行尾**不是纯 LF 也不是纯 CRLF**"
                f"（CRLF {crlf} / LF {lf} / 单独 CR {cr}）⇒ **没动它**")
    sep = "\r\n" if crlf else "\n"
    try:
        lines = data.decode("utf-8").split(sep)
    except UnicodeDecodeError as e:
        return f"⚠️ 盘上这份 `.meta` 不是 UTF-8（{e}）⇒ **没动它**"
    hit = [i for i, ln in enumerate(lines) if ln.strip().startswith("spriteBorder:")]
    if len(hit) != 1:
        return (f"⚠️ 盘上这份 `.meta` 里 `spriteBorder:` 有 **{len(hit)}** 条（要 1 条）⇒ **没动它**"
                f"（要不要补成 `{want.strip()}`，请人来定）")
    if lines[hit[0]] == want:
        return None                       # 已经是对的 ⇒ 一个字节都不写（幂等）
    old, before = lines[hit[0]], meta_guid(dst)
    lines[hit[0]] = want
    with open(mp, "w", encoding="utf-8", newline="") as f:   # `newline=""` ⇒ 不替我们翻行尾
        f.write(sep.join(lines))
    # 回读复核：既不许「说改了、文件里没改」，也不许**把 guid 带坏**
    assert meta_guid(dst) == before, f"{mp}：补 border 时把 guid 带坏了（{before} → {meta_guid(dst)}）"
    return f"只补/改了那一行 `{old.strip()} → {want.strip()}`（**`guid:` 未动**）"


def main():
    check = "--check" in sys.argv
    with open(META_TEMPLATE, encoding="utf-8") as f:
        meta_tpl = f.read()

    missing, added, present = [], [], []
    drifted, conflicts, unverified, multi = [], [], [], []
    # 🆕 2026-10-08（A226）：`.meta` 的 guid 体检（**只读、只报、不修，也不进退出码**）
    meta_same, meta_known, meta_drift, meta_nometa = 0, [], [], []
    # 🆕 2026-10-08（A226 加固）：**「PNG 不在、`.meta` 还在」**这一格的清单 —— 每项
    # `[目标文件名, 盘上 guid（读不出 ⇒ None）, 补 border 的说明（没补 ⇒ None）]`
    orphan_meta = []
    for name, dst_dir in ([(n, DST) for n in NAMES]
                          + [(n, DST_DECK) for n in NAMES_DECK]
                          + [(n, DST_MENU) for n in NAMES_MENU]):
        dst_name = name.replace(" ", "_") + ".png"
        dst = os.path.join(dst_dir, dst_name)
        src, copies = find_src(name)
        if len(copies) > 1 and src is not None:
            multi.append(len(copies))
        # 🔴 **副本分叉 ⇒ 不猜**（2026-10-07 · A209）：原来 `find_src` 取「第一个命中」且静默，
        #    缓存里同名副本一旦内容不同，就会**静默拷错那一份**。
        if copies and src is None:
            conflicts.append(
                f"{name}（工程里{'已有' if os.path.exists(dst) else '还没有'}这张）—— 缓存里 {len(copies)} 份同名副本"
                "**内容不一致**：" + " · ".join(
                    f"{p}（{os.path.getsize(p)} B · md5 {file_md5(p)[:12]}）" for p in copies))
            continue
        if os.path.exists(dst):
            # 🔴 **盘上 `.meta` 的 guid 对不对**（2026-10-08 · A226）—— **只读**（一个字节都不写）、
            #    **只报不改、也不算失败**：它报的是**重建次序**类风险（这张图若删掉重跑，guid 会换成
            #    `guid_for()` 的值 ⇒ 已存盘场景里按老 guid 落的引用全悬空），处置是「先跑本脚本、
            #    再重建场景」，**不是**「这次同步失败」。判据与例外见 `KNOWN_META_GUID_EXCEPTIONS`。
            _st, _got = meta_guid_status(dst, dst_name)
            if _st == "same":
                meta_same += 1
            elif _st == "known":
                meta_known.append((dst_name, _got, guid_for(dst_name)))
            elif _st == "drift":
                meta_drift.append((dst_name, _got, guid_for(dst_name)))
            else:
                meta_nometa.append(dst_name)
            # 🔴 **「文件在」≠「内容对」**（2026-10-07 · A209）：原来只看 `os.path.exists` ⇒ 磁盘上被改坏
            #    （或缓存重切过）它**不报**。现在逐字节比；对不上就点名 + 非 0 退出，**两种模式都不写它**
            #    （不擅自覆盖：对不上有两种可能 —— 我们的文件坏了 / **缓存那份变了**，处置不一样，
            #     得先让人看一眼）。
            if src is None:
                unverified.append(dst_name)     # 盘上有、缓存里没有同名源 ⇒ **核不了 ≠ 核过**
                present.append(dst_name)
                continue
            if file_md5(dst) == file_md5(src):
                present.append(dst_name)
                continue
            drifted.append(f"{dst_name}（盘上 md5 {file_md5(dst)[:12]} ≠ 缓存源 {file_md5(src)[:12]}"
                           f" · 源 {src}）")
            continue
        if src is None:
            missing.append(name)
            continue
        # 🆕 **A226 加固（2026-10-08）：「PNG 不在、`.meta` 还在」这一格** —— 先**只读地**看一眼
        #    盘上有没有 `.meta`（`meta_guid()` 只读一行，不写任何东西；`--check` 也走这一句）。
        #    下面拷完 PNG 之后按它决定**保留还是新建**。
        _orphan = [dst_name, None, None] if os.path.exists(dst + ".meta") else None
        if _orphan is not None:
            _orphan[1] = meta_guid(dst)      # 读不出顶层 `guid:` ⇒ None（**核不了 ≠ 核过**）
            orphan_meta.append(_orphan)
        if check:
            added.append(dst_name + "（待同步）")
            continue
        shutil.copyfile(src, dst)
        # 回读校验（同下面 border 那条一个道理）：落盘的字节必须与源**逐字节相同** ——
        # 「拷看着成功了、内容不对」要当场炸，不能静默（工程红线：不许静默失败）
        with open(src, "rb") as _a, open(dst, "rb") as _b:
            assert _a.read() == _b.read(), f"{dst} 落盘后与源不一致"
        # 🔴 **盘上已经有 `.meta` ⇒ 保留它（`guid:` 一个字节都不碰）**，只按 `BORDERS` 补/改
        #    `spriteBorder` 那一行（`patch_meta_border()`；本就带 border 或已是对的 ⇒ 什么都不写）。
        #    ⛔ 绝不走 `write_meta_template()` —— 那会**连 guid 一起换掉**（= A226 要堵的洞）。
        if _orphan is not None:
            if _orphan[1] is None:
                # 在盘上、但顶层 `guid:` 读不出 ⇒ **保不住** ⇒ **不写**：那等于换掉一个我们**读不出**的
                # 身份，而且换完也没法复核。「这一段谁都没覆盖过」已记进报告「没查清的部分」。
                added.append(dst_name + "（⚠️ 盘上 `.meta` 读不出 guid ⇒ **没写它**）")
            else:
                _orphan[2] = patch_meta_border(dst, dst_name)
                added.append(dst_name + "（保留原 `.meta`）")
            continue
        # 盘上真没有 `.meta` ⇒ 按模板新建（= 原行为，guid 由 `guid_for()` 算）
        border = write_meta_template(dst, dst_name, meta_tpl)
        added.append(dst_name + ("（border %s）" % (border,) if border else ""))

    print(f"已在工程里 : {len(present)} 张 {present}")
    print(f"本次同步   : {len(added)} 张 {added}")
    print(f"缓存里没有 : {len(missing)} 张 {missing}")
    if multi:
        # 只说个数（实测 62/83 个名字都有副本，逐条列出来是噪声；**真有分叉时**那一支会把每一份全点名）
        print(f"同名多副本 : {len(multi)} 个名字都有多份切片（最多 {max(multi)} 份）"
              "—— 逐字节一致 ⇒ 按路径序取第一份")
    # 🆕 **A226 加固（2026-10-08）：「PNG 不在、`.meta` 还在」这一格** —— 报出来，别静默
    #    （它原来**静默**地按 `guid_for()` 把 `.meta` 重写掉 ⇒ 已存盘场景的引用悬空、不报错）
    if orphan_meta:
        _how = ("**只补回 PNG、不重写 `.meta`**（`guid:` 一个字节没动；"
                "只有 `BORDERS` 里的那几张可能补/改 `spriteBorder` 那一行）"
                if not check else
                "**`--check` 一个字节都不写**；同步模式下会**保留它原有的 guid**（不重写 `.meta`）")
        print(f"↩️ 「PNG 不在、`.meta` 还在」: {len(orphan_meta)} 张 —— {_how}")
        for _dn, _g, _note in orphan_meta:
            if _g is None:
                print(f"   · ⚠️ {_dn}：盘上那份 `.meta` **读不出顶层 `guid:`** ⇒ **没写它**"
                      "（核不了 ≠ 核过；**不计入退出码** —— 与「`.meta` 读不出 guid」那条同族）"
                      " —— 这一格要**人**看一眼：⛔ 别让 `guid_for()` 顶上（那会换掉一个我们读不出的身份）")
                continue
            _tail = ""
            if _g != guid_for(_dn):
                _tail = (f" ⚠️ 与 `guid_for()`（{guid_for(_dn)}）**不同** ⇒ **仍保留盘上那份**"
                         "（`--check` 的「`.meta` guid 体检」会把它列进「对不上」）")
            if _note:
                _tail += f" · {_note}"
            print(f"   · {_dn}：盘上 guid {_g}{_tail}")
    # 🆕 **`.meta` 的 guid 体检**（2026-10-08 · A226）—— **只读、只报、不进退出码**
    #    （为什么不算失败，见 `meta_guid_status()` 与 `KNOWN_META_GUID_EXCEPTIONS` 的注释）
    _meta_seen = meta_same + len(meta_known) + len(meta_drift) + len(meta_nometa)
    print(f"`.meta` guid : 已落盘的 {_meta_seen} 张里 —— 与 `guid_for()` 一致 {meta_same} · "
          f"记在案的例外 {len(meta_known)} · 🔴 对不上 {len(meta_drift)} · 读不出 {len(meta_nometa)}"
          "（**只报不修、不影响退出码**）")
    for dn, got, want in meta_known:
        print(f"   ℹ️ 记在案的例外：{dn} —— 盘上 {got} ≠ `guid_for()` {want}"
              "（**不是待修**：已存盘场景就是按**盘上**这个 guid 引它的；"
              "见本文件 `KNOWN_META_GUID_EXCEPTIONS` 的注释）")
    if meta_drift:
        print(f"🔴 `.meta` guid 对不上 : {len(meta_drift)} 张（**只报不修、不算失败**）")
        for dn, got, want in meta_drift:
            print(f"   · {dn}：盘上 {got} ≠ `guid_for()` {want}")
        print("   ⇒ **这几张别删、别重导 `.meta`** —— 已存盘场景是**按盘上那个 guid** 引它们的"
              "（实测 `Card_Frame_Cost_Icon.png` 一张就 **357** 处：`CollectionCheck.unity` 313 · "
              "`DeckEditor.unity` 29 · `CardBase.unity` 15，形态 `m_Texture: {fileID: 2800000, guid: …}`）"
              "⇒ 删掉重跑会换成 `guid_for()` 的值、那些引用**全部悬空**（少一张图、**不报错**）。"
              "正解 = **先跑本脚本（图）→ 再重建场景**（`CollectionScene.Run` / "
              "`DeckScene.BuildAndSaveScene` / `CardBaseDemo.Run`），重建时按**当时盘上**的 guid 落引用。"
              "（🔴 **2026-10-08 A226 加固后**：**只删 PNG、留 `.meta`** 已是安全的 —— 那时脚本**保留**盘上这份 "
              "`.meta`；要把 guid 换掉得**连 `.meta` 一起删**。）")
    if meta_nometa:
        print(f"⚠️ `.meta` 读不出 guid : {len(meta_nometa)} 张（**核不了 ≠ 核过**）{meta_nometa}")
    if drifted:
        print(f"🔴 盘上内容对不上 : {len(drifted)} 张（**没写它** —— 本脚本不擅自覆盖）")
        for d in drifted:
            print(f"   · {d}")
        print("   ⇒ **这不是成功**：这几张图与切片缓存**逐字节不同**。两种可能，处置不一样："
              "① 工程里那份被改坏 ⇒ 直接拿缓存源盖回来（`cp \"<源>\" \"<盘上>\"`，**这样不会动 `.meta`**），"
              "或**删掉那张 PNG** 再重跑本脚本（✅ **只删 PNG 不删 `.meta` ⇒ guid 不变**，2026-10-08 A226 "
              "加固起；见上面「PNG 不在、`.meta` 还在」那条）—— ⚠️ **连 `.meta` 一起删**才会按模板重写它"
              "（guid 变成脚本的 `guid_for()`）⇒ 那样做之前**先确认那三份场景会被重建**（`CollectionCheck.unity` / "
              "`DeckEditor.unity` / `CardBase.unity`；判据与次序见上面那条「`.meta` guid 体检」）；"
              "② **缓存那份变了**（重切过 / 换了解包源）⇒ 先看清是不是我们要的那张，别直接覆盖。")
    if conflicts:
        print(f"🔴 同名副本分叉 : {len(conflicts)} 个（**不猜取哪一份**）")
        for c in conflicts:
            print(f"   · {c}")
        print("   ⇒ **这不是成功**：切片缓存里同名文件有内容不同的几份 ⇒ 取哪一份是**判据**、不许脚本猜。"
              "先按包名确认哪一份是原版的，把其余几份移走（别删解包源），再重跑。")
    if unverified:
        print(f"⚠️ 核不了 : {len(unverified)} 张（工程里有、但切片缓存里找不到同名源 ⇒ **这不是「核过」**）"
              f"{unverified}")
    # 🔴 **不静默**（2026-10-07 · A204）：缺一张就**不是退出码 0** —— 原来这里只印一行就返回，
    #    而整棵 `Resources/Art/` 在 `.gitignore` 里（构建产物）⇒ 新克隆的人「跑过了、看着像成功」，
    #    实际那几张图从没落盘、运行时静默取不到（A204 要补的正是这个洞）。
    if missing:
        print(f"🔴 有 {len(missing)} 张**没同步**（切片缓存里按名字找不到）：{missing}")
        print("   ⇒ **这不是成功**：那几张图现在**不在工程里**，运行时取不到。"
              "先确认 `d:/2/Warpforge_tools/data/ui_extract/` 在不在、名字有没有写错（空格/下划线）。")
    # 退出码 0 的含义（2026-10-07 · A209 起）：**每一张都能证明是对的**（缺 / 对不上 / 核不了 / 副本分叉 ⇒ 1）
    return 1 if (missing or drifted or conflicts or unverified) else 0


if __name__ == "__main__":
    raise SystemExit(main())
