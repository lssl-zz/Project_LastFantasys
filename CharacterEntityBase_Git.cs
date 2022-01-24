using DG.Tweening;
using funLAB.SpineModel;
using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

public class CharacterEntityBase : MonoBehaviour
{
    protected funLAB_M_SpineModel SpineModel = new funLAB_M_SpineModel();
    public funLAB_M_SpineModel GetSpineModel { get { return SpineModel; } }

    protected Transform Root;

    //
    [SerializeField, GetComponentInChildrenName("SkeletonAnimation")]
    protected SkeletonAnimation SkeletonAnimation;

    [SerializeField, GetComponentInChildrenName("SkeletonAnimation")]
    public GameObject GetSkeletonAnimationObject;

    [SerializeField, GetComponentInChildrenName("SkeletonAnimation")]
    public SpineRenderSorter SpineRenderSorter;

    // 
    [SerializeField, GetComponentInChildrenName("HitPoint")]
    protected Transform HitPoint;
    public Transform GetHitPoint { get { return HitPoint; } }

    [SerializeField, GetComponentInChildrenName("Head")]
    protected Transform Head;
    public Transform GetHead { get { return Head; } }

    [SerializeField, GetComponentInChildrenName("HUD")]
    protected Transform HUDTrans;
    public Transform GetHUDTrans { get { return HUDTrans; } }

    [SerializeField, GetComponentInChildrenName("DS")]
    protected Transform DS;
    public Transform GetDSTrans { get { return DS; } }

    [SerializeField, GetComponentInChildrenName("Root")]
    protected Transform RootBone;
    public Transform GetRoot { get { return RootBone; } }

    public Transform GetLinePoint 
    { 
        get
        {
            if(transform.parent == null)
                return null;

            Transform obj = transform.parent.Find("LinePoint"); ;

            if(obj != null)
                return obj;

            if (transform.parent.parent == null)
                return null;

            return transform.parent.parent.Find("LinePoint");
        } 
    }

    protected virtual void Awake()
    {
        Root = transform;

        if(SkeletonAnimation != null)
        {
            SpineModel.InitSpineAnimation(SkeletonAnimation);
        }
    }
}
