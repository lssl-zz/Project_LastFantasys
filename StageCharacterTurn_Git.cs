using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace funLAB_UI
{
    public class StageCharacterTurn : funLAB_UIbase
    {
        [SerializeField, GetComponent]
        private Canvas Canvas;

        [Header("■ Component - GameObject")]
        [SerializeField, GetComponentInChildrenName("TurnHero_Image_EnemyFrame")]
        private GameObject TurnHero_Image_EnemyFrame;
        [SerializeField, GetComponentInChildrenName("TurnHero_Image_PlayerFrame")]
        private GameObject TurnHero_Image_PlayerFrame;

        [Header("■ Component - Image")]
        [SerializeField, GetComponentInChildrenName("HeroBg_Image_Hero")]
        private Image HeroBg_Image_Hero;

        #region Property

        [Header("■ Property")]
        [SerializeField]
        private int SortingOrderBase = 10;

        /// <summary>
        /// StageProcess에 있는 PlayerEntitys와 동일한 데이터입니다
        /// </summary>
        private CY_CharacterEntity CharacterEntityInfo = null;

        /// <summary>
        /// Turn의 시작, 종료 위치값
        /// </summary>
        private Vector3 StartPos, EndPos;

        /// <summary>
        /// 턴 시간 계산을 해주기 위한 변수.
        /// </summary>
        private float TurnTime = 0f;

        private float CompleteRatio = 0f;

        #endregion Property

        /// <summary>
        /// 영웅의 턴 획득 계산 함수.
        /// </summary>
        public void UpdateFunc(float deltaTime)
        {
            bool isDeadCheck = false;
            if (CharacterEntityInfo.IsExist() && CY_StageProcess.IsDieCharacter)
            {
                isDeadCheck = CharacterEntityInfo.GetCharacterStatData.IsZombie ? false : true;
            }
            else
            {
                isDeadCheck = CY_StageProcess.IsDieCharacter;
            }

            if (CY_StageProcess.IsPuase is false && isDeadCheck is false && CharacterEntityInfo.IsExist())
            {
                Progress += deltaTime / CharacterEntityInfo.GetTurnTime();
                Vector3 newPos = (StartPos + (EndPos - StartPos) * Progress) + new Vector3(0, 0.5f, 0);

                transform.localPosition = newPos;

                UpdateSortingGroupOrder();

                if (Progress >= 1)
                {
                    // 행동 가능 영웅 목록에 Add
                    CY_StageProcess.I.GetActionEntityList.Add(CharacterEntityInfo);
                }
            }
        }

        #region Common

        public void InitCharacterEntity(CY_CharacterEntity cY_CharacterEntity, Vector3 startPos, Vector3 endPos)
        {
            // 
            CharacterEntityInfo = cY_CharacterEntity;
            StartPos = startPos;
            EndPos = endPos;

            bool isNPC = cY_CharacterEntity.IsNPC;
            TurnHero_Image_PlayerFrame.SetActive(isNPC is false);
            TurnHero_Image_EnemyFrame.SetActive(isNPC);

            // 
            transform.localPosition = StartPos;

            // 
            HeroBg_Image_Hero.SetSprite(CYResourcesManager.I.GetSpriteForAtlas(AtlasType.Icon_Character_Atlas, cY_CharacterEntity.GetHeroResourcesName()));
        }

        private float Progress = 0f;
        public float GetProgress { get { return Progress; } }

        /// <summary>
        /// 턴 시간을 초기화
        /// </summary>
        public void InitStartTime()
        {
            // 
            TurnTime = 0f;
            Progress = 0f;

            CompleteRatio = 0;
        }

        /// <summary>
        /// 턴 객체 Order표기
        /// </summary>
        /// <param name="order"></param>
        private void UpdateSortingGroupOrder()
        {
            float posY = Mathf.Abs(transform.localPosition.y);
            Canvas.sortingOrder = (int)(SortingOrderBase + Mathf.Floor(posY));
        }

        #endregion Common

        #region override

        //protected override void Awake()
        //{
        //    base.Awake();
        //}

        //public override void Open()
        //{
        //    base.Open();
        //}

        //public override void Close()
        //{
        //    base.Close();
        //}

        //public override void RefreshUI()
        //{
        //    base.RefreshUI();
        //}

        #endregion override

    }
}


