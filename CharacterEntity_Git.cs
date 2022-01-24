using funLAB.SpineModel;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Spine.Unity;
using NodeCanvas.BehaviourTrees;
using fnLAB_UI;

public class CharacterEntity : CharacterEntityBase, ICharacterStat
{
    #region TimeLine Variable

    [SerializeField, GetComponentInChildrenName("SkeletonAnimation")]
    private SkeletonAnimation CharacterSkeleton;
    public Transform GetCharacterSkeleton_Transform { get { return CharacterSkeleton.transform; } }

    [SerializeField, GetComponent]
    private PlayableDirector ActionPlayable;

    [SerializeField, GetComponentInChildrenName("Character")]
    private Animator CharacterAnimator;

    [SerializeField, GetComponentInChildrenName("Projectile")]
    private GameObject ProjectileGameObject;
    private GameObject[] ProjectileGameObjects = new GameObject[10];

    [SerializeField, GetComponentInChildrenName("LocalEffectPoint")]
    private GameObject LocalEffectPoint;
    

    [SerializeField, GetComponentInChildrenName("Character")]
    private Transform CharacterFieldInfoTrans;
    public Transform GetCharacterFieldInfoTrans { get { return CharacterFieldInfoTrans; } }

    private GameObject[] BoneEffects;

    [SerializeField, GetComponentInChildrenName("Casting0")]
    private GameObject BoneEffect0;
    [SerializeField, GetComponentInChildrenName("Casting1")]
    private GameObject BoneEffect1;
    [SerializeField, GetComponentInChildrenName("Casting2")]
    private GameObject BoneEffect2;
    [SerializeField, GetComponentInChildrenName("Casting3")]
    private GameObject BoneEffect3;
    [SerializeField, GetComponentInChildrenName("Casting4")]
    private GameObject BoneEffect4;
    [SerializeField, GetComponentInChildrenName("Casting5")]
    private GameObject BoneEffect5;

    public float CharacterSize = 1f;
    public LocationType InLocationType { get; private set; }

    [HideInInspector]
    Vector3[] TargetPos = new Vector3[3];// 0: 로컬 1: 히트 포이트 2: 라인 위치

    [SerializeField, GetComponentInChildren]
    private TransformManipulation[] ManipulationS;

#if UNITY_EDITOR
    double time = 0;
    bool IsTool = false;
    public bool ToolState { set { IsTool = value; CharacterSkeleton.UpdateState = !value; } }
#endif
    #endregion TimeLine Variable

    #region AI

    [SerializeField, GetComponent]
    private BehaviourTreeOwner AI;

    #endregion AI 

    #region Property
    // Data
    /// <summary>
    /// [Table Data] HeroData
    /// </summary>
    private HeroData HeroData { get; set; } = null;
    public HeroData GetHeroData { get { return HeroData; } }
    //HeroIconData IconData;
    /// <summary>
    /// 보유 영웅 Data
    /// </summary>
    public CY_MyHero MyHeroData { get; private set; } = null;

    /// <summary>
    /// 액티브 스킬 ID를 얻는 함수.
    /// </summary>
    /// <param name="skillIndex"></param>
    /// <returns></returns>
    public int GetActiveSkillID(int skillIndex)
    {
        int activeSkillID = 0;
        if(IsNPC)
            activeSkillID = HeroData.ActiveSkillIDList[skillIndex];
        else
            activeSkillID = MyHeroData.IsExist() ? MyHeroData.EnhanceActiveSkillIDList[skillIndex] : HeroData.ActiveSkillIDList[skillIndex];
        return activeSkillID;
    }

    /// <summary>
    /// 액티브 스킬 최대 개수 반환.
    /// </summary>
    public int GetActiveSkillMaxCount { get { return HeroData.ActiveSkillIDList.Count; } }

    /// <summary>
    /// 패시브 스킬 ID 얻어오는 함수.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public int GetPassiveSkillID(int index)
    {
        int passiveSkillID = 0;
        if (IsNPC)
            passiveSkillID = HeroData.PassiveSkillIDList[index];
        else
            passiveSkillID = MyHeroData.IsExist() ? MyHeroData.EnhancePassiveSkillIDLis[index] : HeroData.PassiveSkillIDList[index];
        return passiveSkillID;
    }

    /// <summary>
    /// 패시브 스킬 최대개수
    /// </summary>
    public int GetPassiveSkillMaxCount { get { return HeroData.PassiveSkillIDList.Count; } }

    /// <summary>
    /// [Table Data] HeroIconData
    /// </summary>
    //public HeroIconData HeroIconData { get; private set; } = null;

    /// <summary>
    /// [Table Data] NPCInfoData
    /// </summary>
    public NPCInfoData NPCInfoData { get; private set; } = null;

    /// <summary>
    /// [Table Data] BotInfoData
    /// </summary>
    public BotInfoData BotInfoData { get; private set; } = null;

    private CharacterData CharacterStatData = new CharacterData();
    public CharacterData GetCharacterStatData { get { return CharacterStatData; } }

    /// <summary>
    /// 생성된 로케이션의 포인트(Index)
    /// </summary>
    private int LocationPoint = 0;
    public int GetLocationPoint { get { return LocationPoint; } }

    /// <summary>
    /// CharacterEntity가 NPC인지(몬스터인지) 구분하는 변수.
    /// </summary>
    public bool IsNPC { get; private set; } = false;
    /// <summary>
    /// 사망시 데이터 보관용일때 사용
    /// </summary>
    /// <param name="isNPC"></param>
    public void SetNPC(bool isNPC)
    {
        IsNPC = isNPC;
    }

    #endregion Property

    #region Property_Combat

    /// <summary>
    /// 스킬 연출중인지 아닌지를 판단하는 변수.(주로 AI로직에서 처리가 될 예정임)
    /// </summary>
    public bool IsActing { get; set; } = false;

    /// <summary>
    /// Active3번 스킬 자동 시전 상태값.
    /// </summary>
    public bool IsAutoSkill { get; set; } = false;

    /// <summary>
    /// 도트 데미지 연출중인지 상태를 관리하는 변수.
    /// </summary>
    public bool IsDOTCheck { get; set; } = false;

    /// <summary>
    /// 현재 사용하는 액티브 스킬의 번호
    /// </summary>
    public int CurrentSkillActiveNumber { get; set; } = 0;

    /// <summary>
    /// 현재 사용하는 스킬 데이터
    /// </summary>
    public SkillData CurrentSkillData { get; set; } = null;

    /// <summary>
    /// 현재 사용하는 스킬 모듈.
    /// </summary>
    public List<SkillSetupData> CurrentSkillSetupDatas { get; set; } = new List<SkillSetupData>();

    /// <summary>
    /// 연출에 적용될 대상들
    /// </summary>
    public List<CY_CharacterEntity> CombatTargets { get; private set; } = new List<CY_CharacterEntity>();

    /// <summary>
    /// 타겟들중 각 타겟의 정보의 정보를 관리.
    /// </summary>
    public CY_CharacterEntity TargetCharacter { get; private set; } = null;

    public int MaxDamage_History { get; private set; }

    /// <summary>
    /// 캐릭터 턴 획득 정보.
    /// </summary>
    public StageCharacterTurn GetStageCharacterTurn { get; private set; } = null;
    public void SetStageCharacterTurn(StageCharacterTurn stageCharacterTurn)
    {
        GetStageCharacterTurn = stageCharacterTurn;
    }
    
    public StageCharacterFieldInfo1 GetStageCharacterFieldInfo { get; private set; } = null;
    public void SetStageCharacterFieldInfo(StageCharacterFieldInfo1 stageCharacterFieldInfo)
    {
        GetStageCharacterFieldInfo = stageCharacterFieldInfo;
    }

    /// <summary>
    /// 전투 연출후 TimeLine데이터 비우기.
    /// </summary>
    public void Clear_TimeLine()
    {
        ActionPlayable.playableAsset = null;
    }

    /// <summary>
    /// 버프 아우라
    /// </summary>
    public EffectController BuffAura { get; set; } = null;
    /// <summary>
    /// 버프 아우라를 반환시킵니다.
    /// </summary>
    public void ClearBuffAura()
    {
        if(BuffAura.IsExist())
        {
            BuffAura.Recycle();
            BuffAura = null;
        }
    }

    public void SetBuffAura(EffectController effectController)
    {
        BuffAura = effectController;
    }

    /// <summary>
    /// 디버프 아우라
    /// </summary>
    public EffectController DebuffAura { get; set; } = null;
    /// <summary>
    /// 디버프 아우라를 반환시킵니다.
    /// </summary>
    public void ClearDebuffAura()
    {
        if (DebuffAura.IsExist())
        {
            DebuffAura.Recycle();
            DebuffAura = null;
        }
    }

    public void SetDebuffAura(EffectController effectController)
    {
        DebuffAura = effectController;
    }

    /// <summary>
    /// CC Loop Effect Group
    /// </summary>
    private EnumDictionary<eModuleGroup, EffectController> EffectCCLoopModuleGroups = new EnumDictionary<eModuleGroup, EffectController>();

    public bool GetEffectCCLoopExist(eModuleGroup moduleGroup)
    {
        if (EffectCCLoopModuleGroups.ContainsKey(moduleGroup) is false)
            return false;

        return EffectCCLoopModuleGroups[moduleGroup].IsExist();
    }

    public void RemoveEffectCCLoop(eModuleGroup moduleGroup)
    {
        if(EffectCCLoopModuleGroups[moduleGroup].IsExist())
        {
            EffectCCLoopModuleGroups[moduleGroup].Recycle();
        }

        EffectCCLoopModuleGroups[moduleGroup] = null;
    }

    /// <summary>
    /// EffectCC Loop 객체 생성 및 설정.
    /// </summary>
    /// <param name="moduleGroup"></param>
    /// <param name="orignalEffectController">메모리 상에 있는것이기 때문에 리스폰을 다시해서 사용해야 함</param>
    public void SetEffectCCLoop(eModuleGroup moduleGroup, EffectController orignalEffectController)
    {
        // effectController
        EffectController newEffectCC = CreateEffectCC(orignalEffectController);
        EffectCCLoopModuleGroups[moduleGroup] = newEffectCC;
    }

    private EffectController CreateEffectCC(EffectController orignalEffectController)
    {
        Transform parent = null;
        switch (orignalEffectController.GetEffectPositionType)
        {
            case EffectPositionType.Bottom:
                parent = GetRoot;
                break;

            case EffectPositionType.Top:
                parent = GetHead;
                break;

            default:
                parent = GetHitPoint;
                break;
        }

        return orignalEffectController.Spawn(parent);
    }

    /// <summary>
    /// 부활 이력 관리
    /// </summary>
    public bool IsUseRecover { get; set; } = false;
    /// <summary>
    /// 불사 이력 관리
    /// </summary>
    public bool IsUseZombie { get; set; } = false;

    #endregion Property_Combat

    private void OnEnable()
    {
        if (GetCharacterStatData.IsDead && gameObject.GetActive())
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }

    protected override void Awake()
    {
        base.Awake();
        BoneEffects = new GameObject[6];
        BoneEffects[0] = BoneEffect0;
        BoneEffects[1] = BoneEffect1;
        BoneEffects[2] = BoneEffect2;
        BoneEffects[3] = BoneEffect3;
        BoneEffects[4] = BoneEffect4;
        BoneEffects[5] = BoneEffect5;

        TransformManipulation[] list = GetComponentsInChildren<TransformManipulation>();
        foreach(TransformManipulation Item in list)
        {
            Item.SetEndPos(TargetPos);
                
            int index = CY_Function.StringExtractInteger(Item.name);
            if(index > 0)
            {
                ProjectileGameObjects[index - 1] = Item.gameObject;
            }
        }
    }

    /// <summary>
    /// 영웅 스탯 메모리 할당
    /// </summary>
    private void InitStats()
    {
        // 스탯 설정.
        for (eHeroStatus heroStatus = eHeroStatus.None + 1; heroStatus < eHeroStatus.Skill; ++heroStatus)
        {
            if (CharacterStatData.HeroStats.ContainsKey(heroStatus) is false)
                CharacterStatData.HeroStats.Add(heroStatus, new CharacterStatData(heroStatus));
            else
                CharacterStatData.HeroStats[heroStatus] = new CharacterStatData(heroStatus);
        }
    }

    /// <summary>
    /// CC Effect 모듈 등록.
    /// </summary>
    private void InitCCEffectModule()
    {
        // CC : 5000번대
        for (eModuleGroup e = eModuleGroup.StunCount; e <= eModuleGroup.SleepCount; ++e)
        {
            if(EffectCCLoopModuleGroups.ContainsKey(e) is false)
            {
                EffectCCLoopModuleGroups.Add(e, null);
            }
            else
            {
                if(EffectCCLoopModuleGroups[e].IsExist())
                    EffectCCLoopModuleGroups[e].Recycle();

                EffectCCLoopModuleGroups[e] = null;
            }
        }
    }

    public void ClearEntityData()
    {
        // 
        CombatTargets.Clear();
        TargetCharacter = null;
        GetStageCharacterTurn = null;
        GetStageCharacterFieldInfo = null;
        CurrentSkillActiveNumber = 0;
        CurrentSkillData = null;
        CurrentSkillSetupDatas.Clear();
        IsActing = false;
        IsDOTCheck = false;
        IsNPC = false;
        LocationPoint = 0;

        HeroData = null;
        MyHeroData = null;
        NPCInfoData = null;

        IsUseRecover = false;
        IsUseZombie = false;

        ClearBuffAura();
        ClearDebuffAura();

        InitCCEffectModule();

        // 
        CharacterStatData.ResetData();

        // 
        Clear_TimeLine();

        //
        SpineModel.SetAnimation(CY_Function.SpineAnimationBinder(CY_SpineKey.Ani_Idle, 1));
    }

    #region Data(Table, Stat, ETC.)

    /// <summary>
    /// 캐릭터의 체력등 Stat을 설정하는 함수.
    /// </summary>
    public void InitState(bool isNPC)
    {
        // 
        InitStats();
        InitCCEffectModule();

        // 
        IsNPC = isNPC;
        GetCharacterStatData.IsNPC = isNPC;
        GetCharacterStatData.IsDead = false;
        GetCharacterStatData.IsZombie = false;
        GetCharacterStatData.BaseCharacterEntity = this;

        //
        transform.localRotation = isNPC ? Quaternion.Euler(new Vector3(0f, -180f, 0f)): Quaternion.Euler(Vector3.zero);

        // 각성 버블의 수
        CharacterStatData.BeadCount = 0;
        CharacterStatData.BeadMaxCount = HeroData.AwakeSkillCost;

        // 튜토리얼시 데이터 설정.
        if(UserInfo.I.IsFirstMainScenarioStage())
        {
            if (isNPC is false)
            {
                CharacterStatData.BeadCount = CharacterStatData.BeadMaxCount - 1;
            }
        }

        // HeroStaus
        {
            // Attack
            int setAttack = isNPC is false ? InitHeroLobbyStat(eHeroStatus.Attack).Int : InitMonsterStat(eHeroStatus.Attack).Int;// NPCInfoData.StatusInfo.Attack;
            CharacterStatData.HeroStats[eHeroStatus.Attack].SetValue(setAttack);

            // PhysicalDefense
            int setPhysicalDefense = isNPC is false ? InitHeroLobbyStat(eHeroStatus.PhysicalDefense).Int : InitMonsterStat(eHeroStatus.PhysicalDefense).Int;//NPCInfoData.StatusInfo.PhysicalDefense;
            CharacterStatData.HeroStats[eHeroStatus.PhysicalDefense].SetValue(setPhysicalDefense);

            // MagicalDefense
            int setMagicalDefense = isNPC is false ? InitHeroLobbyStat(eHeroStatus.MagicalDefense).Int : InitMonsterStat(eHeroStatus.MagicalDefense).Int; //NPCInfoData.StatusInfo.MagicalDefense;
            CharacterStatData.HeroStats[eHeroStatus.MagicalDefense].SetValue(setMagicalDefense);

            // HP
            int setHp = isNPC is false ? InitHeroLobbyStat(eHeroStatus.HP).Int : InitMonsterStat(eHeroStatus.HP).Int;//NPCInfoData.StatusInfo.HP;
            CharacterStatData.HeroStats[eHeroStatus.HP].SetValue(setHp);
            CharacterStatData.HpMax = setHp;

            // CriticalRate
            float setCriticalRate = isNPC is false ? InitHeroLobbyStat(eHeroStatus.CriticalRate).Float : InitMonsterStat(eHeroStatus.CriticalRate).Float; //NPCInfoData.StatusInfo.CriticalRate;
            CharacterStatData.HeroStats[eHeroStatus.CriticalRate].SetValue(setCriticalRate);

            // Critical
            float setCritical = isNPC is false ? InitHeroLobbyStat(eHeroStatus.Critical).Float : InitMonsterStat(eHeroStatus.Critical).Float; // NPCInfoData.StatusInfo.Critical;
            CharacterStatData.HeroStats[eHeroStatus.Critical].SetValue(setCritical);

            // Speed
            int setSpeed = isNPC is false ? InitHeroLobbyStat(eHeroStatus.Speed).Int : InitMonsterStat(eHeroStatus.Speed).Int; //NPCInfoData.StatusInfo.Speed;
            CharacterStatData.HeroStats[eHeroStatus.Speed].SetValue(setSpeed);

            // Special
            int setSpecial = isNPC is false ? InitHeroLobbyStat(eHeroStatus.Special).Int : InitMonsterStat(eHeroStatus.Special).Int; // NPCInfoData.StatusInfo.Special;
            CharacterStatData.HeroStats[eHeroStatus.Special].SetValue(setSpecial);

            // ResistanceUp
            float setResistanceUp = isNPC is false ? InitHeroLobbyStat(eHeroStatus.ResistanceUp).Float : InitMonsterStat(eHeroStatus.ResistanceUp).Int; //NPCInfoData.StatusInfo.ResistanceUp;
            CharacterStatData.HeroStats[eHeroStatus.ResistanceUp].SetValue(setResistanceUp);

            // Block
            float setBlock = isNPC is false ? InitHeroLobbyStat(eHeroStatus.Block).Float : InitMonsterStat(eHeroStatus.Block).Int;//NPCInfoData.StatusInfo.Block;
            CharacterStatData.HeroStats[eHeroStatus.Block].SetValue(setBlock);

            // AP
            int setAp = GetStageStartAp(UserInfo.I.CurrentBattleConditionData);

            CharacterStatData.HeroStats[eHeroStatus.AP].SetValue(setAp);
            CharacterStatData.ApMax = HeroData.ApMax;
        }
    }

    /// <summary>
    /// 캐릭터 생성시 로비정보용 스탯을 산정하는 함수(초기화용) - 아군 전용
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    private CharacterStatData InitHeroLobbyStat(eHeroStatus status)
    {
        CharacterStatData CharacterStatData = new CharacterStatData(status);
        
        switch (status)
        {
            case eHeroStatus.Attack:
                {
                    int attack =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.Attack : HeroData.StatusInfo.Attack;
                    CharacterStatData.SetValue(attack);
                }
                break;

            case eHeroStatus.PhysicalDefense:
                {
                    int physicalDefense =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.PhysicalDefense : HeroData.StatusInfo.PhysicalDefense;
                    CharacterStatData.SetValue(physicalDefense);
                }
                break;

            case eHeroStatus.MagicalDefense:
                {
                    int magicalDefense =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.MagicalDefense : HeroData.StatusInfo.MagicalDefense;
                    CharacterStatData.SetValue(magicalDefense);
                }
                break;

            case eHeroStatus.HP:
                {
                    int hp =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.HP : HeroData.StatusInfo.HP;
                    CharacterStatData.SetValue(hp);
                }
                break;

            case eHeroStatus.Critical:
                {
                    float critical =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.Critical : HeroData.StatusInfo.Critical;
                    CharacterStatData.SetValue(critical);
                }
                break;

            case eHeroStatus.CriticalRate:
                {
                    float criticalRate =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.CriticalRate : HeroData.StatusInfo.CriticalRate;
                    CharacterStatData.SetValue(criticalRate);
                }
                break;

            case eHeroStatus.Speed:
                {
                    int speed =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.Speed : HeroData.StatusInfo.Speed;
                    CharacterStatData.SetValue(speed);
                }
                break;

            case eHeroStatus.Special:
                {
                    int special =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.Special : HeroData.StatusInfo.Special;
                    CharacterStatData.SetValue(special);
                }
                break;

            case eHeroStatus.ResistanceUp:
                {
                    float resistanceUp =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.ResistanceUp : HeroData.StatusInfo.ResistanceUp;
                    CharacterStatData.SetValue(resistanceUp);
                }
                break;

            case eHeroStatus.Block:
                {
                    float block =
                      /*기본 + 레벨업 + 한계돌파 + 장비 + 보석 + 숙련도 + 속성강화*/
                      MyHeroData.IsExist() ? MyHeroData.StatusInfo.Block : HeroData.StatusInfo.Block;
                    CharacterStatData.SetValue(block);
                }
                break;

            case eHeroStatus.AP:
            case eHeroStatus.Skill:
                StringExtention.Log($"초기화가 불가능한 Stat {status}입니다.");
                break;
        }

        return CharacterStatData;
    }

    private CharacterStatData InitMonsterStat(eHeroStatus status)
    {
        CharacterStatData CharacterStatData = new CharacterStatData(status);

        switch (status)
        {
            case eHeroStatus.Attack:
                {
                    int attack = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.Attack : BotInfoData.StatusInfo.Attack;
                    CharacterStatData.SetValue(attack);
                }
                break;

            case eHeroStatus.PhysicalDefense:
                {
                    int physicalDefense = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.PhysicalDefense : BotInfoData.StatusInfo.PhysicalDefense;
                    CharacterStatData.SetValue(physicalDefense);
                }
                break;

            case eHeroStatus.MagicalDefense:
                {
                    int magicalDefense = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.MagicalDefense : BotInfoData.StatusInfo.MagicalDefense;
                    CharacterStatData.SetValue(magicalDefense);
                }
                break;

            case eHeroStatus.HP:
                {
                    int hp = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.HP : BotInfoData.StatusInfo.HP;
                    CharacterStatData.SetValue(hp);
                }
                break;

            case eHeroStatus.Critical:
                {
                    float critical = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.Critical : BotInfoData.StatusInfo.Critical;
                    CharacterStatData.SetValue(critical);
                }
                break;

            case eHeroStatus.CriticalRate:
                {
                    float criticalRate = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.CriticalRate : BotInfoData.StatusInfo.CriticalRate;
                    CharacterStatData.SetValue(criticalRate);
                }
                break;

            case eHeroStatus.Speed:
                {
                    int npcSpeed = 0;
                    if(NPCInfoData.IsExist())
                    {
                        var heroData = DataTable.I.GetHeroData(NPCInfoData.HeroID);
                        npcSpeed = NPCInfoData.StatusInfo.Speed + heroData.StatusInfo.Speed;
                    }

                    int speed = NPCInfoData.IsExist() ? npcSpeed : BotInfoData.StatusInfo.Speed;
                    CharacterStatData.SetValue(speed);
                }
                break;

            case eHeroStatus.Special:
                {
                    int special = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.Special : BotInfoData.StatusInfo.Special;
                    CharacterStatData.SetValue(special);
                }
                break;

            case eHeroStatus.ResistanceUp:
                {
                    float resistanceUp = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.ResistanceUp : BotInfoData.StatusInfo.ResistanceUp;
                    CharacterStatData.SetValue(resistanceUp);
                }
                break;

            case eHeroStatus.Block:
                {
                    float block = NPCInfoData.IsExist() ? NPCInfoData.StatusInfo.Block : BotInfoData.StatusInfo.Block;
                    CharacterStatData.SetValue(block);
                }
                break;

            case eHeroStatus.AP:
            case eHeroStatus.Skill:
                StringExtention.Log($"초기화가 불가능한 Stat {status}입니다.");
                break;
        }

        return CharacterStatData;
    }

    /// <summary>
    /// HeroGroup Buff Stat추가하는 함수.
    /// </summary>
    /// <param name="status"></param>
    public void SetGroupBuffStat(List<HeroGroupBuffEffect> heroGroupBuffEffects)
    {
        for(int i = 0; i < heroGroupBuffEffects.Count; ++i)
        {
            eHeroStatus heroStatus = heroGroupBuffEffects[i].AddOptionData.StatusType;

            if(heroStatus == eHeroStatus.Speed)
            {
                // 수치
                float calcValue = heroGroupBuffEffects[i].AddOptionData.StatusValuePlus;
                if (calcValue == 0f)
                    continue;

                float newValue = CharacterStatData.HeroStats[heroStatus].Float + calcValue;

                CharacterStatData.HeroStats[heroStatus].SetValue(newValue);
            }
            else
            {
                // 퍼센트
                float calcValue = heroGroupBuffEffects[i].AddOptionData.StatusValueMultiply;
                if (calcValue == 0f)
                    continue;

                float value = CharacterStatData.HeroStats[heroStatus].Float * CY_Function.ChangePercentage(calcValue);
                float newValue = CharacterStatData.HeroStats[heroStatus].Float + value;

                CharacterStatData.HeroStats[heroStatus].SetValue(newValue);

                if (heroStatus == eHeroStatus.HP)
                    CharacterStatData.HpMax = (int)newValue;
            }
        }
    }

    /// <summary>
    /// 아군 생성될때 테이블 데이터를 설정하는 함수.
    /// </summary>
    /// <param name="heroUniqueID"></param>
    public void SetTableData(int heroUniqueID, SpineSkinType spineSkin, int locationPoint, Transform trans, bool isPreview = false)
    {
        // 테이블 데이터 설정
        HeroData = DataTable.I.GetHeroData(heroUniqueID);
        MyHeroData = isPreview is false ? UserInfo.I.GetMyHero(heroUniqueID) : null;
        NPCInfoData = null;
        SpineModel.SetSpineSkin(spineSkin);

        //
        LocationPoint = locationPoint;
        SpineRenderSorter?.SetParentTrans(trans);

        // 테이블 데이터 반영한 스탯 정보
        InitState(false);
    }

    /// <summary>
    /// 적군 생성될때 테이블 데이터를 설정하는 함수.
    /// </summary>
    /// <param name="heroUniqueID"></param>
    public void SetTableData(int heroUniqueID, NPCInfoData nPCInfoData, SpineSkinType spineSkin, int locationPoint, Transform trans)
    {
        // 테이블 데이터 설정
        HeroData = DataTable.I.GetHeroData(heroUniqueID);
        MyHeroData = null;
        NPCInfoData = nPCInfoData;
        SpineModel.SetSpineSkin(spineSkin);

        //
        LocationPoint = locationPoint;
        SpineRenderSorter.SetParentTrans(trans);

        // 테이블 데이터 반영한 스탯 정보
        InitState(true);
    }

    /// <summary>
    /// 봇 생성될때 테이블 데이터를 설정하는 함수.
    /// </summary>
    /// <param name="heroUniqueID"></param>
    public void SetTableData(int heroUniqueID, BotInfoData botInfoData, SpineSkinType spineSkin, int locationPoint, Transform trans)
    {
        // 테이블 데이터 설정
        HeroData = DataTable.I.GetHeroData(heroUniqueID);
        MyHeroData = null;
        NPCInfoData = null;
        BotInfoData = botInfoData;
        SpineModel.SetSpineSkin(spineSkin);

        //
        LocationPoint = locationPoint;
        SpineRenderSorter.SetParentTrans(trans);

        // 테이블 데이터 반영한 스탯 정보
        InitState(true);
    }

    private int GetStageStartAp(BattleConditionData battleConditionData)
    {
        if (battleConditionData.IsNull())
            return 0;

        if (battleConditionData.IsExist())
            return battleConditionData.StartAP;
        else
            return 0;
    }

    #endregion Data(Table, Stat, ETC.)

    #region Combat

    // 
    public float GetTurnTime()
    {
        float time = 200f / GetCombatHeroStat(eHeroStatus.Speed).Float;
        return time;
    }

    public void DieCharacter()
    {
        // 죽음 애니메이션.
        SpineModel.SetAnimation(CY_SpineKey.Ani_Die, false);

        // 죽음 후 처리.
        StartCoroutine(DieCharacter_Routine());
    }

    private IEnumerator DieCharacter_Routine()
    {
        StringExtention.Log($"SpineModel.GetAnimationEndTime() : {SpineModel.GetAnimationEndTime()}");

        //
        CY_StageProcess.I.StartDieAction();

        yield return new WaitForSeconds(SpineModel.GetAnimationEndTime());

        //
        CY_StageProcess.I.DieCharacter(IsNPC, this);
    }

    /// <summary>
    /// 전투에 실제 사용되는 스탯정보(버프 등의 효과가 모두 적용된 스탯) - SkillSetup에 따라 값이 변경됨.
    /// </summary>
    /// <param name="heroStatus"></param>
    /// <returns></returns>
    public CharacterStatData GetCombatHeroStat(eHeroStatus heroStatus, CY_CharacterEntity target = null, bool isDot = false)
    {
        int skillIndex = CurrentSkillActiveNumber - 1;
        if(skillIndex < 0)
            skillIndex = 0;

        //
        List<SkillSetupData> skillSetupDatas = CurrentSkillSetupDatas;

        int findDamageRateModuleIndex = skillSetupDatas.FindIndex(x => x.ModuleGroup == eModuleGroup.DamageRate);
        bool isDamageRateModule = findDamageRateModuleIndex != -1;

        //
        int findMagicalDamageRateModuleIndex = skillSetupDatas.FindIndex(x => x.ModuleGroup == eModuleGroup.MagicalDamageRate);
        bool isMagicalDamageRateModule = findMagicalDamageRateModuleIndex != -1;

        // 
        CharacterStatData characterStatData = new CharacterStatData(heroStatus);

        switch (heroStatus)
        {
            case eHeroStatus.Attack:
                {
                    // 기본 공격력
                    int attackDefault = CharacterStatData.HeroStats[heroStatus].Int;

                    // 버프 수치
                    float allBuffAttack = 0f;
                    {
                        // 공격력 증가
                        SkillSetupData attackUpRateSkillSetupData = GetSkillModuleGroup(eModuleGroup.AttackUpRate, isDamageRateModule, isMagicalDamageRateModule);
                        float attackUpRateValue = GetSkillModuleArgumentValue(attackUpRateSkillSetupData);

                        // 물리공격력 증가
                        SkillSetupData damageUpRateSkillSetupData = GetSkillModuleGroup(eModuleGroup.DamageUpRate, isDamageRateModule, isMagicalDamageRateModule);
                        float damageUpRateValue = GetSkillModuleArgumentValue(damageUpRateSkillSetupData);

                        // 마법공격력 증가
                        SkillSetupData magicalDamageUpRateSkillSetupData = GetSkillModuleGroup(eModuleGroup.MagicalDamageUpRate, isDamageRateModule, isMagicalDamageRateModule);
                        float magicalDamageUpRateValue = GetSkillModuleArgumentValue(magicalDamageUpRateSkillSetupData);

                        allBuffAttack = (attackUpRateSkillSetupData.IsExist() ? attackUpRateValue : 0f) +
                            (damageUpRateSkillSetupData.IsExist() ? damageUpRateValue : 0f) +
                            (magicalDamageUpRateSkillSetupData.IsExist() ? magicalDamageUpRateValue : 0f);
                    }

                    // 디버프 수치
                    float allDeBuffAttack = 0f;
                    {
                        // 시전자 기준으로 Value값을 산정함.

                        // 공격력 감소
                        SkillSetupData attackDownRateSkillSetupData = GetSkillModuleGroup(eModuleGroup.AttackDownRate, isDamageRateModule, isMagicalDamageRateModule);
                        float attackDownRateValue = 0f;
                        if (attackDownRateSkillSetupData.IsExist())
                        {
                            CY_CharacterEntity actorEntity = GetSkillModuleActorEntity(attackDownRateSkillSetupData, target);
                            attackDownRateValue = GetSkillModuleArgumentValue(attackDownRateSkillSetupData, true, actorEntity);
                        }

                        // 물리공격력 감소
                        SkillSetupData damageDownRateSkillSetupData = GetSkillModuleGroup(eModuleGroup.DamageDownRate, isDamageRateModule, isMagicalDamageRateModule);
                        float damageDownRateValue = 0f;
                        if(damageDownRateSkillSetupData.IsExist())
                        {
                            CY_CharacterEntity actorEntity = GetSkillModuleActorEntity(damageDownRateSkillSetupData, target);
                            damageDownRateValue = GetSkillModuleArgumentValue(damageDownRateSkillSetupData, true, actorEntity);
                        }

                        // 마법공격력 감소
                        SkillSetupData magicalDamageDownRateSkillSetupData = GetSkillModuleGroup(eModuleGroup.MagicalDamageDownRate, isDamageRateModule, isMagicalDamageRateModule);
                        float magicalDamageDownRateValue = 0f;
                        if(magicalDamageDownRateSkillSetupData.IsExist())
                        {
                            CY_CharacterEntity actorEntity = GetSkillModuleActorEntity(magicalDamageDownRateSkillSetupData, target);
                            magicalDamageDownRateValue = GetSkillModuleArgumentValue(magicalDamageDownRateSkillSetupData, true, actorEntity);
                        }

                        allDeBuffAttack = (attackDownRateSkillSetupData.IsExist() ? attackDownRateValue : 0f) +
                            (damageDownRateSkillSetupData.IsExist() ? damageDownRateValue : 0f) +
                            (magicalDamageDownRateSkillSetupData.IsExist() ? magicalDamageDownRateValue : 0f);
                    }

                    float lastAttack = (attackDefault - (attackDefault * CY_Function.ChangePercentage(allDeBuffAttack))) +
                         ((attackDefault - (attackDefault * CY_Function.ChangePercentage(allDeBuffAttack))) * CY_Function.ChangePercentage(allBuffAttack));

                    //
                    float lastArgument = 100f;

                    // 데미지 계수는 여기서 처리를 함.
                    if(isDot is false)
                    {   // 물리 공격
                        if (isDamageRateModule)
                            lastArgument = GetSkillModuleArgumentValue(skillSetupDatas[findDamageRateModuleIndex]);

                        // 마법 공격
                        if (isMagicalDamageRateModule)
                            lastArgument = GetSkillModuleArgumentValue(skillSetupDatas[findMagicalDamageRateModuleIndex]);
                    }

                    // 
                    lastAttack = lastAttack * CY_Function.ChangePercentage(lastArgument);

                    // 도발 모듈이 있다면 공격력 감소.
                    int findIndex = GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.ProvokeCount);
                    if(findIndex != -1 )
                    {
                        // 도발이 있다면 공격력을 감소 시킴
                        float attackDown = GetCharacterStatData.SkillBuffDatas[findIndex].SkillSetupData.ArgumentList[6];

                        if(GetCharacterStatData.SkillBuffDatas[findIndex].SkillSetupData.ArgumentList[5] == 2f)
                        {
                            CY_CharacterEntity actorEntity = GetSkillModuleActorEntity(GetCharacterStatData.SkillBuffDatas[findIndex].SkillSetupData);
                            attackDown += GetSkillModuleArgumentValue(GetCharacterStatData.SkillBuffDatas[findIndex].SkillSetupData, false, actorEntity);
                        }

                        lastAttack = lastAttack - (lastAttack * CY_Function.ChangePercentage(attackDown));
                    }

                    characterStatData.SetValue((int)lastAttack);
                }
                break;

            case eHeroStatus.HP:
                characterStatData = CharacterStatData.HeroStats[heroStatus];
                break;

            case eHeroStatus.Critical:
                {
                    SkillSetupData criticalSkillModule = GetSkillModuleGroup(eModuleGroup.CriticalUpDamage, isDamageRateModule, isMagicalDamageRateModule);
                    float criticalUpDamage = GetSkillModuleArgumentValue(criticalSkillModule);

                    criticalSkillModule = GetSkillModuleGroup(eModuleGroup.CriticalDownDamage, isDamageRateModule, isMagicalDamageRateModule);
                    float criticalDownDamage = 0f;
                    if (criticalSkillModule.IsExist())
                    {
                        CY_CharacterEntity actorEntity = GetSkillModuleActorEntity(criticalSkillModule);
                        criticalDownDamage = GetSkillModuleArgumentValue(criticalSkillModule, true, actorEntity);
                    }

                    float lastCritical = CharacterStatData.HeroStats[heroStatus].Float + criticalUpDamage - criticalDownDamage;

                    if (lastCritical < 0)
                        lastCritical = 0;

                    characterStatData.SetValue(lastCritical);
                }
                break;
        }

        return characterStatData;
    }

    /// <summary>
    /// 모듈에 따른 수치(%)
    /// </summary>
    /// <param name="moduleGroup"></param>
    /// <param name="isPhysical"></param>
    /// <param name="isMagical"></param>
    /// <returns></returns>
    public SkillSetupData GetSkillModuleGroup(eModuleGroup moduleGroup, bool isPhysical, bool isMagical, CY_CharacterEntity targetSkill = null)
    {
        var moduleGroups = targetSkill.IsNull() ?
                            GetCharacterStatData.SkillBuffDatas.FindAll(x => x.SkillSetupData.ModuleGroup == moduleGroup) :
                            targetSkill.GetCharacterStatData.SkillBuffDatas.FindAll(x => x.SkillSetupData.ModuleGroup == moduleGroup);
        
        SkillSetupData skillSetupData = null;
        bool isOk = (moduleGroups.Count > 0);

        if (isOk)
        {
            for (int i = 0; i < moduleGroups.Count; ++i)
            {
                if (skillSetupData.IsNull())
                {
                    skillSetupData = moduleGroups[i].SkillSetupData;
                    continue;
                }

                if (skillSetupData.IsExist() && GetSkillModuleArgumentValue(skillSetupData) < GetSkillModuleArgumentValue(moduleGroups[i].SkillSetupData))
                    skillSetupData = moduleGroups[i].SkillSetupData;
            }
        }

        return skillSetupData;
    }

    /// <summary>
    /// 스킬 모듈에 있는 Argument 계수를 판단하는 함수.
    /// </summary>
    /// <param name="skillSetupData"></param>
    /// <returns></returns>
    public float GetSkillModuleArgumentValue(SkillSetupData skillSetupData, bool isUseArgument1 = true, CY_CharacterEntity targetSkill = null)
    {
        if (skillSetupData.IsNull())
            return 0f;

        float argument1 = isUseArgument1 ? skillSetupData.ArgumentList[0] : 0f;

        float specialValue = targetSkill.IsNull() ?
                GetCombatHeroStat(eHeroStatus.Special).Float :
                targetSkill.GetCombatHeroStat(eHeroStatus.Special).Float;

        float data1 = (int)(specialValue / skillSetupData.ArgumentList[3]);
        float argument5 = data1 * skillSetupData.ArgumentList[4];           // 어규먼트4

        float value = argument1 + argument5;
        return value;
    }

    /// <summary>
    /// 스킬 사용자를 찾는 함수.
    /// </summary>
    /// <param name="skillSetupData"></param>
    /// <param name="targetEntity"></param>
    /// <returns></returns>
    public CY_CharacterEntity GetSkillModuleActorEntity(SkillSetupData skillSetupData, CY_CharacterEntity targetEntity = null)
    {
        CY_CharacterEntity actorEntity = null;

        int actorIndex = targetEntity.IsNull() ?
            GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData == skillSetupData) :
            targetEntity.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData == skillSetupData);

        if(actorIndex != -1)
        {
            actorEntity = targetEntity.IsNull() ?
                GetCharacterStatData.SkillBuffDatas[actorIndex].actorCharacterEntity :
                targetEntity.GetCharacterStatData.SkillBuffDatas[actorIndex].actorCharacterEntity;
        }

        return actorEntity;
    }

    /// <summary>
    /// 전투에서 실제 데미지를 구하는 함수.
    /// </summary>
    /// <returns></returns>
    public int GetDamage(CY_CharacterEntity targetCharacter, out bool isCritical, out bool isBlock, out bool isEvade, bool idDotCalc = false)
    {
        // 회피 검사.
        isEvade = false;

        int findIndex = targetCharacter.GetCharacterStatData.SkillBuffDatas.FindIndex(x => x.SkillSetupData.ModuleGroup == eModuleGroup.Miss);
        if(findIndex != -1)
        {
            var skillBuffData = targetCharacter.GetCharacterStatData.SkillBuffDatas[findIndex];

            float missValue = GetSkillModuleArgumentValue(skillBuffData.SkillSetupData);
            if(CalcProbability(skillBuffData.SkillSetupData) && CY_Function.CalcPercentage(missValue))
                isEvade = true;
        }

        // 공격 추가 계산 모듈
        var attackBuffs = CurrentSkillSetupDatas.FindAll(x => x.ModuleType == eModuleType.AttackBuff);

        //
        TargetCharacter = targetCharacter;

        // 
        float damage = 0; //GetCombatHeroStat(eHeroStatus.Attack).Int; //0;

        float heroAttack = GetCombatHeroStat(eHeroStatus.Attack, targetCharacter, idDotCalc).Int * DataTable.I.BattleBasicConstValue.AttackRate;

        float slotWeaponAttack = MyHeroData.IsExist() && IsNPC is false ? MyHeroData.GetEquipStatusValue(eSlotType.WeaponSlot) : 0f;

        float weaponAttack = slotWeaponAttack * DataTable.I.BattleBasicConstValue.WeaponAttackRate;

        float enemyDefence = 0f;
        float SpecialArgument = 0f;

        enemyDefence = HeroData.HeroAttackType == eHeroAttackType.Physical ?
            targetCharacter.GetCombatHeroStat(eHeroStatus.PhysicalDefense, targetCharacter).Int :
            targetCharacter.GetCombatHeroStat(eHeroStatus.MagicalDefense, targetCharacter).Int;

        // 방어력 무시 모듈 검사.
        eModuleGroup moduleGroup = HeroData.HeroAttackType == eHeroAttackType.Physical ? eModuleGroup.IgnoreDefenseDamageRate : eModuleGroup.MagicalIgnoreDefenseDamageRate;
        findIndex = attackBuffs.FindIndex(x => x.ModuleGroup == moduleGroup);
        if (attackBuffs.Count > 0 && findIndex != -1 && CalcProbability(attackBuffs[findIndex]))
        {
            float calcEnemyDefence = 0f;
            SpecialArgument = GetSkillModuleArgumentValue(attackBuffs[findIndex]);
            calcEnemyDefence = enemyDefence - (enemyDefence * (CY_Function.ChangePercentage(SpecialArgument)));
            enemyDefence = (int)calcEnemyDefence;
        }

        float defense = enemyDefence / DataTable.I.BattleBasicConstValue.DefenseRate;
        defense = defense <= 0 ? 0 : defense;
        float lastEnemyDefense = 1 + defense;

        damage = (heroAttack + weaponAttack) / lastEnemyDefense;

        int LastDamage = Mathf.FloorToInt(damage);
        int minDamage = (int)(GetCombatHeroStat(eHeroStatus.Attack, targetCharacter).Int * (DataTable.I.BattleBasicConstValue.MinAttackLimit * 0.01f));
        int finalDamage = Mathf.Max(LastDamage, minDamage);

        var criticalRateValue = GetCombatHeroStat(eHeroStatus.CriticalRate, targetCharacter).Float;
        var blockValue = targetCharacter.GetCombatHeroStat(eHeroStatus.Block, targetCharacter).Float;

        float criticalPercenet = criticalRateValue - blockValue;
        isCritical = CY_Function.CalcPercentage(criticalPercenet);
        
        float criticalRate = 1f;
      
        {
            // 공격자 : 받는 물리, 마법 피해 감소
            eModuleGroup downModuleGroup = HeroData.HeroAttackType == eHeroAttackType.Physical ? eModuleGroup.ReceivePhysicalDownDamage : eModuleGroup.ReceiveMagicalDownDamage;
            SkillSetupData buffSkill = GetSkillModuleGroup(downModuleGroup, false, false);
            float buffValue = 0f;
            if (buffSkill.IsExist() && CalcProbability(buffSkill))
            {
                buffValue = GetSkillModuleArgumentValue(buffSkill);
            }

            // 타겟 : 받는 물리, 마법 피해 증가
            eModuleGroup upModuleGroup = HeroData.HeroAttackType == eHeroAttackType.Physical ? eModuleGroup.ReceivePhysicalUpDamage : eModuleGroup.ReceiveMagicalUpDamage;
            buffSkill = targetCharacter.GetSkillModuleGroup(upModuleGroup, false, false);
            float deBuffValue = 0f;
            if(buffSkill.IsExist() && CalcProbability(buffSkill))
            {
                CY_CharacterEntity actorEntity = GetSkillModuleActorEntity(buffSkill, targetCharacter);
                deBuffValue = targetCharacter.GetSkillModuleArgumentValue(buffSkill, true, actorEntity);
            }

            // 
            float buffAllValue = buffValue - deBuffValue;

            float realFinalDamage = 0f;
            float calFinalDamage = ((finalDamage + hpDamage) * criticalRate * blockRate);

            if (buffAllValue < 0)
            {
                // 디버프로 인해 피해량 증가.
                buffAllValue = Mathf.Abs(buffAllValue);
                realFinalDamage = calFinalDamage + (calFinalDamage * CY_Function.ChangePercentage(buffAllValue));
            }
            else if(buffAllValue == 0)
            {
                // 버프가 없거나, 상쇄되어 기본 데미지만 적용.
                realFinalDamage = calFinalDamage;
            }
            else
            {
                // 버프로 인해 피해량 감소.
                realFinalDamage = calFinalDamage - (calFinalDamage * CY_Function.ChangePercentage(buffAllValue));
            }
          
            MaxDamage_History = (int)(realFinalDamage);
            return (int)(realFinalDamage);
        }
    }

    /// <summary>
    /// 스킬 셋업의 기능을 동작하게 해주는 함수.(대상이 없는 경우)
    /// </summary>
    public bool StartSkillSetup(List<SkillSetupData> skillSetupDatas, SkillData skillData)
    {
        // 
        bool isPossibleAp = true;
        if (skillData.AP < 0)
            isPossibleAp = CharacterStatData.IsPossibleAp(skillData.AP);

        // 
        if (isPossibleAp is false)
            return false;

        // 타겟 타입에 따른 최종 타겟
        List<CY_CharacterEntity> lastTargets = new List<CY_CharacterEntity>();

        if (GetCombatTargets(skillSetupDatas, skillData.ModuleType == eModuleType.Recover, skillData.ModuleType == eModuleType.UndeBuff, ref lastTargets) is false)
            return false;

        CombatTargets.Clear();
        CombatTargets.AddRange(lastTargets);
        TargetCharacter = null;

        // 스킬 발동 할수 있음.
        return true;
    }

    public bool StartPreviewSkillSetup(List<SkillSetupData> skillSetupDatas, SkillData skillData)
    {
        // 타겟 타입에 따른 최종 타겟
        List<CY_CharacterEntity> lastTargets = new List<CY_CharacterEntity>();

        if (GetCombatTargets(skillSetupDatas, skillData.ModuleType == eModuleType.Recover, skillData.ModuleType == eModuleType.UndeBuff, ref lastTargets) is false)
            return false;

        CombatTargets.Clear();
        CombatTargets.AddRange(lastTargets);
        TargetCharacter = null;

        // 스킬 발동 할수 있음.
        return true;
    }

    #endregion Combat

    #region AI

    public void UpdateAI(AIType aIType)
    {
        switch(aIType)
        {
            case AIType.Start:
                AI.StartBehaviour();
                break;

            case AIType.Stop:
                AI.StopBehaviour();
                break;
        }
    }

    #endregion AI 

    #region TimeLine

    public void InitManipulation(LocationType Location)
    {
        foreach(TransformManipulation item in ManipulationS)
        {
            item.SetLocationTypeInfo(Location);
        }
    }

    public void SetTimeLineBinding(TimelineAsset asset ,Vector3 endpos , Vector3 hitpos ,Vector3 linepos)
    {
        TargetPos[0] = endpos;
        TargetPos[1] = hitpos;
        TargetPos[2] = linepos;

        for (int i=0; i< asset.rootTrackCount; i++)
        {
            TrackAsset track = asset.GetRootTrack(i);
            switch (track.name)
            {
                case "Move":
                    ActionPlayable.SetGenericBinding(track, CharacterAnimator);
                    break;

                case "Spine":
                    foreach (TimelineClip ChildClip in track.GetClips())
                    {
                        CY_SpinePlayableAsset SpineAsset = ChildClip.asset as CY_SpinePlayableAsset;
                        if (SpineAsset == null)
                        {
                            StringExtention.Log("Spine 트랙의 클립이 CY_SpinePlayableAsset 아닙니다 ");
                            break;
                        }
                        ActionPlayable.SetReferenceValue(SpineAsset.sourceGameObject.exposedName, this);
                    }
                    break;
                   
                case "BoneEffectPrefab":
                    {
                        int Index = 0;
                        foreach (TrackAsset ChildTrack in track.GetChildTracks())
                        {

                            foreach (TimelineClip ChildClip in ChildTrack.GetClips())
                            {
                                CY_ControlPlayableAsset ControlAsset = ChildClip.asset as CY_ControlPlayableAsset;
                                if (ControlAsset == null)
                                {
                                    StringExtention.Log("BoneEffectPrefab 트랙의 클립이 CY_ControlPlayableAsset이 아닙니다 ");
                                    break;
                                }

                                if (BoneEffects.Length > Index)
                                    ActionPlayable.SetReferenceValue(ControlAsset.sourceGameObject.exposedName, BoneEffects[Index]);
                            }
                            Index++;
                        }
                    }
                    break;

                case "LocalEfffectPrefab":
                    foreach (TimelineClip ChildClip in track.GetClips())
                    {
                        CY_ControlPlayableAsset ControlAsset = ChildClip.asset as CY_ControlPlayableAsset;
                        if(ControlAsset == null)
                        {
                            StringExtention.Log("LocalEfffectPrefab 트랙의 클립이 CY_ControlPlayableAsset이 아닙니다 ");
                            break;
                        }
                        ActionPlayable.SetReferenceValue(ControlAsset.sourceGameObject.exposedName, LocalEffectPoint);
                    }
                    break;

                case "LocalEfffectPos":
                    {
                        ActionPlayable.SetGenericBinding(track, LocalEffectPoint);
                    }
                    break;
                
                default:

                    break;
            }

        }

        CY_TimeLine.TimeLineSplit(asset,
                        (track) =>
                        {
                            if (track.GetType() == typeof(GroupTrack))
                            {
                                switch (track.name)
                                {
                                    case "ProjectilePosGroup":
                                        {
                                            int Index = 0;
                                            foreach (TrackAsset ChildTrack in track.GetChildTracks())
                                            {
                                                ActionPlayable.SetGenericBinding(ChildTrack, ProjectileGameObjects[Index]);
                                                Index++;
                                            }
                                            
                                        }
                                        break;

                                    case "ProjectilePrefabGroup":
                                        {
                                            int Index = 0;
                                            foreach (TrackAsset ChildTrack in track.GetChildTracks())
                                            {
                                                foreach (TimelineClip ChildClip in ChildTrack.GetClips())
                                                {
                                                    CY_ControlPlayableAsset ControlAsset = ChildClip.asset as CY_ControlPlayableAsset;
                                                    if (ControlAsset == null)
                                                    {
                                                        StringExtention.Log("ProjectilePrefab 트랙의 클립이 CY_ControlPlayableAsset이 아닙니다 ");
                                                        break;
                                                    }
                                                    ActionPlayable.SetReferenceValue(ControlAsset.sourceGameObject.exposedName, ProjectileGameObjects[Index]);
                                                }
                                                Index++;
                                            }
                                        }
                                        break;
                                }
                            }
                        },
                        (clip) =>
                        {
                            
                        });

        ActionPlayable.playableAsset = asset;
    }
   
    public void SetLocationTypeInfo(LocationType type)
    {
        InLocationType = type;
    }
    #endregion TimeLine

    public string GetHeroResourcesName()
    {
        return HeroData?.HeroResourcesName;
    }

    public void AddAP(int ap, string log)
    {
        GetCharacterStatData.AddAP(ap, name + log);
    }
}

