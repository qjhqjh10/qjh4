#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""功能点索引：把 20 份「类×方法」账（`资料/完全复刻/账/*.tsv`，9,614 行）归并成
**玩家能做的一件事**（功能点）。

⛔ 数据来源只有账；不凭空想功能。
归并方法（可复核）：
  1. **先按方法名**匹配（只对少数巨型类：`BattleManager` / `CardScript` / `EntityScript` /
     `PlayerDataManager` / `WindowsManager` …）—— 这些类一个类就几百个方法，按类归并会把
     「出牌」「攻击」「死亡」全揉成一坨；规则里每一条都是**方法名正则**，写在本文件 `METHOD_RULES`。
  2. **再按原版类**匹配 —— `CLASS_RULES` 里一条 = 一个类名正则 + 功能点。
  3. 两条都没命中的行 → 落到「（域）其它」，并在报告里**点名列出**，⛔ 不静默丢。
  4. 状态 = 该功能点下所有方法的 `状态` 列分布 + **最高档**（A>A 已对齐 > B > C > D > E）。

Run: python -I -X utf8 gen_feature_index.py
Out: D:/4/Unity/资料/完全复刻/功能点索引.md
"""
import io, glob, os, json, re, collections

LEDGER_DIR = r'D:/4/Unity/资料/完全复刻/账'
DEST = r'D:/4/Unity/资料/完全复刻/功能点索引.md'

# 暂停线（用户已暂停）：活动/运营/任务/收件箱/奖励/抽卡
PAUSED_DOMAINS = {'日常/奖励'}
PAUSED_RE = re.compile(r'LiveOps|Daily|Mission|Inbox|Reward|Gacha|ExpansionPass|Raid|Skirmish|FastMode|PlayEvent|Challenge', re.I)

# ---------------------------------------------------------------- 方法名规则
# (功能点, 主链/边角, 域(空=任意), 类名正则, 方法名正则)
METHOD_RULES = [
    ('开局换牌', '主链', '战斗逻辑驱动', r'^(BattleManager|PlayerHand|MatchData)$', r'Mulligan|Redraw|SwapCard'),
    ('开局换牌界面', '主链', '战斗界面/HUD', r'^(MulliganManager)$', None),
    ('出牌与目标选择', '主链', '战斗逻辑驱动',
     r'^(BattleManager|CardScript|PlayerHand|MatchData|SupportMethods)$',
     r'CanPlay|PlayCard|PlayAbility|ChooseTarget|SelectTarget|Target|Choice|Choose|Pick'),
    ('攻击与伤害结算', '主链', '战斗逻辑驱动',
     r'^(BattleManager|CardScript|EntityScript|SupportMethods)$',
     r'Attack|Damage|Strike|Counter|Combat|Hit|Deal|Heal'),
    ('关键词与词条（含光环 / buff）', '主链', '战斗逻辑驱动',
     r'^(BattleManager|CardScript|EntityScript|CardTraitCollection|AbilityLogic|ActiveWarlordAbility|AbilityData|ActiveAbility|CustomAbilityLogic|TriggerAbility)$',
     r'Trait|Keyword|Ability|Aura|Buff|Status|Effect|Enchant|Stun|Combo|Armour|Skull|SpiritStone'),
    ('召唤 / 造牌 / 偷牌 / 变身', '主链', '战斗逻辑驱动',
     r'^(BattleManager|CardScript|MinionManager|SupportMethods|BattleManagerSupport)$',
     r'Summon|Create|Spawn|Steal|Transform|AddCard|AddMinion|Draw'),
    ('死亡 / 残骸 / 墓地', '主链', '战斗逻辑驱动',
     r'^(BattleManager|CardScript|EntityScript|MinionManager|RemnantBody)$',
     r'Death|Die|Destroy|Remnant|Cemetery|Kill|Bury'),
    ('回合流转与能量', '主链', '战斗逻辑驱动',
     r'^(BattleManager|CardScript|MatchData|EnergyContext|TurnExtensions|TurnScriptedData)$',
     r'Turn|Mana|Energy|Resource|StartOf|EndOf|Begin'),
    ('事件触发链与广播', '边角', '战斗逻辑驱动',
     r'^(BattleManager|CardScript|EntityScript|SupportMethods|BattleManagerSupport)$',
     r'Trigger|Broadcast|React|Resolve|Notify'),
    ('胜负判定与结算', '主链', '战斗逻辑驱动',
     r'^(BattleManager|MatchData|EntityScript|CardScript)$',
     r'Win|Lose|Concede|Surrender|Match|Result|GameOver|EndBattle'),
    ('棋盘与站位', '主链', '战斗逻辑驱动',
     r'^(BattleManager|MinionManager|SupportMethods|BoardAnalysis|BoardSlots)$',
     r'Board|Slot|Position|Move|Swap|Shift|Flank|Adjacent'),
    ('随机与种子', '边角', '战斗逻辑驱动', r'^(RandomSeedProvider)$', None),
    ('对手 AI', '边角', '战斗逻辑驱动', r'^(AI|AiScripted|AIBotsConfig|EnemyHeroDamageScorer|DamageCriteria|SummonCriteria|TargetCriteria|DamageInfo|DamageDeltToEnemyUnit|TroopsKilledCount|CircularBuffer)$', None),
    ('战斗统计与成就口径', '边角', '战斗逻辑驱动', r'^(CardDamageCounterController|MinionScaleByPositionSO|WarlordMasteryContainer|WarlordPathData)$', None),
    # —— 🆕 2026-10-25（第二轮）：把 BattleManager 剩下那 224 个 + 同族兜底切细（**顺序=优先级**，
    #    排在前面那 12 条之后 ⇒ 出牌/攻击/关键词等仍先命中，不会被这里抢走）
    ('战斗动作队列与结算流水', '主链', '战斗逻辑驱动',
     r'^(BattleManager|BattleManagerSupport|SupportMethods)$',
     r'Actions?InQueue|Add(?:Auto|Next|Follow|Reset)?\w*Action|ExecuteNextAction|FinishResolvingAction|CommunicateBattleAction|CancelBattleAction|CancelActionStates'),
    ('手牌与牌库流转', '主链', '战斗逻辑驱动',
     r'^(BattleManager|PlayerHand)$',
     r'Add\w*(?:FromHand|ToDeck|ToHand|ToTop)|RecallTo|Discard|Disappear|NewCardToDeck|InitialCardToDeck|GoesSecondCard|AddDrawnCard|AddCardNotDrawn|AddActiveTrap|AddHandEffect|MoveCardsInHand|PositionCardInHand|HideHand|DisplayHand'),
    ('战斗结束 / 投降 / 断线重连', '主链', '战斗逻辑驱动',
     r'^(BattleManager)$',
     r'BattleFinished|BattleEnd|Resign|Forfeit|Disconnect|Reconnect|LeavingBattle|ClosePopupsAfterBattleEnd|CompleteBattle'),
    ('战斗内界面交互（点击/显示/调试）', '边角', '战斗逻辑驱动',
     r'^(BattleManager)$',
     r'^Click|^CanDisplay|^Display|^Debug|Pause|^Skip|Chat'),
    ('卡牌状态切换与动画', '边角', '战斗逻辑驱动',
     r'^(CardScript)$',
     r'Activate|Dissolve|Anim|DoBody|DoPushBack|CardTo|CardReturned|ChangeOwner|ApplyMovement|CompleteReassemble|^Display'),
    ('卡牌静态数据与本地化取值', '边角', '战斗逻辑驱动',
     r'^(RawCardScript)$',
     r'.*'),
    ('通用工具（时间/字符串/校验/随机）', '边角', '战斗逻辑驱动',
     r'^(SupportMethods)$',
     r'.*'),
    ('对局配置与 Bot 随机', '边角', '战斗逻辑驱动',
     r'^(MatchData)$',
     r'.*'),
    ('棋盘随从增删与重整', '主链', '战斗逻辑驱动',
     r'^(MinionManager)$',
     r'.*'),
    ('卡牌状态查询与判定', '边角', '战斗逻辑驱动',
     r'^(CardScript|EntityScript|PlayerHand)$',
     r'^(Can|Check|Is|Has|Should|Already|Count|Find|Are)'),
    ('战斗内声音与回放', '边角', '战斗逻辑驱动',
     r'^(BattleManager)$',
     r'^(Play|NextReplay|ClickPlay|ClickPause|ClickRestart|Parse|Receive|DelayedBody)'),
    ('战斗初始化与战役衔接', '边角', '战斗逻辑驱动',
     r'^(BattleManager)$',
     r'^(Initialize|Land|Hide|Gc|Device|Max|Mobile|Jam|Pay|Leave|Auto|ActionsLeft|InBattle|FinishWaiting|CheckFor|AddExchange|AddExecute|AddFollow|AddReassemble|AddReset|AddReveal|AddStun|AddVoice|ExecuteUse|ShowStatus)'),
    ('战斗查询与辅助', '边角', '战斗逻辑驱动',
     r'^(BattleManager|BattleManagerSupport|EntityScript|PlayerHand)$',
     r'^(Get|All|Is|Can|Has|Set|Update|On|Awake|Start|Shoult|Should|Num)'),
    ('战斗界面 · 场上卡与状态框', '主链', '战斗界面/HUD', r'^(BattleCardUI|BasicCardUI|ActiveSkillDisplay|FlyingCardController|CardHighlight)$', None),
    ('战斗界面 · 手牌/牌库/坟场/能量/计时 HUD', '主链', '战斗界面/HUD', r'^(BattleHud|ManaManager|ManaTypeHolder|ClockManager|CemeteryManager)$', None),
    ('战斗界面 · 目标标线/选卡菜单/攻击钮', '主链', '战斗界面/HUD', r'^(TargetReticleController|ChooseCardMenu|CardDisplayAttackTypeButton|CardDisplayWindow)$', None),
    ('战斗相机与运镜', '边角', '战斗界面/HUD', r'^(CombatCameraZoom)$', None),
    ('战斗设置窗', '边角', '战斗界面/HUD', r'^(BattleSettingsWindow)$', None),
    ('练习模式入口窗', '边角', '战斗界面/HUD', r'^(PracticeModePopup)$', None),
    ('战斗场景加载与开场', '主链', '战斗界面/HUD', r'^(EverguildSceneManager|LoginScreenManager|TestLoginManager|TipPrebattle|ShowStatusWhenConnecting)$', None),
    ('客服/帮助上报', '边角', '战斗界面/HUD', r'^(HelpshiftManager)$', None),
]

# ---------------------------------------------------------------- 类名规则
# (功能点, 主链/边角, 域(空=任意), 类名正则)
CLASS_RULES = [
    # —— 卡组编辑
    ('卡组编辑窗（四页签）', '主链', '卡组编辑', r'^(DeckEditingWindow|DeckEditingPanel|DeckGeneralInfoDemo|DeckListDrawer|DeckSelectionTabController|SelectDecksTab|DeckSelectionPopup|CollectionDeckTab|DeckCollectionTab|DrafModeDeckInfoCardListPanel|DeckEditorCollectionDisplay)$'),
    ('卡组数据结构与规则校验', '主链', '卡组编辑', r'^(CardDeck|DeckUtility|DeckAndWarlordData|DeckManager|DeckInventory|DeckSelectorMenuItemDemo|PrebuiltDeck|DemoDeckInfoSO)$'),
    ('卡组详情弹窗', '边角', '卡组编辑', r'^(DeckInfoPopup)$'),
    ('导入/预组卡组', '边角', '卡组编辑', r'^(ImportDeckPopup|PrebuiltPackUI)$'),
    # —— 收藏
    ('收藏 · 卡池与筛选', '主链', '收藏', r'^(CollectionDisplay|CollectionDisplay<|CardCollectionDisplay|CardCollectionTab|CardListItemScript|CardNameFilter|ToggleCollectionFilter|FilterMethods|CollectionCard|GenericSelectCardContainer|RaycastFilter|CollectionItem|CollectionItem<)$'),
    ('收藏 · 物品与库存', '主链', '收藏', r'^(InventoryManager|Inventory|Inventory<|IInventory<|ItemDrawer|ItemDrawer<|UiBadgeNotification)$'),
    ('收藏 · 异画页签', '边角', '收藏', r'^(AlternateArtCardCollectionTab)$'),
    # —— 商店
    ('商店 · 货架与购买', '主链', '商店', r'^(Shops|ShopStore|ShopStore<|ShopWindow|ShopTab|ShopItemContainer|ShopOfferContainer|PurchasesManager|CurrencyUI|PriceDisplay|VIPCardShopTab|BoostManager)$'),
    ('商店 · 特惠弹窗', '边角', '商店', r'^(BaseOfferPopup|PurchasePremiumWindow|ShopOfferEvent|CampaignPremiumPointsPanel|ExpansionPassPurchasePremiumPanel)$'),
    ('商店 · 内购通道（Steam / 代理 / 广告）', '边角', '商店', r'^(InAppPurchaseProxy|InAppPurchaseSteam|AdsHandler)$'),
    ('抽卡箱（Gacha）', '暂停', '商店', r'^(GachaCratePanel|GachaEventTab)$'),
    # —— 锻造
    ('锻造 · 开包', '主链', '锻造', r'^(BoosterPackOpenWindow|BoosterPackVisualItem|BoosterInfoPopup|BoosterShopTab|CardInBoosterPack|BoosterPackBackground)$'),
    ('锻造 · 合成与等级', '主链', '锻造', r'^(ForgeWindowTab|CraftingPanel|Forge|ForgeHandler|ForgeRewardSelector|ForgeRewardItemContainer|ForgeRewardCurrentLevelPoints|ForgeSelectedArmyInfo|ForgeItemData|ForgePlayerContainer|UpgradeForgeLevel|ForgePointDrawer|ForgePointIconDrawer)$'),
    # —— 战役
    ('战役 · 关卡图与推进', '主链', '战役', r'^(Campaign|CampaignHandler|CampaignTrack|CampaignNode|CampaignNodeData|CampaignNodeTrigger|CampaignPoints|CampaignPointsTrigger|CampaignHeaderDisplay|CampaignUIBackground|CampaignArmyUIDecorator|CampaignPointDrawer|TimedCampaignMainData|ScriptedActionCampaignData|CampaignEventData|CompleteCampaign)$'),
    ('战役 · 奖励窗', '主链', '战役', r'^(CampaignRewardsWindow|CampaignWindowTab)$'),
    ('战役 · 选阵营/选卡组', '主链', '战役', r'^(ArmySelector|RankedDeckSelector|BattleArenaByArmySO)$'),
    # —— 轮抽（暂停）
    ('轮抽模式', '暂停', '轮抽', r'.*'),
    # —— 教程
    ('教程/引导（含教学战）', '主链', '教程/引导', r'.*'),
    # —— 社交
    ('联盟（公会）', '边角', '社交/聊天', r'^(AlliancesManager|AlliancesController|AllianceMemberOptionsPopup|AllianceView|AllianceTrophyController|JoinAllianceMenu|AlliancesTrophyModel)$'),
    ('聊天（世界/联盟/好友）', '边角', '社交/聊天', r'^(ChatGlobalManager|ChatPanel|ChatContentView|ChatMessageUI|ChatPlayerOptionsPanel|ChatManager)$'),
    ('好友与收件箱', '边角', '社交/聊天', r'^(FriendsTab|BaseInboxEvent|PlayerProfileWindow)$'),
    # —— 个人资料
    ('个人资料与头像', '边角', '个人资料/成就/排行', r'^(PlayerProfileMenu|PlayerProfileUIController|AccountTab|AvatarTab|AvatarDisplay|PlayerAvatarDataManager)$'),
    ('成就', '边角', '个人资料/成就/排行', r'^(AchievementsManager|Achievement)$'),
    ('排行榜 / 段位', '边角', '个人资料/成就/排行', r'^(UIRankingListController|LeaderboardManager|RankedV3Event|RankedV2Event|RankedEventWindowV2|RankedEventDivisionChangeWindowNewSystem)$'),
    # —— 菜单外壳
    ('窗口外壳与导航', '主链', '菜单/外壳', r'^(WindowsManager|GameWindow|GameWindowWithTabs|MainMenuWindow|NavigationPanelController|MenuActions|EverguildSceneManager)$'),
    ('通用控件（按钮/开关/列表/拖拽/提示）', '边角', '菜单/外壳', r'^(EverguildButton|EverguildButtonStateFollower|EverguildToggle|EverguildTooltipTrigger|DynamicList|DynamicList<|FlexibleGridLayout|DragAndDropItem|ScreenHighlightPosition|NestedShadowGroupBase|GameSunsetPopup)$'),
    ('设置窗（画面/音频/语言/联机）', '主链', '菜单/外壳', r'^(GraphicsTab|SettingsWindow|SettingsPanel|LanguageSelector)$'),
    ('起名 / 账号登录窗', '边角', '菜单/外壳', r'^(ChooseNamePopup|BasicLoginWithEmailWindow|LoginManager)$'),
    ('异画展示面板', '边角', '菜单/外壳', r'^(AlternateArtPanel)$'),
    ('升级 / 奖励领取面板', '边角', '菜单/外壳', r'^(UpgradePanel)$'),
    # —— 联机
    ('联机 · 传输与房间', '主链', '联机', r'^(NetworkCustomManager|BattleNetworkManager|BattleCommsManager|Room|RoomInfo|CellTreeNode|TypedLobby|TypedLobbyInfo|AuthenticationValues|OnlineData)$'),
    ('联机 · 匹配与找对手', '主链', '联机', r'^(SearchOpponentManager|MatchMakerManager)$'),
    # —— 特效动画
    ('相机震动', '边角', '特效动画', r'^(CameraShakerManager|CameraShake|OverwriteCameraShakePreset)$'),
    ('后处理 / LUT / 调色', '边角', '特效动画', r'^(AnimFXModulePostProcess|LUTBlender|AnimateUIMaterialProperty|CamerasConversionHelper)$'),
    ('UI 特效（描边/淡入/位移）', '边角', '特效动画', r'^(UIBorderGlow|NestedFadeGroupBase|UITransformTweenController|UIBorderGlow)$'),
    ('粒子区域与卡牌 3D 动画', '边角', '特效动画', r'^(ParticleSystemAreaSpawner|ParticleSystemAreaSpawnerController|Card3DAnimationController|SegmentationController)$'),
    # —— 卡牌面
    ('卡面渲染（图文/数值/图标/高亮）', '主链', '卡牌面/渲染', r'^(Card2DController|CardTextsController|CardTextCountersController|CardImageController|CardHighlight|CardFramesSO|CardServices|UICardInfoItem|CardCollider2D)$'),
    ('卡面 tooltip 与词条说明', '边角', '卡牌面/渲染', r'^(TextTooltipController|TraitDescriptionUI|TraitDefinitionDisplay|BoardTraitIcon)$'),
    ('环境效果卡（战场环境切换）', '边角', '卡牌面/渲染', r'^(ScenarioEnvironmentConditionSO)$'),
    # —— 音频
    ('语音（单位台词）', '边角', '音频/语音', r'^(VoiceLinesController|VoiceLinesPopupSelector|VoiceLineBox|UnitsVoiceLinesPanel)$'),
    ('音乐与音效播放', '边角', '音频/语音', r'^(AudioSourcesController|BattleMusicManager|MusicManager|SoundManager|AudioCue|FadingAudioSource|MusicBlendTest|AudioSourceTogglerByVolume|AudioMixerAddresableReferences|AudioWaveEqualizer)$'),
    # —— 本地化
    ('本地化与语言切换', '边角', '本地化', r'.*'),
    # —— 基础设施
    ('存档与玩家数据', '主链', '其他/基础设施', r'^(PlayerDataManager|PlayerManager|InventoryManager)$'),
    ('登录 / 账号 / 平台（Steam / PlayFab / CloudScript）', '边角', '其他/基础设施', r'^(LoginManager|PlayfabWrapper|PlayfabTester|SteamManager|CloudscriptHandler|Authentication|FeatureConfig)$'),
    ('资产加载（Addressables / AssetBundle / LoadableReference）', '边角', '其他/基础设施', r'^(AssetBundleManager|AddressablesManager|LoadableReference|BaseAddressableLibrary|ContainerService|CatalogConfig)$'),
    ('静态数据与配置表', '边角', '其他/基础设施', r'^(GameStaticData|CardsSpecialConfig)$'),
    ('调试工具（选卡器/DebugUI）', '边角', '其他/基础设施', r'^(DebugCardPickerGrid|DebugUIHandler.*)$'),
    ('反射/序列化等工具件', '边角', '其他/基础设施', r'^(ReflectionHelper|BiDirectionalDictionary|ListExtensions|ReflectionExtensions|ComponentReference|UiContainer|ActionScheduler|TapHitMaterialDefinition|UIMultiCardDisplay|BlockingOverlay|UIImageGreyscaleController)$'),
    # —— 🆕 2026-10-25（第二轮）：按「兜底比例高的域」补的类名规则 ——
    #    判据 = 逐域算 catch-all 命中率（菜单/外壳 65% · 特效动画 64% · 个人资料 49% ·
    #    社交 42% · 商店 38% · 收藏 34% · 战斗界面 29%），把各域 top 类名按族归并。
    ('窗口基类与页签', '主链', '菜单/外壳', r'^(WindowTabBase|GeneralMenuController|MediaTab|ChangeNameWindow|DuelPopupWindow|GameWindow|MainMenuWindow)$'),
    ('实用 UI 组件（安全区/缩放/染色/输入）', '边角', '菜单/外壳', r'^(UIAnchorAvoidSafeArea|UIScaleToFit|UIGroupTint|UIImageMaterialColorChanger|EverguildInput|GrabScreenFeature|GrabScreenFeatureU6|EverguildTooltipTrigger)$'),
    ('提示与通知（Toast）', '边角', '菜单/外壳', r'^(ToastNotificationController)$'),
    ('战斗模式入口按钮', '主链', '菜单/外壳', r'^(GameModeButtonDemoVersion|AttackTypesButtonsController|SearchingMatchWindowDemo|ModeButton.*)$'),
    ('运营菜单容器', '暂停', '菜单/外壳', r'^(LiveopMenuContainer|LiveOpsMenu.*)$'),
    ('意见反馈入口', '边角', '菜单/外壳', r'^(GiveFeedbackMenuDemo)$'),
    ('材质淡入 / 混合 / 闪烁', '边角', '特效动画', r'^(MaterialFader|MaterialFlickerEffect|ScenarioGenericMaterialBlend|ScenarioMaterialFader|AnimFXModuleChangeMaterial|PostFXController|PostFX.*)$'),
    ('物体动画 / 浮动 / 光照闪烁', '边角', '特效动画', r'^(ObjAnimEffects|BouyancyEffect|LightFlickerEffect|SpriteFlickerEffect|UIFlippableAndRotableUVs)$'),
    ('动画框架与模块（AnimFX）', '边角', '特效动画', r'^(AnimFXController|AnimFXModule.*)$'),
    ('相机取景与跟随', '边角', '特效动画', r'^(CameraVerticalFramer|CameraFramer.*)$'),
    ('卡牌/词条特效控制器', '边角', '特效动画', r'^(CardDisplayLevelUpAnimationController|MarkerlightController|Card.*AnimationController)$'),
    ('排位赛窗与赛季', '边角', '个人资料/成就/排行', r'^(Ranked.*|DeckGeneralInfoRanked|ArmySelectorRanked|ChangeRankedModeToggle|PlayerUIRankingRow|LeaderboardEntry)$'),
    ('个人资料页', '边角', '个人资料/成就/排行', r'^(Profile.*|PlayerUI.*)$'),
    ('成就页', '边角', '个人资料/成就/排行', r'^(AchievementsMenu|Achievement.*)$'),
    ('收件箱与私信', '暂停', '社交/聊天', r'^(Inbox|InboxWindow|MessageContainer|MessageDisplay|PrivateMessageData|BaseInboxEvent.*)$'),
    ('聊天频道与预览', '边角', '社交/聊天', r'^(Chat.*|TestReporter)$'),
    ('联盟列表与徽章', '边角', '社交/聊天', r'^(Alliance.*|Alliances.*)$'),
    ('好友列表', '边角', '社交/聊天', r'^(Friend.*|FriendsTab)$'),
    ('内购与网页商店', '边角', '商店', r'^(InApp.*|PuchasePremium.*|PurchaseableOfferContainer|WebShopOpenButton|CurrencyValue|CurrencyExtensions)$'),
    ('特惠弹窗与轮播', '边角', '商店', r'^(.*OfferPopup.*|.*OfferEvent.*|Carousel.*|GeneralOffer.*|ContainerOfferEvent)$'),
    ('商店货架容器', '主链', '商店', r'^(ShopContainer|Shop.*|PriceDisplay.*|Boost.*)$'),
    ('抽卡奖励与事件（Gacha）', '暂停', '商店', r'^(Gacha.*)$'),
    ('收藏筛选器', '主链', '收藏', r'^(.*Filter.*|WhereToGetCardsSection)$'),
    ('收藏页与页签', '主链', '收藏', r'^(CollectionScreen|CollectionTab.*|CollectionManager|CardsCollection|CardInventory|ArmyItemContainer|WildcardDisplay)$'),
    ('物品徽章与通知', '边角', '收藏', r'^(UiBadgeNotification.*|GameModesIconCollection)$'),
    ('战斗日志与坟场日志', '边角', '战斗界面/HUD', r'^(BattleLog.*|CemeteryLog.*|RadioMessageScript)$'),
    ('战斗回放 HUD 与结算界面', '边角', '战斗界面/HUD', r'^(ReplayHud|EndBattleDoors|BattleScore.*|BattleErrorUIManager)$'),
    ('战斗相机与自动缩放', '边角', '战斗界面/HUD', r'^(BattleCamera.*|CombatAutoZoom|BattleCameraSreenSize)$'),
    ('战斗内提示与悬浮', '边角', '战斗界面/HUD', r'^(BattleTipController|HoverManager|EverguildTooltipManager)$'),
    ('战斗环境条件管理', '边角', '战斗界面/HUD', r'^(ScenarioEnvironmentConditionsManager|ScenarioEnvironment.*)$'),
    ('提示/评分/搜索等弹窗', '边角', '菜单/外壳', r'^(PromptPopup|RatePopup|SearchingOpponentWindow|Shade|TitleTab|GeneralTab|TabButtons)$'),
    ('滚动、拖拽与富文本工具件', '边角', '菜单/外壳', r'^(ScrollViewFocusFunctions|Draggable|DragButtonInfoInt|NestedShadowGroup|CardDrawer|EverguildTextMeshPro|EverguildTooltipItem)$'),
    ('淡入组与精灵/粒子工具', '边角', '特效动画', r'^(NestedFadeGroup.*|SpriteFaderController|UIEffectBorderGlowToggler|ParticleSystemPoolable|TapParticleController|CrosshairLineEffect|CameraShakePreset)$'),
    ('卡牌效果组件与特效模块', '边角', '特效动画', r'^(CardEffect|CardEffectItem|AnimFxModule.*|EnviromentalEffectCardsSO|ScenarioParticleSpawnerBlender)$'),
    # —— 兜底（放最后，保证不丢行）
    ('编译器生成类型（无玩家语义）', '边角', '', r'^<>', 1),
    ('战斗引擎 · 其它（查询/缓存/辅助）', '边角', '战斗逻辑驱动', r'.*', 1),
    ('活动线 · 事件框架（LiveOps）', '暂停', '日常/奖励', r'^LiveOps'),
    ('活动线 · 任务与挑战', '暂停', '日常/奖励', r'^(Mission|Challenge)'),
    ('活动线 · 突袭与通行证', '暂停', '日常/奖励', r'^(Raid|ExpansionPass)'),
    ('排位赛与遭遇战模式（**未暂停**）', '主链', '日常/奖励', r'^(Ranked|Skirmish)'),
    ('活动线 · 每日/连胜/能量', '暂停', '日常/奖励', r'^(Daily|FastMode|SinglePlayerOnlyEnergy|PlayEvent|DebugEventToggler)'),
    ('活动线 · 其它（用户已暂停）', '暂停', '日常/奖励', r'.*', 1),
    ('场景初始化与启动链', '边角', '其他/基础设施', r'^(SceneInitializer|SceneChanger|GameBootController|EverguildSceneManager)$'),
    ('基础设施 · 其它', '边角', '其他/基础设施', r'.*', 1),
    ('未分类（通用工具件）', '边角', '未分类/其他', r'.*', 1),
    ('特效动画 · 其它（特效框架）', '边角', '特效动画', r'.*', 1),
    ('战斗界面/HUD · 其它', '边角', '战斗界面/HUD', r'.*', 1),
    ('社交 · 其它（联盟徽章/资产库）', '边角', '社交/聊天', r'.*', 1),
    ('个人资料 · 其它', '边角', '个人资料/成就/排行', r'.*', 1),
    ('收藏 · 其它', '边角', '收藏', r'.*', 1),
    ('商店 · 其它', '边角', '商店', r'.*', 1),
    ('卡组编辑 · 其它', '边角', '卡组编辑', r'.*', 1),
    ('卡牌面 · 其它', '边角', '卡牌面/渲染', r'.*', 1),
    ('音频 · 其它', '边角', '音频/语音', r'.*', 1),
    ('战役 · 其它', '边角', '战役', r'.*', 1),
    ('菜单/外壳 · 其它（控件/面板）', '边角', '菜单/外壳', r'.*', 1),
]

# 暂停线：恢复时**先做什么**（来源 = 既有结论 + 账里的类名关系；⛔ 不是新查的）
RESUME = {
    '活动线 · 事件框架（LiveOps）':
        '先建 `LiveOpsHandler` + `LiveOpsEvent<TSave>` 两个基类（原版签名里带 `blockedEvents` / `shared`），'
        '再把 `LiveOpsManager` 的注册表接上 —— 这是**所有活动的底座**，其它 5 条都依赖它',
    '活动线 · 每日/连胜/能量':
        '先做**存档归并**（`PlayerDataManager` 那套中央存档 + 每活动一份 `TSave`）＋ 每日重置口径'
        '（`SupportMethods.DecodeTimePassed` / `GetUtcOriginDate`），再建 `DailyStreakWindow` / 能量刷新',
    '活动线 · 任务与挑战':
        '先有 `MissionEvent` / `MissionChallenge` 的进度记账（依赖活动框架 + 存档），再接 `ChallengeManager` 的领取',
    '活动线 · 排位/遭遇战/突袭事件':
        '先建 `RankedEventWindow` / `SkirmishEventWindow` / `RaidEvent` 的入口窗与分段奖励表（依赖活动框架）',
    '活动线 · 其它（用户已暂停）':
        '先建活动注册表与事件基类（同「事件框架」那条）；这一桶是长尾，恢复时再分类',
    '轮抽模式':
        '先建 `DraftEventBase` 的**状态机**（Pay → SelectPacks → WarlordSelection → Ongoing → Reward），'
        '再建 `DraftRewardPanel`；它是自成一套的吃牌/组牌流程，与普通卡组编辑不共用',
    '活动线 · 突袭与通行证':
        '先建 `RaidEvent` / `ExpansionPassEvent` 的进度与免费/付费两轨（依赖活动框架），'
        '奖励发放走 `CampaignPremiumPointsPanel` / `ExpansionPassPurchasePremiumPanel`',
    '收件箱与私信':
        '先建 `Inbox` 的**消息模型 + 已读/领取状态**（`MessageContainer` / `PrivateMessageData`），'
        '再建 `InboxWindow` 列表；它挂在聊天那套上，不是独立窗',
    '抽卡奖励与事件（Gacha）':
        '先建 `GachaEvent` 的概率表与保底计数（依赖活动框架），再建 `GachaRewardsPanel` 的展示',
    '运营菜单容器':
        '先建活动注册表（同「事件框架」），`LiveopMenuContainer` 只是把这些活动按钮挂进主菜单的那一格',
    '抽卡箱（Gacha）':
        '先建 `GachaCratePanel` 的开箱演出 + 概率表读取（依赖活动框架），奖励发放走 `GachaRewardContainer`',
}

STATE_ORDER = ['A', 'B', 'C', 'D', 'E']
STATE_NAME = {'A': 'A 已对齐', 'B': 'B 已实现未核', 'C': 'C 该做没做', 'D': 'D 不适用', 'E': 'E 未读'}


def match_row(dom, cls, meth):
    # 泛型类名（`CollectionItem<T>` / `LiveOpsHandler<TEvent>`）先去掉 `<>` 再匹配，
    # 否则带 `$` 锚点的规则会漏（实测漏了 21+27+27 行）
    base = cls if cls.startswith('<>') else cls.split('<')[0]   # 编译器生成名（`<>f__AnonymousType…`）不切
    for rule in METHOD_RULES:
        fn, kind, d, cre, mre = (rule + (None,))[:5]
        if d and d != dom:
            continue
        if not re.search(cre, base):
            continue
        if mre is None or re.search(mre, meth):
            return fn, kind, 0
    for rule in CLASS_RULES:
        fn, kind, d, cre, is_catch = (rule + (None, None))[:5]
        if d and d != dom:
            continue
        if re.search(cre, base):
            return fn, kind, int(is_catch or 0)
    return None


def main():
    rows = []
    files = sorted(glob.glob(os.path.join(LEDGER_DIR, '*.tsv')))
    for f in files:
        for line in list(io.open(f, encoding='utf-8'))[1:]:
            p = line.rstrip('\n').split('\t')
            if len(p) < 6:
                continue
            rows.append({'dom': p[0], 'cls': p[1], 'meth': p[2], 'ev': p[3],
                         'state': p[5].strip(), 'ours': p[6] if len(p) > 6 else '',
                         'diff': p[7] if len(p) > 7 else '',
                         'grade': p[8].strip() if len(p) > 8 else '',
                         'file': os.path.basename(f)})
    print('账行', len(rows), 'files', len(files))

    groups = collections.OrderedDict()
    disagreements = []
    unmatched = collections.Counter()
    for r in rows:
        m = match_row(r['dom'], r['cls'], r['meth'])
        if not m:
            unmatched[(r['dom'], r['cls'])] += 1
            key = (f"（{r['dom']}）其它", '边角', r['dom'])
        else:
            if m[2]:
                unmatched[(r['dom'], r['cls'])] += 1
            key = (m[0], m[1], r['dom'])
        g = groups.setdefault(key, {'rows': [], 'classes': collections.Counter()})
        g['rows'].append(r)
        g['classes'][r['cls']] += 1

    L = []
    A = L.append
    A('# 功能点索引（一行 = 玩家能做的一件事）')
    A('')
    A('> **数据来源**：`资料/完全复刻/账/*.tsv`（21 份 · 9,614 行「原版类 × 方法」）。⛔ 没有一条功能点是凭空想的。')
    A('> **归并方法（可复核）**：① 先按**方法名正则**切少数巨型类（`BattleManager`/`CardScript`/`EntityScript`/'
      '`PlayerDataManager`/`WindowsManager` 一个类就几百个方法，按类归并会把「出牌/攻击/死亡」揉成一坨）；'
      '② 再按**原版类名**归并；③ 两条都没命中的落「（域）其它」并在 §3 点名列出。')
    A('> **状态**：取该功能点下所有方法 `状态` 列的分布 + **最高档**（A > B > C > D > E）。'
      '⚠️ 「最高档 = A」只代表**其中至少一个方法**已对齐，**不代表整个功能点做完** —— 看分布列。')
    A('> 🔴 **暂停线**：活动 / 运营 / 任务 / 收件箱 / 奖励 / 抽卡（`LiveOps*` `Daily*` `Mission*` `Inbox*` '
      '`Reward*` `Gacha*`）**用户已暂停**，单独标 `暂停`。')
    A('')
    A('## §1 总表')
    A('')
    A('| # | 功能点 | 主链/边角（我判） | 账推分类（P0⇒主链 / P1P2⇒边角） | 域 | 挂哪些类.方法（前几个） | 方法数 | 状态分布 | 最高档 | 备注 |')
    A('|---|---|---|---|---|---|---|---|---|---|')
    order = {'主链': 0, '边角': 1, '暂停': 2}
    items = sorted(groups.items(), key=lambda kv: (order.get(kv[0][1], 9), -len(kv[1]['rows'])))
    for i, ((fn, kind, dom), g) in enumerate(items, 1):
        rs = g['rows']
        cnt = collections.Counter(r['state'] for r in rs)
        dist = ' · '.join(f'{k}{cnt[k]}' for k in STATE_ORDER if cnt[k])
        best = next((k for k in STATE_ORDER if cnt[k]), '—')
        g0 = collections.Counter(r['grade'] for r in rs if r['state'] == 'C')
        kd = ('主链' if g0.get('P0') else ('边角' if (g0.get('P1') or g0.get('P2')) else '—'))
        derived = (f'**{kd}**（P0×{g0.get("P0",0)} P1×{g0.get("P1",0)} P2×{g0.get("P2",0)}）'
                   if g0 else '—（无 C 行）')
        disagreements.append((fn, kind, dom, kd, dict(g0), len(rs)))
        cls = g['classes'].most_common(4)
        cls_s = '、'.join(f'`{c}`×{n}' for c, n in cls)
        if len(g['classes']) > 4:
            cls_s += f' …（共 {len(g["classes"])} 类）'
        pick = [x for x in rs if not x['cls'].startswith('<')][:2] or rs[:2]
        sample = '、'.join(f'{x["cls"]}.{re.split(r"[(<]", x["meth"])[0]}' for x in pick)
        note = ''
        if kind == '暂停':
            note = '🔴 **用户已暂停** —— **恢复起点**：' + RESUME.get(fn, '（待补）')
        if cnt.get('C'):
            note = (note + ' ' if note else '') + f'有 **{cnt["C"]} 条 C（该做没做）**'
        if fn == '存档与玩家数据':
            note += (' ⚠️ 原版存档系统我们是**整体替换**的，这批 C 是否成立要按方案 §4.1 重判（⛔ 不属本轮）')
        A(f'| {i} | **{fn}** | {kind} | {derived} | {dom} | {cls_s}<br>例：`{sample}` | {len(rs)} | {dist} | **{best}** | {note} |')
    A('')
    A('## §2 怎么读这张表（验收三法怎么挂）')
    A('')
    A('| 列 | 用途 |')
    A('|---|---|')
    A('| 功能点 | 「玩家能做的一件事」。**验收按行跑**，不按类跑 |')
    A('| 主链/边角 | **主链** = 玩家一定会走到（开局·出牌·攻击·目标·结算·收藏/卡组主链·卡面数值）；边角 = 次要/低频/纯观感 |')
    A('| 挂哪些类.方法 | 回到 `账/<域>.tsv` 的原行（按 `原版类` + `方法` 搜），那里有 `判据出处` / `判据要点` / `我们的落点` |')
    A('| 状态分布 | **改成 A 之前先看这一列** —— 有 C 就说明这条功能点还有「该做没做」的洞 |')
    A('')
    A('**三步走**：① **判据签收** = 该功能点下每一条 C/E 都要有 `判据出处`；② **oracle 对照** = 原版离线可跑，'
      '拿这条功能点的行为去比；③ **成品并排比** = 截图/录屏与 PvP 卡面/界面并排。')
    A('')
    A('## §3 没归并进去的行（⛔ 不静默丢）')
    A('')
    if unmatched:
        tot = sum(unmatched.values())
        A(f'共 **{tot}** 行落到**兜底规则**（= 归并粒度到不了单个类，但**没有被丢掉** —— 它们都在 §1 的「（域）其它 / 其它（…）」行里）。'
          '逐类列出（类名 × 行数）；要给某个类单独开功能点，就补 `工具/gen_feature_index.py` 的 `CLASS_RULES`：')
        A('')
        A('| 域 | 类 | 行数 |')
        A('|---|---|---|')
        for (dom, cls), n in unmatched.most_common():
            A(f'| {dom} | `{cls}` | {n} |')
    else:
        A('（无：所有行都命中规则）')
    A('')
    A('## §4 「主链/边角」两版对照（⛔ 这列原先没有一手判据，本轮补了一版**由账推导**的）')
    A('')
    A('**两版的口径**：')
    A('- **我判版**：按方案 §4.2 的 P0/P1 定义人工判断（玩家主链上能看见/能触发 = 主链）。')
    A('- **账推版**：取该功能点下 **`状态 = C` 的那些行的 `等级` 列**（P0 ⇒ 主链 / P1·P2 ⇒ 边角）。'
      '没有 C 行的功能点记 `—`（账里没暴露差异）。')
    A('')
    A('🔴 **先报一个总账级事实**：**9,614 行里 `等级 = P0` 的行有 0 条**'
      '（C 行只有 P1 × 369 与 P2 × 1,202）。⇒ 照「P0 ⇒ 主链」这条口径，**账推版永远判不出主链**。'
      '这不是脚本的错，是**账里没人用 P0**：要么主链问题都已修完，要么判等级时没人往 P0 上打。**这本身要人来定。**')
    A('')
    A('### 4.1 两版不一致的（= 真正需要人判的）')
    A('')
    A('| 功能点 | 域 | 我判 | 账推 | C 行的等级分布 | 方法数 | 怎么看 |')
    A('|---|---|---|---|---|---|---|')
    n1 = 0
    for fn, kind, dom, kd, g0, n in disagreements:
        if kind == '暂停' or kd == '—' or kind == kd:
            continue
        n1 += 1
        dist_ = ('P0×%d P1×%d P2×%d' % (g0.get('P0', 0), g0.get('P1', 0), g0.get('P2', 0))) if g0 else '无 C 行'
        look = ('**账里没有 C 行** ⇒ 要么这条功能点真的已经对齐，要么**账还没审到它**（看方法数）'
                if not g0 else '**有 C 但只判到 P1/P2** ⇒ 要么它确实是边角，要么**当时把主链问题按 P1 打了**')
        A(f'| **{fn}** | {dom} | {kind} | {kd} | {dist_} | {n} | {look} |')
    A('')
    A(f'共 **{n1}** 条**真冲突**（暂停线不计；「账推 = —」的见 4.2，那不算冲突）。')
    A('')
    A('### 4.2 账里**没有 C 行**、因而两版无法对照的（= 账还没暴露差异，或这条真的已对齐）')
    A('')
    A('| 功能点 | 域 | 我判 | 方法数 |')
    A('|---|---|---|---|')
    n2 = 0
    for fn, kind, dom, kd, g0, n in disagreements:
        if kind == '暂停' or kd != '—':
            continue
        n2 += 1
        A(f'| **{fn}** | {dom} | {kind} | {n} |')
    A('')
    A(f'共 **{n2}** 条。⚠️ 它们不是「没问题」，而是**账里这一块一条 C 都没判**（可能全 A/B/D，也可能没人审到）。')
    A('')
    A('**怎么用这一节**：`账推 = 主链` 的条目是**硬的**（账里有 P0 支撑）；`我判 = 主链 但账推 = 边角/—` 的，'
      '要么补 P0 等级，要么改我判版 —— **两条路都要人拍**，脚本不替你定。')
    A('')
    A('## §5 本轮没做到的')
    A('')
    A('1. **不是「功能点全集」**：这张表是**从账里长出来的**，账没覆盖到的功能（例如账里根本没列的方法）不会凭空出现。')
    A('2. **粒度不均**：`战斗逻辑驱动` 那 1,535 行按方法名切成了 12 个功能点，而 `本地化` 只有 10 行却是一个功能点 —— '
      '这是账的规模差异，不是重要性差异。')
    A('3. **「最高档 A」会骗人**：它只表示「至少一个方法已对齐」。判断这条功能点能不能验收，看**分布列**。')
    A('4. **活动线只标了「暂停」**，没有细化 —— 用户暂停期间不做归并。')
    A('')
    with io.open(DEST, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(L) + '\n')
    print('wrote', DEST, '功能点', len(items), '未归并行', sum(unmatched.values()), '未归类', len(unmatched))


if __name__ == '__main__':
    main()
