# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 把 §29·d 里四条【判据其实已在本地】的「等远端 I2」行并进 A1764。
判据（现核）：官方本地化 terms = 9,441 条 / en+zh 两列都有值；
  Battle/Cemetery/* 20 键（Action* 19）· Battle/Tips/* 38 · Battle/Effect/* 28 · Battle/HUD/* 16。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_i2_merge.py
"""
import io

P = r'D:\4\项目任务.md'

MARK = [
    ('`A985⑥②`', '等 `Battle/Cemetery/*` 那 19 条的文案值（远端 I2）'),
    ('`A985⑧` 的**词条 value**', '`RuleCodes.TermKey` 那四个 `Battle/Tips/*` 键的 value 本地一个都没有'),
    ('`Battle/Effect/*` 剩余 4 条 + `Battle/Tips/*` 剩余', '`Battle/Effect/*` 余 4 条 + `Battle/Tips/*` 余量等远端 I2'),
    ('`BattleDriver` 那 12 条中文日志模板', '那 12 条中文日志模板等远端 I2'),
]

TMPL = (
    "| ✅ ~~{id}~~ | ✅ **2026-10-14（第三十会话）现核：判据【已在本地】⇒ 本条不再是「等远端」** —— "
    "`官方本地化_en+zh-CN.json` 的 `terms` 现读 **9,441 条**、en / zh-CN **两列都有值**，本条要的那一族**逐条都在**"
    "（现核：`Battle/Cemetery/*` **20 键**（含 `Action*` **19 条**）· `Battle/Tips/*` **38 键** · "
    "`Battle/Effect/*` **28 键** · `Battle/HUD/*` **16 键**）。"
    "⇒ **已并入 `A1764`**（`资料/任务_卡组卡牌战斗.md:391` —— 那一行现读就写着 `Battle/Tips/*` 38 · `Effect/*` 28 · `Cemetery` 20 · `HUD` 16，与本条吻合；"
    "铁律 6：清单只留一处）⇒ **本表不再列它**。⚠️ **原文留痕**：本条原来写「{orig}」 |"
)


def main():
    b = io.open(P, 'rb').read()
    assert b.count(b'\r\n') == 0
    t = b.decode('utf-8')
    lines = t.split('\n')
    for marker, orig in MARK:
        hits = [i for i, l in enumerate(lines) if l.startswith('| ') and marker in l]
        assert len(hits) == 1, (marker, len(hits))
        i = hits[0]
        assert lines[i].rstrip().endswith(' |'), lines[i][:60]
        lines[i] = TMPL.format(id=marker, orig=orig)
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(P, 'wb').write(out.encode('utf-8'))
    print('OK bytes %d -> %d' % (len(b), len(out.encode('utf-8'))))


if __name__ == '__main__':
    main()
