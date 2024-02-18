﻿using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;

public class PuppetAvatar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrackerHandler kinectDevice;
    [SerializeField] private GameObject rootPosition;

    [Header("Puppet Settings")]
    [SerializeField] private Animator puppetAnimator;
    [SerializeField] private  Transform characterRootTransform;
    [SerializeField] private  float offsetY;
    [SerializeField] private  float offsetZ;

    // memo:
    //   このclassからMicrosoft.Azure.Kinect.BodyTrackingへの依存を削除したい。そのためにはここのJointIdを消して、HumanBodyBonesに変換する必要がある。
    //   その修正ができるとデータを受け取る側が、BonesでQuaternion（関節の回転）にアクセスできるようになり、Kinectを意識しないで、Avatarを動かせるようになる。
    private Dictionary<JointId, Quaternion> absoluteOffsetMap;

    private Dictionary<HumanBodyBones, Quaternion> absoluteOffsetMapNetwork;

    void Start()
    {
        absoluteOffsetMap = new Dictionary<JointId, Quaternion>();
        for (var i = 0; i < (int)JointId.Count; i++)
        {
            var hbb = Mapper.ToHumanBodyBonesFrom((JointId)i);
            if (hbb == HumanBodyBones.LastBone) continue;
            
            var tran = puppetAnimator.GetBoneTransform(hbb);
            var absOffset = puppetAnimator.GetSkeletonBone(tran.name).rotation;
            // find the absolute offset for the tpose
            while (!ReferenceEquals(tran, characterRootTransform))
            {
                tran = tran.parent;
                absOffset = puppetAnimator.GetSkeletonBone(tran.name).rotation * absOffset;
            }
            absoluteOffsetMap[(JointId)i] = absOffset;
            absoluteOffsetMapNetwork[(HumanBodyBones)i] = absOffset;
        }
    }

    void LateUpdate()
    {
        for (var i = 0; i < (int)JointId.Count; i++)
        {
            var hbb = Mapper.ToHumanBodyBonesFrom((JointId)i);
            if (hbb == HumanBodyBones.LastBone || !absoluteOffsetMap.ContainsKey((JointId)i)) continue;
            
            // get the absolute offset
            var absOffset = absoluteOffsetMap[(JointId)i];
            var finalJoint = puppetAnimator.GetBoneTransform(hbb);
            finalJoint.rotation = absOffset * Quaternion.Inverse(absOffset) * kinectDevice.absoluteJointRotations[i] * absOffset;
            
            if (i != 0) continue;
            // character root plus translation reading from the kinect, plus the offset from the script public variables
            var p = rootPosition.transform.localPosition;
            finalJoint.position = characterRootTransform.position + new Vector3(p.x, p.y + offsetY, p.z - offsetZ);
        }
    }
}
