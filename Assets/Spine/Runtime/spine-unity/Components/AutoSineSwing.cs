using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Spine
{
    public class AutoSineSwing : MonoBehaviour
    {
        public int selfMode;
        public string selfAnimationName;
        public string selfBoneName;
        public float selfRotateCenter;
        public float selfRotateTime;
        public float selfRotateRange;
        public float selfRotateOffset;
        public float selfChildOffset;
        public float selfSpring;
        public float selfAffectByLevel;
        public float selfSpringLevel;
        public List<string> selfEndBoneName;
        public float selfMoveXFreq = 1;
        public float selfMoveXAmp = 0;
        public float selfMoveXOctaves = 0;
        public float selfMoveXDelay = 0;
        public float selfMoveXCenter = 0;
        public float selfMoveXSeed;
        public float selfMoveYFreq = 1;
        public float selfMoveYAmp = 0;
        public float selfMoveYOctaves = 0;
        public float selfMoveYDelay = 0;
        public float selfMoveYCenter = 0;
        public bool selfMoveYSameAsX = true;
        public float selfScaleXFreq = 1;
        public float selfScaleXAmp = 0;
        public float selfScaleXOctaves = 0;
        public float selfScaleXDelay = 0;
        public float selfScaleXCenter = 0;
        public float selfScaleYFreq = 1;
        public float selfScaleYAmp = 0;
        public float selfScaleYOctaves = 0;
        public float selfScaleYDelay = 0;
        public float selfScaleYCenter = 0;
        public bool selfScaleYSameAsX = false;
        public float selfRotateSpeed = 0;
        public float selfRotateFreq = 1;
        public float selfRotateAmp = 0;
        public float selfRotateOctaves = 0;
        public float selfRotateDelay = 0;
        public bool selfRotateFollowEnable = false;
        public float selfRotateFollowLimit = 0;
        public float selfRotateFollowSpeed = 0.1f;
        public float selfRotateFollowFlip = 0;
        public float selfRotateFollowXMax = 20f;
        public float selfRotateFollowYMax = 20f;
        // sine mode
        public float selfScaleXRange = 0f;
        public float selfScaleXOffset = 0f;
        public float selfScaleXChildOffset = 0.25f;
        public float selfScaleXSpring = 0f;
        public float selfScaleXTime = 2f;
        public float selfScaleXAffectByLevel = 0.1f;
        public float selfScaleYRange = 0f;
        public float selfScaleYOffset = 0f;
        public float selfScaleYChildOffset = 0.25f;
        public float selfScaleYSpring = 0f;
        public float selfScaleYTime = 2f;
        public float selfScaleYAffectByLevel = 0.1f;
        public bool selfSinScaleXSameAsY = false;
        //mode = 4
        public float selfDelay = 0.1f;
        public float selfLimitRange = 0;
        public float selfSpeed = 0;

        //Append
        public float selfSpringRot = 0;
        public float selfFriction = 0.7f;
        public float selfSpringUseTarget = 0;

        public string selfRootBoneName = "";
        public string selfTargetBoneName = "";
        public string selfTargetEndBoneName = "";
        public float selfTargetWeight = 1;

        // The Speed Of Magica Bone, Added By ZeroFly
        public float selfMagicSpeed = 0.9f;

        // The Lerp Of Mode 3 Bone, Added By ZeroFly
        public float selfMode3Lerp = 1f;

        public class CustomBone
        {
            public CustomBone parent;
            public string name;
            public Transform transform;
            public Quaternion initalRotation;
            public List<CustomBone> children;
            //Local Position x
            //public float x;
            //Local Position y
            //public float y;

            //Local Scale
            //public float scaleX;
            //public float scaleY;
            public float initX;
            public float initY;
            //Quaternion to float
            public float initRotation;
            public float initWorldX;
            public float initWorldY;
            public float initScaleX;
            public float initScaleY;
            public float autoMovePrevWorldX;
            public float autoMovePrevWorldY;
            public float autoMoveSpeedX;
            public float autoMoveSpeedY;
            public float autoMoveFriction;
            public float followRotation;
            public float elasticSpeedX;
            public float elasticSpeedY;


            public float tailAutoMovePrevWorldX = 0;
            public float tailAutoMovePrevWorldY = 0;

            public Spine.Bone boneRef;
        }

        public Transform rootBoneTransform;

        public CustomBone rootBone;

        public CustomBone targetBone;

        public float fTime;

        public SkeletonAnimation skeletonAnimationRef;

        public SkeletonMecanim skeletonMecanimRef;

        private Bone rootBoneRef;

        private Skeleton skeletonRef;

        private Animator relateAnimator;

        // Start is called before the first frame update
        void Start()
        {
            if (!selfMoveYSameAsX)
                selfMoveYSameAsX = selfMoveXFreq == selfMoveYFreq && selfMoveXAmp == selfMoveYAmp && selfMoveXOctaves == selfMoveYOctaves && selfMoveXDelay == selfMoveYDelay && selfMoveXCenter == selfMoveYCenter;

            if(!selfScaleYSameAsX)
                selfScaleYSameAsX = selfScaleXFreq == selfScaleYFreq && selfScaleXAmp == selfScaleYAmp && selfScaleXOctaves == selfScaleYOctaves && selfScaleXDelay == selfScaleYDelay && selfScaleXCenter == selfScaleYCenter;

            selfRotateFollowEnable = 0 != selfRotateFollowLimit;

            if (!selfSinScaleXSameAsY)
            {
                selfSinScaleXSameAsY = selfScaleXRange == selfScaleYRange && selfScaleYCenter == selfScaleXCenter && selfScaleXTime == selfScaleYTime && selfScaleXOffset == selfScaleYOffset && selfScaleXChildOffset == selfScaleYChildOffset && selfScaleXSpring == selfScaleYSpring && selfScaleYAffectByLevel == selfScaleXAffectByLevel;
            }

            selfMoveXSeed = selfMoveXSeed == 0 ? Mathf.Floor(1e4f * Random.Range(0f, 1f)):selfMoveXSeed;

            StartAutoSwing();
        }

        public void StartAutoSwing()
        {
            if (rootBoneTransform == null)
            {
                rootBoneTransform = transform;
            }

            if (rootBoneTransform != null)
            {
                rootBoneRef = rootBoneTransform.GetComponent<Spine.Unity.SkeletonUtilityBone>().bone;

                if(rootBoneRef != null)
                {
                    rootBone = PackTransform(rootBoneTransform);
                }
            }

            if(skeletonAnimationRef != null)
            {
                skeletonRef = skeletonAnimationRef.skeleton;

                CheckSwingParam(skeletonRef, Animator.StringToHash(skeletonAnimationRef.AnimationName));

                skeletonAnimationRef.UpdateLocal += OnNeedToSwing;
            }

            if(skeletonMecanimRef != null)
            {
                skeletonRef = skeletonMecanimRef.skeleton;

                if(relateAnimator == null)
                {
                    relateAnimator = GetComponentInChildren<Animator>();
                }

                if(relateAnimator != null)
                {
                    int hashCode = relateAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                    CheckSwingParam(skeletonRef, hashCode);
                }

                skeletonMecanimRef.UpdateLocal += OnNeedToSwing;
            }
        }

        void CheckSwingParam(Skeleton skeleton,int animationNameHash)
        {
            if (skeleton == null)
                return;

            int selfHash = Animator.StringToHash(selfAnimationName);

            if (selfHash != animationNameHash)
            {
                SwingData s = skeleton.FindSwings(selfBoneName, animationNameHash);

                if(s != null)
                {
                    selfAnimationName = s.selfAnimationName;
                    selfMode = s.selfMode;
                    selfRotateCenter = s.selfRotateCenter;
                    selfRotateTime = s.selfRotateTime;
                    selfRotateRange = s.selfRotateRange;
                    selfRotateOffset = s.selfRotateOffset;
                    selfChildOffset = s.selfChildOffset;
                    selfSpring = s.selfSpring;
                    selfAffectByLevel = s.selfAffectByLevel;
                    selfSpringLevel = s.selfSpringLevel;
                    selfEndBoneName = s.selfEndBoneName;
                    selfMoveXFreq = s.selfMoveXFreq;
                    selfMoveXAmp = s.selfMoveXAmp;
                    selfMoveXOctaves = s.selfMoveXOctaves;
                    selfMoveXDelay = s.selfMoveXDelay;
                    selfMoveXSeed = s.selfMoveXSeed;
                    selfMoveXCenter = s.selfMoveXCenter;
                    selfMoveYFreq = s.selfMoveYFreq;
                    selfMoveYAmp = s.selfMoveYAmp;
                    selfMoveYOctaves = s.selfMoveYOctaves;
                    selfMoveYDelay = s.selfMoveYDelay;
                    selfMoveYCenter = s.selfMoveYCenter;
                    selfScaleXCenter = s.selfScaleXCenter;
                    selfScaleXRange = s.selfScaleXRange;
                    selfScaleXTime = s.selfScaleXTime;
                    selfScaleXOffset = s.selfScaleXOffset;
                    selfScaleXChildOffset = s.selfScaleXChildOffset;
                    selfScaleXSpring = s.selfScaleXSpring;
                    selfScaleXAffectByLevel = s.selfScaleXAffectByLevel;
                    selfScaleYTime = s.selfScaleYTime;
                    selfScaleYFreq = s.selfScaleYFreq;
                    selfScaleYOffset = s.selfScaleYOffset;
                    selfScaleYSpring = s.selfScaleYSpring;
                    selfScaleYAffectByLevel = s.selfScaleYAffectByLevel;
                    selfScaleYChildOffset = s.selfScaleYChildOffset;
                    selfScaleYRange = s.selfScaleYRange;
                    selfScaleXFreq = s.selfScaleXFreq;
                    selfScaleXAmp = s.selfScaleXAmp;
                    selfScaleXOctaves = s.selfScaleXOctaves;
                    selfScaleXDelay = s.selfScaleXDelay;
                    selfScaleXCenter = s.selfScaleXCenter;
                    selfScaleYFreq = s.selfScaleYFreq;
                    selfScaleYAmp = s.selfScaleYAmp;
                    selfScaleYOctaves = s.selfScaleYOctaves;
                    selfScaleYDelay = s.selfScaleYDelay;
                    selfScaleYCenter = s.selfScaleYCenter;
                    selfRotateSpeed = s.selfRotateSpeed;
                    selfRotateFollowFlip = s.selfRotateFollowFlip;
                    selfRotateFollowLimit = s.selfRotateFollowLimit;
                    selfRotateFollowSpeed = s.selfRotateFollowSpeed;
                    selfRotateFollowXMax = s.selfRotateFollowXMax;
                    selfRotateFollowYMax = s.selfRotateFollowYMax;
                    selfDelay = s.selfDelay;
                    selfSpeed = s.selfSpeed;
                    selfSpringRot = s.selfSpringRot;
                    selfFriction = s.selfFriction;
                    selfSpringUseTarget = s.selfSpringUseTarget;

                    selfRootBoneName = s.selfRootBoneName;
                    selfTargetBoneName = s.selfTargetBoneName;
                    selfTargetEndBoneName = s.selfTargetEndBoneName;
                    selfTargetWeight = s.selfTargetWeight;
    }
            }
        }

        public void RefreshInitParam(CustomBone root)
        {
            if(root != null)
            {
                Transform rootTransform = root.transform;
                root.initX = rootTransform.localPosition.x;
                root.initY = rootTransform.localPosition.y;
                root.initWorldX = rootTransform.position.x;
                root.initWorldY = rootTransform.position.y;
                root.initScaleX = rootTransform.localScale.x;
                root.initScaleY = rootTransform.localScale.y;
                root.initalRotation = rootTransform.rotation;

                foreach (var c in root.children)
                {
                    RefreshInitParam(c);
                }
            }
            
        }

        public CustomBone PackTransform(Transform rootTransform)
        {
            CustomBone root = null;

            if (rootTransform != null)
            {
                root = new CustomBone();
                root.parent = null;
                root.name = rootTransform.name;

                if (root.name.Equals(selfTargetBoneName))
                {
                    targetBone = root;
                }
                root.transform = rootTransform;
                root.initX = rootTransform.localPosition.x;
                root.initY = rootTransform.localPosition.y;
                root.initWorldX = rootTransform.position.x;
                root.initWorldY = rootTransform.position.y;
                root.initScaleX = rootTransform.localScale.x;
                root.initScaleY = rootTransform.localScale.y;
                root.initalRotation = rootTransform.rotation;
                //root.boneRef = rootTransform.GetComponent<Spine.Unity.SkeletonUtilityBone>().bone;

                List<CustomBone> result = new List<CustomBone>();

                foreach (Transform tr in rootTransform)
                {
                    result.Add(PackTransform(tr));
                }

                root.children = result;

                ManageBoneRef(root);
            }

            return root;
        }

        public bool ManageBoneRef(CustomBone root)
        {
            bool boneIsEmpty = root.boneRef == null;

            if (boneIsEmpty)
            {
                root.boneRef = root.transform.GetComponent<Spine.Unity.SkeletonUtilityBone>().bone;
            }

            if (boneIsEmpty && root.boneRef != null)
            {
                root.initRotation = root.boneRef.Rotation;

                if (root.children.Count == 0)
                {
                    root.tailAutoMovePrevWorldX = root.boneRef.X * root.boneRef.B + root.boneRef.WorldX;
                    root.tailAutoMovePrevWorldY = root.boneRef.Y * root.boneRef.D + root.boneRef.WorldY;
                }

                return true;
            }

            return root.boneRef != null;
        }

        // Update is called once per frame
        public void OnNeedToSwing(ISkeletonAnimation animatedSkeletonComponent)
        {
            if(rootBoneRef == null)
            {
                rootBoneRef = rootBoneTransform.GetComponent<Spine.Unity.SkeletonUtilityBone>().bone;

                if (rootBoneRef != null)
                {
                    rootBone = PackTransform(rootBoneTransform);
                }
            }

            if (rootBone != null)
            {
                switch (selfMode)
                {
                    case 1:
                        UpdateSineMode(Time.time, rootBone, targetBone, 0, 1f, selfEndBoneName);
                        break;
                    case 3:
                        UpdateMode3Func(Time.time, selfMode3Lerp);
                        break;
                    case 4:
                        Vector3 scale = this.rootBone.transform.localScale;
                        float dimension = scale.x * scale.y;
                        //Third Param is Amplitude
                        UpdateSpringMagic(rootBone, targetBone, selfMagicSpeed, 0, 1f, dimension < 0 ? -1 : 1);
                        break;
                }
            }
        }

        public void ResetMoveFriction(CustomBone root)
        {
            if(root.children.Count > 0)
            {
                foreach(var c in root.children)
                {
                    ResetMoveFriction(c);
                    c.autoMoveFriction = 0;
                }
            }
            //root.autoMoveFriction = 0;
            root.autoMoveFriction = 0;
        }

        void UpdateSineMode(float time, CustomBone rootBoneTransform, CustomBone targetTransform, int index, float mixParam, List<string> endBoneName)
        {
            if(rootBoneTransform.boneRef == null)
            {
                bool findRef = ManageBoneRef(rootBoneTransform);

                if (!findRef)
                {
                    return;
                }
            }

            if(targetTransform == null)
            {
                targetTransform = targetBone;
            }

            if (!rootBoneTransform.name.Equals(endBoneName))
            {
                bool useTargetTransform = targetTransform != null && !targetTransform.name.Equals(selfTargetEndBoneName);

                float initCalcRotation = useTargetTransform ? Mathf.Lerp(rootBoneTransform.initRotation, targetTransform.boneRef.Rotation, selfTargetWeight) : rootBoneTransform.initRotation;
                
                float offset = Mathf.Sin((selfRotateOffset - Mathf.Pow(selfChildOffset * index, 1 + selfSpring) + time)
                * Mathf.PI * 2 / selfRotateTime)
                * selfRotateRange * Mathf.Pow(1 + index * selfAffectByLevel, 1 + selfSpringLevel) + selfRotateCenter;
                
                rootBoneTransform.boneRef.Rotation = Mathf.Lerp(rootBoneTransform.boneRef.Rotation, initCalcRotation + offset, mixParam);
                //Debug.Log("Offset " + name + " " + time + " " + rootBoneTransform.boneRef.Rotation);

                float o = 0; 
                if(0 != selfScaleYRange)
                {
                    float initCalcScaleY = useTargetTransform ? Mathf.Lerp(rootBoneTransform.initScaleY, targetTransform.boneRef.ScaleY, selfTargetWeight) : rootBoneTransform.initScaleY;
                    
                    o = Mathf.Sin((selfScaleYOffset - Mathf.Pow(selfScaleYChildOffset * index, 1 + selfScaleYSpring) + time) * Mathf.PI * 2 / selfScaleYTime) * selfScaleYRange * Mathf.Pow(1 + index * selfScaleYAffectByLevel, 1 + selfSpringLevel) + selfScaleYCenter;

                    rootBoneTransform.boneRef.ScaleY = Mathf.Lerp(rootBoneTransform.boneRef.ScaleY, initCalcScaleY + o, mixParam);

                    if (selfSinScaleXSameAsY)
                    {
                        float initCalcScaleX = useTargetTransform ? Mathf.Lerp(rootBoneTransform.initScaleX, targetTransform.boneRef.ScaleX, selfTargetWeight) : rootBoneTransform.initScaleX;

                        rootBoneTransform.boneRef.ScaleX = Mathf.Lerp(rootBoneTransform.boneRef.ScaleX, initCalcScaleX + o, mixParam);
                    }
                }

                if(!selfSinScaleXSameAsY && selfScaleXRange != 0)
                {
                    float initCalcScaleX = useTargetTransform ? Mathf.Lerp(rootBoneTransform.initScaleX, targetTransform.boneRef.ScaleX, selfTargetWeight) : rootBoneTransform.initScaleX;

                    o = Mathf.Sin((selfScaleXOffset - Mathf.Pow(selfScaleXChildOffset * index, 1 + selfScaleXSpring) + time) * Mathf.PI * 2 / selfScaleXTime)
        * selfScaleXRange * Mathf.Pow(1 + index * selfScaleXAffectByLevel, 1 + selfSpringLevel) + selfScaleXCenter;

                    rootBoneTransform.boneRef.ScaleX = Mathf.Lerp(rootBoneTransform.boneRef.ScaleX, initCalcScaleX + o, mixParam);
                }

                for(int i=0;i< rootBoneTransform.children.Count; i++)
                {
                    CustomBone child = rootBoneTransform.children[i];
                    CustomBone nextTarget = useTargetTransform ? targetBone.children[i] : null;
                    UpdateSineMode(time, child, nextTarget, index + 1, mixParam, endBoneName);
                }

                rootBoneTransform.boneRef.UpdateWorldTransform();
            }
        }

        void UpdateMode3Func(float time,float param)
        {
            if (rootBone.boneRef == null)
            {
                bool findRef = ManageBoneRef(rootBone);

                if (!findRef)
                {
                    return;
                }
            }

            //rootBone.boneRef.UpdateAppliedTransform();

            float m = 0 == selfMoveXAmp ? 0 : UpdateWiggleMode(selfMoveXFreq, selfMoveXAmp, selfMoveXOctaves, time, selfMoveXDelay) + selfMoveXCenter;

            m *= 0.01f; // May be there is a scale between Web And Unity

            rootBone.boneRef.X = Mathf.Lerp(rootBone.boneRef.X, rootBone.initX + m, param);

            if (selfMoveYSameAsX)
            {
                m = 0 == selfMoveXAmp ? 0 : UpdateWiggleMode(selfMoveXFreq, selfMoveXAmp, selfMoveXOctaves, time, selfMoveXDelay + selfMoveXSeed) + selfMoveXCenter;

                m *= 0.01f; // May be there is a scale between Web And Unity

                rootBone.boneRef.Y = Mathf.Lerp(rootBone.boneRef.Y, this.rootBone.initY + m, param);
            }
            else
            {
                m = 0 == selfMoveYAmp ? 0 : UpdateWiggleMode(selfMoveYFreq, selfMoveYAmp, selfMoveYOctaves, time, selfMoveYDelay) + selfMoveYCenter;

                m *= 0.01f; // May be there is a scale between Web And Unity

                rootBone.boneRef.Y = Mathf.Lerp(rootBone.boneRef.Y, rootBone.initY + m, param);
            }

            m = 0 == selfScaleXAmp ? 0 : UpdateWiggleMode(selfScaleXFreq, selfScaleXAmp, selfScaleXOctaves, time, selfScaleXDelay) + selfScaleXCenter;
            rootBone.boneRef.ScaleX = Mathf.Lerp(rootBone.boneRef.ScaleX, rootBone.initScaleX + m, param);
            if (selfScaleYSameAsX)
                rootBone.boneRef.ScaleY = Mathf.Lerp(rootBone.boneRef.ScaleY, rootBone.initScaleY + m, param);
            else
            {
                m = 0 == selfScaleYAmp ? 0 : UpdateWiggleMode(selfScaleYFreq, selfScaleYAmp, selfScaleYOctaves, time, selfScaleYDelay) + selfScaleYCenter;
                rootBone.boneRef.ScaleY = Mathf.Lerp(rootBone.boneRef.ScaleY, rootBone.initScaleY + m, param);
            }

            m = rootBone.initRotation + time * selfRotateSpeed * 360 + selfRotateCenter;
            m += 0 == selfRotateAmp ? 0 : UpdateWiggleMode(selfRotateFreq, selfRotateAmp, selfRotateOctaves, time, selfRotateDelay);

            if (selfRotateFollowEnable){

                float Q = rootBone.boneRef.WorldX - rootBone.autoMovePrevWorldX;
                float W = rootBone.boneRef.WorldY - rootBone.autoMovePrevWorldY;
                float X = 1 == selfRotateFollowFlip ?
                    -selfRotateFollowLimit * Mathf.Max(-1, Mathf.Min(1, Q / selfRotateFollowXMax)) - selfRotateFollowLimit * Mathf.Max(-1, Mathf.Min(1, W / selfRotateFollowYMax)) :
                    (Mathf.Atan2(W, Q) * selfAffectByLevel + 360) % 360;
	            float G = X - rootBone.followRotation;
                if (G >= 180)
                    X -= 360;
                else if(G <= -180) 
                    X += 360;

                rootBone.followRotation += Mathf.Min(selfRotateFollowLimit, Mathf.Max(-selfRotateFollowLimit, X - rootBone.followRotation)) * selfRotateFollowSpeed;
	            rootBone.followRotation = (rootBone.followRotation + 360) % 360;
                if (2 == selfRotateFollowFlip && Mathf.Abs(rootBone.followRotation - 180) < 90)
                    rootBone.boneRef.ScaleY *= -1;
                m += rootBone.followRotation;
            }
            
            rootBone.boneRef.Rotation = Mathf.Lerp(rootBone.boneRef.Rotation, m, param);

            rootBone.boneRef.UpdateWorldTransform();

            rootBone.autoMovePrevWorldX = rootBone.boneRef.WorldX;
            rootBone.autoMovePrevWorldY = rootBone.boneRef.WorldY;
        }

        float UpdateWiggleMode(float freq, float amp, float octives, float time, float delay, float extra = 0.5f)
        {
            float o = 0,s = 1,u = octives + 1,l = 1 / (2 - 1 / Mathf.Pow(2, u - 1)),c = l,f = 0,d = 0;

            for (;d < u; d++)
            {
                o += s * Mathf.Sin(time * c * Mathf.PI * 2 / freq + delay);
                c = l * Mathf.Pow(2, d + 1);
                f += s;
                s *= extra;
            }
            
            return o / f * amp;
        }

        void UpdateSpringMagic(CustomBone rootBoneTransform, CustomBone targetTransform, float r, int index, float param, int dimension)
        {
            if (rootBoneTransform.boneRef == null)
            {
                bool findRef = ManageBoneRef(rootBoneTransform);

                if (!findRef)
                {
                    return;
                }
            }

            if (selfEndBoneName != null && !selfEndBoneName.Contains(rootBoneTransform.transform.name))
            {
                rootBoneTransform.boneRef.UpdateWorldTransform();
                rootBoneTransform.autoMovePrevWorldX = rootBoneTransform.boneRef.WorldX;
                rootBoneTransform.autoMovePrevWorldY = rootBoneTransform.boneRef.WorldY;

                bool useTargetTransform = targetTransform != null && !targetTransform.name.Equals(selfTargetEndBoneName);

                float initCalcRotation = useTargetTransform ? Mathf.Lerp(rootBoneTransform.initRotation, targetTransform.boneRef.Rotation, selfTargetWeight) : rootBoneTransform.initRotation;

                CustomBone boneToUse = selfSpringUseTarget != 0 && targetTransform != null ? targetTransform : rootBoneTransform;
                float h_2 = 1 + index * selfAffectByLevel;

                float l = Mathf.Pow(h_2, 1 + selfSpringLevel);
                float c = selfDelay * l * (1 + selfSpringRot * h_2) * r * (0 == index ? 1 + selfSpring : 1);

                if (rootBoneTransform.children.Count > 0)
                {
                    for (int f = 0; f < rootBoneTransform.children.Count; f++)
                    {
                        var ll = rootBoneTransform.children[f];

                        if (ll.boneRef == null)
                        {
                            bool findChildRef = ManageBoneRef(ll);

                            if (!findChildRef)
                            {
                                continue;
                            }
                        }

                        if (f == 0)
                        {
                            float d = ll.boneRef.X;
                            float p = ll.boneRef.Y;
                            float m = d * boneToUse.boneRef.A + p * boneToUse.boneRef.B + rootBoneTransform.boneRef.WorldX;
                            float h = d * boneToUse.boneRef.C + p * boneToUse.boneRef.D + rootBoneTransform.boneRef.WorldY;

                            m = (m - ll.autoMovePrevWorldX) * c;
                            h = (h - ll.autoMovePrevWorldY) * c;
                            rootBoneTransform.autoMoveSpeedX += m;
                            rootBoneTransform.autoMoveSpeedY += h;

                            rootBoneTransform.autoMoveSpeedX *= selfFriction;
                            rootBoneTransform.autoMoveSpeedY *= selfFriction;

                            float v = ll.autoMovePrevWorldX + rootBoneTransform.autoMoveSpeedX;
                            float A = ll.autoMovePrevWorldY + rootBoneTransform.autoMoveSpeedY;
                            float g = rootBoneTransform.boneRef.WorldToLocalRotation(dimension * Mathf.Atan2(A - rootBoneTransform.boneRef.WorldY, dimension * (v - rootBoneTransform.boneRef.WorldX)) * 180 / Mathf.PI + (0 == index ? selfRotateOffset : 0)),
                            y = Mathf.Min(selfLimitRange, Mathf.Max(-selfLimitRange, g - initCalcRotation)) + initCalcRotation;

                            float targetRotate = (initCalcRotation * selfSpeed + (1 - selfSpeed) * y);
                            rootBoneTransform.boneRef.Rotation = Mathf.Lerp(rootBoneTransform.boneRef.Rotation, targetRotate, param * rootBoneTransform.autoMoveFriction);
                            rootBoneTransform.boneRef.UpdateWorldTransform();
                        }
                        CustomBone nextTarget = useTargetTransform ? targetBone.children[f] : null;
                        UpdateSpringMagic(ll, nextTarget, r, index + 1, param, dimension);
                    }
                }
                else
                {
                    float f = rootBoneTransform.boneRef.X, d = rootBoneTransform.boneRef.Y;
                    float p = f * boneToUse.boneRef.A + d * boneToUse.boneRef.B + rootBoneTransform.boneRef.WorldX;
                    float m = f * boneToUse.boneRef.C + d * boneToUse.boneRef.D + rootBoneTransform.boneRef.WorldY;
                    p = (p - rootBoneTransform.tailAutoMovePrevWorldX) * c;
                    m = (m - rootBoneTransform.tailAutoMovePrevWorldY) * c;
                    rootBoneTransform.autoMoveSpeedX += p;
                    rootBoneTransform.autoMoveSpeedY += m;
                    rootBoneTransform.autoMoveSpeedX *= selfFriction;
                    rootBoneTransform.autoMoveSpeedY *= selfFriction;

                    float h = rootBoneTransform.tailAutoMovePrevWorldX + rootBoneTransform.autoMoveSpeedX;
                    float v = rootBoneTransform.tailAutoMovePrevWorldY + rootBoneTransform.autoMoveSpeedY;
                    float A = rootBoneTransform.boneRef.WorldToLocalRotation(dimension * Mathf.Atan2(v - rootBoneTransform.boneRef.WorldY, dimension * (h - rootBoneTransform.boneRef.WorldX)) * 180 / Mathf.PI + (0 == index ? selfRotateOffset : 0));

                    float g = Mathf.Min(selfLimitRange, Mathf.Max(-selfLimitRange, A - initCalcRotation)) + initCalcRotation;
                    float targetRotate = (initCalcRotation * selfSpeed + (1 - selfSpeed) * g);
                    rootBoneTransform.boneRef.Rotation = Mathf.Lerp(rootBoneTransform.boneRef.Rotation, targetRotate, param * rootBoneTransform.autoMoveFriction) ;
                    rootBoneTransform.boneRef.UpdateWorldTransform();

                    rootBoneTransform.tailAutoMovePrevWorldX = f * rootBoneTransform.boneRef.A + d * rootBoneTransform.boneRef.B + rootBoneTransform.boneRef.WorldX;
                    rootBoneTransform.tailAutoMovePrevWorldY = f * rootBoneTransform.boneRef.C + d * rootBoneTransform.boneRef.D + rootBoneTransform.boneRef.WorldY;

                }
                rootBoneTransform.autoMoveFriction += .1f * (1 - rootBoneTransform.autoMoveFriction) * r;

                //boneTransform.autoMoveFriction = Mathf.Max(-1, Mathf.Min(1, boneTransform.autoMoveFriction));
                fTime = rootBoneTransform.autoMoveFriction;
            }
        }

    }
}
