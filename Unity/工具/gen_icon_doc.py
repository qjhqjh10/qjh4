# -*- coding: utf-8 -*-
"""从 `数据/游戏数据/card_icon_plan.json` 生成人看的文档：`资料/卡面图标_对照与缺口.md`。

为什么单独一个脚本：计划表是**机器**用的（渲染层按 `sprite` 取值），文档是**人**看的
（哪个 token 配哪张图、凭什么、还缺什么）。两者同源，就不存在「文档和实现对不上」。

用法：`PYTHONIOENCODING=utf-8 python 工具/gen_icon_doc.py`
"""
import json
import os
import sys
import collections

sys.stdout.reconfigure(encoding="utf-8")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PLAN = os.path.join(ROOT, "数据/游戏数据/card_icon_plan.json")
OUT = os.path.join(ROOT, "资料/卡面图标_对照与缺口.md")
TRAITS = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/Art/traits")
UI = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/Art/ui")


def main():
    cards = json.load(open(PLAN, encoding="utf-8"))["cards"]      # 数组形状（JsonUtility 能吃的那份）
    have_t = {os.path.splitext(f)[0] for f in os.listdir(TRAITS) if f.endswith(".png")}
    have_u = {os.path.splitext(f)[0] for f in os.listdir(UI) if f.endswith(".png")}

    rows, gaps, sprite_use = [], [], collections.Counter()
    for card in cards:
        cid = card["id"]
        for field_obj in card["fields"]:
            field = field_obj["name"]
            for it in field_obj["items"]:
                tok, sp, why = it["token"], it.get("sprite", ""), it.get("why", "")
                if sp:
                    sprite_use[sp] += 1
                else:
                    gaps.append((cid, field, tok, why))
                rows.append((cid, field, tok, sp, why))

    # 「三、记号怎么用」那节的四类计数 —— **算出来**，别手写（手写的迟早过期）。
    # 判据照 `CardIcons.Rewrite`：首字符是字母/数字 = 词；「图名里有数字」= 数字烘在图里
    # ⚠️ 「数字烘在图里」和前三类**有重叠**（它问的是图名，不是 token 长什么样）
    def _word_start(t):
        """首字符是字母或数字（= `Rewrite` 里那句 `char.IsLetter(c0) || (c0 >= '0' && c0 <= '9')`）。"""
        return bool(t) and (t[0].isalpha() or ("0" <= t[0] <= "9"))

    n_br = sum(1 for r in rows if r[2].startswith("["))                                 # 方括号记号
    n_sym = sum(1 for r in rows if not r[2].startswith("[") and not _word_start(r[2]))   # 符号（☀ ① ⚡ 💀 …）
    n_num = sum(1 for r in rows if any("0" <= ch <= "9" for ch in r[3])
                and r[2].lower() not in r[3].lower())                                   # 数字烘在图里

    lines = []
    A = lines.append
    A("# 卡面图标 —— 记号 → 图标 对照与缺口（2026-09-15）")
    A("")
    A("> **由 `工具/gen_icon_doc.py` 从 `数据/游戏数据/card_icon_plan.json` 生成，别手改。**")
    A("> 计划表本身由 `工具/gen_icon_plan.py` 生成（表在脚本里，每条带证据）。")
    A("> 这条线要解决的问题、为什么要逐张卡定而不是一张全局表，见 `卡面图标_现状与缺口.md`。")
    A("")
    A("## 一、结论（一句话）")
    A("")
    A(f"**{len(cards)} 张卡**的效果文字里有 **{len(rows)} 处**需要画图标的位置，"
      f"其中 **{len(rows) - len(gaps)} 处**已定到具体 sprite，**{len(gaps)} 处是缺口**（下面第五节）。")
    A("原版那份 TMP sprite asset（`Warpforge Trait TextSprites`）已照抄建成我们自己的，"
      "`IconSetup.Verify` 报 **270/270** 计划表里的 sprite 名都查得到。")
    A("")
    A("## 二、🔴 为什么**不能**用一张全局表")
    A("")
    A("这些方括号记号（`[Attack]` / `[Armor]` / `[Health]` …）**不是原版数据**，是**卡图 OCR 猜的**。")
    A("同一个记号在不同卡上指向**不同**图标 —— 逐张看过成品卡图，三对铁证：")
    A("")
    A("| 卡 | 我们的数据 | 卡面上画的是 | 说明 |")
    A("|---|---|---|---|")
    A("| DA44 `Ancient Reliquary` | `+3 [Attack], +3 [Armor]` | `+3`【粉拳】`, +3`【紫枪】 | `[Attack]`=近战、`[Armor]`=远程 |")
    A("| EC8 `Alluress` | `-2 [Health] and -2 [Attack]` | `-2`【粉拳】`and -2`【紫枪】 | `[Health]`=近战、`[Attack]`=**远程** |")
    A("| EC17 `Sonic Blaster Noise Marine` | `-1 [armor] and -1 [attack]` | `-1`【粉拳】`and -1`【紫枪】 | 与上一行**正好相反** |")
    A("")
    A("⇒ **名字不可信，位置才可信**。所以本表是**按卡**的（每条都能追到卡图或逐张还原表原文）。")
    A("")
    A("## 三、记号在卡面上**怎么用**（换法 = `CardPresentation/Core/CardIcons.cs` 的 `Rewrite`）")
    A("")
    A("**四类，换法完全不同** —— 判据是「**这个 token 在卡面上是「字」、还是「图标本身」**」。"
      f"计划表里 **{n_br} 处方括号 / {len(rows) - n_br - n_sym} 处裸关键词 / {n_sym} 处符号**"
      f"（共 {len(rows)} 处）；另有 **{n_num} 处「数字烘在图里」** —— 它和前三类**有重叠**"
      "（问的是「图名里有没有数字」，不是 token 长什么样）。")
    A("")
    A("| 类 | 判据 | 长什么样 | 怎么画 |")
    A("|---|---|---|---|")
    A("| **方括号记号** | token 以 `[` 开头 | `[Attack]` / `[Spirit Stone]` / `[护甲]` … | "
      "**换掉**（词不留）：`[Attack]` → `<sprite name=\"Melee\">`。那是**卡图 OCR 猜出来的占位** "
      "—— 卡面那个位置**本来就没印这个词**，只有图标 |")
    A("| **裸关键词** | 首字符是**字母或数字** | `Waystone.` / `路标石。` / `集结：` / `Blast 1.` | "
      "**图标插在词前面、词留着**：`Waystone.` → `<sprite name=\"waystone\">Waystone.`。"
      "卡面上真印着那个词，原版就是「**图标 + 紧跟那个词**」 |")
    A("| **符号** | 首字符**不是**字母/数字 | `☀` / `①` / `⚡` / `💀` … | "
      "🔴 **整串吃掉、只留图标**。那个字符**就是那张图**（OCR 把图标抄成了字符）—— "
      "留着会「图标 + 那个字符」**画两遍** |")
    A("| **数字烘在图里** | **图名里有数字**（`Rewrite` 的 `numInArt`：图名带数字、"
      "且 token 不是图名的子串） | `1 Quest Point` / `3 [Spirit Stone]` / `①` | "
      "🔴 **连数字一起整串吃掉、只留图标**：`1 Quest Point` → "
      "`<sprite name=\"questPoints1\">`。那个数字已经印在图上了 |")
    A("")
    A("⚠️ **判据别写成「单字符」** —— `☀` 是单字符，但 `①`(U+2460) **不是** ASCII 数字、"
      "`1 Quest Point` 更不是单字符。写「**首字符是不是字母/数字**」和「**图名里有没有数字**」"
      "才准（`Rewrite` 里判的就是这两句）—— 判错会静默画错或画两遍。")
    A("")
    A("两条实现细节（踩过才写下来的，改 `Rewrite` 时别丢）：")
    A("")
    A("1. **长的 token 先换** —— 短 token 是长 token 的子串时（`Armour` ⊂ `Armour 1`），先换短的会把"
      "长的那条**打散**、它再也匹配不上。同长时另按 token 排序，因为 `List.Sort` **不稳定**"
      "—— 不补这条，两份内容一样的数据可能换出不同结果（**不可复现**）。")
    A("2. **整个替换是幂等的** —— 同一份文字会被换**两遍**（`SetData` 会把同一份 `CardData` "
      "再喂给卡面一次）。不判的话，第二遍会给**已经带图标的词**再插一个图标"
      "（实测 `[践踏]` 第二遍变成两个图标 + 词）。")
    A("")
    A("⚠️ 一开始**两种记号都当「换掉」**处理，结果关键词整串消失、卡面只剩图标（`Lychguard` 实测）；"
      "而符号（`☀` `⚡` …）一度被归进裸词那一支 ⇒ 会「图标 + 那个字符」**画两遍** "
      "—— 上表分四行就是为了这两条。")
    A("")
    A("## 四、按规则就能定的那几类（不按卡）")
    A("")
    A("| 记号 | 画哪张 | 判据 |")
    A("|---|---|---|")
    A("| `N [Spirit Stone]:` | `SpiritStone_N` | **档位数字烘在图里** —— 图集 `Atlas_SpiritStone_1..5` "
      "逐张看过（绿圈里就是数字），真卡 `Spiritseer(2)` / `Wraithknight(3)` 核过"
      "（换法见第三节「数字烘在图里」：`N` 连记号一起吃掉） |")
    A("| `[Energy]` / `[能量]` / `[faith]` / `[Faith]` / `[Icon]` / `[icon]` / `[Faith Icon]` | `faith` | "
      "**行首付费前缀**画的是「这一行的资源图标」。修女会 = 金太阳。逐张核过 7 张"
      "（Sister Novitiate / Blade of Faith / Sacred Rose / Paragon Warsuit / Preacher / Miraculous Feat / Daemonbreaker）"
      "—— 数据里名字五花八门，**卡面全是同一张金太阳**，数字写在图标**外面左侧** |")
    A("| `☀` | `faith` | 同上（中文卡面写法） |")
    A("| `①`…`⑤` | `SpiritStone_1..5` | 中文卡面写的是圈码，英文写 `N [Spirit Stone]`，同一张图 |")
    A("| `⚡` | `rally` | 触发前缀。`rally.png` 就是暗金圆底 + 米白闪电 |")
    A("| `💀` | `backlash` | `💀 Backlash:` 里那个骷髅（橄榄黄圆底 + 白骷髅）。"
      "⚠️ 别和 `[skull]` 混 —— 那一个在 `Slay:` 前面、是**橙色**骷髅（`slay.png`） |")
    A("| `🗡` / `⚔` | `Melee` | 近战 |")
    A("| `🔫` | `Ranged` | 远程 |")
    A("| `🛡` | `shield` | 护盾（卡面就是「🛡护盾」） |")
    A("| `❄` | `markOfSlaanesh` | 暗黑契约·纵欲。⚠️ 五张 `markOf*` 切片**逐字节相同**，"
      "认不出是哪位邪神，只能按名字选 |")
    A("")
    A("## 五、缺口（**原版有、我们还没有**）")
    A("")
    if gaps:
        A("⏳ **下面这几处待用户裁决**（2026-09-15 用户点名「你记一下，之后我来裁决」）。"
          "三种可选处理：**① 用现有的近似图**（例如 `[Destroyer]` 用 `savageOrk` 那类近义图标顶）"
          "**② 我们自己画一张** **③ 就这么空着**（卡面上留文字，见 `CardIcons.Rewrite` 的缺口分支）。"
          "裁完把结论写进 `资料/卡面图标_现状与缺口.md` 的「待裁」一节，并回到 `工具/gen_icon_plan.py` "
          "的 `NO_SPRITE` / `GAP_BY_CARD` 改表、重跑本工具。")
        A("")
        A("| # | 卡 | 字段 | 记号 | 为什么还没定 |")
        A("|---|---|---|---|---|")
        for i, (cid, field, tok, why) in enumerate(gaps, 1):
            A(f"| {i} | `{cid}` | {field} | `{tok}` | {why} |")
    else:
        A("（无）")
    A("")
    A("### 另外三件**素材侧**的缺口（不在上面的表里，但画之前要知道）")
    A("")
    A("1. **`Dark Pacts` / `Penitence` 没有独立图标**：规则书里有这两个关键词，"
      "但 78 张图集切片里没有对应的图（`资料/关键词图标/关键词与图标_对照表.md:189`）。"
      "卡面上暗黑契约画的是**暗红盘 + 白八芒星**（`markOfChaos`，五张 `markOf*` 逐字节相同）。")
    A("2. **生命 / 费用类数值图标**：图集里**只有 `Melee`（拳）与 `Ranged`（枪）**；"
      "`+N Health` 在卡面上**永远是文字、没有图标**（已核）；护甲有裸词 `Armour N`（可带银盾图标前缀）。")
    A("3. **字体缺字**：`💀` `🗡` `🔫` `🛡` 这 4 个字符 **NotoSerifCJK 里没有**（本机解 cmap 实测）"
      "—— 所以它们在卡面上**必须走图标渲染**，靠字体永远画不出来。"
      "（`⚡` `⚔` `❄` **有**字，`资料/卡面图标_现状与缺口.md:40` 把这三个也写成缺字，**是错的**，已更正。）")
    A("")
    A("## 六、逐卡明细")
    A("")
    A("| 卡 | 字段 | 记号 | sprite | 证据 |")
    A("|---|---|---|---|---|")
    for cid, field, tok, sp, why in sorted(rows):
        A(f"| `{cid}` | {field} | `{tok}` | {('`' + sp + '`') if sp else '**缺口**'} | {why} |")
    A("")
    A("## 七、素材家底（2026-09-15 实测）")
    A("")
    A(f"- 关键词/数值图标：`Resources/Art/traits/` **{len(have_t)} 张**"
      "（`40ktraiticonatlas` 78 张：73 张 `Atlas_trait_icon_*` + 5 张 `Atlas_SpiritStone_*`）。")
    A(f"  ⚠️ 2026-09-15 之前**只有 73 张** —— 导入脚本只认 `Atlas_trait_icon_` 一个前缀，"
      "5 张灵魂石被静默跳过（`工具/import_original_art.py` 已修）。")
    A("- 原版图集原图：`Assets/CardPresentation/Icons/40k_Trait_icon_atlas.png`（1024×1024，"
      "78 张切片全是 80×80 / `m_PixelsToUnits=100` / pivot 0.5,0.5）。")
    A("- **TMP sprite asset**：`Resources/Fonts/Warpforge Trait TextSprites.asset`"
      "（96 字形 / 156 字符 —— 原版全名 + 我们的短名别名）。"
      "**照抄原版**：字形表来自 `bundle_fonts_assets_all` 的 `Warpforge Trait TextSprites`。")
    A("- 关键词徽标底板：`Resources/Art/ui/Base3d_Trait_Background.png`"
      "（原版棋盘单位卡上 7 个 `TraitIconContainer` 里那个 `Trait Icon Background`）。")
    A("- 原版卡面文字参数（照抄用）：`DescTextUnit` = TMP，`m_fontSize 23.55` / `autoSize 1` "
      "（min 1 / max 24）/ `m_lineSpacing 5` / 居中；卡面 em = 0.2355 卡单位 = 卡高 7.07%。")
    A("")
    A("## 八、出处")
    A("")
    A("- 计划表与生成器：`Unity/工具/gen_icon_plan.py` → `数据/游戏数据/card_icon_plan.json`")
    A("- 逐张还原表（**卡图逐张看出来的**，本表的主要证据）：`资料/卡表核对_卡图提取/_还原效果文字.md`")
    A("- 图标↔关键词对照：`资料/关键词图标/关键词与图标_对照表.md` · `_图标清单.md`")
    A("- 成品卡图：`d:/2/Warpforge部队卡片/<阵营>/…`（900×1200）")
    A("- 原版 sprite asset / 字形表：`bundle_fonts_assets_all/MonoBehaviour/Warpforge Trait TextSprites.json`")
    A("  （dump 工具 `工具/dump_trait_textsprites.py` → `数据/游戏数据/trait_textsprites.json`）")
    A("- 建资产 + 自检：`-executeMethod IconSetup.Run` / `IconSetup.Verify`（探针图 `d:/4/_tmp_view/icons/`）")
    A("")
    A("## 九、⚠️ 还没标定的一件事（画之前要量）")
    A("")
    A("**图标的绝对大小**。原版那份 sprite asset 的 `m_FaceInfo` 是 **0**（`pointSize: 0`），"
      "TMP 遇到这种资产会**退回用字体资产的 face info 算缩放**")
    A("（`com.unity.ugui…/Runtime/TMP/TMP_Text.cs:4154-4166`：`ascentLine / metrics.height` 那一支）。")
    A("而原版卡面用的字体是 `Pragati-Regular SDF`，我们用的是 `NotoSerifCJK-Regular SDF` —— ")
    A("**换字体就会换图标大小**。⇒ 画之前要**标定一次**：拿探针量「图标高 ÷ 大写字母高」，")
    A("再与成品卡图上的同一个比值比，差多少、要不要按「我们挑的」缩放并如实标注。")
    A("（字形自身的 `m_Scale ∈ {1.65, 1.7}` 与 `metrics 80×80 / bearingY 60` 已经是**原版的值**，别动。）")
    A("")

    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))
    print(f"落盘 {OUT}（{len(lines)} 行；逐卡明细 {len(rows)} 行；缺口 {len(gaps)} 处）")
    print("sprite 使用频次 top:", sprite_use.most_common(8))


if __name__ == "__main__":
    main()
