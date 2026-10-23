# -*- coding: utf-8 -*-
"""关键词（trait）tooltip 的文案 → `Resources/trait_tips.json`。

## 🔴 2026-10-14（`A1803`）就地改源头：**官方表优先，规则书只当兜底**

原来这份产物的正文**全部**来自《Warpforge Offline Rulebook》的中文翻译
（`资料/关键词图标/_规则书关键词表.md`）—— 那是**粉丝实体版规则书**（它自己第 5 行写着
`Not official. Fan project.`，见 `CLAUDE.md` 那条权威口径），**不是原版文案**。
2026-10-14 官方 zh-CN 词条表解开之后，正文的正确来源是
`数据/游戏数据/官方本地化_en+zh-CN.json` 的 **`CardTraitDescription/<官方键>`**。

⇒ 本脚本现在的取文顺序：**① 官方 `CardTraitDescription/*` → ② 规则书（只给官方确实没有的那几个词）。**

## 官方键 → 我们的规范键（这一段**不是我们发明的**）

官方 `CardTraitDescription/<key>` 里有一部分的 `key` 与我们 `Core/Loc.cs` 的
`Card_Trait/<规范键>`（= `KeywordTable.Normalize` 出来的小写规范名）**拼法不同**：

| 官方键 | 我们的规范键 | 判据 |
|---|---|---|
| `frenzied` | `destroyer` | `Core/Badges.cs` 的 `SpriteKey`（图名 `frenzied` = 关键词 `Destroyer`）|
| `rage` | `penitence` | 同上（图名 `rage` = 关键词 `Penitence`）|
| `markOfChaos` | `darkpact` | 同上（图名 `markOfChaos` = 关键词 `Dark Pacts`）|
| `concussive` | `concussion` | 同上（图名 `concussive` = 关键词 `Concussion`）|
| `questPoints` | `quest` | 内容同一（官方「每 3 点任务进度加一张隐秘」↔ 规则书 `:199`「每获得 3 点任务：向牌库加入 1 张隐秘并洗牌」）—— ⚠️ **表之外的一条**，见下面 `EXTRA_ALIAS` |

前四条**直接读 `Badges.cs` 那张表**（`SpriteKey` 的**反向**就是「官方键 → 规范键」，
⛔ 不在这里抄第二份）。第五条 `questPoints` **没有代码判据**，单独列在 `EXTRA_ALIAS` 里、
**每条都要写清理由**（铁律 2：不猜）。

其余官方键（`battleHonour` / `berzerk` / `chooseOne` / `combo` / `counterOffensive` /
`courage` / `firstStrike` / `frontLine` / `jam` / `large` / `notCollectable` / `relentless` /
`resolution` / `support` / `flying_old` / `frenzied_old` / `markOf{Khorne,Nurgle,Slaanesh,Tzeentch}`）
**在我们这 62 个规范键里没有对应**（我们没有那些关键词的显示名）⇒ 本脚本**不用它们、
也不凭空造键**，只在报告里列出来（那是「`Card_Trait/*` 显示名」那一批的账）。

## 产物形状（v2）

```json
{ "version": 2, "note": …, "source": …,
  "entries": [ {"key":"armour","zh":"护甲","en":"Armour",
                "bodyZh":"受到的伤害减少X点，最少为1",
                "bodyEn":"Damage received is reduced by X, to a minimum of 1",
                "src":"official"} ] }
```
· `key` = **规范键**（运行时 `TipText.Trait` 按它查；原来按「中文显示名」查，v2 起改直查规范键）。
· `zh` / `en` = **我们自己的显示名**（`Core/Loc.cs` 的 `Card_Trait/*` 两列）—— 只作出处/自检用，
  界面上那行标题走 `CardText.KeywordDisplay`，⛔ 不从本文件取。
· `src` = `official` / `rulebook` / `none`（`none` = 官方与规则书都没有 ⇒ 正文空，
  `TipText.Trait` 会如实印「没有这个词的条目」，⛔ **不许编一句**）。
· 形状是**数组**不是字典 —— `JsonUtility` 读不了字典（同 `card_icon_plan.json`）。

用法：
  python -I -X utf8 d:/4/Unity/工具/gen_trait_tips.py            # 干跑，只报告
  python -I -X utf8 d:/4/Unity/工具/gen_trait_tips.py --write    # 落盘
"""
import io
import json
import os
import re
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n  python -I -X utf8 d:/4/Unity/工具/gen_trait_tips.py            # 干跑，只报告\n  python -I -X utf8 d:/4/Unity/工具/gen_trait_tips.py --write    # 落盘\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))          # d:/4/Unity
RULEBOOK = os.path.join(ROOT, "资料/关键词图标/_规则书关键词表.md")
OFFICIAL = os.path.join(ROOT, "数据/游戏数据/官方本地化_en+zh-CN.json")
LOC_CS = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Core/Loc.cs")
BADGES_CS = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Core/Badges.cs")
OUT = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/trait_tips.json")
WRITE = "--write" in sys.argv

# ---- 表之外的那一条别名（⛔ 别往里加没写理由的）----------------------------------
EXTRA_ALIAS = {
    # 官方键 -> 规范键；理由：官方 `CardTraitDescription/questPoints`
    #   = "Add a Secret to your deck for each 3 Quest Progress earned."
    #     「为每赚取的3点任务进度向你的卡组中添加一个秘密。」
    #   与规则书 `:199`「每获得 3 点任务：向牌库加入 1 张隐秘（Secret）并洗牌」**同一件事**
    #   ⇒ 二者是同一个 trait 的两种键名。⚠️ 今天全池 **0 张卡**的关键词是 `Quest`
    #   （`CardDef.cs` 那条注释自己写着），所以这一条**今天零可见影响**，接上是为了将来。
    #   🔴 `questpoints`（`Badges.KeyOf("questPoints2")` 的产物）**不是**本条 ——
    #      那条走 `Tooltip.ByLink` 的 `Tips/Hud/*` 分支，⛔ 别混。
    "questPoints": "quest",
}

# 规则书表里的「效果（规则书原文）」列，头两行是表头/分隔
EN_ONLY = re.compile(r"^[A-Za-z][A-Za-z' \-]*$")
LOC_PAT = re.compile(
    r'\{\s*"([^"]+)"\s*,\s*new Entry\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)\s*\}')
SPRITE_KEY_PAT = re.compile(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}')


def norm(s):
    """归一化：只留小写字母数字（与 `Badges.Norm` 同口径 —— 「Armour 2」「Pin Down」都能配上）。"""
    return re.sub(r"[^0-9a-z]", "", (s or "").lower())


def read_loc_traits():
    """`Core/Loc.cs` 的 `Card_Trait/<规范键>` → (zh, en)。**规范键表只此一处**。"""
    s = io.open(LOC_CS, encoding="utf-8").read()
    out = {}
    for m in LOC_PAT.finditer(s):
        k, zh, en = m.group(1), m.group(2), m.group(3)
        if k.startswith("Card_Trait/"):
            out[k[len("Card_Trait/"):]] = (zh, en)
    return out


def read_sprite_key():
    """`Core/Badges.cs` 的 `SpriteKey`：图名 → 规范键。**反向 = 官方键 → 规范键**。"""
    s = io.open(BADGES_CS, encoding="utf-8").read()
    i = s.find("SpriteKey = new Dictionary<string, string>")
    if i < 0:
        sys.exit("❌ 读不到 `Badges.SpriteKey` —— 判据表搬走了，本脚本要跟着改（⛔ 别把表抄进来）")
    j = s.find("};", i)
    out = {}
    for m in SPRITE_KEY_PAT.finditer(s[i:j]):
        out[m.group(1)] = m.group(2)
    return out


def read_official():
    """官方表 → {族/键: (en, zh)}。值可能是字符串（en==zh）或两元组。"""
    t = json.load(io.open(OFFICIAL, encoding="utf-8"))["terms"]
    out = {}
    for k, v in t.items():
        if isinstance(v, list):
            out[k] = (v[0] if len(v) > 0 else "", v[1] if len(v) > 1 else "")
        else:
            out[k] = (v or "", v or "")
    return out


def read_rulebook():
    """规则书表 → [(中文名, 英文名, 正文)]（附录行跳过；判据同 v1）。"""
    rows, skipped, started = [], 0, False
    for ln in io.open(RULEBOOK, encoding="utf-8"):
        if ln.startswith("## "):
            if started:
                break
            continue
        if not ln.startswith("|"):
            continue
        p = [x.strip() for x in ln.strip().strip("|").split("|")]
        if len(p) < 4 or p[0] in ("中文名", "") or set(p[0]) <= set("-: "):
            continue
        if not EN_ONLY.match(p[1]):
            skipped += 1
            continue
        started = True
        # 🔴（`A877`，保留 v1 的处置）：规则书引文里的「督军」在**游戏界面上**一律印「战将」。
        #   ⛔ **源表一个字不动** —— 替换只发生在生成这一步。官方正文**不替换**（原样照抄）。
        rows.append((p[0], p[1], p[2].replace("督军", "战将")))
    return rows, skipped


def main():
    traits = read_loc_traits()                      # 规范键 -> 我们显示名（62）
    sprite_key = read_sprite_key()
    official = read_official()
    rules, skipped = read_rulebook()

    inv = {}                                        # 官方键 -> 规范键
    for sprite, canon in sprite_key.items():
        inv.setdefault(sprite, canon)
    for off, canon in EXTRA_ALIAS.items():
        inv.setdefault(off, canon)

    offdesc = {k.split("/", 1)[1]: v for k, v in official.items()
               if k.startswith("CardTraitDescription/")}
    offname = {k.split("/", 1)[1]: v for k, v in official.items()
               if k.startswith("Card_Trait/")}

    # 规则书行 → 规范键（按英文名，其次中文名；两边都归一化）
    rb_by_key, rb_unmatched = {}, []
    for zh, en, body in rules:
        hit = None
        for key, (tzh, ten) in traits.items():
            if norm(ten) == norm(en) and norm(en):
                hit = key
                break
        if hit is None:
            for key, (tzh, ten) in traits.items():
                if norm(tzh) == norm(zh) and norm(zh):
                    hit = key
                    break
        if hit is None or hit in rb_by_key:
            rb_unmatched.append((zh, en))
        else:
            rb_by_key[hit] = (zh, en, body)

    entries, n_off, n_rb, n_none, no_off = [], 0, 0, 0, []
    for key in sorted(traits):
        tzh, ten = traits[key]
        # ① 官方：先按「官方键 → 规范键」的反表，再按键名大小写无关，最后按官方显示名
        offkey = None
        for ok, canon in inv.items():
            if canon == key and ok in offdesc:
                offkey = ok
                break
        if offkey is None:
            low = {k.lower(): k for k in offdesc}
            if key in low:
                offkey = low[key]
        if offkey is None:
            byen = {norm(v[0]): k for k, v in offname.items()}
            if norm(ten) in byen and byen[norm(ten)] in offdesc:
                offkey = byen[norm(ten)]
        if offkey is not None:
            en, zh = offdesc[offkey]
            entries.append({"key": key, "zh": tzh, "en": ten,
                            "bodyZh": zh, "bodyEn": en, "src": "official"})
            n_off += 1
            continue
        no_off.append(key)
        # ② 规则书兜底（官方确实没有这个词）
        if key in rb_by_key:
            _zh, _en, body = rb_by_key[key]
            entries.append({"key": key, "zh": tzh, "en": ten,
                            "bodyZh": body, "bodyEn": "", "src": "rulebook"})
            n_rb += 1
        else:
            entries.append({"key": key, "zh": tzh, "en": ten,
                            "bodyZh": "", "bodyEn": "", "src": "none"})
            n_none += 1

    print("规范键 %d 个（`Core/Loc.cs` 的 `Card_Trait/*`，现读）" % len(traits))
    print("  · 官方 `CardTraitDescription/*` 命中 %d 个（正文 + 官方英文）" % n_off)
    print("  · 官方没有 ⇒ 退回规则书 %d 个：%s"
          % (n_rb, ", ".join(e["key"] for e in entries if e["src"] == "rulebook")))
    print("  · 官方与规则书都没有 %d 个：%s"
          % (n_none, ", ".join(e["key"] for e in entries if e["src"] == "none")))
    print("规则书表：列形合格 %d 行（附录行跳过 %d 行）" % (len(rules), skipped))
    if rb_unmatched:
        print("⚠️ 规则书里对不上任何规范键的 %d 行（官方也没有 ⇒ 正文就空着）：%s"
              % (len(rb_unmatched), rb_unmatched[:8]))
    used = set()
    for k, c in inv.items():
        if c in no_off:
            used.add(k)
    unused = sorted(set(offdesc) - set(
        ok for ok, canon in inv.items() if canon in traits) - set(
        ok for ok in offdesc if ok.lower() in traits) - set(
        ok for ok in offdesc if norm(offname.get(ok, ("", ""))[0]) in
        set(norm(v[1]) for v in traits.values())))
    print("官方 `CardTraitDescription/*` 共 %d 键；我们没显示名、本表不收录的 %d 个：%s"
          % (len(offdesc), len(unused), ", ".join(unused)))

    payload = {
        "version": 2,
        "note": "关键词（trait）tooltip 的文案。由 工具/gen_trait_tips.py 生成，别手改。"
                "**正文 = 官方 zh-CN 词条表的 `CardTraitDescription/*`（`src:official`）**；"
                "官方确实没有的那几个词退回《Warpforge Offline Rulebook》（粉丝实体版，"
                "`src:rulebook`，那几条**不是原版文案**）；两边都没有的 `src:none`（正文空，"
                "运行时如实印「没有这个词的条目」，⛔ 不编）。"
                "键 = `KeywordTable.Normalize` 出来的**小写规范名**。"
                "⚠️ 形状是**数组**不是字典 —— `JsonUtility` 读不了字典（同 `card_icon_plan.json`）。",
        "source": "数据/游戏数据/官方本地化_en+zh-CN.json（`CardTraitDescription/*`）"
                  " + 资料/关键词图标/_规则书关键词表.md（仅兜底）",
        "entries": entries,
    }

    if not WRITE:
        print("（干跑，没写文件）")
        for e in entries[:4] + [x for x in entries if x["src"] != "official"][:3]:
            print("   %s" % json.dumps(e, ensure_ascii=False)[:150])
        return 0

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(payload, f, ensure_ascii=False, indent=1)
    print("落盘 %s（%d 条）" % (OUT, len(entries)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
