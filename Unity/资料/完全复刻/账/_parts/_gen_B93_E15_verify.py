# -*- coding: utf-8 -*-
"""B93_E15: verify A-tier landing points (file exists / line in range / method name within +-4)."""
import io, os, re, sys, json

ASSETS = "d:/4/Unity/MyGame/Assets"
OUT = "d:/4/_tmp_view/e15"
os.makedirs(OUT, exist_ok=True)

def rd(p):
    b = io.open(p, 'rb').read()
    if b[:3] == b'\xef\xbb\xbf':
        b = b[3:]
    return b.decode('utf-8', 'replace').replace('\r\n', '\n').split('\n')

INDEX = {}
for root, dirs, files in os.walk(ASSETS):
    for fn in files:
        if fn.endswith('.cs'):
            INDEX.setdefault(fn, []).append(os.path.join(root, fn).replace('\\', '/'))

def resolve(relpath):
    fn = relpath.split('/')[-1]
    cands = INDEX.get(fn, [])
    if not cands:
        return None, []
    # prefer suffix match on the given relative path
    want = relpath.replace('\\', '/').lower()
    best = [c for c in cands if c.lower().endswith('/' + want)]
    return (best[0] if best else cands[0]), cands

LANDS = {
 '战斗逻辑驱动:897': ('RuleCore.cs', [2616, 9454, 12452], 'SetHealthToZero', 'u.Health = 0'),
 '日常_奖励:157': ('Shell/EventBadge.cs', [122], 'SetNotificationData', None),
 '日常_奖励:158': ('Shell/EventBadge.cs', [157], 'ShowNotification', None),
 '日常_奖励:159': ('Shell/EventBadgeController.cs', [39], 'Awake', None),
 '日常_奖励:655': ('Shell/RewardCollected.cs', [117], 'OnTrigger', None),
 '未分类_其他:7': ('Core/AchievementTally.cs', [231], 'AddSabotage', None),
 '未分类_其他:165': ('Shell/ProfileData.cs', [682], 'CollectSpiritStone', None),
 '未分类_其他:561': ('Battle/BattleDriver.cs', [17915], 'GetTextReference', 'TutorialData/'),
 '未分类_其他:688': ('Battle/ErrorMessageBanner.cs', [66], 'UIMessageController', None),
 '未分类_其他:702': ('Shell/UIScaleToFit.cs', [26], 'CalculateLayoutInputHorizontal', None),
}
# 未分类_其他:165 also claims BattleDriver._waystonesCollected

rep = []
for key, (rel, lines, meth, extra) in LANDS.items():
    p, cands = resolve(rel)
    rep.append("=" * 70)
    rep.append(f"{key}  target={rel}  resolved={p}  cands={len(cands)}")
    if not p:
        rep.append("  !! FILE NOT FOUND")
        continue
    L = rd(p)
    rep.append(f"  file lines={len(L)}")
    for lnno in lines:
        rep.append(f"  --- line {lnno} (1-based) ---")
        for i in range(max(0, lnno - 1 - 4), min(len(L), lnno - 1 + 5)):
            mark = '>>' if i == lnno - 1 else '  '
            rep.append(f"  {mark}{i+1:6d}: {L[i][:150]}")
    # name presence within +-4 of each line
    for lnno in lines:
        win = '\n'.join(L[max(0, lnno - 1 - 4):lnno + 4])
        ok = (meth in win) if meth else True
        okx = (extra in win) if extra else True
        rep.append(f"  CHK line {lnno}: method '{meth}' in +-4 => {ok}; extra '{extra}' => {okx}")

io.open(OUT + "/verify_A.txt", 'w', encoding='utf-8', newline='\n').write('\n'.join(rep))
print('\n'.join(rep))
