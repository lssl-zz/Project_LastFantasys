#define PLAN_A

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using CY_UI;
using Guirao.UltimateTextDamage;
using System;
using NetworkCommon.protocol;

public partial class CY_StageProcess : MonoSingletonBase<CY_StageProcess>, INotificationReceiver
{
    // 
    [SerializeField, FindObjectOfType]
    protected StageBackground StageBackground;
    public StageBackground GetStageBackground { get { return StageBackground; } }

    [SerializeField, FindObjectOfType]
    private StageMain1 StageMain;
    public StageMain1 GetStageMain { get { return StageMain; } }

    [SerializeField, FindGameObject("StageCamera")]
    protected Camera StageCamera;

    [SerializeField, GetComponent]
    protected PlayableDirector ActionPlayable;

    [SerializeField, FindObjectOfType]
    private UltimateTextDamageManager UltimateTextDamageManager;
    public UltimateTextDamageManager GetUltimateTextDamageManager { get { return UltimateTextDamageManager; } }

    public Camera GetStageCamera { get { return StageCamera; } }

    public CameraShakeController CameraShakeController { get; private set; } = null;

    [SerializeField, FindObjectOfType]
    protected CY_PreviewSkillProcess PreviewSkillProcess;

    #region Property

    [Header("■ Property")]
    [SerializeField]
    [Range(30f, 0f)]
    private float PlayerMoveSpeed = 10f;
    public float GetPlayerMoveSpeed { get { return PlayerMoveSpeed; } }

    // System
    /// <summary>
    /// 일시정지, 게임로드등 게임이 진행이 안될때 등을 관리하는 변수.
    /// </summary>
    public static bool IsPuase = true;

    [HideInInspector]
    public bool IsNextWave = false;

    /// <summary>
    /// 게임의 화면 이동과 같이 멈춰야하는 상태를 관리하는 변수.
    /// </summary>
    public static bool IsStageWait = true;

    public static bool IsDieCharacter = false;

    // Wave
    [HideInInspector]
    public int CurrentWaveCount { get; set; } = 0;
    [HideInInspector]
    public int MaxWaveCount { get; private set; } = 0;

    // Round
    [HideInInspector]
    public int CurrentActionRoundCount { get; set; } = 0;
    [HideInInspector]
    public int MaxActionRoundCount { get; set; } = 0;

    // Unit
    /// <summary>
    /// 플레이어 타입별로 메모리에 들고 있다가 리스폰 처리를 할수 있도록 함.
    /// - Key : HeroTable : UniqueID
    /// - Value : CY_CharacterEntity
    /// </summary>
    private Dictionary<int, CY_CharacterEntity> dicPlayerEntitys = new Dictionary<int, CY_CharacterEntity>();

    /// <summary>
    /// 플레이어 정보
    /// </summary>
    private List<CY_CharacterEntity> PlayerEntitys = new List<CY_CharacterEntity>();
    /// <summary>
    /// 플레이어 정보
    /// </summary>
    public List<CY_CharacterEntity> GetPlayerEntitys { get { return PlayerEntitys; } }

    /// <summary>
    /// 몬스터 타입별로 메모리에 들고 있다가 리스폰 가능하도록 해주자.
    /// - Key : HeroTable UniqueIND
    /// - Value : CY_CharacterEntity
    /// </summary>
    private Dictionary<int, CY_CharacterEntity> dicMonsterEntitys = new Dictionary<int, CY_CharacterEntity>();

    /// <summary>
    /// 실제 전투화면에서 동작하는 몬스터 객체들.
    /// </summary>
    private List<CY_CharacterEntity> MonsterEntirys = new List<CY_CharacterEntity>();
    /// <summary>
    /// 실제 전투화면에서 동작하는 몬스터 객체들.
    /// </summary>
    public List<CY_CharacterEntity> GetMonsterEntirys { get { return MonsterEntirys; } }

    /// <summary>
    /// 스테이지에서 사용되는 NPCInfo(몬스터)정보를 보관.
    /// </summary>
    List<NPCInfoData> NpcInfoDatas = new List<NPCInfoData>();

    // InGame
    /// <summary>
    /// 스테이지 시작 연출 관리.
    /// </summary>
    private IEnumerator StartStage = null;

    /// <summary>
    /// 스테이지 메인 루프 관리.
    /// </summary>
    private IEnumerator StageMainLoop = null;

    /// <summary>
    /// 필드에서 표기되는 캐릭터 정보 - 리소스
    /// </summary>
    private StageCharacterFieldInfo1 StageCharacterFieldInfo = null;
    /// <summary>
    /// 필드에 있는 아군 캐릭터 정보등을 부여하게 되는 정보.
    /// </summary>
    private List<StageCharacterFieldInfo1> StageCharacterFieldInfoPlayers = new List<StageCharacterFieldInfo1>();
    public List<StageCharacterFieldInfo1> GetStageCharacterFieldInfoPlayers { get { return StageCharacterFieldInfoPlayers; } }

    /// <summary>
    /// 캐릭터 사망시 사용되는 무덤객체
    /// </summary>
    private Gravestone Gravestone = null;
    /// <summary>
    /// 캐릭터 사망시 사용되는 무덤객체
    /// </summary>
    private List<Gravestone> Gravestones = new List<Gravestone>();
    /// <summary>
    /// 사망한 캐릭터 정보 모음
    /// </summary>
    private List<CY_CharacterEntity> DieCharacters = new List<CY_CharacterEntity>();
    /// <summary>
    /// 사망한 캐릭터 정보 모음
    /// </summary>
    public List<CY_CharacterEntity> GetDieCharacters { get { return DieCharacters; } }

    /// <summary>
    /// 필드에 있는 적군 캐릭터 정보등을 부여하게 되는 정보.
    /// </summary>
    private List<StageCharacterFieldInfo1> StageCharacterFieldInfoMonsters = new List<StageCharacterFieldInfo1>();
    public List<StageCharacterFieldInfo1> GetStageCharacterFieldInfoMonsters { get { return StageCharacterFieldInfoMonsters; } }

    // 공격자와 방어자 정보
    private CY_CharacterEntity SkillAction_AttackerEntity;
    private List<CY_CharacterEntity> SkillAction_DefenderEntitys;

    /// <summary>
    /// 동작중인 스킬의 데미지마크 최대수를 보관합니다.
    /// </summary>
    private int DamageMarkCount = 0;
    /// <summary>
    /// 스킬의 현재 데미지마크 수를 관리합니다.
    /// </summary>
    private int DamageCount = 0;
    /// <summary>
    /// 화면에 표기되는 스킬의 데미지량 입니다.
    /// </summary>
    private int DamageTotal = 0;
    /// <summary>
    /// 데미지 분할 처리 or 데미지 처리
    /// </summary>
    private List<List<int>> DivisionDamages = new List<List<int>>();
    private List<int> DivisionTotalDamages = new List<int>();
    /// <summary>
    /// 타겟의 데미지가 크리티컬인지 판단하는 함수.
    /// </summary>
    private List<bool> TargetCriticals = new List<bool>();
    /// <summary>
    /// 타겟의 데미지가 막기인지 판단하는 함수.
    /// </summary>
    private List<bool> TargetBlocks = new List<bool>();
    /// <summary>
    /// 회피 판단.
    /// </summary>
    private List<bool> IsEvades = new List<bool>();
    /// <summary>
    /// 스킬 사용할때 Hp를 감소하는 SkillSetup이 포함되어 있는지 확인.
    /// </summary>
    private bool IsAttackSkill = false;
    /// <summary>
    /// Heal스킬인지 판단.
    /// </summary>
    private bool IsHealSkill = false;
    /// <summary>
    /// 부활 스킬인지 판단.
    /// </summary>
    private bool IsRecoverSkill = false;
    /// <summary>
    /// 디버프 해제 스킬인지 판단.
    /// </summary>
    private bool IsUndebuffSkill = false;
    /// <summary>
    /// 버프, 디버프, CC 판단.
    /// </summary>
    private bool IsBuffDebuffCCSkill = false;
    /// <summary>
    /// 아레나 시작 연출.
    /// </summary>
    public bool IsArenaMatchingWait { get; set; } = false;

    /// <summary>
    /// 패시브 스킬 사용가능한 ModuleType을 관리
    /// </summary>
    private List<eModuleType> PassiveFilters = new List<eModuleType>() { eModuleType.Buff, eModuleType.Debuff, eModuleType.Immune };
    /// <summary>
    /// 모듈 그룹중 1회성 이펙트 로드.
    /// </summary>
    private EnumDictionary<eModuleGroup, EffectController> EffectOneModuleGroups = new EnumDictionary<eModuleGroup, EffectController>();
    /// <summary>
    /// Die처리
    /// </summary>
    private IEnumerator DieRoutine = null;

    /// <summary>
    /// 모듈 그룹중 Loop 이펙트 로드.(CC만 Loop로 존재)
    /// </summary>
    private EnumDictionary<eModuleGroup, EffectController> EffectCCLoopModuleGroups = new EnumDictionary<eModuleGroup, EffectController>();
    public EnumDictionary<eModuleGroup, EffectController> GetEffectCCLoopModuleGroups { get { return EffectCCLoopModuleGroups; } }

    /// <summary>
    /// 버프, 디버프 유지용 이펙트.
    /// </summary>
    private EnumDictionary<AuraType, EffectController> EffectLoopBuffAndDebuffAuras = new EnumDictionary<AuraType, EffectController>();
    public EffectController CreateAura(AuraType auraType, CY_CharacterEntity characterEntity)
    {
        // 
        if (EffectLoopBuffAndDebuffAuras.ContainsKey(auraType) == false)
            return null;

        // characterEntity 
        Transform parent = null;
        switch(EffectLoopBuffAndDebuffAuras[auraType].GetEffectPositionType)
        {
            case EffectPositionType.Bottom:
                parent = characterEntity.GetRoot;
                break;

            case EffectPositionType.Top:
                parent = characterEntity.GetHead;
                break;

            default:
                parent = characterEntity.GetHitPoint;
                break;
        }

        return EffectLoopBuffAndDebuffAuras[auraType].Spawn(parent);
    }

    /// <summary>
    /// 스테이지에 귀속되는 SkillSetupData
    /// </summary>
    private List<SkillSetupData> StageBuffSkillSetupDatas = new List<SkillSetupData>();
    /// <summary>
    /// 전투중 사망한 플레이어 UniqueID를 보관
    /// </summary>
    private List<int> DiePlayers = new List<int>();

    /// <summary>
    /// Skill이 물리 공격인지 판단해주는 필터.
    /// </summary>
    public List<eModuleGroup> DamageAttackSkillFilters = new List<eModuleGroup> 
    { 
        eModuleGroup.DamageRate, 
        //eModuleGroup.FixedDamage,
    };

    /// <summary>
    /// Skill이 마법 공격인지 판단해주는 필터.
    /// </summary>
    public List<eModuleGroup> MagicalDamageAttackSkillFilters = new List<eModuleGroup>
    {
        eModuleGroup.MagicalDamageRate,
    };

    private List<CY_CharacterEntity> ActionEntityList = new List<CY_CharacterEntity>();
    public List<CY_CharacterEntity> GetActionEntityList { get { return ActionEntityList; } }

    #endregion Property

    #region [기획 테스트용]

    [Header("■ Test 정보")]
    public int[] HeroUniqueID;

//#if UNITY_EDITOR
    [Header("■ 액티브 3번 시작 발동")]
    public bool isActiveSkillOn = false;
//#endif


    #endregion [테스트] - 테스트 후 삭제 필요

    private void Awake()
    {
        IsDieCharacter = false;

        //
        StageCharacterFieldInfo = CYResourcesManager.I.LoadForUnity<GameObject>(CY_Path.FullPath_Resources(PathType.InGame_Stage, "StageCharacterFieldInfo1")).GetComponent<StageCharacterFieldInfo1>();

        if (StageCharacterFieldInfo.IsExist())
            StageCharacterFieldInfo.CreatePool(12);

        //
        CY_Function.CreateScriptable();
        CY_Function.CreateMasterManager();

        //
        if(StageCamera != null)
        CameraShakeController = StageCamera.GetComponent<CameraShakeController>();
    }


    private IEnumerator Start()
    {
        if(PreviewSkillProcess.IsExist())
        {
            PreviewSkillProcess.GetPreviewMain.GetPreviewMain_Image_BlackPanel.SetActive(true);

            yield return PreviewSkillProcess.CreatePlayer();

            yield return PreviewSkillProcess.CreateMonster();

            yield return new WaitUntil(() => PreviewSkillProcess.IsCreatrMosterComplete);

            yield return PreviewSkillProcess.SetEntity();

            ActionPlayable.stopped += OnPlayableDirectorStopped;

            PreviewSkillProcess.GetPreviewMain.GetPreviewMain_Image_BlackPanel.SetActive(false);

            yield break;
        }

        // 데이터 로드 구조 확정시 처리가 필요함.
        if (UserInfo.I.IsFirstAppStart && GlobalDefine.IsEditor)
        {
            var addressInit = Addressables.InitializeAsync();
            yield return new WaitUntil(() => addressInit.IsDone);

#if UNITY_EDITOR
            // [기획-테스트] 플레이어 로케이션 정보 및 배치영웅 - 팀 정보.
            UserInfo.I.InitTeamInformation(HeroUniqueID);
#endif
            // 데이터 로드
            yield return DataLoad_Routine();

            // 아틀라스 로드
            yield return CYResourcesManager.I.SpriteAtlas_Load_All();

            // 101001 : 테이블 첫번째 데이터
            UserInfo.I.CurrentStage = DataTable.I.GetStageData(901001);     // 101002   // 101001
            UserInfo.I.CurrentBattleContentsType = eBattleContentsType.MainScenario;

            // 
            UserInfo.I.IsFirstAppStart = false;
        }

        // 스테이지 추가 정보
        UserInfo.I.CurrentBattleConditionData = DataTable.I.GetBattleConditionDataList().Find(x =>
                x.BattleContentsType == UserInfo.I.CurrentBattleContentsType);

        //
        IsStageWait = true;

        // 1.StageMain UI 
        StageMain.StageMainDisplay(false);

        if(UserInfo.I.CurrentBattleContentsType == eBattleContentsType.Arena)
        {
            if(UserInfo.I.EnemyArenaTeamInfomation.IsExist())
                yield return NetworkArenaSend.I.SendEnterArenaBattleReq_Routine(UserInfo.I.EnemyArenaTeamInfomation.MatchInfo.UserInfo.ID);
            else
                yield return NetworkArenaSend.I.SendEnterRandomArenaBattleReq_Routine();
        }

        // 2.로드 시작.
        yield return CreateStage_Routine();
        ActionPlayable.stopped += OnPlayableDirectorStopped;

        // 3.로드 완료.
        // SceneHandler.I.IsLoaded = true;

        // 4.세력 버프 체크
        yield return HeroGroupBuffCheck_Rouinte();

        // Sound Load
        yield return SoundLoad_Routine();

        if (UserInfo.I.CurrentBattleContentsType == eBattleContentsType.Arena)
            StageMain.SetArenaInfor();

        // 
        int soundIndex = UnityEngine.Random.Range(0, CY_SoundManager.I.BGM_Stages.Length);
        CY_SoundManager.I.PlayBGM(CY_SoundManager.I.BGM_Stages[soundIndex], AudioPath.BGM_Stage);

        // 
        CY_SceneHandler.I.IsNextLoading = true;

        // 4.게임 스타트 연출 시작.
        if (StartStage.IsExist())
        {
            StopCoroutine(StartStage);
            StartStage = null;
        }
        StartStage = StartStage_Routine();
        StartCoroutine(StartStage);
    }

    /// <summary>
    /// 아레나 - 전투 다시 시작 설정
    /// </summary>
    /// <returns></returns>
    public IEnumerator ArenaReset_Routine()
    {
        yield return NetworkArenaSend.I.SendEnterRandomArenaBattleReq_Routine();

        yield return LoadMonsterEntity_Routine();

        yield return ResetStage_Routine();

        // 적군 Wave에 맞는 객체 생성
        yield return ReSpawnMonster_Routine();

        // 4.세력 버프 체크
        yield return HeroGroupBuffCheck_Rouinte();

        StageMain.SetArenaInfor();

        // 4.게임 스타트 연출 시작.
        if (StartStage.IsExist())
        {
            StopCoroutine(StartStage);
            StartStage = null;
        }
        StartStage = StartStage_Routine();
        StartCoroutine(StartStage);
    }

    /// <summary>
    /// Unity Editor Mode에서만 로드하는 함수
    /// </summary>
    /// <returns></returns>
    private IEnumerator DataLoad_Routine()
    {
        yield return DataTable.I.Load_Routine();
    }

    #region Lobby Load

    private IEnumerator SoundLoad_Routine()
    {
        string[] list = CY_SoundManager.I.BGM_Stages;

        for (int i = 0; i < list.Length; ++i)
            yield return CYResourcesManager.I.AudioClip_Load_Routine(list[i], AudioPath.BGM_Stage, (sound) => { });
    }

    private EnumDictionary<eHeroGroupType, List<HeroGroupBuffEffect>> HeroGroupBuffPlayers = new EnumDictionary<eHeroGroupType, List<HeroGroupBuffEffect>>();
    private EnumDictionary<eHeroGroupType, List<HeroGroupBuffEffect>> HeroGroupBuffMonsters = new EnumDictionary<eHeroGroupType, List<HeroGroupBuffEffect>>();

    /// <summary>
    /// 영웅 그룹 버프 관리.
    /// </summary>
    private void SetHeroGroupBuff(eHeroGroupType e, List<int> options, int conditionCount, ref List<HeroGroupBuffEffect> heroGroupBuffEffects)
    {
        for (int i = 0; i < options.Count; ++i)
        {
            if (options[i] == 0)
                continue;

            var addOptionData = DataTable.I.GetAddOptionData(options[i]);

            if (addOptionData.IsNull())
                continue;

            int findIndex = heroGroupBuffEffects.FindIndex(x => x.AddOptionData.UniqueID == addOptionData.UniqueID);
            if (findIndex != -1)
                continue;

            HeroGroupBuffEffect heroGroupBuffEffect = new HeroGroupBuffEffect
            {
                HeroGroupType = e,
                AddOptionData = addOptionData,
                AddOptionCondition = conditionCount,
            };

            heroGroupBuffEffects.Add(heroGroupBuffEffect);
        }
    }

    private IEnumerator HeroGroupBuffCheck_Rouinte()
    {
        // Player
        List<HeroGroupBuffEffect> heroGroupBuffEffectPlayerViews = new List<HeroGroupBuffEffect>();
        CY_Function.SetHeroGroupBuff(PlayerEntitys, heroGroupBuffEffectPlayerViews, HeroGroupBuffPlayers);

        // Monster
        List <HeroGroupBuffEffect> heroGroupBuffEffectMonsterViews = new List<HeroGroupBuffEffect>();
        CY_Function.SetHeroGroupBuff(MonsterEntirys, heroGroupBuffEffectMonsterViews, HeroGroupBuffMonsters);

        // 
        yield return StageMain.InitStageHeroGroupBuff(heroGroupBuffEffectPlayerViews, heroGroupBuffEffectMonsterViews);

        //
        yield return HeroGroupBuffAdd_Routine();
    }

    private IEnumerator HeroGroupBuffAdd_Routine()
    {
        // HeroGroupBuffPlayers
        for(eHeroGroupType e = eHeroGroupType.Arna; e < eHeroGroupType.Max; ++e)
        {
            if (HeroGroupBuffPlayers.ContainsKey(e) is false)
                continue;

            var entitys = PlayerEntitys.FindAll(x => x.GetHeroData.HeroGroupType == e);
            if (entitys.Count <= 0)
                continue;

            for(int i = 0; i < entitys.Count; ++i)
                entitys[i].SetGroupBuffStat(HeroGroupBuffPlayers[e]);
        }

        // HeroGroupBuffMonsters
        for (eHeroGroupType e = eHeroGroupType.Arna; e < eHeroGroupType.Max; ++e)
        {
            if (HeroGroupBuffMonsters.ContainsKey(e) is false)
                continue;

            var entitys = MonsterEntirys.FindAll(x => x.GetHeroData.HeroGroupType == e);
            if (entitys.Count <= 0)
                continue;

            for (int i = 0; i < entitys.Count; ++i)
                entitys[i].SetGroupBuffStat(HeroGroupBuffMonsters[e]);
        }

        yield return null;

        // 세력버프에 체력이 있다면 갱신.
        for (int i = 0; i < StageCharacterFieldInfoPlayers.Count; ++i)
            StageCharacterFieldInfoPlayers[i].UpdateFieldInfoInit();

        for (int i = 0; i < StageCharacterFieldInfoMonsters.Count; ++i)
            StageCharacterFieldInfoMonsters[i].UpdateFieldInfoInit();
    }

    #endregion Lobby Load

    // # 최초 설정이후 반복전투를 위한 Reset 기능 필요
    // Enter : 최초 진입
    // Leave : 스테이지 이탈(ex.로비 이동)
    // Reset : 재도전 또는 다시도전(반복)
    #region Create Stage

    /// <summary>
    /// ■ 최초 진입시 스테이지 설정을 합니다
    /// 1.맵 생성
    /// 2.위치 생성
    /// 3.캐릭터(아군, 적군) 생성
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateStage_Routine()
    {
        // [함수] 호출 순서 중요.

        // Stage Type에 맞는 Buff 설정.
        yield return InitStageBuff();

        // Init NPCInfo (스테이지에서 사용되는 몬스터의 정보라고 보면 됨)
        yield return InitNPCInfo();

        // Wave
        yield return InitWaveSetting_Routine();

        // Gravestone(무덤 풀링)
        yield return InitGravestone();

        // Effect(버프, 디버프, CC등)객체 풀링
        yield return InitEffect();

        // 1.맵 로드
        yield return StageBackground.CreateBackgroundMover_Routine();

        // 2.위치(Location) 생성 - Create Location : Player, Monsters
        yield return StageBackground.CreateLocation_Routine();

        // 아군 객체 로드(오브젝트풀에 등록)
        yield return LoadPlayerEntity_Routine();

        // 아군 객체 생성
        yield return ReSpawnPlayer_Routine();

        // 적군 객체 (오브젝트풀에 등록)
        yield return LoadMonsterEntity_Routine();

        // 적군 Wave에 맞는 객체 생성
        yield return ReSpawnMonster_Routine();
    }

    /// <summary>
    /// 스테이지 버프 설정.
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitStageBuff()
    {
        for(int i = 0; i < UserInfo.I.StageSkills.Count; ++i)
        {
            int skillIndex = UserInfo.I.StageSkills[i];
            var skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex);

            if(skillSetupDatas.Count > 0)
                StageBuffSkillSetupDatas.AddRange(skillSetupDatas);
        }

        yield return null;
    }

    /// <summary>
    /// 아군 Entity를 생성합니다.
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadPlayerEntity_Routine()
    {
        TeamInfomation teamInfomation = UserInfo.I.GetPlayerTeamInfo(UserInfo.I.CurrentTeamIndex);

        for (int i = 0; i < teamInfomation.Heros.Count; ++i)
        {
            int heroUniqueID = teamInfomation.Heros[i];
            int heroLocationPoint = teamInfomation.HeroPoints[i];

            if (heroUniqueID == 0)
                continue;

            string heroResourcesName = null;
            CY_CharacterEntity entity = null;

            yield return CYResourcesManager.I.GameObject_Load_Routine(CY_Function.GetHeroLoadKey(heroUniqueID), GameObjectPath.Entity, (obj) =>
            {
                if (obj.IsNull())
                    StringExtention.Log($"[Hero] heroUniqueID : {heroUniqueID} is Null!!");

                entity = obj.GetComponent<CY_CharacterEntity>();

                entity.CreatePool(1);
                entity.SetTableData(heroUniqueID, SpineSkinType.Normal, heroLocationPoint, StageBackground.GetPlayerLocation.GetLocationTransform(heroLocationPoint));
                heroResourcesName = entity.GetHeroResourcesName();

                if (dicPlayerEntitys.ContainsKey(heroUniqueID) is false)
                    dicPlayerEntitys.Add(heroUniqueID, entity);
            });

            // TimeLine Data Load
            yield return Load_TimeLine(heroResourcesName);
        }

        yield return null;
    }
    
    /// <summary>
    /// 스테이지에 존재하는 몬스터를 오프젝트풀에 등록을 합니다.
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadMonsterEntity_Routine()
    {
        // 스테이지에서 쓰이는.. 모든 NPCInfo를 얻어서 MonsterEntity를 찾아서 오브젝트 풀 등록준비를 합니다.
        List<NPCInfoData> npcInfoDatas = GetNPCInfo();

        for (int i = 0; i < npcInfoDatas.Count; ++i)
        {
            NPCInfoData npcInfoData = npcInfoDatas[i];
            int heroUniqueID = npcInfoData.HeroID;

            string name = null;
            CY_CharacterEntity entity = null;

            yield return CYResourcesManager.I.GameObject_Load_Routine(CY_Function.GetHeroLoadKey(heroUniqueID), GameObjectPath.Entity, (obj) =>
            {
                if (obj.IsNull())
                    StringExtention.Log($"[NPC] heroUniqueID : {heroUniqueID} is Null!!");

                entity = obj.GetComponent<CY_CharacterEntity>();

                //
                entity.CreatePool(1);

                int locationPoint = GetLocationPoint(npcInfoData, i);

                entity.SetTableData(heroUniqueID, npcInfoData, SpineSkinType.Normal, locationPoint, null);
                name = entity.GetHeroResourcesName();

                // 몬스터 오브젝트 풀에 등록.
                if (dicMonsterEntitys.ContainsKey(heroUniqueID) is false)
                    dicMonsterEntitys.Add(heroUniqueID, entity);
            });

            yield return Load_TimeLine(name);
        }
    }

    /// <summary>
    /// 재시도시 플레이어 다시 생성 및 배치해주는 함수.
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReSpawnPlayer_Routine()
    {
        // 초기화.
        ReSpawnPlayer_Reset();
       
        // 
        TeamInfomation teamInfomation = UserInfo.I.GetPlayerTeamInfo(UserInfo.I.CurrentTeamIndex);

        for (int i = 0; i < teamInfomation.Heros.Count; ++i)
        {
            int heroUniqueID = teamInfomation.Heros[i];
            int heroLocationPoint = teamInfomation.HeroPoints[i];

            if (heroUniqueID == 0)
                continue;

            Transform localTranform = StageBackground.GetPlayerLocation.GetLocationTransform(heroLocationPoint - 1);
            CY_CharacterEntity entity = dicPlayerEntitys[heroUniqueID].Spawn(localTranform);

            entity.SetTableData(heroUniqueID, SpineSkinType.Normal, heroLocationPoint, localTranform);
            entity.SetLocationTypeInfo(StageBackground.GetPlayerLocation);

            PlayerEntitys.Add(entity);

            // StageCharacterFieldInfo Create
            {
                StageCharacterFieldInfo1 stageCharacterFieldInfo = StageCharacterFieldInfo.Spawn(entity.GetCharacterFieldInfoTrans);

                stageCharacterFieldInfo.InitCharacterInfo(entity.GetHeroData, UserInfo.I.GetMyHero(heroUniqueID), entity);
                StageCharacterFieldInfoPlayers.Add(stageCharacterFieldInfo);
                entity.SetStageCharacterFieldInfo(stageCharacterFieldInfo);
            }
            entity.InitManipulation(StageBackground.GetPlayerLocation);

            entity.GetSpineModel.InitSpineSkin();
        }

        // CharacterInfo UI 초기 설정 - 아군
        yield return StageMain.InitStageCharacterInfoPlayer();

        // CharacterTurn UI 초기 설정 - 아군
        yield return StageMain.InitStageCharacterTrunPlayer();

        for (int i = 0; i < StageCharacterFieldInfoPlayers.Count; ++i)
            StageCharacterFieldInfoPlayers[i].UpdateStateDisplay();

        
    }

    /// <summary>
    /// 캐릭터 부활
    /// </summary>
    /// <param name="dieCharacterEntity"></param>
    public CY_CharacterEntity ReSpawnCharacter_Recover(CY_CharacterEntity dieCharacterEntity)
    {
        CY_CharacterEntity recoverCharacter = null;

        int heroLocationPoint = dieCharacterEntity.GetLocationPoint;

        bool isNPC = dieCharacterEntity.IsNPC;
        if (isNPC)
        { // 적군

            NPCInfoData nPCInfoData = dieCharacterEntity.NPCInfoData;

            int MonsterLocationIndex = nPCInfoData.FormationNumber;
            var monsterLocation = StageBackground.GetMonsterLocationType(MonsterLocationIndex);
            

            Transform LocationTransform = monsterLocation.GetLocationTransform(nPCInfoData.FormationLocation - 1);
            CY_CharacterEntity monsterEntity = dicMonsterEntitys[nPCInfoData.HeroID].Spawn(LocationTransform);

            monsterEntity.SetTableData(nPCInfoData.HeroID, nPCInfoData, SpineSkinType.Normal, nPCInfoData.FormationLocation, LocationTransform);
            monsterEntity.SetLocationTypeInfo(monsterLocation);

            MonsterEntirys.Insert(nPCInfoData.FormationLocation - 1, monsterEntity);

            monsterEntity.IsUseRecover = true;

            // 
            recoverCharacter = monsterEntity;

            // StageCharacterFieldInfo Create
            {
                StageCharacterFieldInfo1 stageCharacterFieldInfo = StageCharacterFieldInfo.Spawn(monsterEntity.GetCharacterFieldInfoTrans);

                stageCharacterFieldInfo.InitCharacterInfo(nPCInfoData, monsterEntity);
                StageCharacterFieldInfoMonsters.Add(stageCharacterFieldInfo);
                monsterEntity.SetStageCharacterFieldInfo(stageCharacterFieldInfo);
            }
            monsterEntity.InitManipulation(monsterLocation);

            // 
            StageMain.UpdateStageCharacterTurnMonster(monsterEntity);

            //
            int findIndex = Gravestones.FindIndex(x => x.IsNPC == isNPC && x.LocationPoint == heroLocationPoint);
            if (findIndex != -1)
            {
                Gravestones[findIndex].ClearGravestone();
                Gravestones[findIndex].Recycle();
                Gravestones.RemoveAt(findIndex);
            }
        }
        else
        {// 아군

            //
            int heroUniqueID = dieCharacterEntity.GetHeroData.UniqueID;

            // 
            Transform localTranform = StageBackground.GetPlayerLocation.GetLocationTransform(heroLocationPoint - 1);
            CY_CharacterEntity entity = dicPlayerEntitys[heroUniqueID].Spawn(localTranform);

            // 
            entity.IsUseRecover = true;

            entity.SetTableData(heroUniqueID, SpineSkinType.Normal, heroLocationPoint, localTranform);
            entity.SetLocationTypeInfo(StageBackground.GetPlayerLocation);

            PlayerEntitys.Insert(heroLocationPoint - 1, entity);

            // 
            recoverCharacter = entity;

            // StageCharacterFieldInfo Create
            {
                StageCharacterFieldInfo1 stageCharacterFieldInfo = StageCharacterFieldInfo.Spawn(entity.GetCharacterFieldInfoTrans);

                stageCharacterFieldInfo.InitCharacterInfo(entity.GetHeroData, UserInfo.I.GetMyHero(heroUniqueID), entity);
                StageCharacterFieldInfoPlayers.Add(stageCharacterFieldInfo);
                entity.SetStageCharacterFieldInfo(stageCharacterFieldInfo);
            }
            entity.InitManipulation(StageBackground.GetPlayerLocation);

            // 
            StageMain.UpdateStageCharacterTurnPlayer(entity);

            //
            int findIndex = Gravestones.FindIndex(x => x.IsNPC == isNPC && x.LocationPoint == heroLocationPoint);
            if(findIndex != -1)
            {
                Gravestones[findIndex].ClearGravestone();
                Gravestones[findIndex].Recycle();
                Gravestones.RemoveAt(findIndex);
            }
        }
        return recoverCharacter;
    }

    /// <summary>
    /// Player 정보 초기화
    /// 1.CharacterEntity
    /// 2.FieldInfo
    /// 3.CharacterTurn(StageMain)
    /// </summary>
    private void ReSpawnPlayer_Reset()
    {
        // 
        for (int i = 0; i < PlayerEntitys.Count; ++i)
        {
            PlayerEntitys[i].ClearEntityData();
            PlayerEntitys[i].Recycle();
        }
        PlayerEntitys.Clear();

        for (int i = 0; i < StageCharacterFieldInfoPlayers.Count; ++i)
        {
            StageCharacterFieldInfoPlayers[i].ClearFieldInfo();
            StageCharacterFieldInfoPlayers[i].Recycle();
        }
        StageCharacterFieldInfoPlayers.Clear();

        for (int i = 0; i < StageMain.GetStageCharacterTurnPlayers.Count; ++i)
        {
            StageMain.GetStageCharacterTurnPlayers[i].Recycle();
        }
        StageMain.GetStageCharacterTurnPlayers.Clear();
    }

    /// <summary>
    /// Wave에 맞는 몬스터를 생성 및 배치를 해주는 함수.
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReSpawnMonster_Routine()
    {
        ReSpawnMonster_Reset();
     
        List<NPCInfoData> npcInfos = null;
        int monsterLocationIndex = GetMonsterLocationIndex(ref npcInfos);

        // 몬스터가 배치될 Location정보를 얻어옵니다.
        var monsterLocation = StageBackground.GetMonsterLocationType(monsterLocationIndex);

        // 현재 라운드의 NPCInfo의 NPC설정.
        for (int i = 0; i < npcInfos.Count; ++i)
        {
            NPCInfoData nPCInfoData = npcInfos[i];

            int formationLocation = GetLocationTypeIndex(nPCInfoData, i);

            Transform LocationTransform = monsterLocation.GetLocationTransform(formationLocation);

            CY_CharacterEntity monsterEntity = dicMonsterEntitys[nPCInfoData.HeroID].Spawn(LocationTransform);

            monsterEntity.SetTableData(nPCInfoData.HeroID, nPCInfoData, SpineSkinType.Normal, formationLocation + 1, LocationTransform);
            monsterEntity.SetLocationTypeInfo(monsterLocation);

            MonsterEntirys.Add(monsterEntity);

            // StageCharacterFieldInfo Create
            {
                StageCharacterFieldInfo1 stageCharacterFieldInfo = StageCharacterFieldInfo.Spawn(monsterEntity.GetCharacterFieldInfoTrans);

                stageCharacterFieldInfo.InitCharacterInfo(nPCInfoData, monsterEntity);
                StageCharacterFieldInfoMonsters.Add(stageCharacterFieldInfo);
                monsterEntity.SetStageCharacterFieldInfo(stageCharacterFieldInfo);
            }
            monsterEntity.InitManipulation(monsterLocation);

            monsterEntity.GetSpineModel.InitSpineSkin();
        }

        // CharacterInfo UI 초기 설정 - 적군
        yield return StageMain.InitStageCharacterInfoMonster(npcInfos);

        // CharacterTurn UI 초기 설정 - 적군
        yield return StageMain.InitStageCharacterTurnMonster();

        for (int i = 0; i < StageCharacterFieldInfoMonsters.Count; ++i)
            StageCharacterFieldInfoMonsters[i].UpdateStateDisplay();
    }

    /// <summary>
    /// Monster 정보 초기화
    /// 1.CharacterEntity
    /// 2.FieldInfo
    /// 3.CharacterTurn(StageMain)
    /// </summary>
    private void ReSpawnMonster_Reset()
    {
        for (int i = 0; i < MonsterEntirys.Count; ++i)
        {
            MonsterEntirys[i].ClearEntityData();
            MonsterEntirys[i].Recycle();
        }
        MonsterEntirys.Clear();

        for (int i = 0; i < StageCharacterFieldInfoMonsters.Count; ++i)
        {
            StageCharacterFieldInfoMonsters[i].ClearFieldInfo();
            StageCharacterFieldInfoMonsters[i].Recycle();
        }
        StageCharacterFieldInfoMonsters.Clear();

        for (int i = 0; i < StageMain.GetStageCharacterTurnMonsters.Count; ++i)
        {
            StageMain.GetStageCharacterTurnMonsters[i].Recycle();
        }
        StageMain.GetStageCharacterTurnMonsters.Clear();

        
    }

    /// <summary>
    /// 죽은 영웅 객체 반환.
    /// </summary>
    private void DieCharacter_Reset()
    {
        for (int i = 0; i < DieCharacters.Count; ++i)
        {
            DieCharacters[i].ClearEntityData();
            DieCharacters[i].Recycle();
        }
        DieCharacters.Clear();
    }


    /// <summary>
    /// 스테이지의 최대 웨이브를 설정하는 함수.
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitWaveSetting_Routine()
    {
        // 
        InitWaveSetting();

        // 
        StageMain.UpdateRoundDisplay(CurrentActionRoundCount);
        yield return null;
    }

    /// <summary>
    /// 무덤 리소스 초기화 함수.
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitGravestone()
    {
        yield return CYResourcesManager.I.GameObject_Load_Routine("Gravestone", GameObjectPath.Entity, (obj) => 
        {
            Gravestone = obj.GetComponent<Gravestone>();
            Gravestone.CreatePool(11);
        });
    }

    /// <summary>
    /// Effect(버프, 디버프, CC등)객체 풀링
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitEffect()
    {
        // 
        EffectController effectController = null;

        yield return CYResourcesManager.I.GameObject_Load_Routine("Buff_Aura", GameObjectPath.Effect_Common, (obj) =>
        {
            // EffectLoopBuffAndDebuffs
            if (obj.IsExist())
            {
                effectController = obj.GetComponent<EffectController>();

                if (effectController.IsExist())
                {
                    effectController.CreatePool(6);
                    EffectLoopBuffAndDebuffAuras.Add(AuraType.Buff, effectController);
                }
            }
        });

        yield return CYResourcesManager.I.GameObject_Load_Routine("Debuff_Aura", GameObjectPath.Effect_Common, (obj) =>
        {
            // EffectLoopBuffAndDebuffs
            if (obj.IsExist())
            {
                effectController = obj.GetComponent<EffectController>();

                if (effectController.IsExist())
                {
                    effectController.CreatePool(6);
                    EffectLoopBuffAndDebuffAuras.Add(AuraType.Debuff, effectController);
                }
            }
        });

        // 버프 : 2000번대
        for (eModuleGroup e = eModuleGroup.AttackUpRate; e <= eModuleGroup.CriticalRateUp; ++e)
        {
            yield return CYResourcesManager.I.GameObject_Load_Routine($"Buff_{e}", GameObjectPath.Effect_Common, (obj) => 
            {
                // EffectOneModuleGroups
                if(obj.IsExist())
                {
                    effectController = obj.GetComponent<EffectController>();

                    if(effectController.IsExist() && EffectOneModuleGroups.ContainsKey(e) == false)
                    {
                        effectController.CreatePool(6);
                        EffectOneModuleGroups.Add(e, effectController);
                    }
                }
            });
        }

        // 디버프 : 3000번대
        for (eModuleGroup e = eModuleGroup.AttackDownRate; e <= eModuleGroup.CriticalRateDown; ++e)
        {
            yield return CYResourcesManager.I.GameObject_Load_Routine($"Debuff_{e}", GameObjectPath.Effect_Common, (obj) =>
            {
                // EffectOneModuleGroups
                if (obj.IsExist())
                {
                    effectController = obj.GetComponent<EffectController>();

                    if (effectController.IsExist() && EffectOneModuleGroups.ContainsKey(e) == false)
                    {
                        effectController.CreatePool(6);
                        EffectOneModuleGroups.Add(e, effectController);
                    }
                }
            });
        }

        // CC : 5000번대
        for (eModuleGroup e = eModuleGroup.StunCount; e <= eModuleGroup.SleepCount; ++e)
        {
            //;
            yield return CYResourcesManager.I.GameObject_Load_Routine($"CC_{e}", GameObjectPath.Effect_Common, (obj) =>
            {
                // EffectLoopModuleGroups
                if (obj.IsExist())
                {
                    effectController = obj.GetComponent<EffectController>();

                    if (effectController.IsExist() && EffectOneModuleGroups.ContainsKey(e) == false)
                    {
                        effectController.CreatePool(6);
                        EffectCCLoopModuleGroups.Add(e, effectController);
                    }
                }
            });
        }

        yield return null;
    }

    protected IEnumerator Load_TimeLine(string name)
    {
        for (int ActionIndex = 1; ActionIndex < 4; ActionIndex++)
        {
            {
                TimelineAsset asset = null;
                yield return CYResourcesManager.I.TimelineAsset_Load_Routine(name, ActionIndex, TimeLine.TimeLine, (obj) =>
                {
                    asset = obj;
                });

                if (asset != null)
                {
                    //트랙, 클립
                    CY_TimeLine.TimeLineSplit(asset, 
                        (track) =>
                    {
                    },
                        (clip) =>
                    {
                        if (clip.asset.GetType() == typeof(CY_ControlPlayableAsset))
                        {
                            CY_ControlPlayableAsset clipAsset = clip.asset as CY_ControlPlayableAsset;
                            StartCoroutine(
                                    CYResourcesManager.I.GameObject_Load_Routine(clipAsset.prefabGameObject, clipAsset.prefabObjectType, (obj) =>
                                    {
                                        obj.CreatePool(1);
                                    }));
                        }
                    });

                    if (asset.markerTrack != null)
                    {
                        foreach (IMarker marker in asset.markerTrack.GetMarkers())
                        {
                            DamageMarker dmarker = marker as DamageMarker;

                            yield return CYResourcesManager.I.GameObject_Load_Routine(dmarker.EffectName, dmarker.EffectObjectType, (obj) =>
                            {
                                obj.CreatePool(1);
                            });
                        }
                    }
                }
            }

            {
                TimelineAsset asset = null;
                yield return CYResourcesManager.I.TimelineAsset_Load_Routine(name, ActionIndex, TimeLine.TimeLineDefender, (obj) =>
                {
                    asset = obj;
                });

                if (asset != null)
                {
                    CY_TimeLine.TimeLineSplit(asset,
                        (track) =>
                        {
                        },
                        (clip) =>
                        {
                            if (clip.asset.GetType() == typeof(CY_ControlPlayableAsset))
                            {
                                CY_ControlPlayableAsset clipAsset = clip.asset as CY_ControlPlayableAsset;
                                StartCoroutine(
                                        CYResourcesManager.I.GameObject_Load_Routine(clipAsset.prefabGameObject, clipAsset.prefabObjectType, (obj) =>
                                        {
                                            obj.CreatePool(1);
                                        }));
                            }
                        });
                }
            }

            {
                TimelineAsset asset = null;
                yield return CYResourcesManager.I.TimelineAsset_Load_Routine(name, ActionIndex, TimeLine.TimeLineAttacker, (obj) =>
                {
                    asset = obj;
                });

                if (asset != null)
                {
                    CY_TimeLine.TimeLineSplit(asset,
                        (track) =>
                        {
                        },
                        (clip) =>
                        {
                            if (clip.asset.GetType() == typeof(CY_ControlPlayableAsset))
                            {
                                CY_ControlPlayableAsset clipAsset = clip.asset as CY_ControlPlayableAsset;
                                StartCoroutine(
                                        CYResourcesManager.I.GameObject_Load_Routine(clipAsset.prefabGameObject, clipAsset.prefabObjectType, (obj) =>
                                        {
                                            obj.CreatePool(1);
                                        }));
                            }
                        });
                }
            }
        }
    }

    #endregion Create Stage

    #region Start Stage

    // 최초, 리셋등의 이유로 다시 시작할때의 처리(ex.캐릭터 진입등.. 실제 배틀 들어가기전까지의 연출)
    private IEnumerator StartStage_Routine()
    {
        // Stage 시작전 설정.
        UpdateWaveSetting(false, CY_SpineKey.Ani_Walk);

        if(UserInfo.I.CurrentBattleContentsType == eBattleContentsType.Arena)
        {
            IsArenaMatchingWait = true;

            // 아레나 시작 연출.
            CY_StagePanelProcess.I.ShowBattleMatching();
            yield return new WaitUntil(() => IsArenaMatchingWait is false);
        }

        // 1.배틀 시작 패시브 적용
        yield return StartStage_Passive();

        // 2.플레이어 화면으로 In
        yield return StageBackground.StartPlayerIn();

        // 3.이동 시작(Wave)
        StartStageBackgroundMover();
        yield return new WaitUntil(() => StageBackground.GetBackgroundMover.IsMoveComplete);
        IsStageWait = false;

        UpdateWaveSetting(false, CY_Function.SpineAnimationBinder(CY_SpineKey.Ani_Idle, 1));

        // 4.이동 완료
        StageMain.StageMainDisplay(true);

        // Tutorial
        if(UserInfo.I.CurrentBattleContentsType == eBattleContentsType.MainScenario)
        {
            if(UserInfo.I.IsFirstMainScenarioStage())
            {
                yield return TutorialCam_Routine(1);
            }
        }

        // 5.전투전 동작 모두 완료!
        IsPuase = false;
        IsStageWait = false;

        // Main Loop
        if (StageMainLoop.IsExist())
        {
            StopCoroutine(StageMainLoop);
            StageMainLoop = null;
        }

        StageMainLoop = StageMainLoop_Routine();
        StartCoroutine(StageMainLoop);
    }

    #endregion Start Stage

    #region Common

    /// <summary>
    /// 웨이브 종료후 다음 웨이브 포인트로 이동하는 함수
    /// </summary>
    protected void StartStageBackgroundMover()
    {
        IsStageWait = true;
        if (MaxWaveCount > 1)
            StageBackground.UpdateMonsterPointPosX();
        StageBackground.UpdateEnableMonsterLocation(CurrentWaveCount);
        StageBackground.GetBackgroundMover.StartBackgroundMove();
    }

    /// <summary>
    /// Wave전, 후로 UI 및 Animation 처리하는 함수
    /// </summary>
    private void UpdateWaveSetting(bool isActive, string animationName)
    {
        UpdateWaveAction(isActive);
        UpdatePlayerAllAnimation(animationName);
    }

    /// <summary>
    /// 1회성 이펙트 모듈 생성함수
    /// </summary>
    private void CreateEffectOneModule(eModuleGroup moduleGroup, CY_CharacterEntity characterEntity)
    {
        if (EffectOneModuleGroups.ContainsKey(moduleGroup) is false)
        {
            StringExtention.Log($"[Effect]moduleGroup : {moduleGroup}가 없습니다.");
            return;
        }

        Transform parent = null;
        switch (EffectOneModuleGroups[moduleGroup].GetEffectPositionType)
        {
            case EffectPositionType.Bottom:
                parent = characterEntity.GetRoot;
                break;

            case EffectPositionType.Top:
                parent = characterEntity.GetHead;
                break;

            default:
                parent = characterEntity.GetHitPoint;
                break;
        }

        var newEffectOnes = EffectOneModuleGroups[moduleGroup].Spawn(parent);
        newEffectOnes.StartEffect();
    }

    #endregion Common

    #region StageNPCInfoData

    /// <summary>
    /// 스테이지에서 사용하는 전체 NPCInfo 목록을 얻는 함수.
    /// </summary>
    /// <returns></returns>
    public List<NPCInfoData> GetNPCInfo() => (NpcInfoDatas);

    /// <summary>
    /// 해당 웨이브에서 사용되는 NPC목록을 얻는 함수.
    /// </summary>
    /// <returns></returns>
    private List<NPCInfoData> GetWaveNPCInfoList(int currentWave) => NpcInfoDatas.FindAll(x => x.Wave == currentWave);

    #endregion StageNPCInfoData

    #region Combat

    /// <summary>
    /// 스테이지 시작시 각 캐릭터의 패시브 스킬 적용.
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartStage_Passive()
    {
        // Player
        {
            for (int i = 0; i < PlayerEntitys.Count; ++i)
            {
                CY_CharacterEntity cY_CharacterEntity = PlayerEntitys[i];

                for (int ii = 0; ii < cY_CharacterEntity.GetPassiveSkillMaxCount; ++ii)
                {
                    int skillIndex = cY_CharacterEntity.GetPassiveSkillID(ii);

                    List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex);
                    SkillData skillData = DataTable.I.GetSkillDataList().Find(x => x.UniqueID == skillIndex);

                    if (skillIndex == 0 ||
                        skillSetupDatas.IsNull() ||
                        skillSetupDatas.Count <= 0 ||
                        skillData.IsNull() ||
                        PassiveFilters.FindIndex(x => x == skillData.ModuleType) == -1)     // 필터에 내용이 없으면 사용불가!
                        continue;

                    // 
                    cY_CharacterEntity.StartSkillSetup(skillSetupDatas, skillData);
                    cY_CharacterEntity.ActionBuffModule(skillSetupDatas);
                }

                // Stage Buff
                {
                    for(int ii = 0; ii < UserInfo.I.StageSkills.Count; ++ii)
                    {
                        int skillIndex = UserInfo.I.StageSkills[ii];
                        List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex && x.TargetType == eTargetType.Me_Player);
                        SkillData skillData = DataTable.I.GetSkillDataList().Find(x => x.UniqueID == skillIndex);

                        cY_CharacterEntity.StartSkillSetup(skillSetupDatas, skillData);
                        cY_CharacterEntity.ActionBuffModule(skillSetupDatas);
                    }
                }

                // HeroGroupBuff
                {
                    if(HeroGroupBuffPlayers.ContainsKey(cY_CharacterEntity.GetHeroData.HeroGroupType))
                    {
                        var addOptions = HeroGroupBuffPlayers[cY_CharacterEntity.GetHeroData.HeroGroupType].FindAll(x => x.AddOptionData.StatusType == eHeroStatus.Skill);

                        for(int ii = 0; ii < addOptions.Count; ++ii)
                        {
                            int skillIndex = addOptions[ii].AddOptionData.SkillID;
                            //List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex && x.TargetType == eTargetType.Me_Player);
                            List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex);
                            SkillData skillData = DataTable.I.GetSkillDataList().Find(x => x.UniqueID == skillIndex);

                            cY_CharacterEntity.StartSkillSetup(skillSetupDatas, skillData);
                            cY_CharacterEntity.ActionBuffModule(skillSetupDatas);
                        }
                    }
                }
            }
        }

        // Monster
        {
            for (int i = 0; i < MonsterEntirys.Count; ++i)
            {
                CY_CharacterEntity cY_CharacterEntity = MonsterEntirys[i];

                for (int ii = 0; ii < cY_CharacterEntity.GetPassiveSkillMaxCount; ++ii)
                {
                    int skillIndex = cY_CharacterEntity.GetPassiveSkillID(ii);

                    List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex);
                    SkillData skillData = DataTable.I.GetSkillDataList().Find(x => x.UniqueID == skillIndex);

                    if (skillIndex == 0 ||
                        skillSetupDatas.IsNull() ||
                        skillSetupDatas.Count <= 0 ||
                        skillData.IsNull() ||
                        PassiveFilters.FindIndex(x => x == skillData.ModuleType) == -1)     // 필터에 내용이 없으면 사용불가!
                        continue;

                    // 
                    cY_CharacterEntity.StartSkillSetup(skillSetupDatas, skillData);
                    cY_CharacterEntity.ActionBuffModule(skillSetupDatas);
                }

                // Stage Buff
                {
                    for (int ii = 0; ii < UserInfo.I.StageSkills.Count; ++ii)
                    {
                        int skillIndex = UserInfo.I.StageSkills[ii];
                        List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex && x.TargetType == eTargetType.Me_Enemy);
                        SkillData skillData = DataTable.I.GetSkillDataList().Find(x => x.UniqueID == skillIndex);

                        cY_CharacterEntity.StartSkillSetup(skillSetupDatas, skillData);
                        cY_CharacterEntity.ActionBuffModule(skillSetupDatas);
                    }
                }

                // HeroGroupBuff
                {
                    if (HeroGroupBuffMonsters.ContainsKey(cY_CharacterEntity.GetHeroData.HeroGroupType))
                    {
                        var addOptions = HeroGroupBuffMonsters[cY_CharacterEntity.GetHeroData.HeroGroupType].FindAll(x => x.AddOptionData.StatusType == eHeroStatus.Skill);

                        for (int ii = 0; ii < addOptions.Count; ++ii)
                        {
                            int skillIndex = addOptions[ii].AddOptionData.SkillID;
                            //List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex && x.TargetType == eTargetType.Me_Player);
                            List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == skillIndex);
                            SkillData skillData = DataTable.I.GetSkillDataList().Find(x => x.UniqueID == skillIndex);

                            cY_CharacterEntity.StartSkillSetup(skillSetupDatas, skillData);
                            cY_CharacterEntity.ActionBuffModule(skillSetupDatas);
                        }
                    }
                }
            }
        }

        // 버프가 생기면 갱신을 하자.
        for (int i = 0; i < StageCharacterFieldInfoPlayers.Count; ++i)
            StageCharacterFieldInfoPlayers[i].UpdateDisplayBuff();

        // 버프가 생기면 갱신을 하자.
        for (int i = 0; i < StageCharacterFieldInfoMonsters.Count; ++i)
            StageCharacterFieldInfoMonsters[i].UpdateDisplayBuff();

        yield return null;
    }

    /// <summary>
    /// 캐릭터 사망 처리 함수.
    /// </summary>
    /// <param name="isNPC"></param>
    /// <param name="characterEntity"></param>
    public void DieCharacter(bool isNPC, CY_CharacterEntity characterEntity)
    {       

        // 생성 및 해제 규칙
        // 1.CY_CharacterEntity
        // 2.FieldInfo
        // 3.CharacterTurn(StageMain)
        // - 위 3가지는 항상 생성, 반환시 한묶음으로 생각하자.

        CY_CharacterEntity cy_CharacterEntity = null;
        if (isNPC)
        {
            //
            GetStageCharacterFieldInfoMonsters.Remove(characterEntity.GetStageCharacterFieldInfo);
            GetStageMain.GetStageCharacterTurnMonsters.Remove(characterEntity.GetStageCharacterTurn);
            GetStageMain.GetStageCharacterTurnAll.Remove(characterEntity.GetStageCharacterTurn);

            //
            cy_CharacterEntity = GetMonsterEntirys.Find(x => x == characterEntity);
            GetMonsterEntirys.Remove(characterEntity);
        }
        else
        {
            // 
            DiePlayers.Add(characterEntity.GetHeroData.UniqueID);

            //
            GetStageCharacterFieldInfoPlayers.Remove(characterEntity.GetStageCharacterFieldInfo);
            GetStageMain.GetStageCharacterTurnPlayers.Remove(characterEntity.GetStageCharacterTurn);
            GetStageMain.GetStageCharacterTurnAll.Remove(characterEntity.GetStageCharacterTurn);

            //
            cy_CharacterEntity = GetPlayerEntitys.Find(x => x == characterEntity);
            GetPlayerEntitys.Remove(characterEntity);
        }

        //
        characterEntity.GetStageCharacterFieldInfo.ClearFieldInfo();
        characterEntity.GetStageCharacterFieldInfo.Recycle();
        characterEntity.GetStageCharacterTurn.Recycle();

        // 무덤 생성.
        if(cy_CharacterEntity.IsUseRecover is false)
            CreateGravestone(isNPC, cy_CharacterEntity);

        // 보관되어야하는 정보
        //CY_CharacterEntity newCharacterEntiry = new CY_CharacterEntity();
        //newCharacterEntiry.SetCharacterDieData(cy_CharacterEntity);

        // 죽은 객체들 보관
        //GetDieCharacters.Add(newCharacterEntiry);

        //cy_CharacterEntity.ClearEntityData();
        //cy_CharacterEntity.Recycle();

        // 
        // cy_CharacterEntity

        if(isNPC is false)
        {
            StageCharacterInfo1 dieEntityUI = null;

            dieEntityUI = StageMain.GetStageCharacterInfoPlayer(cy_CharacterEntity);
            dieEntityUI?.SetDieActive(true, cy_CharacterEntity.IsUseRecover);
        }
        else
        {
            StageCharacterEnemy dieEntityUI = null;

            dieEntityUI = StageMain.GetStageCharacterInfoMonster(cy_CharacterEntity);
            dieEntityUI?.SetDieActive(true, cy_CharacterEntity.IsUseRecover);
        }

        //StageCharacterInfo dieEntityUI = null;

        //dieEntityUI = isNPC is false ? StageMain.GetStageCharacterInfoPlayer(cy_CharacterEntity) : StageMain.GetStageCharacterInfoMonster(cy_CharacterEntity);
        //dieEntityUI?.SetDieActive(true, cy_CharacterEntity.IsUseRecover);

        cy_CharacterEntity.transform.SetParent(transform);
        cy_CharacterEntity.SetActive(false);
        GetDieCharacters.Add(cy_CharacterEntity);
    }

    /// <summary>
    /// 캐릭터 사망시 무덤 생성하는 함수
    /// [처리필요] : 1번 죽으면 무덤처리 한번 부활후에는 무덤 x
    /// </summary>
    /// <param name="isNPC"></param>
    /// <param name="cY_CharacterEntity"></param>
    private void CreateGravestone(bool isNPC, CY_CharacterEntity cY_CharacterEntity)
    {
        // [주의] : 추후에 Round가 여러개가 된다면.. 이건 또 다른 문제일것이다..
        Transform trans = isNPC ? StageBackground.GetMonsterLocationIndex(CurrentWaveCount).GetLocationTransform(cY_CharacterEntity.GetLocationPoint - 1)
            : StageBackground.GetPlayerLocation.GetLocationTransform(cY_CharacterEntity.GetLocationPoint - 1);

        //Transform trans = temp1.GetLocationTransform(cY_CharacterEntity.GetLocationPoint);

        Gravestone gravestone = Gravestone.Spawn(trans);

        // 
        gravestone.SetCharacterData(cY_CharacterEntity);

        Gravestones.Add(gravestone);
    }

    /// <summary>
    /// 캐릭터의 행동이 종료되면 ActionRound 증가 및 UI 갱신 함수.
    /// </summary>
    public void UpdateActionRound()
    {
        //IsStageWait = false;
        //++CurrentActionRoundCount;

        // ActionRound 관련 버프 스탯 증가.
        //for(int i = 0; i < GetPlayerEntitys.Count; ++i)
        //{
        //    GetPlayerEntitys[i].GetStageCharacterFieldInfo.UpdateBuffCheck();
        //}

        // 
        StageMain.UpdateRoundDisplay(++CurrentActionRoundCount, MaxActionRoundCount);

        if(UserInfo.I.CurrentBattleContentsType == eBattleContentsType.RuinedTemple)
        {
            for(int i = 0; i < GetMonsterEntirys.Count; ++i)
            {
                var monsterEntity = GetMonsterEntirys[i];

                // Attack
                var currentAttack = monsterEntity.GetCombatHeroStat(eHeroStatus.Attack).Float;
                float calcRateAttack = DataTable.I.BattleBasicConstValue.RuinedTempleAttackUp / 100f;
                currentAttack = currentAttack * calcRateAttack;
                monsterEntity.GetCharacterStatData.AddStats(eHeroStatus.Attack, (int)currentAttack);

                // PhysicalDefenseUp
                var currentPhysicalDefenseUp = monsterEntity.GetCombatHeroStat(eHeroStatus.PhysicalDefense).Float;
                float calcRatePhysicalDefenseUp = DataTable.I.BattleBasicConstValue.RuinedTemplePhysicalDefenseUp / 100f;
                currentPhysicalDefenseUp = currentPhysicalDefenseUp * calcRatePhysicalDefenseUp;
                monsterEntity.GetCharacterStatData.AddStats(eHeroStatus.Attack, (int)currentPhysicalDefenseUp);

                // MagicalDefenseUp
                var currentMagicalDefense = monsterEntity.GetCombatHeroStat(eHeroStatus.MagicalDefense).Float;
                float calcRateMagicalDefense = DataTable.I.BattleBasicConstValue.RuinedTempleMagicalDefenseUp / 100f;
                currentMagicalDefense = currentMagicalDefense * calcRateMagicalDefense;
                monsterEntity.GetCharacterStatData.AddStats(eHeroStatus.Attack, (int)currentMagicalDefense);
            }
        }
    }

    private WaitForSeconds waitForSeconds = new WaitForSeconds(0.5f);

    /// <summary>
    /// 캐릭터 턴 연출후 종료(정상 종료)
    /// </summary>
    public IEnumerator EndTurn_Routine(CY_CharacterEntity agent)
    {
        yield return new WaitUntil(() => agent.IsDOTCheck is false);

        // 연출 시작 - BlackPnael
        UpdateBlackPanelAction(agent, agent.CombatTargets, true, agent.CurrentSkillActiveNumber);

        StringExtention.Log($"agent.CombatTargets.Count : {agent.CombatTargets.Count}  /  agent.CurrentSkillActiveNumber : {agent.CurrentSkillActiveNumber}");
        TimeLine_SkillAction(agent, agent.CombatTargets, agent.CurrentSkillActiveNumber);

        yield return new WaitUntil(() => agent.IsActing is false);

        if(IsPuase)
            yield return new WaitUntil(() => IsPuase is false);

        
        // 연출 종료 - BlackPnael
        UpdateBlackPanelAction(agent, agent.CombatTargets, false, agent.CurrentSkillActiveNumber);

        // Die 연출이 있다면 있는거대로 지연을 시켜줘야 함.
        if (IsDieCharacter && agent.GetCharacterStatData.IsZombie is false)
        {
            yield return waitForSeconds;    //new WaitForSeconds(DieDelayTime);
            IsDieCharacter = false;
        }

        //int findIndex = agent.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.Zombie);
        //bool isZombieEnd = false;
        ////if(findIndex != -1)
        //{
        //    if(agent.GetCharacterStatData.IsDead && findIndex == -1)
        //    {
        //        //isZombieEnd = agent.GetCharacterStatData.SkillBuffDatas[findIndex].RepeatCount >= agent.GetCharacterStatData.SkillBuffDatas[findIndex].SkillSetupData.ArgumentList[5];
        //        isZombieEnd = true;
        //        IsDieCharacter = true;
        //        agent.GetCharacterStatData.IsZombie = false;
        //    }
        //}

        //if (IsDieCharacter && agent.GetCharacterStatData.IsZombie is false && isZombieEnd)
        //{
        //    agent.DieCharacter();
        //    yield return waitForSeconds;    //new WaitForSeconds(DieDelayTime);
        //    IsDieCharacter = false;
        //}

        if (IsRecoverSkill && IsDieCharacter is false)
        {// 캐릭터를 부활했다면 적군, 아군판단해서 DieCharacter목록을 지워야 함.
            Recover(agent);
            yield return waitForSeconds;
        }

        // 자체 모듈로 살아나는거 여기서 처리를 해줘야함.
        //int findIndex = agent.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.Recover &&
        //                                                                    x.SkillSetupData.ModuleType == eModuleType.Buff && agent.IsRecover is false);

        int findIndex = -1;
        CY_CharacterEntity dieCharacter = null;
        for (int i = 0; i < DieCharacters.Count; ++i)
        {
            dieCharacter = DieCharacters[i];
            findIndex = dieCharacter.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.Recover &&
                                                                            x.SkillSetupData.ModuleType == eModuleType.Buff && dieCharacter.IsUseRecover is false && 
                                                                            agent.GetCharacterStatData.IsZombie is false);
            
            if (findIndex != -1)
                break;
        }

        //if (findIndex != -1 && agent.GetCharacterStatData.IsDead)
        if (findIndex != -1)
        {
            List<SkillSetupData> skillsetupDatas = new List<SkillSetupData>();
            skillsetupDatas.Add(dieCharacter.GetCharacterStatData.SkillBuffDatas[findIndex].SkillSetupData);
            ActionRecover(skillsetupDatas, dieCharacter);
            Recover(dieCharacter);
            yield return waitForSeconds;
        }

        // 
        if (IsGameoverCheck())
            yield break;

        // Turn 정상 종료
        //EndTurn(agent);

        //
        IsStageWait = false;
        IsAttackSkill = false;
        IsHealSkill = false;
        IsRecoverSkill = false;
        IsUndebuffSkill = false;
        IsBuffDebuffCCSkill = false;

        // ActionRound 버프 체크
        //{
        //    for (int i = 0; i < GetStageCharacterFieldInfoPlayers.Count; ++i)
        //        GetStageCharacterFieldInfoPlayers[i].UpdateStartActionRoundBuff();

            //    for (int i = 0; i < GetStageCharacterFieldInfoMonsters.Count; ++i)
            //        GetStageCharacterFieldInfoMonsters[i].UpdateStartActionRoundBuff();

            //    UpdateActionRound();
            //}

            // 턴 종료 처리
            // 1.속도 초기화
            // 2.AP 충전 or 소비
            // 3.버프, 디버프 턴 차감
            // 4.각성 패시브 체크/ 활성
            // 5.적군(피격자) 사망 여부 체크
            // 6.승패 체크

            // # 번외 작업
            // 7.공격자 + 방어자?들 필드인포 갱신 필요.

        // 1.속도 초기화(시간 초기화.)
        agent.GetStageCharacterTurn.InitStartTime();
        agent.Clear_TimeLine();

        // 2.AP 충전 or 소모
        if (agent.CurrentSkillActiveNumber == 1)
        {
            findIndex = agent.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.AddBlock);
            int isSilenceFindIndex = agent.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.SilenceCount);

            if (findIndex == -1 && isSilenceFindIndex == -1)
            {
                // 스킬1(평타)
                //SkillSetupData skillSetupData = agent.GetSkillModuleGroup(eModuleGroup.APRecovery, false, false);
                //float apRecoveryValue = 0f;
                //if (skillSetupData.IsExist() && agent.CalcProbability(skillSetupData))
                //    apRecoveryValue = agent.GetSkillModuleArgumentValue(skillSetupData);

                SkillSetupData skillSetupData = agent.GetSkillModuleGroup(eModuleGroup.APRecoveryDown, false, false);
                float apRecoveryDownValue = 0f;
                if (skillSetupData.IsExist() && agent.CalcProbability(skillSetupData))
                {
                    //int downIndex = agent.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData == skillSetupData);

                    //CY_CharacterEntity actorEntity = null;
                    //if (downIndex != -1)
                    //    actorEntity = agent.GetCharacterStatData.SkillBuffDatas[downIndex].actorCharacterEntity;

                    CY_CharacterEntity actorEntity = agent.GetSkillModuleActorEntity(skillSetupData, agent);

                    apRecoveryDownValue = agent.GetSkillModuleArgumentValue(skillSetupData, true, actorEntity);
                }

                int lastAP = agent.CurrentSkillData.AP - (int)apRecoveryDownValue;

                if (lastAP < 0)
                    lastAP = 0;

                agent.AddAP(lastAP , "?");

                //// 
                //int lastAP = agent.CurrentSkillData.AP + (int)apRecoveryValue - (int)apRecoveryDownValue;

                //if (lastAP < 0)
                //    lastAP = 0;

                //agent.GetCharacterStatData.AddAP(agent.CurrentSkillData.AP);
            }

            //agent.GetCharacterStatData.AddAP(agent.CurrentSkillData.AP + (int)apRecoveryValue);
        }
        else if(agent.CurrentSkillActiveNumber == 2)
        {// 스킬2(AP사용스킬)

            // AP 사용(감소)
            agent.AddAP(agent.CurrentSkillData.AP , " skill ");

            // 2번 스킬을 사용했다는건 각성 포인트를 추가해줘야함.
            agent.GetCharacterStatData.AddBead();
        }
        else if (agent.CurrentSkillActiveNumber == 3)
        {// 각성 스킬
            agent.GetCharacterStatData.BeadCount = 0;
        }

        // 
        agent.GetStageCharacterFieldInfo.UpdateStateDisplay();

        // Count형 버프
        //agent.GetStageCharacterFieldInfo.UpdateStartCountBuff();

        // 
        if(GetActionEntityList.Count > 0)
            GetActionEntityList.RemoveAt(0);

        yield return null;
    }

    private void Recover(CY_CharacterEntity agent)
    {
        List<CY_CharacterEntity> lifeCharacters = agent.IsNPC ? GetMonsterEntirys : GetPlayerEntitys;
        for (int i = 0; i < lifeCharacters.Count; ++i)
        {
            if (agent.IsNPC)
            {
                int findIndex = DieCharacters.FindIndex(x => x.NPCInfoData.UniqueID == lifeCharacters[i].NPCInfoData.UniqueID);
                if (findIndex != -1)
                {
                    DieCharacters[findIndex].ClearEntityData();
                    DieCharacters[findIndex].Recycle();
                    DieCharacters.RemoveAt(findIndex);
                }
            }
            else
            {
                int findIndex = DieCharacters.FindIndex(x => x.GetHeroData.UniqueID == lifeCharacters[i].GetHeroData.UniqueID);
                if (findIndex != -1)
                {
                    DieCharacters[findIndex].ClearEntityData();
                    DieCharacters[findIndex].Recycle();
                    DieCharacters.RemoveAt(findIndex);
                }

            }
        }
    }

    /// <summary>
    /// 캐릭터 행동 불가시 턴종료 함수.
    /// </summary>
    public IEnumerator EndTurn_InablilityAct_Routine(CY_CharacterEntity agent)
    {
        //
        IsStageWait = false;
        IsAttackSkill = false;
        IsHealSkill = false;
        IsRecoverSkill = false;
        IsUndebuffSkill = false;
        IsBuffDebuffCCSkill = false;

        // ActionRound 버프 체크
        {
            for (int i = 0; i < GetStageCharacterFieldInfoPlayers.Count; ++i)
                GetStageCharacterFieldInfoPlayers[i].UpdateStartActionRoundBuff();

            for (int i = 0; i < GetStageCharacterFieldInfoMonsters.Count; ++i)
                GetStageCharacterFieldInfoMonsters[i].UpdateStartActionRoundBuff();

            UpdateActionRound();
        }

        // 1.속도 초기화(시간 초기화.)
        agent.GetStageCharacterTurn.InitStartTime();
        agent.Clear_TimeLine();

        // 
        agent.GetStageCharacterFieldInfo.UpdateStateDisplay();

        // 
        agent.GetStageCharacterFieldInfo.UpdateStartCountBuff();

        if (GetActionEntityList.Count > 0)
            GetActionEntityList.RemoveAt(0);

        yield return null;
    }

    public IEnumerator EndTurn_DOT_Die_Routine(CY_CharacterEntity agent)
    {
        yield return agent.DOT_DieCharacter();

        int findIndex = -1;
        CY_CharacterEntity dieCharacter = null;
        for (int i = 0; i < DieCharacters.Count; ++i)
        {
            dieCharacter = DieCharacters[i];
            findIndex = dieCharacter.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.Recover &&
                                                                            x.SkillSetupData.ModuleType == eModuleType.Buff && dieCharacter.IsUseRecover is false);
            // 
            if (findIndex != -1)
                break;
        }

        if (findIndex != -1)
        {
            List<SkillSetupData> skillsetupDatas = new List<SkillSetupData>();
            skillsetupDatas.Add(dieCharacter.GetCharacterStatData.SkillBuffDatas[findIndex].SkillSetupData);
            ActionRecover(skillsetupDatas, dieCharacter);
            Recover(dieCharacter);
            yield return waitForSeconds;
        }

        //
        if (IsGameoverCheck())
            yield break;

        //
        IsStageWait = false;
        IsAttackSkill = false;
        IsHealSkill = false;
        IsRecoverSkill = false;
        IsUndebuffSkill = false;
        IsBuffDebuffCCSkill = false;

        // ActionRound 버프 체크
        {
            for (int i = 0; i < GetStageCharacterFieldInfoPlayers.Count; ++i)
                GetStageCharacterFieldInfoPlayers[i].UpdateStartActionRoundBuff();

            for (int i = 0; i < GetStageCharacterFieldInfoMonsters.Count; ++i)
                GetStageCharacterFieldInfoMonsters[i].UpdateStartActionRoundBuff();

            UpdateActionRound();
        }

        if (GetActionEntityList.Count > 0)
            GetActionEntityList.RemoveAt(0);

        yield return null;
    }


    /// <summary>
    /// 캐릭터 턴 종료시 호출되는 함수.(정상 연출 완료 후)
    /// </summary>
    /// <param name="characterEntity"></param>
    public void EndTurn(CY_CharacterEntity characterEntity)
    {
        //
        IsStageWait = false;
        IsAttackSkill = false;
        IsHealSkill = false;
        IsRecoverSkill = false;
        IsUndebuffSkill = false;
        IsBuffDebuffCCSkill = false;

        // ActionRound 버프 체크
        {
            for (int i = 0; i < GetStageCharacterFieldInfoPlayers.Count; ++i)
                GetStageCharacterFieldInfoPlayers[i].UpdateStartActionRoundBuff();

            for (int i = 0; i < GetStageCharacterFieldInfoMonsters.Count; ++i)
                GetStageCharacterFieldInfoMonsters[i].UpdateStartActionRoundBuff();

            UpdateActionRound();
        }

        // 턴 종료 처리
        // 1.속도 초기화
        // 2.AP 충전 or 소비
        // 3.버프, 디버프 턴 차감
        // 4.각성 패시브 체크/ 활성
        // 5.적군(피격자) 사망 여부 체크
        // 6.승패 체크

        // # 번외 작업
        // 7.공격자 + 방어자?들 필드인포 갱신 필요.

        // 1.속도 초기화(시간 초기화.)
        characterEntity.GetStageCharacterTurn.InitStartTime();
        characterEntity.Clear_TimeLine();

        // 2.AP 충전 or 소모
        if (characterEntity.CurrentSkillActiveNumber == 1 || characterEntity.CurrentSkillActiveNumber == 2)
        {   // 스킬1(평타) / 스킬2(AP사용스킬) 두 경우에만 검사를 합니다.

            // AP 차단 모듈
            //if(characterEntity.GetCharacterStatData.SkillBuffDatas.Find(x => x.SkillSetupData.ModuleGroup == ??? ) != null)       

            // 
            int addAP = 0;

            // AP 회복속도 증가
            //if (characterEntity.GetCharacterStatData.SkillBuffDatas.Find(x => x.SkillSetupData.ModuleGroup == ??? ) != null)
            //{
            //    addAP = [0]
            //}

            // 
            characterEntity.AddAP(characterEntity.CurrentSkillData.AP + addAP, "?");

            // 2번 스킬을 사용했다는건 각성 포인트를 추가해줘야함.
            if (characterEntity.CurrentSkillActiveNumber == 2)
            {
                characterEntity.GetCharacterStatData.AddBead();
            }
        }
        else if(characterEntity.CurrentSkillActiveNumber == 3)
        {// 각성 스킬
            characterEntity.GetCharacterStatData.BeadCount = 0;
        }

        // 
        characterEntity.GetStageCharacterFieldInfo.UpdateStateDisplay();

        // 
        characterEntity.GetStageCharacterFieldInfo.UpdateStartCountBuff();
    }

    /// <summary>
    /// 전투 연출시 BlackPanel Layer관리하는 함수.
    /// 1.전투 시작과 종료시 반드시 한번씩 호출이 되어야함
    /// </summary>
    /// <param name="attackerEntity"></param>
    /// <param name="defenderEntity"></param>
    /// <param name="isActionStart"></param>
    public void UpdateBlackPanelAction(CY_CharacterEntity attackerEntity, List<CY_CharacterEntity> defenderEntity, bool isActionStart, int skillNumber)
    {
        string layerName = isActionStart ? CY_Constant.BlackFront : CY_Constant.BlackBack;

        int activeSkillID = attackerEntity.GetActiveSkillID(skillNumber - 1);
        SkillData skillData = DataTable.I.GetSkillDataList().Find(x => x.UniqueID == activeSkillID);
        IsHealSkill = skillData.ModuleType == eModuleType.Heal;

        if (IsBlackPnael(skillNumber))
        {
            if (attackerEntity.IsNPC)
            {
                // 
                StageBackground.GetMonsterLocationIndex(CurrentWaveCount).UpdateSortingGroupLocationPoint(attackerEntity.GetLocationPoint, layerName, true, isActionStart);

                for (int i = 0; i < defenderEntity.Count; ++i)
                {
                    CY_CharacterEntity entity = defenderEntity[i];

                    if(IsHealSkill is false)
                    {
                        if (attackerEntity.IsNPC == entity.IsNPC)
                            continue;
                    }
                    else
                    {
                        if (attackerEntity.IsNPC != entity.IsNPC)
                            continue;
                    }

                    LocationType locationType = IsHealSkill ? StageBackground.GetMonsterLocationIndex(CurrentWaveCount) : StageBackground.GetPlayerLocation;
                    locationType.UpdateSortingGroupLocationPoint(entity.GetLocationPoint, layerName, false, isActionStart);

                    //StageBackground.GetPlayerLocation.UpdateSortingGroupLocationPoint(entity.GetLocationPoint, layerName, false, isActionStart);
                }
            }
            else
            {
                StageBackground.GetPlayerLocation.UpdateSortingGroupLocationPoint(attackerEntity.GetLocationPoint, layerName, true, isActionStart);

                for (int i = 0; i < defenderEntity.Count; ++i)
                {
                    CY_CharacterEntity entity = defenderEntity[i];

                    if(IsHealSkill is false)
                    {
                        if (attackerEntity.IsNPC == entity.IsNPC)
                            continue;
                    }
                    else
                    {
                        if (attackerEntity.IsNPC != entity.IsNPC)
                            continue;
                    }

                    //StageBackground.GetMonsterLocationIndex(CurrentWaveCount).UpdateSortingGroupLocationPoint(entity.GetLocationPoint, layerName, false, isActionStart);

                    LocationType locationType = IsHealSkill ? StageBackground.GetPlayerLocation : StageBackground.GetMonsterLocationIndex(CurrentWaveCount);
                    locationType.UpdateSortingGroupLocationPoint(entity.GetLocationPoint, layerName, false, isActionStart);
                }
            }
        }

        StageBackground.UpdateBlackPanel(IsBlackPnael(skillNumber));
    }

    /// <summary>
    /// 사용스킬에 따라 BlackPanel Active 활성화 체크
    /// </summary>
    /// <param name="skillNumber"></param>
    /// <returns></returns>
    private bool IsBlackPnael(int skillNumber)
    {
        return skillNumber == 2 || skillNumber == 3;
    }

    public void Start_DOT_Delay(CY_CharacterEntity agent)
    {
        StartCoroutine(Start_DOT_Delay_Routine(agent));
    }

    private IEnumerator Start_DOT_Delay_Routine(CY_CharacterEntity agent)
    {
        //agent.UpdateAI(AIType.Pause);
        agent.GetSpineModel.SetAnimation(CY_SpineKey.Ani_Hit);
        agent.GetSpineModel.AddAnimation(CY_Function.SpineAnimationBinder(CY_SpineKey.Ani_Idle, 1), true);
        yield return new WaitForSeconds(agent.GetSpineModel.GetAnimationEndTime());
        //agent.UpdateAI(AIType.ReStart);
        agent.IsDOTCheck = false;
    }

    // 전투 공식 -------------------------------------------------------
    /// <summary>
    /// 전투에 적용 + 연출되는 공격 / 힐을 구하는 함수.
    /// </summary>
    private void CalcCombatDamage(CY_CharacterEntity attackerEntity, List<CY_CharacterEntity> defenderEntitys)
    {
        // 
        DivisionDamages.Clear();
        DivisionTotalDamages.Clear();

        TargetCriticals.Clear();
        TargetBlocks.Clear();

        IsEvades.Clear();

        // 데미지 계산.(공식 추후에 관리하는 부분 만들어야 함.)
        if (IsAttackSkill)
        {
            for (int i = 0; i < defenderEntitys.Count; ++i)
            {
                bool isCritical = false;
                bool isBlock = false;
                bool isEvade = false;

                int mainDamage = attackerEntity.GetDamage(defenderEntitys[i], out isCritical, out isBlock, out isEvade);
                var newList = new List<int>();

                TargetCriticals.Add(isCritical);
                TargetBlocks.Add(isBlock);
                IsEvades.Add(isEvade);

                for (int ii = 0; ii < DamageMarkCount; ++ii)
                {
                    // 
                    //DivisionTotalDamages[ii].Add(0);
                    DivisionTotalDamages.Add(0);

                    //
                    int damage = Mathf.FloorToInt(mainDamage / DamageMarkCount);

                    if (ii == DamageMarkCount - 1 && newList.Count > 0)
                    {
                        int total = 0;
                        for (int iii = 0; iii < newList.Count; ++iii)
                            total += newList[iii];

                        damage = mainDamage - total;
                    }

                    newList.Add(damage);
                }
                DivisionDamages.Add(newList);
            }
        }

        if (IsHealSkill)
        {
            for (int i = 0; i < defenderEntitys.Count; ++i)
            {
                bool isCritical = false;

                float mainDamage = attackerEntity.GetHeal(out isCritical, defenderEntitys[i]);
                var newList = new List<int>();

                TargetCriticals.Add(isCritical);

                for (int ii = 0; ii < DamageMarkCount; ++ii)
                {
                    // 
                    //DivisionTotalDamages[ii].Add(0);
                    DivisionTotalDamages.Add(0);

                    //
                    int damage = Mathf.FloorToInt(mainDamage / DamageMarkCount);

                    if (ii == DamageMarkCount - 1 && newList.Count > 0)
                    {
                        int total = 0;
                        for (int iii = 0; iii < newList.Count; ++iii)
                            total += newList[iii];

                        damage = (int)(mainDamage - total);
                    }

                    newList.Add(damage);
                }

                DivisionDamages.Add(newList);
            }
        }
    }

    #endregion Combat

    #region MainLoop

    /// <summary>
    /// 메인 전투 반복 
    /// </summary>
    /// <returns></returns>
    private IEnumerator StageMainLoop_Routine()
    {
        // 턴 시간 시작!
        StageMain.InitStageCharacterTurnPlayerTime();
        StageMain.InitStageCharacterTurnMonsterTime();

        // Time
        StageMain.UpdateTime(UserInfo.I.GetCombatSpeedIndex(UserInfo.I.CurrentTeamIndex));

        if(UserInfo.I.IsTutorialActing)
            GetStageMain.ActionPause(true);

        // Auto Check
        StageMain.UpdateAutoSkill(UserInfo.I.GetCombatAutoSkill(UserInfo.I.CurrentTeamIndex));

        // 
        StageMain.StageCharacterTurnAllAdd();

        while (true)
        {
            if (IsPuase is false)
            {
                if(GetActionEntityList.Count > 0 && IsStageWait is false)
                {
                    // 전투 연출 시작.
                    IsStageWait = true;

                    GetActionEntityList.Sort((a, b) => b.GetStageCharacterTurn.GetProgress.CompareTo(a.GetStageCharacterTurn.GetProgress));
                    GetActionEntityList[0].AddAP(UserInfo.I.CurrentBattleConditionData.ActionRoundAP, " ActionRoundAP ");//시작할때 ap추가
                    GetActionEntityList[0].GetStageCharacterFieldInfo.UpdateStateDisplay();
                    GetActionEntityList[0].UpdateAI(AIType.Start);
                }
                else
                {
                    StageMain.UpdateStageCharacterTurnAll();
                }

                // 다음 웨이브 이동 연출.
                if (IsNextWave)
                {
                    IsStageWait = true;

                    // 
                    ++CurrentWaveCount;

                    if (CurrentWaveCount >= MaxWaveCount)
                    {
                        break;
                    }

                    StageMain.UpdateRoundDisplay(CurrentActionRoundCount);

                    //
                    StartStageBackgroundMover();

                    // 몬스터 설정
                    yield return ReSpawnMonster_Routine();
                    yield return new WaitUntil(() => StageBackground.GetBackgroundMover.IsMoveComplete);
                    IsStageWait = false;
                }
                yield return new WaitUntil(() => IsNextWave == false);
            }

            yield return null;
        }
    }

    #endregion MainLoop

    #region Reset

    /// <summary>
    /// 스테이지를 재시도 할때 호출되는 함수.
    /// </summary>
    public void ResetStage()
    {
        StartCoroutine(ResetStage_Routine());
    }

    private IEnumerator ResetStage_Routine()
    {
        // 1.배경 원상 복구
        StageBackground.ResetBackground();
        StageBackground.GetBackgroundMover.ResetBackgroundMover();

        // 2.UI 처리
        StageMain.StageMainDisplay(false);

        // 3.데이터 복구
        IsPuase = true;
        IsStageWait = true;
        IsArenaMatchingWait = false;

        StageBuffSkillSetupDatas.Clear();
        DiePlayers.Clear();

        // 
        CurrentWaveCount = 0;

        CurrentActionRoundCount = 0;
        StageMain.UpdateRoundDisplay(CurrentActionRoundCount, MaxActionRoundCount);

        // 
        StageMain.ResetStageCharacterInfo();

        // 무덤 복구
        for (int i = 0; i < Gravestones.Count; ++i)
        {
            Gravestones[i].ClearGravestone();
            Gravestones[i].Recycle();
        }
        Gravestones.Clear();

        // 죽은 영웅 객체 복수
        DieCharacter_Reset();

        // CharacterEntity 복구
        ReSpawnPlayer_Reset();
        ReSpawnMonster_Reset();

        StageMain.StageCharacterTurnAllReset();
        GetActionEntityList.Clear();

        // 아군 리셋
        yield return ReSpawnPlayer_Routine();

        // 적군 리셋
        yield return ReSpawnMonster_Routine();

        StageMain.StageCharacterTurnAllAdd();

        // Start Stage Reset
        if (StartStage.IsExist())
        {
            StopCoroutine(StartStage);
            StartStage = null;
        }

        // Time
        CY_Function.InitTime();

        // Main Loop Reset.
        if (StageMainLoop.IsExist())
        {
            StopCoroutine(StageMainLoop);
            StageMainLoop = null;
        }

        // Start Stage + MainLoop
        StartStage = StartStage_Routine();
        StartCoroutine(StartStage);
    }

    #endregion Reset

    #region GameOver

    public bool IsGameoverCheck()
    {
        bool isGameOver = false;
        if (GetPlayerEntitys.Count <= 0 || GetPlayerEntitys.FindAll(x => x.GetCharacterStatData.IsDead is false).Count <= 0 || CurrentActionRoundCount >= MaxActionRoundCount && MaxActionRoundCount != 0)
        {
            if(GetPlayerEntitys.FindAll(x => x.GetCharacterStatData.IsZombie).Count <= 0)
            {
                OpenGameOver(false);

                for (int i = 0; i < GetMonsterEntirys.Count; ++i)
                {
                    GetMonsterEntirys[i].GetSpineModel.SetAnimation(CY_Function.SpineAnimationBinder(CY_SpineKey.Ani_Idle, 2));
                    GetMonsterEntirys[i].GetSpineModel.AddAnimation(CY_Function.SpineAnimationBinder(CY_SpineKey.Ani_Idle, 1), true);
                }

                isGameOver = true;
            }
        }

        return isGameOver;
    }

    #endregion GameOver

    #region Tutorial

    public void TutorialAction(bool isStart, int groupID)
    {
        if (isStart)
        {
            StartCoroutine(TutorialCam_Routine(groupID));
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneType.Tutorial.ToString());
            CY_SceneHandler.I.SetBattleScene();
        }
    }

    private IEnumerator TutorialCam_Routine(int groupID)
    {
        AsyncOperation async_next = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneType.Tutorial.ToString(), UnityEngine.SceneManagement.LoadSceneMode.Additive);
        yield return new WaitUntil(() => async_next.isDone);

        yield return new WaitUntil(() => CY_TutorialProcess.I.GetTutorialMain.IsInitComplete);

        CY_SceneHandler.I.SetTutorialScene();

        UserInfo.I.IsTutorialActing = true;

        // 
        CY_TutorialProcess.I.StartTutorial(groupID);
    }

    #endregion Tutorial

    #region TimeLine

    public void TimeLine_SkillAction(CY_CharacterEntity attackerEntity, List<CY_CharacterEntity> defenderEntitys, int actionIndex)
    {
        SkillAction_AttackerEntity = attackerEntity;
        SkillAction_DefenderEntitys = defenderEntitys;

        //
        DamageCount = 1;
        DamageTotal = 0;

        IsDieCharacter = false;

        //
        CY_TimeLine.TimeLineBinding(ActionPlayable, attackerEntity, defenderEntitys, actionIndex, StageCamera, StageBackground, out DamageMarkCount);

        //
        CalcCombatDamage(attackerEntity, defenderEntitys);
    }

    /// <summary>
    /// 데미지 알림
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="notification"></param>
    /// <param name="context"></param>
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        StringExtention.Log("OnNotify");

        if(PreviewSkillProcess.IsExist())
        {
            SkillData skillData = SkillAction_AttackerEntity.CurrentSkillData;

            for (int i = 0; i < SkillAction_DefenderEntitys.Count; ++i)
            {
                var defender_entity = SkillAction_DefenderEntitys[i];

                IsHealSkill = skillData.ModuleType == eModuleType.Heal || SkillAction_AttackerEntity.CurrentSkillSetupDatas.FindIndex(x => x.ModuleType == eModuleType.Heal) != -1;
                if (IsHealSkill is false)
                {
                    // Hurt Animation
                    defender_entity.GetSpineModel.SetAnimation(CY_SpineKey.Ani_Hit);
                    defender_entity.GetSpineModel.AddAnimation(CY_Function.SpineAnimationBinder(CY_SpineKey.Ani_Idle, 1), true);
                }

                CY_Function.ActionEffect((DamageMarker)notification, defender_entity.GetHitPoint);
            }
            return;
        }

        StringExtention.Log($"((DamageMarker)notification).EffectName : {((DamageMarker)notification).EffectName}");
        StringExtention.Log($"DamgeMarkCount : {DamageMarkCount}");

        // 
        int skillIndex = SkillAction_AttackerEntity.CurrentSkillActiveNumber - 1;
        int activeSkillID = SkillAction_AttackerEntity.GetActiveSkillID(skillIndex);
        List<SkillSetupData> skillSetupDatas = DataTable.I.GetSkillSetupDataList().FindAll(x => x.SkillID == activeSkillID);
        int findIndex = -1;
        for (int i = 0; i < SkillAction_DefenderEntitys.Count; ++i)
        {
            findIndex = -1;
            int damage = 0;

            if (IsAttackSkill && IsEvades[i] is false)
            {
                findIndex = SkillAction_AttackerEntity.CurrentSkillSetupDatas.FindIndex(x => x.ModuleGroup == eModuleGroup.VampiristicHeal);

                if (findIndex != -1)
                {
                    IsHealSkill = true;
                }

                // 
                DamageFontType damageFontType = DamageFontType.Attack;
                if (TargetCriticals[i])
                {
                    damageFontType = DamageFontType.Critical;
                }

                if (TargetBlocks[i])
                {
                    damageFontType = DamageFontType.Block;
                    CY_TimeLine.OnDamageStatusBlock(GetUltimateTextDamageManager, SkillAction_DefenderEntitys[i]);
                }

                CY_TimeLine.OnDamageMake(origin, notification, context, GetUltimateTextDamageManager, SkillAction_DefenderEntitys[i], DamageTotal, damageFontType);//데미지 알림

                // Hp Cal
                SkillAction_DefenderEntitys[i].GetCharacterStatData.SetHp(damage);
                SkillAction_DefenderEntitys[i].GetStageCharacterFieldInfo.UpdateStateDisplay();
            }
            else if (IsAttackSkill && IsEvades[i])
            {
                CY_TimeLine.OnDamageStatus(eModuleGroup.Miss, GetUltimateTextDamageManager, SkillAction_DefenderEntitys[i]);
            }

            if (IsHealSkill)
            {
                SkillAction_DefenderEntitys[i].GetCharacterStatData.SetHp(-damage, true);
                SkillAction_DefenderEntitys[i].GetStageCharacterFieldInfo.UpdateStateDisplay();
            }

            // 막타 확인 요소
            // 1.사망 체크
            // 2.버프, 디버프 발동.
            if (DamageCount == DamageMarkCount)
            {
                // 1.사망 체크
                if (IsAttackSkill && SkillAction_DefenderEntitys[i].GetCharacterStatData.IsDead)
                {
                    StringExtention.Log("[사망!]");
                    
                    IsDieCharacter = true;

                    // 불사 적용
                    findIndex = SkillAction_DefenderEntitys[i].GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.Zombie);
                    if(findIndex != -1 && 
                        SkillAction_DefenderEntitys[i].GetCharacterStatData.IsZombie is false &&
                        SkillAction_DefenderEntitys[i].IsUseZombie is false)
                    {
                        SkillAction_DefenderEntitys[i].GetCharacterStatData.IsZombie = true;
                        SkillAction_DefenderEntitys[i].IsUseZombie = true;
                    }
                    else
                    {
                        if(SkillAction_DefenderEntitys[i].GetCharacterStatData.IsZombie is false)
                            SkillAction_DefenderEntitys[i].DieCharacter();
                    }
                }
                
                if(IsRecoverSkill)
                {// 부활처리.
                    ActionRecover(skillSetupDatas, SkillAction_DefenderEntitys[i]);
                }
                
                if(IsUndebuffSkill)
                {// 디버프 해제 처리.
                    SkillAction_DefenderEntitys[i].GetCharacterStatData.DebuffAndCC_Clear();

                    // Effect Ones
                    for (int ii = 0; ii < skillSetupDatas.Count; ++ii)
                    {
                        SkillSetupData skillSetupData = skillSetupDatas[ii];

                        if (EffectOneModuleGroups.ContainsKey(skillSetupData.ModuleGroup))
                        {
                            CreateEffectOneModule(skillSetupData.ModuleGroup, SkillAction_DefenderEntitys[i]);
                        }
                    }
                }
                //else
                if(IsBuffDebuffCCSkill)
                {// 2.버프 발동 확인.
                    SkillAction_DefenderEntitys[i].GetStageCharacterFieldInfo.UpdateDisplayBuff();

                    // Effect Ones
                    for (int ii = 0; ii < skillSetupDatas.Count; ++ii)
                    {
                        SkillSetupData skillSetupData = skillSetupDatas[ii];
                        CreateEffectOneModule(skillSetupData.ModuleGroup, SkillAction_AttackerEntity);
                    }
                }

                // 중독 모듈 검사.
                var poisionModules = SkillAction_DefenderEntitys[i].GetCharacterStatData.SkillBuffDatas.FindAll(x => x.SkillSetupData.ModuleGroup == eModuleGroup.PoisonCount);
                if (IsAttackSkill && poisionModules.Count > 0)
                {
                    for(int ii = 0; ii < poisionModules.Count; ++ii)
                    {
                        ++poisionModules[ii].RepeatCount;

                        if(poisionModules[ii].RepeatCount >= poisionModules[ii].SkillSetupData.ArgumentList[6])
                        {
                            poisionModules[ii].RepeatCount = 0;

                            var targetEntity = SkillAction_DefenderEntitys[i];
                            AddDotBurnEffect(ref targetEntity, poisionModules[ii]);
                        }
                    }
                }

                // 수면 모듈 검사.
                findIndex = SkillAction_DefenderEntitys[i].GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.SleepCount);
                if(IsAttackSkill && findIndex != -1)
                {// 모듈이 있다면 삭제(피격후 삭제)
                    SkillAction_DefenderEntitys[i].GetCharacterStatData.SkillBuffDatas.RemoveAt(findIndex);
                }
            }
        } // For End

        if (DamageCount == DamageMarkCount)
        {
            // Count형 버프(순서 중요)
            SkillAction_AttackerEntity.GetStageCharacterFieldInfo.UpdateStartCountBuff();

            // 
            {
                for (int ii = 0; ii < GetStageCharacterFieldInfoPlayers.Count; ++ii)
                    GetStageCharacterFieldInfoPlayers[ii].UpdateStartActionRoundBuff();

                for (int ii = 0; ii < GetStageCharacterFieldInfoMonsters.Count; ++ii)
                    GetStageCharacterFieldInfoMonsters[ii].UpdateStartActionRoundBuff();

                UpdateActionRound();
            }

            // 실제 모듈 적용하는 함수.
            if(IsHealSkill is false)
                SkillAction_AttackerEntity.ActionBuffModule();
            else
                SkillAction_AttackerEntity.ActionBuffModule(dotHealValue: DamageTotal);

            // 데미지 상태 폰트 출력하는 구간.
            if (IsBuffDebuffCCSkill)
            {
                for (int i = 0; i < SkillAction_DefenderEntitys.Count; ++i)
                {
                    // 데미지상태 출력.
                    for (int ii = 0; ii < skillSetupDatas.Count; ++ii)
                    {
                        SkillSetupData skillSetupData = skillSetupDatas[ii];

                        if (skillSetupData.ModuleType == eModuleType.None ||
                            skillSetupData.ModuleType == eModuleType.Attack ||
                            skillSetupData.ModuleType == eModuleType.AttackBuff ||
                            skillSetupData.ModuleType == eModuleType.Heal)
                            continue;

                        if (skillSetupData.TargetType == eTargetType.Me)
                        {
                            CY_TimeLine.OnDamageStatus(skillSetupData, GetUltimateTextDamageManager, SkillAction_AttackerEntity);
                        }
                        else
                        {
                            CY_TimeLine.OnDamageStatus(skillSetupData, GetUltimateTextDamageManager, SkillAction_DefenderEntitys[i]);
                        }
                    }
                }
            }

            //
            for (int i = 0; i < SkillAction_DefenderEntitys.Count; ++i)
            {
                SkillAction_DefenderEntitys[i].GetStageCharacterFieldInfo.UpdateDisplayBuff();
            }
            SkillAction_AttackerEntity.GetStageCharacterFieldInfo.UpdateDisplayBuff();
        }
        
        ++DamageCount;
    }

    public void StartDieAction()
    {
        if(ScriptableManager.I.GetStage_ScriptableObject.IsCharacterDieAction)
        {
            if(DieRoutine == null)
            {
                DieRoutine = DieAction_Routine();
                StartCoroutine(DieRoutine);
            }
        }
    }

    private IEnumerator DieAction_Routine()
    {
        float shakeTime = ScriptableManager.I.GetStage_ScriptableObject.StageCameraShakeTime;
        float shakePower = ScriptableManager.I.GetStage_ScriptableObject.StageCameraShakePower;
        float slowTime = ScriptableManager.I.GetStage_ScriptableObject.StageCameraSlowTime;
        StageCameraShakeType shakeType = ScriptableManager.I.GetStage_ScriptableObject.StageCameraShakeType;

        CameraShakeController.StartShake(shakeType, shakeTime, shakePower);
        if (UserInfo.I.CameraSlowEnable == 0)
        {
            yield break;
        }
        CY_Function.SetTime(slowTime);
        yield return new WaitForSeconds(shakeTime);
        StageMain.UpdateTime(UserInfo.I.GetCombatSpeedIndex(UserInfo.I.CurrentTeamIndex));

        DieRoutine = null;
    }


    /// <summary>
    /// 부활 기능을 수행하는 함수입니다.
    /// </summary>
    /// <param name="skillSetupDatas"></param>
    /// <param name="dieCharacterEntity">StageProcess</param>
    private void ActionRecover(List<SkillSetupData> skillSetupDatas, CY_CharacterEntity dieCharacterEntity)
    {
        CY_CharacterEntity recover = ReSpawnCharacter_Recover(dieCharacterEntity);

        // Effect Ones
        {
            for (int ii = 0; ii < skillSetupDatas.Count; ++ii)
            {
                SkillSetupData skillSetupData = skillSetupDatas[ii];
                CreateEffectOneModule(skillSetupData.ModuleGroup, recover);
            }
        }
    }

    protected virtual TimelineAsset SetPlayableAsset(TimelineAsset asset)
    {
        TimelineAsset Clone = asset;
        ActionPlayable.playableAsset = Clone;
        return Clone;
    }

    public void OnPlayableDirectorStopped(PlayableDirector aDirector)
    {
        StringExtention.Log("OnPlayableDirectorStopped");

        SkillAction_AttackerEntity.GetSpineModel.SetAnimation(CY_Function.SpineAnimationBinder(CY_SpineKey.Ani_Idle, 1));
        SkillAction_AttackerEntity.IsActing = false;
    }

    public void StopDieRoutine()
    {
        if (DieRoutine != null)
        {
            StopCoroutine(DieRoutine);
            DieRoutine = null;
        }
    }
    #endregion TimeLine
}
