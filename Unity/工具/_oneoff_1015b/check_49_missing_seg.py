# -*- coding: utf-8 -*-
"""核 R3 §6 那 49 张：我们少的「那一段」（关键词声明段）**有没有被 `keywords` 列兜住**。
兜住了 ⇒ 卡面照样印得出来（走 `CardText.KeywordSegment`），不是缺陷；
没兜住 ⇒ 卡面真的缺那一段。只读。"""
import json, io, re, sys

POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

GROUPS = {
    "ephemeral": ["Greatest Deeds", "Might of Heroes", "Da Red Waaagh", "Greatest Warboss",
                  "Hymn of Battle", "Wreckin Ball", "Follower of Gork", "Worst Temper",
                  "Master of the Fleet", "Ramshackle", "Mekaniak", "More Dakka", "Sawbonez",
                  "Scryer's Gaze", "Brood Progenitor", "Smite", "Spear of Macragge",
                  "Master Tactician", "Skyborne Deployment", "Righteous Repugnance", "Master of Arcana"],
    "vanguard": ["Wraithblade", "Wraithguard", "Vengeful Wraithblade", "Crusader", "Celestian",
                 "Celestian Sacresant", "Primaris Judiciar"],
    "flying": ["Hemlock Wraithfighter", "Shining Spear", "Hornet", "Seraphim", "Veteran Flyboy",
               "Primaris Inceptor", "Inceptor Sergeant"],
    "armour": ["Wraithknight", "Gorkanaut", "Immolator", "Exorcist"],
    "waystone": ["Storm Guardian", "Warlock", "Spiritseer"],
    "unstable": ["Bomb Squig", "Killa Kan", "Battlewagon"],
    "stealth": ["Sergeant Taaman", "Reiver", "Death Cult Assassin"],
    "flank": ["Arco-Flagellant"],
}

# 每个关键词在中英文里的常见写法（用来在 `keywords` 列里找）
ALIAS = {
    "ephemeral": ["ephemeral"],
    "vanguard": ["vanguard"],
    "flying": ["flying"],
    "armour": ["armour", "armor"],
    "waystone": ["waystone"],
    "unstable": ["unstable"],
    "stealth": ["stealth", "camouflage"],
    "flank": ["flank"],
}


def main():
    pool = json.load(io.open(POOL, encoding="utf-8"))
    cs = pool["cards"] if isinstance(pool, dict) and "cards" in pool else pool
    byname = {}
    for c in cs:
        byname.setdefault(c.get("name") or "", []).append(c)

    miss = []
    total = 0
    for kw, names in GROUPS.items():
        print("=== %s ===" % kw)
        for nm in names:
            total += 1
            hits = byname.get(nm) or []
            if not hits:
                print("  ?? 卡池里没有 `%s`" % nm)
                miss.append((kw, nm, "不在池"))
                continue
            for c in hits:
                kws = c.get("keywords") or []
                blob = " | ".join(str(x) for x in kws).lower()
                ok = any(a in blob for a in ALIAS[kw])
                mark = "✅" if ok else "🔴"
                print("  %s %-28s id=%-34s keywords=%s" % (mark, nm, c.get("id"), kws))
                if not ok:
                    miss.append((kw, nm, c.get("id")))
    print()
    print("共核 %d 张 · `keywords` 兜不住的 %d 张" % (total, len(miss)))
    for m in miss:
        print("   🔴", m)


if __name__ == "__main__":
    main()
